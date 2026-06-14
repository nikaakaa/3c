using ThirdPersonAnimation;
using ThirdPersonCharacterStateMachine;
using UnityEngine;

namespace ThirdPersonMovement
{
    public readonly struct TurnBackMotionResolution
    {
        public TurnBackMotionResolution(
            BasicMovementMotionFacts motionFacts,
            Vector3 appliedPlanarDelta,
            float appliedYawDelta,
            BasicMovementPlanarDeltaSpace deltaSpace,
            Vector3 entryPlanarBasisForward,
            bool entryBasisMissing,
            Vector3 rejectedPlanarDelta)
        {
            MotionFacts = motionFacts;
            AppliedPlanarDelta = appliedPlanarDelta;
            AppliedYawDelta = appliedYawDelta;
            DeltaSpace = deltaSpace;
            EntryPlanarBasisForward = entryPlanarBasisForward;
            EntryBasisMissing = entryBasisMissing;
            RejectedPlanarDelta = rejectedPlanarDelta;
        }

        public BasicMovementMotionFacts MotionFacts { get; }
        public Vector3 AppliedPlanarDelta { get; }
        public float AppliedYawDelta { get; }
        public BasicMovementPlanarDeltaSpace DeltaSpace { get; }
        public Vector3 EntryPlanarBasisForward { get; }
        public bool EntryBasisMissing { get; }
        public Vector3 RejectedPlanarDelta { get; }
    }

    public static class TurnBackMotionResolver
    {
        public static TurnBackMotionResolution Resolve(
            BasicMovementPhase phase,
            string aliasKey,
            in TurnBackMotionPolicy policy,
            in AnimationMotionProfileSample bakedSample,
            Vector3 entryPlanarBasisForward,
            in StateTimelineWindowFacts timelineFacts)
        {
            bool hasTimelineFacts = timelineFacts.StateId == CharacterStateIds.TurnBack;
            bool motionWindowActive = !hasTimelineFacts || timelineFacts.MotionWindowActive;
            bool inputLockActive = hasTimelineFacts ? timelineFacts.InputLockWindowActive : policy.SuppressInputRotation || policy.SuppressInputPlanarMovement;
            BasicMovementPlanarDeltaSpace deltaSpace = policy.TranslationSource == TurnBackMotionTranslationSource.BakedMotionProfile
                ? BasicMovementPlanarDeltaSpace.EntryLocal
                : BasicMovementPlanarDeltaSpace.World;
            bool entryBasisValid = deltaSpace != BasicMovementPlanarDeltaSpace.EntryLocal ||
                                   TryNormalizePlanar(entryPlanarBasisForward, out entryPlanarBasisForward);
            Vector3 planarDelta = motionWindowActive
                ? ResolvePlanarDelta(in policy, in bakedSample)
                : Vector3.zero;
            Vector3 rejectedPlanarDelta = Vector3.zero;
            bool entryBasisMissing = false;
            if (deltaSpace == BasicMovementPlanarDeltaSpace.EntryLocal &&
                planarDelta.sqrMagnitude > 0.000001f &&
                !entryBasisValid)
            {
                rejectedPlanarDelta = planarDelta;
                entryBasisMissing = true;
                planarDelta = Vector3.zero;
            }

            float appliedYawDelta = motionWindowActive
                ? ResolveYawDelta(in policy, in bakedSample)
                : 0f;
            bool hasMotion = planarDelta.sqrMagnitude > 0.000001f || Mathf.Abs(appliedYawDelta) > 0.0001f;
            BasicMovementMotionFacts motionFacts = new BasicMovementMotionFacts(
                hasMotion,
                planarDelta,
                appliedYawDelta,
                phase,
                aliasKey,
                inputLockActive && policy.SuppressInputRotation,
                inputLockActive && policy.SuppressInputPlanarMovement,
                deltaSpace,
                policy,
                entryPlanarBasisForward);

            return new TurnBackMotionResolution(
                motionFacts,
                planarDelta,
                appliedYawDelta,
                deltaSpace,
                entryPlanarBasisForward,
                entryBasisMissing,
                rejectedPlanarDelta);
        }

        public static bool RequiresBakedMotion(in TurnBackMotionPolicy policy)
        {
            return policy.YawSource == TurnBackMotionYawSource.BakedMotionProfile ||
                   policy.TranslationSource == TurnBackMotionTranslationSource.BakedMotionProfile;
        }

        static Vector3 ResolvePlanarDelta(
            in TurnBackMotionPolicy policy,
            in AnimationMotionProfileSample bakedSample)
        {
            switch (policy.TranslationSource)
            {
                case TurnBackMotionTranslationSource.BakedMotionProfile:
                    return bakedSample.HasMotionContribution ? bakedSample.LocalPlanarDelta : Vector3.zero;
                default:
                    return Vector3.zero;
            }
        }

        static float ResolveYawDelta(
            in TurnBackMotionPolicy policy,
            in AnimationMotionProfileSample bakedSample)
        {
            switch (policy.YawSource)
            {
                case TurnBackMotionYawSource.BakedMotionProfile:
                    return bakedSample.HasMotionContribution ? bakedSample.YawDelta : 0f;
                default:
                    return 0f;
            }
        }

        static bool TryNormalizePlanar(Vector3 value, out Vector3 normalized)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            if (sqrMagnitude <= 0.000001f)
            {
                normalized = Vector3.zero;
                return false;
            }

            normalized = value / Mathf.Sqrt(sqrMagnitude);
            return true;
        }
    }
}
