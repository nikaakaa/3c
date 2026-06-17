using System;
using System.Collections.Generic;

namespace ThirdPersonAction
{
    public enum ActionTimelineTrackKind
    {
        None = 0,
        Animation = 1,
        Motion = 2,
        Hitbox = 3,
        Cancel = 4,
        Cue = 5
    }

    public enum ActionTimelineClipKind
    {
        None = 0,
        AnimationKey = 1,
        Motion = 2,
        HitboxWindow = 3,
        CancelWindow = 4,
        Cue = 5
    }

    public readonly struct ActionTimelineClipPayload
    {
        public ActionTimelineClipPayload(
            ActionAnimationKey animationKey,
            ActionMotionSpec motionSpec,
            string factId,
            string cueId)
        {
            AnimationKey = animationKey;
            MotionSpec = motionSpec;
            FactId = (factId ?? string.Empty).Trim();
            CueId = (cueId ?? string.Empty).Trim();
        }

        public ActionAnimationKey AnimationKey { get; }
        public ActionMotionSpec MotionSpec { get; }
        public string FactId { get; }
        public string CueId { get; }

        public static ActionTimelineClipPayload Animation(ActionAnimationKey key)
        {
            return new ActionTimelineClipPayload(key, default, string.Empty, string.Empty);
        }

        public static ActionTimelineClipPayload Motion(ActionMotionSpec spec)
        {
            return new ActionTimelineClipPayload(default, spec, string.Empty, string.Empty);
        }

        public static ActionTimelineClipPayload Fact(string factId)
        {
            return new ActionTimelineClipPayload(default, default, factId, string.Empty);
        }

        public static ActionTimelineClipPayload Cue(string cueId)
        {
            return new ActionTimelineClipPayload(default, default, string.Empty, cueId);
        }
    }

    public readonly struct ActionTimelineClipDefinition
    {
        public ActionTimelineClipDefinition(
            ActionTimelineClipKind kind,
            int startFrame,
            int endFrame,
            ActionTimelineClipPayload payload)
        {
            Kind = kind;
            StartFrame = startFrame;
            EndFrame = endFrame;
            Payload = payload;
        }

        public ActionTimelineClipKind Kind { get; }
        public int StartFrame { get; }
        public int EndFrame { get; }
        public ActionTimelineClipPayload Payload { get; }
        public bool IsDefined => Kind != ActionTimelineClipKind.None;

        public bool ContainsFrame(int frame)
        {
            return IsDefined && StartFrame <= frame && frame < EndFrame;
        }

        public bool TriggersAtFrame(int frame)
        {
            return IsDefined && StartFrame == frame;
        }
    }

    public readonly struct ActionTimelineTrackDefinition
    {
        readonly ActionTimelineClipDefinition[] clips;

        public ActionTimelineTrackDefinition(
            ActionTimelineTrackKind kind,
            ActionTimelineClipDefinition[] clips)
        {
            Kind = kind;
            this.clips = clips ?? Array.Empty<ActionTimelineClipDefinition>();
        }

        public ActionTimelineTrackKind Kind { get; }
        public IReadOnlyList<ActionTimelineClipDefinition> Clips => clips ?? Array.Empty<ActionTimelineClipDefinition>();
        public bool IsDefined => Kind != ActionTimelineTrackKind.None;
    }

    public sealed class ActionTimelineDefinition
    {
        readonly ActionTimelineTrackDefinition[] tracks;

        public ActionTimelineDefinition(
            ActionStateId actionState,
            int durationFrames,
            ActionTimelineTrackDefinition[] tracks)
        {
            ActionState = actionState.IsValid ? actionState : ActionStateIds.None;
            DurationFrames = durationFrames;
            this.tracks = tracks ?? Array.Empty<ActionTimelineTrackDefinition>();
        }

        public ActionStateId ActionState { get; }
        public int DurationFrames { get; }
        public IReadOnlyList<ActionTimelineTrackDefinition> Tracks => tracks ?? Array.Empty<ActionTimelineTrackDefinition>();
        public bool IsDefined => ActionState.IsValid && ActionState != ActionStateIds.None;

        public static ActionTimelineDefinition Empty =>
            new ActionTimelineDefinition(ActionStateIds.None, 0, Array.Empty<ActionTimelineTrackDefinition>());
    }
}
