using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentPatchAuthoringService
    {
        public AgentAuthoringResponse Execute(AgentAuthoringRequest request)
        {
            if (request == null)
                return Failure(default, string.Empty, "request_missing", "Agent authoring request 缺失。");

            string action = AgentAuthoringActionUtility.ToProtocolValue(request.action);
            if (string.IsNullOrEmpty(action))
                return Failure(request.action, request.definitionAssetPath, "unsupported_action", "不支持的 Agent authoring action。");

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return Failure(request.action, request.definitionAssetPath, "editor_busy", "Unity Editor 正在编译或更新 AssetDatabase。");

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return Failure(request.action, request.definitionAssetPath, "play_mode_active", "Play Mode 或 Play Mode 切换期间禁止执行 authoring 操作。");

            if (!TryLoadDefinition(request.definitionAssetPath, out CharacterPipelineDefinition definition, out string code, out string message))
                return Failure(request.action, request.definitionAssetPath, code, message);

            PrepareGraphReferences(definition);

            switch (request.action)
            {
                case AgentAuthoringAction.ExportSnapshot:
                    return ExportSnapshot(request, definition);
                case AgentAuthoringAction.DryRunPatch:
                    return ExecutePatch(request, definition, false);
                case AgentAuthoringAction.ApplyPatch:
                    return ExecutePatch(request, definition, true);
                case AgentAuthoringAction.Validate:
                    return Validate(request, definition);
                default:
                    return Failure(request.action, request.definitionAssetPath, "unsupported_action", "不支持的 Agent authoring action。");
            }
        }

        AgentAuthoringResponse ExportSnapshot(AgentAuthoringRequest request, CharacterPipelineDefinition definition)
        {
            return new AgentAuthoringResponse
            {
                action = AgentAuthoringActionUtility.ToProtocolValue(request.action),
                definitionAssetPath = request.definitionAssetPath,
                success = true,
                snapshot = new AgentGraphSnapshotExporter().ExportFull(definition)
            };
        }

        AgentAuthoringResponse Validate(AgentAuthoringRequest request, CharacterPipelineDefinition definition)
        {
            AgentCompileReport report = new AgentGraphValidator().Validate(definition);
            return FromReport(request, report, false);
        }

        AgentAuthoringResponse ExecutePatch(
            AgentAuthoringRequest request,
            CharacterPipelineDefinition definition,
            bool apply)
        {
            var parseReport = new AgentCompileReport { success = true };
            if (!AgentAuthoringJsonUtility.TryFromJson(request.patchJson, out AgentPatchIR patch, parseReport, "patch-json"))
                return FromReport(request, parseReport, false, "patch_parse_failed", "Patch JSON 解析失败。");

            AgentGraphSnapshot snapshot = new AgentGraphSnapshotExporter().ExportFull(definition);
            var compiler = new AgentPatchCompiler();
            AgentPatchPreparation preparation = compiler.Prepare(definition, snapshot, patch);
            if (!apply || !preparation.IsValid)
                return FromReport(request, preparation.Report, false);

            var ownerCollector = new AgentGraphTransactionOwnerCollector();
            if (!ownerCollector.TryCollect(definition, out UnityEngine.Object[] owners, out string code, out string message))
            {
                preparation.Report.Error("transaction", code, message);
                return FromReport(request, preparation.Report, false, code, message);
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            const string undoName = "Apply BTSMTL Agent Patch";
            Undo.SetCurrentGroupName(undoName);

            AgentCompileReport applied = null;
            try
            {
                Undo.RegisterCompleteObjectUndo(owners, undoName);
                AgentPatchApplyResult applyResult = compiler.Apply(definition, preparation);
                applied = applyResult.Report;

                if (!applied.HasErrors())
                    AppendValidation(applied, new AgentGraphValidator().Validate(definition));

                if (applied.HasErrors())
                {
                    bool rolledBack = Rollback(undoGroup, applied);
                    return FromReport(
                        request,
                        applied,
                        false,
                        rolledBack ? FirstErrorCode(applied, "apply_failed") : "rollback_failed",
                        rolledBack ? "Patch apply 或 graph validation 失败，本次修改已回滚。" : "Patch apply 失败，并且本次 Undo 事务无法完整回滚。");
                }

                MarkTouchedOwnersDirty(applyResult.TouchedOwners);
                AssetDatabase.SaveAssets();
                if (!CharacterSimulationProgramBuildService.Build(definition, true))
                {
                    applied.Error("compiler", "simulation_program_build_failed", "Agent Patch 已通过 Graph 验证，但正式 SimulationProgram/PresentationProjection 发布失败。");
                    bool rolledBack = Rollback(undoGroup, applied);
                    return FromReport(
                        request,
                        applied,
                        false,
                        rolledBack ? "simulation_program_build_failed" : "rollback_failed",
                        rolledBack ? "正式生成产物发布失败，本次 authoring 修改已回滚。" : "正式生成产物发布失败，并且本次 Undo 事务无法完整回滚。");
                }
                Undo.CollapseUndoOperations(undoGroup);
                applied.applied = true;
                applied.success = true;
                return FromReport(request, applied, true);
            }
            catch (Exception exception)
            {
                applied ??= new AgentCompileReport { success = false };
                applied.Error("apply", "apply_exception", exception.Message);
                bool rolledBack = Rollback(undoGroup, applied);
                return FromReport(
                    request,
                    applied,
                    false,
                    rolledBack ? FirstErrorCode(applied, "apply_exception") : "rollback_failed",
                    rolledBack ? "Patch apply 发生异常，本次修改已回滚。" : "Patch apply 发生异常，并且本次 Undo 事务无法完整回滚。");
            }
        }

        static bool TryLoadDefinition(
            string path,
            out CharacterPipelineDefinition definition,
            out string errorCode,
            out string errorMessage)
        {
            definition = null;
            errorCode = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("\\") || path.Contains("/../"))
            {
                errorCode = "definition_path_invalid";
                errorMessage = "definition_asset_path 必须是精确的 Assets/... 项目资产路径。";
                return false;
            }

            definition = AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(path);
            if (!definition || !string.Equals(AssetDatabase.GetAssetPath(definition), path, StringComparison.Ordinal))
            {
                errorCode = "definition_not_found";
                errorMessage = $"无法在指定路径加载 CharacterPipelineDefinition：{path}";
                return false;
            }

            return true;
        }

        static void PrepareGraphReferences(CharacterPipelineDefinition definition)
        {
            BaseTree rootTree = definition?.RootTreeAsset?.Tree;
            if (rootTree == null)
                return;

            var errors = new List<string>();
            CharacterAuthoringTopologyProjection topology = CharacterAuthoringTopologyProjection.Build(rootTree, errors);
            for (int i = 0; i < topology.Graphs.Count; i++)
            {
                CharacterAuthoringGraphEntry entry = topology.Graphs[i];
                if (entry.FirstOccurrence)
                    entry.Graph.RebindReadOnlyViewReferences();
            }
        }

        static void AppendValidation(AgentCompileReport target, AgentCompileReport validation)
        {
            if (target == null || validation == null)
                return;

            for (int i = 0; i < validation.messages.Count; i++)
                target.messages.Add(validation.messages[i]);

            target.metrics.semanticValidCount = validation.metrics.semanticValidCount;
            target.metrics.semanticInvalidCount = validation.metrics.semanticInvalidCount;
            target.metrics.compileSuccessCount = validation.metrics.compileSuccessCount;
            target.metrics.compileFailureCount = validation.metrics.compileFailureCount;
            target.success = !target.HasErrors();
        }

        static void MarkTouchedOwnersDirty(System.Collections.Generic.IReadOnlyList<UnityEngine.Object> owners)
        {
            if (owners == null)
                return;
            for (int i = 0; i < owners.Count; i++)
            {
                if (owners[i])
                    EditorUtility.SetDirty(owners[i]);
            }
        }

        static bool Rollback(int undoGroup, AgentCompileReport report)
        {
            try
            {
                Undo.RevertAllDownToGroup(undoGroup);
                AssetDatabase.SaveAssets();
                if (report != null)
                {
                    report.applied = false;
                    report.success = false;
                }
                return true;
            }
            catch (Exception exception)
            {
                report?.Error("transaction", "rollback_failed", exception.Message);
                return false;
            }
        }

        static AgentAuthoringResponse Failure(
            AgentAuthoringAction action,
            string definitionAssetPath,
            string errorCode,
            string errorMessage)
        {
            var report = new AgentCompileReport { success = false };
            report.Error("bridge", errorCode, errorMessage);
            return new AgentAuthoringResponse
            {
                action = AgentAuthoringActionUtility.ToProtocolValue(action),
                definitionAssetPath = definitionAssetPath ?? string.Empty,
                success = false,
                errorCode = errorCode,
                errorMessage = errorMessage,
                report = report
            };
        }

        static AgentAuthoringResponse FromReport(
            AgentAuthoringRequest request,
            AgentCompileReport report,
            bool saved,
            string errorCode = "",
            string errorMessage = "")
        {
            bool success = report != null && !report.HasErrors();
            return new AgentAuthoringResponse
            {
                action = AgentAuthoringActionUtility.ToProtocolValue(request.action),
                definitionAssetPath = request.definitionAssetPath,
                success = success,
                applied = success && report.applied,
                saved = success && saved,
                errorCode = success ? string.Empty : (!string.IsNullOrEmpty(errorCode) ? errorCode : FirstErrorCode(report, "authoring_failed")),
                errorMessage = success ? string.Empty : errorMessage,
                report = report
            };
        }

        static string FirstErrorCode(AgentCompileReport report, string fallback)
        {
            if (report?.messages != null)
            {
                for (int i = 0; i < report.messages.Count; i++)
                {
                    AgentCompileMessage message = report.messages[i];
                    if (message.severity == AgentReportSeverity.Error.ToString() && !string.IsNullOrEmpty(message.code))
                        return message.code;
                }
            }

            return fallback;
        }
    }
}
