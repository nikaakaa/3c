using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class CharacterSkillCompilationRecord
    {
        internal CharacterSkillCompilationRecord(
            CharacterSkillAuthoringDefinition definition,
            CharacterAuthoringGraphOccurrence entryGraph)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            EntryGraph = entryGraph ?? throw new ArgumentNullException(nameof(entryGraph));
            SkillId = new CharacterSkillId(definition.SkillId);
        }

        public CharacterSkillAuthoringDefinition Definition { get; }
        public CharacterAuthoringGraphOccurrence EntryGraph { get; }
        public CharacterSkillId SkillId { get; }
    }

    public static class CharacterSkillCompilationDiscovery
    {
        public static IReadOnlyList<CharacterSkillCompilationRecord> Discover(
            IReadOnlyList<CharacterSkillAuthoringDefinition> definitions,
            IReadOnlyList<CharacterCompositionRoot> roots,
            CharacterSimulationCompileReport report)
        {
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));
            if (roots == null)
                throw new ArgumentNullException(nameof(roots));
            if (report == null)
                throw new ArgumentNullException(nameof(report));
            var occurrences = new Dictionary<string, List<CharacterAuthoringGraphOccurrence>>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
                Collect(roots[rootIndex].Occurrence);

            var result = new List<CharacterSkillCompilationRecord>();
            var identities = new HashSet<CharacterSkillId>();
            for (int definitionIndex = 0; definitionIndex < definitions.Count; definitionIndex++)
            {
                CharacterSkillAuthoringDefinition definition = definitions[definitionIndex];
                if (definition == null || string.IsNullOrEmpty(definition.SkillId))
                    continue;
                CharacterSkillId skillId;
                try
                {
                    skillId = new CharacterSkillId(definition.SkillId);
                }
                catch (Exception exception)
                {
                    report.DiscoveryError("skill_identity_invalid", definition.SkillId, exception.Message);
                    continue;
                }
                if (!identities.Add(skillId))
                {
                    report.DiscoveryError("skill_identity_duplicate", definition.SkillId, $"Skill '{skillId}' is declared more than once.");
                    continue;
                }
                if (!occurrences.TryGetValue(definition.EntryGraphAuthoringId, out List<CharacterAuthoringGraphOccurrence> matches) || matches.Count != 1)
                {
                    report.DiscoveryError(
                        "skill_entry_graph_ambiguous",
                        definition.SkillId,
                        $"Skill '{skillId}' entry graph '{definition.EntryGraphAuthoringId}' resolves to {matches?.Count ?? 0} occurrences.");
                    continue;
                }
                result.Add(new CharacterSkillCompilationRecord(definition, matches[0]));
            }
            result.Sort((left, right) => left.SkillId.CompareTo(right.SkillId));
            return new ReadOnlyCollection<CharacterSkillCompilationRecord>(result);

            void Collect(CharacterAuthoringGraphOccurrence occurrence)
            {
                if (occurrence == null || !visited.Add(occurrence.Route))
                    return;
                if (!occurrences.TryGetValue(occurrence.Graph.GraphAuthoringId, out List<CharacterAuthoringGraphOccurrence> matches))
                {
                    matches = new List<CharacterAuthoringGraphOccurrence>();
                    occurrences.Add(occurrence.Graph.GraphAuthoringId, matches);
                }
                matches.Add(occurrence);
                for (int referenceIndex = 0; referenceIndex < occurrence.GraphReferences.Count; referenceIndex++)
                    Collect(occurrence.GraphReferences[referenceIndex].Child);
                for (int edgeIndex = 0; edgeIndex < occurrence.Edges.Count; edgeIndex++)
                    Collect(occurrence.Edges[edgeIndex].ConditionGraph);
                for (int edgeIndex = 0; edgeIndex < occurrence.PropertyEdges.Count; edgeIndex++)
                    Collect(occurrence.PropertyEdges[edgeIndex].ConditionGraph);
                for (int timelineIndex = 0; timelineIndex < occurrence.Timelines.Count; timelineIndex++)
                {
                    IReadOnlyList<CharacterAuthoringClipRecord> clips = occurrence.Timelines[timelineIndex].Tracks
                        .SelectMany(value => value.Clips)
                        .ToArray();
                    for (int clipIndex = 0; clipIndex < clips.Count; clipIndex++)
                        Collect(clips[clipIndex].TreeGraph);
                }
            }
        }
    }
}
