using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("State")]
    [NodeColor(118, 167, 255)]
    [NodePath("Base/StateMachine/State")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public class StateNode : RunnableNode
    {
        [SerializeField]
        List<string> m_InputEdgeGUIDs = new List<string>();
        public IReadOnlyList<string> InputEdgeGUIDs => m_InputEdgeGUIDs;

        [SerializeField]
        List<string> m_OutputEdgeGUIDs = new List<string>();
        public IReadOnlyList<string> OutputEdgeGUIDs => m_OutputEdgeGUIDs;

        [NonSerialized]
        SubTree m_RuntimeSubTree;

        [NonSerialized]
        State m_LastRootStatus;

        public SubTree SubTree => GetModule<StateBehaviorGraphReferenceModule>()?.SubTree;
        public SubTree RuntimeSubTree => m_RuntimeSubTree;
        public State LastRootStatus => m_LastRootStatus;

        public static bool CanReferenceGraph(BaseGraph owner, BaseTree graph)
        {
            return owner is StateMachineGraph && StateBehaviorGraphReferenceModule.CanReferenceTree(graph);
        }

        protected override IEnumerable<NodeModule> CreateDefaultModules()
        {
            yield return new StateBehaviorGraphReferenceModule();
        }

#if UNITY_EDITOR
        public override void OnCreated()
        {
            base.OnCreated();
            EnsureInlineStateBehaviorGraph();
        }
#endif

        public override void Init(BaseGraph tree)
        {
            base.Init(tree);
            ResolveFlowLinks();
        }

        public override void Dispose()
        {
            DisposeRuntimeGraph();
            base.Dispose();
            if (m_InputEdgeGUIDs == null)
                m_InputEdgeGUIDs = new List<string>();
            m_InputEdgeGUIDs.Clear();
            if (m_OutputEdgeGUIDs == null)
                m_OutputEdgeGUIDs = new List<string>();
            m_OutputEdgeGUIDs.Clear();
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (m_InputEdgeGUIDs == null)
                m_InputEdgeGUIDs = new List<string>();
            m_InputEdgeGUIDs.Clear();
            if (m_OutputEdgeGUIDs == null)
                m_OutputEdgeGUIDs = new List<string>();
            m_OutputEdgeGUIDs.Clear();
            m_RuntimeSubTree = null;
            m_LastRootStatus = State.None;
        }

        public State UpdateState(float deltaTime)
        {
            if (Owner == null)
                return State.Failure;

            Owner.SetDeltaTime(deltaTime);
            return UpdateNode();
        }

        public bool PrepareRuntimeSubTree()
        {
            EnsureRuntimeGraph();
            return m_RuntimeSubTree != null;
        }

        protected override void OnStart()
        {
            base.OnStart();
            m_LastRootStatus = State.None;
            EnsureRuntimeGraph();
            BeginRuntimeSubTree();
        }

        protected override State OnUpdate()
        {
            if (!SubTree)
                return State.Running;

            return UpdateRuntimeGraph();
        }

        State UpdateRuntimeGraph()
        {
            EnsureRuntimeGraph();
            if (!m_RuntimeSubTree)
            {
                m_LastRootStatus = State.Failure;
                return State.Failure;
            }

            if (m_RuntimeSubTree is StateBehaviorSubTree stateBehaviorSubTree)
                return UpdateStateBehaviorSubTree(stateBehaviorSubTree);

            State graphState = m_RuntimeSubTree.State;
            if (graphState != State.Success && graphState != State.Failure)
                graphState = m_RuntimeSubTree.UpdateTree(Owner.DeltaTime);

            m_LastRootStatus = graphState;
            return graphState == State.Failure ? State.Failure : State.Running;
        }

        State UpdateStateBehaviorSubTree(StateBehaviorSubTree stateBehaviorSubTree)
        {
            State enterState = stateBehaviorSubTree.UpdateStateEnter(Owner.DeltaTime);
            if (enterState == State.Running)
                return State.Running;
            if (enterState == State.Failure)
                return State.Failure;

            State graphState = stateBehaviorSubTree.State;
            if (graphState != State.Success && graphState != State.Failure)
                graphState = stateBehaviorSubTree.UpdateStateRoot(Owner.DeltaTime);

            m_LastRootStatus = graphState;
            return graphState == State.Failure ? State.Failure : State.Running;
        }

        public State UpdateStateExit(float deltaTime, NodeStopContext stopContext)
        {
            if (!SubTree)
                return State.Success;

            EnsureRuntimeGraph();
            if (!m_RuntimeSubTree)
                return State.Failure;

            return m_RuntimeSubTree is StateBehaviorSubTree stateBehaviorSubTree
                ? stateBehaviorSubTree.UpdateStateExit(deltaTime, stopContext)
                : State.Success;
        }

        protected override NodeStopStatus OnStopRequested(NodeStopContext context)
        {
            return m_RuntimeSubTree ? m_RuntimeSubTree.RequestStop(context) : NodeStopStatus.Completed;
        }

        protected override NodeStopStatus OnStopping(NodeStopContext context)
        {
            return m_RuntimeSubTree ? m_RuntimeSubTree.UpdateStopping() : NodeStopStatus.Completed;
        }

        protected override void OnForceStopped(NodeStopContext context)
        {
            m_RuntimeSubTree?.ForceStop(context);
        }

        protected override void OnReset()
        {
            base.OnReset();
            m_LastRootStatus = State.None;
            BeginRuntimeSubTree();
        }

        void ResolveFlowLinks()
        {
            if (m_InputEdgeGUIDs == null)
                m_InputEdgeGUIDs = new List<string>();
            m_InputEdgeGUIDs.Clear();
            if (m_OutputEdgeGUIDs == null)
                m_OutputEdgeGUIDs = new List<string>();
            m_OutputEdgeGUIDs.Clear();

            foreach (var inputEdge in m_Owner.GetInputEdges(this, StateMachinePorts.StateIn))
                m_InputEdgeGUIDs.Add(inputEdge.GUID);

            foreach (var outputEdge in m_Owner.GetOutputEdges(this, StateMachinePorts.StateOut).OrderBy(i => i.FlowOrder))
                m_OutputEdgeGUIDs.Add(outputEdge.GUID);
        }

        void EnsureRuntimeGraph()
        {
            if (m_RuntimeSubTree)
                return;

            if (Owner == null)
                return;

            SubTree subTree = SubTree;
            if (!CanReferenceGraph(Owner, subTree))
                return;

            m_RuntimeSubTree = Application.isPlaying ? subTree.Clone() : subTree;
            StateBehaviorGraphReferenceModule module = GetModule<StateBehaviorGraphReferenceModule>();
            TreeAuthoringRouteId route = TreeAuthoringRouteBuilder.AppendNodeGraph(
                Owner,
                this,
                $"{module.ModuleId}.m_InlineSubTree",
                module.ScopeId,
                m_RuntimeSubTree,
                module.SharedSubTreeAsset ? TreeGraphReferenceOwnership.Shared : TreeGraphReferenceOwnership.Inline);
            m_RuntimeSubTree.InitTree(Owner.User, Owner, route);
        }

#if UNITY_EDITOR
        void EnsureInlineStateBehaviorGraph()
        {
            StateBehaviorGraphReferenceModule module = GetModule<StateBehaviorGraphReferenceModule>();
            if (module == null || module.SubTree != null)
                return;

            module.SetInlineSubTree(CreateDefaultStateBehaviorGraph());
        }

        public static StateBehaviorSubTree CreateDefaultStateBehaviorGraph()
        {
            StateBehaviorSubTree tree = new StateBehaviorSubTree();
            tree.name = "State Behavior";
            RootNode rootNode = tree.CreateNode(typeof(RootNode)) as RootNode;
            StateOnEnterNode onEnterNode = tree.CreateNode(typeof(StateOnEnterNode)) as StateOnEnterNode;
            StateOnExitNode onExitNode = tree.CreateNode(typeof(StateOnExitNode)) as StateOnExitNode;
            rootNode.Position = Vector2.zero;
            onEnterNode.Position = new Vector2(0f, -180f);
            onExitNode.Position = new Vector2(0f, 180f);
            tree.RootGUID = rootNode.GUID;
            tree.OnEnterGUID = onEnterNode.GUID;
            tree.OnExitGUID = onExitNode.GUID;
            return tree;
        }
#endif

        void BeginRuntimeSubTree()
        {
            if (!m_RuntimeSubTree)
                return;

            if (m_RuntimeSubTree is StateBehaviorSubTree stateBehaviorSubTree)
            {
                stateBehaviorSubTree.BeginStateLifecycle();
                return;
            }

            if (m_RuntimeSubTree.LifecyclePhase != NodeLifecyclePhase.Dormant || m_RuntimeSubTree.Running)
                m_RuntimeSubTree.ForceStop(CreateStopContext(NodeStopOriginCause.Reset));
            m_RuntimeSubTree.ResetTree();
        }

        void DisposeRuntimeGraph()
        {
            if (m_RuntimeSubTree)
                m_RuntimeSubTree.DisposeTree();

            m_RuntimeSubTree = null;
        }

#if UNITY_EDITOR
        public override IEnumerable<FlowPortDeclaration> GetFlowPortDeclarations(BaseGraph owner)
        {
            yield return new FlowPortDeclaration(StateMachinePorts.StateIn, PortDirection.Input, PortCapacity.Multi);
            yield return new FlowPortDeclaration(StateMachinePorts.StateOut, PortDirection.Output, PortCapacity.Multi);
        }

        public override void OnInputLinked(BaseEdge edge)
        {
            base.OnInputLinked(edge);
            if (edge.EndPortName == StateMachinePorts.StateIn)
            {
                if (m_InputEdgeGUIDs == null)
                    m_InputEdgeGUIDs = new List<string>();
                if (!m_InputEdgeGUIDs.Contains(edge.GUID))
                    m_InputEdgeGUIDs.Add(edge.GUID);
            }
        }

        public override void OnInputUnlinked(BaseEdge edge)
        {
            base.OnInputUnlinked(edge);
            if (edge.EndPortName == StateMachinePorts.StateIn)
            {
                if (m_InputEdgeGUIDs == null)
                    return;
                m_InputEdgeGUIDs.Remove(edge.GUID);
            }
        }

        public override void OnOutputLinked(BaseEdge edge)
        {
            base.OnOutputLinked(edge);
            if (edge.StartPortName != StateMachinePorts.StateOut)
                return;

            if (!m_OutputEdgeGUIDs.Contains(edge.GUID))
            {
                edge.FlowOrder = m_OutputEdgeGUIDs.Count;
                m_OutputEdgeGUIDs.Add(edge.GUID);
            }
        }

        public override void OnOutputUnlinked(BaseEdge edge)
        {
            base.OnOutputUnlinked(edge);
            if (edge.StartPortName != StateMachinePorts.StateOut)
                return;

            m_OutputEdgeGUIDs.Remove(edge.GUID);
            for (int i = 0; i < m_OutputEdgeGUIDs.Count; i++)
            {
                if (m_Owner.GUIDEdgeMap.TryGetValue(m_OutputEdgeGUIDs[i], out BaseEdge outputEdge))
                    outputEdge.FlowOrder = i;
            }
        }
#endif
    }
}
