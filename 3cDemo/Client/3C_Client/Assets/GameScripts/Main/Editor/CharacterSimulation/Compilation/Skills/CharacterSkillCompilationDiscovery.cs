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

            var callSites = new Dictionary<string, CharacterAuthoringGraphReferenceRecord>(StringComparer.Ordinal);
            foreach (List<CharacterAuthoringGraphOccurrence> matches in occurrences.Values)
            {
                for (int occurrenceIndex = 0; occurrenceIndex < matches.Count; occurrenceIndex++)
                {
                    CharacterAuthoringGraphOccurrence occurrence = matches[occurrenceIndex];
                    for (int referenceIndex = 0; referenceIndex < occurrence.GraphReferences.Count; referenceIndex++)
                    {
                        CharacterAuthoringGraphReferenceRecord reference = occurrence.GraphReferences[referenceIndex];
                        if (!callSites.TryAdd(reference.CallFrame.Identity, reference))
                        {
                            report.DiscoveryError(
                                "skill_dependency_call_site_duplicate",
                                reference.CallFrame.Identity,
                                $"Graph call site '{reference.CallFrame.Identity}' is not unique.");
                        }
                    }
                }
            }

            var result = new List<CharacterSkillCompilationRecord>();
            var declaredSkillIds = new HashSet<CharacterSkillId>();
            for (int definitionIndex = 0; definitionIndex < definitions.Count; definitionIndex++)
            {
                CharacterSkillAuthoringDefinition definition = definitions[definitionIndex];
                if (definition == null || string.IsNullOrEmpty(definition.SkillId))
                    continue;
                try
                {
                    declaredSkillIds.Add(new CharacterSkillId(definition.SkillId));
                }
                catch (ArgumentException)
                {
                }
            }
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
                ValidateRelations(definition, skillId, matches[0], callSites, declaredSkillIds, report);
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

        static void ValidateRelations(
            CharacterSkillAuthoringDefinition definition,
            CharacterSkillId skillId,
            CharacterAuthoringGraphOccurrence entry,
            IReadOnlyDictionary<string, CharacterAuthoringGraphReferenceRecord> callSites,
            ISet<CharacterSkillId> knownSkills,
            CharacterSimulationCompileReport report)
        {
            var reachableCallSites = new HashSet<string>(StringComparer.Ordinal);
            CollectCallSites(entry, reachableCallSites, new HashSet<string>(StringComparer.Ordinal));
            var dependencies = new HashSet<string>(StringComparer.Ordinal);
            for (int dependencyIndex = 0; dependencyIndex < definition.SubgraphDependencies.Count; dependencyIndex++)
            {
                CharacterSkillSubgraphDependencyConfiguration configuration = definition.SubgraphDependencies[dependencyIndex];
                if (configuration == null ||
                    string.IsNullOrWhiteSpace(configuration.SubgraphIdentity) ||
                    string.IsNullOrWhiteSpace(configuration.CallSiteIdentity))
                {
                    report.DiscoveryError(
                        "skill_dependency_invalid",
                        entry.Route,
                        $"Skill '{skillId}' contains an incomplete subgraph dependency.");
                    continue;
                }
                string dependencyKey = $"{configuration.SubgraphIdentity}\u001f{configuration.CallSiteIdentity}";
                if (!dependencies.Add(dependencyKey))
                {
                    report.DiscoveryError(
                        "skill_dependency_duplicate",
                        entry.Route,
                        $"Skill '{skillId}' duplicates subgraph dependency '{configuration.SubgraphIdentity}/{configuration.CallSiteIdentity}'.");
                    continue;
                }
                if (!callSites.TryGetValue(configuration.CallSiteIdentity, out CharacterAuthoringGraphReferenceRecord callSite))
                {
                    report.DiscoveryError(
                        "skill_dependency_call_site_missing",
                        entry.Route,
                        $"Skill '{skillId}' references missing graph call site '{configuration.CallSiteIdentity}'.");
                    continue;
                }
                if (!reachableCallSites.Contains(configuration.CallSiteIdentity))
                {
                    report.DiscoveryError(
                        "skill_dependency_not_reachable",
                        entry.Route,
                        $"Skill '{skillId}' graph call site '{configuration.CallSiteIdentity}' is outside its entry graph closure.");
                }
                if (!string.Equals(
                        callSite.Child.Graph.GraphAuthoringId,
                        configuration.SubgraphIdentity,
                        StringComparison.Ordinal))
                {
                    report.DiscoveryError(
                        "skill_dependency_subgraph_mismatch",
                        entry.Route,
                        $"Skill '{skillId}' call site '{configuration.CallSiteIdentity}' targets graph '{callSite.Child.Graph.GraphAuthoringId}', not '{configuration.SubgraphIdentity}'.");
                }
            }

            var followUps = new HashSet<CharacterSkillId>();
            for (int followUpIndex = 0; followUpIndex < definition.AllowedFollowUpSkillIds.Count; followUpIndex++)
            {
                string followUpValue = definition.AllowedFollowUpSkillIds[followUpIndex];
                if (string.IsNullOrWhiteSpace(followUpValue))
                    continue;
                CharacterSkillId followUp;
                try
                {
                    followUp = new CharacterSkillId(followUpValue);
                }
                catch (Exception exception)
                {
                    report.DiscoveryError("skill_follow_up_invalid", entry.Route, exception.Message);
                    continue;
                }
                if (!followUps.Add(followUp))
                {
                    report.DiscoveryError(
                        "skill_follow_up_duplicate",
                        entry.Route,
                        $"Skill '{skillId}' duplicates follow-up skill '{followUp}'.");
                    continue;
                }
                if (followUp == skillId)
                {
                    report.DiscoveryError(
                        "skill_follow_up_recursive",
                        entry.Route,
                        $"Skill '{skillId}' cannot follow itself.");
                    continue;
                }
                if (!knownSkills.Contains(followUp))
                {
                    report.DiscoveryError(
                        "skill_follow_up_missing",
                        entry.Route,
                        $"Skill '{skillId}' references missing follow-up skill '{followUp}'.");
                }
            }
        }

        static void CollectCallSites(
            CharacterAuthoringGraphOccurrence occurrence,
            HashSet<string> callSites,
            HashSet<string> visitedRoutes)
        {
            if (occurrence == null || !visitedRoutes.Add(occurrence.Route))
                return;
            for (int referenceIndex = 0; referenceIndex < occurrence.GraphReferences.Count; referenceIndex++)
            {
                CharacterAuthoringGraphReferenceRecord reference = occurrence.GraphReferences[referenceIndex];
                callSites.Add(reference.CallFrame.Identity);
                CollectCallSites(reference.Child, callSites, visitedRoutes);
            }
        }
    }
}
