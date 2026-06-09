using Animancer;
using Animancer.TransitionLibraries;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAnimation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AnimancerComponent))]
    public sealed class BasicLocomotionAnimancerPresenter : MonoBehaviour
    {
        const string IdleKey = "Idle";
        const string WalkStartKey = "WalkStart";
        const string WalkLoopKey = "WalkLoop";
        const string WalkEndKey = "WalkEnd";
        const string RunStartKey = "RunStart";
        const string RunLoopKey = "RunLoop";
        const string RunEndKey = "RunEnd";

        [SerializeField] AnimancerComponent animancer;
        [SerializeField] LocomotionAnimationSetSO animationSet;
        [SerializeField, Range(0f, 1f)] float runInputThreshold = 0.65f;
        [SerializeField] bool disableAnimatorRootMotion = true;

        BasicMovementPhase currentPhase = (BasicMovementPhase)(-1);
        StringReference currentKey;
        LocomotionAnimationGait lastMovingGait = LocomotionAnimationGait.Run;
        AnimancerState currentState;
        StringReference lastInvalidKey;
        bool missingSetWarningLogged;

        public BasicMovementPhase CurrentPhase => currentPhase;
        public float RunInputThreshold => runInputThreshold;
        public string CurrentAnimationName
        {
            get
            {
                if (currentState == null)
                    return string.Empty;

                Object mainObject = currentState.MainObject;
                if (mainObject != null)
                    return mainObject.name;

                return currentState.Clip != null ? currentState.Clip.name : string.Empty;
            }
        }

        public float CurrentSpeed { get; private set; }
        public LocomotionAnimationSetSO AnimationSet { get => animationSet; set => animationSet = value; }

        void Reset()
        {
            animancer = GetComponent<AnimancerComponent>();
        }

        void Awake()
        {
            if (animancer == null)
                animancer = GetComponent<AnimancerComponent>();

            ApplyRootMotionPolicy();
        }

        public void Present(in MovementAnimationContext context)
        {
            CurrentSpeed = context.PlanarSpeed;

            if (animancer == null)
                animancer = GetComponent<AnimancerComponent>();

            ApplyRootMotionPolicy();

            if (animancer == null)
                return;

            LocomotionAnimationEntry entry = GetAnimationEntry(in context);
            StringReference nextKey = entry.KeyReference;
            if (currentPhase == context.Phase && currentKey == nextKey)
                return;

            if (!CanPlay(nextKey))
                return;

            AnimancerState nextState = entry.FadeDuration >= 0f
                ? animancer.TryPlay(nextKey, entry.FadeDuration)
                : animancer.TryPlay(nextKey);
            if (nextState == null)
                return;

            nextState.Speed = entry.Speed;
            if (entry.NormalizedStartTime >= 0f)
                nextState.NormalizedTime = entry.NormalizedStartTime;

            currentPhase = context.Phase;
            currentKey = nextKey;
            currentState = nextState;
        }

        bool CanPlay(StringReference key)
        {
            TransitionLibrary library = animancer.Graph.Transitions;
            if (library == null || !library.TryGetTransition(key, out TransitionModifierGroup group))
                return false;

            if (group.Transition.IsValid())
                return true;

            if (lastInvalidKey != key)
            {
                lastInvalidKey = key;
                Debug.LogError($"[BasicLocomotionAnimancerPresenter] Invalid Animancer transition for key '{key}'. {DescribeTransition(group.Transition)}", this);
            }

            return false;
        }

        static string DescribeTransition(ITransition transition)
        {
            Object transitionObject = transition as Object;
            ITransition innerTransition = transition is TransitionAssetBase asset ? asset.GetTransition() : transition;

            if (innerTransition is ClipTransition clipTransition)
            {
                AnimationClip clip = clipTransition.Clip;
                return $"transition='{TransitionName(transitionObject, innerTransition)}', clip='{(clip != null ? clip.name : "null")}', legacy={(clip != null && clip.legacy)}.";
            }

            return $"transition='{TransitionName(transitionObject, innerTransition)}', type='{innerTransition?.GetType().Name ?? "null"}'.";
        }

        static string TransitionName(Object transitionObject, ITransition transition)
        {
            if (transitionObject != null)
                return transitionObject.name;

            return transition?.ToString() ?? "null";
        }

        LocomotionAnimationEntry GetAnimationEntry(in MovementAnimationContext context)
        {
            LocomotionAnimationGait gait = ResolveGait(in context);
            if (context.HasMoveIntent)
                lastMovingGait = gait;

            if (animationSet != null)
                return animationSet.ResolveEntry(context.Phase, gait, lastMovingGait);

            if (!missingSetWarningLogged)
            {
                missingSetWarningLogged = true;
                Debug.LogWarning("[BasicLocomotionAnimancerPresenter] Animation set is missing. Using generated default locomotion animation keys.", this);
            }

            return context.Phase switch
            {
                BasicMovementPhase.MoveStart => gait == LocomotionAnimationGait.Run ? new LocomotionAnimationEntry(RunStartKey) : new LocomotionAnimationEntry(WalkStartKey),
                BasicMovementPhase.MoveLoop => gait == LocomotionAnimationGait.Run ? new LocomotionAnimationEntry(RunLoopKey) : new LocomotionAnimationEntry(WalkLoopKey),
                BasicMovementPhase.MoveStop => lastMovingGait == LocomotionAnimationGait.Run ? new LocomotionAnimationEntry(RunEndKey) : new LocomotionAnimationEntry(WalkEndKey),
                _ => new LocomotionAnimationEntry(IdleKey)
            };
        }

        LocomotionAnimationGait ResolveGait(in MovementAnimationContext context)
        {
            if (!context.HasMoveIntent)
                return lastMovingGait;

            return context.InputStrength >= runInputThreshold ? LocomotionAnimationGait.Run : LocomotionAnimationGait.Walk;
        }

        void ApplyRootMotionPolicy()
        {
            if (disableAnimatorRootMotion && animancer != null && animancer.Animator != null)
                animancer.Animator.applyRootMotion = false;
        }
    }
}
