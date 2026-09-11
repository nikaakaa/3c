using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;
using static ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.AgentMutationLoweringSupport;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentControlMutationLowering
    {
        internal static AgentMutation LowerConfigureControlConfiguration(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
        {
            string moduleId = context.RequiredText(
                operation.controlModuleId,
                string.Empty,
                "controlModuleId",
                "configure_control_configuration 缺少 control module id。");
            var configuration = new AgentDocumentControlConfiguration
            {
                moduleId = moduleId,
                semanticVersion = operation.controlSemanticVersion,
                parameters = new List<AgentControlParameter>()
            };
            foreach (AgentControlParameter parameter in operation.controlParameters ?? new List<AgentControlParameter>())
            {
                if (parameter == null)
                {
                    context.Error("controlParameters", "control_parameter_missing", "控制参数不能为空。");
                    continue;
                }
                if (!Enum.TryParse(parameter.valueType, false, out SemanticValueKind _))
                    context.Error("controlParameters.valueType", "control_parameter_type_invalid", $"控制参数值类型无效：{parameter.valueType}");
                configuration.parameters.Add(new AgentControlParameter
                {
                    id = parameter.id,
                    valueType = parameter.valueType,
                    numericValue = parameter.numericValue
                });
            }
            return context.IsValid
                ? new AgentConfigureControlConfigurationMutation(
                    operation.id,
                    context.Path,
                    configuration)
                : null;
        }

    }
}
