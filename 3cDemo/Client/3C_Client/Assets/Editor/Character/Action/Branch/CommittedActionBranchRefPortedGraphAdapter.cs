using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonAction;
using ThirdPersonCharacterBehavior.Editor.Graph;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using UnityEngine;

namespace ThirdPersonCharacterBehavior.Editor.ActionBranch
{
    public sealed class CommittedActionBranchRefPortedGraphAdapter : ICharacterBehaviorRefPortedGraphAdapter
    {
        static readonly CharacterBehaviorRefPortedCreateNodeOption[] createOptions =
        {
            new CharacterBehaviorRefPortedCreateNodeOption("Selector", "Committed Action/Selector"),
            new CharacterBehaviorRefPortedCreateNodeOption("Condition", "Committed Action/Condition"),
            new CharacterBehaviorRefPortedCreateNodeOption("Timeline", "Committed Action/Timeline")
        };

        readonly CommittedActionBranchSerializedAdapter adapter;

        public CommittedActionBranchRefPortedGraphAdapter(CommittedActionBranchSerializedAdapter adapter)
        {
            this.adapter = adapter;
        }

        public bool IsValid => adapter != null && adapter.IsValid;
        public IReadOnlyList<CharacterBehaviorRefPortedCreateNodeOption> CreateOptions => createOptions;

        public CharacterBehaviorRefPortedGraphSnapshot Capture()
        {
            if (!IsValid)
                return new CharacterBehaviorRefPortedGraphSnapshot(
                    Array.Empty<CharacterBehaviorRefPortedGraphNodeSnapshot>(),
                    Array.Empty<CharacterBehaviorRefPortedGraphEdgeSnapshot>(),
                    string.Empty);

            CommittedActionBranchEditorSnapshot snapshot = adapter.Capture();
            CharacterBehaviorRefPortedGraphNodeSnapshot[] nodes = snapshot.Nodes
                .Select(ToNode)
                .ToArray();
            CharacterBehaviorRefPortedGraphEdgeSnapshot[] edges = snapshot.Nodes
                .SelectMany(parent => parent.ChildNodeIds.Select(child => new CharacterBehaviorRefPortedGraphEdgeSnapshot(parent.NodeId, child)))
                .ToArray();
            return new CharacterBehaviorRefPortedGraphSnapshot(
                nodes,
                edges,
                "Committed Action branch graph. Selector, condition, and timeline nodes edit CharacterActionDefinitionSO branch authoring.");
        }

        public bool AddNode(string optionId, Vector2 position, out string nodeId, out string diagnostic)
        {
            nodeId = string.Empty;
            diagnostic = string.Empty;
            if (!TryResolveKind(optionId, out CommittedActionNodeKind kind))
            {
                diagnostic = $"branch-node-kind-invalid:{optionId}";
                return false;
            }

            nodeId = $"{kind.ToString().ToLowerInvariant()}.{Guid.NewGuid():N}";
            if (!adapter.AddNode(kind, nodeId, out diagnostic))
                return false;
            if (!adapter.SetNodePosition(nodeId, position, out diagnostic))
                return false;
            return true;
        }

        public bool Connect(string parentNodeId, string childNodeId, out string diagnostic)
        {
            return adapter.AppendChild(parentNodeId, childNodeId, out diagnostic);
        }

        public bool Disconnect(string parentNodeId, string childNodeId, out string diagnostic)
        {
            return adapter.RemoveChild(parentNodeId, childNodeId, out diagnostic);
        }

        public bool MoveNode(string nodeId, Vector2 position, out string diagnostic)
        {
            return adapter.SetNodePosition(nodeId, position, out diagnostic);
        }

        public bool DeleteNode(string nodeId, out string diagnostic)
        {
            return adapter.RemoveNode(nodeId, out diagnostic);
        }

