using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    public abstract class RunnableNode : BaseNode
    {
        [NonSerialized]
        protected State m_State;
        public State State { get => m_State; set => m_State = value; }

        public Action OnUpdateCallback;
        public Action OnStartCallback;
        public Action OnResetCallback;

        public virtual State UpdateNode()
        {
            if (m_State != State.Running)
            {
                OnStart();
            }
            if (m_State == State.Running)
            {
                m_State = OnUpdate();
            }
            if (m_State == State.Success || m_State == State.Failure)
            {
                OnStop();
            }
            OnUpdateCallback?.Invoke();
            return m_State;
        }
        public virtual void ResetNode()
        {
            m_State = State.None;
            OnReset();
            OnUpdateCallback?.Invoke();
        }
        public virtual void StopNode()
        {
            if (m_State == State.Running)
                OnStop();
            m_State = State.None;
            OnUpdateCallback?.Invoke();
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
        protected virtual void OnStop()
        {
        }
        protected virtual void OnReset()
        {
            OnResetCallback?.Invoke();
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_State = State.None;
        }
    }
}
