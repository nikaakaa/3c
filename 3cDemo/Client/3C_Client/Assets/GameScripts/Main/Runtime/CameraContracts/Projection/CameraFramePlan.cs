using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    public readonly struct CameraLensPlan
    {
        public CameraLensPlan(float nearClipPlane, float farClipPlane)
        {
            NearClipPlane = nearClipPlane;
            FarClipPlane = farClipPlane;
        }

        public float NearClipPlane { get; }
        public float FarClipPlane { get; }
    }

    public struct CameraFramePlan
    {
        CameraWorldBasicData m_WorldBasicData;
        Vector3 m_AimOffset;
        CameraLensPlan m_Lens;
        Vector2 m_LookDelta;
        string m_SequenceId;
        string m_SourceId;
        ulong m_SourceActionInstanceId;
        float m_BlendProgress;
        bool m_ResetHistory;
        bool m_Valid;
        CameraCollisionResult m_Collision;
        bool m_IgnoreCollision;
        string m_ShotId;
        float m_CameraLocateRatio;

        public CameraFramePlan(
            CameraWorldBasicData worldBasicData,
            Vector3 aimOffset,
            CameraLensPlan lens,
            Vector2 lookDelta,
            string sequenceId,
            string sourceId,
            ulong sourceActionInstanceId,
            float blendProgress,
            bool resetHistory,
            bool valid,
            float cameraLocateRatio)
        {
            m_WorldBasicData = worldBasicData;
            m_AimOffset = aimOffset;
            m_Lens = lens;
            m_LookDelta = lookDelta;
            m_SequenceId = sequenceId ?? string.Empty;
            m_SourceId = sourceId ?? string.Empty;
            m_SourceActionInstanceId = sourceActionInstanceId;
            m_BlendProgress = Mathf.Clamp01(blendProgress);
            m_ResetHistory = resetHistory;
            m_Valid = valid;
            m_Collision = default;
            m_IgnoreCollision = false;
            m_ShotId = string.Empty;
            m_CameraLocateRatio = cameraLocateRatio;
        }

        public CameraWorldBasicData WorldBasicData => m_WorldBasicData;
        public Vector3 PivotLocation => m_WorldBasicData.PivotLocation;
        public Vector3 Location => m_WorldBasicData.Location;
        public Quaternion Rotation => m_WorldBasicData.Rotation;
        public float Yaw => ResolveYaw(Rotation);
        public float Pitch => ResolvePitch(Rotation);
        public Vector3 AimPoint => m_WorldBasicData.PivotLocation + m_AimOffset;
        public Vector3 AimOffset => m_AimOffset;
        public float FieldOfView => m_WorldBasicData.FieldOfView;
        public float NearClipPlane => m_Lens.NearClipPlane;
        public float FarClipPlane => m_Lens.FarClipPlane;
        public Vector2 LookDelta => m_LookDelta;
        public float Radius => m_WorldBasicData.Radius;
        public float CameraLocateRatio => m_CameraLocateRatio;
        public Vector2 Offset => m_WorldBasicData.Offset;
        public string SequenceId => m_SequenceId ?? string.Empty;
        public string SourceId => m_SourceId ?? string.Empty;
        public ulong SourceActionInstanceId => m_SourceActionInstanceId;
        public float BlendProgress => m_BlendProgress;
        public bool ResetHistory => m_ResetHistory;
        public bool Valid => m_Valid;
        public CameraCollisionResult Collision => m_Collision;
        public bool IgnoreCollision => m_IgnoreCollision;
        public string ShotId => m_ShotId ?? string.Empty;

        public static CameraFramePlan Invalid => default;

        public CameraFramePlan WithWorldBasicData(CameraWorldBasicData worldBasicData)
        {
            CameraFramePlan result = this;
            result.m_WorldBasicData = worldBasicData;
            return result;
        }

        public CameraFramePlan WithAimResolved()
        {
            if (m_AimOffset == Vector3.zero)
                return this;
            Vector3 forward = Rotation * Vector3.forward;
            Vector3 cameraToAim = forward * Radius + m_AimOffset;
            CameraFramePlan result = WithWorldBasicData(m_WorldBasicData
                .WithPivotLocation(AimPoint)
                .WithFraming(cameraToAim.magnitude, FieldOfView)
                .WithRotation(Quaternion.LookRotation(cameraToAim, Vector3.up) *
                    Quaternion.AngleAxis(Rotation.eulerAngles.z, Vector3.forward)));
            result.m_AimOffset = Vector3.zero;
            return result;
        }

        public CameraFramePlan WithPivotLocation(Vector3 pivotLocation)
        {
            return WithWorldBasicData(m_WorldBasicData.WithPivotLocation(pivotLocation));
        }

        public CameraFramePlan WithRotation(Quaternion rotation)
        {
            return WithWorldBasicData(m_WorldBasicData.WithRotation(rotation));
        }

        public CameraFramePlan WithOffset(Vector2 offset)
        {
            return WithWorldBasicData(m_WorldBasicData.WithOffset(offset));
        }

        public CameraFramePlan WithPitchClamped(float minimum, float maximum)
        {
            if (!float.IsFinite(minimum) || !float.IsFinite(maximum) || minimum > maximum)
                throw new ArgumentOutOfRangeException(nameof(minimum));
            float currentPitch = Pitch;
            float pitch = Mathf.Clamp(currentPitch, minimum, maximum);
            if (pitch == currentPitch)
                return this;
            Vector3 euler = Rotation.eulerAngles;
            return WithRotation(Quaternion.Euler(-pitch, euler.y, euler.z));
        }

        public CameraFramePlan WithLens(CameraLensPlan lens)
        {
            CameraFramePlan result = this;
            result.m_Lens = lens;
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

        public CameraFramePlan WithCollision(CameraCollisionResult collision)
        {
            CameraFramePlan result = this;
            result.m_Collision = collision;
            return result;
        }

        public CameraFramePlan WithValidity(bool valid)
        {
            CameraFramePlan result = this;
            result.m_Valid = valid;
            return result;
        }

        public CameraFramePlan WithIgnoreCollision(bool ignoreCollision)
        {
            CameraFramePlan result = this;
            result.m_IgnoreCollision = ignoreCollision;
            return result;
        }

        public CameraFramePlan WithShotId(string shotId)
        {
            CameraFramePlan result = this;
            result.m_ShotId = shotId ?? string.Empty;
            return result;
        }

        static float ResolveYaw(Quaternion rotation)
        {
            Vector3 forward = rotation * Vector3.forward;
            Vector3 planar = Vector3.ProjectOnPlane(forward, Vector3.up);
            return planar.sqrMagnitude <= 0.000001f
                ? 0f
                : Mathf.Atan2(planar.x, planar.z) * Mathf.Rad2Deg;
        }

        static float ResolvePitch(Quaternion rotation) =>
            Mathf.Asin(Mathf.Clamp((rotation * Vector3.forward).y, -1f, 1f)) * Mathf.Rad2Deg;

    }
}
