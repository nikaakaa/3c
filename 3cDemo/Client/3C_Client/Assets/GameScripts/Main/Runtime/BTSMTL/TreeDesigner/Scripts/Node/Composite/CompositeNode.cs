using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeColor(6, 214, 160)]
    [Input("Input"), Output("Output", PortCapacity.Multi)]
    public abstract partial class CompositeNode : RunnableNode
    {
        protected sealed class ChildSlot
        {
            ConditionRuleGraphRuntime m_ConditionRuntime;
            bool m_ConditionErrorReported;

            public ChildSlot(BaseEdge edge, RunnableNode child)
            {
                Edge = edge;
                Child = child;
                FlowOrder = edge?.FlowOrder ?? 0;
            }

            public BaseEdge Edge { get; }
            public RunnableNode Child { get; }
            public int FlowOrder { get; }
            public BTAbortPolicy AbortPolicy => Edge != null ? Edge.AbortPolicy : BTAbortPolicy.None;
            public string ConditionError { get; private set; }

            public bool IsConditionMet(BaseGraph context)
            {
                ConditionError = string.Empty;
                if (Edge == null || Edge.ConditionRuleGraphReferenceStatus == ConditionRuleGraphReferenceStatus.Unspecified)
                    return true;

                if (!Edge.TryResolveConditionRuleGraph(out ConditionRuleGraph graph, out string error))
                {
                    ConditionError = error;
                    if (!m_ConditionErrorReported)
                    {
                        Debug.LogError($"BT condition edge is invalid: owner={context?.name}/{context?.GraphAuthoringId} edge={Edge.GUID} ownership={Edge.ConditionRuleGraphOwnership} reason={error}", context?.SerializedOwner);
                        m_ConditionErrorReported = true;
                    }
                    return false;
                }

                m_ConditionRuntime ??= new ConditionRuleGraphRuntime(graph, Edge);
                return m_ConditionRuntime.Evaluate(context);
            }

            public void Reset()
            {
                m_ConditionRuntime?.Reset();
                Child?.ResetNode();
            }

            public void Dispose()
            {
                m_ConditionRuntime?.Dispose();
                m_ConditionRuntime = null;
            }
        }

        [SerializeField]
        string m_InputEdgeGUID;
        public string InputEdgeGUID => m_InputEdgeGUID;

        [SerializeField]
        protected List<string> m_OutputEdgeGUIDs = new List<string>();
        public List<string> OutputGUIDs => m_OutputEdgeGUIDs;

        [NonSerialized]
        protected RunnableNode m_Parent;
        public RunnableNode Parent => m_Parent;

        [NonSerialized]
        protected List<RunnableNode> m_Children = new List<RunnableNode>();
        public List<RunnableNode> Children => m_Children;

        [NonSerialized]
        protected List<ChildSlot> m_ChildSlots = new List<ChildSlot>();

        public override void Init(BaseGraph tree)
        {
            base.Init(tree);

            m_InputEdgeGUID = string.Empty;
            m_Parent = null;
            foreach (var inputEdge in m_Owner.GetInputEdges(this, "Input"))
            {
                m_InputEdgeGUID = inputEdge.GUID;
                m_Parent = inputEdge.StartNode as RunnableNode;
                break;
            }

            m_Children.Clear();
            m_ChildSlots.Clear();
            m_OutputEdgeGUIDs.Clear();
            foreach (var outputEdge in m_Owner.GetOutputEdges(this, "Output").OrderBy(i => i.FlowOrder))
            {
                if (outputEdge.EndNode is RunnableNode child)
                {
                    m_OutputEdgeGUIDs.Add(outputEdge.GUID);
                    m_Children.Add(child);
                    m_ChildSlots.Add(new ChildSlot(outputEdge, child));
                }
            }
        }
        public override void Dispose()
        {
            base.Dispose();

            m_ChildSlots.ForEach(i => i.Dispose());
            m_ChildSlots.Clear();
            m_Parent = null;
            m_Children.Clear();
        }
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_InputEdgeGUID = string.Empty;
            m_Parent = null;
            m_OutputEdgeGUIDs.Clear();
            m_Children.Clear();
            m_ChildSlots.Clear();
        }
        public override void ResetNode()
        {
            base.ResetNode();
            m_ChildSlots.ForEach(i => i.Reset());
        }
        protected override NodeStopStatus OnStopRequested(NodeStopContext context)
        {
            return StopActiveSlots(context);
        }

        protected override NodeStopStatus OnStopping(NodeStopContext context)
        {
            return StopActiveSlots(context);
        }

        protected override void OnForceStopped(NodeStopContext context)
        {
            foreach (ChildSlot slot in m_ChildSlots)
            {
                if (slot?.Child?.LifecyclePhase != NodeLifecyclePhase.Dormant)
                    ForceStopChild(slot.Child, context, slot.Edge?.GUID);
            }
        }

        protected int ChildSlotCount => m_ChildSlots.Count;

        protected bool TryGetChildSlot(int index, out ChildSlot slot)
        {
            if (index >= 0 && index < m_ChildSlots.Count)
            {
                slot = m_ChildSlots[index];
                return true;
            }

            slot = null;
            return false;
        }

        protected bool IsSlotConditionMet(ChildSlot slot)
        {
            bool result = slot != null && slot.IsConditionMet(m_Owner);
            if (slot?.Edge != null)
            {
                if (string.IsNullOrEmpty(slot.ConditionError))
                    TreeRuntimeDiagnostics.PublishEdge(m_Owner, slot.Edge, RuntimeTraceEventKind.EdgeEvaluated, result);
                else
                    TreeRuntimeDiagnostics.PublishInvalidConditionEdge(m_Owner, slot.Edge, slot.ConditionError);
            }
            return result;
        }

        protected State UpdateSlot(ChildSlot slot)
        {
            if (slot?.Edge != null)
                TreeRuntimeDiagnostics.PublishEdge(m_Owner, slot.Edge, RuntimeTraceEventKind.EdgeSelected, true);
            return slot?.Child != null ? UpdateChild(slot.Child, slot.Edge?.GUID) : State.Failure;
        }

        protected NodeStopStatus StopSlot(ChildSlot slot, NodeStopContext context)
        {
            return slot?.Child != null
                ? RequestChildStop(slot.Child, context, slot.Edge?.GUID)
                : NodeStopStatus.Completed;
        }

        protected NodeStopContext CreateSlotStopContext(
            NodeStopOriginCause cause,
            ChildSlot initiator,
            ChildSlot source,
            ChildSlot replacement = null)
        {
            return CreateStopContext(
                cause,
                initiator?.Edge,
                source?.Child,
                source?.Edge,
                replacement?.Child,
                replacement?.Edge);
        }

        NodeStopStatus StopActiveSlots(NodeStopContext context)
        {
            NodeStopStatus aggregate = NodeStopStatus.Completed;
            foreach (ChildSlot slot in m_ChildSlots)
            {
                if (slot?.Child == null || slot.Child.LifecyclePhase == NodeLifecyclePhase.Dormant)
                    continue;

                NodeStopStatus status = StopSlot(slot, context);
                if (status == NodeStopStatus.Failed)
                    return NodeStopStatus.Failed;
                if (status == NodeStopStatus.Running)
                    aggregate = NodeStopStatus.Running;
            }

            return aggregate;
        }

        protected static bool UsesSelfAbort(BTAbortPolicy policy)
        {
            return policy == BTAbortPolicy.Self || policy == BTAbortPolicy.Both;
        }

        protected static bool UsesLowerPriorityAbort(BTAbortPolicy policy)
        {
            return policy == BTAbortPolicy.LowerPriority || policy == BTAbortPolicy.Both;
        }

