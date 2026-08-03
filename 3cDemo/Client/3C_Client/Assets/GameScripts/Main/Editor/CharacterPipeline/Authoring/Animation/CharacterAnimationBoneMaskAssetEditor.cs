using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CustomEditor(typeof(CharacterAnimationBoneMaskAsset))]
    public sealed class CharacterAnimationBoneMaskAssetEditor : UnityEditor.Editor
    {
        SerializedProperty m_MaskId;
        SerializedProperty m_RigId;
        SerializedProperty m_RigRevision;
        SerializedProperty m_Weights;
        CharacterAnimationRigDefinition m_ResolvedRig;
        string m_ResolvedRigKey = string.Empty;
        bool m_RigResolutionReady;
        bool m_ShowDiagnostics;

        CharacterAnimationBoneMaskAsset Mask => target as CharacterAnimationBoneMaskAsset;

        void OnEnable()
        {
            m_MaskId = serializedObject.FindProperty("m_MaskId");
            m_RigId = serializedObject.FindProperty("m_RigId");
            m_RigRevision = serializedObject.FindProperty("m_RigRevision");
            m_Weights = serializedObject.FindProperty("m_Weights");
            EditorApplication.projectChanged += InvalidateRigResolution;
        }

        void OnDisable()
        {
            EditorApplication.projectChanged -= InvalidateRigResolution;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Animation Bone Mask", EditorStyles.boldLabel);
            ResolveRig();
            CharacterAnimationRigDefinition rig = (CharacterAnimationRigDefinition)EditorGUILayout.ObjectField(
                "Rig",
                m_ResolvedRig,
                typeof(CharacterAnimationRigDefinition),
                false);
            if (rig && rig != m_ResolvedRig)
            {
                ConfigureRig(rig);
                serializedObject.Update();
                ResolveRig();
                rig = m_ResolvedRig;
            }
            if (!rig)
            {
                EditorGUILayout.HelpBox("请选择明确的Animation Rig资产。", MessageType.Error);
            }
            else
            {
                DrawWeights(rig);
            }
            if (serializedObject.ApplyModifiedProperties())
                EditorUtility.SetDirty(target);
            DrawDiagnostics();
        }

        void DrawWeights(CharacterAnimationRigDefinition rig)
        {
            var entries = new Dictionary<string, SerializedProperty>(StringComparer.Ordinal);
            for (int i = 0; i < m_Weights.arraySize; i++)
            {
                SerializedProperty entry = m_Weights.GetArrayElementAtIndex(i);
                string id = entry.FindPropertyRelative("m_BoneId").stringValue;
                if (!string.IsNullOrWhiteSpace(id))
                    entries[id] = entry;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"Physical Bones ({rig.PhysicalBoneCount})", EditorStyles.boldLabel);
            for (int i = 0; i < rig.PhysicalBoneCount; i++)
                DrawWeight(rig.PhysicalBones[i].BoneId.Value, entries);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"Virtual Bones ({rig.VirtualBoneCount})", EditorStyles.boldLabel);
            for (int i = 0; i < rig.VirtualBoneCount; i++)
            {
                CharacterAnimationVirtualBoneDefinition bone = rig.VirtualBones[i];
                DrawWeight(bone.VirtualBoneId.Value, entries);
                EditorGUILayout.LabelField(
                    "Source / Target",
                    $"{bone.SourcePhysicalBoneId.Value} → {bone.TargetPhysicalBoneId.Value}",
                    EditorStyles.miniLabel);
            }
        }

        static void DrawWeight(
            string boneId,
            IReadOnlyDictionary<string, SerializedProperty> entries)
        {
            if (!entries.TryGetValue(boneId, out SerializedProperty entry))
            {
                EditorGUILayout.HelpBox($"缺少明确权重：{boneId}", MessageType.Error);
                return;
            }
            SerializedProperty weight = entry.FindPropertyRelative("m_Weight");
            EditorGUILayout.Slider(weight, 0f, 1f, new GUIContent(boneId));
        }

        void ConfigureRig(CharacterAnimationRigDefinition rig)
        {
            CharacterAnimationBoneMaskAsset mask = Mask;
            var current = new Dictionary<AnimationBoneId, float>();
            for (int i = 0; i < mask.Weights.Count; i++)
            {
                CharacterAnimationBoneWeight weight = mask.Weights[i];
                if (weight != null && weight.BoneId.IsValid)
                    current[weight.BoneId] = weight.Weight;
            }
            var weights = new CharacterAnimationBoneWeight[rig.PoseBoneCount];
            for (int i = 0; i < weights.Length; i++)
            {
                AnimationBoneId boneId = rig.GetPoseBoneId(i);
                weights[i] = new CharacterAnimationBoneWeight(
                    boneId,
                    current.TryGetValue(boneId, out float value) ? value : 1f);
            }
            Undo.RecordObject(mask, "Configure Animation Bone Mask Rig");
            mask.Configure(
                string.IsNullOrWhiteSpace(mask.MaskId) ? mask.name : mask.MaskId,
                rig,
                weights);
            EditorUtility.SetDirty(mask);
            InvalidateRigResolution();
        }

        void DrawDiagnostics()
        {
            m_ShowDiagnostics = EditorGUILayout.Foldout(m_ShowDiagnostics, "Diagnostics", true);
            if (!m_ShowDiagnostics)
                return;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(m_MaskId, new GUIContent("Mask Id"));
                EditorGUILayout.PropertyField(m_RigId, new GUIContent("Rig Id"));
                EditorGUILayout.PropertyField(m_RigRevision, new GUIContent("Rig Revision"));
            }
        }

        void ResolveRig()
        {
            string rigId = m_RigId.stringValue;
            string revision = m_RigRevision.stringValue;
            string key = $"{rigId}\n{revision}";
            if (m_RigResolutionReady && string.Equals(m_ResolvedRigKey, key, StringComparison.Ordinal))
                return;
            m_RigResolutionReady = true;
            m_ResolvedRigKey = key;
            m_ResolvedRig = null;
            CharacterAnimationRigDefinition result = null;
            string[] guids = AssetDatabase.FindAssets("t:CharacterAnimationRigDefinition");
            Array.Sort(guids, StringComparer.Ordinal);
            for (int i = 0; i < guids.Length; i++)
            {
                CharacterAnimationRigDefinition candidate =
                    AssetDatabase.LoadAssetAtPath<CharacterAnimationRigDefinition>(
                        AssetDatabase.GUIDToAssetPath(guids[i]));
                if (!candidate ||
                    !string.Equals(candidate.RigId, rigId, StringComparison.Ordinal) ||
                    !string.Equals(candidate.Revision, revision, StringComparison.Ordinal))
                {
                    continue;
                }
                if (result)
                    return;
                result = candidate;
            }
            m_ResolvedRig = result;
        }

        void InvalidateRigResolution()
        {
            m_RigResolutionReady = false;
            m_ResolvedRigKey = string.Empty;
            m_ResolvedRig = null;
        }
    }
}
