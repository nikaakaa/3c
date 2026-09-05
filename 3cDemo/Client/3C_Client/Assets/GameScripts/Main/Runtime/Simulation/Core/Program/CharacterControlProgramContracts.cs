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
        public CharacterControlParameterDescriptor(CharacterControlParameterId id, SemanticValueKind valueKind, double numericValue = 0d)
        {
            if (!id.IsValid || !Enum.IsDefined(typeof(SemanticValueKind), valueKind) || double.IsNaN(numericValue) || double.IsInfinity(numericValue))
                throw new ArgumentException("Character control parameter descriptor is incomplete.");
            Id = id;
            ValueKind = valueKind;
            NumericValue = numericValue;
        }

        public CharacterControlParameterId Id { get; }
        public SemanticValueKind ValueKind { get; }
        public double NumericValue { get; }
    }

    public enum CharacterControlMotionExecutionMode : byte
    {
        Once = 1,
        Timed = 2,
        Continuous = 3
    }

    public sealed class CharacterControlMotionDescriptor
    {
        public CharacterControlMotionDescriptor(
            string binding,
            SimulationInputValueId input,
            double moveSpeed,
            double turnSpeedDegrees,
            CharacterControlMotionExecutionMode executionMode,
            double durationSeconds)
        {
            Binding = SimulationIdentity.Require(binding, nameof(binding));
            if (!input.IsValid || moveSpeed < 0d || turnSpeedDegrees < 0d ||
                !Enum.IsDefined(typeof(CharacterControlMotionExecutionMode), executionMode) ||
                double.IsNaN(durationSeconds) || double.IsInfinity(durationSeconds) || durationSeconds < 0d)
                throw new ArgumentException("Character control motion descriptor is incomplete.");
            if (executionMode == CharacterControlMotionExecutionMode.Timed && durationSeconds <= 0d)
                throw new ArgumentException("Timed character control motion requires a duration.", nameof(durationSeconds));
            Input = input;
            MoveSpeed = moveSpeed;
            TurnSpeedDegrees = turnSpeedDegrees;
            ExecutionMode = executionMode;
            DurationSeconds = durationSeconds;
        }

        public string Binding { get; }
        public SimulationInputValueId Input { get; }
        public double MoveSpeed { get; }
        public double TurnSpeedDegrees { get; }
        public CharacterControlMotionExecutionMode ExecutionMode { get; }
        public double DurationSeconds { get; }
    }

    public sealed class CharacterControlModuleContract
    {
        readonly ReadOnlyCollection<CharacterControlStateDescriptor> m_States;
        readonly ReadOnlyCollection<CharacterControlTransitionDescriptor> m_Transitions;
        readonly ReadOnlyCollection<CharacterControlStateFieldDescriptor> m_StateFields;
        readonly ReadOnlyCollection<CharacterControlParameterDescriptor> m_Parameters;
        readonly ReadOnlyCollection<SimulationInputValueId> m_InputValues;
        readonly ReadOnlyCollection<CharacterControlMotionDescriptor> m_Motions;
        readonly ReadOnlyCollection<CharacterSkillId> m_Skills;

        public CharacterControlModuleContract(
            CharacterControlModuleId moduleId,
            int semanticVersion,
            CharacterControlStateId initialState,
            IEnumerable<CharacterControlStateDescriptor> states,
            IEnumerable<CharacterControlTransitionDescriptor> transitions,
            IEnumerable<CharacterControlStateFieldDescriptor> stateFields,
            IEnumerable<CharacterControlParameterDescriptor> parameters,
            IEnumerable<SimulationInputValueId> inputValues,
            IEnumerable<CharacterControlMotionDescriptor> motions,
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
            m_Motions = Freeze(motions, value => value.Binding, "motion");
            m_Skills = Freeze(skills, value => value, "skill");
            InitialState = initialState;
            ValidateStateGraph();
        }

        public CharacterControlModuleId ModuleId { get; }
        public int SemanticVersion { get; }
        public CharacterControlStateId InitialState { get; }
        public IReadOnlyList<CharacterControlStateDescriptor> States => m_States;
        public IReadOnlyList<CharacterControlTransitionDescriptor> Transitions => m_Transitions;
        public IReadOnlyList<CharacterControlStateFieldDescriptor> StateFields => m_StateFields;
        public IReadOnlyList<CharacterControlParameterDescriptor> Parameters => m_Parameters;
        public IReadOnlyList<SimulationInputValueId> InputValues => m_InputValues;
        public IReadOnlyList<CharacterControlMotionDescriptor> Motions => m_Motions;
        public IReadOnlyList<CharacterSkillId> Skills => m_Skills;

        public CharacterControlStateFieldDescriptor FindStateField(ProgramStateSemantic semantic)
        {
            CharacterControlStateFieldDescriptor result = null;
            for (int i = 0; i < m_StateFields.Count; i++)
            {
                CharacterControlStateFieldDescriptor field = m_StateFields[i];
                if (field.Semantic != semantic)
                    continue;
                if (result != null)
                    throw new InvalidOperationException($"Character control module '{ModuleId}' has duplicate state field semantic '{semantic}'.");
                result = field;
            }
            return result;
        }

        public CharacterControlStateFieldDescriptor RequireStateField(ProgramStateSemantic semantic) =>
            FindStateField(semantic) ?? throw new InvalidOperationException(
                $"Character control module '{ModuleId}' has no state field for semantic '{semantic}'.");

        void ValidateStateGraph()
        {
            var stateIds = new HashSet<CharacterControlStateId>();
            for (int i = 0; i < m_States.Count; i++)
                stateIds.Add(m_States[i].Id);
            if (!InitialState.IsValid || !stateIds.Contains(InitialState))
                throw new ArgumentException("Character control module initial state is not declared.", nameof(InitialState));
            for (int i = 0; i < m_Transitions.Count; i++)
            {
                CharacterControlTransitionDescriptor transition = m_Transitions[i];
                if (!stateIds.Contains(transition.Source) || !stateIds.Contains(transition.Target))
                    throw new ArgumentException(
                        $"Character control transition '{transition.Id}' references an undeclared state.",
                        nameof(m_Transitions));
            }
        }

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

    public sealed class CharacterControlModuleCatalog
    {
        readonly Dictionary<CharacterControlModuleId, ICharacterControlModule> m_Modules;

        public CharacterControlModuleCatalog(IEnumerable<ICharacterControlModule> modules)
        {
            m_Modules = new Dictionary<CharacterControlModuleId, ICharacterControlModule>();
            if (modules == null)
                return;
            foreach (ICharacterControlModule module in modules)
            {
                if (module == null || module.Contract == null)
                    throw new ArgumentException("Character control module catalog contains an incomplete module.", nameof(modules));
                if (!m_Modules.TryAdd(module.Contract.ModuleId, module))
                    throw new ArgumentException(
                        $"Character control module '{module.Contract.ModuleId}' is registered more than once.",
                        nameof(modules));
            }
        }

        public ICharacterControlModule Require(CharacterControlModuleBinding binding)
        {
            if (!binding.IsValid)
                throw new ArgumentException("Character control module binding is invalid.", nameof(binding));
            if (!m_Modules.TryGetValue(binding.ModuleId, out ICharacterControlModule module))
                throw new InvalidOperationException($"Character control module '{binding.ModuleId}' is not installed.");
            if (module.Contract.SemanticVersion != binding.SemanticVersion)
                throw new InvalidOperationException(
                    $"Character control module '{binding.ModuleId}' version '{module.Contract.SemanticVersion}' does not match Program version '{binding.SemanticVersion}'.");
            return module;
        }
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

        public static void ValidateSkillPrograms(
            CharacterControlModuleContract contract,
            CharacterSkillProgramCatalog skills)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            if (skills == null)
                throw new ArgumentNullException(nameof(skills));
            for (int i = 0; i < contract.Skills.Count; i++)
                skills.Require(contract.Skills[i]);
            for (int i = 0; i < skills.Bindings.Count; i++)
            {
                CharacterSkillId skill = skills.Bindings[i].SkillId;
                bool declared = false;
                for (int skillIndex = 0; skillIndex < contract.Skills.Count; skillIndex++)
                {
                    if (contract.Skills[skillIndex] == skill)
                    {
                        declared = true;
                        break;
                    }
                }
                if (!declared)
                    throw new InvalidDataException($"SkillProgram '{skill}' is not declared by control module '{contract.ModuleId}'.");
            }
        }
    }
}
