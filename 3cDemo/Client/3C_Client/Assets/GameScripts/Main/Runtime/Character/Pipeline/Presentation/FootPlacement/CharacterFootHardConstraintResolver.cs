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
        internal Vector3 InputCorrection { get; }
        internal Vector3 MinimumCorrection { get; }
        internal Vector3 OutputCorrection { get; }
    }

    internal static class CharacterFootHardConstraintResolver
    {
        internal static CharacterFootHardConstraintResult Resolve(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame,
            Vector3 correction)
        {
            CharacterFootSwingMotionResult swing = frame.SwingMotion;
            switch (context.Discrete.State)
            {
                case CharacterFootConstraintState.Swing when swing.Accepted:
                case CharacterFootConstraintState.UnlockedSupport
                    when swing.Accepted:
                {
                    if (!frame.CurrentGroundFloor.Accepted)
                    {
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
                    Vector3 minimum =
                        CharacterFootConstraintMath.ResolvePointMinimumCorrection(
                        frame.AnimatedFoot,
                        frame.CurrentGroundFloor.Point,
                        frame.ComponentUp);
                    return Result(
                        true,
                        true,
                        CharacterFootSafetyFloorOwner.CurrentGroundFloor,
                        frame.CurrentGroundFloor.SurfaceIdentity,
                        0,
                        correction,
                        minimum,
                        frame.ComponentUp);
                }
                case CharacterFootConstraintState.Landing:
                case CharacterFootConstraintState.Locked:
                {
                    Vector3 minimum =
                        CharacterFootConstraintMath.ResolveContactCorrection(
                            frame.AnimatedFoot,
                            context.Contact.Anchor);
                    return Result(
                        true,
                        false,
                        CharacterFootSafetyFloorOwner.ContactAnchor,
                        context.Contact.SurfaceIdentity,
                        0,
                        correction,
                        minimum,
                        frame.ComponentUp);
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
            Vector3 componentUp) =>
            new CharacterFootHardConstraintResult(
                resolved,
                available,
                owner,
                surfaceIdentity,
                pathIdentity,
                correction,
                minimum,
                CharacterFootConstraintMath.RaiseToMinimum(
                    correction,
                    minimum,
                    componentUp));

    }
}
