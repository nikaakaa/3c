using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterLinkedPoseCallProjectionDescriptor
    {
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] string m_GroupId = string.Empty;
        [SerializeField] string m_InterfaceId = string.Empty;
        [SerializeField] string m_InterfaceSignature = string.Empty;
        [SerializeField] string m_EntryId = string.Empty;

        public PoseNodeId NodeId => string.IsNullOrWhiteSpace(m_NodeId) ? default : new PoseNodeId(m_NodeId);
        public LinkedPoseGroupId GroupId => string.IsNullOrWhiteSpace(m_GroupId) ? default : new LinkedPoseGroupId(m_GroupId);
        public LinkedPoseInterfaceId InterfaceId => string.IsNullOrWhiteSpace(m_InterfaceId) ? default : new LinkedPoseInterfaceId(m_InterfaceId);
        public StableHash InterfaceSignature => string.IsNullOrWhiteSpace(m_InterfaceSignature) ? default : new StableHash(m_InterfaceSignature);
        public LinkedPoseEntryId EntryId => string.IsNullOrWhiteSpace(m_EntryId) ? default : new LinkedPoseEntryId(m_EntryId);

        public CharacterLinkedPoseCallProjectionDescriptor() { }

        public CharacterLinkedPoseCallProjectionDescriptor(
            PoseNodeId nodeId,
            CharacterLinkedPoseGroupBinding group,
            LinkedPoseEntryId entryId)
        {
            group?.RequireValid();
            if (!nodeId.IsValid || group == null || !entryId.IsValid)
                throw new ArgumentException("Linked Pose Call Projection descriptor input is incomplete.");
            group.Interface.RequireEntry(entryId);
            m_NodeId = nodeId.Value;
            m_GroupId = group.GroupId.Value;
            m_InterfaceId = group.Interface.InterfaceId.Value;
            m_InterfaceSignature = group.Interface.SignatureHash.ToString();
            m_EntryId = entryId.Value;
            RequireValid();
        }

        public void RequireValid()
        {
            if (!NodeId.IsValid || !GroupId.IsValid || !InterfaceId.IsValid || !InterfaceSignature.IsValid || !EntryId.IsValid)
                throw new InvalidOperationException("Linked Pose Call Projection descriptor is incomplete.");
        }
    }

    [Serializable]
    public sealed class CharacterLinkedPoseInterfaceProjectionDescriptor
    {
        [SerializeField] string m_InterfaceId = string.Empty;
        [SerializeField] ulong m_Revision;
        [SerializeField] string m_FactContractIdentity = string.Empty;
        [SerializeField] string m_ExecutionContract = string.Empty;
        [SerializeField] string m_SignatureHash = string.Empty;
        [SerializeField] CharacterLinkedPoseInterfaceEntryDescriptor[] m_Entries = Array.Empty<CharacterLinkedPoseInterfaceEntryDescriptor>();

        public LinkedPoseInterfaceId InterfaceId => string.IsNullOrWhiteSpace(m_InterfaceId) ? default : new LinkedPoseInterfaceId(m_InterfaceId);
        public LinkedPoseRevision Revision => m_Revision == 0 ? default : new LinkedPoseRevision(m_Revision);
        public CharacterPresentationFactContractIdentity FactContractIdentity => string.IsNullOrEmpty(m_FactContractIdentity)
            ? default
            : new CharacterPresentationFactContractIdentity(new StableHash(m_FactContractIdentity));
        public string ExecutionContract => m_ExecutionContract ?? string.Empty;
        public StableHash SignatureHash => string.IsNullOrEmpty(m_SignatureHash) ? default : new StableHash(m_SignatureHash);
        public IReadOnlyList<CharacterLinkedPoseInterfaceEntryDescriptor> Entries => m_Entries ?? Array.Empty<CharacterLinkedPoseInterfaceEntryDescriptor>();

        public CharacterLinkedPoseInterfaceProjectionDescriptor() { }

        public CharacterLinkedPoseInterfaceProjectionDescriptor(CharacterLinkedPoseInterfaceAsset source)
        {
            source?.RequireValid();
            if (!source)
                throw new ArgumentNullException(nameof(source));
            m_InterfaceId = source.InterfaceId.Value;
            m_Revision = source.Revision.Value;
            m_FactContractIdentity = source.FactContractIdentity.ToString();
            m_ExecutionContract = source.ExecutionContract;
            m_SignatureHash = source.SignatureHash.ToString();
            m_Entries = new CharacterLinkedPoseInterfaceEntryDescriptor[source.Entries.Count];
            for (int i = 0; i < m_Entries.Length; i++)
                m_Entries[i] = source.Entries[i];
            RequireValid();
        }

        public void RequireValid()
        {
            if (!InterfaceId.IsValid || !Revision.IsValid || FactContractIdentity != CharacterPresentationFactContract.Current ||
                !string.Equals(ExecutionContract, CharacterLinkedPoseExecutionContract.Current, StringComparison.Ordinal) ||
                !SignatureHash.IsValid || Entries.Count == 0)
            {
                throw new InvalidOperationException("Linked Pose Interface Projection descriptor is incomplete or stale.");
            }
            var entries = new HashSet<LinkedPoseEntryId>();
            for (int i = 0; i < Entries.Count; i++)
            {
                CharacterLinkedPoseInterfaceEntryDescriptor entry = Entries[i];
                entry?.RequireValid();
                if (entry == null || !entries.Add(entry.EntryId))
                    throw new InvalidOperationException($"Linked Pose Interface Projection '{InterfaceId}' Entry #{i} is invalid.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterLinkedPoseEntryFragmentDescriptor
    {
        [SerializeField] string m_EntryId = string.Empty;
        [SerializeField] string m_GraphOwnerIdentity = string.Empty;
        [SerializeField] string m_GraphId = string.Empty;
        [SerializeField] string m_GraphContentRevision = string.Empty;

        public LinkedPoseEntryId EntryId => string.IsNullOrWhiteSpace(m_EntryId) ? default : new LinkedPoseEntryId(m_EntryId);
        public string GraphOwnerIdentity => m_GraphOwnerIdentity ?? string.Empty;
        public PoseGraphId GraphId => string.IsNullOrWhiteSpace(m_GraphId) ? default : new PoseGraphId(m_GraphId);
        public string GraphContentRevision => m_GraphContentRevision ?? string.Empty;

        public CharacterLinkedPoseEntryFragmentDescriptor() { }

        public CharacterLinkedPoseEntryFragmentDescriptor(CharacterLinkedPoseImplementationEntryBinding source)
        {
            CharacterTypedPoseGraph graph = source?.RequireValid();
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            m_EntryId = source.EntryId.Value;
            m_GraphOwnerIdentity = source.GraphOwnerIdentity;
            m_GraphId = source.GraphId.Value;
            m_GraphContentRevision = graph.ContentRevision;
            RequireValid();
        }

        public void RequireValid()
        {
            if (!EntryId.IsValid || string.IsNullOrWhiteSpace(GraphOwnerIdentity) || !GraphId.IsValid || string.IsNullOrWhiteSpace(GraphContentRevision))
                throw new InvalidOperationException("Linked Pose Entry fragment descriptor is incomplete.");
        }
    }

    [Serializable]
    public sealed class CharacterLinkedPoseImplementationProjectionDescriptor
    {
        [SerializeField] string m_ImplementationId = string.Empty;
        [SerializeField] ulong m_Revision;
        [SerializeField] string m_InterfaceId = string.Empty;
        [SerializeField] string m_InterfaceSignature = string.Empty;
        [SerializeField] string m_ContentHash = string.Empty;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] CharacterLinkedPoseEntryFragmentDescriptor[] m_Entries = Array.Empty<CharacterLinkedPoseEntryFragmentDescriptor>();

        public LinkedPoseImplementationId ImplementationId => string.IsNullOrWhiteSpace(m_ImplementationId) ? default : new LinkedPoseImplementationId(m_ImplementationId);
        public LinkedPoseRevision Revision => m_Revision == 0 ? default : new LinkedPoseRevision(m_Revision);
        public LinkedPoseInterfaceId InterfaceId => string.IsNullOrWhiteSpace(m_InterfaceId) ? default : new LinkedPoseInterfaceId(m_InterfaceId);
        public StableHash InterfaceSignature => string.IsNullOrEmpty(m_InterfaceSignature) ? default : new StableHash(m_InterfaceSignature);
        public StableHash ContentHash => string.IsNullOrEmpty(m_ContentHash) ? default : new StableHash(m_ContentHash);
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public IReadOnlyList<CharacterLinkedPoseEntryFragmentDescriptor> Entries => m_Entries ?? Array.Empty<CharacterLinkedPoseEntryFragmentDescriptor>();

        public CharacterLinkedPoseImplementationProjectionDescriptor() { }

        public CharacterLinkedPoseImplementationProjectionDescriptor(
            CharacterLinkedPoseImplementationAsset source,
            CharacterAnimationRigDefinition rig)
        {
            source?.RequireValid();
            if (!source || !rig)
                throw new ArgumentNullException(nameof(source));
            m_ImplementationId = source.ImplementationId.Value;
            m_Revision = source.Revision.Value;
            m_InterfaceId = source.Interface.InterfaceId.Value;
            m_InterfaceSignature = source.Interface.SignatureHash.ToString();
            m_ContentHash = source.ContentHash.ToString();
            m_RigId = rig.RigId;
            m_RigRevision = rig.Revision;
            m_Entries = new CharacterLinkedPoseEntryFragmentDescriptor[source.Entries.Count];
            for (int i = 0; i < m_Entries.Length; i++)
                m_Entries[i] = new CharacterLinkedPoseEntryFragmentDescriptor(source.Entries[i]);
            RequireValid();
        }

        public void RequireValid()
        {
            if (!ImplementationId.IsValid || !Revision.IsValid || !InterfaceId.IsValid || !InterfaceSignature.IsValid || !ContentHash.IsValid ||
                string.IsNullOrWhiteSpace(RigId) || string.IsNullOrWhiteSpace(RigRevision) || Entries.Count == 0)
                throw new InvalidOperationException("Linked Pose Implementation Projection descriptor is incomplete.");
            var entries = new HashSet<LinkedPoseEntryId>();
            for (int i = 0; i < Entries.Count; i++)
            {
                CharacterLinkedPoseEntryFragmentDescriptor entry = Entries[i];
                entry?.RequireValid();
                if (entry == null || !entries.Add(entry.EntryId))
                    throw new InvalidOperationException($"Linked Pose Implementation Projection '{ImplementationId}' Entry #{i} is invalid.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterLinkedPoseGroupProjectionDescriptor
    {
        [SerializeField] string m_GroupId = string.Empty;
        [SerializeField] string m_InterfaceId = string.Empty;
        [SerializeField] string m_InterfaceSignature = string.Empty;
        [SerializeField] string m_SelectorId = string.Empty;

        public LinkedPoseGroupId GroupId => string.IsNullOrWhiteSpace(m_GroupId) ? default : new LinkedPoseGroupId(m_GroupId);
        public LinkedPoseInterfaceId InterfaceId => string.IsNullOrWhiteSpace(m_InterfaceId) ? default : new LinkedPoseInterfaceId(m_InterfaceId);
        public StableHash InterfaceSignature => string.IsNullOrEmpty(m_InterfaceSignature) ? default : new StableHash(m_InterfaceSignature);
        public LinkedPoseSelectorId SelectorId => string.IsNullOrWhiteSpace(m_SelectorId) ? default : new LinkedPoseSelectorId(m_SelectorId);

        public CharacterLinkedPoseGroupProjectionDescriptor() { }

        public CharacterLinkedPoseGroupProjectionDescriptor(
            CharacterLinkedPoseGroupBinding group,
            CharacterLinkedPoseCompiledSelectorDescriptor selector)
        {
            group?.RequireValid();
            selector?.RequireValid();
            if (group == null || selector == null || selector.GroupId != group.GroupId || selector.InterfaceId != group.Interface.InterfaceId)
                throw new InvalidOperationException("Linked Pose Group Projection inputs are inconsistent.");
            m_GroupId = group.GroupId.Value;
            m_InterfaceId = group.Interface.InterfaceId.Value;
            m_InterfaceSignature = group.Interface.SignatureHash.ToString();
            m_SelectorId = selector.SelectorId.Value;
            RequireValid();
        }

        public void RequireValid()
        {
            if (!GroupId.IsValid || !InterfaceId.IsValid || !InterfaceSignature.IsValid || !SelectorId.IsValid)
                throw new InvalidOperationException("Linked Pose Group Projection descriptor is incomplete.");
        }
    }

    [Serializable]
    public sealed class CharacterLinkedPoseProjectionPayload
    {
        [SerializeField] CharacterLinkedPoseInterfaceProjectionDescriptor[] m_Interfaces = Array.Empty<CharacterLinkedPoseInterfaceProjectionDescriptor>();
        [SerializeField] CharacterLinkedPoseGroupProjectionDescriptor[] m_Groups = Array.Empty<CharacterLinkedPoseGroupProjectionDescriptor>();
        [SerializeField] CharacterLinkedPoseCompiledSelectorDescriptor[] m_Selectors = Array.Empty<CharacterLinkedPoseCompiledSelectorDescriptor>();
        [SerializeField] CharacterEquipmentLinkedPoseSelectorDescriptor[] m_EquipmentSelectors = Array.Empty<CharacterEquipmentLinkedPoseSelectorDescriptor>();
        [SerializeField] CharacterLinkedPoseImplementationProjectionDescriptor[] m_Implementations = Array.Empty<CharacterLinkedPoseImplementationProjectionDescriptor>();
        [SerializeField] CharacterLinkedPoseCallProjectionDescriptor[] m_Calls = Array.Empty<CharacterLinkedPoseCallProjectionDescriptor>();
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] string m_FactContractIdentity = string.Empty;
        [SerializeField] string m_ExecutionContract = string.Empty;

        public IReadOnlyList<CharacterLinkedPoseInterfaceProjectionDescriptor> Interfaces => m_Interfaces ?? Array.Empty<CharacterLinkedPoseInterfaceProjectionDescriptor>();
        public IReadOnlyList<CharacterLinkedPoseGroupProjectionDescriptor> Groups => m_Groups ?? Array.Empty<CharacterLinkedPoseGroupProjectionDescriptor>();
        public IReadOnlyList<CharacterLinkedPoseCompiledSelectorDescriptor> Selectors => m_Selectors ?? Array.Empty<CharacterLinkedPoseCompiledSelectorDescriptor>();
        public IReadOnlyList<CharacterEquipmentLinkedPoseSelectorDescriptor> EquipmentSelectors => m_EquipmentSelectors ?? Array.Empty<CharacterEquipmentLinkedPoseSelectorDescriptor>();
        public IReadOnlyList<CharacterLinkedPoseImplementationProjectionDescriptor> Implementations => m_Implementations ?? Array.Empty<CharacterLinkedPoseImplementationProjectionDescriptor>();
        public IReadOnlyList<CharacterLinkedPoseCallProjectionDescriptor> Calls => m_Calls ?? Array.Empty<CharacterLinkedPoseCallProjectionDescriptor>();
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public CharacterPresentationFactContractIdentity FactContractIdentity => string.IsNullOrEmpty(m_FactContractIdentity)
            ? default
            : new CharacterPresentationFactContractIdentity(new StableHash(m_FactContractIdentity));
        public string ExecutionContract => m_ExecutionContract ?? string.Empty;
        public bool IsValid
        {
            get
            {
                try
                {
                    RequireValid();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public CharacterLinkedPoseProjectionPayload() { }

        public CharacterLinkedPoseProjectionPayload(
            string rigId,
            string rigRevision,
            CharacterLinkedPoseInterfaceProjectionDescriptor[] interfaces,
            CharacterLinkedPoseGroupProjectionDescriptor[] groups,
            CharacterLinkedPoseCompiledSelectorDescriptor[] selectors,
            CharacterEquipmentLinkedPoseSelectorDescriptor[] equipmentSelectors,
            CharacterLinkedPoseImplementationProjectionDescriptor[] implementations,
            CharacterLinkedPoseCallProjectionDescriptor[] calls)
        {
            m_RigId = PoseIdentity.Require(rigId, nameof(rigId));
            m_RigRevision = PoseIdentity.Require(rigRevision, nameof(rigRevision));
            m_FactContractIdentity = CharacterPresentationFactContract.Current.ToString();
            m_ExecutionContract = CharacterLinkedPoseExecutionContract.Current;
            m_Interfaces = interfaces ?? Array.Empty<CharacterLinkedPoseInterfaceProjectionDescriptor>();
            m_Groups = groups ?? Array.Empty<CharacterLinkedPoseGroupProjectionDescriptor>();
            m_Selectors = selectors ?? Array.Empty<CharacterLinkedPoseCompiledSelectorDescriptor>();
            m_EquipmentSelectors = equipmentSelectors ?? Array.Empty<CharacterEquipmentLinkedPoseSelectorDescriptor>();
            m_Implementations = implementations ?? Array.Empty<CharacterLinkedPoseImplementationProjectionDescriptor>();
            m_Calls = calls ?? Array.Empty<CharacterLinkedPoseCallProjectionDescriptor>();
            RequireValid();
        }

        public void RequireValid()
        {
            if (string.IsNullOrWhiteSpace(RigId) || string.IsNullOrWhiteSpace(RigRevision))
                throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                    CharacterLinkedPoseDiagnosticCode.RigMismatch,
                    "Projection Rig identity is incomplete."));
            if (FactContractIdentity != CharacterPresentationFactContract.Current)
                throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                    CharacterLinkedPoseDiagnosticCode.FactContractMismatch,
                    $"Projection Fact contract '{FactContractIdentity}' does not match '{CharacterPresentationFactContract.Current}'."));
            if (!string.Equals(ExecutionContract, CharacterLinkedPoseExecutionContract.Current, StringComparison.Ordinal))
                throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                    CharacterLinkedPoseDiagnosticCode.RuntimeAbiMismatch,
                    $"Projection execution contract '{ExecutionContract}' does not match '{CharacterLinkedPoseExecutionContract.Current}'."));
            var interfaces = new Dictionary<LinkedPoseInterfaceId, CharacterLinkedPoseInterfaceProjectionDescriptor>();
            for (int i = 0; i < Interfaces.Count; i++)
            {
                CharacterLinkedPoseInterfaceProjectionDescriptor value = Interfaces[i];
                value?.RequireValid();
                if (value == null || !interfaces.TryAdd(value.InterfaceId, value))
                    throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                        CharacterLinkedPoseDiagnosticCode.SignatureMismatch,
                        $"Projection Interface #{i} is missing or duplicated."));
            }
            var implementations = new Dictionary<LinkedPoseImplementationId, CharacterLinkedPoseImplementationProjectionDescriptor>();
            for (int i = 0; i < Implementations.Count; i++)
            {
                CharacterLinkedPoseImplementationProjectionDescriptor value = Implementations[i];
                value?.RequireValid();
                if (value == null || !implementations.TryAdd(value.ImplementationId, value) ||
                    !interfaces.TryGetValue(value.InterfaceId, out CharacterLinkedPoseInterfaceProjectionDescriptor linkedInterface))
                    throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                        CharacterLinkedPoseDiagnosticCode.MissingEntry,
                        $"Projection Implementation #{i} is missing, duplicated or references an absent Interface."));
                if (value.InterfaceSignature != linkedInterface.SignatureHash)
                    throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                        CharacterLinkedPoseDiagnosticCode.SignatureMismatch,
                        $"Implementation '{value.ImplementationId}' signature '{value.InterfaceSignature}' does not match Interface '{value.InterfaceId}' signature '{linkedInterface.SignatureHash}'."));
                if (value.Entries.Count != linkedInterface.Entries.Count)
                    throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                        CharacterLinkedPoseDiagnosticCode.MissingEntry,
                        $"Implementation '{value.ImplementationId}' has {value.Entries.Count} Entries but Interface '{value.InterfaceId}' requires {linkedInterface.Entries.Count}."));
                if (!string.Equals(value.RigId, RigId, StringComparison.Ordinal) ||
                    !string.Equals(value.RigRevision, RigRevision, StringComparison.Ordinal))
                    throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                        CharacterLinkedPoseDiagnosticCode.RigMismatch,
                        $"Implementation '{value.ImplementationId}' Rig '{value.RigId}@{value.RigRevision}' does not match Projection Rig '{RigId}@{RigRevision}'."));
            }
            var selectors = new Dictionary<LinkedPoseGroupId, CharacterLinkedPoseCompiledSelectorDescriptor>();
            for (int i = 0; i < Selectors.Count; i++)
            {
                CharacterLinkedPoseCompiledSelectorDescriptor value = Selectors[i];
                value?.RequireValid();
                if (value == null)
                    throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                        CharacterLinkedPoseDiagnosticCode.MissingSelector,
                        $"Projection selector #{i} is missing."));
                if (!selectors.TryAdd(value.GroupId, value))
                    throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                        CharacterLinkedPoseDiagnosticCode.DuplicateSelector,
                        $"Projection Group '{value.GroupId}' has duplicate selectors."));
                for (int candidateIndex = 0; candidateIndex < value.CandidateImplementationIds.Count; candidateIndex++)
                {
                    var candidateId = new LinkedPoseImplementationId(value.CandidateImplementationIds[candidateIndex]);
                    if (!implementations.TryGetValue(candidateId, out CharacterLinkedPoseImplementationProjectionDescriptor candidate) || candidate.InterfaceId != value.InterfaceId)
                        throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                            CharacterLinkedPoseDiagnosticCode.SourceClosureMissing,
                            $"Selector '{value.SelectorId}' candidate '{candidateId}' is absent or implements another Interface."));
                }
            }
            var groups = new HashSet<LinkedPoseGroupId>();
            for (int i = 0; i < Groups.Count; i++)
            {
                CharacterLinkedPoseGroupProjectionDescriptor value = Groups[i];
                value?.RequireValid();
                if (value == null || !groups.Add(value.GroupId) || !interfaces.TryGetValue(value.InterfaceId, out CharacterLinkedPoseInterfaceProjectionDescriptor linkedInterface) ||
                    linkedInterface.SignatureHash != value.InterfaceSignature || !selectors.TryGetValue(value.GroupId, out CharacterLinkedPoseCompiledSelectorDescriptor selector) ||
                    selector.SelectorId != value.SelectorId || selector.InterfaceId != value.InterfaceId)
                {
                    throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                        CharacterLinkedPoseDiagnosticCode.SignatureMismatch,
                        $"Projection Group #{i} is missing, duplicated or inconsistent with its Interface/selector."));
                }
            }
            if (groups.Count != selectors.Count)
                throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                    CharacterLinkedPoseDiagnosticCode.MissingSelector,
                    "Projection Group and selector closure is incomplete."));
            var callNodes = new HashSet<PoseNodeId>();
            var callEntries = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < Calls.Count; i++)
            {
                CharacterLinkedPoseCallProjectionDescriptor call = Calls[i];
                if (call == null)
                    throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                        CharacterLinkedPoseDiagnosticCode.MissingEntry,
                        $"Projection Call #{i} is missing."));
                call.RequireValid();
                string entryKey = $"{call.GroupId.Value}\0{call.EntryId.Value}";
                if (!callNodes.Add(call.NodeId) || !callEntries.Add(entryKey) ||
                    !groups.Contains(call.GroupId) || !interfaces.TryGetValue(call.InterfaceId, out CharacterLinkedPoseInterfaceProjectionDescriptor linkedInterface) ||
                    linkedInterface.SignatureHash != call.InterfaceSignature || !linkedInterface.Entries.Any(value => value.EntryId == call.EntryId))
                {
                    throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                        CharacterLinkedPoseDiagnosticCode.DuplicateCall,
                        $"Projection Call #{i} is duplicated or incompatible with Group/Interface/Entry identity."));
                }
            }
            foreach (CharacterLinkedPoseGroupProjectionDescriptor group in Groups)
            {
                CharacterLinkedPoseInterfaceProjectionDescriptor linkedInterface = interfaces[group.InterfaceId];
                for (int entryIndex = 0; entryIndex < linkedInterface.Entries.Count; entryIndex++)
                {
                    string entryKey = $"{group.GroupId.Value}\0{linkedInterface.Entries[entryIndex].EntryId.Value}";
                    if (!callEntries.Contains(entryKey))
                        throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                            CharacterLinkedPoseDiagnosticCode.MissingEntry,
                            $"Group '{group.GroupId}' Entry '{linkedInterface.Entries[entryIndex].EntryId}' has no root Call."));
                }
            }
            for (int i = 0; i < EquipmentSelectors.Count; i++)
            {
                CharacterEquipmentLinkedPoseSelectorDescriptor equipment = EquipmentSelectors[i];
                equipment?.RequireValid();
                if (equipment == null || !selectors.TryGetValue(equipment.Core.GroupId, out CharacterLinkedPoseCompiledSelectorDescriptor core) ||
                    core.SelectorId != equipment.Core.SelectorId)
                {
                    throw new InvalidOperationException($"Equipment Linked Pose Projection selector #{i} is inconsistent.");
                }
            }
        }
    }

    public sealed partial class CharacterPresentationProjection
    {
        [SerializeField] CharacterLinkedPoseProjectionPayload m_LinkedPose = new CharacterLinkedPoseProjectionPayload();

        public CharacterLinkedPoseProjectionPayload LinkedPose => m_LinkedPose;

        internal void SetLinkedPoseProjection(CharacterLinkedPoseProjectionPayload value)
        {
            m_LinkedPose = value ?? throw new ArgumentNullException(nameof(value));
            m_LinkedPose.RequireValid();
        }
    }
}
