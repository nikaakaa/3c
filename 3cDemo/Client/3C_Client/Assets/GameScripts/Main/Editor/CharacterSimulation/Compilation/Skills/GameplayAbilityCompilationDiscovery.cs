using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class GameplayAbilityCompilationRecord
    {
        internal GameplayAbilityCompilationRecord(AbilityGrant grant, BtsmtlSkillGraphOccurrence entryGraph)
        {
            Grant = grant ?? throw new ArgumentNullException(nameof(grant));
            Ability = grant.Ability ?? throw new ArgumentException("AbilityGrant has no Gameplay Ability.", nameof(grant));
            EntryGraph = entryGraph ?? throw new ArgumentNullException(nameof(entryGraph));
            AbilityId = new CharacterSkillId(Ability.AbilityId);
        }

        public GameplayAbilityDefinition Ability { get; }
        public AbilityGrant Grant { get; }
        public BtsmtlSkillGraphOccurrence EntryGraph { get; }
        public CharacterSkillId AbilityId { get; }
        public GameplayAbilityAdmissionProfile AdmissionProfile => Ability.AdmissionProfile;
        public string ActionContextIdentity => Ability.ExecutionContextId;
        public string SourceInputRequestId => Grant.SourceInputRequestId;
        public bool ConsumeSourceInputRequest => Grant.ConsumeSourceInputRequest;
        public string TargetInputValueId => Grant.TargetInputValueId;
        public string TargetKey => Grant.TargetKey;
        public IReadOnlyList<string> AllowedFollowUpIds => Ability.AllowedFollowUpAbilityIds;
    }

    public static class GameplayAbilityCompilationDiscovery
    {
        public static IReadOnlyList<GameplayAbilityCompilationRecord> DiscoverAbilities(
            IReadOnlyList<AbilityGrant> grants,
            TimelineSemanticEmitterRegistry timelineEmitters,
            SimulationCompileReport report)
        {
            if (grants == null || report == null)
                throw new ArgumentNullException();
            var result = new List<GameplayAbilityCompilationRecord>();
            var known = new HashSet<CharacterSkillId>();
            foreach (AbilityGrant grant in grants)
            {
                try
                {
                    if (grant == null || !grant.Ability)
                        throw new ArgumentException("AbilityGrant或Gameplay Ability为空。");
                    if (!grant.Ability.CollectConfigurationErrors(null))
                        throw new InvalidOperationException($"Gameplay Ability '{grant.AbilityId}'配置无效。");
                    CharacterSkillId abilityId = new CharacterSkillId(grant.AbilityId);
                    if (!known.Add(abilityId))
                        throw new ArgumentException($"Gameplay Ability '{abilityId}'重复授予。");
                    BtsmtlSkillFlowGraph graph = grant.Ability.AbilityGraph;
                    if (!graph || graph.Role != BtsmtlSkillFlowGraphRole.Skill)
                        throw new InvalidOperationException($"Gameplay Ability '{abilityId}'缺少正式AbilityGraph。");
                    string abilityPath = AssetDatabase.GetAssetPath(grant.Ability);
                    if (AssetDatabase.GetAssetPath(graph) != abilityPath || !AssetDatabase.IsSubAsset(graph))
                        throw new InvalidOperationException($"Gameplay Ability '{abilityId}'的AbilityGraph必须是Ability资产私有子资产。");
                    BtsmtlSkillGraphOccurrence entry = BtsmtlSkillGraphOccurrence.Read(
                        graph,
                        $"ability:{abilityId.Value}/graph:{graph.AuthoringId}",
                        timelineEmitters,
                        report);
                    ValidateRelations(grant.Ability, entry, report);
                    result.Add(new GameplayAbilityCompilationRecord(grant, entry));
                }
                catch (ArgumentException error)
                {
                    report.DiscoveryError("ability_identity_invalid", grant?.AbilityId ?? "AbilityGrants", error.Message);
                }
                catch (InvalidOperationException error)
                {
                    report.DiscoveryError("ability_graph_invalid", grant?.AbilityId ?? "AbilityGrants", error.Message);
                }
            }
            foreach (GameplayAbilityCompilationRecord record in result)
            {
                foreach (string followUpValue in record.AllowedFollowUpIds)
                {
                    try
                    {
                        CharacterSkillId followUp = new CharacterSkillId(followUpValue);
                        if (!known.Contains(followUp))
                            report.DiscoveryError("ability_follow_up_missing", record.AbilityId.Value,
                                $"Gameplay Ability '{record.AbilityId}'引用了未授予的后续Ability '{followUp}'.");
                        if (followUp == record.AbilityId)
                            report.DiscoveryError("ability_follow_up_recursive", record.AbilityId.Value,
                                "Gameplay Ability不能把自身登记为后续Ability。");
                    }
                    catch (ArgumentException error)
                    {
                        report.DiscoveryError("ability_follow_up_invalid", record.AbilityId.Value, error.Message);
                    }
                }
            }
            result.Sort((left, right) => left.AbilityId.CompareTo(right.AbilityId));
            return new ReadOnlyCollection<GameplayAbilityCompilationRecord>(result);
        }

        static void ValidateRelations(GameplayAbilityDefinition ability,
            BtsmtlSkillGraphOccurrence entry, SimulationCompileReport report)
        {
            var calls = new Dictionary<string, BtsmtlSkillGraphReferenceOccurrence>(StringComparer.Ordinal);
            foreach (BtsmtlSkillGraphOccurrence graph in entry.EnumerateOccurrences())
                foreach (BtsmtlSkillGraphReferenceOccurrence reference in graph.References)
                    if (!calls.TryAdd(reference.CallSiteIdentity, reference))
                        report.DiscoveryError("ability_dependency_call_site_duplicate", reference.CallSiteIdentity, "Gameplay Ability调用路径重复。");
            var dependencies = new HashSet<string>(StringComparer.Ordinal);
            foreach (GameplayAbilitySubgraphDependencyConfiguration dependency in ability.SubgraphDependencies)
            {
                if (dependency == null || string.IsNullOrWhiteSpace(dependency.SubgraphIdentity) || string.IsNullOrWhiteSpace(dependency.CallSiteIdentity))
                {
                    report.DiscoveryError("ability_dependency_invalid", entry.Route, "Gameplay Ability子图依赖身份不完整。");
                    continue;
                }
                if (!dependencies.Add(dependency.SubgraphIdentity + "\u001f" + dependency.CallSiteIdentity))
                    report.DiscoveryError("ability_dependency_duplicate", entry.Route, "Gameplay Ability子图依赖重复。");
                if (!calls.TryGetValue(dependency.CallSiteIdentity, out BtsmtlSkillGraphReferenceOccurrence call))
                    report.DiscoveryError("ability_dependency_not_reachable", entry.Route, $"调用'{dependency.CallSiteIdentity}'不在当前Ability闭包中。");
                else if (!string.Equals(call.Child.GraphId, dependency.SubgraphIdentity, StringComparison.Ordinal))
                    report.DiscoveryError("ability_dependency_subgraph_mismatch", dependency.CallSiteIdentity, "调用目标与Ability声明的子图身份不一致。");
            }
        }
    }
}
