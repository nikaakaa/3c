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
            float elapsed = state.Retired ? state.RetireElapsed : state.Elapsed;
            float weight = Mathf.Clamp01(state.Request.Weight) * EvaluateEnvelope(
                elapsed,
                payload.BlendInSeconds,
                payload.BlendOutSeconds,
                payload.BlendInCurve,
                payload.BlendOutCurve,
                state.Retired);
            Vector3 follow = plan.FollowPoint + payload.Settings.FollowOffset * weight;
            Vector3 aim = plan.AimPoint + payload.Settings.AimOffset * weight;
            return plan
                .WithTargets(follow, aim)
                .WithFieldOfView(Mathf.LerpUnclamped(plan.FieldOfView, payload.Settings.FieldOfView, weight));
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

        static float EvaluateEnvelope(
            float elapsed,
            float blendInSeconds,
            float blendOutSeconds,
            CameraCurvePayload blendInCurve,
            CameraCurvePayload blendOutCurve,
            bool retired)
        {
            if (retired)
            {
                if (blendOutSeconds <= 0f)
                    return 0f;
                return 1f - Mathf.Clamp01(blendOutCurve.Evaluate(
                    Mathf.Clamp01(elapsed / blendOutSeconds)));
            }
            if (blendInSeconds <= 0f)
                return 1f;
            return Mathf.Clamp01(blendInCurve.Evaluate(
                Mathf.Clamp01(elapsed / blendInSeconds)));
        }
    }
}
