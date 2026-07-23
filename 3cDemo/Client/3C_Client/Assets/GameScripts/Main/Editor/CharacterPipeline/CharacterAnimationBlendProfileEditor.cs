using System;
using System.Globalization;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CustomEditor(typeof(CharacterAnimationBlendProfile))]
    public sealed class CharacterAnimationBlendProfileEditor : UnityEditor.Editor
    {
        SerializedProperty m_Schema;
        SerializedProperty m_ProfileId;
        SerializedProperty m_RigId;
        SerializedProperty m_RigRevision;
        SerializedProperty m_GlobalDurationMultiplier;
        SerializedProperty m_BoneOverrides;
        CharacterAnimationRigDefinition m_ResolvedRig;
        string m_ResolvedRigKey = string.Empty;
        string m_RigResolutionError = string.Empty;
        bool m_RigResolutionReady;

        CharacterAnimationBlendProfile Profile => target as CharacterAnimationBlendProfile;

        void OnEnable()
        {
            m_Schema = serializedObject.FindProperty("m_Schema");
            m_ProfileId = serializedObject.FindProperty("m_ProfileId");
            m_RigId = serializedObject.FindProperty("m_RigId");
            m_RigRevision = serializedObject.FindProperty("m_RigRevision");
            m_GlobalDurationMultiplier = serializedObject.FindProperty("m_GlobalDurationMultiplier");
            m_BoneOverrides = serializedObject.FindProperty("m_BoneOverrides");
            EditorApplication.projectChanged += InvalidateRigResolution;
        }

        void OnDisable()
        {
            EditorApplication.projectChanged -= InvalidateRigResolution;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Animation Blend Profile", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(m_Schema, new GUIContent("Schema"));
            EditorGUILayout.PropertyField(m_ProfileId, new GUIContent("Profile Id"));
            EditorGUILayout.PropertyField(m_RigId, new GUIContent("Rig Id"));
            EditorGUILayout.PropertyField(m_RigRevision, new GUIContent("Rig Revision"));
            EditorGUILayout.PropertyField(m_GlobalDurationMultiplier, new GUIContent("Global Duration Multiplier"));
            EditorGUILayout.PropertyField(m_BoneOverrides, new GUIContent("Bone Overrides"), true);
            if (serializedObject.ApplyModifiedProperties())
                InvalidateRigResolution();

            DrawResolvedMultipliers();
        }

        void DrawResolvedMultipliers()
        {
            CharacterAnimationBlendProfile profile = Profile;
            if (!profile)
                return;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Resolved Bone Multipliers", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Rig Identity", $"{profile.RigId} @ {profile.RigRevision}");
            ResolveRig(profile);
            if (!m_ResolvedRig)
            {
                EditorGUILayout.HelpBox(m_RigResolutionError, MessageType.Error);
                return;
            }

            float[] dense;
            try
            {
                dense = profile.BuildDense(m_ResolvedRig);
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Rig Asset", m_ResolvedRig, typeof(CharacterAnimationRigDefinition), false);
            EditorGUILayout.LabelField("Bone Count", dense.Length.ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Bone Id", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("Final Multiplier", EditorStyles.miniBoldLabel, GUILayout.Width(120f));
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < dense.Length; i++)
            {
                CharacterAnimationBoneDefinition bone = m_ResolvedRig.Bones[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(bone.BoneId.Value);
                EditorGUILayout.LabelField(
                    dense[i].ToString("R", CultureInfo.InvariantCulture),
                    GUILayout.Width(120f));
                EditorGUILayout.EndHorizontal();
            }
        }

        void ResolveRig(CharacterAnimationBlendProfile profile)
        {
            string key = $"{profile.RigId}\n{profile.RigRevision}";
            if (m_RigResolutionReady && string.Equals(m_ResolvedRigKey, key, StringComparison.Ordinal))
                return;

            m_RigResolutionReady = true;
            m_ResolvedRigKey = key;
            m_ResolvedRig = null;
            m_RigResolutionError = string.Empty;
            if (string.IsNullOrWhiteSpace(profile.RigId) || string.IsNullOrWhiteSpace(profile.RigRevision))
            {
                m_RigResolutionError = "Blend Profile requires an explicit Rig Id and Rig Revision.";
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:CharacterAnimationRigDefinition");
            Array.Sort(guids, StringComparer.Ordinal);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                CharacterAnimationRigDefinition candidate =
                    AssetDatabase.LoadAssetAtPath<CharacterAnimationRigDefinition>(path);
                if (!candidate ||
                    !string.Equals(candidate.RigId, profile.RigId, StringComparison.Ordinal) ||
                    !string.Equals(candidate.Revision, profile.RigRevision, StringComparison.Ordinal))
                {
                    continue;
                }

                if (m_ResolvedRig)
                {
                    m_RigResolutionError =
                        $"Rig identity '{profile.RigId}@{profile.RigRevision}' is duplicated by '{AssetDatabase.GetAssetPath(m_ResolvedRig)}' and '{path}'.";
                    m_ResolvedRig = null;
                    return;
                }
                m_ResolvedRig = candidate;
            }

            if (!m_ResolvedRig)
            {
                m_RigResolutionError =
                    $"Rig identity '{profile.RigId}@{profile.RigRevision}' does not resolve to a CharacterAnimationRigDefinition asset.";
            }
        }

        void InvalidateRigResolution()
        {
            m_RigResolutionReady = false;
            m_ResolvedRigKey = string.Empty;
            m_ResolvedRig = null;
            m_RigResolutionError = string.Empty;
        }
    }
}
