using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using FlowCanvas;
using NodeCanvas.Editor;
using NodeCanvas.Framework;
using ThirdPersonCharacter.Control.Authoring;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class BtsmtlSkillFlowObservation : IGraphEditorObservation, IBtsmtlSkillObservationControls, IDisposable
    {
        const double ConnectionPulseSeconds = 0.5d;
        const double RepaintIntervalSeconds = 1d / 30d;

        readonly FlowGraph m_Graph;
        readonly string m_GraphId;
        readonly RuntimeDebugSession m_Session;
        readonly RuntimeDebugViewBinding m_Binding = new(RuntimeDebugViewKind.Graph);
        readonly Func<RuntimeDebugViewModel, RuntimeInstanceKey> m_SelectInstance;
        readonly Guid m_CharacterRuntimeId;
        readonly HashSet<string> m_NodeIds;
        readonly HashSet<string> m_EdgeIds;
        readonly List<RuntimeDebugEventView> m_ObservationEvents = new List<RuntimeDebugEventView>();
        readonly List<RuntimeNodeExecutionObservation> m_ExecutionStates = new List<RuntimeNodeExecutionObservation>();
        readonly Dictionary<string, RuntimeNodeExecutionObservation> m_Nodes = new(StringComparer.Ordinal);
        readonly Dictionary<string, RuntimeElementDebugState> m_Edges = new(StringComparer.Ordinal);
        readonly Dictionary<string, (ulong Sequence, double Until)> m_ConnectionPulses = new(StringComparer.Ordinal);
        readonly HashSet<(string Node, string Port)> m_ValuePortIds;
        readonly Dictionary<(string Node, string Port), RuntimeDebugEventView> m_Values = new();
        bool m_CaptureValues;
        bool m_CanReadSnapshot;
        bool m_SamplingLimited;
        bool m_CoverageGap;
        Func<bool> m_CanNavigateParent;
        Action m_NavigateParent;
        Action m_SelectExecution;
        Action<BtsmtlSkillTimelineFlowNode> m_TimelineOpening;
        bool m_Dirty = true;
        bool m_Disposed;
        RuntimeInstanceKey m_PulseInstance;
        double m_PulseUntil;
        double m_NextRepaintTime;
        bool m_RepaintPending;

        public BtsmtlSkillFlowObservation(FlowGraph graph, RuntimeDebugSession session,
            RuntimeDebugTargetRequest request, RuntimeInstanceKey instance)
            : this(graph, session, request, instance, instance.CharacterRuntimeId, null) { }

        internal static BtsmtlSkillFlowObservation ForScope(FlowGraph graph, RuntimeDebugSession session,
            RuntimeDebugTargetRequest request, Guid characterRuntimeId, Func<RuntimeDebugViewModel, RuntimeInstanceKey> select) =>
            new(graph, session, request, default, characterRuntimeId, select);

        BtsmtlSkillFlowObservation(FlowGraph graph, RuntimeDebugSession session, RuntimeDebugTargetRequest request,
            RuntimeInstanceKey instance, Guid characterRuntimeId, Func<RuntimeDebugViewModel, RuntimeInstanceKey> select)
        {
            if (graph is not IBtsmtlSkillFlowGraph authoring || !request.IsValid ||
                !request.Source.Equals(RuntimeSourceElementKey.Graph(authoring.AuthoringId)))
                throw new ArgumentException("技能观察必须使用当前正式图及其作者版本。");
            if (!instance.IsValid && (select == null || characterRuntimeId == Guid.Empty))
                throw new InvalidOperationException("技能观察必须绑定正式诊断中的明确执行实例。");
            if (graph.editorObservation != null)
                throw new InvalidOperationException("当前图已有观察绑定，请先释放原绑定。");
            m_Graph = graph;
            m_GraphId = authoring.AuthoringId;
            m_Session = session ?? throw new ArgumentNullException(nameof(session));
            m_CharacterRuntimeId = characterRuntimeId;
            m_SelectInstance = select;
            m_NodeIds = graph.allNodes.Select(node => node.UID).ToHashSet(StringComparer.Ordinal);
            m_EdgeIds = graph.allNodes.SelectMany(node => node.outConnections).Select(edge => edge.UID).ToHashSet(StringComparer.Ordinal);
            m_ValuePortIds = graph.allNodes.Cast<FlowNode>().SelectMany(node =>
                node.GetInputValuePorts().Cast<Port>().Concat(node.GetOutputValuePorts())
                    .Select(port => (node.UID, port.ID))).ToHashSet();
            m_Binding.Configure(request);
            if (instance.IsValid)
                m_Binding.Pin(instance);
            else
                m_Binding.AwaitInstance(characterRuntimeId);
            graph.editorObservation = this;
            m_Session.Changed += OnChanged;
            EditorApplication.update += Update;
        }

        public RuntimeInstanceKey Instance => m_Binding.SelectedInstance;
        public string InvocationLabel => Instance.IsValid
            ? $"技能 {Instance.ActionInstanceId} · 释放 {Instance.ActivationGeneration} · 调用 {Instance.InvocationGeneration}"
            : "等待该调用执行";
        public bool CanNavigateParent => m_CanNavigateParent?.Invoke() == true;
        public void NavigateParent() => m_NavigateParent?.Invoke();
        public void SelectInstance() => m_SelectExecution?.Invoke();
        internal void SetInstanceSelection(Action select) => m_SelectExecution = select;
        public void NotifyTimelineOpening(BtsmtlSkillTimelineFlowNode node) => m_TimelineOpening?.Invoke(node);
        internal void SetParentNavigation(Func<bool> canNavigate, Action navigate, Action<BtsmtlSkillTimelineFlowNode> timelineOpening)
        {
            m_CanNavigateParent = canNavigate;
            m_NavigateParent = navigate;
            m_TimelineOpening = timelineOpening;
        }
        public string StatusMessage => m_SamplingLimited || m_CoverageGap
            ? $"{m_Binding.StatusMessage} · 该角色的诊断记录不完整"
            : m_Binding.StatusMessage;
        public bool CaptureValues
        {
            get => m_CaptureValues;
            set
            {
                if (m_CaptureValues == value)
                    return;
                m_CaptureValues = value;
                m_SamplingLimited = false;
                m_Values.Clear();
                m_Dirty = true;
            }
        }

        public Status GetNodeStatus(string nodeId)
        {
            if (!m_Nodes.TryGetValue(nodeId, out RuntimeNodeExecutionObservation state))
                return Status.Resting;
            return state.Phase switch
            {
                RuntimeNodeExecutionPhase.Running or RuntimeNodeExecutionPhase.Waiting or RuntimeNodeExecutionPhase.Stopping => Status.Running,
                RuntimeNodeExecutionPhase.Succeeded => Status.Success,
                RuntimeNodeExecutionPhase.Failed => Status.Failure,
                _ => Status.Resting
            };
        }

        public string GetNodeText(string nodeId)
        {
            if (!m_Nodes.TryGetValue(nodeId, out RuntimeNodeExecutionObservation state))
                return m_CoverageGap ? "无保留状态；部分诊断记录已覆盖" : m_CanReadSnapshot ? "尚无执行记录" : "观察未绑定";
            string label = state.Phase switch
            {
                RuntimeNodeExecutionPhase.Running => "执行中",
                RuntimeNodeExecutionPhase.Waiting => WaitingLabel(state.Event.Event.Payload.Detail),
                RuntimeNodeExecutionPhase.Succeeded => "成功完成",
                RuntimeNodeExecutionPhase.Failed => "执行失败",
                RuntimeNodeExecutionPhase.Stopping => "正在停止",
                RuntimeNodeExecutionPhase.Stopped => "已停止",
                RuntimeNodeExecutionPhase.ForceStopped => "已强制停止",
                _ => throw new ArgumentOutOfRangeException()
            };
            string positionName = state.Event.Event.Domain == RuntimeTraceDomain.Presentation ? "表现帧" : "Tick";
            return $"{label} · {positionName} {state.Event.Event.Position}";
        }

        static string WaitingLabel(string reason) => reason switch
        {
            "ChildCompletion" => "等待子步骤完成",
            "ChildStop" => "等待子步骤停止",
            "SubgraphCompletion" => "等待子图完成",
            "PriorityReplacement" => "等待切换优先分支",
            "ParallelCompletion" => "等待并行分支完成",
            "ParallelStop" => "等待并行分支停止",
            "NextIteration" => "等待下一轮更新",
            "StateEnter" => "等待状态进入流程完成",
            "StateExit" => "等待当前状态退出",
            _ => $"等待原因未识别：{reason}"
        };

        public Status GetConnectionStatus(string connectionId)
        {
            return m_CanReadSnapshot && m_ConnectionPulses.TryGetValue(connectionId, out var pulse) &&
                   pulse.Until > EditorApplication.timeSinceStartup ? Status.Running : Status.Resting;
        }

        public string GetPortText(string nodeId, string portId)
        {
            if (!m_CaptureValues)
                return "端口值采集已关闭";
            if (m_Values.TryGetValue((nodeId, portId), out RuntimeDebugEventView sample))
            {
                string positionName = sample.Event.Domain == RuntimeTraceDomain.Presentation ? "表现帧" : "Tick";
                return $"{sample.Event.Payload.Value.DisplayValue()} · {positionName} {sample.Event.Position}";
            }
            return !m_CanReadSnapshot ? StatusMessage : m_SamplingLimited || m_CoverageGap
                ? "此端口没有保留的采样；该角色的诊断存在采样缺口"
                : "此端口尚无值采集记录";
        }

        public string GetConnectionText(string connectionId)
        {
            if (!m_Edges.TryGetValue(connectionId, out RuntimeElementDebugState state))
                return m_CoverageGap ? "无保留记录；部分诊断记录已覆盖" : null;
            string label = state.Kind is RuntimeTraceEventKind.EdgeEvaluated or RuntimeTraceEventKind.StateTransitionEvaluated
                ? state.Payload.Flag ? "条件通过" : "条件未通过"
                : state.Payload.Name == "Value" ? "值已读取" : "已选中经过";
            string positionName = state.Domain == RuntimeTraceDomain.Presentation ? "表现帧" : "Tick";
            return $"{label} · {positionName} {state.Position}";
        }

        void OnChanged() => m_Dirty = true;

        void Update()
        {
            if (m_Disposed)
                return;
            if (m_Graph == null || !m_Session.ViewModel.Attached || GraphEditor.current == null || GraphEditor.currentGraph != m_Graph)
            {
                Dispose();
                return;
            }
            double now = EditorApplication.timeSinceStartup;
            if (!m_Dirty)
            {
                RepaintIfNeeded(now);
                return;
            }
            m_Dirty = false;
            m_Nodes.Clear();
            m_Edges.Clear();
            m_Values.Clear();
            m_SamplingLimited = false;
            if (m_SelectInstance != null)
            {
                RuntimeInstanceKey selected = m_SelectInstance(m_Session.ViewModel);
                if (selected.IsValid)
                    m_Binding.Pin(selected);
                else
                    m_Binding.AwaitInstance(m_CharacterRuntimeId);
            }
            RuntimeDebugTargetResolution resolution = m_Binding.Refresh(m_Session,
                RuntimeTraceChannel.Graph | RuntimeTraceChannel.StateMachine |
                (m_CaptureValues ? RuntimeTraceChannel.Values : RuntimeTraceChannel.None));
            RuntimeDebugViewModel view = m_Session.ViewModel;
            m_CoverageGap = view.HasCoverageGap;
            m_CanReadSnapshot = m_Binding.CanReadSelectedInstance && view.Valid;
            if (!m_CanReadSnapshot)
            {
                ClearConnectionPulses();
                m_RepaintPending = true;
                RepaintIfNeeded(now);
                return;
            }
            if (!m_PulseInstance.Equals(Instance))
            {
                ClearConnectionPulses();
                m_PulseInstance = Instance;
            }
            view.CopyCurrentEvents(m_GraphId, Instance,
                RuntimeTraceChannel.Graph | RuntimeTraceChannel.StateMachine, m_ObservationEvents);
            view.CopyGraphExecutionStates(m_ObservationEvents, m_ExecutionStates);
            for (int i = 0; i < m_ExecutionStates.Count; i++)
            {
                RuntimeNodeExecutionObservation state = m_ExecutionStates[i];
                if (m_NodeIds.Contains(state.Event.Source.ElementAuthoringId))
                    m_Nodes[state.Event.Source.ElementAuthoringId] = state;
            }
            for (int i = 0; i < m_ObservationEvents.Count; i++)
            {
                RuntimeDebugEventView item = m_ObservationEvents[i];
                if (item.Event.Kind == RuntimeTraceEventKind.TraceSamplingLimited)
                    m_SamplingLimited = true;
                if (item.Source.Kind != RuntimeSourceElementKind.Edge ||
                    !m_EdgeIds.Contains(item.Source.ElementAuthoringId))
                    continue;
                if (view.TryGetState(item.Source, Instance, out RuntimeElementDebugState state))
                    m_Edges[item.Source.ElementAuthoringId] = state;
                ulong latestPosition = item.Event.Domain == RuntimeTraceDomain.Presentation
                    ? view.LatestPresentationFrame : view.LatestLogicTick;
                if (item.Event.Kind is not (RuntimeTraceEventKind.EdgeSelected or RuntimeTraceEventKind.StateTransitionSelected) ||
                    item.Event.Position > latestPosition ||
                    latestPosition - item.Event.Position > 1)
                    continue;
                string edgeId = item.Source.ElementAuthoringId;
                if (m_ConnectionPulses.TryGetValue(edgeId, out var pulse) && pulse.Sequence == item.Event.Sequence)
                    continue;
                double until = now + ConnectionPulseSeconds;
                m_ConnectionPulses[edgeId] = (item.Event.Sequence, until);
                m_PulseUntil = Math.Max(m_PulseUntil, until);
            }
            if (m_CaptureValues)
            {
                view.CopyCurrentEvents(m_GraphId, Instance, RuntimeTraceChannel.Values, m_ObservationEvents);
                for (int i = 0; i < m_ObservationEvents.Count; i++)
                {
                    RuntimeDebugEventView sample = m_ObservationEvents[i];
                    if (sample.Event.Kind is RuntimeTraceEventKind.ValueSamplingLimited or RuntimeTraceEventKind.TraceSamplingLimited)
                    {
                        m_SamplingLimited = true;
                        continue;
                    }
                    var key = (sample.Source.ElementAuthoringId, sample.Source.PortAuthoringId);
                    if (sample.Source.Kind == RuntimeSourceElementKind.Port &&
                        sample.Event.Kind == RuntimeTraceEventKind.ValueSampled && m_ValuePortIds.Contains(key) &&
                        (!m_Values.TryGetValue(key, out RuntimeDebugEventView previous) || sample.Event.Sequence > previous.Event.Sequence))
                        m_Values[key] = sample;
                }
            }
            m_RepaintPending = true;
            RepaintIfNeeded(now);
        }

        void RepaintIfNeeded(double now)
        {
            if (m_PulseUntil != 0d && now >= m_PulseUntil)
            {
                m_PulseUntil = 0d;
                m_RepaintPending = true;
            }
            if ((!m_RepaintPending && m_PulseUntil <= now) || now < m_NextRepaintTime)
                return;
            GraphEditor.current?.Repaint();
            m_RepaintPending = false;
            m_NextRepaintTime = now + RepaintIntervalSeconds;
        }

        void ClearConnectionPulses()
        {
            m_ConnectionPulses.Clear();
            m_PulseInstance = default;
            m_PulseUntil = 0d;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Session.Changed -= OnChanged;
            EditorApplication.update -= Update;
            m_Binding.Dispose(m_Session);
            if (m_Graph != null && ReferenceEquals(m_Graph.editorObservation, this))
                m_Graph.editorObservation = null;
            m_Nodes.Clear();
            m_Edges.Clear();
            m_ObservationEvents.Clear();
            ClearConnectionPulses();
            m_Values.Clear();
        }
    }
}
