using TreeDesigner.Authoring;
using System;
using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Framework;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterPoseCanvasNodeEditorHooks
    {
        [InitializeOnLoadMethod]
        static void Register()
        {
            PoseCanvasEditorBridge.InspectorOverride = DrawInspector;
            PoseCanvasEditorBridge.VisualsRefresh = RefreshVisuals;
            PoseCanvasEditorBridge.PortShape = CharacterPoseAuthoringPortProjection.Get;
            PoseCanvasEditorBridge.BodyGUI = DrawBody;
            PoseCanvasEditorBridge.ContextMenu = node => ((CharacterPoseCanvasGraph)node.graph).EditorWriteRouter.BuildSelectionMenu(node.position);
            PoseCanvasEditorBridge.ChildSurface = node => {
                if (CharacterPoseNodeDefinitionModule.Shared.Require(node.Kind).Capability.ChildSurfaces.Count == 0) return false;
                CharacterPoseCanvasInteraction.Apply(() => NodeCanvas.Editor.GraphEditor.OpenEditorChild(node,
                    () => CharacterPoseGraphWorkspace.OpenNodeChild(node)));
                return true;
            };
        }

        static void DrawBody(CharacterPoseCanvasNode node)
        {
            if (node.Payload is CharacterAnimationSlotPosePayload slot)
            {
                GUILayout.Label($"Slot: {slot.SlotId.Value}", EditorStyles.miniLabel);
                if (slot.BlendPolicy)
                    GUILayout.Label($"默认混合: {slot.BlendPolicy.DefaultTransition.BlendLogic} · {slot.BlendPolicy.DefaultTransition.DurationSeconds:0.###} s", EditorStyles.miniLabel);
            }

            if (CharacterPoseGraphWorkspace.TryGetNodeObservation(node, out GraphAuthoringRuntimeTraceProjection trace))
            {
                if (GUILayout.Button("Pose Watch")) CharacterPoseGraphWorkspace.WatchNode(node);
            }

            if (CharacterPoseNodeDefinitionModule.Shared.Require(node.Kind).Capability.ChildSurfaces.Count != 0 &&
                GUILayout.Button("打开子图"))
                node.TryOpenEditorChild();
        }

        static void RefreshVisuals(CharacterPoseCanvasGraph graph)
        {
            if (graph == null)
                return;
            IReadOnlyList<CharacterPoseCanvasNode> nodes = graph.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                CharacterPoseNodeDefinition definition =
                    CharacterPoseNodeDefinitionModule.Shared.Require(nodes[i].Kind);
                nodes[i].customColor = definition.Capability.Color;
            }
        }

        static void DrawInspector(CharacterPoseCanvasNode node)
        {
            CharacterPoseNodeDefinition definition =
                CharacterPoseNodeDefinitionModule.Shared.Require(node.Kind);
            GraphAuthoringCapabilityDescriptor capability = definition.Capability;
            NodeCanvas.Framework.Graph graph = node.graph;
            EditorGUILayout.LabelField(
                capability.DisplayName,
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"Kind: {definition.Kind} · Domain: {capability.ExecutionDomainId}");
            EditorGUILayout.Space();
            string name;
            using (new EditorGUI.DisabledScope(graph.isEditorReadOnly))
                name = EditorGUILayout.DelayedTextField("名称", node.DisplayName);
            if (!string.Equals(name, node.DisplayName, StringComparison.Ordinal))
                CharacterPoseCanvasInteraction.Apply(() => node.name = name);
            foreach (GraphAuthoringFieldDescriptor field in capability.Fields)
            {
                if (!field.AuthoringVisible)
                    continue;
                if (!field.IsVisible(readField => definition.ReadField(
                        node.Payload,
                        readField.Value)))
                    continue;
                if (!DrawField(node, graph, definition, field))
                    return;
            }
        }

        static bool DrawField(
            CharacterPoseCanvasNode node,
            NodeCanvas.Framework.Graph graph,
            CharacterPoseNodeDefinition definition,
            GraphAuthoringFieldDescriptor field)
        {
            string fieldId = field.FieldId.Value;
            object current = definition.ReadField(node.Payload, fieldId);
            object next = current;
            EditorGUI.BeginChangeCheck();
            using (new EditorGUI.DisabledScope(graph.isEditorReadOnly || !field.AuthoringWritable))
            {
                switch (field.ValueKind)
                {
                    case GraphAuthoringFieldValueKind.String:
                        next = EditorGUILayout.DelayedTextField(field.DisplayName, current as string);
                        break;
                    case GraphAuthoringFieldValueKind.Boolean:
                        next = EditorGUILayout.Toggle(field.DisplayName, current is bool value && value);
                        break;
                    case GraphAuthoringFieldValueKind.Integer:
                        {
                            long raw = Convert.ToInt64(current);
                            long typed = EditorGUILayout.LongField(field.DisplayName, raw);
                            if (field.Constraint.Minimum.HasValue)
                                typed = Math.Max((long)field.Constraint.Minimum.Value, typed);
                            if (field.Constraint.Maximum.HasValue)
                                typed = Math.Min((long)field.Constraint.Maximum.Value, typed);
                            next = typed;
                        }
                        break;
                    case GraphAuthoringFieldValueKind.Float:
                        {
                            double raw = Convert.ToDouble(current);
                            double typed = EditorGUILayout.DoubleField(field.DisplayName, raw);
                            if (field.Constraint.Minimum.HasValue)
                                typed = Math.Max(field.Constraint.Minimum.Value, typed);
                            if (field.Constraint.Maximum.HasValue)
                                typed = Math.Min(field.Constraint.Maximum.Value, typed);
                            next = field.Constraint.Finite && !IsFinite(typed) ? current : typed;
                        }
                        break;
                    case GraphAuthoringFieldValueKind.Vector2:
                        next = EditorGUILayout.Vector2Field(field.DisplayName, ToVector2(current));
                        break;
                    case GraphAuthoringFieldValueKind.Vector3:
                        next = EditorGUILayout.Vector3Field(field.DisplayName, ToVector3(current));
                        break;
                    case GraphAuthoringFieldValueKind.Quaternion:
                        Vector3 euler = ToQuaternion(current).eulerAngles;
                        next = Quaternion.Euler(EditorGUILayout.Vector3Field(field.DisplayName, euler));
                        break;
                    case GraphAuthoringFieldValueKind.Enum:
                        next = DrawEnum(field, current);
                        break;
                    case GraphAuthoringFieldValueKind.AssetReference:
                        next = EditorGUILayout.ObjectField(
                            field.DisplayName,
                            current as UnityEngine.Object,
                            field.ObjectType ?? typeof(UnityEngine.Object),
                            false);
                        break;
                    default:
                        EditorGUILayout.LabelField(
                            field.DisplayName,
                            current?.ToString() ?? "—");
                        break;
                }
            }
            bool changed = EditorGUI.EndChangeCheck();
            if (!changed || graph.isEditorReadOnly || !field.AuthoringWritable || Equals(next, current) || next == current)
                return true;
            var poseGraph = graph as CharacterPoseCanvasGraph;
            if (poseGraph?.EditorWriteRouter == null)
                throw new InvalidOperationException(
                    "Pose Canvas field edits require the Pose Canvas editor session.");
            CharacterPoseCanvasInteraction.Apply(() => poseGraph.EditorWriteRouter.SetNodeField(node, fieldId, next));
            return false;
        }

        static object DrawEnum(GraphAuthoringFieldDescriptor field, object current)
        {
            if (field.ObjectType != null && field.ObjectType.IsEnum)
                return EditorGUILayout.EnumPopup(
                    field.DisplayName,
                    current as Enum ?? (Enum)Enum.ToObject(field.ObjectType, 0));
            IReadOnlyList<string> allowed = field.Constraint.AllowedValues;
            if (allowed != null && allowed.Count > 0)
            {
                string value = current as string ?? string.Empty;
                int index = -1;
                for (int i = 0; i < allowed.Count; i++)
                    if (string.Equals(allowed[i], value, StringComparison.Ordinal))
                    {
                        index = i;
                        break;
                    }
                if (index < 0)
                    index = 0;
                int next = EditorGUILayout.Popup(field.DisplayName, index, allowed.ToArray());
                return allowed[next];
            }
            EditorGUILayout.LabelField(field.DisplayName, current?.ToString() ?? "—");
            return current;
        }

        static Vector2 ToVector2(object value) =>
            value is Vector2 typed ? typed : Vector2.zero;

        static Vector3 ToVector3(object value) =>
            value is Vector3 typed ? typed : Vector3.zero;

        static Quaternion ToQuaternion(object value) =>
            value is Quaternion typed ? typed : Quaternion.identity;

        static bool IsFinite(double value) =>
            !double.IsInfinity(value) && !double.IsNaN(value);
    }
}
