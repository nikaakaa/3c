using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using UnityEngine;

namespace TreeDesigner
{
    public sealed class StateMachineGraphRuntime
    {
        readonly StateMachineGraph m_Graph;
        readonly Guid m_RuntimeId = Guid.NewGuid();
        readonly Dictionary<string, ConditionRuleGraphRuntime> m_ConditionRuleRuntimes = new Dictionary<string, ConditionRuleGraphRuntime>();
        readonly HashSet<string> m_ReportedInvalidConditionEdges = new HashSet<string>();
        readonly StateMachineRuntimeFacts m_Facts = new StateMachineRuntimeFacts();
        StateNode m_ActiveState;
        StateNode m_ExitingState;
        StateNode m_PendingTargetState;
        BaseEdge m_PendingTransitionEdge;
        StateMachineExecutionScope m_ActiveScope;
        StateMachineExecutionScope m_ExitingScope;
        StateExitContext m_PendingExitContext;
        NodeStopContext m_PendingRootStopContext;
        ulong m_ActivationGeneration;
        bool m_PendingExitGraph;
        bool m_PendingExternalStop;
        bool m_Completed;
        bool m_Failed;
        NodeStopStatus m_LastPendingExitStatus = NodeStopStatus.Completed;

        public StateNode ActiveState => m_ActiveState;
        public StateNode ExitingState => m_ExitingState;
        public StateNode PendingTargetState => m_PendingTargetState;
        public StateExitContext PendingExitContext => m_PendingExitContext;
        public IStateMachineRuntimeFacts Facts => m_Facts;

        public StateMachineGraphRuntime(StateMachineGraph graph)
        {
            m_Graph = graph;
        }

        public State Update(float deltaTime)
        {
            if (!m_Graph)
                return State.Failure;

            m_Graph.SetDeltaTime(deltaTime);

            if (m_Completed)
                return State.Success;
            if (m_Failed)
                return State.Failure;

            if (TryContinuePendingTransition(deltaTime, out State pendingTransitionState))
                return pendingTransitionState;

            if (m_ActiveState == null)
            {
                if (!TryTransitionFrom(m_Graph.EnterNode, false, out State entryTransitionState))
                    return State.Failure;

                if (entryTransitionState == State.Success)
                    return State.Success;
            }

            if (m_Graph.AnyStateNode != null && TryTransitionFrom(m_Graph.AnyStateNode, true, out State anyStateTransitionState))
                return anyStateTransitionState;

            if (m_ActiveState == null)
                return State.Failure;

            m_Facts.Advance(deltaTime);
            State state = UpdateStateInScope(m_ActiveState, m_ActiveScope, deltaTime);
            m_Facts.SetRootStatus(m_ActiveState.LastRootStatus);
            if (TryTransitionFrom(m_ActiveState, true, out State transitionState))
                return transitionState;

            return state == State.Failure ? State.Failure : State.Running;
        }

        public NodeStopStatus RequestExit(NodeStopContext stopContext, float deltaTime)
        {
            if (m_Failed)
                return NodeStopStatus.Failed;

            if (m_ExitingState == null && m_ActiveState == null)
                return NodeStopStatus.Completed;

            if (m_ExitingState == null)
            {
                BeginSourceExit(
                    m_ActiveState,
                    null,
                    null,
                    false,
                    true,
                    stopContext,
                    CreateTreeExitContext(m_ActiveState, stopContext));
            }
            else
            {
                m_PendingTargetState = null;
                m_PendingTransitionEdge = null;
                m_PendingExitGraph = false;
                m_PendingExternalStop = true;
                m_PendingRootStopContext = stopContext;
                m_PendingExitContext = CreateTreeExitContext(m_ExitingState, stopContext);
                m_Facts.BeginPending(m_ExitingState, null);
            }

            TryContinuePendingTransition(deltaTime, out _);
            return m_LastPendingExitStatus;
        }

        public void ForceStop(NodeStopContext context)
        {
            ReleaseRuntimeStates(context);
            ClearPendingTransition();
            DisposeConditionRuleRuntimes();
            m_Facts.Clear();
            m_Completed = false;
            m_Failed = false;
        }

