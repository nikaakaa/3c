using System;

namespace TreeDesigner
{
    //[AcceptableSubTreeType(typeof(SubTree))]
    public abstract partial class RunnableTree : BaseTree
    {
        [NonSerialized]
        protected bool m_Running;
        public bool Running { get => m_Running; set => m_Running = value; }

        [NonSerialized]
        protected State m_State;
        public State State { get => m_State; set => m_State = value; }

        [NonSerialized]
        NodeLifecyclePhase m_LifecyclePhase;

        [NonSerialized]
        NodeStopContext m_StopContext;

        public NodeLifecyclePhase LifecyclePhase => m_LifecyclePhase;
        public NodeStopContext StopContext => m_StopContext;
        public Action OnCompletedCallback;
        public Action OnStoppedCallback;

        public override void DisposeTree()
        {
            ForceStop(CreateStopContext(NodeStopOriginCause.Shutdown));
            base.DisposeTree();
        }
        public virtual State UpdateTree(float deltaTime)
        {
            SetDeltaTime(deltaTime);

            if (m_LifecyclePhase == NodeLifecyclePhase.Stopping)
                return State.Running;

            if (!m_Running && m_State == State.None)
            {
                m_LifecyclePhase = NodeLifecyclePhase.Active;
                OnStart();
            }
            if (m_Running && m_State == State.Running)
            {
                m_State = OnUpdate();
            }
            if (m_Running && (m_State == State.Success || m_State == State.Failure))
            {
                State result = m_State;
                m_Running = false;
                m_LifecyclePhase = NodeLifecyclePhase.Dormant;
                OnCompleted(result);
                OnCompletedCallback?.Invoke();
            }
            return m_State;
        }

        public NodeStopStatus RequestStop(NodeStopContext context)
        {
            if (m_LifecyclePhase == NodeLifecyclePhase.Stopping)
                return UpdateStopping();

            if (m_LifecyclePhase != NodeLifecyclePhase.Active || !m_Running)
            {
                m_State = State.None;
                m_Running = false;
                m_LifecyclePhase = NodeLifecyclePhase.Dormant;
                return NodeStopStatus.Completed;
            }

            m_StopContext = context;
            m_LifecyclePhase = NodeLifecyclePhase.Stopping;
            return CompleteStop(OnStopRequested(context));
        }

        public NodeStopStatus UpdateStopping()
        {
            if (m_LifecyclePhase != NodeLifecyclePhase.Stopping)
                return NodeStopStatus.Completed;

            return CompleteStop(OnStopping(m_StopContext));
        }

        public void ForceStop(NodeStopContext context)
        {
            if (m_LifecyclePhase == NodeLifecyclePhase.Dormant && !m_Running)
                return;

            OnForceStopped(context);
            m_StopContext = context;
            m_State = State.None;
            m_Running = false;
            m_LifecyclePhase = NodeLifecyclePhase.Dormant;
            OnStoppedCallback?.Invoke();
        }

        public virtual void ResetTree()
        {
            if (m_LifecyclePhase != NodeLifecyclePhase.Dormant || m_Running)
                ForceStop(CreateStopContext(NodeStopOriginCause.Reset));

            m_State = State.None;
            m_Running = false;
            m_LifecyclePhase = NodeLifecyclePhase.Dormant;
            OnReset();
        }

        public abstract void OnStart();
        public abstract State OnUpdate();
        public abstract void OnReset();

        protected virtual void OnCompleted(State result)
        {
        }

        protected abstract NodeStopStatus OnStopRequested(NodeStopContext context);
        protected abstract NodeStopStatus OnStopping(NodeStopContext context);
        protected abstract void OnForceStopped(NodeStopContext context);

        NodeStopContext CreateStopContext(NodeStopOriginCause cause)
        {
            ulong tick = User is INodeStopTickSource tickSource ? tickSource.NodeStopLocalLogicTick : 0;
            return NodeStopContext.Create(cause, tick, null);
        }

        NodeStopStatus CompleteStop(NodeStopStatus status)
        {
            if (status == NodeStopStatus.Running)
                return status;

            m_State = State.None;
            m_Running = false;
            m_LifecyclePhase = NodeLifecyclePhase.Dormant;
            OnStoppedCallback?.Invoke();
            return status;
        }
    }
}
