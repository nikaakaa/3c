using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [CreateAssetMenu(fileName = "CharacterLinkedPoseInterface", menuName = "3C/Character/Linked Pose/Interface")]
    public sealed class CharacterLinkedPoseInterfaceAsset : ScriptableObject
    {
        [SerializeField] string m_OwnerIdentity = string.Empty;
        [SerializeField] string m_InterfaceId = string.Empty;
        [SerializeField] ulong m_Revision;
        [SerializeField] string m_FactContractIdentity = string.Empty;
        [SerializeField] string m_ExecutionContract = CharacterLinkedPoseExecutionContract.Current;
        [SerializeField] CharacterLinkedPoseInterfaceEntryDescriptor[] m_Entries = Array.Empty<CharacterLinkedPoseInterfaceEntryDescriptor>();

        public string OwnerIdentity => m_OwnerIdentity ?? string.Empty;
        public LinkedPoseInterfaceId InterfaceId => string.IsNullOrWhiteSpace(m_InterfaceId) ? default : new LinkedPoseInterfaceId(m_InterfaceId);
        public LinkedPoseRevision Revision => m_Revision == 0 ? default : new LinkedPoseRevision(m_Revision);
        public CharacterPresentationFactContractIdentity FactContractIdentity => string.IsNullOrEmpty(m_FactContractIdentity)
            ? default
            : new CharacterPresentationFactContractIdentity(new StableHash(m_FactContractIdentity));
        public string ExecutionContract => m_ExecutionContract ?? string.Empty;
        public IReadOnlyList<CharacterLinkedPoseInterfaceEntryDescriptor> Entries => m_Entries ?? Array.Empty<CharacterLinkedPoseInterfaceEntryDescriptor>();
        public bool IsStale => !FactContractIdentity.IsValid || FactContractIdentity != CharacterPresentationFactContract.Current ||
                               !string.Equals(ExecutionContract, CharacterLinkedPoseExecutionContract.Current, StringComparison.Ordinal);
        public StableHash SignatureHash => ComputeSignatureHash();

        public void Configure(
            string ownerIdentity,
            LinkedPoseInterfaceId interfaceId,
            LinkedPoseRevision revision,
            CharacterLinkedPoseInterfaceEntryDescriptor[] entries)
        {
            m_OwnerIdentity = PoseIdentity.Require(ownerIdentity, nameof(ownerIdentity));
            m_InterfaceId = interfaceId.IsValid ? interfaceId.Value : throw new ArgumentException("Linked Pose Interface identity is invalid.", nameof(interfaceId));
            m_Revision = revision.IsValid ? revision.Value : throw new ArgumentException("Linked Pose Interface revision is invalid.", nameof(revision));
            m_FactContractIdentity = CharacterPresentationFactContract.Current.ToString();
            m_ExecutionContract = CharacterLinkedPoseExecutionContract.Current;
            m_Entries = entries ?? Array.Empty<CharacterLinkedPoseInterfaceEntryDescriptor>();
            RequireValid();
        }

        public CharacterLinkedPoseInterfaceEntryDescriptor RequireEntry(LinkedPoseEntryId entryId)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                CharacterLinkedPoseInterfaceEntryDescriptor entry = Entries[i];
                if (entry != null && entry.EntryId == entryId)
                    return entry;
            }
            throw new InvalidOperationException($"Linked Pose Interface '{InterfaceId}' has no Entry '{entryId}'.");
        }

        public void RequireValid()
        {
            if (string.IsNullOrWhiteSpace(OwnerIdentity) || !InterfaceId.IsValid || !Revision.IsValid || IsStale || Entries.Count == 0)
                throw new InvalidOperationException($"Linked Pose Interface '{name}' is incomplete or stale.");
            var entryIds = new HashSet<LinkedPoseEntryId>();
            for (int i = 0; i < Entries.Count; i++)
            {
                CharacterLinkedPoseInterfaceEntryDescriptor entry = Entries[i];
                entry?.RequireValid();
                if (entry == null || !entryIds.Add(entry.EntryId))
                    throw new InvalidOperationException($"Linked Pose Interface '{InterfaceId}' Entry #{i} is missing or duplicated.");
            }
        }

        StableHash ComputeSignatureHash()
        {
            var values = new List<string>
            {
                "character-linked-pose-interface-signature/v1",
                InterfaceId.Value ?? string.Empty,
                Revision.IsValid ? Revision.Value.ToString(CultureInfo.InvariantCulture) : "0",
                FactContractIdentity.ToString(),
                ExecutionContract,
                Entries.Count.ToString(CultureInfo.InvariantCulture)
            };
            CharacterLinkedPoseInterfaceEntryDescriptor[] entries = Entries
                .Where(value => value != null)
                .OrderBy(value => value.EntryId)
                .ToArray();
            for (int i = 0; i < entries.Length; i++)
                entries[i].AddSignatureParts(values);
            return StableHash.Compute(values.ToArray());
        }
    }
}
