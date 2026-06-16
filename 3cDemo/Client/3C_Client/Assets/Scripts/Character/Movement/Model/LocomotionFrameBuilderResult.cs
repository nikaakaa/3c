using ThirdPersonCharacterStateMachine;
using UnityEngine;

namespace ThirdPersonMovement
{
    public readonly struct LocomotionFrameBuilderResult
    {
        LocomotionFrameBuilderResult(
            LocomotionDecisionFrame decisionFrame,
            LocomotionStateDecisionFrame stateDecision,
            CharacterStateMachineFrame stateFrame,
            BasicLocomotionFrame frame,
            BasicMovementMotionFacts motionFacts,
            CharacterRuntimeLocomotionFacts locomotionFacts,
            LocomotionFrameRuntimeState runtimeState,
            bool hasDecisionFrame,
            bool hasStateDecision,
            bool hasStateFrame,
            bool hasFrame,
            float currentPhaseTime,
            string activeStatePath,
            Vector3 currentWorldDirection)
        {
            DecisionFrame = decisionFrame;
            StateDecision = stateDecision;
            StateFrame = stateFrame;
            Frame = frame;
            MotionFacts = motionFacts;
            LocomotionFacts = locomotionFacts;
            RuntimeState = runtimeState;
            HasDecisionFrame = hasDecisionFrame;
            HasStateDecision = hasStateDecision;
            HasStateFrame = hasStateFrame;
            HasFrame = hasFrame;
            CurrentPhaseTime = currentPhaseTime;
            ActiveStatePath = activeStatePath ?? string.Empty;
            CurrentWorldDirection = currentWorldDirection;
        }

        public LocomotionDecisionFrame DecisionFrame { get; }
        public LocomotionStateDecisionFrame StateDecision { get; }
        public CharacterStateMachineFrame StateFrame { get; }
        public BasicLocomotionFrame Frame { get; }
        public BasicMovementMotionFacts MotionFacts { get; }
        public CharacterRuntimeLocomotionFacts LocomotionFacts { get; }
        public LocomotionFrameRuntimeState RuntimeState { get; }
        public bool HasDecisionFrame { get; }
        public bool HasStateDecision { get; }
        public bool HasStateFrame { get; }
        public bool HasFrame { get; }
        public float CurrentPhaseTime { get; }
        public string ActiveStatePath { get; }
        public Vector3 CurrentWorldDirection { get; }

        public static LocomotionFrameBuilderResult Prepared(
            in LocomotionDecisionFrame decisionFrame,
            in LocomotionFrameRuntimeState runtimeState)
        {
            return new LocomotionFrameBuilderResult(
                decisionFrame,
                default,
                default,
                default,
                default,
                CharacterRuntimeLocomotionFacts.Default,
                runtimeState,
                true,
                false,
                false,
                false,
                0f,
                string.Empty,
                Vector3.zero);
        }

        public static LocomotionFrameBuilderResult Evaluated(
            in LocomotionStateDecisionFrame stateDecision,
            in LocomotionFrameRuntimeState runtimeState)
        {
            return new LocomotionFrameBuilderResult(
                stateDecision.DecisionFrame,
                stateDecision,
                stateDecision.StateFrame,
                default,
                default,
                CharacterRuntimeLocomotionFacts.Default,
                runtimeState,
                true,
                true,
                true,
                false,
                stateDecision.StateFrame.Snapshot.StateTime,
                stateDecision.StateFrame.Snapshot.ActivePath,
                Vector3.zero);
        }

        public static LocomotionFrameBuilderResult Built(
            in LocomotionStateDecisionFrame stateDecision,
            in BasicLocomotionFrame frame,
            in BasicMovementMotionFacts motionFacts,
            in CharacterRuntimeLocomotionFacts locomotionFacts,
            in LocomotionFrameRuntimeState runtimeState,
            Vector3 currentWorldDirection)
        {
            return new LocomotionFrameBuilderResult(
                stateDecision.DecisionFrame,
                stateDecision,
                stateDecision.StateFrame,
                frame,
                motionFacts,
                locomotionFacts,
                runtimeState,
                true,
                true,
                true,
                true,
                stateDecision.StateFrame.Snapshot.StateTime,
                stateDecision.StateFrame.Snapshot.ActivePath,
                currentWorldDirection);
        }
    }
}
