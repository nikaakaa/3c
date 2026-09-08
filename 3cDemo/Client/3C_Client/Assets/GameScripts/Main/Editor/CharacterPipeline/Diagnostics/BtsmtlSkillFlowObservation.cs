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
    public sealed class BtsmtlSkillFlowObservation : IGraphEditorObservation, IDisposable
    {
        readonly FlowGraph m_Graph;
        readonly string m_GraphId;
        readonly RuntimeDebugSession m_Session;
        readonly RuntimeDebugViewBinding m_Binding = new(RuntimeDebugViewKind.Graph);
        readonly HashSet<string> m_NodeIds;
        readonly HashSet<string> m_EdgeIds;
        readonly Dictionary<string, RuntimeNodeExecutionObservation> m_Nodes = new(StringComparer.Ordinal);
        readonly Dictionary<string, RuntimeElementDebugState> m_Edges = new(StringComparer.Ordinal);
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
            m_Binding.Configure(request);
            m_Binding.Pin(instance);
            graph.editorObservation = this;
            m_Session.Changed += OnChanged;
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        public RuntimeInstanceKey Instance => m_Binding.SelectedInstance;
        public string StatusMessage => m_Binding.StatusMessage;

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
                return string.IsNullOrEmpty(StatusMessage) ? "尚无执行记录" : "观察未绑定";
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

        public string GetPortText(string nodeId, string portId) =>
            string.IsNullOrEmpty(StatusMessage) ? "此端口尚无值采集记录" : StatusMessage;

        public string GetConnectionText(string connectionId) =>
            m_Edges.TryGetValue(connectionId, out RuntimeElementDebugState state)
                ? $"{state.Kind} · Tick {state.Position}"
                : null;

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
            RuntimeDebugTargetResolution resolution = m_Binding.Refresh(m_Session,
                RuntimeTraceChannel.Graph | RuntimeTraceChannel.StateMachine);
            RuntimeDebugViewModel view = m_Session.ViewModel;
            if (!resolution.CanReadSnapshot || !view.Valid)
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
        }
    }
}
