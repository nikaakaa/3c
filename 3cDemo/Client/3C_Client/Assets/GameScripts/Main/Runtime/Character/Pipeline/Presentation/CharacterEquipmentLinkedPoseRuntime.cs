using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    sealed class CharacterEquipmentLinkedPoseRuntime
    {
        readonly ActorId m_ActorId;
        readonly CharacterEquipmentLinkedPoseSelectionAdapter[] m_EquipmentAdapters;
        readonly bool[] m_Captured;
        readonly Dictionary<LinkedPoseGroupId, LinkedPoseImplementationId> m_PreviewOverrides =
            new Dictionary<LinkedPoseGroupId, LinkedPoseImplementationId>();
        readonly CharacterLinkedPoseRuntimeSession m_Session;

        public CharacterEquipmentLinkedPoseRuntime(
            ActorId actorId,
            CharacterPresentationProjection projection)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Linked Pose runtime Actor identity is invalid.", nameof(actorId));
            projection = projection ?? throw new ArgumentNullException(nameof(projection));
            projection.RequirePosePayload();
            CharacterLinkedPoseProjectionPayload linkedPose = projection.LinkedPose;
            m_ActorId = actorId;
            m_EquipmentAdapters = new CharacterEquipmentLinkedPoseSelectionAdapter[linkedPose.EquipmentSelectors.Count];
            var adapters = new ICharacterLinkedPoseRuntimeSelectorAdapter[m_EquipmentAdapters.Length];
            for (int i = 0; i < m_EquipmentAdapters.Length; i++)
            {
                var adapter = new CharacterEquipmentLinkedPoseSelectionAdapter(linkedPose.EquipmentSelectors[i]);
                m_EquipmentAdapters[i] = adapter;
                adapters[i] = adapter;
            }
            m_Captured = new bool[m_EquipmentAdapters.Length];
            m_Session = new CharacterLinkedPoseRuntimeSession(projection, adapters);
        }

        public CharacterLinkedPoseRuntimeSession Session => m_Session;

        public void Capture(IReadOnlyList<EquipmentVisualSelection> selections)
        {
            if (selections == null)
                throw new ArgumentNullException(nameof(selections));
            Array.Clear(m_Captured, 0, m_Captured.Length);
            for (int selectionIndex = 0; selectionIndex < selections.Count; selectionIndex++)
            {
                EquipmentVisualSelection selection = selections[selectionIndex];
                if (selection.ActorId != m_ActorId)
                    throw new InvalidOperationException("Equipment Linked Pose selection targets another Actor.");
                for (int adapterIndex = 0; adapterIndex < m_EquipmentAdapters.Length; adapterIndex++)
                {
                    CharacterEquipmentLinkedPoseSelectionAdapter adapter = m_EquipmentAdapters[adapterIndex];
                    if (adapter.SlotId != selection.SlotId)
                        continue;
                    if (m_Captured[adapterIndex])
                        throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                            CharacterLinkedPoseDiagnosticCode.DuplicateSelector,
                            $"Equipment Group '{adapter.GroupId}' received duplicate Slot '{selection.SlotId}' selections."));
                    adapter.Capture(in selection);
                    m_Captured[adapterIndex] = true;
                }
            }
            for (int i = 0; i < m_Captured.Length; i++)
            {
                if (!m_Captured[i])
                    throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                        CharacterLinkedPoseDiagnosticCode.MissingMapping,
                        $"Equipment Group '{m_EquipmentAdapters[i].GroupId}' did not receive its committed Slot selection."));
            }
            ApplyPreviewOverrides();
        }

        public void SetPreviewOverride(
            LinkedPoseGroupId groupId,
            LinkedPoseImplementationId implementationId)
        {
            if (!groupId.IsValid || !implementationId.IsValid)
                throw new ArgumentException("Linked Pose preview override identity is incomplete.");
            m_PreviewOverrides[groupId] = implementationId;
            ApplyPreviewOverrides();
        }

        public void ClearPreviewOverride(LinkedPoseGroupId groupId)
        {
            if (!groupId.IsValid)
                return;
            if (!m_PreviewOverrides.Remove(groupId))
                return;
            for (int i = 0; i < m_EquipmentAdapters.Length; i++)
            {
                if (m_EquipmentAdapters[i].GroupId == groupId)
                    m_EquipmentAdapters[i].ClearPreviewSelection();
            }
        }

        public void ClearPreviewOverrides()
        {
            m_PreviewOverrides.Clear();
            for (int i = 0; i < m_EquipmentAdapters.Length; i++)
                m_EquipmentAdapters[i].ClearPreviewSelection();
        }

        public void Prepare() => m_Session.Prepare();
        public void Seal() => m_Session.Seal();
        public void Discard() => m_Session.Discard();
        public void Reset() => m_Session.Reset();

        void ApplyPreviewOverrides()
        {
            if (m_PreviewOverrides.Count == 0)
                return;
            foreach (KeyValuePair<LinkedPoseGroupId, LinkedPoseImplementationId> overrideValue in m_PreviewOverrides)
            {
                for (int i = 0; i < m_EquipmentAdapters.Length; i++)
                {
                    CharacterEquipmentLinkedPoseSelectionAdapter adapter = m_EquipmentAdapters[i];
                    if (adapter.GroupId == overrideValue.Key)
                        adapter.SetPreviewSelection(overrideValue.Value);
                }
            }
        }
    }
}