        public void Reset()
        {
            ForceStop(CreateRuntimeStopContext(NodeStopOriginCause.Reset));
        }

        public void Dispose()
        {
            ForceStop(CreateRuntimeStopContext(NodeStopOriginCause.Shutdown));
        }

        bool TryTransitionFrom(BaseNode sourceNode, bool stopActiveState, out State result)
        {
            return TryTransitionFrom(sourceNode, stopActiveState, new HashSet<string>(), out result);
        }

        bool TryTransitionFrom(BaseNode sourceNode, bool stopActiveState, HashSet<string> visitedControlNodes, out State result)
        {
            result = State.Running;
            if (sourceNode == null)
                return false;

            if (sourceNode is StateMachineControlNode && !visitedControlNodes.Add(sourceNode.GUID))
                return false;

            foreach (var edge in GetOutgoingTransitions(sourceNode))
            {
                if (!EvaluateTransitionCondition(edge))
                    continue;

                if (!m_Graph.GUIDNodeMap.TryGetValue(edge.EndNodeGUID, out BaseNode targetNode))
                    continue;

                if (sourceNode is StateMachineEnterNode && !(targetNode is StateNode))
                    continue;

                if (targetNode is StateMachineExitNode)
                {
                    return BeginTransition(edge, null, true, stopActiveState, out result);
                }

                if (!(targetNode is StateNode targetState))
                    continue;

                return BeginTransition(edge, targetState, false, stopActiveState, out result);
            }

            return false;
        }

        bool BeginTransition(
            BaseEdge edge,
            StateNode targetState,
            bool exitsGraph,
            bool stopActiveState,
            out State result)
        {
            result = State.Running;
            TreeRuntimeDiagnostics.PublishStateTransition(
                m_Graph,
                m_RuntimeId,
                m_ActiveScope,
                edge,
                RuntimeTraceEventKind.StateTransitionSelected,
                true);
            if (!stopActiveState || m_ActiveState == null)
            {
                CompleteTransition(targetState, exitsGraph, default, out result);
                return true;
            }

            m_ExitingState = m_ActiveState;
            NodeStopContext stopContext = CreateTransitionStopContext(m_ActiveState, targetState, edge);
            StateExitContext exitContext = CreateTransitionExitContext(m_ActiveState, targetState, edge);
            BeginSourceExit(
                m_ActiveState,
                targetState,
                edge,
                exitsGraph,
                false,
                stopContext,
                exitContext);
            return TryContinuePendingTransition(m_Graph.DeltaTime, out result);
        }

        void BeginSourceExit(
            StateNode sourceState,
            StateNode targetState,
            BaseEdge transitionEdge,
            bool exitsGraph,
            bool externalStop,
            NodeStopContext rootStopContext,
            StateExitContext exitContext)
        {
            m_ExitingState = sourceState;
            m_ExitingScope = m_ActiveScope;
            m_PendingTargetState = targetState;
            m_PendingTransitionEdge = transitionEdge;
            m_PendingExitGraph = exitsGraph;
            m_PendingExternalStop = externalStop;
            m_PendingRootStopContext = rootStopContext;
            m_PendingExitContext = exitContext;
            m_LastPendingExitStatus = NodeStopStatus.Running;
            m_Facts.BeginPending(m_ExitingState, m_PendingTargetState);
            TreeRuntimeDiagnostics.PublishState(
                m_Graph,
                m_RuntimeId,
                sourceState.GUID,
                m_ExitingScope.ActivationGeneration,
                RuntimeTraceEventKind.StateExitStarted,
                targetState?.GUID,
                exitContext.Cause.ToString(),
                "Exiting");
        }

