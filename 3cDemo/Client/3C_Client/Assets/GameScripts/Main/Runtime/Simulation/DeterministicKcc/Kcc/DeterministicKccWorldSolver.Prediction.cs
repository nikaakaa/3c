using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    public sealed partial class DeterministicKccWorldSolver
    {
        const string FutureBodyTrajectorySourceIdentity =
            "thirdperson.simulation.solver.deterministic-kcc.future-body/v3";

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
            WorldBodyState body = m_Current.Bodies[actorIndex];
            var samples = new List<CharacterFutureBodyTrajectorySample>(stepCount + 1)
            {
                new CharacterFutureBodyTrajectorySample(
                    0f,
                    0f,
                    0f,
                    0f,
                    0f,
                    request.CurrentVelocityX,
                    body.Velocity.Y.ToSingle(),
                    request.CurrentVelocityZ,
                    request.YawVelocityDegreesPerSecond)
            };
            FixedVector3 origin = body.Position;
            FixedVector3 position = origin;
            double yawDegrees = body.Yaw.Degrees.ToDouble();
            double relativeYawDegrees = 0d;
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
                    PlanarVelocity velocity = ResolveRequestedPlanarVelocity(
                        in request,
                        elapsed);
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
                    double yawDelta = ResolveYawDelta(
                        in request,
                        velocity,
                        yawDegrees,
                        deltaSeconds);
                    yawDegrees = NormalizeDegrees(yawDegrees + yawDelta);
                    relativeYawDegrees += yawDelta;
                    FixedVector3 relative = position - origin;
                    FixedVector3 appliedVelocity = (position - previousPosition) *
                                                   (FixedScalar.One /
                                                    FixedScalar.FromDouble(deltaSeconds));
                    samples.Add(new CharacterFutureBodyTrajectorySample(
                        elapsed,
                        relative.X.ToSingle(),
                        relative.Y.ToSingle(),
                        relative.Z.ToSingle(),
                        (float)relativeYawDegrees,
                        appliedVelocity.X.ToSingle(),
                        appliedVelocity.Y.ToSingle(),
                        appliedVelocity.Z.ToSingle(),
                        (float)(yawDelta / deltaSeconds)));
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

        static PlanarVelocity ResolveRequestedPlanarVelocity(
            in CharacterFutureBodyTrajectoryRequest request,
            float elapsedSeconds)
        {
            float switchTime = request.CurrentSegmentRemainingSeconds;
            PlanarVelocity velocity;
            if (float.IsPositiveInfinity(switchTime) || elapsedSeconds < switchTime)
            {
                velocity = new PlanarVelocity(
                    request.CurrentVelocityX,
                    request.CurrentVelocityZ);
            }
            else
            {
                velocity = request.HasContinuation
                    ? new PlanarVelocity(
                    request.ContinuationVelocityX,
                    request.ContinuationVelocityZ)
                    : default;
            }
            return RotateVelocity(
                velocity,
                request.TrajectoryCurvatureDegreesPerSecond * elapsedSeconds);
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
                return request.HasContinuation
                    ? IntegrateRotatingVelocity(
                        request.ContinuationVelocityX,
                        request.ContinuationVelocityZ,
                        request.TrajectoryCurvatureDegreesPerSecond,
                        startSeconds,
                        endSeconds)
                    : default;
            }
            PlanarDisplacement current = IntegrateRotatingVelocity(
                request.CurrentVelocityX,
                request.CurrentVelocityZ,
                request.TrajectoryCurvatureDegreesPerSecond,
                startSeconds,
                switchTime);
            return request.HasContinuation
                ? current + IntegrateRotatingVelocity(
                    request.ContinuationVelocityX,
                    request.ContinuationVelocityZ,
                    request.TrajectoryCurvatureDegreesPerSecond,
                    switchTime,
                    endSeconds)
                : current;
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

        static PlanarVelocity RotateVelocity(PlanarVelocity velocity, float yawDegrees)
        {
            double angle = yawDegrees * Math.PI / 180d;
            double sin = Math.Sin(angle);
            double cos = Math.Cos(angle);
            return new PlanarVelocity(
                velocity.X * cos + velocity.Z * sin,
                velocity.Z * cos - velocity.X * sin);
        }

        static double ResolveYawDelta(
            in CharacterFutureBodyTrajectoryRequest request,
            PlanarVelocity velocity,
            double currentYawDegrees,
            double deltaSeconds)
        {
            if (deltaSeconds <= 0d)
                return 0d;
            double speedSquared = velocity.X * velocity.X + velocity.Z * velocity.Z;
            if (speedSquared <= 0.00000001d)
                return request.YawVelocityDegreesPerSecond * deltaSeconds;
            double targetYawDegrees = Math.Atan2(velocity.X, velocity.Z) * 180d / Math.PI;
            double error = DeltaDegrees(currentYawDegrees, targetYawDegrees);
            double maximumRate = request.MaximumYawVelocityDegreesPerSecond > 0f
                ? request.MaximumYawVelocityDegreesPerSecond
                : Math.Abs(request.YawVelocityDegreesPerSecond);
            double maximumDelta = maximumRate * deltaSeconds;
            return Math.Clamp(error, -maximumDelta, maximumDelta);
        }

        static double DeltaDegrees(double from, double to) =>
            NormalizeDegrees(to - from);

        static double NormalizeDegrees(double value)
        {
            double result = value % 360d;
            if (result > 180d)
                result -= 360d;
            else if (result <= -180d)
                result += 360d;
            return result;
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

        readonly struct PlanarVelocity
        {
            internal PlanarVelocity(double x, double z)
            {
                X = x;
                Z = z;
            }

            internal double X { get; }
            internal double Z { get; }
        }
    }
}
