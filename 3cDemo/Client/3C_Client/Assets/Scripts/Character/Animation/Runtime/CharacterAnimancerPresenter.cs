using Animancer;
using Animancer.TransitionLibraries;
using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using ThirdPersonDiagnostics;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAnimation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AnimancerComponent))]
    public sealed class CharacterAnimancerPresenter :
        MonoBehaviour,
        ICharacterAnimationOutputPresenter,
        ILocomotionAnimationPresenter,
        IActionAnimationPresenter,
        IActionAnimationPlaybackProgressController
    {
        const string TurnBackRootMotionLogKeyword = "TURNBACK_RM_CHAIN";
        const string TurnBackRootMotionProbeChannel = "Animation.turnback-root-motion-probe";
        const string TurnBackAliasKey = "Locomotion.Turn.Back";
        const string RunEndKey = "RunEnd";

        [SerializeField] AnimancerComponent animancer;
        [SerializeField] bool disableAnimatorRootMotion = true;

        AnimatorRootMotionController rootMotionController;
        RunLocomotionAnimationConfigSO locomotionAnimationConfig;
        BasicMovementPhase currentLocomotionPhase = (BasicMovementPhase)(-1);
        BasicMovementGait currentLocomotionGait = BasicMovementGait.Walk;
        StringReference currentLocomotionKey;
        string currentLocomotionAliasKey = string.Empty;
        AnimancerState currentLocomotionState;
        StringReference lastInvalidLocomotionKey;
        float currentLocomotionNormalizedTime;
        bool hasRestoredLocomotionPlaybackResume;
        AnimancerState currentActionState;
        ActionAnimationKey currentActionKey;
        ActionAnimationPlaybackIntent currentActionPlaybackIntent;
        CharacterAnimationPlaybackDomain activeDomain;
        string activeStableKey = string.Empty;
        int currentSourceStep;

        public BasicMovementPhase CurrentPhase => currentLocomotionPhase;
        public BasicMovementGait CurrentGait => currentLocomotionGait;
        public AnimationPhasePlaybackProgress CurrentPlaybackProgress => BuildLocomotionPlaybackProgress();
        public string CurrentAnimationName => CurrentLocomotionAnimationName;
        public string CurrentLocomotionAnimationName => AnimationName(currentLocomotionState);
        public string CurrentActionAnimationName => AnimationName(currentActionState);
        public float CurrentSpeed { get; private set; }
        public ActionAnimationKey CurrentKey => currentActionKey;
        public float CurrentNormalizedTime => currentActionState != null ? currentActionState.NormalizedTime : 0f;
        public bool HasValidPlayback => currentActionState != null && currentActionKey.IsValid;
        public ActionAnimationPlaybackProgress CurrentActionPlaybackProgress => BuildActionPlaybackProgress();
        public CharacterAnimationPlaybackSnapshot CurrentSnapshot => new CharacterAnimationPlaybackSnapshot(
            activeDomain,
            activeStableKey,
            BuildLocomotionPlaybackProgress(),
            CurrentLocomotionAnimationName,
            BuildActionPlaybackProgress(),
            CurrentActionAnimationName,
            currentSourceStep);

        ActionAnimationPlaybackProgress IActionAnimationPresenter.CurrentPlaybackProgress => BuildActionPlaybackProgress();
        string IActionAnimationPresenter.CurrentAnimationName => CurrentActionAnimationName;

        void Reset()
        {
            animancer = GetComponent<AnimancerComponent>();
        }

        void Awake()
        {
            if (animancer == null)
                animancer = GetComponent<AnimancerComponent>();

            ApplyLocomotionRootMotionPolicy();
            ApplyActionRootMotionPolicy(false, "awake");
        }

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
            if (currentLocomotionPhase != BasicMovementPhase.TurnBack)
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
                    currentLocomotionAliasKey,
                    string.Empty,
                    0,
                    Time.frameCount,
                    $"[{TurnBackRootMotionLogKeyword}] stage=presenter-skipped phase={currentLocomotionPhase} alias={currentLocomotionAliasKey} animation={CurrentLocomotionAnimationName} applyRootMotion={skippedAnimator.applyRootMotion} manualRootMotionActive={manualRootMotionActive} stateTime={(currentLocomotionState != null ? currentLocomotionState.Time : 0f):F3} normalized={(currentLocomotionState != null ? currentLocomotionState.NormalizedTime : 0f):F3} length={(currentLocomotionState != null ? currentLocomotionState.Length : 0f):F3} visualPosition={skippedAnimator.transform.position.ToString("F3")} visualYaw={skippedAnimator.transform.eulerAngles.y:F3}"));
                return;
            }

            Vector3 worldDelta = animator.deltaPosition;
            worldDelta.y = 0f;
            float yawDelta = Mathf.DeltaAngle(0f, animator.deltaRotation.eulerAngles.y);
            bool hasDelta = worldDelta.sqrMagnitude > 0.000001f || Mathf.Abs(yawDelta) > 0.0001f;
            LogTurnBackRootMotionProbe("delta-ignored", animator, controller, manualRootMotionActive, hasDelta, worldDelta, yawDelta);
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Animation,
                RuntimeDiagnosticLogLevel.Trace,
                "locomotion-root-motion-delta",
                currentLocomotionAliasKey,
                string.Empty,
                0,
                Time.frameCount,
                $"[{TurnBackRootMotionLogKeyword}] stage=presenter-delta-ignored phase={currentLocomotionPhase} alias={currentLocomotionAliasKey} animation={CurrentLocomotionAnimationName} applyRootMotion={animator.applyRootMotion} manualRootMotionActive={manualRootMotionActive} hasDelta={hasDelta} worldDelta={worldDelta.ToString("F3")} yawDelta={yawDelta:F3} pendingWorldDelta={Vector3.zero.ToString("F3")} pendingYawDelta=0.000 stateTime={(currentLocomotionState != null ? currentLocomotionState.Time : 0f):F3} normalized={(currentLocomotionState != null ? currentLocomotionState.NormalizedTime : 0f):F3} length={(currentLocomotionState != null ? currentLocomotionState.Length : 0f):F3} visualPosition={animator.transform.position.ToString("F3")} visualYaw={animator.transform.eulerAngles.y:F3}"));
        }

        public void PresentLocomotion(in MovementAnimationContext context)
        {
            Present(in context);
        }

        public void Present(in MovementAnimationContext context)
        {
            CurrentSpeed = context.PlanarSpeed;
            locomotionAnimationConfig = context.AnimationConfig;

            if (animancer == null)
                animancer = GetComponent<AnimancerComponent>();

            ApplyLocomotionRootMotionPolicy();

            if (animancer == null)
            {
                LogLocomotionPlayback("locomotion-animation-missing-animancer", RuntimeDiagnosticLogLevel.Warning, in context, string.Empty, null);
                return;
            }

            string aliasKey = ResolveAliasKey(in context);
            CharacterAnimationPlaybackRequest request = CharacterAnimationPlaybackRequest.FromLocomotion(in context, aliasKey);
            StringReference nextKey = StringReference.Get(request.StableKey);
            LogLocomotionPresentProbe(in context, request.StableKey, nextKey);
            bool samePlayback = currentLocomotionPhase == context.Phase &&
                                currentLocomotionGait == context.Gait &&
                                currentLocomotionKey == nextKey;
            if (samePlayback &&
                currentLocomotionState != null &&
                currentLocomotionState.IsCurrent)
            {
                activeDomain = CharacterAnimationPlaybackDomain.Locomotion;
                activeStableKey = request.StableKey;
                currentSourceStep = request.SourceStep;
                hasRestoredLocomotionPlaybackResume = false;
                return;
            }

            if (!CanPlayLocomotion(nextKey, in context))
                return;

            AnimancerState nextState = animancer.TryPlay(nextKey);
            if (nextState == null)
            {
                LogLocomotionPlayback("locomotion-animation-play-failed", RuntimeDiagnosticLogLevel.Warning, in context, request.StableKey, null);
                return;
            }

            currentLocomotionPhase = context.Phase;
            currentLocomotionGait = context.Gait;
            currentLocomotionKey = nextKey;
            currentLocomotionAliasKey = request.StableKey;
            currentLocomotionState = nextState;
            if (hasRestoredLocomotionPlaybackResume && samePlayback)
            {
                nextState.NormalizedTime = currentLocomotionNormalizedTime;
            }
            else
            {
                RestartOneShotStateIfNeeded(in context, nextState);
                ApplyEntryFootPhaseMatchIfNeeded(in context, request.StableKey, nextState);
                currentLocomotionNormalizedTime = nextState.NormalizedTime;
            }

            hasRestoredLocomotionPlaybackResume = false;
            currentLocomotionNormalizedTime = nextState.NormalizedTime;
            activeDomain = CharacterAnimationPlaybackDomain.Locomotion;
            activeStableKey = request.StableKey;
            currentSourceStep = request.SourceStep;
            LogLocomotionPlayback("locomotion-animation-played", RuntimeDiagnosticLogLevel.Info, in context, request.StableKey, nextState);
        }

        public bool PresentAction(in CharacterStateAnimationRequest request)
        {
            return Present(in request);
        }

        public bool Present(in CharacterStateAnimationRequest request)
        {
            CharacterAnimationPlaybackRequest playbackRequest = CharacterAnimationPlaybackRequest.FromAction(in request);
            if (!request.HasKey)
            {
                LogActionPlayback("action-animation-missing-key", RuntimeDiagnosticLogLevel.Warning, in request, null);
                return false;
            }

            if (!request.HasActionPlaybackIntent)
            {
                LogActionPlayback("action-animation-missing-playback-intent", RuntimeDiagnosticLogLevel.Warning, in request, null);
                return false;
            }

            if (animancer == null)
                animancer = GetComponent<AnimancerComponent>();

            ApplyActionRootMotionPolicy(disableAnimatorRootMotion, "action-animation");

            if (animancer == null)
            {
                LogActionPlayback("action-animation-missing-animancer", RuntimeDiagnosticLogLevel.Warning, in request, null);
                return false;
            }

            bool samePlayback = currentActionKey == request.Key &&
                                currentActionPlaybackIntent == playbackRequest.ActionPlaybackIntent &&
                                currentActionState != null &&
                                currentActionState.IsCurrent;
            if (samePlayback)
            {
                activeDomain = CharacterAnimationPlaybackDomain.Action;
                activeStableKey = playbackRequest.StableKey;
                currentSourceStep = request.SourceStep;
                return true;
            }

            AnimancerState nextState = TryPlayActionKey(request.Key);
            if (nextState == null)
            {
                LogActionPlayback("action-animation-play-failed", RuntimeDiagnosticLogLevel.Warning, in request, null);
                return false;
            }

            nextState.NormalizedTime = 0f;
            currentActionKey = request.Key;
            currentActionPlaybackIntent = playbackRequest.ActionPlaybackIntent;
            currentActionState = nextState;
            activeDomain = CharacterAnimationPlaybackDomain.Action;
            activeStableKey = playbackRequest.StableKey;
            currentSourceStep = request.SourceStep;
            LogActionPlayback("action-animation-played", RuntimeDiagnosticLogLevel.Info, in request, nextState);
            return true;
        }

        public void ClearActionPlayback()
        {
            Clear();
        }

        public void Clear()
        {
            if (currentActionState == null && !currentActionKey.IsValid && !currentActionPlaybackIntent.IsValid)
                return;

            AnimancerState previousState = currentActionState;
            ActionAnimationKey previousKey = currentActionKey;

            currentActionState = null;
            currentActionKey = default;
            currentActionPlaybackIntent = ActionAnimationPlaybackIntent.Invalid;
            if (activeDomain == CharacterAnimationPlaybackDomain.Action)
            {
                activeDomain = currentLocomotionState != null
                    ? CharacterAnimationPlaybackDomain.Locomotion
                    : CharacterAnimationPlaybackDomain.None;
                activeStableKey = currentLocomotionState != null ? currentLocomotionAliasKey : string.Empty;
            }

            ApplyActionRootMotionPolicy(false, "action-animation-cleared");
            LogActionClear(previousKey, previousState);
        }

        public bool RestorePlaybackProgress(in AnimationPhasePlaybackProgress progress)
        {
            return RestorePlaybackProgress(in progress, ResolveGaitForAlias(progress.Phase, progress.AliasKey, currentLocomotionGait));
        }

        public bool RestorePlaybackProgress(in AnimationPhasePlaybackProgress progress, BasicMovementGait gait)
        {
            if (!progress.HasValidPlayback || string.IsNullOrWhiteSpace(progress.AliasKey))
                return ClearLocomotionPlaybackProgress();

            if (!EnsureAnimancer())
                return false;

            StringReference key = StringReference.Get(progress.AliasKey);
            AnimancerState state = animancer.TryPlay(key);
            if (state == null)
                return false;

            currentLocomotionPhase = progress.Phase;
            currentLocomotionGait = gait;
            currentLocomotionKey = key;
            currentLocomotionAliasKey = progress.AliasKey;
            currentLocomotionState = state;
            currentLocomotionNormalizedTime = progress.NormalizedTime;
            currentLocomotionState.NormalizedTime = currentLocomotionNormalizedTime;
            hasRestoredLocomotionPlaybackResume = true;
            activeDomain = CharacterAnimationPlaybackDomain.Locomotion;
            activeStableKey = progress.AliasKey;
            return true;
        }

        public AnimationPhasePlaybackProgress AdvancePlayback(float deltaTime)
        {
            if (currentLocomotionState == null)
                return AnimationPhasePlaybackProgress.Invalid(currentLocomotionPhase);

            float length = currentLocomotionState.Length;
            float speed = currentLocomotionState.EffectiveSpeed;
            if (length > 0f)
                currentLocomotionNormalizedTime = Mathf.Max(0f, currentLocomotionNormalizedTime + Mathf.Max(0f, deltaTime) * speed / length);

            currentLocomotionState.NormalizedTime = currentLocomotionNormalizedTime;
            return BuildLocomotionPlaybackProgress();
        }

        public bool RestorePlaybackProgress(in ActionAnimationPlaybackProgress progress, string animationName)
        {
            if (!progress.HasValidPlayback)
            {
                Clear();
                return true;
            }

            if (!progress.HasPlaybackIntent)
                return false;

            if (!EnsureAnimancer())
                return false;

            AnimancerState state = TryPlayActionKey(progress.Key);
            if (state == null)
                return false;

            currentActionKey = progress.Key;
            currentActionPlaybackIntent = progress.PlaybackIntent;
            currentActionState = state;
            currentActionState.NormalizedTime = progress.NormalizedTime;
            activeDomain = CharacterAnimationPlaybackDomain.Action;
            activeStableKey = progress.Key.Value;
            return true;
        }

        static void RestartOneShotStateIfNeeded(in MovementAnimationContext context, AnimancerState state)
        {
            if (state == null || context.Phase != BasicMovementPhase.TurnBack)
                return;

            state.NormalizedTime = context.HasTurnBackMotionPolicy
                ? context.TurnBackMotionPolicy.StartNormalizedTime
                : 0f;
        }

        void ApplyEntryFootPhaseMatchIfNeeded(
            in MovementAnimationContext context,
            string aliasKey,
            AnimancerState state)
        {
            if (state == null || !context.HasEntryFootPhaseMatchRequest)
                return;

            LocomotionFootPhaseMatchResult result = context.EntryFootPhaseMatchResult;
            bool runLoopTarget =
                context.Phase == BasicMovementPhase.MoveLoop &&
                context.Gait == BasicMovementGait.Run &&
                result.IsValid &&
                string.Equals(result.TargetAliasKey, aliasKey, System.StringComparison.Ordinal);
            if (runLoopTarget)
            {
                state.NormalizedTime = result.StartNormalizedTime;
                LogFootPhaseMatch("locomotion-foot-phase-match-applied", RuntimeDiagnosticLogLevel.Info, in context, aliasKey, state);
                return;
            }

            LogFootPhaseMatch("locomotion-foot-phase-match-skipped", RuntimeDiagnosticLogLevel.Warning, in context, aliasKey, state);
        }

        bool CanPlayLocomotion(StringReference key, in MovementAnimationContext context)
        {
            TransitionLibrary library = animancer.Graph.Transitions;
            if (library == null)
            {
                LogLocomotionPlayback("locomotion-animation-missing-library", RuntimeDiagnosticLogLevel.Warning, in context, key);
                return false;
            }

            if (!library.TryGetTransition(key, out TransitionModifierGroup group))
            {
                LogLocomotionPlayback("locomotion-animation-missing-transition", RuntimeDiagnosticLogLevel.Warning, in context, key);
                return false;
            }

            if (group.Transition.IsValid())
                return true;

            if (lastInvalidLocomotionKey != key)
            {
                lastInvalidLocomotionKey = key;
                LogLocomotionPlayback("locomotion-animation-invalid-transition", RuntimeDiagnosticLogLevel.Warning, in context, key);
                RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                    RuntimeDiagnosticLogCategory.Animation,
                    RuntimeDiagnosticLogLevel.Error,
                    "invalid-animancer-transition",
                    string.Empty,
                    string.Empty,
                    0,
                    Time.frameCount,
                    $"[CharacterAnimancerPresenter] Invalid Animancer transition for key '{key}'. {DescribeTransition(group.Transition)}"));
            }

            return false;
        }

        AnimancerState TryPlayActionKey(ActionAnimationKey key)
        {
            if (!key.IsValid)
                return null;

            StringReference libraryKey = StringReference.Get(key.Value);
            return animancer.TryPlay(libraryKey);
        }

        bool EnsureAnimancer()
        {
            if (animancer == null)
                animancer = GetComponent<AnimancerComponent>();

            if (animancer == null)
                return false;

            ApplyLocomotionRootMotionPolicy();
            return true;
        }

        bool ClearLocomotionPlaybackProgress()
        {
            if (animancer == null)
                animancer = GetComponent<AnimancerComponent>();
            if (animancer != null)
                animancer.Stop();

            currentLocomotionPhase = (BasicMovementPhase)(-1);
            currentLocomotionGait = BasicMovementGait.Walk;
            currentLocomotionKey = null;
            currentLocomotionAliasKey = string.Empty;
            currentLocomotionState = null;
            currentLocomotionNormalizedTime = 0f;
            hasRestoredLocomotionPlaybackResume = false;
            CurrentSpeed = 0f;
            if (activeDomain == CharacterAnimationPlaybackDomain.Locomotion)
            {
                activeDomain = currentActionState != null
                    ? CharacterAnimationPlaybackDomain.Action
                    : CharacterAnimationPlaybackDomain.None;
                activeStableKey = currentActionState != null ? currentActionKey.Value : string.Empty;
            }

            return true;
        }

        string ResolveAliasKey(in MovementAnimationContext context)
        {
            return LocomotionAnimationAliasResolver.ResolveAliasKey(context.AnimationConfig, in context);
        }

        BasicMovementGait ResolveGaitForAlias(BasicMovementPhase phase, string aliasKey, BasicMovementGait fallback)
        {
            return LocomotionAnimationAliasResolver.ResolveGaitForAlias(locomotionAnimationConfig, phase, aliasKey, fallback);
        }

        void ApplyLocomotionRootMotionPolicy(bool forceEnable = false)
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

        void ApplyActionRootMotionPolicy(bool disabled, string reason)
        {
            AnimatorRootMotionController controller = ResolveRootMotionController();
            if (controller != null)
                controller.SetActionRootMotionDisabled(disabled, reason);
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
            return currentLocomotionPhase == BasicMovementPhase.TurnBack ||
                   string.Equals(currentLocomotionAliasKey, TurnBackAliasKey, System.StringComparison.Ordinal);
        }

        void LogLocomotionPresentProbe(in MovementAnimationContext context, string aliasKey, StringReference nextKey)
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
                currentLocomotionAliasKey,
                0,
                Time.frameCount,
                $"phase={context.Phase} gait={context.Gait} alias={aliasKey} currentPhase={currentLocomotionPhase} currentGait={currentLocomotionGait} currentAlias={currentLocomotionAliasKey} sameKey={(currentLocomotionKey == nextKey)} currentStateCurrent={(currentLocomotionState != null && currentLocomotionState.IsCurrent)} currentAnimation={CurrentLocomotionAnimationName} planarSpeed={context.PlanarSpeed:F3}"));
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
                currentLocomotionAliasKey,
                string.Empty,
                0,
                Time.frameCount,
                $"[{TurnBackRootMotionLogKeyword}] probe={probe} phase={currentLocomotionPhase} alias={currentLocomotionAliasKey} animation={CurrentLocomotionAnimationName} gameObject={name} animatorObject={(animator != null ? animator.gameObject.name : "null")} animatorEnabled={(animator != null && animator.enabled)} animatorActive={(animator != null && animator.gameObject.activeInHierarchy)} applyRootMotion={(animator != null && animator.applyRootMotion)} cullingMode={(animator != null ? animator.cullingMode.ToString() : "null")} updateMode={(animator != null ? animator.updateMode.ToString() : "null")} controllerPresent={(controller != null)} controllerManualRootMotionActive={manualRootMotionActive} locomotionForce={(controller != null && controller.LocomotionForceRequested)} locomotionDisable={(controller != null && controller.LocomotionDisableRequested)} actionDisable={(controller != null && controller.ActionDisableRequested)} rootMotionDisabled={disableAnimatorRootMotion} hasDelta={hasDelta} worldDelta={worldDelta.ToString("F3")} yawDelta={yawDelta:F3} pendingHasDelta=False pendingWorldDelta={Vector3.zero.ToString("F3")} pendingYawDelta=0.000 stateTime={(currentLocomotionState != null ? currentLocomotionState.Time : 0f):F3} normalized={(currentLocomotionState != null ? currentLocomotionState.NormalizedTime : 0f):F3} length={(currentLocomotionState != null ? currentLocomotionState.Length : 0f):F3} visualPosition={(animator != null ? animator.transform.position.ToString("F3") : Vector3.zero.ToString("F3"))} visualYaw={(animator != null ? animator.transform.eulerAngles.y : 0f):F3}",
                TurnBackRootMotionProbeChannel));
        }

        void LogLocomotionPlayback(
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
                currentLocomotionAliasKey,
                0,
                Time.frameCount,
                $"phase={context.Phase} gait={context.Gait} hasMove={context.HasMoveIntent} strength={context.InputStrength:F3} speed={context.PlanarSpeed:F3} direction={context.WorldDirection.ToString("F3")} alias={aliasKey} previousAlias={currentLocomotionAliasKey} currentAnimation={CurrentLocomotionAnimationName} nextAnimation={AnimationName(state)} normalized={(state != null ? state.NormalizedTime : 0f):F3} rootMotionDisabled={disableAnimatorRootMotion}"));
        }

        void LogLocomotionPlayback(
            string message,
            RuntimeDiagnosticLogLevel level,
            in MovementAnimationContext context,
            StringReference aliasKey)
        {
            LogLocomotionPlayback(message, level, in context, aliasKey.ToString(), null);
        }

        void LogFootPhaseMatch(
            string message,
            RuntimeDiagnosticLogLevel level,
            in MovementAnimationContext context,
            string aliasKey,
            AnimancerState state)
        {
            LocomotionFootPhaseMatchResult result = context.EntryFootPhaseMatchResult;
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Animation,
                level,
                message,
                aliasKey,
                currentLocomotionAliasKey,
                0,
                Time.frameCount,
                $"phase={context.Phase} gait={context.Gait} alias={aliasKey} previousAlias={currentLocomotionAliasKey} requested={context.HasEntryFootPhaseMatchRequest} valid={result.IsValid} matchedPhase={result.MatchedPhase} targetAlias={result.TargetAliasKey} targetNormalized={result.StartNormalizedTime:F3} appliedNormalized={(state != null ? state.NormalizedTime : 0f):F3} reason={result.Reason}"));
        }

        void LogActionPlayback(
            string message,
            RuntimeDiagnosticLogLevel level,
            in CharacterStateAnimationRequest request,
            AnimancerState state)
        {
            CharacterStateAnimationBinding binding = request.Binding;
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Animation,
                level,
                message,
                request.Key.Value,
                currentActionKey.Value,
                request.SourceStep,
                Time.frameCount,
                $"key={request.Key.Value} previousKey={currentActionKey.Value} sourceStep={request.SourceStep} timelineBinding={binding.TimelineBindingKey} debugName={binding.DebugName} currentAnimation={CurrentActionAnimationName} nextAnimation={AnimationName(state)} normalized={(state != null ? state.NormalizedTime : 0f):F3} rootMotionDisabled={disableAnimatorRootMotion}"));
        }

        void LogActionClear(ActionAnimationKey previousKey, AnimancerState previousState)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Animation,
                RuntimeDiagnosticLogLevel.Info,
                "action-animation-cleared",
                string.Empty,
                previousKey.Value,
                0,
                Time.frameCount,
                $"previousKey={previousKey.Value} previousAnimation={AnimationName(previousState)} previousNormalized={(previousState != null ? previousState.NormalizedTime : 0f):F3} releasedForLocomotionBlend=True"));
        }

        AnimationPhasePlaybackProgress BuildLocomotionPlaybackProgress()
        {
            if (currentLocomotionState == null)
                return AnimationPhasePlaybackProgress.Invalid(currentLocomotionPhase);

            float normalizedTime = currentLocomotionNormalizedTime;
            return new AnimationPhasePlaybackProgress(
                currentLocomotionPhase,
                currentLocomotionAliasKey,
                normalizedTime,
                true,
                normalizedTime >= currentLocomotionState.NormalizedEndTime);
        }

        ActionAnimationPlaybackProgress BuildActionPlaybackProgress()
        {
            bool hasPlayback = currentActionState != null && currentActionKey.IsValid;
            float normalizedTime = hasPlayback ? currentActionState.NormalizedTime : 0f;
            return new ActionAnimationPlaybackProgress(
                currentActionKey,
                normalizedTime,
                hasPlayback,
                hasPlayback && normalizedTime >= 1f,
                currentActionPlaybackIntent);
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

        static string AnimationName(AnimancerState state)
        {
            if (state == null)
                return string.Empty;

            Object mainObject = state.MainObject;
            if (mainObject != null)
                return mainObject.name;

            return state.Clip != null ? state.Clip.name : string.Empty;
        }
    }
}
