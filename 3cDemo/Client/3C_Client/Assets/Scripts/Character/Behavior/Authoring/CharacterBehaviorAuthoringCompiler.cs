using System;
using System.Collections.Generic;
using ThirdPersonCharacterGraph;

namespace ThirdPersonCharacterBehavior.Authoring
{
    public sealed class CharacterBehaviorAuthoringCompilerResult
    {
        readonly List<string> errors = new List<string>();

        public CharacterBehaviorAuthoringCompilerResult(
            CharacterBehaviorExecutionTree behaviorTree,
            CharacterBehaviorRuntimeDefinition runtimeDefinition,
            IEnumerable<string> errors)
        {
            BehaviorTree = behaviorTree ?? CharacterBehaviorExecutionTree.Empty;
            RuntimeDefinition = runtimeDefinition;
            if (errors != null)
                this.errors.AddRange(errors);
        }

        public CharacterBehaviorExecutionTree BehaviorTree { get; }
        public CharacterBehaviorRuntimeDefinition RuntimeDefinition { get; }
        public IReadOnlyList<string> Errors => errors;
        public bool Success => errors.Count == 0 && BehaviorTree.IsDefined && RuntimeDefinition.IsValid;
    }

    public static class CharacterBehaviorAuthoringCompiler
    {
        public static CharacterBehaviorAuthoringCompilerResult Compile(CharacterBehaviorAuthoringAsset asset)
        {
            List<string> errors = new List<string>();
            if (asset == null)
            {
                errors.Add("asset-missing");
                return Fail(errors);
            }

            if (asset.SchemaVersion != CharacterBehaviorAuthoringAsset.CurrentSchemaVersion)
                errors.Add("schema-version-invalid");
            if (string.IsNullOrWhiteSpace(asset.StableAssetId))
                errors.Add("stable-asset-id-missing");

            Dictionary<string, CharacterBehaviorAuthoringNode> nodeMap = BuildNodeMap(asset, errors);
            Dictionary<string, List<string>> childrenByParent = BuildChildrenByParent(asset, nodeMap, errors);

            CharacterBehaviorAuthoringNode root = FindRoot(asset, errors);
            if (root.IsValid && nodeMap.Count > 0)
            {
                ValidateParentCounts(root.StableId, nodeMap, childrenByParent, errors);
                ValidateReachability(root.StableId, nodeMap, childrenByParent, errors);
                DetectCycles(root.StableId, childrenByParent, new HashSet<string>(), new HashSet<string>(), errors);
            }

            if (errors.Count > 0)
                return Fail(errors);

            CharacterBehaviorExecutionTree tree = CompileTree(root.StableId, nodeMap, childrenByParent);
            CharacterBehaviorExecutionTreeValidationResult treeValidation = CharacterBehaviorExecutionTreeValidator.Validate(tree);
            for (int i = 0; i < treeValidation.Errors.Count; i++)
                errors.Add($"behavior-tree:{treeValidation.Errors[i]}");
            if (errors.Count > 0)
                return Fail(errors);

            CharacterBehaviorRuntimeDefinition runtimeDefinition = CompileRuntimeDefinition(root.StableId, tree);
            if (!runtimeDefinition.IsValid)
            {
                errors.Add($"runtime-definition:{runtimeDefinition.Diagnostic}");
                return Fail(errors);
            }

            return new CharacterBehaviorAuthoringCompilerResult(
                tree,
                runtimeDefinition,
                Array.Empty<string>());
        }

        static Dictionary<string, CharacterBehaviorAuthoringNode> BuildNodeMap(
            CharacterBehaviorAuthoringAsset asset,
            List<string> errors)
        {
            Dictionary<string, CharacterBehaviorAuthoringNode> nodeMap = new Dictionary<string, CharacterBehaviorAuthoringNode>(StringComparer.Ordinal);
            if (asset.Nodes.Count == 0)
                errors.Add("nodes-missing");

            for (int i = 0; i < asset.Nodes.Count; i++)
            {
                CharacterBehaviorAuthoringNode node = asset.Nodes[i];
                if (!node.IsValid)
                {
                    errors.Add($"node-invalid:{i}");
                    continue;
                }

                if (nodeMap.ContainsKey(node.StableId))
                    errors.Add($"node-duplicate:{node.StableId}");
                else
                    nodeMap.Add(node.StableId, node);
            }

            return nodeMap;
        }

