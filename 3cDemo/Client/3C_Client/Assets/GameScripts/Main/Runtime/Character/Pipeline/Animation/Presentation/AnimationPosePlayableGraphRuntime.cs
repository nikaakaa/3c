using System;
using System.Collections.Generic;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Presentation.Animancer;
using ThirdPersonSimulation;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal sealed class AnimationPosePlayableGraphRuntime : IDisposable
    {
        readonly AnimancerComponent m_Animancer;
        readonly CharacterAnimationPresentationBindingIndex m_Bindings;
        readonly AnimationPoseNativeWorkspace m_Workspace;
        readonly CharacterPoseGraphNativeProgram m_PosePlan;
        readonly PoseInertializationNativeProgram m_InertializationPlan;
        readonly AnimationPoseSourcePhysicalRegistry m_PhysicalSources;
        readonly AnimancerPoseSamplingBackend m_SourceBackend;
        readonly ComposedAnimationPoseFramePublisher m_FramePublisher;
        readonly AnimationPresentationRuntimeSnapshotPublisher m_DiagnosticsPublisher;
        readonly AnimationBlendStackRuntime[] m_Stacks;
        readonly AnimationSelectedPosePlayerRuntime[] m_DirectPlayers;
        readonly AnimationSlotBlendJob[] m_SlotJobs;
        readonly AnimationSelectedPosePlayerJob[] m_DirectPlayerJobs;
        readonly AnimationPhysicalSourceIdentity[] m_DirectPhysicalSources;
        readonly int[] m_DirectSourceIndices;
        readonly AnimationReleasedPoseSourceSnapshot[] m_ReleasedSources;
        readonly Dictionary<AnimationChannelId, List<AnimationBlendStackRuntime>> m_StacksByChannel =
            new Dictionary<AnimationChannelId, List<AnimationBlendStackRuntime>>();
        readonly Dictionary<PoseNodeId, AnimationBlendStackRuntime> m_StacksByNode =
            new Dictionary<PoseNodeId, AnimationBlendStackRuntime>();
        readonly Dictionary<PoseNodeId, int> m_PlayerIndicesByNode = new Dictionary<PoseNodeId, int>();
        readonly Dictionary<PoseNodeId, PoseNodeId> m_MarkerNodesByPlayer = new Dictionary<PoseNodeId, PoseNodeId>();
        readonly Dictionary<AnimationChannelId, List<AnimationSelectedPosePlayerRuntime>> m_DirectPlayersByChannel =
            new Dictionary<AnimationChannelId, List<AnimationSelectedPosePlayerRuntime>>();
        readonly AnimationMixerPlayable m_SourceFanIn;
        readonly Playable m_PreviousOutputSource;
        readonly bool m_ManagesGraphClock;
        readonly int m_FootPlacementWeightParameterIndex;

        AnimationScriptPlayable[] m_SlotPlayables;
        AnimationScriptPlayable[] m_DirectPlayerPlayables;
        AnimationScriptPlayable m_PoseGraphPlayable;
        AnimationScriptPlayable m_FinalWriterPlayable;
        ulong m_CompletionIdentity = 1;
        int m_ReleasedSourceCount;
        CharacterPoseGraphNativeBinding m_LastCompletedFrame;
        bool m_HasCompletedFrame;
        bool m_JobsInstalled;
        bool m_Disposed;

        internal AnimationPosePlayableGraphRuntime(
            AnimancerComponent animancer,
            CharacterAnimationRigBinding rigBinding,
            CharacterAnimationPresentationBindingIndex bindings,
            bool managesGraphClock)
        {
            m_Animancer = animancer ? animancer : throw new ArgumentNullException(nameof(animancer));
            m_Bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            if (!bindings.IsValid || bindings.Projection == null)
                throw new ArgumentException("Animation Presentation bindings are invalid.", nameof(bindings));
            CharacterPresentationProjection projection = bindings.Projection;
            projection.RequirePosePayload();

            AnimationPoseNativeWorkspace workspace = null;
            CharacterPoseGraphNativeProgram poseProgram = null;
            PoseInertializationNativeProgram inertializationProgram = null;
            AnimationPoseSourcePhysicalRegistry physicalSources = null;
            AnimancerPoseSamplingBackend sourceBackend = null;
            AnimationBlendStackRuntime[] stacks = null;
            AnimationSelectedPosePlayerRuntime[] directPlayers = null;
            AnimationPresentationRuntimeSnapshotPublisher diagnosticsPublisher = null;
            AnimationMixerPlayable sourceFanIn = default;
            Playable previousOutputSource = default;
            try
            {
                workspace = new AnimationPoseNativeWorkspace(bindings);
                CharacterPoseGraphNativeBinding initialFrame = workspace.BeginFrame(m_CompletionIdentity);
                poseProgram = new CharacterPoseGraphNativeProgram(projection.PosePlan, projection.Rig);
                inertializationProgram = new PoseInertializationNativeProgram(
                    projection.PosePlan,
                    projection.BlendCurveCatalog,
                    projection.BlendProfileCatalog);
                physicalSources = new AnimationPoseSourcePhysicalRegistry(bindings.WorkspaceLayout.SourceCapacity);
                sourceBackend = new AnimancerPoseSamplingBackend(animancer, rigBinding, projection.Rig);
                stacks = new AnimationBlendStackRuntime[projection.PosePlan.BlendNodes.Count];
                for (int stackIndex = 0; stackIndex < stacks.Length; stackIndex++)
                {
                    AnimationBlendNodePayload blendNode = projection.PosePlan.BlendNodes[stackIndex] ??
                        throw new InvalidOperationException($"Pose Plan Blend Stack #{stackIndex} is missing.");
                    CharacterPresentationPoseOperation operation = RequireBlendStackOperation(
                        projection.PosePlan,
                        stackIndex,
                        blendNode.NodeId);
                    CharacterPresentationSelectionInputEntry input =
                        projection.PosePlan.SelectionInputs[operation.SelectionInputIndex];
                    AnimationPlayerPoseNativeWriteBinding initialWrite =
                        workspace.RequirePlayerWriteBinding(operation.PlayerIndex, initialFrame.CompletionIdentity);
                    var stack = new AnimationBlendStackRuntime(
                        blendNode,
                        input.AnimationChannelId,
                        input.Availability,
                        projection.BlendCurveCatalog,
                        projection.BlendProfileCatalog,
                        projection.Rig,
                        in initialWrite);
                    stacks[stackIndex] = stack;
                    m_StacksByNode.Add(blendNode.NodeId, stack);
                    m_PlayerIndicesByNode.Add(blendNode.NodeId, operation.PlayerIndex);
                    IndexMarkerConsumer(projection.PosePlan, operation);
                }
                var directPlayerList = new List<AnimationSelectedPosePlayerRuntime>();
                for (int operationIndex = 0; operationIndex < projection.PosePlan.Operations.Count; operationIndex++)
                {
                    CharacterPresentationPoseOperation operation = projection.PosePlan.Operations[operationIndex];
                    if (operation.Code != CharacterPoseOperationCode.SelectedPosePlayer &&
                        operation.Code != CharacterPoseOperationCode.BlendSpacePlayer)
                        continue;
                    if ((uint)operation.SelectionInputIndex >= (uint)projection.PosePlan.SelectionInputs.Count || operation.PlayerIndex < 0)
                        throw new InvalidOperationException($"Selected Pose Player operation '{operation.NodeId}' has invalid compiled inputs.");
                    CharacterPresentationSelectionInputEntry input = projection.PosePlan.SelectionInputs[operation.SelectionInputIndex];
                    var player = new AnimationSelectedPosePlayerRuntime(
                        operation.NodeId,
                        operation.PlayerIndex,
                        input.AnimationChannelId,
                        input.Availability,
                        operation.Code == CharacterPoseOperationCode.BlendSpacePlayer,
                        projection.Rig,
                        projection.PosePlan.Parameters.Count);
                    directPlayerList.Add(player);
                    m_PlayerIndicesByNode.Add(operation.NodeId, operation.PlayerIndex);
                    IndexMarkerConsumer(projection.PosePlan, operation);
                    if (!m_DirectPlayersByChannel.TryGetValue(input.AnimationChannelId, out List<AnimationSelectedPosePlayerRuntime> consumers))
                    {
                        consumers = new List<AnimationSelectedPosePlayerRuntime>();
                        m_DirectPlayersByChannel.Add(input.AnimationChannelId, consumers);
                    }
                    consumers.Add(player);
                }
                directPlayers = directPlayerList.ToArray();
                IndexSelectionConsumers(projection.PosePlan, stacks);
                diagnosticsPublisher = new AnimationPresentationRuntimeSnapshotPublisher(
                    projection,
                    in initialFrame,
                    workspace,
                    physicalSources.Capacity);

                PlayableGraph graph = animancer.Graph.PlayableGraph;
                if (!graph.IsValid())
                    throw new InvalidOperationException("Animation Pose Graph requires a valid Animancer PlayableGraph.");
                sourceFanIn = AnimationMixerPlayable.Create(
                    graph,
                    checked(bindings.WorkspaceLayout.SourceCapacity + 1),
                    true);
                previousOutputSource = animancer.Graph.Output.GetSourcePlayable();
                animancer.Graph.InsertOutputPlayable(sourceFanIn);
                sourceFanIn.SetInputWeight(0, 1f);
                if (managesGraphClock)
                    animancer.Graph.PauseGraph();
            }
            catch
            {
                if (sourceFanIn.IsValid())
                    sourceFanIn.Destroy();
                if (stacks != null)
                {
                    for (int i = stacks.Length - 1; i >= 0; i--)
                        stacks[i]?.Dispose();
                }
                if (directPlayers != null)
                {
                    for (int i = directPlayers.Length - 1; i >= 0; i--)
                        directPlayers[i]?.Dispose();
                }
                sourceBackend?.Dispose();
                diagnosticsPublisher?.Dispose();
                physicalSources?.Dispose();
                poseProgram?.Dispose();
                inertializationProgram?.Dispose();
                workspace?.Dispose();
                throw;
            }

            m_Workspace = workspace;
            m_PosePlan = poseProgram;
            m_InertializationPlan = inertializationProgram;
            m_PhysicalSources = physicalSources;
            m_SourceBackend = sourceBackend;
            m_Stacks = stacks;
            m_DirectPlayers = directPlayers;
            m_SlotJobs = new AnimationSlotBlendJob[stacks.Length];
            m_DirectPlayerJobs = new AnimationSelectedPosePlayerJob[directPlayers.Length];
            m_DirectPhysicalSources = new AnimationPhysicalSourceIdentity[directPlayers.Length];
            m_DirectSourceIndices = new int[directPlayers.Length];
            m_FramePublisher = new ComposedAnimationPoseFramePublisher(projection.PosePlan);
            m_DiagnosticsPublisher = diagnosticsPublisher;
            m_ReleasedSources = new AnimationReleasedPoseSourceSnapshot[physicalSources.Capacity];
            m_SourceFanIn = sourceFanIn;
            m_PreviousOutputSource = previousOutputSource;
            m_ManagesGraphClock = managesGraphClock;
            m_FootPlacementWeightParameterIndex = projection.PosePlan.RequireParameterIndex(
                AnimationPoseParameterIds.FootPlacementWeight);
        }

        internal bool HasDiagnosticsSnapshot => m_DiagnosticsPublisher.HasCurrent;
        internal AnimationPresentationRuntimeSnapshot DiagnosticsSnapshot => m_DiagnosticsPublisher.Current;

        internal void SetPoseWatchInterests(Guid ownerId, IReadOnlyList<AnimationPoseWatchIdentity> interests) =>
            m_DiagnosticsPublisher.SetPoseWatchInterests(ownerId, interests);

        internal void RemovePoseWatchInterests(Guid ownerId) =>
            m_DiagnosticsPublisher.RemovePoseWatchInterests(ownerId);

        internal AnimationPresentationRuntimeSnapshot PublishDiagnostics(
            IReadOnlyList<AnimationPlaybackLifecycleSnapshot> lifecycle,
            BlendSpaceAnimationPoseRequestResolver blendSpaces) =>
            m_DiagnosticsPublisher.Publish(lifecycle, m_ReleasedSources, m_ReleasedSourceCount, blendSpaces);

        internal void CollectPlayerNodes(
            AnimationChannelId channelId,
            AnimationPoseSourceKind sourceKind,
            List<PoseNodeId> destination)
        {
            RequireAlive();
            if (!channelId.IsValid || !Enum.IsDefined(typeof(AnimationPoseSourceKind), sourceKind) || destination == null)
                throw new ArgumentException("Animation Player node query is invalid.");
            destination.Clear();
            if (sourceKind != AnimationPoseSourceKind.BlendSpace &&
                m_StacksByChannel.TryGetValue(channelId, out List<AnimationBlendStackRuntime> stacks))
            {
                for (int i = 0; i < stacks.Count; i++)
                    destination.Add(stacks[i].PoseNodeId);
            }
            if (m_DirectPlayersByChannel.TryGetValue(channelId, out List<AnimationSelectedPosePlayerRuntime> players))
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i].Accepts(sourceKind))
                        destination.Add(players[i].NodeId);
                }
            }
            destination.Sort((left, right) => left.CompareTo(right));
        }

        internal bool TryGetMarkerNode(PoseNodeId playerNodeId, out PoseNodeId markerNodeId)
        {
            RequireAlive();
            return m_MarkerNodesByPlayer.TryGetValue(playerNodeId, out markerNodeId);
        }

        internal void CollectRetainedPlaybackDemand(HashSet<AnimationPlaybackId> destination)
        {
            RequireAlive();
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            for (int stackIndex = 0; stackIndex < m_Stacks.Length; stackIndex++)
            {
                AnimationBlendStackRuntime stack = m_Stacks[stackIndex];
                for (int entryIndex = 0; entryIndex < stack.EntryCount; entryIndex++)
                {
                    AnimationBlendEntryId entry = stack.GetEntryId(entryIndex);
                    if (!entry.EmptyTarget)
                        destination.Add(entry.SourceId.PlaybackId);
                }
            }
            for (int playerIndex = 0; playerIndex < m_DirectPlayers.Length; playerIndex++)
            {
                AnimationSelectedPosePlayerRuntime player = m_DirectPlayers[playerIndex];
                if (player.HasSelection)
                    destination.Add(player.SourceId.PlaybackId);
            }
        }

        internal void CollectRetainedSourceUsages(
            List<PlayerSourceUsageFrame> destination,
            ulong completionIdentity)
        {
            RequireAlive();
            if (destination == null || completionIdentity == 0)
                throw new ArgumentException("Player source usage query is invalid.");
            for (int stackIndex = 0; stackIndex < m_Stacks.Length; stackIndex++)
            {
                AnimationBlendStackRuntime stack = m_Stacks[stackIndex];
                for (int entryIndex = 0; entryIndex < stack.EntryCount; entryIndex++)
                {
                    AnimationBlendEntryId entry = stack.GetEntryId(entryIndex);
                    if (!entry.EmptyTarget)
                    {
                        destination.Add(new PlayerSourceUsageFrame(
                            stack.PoseNodeId,
                            entry.SourceId,
                            PlayerSourceUsageKind.Retained,
                            completionIdentity));
                    }
                }
            }
            for (int playerIndex = 0; playerIndex < m_DirectPlayers.Length; playerIndex++)
            {
                AnimationSelectedPosePlayerRuntime player = m_DirectPlayers[playerIndex];
                if (player.HasSelection)
                {
                    destination.Add(new PlayerSourceUsageFrame(
                        player.NodeId,
                        player.SourceId,
                        PlayerSourceUsageKind.Retained,
                        completionIdentity));
                }
            }
        }

        internal void AppendReleasedSourceUsages(List<PlayerSourceUsageFrame> destination)
        {
            RequireAlive();
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            for (int i = 0; i < m_ReleasedSourceCount; i++)
            {
                AnimationReleasedPoseSourceSnapshot released = m_ReleasedSources[i];
                destination.Add(new PlayerSourceUsageFrame(
                    released.PoseNodeId,
                    released.SourceId,
                    PlayerSourceUsageKind.Release,
                    released.CompletionIdentity));
            }
        }

        internal bool RetainsPlayback(AnimationPlaybackId playbackId)
        {
            RequireAlive();
            if (!playbackId.IsValid)
                return false;
            for (int stackIndex = 0; stackIndex < m_Stacks.Length; stackIndex++)
            {
                AnimationBlendStackRuntime stack = m_Stacks[stackIndex];
                for (int entryIndex = 0; entryIndex < stack.EntryCount; entryIndex++)
                {
                    AnimationBlendEntryId entry = stack.GetEntryId(entryIndex);
                    if (!entry.EmptyTarget && entry.SourceId.PlaybackId.Equals(playbackId))
                        return true;
                }
            }
            for (int playerIndex = 0; playerIndex < m_DirectPlayers.Length; playerIndex++)
            {
                AnimationSelectedPosePlayerRuntime player = m_DirectPlayers[playerIndex];
                if (player.HasSelection && player.SourceId.PlaybackId.Equals(playbackId))
                    return true;
            }
            return false;
        }

        internal bool RetainsSource(AnimationPoseSourceId sourceId)
        {
            RequireAlive();
            if (!sourceId.IsValid)
                return false;
            for (int stackIndex = 0; stackIndex < m_Stacks.Length; stackIndex++)
            {
                AnimationBlendStackRuntime stack = m_Stacks[stackIndex];
                for (int entryIndex = 0; entryIndex < stack.EntryCount; entryIndex++)
                {
                    AnimationBlendEntryId entry = stack.GetEntryId(entryIndex);
                    if (!entry.EmptyTarget && entry.SourceId.Equals(sourceId))
                        return true;
                }
            }
            for (int playerIndex = 0; playerIndex < m_DirectPlayers.Length; playerIndex++)
            {
                AnimationSelectedPosePlayerRuntime player = m_DirectPlayers[playerIndex];
                if (player.HasSelection && player.SourceId.Equals(sourceId))
                    return true;
            }
            return false;
        }

        internal bool TryGetPlaybackStatus(
            AnimationPlaybackId playbackId,
            out PoseNodeId playerNodeId,
            out AnimationPoseAvailability availability,
            out float outputWeight)
        {
            RequireAlive();
            for (int stackIndex = 0; stackIndex < m_Stacks.Length; stackIndex++)
            {
                AnimationBlendStackRuntime stack = m_Stacks[stackIndex];
                if (!stack.HasCompletedFrame)
                    continue;
                for (int entryIndex = 0; entryIndex < stack.EntryCount; entryIndex++)
                {
                    AnimationBlendEntryId entry = stack.GetEntryId(entryIndex);
                    if (entry.EmptyTarget || !entry.SourceId.PlaybackId.Equals(playbackId))
                        continue;
                    playerNodeId = stack.PoseNodeId;
                    availability = stack.LastAvailability;
                    outputWeight = stack.LastOutputWeight;
                    return true;
                }
            }
            for (int playerIndex = 0; playerIndex < m_DirectPlayers.Length; playerIndex++)
            {
                AnimationSelectedPosePlayerRuntime player = m_DirectPlayers[playerIndex];
                if (!player.HasCompletedFrame || !player.HasSelection ||
                    !player.SourceId.PlaybackId.Equals(playbackId))
                    continue;
                playerNodeId = player.NodeId;
                availability = player.LastAvailability;
                outputWeight = player.LastOutputWeight;
                return true;
            }
            playerNodeId = default;
            availability = AnimationPoseAvailability.Invalid;
            outputWeight = 0f;
            return false;
        }

        internal bool TryGetHandoffSource(
            PoseNodeId playerNodeId,
            AnimationPoseSourceId incoming,
            out AnimationPoseSourceId outgoing)
        {
            RequireAlive();
            if (!playerNodeId.IsValid || !incoming.IsValid)
                throw new ArgumentException("Player handoff source query is invalid.");
            if (m_StacksByNode.TryGetValue(playerNodeId, out AnimationBlendStackRuntime stack))
            {
                for (int i = stack.EntryCount - 1; i >= 0; i--)
                {
                    AnimationBlendEntryId entry = stack.GetEntryId(i);
                    if (!entry.EmptyTarget && !entry.SourceId.Equals(incoming))
                    {
                        outgoing = entry.SourceId;
                        return true;
                    }
                }
            }
            for (int i = 0; i < m_DirectPlayers.Length; i++)
            {
                AnimationSelectedPosePlayerRuntime player = m_DirectPlayers[i];
                if (player.NodeId == playerNodeId && player.HasSelection &&
                    !player.SourceId.Equals(incoming))
                {
                    outgoing = player.SourceId;
                    return true;
                }
            }
            outgoing = default;
            return false;
        }

        internal void PublishSelection(in AnimationSelectionFrame selection)
        {
            RequireAlive();
            if (!selection.IsValid)
                throw new InvalidOperationException("Animation selection is invalid.");
            bool consumed = false;
            if (m_StacksByChannel.TryGetValue(selection.AnimationChannelId, out List<AnimationBlendStackRuntime> stacks))
            {
                for (int i = 0; i < stacks.Count; i++)
                    stacks[i].PushPoseRequest(in selection);
                consumed = true;
            }
            if (m_DirectPlayersByChannel.TryGetValue(selection.AnimationChannelId, out List<AnimationSelectedPosePlayerRuntime> players))
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (!players[i].Accepts(selection.SourceId.SourceKind))
                        continue;
                    players[i].PushSelection(in selection);
                    consumed = true;
                }
            }
            if (!consumed)
                throw new InvalidOperationException($"Animation Channel '{selection.AnimationChannelId}' has no explicit Player consumer.");
        }

        internal void PublishEmptySelection(AnimationChannelId channelId, ulong presentationRequestSequence)
        {
            RequireAlive();
            if (!channelId.IsValid || presentationRequestSequence == 0)
                throw new InvalidOperationException("Empty animation selection is invalid.");
            bool consumed = false;
            if (m_StacksByChannel.TryGetValue(channelId, out List<AnimationBlendStackRuntime> stacks))
            {
                for (int i = 0; i < stacks.Count; i++)
                    stacks[i].PushEmpty(presentationRequestSequence);
                consumed = true;
            }
            if (m_DirectPlayersByChannel.TryGetValue(channelId, out List<AnimationSelectedPosePlayerRuntime> players))
            {
                for (int i = 0; i < players.Count; i++)
                    players[i].PushEmpty();
                consumed = true;
            }
            if (!consumed)
                throw new InvalidOperationException($"Animation Channel '{channelId}' has no explicit Player consumer.");
        }

        internal void PublishUnavailableSelection(AnimationChannelId channelId, AnimationPlaybackId playbackId)
        {
            RequireAlive();
            if (!channelId.IsValid || !playbackId.IsValid)
                throw new InvalidOperationException("Unavailable animation selection is invalid.");
            bool consumed = false;
            if (m_StacksByChannel.TryGetValue(channelId, out List<AnimationBlendStackRuntime> stacks))
            {
                for (int i = 0; i < stacks.Count; i++)
                    stacks[i].PushUnavailable(playbackId);
                consumed = true;
            }
            if (m_DirectPlayersByChannel.TryGetValue(channelId, out List<AnimationSelectedPosePlayerRuntime> players))
            {
                for (int i = 0; i < players.Count; i++)
                    players[i].PushUnavailable(playbackId);
                consumed = true;
            }
            if (!consumed)
                throw new InvalidOperationException($"Required Animation Channel '{channelId}' has no explicit Player consumer for unavailable selection.");
        }

        internal void Advance(float presentationDeltaSeconds)
        {
            RequireAlive();
            if (!float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(presentationDeltaSeconds));
            for (int i = 0; i < m_Stacks.Length; i++)
                m_Stacks[i].Advance(presentationDeltaSeconds);
        }

        internal ComposedAnimationPoseFrame Evaluate(
            float presentationDeltaSeconds,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey, AnimationSourcePoseSample> sourceSamples)
        {
            RequireAlive();
            if (!float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(presentationDeltaSeconds));
            if (sourceSamples == null)
                throw new ArgumentNullException(nameof(sourceSamples));

            ulong completionIdentity = NextCompletionIdentity();
            m_ReleasedSourceCount = 0;
            ReleaseDirectSources(true);
            CharacterPoseGraphNativeBinding frame = m_Workspace.BeginFrame(completionIdentity);
            for (int i = 0; i < m_Stacks.Length; i++)
                m_Stacks[i].BeginSourceFrame(completionIdentity);
            for (int i = 0; i < m_DirectPlayers.Length; i++)
            {
                m_DirectPlayers[i].BeginFrame(completionIdentity);
                m_DirectPhysicalSources[i] = default;
                m_DirectSourceIndices[i] = -1;
            }

            for (int stackIndex = 0; stackIndex < m_Stacks.Length; stackIndex++)
                PrepareStackSources(m_Stacks[stackIndex], presentationDeltaSeconds, sourceSamples);
            for (int playerIndex = 0; playerIndex < m_DirectPlayers.Length; playerIndex++)
                PrepareDirectSource(playerIndex, presentationDeltaSeconds, sourceSamples);

            for (int slotIndex = 0; slotIndex < m_Stacks.Length; slotIndex++)
            {
                AnimationBlendStackRuntime stack = m_Stacks[slotIndex];
                AnimationPlayerPoseNativeWriteBinding write =
                    m_Workspace.RequirePlayerWriteBinding(stack.PlayerIndex, completionIdentity);
                m_SlotJobs[slotIndex] = stack.PrepareSlotJob(
                    completionIdentity,
                    in write,
                    m_PhysicalSources);
            }
            for (int playerIndex = 0; playerIndex < m_DirectPlayers.Length; playerIndex++)
            {
                AnimationSelectedPosePlayerRuntime player = m_DirectPlayers[playerIndex];
                AnimationPlayerPoseNativeWriteBinding write =
                    m_Workspace.RequirePlayerWriteBinding(player.PlayerIndex, completionIdentity);
                m_DirectPlayerJobs[playerIndex] = player.PrepareJob(
                    completionIdentity,
                    in write,
                    m_DirectPhysicalSources[playerIndex],
                    m_DirectSourceIndices[playerIndex]);
            }
            var poseJob = new CharacterPoseGraphNativeJob(
                m_PosePlan,
                m_InertializationPlan,
                m_Workspace.RequirePoseGraphBinding(completionIdentity));
            AnimationFinalPoseNativeReadBinding finalRead =
                m_Workspace.RequireFinalReadBinding(completionIdentity);
            CharacterAnimationRigPayload rig = m_Bindings.Projection.Rig;
            var finalWriter = new AnimationFinalPoseStreamWriterJob(
                finalRead,
                m_SourceBackend.Handles,
                rig.RootBoneIndex,
                rig.RootBonePolicy);
            InstallOrUpdateJobs(poseJob, finalWriter);

            m_Animancer.Evaluate(presentationDeltaSeconds);
            m_SourceBackend.ApplyRootPolicy();
            for (int i = 0; i < m_Stacks.Length; i++)
                m_Stacks[i].CompleteFrame(completionIdentity);
            for (int i = 0; i < m_DirectPlayers.Length; i++)
                m_DirectPlayers[i].CompleteFrame();
            ComposedAnimationPoseFrame result = m_FramePublisher.Publish(in finalRead, m_PhysicalSources);
            m_LastCompletedFrame = frame;
            m_HasCompletedFrame = true;
            m_DiagnosticsPublisher.BeginFrame(
                in frame,
                in finalRead,
                m_Stacks,
                m_InertializationPlan,
                m_PhysicalSources);
            ReleaseCompletedSources(completionIdentity, true);
            return result;
        }

        internal bool TryCopyPlayerPose(
            PoseNodeId playerNodeId,
            int[] rigBoneIndices,
            Vector3[] positions,
            out AnimationFootPlacementSample footPlacement)
        {
            RequireAlive();
            if (!playerNodeId.IsValid || rigBoneIndices == null || positions == null ||
                rigBoneIndices.Length == 0 || positions.Length != rigBoneIndices.Length)
                throw new ArgumentException("Animation Player history copy input is invalid.");
            if (!m_HasCompletedFrame || !m_PlayerIndicesByNode.TryGetValue(playerNodeId, out int playerIndex))
            {
                footPlacement = default;
                return false;
            }
            var read = new AnimationPlayerPoseNativeWriteBinding(in m_LastCompletedFrame, playerIndex);
            if (read.CompletedAt[0] != m_LastCompletedFrame.CompletionIdentity ||
                read.Availability[0] != AnimationPoseAvailability.Pose || read.HasFootFeatures[0] == 0 ||
                read.PoseParameterAvailability[m_FootPlacementWeightParameterIndex] == 0)
            {
                footPlacement = default;
                return false;
            }
            for (int i = 0; i < rigBoneIndices.Length; i++)
            {
                int boneIndex = rigBoneIndices[i];
                if ((uint)boneIndex >= (uint)read.DenseLocalPoses.Length)
                    throw new InvalidOperationException("Motion Matching history Bone index is outside the completed Player pose.");
                positions[i] = read.DenseLocalPoses[boneIndex].Position;
            }
            footPlacement = new AnimationFootPlacementSample(
                read.PoseParameters[m_FootPlacementWeightParameterIndex],
                read.LeftFootFeatures[0],
                read.RightFootFeatures[0]);
            return true;
        }

        internal void Reset(PoseDiscontinuityResetReason reason)
        {
            RequireAlive();
            if (reason == PoseDiscontinuityResetReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            m_FramePublisher.Invalidate();
            m_DiagnosticsPublisher.Invalidate();
            m_ReleasedSourceCount = 0;
            m_LastCompletedFrame = default;
            m_HasCompletedFrame = false;
            m_InertializationPlan.Reset();
            ulong completionIdentity = NextCompletionIdentity();
            for (int i = 0; i < m_Stacks.Length; i++)
                m_Stacks[i].Reset(completionIdentity);
            ReleaseCompletedSources(completionIdentity, false);
            for (int i = 0; i < m_DirectPlayers.Length; i++)
                m_DirectPlayers[i].Reset(reason);
            m_SourceBackend.Clear();
            m_PhysicalSources.Reset();
            for (int port = 1; port < m_SourceFanIn.GetInputCount(); port++)
            {
                if (m_SourceFanIn.GetInput(port).IsValid())
                    m_SourceFanIn.DisconnectInput(port);
                m_SourceFanIn.SetInputWeight(port, 0f);
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_FramePublisher.Invalidate();
            m_LastCompletedFrame = default;
            m_HasCompletedFrame = false;
            Exception failure = null;
            DisposeStep(m_DiagnosticsPublisher.Dispose, ref failure);
            DisposeStep(RemoveJobs, ref failure);
            DisposeStep(m_SourceBackend.Dispose, ref failure);
            DisposeStep(RestoreOutputAndDestroyFanIn, ref failure);
            for (int i = m_Stacks.Length - 1; i >= 0; i--)
            {
                AnimationBlendStackRuntime stack = m_Stacks[i];
                if (stack != null)
                    DisposeStep(stack.Dispose, ref failure);
            }
            for (int i = m_DirectPlayers.Length - 1; i >= 0; i--)
            {
                AnimationSelectedPosePlayerRuntime player = m_DirectPlayers[i];
                if (player != null)
                    DisposeStep(player.Dispose, ref failure);
            }
            DisposeStep(m_PhysicalSources.Dispose, ref failure);
            DisposeStep(m_PosePlan.Dispose, ref failure);
            DisposeStep(m_InertializationPlan.Dispose, ref failure);
            DisposeStep(m_Workspace.Dispose, ref failure);
            DisposeStep(RestoreGraphClock, ref failure);
            if (failure != null)
                throw failure;
        }

        void ConnectSource(
            AnimationPhysicalSourceIdentity physical,
            AnimationPoseSourcePrepareResult prepared)
        {
            int port = checked(physical.Index.Value + 1);
            Playable current = m_SourceFanIn.GetInput(port);
            if (current.IsValid() && current.Equals(prepared.Output))
            {
                m_SourceFanIn.SetInputWeight(port, 1f);
                return;
            }
            if (current.IsValid())
                m_SourceFanIn.DisconnectInput(port);
            m_SourceFanIn.GetGraph().Connect(prepared.Output, 0, m_SourceFanIn, port);
            m_SourceFanIn.SetInputWeight(port, 1f);
        }

        void PrepareStackSources(
            AnimationBlendStackRuntime stack,
            float presentationDeltaSeconds,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey, AnimationSourcePoseSample> sourceSamples)
        {
            if (!stack.HasCurrentSelectionSample)
                return;
            for (int entryIndex = 0; entryIndex < stack.EntryCount; entryIndex++)
            {
                AnimationBlendEntryId entry = stack.GetEntryId(entryIndex);
                if (entry.EmptyTarget || HasEarlierSource(stack, entryIndex, entry.SourceId))
                    continue;
                PrepareSource(stack, entry.SourceId, presentationDeltaSeconds, sourceSamples);
            }
        }

        void PrepareSource(
            AnimationBlendStackRuntime stack,
            AnimationPoseSourceId sourceId,
            float presentationDeltaSeconds,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey, AnimationSourcePoseSample> sourceSamples)
        {
            var key = new AnimationPlayerSourceSampleKey(stack.PoseNodeId, sourceId);
            if (!sourceSamples.TryGetValue(key, out AnimationSourcePoseSample sourceSample))
                throw new InvalidOperationException($"Animation Pose Source '{sourceId}' has no current resolved request.");
            AnimationSelectionFrame request = sourceSample.Selection;
            AnimationPhysicalSourceIdentity physical = m_PhysicalSources.Register(
                request.SourceId,
                stack.PoseNodeId,
                request.ProgramProducerIndex);
            AnimationPoseSourceCaptureBinding capture = stack.PrepareCapture(
                in sourceSample,
                presentationDeltaSeconds);
            AnimationPoseSourcePrepareResult prepared = m_SourceBackend.PrepareOrUpdate(
                in request,
                in capture,
                stack.PoseNodeId);
            ConnectSource(physical, prepared);
        }

        void PrepareDirectSource(
            int playerIndex,
            float presentationDeltaSeconds,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey, AnimationSourcePoseSample> sourceSamples)
        {
            AnimationSelectedPosePlayerRuntime player = m_DirectPlayers[playerIndex];
            if (!player.HasCurrentSample)
                return;
            var key = new AnimationPlayerSourceSampleKey(player.NodeId, player.SourceId);
            if (!sourceSamples.TryGetValue(key, out AnimationSourcePoseSample sample))
                throw new InvalidOperationException($"Animation Pose Source '{player.SourceId}' has no current resolved request.");
            AnimationSelectionFrame selection = sample.Selection;
            AnimationPhysicalSourceIdentity physical = m_PhysicalSources.Register(
                selection.SourceId,
                player.NodeId,
                selection.ProgramProducerIndex);
            AnimationPoseSourceCaptureBinding capture = player.PrepareCapture(in sample, presentationDeltaSeconds);
            AnimationPoseSourcePrepareResult prepared = m_SourceBackend.PrepareOrUpdate(
                in selection,
                in capture,
                player.NodeId);
            ConnectSource(physical, prepared);
            m_DirectPhysicalSources[playerIndex] = physical;
            m_DirectSourceIndices[playerIndex] = capture.SourceIndex;
        }

        static bool HasEarlierSource(
            AnimationBlendStackRuntime stack,
            int entryIndex,
            AnimationPoseSourceId sourceId)
        {
            for (int i = 0; i < entryIndex; i++)
            {
                AnimationBlendEntryId candidate = stack.GetEntryId(i);
                if (!candidate.EmptyTarget && candidate.SourceId.Equals(sourceId))
                    return true;
            }
            return false;
        }

        void InstallOrUpdateJobs(
            CharacterPoseGraphNativeJob poseJob,
            AnimationFinalPoseStreamWriterJob finalWriter)
        {
            if (!m_JobsInstalled)
            {
                m_DirectPlayerPlayables = new AnimationScriptPlayable[m_DirectPlayerJobs.Length];
                for (int i = 0; i < m_DirectPlayerJobs.Length; i++)
                {
                    m_DirectPlayerPlayables[i] = m_Animancer.Graph.InsertOutputJob(m_DirectPlayerJobs[i]);
                    m_DirectPlayerPlayables[i].SetProcessInputs(true);
                }
                m_SlotPlayables = new AnimationScriptPlayable[m_SlotJobs.Length];
                for (int i = 0; i < m_SlotJobs.Length; i++)
                {
                    m_SlotPlayables[i] = m_Animancer.Graph.InsertOutputJob(m_SlotJobs[i]);
                    m_SlotPlayables[i].SetProcessInputs(true);
                }
                m_PoseGraphPlayable = m_Animancer.Graph.InsertOutputJob(poseJob);
                m_PoseGraphPlayable.SetProcessInputs(true);
                m_FinalWriterPlayable = m_Animancer.Graph.InsertOutputJob(finalWriter);
                m_FinalWriterPlayable.SetProcessInputs(true);
                m_JobsInstalled = true;
                return;
            }
            for (int i = 0; i < m_DirectPlayerJobs.Length; i++)
                m_DirectPlayerPlayables[i].SetJobData(m_DirectPlayerJobs[i]);
            for (int i = 0; i < m_SlotJobs.Length; i++)
                m_SlotPlayables[i].SetJobData(m_SlotJobs[i]);
            m_PoseGraphPlayable.SetJobData(poseJob);
            m_FinalWriterPlayable.SetJobData(finalWriter);
        }

        void ReleaseCompletedSources(ulong completionIdentity, bool recordDiagnostics)
        {
            for (int stackIndex = 0; stackIndex < m_Stacks.Length; stackIndex++)
            {
                AnimationBlendStackRuntime stack = m_Stacks[stackIndex];
                while (stack.TryDequeueStackRelease(out AnimationBlendStackRelease release))
                {
                    if (release.CompletionIdentity != completionIdentity)
                        throw new InvalidOperationException("Animation Blend Stack source release does not match the exact completed frame.");
                    if (recordDiagnostics)
                    {
                        if (m_ReleasedSourceCount >= m_ReleasedSources.Length)
                            throw new InvalidOperationException("Animation diagnostics release capacity was exceeded.");
                        m_ReleasedSources[m_ReleasedSourceCount++] = new AnimationReleasedPoseSourceSnapshot(
                            release.PoseNodeId,
                            release.SourceId,
                            release.CompletionIdentity);
                    }
                    AnimationPhysicalSourceIdentity physical = m_PhysicalSources.RequireIdentity(release.SourceId, release.PoseNodeId);
                    int port = checked(physical.Index.Value + 1);
                    if (m_SourceFanIn.GetInput(port).IsValid())
                        m_SourceFanIn.DisconnectInput(port);
                    m_SourceFanIn.SetInputWeight(port, 0f);
                    m_SourceBackend.Release(release.SourceId, release.PoseNodeId);
                    stack.ReleaseSource(release.SourceId);
                    m_PhysicalSources.Release(physical, release.SourceId);
                }
            }
        }

        void RemoveJobs()
        {
            if (!m_JobsInstalled || !m_Animancer || !m_Animancer.IsGraphInitialized)
                return;
            AnimancerUtilities.RemovePlayable(m_FinalWriterPlayable);
            AnimancerUtilities.RemovePlayable(m_PoseGraphPlayable);
            for (int i = m_SlotPlayables.Length - 1; i >= 0; i--)
                AnimancerUtilities.RemovePlayable(m_SlotPlayables[i]);
            for (int i = m_DirectPlayerPlayables.Length - 1; i >= 0; i--)
                AnimancerUtilities.RemovePlayable(m_DirectPlayerPlayables[i]);
            m_JobsInstalled = false;
        }

        void RestoreOutputAndDestroyFanIn()
        {
            if (!m_SourceFanIn.IsValid() || !m_Animancer || !m_Animancer.IsGraphInitialized)
                return;
            PlayableOutput output = m_Animancer.Graph.Output;
            if (output.IsOutputValid() && output.GetSourcePlayable().Equals(m_SourceFanIn))
                output.SetSourcePlayable(m_PreviousOutputSource);
            m_SourceFanIn.Destroy();
        }

        ulong NextCompletionIdentity()
        {
            if (m_CompletionIdentity == ulong.MaxValue)
                throw new InvalidOperationException("Animation Pose completion identity was exhausted.");
            m_CompletionIdentity++;
            return m_CompletionIdentity;
        }

        void RestoreGraphClock()
        {
            if (m_ManagesGraphClock && m_Animancer && m_Animancer.IsGraphInitialized)
                m_Animancer.Graph.UnpauseGraph();
        }

        static void DisposeStep(Action action, ref Exception failure)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                if (failure == null)
                    failure = exception;
            }
        }

        void IndexSelectionConsumers(
            CharacterPresentationPosePlan plan,
            IReadOnlyList<AnimationBlendStackRuntime> stacks)
        {
            for (int operationIndex = 0; operationIndex < plan.Operations.Count; operationIndex++)
            {
                CharacterPresentationPoseOperation operation = plan.Operations[operationIndex];
                if (operation.Code != CharacterPoseOperationCode.BlendStack)
                    continue;
                if ((uint)operation.BlendNodeIndex >= (uint)stacks.Count ||
                    (uint)operation.SelectionInputIndex >= (uint)plan.SelectionInputs.Count)
                    throw new InvalidOperationException($"Blend Stack operation '{operation.NodeId}' has invalid compiled inputs.");
                AnimationBlendStackRuntime stack = stacks[operation.BlendNodeIndex];
                if (stack.PoseNodeId != operation.NodeId)
                    throw new InvalidOperationException($"Blend Stack operation '{operation.NodeId}' does not match its payload.");
                AnimationChannelId channelId = plan.SelectionInputs[operation.SelectionInputIndex].AnimationChannelId;
                if (!m_StacksByChannel.TryGetValue(channelId, out List<AnimationBlendStackRuntime> consumers))
                {
                    consumers = new List<AnimationBlendStackRuntime>();
                    m_StacksByChannel.Add(channelId, consumers);
                }
                consumers.Add(stack);
            }
        }

        void IndexMarkerConsumer(CharacterPresentationPosePlan plan, CharacterPresentationPoseOperation player)
        {
            if (player.MarkerSyncOperationIndex < 0)
                return;
            CharacterPresentationPoseOperation marker = plan.Operations[player.MarkerSyncOperationIndex];
            if (marker.Code != CharacterPoseOperationCode.MarkerSync ||
                marker.SelectionInputIndex != player.SelectionInputIndex ||
                !m_MarkerNodesByPlayer.TryAdd(player.NodeId, marker.NodeId))
                throw new InvalidOperationException($"Player '{player.NodeId}' has an invalid Marker Sync binding.");
        }

        void ReleaseDirectSources(bool recordDiagnostics)
        {
            for (int playerIndex = 0; playerIndex < m_DirectPlayers.Length; playerIndex++)
            {
                AnimationSelectedPosePlayerRuntime player = m_DirectPlayers[playerIndex];
                while (player.TryDequeueRelease(out AnimationPoseSourceId sourceId))
                {
                    AnimationPhysicalSourceIdentity physical = m_PhysicalSources.RequireIdentity(sourceId, player.NodeId);
                    int port = checked(physical.Index.Value + 1);
                    if (m_SourceFanIn.GetInput(port).IsValid())
                        m_SourceFanIn.DisconnectInput(port);
                    m_SourceFanIn.SetInputWeight(port, 0f);
                    if (recordDiagnostics)
                    {
                        if (m_ReleasedSourceCount >= m_ReleasedSources.Length)
                            throw new InvalidOperationException("Animation diagnostics release capacity was exceeded.");
                        m_ReleasedSources[m_ReleasedSourceCount++] = new AnimationReleasedPoseSourceSnapshot(
                            player.NodeId,
                            sourceId,
                            m_CompletionIdentity);
                    }
                    m_SourceBackend.Release(sourceId, player.NodeId);
                    m_PhysicalSources.Release(physical, sourceId);
                }
            }
        }

        static CharacterPresentationPoseOperation RequireBlendStackOperation(
            CharacterPresentationPosePlan plan,
            int blendNodeIndex,
            PoseNodeId nodeId)
        {
            CharacterPresentationPoseOperation result = null;
            for (int i = 0; i < plan.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation candidate = plan.Operations[i];
                if (candidate.Code != CharacterPoseOperationCode.BlendStack ||
                    candidate.BlendNodeIndex != blendNodeIndex || candidate.NodeId != nodeId)
                    continue;
                if (result != null)
                    throw new InvalidOperationException($"Pose Plan duplicates Blend Stack operation '{nodeId}'.");
                result = candidate;
            }
            if (result == null || (uint)result.SelectionInputIndex >= (uint)plan.SelectionInputs.Count)
                throw new InvalidOperationException($"Pose Plan has no valid Blend Stack operation '{nodeId}'.");
            return result;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AnimationPosePlayableGraphRuntime));
        }
    }
}