        bool TryContinuePendingTransition(float deltaTime, out State result)
        {
            result = State.Running;

            if (m_ExitingState == null)
            {
                m_LastPendingExitStatus = NodeStopStatus.Completed;
                return false;
            }

            State exitState = UpdateStateExitInScope(
                m_ExitingState,
                m_ExitingScope,
                m_PendingExitContext,
                m_PendingRootStopContext,
                deltaTime);
            if (exitState == State.Running)
            {
                m_LastPendingExitStatus = NodeStopStatus.Running;
                TreeRuntimeDiagnostics.PublishState(
                    m_Graph,
                    m_RuntimeId,
                    m_ExitingState.GUID,
                    m_ExitingScope.ActivationGeneration,
                    RuntimeTraceEventKind.StateExitWaiting,
                    m_PendingTargetState?.GUID,
                    m_PendingExitContext.Cause.ToString(),
                    "Waiting");
                return true;
            }

            if (exitState == State.Failure)
            {
                Debug.LogError($"StateMachine pending exit failed: graph={m_Graph.name} source={m_ExitingState?.DisplayName}/{m_ExitingState?.GUID} edge={m_PendingTransitionEdge?.GUID}");
                m_Failed = true;
                ClearPendingTransition();
                result = State.Failure;
                m_LastPendingExitStatus = NodeStopStatus.Failed;
                return true;
            }

            StateNode sourceState = m_ExitingState;
            StateMachineExecutionScope sourceScope = m_ExitingScope;
            bool externalStop = m_PendingExternalStop;
            ForceStopStateInScope(sourceState, sourceScope, m_PendingRootStopContext);
            NotifyStateExited(
                sourceScope,
                m_PendingTargetState?.GUID,
                m_PendingExitContext.Cause.ToString());

            if (externalStop)
            {
                m_ActiveState = null;
                m_ActiveScope = default;
                m_Facts.Clear();
                result = State.Success;
            }
            else
            {
                CompleteTransition(
                    m_PendingTargetState,
                    m_PendingExitGraph,
                    sourceScope,
                    out result);
            }

            ClearPendingTransition();
            m_LastPendingExitStatus = NodeStopStatus.Completed;
            return true;
        }

        void CompleteTransition(
            StateNode targetState,
            bool exitsGraph,
            StateMachineExecutionScope sourceScope,
            out State result)
        {
            if (exitsGraph)
            {
                m_ActiveState = null;
                m_ActiveScope = default;
                m_Completed = true;
                m_Facts.Clear();
                result = State.Success;
                return;
            }

            m_ActiveState = targetState;
            m_ActiveScope = CreateExecutionScope(targetState);
            if (!m_ActiveScope.IsValid)
            {
                Debug.LogError($"StateMachine '{m_Graph.name}' failed to create execution scope for '{targetState?.ResolvedDisplayName}'.");
                m_ActiveState = null;
                m_Failed = true;
                result = State.Failure;
                return;
            }
            m_Facts.Enter(m_ActiveState);
            NotifyStateEntered(m_ActiveScope, sourceScope.StateId);
            result = State.Running;
        }

        void ClearPendingTransition()
        {
            m_ExitingState = null;
            m_PendingTargetState = null;
            m_PendingTransitionEdge = null;
            m_ExitingScope = default;
            m_PendingExitGraph = false;
            m_PendingExternalStop = false;
            m_PendingRootStopContext = default;
            m_PendingExitContext = default;
            m_Facts.ClearPending();
        }

        IEnumerable<BaseEdge> GetOutgoingTransitions(BaseNode sourceNode)
        {
            return m_Graph.GetOutputEdges(sourceNode, StateMachinePorts.StateOut)
                .OrderBy(i => i.TransitionPriority)
                .ThenBy(i => i.FlowOrder);
        }

