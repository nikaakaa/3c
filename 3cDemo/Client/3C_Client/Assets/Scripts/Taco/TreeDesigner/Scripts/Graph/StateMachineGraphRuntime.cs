using System.Collections.Generic;
using System.Linq;

namespace TreeDesigner
{
    public sealed class StateMachineGraphRuntime
    {
        readonly StateMachineGraph m_Graph;
        readonly Dictionary<string, TransitionRuleGraphRuntime> m_TransitionRuleRuntimes = new Dictionary<string, TransitionRuleGraphRuntime>();
        StateNode m_ActiveState;
        StateNode m_ExitingState;
        StateNode m_PendingTargetState;
        bool m_PendingExitGraph;
        bool m_Completed;

        public StateNode ActiveState => m_ActiveState;

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

            State state = m_ActiveState.UpdateState(deltaTime);
            if (TryTransitionFrom(m_ActiveState, true, out State transitionState))
                return transitionState;

            return state == State.Failure ? State.Failure : State.Running;
        }

        public void Stop()
        {
            m_ExitingState?.StopNode();
            m_ActiveState?.StopNode();
            ClearPendingTransition();
            DisposeTransitionRuleRuntimes();
        }

        public void Reset()
        {
            m_ExitingState?.ResetNode();
            m_ActiveState?.ResetNode();
            m_ActiveState = null;
            ClearPendingTransition();
            DisposeTransitionRuleRuntimes();
            m_Completed = false;
        }

        public void Dispose()
        {
            Stop();
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
                if (sourceNode is StateMachineAnyStateNode && !edge.HasTransitionRuleGraph)
                    continue;

                if (!EvaluateTransitionCondition(edge))
                    continue;

                if (!m_Graph.GUIDNodeMap.TryGetValue(edge.EndNodeGUID, out BaseNode targetNode))
                    continue;

                if (sourceNode is StateMachineEnterNode && !(targetNode is StateNode))
                    continue;

                if (targetNode is StateMachineExitNode)
                {
                    return BeginTransition(null, true, stopActiveState, out result);
                }

                if (!(targetNode is StateNode targetState))
                    continue;

                return BeginTransition(targetState, false, stopActiveState, out result);
            }

            return false;
        }

        bool BeginTransition(StateNode targetState, bool exitsGraph, bool stopActiveState, out State result)
        {
            result = State.Running;

            if (!stopActiveState || m_ActiveState == null)
            {
                CompleteTransition(targetState, exitsGraph, out result);
                return true;
            }

            m_ExitingState = m_ActiveState;
            m_PendingTargetState = targetState;
            m_PendingExitGraph = exitsGraph;
            return TryContinuePendingTransition(m_Graph.DeltaTime, out result);
        }

        bool TryContinuePendingTransition(float deltaTime, out State result)
        {
            result = State.Running;

            if (m_ExitingState == null)
                return false;

            State exitState = m_ExitingState.UpdateStateExit(deltaTime);
            if (exitState == State.Running)
                return true;

            if (exitState == State.Failure)
            {
                ClearPendingTransition();
                result = State.Failure;
                return true;
            }

            m_ExitingState.StopNode();
            CompleteTransition(m_PendingTargetState, m_PendingExitGraph, out result);
            ClearPendingTransition();
            return true;
        }

        void CompleteTransition(StateNode targetState, bool exitsGraph, out State result)
        {
            if (exitsGraph)
            {
                m_ActiveState = null;
                m_Completed = true;
                result = State.Success;
                return;
            }

            m_ActiveState = targetState;
            result = State.Running;
        }

        void ClearPendingTransition()
        {
            m_ExitingState = null;
            m_PendingTargetState = null;
            m_PendingExitGraph = false;
        }

        IEnumerable<BaseEdge> GetOutgoingTransitions(BaseNode sourceNode)
        {
            return m_Graph.GetOutputEdges(sourceNode, StateMachinePorts.StateOut)
                .OrderBy(i => i.TransitionPriority)
                .ThenBy(i => i.FlowOrder);
        }

        bool EvaluateTransitionCondition(BaseEdge edge)
        {
            if (!edge.HasTransitionRuleGraph)
                return true;

            if (!m_TransitionRuleRuntimes.TryGetValue(edge.GUID, out TransitionRuleGraphRuntime runtime))
            {
                runtime = new TransitionRuleGraphRuntime(edge.TransitionRuleGraph);
                m_TransitionRuleRuntimes.Add(edge.GUID, runtime);
            }

            return runtime.Evaluate(m_Graph);
        }

        void DisposeTransitionRuleRuntimes()
        {
            foreach (var runtime in m_TransitionRuleRuntimes.Values)
                runtime.Dispose();
            m_TransitionRuleRuntimes.Clear();
        }
    }
}
