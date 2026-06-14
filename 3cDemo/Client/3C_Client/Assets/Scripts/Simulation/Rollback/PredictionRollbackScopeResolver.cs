using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using ThirdPersonMovement;

namespace ThirdPersonSimulation
{
    public static class PredictionRollbackScopeResolver
    {
        const string TurnBackAliasKey = "Locomotion.Turn.Back";

        public static PredictionRollbackAuthorityPolicy ResolveRootPose()
        {
            return PredictionRollbackAuthorityPolicy.StrictLogic;
        }

        public static PredictionRollbackAuthorityPolicy ResolveMotionExecutor()
        {
            return PredictionRollbackAuthorityPolicy.StrictLogic;
        }

        public static PredictionRollbackAuthorityPolicy ResolveLocomotionFacts()
        {
            return PredictionRollbackAuthorityPolicy.StrictLogic;
        }

        public static PredictionRollbackAuthorityPolicy ResolveActionFacts()
        {
            return PredictionRollbackAuthorityPolicy.ActionTimeline;
        }

        public static PredictionRollbackAuthorityPolicy ResolveLocomotionPlayback(
            BasicMovementPhase phase,
            string aliasKey)
        {
            return IsTurnBackProfilePlayback(phase, aliasKey)
                ? PredictionRollbackAuthorityPolicy.ProfileDriven
                : PredictionRollbackAuthorityPolicy.VisualOnly;
        }

        public static PredictionRollbackAuthorityPolicy ResolveActionPlayback(ActionAnimationKey key)
        {
            return key.IsValid
                ? PredictionRollbackAuthorityPolicy.ActionVisualPlayback
                : PredictionRollbackAuthorityPolicy.VisualOnly;
        }

        public static RollbackCompareScope ResolveSnapshotAnimationScope(
            in CharacterSimulationSnapshot expected,
            in CharacterSimulationSnapshot actual)
        {
            PredictionRollbackAuthorityPolicy expectedSnapshot = ResolveLocomotionPlayback(expected.LocomotionPhase, expected.AnimationKey);
            PredictionRollbackAuthorityPolicy actualSnapshot = ResolveLocomotionPlayback(actual.LocomotionPhase, actual.AnimationKey);
            PredictionRollbackAuthorityPolicy expectedBlackboard = ResolveLocomotionPlayback(
                expected.RuntimeBlackboard.Animation.LocomotionProgress.Phase,
                expected.RuntimeBlackboard.Animation.LocomotionProgress.AliasKey);
            PredictionRollbackAuthorityPolicy actualBlackboard = ResolveLocomotionPlayback(
                actual.RuntimeBlackboard.Animation.LocomotionProgress.Phase,
                actual.RuntimeBlackboard.Animation.LocomotionProgress.AliasKey);

            if (IsStrict(expectedSnapshot) || IsStrict(actualSnapshot) || IsStrict(expectedBlackboard) || IsStrict(actualBlackboard))
                return RollbackCompareScope.StrictGameplay;

            return RollbackCompareScope.PresentationDrift;
        }

        public static bool IsStrict(RollbackCompareScope scope)
        {
            return scope == RollbackCompareScope.StrictGameplay;
        }

        public static bool IsStrict(in PredictionRollbackAuthorityPolicy policy)
        {
            return IsStrict(policy.CompareScope);
        }

        static bool IsTurnBackProfilePlayback(BasicMovementPhase phase, string aliasKey)
        {
            return phase == BasicMovementPhase.TurnBack &&
                   string.Equals(aliasKey, TurnBackAliasKey, System.StringComparison.Ordinal);
        }
    }
}
