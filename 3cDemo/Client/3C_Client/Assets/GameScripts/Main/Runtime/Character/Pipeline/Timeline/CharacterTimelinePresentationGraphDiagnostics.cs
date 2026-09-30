using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline.Runtime;
using ThirdPersonSimulation;
using UnityEngine;
using ThirdPersonCharacter.Pipeline.Diagnostics;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal sealed class CharacterTimelinePresentationGraphDiagnostics
    {
        internal sealed class ProgramSources
        {
            internal readonly RuntimeSourceElementKey[] Operations;
            internal readonly Dictionary<string, RuntimeSourceElementKey> Edges = new(StringComparer.Ordinal);
            internal readonly Dictionary<(int Operation, string Port, bool Input), RuntimeSourceElementKey> Ports = new();
            internal readonly BindingSources[] Bindings;
            internal IDebugSourceMap SourceMap { get; }

            internal ProgramSources(Float32GameplayAbilityExecutionData data, Float32PresentationGraphRuntime runtime,
                IDebugSourceMap sourceMap)
            {
                SourceMap = sourceMap;
                Operations = new RuntimeSourceElementKey[data.Operations.Count];
                Bindings = new BindingSources[runtime.EntryCount];
                for (int i = 0; i < data.SourceMap.Count; i++)
                {
                    ProgramSourceMapEntry source = data.SourceMap[i];
                    switch (source.TargetKind)
                    {
                        case ProgramSourceTargetKind.Operation:
                            Operations[source.TargetIndex] = AbilityDebugSourceMapFiller.SourceKey(source);
                            break;
                        case ProgramSourceTargetKind.Reference when !string.IsNullOrEmpty(source.EdgeId):
                            Edges.Add(data.ControlFlow[source.TargetIndex].Identity, AbilityDebugSourceMapFiller.SourceKey(source));
                            break;
                        case ProgramSourceTargetKind.OperationPort when source.ValuePortDirection != ProgramValuePortDirection.None:
                            Ports.Add((source.TargetIndex, source.CompiledPortId,
                                source.ValuePortDirection == ProgramValuePortDirection.Input), AbilityDebugSourceMapFiller.SourceKey(source));
                            break;
                    }
                }
                for (int i = 0; i < Bindings.Length; i++)
                    Bindings[i] = new BindingSources(data, runtime.Entry(i));
            }
        }

        internal sealed class BindingSources
        {
            internal readonly ProgramSourceMapEntry[] Invocations;
            internal readonly int[] Parents;
            internal readonly int[] OperationInvocations;
            internal readonly ulong[] Generations;

            internal BindingSources(Float32GameplayAbilityExecutionData data, ProgramSourceMapEntry root)
            {
                var invocations = new List<ProgramSourceMapEntry> { root };
                var parents = new List<int> { -1 };
                for (int parent = 0; parent < invocations.Count; parent++)
                    for (int i = 0; i < data.SourceMap.Count; i++)
                    {
                        ProgramSourceMapEntry source = data.SourceMap[i];
                        if (source.TargetKind == ProgramSourceTargetKind.GraphInvocation &&
                            source.InvocationCallerKind is ProgramInvocationCallerKind.Node or ProgramInvocationCallerKind.Edge &&
                            source.ParentInvocationPath == invocations[parent].SourceInvocationPath)
                        {
                            invocations.Add(source);
                            parents.Add(parent);
                        }
                    }
                Invocations = invocations.ToArray();
                Parents = parents.ToArray();
                Generations = new ulong[Invocations.Length];
                OperationInvocations = new int[data.Operations.Count];
                Array.Fill(OperationInvocations, -1);
                var bySource = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int i = 0; i < Invocations.Length; i++)
                    bySource.Add(Invocations[i].SourceInvocationPath, i);
                for (int i = 0; i < data.SourceMap.Count; i++)
                {
                    ProgramSourceMapEntry source = data.SourceMap[i];
                    if (source.TargetKind == ProgramSourceTargetKind.Operation &&
                        bySource.TryGetValue(source.GraphInvocationPath, out int invocation))
                        OperationInvocations[source.TargetIndex] = invocation;
                }
            }
        }

        struct PendingTrace
        {
            internal IDebugSourceMap SourceMap;
            internal RuntimeTraceChannel Channel;
            internal RuntimeTraceEventKind Kind;
            internal RuntimeSourceElementKey Source;
            internal RuntimeInstanceKey Instance;
            internal RuntimeTracePayload Payload;
        }

        readonly Guid m_RuntimeId = Guid.NewGuid();
        RuntimeDiagnosticsContext m_Context;
        PendingTrace[] m_Pending = Array.Empty<PendingTrace>();
        int m_Count;
        ulong m_NextInvocationGeneration;
        ProgramSources m_Program;
        BindingSources m_Binding;
        RuntimeTimelinePlaybackProvenance m_Provenance;
        TimelineRuntimePlaybackHandle m_Playback;
        ulong m_PlaybackGeneration;
        string m_SkillId;
        ulong m_ActionInstanceId;
        string m_Caller;
        int m_Cycle;
        float m_Time;
        internal bool CaptureGraph { get; private set; }
        internal bool CaptureValues { get; private set; }

        internal void Attach(RuntimeDiagnosticsContext context)
        {
            m_Context = context;
            m_Pending = new PendingTrace[context.Store.LiveStateCapacity];
        }

        internal void Detach()
        {
            Discard();
            m_Context = null;
            m_Pending = Array.Empty<PendingTrace>();
        }

        internal void Begin(ProgramSources program, int binding, in RuntimeTimelinePlaybackProvenance provenance,
            in TimelineRuntimePresentationFrame frame, string skillId, ulong actionInstanceId, string caller,
            int cycle, long time)
        {
            CaptureGraph = m_Context != null && m_Context.ShouldPublish(RuntimeTraceChannel.Graph, RuntimeTraceEventKind.NodeEntered);
            CaptureValues = m_Context != null && m_Context.ShouldPublish(RuntimeTraceChannel.Values, RuntimeTraceEventKind.ValueSampled);
            if (!CaptureGraph && !CaptureValues)
                return;
            m_Program = program;
            m_Binding = program.Bindings[binding];
            Array.Clear(m_Binding.Generations, 0, m_Binding.Generations.Length);
            m_Provenance = provenance;
            m_Playback = frame.Handle;
            m_PlaybackGeneration = frame.Generation;
            m_SkillId = skillId;
            m_ActionInstanceId = actionInstanceId;
            m_Caller = caller;
            m_Cycle = cycle;
            m_Time = ThirdPersonSimulation.Fixed.FixedScalar.FromRaw(time).ToSingle();
        }

        internal void End()
        {
            CaptureGraph = false;
            CaptureValues = false;
            m_Program = null;
            m_Binding = null;
            m_SkillId = null;
            m_Caller = null;
            m_Provenance = default;
            m_Playback = default;
        }

        internal void TraceOperation(OperationHandle operation, string code, string detail, ulong generation)
        {
            if (code == "operation_enter")
                for (int i = 0; i < m_Binding.Invocations.Length; i++)
                    if (m_Binding.Invocations[i].TargetIndex == operation.Value)
                        m_Binding.Generations[i] = checked(++m_NextInvocationGeneration);
            if (!CaptureGraph)
                return;
            RuntimeTraceEventKind kind = code switch
            {
                "operation_enter" => RuntimeTraceEventKind.NodeEntered,
                "operation_complete" => RuntimeTraceEventKind.NodeCompleted,
                "operation_running" => RuntimeTraceEventKind.NodeRunning,
                "operation_waiting" => RuntimeTraceEventKind.NodeWaiting,
                _ => RuntimeTraceEventKind.None
            };
            if (kind == RuntimeTraceEventKind.None)
                return;
            int invocation = m_Binding.OperationInvocations[operation.Value];
            RuntimeSourceElementKey source = m_Program.Operations[operation.Value];
            if (source.Kind == RuntimeSourceElementKind.Graph)
                kind = kind switch
                {
                    RuntimeTraceEventKind.NodeEntered => RuntimeTraceEventKind.GraphCreated,
                    RuntimeTraceEventKind.NodeCompleted => RuntimeTraceEventKind.GraphDestroyed,
                    _ => kind
                };
            RuntimeTracePayload payload = Payload(invocation);
            payload.ActivationGeneration = generation;
            payload.Detail = detail;
            Append(RuntimeTraceChannel.Graph, kind, source, invocation, payload);
        }

        internal void TraceEdge(ProgramControlFlowEdge edge, bool selected, bool passed)
        {
            if (!m_Program.Edges.TryGetValue(edge.Identity, out RuntimeSourceElementKey source))
                return;
            int invocation = m_Binding.OperationInvocations[edge.Source.Value];
            RuntimeTracePayload payload = Payload(invocation);
            payload.Name = edge.Kind == ProgramControlFlowKind.Value ? "Value" : "Flow";
            payload.Status = selected ? "Selected" : passed ? "Passed" : "Rejected";
            payload.Flag = passed;
            Append(RuntimeTraceChannel.Graph, selected ? RuntimeTraceEventKind.EdgeSelected : RuntimeTraceEventKind.EdgeEvaluated,
                source, invocation, payload);
        }

        internal void TraceValue(OperationHandle operation, string port, AbilityStateValue value, bool input)
        {
            int invocation = m_Binding.OperationInvocations[operation.Value];
            RuntimeSourceElementKey source = string.IsNullOrEmpty(port)
                ? m_Program.Operations[operation.Value] : m_Program.Ports[(operation.Value, port, input)];
            RuntimeTracePayload payload = Payload(invocation);
            payload.Name = port;
            payload.Status = input ? "Input" : "Output";
            payload.Value = CaptureValue(value);
            Append(RuntimeTraceChannel.Values, RuntimeTraceEventKind.ValueSampled, source, invocation, payload);
        }

        RuntimeInstanceKey Instance(int invocation) => new(
            RuntimeInstanceKind.SkillExecution, m_Context.CharacterRuntimeId, m_RuntimeId, m_SkillId,
            m_Provenance.SkillExecutionGeneration, m_Playback.Value, m_Cycle, m_ActionInstanceId,
            m_Binding.Invocations[invocation].GraphInvocationPath, m_Binding.Generations[invocation],
            m_Provenance.SourceOperationIndex);

        RuntimeTracePayload Payload(int invocation) => new()
        {
            SkillId = m_SkillId,
            ActionInstanceId = m_ActionInstanceId,
            CallSiteId = m_Binding.Invocations[invocation].GraphInvocationPath,
            SkillExecutionGeneration = m_Provenance.SkillExecutionGeneration,
            GraphInvocationGeneration = m_Binding.Generations[invocation],
            ParentInvocationGeneration = m_Binding.Parents[invocation] < 0
                ? m_Provenance.SourceActivationGeneration : m_Binding.Generations[m_Binding.Parents[invocation]],
            RelatedElementId = m_Caller,
            Cycle = m_Cycle,
            Time = m_Time,
            Revision = m_PlaybackGeneration,
            TimelinePlayback = m_Provenance
        };

        void Append(RuntimeTraceChannel channel, RuntimeTraceEventKind kind, RuntimeSourceElementKey source,
            int invocation, RuntimeTracePayload payload)
        {
            if (m_Count == m_Pending.Length)
            {
                m_Pending[m_Count - 1].Payload.StackCount++;
                return;
            }
            if (m_Count == m_Pending.Length - 1)
            {
                kind = RuntimeTraceEventKind.TraceSamplingLimited;
                channel = CaptureGraph ? RuntimeTraceChannel.Graph : RuntimeTraceChannel.Values;
                source = RuntimeSourceElementKey.Graph(m_Binding.Invocations[invocation].GraphId);
                payload.Status = "Limited";
                payload.Detail = "表现图候选记录达到采集容量";
                payload.StackCount = 1;
            }
            m_Pending[m_Count++] = new PendingTrace
            {
                SourceMap = m_Program.SourceMap,
                Channel = channel,
                Kind = kind,
                Source = source,
                Instance = Instance(invocation),
                Payload = payload
            };
        }

        internal void Commit(ulong frame)
        {
            if (m_Count == 0)
                return;
            m_Context.BeginPresentationFrame(frame);
            for (int i = 0; i < m_Count; i++)
            {
                ref readonly PendingTrace trace = ref m_Pending[i];
                m_Context.Publish(trace.SourceMap, trace.Channel, RuntimeTraceDomain.Presentation, trace.Kind,
                    trace.Source, trace.Instance, trace.Payload);
            }
            Discard();
        }

        internal void Discard()
        {
            Array.Clear(m_Pending, 0, m_Count);
            m_Count = 0;
        }

        static DebugValueSnapshot CaptureValue(AbilityStateValue value) => value.Kind switch
        {
            ProgramStateValueKind.Boolean => new DebugValueSnapshot(DebugValueKind.Boolean, value.Boolean, 0, 0, 0, "Boolean", default),
            ProgramStateValueKind.Int32 => new DebugValueSnapshot(DebugValueKind.Int64, false, value.Int32, 0, 0, "Int32", default),
            ProgramStateValueKind.UInt64 => new DebugValueSnapshot(DebugValueKind.UInt64, false, 0, value.UInt64, 0, "UInt64", default),
            ProgramStateValueKind.Scalar => new DebugValueSnapshot(DebugValueKind.Double, false, 0, 0, value.Scalar.ToSingle(), "Float32", default),
            ProgramStateValueKind.Yaw => new DebugValueSnapshot(DebugValueKind.Double, false, 0, 0, value.Yaw.Degrees.ToSingle(), "Float32YawDegrees", default),
            ProgramStateValueKind.Identity => new DebugValueSnapshot(DebugValueKind.String, false, 0, 0, 0, value.Identity, default),
            ProgramStateValueKind.Vector2 => new DebugValueSnapshot(DebugValueKind.Vector2, false, 0, 0, 0, "Float32Vector2", new Vector4(value.Vector2.X.ToSingle(), value.Vector2.Y.ToSingle(), 0, 0)),
            ProgramStateValueKind.Vector3 => new DebugValueSnapshot(DebugValueKind.Vector3, false, 0, 0, 0, "Float32Vector3", new Vector4(value.Vector3.X.ToSingle(), value.Vector3.Y.ToSingle(), value.Vector3.Z.ToSingle(), 0)),
            _ => new DebugValueSnapshot(DebugValueKind.TypeOnly, false, (int)value.Kind, 0, 0, "ProgramStateValueKind", default)
        };
    }
}
