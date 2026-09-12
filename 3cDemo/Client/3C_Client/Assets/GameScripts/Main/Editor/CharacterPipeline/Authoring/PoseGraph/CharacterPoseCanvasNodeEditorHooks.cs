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
            PoseCanvasEditorBridge.Toolbar = CharacterPoseGraphWorkspace.DrawNativeToolbar;
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
            if (node.Payload is CharacterProgramParameterInputPosePayload parameter)
                GUILayout.Label(
                    $"Get: {CharacterPoseAuthoringDisplayNames.ForParameter(parameter.ParameterId)}",
                    EditorStyles.miniLabel);

            if (node.Payload is CharacterAnimationSlotPosePayload slot)
            {
                GUILayout.Label(
                    $"Slot: {CharacterPoseAuthoringDisplayNames.ForIdentity(slot.SlotId.Value)}",
                    EditorStyles.miniLabel);
                GUILayout.Label(
                    $"Blend Policy Slot: {(slot.BlendPolicySlot ? slot.BlendPolicySlot.name : "Missing")}",
                    EditorStyles.miniLabel);
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
            if (field.PickerKind == "pose-parameter-policy")
                return DrawParameterPolicies(
                    node,
                    graph,
                    field,
                    current as CharacterPoseParameterPolicy[] ?? Array.Empty<CharacterPoseParameterPolicy>());
            if (field.PickerKind == "full-body-ik-goal-binding")
                return DrawGoalBindings(
                    node,
                    graph,
                    field,
                    current as CharacterPoseBoneIkGoalBinding[] ?? Array.Empty<CharacterPoseBoneIkGoalBinding>());
            if (field.ValueKind == GraphAuthoringFieldValueKind.IdentityReference)
                return DrawIdentityReference(node, graph, field, current);
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

        static bool DrawIdentityReference(
            CharacterPoseCanvasNode node,
            NodeCanvas.Framework.Graph graph,
            GraphAuthoringFieldDescriptor field,
            object current)
        {
            string currentValue = current?.ToString() ?? string.Empty;
            if (CharacterPoseGraphWorkspace.TryGetFieldOptions(
                    node,
                    field,
                    out IReadOnlyList<GraphAuthoringFieldOption> options) &&
                options.Count != 0)
            {
                var values = new List<string>(options.Count + 1);
                var labels = new List<string>(options.Count + 1);
                foreach (GraphAuthoringFieldOption option in options)
                {
                    values.Add(option.Value);
                    labels.Add(option.DisplayName);
                }
                if (!string.IsNullOrEmpty(currentValue) &&
                    !values.Contains(currentValue))
                {
                    values.Add(currentValue);
                    labels.Add("Missing Reference");
                }
                int selected = Math.Max(0, values.IndexOf(currentValue));
                EditorGUI.BeginChangeCheck();
                using (new EditorGUI.DisabledScope(graph.isEditorReadOnly || !field.AuthoringWritable))
                    selected = EditorGUILayout.Popup(field.DisplayName, selected, labels.ToArray());
                if (!EditorGUI.EndChangeCheck() || graph.isEditorReadOnly || !field.AuthoringWritable)
                    return true;
                ApplyNodeField(node, graph, field.FieldId.Value, values[selected]);
                return false;
            }

            EditorGUI.BeginChangeCheck();
            string next = currentValue;
            using (new EditorGUI.DisabledScope(graph.isEditorReadOnly || !field.AuthoringWritable))
                next = EditorGUILayout.DelayedTextField(field.DisplayName, currentValue);
            if (!EditorGUI.EndChangeCheck() || graph.isEditorReadOnly || !field.AuthoringWritable ||
                string.Equals(next, currentValue, StringComparison.Ordinal))
                return true;
            ApplyNodeField(node, graph, field.FieldId.Value, next);
            return false;
        }

        static bool DrawParameterPolicies(
            CharacterPoseCanvasNode node,
            NodeCanvas.Framework.Graph graph,
            GraphAuthoringFieldDescriptor field,
            CharacterPoseParameterPolicy[] policies)
        {
            EditorGUILayout.LabelField(field.DisplayName, EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(graph.isEditorReadOnly || !field.AuthoringWritable))
            {
                for (int index = 0; index < policies.Length; index++)
                {
                    CharacterPoseParameterPolicy policy = policies[index];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(
                        CharacterPoseAuthoringDisplayNames.ForParameter(policy.ParameterId),
                        GUILayout.MinWidth(120f));
                    EditorGUI.BeginChangeCheck();
                    PoseParameterResolvePolicy nextPolicy = (PoseParameterResolvePolicy)EditorGUILayout.EnumPopup(policy.Policy);
                    bool remove = GUILayout.Button("−", GUILayout.Width(24f));
                    bool changed = EditorGUI.EndChangeCheck();
                    EditorGUILayout.EndHorizontal();
                    if (remove)
                    {
                        ApplyNodeField(node, graph, field.FieldId.Value,
                            policies.Where((_, itemIndex) => itemIndex != index).ToArray());
                        return false;
                    }
                    if (changed && nextPolicy != policy.Policy)
                    {
                        CharacterPoseParameterPolicy[] next = policies.ToArray();
                        next[index] = new CharacterPoseParameterPolicy(policy.ParameterId, nextPolicy);
                        ApplyNodeField(node, graph, field.FieldId.Value, next);
                        return false;
                    }
                }

                CharacterPoseCanvasGraph poseGraph = graph as CharacterPoseCanvasGraph;
                if (poseGraph != null)
                {
                    var used = new HashSet<string>(policies.Select(value => value.ParameterId.Value), StringComparer.Ordinal);
                    CharacterPoseParameterDeclaration[] choices = poseGraph.Parameters
                        .Where(value => value != null && value.ParameterId.IsValid &&
                                        !used.Contains(value.ParameterId.Value))
                        .ToArray();
                    if (choices.Length > 0)
                    {
                        int selected = EditorGUILayout.Popup(
                            "参数",
                            0,
                            choices.Select(value => value.DisplayName).ToArray());
                        PoseParameterResolvePolicy policy = (PoseParameterResolvePolicy)EditorGUILayout.EnumPopup(
                            "混合策略",
                            PoseParameterResolvePolicy.Weighted);
                        if (GUILayout.Button("添加参数策略"))
                        {
                            ApplyNodeField(node, graph, field.FieldId.Value, policies.Append(
                                new CharacterPoseParameterPolicy(
                                    choices[selected].ParameterId,
                                    policy)).ToArray());
                            return false;
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("没有可添加的参数");
                    }
                }
            }
            return true;
        }

        static bool DrawGoalBindings(
            CharacterPoseCanvasNode node,
            NodeCanvas.Framework.Graph graph,
            GraphAuthoringFieldDescriptor field,
            CharacterPoseBoneIkGoalBinding[] bindings)
        {
            EditorGUILayout.LabelField(field.DisplayName, EditorStyles.boldLabel);
            CharacterAnimationRigDefinition rig = CharacterPoseGraphWorkspace.CurrentRigDefinition;
            if (!rig)
            {
                EditorGUILayout.HelpBox("Unavailable: exact Rig context required.", MessageType.Info);
                return true;
            }

            string[] boneIds = rig.PhysicalBones
                .Select(value => value.BoneId.Value)
                .Concat(rig.VirtualBones.Select(value => value.VirtualBoneId.Value))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (boneIds.Length == 0)
            {
                EditorGUILayout.HelpBox("Unavailable: Rig has no pose bones.", MessageType.Info);
                return true;
            }
            string[] boneLabels = boneIds
                .Select(CharacterPoseAuthoringDisplayNames.ForIdentity)
                .ToArray();

            using (new EditorGUI.DisabledScope(graph.isEditorReadOnly || !field.AuthoringWritable))
            {
                for (int index = 0; index < bindings.Length; index++)
                {
                    CharacterPoseBoneIkGoalBinding binding = bindings[index];
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUI.BeginChangeCheck();
                    CharacterFullBodyIkEffectorSlot effector = (CharacterFullBodyIkEffectorSlot)EditorGUILayout.EnumPopup(
                        "效应器",
                        binding.EffectorSlot);
                    int boneIndex = Array.IndexOf(boneIds, binding.TargetPoseBoneId.Value);
                    int nextBoneIndex = EditorGUILayout.Popup(
                        "目标骨骼",
                        Mathf.Max(0, boneIndex),
                        boneLabels);
                    Vector3 position = EditorGUILayout.Vector3Field("位置偏移", binding.PositionOffset);
                    Vector3 rotation = EditorGUILayout.Vector3Field("旋转偏移", binding.RotationOffset.eulerAngles);
                    float positionWeight = EditorGUILayout.Slider("位置权重", binding.PositionWeight, 0f, 1f);
                    float rotationWeight = EditorGUILayout.Slider("旋转权重", binding.RotationWeight, 0f, 1f);
                    bool remove = GUILayout.Button("删除绑定");
                    bool changed = EditorGUI.EndChangeCheck();
                    EditorGUILayout.EndVertical();
                    if (remove)
                    {
                        ApplyNodeField(node, graph, field.FieldId.Value,
                            bindings.Where((_, itemIndex) => itemIndex != index).ToArray());
                        return false;
                    }
                    if (changed)
                    {
                        CharacterPoseBoneIkGoalBinding[] next = bindings.ToArray();
                        next[index] = new CharacterPoseBoneIkGoalBinding(
                            effector,
                            new AnimationBoneId(boneIds[nextBoneIndex]),
                            position,
                            rotation,
                            positionWeight,
                            rotationWeight);
                        ApplyNodeField(node, graph, field.FieldId.Value, next);
                        return false;
                    }
                }

                CharacterFullBodyIkEffectorSlot[] availableSlots = Enum.GetValues(
                        typeof(CharacterFullBodyIkEffectorSlot))
                    .Cast<CharacterFullBodyIkEffectorSlot>()
                    .Where(value => value >= CharacterFullBodyIkEffectorSlot.Body &&
                                   value <= CharacterFullBodyIkEffectorSlot.RightFoot &&
                                   bindings.All(binding => binding.EffectorSlot != value))
                    .ToArray();
                if (availableSlots.Length > 0)
                {
                    int selectedSlot = EditorGUILayout.Popup(
                        "新效应器",
                        0,
                        availableSlots.Select(value => value.ToString()).ToArray());
                    int selectedBone = EditorGUILayout.Popup("目标骨骼", 0, boneLabels);
                    if (GUILayout.Button("添加效应器绑定"))
                    {
                        ApplyNodeField(node, graph, field.FieldId.Value, bindings.Append(
                            new CharacterPoseBoneIkGoalBinding(
                                availableSlots[selectedSlot],
                                new AnimationBoneId(boneIds[selectedBone]),
                                Vector3.zero,
                                Vector3.zero,
                                1f,
                                1f)).ToArray());
                        return false;
                    }
                }
            }
            return true;
        }

        static void ApplyNodeField(
            CharacterPoseCanvasNode node,
            NodeCanvas.Framework.Graph graph,
            string fieldId,
            object value)
        {
            CharacterPoseCanvasGraph poseGraph = graph as CharacterPoseCanvasGraph;
            if (poseGraph?.EditorWriteRouter == null)
                throw new InvalidOperationException(
                    "Pose Canvas field edits require the Pose Canvas editor session.");
            CharacterPoseCanvasInteraction.Apply(() =>
                poseGraph.EditorWriteRouter.SetNodeField(node, fieldId, value));
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
