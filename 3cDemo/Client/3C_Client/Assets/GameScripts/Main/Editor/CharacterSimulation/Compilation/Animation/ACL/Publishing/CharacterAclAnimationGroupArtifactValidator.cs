using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterAclAnimationGroupArtifactValidator
    {
        internal static bool TryReuse(
            CharacterAclAnimationPublishGroup group,
            string folder,
            CharacterAclPublishedGroupInventoryEntry entry)
        {
            try
            {
                Validate(group, folder, entry);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static void Validate(
            CharacterAclAnimationPublishGroup group,
            string folder,
            CharacterAclPublishedGroupInventoryEntry entry)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));
            if (entry == null || !entry.Resource)
                throw new InvalidOperationException(
                    $"ACL group '{group.AssetStem}' has no readable resource asset.");
            if (!string.Equals(
                    entry.Stem,
                    group.AssetStem,
                    StringComparison.Ordinal) ||
                !entry.Resource.MatchesDescriptor(group.Descriptor) ||
                entry.Resource.Manifest == null ||
                !string.Equals(
                    entry.Resource.Manifest.ResourceAddress,
                    group.AssetStem,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    entry.Resource.Manifest.GroupContentHash,
                    group.Descriptor.GroupContentHash,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"ACL group '{group.AssetStem}' manifest does not match its compiled descriptor.");
            }
            RequireInventory(entry, group);
            CharacterAclResourceReadinessResult readiness =
                entry.Resource.ValidateRuntime();
            if (!readiness.IsReady)
                throw new InvalidOperationException(
                    $"ACL group '{group.AssetStem}' failed runtime validation: {readiness.Message}");
            for (int i = 0; i < group.Descriptor.ClipManifests.Count; i++)
            {
                CharacterAclAnimationResourceManifest manifest =
                    entry.Resource.GetGroupManifest(i);
                CharacterAclAnimationResourceManifest expected =
                    group.Descriptor.RequireManifest(i);
                if (manifest.GroupClipIndex != i ||
                    !string.Equals(
                        manifest.ResourceAddress,
                        group.AssetStem,
                        StringComparison.Ordinal) ||
                    !string.Equals(manifest.ResourceIdentity, expected.ResourceIdentity, StringComparison.Ordinal) ||
                    !string.Equals(manifest.NativeArtifactIdentity, expected.NativeArtifactIdentity, StringComparison.Ordinal) ||
                    !string.Equals(manifest.NativeBinarySha256, expected.NativeBinarySha256, StringComparison.Ordinal) ||
                    !string.Equals(manifest.NativePlatform, expected.NativePlatform, StringComparison.Ordinal) ||
                    manifest.NativeAbiVersion != expected.NativeAbiVersion ||
                    manifest.NativePayloadFormatVersion != expected.NativePayloadFormatVersion)
                {
                    throw new InvalidOperationException(
                        $"ACL group '{group.AssetStem}' contains an invalid manifest mapping.");
                }
                if (!group.Artifact.HasPayloads)
                    continue;
                RequireEqualPayload(
                    entry.Resource.RequirePayload(
                        CharacterAclDataBlockKind.Transform,
                        i),
                    group.Artifact.TransformPayloads[i],
                    group.AssetStem);
                RequireEqualPayload(
                    entry.Resource.RequirePayload(
                        CharacterAclDataBlockKind.Scalar,
                        i),
                    group.Artifact.ScalarPayloads[i],
                    group.AssetStem);
                RequirePayloadFile(
                    $"{folder}/{group.AssetStem}.transform.{i}.bytes",
                    group.Artifact.TransformPayloads[i],
                    group.AssetStem);
                RequirePayloadFile(
                    $"{folder}/{group.AssetStem}.scalar.{i}.bytes",
                    group.Artifact.ScalarPayloads[i],
                    group.AssetStem);
            }
            if (!group.Artifact.HasPayloads)
                return;
            RequireEqualPayload(
                entry.Resource.RequirePayload(
                    CharacterAclDataBlockKind.DatabaseHeader,
                    0),
                group.Artifact.DatabaseHeaderPayload,
                group.AssetStem);
            RequireEqualPayload(
                entry.Resource.RequirePayload(
                    CharacterAclDataBlockKind.BulkMedium,
                    0),
                group.Artifact.BulkMediumPayload,
                group.AssetStem);
            RequireEqualPayload(
                entry.Resource.RequirePayload(
                    CharacterAclDataBlockKind.BulkLow,
                    0),
                group.Artifact.BulkLowPayload,
                group.AssetStem);
            RequirePayloadFile(
                $"{folder}/{group.AssetStem}.database.bytes",
                group.Artifact.DatabaseHeaderPayload,
                group.AssetStem);
            RequirePayloadFile(
                $"{folder}/{group.AssetStem}.medium.bytes",
                group.Artifact.BulkMediumPayload,
                group.AssetStem);
            RequirePayloadFile(
                $"{folder}/{group.AssetStem}.low.bytes",
                group.Artifact.BulkLowPayload,
                group.AssetStem);
            string reportPath = $"{folder}/{group.AssetStem}.quality.json";
            string expectedReport = JsonUtility.ToJson(
                new ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL.CharacterAclAnimationQualityBundle(
                    group.Artifact.QualityReports),
                true);
            if (!File.Exists(
                    CharacterAclAnimationArtifactFileStore.ToProjectFilePath(
                        reportPath)) ||
                !string.Equals(
                    CharacterAclAnimationArtifactFileStore.ReadAllTextUtf8(
                        reportPath),
                    expectedReport,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"ACL group '{group.AssetStem}' quality report does not match its artifact.");
            }
        }

        static void RequireInventory(
            CharacterAclPublishedGroupInventoryEntry entry,
            CharacterAclAnimationPublishGroup group)
        {
            var expected = new HashSet<string>(StringComparer.Ordinal)
            {
                $"{entry.Folder}/{group.AssetStem}.asset",
                $"{entry.Folder}/{group.AssetStem}.quality.json"
            };
            for (int i = 0; i < group.Artifact.Manifests.Length; i++)
            {
                expected.Add(
                    $"{entry.Folder}/{group.AssetStem}.transform.{i}.bytes");
                if (group.Artifact.Manifests[i].Scalar.Exists)
                    expected.Add(
                        $"{entry.Folder}/{group.AssetStem}.scalar.{i}.bytes");
            }
            CharacterAclAnimationResourceManifest manifest = group.Artifact.Manifests[0];
            if (manifest.DatabaseHeader.Exists)
                expected.Add(
                    $"{entry.Folder}/{group.AssetStem}.database.bytes");
            if (manifest.BulkMedium.Exists)
                expected.Add(
                    $"{entry.Folder}/{group.AssetStem}.medium.bytes");
            if (manifest.BulkLow.Exists)
                expected.Add(
                    $"{entry.Folder}/{group.AssetStem}.low.bytes");
            if (expected.Count != entry.Paths.Count)
                throw new InvalidOperationException(
                    $"ACL group '{group.AssetStem}' file inventory is incomplete.");
            for (int i = 0; i < entry.Paths.Count; i++)
            {
                if (!expected.Contains(entry.Paths[i]))
                    throw new InvalidOperationException(
                        $"ACL group '{group.AssetStem}' file inventory contains an undeclared file.");
            }
        }

        static void RequirePayloadFile(
            string assetPath,
            byte[] expected,
            string assetStem)
        {
            string fullPath =
                CharacterAclAnimationArtifactFileStore.ToProjectFilePath(
                    assetPath);
            bool exists = File.Exists(fullPath);
            if ((expected == null || expected.Length == 0) != !exists)
                throw new InvalidOperationException(
                    $"ACL group '{assetStem}' payload file presence is invalid: {assetPath}.");
            if (!exists)
                return;
            RequireEqualPayload(
                CharacterAclAnimationArtifactFileStore.ReadAllBytes(assetPath),
                expected,
                assetStem);
        }

        static void RequireEqualPayload(
            byte[] actual,
            byte[] expected,
            string assetStem)
        {
            if (actual == null || expected == null || actual.Length != expected.Length)
                throw new InvalidOperationException(
                    $"ACL group '{assetStem}' payload length does not match its artifact.");
            for (int i = 0; i < actual.Length; i++)
            {
                if (actual[i] != expected[i])
                    throw new InvalidOperationException(
                        $"ACL group '{assetStem}' payload bytes do not match its artifact.");
            }
        }

    }
}
