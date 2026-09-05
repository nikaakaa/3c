using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
            IEnumerable<CharacterSkillId> allowedFollowUps)
        {
            if (!skillId.IsValid)
                throw new ArgumentException("Character skill identity is invalid.", nameof(skillId));
            SkillId = skillId;
            ActionProfileId = SimulationIdentity.Require(actionProfileId, nameof(actionProfileId));
            EntryIdentity = SimulationIdentity.Require(entryIdentity, nameof(entryIdentity));
            EntrySignature = entrySignature ?? throw new ArgumentNullException(nameof(entrySignature));
            m_Dependencies = FreezeDependencies(dependencies);
            m_AllowedFollowUps = FreezeFollowUps(skillId, allowedFollowUps);
        }

        public CharacterSkillId SkillId { get; }
        public string ActionProfileId { get; }
        public string EntryIdentity { get; }
        public CharacterSkillEntrySignature EntrySignature { get; }
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

    public sealed class CharacterSkillCatalog
    {
        readonly ReadOnlyCollection<CharacterSkillDefinition> m_Definitions;

        public CharacterSkillCatalog(IEnumerable<CharacterSkillDefinition> definitions)
        {
            var values = definitions == null
                ? new List<CharacterSkillDefinition>()
                : new List<CharacterSkillDefinition>(definitions);
            values.Sort((left, right) => left.SkillId.CompareTo(right.SkillId));
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null || i > 0 && values[i - 1].SkillId == values[i].SkillId)
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
