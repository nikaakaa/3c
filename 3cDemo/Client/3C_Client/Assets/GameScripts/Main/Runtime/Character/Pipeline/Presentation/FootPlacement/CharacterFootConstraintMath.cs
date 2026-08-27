using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class CharacterFootConstraintMath
    {
        internal const float GeometryEpsilon = 0.0001f;

        internal static Vector3 ResolveOriginalSole(
            CharacterFootPlacementAnimatedFootPose foot) =>
            (foot.HeelPosition + foot.ToePosition) * 0.5f;

        internal static Vector3 ResolveSwingCorrection(
            CharacterFootPlacementAnimatedFootPose foot,
            in CharacterFootSwingMotionResult swing) =>
            swing.Accepted
                ? swing.CorrectedAnkle - foot.AnklePosition
                : default;

        internal static Vector3 ResolveContactCorrection(
            CharacterFootPlacementAnimatedFootPose foot,
            Vector3 contactAnchor) =>
            contactAnchor - ResolveOriginalSole(foot);

        internal static Vector3 ResolveSlidingCorrection(
            Vector3 fullCorrection,
            Vector3 componentUp,
            float horizontalError,
            CharacterFootMotionSettings settings)
        {
            Vector3 up = componentUp.normalized;
            Vector3 horizontal = Vector3.ProjectOnPlane(fullCorrection, up);
            float horizontalWeight = Mathf.InverseLerp(
                settings.SlideDistance,
                settings.LockDistance,
                horizontalError);
            return horizontal * horizontalWeight +
                   up * Vector3.Dot(fullCorrection, up);
        }

        internal static float ResolveHorizontalError(
            Vector3 correction,
            Vector3 componentUp) =>
            Vector3.ProjectOnPlane(
                correction,
                componentUp.normalized).magnitude;

        internal static Vector3 RaiseToMinimum(
            Vector3 correction,
            Vector3 minimumCorrection,
            Vector3 componentUp)
        {
            Vector3 up = componentUp.normalized;
            float missing = Vector3.Dot(
                minimumCorrection - correction,
                up);
            return missing > 0f
                ? correction + up * missing
                : correction;
        }

        internal static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z);
    }
}
