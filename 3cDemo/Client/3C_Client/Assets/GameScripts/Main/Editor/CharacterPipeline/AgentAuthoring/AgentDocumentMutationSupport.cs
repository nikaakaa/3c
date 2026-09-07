using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Motion;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentDocumentMutationSupport
    {
        internal static Vector2 ToVector(AgentSnapshotVector2 value)
        {
            return value == null ? Vector2.zero : new Vector2(value.x, value.y);
        }

        internal static void SetMotionConfiguration(
            AgentMutationDraft operation,
            AgentSnapshotNode node)
        {
            operation.moveSpeed = node.moveSpeed;
            operation.displacementMode = node.displacementMode;
            AgentSnapshotAssetReference actionMotionCurve = (node.assetReferences ?? new List<AgentSnapshotAssetReference>())
                .SingleOrDefault(value => string.Equals(value.key, "m_ActionMotionCurve", StringComparison.Ordinal));
            if (actionMotionCurve != null)
            {
                operation.actionMotionCurve = actionMotionCurve.label;
                operation.actionMotionCurveAssetPath = actionMotionCurve.assetPath;
                operation.actionMotionCurveAssetGuid = actionMotionCurve.assetGuid;
            }
            operation.turnSpeedDegrees = node.turnSpeedDegrees;
            operation.cameraRelative = node.cameraRelative;
            operation.executionMode = node.executionMode;
            operation.durationSeconds = node.durationSeconds;
        }

        internal static Vector2 FindNodePosition(IReadOnlyList<AgentSnapshotGraph> graphs, string identity)
        {
            AgentSnapshotNode node = (graphs ?? Array.Empty<AgentSnapshotGraph>())
                .SelectMany(graph => graph.nodes ?? new List<AgentSnapshotNode>())
                .FirstOrDefault(value => string.Equals(value.elementAuthoringId, identity, StringComparison.Ordinal));
            return ToVector(node?.position);
        }

        internal static void SetDefaultValue(AgentMutationDraft operation, string type, string value)
        {
            if (string.Equals(type, typeof(bool).FullName, StringComparison.Ordinal) && bool.TryParse(value, out bool boolValue))
                operation.blackboardBoolValue = boolValue;
            else if (string.Equals(type, typeof(int).FullName, StringComparison.Ordinal) && int.TryParse(value, out int intValue))
                operation.blackboardIntValue = intValue;
            else if (string.Equals(type, typeof(float).FullName, StringComparison.Ordinal) && float.TryParse(value, out float floatValue))
                operation.blackboardFloatValue = floatValue;
        }

        internal static void Add(AgentMutationDraftSet mutations, string path, AgentMutationKind kind, Action<AgentMutationDraft> configure)
        {
            var operation = new AgentMutationDraft
            {
                id = "mutation-" + mutations.mutations.Count.ToString("D4"),
                sourcePath = path,
                kind = kind
            };
            configure(operation);
            mutations.mutations.Add(operation);
        }

        internal static void SetDeclarationReference(AgentMutationDraft operation, string identity, bool targetSnapshot)
        {
            if (targetSnapshot)
            {
                SetTargetSnapshotDeclarationReference(operation, identity);
                return;
            }
            if (IsLocal(identity))
                operation.declarationPlannedIdentity = LocalIdentity(identity);
            else
                operation.declarationAuthoringId = identity;
        }

        internal static void SetTargetSnapshotDeclarationReference(AgentMutationDraft operation, string identity)
        {
            if (string.IsNullOrEmpty(identity))
                return;
            if (IsLocal(identity))
                operation.targetSnapshotBlackboardDeclarationPlannedIdentity = LocalIdentity(identity);
            else
                operation.targetSnapshotBlackboardDeclarationId = identity;
        }

        internal static void SetGraph(AgentMutationDraft operation, string identity)
        {
            if (IsLocal(identity))
                operation.graphPlannedIdentity = LocalIdentity(identity);
            else
                operation.graphAuthoringId = identity;
        }

        internal static void SetGraph(AgentMutationDraft operation, AgentSnapshotGraph graph)
        {
            if (graph != null &&
                IsLocal(graph.graphAuthoringId) &&
                IsLocal(graph.ownerElementAuthoringId))
            {
                operation.graphPlannedIdentity = LocalIdentity(graph.ownerElementAuthoringId);
                return;
            }
            SetGraph(operation, graph?.graphAuthoringId);
        }

        internal static void SetTargetGraph(AgentMutationDraft operation, AgentSnapshotGraph graph)
        {
            if (graph != null &&
                IsLocal(graph.graphAuthoringId) &&
                IsLocal(graph.ownerElementAuthoringId))
            {
                operation.targetGraphPlannedIdentity = LocalIdentity(graph.ownerElementAuthoringId);
                return;
            }
            if (IsLocal(graph?.graphAuthoringId))
                operation.targetGraphPlannedIdentity = LocalIdentity(graph.graphAuthoringId);
            else
                operation.targetGraphAuthoringId = graph?.graphAuthoringId;
        }

        internal static void SetStateMachine(
            AgentMutationDraft operation,
            AgentSnapshotGraph graph,
            string fallbackIdentity)
        {
            if (graph != null)
            {
                SetStateMachine(operation, graph);
                return;
            }
            SetStateMachine(operation, fallbackIdentity);
        }

        internal static void SetStateMachine(AgentMutationDraft operation, string identity)
        {
            if (IsLocal(identity))
                operation.stateMachinePlannedIdentity = LocalIdentity(identity);
            else
                operation.stateMachineGraphAuthoringId = identity;
        }

        internal static void SetStateMachine(AgentMutationDraft operation, AgentSnapshotGraph graph)
        {
            if (graph != null &&
                IsLocal(graph.graphAuthoringId) &&
                IsLocal(graph.ownerElementAuthoringId))
            {
                operation.stateMachinePlannedIdentity = LocalIdentity(graph.ownerElementAuthoringId);
                return;
            }
            SetStateMachine(operation, graph?.graphAuthoringId);
        }

        internal static void SetElement(AgentMutationDraft operation, string identity, bool source)
        {
            if (source)
            {
                if (IsLocal(identity))
                    operation.fromPlannedIdentity = LocalIdentity(identity);
                else
                    operation.fromElementAuthoringId = identity;
            }
            else
            {
                if (IsLocal(identity))
                    operation.toPlannedIdentity = LocalIdentity(identity);
                else
                    operation.toElementAuthoringId = identity;
            }
        }

        internal static void SetLinkElement(AgentMutationDraft operation, string identity, bool source)
        {
            if (source)
            {
                if (IsLocal(identity))
                    operation.sourcePlannedIdentity = LocalIdentity(identity);
                else
                    operation.sourceElementAuthoringId = identity;
            }
            else
            {
                if (IsLocal(identity))
                    operation.targetPlannedIdentity = LocalIdentity(identity);
                else
                    operation.targetElementAuthoringId = identity;
            }
        }

        internal static void SetOptionalExisting(AgentMutationDraft operation, string identity, bool declaration)
        {
            if (IsLocal(identity))
                return;
            if (declaration)
                operation.declarationAuthoringId = identity;
            else
                operation.targetElementAuthoringId = identity;
        }

        internal static string LocalIdentity(string identity)
        {
            return identity;
        }

        internal static bool IsLocal(string identity)
        {
            return !string.IsNullOrEmpty(identity) && identity.StartsWith("local:", StringComparison.Ordinal);
        }

        internal static Dictionary<string, T> Index<T>(
            IEnumerable<T> values,
            Func<T, string> identity,
            string path,
            AgentCompileReport report)
            where T : class
        {
            var result = new Dictionary<string, T>(StringComparer.Ordinal);
            int index = 0;
            foreach (T value in values ?? Array.Empty<T>())
            {
                string key = value == null ? string.Empty : identity(value);
                if (string.IsNullOrWhiteSpace(key))
                    report.Error($"{path}[{index}]", "entity_identity_missing", "Document entity缺少identity。");
                else if (!result.TryAdd(key, value))
                    report.Error($"{path}[{index}]", "entity_identity_duplicate", $"Document entity identity重复：{key}");
                index++;
            }
            return result;
        }

        internal static bool Same(object left, object right)
        {
            return string.Equals(
                AgentAuthoringDocumentCodec.Hash(left),
                AgentAuthoringDocumentCodec.Hash(right),
                StringComparison.Ordinal);
        }

        internal static bool SameList(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            return (left ?? Array.Empty<string>()).SequenceEqual(right ?? Array.Empty<string>(), StringComparer.Ordinal);
        }

        internal static bool SameOptionalText(string left, string right)
        {
            return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal);
        }

        internal static bool SameFlowEdge(AgentSnapshotFlowEdge left, AgentSnapshotFlowEdge right)
        {
            return SameOptionalText(left?.elementAuthoringId, right?.elementAuthoringId) &&
                   SameOptionalText(left?.startElementAuthoringId, right?.startElementAuthoringId) &&
                   SameOptionalText(left?.endElementAuthoringId, right?.endElementAuthoringId) &&
                   SameOptionalText(left?.startPort, right?.startPort) &&
                   SameOptionalText(left?.endPort, right?.endPort) &&
                   left?.flowOrder == right?.flowOrder &&
                   left?.transitionPriority == right?.transitionPriority &&
                   SameOptionalText(left?.abortPolicy, right?.abortPolicy) &&
                   SameOptionalText(left?.conditionRuleGraphAuthoringId, right?.conditionRuleGraphAuthoringId);
        }

        internal static string Escape(string identity)
        {
            return "'" + (identity ?? string.Empty).Replace("'", "\\'") + "'";
        }
    }
}
