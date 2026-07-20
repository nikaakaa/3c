using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonGameplay.Networking
{
    public sealed class GameplayNetworkModelSourceRequirements
    {
        readonly ReadOnlyCollection<SimulationPipelinePassRequirement> m_RequiredPasses;
        readonly ReadOnlyCollection<SimulationPipelinePortRequirement> m_RequiredSourcePorts;

        public GameplayNetworkModelSourceRequirements(
            SimulationComponentIdentity model,
            SimulationProtocolIdentity protocol,
            SimulationComponentIdentity endpoint,
            string sourceComponentId,
            string sourceSemanticVersion,
            NumericProfileId numericProfileId,
            TargetAbiVersion targetAbiVersion,
            SimulationTickSourceKind outerTickKind,
            SimulationPipelineExecutionSupport executionSupport,
            bool deterministic,
            string requiredBackendId,
            SimulationPipelineId requiredPipelineId,
            WorldCapability requiredSolverCapabilities,
            IEnumerable<SimulationPipelinePassRequirement> requiredPasses,
            IEnumerable<SimulationPipelinePortRequirement> requiredSourcePorts)
        {
            RequireRole(model, SimulationComponentRole.Model, nameof(model));
            RequireRole(endpoint, SimulationComponentRole.Endpoint, nameof(endpoint));
            if (!protocol.IsValid || !numericProfileId.IsValid || !targetAbiVersion.IsValid ||
                !Enum.IsDefined(typeof(SimulationTickSourceKind), outerTickKind) ||
                (executionSupport & SimulationPipelineExecutionSupport.Forward) == 0 ||
                !requiredPipelineId.IsValid || requiredSolverCapabilities == WorldCapability.None)
            {
                throw new ArgumentException("Network Model Source requirements are incomplete.");
            }
            Model = model;
            Protocol = protocol;
            Endpoint = endpoint;
            SourceComponentId = SimulationIdentityAuthoring.Require(sourceComponentId, nameof(sourceComponentId));
            SourceSemanticVersion = SimulationIdentityAuthoring.Require(sourceSemanticVersion, nameof(sourceSemanticVersion));
            NumericProfileId = numericProfileId;
            TargetAbiVersion = targetAbiVersion;
            OuterTickKind = outerTickKind;
            ExecutionSupport = executionSupport;
            Deterministic = deterministic;
            RequiredBackendId = SimulationIdentityAuthoring.Require(requiredBackendId, nameof(requiredBackendId));
            RequiredPipelineId = requiredPipelineId;
            RequiredSolverCapabilities = requiredSolverCapabilities;
            m_RequiredPasses = FreezePasses(requiredPasses);
            m_RequiredSourcePorts = FreezePorts(requiredSourcePorts);
            if (m_RequiredPasses.Count == 0)
                throw new ArgumentException("Network Model Source requires at least one model Pipeline Pass.", nameof(requiredPasses));
            if (m_RequiredSourcePorts.Count == 0)
                throw new ArgumentException("Network Model Source requires at least one Source port.", nameof(requiredSourcePorts));
            RequirementsHash = ComputeHash();
        }

        public SimulationComponentIdentity Model { get; }
        public string ModelId => Model.ComponentId;
        public SimulationProtocolIdentity Protocol { get; }
        public SimulationComponentIdentity Endpoint { get; }
        public string SourceComponentId { get; }
        public string SourceSemanticVersion { get; }
        public NumericProfileId NumericProfileId { get; }
        public TargetAbiVersion TargetAbiVersion { get; }
        public SimulationTickSourceKind OuterTickKind { get; }
        public SimulationPipelineExecutionSupport ExecutionSupport { get; }
        public bool Deterministic { get; }
        public string RequiredBackendId { get; }
        public SimulationPipelineId RequiredPipelineId { get; }
        public WorldCapability RequiredSolverCapabilities { get; }
        public IReadOnlyList<SimulationPipelinePassRequirement> RequiredPasses => m_RequiredPasses;
        public IReadOnlyList<SimulationPipelinePortRequirement> RequiredSourcePorts => m_RequiredSourcePorts;
        public StableHash RequirementsHash { get; }

        StableHash ComputeHash()
        {
            var values = new List<string>
            {
                "gameplay-network-model-source-requirements/1",
                Model.ToString(),
                Protocol.ToString(),
                Endpoint.ToString(),
                SourceComponentId,
                SourceSemanticVersion,
                NumericProfileId.Value,
                TargetAbiVersion.ToString(),
                ((int)OuterTickKind).ToString(),
                ((int)ExecutionSupport).ToString(),
                Deterministic ? "1" : "0",
                RequiredBackendId,
                RequiredPipelineId.Value,
                Convert.ToUInt64(RequiredSolverCapabilities).ToString()
            };
            for (int i = 0; i < m_RequiredPasses.Count; i++)
                values.Add($"pass:{m_RequiredPasses[i]}");
            for (int i = 0; i < m_RequiredSourcePorts.Count; i++)
            {
                SimulationPipelinePortRequirement port = m_RequiredSourcePorts[i];
                values.Add($"port:{port.PortId}:{port.SchemaId}:{port.SchemaVersion}:{(int)port.Direction}");
            }
            return StableHash.Compute(values.ToArray());
        }

        static ReadOnlyCollection<SimulationPipelinePassRequirement> FreezePasses(
            IEnumerable<SimulationPipelinePassRequirement> source)
        {
            var values = source == null
                ? new List<SimulationPipelinePassRequirement>()
                : new List<SimulationPipelinePassRequirement>(source);
            values.Sort((left, right) => left.PassId.CompareTo(right.PassId));
            for (int i = 0; i < values.Count; i++)
            {
                if (!values[i].PassId.IsValid || !values[i].ImplementationVersion.IsValid ||
                    !Enum.IsDefined(typeof(SimulationPipelinePhase), values[i].Phase) ||
                    i > 0 && values[i - 1].PassId.Equals(values[i].PassId))
                {
                    throw new ArgumentException("Network Model Source contains an invalid or duplicate required Pass.", nameof(source));
                }
            }
            return values.AsReadOnly();
        }

        static ReadOnlyCollection<SimulationPipelinePortRequirement> FreezePorts(
            IEnumerable<SimulationPipelinePortRequirement> source)
        {
            var values = source == null
                ? new List<SimulationPipelinePortRequirement>()
                : new List<SimulationPipelinePortRequirement>(source);
            values.Sort((left, right) => string.CompareOrdinal(left.PortId, right.PortId));
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].Role != SimulationPipelineBindingPortRole.Source ||
                    i > 0 && string.Equals(values[i - 1].PortId, values[i].PortId, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Network Model Source contains an invalid or duplicate Source port requirement.", nameof(source));
                }
            }
            return values.AsReadOnly();
        }

        static void RequireRole(SimulationComponentIdentity identity, SimulationComponentRole role, string parameter)
        {
            if (!identity.IsValid || identity.Role != role)
                throw new ArgumentException($"Component role must be {role}.", parameter);
        }
    }

    public sealed class GameplayNetworkModelPreparationContext
    {
        public GameplayNetworkModelPreparationContext(SimulationSessionSourcePreparationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            SessionId = context.SessionId;
            SourceClockId = context.SourceClockId;
            TickRate = context.TickRate;
            ProgramRuntime = context.ProgramRuntime;
            ExecutionBackend = context.ExecutionBackend;
            WorldSolver = context.WorldSolver;
            WorldIdentity = context.WorldIdentity;
            Registrations = context.Registrations;
        }

        public SimulationSessionId SessionId { get; }
        public SimulationSourceClockId SourceClockId { get; }
        public int TickRate { get; }
        public SimulationProgramRuntimeDescriptor ProgramRuntime { get; }
        public SimulationExecutionBackendDefinition ExecutionBackend { get; }
        public SimulationWorldSolverDefinitionDescriptor WorldSolver { get; }
        public SimulationWorldIdentityDescriptor WorldIdentity { get; }
        public IReadOnlyList<ISimulationActorRegistration> Registrations { get; }
    }

    public abstract class GameplayNetworkModelDefinition : ScriptableObject
    {
        public abstract SimulationComponentIdentity BuildModelIdentity();
    }

    public abstract class GameplayNetworkModelSessionSourceDefinition : SimulationSessionSourceDefinition
    {
        public GameplayNetworkModelSourceRequirements Requirements =>
            BuildRequirements() ?? throw new InvalidOperationException($"Network Model '{name}' returned no Source requirements.");

        public sealed override SimulationSessionSourceAuthoringDescriptor BuildAuthoringDescriptor()
        {
            GameplayNetworkModelSourceRequirements requirements = Requirements;
            var sourceIdentity = new SimulationComponentIdentity(
                SimulationComponentRole.SessionSource,
                requirements.SourceComponentId,
                requirements.SourceSemanticVersion,
                requirements.RequirementsHash);
            var source = new SimulationSessionSourceDescriptor(
                sourceIdentity,
                requirements.NumericProfileId,
                requirements.TargetAbiVersion,
                requirements.OuterTickKind,
                requirements.ExecutionSupport,
                requirements.Deterministic,
                requirements.RequiredBackendId,
                requirements.RequiredPipelineId,
                requirements.Model,
                requirements.Endpoint,
                requirements.Protocol,
                requirements.RequiredSolverCapabilities,
                requirements.RequiredPasses,
                requirements.RequiredSourcePorts);
            var ports = new SimulationPortDescriptor[requirements.RequiredSourcePorts.Count];
            for (int i = 0; i < ports.Length; i++)
            {
                SimulationPipelinePortRequirement required = requirements.RequiredSourcePorts[i];
                ports[i] = SimulationPortDescriptor.CreateSource(required, sourceIdentity);
            }
            return new SimulationSessionSourceAuthoringDescriptor(source, ports);
        }

        protected sealed override ISimulationSessionSourcePreparation CreatePreparationCore(
            SimulationSessionSourcePreparationContext context)
        {
            GameplayNetworkModelSourceRequirements requirements = Requirements;
            if (!context.ProgramRuntime.NumericProfileId.Equals(requirements.NumericProfileId))
            {
                throw new InvalidOperationException(
                    $"Network Model '{requirements.ModelId}' requires NumericProfile '{requirements.NumericProfileId}', " +
                    $"but the selected Program Runtime provides '{context.ProgramRuntime.NumericProfileId}'.");
            }
            if (!context.ProgramRuntime.TargetAbiVersion.Equals(requirements.TargetAbiVersion))
            {
                throw new InvalidOperationException(
                    $"Network Model '{requirements.ModelId}' requires Target ABI '{requirements.TargetAbiVersion}', " +
                    $"but the selected Program Runtime provides '{context.ProgramRuntime.TargetAbiVersion}'.");
            }
            ISimulationSessionSourcePreparation preparation = CreateModelPreparation(
                new GameplayNetworkModelPreparationContext(context),
                requirements) ?? throw new InvalidOperationException(
                    $"Network Model '{requirements.ModelId}' returned no Source preparation.");
            return new GameplayNetworkModelSourcePreparationValidation(preparation, requirements);
        }

        protected abstract GameplayNetworkModelSourceRequirements BuildRequirements();

        protected abstract ISimulationSessionSourcePreparation CreateModelPreparation(
            GameplayNetworkModelPreparationContext context,
            GameplayNetworkModelSourceRequirements requirements);
    }

    internal sealed class GameplayNetworkModelSourcePreparationValidation : ISimulationSessionSourcePreparation
    {
        readonly ISimulationSessionSourcePreparation m_Inner;
        readonly GameplayNetworkModelSourceRequirements m_Requirements;
        bool m_Disposed;

        public GameplayNetworkModelSourcePreparationValidation(
            ISimulationSessionSourcePreparation inner,
            GameplayNetworkModelSourceRequirements requirements)
        {
            m_Inner = inner ?? throw new ArgumentNullException(nameof(inner));
            m_Requirements = requirements ?? throw new ArgumentNullException(nameof(requirements));
            ValidateDescriptor(m_Inner.Descriptor, m_Requirements);
        }

        public SimulationSessionPreparationStatus Status => m_Inner.Status;
        public SimulationSessionFailure Failure => m_Inner.Failure;
        public SimulationSessionSourceDescriptor Descriptor => m_Inner.Descriptor;

        public SimulationSessionPreparationStatus Step(SimulationSessionLogicTickContext context)
        {
            ThrowIfDisposed();
            SimulationSessionPreparationStatus status = m_Inner.Step(context);
            if (status == SimulationSessionPreparationStatus.Ready)
                ValidateDescriptor(m_Inner.Descriptor, m_Requirements);
            return status;
        }

        public ISimulationSessionPreparedSource TakePreparedSource()
        {
            ThrowIfDisposed();
            ISimulationSessionPreparedSource source = m_Inner.TakePreparedSource() ??
                throw new InvalidOperationException($"Network Model '{m_Requirements.ModelId}' returned no prepared Source.");
            try
            {
                ValidateDescriptor(source.Descriptor, m_Requirements);
                ValidateRuntimePorts(source.RuntimePorts, source.Descriptor.Identity, m_Requirements);
                return source;
            }
            catch
            {
                source.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Inner.Dispose();
        }

        static void ValidateDescriptor(
            SimulationSessionSourceDescriptor descriptor,
            GameplayNetworkModelSourceRequirements requirements)
        {
            if (descriptor == null)
                throw new InvalidOperationException($"Network Model '{requirements.ModelId}' Source has no descriptor.");
            if (!string.Equals(descriptor.Identity.ComponentId, requirements.SourceComponentId, StringComparison.Ordinal) ||
                !string.Equals(descriptor.Identity.SemanticVersion, requirements.SourceSemanticVersion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Network Model '{requirements.ModelId}' Source identity does not match its requirement.");
            }
            if (!descriptor.Model.HasValue || !descriptor.Model.Value.Equals(requirements.Model))
                throw new InvalidOperationException($"Network Model '{requirements.ModelId}' Source has no matching Model identity.");
            if (!descriptor.Endpoint.HasValue || !descriptor.Endpoint.Value.Equals(requirements.Endpoint))
                throw new InvalidOperationException($"Network Model '{requirements.ModelId}' Source has no matching Endpoint identity.");
            if (!descriptor.Protocol.HasValue || !descriptor.Protocol.Value.Equals(requirements.Protocol))
                throw new InvalidOperationException($"Network Model '{requirements.ModelId}' Source has no matching protocol identity.");
            if (!descriptor.NumericProfileId.Equals(requirements.NumericProfileId) ||
                !descriptor.TargetAbiVersion.Equals(requirements.TargetAbiVersion))
            {
                throw new InvalidOperationException($"Network Model '{requirements.ModelId}' Source Target ABI does not match its requirement.");
            }
            if (descriptor.OuterTickKind != requirements.OuterTickKind ||
                descriptor.ExecutionSupport != requirements.ExecutionSupport ||
                descriptor.Deterministic != requirements.Deterministic)
            {
                throw new InvalidOperationException($"Network Model '{requirements.ModelId}' Source execution contract does not match its Definition.");
            }
            if (!string.Equals(descriptor.RequiredBackendId, requirements.RequiredBackendId, StringComparison.Ordinal))
                throw new InvalidOperationException($"Network Model '{requirements.ModelId}' Source Backend requirement does not match its Definition.");
            if (!descriptor.RequiredPipelineId.Equals(requirements.RequiredPipelineId))
                throw new InvalidOperationException($"Network Model '{requirements.ModelId}' Source Pipeline requirement does not match its Definition.");
            if (descriptor.RequiredSolverCapabilities != requirements.RequiredSolverCapabilities)
                throw new InvalidOperationException($"Network Model '{requirements.ModelId}' Source Solver capability requirement does not match its Definition.");
            RequirePassRequirements(descriptor.RequiredPipelinePasses, requirements.RequiredPasses, requirements.ModelId);
            RequirePortRequirements(descriptor.RequiredPipelineSourcePorts, requirements.RequiredSourcePorts, requirements.ModelId);
        }

        static void ValidateRuntimePorts(
            SimulationRuntimePortSet runtimePorts,
            SimulationComponentIdentity source,
            GameplayNetworkModelSourceRequirements requirements)
        {
            if (runtimePorts == null)
                throw new InvalidOperationException($"Network Model '{requirements.ModelId}' Source returned no runtime ports.");
            for (int requiredIndex = 0; requiredIndex < requirements.RequiredSourcePorts.Count; requiredIndex++)
            {
                SimulationPipelinePortRequirement required = requirements.RequiredSourcePorts[requiredIndex];
                bool found = false;
                for (int portIndex = 0; portIndex < runtimePorts.Ports.Count; portIndex++)
                {
                    SimulationPortDescriptor actual = runtimePorts.Ports[portIndex].Descriptor;
                    if (!string.Equals(actual.PortId, required.PortId, StringComparison.Ordinal))
                        continue;
                    found = Matches(actual, required) &&
                            string.Equals(actual.OwnerComponentId, source.ComponentId, StringComparison.Ordinal);
                    break;
                }
                if (!found)
                {
                    throw new InvalidOperationException(
                        $"Network Model '{requirements.ModelId}' Source runtime port '{required.PortId}' is missing or incompatible.");
                }
            }
        }

        static void RequirePassRequirements(
            IReadOnlyList<SimulationPipelinePassRequirement> actual,
            IReadOnlyList<SimulationPipelinePassRequirement> expected,
            string modelId)
        {
            if (actual.Count != expected.Count)
                throw new InvalidOperationException($"Network Model '{modelId}' Source required Pass count does not match its Definition.");
            for (int i = 0; i < actual.Count; i++)
            {
                if (!actual[i].Equals(expected[i]))
                    throw new InvalidOperationException($"Network Model '{modelId}' Source required Pass '{expected[i]}' is missing.");
            }
        }

        static void RequirePortRequirements(
            IReadOnlyList<SimulationPipelinePortRequirement> actual,
            IReadOnlyList<SimulationPipelinePortRequirement> expected,
            string modelId)
        {
            if (actual.Count != expected.Count)
                throw new InvalidOperationException($"Network Model '{modelId}' Source required port count does not match its Definition.");
            for (int i = 0; i < actual.Count; i++)
            {
                if (!Matches(actual[i], expected[i]))
                    throw new InvalidOperationException($"Network Model '{modelId}' Source required port '{expected[i].PortId}' is missing.");
            }
        }

        static bool Matches(SimulationPortDescriptor actual, SimulationPipelinePortRequirement expected)
        {
            return string.Equals(actual.PortId, expected.PortId, StringComparison.Ordinal) &&
                   string.Equals(actual.SchemaId, expected.SchemaId, StringComparison.Ordinal) &&
                   actual.SchemaVersion == expected.SchemaVersion &&
                   actual.Direction == expected.Direction;
        }

        static bool Matches(
            SimulationPipelinePortRequirement actual,
            SimulationPipelinePortRequirement expected)
        {
            return actual.Role == expected.Role &&
                   string.Equals(actual.PortId, expected.PortId, StringComparison.Ordinal) &&
                   string.Equals(actual.SchemaId, expected.SchemaId, StringComparison.Ordinal) &&
                   actual.SchemaVersion == expected.SchemaVersion &&
                   actual.Direction == expected.Direction;
        }

        void ThrowIfDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(GameplayNetworkModelSourcePreparationValidation));
        }
    }
}
