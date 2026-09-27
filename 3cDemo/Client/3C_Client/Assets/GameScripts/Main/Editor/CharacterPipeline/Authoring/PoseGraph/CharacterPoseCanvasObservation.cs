using System;
using System.Collections.Generic;
using NodeCanvas.Framework;
using ParadoxNotion;
using BTSMTL.Authoring.Graph;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterPoseCanvasObservation : IGraphEditorObservation
    {
        IReadOnlyDictionary<string, GraphAuthoringRuntimeTraceProjection> m_Nodes;
        IReadOnlyDictionary<string, string> m_Ports;
        readonly HashSet<string> m_Active = new HashSet<string>(StringComparer.Ordinal);
        string m_ConnectionText;

        internal void Update(IReadOnlyDictionary<string, GraphAuthoringRuntimeTraceProjection> nodes,
            IReadOnlyDictionary<string, string> ports, ISet<string> active, string connectionText)
        {
            m_Nodes = nodes;
            m_Ports = ports;
            m_Active.Clear();
            m_Active.UnionWith(active);
            m_ConnectionText = connectionText;
        }

        public Status GetNodeStatus(string nodeId)
        {
            if (m_Nodes == null || !m_Nodes.TryGetValue(nodeId, out var trace))
                return m_Active.Contains(nodeId) ? Status.Running : Status.Resting;
            return trace.Status switch
            {
                "完成 · 有贡献" => Status.Success,
                "已求值" => Status.Success,
                "不可用" => Status.Error,
                _ => Status.Resting
            };
        }

        public string GetNodeText(string nodeId) => m_Nodes != null && m_Nodes.TryGetValue(nodeId, out var trace)
            ? trace.Status == "已求值" ? trace.Detail : trace.Status : m_Active.Contains(nodeId) ? "当前状态" : null;
        public Status GetConnectionStatus(string connectionId) => m_Active.Contains(connectionId) ? Status.Running : Status.Resting;
        public string GetConnectionText(string connectionId) => m_Active.Contains(connectionId) ? m_ConnectionText : null;
        public string GetPortText(string nodeId, string portId) => m_Ports != null && m_Ports.TryGetValue(nodeId + "\0" + portId, out string text)
            ? text : "未采集端口值或读取记录";
    }
}
