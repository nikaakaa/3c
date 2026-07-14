using System;
using BTSMTL.Diagnostics;
using UnityEngine;

namespace TreeDesigner
{
    public enum NodeLifecyclePhase
    {
        Dormant,
        Active,
        Stopping
    }

    public enum NodeStopOriginCause
    {
        SelfAbort,
        LowerPriorityAbort,
        ExplicitParentStop,
        StateTransition,
        Reset,
        Shutdown
    }

    public enum NodeStopStatus
    {
        Running,
        Completed,
        Failed
    }

    public interface INodeStopTickSource
    {
        ulong NodeStopLocalLogicTick { get; }
    }

    public readonly struct NodeStopContext
    {
        public NodeStopContext(
            NodeStopOriginCause originCause,
            ulong localLogicTick,
            string initiatorEdgeGuid,
            string initiatorNodeGuid,
            string sourceEdgeGuid,
            string sourceNodeGuid,
            string replacementEdgeGuid,
            string replacementNodeGuid,
            string immediateParentNodeGuid,
            int propagationDepth)
        {
            OriginCause = originCause;
            LocalLogicTick = localLogicTick;
            InitiatorEdgeGuid = initiatorEdgeGuid ?? string.Empty;
            InitiatorNodeGuid = initiatorNodeGuid ?? string.Empty;
            SourceEdgeGuid = sourceEdgeGuid ?? string.Empty;
            SourceNodeGuid = sourceNodeGuid ?? string.Empty;
            ReplacementEdgeGuid = replacementEdgeGuid ?? string.Empty;
            ReplacementNodeGuid = replacementNodeGuid ?? string.Empty;
            ImmediateParentNodeGuid = immediateParentNodeGuid ?? string.Empty;
            PropagationDepth = Mathf.Max(0, propagationDepth);
        }

        public NodeStopOriginCause OriginCause { get; }
        public ulong LocalLogicTick { get; }
        public string InitiatorEdgeGuid { get; }
        public string InitiatorNodeGuid { get; }
        public string SourceEdgeGuid { get; }
        public string SourceNodeGuid { get; }
        public string ReplacementEdgeGuid { get; }
        public string ReplacementNodeGuid { get; }
        public string ImmediateParentNodeGuid { get; }
        public int PropagationDepth { get; }

        public NodeStopContext Propagate(BaseNode immediateParent, BaseEdge sourceEdge, BaseNode sourceNode)
        {
            return new NodeStopContext(
                OriginCause,
                LocalLogicTick,
                InitiatorEdgeGuid,
                InitiatorNodeGuid,
                sourceEdge?.GUID,
                sourceNode?.GUID,
                ReplacementEdgeGuid,
                ReplacementNodeGuid,
                immediateParent?.GUID,
                PropagationDepth + 1);
        }

        public static NodeStopContext Create(
            NodeStopOriginCause originCause,
            ulong localLogicTick,
            BaseNode initiatorNode,
            BaseEdge initiatorEdge = null,
            BaseNode sourceNode = null,
            BaseEdge sourceEdge = null,
            BaseNode replacementNode = null,
            BaseEdge replacementEdge = null)
        {
            return new NodeStopContext(
                originCause,
                localLogicTick,
                initiatorEdge?.GUID,
                initiatorNode?.GUID,
                sourceEdge?.GUID,
                sourceNode?.GUID,
                replacementEdge?.GUID,
                replacementNode?.GUID,
                initiatorNode?.GUID,
                0);
        }
    }

    [Serializable]
    public abstract class RunnableNode : BaseNode
    {
        [NonSerialized]
        protected State m_State;

        [NonSerialized]
        NodeLifecyclePhase m_LifecyclePhase;

        [NonSerialized]
        NodeStopContext m_StopContext;

        [NonSerialized]
        NodeStopStatus m_LastStopStatus = NodeStopStatus.Completed;

        [NonSerialized]
        ulong m_ActivationGeneration;

        [NonSerialized]
        TreeExecutionActivationScope m_ActivationScope;

        public State State { get => m_State; set => m_State = value; }
        [ShowInInspector("Lifecycle Phase")]
        public NodeLifecyclePhase LifecyclePhase => m_LifecyclePhase;
        [ShowInInspector("Stop Status")]
        public NodeStopStatus LastStopStatus => m_LastStopStatus;
        [ShowInInspector("Stop Cause")]
        public NodeStopOriginCause StopCause => m_StopContext.OriginCause;
        [ShowInInspector("Pending Stop Ticks")]
        public ulong PendingStopElapsedTicks => m_LifecyclePhase == NodeLifecyclePhase.Stopping && LocalLogicTick >= m_StopContext.LocalLogicTick
            ? LocalLogicTick - m_StopContext.LocalLogicTick
            : 0;
        [ShowInInspector("Stop Source")]
        public string StopSource => $"{m_StopContext.SourceNodeGuid}/{m_StopContext.SourceEdgeGuid}";
        [ShowInInspector("Stop Replacement")]
        public string StopReplacement => $"{m_StopContext.ReplacementNodeGuid}/{m_StopContext.ReplacementEdgeGuid}";
        public NodeStopContext StopContext => m_StopContext;
        public ulong ActivationGeneration => m_ActivationGeneration;
        public TreeExecutionActivationScope ActivationScope => m_ActivationScope;

