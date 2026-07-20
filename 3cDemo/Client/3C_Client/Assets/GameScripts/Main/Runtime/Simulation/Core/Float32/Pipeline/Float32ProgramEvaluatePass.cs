using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public sealed class Float32ProgramEvaluatePassRuntimeFactory : IFloat32PipelinePassRuntimeFactory
    {
        static readonly SimulationPipelinePassFactoryDescriptor s_Descriptor =
            StandardFloat32PipelinePassContracts.CreateFactoryDescriptor(
                StandardFloat32PipelinePassContracts.ProgramEvaluate);

        public SimulationPipelinePassFactoryDescriptor Descriptor => s_Descriptor;

        public IFloat32CompiledPipelinePassRuntime Create(Float32PipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new Float32ProgramEvaluateReadPorts(
                context.BindTargetPort<IFloat32ProgramRuntimePort>(Float32PipelineRuntimePortIds.ProgramRuntime),
                context.BindTargetPort<IFloat32WorkingStateReadPort>(Float32PipelineRuntimePortIds.WorkingState),
                context.BindDiagnosticsPort<IFloat32DiagnosticsRuntimePort>(Float32PipelineRuntimePortIds.Diagnostics));
            var writes = new Float32ProgramEvaluateWritePorts(
                context.Products.BindExclusiveWriter<Float32PendingEvaluationBatch>(SimulationPipelineProducts.PendingActorEvaluations),
                context.Products.BindExclusiveWriter<WorldSolveBatchRequest>(SimulationPipelineProducts.WorldSolveBatchRequest));
            return new Float32StepPassRuntimeAdapter<Float32ProgramEvaluateReadPorts, Float32ProgramEvaluateWritePorts>(
                new Float32ProgramEvaluatePassRuntime(
                    context.Pass.Descriptor,
                    reads.ProgramRuntime.Roster.Count),
                reads,
                writes);
        }
    }

    public sealed class Float32ProgramEvaluatePassRuntime :
        Float32PipelinePassRuntimeBase,
        ISimulationStepPassRuntime<Float32ProgramEvaluateReadPorts, Float32ProgramEvaluateWritePorts>
    {
        readonly PendingCharacterEvaluation[] m_Pending;
        readonly CharacterWorldSolveRequest[] m_Requests;
        readonly List<SimulationIngress>[] m_Ingress;
        readonly Float32PendingEvaluationBatch m_PendingBatch;

        public Float32ProgramEvaluatePassRuntime(
            SimulationPipelinePassDescriptor descriptor,
            int actorCount)
            : base(descriptor)
        {
            if (actorCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(actorCount));
            m_Pending = new PendingCharacterEvaluation[actorCount];
            m_Requests = new CharacterWorldSolveRequest[actorCount];
            m_Ingress = new List<SimulationIngress>[actorCount];
            m_PendingBatch = new Float32PendingEvaluationBatch(actorCount);
            for (int i = 0; i < actorCount; i++)
                m_Ingress[i] = new List<SimulationIngress>();
        }

        public void Execute(
            SimulationPipelineStepTransactionContext context,
            Float32ProgramEvaluateReadPorts readPorts,
            Float32ProgramEvaluateWritePorts writePorts)
        {
            RequireExecution();
            SimulationWorldStateSet state = readPorts.WorkingState.Current ??
                throw new InvalidOperationException("Program Evaluate Pass has no working state.");
            Float32SimulationStep step = readPorts.WorkingState.Step ??
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
                writePorts.WorldBatch.Write(new WorldSolveBatchRequest(
                    step.Tick,
                    state.WorldState,
                    m_Requests,
                    step.ObservedWorldConstraints));
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

        void PrepareIngress(Float32SimulationStep step, IFloat32ProgramRuntimePort runtime)
        {
            for (int i = 0; i < m_Ingress.Length; i++)
                m_Ingress[i].Clear();
            for (int i = 0; i < step.Ingress.Count; i++)
                m_Ingress[runtime.GetActorIndex(step.Ingress[i].ActorId)].Add(step.Ingress[i].Value);
        }
    }

    public sealed class Float32ProgramEvaluateReadPorts : ISimulationPipelineReadPortSet
    {
        public Float32ProgramEvaluateReadPorts(
            IFloat32ProgramRuntimePort programRuntime,
            IFloat32WorkingStateReadPort workingState,
            IFloat32DiagnosticsRuntimePort diagnostics)
        {
            ProgramRuntime = programRuntime ?? throw new ArgumentNullException(nameof(programRuntime));
            WorkingState = workingState ?? throw new ArgumentNullException(nameof(workingState));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public IFloat32ProgramRuntimePort ProgramRuntime { get; }
        public IFloat32WorkingStateReadPort WorkingState { get; }
        public IFloat32DiagnosticsRuntimePort Diagnostics { get; }
    }

    public sealed class Float32ProgramEvaluateWritePorts : ISimulationPipelineWritePortSet
    {
        public Float32ProgramEvaluateWritePorts(
            IExclusiveSimulationPipelineProductWriter<Float32PendingEvaluationBatch> pendingEvaluations,
            IExclusiveSimulationPipelineProductWriter<WorldSolveBatchRequest> worldBatch)
        {
            PendingEvaluations = pendingEvaluations ?? throw new ArgumentNullException(nameof(pendingEvaluations));
            WorldBatch = worldBatch ?? throw new ArgumentNullException(nameof(worldBatch));
        }

        public IExclusiveSimulationPipelineProductWriter<Float32PendingEvaluationBatch> PendingEvaluations { get; }
        public IExclusiveSimulationPipelineProductWriter<WorldSolveBatchRequest> WorldBatch { get; }
    }

    static class Float32PipelineDiagnostics
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
