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
            for (int i = 0; i < active.Count; i++)
                if (active[i].Request.Kind == Kind)
                    throw new InvalidOperationException(
                        "Camera Shake evaluation is unavailable before its ZZZ consumer semantics are closed.");
            return plan;
        }

        public float ResolveDelta(CameraEffectRuntimeState active, in CameraFrameInput input)
        {
            CameraShakePayload payload = m_Projection.TryGetShake(
                active.Request.ResourceId,
                out CameraShakePayload value)
                ? value
                : throw new InvalidOperationException(
                    $"Camera Shake resource '{active.Request.ResourceId}' is not present in the Projection.");
            return payload.IgnoreTimeScale
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
    }
}
