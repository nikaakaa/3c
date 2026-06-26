using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("State Machine")]
    [NodeColor(118, 167, 255)]
    [NodePath("Base/Nesting/StateMachine")]
    [Input("Input")]
    public sealed class StateMachineNode : RunnableNode
    {
        [SerializeField]
        string m_InputEdgeGUID;
        public string InputEdgeGUID => m_InputEdgeGUID;

        [NonSerialized]
        RunnableNode m_Parent;
        public RunnableNode Parent => m_Parent;

        [NonSerialized]
        StateMachineGraph m_RuntimeGraph;

        [NonSerialized]
        StateMachineGraphRuntime m_StateMachineRuntime;

        public BaseTree Graph => GetModule<ScopedGraphReferenceModule>()?.Graph;

        public bool CanReferenceGraph(BaseTree graph)
        {
            return CanReferenceGraph(Owner, graph);
        }

        public static bool CanReferenceGraph(BaseGraph owner, BaseTree graph)
        {
            return !(owner is StateMachineGraph) && graph is StateMachineGraph;
        }

        protected override IEnumerable<NodeModule> CreateDefaultModules()
        {
            yield return new ScopedGraphReferenceModule();
        }

        public override void Init(BaseGraph tree)
        {
            base.Init(tree);
            ResolveFlowLinks();
        }

        public override void Dispose()
        {
            DisposeRuntimeGraph();
            base.Dispose();
            m_Parent = null;
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_InputEdgeGUID = string.Empty;
            m_Parent = null;
            m_RuntimeGraph = null;
            m_StateMachineRuntime = null;
        }

        protected override void OnStart()
        {
            base.OnStart();
            EnsureRuntimeGraph();
        }

        protected override State OnUpdate()
        {
            if (!CanRunFromParent())
                return State.None;

            EnsureRuntimeGraph();
            if (!m_RuntimeGraph)
                return State.Failure;

            if (m_StateMachineRuntime == null)
                m_StateMachineRuntime = new StateMachineGraphRuntime(m_RuntimeGraph);

            return m_StateMachineRuntime.Update(Owner.DeltaTime);
        }

        protected override void OnStop()
        {
            m_StateMachineRuntime?.Stop();
        }

        protected override void OnReset()
        {
            base.OnReset();
            m_StateMachineRuntime?.Reset();
        }

        void ResolveFlowLinks()
        {
            m_InputEdgeGUID = string.Empty;
            m_Parent = null;

            foreach (var inputEdge in m_Owner.GetInputEdges(this, "Input"))
            {
                m_InputEdgeGUID = inputEdge.GUID;
                m_Parent = inputEdge.StartNode as RunnableNode;
                break;
            }
        }

        bool CanRunFromParent()
        {
            return Owner != null && (m_Parent == null || m_Parent.State == State.Running);
        }

        void EnsureRuntimeGraph()
        {
            if (m_RuntimeGraph)
                return;

            if (Owner == null)
                return;

            BaseTree graph = Graph;
            if (!CanReferenceGraph(graph))
                return;

            m_RuntimeGraph = Application.isPlaying
                ? UnityEngine.Object.Instantiate((StateMachineGraph)graph)
                : (StateMachineGraph)graph;
            m_RuntimeGraph.InitTree(Owner.User);
            m_StateMachineRuntime = new StateMachineGraphRuntime(m_RuntimeGraph);
        }

        void DisposeRuntimeGraph()
        {
            m_StateMachineRuntime?.Dispose();

            if (m_RuntimeGraph)
                m_RuntimeGraph.DisposeTree();

            m_RuntimeGraph = null;
            m_StateMachineRuntime = null;
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

        public override void OnMoved()
        {
            base.OnMoved();
            if (m_Parent is CompositeNode compositeNode)
                compositeNode.OrderChildren();
        }
#endif
    }
}
