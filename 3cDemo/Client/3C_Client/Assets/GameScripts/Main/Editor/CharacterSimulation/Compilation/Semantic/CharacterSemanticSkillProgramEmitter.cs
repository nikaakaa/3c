using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterSemanticSkillProgramEmitter
    {
        readonly IReadOnlyList<CharacterSkillCompilationRecord> m_SkillRecords;
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;

        public CharacterSemanticSkillProgramEmitter(
            IReadOnlyList<CharacterSkillCompilationRecord> skillRecords,
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report)
        {
            m_SkillRecords = skillRecords ?? throw new ArgumentNullException(nameof(skillRecords));
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public void DeclareExecutionState()
        {
            m_Builder.DeclareStandaloneStateSlot(
                new CharacterSimulationSourceLocation(
                    typeof(CharacterSkillProgramBinding).FullName,
                    "SkillExecutionState",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    "character/skill-execution-state"),
                ProgramStateValueKind.SkillExecutionState,
                ProgramStateOwnerKind.Action,
                ProgramStateSemantic.SkillExecutionState,
                "action:skill-execution");
        }

        public bool Emit(
            CharacterControlModuleContract contract,
            Func<CharacterSkillCompilationRecord, OperationHandle> compileEntry)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            if (compileEntry == null)
                throw new ArgumentNullException(nameof(compileEntry));
            var emittedSkills = new HashSet<CharacterSkillId>();
            for (int skillIndex = 0; skillIndex < contract.Skills.Count; skillIndex++)
            {
                CharacterSkillId skillId = contract.Skills[skillIndex];
                CharacterSkillCompilationRecord record = m_SkillRecords
                    .SingleOrDefault(value => value.SkillId == skillId);
                if (record == null)
                {
                    m_Report.Error("control_skill_definition_missing", skillId.Value, $"Control module skill '{skillId}' has no formal Skill definition.");
                    continue;
                }
                if (!emittedSkills.Add(skillId))
                {
                    m_Report.Error("control_skill_duplicate", skillId.Value, $"Control module skill '{skillId}' is emitted more than once.");
                    continue;
                }
                OperationHandle entry = compileEntry(record);
                if (!entry.IsValid)
                    continue;
                CharacterSkillAuthoringDefinition definition = record.Definition;
                CharacterSimulationSourceLocation source = new CharacterSimulationSourceLocation(
                    typeof(CharacterSkillAuthoringDefinition).FullName,
                    record.EntryGraph.GraphId,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    $"{record.EntryGraph.Route}/skill:{skillId.Value}",
                    contentHash: record.EntryGraph.ContentHash);
                var dependencies = new List<CharacterSkillDependency>();
                for (int dependencyIndex = 0; dependencyIndex < definition.SubgraphDependencies.Count; dependencyIndex++)
                {
                    CharacterSkillSubgraphDependencyConfiguration configuration = definition.SubgraphDependencies[dependencyIndex];
                    if (configuration == null)
                        continue;
                    try
                    {
                        dependencies.Add(new CharacterSkillDependency(
                            configuration.SubgraphIdentity,
                            configuration.CallSiteIdentity));
                    }
                    catch (Exception exception)
                    {
                        m_Report.Error("skill_dependency_invalid", source.Identity, exception.Message);
                    }
                }
                var allowedFollowUps = new List<CharacterSkillId>();
                for (int followUpIndex = 0; followUpIndex < definition.AllowedFollowUpSkillIds.Count; followUpIndex++)
                {
                    try
                    {
                        CharacterSkillId followUp = new CharacterSkillId(definition.AllowedFollowUpSkillIds[followUpIndex]);
                        if (!contract.Skills.Contains(followUp))
                        {
                            m_Report.Error(
                                "control_skill_follow_up_missing",
                                source.Identity,
                                $"Skill '{skillId}' references follow-up skill '{followUp}' that is not declared by control module '{contract.ModuleId}'.");
                            continue;
                        }
                        allowedFollowUps.Add(followUp);
                    }
                    catch (Exception exception)
                    {
                        m_Report.Error("skill_follow_up_invalid", source.Identity, exception.Message);
                    }
                }
                var fields = new List<ProgramCatalogField>
                {
                    m_Builder.IdentityField("ActionProfile", $"action:{definition.ActionProfile.ActionId}"),
                    m_Builder.IdentityField("EntryIdentity", definition.EntryGraphAuthoringId),
                    m_Builder.IdentityField("ActionContext", CharacterSimulationNodeEmitterContext.AssetIdentity(definition.ActionContext)),
                    m_Builder.IdentityField("SourceInputRequest", definition.SourceInputRequestId),
                    m_Builder.IdentityField("ConsumeSourceInputRequest", definition.ConsumeSourceInputRequest ? "true" : "false")
                };
                for (int dependencyIndex = 0; dependencyIndex < dependencies.Count; dependencyIndex++)
                {
                    CharacterSkillDependency dependency = dependencies[dependencyIndex];
                    fields.Add(m_Builder.IdentityField(
                        $"Dependency:{dependencyIndex:D4}:Subgraph",
                        dependency.SubgraphIdentity));
                    fields.Add(m_Builder.IdentityField(
                        $"Dependency:{dependencyIndex:D4}:CallSite",
                        dependency.CallSiteIdentity));
                }
                allowedFollowUps.Sort();
                for (int followUpIndex = 0; followUpIndex < allowedFollowUps.Count; followUpIndex++)
                    fields.Add(m_Builder.IdentityField($"FollowUp:{followUpIndex:D4}", $"skill:{allowedFollowUps[followUpIndex].Value}"));
                if (!string.IsNullOrEmpty(definition.TargetInputValueId))
                    fields.Add(m_Builder.IdentityField("TargetInputValue", definition.TargetInputValueId));
                if (!string.IsNullOrEmpty(definition.TargetKey))
                    fields.Add(m_Builder.IdentityField("TargetKey", definition.TargetKey));
                int catalog = m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.SkillProgram,
                    $"skill:{skillId.Value}",
                    1,
                    fields.Where(value => value != null).ToArray(),
                    source);
                if (catalog >= 0)
                {
                    m_Builder.DeclareReference(
                        $"skill:{skillId.Value}/entry",
                        entry,
                        ProgramReferenceKind.CatalogEntry,
                        catalog,
                        $"skill:{skillId.Value}",
                        source);
                }
            }
            return emittedSkills.Count == contract.Skills.Count;
        }
    }
}
