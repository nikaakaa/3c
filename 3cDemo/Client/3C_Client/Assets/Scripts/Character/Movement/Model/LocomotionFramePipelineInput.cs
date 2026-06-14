using ThirdPersonCharacterStateMachine;

namespace ThirdPersonMovement
{
    public readonly struct LocomotionFramePipelineInput
    {
        public LocomotionFramePipelineInput(
            BasicLocomotionInputSnapshot input,
            int currentStep,
            BasicMovementPhase currentPhase,
            float currentPhaseTime,
            BasicMovementSettings baseSettings,
            CharacterInputRequestFact inputRequest,
            CharacterRuntimeBlackboardSnapshot blackboardSnapshot,
            LocomotionFrameRuntimeState runtimeState,
            string activeStatePath)
        {
            Input = input;
            CurrentStep = currentStep;
            CurrentPhase = currentPhase;
            CurrentPhaseTime = currentPhaseTime;
            BaseSettings = baseSettings;
            InputRequest = inputRequest;
            BlackboardSnapshot = blackboardSnapshot;
            RuntimeState = runtimeState;
            ActiveStatePath = activeStatePath ?? string.Empty;
        }

        public BasicLocomotionInputSnapshot Input { get; }
        public int CurrentStep { get; }
        public BasicMovementPhase CurrentPhase { get; }
        public float CurrentPhaseTime { get; }
        public BasicMovementSettings BaseSettings { get; }
        public CharacterInputRequestFact InputRequest { get; }
        public CharacterRuntimeBlackboardSnapshot BlackboardSnapshot { get; }
        public LocomotionFrameRuntimeState RuntimeState { get; }
        public string ActiveStatePath { get; }
    }
}
