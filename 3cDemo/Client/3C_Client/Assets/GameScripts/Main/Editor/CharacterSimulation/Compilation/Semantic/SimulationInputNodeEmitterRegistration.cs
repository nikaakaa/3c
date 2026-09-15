using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class SimulationInputNodeEmitterRegistration
    {
        public static void Register(SimulationNodeEmitterRegistry registry)
        {
            registry.Register(SimulationNodeEmitterRegistry.Simple<CharacterInputBoolInfoNode>(node => Input(SimulationOperationCode.InputBoolean, node)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<CharacterInputFloatInfoNode>(node => Input(SimulationOperationCode.InputScalar, node)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<CharacterInputVector2InfoNode>(node => Input(SimulationOperationCode.InputVector2, node)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<CharacterInputVector2MagnitudeInfoNode>(node => Input(SimulationOperationCode.InputVector2Magnitude, node)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<CharacterActionRequestInfoNode>(node => Request(node)));
        }

        static SimulationNodeEmission Input(
            SimulationOperationCode code,
            ICharacterInputValueAuthoring node) =>
            new SimulationNodeEmission(code, text0: node.InputId);

        static SimulationNodeEmission Request(
            ICharacterActionRequestAuthoring node) =>
            new SimulationNodeEmission(SimulationOperationCode.InputRequest, text0: node.RequestId);
    }

    internal static class SimulationBlackboardNodeEmitterRegistration
    {
        public static void Register(SimulationNodeEmitterRegistry registry)
        {
            registry.Register(SimulationNodeEmitterRegistry.Simple<PipelineBlackboardBoolInfoNode>(node => BlackboardRead(node)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<PipelineBlackboardIntInfoNode>(node => BlackboardRead(node)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<PipelineBlackboardFloatInfoNode>(node => BlackboardRead(node)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<PipelineBlackboardStringInfoNode>(node => BlackboardRead(node)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<PipelineBlackboardVector2InfoNode>(node => BlackboardRead(node)));
            registry.Register(SimulationNodeEmitterRegistry.Simple<PipelineBlackboardVector3InfoNode>(node => BlackboardRead(node)));
        }

        static SimulationNodeEmission BlackboardRead(PipelineBlackboardValueInfoNode node)
        {
            ICharacterBlackboardAuthoring authoring = node;
            return new SimulationNodeEmission(
                SimulationOperationCode.BlackboardGet,
                text0: authoring.DeclarationId,
                constants: SimulationNodeEmitterRegistry.Fields(("DeclarationOwner", authoring.OwnerId)));
        }
    }
}
