using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    public enum BTAbortPolicy
    {
        None,
        Self,
        LowerPriority,
        Both
    }

    public enum ConditionRuleGraphOwnership
    {
        Unspecified,
        Inline,
        Shared
    }

    public enum ConditionRuleGraphReferenceStatus
    {
        Unspecified,
        ResolvedInline,
        ResolvedShared,
        InvalidOwnership,
        UnspecifiedWithSource,
        InlineGraphMissing,
        InlineWithSharedSource,
        SharedAssetMissing,
        SharedAssetTypeMismatch,
        SharedWithInlineSource
    }

    [Serializable]
    public partial class BaseEdge
    {
        [SerializeField]
        protected string m_GUID;
        public string GUID => m_GUID;

        [SerializeField]
        protected string m_StartNodeGUID;
        public string StartNodeGUID => m_StartNodeGUID;

        [SerializeField]
        protected string m_EndNodeGUID;
        public string EndNodeGUID => m_EndNodeGUID;

        [SerializeField]
        protected string m_StartPortName;
        public string StartPortName => m_StartPortName;

        [SerializeField]
        protected string m_EndPortName;
        public string EndPortName => m_EndPortName;

        [SerializeField]
        protected int m_FlowOrder;
        public int FlowOrder { get => m_FlowOrder; set => m_FlowOrder = value; }

        [SerializeField]
        protected int m_TransitionPriority;
        public int TransitionPriority { get => m_TransitionPriority; set => m_TransitionPriority = value; }

        [SerializeField]
        protected BTAbortPolicy m_AbortPolicy;
        public BTAbortPolicy AbortPolicy { get => m_AbortPolicy; set => m_AbortPolicy = value; }

        [SerializeReference]
        protected ConditionRuleGraph m_InlineConditionRuleGraph;

        [SerializeField]
        protected BaseTreeAsset m_SharedConditionRuleGraphAsset;

        [SerializeField]
        protected ConditionRuleGraphOwnership m_ConditionRuleGraphOwnership;

        public ConditionRuleGraphOwnership ConditionRuleGraphOwnership => m_ConditionRuleGraphOwnership;
        public ConditionRuleGraphReferenceStatus ConditionRuleGraphReferenceStatus => ResolveConditionRuleGraphReferenceStatus();
        public ConditionRuleGraph ConditionRuleGraph => TryResolveConditionRuleGraph(out ConditionRuleGraph graph, out _) ? graph : null;
        public ConditionRuleGraph InlineConditionRuleGraph => m_InlineConditionRuleGraph;
        public BaseTreeAsset SharedConditionRuleGraphAsset => m_SharedConditionRuleGraphAsset;
        public bool HasResolvedConditionRuleGraph => ConditionRuleGraphReferenceStatus == ConditionRuleGraphReferenceStatus.ResolvedInline ||
                                                     ConditionRuleGraphReferenceStatus == ConditionRuleGraphReferenceStatus.ResolvedShared;
        public bool HasConditionRuleGraphConfiguration => m_ConditionRuleGraphOwnership != ConditionRuleGraphOwnership.Unspecified ||
                                                          m_InlineConditionRuleGraph != null ||
                                                          m_SharedConditionRuleGraphAsset;
        public string ConditionRuleGraphReferenceError => DescribeConditionRuleGraphReferenceError(ConditionRuleGraphReferenceStatus);

        [NonSerialized]
        protected BaseGraph m_Owner;
        public BaseGraph Owner => m_Owner;

        [NonSerialized]
        protected BaseNode m_StartNode;
        public BaseNode StartNode => m_StartNode;

        [NonSerialized]
        protected BaseNode m_EndNode;
        public BaseNode EndNode => m_EndNode;

        public BaseEdge() { }
        public BaseEdge(BaseNode startNode, BaseNode endNode, string startPortName, string endPortName)
        {
            m_GUID = Guid.NewGuid().ToString();
            m_StartNodeGUID = startNode.GUID;
            m_EndNodeGUID = endNode.GUID;
            m_StartPortName = startPortName;
            m_EndPortName = endPortName;

            m_StartNode = startNode;
            m_EndNode = endNode;
        }

        public virtual void Init(BaseGraph tree)
        {
            RebindReferences(tree);
        }

        public virtual void RebindReferences(BaseGraph tree)
        {
            m_Owner = tree;
            m_StartNode = null;
            m_EndNode = null;
            if (m_Owner.GUIDNodeMap.TryGetValue(m_StartNodeGUID, out BaseNode startNode))
                m_StartNode = startNode;
            if (m_Owner.GUIDNodeMap.TryGetValue(m_EndNodeGUID, out BaseNode endNode))
                m_EndNode = endNode;
            BindInlineConditionRuleGraph();
        }
        public virtual void Dispose()
        {
            m_Owner = null;
            m_StartNode = null;
            m_EndNode = null;
        }

        public void SetConditionRuleGraph(ConditionRuleGraph graph)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));

            m_InlineConditionRuleGraph = graph;
            m_SharedConditionRuleGraphAsset = null;
            m_ConditionRuleGraphOwnership = ConditionRuleGraphOwnership.Inline;
            BindInlineConditionRuleGraph();
        }

        public void SetConditionRuleGraphAsset(BaseTreeAsset asset)
        {
            if (!asset)
                throw new ArgumentNullException(nameof(asset));
            if (!(asset.Tree is ConditionRuleGraph))
                throw new ArgumentException("Shared condition rule asset must contain ConditionRuleGraph.", nameof(asset));

            m_SharedConditionRuleGraphAsset = asset;
            m_InlineConditionRuleGraph = null;
            m_ConditionRuleGraphOwnership = ConditionRuleGraphOwnership.Shared;
        }

        public void ClearConditionRuleGraph()
        {
            m_InlineConditionRuleGraph = null;
            m_SharedConditionRuleGraphAsset = null;
            m_ConditionRuleGraphOwnership = ConditionRuleGraphOwnership.Unspecified;
        }

        public bool TryResolveConditionRuleGraph(out ConditionRuleGraph graph, out string error)
        {
            ConditionRuleGraphReferenceStatus status = ConditionRuleGraphReferenceStatus;
            switch (status)
            {
                case ConditionRuleGraphReferenceStatus.ResolvedInline:
                    graph = m_InlineConditionRuleGraph;
                    error = string.Empty;
                    return true;
                case ConditionRuleGraphReferenceStatus.ResolvedShared:
                    graph = m_SharedConditionRuleGraphAsset.Tree as ConditionRuleGraph;
                    error = string.Empty;
                    return true;
                default:
                    graph = null;
                    error = DescribeConditionRuleGraphReferenceError(status);
                    return false;
            }
        }

