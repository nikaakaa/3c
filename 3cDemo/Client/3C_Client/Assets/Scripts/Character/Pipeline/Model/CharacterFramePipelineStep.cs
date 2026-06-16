namespace ThirdPersonAction
{
    public enum CharacterFramePipelineStep
    {
        None = 0,
        ReadInput = 1,
        UpdateInputBuffer = 2,
        GameplayDecision = 3,
        BuildMotion = 4,
        ExecuteMotion = 5,
        PresentationBridge = 6,
        WriteSnapshotAndEvents = 7,
        Completed = 8,
        Failed = 9
    }
}
