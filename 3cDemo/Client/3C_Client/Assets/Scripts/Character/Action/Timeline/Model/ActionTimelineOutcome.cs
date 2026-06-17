using System;
using System.Collections.Generic;

namespace ThirdPersonAction
{
    public readonly struct ActionCueRequest
    {
        public ActionCueRequest(string cueId, int sourceFrame, int sourceStep)
        {
            CueId = (cueId ?? string.Empty).Trim();
            SourceFrame = sourceFrame < 0 ? 0 : sourceFrame;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
        }

        public string CueId { get; }
        public int SourceFrame { get; }
        public int SourceStep { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(CueId);
    }

    public readonly struct ActionTimelineOutcome
    {
        readonly string[] activeWindowFactIds;
        readonly ActionCueRequest[] cueRequests;

        public ActionTimelineOutcome(
            int currentFrame,
            int sourceStep,
            ActionAnimationKey animationKey,
            bool hasAnimation,
            ActionMotionSpec motionSpec,
            bool hasMotion,
            string[] activeWindowFactIds,
            ActionCueRequest[] cueRequests)
        {
            CurrentFrame = currentFrame < 0 ? 0 : currentFrame;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
            AnimationKey = animationKey;
            HasAnimation = hasAnimation && animationKey.IsValid;
            MotionSpec = motionSpec;
            HasMotion = hasMotion && motionSpec.HasSpec;
            this.activeWindowFactIds = activeWindowFactIds ?? Array.Empty<string>();
            this.cueRequests = cueRequests ?? Array.Empty<ActionCueRequest>();
        }

        public int CurrentFrame { get; }
        public int SourceStep { get; }
        public ActionAnimationKey AnimationKey { get; }
        public bool HasAnimation { get; }
        public ActionMotionSpec MotionSpec { get; }
        public bool HasMotion { get; }
        public IReadOnlyList<string> ActiveWindowFactIds => activeWindowFactIds ?? Array.Empty<string>();
        public IReadOnlyList<ActionCueRequest> CueRequests => cueRequests ?? Array.Empty<ActionCueRequest>();
        public bool HasCue => CueRequests.Count > 0;
        public bool HasOutcome => HasAnimation || HasMotion || ActiveWindowFactIds.Count > 0 || HasCue;

        public static ActionTimelineOutcome None(int currentFrame = 0, int sourceStep = 0)
        {
            return new ActionTimelineOutcome(
                currentFrame,
                sourceStep,
                default,
                false,
                ActionMotionSpec.None(sourceStep),
                false,
                Array.Empty<string>(),
                Array.Empty<ActionCueRequest>());
        }
    }
}
