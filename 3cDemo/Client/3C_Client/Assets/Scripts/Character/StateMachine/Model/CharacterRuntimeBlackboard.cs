using ThirdPersonAction;
using ThirdPersonAnimation;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonCharacterStateMachine
{
    public sealed class CharacterRuntimeBlackboard
    {
        CharacterRuntimeLocomotionFacts locomotionFacts = CharacterRuntimeLocomotionFacts.Default;
        CharacterRuntimeActionFacts actionFacts = CharacterRuntimeActionFacts.Default;
        CharacterRuntimeAnimationFacts animationFacts = CharacterRuntimeAnimationFacts.Default;
        LocomotionPreemptionFact locomotionPreemptionFact = LocomotionPreemptionFact.None;
        CharacterRuntimeDebugFacts debugFacts = CharacterRuntimeDebugFacts.Default;

        public CharacterRuntimeBlackboardSnapshot Snapshot => new CharacterRuntimeBlackboardSnapshot(
            locomotionFacts,
            actionFacts,
            animationFacts,
            locomotionPreemptionFact,
            debugFacts);

        public CharacterRuntimeBlackboardRestoreState CaptureRestoreState()
        {
            return new CharacterRuntimeBlackboardRestoreState(Snapshot);
        }

        public void Reset()
        {
            locomotionFacts = CharacterRuntimeLocomotionFacts.Default;
            actionFacts = CharacterRuntimeActionFacts.Default;
            animationFacts = CharacterRuntimeAnimationFacts.Default;
            locomotionPreemptionFact = LocomotionPreemptionFact.None;
            debugFacts = CharacterRuntimeDebugFacts.Default;
        }

        public void Restore(in CharacterRuntimeBlackboardRestoreState restoreState)
        {
            Restore(restoreState.Snapshot);
        }

        public void Restore(in CharacterRuntimeBlackboardSnapshot snapshot)
        {
            locomotionFacts = snapshot.Locomotion;
            actionFacts = snapshot.Action;
            animationFacts = snapshot.Animation;
            locomotionPreemptionFact = snapshot.LocomotionPreemption;
            debugFacts = snapshot.Debug;
        }

        public void WriteLocomotionFacts(in CharacterRuntimeLocomotionFacts facts)
        {
            locomotionFacts = facts;
            debugFacts = CharacterRuntimeDebugFacts.Record("Locomotion", facts.SourceStep);
        }

        public void WriteActionFacts(in CharacterRuntimeActionFacts facts)
        {
            actionFacts = facts;
            debugFacts = CharacterRuntimeDebugFacts.Record("Action", facts.SourceStep);
        }

        public void WriteAnimationFacts(in CharacterRuntimeAnimationFacts facts)
        {
            animationFacts = facts;
            debugFacts = CharacterRuntimeDebugFacts.Record("Animation", facts.SourceStep);
        }

        public void WriteLocomotionPreemptionFact(in LocomotionPreemptionFact fact)
        {
            locomotionPreemptionFact = fact;
            debugFacts = CharacterRuntimeDebugFacts.Record("LocomotionPreemption", fact.SourceStep);
        }
    }

    public readonly struct CharacterRuntimeBlackboardSnapshot
    {
        public CharacterRuntimeBlackboardSnapshot(
            CharacterRuntimeLocomotionFacts locomotion,
            CharacterRuntimeActionFacts action,
            CharacterRuntimeAnimationFacts animation,
            CharacterRuntimeDebugFacts debug)
            : this(
                locomotion,
                action,
                animation,
                LocomotionPreemptionFact.None,
                debug)
        {
        }

        public CharacterRuntimeBlackboardSnapshot(
            CharacterRuntimeLocomotionFacts locomotion,
            CharacterRuntimeActionFacts action,
            CharacterRuntimeAnimationFacts animation,
            LocomotionPreemptionFact locomotionPreemption,
            CharacterRuntimeDebugFacts debug)
        {
            Locomotion = locomotion;
            Action = action;
            Animation = animation;
            LocomotionPreemption = locomotionPreemption;
            Debug = debug;
        }

        public CharacterRuntimeLocomotionFacts Locomotion { get; }
        public CharacterRuntimeActionFacts Action { get; }
        public CharacterRuntimeAnimationFacts Animation { get; }
        public LocomotionPreemptionFact LocomotionPreemption { get; }
        public CharacterRuntimeDebugFacts Debug { get; }

        public static CharacterRuntimeBlackboardSnapshot Empty => new CharacterRuntimeBlackboardSnapshot(
            CharacterRuntimeLocomotionFacts.Default,
            CharacterRuntimeActionFacts.Default,
            CharacterRuntimeAnimationFacts.Default,
            LocomotionPreemptionFact.None,
            CharacterRuntimeDebugFacts.Default);
    }

    public enum LocomotionPreemptionReason
    {
        None = 0,
        CommittedActionStarted = 1
    }

    public readonly struct LocomotionPreemptionFact
    {
        public LocomotionPreemptionFact(
            CharacterStateId sourceLocomotionState,
            ActionStateId sourceActionId,
            int sourceStep,
            LocomotionPreemptionReason reason)
        {
            SourceLocomotionState = sourceLocomotionState;
            SourceActionId = sourceActionId.IsValid ? sourceActionId : ActionStateIds.None;
            SourceStep = Mathf.Max(0, sourceStep);
            Reason = reason;
        }

        public CharacterStateId SourceLocomotionState { get; }
        public ActionStateId SourceActionId { get; }
        public int SourceStep { get; }
        public LocomotionPreemptionReason Reason { get; }
        public bool HasPreemption =>
            SourceLocomotionState.IsValid &&
            SourceActionId.IsValid &&
            SourceActionId != ActionStateIds.None &&
            Reason != LocomotionPreemptionReason.None;

        public bool MatchesSource(CharacterStateId stateId)
        {
            return HasPreemption && SourceLocomotionState == stateId;
        }

        public static LocomotionPreemptionFact None => default;

        public static LocomotionPreemptionFact CommittedActionStarted(
            CharacterStateId sourceLocomotionState,
            ActionStateId sourceActionId,
            int sourceStep)
        {
            return new LocomotionPreemptionFact(
                sourceLocomotionState,
                sourceActionId,
                sourceStep,
                LocomotionPreemptionReason.CommittedActionStarted);
        }
    }

    public readonly struct CharacterRuntimeBlackboardRestoreState
    {
        public CharacterRuntimeBlackboardRestoreState(CharacterRuntimeBlackboardSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        public CharacterRuntimeBlackboardSnapshot Snapshot { get; }

        public static CharacterRuntimeBlackboardRestoreState Empty =>
            new CharacterRuntimeBlackboardRestoreState(CharacterRuntimeBlackboardSnapshot.Empty);
    }

    public readonly struct CharacterRuntimeLocomotionFacts
    {
        public CharacterRuntimeLocomotionFacts(
            BasicMovementPhase phase,
            BasicMovementGait frameGait,
            BasicMovementGait lastMovingGait,
            bool hasMoveStopEntryGait,
            BasicMovementGait moveStopEntryGait,
            bool runLatchActive,
            Vector3 worldDirection,
            bool hasMoveIntent,
            float moveStrength,
            int sourceStep)
        {
            Phase = phase;
            FrameGait = frameGait;
            LastMovingGait = lastMovingGait;
            HasMoveStopEntryGait = hasMoveStopEntryGait;
            MoveStopEntryGait = moveStopEntryGait;
            RunLatchActive = runLatchActive;
            WorldDirection = NormalizePlanarOrZero(worldDirection);
            HasMoveIntent = hasMoveIntent;
            MoveStrength = Mathf.Clamp01(moveStrength);
            SourceStep = Mathf.Max(0, sourceStep);
        }

        public BasicMovementPhase Phase { get; }
        public BasicMovementGait FrameGait { get; }
        public BasicMovementGait LastMovingGait { get; }
        public bool HasMoveStopEntryGait { get; }
        public BasicMovementGait MoveStopEntryGait { get; }
        public bool RunLatchActive { get; }
        public Vector3 WorldDirection { get; }
        public bool HasMoveIntent { get; }
        public float MoveStrength { get; }
        public int SourceStep { get; }

        public static CharacterRuntimeLocomotionFacts Default => new CharacterRuntimeLocomotionFacts(
            BasicMovementPhase.Idle,
            BasicMovementGait.Walk,
            BasicMovementGait.Walk,
            false,
            BasicMovementGait.Walk,
            false,
            Vector3.zero,
            false,
            0f,
            0);

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }

    public readonly struct CharacterRuntimeActionFacts
    {
        public CharacterRuntimeActionFacts(
            bool active,
            ActionStateId state,
            bool completed,
            bool exitedToLocomotion,
            bool hasMovement,
            Vector3 worldDirection,
            float planarDistance,
            bool rotateToDirection,
            int sourceStep)
        {
            Active = active;
            State = state.IsValid ? state : ActionStateIds.None;
            Completed = completed;
            ExitedToLocomotion = exitedToLocomotion;
            HasMovement = hasMovement;
            WorldDirection = NormalizePlanarOrZero(worldDirection);
            PlanarDistance = Mathf.Max(0f, planarDistance);
            RotateToDirection = rotateToDirection;
            SourceStep = Mathf.Max(0, sourceStep);
        }

        public bool Active { get; }
        public ActionStateId State { get; }
        public bool Completed { get; }
        public bool ExitedToLocomotion { get; }
        public bool HasMovement { get; }
        public Vector3 WorldDirection { get; }
        public float PlanarDistance { get; }
        public bool RotateToDirection { get; }
        public int SourceStep { get; }

        public static CharacterRuntimeActionFacts Default => new CharacterRuntimeActionFacts(
            false,
            ActionStateIds.None,
            false,
            false,
            false,
            Vector3.zero,
            0f,
            false,
            0);

        public static CharacterRuntimeActionFacts FromStateFrame(
            in CharacterStateMachineFrame frame,
            bool exitedToLocomotion,
            int sourceStep)
        {
            ActionMotionResolveResult result = ActionMotionResolveResult.None(sourceStep);
            return FromStateFrame(in frame, in result, exitedToLocomotion, sourceStep);
        }

        public static CharacterRuntimeActionFacts FromStateFrame(
            in CharacterStateMachineFrame frame,
            in ActionMotionResolveResult actionMotionResult,
            bool exitedToLocomotion,
            int sourceStep)
        {
            ActionMovementCommand command = actionMotionResult.MovementCommand;
            bool hasActionState = frame.ActionState.IsValid && frame.ActionState != ActionStateIds.None;
            return new CharacterRuntimeActionFacts(
                hasActionState,
                frame.ActionState,
                actionMotionResult.ActionCompleted,
                exitedToLocomotion,
                actionMotionResult.HasActionMovement,
                command.WorldDirection,
                command.PlanarDistance,
                command.RotateToDirection,
                actionMotionResult.SourceStep);
        }

        public static CharacterRuntimeActionFacts FromActionMotionResult(
            in ActionMotionResolveResult actionMotionResult,
            bool exitedToLocomotion,
            int sourceStep)
        {
            ActionMovementCommand command = actionMotionResult.MovementCommand;
            bool hasActionState =
                actionMotionResult.HasSpec &&
                !actionMotionResult.ActionCompleted &&
                actionMotionResult.Spec.ActionState.IsValid &&
                actionMotionResult.Spec.ActionState != ActionStateIds.None;
            return new CharacterRuntimeActionFacts(
                hasActionState,
                hasActionState ? actionMotionResult.Spec.ActionState : ActionStateIds.None,
                actionMotionResult.ActionCompleted,
                exitedToLocomotion,
                actionMotionResult.HasActionMovement,
                command.WorldDirection,
                command.PlanarDistance,
                command.RotateToDirection,
                sourceStep);
        }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }

    public readonly struct CharacterRuntimeAnimationFacts
    {
        public CharacterRuntimeAnimationFacts(
            AnimationPhasePlaybackProgress locomotionProgress,
            string locomotionAnimationName,
            ActionAnimationKey actionKey,
            float actionNormalizedTime,
            bool actionHasValidPlayback,
            string actionAnimationName,
            int sourceStep)
            : this(
                locomotionProgress,
                locomotionAnimationName,
                new ActionAnimationPlaybackProgress(actionKey, actionNormalizedTime, actionHasValidPlayback, false),
                actionAnimationName,
                sourceStep)
        {
        }

        public CharacterRuntimeAnimationFacts(
            AnimationPhasePlaybackProgress locomotionProgress,
            string locomotionAnimationName,
            ActionAnimationKey actionKey,
            float actionNormalizedTime,
            bool actionHasValidPlayback,
            bool actionIsEnded,
            string actionAnimationName,
            int sourceStep)
            : this(
                locomotionProgress,
                locomotionAnimationName,
                new ActionAnimationPlaybackProgress(actionKey, actionNormalizedTime, actionHasValidPlayback, actionIsEnded),
                actionAnimationName,
                sourceStep)
        {
        }

        public CharacterRuntimeAnimationFacts(
            AnimationPhasePlaybackProgress locomotionProgress,
            string locomotionAnimationName,
            ActionAnimationPlaybackProgress actionProgress,
            string actionAnimationName,
            int sourceStep)
            : this(
                locomotionProgress,
                locomotionAnimationName,
                actionProgress,
                actionAnimationName,
                LocomotionFootPhaseSample.Invalid(locomotionProgress.Phase),
                LocomotionFootPhaseSample.Invalid(BasicMovementPhase.TurnBack),
                sourceStep)
        {
        }

        public CharacterRuntimeAnimationFacts(
            AnimationPhasePlaybackProgress locomotionProgress,
            string locomotionAnimationName,
            ActionAnimationPlaybackProgress actionProgress,
            string actionAnimationName,
            LocomotionFootPhaseSample currentLocomotionFootPhase,
            LocomotionFootPhaseSample lastLocomotionExitFootPhase,
            int sourceStep)
        {
            LocomotionProgress = locomotionProgress;
            LocomotionAnimationName = locomotionAnimationName ?? string.Empty;
            ActionProgress = actionProgress;
            ActionAnimationName = actionAnimationName ?? string.Empty;
            CurrentLocomotionFootPhase = currentLocomotionFootPhase;
            LastLocomotionExitFootPhase = lastLocomotionExitFootPhase;
            SourceStep = Mathf.Max(0, sourceStep);
        }

        public AnimationPhasePlaybackProgress LocomotionProgress { get; }
        public string LocomotionAnimationName { get; }
        public LocomotionFootPhaseSample CurrentLocomotionFootPhase { get; }
        public LocomotionFootPhaseSample LastLocomotionExitFootPhase { get; }
        public ActionAnimationPlaybackProgress ActionProgress { get; }
        public ActionAnimationKey ActionKey => ActionProgress.Key;
        public float ActionNormalizedTime => ActionProgress.NormalizedTime;
        public bool ActionHasValidPlayback => ActionProgress.HasValidPlayback;
        public bool ActionIsEnded => ActionProgress.IsEnded;
        public string ActionAnimationName { get; }
        public int SourceStep { get; }

        public static CharacterRuntimeAnimationFacts Default => new CharacterRuntimeAnimationFacts(
            AnimationPhasePlaybackProgress.Invalid(BasicMovementPhase.Idle),
            string.Empty,
            ActionAnimationPlaybackProgress.Invalid,
            string.Empty,
            0);
    }

    public readonly struct ActionAnimationPlaybackProgress
    {
        public ActionAnimationPlaybackProgress(
            ActionAnimationKey key,
            float normalizedTime,
            bool hasValidPlayback,
            bool isEnded)
            : this(
                key,
                normalizedTime,
                hasValidPlayback,
                isEnded,
                ActionAnimationPlaybackIntent.Invalid)
        {
        }

        public ActionAnimationPlaybackProgress(
            ActionAnimationKey key,
            float normalizedTime,
            bool hasValidPlayback,
            bool isEnded,
            ActionAnimationPlaybackIntent playbackIntent)
        {
            Key = key;
            NormalizedTime = normalizedTime < 0f ? 0f : normalizedTime;
            HasValidPlayback = hasValidPlayback && key.IsValid;
            IsEnded = HasValidPlayback && isEnded;
            PlaybackIntent = playbackIntent;
        }

        public ActionAnimationKey Key { get; }
        public float NormalizedTime { get; }
        public bool HasValidPlayback { get; }
        public bool IsEnded { get; }
        public ActionAnimationPlaybackIntent PlaybackIntent { get; }
        public bool HasPlaybackIntent => PlaybackIntent.IsValid;

        public static ActionAnimationPlaybackProgress Invalid =>
            new ActionAnimationPlaybackProgress(default, 0f, false, false);
    }

    public readonly struct CharacterRuntimeDebugFacts
    {
        public CharacterRuntimeDebugFacts(string lastWriter, int lastWriteStep)
        {
            LastWriter = lastWriter ?? string.Empty;
            LastWriteStep = Mathf.Max(0, lastWriteStep);
        }

        public string LastWriter { get; }
        public int LastWriteStep { get; }

        public static CharacterRuntimeDebugFacts Default => new CharacterRuntimeDebugFacts(string.Empty, 0);

        public static CharacterRuntimeDebugFacts Record(string writer, int step)
        {
            return new CharacterRuntimeDebugFacts(writer, step);
        }
    }
}
