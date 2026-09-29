using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    [Serializable]
    public sealed class CameraDelaySettings
    {
        [SerializeField] bool m_Muted;
        [SerializeField] bool m_AutoChangeMode;
        [SerializeField] bool m_FollowAnimationState;
        [SerializeField] int m_DefaultMode;
        [SerializeField] float m_SpeedSmoothTime;
        [SerializeField] float m_OrbitLerpTime;
        [SerializeField] float m_OverAxisProtectRadius;
        [SerializeField] CameraDelayModeSettings[] m_Modes;
        [SerializeField] CameraDelayBlendSettings[] m_Blends;
        [SerializeField] CameraVerticalDelaySettings m_Upward;
        [SerializeField] CameraVerticalDelaySettings m_Downward;

        public bool Muted => m_Muted;
        public bool AutoChangeMode => m_AutoChangeMode;
        public bool FollowAnimationState => m_FollowAnimationState;
        public int DefaultMode => m_DefaultMode;
        public float SpeedSmoothTime => m_SpeedSmoothTime;
        public float OrbitLerpTime => m_OrbitLerpTime;
        public float OverAxisProtectRadius => m_OverAxisProtectRadius;
        public IReadOnlyList<CameraDelayModeSettings> Modes => m_Modes;
        public IReadOnlyList<CameraDelayBlendSettings> Blends => m_Blends;
        public CameraVerticalDelaySettings Upward => m_Upward;
        public CameraVerticalDelaySettings Downward => m_Downward;

        internal CameraDelaySettings(CameraDelaySettings source)
            : this(source.Muted, source.AutoChangeMode, source.FollowAnimationState,
                source.DefaultMode, source.SpeedSmoothTime, source.OrbitLerpTime,
                source.OverAxisProtectRadius, new CameraDelayModeSettings[source.Modes.Count],
                new CameraDelayBlendSettings[source.Blends.Count],
                new CameraVerticalDelaySettings(source.Upward), new CameraVerticalDelaySettings(source.Downward))
        {
            for (int i = 0; i < m_Modes.Length; i++)
                m_Modes[i] = new CameraDelayModeSettings(source.Modes[i]);
            for (int i = 0; i < m_Blends.Length; i++)
                m_Blends[i] = new CameraDelayBlendSettings(source.Blends[i]);
        }

        public CameraDelaySettings(
            bool muted,
            bool autoChangeMode,
            bool followAnimationState,
            int defaultMode,
            float speedSmoothTime,
            float orbitLerpTime,
            float overAxisProtectRadius,
            CameraDelayModeSettings[] modes,
            CameraDelayBlendSettings[] blends,
            CameraVerticalDelaySettings upward,
            CameraVerticalDelaySettings downward)
        {
            m_Muted = muted;
            m_AutoChangeMode = autoChangeMode;
            m_FollowAnimationState = followAnimationState;
            m_DefaultMode = defaultMode;
            m_SpeedSmoothTime = speedSmoothTime;
            m_OrbitLerpTime = orbitLerpTime;
            m_OverAxisProtectRadius = overAxisProtectRadius;
            m_Modes = modes;
            m_Blends = blends;
            m_Upward = upward;
            m_Downward = downward;
        }
    }

    [Serializable]
    public struct CameraDelayModeSettings
    {
        [SerializeField] int m_Mode;
        [SerializeField] float m_FieldOfView;
        [SerializeField] float m_MinimumDistanceRatio;
        [SerializeField] CameraDelayOrbitSettings m_Bottom;
        [SerializeField] CameraDelayOrbitSettings m_Middle;
        [SerializeField] CameraDelayOrbitSettings m_Top;

        public int Mode => m_Mode;
        public float FieldOfView => m_FieldOfView;
        public float MinimumDistanceRatio => m_MinimumDistanceRatio;
        public CameraDelayOrbitSettings Bottom => m_Bottom;
        public CameraDelayOrbitSettings Middle => m_Middle;
        public CameraDelayOrbitSettings Top => m_Top;

        internal CameraDelayModeSettings(CameraDelayModeSettings source)
            : this(source.Mode, source.FieldOfView, source.MinimumDistanceRatio,
                new CameraDelayOrbitSettings(source.Bottom), new CameraDelayOrbitSettings(source.Middle),
                new CameraDelayOrbitSettings(source.Top)) { }

        public CameraDelayModeSettings(
            int mode,
            float fieldOfView,
            float minimumDistanceRatio,
            CameraDelayOrbitSettings bottom,
            CameraDelayOrbitSettings middle,
            CameraDelayOrbitSettings top)
        {
            m_Mode = mode;
            m_FieldOfView = fieldOfView;
            m_MinimumDistanceRatio = minimumDistanceRatio;
            m_Bottom = bottom;
            m_Middle = middle;
            m_Top = top;
        }
    }

    [Serializable]
    public struct CameraDelayOrbitSettings
    {
        [SerializeField] float m_FollowRotateCoefficient;
        [SerializeField] Vector3 m_FollowPositionDamping;
        [SerializeField] Vector3 m_FollowRotationDamping;
        [SerializeField] CameraDelayDirectionSettings m_FollowDirection;
        [SerializeField] CameraDelayAnimationSettings m_FollowAnimation;
        [SerializeField] Vector2 m_CompositionDamping;
        [SerializeField] float m_RotateDamping;
        [SerializeField] Vector2 m_ScreenPosition;
        [SerializeField] Vector2 m_DeadZone;
        [SerializeField] Vector2 m_SoftZone;
        [SerializeField] Vector2 m_Bias;
        [SerializeField] CameraDelayDirectionSettings m_LookAtDirection;
        [SerializeField] CameraDelayAnimationSettings m_LookAtAnimation;

        public float FollowRotateCoefficient => m_FollowRotateCoefficient;
        public Vector3 FollowPositionDamping => m_FollowPositionDamping;
        public Vector3 FollowRotationDamping => m_FollowRotationDamping;
        public CameraDelayDirectionSettings FollowDirection => m_FollowDirection;
        public CameraDelayAnimationSettings FollowAnimation => m_FollowAnimation;
        public Vector2 CompositionDamping => m_CompositionDamping;
        public float RotateDamping => m_RotateDamping;
        public Vector2 ScreenPosition => m_ScreenPosition;
        public Vector2 DeadZone => m_DeadZone;
        public Vector2 SoftZone => m_SoftZone;
        public Vector2 Bias => m_Bias;
        public CameraDelayDirectionSettings LookAtDirection => m_LookAtDirection;
        public CameraDelayAnimationSettings LookAtAnimation => m_LookAtAnimation;

        internal CameraDelayOrbitSettings(CameraDelayOrbitSettings source)
            : this(source.FollowRotateCoefficient, source.FollowPositionDamping, source.FollowRotationDamping,
                source.FollowDirection, new CameraDelayAnimationSettings(source.FollowAnimation),
                source.CompositionDamping, source.RotateDamping, source.ScreenPosition, source.DeadZone,
                source.SoftZone, source.Bias, source.LookAtDirection,
                new CameraDelayAnimationSettings(source.LookAtAnimation)) { }

        public CameraDelayOrbitSettings(
            float followRotateCoefficient,
            Vector3 followPositionDamping,
            Vector3 followRotationDamping,
            CameraDelayDirectionSettings followDirection,
            CameraDelayAnimationSettings followAnimation,
            Vector2 compositionDamping,
            float rotateDamping,
            Vector2 screenPosition,
            Vector2 deadZone,
            Vector2 softZone,
            Vector2 bias,
            CameraDelayDirectionSettings lookAtDirection,
            CameraDelayAnimationSettings lookAtAnimation)
        {
            m_FollowRotateCoefficient = followRotateCoefficient;
            m_FollowPositionDamping = followPositionDamping;
            m_FollowRotationDamping = followRotationDamping;
            m_FollowDirection = followDirection;
            m_FollowAnimation = followAnimation;
            m_CompositionDamping = compositionDamping;
            m_RotateDamping = rotateDamping;
            m_ScreenPosition = screenPosition;
            m_DeadZone = deadZone;
            m_SoftZone = softZone;
            m_Bias = bias;
            m_LookAtDirection = lookAtDirection;
            m_LookAtAnimation = lookAtAnimation;
        }
    }

    [Serializable]
    public struct CameraDelayDirectionSettings
    {
        [SerializeField] float m_Idle;
        [SerializeField] float m_Side;
        [SerializeField] float m_Forward;
        [SerializeField] float m_Backward;

        public float Idle => m_Idle;
        public float Side => m_Side;
        public float Forward => m_Forward;
        public float Backward => m_Backward;

        public CameraDelayDirectionSettings(
            float idle,
            float side,
            float forward,
            float backward)
        {
            m_Idle = idle;
            m_Side = side;
            m_Forward = forward;
            m_Backward = backward;
        }
    }

    [Serializable]
    public struct CameraDelayAnimationSettings
    {
        [SerializeField] CameraDelayStateRatio[] m_States;
        [SerializeField] CameraDelayTagRatio[] m_Tags;

        public IReadOnlyList<CameraDelayStateRatio> States => m_States;
        public IReadOnlyList<CameraDelayTagRatio> Tags => m_Tags;

        internal CameraDelayAnimationSettings(CameraDelayAnimationSettings source)
            : this((CameraDelayStateRatio[])source.m_States.Clone(), (CameraDelayTagRatio[])source.m_Tags.Clone()) { }

        public CameraDelayAnimationSettings(
            CameraDelayStateRatio[] states,
            CameraDelayTagRatio[] tags)
        {
            m_States = states;
            m_Tags = tags;
        }
    }

    [Serializable]
    public struct CameraDelayStateRatio
    {
        [SerializeField] string m_State;
        [SerializeField] float m_Ratio;

        public string State => m_State;
        public float Ratio => m_Ratio;

        public CameraDelayStateRatio(
            string state,
            float ratio)
        {
            m_State = state;
            m_Ratio = ratio;
        }
    }

    [Serializable]
    public struct CameraDelayTagRatio
    {
        [SerializeField] int m_Tag;
        [SerializeField] float m_Ratio;

        public int Tag => m_Tag;
        public float Ratio => m_Ratio;

        public CameraDelayTagRatio(
            int tag,
            float ratio)
        {
            m_Tag = tag;
            m_Ratio = ratio;
        }
    }

    [Serializable]
    public struct CameraDelayBlendSettings
    {
        [SerializeField] int m_FromMode;
        [SerializeField] int m_ToMode;
        [SerializeField] int m_Style;
        [SerializeField] float m_Duration;
        [SerializeField] float m_StableTime;
        [SerializeField] AnimationCurve m_Curve;

        public int FromMode => m_FromMode;
        public int ToMode => m_ToMode;
        public int Style => m_Style;
        public float Duration => m_Duration;
        public float StableTime => m_StableTime;
        public AnimationCurve Curve => m_Curve;

        internal CameraDelayBlendSettings(CameraDelayBlendSettings source)
            : this(source.FromMode, source.ToMode, source.Style, source.Duration, source.StableTime,
                new AnimationCurve(source.Curve.keys)
                {
                    preWrapMode = source.Curve.preWrapMode,
                    postWrapMode = source.Curve.postWrapMode
                }) { }

        public CameraDelayBlendSettings(
            int fromMode,
            int toMode,
            int style,
            float duration,
            float stableTime,
            AnimationCurve curve)
        {
            m_FromMode = fromMode;
            m_ToMode = toMode;
            m_Style = style;
            m_Duration = duration;
            m_StableTime = stableTime;
            m_Curve = curve;
        }
    }

    [Serializable]
    public struct CameraVerticalDelaySettings
    {
        [SerializeField] float m_VelocityThreshold;
        [SerializeField] float m_DampingRatio;
        [SerializeField] float m_Duration;
        [SerializeField] AnimationCurve m_Curve;

        public float VelocityThreshold => m_VelocityThreshold;
        public float DampingRatio => m_DampingRatio;
        public float Duration => m_Duration;
        public AnimationCurve Curve => m_Curve;

        internal CameraVerticalDelaySettings(CameraVerticalDelaySettings source)
            : this(source.VelocityThreshold, source.DampingRatio, source.Duration,
                new AnimationCurve(source.Curve.keys)
                {
                    preWrapMode = source.Curve.preWrapMode,
                    postWrapMode = source.Curve.postWrapMode
                }) { }

        public CameraVerticalDelaySettings(
            float velocityThreshold,
            float dampingRatio,
            float duration,
            AnimationCurve curve)
        {
            m_VelocityThreshold = velocityThreshold;
            m_DampingRatio = dampingRatio;
            m_Duration = duration;
            m_Curve = curve;
        }
    }
}
