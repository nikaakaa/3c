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
                CommittedActionBranchOutcome.None(sourceStep))
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
            CommittedActionBranchOutcome committedActionBranchOutcome)
        {
            int sanitizedSourceStep = sourceStep < 0 ? 0 : sourceStep;
            Action = action;
            MotionSpec = motionSpec;
            AnimationRequest = animationRequest;
            HasAnimationRequest = hasAnimationRequest;
            StartedThisFrame = startedThisFrame;
            ExitedThisFrame = exitedThisFrame;
            SourceStep = sanitizedSourceStep;
            CommittedActionBranchOutcome = committedActionBranchOutcome.HasEvaluation
                ? committedActionBranchOutcome
                : CommittedActionBranchOutcome.None(sanitizedSourceStep);
        }

        public CharacterResolvedAction Action { get; }
        public ActionMotionSpec MotionSpec { get; }
        public CharacterStateAnimationRequest AnimationRequest { get; }
        public CommittedActionBranchOutcome CommittedActionBranchOutcome { get; }
        public bool HasAnimationRequest { get; }
        public bool StartedThisFrame { get; }
        public bool ExitedThisFrame { get; }
        public int SourceStep { get; }
        public bool HasAction => Action.HasResolvedAction && MotionSpec.HasSpec;
        public ActionStateId ActionState => HasAction ? MotionSpec.ActionState : ActionStateIds.None;
        public bool HasCommittedActionBranchOutcome => CommittedActionBranchOutcome.HasOutcome;

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
            CommittedActionBranchOutcome committedActionBranchOutcome = default)
        {
            ActionTimelineOutcome timelineOutcome = committedActionBranchOutcome.TimelineOutcome;
            bool timelineEvaluated = committedActionBranchOutcome.HasEvaluation;
            ActionMotionSpec spec = timelineOutcome.HasMotion
                ? timelineOutcome.MotionSpec
                : timelineEvaluated
                    ? ActionMotionSpec.None(sourceStep)
                    : action.MotionSpec;
            Vector3 lockedWorldDirection = timelineOutcome.HasMotion &&
                                           spec.LockedWorldDirection.sqrMagnitude > 0.000001f
                ? spec.LockedWorldDirection
                : action.MotionSpec.LockedWorldDirection;
            ActionMotionSpec motionSpec = new ActionMotionSpec(
                spec.ActionState,
                spec.SourceState,
                spec.Variant,
                spec.Duration,
                spec.Distance,
                spec.RotateToDirection,
                spec.SetRunLatchOnComplete,
                lockedWorldDirection,
                stateTime,
                sourceStep);
            ActionAnimationKey animationKey = timelineOutcome.HasAnimation
                ? timelineOutcome.AnimationKey
                : timelineEvaluated
                    ? default
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
                committedActionBranchOutcome);
        }

        public ActionLifecycleFrame WithCommittedActionBranchOutcome(CommittedActionBranchOutcome committedActionBranchOutcome)
        {
            return new ActionLifecycleFrame(
                Action,
                MotionSpec,
                AnimationRequest,
                HasAnimationRequest,
                StartedThisFrame,
                ExitedThisFrame,
                SourceStep,
                committedActionBranchOutcome);
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
                0,
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
            : this(
                hasActiveAction,
                activeAction,
                stateTime,
                exitedThisFrame,
                activePlaybackIntent,
                nextPlaybackIntentValue,
                0)
        {
        }

        public ActionLifecycleRestoreState(
            bool hasActiveAction,
            CharacterResolvedAction activeAction,
            float stateTime,
            bool exitedThisFrame,
            ActionAnimationPlaybackIntent activePlaybackIntent,
            int nextPlaybackIntentValue,
            int actionStartStep)
        {
            HasActiveAction = hasActiveAction && activeAction.HasResolvedAction;
            ActiveAction = HasActiveAction ? activeAction : default;
            StateTime = Mathf.Max(0f, stateTime);
            ExitedThisFrame = exitedThisFrame;
            ActivePlaybackIntent = HasActiveAction ? activePlaybackIntent : ActionAnimationPlaybackIntent.Invalid;
            NextPlaybackIntentValue = Mathf.Max(nextPlaybackIntentValue, ActivePlaybackIntent.Value);
            ActionStartStep = Mathf.Max(0, actionStartStep);
        }

        public bool HasActiveAction { get; }
        public CharacterResolvedAction ActiveAction { get; }
        public float StateTime { get; }
        public bool ExitedThisFrame { get; }
        public ActionAnimationPlaybackIntent ActivePlaybackIntent { get; }
        public int NextPlaybackIntentValue { get; }
        public int ActionStartStep { get; }

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
        int actionStartStep;

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
                actionStartStep = Mathf.Max(0, sourceStep);
                hasActiveAction = true;
                started = true;
                exited = false;
            }

            if (!hasActiveAction)
                return ActionLifecycleFrame.None(sourceStep, exited);

            float tickInterval = Mathf.Max(0f, deltaTime);
            int localTick = ResolveLocalTick(sourceStep);
            stateTime = Mathf.Max(0f, (localTick + 1) * tickInterval);
            CommittedActionBranchOutcome committedActionBranchOutcome = EvaluateCommittedActionBranch(
                in actionCatalog,
                localTick,
                sourceStep);
            return ActionLifecycleFrame.FromResolvedAction(
                in activeAction,
                stateTime,
                started,
                exited,
                sourceStep,
                activePlaybackIntent,
                committedActionBranchOutcome);
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
                nextPlaybackIntentValue,
                actionStartStep);
        }

        public void Restore(in ActionLifecycleRestoreState restoreState)
        {
            hasActiveAction = restoreState.HasActiveAction;
            activeAction = restoreState.ActiveAction;
            activePlaybackIntent = restoreState.ActivePlaybackIntent;
            stateTime = restoreState.StateTime;
            exitedThisFrame = restoreState.ExitedThisFrame;
            nextPlaybackIntentValue = Mathf.Max(restoreState.NextPlaybackIntentValue, activePlaybackIntent.Value);
            actionStartStep = restoreState.ActionStartStep;
            if (!hasActiveAction)
                ResetActiveAction();
        }

        void ResetActiveAction()
        {
            activeAction = default;
            activePlaybackIntent = ActionAnimationPlaybackIntent.Invalid;
            stateTime = 0f;
            hasActiveAction = false;
            actionStartStep = 0;
        }

        ActionAnimationPlaybackIntent CreateNextPlaybackIntent()
        {
            nextPlaybackIntentValue = Mathf.Max(1, nextPlaybackIntentValue + 1);
            return new ActionAnimationPlaybackIntent(nextPlaybackIntentValue);
        }

        CommittedActionBranchOutcome EvaluateCommittedActionBranch(
            in CharacterActionCatalog actionCatalog,
            int localTick,
            int sourceStep)
        {
            if (!hasActiveAction ||
                !actionCatalog.HasCatalog ||
                !actionCatalog.TryGetCommittedActionBranch(activeAction.MotionSpec.ActionState, out CommittedActionBranchDefinition branch))
            {
                return CommittedActionBranchOutcome.None(sourceStep);
            }

            CommittedActionBranchEvaluationContext context =
                CommittedActionBranchEvaluationContext.FromActiveAction(in activeAction, sourceStep);
            return CommittedActionBranchEvaluator.Evaluate(
                new CommittedActionBranchEvaluationInput(branch, localTick, sourceStep, context));
        }

        int ResolveLocalTick(int sourceStep)
        {
            return Mathf.Max(0, Mathf.Max(0, sourceStep) - actionStartStep);
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
