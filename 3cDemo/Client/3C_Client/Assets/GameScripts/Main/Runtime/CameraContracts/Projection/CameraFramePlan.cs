using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
public readonly struct CameraFramePlan
    {
        public CameraFramePlan(
            Vector3 followPoint,
            Vector3 aimPoint,
            float fieldOfView,
            float nearClipPlane,
            float farClipPlane,
            Vector2 lookDelta,
            float orbitYaw,
            float orbitPitch,
            float orbitRadius,
            string sequenceId,
            string sourceId,
            ulong sourceActionInstanceId,
            float blendProgress,
            bool resetHistory,
            bool valid,
            float radiusScale = 1f,
            Vector3 cameraOffset = default,
            float rollDegrees = 0f)
        {
            FollowPoint = followPoint;
            AimPoint = aimPoint;
            FieldOfView = fieldOfView;
            NearClipPlane = nearClipPlane;
            FarClipPlane = farClipPlane;
            LookDelta = lookDelta;
            OrbitYaw = orbitYaw;
            OrbitPitch = orbitPitch;
            OrbitRadius = orbitRadius;
            SequenceId = sequenceId ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
            SourceActionInstanceId = sourceActionInstanceId;
            BlendProgress = Mathf.Clamp01(blendProgress);
            ResetHistory = resetHistory;
            RadiusScale = radiusScale;
            CameraOffset = cameraOffset;
            RollDegrees = rollDegrees;
            Valid = valid;
        }

        public Vector3 FollowPoint { get; }
        public Vector3 AimPoint { get; }
        public float FieldOfView { get; }
        public float NearClipPlane { get; }
        public float FarClipPlane { get; }
        public Vector2 LookDelta { get; }
        public float OrbitYaw { get; }
        public float OrbitPitch { get; }
        public float OrbitRadius { get; }
        public string SequenceId { get; }
        public string SourceId { get; }
        public ulong SourceActionInstanceId { get; }
        public float BlendProgress { get; }
        public bool ResetHistory { get; }
        public float RadiusScale { get; }
        public Vector3 CameraOffset { get; }
        public float RollDegrees { get; }
        public bool Valid { get; }

        public static CameraFramePlan Invalid => default;

        public CameraFramePlan WithTargets(Vector3 followPoint, Vector3 aimPoint) => new CameraFramePlan(
            followPoint,
            aimPoint,
            FieldOfView,
            NearClipPlane,
            FarClipPlane,
            LookDelta,
            OrbitYaw,
            OrbitPitch,
            OrbitRadius,
            SequenceId,
            SourceId,
            SourceActionInstanceId,
            BlendProgress,
            ResetHistory,
            Valid,
            RadiusScale,
            cameraOffset: CameraOffset,
            rollDegrees: RollDegrees);

        public CameraFramePlan WithFieldOfView(float fieldOfView) => new CameraFramePlan(
            FollowPoint,
            AimPoint,
            fieldOfView,
            NearClipPlane,
            FarClipPlane,
            LookDelta,
            OrbitYaw,
            OrbitPitch,
            OrbitRadius,
            SequenceId,
            SourceId,
            SourceActionInstanceId,
            BlendProgress,
            ResetHistory,
            Valid,
            RadiusScale,
            CameraOffset,
            RollDegrees);

        public CameraFramePlan WithLookDelta(Vector2 lookDelta) => new CameraFramePlan(
            FollowPoint,
            AimPoint,
            FieldOfView,
            NearClipPlane,
            FarClipPlane,
            lookDelta,
            OrbitYaw,
            OrbitPitch,
            OrbitRadius,
            SequenceId,
            SourceId,
            SourceActionInstanceId,
            BlendProgress,
            ResetHistory,
            Valid,
            RadiusScale,
            CameraOffset,
            RollDegrees);

        public CameraFramePlan WithResetHistory(bool resetHistory) => new CameraFramePlan(
            FollowPoint,
            AimPoint,
            FieldOfView,
            NearClipPlane,
            FarClipPlane,
            LookDelta,
            OrbitYaw,
            OrbitPitch,
            OrbitRadius,
            SequenceId,
            SourceId,
            SourceActionInstanceId,
            BlendProgress,
            resetHistory,
            Valid,
            RadiusScale,
            CameraOffset,
            RollDegrees);

        public CameraFramePlan WithOrbit(float yaw, float pitch, float radius) => new CameraFramePlan(
            FollowPoint,
            AimPoint,
            FieldOfView,
            NearClipPlane,
            FarClipPlane,
            LookDelta,
            yaw,
            pitch,
            radius,
            SequenceId,
            SourceId,
            SourceActionInstanceId,
            BlendProgress,
            ResetHistory,
            Valid,
            RadiusScale,
            CameraOffset,
            RollDegrees);

        public CameraFramePlan WithCameraOffset(Vector3 cameraOffset) => new CameraFramePlan(
            FollowPoint,
            AimPoint,
            FieldOfView,
            NearClipPlane,
            FarClipPlane,
            LookDelta,
            OrbitYaw,
            OrbitPitch,
            OrbitRadius,
            SequenceId,
            SourceId,
            SourceActionInstanceId,
            BlendProgress,
            ResetHistory,
            Valid,
            RadiusScale,
            cameraOffset,
            RollDegrees);

        public CameraFramePlan WithRoll(float rollDegrees) => new CameraFramePlan(
            FollowPoint,
            AimPoint,
            FieldOfView,
            NearClipPlane,
            FarClipPlane,
            LookDelta,
            OrbitYaw,
            OrbitPitch,
            OrbitRadius,
            SequenceId,
            SourceId,
            SourceActionInstanceId,
            BlendProgress,
            ResetHistory,
            Valid,
            RadiusScale,
            CameraOffset,
            rollDegrees);

        public CameraFramePlan WithRadiusScale(float radiusScale) => new CameraFramePlan(
            FollowPoint,
            AimPoint,
            FieldOfView,
            NearClipPlane,
            FarClipPlane,
            LookDelta,
            OrbitYaw,
            OrbitPitch,
            OrbitRadius,
            SequenceId,
            SourceId,
            SourceActionInstanceId,
            BlendProgress,
            ResetHistory,
            Valid,
            radiusScale,
            CameraOffset,
            RollDegrees);
    }
}
