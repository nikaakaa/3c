using System.Collections.Generic;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    public interface IFullBodySubmissionRuntimePort
    {
        ILocomotionFrameRuntimePort LocomotionFrameRuntime { get; }
        CharacterStateMachineRunner StateMachine { get; }
        CharacterStateMachineSnapshot CurrentStateSnapshot { get; }
        InputRequestBuffer InputRequestBuffer { get; }

        bool PrepareFrameRuntimeAdapters();
        bool TryResolveDodgeActionConfig(out DodgeActionConfig config);
        int ResolveCurrentActionResistance();
        IReadOnlyList<ActionInterruptPolicy> ResolveInterruptPolicies();
    }

    public interface IFullBodyOutputRuntimePort
    {
        string ActiveFullBodyStatePath { get; }

        void SetLastFrameOutputs(
            in BasicLocomotionFrame locomotionFrame,
            in CharacterStateMachineFrame stateFrame,
            in ActionMotionResolveResult actionMotionResult);
        bool ConsumeStateFrameInputRequest(in CharacterStateMachineFrame stateFrame, int step);
        void ExecuteStateFrameMotion(
            in CharacterStateMachineFrame stateFrame,
            in BasicLocomotionFrame locomotionFrame,
            in ActionMotionResolveResult actionMotionResult,
            out bool actionMovementExecuted,
            out bool basicMovementExecuted);
        void PresentStateFrameAnimation(
            in CharacterStateMachineFrame stateFrame,
            in BasicLocomotionFrame locomotionFrame,
            bool exitedToLocomotion,
            out bool actionAnimationPresented,
            out bool locomotionAnimationPresented);
        void WriteStateFrameActionFacts(
            in CharacterStateMachineFrame stateFrame,
            in ActionMotionResolveResult actionMotionResult,
            bool exitedToLocomotion,
            int step);
        void UpdateStateSnapshot(in CharacterStateMachineFrame stateFrame, int step);
        void WriteAnimationRuntimeFacts(int step);
        void CompleteLocomotionTick();
        void LogDiagnosticTickSnapshots(int step);
    }
}
