using System;
using System.Collections.Generic;

namespace ThirdPersonAnimation
{
    public static class LocomotionFootPhaseProfileValidator
    {
        public static void ValidateBinding(
            in LocomotionPhaseFootPhaseProfileBinding binding,
            string ownerName,
            RunLocomotionAnimationConfigValidationResult result)
        {
            if (result == null || !binding.IsEnabled)
                return;

            string name = string.IsNullOrWhiteSpace(ownerName) ? "FootPhaseProfile" : ownerName;
            if (string.IsNullOrWhiteSpace(binding.AliasKey))
                result.AddError($"{name} alias key is missing.");

            LocomotionFootPhaseProfileSO profile = binding.Profile;
            if (profile == null)
            {
                result.AddError($"{name} profile is missing.");
                return;
            }

            if (profile.Phase != binding.Phase)
                result.AddError($"{name} profile phase does not match binding phase.");
            if (profile.Gait != binding.Gait)
                result.AddError($"{name} profile gait does not match binding gait.");
            if (!string.Equals(profile.AliasKey, binding.AliasKey, StringComparison.Ordinal))
                result.AddError($"{name} profile alias does not match binding alias.");

            ValidateProfile(profile, name, result);
        }

        public static void ValidateProfile(
            LocomotionFootPhaseProfileSO profile,
            string ownerName,
            RunLocomotionAnimationConfigValidationResult result)
        {
            if (result == null || profile == null)
                return;

            string name = string.IsNullOrWhiteSpace(ownerName) ? profile.name : ownerName;
            if (!profile.EnablePhaseMatching)
                return;

            if (string.IsNullOrWhiteSpace(profile.AliasKey))
                result.AddError($"{name} profile alias key is missing.");

            LocomotionFootPhaseMarker[] markers = profile.Markers;
            if (markers.Length == 0)
            {
                result.AddError($"{name} foot phase markers are missing.");
                return;
            }

            float previousTime = float.NegativeInfinity;
            HashSet<LocomotionFootPhase> seenPhases = new HashSet<LocomotionFootPhase>();
            for (int i = 0; i < markers.Length; i++)
            {
                LocomotionFootPhaseMarker marker = markers[i];
                if (!marker.HasKnownPhase)
                    result.AddError($"{name} marker[{i}] phase is unknown.");
                if (!marker.HasValidTime)
                    result.AddError($"{name} marker[{i}] normalized time is invalid.");
                if (marker.HasValidTime && marker.NormalizedTime < previousTime)
                    result.AddError($"{name} marker[{i}] normalized time must be sorted ascending.");
                if (marker.HasKnownPhase && !seenPhases.Add(marker.Phase))
                    result.AddError($"{name} marker[{i}] phase is duplicated.");
                if (marker.HasValidTime)
                    previousTime = marker.NormalizedTime;
            }
        }
    }
}