        static Dictionary<string, List<string>> BuildChildrenByParent(
            CharacterBehaviorAuthoringAsset asset,
            Dictionary<string, CharacterBehaviorAuthoringNode> nodeMap,
            List<string> errors)
        {
            Dictionary<string, List<string>> childrenByParent = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            for (int i = 0; i < asset.Edges.Count; i++)
            {
                CharacterBehaviorAuthoringEdge edge = asset.Edges[i];
                if (!edge.IsValid)
                {
                    errors.Add($"edge-invalid:{i}");
                    continue;
                }

                if (!nodeMap.TryGetValue(edge.ParentNodeId, out CharacterBehaviorAuthoringNode parent))
                {
                    errors.Add($"edge-parent-missing:{edge.ParentNodeId}");
                    continue;
                }

                if (!nodeMap.ContainsKey(edge.ChildNodeId))
                {
                    errors.Add($"edge-child-missing:{edge.ChildNodeId}");
                    continue;
                }

                if (!IsComposite(parent.Kind))
                    errors.Add($"edge-parent-not-composite:{edge.ParentNodeId}");
                if (!string.Equals(edge.OutputPortId, CharacterBehaviorAuthoringPortIds.Children, StringComparison.Ordinal) ||
                    !string.Equals(edge.InputPortId, CharacterBehaviorAuthoringPortIds.Input, StringComparison.Ordinal))
                    errors.Add($"edge-port-incompatible:{edge.ParentNodeId}->{edge.ChildNodeId}");

                if (!childrenByParent.TryGetValue(edge.ParentNodeId, out List<string> children))
                {
                    children = new List<string>();
                    childrenByParent.Add(edge.ParentNodeId, children);
                }

                children.Add(edge.ChildNodeId);
            }

            return childrenByParent;
        }

        static CharacterBehaviorAuthoringNode FindRoot(
            CharacterBehaviorAuthoringAsset asset,
            List<string> errors)
        {
            CharacterBehaviorAuthoringNode root = default;
            int rootCount = 0;
            for (int i = 0; i < asset.Nodes.Count; i++)
            {
                CharacterBehaviorAuthoringNode node = asset.Nodes[i];
                if (node.Kind != CharacterBehaviorAuthoringNodeKind.Root)
                    continue;

                root = node;
                rootCount++;
            }

            if (rootCount == 0)
                errors.Add("root-missing");
            if (rootCount > 1)
                errors.Add("root-duplicate");
            return root;
        }

        static void ValidateParentCounts(
            string rootId,
            Dictionary<string, CharacterBehaviorAuthoringNode> nodeMap,
            Dictionary<string, List<string>> childrenByParent,
            List<string> errors)
        {
            Dictionary<string, int> parentCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, List<string>> pair in childrenByParent)
            {
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    string childId = pair.Value[i];
                    parentCounts.TryGetValue(childId, out int count);
                    parentCounts[childId] = count + 1;
                }
            }

