using System;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedPendingAbilityEvaluation
    {
        readonly FixedCharacterRuntimeStateTransaction m_Transaction;
        bool m_Consumed;

        internal FixedPendingAbilityEvaluation(
            FixedGameplayAbilityExecutionInstallation installation,
            ActorId actorId,
            SimulationTick tick,
            FixedCharacterRuntimeState sourceState,
            FixedCharacterRuntimeStateTransaction transaction,
            CharacterWorldSolveRequest worldRequest,
            ResolvedGameplayMotion gameplayMotion,
            bool diagnosticsEnabled)
        {
            Installation = installation ?? throw new ArgumentNullException(nameof(installation));
            if (!actorId.IsValid || !tick.IsValid)
                throw new ArgumentException("Fixed pending Ability evaluation identity is incomplete.");
            SourceState = sourceState ?? throw new ArgumentNullException(nameof(sourceState));
            m_Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            WorldRequest = worldRequest ?? throw new ArgumentNullException(nameof(worldRequest));
            if (worldRequest.ActorId != actorId || worldRequest.Tick != tick ||
                !worldRequest.NumericProfile.Equals(installation.Data.NumericProfile) ||
                transaction.ActorId != actorId || transaction.Tick != tick ||
                !ReferenceEquals(transaction.BaseState, sourceState))
            {
                throw new InvalidOperationException("Fixed pending Ability evaluation binding is invalid.");
            }
            ActorId = actorId;
            Tick = tick;
            GameplayMotion = gameplayMotion;
            DiagnosticsEnabled = diagnosticsEnabled;
        }

        public FixedGameplayAbilityExecutionInstallation Installation { get; }
        public GameplayAbilityExecutionIdentity Identity => Installation.Identity;
        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public CharacterWorldSolveRequest WorldRequest { get; }
        public ResolvedGameplayMotion GameplayMotion { get; }
        public bool DiagnosticsEnabled { get; }
        internal FixedCharacterRuntimeState SourceState { get; }
        internal FixedCharacterRuntimeStateTransaction Transaction => m_Transaction;

        internal FixedCharacterRuntimeStateTransaction ClaimForFinalize()
        {
            if (m_Consumed)
                throw new InvalidOperationException("Fixed pending Ability evaluation has already been consumed.");
            m_Consumed = true;
            return m_Transaction;
        }

        internal void AbortUnconsumed()
        {
            if (m_Consumed)
                return;
            m_Consumed = true;
            m_Transaction.Abort();
        }
    }
}
