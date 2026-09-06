using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentBlackboardDocumentMapper
    {
        internal static bool ValidateBlackboardPackage(
            AgentPackageBlackboardFile blackboard,
            AgentCompileReport report)
        {
            bool valid = ValidateBlackboardSchemaRevision(
                blackboard?.schemaRevision ?? 0,
                "editable/blackboard.json.schemaRevision",
                report);
            int index = 0;
            foreach (AgentSnapshotBlackboardDeclaration declaration in
                     blackboard?.declarations ?? new List<AgentSnapshotBlackboardDeclaration>())
            {
                string path = $"editable/blackboard.json.declarations[{index}]";
                if (declaration == null)
                {
                    report.Error(path, "blackboard_declaration_missing", "Blackboard declaration不能为空。");
                    valid = false;
                    index++;
                    continue;
                }
                if (declaration.inputBinding != null &&
                    string.IsNullOrWhiteSpace(declaration.inputBinding.inputValueId))
                {
                    report.Error(
                        path + ".inputBinding.inputValueId",
                        "blackboard_input_value_id_missing",
                        "Blackboard Input Binding必须提供非空inputValueId；没有绑定时应省略inputBinding。");
                    valid = false;
                }
                if (declaration.factProjection != null)
                {
                    if (!Enum.TryParse(
                            declaration.factProjection.kind,
                            false,
                            out PipelineBlackboardFactProjectionKind kind) ||
                        kind != PipelineBlackboardFactProjectionKind.ActionWindow)
                    {
                        report.Error(
                            path + ".factProjection.kind",
                            "blackboard_fact_projection_kind_invalid",
                            "Blackboard Fact Projection必须提供受支持的kind；没有投影时应省略factProjection。");
                        valid = false;
                    }
                    if (string.IsNullOrWhiteSpace(declaration.factProjection.windowType))
                    {
                        report.Error(
                            path + ".factProjection.windowType",
                            "blackboard_action_window_type_missing",
                            "ActionWindow Fact Projection必须提供windowType。");
                        valid = false;
                    }
                    if (string.IsNullOrWhiteSpace(declaration.factProjection.windowId))
                    {
                        report.Error(
                            path + ".factProjection.windowId",
                            "blackboard_action_window_id_missing",
                            "ActionWindow Fact Projection必须提供windowId。");
                        valid = false;
                    }
                }
                index++;
            }
            return valid;
        }

        internal static bool ValidateBlackboardSchemaRevision(
            int revision,
            string path,
            AgentCompileReport report)
        {
            if (revision == PipelineBlackboardAuthoringSchema.CurrentRevision)
                return true;
            report.Error(
                path,
                "blackboard_schema_revision_outdated",
                $"Blackboard schema revision必须是{PipelineBlackboardAuthoringSchema.CurrentRevision}；请重新checkout Document后再apply。");
            return false;
        }
    }
}
