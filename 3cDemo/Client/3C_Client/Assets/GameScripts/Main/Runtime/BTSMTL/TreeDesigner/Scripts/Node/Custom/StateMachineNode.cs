using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("State Machine")]
    [NodeColor(118, 167, 255)]
    [NodePath("Base/Nesting/StateMachine")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
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

        public StateMachineGraph Graph => GetModule<ScopedGraphReferenceModule>()?.Graph;
        [ShowInInspector("Active State")]
        public string ActiveStateDebug => StateLabel(m_StateMachineRuntime?.ActiveState);
        [ShowInInspector("Exiting State")]
        public string ExitingStateDebug => StateLabel(m_StateMachineRuntime?.ExitingState);
        [ShowInInspector("Target State")]
        public string TargetStateDebug => StateLabel(m_StateMachineRuntime?.PendingTargetState);
        [ShowInInspector("State Exit Context")]
        public string StateExitContextDebug => FormatExitContext(m_StateMachineRuntime?.PendingExitContext ?? default);

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

#if UNITY_EDITOR
        public override void OnCreated()
        {
            base.OnCreated();
            EnsureInlineStateMachineGraph();
        }
#endif

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

        protected override NodeStopStatus OnStopRequested(NodeStopContext context)
        {
            return m_StateMachineRuntime != null
                ? m_StateMachineRuntime.RequestExit(context, Owner?.DeltaTime ?? 0f)
                : NodeStopStatus.Completed;
        }

        protected override NodeStopStatus OnStopping(NodeStopContext context)
        {
            return m_StateMachineRuntime != null
                ? m_StateMachineRuntime.RequestExit(context, Owner?.DeltaTime ?? 0f)
                : NodeStopStatus.Completed;
        }

        protected override void OnForceStopped(NodeStopContext context)
        {
            m_StateMachineRuntime?.ForceStop(context);
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

            StateMachineGraph graph = Graph;
            if (!CanReferenceGraph(graph))
                return;

            m_RuntimeGraph = Application.isPlaying ? graph.Clone() : graph;
            ScopedGraphReferenceModule module = GetModule<ScopedGraphReferenceModule>();
            TreeAuthoringRouteId route = TreeAuthoringRouteBuilder.AppendNodeGraph(
                Owner,
                this,
                $"{module.ModuleId}.m_InlineGraph",
                module.ScopeId,
                m_RuntimeGraph,
                module.SharedGraphAsset ? TreeGraphReferenceOwnership.Shared : TreeGraphReferenceOwnership.Inline);
            m_RuntimeGraph.InitTree(Owner.User, Owner, route);
            m_StateMachineRuntime = new StateMachineGraphRuntime(m_RuntimeGraph);
        }

#if UNITY_EDITOR
        void EnsureInlineStateMachineGraph()
        {
            ScopedGraphReferenceModule module = GetModule<ScopedGraphReferenceModule>();
            if (module == null || module.Graph != null)
                return;

            module.SetInlineGraph(CreateDefaultGraph());
        }

        public static StateMachineGraph CreateDefaultGraph()
        {
            StateMachineGraph graph = new StateMachineGraph();
            graph.name = "State Machine";

            StateMachineEnterNode enterNode = graph.CreateNode(typeof(StateMachineEnterNode)) as StateMachineEnterNode;
            StateMachineAnyStateNode anyStateNode = graph.CreateNode(typeof(StateMachineAnyStateNode)) as StateMachineAnyStateNode;
            StateMachineExitNode exitNode = graph.CreateNode(typeof(StateMachineExitNode)) as StateMachineExitNode;
            StateNode stateNode = graph.CreateNode(typeof(StateNode)) as StateNode;

            enterNode.Position = new Vector2(-360f, -120f);
            anyStateNode.Position = new Vector2(-360f, 120f);
            stateNode.Position = Vector2.zero;
            exitNode.Position = new Vector2(360f, 0f);
            graph.Link(enterNode, stateNode, StateMachinePorts.StateOut, StateMachinePorts.StateIn);
            return graph;
        }
#endif

        void DisposeRuntimeGraph()
        {
            m_StateMachineRuntime?.Dispose();

            if (m_RuntimeGraph)
                m_RuntimeGraph.DisposeTree();

            m_RuntimeGraph = null;
            m_StateMachineRuntime = null;
        }

        static string StateLabel(StateNode state)
        {
            return state != null ? $"{state.DisplayName}/{state.GUID}" : string.Empty;
        }

        static string FormatExitContext(StateExitContext context)
        {
            return context.IsValid
                ? $"{context.Cause} {context.SourceStateGuid}->{context.TargetStateGuid} tree:{context.ParentSourceNodeGuid}->{context.ReplacementNodeGuid}"
                : string.Empty;
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
