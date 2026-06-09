namespace ThirdPersonSimulation
{
    public enum SimulationTickPhase
    {
        ReadInput,
        UpdateInputBuffer,
        GameplayDecision,
        BuildMotion,
        ExecuteMotion,
        WriteSnapshotAndEvents,
        PresentationBridge
    }
}
