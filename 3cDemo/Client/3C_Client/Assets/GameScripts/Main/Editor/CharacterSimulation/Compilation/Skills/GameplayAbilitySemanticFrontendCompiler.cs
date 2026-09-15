using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class GameplayAbilitySemanticFrontendResult
    {
        public GameplayAbilitySemanticFrontendResult(
            ValidatedSemanticIrArtifact artifact,
            GameplayAbilityAuthoringCompilationModel compilationModel,
            CharacterSimulationCompileReport report)
        {
            Artifact = artifact;
            CompilationModel = compilationModel;
            Report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public ValidatedSemanticIrArtifact Artifact { get; }
        public GameplayAbilityAuthoringCompilationModel CompilationModel { get; }
        public CharacterSimulationCompileReport Report { get; }
        public bool IsValid => Artifact != null && CompilationModel != null && Report.IsValid;

        public static GameplayAbilitySemanticFrontendResult Failed(CharacterSimulationCompileReport report) =>
            new GameplayAbilitySemanticFrontendResult(null, null, report);
    }

    public static class GameplayAbilitySemanticFrontendCompiler
    {
        public const string CompilerVersion = "gameplay-ability-semantic-compiler/1";
        public static readonly OperationSetVersion OperationSetVersion = GameplayAbilityOperationSet.Version;

        public static GameplayAbilitySemanticFrontendResult Compile(GameplayAbilityDefinition definition)
        {
            var report = new CharacterSimulationCompileReport();
            GameplayAbilityAuthoringCompilationModel model = GameplayAbilityAuthoringDiscovery.Discover(definition, report);
            if (model == null || !report.IsValid)
                return GameplayAbilitySemanticFrontendResult.Failed(report);
            try
            {
                var root = new GameplayAbilityRootDescriptor(
                    SimulationRootKind.Ability,
                    model.DefinitionGuid,
                    model.EntryIdentity,
                    model.SourceRevision.Value);
                var builder = new GameplayAbilitySemanticBuilder(
                    model.ProgramId,
                    CompilerVersion,
                    OperationSetVersion,
                    model.TickRate,
                    model.SourceRevision,
                    report,
                    root);
                CharacterSimulationSourceLocation abilitySource = new(
                    typeof(GameplayAbilityDefinition).FullName,
                    model.DefinitionGuid,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    model.DefinitionPath,
                    contentHash: model.SourceRevision.Value);
                builder.RequireGameplayCapability("Action");
                builder.RequireGameplayCapability("RunnableTree");
                var catalogIndex = new GameplayAbilityCatalogIndex();
                var catalogEmitter = new GameplayAbilitySemanticDependencyCatalogEmitter(builder, report, catalogIndex);
                GameplayAbilityProviderOwnerSet providers = catalogEmitter.Emit(model);
                var blackboard = new GameplayAbilitySemanticBlackboardEmitter(model.Declarations, builder, report);
                blackboard.CompileDeclarations();
                if (model.Declarations.Count > 0)
                    builder.RequireGameplayCapability("PipelineBlackboard");
                var domainBindings = new CharacterSemanticDomainBindingEmitter(
                    catalogIndex,
                    builder,
                    report,
                    blackboard);
                var graphCompiler = new BtsmtlSkillGraphCompiler(
                    builder,
                    domainBindings.Bind,
                    model.TimelineEmitters,
                    blackboard,
                    providers.ControlModuleId,
                    providers.InputProviderOwnerId,
                    providers.GameplayProviderOwnerId);
                RequireGraphCapabilities(model, builder);
                BtsmtlSkillGraphCompilation graph = graphCompiler.Compile(model.EntryGraph, OperationHandle.Invalid);
                if (!graph.Entry.IsValid)
                    return GameplayAbilitySemanticFrontendResult.Failed(report);
                DeclareAbilityCatalog(model, graph.Entry, builder, report, abilitySource);
                builder.DeclareReference(
                    "ability:root-operation",
                    OperationHandle.Invalid,
                    ProgramReferenceKind.Operation,
                    graph.Entry.Value,
                    model.EntryIdentity,
                    abilitySource);
                blackboard.DeclareScopes();
                GameplayAbilitySemanticIr semanticIr = builder.Build();
                ValidatedSemanticIrArtifact artifact = ValidateArtifact(semanticIr, report, model.DefinitionPath);
                if (artifact == null || !report.IsValid)
                    return GameplayAbilitySemanticFrontendResult.Failed(report);
                return new GameplayAbilitySemanticFrontendResult(artifact, model, report);
            }
            catch (Exception exception)
            {
                report.EmissionError("ability_semantic_emission_failed", model.DefinitionPath, exception.Message);
                return GameplayAbilitySemanticFrontendResult.Failed(report);
            }
        }

        static void RequireGraphCapabilities(
            GameplayAbilityAuthoringCompilationModel model,
            GameplayAbilitySemanticBuilder builder)
        {
            bool requiresGameplayEffect = false;
            bool requiresEquipment = false;
            foreach (BtsmtlSkillGraphOccurrence occurrence in model.EntryGraph.EnumerateOccurrences())
            {
                if (occurrence.Role == BtsmtlSkillFlowGraphRole.StateMachine)
                    builder.RequireGameplayCapability("StateMachine");
                if (occurrence.Timelines.Count > 0)
                    builder.RequireGameplayCapability("Timeline");
                foreach (FlowNode node in occurrence.Nodes)
                {
                    if (node is BtsmtlSkillApplyGameplayEffectFlowNode ||
                        node is BtsmtlSkillRemoveGameplayEffectFlowNode)
                        requiresGameplayEffect = true;
                    if (node is ReadEquipmentIdentityNode ||
                        node is ReadEquipmentParameterNode ||
                        node is EquipmentChangeOperationNode)
                        requiresEquipment = true;
                }
            }
            if (requiresGameplayEffect)
                builder.RequireGameplayCapability("GameplayEffect");
            if (requiresEquipment)
                builder.RequireGameplayCapability("Equipment");
        }

        static void DeclareAbilityCatalog(
            GameplayAbilityAuthoringCompilationModel model,
            OperationHandle entry,
            GameplayAbilitySemanticBuilder builder,
            CharacterSimulationCompileReport report,
            CharacterSimulationSourceLocation source)
        {
            GameplayAbilityAdmissionProfile profile = model.Definition.AdmissionProfile;
            if (!profile || string.IsNullOrEmpty(profile.ActionId))
            {
                report.Error("ability_admission_profile_invalid", source.Identity, "Ability admission profile无效。");
                return;
            }
            var fields = new List<ProgramCatalogField>
            {
                builder.IdentityField("AdmissionProfile", $"action:{profile.ActionId}"),
                builder.IdentityField("EntryIdentity", model.EntryIdentity),
                builder.IdentityField("ActionContext", model.Definition.ExecutionContextId)
            };
            var dependencies = new List<GameplayAbilityDependency>();
            foreach (GameplayAbilitySubgraphDependencyConfiguration configuration in model.Definition.SubgraphDependencies)
            {
                if (configuration == null)
                    continue;
                dependencies.Add(new GameplayAbilityDependency(
                    configuration.SubgraphIdentity,
                    configuration.CallSiteIdentity));
            }
            dependencies.Sort((left, right) =>
            {
                int result = string.CompareOrdinal(left.SubgraphIdentity, right.SubgraphIdentity);
                return result != 0 ? result : string.CompareOrdinal(left.CallSiteIdentity, right.CallSiteIdentity);
            });
            for (int i = 0; i < dependencies.Count; i++)
            {
                fields.Add(builder.IdentityField($"Dependency:{i:D4}:Subgraph", dependencies[i].SubgraphIdentity));
                fields.Add(builder.IdentityField($"Dependency:{i:D4}:CallSite", dependencies[i].CallSiteIdentity));
            }
            var followUps = new List<CharacterSkillId>();
            foreach (string value in model.Definition.AllowedFollowUpAbilityIds)
                followUps.Add(new CharacterSkillId(value));
            followUps.Sort();
            for (int i = 0; i < followUps.Count; i++)
                fields.Add(builder.IdentityField($"FollowUp:{i:D4}", $"ability:{followUps[i].Value}"));
            for (int i = 0; i < model.Definition.EndRules.Count; i++)
            {
                GameplayAbilityEndRule rule = model.Definition.EndRules[i];
                if (rule == null)
                    continue;
                fields.Add(builder.IdentityField($"EndRule:{i:D4}:Trigger", rule.Trigger.ToString()));
                fields.Add(builder.IdentityField($"EndRule:{i:D4}:Transition", ((int)rule.TransitionType).ToString()));
                if (!string.IsNullOrEmpty(rule.ActionWindowType))
                    fields.Add(builder.IdentityField($"EndRule:{i:D4}:Window", rule.ActionWindowType));
                if (!string.IsNullOrEmpty(rule.Reason))
                    fields.Add(builder.IdentityField($"EndRule:{i:D4}:Reason", rule.Reason));
            }
            int catalog = builder.DeclareCatalogEntry(
                ProgramCatalogEntryKind.AbilityProgram,
                model.EntryIdentity,
                1,
                fields.Where(value => value != null),
                source);
            if (catalog < 0)
                return;
            builder.DeclareReference(
                $"{model.EntryIdentity}/entry",
                entry,
                ProgramReferenceKind.CatalogEntry,
                catalog,
                model.EntryIdentity,
                source);
        }

        static ValidatedSemanticIrArtifact ValidateArtifact(
            GameplayAbilitySemanticIr semanticIr,
            CharacterSimulationCompileReport report,
            string sourceIdentity)
        {
            if (semanticIr == null || !report.IsValid)
                return null;
            try
            {
                ValidatedSemanticIrArtifact artifact = GameplayAbilitySemanticIrCodec.CreateValidatedArtifact(semanticIr);
                return GameplayAbilitySemanticIrCodec.ReadValidatedArtifact(
                    artifact.ToArray(),
                    new SemanticIrLoadExpectation(
                        semanticIr.Manifest.ProgramId,
                        semanticIr.Manifest.CompilerVersion,
                        semanticIr.Manifest.OperationSetVersion,
                        semanticIr.Manifest.TickRate,
                        semanticIr.Manifest.SourceRevision,
                        semanticIr.SemanticHash,
                        semanticIr.Manifest.Root));
            }
            catch (Exception exception)
            {
                report.ArtifactError("ability_semantic_ir_validation_failed", sourceIdentity, exception.Message);
                return null;
            }
        }
    }
}
