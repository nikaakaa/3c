using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal sealed class CameraDelayModeRuntime
    {
        readonly CameraDelaySettings m_Settings;
        readonly Dictionary<int, CameraDelayModeSettings> m_Modes = new Dictionary<int, CameraDelayModeSettings>();
        readonly Dictionary<(int From, int To), CameraDelayBlendSettings> m_Blends = new Dictionary<(int, int), CameraDelayBlendSettings>();
        CameraDelayModeSettings m_Current;
        CameraDelayModeSettings m_From;
        CameraDelayBlendSettings m_Blend;
        float m_Elapsed;
        int m_Mode;
        bool m_PreviousRotationControl;

        public CameraDelayModeRuntime(CameraDelaySettings settings)
        {
            m_Settings = settings;
            for (int i = 0; i < settings.Modes.Count; i++)
                m_Modes.Add(settings.Modes[i].Mode, settings.Modes[i]);
            for (int i = 0; i < settings.Blends.Count; i++)
            {
                CameraDelayBlendSettings blend = settings.Blends[i];
                if (blend.FromMode != 99)
                    continue;
                for (int j = 0; j < settings.Modes.Count; j++)
                    m_Blends[(settings.Modes[j].Mode, blend.ToMode)] = blend;
            }
            for (int i = 0; i < settings.Blends.Count; i++)
            {
                CameraDelayBlendSettings blend = settings.Blends[i];
                if (blend.FromMode != 99)
                    m_Blends[(blend.FromMode, blend.ToMode)] = blend;
            }
            Reset();
        }

        public int Mode => m_Mode;
        public float MinimumDistanceRatio => m_Current.MinimumDistanceRatio;

        public void Reset()
        {
            m_Mode = m_Settings.DefaultMode;
            m_Current = m_Modes[m_Mode];
            m_From = m_Current;
            m_Blend = default;
            m_Elapsed = 0f;
            m_PreviousRotationControl = false;
        }

        public CameraDelayOrbitSettings Evaluate(in CameraFrameInput input, bool rotationControl, float elevation)
        {
            int requestedMode = m_Settings.DefaultMode;
            bool explicitChange = false;
            if (m_Settings.AutoChangeMode)
            {
                if (rotationControl)
                {
                    requestedMode = 3;
                    explicitChange = true;
                }
                else if (m_PreviousRotationControl &&
                         (!m_Settings.FollowAnimationState ||
                          (input.CharacterState & CameraCharacterState.Idle) != 0 ||
                          input.HasMoveInput && (input.CharacterState & (CameraCharacterState.Move | CameraCharacterState.Evade)) != 0))
                {
                    requestedMode = 2;
                    explicitChange = true;
                }
            }
            if (input.PresentationDeltaSeconds > 0f && !input.Paused)
            {
                if (requestedMode != m_Mode &&
                    (explicitChange || m_Elapsed >= m_Blend.Duration + m_Blend.StableTime))
                {
                    m_From = m_Current;
                    m_Blend = m_Blends[(m_Mode, requestedMode)];
                    m_Mode = requestedMode;
                    m_Elapsed = 0f;
                }
                m_Elapsed += input.PresentationDeltaSeconds;
                CameraDelayModeSettings target = m_Modes[m_Mode];
                m_Current = m_Elapsed >= m_Blend.Duration + m_Blend.StableTime
                    ? target
                    : BlendMode(m_From, target, m_Blend.Curve.Evaluate(m_Elapsed / m_Blend.Duration));
                m_PreviousRotationControl = rotationControl;
            }
            return elevation > 0.5f
                ? BlendOrbit(m_Current.Middle, m_Current.Top, (elevation - 0.5f) * 2f / m_Settings.OrbitLerpTime)
                : BlendOrbit(m_Current.Bottom, m_Current.Middle, elevation * 2f / m_Settings.OrbitLerpTime);
        }

        static CameraDelayModeSettings BlendMode(CameraDelayModeSettings from, CameraDelayModeSettings to, float t) =>
            new CameraDelayModeSettings(to.Mode,
                Mathf.Lerp(from.FieldOfView, to.FieldOfView, t),
                Mathf.Lerp(from.MinimumDistanceRatio, to.MinimumDistanceRatio, t),
                BlendOrbit(from.Bottom, to.Bottom, t),
                BlendOrbit(from.Middle, to.Middle, t),
                BlendOrbit(from.Top, to.Top, t));

        static CameraDelayDirectionSettings BlendDirection(CameraDelayDirectionSettings from, CameraDelayDirectionSettings to, float t) =>
            new CameraDelayDirectionSettings(Mathf.Lerp(from.Idle, to.Idle, t),
                Mathf.Lerp(from.Side, to.Side, t), Mathf.Lerp(from.Forward, to.Forward, t),
                Mathf.Lerp(from.Backward, to.Backward, t));

        static CameraDelayOrbitSettings BlendOrbit(CameraDelayOrbitSettings from, CameraDelayOrbitSettings to, float t) =>
            new CameraDelayOrbitSettings(
                Mathf.Lerp(from.FollowRotateCoefficient, to.FollowRotateCoefficient, t),
                Vector3.Lerp(from.FollowPositionDamping, to.FollowPositionDamping, t),
                Vector3.Lerp(from.FollowRotationDamping, to.FollowRotationDamping, t),
                BlendDirection(from.FollowDirection, to.FollowDirection, t), to.FollowAnimation,
                Vector2.Lerp(from.CompositionDamping, to.CompositionDamping, t),
                Mathf.Lerp(from.RotateDamping, to.RotateDamping, t),
                Vector2.Lerp(from.ScreenPosition, to.ScreenPosition, t),
                Vector2.Lerp(from.DeadZone, to.DeadZone, t), Vector2.Lerp(from.SoftZone, to.SoftZone, t),
                Vector2.Lerp(from.Bias, to.Bias, t),
                BlendDirection(from.LookAtDirection, to.LookAtDirection, t), to.LookAtAnimation);
    }

    internal sealed class CameraDelayDirectionRuntime
    {
        readonly Vector3[] m_Directions = new Vector3[3];
        readonly Vector2[] m_Knots = new Vector2[5];
        readonly Vector2[] m_RightHandSide = new Vector2[4];
        readonly Vector2[] m_FirstControl = new Vector2[4];
        readonly float[] m_Diagonal = new float[4];
        Vector3 m_DirectionSum;
        int m_DirectionIndex;

        public void Reset()
        {
            System.Array.Clear(m_Directions, 0, m_Directions.Length);
            m_DirectionSum = Vector3.zero;
            m_DirectionIndex = 0;
        }

        public void AddModelForward(Vector3 direction)
        {
            m_DirectionSum += direction - m_Directions[m_DirectionIndex];
            m_Directions[m_DirectionIndex] = direction;
            m_DirectionIndex = (m_DirectionIndex + 1) % m_Directions.Length;
        }

        public float Evaluate(CameraDelayDirectionSettings settings, Quaternion previousCameraRotation)
        {
            m_Knots[0] = Vector2.zero;
            m_Knots[1] = new Vector2(0f, settings.Forward);
            m_Knots[2] = new Vector2(settings.Side, 0f);
            m_Knots[3] = new Vector2(0f, -settings.Backward);
            m_Knots[4] = Vector2.zero;
            m_Diagonal[0] = 2f;
            m_RightHandSide[0] = m_Knots[0] + 2f * m_Knots[1];
            for (int i = 1; i < 3; i++)
            {
                m_Diagonal[i] = 4f;
                m_RightHandSide[i] = 4f * m_Knots[i] + 2f * m_Knots[i + 1];
            }
            m_Diagonal[3] = 7f;
            m_RightHandSide[3] = 8f * m_Knots[3] + m_Knots[4];
            for (int i = 1; i < 4; i++)
            {
                float coefficient = (i == 3 ? 2f : 1f) / m_Diagonal[i - 1];
                m_Diagonal[i] -= coefficient;
                m_RightHandSide[i] -= coefficient * m_RightHandSide[i - 1];
            }
            m_FirstControl[3] = m_RightHandSide[3] / m_Diagonal[3];
            for (int i = 2; i >= 0; i--)
                m_FirstControl[i] = (m_RightHandSide[i] - m_FirstControl[i + 1]) / m_Diagonal[i];
            Vector3 cameraBackward = Vector3.ProjectOnPlane(previousCameraRotation * Vector3.back, Vector3.up).normalized;
            float ratio = 1f - Vector3.Angle(cameraBackward, m_DirectionSum.normalized) / 180f;
            int segment = ratio > 0.5f ? 2 : 1;
            float t = Mathf.Clamp01((ratio - (segment == 2 ? 0.5f : 0f)) * 2f);
            float u = 1f - t;
            Vector2 secondControl = 2f * m_Knots[segment + 1] - m_FirstControl[segment + 1];
            Vector2 sample = u * u * u * m_Knots[segment] +
                3f * u * u * t * m_FirstControl[segment] +
                3f * u * t * t * secondControl + t * t * t * m_Knots[segment + 1];
            return sample.magnitude;
        }
    }
}
