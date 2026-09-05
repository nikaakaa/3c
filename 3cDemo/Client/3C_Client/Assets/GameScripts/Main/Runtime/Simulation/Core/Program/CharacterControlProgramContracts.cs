using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace ThirdPersonSimulation
{
    public enum CharacterControlNumericComparison : byte
    {
        Less = 1,
        LessOrEqual = 2,
        Equal = 3,
        Greater = 4,
        GreaterOrEqual = 5
    }

    public sealed class CharacterControlStateDescriptor
    {
        public CharacterControlStateDescriptor(CharacterControlStateId id, int evaluationOrder)
        {
            if (!id.IsValid || evaluationOrder < 0)
                throw new ArgumentException("Character control state descriptor is incomplete.");
            Id = id;
            EvaluationOrder = evaluationOrder;
        }

        public CharacterControlStateId Id { get; }
        public int EvaluationOrder { get; }
    }

    public sealed class CharacterControlTransitionDescriptor
    {
        public CharacterControlTransitionDescriptor(
            CharacterControlTransitionId id,
            CharacterControlStateId source,
            CharacterControlStateId target,
            int priority,
            int evaluationOrder)
        {
            if (!id.IsValid || !source.IsValid || !target.IsValid || evaluationOrder < 0)
                throw new ArgumentException("Character control transition descriptor is incomplete.");
            Id = id;
            Source = source;
            Target = target;
            Priority = priority;
            EvaluationOrder = evaluationOrder;
        }

        public CharacterControlTransitionId Id { get; }
        public CharacterControlStateId Source { get; }
        public CharacterControlStateId Target { get; }
        public int Priority { get; }
        public int EvaluationOrder { get; }
    }

    public sealed class CharacterControlStateFieldDescriptor
    {
        public CharacterControlStateFieldDescriptor(
            CharacterControlStateFieldId id,
            ProgramStateValueKind valueKind,
            ProgramStateSemantic semantic)
        {
            if (!id.IsValid)
                throw new ArgumentException("Character control state field identity is invalid.", nameof(id));
            ProgramStateSchema.RequireSlot(valueKind, ProgramStateOwnerKind.Control, semantic);
            Id = id;
            ValueKind = valueKind;
            Semantic = semantic;
        }

        public CharacterControlStateFieldId Id { get; }
        public ProgramStateValueKind ValueKind { get; }
        public ProgramStateSemantic Semantic { get; }
    }

    public sealed class CharacterControlParameterDescriptor
    {
        public CharacterControlParameterDescriptor(CharacterControlParameterId id, SemanticValueKind valueKind)
        {
            if (!id.IsValid || !Enum.IsDefined(typeof(SemanticValueKind), valueKind))
                throw new ArgumentException("Character control parameter descriptor is incomplete.");
            Id = id;
            ValueKind = valueKind;
        }

        public CharacterControlParameterId Id { get; }
        public SemanticValueKind ValueKind { get; }
    }

    public sealed class CharacterControlModuleContract
    {
        readonly ReadOnlyCollection<CharacterControlStateDescriptor> m_States;
        readonly ReadOnlyCollection<CharacterControlTransitionDescriptor> m_Transitions;
        readonly ReadOnlyCollection<CharacterControlStateFieldDescriptor> m_StateFields;
        readonly ReadOnlyCollection<CharacterControlParameterDescriptor> m_Parameters;
        readonly ReadOnlyCollection<SimulationInputValueId> m_InputValues;
        readonly ReadOnlyCollection<CharacterSkillId> m_Skills;

        public CharacterControlModuleContract(
            CharacterControlModuleId moduleId,
            int semanticVersion,
            IEnumerable<CharacterControlStateDescriptor> states,
            IEnumerable<CharacterControlTransitionDescriptor> transitions,
            IEnumerable<CharacterControlStateFieldDescriptor> stateFields,
            IEnumerable<CharacterControlParameterDescriptor> parameters,
            IEnumerable<SimulationInputValueId> inputValues,
            IEnumerable<CharacterSkillId> skills)
        {
            if (!moduleId.IsValid || semanticVersion <= 0)
                throw new ArgumentException("Character control module contract is incomplete.");
            ModuleId = moduleId;
            SemanticVersion = semanticVersion;
            m_States = Freeze(states, value => value.Id, "state");
            m_Transitions = Freeze(transitions, value => value.Id, "transition");
            m_StateFields = Freeze(stateFields, value => value.Id, "state field");
            m_Parameters = Freeze(parameters, value => value.Id, "parameter");
            m_InputValues = Freeze(inputValues, value => value, "input value");
            m_Skills = Freeze(skills, value => value, "skill");
        }

        public CharacterControlModuleId ModuleId { get; }
        public int SemanticVersion { get; }
        public IReadOnlyList<CharacterControlStateDescriptor> States => m_States;
        public IReadOnlyList<CharacterControlTransitionDescriptor> Transitions => m_Transitions;
        public IReadOnlyList<CharacterControlStateFieldDescriptor> StateFields => m_StateFields;
        public IReadOnlyList<CharacterControlParameterDescriptor> Parameters => m_Parameters;
        public IReadOnlyList<SimulationInputValueId> InputValues => m_InputValues;
        public IReadOnlyList<CharacterSkillId> Skills => m_Skills;

        static ReadOnlyCollection<T> Freeze<T, TKey>(IEnumerable<T> source, Func<T, TKey> key, string label)
            where T : class
        {
            var values = source == null ? new List<T>() : new List<T>(source);
            var keys = new HashSet<TKey>();
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null || !keys.Add(key(values[i])))
                    throw new ArgumentException($"Character control {label} identity is invalid or duplicated.", nameof(source));
            }
            return values.AsReadOnly();
        }

        static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> source, Func<T, T> key, string label)
            where T : struct
        {
            var values = source == null ? new List<T>() : new List<T>(source);
            var keys = new HashSet<T>();
            for (int i = 0; i < values.Count; i++)
            {
                if (!keys.Add(key(values[i])))
                    throw new ArgumentException($"Character control {label} identity is duplicated.", nameof(source));
            }
            values.Sort((left, right) => Comparer<T>.Default.Compare(left, right));
            return values.AsReadOnly();
        }
    }

    public readonly struct CharacterControlModuleBinding
    {
        public CharacterControlModuleBinding(int catalogEntryIndex, CharacterControlModuleId moduleId, int semanticVersion)
        {
            if (catalogEntryIndex < 0 || !moduleId.IsValid || semanticVersion <= 0)
                throw new ArgumentException("Character control module binding is incomplete.");
            CatalogEntryIndex = catalogEntryIndex;
            ModuleId = moduleId;
            SemanticVersion = semanticVersion;
        }

        public int CatalogEntryIndex { get; }
        public CharacterControlModuleId ModuleId { get; }
        public int SemanticVersion { get; }
        public bool IsValid => CatalogEntryIndex >= 0 && ModuleId.IsValid && SemanticVersion > 0;
    }

    public static class CharacterControlProgramCatalogValidator
    {
        public static CharacterControlModuleBinding Resolve(
            IReadOnlyList<ProgramCatalogEntry> catalogEntries,
            IReadOnlyList<ProgramStateSlot> stateSlots)
        {
            if (catalogEntries == null)
                throw new ArgumentNullException(nameof(catalogEntries));
            if (stateSlots == null)
                throw new ArgumentNullException(nameof(stateSlots));

            CharacterControlModuleBinding binding = default;
            for (int i = 0; i < catalogEntries.Count; i++)
            {
                ProgramCatalogEntry entry = catalogEntries[i];
                if (entry.Kind != ProgramCatalogEntryKind.ControlModule)
                    continue;
                if (binding.IsValid)
                    throw new InvalidDataException("Program contains more than one Character control module.");
                binding = new CharacterControlModuleBinding(
                    entry.Index,
                    new CharacterControlModuleId(entry.Identity),
                    entry.Revision);
            }

            for (int i = 0; i < stateSlots.Count; i++)
            {
                ProgramStateSlot slot = stateSlots[i];
                if (slot.OwnerKind != ProgramStateOwnerKind.Control)
                    continue;
                if (!binding.IsValid || !string.Equals(slot.OwnerIdentity, binding.ModuleId.Value, StringComparison.Ordinal))
                    throw new InvalidDataException($"Control state slot '{slot.Identity}' has no matching ControlModule catalog binding.");
            }
            return binding;
        }
    }
}
