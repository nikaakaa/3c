using System;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public abstract class Float32WorldBodyBinding : MonoBehaviour
    {
        [SerializeField] string m_BindingId = string.Empty;
        [SerializeField] string m_ActorId = string.Empty;

        public string BindingId => string.IsNullOrWhiteSpace(m_BindingId) ? string.Empty : m_BindingId.Trim();
        public ActorId ActorId => new ActorId(m_ActorId);
        public WorldBodyState InitialBody
        {
            get
            {
                RequireValid();
                WorldBodyState body = BuildInitialBody(ActorId);
                if (body.ActorId != ActorId)
                    throw new InvalidOperationException($"World body binding '{BindingId}' produced an InitialBody for another Actor.");
                return body;
            }
        }

        public void BindSessionActor(ActorId actorId)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Session Actor identity is invalid.", nameof(actorId));
            m_ActorId = actorId.Value;
        }

        protected void ConfigureIdentity(string bindingId, ActorId actorId)
        {
            if (string.IsNullOrWhiteSpace(bindingId) || !actorId.IsValid)
                throw new ArgumentException("World body binding identity is incomplete.");
            m_BindingId = bindingId.Trim();
            m_ActorId = actorId.Value;
        }

        public void RequireValid()
        {
            if (string.IsNullOrEmpty(BindingId))
                throw new InvalidOperationException($"World body binding '{name}' has no BindingId.");
            if (!ActorId.IsValid)
                throw new InvalidOperationException($"World body binding '{name}' has no ActorId.");
            RequireImplementationValid();
        }

        protected abstract void RequireImplementationValid();
        protected abstract WorldBodyState BuildInitialBody(ActorId actorId);
    }
}
