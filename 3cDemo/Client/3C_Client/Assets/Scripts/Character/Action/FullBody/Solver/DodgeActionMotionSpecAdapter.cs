using ThirdPersonCharacterStateMachine;

namespace ThirdPersonAction
{
    public static class DodgeActionMotionSpecAdapter
    {
        public static ActionMotionSpec Resolve(in ActionMotionSpec spec, bool hasConfig, in DodgeActionConfig config)
        {
            if (!spec.HasSpec || spec.ActionState != ActionStateIds.Dodge || !hasConfig)
                return spec;

            DodgeActionVariant variant = spec.Variant == CharacterStateVariant.Backstep
                ? DodgeActionVariant.Backstep
                : DodgeActionVariant.Directional;
            return new ActionMotionSpec(
                spec.ActionState,
                spec.SourceState,
                spec.Variant,
                config.ResolveDuration(variant),
                config.ResolveDistance(variant),
                config.ShouldRotateToDirection(variant),
                spec.SetRunLatchOnComplete,
                spec.LockedWorldDirection,
                spec.StateTime,
                spec.SourceStep);
        }
    }
}
