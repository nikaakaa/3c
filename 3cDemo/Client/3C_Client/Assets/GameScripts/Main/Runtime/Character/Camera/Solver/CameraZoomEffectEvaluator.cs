using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal sealed class CameraZoomEffectEvaluator : ICameraEffectOwner
    {
        readonly CharacterCameraProjectionPayload m_Projection;

        public CameraZoomEffectEvaluator(CharacterCameraProjectionPayload projection)
        {
            m_Projection = projection;
        }

        public CameraEffectKind Kind => CameraEffectKind.Zoom;
        public CameraEffectStage Stage => CameraEffectStage.Zoom;
        public bool UpdatesBySource => false;

        public bool HasResource(string resourceId) =>
            m_Projection.TryGetZoom(resourceId, out _);

        public CameraFramePlan Apply(
            CameraFramePlan plan,
            IReadOnlyList<CameraEffectRuntimeState> active,
            in CameraFrameInput input)
        {
            float baseOffset = 0f;
            float additiveOffset = 0f;
            for (int i = 0; i < active.Count; i++)
            {
                CameraEffectRuntimeState state = active[i];
                if (state.Request.Kind != Kind)
                    continue;
                m_Projection.TryGetZoom(state.Request.ResourceId, out CameraZoomPayload payload);
                float offset = EvaluateOffset(plan.FieldOfView, state, payload);
                if (payload.PlayStackingType == CameraEffectStackingType.Add)
                {
                    if (Mathf.Abs(offset) > Mathf.Abs(additiveOffset))
                        additiveOffset = offset;
                }
                else if (Mathf.Abs(offset) > Mathf.Abs(baseOffset))
                    baseOffset = offset;
            }
            return plan.WithFieldOfView(plan.FieldOfView + baseOffset + additiveOffset);
        }

        static float EvaluateOffset(
            float baseFieldOfView,
            CameraEffectRuntimeState state,
            CameraZoomPayload payload)
        {
            float envelope = state.Retired
                ? CameraEffectEvaluationMath.ResolveRetiredWeight(
                    state,
                    payload.DelayTime,
                    payload.StartTime,
                    payload.LastTime,
                    payload.EndTime,
                    payload.StartCurve,
                    payload.EndCurve)
                : CameraEffectEvaluationMath.ResolvePhaseWeight(
                    state.Elapsed,
                    payload.DelayTime,
                    payload.StartTime,
                    payload.LastTime,
                    payload.EndTime,
                    payload.StartCurve,
                    payload.EndCurve);
            float weight = state.Request.Weight * envelope;
            float referenceFov = payload.PlayStackingType == CameraEffectStackingType.Add ? 0f : baseFieldOfView;
            return payload.FovVariationType switch
            {
                CameraFovVariationType.Additive => payload.FieldOfView * weight,
                CameraFovVariationType.Multiplicative => baseFieldOfView * (payload.FieldOfView - 1f) * weight,
                _ => (payload.FieldOfView - referenceFov) * weight
            };
        }

        public float ResolveDelta(CameraEffectRuntimeState active, in CameraFrameInput input)
        {
            m_Projection.TryGetZoom(active.Request.ResourceId, out CameraZoomPayload payload);
            return CameraEffectEvaluationMath.ResolveDelta(
                payload.IgnoreWorldTimeScale,
                payload.IgnoreOwnerTimeScale,
                payload.IgnoreLocalAvatar,
                in input);
        }

        public bool IsExpired(CameraEffectRuntimeState active)
        {
            m_Projection.TryGetZoom(active.Request.ResourceId, out CameraZoomPayload payload);
            return payload.LastTime >= 0f &&
                   active.Elapsed >= payload.DelayTime + payload.StartTime + payload.LastTime + payload.EndTime;
        }

        public float RetireDuration(CameraEffectRuntimeState active)
        {
            m_Projection.TryGetZoom(active.Request.ResourceId, out CameraZoomPayload payload);
            return payload.EndTime;
        }
    }
}
