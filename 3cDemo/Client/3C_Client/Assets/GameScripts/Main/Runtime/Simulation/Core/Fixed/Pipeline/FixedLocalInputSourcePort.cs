using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    public readonly struct FixedCharacterInputBuildContext
    {
        public FixedCharacterInputBuildContext(
            ActorId actorId,
            SimulationTick simulationTick,
            SimulationTickSourceIdentity source,
            ulong inputSequence,
            int tickRate,
            int offensiveRequestDelayTicks,
            int maximumPendingRequests)
            : this(
                actorId,
                simulationTick,
                source,
                inputSequence,
                tickRate,
                offensiveRequestDelayTicks,
                maximumPendingRequests,
                null)
        {
        }

        public FixedCharacterInputBuildContext(
            ActorId actorId,
            SimulationTick simulationTick,
            SimulationTickSourceIdentity source,
            ulong inputSequence,
            int tickRate,
            int offensiveRequestDelayTicks,
            int maximumPendingRequests,
            FixedCommittedActorObservationSnapshot committedObservation)
        {
            if (!actorId.IsValid || !simulationTick.IsValid || string.IsNullOrEmpty(source.ClockId) ||
                source.Kind != SimulationTickSourceKind.LocalLogic ||
                inputSequence == 0 || tickRate <= 0 || offensiveRequestDelayTicks < 0 || maximumPendingRequests <= 0)
            {
                throw new ArgumentException("Fixed character input build context is incomplete.");
            }
            ActorId = actorId;
            SimulationTick = simulationTick;
            Source = source;
            InputSequence = inputSequence;
            TickRate = tickRate;
            OffensiveRequestDelayTicks = offensiveRequestDelayTicks;
            MaximumPendingRequests = maximumPendingRequests;
            CommittedObservation = committedObservation;
        }

        public ActorId ActorId { get; }
        public SimulationTick SimulationTick { get; }
        public SimulationTickSourceIdentity Source { get; }
        public ulong InputSequence { get; }
        public int TickRate { get; }
        public int OffensiveRequestDelayTicks { get; }
        public int MaximumPendingRequests { get; }
        public FixedCommittedActorObservationSnapshot CommittedObservation { get; }
    }

    public enum FixedCharacterControlSourceStateDisposition : byte
    {
        Prepared = 1,
        Committed = 2,
        Discarded = 3,
        Restored = 4
    }

    public readonly struct FixedCharacterControlSourceDiagnosticsSnapshot
    {
        public FixedCharacterControlSourceDiagnosticsSnapshot(
            int pendingOffensiveRequestCount,
            ulong oldestCaptureTick,
            ulong oldestEligibleTick)
        {
            if (pendingOffensiveRequestCount < 0)
                throw new ArgumentOutOfRangeException(nameof(pendingOffensiveRequestCount));
            PendingOffensiveRequestCount = pendingOffensiveRequestCount;
            OldestCaptureTick = oldestCaptureTick;
            OldestEligibleTick = oldestEligibleTick;
        }

        public int PendingOffensiveRequestCount { get; }
        public ulong OldestCaptureTick { get; }
        public ulong OldestEligibleTick { get; }
    }

    public interface IFixedCharacterControlSourceRuntime
    {
        string SourceIdentity { get; }
        ProgramId CharacterProgramId { get; }
        ProgramHash CharacterProgramHash { get; }
        CharacterSimulationInput BuildInput(FixedCharacterInputBuildContext context);
        byte[] CaptureState();
        void RestoreState(byte[] state);
        void NotifyStateDisposition(FixedCharacterControlSourceStateDisposition disposition);
        FixedCharacterControlSourceDiagnosticsSnapshot CaptureDiagnostics();
    }

    public sealed class FixedLocalSimulationInputBinding
    {
        public FixedLocalSimulationInputBinding(ActorId actorId, IFixedCharacterControlSourceRuntime controlSource)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Fixed Local input binding ActorId is invalid.", nameof(actorId));
            ActorId = actorId;
            ControlSource = controlSource ?? throw new ArgumentNullException(nameof(controlSource));
        }

        public ActorId ActorId { get; }
        public IFixedCharacterControlSourceRuntime ControlSource { get; }
    }

    public static class FixedLocalInputSourcePortContract
    {
        public const string PortId = "simulation.source.fixed-local-actor-input";
        public const string SchemaId = "fixed-local-actor-input-source";
        public const int SchemaVersion = 1;

        public static SimulationPipelinePortRequirement Requirement =>
            new SimulationPipelinePortRequirement(
                SimulationPipelineBindingPortRole.Source,
                PortId,
                SchemaId,
                SchemaVersion,
                SimulationPortDirection.Input);
    }

    public sealed class FixedLocalInputFrame
    {
        public FixedLocalInputFrame(FixedCanonicalInputBatch canonicalInputs, FixedTypedIngressBatch typedIngress)
        {
            CanonicalInputs = canonicalInputs ?? throw new ArgumentNullException(nameof(canonicalInputs));
            TypedIngress = typedIngress ?? throw new ArgumentNullException(nameof(typedIngress));
        }

        public FixedCanonicalInputBatch CanonicalInputs { get; }
        public FixedTypedIngressBatch TypedIngress { get; }
    }

    public interface IFixedLocalInputSourcePort : ISimulationRuntimePort
    {
        FixedLocalInputFrame Read(
            SimulationTickSourceIdentity source,
            SimulationTick simulationTick,
            int tickRate,
            IReadOnlyList<SimulationActorBinding> roster,
            FixedCommittedActorObservationSnapshot committedObservation);
        byte[] CaptureState();
        void RestoreState(byte[] state);
        void NotifyStateDisposition(FixedCharacterControlSourceStateDisposition disposition);
    }

    public sealed class FixedLocalInputSourcePort : IFixedLocalInputSourcePort
    {
        readonly ReadOnlyCollection<FixedLocalSimulationInputBinding> m_Bindings;
        readonly int m_OffensiveRequestDelayTicks;
        readonly int m_MaximumPendingRequests;
        ulong m_LastReadSourceTick;

        public FixedLocalInputSourcePort(
            SimulationComponentIdentity sessionSource,
            IEnumerable<FixedLocalSimulationInputBinding> bindings,
            int offensiveRequestDelayTicks,
            int maximumPendingRequests)
        {
            if (!sessionSource.IsValid || sessionSource.Role != SimulationComponentRole.SessionSource)
                throw new ArgumentException("Fixed Local Session Source identity is invalid.", nameof(sessionSource));
            if (offensiveRequestDelayTicks < 0)
                throw new ArgumentOutOfRangeException(nameof(offensiveRequestDelayTicks));
            if (maximumPendingRequests <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumPendingRequests));
            var values = bindings == null
                ? new List<FixedLocalSimulationInputBinding>()
                : new List<FixedLocalSimulationInputBinding>(bindings);
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            if (values.Count == 0)
                throw new ArgumentException("Fixed Local input Source requires an Actor roster.", nameof(bindings));
            var identity = new string[values.Count + 4];
            identity[0] = "fixed-local-input-source-port/1";
            identity[1] = sessionSource.ToString();
            identity[2] = offensiveRequestDelayTicks.ToString();
            identity[3] = maximumPendingRequests.ToString();
            for (int i = 0; i < values.Count; i++)
            {
                FixedLocalSimulationInputBinding binding = values[i] ??
                    throw new ArgumentException("Fixed Local input Source contains a missing binding.", nameof(bindings));
                if (i > 0 && values[i - 1].ActorId.Equals(binding.ActorId))
                    throw new ArgumentException("Fixed Local input Source contains duplicate ActorId.", nameof(bindings));
                identity[i + 4] = $"{binding.ActorId}:{binding.ControlSource.SourceIdentity}:{binding.ControlSource.CharacterProgramId}:{binding.ControlSource.CharacterProgramHash}";
            }
            m_Bindings = values.AsReadOnly();
            m_OffensiveRequestDelayTicks = offensiveRequestDelayTicks;
            m_MaximumPendingRequests = maximumPendingRequests;
            Descriptor = new SimulationPortDescriptor(
                FixedLocalInputSourcePortContract.PortId,
                FixedLocalInputSourcePortContract.SchemaId,
                FixedLocalInputSourcePortContract.SchemaVersion,
                SimulationPortDirection.Input,
                sessionSource.ComponentId,
                StableHash.Compute(identity));
        }

        public SimulationPortDescriptor Descriptor { get; }

        public FixedLocalInputFrame Read(
            SimulationTickSourceIdentity source,
            SimulationTick simulationTick,
            int tickRate,
            IReadOnlyList<SimulationActorBinding> roster,
            FixedCommittedActorObservationSnapshot committedObservation)
        {
            if (source.Kind != SimulationTickSourceKind.LocalLogic || source.SourceTick <= m_LastReadSourceTick)
                throw new InvalidOperationException("Fixed Local input Source requires a new LocalLogic source Tick.");
            if (!simulationTick.IsValid || tickRate <= 0 ||
                roster == null || roster.Count != m_Bindings.Count)
            {
                throw new InvalidOperationException("Fixed Local input Source request does not match its locked Actor roster.");
            }
            if (committedObservation == null || committedObservation.ObservationTick != simulationTick.Value - 1)
                throw new InvalidOperationException("Fixed Local input Source requires the committed observation immediately preceding the requested simulation Tick.");
            if (committedObservation.Actors.Count != roster.Count)
                throw new InvalidOperationException("Fixed Local input Source committed observation does not match its locked Actor roster.");
            for (int i = 0; i < roster.Count; i++)
            {
                if (roster[i] == null || !committedObservation.Actors[i].ActorId.Equals(roster[i].ActorId))
                    throw new InvalidOperationException("Fixed Local input Source committed observation Actor order does not match its locked roster.");
            }
            var inputs = new SimulationPipelineActorInput<FixedStepInput>[roster.Count];
            for (int i = 0; i < roster.Count; i++)
            {
                SimulationActorBinding actor = roster[i] ??
                    throw new InvalidOperationException("Fixed Local input Source roster contains a missing Actor binding.");
                FixedLocalSimulationInputBinding binding = m_Bindings[i];
                IFixedCharacterControlSourceRuntime controlSource = binding.ControlSource;
                if (!actor.ActorId.Equals(binding.ActorId) ||
                    !controlSource.CharacterProgramId.Equals(actor.ProgramId) ||
                    !controlSource.CharacterProgramHash.Equals(actor.ProgramHash))
                {
                    throw new InvalidOperationException($"Fixed Local input binding for Actor '{actor.ActorId}' is incompatible.");
                }
                var context = new FixedCharacterInputBuildContext(
                    actor.ActorId,
                    simulationTick,
                    source,
                    source.SourceTick,
                    tickRate,
                    m_OffensiveRequestDelayTicks,
                    m_MaximumPendingRequests,
                    committedObservation);
                CharacterSimulationInput input = controlSource.BuildInput(context);
                if (input == null || input.NumericProfile != FixedSimulationNumericProfile.Value ||
                    !input.TickSource.Equals(source) || input.Sequence != source.SourceTick)
                {
                    throw new InvalidOperationException($"Fixed Control Source '{controlSource.SourceIdentity}' returned input outside the Local Source contract.");
                }
                inputs[i] = new SimulationPipelineActorInput<FixedStepInput>(
                    actor.ActorId,
                    source.SourceTick,
                    new FixedStepInput(input));
            }
            m_LastReadSourceTick = source.SourceTick;
            return new FixedLocalInputFrame(
                new FixedCanonicalInputBatch(source, inputs),
                new FixedTypedIngressBatch(Array.Empty<SimulationPipelineTypedIngress<SimulationIngress>>()));
        }

        public byte[] CaptureState()
        {
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(0x53434c58);
            writer.WriteInt32(1);
            writer.WriteUInt64(m_LastReadSourceTick);
            writer.WriteInt32(m_Bindings.Count);
            for (int i = 0; i < m_Bindings.Count; i++)
            {
                FixedLocalSimulationInputBinding binding = m_Bindings[i];
                writer.WriteString(binding.ActorId.Value);
                writer.WriteString(binding.ControlSource.SourceIdentity);
                writer.WriteBytes(binding.ControlSource.CaptureState());
            }
            return writer.ToArray();
        }

        public void RestoreState(byte[] state)
        {
            var reader = new CanonicalReader(state ?? throw new ArgumentNullException(nameof(state)));
            if (reader.ReadUInt32() != 0x53434c58 || reader.ReadInt32() != 1)
                throw new InvalidDataException("Fixed Local Control Source state header is invalid.");
            ulong lastReadSourceTick = reader.ReadUInt64();
            int count = reader.ReadInt32();
            if (count != m_Bindings.Count)
                throw new InvalidDataException("Fixed Local Control Source state roster does not match its locked bindings.");
            for (int i = 0; i < count; i++)
            {
                FixedLocalSimulationInputBinding binding = m_Bindings[i];
                string actorId = reader.ReadString();
                string sourceIdentity = reader.ReadString();
                byte[] payload = reader.ReadBytes();
                if (!string.Equals(actorId, binding.ActorId.Value, StringComparison.Ordinal) ||
                    !string.Equals(sourceIdentity, binding.ControlSource.SourceIdentity, StringComparison.Ordinal))
                {
                    throw new InvalidDataException("Fixed Local Control Source state identity does not match its locked binding.");
                }
                binding.ControlSource.RestoreState(payload);
            }
            reader.RequireComplete();
            m_LastReadSourceTick = lastReadSourceTick;
        }

        public void NotifyStateDisposition(FixedCharacterControlSourceStateDisposition disposition)
        {
            if (!Enum.IsDefined(typeof(FixedCharacterControlSourceStateDisposition), disposition))
                throw new ArgumentOutOfRangeException(nameof(disposition));
            for (int i = 0; i < m_Bindings.Count; i++)
                m_Bindings[i].ControlSource.NotifyStateDisposition(disposition);
        }
    }
}
