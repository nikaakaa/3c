using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterSimulationInputNodeEmitterRegistration
    {
        public static void Register(CharacterSimulationNodeEmitterRegistry registry)
        {
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<CharacterInputBoolInfoNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.InputBoolean, text0: node.InputValueId)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<CharacterInputFloatInfoNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.InputScalar, text0: node.InputValueId)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<CharacterInputVector2InfoNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.InputVector2, text0: node.InputValueId)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<CharacterInputVector2MagnitudeInfoNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.InputVector2Magnitude, text0: node.InputValueId)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<CharacterActionRequestInfoNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.InputRequest, text0: node.RequestId)));
        }
    }

    internal static class CharacterSimulationBlackboardNodeEmitterRegistration
    {
        public static void Register(CharacterSimulationNodeEmitterRegistry registry)
        {
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<PipelineBlackboardBoolInfoNode>(node => BlackboardRead(node)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<PipelineBlackboardIntInfoNode>(node => BlackboardRead(node)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<PipelineBlackboardFloatInfoNode>(node => BlackboardRead(node)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<PipelineBlackboardStringInfoNode>(node => BlackboardRead(node)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<PipelineBlackboardVector2InfoNode>(node => BlackboardRead(node)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<PipelineBlackboardVector3InfoNode>(node => BlackboardRead(node)));
        }

        static CharacterSimulationNodeEmission BlackboardRead(PipelineBlackboardValueInfoNode node)
        {
            return new CharacterSimulationNodeEmission(
                SimulationOperationCode.BlackboardGet,
                text0: node.BlackboardVariable.DeclarationId,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("DeclarationOwner", node.BlackboardVariable.DeclarationOwnerId)));
        }
    }
}
