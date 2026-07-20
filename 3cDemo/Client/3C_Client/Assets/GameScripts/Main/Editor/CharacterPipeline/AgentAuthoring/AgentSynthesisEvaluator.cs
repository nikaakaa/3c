using ThirdPersonCharacter.Pipeline;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentSynthesisEvaluator
    {
        readonly AgentGraphValidator m_Validator = new AgentGraphValidator();

        public AgentCompileReport EvaluateDefaultSamples(CharacterPipelineDefinition definition)
        {
            return m_Validator.Validate(definition);
        }
    }
}
