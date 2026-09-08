using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NodeCanvas.Editor;
using NodeCanvas.Framework;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterPoseCanvasPortsGUI
    {
        readonly struct CanvasPort
        {
            internal readonly string Id;
            internal readonly string Name;
            internal readonly string Kind;
            internal readonly CharacterPosePortDirection Direction;
            internal CanvasPort(string id, string name, string kind, CharacterPosePortDirection direction)
            {
                Id = id;
                Name = name;
                Kind = kind;
                Direction = direction;
            }
        }

        sealed class NodePorts
        {
            internal object Payload;
            internal object DynamicPorts;
            internal CanvasPort[] Inputs;
            internal CanvasPort[] Outputs;
            internal Rect[] InputRects;
            internal Rect[] OutputRects;
        }

        static readonly ConditionalWeakTable<Node, NodePorts> s_Ports = new();
        static readonly ConditionalWeakTable<NodeCanvas.Framework.Graph, HashSet<string>> s_ActiveNodes = new();
        static Node s_DragSource;
        static int s_DragPort;
        static GUIStyle s_OutputLabel;

        internal static void ResetDrag() => s_DragSource = null;

        internal static void SetActiveNodes(NodeCanvas.Framework.Graph graph, ISet<string> ids)
        {
            HashSet<string> active = s_ActiveNodes.GetValue(graph, _ => new HashSet<string>(StringComparer.Ordinal));
            active.Clear();
            active.UnionWith(ids);
        }

        static NodePorts Ports(Node node)
        {
            NodePorts value = s_Ports.GetValue(node, _ => new NodePorts());
            object payload = node is CharacterPoseCanvasNode pose ? pose.Payload : ((CharacterPoseDocumentCanvasNode)node).Ports;
            object dynamicPorts = node is CharacterPoseCanvasNode typed ? typed.DynamicPorts : payload;
            if (value.Inputs != null && ReferenceEquals(value.Payload, payload) &&
                ReferenceEquals(value.DynamicPorts, dynamicPorts))
                return value;
            value.Payload = payload;
            value.DynamicPorts = dynamicPorts;
            IEnumerable<CanvasPort> ports = node is CharacterPoseCanvasNode original
                ? CharacterPoseAuthoringPortProjection.Get(original).Select(port => new CanvasPort(
                    port.PortId.Value, port.Name, port.Kind.ToString(), port.Direction))
                : ((CharacterPoseDocumentCanvasNode)node).Ports.Select(port => new CanvasPort(
                    port.PortId.Value, port.DisplayName, port.ValueTypeId,
                    port.Direction == GraphAuthoringPortDirection.Input ? CharacterPosePortDirection.Input : CharacterPosePortDirection.Output));
            value.Inputs = ports.Where(port => port.Direction == CharacterPosePortDirection.Input).ToArray();
            value.Outputs = ports.Where(port => port.Direction == CharacterPosePortDirection.Output).ToArray();
            value.InputRects = new Rect[value.Inputs.Length];
            value.OutputRects = new Rect[value.Outputs.Length];
            return value;
        }

        internal static void DrawBody(Node node)
        {
            NodePorts ports = Ports(node);
            int count = Math.Max(ports.Inputs.Length, ports.Outputs.Length);
            for (int i = 0; i < count; i++)
            {
                Rect row = GUILayoutUtility.GetRect(240f, 20f);
                if (i < ports.Inputs.Length)
                {
                    CanvasPort port = ports.Inputs[i];
                    GUI.Label(new Rect(row.x + 3f, row.y, row.width * .5f - 6f, row.height),
                        new GUIContent(port.Name, $"{port.Id} · {port.Kind}"));
                    if (Event.current.type == EventType.Repaint)
                        ports.InputRects[i] = new Rect(-6f, row.center.y - 6f, 12f, 12f);
                }
                if (i < ports.Outputs.Length)
                {
                    CanvasPort port = ports.Outputs[i];
                    s_OutputLabel ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };
                    GUI.Label(new Rect(row.center.x, row.y, row.width * .5f - 3f, row.height),
                        new GUIContent(port.Name, $"{port.Id} · {port.Kind}"), s_OutputLabel);
                    if (Event.current.type == EventType.Repaint)
                        ports.OutputRects[i] = new Rect(node.rect.width - 6f, row.center.y - 6f, 12f, 12f);
                }
            }
            if (node is CharacterPoseCanvasNode pose &&
                CharacterPoseNodeDefinitionModule.Shared.Require(pose.Kind).Capability.ChildSurfaces.Count != 0 &&
                GUILayout.Button("打开子图"))
                Apply(() => CharacterPoseGraphWorkspace.OpenNodeChild(pose));
        }

        internal static void DrawConnections(Node node, Rect canvas, bool fullDrawPass, Vector2 mouse, float zoom)
        {
            string nodeId = node is CharacterPoseCanvasNode pose ? pose.NodeId.Value : ((CharacterPoseDocumentCanvasNode)node).ElementId.Value;
            if (Event.current.type == EventType.Repaint && s_ActiveNodes.TryGetValue(node.graph, out HashSet<string> active) && active.Contains(nodeId))
                Handles.DrawSolidRectangleWithOutline(node.rect, Color.clear, new Color(.25f, .8f, .45f));
            NodePorts ports = Ports(node);
            DrawPorts(node, ports.Inputs, ports.InputRects, false);
            DrawPorts(node, ports.Outputs, ports.OutputRects, true);
            foreach (Connection edge in node.outConnections.ToArray())
            {
                string sourcePort = edge is CharacterPoseCanvasConnection poseEdge ? poseEdge.SourcePortId.Value : ((CharacterPoseDocumentCanvasConnection)edge).SourcePort;
                string targetPort = edge is CharacterPoseCanvasConnection targetEdge ? targetEdge.TargetPortId.Value : ((CharacterPoseDocumentCanvasConnection)edge).TargetPort;
                NodePorts target = Ports(edge.targetNode);
                int output = Array.FindIndex(ports.Outputs, value => value.Id == sourcePort);
                int input = Array.FindIndex(target.Inputs, value => value.Id == targetPort);
                if (output < 0 || input < 0)
                    throw new InvalidOperationException($"Pose connection '{sourcePort} → {targetPort}' references an unknown port.");
                edge.DrawConnectionGUI(WorldRect(node, ports.OutputRects[output]).center,
                    WorldRect(edge.targetNode, target.InputRects[input]).center);
            }
            if (s_DragSource != node)
                return;
            Event current = Event.current;
            Vector2 start = WorldRect(node, ports.OutputRects[s_DragPort]).center;
            Handles.DrawBezier(start, current.mousePosition, start + Vector2.right * 60f,
                current.mousePosition - Vector2.right * 60f, Color.cyan, null, 3f);
            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            {
                ResetDrag();
                current.Use();
            }
            else if (current.type == EventType.MouseUp && current.button == 0)
            {
                int sourceIndex = s_DragPort;
                ResetDrag();
                if (TryPickPort(node.graph, current.mousePosition, CharacterPosePortDirection.Input,
                        out Node target, out int input))
                    Apply(() => node.graph.ConnectNodes(node, target, sourceIndex, input));
                else if (!node.graph.allNodes.Any(value => value.rect.Contains(current.mousePosition)))
                {
                    GenericMenu menu = node.graph.GetNodeSelectionMenu(new NodeCanvas.Framework.Graph.NodeCreationRequestContext
                    {
                        position = current.mousePosition,
                        connectSource = node,
                        connectSourcePortIndex = sourceIndex
                    });
                    GraphEditorUtility.PostGUI += menu.ShowAsContext;
                }
                current.Use();
            }
            GraphEditor.current?.Repaint();
        }

        static void DrawPorts(Node node, CanvasPort[] ports, Rect[] rects, bool output)
        {
            Event current = Event.current;
            for (int i = 0; i < ports.Length; i++)
            {
                Rect rect = WorldRect(node, rects[i]);
                GUI.Box(rect, new GUIContent(string.Empty, $"{ports[i].Name} · {ports[i].Kind}"), EditorStyles.radioButton);
                if (node.graph.isEditorReadOnly || !GraphEditorUtility.allowClick || !rect.Contains(current.mousePosition))
                    continue;
                if (output && current.type == EventType.MouseDown && current.button == 0)
                {
                    s_DragSource = node;
                    s_DragPort = i;
                    current.Use();
                }
                else if (current.type == EventType.ContextClick)
                {
                    string portId = ports[i].Id;
                    Connection[] edges = (output ? node.outConnections : node.inConnections)
                        .Where(value => value is CharacterPoseCanvasConnection pose
                            ? (output ? pose.SourcePortId.Value : pose.TargetPortId.Value) == portId
                            : (output ? ((CharacterPoseDocumentCanvasConnection)value).SourcePort : ((CharacterPoseDocumentCanvasConnection)value).TargetPort) == portId).ToArray();
                    var menu = new GenericMenu();
                    foreach (Connection edge in edges)
                        menu.AddItem(new GUIContent($"断开/{edge.sourceNode.name} → {edge.targetNode.name}"),
                            false, () => Apply(() => node.graph.RemoveConnection(edge)));
                    if (edges.Length == 0)
                        menu.AddDisabledItem(new GUIContent("没有连接"));
                    GraphEditorUtility.PostGUI += menu.ShowAsContext;
                    current.Use();
                }
            }
        }

        internal static void Relink(CharacterPoseCanvasNode node, Connection connection)
        {
            var edge = (CharacterPoseCanvasConnection)connection;
            CharacterPosePortDirection direction = connection.relinkState == Connection.RelinkState.Source
                ? CharacterPosePortDirection.Output : CharacterPosePortDirection.Input;
            if (!TryPickPort(node.graph, Event.current.mousePosition, direction, out Node target, out int port))
                return;
            Apply(() => ((CharacterPoseCanvasGraph)node.graph).EditorWriteRouter.Reconnect(edge,
                direction == CharacterPosePortDirection.Output ? target : edge.SourceNode,
                direction == CharacterPosePortDirection.Output ? port : -1,
                direction == CharacterPosePortDirection.Input ? target : edge.TargetNode,
                direction == CharacterPosePortDirection.Input ? port : -1));
        }

        static bool TryPickPort(NodeCanvas.Framework.Graph graph, Vector2 mouse, CharacterPosePortDirection direction,
            out Node picked, out int index)
        {
            foreach (Node node in graph.allNodes)
            {
                NodePorts ports = Ports(node);
                Rect[] rects = direction == CharacterPosePortDirection.Input ? ports.InputRects : ports.OutputRects;
                for (int i = 0; i < rects.Length; i++)
                {
                    Rect rect = WorldRect(node, rects[i]);
                    rect.xMin -= 5f;
                    rect.xMax += 5f;
                    if (!rect.Contains(mouse))
                        continue;
                    picked = node;
                    index = i;
                    return true;
                }
            }
            picked = null;
            index = -1;
            return false;
        }

        static Rect WorldRect(Node node, Rect local)
        {
            local.position += node.position;
            return local;
        }

        internal static void Apply(Action action)
        {
            try { action(); }
            catch (InvalidOperationException error) { GraphEditor.current?.ShowNotification(new GUIContent(error.Message)); }
            catch (ArgumentException error) { GraphEditor.current?.ShowNotification(new GUIContent(error.Message)); }
        }
    }
}
