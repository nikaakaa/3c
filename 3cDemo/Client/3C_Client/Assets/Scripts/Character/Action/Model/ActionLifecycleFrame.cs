using ThirdPersonCharacterStateMachine;
using UnityEngine;

namespace ThirdPersonAction
{
    public readonly struct ActionLifecycleFrame
    {
        public ActionLifecycleFrame(
            CharacterResolvedAction action,
            ActionMotionSpec motionSpec,
            CharacterStateAnimationRequest animationRequest,
            bool hasAnimationRequest,
            bool startedThisFrame,
            bool exitedThisFrame,
            int sourceStep)
            : this(
                action,
                motionSpec,
                animationRequest,
                hasAnimationRequest,
                startedThisFrame,
                exitedThisFrame,
                sourceStep,
                ActionBranchOutcome.None(sourceStep))
        {
        }

        public ActionLifecycleFrame(
            CharacterResolvedAction action,
            ActionMotionSpec motionSpec,
            CharacterStateAnimationRequest animationRequest,
            bool hasAnimationRequest,
            bool startedThisFrame,
            bool exitedThisFrame,
            int sourceStep,
            ActionBranchOutcome actionBranchOutcome)
        {
            int sanitizedSourceStep = sourceStep < 0 ? 0 : sourceStep;
            Action = action;
            MotionSpec = motionSpec;
            AnimationRequest = animationRequest;
            HasAnimationRequest = hasAnimationRequest;
            StartedThisFrame = startedThisFrame;
            ExitedThisFrame = exitedThisFrame;
            SourceStep = sanitizedSourceStep;
            ActionBranchOutcome = actionBranchOutcome.HasOutcome
                ? actionBranchOutcome
                : ActionBranchOutcome.None(sanitizedSourceStep);
        }

        public CharacterResolvedAction Action { get; }
        public ActionMotionSpec MotionSpec { get; }
        public CharacterStateAnimationRequest AnimationRequest { get; }
        public ActionBranchOutcome ActionBranchOutcome { get; }
        public bool HasAnimationRequest { get; }
        public bool StartedThisFrame { get; }
        public bool ExitedThisFrame { get; }
        public int SourceStep { get; }
        public bool HasAction => Action.HasResolvedAction && MotionSpec.HasSpec;
        public ActionStateId ActionState => HasAction ? MotionSpec.ActionState : ActionStateIds.None;
        public bool HasActionBranchOutcome => ActionBranchOutcome.HasOutcome;

        public static ActionLifecycleFrame None(int sourceStep, bool exitedThisFrame = false)
        {
            return new ActionLifecycleFrame(
                default,
                ActionMotionSpec.None(sourceStep),
                default,
                false,
                false,
                exitedThisFrame,
                sourceStep);
        }

        public static ActionLifecycleFrame FromResolvedAction(
            in CharacterResolvedAction action,
            float stateTime,
            bool startedThisFrame,
            bool exitedThisFrame,
            int sourceStep,
            ActionAnimationPlaybackIntent playbackIntent = default,
            ActionBranchOutcome actionBranchOutcome = default)
        {
            ActionTimelineOutcome timelineOutcome = actionBranchOutcome.TimelineOutcome;
            ActionMotionSpec spec = timelineOutcome.HasMotion
                ? timelineOutcome.MotionSpec
                : action.MotionSpec;
            ActionMotionSpec motionSpec = new ActionMotionSpec(
                spec.ActionState,
                spec.SourceState,
                spec.Variant,
                spec.Duration,
                spec.Distance,
                spec.RotateToDirection,
                spec.SetRunLatchOnComplete,
                spec.LockedWorldDirection,
                stateTime,
                sourceStep);
            ActionAnimationKey animationKey = timelineOutcome.HasAnimation
                ? timelineOutcome.AnimationKey
                : action.AnimationKey;
            CharacterStateAnimationRequest animationRequest = default;
            bool hasAnimation = animationKey.IsValid;
            if (hasAnimation)
            {
                CharacterStateAnimationBinding binding =
                    CharacterStateAnimationBinding.FromKey(animationKey.Value, animationKey.Value);
                animationRequest = new CharacterStateAnimationRequest(
                    binding,
                    CharacterStatePlaybackFactSource.Action,
                    sourceStep,
                    playbackIntent);
            }

            return new ActionLifecycleFrame(
                action,
                motionSpec,
                animationRequest,
                hasAnimation,
                startedThisFrame,
                exitedThisFrame,
                sourceStep,
                actionBranchOutcome);
        }

        public ActionLifecycleFrame WithActionBranchOutcome(ActionBranchOutcome actionBranchOutcome)
        {
            return new ActionLifecycleFrame(
                Action,
                MotionSpec,
                AnimationRequest,
                HasAnimationRequest,
                StartedThisFrame,
                ExitedThisFrame,
                SourceStep,
                actionBranchOutcome);
        }
    }

