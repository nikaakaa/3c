using System;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [DisallowMultipleComponent]
    public sealed class SessionActorActionTargetInputProvider : CharacterActionTargetInputProvider
    {
        [SerializeField] MonoBehaviour m_Target;

        public ISimulationSessionActorHost Target => m_Target as ISimulationSessionActorHost;
        public override string ProviderIdentity => Target != null
            ? $"session-actor-target/{Target.SimulationActorId}"
            : "session-actor-target/unbound";

        public override bool TryGetTargetActorId(ISimulationSessionActorHost owner, out ActorId actorId)
        {
            actorId = default;
            if (owner == null || Target == null)
                return false;
            if (ReferenceEquals(owner, Target))
                throw new InvalidOperationException("Action target provider cannot target its owner Character host.");
            if (!owner.SessionHost || owner.SessionHost != Target.SessionHost)
                throw new InvalidOperationException("Action target provider owner and target must belong to the same SimulationSessionHost.");
            ActorId ownerId = owner.SimulationActorId;
            actorId = Target.SimulationActorId;
            if (!ownerId.IsValid || !actorId.IsValid || ownerId == actorId)
                throw new InvalidOperationException("Action target provider requires distinct valid owner and target ActorIds.");
            return true;
        }

#if UNITY_EDITOR
        public void SetAuthoring(MonoBehaviour target)
        {
            if (target is not ISimulationSessionActorHost)
                throw new ArgumentException("Action target must implement the Simulation Session Actor Host contract.", nameof(target));
            m_Target = target;
        }
#endif
    }
}
