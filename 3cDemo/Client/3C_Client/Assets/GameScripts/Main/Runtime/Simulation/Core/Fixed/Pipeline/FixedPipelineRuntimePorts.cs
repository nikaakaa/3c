using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation.Fixed
{
    public static class FixedPipelineRuntimePortIds
    {
        public const string ProgramRuntime = "simulation.target.fixed-program-runtime";
        public const string ProgramRuntimeSchema = "fixed-program-runtime-services";
        public const string WorkingState = "simulation.target.fixed-working-state";
        public const string WorkingStateSchema = "fixed-working-state-read";
        public const string CompletedSteps = "simulation.target.fixed-completed-steps";
        public const string CompletedStepsSchema = "fixed-completed-step-read";
        public const string CommittedObservation = "simulation.target.fixed-committed-actor-observation";
        public const string CommittedObservationSchema = "committed-actor-pose-observation";
        public const string WorldSolver = "simulation.solver.fixed-world";
        public const string WorldSolverSchema = "fixed-world-solver";
        public const string Diagnostics = "simulation.diagnostics.session";
        public const string DiagnosticsSchema = "simulation-diagnostics-sink";
    }

    public interface IFixedProgramRuntimePort : ISimulationRuntimePort
    {
        SimulationProgramCatalog Catalog { get; }
        SimulationKernel Kernel { get; }
        IReadOnlyList<SimulationActorBinding> Roster { get; }
        SimulationActorRosterDescriptor RosterDescriptor { get; }
        int GetActorIndex(ActorId actorId);
        CharacterSimulationProgram GetProgram(int actorIndex);
        ProgramExecutionLayout GetExecutionLayout(int actorIndex);
        KernelProgramBinding GetKernelBinding(int actorIndex);
    }

    public sealed class FixedProgramRuntimePort : IFixedProgramRuntimePort
    {
        readonly ReadOnlyCollection<SimulationActorBinding> m_Roster;
        readonly ReadOnlyCollection<CharacterSimulationProgram> m_Programs;
        readonly ReadOnlyCollection<ProgramExecutionLayout> m_Layouts;
        readonly ReadOnlyCollection<KernelProgramBinding> m_Bindings;
        readonly SimulationActorRosterDescriptor m_RosterDescriptor;
        readonly Dictionary<ActorId, int> m_ActorIndices = new Dictionary<ActorId, int>();

        public FixedProgramRuntimePort(
            SimulationComponentIdentity programRuntime,
            FixedProgramRuntime runtime)
        {
            if (!programRuntime.IsValid || programRuntime.Role != SimulationComponentRole.ProgramRuntime)
                throw new ArgumentException("Program Runtime identity is invalid.", nameof(programRuntime));
            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));
            Catalog = runtime.Catalog;
            Kernel = runtime.Kernel;
            var bindings = new List<SimulationActorBinding>(runtime.Roster);
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i] == null)
                    throw new ArgumentException("Program Runtime roster contains a missing binding.", nameof(runtime));
            }
            bindings.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            if (bindings.Count == 0)
                throw new ArgumentException("Program Runtime roster cannot be empty.", nameof(runtime));
            var programs = new CharacterSimulationProgram[bindings.Count];
            var layouts = new ProgramExecutionLayout[bindings.Count];
            var kernelBindings = new KernelProgramBinding[bindings.Count];
            var actorIds = new ActorId[bindings.Count];
            var sharedLayouts = new Dictionary<ProgramId, ProgramExecutionLayout>();
            for (int i = 0; i < bindings.Count; i++)
            {
                if (i > 0 && bindings[i - 1].ActorId.Equals(bindings[i].ActorId))
                    throw new ArgumentException("Program Runtime roster contains duplicate ActorId.", nameof(runtime));
                CharacterSimulationProgram program = Catalog.GetRequired(bindings[i].ProgramId);
                if (!program.ProgramHash.Equals(bindings[i].ProgramHash) || !program.LayoutHash.Equals(bindings[i].LayoutHash))
                    throw new ArgumentException($"Actor '{bindings[i].ActorId}' Program binding is stale.", nameof(runtime));
                KernelProgramBinding kernelBinding = runtime.GetBinding(program.Manifest.ProgramId);
                ProgramExecutionLayout layout = kernelBinding.Layout;
                kernelBinding.Require(program, layout, Kernel.Specialization);
                if (sharedLayouts.TryGetValue(program.Manifest.ProgramId, out ProgramExecutionLayout shared) &&
                    !ReferenceEquals(shared.Services, layout.Services))
                {
                    throw new InvalidOperationException($"Program '{program.Manifest.ProgramId}' created more than one execution services instance.");
                }
                sharedLayouts[program.Manifest.ProgramId] = layout;
                actorIds[i] = bindings[i].ActorId;
                m_ActorIndices.Add(bindings[i].ActorId, i);
                programs[i] = program;
                layouts[i] = layout;
                kernelBindings[i] = kernelBinding;
            }
            m_Roster = bindings.AsReadOnly();
            m_Programs = Array.AsReadOnly(programs);
            m_Layouts = Array.AsReadOnly(layouts);
            m_Bindings = Array.AsReadOnly(kernelBindings);
            m_RosterDescriptor = new SimulationActorRosterDescriptor(actorIds);
            Descriptor = FixedPipelineRuntimePortDescriptor.Create(
                FixedPipelineRuntimePortIds.ProgramRuntime,
                FixedPipelineRuntimePortIds.ProgramRuntimeSchema,
                programRuntime.ComponentId,
                StableHash.Compute(programRuntime.ToString(), Catalog.CatalogHash.ToString()),
                SimulationPortDirection.Input);
        }

        public SimulationPortDescriptor Descriptor { get; }
        public SimulationProgramCatalog Catalog { get; }
        public SimulationKernel Kernel { get; }
        public IReadOnlyList<SimulationActorBinding> Roster => m_Roster;
        public SimulationActorRosterDescriptor RosterDescriptor => m_RosterDescriptor;
        public int GetActorIndex(ActorId actorId)
        {
            if (!m_ActorIndices.TryGetValue(actorId, out int index))
                throw new InvalidOperationException($"Actor '{actorId}' is not part of the locked Program Runtime roster.");
            return index;
        }
        public CharacterSimulationProgram GetProgram(int actorIndex) => m_Programs[actorIndex];
        public ProgramExecutionLayout GetExecutionLayout(int actorIndex) => m_Layouts[actorIndex];
        public KernelProgramBinding GetKernelBinding(int actorIndex) => m_Bindings[actorIndex];
    }

    public interface IFixedWorkingStateReadPort : ISimulationRuntimePort
    {
        SimulationWorldStateSet Current { get; }
        FixedSimulationStep Step { get; }
    }

    public sealed class FixedWorkingStatePort : IFixedWorkingStateReadPort
    {
        public FixedWorkingStatePort(SimulationComponentIdentity backend)
        {
            if (!backend.IsValid || backend.Role != SimulationComponentRole.ExecutionBackend)
                throw new ArgumentException("Execution Backend identity is invalid.", nameof(backend));
            Descriptor = FixedPipelineRuntimePortDescriptor.Create(
                FixedPipelineRuntimePortIds.WorkingState,
                FixedPipelineRuntimePortIds.WorkingStateSchema,
                backend.ComponentId,
                StableHash.Compute(backend.ToString(), "working-state/1"),
                SimulationPortDirection.Input);
        }

        public SimulationPortDescriptor Descriptor { get; }
        public SimulationWorldStateSet Current { get; private set; }
        public FixedSimulationStep Step { get; private set; }

        internal void Set(SimulationWorldStateSet current, FixedSimulationStep step)
        {
            Current = current ?? throw new ArgumentNullException(nameof(current));
            Step = step ?? throw new ArgumentNullException(nameof(step));
            if (current.Actors.Count != step.Actors.Count)
                throw new InvalidOperationException("Working state and Step rosters do not match.");
            for (int i = 0; i < current.Actors.Count; i++)
            {
                if (!current.Actors[i].ActorId.Equals(step.Actors[i]))
                    throw new InvalidOperationException("Working state and Step Actor order do not match.");
            }
        }

        internal void Clear()
        {
            Current = null;
            Step = null;
        }
    }

    public interface IFixedCompletedStepReadPort : ISimulationRuntimePort
    {
        IReadOnlyList<FixedCompletedSimulationStep> Steps { get; }
    }

    public sealed class FixedCompletedStepPort : IFixedCompletedStepReadPort
    {
        IReadOnlyList<FixedCompletedSimulationStep> m_Steps = Array.Empty<FixedCompletedSimulationStep>();

        public FixedCompletedStepPort(SimulationComponentIdentity backend)
        {
            if (!backend.IsValid || backend.Role != SimulationComponentRole.ExecutionBackend)
                throw new ArgumentException("Execution Backend identity is invalid.", nameof(backend));
            Descriptor = FixedPipelineRuntimePortDescriptor.Create(
                FixedPipelineRuntimePortIds.CompletedSteps,
                FixedPipelineRuntimePortIds.CompletedStepsSchema,
                backend.ComponentId,
                StableHash.Compute(backend.ToString(), "completed-steps/1"),
                SimulationPortDirection.Input);
        }

        public SimulationPortDescriptor Descriptor { get; }
        public IReadOnlyList<FixedCompletedSimulationStep> Steps => m_Steps;

        internal void Set(IReadOnlyList<FixedCompletedSimulationStep> steps)
        {
            m_Steps = steps ?? throw new ArgumentNullException(nameof(steps));
        }

        internal void Clear() => m_Steps = Array.Empty<FixedCompletedSimulationStep>();
    }

    public interface IFixedWorldSolverRuntimePort : ISimulationRuntimePort
    {
        ICharacterWorldSolver Solver { get; }
    }

    public sealed class FixedWorldSolverRuntimePort : IFixedWorldSolverRuntimePort
    {
        public FixedWorldSolverRuntimePort(
            SimulationComponentIdentity worldSolver,
            ICharacterWorldSolver solver)
        {
            if (!worldSolver.IsValid || worldSolver.Role != SimulationComponentRole.WorldSolver)
                throw new ArgumentException("World Solver component identity is invalid.", nameof(worldSolver));
            Solver = solver ?? throw new ArgumentNullException(nameof(solver));
            Descriptor = FixedPipelineRuntimePortDescriptor.Create(
                FixedPipelineRuntimePortIds.WorldSolver,
                FixedPipelineRuntimePortIds.WorldSolverSchema,
                worldSolver.ComponentId,
                StableHash.Compute(worldSolver.ToString(), solver.Descriptor.ImplementationId.Value, solver.Descriptor.Version),
                SimulationPortDirection.Input);
        }

        public SimulationPortDescriptor Descriptor { get; }
        public ICharacterWorldSolver Solver { get; }
    }

    public interface IFixedDiagnosticsRuntimePort : ISimulationRuntimePort
    {
        ISimulationDiagnosticsSink Sink { get; }
    }

    public sealed class FixedDiagnosticsRuntimePort : IFixedDiagnosticsRuntimePort
    {
        public FixedDiagnosticsRuntimePort(
            SimulationComponentIdentity diagnostics,
            ISimulationDiagnosticsSink sink)
        {
            if (!diagnostics.IsValid || diagnostics.Role != SimulationComponentRole.Diagnostics)
                throw new ArgumentException("Diagnostics component identity is invalid.", nameof(diagnostics));
            Sink = sink ?? throw new ArgumentNullException(nameof(sink));
            Descriptor = FixedPipelineRuntimePortDescriptor.Create(
                FixedPipelineRuntimePortIds.Diagnostics,
                FixedPipelineRuntimePortIds.DiagnosticsSchema,
                diagnostics.ComponentId,
                diagnostics.ConfigurationHash,
                SimulationPortDirection.Input);
        }

        public SimulationPortDescriptor Descriptor { get; }
        public ISimulationDiagnosticsSink Sink { get; }
    }

    static class FixedPipelineRuntimePortDescriptor
    {
        public static SimulationPortDescriptor Create(
            string portId,
            string schemaId,
            string owner,
            StableHash configurationHash,
            SimulationPortDirection direction)
        {
            return new SimulationPortDescriptor(portId, schemaId, 1, direction, owner, configurationHash);
        }
    }
}

