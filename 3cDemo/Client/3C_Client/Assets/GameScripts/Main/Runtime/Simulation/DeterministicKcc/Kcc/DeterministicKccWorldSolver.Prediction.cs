using System;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    public sealed partial class DeterministicKccWorldSolver
    {
        const string FutureBodyTranslationSourceIdentity =
            "thirdperson.simulation.solver.deterministic-kcc.future-body-translation/v2";

        public bool TryPredict(
            in CharacterFutureBodyTranslationRequest request,
            out CharacterFutureBodyTranslation translation)
        {
            RequireAlive();
            RequireCurrent();
            translation = null;
            int actorIndex = FindBinding(request.ActorId);
            if (actorIndex < 0 || m_KccStates == null || actorIndex >= m_KccStates.Length)
                return false;
            Span<float> sampleTimes = stackalloc float[4];
            int sampleCount = CollectSampleTimes(in request, sampleTimes);
            WorldBodyState body = m_Current.Bodies[actorIndex];
            var samples = new CharacterFutureBodyTranslationSample[sampleCount + 1];
            samples[0] = new CharacterFutureBodyTranslationSample(
                0f,
                0f,
                0f,
                0f,
                request.CurrentVelocityX,
                body.Velocity.Y.ToSingle(),
                request.CurrentVelocityZ);
            FixedVector3 origin = body.Position;
            FixedVector3 position = origin;
            DeterministicKccBodyState state = m_KccStates[actorIndex];
            DeterministicKccMotor motor = m_PredictionMotors[actorIndex];
            float previousTime = 0f;
            try
            {
                for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
                {
                    float elapsed = sampleTimes[sampleIndex];
                    float deltaSeconds = elapsed - previousTime;
                    FixedVector3 previousPosition = position;
                    MoveInterval(
                        in request,
                        request.ActorId,
                        previousTime,
                        elapsed,
                        motor,
                        ref position,
                        ref state);
                    FixedVector3 relative = position - origin;
                    FixedVector3 appliedVelocity = (position - previousPosition) *
                                                   (FixedScalar.One /
                                                    FixedScalar.FromDouble(deltaSeconds));
                    samples[sampleIndex + 1] = new CharacterFutureBodyTranslationSample(
                        elapsed,
                        relative.X.ToSingle(),
                        relative.Y.ToSingle(),
                        relative.Z.ToSingle(),
                        appliedVelocity.X.ToSingle(),
                        appliedVelocity.Y.ToSingle(),
                        appliedVelocity.Z.ToSingle());
                    previousTime = elapsed;
                }
            }
            catch (DeterministicKccQueryException)
            {
                return false;
            }
            translation = new CharacterFutureBodyTranslation(
                FutureBodyTranslationSourceIdentity,
                samples);
            return true;
        }

        static int CollectSampleTimes(
            in CharacterFutureBodyTranslationRequest request,
            Span<float> output)
        {
            int count = 0;
            for (int sourceIndex = 0; sourceIndex < 4; sourceIndex++)
            {
                float value = request.SampleTimeAt(sourceIndex);
                if (value <= 0.000001f)
                    continue;
                int insertion = count;
                while (insertion > 0 && value < output[insertion - 1])
                {
                    output[insertion] = output[insertion - 1];
                    insertion--;
                }
                if (insertion > 0 && Math.Abs(output[insertion - 1] - value) <= 0.000001f ||
                    insertion < count && Math.Abs(output[insertion] - value) <= 0.000001f)
                {
                    continue;
                }
                output[insertion] = value;
                count++;
            }
            if (count <= 0 || Math.Abs(output[count - 1] - request.DurationSeconds) > 0.0001f)
                throw new InvalidOperationException("Future Body Translation sample schedule is invalid.");
            return count;
        }

        void MoveInterval(
            in CharacterFutureBodyTranslationRequest request,
            ActorId actorId,
            float startSeconds,
            float endSeconds,
            DeterministicKccMotor motor,
            ref FixedVector3 position,
            ref DeterministicKccBodyState state)
        {
            float switchTime = request.CurrentSegmentRemainingSeconds;
            if (float.IsFinite(switchTime) &&
                startSeconds < switchTime && switchTime < endSeconds)
            {
                MoveDisplacement(
                    actorId,
                    IntegrateRequestedPlanarDisplacement(
                        in request,
                        startSeconds,
                        switchTime),
                    motor,
                    ref position,
                    ref state);
                MoveDisplacement(
                    actorId,
                    IntegrateRequestedPlanarDisplacement(
                        in request,
                        switchTime,
                        endSeconds),
                    motor,
                    ref position,
                    ref state);
                return;
            }
            MoveDisplacement(
                actorId,
                IntegrateRequestedPlanarDisplacement(
                    in request,
                    startSeconds,
                    endSeconds),
                motor,
                ref position,
                ref state);
        }

        void MoveDisplacement(
            ActorId actorId,
            PlanarDisplacement requested,
            DeterministicKccMotor motor,
            ref FixedVector3 position,
            ref DeterministicKccBodyState state)
        {
            FixedVector3 remaining = ToFixed(requested);
            FixedScalar magnitude = remaining.Magnitude;
            int segmentCount = magnitude > m_Configuration.MaximumMovementDistance
                ? checked((int)Math.Ceiling(
                    magnitude.ToDouble() /
                    m_Configuration.MaximumMovementDistance.ToDouble()))
                : 1;
            FixedVector3 segment = remaining *
                                   (FixedScalar.One / FixedScalar.FromInt64(segmentCount));
            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                FixedVector3 displacement = segmentIndex == segmentCount - 1
                    ? remaining
                    : segment;
                DeterministicKccMotorResult result = motor.Move(
                    position,
                    state,
                    displacement);
                position = result.Position;
                state = CreateKccState(actorId, result);
                remaining -= displacement;
            }
        }

        static PlanarDisplacement IntegrateRequestedPlanarDisplacement(
            in CharacterFutureBodyTranslationRequest request,
            float startSeconds,
            float endSeconds)
        {
            float switchTime = request.CurrentSegmentRemainingSeconds;
            if (float.IsPositiveInfinity(switchTime) || endSeconds <= switchTime)
            {
                return IntegrateVelocity(
                    request.CurrentVelocityX,
                    request.CurrentVelocityZ,
                    startSeconds,
                    endSeconds);
            }
            if (startSeconds >= switchTime)
            {
                return request.HasContinuation
                    ? IntegrateVelocity(
                        request.ContinuationVelocityX,
                        request.ContinuationVelocityZ,
                        startSeconds,
                        endSeconds)
                    : default;
            }
            PlanarDisplacement current = IntegrateVelocity(
                request.CurrentVelocityX,
                request.CurrentVelocityZ,
                startSeconds,
                switchTime);
            return request.HasContinuation
                ? current + IntegrateVelocity(
                    request.ContinuationVelocityX,
                    request.ContinuationVelocityZ,
                    switchTime,
                    endSeconds)
                : current;
        }

        static PlanarDisplacement IntegrateVelocity(
            float velocityX,
            float velocityZ,
            float startSeconds,
            float endSeconds)
        {
            double duration = Math.Max(0d, endSeconds - startSeconds);
            if (duration <= 0d)
                return default;
            return new PlanarDisplacement(
                velocityX * duration,
                velocityZ * duration);
        }

        static FixedVector3 ToFixed(PlanarDisplacement value) => new FixedVector3(
            FixedScalar.FromDouble(value.X),
            FixedScalar.Zero,
            FixedScalar.FromDouble(value.Z));

        readonly struct PlanarDisplacement
        {
            internal PlanarDisplacement(double x, double z)
            {
                X = x;
                Z = z;
            }

            internal double X { get; }
            internal double Z { get; }

            public static PlanarDisplacement operator +(
                PlanarDisplacement left,
                PlanarDisplacement right) =>
                new PlanarDisplacement(left.X + right.X, left.Z + right.Z);
        }

    }
}
