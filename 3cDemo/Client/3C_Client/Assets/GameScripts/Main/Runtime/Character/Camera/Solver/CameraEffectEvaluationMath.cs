using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal static class CameraEffectEvaluationMath
    {
        public static float ResolveDelta(
            bool ignoreWorldTimeScale,
            bool ignoreOwnerTimeScale,
            in CameraFrameInput input)
        {
            float delta = ignoreWorldTimeScale
                ? input.UnscaledDeltaSeconds
                : input.PresentationDeltaSeconds;
            if (!ignoreOwnerTimeScale)
            {
                if (!input.HasOwnerTimeScale)
                    throw new InvalidOperationException("Camera effect requires an OwnerTimeScale input.");
                delta *= input.OwnerTimeScale;
            }
            return delta;
        }

        public static float ResolvePhaseWeight(
            float elapsed,
            float delayTime,
            float enterTime,
            float holdTime,
            float exitTime,
            CameraCurvePayload enterCurve,
            CameraCurvePayload exitCurve)
        {
            if (elapsed < delayTime)
                return 0f;
            float phaseElapsed = elapsed - delayTime;
            if (enterTime > 0f && phaseElapsed < enterTime)
                return Mathf.Clamp01(enterCurve.Evaluate(phaseElapsed / enterTime));
            phaseElapsed -= enterTime;
            if (holdTime < 0f || phaseElapsed < holdTime)
                return 1f;
            phaseElapsed -= holdTime;
            if (exitTime <= 0f)
                return 0f;
            return Mathf.Clamp01(1f - exitCurve.Evaluate(phaseElapsed / exitTime));
        }

        public static float ResolveRetiredWeight(
            CameraEffectRuntimeState active,
            float delayTime,
            float enterTime,
            float holdTime,
            float exitTime,
            CameraCurvePayload enterCurve,
            CameraCurvePayload exitCurve)
        {
            if (exitTime <= 0f)
                return 0f;
            float startWeight = ResolvePhaseWeight(
                active.RetireStartElapsed,
                delayTime,
                enterTime,
                holdTime,
                exitTime,
                enterCurve,
                exitCurve);
            if (startWeight <= 0f)
                return 0f;
            float fade = Mathf.Clamp01(active.RetireElapsed / exitTime);
            return startWeight * Mathf.Clamp01(1f - exitCurve.Evaluate(fade));
        }
    }
}