            foreach (string nodeId in nodeMap.Keys)
            {
                parentCounts.TryGetValue(nodeId, out int count);
                if (string.Equals(nodeId, rootId, StringComparison.Ordinal))
                {
                    if (count > 0)
                        errors.Add($"root-has-parent:{nodeId}");
                    continue;
                }

                if (count == 0)
                    errors.Add($"node-parent-missing:{nodeId}");
                if (count > 1)
                    errors.Add($"node-multiple-parents:{nodeId}");
            }
        }

        static void ValidateReachability(
            string rootId,
            Dictionary<string, CharacterBehaviorAuthoringNode> nodeMap,
            Dictionary<string, List<string>> childrenByParent,
            List<string> errors)
        {
            HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
            Visit(rootId, childrenByParent, visited);
            foreach (string nodeId in nodeMap.Keys)
            {
                if (!visited.Contains(nodeId))
                    errors.Add($"node-disconnected:{nodeId}");
            }
        }

        static void DetectCycles(
            string nodeId,
            Dictionary<string, List<string>> childrenByParent,
            HashSet<string> visiting,
            HashSet<string> visited,
            List<string> errors)
        {
            if (visited.Contains(nodeId))
                return;

            if (visiting.Contains(nodeId))
            {
                errors.Add($"node-cycle:{nodeId}");
                return;
            }

            visiting.Add(nodeId);
            if (childrenByParent.TryGetValue(nodeId, out List<string> children))
            {
                for (int i = 0; i < children.Count; i++)
                    DetectCycles(children[i], childrenByParent, visiting, visited, errors);
            }

            visiting.Remove(nodeId);
            visited.Add(nodeId);
        }

        static void Visit(
            string nodeId,
            Dictionary<string, List<string>> childrenByParent,
            HashSet<string> visited)
        {
            if (!visited.Add(nodeId))
                return;

            if (!childrenByParent.TryGetValue(nodeId, out List<string> children))
                return;

            for (int i = 0; i < children.Count; i++)
                Visit(children[i], childrenByParent, visited);
        }

        static CharacterBehaviorExecutionTree CompileTree(
            string rootId,
            Dictionary<string, CharacterBehaviorAuthoringNode> nodeMap,
            Dictionary<string, List<string>> childrenByParent)
        {
            CharacterExecutionNodeDefinition[] nodes = new CharacterExecutionNodeDefinition[nodeMap.Count];
            int index = 0;
            foreach (KeyValuePair<string, CharacterBehaviorAuthoringNode> pair in nodeMap)
            {
                CharacterExecutionNodeId[] childIds = ResolveChildren(pair.Key, childrenByParent);
                nodes[index++] = ToRuntimeNode(pair.Value, childIds);
            }

            return new CharacterBehaviorExecutionTree(new CharacterExecutionNodeId(rootId), nodes);
        }

        static CharacterExecutionNodeId[] ResolveChildren(
            string nodeId,
            Dictionary<string, List<string>> childrenByParent)
        {
            if (!childrenByParent.TryGetValue(nodeId, out List<string> children))
                return Array.Empty<CharacterExecutionNodeId>();

            CharacterExecutionNodeId[] childIds = new CharacterExecutionNodeId[children.Count];
            for (int i = 0; i < children.Count; i++)
                childIds[i] = new CharacterExecutionNodeId(children[i]);
            return childIds;
        }

        static CharacterExecutionNodeDefinition ToRuntimeNode(
            CharacterBehaviorAuthoringNode node,
            CharacterExecutionNodeId[] children)
        {
            switch (node.Kind)
            {
                case CharacterBehaviorAuthoringNodeKind.Root:
                    return CharacterExecutionNodeDefinition.Root(node.StableId, children);
                case CharacterBehaviorAuthoringNodeKind.Parallel:
                    return CharacterExecutionNodeDefinition.Parallel(node.StableId, children);
                case CharacterBehaviorAuthoringNodeKind.LocomotionLeaf:
                    return CharacterExecutionNodeDefinition.Branch(node.StableId, CharacterGraphBranchKind.Locomotion, children);
                case CharacterBehaviorAuthoringNodeKind.CommittedActionLeaf:
                    return CharacterExecutionNodeDefinition.Branch(node.StableId, CharacterGraphBranchKind.Action, children);
                default:
                    return default;
            }
        }

        static CharacterBehaviorRuntimeDefinition CompileRuntimeDefinition(
            string rootId,
            CharacterBehaviorExecutionTree tree)
        {
            List<CharacterBehaviorSourceKind> leaves = new List<CharacterBehaviorSourceKind>();
            CollectLeaves(tree.RootNodeId, tree, leaves);
            return new CharacterBehaviorRuntimeDefinition(
                new CharacterBehaviorSourceId(rootId),
                leaves.ToArray());
        }

        static void CollectLeaves(
            CharacterExecutionNodeId nodeId,
            CharacterBehaviorExecutionTree tree,
            List<CharacterBehaviorSourceKind> leaves)
        {
            if (!tree.TryGetNode(nodeId, out CharacterExecutionNodeDefinition node))
                return;

            if (node.Kind == CharacterExecutionNodeKind.Branch)
            {
                if (node.BranchKind == CharacterGraphBranchKind.Locomotion)
                    leaves.Add(CharacterBehaviorSourceKind.Locomotion);
                else if (node.BranchKind == CharacterGraphBranchKind.Action)
                    leaves.Add(CharacterBehaviorSourceKind.CommittedAction);
                return;
            }

            for (int i = 0; i < node.Children.Count; i++)
                CollectLeaves(node.Children[i], tree, leaves);
        }

        static bool IsComposite(CharacterBehaviorAuthoringNodeKind kind)
        {
            return kind == CharacterBehaviorAuthoringNodeKind.Root ||
                   kind == CharacterBehaviorAuthoringNodeKind.Parallel;
        }

        static CharacterBehaviorAuthoringCompilerResult Fail(IEnumerable<string> errors)
        {
            return new CharacterBehaviorAuthoringCompilerResult(
                CharacterBehaviorExecutionTree.Empty,
                CharacterBehaviorRuntimeDefinition.Invalid("authoring-compile-failed"),
                errors);
        }
    }
}
