using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    public sealed partial class DeterministicKccWorldSolver
    {
        const string FutureBodyTranslationSourceIdentity =
            "thirdperson.simulation.solver.deterministic-kcc.future-body-translation/v1";

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
            int stepCount = checked((int)Math.Ceiling(request.DurationSeconds * m_TickRate));
            if (stepCount <= 0 || stepCount > m_TickRate * 10)
                return false;
            WorldBodyState body = m_Current.Bodies[actorIndex];
            var samples = new List<CharacterFutureBodyTranslationSample>(stepCount + 1)
            {
                new CharacterFutureBodyTranslationSample(
                    0f,
                    0f,
                    0f,
                    0f,
                    request.CurrentVelocityX,
                    body.Velocity.Y.ToSingle(),
                    request.CurrentVelocityZ)
            };
            FixedVector3 origin = body.Position;
            FixedVector3 position = origin;
            DeterministicKccBodyState state = m_KccStates[actorIndex];
            DeterministicKccMotor motor = m_PredictionMotors[actorIndex];
            float previousTime = 0f;
            try
            {
                for (int stepIndex = 1; stepIndex <= stepCount; stepIndex++)
                {
                    float tickElapsed = stepIndex / (float)m_TickRate;
                    float elapsed = stepIndex == stepCount || tickElapsed >= request.DurationSeconds
                        ? request.DurationSeconds
                        : tickElapsed;
                    if (elapsed <= previousTime)
                        continue;
                    float deltaSeconds = elapsed - previousTime;
                    FixedVector3 requestedDisplacement = ToFixed(
                        IntegrateRequestedPlanarDisplacement(
                            in request,
                            previousTime,
                            elapsed));
                    FixedVector3 previousPosition = position;
                    DeterministicKccMotorResult result = motor.Move(
                        position,
                        state,
                        requestedDisplacement);
                    position = result.Position;
                    state = CreateKccState(request.ActorId, result);
                    FixedVector3 relative = position - origin;
                    FixedVector3 appliedVelocity = (position - previousPosition) *
                                                   (FixedScalar.One /
                                                    FixedScalar.FromDouble(deltaSeconds));
                    samples.Add(new CharacterFutureBodyTranslationSample(
                        elapsed,
                        relative.X.ToSingle(),
                        relative.Y.ToSingle(),
                        relative.Z.ToSingle(),
                        appliedVelocity.X.ToSingle(),
                        appliedVelocity.Y.ToSingle(),
                        appliedVelocity.Z.ToSingle()));
                    previousTime = elapsed;
                }
            }
            catch (DeterministicKccQueryException)
            {
                return false;
            }
            translation = new CharacterFutureBodyTranslation(
                FutureBodyTranslationSourceIdentity,
                samples.ToArray());
            return true;
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
