using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal sealed class CameraStretchEffectEvaluator : ICameraEffectOwner
    {
        readonly CharacterCameraProjectionPayload m_Projection;

        public CameraStretchEffectEvaluator(CharacterCameraProjectionPayload projection)
        {
            m_Projection = projection;
        }

        public CameraEffectKind Kind => CameraEffectKind.Stretch;
        public CameraEffectStage Stage => CameraEffectStage.Stretch;
        public bool UpdatesBySource => true;

        public bool HasResource(string resourceId) =>
            m_Projection.TryGetStretch(resourceId, out _);

        public CameraFramePlan Apply(
            CameraFramePlan plan,
            IReadOnlyList<CameraEffectRuntimeState> active,
            in CameraFrameInput input)
        {
            CameraEffectRuntimeState state = CameraEffectRuntimeStateStore.Select(active, Kind);
            if (state == null || !m_Projection.TryGetStretch(state.Request.ResourceId, out CameraStretchPayload payload))
                return plan;
            float progress = CameraEffectEvaluationMath.EffectProgress(
                state.Elapsed,
                payload.DelayTime,
                payload.StretchTime,
                payload.HoldTime);
            float start = payload.StartCurve.Evaluate(progress);
            float end = payload.EndCurve.Evaluate(progress);
            float envelope = Mathf.LerpUnclamped(start, end, progress) * state.Request.Weight;
            envelope *= CameraEffectEvaluationMath.ReleaseWeight(state, Mathf.Max(0.016f, payload.RecoilTime));
            float radiusScale = plan.RadiusScale + payload.RadiusRatio * envelope;
            Vector3 offset = payload.CamOffset * envelope;
            float pitch = plan.OrbitPitch;
            if (payload.IsElevationAngleAbsolute)
                pitch = Mathf.LerpUnclamped(pitch, payload.ElevationAngleMax, envelope);
            else
                pitch += Mathf.LerpUnclamped(payload.ElevationAngleMin, payload.ElevationAngleMax, envelope);
            return plan
                .WithRadiusScale(radiusScale)
                .WithCameraOffset(offset)
                .WithOrbit(plan.OrbitYaw, pitch, plan.OrbitRadius);
        }

        public float ResolveDelta(CameraEffectRuntimeState active, in CameraFrameInput input)
        {
            CameraStretchPayload payload = m_Projection.TryGetStretch(
                active.Request.ResourceId,
                out CameraStretchPayload value)
                ? value
                : throw new InvalidOperationException(
                    $"Camera Stretch resource '{active.Request.ResourceId}' is not present in the Projection.");
            return CameraEffectEvaluationMath.ResolveDelta(
                payload.IgnoreWorldTimeScale,
                payload.IgnoreOwnerTimeScale,
                payload.IgnoreLocalAvatar,
                in input);
        }

        public bool IsExpired(CameraEffectRuntimeState active)
        {
            return m_Projection.TryGetStretch(active.Request.ResourceId, out CameraStretchPayload payload) &&
                   payload.HoldTime >= 0f &&
                   active.Elapsed >= payload.DelayTime + payload.StretchTime + payload.HoldTime + payload.RecoilTime;
        }

        public float RetireDuration(CameraEffectRuntimeState active)
        {
            return m_Projection.TryGetStretch(active.Request.ResourceId, out CameraStretchPayload payload)
                ? Mathf.Max(0.016f, payload.RecoilTime)
                : 0.016f;
        }
    }
}
