using System;
using System.Collections.Generic;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Presentation.Animancer;
using ThirdPersonSimulation;
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
        readonly AnimationBlendStackRuntime[] m_Stacks;
        readonly Dictionary<AnimationChannelId, AnimationBlendStackRuntime> m_StacksByChannel =
            new Dictionary<AnimationChannelId, AnimationBlendStackRuntime>();
        readonly Dictionary<PoseSlotId, AnimationBlendStackRuntime> m_StacksBySlot =
            new Dictionary<PoseSlotId, AnimationBlendStackRuntime>();
        readonly AnimationMixerPlayable m_SourceFanIn;
        readonly Playable m_PreviousOutputSource;
        readonly bool m_ManagesGraphClock;

        AnimationScriptPlayable[] m_SlotPlayables;
        AnimationScriptPlayable m_PoseGraphPlayable;
        AnimationScriptPlayable m_FinalWriterPlayable;
        ulong m_CompletionIdentity = 1;
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
            m_FramePublisher = new FinalAnimationPoseFramePublisher(projection.PoseProgram);
            m_SourceFanIn = sourceFanIn;
            m_PreviousOutputSource = previousOutputSource;
            m_ManagesGraphClock = managesGraphClock;
        }

        internal IReadOnlyList<AnimationBlendStackRuntime> Stacks => m_Stacks;

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
            {
                AnimationBlendStackRuntime stack = m_Stacks[stackIndex];
                for (int entryIndex = 0; entryIndex < stack.EntryCount; entryIndex++)
                {
                    AnimationBlendEntryId entry = stack.GetEntryId(entryIndex);
                    if (entry.EmptyTarget)
                        continue;
                    if (!requests.TryGetValue(entry.SourceId, out ResolvedAnimationPoseRequest request))
                    {
                        throw new InvalidOperationException(
                            $"Animation Pose Source '{entry.SourceId}' has no current resolved request.");
                    }
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
            }

            var slotJobs = new AnimationSlotBlendJob[m_Stacks.Length];
            for (int slotIndex = 0; slotIndex < m_Stacks.Length; slotIndex++)
            {
                AnimationPoseSlotNativeWriteBinding write =
                    m_Workspace.RequireSlotWriteBinding(slotIndex, completionIdentity);
                slotJobs[slotIndex] = m_Stacks[slotIndex].PrepareSlotJob(
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
            InstallOrUpdateJobs(slotJobs, poseJob, finalWriter);

            m_Animancer.Evaluate(presentationDeltaSeconds);
            for (int i = 0; i < m_Stacks.Length; i++)
                m_Stacks[i].CompleteFrame(completionIdentity);
            FinalAnimationPoseFrame result = m_FramePublisher.Publish(in finalRead, m_PhysicalSources);
            ReleaseCompletedSources(completionIdentity);
            return result;
        }

        internal void Reset()
        {
            RequireAlive();
            ulong completionIdentity = NextCompletionIdentity();
            for (int i = 0; i < m_Stacks.Length; i++)
                m_Stacks[i].Reset(completionIdentity);
            ReleaseCompletedSources(completionIdentity);
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
            try
            {
                RemoveJobs();
                m_SourceBackend.Dispose();
                RestoreOutputAndDestroyFanIn();
            }
            finally
            {
                for (int i = m_Stacks.Length - 1; i >= 0; i--)
                    m_Stacks[i]?.Dispose();
                m_PhysicalSources.Dispose();
                m_PoseProgram.Dispose();
                m_Workspace.Dispose();
                if (m_ManagesGraphClock && m_Animancer && m_Animancer.IsGraphInitialized)
                    m_Animancer.Graph.UnpauseGraph();
            }
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

        void InstallOrUpdateJobs(
            AnimationSlotBlendJob[] slotJobs,
            CharacterPoseGraphNativeJob poseJob,
            AnimationFinalPoseStreamWriterJob finalWriter)
        {
            if (!m_JobsInstalled)
            {
                m_SlotPlayables = new AnimationScriptPlayable[slotJobs.Length];
                for (int i = 0; i < slotJobs.Length; i++)
                {
                    m_SlotPlayables[i] = m_Animancer.Graph.InsertOutputJob(slotJobs[i]);
                    m_SlotPlayables[i].SetProcessInputs(true);
                }
                m_PoseGraphPlayable = m_Animancer.Graph.InsertOutputJob(poseJob);
                m_PoseGraphPlayable.SetProcessInputs(true);
                m_FinalWriterPlayable = m_Animancer.Graph.InsertOutputJob(finalWriter);
                m_FinalWriterPlayable.SetProcessInputs(true);
                m_JobsInstalled = true;
                return;
            }
            for (int i = 0; i < slotJobs.Length; i++)
                m_SlotPlayables[i].SetJobData(slotJobs[i]);
            m_PoseGraphPlayable.SetJobData(poseJob);
            m_FinalWriterPlayable.SetJobData(finalWriter);
        }

        void ReleaseCompletedSources(ulong completionIdentity)
        {
            for (int stackIndex = 0; stackIndex < m_Stacks.Length; stackIndex++)
            {
                AnimationBlendStackRuntime stack = m_Stacks[stackIndex];
                while (stack.TryDequeueStackRelease(out AnimationBlendStackRelease release))
                {
                    if (release.CompletionIdentity != completionIdentity)
                        throw new InvalidOperationException("Animation Blend Stack source release does not match the exact completed frame.");
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
