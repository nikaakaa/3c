using System;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAnimation
{
    public static class LocomotionFootPhaseSampler
    {
        public static LocomotionFootPhaseSample Sample(
            LocomotionFootPhaseProfileSO profile,
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            float normalizedTime,
            int sourceStep = 0)
        {
            if (profile == null ||
                !profile.EnablePhaseMatching ||
                profile.Phase != phase ||
                profile.Gait != gait ||
                !string.Equals(profile.AliasKey, aliasKey ?? string.Empty, StringComparison.Ordinal))
            {
                return LocomotionFootPhaseSample.Invalid(phase);
            }

            LocomotionFootPhaseMarker[] markers = profile.Markers;
            if (markers.Length == 0)
                return LocomotionFootPhaseSample.Invalid(phase);

            float time = profile.Loop ? Mathf.Repeat(normalizedTime, 1f) : Mathf.Clamp01(normalizedTime);
            int selectedIndex = -1;
            float selectedTime = float.NegativeInfinity;
            for (int i = 0; i < markers.Length; i++)
            {
                LocomotionFootPhaseMarker marker = markers[i];
                if (!marker.HasKnownPhase || !marker.HasValidTime)
                    continue;

                if (marker.NormalizedTime <= time && marker.NormalizedTime >= selectedTime)
                {
                    selectedIndex = i;
                    selectedTime = marker.NormalizedTime;
                }
            }

            if (selectedIndex < 0 && profile.Loop)
                selectedIndex = FindLatestValidMarker(markers);
            if (selectedIndex < 0)
                selectedIndex = FindEarliestValidMarker(markers);
            if (selectedIndex < 0)
                return LocomotionFootPhaseSample.Invalid(phase);

            LocomotionFootPhaseMarker selected = markers[selectedIndex];
            return new LocomotionFootPhaseSample(
                true,
                phase,
                gait,
                aliasKey,
                time,
                selected.Phase,
                sourceStep);
        }

        static int FindLatestValidMarker(LocomotionFootPhaseMarker[] markers)
        {
            int selectedIndex = -1;
            float selectedTime = float.NegativeInfinity;
            for (int i = 0; i < markers.Length; i++)
            {
                LocomotionFootPhaseMarker marker = markers[i];
                if (marker.HasKnownPhase && marker.HasValidTime && marker.NormalizedTime >= selectedTime)
                {
                    selectedIndex = i;
                    selectedTime = marker.NormalizedTime;
                }
            }

            return selectedIndex;
        }

        static int FindEarliestValidMarker(LocomotionFootPhaseMarker[] markers)
        {
            int selectedIndex = -1;
            float selectedTime = float.PositiveInfinity;
            for (int i = 0; i < markers.Length; i++)
            {
                LocomotionFootPhaseMarker marker = markers[i];
                if (marker.HasKnownPhase && marker.HasValidTime && marker.NormalizedTime <= selectedTime)
                {
                    selectedIndex = i;
                    selectedTime = marker.NormalizedTime;
                }
            }

            return selectedIndex;
        }
    }
}

