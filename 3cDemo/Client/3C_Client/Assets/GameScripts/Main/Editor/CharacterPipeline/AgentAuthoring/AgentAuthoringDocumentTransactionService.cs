using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentAuthoringDocumentApplicationService
    {
        readonly AgentAuthoringDocumentStore m_Store = new AgentAuthoringDocumentStore();
        readonly AgentAuthoringDocumentExporter m_Exporter = new AgentAuthoringDocumentExporter();
        readonly AgentDocumentReconciler m_Reconciler = new AgentDocumentReconciler();

        public AgentAuthoringResponse Execute(AgentAuthoringRequest request)
        {
            if (request == null)
                return Failure(default, string.Empty, string.Empty, "request_missing", "Agent authoring request缺失。");
            if (!AgentAuthoringSchema.IsDomain(request.domain))
                return Failure(request.action, request.domain, request.rootAssetPath, "unsupported_domain", $"不支持的Agent authoring domain：{request.domain}");
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return Failure(request.action, request.domain, request.rootAssetPath, "editor_busy", "Unity Editor正在编译或更新AssetDatabase。");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return Failure(request.action, request.domain, request.rootAssetPath, "play_mode_active", "Play Mode或切换期间禁止执行authoring操作。");

            try
            {
                if (string.Equals(request.domain, AgentAuthoringSchema.CharacterControllerDomain, StringComparison.Ordinal))
                {
                    if (!TryLoadRoot(request.rootAssetPath, out CharacterPipelineDefinition definition, out string code, out string message))
                        return Failure(request.action, request.domain, request.rootAssetPath, code, message);
                    SimulationSessionCompositionDefinition composition = null;
                    if (request.action == AgentAuthoringAction.Validate &&
                        !string.IsNullOrWhiteSpace(request.compositionAssetPath) &&
                        !TryLoadExactAsset(
                            request.compositionAssetPath,
                            "composition",
                            out composition,
                            out code,
                            out message))
                    {
                        return Failure(request.action, request.domain, request.compositionAssetPath, code, message);
                    }
                    return ExecuteCharacter(request, definition, composition);
                }
                return Failure(
                    request.action,
                    request.domain,
                    request.rootAssetPath,
                    "unsupported_domain",
                    "Agent Document只服务CharacterController；Behavior Designer行为不进入BTSMTL Document。");
            }
            catch (AgentAuthoringOperationException exception)
            {
                return Failure(
                    request.action,
                    request.domain,
                    request.rootAssetPath,
                    exception.Code,
                    exception.Message,
                    exception.Path,
                    exception.Suggestion);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return Failure(request.action, request.domain, request.rootAssetPath, "document_operation_exception", exception.Message);
            }
        }

        AgentAuthoringResponse ExecuteCharacter(
            AgentAuthoringRequest request,
            CharacterPipelineDefinition definition,
            SimulationSessionCompositionDefinition composition)
        {
            AgentAuthoringPackageProjection projection = m_Exporter.Export(definition);
            return request.action switch
            {
                AgentAuthoringAction.CheckoutDocument => Checkout(request, projection),
                AgentAuthoringAction.RebaseDocument => Rebase(request, projection),
                AgentAuthoringAction.DryRunDocument => DryRun(request, projection, definition),
                AgentAuthoringAction.ApplyDocument => Apply(request, projection, definition),
                AgentAuthoringAction.Validate => FromReport(request, new AgentCharacterAuthoringValidator().Validate(definition, composition), projection, null),
                _ => Failure(request.action, request.domain, request.rootAssetPath, "unsupported_action", "不支持的Agent Document action。")
            };
        }

        AgentAuthoringResponse Checkout(AgentAuthoringRequest request, AgentAuthoringPackageProjection projection)
        {
            string path = m_Store.GetPackagePath(request.domain, request.rootAssetPath, projection.Target.rootIdentity);
            AgentCompileReport report = CreateReport(request, projection.Target.rootIdentity);
            if (!m_Store.Exists(path))
                return WriteProjection(request, projection, path, report);
            if (m_Store.RequiresCheckout(path))
                return WriteProjection(request, projection, path, report);
            if (!TryLoadState(request, projection, path, report, out AgentAuthoringPackageState state))
                return FromReport(request, report, projection, path);
            if (state.SyncState == AgentDocumentSyncState.Clean || state.SyncState == AgentDocumentSyncState.TreeDirty)
                return WriteProjection(request, projection, path, report);
            if (state.SyncState == AgentDocumentSyncState.Conflict)
                report.Warning("document.sync", "document_conflict", "Unity authoring/context与Document editable均已变化，checkout不会覆盖任一侧。");
            return Success(request, projection, path, state.SyncState, state.DocumentHash, report, state.EditableHash, state.ContextHash);
        }

        AgentAuthoringResponse Rebase(AgentAuthoringRequest request, AgentAuthoringPackageProjection projection)
        {
            string path = m_Store.GetPackagePath(request.domain, request.rootAssetPath, projection.Target.rootIdentity);
            AgentCompileReport report = CreateReport(request, projection.Target.rootIdentity);
            if (!request.confirmRebase)
            {
                report.Error("document.sync", "rebase_confirmation_required", "rebase_document必须显式提交confirm_rebase=true。");
                return FromReport(request, report, projection, path);
            }
            if (!TryLoadState(request, projection, path, report, out AgentAuthoringPackageState state))
                return FromReport(request, report, projection, path);

            AgentAuthoringTarget rebased = new AgentAuthoringTarget
            {
                domain = state.Target.domain,
                rootIdentity = state.Target.rootIdentity,
                editable = state.Target.editable,
                context = projection.Target.context
            };
            AgentAuthoringPackageSync sync = CreateSync(request, projection);
            string documentHash = m_Store.Write(
                path,
                rebased,
                projection.Snapshot,
                sync,
                report,
                false,
                out string editableHash,
                out string contextHash);
            AgentDocumentSyncState syncState = string.Equals(editableHash, projection.EditableHash, StringComparison.Ordinal)
                ? AgentDocumentSyncState.Clean
                : AgentDocumentSyncState.DocumentDirty;
            report.Info("document.sync", "document_rebased", "Document已接受当前Unity authoring/context为新基线，editable目标正文保持不变。");
            return Success(request, projection, path, syncState, documentHash, report, editableHash, contextHash);
        }

        AgentAuthoringResponse DryRun(
            AgentAuthoringRequest request,
            AgentAuthoringPackageProjection projection,
            CharacterPipelineDefinition character)
        {
            string path = m_Store.GetPackagePath(request.domain, request.rootAssetPath, projection.Target.rootIdentity);
            AgentCompileReport report = CreateReport(request, projection.Target.rootIdentity);
            if (!TryLoadState(request, projection, path, report, out AgentAuthoringPackageState state))
                return FromReport(request, report, projection, path);
            if (state.SyncState == AgentDocumentSyncState.TreeDirty || state.SyncState == AgentDocumentSyncState.Conflict)
            {
                report.Error("document.sync", "document_sync_state_blocked", $"同步状态{state.SyncState}禁止dry-run，请先checkout或rebase。");
                return FromReport(request, report, projection, path, state);
            }

            AgentDocumentPreparation preparation = m_Reconciler.Prepare(character, projection.Snapshot, state.Target);
            AgentAuthoringResponse response = FromReport(request, preparation.Report, projection, path, state);
            response.documentHash = state.DocumentHash;
            response.planHash = AgentAuthoringDocumentCodec.Hash(preparation.Report.plannedDiff);
            response.syncState = state.SyncState.ToString();
            return response;
        }

        AgentAuthoringResponse Apply(
            AgentAuthoringRequest request,
            AgentAuthoringPackageProjection projection,
            CharacterPipelineDefinition character)
        {
            string path = m_Store.GetPackagePath(request.domain, request.rootAssetPath, projection.Target.rootIdentity);
            AgentCompileReport report = CreateReport(request, projection.Target.rootIdentity);
            if (string.IsNullOrWhiteSpace(request.expectedDocumentHash))
            {
                report.Error("document", "expected_document_hash_required", "apply_document必须提交dry-run返回的expected_document_hash。");
                return FromReport(request, report, projection, path);
            }
            if (!TryLoadState(request, projection, path, report, out AgentAuthoringPackageState state))
                return FromReport(request, report, projection, path);
            if (!string.Equals(request.expectedDocumentHash, state.DocumentHash, StringComparison.Ordinal))
            {
                report.Error("document", "document_hash_changed", "Document package semantic hash与dry-run结果不一致，必须重新dry-run。");
                return FromReport(request, report, projection, path, state);
            }
            if (state.SyncState == AgentDocumentSyncState.TreeDirty || state.SyncState == AgentDocumentSyncState.Conflict)
            {
                report.Error("document.sync", "document_sync_state_blocked", $"同步状态{state.SyncState}禁止apply。");
                return FromReport(request, report, projection, path, state);
            }

            AgentDocumentPreparation preparation = m_Reconciler.Prepare(character, projection.Snapshot, state.Target);
            if (!preparation.IsValid)
                return FromReport(request, preparation.Report, projection, path, state);
            if (!TryCollectOwners(
                    character,
                    preparation,
                    preparation.Report,
                    out UnityEngine.Object[] owners))
                return FromReport(request, preparation.Report, projection, path, state);

            var createdSkillRootPaths = new List<string>();
            if (character)
            {
                foreach (AgentSetSkillFlowDocumentMutation mutation in preparation.Plan.Commands
                             .OfType<AgentSetSkillFlowDocumentMutation>())
                    createdSkillRootPaths.AddRange(AgentSkillFlowAssetPaths.PlannedRoots(character, mutation.Document));
            }
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            const string undoName = "Apply BTSMTL Agent Document";
            Undo.SetCurrentGroupName(undoName);
            AgentCompileReport applied = null;
            AgentDocumentApplyResult applyResult = null;
            try
            {
                Undo.RegisterCompleteObjectUndo(owners, undoName);
                var compiler = new AgentDocumentMutationCompiler();
                applyResult = compiler.Apply(character, preparation);
                applied = applyResult.Report;
                AgentAuthoringPackageProjection appliedProjection = null;
                if (!applied.HasErrors() && character)
                    ApplyPresentation(preparation.PresentationPlan, applied);
                if (!applied.HasErrors())
                {
                    AppendValidation(
                        applied,
                        new AgentCharacterAuthoringValidator().Validate(character, false));
                }
                if (!applied.HasErrors() && character)
                    AppendPresentationValidation(
                        applied,
                        preparation.PresentationPlan);
                if (applied.HasErrors())
                    return RollbackResponse(request, projection, path, state, undoGroup, applied, applyResult, "document_apply_failed", createdSkillRootPaths);

                MarkTouchedOwnersDirty(applyResult.TouchedOwners);
                MarkPresentationOwnersDirty(preparation.PresentationPlan);
                RecordTouchedOwners(
                    applied,
                    applyResult.TouchedOwners,
                    preparation.PresentationPlan);
                AssetDatabase.SaveAssets();
                appliedProjection = m_Exporter.Export(character);
                AgentAuthoringPackageProjection finalProjection = appliedProjection;
                AgentAuthoringPackageSync finalSync = CreateSync(request, finalProjection);
                string finalDocumentHash = m_Store.Write(
                    path,
                    finalProjection.Target,
                    finalProjection.Snapshot,
                    finalSync,
                    applied,
                    false,
                    out string finalEditableHash,
                    out string finalContextHash);
                Undo.CollapseUndoOperations(undoGroup);
                applied.applied = true;
                applied.success = true;
                AgentAuthoringResponse response = FromReport(request, applied, finalProjection, path);
                response.applied = true;
                response.saved = true;
                response.syncState = AgentDocumentSyncState.Clean.ToString();
                response.editableHash = finalEditableHash;
                response.contextHash = finalContextHash;
                response.documentHash = finalDocumentHash;
                response.planHash = AgentAuthoringDocumentCodec.Hash(preparation.Report.plannedDiff);
                return response;
            }
            catch (Exception exception)
            {
                applied ??= CreateReport(request, projection.Target.rootIdentity);
                applied.Error("apply", "apply_exception", exception.ToString());
                return RollbackResponse(request, projection, path, state, undoGroup, applied, applyResult, "apply_exception", createdSkillRootPaths);
            }
        }

        AgentAuthoringResponse WriteProjection(
            AgentAuthoringRequest request,
            AgentAuthoringPackageProjection projection,
            string path,
            AgentCompileReport report)
        {
            AgentAuthoringPackageSync sync = CreateSync(request, projection);
            string documentHash = m_Store.Write(
                path,
                projection.Target,
                projection.Snapshot,
                sync,
                report,
                true,
                out string editableHash,
                out string contextHash);
            return Success(request, projection, path, AgentDocumentSyncState.Clean, documentHash, report, editableHash, contextHash);
        }

        bool TryLoadState(
            AgentAuthoringRequest request,
            AgentAuthoringPackageProjection projection,
            string path,
            AgentCompileReport report,
            out AgentAuthoringPackageState state)
        {
            state = null;
            if (!m_Store.TryRead(
                    path,
                    request.domain,
                    projection.Target.rootIdentity,
                    request.rootAssetPath,
                    projection.Snapshot,
                    report,
                    out AgentAuthoringTarget target,
                    out AgentAuthoringPackageSync sync,
                    out string editableHash,
                    out string contextHash,
                    out string documentHash))
                return false;
            if (!string.Equals(contextHash, sync.baseContextHash, StringComparison.Ordinal))
            {
                report.Error("document.context", "readonly_context_modified", "Document只读context与service基线不一致。");
                return false;
            }

            bool unityChanged =
                !string.Equals(projection.EditableHash, sync.baseEditableHash, StringComparison.Ordinal) ||
                !string.Equals(projection.ContextHash, sync.baseContextHash, StringComparison.Ordinal);
            bool documentChanged = !string.Equals(editableHash, sync.baseEditableHash, StringComparison.Ordinal);
            AgentDocumentSyncState syncState = unityChanged
                ? documentChanged ? AgentDocumentSyncState.Conflict : AgentDocumentSyncState.TreeDirty
                : documentChanged ? AgentDocumentSyncState.DocumentDirty : AgentDocumentSyncState.Clean;
            state = new AgentAuthoringPackageState(path, target, sync, editableHash, contextHash, documentHash, syncState);
            return true;
        }

        static AgentAuthoringPackageSync CreateSync(AgentAuthoringRequest request, AgentAuthoringPackageProjection projection)
        {
            return new AgentAuthoringPackageSync
            {
                domain = request.domain,
                rootIdentity = projection.Target.rootIdentity,
                rootAssetPath = request.rootAssetPath,
                baseSourceRevision = projection.SourceRevision,
                baseEditableHash = projection.EditableHash,
                baseContextHash = projection.ContextHash
            };
        }

        static bool TryCollectOwners(
            CharacterPipelineDefinition character,
            AgentDocumentPreparation preparation,
            AgentCompileReport report,
            out UnityEngine.Object[] owners)
        {
            if (character)
            {
                if (!new AgentCharacterTransactionOwnerCollector().TryCollect(
                        character,
                        out UnityEngine.Object[] graphOwners,
                        out string code,
                        out string message))
                    return FailOwner(report, code, message, out owners);
                var allOwners = new HashSet<UnityEngine.Object>(graphOwners);
                AgentSetSkillFlowDocumentMutation skillFlow = preparation?.Plan?.Commands
                    .OfType<AgentSetSkillFlowDocumentMutation>()
                    .FirstOrDefault();
                if (skillFlow != null &&
                    !AgentSkillFlowDocumentOwnerCollector.TryCollect(
                        character,
                        skillFlow.Document,
                        allOwners,
                        report))
                {
                    owners = Array.Empty<UnityEngine.Object>();
                    return false;
                }
                AgentPresentationMutationPlan presentation =
                    preparation?.PresentationPlan;
                if (presentation != null)
                {
                    if (!TryAddPersistentOwner(
                            presentation.Profile,
                            allOwners,
                            report,
                            "presentation_profile") ||
                        !TryAddPersistentOwner(
                            presentation.PoseGraph,
                            allOwners,
                            report,
                            "presentation_pose_graph"))
                    {
                        owners = Array.Empty<UnityEngine.Object>();
                        return false;
                    }
                    foreach (UnityEngine.Object linkedOwner in presentation.Profile
                                 .LinkedPoseImplementations
                                 .Where(value => value)
                                 .Cast<UnityEngine.Object>()
                                 .Concat(presentation.Profile.LinkedPoseSelectors
                                     .Where(value => value))
                                 .Concat(presentation.Profile.LinkedPoseImplementations
                                     .Where(value => value)
                                     .SelectMany(value => value.Entries)
                                     .Where(value => value != null && value.GraphOwner)
                                     .Select(value =>
                                         (UnityEngine.Object)value.GraphOwner)
                                     .Distinct()))
                    {
                        if (!TryAddPersistentOwner(
                                linkedOwner,
                                allOwners,
                                report,
                                "presentation_linked_pose"))
                        {
                            owners = Array.Empty<UnityEngine.Object>();
                            return false;
                        }
                    }
                    foreach (AgentLinkedPoseGraphMutationPlan linked in
                             presentation.LinkedPoseGraphs)
                    {
                        string linkedPath = linked.GraphOwner
                            ? AssetDatabase.GetAssetPath(linked.GraphOwner)
                            : string.Empty;
                        if (!string.IsNullOrWhiteSpace(linkedPath) &&
                            !TryAddPersistentOwner(
                                linked.GraphOwner,
                                allOwners,
                                report,
                                "presentation_linked_pose_graph"))
                        {
                            owners = Array.Empty<UnityEngine.Object>();
                            return false;
                        }
                    }
                    foreach (AgentAnimationClipCurveMutationPlan clip in presentation.AnimationClips)
                    {
                        if (!TryAddPersistentOwner(
                                clip.Clip,
                                allOwners,
                                report,
                                "presentation_animation_clip"))
                        {
                            owners = Array.Empty<UnityEngine.Object>();
                            return false;
                        }
                    }
                }
                owners = allOwners.ToArray();
                return true;
            }
            return FailOwner(report, "character_transaction_owner_missing", "Character Definition缺失，无法建立事务owner。", out owners);
        }

        static bool FailOwner(AgentCompileReport report, string code, string message, out UnityEngine.Object[] owners)
        {
            owners = Array.Empty<UnityEngine.Object>();
            report.Error("transaction", code, message);
            return false;
        }

        static bool TryAddPersistentOwner(
            UnityEngine.Object owner,
            ISet<UnityEngine.Object> owners,
            AgentCompileReport report,
            string label)
        {
            string path = owner ? AssetDatabase.GetAssetPath(owner) : string.Empty;
            if (!owner || string.IsNullOrWhiteSpace(path))
            {
                report.Error(
                    "transaction." + label,
                    "presentation_transaction_owner_invalid",
                    $"Presentation事务owner '{label}'缺失或不是持久化资产。");
                return false;
            }
            owners.Add(owner);
            return true;
        }

        static void ApplyPresentation(
            AgentPresentationMutationPlan plan,
            AgentCompileReport report)
        {
            if (plan == null || plan.IsEmpty)
                return;
            ApplyAnimationClipCurves(plan.AnimationClips);
            var service = new CharacterPresentationMutationService();
            if (plan.GraphTransaction.Mutations.Count > 0)
            {
                service.ApplyWithoutUndo(
                    new CharacterPoseGraphAssetMutationOwner(
                        plan.PoseGraph,
                        plan.Profile),
                    plan.GraphTransaction);
                SaveCreatedSubassets(
                    plan.PoseGraph,
                    plan.GraphTransaction.Mutations
                        .OfType<CreatePoseSourceSlotMutation>()
                        .Select(value => (UnityEngine.Object)value.Slot));
            }
            foreach (AgentLinkedPoseGraphMutationPlan linked in
                     plan.LinkedPoseGraphs.Where(value =>
                         value.Transaction.Mutations.Count > 0))
            {
                service.ApplyWithoutUndo(
                    new CharacterPoseGraphAssetMutationOwner(
                        linked.GraphOwner,
                        plan.Profile),
                    linked.Transaction);
            }
            if (plan.ProfileTransaction.Mutations.Count > 0)
            {
                service.ApplyWithoutUndo(
                    new CharacterPresentationProfileMutationOwner(
                        plan.Profile,
                        plan.ProfileId),
                    plan.ProfileTransaction);
                SaveCreatedSubassets(
                    plan.Profile,
                    plan.ProfileTransaction.Mutations
                        .OfType<CreateProfileSourceBindingMutation>()
                        .Select(value =>
                            (UnityEngine.Object)value.Binding)
                        .Concat(plan.ProfileTransaction.Mutations
                            .OfType<CreateLinkedPoseImplementationMutation>()
                            .SelectMany(value => new UnityEngine.Object[]
                            {
                                value.Implementation,
                                value.GraphOwner
                            }))
                        .Concat(plan.ProfileTransaction.Mutations
                            .OfType<CreateLinkedPoseInterfaceMutation>()
                            .Select(value =>
                                (UnityEngine.Object)value.Interface))
                        .Concat(plan.ProfileTransaction.Mutations
                            .OfType<CreateEquipmentLinkedPoseSelectorMutation>()
                            .Select(value =>
                                 (UnityEngine.Object)value.Selector)));
            }
            if (plan.SetLocomotionSyncGroups)
            {
                plan.Profile.SetLocomotionSyncGroups(plan.LocomotionSyncGroups);
                EditorUtility.SetDirty(plan.Profile);
            }
            RequirePresentationPlanApplied(plan);
            foreach (AgentCompileDiffEntry diff in report.plannedDiff.Where(
                         value => value.mutationId?.StartsWith(
                             "presentation-",
                             StringComparison.Ordinal) == true))
            {
                report.appliedDiff.Add(new AgentCompileDiffEntry
                {
                    mutationId = diff.mutationId,
                    action = diff.action,
                    graph = diff.graph,
                    target = diff.target,
                    detail = diff.detail
                });
            }
        }

        static void ApplyAnimationClipCurves(
            IReadOnlyList<AgentAnimationClipCurveMutationPlan> plans)
        {
            for (int planIndex = 0; planIndex < plans.Count; planIndex++)
            {
                AgentAnimationClipCurveMutationPlan plan = plans[planIndex];
                CharacterAnimationClipContentIdentity identity =
                    CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(plan.Clip);
                if (!string.Equals(
                        identity.FullDependencyHash,
                        plan.Target.dependencyBaseline,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        identity.AnalysisInputHash,
                        plan.Target.analysisInputHash,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        identity.RegisteredCurveHash,
                        plan.Target.registeredCurveHash,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"AnimationClip '{plan.Clip.name}' dependency changed after preflight.");
                }
                var footMotionCurves = new Dictionary<string, AnimationCurve>(StringComparer.Ordinal);
                for (int curveIndex = 0; curveIndex < plan.Target.curves.Count; curveIndex++)
                {
                    AgentPackageCurve curve = plan.Target.curves[curveIndex];
                    CharacterAnimationClipRegisteredCurveDescriptor descriptor =
                        CharacterAnimationClipRegisteredCurveCatalog.Require(curve.channelId);
                    if (descriptor.FootMotionData)
                    {
                        footMotionCurves.Add(curve.channelId, ConvertCurve(curve));
                        continue;
                    }
                    CharacterAnimationClipRegisteredCurveCatalog.Replace(
                        plan.Clip,
                        curve.channelId,
                        ConvertCurve(curve));
                }
                if (footMotionCurves.Count > 0)
                    CharacterAnimationClipRegisteredCurveCatalog.ReplaceFootMotionGroup(
                        plan.Clip,
                        footMotionCurves);
                var targetChannels = new HashSet<string>(
                    plan.Target.curves.Select(value => value.channelId),
                    StringComparer.Ordinal);
                for (int channelIndex = 0;
                     channelIndex < CharacterAnimationClipRegisteredCurveCatalog.Channels.Count;
                     channelIndex++)
                {
                    string channelId = CharacterAnimationClipRegisteredCurveCatalog.Channels[channelIndex].ChannelId;
                    if (!targetChannels.Contains(channelId))
                        CharacterAnimationClipRegisteredCurveCatalog.Remove(plan.Clip, channelId);
                }
            }
        }

        static AnimationCurve ConvertCurve(AgentPackageCurve value)
        {
            var keys = value.keys.Select(key =>
            {
                var result = new Keyframe(
                    key.time,
                    key.value,
                    key.inTangent,
                    key.outTangent,
                    key.inWeight,
                    key.outWeight)
                {
                    weightedMode = Enum.Parse<WeightedMode>(
                        key.weightedMode,
                        false)
                };
                return result;
            }).ToArray();
            return new AnimationCurve(keys)
            {
                preWrapMode = Enum.Parse<WrapMode>(value.preWrapMode, false),
                postWrapMode = Enum.Parse<WrapMode>(value.postWrapMode, false)
            };
        }

        static void EnsureAssetFolder(string assetPath)
        {
            string folder = System.IO.Path.GetDirectoryName(assetPath)
                ?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(folder) ||
                AssetDatabase.IsValidFolder(folder))
                return;
            string parent = System.IO.Path.GetDirectoryName(folder)
                ?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(parent) ||
                !AssetDatabase.IsValidFolder(parent))
                throw new InvalidOperationException(
                    $"Asset parent folder is missing: {parent}");
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }

        static void SaveCreatedSubassets(
            UnityEngine.Object owner,
            IEnumerable<UnityEngine.Object> created)
        {
            UnityEngine.Object[] assets = created
                .Where(value => value)
                .ToArray();
            for (int i = 0; i < assets.Length; i++)
            {
                EditorUtility.SetDirty(assets[i]);
                AssetDatabase.SaveAssetIfDirty(assets[i]);
            }
            EditorUtility.SetDirty(owner);
            AssetDatabase.SaveAssetIfDirty(owner);
            foreach (UnityEngine.Object asset in assets)
            {
                if (!asset ||
                    !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                        asset,
                        out _,
                        out long localFileId) ||
                    localFileId == 0)
                {
                    throw new InvalidOperationException(
                        $"Created subasset '{asset?.name ?? "missing"}' did not receive a persistent object identity from '{owner.name}'.");
                }
            }
        }

        static void RequirePresentationPlanApplied(
            AgentPresentationMutationPlan plan)
        {
            if (plan.Profile.PoseGraph != plan.PoseGraph)
                throw new InvalidOperationException(
                    "Presentation Profile did not retain the reconciled Pose Graph owner.");
            Dictionary<string, CharacterPoseStateTransition> applied = plan.PoseGraph
                .EnumerateStateMachines()
                .SelectMany(value => value.Transitions)
                .ToDictionary(value => value.TransitionId.Value, StringComparer.Ordinal);
            foreach (CreatePoseTransitionMutation mutation in plan.GraphTransaction.Mutations
                         .OfType<CreatePoseTransitionMutation>())
            {
                if (!applied.TryGetValue(
                        mutation.Transition.TransitionId.Value,
                        out CharacterPoseStateTransition transition) ||
                    transition.BlendProfile != mutation.Transition.BlendProfile)
                {
                    throw new InvalidOperationException(
                        $"Pose Transition '{mutation.Transition.TransitionId}' did not retain its reconciled Blend Profile.");
                }
            }
            foreach (AgentLinkedPoseGraphMutationPlan linked in
                     plan.LinkedPoseGraphs)
            {
                if (!linked.Implementation || !linked.GraphOwner ||
                    !plan.Profile.LinkedPoseImplementations.Contains(
                        linked.Implementation))
                    throw new InvalidOperationException(
                        "Linked Pose Implementation plan was not attached to the Profile.");
                Dictionary<string, CharacterPoseStateTransition> transitions =
                    linked.GraphOwner.EnumerateStateMachines()
                        .SelectMany(value => value.Transitions)
                        .ToDictionary(
                            value => value.TransitionId.Value,
                            StringComparer.Ordinal);
                foreach (CreatePoseTransitionMutation mutation in
                         linked.Transaction.Mutations
                             .OfType<CreatePoseTransitionMutation>())
                {
                    if (!transitions.TryGetValue(
                            mutation.Transition.TransitionId.Value,
                            out CharacterPoseStateTransition transition) ||
                        transition.BlendProfile != mutation.Transition.BlendProfile)
                        throw new InvalidOperationException(
                            $"Linked Pose Transition '{mutation.Transition.TransitionId}' did not retain its reconciled Blend Profile.");
                }
            }
        }

        static void AppendPresentationValidation(
            AgentCompileReport report,
            AgentPresentationMutationPlan plan)
        {
            if (plan == null)
                return;
            IReadOnlyList<string> capabilityErrors =
                CharacterPoseGraphCapabilityValidator.Validate(plan.PoseGraph);
            for (int i = 0; i < capabilityErrors.Count; i++)
            {
                report.Error(
                    "editable/presentation/pose-graphs",
                    "presentation_capability_validation_failed",
                    capabilityErrors[i]);
            }
            CharacterPoseGraphValidationReport graphValidation =
                CharacterPoseTopologyValidator.Validate(
                    plan.PoseGraph,
                    plan.Profile.RigDefinition,
                    CharacterPoseAuthoringPortProjection.Get);
            for (int i = 0; i < graphValidation.Issues.Count; i++)
            {
                CharacterPoseGraphValidationIssue issue =
                    graphValidation.Issues[i];
                report.Error(
                    "editable/presentation/pose-graphs/" +
                    issue.GraphId +
                    (issue.NodeId.IsValid
                        ? "/nodes/" + issue.NodeId.Value
                        : string.Empty),
                    "presentation_pose_validation_" +
                    issue.Code.ToString(),
                    issue.Message);
            }
            foreach (AgentLinkedPoseGraphMutationPlan linked in
                     plan.LinkedPoseGraphs)
            {
                IReadOnlyList<string> linkedCapabilityErrors =
                    CharacterPoseGraphCapabilityValidator.Validate(
                        linked.GraphOwner,
                        linked.Implementation.Entries
                            .Where(value => value != null)
                            .Select(value => value.GraphId)
                            .ToArray());
                for (int i = 0; i < linkedCapabilityErrors.Count; i++)
                {
                    report.Error(
                        "editable/presentation/linked-pose-implementations/" +
                        linked.Implementation.ImplementationId,
                        "linked_pose_capability_validation_failed",
                        linkedCapabilityErrors[i]);
                }
                try
                {
                    linked.Implementation.RequireValid();
                    foreach (CharacterLinkedPoseImplementationEntryBinding entry in
                             linked.Implementation.Entries)
                        CharacterLinkedPosePortProjection.RequireEntryGraphMatch(
                            entry.RequireValid(),
                            linked.Implementation.Interface,
                            entry.EntryId);
                }
                catch (Exception exception)
                {
                    report.Error(
                        "editable/presentation/linked-pose-implementations/" +
                        linked.Implementation.ImplementationId,
                        "linked_pose_implementation_validation_failed",
                        exception.Message);
                }
            }
            var profileErrors = new List<string>();
            plan.Profile.CollectConfigurationErrors(profileErrors);
            foreach (string error in profileErrors)
            {
                report.Error(
                    "editable/presentation/profile.json.linkedPose",
                    "linked_pose_profile_validation_failed",
                    error);
            }
            foreach (CharacterPresentationPoseSourceBinding source in
                     plan.Profile.PoseSourceBindings)
            {
                try
                {
                    source.RequireValid(plan.Profile.RigDefinition);
                }
                catch (Exception exception)
                {
                    report.Error(
                        "editable/presentation/profile.json.poseSources[" +
                        (source && source.Slot ? source.Slot.name : "missing-slot") + "]",
                        "presentation_pose_source_validation_failed",
                        exception.Message);
                }
            }
        }

        static void AppendValidation(AgentCompileReport target, AgentCompileReport validation)
        {
            target.messages.AddRange(validation.messages);
            target.metrics.semanticValidCount = validation.metrics.semanticValidCount;
            target.metrics.semanticInvalidCount = validation.metrics.semanticInvalidCount;
            target.metrics.compileSuccessCount = validation.metrics.compileSuccessCount;
            target.metrics.compileFailureCount = validation.metrics.compileFailureCount;
            target.success = !target.HasErrors();
        }

        static void MarkTouchedOwnersDirty(IReadOnlyList<UnityEngine.Object> owners)
        {
            for (int i = 0; i < owners.Count; i++)
            {
                if (owners[i])
                    EditorUtility.SetDirty(owners[i]);
            }
        }

        static void MarkPresentationOwnersDirty(
            AgentPresentationMutationPlan plan)
        {
            if (plan == null)
                return;
            if (plan.GraphTransaction.Mutations.Count > 0 && plan.PoseGraph)
                EditorUtility.SetDirty(plan.PoseGraph);
            if (plan.ProfileTransaction.Mutations.Count > 0 && plan.Profile)
                EditorUtility.SetDirty(plan.Profile);
            foreach (AgentLinkedPoseGraphMutationPlan linked in plan.LinkedPoseGraphs)
            {
                if (linked.Transaction.Mutations.Count > 0 && linked.GraphOwner)
                    EditorUtility.SetDirty(linked.GraphOwner);
                if (linked.Implementation)
                    EditorUtility.SetDirty(linked.Implementation);
            }
            foreach (AgentAnimationClipCurveMutationPlan clip in plan.AnimationClips)
                EditorUtility.SetDirty(clip.Clip);
        }

        static void RecordTouchedOwners(
            AgentCompileReport report,
            IReadOnlyList<UnityEngine.Object> gameplayOwners,
            AgentPresentationMutationPlan presentation)
        {
            var owners = new HashSet<UnityEngine.Object>(
                gameplayOwners.Where(value => value));
            if (presentation?.GraphTransaction.Mutations.Count > 0)
                owners.Add(presentation.PoseGraph);
            if (presentation?.ProfileTransaction.Mutations.Count > 0)
                owners.Add(presentation.Profile);
            if (presentation != null)
            {
                foreach (AgentLinkedPoseGraphMutationPlan linked in
                         presentation.LinkedPoseGraphs)
                {
                    if (linked.Transaction.Mutations.Count > 0)
                        owners.Add(linked.GraphOwner);
                    if (presentation.ProfileTransaction.Mutations.Count > 0)
                        owners.Add(linked.Implementation);
                }
            }
            var touched = owners
                .Where(value => value)
                .Select(value =>
                {
                    string assetPath = AssetDatabase.GetAssetPath(value);
                    return new AgentTouchedOwner
                    {
                        assetGuid =
                            AssetDatabase.AssetPathToGUID(assetPath),
                        assetPath = assetPath,
                        assetType = value.GetType().FullName
                    };
                })
                .ToList();
            if (presentation != null)
            {
                touched.AddRange(presentation.AnimationClips.Select(value =>
                    new AgentTouchedOwner
                    {
                        assetGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value.Clip)),
                        assetPath = AssetDatabase.GetAssetPath(value.Clip),
                        assetType = typeof(UnityEngine.AnimationClip).FullName
                    }));
            }
            report.touchedOwners = touched
                .GroupBy(value => value.assetPath, StringComparer.Ordinal)
                .Select(value => value.First())
                .OrderBy(value => value.assetPath, StringComparer.Ordinal)
                .ToList();
        }

        AgentAuthoringResponse RollbackResponse(
            AgentAuthoringRequest request,
            AgentAuthoringPackageProjection projection,
            string path,
            AgentAuthoringPackageState state,
            int undoGroup,
            AgentCompileReport report,
            AgentDocumentApplyResult applyResult,
            string code,
            IReadOnlyList<string> createdSkillRootPaths)
        {
            Exception authoringRollbackFailure = null;
            try
            {
                applyResult?.RollbackAuthoring();
            }
            catch (Exception exception)
            {
                authoringRollbackFailure = exception;
                report.Error("transaction", "authoring_rollback_failed", exception.Message);
            }
            try
            {
                Undo.RevertAllDownToGroup(undoGroup);
                foreach (string assetPath in createdSkillRootPaths)
                {
                    if (System.IO.File.Exists(assetPath) || System.IO.File.Exists(assetPath + ".meta"))
                    {
                        if (!AssetDatabase.DeleteAsset(assetPath))
                            throw new InvalidOperationException($"无法清理本次事务创建的技能根：{assetPath}");
                    }
                }
                AssetDatabase.SaveAssets();
            }
            catch (Exception exception)
            {
                report.Error("transaction", "rollback_failed", exception.Message);
                code = "rollback_failed";
            }
            if (authoringRollbackFailure != null)
                code = "rollback_failed";
            report.applied = false;
            report.success = false;
            LogRollbackFailure(report, code);
            AgentAuthoringResponse response = FromReport(request, report, projection, path, state);
            response.applied = false;
            response.saved = false;
            response.syncState = AgentDocumentSyncState.ApplyFailed.ToString();
            response.errorCode = code;
            response.errorMessage = "Document apply失败，Unity authoring事务已回滚。";
            return response;
        }

        static void LogRollbackFailure(AgentCompileReport report, string code)
        {
            AgentCompileMessage firstError = null;
            var errorCodes = new List<string>();
            int errorCount = 0;
            for (int i = 0; i < report.messages.Count; i++)
            {
                if (!string.Equals(report.messages[i].severity, AgentReportSeverity.Error.ToString(), StringComparison.Ordinal))
                    continue;
                errorCount++;
                firstError ??= report.messages[i];
                string errorCode = report.messages[i].code ?? "unknown";
                if (errorCodes.Count >= 12 || errorCodes.Contains(errorCode))
                    continue;
                errorCodes.Add(errorCode);
            }
            Debug.LogError(
                $"BTSMTL Agent Document rollback: {code}; applied={report.appliedDiff.Count}; " +
                $"errors={errorCount}; codes={string.Join(",", errorCodes)}; " +
                $"first={firstError?.code ?? "unknown"}: {firstError?.message ?? "Unknown apply failure."}");
        }

        static bool TryLoadRoot<T>(string path, out T definition, out string code, out string message)
            where T : UnityEngine.Object
        {
            return TryLoadExactAsset(path, "definition", out definition, out code, out message);
        }

        static bool TryLoadExactAsset<T>(
            string path,
            string label,
            out T asset,
            out string code,
            out string message)
            where T : UnityEngine.Object
        {
            asset = null;
            code = string.Empty;
            message = string.Empty;
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("\\") || path.Contains("/../"))
            {
                code = label + "_path_invalid";
                message = label + "路径必须是精确的Assets/...项目资产路径。";
                return false;
            }
            asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!asset || !string.Equals(AssetDatabase.GetAssetPath(asset), path, StringComparison.Ordinal))
            {
                code = label + "_not_found";
                message = $"无法在指定路径加载{typeof(T).Name}：{path}";
                return false;
            }
            return true;
        }

        static AgentCompileReport CreateReport(AgentAuthoringRequest request, string rootIdentity)
        {
            return new AgentCompileReport
            {
                success = true,
                domain = request.domain,
                rootIdentity = rootIdentity
            };
        }

        static AgentAuthoringResponse Success(
            AgentAuthoringRequest request,
            AgentAuthoringPackageProjection projection,
            string path,
            AgentDocumentSyncState state,
            string documentHash,
            AgentCompileReport report,
            string editableHash = null,
            string contextHash = null)
        {
            report ??= CreateReport(request, projection.Target.rootIdentity);
            report.success = !report.HasErrors();
            return new AgentAuthoringResponse
            {
                action = AgentAuthoringActionUtility.ToProtocolValue(request.action),
                domain = request.domain,
                rootAssetPath = request.rootAssetPath,
                rootIdentity = projection.Target.rootIdentity,
                success = report.success,
                packagePath = path,
                syncState = state.ToString(),
                sourceRevision = projection.SourceRevision,
                editableHash = editableHash ?? projection.EditableHash,
                contextHash = contextHash ?? projection.ContextHash,
                documentHash = documentHash,
                report = report
            };
        }

        static AgentAuthoringResponse FromReport(
            AgentAuthoringRequest request,
            AgentCompileReport report,
            AgentAuthoringPackageProjection projection,
            string path,
            AgentAuthoringPackageState state = null)
        {
            report.success = !report.HasErrors();
            return new AgentAuthoringResponse
            {
                action = AgentAuthoringActionUtility.ToProtocolValue(request.action),
                domain = request.domain,
                rootAssetPath = request.rootAssetPath,
                rootIdentity = projection.Target.rootIdentity,
                success = report.success,
                applied = report.applied,
                saved = report.applied,
                errorCode = report.HasErrors() ? FirstErrorCode(report) : string.Empty,
                errorMessage = report.HasErrors() ? "Agent Document操作失败。" : string.Empty,
                packagePath = path,
                syncState = state?.SyncState.ToString() ?? string.Empty,
                sourceRevision = projection.SourceRevision,
                editableHash = state?.EditableHash ?? projection.EditableHash,
                contextHash = state?.ContextHash ?? projection.ContextHash,
                documentHash = state?.DocumentHash ?? string.Empty,
                report = report
            };
        }

        static string FirstErrorCode(AgentCompileReport report)
        {
            for (int i = 0; i < report.messages.Count; i++)
            {
                AgentCompileMessage message = report.messages[i];
                if (message.severity == AgentReportSeverity.Error.ToString())
                    return message.code;
            }
            return "agent_document_failed";
        }

        static AgentAuthoringResponse Failure(
            AgentAuthoringAction action,
            string domain,
            string path,
            string code,
            string message,
            string diagnosticPath = "bridge",
            string suggestion = "")
        {
            var report = new AgentCompileReport { success = false, domain = domain ?? string.Empty };
            report.Error(diagnosticPath, code, message, suggestion);
            return new AgentAuthoringResponse
            {
                action = AgentAuthoringActionUtility.ToProtocolValue(action),
                domain = domain,
                rootAssetPath = path,
                rootIdentity = report.rootIdentity,
                success = false,
                errorCode = code,
                errorMessage = message,
                report = report
            };
        }
    }
}
