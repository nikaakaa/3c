using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterAnimationRigPrefabAnalysis
    {
        internal CharacterAnimationRigPrefabAnalysis(
            Mesh mesh,
            CharacterAnimationMeshContentIdentity meshIdentity,
            IReadOnlyList<CharacterAnimationPropertyImportPrefabTarget> prefabs)
        {
            Mesh = mesh ? mesh : throw new ArgumentNullException(nameof(mesh));
            MeshIdentity = meshIdentity;
            Prefabs = prefabs ?? throw new ArgumentNullException(nameof(prefabs));
        }

        internal Mesh Mesh { get; }
        internal CharacterAnimationMeshContentIdentity MeshIdentity { get; }
        internal IReadOnlyList<CharacterAnimationPropertyImportPrefabTarget> Prefabs { get; }
    }

    internal static class CharacterAnimationRigPrefabAnalyzer
    {
        const string RuntimeProfilePrefabRoot = "Assets/Prefabs/Characters/RuntimeProfiles/";

        internal static CharacterAnimationRigPrefabAnalysis Analyze(
            CharacterAnimationRigDefinition rig,
            string rendererBindingId,
            string animationCurvePath)
        {
            var targets = new List<CharacterAnimationPropertyImportPrefabTarget>();
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            Array.Sort(prefabGuids, StringComparer.Ordinal);
            Mesh mesh = null;
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (!path.StartsWith(RuntimeProfilePrefabRoot, StringComparison.Ordinal))
                    continue;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!prefab)
                    continue;
                CharacterAnimationRigBinding[] matches = prefab
                    .GetComponentsInChildren<CharacterAnimationRigBinding>(true)
                    .Where(value => value &&
                                    CharacterAnimationRigRendererResolver.IsOwnedByPrefabAsset(value.gameObject, path) &&
                                    string.Equals(value.RigId, rig.RigId, StringComparison.Ordinal))
                    .ToArray();
                if (matches.Length == 0)
                    continue;
                if (matches.Length != 1)
                    throw new InvalidOperationException(
                        $"Prefab '{path}' contains {matches.Length} Animation Rig Bindings for Rig '{rig.RigId}'.");
                CharacterAnimationRigBinding binding = matches[0];
                if (!string.Equals(binding.RigRevision, rig.Revision, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"Prefab '{path}' Rig revision '{binding.RigRevision}' does not match '{rig.Revision}'.");
                SkinnedMeshRenderer renderer = CharacterAnimationRigRendererResolver.Require(
                    binding.Animator,
                    animationCurvePath,
                    path);
                if (mesh == null)
                    mesh = renderer.sharedMesh;
                if (renderer.sharedMesh != mesh)
                    throw new InvalidOperationException(
                        $"Prefab '{path}' Renderer '{animationCurvePath}' does not use the common Mesh.");
                CharacterAnimationRendererBinding current =
                    binding.FindRendererBinding(rendererBindingId);
                string currentMeshGuid = string.Empty;
                long currentMeshLocalFileId = 0;
                string currentMeshContentHash = string.Empty;
                if (current?.ExpectedMesh &&
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                        current.ExpectedMesh,
                        out currentMeshGuid,
                        out currentMeshLocalFileId))
                {
                    currentMeshContentHash = current.MeshContentHash;
                }
                string currentRendererPath = current?.Renderer
                    ? AnimationUtility.CalculateTransformPath(
                        current.Renderer.transform,
                        binding.Animator.transform)
                    : string.Empty;
                targets.Add(new CharacterAnimationPropertyImportPrefabTarget(
                    path,
                    binding.RigId,
                    binding.RigRevision,
                    AnimationUtility.CalculateTransformPath(
                        renderer.transform,
                        binding.Animator.transform),
                    rendererBindingId,
                    current?.BindingId,
                    currentRendererPath,
                    currentMeshGuid,
                    currentMeshLocalFileId,
                    currentMeshContentHash,
                    string.Empty,
                    0,
                    string.Empty));
            }
            if (mesh == null || targets.Count == 0)
                throw new InvalidOperationException(
                    $"No persisted Prefab with Animation Rig Id '{rig.RigId}' and Renderer '{animationCurvePath}' was found.");
            CharacterAnimationMeshContentIdentity meshIdentity =
                CharacterAnimationMeshContentIdentity.Resolve(mesh);
            for (int i = 0; i < targets.Count; i++)
            {
                CharacterAnimationPropertyImportPrefabTarget current = targets[i];
                targets[i] = new CharacterAnimationPropertyImportPrefabTarget(
                    current.AssetPath,
                    current.RigId,
                    current.RigRevision,
                    current.RendererPath,
                    current.BindingId,
                    current.CurrentBindingId,
                    current.CurrentRendererPath,
                    current.CurrentMeshGuid,
                    current.CurrentMeshLocalFileId,
                    current.CurrentMeshContentHash,
                    meshIdentity.AssetGuid,
                    meshIdentity.LocalFileId,
                    meshIdentity.ContentHash);
            }
            targets.Sort((left, right) => string.CompareOrdinal(left.AssetPath, right.AssetPath));
            return new CharacterAnimationRigPrefabAnalysis(mesh, meshIdentity, targets.ToArray());
        }
    }
}
