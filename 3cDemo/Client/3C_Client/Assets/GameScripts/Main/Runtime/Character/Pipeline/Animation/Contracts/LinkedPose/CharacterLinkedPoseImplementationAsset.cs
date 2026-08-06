using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [CreateAssetMenu(fileName = "CharacterLinkedPoseImplementation", menuName = "3C/Character/Linked Pose/Implementation")]
    public sealed class CharacterLinkedPoseImplementationAsset : ScriptableObject
    {
        [SerializeField] string m_OwnerIdentity = string.Empty;
        [SerializeField] string m_ImplementationId = string.Empty;
        [SerializeField] ulong m_Revision;
        [SerializeField] CharacterLinkedPoseInterfaceAsset m_Interface;
        [SerializeField] string m_CapturedInterfaceSignature = string.Empty;
        [SerializeField] CharacterLinkedPoseImplementationEntryBinding[] m_Entries = Array.Empty<CharacterLinkedPoseImplementationEntryBinding>();

        public string OwnerIdentity => m_OwnerIdentity ?? string.Empty;
        public LinkedPoseImplementationId ImplementationId => string.IsNullOrWhiteSpace(m_ImplementationId) ? default : new LinkedPoseImplementationId(m_ImplementationId);
        public LinkedPoseRevision Revision => m_Revision == 0 ? default : new LinkedPoseRevision(m_Revision);
        public CharacterLinkedPoseInterfaceAsset Interface => m_Interface;
        public string CapturedInterfaceSignature => m_CapturedInterfaceSignature ?? string.Empty;
        public IReadOnlyList<CharacterLinkedPoseImplementationEntryBinding> Entries => m_Entries ?? Array.Empty<CharacterLinkedPoseImplementationEntryBinding>();
        public bool IsStale => !Interface || !string.Equals(CapturedInterfaceSignature, Interface.SignatureHash.ToString(), StringComparison.Ordinal);
        public StableHash ContentHash => ComputeContentHash();

        public void Configure(
            string ownerIdentity,
            LinkedPoseImplementationId implementationId,
            LinkedPoseRevision revision,
            CharacterLinkedPoseInterfaceAsset linkedInterface,
            CharacterLinkedPoseImplementationEntryBinding[] entries)
        {
            m_OwnerIdentity = PoseIdentity.Require(ownerIdentity, nameof(ownerIdentity));
            m_ImplementationId = implementationId.IsValid
                ? implementationId.Value
                : throw new ArgumentException("Linked Pose Implementation identity is invalid.", nameof(implementationId));
            m_Revision = revision.IsValid ? revision.Value : throw new ArgumentException("Linked Pose Implementation revision is invalid.", nameof(revision));
            m_Interface = linkedInterface ? linkedInterface : throw new ArgumentNullException(nameof(linkedInterface));
            m_CapturedInterfaceSignature = linkedInterface.SignatureHash.ToString();
            m_Entries = entries ?? Array.Empty<CharacterLinkedPoseImplementationEntryBinding>();
            RequireValid();
        }

        public CharacterLinkedPoseImplementationEntryBinding RequireEntry(LinkedPoseEntryId entryId)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                CharacterLinkedPoseImplementationEntryBinding entry = Entries[i];
                if (entry != null && entry.EntryId == entryId)
                    return entry;
            }
            throw new InvalidOperationException($"Linked Pose Implementation '{ImplementationId}' has no Entry '{entryId}'.");
        }

        public void RequireValid()
        {
            Interface?.RequireValid();
            if (string.IsNullOrWhiteSpace(OwnerIdentity) || !ImplementationId.IsValid || !Revision.IsValid || !Interface || IsStale)
                throw new InvalidOperationException($"Linked Pose Implementation '{name}' is incomplete or stale.");
            var entryIds = new HashSet<LinkedPoseEntryId>();
            for (int i = 0; i < Entries.Count; i++)
            {
                CharacterLinkedPoseImplementationEntryBinding binding = Entries[i];
                binding?.RequireValid();
                if (binding == null || !entryIds.Add(binding.EntryId))
                    throw new InvalidOperationException($"Linked Pose Implementation '{ImplementationId}' Entry #{i} is missing or duplicated.");
                Interface.RequireEntry(binding.EntryId);
            }
            if (entryIds.Count != Interface.Entries.Count)
                throw new InvalidOperationException($"Linked Pose Implementation '{ImplementationId}' does not cover every required Interface Entry.");
        }

        StableHash ComputeContentHash()
        {
            var values = new List<string>
            {
                "character-linked-pose-implementation-content/v1",
                ImplementationId.Value ?? string.Empty,
                Revision.IsValid ? Revision.Value.ToString(CultureInfo.InvariantCulture) : "0",
                CapturedInterfaceSignature,
                Entries.Count.ToString(CultureInfo.InvariantCulture)
            };
            CharacterLinkedPoseImplementationEntryBinding[] entries = Entries
                .Where(value => value != null)
                .OrderBy(value => value.EntryId)
                .ToArray();
            for (int i = 0; i < entries.Length; i++)
            {
                CharacterLinkedPoseImplementationEntryBinding binding = entries[i];
                CharacterTypedPoseGraph graph = binding.RequireValid();
                values.Add(binding.EntryId.Value);
                values.Add(binding.GraphOwnerIdentity);
                values.Add(binding.GraphId.Value);
                values.Add(graph.ContentRevision);
            }
            return StableHash.Compute(values.ToArray());
        }
    }
}
