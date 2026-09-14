using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class GameplayAbilityProviderOwnerSet
    {
        internal GameplayAbilityProviderOwnerSet(
            string controlModuleId,
            string inputProviderOwnerId,
            string gameplayProviderOwnerId)
        {
            ControlModuleId = controlModuleId ?? string.Empty;
            InputProviderOwnerId = inputProviderOwnerId ?? string.Empty;
            GameplayProviderOwnerId = gameplayProviderOwnerId ?? string.Empty;
        }

        public string ControlModuleId { get; }
        public string InputProviderOwnerId { get; }
        public string GameplayProviderOwnerId { get; }

        public static GameplayAbilityProviderOwnerSet Discover(
            BtsmtlSkillGraphOccurrence entry,
            CharacterSimulationCompileReport report)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            if (report == null)
                throw new ArgumentNullException(nameof(report));
            string controlModuleId = string.Empty;
            string inputProviderOwnerId = string.Empty;
            string gameplayProviderOwnerId = string.Empty;
            foreach (BtsmtlSkillGraphOccurrence occurrence in entry.EnumerateOccurrences())
                foreach (FlowNode node in occurrence.Nodes)
                {
                    if (node is BtsmtlSkillLocomotionFlowNode)
                    {
                        report.Error(
                            "ability_locomotion_node_forbidden",
                            $"{occurrence.Route}/node:{node.UID}",
                            "Ability图不能包含Locomotion节点。");
                        continue;
                    }
                    BtsmtlSkillProviderKind kind = BtsmtlSkillProviderContract.Resolve(node);
                    if (kind == BtsmtlSkillProviderKind.None)
                        continue;
                    string owner = BtsmtlSkillGraphAuthoringMetadata.ReadIdentity(node, "providerOwnerId");
                    if (string.IsNullOrWhiteSpace(owner))
                    {
                        report.Error(
                            BtsmtlSkillProviderContract.MissingCode(kind),
                            $"{occurrence.Route}/node:{node.UID}",
                            "Ability外部provider必须指定owner。");
                        continue;
                    }
                    switch (kind)
                    {
                        case BtsmtlSkillProviderKind.CharacterControlModule:
                            if (!CharacterStateProviderFields.IsOwner(owner))
                            {
                                report.Error(
                                    BtsmtlSkillProviderContract.InvalidCode(kind),
                                    $"{occurrence.Route}/node:{node.UID}",
                                    BtsmtlSkillProviderContract.InvalidMessage(kind));
                                continue;
                            }
                            Merge(
                                ref controlModuleId,
                                owner.Substring(CharacterStateProviderFields.OwnerPrefix.Length),
                                occurrence.Route,
                                node.UID,
                                "Ability引用了多个ControlModule。");
                            break;
                        case BtsmtlSkillProviderKind.InputProfile:
                            MergeAsset(
                                ref inputProviderOwnerId,
                                owner,
                                occurrence.Route,
                                node.UID,
                                kind,
                                report);
                            break;
                        case BtsmtlSkillProviderKind.GameplayEffectProfile:
                            MergeAsset(
                                ref gameplayProviderOwnerId,
                                owner,
                                occurrence.Route,
                                node.UID,
                                kind,
                                report);
                            break;
                        default:
                            report.Error(
                                BtsmtlSkillProviderContract.InvalidCode(kind),
                                $"{occurrence.Route}/node:{node.UID}",
                                "Ability provider类型未登记。");
                            break;
                    }
                }
            return new GameplayAbilityProviderOwnerSet(
                controlModuleId,
                inputProviderOwnerId,
                gameplayProviderOwnerId);
        }

        static void MergeAsset(
            ref string current,
            string value,
            string route,
            string nodeId,
            BtsmtlSkillProviderKind kind,
            CharacterSimulationCompileReport report)
        {
            if (!CharacterSkillProviderOwners.IsAssetOwner(value))
            {
                report.Error(
                    BtsmtlSkillProviderContract.InvalidCode(kind),
                    $"{route}/node:{nodeId}",
                    BtsmtlSkillProviderContract.InvalidMessage(kind));
                return;
            }
            Merge(ref current, value, route, nodeId, "Ability引用了多个同类型provider资产。");
        }

        static void Merge(
            ref string current,
            string value,
            string route,
            string nodeId,
            string message)
        {
            if (string.IsNullOrEmpty(current))
            {
                current = value;
                return;
            }
            if (!string.Equals(current, value, StringComparison.Ordinal))
                throw new InvalidOperationException($"{route}/node:{nodeId}: {message}");
        }
    }

    public sealed class GameplayAbilitySemanticDependencyCatalogEmitter
    {
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;
        readonly CharacterSimulationCatalogIndex m_Index;
        public GameplayAbilitySemanticDependencyCatalogEmitter(
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report,
            CharacterSimulationCatalogIndex index)
        {
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
            m_Index = index ?? throw new ArgumentNullException(nameof(index));
        }

        public GameplayAbilityProviderOwnerSet Emit(GameplayAbilityAuthoringCompilationModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            GameplayAbilityProviderOwnerSet owners = GameplayAbilityProviderOwnerSet.Discover(model.EntryGraph, m_Report);
            CharacterSimulationSourceLocation rootSource = Source(model, model.EntryGraph, "ability");
            DeclareAction(model.Definition.AdmissionProfile, rootSource);
            foreach (GameplayEffectDefinition effect in model.Definition.Effects)
                DeclareEffect(effect, owners.GameplayProviderOwnerId, rootSource);
            foreach (BtsmtlSkillGraphOccurrence occurrence in model.EntryGraph.EnumerateOccurrences())
                foreach (FlowNode node in occurrence.Nodes)
                    DeclareNodeDependencies(model, occurrence, node, owners);
            return owners;
        }

        void DeclareNodeDependencies(
            GameplayAbilityAuthoringCompilationModel model,
            BtsmtlSkillGraphOccurrence occurrence,
            FlowNode node,
            GameplayAbilityProviderOwnerSet owners)
        {
            CharacterSimulationSourceLocation source = Source(model, occurrence, $"node:{node.UID}", node);
            if (node is BtsmtlSkillActionRequestFlowNode request)
            {
                DeclareInput(
                    ProgramCatalogEntryKind.InputRequest,
                    request.InputId,
                    request.ProviderOwnerId,
                    source,
                    m_Index.InputRequests);
                return;
            }
            if (node is IBtsmtlSkillInputNode input)
            {
                DeclareInput(
                    ProgramCatalogEntryKind.InputValue,
                    input.InputId,
                    input.ProviderOwnerId,
                    source,
                    m_Index.InputValues);
                return;
            }
            if (node is BtsmtlSkillMoveFacingAngleFlowNode move)
            {
                DeclareIdentity(
                    ProgramCatalogEntryKind.CharacterState,
                    "move-facing-angle",
                    move.ProviderOwnerId,
                    source,
                    m_Index.CharacterStates,
                    "character-state");
                return;
            }
            if (node is IBtsmtlSkillCharacterStateNode state)
            {
                DeclareIdentity(
                    ProgramCatalogEntryKind.CharacterState,
                    state.FieldId,
                    state.ProviderOwnerId,
                    source,
                    m_Index.CharacterStates,
                    "character-state");
                return;
            }
            if (node is BtsmtlSkillCanActivateActionFlowNode admission)
            {
                object profile = BtsmtlSkillGraphAuthoringMetadata.ReadField(admission, "admissionProfile");
                DeclareActionIdentity(
                    BtsmtlSkillGraphAuthoringMetadata.ReadIdentity(admission, "admissionProfile"),
                    profile,
                    source);
                return;
            }
            if (node is BtsmtlSkillGameplayTagFlowNode tag)
            {
                DeclareTag(tag.Tag.Value, tag.ProviderOwnerId, source);
                return;
            }
            if (node is BtsmtlSkillGameplayTagQueryFlowNode query)
            {
                DeclareQuery(query.Query, query.ProviderOwnerId, source);
                return;
            }
            if (node is BtsmtlSkillGameplayAttributeFlowNode attribute)
            {
                DeclareIdentity(
                    ProgramCatalogEntryKind.Attribute,
                    attribute.Attribute.Value,
                    attribute.ProviderOwnerId,
                    source,
                    m_Index.Attributes,
                    "attribute");
                return;
            }
            if (node is BtsmtlSkillApplyGameplayEffectFlowNode apply)
            {
                DeclareEffectReference(model, apply.Effect, owners.GameplayProviderOwnerId, source);
                return;
            }
            if (node is BtsmtlSkillRemoveGameplayEffectFlowNode remove)
            {
                if (remove.Selector == ThirdPersonGameplay.Effects.GameplayEffectRemoveSelector.EffectId)
                    DeclareEffectReference(model, remove.Effect, owners.GameplayProviderOwnerId, source);
                else
                    DeclareQuery(remove.EffectTagQuery, remove.ProviderOwnerId, source);
            }
        }

        void DeclareAction(GameplayAbilityAdmissionProfile profile, CharacterSimulationSourceLocation source)
        {
            if (!profile || string.IsNullOrEmpty(profile.ActionId))
            {
                m_Report.Error("ability_admission_profile_invalid", source.Identity, "Ability admission profile无效。");
                return;
            }
            DeclareActionIdentity(profile.ActionId, profile, source);
        }

        void DeclareActionIdentity(string actionId, object profile, CharacterSimulationSourceLocation source)
        {
            if (string.IsNullOrEmpty(actionId))
            {
                m_Report.Error("ability_action_reference_invalid", source.Identity, "Ability引用的admission profile没有身份。");
                return;
            }
            m_Index.Actions.Add(actionId);
            var fields = new List<ProgramCatalogField>();
            if (profile is ThirdPersonGameplay.Contracts.IGameplayBehaviorProfile behavior)
            {
                fields.AddRange(CharacterSemanticBehaviorCatalogFields.Emit(behavior, m_Builder, source));
                if (profile is GameplayAbilityAdmissionProfile admission)
                {
                    fields.Add(m_Builder.ConstantField(source, "TargetRequirement", admission.TargetRequirement));
                    fields.Add(m_Builder.ConstantField(source, "MaxConcurrentInstances", admission.MaxConcurrentInstances));
                    AddQueryFields(fields, source, "Required", admission.RequiredTags);
                    AddQueryFields(fields, source, "Block", admission.BlockTags);
                    AddQueryFields(fields, source, "Cancel", admission.CancelTags);
                }
            }
            m_Builder.DeclareCatalogEntry(
                ProgramCatalogEntryKind.Action,
                $"action:{actionId}",
                4,
                fields.Where(value => value != null),
                source);
        }

        void AddQueryFields(
            List<ProgramCatalogField> fields,
            CharacterSimulationSourceLocation source,
            string prefix,
            GameplayTagQuery query)
        {
            if (query == null)
            {
                m_Report.Error("ability_action_tag_query_missing", source.Identity, $"Ability {prefix} Tag query为空。");
                return;
            }
            AddTags(fields, $"{prefix}:All", query.All);
            AddTags(fields, $"{prefix}:Any", query.Any);
            AddTags(fields, $"{prefix}:None", query.None);
        }

        void AddTags(List<ProgramCatalogField> fields, string prefix, IReadOnlyList<GameplayTagId> tags)
        {
            for (int i = 0; i < tags.Count; i++)
                fields.Add(m_Builder.IdentityField($"{prefix}:{i:D4}", $"tag:{tags[i].Value}"));
        }

        void DeclareEffect(
            GameplayEffectDefinition effect,
            string providerOwner,
            CharacterSimulationSourceLocation source)
        {
            if (!effect || !effect.EffectId.IsValid)
                return;
            SemanticDataDocument definition = CharacterSemanticGameplayEffectCatalogEmitter.EncodeDefinition(
                effect,
                source,
                m_Report);
            var fields = new List<ProgramCatalogField>
            {
                m_Builder.ConstantField(source, "Definition", definition)
            };
            DeclareIdentity(
                ProgramCatalogEntryKind.GameplayEffect,
                effect.EffectId.Value,
                providerOwner,
                source,
                m_Index.GameplayEffects,
                "effect",
                checked((int)effect.DefinitionRevision),
                fields);
        }

        void DeclareEffectReference(
            GameplayAbilityAuthoringCompilationModel model,
            GameplayEffectDefinition effect,
            string providerOwner,
            CharacterSimulationSourceLocation source)
        {
            if (!effect || !effect.EffectId.IsValid)
            {
                m_Report.Error("ability_effect_reference_invalid", source.Identity, "Ability Gameplay Effect引用无效。");
                return;
            }
            bool declared = model.Definition.Effects.Any(value => value && value.EffectId == effect.EffectId);
            if (!declared)
                m_Report.Error("ability_effect_not_declared", source.Identity, $"Gameplay Effect '{effect.EffectId.Value}'未登记在Ability定义中。");
            DeclareEffect(effect, providerOwner, source);
        }

        void DeclareInput(
            ProgramCatalogEntryKind kind,
            string identity,
            string providerOwner,
            CharacterSimulationSourceLocation source,
            HashSet<string> index)
        {
            DeclareIdentity(kind, identity, providerOwner, source, index, kind == ProgramCatalogEntryKind.InputRequest ? "input:request" : "input:value");
        }

        void DeclareTag(
            string identity,
            string providerOwner,
            CharacterSimulationSourceLocation source)
        {
            DeclareIdentity(ProgramCatalogEntryKind.GameplayTag, identity, providerOwner, source, m_Index.GameplayTags, "tag");
        }

        void DeclareQuery(
            GameplayTagQuery query,
            string providerOwner,
            CharacterSimulationSourceLocation source)
        {
            if (query == null)
            {
                m_Report.Error("ability_tag_query_missing", source.Identity, "Ability Gameplay Tag查询为空。");
                return;
            }
            DeclareQueryTags(query.All, providerOwner, source);
            DeclareQueryTags(query.Any, providerOwner, source);
            DeclareQueryTags(query.None, providerOwner, source);
        }

        void DeclareQueryTags(
            IReadOnlyList<GameplayTagId> tags,
            string providerOwner,
            CharacterSimulationSourceLocation source)
        {
            for (int i = 0; i < tags.Count; i++)
                DeclareTag(tags[i].Value, providerOwner, source);
        }

        void DeclareIdentity(
            ProgramCatalogEntryKind kind,
            string identity,
            string providerOwner,
            CharacterSimulationSourceLocation source,
            HashSet<string> index,
            string prefix,
            int revision = 1,
            IEnumerable<ProgramCatalogField> extraFields = null)
        {
            if (string.IsNullOrWhiteSpace(identity))
            {
                m_Report.Error("ability_dependency_identity_invalid", source.Identity, $"Ability {prefix}引用没有身份。");
                return;
            }
            string normalized = identity.Trim();
            index.Add(normalized);
            var fields = new List<ProgramCatalogField>(extraFields ?? Array.Empty<ProgramCatalogField>());
            if (!string.IsNullOrWhiteSpace(providerOwner))
                fields.Add(m_Builder.IdentityField("ProviderOwner", providerOwner));
            m_Builder.DeclareCatalogEntry(
                kind,
                $"{prefix}:{normalized}",
                revision,
                fields,
                source);
        }

        static CharacterSimulationSourceLocation Source(
            GameplayAbilityAuthoringCompilationModel model,
            BtsmtlSkillGraphOccurrence occurrence,
            string suffix,
            FlowNode node = null) =>
            new(
                node?.GetType().FullName ?? typeof(GameplayAbilityDefinition).FullName,
                occurrence.GraphId,
                node?.UID ?? string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                $"{occurrence.Route}/{suffix}",
                contentHash: occurrence.ContentHash);
    }
}
