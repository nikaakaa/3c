using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    public interface IFixedSourceEgressOutputPort
    {
        void Commit(FixedSourceEgressRecord record);
    }

    public interface IFixedPublishedActorResultObserver
    {
        void ObservePublished(SimulationActorTickResult result);
    }

    public interface IFixedPresentationCommitOutputPort : ISimulationPresentationOutputPort
    {
        void BeginCommit();
        void CompleteCommit(ulong committedTick);
        void AbortCommit();
    }

    public interface IFixedSimulationResultOutputPort :
        IFixedPresentationCommitOutputPort,
        IFixedPublishedActorResultObserver
    {
    }

    public sealed class NullFixedSourceEgressOutputPort : IFixedSourceEgressOutputPort
    {
        public static readonly NullFixedSourceEgressOutputPort Instance = new NullFixedSourceEgressOutputPort();
        NullFixedSourceEgressOutputPort() { }
        public void Commit(FixedSourceEgressRecord record) { }
    }

    public sealed class FixedSimulationCommitterAdapter : IFixedSimulationCommitter
    {
        readonly SimulationCommitter m_CharacterCommitter;
        readonly IFixedSourceEgressOutputPort m_SourceEgress;
        readonly IFixedPublishedActorResultObserver m_ResultObserver;
        readonly Dictionary<EventId, SimulationOutputDisposition> m_DispositionsByEvent =
            new Dictionary<EventId, SimulationOutputDisposition>();
        readonly List<SimulationOutputDisposition> m_StepDispositions =
            new List<SimulationOutputDisposition>();

        public FixedSimulationCommitterAdapter(
            SimulationComponentIdentity identity,
            SimulationCommitter characterCommitter,
            IFixedSourceEgressOutputPort sourceEgress,
            IFixedPublishedActorResultObserver resultObserver)
        {
            if (!identity.IsValid || identity.Role != SimulationComponentRole.Committer)
                throw new ArgumentException("Committer identity is invalid.", nameof(identity));
            Identity = identity;
            m_CharacterCommitter = characterCommitter ?? throw new ArgumentNullException(nameof(characterCommitter));
            m_SourceEgress = sourceEgress ?? throw new ArgumentNullException(nameof(sourceEgress));
            m_ResultObserver = resultObserver ?? throw new ArgumentNullException(nameof(resultObserver));
        }

        public SimulationComponentIdentity Identity { get; }

        public void Commit(FixedSimulationCommitBatch batch)
        {
            if (batch == null)
                throw new ArgumentNullException(nameof(batch));
            m_DispositionsByEvent.Clear();
            try
            {
                for (int i = 0; i < batch.OutputDispositions.Dispositions.Count; i++)
                {
                    SimulationOutputDisposition disposition = batch.OutputDispositions.Dispositions[i];
                    m_DispositionsByEvent.Add(disposition.SourceEventId, disposition);
                }
                for (int stepIndex = 0; stepIndex < batch.Steps.Count; stepIndex++)
                {
                    SimulationTickResult result = batch.Steps[stepIndex].Result;
                    m_StepDispositions.Clear();
                    for (int i = 0; i < result.OutputEvents.Count; i++)
                    {
                        if (!m_DispositionsByEvent.TryGetValue(result.OutputEvents[i], out SimulationOutputDisposition disposition))
                            throw new InvalidOperationException($"Commit batch has no disposition for EventId '{result.OutputEvents[i]}'.");
                        m_StepDispositions.Add(disposition);
                    }
                    m_CharacterCommitter.Commit(result, m_StepDispositions);
                    for (int actorIndex = 0; actorIndex < result.Actors.Count; actorIndex++)
                        m_ResultObserver.ObservePublished(result.Actors[actorIndex]);
                }
                for (int i = 0; i < batch.SourceEgress.Count; i++)
                    m_SourceEgress.Commit(batch.SourceEgress[i]);
            }
            finally
            {
                m_StepDispositions.Clear();
                m_DispositionsByEvent.Clear();
            }
        }
    }
}

