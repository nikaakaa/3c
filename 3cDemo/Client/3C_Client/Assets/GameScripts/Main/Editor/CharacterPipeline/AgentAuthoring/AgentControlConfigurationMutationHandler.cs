using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Control.Rules;
using ThirdPersonSimulation;

using ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.Skill;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal sealed class AgentControlConfigurationMutationHandler : IAgentMutationHandler
    {
        public bool Preflight(AgentMutationSession session, AgentMutation command)
        {
            if (!session.Definition || command is not AgentConfigureControlConfigurationMutation configure)
            {
                session.Report.Error(command.Path, "control_configuration_domain_invalid", "Control configuration mutation只能作用于CharacterController。");
                return false;
            }
            bool valid = AgentControlDocumentMapper.Validate(configure.Configuration, session.Report);
            if (valid)
                session.AddPlanned(command, configure.Configuration.moduleId, configure.Configuration.parameters.Count.ToString());
            return valid;
        }

        public void Apply(AgentMutationSession session, AgentMutation command)
        {
            AgentConfigureControlConfigurationMutation configure =
                (AgentConfigureControlConfigurationMutation)command;
            var parameters = new List<CharacterControlParameterConfiguration>();
            foreach (AgentControlParameter parameter in configure.Configuration.parameters ?? new List<AgentControlParameter>())
            {
                parameters.Add(new CharacterControlParameterConfiguration(
                    parameter.id,
                    Enum.Parse<SemanticValueKind>(parameter.valueType, false),
                    parameter.numericValue));
            }
            session.Definition.SetControlConfiguration(
                CorinCharacterControlModuleCatalog.Create(),
                configure.Configuration.moduleId,
                parameters);
            session.AddAppliedAuthoring(
                command,
                session.Definition,
                session.Definition.ControlModuleId,
                configure.Configuration.moduleId,
                "control module binding and parameter configuration");
        }
    }
}
