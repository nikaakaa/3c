using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct CharacterLinkedPoseGenerationHandle
    {
        public CharacterLinkedPoseGenerationHandle(
            LinkedPoseGroupId groupId,
            LinkedPoseInterfaceId interfaceId,
            LinkedPoseImplementationId implementationId,
            LinkedPoseRevision selectionRevision,
            ulong generation,
            int pageIndex,
            bool poseDiscontinuity)
        {
            if (!groupId.IsValid || !interfaceId.IsValid || !implementationId.IsValid || !selectionRevision.IsValid ||
                generation == 0 || pageIndex < 0 || pageIndex > 1)
            {
                throw new ArgumentException("Linked Pose generation handle is incomplete.");
            }
            GroupId = groupId;
            InterfaceId = interfaceId;
            ImplementationId = implementationId;
            SelectionRevision = selectionRevision;
            Generation = generation;
            PageIndex = pageIndex;
            PoseDiscontinuity = poseDiscontinuity;
        }

        public LinkedPoseGroupId GroupId { get; }
        public LinkedPoseInterfaceId InterfaceId { get; }
        public LinkedPoseImplementationId ImplementationId { get; }
        public LinkedPoseRevision SelectionRevision { get; }
        public ulong Generation { get; }
        public int PageIndex { get; }
        public bool PoseDiscontinuity { get; }
        public bool IsValid => GroupId.IsValid && InterfaceId.IsValid && ImplementationId.IsValid &&
                               SelectionRevision.IsValid && Generation != 0 && PageIndex >= 0 && PageIndex <= 1;
    }

    public readonly struct CharacterLinkedPoseRuntimeGroupSnapshot
    {
        public CharacterLinkedPoseRuntimeGroupSnapshot(
            LinkedPoseGroupId groupId,
            LinkedPoseInterfaceId interfaceId,
            StableHash interfaceSignature,
            LinkedPoseSelectorId selectorId,
            LinkedPoseImplementationId implementationId,
            LinkedPoseRevision implementationRevision,
            StableHash implementationContentHash,
            LinkedPoseRevision selectionRevision,
            ulong generation,
            bool stateReset,
            in CharacterLinkedPoseRuntimeCapacity maximumCapacity,
            in CharacterLinkedPoseRuntimeCapacity activeCapacity)
        {
            if (!groupId.IsValid || !interfaceId.IsValid || !interfaceSignature.IsValid ||
                !selectorId.IsValid || !implementationId.IsValid || !implementationRevision.IsValid ||
                !implementationContentHash.IsValid || !selectionRevision.IsValid || generation == 0)
            {
                throw new ArgumentException("Linked Pose runtime Group snapshot is incomplete.");
            }
            GroupId = groupId;
            InterfaceId = interfaceId;
            InterfaceSignature = interfaceSignature;
            SelectorId = selectorId;
            ImplementationId = implementationId;
            ImplementationRevision = implementationRevision;
            ImplementationContentHash = implementationContentHash;
            SelectionRevision = selectionRevision;
            Generation = generation;
            StateReset = stateReset;
            MaximumCapacity = maximumCapacity;
            ActiveCapacity = activeCapacity;
        }

        public LinkedPoseGroupId GroupId { get; }
        public LinkedPoseInterfaceId InterfaceId { get; }
        public StableHash InterfaceSignature { get; }
        public LinkedPoseSelectorId SelectorId { get; }
        public LinkedPoseImplementationId ImplementationId { get; }
        public LinkedPoseRevision ImplementationRevision { get; }
        public StableHash ImplementationContentHash { get; }
        public LinkedPoseRevision SelectionRevision { get; }
        public ulong Generation { get; }
        public bool StateReset { get; }
        public CharacterLinkedPoseRuntimeCapacity MaximumCapacity { get; }
        public CharacterLinkedPoseRuntimeCapacity ActiveCapacity { get; }
    }

    sealed class CharacterLinkedPoseGroupRuntimeState
    {
        readonly CharacterLinkedPoseGroupProjectionDescriptor m_Group;
        readonly CharacterLinkedPoseCompiledSelectorDescriptor m_Selector;
        readonly CharacterLinkedPoseInterfaceProjectionDescriptor m_Interface;
        readonly IReadOnlyList<CharacterLinkedPoseImplementationProjectionDescriptor> m_Implementations;
        readonly CharacterLinkedPoseGroupRuntimeLayout m_Layout;
        CharacterLinkedPoseGenerationHandle m_Committed;
        CharacterLinkedPoseGenerationHandle m_Incoming;
        CharacterLinkedPoseImplementationRuntimeLayout m_CommittedImplementation;
        CharacterLinkedPoseImplementationRuntimeLayout m_IncomingImplementation;
        bool m_HasCommitted;
        bool m_HasIncoming;

        public CharacterLinkedPoseGroupRuntimeState(
            CharacterLinkedPoseGroupProjectionDescriptor group,
            CharacterLinkedPoseCompiledSelectorDescriptor selector,
            CharacterLinkedPoseInterfaceProjectionDescriptor linkedInterface,
            IReadOnlyList<CharacterLinkedPoseImplementationProjectionDescriptor> implementations,
            CharacterLinkedPoseGroupRuntimeLayout layout)
        {
            m_Group = group ?? throw new ArgumentNullException(nameof(group));
            m_Selector = selector ?? throw new ArgumentNullException(nameof(selector));
            m_Interface = linkedInterface ?? throw new ArgumentNullException(nameof(linkedInterface));
            m_Implementations = implementations ?? throw new ArgumentNullException(nameof(implementations));
            m_Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            m_Group.RequireValid();
            m_Selector.RequireValid();
            m_Interface.RequireValid();
            m_Layout.RequireValid();
            if (m_Group.GroupId != m_Selector.GroupId || m_Group.InterfaceId != m_Selector.InterfaceId ||
                m_Group.SelectorId != m_Selector.SelectorId || m_Layout.GroupId != m_Group.GroupId ||
                m_Interface.InterfaceId != m_Group.InterfaceId ||
                m_Interface.SignatureHash != m_Group.InterfaceSignature ||
                m_Layout.Implementations.Count != m_Selector.CandidateImplementationIds.Count)
            {
                throw new InvalidOperationException("Linked Pose runtime Group, selector and layout are inconsistent.");
            }
            for (int i = 0; i < m_Selector.CandidateImplementationIds.Count; i++)
            {
                var implementationId = new LinkedPoseImplementationId(m_Selector.CandidateImplementationIds[i]);
                _ = m_Layout.RequireImplementation(implementationId);
                _ = RequireImplementation(implementationId);
            }
        }

        public LinkedPoseGroupId GroupId => m_Group.GroupId;
        public CharacterLinkedPoseGenerationHandle Incoming => m_HasIncoming
            ? m_Incoming
            : throw new InvalidOperationException($"Linked Pose Group '{GroupId}' has no prepared generation.");

        public void Prepare(in CharacterLinkedPoseSelectionFrame frame)
        {
            if (m_HasIncoming)
                throw new InvalidOperationException($"Linked Pose Group '{GroupId}' is already prepared.");
            if (!frame.IsValid || frame.GroupId != m_Group.GroupId || frame.InterfaceId != m_Group.InterfaceId ||
                !m_Selector.Contains(frame.ImplementationId))
            {
                throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                    CharacterLinkedPoseDiagnosticCode.MissingMapping,
                    $"Selection frame for Group '{GroupId}' is invalid or outside the compiled candidate closure."));
            }
            CharacterLinkedPoseImplementationRuntimeLayout selected = m_Layout.RequireImplementation(frame.ImplementationId);
            if (!m_HasCommitted)
            {
                m_Incoming = new CharacterLinkedPoseGenerationHandle(
                    frame.GroupId,
                    frame.InterfaceId,
                    frame.ImplementationId,
                    frame.SelectionRevision,
                    1,
                    0,
                    true);
                m_IncomingImplementation = selected;
                m_HasIncoming = true;
                return;
            }
            int revisionOrder = frame.SelectionRevision.CompareTo(m_Committed.SelectionRevision);
            if (revisionOrder < 0 || revisionOrder == 0 && frame.ImplementationId != m_Committed.ImplementationId)
                throw new InvalidOperationException($"Linked Pose selection for Group '{GroupId}' regressed or changed without a revision.");
            if (revisionOrder == 0)
            {
                m_Incoming = new CharacterLinkedPoseGenerationHandle(
                    m_Committed.GroupId,
                    m_Committed.InterfaceId,
                    m_Committed.ImplementationId,
                    m_Committed.SelectionRevision,
                    m_Committed.Generation,
                    m_Committed.PageIndex,
                    false);
                m_IncomingImplementation = m_CommittedImplementation;
            }
            else
            {
                m_Incoming = new CharacterLinkedPoseGenerationHandle(
                    frame.GroupId,
                    frame.InterfaceId,
                    frame.ImplementationId,
                    frame.SelectionRevision,
                    checked(m_Committed.Generation + 1),
                    1 - m_Committed.PageIndex,
                    true);
                m_IncomingImplementation = selected;
            }
            m_HasIncoming = true;
        }

        public CharacterLinkedPoseEntryGenerationHandle RequireIncomingEntry(LinkedPoseEntryId entryId)
        {
            if (!m_HasIncoming || m_IncomingImplementation == null)
                throw new InvalidOperationException($"Linked Pose Group '{GroupId}' has no prepared generation.");
            return new CharacterLinkedPoseEntryGenerationHandle(
                in m_Incoming,
                m_IncomingImplementation.RequireEntry(entryId));
        }

        public CharacterLinkedPoseRuntimeGroupSnapshot CreateCommittedSnapshot()
        {
            if (!m_HasCommitted || m_CommittedImplementation == null)
                throw new InvalidOperationException($"Linked Pose Group '{GroupId}' has no committed generation.");
            CharacterLinkedPoseImplementationProjectionDescriptor implementation =
                RequireImplementation(m_Committed.ImplementationId);
            CharacterLinkedPoseRuntimeCapacity maximumCapacity = m_Layout.MaximumCapacity;
            CharacterLinkedPoseRuntimeCapacity activeCapacity = m_CommittedImplementation.Capacity;
            return new CharacterLinkedPoseRuntimeGroupSnapshot(
                m_Committed.GroupId,
                m_Committed.InterfaceId,
                m_Interface.SignatureHash,
                m_Selector.SelectorId,
                m_Committed.ImplementationId,
                implementation.Revision,
                implementation.ContentHash,
                m_Committed.SelectionRevision,
                m_Committed.Generation,
                m_Committed.PoseDiscontinuity,
                in maximumCapacity,
                in activeCapacity);
        }

        public void Seal()
        {
            if (!m_HasIncoming || m_IncomingImplementation == null)
                throw new InvalidOperationException($"Linked Pose Group '{GroupId}' has no incoming generation to seal.");
            m_Committed = m_Incoming;
            m_CommittedImplementation = m_IncomingImplementation;
            m_HasCommitted = true;
            m_Incoming = default;
            m_IncomingImplementation = null;
            m_HasIncoming = false;
        }

        public void Discard()
        {
            m_Incoming = default;
            m_IncomingImplementation = null;
            m_HasIncoming = false;
        }

        public void Reset()
        {
            m_Committed = default;
            m_Incoming = default;
            m_CommittedImplementation = null;
            m_IncomingImplementation = null;
            m_HasCommitted = false;
            m_HasIncoming = false;
        }

        CharacterLinkedPoseImplementationProjectionDescriptor RequireImplementation(
            LinkedPoseImplementationId implementationId)
        {
            for (int i = 0; i < m_Implementations.Count; i++)
            {
                CharacterLinkedPoseImplementationProjectionDescriptor implementation = m_Implementations[i];
                if (implementation != null && implementation.ImplementationId == implementationId)
                    return implementation;
            }
            throw new InvalidOperationException(
                CharacterLinkedPoseDiagnostic.Format(
                    CharacterLinkedPoseDiagnosticCode.SourceClosureMissing,
                    $"Group '{GroupId}' candidate '{implementationId}' is absent from the Projection catalog."));
        }
    }

    public sealed class CharacterLinkedPoseRuntimeSession
    {
        readonly CharacterLinkedPoseGroupRuntimeState[] m_Groups;
        readonly ICharacterLinkedPoseRuntimeSelectorAdapter[] m_Selectors;
        readonly CharacterLinkedPoseRuntimeLayoutCatalog m_Layouts;
        bool m_Prepared;

        public CharacterLinkedPoseRuntimeSession(
            CharacterPresentationProjection projection,
            IReadOnlyList<ICharacterLinkedPoseRuntimeSelectorAdapter> selectors)
        {
            projection = projection ?? throw new ArgumentNullException(nameof(projection));
            projection.RequirePosePayload();
            CharacterLinkedPoseProjectionPayload linkedPose = projection.LinkedPose;
            if (selectors == null)
                throw new ArgumentNullException(nameof(selectors));
            m_Layouts = CharacterLinkedPoseRuntimeLayoutBuilder.Build(projection);
            if (m_Layouts.Count != linkedPose.Groups.Count)
                throw new InvalidOperationException("Linked Pose runtime layout catalog does not match the Projection Group closure.");
            m_Selectors = new ICharacterLinkedPoseRuntimeSelectorAdapter[selectors.Count];
            for (int i = 0; i < selectors.Count; i++)
                m_Selectors[i] = selectors[i] ?? throw new InvalidOperationException($"Linked Pose runtime selector #{i} is missing.");
            m_Groups = new CharacterLinkedPoseGroupRuntimeState[linkedPose.Groups.Count];
            for (int groupIndex = 0; groupIndex < linkedPose.Groups.Count; groupIndex++)
            {
                CharacterLinkedPoseGroupProjectionDescriptor group = linkedPose.Groups[groupIndex];
                CharacterLinkedPoseCompiledSelectorDescriptor selector = RequireSelector(linkedPose.Selectors, group.GroupId);
                int adapterCount = CountAdapters(group.GroupId);
                if (adapterCount != 1)
                {
                    CharacterLinkedPoseDiagnosticCode code = adapterCount == 0
                        ? CharacterLinkedPoseDiagnosticCode.MissingSelector
                        : CharacterLinkedPoseDiagnosticCode.DuplicateSelector;
                    throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                        code,
                        $"Runtime Group '{group.GroupId}' has {adapterCount} selector adapters; exactly one is required."));
                }
                m_Groups[groupIndex] = new CharacterLinkedPoseGroupRuntimeState(
                    group,
                    selector,
                    RequireInterface(linkedPose.Interfaces, group.InterfaceId),
                    linkedPose.Implementations,
                    m_Layouts.RequireGroup(group.GroupId));
            }
        }

        public CharacterLinkedPoseRuntimeLayoutCatalog Layouts => m_Layouts;
        public int GroupCount => m_Groups.Length;

        public CharacterLinkedPoseRuntimeGroupSnapshot CreateCommittedSnapshot(int groupIndex)
        {
            if ((uint)groupIndex >= (uint)m_Groups.Length)
                throw new ArgumentOutOfRangeException(nameof(groupIndex));
            return m_Groups[groupIndex].CreateCommittedSnapshot();
        }

        public void Prepare()
        {
            if (m_Prepared)
                throw new InvalidOperationException("Linked Pose runtime session is already prepared.");
            int preparedCount = 0;
            try
            {
                for (int i = 0; i < m_Groups.Length; i++)
                {
                    CharacterLinkedPoseGroupRuntimeState group = m_Groups[i];
                    ICharacterLinkedPoseRuntimeSelectorAdapter selector = RequireAdapter(group.GroupId);
                    if (!selector.TryReadSelection(out CharacterLinkedPoseSelectionFrame frame))
                        throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                            CharacterLinkedPoseDiagnosticCode.MissingMapping,
                            $"Group '{group.GroupId}' has no committed selector frame."));
                    group.Prepare(in frame);
                    preparedCount++;
                }
                m_Prepared = true;
            }
            catch
            {
                for (int i = 0; i < preparedCount; i++)
                    m_Groups[i].Discard();
                throw;
            }
        }

        public CharacterLinkedPoseGenerationHandle RequireIncoming(LinkedPoseGroupId groupId)
        {
            if (!m_Prepared)
                throw new InvalidOperationException("Linked Pose runtime session is not prepared.");
            for (int i = 0; i < m_Groups.Length; i++)
            {
                if (m_Groups[i].GroupId == groupId)
                    return m_Groups[i].Incoming;
            }
            throw new InvalidOperationException($"Linked Pose runtime Group '{groupId}' is absent from the Projection.");
        }

        public CharacterLinkedPoseEntryGenerationHandle RequireIncomingEntry(
            LinkedPoseGroupId groupId,
            LinkedPoseEntryId entryId)
        {
            if (!m_Prepared)
                throw new InvalidOperationException("Linked Pose runtime session is not prepared.");
            for (int i = 0; i < m_Groups.Length; i++)
            {
                if (m_Groups[i].GroupId == groupId)
                    return m_Groups[i].RequireIncomingEntry(entryId);
            }
            throw new InvalidOperationException($"Linked Pose runtime Group '{groupId}' is absent from the Projection.");
        }

        public void Seal()
        {
            if (!m_Prepared)
                throw new InvalidOperationException("Linked Pose runtime session is not prepared.");
            for (int i = 0; i < m_Groups.Length; i++)
                m_Groups[i].Seal();
            m_Prepared = false;
        }

        public void Discard()
        {
            for (int i = 0; i < m_Groups.Length; i++)
                m_Groups[i].Discard();
            m_Prepared = false;
        }

        public void Reset()
        {
            Discard();
            for (int i = 0; i < m_Groups.Length; i++)
                m_Groups[i].Reset();
            for (int i = 0; i < m_Selectors.Length; i++)
                m_Selectors[i].Reset();
        }

        static CharacterLinkedPoseCompiledSelectorDescriptor RequireSelector(
            IReadOnlyList<CharacterLinkedPoseCompiledSelectorDescriptor> selectors,
            LinkedPoseGroupId groupId)
        {
            CharacterLinkedPoseCompiledSelectorDescriptor result = null;
            for (int i = 0; i < selectors.Count; i++)
            {
                if (selectors[i].GroupId != groupId)
                    continue;
                if (result != null)
                    throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                        CharacterLinkedPoseDiagnosticCode.DuplicateSelector,
                        $"Runtime Group '{groupId}' has duplicate compiled selectors."));
                result = selectors[i];
            }
            return result ?? throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                CharacterLinkedPoseDiagnosticCode.MissingSelector,
                $"Runtime Group '{groupId}' has no compiled selector."));
        }

        static CharacterLinkedPoseInterfaceProjectionDescriptor RequireInterface(
            IReadOnlyList<CharacterLinkedPoseInterfaceProjectionDescriptor> interfaces,
            LinkedPoseInterfaceId interfaceId)
        {
            CharacterLinkedPoseInterfaceProjectionDescriptor result = null;
            for (int i = 0; i < interfaces.Count; i++)
            {
                if (interfaces[i].InterfaceId != interfaceId)
                    continue;
                if (result != null)
                    throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                        CharacterLinkedPoseDiagnosticCode.SignatureMismatch,
                        $"Interface '{interfaceId}' is duplicated in the Projection."));
                result = interfaces[i];
            }
            return result ?? throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                CharacterLinkedPoseDiagnosticCode.SignatureMismatch,
                $"Interface '{interfaceId}' is absent from the Projection."));
        }

        ICharacterLinkedPoseRuntimeSelectorAdapter RequireAdapter(LinkedPoseGroupId groupId)
        {
            for (int i = 0; i < m_Selectors.Length; i++)
            {
                if (m_Selectors[i].GroupId == groupId)
                    return m_Selectors[i];
            }
            throw new InvalidOperationException($"Linked Pose runtime Group '{groupId}' has no selector adapter.");
        }

        int CountAdapters(LinkedPoseGroupId groupId)
        {
            int count = 0;
            for (int i = 0; i < m_Selectors.Length; i++)
            {
                if (m_Selectors[i].GroupId == groupId)
                    count++;
            }
            return count;
        }
    }
}
