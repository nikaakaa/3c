using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using FlowCanvas;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class GameplayAbilityAuthoringCompilationModel
    {
        readonly ReadOnlyDictionary<string, GameplayAbilityAuthoringBlackboardDeclaration> m_Declarations;

        internal GameplayAbilityAuthoringCompilationModel(
            GameplayAbilityDefinition definition,
            string definitionPath,
            string definitionGuid,
            ProgramId programId,
            ProgramRevision sourceRevision,
            BtsmtlSkillGraphOccurrence entryGraph,
            IDictionary<string, GameplayAbilityAuthoringBlackboardDeclaration> declarations,
            TimelineSemanticEmitterRegistry timelineEmitters)
        {
            Definition = definition ? definition : throw new ArgumentNullException(nameof(definition));
            DefinitionPath = string.IsNullOrEmpty(definitionPath)
                ? throw new ArgumentException("Gameplay Ability资产路径不能为空。", nameof(definitionPath))
                : definitionPath;
            DefinitionGuid = string.IsNullOrEmpty(definitionGuid)
                ? throw new ArgumentException("Gameplay Ability资产GUID不能为空。", nameof(definitionGuid))
                : definitionGuid;
            ProgramId = programId;
            SourceRevision = sourceRevision;
            EntryGraph = entryGraph ?? throw new ArgumentNullException(nameof(entryGraph));
            m_Declarations = new ReadOnlyDictionary<string, GameplayAbilityAuthoringBlackboardDeclaration>(
                new SortedDictionary<string, GameplayAbilityAuthoringBlackboardDeclaration>(
                    declarations ?? throw new ArgumentNullException(nameof(declarations)),
                    StringComparer.Ordinal));
            TimelineEmitters = timelineEmitters ?? throw new ArgumentNullException(nameof(timelineEmitters));
            AbilityId = new CharacterSkillId(definition.AbilityId);
        }

        public GameplayAbilityDefinition Definition { get; }
        public string DefinitionPath { get; }
        public string DefinitionGuid { get; }
        public ProgramId ProgramId { get; }
        public ProgramRevision SourceRevision { get; }
        public CharacterSkillId AbilityId { get; }
        public int TickRate => GameplayTickSettings.DefaultLocalLogicTickRate;
        public string EntryIdentity => $"ability:{AbilityId.Value}";
        public BtsmtlSkillGraphOccurrence EntryGraph { get; }
        public IReadOnlyDictionary<string, GameplayAbilityAuthoringBlackboardDeclaration> Declarations => m_Declarations;
        public TimelineSemanticEmitterRegistry TimelineEmitters { get; }
    }

    public static class GameplayAbilityAuthoringDiscovery
    {
        public static GameplayAbilityAuthoringCompilationModel Discover(
            GameplayAbilityDefinition definition,
            CharacterSimulationCompileReport report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));
            return Discover(definition, TimelineSemanticEmitterRegistry.CreateDefault(), report);
        }

        public static GameplayAbilityAuthoringCompilationModel Discover(
            GameplayAbilityDefinition definition,
            TimelineSemanticEmitterRegistry timelineEmitters,
            CharacterSimulationCompileReport report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));
            if (!definition)
            {
                report.DiscoveryError("ability_definition_missing", "GameplayAbilityDefinition", "Gameplay Ability根资产缺失。");
                return null;
            }
            if (timelineEmitters == null)
                throw new ArgumentNullException(nameof(timelineEmitters));
            try
            {
                string path = AssetDatabase.GetAssetPath(definition);
                string guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(guid))
                {
                    report.DiscoveryError("ability_definition_not_persisted", definition.name, "Gameplay Ability必须是带GUID的持久化资产。");
                    return null;
                }
                if (!definition.CollectConfigurationErrors(null))
                {
                    report.DiscoveryError("ability_definition_invalid", path, "Gameplay Ability配置无效。");
                    return null;
                }
                BtsmtlSkillFlowGraph graph = definition.AbilityGraph;
                if (!graph || graph.Role != BtsmtlSkillFlowGraphRole.Skill)
                {
                    report.DiscoveryError("ability_graph_invalid", path, "Gameplay Ability必须拥有Skill角色的私有图。");
                    return null;
                }
                if (AssetDatabase.GetAssetPath(graph) != path || !AssetDatabase.IsSubAsset(graph))
                {
                    report.DiscoveryError("ability_graph_not_private", path, "Gameplay Ability图必须是根资产的私有子资产。");
                    return null;
                }
                ProgramRevision sourceRevision = GameplayAbilitySourceRevision.Compute(path);
                BtsmtlSkillGraphOccurrence entry = BtsmtlSkillGraphOccurrence.Read(
                    graph,
                    $"ability:{definition.AbilityId}/graph:{graph.AuthoringId}",
                    timelineEmitters,
                    report);
                if (!ValidateSubgraphDependencies(definition, entry, report))
                    return null;
                var declarations = new Dictionary<string, GameplayAbilityAuthoringBlackboardDeclaration>(StringComparer.Ordinal);
                foreach (BtsmtlSkillGraphOccurrence occurrence in entry.EnumerateOccurrences())
                    foreach (BtsmtlSkillBlackboardDeclaration declarationRecord in
                             ((IBtsmtlSkillFlowGraph)occurrence.Graph).BlackboardDeclarations)
                    {
                        var declaration = new GameplayAbilityAuthoringBlackboardDeclaration(
                            occurrence.Graph,
                            declarationRecord,
                            occurrence.Route,
                            occurrence.ContentHash);
                        string identity = DeclarationIdentity(occurrence.GraphId, declarationRecord.VariableId);
                        if (declarations.TryGetValue(identity, out GameplayAbilityAuthoringBlackboardDeclaration existing))
                        {
                            if (!ReferenceEquals(existing.AuthoringDeclaration, declaration.AuthoringDeclaration))
                                report.DiscoveryError("ability_blackboard_duplicate", occurrence.Route, $"Ability黑板声明'{identity}'指向不同变量。");
                        }
                        else
                            declarations.Add(identity, declaration);
                    }
                if (!report.IsValid)
                    return null;
                return new GameplayAbilityAuthoringCompilationModel(
                    definition,
                    path,
                    guid,
                    new ProgramId($"ability:{guid}"),
                    sourceRevision,
                    entry,
                    declarations,
                    timelineEmitters);
            }
            catch (Exception exception)
            {
                report.DiscoveryError("ability_authoring_discovery_failed", definition.name, exception.Message);
                return null;
            }
        }

        static bool ValidateSubgraphDependencies(
            GameplayAbilityDefinition definition,
            BtsmtlSkillGraphOccurrence entry,
            CharacterSimulationCompileReport report)
        {
            var calls = new Dictionary<string, BtsmtlSkillGraphReferenceOccurrence>(StringComparer.Ordinal);
            foreach (BtsmtlSkillGraphOccurrence occurrence in entry.EnumerateOccurrences())
                foreach (BtsmtlSkillGraphReferenceOccurrence reference in occurrence.References)
                    if (!calls.TryAdd(reference.CallSiteIdentity, reference))
                        report.DiscoveryError("ability_dependency_call_site_duplicate", reference.CallSiteIdentity, "Ability调用路径重复。");
            var dependencies = new HashSet<string>(StringComparer.Ordinal);
            foreach (GameplayAbilitySubgraphDependencyConfiguration dependency in definition.SubgraphDependencies)
            {
                if (dependency == null || string.IsNullOrWhiteSpace(dependency.SubgraphIdentity) ||
                    string.IsNullOrWhiteSpace(dependency.CallSiteIdentity))
                {
                    report.DiscoveryError("ability_dependency_invalid", entry.Route, "Ability子图依赖身份不完整。");
                    continue;
                }
                string key = dependency.SubgraphIdentity + "\u001f" + dependency.CallSiteIdentity;
                if (!dependencies.Add(key))
                    report.DiscoveryError("ability_dependency_duplicate", entry.Route, "Ability子图依赖重复。");
                if (!calls.TryGetValue(dependency.CallSiteIdentity, out BtsmtlSkillGraphReferenceOccurrence call))
                    report.DiscoveryError("ability_dependency_not_reachable", entry.Route, $"调用'{dependency.CallSiteIdentity}'不在Ability闭包中。");
                else if (!string.Equals(call.Child?.GraphId, dependency.SubgraphIdentity, StringComparison.Ordinal))
                    report.DiscoveryError("ability_dependency_subgraph_mismatch", dependency.CallSiteIdentity, "调用目标与Ability声明的子图身份不一致。");
            }
            return report.IsValid;
        }

        static string DeclarationIdentity(string graphId, string declarationId) =>
            $"blackboard:{graphId}:{declarationId}";
    }

    public static class GameplayAbilitySourceRevision
    {
        public const string Version = "gameplay-ability-source/1";

        public static ProgramRevision Compute(GameplayAbilityDefinition definition)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            string path = AssetDatabase.GetAssetPath(definition);
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("Gameplay Ability必须是持久化资产。");
            return Compute(path);
        }

        internal static ProgramRevision Compute(string definitionPath)
        {
            string[] dependencies = AssetDatabase.GetDependencies(definitionPath, true)
                .Append(definitionPath)
                .Distinct(StringComparer.Ordinal)
                .Where(IsSourceDependency)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            using var writer = new CanonicalWriter();
            writer.WriteString(Version);
            writer.WriteInt32(dependencies.Length);
            for (int i = 0; i < dependencies.Length; i++)
            {
                string path = dependencies[i].Replace('\\', '/');
                string absolute = Path.GetFullPath(path);
                if (!File.Exists(absolute))
                    throw new FileNotFoundException($"Ability依赖'{path}'不存在。", absolute);
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid))
                    throw new InvalidOperationException($"Ability依赖'{path}'没有GUID。");
                writer.WriteString(path);
                writer.WriteString(guid);
                writer.WriteBytes(File.ReadAllBytes(absolute));
            }
            return new ProgramRevision(writer.ComputeHash().Value);
        }

        static bool IsSourceDependency(string path)
        {
            string extension = Path.GetExtension(path);
            return string.Equals(extension, ".asset", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".inputactions", StringComparison.OrdinalIgnoreCase);
        }
    }
}
