using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    public sealed partial class DeterministicKccWorldSolver
    {
        const string FutureBodyTrajectorySourceIdentity =
            "thirdperson.simulation.solver.deterministic-kcc.future-body/v1";

        public bool TryPredict(
            in CharacterFutureBodyTrajectoryRequest request,
            out CharacterFutureBodyTrajectory trajectory)
        {
            RequireAlive();
            RequireCurrent();
            trajectory = null;
            int actorIndex = FindBinding(request.ActorId);
            if (actorIndex < 0 || m_KccStates == null || actorIndex >= m_KccStates.Length)
                return false;
            int stepCount = checked((int)Math.Ceiling(request.DurationSeconds * m_TickRate));
            if (stepCount <= 0 || stepCount > m_TickRate * 10)
                return false;
            var samples = new List<CharacterFutureBodyTrajectorySample>(stepCount + 1)
            {
                new CharacterFutureBodyTrajectorySample(0f, 0f, 0f, 0f)
            };
            WorldBodyState body = m_Current.Bodies[actorIndex];
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
                    FixedVector3 requestedDisplacement = ToFixed(
                        IntegrateRequestedPlanarDisplacement(
                            in request,
                            previousTime,
                            elapsed));
                    DeterministicKccMotorResult result = motor.Move(
                        position,
                        state,
                        requestedDisplacement);
                    position = result.Position;
                    state = CreateKccState(request.ActorId, result);
                    FixedVector3 relative = position - origin;
                    samples.Add(new CharacterFutureBodyTrajectorySample(
                        elapsed,
                        relative.X.ToSingle(),
                        relative.Y.ToSingle(),
                        relative.Z.ToSingle()));
                    previousTime = elapsed;
                }
            }
            catch (DeterministicKccQueryException)
            {
                return false;
            }
            trajectory = new CharacterFutureBodyTrajectory(
                FutureBodyTrajectorySourceIdentity,
                samples.ToArray());
            return true;
        }

        static PlanarDisplacement IntegrateRequestedPlanarDisplacement(
            in CharacterFutureBodyTrajectoryRequest request,
            float startSeconds,
            float endSeconds)
        {
            float switchTime = request.CurrentSegmentRemainingSeconds;
            if (float.IsPositiveInfinity(switchTime) || endSeconds <= switchTime)
            {
                return IntegrateRotatingVelocity(
                    request.CurrentVelocityX,
                    request.CurrentVelocityZ,
                    request.TrajectoryCurvatureDegreesPerSecond,
                    startSeconds,
                    endSeconds);
            }
            if (startSeconds >= switchTime)
            {
                if (!request.HasContinuation)
                    return default;
                return IntegrateRotatingVelocity(
                    request.ContinuationVelocityX,
                    request.ContinuationVelocityZ,
                    request.TrajectoryCurvatureDegreesPerSecond,
                    startSeconds,
                    endSeconds);
            }
            PlanarDisplacement current = IntegrateRotatingVelocity(
                       request.CurrentVelocityX,
                       request.CurrentVelocityZ,
                       request.TrajectoryCurvatureDegreesPerSecond,
                       startSeconds,
                       switchTime);
            return !request.HasContinuation
                ? current
                : current + IntegrateRotatingVelocity(
                       request.ContinuationVelocityX,
                       request.ContinuationVelocityZ,
                       request.TrajectoryCurvatureDegreesPerSecond,
                       switchTime,
                       endSeconds);
        }

        static PlanarDisplacement IntegrateRotatingVelocity(
            float velocityX,
            float velocityZ,
            float yawRateDegreesPerSecond,
            float startSeconds,
            float endSeconds)
        {
            double duration = Math.Max(0d, endSeconds - startSeconds);
            if (duration <= 0d)
                return default;
            double angularVelocity = yawRateDegreesPerSecond * Math.PI / 180d;
            if (Math.Abs(angularVelocity) <= 0.000001d)
                return new PlanarDisplacement(velocityX * duration, velocityZ * duration);
            double startAngle = angularVelocity * startSeconds;
            double endAngle = angularVelocity * endSeconds;
            double along = (Math.Sin(endAngle) - Math.Sin(startAngle)) / angularVelocity;
            double across = (Math.Cos(startAngle) - Math.Cos(endAngle)) / angularVelocity;
            return new PlanarDisplacement(
                velocityX * along + velocityZ * across,
                velocityZ * along - velocityX * across);
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
