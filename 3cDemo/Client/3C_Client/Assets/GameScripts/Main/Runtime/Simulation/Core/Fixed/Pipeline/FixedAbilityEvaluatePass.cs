using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedAbilityEvaluatePassRuntimeFactory : IFixedPipelinePassRuntimeFactory
    {
        static readonly SimulationPipelinePassFactoryDescriptor s_Descriptor =
            StandardFixedPipelinePassContracts.CreateFactoryDescriptor(
                StandardFixedPipelinePassContracts.AbilityEvaluate);

        public SimulationPipelinePassFactoryDescriptor Descriptor => s_Descriptor;

        public IFixedCompiledPipelinePassRuntime Create(FixedPipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new FixedAbilityEvaluateReadPorts(
                context.BindTargetPort<IFixedCharacterRuntimePort>(FixedPipelineRuntimePortIds.CharacterRuntime),
                context.BindTargetPort<IFixedWorkingStateReadPort>(FixedPipelineRuntimePortIds.WorkingState),
                context.BindDiagnosticsPort<IFixedDiagnosticsRuntimePort>(FixedPipelineRuntimePortIds.Diagnostics));
            var writes = new FixedAbilityEvaluateWritePorts(
                context.Products.BindExclusiveWriter<FixedCharacterEvaluationResultBatch>(SimulationPipelineProducts.CharacterEvaluationResults),
                context.Products.BindExclusiveWriter<WorldSolveBatchRequest>(SimulationPipelineProducts.WorldSolveBatchRequest));
            return new FixedStepPassRuntimeAdapter<FixedAbilityEvaluateReadPorts, FixedAbilityEvaluateWritePorts>(
                new FixedAbilityEvaluatePassRuntime(context.Pass.Descriptor, reads.CharacterRuntime.Roster.Count),
                reads,
                writes);
        }
    }

    public sealed class FixedAbilityEvaluatePassRuntime :
        FixedPipelinePassRuntimeBase,
        ISimulationStepPassRuntime<FixedAbilityEvaluateReadPorts, FixedAbilityEvaluateWritePorts>
    {
        readonly FixedCharacterEvaluationResult[] m_Evaluations;
        readonly CharacterWorldSolveRequest[] m_Requests;
        readonly List<SimulationIngress>[] m_Ingress;
        readonly FixedCharacterEvaluationResultBatch m_EvaluationBatch;

        public FixedAbilityEvaluatePassRuntime(
            SimulationPipelinePassDescriptor descriptor,
            int actorCount)
            : base(descriptor)
        {
            if (actorCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(actorCount));
            m_Evaluations = new FixedCharacterEvaluationResult[actorCount];
            m_Requests = new CharacterWorldSolveRequest[actorCount];
            m_Ingress = new List<SimulationIngress>[actorCount];
            m_EvaluationBatch = new FixedCharacterEvaluationResultBatch(actorCount);
            for (int i = 0; i < actorCount; i++)
                m_Ingress[i] = new List<SimulationIngress>();
        }

        public void Execute(
            SimulationPipelineStepTransactionContext context,
            FixedAbilityEvaluateReadPorts readPorts,
            FixedAbilityEvaluateWritePorts writePorts)
        {
            RequireExecution();
            SimulationWorldStateSet state = readPorts.WorkingState.Current ??
                throw new InvalidOperationException("Ability Evaluate Pass has no working state.");
            FixedSimulationStep step = readPorts.WorkingState.Step ??
                throw new InvalidOperationException("Ability Evaluate Pass has no current Step.");
            if (step.Tick != context.Tick || state.Actors.Count != readPorts.CharacterRuntime.Roster.Count ||
                state.Actors.Count != m_Evaluations.Length)
                throw new InvalidOperationException("Ability Evaluate Pass Step does not match the working roster.");

            PrepareIngress(step, readPorts.CharacterRuntime);
            try
            {
                for (int i = 0; i < m_Evaluations.Length; i++)
                {
                    SimulationActorBinding actor = readPorts.CharacterRuntime.Roster[i];
                    if (!state.Actors[i].ActorId.Equals(actor.ActorId) ||
                        !state.WorldState.Bodies[i].ActorId.Equals(actor.ActorId) ||
                        !step.Inputs[i].ActorId.Equals(actor.ActorId))
                        throw new InvalidOperationException("Ability Evaluate Pass Actor order does not match the locked roster.");
                    CharacterSimulationInput input = step.Inputs[i].Value.Input;
                    m_Evaluations[i] = FixedCharacterEvaluationRuntime.Evaluate(
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
                    m_Requests[i] = m_Evaluations[i].WorldRequest;
                }
                writePorts.CharacterEvaluationResults.Write(m_EvaluationBatch.Reset(step.Tick, m_Evaluations));
                writePorts.WorldBatch.Write(new WorldSolveBatchRequest(step.Tick, state.WorldState, m_Requests));
            }
            catch
            {
                for (int i = 0; i < m_Evaluations.Length; i++)
                    m_Evaluations[i]?.AbortUnconsumed();
                throw;
            }
            finally
            {
                for (int i = 0; i < m_Evaluations.Length; i++)
                {
                    m_Evaluations[i] = null;
                    m_Requests[i] = null;
                    m_Ingress[i].Clear();
                }
            }
        }

        void PrepareIngress(FixedSimulationStep step, IFixedCharacterRuntimePort runtime)
        {
            for (int i = 0; i < m_Ingress.Length; i++)
                m_Ingress[i].Clear();
            for (int i = 0; i < step.Ingress.Count; i++)
                m_Ingress[runtime.GetActorIndex(step.Ingress[i].ActorId)].Add(step.Ingress[i].Value);
        }
    }

    public sealed class FixedAbilityEvaluateReadPorts : ISimulationPipelineReadPortSet
    {
        public FixedAbilityEvaluateReadPorts(
            IFixedCharacterRuntimePort characterRuntime,
            IFixedWorkingStateReadPort workingState,
            IFixedDiagnosticsRuntimePort diagnostics)
        {
            CharacterRuntime = characterRuntime ?? throw new ArgumentNullException(nameof(characterRuntime));
            WorkingState = workingState ?? throw new ArgumentNullException(nameof(workingState));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public IFixedCharacterRuntimePort CharacterRuntime { get; }
        public IFixedWorkingStateReadPort WorkingState { get; }
        public IFixedDiagnosticsRuntimePort Diagnostics { get; }
    }

    public sealed class FixedAbilityEvaluateWritePorts : ISimulationPipelineWritePortSet
    {
        public FixedAbilityEvaluateWritePorts(
            IExclusiveSimulationPipelineProductWriter<FixedCharacterEvaluationResultBatch> characterEvaluationResults,
            IExclusiveSimulationPipelineProductWriter<WorldSolveBatchRequest> worldBatch)
        {
            CharacterEvaluationResults = characterEvaluationResults ?? throw new ArgumentNullException(nameof(characterEvaluationResults));
            WorldBatch = worldBatch ?? throw new ArgumentNullException(nameof(worldBatch));
        }

        public IExclusiveSimulationPipelineProductWriter<FixedCharacterEvaluationResultBatch> CharacterEvaluationResults { get; }
        public IExclusiveSimulationPipelineProductWriter<WorldSolveBatchRequest> WorldBatch { get; }
    }
}