#if UNITY_EDITOR
        public void Retarget(BaseNode startNode, BaseNode endNode)
        {
            if (startNode == null || endNode == null)
                throw new ArgumentNullException(startNode == null ? nameof(startNode) : nameof(endNode));
            m_StartNodeGUID = startNode.GUID;
            m_EndNodeGUID = endNode.GUID;
            m_StartNode = startNode;
            m_EndNode = endNode;
        }
#endif

        void BindInlineConditionRuleGraph()
        {
            if (m_ConditionRuleGraphOwnership != ConditionRuleGraphOwnership.Inline ||
                m_InlineConditionRuleGraph == null ||
                m_Owner == null)
                return;

            string edgePath = m_Owner.GetEdgeSerializedPropertyPath(this);
            if (!string.IsNullOrEmpty(edgePath))
                m_InlineConditionRuleGraph.BindSerializedOwner(m_Owner.SerializedOwner, $"{edgePath}.m_InlineConditionRuleGraph");
        }

        ConditionRuleGraphReferenceStatus ResolveConditionRuleGraphReferenceStatus()
        {
            switch (m_ConditionRuleGraphOwnership)
            {
                case ConditionRuleGraphOwnership.Unspecified:
                    return m_InlineConditionRuleGraph != null || m_SharedConditionRuleGraphAsset
                        ? ConditionRuleGraphReferenceStatus.UnspecifiedWithSource
                        : ConditionRuleGraphReferenceStatus.Unspecified;
                case ConditionRuleGraphOwnership.Inline:
                    if (m_SharedConditionRuleGraphAsset)
                        return ConditionRuleGraphReferenceStatus.InlineWithSharedSource;
                    return m_InlineConditionRuleGraph != null
                        ? ConditionRuleGraphReferenceStatus.ResolvedInline
                        : ConditionRuleGraphReferenceStatus.InlineGraphMissing;
                case ConditionRuleGraphOwnership.Shared:
                    if (m_InlineConditionRuleGraph != null)
                        return ConditionRuleGraphReferenceStatus.SharedWithInlineSource;
                    if (!m_SharedConditionRuleGraphAsset)
                        return ConditionRuleGraphReferenceStatus.SharedAssetMissing;
                    return m_SharedConditionRuleGraphAsset.Tree is ConditionRuleGraph
                        ? ConditionRuleGraphReferenceStatus.ResolvedShared
                        : ConditionRuleGraphReferenceStatus.SharedAssetTypeMismatch;
                default:
                    return ConditionRuleGraphReferenceStatus.InvalidOwnership;
            }
        }

        static string DescribeConditionRuleGraphReferenceError(ConditionRuleGraphReferenceStatus status)
        {
            switch (status)
            {
                case ConditionRuleGraphReferenceStatus.Unspecified:
                    return "ConditionRuleGraph ownership is Unspecified.";
                case ConditionRuleGraphReferenceStatus.InvalidOwnership:
                    return "ConditionRuleGraph ownership value is invalid.";
                case ConditionRuleGraphReferenceStatus.UnspecifiedWithSource:
                    return "ConditionRuleGraph has serialized data while ownership is Unspecified.";
                case ConditionRuleGraphReferenceStatus.InlineGraphMissing:
                    return "Inline ownership requires inline ConditionRuleGraph data.";
                case ConditionRuleGraphReferenceStatus.InlineWithSharedSource:
                    return "Inline ownership cannot retain a shared ConditionRuleGraph asset.";
                case ConditionRuleGraphReferenceStatus.SharedAssetMissing:
                    return "Shared ownership requires a resolved ConditionRuleGraph asset.";
                case ConditionRuleGraphReferenceStatus.SharedAssetTypeMismatch:
                    return "Shared ownership asset does not contain ConditionRuleGraph.";
                case ConditionRuleGraphReferenceStatus.SharedWithInlineSource:
                    return "Shared ownership cannot retain inline ConditionRuleGraph data.";
                default:
                    return string.Empty;
            }
        }

        public static implicit operator bool(BaseEdge exists) => exists != null;
    }
}
