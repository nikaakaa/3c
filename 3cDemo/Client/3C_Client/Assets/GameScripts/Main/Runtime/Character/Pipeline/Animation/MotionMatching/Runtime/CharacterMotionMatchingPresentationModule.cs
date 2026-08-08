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
        MissingTrajectory = 5,
        PreviewDemandMismatch = 6,
        SelectionCapacityExceeded = 7,
        RetainedOutputMissing = 8,
        MissingResolve = 9,
        CompletionIdentityMismatch = 10,
        PresentationFrameMismatch = 11,
        ResetIdentityMismatch = 12,
        PosePlanCompletionMismatch = 13,
        HistoryBindingMismatch = 14
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

    internal readonly struct MotionMatchingFrameMutationLease
    {
        internal MotionMatchingFrameMutationLease(ulong frameIdentity)
        {
            FrameIdentity = frameIdentity;
        }

        internal ulong FrameIdentity { get; }
        internal bool IsValid => FrameIdentity != 0;
    }

    internal sealed class CharacterMotionMatchingPresentationModule : IDisposable
    {
        readonly struct FramePage
        {
            internal FramePage(
                ulong resetSequence,
                ulong previousResetSequence,
                ulong openCompletionIdentity,
                ulong openPresentationFrame,
                ulong openResetSequence,
                ulong lastResolvedPresentationFrame,
                ulong lastCompletedPresentationFrame,
                int lastHistoryAppendCount,
                int lastHistoryGapCount,
                MotionMatchingSearchReplayArtifact previewQuery,
                string previewProviderId,
                MotionMatchingPresentationResetReason lastResetReason,
                bool frameOpen)
            {
                ResetSequence = resetSequence;
                PreviousResetSequence = previousResetSequence;
                OpenCompletionIdentity = openCompletionIdentity;
                OpenPresentationFrame = openPresentationFrame;
                OpenResetSequence = openResetSequence;
                LastResolvedPresentationFrame =
                    lastResolvedPresentationFrame;
                LastCompletedPresentationFrame =
                    lastCompletedPresentationFrame;
                LastHistoryAppendCount = lastHistoryAppendCount;
                LastHistoryGapCount = lastHistoryGapCount;
                PreviewQuery = previewQuery;
                PreviewProviderId = previewProviderId;
                LastResetReason = lastResetReason;
                FrameOpen = frameOpen;
            }

            internal ulong ResetSequence { get; }
            internal ulong PreviousResetSequence { get; }
            internal ulong OpenCompletionIdentity { get; }
            internal ulong OpenPresentationFrame { get; }
            internal ulong OpenResetSequence { get; }
            internal ulong LastResolvedPresentationFrame { get; }
            internal ulong LastCompletedPresentationFrame { get; }
            internal int LastHistoryAppendCount { get; }
            internal int LastHistoryGapCount { get; }
            internal MotionMatchingSearchReplayArtifact PreviewQuery { get; }
            internal string PreviewProviderId { get; }
            internal MotionMatchingPresentationResetReason LastResetReason
            {
                get;
            }
            internal bool FrameOpen { get; }
        }

        struct FrozenOutputMutation
        {
            internal AnimationPoseSourceId SourceId;
            internal MotionMatchingFrozenSelection Frozen;
            internal bool Retained;
        }

        struct FrozenOutputSlot
        {
            internal bool InUse;
            internal bool Retained;
            internal AnimationPoseSourceId SourceId;
            internal MotionMatchingFrozenSelection Frozen;
        }

        struct PreparedHistoryMutation
        {
            internal CharacterMotionMatchingProviderRuntime Runtime;
            internal string ProviderId;
            internal PoseNodeId PlayerNodeId;
            internal AnimationPoseSourceId SourceId;
        }

        readonly ActorId m_ActorId;
        readonly CharacterBodyPresentationSourceMode m_BodySourceMode;
        readonly CharacterPresentationProjection m_Projection;
        readonly MotionMatchingTrajectoryAdapter m_TrajectoryAdapter;
        readonly Dictionary<string, CharacterMotionMatchingProviderRuntime> m_Providers;
        readonly CharacterMotionMatchingProviderRuntime[] m_ProviderRuntimes;
        readonly FrozenOutputSlot[] m_FrozenOutputs;
        readonly HashSet<string> m_ResolvedProviders;
        readonly MotionMatchingSelectionBatchItem[] m_Selections;
        readonly AnimationPoseRequestWorkspace m_SelectionWorkspace;
        readonly FrozenOutputMutation[] m_FrozenOutputMutations;
        readonly PreparedHistoryMutation[] m_PreparedHistoryMutations;

        int m_FrozenOutputCount;
        int m_SelectionCount;
        int m_FrozenOutputMutationCount;
        int m_PreparedHistoryMutationCount;
        ulong m_ResetSequence;
        ulong m_PreviousResetSequence;
        ulong m_CompletionSequence;
        ulong m_SelectionWorkspaceCompletionIdentity;
        ulong m_OpenCompletionIdentity;
        ulong m_OpenPresentationFrame;
        ulong m_OpenResetSequence;
        ulong m_LastResolvedPresentationFrame;
        ulong m_LastCompletedPresentationFrame;
        int m_LastHistoryAppendCount;
        int m_LastHistoryGapCount;
        MotionMatchingSearchReplayArtifact m_PreviewQuery;
        string m_PreviewProviderId;
        MotionMatchingPresentationResetReason m_LastResetReason;
        MotionMatchingPreparedFrameCompletion m_PreparedCompletion;
        FramePage m_CommittedPage;
        MotionMatchingFrameMutationLease m_ActiveMutationLease;
        bool m_PreparedCompletionApplied;
        bool m_FrameOpen;
        bool m_Disposed;

        internal CharacterMotionMatchingPresentationModule(
            ActorId actorId,
            CharacterBodyPresentationSourceMode bodySourceMode,
            CharacterPresentationProjection projection)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Motion Matching Presentation Module Actor identity is invalid.", nameof(actorId));
            if (bodySourceMode != CharacterBodyPresentationSourceMode.CommittedStream &&
                bodySourceMode != CharacterBodyPresentationSourceMode.SelectedStream)
                throw new ArgumentOutOfRangeException(nameof(bodySourceMode));
            if (projection == null || projection.MotionMatching == null)
                throw new ArgumentException("Motion Matching Presentation Module requires a compiled Projection payload.", nameof(projection));
            AnimationPoseRequestWorkspaceLayout workspaceLayout =
                AnimationPoseRequestWorkspaceLayoutFactory.Create(
                    projection);

            m_ActorId = actorId;
            m_BodySourceMode = bodySourceMode;
            m_Projection = projection;
            int sourceCapacity = workspaceLayout.SourceCapacity;
            m_Providers = new Dictionary<string, CharacterMotionMatchingProviderRuntime>(
                projection.MotionMatching.NodeBindingCount,
                StringComparer.Ordinal);
            m_ProviderRuntimes =
                new CharacterMotionMatchingProviderRuntime[
                    projection.MotionMatching.NodeBindingCount];
            m_FrozenOutputs = new FrozenOutputSlot[sourceCapacity];
            m_ResolvedProviders = new HashSet<string>(sourceCapacity, StringComparer.Ordinal);
            m_Selections = new MotionMatchingSelectionBatchItem[sourceCapacity];
            m_SelectionWorkspace = new AnimationPoseRequestWorkspace(workspaceLayout);
            m_FrozenOutputMutations = new FrozenOutputMutation[sourceCapacity];
            m_PreparedHistoryMutations =
                new PreparedHistoryMutation[
                    projection.MotionMatching.NodeBindingCount];
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
                BuildProviderRuntimes(projection.MotionMatching);
            }
            catch
            {
                DisposeProviderRuntimes();
                m_TrajectoryAdapter.Dispose();
                m_SelectionWorkspace.Dispose();
                throw;
            }
            m_CommittedPage = ReadPage();
        }

        internal bool Enabled => true;
        internal bool AcceptsTrajectoryIntent => m_TrajectoryAdapter.AcceptsIntent;
        internal int FrozenOutputCount => m_FrozenOutputCount;
        internal ulong LastResolvedCompletionIdentity => m_OpenCompletionIdentity;
        internal ulong LastCompletedPresentationFrame => m_LastCompletedPresentationFrame;
        internal int LastHistoryAppendCount => m_LastHistoryAppendCount;
        internal int LastHistoryGapCount => m_LastHistoryGapCount;

        internal MotionMatchingFrameMutationLease BeginPendingFrame(
            ulong frameIdentity)
        {
            RequireAlive();
            if (frameIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(frameIdentity));
            if (m_ActiveMutationLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Motion Matching frame mutation is already open.");
            }
            LoadPage(in m_CommittedPage);
            ClearFrameScratch();
            m_TrajectoryAdapter.BeginFrame();
            int begunProviders = 0;
            try
            {
                for (int providerIndex = 0;
                     providerIndex < m_ProviderRuntimes.Length;
                     providerIndex++)
                {
                    m_ProviderRuntimes[providerIndex].BeginFrame();
                    begunProviders++;
                }
                m_ActiveMutationLease =
                    new MotionMatchingFrameMutationLease(
                        frameIdentity);
                return m_ActiveMutationLease;
            }
            catch
            {
                for (int providerIndex = begunProviders - 1;
                     providerIndex >= 0;
                     providerIndex--)
                    m_ProviderRuntimes[providerIndex].DiscardFrame();
                m_TrajectoryAdapter.DiscardFrame();
                ClearFrameScratch();
                throw;
            }
        }

        internal void SealFrame(
            MotionMatchingFrameMutationLease lease)
        {
            RequireMutation(lease);
            if (m_PreparedCompletion.IsValid &&
                !m_PreparedCompletionApplied)
            {
                throw new InvalidOperationException(
                    "Motion Matching prepared completion was not applied before Seal.");
            }
            for (int providerIndex = 0;
                 providerIndex < m_ProviderRuntimes.Length;
                 providerIndex++)
                m_ProviderRuntimes[providerIndex].CommitFrame();
            m_TrajectoryAdapter.CommitFrame();
            ApplyFrozenOutputMutations();
            m_CommittedPage = ReadPage();
            ClearFrameScratch();
            m_ActiveMutationLease = default;
        }

        internal void DiscardFrame(
            MotionMatchingFrameMutationLease lease)
        {
            RequireMutation(lease);
            for (int providerIndex = m_ProviderRuntimes.Length - 1;
                 providerIndex >= 0;
                 providerIndex--)
                m_ProviderRuntimes[providerIndex].DiscardFrame();
            m_TrajectoryAdapter.DiscardFrame();
            LoadPage(in m_CommittedPage);
            m_SelectionWorkspace.Reset();
            ClearFrameScratch();
            m_ActiveMutationLease = default;
        }

        internal bool HasFrameWork(in MotionMatchingPoseStateDemandBatch demands)
        {
            RequireAlive();
            return demands.Count > 0 || m_FrozenOutputCount > 0 || m_PreviewQuery != null;
        }

        internal void CaptureTrajectoryIntent(CharacterPresentationTrajectoryIntent intent)
        {
            RequireAlive();
            m_TrajectoryAdapter.CaptureIntent(intent);
        }

        internal void CapturePreviewQuery(
            string providerId,
            MotionMatchingSearchReplayArtifact query)
        {
            RequireAlive();
            if (m_ActiveMutationLease.IsValid)
                throw new InvalidOperationException("Motion Matching preview query cannot change while a frame mutation is open.");
            if (query == null || string.IsNullOrWhiteSpace(providerId) ||
                !m_Providers.ContainsKey(providerId) ||
                !string.Equals(
                    query.ProjectionIdentity,
                    $"{m_Projection.ProgramId}@{m_Projection.SourceRevision}:{m_Projection.ContractHash}",
                    StringComparison.Ordinal))
                throw new InvalidOperationException("Motion Matching preview query identity does not match this Module.");
            if (m_PreviewQuery != null)
                throw new InvalidOperationException("Motion Matching Module already has a pending preview query.");
            if (query.ResetSequence != m_ResetSequence)
                Reset(
                    query.ResetSequence,
                    MotionMatchingPresentationResetReason
                        .PresentationReset);
            m_PreviewProviderId = providerId;
            m_PreviewQuery = query;
            m_CommittedPage = ReadPage();
        }

        internal MotionMatchingFrameResolution ResolveFrame(
            ulong presentationFrame,
            float presentationDeltaSeconds,
            in CharacterBodyPresentationFrame bodyFrame,
            in MotionMatchingPoseStateDemandBatch demands,
            RuntimeDiagnosticsContext diagnostics)
        {
            RequireAlive();
            RequireOpenMutation();
            MotionMatchingSearchReplayArtifact previewQuery = m_PreviewQuery;
            string previewProviderId = m_PreviewProviderId;
            bool previewFrame = previewQuery != null;
            if (presentationFrame == m_LastResolvedPresentationFrame)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.DuplicateResolve,
                    "Motion Matching cannot Resolve twice in one Presentation frame.");
            if (presentationFrame == 0 ||
                !previewFrame && (!bodyFrame.IsValid || bodyFrame.SourceMode != m_BodySourceMode) ||
                !float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f ||
                demands.PresentationFrame != presentationFrame)
                throw new ArgumentException("Motion Matching Resolve frame input is invalid.");
            if (m_FrameOpen)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.PreviousFrameIncomplete,
                    "Motion Matching cannot Resolve a new frame before the previous frame completes.");
            if (!previewFrame && bodyFrame.ResetSequence != m_ResetSequence)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.ResetIdentityMismatch,
                    "Motion Matching Body Reset identity was not applied before Resolve.");

            ulong frameResetSequence = previewFrame ? previewQuery.ResetSequence : bodyFrame.ResetSequence;
            if (!previewFrame && demands.ResetSequence != frameResetSequence)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.ResetIdentityMismatch,
                    "Motion Matching Pose State demand Reset identity does not match the Body frame.");
            if (previewFrame && frameResetSequence != m_ResetSequence)
                Reset(
                    frameResetSequence,
                    MotionMatchingPresentationResetReason
                        .PresentationReset);

            m_ResolvedProviders.Clear();
            Array.Clear(m_Selections, 0, m_SelectionCount);
            m_SelectionCount = 0;
            m_SelectionWorkspace.BeginFrame(NextSelectionWorkspaceCompletionIdentity());
            MotionMatchingTrajectorySourceFrame trajectoryFrame = default;
            bool hasTrajectory = previewFrame || demands.Count == 0;
            if (!hasTrajectory)
                hasTrajectory = m_TrajectoryAdapter.TryResolve(in bodyFrame, out trajectoryFrame);
            if (!hasTrajectory)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.MissingTrajectory,
                    "Selected Motion Matching playback requires a formal trajectory input for the current Body branch.");

            for (int demandIndex = 0; demandIndex < demands.Count; demandIndex++)
            {
                MotionMatchingPoseStateDemand demand = demands.GetDemand(demandIndex);
                if (demand.ResetSequence != frameResetSequence)
                    throw FrameFailure(
                        MotionMatchingFrameTransactionInvalidReason.ResetIdentityMismatch,
                        $"Motion Matching provider '{demand.ProviderId}' demand has a stale Reset identity.");
                if (!m_ResolvedProviders.Add(demand.ProviderId))
                    throw FrameFailure(
                        MotionMatchingFrameTransactionInvalidReason.DuplicateDemand,
                        $"Motion Matching provider '{demand.ProviderId}' was demanded more than once.");
                CharacterMotionMatchingProviderRuntime runtime = RequireProviderRuntime(demand.ProviderId);
                if (previewFrame && (demands.Count != 1 ||
                                     !string.Equals(demand.ProviderId, previewProviderId, StringComparison.Ordinal)))
                    throw FrameFailure(
                        MotionMatchingFrameTransactionInvalidReason.PreviewDemandMismatch,
                        "Motion Matching preview query must target one exact provider demand.");
                MotionMatchingPoseSourceOutput output = previewFrame
                    ? runtime.ResolvePreviewQuery(
                        presentationFrame,
                        presentationDeltaSeconds,
                        previewQuery,
                        diagnostics)
                    : runtime.Resolve(
                        presentationFrame,
                        presentationDeltaSeconds,
                        trajectoryFrame,
                        diagnostics);
                var sourceId = new AnimationPoseSourceId(
                    output.SourceIndex,
                    AnimationPoseSourceKind.MotionMatching,
                    new AnimationPoseSelectionGeneration(output.SelectionGeneration.Value));
                var frozen = new MotionMatchingFrozenSelection(
                    demand.ProviderId,
                    demand.StateMachineIndex,
                    demand.StateIndex,
                    demand.PlayerIndex,
                    demand.PlayerNodeId,
                    new PoseSourceProviderDemandGeneration(
                        demand.RelevanceGeneration),
                    in output);
                StageFrozenOutput(sourceId, in frozen);
                AddSelection(
                    in frozen,
                    presentationFrame,
                    true,
                    runtime);
            }
            AddRetainedSelections(presentationFrame);
            for (int providerIndex = 0;
                 providerIndex < m_ProviderRuntimes.Length;
                 providerIndex++)
            {
                CharacterMotionMatchingProviderRuntime runtime =
                    m_ProviderRuntimes[providerIndex];
                if (!m_ResolvedProviders.Contains(runtime.ProviderId))
                    runtime.ReleaseDomain();
            }

            ulong completionIdentity = NextCompletionIdentity();
            m_OpenCompletionIdentity = completionIdentity;
            m_OpenPresentationFrame = presentationFrame;
            m_OpenResetSequence = frameResetSequence;
            m_LastResolvedPresentationFrame = presentationFrame;
            m_FrameOpen = true;
            m_PreviewQuery = null;
            m_PreviewProviderId = null;
            PublishFrameDiagnostics(diagnostics, "Resolved", completionIdentity, m_SelectionCount, 0, 0);
            return new MotionMatchingFrameResolution(
                presentationFrame,
                frameResetSequence,
                completionIdentity,
                m_Selections,
                m_SelectionCount,
                m_ResolvedProviders.Count,
                m_ResolvedProviders.Count > 0);
        }

        internal MotionMatchingPreparedFrameCompletion
            PrepareFrameCompletion(
                in MotionMatchingFrameResolution resolution,
                ulong posePlanCompletionIdentity)
        {
            RequireAlive();
            RequireOpenMutation();
            ValidateOpenResolution(in resolution);
            if (posePlanCompletionIdentity == 0)
                throw new ArgumentOutOfRangeException(
                    nameof(posePlanCompletionIdentity));
            if (m_PreparedCompletion.IsValid)
            {
                throw new InvalidOperationException(
                    "Motion Matching frame completion is already prepared.");
            }

            ValidateFrozenCandidates(in resolution);
            m_PreparedHistoryMutationCount = 0;
            for (int selectionIndex = 0;
                 selectionIndex < resolution.SelectionCount;
                 selectionIndex++)
            {
                MotionMatchingSelectionBatchItem selection =
                    resolution.GetSelection(selectionIndex);
                if (!selection.RequiresHistory)
                    continue;
                if (m_PreparedHistoryMutationCount >=
                    m_PreparedHistoryMutations.Length)
                {
                    throw FrameFailure(
                        MotionMatchingFrameTransactionInvalidReason
                            .SelectionCapacityExceeded,
                        "Motion Matching history mutation capacity was exceeded.");
                }
                CharacterMotionMatchingProviderRuntime runtime =
                    RequireProviderRuntime(selection.ProviderId);
                if (runtime.PoseNodeId != selection.PlayerNodeId ||
                    runtime.PresentationPoseSourceIndex !=
                    selection.SourceSample.SourceIndex)
                {
                    throw FrameFailure(
                        MotionMatchingFrameTransactionInvalidReason
                            .HistoryBindingMismatch,
                        $"Motion Matching provider '{selection.ProviderId}' history binding does not match its compiled Player and source index.");
                }
                runtime.PrepareBasePoseCompletion(
                    resolution.PresentationFrame,
                    selection.SourceSample.SourceIndex,
                    new MotionMatchingSelectionGeneration(
                        selection.SourceSample.SourceGeneration.Value));
                m_PreparedHistoryMutations[
                    m_PreparedHistoryMutationCount++] =
                    new PreparedHistoryMutation
                    {
                        Runtime = runtime,
                        ProviderId = selection.ProviderId,
                        PlayerNodeId = selection.PlayerNodeId,
                        SourceId = selection.SourceIdentity
                    };
            }

            m_PreparedCompletion =
                new MotionMatchingPreparedFrameCompletion(
                    resolution.PresentationFrame,
                    resolution.ResetSequence,
                    resolution.CompletionIdentity,
                    posePlanCompletionIdentity,
                    m_PreparedHistoryMutationCount);
            m_PreparedCompletionApplied = false;
            return m_PreparedCompletion;
        }

        internal void CompleteFrame(
            in MotionMatchingPosePlanCompletion posePlanCompletion)
        {
            RequireAlive();
            RequireOpenMutation();
            if (!m_PreparedCompletion.IsValid ||
                m_PreparedCompletionApplied)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.MissingResolve,
                    "Motion Matching Complete has no prepared frame completion.");
            if (posePlanCompletion.SelectionCompletionIdentity !=
                    m_PreparedCompletion.SelectionCompletionIdentity ||
                posePlanCompletion.PresentationFrame !=
                    m_PreparedCompletion.PresentationFrame ||
                posePlanCompletion.ResetSequence !=
                    m_PreparedCompletion.ResetSequence ||
                posePlanCompletion.PosePlanCompletionIdentity !=
                    m_PreparedCompletion.PosePlanCompletionIdentity ||
                posePlanCompletion.HistoryCount !=
                    m_PreparedCompletion.HistoryCount)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.PosePlanCompletionMismatch,
                    "Motion Matching Pose Plan completion token does not match the prepared frame completion.");

            int appended = 0;
            int gaps = 0;
            for (int historyIndex = 0;
                 historyIndex < m_PreparedHistoryMutationCount;
                 historyIndex++)
            {
                PreparedHistoryMutation mutation =
                    m_PreparedHistoryMutations[historyIndex];
                MotionMatchingPosePlanHistoryCompletion history =
                    posePlanCompletion.GetHistory(historyIndex);
                AnimationFootPlacementSample footPlacement =
                    history.FootPlacement;
                MotionMatchingHistoryCompletionOutcome outcome =
                    mutation.Runtime.CompletePreparedBasePose(
                        history.PoseAvailable,
                        in footPlacement);
                if (outcome ==
                    MotionMatchingHistoryCompletionOutcome.Appended)
                {
                    appended++;
                }
                else if (outcome ==
                         MotionMatchingHistoryCompletionOutcome.Gap)
                {
                    gaps++;
                }
            }

            MarkRetainedFrozenOutputs(in posePlanCompletion);
            m_LastCompletedPresentationFrame =
                m_PreparedCompletion.PresentationFrame;
            m_LastHistoryAppendCount = appended;
            m_LastHistoryGapCount = gaps;
            m_FrameOpen = false;
            m_OpenCompletionIdentity = 0;
            m_OpenPresentationFrame = 0;
            m_OpenResetSequence = 0;
            m_PreparedCompletionApplied = true;
        }

        internal void PublishCommittedFrameDiagnostics(
            RuntimeDiagnosticsContext diagnostics,
            in MotionMatchingFrameResolution resolution)
        {
            RequireAlive();
            if (resolution.CompletionIdentity == 0 ||
                resolution.PresentationFrame !=
                    m_LastCompletedPresentationFrame ||
                m_FrameOpen ||
                m_ActiveMutationLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Motion Matching committed diagnostics have no completed frame.");
            }
            PublishFrameDiagnostics(
                diagnostics,
                "Committed",
                resolution.CompletionIdentity,
                resolution.SelectionCount,
                m_LastHistoryAppendCount,
                m_LastHistoryGapCount);
        }

        internal bool TryCaptureSearchReplay(
            string providerId,
            out MotionMatchingSearchReplayArtifact artifact)
        {
            RequireAlive();
            artifact = null;
            return !string.IsNullOrWhiteSpace(providerId) &&
                   m_Providers.TryGetValue(providerId, out CharacterMotionMatchingProviderRuntime runtime) &&
                   runtime.TryCaptureSearchReplay(out artifact);
        }

        internal void Reset(
            ulong resetSequence,
            MotionMatchingPresentationResetReason reason)
        {
            RequireAlive();
            if (m_ActiveMutationLease.IsValid)
                throw new InvalidOperationException("Motion Matching Module cannot reset while a frame mutation is open.");
            if (!Enum.IsDefined(typeof(MotionMatchingPresentationResetReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            if (m_BodySourceMode == CharacterBodyPresentationSourceMode.CommittedStream &&
                reason == MotionMatchingPresentationResetReason.BodyStreamReset)
            {
                RetargetBodyBranch(resetSequence);
                return;
            }
            m_TrajectoryAdapter.Reset(resetSequence);
            for (int providerIndex = 0;
                 providerIndex < m_ProviderRuntimes.Length;
                 providerIndex++)
            {
                m_ProviderRuntimes[providerIndex].Reset(resetSequence);
            }
            Array.Clear(m_FrozenOutputs, 0, m_FrozenOutputs.Length);
            m_FrozenOutputCount = 0;
            ClearFrameScratch();
            m_SelectionWorkspace.Reset();
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
            m_PreviewQuery = null;
            m_PreviewProviderId = null;
            m_FrameOpen = false;
            m_CommittedPage = ReadPage();
        }

        internal void RetargetBodyBranch(ulong resetSequence)
        {
            RequireAlive();
            if (m_ActiveMutationLease.IsValid)
                throw new InvalidOperationException("Motion Matching Module cannot retarget while a frame mutation is open.");
            if (resetSequence == 0)
                throw new ArgumentOutOfRangeException(nameof(resetSequence));
            if (resetSequence == m_ResetSequence)
                return;
            m_TrajectoryAdapter.RetargetBodyBranch(resetSequence);
            for (int providerIndex = 0;
                 providerIndex < m_ProviderRuntimes.Length;
                 providerIndex++)
            {
                m_ProviderRuntimes[providerIndex].RetargetBodyBranch(resetSequence);
            }
            m_PreviousResetSequence = m_ResetSequence;
            m_ResetSequence = resetSequence;
            Array.Clear(m_FrozenOutputs, 0, m_FrozenOutputs.Length);
            m_FrozenOutputCount = 0;
            ClearFrameScratch();
            m_SelectionWorkspace.Reset();
            m_OpenCompletionIdentity = 0;
            m_OpenPresentationFrame = 0;
            m_OpenResetSequence = 0;
            m_LastResetReason = MotionMatchingPresentationResetReason.BodyStreamReset;
            m_PreviewQuery = null;
            m_PreviewProviderId = null;
            m_CommittedPage = ReadPage();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_FrameOpen = false;
            m_TrajectoryAdapter.Dispose();
            DisposeProviderRuntimes();
            m_SelectionWorkspace.Dispose();
            Array.Clear(m_FrozenOutputs, 0, m_FrozenOutputs.Length);
            m_FrozenOutputCount = 0;
            ClearFrameScratch();
            m_PreviewQuery = null;
            m_PreviewProviderId = null;
        }

        void BuildProviderRuntimes(MotionMatchingProjectionPayload payload)
        {
            for (int bindingIndex = 0; bindingIndex < payload.NodeBindingCount; bindingIndex++)
            {
                MotionMatchingNodeBindingPayload binding = payload.GetNodeBinding(bindingIndex);
                if (!TryResolveProviderUsage(
                        binding,
                        out string providerId,
                        out PresentationPoseSourceIndex sourceIndex))
                    throw new InvalidOperationException($"Motion Matching node binding '{binding.PoseNodeId}' does not resolve to one Projection source provider.");
                var runtime =
                    new CharacterMotionMatchingProviderRuntime(
                        $"{m_Projection.ProgramId}@{m_Projection.SourceRevision}:{m_Projection.ContractHash}",
                        payload,
                        binding,
                        providerId,
                        sourceIndex,
                        m_Projection.Rig);
                try
                {
                    m_Providers.Add(providerId, runtime);
                    m_ProviderRuntimes[bindingIndex] = runtime;
                }
                catch
                {
                    runtime.Dispose();
                    throw;
                }
            }
            if (m_Providers.Count == 0)
                throw new InvalidOperationException("Motion Matching Projection payload has no provider runtime binding.");
        }

        bool TryResolveProviderUsage(
            MotionMatchingNodeBindingPayload binding,
            out string providerId,
            out PresentationPoseSourceIndex sourceIndex)
        {
            providerId = null;
            sourceIndex = default;
            int count = 0;
            for (int machineIndex = 0;
                 machineIndex <
                 m_Projection.PosePlan.StateMachines.Count;
                 machineIndex++)
            {
                CharacterPoseStateMachineDescriptor machine =
                    m_Projection.PosePlan.StateMachines[
                        machineIndex];
                for (int stateIndex = 0;
                     stateIndex < machine.States.Count;
                     stateIndex++)
                {
                    IReadOnlyList<PoseStateSourceProviderPlan>
                        providers =
                            machine.States[stateIndex]
                                .SourceProviders;
                    for (int providerIndex = 0;
                         providerIndex < providers.Count;
                         providerIndex++)
                    {
                        PoseStateSourceProviderPlan provider =
                            providers[providerIndex];
                        if (provider.SourceKind ==
                                AnimationPoseSourceKind
                                    .MotionMatching &&
                            provider.PlayerNodeId ==
                                binding.PoseNodeId)
                        {
                            providerId = provider.ProviderId.Value;
                            sourceIndex = provider.PresentationPoseSourceIndex;
                            count++;
                        }
                    }
                }
            }
            return count == 1 &&
                   !string.IsNullOrWhiteSpace(providerId) &&
                   sourceIndex.IsValid;
        }

        void AddRetainedSelections(ulong presentationFrame)
        {
            for (int slotIndex = 0;
                 slotIndex < m_FrozenOutputs.Length;
                 slotIndex++)
            {
                FrozenOutputSlot slot = m_FrozenOutputs[slotIndex];
                if (!slot.InUse ||
                    ContainsSelection(
                        slot.Frozen.PlayerNodeId,
                        slot.SourceId))
                    continue;
                MotionMatchingFrozenSelection frozen = slot.Frozen;
                AddSelection(
                    in frozen,
                    presentationFrame,
                    false,
                    null);
            }
        }

        void AddSelection(
            in MotionMatchingFrozenSelection frozen,
            ulong presentationFrame,
            bool requiresHistory,
            CharacterMotionMatchingProviderRuntime runtime)
        {
            MotionMatchingPoseSourceOutput output = frozen.Output;
            var sourceId = new AnimationPoseSourceId(
                output.SourceIndex,
                AnimationPoseSourceKind.MotionMatching,
                new AnimationPoseSelectionGeneration(output.SelectionGeneration.Value));
            if (ContainsSelection(frozen.PlayerNodeId, sourceId))
                return;
            if (m_SelectionCount >= m_Selections.Length)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.SelectionCapacityExceeded,
                    "Motion Matching Selection batch capacity was exceeded.");
            PresentationPoseSourceSample sourceSample =
                MotionMatchingSelectionFactory.Create(
                in output,
                m_Projection,
                m_SelectionWorkspace);
            if (sourceSample.FrameSequence != presentationFrame)
            {
                sourceSample =
                    PresentationPoseSourceSample.Ready(
                        sourceSample.ProviderId,
                        sourceSample.PlayerNodeId,
                        sourceSample.SourceIndex,
                        sourceSample.SourceKind,
                        sourceSample.ProjectionDatabaseIndex,
                        sourceSample.SourceGeneration,
                        sourceSample
                            .SourcePoseContinuityIdentity,
                        presentationFrame,
                        sourceSample.RawSample,
                        sourceSample.EffectiveSample,
                        sourceSample.Clips,
                        sourceSample.ParameterPageId,
                        sourceSample.PoseParameters,
                        sourceSample
                            .PoseParameterAvailability,
                        sourceSample.LeftFootFeatures,
                        sourceSample.RightFootFeatures,
                        sourceSample.HasFootFeatures);
            }
            m_Selections[m_SelectionCount++] = new MotionMatchingSelectionBatchItem(
                frozen.ProviderId,
                frozen.StateMachineIndex,
                frozen.StateIndex,
                frozen.PlayerIndex,
                frozen.PlayerNodeId,
                frozen.DemandGeneration,
                in sourceSample,
                requiresHistory,
                requiresHistory,
                requiresHistory ? runtime.FeatureRigBoneIndices : null,
                requiresHistory ? runtime.FeatureBonePositionWorkspace : null);
        }

        bool ContainsSelection(PoseNodeId playerNodeId, AnimationPoseSourceId sourceId)
        {
            for (int i = 0; i < m_SelectionCount; i++)
            {
                MotionMatchingSelectionBatchItem selection = m_Selections[i];
                if (selection.PlayerNodeId == playerNodeId &&
                    selection.SourceIdentity.Equals(sourceId))
                    return true;
            }
            return false;
        }

        void ValidateOpenResolution(
            in MotionMatchingFrameResolution resolution)
        {
            if (!m_FrameOpen || resolution.CompletionIdentity == 0)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.MissingResolve,
                    "Motion Matching completion preparation has no open Resolve frame transaction.");
            if (resolution.CompletionIdentity != m_OpenCompletionIdentity)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.CompletionIdentityMismatch,
                    "Motion Matching completion preparation identity does not match Resolve.");
            if (resolution.PresentationFrame != m_OpenPresentationFrame)
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.PresentationFrameMismatch,
                    "Motion Matching completion preparation frame does not match Resolve.");
            if (resolution.ResetSequence != m_OpenResetSequence ||
                resolution.ResetSequence != m_ResetSequence)
            {
                throw FrameFailure(
                    MotionMatchingFrameTransactionInvalidReason.ResetIdentityMismatch,
                    "Motion Matching completion preparation Reset identity does not match Resolve.");
            }
        }

        void ValidateFrozenCandidates(
            in MotionMatchingFrameResolution resolution)
        {
            for (int selectionIndex = 0;
                 selectionIndex < resolution.SelectionCount;
                 selectionIndex++)
            {
                MotionMatchingSelectionBatchItem selection =
                    resolution.GetSelection(selectionIndex);
                if (!TryGetFrozenOutput(
                        selection.SourceIdentity,
                        out MotionMatchingFrozenSelection frozen) ||
                    frozen.PlayerNodeId != selection.PlayerNodeId ||
                    !string.Equals(
                        frozen.ProviderId,
                        selection.ProviderId,
                        StringComparison.Ordinal))
                {
                    throw FrameFailure(
                        MotionMatchingFrameTransactionInvalidReason.RetainedOutputMissing,
                        $"Motion Matching Player '{selection.PlayerNodeId}' has no exact frozen candidate '{selection.SourceIdentity}'.");
                }
            }
        }

        void MarkRetainedFrozenOutputs(
            in MotionMatchingPosePlanCompletion completion)
        {
            for (int usageIndex = 0;
                 usageIndex < completion.SourceUsageCount;
                 usageIndex++)
            {
                MotionMatchingPosePlanSourceUsage usage =
                    completion.GetSourceUsage(usageIndex);
                int mutationIndex =
                    FindFrozenOutputMutation(usage.SourceId);
                if (mutationIndex >= 0)
                {
                    FrozenOutputMutation mutation =
                        m_FrozenOutputMutations[mutationIndex];
                    mutation.Retained = true;
                    m_FrozenOutputMutations[mutationIndex] = mutation;
                    continue;
                }
                int slotIndex = FindFrozenOutputSlot(usage.SourceId);
                if (slotIndex < 0)
                {
                    throw FrameFailure(
                        MotionMatchingFrameTransactionInvalidReason
                            .RetainedOutputMissing,
                        "Motion Matching Pose Plan completion references a missing prepared frozen output.");
                }
                FrozenOutputSlot slot = m_FrozenOutputs[slotIndex];
                slot.Retained = true;
                m_FrozenOutputs[slotIndex] = slot;
            }
        }

        void StageFrozenOutput(
            AnimationPoseSourceId sourceId,
            in MotionMatchingFrozenSelection frozen)
        {
            int mutationIndex = FindFrozenOutputMutation(sourceId);
            if (mutationIndex < 0)
            {
                if (m_FrozenOutputMutationCount >= m_FrozenOutputMutations.Length)
                    throw FrameFailure(
                        MotionMatchingFrameTransactionInvalidReason.SelectionCapacityExceeded,
                        "Motion Matching frozen output journal capacity was exceeded.");
                mutationIndex = m_FrozenOutputMutationCount++;
            }
            m_FrozenOutputMutations[mutationIndex] = new FrozenOutputMutation
            {
                SourceId = sourceId,
                Frozen = frozen
            };
        }

        bool TryGetFrozenOutput(
            AnimationPoseSourceId sourceId,
            out MotionMatchingFrozenSelection frozen)
        {
            int mutationIndex = FindFrozenOutputMutation(sourceId);
            if (mutationIndex >= 0)
            {
                FrozenOutputMutation mutation =
                    m_FrozenOutputMutations[mutationIndex];
                frozen = mutation.Frozen;
                return true;
            }
            int slotIndex = FindFrozenOutputSlot(sourceId);
            if (slotIndex >= 0)
            {
                frozen = m_FrozenOutputs[slotIndex].Frozen;
                return true;
            }
            frozen = default;
            return false;
        }

        int FindFrozenOutputMutation(AnimationPoseSourceId sourceId)
        {
            for (int i = 0; i < m_FrozenOutputMutationCount; i++)
            {
                if (m_FrozenOutputMutations[i].SourceId.Equals(sourceId))
                    return i;
            }
            return -1;
        }

        int FindFrozenOutputSlot(AnimationPoseSourceId sourceId)
        {
            for (int i = 0; i < m_FrozenOutputs.Length; i++)
            {
                if (m_FrozenOutputs[i].InUse &&
                    m_FrozenOutputs[i].SourceId.Equals(sourceId))
                {
                    return i;
                }
            }
            return -1;
        }

        void ApplyFrozenOutputMutations()
        {
            for (int slotIndex = 0;
                 slotIndex < m_FrozenOutputs.Length;
                 slotIndex++)
            {
                FrozenOutputSlot slot = m_FrozenOutputs[slotIndex];
                if (!slot.InUse || slot.Retained)
                    continue;
                m_FrozenOutputs[slotIndex] = default;
                m_FrozenOutputCount--;
            }
            for (int i = 0; i < m_FrozenOutputMutationCount; i++)
            {
                FrozenOutputMutation mutation = m_FrozenOutputMutations[i];
                if (!mutation.Retained)
                    continue;
                int slotIndex =
                    FindFrozenOutputSlot(mutation.SourceId);
                if (slotIndex < 0)
                {
                    for (int candidateIndex = 0;
                         candidateIndex < m_FrozenOutputs.Length;
                         candidateIndex++)
                    {
                        if (!m_FrozenOutputs[candidateIndex].InUse)
                        {
                            slotIndex = candidateIndex;
                            break;
                        }
                    }
                    if (slotIndex < 0)
                    {
                        throw new InvalidOperationException(
                            "Motion Matching frozen output capacity invariant was broken after validation.");
                    }
                    m_FrozenOutputCount++;
                }
                m_FrozenOutputs[slotIndex] =
                    new FrozenOutputSlot
                    {
                        InUse = true,
                        Retained = true,
                        SourceId = mutation.SourceId,
                        Frozen = mutation.Frozen
                    };
            }
        }

        CharacterMotionMatchingProviderRuntime RequireProviderRuntime(string providerId) =>
            m_Providers.TryGetValue(providerId, out CharacterMotionMatchingProviderRuntime runtime)
                ? runtime
                : throw new InvalidOperationException($"Motion Matching provider '{providerId}' has no compiled Runtime workspace.");

        ulong NextCompletionIdentity()
        {
            if (m_CompletionSequence == ulong.MaxValue)
                throw new InvalidOperationException("Motion Matching frame completion identity was exhausted.");
            return ++m_CompletionSequence;
        }

        ulong NextSelectionWorkspaceCompletionIdentity()
        {
            if (m_SelectionWorkspaceCompletionIdentity == ulong.MaxValue)
                throw new InvalidOperationException("Motion Matching Selection workspace completion identity was exhausted.");
            return ++m_SelectionWorkspaceCompletionIdentity;
        }

        void PublishFrameDiagnostics(
            RuntimeDiagnosticsContext diagnostics,
            string status,
            ulong completionIdentity,
            int selectionCount,
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
                    Priority = selectionCount,
                    Detail = $"completion={completionIdentity};frame={m_LastResolvedPresentationFrame};previousReset={m_PreviousResetSequence};reset={m_ResetSequence};selections={selectionCount};historyAppended={historyAppended};historyGaps={historyGaps};retainedFrozen={m_FrozenOutputCount}"
                });
        }

        FramePage ReadPage() => new FramePage(
            m_ResetSequence,
            m_PreviousResetSequence,
            m_OpenCompletionIdentity,
            m_OpenPresentationFrame,
            m_OpenResetSequence,
            m_LastResolvedPresentationFrame,
            m_LastCompletedPresentationFrame,
            m_LastHistoryAppendCount,
            m_LastHistoryGapCount,
            m_PreviewQuery,
            m_PreviewProviderId,
            m_LastResetReason,
            m_FrameOpen);

        void LoadPage(in FramePage page)
        {
            m_ResetSequence = page.ResetSequence;
            m_PreviousResetSequence = page.PreviousResetSequence;
            m_OpenCompletionIdentity = page.OpenCompletionIdentity;
            m_OpenPresentationFrame = page.OpenPresentationFrame;
            m_OpenResetSequence = page.OpenResetSequence;
            m_LastResolvedPresentationFrame = page.LastResolvedPresentationFrame;
            m_LastCompletedPresentationFrame = page.LastCompletedPresentationFrame;
            m_LastHistoryAppendCount = page.LastHistoryAppendCount;
            m_LastHistoryGapCount = page.LastHistoryGapCount;
            m_PreviewQuery = page.PreviewQuery;
            m_PreviewProviderId = page.PreviewProviderId;
            m_LastResetReason = page.LastResetReason;
            m_FrameOpen = page.FrameOpen;
        }

        void ClearFrameScratch()
        {
            Array.Clear(m_Selections, 0, m_SelectionCount);
            m_SelectionCount = 0;
            m_ResolvedProviders.Clear();
            Array.Clear(
                m_FrozenOutputMutations,
                0,
                m_FrozenOutputMutationCount);
            m_FrozenOutputMutationCount = 0;
            Array.Clear(
                m_PreparedHistoryMutations,
                0,
                m_PreparedHistoryMutationCount);
            m_PreparedHistoryMutationCount = 0;
            m_PreparedCompletion = default;
            m_PreparedCompletionApplied = false;
            for (int slotIndex = 0;
                 slotIndex < m_FrozenOutputs.Length;
                 slotIndex++)
            {
                FrozenOutputSlot slot = m_FrozenOutputs[slotIndex];
                if (!slot.InUse || !slot.Retained)
                    continue;
                slot.Retained = false;
                m_FrozenOutputs[slotIndex] = slot;
            }
        }

        void RequireMutation(
            MotionMatchingFrameMutationLease lease)
        {
            RequireAlive();
            if (!lease.IsValid ||
                !m_ActiveMutationLease.IsValid ||
                lease.FrameIdentity !=
                    m_ActiveMutationLease.FrameIdentity)
            {
                throw new InvalidOperationException(
                    "Motion Matching frame mutation lease is stale.");
            }
        }

        void RequireOpenMutation()
        {
            if (!m_ActiveMutationLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Motion Matching frame work requires an open animation frame mutation.");
            }
        }

        void DisposeProviderRuntimes()
        {
            for (int i = m_ProviderRuntimes.Length - 1; i >= 0; i--)
            {
                m_ProviderRuntimes[i]?.Dispose();
                m_ProviderRuntimes[i] = null;
            }
            m_Providers.Clear();
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterMotionMatchingPresentationModule));
        }

        static MotionMatchingFrameTransactionException FrameFailure(
            MotionMatchingFrameTransactionInvalidReason reason,
            string message) => new MotionMatchingFrameTransactionException(reason, message);

        readonly struct MotionMatchingFrozenSelection
        {
            internal MotionMatchingFrozenSelection(
                string providerId,
                int stateMachineIndex,
                int stateIndex,
                int playerIndex,
                PoseNodeId playerNodeId,
                PoseSourceProviderDemandGeneration
                    demandGeneration,
                in MotionMatchingPoseSourceOutput output)
            {
                if (string.IsNullOrWhiteSpace(providerId) || stateMachineIndex < 0 || stateIndex < 0 ||
                    playerIndex < 0 || !playerNodeId.IsValid ||
                    !demandGeneration.IsValid ||
                    output.ProviderId !=
                        new PresentationPoseSourceProviderId(
                            providerId) ||
                    output.PlayerNodeId != playerNodeId)
                    throw new ArgumentException("Motion Matching frozen Selection binding is invalid.");
                ProviderId = providerId;
                StateMachineIndex = stateMachineIndex;
                StateIndex = stateIndex;
                PlayerIndex = playerIndex;
                PlayerNodeId = playerNodeId;
                DemandGeneration = demandGeneration;
                Output = output;
            }

            internal string ProviderId { get; }
            internal int StateMachineIndex { get; }
            internal int StateIndex { get; }
            internal int PlayerIndex { get; }
            internal PoseNodeId PlayerNodeId { get; }
            internal PoseSourceProviderDemandGeneration
                DemandGeneration { get; }
            internal MotionMatchingPoseSourceOutput Output { get; }
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
            internal abstract void BeginFrame();
            internal abstract void CommitFrame();
            internal abstract void DiscardFrame();
            internal abstract void Reset(ulong resetSequence);
            internal abstract void RetargetBodyBranch(ulong resetSequence);
            public abstract void Dispose();
        }

        sealed class AcceptedIntentTrajectoryAdapter : MotionMatchingTrajectoryAdapter
        {
            CharacterPresentationTrajectoryIntent m_Intent;
            bool m_HasIntent;
            bool m_FrameOpen;
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
                if (m_FrameOpen)
                    throw new InvalidOperationException("Accepted Intent cannot change while a Motion Matching frame is open.");
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

            internal override void RetargetBodyBranch(ulong resetSequence)
            {
                RequireAlive();
                if (!m_HasIntent)
                    return;
                m_Intent = new CharacterPresentationTrajectoryIntent(
                    m_Intent.ActorId,
                    m_Intent.PreviousTick,
                    m_Intent.CurrentTick,
                    m_Intent.SourceSequence,
                    m_Intent.DesiredPlanarVelocity,
                    m_Intent.DesiredFacing,
                    m_Intent.AcceptedAcceleration,
                    m_Intent.AcceptedTurnRateDegrees,
                    m_Intent.HasMotion,
                    m_Intent.Grounded,
                    m_Intent.MovementModeId,
                    resetSequence);
            }

            internal override void BeginFrame()
            {
                RequireAlive();
                if (m_FrameOpen)
                    throw new InvalidOperationException("Accepted Intent trajectory frame is already open.");
                m_FrameOpen = true;
            }

            internal override void CommitFrame()
            {
                RequireOpenFrame();
                m_FrameOpen = false;
            }

            internal override void DiscardFrame()
            {
                RequireOpenFrame();
                m_FrameOpen = false;
            }

            public override void Dispose()
            {
                m_Intent = default;
                m_HasIntent = false;
                m_FrameOpen = false;
                m_Disposed = true;
            }

            void RequireAlive()
            {
                if (m_Disposed)
                    throw new ObjectDisposedException(nameof(AcceptedIntentTrajectoryAdapter));
            }

            void RequireOpenFrame()
            {
                RequireAlive();
                if (!m_FrameOpen)
                    throw new InvalidOperationException("Accepted Intent trajectory has no open frame.");
            }
        }

        sealed class SelectedBodyTrajectoryAdapter : MotionMatchingTrajectoryAdapter
        {
            ulong m_CommittedSourceSequence;
            ulong m_PendingSourceSequence;
            bool m_FrameOpen;
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
                if (!m_FrameOpen)
                    throw new InvalidOperationException("Selected Body trajectory requires an open frame.");
                if (m_PendingSourceSequence == ulong.MaxValue)
                    throw new InvalidOperationException("Selected Body trajectory sequence was exhausted.");
                Vector3 forward = bodyFrame.TargetRotation * Vector3.forward;
                Vector2 planarVelocity = new Vector2(bodyFrame.TargetVelocity.x, bodyFrame.TargetVelocity.z);
                frame = new MotionMatchingTrajectorySourceFrame(
                    Identity,
                    MotionMatchingTrajectorySourceKind.SelectedBody,
                    ActorId,
                    new SimulationTick(bodyFrame.CurrentTick),
                    ++m_PendingSourceSequence,
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
            }

            internal override void RetargetBodyBranch(ulong resetSequence)
            {
                RequireAlive();
            }

            internal override void BeginFrame()
            {
                RequireAlive();
                if (m_FrameOpen)
                    throw new InvalidOperationException("Selected Body trajectory frame is already open.");
                m_PendingSourceSequence = m_CommittedSourceSequence;
                m_FrameOpen = true;
            }

            internal override void CommitFrame()
            {
                RequireOpenFrame();
                m_CommittedSourceSequence = m_PendingSourceSequence;
                m_FrameOpen = false;
            }

            internal override void DiscardFrame()
            {
                RequireOpenFrame();
                m_PendingSourceSequence = m_CommittedSourceSequence;
                m_FrameOpen = false;
            }

            public override void Dispose()
            {
                m_FrameOpen = false;
                m_Disposed = true;
            }

            void RequireAlive()
            {
                if (m_Disposed)
                    throw new ObjectDisposedException(nameof(SelectedBodyTrajectoryAdapter));
            }

            void RequireOpenFrame()
            {
                RequireAlive();
                if (!m_FrameOpen)
                    throw new InvalidOperationException("Selected Body trajectory has no open frame.");
            }
        }
    }
}
