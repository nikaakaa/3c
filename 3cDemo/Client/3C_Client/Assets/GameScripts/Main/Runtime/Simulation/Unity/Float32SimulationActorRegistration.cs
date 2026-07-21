using System;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public interface ISimulationActorRegistration : IDisposable
    {
        ActorId ActorId { get; }
        string OwnerIdentity { get; }
        StableHash DiagnosticsConfigurationHash { get; }
        SimulationOutputRouteDescriptor OutputRoute { get; }
        void Activate();
        void Deactivate();
        void CaptureRenderFrame(ulong renderFrame);
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
