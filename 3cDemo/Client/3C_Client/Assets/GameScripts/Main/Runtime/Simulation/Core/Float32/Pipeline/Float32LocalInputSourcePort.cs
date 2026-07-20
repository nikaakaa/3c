using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
            int tickRate)
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
        }

        public ActorId ActorId { get; }
        public SimulationNumericProfile NumericProfile { get; }
        public SimulationTick SimulationTick { get; }
        public SimulationTickSourceIdentity Source { get; }
        public ulong InputSequence { get; }
        public int TickRate { get; }
    }

    public interface ISimulationInputAdapter
    {
        string AdapterIdentity { get; }
        SimulationNumericProfile NumericProfile { get; }
        CharacterSimulationInput BuildInput(SimulationInputBuildContext context);
    }

    public sealed class LocalSimulationInputBinding
    {
        public LocalSimulationInputBinding(ActorId actorId, ISimulationInputAdapter adapter)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Local input binding ActorId is invalid.", nameof(actorId));
            ActorId = actorId;
            Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        public ActorId ActorId { get; }
        public ISimulationInputAdapter Adapter { get; }
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
            IReadOnlyList<SimulationActorBinding> roster);
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
            identityValues[0] = "float32-local-input-source-port/1";
            identityValues[1] = sessionSource.ToString();
            for (int i = 0; i < values.Count; i++)
            {
                LocalSimulationInputBinding binding = values[i] ??
                    throw new ArgumentException("Local input Source contains a missing binding.", nameof(bindings));
                if (i > 0 && values[i - 1].ActorId.Equals(binding.ActorId))
                    throw new ArgumentException("Local input Source contains duplicate ActorId.", nameof(bindings));
                identityValues[i + 2] = $"{binding.ActorId}:{binding.Adapter.AdapterIdentity}:{binding.Adapter.NumericProfile}";
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
            IReadOnlyList<SimulationActorBinding> roster)
        {
            if (source.Kind != SimulationTickSourceKind.LocalLogic || source.SourceTick <= m_LastReadSourceTick)
                throw new InvalidOperationException("Local input Source requires a new LocalLogic source Tick.");
            if (!simulationTick.IsValid || !numericProfile.IsValid || tickRate <= 0 ||
                roster == null || roster.Count != m_Bindings.Count)
            {
                throw new InvalidOperationException("Local input Source request does not match its locked Actor bindings.");
            }
            var inputs = new SimulationPipelineActorInput<Float32StepInput>[roster.Count];
            for (int i = 0; i < roster.Count; i++)
            {
                SimulationActorBinding actor = roster[i] ??
                    throw new InvalidOperationException("Local input Source roster contains a missing Actor binding.");
                LocalSimulationInputBinding binding = m_Bindings[i];
                if (!actor.ActorId.Equals(binding.ActorId) || binding.Adapter.NumericProfile != numericProfile)
                    throw new InvalidOperationException($"Local input Source binding for Actor '{actor.ActorId}' is incompatible.");
                var context = new SimulationInputBuildContext(
                    actor.ActorId,
                    numericProfile,
                    simulationTick,
                    source,
                    source.SourceTick,
                    tickRate);
                CharacterSimulationInput input = binding.Adapter.BuildInput(context);
                if (input == null || input.NumericProfile != numericProfile ||
                    !input.TickSource.Equals(source) || input.Sequence != source.SourceTick)
                {
                    throw new InvalidOperationException($"Input Adapter '{binding.Adapter.AdapterIdentity}' returned input outside the Local Source contract.");
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
    }
}
