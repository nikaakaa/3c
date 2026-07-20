using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation
{
    public interface ISimulationPipelineReadPortSet
    {
    }

    public interface ISimulationPipelineWritePortSet
    {
    }

    public interface IReadOnlySimulationPipelineProductPort<T>
    {
        SimulationPipelineProductContract Contract { get; }
        bool HasValue { get; }
        T Read();
    }

    public interface IExclusiveSimulationPipelineProductWriter<T>
    {
        SimulationPipelineProductContract Contract { get; }
        void Write(T value);
    }

    public interface IAppendOnlySimulationPipelineProductWriter<T>
    {
        SimulationPipelineProductContract Contract { get; }
        void Append(SimulationPipelineAppendEntryIdentity identity, T value);
    }

    public readonly struct SimulationPipelineAppendProductEntry<T>
    {
        public SimulationPipelineAppendProductEntry(SimulationPipelineAppendEntryIdentity identity, T value)
        {
            Identity = identity;
            Value = value;
        }

        public SimulationPipelineAppendEntryIdentity Identity { get; }
        public T Value { get; }
    }

    public interface IReadOnlySimulationPipelineAppendPort<T>
    {
        SimulationPipelineProductContract Contract { get; }
        int Count { get; }
        SimulationPipelineAppendProductEntry<T> Get(int index);
    }

    public readonly struct SimulationPipelineIngressContext
    {
        public SimulationPipelineIngressContext(
            SimulationSessionCompositionIdentity session,
            SimulationPipelineIdentity pipeline,
            SimulationTickSourceIdentity source,
            ulong currentCompletedTick)
        {
            if (!session.IsValid || !pipeline.IsValid || string.IsNullOrEmpty(source.ClockId) || source.SourceTick == 0)
                throw new ArgumentException("Ingress context identity is incomplete.");
            Session = session;
            Pipeline = pipeline;
            Source = source;
            CurrentCompletedTick = currentCompletedTick;
        }

        public SimulationSessionCompositionIdentity Session { get; }
        public SimulationPipelineIdentity Pipeline { get; }
        public SimulationTickSourceIdentity Source { get; }
        public ulong CurrentCompletedTick { get; }
    }

    public readonly struct SimulationPipelineScheduleContext
    {
        public SimulationPipelineScheduleContext(
            SimulationSessionCompositionIdentity session,
            SimulationPipelineIdentity pipeline,
            SimulationTickSourceIdentity source,
            ulong currentCompletedTick)
        {
            if (!session.IsValid || !pipeline.IsValid || string.IsNullOrEmpty(source.ClockId) || source.SourceTick == 0)
                throw new ArgumentException("Schedule context identity is incomplete.");
            Session = session;
            Pipeline = pipeline;
            Source = source;
            CurrentCompletedTick = currentCompletedTick;
        }

        public SimulationSessionCompositionIdentity Session { get; }
        public SimulationPipelineIdentity Pipeline { get; }
        public SimulationTickSourceIdentity Source { get; }
        public ulong CurrentCompletedTick { get; }
    }

    public readonly struct SimulationPipelineStepTransactionContext
    {
        public SimulationPipelineStepTransactionContext(
            SimulationSessionCompositionIdentity session,
            SimulationPipelineIdentity pipeline,
            SimulationTick tick,
            SimulationPipelineStepExecutionKind executionKind,
            int stepIndex,
            int stepCount,
            StableHash transactionIdentity,
            ISimulationPerformanceSink performance)
        {
            if (!session.IsValid || !pipeline.IsValid || !tick.IsValid ||
                !Enum.IsDefined(typeof(SimulationPipelineStepExecutionKind), executionKind) ||
                stepIndex < 0 || stepCount <= 0 || stepIndex >= stepCount || !transactionIdentity.IsValid)
            {
                throw new ArgumentException("Step transaction context is incomplete.");
            }
            Session = session;
            Pipeline = pipeline;
            Tick = tick;
            ExecutionKind = executionKind;
            StepIndex = stepIndex;
            StepCount = stepCount;
            TransactionIdentity = transactionIdentity;
            Performance = performance ?? NullSimulationPerformanceSink.Instance;
        }

        public SimulationSessionCompositionIdentity Session { get; }
        public SimulationPipelineIdentity Pipeline { get; }
        public SimulationTick Tick { get; }
        public SimulationPipelineStepExecutionKind ExecutionKind { get; }
        public int StepIndex { get; }
        public int StepCount { get; }
        public StableHash TransactionIdentity { get; }
        public ISimulationPerformanceSink Performance { get; }
    }

    public readonly struct SimulationPipelineEgressContext
    {
        public SimulationPipelineEgressContext(
            SimulationSessionCompositionIdentity session,
            SimulationPipelineIdentity pipeline,
            SimulationTickSourceIdentity source,
            int completedStepCount,
            StableHash transactionIdentity)
        {
            if (!session.IsValid || !pipeline.IsValid || string.IsNullOrEmpty(source.ClockId) || source.SourceTick == 0 ||
                completedStepCount < 0 || !transactionIdentity.IsValid)
            {
                throw new ArgumentException("Egress context identity is incomplete.");
            }
            Session = session;
            Pipeline = pipeline;
            Source = source;
            CompletedStepCount = completedStepCount;
            TransactionIdentity = transactionIdentity;
        }

        public SimulationSessionCompositionIdentity Session { get; }
        public SimulationPipelineIdentity Pipeline { get; }
        public SimulationTickSourceIdentity Source { get; }
        public int CompletedStepCount { get; }
        public StableHash TransactionIdentity { get; }
    }

    public interface ISimulationIngressPassRuntime<TReadPorts, TWritePorts> : ISimulationPipelinePassRuntime
        where TReadPorts : ISimulationPipelineReadPortSet
        where TWritePorts : ISimulationPipelineWritePortSet
    {
        void Execute(SimulationPipelineIngressContext context, TReadPorts readPorts, TWritePorts writePorts);
    }

    public interface ISimulationSchedulePassRuntime<TReadPorts, TWritePorts> : ISimulationPipelinePassRuntime
        where TReadPorts : ISimulationPipelineReadPortSet
        where TWritePorts : ISimulationPipelineWritePortSet
    {
        void Execute(SimulationPipelineScheduleContext context, TReadPorts readPorts, TWritePorts writePorts);
    }

    public interface ISimulationExecutionPlanSchedulePassRuntime<TReadPorts, TWritePorts> :
        ISimulationSchedulePassRuntime<TReadPorts, TWritePorts>
        where TReadPorts : ISimulationPipelineReadPortSet
        where TWritePorts : ISimulationPipelineWritePortSet
    {
    }

    public interface ISimulationStepPassRuntime<TReadPorts, TWritePorts> : ISimulationPipelinePassRuntime
        where TReadPorts : ISimulationPipelineReadPortSet
        where TWritePorts : ISimulationPipelineWritePortSet
    {
        void Execute(SimulationPipelineStepTransactionContext context, TReadPorts readPorts, TWritePorts writePorts);
    }

    public interface ISimulationEgressPassRuntime<TReadPorts, TWritePorts> : ISimulationPipelinePassRuntime
        where TReadPorts : ISimulationPipelineReadPortSet
        where TWritePorts : ISimulationPipelineWritePortSet
    {
        void Execute(SimulationPipelineEgressContext context, TReadPorts readPorts, TWritePorts writePorts);
    }

    public enum SimulationPipelinePassRuntimeState : byte
    {
        Created = 1,
        Active = 2,
        Disposed = 3
    }

    public interface ISimulationPipelinePassRuntime : IDisposable
    {
        SimulationPipelinePassDescriptor Descriptor { get; }
        SimulationPipelinePassRuntimeState State { get; }
        void Activate();
    }

    public sealed class SimulationPipelinePassStateSnapshot
    {
        readonly byte[] m_Payload;

        public SimulationPipelinePassStateSnapshot(
            SimulationPipelinePassId passId,
            SimulationPipelinePassImplementationVersion implementationVersion,
            string stateOwner,
            string stateSchemaId,
            int stateSchemaVersion,
            StableHash stateHash,
            byte[] payload)
        {
            if (!passId.IsValid || !implementationVersion.IsValid || stateSchemaVersion <= 0 || !stateHash.IsValid || payload == null)
                throw new ArgumentException("Pass state snapshot identity is incomplete.");
            PassId = passId;
            ImplementationVersion = implementationVersion;
            StateOwner = SimulationIdentity.Require(stateOwner, nameof(stateOwner));
            StateSchemaId = SimulationIdentity.Require(stateSchemaId, nameof(stateSchemaId));
            StateSchemaVersion = stateSchemaVersion;
            StateHash = stateHash;
            m_Payload = (byte[])payload.Clone();
            StableHash computed = SimulationCanonicalPayloadHash.Compute(m_Payload);
            if (!computed.Equals(stateHash))
                throw new ArgumentException("Pass state payload hash does not match its canonical bytes.", nameof(stateHash));
        }

        public SimulationPipelinePassId PassId { get; }
        public SimulationPipelinePassImplementationVersion ImplementationVersion { get; }
        public string StateOwner { get; }
        public string StateSchemaId { get; }
        public int StateSchemaVersion { get; }
        public StableHash StateHash { get; }
        public byte[] CopyPayload() => (byte[])m_Payload.Clone();
    }

    public readonly struct SimulationPipelineReconstructionContext
    {
        public SimulationPipelineReconstructionContext(
            SimulationSessionCompositionIdentity session,
            SimulationPipelineIdentity pipeline,
            ProgramCatalogHash programCatalogHash,
            StableHash rosterHash,
            WorldRevision worldRevision)
        {
            if (!session.IsValid || !pipeline.IsValid || !programCatalogHash.IsValid || !rosterHash.IsValid || string.IsNullOrEmpty(worldRevision.Value))
                throw new ArgumentException("Pass reconstruction context is incomplete.");
            Session = session;
            Pipeline = pipeline;
            ProgramCatalogHash = programCatalogHash;
            RosterHash = rosterHash;
            WorldRevision = worldRevision;
        }

        public SimulationSessionCompositionIdentity Session { get; }
        public SimulationPipelineIdentity Pipeline { get; }
        public ProgramCatalogHash ProgramCatalogHash { get; }
        public StableHash RosterHash { get; }
        public WorldRevision WorldRevision { get; }
    }

    public interface ISimulationPipelineReconstructiblePass
    {
        void Reconstruct(SimulationPipelineReconstructionContext context);
    }

    public sealed class SimulationPipelinePassLifecycleController
    {
        public SimulationPipelinePassRuntimeState State { get; private set; } = SimulationPipelinePassRuntimeState.Created;

        public void Activate()
        {
            Require(SimulationPipelinePassRuntimeState.Created);
            State = SimulationPipelinePassRuntimeState.Active;
        }

        public void RequireExecution()
        {
            Require(SimulationPipelinePassRuntimeState.Active);
        }

        public void RequireCaptureOrRestore()
        {
            Require(SimulationPipelinePassRuntimeState.Active);
        }

        public void MarkDisposed()
        {
            State = SimulationPipelinePassRuntimeState.Disposed;
        }

        void Require(SimulationPipelinePassRuntimeState expected)
        {
            if (State != expected)
                throw new InvalidOperationException($"Pipeline Pass lifecycle is '{State}', expected '{expected}'.");
        }
    }

    public sealed class SimulationPipelinePassBindingDescriptor
    {
        readonly ReadOnlyCollection<SimulationPortDescriptor> m_SourcePorts;
        readonly ReadOnlyCollection<SimulationPortDescriptor> m_TargetPorts;
        readonly ReadOnlyCollection<SimulationPortDescriptor> m_SolverPorts;
        readonly ReadOnlyCollection<SimulationPortDescriptor> m_DiagnosticsPorts;

        public SimulationPipelinePassBindingDescriptor(
            SimulationPipelinePassDescriptor pass,
            IEnumerable<SimulationPortDescriptor> sourcePorts,
            IEnumerable<SimulationPortDescriptor> targetPorts,
            IEnumerable<SimulationPortDescriptor> solverPorts,
            IEnumerable<SimulationPortDescriptor> diagnosticsPorts)
        {
            Pass = pass ?? throw new ArgumentNullException(nameof(pass));
            m_SourcePorts = Freeze(sourcePorts, nameof(sourcePorts));
            m_TargetPorts = Freeze(targetPorts, nameof(targetPorts));
            m_SolverPorts = Freeze(solverPorts, nameof(solverPorts));
            m_DiagnosticsPorts = Freeze(diagnosticsPorts, nameof(diagnosticsPorts));
            RequireExactPorts(pass, SimulationPipelineBindingPortRole.Source, m_SourcePorts, nameof(sourcePorts));
            RequireExactPorts(pass, SimulationPipelineBindingPortRole.Target, m_TargetPorts, nameof(targetPorts));
            RequireExactPorts(pass, SimulationPipelineBindingPortRole.Solver, m_SolverPorts, nameof(solverPorts));
            RequireExactPorts(pass, SimulationPipelineBindingPortRole.Diagnostics, m_DiagnosticsPorts, nameof(diagnosticsPorts));
        }

        public SimulationPipelinePassDescriptor Pass { get; }
        public IReadOnlyList<SimulationPortDescriptor> SourcePorts => m_SourcePorts;
        public IReadOnlyList<SimulationPortDescriptor> TargetPorts => m_TargetPorts;
        public IReadOnlyList<SimulationPortDescriptor> SolverPorts => m_SolverPorts;
        public IReadOnlyList<SimulationPortDescriptor> DiagnosticsPorts => m_DiagnosticsPorts;

        static ReadOnlyCollection<SimulationPortDescriptor> Freeze(IEnumerable<SimulationPortDescriptor> source, string parameter)
        {
            var values = source == null ? new List<SimulationPortDescriptor>() : new List<SimulationPortDescriptor>(source);
            values.Sort((left, right) => string.CompareOrdinal(left.PortId, right.PortId));
            for (int i = 1; i < values.Count; i++)
            {
                if (string.Equals(values[i - 1].PortId, values[i].PortId, StringComparison.Ordinal))
                    throw new ArgumentException("Pass binding contains duplicate port identity.", parameter);
            }
            return values.AsReadOnly();
        }

        static void RequireExactPorts(
            SimulationPipelinePassDescriptor pass,
            SimulationPipelineBindingPortRole role,
            IReadOnlyList<SimulationPortDescriptor> actual,
            string parameter)
        {
            IReadOnlyList<SimulationPipelinePortRequirement> required = pass.GetPortRequirements(role);
            if (required.Count != actual.Count)
                throw new ArgumentException($"Pass binding {role} port count does not match its declaration.", parameter);
            for (int i = 0; i < required.Count; i++)
            {
                SimulationPipelinePortRequirement requirement = required[i];
                SimulationPortDescriptor port = actual[i];
                if (!string.Equals(port.PortId, requirement.PortId, StringComparison.Ordinal) ||
                    !string.Equals(port.SchemaId, requirement.SchemaId, StringComparison.Ordinal) ||
                    port.SchemaVersion != requirement.SchemaVersion || port.Direction != requirement.Direction)
                {
                    throw new ArgumentException($"Pass binding {role} port '{port.PortId}' does not match its declaration.", parameter);
                }
            }
        }
    }

    public readonly struct SimulationPipelinePassFactoryIdentity
    {
        public SimulationPipelinePassFactoryIdentity(
            SimulationPipelinePassId passId,
            SimulationPipelinePassImplementationVersion implementationVersion,
            string factoryVersion,
            StableHash bindingSchemaHash)
        {
            if (!passId.IsValid || !implementationVersion.IsValid || !bindingSchemaHash.IsValid)
                throw new ArgumentException("Pass factory identity is incomplete.");
            PassId = passId;
            ImplementationVersion = implementationVersion;
            FactoryVersion = SimulationIdentity.Require(factoryVersion, nameof(factoryVersion));
            BindingSchemaHash = bindingSchemaHash;
        }

        public SimulationPipelinePassId PassId { get; }
        public SimulationPipelinePassImplementationVersion ImplementationVersion { get; }
        public string FactoryVersion { get; }
        public StableHash BindingSchemaHash { get; }
    }

}
