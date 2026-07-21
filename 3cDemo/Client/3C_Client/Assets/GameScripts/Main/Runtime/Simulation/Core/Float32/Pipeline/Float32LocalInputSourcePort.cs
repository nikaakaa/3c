using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace ThirdPersonSimulation
{
    public readonly struct SimulationInputBuildContext
    {
        public SimulationInputBuildContext(
            ActorId actorId,
            SimulationNumericProfile numericProfile,
            SimulationTick simulationTick,
            SimulationTickSourceIdentity source,
            ulong inputSequence,
            int tickRate,
            CommittedActorObservationSnapshot committedObservation)
        {
            if (!actorId.IsValid || !numericProfile.IsValid || !simulationTick.IsValid ||
                string.IsNullOrEmpty(source.ClockId) || inputSequence == 0 || tickRate <= 0)
                throw new ArgumentException("Simulation input build context is incomplete.");
            ActorId = actorId;
            NumericProfile = numericProfile;
            SimulationTick = simulationTick;
            Source = source;
            InputSequence = inputSequence;
            TickRate = tickRate;
            CommittedObservation = committedObservation ?? throw new ArgumentNullException(nameof(committedObservation));
        }

        public ActorId ActorId { get; }
        public SimulationNumericProfile NumericProfile { get; }
        public SimulationTick SimulationTick { get; }
        public SimulationTickSourceIdentity Source { get; }
        public ulong InputSequence { get; }
        public int TickRate { get; }
        public CommittedActorObservationSnapshot CommittedObservation { get; }
    }

    [Flags]
    public enum CharacterControlSourceCapability : byte
    {
        None = 0,
        CommittedObservation = 1,
        TransactionalState = 2
    }

    public interface ICharacterControlSourceRuntime
    {
        string SourceIdentity { get; }
        SimulationNumericProfile NumericProfile { get; }
        ProgramId CharacterProgramId { get; }
        ProgramHash CharacterProgramHash { get; }
        CharacterControlSourceCapability Capabilities { get; }
        CharacterSimulationInput BuildInput(SimulationInputBuildContext context);
    }

    public interface ICharacterControlSourceStateRuntime
    {
        string StateSchemaId { get; }
        int StateSchemaVersion { get; }
        byte[] CaptureState();
        void RestoreState(byte[] state);
    }

    public enum CharacterControlSourceStateDisposition : byte
    {
        Prepared = 1,
        Committed = 2,
        Discarded = 3,
        Restored = 4
    }

    public interface ICharacterControlSourceTransactionObserver
    {
        void NotifyStateDisposition(CharacterControlSourceStateDisposition disposition);
    }

    public interface ICharacterControlSourceRosterRuntime
    {
        void ValidateRoster(
            ActorId actorId,
            IReadOnlyList<ActorId> roster,
            StableHash committedObservationCapability);
    }

    public sealed class LocalSimulationInputBinding
    {
        public LocalSimulationInputBinding(ActorId actorId, ICharacterControlSourceRuntime controlSource)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Local input binding ActorId is invalid.", nameof(actorId));
            ActorId = actorId;
            ControlSource = controlSource ?? throw new ArgumentNullException(nameof(controlSource));
        }

        public ActorId ActorId { get; }
        public ICharacterControlSourceRuntime ControlSource { get; }
    }

    public static class Float32LocalInputSourcePortContract
    {
        public const string PortId = "simulation.source.local-actor-input";
        public const string SchemaId = "float32-local-actor-input-source";
        public const int SchemaVersion = 1;

        public static SimulationPipelinePortRequirement Requirement =>
            new SimulationPipelinePortRequirement(
                SimulationPipelineBindingPortRole.Source,
                PortId,
                SchemaId,
                SchemaVersion,
                SimulationPortDirection.Input);
    }

    public sealed class Float32LocalInputFrame
    {
        public Float32LocalInputFrame(
            Float32CanonicalInputBatch canonicalInputs,
            Float32TypedIngressBatch typedIngress)
        {
            CanonicalInputs = canonicalInputs ?? throw new ArgumentNullException(nameof(canonicalInputs));
            TypedIngress = typedIngress ?? throw new ArgumentNullException(nameof(typedIngress));
        }

        public Float32CanonicalInputBatch CanonicalInputs { get; }
        public Float32TypedIngressBatch TypedIngress { get; }
    }

    public interface IFloat32LocalInputSourcePort : ISimulationRuntimePort
    {
        Float32LocalInputFrame Read(
            SimulationTickSourceIdentity source,
            SimulationTick simulationTick,
            SimulationNumericProfile numericProfile,
            int tickRate,
            IReadOnlyList<SimulationActorBinding> roster,
            CommittedActorObservationSnapshot committedObservation);
        byte[] CaptureState();
        void RestoreState(byte[] state);
        void NotifyStateDisposition(CharacterControlSourceStateDisposition disposition);
    }

    public sealed class Float32LocalInputSourcePort : IFloat32LocalInputSourcePort
    {
        readonly ReadOnlyCollection<LocalSimulationInputBinding> m_Bindings;
        ulong m_LastReadSourceTick;

        public Float32LocalInputSourcePort(
            SimulationComponentIdentity sessionSource,
            IEnumerable<LocalSimulationInputBinding> bindings)
        {
            if (!sessionSource.IsValid || sessionSource.Role != SimulationComponentRole.SessionSource)
                throw new ArgumentException("Session Source identity is invalid.", nameof(sessionSource));
            var values = bindings == null
                ? new List<LocalSimulationInputBinding>()
                : new List<LocalSimulationInputBinding>(bindings);
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            if (values.Count == 0)
                throw new ArgumentException("Local input Source requires explicit Actor input bindings.", nameof(bindings));
            var identityValues = new string[values.Count + 2];
            identityValues[0] = "float32-local-input-source-port/2";
            identityValues[1] = sessionSource.ToString();
            for (int i = 0; i < values.Count; i++)
            {
                LocalSimulationInputBinding binding = values[i] ??
                    throw new ArgumentException("Local input Source contains a missing binding.", nameof(bindings));
                if (i > 0 && values[i - 1].ActorId.Equals(binding.ActorId))
                    throw new ArgumentException("Local input Source contains duplicate ActorId.", nameof(bindings));
                ICharacterControlSourceRuntime source = binding.ControlSource;
                if (source.Capabilities.HasFlag(CharacterControlSourceCapability.TransactionalState) !=
                    source is ICharacterControlSourceStateRuntime)
                {
                    throw new ArgumentException($"Control Source '{source.SourceIdentity}' state capability and runtime contract disagree.", nameof(bindings));
                }
                string stateIdentity = source is ICharacterControlSourceStateRuntime stateful
                    ? $"{stateful.StateSchemaId}:{stateful.StateSchemaVersion}"
                    : "stateless";
                identityValues[i + 2] = $"{binding.ActorId}:{source.SourceIdentity}:{source.NumericProfile}:{source.CharacterProgramId}:{source.CharacterProgramHash}:{source.Capabilities}:{stateIdentity}";
            }
            m_Bindings = values.AsReadOnly();
            Descriptor = new SimulationPortDescriptor(
                Float32LocalInputSourcePortContract.PortId,
                Float32LocalInputSourcePortContract.SchemaId,
                Float32LocalInputSourcePortContract.SchemaVersion,
                SimulationPortDirection.Input,
                sessionSource.ComponentId,
                StableHash.Compute(identityValues));
        }

        public SimulationPortDescriptor Descriptor { get; }

        public Float32LocalInputFrame Read(
            SimulationTickSourceIdentity source,
            SimulationTick simulationTick,
            SimulationNumericProfile numericProfile,
            int tickRate,
            IReadOnlyList<SimulationActorBinding> roster,
            CommittedActorObservationSnapshot committedObservation)
        {
            if (source.Kind != SimulationTickSourceKind.LocalLogic || source.SourceTick <= m_LastReadSourceTick)
                throw new InvalidOperationException("Local input Source requires a new LocalLogic source Tick.");
            if (!simulationTick.IsValid || !numericProfile.IsValid || tickRate <= 0 ||
                roster == null || roster.Count != m_Bindings.Count)
            {
                throw new InvalidOperationException("Local input Source request does not match its locked Actor bindings.");
            }
            if (committedObservation == null || committedObservation.ObservationTick != simulationTick.Value - 1)
                throw new InvalidOperationException("Local input Source requires the committed observation immediately preceding the requested simulation Tick.");
            if (committedObservation.Actors.Count != roster.Count)
                throw new InvalidOperationException("Local input Source committed observation does not match its locked Actor roster.");
            for (int i = 0; i < roster.Count; i++)
            {
                if (roster[i] == null || committedObservation.Actors[i].ActorId != roster[i].ActorId)
                    throw new InvalidOperationException("Local input Source committed observation Actor order does not match its locked roster.");
            }
            var inputs = new SimulationPipelineActorInput<Float32StepInput>[roster.Count];
            for (int i = 0; i < roster.Count; i++)
            {
                SimulationActorBinding actor = roster[i] ??
                    throw new InvalidOperationException("Local input Source roster contains a missing Actor binding.");
                LocalSimulationInputBinding binding = m_Bindings[i];
                ICharacterControlSourceRuntime controlSource = binding.ControlSource;
                if (!actor.ActorId.Equals(binding.ActorId) || controlSource.NumericProfile != numericProfile ||
                    !controlSource.CharacterProgramId.Equals(actor.ProgramId) ||
                    !controlSource.CharacterProgramHash.Equals(actor.ProgramHash))
                    throw new InvalidOperationException($"Local input Source binding for Actor '{actor.ActorId}' is incompatible.");
                var context = new SimulationInputBuildContext(
                    actor.ActorId,
                    numericProfile,
                    simulationTick,
                    source,
                    source.SourceTick,
                    tickRate,
                    committedObservation);
                CharacterSimulationInput input = controlSource.BuildInput(context);
                if (input == null || input.NumericProfile != numericProfile ||
                    !input.TickSource.Equals(source) || input.Sequence != source.SourceTick)
                {
                    throw new InvalidOperationException($"Control Source '{controlSource.SourceIdentity}' returned input outside the Local Source contract.");
                }
                inputs[i] = new SimulationPipelineActorInput<Float32StepInput>(
                    actor.ActorId,
                    source.SourceTick,
                    new Float32StepInput(input));
            }
            m_LastReadSourceTick = source.SourceTick;
            return new Float32LocalInputFrame(
                new Float32CanonicalInputBatch(source, inputs),
                new Float32TypedIngressBatch(Array.Empty<SimulationPipelineTypedIngress<SimulationIngress>>()));
        }

        public byte[] CaptureState()
        {
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(0x53434c46);
            writer.WriteInt32(1);
            writer.WriteUInt64(m_LastReadSourceTick);
            int statefulCount = 0;
            for (int i = 0; i < m_Bindings.Count; i++)
            {
                if (m_Bindings[i].ControlSource is ICharacterControlSourceStateRuntime)
                    statefulCount++;
            }
            writer.WriteInt32(statefulCount);
            for (int i = 0; i < m_Bindings.Count; i++)
            {
                if (m_Bindings[i].ControlSource is not ICharacterControlSourceStateRuntime stateful)
                    continue;
                writer.WriteString(m_Bindings[i].ActorId.Value);
                writer.WriteString(m_Bindings[i].ControlSource.SourceIdentity);
                writer.WriteString(stateful.StateSchemaId);
                writer.WriteInt32(stateful.StateSchemaVersion);
                writer.WriteBytes(stateful.CaptureState());
            }
            return writer.ToArray();
        }

        public void RestoreState(byte[] state)
        {
            var reader = new CanonicalReader(state ?? throw new ArgumentNullException(nameof(state)));
            if (reader.ReadUInt32() != 0x53434c46 || reader.ReadInt32() != 1)
                throw new InvalidDataException("Local Control Source state header is invalid.");
            ulong lastReadSourceTick = reader.ReadUInt64();
            int count = reader.ReadInt32();
            var stateful = new List<LocalSimulationInputBinding>();
            for (int i = 0; i < m_Bindings.Count; i++)
            {
                if (m_Bindings[i].ControlSource is ICharacterControlSourceStateRuntime)
                    stateful.Add(m_Bindings[i]);
            }
            if (count != stateful.Count)
                throw new InvalidDataException("Local Control Source state roster does not match its locked bindings.");
            for (int i = 0; i < count; i++)
            {
                LocalSimulationInputBinding binding = stateful[i];
                var runtime = (ICharacterControlSourceStateRuntime)binding.ControlSource;
                string actorId = reader.ReadString();
                string sourceIdentity = reader.ReadString();
                string schemaId = reader.ReadString();
                int schemaVersion = reader.ReadInt32();
                byte[] payload = reader.ReadBytes();
                if (!string.Equals(actorId, binding.ActorId.Value, StringComparison.Ordinal) ||
                    !string.Equals(sourceIdentity, binding.ControlSource.SourceIdentity, StringComparison.Ordinal) ||
                    !string.Equals(schemaId, runtime.StateSchemaId, StringComparison.Ordinal) ||
                    schemaVersion != runtime.StateSchemaVersion)
                {
                    throw new InvalidDataException("Local Control Source state identity does not match its locked binding.");
                }
                runtime.RestoreState(payload);
            }
            reader.RequireComplete();
            m_LastReadSourceTick = lastReadSourceTick;
        }

        public void NotifyStateDisposition(CharacterControlSourceStateDisposition disposition)
        {
            if (!Enum.IsDefined(typeof(CharacterControlSourceStateDisposition), disposition))
                throw new ArgumentOutOfRangeException(nameof(disposition));
            for (int i = 0; i < m_Bindings.Count; i++)
            {
                if (m_Bindings[i].ControlSource is ICharacterControlSourceTransactionObserver observer)
                    observer.NotifyStateDisposition(disposition);
            }
        }
    }
}
