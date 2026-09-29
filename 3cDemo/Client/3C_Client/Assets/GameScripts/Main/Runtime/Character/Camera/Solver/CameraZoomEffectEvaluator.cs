using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal sealed class CameraZoomEffectEvaluator : ICameraEffectOwner
    {
        readonly CharacterCameraProjectionPayload m_Projection;
        float m_BaseOffset;
        float m_AdditiveOffset;

        public CameraZoomEffectEvaluator(CharacterCameraProjectionPayload projection)
        {
            m_Projection = projection;
        }

        public CameraEffectKind Kind => CameraEffectKind.Zoom;
        public CameraEffectStage Stage => CameraEffectStage.Zoom;
        public bool UpdatesBySource => false;

        public bool HasResource(string resourceId) =>
            m_Projection.TryGetZoom(resourceId, out _);

        public void Reset()
        {
            m_BaseOffset = 0f;
            m_AdditiveOffset = 0f;
        }

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
                if (!state.ZoomInitialized)
                {
                    bool additive = payload.PlayStackingType == CameraEffectStackingType.Add;
                    float referenceFov = additive ? 0f : plan.FieldOfView;
                    state.ZoomStartOffset = payload.StackingType == CameraEffectStackingType.Replace
                        ? additive ? m_AdditiveOffset : m_BaseOffset
                        : 0f;
                    state.ZoomTargetOffset = payload.FovVariationType switch
                    {
                        CameraFovVariationType.Additive => state.ZoomStartOffset + payload.FieldOfView,
                        CameraFovVariationType.Multiplicative => plan.FieldOfView * (payload.FieldOfView - 1f),
                        _ => payload.FieldOfView - referenceFov
                    };
                    state.ZoomInitialized = true;
                }
                float offset = EvaluateOffset(state, payload) * state.Request.Weight;
                if (payload.PlayStackingType == CameraEffectStackingType.Add)
                {
                    if (Mathf.Abs(offset) > Mathf.Abs(additiveOffset))
                        additiveOffset = offset;
                }
                else if (Mathf.Abs(offset) > Mathf.Abs(baseOffset))
                    baseOffset = offset;
            }
            m_BaseOffset = baseOffset;
            m_AdditiveOffset = additiveOffset;
            return plan.WithFieldOfView(plan.FieldOfView + baseOffset + additiveOffset);
        }

        static float EvaluateOffset(
            CameraEffectRuntimeState state,
            CameraZoomPayload payload)
        {
            if (!state.Retired)
                return SampleOffset(state.Elapsed, state, payload);
            if (payload.EndTime == 0f)
                return 0f;
            float offset = SampleOffset(state.RetireStartElapsed, state, payload);
            return offset * Mathf.Clamp01(1f - payload.EndCurve.Evaluate(state.RetireElapsed / payload.EndTime));
        }

        static float SampleOffset(float elapsed, CameraEffectRuntimeState state, CameraZoomPayload payload)
        {
            float phaseElapsed = elapsed - payload.DelayTime;
            if (phaseElapsed < 0f)
                return 0f;
            if (payload.StartTime > 0f && phaseElapsed < payload.StartTime)
                return Mathf.LerpUnclamped(state.ZoomStartOffset, state.ZoomTargetOffset,
                    Mathf.Clamp01(payload.StartCurve.Evaluate(phaseElapsed / payload.StartTime)));
            phaseElapsed -= payload.StartTime;
            if (payload.LastTime < 0f || phaseElapsed < payload.LastTime)
                return state.ZoomTargetOffset;
            phaseElapsed -= payload.LastTime;
            if (payload.EndTime == 0f)
                return 0f;
            return state.ZoomTargetOffset * Mathf.Clamp01(1f - payload.EndCurve.Evaluate(phaseElapsed / payload.EndTime));
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
