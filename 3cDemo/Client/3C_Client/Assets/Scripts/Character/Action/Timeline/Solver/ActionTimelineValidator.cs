namespace ThirdPersonAction
{
    public static class ActionTimelineValidator
    {
        public static ActionTimelineValidationResult Validate(ActionTimelineDefinition timeline)
        {
            ActionTimelineValidationResult result = new ActionTimelineValidationResult();
            if (timeline == null || !timeline.IsDefined)
                return result;

            if (timeline.DurationTicks < 0)
                result.AddError("timeline-duration-ticks-negative");

            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                ActionTimelineTrackDefinition track = timeline.Tracks[trackIndex];
                if (!track.IsDefined)
                    result.AddError($"track-kind-invalid:{trackIndex}");

                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    ActionTimelineClipDefinition clip = track.Clips[clipIndex];
                    if (!clip.IsDefined)
                        result.AddError($"clip-kind-invalid:{trackIndex}:{clipIndex}");
                    if (clip.StartTick < 0)
                        result.AddError($"clip-start-tick-negative:{trackIndex}:{clipIndex}");
                    if (clip.EndTick < clip.StartTick)
                        result.AddError($"clip-tick-range-invalid:{trackIndex}:{clipIndex}");
                    if (timeline.DurationTicks >= 0 && clip.EndTick > timeline.DurationTicks)
                        result.AddWarning($"clip-exceeds-duration:{trackIndex}:{clipIndex}");
                    ValidatePayload(trackIndex, clipIndex, in clip, result);
                }
            }

            return result;
        }

        static void ValidatePayload(
            int trackIndex,
            int clipIndex,
            in ActionTimelineClipDefinition clip,
            ActionTimelineValidationResult result)
        {
            switch (clip.Kind)
            {
                case ActionTimelineClipKind.AnimationKey:
                    if (!clip.Payload.AnimationKey.IsValid)
                        result.AddError($"clip-animation-key-missing:{trackIndex}:{clipIndex}");
                    break;
                case ActionTimelineClipKind.Motion:
                    if (!clip.Payload.MotionSpec.HasSpec)
                        result.AddError($"clip-motion-spec-missing:{trackIndex}:{clipIndex}");
                    ValidateMotionWarpPayload(trackIndex, clipIndex, in clip, result);
                    break;
                case ActionTimelineClipKind.HitboxWindow:
                case ActionTimelineClipKind.CancelWindow:
                    if (string.IsNullOrWhiteSpace(clip.Payload.FactId))
                        result.AddError($"clip-window-fact-missing:{trackIndex}:{clipIndex}");
                    break;
                case ActionTimelineClipKind.Cue:
                    if (string.IsNullOrWhiteSpace(clip.Payload.CueId))
                        result.AddError($"clip-cue-missing:{trackIndex}:{clipIndex}");
                    break;
            }
        }

        static void ValidateMotionWarpPayload(
            int trackIndex,
            int clipIndex,
            in ActionTimelineClipDefinition clip,
            ActionTimelineValidationResult result)
        {
            if (!clip.Payload.MotionWarpPayload.HasWarp)
                return;

            if (!clip.Payload.MotionWarpPayload.IsValid)
                result.AddError($"clip-motion-warp-policy-missing:{trackIndex}:{clipIndex}");
            if (!clip.Payload.MotionWarpPayload.HasRequiredTargetBinding)
                result.AddError($"clip-motion-warp-target-binding-missing:{trackIndex}:{clipIndex}");
            if (!clip.Payload.MotionWarpPayload.HasRequiredMotionProfile)
                result.AddError($"clip-motion-warp-profile-missing:{trackIndex}:{clipIndex}");
        }
    }
}