        public Action OnUpdateCallback;
        public Action OnStartCallback;
        public Action OnResetCallback;
        public Action OnCompletedCallback;
        public Action OnStoppedCallback;

        public virtual State UpdateNode()
        {
            if (m_LifecyclePhase == NodeLifecyclePhase.Stopping)
                return State.Running;

            bool entering = m_State != State.Running;
            if (entering)
            {
                BeginActivation();
                m_LifecyclePhase = NodeLifecyclePhase.Active;
            }

            TreeExecutionActivationScope executionScope = m_ActivationScope;
            PushActivation(executionScope);
            try
            {
                if (entering)
                {
                    OnStart();
                    TreeRuntimeDiagnostics.PublishNode(this, RuntimeTraceEventKind.NodeEntered, m_State.ToString());
                }

                if (m_State == State.Running)
                    m_State = OnUpdate();

                TreeRuntimeDiagnostics.PublishNode(this, RuntimeTraceEventKind.NodeStatus, m_State.ToString());

                if (m_State == State.Success || m_State == State.Failure)
                {
                    State result = m_State;
                    OnCompleted(result);
                    TreeRuntimeDiagnostics.PublishNode(this, RuntimeTraceEventKind.NodeCompleted, result.ToString());
                    m_LifecyclePhase = NodeLifecyclePhase.Dormant;
                    OnCompletedCallback?.Invoke();
                    ClearActivation();
                }
            }
            finally
            {
                PopActivation(executionScope);
            }

            OnUpdateCallback?.Invoke();
            return m_State;
        }

        public virtual NodeStopStatus RequestStop(NodeStopContext context)
        {
            if (m_LifecyclePhase == NodeLifecyclePhase.Stopping)
                return UpdateStopping();

            if (m_LifecyclePhase != NodeLifecyclePhase.Active || m_State != State.Running)
            {
                m_State = State.None;
                m_LifecyclePhase = NodeLifecyclePhase.Dormant;
                OnUpdateCallback?.Invoke();
                return NodeStopStatus.Completed;
            }

            m_StopContext = context;
            m_LifecyclePhase = NodeLifecyclePhase.Stopping;
            m_LastStopStatus = NodeStopStatus.Running;
            TreeRuntimeDiagnostics.PublishNode(this, RuntimeTraceEventKind.NodeStopRequested, m_LastStopStatus.ToString(), context);
            TreeExecutionActivationScope executionScope = m_ActivationScope;
            PushActivation(executionScope);
            try
            {
                return CompleteStop(OnStopRequested(context));
            }
            finally
            {
                PopActivation(executionScope);
            }
        }

        public virtual NodeStopStatus UpdateStopping()
        {
            if (m_LifecyclePhase != NodeLifecyclePhase.Stopping)
                return NodeStopStatus.Completed;

            TreeRuntimeDiagnostics.PublishNode(this, RuntimeTraceEventKind.NodeStopping, m_LastStopStatus.ToString(), m_StopContext);
            TreeExecutionActivationScope executionScope = m_ActivationScope;
            PushActivation(executionScope);
            try
            {
                return CompleteStop(OnStopping(m_StopContext));
            }
            finally
            {
                PopActivation(executionScope);
            }
        }

        public virtual void ForceStop(NodeStopContext context)
        {
            if (m_LifecyclePhase == NodeLifecyclePhase.Dormant && m_State != State.Running)
                return;

            TreeExecutionActivationScope executionScope = m_ActivationScope;
            PushActivation(executionScope);
            try
            {
                OnForceStopped(context);
                TreeRuntimeDiagnostics.PublishNode(this, RuntimeTraceEventKind.NodeForceStopped, NodeStopStatus.Completed.ToString(), context);
                m_StopContext = context;
                m_LastStopStatus = NodeStopStatus.Completed;
                m_State = State.None;
                m_LifecyclePhase = NodeLifecyclePhase.Dormant;
                OnStoppedCallback?.Invoke();
                ClearActivation();
            }
            finally
            {
                PopActivation(executionScope);
            }
            OnUpdateCallback?.Invoke();
        }

        public virtual void ResetNode()
        {
            if (m_LifecyclePhase != NodeLifecyclePhase.Dormant || m_State == State.Running)
                ForceStop(CreateStopContext(NodeStopOriginCause.Reset));

            m_State = State.None;
            m_LifecyclePhase = NodeLifecyclePhase.Dormant;
            OnReset();
            OnUpdateCallback?.Invoke();
        }

