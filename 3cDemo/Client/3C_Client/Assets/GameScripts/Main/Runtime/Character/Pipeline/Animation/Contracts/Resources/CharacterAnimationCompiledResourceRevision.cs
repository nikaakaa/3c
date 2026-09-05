using System;
using System.Collections.Generic;
using System.Globalization;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public static class CharacterAnimationCompiledResourceRevision
    {
        public static string Compute(
            IReadOnlyList<CharacterAnimationCompiledResourceDescriptor> resources)
        {
            if (resources == null)
                throw new ArgumentNullException(nameof(resources));
            var values = new List<string>(resources.Count * 16 + 3)
            {
                "character-animation-compiled-resource-revision/v1",
                resources.Count.ToString(CultureInfo.InvariantCulture)
            };
            if (resources.Count == 0)
            {
                values.Add("none");
                return CharacterAclHash.ComputeStrings(values);
            }
            for (int resourceIndex = 0; resourceIndex < resources.Count; resourceIndex++)
            {
                CharacterAnimationCompiledResourceDescriptor resource =
                    resources[resourceIndex] ??
                    throw new InvalidOperationException(
                        $"Compiled animation resource #{resourceIndex} is missing.");
                resource.RequireValid();
                if (resource.ResourceIndex != resourceIndex)
                    throw new InvalidOperationException(
                        "Compiled animation resource revision requires dense ResourceIndex values.");
                values.Add(resource.ResourceIdentity);
                values.Add(resource.ResourceAddress);
                values.Add(resource.GroupContentHash);
                values.Add(resource.ClipManifests.Count.ToString(
                    CultureInfo.InvariantCulture));
                for (int clipIndex = 0; clipIndex < resource.ClipManifests.Count; clipIndex++)
                {
                    CharacterAclAnimationResourceManifest manifest =
                        resource.RequireManifest(clipIndex);
                    values.Add(manifest.SchemaVersion);
                    values.Add(manifest.FormalClipIdentity);
                    values.Add(manifest.SourceDependencyHash);
                    values.Add(manifest.RigId);
                    values.Add(manifest.RigRevision);
                    values.Add(manifest.ReferencePoseIdentity);
                    values.Add(manifest.BindingHash);
                    values.Add(manifest.ContentHash);
                    values.Add(manifest.BuildRevision);
                    values.Add(manifest.GroupContentHash);
                    values.Add(manifest.NativeArtifactIdentity);
                    values.Add(manifest.NativeBinarySha256);
                    values.Add(manifest.NativePlatform);
                    values.Add(manifest.NativeAbiVersion.ToString(
                        CultureInfo.InvariantCulture));
                    values.Add(manifest.NativePayloadFormatVersion.ToString(
                        CultureInfo.InvariantCulture));
                    values.Add(manifest.GroupClipIndex.ToString(
                        CultureInfo.InvariantCulture));
                    AddBlock(values, manifest.Transform);
                    AddBlock(values, manifest.Scalar);
                    AddBlock(values, manifest.DatabaseHeader);
                    AddBlock(values, manifest.BulkMedium);
                    AddBlock(values, manifest.BulkLow);
                }
            }
            return CharacterAclHash.ComputeStrings(values);
        }

        static void AddBlock(
            List<string> values,
            CharacterAclDataBlockDescriptor block)
        {
            values.Add(block.Exists ? "exists" : "absent");
            values.Add(block.Length.ToString(CultureInfo.InvariantCulture));
            values.Add(block.FormatVersion.ToString(CultureInfo.InvariantCulture));
            values.Add(block.ContentHash);
        }
    }
}
