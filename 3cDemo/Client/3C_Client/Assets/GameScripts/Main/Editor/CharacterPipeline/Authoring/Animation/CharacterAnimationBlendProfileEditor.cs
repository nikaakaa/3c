using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CustomEditor(typeof(CharacterAnimationBlendProfile))]
    public sealed class CharacterAnimationBlendProfileEditor : UnityEditor.Editor
    {
        CharacterAnimationRigDefinition m_ResolvedRig;
        string m_ResolvedRigKey = string.Empty;
        string m_RigResolutionError = string.Empty;
        bool m_RigResolutionReady;
        bool m_ShowDiagnostics;

        CharacterAnimationBlendProfile Profile => target as CharacterAnimationBlendProfile;

        void OnEnable()
        {
            EditorApplication.projectChanged += InvalidateRigResolution;
        }

        void OnDisable()
        {
            EditorApplication.projectChanged -= InvalidateRigResolution;
        }

        public override void OnInspectorGUI()
        {
            CharacterAnimationBlendProfile profile = Profile;
            if (!profile)
                return;

            EditorGUILayout.LabelField("Animation Blend Profile", EditorStyles.boldLabel);
            ResolveRig(profile);
            DrawAuthoring(profile);
            DrawDiagnostics(profile);
        }

        void DrawAuthoring(CharacterAnimationBlendProfile profile)
        {
            CharacterAnimationRigDefinition rig = (CharacterAnimationRigDefinition)EditorGUILayout.ObjectField(
                "Rig",
                m_ResolvedRig,
                typeof(CharacterAnimationRigDefinition),
                false);
            if (!rig)
            {
                EditorGUILayout.HelpBox(m_RigResolutionError, MessageType.Error);
                return;
            }

            var current = new Dictionary<AnimationBoneId, float>();
            for (int i = 0; i < profile.BoneOverrides.Count; i++)
            {
                CharacterAnimationBoneDurationMultiplier value = profile.BoneOverrides[i];
                if (value != null && value.BoneId.IsValid)
                    current[value.BoneId] = value.Multiplier;
            }

            EditorGUI.BeginChangeCheck();
            float globalMultiplier = EditorGUILayout.FloatField(
                "Global Duration Multiplier",
                profile.GlobalDurationMultiplier);
            bool valid = float.IsFinite(globalMultiplier) && globalMultiplier > 0f;
            if (!valid)
                EditorGUILayout.HelpBox("Global Duration Multiplier must be greater than zero.", MessageType.Error);
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Bone Duration Multipliers", EditorStyles.boldLabel);
            var overrides = new CharacterAnimationBoneDurationMultiplier[rig.PoseBoneCount];
            for (int i = 0; i < rig.PoseBoneCount; i++)
            {
                if (i == 0)
                    EditorGUILayout.LabelField("Physical Bones", EditorStyles.boldLabel);
                if (i == rig.PhysicalBoneCount)
                    EditorGUILayout.LabelField("Virtual Bones", EditorStyles.boldLabel);
                AnimationBoneId boneId = rig.GetPoseBoneId(i);
                float multiplier = current.TryGetValue(boneId, out float existing) ? existing : 1f;
                multiplier = EditorGUILayout.FloatField(boneId.Value, multiplier);
                if (!float.IsFinite(multiplier) || multiplier <= 0f)
                {
                    EditorGUILayout.HelpBox($"{boneId.Value} must be greater than zero.", MessageType.Error);
                    valid = false;
                }
                if (valid)
                    overrides[i] = new CharacterAnimationBoneDurationMultiplier(boneId, multiplier);
            }

            bool changed = EditorGUI.EndChangeCheck() || rig != m_ResolvedRig;
            if (!changed || !valid)
                return;

            try
            {
                Undo.RecordObject(profile, "Configure Animation Blend Profile");
                profile.Configure(
                    string.IsNullOrWhiteSpace(profile.ProfileId) ? profile.name : profile.ProfileId,
                    rig,
                    globalMultiplier,
                    overrides);
                EditorUtility.SetDirty(profile);
                InvalidateRigResolution();
                ResolveRig(profile);
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
            }
        }

        void DrawDiagnostics(CharacterAnimationBlendProfile profile)
        {
            EditorGUILayout.Space(6f);
            m_ShowDiagnostics = EditorGUILayout.Foldout(m_ShowDiagnostics, "Diagnostics", true);
            if (!m_ShowDiagnostics)
                return;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Schema", profile.Schema);
                EditorGUILayout.TextField("Profile Id", profile.ProfileId);
                EditorGUILayout.TextField("Rig Id", profile.RigId);
                EditorGUILayout.TextField("Rig Revision", profile.RigRevision);
                EditorGUILayout.IntField("Bone Count", m_ResolvedRig ? m_ResolvedRig.PoseBoneCount : 0);
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
