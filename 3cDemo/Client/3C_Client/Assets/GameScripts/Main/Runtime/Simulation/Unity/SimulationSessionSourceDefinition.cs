using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public sealed class SimulationSessionSourceAuthoringDescriptor
    {
        readonly ReadOnlyCollection<SimulationPortDescriptor> m_SourcePorts;

        public SimulationSessionSourceAuthoringDescriptor(
            SimulationSessionSourceDescriptor source,
            IEnumerable<SimulationPortDescriptor> sourcePorts)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            var values = sourcePorts == null
                ? new List<SimulationPortDescriptor>()
                : new List<SimulationPortDescriptor>(sourcePorts);
            values.Sort((left, right) => string.CompareOrdinal(left.PortId, right.PortId));
            for (int i = 0; i < values.Count; i++)
            {
                if (!string.Equals(values[i].OwnerComponentId, source.Identity.ComponentId, StringComparison.Ordinal) ||
                    i > 0 && string.Equals(values[i - 1].PortId, values[i].PortId, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Session Source authoring ports contain an invalid owner or duplicate identity.", nameof(sourcePorts));
                }
            }
            m_SourcePorts = values.AsReadOnly();
        }

        public SimulationSessionSourceDescriptor Source { get; }
        public IReadOnlyList<SimulationPortDescriptor> SourcePorts => m_SourcePorts;
    }

    public sealed class SimulationSessionSourcePreparationContext
    {
        internal SimulationSessionSourcePreparationContext(
            SimulationSessionId sessionId,
            SimulationSourceClockId sourceClockId,
            int tickRate,
            SimulationProgramRuntimeDescriptor programRuntime,
            SimulationExecutionBackendDefinition executionBackend,
            SimulationWorldSolverDefinitionDescriptor worldSolver,
            SimulationWorldIdentityDescriptor worldIdentity,
            IReadOnlyList<ISimulationActorRegistration> registrations)
        {
            if (!sessionId.IsValid || !sourceClockId.IsValid || tickRate <= 0)
                throw new ArgumentException("Session Source preparation identity is incomplete.");
            SessionId = sessionId;
            SourceClockId = sourceClockId;
            TickRate = tickRate;
            ProgramRuntime = programRuntime ?? throw new ArgumentNullException(nameof(programRuntime));
            ExecutionBackend = executionBackend ? executionBackend : throw new ArgumentNullException(nameof(executionBackend));
            WorldSolver = worldSolver ?? throw new ArgumentNullException(nameof(worldSolver));
            WorldIdentity = worldIdentity ?? throw new ArgumentNullException(nameof(worldIdentity));
            if (!WorldIdentity.Solver.Identity.Equals(WorldSolver.Identity))
                throw new ArgumentException("Session Source World identity does not match its World Solver descriptor.");
            Registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
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

    public interface ISimulationSessionPreparedSource : IDisposable
    {
        SimulationSessionSourceDescriptor Descriptor { get; }
        SimulationRuntimePortSet RuntimePorts { get; }
    }

    public interface IFloat32SimulationSessionPreparedSource : ISimulationSessionPreparedSource
    {
        IFloat32SimulationSessionRuntimeLauncher RuntimeLauncher { get; }
        IFloat32SimulationRestoreSource RestoreSource { get; }
        IFloat32SourceEgressOutputPort SourceEgress { get; }
    }

    public interface ISimulationSessionSourcePreparation : IDisposable
    {
        SimulationSessionPreparationStatus Status { get; }
        SimulationSessionFailure Failure { get; }
        SimulationSessionSourceDescriptor Descriptor { get; }
        SimulationSessionPreparationStatus Step(SimulationSessionLogicTickContext context);
        ISimulationSessionPreparedSource TakePreparedSource();
    }

    public abstract class SimulationSessionSourceDefinition : ScriptableObject
    {
        public abstract SimulationSessionSourceAuthoringDescriptor BuildAuthoringDescriptor();

        internal ISimulationSessionSourcePreparation CreatePreparation(
            SimulationSessionSourcePreparationContext context) =>
            CreatePreparationCore(context) ?? throw new InvalidOperationException(
                $"Session Source Definition '{name}' returned no preparation.");

        protected abstract ISimulationSessionSourcePreparation CreatePreparationCore(
            SimulationSessionSourcePreparationContext context);
    }
}
