using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal sealed class CameraShotEffectEvaluator : ICameraEffectOwner
    {
        readonly CharacterCameraProjectionPayload m_Projection;

        public CameraShotEffectEvaluator(CharacterCameraProjectionPayload projection)
        {
            m_Projection = projection;
        }

        public CameraEffectKind Kind => CameraEffectKind.Shot;
        public CameraEffectStage Stage => CameraEffectStage.Shot;
        public bool UpdatesBySource => false;

        public bool HasResource(string resourceId) =>
            m_Projection.TryGetShot(resourceId, out _);

        public CameraFramePlan Apply(
            CameraFramePlan plan,
            IReadOnlyList<CameraEffectRuntimeState> active,
            in CameraFrameInput input)
        {
            CameraEffectRuntimeState state = CameraEffectRuntimeStateStore.Select(active, Kind);
            if (state == null || !m_Projection.TryGetShot(state.Request.ResourceId, out CameraShotPayload payload))
                return plan;
            if (!FindTarget(input.Targets, payload.FollowTargetSlotId, out CameraTargetSnapshot follow))
                throw new InvalidOperationException(
                    $"Camera Shot '{payload.ShotId}' requires follow target slot '{payload.FollowTargetSlotId}'.");
            if (!FindTarget(input.Targets, payload.LookAtTargetSlotId, out CameraTargetSnapshot lookAt))
                throw new InvalidOperationException(
                    $"Camera Shot '{payload.ShotId}' requires look-at target slot '{payload.LookAtTargetSlotId}'.");
            float envelope = state.Retired
                ? CameraEffectEvaluationMath.ResolveRetiredWeight(
                    state,
                    0f,
                    payload.BlendIn.Duration,
                    payload.Duration < 0f
                        ? -1f
                        : Mathf.Max(0f, payload.Duration - payload.BlendIn.Duration - payload.BlendOut.Duration),
                    payload.BlendOut.Duration,
                    payload.BlendIn.Curve,
                    payload.BlendOut.Curve)
                : CameraEffectEvaluationMath.ResolvePhaseWeight(
                    state.Elapsed,
                    0f,
                    payload.BlendIn.Duration,
                    payload.Duration < 0f
                        ? -1f
                        : Mathf.Max(0f, payload.Duration - payload.BlendIn.Duration - payload.BlendOut.Duration),
                    payload.BlendOut.Duration,
                    payload.BlendIn.Curve,
                    payload.BlendOut.Curve);
            envelope *= state.Request.Weight;
            Vector3 followPoint = follow.AnchorPoint + payload.FollowOffset;
            Vector3 aimPoint = lookAt.AimPoint + payload.LookAtOffset;
            Vector3 location = plan.Location;
            Vector3 direction = aimPoint - location;
            Quaternion rotation = direction.sqrMagnitude > 0.000001f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : plan.Rotation;
            rotation = rotation * Quaternion.Euler(payload.OffsetRotation);
            if (payload.BlendWithIgnoreLookAtTarget && envelope < 1f)
                rotation = plan.Rotation;
            float radius = Mathf.Max(
                0.01f,
                Vector3.Dot(followPoint - location, -(rotation * Vector3.forward)));
            if (!float.IsFinite(radius) || radius <= 0.01f)
                radius = plan.Radius;
            CameraWorldBasicData target = new CameraWorldBasicData(
                aimPoint,
                rotation,
                radius,
                Vector2.zero,
                payload.FieldOfView);
            return plan
                .WithWorldBasicData(CameraWorldBasicData.Lerp(
                    plan.WorldBasicData,
                    target,
                    envelope))
                .WithLens(new CameraLensPlan(
                    Mathf.LerpUnclamped(plan.NearClipPlane, payload.NearClipPlane, envelope),
                    Mathf.LerpUnclamped(plan.FarClipPlane, payload.FarClipPlane, envelope)))
                .WithIgnoreCollision(payload.IgnoreCameraCollision && envelope > 0f)
                .WithShotId(envelope > 0f ? payload.ShotId : string.Empty);
        }

        public float ResolveDelta(CameraEffectRuntimeState active, in CameraFrameInput input)
        {
            CameraShotPayload payload = m_Projection.TryGetShot(
                active.Request.ResourceId,
                out CameraShotPayload value)
                ? value
                : throw new InvalidOperationException(
                    $"Camera Shot resource '{active.Request.ResourceId}' is not present in the Projection.");
            return payload.ApplyEntityTimeScale
                ? input.Delta(payload.TimeDomain)
                : input.Delta(CameraTimeDomain.PresentationScaled);
        }

        public bool IsExpired(CameraEffectRuntimeState active)
        {
            return m_Projection.TryGetShot(active.Request.ResourceId, out CameraShotPayload payload) &&
                   payload.Duration >= 0f && active.Elapsed >= payload.Duration;
        }

        public float RetireDuration(CameraEffectRuntimeState active)
        {
            return m_Projection.TryGetShot(active.Request.ResourceId, out CameraShotPayload payload)
                ? Mathf.Max(0.016f, payload.BlendOut.Duration)
                : 0.016f;
        }

        static bool FindTarget(
            IReadOnlyList<CameraTargetSnapshot> targets,
            string key,
            out CameraTargetSnapshot target)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                CameraTargetSnapshot candidate = targets[i];
                if (candidate.Valid && string.Equals(candidate.Key, key, StringComparison.Ordinal))
                {
                    target = candidate;
                    return true;
                }
            }
            target = default;
            return false;
        }
    }
}
