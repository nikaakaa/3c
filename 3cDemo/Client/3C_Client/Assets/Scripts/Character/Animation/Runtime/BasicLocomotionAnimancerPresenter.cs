using Animancer;
using Animancer.TransitionLibraries;
using ThirdPersonDiagnostics;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAnimation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AnimancerComponent))]
    public sealed class BasicLocomotionAnimancerPresenter : MonoBehaviour, IAnimationPhasePlaybackProgressSource
    {
        const string IdleKey = "Idle";
        const string WalkStartKey = "WalkStart";
        const string WalkLoopKey = "WalkLoop";
        const string WalkEndKey = "WalkEnd";
        const string RunStartKey = "RunStart";
        const string RunLoopKey = "RunLoop";
        const string RunEndKey = "RunEnd";

        [SerializeField] AnimancerComponent animancer;
        [SerializeField] RunLocomotionAnimationConfigSO runAnimationConfig;
        [SerializeField] bool disableAnimatorRootMotion = true;

        BasicMovementPhase currentPhase = (BasicMovementPhase)(-1);
        BasicMovementGait currentGait = BasicMovementGait.Walk;
        StringReference currentKey;
        string currentAliasKey = string.Empty;
        AnimancerState currentState;
        StringReference lastInvalidKey;

        public BasicMovementPhase CurrentPhase => currentPhase;
        public BasicMovementGait CurrentGait => currentGait;
        public AnimationPhasePlaybackProgress CurrentPlaybackProgress => BuildPlaybackProgress();
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
        public RunLocomotionAnimationConfigSO RunAnimationConfig { get => runAnimationConfig; set => runAnimationConfig = value; }

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
            {
                LogPlayback("locomotion-animation-missing-animancer", RuntimeDiagnosticLogLevel.Warning, in context, string.Empty, null);
                return;
            }

            string aliasKey = ResolveAliasKey(context.Phase, context.Gait);
            StringReference nextKey = StringReference.Get(aliasKey);
            LogPresentProbe(in context, aliasKey, nextKey);
            if (currentPhase == context.Phase && currentGait == context.Gait && currentKey == nextKey && currentState != null && currentState.IsCurrent)
                return;

            if (!CanPlay(nextKey, in context))
                return;

            AnimancerState nextState = animancer.TryPlay(nextKey);
            if (nextState == null)
            {
                LogPlayback("locomotion-animation-play-failed", RuntimeDiagnosticLogLevel.Warning, in context, aliasKey, null);
                return;
            }

            currentPhase = context.Phase;
            currentGait = context.Gait;
            currentKey = nextKey;
            currentAliasKey = aliasKey;
            currentState = nextState;
            LogPlayback("locomotion-animation-played", RuntimeDiagnosticLogLevel.Info, in context, aliasKey, nextState);
        }

        bool CanPlay(StringReference key, in MovementAnimationContext context)
        {
            TransitionLibrary library = animancer.Graph.Transitions;
            if (library == null)
            {
                LogPlayback("locomotion-animation-missing-library", RuntimeDiagnosticLogLevel.Warning, in context, key);
                return false;
            }

            if (!library.TryGetTransition(key, out TransitionModifierGroup group))
            {
                LogPlayback("locomotion-animation-missing-transition", RuntimeDiagnosticLogLevel.Warning, in context, key);
                return false;
            }

            if (group.Transition.IsValid())
                return true;

            if (lastInvalidKey != key)
            {
                lastInvalidKey = key;
                LogPlayback("locomotion-animation-invalid-transition", RuntimeDiagnosticLogLevel.Warning, in context, key);
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

        string ResolveAliasKey(BasicMovementPhase phase, BasicMovementGait gait)
        {
            if (runAnimationConfig != null)
                return runAnimationConfig.ResolveAliasKey(phase, gait);

            if (gait == BasicMovementGait.Walk)
            {
                return phase switch
                {
                    BasicMovementPhase.MoveStart => WalkStartKey,
                    BasicMovementPhase.MoveLoop => WalkLoopKey,
                    BasicMovementPhase.MoveStop => WalkEndKey,
                    _ => IdleKey
                };
            }

            return phase switch
            {
                BasicMovementPhase.MoveStart => RunStartKey,
                BasicMovementPhase.MoveLoop => RunLoopKey,
                BasicMovementPhase.MoveStop => RunEndKey,
                _ => IdleKey
            };
        }

        void LogPresentProbe(in MovementAnimationContext context, string aliasKey, StringReference nextKey)
        {
            bool relevant = context.Phase == BasicMovementPhase.MoveStop ||
                            context.Gait == BasicMovementGait.Run ||
                            string.Equals(aliasKey, RunEndKey, System.StringComparison.Ordinal);
            if (!relevant)
                return;

            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Animation,
                RuntimeDiagnosticLogLevel.Trace,
                "locomotion-animation-present-probe",
                aliasKey,
                currentAliasKey,
                0,
                Time.frameCount,
                $"phase={context.Phase} gait={context.Gait} alias={aliasKey} currentPhase={currentPhase} currentGait={currentGait} currentAlias={currentAliasKey} sameKey={(currentKey == nextKey)} currentStateCurrent={(currentState != null && currentState.IsCurrent)} currentAnimation={CurrentAnimationName} planarSpeed={context.PlanarSpeed:F3}"));
        }

        void ApplyRootMotionPolicy()
        {
            if (disableAnimatorRootMotion && animancer != null && animancer.Animator != null)
                animancer.Animator.applyRootMotion = false;
        }

        void LogPlayback(
            string message,
            RuntimeDiagnosticLogLevel level,
            in MovementAnimationContext context,
            string aliasKey,
            AnimancerState state)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Animation,
                level,
                message,
                aliasKey,
                currentAliasKey,
                0,
                Time.frameCount,
                $"phase={context.Phase} gait={context.Gait} hasMove={context.HasMoveIntent} strength={context.InputStrength:F3} speed={context.PlanarSpeed:F3} direction={context.WorldDirection.ToString("F3")} alias={aliasKey} previousAlias={currentAliasKey} currentAnimation={CurrentAnimationName} nextAnimation={AnimationName(state)} normalized={(state != null ? state.NormalizedTime : 0f):F3} rootMotionDisabled={disableAnimatorRootMotion}"));
        }

        void LogPlayback(
            string message,
            RuntimeDiagnosticLogLevel level,
            in MovementAnimationContext context,
            StringReference aliasKey)
        {
            LogPlayback(message, level, in context, aliasKey.ToString(), null);
        }

        static string AnimationName(AnimancerState state)
        {
            if (state == null)
                return string.Empty;

            Object mainObject = state.MainObject;
            if (mainObject != null)
                return mainObject.name;

            return state.Clip != null ? state.Clip.name : string.Empty;
        }

        AnimationPhasePlaybackProgress BuildPlaybackProgress()
        {
            if (currentState == null)
                return AnimationPhasePlaybackProgress.Invalid(currentPhase);

            float normalizedTime = currentState.NormalizedTime;
            return new AnimationPhasePlaybackProgress(
                currentPhase,
                currentAliasKey,
                normalizedTime,
                true,
                normalizedTime >= currentState.NormalizedEndTime);
        }
    }
}
