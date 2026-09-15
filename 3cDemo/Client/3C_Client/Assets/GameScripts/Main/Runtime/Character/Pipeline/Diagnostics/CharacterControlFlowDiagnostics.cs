using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics
{
    public sealed class CharacterControlFlowDiagnostics
    {
        readonly struct Projection
        {
            internal Projection(RuntimeSourceElementHandle source, string invocation)
            {
                Source = source;
                Invocation = invocation;
            }
            internal RuntimeSourceElementHandle Source { get; }
            internal string Invocation { get; }
        }

        readonly RuntimeDiagnosticsContext m_Context;
        readonly Guid m_ExecutionId;
        readonly Dictionary<string, Projection> m_Edges = new(StringComparer.Ordinal);

        public CharacterControlFlowDiagnostics(RuntimeDiagnosticsContext context, Guid executionId,
            IReadOnlyList<ProgramSourceMapEntry> sources, IReadOnlyList<ProgramControlFlowEdge> edges)
        {
            m_Context = context;
            m_ExecutionId = executionId;
            foreach (ProgramSourceMapEntry source in sources)
            {
                if (source.TargetKind != ProgramSourceTargetKind.Reference || string.IsNullOrEmpty(source.EdgeId))
                    continue;
                if (!context.SourceMap.TryGetIndexedTarget(new RuntimeSourceTarget(RuntimeSourceTargetKind.Reference, source.TargetIndex), out RuntimeSourceElementHandle handle))
                    throw new InvalidOperationException("编译控制边缺少诊断来源。");
                if (handle.Kind == RuntimeSourceElementKind.Edge)
                    m_Edges.Add(edges[source.TargetIndex].Identity, new Projection(handle, source.SourceInvocationPath));
            }
        }

        public void Publish(ProgramControlFlowEdge edge, bool selected, bool passed, string skillId,
            ulong actionInstanceId, ulong skillGeneration, ulong nodeGeneration, ulong invocationGeneration, ulong parentGeneration)
        {
            if (!m_Edges.TryGetValue(edge.Identity, out Projection projection))
                return;
            if (actionInstanceId != 0 && string.IsNullOrEmpty(projection.Invocation))
                throw new InvalidOperationException("技能边缺少编译调用路径。");
            RuntimeInstanceKey instance = actionInstanceId != 0
                ? RuntimeInstanceKey.SkillExecution(m_Context.CharacterRuntimeId, m_ExecutionId, skillId,
                    actionInstanceId, projection.Invocation, skillGeneration, invocationGeneration)
                : RuntimeInstanceKey.Runnable(m_Context.CharacterRuntimeId, m_ExecutionId, edge.Source.Value.ToString(), nodeGeneration);
            bool transition = edge.Kind == ProgramControlFlowKind.Transition;
            RuntimeTraceEventKind kind = selected
                ? transition ? RuntimeTraceEventKind.StateTransitionSelected : RuntimeTraceEventKind.EdgeSelected
                : transition ? RuntimeTraceEventKind.StateTransitionEvaluated : RuntimeTraceEventKind.EdgeEvaluated;
            m_Context.Publish(transition ? RuntimeTraceChannel.StateMachine : RuntimeTraceChannel.Graph,
                RuntimeTraceDomain.Logic, kind, projection.Source, instance, new RuntimeTracePayload
                {
                    Name = edge.Kind.ToString(),
                    Status = selected ? "Selected" : passed ? "Passed" : "Rejected",
                    Flag = passed,
                    Value = DebugValueSnapshot.Capture(passed),
                    SkillId = skillId,
                    ActionInstanceId = actionInstanceId,
                    CallSiteId = projection.Invocation,
                    ActivationGeneration = nodeGeneration,
                    SkillExecutionGeneration = skillGeneration,
                    GraphInvocationGeneration = invocationGeneration,
                    ParentInvocationGeneration = parentGeneration
                });
        }
    }
}
