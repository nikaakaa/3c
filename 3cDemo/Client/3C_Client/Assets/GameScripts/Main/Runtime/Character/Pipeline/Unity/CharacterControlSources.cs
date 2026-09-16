using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public interface ISimulationSessionActorHost
    {
        ActorId SimulationActorId { get; }
        SimulationSessionHost SessionHost { get; }
    }

    public interface ICharacterActionTargetInputProvider
    {
        string ProviderIdentity { get; }
        bool TryGetTargetActorId(ISimulationSessionActorHost owner, out ActorId actorId);
    }

    public abstract class CharacterActionTargetInputProvider : MonoBehaviour, ICharacterActionTargetInputProvider
    {
        public abstract string ProviderIdentity { get; }
        public abstract bool TryGetTargetActorId(ISimulationSessionActorHost owner, out ActorId actorId);
    }

}

namespace ThirdPersonCharacter.Pipeline
{
    public enum CharacterPresentationRole : byte
    {
        LocalOwner = 1,
        SimulatedActor = 2
    }
}