        static CharacterBehaviorRefPortedGraphNodeSnapshot ToNode(CommittedActionBranchNodeEditorSnapshot node)
        {
            return new CharacterBehaviorRefPortedGraphNodeSnapshot(
                node.NodeId,
                ResolveTitle(node.Kind),
                ResolveDescription(node),
                ResolveSummary(node),
                node.EditorPosition,
                node.Kind != CommittedActionNodeKind.Root,
                node.Kind == CommittedActionNodeKind.Root ||
                node.Kind == CommittedActionNodeKind.Selector ||
                node.Kind == CommittedActionNodeKind.Condition ||
                node.Kind == CommittedActionNodeKind.Timeline,
                node.IsRoot,
                node.CanDelete,
                !node.IsProtected);
        }

        static string ResolveTitle(CommittedActionNodeKind kind)
        {
            switch (kind)
            {
                case CommittedActionNodeKind.Root:
                    return "Branch Root";
                case CommittedActionNodeKind.Selector:
                    return "Selector";
                case CommittedActionNodeKind.Condition:
                    return "Condition";
                case CommittedActionNodeKind.Timeline:
                    return "Timeline";
                default:
                    return "Unknown";
            }
        }

        static string ResolveDescription(CommittedActionBranchNodeEditorSnapshot node)
        {
            switch (node.Kind)
            {
                case CommittedActionNodeKind.Root:
                    return "Fixed branch entry. It only points at the first action-internal selector or timeline node.";
                case CommittedActionNodeKind.Selector:
                    return "Selector chooses the first child branch that can evaluate inside one committed action.";
                case CommittedActionNodeKind.Condition:
                    return "Condition checks request, fact, or action variant before reaching child timeline nodes.";
                case CommittedActionNodeKind.Timeline:
                    return "Timeline node owns seconds-authored action timeline data for this committed action branch.";
                default:
                    return string.Empty;
            }
        }

        static string ResolveSummary(CommittedActionBranchNodeEditorSnapshot node)
        {
            switch (node.Kind)
            {
                case CommittedActionNodeKind.Root:
                    return node.ChildNodeIds.Count == 0
                        ? $"{node.NodeId}\nchild: -"
                        : $"{node.NodeId}\nchild: {node.ChildNodeIds[0]}";
                case CommittedActionNodeKind.Selector:
                    string rootPrefix = node.IsRoot ? "protected root\n" : string.Empty;
                    return node.ChildNodeIds.Count == 0
                        ? $"{rootPrefix}{node.NodeId}\nchildren: -"
                        : $"{rootPrefix}{node.NodeId}\nchildren: {node.ChildNodeIds.Count}";
                case CommittedActionNodeKind.Condition:
                    return $"{node.NodeId}\n{ConditionSummary(node)}";
                case CommittedActionNodeKind.Timeline:
                    return $"{node.NodeId}\n{TimelineSummary(node)}";
                default:
                    return node.NodeId;
            }
        }

        static string ConditionSummary(CommittedActionBranchNodeEditorSnapshot node)
        {
            if (!string.IsNullOrWhiteSpace(node.RequiredFactId))
                return $"{node.ConditionKind} | fact: {node.RequiredFactId}";
            if (node.RequestKind != 0)
                return $"{node.ConditionKind} | request: {node.RequestKind}";
            if (node.ExpectedVariant != CharacterStateVariant.None)
                return $"{node.ConditionKind} | variant: {node.ExpectedVariant}";
            return node.ConditionKind.ToString();
        }

        static string TimelineSummary(CommittedActionBranchNodeEditorSnapshot node)
        {
            string animation = string.IsNullOrWhiteSpace(node.PrimaryAnimationKey)
                ? "animation: -"
                : $"animation: {node.PrimaryAnimationKey}";
            return $"{node.TimelineNodeId} | {node.DurationSeconds:0.###}s | {animation}";
        }

        static bool TryResolveKind(string optionId, out CommittedActionNodeKind kind)
        {
            if (Enum.TryParse(optionId, out kind) &&
                kind != CommittedActionNodeKind.None &&
                kind != CommittedActionNodeKind.Root)
                return true;

            kind = CommittedActionNodeKind.None;
            return false;
        }
    }
}
