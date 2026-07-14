using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    public sealed class TreeReferenceModule : NodeModule
    {
        [SerializeField, ShowInPanel("Shared Tree")]
        BaseTreeAsset m_SharedTreeAsset;

        public override string DefaultModuleId => "treeReference";
        public BaseTree Tree => m_SharedTreeAsset ? m_SharedTreeAsset.Tree : null;
        public BaseTreeAsset SharedTreeAsset => m_SharedTreeAsset;

        public void SetTreeAsset(BaseTreeAsset treeAsset)
        {
            m_SharedTreeAsset = treeAsset;
        }

        public override IEnumerable<NodeGraphReference> GetGraphReferences()
        {
            yield return new NodeGraphReference(Owner, $"{ModuleId}.m_SharedTreeAsset", "Tree", Tree, m_SharedTreeAsset, false, string.Empty, true);
        }
    }

    [Serializable]
    public sealed class ScopedGraphReferenceModule : NodeModule
    {
        [SerializeReference]
        StateMachineGraph m_InlineGraph;

        [SerializeField, OnValueChanged(nameof(OnSharedGraphAssetChanged))]
        BaseTreeAsset m_SharedGraphAsset;

        [SerializeField]
        string m_ScopeId;

        public override string DefaultModuleId => "scopedGraph";
        public StateMachineGraph Graph => m_SharedGraphAsset ? m_SharedGraphAsset.Tree as StateMachineGraph : m_InlineGraph;
        public StateMachineGraph InlineGraph => m_InlineGraph;
        public BaseTreeAsset SharedGraphAsset => m_SharedGraphAsset;
        public string ScopeId => m_ScopeId;

        public void SetInlineGraph(StateMachineGraph graph)
        {
            m_InlineGraph = graph;
            m_SharedGraphAsset = null;
            BindInlineGraph();
        }

        public void SetSharedGraphAsset(BaseTreeAsset graphAsset)
        {
            m_SharedGraphAsset = graphAsset;
            if (m_SharedGraphAsset)
                m_InlineGraph = null;
        }

#if UNITY_EDITOR
        public void RegenerateScopeId(string ownerAuthoringId)
        {
            m_ScopeId = ownerAuthoringId ?? string.Empty;
        }
#endif

        void OnSharedGraphAssetChanged()
        {
            if (m_SharedGraphAsset)
                m_InlineGraph = null;
        }

        public override void Init(BaseNode owner, string defaultModuleId)
        {
            base.Init(owner, defaultModuleId);
            if (string.IsNullOrEmpty(m_ScopeId))
                m_ScopeId = string.IsNullOrEmpty(owner.GUID) ? ModuleId : owner.GUID;
            BindInlineGraph();
        }

        public override void RebindReadOnlyViewOwner(BaseNode owner)
        {
            base.RebindReadOnlyViewOwner(owner);
            BindInlineGraph();
        }

        public override IEnumerable<NodeGraphReference> GetGraphReferences()
        {
            yield return new NodeGraphReference(Owner, $"{ModuleId}.m_InlineGraph", "Graph", Graph, m_SharedGraphAsset, !m_SharedGraphAsset, m_ScopeId, true);
        }

        int ModuleIndex()
        {
            IReadOnlyList<NodeModule> modules = Owner?.Modules;
            if (modules == null)
                return -1;
            for (int i = 0; i < modules.Count; i++)
            {
                if (ReferenceEquals(modules[i], this))
                    return i;
            }
            return -1;
        }

        void BindInlineGraph()
        {
            if (m_InlineGraph != null && Owner?.Owner != null)
                m_InlineGraph.BindSerializedOwner(Owner.Owner.SerializedOwner, $"{Owner.Owner.GetNodeSerializedPropertyPath(Owner)}.m_Modules.Array.data[{ModuleIndex()}].m_InlineGraph");
        }
    }

    [Serializable]
    public sealed class StateBehaviorGraphReferenceModule : NodeModule
    {
        [SerializeReference]
        SubTree m_InlineSubTree;

        [SerializeField, OnValueChanged(nameof(OnSharedSubTreeAssetChanged))]
        BaseTreeAsset m_SharedSubTreeAsset;

        [SerializeField]
        string m_ScopeId;

        public override string DefaultModuleId => "stateBehaviorGraph";
        public SubTree SubTree => m_SharedSubTreeAsset ? m_SharedSubTreeAsset.Tree as SubTree : m_InlineSubTree;
        public SubTree InlineSubTree => m_InlineSubTree;
        public BaseTreeAsset SharedSubTreeAsset => m_SharedSubTreeAsset;
        public string ScopeId => m_ScopeId;

        public void SetInlineSubTree(SubTree subTree)
        {
            m_InlineSubTree = subTree;
            m_SharedSubTreeAsset = null;
            BindInlineSubTree();
        }

        public void SetSharedSubTreeAsset(BaseTreeAsset subTreeAsset)
        {
            m_SharedSubTreeAsset = subTreeAsset;
            if (m_SharedSubTreeAsset)
                m_InlineSubTree = null;
        }

#if UNITY_EDITOR
        public void RegenerateScopeId(string ownerAuthoringId)
        {
            m_ScopeId = ownerAuthoringId ?? string.Empty;
        }
#endif

        void OnSharedSubTreeAssetChanged()
        {
            if (m_SharedSubTreeAsset)
                m_InlineSubTree = null;
        }

        public static bool CanReferenceTree(BaseTree tree)
        {
            return tree is SubTree;
        }

        public override void Init(BaseNode owner, string defaultModuleId)
        {
            base.Init(owner, defaultModuleId);
            if (string.IsNullOrEmpty(m_ScopeId))
                m_ScopeId = string.IsNullOrEmpty(owner.GUID) ? ModuleId : owner.GUID;
            BindInlineSubTree();
        }

        public override void RebindReadOnlyViewOwner(BaseNode owner)
        {
            base.RebindReadOnlyViewOwner(owner);
            BindInlineSubTree();
        }

        public override IEnumerable<NodeGraphReference> GetGraphReferences()
        {
            yield return new NodeGraphReference(Owner, $"{ModuleId}.m_InlineSubTree", "SubTree", SubTree, m_SharedSubTreeAsset, !m_SharedSubTreeAsset, m_ScopeId, false);
        }

        int ModuleIndex()
        {
            IReadOnlyList<NodeModule> modules = Owner?.Modules;
            if (modules == null)
                return -1;
            for (int i = 0; i < modules.Count; i++)
            {
                if (ReferenceEquals(modules[i], this))
                    return i;
            }
            return -1;
        }

        void BindInlineSubTree()
        {
            if (m_InlineSubTree != null && Owner?.Owner != null)
                m_InlineSubTree.BindSerializedOwner(Owner.Owner.SerializedOwner, $"{Owner.Owner.GetNodeSerializedPropertyPath(Owner)}.m_Modules.Array.data[{ModuleIndex()}].m_InlineSubTree");
        }
    }
}
