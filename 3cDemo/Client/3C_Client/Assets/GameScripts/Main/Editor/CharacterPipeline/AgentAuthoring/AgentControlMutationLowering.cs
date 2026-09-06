using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.AI;
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
                parameters = new List<AgentSnapshotControlParameter>()
            };
            foreach (AgentSnapshotControlParameter parameter in operation.controlParameters ?? new List<AgentSnapshotControlParameter>())
            {
                if (parameter == null)
                {
                    context.Error("controlParameters", "control_parameter_missing", "控制参数不能为空。");
                    continue;
                }
                if (!Enum.TryParse(parameter.valueType, false, out SemanticValueKind _))
                    context.Error("controlParameters.valueType", "control_parameter_type_invalid", $"控制参数值类型无效：{parameter.valueType}");
                configuration.parameters.Add(new AgentSnapshotControlParameter
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

        internal static AgentMutation LowerSetSkillDefinition(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
        {
            string skillId = context.RequiredText(operation.skillId, string.Empty, "skillId", "set_skill_definition 缺少 skillId。");
            string entryGraphAuthoringId = context.RequiredText(
                operation.entryGraphAuthoringId,
                operation.entryGraphPlannedIdentity,
                "entryGraphAuthoringId",
                "set_skill_definition 缺少 entryGraphAuthoringId。");
            AgentGraphTargetReference entryGraph = context.RequiredGraph(
                operation.entryGraphAuthoringId,
                operation.entryGraphPlannedIdentity,
                "entryGraph");
            string actionProfileId = context.RequiredText(
                operation.actionProfile,
                string.Empty,
                "actionProfile",
                "set_skill_definition 缺少 ActionProfile identity。");
            string actionContext = context.RequiredText(
                operation.actionContext,
                string.Empty,
                "actionContext",
                "set_skill_definition 缺少 ActionContext identity。");
            return context.IsValid
                ? new AgentSetSkillDefinitionMutation(
                    operation.id,
                    context.Path,
                    new AgentSnapshotSkillDefinition
                    {
                        skillId = skillId,
                        entryGraphAuthoringId = entryGraphAuthoringId,
                        actionProfileId = actionProfileId,
                        actionProfileAssetPath = operation.actionProfileAssetPath,
                        actionProfileAssetGuid = operation.actionProfileAssetGuid,
                        actionContext = actionContext,
                        actionContextAssetPath = operation.actionContextAssetPath,
                        actionContextAssetGuid = operation.actionContextAssetGuid,
                        sourceInputRequestId = operation.sourceInputRequestId,
                        consumeSourceInputRequest = operation.consumeSourceInputRequest,
                        targetInputValueId = operation.targetInputValueId,
                        targetKey = operation.targetKey,
                        subgraphDependencies = operation.subgraphDependencies
                            ?.Select(value => AgentAuthoringDocumentCodec.Clone(value))
                            .ToList() ?? new List<AgentSnapshotSkillSubgraphDependency>(),
                        allowedFollowUpSkillIds = operation.allowedFollowUpSkillIds?.ToList() ?? new List<string>()
                    },
                    entryGraph)
                : null;
        }

        internal static AgentMutation LowerDeleteSkillDefinition(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
        {
            string skillId = context.RequiredText(operation.skillId, string.Empty, "skillId", "delete_skill_definition 缺少 skillId。");
            return context.IsValid
                ? new AgentDeleteSkillDefinitionMutation(operation.id, context.Path, skillId)
                : null;
        }
    }
}
