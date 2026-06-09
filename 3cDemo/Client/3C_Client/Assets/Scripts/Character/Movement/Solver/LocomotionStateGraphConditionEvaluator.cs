namespace ThirdPersonMovement
{
    public static class LocomotionStateGraphConditionEvaluator
    {
        public static bool Evaluate(LocomotionStateGraphCondition condition, in LocomotionStateGraphContext context)
        {
            return condition switch
            {
                LocomotionStateGraphCondition.HasMoveIntent => context.HasMoveIntent,
                LocomotionStateGraphCondition.NoMoveIntent => !context.HasMoveIntent,
                LocomotionStateGraphCondition.MoveStartMinTimeReached => context.PhaseTime >= context.Settings.MoveStartMinTime,
                LocomotionStateGraphCondition.MoveStopMinTimeReached => context.PhaseTime >= context.Settings.MoveStopExitDuration,
                LocomotionStateGraphCondition.Always => true,
                _ => false
            };
        }
    }
}
