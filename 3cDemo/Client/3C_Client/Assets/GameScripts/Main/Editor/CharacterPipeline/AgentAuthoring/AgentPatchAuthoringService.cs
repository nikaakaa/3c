using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.AI.Editor;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
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
                return Failure(default, string.Empty, string.Empty, "request_missing", "Agent authoring request 缺失。");

            string action = AgentAuthoringActionUtility.ToProtocolValue(request.action);
            if (string.IsNullOrEmpty(action))
                return Failure(request.action, request.domain, request.rootAssetPath, "unsupported_action", "不支持的 Agent authoring action。");

            if (!AgentAuthoringSchema.IsDomain(request.domain))
                return Failure(request.action, request.domain, request.rootAssetPath, "unsupported_domain", $"不支持的 Agent authoring domain：{request.domain}");

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return Failure(request.action, request.domain, request.rootAssetPath, "editor_busy", "Unity Editor 正在编译或更新 AssetDatabase。");

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return Failure(request.action, request.domain, request.rootAssetPath, "play_mode_active", "Play Mode 或 Play Mode 切换期间禁止执行 authoring 操作。");

            if (request.action == AgentAuthoringAction.BootstrapAIController)
                return BootstrapAIController(request);

            if (string.Equals(request.domain, AgentAuthoringSchema.CharacterControllerDomain, StringComparison.Ordinal))
            {
                if (!TryLoadRoot(request.rootAssetPath, out CharacterPipelineDefinition definition, out string code, out string message))
                    return Failure(request.action, request.domain, request.rootAssetPath, code, message);
                PrepareGraphReferences(definition.RootTreeAsset?.Tree);
                return ExecuteCharacter(request, definition);
            }

            if (!TryLoadRoot(request.rootAssetPath, out AIControllerDefinition aiDefinition, out string aiCode, out string aiMessage))
                return Failure(request.action, request.domain, request.rootAssetPath, aiCode, aiMessage);
            PrepareGraphReferences(aiDefinition.RootTreeAsset?.Tree);
            return ExecuteAI(request, aiDefinition);
        }

        AgentAuthoringResponse ExecuteCharacter(AgentAuthoringRequest request, CharacterPipelineDefinition definition)
        {
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
                    return Failure(request.action, request.domain, request.rootAssetPath, "unsupported_action", "不支持的 Agent authoring action。");
            }
        }

        AgentAuthoringResponse BootstrapAIController(AgentAuthoringRequest request)
        {
            if (!string.Equals(request.domain, AgentAuthoringSchema.AIControllerDomain, StringComparison.Ordinal))
                return Failure(request.action, request.domain, request.rootAssetPath, "bootstrap_domain_invalid", "bootstrap_ai_controller 只允许 AIController domain。");

            var report = new AgentCompileReport
            {
                success = true,
                domain = request.domain,
                rootIdentity = request.rootAssetPath
            };
            if (!AgentAuthoringJsonUtility.TryFromJson(request.patchJson, out AgentAIControllerBootstrapRequest bootstrap, report, "bootstrap-json"))
                return FromReport(request, report, false, "bootstrap_parse_failed", "AI Controller bootstrap JSON 解析失败。");
            if (!string.Equals(bootstrap.schemaVersion, AgentAuthoringSchema.Version, StringComparison.Ordinal))
                report.Error("bootstrap.schemaVersion", "unsupported_schema_version", $"AI bootstrap schema 必须是 {AgentAuthoringSchema.Version}。");
            if (string.IsNullOrWhiteSpace(bootstrap.controllerId) || !string.Equals(bootstrap.controllerId, bootstrap.controllerId.Trim(), StringComparison.Ordinal))
                report.Error("bootstrap.controllerId", "ai_controller_id_invalid", "ControllerId 缺失或包含首尾空白。");

            string[] targets = { request.rootAssetPath, bootstrap.rootTreeAssetPath, bootstrap.perceptionProfileAssetPath };
            var uniqueTargets = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < targets.Length; i++)
            {
                string path = targets[i];
                if (!IsCreatableAssetPath(path))
                    report.Error($"bootstrap.assetPaths[{i}]", "ai_bootstrap_asset_path_invalid", $"目标必须是父目录已存在的精确 Assets/... .asset 路径：{path}");
                else if (!uniqueTargets.Add(path))
                    report.Error($"bootstrap.assetPaths[{i}]", "ai_bootstrap_asset_path_duplicate", $"AI bootstrap 目标路径重复：{path}");
                else if (AssetDatabase.LoadMainAssetAtPath(path))
                    report.Error($"bootstrap.assetPaths[{i}]", "ai_bootstrap_asset_exists", $"AI bootstrap 不覆盖现有资产：{path}");
            }

            if (!TryLoadRoot(bootstrap.controlledCharacterAssetPath, out CharacterPipelineDefinition character, out string code, out string message))
                report.Error("bootstrap.controlledCharacter", code, message);
            else if (!character.SimulationProgram)
                report.Error("bootstrap.controlledCharacter", "controlled_character_program_missing", "Controlled Character 缺少正式 Simulation Program。");
            if (report.HasErrors())
                return FromReport(request, report, false, "ai_bootstrap_invalid", "AI Controller bootstrap 输入无效。");

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            const string undoName = "Bootstrap BTSMTL Agent AI Controller";
            Undo.SetCurrentGroupName(undoName);
            var createdPaths = new List<string>(3);
            try
            {
                var rootTree = new AIControllerTree { name = Path.GetFileNameWithoutExtension(bootstrap.rootTreeAssetPath) };
                rootTree.CheckInit();
                var treeAsset = ScriptableObject.CreateInstance<BaseTreeAsset>();
                treeAsset.name = Path.GetFileNameWithoutExtension(bootstrap.rootTreeAssetPath);
                treeAsset.SetTree(rootTree);

                var perception = ScriptableObject.CreateInstance<AIPerceptionProfile>();
                perception.name = Path.GetFileNameWithoutExtension(bootstrap.perceptionProfileAssetPath);
                perception.ConfigureAuthoring(Array.Empty<string>(), AICandidateOrdering.DistanceThenActorId);

                var definition = ScriptableObject.CreateInstance<AIControllerDefinition>();
                definition.name = Path.GetFileNameWithoutExtension(request.rootAssetPath);
                definition.ConfigureAuthoring(bootstrap.controllerId, treeAsset, character, perception);

                AssetDatabase.CreateAsset(treeAsset, bootstrap.rootTreeAssetPath);
                createdPaths.Add(bootstrap.rootTreeAssetPath);
                AssetDatabase.CreateAsset(perception, bootstrap.perceptionProfileAssetPath);
                createdPaths.Add(bootstrap.perceptionProfileAssetPath);
                AssetDatabase.CreateAsset(definition, request.rootAssetPath);
                createdPaths.Add(request.rootAssetPath);
                Undo.RegisterCreatedObjectUndo(treeAsset, undoName);
                Undo.RegisterCreatedObjectUndo(perception, undoName);
                Undo.RegisterCreatedObjectUndo(definition, undoName);
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);

                report.applied = true;
                report.success = true;
                report.Info("bootstrap", "ai_controller_bootstrapped", $"已创建 AI Controller、RootTree 与 Perception：{bootstrap.controllerId}");
                return new AgentAuthoringResponse
                {
                    action = AgentAuthoringActionUtility.ToProtocolValue(request.action),
                    domain = request.domain,
                    rootAssetPath = request.rootAssetPath,
                    success = true,
                    applied = true,
                    saved = true,
                    snapshot = new AgentAIControllerSnapshotExporter().Export(definition, AgentSnapshotExportMode.Full),
                    report = report
                };
            }
            catch (Exception exception)
            {
                for (int i = createdPaths.Count - 1; i >= 0; i--)
                    AssetDatabase.DeleteAsset(createdPaths[i]);
                AssetDatabase.SaveAssets();
                report.Error("bootstrap", "ai_bootstrap_exception", exception.Message);
                return FromReport(request, report, false, "ai_bootstrap_exception", "AI Controller bootstrap 失败，已删除本次创建的资产。");
            }
        }

        AgentAuthoringResponse ExecuteAI(AgentAuthoringRequest request, AIControllerDefinition definition)
        {
            switch (request.action)
            {
                case AgentAuthoringAction.ExportSnapshot:
                    return new AgentAuthoringResponse
                    {
                        action = AgentAuthoringActionUtility.ToProtocolValue(request.action),
                        domain = request.domain,
                        rootAssetPath = request.rootAssetPath,
                        success = true,
                        snapshot = new AgentAIControllerSnapshotExporter().Export(definition, AgentSnapshotExportMode.Full)
                    };
                case AgentAuthoringAction.DryRunPatch:
                    return ExecuteAIPatch(request, definition, false);
                case AgentAuthoringAction.ApplyPatch:
                    return ExecuteAIPatch(request, definition, true);
                case AgentAuthoringAction.Validate:
                    return FromReport(request, new AgentAIControllerValidator().Validate(definition), false);
                default:
                    return Failure(request.action, request.domain, request.rootAssetPath, "unsupported_action", "不支持的 Agent authoring action。");
            }
        }

        AgentAuthoringResponse ExportSnapshot(AgentAuthoringRequest request, CharacterPipelineDefinition definition)
        {
            return new AgentAuthoringResponse
            {
                action = AgentAuthoringActionUtility.ToProtocolValue(request.action),
                domain = request.domain,
                rootAssetPath = request.rootAssetPath,
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
            var parseReport = new AgentCompileReport
            {
                success = true,
                domain = request.domain,
                rootIdentity = AssetDatabase.AssetPathToGUID(request.rootAssetPath)
            };
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
                applied ??= new AgentCompileReport
                {
                    success = false,
                    domain = request.domain,
                    rootIdentity = AssetDatabase.AssetPathToGUID(request.rootAssetPath)
                };
                applied.Error("apply", "apply_exception", exception.ToString());
                bool rolledBack = Rollback(undoGroup, applied);
                return FromReport(
                    request,
                    applied,
                    false,
                    rolledBack ? FirstErrorCode(applied, "apply_exception") : "rollback_failed",
                    rolledBack ? "Patch apply 发生异常，本次修改已回滚。" : "Patch apply 发生异常，并且本次 Undo 事务无法完整回滚。");
            }
        }

        AgentAuthoringResponse ExecuteAIPatch(AgentAuthoringRequest request, AIControllerDefinition definition, bool apply)
        {
            var parseReport = new AgentCompileReport { success = true, domain = request.domain, rootIdentity = definition.ControllerId };
            if (!AgentAuthoringJsonUtility.TryFromJson(request.patchJson, out AgentPatchIR patch, parseReport, "patch-json"))
                return FromReport(request, parseReport, false, "patch_parse_failed", "Patch JSON 解析失败。");

            AgentGraphSnapshot snapshot = new AgentAIControllerSnapshotExporter().Export(definition, AgentSnapshotExportMode.Full);
            var compiler = new AgentPatchCompiler();
            AgentPatchPreparation preparation = compiler.Prepare(definition, snapshot, patch);
            if (!apply || !preparation.IsValid)
                return FromReport(request, preparation.Report, false);

            UnityEngine.Object[] owners = { definition, definition.RootTreeAsset, definition.PerceptionProfile };
            if (Array.Exists(owners, value => !value))
            {
                preparation.Report.Error("transaction", "ai_transaction_owner_missing", "AI Definition、RootTree 与 Perception Profile 必须同时存在并进入资产事务。");
                return FromReport(request, preparation.Report, false);
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            const string undoName = "Apply BTSMTL Agent AI Patch";
            Undo.SetCurrentGroupName(undoName);
            AgentCompileReport applied = null;
            try
            {
                Undo.RegisterCompleteObjectUndo(owners, undoName);
                AgentPatchApplyResult applyResult = compiler.Apply(definition, preparation);
                applied = applyResult.Report;
                if (!applied.HasErrors())
                    AppendValidation(applied, new AgentAIControllerValidator().Validate(definition));
                if (applied.HasErrors())
                {
                    bool rolledBack = Rollback(undoGroup, applied);
                    return FromReport(request, applied, false,
                        rolledBack ? FirstErrorCode(applied, "apply_failed") : "rollback_failed",
                        rolledBack ? "AI Patch apply 或 validation 失败，本次修改已回滚。" : "AI Patch apply 失败，并且 Undo 事务无法完整回滚。");
                }
                MarkTouchedOwnersDirty(applyResult.TouchedOwners);
                AssetDatabase.SaveAssets();
                AIIntentProgramBuildService.CompileAndPublish(definition);
                Undo.CollapseUndoOperations(undoGroup);
                applied.applied = true;
                applied.success = true;
                return FromReport(request, applied, true);
            }
            catch (Exception exception)
            {
                applied ??= new AgentCompileReport { success = false, domain = request.domain, rootIdentity = definition.ControllerId };
                applied.Error("apply", "apply_exception", exception.ToString());
                bool rolledBack = Rollback(undoGroup, applied);
                return FromReport(request, applied, false,
                    rolledBack ? FirstErrorCode(applied, "apply_exception") : "rollback_failed",
                    rolledBack ? "AI Patch apply 发生异常，本次修改已回滚。" : "AI Patch apply 发生异常，并且 Undo 事务无法完整回滚。");
            }
        }

        static bool TryLoadRoot<T>(
            string path,
            out T definition,
            out string errorCode,
            out string errorMessage) where T : UnityEngine.Object
        {
            definition = null;
            errorCode = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("\\") || path.Contains("/../"))
            {
                errorCode = "definition_path_invalid";
                errorMessage = "root_asset_path 必须是精确的 Assets/... 项目资产路径。";
                return false;
            }

            definition = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!definition || !string.Equals(AssetDatabase.GetAssetPath(definition), path, StringComparison.Ordinal))
            {
                errorCode = "definition_not_found";
                errorMessage = $"无法在指定路径加载 {typeof(T).Name}：{path}";
                return false;
            }

            return true;
        }

        static bool IsCreatableAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("\\") || path.Contains("/../") || !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                return false;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            return !string.IsNullOrEmpty(parent) && AssetDatabase.IsValidFolder(parent);
        }

        static void PrepareGraphReferences(BaseTree rootTree)
        {
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
            string domain,
            string rootAssetPath,
            string errorCode,
            string errorMessage)
        {
            var report = new AgentCompileReport { success = false, domain = domain ?? string.Empty };
            report.Error("bridge", errorCode, errorMessage);
            return new AgentAuthoringResponse
            {
                action = AgentAuthoringActionUtility.ToProtocolValue(action),
                domain = domain ?? string.Empty,
                rootAssetPath = rootAssetPath ?? string.Empty,
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
                domain = request.domain,
                rootAssetPath = request.rootAssetPath,
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

    public sealed class AgentAIControllerSnapshotExporter
    {
        public AgentGraphSnapshot Export(AIControllerDefinition definition, AgentSnapshotExportMode mode)
        {
            string definitionPath = definition ? AssetDatabase.GetAssetPath(definition) : string.Empty;
            var snapshot = new AgentGraphSnapshot
            {
                domain = AgentAuthoringSchema.AIControllerDomain,
                exportMode = mode.ToString(),
                definitionName = definition ? definition.name : string.Empty,
                definitionAssetPath = definitionPath,
                rootAssetPath = definitionPath,
                rootIdentity = definition ? definition.ControllerId : string.Empty,
                rootTreeAssetPath = definition?.RootTreeAsset ? AssetDatabase.GetAssetPath(definition.RootTreeAsset) : string.Empty
            };
            if (!definition || definition.RootTreeAsset?.Tree is not AIControllerTree root)
                return snapshot;
            root.RebindReadOnlyViewReferences();
            snapshot.rootGraphAuthoringId = root.GraphAuthoringId;
            snapshot.aiController = ExportController(definition, root);
            snapshot.sourceRevision = snapshot.aiController.sourceRevision;
            ExportGraph(root, mode, snapshot);
            return snapshot;
        }

        static AgentSnapshotAIController ExportController(AIControllerDefinition definition, AIControllerTree root)
        {
            string definitionPath = AssetDatabase.GetAssetPath(definition);
            string treePath = AssetDatabase.GetAssetPath(definition.RootTreeAsset);
            string perceptionPath = definition.PerceptionProfile ? AssetDatabase.GetAssetPath(definition.PerceptionProfile) : string.Empty;
            string characterPath = definition.ControlledCharacter ? AssetDatabase.GetAssetPath(definition.ControlledCharacter) : string.Empty;
            var result = new AgentSnapshotAIController
            {
                controllerId = definition.ControllerId,
                definitionAssetPath = definitionPath,
                definitionAssetGuid = AssetDatabase.AssetPathToGUID(definitionPath),
                treeAssetPath = treePath,
                treeAssetGuid = AssetDatabase.AssetPathToGUID(treePath),
                graphAuthoringId = root.GraphAuthoringId,
                authoringRole = root.AuthoringRole.ToString(),
                perceptionAssetPath = perceptionPath,
                perceptionAssetGuid = AssetDatabase.AssetPathToGUID(perceptionPath),
                candidateOrdering = definition.PerceptionProfile ? definition.PerceptionProfile.Ordering.ToString() : string.Empty,
                controlledCharacterAssetPath = characterPath,
                controlledCharacterAssetGuid = AssetDatabase.AssetPathToGUID(characterPath)
            };
            if (definition.PerceptionProfile)
                result.candidateActorIds.AddRange(definition.PerceptionProfile.CandidateActorIds);
            if (definition.ControlledCharacter?.SimulationProgram && definition.PerceptionProfile)
            {
                CharacterSimulationProgram characterProgram = definition.ControlledCharacter.SimulationProgram.Load();
                result.characterProgramId = characterProgram.Manifest.ProgramId.Value;
                result.characterProgramHash = characterProgram.ProgramHash.ToString();
                ExportInputCatalog(characterProgram, result);
                var candidates = result.candidateActorIds.Select(value => new ActorId(value)).ToArray();
                var perception = new AIPerceptionDescriptor(candidates, definition.PerceptionProfile.Ordering == AICandidateOrdering.DistanceThenActorId);
                result.sourceRevision = AIControllerSourceRevision.Compute(definition, characterProgram.Manifest.ProgramId, characterProgram.ProgramHash, perception.SchemaHash);
            }
            for (int i = 0; i < root.ExposedProperties.Count; i++)
            {
                BaseExposedProperty declaration = root.ExposedProperties[i];
                if (declaration == null)
                    continue;
                result.blackboardDeclarations.Add(new AgentSnapshotAIBlackboardDeclaration
                {
                    declarationAuthoringId = declaration.DeclarationId,
                    ownerGraphAuthoringId = declaration.DeclarationOwnerId,
                    displayName = declaration.BlackboardKey,
                    valueType = declaration.ValueType?.FullName ?? string.Empty,
                    scope = declaration.BlackboardScope.ToString(),
                    lifetime = declaration.BlackboardLifetime.ToString(),
                    authority = declaration.BlackboardAuthority.ToString(),
                    syncPolicy = declaration.BlackboardSyncPolicy.ToString(),
                    defaultValue = Convert.ToString(declaration.GetValue(), System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
                });
            }
            for (int i = 0; i < root.Nodes.Count; i++)
            {
                BaseNode node = root.Nodes[i];
                if (node == null)
                    continue;
                NodeAuthoringCapabilityPolicy.TryGetCapability(node.GetType(), out NodeAuthoringCapability capability);
                var entry = new AgentSnapshotAINode
                {
                    graphAuthoringId = root.GraphAuthoringId,
                    nodeAuthoringId = node.GUID,
                    nodeType = node.GetType().FullName,
                    capability = capability.ToString()
                };
                switch (node)
                {
                    case ReadAIMemoryNode read:
                        entry.memoryDeclarationAuthoringId = read.BlackboardVariable.DeclarationId;
                        entry.memoryValueKind = read.ValueKind.ToString();
                        break;
                    case WriteAIMemoryNode write:
                        entry.memoryDeclarationAuthoringId = write.BlackboardVariable.DeclarationId;
                        entry.memoryValueKind = write.ValueKind.ToString();
                        break;
                    case WriteContinuousInputNode continuous:
                        entry.inputId = continuous.InputId;
                        break;
                    case WriteActionTargetSnapshotNode target:
                        entry.inputId = target.InputId;
                        break;
                    case SubmitActionRequestNode request:
                        entry.requestId = request.RequestId;
                        entry.requestBufferSeconds = request.BufferSeconds;
                        entry.requestPriority = request.Priority;
                        entry.requestRepeatPolicy = request.RepeatPolicy.ToString();
                        break;
                }
                result.nodes.Add(entry);
            }
            if (definition.IntentProgram)
            {
                string programPath = AssetDatabase.GetAssetPath(definition.IntentProgram);
                result.intentProgramAssetPath = programPath;
                result.intentProgramAssetGuid = AssetDatabase.AssetPathToGUID(programPath);
                result.intentProgramId = definition.IntentProgram.ProgramId;
                result.intentProgramHash = definition.IntentProgram.ProgramHash;
                result.intentProgramSourceRevision = definition.IntentProgram.SourceRevision;
                result.intentProgramStale = !AIIntentProgramBuildService.IsCurrent(definition, out _);
            }
            else
            {
                result.intentProgramStale = true;
            }
            return result;
        }

        static void ExportGraph(AIControllerTree root, AgentSnapshotExportMode mode, AgentGraphSnapshot snapshot)
        {
            ExportGraph(root, mode, snapshot, "root", AgentGraphOwnership.RootAsset, string.Empty,
                string.Empty, snapshot.rootTreeAssetPath, new HashSet<BaseTree>());
        }

        static void ExportGraph(
            BaseTree tree,
            AgentSnapshotExportMode mode,
            AgentGraphSnapshot snapshot,
            string path,
            AgentGraphOwnership ownership,
            string ownerElementAuthoringId,
            string referenceKey,
            string sharedAssetPath,
            HashSet<BaseTree> exported)
        {
            if (tree == null || !exported.Add(tree))
                return;

            tree.CheckInit();
            string kind = (tree is ConditionRuleGraph ? AgentGraphKind.ConditionRuleGraph : AgentGraphKind.BaseTree).ToString();
            var graph = new AgentSnapshotGraph
            {
                graphAuthoringId = tree.GraphAuthoringId,
                path = path,
                name = tree.name,
                kind = kind,
                ownership = ownership.ToString(),
                ownerElementAuthoringId = ownerElementAuthoringId,
                referenceKey = referenceKey,
                sharedAssetPath = sharedAssetPath
            };
            snapshot.graphs.Add(graph);
            snapshot.graphSummaries.Add(new AgentSnapshotGraphSummary
            {
                graphAuthoringId = tree.GraphAuthoringId,
                path = path,
                name = tree.name,
                kind = kind,
                ownership = ownership.ToString(),
                ownerNode = ownerElementAuthoringId,
                referenceKey = referenceKey
            });

            for (int i = 0; i < tree.Nodes.Count; i++)
            {
                BaseNode node = tree.Nodes[i];
                if (node == null)
                    continue;
                var entry = new AgentSnapshotNode
                {
                    elementAuthoringId = node.GUID,
                    typeName = node.GetType().FullName,
                    displayName = node.ResolvedDisplayName,
                    nodeTypeDisplayName = node.NodeTypeDisplayName,
                    position = new AgentSnapshotVector2 { x = node.Position.x, y = node.Position.y }
                };
                if (node is LoopNode loopNode)
                    entry.loopStopType = loopNode.LoopStopType.ToString();
                else if (node is CompareNode compareNode)
                    entry.compareType = compareNode.Comparison.ToString();
                if (mode == AgentSnapshotExportMode.Full)
                {
                    foreach (PropertyPort port in node.PropertyPortMap.Values.OrderBy(value => value.PortId, StringComparer.Ordinal))
                    {
                        entry.propertyPorts.Add(new AgentSnapshotPropertyPort
                        {
                            portId = port.PortId,
                            displayName = port.DisplayName,
                            direction = port.Direction.ToString(),
                            valueType = port.ValueType?.FullName ?? string.Empty
                        });
                    }
                }
                graph.nodes.Add(entry);
            }

            for (int i = 0; i < tree.Edges.Count; i++)
            {
                BaseEdge edge = tree.Edges[i];
                if (edge == null)
                    continue;

                ConditionRuleGraph condition = edge.ConditionRuleGraph;
                string conditionPath = condition
                    ? $"{path}/ConditionRule:Edge:{edge.GUID}/conditionRule/{condition.GraphAuthoringId}"
                    : string.Empty;
                graph.flowEdges.Add(new AgentSnapshotFlowEdge
                {
                    elementAuthoringId = edge.GUID,
                    startElementAuthoringId = edge.StartNodeGUID,
                    endElementAuthoringId = edge.EndNodeGUID,
                    startPort = edge.StartPortName,
                    endPort = edge.EndPortName,
                    flowOrder = edge.FlowOrder,
                    transitionPriority = edge.TransitionPriority,
                    abortPolicy = edge.AbortPolicy.ToString(),
                    conditionRuleGraphAuthoringId = condition ? condition.GraphAuthoringId : string.Empty,
                    conditionRuleGraphPath = conditionPath
                });
                if (condition)
                {
                    ExportGraph(condition, mode, snapshot, conditionPath, AgentGraphOwnership.Inline, edge.GUID,
                        "conditionRule", string.Empty, exported);
                }
            }

            for (int i = 0; i < tree.PropertyEdges.Count; i++)
            {
                PropertyEdge edge = tree.PropertyEdges[i];
                if (edge == null)
                    continue;
                graph.propertyEdges.Add(new AgentSnapshotPropertyEdge
                {
                    elementAuthoringId = edge.GUID,
                    startElementAuthoringId = edge.StartNodeGUID,
                    endElementAuthoringId = edge.EndNodeGUID,
                    startPortId = edge.StartPortName,
                    endPortId = edge.EndPortName
                });
            }
        }

        static void ExportInputCatalog(CharacterSimulationProgram program, AgentSnapshotAIController result)
        {
            foreach (ProgramCatalogEntry entry in program.CatalogEntries.OrderBy(value => value.Identity, StringComparer.Ordinal))
            {
                if (entry.Kind == ProgramCatalogEntryKind.InputValue)
                {
                    result.inputValues.Add(new AgentSnapshotInputValue
                    {
                        inputValueId = StripCatalogPrefix(entry.Identity, "input:value:"),
                        valueType = ReadCatalogEnum<ProgramInputValueKind>(program, entry, "ValueType").ToString()
                    });
                }
                else if (entry.Kind == ProgramCatalogEntryKind.InputRequest)
                {
                    result.actionRequests.Add(new AgentSnapshotActionRequest
                    {
                        requestId = StripCatalogPrefix(entry.Identity, "input:request:"),
                        timingClass = ReadCatalogEnum<CharacterActionRequestTimingClass>(program, entry, "TimingClass").ToString()
                    });
                }
            }
        }

        static T ReadCatalogEnum<T>(CharacterSimulationProgram program, ProgramCatalogEntry entry, string fieldName) where T : struct, Enum
        {
            for (int i = 0; i < entry.Fields.Count; i++)
            {
                ProgramCatalogField field = entry.Fields[i];
                if (!string.Equals(field.Name, fieldName, StringComparison.Ordinal) || field.Kind != ProgramCatalogFieldKind.Constant)
                    continue;
                ProgramConstant value = program.Constants[field.ConstantIndex];
                object candidate = Enum.ToObject(typeof(T), value.Int32);
                if (value.Kind == ProgramConstantKind.Int32 && Enum.IsDefined(typeof(T), candidate))
                    return (T)candidate;
            }
            throw new InvalidOperationException($"Catalog entry '{entry.Identity}' lacks {fieldName}.");
        }

        static string StripCatalogPrefix(string value, string prefix)
        {
            return value.StartsWith(prefix, StringComparison.Ordinal) ? value.Substring(prefix.Length) : value;
        }
    }
}
