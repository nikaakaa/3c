using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterAnimationRigRendererResolver
    {
        internal static bool IsOwnedByPrefabAsset(GameObject gameObject, string assetPath)
        {
            string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
            return string.IsNullOrEmpty(sourcePath) ||
                   string.Equals(
                       sourcePath.Replace('\\', '/'),
                       assetPath.Replace('\\', '/'),
                       StringComparison.Ordinal);
        }

        internal static SkinnedMeshRenderer Require(
            Animator animator,
            string rendererPath,
            string assetPath)
        {
            if (!animator)
                throw new InvalidOperationException($"Asset '{assetPath}' Animation Rig Binding has no Animator.");
            if (string.IsNullOrEmpty(rendererPath))
                throw new InvalidOperationException($"Asset '{assetPath}' Renderer path is empty.");

            SkinnedMeshRenderer[] candidates = animator
                .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(value => value && string.Equals(
                    AnimationUtility.CalculateTransformPath(value.transform, animator.transform),
                    rendererPath,
                    StringComparison.Ordinal))
                .ToArray();
            if (candidates.Length != 1)
                throw new InvalidOperationException(
                    $"Asset '{assetPath}' has {candidates.Length} SkinnedMeshRenderer candidates at exact path '{rendererPath}' below its Animator.");
            if (!candidates[0].sharedMesh)
                throw new InvalidOperationException(
                    $"Asset '{assetPath}' SkinnedMeshRenderer at '{rendererPath}' has no Mesh.");
            return candidates[0];
        }
    }
}
