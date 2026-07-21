using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DeterministicRollback;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback
{
    public interface IDeterministicRollbackSimulationActorRegistration : IFixedSimulationActorRegistration
    {
        IFixedCharacterControlSourceRuntime RollbackInput { get; }
        void BindRuntimeDiagnostics(
            RollbackRuntimeState state,
            RollbackOutputCommitter outputCommitter,
            IRollbackNetworkDiagnosticsSource networkDiagnostics);
    }

    public interface IDeterministicRollbackPreparedSource : IFixedSimulationPreparedSource
    {
        DeterministicRollbackModelDefinition ModelDefinition { get; }
        string LocalPeerId { get; }
    }
}
