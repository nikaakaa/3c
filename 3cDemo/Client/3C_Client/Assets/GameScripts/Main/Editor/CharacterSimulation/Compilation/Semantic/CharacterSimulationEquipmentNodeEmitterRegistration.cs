using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterSimulationEquipmentNodeEmitterRegistration
    {
        public static void Register(CharacterSimulationNodeEmitterRegistry registry)
        {
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<ReadEquipmentIdentityNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.ReadEquipmentIdentity,
                text0: node.SlotId)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<ReadEquipmentParameterNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.ReadEquipmentParameter,
                integer0: (int)node.ValueKind,
                text0: node.ParameterId)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<RequestEquipmentChangeNode>(node => EquipmentChange(
                SimulationOperationCode.RequestEquipmentChange,
                node)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<BeginEquipmentChangeNode>(node => EquipmentChange(
                SimulationOperationCode.BeginEquipmentChange,
                node)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<CommitEquipmentChangeNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.CommitEquipmentChange)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<CancelEquipmentChangeNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.CancelEquipmentChange)));
        }

        static CharacterSimulationNodeEmission EquipmentChange(SimulationOperationCode code, EquipmentChangeOperationNode node)
        {
            return new CharacterSimulationNodeEmission(
                code,
                text0: node.SlotId,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("ActionContext", CharacterSimulationNodeEmitterContext.AssetIdentity(node.ActionContext))));
        }
    }
}
