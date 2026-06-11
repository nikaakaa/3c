using System;
using UnityEngine;

namespace ThirdPersonAnimation
{
    public static class AnimationMotionProfileSampler
    {
        const float PlanarDeltaEpsilon = 0.000001f;
        const float YawDeltaEpsilon = 0.0001f;

        public static AnimationMotionProfileSample Sample(
            LocomotionMotionProfileSO profile,
            in AnimationMotionPlaybackWindow playbackWindow)
        {
            if (profile == null || !profile.IsValid || !playbackWindow.HasValidPlayback)
                return AnimationMotionProfileSample.None(playbackWindow.Phase);

            if (profile.Phase != playbackWindow.Phase ||
                profile.Gait != playbackWindow.Gait ||
                !string.Equals(profile.AliasKey, playbackWindow.AliasKey, StringComparison.Ordinal))
            {
                return AnimationMotionProfileSample.None(playbackWindow.Phase);
            }

            float current = Mathf.Clamp01(playbackWindow.CurrentNormalizedTime);
            float previous = Mathf.Clamp01(playbackWindow.PreviousNormalizedTime);

            if (current < previous)
                previous = current;

            Vector3 localDelta = profile.EvaluateCumulativeLocalPlanarDelta(current) -
                                 profile.EvaluateCumulativeLocalPlanarDelta(previous);
            float yawDelta = profile.EvaluateCumulativeYaw(current) - profile.EvaluateCumulativeYaw(previous);
            bool hasContribution = localDelta.sqrMagnitude > PlanarDeltaEpsilon || Mathf.Abs(yawDelta) > YawDeltaEpsilon;

            return new AnimationMotionProfileSample(
                hasContribution,
                localDelta,
                yawDelta,
                profile.Phase,
                profile.AliasKey);
        }
    }
}