    public readonly struct ActionLifecycleRestoreState
    {
        public ActionLifecycleRestoreState(
            bool hasActiveAction,
            CharacterResolvedAction activeAction,
            float stateTime,
            bool exitedThisFrame)
            : this(
                hasActiveAction,
                activeAction,
                stateTime,
                exitedThisFrame,
                ActionAnimationPlaybackIntent.Invalid,
                0)
        {
        }

        public ActionLifecycleRestoreState(
            bool hasActiveAction,
            CharacterResolvedAction activeAction,
            float stateTime,
            bool exitedThisFrame,
            ActionAnimationPlaybackIntent activePlaybackIntent,
            int nextPlaybackIntentValue)
        {
            HasActiveAction = hasActiveAction && activeAction.HasResolvedAction;
            ActiveAction = HasActiveAction ? activeAction : default;
            StateTime = Mathf.Max(0f, stateTime);
            ExitedThisFrame = exitedThisFrame;
            ActivePlaybackIntent = HasActiveAction ? activePlaybackIntent : ActionAnimationPlaybackIntent.Invalid;
            NextPlaybackIntentValue = Mathf.Max(nextPlaybackIntentValue, ActivePlaybackIntent.Value);
        }

        public bool HasActiveAction { get; }
        public CharacterResolvedAction ActiveAction { get; }
        public float StateTime { get; }
        public bool ExitedThisFrame { get; }
        public ActionAnimationPlaybackIntent ActivePlaybackIntent { get; }
        public int NextPlaybackIntentValue { get; }

        public static ActionLifecycleRestoreState Inactive =>
            new ActionLifecycleRestoreState(false, default, 0f, false);
    }

    internal sealed class ActionLifecycleRuntime
    {
        CharacterResolvedAction activeAction;
        ActionAnimationPlaybackIntent activePlaybackIntent;
        float stateTime;
        bool hasActiveAction;
        bool exitedThisFrame;
        int nextPlaybackIntentValue;

        public bool IsActive => hasActiveAction;
        public ActionStateId ActiveActionState => hasActiveAction
            ? activeAction.MotionSpec.ActionState
            : ActionStateIds.None;

        public ActionLifecycleFrame Tick(
            in CharacterResolvedAction acceptedAction,
            float deltaTime,
            int sourceStep)
        {
            CharacterActionCatalog emptyCatalog = CharacterActionCatalog.Empty;
            return Tick(in acceptedAction, in emptyCatalog, deltaTime, sourceStep);
        }

