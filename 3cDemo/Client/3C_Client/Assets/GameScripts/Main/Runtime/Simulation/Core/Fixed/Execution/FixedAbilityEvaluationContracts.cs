using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedPendingActorEvaluation
    {
        readonly FixedCharacterRuntimeStateTransaction m_Transaction;
        readonly IReadOnlyList<GameplayFact> m_GameplayFacts;
        readonly IReadOnlyList<PresentationCommand> m_PresentationCommands;
        readonly IReadOnlyList<SimulationTraceRecord> m_TraceRecords;
        bool m_Consumed;

        internal FixedPendingActorEvaluation(
            ActorId actorId,
            SimulationTick tick,
            FixedCharacterRuntimeState sourceState,
            FixedCharacterRuntimeStateTransaction transaction,
            CharacterWorldSolveRequest worldRequest,
            IEnumerable<GameplayFact> gameplayFacts,
            IEnumerable<PresentationCommand> presentationCommands,
            IEnumerable<SimulationTraceRecord> traceRecords,
            bool diagnosticsEnabled)
        {
            if (!actorId.IsValid || !tick.IsValid)
                throw new ArgumentException("Fixed pending Ability evaluation identity is incomplete.");
            SourceState = sourceState ?? throw new ArgumentNullException(nameof(sourceState));
            m_Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            WorldRequest = worldRequest ?? throw new ArgumentNullException(nameof(worldRequest));
            if (worldRequest.ActorId != actorId || worldRequest.Tick != tick ||
                transaction.ActorId != actorId || transaction.Tick != tick)
            {
                throw new InvalidOperationException("Fixed pending Ability evaluation binding is invalid.");
            }
            ActorId = actorId;
            Tick = tick;
            m_GameplayFacts = Copy(gameplayFacts);
            m_PresentationCommands = Copy(presentationCommands);
            m_TraceRecords = Copy(traceRecords);
            DiagnosticsEnabled = diagnosticsEnabled;
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public CharacterWorldSolveRequest WorldRequest { get; }
        public bool DiagnosticsEnabled { get; }
        internal FixedCharacterRuntimeState SourceState { get; }
        internal FixedCharacterRuntimeStateTransaction Transaction => m_Transaction;
        internal IReadOnlyList<GameplayFact> GameplayFacts => m_GameplayFacts;
        internal IReadOnlyList<PresentationCommand> PresentationCommands => m_PresentationCommands;
        internal IReadOnlyList<SimulationTraceRecord> TraceRecords => m_TraceRecords;

        internal FixedCharacterRuntimeStateTransaction ClaimForFinalize()
        {
            if (m_Consumed)
                throw new InvalidOperationException("Fixed pending Actor evaluation has already been consumed.");
            m_Consumed = true;
            return m_Transaction;
        }

        internal void AbortUnconsumed()
        {
            if (m_Consumed)
                return;
            m_Consumed = true;
            m_Transaction.Dispose();
        }

        static IReadOnlyList<T> Copy<T>(IEnumerable<T> values)
        {
            var result = values == null ? new List<T>() : new List<T>(values);
            for (int i = 0; i < result.Count; i++)
                if (result[i] == null)
                    throw new ArgumentException("Fixed pending Actor evaluation contains a missing output.", nameof(values));
            return result.AsReadOnly();
        }
    }
}
