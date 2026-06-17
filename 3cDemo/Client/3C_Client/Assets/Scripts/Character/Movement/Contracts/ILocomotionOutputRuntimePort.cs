using ThirdPersonCharacterStateMachine;

namespace ThirdPersonMovement
{
    public interface ILocomotionOutputRuntimePort
    {
        void ExecuteLocomotionMotion(in BasicLocomotionFrame frame);
        void PresentLocomotionAnimation(in BasicLocomotionFrame frame);
        void SetRunLatchActive(bool active);
        void WriteActionFacts(in CharacterRuntimeActionFacts facts);
        void WriteAnimationFacts(in CharacterRuntimeAnimationFacts facts);
        void WriteLocomotionPreemptionFact(in LocomotionPreemptionFact fact);
        void CompleteLocomotionTick();
    }
}