        protected NodeStopContext CreateStopContext(NodeStopOriginCause originCause)
        {
            return CreateStopContext(originCause, null, this);
        }

        protected NodeStopContext CreateStopContext(
            NodeStopOriginCause originCause,
            BaseEdge initiatorEdge,
            BaseNode sourceNode,
            BaseEdge sourceEdge = null,
            BaseNode replacementNode = null,
            BaseEdge replacementEdge = null)
        {
            return NodeStopContext.Create(
                originCause,
                LocalLogicTick,
                initiatorEdge?.EndNode ?? this,
                initiatorEdge,
                sourceNode,
                sourceEdge,
                replacementNode,
                replacementEdge);
        }

        protected ulong LocalLogicTick => Owner?.User is INodeStopTickSource tickSource
            ? tickSource.NodeStopLocalLogicTick
            : 0;

        protected TreeExecutionContext ExecutionContext => Owner?.User is ITreeExecutionContextSource source
            ? source.TreeExecutionContext
            : null;

        protected State UpdateChild(
            RunnableNode child,
            string edgeGuid)
        {
            if (child == null)
                return State.Failure;
            return child.UpdateNode();
        }

        protected NodeStopStatus RequestChildStop(RunnableNode child, NodeStopContext context, string edgeGuid = null)
        {
            if (child == null)
                return NodeStopStatus.Completed;

            BaseEdge edge = null;
            if (Owner != null && !string.IsNullOrEmpty(edgeGuid))
                Owner.GUIDEdgeMap.TryGetValue(edgeGuid, out edge);

            NodeStopContext childContext = context.Propagate(this, edge, child);
            return child.LifecyclePhase == NodeLifecyclePhase.Stopping
                ? child.UpdateStopping()
                : child.RequestStop(childContext);
        }

        protected void ForceStopChild(RunnableNode child, NodeStopContext context, string edgeGuid = null)
        {
            if (child == null)
                return;

            BaseEdge edge = null;
            if (Owner != null && !string.IsNullOrEmpty(edgeGuid))
                Owner.GUIDEdgeMap.TryGetValue(edgeGuid, out edge);
            child.ForceStop(context.Propagate(this, edge, child));
        }

        protected virtual void OnStart()
        {
            m_State = State.Running;
            InputValue();
            OnStartCallback?.Invoke();
        }

        protected virtual State OnUpdate()
        {
            return State.None;
        }

        protected virtual void OnCompleted(State result)
        {
        }

        protected virtual NodeStopStatus OnStopRequested(NodeStopContext context)
        {
            return NodeStopStatus.Completed;
        }

        protected virtual NodeStopStatus OnStopping(NodeStopContext context)
        {
            return NodeStopStatus.Completed;
        }

        protected virtual void OnStopped(NodeStopContext context, NodeStopStatus status)
        {
        }

        protected virtual void OnForceStopped(NodeStopContext context)
        {
        }

        protected virtual void OnReset()
        {
            OnResetCallback?.Invoke();
        }

        NodeStopStatus CompleteStop(NodeStopStatus status)
        {
            if (status == NodeStopStatus.Running)
            {
                m_LastStopStatus = status;
                OnUpdateCallback?.Invoke();
                return status;
            }

            m_LastStopStatus = status;
            OnStopped(m_StopContext, status);
            TreeRuntimeDiagnostics.PublishNode(this, RuntimeTraceEventKind.NodeStopped, status.ToString(), m_StopContext);
            m_State = State.None;
            m_LifecyclePhase = NodeLifecyclePhase.Dormant;
            OnStoppedCallback?.Invoke();
            ClearActivation();
            OnUpdateCallback?.Invoke();
            return status;
        }

        void BeginActivation()
        {
            m_ActivationGeneration++;
            if (m_ActivationGeneration == 0)
                m_ActivationGeneration++;
            TreeExecutionContext context = ExecutionContext;
            m_ActivationScope = context != null
                ? context.BeginActivation(Owner, this, m_ActivationGeneration)
                : default;
        }

        void PushActivation(TreeExecutionActivationScope scope)
        {
            if (scope.IsValid)
                ExecutionContext.PushActivation(scope);
        }

        void PopActivation(TreeExecutionActivationScope scope)
        {
            if (scope.IsValid)
                ExecutionContext.PopActivation(scope);
        }

        void ClearActivation()
        {
            m_ActivationScope = default;
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_State = State.None;
            m_LifecyclePhase = NodeLifecyclePhase.Dormant;
            m_StopContext = default;
            m_LastStopStatus = NodeStopStatus.Completed;
            m_ActivationGeneration = 0;
            m_ActivationScope = default;
        }
    }
}
