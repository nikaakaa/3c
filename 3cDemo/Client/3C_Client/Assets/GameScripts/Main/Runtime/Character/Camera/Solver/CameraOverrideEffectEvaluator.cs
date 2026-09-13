using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal sealed class CameraOverrideEffectEvaluator : ICameraEffectOwner
    {
        readonly CharacterCameraProjectionPayload m_Projection;

        public CameraOverrideEffectEvaluator(CharacterCameraProjectionPayload projection)
        {
            m_Projection = projection;
        }

        public CameraEffectKind Kind => CameraEffectKind.Override;
        public CameraEffectStage Stage => CameraEffectStage.Override;
        public bool UpdatesBySource => true;

        public bool HasResource(string resourceId) =>
            m_Projection.TryGetOverride(resourceId, out _);

        public CameraFramePlan Apply(
            CameraFramePlan plan,
            IReadOnlyList<CameraEffectRuntimeState> active,
            in CameraFrameInput input)
        {
            CameraEffectRuntimeState state = CameraEffectRuntimeStateStore.Select(active, Kind);
            if (state == null || !m_Projection.TryGetOverride(state.Request.ResourceId, out CameraOverrideTrackPayload payload))
                return plan;
            float envelope = state.Retired
                ? CameraEffectEvaluationMath.ResolveRetiredWeight(
                    state,
                    0f,
                    payload.BlendInSeconds,
                    payload.Duration < 0f
                        ? -1f
                        : Mathf.Max(0f, payload.Duration - payload.BlendInSeconds - payload.BlendOutSeconds),
                    payload.BlendOutSeconds,
                    payload.BlendInCurve,
                    payload.BlendOutCurve)
                : CameraEffectEvaluationMath.ResolvePhaseWeight(
                    state.Elapsed,
                    0f,
                    payload.BlendInSeconds,
                    payload.Duration < 0f
                        ? -1f
                        : Mathf.Max(0f, payload.Duration - payload.BlendInSeconds - payload.BlendOutSeconds),
                    payload.BlendOutSeconds,
                    payload.BlendInCurve,
                    payload.BlendOutCurve);
            envelope *= state.Request.Weight;
            CameraOrbitPayload orbit = ResolveOrbit(payload.Settings, plan.Pitch);
            float pitch = Mathf.Atan2(orbit.Height, orbit.Radius) * Mathf.Rad2Deg;
            float yaw = plan.Rotation.eulerAngles.y;
            float roll = plan.Rotation.eulerAngles.z;
            Vector2 offset = ResolveOffset(payload.Settings, plan.Pitch);
            CameraWorldBasicData target = new CameraWorldBasicData(
                plan.PivotLocation + payload.Settings.FollowOffset + payload.Settings.AimOffset,
                Quaternion.Euler(pitch, yaw, roll),
                Mathf.Sqrt(orbit.Height * orbit.Height + orbit.Radius * orbit.Radius),
                offset,
                payload.Settings.FieldOfView);
            return plan.WithWorldBasicData(CameraWorldBasicData.Lerp(
                plan.WorldBasicData,
                target,
                envelope));
        }

        public float ResolveDelta(CameraEffectRuntimeState active, in CameraFrameInput input)
        {
            CameraOverrideTrackPayload payload = m_Projection.TryGetOverride(
                active.Request.ResourceId,
                out CameraOverrideTrackPayload value)
                ? value
                : throw new InvalidOperationException(
                    $"Camera Override resource '{active.Request.ResourceId}' is not present in the Projection.");
            return CameraEffectEvaluationMath.ResolveDelta(
                payload.IgnoreWorldTimeScale,
                payload.IgnoreOwnerTimeScale,
                payload.IgnoreLocalAvatar,
                in input);
        }

        public bool IsExpired(CameraEffectRuntimeState active)
        {
            return m_Projection.TryGetOverride(active.Request.ResourceId, out CameraOverrideTrackPayload payload) &&
                   payload.Duration >= 0f && active.Elapsed >= payload.Duration;
        }

        public float RetireDuration(CameraEffectRuntimeState active)
        {
            return m_Projection.TryGetOverride(active.Request.ResourceId, out CameraOverrideTrackPayload payload)
                ? payload.BlendOutSeconds
                : 0.016f;
        }

        static CameraOrbitPayload ResolveOrbit(
            CameraOverrideTrackSettingsPayload settings,
            float pitch)
        {
            if (settings.Orbits.Count == 0)
                return settings.TopOrbit;
            float ratio = Mathf.InverseLerp(-70f, 70f, pitch);
            int index = Mathf.Clamp(
                Mathf.RoundToInt(ratio * (settings.Orbits.Count - 1)),
                0,
                settings.Orbits.Count - 1);
            return settings.Orbits[index];
        }

        static Vector2 ResolveOffset(
            CameraOverrideTrackSettingsPayload settings,
            float pitch)
        {
            if (settings.ScreenY.Count == 0)
                return Vector2.zero;
            float ratio = Mathf.InverseLerp(-70f, 70f, pitch);
            int index = Mathf.Clamp(
                Mathf.RoundToInt(ratio * (settings.ScreenY.Count - 1)),
                0,
                settings.ScreenY.Count - 1);
            return new Vector2(0f, settings.ScreenY[index]);
        }

    }
}
