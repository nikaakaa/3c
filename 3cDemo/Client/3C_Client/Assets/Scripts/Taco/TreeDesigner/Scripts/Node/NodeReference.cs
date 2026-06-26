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
        public readonly string ScopeId;
        public readonly bool Required;

        public NodeGraphReference(BaseNode ownerNode, string key, string label, BaseTree tree, string scopeId, bool required)
        {
            OwnerNode = ownerNode;
            Key = key;
            Label = label;
            Tree = tree;
            ScopeId = scopeId;
            Required = required;
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
