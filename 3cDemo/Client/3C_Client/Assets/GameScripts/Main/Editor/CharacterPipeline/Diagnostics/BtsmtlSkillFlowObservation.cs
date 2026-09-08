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
        readonly FlowGraph m_Graph;
        readonly string m_GraphId;
        readonly RuntimeDebugSession m_Session;
        readonly RuntimeDebugViewBinding m_Binding = new(RuntimeDebugViewKind.Graph);
        readonly HashSet<string> m_NodeIds;
        readonly HashSet<string> m_EdgeIds;
        readonly Dictionary<string, RuntimeNodeExecutionObservation> m_Nodes = new(StringComparer.Ordinal);
        readonly Dictionary<string, RuntimeElementDebugState> m_Edges = new(StringComparer.Ordinal);
        readonly HashSet<(string Node, string Port)> m_ValuePortIds;
        readonly Dictionary<(string Node, string Port), RuntimeDebugEventView> m_Values = new();
        bool m_CaptureValues;
        bool m_CanReadSnapshot;
        bool m_ValueSamplingLimited;
        bool m_CoverageGap;
        bool m_Dirty = true;
        bool m_Disposed;
        ulong m_LatestLogicTick;

        public BtsmtlSkillFlowObservation(FlowGraph graph, RuntimeDebugSession session,
            RuntimeDebugTargetRequest request, RuntimeInstanceKey instance)
        {
            if (graph is not IBtsmtlSkillFlowGraph authoring || !request.IsValid ||
                !request.Source.Equals(RuntimeSourceElementKey.Graph(authoring.AuthoringId)))
                throw new ArgumentException("技能观察必须使用当前正式图及其作者版本。");
            if (!Application.isPlaying || !instance.IsValid)
                throw new InvalidOperationException("技能观察必须绑定Play中的明确执行实例。");
            if (graph.editorObservation != null)
                throw new InvalidOperationException("当前图已有观察绑定，请先释放原绑定。");
            m_Graph = graph;
            m_GraphId = authoring.AuthoringId;
            m_Session = session ?? throw new ArgumentNullException(nameof(session));
            m_NodeIds = graph.allNodes.Select(node => node.UID).ToHashSet(StringComparer.Ordinal);
            m_EdgeIds = graph.allNodes.SelectMany(node => node.outConnections).Select(edge => edge.UID).ToHashSet(StringComparer.Ordinal);
            m_ValuePortIds = graph.allNodes.Cast<FlowNode>().SelectMany(node =>
                node.GetInputValuePorts().Cast<Port>().Concat(node.GetOutputValuePorts())
                    .Select(port => (node.UID, port.ID))).ToHashSet();
            m_Binding.Configure(request);
            m_Binding.Pin(instance);
            graph.editorObservation = this;
            m_Session.Changed += OnChanged;
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        public RuntimeInstanceKey Instance => m_Binding.SelectedInstance;
        public string StatusMessage => m_ValueSamplingLimited || m_CoverageGap
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
                m_ValueSamplingLimited = false;
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
                RuntimeNodeExecutionPhase.Running or RuntimeNodeExecutionPhase.Stopping => Status.Running,
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
                RuntimeNodeExecutionPhase.Succeeded => "成功完成",
                RuntimeNodeExecutionPhase.Failed => "执行失败",
                RuntimeNodeExecutionPhase.Stopping => "正在停止",
                RuntimeNodeExecutionPhase.Stopped => "已停止",
                RuntimeNodeExecutionPhase.ForceStopped => "已强制停止",
                _ => throw new ArgumentOutOfRangeException()
            };
            return $"{label} · Tick {state.Event.Event.Position}";
        }

        public Status GetConnectionStatus(string connectionId)
        {
            if (!m_Edges.TryGetValue(connectionId, out RuntimeElementDebugState state) || state.Position != m_LatestLogicTick)
                return Status.Resting;
            return state.Kind is RuntimeTraceEventKind.EdgeSelected or RuntimeTraceEventKind.StateTransitionSelected
                ? Status.Running
                : Status.Resting;
        }

        public string GetPortText(string nodeId, string portId)
        {
            if (!m_CaptureValues)
                return "端口值采集已关闭";
            if (m_Values.TryGetValue((nodeId, portId), out RuntimeDebugEventView sample))
                return $"{sample.Event.Payload.Value.DisplayValue()} · Tick {sample.Event.Position}";
            return !m_CanReadSnapshot ? StatusMessage : m_ValueSamplingLimited || m_CoverageGap
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
            return $"{label} · Tick {state.Position}";
        }

        void OnChanged() => m_Dirty = true;

        void Update()
        {
            if (m_Graph == null || !Application.isPlaying || GraphEditor.current == null || GraphEditor.currentGraph != m_Graph)
            {
                Dispose();
                return;
            }
            if (!m_Dirty)
                return;
            m_Dirty = false;
            m_Nodes.Clear();
            m_Edges.Clear();
            m_Values.Clear();
            m_ValueSamplingLimited = false;
            RuntimeDebugTargetResolution resolution = m_Binding.Refresh(m_Session,
                RuntimeTraceChannel.Graph | RuntimeTraceChannel.StateMachine |
                (m_CaptureValues ? RuntimeTraceChannel.Values : RuntimeTraceChannel.None));
            RuntimeDebugViewModel view = m_Session.ViewModel;
            m_CoverageGap = view.HasCoverageGap;
            m_CanReadSnapshot = resolution.CanReadSnapshot && view.Valid;
            if (!m_CanReadSnapshot)
            {
                GraphEditor.current?.Repaint();
                return;
            }
            m_LatestLogicTick = view.LatestLogicTick;
            foreach (RuntimeNodeExecutionObservation state in view.GetGraphExecutionStates(m_GraphId, Instance))
                if (m_NodeIds.Contains(state.Event.Source.ElementAuthoringId))
                    m_Nodes[state.Event.Source.ElementAuthoringId] = state;
            foreach (RuntimeElementDebugState state in view.GetGraphStates(m_GraphId, Instance, false))
                if (state.Source.Kind == RuntimeSourceElementKind.Edge && m_EdgeIds.Contains(state.Source.ElementAuthoringId))
                    m_Edges[state.Source.ElementAuthoringId] = state;
            if (m_CaptureValues)
                foreach (RuntimeDebugEventView sample in view.GetCurrentEvents(RuntimeTraceChannel.Values))
                {
                    if (sample.Event.Kind == RuntimeTraceEventKind.ValueSamplingLimited)
                    {
                        m_ValueSamplingLimited = true;
                        continue;
                    }
                    var key = (sample.Source.ElementAuthoringId, sample.Source.PortAuthoringId);
                    if (sample.Event.RuntimeInstance.Equals(Instance) && sample.Source.GraphAuthoringId == m_GraphId && sample.Source.Kind == RuntimeSourceElementKind.Port &&
                        sample.Event.Kind == RuntimeTraceEventKind.ValueSampled && m_ValuePortIds.Contains(key))
                        m_Values.TryAdd(key, sample);
                }
            GraphEditor.current?.Repaint();
        }

        void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                Dispose();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Session.Changed -= OnChanged;
            EditorApplication.update -= Update;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            m_Binding.Dispose(m_Session);
            if (m_Graph != null && ReferenceEquals(m_Graph.editorObservation, this))
                m_Graph.editorObservation = null;
            m_Nodes.Clear();
            m_Edges.Clear();
            m_Values.Clear();
        }
    }
}
