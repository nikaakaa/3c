using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using FixedSimulationActorBinding = ThirdPersonSimulation.Fixed.SimulationActorBinding;
using FixedSimulationActorTickResult = ThirdPersonSimulation.Fixed.SimulationActorTickResult;
using FixedSimulationBoundaryTraceRecord = ThirdPersonSimulation.Fixed.SimulationBoundaryTraceRecord;
using FixedSimulationDiagnosticsSink = ThirdPersonSimulation.Fixed.ISimulationDiagnosticsSink;
using FixedSimulationModelTraceRecord = ThirdPersonSimulation.Fixed.SimulationModelTraceRecord;
using FixedSimulationPipelineTraceRecord = ThirdPersonSimulation.Fixed.SimulationPipelineTraceRecord;
using FixedSimulationTraceRecord = ThirdPersonSimulation.Fixed.SimulationTraceRecord;
using FixedSimulationWorldTraceRecord = ThirdPersonSimulation.Fixed.SimulationWorldTraceRecord;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    internal sealed class FixedCharacterRuntimeDiagnosticsAdapter :
        FixedSimulationDiagnosticsSink,
        ISimulationControlTraceInterest,
        ISimulationValueTraceInterest,
        IFixedCommittedDiagnosticsPublisher
    {
        readonly struct OperationSource
        {
            public OperationSource(RuntimeSourceElementHandle handle, List<ProgramSourceMapEntry> invocations)
            {
                Handle = handle;
                Invocations = invocations;
            }

            public RuntimeSourceElementHandle Handle { get; }
            public List<ProgramSourceMapEntry> Invocations { get; }

            public string ResolvePath(AbilityTreeClipInvocation? treeClip)
            {
                if (Invocations.Count == 1)
                    return Invocations[0].GraphInvocationPath;
                if (!treeClip.HasValue)
                    throw new InvalidOperationException("多入口 TreeClip 技能 Trace 缺少当前 Clip 调用身份。");
                string hook = treeClip.Value.Hook switch
                {
                    AbilityTreeClipHook.OnEnable => "OnEnable",
                    AbilityTreeClipHook.OnDisable => "OnDisable",
                    AbilityTreeClipHook.OnDestroy => "OnDestroy",
                    AbilityTreeClipHook.Root => "Root",
                    _ => throw new ArgumentOutOfRangeException(nameof(treeClip))
                };
                for (int i = 0; i < Invocations.Count; i++)
                {
                    ProgramSourceMapEntry invocation = Invocations[i];
                    if (string.Equals(invocation.InvocationCallerClipId, treeClip.Value.ClipAuthoringId, StringComparison.Ordinal) &&
                        string.Equals(invocation.InvocationCallerId, hook, StringComparison.Ordinal))
                        return invocation.GraphInvocationPath;
                }
                throw new InvalidOperationException("TreeClip 技能 Trace 的当前入口不在编译调用表中。");
            }
        }

        sealed class AbilitySources
        {
            readonly OperationSource[] m_Operations;
            readonly Dictionary<string, RuntimeSourceElementHandle> m_Edges = new(StringComparer.Ordinal);

            public AbilitySources(RuntimeDiagnosticsContext context, FixedGameplayAbilityExecutionData ability)
            {
                m_Operations = new OperationSource[ability.Operations.Count];
                var invocations = new Dictionary<string, List<ProgramSourceMapEntry>>(StringComparer.Ordinal);
                IReadOnlyList<ProgramSourceMapEntry> sources = ability.SourceMap;
                for (int i = 0; i < sources.Count; i++)
                {
                    ProgramSourceMapEntry source = sources[i];
                    if (source.TargetKind != ProgramSourceTargetKind.GraphInvocation)
                        continue;
                    if (!invocations.TryGetValue(source.SourceInvocationPath, out List<ProgramSourceMapEntry> entries))
                    {
                        entries = new List<ProgramSourceMapEntry>();
                        invocations.Add(source.SourceInvocationPath, entries);
                    }
                    entries.Add(source);
                }
                for (int i = 0; i < sources.Count; i++)
                {
                    ProgramSourceMapEntry source = sources[i];
                    if (source.TargetKind != ProgramSourceTargetKind.Operation &&
                        (source.TargetKind != ProgramSourceTargetKind.Reference || string.IsNullOrEmpty(source.EdgeId)))
                        continue;
                    RuntimeSourceElementKey key = AbilityDebugSourceMapFiller.SourceKey(source);
                    if (!context.SourceMap.TryGetHandle(key, out RuntimeSourceElementHandle handle))
                        throw new InvalidOperationException("Fixed 技能编译来源缺少 RuntimeDebug SourceMap 句柄。");
                    if (source.TargetKind == ProgramSourceTargetKind.Operation)
                    {
                        if (!invocations.TryGetValue(source.GraphInvocationPath, out List<ProgramSourceMapEntry> entries))
                            throw new InvalidOperationException("Fixed 技能操作缺少编译图调用路径。");
                        m_Operations[source.TargetIndex] = new OperationSource(handle, entries);
                    }
                    else
                    {
                        m_Edges.Add(ability.ControlFlow[source.TargetIndex].Identity, handle);
                    }
                }
            }

            public bool TryGetOperation(int index, out OperationSource source)
            {
                source = index >= 0 && index < m_Operations.Length ? m_Operations[index] : default;
                return source.Handle.IsValid;
            }

            public bool TryGetEdge(string identity, out RuntimeSourceElementHandle handle) =>
                m_Edges.TryGetValue(identity, out handle);
        }

        readonly RuntimeDiagnosticsContext m_Context;
        readonly Guid m_ExecutionId;
        readonly Dictionary<string, AbilitySources> m_Abilities = new(StringComparer.Ordinal);

        public FixedCharacterRuntimeDiagnosticsAdapter(RuntimeDiagnosticsContext context, FixedSimulationActorBinding binding)
        {
            m_Context = context ?? throw new ArgumentNullException(nameof(context));
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
            m_ExecutionId = Guid.NewGuid();
            IReadOnlyList<FixedGameplayAbilityExecutionInstallation> installations = binding.AbilityInstallations.Installations;
            for (int i = 0; i < installations.Count; i++)
            {
                FixedGameplayAbilityExecutionData ability = installations[i].Data;
                m_Abilities.Add(ability.AbilityId.Value, new AbilitySources(context, ability));
            }
        }

        public bool IsEnabled =>
            (m_Context.Store.EffectiveChannels & (RuntimeTraceChannel.Graph | RuntimeTraceChannel.StateMachine)) != 0;

        public bool IsControlCaptureRequested(ActorId actorId) =>
            (m_Context.Store.EffectiveChannels & (RuntimeTraceChannel.Graph | RuntimeTraceChannel.StateMachine)) != 0;

        public bool IsValueCaptureRequested(ActorId actorId) => false;

        public void PublishBoundary(FixedSimulationBoundaryTraceRecord record) { }
        public void PublishPipeline(FixedSimulationPipelineTraceRecord record) { }
        public void PublishOperation(FixedSimulationTraceRecord record) { }
        public void PublishModel(FixedSimulationModelTraceRecord record) { }
        public void PublishWorld(FixedSimulationWorldTraceRecord record) { }

        public void PublishCommitted(FixedSimulationActorTickResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (result.TraceRecords.Count == 0)
                return;
            m_Context.BeginLogicTick(result.Tick.Value);
            IReadOnlyList<FixedSimulationTraceRecord> records = result.TraceRecords;
            for (int i = 0; i < records.Count; i++)
            {
                FixedSimulationTraceRecord record = records[i];
                if (record.ActionInstanceId == 0 || !record.Header.Activation.Source.IsSkillOperation)
                    continue;
                bool isNode = TryNodeKind(record.Code, out RuntimeTraceEventKind kind);
                if (!isNode && record.ControlFlow == null)
                    continue;
                if (!m_Abilities.TryGetValue(record.SkillId, out AbilitySources ability))
                    throw new InvalidOperationException("Fixed 技能 Trace 不属于已装配的技能。");
                if (!ability.TryGetOperation(record.Header.Activation.Source.Operation.Value, out OperationSource operation))
                    continue;
                RuntimeSourceElementHandle edge = default;
                if (!isNode && !ability.TryGetEdge(record.ControlFlow.Identity, out edge))
                    continue;
                if (record.SkillExecutionGeneration == 0 || record.GraphInvocationGeneration == 0)
                    throw new InvalidOperationException(
                        $"Fixed 技能图 Trace '{record.Code}' 缺少释放或图调用代数 ({record.SkillExecutionGeneration}/{record.GraphInvocationGeneration})。");
                string path = operation.ResolvePath(record.Header.TreeClipInvocation);
                RuntimeInstanceKey instance = RuntimeInstanceKey.SkillExecution(
                    m_Context.CharacterRuntimeId,
                    m_ExecutionId,
                    record.SkillId,
                    record.ActionInstanceId,
                    path,
                    record.SkillExecutionGeneration,
                    record.GraphInvocationGeneration);
                RuntimeTracePayload payload = new RuntimeTracePayload
                {
                    Detail = record.Detail,
                    SkillId = record.SkillId,
                    ActionInstanceId = record.ActionInstanceId,
                    CallSiteId = path,
                    ActivationGeneration = record.Header.Activation.Generation,
                    SkillExecutionGeneration = record.SkillExecutionGeneration,
                    GraphInvocationGeneration = record.GraphInvocationGeneration,
                    ParentInvocationGeneration = record.ParentInvocationGeneration
                };
                if (isNode)
                {
                    m_Context.Publish(RuntimeTraceChannel.Graph, RuntimeTraceDomain.Logic,
                        kind, operation.Handle, instance, payload);
                    continue;
                }
                ProgramControlFlowEdge flow = record.ControlFlow;
                bool transition = flow.Kind == ProgramControlFlowKind.Transition;
                payload.Name = flow.Kind == ProgramControlFlowKind.Value ? "Value" : "Flow";
                payload.Status = record.ControlFlowSelected ? "Selected" :
                    record.ControlFlowPassed ? "Passed" : "Rejected";
                payload.Flag = record.ControlFlowPassed;
                m_Context.Publish(transition ? RuntimeTraceChannel.StateMachine : RuntimeTraceChannel.Graph,
                    RuntimeTraceDomain.Logic,
                    transition
                        ? record.ControlFlowSelected ? RuntimeTraceEventKind.StateTransitionSelected : RuntimeTraceEventKind.StateTransitionEvaluated
                        : record.ControlFlowSelected ? RuntimeTraceEventKind.EdgeSelected : RuntimeTraceEventKind.EdgeEvaluated,
                    edge, instance, payload);
            }
        }

        static bool TryNodeKind(string code, out RuntimeTraceEventKind kind)
        {
            kind = code switch
            {
                "operation_enter" => RuntimeTraceEventKind.NodeEntered,
                "operation_running" => RuntimeTraceEventKind.NodeRunning,
                "operation_waiting" => RuntimeTraceEventKind.NodeWaiting,
                "operation_complete" => RuntimeTraceEventKind.NodeCompleted,
                "operation_stop_requested" => RuntimeTraceEventKind.NodeStopRequested,
                "operation_stopped" => RuntimeTraceEventKind.NodeStopped,
                "operation_force_stopped" => RuntimeTraceEventKind.NodeForceStopped,
                _ => RuntimeTraceEventKind.None
            };
            return kind != RuntimeTraceEventKind.None;
        }
    }
}
