using System;

namespace ThirdPersonSimulation
{
    public readonly struct CharacterFutureBodyTrajectoryRequest
    {
        public CharacterFutureBodyTrajectoryRequest(
            ActorId actorId,
            float durationSeconds,
            float currentVelocityX,
            float currentVelocityZ,
            float continuationVelocityX,
            float continuationVelocityZ,
            float currentSegmentRemainingSeconds,
            bool hasContinuation,
            float yawVelocityDegreesPerSecond,
            float maximumYawVelocityDegreesPerSecond)
        {
            if (!actorId.IsValid || !float.IsFinite(durationSeconds) || durationSeconds <= 0f ||
                !float.IsFinite(currentVelocityX) || !float.IsFinite(currentVelocityZ) ||
                !float.IsFinite(continuationVelocityX) || !float.IsFinite(continuationVelocityZ) ||
                (!float.IsFinite(currentSegmentRemainingSeconds) &&
                 !float.IsPositiveInfinity(currentSegmentRemainingSeconds)) ||
                currentSegmentRemainingSeconds < 0f ||
                !float.IsFinite(yawVelocityDegreesPerSecond) ||
                !float.IsFinite(maximumYawVelocityDegreesPerSecond) ||
                maximumYawVelocityDegreesPerSecond < 0f)
            {
                throw new ArgumentException("Future Body Trajectory request is invalid.");
            }
            ActorId = actorId;
            DurationSeconds = durationSeconds;
            CurrentVelocityX = currentVelocityX;
            CurrentVelocityZ = currentVelocityZ;
            ContinuationVelocityX = continuationVelocityX;
            ContinuationVelocityZ = continuationVelocityZ;
            CurrentSegmentRemainingSeconds = currentSegmentRemainingSeconds;
            HasContinuation = hasContinuation;
            YawVelocityDegreesPerSecond = yawVelocityDegreesPerSecond;
            MaximumYawVelocityDegreesPerSecond = maximumYawVelocityDegreesPerSecond;
        }

        public ActorId ActorId { get; }
        public float DurationSeconds { get; }
        public float CurrentVelocityX { get; }
        public float CurrentVelocityZ { get; }
        public float ContinuationVelocityX { get; }
        public float ContinuationVelocityZ { get; }
        public float CurrentSegmentRemainingSeconds { get; }
        public bool HasContinuation { get; }
        public float YawVelocityDegreesPerSecond { get; }
        public float MaximumYawVelocityDegreesPerSecond { get; }
    }

    public readonly struct CharacterFutureBodyTrajectorySample
    {
        public CharacterFutureBodyTrajectorySample(
            float elapsedSeconds,
            float relativePositionX,
            float relativePositionY,
            float relativePositionZ,
            float relativeYawDegrees,
            float velocityX,
            float velocityY,
            float velocityZ,
            float yawVelocityDegreesPerSecond)
        {
            if (!float.IsFinite(elapsedSeconds) || elapsedSeconds < 0f ||
                !float.IsFinite(relativePositionX) || !float.IsFinite(relativePositionY) ||
                !float.IsFinite(relativePositionZ) ||
                !float.IsFinite(relativeYawDegrees) ||
                !float.IsFinite(velocityX) || !float.IsFinite(velocityY) ||
                !float.IsFinite(velocityZ) ||
                !float.IsFinite(yawVelocityDegreesPerSecond))
            {
                throw new ArgumentException("Future Body Trajectory sample is invalid.");
            }
            ElapsedSeconds = elapsedSeconds;
            RelativePositionX = relativePositionX;
            RelativePositionY = relativePositionY;
            RelativePositionZ = relativePositionZ;
            RelativeYawDegrees = relativeYawDegrees;
            VelocityX = velocityX;
            VelocityY = velocityY;
            VelocityZ = velocityZ;
            YawVelocityDegreesPerSecond = yawVelocityDegreesPerSecond;
        }

        public float ElapsedSeconds { get; }
        public float RelativePositionX { get; }
        public float RelativePositionY { get; }
        public float RelativePositionZ { get; }
        public float RelativeYawDegrees { get; }
        public float VelocityX { get; }
        public float VelocityY { get; }
        public float VelocityZ { get; }
        public float YawVelocityDegreesPerSecond { get; }
    }

    public sealed class CharacterFutureBodyTrajectory
    {
        readonly CharacterFutureBodyTrajectorySample[] m_Samples;

        public CharacterFutureBodyTrajectory(
            string sourceIdentity,
            CharacterFutureBodyTrajectorySample[] samples)
        {
            SourceIdentity = SimulationIdentity.Require(sourceIdentity, nameof(sourceIdentity));
            if (samples == null || samples.Length < 2 || samples[0].ElapsedSeconds != 0f)
                throw new ArgumentException("Future Body Trajectory requires an anchored sample sequence.", nameof(samples));
            m_Samples = new CharacterFutureBodyTrajectorySample[samples.Length];
            float previousTime = -1f;
            for (int i = 0; i < samples.Length; i++)
            {
                CharacterFutureBodyTrajectorySample sample = samples[i];
                if (sample.ElapsedSeconds <= previousTime)
                    throw new ArgumentException("Future Body Trajectory sample time is not strictly increasing.", nameof(samples));
                m_Samples[i] = sample;
                previousTime = sample.ElapsedSeconds;
            }
        }

        public string SourceIdentity { get; }
        public int SampleCount => m_Samples.Length;
        public float DurationSeconds => m_Samples[m_Samples.Length - 1].ElapsedSeconds;

        public CharacterFutureBodyTrajectorySample SampleAt(int index)
        {
            if ((uint)index >= (uint)m_Samples.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Samples[index];
        }

        public CharacterFutureBodyTrajectorySample Evaluate(float elapsedSeconds)
        {
            if (!float.IsFinite(elapsedSeconds) || elapsedSeconds < 0f ||
                elapsedSeconds > DurationSeconds + 0.0001f)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            }
            float time = Math.Min(elapsedSeconds, DurationSeconds);
            if (time <= 0f)
                return m_Samples[0];
            for (int i = 1; i < m_Samples.Length; i++)
            {
                CharacterFutureBodyTrajectorySample end = m_Samples[i];
                if (time > end.ElapsedSeconds)
                    continue;
                CharacterFutureBodyTrajectorySample start = m_Samples[i - 1];
                float duration = end.ElapsedSeconds - start.ElapsedSeconds;
                float t = duration > 0.000001f
                    ? Math.Clamp((time - start.ElapsedSeconds) / duration, 0f, 1f)
                    : 1f;
                return new CharacterFutureBodyTrajectorySample(
                    time,
                    Lerp(start.RelativePositionX, end.RelativePositionX, t),
                    Lerp(start.RelativePositionY, end.RelativePositionY, t),
                    Lerp(start.RelativePositionZ, end.RelativePositionZ, t),
                    Lerp(start.RelativeYawDegrees, end.RelativeYawDegrees, t),
                    Lerp(start.VelocityX, end.VelocityX, t),
                    Lerp(start.VelocityY, end.VelocityY, t),
                    Lerp(start.VelocityZ, end.VelocityZ, t),
                    Lerp(
                        start.YawVelocityDegreesPerSecond,
                        end.YawVelocityDegreesPerSecond,
                        t));
            }
            return m_Samples[m_Samples.Length - 1];
        }

        static float Lerp(float start, float end, float t) => start + (end - start) * t;
    }

    public interface ICharacterFutureBodyTrajectorySource
    {
        bool TryPredict(
            in CharacterFutureBodyTrajectoryRequest request,
            out CharacterFutureBodyTrajectory trajectory);
    }
}
