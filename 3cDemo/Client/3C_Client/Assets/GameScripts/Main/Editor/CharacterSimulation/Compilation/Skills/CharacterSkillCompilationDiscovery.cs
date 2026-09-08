using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class CharacterSkillCompilationRecord
    {
        internal CharacterSkillCompilationRecord(CharacterSkillAuthoringDefinition definition, BtsmtlSkillGraphOccurrence entryGraph)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            EntryGraph = entryGraph ?? throw new ArgumentNullException(nameof(entryGraph));
            SkillId = new CharacterSkillId(definition.SkillId);
        }

        public CharacterSkillAuthoringDefinition Definition { get; }
        public BtsmtlSkillGraphOccurrence EntryGraph { get; }
        public CharacterSkillId SkillId { get; }
    }

    public static class CharacterSkillCompilationDiscovery
    {
        public static IReadOnlyList<CharacterSkillCompilationRecord> Discover(
            IReadOnlyList<CharacterSkillAuthoringDefinition> definitions,
            IReadOnlyList<BtsmtlSkillFlowGraph> roots,
            CharacterSimulationCompileReport report)
        {
            if (definitions == null || roots == null || report == null)
                throw new ArgumentNullException();
            var graphs = new Dictionary<string, BtsmtlSkillFlowGraph>(StringComparer.Ordinal);
            foreach (BtsmtlSkillFlowGraph graph in roots)
            {
                if (graph == null || graph.Role != BtsmtlSkillFlowGraphRole.Skill)
                {
                    report.DiscoveryError("skill_root_graph_invalid", "SkillGraphs", "技能根目录只能引用正式Skill页面。");
                    continue;
                }
                if (!graphs.TryAdd(graph.AuthoringId, graph))
                    report.DiscoveryError("skill_root_graph_duplicate", graph.AuthoringId, "技能根图身份重复。");
            }
            var declarations = new List<(CharacterSkillAuthoringDefinition Definition, CharacterSkillId Id)>();
            var knownSkills = new HashSet<CharacterSkillId>();
            foreach (CharacterSkillAuthoringDefinition definition in definitions)
            {
                try
                {
                    if (definition == null)
                        throw new ArgumentException("技能定义为空。");
                    var id = new CharacterSkillId(definition.SkillId);
                    if (!knownSkills.Add(id))
                        throw new ArgumentException($"技能'{id}'重复声明。");
                    declarations.Add((definition, id));
                }
                catch (ArgumentException error)
                {
                    report.DiscoveryError("skill_identity_invalid", definition?.SkillId ?? "SkillDefinitions", error.Message);
                }
            }
            var result = new List<CharacterSkillCompilationRecord>();
            foreach (var declaration in declarations)
            {
                CharacterSkillAuthoringDefinition definition = declaration.Definition;
                if (!graphs.TryGetValue(definition.EntryGraphAuthoringId, out BtsmtlSkillFlowGraph graph))
                {
                    report.DiscoveryError("skill_entry_graph_missing", definition.SkillId,
                        $"技能'{definition.SkillId}'入口'{definition.EntryGraphAuthoringId}'不在Definition的正式SkillGraphs中。");
                    continue;
                }
                try
                {
                    BtsmtlSkillGraphOccurrence entry = BtsmtlSkillGraphOccurrence.Read(graph,
                        $"skill:{definition.SkillId}/graph:{graph.AuthoringId}");
                    ValidateRelations(definition, declaration.Id, entry, knownSkills, report);
                    result.Add(new CharacterSkillCompilationRecord(definition, entry));
                }
                catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
                {
                    report.DiscoveryError("skill_graph_invalid", definition.SkillId, error.Message);
                }
            }
            result.Sort((left, right) => left.SkillId.CompareTo(right.SkillId));
            return new ReadOnlyCollection<CharacterSkillCompilationRecord>(result);
        }

        static void ValidateRelations(CharacterSkillAuthoringDefinition definition, CharacterSkillId skillId,
            BtsmtlSkillGraphOccurrence entry, ISet<CharacterSkillId> knownSkills, CharacterSimulationCompileReport report)
        {
            var calls = new Dictionary<string, BtsmtlSkillGraphReferenceOccurrence>(StringComparer.Ordinal);
            foreach (BtsmtlSkillGraphOccurrence graph in entry.EnumerateOccurrences())
                foreach (BtsmtlSkillGraphReferenceOccurrence reference in graph.References)
                    if (!calls.TryAdd(reference.CallSiteIdentity, reference))
                        report.DiscoveryError("skill_dependency_call_site_duplicate", reference.CallSiteIdentity, "技能调用路径重复。");
            var dependencies = new HashSet<string>(StringComparer.Ordinal);
            foreach (CharacterSkillSubgraphDependencyConfiguration dependency in definition.SubgraphDependencies)
            {
                if (dependency == null || string.IsNullOrWhiteSpace(dependency.SubgraphIdentity) || string.IsNullOrWhiteSpace(dependency.CallSiteIdentity))
                {
                    report.DiscoveryError("skill_dependency_invalid", entry.Route, "技能子图依赖身份不完整。");
                    continue;
                }
                if (!dependencies.Add(dependency.SubgraphIdentity + "\u001f" + dependency.CallSiteIdentity))
                    report.DiscoveryError("skill_dependency_duplicate", entry.Route, "技能子图依赖重复。");
                if (!calls.TryGetValue(dependency.CallSiteIdentity, out BtsmtlSkillGraphReferenceOccurrence call))
                    report.DiscoveryError("skill_dependency_not_reachable", entry.Route, $"调用'{dependency.CallSiteIdentity}'不在当前技能闭包中。");
                else if (!string.Equals(call.Child.GraphId, dependency.SubgraphIdentity, StringComparison.Ordinal))
                    report.DiscoveryError("skill_dependency_subgraph_mismatch", dependency.CallSiteIdentity, "调用目标与声明的子图身份不一致。");
            }
            var followUps = new HashSet<CharacterSkillId>();
            foreach (string value in definition.AllowedFollowUpSkillIds)
            {
                try
                {
                    var followUp = new CharacterSkillId(value);
                    if (!followUps.Add(followUp))
                        report.DiscoveryError("skill_follow_up_duplicate", entry.Route, $"后续技能'{followUp}'重复。");
                    if (followUp == skillId)
                        report.DiscoveryError("skill_follow_up_recursive", entry.Route, "技能不能把自身登记为后续技能。");
                    else if (!knownSkills.Contains(followUp))
                        report.DiscoveryError("skill_follow_up_missing", entry.Route, $"后续技能'{followUp}'未声明。");
                }
                catch (ArgumentException error)
                {
                    report.DiscoveryError("skill_follow_up_invalid", entry.Route, error.Message);
                }
            }
        }
    }
}
