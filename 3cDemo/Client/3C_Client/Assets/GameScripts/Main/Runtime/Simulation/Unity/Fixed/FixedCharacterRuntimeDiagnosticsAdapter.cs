using System;
using ThirdPersonCharacter.Pipeline.Diagnostics;
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
using AbilityStateValue = ThirdPersonSimulation.Fixed.AbilityStateValue;
using GameplayFact = ThirdPersonSimulation.Fixed.GameplayFact;
using GameplayFactKind = ThirdPersonSimulation.Fixed.GameplayFactKind;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    internal sealed class FixedCharacterRuntimeDiagnosticsAdapter :
        FixedSimulationDiagnosticsSink,
        ISimulationControlTraceInterest,
        ISimulationValueTraceInterest,
        ISimulationBlackboardTraceInterest,
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
            readonly RuntimeSourceElementHandle[] m_BlackboardDeclarations;
            readonly Dictionary<string, RuntimeSourceElementHandle> m_Edges = new(StringComparer.Ordinal);

            public AbilitySources(RuntimeDiagnosticsContext context, FixedGameplayAbilityExecutionData ability)
            {
                m_Operations = new OperationSource[ability.Operations.Count];
                m_BlackboardDeclarations = new RuntimeSourceElementHandle[ability.StateSlots.Count];
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
                    if (source.TargetKind == ProgramSourceTargetKind.StateSlot && !string.IsNullOrEmpty(source.DeclarationId))
                    {
                        m_BlackboardDeclarations[source.TargetIndex] = context.ResolveSourceHandle(AbilityDebugSourceMapFiller.SourceKey(source));
                        continue;
                    }
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
                for (int i = 0; i < ability.StateSlots.Count; i++)
                    if (ability.StateSlots[i].Semantic == ProgramStateSemantic.BlackboardValue && !m_BlackboardDeclarations[i].IsValid)
                        throw new InvalidOperationException("黑板值槽缺少编译声明来源。");
            }

            public RuntimeSourceElementHandle BlackboardDeclaration(int stateSlot) => m_BlackboardDeclarations[stateSlot];

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
        readonly IReadOnlyList<FixedGameplayAbilityExecutionInstallation> m_Installations;
        readonly List<FixedBlackboardValueSnapshot> m_BlackboardValues = new();
        readonly Dictionary<(string Ability, ulong Action, int Slot), FixedBlackboardValueSnapshot> m_LastBlackboardValues = new();
        readonly HashSet<(string Ability, ulong Action, int Slot)> m_SeenBlackboardValues = new();
        readonly List<(string Ability, ulong Action, int Slot)> m_RemovedBlackboardValues = new();
        long m_BlackboardLiveGeneration = -1;
        Guid m_BlackboardCaptureId;

        internal bool TryGetBlackboardValue(CharacterSkillId ability, ulong actionInstanceId, int stateSlot,
            out FixedBlackboardValueSnapshot value) =>
            m_LastBlackboardValues.TryGetValue((ability.Value, actionInstanceId, stateSlot), out value);

        public FixedCharacterRuntimeDiagnosticsAdapter(RuntimeDiagnosticsContext context, FixedSimulationActorBinding binding)
        {
            m_Context = context ?? throw new ArgumentNullException(nameof(context));
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
            m_ExecutionId = Guid.NewGuid();
            IReadOnlyList<FixedGameplayAbilityExecutionInstallation> installations = binding.AbilityInstallations.Installations;
            m_Installations = installations;
            for (int i = 0; i < installations.Count; i++)
            {
                FixedGameplayAbilityExecutionData ability = installations[i].Data;
                m_Abilities.Add(ability.AbilityId.Value, new AbilitySources(context, ability));
            }
        }

        public bool IsEnabled =>
            (m_Context.Store.EffectiveChannels & (RuntimeTraceChannel.Graph | RuntimeTraceChannel.StateMachine |
                RuntimeTraceChannel.Blackboard | RuntimeTraceChannel.GameplayEffect | RuntimeTraceChannel.Values)) != 0;

        public bool IsControlCaptureRequested(ActorId actorId) =>
            (m_Context.Store.EffectiveChannels & (RuntimeTraceChannel.Graph | RuntimeTraceChannel.StateMachine)) != 0;

        public bool IsBlackboardCaptureRequested(ActorId actorId) =>
            (m_Context.Store.EffectiveChannels & RuntimeTraceChannel.Blackboard) != 0;

        public bool IsValueCaptureRequested(ActorId actorId) =>
            (m_Context.Store.EffectiveChannels & RuntimeTraceChannel.Values) != 0;

        public void PublishBoundary(FixedSimulationBoundaryTraceRecord record) { }
        public void PublishPipeline(FixedSimulationPipelineTraceRecord record) { }
        public void PublishOperation(FixedSimulationTraceRecord record) { }
        public void PublishModel(FixedSimulationModelTraceRecord record) { }
        public void PublishWorld(FixedSimulationWorldTraceRecord record) { }

        public void PublishCommitted(FixedSimulationActorTickResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            m_Context.BeginLogicTick(result.Tick.Value);
            PublishFacts(result.GameplayFacts);
            IReadOnlyList<FixedSimulationTraceRecord> records = result.TraceRecords;
            for (int i = 0; i < records.Count; i++)
            {
                FixedSimulationTraceRecord record = records[i];
                if (record.BlackboardTrace.HasValue)
                {
                    PublishBlackboard(in record);
                    continue;
                }
                if (PublishObservation(in record))
                    continue;
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
                    ParentInvocationGeneration = record.ParentInvocationGeneration,
                    LoopIteration = record.LoopIteration
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
            PublishBlackboardSnapshots(result.State);
        }

        void PublishBlackboardSnapshots(FixedCharacterRuntimeState state)
        {
            if (!m_Context.ShouldPublish(RuntimeTraceChannel.Blackboard, RuntimeTraceEventKind.BlackboardSnapshot))
            {
                m_LastBlackboardValues.Clear();
                return;
            }
            long liveGeneration = m_Context.Store.LiveStateGeneration;
            Guid captureId = m_Context.Store.CaptureStatus.CaptureId;
            if (m_BlackboardLiveGeneration != liveGeneration || m_BlackboardCaptureId != captureId)
            {
                m_LastBlackboardValues.Clear();
                m_BlackboardLiveGeneration = liveGeneration;
                m_BlackboardCaptureId = captureId;
            }
            m_BlackboardValues.Clear();
            m_SeenBlackboardValues.Clear();
            for (int i = 0; i < m_Installations.Count; i++)
                m_Installations[i].AppendBlackboardSnapshot(state, m_BlackboardValues);
            for (int i = 0; i < m_BlackboardValues.Count; i++)
            {
                FixedBlackboardValueSnapshot value = m_BlackboardValues[i];
                var key = (value.Ability.Value, value.ActionInstanceId, value.StateSlot);
                m_SeenBlackboardValues.Add(key);
                if (m_LastBlackboardValues.TryGetValue(key, out FixedBlackboardValueSnapshot previous) &&
                    previous.SkillGeneration == value.SkillGeneration && previous.Owner == value.Owner &&
                    previous.IsActive == value.IsActive && previous.Value.Equals(value.Value))
                    continue;
                PublishBlackboardSnapshot(value, value.IsActive);
                m_LastBlackboardValues[key] = value;
            }
            m_RemovedBlackboardValues.Clear();
            foreach (var previous in m_LastBlackboardValues)
            {
                if (m_SeenBlackboardValues.Contains(previous.Key))
                    continue;
                PublishBlackboardSnapshot(previous.Value, false);
                m_RemovedBlackboardValues.Add(previous.Key);
            }
            for (int i = 0; i < m_RemovedBlackboardValues.Count; i++)
                m_LastBlackboardValues.Remove(m_RemovedBlackboardValues[i]);
        }

        void PublishBlackboardSnapshot(FixedBlackboardValueSnapshot value, bool active)
        {
            RuntimeInstanceKey instance = value.ActionInstanceId == 0
                ? RuntimeInstanceKey.Character(m_Context.CharacterRuntimeId)
                : RuntimeInstanceKey.ActionInstance(m_Context.CharacterRuntimeId, m_ExecutionId,
                    value.Ability.Value, value.ActionInstanceId, value.SkillGeneration);
            var payload = new RuntimeTracePayload
            {
                Name = value.ScopeIdentity,
                Status = active ? "Active" : "Inactive",
                SkillId = value.Ability.Value,
                ActionInstanceId = value.ActionInstanceId,
                SkillExecutionGeneration = value.SkillGeneration,
                BlackboardStateSlot = value.StateSlot,
                BlackboardScope = (int)value.ScopeKind,
                BlackboardOwnerIndex = value.CompiledOwnerIndex,
                BlackboardOwnerGeneration = value.Owner.Generation,
                BlackboardLifetime = (int)value.Lifetime,
                BlackboardIsActive = active,
                Value = CaptureValue(value.Value)
            };
            m_Context.Publish(RuntimeTraceChannel.Blackboard, RuntimeTraceDomain.Logic,
                RuntimeTraceEventKind.BlackboardSnapshot,
                m_Abilities[value.Ability.Value].BlackboardDeclaration(value.StateSlot), instance, payload);
        }

        void PublishBlackboard(in FixedSimulationTraceRecord record)
        {
            if (!m_Context.ShouldPublish(RuntimeTraceChannel.Blackboard, RuntimeTraceEventKind.BlackboardWritten))
                return;
            var value = record.BlackboardTrace.Value;
            AbilitySources ability = m_Abilities[record.SkillId];
            RuntimeSourceElementHandle source = ability.BlackboardDeclaration(value.StateSlot);
            string path = string.Empty;
            bool command = record.Header.Activation.Source.IsBlackboardCommand;
            if (!command)
            {
                if (!ability.TryGetOperation(record.Header.Activation.Source.Operation.Value, out OperationSource operation))
                    throw new InvalidOperationException("黑板写入缺少正式操作来源。");
                path = operation.ResolvePath(record.Header.TreeClipInvocation);
            }
            RuntimeInstanceKey instance = record.ActionInstanceId == 0
                ? RuntimeInstanceKey.Character(m_Context.CharacterRuntimeId)
                : command
                    ? RuntimeInstanceKey.ActionInstance(m_Context.CharacterRuntimeId, m_ExecutionId,
                        record.SkillId, record.ActionInstanceId, record.SkillExecutionGeneration)
                    : RuntimeInstanceKey.SkillExecution(
                        m_Context.CharacterRuntimeId, m_ExecutionId, record.SkillId, record.ActionInstanceId,
                        path, record.SkillExecutionGeneration, record.GraphInvocationGeneration);
            var payload = new RuntimeTracePayload
            {
                Status = record.Code,
                SkillId = record.SkillId,
                ActionInstanceId = record.ActionInstanceId,
                CallSiteId = path,
                ActivationGeneration = record.Header.Activation.Generation,
                SkillExecutionGeneration = record.SkillExecutionGeneration,
                GraphInvocationGeneration = record.GraphInvocationGeneration,
                ParentInvocationGeneration = record.ParentInvocationGeneration,
                BlackboardStateSlot = value.StateSlot,
                BlackboardScope = (int)value.Owner.ScopeKind,
                BlackboardOwnerIndex = value.Owner.CompiledOwnerIndex,
                BlackboardOwnerGeneration = value.Owner.Generation,
                BlackboardLifetime = (int)value.Lifetime,
                BlackboardIsActive = true,
                Value = CaptureValue(value.Value)
            };
            m_Context.Publish(RuntimeTraceChannel.Blackboard, RuntimeTraceDomain.Logic,
                RuntimeTraceEventKind.BlackboardWritten, source, instance, payload);
        }

        bool PublishObservation(in FixedSimulationTraceRecord record)
        {
            RuntimeTraceChannel channel;
            RuntimeTraceEventKind kind;
            if (record.ValueTrace != null)
            {
                channel = RuntimeTraceChannel.Values;
                kind = RuntimeTraceEventKind.ValueSampled;
            }
            else if (record.Code == "value_sampling_limit")
            {
                channel = RuntimeTraceChannel.Values;
                kind = RuntimeTraceEventKind.ValueSamplingLimited;
            }
            else if (record.Code == "action_result")
            {
                channel = RuntimeTraceChannel.StateMachine;
                kind = RuntimeTraceEventKind.ActionResultSubmitted;
            }
            else if (record.Code == "blackboard_action_window_projected")
            {
                channel = RuntimeTraceChannel.Blackboard;
                kind = RuntimeTraceEventKind.BlackboardProjected;
            }
            else if (record.Code == "action_window_active" || record.Code == "action_window_inactive")
            {
                channel = RuntimeTraceChannel.Blackboard;
                kind = RuntimeTraceEventKind.ActionWindowSampled;
            }
            else
                return false;
            if (!m_Context.ShouldPublish(channel, kind))
                return true;
            RuntimeSourceElementHandle source = RuntimeSourceElementHandle.Invalid;
            if (record.Header.Activation.Source.IsSkillOperation && m_Abilities.TryGetValue(record.SkillId, out var ability) &&
                ability.TryGetOperation(record.Header.Activation.Source.Operation.Value, out var operation))
                source = operation.Handle;
            var payload = new RuntimeTracePayload
            {
                Name = record.ActionId, Status = record.Code, Detail = record.Detail,
                SkillId = record.SkillId, ActionInstanceId = record.ActionInstanceId,
                InputSequence = record.InputSequence, ActivationGeneration = record.Header.Activation.Generation,
                SkillExecutionGeneration = record.SkillExecutionGeneration,
                GraphInvocationGeneration = record.GraphInvocationGeneration,
                ParentInvocationGeneration = record.ParentInvocationGeneration,
                Flag = record.Code == "action_window_active", ActionResult = (int)record.ActionResult,
                CallSiteId = record.Header.Activation.Source.ExecutionPath
            };
            if (record.ValueTrace != null)
            {
                payload.Name = record.ValueTrace.PortId;
                payload.Status = record.ValueTrace.Direction == ProgramValuePortDirection.Input ? "Input" : "Output";
                payload.Value = CaptureValue(record.ValueTrace.Value);
            }
            m_Context.Publish(channel, RuntimeTraceDomain.Logic, kind, source,
                RuntimeInstanceKey.Character(m_Context.CharacterRuntimeId), payload);
            return true;
        }

        static DebugValueSnapshot CaptureValue(AbilityStateValue value) => value.Kind switch
        {
            ProgramStateValueKind.Boolean => new DebugValueSnapshot(DebugValueKind.Boolean, value.Boolean, 0, 0, 0, "Boolean", default),
            ProgramStateValueKind.Int32 => new DebugValueSnapshot(DebugValueKind.Int64, false, value.Int32, 0, 0, "Int32", default),
            ProgramStateValueKind.UInt64 => new DebugValueSnapshot(DebugValueKind.UInt64, false, 0, value.UInt64, 0, "UInt64", default),
            ProgramStateValueKind.Scalar => new DebugValueSnapshot(DebugValueKind.Double, false, value.Scalar.Raw, 0, value.Scalar.ToDouble(), "FixedQ32.32", default),
            ProgramStateValueKind.Yaw => new DebugValueSnapshot(DebugValueKind.Double, false, value.Yaw.Degrees.Raw, 0, value.Yaw.Degrees.ToDouble(), "FixedYawDegrees", default),
            ProgramStateValueKind.Identity => new DebugValueSnapshot(DebugValueKind.String, false, 0, 0, 0, value.Identity, default),
            ProgramStateValueKind.Vector2 => new DebugValueSnapshot(DebugValueKind.Vector2, false, 0, 0, 0, "FixedVector2", new UnityEngine.Vector4((float)value.Vector2.X.ToDouble(), (float)value.Vector2.Y.ToDouble(), 0, 0)),
            ProgramStateValueKind.Vector3 => new DebugValueSnapshot(DebugValueKind.Vector3, false, 0, 0, 0, "FixedVector3", new UnityEngine.Vector4((float)value.Vector3.X.ToDouble(), (float)value.Vector3.Y.ToDouble(), (float)value.Vector3.Z.ToDouble(), 0)),
            _ => new DebugValueSnapshot(DebugValueKind.TypeOnly, false, (int)value.Kind, 0, 0, "ProgramStateValueKind", default)
        };

        void PublishFacts(IReadOnlyList<GameplayFact> facts)
        {
            for (int index = 0; index < facts.Count; index++)
            {
                GameplayFact fact = facts[index];
                RuntimeTraceChannel channel;
                RuntimeTraceEventKind kind;
                var payload = new RuntimeTracePayload { Name = fact.SubjectId, Status = fact.StateId };
                switch (fact.Kind)
                {
                    case GameplayFactKind.Action:
                        channel = RuntimeTraceChannel.StateMachine;
                        kind = RuntimeTraceEventKind.ActionLifecycleTransitioned;
                        payload.Name = fact.Action.ActionId;
                        payload.SkillId = fact.Action.SkillId.Value;
                        payload.ActionInstanceId = fact.Action.ActionInstanceId;
                        payload.InputSequence = fact.Action.InputSequence;
                        payload.Cause = fact.Action.Reason;
                        payload.ActionPhase = (int)fact.Action.Phase;
                        payload.ActionState = (int)fact.Action.State;
                        payload.LifecycleOperation = (int)fact.Action.TransitionType;
                        break;
                    case GameplayFactKind.ActionWindow:
                        channel = RuntimeTraceChannel.Blackboard;
                        kind = RuntimeTraceEventKind.ActionWindowSampled;
                        payload.Name = fact.ActionWindow.WindowId;
                        payload.Status = fact.ActionWindow.WindowType;
                        payload.OwnerId = fact.ActionWindow.ActionId;
                        payload.ActionInstanceId = fact.ActionWindow.ActionInstanceId;
                        payload.StartTick = fact.ActionWindow.StartTick;
                        payload.EndTick = fact.ActionWindow.EndTick;
                        payload.Revision = fact.ActionWindow.Digest;
                        payload.Flag = true;
                        break;
                    case GameplayFactKind.Effect:
                        channel = RuntimeTraceChannel.GameplayEffect;
                        kind = RuntimeTraceEventKind.GameplayEffectLifecycle;
                        payload.Name = fact.Effect.EffectId;
                        payload.ActivationGeneration = fact.Effect.InstanceId;
                        payload.LifecycleOperation = (int)fact.Effect.Operation;
                        payload.StackCount = fact.Effect.StackCount;
                        payload.StartTick = fact.Effect.StartTick;
                        payload.EndTick = fact.Effect.EndTick;
                        payload.Revision = fact.Effect.LifecycleRevision;
                        break;
                    case GameplayFactKind.Attribute:
                        channel = RuntimeTraceChannel.GameplayEffect;
                        kind = RuntimeTraceEventKind.GameplayAttributeChanged;
                        payload.Name = fact.Attribute.AttributeId;
                        payload.Cause = fact.Attribute.CauseEffectId;
                        payload.Value = CaptureValue(AbilityStateValue.FromScalar(fact.Attribute.CurrentValue));
                        payload.Revision = fact.Attribute.ValueRevision;
                        break;
                    default:
                        continue;
                }
                if (m_Context.ShouldPublish(channel, kind))
                    m_Context.Publish(channel, RuntimeTraceDomain.Logic, kind, RuntimeSourceElementHandle.Invalid,
                        RuntimeInstanceKey.Character(m_Context.CharacterRuntimeId), payload);
            }
        }

        static bool TryNodeKind(string code, out RuntimeTraceEventKind kind)
        {
            kind = code switch
            {
                "loop_iteration_enter" => RuntimeTraceEventKind.LoopIterationEntered,
                "loop_iteration_complete" => RuntimeTraceEventKind.LoopIterationCompleted,
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
