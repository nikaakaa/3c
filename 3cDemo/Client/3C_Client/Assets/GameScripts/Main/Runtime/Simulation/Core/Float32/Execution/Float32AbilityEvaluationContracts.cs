using System;

namespace ThirdPersonSimulation
{
    public sealed class Float32PendingAbilityEvaluation
    {
        readonly IFloat32AbilityExecutionStateTransaction m_Transaction;
        bool m_Consumed;

        internal Float32PendingAbilityEvaluation(
            Float32GameplayAbilityExecutionInstallation installation,
            ActorId actorId,
            SimulationTick tick,
            Float32AbilityRuntimeState sourceState,
            IFloat32AbilityExecutionStateTransaction transaction,
            CharacterWorldSolveRequest worldRequest,
            ResolvedGameplayMotion gameplayMotion,
            bool diagnosticsEnabled)
        {
            Installation = installation ?? throw new ArgumentNullException(nameof(installation));
            if (!actorId.IsValid || !tick.IsValid)
                throw new ArgumentException("Float32 pending Ability evaluation identity is incomplete.");
            SourceState = sourceState ?? throw new ArgumentNullException(nameof(sourceState));
            m_Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            WorldRequest = worldRequest ?? throw new ArgumentNullException(nameof(worldRequest));
            if (worldRequest.ActorId != actorId || worldRequest.Tick != tick ||
                !worldRequest.NumericProfile.Equals(installation.Data.NumericProfile) ||
                !transaction.Installation.Identity.Equals(installation.Identity))
            {
                throw new InvalidOperationException("Float32 pending Ability evaluation binding is invalid.");
            }
            ActorId = actorId;
            Tick = tick;
            GameplayMotion = gameplayMotion;
            DiagnosticsEnabled = diagnosticsEnabled;
        }

        public Float32GameplayAbilityExecutionInstallation Installation { get; }
        public GameplayAbilityExecutionIdentity Identity => Installation.Identity;
        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public CharacterWorldSolveRequest WorldRequest { get; }
        public ResolvedGameplayMotion GameplayMotion { get; }
        public bool DiagnosticsEnabled { get; }
        internal Float32AbilityRuntimeState SourceState { get; }
        internal IFloat32AbilityExecutionStateTransaction Transaction => m_Transaction;

        internal IFloat32AbilityExecutionStateTransaction ClaimForFinalize()
        {
            if (m_Consumed)
                throw new InvalidOperationException("Float32 pending Ability evaluation has already been consumed.");
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
