using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterAnimationCompiledResourceDescriptor
    {
        public const string CurrentSchemaVersion = "character-animation-compiled-resource/v1";

        [SerializeField] string m_SchemaVersion = CurrentSchemaVersion;
        [SerializeField] int m_ResourceIndex = -1;
        [SerializeField] int m_GroupIndex = -1;
        [SerializeField] string m_ResourceAddress = string.Empty;
        [SerializeField] string m_GroupContentHash = string.Empty;
        [SerializeField] CharacterAclAnimationResourceManifest[] m_ClipManifests =
            Array.Empty<CharacterAclAnimationResourceManifest>();

        internal CharacterAnimationCompiledResourceDescriptor(
            int resourceIndex,
            int groupIndex,
            string resourceAddress,
            string groupContentHash,
            CharacterAclAnimationResourceManifest[] clipManifests)
        {
            m_ResourceIndex = resourceIndex;
            m_GroupIndex = groupIndex;
            m_ResourceAddress = resourceAddress ?? string.Empty;
            m_GroupContentHash = groupContentHash ?? string.Empty;
            m_ClipManifests = clipManifests ??
                throw new ArgumentNullException(nameof(clipManifests));
            RequireValid();
        }

        public string SchemaVersion => m_SchemaVersion ?? string.Empty;
        public int ResourceIndex => m_ResourceIndex;
        public int GroupIndex => m_GroupIndex;
        public string ResourceAddress => m_ResourceAddress ?? string.Empty;
        public string GroupContentHash => m_GroupContentHash ?? string.Empty;
        public string ResourceIdentity =>
            ClipManifests.Count == 0 ? string.Empty : ClipManifests[0]?.ResourceIdentity ?? string.Empty;
        public IReadOnlyList<CharacterAclAnimationResourceManifest> ClipManifests =>
            m_ClipManifests ?? Array.Empty<CharacterAclAnimationResourceManifest>();

        public CharacterAclAnimationResourceManifest RequireManifest(int groupClipIndex)
        {
            if ((uint)groupClipIndex >= (uint)ClipManifests.Count)
                throw new ArgumentOutOfRangeException(nameof(groupClipIndex));
            return ClipManifests[groupClipIndex] ??
                throw new InvalidOperationException("Compiled ACL resource Clip manifest is missing.");
        }

        public void RequireValid()
        {
            if (!string.Equals(SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal) ||
                ResourceIndex < 0 || GroupIndex < 0 ||
                string.IsNullOrWhiteSpace(ResourceAddress) ||
                !CharacterAclHash.IsSha256(GroupContentHash) ||
                ClipManifests.Count == 0)
            {
                throw new InvalidOperationException("Compiled animation resource descriptor is invalid.");
            }
            for (int i = 0; i < ClipManifests.Count; i++)
            {
                CharacterAclAnimationResourceManifest manifest = RequireManifest(i);
                manifest.RequireValid();
                if (manifest.GroupClipIndex != i ||
                    !string.Equals(manifest.ResourceAddress, ResourceAddress, StringComparison.Ordinal) ||
                    !string.Equals(manifest.GroupContentHash, GroupContentHash, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Compiled animation resource descriptor group mapping is invalid.");
                }
            }
        }
    }
}
