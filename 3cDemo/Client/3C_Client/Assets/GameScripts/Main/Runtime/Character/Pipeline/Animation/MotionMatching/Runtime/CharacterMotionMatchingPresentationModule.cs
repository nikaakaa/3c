using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    internal enum MotionMatchingFrameTransactionInvalidReason : byte
    {
        PreviousFrameIncomplete = 1,
        DuplicateResolve = 2,
        DuplicateDemand = 3,
        DemandCapacityExceeded = 4,
        MissingTrajectory = 5,
        FixtureDemandMismatch = 6,
        RequestCapacityExceeded = 7,
        RetainedOutputMissing = 8,
        MissingResolve = 9,
        CompletionIdentityMismatch = 10,
        PresentationFrameMismatch = 11,
        ResetIdentityMismatch = 12
    }

    internal sealed class MotionMatchingFrameTransactionException : InvalidOperationException
    {
        internal MotionMatchingFrameTransactionException(
            MotionMatchingFrameTransactionInvalidReason reason,
            string message)
            : base(message)
        {
            Reason = reason;
        }

        internal MotionMatchingFrameTransactionInvalidReason Reason { get; }
    }

    internal enum MotionMatchingPresentationResetReason : byte
    {
        BodyStreamReset = 1,
        AnimationBranchReplacement = 2,
        PresentationReset = 3,
        ProjectionReplacement = 4
    }

    internal readonly struct MotionMatchingResolvedFrameRequest
    {
        internal MotionMatchingResolvedFrameRequest(
            in AnimationSourcePoseSample sourceSample,
            bool submitToLifecycle)
        {
            SourceSample = sourceSample;
            SubmitToLifecycle = submitToLifecycle;
        }

        internal AnimationSourcePoseSample SourceSample { get; }
        internal AnimationSelectionFrame Selection => SourceSample.Selection;
        internal bool SubmitToLifecycle { get; }
    }

    internal readonly struct MotionMatchingFrameResolution
    {
        readonly MotionMatchingResolvedFrameRequest[] m_Requests;

        internal MotionMatchingFrameResolution(
            ulong presentationFrame,
            ulong resetSequence,
            ulong completionIdentity,
            MotionMatchingResolvedFrameRequest[] requests,
            int requestCount,
            int resolvedProducerCount,
            bool requiresHistoryCompletion)
        {
            if (presentationFrame == 0 || completionIdentity == 0 || requests == null ||
                requestCount < 0 || requestCount > requests.Length || resolvedProducerCount < 0)
                throw new ArgumentException("Motion Matching frame resolution is invalid.");
            PresentationFrame = presentationFrame;
            ResetSequence = resetSequence;
            CompletionIdentity = completionIdentity;
            m_Requests = requests;
            RequestCount = requestCount;
            ResolvedProducerCount = resolvedProducerCount;
            RequiresHistoryCompletion = requiresHistoryCompletion;
        }

        internal ulong PresentationFrame { get; }
        internal ulong ResetSequence { get; }
        internal ulong CompletionIdentity { get; }
        internal int RequestCount { get; }
        internal int ResolvedProducerCount { get; }
        internal bool RequiresHistoryCompletion { get; }

        internal MotionMatchingResolvedFrameRequest GetRequest(int index) =>
            (uint)index < (uint)RequestCount
                ? m_Requests[index]
                : throw new ArgumentOutOfRangeException(nameof(index));
    }

    internal sealed class CharacterMotionMatchingPresentationModule : IDisposable
    {
        readonly ActorId m_ActorId;
        readonly CharacterBodyPresentationSourceMode m_BodySourceMode;
        readonly CharacterPresentationProjection m_Projection;
        readonly MotionMatchingTrajectoryAdapter m_TrajectoryAdapter;
        readonly Dictionary<string, CharacterMotionMatchingProducerRuntime> m_Producers =
            new Dictionary<string, CharacterMotionMatchingProducerRuntime>(StringComparer.Ordinal);
        readonly Dictionary<AnimationPlaybackId, CharacterPresentationProducerEntry> m_Sampling =
            new Dictionary<AnimationPlaybackId, CharacterPresentationProducerEntry>();
        readonly Dictionary<AnimationPoseSourceId, MotionMatchingPoseSourceOutput> m_FrozenOutputs =
            new Dictionary<AnimationPoseSourceId, MotionMatchingPoseSourceOutput>();
        readonly HashSet<string> m_ResolvedProducers = new HashSet<string>(StringComparer.Ordinal);
        readonly MotionMatchingPlaybackDemand[] m_Demands;
        readonly MotionMatchingFrameSelection[] m_FrameSelections;
        readonly MotionMatchingResolvedFrameRequest[] m_ResolvedRequests;
        readonly List<AnimationPoseSourceId> m_RemoveOutputs;
        readonly List<AnimationPlaybackId> m_RemoveSampling;

        int m_DemandCount;
        int m_FrameSelectionCount;
        int m_ResolvedRequestCount;
        ulong m_ResetSequence;
        ulong m_PreviousResetSequence;
        ulong m_CompletionSequence;
        ulong m_OpenCompletionIdentity;
        ulong m_OpenPresentationFrame;
        ulong m_OpenResetSequence;
        ulong m_LastResolvedPresentationFrame;
        ulong m_LastCompletedPresentationFrame;
        int m_LastHistoryAppendCount;
        int m_LastHistoryGapCount;
        MotionMatchingSearchReplayArtifact m_FixtureQuery;
        string m_FixtureProducerId;
        MotionMatchingPresentationResetReason m_LastResetReason;
        bool m_FrameOpen;
        bool m_Disposed;

        internal CharacterMotionMatchingPresentationModule(
            ActorId actorId,
            CharacterBodyPresentationSourceMode bodySourceMode,
            CharacterPresentationProjection projection,
            int sourceCapacity)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Motion Matching Presentation Module Actor identity is invalid.", nameof(actorId));
            if (bodySourceMode != CharacterBodyPresentationSourceMode.CommittedStream &&
                bodySourceMode != CharacterBodyPresentationSourceMode.SelectedStream)
                throw new ArgumentOutOfRangeException(nameof(bodySourceMode));
            if (projection == null || projection.MotionMatching == null)
                throw new ArgumentException("Motion Matching Presentation Module requires a compiled Projection payload.", nameof(projection));
            if (sourceCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(sourceCapacity));

            m_ActorId = actorId;
            m_BodySourceMode = bodySourceMode;
            m_Projection = projection;
            m_Demands = new MotionMatchingPlaybackDemand[sourceCapacity];
            m_FrameSelections = new MotionMatchingFrameSelection[sourceCapacity];
            m_ResolvedRequests = new MotionMatchingResolvedFrameRequest[sourceCapacity];
            m_RemoveOutputs = new List<AnimationPoseSourceId>(sourceCapacity);
            m_RemoveSampling = new List<AnimationPlaybackId>(sourceCapacity);
            string actorSuffix = actorId.ToString();
            m_TrajectoryAdapter = bodySourceMode == CharacterBodyPresentationSourceMode.SelectedStream
                ? new SelectedBodyTrajectoryAdapter(
                    actorId,
                    new MotionMatchingTrajectorySourceIdentity("selected-body/" + actorSuffix))
                : new AcceptedIntentTrajectoryAdapter(
                    actorId,
                    new MotionMatchingTrajectorySourceIdentity("accepted-intent/" + actorSuffix));
            try
            {
                BuildProducerRuntimes(projection.MotionMatching);
            }
            catch
            {
                DisposeProducerRuntimes();
                m_TrajectoryAdapter.Dispose();
                throw;
            }
        }

        internal bool Enabled => true;
        internal bool AcceptsTrajectoryIntent => m_TrajectoryAdapter.AcceptsIntent;
        internal int FrozenOutputCount => m_FrozenOutputs.Count;
        internal ulong LastResolvedCompletionIdentity => m_OpenCompletionIdentity;
        internal ulong LastCompletedPresentationFrame => m_LastCompletedPresentationFrame;
        internal int LastHistoryAppendCount => m_LastHistoryAppendCount;
        internal int LastHistoryGapCount => m_LastHistoryGapCount;

        internal bool HasFrameWork(AnimationPosePlayableGraphRuntime poseRuntime)
        {
            RequireAlive();
            if (poseRuntime == null)
                throw new ArgumentNullException(nameof(poseRuntime));
            if (m_DemandCount > 0 || m_FrozenOutputs.Count > 0 || m_FixtureQuery != null)
                return true;
            return false;
        }

        internal void CaptureTrajectoryIntent(CharacterPresentationTrajectoryIntent intent)
        {
            RequireAlive();
            m_TrajectoryAdapter.CaptureIntent(intent);
        }

        internal void CaptureFixtureQuery(
            string programProducerId,
            MotionMatchingSearchReplayArtifact fixture)
        {
            RequireAlive();
            if (fixture == null || string.IsNullOrWhiteSpace(programProducerId) ||
                !m_Producers.ContainsKey(programProducerId) ||
                !string.Equals(
                    fixture.ProjectionIdentity,
                    $"{m_Projection.ProgramId}@{m_Projection.SourceRevision}:{m_Projection.ContractHash}",
                    StringComparison.Ordinal))
                throw new InvalidOperationException("Motion Matching Query Fixture identity does not match this Module.");
            if (m_FixtureQuery != null)
                throw new InvalidOperationException("Motion Matching Module already has a pending Query Fixture input.");
            if (fixture.ResetSequence != m_ResetSequence)
                Reset(fixture.ResetSequence, MotionMatchingPresentationResetReason.PresentationReset, false);
            m_FixtureProducerId = programProducerId;
            m_FixtureQuery = fixture;
        }

        internal void PublishSample(
            AnimationPlaybackId playbackId,
            CharacterPresentationProducerEntry producer)
        {
            RequireAlive();
            RequireProducer(producer);
            if (!playbackId.IsValid || !playbackId.ProducerId.Equals(producer.ProducerId))
                throw new ArgumentException("Motion Matching sample Playback identity is invalid.", nameof(playbackId));
            m_Sampling[playbackId] = producer;
        }

        internal void ReplaceSample(
            AnimationPlaybackId playbackId,
            CharacterPresentationProducerEntry producer)
        {
            RequireAlive();
            RequireProducer(producer);
            if (!playbackId.IsValid || !playbackId.ProducerId.Equals(producer.ProducerId))
                throw new ArgumentException("Motion Matching replacement Playback identity is invalid.", nameof(playbackId));
            RequireProducerRuntime(producer.ProgramProducerIdentity).Reset(m_ResetSequence);
            m_LastResetReason = MotionMatchingPresentationResetReason.AnimationBranchReplacement;
            m_Sampling[playbackId] = producer;
        }

        internal void ReplaceSelection(
            CharacterPresentationProducerEntry current,
            CharacterPresentationProducerEntry replacement)
        {
            RequireAlive();
            if (current?.AnimationSourceKind == AnimationPoseSourceKind.MotionMatching)
                RequireProducerRuntime(current.ProgramProducerIdentity).Reset(m_ResetSequence);
            if (replacement?.AnimationSourceKind == AnimationPoseSourceKind.MotionMatching &&
                (current == null || !string.Equals(
                    current.ProgramProducerIdentity,
                    replacement.ProgramProducerIdentity,
                    StringComparison.Ordinal)))
                RequireProducerRuntime(replacement.ProgramProducerIdentity).Reset(m_ResetSequence);
            m_LastResetReason = MotionMatchingPresentationResetReason.AnimationBranchReplacement;
        }

        internal void RetireSample(AnimationPlaybackId playbackId, bool retained)
        {
            RequireAlive();
            if (retained || !m_Sampling.TryGetValue(playbackId, out CharacterPresentationProducerEntry producer))
                return;
            RequireProducerRuntime(producer.ProgramProducerIdentity).ReleaseDomain();
            m_Sampling.Remove(playbackId);
            RemoveFrozenOutputs(playbackId);
        }

        internal bool ContainsSampling(AnimationPlaybackId playbackId) =>
            m_Sampling.ContainsKey(playbackId);

        internal bool TryGetSamplingChannel(
            AnimationPlaybackId playbackId,
            out AnimationChannelId animationChannelId)
        {
            RequireAlive();
            if (m_Sampling.TryGetValue(playbackId, out CharacterPresentationProducerEntry producer))
            {
                animationChannelId = producer.AnimationChannelId;
                return true;
            }
            animationChannelId = default;
            return false;
        }

        internal void BeginDemandFrame()
        {
            RequireAlive();
            if (m_FrameOpen)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.PreviousFrameIncomplete,
                    "Motion Matching cannot begin demand collection before the previous frame completes.");
            Array.Clear(m_Demands, 0, m_DemandCount);
            m_DemandCount = 0;
        }

        internal void SubmitDemand(AnimationPlaybackId playbackId)
        {
            RequireAlive();
            if (!m_Sampling.TryGetValue(playbackId, out CharacterPresentationProducerEntry producer))
                throw new InvalidOperationException($"Motion Matching Playback '{playbackId}' has no sampled producer.");
            for (int i = 0; i < m_DemandCount; i++)
            {
                if (m_Demands[i].PlaybackId.Equals(playbackId))
                    throw FrameFailure(
                        MotionMatchingFrameTransactionInvalidReason.DuplicateDemand,
                        $"Motion Matching Playback '{playbackId}' was demanded twice in one frame.");
            }
            if (m_DemandCount >= m_Demands.Length)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.DemandCapacityExceeded,
                    "Motion Matching playback demand capacity was exceeded.");
            m_Demands[m_DemandCount++] = new MotionMatchingPlaybackDemand(playbackId, producer);
        }

        internal MotionMatchingFrameResolution ResolveFrame(
            ulong presentationFrame,
            float presentationDeltaSeconds,
            in CharacterBodyPresentationFrame bodyFrame,
            AnimationPoseRequestWorkspace requestWorkspace,
            AnimationPosePlayableGraphRuntime poseRuntime,
            Func<ulong> nextPresentationRequestSequence,
            RuntimeDiagnosticsContext diagnostics)
        {
            RequireAlive();
            MotionMatchingSearchReplayArtifact fixtureQuery = m_FixtureQuery;
            string fixtureProducerId = m_FixtureProducerId;
            bool fixtureFrame = fixtureQuery != null;
            if (presentationFrame == m_LastResolvedPresentationFrame)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.DuplicateResolve,
                    "Motion Matching cannot Resolve twice in one Presentation frame.");
            if (presentationFrame == 0 ||
                !fixtureFrame && (!bodyFrame.IsValid || bodyFrame.SourceMode != m_BodySourceMode) ||
                !float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f ||
                requestWorkspace == null || poseRuntime == null || nextPresentationRequestSequence == null)
                throw new ArgumentException("Motion Matching Resolve frame input is invalid.");
            if (m_FrameOpen)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.PreviousFrameIncomplete,
                    "Motion Matching cannot Resolve a new frame before the previous frame completes.");
            if (!fixtureFrame && bodyFrame.ResetSequence != m_ResetSequence)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.ResetIdentityMismatch,
                    "Motion Matching Body Reset identity was not applied before Resolve.");

            ulong frameResetSequence = fixtureFrame ? fixtureQuery.ResetSequence : bodyFrame.ResetSequence;
            if (fixtureFrame && frameResetSequence != m_ResetSequence)
                Reset(frameResetSequence, MotionMatchingPresentationResetReason.PresentationReset, false);

            m_ResolvedProducers.Clear();
            Array.Clear(m_FrameSelections, 0, m_FrameSelectionCount);
            Array.Clear(m_ResolvedRequests, 0, m_ResolvedRequestCount);
            m_FrameSelectionCount = 0;
            m_ResolvedRequestCount = 0;
            MotionMatchingTrajectorySourceFrame trajectoryFrame = default;
            bool hasTrajectory = fixtureFrame || m_DemandCount == 0;
            if (!hasTrajectory)
                hasTrajectory = m_TrajectoryAdapter.TryResolve(in bodyFrame, out trajectoryFrame);
            if (!hasTrajectory)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.MissingTrajectory,
                    "Selected Motion Matching playback requires a formal trajectory input for the current Body branch.");

            for (int demandIndex = 0; demandIndex < m_DemandCount; demandIndex++)
            {
                MotionMatchingPlaybackDemand demand = m_Demands[demandIndex];
                CharacterMotionMatchingProducerRuntime runtime = RequireProducerRuntime(
                    demand.Producer.ProgramProducerIdentity);
                if (fixtureFrame && (m_DemandCount != 1 ||
                                     !string.Equals(demand.Producer.ProgramProducerIdentity, fixtureProducerId, StringComparison.Ordinal)))
                    throw FrameFailure(
                        MotionMatchingFrameTransactionInvalidReason.FixtureDemandMismatch,
                        "Motion Matching Query Fixture must target one selected producer demand.");
                MotionMatchingPoseSourceOutput output = fixtureFrame
                    ? runtime.ResolveFixture(
                        presentationFrame,
                        presentationDeltaSeconds,
                        fixtureQuery,
                        demand.PlaybackId,
                        nextPresentationRequestSequence(),
                        demand.Producer.ProgramProducerIndex,
                        diagnostics)
                    : runtime.Resolve(
                        presentationFrame,
                        presentationDeltaSeconds,
                        trajectoryFrame,
                        demand.PlaybackId,
                        nextPresentationRequestSequence(),
                        demand.Producer.ProgramProducerIndex,
                        diagnostics);
                var sourceId = new AnimationPoseSourceId(
                    output.PlaybackId,
                    AnimationPoseSourceKind.MotionMatching,
                    new AnimationPoseSelectionGeneration(output.SelectionGeneration.Value));
                m_FrozenOutputs[sourceId] = output;
                AddRequest(in output, requestWorkspace, true);
                m_ResolvedProducers.Add(runtime.ProgramProducerId);
                if (m_FrameSelectionCount >= m_FrameSelections.Length)
                    throw FrameFailure(
                        MotionMatchingFrameTransactionInvalidReason.DemandCapacityExceeded,
                        "Motion Matching frame selection capacity was exceeded.");
                m_FrameSelections[m_FrameSelectionCount++] = new MotionMatchingFrameSelection(runtime, demand.PlaybackId);
            }
            AddRetainedRequests(poseRuntime, requestWorkspace);
            foreach (CharacterMotionMatchingProducerRuntime runtime in m_Producers.Values)
            {
                if (!m_ResolvedProducers.Contains(runtime.ProgramProducerId))
                    runtime.ReleaseDomain();
            }

            ulong completionIdentity = NextCompletionIdentity();
            m_OpenCompletionIdentity = completionIdentity;
            m_OpenPresentationFrame = presentationFrame;
            m_OpenResetSequence = frameResetSequence;
            m_LastResolvedPresentationFrame = presentationFrame;
            m_FrameOpen = true;
            m_DemandCount = 0;
            m_FixtureQuery = null;
            m_FixtureProducerId = null;
            PublishFrameDiagnostics(diagnostics, "Resolved", completionIdentity, m_ResolvedRequestCount, 0, 0);
            return new MotionMatchingFrameResolution(
                presentationFrame,
                frameResetSequence,
                completionIdentity,
                m_ResolvedRequests,
                m_ResolvedRequestCount,
                m_ResolvedProducers.Count,
                m_FrameSelectionCount > 0);
        }

        internal void CompleteFrame(
            in MotionMatchingFrameResolution resolution,
            AnimationPosePlayableGraphRuntime poseRuntime,
            RuntimeDiagnosticsContext diagnostics)
        {
            RequireAlive();
            if (!m_FrameOpen || resolution.CompletionIdentity == 0)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.MissingResolve,
                    "Motion Matching Complete has no open Resolve frame transaction.");
            if (resolution.CompletionIdentity != m_OpenCompletionIdentity)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.CompletionIdentityMismatch,
                    "Motion Matching Complete identity does not match the open Resolve frame transaction.");
            if (resolution.PresentationFrame != m_OpenPresentationFrame)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.PresentationFrameMismatch,
                    "Motion Matching Complete Presentation frame does not match Resolve.");
            if (resolution.ResetSequence != m_OpenResetSequence || resolution.ResetSequence != m_ResetSequence)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.ResetIdentityMismatch,
                    "Motion Matching Complete Reset identity does not match Resolve.");
            if (poseRuntime == null)
                throw new ArgumentNullException();

            int appended = 0;
            int gaps = 0;
            for (int selectionIndex = 0; selectionIndex < m_FrameSelectionCount; selectionIndex++)
            {
                MotionMatchingFrameSelection selection = m_FrameSelections[selectionIndex];
                CharacterMotionMatchingProducerRuntime runtime = selection.Runtime;
                if (!poseRuntime.TryCopyPlayerPose(
                        runtime.PoseNodeId,
                        runtime.FeatureRigBoneIndices,
                        runtime.FeatureBonePositionWorkspace,
                        out AnimationFootPlacementSample footPlacement))
                {
                    runtime.History.MarkGap(m_ResetSequence);
                    gaps++;
                    continue;
                }
                runtime.AppendBasePose(resolution.PresentationFrame, selection.PlaybackId, footPlacement);
                appended++;
            }

            PruneFrozenOutputs(poseRuntime);
            m_LastCompletedPresentationFrame = resolution.PresentationFrame;
            m_LastHistoryAppendCount = appended;
            m_LastHistoryGapCount = gaps;
            m_FrameOpen = false;
            m_OpenCompletionIdentity = 0;
            m_OpenPresentationFrame = 0;
            m_OpenResetSequence = 0;
            Array.Clear(m_FrameSelections, 0, m_FrameSelectionCount);
            m_FrameSelectionCount = 0;
            PublishFrameDiagnostics(
                diagnostics,
                "Completed",
                resolution.CompletionIdentity,
                resolution.RequestCount,
                appended,
                gaps);
        }

        internal void PruneUnreferencedSampling(
            AnimationPlaybackLifecycle lifecycle,
            AnimationPosePlayableGraphRuntime poseRuntime,
            Func<AnimationPlaybackId, bool> hasRawSelection,
            List<AnimationPlaybackId> retiredPlaybacks)
        {
            RequireAlive();
            if (lifecycle == null || poseRuntime == null || hasRawSelection == null || retiredPlaybacks == null)
                throw new ArgumentNullException();
            m_RemoveSampling.Clear();
            foreach (AnimationPlaybackId playbackId in m_Sampling.Keys)
            {
                if (!lifecycle.Retains(playbackId, poseRuntime) && !hasRawSelection(playbackId))
                    m_RemoveSampling.Add(playbackId);
            }
            for (int i = 0; i < m_RemoveSampling.Count; i++)
            {
                AnimationPlaybackId playbackId = m_RemoveSampling[i];
                m_Sampling.Remove(playbackId);
                retiredPlaybacks.Add(playbackId);
            }
        }

        internal bool TryCaptureSearchReplay(
            string programProducerId,
            out MotionMatchingSearchReplayArtifact artifact)
        {
            RequireAlive();
            artifact = null;
            return !string.IsNullOrWhiteSpace(programProducerId) &&
                   m_Producers.TryGetValue(programProducerId, out CharacterMotionMatchingProducerRuntime runtime) &&
                   runtime.TryCaptureSearchReplay(out artifact);
        }

        internal void Reset(
            ulong resetSequence,
            MotionMatchingPresentationResetReason reason,
            bool clearSampling)
        {
            RequireAlive();
            if (!Enum.IsDefined(typeof(MotionMatchingPresentationResetReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            m_TrajectoryAdapter.Reset(resetSequence);
            foreach (CharacterMotionMatchingProducerRuntime runtime in m_Producers.Values)
                runtime.Reset(resetSequence);
            if (clearSampling)
                m_Sampling.Clear();
            m_FrozenOutputs.Clear();
            m_ResolvedProducers.Clear();
            if (clearSampling)
                Array.Clear(m_Demands, 0, m_DemandCount);
            Array.Clear(m_FrameSelections, 0, m_FrameSelectionCount);
            Array.Clear(m_ResolvedRequests, 0, m_ResolvedRequestCount);
            if (clearSampling)
                m_DemandCount = 0;
            m_FrameSelectionCount = 0;
            m_ResolvedRequestCount = 0;
            m_RemoveOutputs.Clear();
            m_RemoveSampling.Clear();
            m_PreviousResetSequence = m_ResetSequence;
            m_ResetSequence = resetSequence;
            m_OpenCompletionIdentity = 0;
            m_OpenPresentationFrame = 0;
            m_OpenResetSequence = 0;
            m_LastResolvedPresentationFrame = 0;
            m_LastCompletedPresentationFrame = 0;
            m_LastHistoryAppendCount = 0;
            m_LastHistoryGapCount = 0;
            m_LastResetReason = reason;
            m_FixtureQuery = null;
            m_FixtureProducerId = null;
            m_FrameOpen = false;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_FrameOpen = false;
            m_TrajectoryAdapter.Dispose();
            DisposeProducerRuntimes();
            m_Sampling.Clear();
            m_FrozenOutputs.Clear();
            m_ResolvedProducers.Clear();
            m_RemoveOutputs.Clear();
            m_RemoveSampling.Clear();
            m_FixtureQuery = null;
            m_FixtureProducerId = null;
        }

        void BuildProducerRuntimes(MotionMatchingProjectionPayload payload)
        {
            for (int bindingIndex = 0; bindingIndex < payload.ProducerBindingCount; bindingIndex++)
            {
                MotionMatchingProducerBindingPayload binding = payload.GetProducerBinding(bindingIndex);
                if (!m_Projection.TryGetProducer(binding.ProgramProducerId, out CharacterPresentationProducerEntry producer) ||
                    producer.Kind != CharacterPresentationProducerKind.Animation ||
                    producer.AnimationSourceKind != AnimationPoseSourceKind.MotionMatching ||
                    !producer.AnimationChannelId.Equals(binding.AnimationChannelId) ||
                    !HasSelectionInput(binding))
                    throw new InvalidOperationException($"Motion Matching producer binding '{binding.ProgramProducerId}' does not match the Projection producer.");
                m_Producers.Add(
                    binding.ProgramProducerId,
                    new CharacterMotionMatchingProducerRuntime(
                        $"{m_Projection.ProgramId}@{m_Projection.SourceRevision}:{m_Projection.ContractHash}",
                        payload,
                        binding,
                        m_Projection.Rig));
            }
            if (m_Producers.Count == 0)
                throw new InvalidOperationException("Motion Matching Projection payload has no producer runtime binding.");
        }

        bool HasSelectionInput(MotionMatchingProducerBindingPayload binding)
        {
            int count = 0;
            for (int i = 0; i < m_Projection.PosePlan.SelectionInputs.Count; i++)
            {
                CharacterPresentationSelectionInputEntry input = m_Projection.PosePlan.SelectionInputs[i];
                if (input.MotionMatching && input.NodeId == binding.PoseNodeId &&
                    input.AnimationChannelId == binding.AnimationChannelId &&
                    string.Equals(input.ProgramProducerId, binding.ProgramProducerId, StringComparison.Ordinal))
                    count++;
            }
            return count == 1;
        }

        void AddRetainedRequests(
            AnimationPosePlayableGraphRuntime poseRuntime,
            AnimationPoseRequestWorkspace requestWorkspace)
        {
            foreach (KeyValuePair<AnimationPoseSourceId, MotionMatchingPoseSourceOutput> pair in m_FrozenOutputs)
            {
                if (!poseRuntime.RetainsSource(pair.Key) || ContainsResolvedRequest(pair.Key))
                    continue;
                MotionMatchingPoseSourceOutput output = pair.Value;
                AddRequest(in output, requestWorkspace, false);
            }
        }

        void AddRequest(
            in MotionMatchingPoseSourceOutput output,
            AnimationPoseRequestWorkspace requestWorkspace,
            bool submitToLifecycle)
        {
            var sourceId = new AnimationPoseSourceId(
                output.PlaybackId,
                AnimationPoseSourceKind.MotionMatching,
                new AnimationPoseSelectionGeneration(output.SelectionGeneration.Value));
            if (ContainsResolvedRequest(sourceId))
                return;
            if (m_ResolvedRequestCount >= m_ResolvedRequests.Length)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.RequestCapacityExceeded,
                    "Motion Matching resolved request capacity was exceeded.");
            AnimationSourcePoseSample sourceSample = MotionMatchingSelectionFactory.Create(
                in output,
                m_Projection.PosePlan,
                requestWorkspace);
            m_ResolvedRequests[m_ResolvedRequestCount++] = new MotionMatchingResolvedFrameRequest(
                in sourceSample,
                submitToLifecycle);
        }

        bool ContainsResolvedRequest(AnimationPoseSourceId sourceId)
        {
            for (int i = 0; i < m_ResolvedRequestCount; i++)
            {
                if (m_ResolvedRequests[i].Selection.SourceId.Equals(sourceId))
                    return true;
            }
            return false;
        }

        void PruneFrozenOutputs(AnimationPosePlayableGraphRuntime poseRuntime)
        {
            m_RemoveOutputs.Clear();
            foreach (AnimationPoseSourceId sourceId in m_FrozenOutputs.Keys)
            {
                if (!poseRuntime.RetainsSource(sourceId))
                    m_RemoveOutputs.Add(sourceId);
            }
            for (int i = 0; i < m_RemoveOutputs.Count; i++)
                m_FrozenOutputs.Remove(m_RemoveOutputs[i]);
        }

        void RemoveFrozenOutputs(AnimationPlaybackId playbackId)
        {
            m_RemoveOutputs.Clear();
            foreach (AnimationPoseSourceId sourceId in m_FrozenOutputs.Keys)
            {
                if (sourceId.PlaybackId.Equals(playbackId))
                    m_RemoveOutputs.Add(sourceId);
            }
            for (int i = 0; i < m_RemoveOutputs.Count; i++)
                m_FrozenOutputs.Remove(m_RemoveOutputs[i]);
        }

        CharacterMotionMatchingProducerRuntime RequireProducerRuntime(string programProducerId) =>
            m_Producers.TryGetValue(programProducerId, out CharacterMotionMatchingProducerRuntime runtime)
                ? runtime
                : throw new InvalidOperationException($"Motion Matching producer '{programProducerId}' has no compiled Runtime workspace.");

        void RequireProducer(CharacterPresentationProducerEntry producer)
        {
            if (producer == null || producer.Kind != CharacterPresentationProducerKind.Animation ||
                producer.AnimationSourceKind != AnimationPoseSourceKind.MotionMatching ||
                !m_Producers.ContainsKey(producer.ProgramProducerIdentity))
                throw new InvalidOperationException("Motion Matching sample targets a producer outside this Module.");
        }

        ulong NextCompletionIdentity()
        {
            if (m_CompletionSequence == ulong.MaxValue)
                throw new InvalidOperationException("Motion Matching frame completion identity was exhausted.");
            return ++m_CompletionSequence;
        }

        void PublishFrameDiagnostics(
            RuntimeDiagnosticsContext diagnostics,
            string status,
            ulong completionIdentity,
            int requestCount,
            int historyAppended,
            int historyGaps)
        {
            if (diagnostics == null ||
                !diagnostics.ShouldPublish(RuntimeTraceChannel.Animation, RuntimeTraceEventKind.MotionMatchingFrame))
                return;
            diagnostics.Publish(
                RuntimeTraceChannel.Animation,
                RuntimeTraceDomain.Presentation,
                RuntimeTraceEventKind.MotionMatchingFrame,
                RuntimeSourceElementHandle.Invalid,
                RuntimeInstanceKey.Character(diagnostics.CharacterRuntimeId),
                new RuntimeTracePayload
                {
                    Status = status,
                    OwnerId = m_ActorId.ToString(),
                    Cause = m_LastResetReason.ToString(),
                    Priority = requestCount,
                    Detail = $"completion={completionIdentity};frame={m_LastResolvedPresentationFrame};previousReset={m_PreviousResetSequence};reset={m_ResetSequence};requests={requestCount};historyAppended={historyAppended};historyGaps={historyGaps};retainedFrozen={m_FrozenOutputs.Count}"
                });
        }

        void DisposeProducerRuntimes()
        {
            foreach (CharacterMotionMatchingProducerRuntime runtime in m_Producers.Values)
                runtime.Dispose();
            m_Producers.Clear();
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterMotionMatchingPresentationModule));
        }

        static MotionMatchingFrameTransactionException FrameFailure(
            MotionMatchingFrameTransactionInvalidReason reason,
            string message) => new MotionMatchingFrameTransactionException(reason, message);

        readonly struct MotionMatchingPlaybackDemand
        {
            internal MotionMatchingPlaybackDemand(
                AnimationPlaybackId playbackId,
                CharacterPresentationProducerEntry producer)
            {
                PlaybackId = playbackId;
                Producer = producer ?? throw new ArgumentNullException(nameof(producer));
            }

            internal AnimationPlaybackId PlaybackId { get; }
            internal CharacterPresentationProducerEntry Producer { get; }
        }

        readonly struct MotionMatchingFrameSelection
        {
            internal MotionMatchingFrameSelection(
                CharacterMotionMatchingProducerRuntime runtime,
                AnimationPlaybackId playbackId)
            {
                Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
                PlaybackId = playbackId.IsValid
                    ? playbackId
                    : throw new ArgumentException("Motion Matching frame Playback identity is invalid.", nameof(playbackId));
            }

            internal CharacterMotionMatchingProducerRuntime Runtime { get; }
            internal AnimationPlaybackId PlaybackId { get; }
        }

        abstract class MotionMatchingTrajectoryAdapter : IDisposable
        {
            protected MotionMatchingTrajectoryAdapter(
                ActorId actorId,
                MotionMatchingTrajectorySourceIdentity identity)
            {
                ActorId = actorId.IsValid
                    ? actorId
                    : throw new ArgumentException("Trajectory Adapter Actor identity is invalid.", nameof(actorId));
                Identity = identity.IsValid
                    ? identity
                    : throw new ArgumentException("Trajectory Adapter identity is invalid.", nameof(identity));
            }

            protected ActorId ActorId { get; }
            protected MotionMatchingTrajectorySourceIdentity Identity { get; }
            internal abstract bool AcceptsIntent { get; }
            internal abstract void CaptureIntent(CharacterPresentationTrajectoryIntent intent);
            internal abstract bool TryResolve(
                in CharacterBodyPresentationFrame bodyFrame,
                out MotionMatchingTrajectorySourceFrame frame);
            internal abstract void Reset(ulong resetSequence);
            public abstract void Dispose();
        }

        sealed class AcceptedIntentTrajectoryAdapter : MotionMatchingTrajectoryAdapter
        {
            CharacterPresentationTrajectoryIntent m_Intent;
            bool m_HasIntent;
            bool m_Disposed;

            internal AcceptedIntentTrajectoryAdapter(
                ActorId actorId,
                MotionMatchingTrajectorySourceIdentity identity)
                : base(actorId, identity)
            {
            }

            internal override bool AcceptsIntent => true;

            internal override void CaptureIntent(CharacterPresentationTrajectoryIntent intent)
            {
                RequireAlive();
                if (intent.ActorId != ActorId)
                    throw new InvalidOperationException("Presentation Trajectory Intent targets another Actor.");
                if (m_HasIntent && intent.SourceSequence <= m_Intent.SourceSequence)
                    throw new InvalidOperationException("Presentation Trajectory Intent sequence did not advance.");
                m_Intent = intent;
                m_HasIntent = true;
            }

            internal override bool TryResolve(
                in CharacterBodyPresentationFrame bodyFrame,
                out MotionMatchingTrajectorySourceFrame frame)
            {
                RequireAlive();
                if (!m_HasIntent)
                {
                    frame = default;
                    return false;
                }
                if (m_Intent.ResetSequence != bodyFrame.ResetSequence ||
                    m_Intent.CurrentTick.Value > bodyFrame.CurrentTick)
                    throw new InvalidOperationException("Accepted Intent trajectory input does not match the current Body presentation branch.");
                frame = new MotionMatchingTrajectorySourceFrame(
                    Identity,
                    MotionMatchingTrajectorySourceKind.AcceptedIntent,
                    ActorId,
                    m_Intent.CurrentTick,
                    m_Intent.SourceSequence,
                    bodyFrame.VisiblePosition,
                    bodyFrame.VisibleRotation,
                    new Vector2(bodyFrame.VisibleVelocity.x, bodyFrame.VisibleVelocity.z),
                    bodyFrame.VisibleYawVelocityDegreesPerSecond,
                    m_Intent.DesiredPlanarVelocity,
                    m_Intent.DesiredFacing,
                    m_Intent.AcceptedAcceleration,
                    m_Intent.AcceptedTurnRateDegrees,
                    m_Intent.Grounded,
                    m_Intent.MovementModeId,
                    0f,
                    m_Intent.ResetSequence);
                return true;
            }

            internal override void Reset(ulong resetSequence)
            {
                RequireAlive();
                m_Intent = default;
                m_HasIntent = false;
            }

            public override void Dispose()
            {
                m_Intent = default;
                m_HasIntent = false;
                m_Disposed = true;
            }

            void RequireAlive()
            {
                if (m_Disposed)
                    throw new ObjectDisposedException(nameof(AcceptedIntentTrajectoryAdapter));
            }
        }

        sealed class SelectedBodyTrajectoryAdapter : MotionMatchingTrajectoryAdapter
        {
            ulong m_SourceSequence;
            bool m_Disposed;

            internal SelectedBodyTrajectoryAdapter(
                ActorId actorId,
                MotionMatchingTrajectorySourceIdentity identity)
                : base(actorId, identity)
            {
            }

            internal override bool AcceptsIntent => false;

            internal override void CaptureIntent(CharacterPresentationTrajectoryIntent intent)
            {
                RequireAlive();
                throw new InvalidOperationException("Selected Body Presentation does not accept an Accepted Intent trajectory input.");
            }

            internal override bool TryResolve(
                in CharacterBodyPresentationFrame bodyFrame,
                out MotionMatchingTrajectorySourceFrame frame)
            {
                RequireAlive();
                if (m_SourceSequence == ulong.MaxValue)
                    throw new InvalidOperationException("Selected Body trajectory sequence was exhausted.");
                Vector3 forward = bodyFrame.TargetRotation * Vector3.forward;
                Vector2 planarVelocity = new Vector2(bodyFrame.TargetVelocity.x, bodyFrame.TargetVelocity.z);
                frame = new MotionMatchingTrajectorySourceFrame(
                    Identity,
                    MotionMatchingTrajectorySourceKind.SelectedBody,
                    ActorId,
                    new SimulationTick(bodyFrame.CurrentTick),
                    ++m_SourceSequence,
                    bodyFrame.TargetPosition,
                    bodyFrame.TargetRotation,
                    planarVelocity,
                    bodyFrame.TargetYawVelocityDegreesPerSecond,
                    planarVelocity,
                    new Vector2(forward.x, forward.z),
                    0f,
                    0f,
                    bodyFrame.TargetGrounded,
                    string.Empty,
                    bodyFrame.SampleAgeSeconds,
                    bodyFrame.ResetSequence);
                return true;
            }

            internal override void Reset(ulong resetSequence)
            {
                RequireAlive();
                m_SourceSequence = 0;
            }

            public override void Dispose()
            {
                m_SourceSequence = 0;
                m_Disposed = true;
            }

            void RequireAlive()
            {
                if (m_Disposed)
                    throw new ObjectDisposedException(nameof(SelectedBodyTrajectoryAdapter));
            }
        }
    }
}
