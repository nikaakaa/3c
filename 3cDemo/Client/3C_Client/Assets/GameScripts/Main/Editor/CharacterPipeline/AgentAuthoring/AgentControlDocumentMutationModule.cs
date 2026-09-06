using System;
using System.Collections.Generic;
using System.Linq;
using static ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.AgentDocumentMutationSupport;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentControlDocumentMutationModule
    {
        internal static void BuildControlConfigurationMutations(
            AgentGraphSnapshot current,
            AgentDocumentEditable target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            var currentControl = new AgentDocumentControlConfiguration
            {
                moduleId = current?.controlModuleId,
                semanticVersion = current?.controlSemanticVersion ?? 0,
                parameters = current?.controlParameters ?? new List<AgentSnapshotControlParameter>()
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
            Add(mutations, "document.editable.controller.control", AgentMutationKind.ConfigureControlConfiguration, operation =>
            {
                operation.controlModuleId = target.control.moduleId;
                operation.controlSemanticVersion = target.control.semanticVersion;
                operation.controlParameters = target.control.parameters
                    ?.Select(value => AgentAuthoringDocumentCodec.Clone(value))
                    .ToList() ?? new List<AgentSnapshotControlParameter>();
            });
        }
    }
}
