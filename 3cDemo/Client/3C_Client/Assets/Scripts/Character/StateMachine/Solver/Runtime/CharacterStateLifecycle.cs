using ThirdPersonInput;
using UnityEngine;

namespace ThirdPersonCharacterStateMachine
{
    public interface ICharacterStateLifecycle
    {
        void Enter(in CharacterStateLifecycleContext context, CharacterStateMachineFrameBuilder builder);
        void Tick(in CharacterStateLifecycleContext context, CharacterStateMachineFrameBuilder builder);
        void Exit(in CharacterStateLifecycleContext context, CharacterStateMachineFrameBuilder builder);
    }

    public readonly struct CharacterStateLifecycleContext
    {
        public CharacterStateLifecycleContext(
            CharacterStateNodeDefinition node,
            CharacterStateNodeDefinition targetNode,
            CharacterStateId stateId,
            CharacterStateVariant variant,
            float stateTime,
            Vector3 actionWorldDirection,
            in CharacterStateMachineContext frameContext,
            bool animationRequestedForState)
        {
            Node = node;
            TargetNode = targetNode;
            StateId = stateId;
            Variant = variant;
            StateTime = Mathf.Max(0f, stateTime);
            ActionWorldDirection = NormalizePlanarOrZero(actionWorldDirection);
            FrameContext = frameContext;
            AnimationRequestedForState = animationRequestedForState;
        }

        public CharacterStateNodeDefinition Node { get; }
        public CharacterStateNodeDefinition TargetNode { get; }
        public CharacterStateId StateId { get; }
        public CharacterStateVariant Variant { get; }
        public float StateTime { get; }
        public Vector3 ActionWorldDirection { get; }
        public CharacterStateMachineContext FrameContext { get; }
        public bool AnimationRequestedForState { get; }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }

    public sealed class CharacterStateMachineFrameBuilder
    {
        CharacterStateAnimationRequest animationRequest;
        public bool ConsumeInputRequest { get; private set; }
        public InputRequestKind ConsumedRequestKind { get; private set; }
        public bool SetRunLatch { get; private set; }
        public bool ResetRunLatch { get; private set; }
        public bool HasAnimationRequest { get; private set; }
        public CharacterStateAnimationRequest AnimationRequest => animationRequest;

        public void ConsumeRequest(InputRequestKind kind)
        {
            ConsumeInputRequest = true;
            ConsumedRequestKind = kind;
        }

        public void SetRunLatchActive()
        {
            SetRunLatch = true;
        }

        public void ResetRunLatchActive()
        {
            ResetRunLatch = true;
        }

        public void RequestAnimation(
            CharacterStateAnimationBinding binding,
            CharacterStatePlaybackFactSource playbackFactSource,
            int sourceStep)
        {
            if (!binding.HasKey || HasAnimationRequest)
                return;

            animationRequest = new CharacterStateAnimationRequest(binding, playbackFactSource, sourceStep);
            HasAnimationRequest = true;
        }
    }

    public sealed class CharacterStateNodeLifecycle : ICharacterStateLifecycle
    {
        public static readonly CharacterStateNodeLifecycle Instance = new CharacterStateNodeLifecycle();

        CharacterStateNodeLifecycle()
        {
        }

        public void Enter(in CharacterStateLifecycleContext context, CharacterStateMachineFrameBuilder builder)
        {
            CharacterStateNodeDefinition node = context.Node;
            if (node == null)
                return;

            if (node.TryGetInputConsumeKind(out InputRequestKind consumeKind))
                builder.ConsumeRequest(consumeKind);
            if (node.ResetRunLatchOnEnterFromModules)
                builder.ResetRunLatchActive();
            if (!context.AnimationRequestedForState)
            {
                CharacterStateAnimationBinding binding = ResolveAnimationBinding(node, context.Variant, out CharacterStatePlaybackFactSource playbackFactSource);
                builder.RequestAnimation(binding, playbackFactSource, context.FrameContext.CurrentStep);
            }
        }

        public void Tick(in CharacterStateLifecycleContext context, CharacterStateMachineFrameBuilder builder)
        {
        }

        public void Exit(in CharacterStateLifecycleContext context, CharacterStateMachineFrameBuilder builder)
        {
            CharacterStateNodeDefinition node = context.Node;
            CharacterStateNodeDefinition targetNode = context.TargetNode;
            bool enteringActionState = targetNode != null && targetNode.IsActionCapabilityState;
            bool leavingActionState = node != null && node.IsActionCapabilityState && !enteringActionState;
            if (!leavingActionState)
                return;

            if (node.TryResolveActionMovement(context.Variant, out CharacterActionMovementDefinition movement) &&
                movement.SetRunLatchOnComplete)
            {
                builder.SetRunLatchActive();
            }
        }

        static CharacterStateAnimationBinding ResolveAnimationBinding(
            CharacterStateNodeDefinition node,
            CharacterStateVariant variant,
            out CharacterStatePlaybackFactSource playbackFactSource)
        {
            return node.TryResolveAnimationBinding(variant, out CharacterStateAnimationBinding binding, out playbackFactSource)
                ? binding
                : default;
        }
    }
}
