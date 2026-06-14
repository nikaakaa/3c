namespace ThirdPersonSimulation
{
    public enum AnimationPlaybackAuthority
    {
        VisualOnly,
        LogicTimed,
        ProfileDriven,
        AnimatorRuntimeDirect
    }

    public enum RollbackMotionAuthority
    {
        None,
        KinematicInput,
        StateTimeline,
        AnimationProfile,
        AnimatorRuntime,
        MotionExecutor
    }

    public enum RollbackCompareScope
    {
        StrictGameplay,
        PredictiveGameplay,
        PresentationDrift,
        Ignored
    }

    public readonly struct PredictionRollbackAuthorityPolicy
    {
        public PredictionRollbackAuthorityPolicy(
            AnimationPlaybackAuthority animationAuthority,
            RollbackMotionAuthority motionAuthority,
            RollbackCompareScope compareScope)
        {
            AnimationAuthority = animationAuthority;
            MotionAuthority = motionAuthority;
            CompareScope = compareScope;
        }

        public AnimationPlaybackAuthority AnimationAuthority { get; }
        public RollbackMotionAuthority MotionAuthority { get; }
        public RollbackCompareScope CompareScope { get; }

        public static PredictionRollbackAuthorityPolicy StrictLogic =>
            new PredictionRollbackAuthorityPolicy(
                AnimationPlaybackAuthority.LogicTimed,
                RollbackMotionAuthority.MotionExecutor,
                RollbackCompareScope.StrictGameplay);

        public static PredictionRollbackAuthorityPolicy VisualOnly =>
            new PredictionRollbackAuthorityPolicy(
                AnimationPlaybackAuthority.VisualOnly,
                RollbackMotionAuthority.KinematicInput,
                RollbackCompareScope.PresentationDrift);

        public static PredictionRollbackAuthorityPolicy ProfileDriven =>
            new PredictionRollbackAuthorityPolicy(
                AnimationPlaybackAuthority.ProfileDriven,
                RollbackMotionAuthority.AnimationProfile,
                RollbackCompareScope.StrictGameplay);

        public static PredictionRollbackAuthorityPolicy ActionTimeline =>
            new PredictionRollbackAuthorityPolicy(
                AnimationPlaybackAuthority.LogicTimed,
                RollbackMotionAuthority.StateTimeline,
                RollbackCompareScope.StrictGameplay);

        public static PredictionRollbackAuthorityPolicy ActionVisualPlayback =>
            new PredictionRollbackAuthorityPolicy(
                AnimationPlaybackAuthority.VisualOnly,
                RollbackMotionAuthority.StateTimeline,
                RollbackCompareScope.PresentationDrift);
    }
}
