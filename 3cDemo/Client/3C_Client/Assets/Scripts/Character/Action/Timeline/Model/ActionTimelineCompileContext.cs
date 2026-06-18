using System;
using ThirdPersonSimulation;

namespace ThirdPersonAction
{
    public readonly struct ActionTimelineCompileContext
    {
        public const float DefaultLegacyAuthoringFrameRate = 60f;

        public ActionTimelineCompileContext(float fixedTickSeconds)
            : this(fixedTickSeconds, DefaultLegacyAuthoringFrameRate)
        {
        }

        public ActionTimelineCompileContext(float fixedTickSeconds, float legacyAuthoringFrameRate)
        {
            if (float.IsNaN(fixedTickSeconds) || float.IsInfinity(fixedTickSeconds) || fixedTickSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(fixedTickSeconds), fixedTickSeconds, "Fixed tick seconds must be greater than zero.");
            if (float.IsNaN(legacyAuthoringFrameRate) || float.IsInfinity(legacyAuthoringFrameRate) || legacyAuthoringFrameRate <= 0f)
                throw new ArgumentOutOfRangeException(nameof(legacyAuthoringFrameRate), legacyAuthoringFrameRate, "Legacy authoring frame rate must be greater than zero.");

            FixedTickSeconds = fixedTickSeconds;
            LegacyAuthoringFrameRate = legacyAuthoringFrameRate;
        }

        public float FixedTickSeconds { get; }
        public float LegacyAuthoringFrameRate { get; }

        public static ActionTimelineCompileContext FromTickRate(SimulationTickRate tickRate)
        {
            return new ActionTimelineCompileContext(tickRate.FixedDeltaSecondsFloat);
        }
    }
}
