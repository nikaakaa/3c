using ThirdPersonCharacterStateMachine;
using UnityEngine;

namespace ThirdPersonAction
{
    public static class DodgeActionBranchTimelineBuilder
    {
        public static bool TryBuild(
            in CharacterActionDefinition definition,
            DodgeActionVariant variant,
            float tickIntervalSeconds,
            out ActionBranchDefinition branch)
        {
            if (!definition.TryGetDodgeVariant(variant, out DodgeActionVariantDefinition dodge))
            {
                branch = ActionBranchDefinition.Empty;
                return false;
            }

            int durationFrames = ResolveDurationFrames(dodge.Duration, tickIntervalSeconds);
            CharacterStateVariant stateVariant = variant == DodgeActionVariant.Backstep
                ? CharacterStateVariant.Backstep
                : CharacterStateVariant.Directional;
            ActionMotionSpec motionSpec = new ActionMotionSpec(
                definition.ActionState,
                definition.MotionSourceState,
                stateVariant,
                dodge.Duration,
                dodge.Distance,
                dodge.RotateToDirection,
                variant == DodgeActionVariant.Directional,
                Vector3.zero,
                0f,
                0);
            ActionTimelineDefinition timeline = new ActionTimelineDefinition(
                definition.ActionState,
                durationFrames,
                new[]
                {
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Animation,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.AnimationKey,
                                0,
                                durationFrames,
                                ActionTimelineClipPayload.Animation(dodge.AnimationKey))
                        }),
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Motion,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.Motion,
                                0,
                                durationFrames,
                                ActionTimelineClipPayload.Motion(motionSpec))
                        })
                });
            string suffix = variant == DodgeActionVariant.Backstep ? "backstep" : "directional";
            branch = ActionBranchDefinition.Define(
                $"action.dodge.{suffix}",
                definition.ActionState,
                ActionNodeDefinition.Timeline($"timeline.dodge.{suffix}", timeline),
                BodyOccupancyClaim.FullBodyAction(0));
            return branch.CanEvaluate;
        }

        public static int ResolveDurationFrames(float durationSeconds, float tickIntervalSeconds)
        {
            if (durationSeconds <= 0f || tickIntervalSeconds <= 0f)
                return 0;

            return Mathf.Max(1, Mathf.CeilToInt(durationSeconds / tickIntervalSeconds));
        }
    }
}
