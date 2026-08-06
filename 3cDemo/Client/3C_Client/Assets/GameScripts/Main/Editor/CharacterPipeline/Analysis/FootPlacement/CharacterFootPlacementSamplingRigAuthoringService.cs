using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterFootPlacementSamplingRigAuthoringService
    {
        const string RebuildAllMenu = "Tools/Character Pipeline/Foot Placement/Rebuild All Geometry Validations";

        public static void SynchronizeBinding(CharacterFootPlacementAnalysisSource source)
        {
            if (!source)
                throw new ArgumentNullException(nameof(source));
            source.RequireCalibrationAuthoringInput();
            string path = RequireSamplingRigPath(source);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                CharacterAnimationRigBinding[] bindings =
                    root.GetComponentsInChildren<CharacterAnimationRigBinding>(true);
                Animator[] animators = root.GetComponentsInChildren<Animator>(true);
                if (bindings.Length != 1 || animators.Length != 1)
                {
                    throw new InvalidOperationException(
                        $"Sampling Rig requires exactly one Animation Rig Binding and Animator; found {bindings.Length}/{animators.Length}.");
                }

                CharacterAnimationRigBinding binding = bindings[0];
                Animator animator = animators[0];
                if (binding.Animator != animator)
                    throw new InvalidOperationException("Sampling Rig Animation Rig Binding and Animator do not match exactly.");
                CharacterAnimationRigPayload rig = new CharacterAnimationRigPayload(source.RigDefinition);
                if (string.Equals(binding.RigId, rig.RigId, StringComparison.Ordinal) &&
                    string.Equals(binding.RigRevision, rig.RigRevision, StringComparison.Ordinal))
                {
                    binding.RequireValid(rig);
                    return;
                }
                if (!string.Equals(binding.RigId, rig.RigId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Sampling Rig Binding belongs to a different Animation Rig identity.");
                if (binding.PhysicalBones.Count != rig.PhysicalBoneCount)
                    throw new InvalidOperationException("Sampling Rig Binding physical Bone count does not match the current Rig Definition.");

                var physicalBones = new Transform[binding.PhysicalBones.Count];
                for (int i = 0; i < physicalBones.Length; i++)
                    physicalBones[i] = binding.PhysicalBones[i];
                binding.Configure(animator, rig, physicalBones);
                EditorUtility.SetDirty(binding);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        public static CharacterFootPlacementRigGeometryValidationIdentity RebuildGeometryValidation(
            CharacterFootPlacementAnalysisSource source)
        {
            SynchronizeBinding(source);
            CharacterFootPlacementRigGeometryReport report =
                CharacterFootPlacementAnimationAnalyzer.EvaluateCalibrationGeometry(source);
            CharacterFootPlacementRigGeometryValidationIdentity identity =
                CharacterFootPlacementRigGeometryValidationPublisher.Publish(source, report);
            EditorUtility.SetDirty(source.RigCalibration);
            AssetDatabase.SaveAssetIfDirty(source.RigCalibration);
            return identity;
        }

        [MenuItem(RebuildAllMenu)]
        static void RebuildAllGeometryValidations()
        {
            string[] guids = AssetDatabase.FindAssets("t:CharacterFootPlacementAnalysisSource");
            Array.Sort(guids, StringComparer.Ordinal);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                CharacterFootPlacementAnalysisSource source =
                    AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(path);
                if (source)
                    RebuildGeometryValidation(source);
            }
            Debug.Log($"Rebuilt {guids.Length} Foot Placement geometry validation asset(s).");
        }

        static string RequireSamplingRigPath(CharacterFootPlacementAnalysisSource source)
        {
            string path = AssetDatabase.GUIDToAssetPath(source.SamplingRigAssetGuid);
            if (string.IsNullOrEmpty(path) || !AssetDatabase.LoadAssetAtPath<GameObject>(path))
                throw new InvalidOperationException("Foot Placement Sampling Rig does not resolve to a Prefab asset.");
            return path;
        }
    }
}
