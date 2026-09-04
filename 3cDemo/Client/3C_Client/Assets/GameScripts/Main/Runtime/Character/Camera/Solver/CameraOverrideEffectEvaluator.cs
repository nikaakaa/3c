using System;
using System.Collections.Generic;

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
            for (int i = 0; i < active.Count; i++)
                if (active[i].Request.Kind == Kind)
                    throw new InvalidOperationException(
                        "Camera Override evaluation is unavailable before its ZZZ track and tag consumer semantics are closed.");
            return plan;
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

    }
}
