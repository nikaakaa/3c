using System;

namespace ThirdPersonSimulation
{
    public readonly struct CharacterFutureBodyTranslationRequest
    {
        public CharacterFutureBodyTranslationRequest(
            ActorId actorId,
            float durationSeconds,
            float currentVelocityX,
            float currentVelocityZ,
            float continuationVelocityX,
            float continuationVelocityZ,
            float currentSegmentRemainingSeconds,
            bool hasContinuation)
        {
            if (!actorId.IsValid || !float.IsFinite(durationSeconds) || durationSeconds <= 0f ||
                !float.IsFinite(currentVelocityX) || !float.IsFinite(currentVelocityZ) ||
                !float.IsFinite(continuationVelocityX) || !float.IsFinite(continuationVelocityZ) ||
                (!float.IsFinite(currentSegmentRemainingSeconds) &&
                 !float.IsPositiveInfinity(currentSegmentRemainingSeconds)) ||
                currentSegmentRemainingSeconds < 0f)
            {
                throw new ArgumentException("Future Body Translation request is invalid.");
            }
            ActorId = actorId;
            DurationSeconds = durationSeconds;
            CurrentVelocityX = currentVelocityX;
            CurrentVelocityZ = currentVelocityZ;
            ContinuationVelocityX = continuationVelocityX;
            ContinuationVelocityZ = continuationVelocityZ;
            CurrentSegmentRemainingSeconds = currentSegmentRemainingSeconds;
            HasContinuation = hasContinuation;
        }

        public ActorId ActorId { get; }
        public float DurationSeconds { get; }
        public float CurrentVelocityX { get; }
        public float CurrentVelocityZ { get; }
        public float ContinuationVelocityX { get; }
        public float ContinuationVelocityZ { get; }
        public float CurrentSegmentRemainingSeconds { get; }
        public bool HasContinuation { get; }
    }

    public readonly struct CharacterFutureBodyTranslationSample
    {
        public CharacterFutureBodyTranslationSample(
            float elapsedSeconds,
            float relativePositionX,
            float relativePositionY,
            float relativePositionZ,
            float velocityX,
            float velocityY,
            float velocityZ)
        {
            if (!float.IsFinite(elapsedSeconds) || elapsedSeconds < 0f ||
                !float.IsFinite(relativePositionX) || !float.IsFinite(relativePositionY) ||
                !float.IsFinite(relativePositionZ) ||
                !float.IsFinite(velocityX) || !float.IsFinite(velocityY) ||
                !float.IsFinite(velocityZ))
            {
                throw new ArgumentException("Future Body Translation sample is invalid.");
            }
            ElapsedSeconds = elapsedSeconds;
            RelativePositionX = relativePositionX;
            RelativePositionY = relativePositionY;
            RelativePositionZ = relativePositionZ;
            VelocityX = velocityX;
            VelocityY = velocityY;
            VelocityZ = velocityZ;
        }

        public float ElapsedSeconds { get; }
        public float RelativePositionX { get; }
        public float RelativePositionY { get; }
        public float RelativePositionZ { get; }
        public float VelocityX { get; }
        public float VelocityY { get; }
        public float VelocityZ { get; }
    }

    public sealed class CharacterFutureBodyTranslation
    {
        readonly CharacterFutureBodyTranslationSample[] m_Samples;

        public CharacterFutureBodyTranslation(
            string sourceIdentity,
            CharacterFutureBodyTranslationSample[] samples)
        {
            SourceIdentity = SimulationIdentity.Require(sourceIdentity, nameof(sourceIdentity));
            if (samples == null || samples.Length < 2 || samples[0].ElapsedSeconds != 0f)
                throw new ArgumentException("Future Body Translation requires an anchored sample sequence.", nameof(samples));
            m_Samples = new CharacterFutureBodyTranslationSample[samples.Length];
            float previousTime = -1f;
            for (int i = 0; i < samples.Length; i++)
            {
                CharacterFutureBodyTranslationSample sample = samples[i];
                if (sample.ElapsedSeconds <= previousTime)
                    throw new ArgumentException("Future Body Translation sample time is not strictly increasing.", nameof(samples));
                m_Samples[i] = sample;
                previousTime = sample.ElapsedSeconds;
            }
        }

        public string SourceIdentity { get; }
        public int SampleCount => m_Samples.Length;
        public float DurationSeconds => m_Samples[m_Samples.Length - 1].ElapsedSeconds;

        public CharacterFutureBodyTranslationSample SampleAt(int index)
        {
            if ((uint)index >= (uint)m_Samples.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Samples[index];
        }

        public CharacterFutureBodyTranslationSample Evaluate(float elapsedSeconds)
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
                CharacterFutureBodyTranslationSample end = m_Samples[i];
                if (time > end.ElapsedSeconds)
                    continue;
                CharacterFutureBodyTranslationSample start = m_Samples[i - 1];
                float duration = end.ElapsedSeconds - start.ElapsedSeconds;
                float t = duration > 0.000001f
                    ? Math.Clamp((time - start.ElapsedSeconds) / duration, 0f, 1f)
                    : 1f;
                return new CharacterFutureBodyTranslationSample(
                    time,
                    Lerp(start.RelativePositionX, end.RelativePositionX, t),
                    Lerp(start.RelativePositionY, end.RelativePositionY, t),
                    Lerp(start.RelativePositionZ, end.RelativePositionZ, t),
                    Lerp(start.VelocityX, end.VelocityX, t),
                    Lerp(start.VelocityY, end.VelocityY, t),
                    Lerp(start.VelocityZ, end.VelocityZ, t));
            }
            return m_Samples[m_Samples.Length - 1];
        }

        static float Lerp(float start, float end, float t) => start + (end - start) * t;
    }

    public interface ICharacterFutureBodyTranslationSource
    {
        bool TryPredict(
            in CharacterFutureBodyTranslationRequest request,
            out CharacterFutureBodyTranslation translation);
    }
}
