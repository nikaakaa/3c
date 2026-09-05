using System;
using System.Globalization;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public readonly struct CharacterAnimationMeshContentIdentity
    {
        public const string Schema = "character-animation-mesh-content/v1";

        CharacterAnimationMeshContentIdentity(
            string assetPath,
            string assetGuid,
            long localFileId,
            string dependencyHash,
            string contentHash)
        {
            AssetPath = assetPath;
            AssetGuid = assetGuid;
            LocalFileId = localFileId;
            DependencyHash = dependencyHash;
            ContentHash = contentHash;
        }

        public string AssetPath { get; }
        public string AssetGuid { get; }
        public long LocalFileId { get; }
        public string DependencyHash { get; }
        public string ContentHash { get; }

        public static CharacterAnimationMeshContentIdentity Resolve(Mesh mesh)
        {
            if (!mesh)
                throw new ArgumentNullException(nameof(mesh));
            string assetPath = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrWhiteSpace(assetPath) ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    mesh,
                    out string assetGuid,
                    out long localFileId) ||
                string.IsNullOrWhiteSpace(assetGuid) ||
                localFileId == 0)
            {
                throw new InvalidOperationException(
                    $"Mesh '{mesh.name}' does not have a stable asset identity.");
            }
            string dependencyHash = AssetDatabase.GetAssetDependencyHash(assetPath).ToString();
            if (string.IsNullOrWhiteSpace(dependencyHash))
                throw new InvalidOperationException(
                    $"Mesh '{mesh.name}' does not have a dependency hash.");
            return new CharacterAnimationMeshContentIdentity(
                assetPath,
                assetGuid,
                localFileId,
                dependencyHash,
                CharacterAclHash.ComputeStrings(new[]
                {
                    Schema,
                    assetGuid,
                    localFileId.ToString(CultureInfo.InvariantCulture),
                    dependencyHash
                }));
        }

        public static void RequireCurrent(Mesh mesh, string expectedContentHash)
        {
            CharacterAnimationMeshContentIdentity actual = Resolve(mesh);
            if (!string.Equals(actual.ContentHash, expectedContentHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Mesh '{mesh.name}' content hash changed from '{expectedContentHash}' to '{actual.ContentHash}'.");
            }
        }
    }
}
