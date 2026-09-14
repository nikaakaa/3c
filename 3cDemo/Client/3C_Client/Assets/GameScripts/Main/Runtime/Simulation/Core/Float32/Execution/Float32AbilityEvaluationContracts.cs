using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public sealed class Float32PendingActorEvaluation
    {
        readonly Float32CharacterRuntimeStateTransaction m_Transaction;
        readonly IReadOnlyList<GameplayFact> m_GameplayFacts;
        readonly IReadOnlyList<PresentationCommand> m_PresentationCommands;
        readonly IReadOnlyList<SimulationTraceRecord> m_TraceRecords;
        bool m_Consumed;

        internal Float32PendingActorEvaluation(
            ActorId actorId,
            SimulationTick tick,
            Float32CharacterRuntimeState sourceState,
            Float32CharacterRuntimeStateTransaction transaction,
            CharacterWorldSolveRequest worldRequest,
            IEnumerable<GameplayFact> gameplayFacts,
            IEnumerable<PresentationCommand> presentationCommands,
            IEnumerable<SimulationTraceRecord> traceRecords,
            bool diagnosticsEnabled)
        {
            if (!actorId.IsValid || !tick.IsValid)
                throw new ArgumentException("Float32 pending Ability evaluation identity is incomplete.");
            SourceState = sourceState ?? throw new ArgumentNullException(nameof(sourceState));
            m_Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            WorldRequest = worldRequest ?? throw new ArgumentNullException(nameof(worldRequest));
            if (worldRequest.ActorId != actorId || worldRequest.Tick != tick ||
                transaction.ActorId != actorId || transaction.Tick != tick)
            {
                throw new InvalidOperationException("Float32 pending Ability evaluation binding is invalid.");
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
        internal Float32CharacterRuntimeState SourceState { get; }
        internal Float32CharacterRuntimeStateTransaction Transaction => m_Transaction;
        internal IReadOnlyList<GameplayFact> GameplayFacts => m_GameplayFacts;
        internal IReadOnlyList<PresentationCommand> PresentationCommands => m_PresentationCommands;
        internal IReadOnlyList<SimulationTraceRecord> TraceRecords => m_TraceRecords;

        internal Float32CharacterRuntimeStateTransaction ClaimForFinalize()
        {
            if (m_Consumed)
                throw new InvalidOperationException("Float32 pending Actor evaluation has already been consumed.");
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
                    throw new ArgumentException("Float32 pending Actor evaluation contains a missing output.", nameof(values));
            return result.AsReadOnly();
        }
    }
}
