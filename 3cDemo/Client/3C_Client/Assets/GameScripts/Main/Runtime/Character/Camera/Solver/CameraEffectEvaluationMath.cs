using System;
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
            float delta = ignoreWorldTimeScale
                ? input.UnscaledDeltaSeconds
                : input.PresentationDeltaSeconds;
            if (!ignoreOwnerTimeScale)
            {
                if (!input.HasOwnerTimeScale)
                    throw new InvalidOperationException("Camera effect requires an OwnerTimeScale input.");
                delta *= input.OwnerTimeScale;
            }
            if (!ignoreLocalAvatar)
            {
                if (!input.HasLocalAvatarTimeScale)
                    throw new InvalidOperationException("Camera effect requires a LocalAvatarTimeScale input.");
                delta *= input.LocalAvatarTimeScale;
            }
            return delta;
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
