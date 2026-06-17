using System.Collections.Generic;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    public interface ICharacterFrameSubmissionRuntimePort
    {
        ILocomotionFrameRuntimePort LocomotionFrameRuntime { get; }
        CharacterStateMachineRunner StateMachine { get; }
        CharacterStateMachineSnapshot CurrentStateSnapshot { get; }
        InputRequestBuffer InputRequestBuffer { get; }

        bool PrepareFrameRuntimeAdapters();
        bool TryResolveActionCatalog(out CharacterActionCatalog catalog);
        bool TryResolveBodyClaimPolicy(out BodyClaimPolicy policy);
        ActionLifecycleFrame TickActionLifecycle(
            in CharacterResolvedAction acceptedAction,
            in CharacterActionCatalog actionCatalog,
            float deltaTime,
            int step);
        void CompleteActionLifecycle(in ActionMotionResolveResult result, bool requireAnimationEnded);
        int ResolveCurrentActionResistance();
        IReadOnlyList<ActionInterruptPolicy> ResolveInterruptPolicies();
    }

    public interface ICharacterFrameOutputRuntimePort
    {
        string ActiveFrameStatePath { get; }

        void SetLastFrameOutputs(
            in BasicLocomotionFrame locomotionFrame,
            in CharacterStateMachineFrame stateFrame,
            in ActionMotionResolveResult actionMotionResult);
        bool ConsumeFrameInputRequest(in CharacterFrameInputConsumeSubmission inputConsume);
        void ExecuteFrameMotion(
            in CharacterFrameMovementSubmission movement,
            out bool actionMovementExecuted,
            out bool basicMovementExecuted);
        void PresentFrameAnimation(
            in CharacterFrameAnimationSubmission animation,
            in BasicLocomotionFrame locomotionFrame,
            out bool actionAnimationPresented,
            out bool locomotionAnimationPresented);
        void WriteStateFrameActionFacts(
            in CharacterStateMachineFrame stateFrame,
            in ActionMotionResolveResult actionMotionResult,
            bool exitedToLocomotion,
            int step);
        void WriteLocomotionPreemptionFact(in LocomotionPreemptionFact fact);
        void UpdateStateSnapshot(in CharacterStateMachineFrame stateFrame, int step);
        void WriteAnimationRuntimeFacts(int step);
        void CompleteLocomotionTick();
        void LogDiagnosticTickSnapshots(int step);
    }

    public interface ICharacterFrameRuntimePort : ICharacterFrameSubmissionRuntimePort, ICharacterFrameOutputRuntimePort
    {
        bool WriteBufferedInputFacts(in CharacterFrameInput input);
    }
}
