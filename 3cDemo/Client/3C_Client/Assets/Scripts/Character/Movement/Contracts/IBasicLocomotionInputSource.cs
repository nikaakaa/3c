using ThirdPersonCharacterConfig;

namespace ThirdPersonMovement
{
    public interface IBasicLocomotionInputSource
    {
        BasicLocomotionInputSnapshot ReadInput(float deltaTime);
        void SetInputEnabled(bool enabled);
    }

    public interface IFormalLocomotionInputConfigReceiver
    {
        void ApplyFormalInputConfig(CharacterConfigSO config);
    }
}
