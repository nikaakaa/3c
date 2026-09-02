using System;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal sealed class CharacterPoseActorState : IDisposable
    {
        bool m_Disposed;

        internal CharacterPoseActorState(
            AnimationBlendStackRuntime[] stacks,
            CharacterAnimationTransitionRouteRuntime[] routes,
            AnimationSelectedPosePlayerRuntime[] directPlayers,
            PoseStateAndSourceRuntime poseStateSources,
            RootOrientationWarpRuntime[] rootOrientationWarps,
            PoseInertializationNativeProgram inertialization,
            CharacterPoseProgramNodeRuntimeIndex nodeRuntimeIndex,
            int sourceRetirementCapacity)
        {
            Stacks = stacks ?? throw new ArgumentNullException(nameof(stacks));
            Routes = routes ?? throw new ArgumentNullException(nameof(routes));
            DirectPlayers = directPlayers ??
                throw new ArgumentNullException(nameof(directPlayers));
            PoseStateSources = poseStateSources ??
                throw new ArgumentNullException(nameof(poseStateSources));
            RootOrientationWarps = rootOrientationWarps ??
                throw new ArgumentNullException(nameof(rootOrientationWarps));
            Inertialization = inertialization ??
                throw new ArgumentNullException(nameof(inertialization));
            NodeRuntimeIndex = nodeRuntimeIndex ??
                throw new ArgumentNullException(nameof(nodeRuntimeIndex));
            if (Stacks.Length != Routes.Length ||
                sourceRetirementCapacity <= 0)
            {
                throw new ArgumentException(
                    "Character Pose Actor State layout is invalid.");
            }
            SourceRetirement =
                new CharacterPoseProgramSourceRetirementState(
                    sourceRetirementCapacity);
        }

        internal AnimationBlendStackRuntime[] Stacks { get; }
        internal CharacterAnimationTransitionRouteRuntime[] Routes { get; }
        internal AnimationSelectedPosePlayerRuntime[] DirectPlayers { get; }
        internal PoseStateAndSourceRuntime PoseStateSources { get; }
        internal RootOrientationWarpRuntime[] RootOrientationWarps { get; }
        internal PoseInertializationNativeProgram Inertialization { get; }
        internal CharacterPoseProgramNodeRuntimeIndex NodeRuntimeIndex { get; }
        internal CharacterPoseProgramSourceRetirementState SourceRetirement
        {
            get;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            Exception failure = null;
            for (int i = Stacks.Length - 1; i >= 0; i--)
            {
                if (Stacks[i] != null)
                    DisposeStep(Stacks[i].Dispose, ref failure);
            }
            for (int i = DirectPlayers.Length - 1; i >= 0; i--)
            {
                if (DirectPlayers[i] != null)
                    DisposeStep(DirectPlayers[i].Dispose, ref failure);
            }
            for (int i = PoseStateSources.ClipPlayers.Length - 1;
                 i >= 0;
                 i--)
            {
                if (PoseStateSources.ClipPlayers[i] != null)
                {
                    DisposeStep(
                        PoseStateSources.ClipPlayers[i].Dispose,
                        ref failure);
                }
            }
            for (int i = PoseStateSources.BlendSpacePlayers.Length - 1;
                 i >= 0;
                 i--)
            {
                if (PoseStateSources.BlendSpacePlayers[i] != null)
                {
                    DisposeStep(
                        PoseStateSources.BlendSpacePlayers[i].Dispose,
                        ref failure);
                }
            }
            DisposeStep(Inertialization.Dispose, ref failure);
            if (failure != null)
                throw failure;
        }

        static void DisposeStep(Action dispose, ref Exception failure)
        {
            try
            {
                dispose();
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
