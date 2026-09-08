using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics
{
    public sealed class CharacterPortValueDiagnostics
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
        readonly Dictionary<(int Operation, string Port, ProgramValuePortDirection Direction), List<Projection>> m_Ports = new();

        public CharacterPortValueDiagnostics(RuntimeDiagnosticsContext context, Guid executionId, IReadOnlyList<ProgramSourceMapEntry> sources)
        {
            m_Context = context;
            m_ExecutionId = executionId;
            foreach (ProgramSourceMapEntry source in sources)
            {
                if (source.TargetKind != ProgramSourceTargetKind.OperationPort || source.ValuePortDirection == ProgramValuePortDirection.None)
                    continue;
                var key = (source.TargetIndex, source.CompiledPortId, source.ValuePortDirection);
                if (!m_Ports.TryGetValue(key, out List<Projection> projections))
                    m_Ports.Add(key, projections = new List<Projection>());
                if (!context.SourceMap.TryGetHandle(RuntimeSourceElementKey.Port(source.GraphId, source.NodeId, source.PortId), out RuntimeSourceElementHandle handle))
                    throw new InvalidOperationException("编译值端口缺少对应的诊断来源。");
                projections.Add(new Projection(handle, source.SourceInvocationPath));
            }
        }

        public void Publish(int operation, string port, ProgramValuePortDirection direction, DebugValueSnapshot value,
            string skillId, ulong actionInstanceId, ulong skillGeneration, ulong nodeGeneration)
        {
            if (!m_Ports.TryGetValue((operation, port, direction), out List<Projection> projections))
                return;
            foreach (Projection projection in projections)
            {
                RuntimeInstanceKey instance = actionInstanceId != 0
                    ? RuntimeInstanceKey.SkillExecution(m_Context.CharacterRuntimeId, m_ExecutionId, skillId,
                        actionInstanceId, projection.Invocation, skillGeneration)
                    : RuntimeInstanceKey.Runnable(m_Context.CharacterRuntimeId, m_ExecutionId, operation.ToString(), nodeGeneration);
                m_Context.Publish(RuntimeTraceChannel.Values, RuntimeTraceDomain.Logic, RuntimeTraceEventKind.ValueSampled,
                    projection.Source, instance, new RuntimeTracePayload
                    {
                        Name = direction.ToString(),
                        Status = "Sampled",
                        Flag = true,
                        SkillId = skillId,
                        ActionInstanceId = actionInstanceId,
                        CallSiteId = projection.Invocation,
                        ActivationGeneration = nodeGeneration,
                        SkillExecutionGeneration = skillGeneration,
                        Value = value
                    });
            }
        }

        public void PublishLimit(string detail)
        {
            m_Context.Publish(RuntimeTraceChannel.Values, RuntimeTraceDomain.Logic, RuntimeTraceEventKind.ValueSamplingLimited,
                RuntimeSourceElementHandle.Invalid, RuntimeInstanceKey.Character(m_Context.CharacterRuntimeId),
                new RuntimeTracePayload { Name = "端口采样上限", Status = "Incomplete", Detail = detail,
                    Value = DebugValueSnapshot.Capture(SimulationValueTraceLimits.MaxSamplesPerEvaluation) });
        }
    }
}
