using ThirdPersonMovement;

namespace ThirdPersonAnimation
{
    public static class LocomotionAnimationAliasResolver
    {
        const string TurnBackAliasKey = "Locomotion.Turn.Back";
        const string IdleKey = "Idle";
        const string WalkStartKey = "WalkStart";
        const string WalkLoopKey = "WalkLoop";
        const string WalkEndKey = "WalkEnd";
        const string RunStartKey = "RunStart";
        const string RunLoopKey = "RunLoop";
        const string RunEndKey = "RunEnd";

        public static string ResolveAliasKey(
            RunLocomotionAnimationConfigSO config,
            in MovementAnimationContext context)
        {
            return ResolveAliasKey(config, context.Phase, context.Gait);
        }

        public static string ResolveAliasKey(
            RunLocomotionAnimationConfigSO config,
            BasicMovementPhase phase,
            BasicMovementGait gait)
        {
            if (config != null)
                return config.ResolveAliasKey(phase, gait);

            if (gait == BasicMovementGait.Walk)
            {
                return phase switch
                {
                    BasicMovementPhase.MoveStart => WalkStartKey,
                    BasicMovementPhase.MoveLoop => WalkLoopKey,
                    BasicMovementPhase.MoveStop => WalkEndKey,
                    BasicMovementPhase.TurnBack => TurnBackAliasKey,
                    _ => IdleKey
                };
            }

            return phase switch
            {
                BasicMovementPhase.MoveStart => RunStartKey,
                BasicMovementPhase.MoveLoop => RunLoopKey,
                BasicMovementPhase.MoveStop => RunEndKey,
                BasicMovementPhase.TurnBack => TurnBackAliasKey,
                _ => IdleKey
            };
        }

        public static BasicMovementGait ResolveGaitForAlias(
            RunLocomotionAnimationConfigSO config,
            BasicMovementPhase phase,
            string aliasKey,
            BasicMovementGait fallback)
        {
            if (string.Equals(aliasKey, ResolveAliasKey(config, phase, BasicMovementGait.Walk), System.StringComparison.Ordinal))
                return BasicMovementGait.Walk;
            if (string.Equals(aliasKey, ResolveAliasKey(config, phase, BasicMovementGait.Run), System.StringComparison.Ordinal))
                return BasicMovementGait.Run;

            return fallback;
        }
    }
}
