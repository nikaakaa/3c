using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterSemanticAbilityProgramEmitter
    {
        readonly IReadOnlyList<GameplayAbilityCompilationRecord> m_AbilityRecords;
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;

        public CharacterSemanticAbilityProgramEmitter(
            IReadOnlyList<GameplayAbilityCompilationRecord> abilityRecords,
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report)
        {
            m_AbilityRecords = abilityRecords ?? throw new ArgumentNullException(nameof(abilityRecords));
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public bool Emit(
            CharacterControlModuleContract contract,
            Func<GameplayAbilityCompilationRecord, OperationHandle> compileEntry)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            if (compileEntry == null)
                throw new ArgumentNullException(nameof(compileEntry));
            var emittedAbilities = new HashSet<CharacterSkillId>();
            for (int abilityIndex = 0; abilityIndex < contract.Abilities.Count; abilityIndex++)
            {
                CharacterSkillId abilityId = contract.Abilities[abilityIndex];
                GameplayAbilityCompilationRecord record = m_AbilityRecords
                    .SingleOrDefault(value => value.AbilityId == abilityId);
                if (record == null)
                {
                    m_Report.Error("control_ability_definition_missing", abilityId.Value, $"Control module Ability '{abilityId}' has no formal Gameplay Ability definition.");
                    continue;
                }
                if (!emittedAbilities.Add(abilityId))
                {
                    m_Report.Error("control_ability_duplicate", abilityId.Value, $"Control module Ability '{abilityId}' is emitted more than once.");
                    continue;
                }
                OperationHandle entry = compileEntry(record);
                if (!entry.IsValid)
                    continue;
                if (record.AdmissionProfile == null)
                {
                    m_Report.Error("ability_admission_profile_missing", abilityId.Value,
                        $"Gameplay Ability '{abilityId}' has no admission profile.");
                    continue;
                }
                CharacterSimulationSourceLocation source = new CharacterSimulationSourceLocation(
                    typeof(AbilityGrant).FullName,
                    record.EntryGraph.GraphId,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    $"{record.EntryGraph.Route}/ability:{abilityId.Value}",
                    contentHash: record.EntryGraph.ContentHash);
                var dependencies = new List<GameplayAbilityDependency>();
                IReadOnlyList<GameplayAbilitySubgraphDependencyConfiguration> configuredDependencies =
                    record.Ability.SubgraphDependencies;
                for (int dependencyIndex = 0; dependencyIndex < configuredDependencies.Count; dependencyIndex++)
                {
                    GameplayAbilitySubgraphDependencyConfiguration configuration = configuredDependencies[dependencyIndex];
                    if (configuration == null)
                        continue;
                    try
                    {
                        dependencies.Add(new GameplayAbilityDependency(
                            configuration.SubgraphIdentity,
                            configuration.CallSiteIdentity));
                    }
                    catch (Exception exception)
                    {
                        m_Report.Error("ability_dependency_invalid", source.Identity, exception.Message);
                    }
                }
                var allowedFollowUps = new List<CharacterSkillId>();
                for (int followUpIndex = 0; followUpIndex < record.AllowedFollowUpIds.Count; followUpIndex++)
                {
                    try
                    {
                        CharacterSkillId followUp = new CharacterSkillId(record.AllowedFollowUpIds[followUpIndex]);
                        if (!contract.Abilities.Contains(followUp))
                        {
                            m_Report.Error(
                                "control_ability_follow_up_missing",
                                source.Identity,
                                $"Gameplay Ability '{abilityId}' references follow-up Ability '{followUp}' that is not declared by control module '{contract.ModuleId}'.");
                            continue;
                        }
                        allowedFollowUps.Add(followUp);
                    }
                    catch (Exception exception)
                    {
                        m_Report.Error("ability_follow_up_invalid", source.Identity, exception.Message);
                    }
                }
                var fields = new List<ProgramCatalogField>
                {
                    m_Builder.IdentityField("AdmissionProfile", $"action:{record.AdmissionProfile.ActionId}"),
                    m_Builder.IdentityField("EntryIdentity", record.EntryGraph.GraphId),
                    m_Builder.IdentityField("ActionContext", record.ActionContextIdentity),
                    m_Builder.IdentityField("SourceInputRequest", record.SourceInputRequestId),
                    m_Builder.IdentityField("ConsumeSourceInputRequest", record.ConsumeSourceInputRequest ? "true" : "false")
                };
                for (int dependencyIndex = 0; dependencyIndex < dependencies.Count; dependencyIndex++)
                {
                    GameplayAbilityDependency dependency = dependencies[dependencyIndex];
                    fields.Add(m_Builder.IdentityField(
                        $"Dependency:{dependencyIndex:D4}:Subgraph",
                        dependency.SubgraphIdentity));
                    fields.Add(m_Builder.IdentityField(
                        $"Dependency:{dependencyIndex:D4}:CallSite",
                        dependency.CallSiteIdentity));
                }
                allowedFollowUps.Sort();
                for (int followUpIndex = 0; followUpIndex < allowedFollowUps.Count; followUpIndex++)
                    fields.Add(m_Builder.IdentityField($"FollowUp:{followUpIndex:D4}", $"ability:{allowedFollowUps[followUpIndex].Value}"));
                if (record.Ability != null)
                {
                    for (int endRuleIndex = 0; endRuleIndex < record.Ability.EndRules.Count; endRuleIndex++)
                    {
                        GameplayAbilityEndRule rule = record.Ability.EndRules[endRuleIndex];
                        if (rule == null)
                            continue;
                        fields.Add(m_Builder.IdentityField(
                            $"EndRule:{endRuleIndex:D4}:Trigger",
                            rule.Trigger.ToString()));
                        fields.Add(m_Builder.IdentityField(
                            $"EndRule:{endRuleIndex:D4}:Transition",
                            ((int)rule.TransitionType).ToString()));
                        if (!string.IsNullOrEmpty(rule.ActionWindowType))
                            fields.Add(m_Builder.IdentityField(
                                $"EndRule:{endRuleIndex:D4}:Window",
                                rule.ActionWindowType));
                        if (!string.IsNullOrEmpty(rule.Reason))
                            fields.Add(m_Builder.IdentityField(
                                $"EndRule:{endRuleIndex:D4}:Reason",
                                rule.Reason));
                    }
                }
                if (!string.IsNullOrEmpty(record.TargetInputValueId))
                    fields.Add(m_Builder.IdentityField("TargetInputValue", record.TargetInputValueId));
                if (!string.IsNullOrEmpty(record.TargetKey))
                    fields.Add(m_Builder.IdentityField("TargetKey", record.TargetKey));
                int catalog = m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.AbilityProgram,
                    $"ability:{abilityId.Value}",
                    1,
                    fields.Where(value => value != null).ToArray(),
                    source);
                if (catalog >= 0)
                {
                    m_Builder.DeclareReference(
                        $"ability:{abilityId.Value}/entry",
                        entry,
                        ProgramReferenceKind.CatalogEntry,
                        catalog,
                        $"ability:{abilityId.Value}",
                        source);
                }
            }
            return emittedAbilities.Count == contract.Abilities.Count;
        }
    }
}
