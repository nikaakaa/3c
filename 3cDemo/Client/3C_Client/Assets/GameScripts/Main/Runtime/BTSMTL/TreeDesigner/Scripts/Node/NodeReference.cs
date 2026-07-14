using System;
using UnityEngine;

namespace TreeDesigner
{
    public readonly struct NodeGraphReference
    {
        public readonly BaseNode OwnerNode;
        public readonly string Key;
        public readonly string Label;
        public readonly BaseTree Tree;
        public readonly BaseTreeAsset SharedAsset;
        public readonly bool Inline;
        public readonly string ScopeId;
        public readonly bool Required;

        public NodeGraphReference(BaseNode ownerNode, string key, string label, BaseTree tree, BaseTreeAsset sharedAsset, bool inline, string scopeId, bool required)
        {
            OwnerNode = ownerNode;
            Key = key;
            Label = label;
            Tree = tree;
            SharedAsset = sharedAsset;
            Inline = inline;
            ScopeId = scopeId;
            Required = required;
        }

        public NodeGraphReference(BaseNode ownerNode, string key, string label, BaseTree tree, string scopeId, bool required)
            : this(ownerNode, key, label, tree, null, true, scopeId, required)
        {
        }
    }

    public readonly struct NodeAssetReference
    {
        public readonly BaseNode OwnerNode;
        public readonly string Key;
        public readonly string Label;
        public readonly UnityEngine.Object Asset;
        public readonly bool Required;

        public NodeAssetReference(BaseNode ownerNode, string key, string label, UnityEngine.Object asset, bool required)
        {
            OwnerNode = ownerNode;
            Key = key;
            Label = label;
            Asset = asset;
            Required = required;
        }
    }
}
