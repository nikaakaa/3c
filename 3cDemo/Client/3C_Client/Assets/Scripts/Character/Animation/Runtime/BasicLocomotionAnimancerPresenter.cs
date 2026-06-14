using Animancer;
using Animancer.TransitionLibraries;
using ThirdPersonDiagnostics;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAnimation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AnimancerComponent))]
    public sealed class BasicLocomotionAnimancerPresenter : MonoBehaviour, ILocomotionAnimationPlaybackProgressController
    {
        const string TurnBackRootMotionLogKeyword = "TURNBACK_RM_CHAIN";
        const string TurnBackRootMotionProbeChannel = "Animation.turnback-root-motion-probe";
        const string TurnBackAliasKey = "Locomotion.Turn.Back";
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

        AnimatorRootMotionController rootMotionController;
        BasicMovementPhase currentPhase = (BasicMovementPhase)(-1);
        BasicMovementGait currentGait = BasicMovementGait.Walk;
        StringReference currentKey;
        string currentAliasKey = string.Empty;
        AnimancerState currentState;
        StringReference lastInvalidKey;
        float currentNormalizedTime;
        bool hasRestoredPlaybackResume;

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

        void OnAnimatorMove()
        {
            if (animancer == null || animancer.Animator == null)
            {
                LogTurnBackRootMotionProbe("missing-animator", null, null, false, false, Vector3.zero, 0f);
                return;
            }

            Animator animator = animancer.Animator;
            AnimatorRootMotionController controller = ResolveRootMotionController();
            bool manualRootMotionActive = controller != null && controller.ManualRootMotionActive;
            if (currentPhase != BasicMovementPhase.TurnBack)
            {
                LogTurnBackRootMotionProbe("skip-non-turnback-phase", animator, controller, manualRootMotionActive, false, animator.deltaPosition, Mathf.DeltaAngle(0f, animator.deltaRotation.eulerAngles.y));
                return;
            }

            LogTurnBackRootMotionProbe("on-animator-move-enter", animator, controller, manualRootMotionActive, false, animator.deltaPosition, Mathf.DeltaAngle(0f, animator.deltaRotation.eulerAngles.y));

            if (!manualRootMotionActive)
            {
                Animator skippedAnimator = animator;
                LogTurnBackRootMotionProbe("manual-root-motion-inactive", skippedAnimator, controller, manualRootMotionActive, false, skippedAnimator.deltaPosition, Mathf.DeltaAngle(0f, skippedAnimator.deltaRotation.eulerAngles.y));
                RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                    RuntimeDiagnosticLogCategory.Animation,
                    RuntimeDiagnosticLogLevel.Trace,
                    "locomotion-root-motion-skipped",
                    currentAliasKey,
                    string.Empty,
                    0,
                    Time.frameCount,
                    $"[{TurnBackRootMotionLogKeyword}] stage=presenter-skipped phase={currentPhase} alias={currentAliasKey} animation={CurrentAnimationName} applyRootMotion={skippedAnimator.applyRootMotion} manualRootMotionActive={manualRootMotionActive} stateTime={(currentState != null ? currentState.Time : 0f):F3} normalized={(currentState != null ? currentState.NormalizedTime : 0f):F3} length={(currentState != null ? currentState.Length : 0f):F3} visualPosition={skippedAnimator.transform.position.ToString("F3")} visualYaw={skippedAnimator.transform.eulerAngles.y:F3}"));
                return;
            }

            Vector3 worldDelta = animator.deltaPosition;
            worldDelta.y = 0f;
            float yawDelta = animator.deltaRotation.eulerAngles.y;
            yawDelta = Mathf.DeltaAngle(0f, yawDelta);
            bool hasDelta = worldDelta.sqrMagnitude > 0.000001f || Mathf.Abs(yawDelta) > 0.0001f;
            LogTurnBackRootMotionProbe("delta-ignored", animator, controller, manualRootMotionActive, hasDelta, worldDelta, yawDelta);
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Animation,
                RuntimeDiagnosticLogLevel.Trace,
                "locomotion-root-motion-delta",
                currentAliasKey,
                string.Empty,
                0,
                Time.frameCount,
                $"[{TurnBackRootMotionLogKeyword}] stage=presenter-delta-ignored phase={currentPhase} alias={currentAliasKey} animation={CurrentAnimationName} applyRootMotion={animator.applyRootMotion} manualRootMotionActive={manualRootMotionActive} hasDelta={hasDelta} worldDelta={worldDelta.ToString("F3")} yawDelta={yawDelta:F3} pendingWorldDelta={Vector3.zero.ToString("F3")} pendingYawDelta=0.000 stateTime={(currentState != null ? currentState.Time : 0f):F3} normalized={(currentState != null ? currentState.NormalizedTime : 0f):F3} length={(currentState != null ? currentState.Length : 0f):F3} visualPosition={animator.transform.position.ToString("F3")} visualYaw={animator.transform.eulerAngles.y:F3}"));
        }

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

            string aliasKey = ResolveAliasKey(in context);
            StringReference nextKey = StringReference.Get(aliasKey);
            LogPresentProbe(in context, aliasKey, nextKey);
            bool samePlayback = currentPhase == context.Phase &&
                                currentGait == context.Gait &&
                                currentKey == nextKey;
            if (samePlayback &&
                currentState != null &&
                currentState.IsCurrent)
            {
                hasRestoredPlaybackResume = false;
                return;
            }

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
            if (hasRestoredPlaybackResume && samePlayback)
            {
                nextState.NormalizedTime = currentNormalizedTime;
            }
            else
            {
                RestartOneShotStateIfNeeded(in context, nextState);
                currentNormalizedTime = nextState.NormalizedTime;
            }
            hasRestoredPlaybackResume = false;
            currentNormalizedTime = nextState.NormalizedTime;
            LogPlayback("locomotion-animation-played", RuntimeDiagnosticLogLevel.Info, in context, aliasKey, nextState);
        }

        static void RestartOneShotStateIfNeeded(in MovementAnimationContext context, AnimancerState state)
        {
            if (state == null || context.Phase != BasicMovementPhase.TurnBack)
                return;

            state.NormalizedTime = context.HasTurnBackMotionPolicy
                ? context.TurnBackMotionPolicy.StartNormalizedTime
                : 0f;
        }

        public bool RestorePlaybackProgress(in AnimationPhasePlaybackProgress progress)
        {
            return RestorePlaybackProgress(in progress, ResolveGaitForAlias(progress.Phase, progress.AliasKey, currentGait));
        }

        public bool RestorePlaybackProgress(in AnimationPhasePlaybackProgress progress, BasicMovementGait gait)
        {
            if (!progress.HasValidPlayback || string.IsNullOrWhiteSpace(progress.AliasKey))
                return ClearPlaybackProgress();

            if (!EnsureAnimancer())
                return false;

            StringReference key = StringReference.Get(progress.AliasKey);
            AnimancerState state = animancer.TryPlay(key);
            if (state == null)
                return false;

            currentPhase = progress.Phase;
            currentGait = gait;
            currentKey = key;
            currentAliasKey = progress.AliasKey;
            currentState = state;
            currentNormalizedTime = progress.NormalizedTime;
            currentState.NormalizedTime = currentNormalizedTime;
            hasRestoredPlaybackResume = true;
            return true;
        }

        public AnimationPhasePlaybackProgress AdvancePlayback(float deltaTime)
        {
            if (currentState == null)
                return AnimationPhasePlaybackProgress.Invalid(currentPhase);

            float length = currentState.Length;
            float speed = currentState.EffectiveSpeed;
            if (length > 0f)
                currentNormalizedTime = Mathf.Max(0f, currentNormalizedTime + Mathf.Max(0f, deltaTime) * speed / length);

            currentState.NormalizedTime = currentNormalizedTime;
            return BuildPlaybackProgress();
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
                 RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                     RuntimeDiagnosticLogCategory.Animation,
                     RuntimeDiagnosticLogLevel.Error,
                     "invalid-animancer-transition",
                     "",
                     "",
                     0,
                     Time.frameCount,
                     $"[BasicLocomotionAnimancerPresenter] Invalid Animancer transition for key '{key}'. {DescribeTransition(group.Transition)}"));
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

        string ResolveAliasKey(in MovementAnimationContext context)
        {
            return ResolveAliasKey(context.Phase, context.Gait);
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
                    BasicMovementPhase.TurnBack => TurnBackAliasKey,
                    _ => IdleKey
                };
            }

            return phase switch
            {
                BasicMovementPhase.MoveStart => RunStartKey,
                BasicMovementPhase.MoveLoop => RunLoopKey,
                BasicMovementPhase.MoveStop => RunEndKey,
                BasicMovementPhase.TurnBack => TurnBackAliasKey,
                _ => IdleKey
            };
        }

        BasicMovementGait ResolveGaitForAlias(BasicMovementPhase phase, string aliasKey, BasicMovementGait fallback)
        {
            if (string.Equals(aliasKey, ResolveAliasKey(phase, BasicMovementGait.Walk), System.StringComparison.Ordinal))
                return BasicMovementGait.Walk;
            if (string.Equals(aliasKey, ResolveAliasKey(phase, BasicMovementGait.Run), System.StringComparison.Ordinal))
                return BasicMovementGait.Run;

            return fallback;
        }

        bool EnsureAnimancer()
        {
            if (animancer == null)
                animancer = GetComponent<AnimancerComponent>();

            ApplyRootMotionPolicy();
            return animancer != null;
        }

        bool ClearPlaybackProgress()
        {
            if (animancer == null)
                animancer = GetComponent<AnimancerComponent>();
            if (animancer != null)
                animancer.Stop();

            currentPhase = (BasicMovementPhase)(-1);
            currentGait = BasicMovementGait.Walk;
            currentKey = null;
            currentAliasKey = string.Empty;
            currentState = null;
            currentNormalizedTime = 0f;
            hasRestoredPlaybackResume = false;
            CurrentSpeed = 0f;
            return true;
        }

        void LogPresentProbe(in MovementAnimationContext context, string aliasKey, StringReference nextKey)
        {
            bool relevant = context.Phase == BasicMovementPhase.MoveStop ||
                            context.Phase == BasicMovementPhase.TurnBack ||
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

        void ApplyRootMotionPolicy(bool forceEnable = false)
        {
            AnimatorRootMotionController controller = ResolveRootMotionController();
            if (controller == null)
            {
                if (forceEnable)
                    LogTurnBackRootMotionProbe("policy-missing-controller", animancer != null ? animancer.Animator : null, null, false, false, Vector3.zero, 0f, true);
                return;
            }

            controller.SetLocomotionRootMotion(false, disableAnimatorRootMotion, "locomotion-default");
            if (forceEnable || IsTurnBackRootMotionProbeRelevant())
                LogTurnBackRootMotionProbe("policy", animancer != null ? animancer.Animator : null, controller, controller.ManualRootMotionActive, false, Vector3.zero, 0f, forceEnable);
        }

        AnimatorRootMotionController ResolveRootMotionController()
        {
            if (rootMotionController != null)
                return rootMotionController;

            rootMotionController = AnimatorRootMotionController.Resolve(animancer);
            return rootMotionController;
        }

        bool IsTurnBackRootMotionProbeRelevant()
        {
            return currentPhase == BasicMovementPhase.TurnBack ||
                   string.Equals(currentAliasKey, TurnBackAliasKey, System.StringComparison.Ordinal);
        }

        void LogTurnBackRootMotionProbe(
            string probe,
            Animator animator,
            AnimatorRootMotionController controller,
            bool manualRootMotionActive,
            bool hasDelta,
            Vector3 worldDelta,
            float yawDelta,
            bool forceLog = false)
        {
            if (!forceLog && !IsTurnBackRootMotionProbeRelevant())
                return;

            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Animation,
                RuntimeDiagnosticLogLevel.Info,
                "turnback-root-motion-probe",
                currentAliasKey,
                string.Empty,
                0,
                Time.frameCount,
                $"[{TurnBackRootMotionLogKeyword}] probe={probe} phase={currentPhase} alias={currentAliasKey} animation={CurrentAnimationName} gameObject={name} animatorObject={(animator != null ? animator.gameObject.name : "null")} animatorEnabled={(animator != null && animator.enabled)} animatorActive={(animator != null && animator.gameObject.activeInHierarchy)} applyRootMotion={(animator != null && animator.applyRootMotion)} cullingMode={(animator != null ? animator.cullingMode.ToString() : "null")} updateMode={(animator != null ? animator.updateMode.ToString() : "null")} controllerPresent={(controller != null)} controllerManualRootMotionActive={manualRootMotionActive} locomotionForce={(controller != null && controller.LocomotionForceRequested)} locomotionDisable={(controller != null && controller.LocomotionDisableRequested)} actionDisable={(controller != null && controller.ActionDisableRequested)} rootMotionDisabled={disableAnimatorRootMotion} hasDelta={hasDelta} worldDelta={worldDelta.ToString("F3")} yawDelta={yawDelta:F3} pendingHasDelta=False pendingWorldDelta={Vector3.zero.ToString("F3")} pendingYawDelta=0.000 stateTime={(currentState != null ? currentState.Time : 0f):F3} normalized={(currentState != null ? currentState.NormalizedTime : 0f):F3} length={(currentState != null ? currentState.Length : 0f):F3} visualPosition={(animator != null ? animator.transform.position.ToString("F3") : Vector3.zero.ToString("F3"))} visualYaw={(animator != null ? animator.transform.eulerAngles.y : 0f):F3}",
                TurnBackRootMotionProbeChannel));
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

            float normalizedTime = currentNormalizedTime;
            return new AnimationPhasePlaybackProgress(
                currentPhase,
                currentAliasKey,
                normalizedTime,
                true,
                normalizedTime >= currentState.NormalizedEndTime);
        }
    }
}
