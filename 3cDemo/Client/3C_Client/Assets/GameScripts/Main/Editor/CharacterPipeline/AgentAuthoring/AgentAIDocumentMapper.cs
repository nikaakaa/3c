using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.AI;
using TreeDesigner;
using UnityEngine;
using static ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.AgentPackageMappingSupport;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentAIDocumentMapper
    {
        internal static AgentPackageAIController ToPackageAI(AgentDocumentAIEditable source)
        {
            return new AgentPackageAIController
            {
                controllerId = source.controllerId,
                definitionAssetPath = source.definitionAssetPath,
                definitionAssetGuid = source.definitionAssetGuid,
                treeAssetPath = source.treeAssetPath,
                treeAssetGuid = source.treeAssetGuid,
                graphAuthoringId = source.graphAuthoringId,
                authoringRole = source.authoringRole,
                perceptionAssetPath = source.perceptionAssetPath,
                perceptionAssetGuid = source.perceptionAssetGuid,
                candidateOrdering = source.candidateOrdering,
                candidateActorIds = source.candidateActorIds,
                controlledCharacterAssetPath = source.controlledCharacterAssetPath,
                controlledCharacterAssetGuid = source.controlledCharacterAssetGuid,
                blackboard = (source.blackboardDeclarations ?? new List<AgentSnapshotAIBlackboardDeclaration>())
                    .Select(value => new AgentPackageAIBlackboardDeclaration
                    {
                        id = value.declarationAuthoringId,
                        key = value.displayName,
                        valueType = StableValueType(value.valueType),
                        scope = value.scope,
                        defaultValue = value.defaultValue
                    }).ToList(),
                nodes = (source.nodes ?? new List<AgentSnapshotAINode>())
                    .Select(value => new AgentPackageAINodeConfiguration
                    {
                        id = value.nodeAuthoringId,
                        memoryValueKind = value.memoryValueKind,
                        memoryDeclarationId = value.memoryDeclarationAuthoringId,
                        inputId = value.inputId,
                        requestId = value.requestId,
                        requestBufferSeconds = value.requestBufferSeconds,
                        requestPriority = value.requestPriority,
                        requestRepeatPolicy = value.requestRepeatPolicy
                    }).ToList()
            };
        }

        internal static bool TryFromPackageAI(
            AgentPackageAIController source,
            IReadOnlyList<AgentSnapshotGraph> graphs,
            AgentCompileReport report,
            out AgentDocumentAIEditable target)
        {
            target = new AgentDocumentAIEditable
            {
                controllerId = source.controllerId,
                definitionAssetPath = source.definitionAssetPath,
                definitionAssetGuid = source.definitionAssetGuid,
                treeAssetPath = source.treeAssetPath,
                treeAssetGuid = source.treeAssetGuid,
                graphAuthoringId = source.graphAuthoringId,
                authoringRole = source.authoringRole,
                perceptionAssetPath = source.perceptionAssetPath,
                perceptionAssetGuid = source.perceptionAssetGuid,
                candidateOrdering = source.candidateOrdering,
                candidateActorIds = source.candidateActorIds ?? new List<string>(),
                controlledCharacterAssetPath = source.controlledCharacterAssetPath,
                controlledCharacterAssetGuid = source.controlledCharacterAssetGuid,
                blackboardDeclarations = (source.blackboard ?? new List<AgentPackageAIBlackboardDeclaration>())
                    .Select(value => new AgentSnapshotAIBlackboardDeclaration
                    {
                        declarationAuthoringId = value.id,
                        ownerGraphAuthoringId = source.graphAuthoringId,
                        displayName = value.key,
                        valueType = InternalValueType(value.valueType),
                        scope = value.scope,
                        lifetime = ResolveAIDefaultLifetime(value.scope),
                        defaultValue = value.defaultValue
                    }).ToList()
            };
            var graphNodes = (graphs ?? Array.Empty<AgentSnapshotGraph>())
                .SelectMany(graph => graph.nodes ?? new List<AgentSnapshotNode>())
                .Where(node => node != null && !string.IsNullOrEmpty(node.elementAuthoringId))
                .ToDictionary(node => node.elementAuthoringId, node => node, StringComparer.Ordinal);
            var configurationIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageAINodeConfiguration configuration in source.nodes ?? new List<AgentPackageAINodeConfiguration>())
            {
                if (configuration == null ||
                    string.IsNullOrEmpty(configuration.id) ||
                    !configurationIds.Add(configuration.id) ||
                    !graphNodes.TryGetValue(configuration.id, out AgentSnapshotNode graphNode))
                {
                    report.Error("editable/ai/perception.json.nodes", "ai_node_graph_reference_invalid", $"AI node配置没有对应Graph Node：{configuration?.id}");
                    return false;
                }
                target.nodes.Add(new AgentSnapshotAINode
                {
                    graphAuthoringId = source.graphAuthoringId,
                    nodeAuthoringId = configuration.id,
                    nodeType = graphNode.typeName,
                    memoryValueKind = configuration.memoryValueKind,
                    memoryDeclarationAuthoringId = configuration.memoryDeclarationId,
                    inputId = configuration.inputId,
                    requestId = configuration.requestId,
                    requestBufferSeconds = configuration.requestBufferSeconds,
                    requestPriority = configuration.requestPriority,
                    requestRepeatPolicy = configuration.requestRepeatPolicy
                });
            }
            AgentSnapshotGraph rootGraph = (graphs ?? Array.Empty<AgentSnapshotGraph>())
                .FirstOrDefault(graph => string.Equals(graph.graphAuthoringId, source.graphAuthoringId, StringComparison.Ordinal));
            var catalog = new BtsmtlGraphAuthoringCapabilities();
            string missing = (rootGraph?.nodes ?? new List<AgentSnapshotNode>())
                .Where(node => node != null &&
                               !catalog.TryGetAnchor(node.typeName, out _) &&
                               !configurationIds.Contains(node.elementAuthoringId))
                .Select(node => node.elementAuthoringId)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(missing))
            {
                report.Error("editable/ai/perception.json.nodes", "ai_node_configuration_missing", $"AI root Graph Node缺少配置记录：{missing}");
                return false;
            }
            return true;
        }

        static string ResolveAIDefaultLifetime(string scope)
        {
            return Enum.TryParse(scope, false, out PipelineBlackboardVariableScope parsed)
                ? PipelineBlackboardVariablePolicy.DefaultLifetime(parsed).ToString()
                : string.Empty;
        }

        static string StableValueType(string value)
        {
            if (string.Equals(value, typeof(bool).FullName, StringComparison.Ordinal)) return "bool";
            if (string.Equals(value, typeof(int).FullName, StringComparison.Ordinal)) return "int";
            if (string.Equals(value, typeof(float).FullName, StringComparison.Ordinal)) return "float";
            if (string.Equals(value, typeof(string).FullName, StringComparison.Ordinal)) return "string";
            if (string.Equals(value, typeof(UnityEngine.Vector2).FullName, StringComparison.Ordinal)) return "vector2";
            if (string.Equals(value, typeof(UnityEngine.Vector3).FullName, StringComparison.Ordinal)) return "vector3";
            if (string.Equals(value, typeof(ThirdPersonCharacter.AI.AIActionTargetSnapshotValue).FullName, StringComparison.Ordinal)) return "aiActionTargetSnapshot";
            int separator = value?.LastIndexOf('.') ?? -1;
            return separator >= 0 ? value.Substring(separator + 1) : value;
        }

        static string InternalValueType(string value)
        {
            switch (value?.Trim().ToLowerInvariant())
            {
                case "bool":
                case "boolean":
                    return typeof(bool).FullName;
                case "int":
                case "int32":
                    return typeof(int).FullName;
                case "float":
                case "single":
                    return typeof(float).FullName;
                case "string":
                    return typeof(string).FullName;
                case "vector2":
                    return typeof(UnityEngine.Vector2).FullName;
                case "vector3":
                    return typeof(UnityEngine.Vector3).FullName;
                case "aiactiontargetsnapshot":
                    return typeof(ThirdPersonCharacter.AI.AIActionTargetSnapshotValue).FullName;
                default:
                    return value;
            }
        }
    }
}
