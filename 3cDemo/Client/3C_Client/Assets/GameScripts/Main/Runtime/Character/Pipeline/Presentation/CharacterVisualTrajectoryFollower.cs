using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct CharacterVisualTrajectorySample
    {
        public CharacterVisualTrajectorySample(
            Vector3 position,
            Quaternion rotation,
            Vector3 linearVelocity,
            float yawVelocityDegreesPerSecond,
            bool grounded)
        {
            Position = position;
            Rotation = rotation.normalized;
            LinearVelocity = linearVelocity;
            YawVelocityDegreesPerSecond = yawVelocityDegreesPerSecond;
            Grounded = grounded;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 LinearVelocity { get; }
        public float YawVelocityDegreesPerSecond { get; }
        public bool Grounded { get; }
        public float Yaw => Rotation.eulerAngles.y;
    }

    internal readonly struct CharacterVisualTrajectoryResult
    {
        public CharacterVisualTrajectoryResult(
            Vector3 position,
            Quaternion rotation,
            Vector3 velocity,
            float yawVelocityDegreesPerSecond,
            Vector3 positionError,
            float yawErrorDegrees,
            Vector3 correctionVelocity,
            float yawCorrectionVelocityDegreesPerSecond,
            bool correctionActive,
            bool correctionClamped,
            bool settled)
        {
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
            YawVelocityDegreesPerSecond = yawVelocityDegreesPerSecond;
            PositionError = positionError;
            YawErrorDegrees = yawErrorDegrees;
            CorrectionVelocity = correctionVelocity;
            YawCorrectionVelocityDegreesPerSecond = yawCorrectionVelocityDegreesPerSecond;
            CorrectionActive = correctionActive;
            CorrectionClamped = correctionClamped;
            Settled = settled;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 Velocity { get; }
        public float YawVelocityDegreesPerSecond { get; }
        public Vector3 PositionError { get; }
        public float YawErrorDegrees { get; }
        public Vector3 CorrectionVelocity { get; }
        public float YawCorrectionVelocityDegreesPerSecond { get; }
        public bool CorrectionActive { get; }
        public bool CorrectionClamped { get; }
        public bool Settled { get; }
    }

    internal sealed class CharacterVisualTrajectoryFollower
    {
        const float HalfLifeLambda = 0.69314718056f;

        readonly CharacterBodyPresentationSettings m_Settings;
        Vector3 m_VisiblePosition;
        Vector3 m_VisibleVelocity;
        float m_VisibleYaw;
        float m_VisibleYawVelocity;
        Vector3 m_PositionError;
        Vector3 m_PositionErrorVelocity;
        float m_YawError;
        float m_YawErrorVelocity;
        bool m_Initialized;
        bool m_CorrectionActive;
        bool m_CorrectionClamped;

        public CharacterVisualTrajectoryFollower(CharacterBodyPresentationSettings settings)
        {
            settings.RequireValid(nameof(CharacterVisualTrajectoryFollower));
            m_Settings = settings;
        }

        public CharacterVisualTrajectoryMode Mode => m_Settings.TrajectoryMode;

        public void Reset(CharacterVisualTrajectorySample target)
        {
            m_VisiblePosition = target.Position;
            m_VisibleVelocity = target.LinearVelocity;
            m_VisibleYaw = target.Yaw;
            m_VisibleYawVelocity = target.YawVelocityDegreesPerSecond;
            m_PositionError = Vector3.zero;
            m_PositionErrorVelocity = Vector3.zero;
            m_YawError = 0f;
            m_YawErrorVelocity = 0f;
            m_CorrectionActive = false;
            m_CorrectionClamped = false;
            m_Initialized = true;
        }

        public void Retarget(CharacterVisualTrajectorySample target)
        {
            if (!m_Initialized || m_Settings.TrajectoryMode == CharacterVisualTrajectoryMode.Direct)
            {
                Reset(target);
                return;
            }

            m_CorrectionClamped = false;
            m_PositionError = m_VisiblePosition - target.Position;
            m_PositionErrorVelocity = m_VisibleVelocity - target.LinearVelocity;
            if (target.Grounded)
            {
                m_PositionError.y = 0f;
                m_PositionErrorVelocity.y = 0f;
            }
            ClampPositionError();

            m_YawError = Mathf.DeltaAngle(target.Yaw, m_VisibleYaw);
            m_YawErrorVelocity = m_VisibleYawVelocity - target.YawVelocityDegreesPerSecond;
            ClampYawError();
            SettleIfNeeded();
        }

        public CharacterVisualTrajectoryResult Evaluate(
            CharacterVisualTrajectorySample target,
            float presentationDeltaSeconds)
        {
            if (!m_Initialized)
                Reset(target);
            if (m_Settings.TrajectoryMode == CharacterVisualTrajectoryMode.Direct)
            {
                Reset(target);
                return BuildResult();
            }

            float deltaSeconds = Mathf.Max(0f, presentationDeltaSeconds);
            if (m_CorrectionActive && deltaSeconds > 0f)
            {
                DecayCritical(
                    ref m_PositionError,
                    ref m_PositionErrorVelocity,
                    m_Settings.PositionHalfLifeSeconds,
                    deltaSeconds);
                DecayCritical(
                    ref m_YawError,
                    ref m_YawErrorVelocity,
                    m_Settings.YawHalfLifeSeconds,
                    deltaSeconds);
            }
            if (target.Grounded)
            {
                m_PositionError.y = 0f;
                m_PositionErrorVelocity.y = 0f;
            }
            ClampPositionError();
            ClampYawError();
            SettleIfNeeded();

            m_VisiblePosition = target.Position + m_PositionError;
            m_VisibleVelocity = target.LinearVelocity + m_PositionErrorVelocity;
            if (target.Grounded)
            {
                m_VisiblePosition.y = target.Position.y;
                m_VisibleVelocity.y = target.LinearVelocity.y;
            }
            m_VisibleYaw = target.Yaw + m_YawError;
            m_VisibleYawVelocity = target.YawVelocityDegreesPerSecond + m_YawErrorVelocity;
            return BuildResult();
        }

        public void Clear()
        {
            m_VisiblePosition = Vector3.zero;
            m_VisibleVelocity = Vector3.zero;
            m_VisibleYaw = 0f;
            m_VisibleYawVelocity = 0f;
            m_PositionError = Vector3.zero;
            m_PositionErrorVelocity = Vector3.zero;
            m_YawError = 0f;
            m_YawErrorVelocity = 0f;
            m_Initialized = false;
            m_CorrectionActive = false;
            m_CorrectionClamped = false;
        }

        CharacterVisualTrajectoryResult BuildResult()
        {
            return new CharacterVisualTrajectoryResult(
                m_VisiblePosition,
                Quaternion.Euler(0f, m_VisibleYaw, 0f),
                m_VisibleVelocity,
                m_VisibleYawVelocity,
                m_PositionError,
                m_YawError,
                m_PositionErrorVelocity,
                m_YawErrorVelocity,
                m_CorrectionActive,
                m_CorrectionClamped,
                !m_CorrectionActive);
        }

        void ClampPositionError()
        {
            float maximum = m_Settings.MaximumPositionErrorMeters;
            float magnitude = m_PositionError.magnitude;
            if (magnitude <= maximum)
                return;
            m_PositionError *= maximum / magnitude;
            RemoveOutwardVelocity(ref m_PositionErrorVelocity, m_PositionError);
            m_CorrectionClamped = true;
        }

        void ClampYawError()
        {
            float maximum = m_Settings.MaximumYawErrorDegrees;
            if (Mathf.Abs(m_YawError) <= maximum)
                return;
            m_YawError = Mathf.Clamp(m_YawError, -maximum, maximum);
            if (Mathf.Sign(m_YawErrorVelocity) == Mathf.Sign(m_YawError))
                m_YawErrorVelocity = 0f;
            m_CorrectionClamped = true;
        }

        void SettleIfNeeded()
        {
            float positionVelocityThreshold =
                m_Settings.PositionSettleDistanceMeters / m_Settings.PositionHalfLifeSeconds;
            if (m_PositionError.magnitude <= m_Settings.PositionSettleDistanceMeters &&
                m_PositionErrorVelocity.magnitude <= positionVelocityThreshold)
            {
                m_PositionError = Vector3.zero;
                m_PositionErrorVelocity = Vector3.zero;
            }

            float yawVelocityThreshold = m_Settings.YawSettleDegrees / m_Settings.YawHalfLifeSeconds;
            if (Mathf.Abs(m_YawError) <= m_Settings.YawSettleDegrees &&
                Mathf.Abs(m_YawErrorVelocity) <= yawVelocityThreshold)
            {
                m_YawError = 0f;
                m_YawErrorVelocity = 0f;
            }
            m_CorrectionActive =
                m_PositionError.sqrMagnitude > 0f ||
                m_PositionErrorVelocity.sqrMagnitude > 0f ||
                !Mathf.Approximately(m_YawError, 0f) ||
                !Mathf.Approximately(m_YawErrorVelocity, 0f);
        }

        static void DecayCritical(
            ref Vector3 value,
            ref Vector3 velocity,
            float halfLife,
            float deltaSeconds)
        {
            float damping = HalfLifeLambda / halfLife;
            Vector3 velocityTerm = velocity + damping * value;
            float decay = Mathf.Exp(-damping * deltaSeconds);
            value = (value + velocityTerm * deltaSeconds) * decay;
            velocity = (velocity - damping * velocityTerm * deltaSeconds) * decay;
        }

        static void DecayCritical(
            ref float value,
            ref float velocity,
            float halfLife,
            float deltaSeconds)
        {
            float damping = HalfLifeLambda / halfLife;
            float velocityTerm = velocity + damping * value;
            float decay = Mathf.Exp(-damping * deltaSeconds);
            value = (value + velocityTerm * deltaSeconds) * decay;
            velocity = (velocity - damping * velocityTerm * deltaSeconds) * decay;
        }

        static void RemoveOutwardVelocity(ref Vector3 velocity, Vector3 error)
        {
            float squaredMagnitude = error.sqrMagnitude;
            if (squaredMagnitude <= 0f)
                return;
            float outward = Vector3.Dot(velocity, error) / squaredMagnitude;
            if (outward > 0f)
                velocity -= outward * error;
        }
    }
}