        bool EvaluateTransitionCondition(BaseEdge edge)
        {
            string error = edge == null ? "Transition edge is missing." : string.Empty;
            if (edge == null || !edge.TryResolveConditionRuleGraph(out ConditionRuleGraph graph, out error))
            {
                if (edge != null && m_ReportedInvalidConditionEdges.Add(edge.GUID))
                {
                    Debug.LogError($"StateMachine condition edge is invalid: owner={m_Graph.name}/{m_Graph.GraphAuthoringId} edge={edge.GUID} ownership={edge.ConditionRuleGraphOwnership} reason={error}", m_Graph.SerializedOwner);
                }
                TreeRuntimeDiagnostics.PublishStateTransition(
                    m_Graph,
                    m_RuntimeId,
                    m_ActiveScope,
                    edge,
                    RuntimeTraceEventKind.StateTransitionEvaluated,
                    false,
                    "InvalidConditionRuleGraph",
                    $"owner={m_Graph?.name}/{m_Graph?.GraphAuthoringId} edge={edge?.GUID} ownership={edge?.ConditionRuleGraphOwnership} reason={error}");
                return false;
            }

            if (!m_ConditionRuleRuntimes.TryGetValue(edge.GUID, out ConditionRuleGraphRuntime runtime))
            {
                runtime = new ConditionRuleGraphRuntime(graph, edge);
                m_ConditionRuleRuntimes.Add(edge.GUID, runtime);
            }

            bool result = runtime.Evaluate(m_Graph, m_Facts, m_ActiveScope);
            TreeRuntimeDiagnostics.PublishStateTransition(
                m_Graph,
                m_RuntimeId,
                m_ActiveScope,
                edge,
                RuntimeTraceEventKind.StateTransitionEvaluated,
                result);
            return result;
        }

        void DisposeConditionRuleRuntimes()
        {
            foreach (var runtime in m_ConditionRuleRuntimes.Values)
                runtime.Dispose();
            m_ConditionRuleRuntimes.Clear();
            m_ReportedInvalidConditionEdges.Clear();
        }

        void NotifyStateEntered(StateMachineExecutionScope scope, string sourceStateId)
        {
            if (scope.IsValid && m_Graph.User is IPipelineBlackboardRuntimeAccess blackboardRuntime)
                blackboardRuntime.NotifyPipelineBlackboardStateEntered(scope);
            if (scope.IsValid)
            {
                TreeRuntimeDiagnostics.PublishState(
                    m_Graph,
                    m_RuntimeId,
                    scope.StateId,
                    scope.ActivationGeneration,
                    RuntimeTraceEventKind.StateScopeEntered,
                    sourceStateId,
                    NodeStopOriginCause.StateTransition.ToString(),
                    "Active");
            }
        }

        void NotifyStateExited(StateMachineExecutionScope scope, string targetStateId, string cause)
        {
            if (scope.IsValid && m_Graph.User is IPipelineBlackboardRuntimeAccess blackboardRuntime)
                blackboardRuntime.NotifyPipelineBlackboardStateExited(scope);
            if (scope.IsValid)
            {
                TreeRuntimeDiagnostics.PublishState(
                    m_Graph,
                    m_RuntimeId,
                    scope.StateId,
                    scope.ActivationGeneration,
                    RuntimeTraceEventKind.StateScopeExited,
                    targetStateId,
                    cause,
                    "Released");
            }
        }

        StateMachineExecutionScope CreateExecutionScope(StateNode state)
        {
            if (state == null || !state.PrepareRuntimeSubTree())
                return default;

            m_ActivationGeneration++;
            if (m_ActivationGeneration == 0)
                m_ActivationGeneration++;
            SubTree stateBody = state.RuntimeSubTree;
            return new StateMachineExecutionScope(
                m_RuntimeId,
                state.GUID,
                m_ActivationGeneration,
                m_Graph.GraphAuthoringId,
                m_Graph.RuntimeId,
                stateBody.GraphAuthoringId,
                stateBody.RuntimeId);
        }

        State UpdateStateInScope(StateNode state, StateMachineExecutionScope scope, float deltaTime)
        {
            PushScope(scope);
            try
            {
                return state.UpdateState(deltaTime);
            }
            finally
            {
                PopScope(scope);
            }
        }

        State UpdateStateExitInScope(
            StateNode state,
            StateMachineExecutionScope scope,
            StateExitContext exitContext,
            NodeStopContext rootStopContext,
            float deltaTime)
        {
            PushScope(scope);
            PushExitContext(exitContext);
            try
            {
                return state.UpdateStateExit(deltaTime, rootStopContext);
            }
            finally
            {
                PopExitContext(exitContext);
                PopScope(scope);
            }
        }

        void ForceStopStateInScope(StateNode state, StateMachineExecutionScope scope, NodeStopContext context)
        {
            if (state == null)
                return;

            PushScope(scope);
            try
            {
                state.ForceStop(context);
            }
            finally
            {
                PopScope(scope);
            }
        }

