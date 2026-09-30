using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    public readonly struct CharacterBodySample
    {
        public CharacterBodySample(
            ActorId actorId,
            SimulationTick tick,
            WorldBodyState beforeBody,
            WorldBodyState finalBody,
            FixedVector3 appliedDisplacement,
            FixedScalar appliedYawDegrees)
        {
            if (!actorId.IsValid || !tick.IsValid || beforeBody.ActorId != actorId || finalBody.ActorId != actorId)
                throw new ArgumentException("Body sample identity is incomplete.");
            ActorId = actorId;
            Tick = tick;
            BeforeBody = beforeBody;
            FinalBody = finalBody;
            AppliedDisplacement = appliedDisplacement;
            AppliedYawDegrees = appliedYawDegrees;
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public WorldBodyState BeforeBody { get; }
        public WorldBodyState FinalBody { get; }
        public FixedVector3 AppliedDisplacement { get; }
        public FixedScalar AppliedYawDegrees { get; }
    }

    internal interface IFixedPipelineProductValueRelease
    {
        void ReleaseOwnedValue();
    }

    public sealed class SimulationActorTickResult : IFixedPipelineProductValueRelease
    {
        FixedCharacterEvaluationOutput m_Output;
        SimulationActorBinding m_OutputOwner;
        CharacterStateHash m_StateHash;

        SimulationActorTickResult(
            ActorId actorId,
            SimulationTick tick,
            FixedCharacterRuntimeState state,
            CharacterStateHash stateHash,
            CharacterBodySample bodySample,
            CharacterMotionRequest motion,
            FixedCharacterEvaluationOutput output,
            SimulationActorBinding outputOwner)
        {
            if (!actorId.IsValid || !tick.IsValid || bodySample.ActorId != actorId || bodySample.Tick != tick)
                throw new ArgumentException("Actor Tick result identity is incomplete.");
            State = state ?? throw new ArgumentNullException(nameof(state));
            if (!stateHash.IsValid)
                throw new ArgumentException("Fixed Character runtime state hash is invalid.", nameof(stateHash));
            m_StateHash = stateHash;
            if (state.LastCompletedTick != tick.Value)
                throw new ArgumentException("Actor state Tick does not match result Tick.", nameof(state));
            ActorId = actorId;
            Tick = tick;
            BodySample = bodySample;
            Motion = motion;
            m_Output = output ?? throw new ArgumentNullException(nameof(output));
            m_OutputOwner = outputOwner ?? throw new ArgumentNullException(nameof(outputOwner));
            ValidateHeaders(m_Output.Facts, state.NumericProfile, actorId, tick, "Gameplay fact");
            ValidateHeaders(m_Output.Presentation, state.NumericProfile, actorId, tick, "Presentation command");
            ValidateHeaders(m_Output.Trace, state.NumericProfile, actorId, tick, "Trace record");
        }

        internal static SimulationActorTickResult FromOwnedOutputs(
            ActorId actorId,
            SimulationTick tick,
            FixedCharacterRuntimeState state,
            CharacterStateHash stateHash,
            CharacterBodySample bodySample,
            CharacterMotionRequest motion,
            FixedCharacterEvaluationOutput output,
            SimulationActorBinding outputOwner)
        {
            return new SimulationActorTickResult(
                actorId,
                tick,
                state,
                stateHash,
                bodySample,
                motion,
                output,
                outputOwner);
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public FixedCharacterRuntimeState State { get; }
        public CharacterStateHash StateHash => m_StateHash;
        public CharacterBodySample BodySample { get; }
        public CharacterMotionRequest Motion { get; }
        public IReadOnlyList<GameplayFact> GameplayFacts => m_Output.Facts;
        public IReadOnlyList<PresentationCommand> PresentationCommands => m_Output.Presentation;
        public IReadOnlyList<SimulationTraceRecord> TraceRecords => m_Output.Trace;

        void IFixedPipelineProductValueRelease.ReleaseOwnedValue()
        {
            if (m_Output == null)
                return;
            m_OutputOwner.ReturnEvaluationOutput(m_Output);
            m_Output = null;
            m_OutputOwner = null;
        }

        static void ValidateHeaders(
            IReadOnlyList<GameplayFact> values,
            SimulationNumericProfile numericProfile,
            ActorId actorId,
            SimulationTick tick,
            string label)
        {
            for (int i = 0; i < values.Count; i++)
            {
                SimulationEventHeader header = values[i].Header;
                if (header.NumericProfile != numericProfile || header.ActorId != actorId || header.Tick != tick)
                    throw new ArgumentException($"{label} header does not match Actor result identity.");
            }
        }

        static void ValidateHeaders(
            IReadOnlyList<PresentationCommand> values,
            SimulationNumericProfile numericProfile,
            ActorId actorId,
            SimulationTick tick,
            string label)
        {
            for (int i = 0; i < values.Count; i++)
            {
                SimulationEventHeader header = values[i].Header;
                if (header.NumericProfile != numericProfile || header.ActorId != actorId || header.Tick != tick)
                    throw new ArgumentException($"{label} header does not match Actor result identity.");
            }
        }

        static void ValidateHeaders(
            IReadOnlyList<SimulationTraceRecord> values,
            SimulationNumericProfile numericProfile,
            ActorId actorId,
            SimulationTick tick,
            string label)
        {
            for (int i = 0; i < values.Count; i++)
            {
                SimulationEventHeader header = values[i].Header;
                if (header.NumericProfile != numericProfile || header.ActorId != actorId || header.Tick != tick)
                    throw new ArgumentException($"{label} header does not match Actor result identity.");
            }
        }
    }
}
