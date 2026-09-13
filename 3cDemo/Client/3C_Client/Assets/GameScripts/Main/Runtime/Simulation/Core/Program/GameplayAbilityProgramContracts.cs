using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace ThirdPersonSimulation
{
    public readonly struct GameplayAbilityProgramEndRule
    {
        public GameplayAbilityProgramEndRule(
            string trigger,
            int transition,
            string actionWindowType,
            string reason)
        {
            Trigger = SimulationIdentity.Require(trigger, nameof(trigger));
            if (!IsKnownTrigger(Trigger))
                throw new ArgumentException("Gameplay Ability end trigger is invalid.", nameof(trigger));
            if (!IsTerminalTransition(transition))
                throw new ArgumentOutOfRangeException(nameof(transition));
            Transition = transition;
            ActionWindowType = actionWindowType ?? string.Empty;
            Reason = reason ?? string.Empty;
            if (Trigger == GameplayAbilityEndTriggerNames.ActionWindowClosed &&
                string.IsNullOrWhiteSpace(ActionWindowType))
                throw new ArgumentException("Action window end rule requires a window type.", nameof(actionWindowType));
        }

        public string Trigger { get; }
        public int Transition { get; }
        public string ActionWindowType { get; }
        public string Reason { get; }

        static bool IsTerminalTransition(int transition) =>
            transition == 2 ||
            transition == 3 ||
            transition == 4 ||
            transition == 7;

        static bool IsKnownTrigger(string trigger) =>
            string.Equals(trigger, GameplayAbilityEndTriggerNames.ExecutionCompleted, StringComparison.Ordinal) ||
            string.Equals(trigger, GameplayAbilityEndTriggerNames.CancelRequested, StringComparison.Ordinal) ||
            string.Equals(trigger, GameplayAbilityEndTriggerNames.InterruptRequested, StringComparison.Ordinal) ||
            string.Equals(trigger, GameplayAbilityEndTriggerNames.AbortRequested, StringComparison.Ordinal) ||
            string.Equals(trigger, GameplayAbilityEndTriggerNames.ActionWindowClosed, StringComparison.Ordinal);
    }

    public static class GameplayAbilityEndTriggerNames
    {
        public const string ExecutionCompleted = "ExecutionCompleted";
        public const string CancelRequested = "CancelRequested";
        public const string InterruptRequested = "InterruptRequested";
        public const string AbortRequested = "AbortRequested";
        public const string ActionWindowClosed = "ActionWindowClosed";
    }

    public readonly struct GameplayAbilityDependency : IEquatable<GameplayAbilityDependency>
    {
        public GameplayAbilityDependency(string subgraphIdentity, string callSiteIdentity)
        {
            SubgraphIdentity = SimulationIdentity.Require(subgraphIdentity, nameof(subgraphIdentity));
            CallSiteIdentity = SimulationIdentity.Require(callSiteIdentity, nameof(callSiteIdentity));
        }

        public string SubgraphIdentity { get; }
        public string CallSiteIdentity { get; }
        public bool IsValid => !string.IsNullOrEmpty(SubgraphIdentity) && !string.IsNullOrEmpty(CallSiteIdentity);
        public bool Equals(GameplayAbilityDependency other) =>
            string.Equals(SubgraphIdentity, other.SubgraphIdentity, StringComparison.Ordinal) &&
            string.Equals(CallSiteIdentity, other.CallSiteIdentity, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is GameplayAbilityDependency other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(SubgraphIdentity, CallSiteIdentity);
    }

    public sealed class GameplayAbilityEntrySignature
    {
        readonly ReadOnlyCollection<CharacterControlParameterDescriptor> m_Parameters;

        public GameplayAbilityEntrySignature(IEnumerable<CharacterControlParameterDescriptor> parameters)
        {
            var values = parameters == null
                ? new List<CharacterControlParameterDescriptor>()
                : new List<CharacterControlParameterDescriptor>(parameters);
            var ids = new HashSet<CharacterControlParameterId>();
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null || !ids.Add(values[i].Id))
                    throw new ArgumentException("Character skill entry parameters are invalid or duplicated.", nameof(parameters));
            }
            values.Sort((left, right) => left.Id.CompareTo(right.Id));
            m_Parameters = values.AsReadOnly();
        }

        public IReadOnlyList<CharacterControlParameterDescriptor> Parameters => m_Parameters;
    }

    public sealed class GameplayAbilityProgramDefinition
    {
        readonly ReadOnlyCollection<GameplayAbilityDependency> m_Dependencies;
        readonly ReadOnlyCollection<CharacterSkillId> m_AllowedFollowUps;

        public GameplayAbilityProgramDefinition(
            CharacterSkillId skillId,
            string admissionProfileId,
            string entryIdentity,
            GameplayAbilityEntrySignature entrySignature,
            IEnumerable<GameplayAbilityDependency> dependencies,
            IEnumerable<CharacterSkillId> allowedFollowUps,
            string sourceInputRequestId = "",
            bool consumeSourceInputRequest = true,
            string targetInputValueId = "",
            string targetKey = "")
        {
            if (!skillId.IsValid)
                throw new ArgumentException("Character skill identity is invalid.", nameof(skillId));
            SkillId = skillId;
            AdmissionProfileId = SimulationIdentity.Require(admissionProfileId, nameof(admissionProfileId));
            EntryIdentity = SimulationIdentity.Require(entryIdentity, nameof(entryIdentity));
            EntrySignature = entrySignature ?? throw new ArgumentNullException(nameof(entrySignature));
            SourceInputRequestId = sourceInputRequestId ?? string.Empty;
            TargetInputValueId = targetInputValueId ?? string.Empty;
            TargetKey = targetKey ?? string.Empty;
            ConsumeSourceInputRequest = consumeSourceInputRequest;
            m_Dependencies = FreezeDependencies(dependencies);
            m_AllowedFollowUps = FreezeFollowUps(skillId, allowedFollowUps);
        }

        public CharacterSkillId SkillId { get; }
        public string AdmissionProfileId { get; }
        public string EntryIdentity { get; }
        public GameplayAbilityEntrySignature EntrySignature { get; }
        public string SourceInputRequestId { get; }
        public bool ConsumeSourceInputRequest { get; }
        public string TargetInputValueId { get; }
        public string TargetKey { get; }
        public IReadOnlyList<GameplayAbilityDependency> Dependencies => m_Dependencies;
        public IReadOnlyList<CharacterSkillId> AllowedFollowUps => m_AllowedFollowUps;

        static ReadOnlyCollection<GameplayAbilityDependency> FreezeDependencies(
            IEnumerable<GameplayAbilityDependency> dependencies)
        {
            var values = dependencies == null
                ? new List<GameplayAbilityDependency>()
                : new List<GameplayAbilityDependency>(dependencies);
            var seen = new HashSet<GameplayAbilityDependency>();
            for (int i = 0; i < values.Count; i++)
            {
                if (!values[i].IsValid || !seen.Add(values[i]))
                    throw new ArgumentException("Character skill subgraph dependencies are invalid or duplicated.", nameof(dependencies));
            }
            values.Sort((left, right) =>
            {
                int bySubgraph = string.CompareOrdinal(left.SubgraphIdentity, right.SubgraphIdentity);
                return bySubgraph != 0
                    ? bySubgraph
                    : string.CompareOrdinal(left.CallSiteIdentity, right.CallSiteIdentity);
            });
            return values.AsReadOnly();
        }

        static ReadOnlyCollection<CharacterSkillId> FreezeFollowUps(
            CharacterSkillId owner,
            IEnumerable<CharacterSkillId> allowedFollowUps)
        {
            var values = allowedFollowUps == null
                ? new List<CharacterSkillId>()
                : new List<CharacterSkillId>(allowedFollowUps);
            var seen = new HashSet<CharacterSkillId>();
            for (int i = 0; i < values.Count; i++)
            {
                if (!values[i].IsValid || values[i] == owner || !seen.Add(values[i]))
                    throw new ArgumentException("Character skill follow-up candidates are invalid, recursive, or duplicated.", nameof(allowedFollowUps));
            }
            values.Sort();
            return values.AsReadOnly();
        }
    }

    public sealed class GameplayAbilityProgramBinding
    {
        public GameplayAbilityProgramBinding(
            CharacterSkillId skillId,
            string admissionProfileId,
            string entryIdentity,
            OperationHandle entryOperation,
            string actionContextId,
            string sourceInputRequestId,
            bool consumeSourceInputRequest,
            string targetInputValueId,
            string targetKey,
            IEnumerable<GameplayAbilityDependency> dependencies,
            IEnumerable<CharacterSkillId> allowedFollowUps,
            IEnumerable<GameplayAbilityProgramEndRule> endRules = null)
        {
            if (!skillId.IsValid || !entryOperation.IsValid)
                throw new ArgumentException("Character SkillProgram binding is incomplete.");
            SkillId = skillId;
            AdmissionProfileId = SimulationIdentity.Require(admissionProfileId, nameof(admissionProfileId));
            EntryIdentity = SimulationIdentity.Require(entryIdentity, nameof(entryIdentity));
            EntryOperation = entryOperation;
            ActionContextId = SimulationIdentity.Require(actionContextId, nameof(actionContextId));
            SourceInputRequestId = sourceInputRequestId ?? string.Empty;
            ConsumeSourceInputRequest = consumeSourceInputRequest;
            TargetInputValueId = targetInputValueId ?? string.Empty;
            TargetKey = targetKey ?? string.Empty;
            m_Dependencies = FreezeDependencies(dependencies);
            m_AllowedFollowUps = FreezeFollowUps(skillId, allowedFollowUps);
            m_EndRules = FreezeEndRules(endRules);
        }

        readonly ReadOnlyCollection<GameplayAbilityDependency> m_Dependencies;
        readonly ReadOnlyCollection<CharacterSkillId> m_AllowedFollowUps;
        readonly ReadOnlyCollection<GameplayAbilityProgramEndRule> m_EndRules;

        public CharacterSkillId SkillId { get; }
        public string AdmissionProfileId { get; }
        public string EntryIdentity { get; }
        public OperationHandle EntryOperation { get; }
        public string ActionContextId { get; }
        public string SourceInputRequestId { get; }
        public bool ConsumeSourceInputRequest { get; }
        public string TargetInputValueId { get; }
        public string TargetKey { get; }
        public IReadOnlyList<GameplayAbilityDependency> Dependencies => m_Dependencies;
        public IReadOnlyList<CharacterSkillId> AllowedFollowUps => m_AllowedFollowUps;
        public IReadOnlyList<GameplayAbilityProgramEndRule> EndRules => m_EndRules;

        public bool TryGetEndRule(
            string trigger,
            string actionWindowType,
            out GameplayAbilityProgramEndRule rule)
        {
            string triggerName = SimulationIdentity.Require(trigger, nameof(trigger));
            string windowType = actionWindowType ?? string.Empty;
            for (int i = 0; i < m_EndRules.Count; i++)
            {
                GameplayAbilityProgramEndRule candidate = m_EndRules[i];
                if (string.Equals(candidate.Trigger, triggerName, StringComparison.Ordinal) &&
                    (triggerName != GameplayAbilityEndTriggerNames.ActionWindowClosed ||
                     string.Equals(candidate.ActionWindowType, windowType, StringComparison.Ordinal)))
                {
                    rule = candidate;
                    return true;
                }
            }
            rule = default;
            return false;
        }

        static ReadOnlyCollection<GameplayAbilityDependency> FreezeDependencies(
            IEnumerable<GameplayAbilityDependency> dependencies)
        {
            var values = dependencies == null
                ? new List<GameplayAbilityDependency>()
                : new List<GameplayAbilityDependency>(dependencies);
            var seen = new HashSet<GameplayAbilityDependency>();
            for (int i = 0; i < values.Count; i++)
            {
                if (!values[i].IsValid || !seen.Add(values[i]))
                    throw new ArgumentException("Character SkillProgram dependencies are invalid or duplicated.", nameof(dependencies));
            }
            values.Sort((left, right) =>
            {
                int bySubgraph = string.CompareOrdinal(left.SubgraphIdentity, right.SubgraphIdentity);
                return bySubgraph != 0
                    ? bySubgraph
                    : string.CompareOrdinal(left.CallSiteIdentity, right.CallSiteIdentity);
            });
            return values.AsReadOnly();
        }

        static ReadOnlyCollection<CharacterSkillId> FreezeFollowUps(
            CharacterSkillId owner,
            IEnumerable<CharacterSkillId> allowedFollowUps)
        {
            var values = allowedFollowUps == null
                ? new List<CharacterSkillId>()
                : new List<CharacterSkillId>(allowedFollowUps);
            var seen = new HashSet<CharacterSkillId>();
            for (int i = 0; i < values.Count; i++)
            {
                if (!values[i].IsValid || values[i] == owner || !seen.Add(values[i]))
                    throw new ArgumentException("Character SkillProgram follow-up candidates are invalid, recursive, or duplicated.", nameof(allowedFollowUps));
            }
            values.Sort();
            return values.AsReadOnly();
        }

        static ReadOnlyCollection<GameplayAbilityProgramEndRule> FreezeEndRules(
            IEnumerable<GameplayAbilityProgramEndRule> endRules)
        {
            var values = endRules == null
                ? new List<GameplayAbilityProgramEndRule>()
                : new List<GameplayAbilityProgramEndRule>(endRules);
            values.Sort((left, right) =>
            {
                int byTrigger = string.CompareOrdinal(left.Trigger, right.Trigger);
                return byTrigger != 0 ? byTrigger : left.Transition.CompareTo(right.Transition);
            });
            return values.AsReadOnly();
        }
    }

    public sealed class GameplayAbilityProgramCatalog
    {
        readonly ReadOnlyCollection<GameplayAbilityProgramBinding> m_Bindings;

        public GameplayAbilityProgramCatalog(
            IReadOnlyList<ProgramCatalogEntry> entries,
            IReadOnlyList<ProgramReference> references)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));
            if (references == null)
                throw new ArgumentNullException(nameof(references));
            var values = new List<GameplayAbilityProgramBinding>();
            for (int i = 0; i < entries.Count; i++)
            {
                ProgramCatalogEntry entry = entries[i];
                if (entry.Kind != ProgramCatalogEntryKind.AbilityProgram)
                    continue;
                OperationHandle operation = OperationHandle.Invalid;
                for (int referenceIndex = 0; referenceIndex < references.Count; referenceIndex++)
                {
                    ProgramReference reference = references[referenceIndex];
                    if (reference.Kind != ProgramReferenceKind.CatalogEntry ||
                        reference.TargetIndex != entry.Index || !reference.SourceOperation.IsValid)
                        continue;
                    if (operation.IsValid)
                        throw new InvalidDataException($"AbilityProgram '{entry.Identity}' has multiple entry operations.");
                    operation = reference.SourceOperation;
                }
                if (!operation.IsValid)
                    throw new InvalidDataException($"AbilityProgram '{entry.Identity}' has no entry operation reference.");
                string skillValue = RequirePrefix(entry.Identity, "ability:");
                string admissionProfileIdentity = RequireIdentity(entry, "AdmissionProfile", "action:");
                if (!ContainsEntry(entries, ProgramCatalogEntryKind.Action, $"action:{admissionProfileIdentity}"))
                    throw new InvalidDataException($"AbilityProgram '{entry.Identity}' references missing admission profile '{admissionProfileIdentity}'.");
                ReadRelations(
                    entry,
                    out List<GameplayAbilityDependency> dependencies,
                    out List<CharacterSkillId> allowedFollowUps,
                    out List<GameplayAbilityProgramEndRule> endRules);
                values.Add(new GameplayAbilityProgramBinding(
                    new CharacterSkillId(skillValue),
                    admissionProfileIdentity,
                    RequireIdentity(entry, "EntryIdentity", null),
                    operation,
                    RequireIdentity(entry, "ActionContext", null),
                    OptionalIdentity(entry, "SourceInputRequest", null),
                    OptionalBoolean(entry, "ConsumeSourceInputRequest", true),
                    OptionalIdentity(entry, "TargetInputValue", null),
                    OptionalIdentity(entry, "TargetKey", null),
                    dependencies,
                    allowedFollowUps,
                    endRules));
            }
            values.Sort((left, right) => left.SkillId.CompareTo(right.SkillId));
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i - 1].SkillId == values[i].SkillId)
                    throw new InvalidDataException($"AbilityProgram '{values[i].SkillId}' is duplicated.");
            }
            m_Bindings = values.AsReadOnly();
        }

        public IReadOnlyList<GameplayAbilityProgramBinding> Bindings => m_Bindings;

        public GameplayAbilityProgramBinding Require(CharacterSkillId skillId)
        {
            for (int i = 0; i < m_Bindings.Count; i++)
            {
                if (m_Bindings[i].SkillId == skillId)
                    return m_Bindings[i];
            }
            throw new InvalidOperationException($"AbilityProgram '{skillId}' is absent from the Program catalog.");
        }

        static string RequirePrefix(string value, string prefix)
        {
            if (value == null || !value.StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidDataException($"AbilityProgram identity '{value}' has no '{prefix}' prefix.");
            return SimulationIdentity.Require(value.Substring(prefix.Length), nameof(value));
        }

        static bool ContainsEntry(
            IReadOnlyList<ProgramCatalogEntry> entries,
            ProgramCatalogEntryKind kind,
            string identity)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Kind == kind && string.Equals(entries[i].Identity, identity, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        static string RequireIdentity(ProgramCatalogEntry entry, string name, string prefix)
        {
            string value = OptionalIdentity(entry, name, prefix);
            return value.Length == 0
                ? throw new InvalidDataException($"AbilityProgram '{entry.Identity}' has no '{name}' field.")
                : value;
        }

        static string OptionalIdentity(ProgramCatalogEntry entry, string name, string prefix)
        {
            for (int i = 0; i < entry.Fields.Count; i++)
            {
                ProgramCatalogField field = entry.Fields[i];
                if (!string.Equals(field.Name, name, StringComparison.Ordinal))
                    continue;
                if (field.Kind != ProgramCatalogFieldKind.Identity)
                    throw new InvalidDataException($"AbilityProgram '{entry.Identity}' field '{name}' is not an identity.");
                if (string.IsNullOrEmpty(field.Identity))
                    return string.Empty;
                if (prefix != null)
                    return RequirePrefix(field.Identity, prefix);
                return field.Identity;
            }
            return string.Empty;
        }

        static bool OptionalBoolean(ProgramCatalogEntry entry, string name, bool defaultValue)
        {
            for (int i = 0; i < entry.Fields.Count; i++)
            {
                ProgramCatalogField field = entry.Fields[i];
                if (!string.Equals(field.Name, name, StringComparison.Ordinal))
                    continue;
                if (field.Kind != ProgramCatalogFieldKind.Identity ||
                    !bool.TryParse(field.Identity, out bool value))
                    throw new InvalidDataException($"AbilityProgram '{entry.Identity}' field '{name}' is not a Boolean identity.");
                return value;
            }
            return defaultValue;
        }

        static void ReadRelations(
            ProgramCatalogEntry entry,
            out List<GameplayAbilityDependency> dependencies,
            out List<CharacterSkillId> allowedFollowUps,
            out List<GameplayAbilityProgramEndRule> endRules)
        {
            var subgraphs = new Dictionary<int, string>();
            var callSites = new Dictionary<int, string>();
            var followUps = new Dictionary<int, CharacterSkillId>();
            var endRuleValues = new Dictionary<int, GameplayAbilityProgramEndRule>();
            for (int i = 0; i < entry.Fields.Count; i++)
            {
                ProgramCatalogField field = entry.Fields[i];
                if (field.Name.StartsWith("Dependency:", StringComparison.Ordinal))
                {
                    if (field.Kind != ProgramCatalogFieldKind.Identity)
                        throw new InvalidDataException($"SkillProgram '{entry.Identity}' dependency field '{field.Name}' is not an identity.");
                    ReadDependencyField(entry, field, out int index, out bool subgraph);
                    var destination = subgraph ? subgraphs : callSites;
                    if (!destination.TryAdd(index, field.Identity))
                        throw new InvalidDataException($"SkillProgram '{entry.Identity}' dependency index '{index}' is duplicated.");
                    continue;
                }
                if (!field.Name.StartsWith("FollowUp:", StringComparison.Ordinal))
                {
                    if (field.Name.StartsWith("EndRule:", StringComparison.Ordinal))
                    {
                        ReadEndRuleField(entry, field, endRuleValues);
                    }
                    continue;
                }
                if (field.Kind != ProgramCatalogFieldKind.Identity)
                    throw new InvalidDataException($"SkillProgram '{entry.Identity}' follow-up field '{field.Name}' is not an identity.");
                int followUpIndex = ReadIndexedField(entry, field.Name, "FollowUp:");
                CharacterSkillId followUp = new CharacterSkillId(RequirePrefix(field.Identity, "ability:"));
                if (!followUps.TryAdd(followUpIndex, followUp))
                    throw new InvalidDataException($"SkillProgram '{entry.Identity}' follow-up index '{followUpIndex}' is duplicated.");
            }

            dependencies = new List<GameplayAbilityDependency>();
            var dependencyIndexes = new HashSet<int>(subgraphs.Keys);
            dependencyIndexes.UnionWith(callSites.Keys);
            foreach (int index in dependencyIndexes.OrderBy(value => value))
            {
                if (!subgraphs.TryGetValue(index, out string subgraph) || !callSites.TryGetValue(index, out string callSite))
                    throw new InvalidDataException($"SkillProgram '{entry.Identity}' dependency index '{index}' is incomplete.");
                dependencies.Add(new GameplayAbilityDependency(subgraph, callSite));
            }
            allowedFollowUps = followUps
                .OrderBy(value => value.Key)
                .Select(value => value.Value)
                .ToList();
            endRules = endRuleValues
                .OrderBy(value => value.Key)
                .Select(value => value.Value)
                .ToList();
        }

        static void ReadEndRuleField(
            ProgramCatalogEntry entry,
            ProgramCatalogField field,
            IDictionary<int, GameplayAbilityProgramEndRule> values)
        {
            if (field.Kind != ProgramCatalogFieldKind.Identity)
                throw new InvalidDataException($"AbilityProgram '{entry.Identity}' end rule field '{field.Name}' is not an identity.");
            string suffix = field.Name.Substring("EndRule:".Length);
            int separator = suffix.IndexOf(':');
            if (separator <= 0 || !int.TryParse(suffix.Substring(0, separator), out int index) || index < 0)
                throw new InvalidDataException($"AbilityProgram '{entry.Identity}' end rule field '{field.Name}' is malformed.");
            string component = suffix.Substring(separator + 1);
            if (!values.TryGetValue(index, out GameplayAbilityProgramEndRule current))
                current = new GameplayAbilityProgramEndRule("ExecutionCompleted", 2, string.Empty, string.Empty);
            string trigger = current.Trigger;
            int transition = current.Transition;
            string actionWindowType = current.ActionWindowType;
            string reason = current.Reason;
            switch (component)
            {
                case "Trigger":
                    trigger = field.Identity;
                    break;
                case "Transition":
                    if (!int.TryParse(field.Identity, out transition) || transition <= 0)
                        throw new InvalidDataException($"AbilityProgram '{entry.Identity}' end rule field '{field.Name}' has an invalid transition.");
                    break;
                case "Window":
                    actionWindowType = field.Identity;
                    break;
                case "Reason":
                    reason = field.Identity;
                    break;
                default:
                    throw new InvalidDataException($"AbilityProgram '{entry.Identity}' end rule field '{field.Name}' has an unknown component.");
            }
            values[index] = new GameplayAbilityProgramEndRule(trigger, transition, actionWindowType, reason);
        }

        static void ReadDependencyField(
            ProgramCatalogEntry entry,
            ProgramCatalogField field,
            out int index,
            out bool subgraph)
        {
            string suffix = field.Name.Substring("Dependency:".Length);
            int separator = suffix.IndexOf(':');
            if (separator <= 0)
                throw new InvalidDataException($"SkillProgram '{entry.Identity}' dependency field '{field.Name}' is malformed.");
            if (!int.TryParse(suffix.Substring(0, separator), out index) || index < 0)
                throw new InvalidDataException($"SkillProgram '{entry.Identity}' dependency field '{field.Name}' has an invalid index.");
            string component = suffix.Substring(separator + 1);
            if (string.Equals(component, "Subgraph", StringComparison.Ordinal))
                subgraph = true;
            else if (string.Equals(component, "CallSite", StringComparison.Ordinal))
                subgraph = false;
            else
                throw new InvalidDataException($"SkillProgram '{entry.Identity}' dependency field '{field.Name}' has an unknown component.");
        }

        static int ReadIndexedField(ProgramCatalogEntry entry, string fieldName, string prefix)
        {
            string value = fieldName.Substring(prefix.Length);
            if (!int.TryParse(value, out int index) || index < 0)
                throw new InvalidDataException($"SkillProgram '{entry.Identity}' field '{fieldName}' has an invalid index.");
            return index;
        }
    }

    public sealed class GameplayAbilityCatalog
    {
        readonly ReadOnlyCollection<GameplayAbilityProgramDefinition> m_Definitions;

        public GameplayAbilityCatalog(IEnumerable<GameplayAbilityProgramDefinition> definitions)
        {
            var values = definitions == null
                ? new List<GameplayAbilityProgramDefinition>()
                : new List<GameplayAbilityProgramDefinition>(definitions);
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null)
                    throw new ArgumentException("Character skill catalog contains a missing definition.", nameof(definitions));
            }
            values.Sort((left, right) => left.SkillId.CompareTo(right.SkillId));
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0 && values[i - 1].SkillId == values[i].SkillId)
                    throw new ArgumentException("Character skill catalog contains an invalid or duplicated definition.", nameof(definitions));
            }
            m_Definitions = values.AsReadOnly();
        }

        public IReadOnlyList<GameplayAbilityProgramDefinition> Definitions => m_Definitions;

        public GameplayAbilityProgramDefinition Require(CharacterSkillId skillId)
        {
            for (int i = 0; i < m_Definitions.Count; i++)
            {
                if (m_Definitions[i].SkillId == skillId)
                    return m_Definitions[i];
            }
            throw new InvalidOperationException($"Character skill '{skillId}' is absent from the catalog.");
        }

        public IReadOnlyList<GameplayAbilityProgramDefinition> FindByAdmissionProfile(string admissionProfileId)
        {
            string identity = SimulationIdentity.Require(admissionProfileId, nameof(admissionProfileId));
            var result = new List<GameplayAbilityProgramDefinition>();
            for (int i = 0; i < m_Definitions.Count; i++)
            {
                if (string.Equals(m_Definitions[i].AdmissionProfileId, identity, StringComparison.Ordinal))
                    result.Add(m_Definitions[i]);
            }
            return result.AsReadOnly();
        }
    }
}