#if UNITY_EDITOR
        public override void OnInputLinked(BaseEdge edge)
        {
            base.OnInputLinked(edge);

            m_InputEdgeGUID = edge.GUID;
            m_Parent = edge.StartNode as RunnableNode;
        }
        public override void OnInputUnlinked(BaseEdge edge)
        {
            base.OnInputUnlinked(edge);

            m_InputEdgeGUID = string.Empty;
            m_Parent = null;
        }
        public override void OnOutputLinked(BaseEdge edge)
        {
            base.OnOutputLinked(edge);

            m_OutputEdgeGUIDs.Add(edge.GUID);
            m_Children.Add(edge.EndNode as RunnableNode);

            OrderChildren();
        }
        public override void OnOutputUnlinked(BaseEdge edge)
        {
            base.OnOutputUnlinked(edge);

            m_OutputEdgeGUIDs.Remove(edge.GUID);
            m_Children.Remove(edge.EndNode as RunnableNode);

            OrderChildren();
        }
        public override void OnMoved()
        {
            base.OnMoved();
            if (m_Parent is CompositeNode compositeNode)
                compositeNode.OrderChildren();
        }
        public void OrderChildren()
        {
            m_Children = m_Children.OrderBy(i => i.Position.y).ToList();
            m_OutputEdgeGUIDs = m_OutputEdgeGUIDs.OrderBy(i =>
            {
                if (!m_Owner.GUIDEdgeMap.ContainsKey(i)) return 0f;
                return m_Owner.GUIDEdgeMap[i].EndNode.Position.y;
            }).ToList();

            for (int i = 0; i < m_OutputEdgeGUIDs.Count; i++)
            {
                if (m_Owner.GUIDEdgeMap.TryGetValue(m_OutputEdgeGUIDs[i], out BaseEdge edge))
                    edge.FlowOrder = i;
            }
        }
#endif
    }
}

