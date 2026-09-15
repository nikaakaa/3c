using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseProgramActorRuntime
    {
        readonly CharacterPoseProgramImage m_Image;
        readonly CharacterPoseProgramExecutionView m_ExecutionView;
        readonly CharacterPoseActorState m_ActorState;
        readonly CharacterPoseProgramFramePages m_FramePages;
        readonly CharacterPoseProgramActionRuntime m_Action;
        readonly PresentationFrameWorkspace m_PresentationWorkspace;
        readonly CharacterPoseSourceModule m_SourceModule;
        readonly CharacterPoseProgramSourcePreparationRuntime
            m_SourcePreparation;

        internal CharacterPoseProgramActorRuntime(
            CharacterPoseProgramImage image,
            CharacterPoseProgramExecutionView executionView,
            CharacterPoseActorState actorState,
            CharacterPoseProgramFramePages framePages,
            CharacterPoseProgramActionRuntime action,
            PresentationFrameWorkspace presentationWorkspace,
            CharacterPoseSourceModule sourceModule,
            CharacterPoseProgramSourcePreparationRuntime sourcePreparation)
        {
            m_Image = image ?? throw new ArgumentNullException(nameof(image));
            m_ExecutionView = executionView ??
                throw new ArgumentNullException(nameof(executionView));
            m_ActorState = actorState ??
                throw new ArgumentNullException(nameof(actorState));
            m_FramePages = framePages ??
                throw new ArgumentNullException(nameof(framePages));
            m_Action = action ??
                throw new ArgumentNullException(nameof(action));
            m_PresentationWorkspace = presentationWorkspace ??
                throw new ArgumentNullException(nameof(presentationWorkspace));
            m_SourceModule = sourceModule ??
                throw new ArgumentNullException(nameof(sourceModule));
            m_SourcePreparation = sourcePreparation ??
                throw new ArgumentNullException(nameof(sourcePreparation));
        }

        internal bool IsPlayerActive(int playerIndex) =>
            m_ActorState.LinkedFragments.IsPlayerActive(playerIndex);

        internal void Advance(
            CharacterPoseProgramFrameLease lease,
            float presentationDeltaSeconds,
            in CharacterPresentationFactFrame factFrame,
            in CharacterAnimationPoseInputFrame parameterFrame,
            in CharacterPoseSourceTuningView sourceTuning)
        {
            if (!float.IsFinite(presentationDeltaSeconds) ||
                presentationDeltaSeconds < 0f ||
                !factFrame.IsValid ||
                !parameterFrame.IsValid)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(presentationDeltaSeconds));
            }
            if (m_SourcePreparation.ApplySequencePreview())
                return;
            m_ActorState.PoseStateSources.PrepareFrame(
                presentationDeltaSeconds,
                in factFrame,
                in parameterFrame);
            for (int i = 0; i < m_ActorState.Routes.Length; i++)
            {
                AnimationBlendStackRuntime stack = m_ActorState.Stacks[i];
                if (!m_ActorState.LinkedFragments.IsPlayerActive(
                        stack.PlayerIndex))
                {
                    continue;
                }
                CharacterAnimationTransitionRouteRuntime route =
                    m_ActorState.Routes[i];
                route.FlushReleaseCompletion();
                if (!route.IsAnimationSlot)
                    continue;
                CharacterAnimationSlotNativeControl control =
                    route.NativeControl;
                m_FramePages.SetAnimationSlotControl(
                    route.AnimationSlotIndex,
                    in control);
            }
            for (int i = 0; i < m_ActorState.Stacks.Length; i++)
            {
                AnimationBlendStackRuntime stack = m_ActorState.Stacks[i];
                if (m_ActorState.LinkedFragments.IsPlayerActive(
                        stack.PlayerIndex))
                {
                    stack.Advance(presentationDeltaSeconds);
                }
            }
            m_ActorState.PoseStateSources.AdvanceSources(
                m_SourceModule,
                presentationDeltaSeconds,
                in factFrame,
                in parameterFrame,
                sourceTuning);
        }

        internal void FinalizePoseStateFrame(
            CharacterPoseProgramFrameLease lease,
            in CharacterPresentationFactFrame factFrame,
            in CharacterAnimationPoseInputFrame parameterFrame)
        {
            m_Action.RequireWorkspace(lease.Lineage.FrameIdentity);
            PresentationFrameWorkspaceLease workspaceFrame =
                m_Action.WorkspaceFrame;
            if (!factFrame.IsValid || !parameterFrame.IsValid ||
                !workspaceFrame.IsValid)
            {
                throw new ArgumentException(
                    "Pose State frame finalization is invalid.",
                    nameof(factFrame));
            }
            if (m_SourcePreparation.HasSequencePreview)
                return;
            m_ActorState.PoseStateSources.EvaluateTransitions(
                in factFrame,
                in parameterFrame,
                m_FramePages,
                m_PresentationWorkspace,
                workspaceFrame);
            for (int i = 0;
                 i < m_ActorState.RootOrientationWarps.Length;
                 i++)
            {
                if (!m_ActorState.LinkedFragments
                        .IsRootOrientationWarpActive(i))
                {
                    continue;
                }
                CharacterRootOrientationWarpNativeControl control =
                    m_ActorState.RootOrientationWarps[i]
                        .Prepare(in factFrame, in parameterFrame);
                m_FramePages.SetRootOrientationWarpControl(i, in control);
            }
        }

        internal void BeginFrame(
            CharacterLinkedPoseRuntimeSession linkedPose,
            IReadOnlyList<CharacterLinkedPoseGroupProjectionDescriptor> groups,
            ulong resetCompletionIdentity)
        {
            bool nodeFramesOpen = false;
            try
            {
                PrepareLinkedPoseSelection(linkedPose, groups);
                m_ActorState.Inertialization.BeginFrame();
                for (int i = 0; i < m_ActorState.Routes.Length; i++)
                    m_ActorState.Routes[i].BeginFrame();
                for (int i = 0;
                     i < m_ActorState.RootOrientationWarps.Length;
                     i++)
                {
                    m_ActorState.RootOrientationWarps[i].BeginFrame();
                }
                BeginNodeFrames();
                nodeFramesOpen = true;
                ApplyLinkedPoseGenerationResets(resetCompletionIdentity);
            }
            catch
            {
                if (nodeFramesOpen)
                    DiscardNodeFrames();
                for (int i = m_ActorState.Routes.Length - 1; i >= 0; i--)
                {
                    if (m_ActorState.Routes[i].HasOpenFrame)
                        m_ActorState.Routes[i].DiscardFrame();
                }
                for (int i = m_ActorState.RootOrientationWarps.Length - 1;
                     i >= 0;
                     i--)
                {
                    if (m_ActorState.RootOrientationWarps[i].HasOpenFrame)
                        m_ActorState.RootOrientationWarps[i].DiscardFrame();
                }
                if (m_ActorState.Inertialization.HasOpenFrame)
                    m_ActorState.Inertialization.DiscardFrame();
                m_ActorState.LinkedFragments.Clear();
                throw;
            }
        }

        internal void CommitFrame()
        {
            for (int i = 0; i < m_ActorState.Routes.Length; i++)
                m_ActorState.Routes[i].CommitFrame();
            for (int i = 0;
                 i < m_ActorState.RootOrientationWarps.Length;
                 i++)
            {
                m_ActorState.RootOrientationWarps[i].CommitFrame();
            }
            for (int i = 0; i < m_ActorState.Stacks.Length; i++)
                m_ActorState.Stacks[i].CommitFrame();
            for (int i = 0; i < m_ActorState.DirectPlayers.Length; i++)
                m_ActorState.DirectPlayers[i].CommitFrame();
            m_ActorState.PoseStateSources.CommitFrame();
        }

        internal void DiscardNodeFrames()
        {
            Exception failure = null;
            for (int i = m_ActorState.Routes.Length - 1; i >= 0; i--)
            {
                CharacterAnimationTransitionRouteRuntime route =
                    m_ActorState.Routes[i];
                if (route.HasOpenFrame)
                    DiscardStep(route.DiscardFrame, ref failure);
            }
            DiscardStep(DiscardPlayerFrames, ref failure);
            if (failure != null)
            {
                throw new AggregateException(
                    "Character Pose Program actor node discard failed.",
                    failure);
            }
        }

        internal void DiscardRootOrientationWarpFrames()
        {
            Exception failure = null;
            for (int i = m_ActorState.RootOrientationWarps.Length - 1;
                 i >= 0;
                 i--)
            {
                RootOrientationWarpRuntime warp =
                    m_ActorState.RootOrientationWarps[i];
                if (warp.HasOpenFrame)
                    DiscardStep(warp.DiscardFrame, ref failure);
            }
            if (failure != null)
            {
                throw new AggregateException(
                    "Character Pose Program Root Orientation Warp discard failed.",
                    failure);
            }
        }

        internal void ClearLinkedPoseFrameSelection() =>
            m_ActorState.LinkedFragments.Clear();

        internal void ResetBlendState(ulong completionIdentity)
        {
            for (int i = 0; i < m_ActorState.Routes.Length; i++)
                m_ActorState.Routes[i].Reset();
            for (int i = 0; i < m_ActorState.Stacks.Length; i++)
                m_ActorState.Stacks[i].Reset(completionIdentity);
        }

        internal void ResetPoseState(PoseDiscontinuityResetReason reason)
        {
            for (int i = 0; i < m_ActorState.DirectPlayers.Length; i++)
                m_ActorState.DirectPlayers[i].Reset(reason);
            m_ActorState.PoseStateSources.Reset(reason);
            for (int i = 0;
                 i < m_ActorState.RootOrientationWarps.Length;
                 i++)
            {
                m_ActorState.RootOrientationWarps[i].Reset();
                var control = new CharacterRootOrientationWarpNativeControl(
                    false,
                    0f);
                m_FramePages.SetRootOrientationWarpControl(i, in control);
            }
        }

        void PrepareLinkedPoseSelection(
            CharacterLinkedPoseRuntimeSession linkedPose,
            IReadOnlyList<CharacterLinkedPoseGroupProjectionDescriptor> groups)
        {
            if (linkedPose == null)
                throw new ArgumentNullException(nameof(linkedPose));
            if (groups == null)
                throw new ArgumentNullException(nameof(groups));
            m_ActorState.LinkedFragments.Clear();
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                CharacterLinkedPoseGroupProjectionDescriptor group =
                    groups[groupIndex];
                CharacterLinkedPoseGenerationHandle selection =
                    linkedPose.RequireIncoming(group.GroupId);
                SetLinkedPoseGroupSelection(in selection);
                m_ActorState.LinkedFragments.ApplySelection(in selection);
            }
        }

        void ApplyLinkedPoseGenerationResets(
            ulong resetCompletionIdentity)
        {
            if (resetCompletionIdentity == 0)
                throw new ArgumentOutOfRangeException(
                    nameof(resetCompletionIdentity));
            CharacterPoseLinkedFragmentState linkedFragments =
                m_ActorState.LinkedFragments;
            if (!linkedFragments.HasFragments)
                return;
            for (int i = 0; i < m_ActorState.Stacks.Length; i++)
            {
                AnimationBlendStackRuntime stack = m_ActorState.Stacks[i];
                if (!linkedFragments.RequiresPlayerReset(stack.PlayerIndex))
                    continue;
                m_ActorState.Routes[i].Reset();
                stack.Reset(resetCompletionIdentity);
            }
            for (int i = 0; i < m_ActorState.DirectPlayers.Length; i++)
            {
                AnimationSelectedPosePlayerRuntime player =
                    m_ActorState.DirectPlayers[i];
                if (linkedFragments.RequiresPlayerReset(player.PlayerIndex))
                {
                    player.Reset(
                        PoseDiscontinuityResetReason.BranchReplacement);
                }
            }
            m_ActorState.PoseStateSources.ApplyLinkedPoseGenerationResets();
            for (int i = 0; i < m_Image.Inertializations.Count; i++)
            {
                if (linkedFragments.RequiresInertializationReset(i))
                    m_ActorState.Inertialization.RequestReset(i);
            }
            for (int i = 0;
                 i < m_ActorState.RootOrientationWarps.Length;
                 i++)
            {
                if (linkedFragments.RequiresRootOrientationWarpReset(i))
                    m_ActorState.RootOrientationWarps[i].Reset();
            }
        }

        void SetLinkedPoseGroupSelection(
            in CharacterLinkedPoseGenerationHandle selection)
        {
            if (!m_FramePages.HasOpenFrame)
            {
                throw new InvalidOperationException(
                    "Character Pose Program frame pages are not open.");
            }
            if (!selection.IsValid)
            {
                throw new ArgumentException(
                    "Linked Pose generation selection is invalid.",
                    nameof(selection));
            }
            NativeArray<AnimationPoseGraphNativeLinkedPoseCallControl>
                controls = m_FramePages.LinkedPoseCallControls;
            NativeArray<byte> activeFragments =
                m_FramePages.LinkedPoseActiveFragments;
            int matchingCallCount = 0;
            for (int callIndex = 0;
                 callIndex < m_ExecutionView.LinkedPoseCalls.Length;
                 callIndex++)
            {
                if (m_ExecutionView.GetLinkedPoseCallGroupId(callIndex) !=
                    selection.GroupId)
                {
                    continue;
                }
                matchingCallCount++;
                if (m_ExecutionView.GetLinkedPoseCallInterfaceId(callIndex) !=
                        selection.InterfaceId ||
                    controls[callIndex].IsActive ||
                    m_ExecutionView.FindLinkedPoseCandidate(
                        callIndex,
                        selection.ImplementationId) < 0)
                {
                    throw new InvalidOperationException(
                        $"Linked Pose Group '{selection.GroupId}' selection does not match call #{callIndex}.");
                }
            }
            if (matchingCallCount == 0)
            {
                throw new InvalidOperationException(
                    $"Linked Pose Group '{selection.GroupId}' has no compiled calls.");
            }
            for (int callIndex = 0;
                 callIndex < m_ExecutionView.LinkedPoseCalls.Length;
                 callIndex++)
            {
                if (m_ExecutionView.GetLinkedPoseCallGroupId(callIndex) !=
                    selection.GroupId)
                {
                    continue;
                }
                int candidateIndex =
                    m_ExecutionView.FindLinkedPoseCandidate(
                        callIndex,
                        selection.ImplementationId);
                AnimationPoseGraphNativeLinkedPoseCandidate candidate =
                    m_ExecutionView.LinkedPoseCandidates[candidateIndex];
                controls[callIndex] =
                    new AnimationPoseGraphNativeLinkedPoseCallControl(
                        candidateIndex,
                        selection.Generation,
                        selection.PoseDiscontinuity);
                activeFragments[candidate.FragmentIndex] = 1;
            }
        }

        void BeginNodeFrames()
        {
            int stackCount = 0;
            int directPlayerCount = 0;
            bool poseStateSourcesOpen = false;
            try
            {
                m_ActorState.PoseStateSources.BeginFrame();
                poseStateSourcesOpen = true;
                for (; stackCount < m_ActorState.Stacks.Length; stackCount++)
                    m_ActorState.Stacks[stackCount].BeginFrame();
                for (;
                     directPlayerCount < m_ActorState.DirectPlayers.Length;
                     directPlayerCount++)
                {
                    m_ActorState.DirectPlayers[directPlayerCount].BeginFrame();
                }
            }
            catch
            {
                for (int i = directPlayerCount - 1; i >= 0; i--)
                    m_ActorState.DirectPlayers[i].DiscardFrame();
                for (int i = stackCount - 1; i >= 0; i--)
                    m_ActorState.Stacks[i].DiscardFrame();
                if (poseStateSourcesOpen)
                    m_ActorState.PoseStateSources.DiscardFrame();
                throw;
            }
        }

        void DiscardPlayerFrames()
        {
            for (int i = m_ActorState.DirectPlayers.Length - 1; i >= 0; i--)
                m_ActorState.DirectPlayers[i].DiscardFrame();
            for (int i = m_ActorState.Stacks.Length - 1; i >= 0; i--)
                m_ActorState.Stacks[i].DiscardFrame();
            m_ActorState.PoseStateSources.DiscardFrame();
        }

        static void DiscardStep(Action discard, ref Exception failure)
        {
            try
            {
                discard();
            }
            catch (Exception exception)
            {
                failure = failure == null
                    ? exception
                    : new AggregateException(failure, exception);
            }
        }
    }
}
