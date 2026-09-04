using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal sealed class CameraZoomEffectEvaluator : ICameraEffectOwner
    {
        readonly CharacterCameraProjectionPayload m_Projection;

        public CameraZoomEffectEvaluator(CharacterCameraProjectionPayload projection)
        {
            m_Projection = projection;
        }

        public CameraEffectKind Kind => CameraEffectKind.Zoom;
        public CameraEffectStage Stage => CameraEffectStage.Zoom;
        public bool UpdatesBySource => true;

        public bool HasResource(string resourceId) =>
            m_Projection.TryGetZoom(resourceId, out _);

        public CameraFramePlan Apply(
            CameraFramePlan plan,
            IReadOnlyList<CameraEffectRuntimeState> active,
            in CameraFrameInput input)
        {
            CameraEffectRuntimeState state = CameraEffectRuntimeStateStore.Select(active, Kind);
            if (state == null || !m_Projection.TryGetZoom(state.Request.ResourceId, out CameraZoomPayload payload))
                return plan;
            float progress = CameraEffectEvaluationMath.EffectProgress(
                state.Elapsed,
                payload.DelayTime,
                payload.StartTime,
                payload.EndTime);
            if (progress <= 0f && state.Elapsed < payload.DelayTime + payload.StartTime)
                return plan;
            float start = payload.StartCurve.Evaluate(progress);
            float end = payload.EndCurve.Evaluate(progress);
            float envelope = Mathf.Clamp01(Mathf.LerpUnclamped(start, end, progress));
            float weight = state.Request.Weight * envelope * CameraEffectEvaluationMath.ReleaseWeight(
                state,
                Mathf.Max(0.016f, payload.EndTime - payload.StartTime));
            float fov = payload.FovVariationType switch
            {
                CameraFovVariationType.Additive => plan.FieldOfView + payload.FieldOfView * weight,
                CameraFovVariationType.Multiplicative => plan.FieldOfView * Mathf.LerpUnclamped(
                    1f,
                    payload.FieldOfView,
                    weight),
                _ => Mathf.LerpUnclamped(plan.FieldOfView, payload.FieldOfView, weight)
            };
            return plan.WithFieldOfView(fov);
        }

        public float ResolveDelta(CameraEffectRuntimeState active, in CameraFrameInput input)
        {
            CameraZoomPayload payload = m_Projection.TryGetZoom(
                active.Request.ResourceId,
                out CameraZoomPayload value)
                ? value
                : throw new InvalidOperationException(
                    $"Camera Zoom resource '{active.Request.ResourceId}' is not present in the Projection.");
            return CameraEffectEvaluationMath.ResolveDelta(
                payload.IgnoreWorldTimeScale,
                payload.IgnoreOwnerTimeScale,
                payload.IgnoreLocalAvatar,
                in input);
        }

        public bool IsExpired(CameraEffectRuntimeState active)
        {
            return m_Projection.TryGetZoom(active.Request.ResourceId, out CameraZoomPayload payload) &&
                   payload.LastTime >= 0f && active.Elapsed >= payload.LastTime;
        }

        public float RetireDuration(CameraEffectRuntimeState active)
        {
            return m_Projection.TryGetZoom(active.Request.ResourceId, out CameraZoomPayload payload)
                ? Mathf.Max(0.016f, payload.EndTime - payload.StartTime)
                : 0.016f;
        }
    }
}
