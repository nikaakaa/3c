using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal sealed class CameraStretchEffectEvaluator : ICameraEffectOwner
    {
        readonly CharacterCameraProjectionPayload m_Projection;
        StretchContribution m_BaseContribution;
        StretchContribution m_AdditiveContribution;

        public CameraStretchEffectEvaluator(CharacterCameraProjectionPayload projection)
        {
            m_Projection = projection;
        }

        public CameraEffectKind Kind => CameraEffectKind.Stretch;
        public CameraEffectStage Stage => CameraEffectStage.Stretch;
        public bool UpdatesBySource => false;

        public bool HasResource(string resourceId) =>
            m_Projection.TryGetStretch(resourceId, out _);

        public void Reset()
        {
            m_BaseContribution = default;
            m_AdditiveContribution = default;
        }

        public CameraFramePlan Apply(
            CameraFramePlan plan,
            IReadOnlyList<CameraEffectRuntimeState> active,
            in CameraFrameInput input)
        {
            CameraEffectRuntimeState elevationOwner = CameraEffectRuntimeStateStore.Select(active, Kind);
            if (elevationOwner == null)
            {
                Reset();
                return plan;
            }
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
                if (!state.StretchInitialized)
                {
                    StretchContribution previous = payload.PlayStackingType == CameraEffectStackingType.Add
                        ? m_AdditiveContribution : m_BaseContribution;
                    if (payload.StackingType == 0)
                    {
                        state.StretchStartRadiusOffset = previous.RadiusOffset;
                        state.StretchStartPositionOffset = previous.RawOffset;
                        state.StretchStartRollOffset = previous.RollOffset;
                    }
                    state.StretchInitialized = true;
                }
                StretchContribution sample = Sample(state, payload);
                float envelope = sample.Envelope * state.Request.Weight;
                Vector3 rawOffset = sample.RawOffset * state.Request.Weight;
                Vector3 offset = ResolveWorldOffset(rawOffset, payload, plan, in input);
                if (payload.ApplyRuntimeCamFollowYOffset)
                    offset += input.BodyRotation * Vector3.up *
                        (payload.RuntimeCamFollowYOffsetRatio * plan.Radius * envelope);
                if (payload.ApplyAimPointsCameraFollowYOffset)
                    offset += Vector3.up * (payload.RuntimeCamFollowYPoints * envelope);
                float radiusOffset = sample.RadiusOffset * state.Request.Weight;
                float rollOffset = sample.RollOffset * state.Request.Weight;
                if (payload.PlayStackingType == CameraEffectStackingType.Add)
                    additiveContribution.Select(radiusOffset, rawOffset, offset, rollOffset);
                else
                    baseContribution.Select(radiusOffset, rawOffset, offset, rollOffset);
                if (state == elevationOwner || payload.PlayStackingType == CameraEffectStackingType.Add)
                    pitch = ResolvePitch(pitch, payload, envelope);
            }
            m_BaseContribution = baseContribution;
            m_AdditiveContribution = additiveContribution;
            Quaternion rotation = Quaternion.Euler(
                pitch, euler.y, euler.z + baseContribution.RollOffset + additiveContribution.RollOffset);
            return plan.WithWorldBasicData(
                plan.WorldBasicData
                    .WithPivotLocation(plan.PivotLocation + baseContribution.Offset + additiveContribution.Offset)
                    .WithRotation(rotation)
                    .WithRadius(plan.Radius * (1f + baseContribution.RadiusOffset + additiveContribution.RadiusOffset)));
        }

        static StretchContribution Sample(
            CameraEffectRuntimeState state,
            CameraStretchPayload payload)
        {
            if (!state.Retired)
                return SampleAt(state.Elapsed, state, payload);
            if (payload.RecoilTime == 0f)
                return default;
            StretchContribution sample = SampleAt(state.RetireStartElapsed, state, payload);
            sample.Scale(1f - Mathf.Clamp01(payload.EndCurve.Evaluate(state.RetireElapsed / payload.RecoilTime)));
            return sample;
        }

        static StretchContribution SampleAt(float elapsed, CameraEffectRuntimeState state, CameraStretchPayload payload)
        {
            float phaseElapsed = elapsed - payload.DelayTime;
            if (phaseElapsed < 0f)
                return default;
            if (payload.StretchTime > 0f && phaseElapsed < payload.StretchTime)
            {
                float t = Mathf.Clamp01(payload.StartCurve.Evaluate(phaseElapsed / payload.StretchTime));
                return new StretchContribution
                {
                    Envelope = t,
                    RadiusOffset = Mathf.LerpUnclamped(state.StretchStartRadiusOffset, payload.RadiusRatio, t),
                    RawOffset = Vector3.LerpUnclamped(state.StretchStartPositionOffset, payload.CamOffset, t),
                    RollOffset = Mathf.LerpUnclamped(state.StretchStartRollOffset, payload.RotationZ, t)
                };
            }
            phaseElapsed -= payload.StretchTime;
            StretchContribution target = new StretchContribution
            {
                Envelope = 1f,
                RadiusOffset = payload.RadiusRatio,
                RawOffset = payload.CamOffset,
                RollOffset = payload.RotationZ
            };
            if (payload.HoldTime < 0f || phaseElapsed < payload.HoldTime)
                return target;
            phaseElapsed -= payload.HoldTime;
            if (payload.RecoilTime == 0f)
                return default;
            target.Scale(1f - Mathf.Clamp01(payload.EndCurve.Evaluate(phaseElapsed / payload.RecoilTime)));
            return target;
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
            public float Envelope;
            public float RadiusOffset;
            public Vector3 RawOffset;
            public Vector3 Offset;
            public float RollOffset;

            public void Scale(float weight)
            {
                Envelope *= weight;
                RadiusOffset *= weight;
                RawOffset *= weight;
                RollOffset *= weight;
            }

            public void Select(float radiusOffset, Vector3 rawOffset, Vector3 offset, float rollOffset)
            {
                if (Mathf.Abs(radiusOffset) > Mathf.Abs(RadiusOffset))
                    RadiusOffset = radiusOffset;
                if (offset.sqrMagnitude > Offset.sqrMagnitude)
                {
                    RawOffset = rawOffset;
                    Offset = offset;
                }
                if (Mathf.Abs(rollOffset) > Mathf.Abs(RollOffset))
                    RollOffset = rollOffset;
            }
        }

        static Vector3 ResolveWorldOffset(
            Vector3 offset,
            CameraStretchPayload payload,
            CameraFramePlan plan,
            in CameraFrameInput input)
        {
            switch (payload.CamOffsetSpace)
            {
                case CameraSpace.World:
                    return offset;
                case CameraSpace.LocalAvatar:
                    return input.BodyRotation * offset;
                case CameraSpace.Camera:
                    return plan.Rotation * offset;
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
