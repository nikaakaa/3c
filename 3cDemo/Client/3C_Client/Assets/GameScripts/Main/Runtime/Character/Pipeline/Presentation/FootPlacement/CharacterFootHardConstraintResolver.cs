using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct CharacterFootHardConstraintResult
    {

        internal CharacterFootHardConstraintResult(
            bool resolved,
            bool available,
            CharacterFootSafetyFloorOwner owner,
            int surfaceIdentity,
            ulong pathIdentity,
            Vector3 inputCorrection,
            Vector3 minimumCorrection,
            Vector3 outputCorrection)
        {
            Resolved = resolved;
            Available = available;
            Owner = owner;
            SurfaceIdentity = surfaceIdentity;
            PathIdentity = pathIdentity;
            InputCorrection = inputCorrection;
            MinimumCorrection = minimumCorrection;
            OutputCorrection = outputCorrection;
        }

        internal bool Resolved { get; }
        internal bool Available { get; }
        internal CharacterFootSafetyFloorOwner Owner { get; }
        internal int SurfaceIdentity { get; }
        internal ulong PathIdentity { get; }
        internal readonly Vector3 InputCorrection;
        internal readonly Vector3 MinimumCorrection;
        internal readonly Vector3 OutputCorrection;
    }

    internal static class CharacterFootHardConstraintResolver
    {
        internal static CharacterFootHardConstraintResult Resolve(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame,
            in CharacterFootSupportTarget selectedTarget,
            Vector3 correction)
        {
            ref readonly CharacterFootSwingMotionResult swing = ref frame.SwingMotion;
            bool ownsSwingPath = selectedTarget.Kind ==
                CharacterFootSupportTargetKind.SwingGround && swing.Accepted;
            switch (context.Discrete.State)
            {
                case CharacterFootConstraintState.Swing when ownsSwingPath:
                case CharacterFootConstraintState.UnlockedSupport
                    when ownsSwingPath:
                {
                    Vector3 minimum =
                        CharacterFootConstraintMath.ResolvePointMinimumCorrection(
                        frame.AnimatedFoot,
                        swing.EnvelopeSample,
                        frame.ComponentUp);
                    return Result(
                        true,
                        true,
                        CharacterFootSafetyFloorOwner.GroundPathEnvelope,
                        0,
                        swing.GroundPathInputIdentity,
                        correction,
                        minimum,
                        frame.ComponentUp,
                        true);
                }
                case CharacterFootConstraintState.Swing
                    when frame.PreparedPlantActive:
                case CharacterFootConstraintState.UnlockedSupport
                    when frame.PreparedPlantActive:
                {
                    Vector3 minimum =
                        CharacterFootConstraintMath.ResolvePointMinimumCorrection(
                            frame.AnimatedFoot,
                            frame.PreparedPlantTarget.Point,
                            frame.ComponentUp);
                    return Result(
                        true,
                        true,
                        CharacterFootSafetyFloorOwner.PlantTarget,
                        frame.PreparedPlantTarget.SurfaceIdentity,
                        0,
                        correction,
                        minimum,
                        frame.ComponentUp,
                        false);
                }
                case CharacterFootConstraintState.Locked:
                {
                    Vector3 minimum =
                        CharacterFootConstraintMath.ResolveContactCorrection(
                            frame.AnimatedFoot,
                            context.Contact.Anchor);
                    return Result(
                        true,
                        true,
                        CharacterFootSafetyFloorOwner.ContactAnchor,
                        context.Contact.SurfaceIdentity,
                        0,
                        correction,
                        minimum,
                        frame.ComponentUp,
                        true);
                }
                default:
                    return new CharacterFootHardConstraintResult(
                        false,
                        false,
                        CharacterFootSafetyFloorOwner.None,
                        0,
                        0,
                        correction,
                        default,
                        correction);
            }
        }

        static CharacterFootHardConstraintResult Result(
            bool resolved,
            bool available,
            CharacterFootSafetyFloorOwner owner,
            int surfaceIdentity,
            ulong pathIdentity,
            Vector3 correction,
            Vector3 minimum,
            Vector3 componentUp,
            bool applyMinimum) =>
            new CharacterFootHardConstraintResult(
                resolved,
                available,
                owner,
                surfaceIdentity,
                pathIdentity,
                correction,
                minimum,
                applyMinimum
                    ? CharacterFootConstraintMath.RaiseToMinimum(
                        correction,
                        minimum,
                        componentUp)
                    : correction);

    }
}
