using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public sealed class Float32AbilityEvaluatePassRuntimeFactory : IFloat32PipelinePassRuntimeFactory
    {
        static readonly SimulationPipelinePassFactoryDescriptor s_Descriptor =
            StandardFloat32PipelinePassContracts.CreateFactoryDescriptor(
                StandardFloat32PipelinePassContracts.AbilityEvaluate);

        public SimulationPipelinePassFactoryDescriptor Descriptor => s_Descriptor;

        public IFloat32CompiledPipelinePassRuntime Create(Float32PipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new Float32AbilityEvaluateReadPorts(
                context.BindTargetPort<IFloat32CharacterRuntimePort>(Float32PipelineRuntimePortIds.CharacterRuntime),
                context.BindTargetPort<IFloat32WorkingStateReadPort>(Float32PipelineRuntimePortIds.WorkingState),
                context.BindDiagnosticsPort<IFloat32DiagnosticsRuntimePort>(Float32PipelineRuntimePortIds.Diagnostics));
            var writes = new Float32AbilityEvaluateWritePorts(
                context.Products.BindExclusiveWriter<Float32PendingEvaluationBatch>(SimulationPipelineProducts.PendingActorEvaluations),
                context.Products.BindExclusiveWriter<WorldSolveBatchRequest>(SimulationPipelineProducts.WorldSolveBatchRequest));
            return new Float32StepPassRuntimeAdapter<Float32AbilityEvaluateReadPorts, Float32AbilityEvaluateWritePorts>(
                new Float32AbilityEvaluatePassRuntime(context.Pass.Descriptor, reads.CharacterRuntime.Roster.Count),
                reads,
                writes);
        }
    }

    public sealed class Float32AbilityEvaluatePassRuntime :
        Float32PipelinePassRuntimeBase,
        ISimulationStepPassRuntime<Float32AbilityEvaluateReadPorts, Float32AbilityEvaluateWritePorts>
    {
        readonly Float32PendingActorEvaluation[] m_Pending;
        readonly CharacterWorldSolveRequest[] m_Requests;
        readonly List<SimulationIngress>[] m_Ingress;
        readonly Float32PendingEvaluationBatch m_PendingBatch;

        public Float32AbilityEvaluatePassRuntime(
            SimulationPipelinePassDescriptor descriptor,
            int actorCount)
            : base(descriptor)
        {
            if (actorCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(actorCount));
            m_Pending = new Float32PendingActorEvaluation[actorCount];
            m_Requests = new CharacterWorldSolveRequest[actorCount];
            m_Ingress = new List<SimulationIngress>[actorCount];
            m_PendingBatch = new Float32PendingEvaluationBatch(actorCount);
            for (int i = 0; i < actorCount; i++)
                m_Ingress[i] = new List<SimulationIngress>();
        }

        public void Execute(
            SimulationPipelineStepTransactionContext context,
            Float32AbilityEvaluateReadPorts readPorts,
            Float32AbilityEvaluateWritePorts writePorts)
        {
            RequireExecution();
            SimulationWorldStateSet state = readPorts.WorkingState.Current ??
                throw new InvalidOperationException("Ability Evaluate Pass has no working state.");
            Float32SimulationStep step = readPorts.WorkingState.Step ??
                throw new InvalidOperationException("Ability Evaluate Pass has no current Step.");
            if (step.Tick != context.Tick || state.Actors.Count != readPorts.CharacterRuntime.Roster.Count ||
                state.Actors.Count != m_Pending.Length)
                throw new InvalidOperationException("Ability Evaluate Pass Step does not match the working roster.");

            PrepareIngress(step, readPorts.CharacterRuntime);
            try
            {
                for (int i = 0; i < m_Pending.Length; i++)
                {
                    SimulationActorBinding actor = readPorts.CharacterRuntime.Roster[i];
                    if (!state.Actors[i].ActorId.Equals(actor.ActorId) ||
                        !state.WorldState.Bodies[i].ActorId.Equals(actor.ActorId) ||
                        !step.Inputs[i].ActorId.Equals(actor.ActorId))
                        throw new InvalidOperationException("Ability Evaluate Pass Actor order does not match the locked roster.");
                    CharacterSimulationInput input = step.Inputs[i].Value.Input;
                    m_Pending[i] = Float32CharacterEvaluationRuntime.Evaluate(
                        readPorts.CharacterRuntime.Runtime,
                        actor,
                        state.Actors[i].State,
                        step.Tick,
                        input,
                        m_Ingress[i],
                        state.WorldState.Bodies[i],
                        readPorts.Diagnostics.Sink.IsEnabled,
                        readPorts.Diagnostics.Sink is ISimulationValueTraceInterest valueInterest &&
                        valueInterest.IsValueCaptureRequested(actor.ActorId),
                        readPorts.Diagnostics.Sink is ISimulationControlTraceInterest controlInterest &&
                        controlInterest.IsControlCaptureRequested(actor.ActorId));
                    m_Requests[i] = m_Pending[i].WorldRequest;
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

        void PrepareIngress(Float32SimulationStep step, IFloat32CharacterRuntimePort runtime)
        {
            for (int i = 0; i < m_Ingress.Length; i++)
                m_Ingress[i].Clear();
            for (int i = 0; i < step.Ingress.Count; i++)
                m_Ingress[runtime.GetActorIndex(step.Ingress[i].ActorId)].Add(step.Ingress[i].Value);
        }
    }

    public sealed class Float32AbilityEvaluateReadPorts : ISimulationPipelineReadPortSet
    {
        public Float32AbilityEvaluateReadPorts(
            IFloat32CharacterRuntimePort characterRuntime,
            IFloat32WorkingStateReadPort workingState,
            IFloat32DiagnosticsRuntimePort diagnostics)
        {
            CharacterRuntime = characterRuntime ?? throw new ArgumentNullException(nameof(characterRuntime));
            WorkingState = workingState ?? throw new ArgumentNullException(nameof(workingState));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public IFloat32CharacterRuntimePort CharacterRuntime { get; }
        public IFloat32WorkingStateReadPort WorkingState { get; }
        public IFloat32DiagnosticsRuntimePort Diagnostics { get; }
    }

    public sealed class Float32AbilityEvaluateWritePorts : ISimulationPipelineWritePortSet
    {
        public Float32AbilityEvaluateWritePorts(
            IExclusiveSimulationPipelineProductWriter<Float32PendingEvaluationBatch> pendingEvaluations,
            IExclusiveSimulationPipelineProductWriter<WorldSolveBatchRequest> worldBatch)
        {
            PendingEvaluations = pendingEvaluations ?? throw new ArgumentNullException(nameof(pendingEvaluations));
            WorldBatch = worldBatch ?? throw new ArgumentNullException(nameof(worldBatch));
        }

        public IExclusiveSimulationPipelineProductWriter<Float32PendingEvaluationBatch> PendingEvaluations { get; }
        public IExclusiveSimulationPipelineProductWriter<WorldSolveBatchRequest> WorldBatch { get; }
    }
}
