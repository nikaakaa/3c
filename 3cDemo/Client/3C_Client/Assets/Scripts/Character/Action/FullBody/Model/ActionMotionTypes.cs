using ThirdPersonCharacterStateMachine;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAction
{
    public readonly struct ActionMotionSpec
    {
        public ActionMotionSpec(
            ActionStateId actionState,
            CharacterStateId sourceState,
            CharacterStateVariant variant,
            float duration,
            float distance,
            bool rotateToDirection,
            bool setRunLatchOnComplete,
            Vector3 lockedWorldDirection,
            float stateTime,
            int sourceStep)
        {
            ActionState = actionState.IsValid ? actionState : ActionStateIds.None;
            SourceState = sourceState;
            Variant = variant;
            Duration = Mathf.Max(0f, duration);
            Distance = Mathf.Max(0f, distance);
            RotateToDirection = rotateToDirection;
            SetRunLatchOnComplete = setRunLatchOnComplete;
            LockedWorldDirection = NormalizePlanarOrZero(lockedWorldDirection);
            StateTime = Mathf.Max(0f, stateTime);
            SourceStep = Mathf.Max(0, sourceStep);
        }

        public ActionStateId ActionState { get; }
        public CharacterStateId SourceState { get; }
        public CharacterStateVariant Variant { get; }
        public float Duration { get; }
        public float Distance { get; }
        public bool RotateToDirection { get; }
        public bool SetRunLatchOnComplete { get; }
        public Vector3 LockedWorldDirection { get; }
        public float StateTime { get; }
        public int SourceStep { get; }
        public bool HasSpec => ActionState.IsValid && ActionState != ActionStateIds.None;

        public static ActionMotionSpec None(int sourceStep = 0)
        {
            return new ActionMotionSpec(
                ActionStateIds.None,
                default,
                CharacterStateVariant.None,
                0f,
                0f,
                false,
                false,
                Vector3.zero,
                0f,
                sourceStep);
        }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }

    public readonly struct ActionMotionResolveInput
    {
        public ActionMotionResolveInput(
            ActionMotionSpec spec,
            float deltaTime,
            StateTimelineWindowFacts timelineFacts,
            CharacterRuntimeActionFacts previousActionFacts)
        {
            Spec = spec;
            DeltaTime = Mathf.Max(0f, deltaTime);
            TimelineFacts = timelineFacts;
            PreviousActionFacts = previousActionFacts;
        }

        public ActionMotionSpec Spec { get; }
        public float DeltaTime { get; }
        public StateTimelineWindowFacts TimelineFacts { get; }
        public CharacterRuntimeActionFacts PreviousActionFacts { get; }
    }

    public readonly struct ActionMotionResolveResult
    {
        public ActionMotionResolveResult(
            ActionMotionSpec spec,
            ActionMovementCommand movementCommand,
            bool hasActionMovement,
            bool actionCompleted,
            bool setRunLatch,
            int sourceStep,
            string diagnosticSummary)
        {
            Spec = spec;
            MovementCommand = movementCommand;
            HasActionMovement = hasActionMovement;
            ActionCompleted = actionCompleted;
            SetRunLatch = setRunLatch;
            SourceStep = Mathf.Max(0, sourceStep);
            DiagnosticSummary = diagnosticSummary ?? string.Empty;
        }

        public ActionMotionSpec Spec { get; }
        public ActionMovementCommand MovementCommand { get; }
        public bool HasSpec => Spec.HasSpec;
        public bool HasActionMovement { get; }
        public bool ActionCompleted { get; }
        public bool SetRunLatch { get; }
        public int SourceStep { get; }
        public string DiagnosticSummary { get; }

        public static ActionMotionResolveResult None(int sourceStep = 0)
        {
            ActionMotionSpec spec = ActionMotionSpec.None(sourceStep);
            return new ActionMotionResolveResult(
                spec,
                default,
                false,
                false,
                false,
                sourceStep,
                $"actionMotion=none sourceStep={Mathf.Max(0, sourceStep)}");
        }
    }
}
