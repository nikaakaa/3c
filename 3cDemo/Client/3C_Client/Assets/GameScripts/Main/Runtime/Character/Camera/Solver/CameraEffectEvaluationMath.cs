using UnityEngine;

namespace ThirdPersonCamera
{
    internal static class CameraEffectEvaluationMath
    {
        public static float ResolveDelta(
            bool ignoreWorldTimeScale,
            bool ignoreOwnerTimeScale,
            bool ignoreLocalAvatar,
            in CameraFrameInput input)
        {
            if (ignoreWorldTimeScale || ignoreOwnerTimeScale || ignoreLocalAvatar)
                return input.UnscaledDeltaSeconds;
            return input.PresentationDeltaSeconds;
        }

        public static float EffectProgress(float elapsed, float delay, float start, float end)
        {
            float duration = Mathf.Max(0.0001f, end - start);
            return Mathf.Clamp01((elapsed - delay - start) / duration);
        }

        public static float ReleaseWeight(CameraEffectRuntimeState active, float duration) =>
            !active.Retired
                ? 1f
                : Mathf.Clamp01(1f - active.RetireElapsed / Mathf.Max(0.016f, duration));
    }
}
