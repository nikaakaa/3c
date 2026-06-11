namespace ThirdPersonMovement
{
    public interface IActionMovementExecutor
    {
        void ExecuteActionMovement(in ActionMovementCommand command);
    }
}
