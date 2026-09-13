using System;
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
            CameraFramePlan result = plan;
            CameraEffectRuntimeState selected = CameraEffectRuntimeStateStore.Select(active, Kind);
            for (int i = 0; i < active.Count; i++)
            {
                CameraEffectRuntimeState state = active[i];
                if (state.Request.Kind != Kind || !m_Projection.TryGetShake(state.Request.ResourceId, out CameraShakePayload payload))
                    continue;
                if (payload.PlayStackingType != CameraEffectStackingType.Add && state != selected)
                    continue;
                float envelope = ResolveEnvelope(state, payload);
                if (envelope <= 0f)
                    continue;
                float normalizedTime = Mathf.Clamp01(state.Elapsed / payload.ShakeTotalTime);
                float weight = state.Request.Weight * envelope *
                    Mathf.Clamp01(payload.Curve.Evaluate(normalizedTime));
                if (payload.DissipationDistance > 0f)
                    weight *= Mathf.Clamp01(1f - Mathf.Max(0f, payload.DistanceToPlane) / payload.DissipationDistance);
                if (weight <= 0f)
                    continue;
                float phase = ResolvePhase(state.Request);
                float time = state.Elapsed * payload.Frequency * Mathf.PI * 2f + phase;
                float noise = Mathf.Sin(time * Mathf.Max(0f, payload.NoiseRatio));
                float pitch = (Mathf.Sin(time + payload.NoiseAngle) * payload.PitchAmplitude + payload.AngleVertical) * weight;
                float yaw = Mathf.Cos(time * 1.07f + payload.NoiseAngle) * payload.YawAmplitude * weight;
                float roll = Mathf.Sin(time * 1.13f + payload.NoiseAngle) * payload.RollAmplitude * weight;
                Vector3 localPosition = new Vector3(
                    Mathf.Sin(time) * payload.RadiusLength * weight,
                    noise * payload.RadiusLength * weight,
                    Mathf.Cos(time) * payload.RadiusLength * weight);
                Vector3 worldPosition;
                Quaternion rotationDelta;
                switch (payload.ShakeCenterSpace)
                {
                    case CameraSpace.World:
                        worldPosition = localPosition;
                        rotationDelta = Quaternion.Euler(pitch, yaw, roll);
                        break;
                    case CameraSpace.LocalAvatar:
                        worldPosition = input.BodyRotation * localPosition;
                        rotationDelta = Quaternion.Euler(pitch, yaw, roll);
                        break;
                    case CameraSpace.Camera:
                        worldPosition = result.Rotation * localPosition;
                        rotationDelta = Quaternion.Euler(pitch, yaw, roll);
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Camera Shake '{payload.ShakeId}' has no supported center space '{payload.ShakeCenterSpace}'.");
                }
                Quaternion rotation = payload.ShakeCenterSpace == CameraSpace.Camera
                    ? result.Rotation * rotationDelta
                    : rotationDelta * result.Rotation;
                result = result.WithWorldBasicData(
                    result.WorldBasicData
                        .WithPivotLocation(result.PivotLocation + worldPosition)
                        .WithRotation(rotation));
            }
            return result;
        }

        public float ResolveDelta(CameraEffectRuntimeState active, in CameraFrameInput input)
        {
            CameraShakePayload payload = m_Projection.TryGetShake(
                active.Request.ResourceId,
                out CameraShakePayload value)
                ? value
                : throw new InvalidOperationException(
                    $"Camera Shake resource '{active.Request.ResourceId}' is not present in the Projection.");
            return payload.IgnoreTimeScale || payload.RealtimeVibration
                ? input.UnscaledDeltaSeconds
                : input.PresentationDeltaSeconds;
        }

        public bool IsExpired(CameraEffectRuntimeState active)
        {
            return m_Projection.TryGetShake(active.Request.ResourceId, out CameraShakePayload payload) &&
                   active.Elapsed >= payload.ShakeTotalTime;
        }

        public float RetireDuration(CameraEffectRuntimeState active)
        {
            return m_Projection.TryGetShake(active.Request.ResourceId, out CameraShakePayload payload)
                ? Mathf.Max(0.016f, payload.FadeOutDuration)
                : 0.016f;
        }

        static float ResolveEnvelope(
            CameraEffectRuntimeState state,
            CameraShakePayload payload)
        {
            return state.Retired
                ? CameraEffectEvaluationMath.ResolveRetiredWeight(
                    state,
                    0f,
                    payload.FadeInDuration,
                    Mathf.Max(0f, payload.ShakeTotalTime - payload.FadeInDuration - payload.FadeOutDuration),
                    payload.FadeOutDuration,
                    payload.FadeInCurve,
                    payload.FadeOutCurve)
                : CameraEffectEvaluationMath.ResolvePhaseWeight(
                    state.Elapsed,
                    0f,
                    payload.FadeInDuration,
                    Mathf.Max(0f, payload.ShakeTotalTime - payload.FadeInDuration - payload.FadeOutDuration),
                    payload.FadeOutDuration,
                    payload.FadeInCurve,
                    payload.FadeOutCurve);
        }

        static float ResolvePhase(CameraEffectRequest request)
        {
            unchecked
            {
                uint hash = 2166136261u;
                string value = request.ResourceId + "|" + request.SourceId + "|" + request.EventId;
                for (int i = 0; i < value.Length; i++)
                    hash = (hash ^ value[i]) * 16777619u;
                return hash / (float)uint.MaxValue * Mathf.PI * 2f;
            }
        }
    }
}
