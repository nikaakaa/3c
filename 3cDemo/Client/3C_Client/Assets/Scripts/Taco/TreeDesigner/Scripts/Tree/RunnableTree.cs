using System;

namespace TreeDesigner
{
    //[AcceptableSubTreeType(typeof(SubTree))]
    public abstract partial class RunnableTree : BaseTree
    {
        [NonSerialized, ShowInInspector("Running")]
        protected bool m_Running;
        public bool Running { get => m_Running; set => m_Running = value; }

        [NonSerialized, ShowInInspector("State")]
        protected State m_State;
        public State State { get => m_State; set => m_State = value; }

        public Action OnStopCallback;

        public override void DisposeTree()
        {
            OnStop();
            base.DisposeTree();
        }
        public virtual State UpdateTree(float deltaTime)
        {
            SetDeltaTime(deltaTime);

            if (!m_Running && m_State == State.None)
            {
                OnStart();
            }
            if (m_Running && m_State == State.Running)
            {
                m_State = OnUpdate();
            }
            if (m_Running && m_State == State.Success || m_State == State.Failure)
            {
                OnStop();
            }
            return m_State;
        }
        public virtual void ResetTree()
        {
            m_State = State.None;
            OnReset();
        }

        public abstract void OnStart();
        public abstract State OnUpdate();
        public abstract void OnStop();
        public abstract void OnReset();
    }
}
