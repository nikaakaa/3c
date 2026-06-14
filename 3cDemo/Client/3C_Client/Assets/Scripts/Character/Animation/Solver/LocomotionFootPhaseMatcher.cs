using System;

namespace ThirdPersonAnimation
{
    public static class LocomotionFootPhaseMatcher
    {
        public static LocomotionFootPhaseMatchResult Match(
            in LocomotionFootPhaseMatchRequest request,
            LocomotionFootPhaseProfileSO targetProfile)
        {
            if (!request.IsValid)
                return LocomotionFootPhaseMatchResult.Invalid("request-invalid");
            if (targetProfile == null)
                return LocomotionFootPhaseMatchResult.Invalid("target-profile-missing");
            if (!targetProfile.EnablePhaseMatching)
                return LocomotionFootPhaseMatchResult.Invalid("target-profile-disabled");
            if (targetProfile.Phase != request.TargetPhase ||
                targetProfile.Gait != request.TargetGait ||
                !string.Equals(targetProfile.AliasKey, request.TargetAliasKey, StringComparison.Ordinal))
            {
                return LocomotionFootPhaseMatchResult.Invalid("target-profile-mismatch");
            }

            LocomotionFootPhase targetPhase = request.ExitFootPhase.FootPhase;
            LocomotionFootPhaseMarker[] markers = targetProfile.Markers;
            for (int i = 0; i < markers.Length; i++)
            {
                LocomotionFootPhaseMarker marker = markers[i];
                if (marker.Phase == targetPhase && marker.HasValidTime)
                {
                    return new LocomotionFootPhaseMatchResult(
                        true,
                        targetPhase,
                        marker.NormalizedTime,
                        targetProfile.AliasKey,
                        "matched");
                }
            }

            return LocomotionFootPhaseMatchResult.Invalid("target-marker-missing");
        }
    }
}

