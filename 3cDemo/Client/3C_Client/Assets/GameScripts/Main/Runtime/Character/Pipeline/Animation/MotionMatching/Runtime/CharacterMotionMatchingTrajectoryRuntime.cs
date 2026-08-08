using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public readonly struct MotionMatchingTrajectoryPolicyRuntimePoint
    {
        public MotionMatchingTrajectoryPolicyRuntimePoint(
            float timeOffset,
            float acceptedPositionTolerance,
            float acceptedFacingToleranceDegrees,
            float acceptedConfidence,
            float selectedPositionTolerance,
            float selectedFacingToleranceDegrees,
            float selectedConfidence)
        {
            if (!float.IsFinite(timeOffset) || timeOffset < 0f ||
                !float.IsFinite(acceptedPositionTolerance) || acceptedPositionTolerance < 0f ||
                !float.IsFinite(acceptedFacingToleranceDegrees) || acceptedFacingToleranceDegrees < 0f ||
                !float.IsFinite(acceptedConfidence) || acceptedConfidence < 0f || acceptedConfidence > 1f ||
                !float.IsFinite(selectedPositionTolerance) || selectedPositionTolerance < 0f ||
                !float.IsFinite(selectedFacingToleranceDegrees) || selectedFacingToleranceDegrees < 0f ||
                !float.IsFinite(selectedConfidence) || selectedConfidence < 0f || selectedConfidence > 1f)
                throw new ArgumentException("Compiled Motion Matching Trajectory policy point is invalid.");
            TimeOffset = timeOffset;
            AcceptedPositionTolerance = acceptedPositionTolerance;
            AcceptedFacingToleranceDegrees = acceptedFacingToleranceDegrees;
            AcceptedConfidence = acceptedConfidence;
            SelectedPositionTolerance = selectedPositionTolerance;
            SelectedFacingToleranceDegrees = selectedFacingToleranceDegrees;
            SelectedConfidence = selectedConfidence;
        }

        public float TimeOffset { get; }
        public float AcceptedPositionTolerance { get; }
        public float AcceptedFacingToleranceDegrees { get; }
        public float AcceptedConfidence { get; }
        public float SelectedPositionTolerance { get; }
        public float SelectedFacingToleranceDegrees { get; }
        public float SelectedConfidence { get; }
    }

    public sealed class MotionMatchingTrajectoryPolicyPayload
    {
        readonly MotionMatchingTrajectoryPolicyRuntimePoint[] m_Points;

        public MotionMatchingTrajectoryPolicyPayload(
            string policyId,
            int revision,
            float maximumAcceleration,
            float maximumTurnRateDegrees,
            float selectedAgePositionTolerancePerSecond,
            float selectedAgeFacingTolerancePerSecond,
            float selectedAgeConfidenceDecayPerSecond,
            MotionMatchingTrajectoryPolicyRuntimePoint[] points)
        {
            PolicyId = MotionMatchingIdentity.Require(policyId, nameof(policyId));
            if (revision <= 0 || !float.IsFinite(maximumAcceleration) || maximumAcceleration <= 0f ||
                !float.IsFinite(maximumTurnRateDegrees) || maximumTurnRateDegrees <= 0f ||
                !float.IsFinite(selectedAgePositionTolerancePerSecond) || selectedAgePositionTolerancePerSecond < 0f ||
                !float.IsFinite(selectedAgeFacingTolerancePerSecond) || selectedAgeFacingTolerancePerSecond < 0f ||
                !float.IsFinite(selectedAgeConfidenceDecayPerSecond) || selectedAgeConfidenceDecayPerSecond < 0f)
                throw new ArgumentException("Compiled Motion Matching Trajectory policy scalar is invalid.");
            if (points == null || points.Length == 0)
                throw new ArgumentException("Compiled Motion Matching Trajectory policy has no points.", nameof(points));
            m_Points = (MotionMatchingTrajectoryPolicyRuntimePoint[])points.Clone();
            for (int i = 1; i < m_Points.Length; i++)
            {
                if (m_Points[i].TimeOffset <= m_Points[i - 1].TimeOffset)
                    throw new ArgumentException("Compiled Motion Matching Trajectory policy points are not strictly ordered.", nameof(points));
            }
            Revision = revision;
            MaximumAcceleration = maximumAcceleration;
            MaximumTurnRateDegrees = maximumTurnRateDegrees;
            SelectedAgePositionTolerancePerSecond = selectedAgePositionTolerancePerSecond;
            SelectedAgeFacingTolerancePerSecond = selectedAgeFacingTolerancePerSecond;
            SelectedAgeConfidenceDecayPerSecond = selectedAgeConfidenceDecayPerSecond;
        }

        public string PolicyId { get; }
        public int Revision { get; }
        public float MaximumAcceleration { get; }
        public float MaximumTurnRateDegrees { get; }
        public float SelectedAgePositionTolerancePerSecond { get; }
        public float SelectedAgeFacingTolerancePerSecond { get; }
        public float SelectedAgeConfidenceDecayPerSecond { get; }
        public int PointCount => m_Points.Length;
        public MotionMatchingTrajectoryPolicyRuntimePoint GetPoint(int index) => m_Points[index];
    }

    internal sealed class CharacterMotionMatchingTrajectoryRuntime
    {
        readonly MotionMatchingTrajectoryPolicyPayload m_Policy;
        ulong m_CommittedResetSequence;
        ulong m_PendingResetSequence;
        bool m_FrameOpen;

        public CharacterMotionMatchingTrajectoryRuntime(MotionMatchingTrajectoryPolicyPayload policy)
        {
            m_Policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public void Build(MotionMatchingTrajectorySourceFrame source, MotionMatchingTrajectoryEnvelope output)
        {
            RequireOpenFrame();
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (output.Capacity < m_Policy.PointCount)
                throw new InvalidOperationException("Motion Matching Trajectory Envelope capacity is smaller than the compiled policy.");
            if (source.ResetSequence != m_PendingResetSequence)
            {
                output.Clear();
                m_PendingResetSequence = source.ResetSequence;
            }
            output.Begin(source);
            Quaternion worldToLocal = Quaternion.Inverse(source.WorldRotation);
            Vector2 currentVelocity = ToPlanar(worldToLocal * ToWorld(source.PlanarVelocity));
            Vector2 desiredVelocity = ToPlanar(worldToLocal * ToWorld(source.DesiredPlanarVelocity));
            Vector2 desiredFacing = ToPlanar(worldToLocal * ToWorld(source.DesiredFacing)).normalized;

            for (int i = 0; i < m_Policy.PointCount; i++)
            {
                MotionMatchingTrajectoryPolicyRuntimePoint policyPoint = m_Policy.GetPoint(i);
                float time = policyPoint.TimeOffset;
                Vector2 position;
                Vector2 facing;
                float positionTolerance;
                float facingTolerance;
                float confidence;
                if (source.Kind == MotionMatchingTrajectorySourceKind.AcceptedIntent)
                {
                    float acceleration = Mathf.Min(source.AcceptedAcceleration, m_Policy.MaximumAcceleration);
                    Vector2 futureVelocity = Vector2.MoveTowards(currentVelocity, desiredVelocity, acceleration * time);
                    position = (currentVelocity + futureVelocity) * (0.5f * time);
                    float turnRate = Mathf.Min(source.AcceptedTurnRateDegrees, m_Policy.MaximumTurnRateDegrees);
                    facing = RotateTowards(Vector2.up, desiredFacing, turnRate * time);
                    positionTolerance = policyPoint.AcceptedPositionTolerance;
                    facingTolerance = policyPoint.AcceptedFacingToleranceDegrees;
                    confidence = policyPoint.AcceptedConfidence;
                }
                else
                {
                    position = currentVelocity * time;
                    facing = Rotate(Vector2.up, source.YawVelocityDegrees * time);
                    positionTolerance = policyPoint.SelectedPositionTolerance + source.SampleAge * m_Policy.SelectedAgePositionTolerancePerSecond;
                    facingTolerance = policyPoint.SelectedFacingToleranceDegrees + source.SampleAge * m_Policy.SelectedAgeFacingTolerancePerSecond;
                    confidence = Mathf.Clamp01(policyPoint.SelectedConfidence - source.SampleAge * m_Policy.SelectedAgeConfidenceDecayPerSecond);
                }
                output.Add(new MotionMatchingTrajectoryEnvelopePoint(
                    time,
                    position,
                    facing,
                    positionTolerance,
                    facingTolerance,
                    confidence));
            }
        }

        public void Reset(ulong resetSequence)
        {
            if (m_FrameOpen)
                throw new InvalidOperationException("Motion Matching Trajectory cannot reset while a frame is open.");
            m_CommittedResetSequence = resetSequence;
            m_PendingResetSequence = resetSequence;
        }

        public void RetargetResetSequence(ulong resetSequence)
        {
            if (m_FrameOpen)
                throw new InvalidOperationException("Motion Matching Trajectory cannot retarget during a frame.");
            m_CommittedResetSequence = resetSequence;
            m_PendingResetSequence = resetSequence;
        }

        internal void BeginFrame()
        {
            if (m_FrameOpen)
                throw new InvalidOperationException("Motion Matching Trajectory frame is already open.");
            m_PendingResetSequence = m_CommittedResetSequence;
            m_FrameOpen = true;
        }

        internal void CommitFrame()
        {
            RequireOpenFrame();
            m_CommittedResetSequence = m_PendingResetSequence;
            m_FrameOpen = false;
        }

        internal void DiscardFrame()
        {
            RequireOpenFrame();
            m_PendingResetSequence = m_CommittedResetSequence;
            m_FrameOpen = false;
        }

        static Vector3 ToWorld(Vector2 planar) => new Vector3(planar.x, 0f, planar.y);
        static Vector2 ToPlanar(Vector3 world) => new Vector2(world.x, world.z);

        static Vector2 RotateTowards(Vector2 current, Vector2 target, float maximumDegrees)
        {
            if (target.sqrMagnitude <= 0f)
                return current;
            float delta = Mathf.Clamp(Vector2.SignedAngle(current, target), -maximumDegrees, maximumDegrees);
            return Rotate(current, delta);
        }

        static Vector2 Rotate(Vector2 value, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos).normalized;
        }

        void RequireOpenFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException("Motion Matching Trajectory has no open frame.");
        }
    }
}
