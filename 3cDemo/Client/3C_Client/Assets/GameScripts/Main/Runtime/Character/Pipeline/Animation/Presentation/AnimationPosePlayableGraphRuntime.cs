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
        readonly CharacterPoseGraphNativeProgram m_PoseProgram;
        readonly AnimationPoseSourcePhysicalRegistry m_PhysicalSources;
        readonly AnimancerPoseSamplingBackend m_SourceBackend;
        readonly FinalAnimationPoseFramePublisher m_FramePublisher;
        readonly AnimationPresentationRuntimeSnapshotPublisher m_DiagnosticsPublisher;
        readonly AnimationBlendStackRuntime[] m_Stacks;
        readonly AnimationSlotBlendJob[] m_SlotJobs;
        readonly AnimationReleasedPoseSourceSnapshot[] m_ReleasedSources;
        readonly Dictionary<AnimationChannelId, AnimationBlendStackRuntime> m_StacksByChannel =
            new Dictionary<AnimationChannelId, AnimationBlendStackRuntime>();
        readonly Dictionary<PoseSlotId, AnimationBlendStackRuntime> m_StacksBySlot =
            new Dictionary<PoseSlotId, AnimationBlendStackRuntime>();
        readonly AnimationMixerPlayable m_SourceFanIn;
        readonly Playable m_PreviousOutputSource;
        readonly bool m_ManagesGraphClock;
        readonly int m_FootPlacementWeightParameterIndex;

        AnimationScriptPlayable[] m_SlotPlayables;
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
            AnimationPoseSourcePhysicalRegistry physicalSources = null;
            AnimancerPoseSamplingBackend sourceBackend = null;
            AnimationBlendStackRuntime[] stacks = null;
            AnimationPresentationRuntimeSnapshotPublisher diagnosticsPublisher = null;
            AnimationMixerPlayable sourceFanIn = default;
            Playable previousOutputSource = default;
            try
            {
                workspace = new AnimationPoseNativeWorkspace(bindings);
                CharacterPoseGraphNativeBinding initialFrame = workspace.BeginFrame(m_CompletionIdentity);
                poseProgram = new CharacterPoseGraphNativeProgram(projection.PoseProgram, projection.Rig);
                physicalSources = new AnimationPoseSourcePhysicalRegistry(bindings.WorkspaceLayout.SourceCapacity);
                sourceBackend = new AnimancerPoseSamplingBackend(animancer, rigBinding, projection.Rig);
                stacks = new AnimationBlendStackRuntime[projection.PoseProgram.Slots.Count];
                for (int slotIndex = 0; slotIndex < stacks.Length; slotIndex++)
                {
                    ResolvedAnimationPoseSlot slot = RequireSlot(bindings, projection.PoseProgram.Slots[slotIndex]);
                    AnimationPoseSlotNativeWriteBinding initialWrite =
                        workspace.RequireSlotWriteBinding(slot.Index, initialFrame.CompletionIdentity);
                    var stack = new AnimationBlendStackRuntime(
                        slot.BlendPayload,
                        projection.BlendCurveCatalog,
                        projection.BlendProfileCatalog,
                        projection.Rig,
                        in initialWrite);
                    stacks[slotIndex] = stack;
                    m_StacksByChannel.Add(slot.AnimationChannelId, stack);
                    m_StacksBySlot.Add(slot.PoseSlotId, stack);
                }
                diagnosticsPublisher = new AnimationPresentationRuntimeSnapshotPublisher(
                    projection,
                    in initialFrame,
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
                sourceBackend?.Dispose();
                diagnosticsPublisher?.Dispose();
                physicalSources?.Dispose();
                poseProgram?.Dispose();
                workspace?.Dispose();
                throw;
            }

            m_Workspace = workspace;
            m_PoseProgram = poseProgram;
            m_PhysicalSources = physicalSources;
            m_SourceBackend = sourceBackend;
            m_Stacks = stacks;
            m_SlotJobs = new AnimationSlotBlendJob[stacks.Length];
            m_FramePublisher = new FinalAnimationPoseFramePublisher(projection.PoseProgram);
            m_DiagnosticsPublisher = diagnosticsPublisher;
            m_ReleasedSources = new AnimationReleasedPoseSourceSnapshot[physicalSources.Capacity];
            m_SourceFanIn = sourceFanIn;
            m_PreviousOutputSource = previousOutputSource;
            m_ManagesGraphClock = managesGraphClock;
            m_FootPlacementWeightParameterIndex = projection.PoseProgram.RequireParameterIndex(
                AnimationPoseParameterIds.FootPlacementWeight);
        }

        internal IReadOnlyList<AnimationBlendStackRuntime> Stacks => m_Stacks;
        internal bool HasDiagnosticsSnapshot => m_DiagnosticsPublisher.HasCurrent;
        internal AnimationPresentationRuntimeSnapshot DiagnosticsSnapshot => m_DiagnosticsPublisher.Current;

        internal AnimationPresentationRuntimeSnapshot PublishDiagnostics(
            IReadOnlyList<AnimationPlaybackLifecycleSnapshot> lifecycle) =>
            m_DiagnosticsPublisher.Publish(lifecycle, m_ReleasedSources, m_ReleasedSourceCount);

        internal AnimationBlendStackRuntime RequireStack(AnimationChannelId channelId)
        {
            RequireAlive();
            return m_StacksByChannel.TryGetValue(channelId, out AnimationBlendStackRuntime stack)
                ? stack
                : throw new KeyNotFoundException($"Animation Channel '{channelId}' has no Pose Slot Blend Stack.");
        }

        internal void PushPoseRequest(in ResolvedAnimationPoseRequest request) =>
            RequireStack(request.AnimationChannelId).PushPoseRequest(in request);

        internal void PushEmpty(AnimationChannelId channelId, ulong presentationRequestSequence) =>
            RequireStack(channelId).PushEmpty(presentationRequestSequence);

        internal void Advance(float presentationDeltaSeconds)
        {
            RequireAlive();
            if (!float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(presentationDeltaSeconds));
            for (int i = 0; i < m_Stacks.Length; i++)
                m_Stacks[i].Advance(presentationDeltaSeconds);
        }

        internal FinalAnimationPoseFrame Evaluate(
            float presentationDeltaSeconds,
            IReadOnlyDictionary<AnimationPoseSourceId, ResolvedAnimationPoseRequest> requests)
        {
            RequireAlive();
            if (!float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(presentationDeltaSeconds));
            if (requests == null)
                throw new ArgumentNullException(nameof(requests));

            ulong completionIdentity = NextCompletionIdentity();
            CharacterPoseGraphNativeBinding frame = m_Workspace.BeginFrame(completionIdentity);
            for (int i = 0; i < m_Stacks.Length; i++)
                m_Stacks[i].BeginSourceFrame(completionIdentity);

            for (int stackIndex = 0; stackIndex < m_Stacks.Length; stackIndex++)
                PrepareStackSources(m_Stacks[stackIndex], presentationDeltaSeconds, requests);

            for (int slotIndex = 0; slotIndex < m_Stacks.Length; slotIndex++)
            {
                AnimationPoseSlotNativeWriteBinding write =
                    m_Workspace.RequireSlotWriteBinding(slotIndex, completionIdentity);
                m_SlotJobs[slotIndex] = m_Stacks[slotIndex].PrepareSlotJob(
                    completionIdentity,
                    in write,
                    m_PhysicalSources);
            }
            var poseJob = new CharacterPoseGraphNativeJob(
                m_PoseProgram,
                m_Workspace.RequirePoseGraphBinding(completionIdentity));
            AnimationFinalPoseNativeReadBinding finalRead =
                m_Workspace.RequireFinalReadBinding(completionIdentity);
            var finalWriter = new AnimationFinalPoseStreamWriterJob(finalRead, m_SourceBackend.Handles);
            InstallOrUpdateJobs(poseJob, finalWriter);

            m_Animancer.Evaluate(presentationDeltaSeconds);
            for (int i = 0; i < m_Stacks.Length; i++)
                m_Stacks[i].CompleteFrame(completionIdentity);
            FinalAnimationPoseFrame result = m_FramePublisher.Publish(in finalRead, m_PhysicalSources);
            m_LastCompletedFrame = frame;
            m_HasCompletedFrame = true;
            m_DiagnosticsPublisher.BeginFrame(in frame, in finalRead, m_Stacks, m_PhysicalSources);
            m_ReleasedSourceCount = 0;
            ReleaseCompletedSources(completionIdentity, true);
            return result;
        }

        internal bool TryCopySlotPose(
            PoseSlotId poseSlotId,
            int[] rigBoneIndices,
            Vector3[] positions,
            out AnimationFootPlacementSample footPlacement)
        {
            RequireAlive();
            if (!poseSlotId.IsValid || rigBoneIndices == null || positions == null ||
                rigBoneIndices.Length == 0 || positions.Length != rigBoneIndices.Length)
                throw new ArgumentException("Animation Pose Slot history copy input is invalid.");
            if (!m_HasCompletedFrame || !m_Bindings.TryGetSlot(poseSlotId, out ResolvedAnimationPoseSlot slot))
            {
                footPlacement = default;
                return false;
            }
            var read = new AnimationPoseSlotNativeWriteBinding(in m_LastCompletedFrame, slot.Index);
            if (read.CompletedAt[0] != m_LastCompletedFrame.CompletionIdentity ||
                read.Availability[0] != PoseSlotFrameAvailability.Pose || read.HasFootFeatures[0] == 0)
            {
                footPlacement = default;
                return false;
            }
            for (int i = 0; i < rigBoneIndices.Length; i++)
            {
                int boneIndex = rigBoneIndices[i];
                if ((uint)boneIndex >= (uint)read.DenseLocalPoses.Length)
                    throw new InvalidOperationException("Motion Matching history Bone index is outside the completed Pose Slot.");
                positions[i] = read.DenseLocalPoses[boneIndex].Position;
            }
            footPlacement = new AnimationFootPlacementSample(
                read.PoseParameters[m_FootPlacementWeightParameterIndex],
                read.LeftFootFeatures[0],
                read.RightFootFeatures[0]);
            return true;
        }

        internal void Reset()
        {
            RequireAlive();
            m_FramePublisher.Invalidate();
            m_DiagnosticsPublisher.Invalidate();
            m_ReleasedSourceCount = 0;
            m_LastCompletedFrame = default;
            m_HasCompletedFrame = false;
            ulong completionIdentity = NextCompletionIdentity();
            for (int i = 0; i < m_Stacks.Length; i++)
                m_Stacks[i].Reset(completionIdentity);
            ReleaseCompletedSources(completionIdentity, false);
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
            DisposeStep(m_PhysicalSources.Dispose, ref failure);
            DisposeStep(m_PoseProgram.Dispose, ref failure);
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
            IReadOnlyDictionary<AnimationPoseSourceId, ResolvedAnimationPoseRequest> requests)
        {
            for (int entryIndex = 0; entryIndex < stack.EntryCount; entryIndex++)
            {
                AnimationBlendEntryId entry = stack.GetEntryId(entryIndex);
                if (entry.EmptyTarget || HasEarlierSource(stack, entryIndex, entry.SourceId))
                    continue;
                PrepareSource(stack, entry.SourceId, presentationDeltaSeconds, requests);
            }
            if (stack.TryGetPendingInertialSourceId(out AnimationPoseSourceId pendingSource) &&
                !ContainsEntrySource(stack, pendingSource))
            {
                PrepareSource(stack, pendingSource, presentationDeltaSeconds, requests);
            }
        }

        void PrepareSource(
            AnimationBlendStackRuntime stack,
            AnimationPoseSourceId sourceId,
            float presentationDeltaSeconds,
            IReadOnlyDictionary<AnimationPoseSourceId, ResolvedAnimationPoseRequest> requests)
        {
            if (!requests.TryGetValue(sourceId, out ResolvedAnimationPoseRequest request))
                throw new InvalidOperationException($"Animation Pose Source '{sourceId}' has no current resolved request.");
            AnimationPhysicalSourceIdentity physical = m_PhysicalSources.Register(
                request.SourceId,
                request.PoseSlotId,
                request.ProgramProducerIndex);
            AnimationPoseSourceCaptureBinding capture = stack.PrepareCapture(
                in request,
                presentationDeltaSeconds);
            AnimationPoseSourcePrepareResult prepared = m_SourceBackend.PrepareOrUpdate(
                in request,
                in capture);
            ConnectSource(physical, prepared);
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

        static bool ContainsEntrySource(
            AnimationBlendStackRuntime stack,
            AnimationPoseSourceId sourceId)
        {
            for (int i = 0; i < stack.EntryCount; i++)
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
                            release.PoseSlotId,
                            release.SourceId,
                            release.CompletionIdentity);
                    }
                    AnimationPhysicalSourceIdentity physical = m_PhysicalSources.RequireIdentity(release.SourceId);
                    int port = checked(physical.Index.Value + 1);
                    if (m_SourceFanIn.GetInput(port).IsValid())
                        m_SourceFanIn.DisconnectInput(port);
                    m_SourceFanIn.SetInputWeight(port, 0f);
                    m_SourceBackend.Release(release.SourceId);
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

        static ResolvedAnimationPoseSlot RequireSlot(
            CharacterAnimationPresentationBindingIndex bindings,
            CharacterPresentationPoseSlotProgramEntry programSlot)
        {
            if (programSlot == null || !bindings.TryGetSlot(programSlot.PoseSlotId, out ResolvedAnimationPoseSlot slot) ||
                slot.Index != programSlot.Index || slot.AnimationChannelId != programSlot.AnimationChannelId)
            {
                throw new InvalidOperationException("Animation Pose Program Slot is not present in the binding index.");
            }
            return slot;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AnimationPosePlayableGraphRuntime));
        }
    }
}
