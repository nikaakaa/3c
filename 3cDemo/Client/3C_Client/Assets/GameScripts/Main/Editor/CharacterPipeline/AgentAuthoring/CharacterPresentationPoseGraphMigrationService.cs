using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    [Serializable]
    public sealed class CharacterPresentationPoseGraphMigrationResponse
    {
        public string action = "migrate_legacy_pose_state_graphs";
        public string definitionAssetPath;
        public string presentationPoseGraphPath;
        public bool success;
        public bool applied;
        public bool saved;
        public int migratedNodeCount;
        public int migratedGraphCount;
        public int migratedProfileCount;
        public string assetGuid;
        public string revision;
        public string errorCode;
        public string errorMessage;
        public string remediation;
    }

    public static class CharacterPresentationPoseGraphMigrationService
    {
        const string LegacyPayloadName = "CharacterPredictiveFootPlacementPosePayload";

        public static CharacterPresentationPoseGraphMigrationResponse Migrate(
            string definitionAssetPath)
        {
            var response = new CharacterPresentationPoseGraphMigrationResponse
            {
                definitionAssetPath = definitionAssetPath ?? string.Empty
            };
            try
            {
                string path = RequireDefinitionPath(definitionAssetPath);
                CharacterPipelineDefinition definition =
                    AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(path);
                if (!definition)
                    throw Failure(
                        "definition_not_found",
                        path,
                        $"CharacterPipelineDefinition '{path}' does not exist.",
                        "传入一个精确的 Assets/... CharacterPipelineDefinition 路径。");
                CharacterAnimationPresentationProfile profile = definition.AnimationPresentationProfile;
                if (!profile || !profile.PoseGraph)
                    throw Failure(
                        "presentation_pose_graph_missing",
                        path,
                        "Character Definition does not have a Presentation Pose Graph.",
                        "先给该 Definition 配置正式 Animation Presentation Profile 与 Pose Graph。");

                CharacterPresentationPoseGraphAsset poseGraph = profile.PoseGraph;
                string poseGraphPath = AssetDatabase.GetAssetPath(poseGraph);
                response.presentationPoseGraphPath = poseGraphPath ?? string.Empty;
                response.assetGuid = AssetDatabase.AssetPathToGUID(poseGraphPath);
                Undo.IncrementCurrentGroup();
                int undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Migrate Legacy Presentation Pose Graph");
                Undo.RegisterCompleteObjectUndo(
                    poseGraph,
                    "Migrate Legacy Presentation Pose Graph");
                try
                {
                    int nodeCount = MigrateSerializedPayloads(
                        poseGraph,
                        out int graphCount);
                    if (nodeCount == 0)
                    {
                        nodeCount = poseGraph.EnumerateGraphs()
                            .Where(value => value != null)
                            .SelectMany(value => value.Nodes)
                            .Count(value =>
                                value?.Payload is
                                    CharacterFootGroundingPosePayload);
                        if (nodeCount > 0)
                            graphCount = poseGraph.EnumerateGraphs()
                                .Count(value => value != null &&
                                    value.Nodes.Any(node =>
                                        node?.Payload is
                                            CharacterFootGroundingPosePayload));
                    }
                    response.migratedNodeCount = nodeCount;
                    response.migratedGraphCount = graphCount;
                    response.migratedProfileCount = MigrateProfiles(poseGraph);
                    response.revision = ResolveRevision(poseGraph);
                    response.applied = nodeCount > 0 || response.migratedProfileCount > 0;
                    if (response.applied)
                    {
                        EditorUtility.SetDirty(poseGraph);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.ForceReserializeAssets(
                            new[] { poseGraphPath },
                            ForceReserializeAssetsOptions.ReserializeAssets);
                        response.revision = ResolveRevision(poseGraph);
                    }
                    response.saved = true;
                    Undo.CollapseUndoOperations(undoGroup);
                }
                catch
                {
                    Undo.RevertAllDownToGroup(undoGroup);
                    throw;
                }
                response.success = true;
                return response;
            }
            catch (CharacterPresentationPoseGraphMigrationException exception)
            {
                response.errorCode = exception.Code;
                response.errorMessage = exception.Message;
                response.remediation = exception.Remediation;
                return response;
            }
            catch (Exception exception)
            {
                response.errorCode = "pose_graph_migration_failed";
                response.errorMessage = exception.Message;
                response.remediation = "检查该 Definition 的 Presentation Pose Graph 与旧 payload 是否仍保持正式资产引用。";
                return response;
            }
        }

        static int MigrateSerializedPayloads(
            CharacterPresentationPoseGraphAsset poseGraph,
            out int graphCount)
        {
            Dictionary<long, ManagedReferenceMissingType> missing =
                SerializationUtility.GetManagedReferencesWithMissingTypes(
                        poseGraph)
                    .Where(value => string.Equals(
                        value.className,
                        LegacyPayloadName,
                        StringComparison.Ordinal))
                    .ToDictionary(value => value.referenceId);
            var serialized = new SerializedObject(poseGraph);
            SerializedProperty root = serialized.FindProperty("m_TypedGraph");
            SerializedProperty catalog = serialized.FindProperty("m_TypedGraphCatalog");
            int migratedNodes = 0;
            graphCount = 0;
            if (root != null && MigrateGraph(root, missing, ref migratedNodes))
                graphCount++;
            if (catalog != null && catalog.isArray)
            {
                for (int i = 0; i < catalog.arraySize; i++)
                {
                    SerializedProperty graph = catalog.GetArrayElementAtIndex(i);
                    if (graph != null &&
                        MigrateGraph(graph, missing, ref migratedNodes))
                        graphCount++;
                }
            }
            if (migratedNodes == 0 && missing.Count > 0)
            {
                string detail = string.Join(
                    " | ",
                    missing.Values.Select(value =>
                        $"ref={value.referenceId}; type={value.namespaceName}.{value.className}; data={value.serializedData}"));
                throw Failure(
                    "legacy_payload_location_unresolved",
                    AssetDatabase.GetAssetPath(poseGraph),
                    "Legacy managed payload was found but could not be mapped to a Pose node: " + detail,
                    "检查旧 managed reference identity 与 Pose Graph node payload 引用是否仍一致。");
            }
            if (migratedNodes > 0)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                serialized.UpdateIfRequiredOrScript();
            }
            return migratedNodes;
        }

        static int MigrateProfiles(CharacterPresentationPoseGraphAsset poseGraph)
        {
            var profiles = poseGraph.EnumerateGraphs()
                .Where(value => value != null)
                .SelectMany(value => value.Nodes)
                .Select(value => (value?.Payload as CharacterFootGroundingPosePayload)?.Profile)
                .Where(value => value)
                .Distinct()
                .ToArray();
            int migrated = 0;
            for (int i = 0; i < profiles.Length; i++)
            {
                if (!CharacterFootPlacementProfileMigrationService.MigrateIfRequired(profiles[i]))
                    continue;
                EditorUtility.SetDirty(profiles[i]);
                migrated++;
            }
            return migrated;
        }

        static bool MigrateGraph(
            SerializedProperty graph,
            IReadOnlyDictionary<long, ManagedReferenceMissingType> missing,
            ref int migratedNodes)
        {
            if (graph == null)
                return false;
            SerializedProperty nodes = graph.FindPropertyRelative("m_Nodes");
            if (nodes == null || !nodes.isArray)
                return false;
            bool changed = false;
            for (int i = 0; i < nodes.arraySize; i++)
            {
                SerializedProperty node = nodes.GetArrayElementAtIndex(i);
                SerializedProperty payload = node?.FindPropertyRelative("m_Payload");
                if (payload == null)
                    continue;
                bool loadedLegacy = payload.managedReferenceFullTypename.Contains(
                    LegacyPayloadName,
                    StringComparison.Ordinal);
                bool missingLegacy = missing.TryGetValue(
                    payload.managedReferenceId,
                    out ManagedReferenceMissingType missingType);
                if (!loadedLegacy && !missingLegacy)
                    continue;
                CharacterFootGroundingPosePayload replacement;
                if (missingLegacy)
                {
                    replacement = new CharacterFootGroundingPosePayload();
                    EditorJsonUtility.FromJsonOverwrite(
                        missingType.serializedData,
                        replacement);
                }
                else
                {
                    SerializedProperty profile = payload.FindPropertyRelative("m_Profile");
                    SerializedProperty calibration = payload.FindPropertyRelative("m_Calibration");
                    replacement = new CharacterFootGroundingPosePayload(
                        profile?.objectReferenceValue as CharacterFootPlacementProfile,
                        calibration?.objectReferenceValue as CharacterFootPlacementRigCalibration);
                }
                CharacterFootPlacementProfile profileAsset = replacement.Profile;
                CharacterFootPlacementRigCalibration calibrationAsset = replacement.Calibration;
                if (!profileAsset || !calibrationAsset)
                {
                    string nodeId = node.FindPropertyRelative("m_NodeId")?.stringValue ?? string.Empty;
                    throw Failure(
                        "legacy_payload_reference_missing",
                        $"m_TypedGraph.m_Nodes[{i}]",
                        $"Legacy Foot Placement node '{nodeId}' has no valid Profile or Calibration reference.",
                        "恢复该节点原有的正式 Foot Placement Profile 与 Rig Calibration 引用后重试迁移。");
                }
                payload.managedReferenceValue = replacement;
                migratedNodes++;
                changed = true;
            }
            return changed;
        }

        static string ResolveRevision(CharacterPresentationPoseGraphAsset poseGraph)
        {
            CharacterTypedPoseGraph[] graphs = poseGraph.EnumerateGraphs()
                .Where(value => value != null)
                .ToArray();
            return string.Join(",", graphs.Select(value => value.ContentRevision));
        }

        static string RequireDefinitionPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw Failure(
                    "definition_asset_path_required",
                    "definition_asset_path",
                    "Definition asset path is required.",
                    "传入一个精确的 Assets/... CharacterPipelineDefinition 路径。");
            string normalized = path.Trim().Replace('\\', '/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal) ||
                !normalized.EndsWith(".asset", StringComparison.Ordinal))
            {
                throw Failure(
                    "definition_asset_path_invalid",
                    "definition_asset_path",
                    $"Definition asset path '{normalized}' is invalid.",
                    "路径必须是精确的 Assets/.../*.asset。");
            }
            return normalized;
        }

        static CharacterPresentationPoseGraphMigrationException Failure(
            string code,
            string path,
            string message,
            string remediation) =>
            new CharacterPresentationPoseGraphMigrationException(
                code,
                path,
                message,
                remediation);
    }

    public sealed class CharacterPresentationPoseGraphMigrationException : Exception
    {
        public CharacterPresentationPoseGraphMigrationException(
            string code,
            string path,
            string message,
            string remediation)
            : base(message)
        {
            Code = code ?? string.Empty;
            Path = path ?? string.Empty;
            Remediation = remediation ?? string.Empty;
        }

        public string Code { get; }
        public string Path { get; }
        public string Remediation { get; }
    }
}
