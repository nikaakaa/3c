using System;
using System.Collections.Generic;

namespace ThirdPersonAction
{
    public readonly struct ActionTimelineEvaluationInput
    {
        public ActionTimelineEvaluationInput(
            ActionTimelineDefinition timeline,
            int localTick,
            int sourceStep)
        {
            Timeline = timeline;
            LocalTick = localTick < 0 ? 0 : localTick;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
        }

        public ActionTimelineDefinition Timeline { get; }
        public int LocalTick { get; }
        public int SourceStep { get; }
    }

    public static class ActionTimelineEvaluator
    {
        public static ActionTimelineOutcome Evaluate(in ActionTimelineEvaluationInput input)
        {
            ActionTimelineDefinition timeline = input.Timeline;
            if (timeline == null || !timeline.IsDefined)
                return ActionTimelineOutcome.None(input.LocalTick, input.SourceStep);

            ActionAnimationKey animationKey = default;
            bool hasAnimation = false;
            ActionMotionSpec motionSpec = ActionMotionSpec.None(input.SourceStep);
            bool hasMotion = false;
            List<string> activeWindowFacts = new List<string>();
            List<ActionCueRequest> cueRequests = new List<ActionCueRequest>();

            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                ActionTimelineTrackDefinition track = timeline.Tracks[trackIndex];
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    ActionTimelineClipDefinition clip = track.Clips[clipIndex];
                    if (clip.Kind == ActionTimelineClipKind.Cue)
                    {
                        if (clip.TriggersAtTick(input.LocalTick) && !string.IsNullOrWhiteSpace(clip.Payload.CueId))
                            cueRequests.Add(new ActionCueRequest(clip.Payload.CueId, input.LocalTick, input.SourceStep));
                        continue;
                    }

                    if (!clip.ContainsTick(input.LocalTick))
                        continue;

                    switch (clip.Kind)
                    {
                        case ActionTimelineClipKind.AnimationKey:
                            if (!hasAnimation && clip.Payload.AnimationKey.IsValid)
                            {
                                animationKey = clip.Payload.AnimationKey;
                                hasAnimation = true;
                            }
                            break;
                        case ActionTimelineClipKind.Motion:
                            if (!hasMotion && clip.Payload.MotionSpec.HasSpec)
                            {
                                motionSpec = clip.Payload.MotionSpec;
                                hasMotion = true;
                            }
                            break;
                        case ActionTimelineClipKind.HitboxWindow:
                        case ActionTimelineClipKind.CancelWindow:
                            if (!string.IsNullOrWhiteSpace(clip.Payload.FactId))
                                activeWindowFacts.Add(clip.Payload.FactId);
                            break;
                    }
                }
            }

            return new ActionTimelineOutcome(
                input.LocalTick,
                input.SourceStep,
                animationKey,
                hasAnimation,
                motionSpec,
                hasMotion,
                activeWindowFacts.ToArray(),
                cueRequests.ToArray());
        }
    }
}
