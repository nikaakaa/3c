using System;
using ThirdPersonSimulation;
using Unity.Profiling;
using UnityEngine.Profiling;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public sealed class UnitySimulationPerformanceSink : ISimulationPerformanceSink
    {
        static readonly ProfilerMarker[] s_Markers = CreateMarkers();
        public static readonly UnitySimulationPerformanceSink Instance = new UnitySimulationPerformanceSink();

        UnitySimulationPerformanceSink()
        {
        }

        public bool IsEnabled => Profiler.enabled;

        public void Begin(SimulationPerformancePhase phase)
        {
            RequireMarker(phase).Begin();
        }

        public void End(SimulationPerformancePhase phase)
        {
            RequireMarker(phase).End();
        }

        static ProfilerMarker RequireMarker(SimulationPerformancePhase phase)
        {
            int index = RequireIndex(phase);
            return s_Markers[index];
        }

        static int RequireIndex(SimulationPerformancePhase phase)
        {
            int index = (int)phase;
            if (index <= 0 || index >= s_Markers.Length)
                throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
            return index;
        }

        static ProfilerMarker[] CreateMarkers()
        {
            var markers = new ProfilerMarker[31];
            markers[(int)SimulationPerformancePhase.PipelineTransaction] = new ProfilerMarker("ThirdPerson.Simulation.Pipeline.Transaction");
            markers[(int)SimulationPerformancePhase.PipelineCheckpointCapture] = new ProfilerMarker("ThirdPerson.Simulation.Pipeline.CheckpointCapture");
            markers[(int)SimulationPerformancePhase.PipelineIngress] = new ProfilerMarker("ThirdPerson.Simulation.Pipeline.Ingress");
            markers[(int)SimulationPerformancePhase.PipelineSchedule] = new ProfilerMarker("ThirdPerson.Simulation.Pipeline.Schedule");
            markers[(int)SimulationPerformancePhase.PipelineRestore] = new ProfilerMarker("ThirdPerson.Simulation.Pipeline.Restore");
            markers[(int)SimulationPerformancePhase.PipelineEvaluate] = new ProfilerMarker("ThirdPerson.Simulation.Pipeline.Evaluate");
            markers[(int)SimulationPerformancePhase.PipelineWorldResolve] = new ProfilerMarker("ThirdPerson.Simulation.Pipeline.WorldResolve");
            markers[(int)SimulationPerformancePhase.PipelineFinalize] = new ProfilerMarker("ThirdPerson.Simulation.Pipeline.Finalize");
            markers[(int)SimulationPerformancePhase.PipelineEgress] = new ProfilerMarker("ThirdPerson.Simulation.Pipeline.Egress");
            markers[(int)SimulationPerformancePhase.PipelineCommitFreeze] = new ProfilerMarker("ThirdPerson.Simulation.Pipeline.CommitFreeze");
            markers[(int)SimulationPerformancePhase.PipelineStatePublish] = new ProfilerMarker("ThirdPerson.Simulation.Pipeline.StatePublish");
            markers[(int)SimulationPerformancePhase.PipelineExternalCommit] = new ProfilerMarker("ThirdPerson.Simulation.Pipeline.ExternalCommit");
            markers[(int)SimulationPerformancePhase.KernelEvaluate] = new ProfilerMarker("ThirdPerson.Simulation.Kernel.Evaluate");
            markers[(int)SimulationPerformancePhase.KernelProgramValidation] = new ProfilerMarker("ThirdPerson.Simulation.Kernel.ProgramValidation");
            markers[(int)SimulationPerformancePhase.KernelWorkspace] = new ProfilerMarker("ThirdPerson.Simulation.Kernel.Workspace");
            markers[(int)SimulationPerformancePhase.OperationFrameBegin] = new ProfilerMarker("ThirdPerson.Simulation.Operation.FrameBegin");
            markers[(int)SimulationPerformancePhase.OperationSetup] = new ProfilerMarker("ThirdPerson.Simulation.Operation.Setup");
            markers[(int)SimulationPerformancePhase.OperationIngress] = new ProfilerMarker("ThirdPerson.Simulation.Operation.Ingress");
            markers[(int)SimulationPerformancePhase.GameplayEffectAdvance] = new ProfilerMarker("ThirdPerson.Simulation.Operation.GameplayEffectAdvance");
            markers[(int)SimulationPerformancePhase.InputRequestApply] = new ProfilerMarker("ThirdPerson.Simulation.Operation.InputRequestApply");
            markers[(int)SimulationPerformancePhase.TimelineDecision] = new ProfilerMarker("ThirdPerson.Simulation.Operation.TimelineDecision");
            markers[(int)SimulationPerformancePhase.ControlTick] = new ProfilerMarker("ThirdPerson.Simulation.Operation.ControlTick");
            markers[(int)SimulationPerformancePhase.MotionResolve] = new ProfilerMarker("ThirdPerson.Simulation.Operation.MotionResolve");
            markers[(int)SimulationPerformancePhase.BlackboardFinalize] = new ProfilerMarker("ThirdPerson.Simulation.Operation.BlackboardFinalize");
            markers[(int)SimulationPerformancePhase.EvaluationFreeze] = new ProfilerMarker("ThirdPerson.Simulation.Operation.FrameComplete");
            markers[(int)SimulationPerformancePhase.KernelFinalize] = new ProfilerMarker("ThirdPerson.Simulation.Kernel.Finalize");
            markers[(int)SimulationPerformancePhase.KernelStateCommit] = new ProfilerMarker("ThirdPerson.Simulation.Kernel.StateCommit");
            markers[(int)SimulationPerformancePhase.KernelResultFreeze] = new ProfilerMarker("ThirdPerson.Simulation.Kernel.ResultFreeze");
            markers[(int)SimulationPerformancePhase.PipelineStepOther] = new ProfilerMarker("ThirdPerson.Simulation.Pipeline.StepOther");
            markers[(int)SimulationPerformancePhase.KernelPendingLease] = new ProfilerMarker("ThirdPerson.Simulation.Kernel.PendingLease");
            return markers;
        }
    }
}
