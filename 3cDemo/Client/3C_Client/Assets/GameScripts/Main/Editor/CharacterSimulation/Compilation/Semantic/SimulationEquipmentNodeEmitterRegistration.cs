using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class SimulationEquipmentNodeEmitterRegistration
    {
        public static void Register(SimulationNodeEmitterRegistry registry)
        {
            registry.Register(SimulationNodeEmitterRegistry.Simple<ReadEquipmentIdentityNode>(node => new SimulationNodeEmission(
                SimulationOperationCode.ReadEquipmentIdentity,
                text0: node.SlotId)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<ReadEquipmentParameterNode>(node => new SimulationNodeEmission(
                SimulationOperationCode.ReadEquipmentParameter,
                integer0: (int)node.ValueKind,
                text0: node.ParameterId)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<RequestEquipmentChangeNode>(node => EquipmentChange(
                SimulationOperationCode.RequestEquipmentChange,
                node)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<BeginEquipmentChangeNode>(node => EquipmentChange(
                SimulationOperationCode.BeginEquipmentChange,
                node)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<CommitEquipmentChangeNode>(node => new SimulationNodeEmission(SimulationOperationCode.CommitEquipmentChange)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<CancelEquipmentChangeNode>(node => new SimulationNodeEmission(SimulationOperationCode.CancelEquipmentChange)));
        }

        static SimulationNodeEmission EquipmentChange(SimulationOperationCode code, EquipmentChangeOperationNode node)
        {
            return new SimulationNodeEmission(
                code,
                text0: node.SlotId,
                constants: SimulationNodeEmitterRegistry.Fields(("ActionContext", SimulationNodeEmitterContext.AssetIdentity(node.ActionContext))));
        }
    }
}
