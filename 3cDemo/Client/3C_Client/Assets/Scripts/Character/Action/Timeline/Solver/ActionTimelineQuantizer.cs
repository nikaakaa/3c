using System;

namespace ThirdPersonAction
{
    public static class ActionTimelineQuantizer
    {
        const double Epsilon = 0.0000001d;

        public static int QuantizeSecondsToTick(float seconds, in ActionTimelineCompileContext context)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Timeline seconds must be zero or greater.");

            return Math.Max(0, (int)Math.Ceiling(seconds / context.FixedTickSeconds - Epsilon));
        }

        public static int LegacyFrameToTick(int frame, in ActionTimelineCompileContext context)
        {
            if (frame < 0)
                throw new ArgumentOutOfRangeException(nameof(frame), frame, "Legacy authoring frame must be zero or greater.");

            float seconds = frame / context.LegacyAuthoringFrameRate;
            return QuantizeSecondsToTick(seconds, in context);
        }

        public static float LegacyFrameToSeconds(int frame, in ActionTimelineCompileContext context)
        {
            if (frame < 0)
                throw new ArgumentOutOfRangeException(nameof(frame), frame, "Legacy authoring frame must be zero or greater.");

            return frame / context.LegacyAuthoringFrameRate;
        }
    }
}
