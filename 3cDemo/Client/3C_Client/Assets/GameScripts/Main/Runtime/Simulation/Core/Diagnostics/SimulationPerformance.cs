using System;
using System.Collections.Generic;
using ThirdPersonPerformance;

namespace ThirdPersonSimulation
{
    public enum SimulationPerformancePhase : byte
    {
        PipelineTransaction = 1,
        PipelineCheckpointCapture = 2,
        PipelineIngress = 3,
        PipelineSchedule = 4,
        PipelineRestore = 5,
        PipelineEvaluate = 6,
        PipelineWorldResolve = 7,
        PipelineFinalize = 8,
        PipelineEgress = 9,
        PipelineCommitFreeze = 10,
        PipelineStatePublish = 11,
        PipelineExternalCommit = 12,
        KernelEvaluate = 13,
        KernelProgramValidation = 14,
        KernelWorkspace = 15,
        OperationFrameBegin = 16,
        OperationSetup = 17,
        OperationIngress = 18,
        GameplayEffectAdvance = 19,
        InputRequestApply = 20,
        TimelineDecision = 21,
        ControlTick = 22,
        MotionResolve = 23,
        BlackboardFinalize = 24,
        EvaluationFreeze = 25,
        KernelFinalize = 26,
        KernelStateCommit = 27,
        KernelResultFreeze = 28,
        PipelineStepOther = 29,
        KernelPendingLease = 30
    }

    public static class SimulationPerformanceMetrics
    {
        static readonly PerformanceMetricDefinition[] s_ByPhase = CreateByPhase();
        static readonly PerformanceMetricDefinition[] s_All = CreateAll();

        public static IReadOnlyList<PerformanceMetricDefinition> All => s_All;

        public static PerformanceMetricDefinition Require(SimulationPerformancePhase phase)
        {
            int index = (int)phase;
            if (index <= 0 || index >= s_ByPhase.Length || s_ByPhase[index] == null)
                throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
            return s_ByPhase[index];
        }

        static PerformanceMetricDefinition[] CreateByPhase()
        {
            var values = new PerformanceMetricDefinition[31];
            values[(int)SimulationPerformancePhase.PipelineTransaction] = Metric("simulation.pipeline.transaction", "ThirdPerson.Simulation.Pipeline.Transaction", string.Empty, PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.PipelineCheckpointCapture] = Metric("simulation.pipeline.checkpoint-capture", "ThirdPerson.Simulation.Pipeline.CheckpointCapture", "simulation.pipeline.transaction", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.PipelineIngress] = Metric("simulation.pipeline.ingress", "ThirdPerson.Simulation.Pipeline.Ingress", "simulation.pipeline.transaction", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.PipelineSchedule] = Metric("simulation.pipeline.schedule", "ThirdPerson.Simulation.Pipeline.Schedule", "simulation.pipeline.transaction", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.PipelineRestore] = Metric("simulation.pipeline.restore", "ThirdPerson.Simulation.Pipeline.Restore", "simulation.pipeline.transaction", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.PipelineEvaluate] = Metric("simulation.pipeline.evaluate", "ThirdPerson.Simulation.Pipeline.Evaluate", "simulation.pipeline.transaction", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.PipelineWorldResolve] = Metric("simulation.pipeline.world-resolve", "ThirdPerson.Simulation.Pipeline.WorldResolve", "simulation.pipeline.transaction", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.PipelineFinalize] = Metric("simulation.pipeline.finalize", "ThirdPerson.Simulation.Pipeline.Finalize", "simulation.pipeline.transaction", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.PipelineEgress] = Metric("simulation.pipeline.egress", "ThirdPerson.Simulation.Pipeline.Egress", "simulation.pipeline.transaction", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.PipelineCommitFreeze] = Metric("simulation.pipeline.commit-freeze", "ThirdPerson.Simulation.Pipeline.CommitFreeze", "simulation.pipeline.transaction", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.PipelineStatePublish] = Metric("simulation.pipeline.state-publish", "ThirdPerson.Simulation.Pipeline.StatePublish", "simulation.pipeline.transaction", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.PipelineExternalCommit] = Metric("simulation.pipeline.external-commit", "ThirdPerson.Simulation.Pipeline.ExternalCommit", "simulation.pipeline.transaction", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.KernelEvaluate] = Metric("simulation.kernel.evaluate", "ThirdPerson.Simulation.Kernel.Evaluate", "simulation.pipeline.evaluate", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.KernelProgramValidation] = Metric("simulation.kernel.program-validation", "ThirdPerson.Simulation.Kernel.ProgramValidation", "simulation.kernel.evaluate", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.KernelWorkspace] = Metric("simulation.kernel.workspace", "ThirdPerson.Simulation.Kernel.Workspace", "simulation.kernel.evaluate", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.OperationFrameBegin] = Metric("simulation.operation.frame-begin", "ThirdPerson.Simulation.Operation.FrameBegin", "simulation.kernel.evaluate", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.OperationSetup] = Metric("simulation.operation.setup", "ThirdPerson.Simulation.Operation.Setup", "simulation.kernel.evaluate", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.OperationIngress] = Metric("simulation.operation.ingress", "ThirdPerson.Simulation.Operation.Ingress", "simulation.kernel.evaluate", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.GameplayEffectAdvance] = Metric("simulation.operation.gameplay-effect-advance", "ThirdPerson.Simulation.Operation.GameplayEffectAdvance", "simulation.kernel.evaluate", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.InputRequestApply] = Metric("simulation.operation.input-request-apply", "ThirdPerson.Simulation.Operation.InputRequestApply", "simulation.kernel.evaluate", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.TimelineDecision] = Metric("simulation.operation.timeline-decision", "ThirdPerson.Simulation.Operation.TimelineDecision", "simulation.kernel.evaluate", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.ControlTick] = Metric("simulation.operation.control-tick", "ThirdPerson.Simulation.Operation.ControlTick", "simulation.kernel.evaluate", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.MotionResolve] = Metric("simulation.operation.motion-resolve", "ThirdPerson.Simulation.Operation.MotionResolve", "simulation.kernel.evaluate", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.BlackboardFinalize] = Metric("simulation.operation.blackboard-finalize", "ThirdPerson.Simulation.Operation.BlackboardFinalize", "simulation.kernel.evaluate", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.EvaluationFreeze] = Metric("simulation.operation.frame-complete", "ThirdPerson.Simulation.Operation.FrameComplete", "simulation.kernel.evaluate", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.KernelFinalize] = Metric("simulation.kernel.finalize", "ThirdPerson.Simulation.Kernel.Finalize", "simulation.pipeline.finalize", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.KernelStateCommit] = Metric("simulation.kernel.state-commit", "ThirdPerson.Simulation.Kernel.StateCommit", "simulation.kernel.finalize", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.KernelResultFreeze] = Metric("simulation.kernel.result-freeze", "ThirdPerson.Simulation.Kernel.ResultFreeze", "simulation.kernel.finalize", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.PipelineStepOther] = Metric("simulation.pipeline.step-other", "ThirdPerson.Simulation.Pipeline.StepOther", "simulation.pipeline.transaction", PerformanceSampleScope.LogicTick);
            values[(int)SimulationPerformancePhase.KernelPendingLease] = Metric("simulation.kernel.pending-lease", "ThirdPerson.Simulation.Kernel.PendingLease", "simulation.kernel.evaluate", PerformanceSampleScope.LogicTick);
            return values;
        }

