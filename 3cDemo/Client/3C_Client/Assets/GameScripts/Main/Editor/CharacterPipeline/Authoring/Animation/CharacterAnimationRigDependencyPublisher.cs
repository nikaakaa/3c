using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterAnimationRigDependencyPublisher
    {
        const string PublishAllMenu = "Tools/3C/Character/Publish Rig Revisions To Authoring Dependencies";

        [MenuItem(PublishAllMenu)]
        static void SchedulePublishAll()
        {
            EditorApplication.delayCall += PublishAll;
        }

        public static void PublishAll()
        {
            Dictionary<string, CharacterAnimationRigDefinition> rigs = LoadRigs();
            int profileCount = PublishBlendProfiles(rigs);
            int prefabCount = PublishPrefabBindings(rigs);
            Debug.Log(
                $"Published current Animation Rig revisions to {profileCount} Blend Profile(s) and {prefabCount} Prefab Binding(s).");
        }

        static Dictionary<string, CharacterAnimationRigDefinition> LoadRigs()
        {
            string[] guids = AssetDatabase.FindAssets("t:CharacterAnimationRigDefinition");
            Array.Sort(guids, StringComparer.Ordinal);
            var result = new Dictionary<string, CharacterAnimationRigDefinition>(StringComparer.Ordinal);
            for (int i = 0; i < guids.Length; i++)
            {
                CharacterAnimationRigDefinition rig =
                    AssetDatabase.LoadAssetAtPath<CharacterAnimationRigDefinition>(
                        AssetDatabase.GUIDToAssetPath(guids[i]));
                if (!rig)
                    continue;
                try
                {
                    rig.RequireValid();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        $"Skipped Rig dependency publication for '{AssetDatabase.GetAssetPath(rig)}': {exception.Message}");
                    continue;
                }
                if (!result.TryAdd(rig.RigId, rig))
                    throw new InvalidOperationException($"Animation Rig Id '{rig.RigId}' is duplicated.");
            }
            return result;
        }

        static int PublishBlendProfiles(
            IReadOnlyDictionary<string, CharacterAnimationRigDefinition> rigs)
        {
            string[] guids = AssetDatabase.FindAssets("t:CharacterAnimationBlendProfile");
            Array.Sort(guids, StringComparer.Ordinal);
            int count = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                CharacterAnimationBlendProfile profile =
                    AssetDatabase.LoadAssetAtPath<CharacterAnimationBlendProfile>(
                        AssetDatabase.GUIDToAssetPath(guids[i]));
                if (!profile || !rigs.TryGetValue(profile.RigId, out CharacterAnimationRigDefinition rig))
                    continue;
                if (string.Equals(profile.RigRevision, rig.Revision, StringComparison.Ordinal))
                    continue;
                var overrides = new CharacterAnimationBoneDurationMultiplier[profile.BoneOverrides.Count];
                for (int boneIndex = 0; boneIndex < overrides.Length; boneIndex++)
                {
                    CharacterAnimationBoneDurationMultiplier value = profile.BoneOverrides[boneIndex];
                    if (value == null)
                        throw new InvalidOperationException($"Animation Blend Profile '{profile.name}' has a missing Bone override.");
                    overrides[boneIndex] = new CharacterAnimationBoneDurationMultiplier(
                        value.BoneId,
                        value.Multiplier);
                }
                profile.Configure(
                    profile.ProfileId,
                    rig,
                    profile.GlobalDurationMultiplier,
                    overrides);
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
                count++;
            }
            return count;
        }

        static int PublishPrefabBindings(
            IReadOnlyDictionary<string, CharacterAnimationRigDefinition> rigs)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            Array.Sort(guids, StringComparer.Ordinal);
            int count = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!prefab)
                    continue;
                CharacterAnimationRigBinding[] existing =
                    prefab.GetComponentsInChildren<CharacterAnimationRigBinding>(true);
                bool requiresPublish = false;
                for (int bindingIndex = 0; bindingIndex < existing.Length; bindingIndex++)
                {
                    CharacterAnimationRigBinding binding = existing[bindingIndex];
                    if (rigs.TryGetValue(binding.RigId, out CharacterAnimationRigDefinition rig) &&
                        !string.Equals(binding.RigRevision, rig.Revision, StringComparison.Ordinal))
                    {
                        requiresPublish = true;
                        break;
                    }
                }
                if (!requiresPublish)
                    continue;

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    CharacterAnimationRigBinding[] bindings =
                        root.GetComponentsInChildren<CharacterAnimationRigBinding>(true);
                    bool changed = false;
                    for (int bindingIndex = 0; bindingIndex < bindings.Length; bindingIndex++)
                    {
                        CharacterAnimationRigBinding binding = bindings[bindingIndex];
                        if (!rigs.TryGetValue(binding.RigId, out CharacterAnimationRigDefinition rig) ||
                            string.Equals(binding.RigRevision, rig.Revision, StringComparison.Ordinal))
                        {
                            continue;
                        }
                        CharacterAnimationRigPayload payload = new CharacterAnimationRigPayload(rig);
                        if (binding.PhysicalBones.Count != payload.PhysicalBoneCount)
                        {
                            throw new InvalidOperationException(
                                $"Prefab '{path}' Rig Binding Bone count does not match '{rig.RigId}@{rig.Revision}'.");
                        }
                        var physicalBones = new Transform[binding.PhysicalBones.Count];
                        for (int boneIndex = 0; boneIndex < physicalBones.Length; boneIndex++)
                            physicalBones[boneIndex] = binding.PhysicalBones[boneIndex];
                        binding.Configure(binding.Animator, payload, physicalBones);
                        EditorUtility.SetDirty(binding);
                        changed = true;
                        count++;
                    }
                    if (changed)
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            return count;
        }
    }
}
