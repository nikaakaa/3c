using System;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [DisallowMultipleComponent]
    public sealed class SessionActorActionTargetInputProvider : CharacterActionTargetInputProvider
    {
        [SerializeField] CharacterPipelineHost m_Target;

        public CharacterPipelineHost Target => m_Target;
        public override string ProviderIdentity => m_Target
            ? $"session-actor-target/{m_Target.ActorId}"
            : "session-actor-target/unbound";

        public override bool TryCapture(CharacterPipelineHost owner, out CharacterActionTargetInputSample sample)
        {
            sample = default;
            if (!owner || !m_Target)
                return false;
            if (ReferenceEquals(owner, m_Target))
                throw new InvalidOperationException("Action target provider cannot target its owner Character host.");
            if (!owner.SessionHost || owner.SessionHost != m_Target.SessionHost)
                throw new InvalidOperationException("Action target provider owner and target must belong to the same SimulationSessionHost.");
            var ownerId = new ActorId(owner.ActorId);
            var targetId = new ActorId(m_Target.ActorId);
            if (!ownerId.IsValid || !targetId.IsValid || ownerId == targetId)
                throw new InvalidOperationException("Action target provider requires distinct valid owner and target ActorIds.");
            CharacterSimulationActorRegistration registration = m_Target.Registration;
            if (registration == null || !registration.TryGetLatestCommittedBody(out WorldBodyState body, out SimulationTick tick))
                return false;
            sample = new CharacterActionTargetInputSample(
                targetId,
                new Vector3(body.Position.X.Value, body.Position.Y.Value, body.Position.Z.Value),
                body.Yaw.Degrees.Value,
                tick.Value);
            return true;
        }
    }
}
