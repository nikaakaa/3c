using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BTSMTL.EventGraphs;
using ThirdPersonSimulation;
using ThirdPersonCharacter.Animation.TransitionRouting;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class CharacterPoseNativeStateMachineRegistration
    {
        internal static void Register(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            CharacterAnimationPresentationProfile profile)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            registry.Register(
                CharacterPoseNodeKind.PoseStateMachine,
                (CharacterPoseCanvasNode node,
                    in CharacterPoseNativePreparedBinding preparedBinding,
                    in CharacterPoseNativeInstanceContext context) =>
                {
                    CharacterPoseNativeStateMachineSource source = null;
                    CharacterPoseNativeNodePoseBuffer buffer = null;
                    try
                    {
                        int capacity = Math.Max(
                            1,
                            preparedBinding.Graph.Nodes.Count);
                        source = new CharacterPoseNativeStateMachineSource(
                            node,
                            in preparedBinding,
                            profile,
                            registry,
                            capacity);
                        buffer = new CharacterPoseNativeNodePoseBuffer(
                            FindNodeIndex(preparedBinding.Graph, node),
                            preparedBinding.Rig.PoseBoneCount,
                            preparedBinding.InputContract.Parameters.Count,
                            capacity);
                        return new CharacterPoseNativeStateMachineHandler(
                            node.NodeId,
                            source,
                            buffer);
                    }
                    catch
                    {
                        buffer?.Dispose();
                        source?.Dispose();
                        throw;
                    }
                });
        }

        static int FindNodeIndex(
            CharacterPoseCanvasGraph graph,
            CharacterPoseCanvasNode node)
        {
            for (int i = 0; i < graph.Nodes.Count; i++)
                if (ReferenceEquals(graph.Nodes[i], node))
                    return i;
            throw new InvalidOperationException(
                $"Pose StateMachine node '{node.NodeId}' is not owned by graph '{graph.GraphId}'.");
        }
    }

    internal sealed class CharacterPoseNativeStateMachineSource :
        ICharacterPoseNativeStateMachineSource
    {
        sealed class StateRuntime
        {
            internal StateRuntime(CharacterPoseStateDefinition definition)
            {
                Definition = definition;
            }

            internal readonly CharacterPoseStateDefinition Definition;
            internal CharacterPoseNativeGraphRuntime Graph;
            internal CharacterPoseNativeFrameLease Lease;
            internal CharacterPoseNativePreparationResult Preparation;
            internal CharacterPoseNativeEvaluationResult Evaluation;
            internal bool FrameOpen;
        }

        readonly struct BoundRuleOperation
        {
            internal BoundRuleOperation(
                CharacterPoseTransitionRuleOperation definition,
                CharacterAnimationVariableContract variables)
            {
                Definition = definition;
                Variable = definition.Kind == PoseTransitionRuleOperationKind.AnimationVariableInput
                    ? variables.Bind(definition.ParameterId.Value)
                    : default;
            }

            internal CharacterPoseTransitionRuleOperation Definition { get; }
            internal EventGraphVariableBinding Variable { get; }
        }

        enum RuleValueKind : byte
        {
            Bool = 1,
            Number = 2,
            Identity = 3
        }

        readonly struct RuleValue
        {
            internal RuleValue(bool value)
            {
                Kind = RuleValueKind.Bool;
                Bool = value;
                Number = 0f;
                Identity = string.Empty;
            }

            internal RuleValue(float value)
            {
                if (!float.IsFinite(value))
                    throw new ArgumentOutOfRangeException(nameof(value));
                Kind = RuleValueKind.Number;
                Bool = false;
                Number = value;
                Identity = string.Empty;
            }

            internal RuleValue(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Pose StateMachine rule identity is missing.", nameof(value));
                Kind = RuleValueKind.Identity;
                Bool = false;
                Number = 0f;
                Identity = value.Trim();
            }

            internal RuleValueKind Kind { get; }
            internal bool Bool { get; }
            internal float Number { get; }
            internal string Identity { get; }
        }

        readonly PoseNodeId m_NodeId;
        readonly CharacterPresentationPoseGraphAsset m_GraphAsset;
        readonly CharacterPoseStateMachineDefinition m_Definition;
        readonly CharacterAnimationPresentationProfile m_Profile;
        readonly CharacterPoseStateGraphCreationMode m_CreationMode;
        readonly ICharacterPoseNativeNodeHandlerFactory m_Factory;
        readonly Dictionary<PoseStateId, StateRuntime> m_States;
        readonly CharacterPoseNativeNodePoseBuffer m_OutputBuffer;
        readonly CharacterPoseNativeNodePoseBuffer m_SecondaryOutputBuffer;
        readonly Dictionary<PoseStateId, float> m_StateDurations;
        readonly Dictionary<PoseStateId, CharacterPoseStateTransition[]> m_TransitionsByState;
        readonly StateRuntime[] m_ActiveStates = new StateRuntime[2];
        int m_ActiveStateCount;
        FixedCapacityFrameBuffer<CharacterPoseNativeSourceRequest> m_SourceRequests;
        readonly Dictionary<CharacterPoseTransitionRuleGraph,
            Dictionary<PoseTransitionRuleOperationId, BoundRuleOperation>> m_RuleOperationTables;
        readonly Dictionary<PoseTransitionRuleOperationId, RuleValue> m_RuleValues;
        readonly HashSet<PoseTransitionRuleOperationId> m_RuleVisiting;
        CharacterPoseNativeGraphRuntime m_ParentRuntime;
        CharacterPoseNativeLocalPoseValue m_Output;
        CharacterPoseNativeFrameLineage m_Lineage;
        CharacterPoseStateTransition m_CommittedTransition;
        CharacterPoseStateTransition m_PendingTransition;
        PoseStateId m_CommittedState;
        PoseStateId m_PendingState;
        float m_CommittedTime;
        float m_PendingTime;
        float m_CommittedTransitionElapsed;
        float m_PendingTransitionElapsed;
        ulong m_NextRequestId = 1;
        ulong m_NextChildInstanceId = 1;
        ulong m_NextContinuityIdentity = 1;
        ulong m_CommittedContinuityIdentity;
        ulong m_ContinuityIdentity;
        int m_PageIndex = -1;
        int m_CommittedPageIndex = -1;
        bool m_CommittedInitialized;
        bool m_PendingInitialized;
        bool m_FrameOpen;
        bool m_EvaluationPrepared;
        bool m_Disposed;

        public int PhasePlayerCount
        {
            get
            {
                if (!m_FrameOpen)
                    return 0;
                int count = 0;
                for (int i = 0; i < m_ActiveStateCount; i++)
                    if (m_ActiveStates[i].FrameOpen)
                        count += m_ActiveStates[i].Graph.PhaseSources.PhasePlayerCount;
                return count;
            }
        }

        public Presentation.AnimationClipPlayerRuntime ReadPhasePlayer(int index)
        {
            for (int i = 0; i < m_ActiveStateCount; i++)
            {
                StateRuntime state = m_ActiveStates[i];
                if (!state.FrameOpen)
                    continue;
                int count = state.Graph.PhaseSources.PhasePlayerCount;
                if (index < count)
                    return state.Graph.PhaseSources.ReadPhasePlayer(index);
                index -= count;
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        void SynchronizeTransition(int activeStateCount)
        {
            if (m_PendingTransition == null || activeStateCount != 2)
                return;
            ICharacterPoseNativePhaseSource outgoing = m_ActiveStates[0].Graph.PhaseSources;
            ICharacterPoseNativePhaseSource incoming = m_ActiveStates[1].Graph.PhaseSources;
            int outgoingCount = outgoing.PhasePlayerCount;
            int incomingCount = incoming.PhasePlayerCount;
            for (int targetIndex = 0; targetIndex < incomingCount; targetIndex++)
            {
                Presentation.AnimationClipPlayerRuntime target = incoming.ReadPhasePlayer(targetIndex);
                for (int sourceIndex = 0; sourceIndex < outgoingCount; sourceIndex++)
                {
                    Presentation.AnimationClipPlayerRuntime source = outgoing.ReadPhasePlayer(sourceIndex);
                    if (!string.Equals(source.SyncGroupId, target.SyncGroupId, StringComparison.Ordinal))
                        continue;
                    target.SynchronizePhase(source,
                        Math.Max(0f, m_PendingTransition.DurationSeconds - m_PendingTransitionElapsed));
                    break;
                }
            }
        }

        public int StateCaptureCount
        {
            get
            {
                if (!m_CommittedInitialized)
                    return 0;
                int count = 1;
                foreach (StateRuntime state in m_States.Values)
                    if (IsCommittedState(state) && state.Graph != null)
                        count += state.Graph.StateCapture.Count;
                return count;
            }
        }
        bool IsCommittedState(StateRuntime state) => state.Definition.StateId == m_CommittedState ||
            m_CommittedTransition != null && state.Definition.StateId == m_CommittedTransition.TargetStateId;
        public Diagnostics.CharacterNativeStateCaptureRow ReadStateCapture(int index)
        {
            if (index == 0 && m_CommittedInitialized)
                return new Diagnostics.CharacterNativeStateCaptureRow(m_NodeId.Value, m_CommittedState.Value,
                    m_CommittedTransition?.TargetStateId.Value ?? string.Empty,
                    m_CommittedTransition?.TransitionId.Value ?? string.Empty,
                    m_CommittedTime, m_CommittedTransitionElapsed, m_CommittedTransition?.DurationSeconds ?? 0f);
            index--;
            foreach (StateRuntime state in m_States.Values)
            {
                if (!IsCommittedState(state) || state.Graph == null)
                    continue;
                var page = state.Graph.StateCapture;
                if (index < page.Count)
                    return page[index];
                index -= page.Count;
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        internal CharacterPoseNativeStateMachineSource(
            CharacterPoseCanvasNode node,
            in CharacterPoseNativePreparedBinding preparedBinding,
            CharacterAnimationPresentationProfile profile,
            ICharacterPoseNativeNodeHandlerFactory factory,
            int contributionCapacity)
        {
            if (node == null || !preparedBinding.IsValid || !profile || factory == null ||
                contributionCapacity <= 0)
            {
                throw new ArgumentException(
                    "Pose native StateMachine source binding is invalid.");
            }
            m_NodeId = node.NodeId;
            m_GraphAsset = profile.PoseGraph;
            m_Definition = node.PoseStateMachine ??
                throw new InvalidOperationException(
                    $"Pose StateMachine '{node.NodeId}' has no definition.");
            int ruleOperationCapacity = ResolveRuleOperationCapacity();
            m_RuleOperationTables = new Dictionary<CharacterPoseTransitionRuleGraph,
                Dictionary<PoseTransitionRuleOperationId, BoundRuleOperation>>();
            m_RuleValues = new Dictionary<PoseTransitionRuleOperationId, RuleValue>(
                ruleOperationCapacity);
            m_RuleVisiting = new HashSet<PoseTransitionRuleOperationId>(
                ruleOperationCapacity);
            m_Profile = profile;
            m_CreationMode = profile.StateGraphCreationMode;
            m_Factory = factory;
            m_States = new Dictionary<PoseStateId, StateRuntime>();
            m_StateDurations = new Dictionary<PoseStateId, float>();
            RequireDefinition();
            m_TransitionsByState = BuildTransitionsByState();
            m_OutputBuffer = new CharacterPoseNativeNodePoseBuffer(
                0,
                preparedBinding.Rig.PoseBoneCount,
                preparedBinding.InputContract.Parameters.Count,
                contributionCapacity);
            m_SecondaryOutputBuffer = m_OutputBuffer.CreateSibling();
        }

        int ResolveRuleOperationCapacity()
        {
            int capacity = 0;
            for (int i = 0; i < m_Definition.Transitions.Count; i++)
            {
                CharacterPoseTransitionRuleGraph rule =
                    m_Definition.Transitions[i]?.Rule;
                if (rule != null)
                    capacity = Math.Max(capacity, rule.Operations.Count);
            }
            return capacity;
        }

        void BuildRuleOperationTables(CharacterAnimationVariableContract variables)
        {
            for (int i = 0; i < m_Definition.Transitions.Count; i++)
            {
                CharacterPoseTransitionRuleGraph rule =
                    m_Definition.Transitions[i]?.Rule;
                if (rule == null || m_RuleOperationTables.ContainsKey(rule))
                    continue;
                var operations = new Dictionary<PoseTransitionRuleOperationId,
                    BoundRuleOperation>(rule.Operations.Count);
                for (int operationIndex = 0; operationIndex < rule.Operations.Count; operationIndex++)
                {
                    CharacterPoseTransitionRuleOperation operation = rule.Operations[operationIndex];
                    if (operation == null || !operation.OperationId.IsValid ||
                        !operations.TryAdd(operation.OperationId, new BoundRuleOperation(operation, variables)))
                    {
                        throw new InvalidOperationException(
                            $"Pose StateMachine '{m_NodeId}' transition rule contains duplicate or missing operation.");
                    }
                }
                if (!operations.ContainsKey(rule.OutputOperationId))
                    throw new InvalidOperationException(
                        $"Pose StateMachine '{m_NodeId}' transition rule output operation is missing.");
                m_RuleOperationTables.Add(rule, operations);
            }
        }

        public void PrepareGraphs(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            m_SourceRequests = new FixedCapacityFrameBuffer<CharacterPoseNativeSourceRequest>(
                runtime.InstanceContext.SourceRequestLayout.RequireStateMachine(m_NodeId));
            BuildRuleOperationTables(runtime.InstanceContext.VariableContract);
            switch (m_CreationMode)
            {
                case CharacterPoseStateGraphCreationMode.DuringPreparation:
                    foreach (StateRuntime state in m_States.Values)
                        EnsureState(runtime, state);
                    break;
                case CharacterPoseStateGraphCreationMode.OnFirstEntry:
                    break;
                default:
                    throw new InvalidOperationException("Pose StateMachine graph creation mode is invalid.");
            }
        }

        public IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            if (runtime == null || node == null || node.NodeId != m_NodeId ||
                !input.IsValid || !runtime.CurrentLineage.Matches(in lineage) || m_FrameOpen)
            {
                throw new ArgumentException(
                    "Pose native StateMachine source frame input is invalid.");
            }
            m_ParentRuntime = runtime;
            m_Lineage = lineage;
            m_FrameOpen = true;
            m_EvaluationPrepared = false;
            m_PageIndex = m_CommittedPageIndex < 0
                ? 0
                : 1 - m_CommittedPageIndex;
            CopyCommittedState();
            if (!m_PendingInitialized)
            {
                m_PendingState = m_Definition.Entry.TargetStateId;
                m_PendingInitialized = true;
                m_PendingTime = 0f;
            }
            else if (m_PendingTransition == null)
            {
                m_PendingTime = m_CommittedTime + input.DeltaSeconds;
                ref readonly CharacterPresentationFactFrame factFrame =
                    ref input.FactFrame;
                ref readonly CharacterAnimationPoseInputFrame parameterFrame =
                    ref input.ParameterFrame;
                CharacterPoseStateTransition transition = SelectTransition(
                    m_PendingState,
                    in factFrame,
                    in parameterFrame,
                    m_PendingTime);
                if (transition != null)
                {
                    if (transition.BlendLogic == AnimationTransitionBlendLogic.Inertialization)
                    {
                        m_PendingState = transition.TargetStateId;
                        m_PendingTime = 0f;
                        m_PendingTransition = null;
                        m_PendingTransitionElapsed = 0f;
                        m_ContinuityIdentity = AllocateContinuityIdentity();
                    }
                    else
                    {
                        m_PendingTransition = transition;
                        m_PendingTransitionElapsed = 0f;
                        m_ContinuityIdentity = AllocateContinuityIdentity();
                    }
                }
            }
            else
            {
                m_PendingTime = m_CommittedTime + input.DeltaSeconds;
                m_PendingTransitionElapsed =
                    m_CommittedTransitionElapsed + input.DeltaSeconds;
            }

            if (m_PendingTransition != null &&
                (m_PendingTransition.DurationSeconds <= 0f ||
                 m_PendingTransitionElapsed >= m_PendingTransition.DurationSeconds))
            {
                m_PendingState = m_PendingTransition.TargetStateId;
                m_PendingTime = m_PendingTransitionElapsed;
                m_PendingTransition = null;
                m_PendingTransitionElapsed = 0f;
                m_ContinuityIdentity = AllocateContinuityIdentity();
            }

            try
            {
                m_SourceRequests.Clear();
                CollectActiveStates();
                for (int activeIndex = 0; activeIndex < m_ActiveStateCount; activeIndex++)
                {
                    StateRuntime state = m_ActiveStates[activeIndex];
                    bool entering = state.Graph != null &&
                        (!m_CommittedInitialized ||
                         state.Definition.StateId != m_CommittedState &&
                         (m_CommittedTransition == null ||
                          state.Definition.StateId != m_CommittedTransition.TargetStateId));
                    EnsureState(runtime, state);
                    SynchronizeReset(runtime, state);
                    if (entering)
                        state.Graph.ResetForStateEntry();
                    state.Lease = state.Graph.BeginFrame(in input, lineage.CompletionIdentity);
                    state.Preparation = state.Graph.PrepareFrame(state.Lease);
                    if (!state.Preparation.IsValid ||
                        state.Preparation.Status != CharacterPoseNativeFrameStatus.Prepared)
                    {
                        throw new InvalidOperationException(
                            $"Pose StateMachine '{m_NodeId}' state '{state.Definition.StateId}' source preparation failed: {state.Preparation.Message}");
                    }
                    state.FrameOpen = true;
                    for (int i = 0; i < state.Preparation.Demand.Requests.Count; i++)
                        m_SourceRequests.Add(state.Preparation.Demand.Requests[i]);
                }
                return m_SourceRequests;
            }
            catch
            {
                DiscardChildFrames(CharacterPoseNativeFailureCode.SourcePending);
                m_FrameOpen = false;
                throw;
            }
        }

        public void PrepareEvaluation(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeSourceDemand demand,
            in CharacterPoseNativeFrameLineage lineage,
            ulong barrierIdentity)
        {
            RequireAlive();
            RequireFrame(runtime, in lineage);
            if (!demand.IsValid || demand.Lineage != lineage || barrierIdentity == 0)
                throw new ArgumentException(
                    "Pose native StateMachine source evaluation preparation is invalid.");
            SynchronizeTransition(m_ActiveStateCount);
            for (int activeIndex = 0; activeIndex < m_ActiveStateCount; activeIndex++)
            {
                StateRuntime state = m_ActiveStates[activeIndex];
                for (int requestIndex = 0;
                     requestIndex < state.Preparation.Demand.Requests.Count;
                     requestIndex++)
                {
                    CharacterPoseNativeSourceRequest expected =
                        state.Preparation.Demand.Requests[requestIndex];
                    if (!demand.Contains(in expected))
                        throw new InvalidOperationException(
                            $"Pose StateMachine '{m_NodeId}' child source request '{expected.NodeId}/{expected.SourceId}' was lost before the evaluation barrier.");
                }
                CharacterPoseNativeSourceDemand childDemand = state.Preparation.Demand;
                state.Graph.PrepareEvaluation(
                    state.Lease,
                    in childDemand,
                    barrierIdentity);
            }
            m_EvaluationPrepared = true;
        }

        public CharacterPoseNativeLocalPoseValue Evaluate(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            ulong barrierIdentity)
        {
            RequireAlive();
            RequireFrame(runtime, in lineage);
            if (!m_EvaluationPrepared || barrierIdentity == 0)
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' has no prepared native evaluation.");
            CharacterPoseNativeLocalPoseValue firstValue = null;
            CharacterPoseNativeLocalPoseValue secondValue = null;
            int valueCount = 0;
            for (int activeIndex = 0; activeIndex < m_ActiveStateCount; activeIndex++)
            {
                StateRuntime state = m_ActiveStates[activeIndex];
                CharacterPoseNativeSourceDemand demand = state.Preparation.Demand;
                state.Evaluation = state.Graph.Evaluate(
                    state.Lease,
                    in demand,
                    barrierIdentity);
                if (!state.Evaluation.IsValid ||
                    state.Evaluation.Status != CharacterPoseNativeFrameStatus.Evaluated ||
                    !(state.Evaluation.Output is CharacterPoseNativeLocalPoseValue value) ||
                    !value.Native.IsValid ||
                    value.Native.CompletionIdentity != lineage.CompletionIdentity)
                {
                    string detail = state.Evaluation.Status == CharacterPoseNativeFrameStatus.Evaluated
                        ? $"output={state.Evaluation.Output?.GetType().Name ?? "null"}, outputCompletion={(state.Evaluation.Output is CharacterPoseNativeLocalPoseValue v && v.Native.IsValid ? v.Native.CompletionIdentity.ToString() : "invalid")}, expectedCompletion={lineage.CompletionIdentity}"
                        : $"status={state.Evaluation.Status}, failure={state.Evaluation.FailureCode} ({state.Evaluation.Source}): {state.Evaluation.Message}";
                    throw new InvalidOperationException(
                        $"Pose StateMachine '{m_NodeId}' state '{state.Definition.StateId}' did not produce the current Local Pose ({detail}).");
                }
                if (valueCount == 0)
                    firstValue = value;
                else
                    secondValue = value;
                valueCount++;
            }
            if (valueCount == 1)
            {
                CopySingle(in firstValue);
            }
            else if (valueCount == 2)
            {
                BlendTransition(in firstValue, in secondValue);
            }
            else
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' has no active state output.");
            CharacterPoseNativePoseReadBinding output =
                new CharacterPoseNativePoseReadBinding(
                    (m_PageIndex == 0
                        ? m_OutputBuffer
                        : m_SecondaryOutputBuffer).RequireWriteBinding(
                            lineage.CompletionIdentity));
            m_Output = CharacterPoseNativeLocalPoseValue.Reuse(
                m_Output,
                m_NodeId,
                in output);
            return m_Output;
        }

        public void CommitFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            RequireFrame(runtime, in lineage);
            for (int activeIndex = 0; activeIndex < m_ActiveStateCount; activeIndex++)
            {
                StateRuntime state = m_ActiveStates[activeIndex];
                state.Graph.ValidatePending(state.Lease, in state.Evaluation);
                state.Graph.Commit(state.Lease, in state.Evaluation);
            }
            m_CommittedState = m_PendingState;
            m_CommittedTime = m_PendingTime;
            m_CommittedTransition = m_PendingTransition;
            m_CommittedTransitionElapsed = m_PendingTransitionElapsed;
            m_CommittedInitialized = m_PendingInitialized;
            m_CommittedContinuityIdentity = m_ContinuityIdentity;
            m_CommittedPageIndex = m_PageIndex;
            ClearChildFrames();
            m_FrameOpen = false;
            m_EvaluationPrepared = false;
        }

        public void DiscardFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativeFailureCode reason)
        {
            RequireAlive();
            if (!m_FrameOpen)
                return;
            DiscardChildFrames(reason);
            CopyCommittedState();
            m_FrameOpen = false;
            m_EvaluationPrepared = false;
        }

        public void ResetFrame()
        {
            RequireAlive();
            DiscardChildFrames(CharacterPoseNativeFailureCode.Stale);
            m_CommittedState = default;
            m_PendingState = default;
            m_CommittedTime = 0f;
            m_PendingTime = 0f;
            m_CommittedTransition = null;
            m_PendingTransition = null;
            m_CommittedTransitionElapsed = 0f;
            m_PendingTransitionElapsed = 0f;
            m_CommittedContinuityIdentity = 0;
            m_ContinuityIdentity = 0;
            m_CommittedPageIndex = -1;
            m_PageIndex = -1;
            m_CommittedInitialized = false;
            m_PendingInitialized = false;
            m_FrameOpen = false;
            m_EvaluationPrepared = false;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            Exception failure = null;
            foreach (StateRuntime state in m_States.Values)
            {
                try
                {
                    state.Graph?.Dispose();
                }
                catch (Exception exception)
                {
                    failure = failure == null
                        ? exception
                        : new AggregateException(failure, exception);
                }
            }
            m_States.Clear();
            try
            {
                m_SecondaryOutputBuffer.Dispose();
                m_OutputBuffer.Dispose();
            }
            catch (Exception exception)
            {
                failure = failure == null
                    ? exception
                    : new AggregateException(failure, exception);
            }
            if (failure != null)
                throw failure;
        }

        void CollectActiveStates()
        {
            StateRuntime current = RequireState(m_PendingState);
            m_ActiveStates[0] = current;
            m_ActiveStates[1] = null;
            if (m_PendingTransition != null)
            {
                StateRuntime target = RequireState(m_PendingTransition.TargetStateId);
                if (!ReferenceEquals(current, target))
                {
                    m_ActiveStates[1] = target;
                    m_ActiveStateCount = 2;
                    return;
                }
            }
            m_ActiveStateCount = 1;
        }

        StateRuntime RequireState(PoseStateId stateId)
        {
            if (!m_States.TryGetValue(stateId, out StateRuntime state))
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' has no state '{stateId}'.");
            return state;
        }

        void EnsureState(
            CharacterPoseNativeGraphRuntime parent,
            StateRuntime state)
        {
            if (state.Graph != null)
                return;
            ulong requestId = AllocateRequestId();
            ulong instanceId = AllocateChildInstanceId(parent.InstanceId, state.Definition.StateId.Value);
            CharacterPoseNativeAdoptedResult adopted = parent.CreateChild(
                requestId,
                state.Definition.PoseGraphId,
                CharacterPoseNativeGraphBoundary.State,
                instanceId,
                parent.ResetGeneration,
                $"state/{m_NodeId}/{state.Definition.StateId}",
                m_Factory,
                out CharacterPoseNativeGraphRuntime child);
            if (!adopted.IsAdopted || child == null)
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' state '{state.Definition.StateId}' could not create its child graph: {adopted.Message}");
            state.Graph = child;
            child.Graph.RequireNode(state.Definition.OutputPoseNodeId);
        }

        void SynchronizeReset(
            CharacterPoseNativeGraphRuntime parent,
            StateRuntime state)
        {
            if (state.Graph.ResetGeneration > parent.ResetGeneration)
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' child state '{state.Definition.StateId}' has a newer reset generation.");
            if (state.Graph.ResetGeneration == parent.ResetGeneration)
                return;
            CharacterPoseNativeResetResult reset = state.Graph.ResetInstance(
                parent.ResetGeneration);
            if (!reset.IsReset)
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' child state '{state.Definition.StateId}' reset failed: {reset.Message}");
        }

        void DiscardChildFrames(CharacterPoseNativeFailureCode reason)
        {
            foreach (StateRuntime state in m_States.Values)
            {
                if (!state.FrameOpen || state.Graph == null)
                    continue;
                try
                {
                    state.Graph.Discard(state.Lease, reason);
                }
                finally
                {
                    state.FrameOpen = false;
                    state.Lease = default;
                    state.Preparation = default;
                    state.Evaluation = default;
                }
            }
            m_ActiveStateCount = 0;
        }

        void ClearChildFrames()
        {
            foreach (StateRuntime state in m_States.Values)
            {
                state.FrameOpen = false;
                state.Lease = default;
                state.Preparation = default;
                state.Evaluation = default;
            }
            m_ActiveStateCount = 0;
        }

        void CopyCommittedState()
        {
            m_PendingState = m_CommittedState;
            m_PendingTime = m_CommittedTime;
            m_PendingTransition = m_CommittedTransition;
            m_PendingTransitionElapsed = m_CommittedTransitionElapsed;
            m_PendingInitialized = m_CommittedInitialized;
            m_ContinuityIdentity = m_CommittedContinuityIdentity;
        }

        void CopySingle(in CharacterPoseNativeLocalPoseValue value)
        {
            CharacterPoseNativePoseReadBinding input = value.Native;
            AnimationPlayerPoseNativeWriteBinding output =
                (m_PageIndex == 0
                    ? m_OutputBuffer
                    : m_SecondaryOutputBuffer).RequireWriteBinding(
                        input.CompletionIdentity);
            CharacterPoseNativePoseBufferCopy.CopyMetadata(in input, in output);
            NativeSlice<AnimationLocalBonePose> poses = output.DenseLocalPoses;
            poses.CopyFrom(input.DenseLocalPoses);
            output.ContinuityIdentity[0] =
                m_PendingTransition == null
                    ? input.ContinuityIdentity[0]
                    : m_ContinuityIdentity;
        }

        void BlendTransition(
            in CharacterPoseNativeLocalPoseValue source,
            in CharacterPoseNativeLocalPoseValue target)
        {
            CharacterPoseNativePoseReadBinding sourcePose = source.Native;
            CharacterPoseNativePoseReadBinding targetPose = target.Native;
            if (sourcePose.Space != CharacterPoseSpace.Local ||
                targetPose.Space != CharacterPoseSpace.Local ||
                sourcePose.DenseLocalPoses.Length != targetPose.DenseLocalPoses.Length)
            {
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' transition pose layouts are inconsistent.");
            }
            AnimationPlayerPoseNativeWriteBinding output =
                (m_PageIndex == 0
                    ? m_OutputBuffer
                    : m_SecondaryOutputBuffer).RequireWriteBinding(
                        sourcePose.CompletionIdentity);
            CharacterPoseNativePoseBufferCopy.ValidateLayout(
                in sourcePose,
                in output);
            float duration = m_PendingTransition?.DurationSeconds ?? 0f;
            float weight = duration <= 0f
                ? 1f
                : Mathf.Clamp01(m_PendingTransitionElapsed / duration);
            float sourceWeight = 1f - weight;
            float targetWeight = weight;
            float sourceOutputWeight = Mathf.Clamp01(sourcePose.OutputWeight[0]);
            float targetOutputWeight = Mathf.Clamp01(targetPose.OutputWeight[0]);
            float totalWeight = sourceWeight * sourceOutputWeight +
                                targetWeight * targetOutputWeight;
            if (!float.IsFinite(totalWeight) || totalWeight <= 0f)
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' transition has no visible output.");
            for (int bone = 0; bone < output.DenseLocalPoses.Length; bone++)
            {
                AnimationLocalBonePose sourceBone = sourcePose.DenseLocalPoses[bone];
                AnimationLocalBonePose targetBone = targetPose.DenseLocalPoses[bone];
                Vector3 position =
                    (sourceBone.Position * sourceWeight * sourceOutputWeight +
                     targetBone.Position * targetWeight * targetOutputWeight) /
                    totalWeight;
                Vector3 scale =
                    (sourceBone.Scale * sourceWeight * sourceOutputWeight +
                     targetBone.Scale * targetWeight * targetOutputWeight) /
                    totalWeight;
                Vector4 rotation =
                    new Vector4(
                        sourceBone.Rotation.x * sourceWeight * sourceOutputWeight,
                        sourceBone.Rotation.y * sourceWeight * sourceOutputWeight,
                        sourceBone.Rotation.z * sourceWeight * sourceOutputWeight,
                        sourceBone.Rotation.w * sourceWeight * sourceOutputWeight) +
                    AnimationPoseMath.AlignAndScale(
                        targetBone.Rotation,
                        sourceBone.Rotation,
                        targetWeight * targetOutputWeight);
                output.DenseLocalPoses[bone] =
                    AnimationPoseMath.BlendWeighted(
                        position * totalWeight,
                        rotation,
                        scale * totalWeight,
                        totalWeight,
                        sourceBone);
                AnimationBlendBoneVelocity sourceVelocity =
                    sourcePose.DenseVelocities[bone];
                AnimationBlendBoneVelocity targetVelocity =
                    targetPose.DenseVelocities[bone];
                output.DenseVelocities[bone] =
                    new AnimationBlendBoneVelocity(
                        (sourceVelocity.Linear * sourceWeight * sourceOutputWeight +
                         targetVelocity.Linear * targetWeight * targetOutputWeight) /
                        totalWeight,
                        (sourceVelocity.Angular * sourceWeight * sourceOutputWeight +
                         targetVelocity.Angular * targetWeight * targetOutputWeight) /
                        totalWeight,
                        (sourceVelocity.Scale * sourceWeight * sourceOutputWeight +
                         targetVelocity.Scale * targetWeight * targetOutputWeight) /
                        totalWeight);
            }
            BlendParameters(in sourcePose, in targetPose, sourceWeight, targetWeight, totalWeight, output);
            BlendContributions(in sourcePose, in targetPose, sourceWeight, targetWeight, totalWeight, output);
            BlendFeet(in sourcePose, in targetPose, sourceWeight * sourceOutputWeight, targetWeight * targetOutputWeight, output);
            output.OutputWeight[0] = Mathf.Clamp01(totalWeight);
            output.Availability[0] = AnimationPoseAvailability.Pose;
            output.ContinuityIdentity[0] = m_ContinuityIdentity;
            output.Discontinuity[0] = weight >= 0.5f
                ? targetPose.Discontinuity[0]
                : sourcePose.Discontinuity[0];
            output.InvalidReason[0] = AnimationPoseNativeInvalidReason.None;
            output.CompletedAt[0] = output.CompletionIdentity;
        }

        static void BlendParameters(
            in CharacterPoseNativePoseReadBinding source,
            in CharacterPoseNativePoseReadBinding target,
            float sourceWeight,
            float targetWeight,
            float totalWeight,
            AnimationPlayerPoseNativeWriteBinding output)
        {
            for (int i = 0; i < output.PoseParameters.Length; i++)
            {
                byte sourceAvailable = source.PoseParameterAvailability[i];
                byte targetAvailable = target.PoseParameterAvailability[i];
                if (sourceAvailable != 0 && targetAvailable != 0)
                {
                    output.PoseParameters[i] =
                        (source.PoseParameters[i] * sourceWeight +
                         target.PoseParameters[i] * targetWeight) /
                        totalWeight;
                    output.PoseParameterAvailability[i] = 1;
                }
                else if (targetAvailable != 0)
                {
                    output.PoseParameters[i] = target.PoseParameters[i];
                    output.PoseParameterAvailability[i] = 1;
                }
                else
                {
                    output.PoseParameters[i] = source.PoseParameters[i];
                    output.PoseParameterAvailability[i] = sourceAvailable;
                }
            }
        }

        static void BlendContributions(
            in CharacterPoseNativePoseReadBinding source,
            in CharacterPoseNativePoseReadBinding target,
            float sourceWeight,
            float targetWeight,
            float totalWeight,
            AnimationPlayerPoseNativeWriteBinding output)
        {
            int count = 0;
            AppendContributions(in source, sourceWeight / totalWeight, ref count, output);
            AppendContributions(in target, targetWeight / totalWeight, ref count, output);
            CharacterPoseNativePoseBufferCopy.CompleteContributions(in output, count);
        }

        static void AppendContributions(
            in CharacterPoseNativePoseReadBinding input,
            float factor,
            ref int count,
            AnimationPlayerPoseNativeWriteBinding output)
        {
            if (factor <= 0f)
                return;
            for (int i = 0; i < input.ContributionCount[0]; i++)
            {
                if (count >= output.Contributions.Length)
                    throw new InvalidOperationException(
                        "Pose StateMachine transition contribution capacity was exceeded.");
                AnimationPrimitivePoseContribution value = input.Contributions[i];
                CharacterPoseNativePoseBufferCopy.ExtendContributionPrefix(in output, count + 1);
                output.Contributions[count] = new AnimationPrimitivePoseContribution(
                    value.PhysicalPlayerIndex,
                    value.PhysicalSourceIndex,
                    value.PhysicalSourceGeneration,
                    value.Kind,
                    value.SourceOwnerIndex,
                    value.ContributionContinuityIdentity,
                    value.Weight * factor,
                    value.LeftFootWeight * factor,
                    value.RightFootWeight * factor);
                for (int bone = 0; bone < output.DenseLocalPoses.Length; bone++)
                    output.DenseContributionWeights[count * output.DenseLocalPoses.Length + bone] =
                        input.DenseContributionWeights[i * input.DenseLocalPoses.Length + bone] * factor;
                count++;
            }
        }

        static void BlendFeet(
            in CharacterPoseNativePoseReadBinding source,
            in CharacterPoseNativePoseReadBinding target,
            float sourceWeight,
            float targetWeight,
            AnimationPlayerPoseNativeWriteBinding output)
        {
            bool hasSource = source.HasFootFeatures[0] != 0;
            bool hasTarget = target.HasFootFeatures[0] != 0;
            if (hasSource && hasTarget && sourceWeight > 0f && targetWeight > 0f)
            {
                var left = new AnimationFootFeatureBlendAccumulator();
                var right = new AnimationFootFeatureBlendAccumulator();
                left.Add(source.LeftFootFeatures[0], sourceWeight);
                left.Add(target.LeftFootFeatures[0], targetWeight);
                right.Add(source.RightFootFeatures[0], sourceWeight);
                right.Add(target.RightFootFeatures[0], targetWeight);
                output.LeftFootFeatures[0] = left.Resolve();
                output.RightFootFeatures[0] = right.Resolve();
                output.HasFootFeatures[0] = 1;
            }
            else if (hasTarget && targetWeight > 0f)
            {
                output.LeftFootFeatures[0] = target.LeftFootFeatures[0];
                output.RightFootFeatures[0] = target.RightFootFeatures[0];
                output.HasFootFeatures[0] = 1;
            }
            else if (hasSource && sourceWeight > 0f)
            {
                output.LeftFootFeatures[0] = source.LeftFootFeatures[0];
                output.RightFootFeatures[0] = source.RightFootFeatures[0];
                output.HasFootFeatures[0] = 1;
            }
            else
            {
                output.LeftFootFeatures[0] = default;
                output.RightFootFeatures[0] = default;
                output.HasFootFeatures[0] = 0;
            }
        }

        CharacterPoseStateTransition SelectTransition(
            PoseStateId stateId,
            in CharacterPresentationFactFrame facts,
            in CharacterAnimationPoseInputFrame inputs,
            float timeInState)
        {
            CharacterPoseStateTransition[] candidates = m_TransitionsByState[stateId];
            for (int i = 0; i < candidates.Length; i++)
            {
                CharacterPoseStateTransition candidate = candidates[i];
                if (EvaluateRule(
                        candidate.Rule,
                        in facts,
                        in inputs,
                        timeInState,
                        ResolveRemainingTime(stateId, timeInState)))
                    return candidate;
            }
            return null;
        }

        Dictionary<PoseStateId, CharacterPoseStateTransition[]> BuildTransitionsByState()
        {
            var result =
                new Dictionary<PoseStateId, CharacterPoseStateTransition[]>(m_States.Count);
            foreach (PoseStateId stateId in m_States.Keys)
            {
                var candidates = new List<CharacterPoseStateTransition>();
                for (int i = 0; i < m_Definition.Transitions.Count; i++)
                {
                    CharacterPoseStateTransition transition =
                        m_Definition.Transitions[i];
                    if (transition != null && AppliesToState(transition.Source, stateId))
                        candidates.Add(transition);
                }
                candidates.Sort(CompareTransitions);
                result.Add(
                    stateId,
                    candidates.Count == 0
                        ? Array.Empty<CharacterPoseStateTransition>()
                        : candidates.ToArray());
            }
            return result;
        }

        static int CompareTransitions(
            CharacterPoseStateTransition left,
            CharacterPoseStateTransition right)
        {
            int priority = left.Priority.CompareTo(right.Priority);
            return priority != 0
                ? priority
                : string.Compare(
                    left.TransitionId.Value,
                    right.TransitionId.Value,
                    StringComparison.Ordinal);
        }

        bool AppliesToState(
            CharacterPoseStateTransitionSource source,
            PoseStateId stateId)
        {
            if (source == null)
                return false;
            if (source.Kind == PoseStateTransitionSourceKind.State)
                return source.StateId == stateId;
            CharacterPoseStateAlias alias = null;
            for (int i = 0; i < m_Definition.Aliases.Count; i++)
            {
                CharacterPoseStateAlias candidate = m_Definition.Aliases[i];
                if (candidate != null && candidate.AliasId == source.AliasId)
                {
                    alias = candidate;
                    break;
                }
            }
            if (alias == null)
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' transition references missing alias '{source.AliasId}'.");
            for (int i = 0; i < alias.Sources.Count; i++)
            {
                CharacterPoseStateTransitionSource candidate = alias.Sources[i];
                if (candidate != null &&
                    candidate.Kind == PoseStateTransitionSourceKind.State &&
                    candidate.StateId == stateId)
                {
                    return true;
                }
            }
            return false;
        }

        float ResolveRemainingTime(PoseStateId stateId, float timeInState)
        {
            if (!m_StateDurations.TryGetValue(stateId, out float duration))
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' has no duration for state '{stateId}'.");
            return Math.Max(0f, duration - timeInState);
        }

        bool EvaluateRule(
            CharacterPoseTransitionRuleGraph rule,
            in CharacterPresentationFactFrame facts,
            in CharacterAnimationPoseInputFrame inputs,
            float timeInState,
            float remainingTime)
        {
            if (rule == null || !rule.OutputOperationId.IsValid)
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' transition rule is invalid.");
            m_RuleValues.Clear();
            m_RuleVisiting.Clear();
            if (!m_RuleOperationTables.TryGetValue(
                    rule,
                    out Dictionary<PoseTransitionRuleOperationId,
                        BoundRuleOperation> operations))
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' transition rule was not prepared.");
            RuleValue result = EvaluateOperation(
                rule.OutputOperationId,
                operations,
                m_RuleValues,
                m_RuleVisiting,
                in facts,
                in inputs,
                timeInState,
                remainingTime);
            if (result.Kind != RuleValueKind.Bool)
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' transition rule output is not Bool.");
            return result.Bool;
        }

        RuleValue EvaluateOperation(
            PoseTransitionRuleOperationId id,
            IReadOnlyDictionary<PoseTransitionRuleOperationId, BoundRuleOperation> operations,
            IDictionary<PoseTransitionRuleOperationId, RuleValue> values,
            ISet<PoseTransitionRuleOperationId> visiting,
            in CharacterPresentationFactFrame facts,
            in CharacterAnimationPoseInputFrame inputs,
            float timeInState,
            float remainingTime)
        {
            if (values.TryGetValue(id, out RuleValue cached))
                return cached;
            if (!visiting.Add(id) || !operations.TryGetValue(id, out BoundRuleOperation bound))
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' transition rule references an invalid operation '{id}'.");
            CharacterPoseTransitionRuleOperation operation = bound.Definition;
            RuleValue result;
            switch (operation.Kind)
            {
                case PoseTransitionRuleOperationKind.FactInput:
                    result = FromFact(facts.Require(operation.FactId));
                    break;
                case PoseTransitionRuleOperationKind.AnimationVariableInput:
                    result = FromEvent(inputs.RequireValue(bound.Variable));
                    break;
                case PoseTransitionRuleOperationKind.BoolLiteral:
                    result = new RuleValue(operation.BoolLiteral);
                    break;
                case PoseTransitionRuleOperationKind.FloatLiteral:
                    result = new RuleValue(operation.FloatLiteral);
                    break;
                case PoseTransitionRuleOperationKind.EnumLiteral:
                    result = new RuleValue(operation.EnumLiteral);
                    break;
                case PoseTransitionRuleOperationKind.IdentityLiteral:
                    result = new RuleValue(operation.IdentityLiteral);
                    break;
                case PoseTransitionRuleOperationKind.TimeInState:
                    result = new RuleValue(timeInState);
                    break;
                case PoseTransitionRuleOperationKind.StatePoseRemainingTime:
                    result = new RuleValue(remainingTime);
                    break;
                case PoseTransitionRuleOperationKind.Not:
                    result = new RuleValue(!RequireBool(EvaluateOperation(
                        operation.InputA,
                        operations,
                        values,
                        visiting,
                        in facts,
                        in inputs,
                        timeInState,
                        remainingTime)));
                    break;
                case PoseTransitionRuleOperationKind.And:
                    result = new RuleValue(
                        RequireBool(EvaluateOperation(operation.InputA, operations, values, visiting, in facts, in inputs, timeInState, remainingTime)) &&
                        RequireBool(EvaluateOperation(operation.InputB, operations, values, visiting, in facts, in inputs, timeInState, remainingTime)));
                    break;
                case PoseTransitionRuleOperationKind.Or:
                    result = new RuleValue(
                        RequireBool(EvaluateOperation(operation.InputA, operations, values, visiting, in facts, in inputs, timeInState, remainingTime)) ||
                        RequireBool(EvaluateOperation(operation.InputB, operations, values, visiting, in facts, in inputs, timeInState, remainingTime)));
                    break;
                case PoseTransitionRuleOperationKind.Equal:
                    result = new RuleValue(Compare(
                        EvaluateOperation(operation.InputA, operations, values, visiting, in facts, in inputs, timeInState, remainingTime),
                        EvaluateOperation(operation.InputB, operations, values, visiting, in facts, in inputs, timeInState, remainingTime),
                        false));
                    break;
                case PoseTransitionRuleOperationKind.NotEqual:
                    result = new RuleValue(!Compare(
                        EvaluateOperation(operation.InputA, operations, values, visiting, in facts, in inputs, timeInState, remainingTime),
                        EvaluateOperation(operation.InputB, operations, values, visiting, in facts, in inputs, timeInState, remainingTime),
                        false));
                    break;
                case PoseTransitionRuleOperationKind.Greater:
                case PoseTransitionRuleOperationKind.GreaterOrEqual:
                case PoseTransitionRuleOperationKind.Less:
                case PoseTransitionRuleOperationKind.LessOrEqual:
                    result = new RuleValue(CompareNumbers(
                        EvaluateOperation(operation.InputA, operations, values, visiting, in facts, in inputs, timeInState, remainingTime),
                        EvaluateOperation(operation.InputB, operations, values, visiting, in facts, in inputs, timeInState, remainingTime),
                        operation.Kind));
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Pose StateMachine '{m_NodeId}' transition rule operation '{operation.Kind}' is unsupported.");
            }
            visiting.Remove(id);
            values.Add(id, result);
            return result;
        }

        static RuleValue FromFact(CharacterPresentationFactValue value) =>
            value.Kind switch
            {
                PresentationFactValueKind.Bool => new RuleValue(value.BoolValue),
                PresentationFactValueKind.Float => new RuleValue(value.FloatValue),
                PresentationFactValueKind.Enum => new RuleValue(value.EnumValue),
                PresentationFactValueKind.Identity => new RuleValue(value.IdentityValue),
                _ => throw new InvalidOperationException(
                    $"Presentation Fact kind '{value.Kind}' cannot be used by a Pose StateMachine rule.")
            };

        static RuleValue FromEvent(EventGraphValue value) =>
            value.Kind switch
            {
                EventGraphValueKind.Bool => new RuleValue(value.BoolValue),
                EventGraphValueKind.Int32 => new RuleValue(value.Int32Value),
                EventGraphValueKind.Float32 => new RuleValue(value.Float32Value),
                EventGraphValueKind.Enum => new RuleValue(value.EnumValue),
                _ => throw new InvalidOperationException(
                    $"EventGraph value kind '{value.Kind}' cannot be used by a Pose StateMachine rule.")
            };

        static bool RequireBool(RuleValue value) =>
            value.Kind == RuleValueKind.Bool
                ? value.Bool
                : throw new InvalidOperationException(
                    "Pose StateMachine rule Boolean operation received a non-Boolean value.");

        static bool Compare(RuleValue left, RuleValue right, bool numericOnly)
        {
            if (left.Kind != right.Kind)
            {
                if (left.Kind != RuleValueKind.Number || right.Kind != RuleValueKind.Number || numericOnly)
                    return false;
            }
            if (left.Kind == RuleValueKind.Number && right.Kind == RuleValueKind.Number)
                return left.Number == right.Number;
            if (left.Kind == RuleValueKind.Bool && right.Kind == RuleValueKind.Bool)
                return left.Bool == right.Bool;
            if (left.Kind == RuleValueKind.Identity && right.Kind == RuleValueKind.Identity)
                return string.Equals(left.Identity, right.Identity, StringComparison.Ordinal);
            return false;
        }

        static bool CompareNumbers(
            RuleValue left,
            RuleValue right,
            PoseTransitionRuleOperationKind kind)
        {
            if (left.Kind != RuleValueKind.Number || right.Kind != RuleValueKind.Number)
                throw new InvalidOperationException(
                    "Pose StateMachine numeric comparison received a non-numeric value.");
            return kind switch
            {
                PoseTransitionRuleOperationKind.Greater => left.Number > right.Number,
                PoseTransitionRuleOperationKind.GreaterOrEqual => left.Number >= right.Number,
                PoseTransitionRuleOperationKind.Less => left.Number < right.Number,
                PoseTransitionRuleOperationKind.LessOrEqual => left.Number <= right.Number,
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        void RequireDefinition()
        {
            if (!m_GraphAsset || m_Definition.Entry == null ||
                !m_Definition.Entry.TargetStateId.IsValid ||
                m_Definition.States.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' definition is incomplete.");
            }
            foreach (CharacterPoseStateDefinition definition in m_Definition.States)
            {
                if (definition == null || !definition.StateId.IsValid ||
                    !m_States.TryAdd(definition.StateId, new StateRuntime(definition)))
                {
                    throw new InvalidOperationException(
                        $"Pose StateMachine '{m_NodeId}' contains a missing or duplicate state.");
                }
                m_GraphAsset.RequireGraph(definition.PoseGraphId)
                    .RequireNode(definition.OutputPoseNodeId);
                m_StateDurations.Add(
                    definition.StateId,
                    ResolveStateDuration(definition.PoseGraphId));
            }
            if (!m_States.ContainsKey(m_Definition.Entry.TargetStateId))
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' entry targets an unknown state.");
        }

        float ResolveStateDuration(PoseGraphId graphId)
        {
            CharacterPoseCanvasGraph graph = m_GraphAsset.RequireGraph(graphId);
            float duration = 0f;
            bool hasFiniteDuration = false;
            foreach (CharacterPoseCanvasNode node in graph.Nodes)
            {
                if (node.Kind == CharacterPoseNodeKind.ClipPlayer)
                {
                    CharacterClipPoseSourceBinding binding =
                        m_Profile.FindPoseSourceBinding(node.PresentationPoseSourceSlot)
                            as CharacterClipPoseSourceBinding;
                    if (binding == null || !binding.Clip)
                        throw new InvalidOperationException(
                            $"Pose StateMachine '{m_NodeId}' state graph '{graphId}' Clip Player '{node.NodeId}' has no exact source binding.");
                    CharacterClipPlayerPosePayload payload =
                        node.RequirePayload<CharacterClipPlayerPosePayload>();
                    if (!payload.LoopAnimation)
                    {
                        duration = Math.Max(
                            duration,
                            Math.Max(0f, binding.Clip.length - payload.InitialTime) /
                            Mathf.Max(0.0001f, payload.PlayRate));
                        hasFiniteDuration = true;
                    }
                }
                else if (node.Kind == CharacterPoseNodeKind.BlendSpacePlayer)
                {
                    CharacterBlendSpacePoseSourceBinding binding =
                        m_Profile.FindPoseSourceBinding(node.PresentationPoseSourceSlot)
                            as CharacterBlendSpacePoseSourceBinding;
                    if (binding == null || !binding.BlendSpace)
                        throw new InvalidOperationException(
                            $"Pose StateMachine '{m_NodeId}' state graph '{graphId}' Blend Space Player '{node.NodeId}' has no exact source binding.");
                    foreach (CharacterAnimationBlendSpaceSample sample in binding.BlendSpace.Samples)
                    {
                        if (sample?.Clip && sample.Role != CharacterAnimationBlendSpaceSampleRole.DynamicCycle)
                        {
                            duration = Math.Max(duration, sample.Clip.length);
                            hasFiniteDuration = true;
                        }
                    }
                }
            }
            return hasFiniteDuration ? duration : 0f;
        }

        ulong AllocateRequestId()
        {
            if (m_NextRequestId == ulong.MaxValue)
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' child request identity was exhausted.");
            return m_NextRequestId++;
        }

        ulong AllocateChildInstanceId(ulong parentInstanceId, string stateId)
        {
            if (string.IsNullOrWhiteSpace(stateId))
                throw new ArgumentNullException(nameof(stateId));
            if (m_NextChildInstanceId == ulong.MaxValue)
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' child instance identity was exhausted.");
            string stableHash = StableHash.Compute(
                parentInstanceId.ToString(CultureInfo.InvariantCulture),
                stateId,
                m_NextChildInstanceId.ToString(CultureInfo.InvariantCulture)).Value;
            m_NextChildInstanceId++;
            ulong child = Convert.ToUInt64(stableHash.Substring(0, 16), 16);
            if (child == 0 || child == parentInstanceId)
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' child instance identity for state '{stateId}' collided with its parent instance.");
            return child;
        }

        ulong AllocateContinuityIdentity()
        {
            if (m_NextContinuityIdentity == ulong.MaxValue)
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' continuity identity was exhausted.");
            return m_NextContinuityIdentity++;
        }

        void RequireFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage)
        {
            if (!m_FrameOpen || runtime == null || !ReferenceEquals(runtime, m_ParentRuntime) ||
                lineage != m_Lineage)
            {
                throw new InvalidOperationException(
                    $"Pose StateMachine '{m_NodeId}' native frame is stale.");
            }
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeStateMachineSource));
        }
    }
}
