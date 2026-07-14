namespace ThirdPersonCharacter.ActionSystem
{
    public enum ActionProfileInspectorSection
    {
        Identity,
        Tags,
        Debug
    }

    public static class ActionActivationAuthoringContract
    {
        public const string ActivateActionInstanceName = "ActivateActionInstance";
        public const string LifecycleTransitionName = "SubmitActionLifecycleTransition";
        public const string ActionProfileField = "ActionProfile";
        public const string SourceInputRequestIdField = "SourceInputRequestId";
        public const string TargetKeyField = "TargetKey";
        public const string TargetSnapshotBlackboardKeyField = "TargetSnapshotBlackboardKey";
        public const string ConsumeSourceInputRequestField = "ConsumeSourceInputRequest";
        public const string ActionContextField = "ActionContext";
    }

    public static class TimelineWindowInspectorContract
    {
        public const string WindowTypeField = "WindowType";
        public const string WindowIdField = "WindowId";
        public const string WindowParametersField = "Parameters";
    }

    public static class ActionRuntimeDebugContract
    {
        public const string ActionInstancePanel = "ActionInstance";
        public const string WindowPanel = "Windows";
    }
}
