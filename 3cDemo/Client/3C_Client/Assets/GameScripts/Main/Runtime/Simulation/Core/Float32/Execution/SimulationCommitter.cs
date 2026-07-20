using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public interface ISimulationGameplayOutputPort
    {
        void Publish(GameplayFact fact);
        void Replace(EventId targetEventId, GameplayFact fact);
        void Retire(ActorId actorId, EventId sourceEventId, EventId targetEventId);
    }

    public interface ISimulationPresentationOutputPort
    {
        void Publish(PresentationCommand command);
    }

    public sealed class SimulationCommitException : InvalidOperationException
    {
        public SimulationCommitException(EventId eventId, Exception innerException)
            : base($"Simulation output commit failed for EventId '{eventId}'.", innerException)
        {
            EventId = eventId;
        }

        public EventId EventId { get; }
    }

    public sealed class SimulationCommitter
    {
        readonly ISimulationGameplayOutputPort m_GameplayPort;
        readonly ISimulationPresentationOutputPort m_PresentationPort;
        readonly Dictionary<EventId, SimulationOutputDisposition> m_Dispositions =
            new Dictionary<EventId, SimulationOutputDisposition>();
        readonly List<OrderedOutput> m_Outputs = new List<OrderedOutput>();

        public SimulationCommitter(
            ISimulationGameplayOutputPort gameplayPort,
            ISimulationPresentationOutputPort presentationPort)
        {
            m_GameplayPort = gameplayPort ?? throw new ArgumentNullException(nameof(gameplayPort));
            m_PresentationPort = presentationPort ?? throw new ArgumentNullException(nameof(presentationPort));
        }

        public void Commit(
            SimulationTickResult result,
            IReadOnlyList<SimulationOutputDisposition> dispositions)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (dispositions == null || dispositions.Count != result.OutputEvents.Count)
                throw new ArgumentException("Simulation Committer received mismatched output dispositions.", nameof(dispositions));

            IndexDispositions(dispositions);
            for (int actor = 0; actor < result.Actors.Count; actor++)
            {
                SimulationActorTickResult actorResult = result.Actors[actor];
                m_Outputs.Clear();
                for (int i = 0; i < actorResult.GameplayFacts.Count; i++)
                    m_Outputs.Add(new OrderedOutput(actorResult.GameplayFacts[i]));
                for (int i = 0; i < actorResult.PresentationCommands.Count; i++)
                    m_Outputs.Add(new OrderedOutput(actorResult.PresentationCommands[i]));
                m_Outputs.Sort(OrderedOutput.Compare);

                for (int i = 0; i < m_Outputs.Count; i++)
                {
                    OrderedOutput output = m_Outputs[i];
                    if (!m_Dispositions.TryGetValue(output.Header.EventId, out SimulationOutputDisposition disposition))
                        throw new InvalidOperationException(
                            $"OutputPlan has no disposition for EventId '{output.Header.EventId}'.");
                    if (!disposition.ActorId.Equals(output.Header.ActorId))
                        throw new InvalidOperationException(
                            $"OutputPlan disposition for EventId '{output.Header.EventId}' targets another Actor.");
                    try
                    {
                        if (disposition.Kind == SimulationOutputDispositionKind.Suppress)
                            continue;
                        if (output.IsGameplay)
                            CommitGameplay(disposition, output.Gameplay);
                        else
                            CommitPresentation(disposition, output.Presentation);
                    }
                    catch (SimulationCommitException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        throw new SimulationCommitException(disposition.SourceEventId, exception);
                    }
                }
            }
        }

        void CommitGameplay(SimulationOutputDisposition disposition, GameplayFact fact)
        {
            switch (disposition.Kind)
            {
                case SimulationOutputDispositionKind.Publish:
                    m_GameplayPort.Publish(fact);
                    break;
                case SimulationOutputDispositionKind.Replace:
                    m_GameplayPort.Replace(disposition.TargetEventId, fact);
                    break;
                case SimulationOutputDispositionKind.Retire:
                    m_GameplayPort.Retire(disposition.ActorId, disposition.SourceEventId, disposition.TargetEventId);
                    break;
                default:
                    throw new InvalidOperationException($"Gameplay output disposition '{disposition.Kind}' cannot be committed.");
            }
        }

        void CommitPresentation(SimulationOutputDisposition disposition, PresentationCommand command)
        {
            switch (disposition.Kind)
            {
                case SimulationOutputDispositionKind.Publish:
                    m_PresentationPort.Publish(command);
                    break;
                case SimulationOutputDispositionKind.Replace:
                case SimulationOutputDispositionKind.Retire:
                    throw new InvalidOperationException(
                        "Presentation Egress must publish final producer lifecycle commands instead of replacing or retiring historical commands.");
                default:
                    throw new InvalidOperationException($"Presentation output disposition '{disposition.Kind}' cannot be committed.");
            }
        }

        void IndexDispositions(IReadOnlyList<SimulationOutputDisposition> dispositions)
        {
            m_Dispositions.Clear();
            for (int i = 0; i < dispositions.Count; i++)
            {
                SimulationOutputDisposition disposition = dispositions[i];
                if (!m_Dispositions.TryAdd(disposition.SourceEventId, disposition))
                    throw new InvalidOperationException(
                        $"OutputPlan contains duplicate EventId '{disposition.SourceEventId}'.");
            }
        }

        readonly struct OrderedOutput
        {
            public OrderedOutput(GameplayFact gameplay)
            {
                Gameplay = gameplay;
                Presentation = default;
                IsGameplay = true;
            }

            public OrderedOutput(PresentationCommand presentation)
            {
                Gameplay = default;
                Presentation = presentation;
                IsGameplay = false;
            }

            public GameplayFact Gameplay { get; }
            public PresentationCommand Presentation { get; }
            public bool IsGameplay { get; }
            public SimulationEventHeader Header => IsGameplay ? Gameplay.Header : Presentation.Header;

            public static int Compare(OrderedOutput left, OrderedOutput right)
            {
                int sequence = left.Header.Sequence.CompareTo(right.Header.Sequence);
                return sequence != 0
                    ? sequence
                    : left.Header.EventId.CompareTo(right.Header.EventId);
            }
        }
    }
}
