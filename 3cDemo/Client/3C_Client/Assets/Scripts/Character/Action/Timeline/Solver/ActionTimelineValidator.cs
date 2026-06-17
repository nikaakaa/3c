namespace ThirdPersonAction
{
    public static class ActionTimelineValidator
    {
        public static ActionTimelineValidationResult Validate(ActionTimelineDefinition timeline)
        {
            ActionTimelineValidationResult result = new ActionTimelineValidationResult();
            if (timeline == null || !timeline.IsDefined)
                return result;

            if (timeline.DurationFrames < 0)
                result.AddError("timeline-duration-negative");

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
                    if (clip.StartFrame < 0)
                        result.AddError($"clip-start-negative:{trackIndex}:{clipIndex}");
                    if (clip.EndFrame < clip.StartFrame)
                        result.AddError($"clip-range-invalid:{trackIndex}:{clipIndex}");
                    if (timeline.DurationFrames >= 0 && clip.EndFrame > timeline.DurationFrames)
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
    }
}
