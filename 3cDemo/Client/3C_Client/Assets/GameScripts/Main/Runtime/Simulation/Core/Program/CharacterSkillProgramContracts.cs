using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace ThirdPersonSimulation
{
    public readonly struct CharacterSkillDependency : IEquatable<CharacterSkillDependency>
    {
        public CharacterSkillDependency(string subgraphIdentity, string callSiteIdentity)
        {
            SubgraphIdentity = SimulationIdentity.Require(subgraphIdentity, nameof(subgraphIdentity));
            CallSiteIdentity = SimulationIdentity.Require(callSiteIdentity, nameof(callSiteIdentity));
        }

        public string SubgraphIdentity { get; }
        public string CallSiteIdentity { get; }
        public bool IsValid => !string.IsNullOrEmpty(SubgraphIdentity) && !string.IsNullOrEmpty(CallSiteIdentity);
        public bool Equals(CharacterSkillDependency other) =>
            string.Equals(SubgraphIdentity, other.SubgraphIdentity, StringComparison.Ordinal) &&
            string.Equals(CallSiteIdentity, other.CallSiteIdentity, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CharacterSkillDependency other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(SubgraphIdentity, CallSiteIdentity);
    }

    public sealed class CharacterSkillEntrySignature
    {
        readonly ReadOnlyCollection<CharacterControlParameterDescriptor> m_Parameters;

        public CharacterSkillEntrySignature(IEnumerable<CharacterControlParameterDescriptor> parameters)
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

    public sealed class CharacterSkillDefinition
    {
        readonly ReadOnlyCollection<CharacterSkillDependency> m_Dependencies;
        readonly ReadOnlyCollection<CharacterSkillId> m_AllowedFollowUps;

        public CharacterSkillDefinition(
            CharacterSkillId skillId,
            string actionProfileId,
            string entryIdentity,
            CharacterSkillEntrySignature entrySignature,
            IEnumerable<CharacterSkillDependency> dependencies,
            IEnumerable<CharacterSkillId> allowedFollowUps,
            string sourceInputRequestId = "",
            bool consumeSourceInputRequest = true,
            string targetInputValueId = "",
            string targetKey = "")
        {
            if (!skillId.IsValid)
                throw new ArgumentException("Character skill identity is invalid.", nameof(skillId));
            SkillId = skillId;
            ActionProfileId = SimulationIdentity.Require(actionProfileId, nameof(actionProfileId));
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
        public string ActionProfileId { get; }
        public string EntryIdentity { get; }
        public CharacterSkillEntrySignature EntrySignature { get; }
        public string SourceInputRequestId { get; }
        public bool ConsumeSourceInputRequest { get; }
        public string TargetInputValueId { get; }
        public string TargetKey { get; }
        public IReadOnlyList<CharacterSkillDependency> Dependencies => m_Dependencies;
        public IReadOnlyList<CharacterSkillId> AllowedFollowUps => m_AllowedFollowUps;

        static ReadOnlyCollection<CharacterSkillDependency> FreezeDependencies(
            IEnumerable<CharacterSkillDependency> dependencies)
        {
            var values = dependencies == null
                ? new List<CharacterSkillDependency>()
                : new List<CharacterSkillDependency>(dependencies);
            var seen = new HashSet<CharacterSkillDependency>();
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

    public sealed class CharacterSkillProgramBinding
    {
        public CharacterSkillProgramBinding(
            CharacterSkillId skillId,
            string actionProfileId,
            string entryIdentity,
            OperationHandle entryOperation,
            string actionContextId,
            string sourceInputRequestId,
            bool consumeSourceInputRequest,
            string targetInputValueId,
            string targetKey,
            IEnumerable<CharacterSkillDependency> dependencies,
            IEnumerable<CharacterSkillId> allowedFollowUps)
        {
            if (!skillId.IsValid || !entryOperation.IsValid)
                throw new ArgumentException("Character SkillProgram binding is incomplete.");
            SkillId = skillId;
            ActionProfileId = SimulationIdentity.Require(actionProfileId, nameof(actionProfileId));
            EntryIdentity = SimulationIdentity.Require(entryIdentity, nameof(entryIdentity));
            EntryOperation = entryOperation;
            ActionContextId = SimulationIdentity.Require(actionContextId, nameof(actionContextId));
            SourceInputRequestId = sourceInputRequestId ?? string.Empty;
            ConsumeSourceInputRequest = consumeSourceInputRequest;
            TargetInputValueId = targetInputValueId ?? string.Empty;
            TargetKey = targetKey ?? string.Empty;
            m_Dependencies = FreezeDependencies(dependencies);
            m_AllowedFollowUps = FreezeFollowUps(skillId, allowedFollowUps);
        }

        readonly ReadOnlyCollection<CharacterSkillDependency> m_Dependencies;
        readonly ReadOnlyCollection<CharacterSkillId> m_AllowedFollowUps;

        public CharacterSkillId SkillId { get; }
        public string ActionProfileId { get; }
        public string EntryIdentity { get; }
        public OperationHandle EntryOperation { get; }
        public string ActionContextId { get; }
        public string SourceInputRequestId { get; }
        public bool ConsumeSourceInputRequest { get; }
        public string TargetInputValueId { get; }
        public string TargetKey { get; }
        public IReadOnlyList<CharacterSkillDependency> Dependencies => m_Dependencies;
        public IReadOnlyList<CharacterSkillId> AllowedFollowUps => m_AllowedFollowUps;

        static ReadOnlyCollection<CharacterSkillDependency> FreezeDependencies(
            IEnumerable<CharacterSkillDependency> dependencies)
        {
            var values = dependencies == null
                ? new List<CharacterSkillDependency>()
                : new List<CharacterSkillDependency>(dependencies);
            var seen = new HashSet<CharacterSkillDependency>();
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
    }

    public sealed class CharacterSkillProgramCatalog
    {
        readonly ReadOnlyCollection<CharacterSkillProgramBinding> m_Bindings;

        public CharacterSkillProgramCatalog(
            IReadOnlyList<ProgramCatalogEntry> entries,
            IReadOnlyList<ProgramReference> references)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));
            if (references == null)
                throw new ArgumentNullException(nameof(references));
            var values = new List<CharacterSkillProgramBinding>();
            for (int i = 0; i < entries.Count; i++)
            {
                ProgramCatalogEntry entry = entries[i];
                if (entry.Kind != ProgramCatalogEntryKind.SkillProgram)
                    continue;
                OperationHandle operation = OperationHandle.Invalid;
                for (int referenceIndex = 0; referenceIndex < references.Count; referenceIndex++)
                {
                    ProgramReference reference = references[referenceIndex];
                    if (reference.Kind != ProgramReferenceKind.CatalogEntry ||
                        reference.TargetIndex != entry.Index || !reference.SourceOperation.IsValid)
                        continue;
                    if (operation.IsValid)
                        throw new InvalidDataException($"SkillProgram '{entry.Identity}' has multiple entry operations.");
                    operation = reference.SourceOperation;
                }
                if (!operation.IsValid)
                    throw new InvalidDataException($"SkillProgram '{entry.Identity}' has no entry operation reference.");
                string skillValue = RequirePrefix(entry.Identity, "skill:");
                ReadRelations(entry, out List<CharacterSkillDependency> dependencies, out List<CharacterSkillId> allowedFollowUps);
                values.Add(new CharacterSkillProgramBinding(
                    new CharacterSkillId(skillValue),
                    RequireIdentity(entry, "ActionProfile", "action:"),
                    RequireIdentity(entry, "EntryIdentity", null),
                    operation,
                    RequireIdentity(entry, "ActionContext", null),
                    OptionalIdentity(entry, "SourceInputRequest", null),
                    OptionalBoolean(entry, "ConsumeSourceInputRequest", true),
                    OptionalIdentity(entry, "TargetInputValue", null),
                    OptionalIdentity(entry, "TargetKey", null),
                    dependencies,
                    allowedFollowUps));
            }
            values.Sort((left, right) => left.SkillId.CompareTo(right.SkillId));
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i - 1].SkillId == values[i].SkillId)
                    throw new InvalidDataException($"SkillProgram '{values[i].SkillId}' is duplicated.");
            }
            m_Bindings = values.AsReadOnly();
        }

        public IReadOnlyList<CharacterSkillProgramBinding> Bindings => m_Bindings;

        public CharacterSkillProgramBinding Require(CharacterSkillId skillId)
        {
            for (int i = 0; i < m_Bindings.Count; i++)
            {
                if (m_Bindings[i].SkillId == skillId)
                    return m_Bindings[i];
            }
            throw new InvalidOperationException($"SkillProgram '{skillId}' is absent from the Program catalog.");
        }

        static string RequirePrefix(string value, string prefix)
        {
            if (value == null || !value.StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidDataException($"SkillProgram identity '{value}' has no '{prefix}' prefix.");
            return SimulationIdentity.Require(value.Substring(prefix.Length), nameof(value));
        }

        static string RequireIdentity(ProgramCatalogEntry entry, string name, string prefix)
        {
            string value = OptionalIdentity(entry, name, prefix);
            return value.Length == 0
                ? throw new InvalidDataException($"SkillProgram '{entry.Identity}' has no '{name}' field.")
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
                    throw new InvalidDataException($"SkillProgram '{entry.Identity}' field '{name}' is not an identity.");
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
                    throw new InvalidDataException($"SkillProgram '{entry.Identity}' field '{name}' is not a Boolean identity.");
                return value;
            }
            return defaultValue;
        }

        static void ReadRelations(
            ProgramCatalogEntry entry,
            out List<CharacterSkillDependency> dependencies,
            out List<CharacterSkillId> allowedFollowUps)
        {
            var subgraphs = new Dictionary<int, string>();
            var callSites = new Dictionary<int, string>();
            var followUps = new Dictionary<int, CharacterSkillId>();
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
                    continue;
                if (field.Kind != ProgramCatalogFieldKind.Identity)
                    throw new InvalidDataException($"SkillProgram '{entry.Identity}' follow-up field '{field.Name}' is not an identity.");
                int followUpIndex = ReadIndexedField(entry, field.Name, "FollowUp:");
                CharacterSkillId followUp = new CharacterSkillId(RequirePrefix(field.Identity, "skill:"));
                if (!followUps.TryAdd(followUpIndex, followUp))
                    throw new InvalidDataException($"SkillProgram '{entry.Identity}' follow-up index '{followUpIndex}' is duplicated.");
            }

            dependencies = new List<CharacterSkillDependency>();
            var dependencyIndexes = new HashSet<int>(subgraphs.Keys);
            dependencyIndexes.UnionWith(callSites.Keys);
            foreach (int index in dependencyIndexes.OrderBy(value => value))
            {
                if (!subgraphs.TryGetValue(index, out string subgraph) || !callSites.TryGetValue(index, out string callSite))
                    throw new InvalidDataException($"SkillProgram '{entry.Identity}' dependency index '{index}' is incomplete.");
                dependencies.Add(new CharacterSkillDependency(subgraph, callSite));
            }
            allowedFollowUps = followUps
                .OrderBy(value => value.Key)
                .Select(value => value.Value)
                .ToList();
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

    public sealed class CharacterSkillCatalog
    {
        readonly ReadOnlyCollection<CharacterSkillDefinition> m_Definitions;

        public CharacterSkillCatalog(IEnumerable<CharacterSkillDefinition> definitions)
        {
            var values = definitions == null
                ? new List<CharacterSkillDefinition>()
                : new List<CharacterSkillDefinition>(definitions);
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

        public IReadOnlyList<CharacterSkillDefinition> Definitions => m_Definitions;

        public CharacterSkillDefinition Require(CharacterSkillId skillId)
        {
            for (int i = 0; i < m_Definitions.Count; i++)
            {
                if (m_Definitions[i].SkillId == skillId)
                    return m_Definitions[i];
            }
            throw new InvalidOperationException($"Character skill '{skillId}' is absent from the catalog.");
        }

        public IReadOnlyList<CharacterSkillDefinition> FindByActionProfile(string actionProfileId)
        {
            string identity = SimulationIdentity.Require(actionProfileId, nameof(actionProfileId));
            var result = new List<CharacterSkillDefinition>();
            for (int i = 0; i < m_Definitions.Count; i++)
            {
                if (string.Equals(m_Definitions[i].ActionProfileId, identity, StringComparison.Ordinal))
                    result.Add(m_Definitions[i]);
            }
            return result.AsReadOnly();
        }
    }
}
