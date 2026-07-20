using System;

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