        static PerformanceMetricDefinition[] CreateAll()
        {
            var values = new List<PerformanceMetricDefinition>(s_ByPhase.Length - 1);
            for (int i = 1; i < s_ByPhase.Length; i++)
            {
                if (s_ByPhase[i] != null)
                    values.Add(s_ByPhase[i]);
            }
            return values.ToArray();
        }

        static PerformanceMetricDefinition Metric(
            string id,
            string name,
            string parent,
            PerformanceSampleScope scope) =>
            new PerformanceMetricDefinition(
                id,
                name,
                PerformanceMetricDomain.Simulation,
                parent,
                scope,
                PerformanceMetricUnit.Nanoseconds,
                PerformanceMetricAggregation.InclusiveDuration);
    }

    public interface ISimulationPerformanceSink
    {
        bool IsEnabled { get; }
        void Begin(SimulationPerformancePhase phase);
        void End(SimulationPerformancePhase phase);
    }

    public sealed class NullSimulationPerformanceSink : ISimulationPerformanceSink
    {
        public static readonly NullSimulationPerformanceSink Instance = new NullSimulationPerformanceSink();

        NullSimulationPerformanceSink()
        {
        }

        public bool IsEnabled => false;
        public void Begin(SimulationPerformancePhase phase) { }
        public void End(SimulationPerformancePhase phase) { }
    }

    public readonly struct SimulationPerformanceScope : IDisposable
    {
        readonly ISimulationPerformanceSink m_Sink;
        readonly SimulationPerformancePhase m_Phase;
        readonly bool m_Active;

        public SimulationPerformanceScope(ISimulationPerformanceSink sink, SimulationPerformancePhase phase)
        {
            m_Sink = sink ?? NullSimulationPerformanceSink.Instance;
            m_Phase = phase;
            m_Active = m_Sink.IsEnabled;
            if (m_Active)
                m_Sink.Begin(phase);
        }

        public void Dispose()
        {
            if (m_Active)
                m_Sink.End(m_Phase);
        }
    }

    public static class SimulationPerformanceSinkExtensions
    {
        public static SimulationPerformanceScope Measure(
            this ISimulationPerformanceSink sink,
            SimulationPerformancePhase phase)
        {
            return new SimulationPerformanceScope(sink, phase);
        }
    }
}
