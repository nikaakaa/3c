namespace ThirdPersonMovement
{
    public interface IBasicLocomotionInputSource
    {
        BasicLocomotionInputSnapshot ReadInput(float deltaTime);
        void SetInputEnabled(bool enabled);
    }
}
