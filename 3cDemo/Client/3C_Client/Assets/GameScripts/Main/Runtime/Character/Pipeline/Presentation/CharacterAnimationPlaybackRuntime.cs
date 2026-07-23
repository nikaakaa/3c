using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public sealed class CharacterAnimationPlaybackRuntime : IDisposable
    {
        readonly CharacterAnimationPresentationBindingIndex m_Bindings;
        readonly AnimationPoseRequestWorkspace m_RequestWorkspace;
        readonly AnimationPosePlayableGraphRuntime m_PoseRuntime;
        readonly AnimationPlaybackLifecycle m_Lifecycle;
        readonly CharacterAnimationTracePublisher m_TracePublisher = new CharacterAnimationTracePublisher();
        readonly CharacterAnimationPlaybackCommandQueue m_Commands = new CharacterAnimationPlaybackCommandQueue();
        readonly List<AnimationPlaybackCommand> m_CommandBuffer = new List<AnimationPlaybackCommand>();
        readonly List<AnimationPlaybackLifecycleSnapshot> m_Snapshots = new List<AnimationPlaybackLifecycleSnapshot>();
        readonly List<AnimationMarkerSyncRelationSnapshot> m_MarkerSyncSnapshots = new List<AnimationMarkerSyncRelationSnapshot>();
        readonly List<AnimationMarkerSyncPlaybackSnapshot> m_MarkerSyncPlaybackSnapshots = new List<AnimationMarkerSyncPlaybackSnapshot>();
        readonly HashSet<AnimationPlaybackId> m_DemandedPlaybacks = new HashSet<AnimationPlaybackId>();
        readonly List<AnimationPlaybackId> m_RetiredPlaybacks = new List<AnimationPlaybackId>();
        readonly Dictionary<AnimationChannelId, AnimationSelectionState> m_Selections =
            new Dictionary<AnimationChannelId, AnimationSelectionState>();
        readonly Dictionary<AnimationPlaybackId, AnimationSamplingState> m_Sampling =
            new Dictionary<AnimationPlaybackId, AnimationSamplingState>();
        readonly List<AnimationTerminalState> m_ActiveTerminals = new List<AnimationTerminalState>();
        readonly HashSet<AnimationChannelId> m_RequiredChannels = new HashSet<AnimationChannelId>();
        readonly CharacterMotionMatchingPresentationModule m_MotionMatching;
        readonly BlendSpaceAnimationPoseRequestResolver m_BlendSpaces;
        readonly Dictionary<AnimationPlaybackId, AnimationMarkerSyncRawSample> m_RawSamples =
            new Dictionary<AnimationPlaybackId, AnimationMarkerSyncRawSample>();
        readonly AnimationMarkerSyncEffectiveSamplePage m_EffectiveSamples =
            new AnimationMarkerSyncEffectiveSamplePage();
        readonly Dictionary<AnimationPlayerSourceSampleKey, AnimationSourcePoseSample> m_ResolvedRequests =
            new Dictionary<AnimationPlayerSourceSampleKey, AnimationSourcePoseSample>();
        readonly Dictionary<AnimationPlaybackId, AnimationMarkerSyncEffectiveSample> m_MarkerEvaluation =
            new Dictionary<AnimationPlaybackId, AnimationMarkerSyncEffectiveSample>();
        readonly List<PoseNodeId> m_PlayerNodes = new List<PoseNodeId>();
        readonly List<AnimationMarkerSyncRelationSnapshot> m_MarkerRelationBuffer = new List<AnimationMarkerSyncRelationSnapshot>();
        readonly List<AnimationMarkerSyncPlaybackSnapshot> m_MarkerPlaybackBuffer = new List<AnimationMarkerSyncPlaybackSnapshot>();
        readonly MarkerRuntimeBinding[] m_MarkerRuntimes;
        readonly List<AnimationPlaybackId> m_RemoveSampling = new List<AnimationPlaybackId>();
        readonly List<AnimationTerminalState> m_Terminals = new List<AnimationTerminalState>();
        readonly List<PlayerSourceUsageFrame> m_PlayerSourceUsages = new List<PlayerSourceUsageFrame>();

        ulong m_SelectionSequence;
        ulong m_PresentationRequestSequence;
        ulong m_SourceContinuityIdentity;
        ulong m_RequestWorkspaceCompletionIdentity;
        ulong m_PlayerSourceUsageCompletionIdentity;
        bool m_Disposed;

        internal CharacterAnimationPlaybackRuntime(
            CharacterAnimationPresentationBindingIndex bindings,
            CharacterMotionMatchingPresentationModule motionMatching,
            AnimancerComponent animancer,
            CharacterAnimationRigBinding rigBinding,
            bool ownsGraphClock)
        {
            if (bindings == null || !bindings.IsValid)
                throw new ArgumentException("Animation Playback requires a valid presentation binding index.", nameof(bindings));
            m_Bindings = bindings;
            int sourceCapacity = m_Bindings.WorkspaceLayout.SourceCapacity;
            m_Snapshots.Capacity = checked(sourceCapacity + m_Bindings.SelectionInputs.Count);
            m_MarkerSyncSnapshots.Capacity = sourceCapacity;
            m_MarkerSyncPlaybackSnapshots.Capacity = sourceCapacity;
            m_RetiredPlaybacks.Capacity = sourceCapacity;
            m_RemoveSampling.Capacity = sourceCapacity;
            m_ActiveTerminals.Capacity = sourceCapacity;
            IReadOnlyList<CharacterPresentationSelectionInputEntry> selectionInputs =
                bindings.Projection.PosePlan.SelectionInputs;
            for (int i = 0; i < selectionInputs.Count; i++)
            {
                CharacterPresentationSelectionInputEntry input = selectionInputs[i];
                if (input.Availability == AnimationSelectionAvailabilityPolicy.RequireSelection)
                    m_RequiredChannels.Add(input.AnimationChannelId);
            }
            try
            {
                m_RequestWorkspace = new AnimationPoseRequestWorkspace(m_Bindings.WorkspaceLayout);
                m_PoseRuntime = new AnimationPosePlayableGraphRuntime(
                    animancer,
                    rigBinding,
                    m_Bindings,
                    ownsGraphClock);
                m_Lifecycle = new AnimationPlaybackLifecycle(m_Bindings);
                m_MotionMatching = motionMatching;
                var markerRuntimes = new List<MarkerRuntimeBinding>();
                for (int i = 0; i < bindings.Projection.PosePlan.Operations.Count; i++)
                {
                    CharacterPresentationPoseOperation operation = bindings.Projection.PosePlan.Operations[i];
                    if (operation.Code != CharacterPoseOperationCode.SelectedPosePlayer &&
                        operation.Code != CharacterPoseOperationCode.BlendStack &&
                        operation.Code != CharacterPoseOperationCode.BlendSpacePlayer || operation.MarkerSyncOperationIndex < 0)
                        continue;
                    CharacterPresentationPoseOperation marker =
                        bindings.Projection.PosePlan.Operations[operation.MarkerSyncOperationIndex];
                    AnimationChannelId channelId =
                        bindings.Projection.PosePlan.SelectionInputs[operation.SelectionInputIndex].AnimationChannelId;
                    markerRuntimes.Add(new MarkerRuntimeBinding(marker.NodeId, operation.NodeId, channelId));
                }
                m_MarkerRuntimes = markerRuntimes.ToArray();
                m_BlendSpaces = bindings.Projection.BlendSpacePlayers.Count > 0
                    ? new BlendSpaceAnimationPoseRequestResolver(bindings.Projection, bindings.WorkspaceLayout.SourceCapacity)
                    : null;
            }
            catch
            {
                m_PoseRuntime?.Dispose();
                m_RequestWorkspace?.Dispose();
                throw;
            }
        }

        internal static CharacterAnimationPresentationBindingIndex BuildBindings(
            CharacterPresentationSemanticContract contract,
            CharacterPresentationProjection projection)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            projection.RequireContract(contract);
            var errors = new List<string>();
            CharacterAnimationPresentationBindingIndex bindings =
                CharacterAnimationPresentationBindingIndex.Build(projection, contract, errors);
            if (!bindings.IsValid)
                throw new InvalidOperationException(string.Join("\n", errors));
            return bindings;
        }

        public IReadOnlyList<AnimationPlaybackId> RetiredPlaybacks => m_RetiredPlaybacks;
        public IReadOnlyList<AnimationPlaybackLifecycleSnapshot> Snapshots => m_Snapshots;
        public bool HasRuntimeDiagnosticsSnapshot => m_PoseRuntime.HasDiagnosticsSnapshot;
        public AnimationPresentationRuntimeSnapshot RuntimeDiagnosticsSnapshot => m_PoseRuntime.DiagnosticsSnapshot;
        public void SetPoseWatchInterests(Guid ownerId, IReadOnlyList<AnimationPoseWatchIdentity> interests) =>
            m_PoseRuntime.SetPoseWatchInterests(ownerId, interests);
        public void RemovePoseWatchInterests(Guid ownerId) => m_PoseRuntime.RemovePoseWatchInterests(ownerId);
        public bool MotionMatchingRuntimeEnabled => m_MotionMatching != null && m_MotionMatching.Enabled;
        public bool AcceptsMotionMatchingTrajectoryIntent => m_MotionMatching?.AcceptsTrajectoryIntent == true;
        public IReadOnlyList<AnimationMarkerSyncRelationSnapshot> MarkerSyncSnapshots => m_MarkerSyncSnapshots;
        public IReadOnlyList<AnimationMarkerSyncPlaybackSnapshot> MarkerSyncPlaybackSnapshots => m_MarkerSyncPlaybackSnapshots;
        public IReadOnlyList<PlayerSourceUsageFrame> PlayerSourceUsages => m_PlayerSourceUsages;
        public bool HasRequiredOutput
        {
            get
            {
                IReadOnlyList<CharacterPresentationSelectionInputEntry> inputs = m_Bindings.Projection.PosePlan.SelectionInputs;
                for (int i = 0; i < inputs.Count; i++)
                {
                    CharacterPresentationSelectionInputEntry input = inputs[i];
                    if (input.Availability != AnimationSelectionAvailabilityPolicy.RequireSelection)
                        continue;
                    if (!m_Selections.TryGetValue(input.AnimationChannelId, out AnimationSelectionState selection) ||
                        !selection.HasPlayback ||
                        !m_Sampling.ContainsKey(selection.PlaybackId) &&
                        (m_MotionMatching == null || !m_MotionMatching.ContainsSampling(selection.PlaybackId)))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public bool TryCaptureMotionMatchingSearchReplay(
            string programProducerId,
            out MotionMatchingSearchReplayArtifact artifact)
        {
            RequireAlive();
            artifact = null;
            return m_MotionMatching != null &&
                   m_MotionMatching.TryCaptureSearchReplay(programProducerId, out artifact);
        }

        public void CaptureMotionMatchingTrajectoryIntent(CharacterPresentationTrajectoryIntent intent)
        {
            RequireAlive();
            if (m_MotionMatching == null)
                throw new InvalidOperationException("Presentation without a Motion Matching payload cannot accept trajectory intent.");
            m_MotionMatching.CaptureTrajectoryIntent(intent);
        }

        internal void CaptureMotionMatchingFixtureQuery(
            string programProducerId,
            MotionMatchingSearchReplayArtifact fixture)
        {
            RequireAlive();
            RequireMotionMatchingModule().CaptureFixtureQuery(programProducerId, fixture);
        }

        public void Publish(PresentationCommand command, CharacterPresentationProducerEntry producer) =>
            Publish(CharacterPresentationCommand.FromFloat32(command), producer);

        public void Publish(CharacterPresentationCommand command, CharacterPresentationProducerEntry producer)
        {
            RequireAlive();
            RequireAnimationProducer(producer, command.Kind);
            switch (command.Kind)
            {
                case CharacterPresentationCommandKind.SelectProducer:
                    PublishSelection(command, producer);
                    break;
                case CharacterPresentationCommandKind.SampleProducer:
                    PublishSample(command, producer);
                    break;
                case CharacterPresentationCommandKind.CompleteProducer:
                case CharacterPresentationCommandKind.ReleaseProducer:
                    PublishTerminal(command, producer);
                    break;
                default:
                    throw new ArgumentException(
                        $"Presentation command '{command.Kind}' is not an animation playback command.",
                        nameof(command));
            }
        }

        public void Retire(PresentationCommand command, CharacterPresentationProducerEntry producer) =>
            Retire(CharacterPresentationCommand.FromFloat32(command), producer);

        public void Retire(CharacterPresentationCommand command, CharacterPresentationProducerEntry producer)
        {
            RequireAlive();
            RequireAnimationProducer(producer, command.Kind);
            var playbackId = new AnimationPlaybackId(producer.ProducerId, command.ProducerGeneration);
            switch (command.Kind)
            {
                case CharacterPresentationCommandKind.SelectProducer:
                    if (m_Selections.TryGetValue(producer.AnimationChannelId, out AnimationSelectionState selection) &&
                        selection.EventId.Equals(command.Header.EventId))
                    {
                        m_Selections[producer.AnimationChannelId] = AnimationSelectionState.Empty(
                            producer.AnimationChannelId,
                            command.Header.Tick.Value,
                            command.Header.Sequence);
                    }
                    break;
                case CharacterPresentationCommandKind.SampleProducer:
                    if (producer.AnimationSourceKind == AnimationPoseSourceKind.MotionMatching)
                    {
                        RequireMotionMatchingModule().RetireSample(playbackId, IsSamplingRetained(playbackId));
                    }
                    else if (m_Sampling.TryGetValue(playbackId, out AnimationSamplingState sampling) &&
                             sampling.EventId.Equals(command.Header.EventId) &&
                             !IsSamplingRetained(playbackId))
                    {
                        m_Sampling.Remove(playbackId);
                    }
                    break;
                case CharacterPresentationCommandKind.CompleteProducer:
                case CharacterPresentationCommandKind.ReleaseProducer:
                    for (int i = m_Terminals.Count - 1; i >= 0; i--)
                    {
                        if (m_Terminals[i].EventId.Equals(command.Header.EventId))
                            m_Terminals.RemoveAt(i);
                    }
                    for (int i = m_ActiveTerminals.Count - 1; i >= 0; i--)
                    {
                        if (m_ActiveTerminals[i].EventId.Equals(command.Header.EventId))
                            m_ActiveTerminals.RemoveAt(i);
                    }
                    break;
                default:
                    throw new ArgumentException(
                        $"Presentation command '{command.Kind}' is not an animation playback command.",
                        nameof(command));
            }
        }

        public void Replace(
            CharacterPresentationCommand current,
            CharacterPresentationCommand replacement,
            CharacterPresentationProducerEntry currentProducer,
            CharacterPresentationProducerEntry replacementProducer)
        {
            RequireAlive();
            RequireAnimationProducer(currentProducer, current.Kind);
            RequireAnimationProducer(replacementProducer, replacement.Kind);
            switch (replacement.Kind)
            {
                case CharacterPresentationCommandKind.SelectProducer:
                    if (current.Kind != CharacterPresentationCommandKind.SelectProducer ||
                        currentProducer.AnimationChannelId != replacementProducer.AnimationChannelId)
                    {
                        throw new InvalidOperationException(
                            "Animation selection replacement changed its Animation Channel or command kind.");
                    }
                    if (currentProducer.AnimationSourceKind == AnimationPoseSourceKind.MotionMatching ||
                        replacementProducer.AnimationSourceKind == AnimationPoseSourceKind.MotionMatching)
                        RequireMotionMatchingModule().ReplaceSelection(currentProducer, replacementProducer);
                    PublishSelection(replacement, replacementProducer);
                    break;
                case CharacterPresentationCommandKind.SampleProducer:
                    if (current.Kind != CharacterPresentationCommandKind.SampleProducer)
                        throw new InvalidOperationException("Animation sample replacement changed its command kind.");
                    var currentPlayback = new AnimationPlaybackId(
                        currentProducer.ProducerId,
                        current.ProducerGeneration);
                    var replacementPlayback = new AnimationPlaybackId(
                        replacementProducer.ProducerId,
                        replacement.ProducerGeneration);
                    if (!currentPlayback.Equals(replacementPlayback))
                        throw new InvalidOperationException("Animation sample replacement changed its playback identity.");
                    if (replacementProducer.AnimationSourceKind == AnimationPoseSourceKind.MotionMatching)
                        RequireMotionMatchingModule().ReplaceSample(replacementPlayback, replacementProducer);
                    else if (m_Sampling.TryGetValue(currentPlayback, out AnimationSamplingState sampling))
                        sampling.Replace(current, replacement);
                    else
                        m_Sampling.Add(replacementPlayback, CreateSamplingState(replacementProducer, replacement));
                    break;
                case CharacterPresentationCommandKind.CompleteProducer:
                case CharacterPresentationCommandKind.ReleaseProducer:
                    if (current.Kind != CharacterPresentationCommandKind.CompleteProducer &&
                        current.Kind != CharacterPresentationCommandKind.ReleaseProducer)
                    {
                        throw new InvalidOperationException("Animation terminal replacement changed its command family.");
                    }
                    Retire(current, currentProducer);
                    PublishTerminal(replacement, replacementProducer);
                    break;
                default:
                    throw new ArgumentException(
                        $"Presentation command '{replacement.Kind}' is not an animation playback command.",
                        nameof(replacement));
            }
        }

        internal ComposedAnimationPoseFrame Present(
            ulong presentationFrame,
            ulong latestSimulationTick,
            float interpolationAlpha,
            float presentationDeltaSeconds,
            in CharacterBodyPresentationFrame bodyFrame,
            RuntimeDiagnosticsContext diagnostics = null)
        {
            RequireAlive();
            if (presentationFrame == 0 || !float.IsFinite(interpolationAlpha) || !float.IsFinite(presentationDeltaSeconds) ||
                presentationDeltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(presentationDeltaSeconds));
            }

            m_Commands.DiscardFrameLocalPoseCommands();
            foreach (AnimationSelectionState selection in m_Selections.Values)
            {
                m_Commands.EnqueueSelection(HasEffectivePlayback(selection)
                    ? AnimationChannelSelection.Select(
                        selection.AnimationChannelId,
                        selection.PlaybackId,
                        latestSimulationTick,
                        NextSelectionSequence())
                    : AnimationChannelSelection.Empty(
                        selection.AnimationChannelId,
                        latestSimulationTick,
                        NextSelectionSequence()));
            }
            for (int i = 0; i < m_Terminals.Count; i++)
            {
                AnimationTerminalState terminal = m_Terminals[i];
                if (terminal.Kind == CharacterPresentationCommandKind.CompleteProducer)
                    m_Commands.EnqueuePlaybackComplete(latestSimulationTick, terminal.PlaybackId);
                else
                    m_Commands.EnqueuePlaybackRelease(latestSimulationTick, terminal.PlaybackId);
            }
            m_Terminals.Clear();

            m_Commands.CopyPendingTo(m_CommandBuffer);
            m_Lifecycle.CollectSampleDemand(m_CommandBuffer, m_PoseRuntime, m_DemandedPlaybacks);
            BuildPlayerSourceUsages();
            double presentationSampleTick = bodyFrame.PreviousTick +
                                            (bodyFrame.CurrentTick - bodyFrame.PreviousTick) *
                                            (double)bodyFrame.SampleAlpha;
            ResolveRawAndEffectiveSamples(
                presentationSampleTick,
                presentationDeltaSeconds,
                diagnostics);

            m_PoseRuntime.Advance(presentationDeltaSeconds);
            m_RequestWorkspace.BeginFrame(NextRequestWorkspaceCompletionIdentity());
            m_BlendSpaces?.BeginFrame();
            m_ResolvedRequests.Clear();
            MotionMatchingFrameResolution motionMatchingResolution = default;
            bool hasMotionMatchingResolution = false;
            if (m_MotionMatching != null)
            {
                m_MotionMatching.BeginDemandFrame();
                foreach (AnimationPlaybackId playbackId in m_DemandedPlaybacks)
                {
                    if (m_MotionMatching.TryGetSamplingChannel(playbackId, out AnimationChannelId channelId) &&
                        IsSelectedPlayback(channelId, playbackId))
                    {
                        m_MotionMatching.SubmitDemand(playbackId);
                    }
                }
                if (m_MotionMatching.HasFrameWork(m_PoseRuntime))
                {
                    motionMatchingResolution = m_MotionMatching.ResolveFrame(
                        presentationFrame,
                        presentationDeltaSeconds,
                        in bodyFrame,
                        m_RequestWorkspace,
                        m_PoseRuntime,
                        NextPresentationRequestSequence,
                        diagnostics);
                    hasMotionMatchingResolution = true;
                    for (int requestIndex = 0; requestIndex < motionMatchingResolution.RequestCount; requestIndex++)
                    {
                        MotionMatchingResolvedFrameRequest resolved = motionMatchingResolution.GetRequest(requestIndex);
                        AnimationSourcePoseSample sourceSample = resolved.SourceSample;
                        AnimationSelectionFrame request = sourceSample.Selection;
                        m_PoseRuntime.CollectPlayerNodes(request.AnimationChannelId, request.SourceId.SourceKind, m_PlayerNodes);
                        for (int playerIndex = 0; playerIndex < m_PlayerNodes.Count; playerIndex++)
                        {
                            AddPlayerSourceUsage(
                                m_PlayerNodes[playerIndex],
                                request.SourceId,
                                PlayerSourceUsageKind.Sample);
                            m_ResolvedRequests.Add(
                                new AnimationPlayerSourceSampleKey(m_PlayerNodes[playerIndex], request.SourceId),
                                sourceSample);
                        }
                        if (resolved.SubmitToLifecycle)
                            m_Commands.EnqueuePoseRequest(latestSimulationTick, request);
                    }
                }
            }
            foreach (AnimationPlaybackId playbackId in m_DemandedPlaybacks)
            {
                if (m_MotionMatching?.ContainsSampling(playbackId) == true)
                    continue;
                if (!m_Sampling.TryGetValue(playbackId, out AnimationSamplingState sampling) ||
                    !m_RawSamples.TryGetValue(playbackId, out AnimationMarkerSyncRawSample raw))
                {
                    continue;
                }
                m_PoseRuntime.CollectPlayerNodes(sampling.Producer.AnimationChannelId, sampling.SourceId.SourceKind, m_PlayerNodes);
                bool submitted = false;
                bool unavailable = false;
                for (int playerIndex = 0; playerIndex < m_PlayerNodes.Count; playerIndex++)
                {
                    PoseNodeId playerNodeId = m_PlayerNodes[playerIndex];
                    AnimationMarkerSyncEffectiveSample effective = m_EffectiveSamples.TryGet(
                        m_PlayerSourceUsageCompletionIdentity,
                        playerNodeId,
                        sampling.SourceId,
                        out AnimationMarkerSyncEffectiveSample mapped)
                        ? mapped
                        : RawEffective(raw);
                    bool resolved;
                    AnimationSourcePoseSample sourceSample;
                    if (sampling.SourceId.SourceKind == AnimationPoseSourceKind.BlendSpace)
                    {
                        if (m_BlendSpaces == null)
                            throw new InvalidOperationException("Blend Space source has no compiled request resolver.");
                        resolved = m_BlendSpaces.TryResolve(
                            m_Bindings,
                            m_RequestWorkspace,
                            playerNodeId,
                            sampling.Producer.AnimationChannelId,
                            sampling.SourceId,
                            sampling.SourcePoseContinuityIdentity,
                            NextPresentationRequestSequence(),
                            sampling.Producer.ProgramProducerIndex,
                            effective.LocalTime,
                            effective.ContinuousTime,
                            effective.Cycle,
                            sampling.ResolveVisualTimeScale(effective, presentationDeltaSeconds),
                            in bodyFrame,
                            diagnostics,
                            out sourceSample);
                    }
                    else
                    {
                        resolved = TimelineAnimationPoseRequestResolver.TryResolve(
                            m_Bindings,
                            m_RequestWorkspace,
                            sampling.Producer.AnimationChannelId,
                            sampling.SourceId,
                            sampling.SourcePoseContinuityIdentity,
                            NextPresentationRequestSequence(),
                            sampling.Producer.ProgramProducerIndex,
                            effective.LocalTime,
                            effective.ContinuousTime,
                            effective.Cycle,
                            sampling.ResolveVisualTimeScale(effective, presentationDeltaSeconds),
                            sampling.IsTrackLooping,
                            HasPlayerSourceUsage(playerNodeId, sampling.SourceId, PlayerSourceUsageKind.Retained) &&
                            !HasPlayerSourceUsage(playerNodeId, sampling.SourceId, PlayerSourceUsageKind.Sample),
                            out sourceSample);
                    }
                    if (!resolved)
                    {
                        if (submitted)
                            throw new InvalidOperationException("Timeline source availability differs between Player consumers.");
                        unavailable = true;
                        continue;
                    }
                    if (unavailable)
                        throw new InvalidOperationException("Timeline source availability differs between Player consumers.");
                    AnimationSelectionFrame request = sourceSample.Selection;
                    m_ResolvedRequests.Add(new AnimationPlayerSourceSampleKey(playerNodeId, request.SourceId), sourceSample);
                    if (!submitted)
                    {
                        m_Commands.EnqueuePoseRequest(latestSimulationTick, request);
                        submitted = true;
                    }
                }
                if (unavailable)
                {
                    m_Commands.EnqueuePoseUnavailable(
                        latestSimulationTick,
                        sampling.Producer.AnimationChannelId,
                        playbackId);
                }
            }

            m_Commands.CopyPendingTo(m_CommandBuffer);
            m_Lifecycle.Apply(
                m_CommandBuffer,
                m_PoseRuntime,
                NextPresentationRequestSequence);
            ComposedAnimationPoseFrame composedPose = m_PoseRuntime.Evaluate(
                presentationDeltaSeconds,
                m_ResolvedRequests);
            m_PoseRuntime.AppendReleasedSourceUsages(m_PlayerSourceUsages);
            if (hasMotionMatchingResolution)
            {
                m_MotionMatching.CompleteFrame(
                    in motionMatchingResolution,
                    m_PoseRuntime,
                    diagnostics);
            }
            m_Lifecycle.BuildSnapshot(m_PoseRuntime, m_Snapshots);
            m_PoseRuntime.PublishDiagnostics(m_Snapshots, m_BlendSpaces);
            BuildMarkerSnapshots();
            AttachMarkerLifecyclePhases();
            PruneUnreferencedSampling();
            for (int i = 0; i < m_MarkerRuntimes.Length; i++)
                m_MarkerRuntimes[i].Runtime.Retire(m_RetiredPlaybacks);
            if (diagnostics != null)
            {
                m_TracePublisher.PublishPlaybackLifecycle(
                    diagnostics,
                    m_CommandBuffer,
                    m_Snapshots,
                    m_MarkerSyncSnapshots,
                    m_RetiredPlaybacks);
            }
            m_Commands.Acknowledge(m_CommandBuffer);
            m_CommandBuffer.Clear();
            return composedPose;
        }

        public void Reset()
        {
            Reset(PoseDiscontinuityResetReason.PresentationReset);
        }

        internal void Reset(PoseDiscontinuityResetReason reason)
        {
            if (m_Disposed)
                return;
            m_PoseRuntime.Reset(reason);
            m_RequestWorkspace.Reset();
            m_Commands.Clear();
            m_Lifecycle.Reset();
            m_CommandBuffer.Clear();
            m_Snapshots.Clear();
            m_MarkerSyncSnapshots.Clear();
            m_MarkerSyncPlaybackSnapshots.Clear();
            m_DemandedPlaybacks.Clear();
            m_RetiredPlaybacks.Clear();
            m_Selections.Clear();
            m_Sampling.Clear();
            m_ActiveTerminals.Clear();
            m_RawSamples.Clear();
            m_EffectiveSamples.Reset();
            m_ResolvedRequests.Clear();
            m_PlayerSourceUsages.Clear();
            m_MotionMatching?.Reset(
                0,
                MotionMatchingPresentationResetReason.PresentationReset,
                true);
            m_RemoveSampling.Clear();
            ResetMarkerRuntimes();
            m_Terminals.Clear();
        }

        internal void ResetPoseBranch(ulong resetSequence)
        {
            RequireAlive();
            m_PoseRuntime.Reset(PoseDiscontinuityResetReason.BranchReplacement);
            foreach (AnimationSamplingState sampling in m_Sampling.Values)
                sampling.RebasePresentation();
            m_RequestWorkspace.Reset();
            m_Commands.Clear();
            m_Lifecycle.Reset();
            m_CommandBuffer.Clear();
            m_Snapshots.Clear();
            m_MarkerSyncSnapshots.Clear();
            m_MarkerSyncPlaybackSnapshots.Clear();
            m_DemandedPlaybacks.Clear();
            m_RetiredPlaybacks.Clear();
            m_RawSamples.Clear();
            m_EffectiveSamples.Reset();
            m_ResolvedRequests.Clear();
            m_PlayerSourceUsages.Clear();
            ResetMarkerRuntimes();
            m_Terminals.Clear();
            m_MotionMatching?.Reset(
                resetSequence,
                MotionMatchingPresentationResetReason.BodyStreamReset,
                false);
        }

        internal void RetargetBodyBranch(ulong resetSequence)
        {
            RequireAlive();
            if (resetSequence == 0)
                throw new ArgumentOutOfRangeException(nameof(resetSequence));
            m_MotionMatching?.Reset(
                resetSequence,
                MotionMatchingPresentationResetReason.BodyStreamReset,
                false);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            Exception failure = null;
            try
            {
                m_PoseRuntime.Dispose();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            try
            {
                m_MotionMatching?.Dispose();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            try
            {
                m_RequestWorkspace.Dispose();
            }
            catch (Exception exception)
            {
                if (failure == null)
                    failure = exception;
            }
            if (failure != null)
                throw failure;
        }

        void ResolveRawAndEffectiveSamples(
            double presentationSampleTick,
            float presentationDeltaSeconds,
            RuntimeDiagnosticsContext diagnostics)
        {
            m_RawSamples.Clear();
            foreach (AnimationPlaybackId playbackId in m_DemandedPlaybacks)
            {
                if (!m_Sampling.TryGetValue(playbackId, out AnimationSamplingState sampling))
                    continue;
                m_RawSamples.Add(playbackId, sampling.ResolveRawSample(
                    playbackId,
                    presentationSampleTick,
                    presentationDeltaSeconds));
            }
            try
            {
                m_EffectiveSamples.BeginFrame(m_PlayerSourceUsageCompletionIdentity);
                for (int markerIndex = 0; markerIndex < m_MarkerRuntimes.Length; markerIndex++)
                {
                    MarkerRuntimeBinding marker = m_MarkerRuntimes[markerIndex];
                    marker.Runtime.BeginFrame();
                    if (!m_Selections.TryGetValue(marker.ChannelId, out AnimationSelectionState selection) ||
                        !HasEffectivePlayback(selection) ||
                        !m_RawSamples.TryGetValue(selection.PlaybackId, out AnimationMarkerSyncRawSample target))
                        continue;
                    if (!TryGetPlayerUsage(
                            marker.PlayerNodeId,
                            PlayerSourceUsageKind.HandoffReference,
                            out PlayerSourceUsageFrame handoff))
                    {
                        marker.Runtime.RecordNoCurrentSource(target);
                    }
                    else
                    {
                        AnimationPlaybackId sourcePlayback = handoff.SourceId.PlaybackId;
                        if (!m_RawSamples.TryGetValue(sourcePlayback, out AnimationMarkerSyncRawSample source))
                            throw new AnimationMarkerSyncException(AnimationMarkerSyncInvalidReason.SourceSampleMissing, sourcePlayback);
                        marker.Runtime.EnsureHandoff(source, target);
                    }
                    marker.Runtime.Evaluate(m_RawSamples, m_MarkerEvaluation);
                    if (!m_MarkerEvaluation.TryGetValue(selection.PlaybackId, out AnimationMarkerSyncEffectiveSample effective))
                        throw new AnimationMarkerSyncException(AnimationMarkerSyncInvalidReason.NonFiniteResult, selection.PlaybackId);
                    for (int usageIndex = 0; usageIndex < m_PlayerSourceUsages.Count; usageIndex++)
                    {
                        PlayerSourceUsageFrame usage = m_PlayerSourceUsages[usageIndex];
                        if (usage.PlayerNodeId != marker.PlayerNodeId || usage.Kind == PlayerSourceUsageKind.Release ||
                            !m_MarkerEvaluation.TryGetValue(usage.SourceId.PlaybackId, out AnimationMarkerSyncEffectiveSample usageEffective))
                        {
                            continue;
                        }
                        m_EffectiveSamples.Set(
                            marker.MarkerNodeId,
                            marker.PlayerNodeId,
                            usage.SourceId,
                            in usageEffective);
                    }
                }
            }
            catch (AnimationMarkerSyncException failure)
            {
                m_RawSamples.TryGetValue(failure.PlaybackId, out AnimationMarkerSyncRawSample failedSample);
                m_TracePublisher.PublishMarkerSyncFailure(diagnostics, failure, failedSample);
                throw;
            }
        }

        void PublishSelection(CharacterPresentationCommand command, CharacterPresentationProducerEntry producer)
        {
            var playbackId = new AnimationPlaybackId(producer.ProducerId, command.ProducerGeneration);
            if (!m_Selections.TryGetValue(producer.AnimationChannelId, out AnimationSelectionState current) ||
                IsNewer(command.Header, current.Tick, current.Sequence))
            {
                m_Selections[producer.AnimationChannelId] = AnimationSelectionState.Select(
                    producer.AnimationChannelId,
                    playbackId,
                    command.Header.EventId,
                    command.Header.Tick.Value,
                    command.Header.Sequence);
            }
        }

        void PublishSample(CharacterPresentationCommand command, CharacterPresentationProducerEntry producer)
        {
            var playbackId = new AnimationPlaybackId(producer.ProducerId, command.ProducerGeneration);
            if (producer.AnimationSourceKind == AnimationPoseSourceKind.MotionMatching)
            {
                RequireMotionMatchingModule().PublishSample(playbackId, producer);
                return;
            }
            if (!m_Sampling.TryGetValue(playbackId, out AnimationSamplingState sampling))
            {
                m_Sampling.Add(playbackId, CreateSamplingState(producer, command));
                return;
            }
            sampling.Capture(command);
        }

        AnimationSamplingState CreateSamplingState(
            CharacterPresentationProducerEntry producer,
            CharacterPresentationCommand command)
        {
            return new AnimationSamplingState(
                producer,
                command,
                NextSourceContinuityIdentity());
        }

        void PublishTerminal(CharacterPresentationCommand command, CharacterPresentationProducerEntry producer)
        {
            var playbackId = new AnimationPlaybackId(producer.ProducerId, command.ProducerGeneration);
            var terminal = new AnimationTerminalState(
                command.Kind,
                producer.AnimationChannelId,
                playbackId,
                command.Header.EventId,
                command.Header.Tick.Value,
                command.Header.Sequence);
            m_Terminals.Add(terminal);
            for (int i = 0; i < m_ActiveTerminals.Count; i++)
            {
                if (!m_ActiveTerminals[i].EventId.Equals(command.Header.EventId))
                    continue;
                m_ActiveTerminals[i] = terminal;
                return;
            }
            m_ActiveTerminals.Add(terminal);
        }

        void AttachMarkerLifecyclePhases()
        {
            for (int markerIndex = 0; markerIndex < m_MarkerSyncSnapshots.Count; markerIndex++)
            {
                AnimationMarkerSyncRelationSnapshot marker = m_MarkerSyncSnapshots[markerIndex];
                for (int lifecycleIndex = 0; lifecycleIndex < m_Snapshots.Count; lifecycleIndex++)
                {
                    AnimationPlaybackLifecycleSnapshot lifecycle = m_Snapshots[lifecycleIndex];
                    if (!lifecycle.PlaybackId.Equals(marker.Target))
                        continue;
                    m_MarkerSyncSnapshots[markerIndex] = marker.WithLifecyclePhase(lifecycle.Phase);
                    break;
                }
            }
        }

        static void RequireAnimationProducer(
            CharacterPresentationProducerEntry producer,
            CharacterPresentationCommandKind commandKind)
        {
            if (producer == null || producer.Kind != CharacterPresentationProducerKind.Animation ||
                !producer.AnimationChannelId.IsValid ||
                !Enum.IsDefined(typeof(AnimationPoseSourceKind), producer.AnimationSourceKind) ||
                (producer.AnimationSourceKind == AnimationPoseSourceKind.Timeline && producer.Animation == null) ||
                (producer.AnimationSourceKind == AnimationPoseSourceKind.MotionMatching && producer.Animation != null) ||
                (producer.AnimationSourceKind == AnimationPoseSourceKind.BlendSpace && producer.BlendSpacePlanIndex < 0))
            {
                throw new InvalidOperationException(
                    $"Presentation command '{commandKind}' targets a non-animation producer.");
            }
        }

        bool IsSamplingRetained(AnimationPlaybackId playbackId) =>
            m_Lifecycle.Retains(playbackId, m_PoseRuntime);

        void PruneUnreferencedSampling()
        {
            m_RetiredPlaybacks.Clear();
            m_RemoveSampling.Clear();
            foreach (AnimationPlaybackId playbackId in m_Sampling.Keys)
            {
                if (!IsSamplingRetained(playbackId) && !HasRawSelection(playbackId))
                    m_RemoveSampling.Add(playbackId);
            }
            for (int i = 0; i < m_RemoveSampling.Count; i++)
            {
                AnimationPlaybackId playbackId = m_RemoveSampling[i];
                m_Sampling.Remove(playbackId);
                m_RetiredPlaybacks.Add(playbackId);
            }
            m_MotionMatching?.PruneUnreferencedSampling(
                m_Lifecycle,
                m_PoseRuntime,
                HasRawSelection,
                m_RetiredPlaybacks);
        }

        void BuildPlayerSourceUsages()
        {
            m_PlayerSourceUsages.Clear();
            ulong completionIdentity = NextPlayerSourceUsageCompletionIdentity();
            m_PoseRuntime.CollectRetainedSourceUsages(m_PlayerSourceUsages, completionIdentity);
            foreach (AnimationSelectionState selection in m_Selections.Values)
            {
                if (!HasEffectivePlayback(selection) ||
                    !m_Sampling.TryGetValue(selection.PlaybackId, out AnimationSamplingState sampling))
                    continue;
                AnimationPoseSourceId incoming = sampling.SourceId;
                m_PoseRuntime.CollectPlayerNodes(selection.AnimationChannelId, incoming.SourceKind, m_PlayerNodes);
                PlayerSourceUsageKind usageKind = HasTerminalAfterSelection(selection)
                    ? PlayerSourceUsageKind.Retained
                    : PlayerSourceUsageKind.Sample;
                for (int playerIndex = 0; playerIndex < m_PlayerNodes.Count; playerIndex++)
                {
                    PoseNodeId playerNodeId = m_PlayerNodes[playerIndex];
                    AddPlayerSourceUsage(playerNodeId, incoming, usageKind);
                    if (usageKind == PlayerSourceUsageKind.Sample &&
                        m_PoseRuntime.TryGetHandoffSource(playerNodeId, incoming, out AnimationPoseSourceId outgoing))
                        AddPlayerSourceUsage(playerNodeId, outgoing, PlayerSourceUsageKind.HandoffReference);
                }
            }
        }

        void AddPlayerSourceUsage(
            PoseNodeId playerNodeId,
            AnimationPoseSourceId sourceId,
            PlayerSourceUsageKind kind)
        {
            for (int i = 0; i < m_PlayerSourceUsages.Count; i++)
            {
                PlayerSourceUsageFrame usage = m_PlayerSourceUsages[i];
                if (usage.PlayerNodeId == playerNodeId && usage.SourceId == sourceId && usage.Kind == kind)
                    return;
            }
            m_PlayerSourceUsages.Add(new PlayerSourceUsageFrame(
                playerNodeId,
                sourceId,
                kind,
                m_PlayerSourceUsageCompletionIdentity));
        }

        bool TryGetPlayerUsage(
            PoseNodeId playerNodeId,
            PlayerSourceUsageKind kind,
            out PlayerSourceUsageFrame result)
        {
            for (int i = 0; i < m_PlayerSourceUsages.Count; i++)
            {
                PlayerSourceUsageFrame usage = m_PlayerSourceUsages[i];
                if (usage.PlayerNodeId == playerNodeId && usage.Kind == kind)
                {
                    result = usage;
                    return true;
                }
            }
            result = default;
            return false;
        }

        bool HasPlayerSourceUsage(
            PoseNodeId playerNodeId,
            AnimationPoseSourceId sourceId,
            PlayerSourceUsageKind kind)
        {
            for (int i = 0; i < m_PlayerSourceUsages.Count; i++)
            {
                PlayerSourceUsageFrame usage = m_PlayerSourceUsages[i];
                if (usage.PlayerNodeId == playerNodeId && usage.SourceId == sourceId && usage.Kind == kind)
                    return true;
            }
            return false;
        }

        bool IsSelectedPlayback(AnimationChannelId channelId, AnimationPlaybackId playbackId) =>
            m_Selections.TryGetValue(channelId, out AnimationSelectionState selection) &&
            HasEffectivePlayback(selection) && selection.PlaybackId.Equals(playbackId);

        bool HasEffectivePlayback(AnimationSelectionState selection)
        {
            if (!selection.HasPlayback)
                return false;
            if (m_RequiredChannels.Contains(selection.AnimationChannelId))
                return true;
            return !HasTerminalAfterSelection(selection);
        }

        bool HasTerminalAfterSelection(AnimationSelectionState selection)
        {
            for (int i = 0; i < m_ActiveTerminals.Count; i++)
            {
                AnimationTerminalState terminal = m_ActiveTerminals[i];
                if (terminal.PlaybackId.Equals(selection.PlaybackId) &&
                    (terminal.Tick > selection.Tick ||
                     terminal.Tick == selection.Tick && terminal.Sequence > selection.Sequence))
                {
                    return true;
                }
            }
            return false;
        }

        bool HasRawSelection(AnimationPlaybackId playbackId)
        {
            foreach (AnimationSelectionState selection in m_Selections.Values)
            {
                if (selection.HasPlayback && selection.PlaybackId.Equals(playbackId))
                    return true;
            }
            return false;
        }

        CharacterMotionMatchingPresentationModule RequireMotionMatchingModule() =>
            m_MotionMatching ?? throw new InvalidOperationException(
                "Motion Matching animation producer has no compiled Presentation Module.");

        ulong NextSelectionSequence() => Next(ref m_SelectionSequence, "selection");
        ulong NextPresentationRequestSequence() => Next(ref m_PresentationRequestSequence, "pose request");
        ulong NextSourceContinuityIdentity() => Next(ref m_SourceContinuityIdentity, "source continuity");
        ulong NextRequestWorkspaceCompletionIdentity() => Next(ref m_RequestWorkspaceCompletionIdentity, "request workspace completion");
        ulong NextPlayerSourceUsageCompletionIdentity() => Next(ref m_PlayerSourceUsageCompletionIdentity, "Player source usage completion");

        static ulong Next(ref ulong value, string name)
        {
            if (value == ulong.MaxValue)
                throw new InvalidOperationException($"Animation {name} identity was exhausted.");
            value++;
            return value;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterAnimationPlaybackRuntime));
        }

        static bool IsNewer(CharacterPresentationEventHeader header, ulong tick, ulong sequence) =>
            header.Tick.Value > tick || header.Tick.Value == tick && header.Sequence > sequence;

        static AnimationMarkerSyncEffectiveSample RawEffective(AnimationMarkerSyncRawSample raw) =>
            new AnimationMarkerSyncEffectiveSample(
                raw.PlaybackId,
                raw.LocalTime,
                raw.ContinuousTime,
                raw.Cycle,
                string.Empty,
                string.Empty,
                0f,
                false,
                false);

        void BuildMarkerSnapshots()
        {
            m_MarkerSyncSnapshots.Clear();
            m_MarkerSyncPlaybackSnapshots.Clear();
            for (int i = 0; i < m_MarkerRuntimes.Length; i++)
            {
                MarkerRuntimeBinding marker = m_MarkerRuntimes[i];
                marker.Runtime.BuildRelationSnapshot(m_MarkerRelationBuffer);
                for (int relationIndex = 0; relationIndex < m_MarkerRelationBuffer.Count; relationIndex++)
                {
                    m_MarkerSyncSnapshots.Add(m_MarkerRelationBuffer[relationIndex].WithNodeContext(
                        marker.MarkerNodeId,
                        marker.PlayerNodeId));
                }
                marker.Runtime.BuildPlaybackSnapshot(m_MarkerPlaybackBuffer);
                for (int playbackIndex = 0; playbackIndex < m_MarkerPlaybackBuffer.Count; playbackIndex++)
                {
                    m_MarkerSyncPlaybackSnapshots.Add(m_MarkerPlaybackBuffer[playbackIndex].WithNodeContext(
                        marker.MarkerNodeId,
                        marker.PlayerNodeId));
                }
            }
        }

        void ResetMarkerRuntimes()
        {
            for (int i = 0; i < m_MarkerRuntimes.Length; i++)
                m_MarkerRuntimes[i].Runtime.Reset();
            m_MarkerEvaluation.Clear();
            m_MarkerRelationBuffer.Clear();
            m_MarkerPlaybackBuffer.Clear();
        }

        sealed class MarkerRuntimeBinding
        {
            internal MarkerRuntimeBinding(PoseNodeId markerNodeId, PoseNodeId playerNodeId, AnimationChannelId channelId)
            {
                if (!markerNodeId.IsValid || !playerNodeId.IsValid || !channelId.IsValid)
                    throw new ArgumentException("Marker Sync runtime binding is invalid.");
                MarkerNodeId = markerNodeId;
                PlayerNodeId = playerNodeId;
                ChannelId = channelId;
                Runtime = new AnimationMarkerSyncRuntime();
            }

            internal PoseNodeId MarkerNodeId { get; }
            internal PoseNodeId PlayerNodeId { get; }
            internal AnimationChannelId ChannelId { get; }
            internal AnimationMarkerSyncRuntime Runtime { get; }
        }

        readonly struct AnimationSelectionState
        {
            AnimationSelectionState(
                AnimationChannelId animationChannelId,
                AnimationPlaybackId playbackId,
                bool hasPlayback,
                EventId eventId,
                ulong tick,
                ulong sequence)
            {
                AnimationChannelId = animationChannelId;
                PlaybackId = playbackId;
                HasPlayback = hasPlayback;
                EventId = eventId;
                Tick = tick;
                Sequence = sequence;
            }

            public AnimationChannelId AnimationChannelId { get; }
            public AnimationPlaybackId PlaybackId { get; }
            public bool HasPlayback { get; }
            public EventId EventId { get; }
            public ulong Tick { get; }
            public ulong Sequence { get; }

            public static AnimationSelectionState Select(
                AnimationChannelId animationChannelId,
                AnimationPlaybackId playbackId,
                EventId eventId,
                ulong tick,
                ulong sequence) =>
                new AnimationSelectionState(animationChannelId, playbackId, true, eventId, tick, sequence);

            public static AnimationSelectionState Empty(
                AnimationChannelId animationChannelId,
                ulong tick,
                ulong sequence) =>
                new AnimationSelectionState(animationChannelId, default, false, default, tick, sequence);
        }

        sealed class AnimationSamplingState
        {
            readonly List<AnimationSamplePoint> m_Samples = new List<AnimationSamplePoint>();
            double m_VisualContinuousTime;
            double m_PreviousPresentedEffectiveTime;
            double m_LastPresentationSampleTick;
            bool m_HasVisualCursor;
            bool m_HasPresentedEffectiveTime;
            bool m_WasRebased;

            public AnimationSamplingState(
                CharacterPresentationProducerEntry producer,
                CharacterPresentationCommand command,
                ulong sourcePoseContinuityIdentity)
            {
                Producer = producer ?? throw new ArgumentNullException(nameof(producer));
                if (sourcePoseContinuityIdentity == 0)
                    throw new ArgumentOutOfRangeException(nameof(sourcePoseContinuityIdentity));
                var playbackId = new AnimationPlaybackId(producer.ProducerId, command.ProducerGeneration);
                SourceId = new AnimationPoseSourceId(
                    playbackId,
                    producer.AnimationSourceKind,
                    new AnimationPoseSelectionGeneration(command.ProducerGeneration));
                SourcePoseContinuityIdentity = sourcePoseContinuityIdentity;
                Insert(command, true);
                RefreshLatestIdentity();
            }

            public CharacterPresentationProducerEntry Producer { get; }
            public AnimationPoseSourceId SourceId { get; }
            public ulong SourcePoseContinuityIdentity { get; }
            public EventId EventId { get; private set; }
            public bool IsTrackLooping => Producer.PlaybackMode == BTSMTL.Timeline.TimelinePlaybackMode.Loop;

            public void Capture(CharacterPresentationCommand command)
            {
                if (Insert(command, false))
                    RefreshLatestIdentity();
            }

            public float ResolveVisualTimeScale(
                AnimationMarkerSyncEffectiveSample effective,
                float presentationDeltaSeconds)
            {
                double current = effective.ContinuousTime;
                if (double.IsNaN(current) || double.IsInfinity(current) || current < 0d)
                    throw new InvalidOperationException($"Animation playback '{effective.PlaybackId}' produced an invalid effective time.");
                bool previousFrameRebased = m_WasRebased;
                bool beganRebase = effective.Rebased && !previousFrameRebased;
                m_WasRebased = effective.Rebased;
                if (!m_HasPresentedEffectiveTime || beganRebase || presentationDeltaSeconds <= 0.000001f)
                {
                    m_PreviousPresentedEffectiveTime = current;
                    m_HasPresentedEffectiveTime = true;
                    return 0f;
                }
                double elapsed = current - m_PreviousPresentedEffectiveTime;
                double previous = m_PreviousPresentedEffectiveTime;
                m_PreviousPresentedEffectiveTime = current;
                if (elapsed < -0.000001d)
                {
                    throw new InvalidOperationException(
                        $"Animation playback '{effective.PlaybackId}' effective time moved backwards without a rebase. " +
                        $"PreviousEffective={previous:R}, CurrentEffective={current:R}, RawVisual={m_VisualContinuousTime:R}, " +
                        $"PresentationSampleTick={m_LastPresentationSampleTick:R}, " +
                        $"RetainedSampleRange={m_Samples[0].Tick}..{m_Samples[m_Samples.Count - 1].Tick}, " +
                        $"EffectiveRebased={effective.Rebased}, PreviousFrameRebased={previousFrameRebased}, DeltaSeconds={presentationDeltaSeconds:R}.");
                }
                float scale = (float)(Math.Max(0d, elapsed) / presentationDeltaSeconds);
                if (!float.IsFinite(scale))
                    throw new InvalidOperationException($"Animation playback '{effective.PlaybackId}' produced an invalid visual time scale.");
                return scale;
            }

            public void RebasePresentation()
            {
                m_HasPresentedEffectiveTime = false;
                m_WasRebased = false;
            }

            public void Replace(
                CharacterPresentationCommand current,
                CharacterPresentationCommand replacement)
            {
                m_HasPresentedEffectiveTime = false;
                m_WasRebased = false;
                int currentIndex = Find(current.Header.EventId);
                if (currentIndex >= 0)
                    m_Samples.RemoveAt(currentIndex);
                else if (m_Samples.Count > 0 && replacement.Header.Tick.Value < m_Samples[0].Tick)
                    return;
                Insert(replacement, true);
                RefreshLatestIdentity();
            }

            public AnimationMarkerSyncRawSample ResolveRawSample(
                AnimationPlaybackId playbackId,
                double presentationSampleTick,
                float deltaSeconds)
            {
                if (double.IsNaN(presentationSampleTick) || double.IsInfinity(presentationSampleTick) ||
                    presentationSampleTick < 0d || !float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
                    throw new ArgumentOutOfRangeException(nameof(presentationSampleTick));
                if (m_Samples.Count == 0)
                    throw new InvalidOperationException($"Animation playback '{playbackId}' has no retained samples.");

                m_LastPresentationSampleTick = presentationSampleTick;
                PruneSamples(presentationSampleTick);
                double target = ResolveTargetContinuousTime(presentationSampleTick, deltaSeconds);
                if (!m_HasVisualCursor)
                {
                    m_VisualContinuousTime = target;
                    m_HasVisualCursor = true;
                }
                else if (target > m_VisualContinuousTime)
                {
                    m_VisualContinuousTime = target;
                }

                AnimationMarkerSyncBinding binding = Producer.MarkerSync;
                if (binding != null && binding.IsMarkerGroup)
                {
                    if (binding.SequenceTopology == BTSMTL.Timeline.AnimationMarkerSequenceTopology.Finite)
                    {
                        m_VisualContinuousTime = Math.Clamp(m_VisualContinuousTime, 0d, binding.DurationSeconds);
                        return new AnimationMarkerSyncRawSample(
                            playbackId,
                            Producer.AnimationChannelId,
                            binding,
                            (float)m_VisualContinuousTime,
                            m_VisualContinuousTime,
                            0);
                    }
                    int cycle = (int)Math.Floor(m_VisualContinuousTime / binding.DurationSeconds);
                    float localTime = (float)(m_VisualContinuousTime - cycle * binding.DurationSeconds);
                    return new AnimationMarkerSyncRawSample(
                        playbackId,
                        Producer.AnimationChannelId,
                        binding,
                        localTime,
                        m_VisualContinuousTime,
                        cycle);
                }
                int visualCycle = ResolveCycle(m_VisualContinuousTime, Producer.SourceDurationSeconds);
                float visualTime = ResolveLocalTime(
                    m_VisualContinuousTime,
                    Producer.SourceDurationSeconds,
                    visualCycle);
                return new AnimationMarkerSyncRawSample(
                    playbackId,
                    Producer.AnimationChannelId,
                    binding,
                    visualTime,
                    m_VisualContinuousTime,
                    visualCycle);
            }

            bool Insert(CharacterPresentationCommand command, bool replace)
            {
                var sample = new AnimationSamplePoint(
                    command.Header.EventId,
                    command.Header.Tick.Value,
                    command.Header.Sequence,
                    ToContinuousTime(command.SampleTime, command.Cycle));
                for (int i = 0; i < m_Samples.Count; i++)
                {
                    AnimationSamplePoint existing = m_Samples[i];
                    if (existing.Tick == sample.Tick)
                    {
                        if (!replace && sample.Sequence <= existing.Sequence)
                            return false;
                        m_Samples[i] = sample;
                        return true;
                    }
                    if (existing.Tick > sample.Tick)
                    {
                        m_Samples.Insert(i, sample);
                        return true;
                    }
                }
                m_Samples.Add(sample);
                return true;
            }

            int Find(EventId eventId)
            {
                for (int i = 0; i < m_Samples.Count; i++)
                {
                    if (m_Samples[i].EventId.Equals(eventId))
                        return i;
                }
                return -1;
            }

            void RefreshLatestIdentity()
            {
                if (m_Samples.Count == 0)
                    throw new InvalidOperationException("Animation sampling history became empty.");
                EventId = m_Samples[m_Samples.Count - 1].EventId;
            }

            void PruneSamples(double presentationSampleTick)
            {
                while (m_Samples.Count > 2 && m_Samples[1].Tick <= presentationSampleTick)
                    m_Samples.RemoveAt(0);
            }

            double ResolveTargetContinuousTime(double presentationSampleTick, float deltaSeconds)
            {
                AnimationSamplePoint first = m_Samples[0];
                if (presentationSampleTick <= first.Tick)
                    return first.ContinuousTime;
                for (int i = 1; i < m_Samples.Count; i++)
                {
                    AnimationSamplePoint current = m_Samples[i];
                    if (presentationSampleTick > current.Tick)
                        continue;
                    AnimationSamplePoint previous = m_Samples[i - 1];
                    if (current.Tick == previous.Tick)
                        return current.ContinuousTime;
                    double alpha = Math.Clamp(
                        (presentationSampleTick - previous.Tick) / (current.Tick - previous.Tick),
                        0d,
                        1d);
                    return previous.ContinuousTime +
                           (current.ContinuousTime - previous.ContinuousTime) * alpha;
                }
                return m_HasVisualCursor
                    ? m_VisualContinuousTime + deltaSeconds
                    : m_Samples[m_Samples.Count - 1].ContinuousTime;
            }

            int ResolveCycle(double continuousTime, float duration)
            {
                if (!IsTrackLooping || !float.IsFinite(duration) || duration <= 0f)
                    return 0;
                double cycle = Math.Floor(continuousTime / duration);
                if (cycle > int.MaxValue)
                    throw new InvalidOperationException("Animation visual cycle overflowed.");
                return (int)cycle;
            }

            float ResolveLocalTime(double continuousTime, float duration, int cycle)
            {
                if (!IsTrackLooping || !float.IsFinite(duration) || duration <= 0f)
                    return (float)continuousTime;
                return (float)Math.Max(0d, continuousTime - cycle * (double)duration);
            }

            double ToContinuousTime(float sampleTime, int cycle) =>
                Math.Max(0d, cycle * (double)Producer.SourceDurationSeconds + sampleTime);

            readonly struct AnimationSamplePoint
            {
                public AnimationSamplePoint(
                    EventId eventId,
                    ulong tick,
                    ulong sequence,
                    double continuousTime)
                {
                    EventId = eventId;
                    Tick = tick;
                    Sequence = sequence;
                    ContinuousTime = continuousTime;
                }

                public EventId EventId { get; }
                public ulong Tick { get; }
                public ulong Sequence { get; }
                public double ContinuousTime { get; }
            }
        }

        readonly struct AnimationTerminalState
        {
            public AnimationTerminalState(
                CharacterPresentationCommandKind kind,
                AnimationChannelId animationChannelId,
                AnimationPlaybackId playbackId,
                EventId eventId,
                ulong tick,
                ulong sequence)
            {
                if (!animationChannelId.IsValid || !playbackId.IsValid)
                    throw new ArgumentException("Animation terminal state is invalid.");
                Kind = kind;
                AnimationChannelId = animationChannelId;
                PlaybackId = playbackId;
                EventId = eventId;
                Tick = tick;
                Sequence = sequence;
            }

            public CharacterPresentationCommandKind Kind { get; }
            public AnimationChannelId AnimationChannelId { get; }
            public AnimationPlaybackId PlaybackId { get; }
            public EventId EventId { get; }
            public ulong Tick { get; }
            public ulong Sequence { get; }
        }
    }
}
