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
        public bool UpdatesBySource => false;

        public bool HasResource(string resourceId) =>
            m_Projection.TryGetStretch(resourceId, out _);

        public CameraFramePlan Apply(
            CameraFramePlan plan,
            IReadOnlyList<CameraEffectRuntimeState> active,
            in CameraFrameInput input)
        {
            CameraEffectRuntimeState selected = CameraEffectRuntimeStateStore.Select(active, Kind);
            if (selected == null)
                return plan;
            CameraFramePlan result = plan;
            for (int i = 0; i < active.Count; i++)
            {
                CameraEffectRuntimeState state = active[i];
                if (state.Request.Kind != Kind ||
                    !m_Projection.TryGetStretch(state.Request.ResourceId, out CameraStretchPayload payload) ||
                    state != selected && payload.PlayStackingType != CameraEffectStackingType.Add)
                    continue;
                result = ApplySingle(result, state, payload, in input);
            }
            return result;
        }

        static CameraFramePlan ApplySingle(
            CameraFramePlan plan,
            CameraEffectRuntimeState state,
            CameraStretchPayload payload,
            in CameraFrameInput input)
        {
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
            if (payload.ApplyRuntimeCamFollowYOffset)
                offset += input.BodyRotation * Vector3.up *
                    (payload.RuntimeCamFollowYOffsetRatio * plan.Radius * envelope);
            if (payload.ApplyAimPointsCameraFollowYOffset)
                offset += Vector3.up * (payload.RuntimeCamFollowYPoints * envelope);
            Vector3 euler = plan.Rotation.eulerAngles;
            float pitch = NormalizeAngle(euler.x);
            bool useEndAngle = payload.IsAppliedEndElevationAngle && envelope > 0.5f;
            float angleMin = useEndAngle ? payload.EndElevationAngleMin : payload.ElevationAngleMin;
            float angleMax = useEndAngle ? payload.EndElevationAngleMax : payload.ElevationAngleMax;
            bool useAbsoluteAngle = useEndAngle
                ? payload.IsEndElevationAngleAbsolute
                : payload.IsElevationAngleAbsolute;
            if (payload.IsAppliedElevationRatio)
            {
                float targetPitch = Mathf.LerpUnclamped(angleMin, angleMax, 0.5f);
                pitch = useAbsoluteAngle
                    ? Mathf.LerpUnclamped(pitch, targetPitch, envelope)
                    : pitch + targetPitch * envelope;
            }
            Quaternion rotation = Quaternion.Euler(pitch, euler.y, euler.z + payload.RotationZ * envelope);
            return plan.WithWorldBasicData(
                plan.WorldBasicData
                    .WithPivotLocation(plan.PivotLocation + offset)
                    .WithRotation(rotation)
                    .WithRadius(plan.Radius * radiusScale));
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
                    return plan.Rotation * payload.CamOffset;
                default:
                    throw new InvalidOperationException(
                        $"Camera Stretch '{payload.StretchId}' has no supported offset space '{payload.CamOffsetSpace}'.");
            }
        }

        static float NormalizeAngle(float angle) => Mathf.Repeat(angle + 180f, 360f) - 180f;

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
