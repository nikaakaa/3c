using ThirdPersonCharacterStateMachine;

namespace ThirdPersonAction
{
    public static class DodgeActionMotionSpecAdapter
    {
        public static ActionMotionSpec Resolve(in ActionMotionSpec spec, bool hasTuning, in DodgeActionTuning tuning)
        {
            if (!spec.HasSpec || spec.ActionState != ActionStateIds.Dodge || !hasTuning)
                return spec;

            DodgeActionVariant variant = spec.Variant == CharacterStateVariant.Backstep
                ? DodgeActionVariant.Backstep
                : DodgeActionVariant.Directional;
            return new ActionMotionSpec(
                spec.ActionState,
                spec.SourceState,
                spec.Variant,
                tuning.ResolveDuration(variant),
                tuning.ResolveDistance(variant),
                tuning.ShouldRotateToDirection(variant),
                spec.SetRunLatchOnComplete,
                spec.LockedWorldDirection,
                spec.StateTime,
                spec.SourceStep);
        }
    }
}
