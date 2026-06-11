using System;

namespace ThirdPersonAnimation
{
    public static class LocomotionMotionProfileValidator
    {
        public static void ValidateBinding(
            in LocomotionPhaseMotionProfileBinding binding,
            string ownerName,
            RunLocomotionAnimationConfigValidationResult result)
        {
            if (result == null)
                return;

            string name = string.IsNullOrWhiteSpace(ownerName) ? "MotionProfile" : ownerName;

            if (binding.MotionMode == LocomotionMotionProfileMode.Disabled)
                return;

            if (string.IsNullOrWhiteSpace(binding.AliasKey))
                result.AddError($"{name} alias key is missing.");

            LocomotionMotionProfileSO profile = binding.Profile;
            if (profile == null)
            {
                result.AddError($"{name} profile is missing.");
                return;
            }

            if (profile.Phase != binding.Phase)
                result.AddError($"{name} profile phase does not match binding phase.");

            if (profile.Gait != binding.Gait)
                result.AddError($"{name} profile gait does not match binding gait.");

            if (!string.Equals(profile.AliasKey, binding.AliasKey ?? string.Empty, StringComparison.Ordinal))
                result.AddError($"{name} profile alias does not match binding alias.");

            ValidateProfile(profile, name, result);
        }

        public static void ValidateProfile(
            LocomotionMotionProfileSO profile,
            string ownerName,
            RunLocomotionAnimationConfigValidationResult result)
        {
            if (result == null || profile == null)
                return;

            string name = string.IsNullOrWhiteSpace(ownerName) ? profile.name : ownerName;

            if (string.IsNullOrWhiteSpace(profile.AliasKey))
                result.AddError($"{name} profile alias key is missing.");

            if (profile.Duration <= 0f)
                result.AddError($"{name} profile duration is invalid.");

            if (!profile.HasValidCurves)
                result.AddError($"{name} profile curves are missing.");
        }
    }
}
