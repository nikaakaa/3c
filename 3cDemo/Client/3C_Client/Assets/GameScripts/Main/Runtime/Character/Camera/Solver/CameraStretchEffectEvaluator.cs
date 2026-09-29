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
            CameraEffectRuntimeState elevationOwner = CameraEffectRuntimeStateStore.Select(active, Kind);
            if (elevationOwner == null)
                return plan;
            StretchContribution baseContribution = default;
            StretchContribution additiveContribution = default;
            Vector3 euler = plan.Rotation.eulerAngles;
            float pitch = NormalizeAngle(euler.x);
            for (int i = 0; i < active.Count; i++)
            {
                CameraEffectRuntimeState state = active[i];
                if (state.Request.Kind != Kind)
                    continue;
                m_Projection.TryGetStretch(state.Request.ResourceId, out CameraStretchPayload payload);
                float envelope = SampleEnvelope(state, payload) * state.Request.Weight;
                Vector3 offset = ResolveWorldOffset(payload, plan, in input) * envelope;
                if (payload.ApplyRuntimeCamFollowYOffset)
                    offset += input.BodyRotation * Vector3.up *
                        (payload.RuntimeCamFollowYOffsetRatio * plan.Radius * envelope);
                if (payload.ApplyAimPointsCameraFollowYOffset)
                    offset += Vector3.up * (payload.RuntimeCamFollowYPoints * envelope);
                float radiusOffset = payload.RadiusRatio * envelope;
                float rollOffset = payload.RotationZ * envelope;
                if (payload.PlayStackingType == CameraEffectStackingType.Add)
                    additiveContribution.Select(radiusOffset, offset, rollOffset);
                else
                    baseContribution.Select(radiusOffset, offset, rollOffset);
                if (state == elevationOwner || payload.PlayStackingType == CameraEffectStackingType.Add)
                    pitch = ResolvePitch(pitch, payload, envelope);
            }
            Quaternion rotation = Quaternion.Euler(
                pitch, euler.y, euler.z + baseContribution.RollOffset + additiveContribution.RollOffset);
            return plan.WithWorldBasicData(
                plan.WorldBasicData
                    .WithPivotLocation(plan.PivotLocation + baseContribution.Offset + additiveContribution.Offset)
                    .WithRotation(rotation)
                    .WithRadius(plan.Radius * (1f + baseContribution.RadiusOffset + additiveContribution.RadiusOffset)));
        }

        static float SampleEnvelope(
            CameraEffectRuntimeState state,
            CameraStretchPayload payload)
        {
            return state.Retired
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
        }

        static float ResolvePitch(float pitch, CameraStretchPayload payload, float envelope)
        {
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
            return pitch;
        }

        struct StretchContribution
        {
            public float RadiusOffset;
            public Vector3 Offset;
            public float RollOffset;

            public void Select(float radiusOffset, Vector3 offset, float rollOffset)
            {
                if (Mathf.Abs(radiusOffset) > Mathf.Abs(RadiusOffset))
                    RadiusOffset = radiusOffset;
                if (offset.sqrMagnitude > Offset.sqrMagnitude)
                    Offset = offset;
                if (Mathf.Abs(rollOffset) > Mathf.Abs(RollOffset))
                    RollOffset = rollOffset;
            }
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
            m_Projection.TryGetStretch(active.Request.ResourceId, out CameraStretchPayload payload);
            return CameraEffectEvaluationMath.ResolveDelta(
                payload.IgnoreWorldTimeScale,
                payload.IgnoreOwnerTimeScale,
                payload.IgnoreLocalAvatar,
                in input);
        }

        public bool IsExpired(CameraEffectRuntimeState active)
        {
            m_Projection.TryGetStretch(active.Request.ResourceId, out CameraStretchPayload payload);
            return payload.HoldTime >= 0f &&
                   active.Elapsed >= payload.DelayTime + payload.StretchTime + payload.HoldTime + payload.RecoilTime;
        }

        public float RetireDuration(CameraEffectRuntimeState active)
        {
            m_Projection.TryGetStretch(active.Request.ResourceId, out CameraStretchPayload payload);
            return payload.RecoilTime;
        }
    }
}
