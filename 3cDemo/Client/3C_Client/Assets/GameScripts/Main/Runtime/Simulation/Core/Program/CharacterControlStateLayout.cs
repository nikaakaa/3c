using System;
using System.Collections.Generic;
using System.IO;

namespace ThirdPersonSimulation
{
    public sealed class CharacterControlStateLayout
    {
        readonly Dictionary<CharacterControlStateFieldId, int> m_Slots;
        readonly Dictionary<CharacterControlStateFieldId, ProgramStateValueKind> m_Kinds;

        public CharacterControlStateLayout(
            CharacterControlModuleContract contract,
            IReadOnlyList<ProgramStateSlot> stateSlots)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            if (stateSlots == null)
                throw new ArgumentNullException(nameof(stateSlots));
            m_Slots = new Dictionary<CharacterControlStateFieldId, int>();
            m_Kinds = new Dictionary<CharacterControlStateFieldId, ProgramStateValueKind>();
            for (int i = 0; i < stateSlots.Count; i++)
            {
                ProgramStateSlot slot = stateSlots[i];
                if (slot.OwnerKind != ProgramStateOwnerKind.Control)
                    continue;
                var fieldId = new CharacterControlStateFieldId(slot.Identity);
                CharacterControlStateFieldDescriptor field = FindField(contract, fieldId);
                if (field == null)
                    throw new InvalidDataException(
                        $"Control state slot '{slot.Identity}' is not declared by module '{contract.ModuleId}'.");
                if (field.ValueKind != slot.ValueKind || field.Semantic != slot.Semantic)
                    throw new InvalidDataException(
                        $"Control state field '{fieldId}' does not match its Program state slot.");
                if (!m_Slots.TryAdd(fieldId, i))
                    throw new InvalidDataException($"Control state field '{fieldId}' has multiple Program slots.");
                m_Kinds.Add(fieldId, field.ValueKind);
            }
            for (int i = 0; i < contract.StateFields.Count; i++)
            {
                CharacterControlStateFieldDescriptor field = contract.StateFields[i];
                if (!m_Slots.ContainsKey(field.Id))
                    throw new InvalidDataException(
                        $"Control state field '{field.Id}' has no Program state slot for module '{contract.ModuleId}'.");
            }
        }

        public int RequireSlot(CharacterControlStateFieldId field)
        {
            if (!m_Slots.TryGetValue(field, out int slot))
                throw new InvalidOperationException($"Control state field '{field}' is not bound to a Program state slot.");
            return slot;
        }

        public ProgramStateValueKind RequireKind(CharacterControlStateFieldId field) =>
            m_Kinds.TryGetValue(field, out ProgramStateValueKind kind)
                ? kind
                : throw new InvalidOperationException($"Control state field '{field}' is not bound to a Program state slot.");

        static CharacterControlStateFieldDescriptor FindField(
            CharacterControlModuleContract contract,
            CharacterControlStateFieldId fieldId)
        {
            for (int i = 0; i < contract.StateFields.Count; i++)
            {
                if (contract.StateFields[i].Id == fieldId)
                    return contract.StateFields[i];
            }
            return null;
        }
    }
}
