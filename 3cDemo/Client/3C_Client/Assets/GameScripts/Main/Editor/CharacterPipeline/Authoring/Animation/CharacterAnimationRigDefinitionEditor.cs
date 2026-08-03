using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CustomEditor(typeof(CharacterAnimationRigDefinition))]
    public sealed class CharacterAnimationRigDefinitionEditor : UnityEditor.Editor
    {
        SerializedProperty m_Schema;
        SerializedProperty m_RigId;
        SerializedProperty m_Revision;
        SerializedProperty m_PhysicalBones;
        SerializedProperty m_VirtualBones;
        SerializedProperty m_RootBonePolicy;
        SerializedProperty m_ScalePolicy;
        SerializedProperty m_PelvisBoneId;
        SerializedProperty m_LeftLeg;
        SerializedProperty m_RightLeg;
        bool m_ShowPhysicalBones;
        bool m_ShowDiagnostics;
        string m_ValidationMessage = string.Empty;
        MessageType m_ValidationType;

        void OnEnable()
        {
            m_Schema = serializedObject.FindProperty("m_Schema");
            m_RigId = serializedObject.FindProperty("m_RigId");
            m_Revision = serializedObject.FindProperty("m_Revision");
            m_PhysicalBones = serializedObject.FindProperty("m_PhysicalBones");
            m_VirtualBones = serializedObject.FindProperty("m_VirtualBones");
            m_RootBonePolicy = serializedObject.FindProperty("m_RootBonePolicy");
            m_ScalePolicy = serializedObject.FindProperty("m_ScalePolicy");
            m_PelvisBoneId = serializedObject.FindProperty("m_PelvisBoneId");
            m_LeftLeg = serializedObject.FindProperty("m_LeftLeg");
            m_RightLeg = serializedObject.FindProperty("m_RightLeg");
            RefreshValidation();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Animation Rig v3", EditorStyles.boldLabel);
            if (string.IsNullOrWhiteSpace(m_RigId.stringValue))
            {
                EditorGUILayout.HelpBox("Rig尚未初始化机器身份。", MessageType.Error);
                if (GUILayout.Button("Initialize Rig Identity"))
                {
                    Undo.RecordObject(target, "Initialize Animation Rig Identity");
                    m_RigId.stringValue = $"animation-rig/{Guid.NewGuid():N}";
                    RegenerateRevision();
                }
            }
            EditorGUILayout.PropertyField(m_RootBonePolicy, new GUIContent("Root Bone Policy"));
            EditorGUILayout.PropertyField(m_ScalePolicy, new GUIContent("Scale Policy"));
            string[] physicalIds = GetPhysicalBoneIds();
            EditorGUI.BeginChangeCheck();
            DrawPhysicalPicker("Pelvis", m_PelvisBoneId, physicalIds, string.Empty);
            DrawLegChain("Left Leg", m_LeftLeg, physicalIds);
            DrawLegChain("Right Leg", m_RightLeg, physicalIds);
            if (EditorGUI.EndChangeCheck())
                RegenerateRevision();

            DrawPhysicalBones();
            DrawVirtualBones();
            DrawDiagnostics();

            if (serializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(target);
                RefreshValidation();
            }
        }

        static void DrawLegChain(string label, SerializedProperty leg, string[] physicalIds)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            DrawPhysicalPicker("Hip", leg.FindPropertyRelative("m_HipBoneId"), physicalIds, string.Empty);
            DrawPhysicalPicker("Knee", leg.FindPropertyRelative("m_KneeBoneId"), physicalIds, string.Empty);
            DrawPhysicalPicker("Ankle", leg.FindPropertyRelative("m_AnkleBoneId"), physicalIds, string.Empty);
            DrawPhysicalPicker("Toe", leg.FindPropertyRelative("m_ToeBoneId"), physicalIds, string.Empty);
            EditorGUILayout.EndVertical();
        }

        void DrawPhysicalBones()
        {
            EditorGUILayout.Space(6f);
            m_ShowPhysicalBones = EditorGUILayout.Foldout(
                m_ShowPhysicalBones,
                $"Physical Bones ({m_PhysicalBones.arraySize})",
                true);
            if (!m_ShowPhysicalBones)
                return;
            using (new EditorGUI.DisabledScope(true))
            {
                for (int i = 0; i < m_PhysicalBones.arraySize; i++)
                {
                    SerializedProperty bone = m_PhysicalBones.GetArrayElementAtIndex(i);
                    string id = bone.FindPropertyRelative("m_BoneId").stringValue;
                    int parent = bone.FindPropertyRelative("m_ParentIndex").intValue;
                    EditorGUILayout.LabelField($"[{i}] {ShortName(id)}", parent < 0 ? "Root" : $"Parent [{parent}]");
                }
            }
        }

        void DrawVirtualBones()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Virtual Bones ({m_VirtualBones.arraySize})", EditorStyles.boldLabel);
            if (GUILayout.Button("Add", GUILayout.Width(64f)))
                AddVirtualBone();
            EditorGUILayout.EndHorizontal();

            string[] physicalIds = GetPhysicalBoneIds();
            for (int i = 0; i < m_VirtualBones.arraySize; i++)
            {
                SerializedProperty bone = m_VirtualBones.GetArrayElementAtIndex(i);
                SerializedProperty id = bone.FindPropertyRelative("m_VirtualBoneId");
                SerializedProperty displayName = bone.FindPropertyRelative("m_DisplayName");
                SerializedProperty source = bone.FindPropertyRelative("m_SourcePhysicalBoneId");
                SerializedProperty targetBone = bone.FindPropertyRelative("m_TargetPhysicalBoneId");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (m_ShowDiagnostics)
                {
                    using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(id, new GUIContent("Virtual Bone Id"));
                }
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(displayName, new GUIContent("Display Name"));
                DrawPhysicalPicker("Source Physical Bone", source, physicalIds, string.Empty);
                DrawPhysicalPicker("Target Physical Bone", targetBone, physicalIds, source.stringValue);
                if (EditorGUI.EndChangeCheck())
                    RegenerateRevision();

                EditorGUILayout.LabelField(
                    "Source / Target",
                    $"{ShortName(source.stringValue)} → {ShortName(targetBone.stringValue)}");
                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(i == 0))
                {
                    if (GUILayout.Button("Up"))
                        MoveVirtualBone(i, i - 1);
                }
                using (new EditorGUI.DisabledScope(i >= m_VirtualBones.arraySize - 1))
                {
                    if (GUILayout.Button("Down"))
                        MoveVirtualBone(i, i + 1);
                }
                if (GUILayout.Button("Remove"))
                    RemoveVirtualBone(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        void AddVirtualBone()
        {
            string[] physicalIds = GetPhysicalBoneIds();
            if (physicalIds.Length < 2)
            {
                EditorUtility.DisplayDialog("Virtual Bone", "Rig 至少需要两个 Physical Bone。", "确定");
                return;
            }
            Undo.RecordObject(target, "Add Virtual Bone");
            int index = m_VirtualBones.arraySize;
            m_VirtualBones.InsertArrayElementAtIndex(index);
            SerializedProperty bone = m_VirtualBones.GetArrayElementAtIndex(index);
            bone.FindPropertyRelative("m_VirtualBoneId").stringValue =
                $"animation-bone/virtual/{Guid.NewGuid():N}";
            bone.FindPropertyRelative("m_DisplayName").stringValue = $"Virtual Bone {index + 1}";
            bone.FindPropertyRelative("m_SourcePhysicalBoneId").stringValue = physicalIds[0];
            bone.FindPropertyRelative("m_TargetPhysicalBoneId").stringValue = physicalIds[1];
            RegenerateRevision();
        }

        void RemoveVirtualBone(int index)
        {
            Undo.RecordObject(target, "Remove Virtual Bone");
            m_VirtualBones.DeleteArrayElementAtIndex(index);
            RegenerateRevision();
            GUIUtility.ExitGUI();
        }

        void MoveVirtualBone(int sourceIndex, int destinationIndex)
        {
            Undo.RecordObject(target, "Reorder Virtual Bone");
            m_VirtualBones.MoveArrayElement(sourceIndex, destinationIndex);
            RegenerateRevision();
            GUIUtility.ExitGUI();
        }

        void RegenerateRevision()
        {
            m_Revision.stringValue = Guid.NewGuid().ToString("N");
        }

        void DrawDiagnostics()
        {
            EditorGUILayout.Space(6f);
            m_ShowDiagnostics = EditorGUILayout.Foldout(m_ShowDiagnostics, "Diagnostics", true);
            if (!m_ShowDiagnostics)
                return;
            if (!string.IsNullOrWhiteSpace(m_ValidationMessage))
                EditorGUILayout.HelpBox(m_ValidationMessage, m_ValidationType);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(m_Schema, new GUIContent("Schema"));
                EditorGUILayout.PropertyField(m_RigId, new GUIContent("Rig Id"));
                EditorGUILayout.PropertyField(m_Revision, new GUIContent("Revision"));
            }
        }

        void RefreshValidation()
        {
            try
            {
                ((CharacterAnimationRigDefinition)target).RequireValid();
                m_ValidationMessage = "Rig v3 contract is valid.";
                m_ValidationType = MessageType.Info;
            }
            catch (Exception exception)
            {
                m_ValidationMessage = exception.Message;
                m_ValidationType = MessageType.Error;
            }
        }

        string[] GetPhysicalBoneIds()
        {
            var ids = new List<string>(m_PhysicalBones.arraySize);
            for (int i = 0; i < m_PhysicalBones.arraySize; i++)
            {
                string id = m_PhysicalBones.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("m_BoneId")
                    .stringValue;
                if (!string.IsNullOrWhiteSpace(id))
                    ids.Add(id);
            }
            return ids.ToArray();
        }

        static void DrawPhysicalPicker(
            string label,
            SerializedProperty property,
            string[] physicalIds,
            string excludedId)
        {
            var choices = new List<string>(physicalIds.Length);
            for (int i = 0; i < physicalIds.Length; i++)
            {
                if (!string.Equals(physicalIds[i], excludedId, StringComparison.Ordinal))
                    choices.Add(physicalIds[i]);
            }
            if (choices.Count == 0)
            {
                EditorGUILayout.LabelField(label, "Unavailable");
                return;
            }
            int current = choices.IndexOf(property.stringValue);
            string[] labels = new string[choices.Count + 1];
            labels[0] = "(Select Physical Bone)";
            for (int i = 0; i < choices.Count; i++)
                labels[i + 1] = ShortName(choices[i]);
            int selected = EditorGUILayout.Popup(label, current + 1, labels);
            if (selected > 0)
                property.stringValue = choices[selected - 1];
        }

        static string ShortName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "(Missing)";
            int separator = value.LastIndexOf('/');
            return separator >= 0 ? value.Substring(separator + 1) : value;
        }
    }
}
