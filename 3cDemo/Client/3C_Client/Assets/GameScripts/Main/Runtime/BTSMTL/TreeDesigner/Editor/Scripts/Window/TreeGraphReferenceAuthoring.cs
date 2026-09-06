using System;
using System.Collections.Generic;

namespace TreeDesigner.Editor
{
    public readonly struct TreeGraphReferenceDescriptor
    {
        public TreeGraphReferenceDescriptor(
            BaseNode owner,
            string key,
            string label,
            BaseTree tree,
            BaseTreeAsset sharedAsset,
            Type graphType,
            bool supportsInline,
            bool supportsShared,
            string scopeId,
            bool required)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Key = string.IsNullOrWhiteSpace(key) ? throw new ArgumentException("Graph reference key is missing.", nameof(key)) : key;
            Label = label ?? string.Empty;
            Tree = tree;
            SharedAsset = sharedAsset;
            GraphType = graphType ?? throw new ArgumentNullException(nameof(graphType));
            SupportsInline = supportsInline;
            SupportsShared = supportsShared;
            ScopeId = scopeId ?? string.Empty;
            Required = required;
        }

        public BaseNode Owner { get; }
        public string Key { get; }
        public string Label { get; }
        public BaseTree Tree { get; }
        public BaseTreeAsset SharedAsset { get; }
        public Type GraphType { get; }
        public bool SupportsInline { get; }
        public bool SupportsShared { get; }
        public string ScopeId { get; }
        public bool Required { get; }
    }

    public static class TreeGraphReferenceAuthoring
    {
        public static IReadOnlyList<TreeGraphReferenceDescriptor> Describe(BaseNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            var result = new List<TreeGraphReferenceDescriptor>();
            if (node is SubTreeNode subTreeNode)
            {
                result.Add(new TreeGraphReferenceDescriptor(
                    node,
                    "m_SubTree",
                    "SubTree",
                    subTreeNode.SubTree,
                    null,
                    typeof(SubTree),
                    true,
                    false,
                    node.GUID,
                    false));
            }

            if (node is TreeReferenceNode)
            {
                TreeReferenceModule module = node.GetModule<TreeReferenceModule>();
                if (module != null)
                {
                    result.Add(new TreeGraphReferenceDescriptor(
                        node,
                        $"{module.ModuleId}.m_SharedTreeAsset",
                        "Tree",
                        module.Tree,
                        module.SharedTreeAsset,
                        typeof(BaseTree),
                        false,
                        true,
                        string.Empty,
                        true));
                }
            }

            if (node is StateMachineNode)
            {
                ScopedGraphReferenceModule module = node.GetModule<ScopedGraphReferenceModule>();
                if (module != null)
                {
                    result.Add(new TreeGraphReferenceDescriptor(
                        node,
                        $"{module.ModuleId}.m_InlineGraph",
                        "State Machine",
                        module.Graph,
                        module.SharedGraphAsset,
                        typeof(StateMachineGraph),
                        true,
                        true,
                        module.ScopeId,
                        true));
                }
            }

            if (node is StateNode)
            {
                StateBehaviorGraphReferenceModule module = node.GetModule<StateBehaviorGraphReferenceModule>();
                if (module != null)
                {
                    result.Add(new TreeGraphReferenceDescriptor(
                        node,
                        $"{module.ModuleId}.m_InlineSubTree",
                        "State Behavior",
                        module.SubTree,
                        module.SharedSubTreeAsset,
                        typeof(SubTree),
                        true,
                        true,
                        module.ScopeId,
                        false));
                }
            }

            return result;
        }

        public static TreeGraphReferenceDescriptor RequireSlot(BaseNode node, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Graph reference key is missing.", nameof(key));

            foreach (TreeGraphReferenceDescriptor descriptor in Describe(node))
            {
                if (string.Equals(descriptor.Key, key, StringComparison.Ordinal))
                    return descriptor;
            }

            throw new InvalidOperationException($"Node '{node?.GetType().Name ?? "null"}' has no graph reference slot '{key}'.");
        }

        public static void SetInline(BaseNode node, string key, BaseTree tree)
        {
            TreeGraphReferenceDescriptor descriptor = RequireSlot(node, key);
            RequireOwner(node);
            if (!descriptor.SupportsInline)
                throw new InvalidOperationException($"Graph reference slot '{key}' does not support inline ownership.");
            ValidateTree(node, descriptor, tree);

            switch (node)
            {
                case SubTreeNode subTreeNode:
                    subTreeNode.SetSubTree((SubTree)tree);
                    return;
                case StateMachineNode:
                    node.GetModule<ScopedGraphReferenceModule>().SetInlineGraph((StateMachineGraph)tree);
                    return;
                case StateNode:
                    node.GetModule<StateBehaviorGraphReferenceModule>().SetInlineSubTree((SubTree)tree);
                    return;
                default:
                    throw new InvalidOperationException($"Node '{node.GetType().Name}' does not expose an inline graph writer.");
            }
        }

        public static void SetShared(BaseNode node, string key, BaseTreeAsset asset)
        {
            TreeGraphReferenceDescriptor descriptor = RequireSlot(node, key);
            RequireOwner(node);
            if (!descriptor.SupportsShared)
                throw new InvalidOperationException($"Graph reference slot '{key}' does not support shared ownership.");
            if (!asset || asset.Tree == null)
                throw new ArgumentException("Shared graph asset is missing its graph.", nameof(asset));
            ValidateTree(node, descriptor, asset.Tree);

            switch (node)
            {
                case TreeReferenceNode:
                    node.GetModule<TreeReferenceModule>().SetTreeAsset(asset);
                    return;
                case StateMachineNode:
                    node.GetModule<ScopedGraphReferenceModule>().SetSharedGraphAsset(asset);
                    return;
                case StateNode:
                    node.GetModule<StateBehaviorGraphReferenceModule>().SetSharedSubTreeAsset(asset);
                    return;
                default:
                    throw new InvalidOperationException($"Node '{node.GetType().Name}' does not expose a shared graph writer.");
            }
        }

        public static void Clear(BaseNode node, string key)
        {
            RequireSlot(node, key);
            RequireOwner(node);

            switch (node)
            {
                case SubTreeNode subTreeNode:
                    subTreeNode.SetSubTree(null);
                    return;
                case TreeReferenceNode:
                    node.GetModule<TreeReferenceModule>().SetTreeAsset(null);
                    return;
                case StateMachineNode:
                    node.GetModule<ScopedGraphReferenceModule>().SetInlineGraph(null);
                    return;
                case StateNode:
                    node.GetModule<StateBehaviorGraphReferenceModule>().SetInlineSubTree(null);
                    return;
                default:
                    throw new InvalidOperationException($"Node '{node.GetType().Name}' does not expose a graph writer.");
            }
        }

        static void ValidateTree(
            BaseNode node,
            TreeGraphReferenceDescriptor descriptor,
            BaseTree tree)
        {
            if (tree == null || !descriptor.GraphType.IsInstanceOfType(tree))
                throw new ArgumentException($"Graph reference slot '{descriptor.Key}' requires '{descriptor.GraphType.Name}'.", nameof(tree));
            if (node is StateMachineNode && !StateMachineNode.CanReferenceGraph(node.Owner, tree))
                throw new InvalidOperationException("State Machine graph ownership is invalid for the current graph.");
            if (node is StateNode && !StateNode.CanReferenceGraph(node.Owner, tree))
                throw new InvalidOperationException("State Behavior graph ownership is invalid for the current graph.");
        }

        static void RequireOwner(BaseNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (node.Owner == null)
                throw new InvalidOperationException($"Node '{node.GUID}' must belong to an authoring graph before its graph reference is changed.");
        }
    }
}
