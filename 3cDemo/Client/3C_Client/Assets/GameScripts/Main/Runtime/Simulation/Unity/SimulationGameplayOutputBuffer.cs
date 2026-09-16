using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public interface ISimulationGameplayOutputBuffer : ISimulationGameplayOutputPort
    {
        IReadOnlyList<SimulationGameplayOutputChange> CurrentTickChanges { get; }
        void BeginTick();
    }

    public readonly struct SimulationGameplayOutputChange
    {
        SimulationGameplayOutputChange(
            SimulationOutputDispositionKind kind,
            ActorId actorId,
            EventId sourceEventId,
            EventId targetEventId,
            GameplayFact fact,
            bool hasFact)
        {
            if (kind != SimulationOutputDispositionKind.Publish &&
                kind != SimulationOutputDispositionKind.Replace &&
                kind != SimulationOutputDispositionKind.Retire)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
            bool requiresTarget = kind == SimulationOutputDispositionKind.Replace ||
                                  kind == SimulationOutputDispositionKind.Retire;
            bool requiresFact = kind != SimulationOutputDispositionKind.Retire;
            if (!actorId.IsValid || !sourceEventId.IsValid ||
                requiresTarget != targetEventId.IsValid || targetEventId.Equals(sourceEventId) ||
                hasFact != requiresFact)
            {
                throw new ArgumentException("Gameplay output change identity does not match its lifecycle kind.");
            }
            Kind = kind;
            ActorId = actorId;
            SourceEventId = sourceEventId;
            TargetEventId = targetEventId;
            Fact = fact;
            HasFact = hasFact;
        }

        public SimulationOutputDispositionKind Kind { get; }
        public ActorId ActorId { get; }
        public EventId SourceEventId { get; }
        public EventId TargetEventId { get; }
        public GameplayFact Fact { get; }
        public bool HasFact { get; }

        public static SimulationGameplayOutputChange Publish(GameplayFact fact)
        {
            return new SimulationGameplayOutputChange(
                SimulationOutputDispositionKind.Publish,
                fact.Header.ActorId,
                fact.Header.EventId,
                default,
                fact,
                true);
        }

        public static SimulationGameplayOutputChange Replace(EventId targetEventId, GameplayFact fact)
        {
            return new SimulationGameplayOutputChange(
                SimulationOutputDispositionKind.Replace,
                fact.Header.ActorId,
                fact.Header.EventId,
                targetEventId,
                fact,
                true);
        }

        public static SimulationGameplayOutputChange Retire(
            ActorId actorId,
            EventId sourceEventId,
            EventId targetEventId)
        {
            return new SimulationGameplayOutputChange(
                SimulationOutputDispositionKind.Retire,
                actorId,
                sourceEventId,
                targetEventId,
                default,
                false);
        }
    }

    public sealed class SimulationGameplayOutputBuffer : ISimulationGameplayOutputBuffer
    {
        readonly List<SimulationGameplayOutputChange> m_CurrentTickChanges =
            new List<SimulationGameplayOutputChange>();

        public IReadOnlyList<SimulationGameplayOutputChange> CurrentTickChanges => m_CurrentTickChanges;

        public void BeginTick()
        {
            m_CurrentTickChanges.Clear();
        }

        public void Publish(GameplayFact fact)
        {
            m_CurrentTickChanges.Add(SimulationGameplayOutputChange.Publish(fact));
        }

        public void Replace(EventId targetEventId, GameplayFact fact)
        {
            m_CurrentTickChanges.Add(SimulationGameplayOutputChange.Replace(targetEventId, fact));
        }

        public void Retire(ActorId actorId, EventId sourceEventId, EventId targetEventId)
        {
            m_CurrentTickChanges.Add(SimulationGameplayOutputChange.Retire(actorId, sourceEventId, targetEventId));
        }
    }
}
