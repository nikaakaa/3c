using System;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public interface ISimulationActorRegistration : IDisposable, ISimulationCheckpointTracePublisher
    {
        ActorId ActorId { get; }
        string OwnerIdentity { get; }
        StableHash DiagnosticsConfigurationHash { get; }
        SimulationOutputRouteDescriptor OutputRoute { get; }
        void BindProgramEpoch(ulong programEpoch);
        void BindExecutionBranch(Guid executionBranchId);
        void Activate();
        void Deactivate();
        void CaptureRenderFrame(ulong renderFrame);
    }

    public interface ISimulationActorStartGate
    {
        bool IsSimulationStartReady { get; }
        string SimulationStartWaitReason { get; }
    }

    public interface ISimulationCheckpointTracePublisher
    {
        void PublishCheckpoint(ulong tick, string snapshotIdentity, StableHash snapshotHash);
    }

    public interface ISimulationPresentationCheckpointRuntime
    {
        bool SupportsPresentationCheckpointCapture { get; }
        bool SupportsPresentationCheckpointRestore { get; }
        bool TryCapturePresentationCheckpoint(
            SimulationSessionCheckpoint checkpoint,
            out string error);
        bool TryRestorePresentationCheckpoint(
            SimulationSessionCheckpoint checkpoint,
            out string error);
    }

    public interface ISimulationProgramEpochRegistration
    {
        ISimulationProgramBinding ProgramBinding { get; }
        void CommitProgramEpoch();
        void DiscardProgramEpoch();
    }

    public interface IFloat32SimulationActorRegistration :
        ISimulationActorRegistration,
        IFloat32PublishedActorResultObserver
    {
        CharacterSimulationProgram Program { get; }
        SimulationActorBinding ProgramIdentity { get; }
        Float32WorldBodyBinding WorldBodyBinding { get; }
        WorldBodyState InitialBody { get; }
        ISimulationGameplayOutputPort GameplayOutput { get; }
        ISimulationPresentationOutputPort PresentationOutput { get; }
        ISimulationDiagnosticsSink SimulationDiagnostics { get; }
        void BeginLogicTick();
    }

    public interface ILocalSimulationActorRegistration : IFloat32SimulationActorRegistration
    {
        ICharacterControlSourceRuntime LocalControlSource { get; }
    }
}
