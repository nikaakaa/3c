using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    public readonly struct CameraLensPlan
    {
        public CameraLensPlan(float fieldOfView, float nearClipPlane, float farClipPlane)
        {
            FieldOfView = fieldOfView;
            NearClipPlane = nearClipPlane;
            FarClipPlane = farClipPlane;
        }

        public float FieldOfView { get; }
        public float NearClipPlane { get; }
        public float FarClipPlane { get; }

        public CameraLensPlan WithFieldOfView(float fieldOfView) =>
            new CameraLensPlan(fieldOfView, NearClipPlane, FarClipPlane);
    }

    public struct CameraFramePlan
    {
        Vector3 m_FollowPoint;
        Vector3 m_AimPoint;
        CameraLensPlan m_Lens;
        Vector2 m_LookDelta;
        CameraOrbitComposition m_Orbit;
        string m_SequenceId;
        string m_SourceId;
        ulong m_SourceActionInstanceId;
        float m_BlendProgress;
        bool m_ResetHistory;
        Vector3 m_CameraOffset;
        float m_RollDegrees;
        bool m_Valid;

        public CameraFramePlan(
            Vector3 followPoint,
            Vector3 aimPoint,
            CameraLensPlan lens,
            Vector2 lookDelta,
            CameraOrbitComposition orbit,
            string sequenceId,
            string sourceId,
            ulong sourceActionInstanceId,
            float blendProgress,
            bool resetHistory,
            bool valid,
            Vector3 cameraOffset = default,
            float rollDegrees = 0f)
        {
            m_FollowPoint = followPoint;
            m_AimPoint = aimPoint;
            m_Lens = lens;
            m_LookDelta = lookDelta;
            m_Orbit = orbit;
            m_SequenceId = sequenceId ?? string.Empty;
            m_SourceId = sourceId ?? string.Empty;
            m_SourceActionInstanceId = sourceActionInstanceId;
            m_BlendProgress = Mathf.Clamp01(blendProgress);
            m_ResetHistory = resetHistory;
            m_CameraOffset = cameraOffset;
            m_RollDegrees = rollDegrees;
            m_Valid = valid;
        }

        public Vector3 FollowPoint => m_FollowPoint;
        public Vector3 AimPoint => m_AimPoint;
        public float FieldOfView => m_Lens.FieldOfView;
        public float NearClipPlane => m_Lens.NearClipPlane;
        public float FarClipPlane => m_Lens.FarClipPlane;
        public Vector2 LookDelta => m_LookDelta;
        public float OrbitYaw => m_Orbit.Yaw;
        public float OrbitPitch => m_Orbit.Pitch;
        public float OrbitRadius => m_Orbit.CenterRadius;
        public string SequenceId => m_SequenceId ?? string.Empty;
        public string SourceId => m_SourceId ?? string.Empty;
        public ulong SourceActionInstanceId => m_SourceActionInstanceId;
        public float BlendProgress => m_BlendProgress;
        public bool ResetHistory => m_ResetHistory;
        public Vector3 CameraOffset => m_CameraOffset;
        public float RollDegrees => m_RollDegrees;
        public CameraOrbitComposition Orbit => m_Orbit;
        public bool Valid => m_Valid;

        public static CameraFramePlan Invalid => default;

        public CameraFramePlan WithTargets(Vector3 followPoint, Vector3 aimPoint)
        {
            CameraFramePlan result = this;
            result.m_FollowPoint = followPoint;
            result.m_AimPoint = aimPoint;
            return result;
        }

        public CameraFramePlan WithFieldOfView(float fieldOfView)
        {
            CameraFramePlan result = this;
            result.m_Lens = m_Lens.WithFieldOfView(fieldOfView);
            return result;
        }

        public CameraFramePlan WithLookDelta(Vector2 lookDelta)
        {
            CameraFramePlan result = this;
            result.m_LookDelta = lookDelta;
            return result;
        }

        public CameraFramePlan WithResetHistory(bool resetHistory)
        {
            CameraFramePlan result = this;
            result.m_ResetHistory = resetHistory;
            return result;
        }

        public CameraFramePlan WithOrbit(float yaw, float pitch, float radius)
        {
            CameraFramePlan result = this;
            result.m_Orbit = m_Orbit.WithPose(yaw, pitch, radius);
            return result;
        }

        public CameraFramePlan WithCameraOffset(Vector3 cameraOffset)
        {
            CameraFramePlan result = this;
            result.m_CameraOffset = cameraOffset;
            return result;
        }

        public CameraFramePlan WithRoll(float rollDegrees)
        {
            CameraFramePlan result = this;
            result.m_RollDegrees = rollDegrees;
            return result;
        }

        public CameraFramePlan WithRadiusScale(float radiusScale)
        {
            CameraFramePlan result = this;
            result.m_Orbit = m_Orbit.Scale(radiusScale);
            return result;
        }
    }
}