        void ReleaseRuntimeStates(NodeStopContext context)
        {
            StateNode exitingState = m_ExitingState;
            StateMachineExecutionScope exitingScope = m_ExitingScope;
            StateNode activeState = m_ActiveState;
            StateMachineExecutionScope activeScope = m_ActiveScope;

            if (exitingState != null)
            {
                ForceStopStateInScope(exitingState, exitingScope, context);
                NotifyStateExited(exitingScope, context.ReplacementNodeGuid, context.OriginCause.ToString());
            }

            if (activeState != null && !ReferenceEquals(activeState, exitingState))
            {
                ForceStopStateInScope(activeState, activeScope, context);
                NotifyStateExited(activeScope, context.ReplacementNodeGuid, context.OriginCause.ToString());
            }

            m_ActiveState = null;
            m_ActiveScope = default;
        }

        NodeStopContext CreateTransitionStopContext(StateNode sourceState, StateNode targetState, BaseEdge edge)
        {
            ulong tick = m_Graph.User is INodeStopTickSource tickSource ? tickSource.NodeStopLocalLogicTick : 0;
            return NodeStopContext.Create(
                NodeStopOriginCause.StateTransition,
                tick,
                targetState ?? sourceState,
                edge,
                sourceState,
                edge,
                targetState,
                edge);
        }

        NodeStopContext CreateRuntimeStopContext(NodeStopOriginCause cause)
        {
            ulong tick = m_Graph != null && m_Graph.User is INodeStopTickSource tickSource
                ? tickSource.NodeStopLocalLogicTick
                : 0;
            return NodeStopContext.Create(cause, tick, null);
        }

        static StateExitContext CreateTransitionExitContext(StateNode sourceState, StateNode targetState, BaseEdge edge)
        {
            ulong tick = sourceState?.Owner?.User is INodeStopTickSource tickSource
                ? tickSource.NodeStopLocalLogicTick
                : 0;
            return new StateExitContext(
                StateExitCause.StateTransition,
                sourceState?.GUID,
                targetState?.GUID,
                edge?.GUID,
                null,
                null,
                null,
                null,
                tick);
        }

        static StateExitContext CreateTreeExitContext(StateNode sourceState, NodeStopContext stopContext)
        {
            return new StateExitContext(
                MapTreeExitCause(stopContext.OriginCause),
                sourceState?.GUID,
                null,
                null,
                stopContext.SourceEdgeGuid,
                stopContext.SourceNodeGuid,
                stopContext.ReplacementEdgeGuid,
                stopContext.ReplacementNodeGuid,
                stopContext.LocalLogicTick);
        }

        static StateExitCause MapTreeExitCause(NodeStopOriginCause cause)
        {
            switch (cause)
            {
                case NodeStopOriginCause.SelfAbort:
                    return StateExitCause.TreeSelfAbort;
                case NodeStopOriginCause.LowerPriorityAbort:
                    return StateExitCause.TreeLowerPriorityAbort;
                default:
                    return StateExitCause.TreeParentStop;
            }
        }

        void PushScope(StateMachineExecutionScope scope)
        {
            if (scope.IsValid && m_Graph.User is IStateMachineExecutionScopeSink sink)
                sink.PushStateMachineExecutionScope(scope);
        }

        void PopScope(StateMachineExecutionScope scope)
        {
            if (scope.IsValid && m_Graph.User is IStateMachineExecutionScopeSink sink)
                sink.PopStateMachineExecutionScope(scope);
        }

        void PushExitContext(StateExitContext context)
        {
            if (context.IsValid && m_Graph.User is IStateExitContextRuntimeAccess access)
                access.PushStateExitContext(context);
        }

        void PopExitContext(StateExitContext context)
        {
            if (context.IsValid && m_Graph.User is IStateExitContextRuntimeAccess access)
                access.PopStateExitContext(context);
        }

        TreeExecutionContext ExecutionContext => m_Graph.User is ITreeExecutionContextSource source
            ? source.TreeExecutionContext
            : null;

        ulong CurrentLogicTick()
        {
            return m_Graph.User is INodeStopTickSource tickSource ? tickSource.NodeStopLocalLogicTick : 0;
        }
    }
}
