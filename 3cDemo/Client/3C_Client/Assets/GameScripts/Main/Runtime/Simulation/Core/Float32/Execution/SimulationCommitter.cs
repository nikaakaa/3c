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
        readonly OutputComparer m_OutputComparer = new OutputComparer();
        OrderedOutput[] m_Outputs = Array.Empty<OrderedOutput>();
        int m_OutputCount;

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
                Array.Clear(m_Outputs, 0, m_OutputCount);
                m_OutputCount = 0;
                for (int i = 0; i < actorResult.GameplayFacts.Count; i++)
                    AddOutput(new OrderedOutput(actorResult.GameplayFacts[i]));
                for (int i = 0; i < actorResult.PresentationCommands.Count; i++)
                    AddOutput(new OrderedOutput(actorResult.PresentationCommands[i]));
                Array.Sort(m_Outputs, 0, m_OutputCount, m_OutputComparer);

                for (int i = 0; i < m_OutputCount; i++)
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

        void AddOutput(OrderedOutput output)
        {
            if (m_OutputCount == m_Outputs.Length)
            {
                int capacity = Math.Max(4, m_Outputs.Length * 2);
                var values = new OrderedOutput[capacity];
                Array.Copy(m_Outputs, values, m_OutputCount);
                m_Outputs = values;
            }

            m_Outputs[m_OutputCount++] = output;
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

        }

        sealed class OutputComparer : IComparer<OrderedOutput>
        {
            public int Compare(OrderedOutput left, OrderedOutput right)
            {
                int sequence = left.Header.Sequence.CompareTo(right.Header.Sequence);
                return sequence != 0
                    ? sequence
                    : left.Header.EventId.CompareTo(right.Header.EventId);
            }
        }
    }
}
