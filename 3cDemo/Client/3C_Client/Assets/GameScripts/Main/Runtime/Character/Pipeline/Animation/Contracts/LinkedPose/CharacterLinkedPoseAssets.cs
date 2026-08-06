using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterLinkedPoseImplementationEntryBinding
    {
        [SerializeField] string m_EntryId = string.Empty;
        [SerializeField] string m_GraphOwnerIdentity = string.Empty;
        [SerializeField] CharacterPresentationPoseGraphAsset m_GraphOwner;
        [SerializeField] string m_GraphId = string.Empty;

        public LinkedPoseEntryId EntryId => string.IsNullOrWhiteSpace(m_EntryId) ? default : new LinkedPoseEntryId(m_EntryId);
        public string GraphOwnerIdentity => m_GraphOwnerIdentity ?? string.Empty;
        public CharacterPresentationPoseGraphAsset GraphOwner => m_GraphOwner;
        public PoseGraphId GraphId => string.IsNullOrWhiteSpace(m_GraphId) ? default : new PoseGraphId(m_GraphId);

        public CharacterLinkedPoseImplementationEntryBinding() { }

        public CharacterLinkedPoseImplementationEntryBinding(
            LinkedPoseEntryId entryId,
            string graphOwnerIdentity,
            CharacterPresentationPoseGraphAsset graphOwner,
            PoseGraphId graphId)
        {
            m_EntryId = entryId.IsValid ? entryId.Value : throw new ArgumentException("Linked Pose Entry identity is invalid.", nameof(entryId));
            m_GraphOwnerIdentity = PoseIdentity.Require(graphOwnerIdentity, nameof(graphOwnerIdentity));
            m_GraphOwner = graphOwner ? graphOwner : throw new ArgumentNullException(nameof(graphOwner));
            m_GraphId = graphId.IsValid ? graphId.Value : throw new ArgumentException("Pose Graph identity is invalid.", nameof(graphId));
            RequireValid();
        }

        public CharacterTypedPoseGraph RequireValid()
        {
            if (!EntryId.IsValid || string.IsNullOrWhiteSpace(GraphOwnerIdentity) || !GraphOwner || !GraphId.IsValid)
                throw new InvalidOperationException("Linked Pose Implementation Entry binding is incomplete.");
            return GraphOwner.RequireGraph(GraphId);
        }
    }

    [Serializable]
    public sealed class CharacterLinkedPoseGroupBinding
    {
        [SerializeField] string m_GroupId = string.Empty;
        [SerializeField] CharacterLinkedPoseInterfaceAsset m_Interface;

        public LinkedPoseGroupId GroupId => string.IsNullOrWhiteSpace(m_GroupId) ? default : new LinkedPoseGroupId(m_GroupId);
        public CharacterLinkedPoseInterfaceAsset Interface => m_Interface;

        public CharacterLinkedPoseGroupBinding() { }

        public CharacterLinkedPoseGroupBinding(LinkedPoseGroupId groupId, CharacterLinkedPoseInterfaceAsset linkedInterface)
        {
            m_GroupId = groupId.IsValid ? groupId.Value : throw new ArgumentException("Linked Pose Group identity is invalid.", nameof(groupId));
            m_Interface = linkedInterface ? linkedInterface : throw new ArgumentNullException(nameof(linkedInterface));
            RequireValid();
        }

        public void RequireValid()
        {
            Interface?.RequireValid();
            if (!GroupId.IsValid || !Interface)
                throw new InvalidOperationException("Linked Pose Group binding is incomplete.");
        }
    }
}
