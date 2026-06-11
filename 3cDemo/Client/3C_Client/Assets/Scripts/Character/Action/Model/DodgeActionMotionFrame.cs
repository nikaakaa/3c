using ThirdPersonMovement;

namespace ThirdPersonAction
{
    public readonly struct DodgeActionMotionFrame
    {
        public DodgeActionMotionFrame(
            bool active,
            ActionMovementCommand movementCommand,
            bool completed,
            DodgeActionVariant variant = DodgeActionVariant.Backstep)
        {
            Active = active;
            MovementCommand = movementCommand;
            Completed = completed;
            Variant = variant;
        }

        public bool Active { get; }
        public ActionMovementCommand MovementCommand { get; }
        public bool Completed { get; }
        public DodgeActionVariant Variant { get; }
        public bool ShouldLatchRun => Active && Completed && Variant == DodgeActionVariant.Directional;
    }
}