        public ActionLifecycleFrame Tick(
            in CharacterResolvedAction acceptedAction,
            in CharacterActionCatalog actionCatalog,
            float deltaTime,
            int sourceStep)
        {
            bool started = false;
            bool exited = exitedThisFrame;
            exitedThisFrame = false;
            if (acceptedAction.HasResolvedAction)
            {
                activeAction = acceptedAction;
                activePlaybackIntent = CreateNextPlaybackIntent();
                stateTime = 0f;
                hasActiveAction = true;
                started = true;
                exited = false;
            }

            if (!hasActiveAction)
                return ActionLifecycleFrame.None(sourceStep, exited);

            float tickInterval = Mathf.Max(0f, deltaTime);
            stateTime = Mathf.Max(0f, stateTime + tickInterval);
            ActionBranchOutcome actionBranchOutcome = EvaluateActionBranch(
                in actionCatalog,
                stateTime,
                tickInterval,
                sourceStep);
            return ActionLifecycleFrame.FromResolvedAction(
                in activeAction,
                stateTime,
                started,
                exited,
                sourceStep,
                activePlaybackIntent,
                actionBranchOutcome);
        }

        public void Complete(
            in ActionMotionResolveResult result,
            in ActionAnimationPlaybackProgress actionProgress,
            bool requireAnimationEnded)
        {
            if (!hasActiveAction ||
                !result.HasSpec ||
                !result.ActionCompleted ||
                result.Spec.ActionState != activeAction.MotionSpec.ActionState)
            {
                return;
            }

            if (requireAnimationEnded && !MatchesActiveActionAnimationEnd(in actionProgress))
                return;

            ResetActiveAction();
            exitedThisFrame = true;
        }

        public void Reset()
        {
            ResetActiveAction();
            exitedThisFrame = false;
            nextPlaybackIntentValue = 0;
        }

        public ActionLifecycleRestoreState CaptureRestoreState()
        {
            return new ActionLifecycleRestoreState(
                hasActiveAction,
                activeAction,
                stateTime,
                exitedThisFrame,
                activePlaybackIntent,
                nextPlaybackIntentValue);
        }

        public void Restore(in ActionLifecycleRestoreState restoreState)
        {
            hasActiveAction = restoreState.HasActiveAction;
            activeAction = restoreState.ActiveAction;
            activePlaybackIntent = restoreState.ActivePlaybackIntent;
            stateTime = restoreState.StateTime;
            exitedThisFrame = restoreState.ExitedThisFrame;
            nextPlaybackIntentValue = Mathf.Max(restoreState.NextPlaybackIntentValue, activePlaybackIntent.Value);
            if (!hasActiveAction)
                ResetActiveAction();
        }

        void ResetActiveAction()
        {
            activeAction = default;
            activePlaybackIntent = ActionAnimationPlaybackIntent.Invalid;
            stateTime = 0f;
            hasActiveAction = false;
        }

        ActionAnimationPlaybackIntent CreateNextPlaybackIntent()
        {
            nextPlaybackIntentValue = Mathf.Max(1, nextPlaybackIntentValue + 1);
            return new ActionAnimationPlaybackIntent(nextPlaybackIntentValue);
        }

        ActionBranchOutcome EvaluateActionBranch(
            in CharacterActionCatalog actionCatalog,
            float currentStateTime,
            float tickInterval,
            int sourceStep)
        {
            if (!hasActiveAction ||
                !actionCatalog.HasCatalog ||
                !actionCatalog.TryGetActionBranch(activeAction.MotionSpec.ActionState, out ActionBranchDefinition branch))
            {
                return ActionBranchOutcome.None(sourceStep);
            }

            float frameStartTime = Mathf.Max(0f, currentStateTime - tickInterval);
            int currentFrame = ActionTimelineEvaluationInput.ResolveFrame(frameStartTime, tickInterval);
            return ActionBranchEvaluator.Evaluate(new ActionBranchEvaluationInput(branch, currentFrame, sourceStep));
        }

        bool MatchesActiveActionAnimationEnd(in ActionAnimationPlaybackProgress actionProgress)
        {
            ActionAnimationKey animationKey = activeAction.AnimationKey;
            return animationKey.IsValid &&
                actionProgress.HasValidPlayback &&
                actionProgress.Key == animationKey &&
                actionProgress.IsEnded;
        }
    }
}
