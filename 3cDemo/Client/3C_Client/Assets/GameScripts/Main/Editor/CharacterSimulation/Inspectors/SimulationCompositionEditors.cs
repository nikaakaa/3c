using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    [CustomEditor(typeof(SimulationSessionCompositionDefinition))]
    public sealed class SimulationSessionCompositionDefinitionEditor : UnityEditor.Editor
    {
        SerializedProperty m_SessionId;
        SerializedProperty m_WorldId;
        SerializedProperty m_MapId;
        SerializedProperty m_WorldRevision;
        SerializedProperty m_SourceClockId;
        SerializedProperty m_TickRate;
        SerializedProperty m_ProgramRuntime;
        SerializedProperty m_ExecutionBackend;
        SerializedProperty m_Pipeline;
        SerializedProperty m_SessionSource;
        SerializedProperty m_WorldSolver;
        SimulationSessionCompositionDefinition m_Definition;
        SimulationSessionCompositionCompatibilityReport m_Compatibility;
        string m_CompatibilityError;
        bool m_CompatibilityRefreshQueued;

        void OnEnable()
        {
            m_SessionId = serializedObject.FindProperty("m_SessionId");
            m_WorldId = serializedObject.FindProperty("m_WorldId");
            m_MapId = serializedObject.FindProperty("m_MapId");
            m_WorldRevision = serializedObject.FindProperty("m_WorldRevision");
            m_SourceClockId = serializedObject.FindProperty("m_SourceClockId");
            m_TickRate = serializedObject.FindProperty("m_TickRate");
            m_ProgramRuntime = serializedObject.FindProperty("m_ProgramRuntime");
            m_ExecutionBackend = serializedObject.FindProperty("m_ExecutionBackend");
            m_Pipeline = serializedObject.FindProperty("m_Pipeline");
            m_SessionSource = serializedObject.FindProperty("m_SessionSource");
            m_WorldSolver = serializedObject.FindProperty("m_WorldSolver");
            m_Definition = target as SimulationSessionCompositionDefinition;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Session Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_SessionId, new GUIContent("Session ID"));
            EditorGUILayout.PropertyField(m_WorldId, new GUIContent("World ID"));
            EditorGUILayout.PropertyField(m_MapId, new GUIContent("Map ID"));
            EditorGUILayout.PropertyField(m_WorldRevision, new GUIContent("World Revision"));
            EditorGUILayout.PropertyField(m_SourceClockId, new GUIContent("Source Clock ID"));
            EditorGUILayout.PropertyField(m_TickRate, new GUIContent("Tick Rate"));
            EditorGUILayout.Space(6f);

            EditorGUILayout.LabelField("Composition", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_ProgramRuntime, new GUIContent("Program Runtime"));
            EditorGUILayout.PropertyField(m_ExecutionBackend, new GUIContent("Execution Backend"));
            EditorGUILayout.PropertyField(m_Pipeline, new GUIContent("Pipeline"));
            EditorGUILayout.PropertyField(m_SessionSource, new GUIContent("Session Source"));
            EditorGUILayout.PropertyField(m_WorldSolver, new GUIContent("World Solver"));
            bool modified = serializedObject.hasModifiedProperties;
            serializedObject.ApplyModifiedProperties();
            if (modified)
            {
                m_Compatibility = null;
                m_CompatibilityError = null;
            }

            EditorGUILayout.Space(8f);
            DrawCompatibility();
        }

        void DrawCompatibility()
        {
            EditorGUILayout.LabelField("Compatibility", EditorStyles.boldLabel);
            if (!m_Definition)
                return;
            using (new EditorGUI.DisabledScope(m_CompatibilityRefreshQueued))
            {
                if (GUILayout.Button("刷新兼容性"))
                {
                    m_CompatibilityRefreshQueued = true;
                    EditorApplication.delayCall += RefreshCompatibility;
                }
            }
            if (m_CompatibilityRefreshQueued)
            {
                EditorGUILayout.HelpBox("正在刷新兼容性……", MessageType.Info);
                return;
            }
            if (!string.IsNullOrEmpty(m_CompatibilityError))
            {
                EditorGUILayout.HelpBox(m_CompatibilityError, MessageType.Error);
                return;
            }
            if (m_Compatibility == null)
            {
                EditorGUILayout.HelpBox("尚未执行兼容性检查。", MessageType.Info);
                return;
            }
            if (m_Compatibility.Issues.Count > 0)
            {
                for (int i = 0; i < m_Compatibility.Issues.Count; i++)
                    EditorGUILayout.HelpBox(m_Compatibility.Issues[i].ToString(), MessageType.Error);
                return;
            }
            if (m_Compatibility.Compilation == null)
            {
                EditorGUILayout.HelpBox("Pipeline compilation did not produce a result.", MessageType.Error);
                return;
            }
            if (!m_Compatibility.Compilation.IsValid)
            {
                for (int i = 0; i < m_Compatibility.Compilation.Errors.Count; i++)
                {
                    SimulationPipelineCompileError error = m_Compatibility.Compilation.Errors[i];
                    EditorGUILayout.HelpBox(
                        $"{error.Code}\n{error.Message}\n{error.ComponentIdentity}",
                        MessageType.Error);
                }
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Program Runtime ABI", $"{m_Compatibility.ProgramRuntime.NumericProfileId} / {m_Compatibility.ProgramRuntime.TargetAbiVersion}");
                EditorGUILayout.TextField("Backend", $"{m_Compatibility.Backend.BackendId}@{m_Compatibility.Backend.SemanticVersion}");
                EditorGUILayout.TextField("Pipeline ID", m_Compatibility.PipelineIdentity.Id.Value);
                EditorGUILayout.TextField("Pipeline Revision", m_Compatibility.PipelineIdentity.Revision.Value);
                EditorGUILayout.TextField("Pipeline Hash", m_Compatibility.PipelineIdentity.Hash.ToString());
                EditorGUILayout.TextField("Plan Hash", m_Compatibility.PlanHash.ToString());
                EditorGUILayout.TextField("Source", m_Compatibility.Source.Source.Identity.ToString());
                EditorGUILayout.TextField("Solver", m_Compatibility.Solver.Identity.ToString());
            }
            EditorGUILayout.HelpBox("Composition is compatible.", MessageType.Info);
        }

        void RefreshCompatibility()
        {
            m_CompatibilityRefreshQueued = false;
            if (!this || !m_Definition)
                return;
            try
            {
                m_Compatibility = SimulationSessionCompositionCompatibility.Evaluate(m_Definition);
                m_CompatibilityError = null;
            }
            catch (Exception exception)
            {
                m_Compatibility = null;
                m_CompatibilityError = exception.Message;
            }
            Repaint();
        }

        void OnDisable()
        {
            EditorApplication.delayCall -= RefreshCompatibility;
        }
    }

    [CustomEditor(typeof(SimulationPipelineDefinition), true)]
    public sealed class SimulationPipelineDefinitionEditor : UnityEditor.Editor
    {
        readonly Dictionary<SimulationPipelinePhase, ReorderableList> m_Lists =
            new Dictionary<SimulationPipelinePhase, ReorderableList>();
        SerializedProperty m_PipelineId;
        SerializedProperty m_Revision;
        SerializedProperty m_SchemaVersion;
        string m_AuthoringError = string.Empty;

        void OnEnable()
        {
            m_PipelineId = serializedObject.FindProperty("m_PipelineId");
            m_Revision = serializedObject.FindProperty("m_Revision");
            m_SchemaVersion = serializedObject.FindProperty("m_SchemaVersion");
            CreateList(SimulationPipelinePhase.Ingress, "m_IngressPasses");
            CreateList(SimulationPipelinePhase.Schedule, "m_SchedulePasses");
            CreateList(SimulationPipelinePhase.Step, "m_StepPasses");
            CreateList(SimulationPipelinePhase.Egress, "m_EgressPasses");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Pipeline Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_PipelineId, new GUIContent("Pipeline ID"));
            EditorGUILayout.PropertyField(m_Revision);
            EditorGUILayout.PropertyField(m_SchemaVersion, new GUIContent("Schema Version"));
            EditorGUILayout.Space(6f);
            DrawList(SimulationPipelinePhase.Ingress);
            DrawList(SimulationPipelinePhase.Schedule);
            DrawList(SimulationPipelinePhase.Step);
            DrawList(SimulationPipelinePhase.Egress);
            serializedObject.ApplyModifiedProperties();

            if (!string.IsNullOrEmpty(m_AuthoringError))
                EditorGUILayout.HelpBox(m_AuthoringError, MessageType.Error);
            DrawDescriptor(target as SimulationPipelineDefinition);
        }

        void CreateList(SimulationPipelinePhase phase, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            var list = new ReorderableList(serializedObject, property, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, phase.ToString()),
                elementHeight = EditorGUIUtility.singleLineHeight * 4f + 8f
            };
            list.drawElementCallback = (rect, index, active, focused) =>
                DrawPassElement(rect, property.GetArrayElementAtIndex(index), phase);
            list.onAddCallback = value => ReorderableList.defaultBehaviours.DoAddButton(value);
            m_Lists[phase] = list;
        }

        void DrawList(SimulationPipelinePhase phase)
        {
            m_Lists[phase].DoLayoutList();
            EditorGUILayout.Space(3f);
        }

        void DrawPassElement(Rect rect, SerializedProperty element, SimulationPipelinePhase phase)
        {
            float line = EditorGUIUtility.singleLineHeight;
            rect.y += 2f;
            var objectRect = new Rect(rect.x, rect.y, rect.width, line);
            EditorGUI.BeginChangeCheck();
            var selected = EditorGUI.ObjectField(
                objectRect,
                element.objectReferenceValue,
                typeof(SimulationPipelinePassDefinition),
                false) as SimulationPipelinePassDefinition;
            if (EditorGUI.EndChangeCheck())
            {
                if (selected && selected.Phase != phase)
                {
                    m_AuthoringError = $"Pass '{selected.name}' belongs to {selected.Phase}, not {phase}.";
                }
                else
                {
                    element.objectReferenceValue = selected;
                    m_AuthoringError = string.Empty;
                }
            }

            var definition = element.objectReferenceValue as SimulationPipelinePassDefinition;
            if (!definition)
            {
                EditorGUI.LabelField(new Rect(rect.x, rect.y + line + 2f, rect.width, line), "Missing Pass reference");
                return;
            }
            try
            {
                SimulationPipelinePassDescriptor descriptor = definition.BuildPortableDescriptor();
                EditorGUI.LabelField(
                    new Rect(rect.x, rect.y + line + 2f, rect.width, line),
                    $"{descriptor.VersionedIdentity} | {descriptor.StateClass} | {descriptor.ExecutionSupport}");
                EditorGUI.LabelField(
                    new Rect(rect.x, rect.y + (line + 2f) * 2f, rect.width, line),
                    $"Requires: Solver={descriptor.RequiredSolverCapabilities}, Target={descriptor.NumericProfileId}/ABI {descriptor.TargetAbiVersion}");
                EditorGUI.LabelField(
                    new Rect(rect.x, rect.y + (line + 2f) * 3f, rect.width, line),
                    BuildProductSummary(descriptor));
            }
            catch (Exception exception)
            {
                EditorGUI.LabelField(
                    new Rect(rect.x, rect.y + line + 2f, rect.width, line * 2f),
                    exception.Message,
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        static string BuildProductSummary(SimulationPipelinePassDescriptor descriptor)
        {
            var consumes = new List<string>();
            var produces = new List<string>();
            for (int i = 0; i < descriptor.ProductAccesses.Count; i++)
            {
                SimulationPipelineProductAccess access = descriptor.ProductAccesses[i];
                string identity = access.Product.ProductId.Value;
                if (access.IsProducer)
                    produces.Add(identity);
                else
                    consumes.Add(access.Required ? identity : $"{identity}?");
            }
            string input = consumes.Count == 0 ? "-" : string.Join(", ", consumes);
            string output = produces.Count == 0 ? "-" : string.Join(", ", produces);
            return $"Consumes: {input} | Produces: {output}";
        }

        static void DrawDescriptor(SimulationPipelineDefinition definition)
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Portable Descriptor", EditorStyles.boldLabel);
            if (!definition)
                return;
            try
            {
                SimulationPipelineDescriptor descriptor = definition.BuildPortableDescriptor();
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Pipeline ID", descriptor.PipelineId.Value);
                    EditorGUILayout.TextField("Revision", descriptor.Revision.Value);
                    EditorGUILayout.IntField("Schema Version", descriptor.SchemaVersion.Value);
                    EditorGUILayout.TextField("Descriptor Hash", descriptor.DescriptorHash.ToString());
                    EditorGUILayout.IntField("Pass Count", descriptor.Passes.Count);
                }
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
            }
        }
    }
}
