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
            float envelope = state.Retired
                ? CameraEffectEvaluationMath.ResolveRetiredWeight(
                    state,
                    payload.DelayTime,
                    payload.StretchTime,
                    payload.HoldTime,
                    payload.RecoilTime,
                    payload.StartCurve,
                    payload.EndCurve)
                : CameraEffectEvaluationMath.ResolvePhaseWeight(
                    state.Elapsed,
                    payload.DelayTime,
                    payload.StretchTime,
                    payload.HoldTime,
                    payload.RecoilTime,
                    payload.StartCurve,
                    payload.EndCurve);
            envelope *= state.Request.Weight;
            float radiusScale = 1f + payload.RadiusRatio * envelope;
            Vector3 offset = ResolveWorldOffset(payload, plan, in input) * envelope;
            float pitch = plan.OrbitPitch;
            if (payload.IsElevationAngleAbsolute)
                pitch = Mathf.LerpUnclamped(pitch, payload.ElevationAngleMax, envelope);
            else
                pitch += Mathf.LerpUnclamped(payload.ElevationAngleMin, payload.ElevationAngleMax, envelope);
            return plan
                .WithOrbit(plan.OrbitYaw, pitch, plan.OrbitRadius)
                .WithRadiusScale(radiusScale)
                .WithCameraOffset(offset);
        }

        static Vector3 ResolveWorldOffset(
            CameraStretchPayload payload,
            CameraFramePlan plan,
            in CameraFrameInput input)
        {
            switch (payload.CamOffsetSpace)
            {
                case CameraSpace.World:
                    return payload.CamOffset;
                case CameraSpace.LocalAvatar:
                    return input.BodyRotation * payload.CamOffset;
                case CameraSpace.Camera:
                    return Quaternion.Euler(plan.OrbitPitch, plan.OrbitYaw, plan.RollDegrees) * payload.CamOffset;
                default:
                    throw new InvalidOperationException(
                        $"Camera Stretch '{payload.StretchId}' has no supported offset space '{payload.CamOffsetSpace}'.");
            }
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
                ? payload.RecoilTime
                : 0f;
        }
    }
}
