using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    public sealed class TreeReferenceModule : NodeModule
    {
        [SerializeField, ShowInPanel("Tree")]
        BaseTree m_Tree;

        public override string DefaultModuleId => "treeReference";
        public BaseTree Tree => m_Tree;

        public override IEnumerable<NodeGraphReference> GetGraphReferences()
        {
            yield return new NodeGraphReference(Owner, $"{ModuleId}.m_Tree", "Tree", m_Tree, string.Empty, true);
        }
    }

    [Serializable]
    public sealed class ScopedGraphReferenceModule : NodeModule
    {
        [SerializeField, ShowInPanel("Graph")]
        BaseTree m_Graph;

        [SerializeField, ShowInPanel("Scope Id"), ReadOnly]
        string m_ScopeId;

        public override string DefaultModuleId => "scopedGraph";
        public BaseTree Graph => m_Graph;
        public string ScopeId => m_ScopeId;

        public override void Init(BaseNode owner, string defaultModuleId)
        {
            base.Init(owner, defaultModuleId);
            if (string.IsNullOrEmpty(m_ScopeId))
                m_ScopeId = string.IsNullOrEmpty(owner.GUID) ? ModuleId : owner.GUID;
        }

        public override IEnumerable<NodeGraphReference> GetGraphReferences()
        {
            yield return new NodeGraphReference(Owner, $"{ModuleId}.m_Graph", "Graph", m_Graph, m_ScopeId, true);
        }
    }

    [Serializable]
    public sealed class StateBehaviorGraphReferenceModule : NodeModule
    {
        [SerializeField, ShowInPanel("SubTree")]
        SubTree m_SubTree;

        [SerializeField, ShowInPanel("Scope Id"), ReadOnly]
        string m_ScopeId;

        public override string DefaultModuleId => "stateBehaviorGraph";
        public SubTree SubTree => m_SubTree;
        public string ScopeId => m_ScopeId;

        public static bool CanReferenceTree(BaseTree tree)
        {
            return tree is SubTree;
        }

        public override void Init(BaseNode owner, string defaultModuleId)
        {
            base.Init(owner, defaultModuleId);
            if (string.IsNullOrEmpty(m_ScopeId))
                m_ScopeId = string.IsNullOrEmpty(owner.GUID) ? ModuleId : owner.GUID;
        }

        public override IEnumerable<NodeGraphReference> GetGraphReferences()
        {
            yield return new NodeGraphReference(Owner, $"{ModuleId}.m_SubTree", "SubTree", m_SubTree, m_ScopeId, false);
        }
    }
}
