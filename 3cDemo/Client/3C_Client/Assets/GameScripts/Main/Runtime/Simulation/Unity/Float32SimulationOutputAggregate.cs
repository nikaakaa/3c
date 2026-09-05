using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    internal sealed class Float32SimulationOutputAggregate :
        ISimulationGameplayOutputPort,
        ISimulationPresentationOutputPort,
        IFloat32PublishedActorResultObserver,
        ISimulationSessionOutputLifecycle
    {
        readonly Dictionary<ActorId, IFloat32SimulationActorRegistration> m_ByActor;
        readonly IFloat32SimulationActorRegistration[] m_Ordered;

        public Float32SimulationOutputAggregate(IReadOnlyList<IFloat32SimulationActorRegistration> registrations)
        {
            if (registrations == null || registrations.Count == 0)
                throw new ArgumentException("Character output aggregate requires an Actor roster.", nameof(registrations));
            m_ByActor = new Dictionary<ActorId, IFloat32SimulationActorRegistration>();
            m_Ordered = new IFloat32SimulationActorRegistration[registrations.Count];
            for (int i = 0; i < registrations.Count; i++)
            {
                IFloat32SimulationActorRegistration registration = registrations[i] ??
                    throw new ArgumentException("Character output aggregate contains a missing registration.", nameof(registrations));
                if (!m_ByActor.TryAdd(registration.ActorId, registration))
                    throw new ArgumentException($"Character output aggregate contains duplicate ActorId '{registration.ActorId}'.", nameof(registrations));
                m_Ordered[i] = registration;
            }
            Array.Sort(m_Ordered, (left, right) => left.ActorId.CompareTo(right.ActorId));
        }

        public void BeginLogicTick()
        {
            for (int i = 0; i < m_Ordered.Length; i++)
                m_Ordered[i].BeginLogicTick();
        }

        public void Publish(GameplayFact fact)
        {
            Route(fact.Header.ActorId).GameplayOutput.Publish(fact);
        }

        public void Replace(EventId targetEventId, GameplayFact fact)
        {
            Route(fact.Header.ActorId).GameplayOutput.Replace(targetEventId, fact);
        }

        public void Retire(ActorId actorId, EventId sourceEventId, EventId targetEventId)
        {
            Route(actorId).GameplayOutput.Retire(actorId, sourceEventId, targetEventId);
        }

        public void Publish(PresentationCommand command)
        {
            Route(command.Header.ActorId).PresentationOutput.Publish(command);
        }

        public void ObservePublished(SimulationActorTickResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            Route(result.ActorId).ObservePublished(result);
        }

        IFloat32SimulationActorRegistration Route(ActorId actorId)
        {
            if (!m_ByActor.TryGetValue(actorId, out IFloat32SimulationActorRegistration registration))
                throw new InvalidOperationException($"Simulation output targets unknown Actor '{actorId}'.");
            return registration;
        }

    }
}
