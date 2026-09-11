using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentControlDocumentMutationModule
    {
        internal static void BuildControlConfigurationMutations(
            AgentAuthoringTarget current,
            AgentDocumentEditable target,
            AgentMutationPlanBuilder mutations,
            AgentCompileReport report)
        {
            var currentControl = new AgentDocumentControlConfiguration
            {
                moduleId = current?.editable?.control?.moduleId,
                semanticVersion = current?.editable?.control?.semanticVersion ?? 0,
                parameters = current?.editable?.control?.parameters ?? new List<AgentControlParameter>()
            };
            if (AgentControlDocumentMapper.SemanticEquals(currentControl, target?.control))
                return;
            if (target?.control == null)
            {
                report.Error(
                    "document.editable.controller.control",
                    "control_configuration_missing",
                    "控制配置缺失，不能创建Control Definition Mutation。");
                return;
            }
            string path = "document.editable.controller.control";
            mutations.Add(path, id =>
            {
                var configuration = new AgentDocumentControlConfiguration
                {
                    moduleId = target.control.moduleId,
                    semanticVersion = target.control.semanticVersion,
                    parameters = new List<AgentControlParameter>()
                };
                foreach (AgentControlParameter parameter in target.control.parameters ?? new List<AgentControlParameter>())
                {
                    if (parameter == null)
                    {
                        report.Error(path, "control_parameter_missing", "控制参数不能为空。");
                        continue;
                    }
                    if (!Enum.TryParse(parameter.valueType, false, out SemanticValueKind _))
                    {
                        report.Error(path + ".parameters.valueType", "control_parameter_type_invalid", $"控制参数值类型无效：{parameter.valueType}");
                        continue;
                    }
                    configuration.parameters.Add(new AgentControlParameter
                    {
                        id = parameter.id,
                        valueType = parameter.valueType,
                        numericValue = parameter.numericValue
                    });
                }
                return new AgentConfigureControlConfigurationMutation(id, path, configuration);
            });
        }
    }
}
