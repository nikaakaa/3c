using System;
using System.Linq;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonSimulation;
using TreeDesigner;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CustomEditor(typeof(CharacterEquipmentProfile))]
    public sealed class CharacterEquipmentProfileEditor : UnityEditor.Editor
    {
        SerializedProperty m_Slots;
        SerializedProperty m_Routes;
        SerializedProperty m_Features;
        SerializedProperty m_Equipment;
        SerializedProperty m_InitialLoadout;

        void OnEnable()
        {
            m_Slots = serializedObject.FindProperty("m_Slots");
            m_Routes = serializedObject.FindProperty("m_Routes");
            m_Features = serializedObject.FindProperty("m_Features");
            m_Equipment = serializedObject.FindProperty("m_Equipment");
            m_InitialLoadout = serializedObject.FindProperty("m_InitialLoadout");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawSlots();
            DrawRoutes();
            EditorGUILayout.PropertyField(m_Features, new GUIContent("Feature Catalog"), true);
            EditorGUILayout.PropertyField(m_Equipment, new GUIContent("Equipment Catalog"), true);
            EditorGUILayout.PropertyField(m_InitialLoadout, new GUIContent("Initial Loadout"), true);
            serializedObject.ApplyModifiedProperties();
        }

        void DrawSlots()
        {
            m_Slots.isExpanded = EditorGUILayout.Foldout(m_Slots.isExpanded, $"Slots ({m_Slots.arraySize})", true);
            if (!m_Slots.isExpanded)
                return;
            for (int i = 0; i < m_Slots.arraySize; i++)
            {
                SerializedProperty item = m_Slots.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (EquipmentEditorGui.DrawArrayHeader(m_Slots, i, "Slot"))
                {
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.PropertyField(item.FindPropertyRelative("m_SlotId"), new GUIContent("Slot Id"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("m_Requirement"), new GUIContent("Requirement"));
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Add Slot"))
            {
                SerializedProperty item = EquipmentEditorGui.AddElement(m_Slots);
                item.FindPropertyRelative("m_SlotId").stringValue = EquipmentEditorGui.NewIdentity("slot");
                item.FindPropertyRelative("m_Requirement").enumValueIndex = 0;
            }
        }

        void DrawRoutes()
        {
            m_Routes.isExpanded = EditorGUILayout.Foldout(m_Routes.isExpanded, $"Action Routes ({m_Routes.arraySize})", true);
            if (!m_Routes.isExpanded)
                return;
            for (int i = 0; i < m_Routes.arraySize; i++)
            {
                SerializedProperty item = m_Routes.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (EquipmentEditorGui.DrawArrayHeader(m_Routes, i, "Route"))
                {
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.PropertyField(item.FindPropertyRelative("m_RouteId"), new GUIContent("Route Id"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("m_OwnerSlotId"), new GUIContent("Owner Slot Id"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("m_InputRequestId"), new GUIContent("Input Request Id"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("m_RequestConsumption"), new GUIContent("Request Consumption"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("m_MissingImplementation"), new GUIContent("Missing Implementation"));
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Add Route"))
            {
                SerializedProperty item = EquipmentEditorGui.AddElement(m_Routes);
                item.FindPropertyRelative("m_RouteId").stringValue = EquipmentEditorGui.NewIdentity("route");
                item.FindPropertyRelative("m_OwnerSlotId").stringValue = string.Empty;
                item.FindPropertyRelative("m_InputRequestId").stringValue = string.Empty;
                item.FindPropertyRelative("m_RequestConsumption").enumValueIndex = 0;
                item.FindPropertyRelative("m_MissingImplementation").enumValueIndex = 0;
            }
        }
    }

    [CustomEditor(typeof(CharacterEquipmentFeatureDefinition))]
    public sealed class CharacterEquipmentFeatureDefinitionEditor : UnityEditor.Editor
    {
        SerializedProperty m_FeatureId;
        SerializedProperty m_FeatureRevision;
        SerializedProperty m_Parameters;
        SerializedProperty m_LocalStates;
        SerializedProperty m_GrantedTags;
        SerializedProperty m_PassiveEffects;
        SerializedProperty m_PersistentGraph;
        SerializedProperty m_RouteImplementations;
        SerializedProperty m_RequiredGameplayCapabilities;
        SerializedProperty m_RequiredWorldCapabilities;
        CharacterPipelineDefinition m_Context;

        CharacterEquipmentFeatureDefinition Feature => target as CharacterEquipmentFeatureDefinition;

        void OnEnable()
        {
            m_FeatureId = serializedObject.FindProperty("m_FeatureId");
            m_FeatureRevision = serializedObject.FindProperty("m_FeatureRevision");
            m_Parameters = serializedObject.FindProperty("m_Parameters");
            m_LocalStates = serializedObject.FindProperty("m_LocalStates");
            m_GrantedTags = serializedObject.FindProperty("m_GrantedTags");
            m_PassiveEffects = serializedObject.FindProperty("m_PassiveEffects");
            m_PersistentGraph = serializedObject.FindProperty("m_PersistentGraph");
            m_RouteImplementations = serializedObject.FindProperty("m_RouteImplementations");
            m_RequiredGameplayCapabilities = serializedObject.FindProperty("m_RequiredGameplayCapabilities");
            m_RequiredWorldCapabilities = serializedObject.FindProperty("m_RequiredWorldCapabilities");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_FeatureId, new GUIContent("Feature Id"));
            EditorGUILayout.PropertyField(m_FeatureRevision, new GUIContent("Feature Revision"));
            DrawParameters();
            DrawLocalStates();
            EditorGUILayout.PropertyField(m_GrantedTags, new GUIContent("Granted Tags"), true);
            EditorGUILayout.PropertyField(m_PassiveEffects, new GUIContent("Passive Effects"), true);
            EditorGUILayout.PropertyField(m_RequiredGameplayCapabilities, new GUIContent("Gameplay Capabilities"), true);
            EditorGUILayout.PropertyField(m_RequiredWorldCapabilities, new GUIContent("World Capabilities"));
            DrawContext();
            DrawPersistentGraph();
            DrawRoutes();
            serializedObject.ApplyModifiedProperties();
        }

        void DrawContext()
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Graph Context", EditorStyles.boldLabel);
            m_Context = EditorGUILayout.ObjectField("Pipeline Definition", m_Context, typeof(CharacterPipelineDefinition), false) as CharacterPipelineDefinition;
            if (m_Context && !ReferencesFeature(m_Context, Feature))
                EditorGUILayout.HelpBox("The selected Pipeline Definition does not reference this Feature.", MessageType.Error);
        }

        void DrawParameters()
        {
            m_Parameters.isExpanded = EditorGUILayout.Foldout(m_Parameters.isExpanded, $"Parameter Schema ({m_Parameters.arraySize})", true);
            if (!m_Parameters.isExpanded)
                return;
            for (int i = 0; i < m_Parameters.arraySize; i++)
            {
                SerializedProperty parameter = m_Parameters.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (EquipmentEditorGui.DrawArrayHeader(m_Parameters, i, "Parameter"))
                {
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.PropertyField(parameter.FindPropertyRelative("m_ParameterId"), new GUIContent("Parameter Id"));
                EditorGUILayout.PropertyField(parameter.FindPropertyRelative("m_ValueKind"), new GUIContent("Value Kind"));
                EditorGUILayout.PropertyField(parameter.FindPropertyRelative("m_Required"), new GUIContent("Required"));
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Add Parameter"))
            {
                SerializedProperty parameter = EquipmentEditorGui.AddElement(m_Parameters);
                parameter.FindPropertyRelative("m_ParameterId").stringValue = EquipmentEditorGui.NewIdentity("parameter");
                parameter.FindPropertyRelative("m_ValueKind").enumValueIndex = 2;
                parameter.FindPropertyRelative("m_Required").boolValue = true;
            }
        }

        void DrawLocalStates()
        {
            m_LocalStates.isExpanded = EditorGUILayout.Foldout(m_LocalStates.isExpanded, $"Local State ({m_LocalStates.arraySize})", true);
            if (!m_LocalStates.isExpanded)
                return;
            for (int i = 0; i < m_LocalStates.arraySize; i++)
            {
                SerializedProperty state = m_LocalStates.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (EquipmentEditorGui.DrawArrayHeader(m_LocalStates, i, "State"))
                {
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.PropertyField(state.FindPropertyRelative("m_StateId"), new GUIContent("State Id"));
                EditorGUILayout.PropertyField(state.FindPropertyRelative("m_ValueKind"), new GUIContent("Value Kind"));
                EditorGUILayout.PropertyField(state.FindPropertyRelative("m_DefaultValue"), new GUIContent("Default Value"), true);
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Add Local State"))
            {
                SerializedProperty state = EquipmentEditorGui.AddElement(m_LocalStates);
                state.FindPropertyRelative("m_StateId").stringValue = EquipmentEditorGui.NewIdentity("state");
                state.FindPropertyRelative("m_ValueKind").enumValueIndex = 0;
            }
        }

        void DrawPersistentGraph()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Persistent Inline Graph", EditorStyles.boldLabel);
            bool exists = m_PersistentGraph.managedReferenceValue != null;
            using (new EditorGUI.DisabledScope(exists))
            {
                if (GUILayout.Button("Create Persistent Graph"))
                {
                    serializedObject.ApplyModifiedProperties();
                    Undo.RecordObject(Feature, "Create Equipment Persistent Graph");
                    Feature.CreatePersistentGraph();
                    EditorUtility.SetDirty(Feature);
                    serializedObject.Update();
                }
            }
            using (new EditorGUI.DisabledScope(!exists || !CanOpenGraph()))
            {
                if (GUILayout.Button("Open Persistent Graph"))
                    OpenGraph(Feature.PersistentGraph, "Persistent", "equipmentPersistent");
            }
        }

        void DrawRoutes()
        {
            m_RouteImplementations.isExpanded = EditorGUILayout.Foldout(
                m_RouteImplementations.isExpanded,
                $"Route Implementations ({m_RouteImplementations.arraySize})",
                true);
            if (!m_RouteImplementations.isExpanded)
                return;
            for (int i = 0; i < m_RouteImplementations.arraySize; i++)
            {
                SerializedProperty route = m_RouteImplementations.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (EquipmentEditorGui.DrawArrayHeader(m_RouteImplementations, i, "Route Implementation"))
                {
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.PropertyField(route.FindPropertyRelative("m_RouteId"), new GUIContent("Route Id"));
                EditorGUILayout.PropertyField(route.FindPropertyRelative("m_ActionProfile"), new GUIContent("Action Profile"));
                EditorGUILayout.PropertyField(route.FindPropertyRelative("m_RequiredParameterIds"), new GUIContent("Required Parameters"), true);
                EditorGUILayout.PropertyField(route.FindPropertyRelative("m_RequiredProducerIds"), new GUIContent("Required Producers"), true);
                SerializedProperty graph = route.FindPropertyRelative("m_InlineGraph");
                bool exists = graph.managedReferenceValue != null;
                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(exists))
                {
                    if (GUILayout.Button("Create Graph"))
                        CreateRouteGraph(i);
                }
                using (new EditorGUI.DisabledScope(!exists || !CanOpenGraph()))
                {
                    if (GUILayout.Button("Open Graph"))
                        OpenGraph(Feature.RouteImplementations[i].InlineGraph, $"Route {Feature.RouteImplementations[i].RouteIdValue}", $"equipmentRoute:{i}");
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Add Route Implementation"))
            {
                SerializedProperty route = EquipmentEditorGui.AddElement(m_RouteImplementations);
                route.FindPropertyRelative("m_RouteId").stringValue = EquipmentEditorGui.NewIdentity("route");
                route.FindPropertyRelative("m_ActionProfile").objectReferenceValue = null;
                route.FindPropertyRelative("m_InlineGraph").managedReferenceValue = null;
                route.FindPropertyRelative("m_RequiredParameterIds").arraySize = 0;
                route.FindPropertyRelative("m_RequiredProducerIds").arraySize = 0;
            }
        }

        void CreateRouteGraph(int index)
        {
            serializedObject.ApplyModifiedProperties();
            Undo.RecordObject(Feature, "Create Equipment Route Graph");
            Feature.RouteImplementations[index].CreateInlineGraph(Feature, index);
            EditorUtility.SetDirty(Feature);
            serializedObject.Update();
        }

        bool CanOpenGraph() => m_Context && ReferencesFeature(m_Context, Feature);

        void OpenGraph(BaseTree graph, string displayName, string referenceKey)
        {
            if (!graph || !CanOpenGraph())
                return;
            BaseTreeWindow window = CharacterPipelineDefinitionTreeWindowUtility.OpenRootTree(m_Context);
            window?.PushTreePage(
                graph,
                null,
                displayName,
                $"equipment-feature:{Feature.FeatureIdValue}",
                referenceKey);
        }

        static bool ReferencesFeature(CharacterPipelineDefinition definition, CharacterEquipmentFeatureDefinition feature)
        {
            return definition && feature && definition.EquipmentCapabilityEnabled && definition.EquipmentProfile &&
                   definition.EquipmentProfile.Features.Contains(feature);
        }
    }

    [CustomEditor(typeof(EquipmentDefinition))]
    public sealed class EquipmentDefinitionEditor : UnityEditor.Editor
    {
        SerializedProperty m_EquipmentId;
        SerializedProperty m_SlotId;
        SerializedProperty m_Feature;
        SerializedProperty m_ParameterValues;
        SerializedProperty m_VisualBindingId;

        EquipmentDefinition Equipment => target as EquipmentDefinition;

        void OnEnable()
        {
            m_EquipmentId = serializedObject.FindProperty("m_EquipmentId");
            m_SlotId = serializedObject.FindProperty("m_SlotId");
            m_Feature = serializedObject.FindProperty("m_Feature");
            m_ParameterValues = serializedObject.FindProperty("m_ParameterValues");
            m_VisualBindingId = serializedObject.FindProperty("m_VisualBindingId");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_EquipmentId, new GUIContent("Equipment Id"));
            EditorGUILayout.PropertyField(m_SlotId, new GUIContent("Slot Id"));
            EditorGUILayout.PropertyField(m_Feature, new GUIContent("Feature"));
            EditorGUILayout.PropertyField(m_VisualBindingId, new GUIContent("Visual Binding Id"));
            DrawParameters();
            serializedObject.ApplyModifiedProperties();
        }

        void DrawParameters()
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Typed Parameters", EditorStyles.boldLabel);
            CharacterEquipmentFeatureDefinition feature = m_Feature.objectReferenceValue as CharacterEquipmentFeatureDefinition;
            if (!feature)
            {
                EditorGUILayout.HelpBox("Select a Feature to edit typed parameter values.", MessageType.Info);
                return;
            }
            for (int i = 0; i < feature.Parameters.Count; i++)
            {
                EquipmentParameterSchema schema = feature.Parameters[i];
                if (schema == null)
                    continue;
                int valueIndex = FindParameter(schema.ParameterIdValue);
                if (valueIndex < 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{schema.ParameterIdValue} ({schema.ValueKind})");
                    if (GUILayout.Button("Add", GUILayout.Width(64f)))
                        AddParameter(schema);
                    EditorGUILayout.EndHorizontal();
                    continue;
                }
                DrawParameter(m_ParameterValues.GetArrayElementAtIndex(valueIndex), schema);
            }
            for (int i = m_ParameterValues.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty value = m_ParameterValues.GetArrayElementAtIndex(i);
                string id = value.FindPropertyRelative("m_ParameterId").stringValue;
                if (feature.Parameters.Any(item => item != null && string.Equals(item.ParameterIdValue, id, StringComparison.Ordinal)))
                    continue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox($"Unknown parameter '{id}'.", MessageType.Error);
                if (GUILayout.Button("Remove", GUILayout.Width(72f)))
                    m_ParameterValues.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
            }
        }

        void DrawParameter(SerializedProperty value, EquipmentParameterSchema schema)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(schema.ParameterIdValue, schema.ValueKind.ToString());
            SerializedProperty field = value.FindPropertyRelative(schema.ValueKind switch
            {
                EquipmentParameterValueKind.Boolean => "m_Boolean",
                EquipmentParameterValueKind.Int32 => "m_Int32",
                EquipmentParameterValueKind.Scalar => "m_Scalar",
                EquipmentParameterValueKind.Vector2 => "m_Vector2",
                EquipmentParameterValueKind.Vector3 => "m_Vector3",
                EquipmentParameterValueKind.Yaw => "m_YawDegrees",
                EquipmentParameterValueKind.GameplayTag => "m_GameplayTag",
                EquipmentParameterValueKind.GameplayEffect => "m_GameplayEffect",
                EquipmentParameterValueKind.AnimationProducer => "m_AnimationProducerId",
                _ => throw new ArgumentOutOfRangeException()
            });
            EditorGUILayout.PropertyField(field, GUIContent.none, true);
            EditorGUILayout.EndVertical();
        }

        int FindParameter(string parameterId)
        {
            for (int i = 0; i < m_ParameterValues.arraySize; i++)
                if (string.Equals(m_ParameterValues.GetArrayElementAtIndex(i).FindPropertyRelative("m_ParameterId").stringValue, parameterId, StringComparison.Ordinal))
                    return i;
            return -1;
        }

        void AddParameter(EquipmentParameterSchema schema)
        {
            SerializedProperty value = EquipmentEditorGui.AddElement(m_ParameterValues);
            value.FindPropertyRelative("m_ParameterId").stringValue = schema.ParameterIdValue;
            value.FindPropertyRelative("m_ValueKind").enumValueIndex = Math.Max(0, (int)schema.ValueKind - 1);
			value.FindPropertyRelative("m_Boolean").boolValue = false;
			value.FindPropertyRelative("m_Int32").intValue = 0;
			value.FindPropertyRelative("m_UInt64").longValue = 0;
			value.FindPropertyRelative("m_Scalar").floatValue = 0f;
			value.FindPropertyRelative("m_Vector2").vector2Value = Vector2.zero;
			value.FindPropertyRelative("m_Vector3").vector3Value = Vector3.zero;
			value.FindPropertyRelative("m_YawDegrees").floatValue = 0f;
			value.FindPropertyRelative("m_GameplayEffect").objectReferenceValue = null;
			value.FindPropertyRelative("m_AnimationProducerId").stringValue = string.Empty;
			value.FindPropertyRelative("m_Identity").stringValue = string.Empty;
        }
    }

    [CustomEditor(typeof(CharacterEquipmentPresentationProfile))]
    public sealed class CharacterEquipmentPresentationProfileEditor : UnityEditor.Editor
    {
        SerializedProperty m_Bindings;

        void OnEnable()
        {
            m_Bindings = serializedObject.FindProperty("m_VisualBindings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            for (int i = 0; i < m_Bindings.arraySize; i++)
            {
                SerializedProperty binding = m_Bindings.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (EquipmentEditorGui.DrawArrayHeader(m_Bindings, i, "Visual Binding"))
                {
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.PropertyField(binding.FindPropertyRelative("m_VisualBindingId"), new GUIContent("Binding Id"));
                SerializedProperty kind = binding.FindPropertyRelative("m_Kind");
                EditorGUILayout.PropertyField(kind, new GUIContent("Kind"));
                EquipmentVisualBindingKind value = (EquipmentVisualBindingKind)(kind.enumValueIndex + 1);
                if (value == EquipmentVisualBindingKind.ExistingRigObject)
                {
                    EditorGUILayout.PropertyField(binding.FindPropertyRelative("m_RigBindingId"), new GUIContent("Rig Binding Id"));
                    EditorGUILayout.PropertyField(binding.FindPropertyRelative("m_RendererBindingIds"), new GUIContent("Renderer Binding Ids"), true);
                }
                else
                {
                    EditorGUILayout.PropertyField(binding.FindPropertyRelative("m_VisualPrefab"), new GUIContent("Visual Prefab"));
                    EditorGUILayout.PropertyField(binding.FindPropertyRelative("m_SocketBindingId"), new GUIContent("Socket Binding Id"));
                    EditorGUILayout.PropertyField(binding.FindPropertyRelative("m_LocalPosition"), new GUIContent("Local Position"));
                    EditorGUILayout.PropertyField(binding.FindPropertyRelative("m_LocalEulerAngles"), new GUIContent("Local Rotation"));
                    EditorGUILayout.PropertyField(binding.FindPropertyRelative("m_LocalScale"), new GUIContent("Local Scale"));
                }
                EditorGUILayout.PropertyField(binding.FindPropertyRelative("m_LifecyclePolicy"), new GUIContent("Lifecycle"));
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Add Visual Binding"))
            {
                SerializedProperty binding = EquipmentEditorGui.AddElement(m_Bindings);
                binding.FindPropertyRelative("m_VisualBindingId").stringValue = EquipmentEditorGui.NewIdentity("visual");
                binding.FindPropertyRelative("m_Kind").enumValueIndex = 0;
                binding.FindPropertyRelative("m_RigBindingId").stringValue = string.Empty;
                binding.FindPropertyRelative("m_RendererBindingIds").arraySize = 0;
                binding.FindPropertyRelative("m_VisualPrefab").objectReferenceValue = null;
                binding.FindPropertyRelative("m_SocketBindingId").stringValue = string.Empty;
                binding.FindPropertyRelative("m_LocalPosition").vector3Value = Vector3.zero;
                binding.FindPropertyRelative("m_LocalEulerAngles").vector3Value = Vector3.zero;
                binding.FindPropertyRelative("m_LocalScale").vector3Value = Vector3.one;
                binding.FindPropertyRelative("m_LifecyclePolicy").enumValueIndex = 0;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(CharacterEquipmentRigBindingCatalog))]
    public sealed class CharacterEquipmentRigBindingCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var errors = new System.Collections.Generic.List<string>();
            CharacterEquipmentRigBindingCatalog catalog = target as CharacterEquipmentRigBindingCatalog;
            if (catalog != null && catalog.CollectConfigurationErrors(errors))
            {
                EditorGUILayout.HelpBox("Equipment Rig and Socket bindings are valid.", MessageType.Info);
                return;
            }
            for (int i = 0; i < errors.Count; i++)
                EditorGUILayout.HelpBox(errors[i], MessageType.Error);
        }
    }

    static class EquipmentEditorGui
    {
        public static string NewIdentity(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

        public static SerializedProperty AddElement(SerializedProperty array)
        {
            int index = array.arraySize;
            array.InsertArrayElementAtIndex(index);
            return array.GetArrayElementAtIndex(index);
        }

        public static bool DrawArrayHeader(SerializedProperty array, int index, string label)
        {
            bool changed = false;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{label} {index}", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(index == 0))
            {
                if (GUILayout.Button("Up", GUILayout.Width(42f)))
                {
                    array.MoveArrayElement(index, index - 1);
                    changed = true;
                }
            }
            using (new EditorGUI.DisabledScope(index >= array.arraySize - 1))
            {
                if (GUILayout.Button("Down", GUILayout.Width(48f)))
                {
                    array.MoveArrayElement(index, index + 1);
                    changed = true;
                }
            }
            if (GUILayout.Button("Remove", GUILayout.Width(64f)))
            {
                array.DeleteArrayElementAtIndex(index);
                changed = true;
            }
            EditorGUILayout.EndHorizontal();
            return changed;
        }
    }
}
