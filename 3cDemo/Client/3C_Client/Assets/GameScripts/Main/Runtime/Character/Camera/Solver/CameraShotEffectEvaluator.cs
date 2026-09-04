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
        public bool UpdatesBySource => true;

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
            float progress = payload.Duration < 0f
                ? 1f
                : Mathf.Clamp01(state.Elapsed / Mathf.Max(0.0001f, payload.Duration));
            float envelope = Mathf.Clamp01(state.Request.Weight) * progress;
            envelope *= CameraEffectEvaluationMath.ReleaseWeight(state, payload.BlendOut.Duration);
            return plan
                .WithTargets(
                    plan.FollowPoint + payload.FollowOffset * envelope,
                    plan.AimPoint + payload.LookAtOffset * envelope)
                .WithFieldOfView(Mathf.LerpUnclamped(plan.FieldOfView, payload.FieldOfView, envelope));
        }

        public float ResolveDelta(CameraEffectRuntimeState active, in CameraFrameInput input)
        {
            CameraShotPayload payload = m_Projection.TryGetShot(
                active.Request.ResourceId,
                out CameraShotPayload value)
                ? value
                : throw new InvalidOperationException(
                    $"Camera Shot resource '{active.Request.ResourceId}' is not present in the Projection.");
            return input.Delta(payload.TimeDomain);
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
    }
}
