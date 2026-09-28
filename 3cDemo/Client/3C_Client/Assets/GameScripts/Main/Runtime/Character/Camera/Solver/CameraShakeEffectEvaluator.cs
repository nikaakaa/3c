using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal sealed class CameraShakeEffectEvaluator : ICameraEffectOwner
    {
        readonly CharacterCameraProjectionPayload m_Projection;

        public CameraShakeEffectEvaluator(CharacterCameraProjectionPayload projection)
        {
            m_Projection = projection;
        }

        public CameraEffectKind Kind => CameraEffectKind.Shake;
        public CameraEffectStage Stage => CameraEffectStage.Shake;
        public bool UpdatesBySource => false;

        public bool HasResource(string resourceId) =>
            m_Projection.TryGetShake(resourceId, out _);

        public CameraFramePlan Apply(
            CameraFramePlan plan,
            IReadOnlyList<CameraEffectRuntimeState> active,
            in CameraFrameInput input)
        {
            Vector3 basePosition = Vector3.zero;
            Vector3 baseRotation = Vector3.zero;
            Vector3 addedPosition = Vector3.zero;
            Vector3 addedRotation = Vector3.zero;
            CameraShakePayload selected = null;
            for (int i = 0; i < active.Count; i++)
            {
                CameraEffectRuntimeState state = active[i];
                if (state.Request.Kind != Kind)
                    continue;
                m_Projection.TryGetShake(state.Request.ResourceId, out CameraShakePayload payload);
                if (state.Retired && (payload.ShakeTotalTime >= 0f || payload.FadeOutDuration == 0f))
                    continue;
                if (state.ShakeSampleCount == 0)
                {
                    state.ShakeSourceForward = input.BodyRotation * Vector3.forward;
                    state.ShakeDistance = Vector3.Distance(plan.Location, input.BodyPosition);
                }
                else if (payload.RealtimeVibration)
                    state.ShakeDistance = Vector3.Distance(plan.Location, input.BodyPosition);

                float weight = state.Request.Weight * ResolveEnvelope(state, payload) *
                    ResolveDistanceWeight(payload, state.ShakeDistance);
                float angle = payload.AngleVertical;
                if (state.ShakeSampleCount != 0)
                    angle += 2f * (Mathf.PerlinNoise(state.Elapsed, 0f) - 0.5f) * payload.NoiseAngle;
                Vector3 direction = payload.ShakeType == 0
                    ? state.ShakeSourceForward
                    : payload.ShakeType == 1 ? plan.Rotation * Vector3.forward : Vector3.forward;
                direction.y = 0f;
                direction.Normalize();
                Vector3 radial = (Quaternion.AngleAxis(angle, direction) *
                    Vector3.Cross(Vector3.up, direction)).normalized;
                Vector3 position = (radial * payload.RadiusLength + direction * payload.DistanceToPlane) *
                    Signal(payload, state.Elapsed, 0f);
                if (payload.ShakeType == 2)
                    position = ScreenToWorldOffset(plan, input.PixelHeight, position);
                position *= weight;
                Vector3 rotation = new Vector3(
                    Signal(payload, state.Elapsed, 0.25f) * payload.PitchAmplitude,
                    Signal(payload, state.Elapsed, 0.5f) * payload.YawAmplitude,
                    Signal(payload, state.Elapsed, 0f) * payload.RollAmplitude) * weight;
                state.ShakeSampleCount++;

                if (payload.PlayStackingType == CameraEffectStackingType.Add)
                {
                    addedPosition += position;
                    addedRotation += rotation;
                }
                else if ((selected == null || payload.PlayPriority == selected.PlayPriority)
                    ? position.sqrMagnitude > basePosition.sqrMagnitude
                    : payload.PlayPriority > selected.PlayPriority)
                {
                    selected = payload;
                    basePosition = position;
                    baseRotation = rotation;
                }
            }
            Quaternion orientation = plan.Rotation * Quaternion.Euler(baseRotation + addedRotation);
            return plan.WithWorldBasicData(plan.WorldBasicData
                .WithRotation(orientation)
                .WithLocation(plan.Location + basePosition + addedPosition));
        }

        public float ResolveDelta(CameraEffectRuntimeState active, in CameraFrameInput input) =>
            input.ScaledDeltaSeconds;

        public bool IsExpired(CameraEffectRuntimeState active)
        {
            m_Projection.TryGetShake(active.Request.ResourceId, out CameraShakePayload payload);
            return payload.ShakeTotalTime >= 0f && active.Elapsed >= payload.ShakeTotalTime;
        }

        public float RetireDuration(CameraEffectRuntimeState active)
        {
            m_Projection.TryGetShake(active.Request.ResourceId, out CameraShakePayload payload);
            return payload.ShakeTotalTime < 0f ? payload.FadeOutDuration : 0f;
        }

        static float Signal(CameraShakePayload payload, float elapsed, float offset)
        {
            float phase = payload.Frequency * elapsed + offset;
            float value = 0.5f * Mathf.Cos(2f * Mathf.PI * phase);
            if (payload.NoiseRatio >= 0.0001f)
                value += 2f * (Mathf.PerlinNoise(phase, 0f) - 0.5f) * payload.NoiseRatio;
            return value;
        }

        static float ResolveEnvelope(CameraEffectRuntimeState state, CameraShakePayload payload)
        {
            if (payload.ShakeTotalTime > 0f)
                return state.Retired ? 0f : payload.Curve.Evaluate(
                    Mathf.Clamp01(state.Elapsed / payload.ShakeTotalTime));
            if (state.Retired)
                return payload.FadeOutDuration > 0f
                    ? state.ShakeEnvelope * payload.FadeOutCurve.Evaluate(
                        Mathf.Clamp01(state.RetireElapsed / payload.FadeOutDuration))
                    : 0f;
            state.ShakeEnvelope = state.Elapsed < payload.FadeInDuration
                ? payload.FadeInCurve.Evaluate(Mathf.Clamp01(state.Elapsed / payload.FadeInDuration))
                : 1f;
            return state.ShakeEnvelope;
        }

        static float ResolveDistanceWeight(CameraShakePayload payload, float distance)
        {
            if (payload.DissipationMode == 0)
                return 1f;
            if (payload.DissipationMode == 5)
                return payload.CustomCurve.Evaluate(distance);
            float outsideRadius = distance - Mathf.Max(0f, payload.ImpactRadius);
            if (outsideRadius < 0f)
                return 1f;
            if (outsideRadius >= payload.DissipationDistance)
                return 0f;
            switch (payload.DissipationMode)
            {
                case 1:
                    return Mathf.Min(1f, payload.ImpactRadius * payload.ImpactRadius /
                        (outsideRadius * outsideRadius));
                case 3:
                    return 0.5f * (1f + Mathf.Cos(Mathf.PI * outsideRadius / payload.DissipationDistance));
                case 4:
                    return 1f - Cinemachine.Utility.Damper.Damp(1f, payload.DissipationDistance, outsideRadius);
                default:
                    return 1f - Mathf.Clamp01(outsideRadius / payload.DissipationDistance);
            }
        }

        static Vector3 ScreenToWorldOffset(CameraFramePlan plan, int pixelHeight, Vector3 signal)
        {
            Vector3 aim = Quaternion.Inverse(plan.Rotation) * (plan.AimPoint - plan.Location);
            float depth = Mathf.Clamp(aim.z, plan.NearClipPlane, plan.FarClipPlane);
            float unitsPerPixel = 2f * depth * Mathf.Tan(plan.FieldOfView * Mathf.Deg2Rad * 0.5f) / pixelHeight;
            Vector3 point = new Vector3(
                aim.x * depth / aim.z + signal.x * unitsPerPixel,
                aim.y * depth / aim.z + signal.y * unitsPerPixel,
                depth + signal.z);
            return plan.Location + plan.Rotation * point - plan.AimPoint;
        }
    }
}
