using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedProgramEvaluatePassRuntimeFactory : IFixedPipelinePassRuntimeFactory
    {
        static readonly SimulationPipelinePassFactoryDescriptor s_Descriptor =
            StandardFixedPipelinePassContracts.CreateFactoryDescriptor(
                StandardFixedPipelinePassContracts.ProgramEvaluate);

        public SimulationPipelinePassFactoryDescriptor Descriptor => s_Descriptor;

        public IFixedCompiledPipelinePassRuntime Create(FixedPipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new FixedProgramEvaluateReadPorts(
                context.BindTargetPort<IFixedProgramRuntimePort>(FixedPipelineRuntimePortIds.ProgramRuntime),
                context.BindTargetPort<IFixedWorkingStateReadPort>(FixedPipelineRuntimePortIds.WorkingState),
                context.BindDiagnosticsPort<IFixedDiagnosticsRuntimePort>(FixedPipelineRuntimePortIds.Diagnostics));
            var writes = new FixedProgramEvaluateWritePorts(
                context.Products.BindExclusiveWriter<FixedPendingEvaluationBatch>(SimulationPipelineProducts.PendingActorEvaluations),
                context.Products.BindExclusiveWriter<WorldSolveBatchRequest>(SimulationPipelineProducts.WorldSolveBatchRequest));
            return new FixedStepPassRuntimeAdapter<FixedProgramEvaluateReadPorts, FixedProgramEvaluateWritePorts>(
                new FixedProgramEvaluatePassRuntime(
                    context.Pass.Descriptor,
                    reads.ProgramRuntime.Roster.Count),
                reads,
                writes);
        }
    }

    public sealed class FixedProgramEvaluatePassRuntime :
        FixedPipelinePassRuntimeBase,
        ISimulationStepPassRuntime<FixedProgramEvaluateReadPorts, FixedProgramEvaluateWritePorts>
    {
        readonly PendingCharacterEvaluation[] m_Pending;
        readonly CharacterWorldSolveRequest[] m_Requests;
        readonly List<SimulationIngress>[] m_Ingress;
        readonly FixedPendingEvaluationBatch m_PendingBatch;

        public FixedProgramEvaluatePassRuntime(
            SimulationPipelinePassDescriptor descriptor,
            int actorCount)
            : base(descriptor)
        {
            if (actorCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(actorCount));
            m_Pending = new PendingCharacterEvaluation[actorCount];
            m_Requests = new CharacterWorldSolveRequest[actorCount];
            m_Ingress = new List<SimulationIngress>[actorCount];
            m_PendingBatch = new FixedPendingEvaluationBatch(actorCount);
            for (int i = 0; i < actorCount; i++)
                m_Ingress[i] = new List<SimulationIngress>();
        }

        public void Execute(
            SimulationPipelineStepTransactionContext context,
            FixedProgramEvaluateReadPorts readPorts,
            FixedProgramEvaluateWritePorts writePorts)
        {
            RequireExecution();
            SimulationWorldStateSet state = readPorts.WorkingState.Current ??
                throw new InvalidOperationException("Program Evaluate Pass has no working state.");
            FixedSimulationStep step = readPorts.WorkingState.Step ??
                throw new InvalidOperationException("Program Evaluate Pass has no current Step.");
            if (step.Tick != context.Tick || state.Actors.Count != readPorts.ProgramRuntime.Roster.Count)
                throw new InvalidOperationException("Program Evaluate Pass Step does not match the working roster.");
            if (state.Actors.Count != m_Pending.Length)
                throw new InvalidOperationException("Program Evaluate Pass workspace does not match the locked roster.");

            PrepareIngress(step, readPorts.ProgramRuntime);
            try
            {
                for (int i = 0; i < m_Pending.Length; i++)
                {
                    SimulationActorBinding actor = readPorts.ProgramRuntime.Roster[i];
                    if (!state.Actors[i].ActorId.Equals(actor.ActorId) ||
                        !state.WorldState.Bodies[i].ActorId.Equals(actor.ActorId) ||
                        !step.Inputs[i].ActorId.Equals(actor.ActorId))
                    {
                        throw new InvalidOperationException("Program Evaluate Pass Actor order does not match the locked roster.");
                    }
                    CharacterSimulationProgram program = readPorts.ProgramRuntime.GetProgram(i);
                    PendingCharacterEvaluation evaluation = readPorts.ProgramRuntime.Kernel.Evaluate(
                        new SimulationEvaluateRequest(
                            readPorts.ProgramRuntime.GetKernelBinding(i),
                            actor.ActorId,
                            step.Tick,
                            step.Inputs[i].Value.Input,
                            m_Ingress[i],
                            state.Actors[i].State,
                            state.WorldState.Bodies[i],
                            readPorts.Diagnostics.Sink.IsEnabled,
                            context.Performance));
                    m_Pending[i] = evaluation;
                    m_Requests[i] = evaluation.WorldRequest;
                }
                writePorts.PendingEvaluations.Write(m_PendingBatch.Reset(step.Tick, m_Pending));
                writePorts.WorldBatch.Write(new WorldSolveBatchRequest(step.Tick, state.WorldState, m_Requests));
            }
            catch
            {
                for (int i = 0; i < m_Pending.Length; i++)
                    m_Pending[i]?.AbortUnconsumed();
                throw;
            }
            finally
            {
                for (int i = 0; i < m_Pending.Length; i++)
                {
                    m_Pending[i] = null;
                    m_Requests[i] = null;
                    m_Ingress[i].Clear();
                }
            }
        }

        void PrepareIngress(FixedSimulationStep step, IFixedProgramRuntimePort runtime)
        {
            for (int i = 0; i < m_Ingress.Length; i++)
                m_Ingress[i].Clear();
            for (int i = 0; i < step.Ingress.Count; i++)
                m_Ingress[runtime.GetActorIndex(step.Ingress[i].ActorId)].Add(step.Ingress[i].Value);
        }
    }

    public sealed class FixedProgramEvaluateReadPorts : ISimulationPipelineReadPortSet
    {
        public FixedProgramEvaluateReadPorts(
            IFixedProgramRuntimePort programRuntime,
            IFixedWorkingStateReadPort workingState,
            IFixedDiagnosticsRuntimePort diagnostics)
        {
            ProgramRuntime = programRuntime ?? throw new ArgumentNullException(nameof(programRuntime));
            WorkingState = workingState ?? throw new ArgumentNullException(nameof(workingState));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public IFixedProgramRuntimePort ProgramRuntime { get; }
        public IFixedWorkingStateReadPort WorkingState { get; }
        public IFixedDiagnosticsRuntimePort Diagnostics { get; }
    }

    public sealed class FixedProgramEvaluateWritePorts : ISimulationPipelineWritePortSet
    {
        public FixedProgramEvaluateWritePorts(
            IExclusiveSimulationPipelineProductWriter<FixedPendingEvaluationBatch> pendingEvaluations,
            IExclusiveSimulationPipelineProductWriter<WorldSolveBatchRequest> worldBatch)
        {
            PendingEvaluations = pendingEvaluations ?? throw new ArgumentNullException(nameof(pendingEvaluations));
            WorldBatch = worldBatch ?? throw new ArgumentNullException(nameof(worldBatch));
        }

        public IExclusiveSimulationPipelineProductWriter<FixedPendingEvaluationBatch> PendingEvaluations { get; }
        public IExclusiveSimulationPipelineProductWriter<WorldSolveBatchRequest> WorldBatch { get; }
    }

    static class FixedPipelineDiagnostics
    {
        public static void PublishOperations(
            ISimulationDiagnosticsSink sink,
            IReadOnlyList<SimulationTraceRecord> records,
            int start)
        {
            if (sink == null || !sink.IsEnabled || records == null)
                return;
            for (int i = Math.Max(0, start); i < records.Count; i++)
                sink.PublishOperation(records[i]);
        }
    }
}

