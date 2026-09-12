using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterSimulationInputNodeEmitterRegistration
    {
        public static void Register(CharacterSimulationNodeEmitterRegistry registry)
        {
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<CharacterInputBoolInfoNode>(node => Input(SimulationOperationCode.InputBoolean, node)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<CharacterInputFloatInfoNode>(node => Input(SimulationOperationCode.InputScalar, node)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<CharacterInputVector2InfoNode>(node => Input(SimulationOperationCode.InputVector2, node)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<CharacterInputVector2MagnitudeInfoNode>(node => Input(SimulationOperationCode.InputVector2Magnitude, node)));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<CharacterActionRequestInfoNode>(node => Request(node)));
        }

        static CharacterSimulationNodeEmission Input(
            SimulationOperationCode code,
            ICharacterInputValueAuthoring node) =>
            new CharacterSimulationNodeEmission(code, text0: node.InputId);

        static CharacterSimulationNodeEmission Request(
            ICharacterActionRequestAuthoring node) =>
            new CharacterSimulationNodeEmission(SimulationOperationCode.InputRequest, text0: node.RequestId);
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
            ICharacterBlackboardAuthoring authoring = node;
            return new CharacterSimulationNodeEmission(
                SimulationOperationCode.BlackboardGet,
                text0: authoring.DeclarationId,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("DeclarationOwner", authoring.OwnerId)));
        }
    }
}
