using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Behavior;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.GameplayEffect;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Contracts;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class CharacterSimulationCatalogIndex
    {
        public HashSet<string> InputValues { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> InputRequests { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> Actions { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> Behaviors { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> GameplayTags { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> Attributes { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> GameplayEffects { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> EquipmentSlots { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> EquipmentRoutes { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> EquipmentFeatures { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> EquipmentItems { get; } = new HashSet<string>(StringComparer.Ordinal);
    }

    public sealed class CharacterSimulationCatalogCompiler
    {
        readonly CharacterAuthoringCompilationModel m_Model;
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;
        readonly string m_RootGraphId;
        readonly CharacterSimulationCatalogIndex m_Index = new CharacterSimulationCatalogIndex();

        public CharacterSimulationCatalogCompiler(
            CharacterAuthoringCompilationModel model,
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report)
        {
            m_Model = model ?? throw new ArgumentNullException(nameof(model));
            m_RootGraphId = model.Root.Graph.GraphAuthoringId;
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public CharacterSimulationCatalogIndex Compile()
        {
            CompileInput();
            CompileGameplayTagsAndAttributes();
            CompileActions();
            CompileBehaviors();
            CompileGameplayEffects();
            CompileEquipment();
            DeclareGlobalState();
            return m_Index;
        }

        void CompileInput()
        {
            CharacterInputProfile profile = m_Model.InputProfile;
            if (!profile)
            {
                m_Report.Error("input_profile_missing", DefinitionSource.Identity, "Character Input Profile is missing.");
                return;
            }

            foreach (CharacterInputValueDefinition value in m_Model.InputValues)
            {
                if (value == null || string.IsNullOrEmpty(value.InputValueId))
                    continue;
                m_Index.InputValues.Add(value.InputValueId);
                CharacterSimulationSourceLocation source = AssetSource(profile, $"input:value:{value.InputValueId}");
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.InputValue,
                    $"input:value:{value.InputValueId}",
                    2,
                    Fields(
                        m_Builder.ConstantField(source, "ValueType", MapInputValueKind(value.ValueType))),
                    source);
            }

            foreach (CharacterAuthoringBlackboardDeclaration item in m_Model.Declarations.Values)
            {
                BaseExposedProperty declaration = item.Declaration;
                if (declaration.BlackboardSyncPolicy != PipelineBlackboardVariableSyncPolicy.InputDerived ||
                    string.IsNullOrWhiteSpace(declaration.InputValueId))
                    continue;
                ProgramInputValueKind kind = MapInputValueKind(declaration.ValueType);
                if (!m_Index.InputValues.Add(declaration.InputValueId))
                {
                    m_Report.Error(
                        "input_value_identity_duplicate",
                        item.Route,
                        $"InputDerived declaration '{declaration.BlackboardKey}' duplicates input value '{declaration.InputValueId}'.");
                    continue;
                }
                CharacterSimulationSourceLocation source = AssetSource(
                    m_Model.Definition,
                    $"input:value:{declaration.InputValueId}/declaration:{declaration.DeclarationId}");
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.InputValue,
                    $"input:value:{declaration.InputValueId}",
                    2,
                    Fields(m_Builder.ConstantField(source, "ValueType", kind)),
                    source);
            }

            if (m_Model.Roots.Any(root => ContainsCameraBasisRead(root.Occurrence)))
            {
                DeclareRuntimeInput(CameraProgramOperationSchema.BasisValidInputId, ProgramInputValueKind.Boolean);
                DeclareRuntimeInput(CameraProgramOperationSchema.BasisPlanarForwardInputId, ProgramInputValueKind.Vector3);
                DeclareRuntimeInput(CameraProgramOperationSchema.BasisPlanarRightInputId, ProgramInputValueKind.Vector3);
                DeclareRuntimeInput(CameraProgramOperationSchema.BasisLookDirectionInputId, ProgramInputValueKind.Vector3);
                DeclareRuntimeInput(CameraProgramOperationSchema.BasisAimPointInputId, ProgramInputValueKind.Vector3);
                DeclareRuntimeInput(CameraProgramOperationSchema.BasisYawInputId, ProgramInputValueKind.Yaw);
                DeclareRuntimeInput(CameraProgramOperationSchema.BasisPitchInputId, ProgramInputValueKind.Scalar);
            }

            foreach (CharacterActionRequestDefinition request in m_Model.InputRequests)
            {
                if (request == null || string.IsNullOrEmpty(request.RequestId))
                    continue;
                m_Index.InputRequests.Add(request.RequestId);
                CharacterSimulationSourceLocation source = AssetSource(profile, $"input:request:{request.RequestId}");
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.InputRequest,
                    $"input:request:{request.RequestId}",
                    1,
                    Fields(
                        m_Builder.ConstantField(source, "BufferSeconds", request.BufferSeconds),
                        m_Builder.ConstantField(source, "Priority", request.Priority)),
                    source);
                m_Builder.DeclareStandaloneStateSlot(
                    source,
                    ProgramStateValueKind.InputRequest,
                    ProgramStateOwnerKind.Input,
                    ProgramStateSemantic.InputRequestBuffer,
                    $"input:request:{request.RequestId}");
            }
        }

        void DeclareRuntimeInput(string inputId, ProgramInputValueKind kind)
        {
            if (!m_Index.InputValues.Add(inputId))
                throw new InvalidOperationException($"Runtime input value '{inputId}' conflicts with an authored input declaration.");
            CharacterSimulationSourceLocation source = DefinitionSource;
            m_Builder.DeclareCatalogEntry(
                ProgramCatalogEntryKind.InputValue,
                $"input:value:{inputId}",
                2,
                Fields(m_Builder.ConstantField(source, "ValueType", kind)),
                source);
        }

        static bool ContainsCameraBasisRead(CharacterAuthoringGraphOccurrence occurrence)
        {
            for (int i = 0; i < occurrence.Nodes.Count; i++)
            {
                if (occurrence.Nodes[i] is ReadCameraBasisNode)
                    return true;
            }
            for (int i = 0; i < occurrence.GraphReferences.Count; i++)
            {
                if (ContainsCameraBasisRead(occurrence.GraphReferences[i].Child))
                    return true;
            }
            for (int i = 0; i < occurrence.Edges.Count; i++)
            {
                CharacterAuthoringGraphOccurrence condition = occurrence.Edges[i].ConditionGraph;
                if (condition != null && ContainsCameraBasisRead(condition))
                    return true;
            }
            return false;
        }

        static ProgramInputValueKind MapInputValueKind(CharacterInputValueType type)
        {
            return type switch
            {
                CharacterInputValueType.Bool => ProgramInputValueKind.Boolean,
                CharacterInputValueType.Float => ProgramInputValueKind.Scalar,
                CharacterInputValueType.Vector2 => ProgramInputValueKind.Vector2,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        static ProgramInputValueKind MapInputValueKind(Type type)
        {
            if (type == typeof(ActionTargetSnapshot))
                return ProgramInputValueKind.ActionTargetSnapshot;
            throw new InvalidOperationException($"InputDerived Blackboard type '{type?.FullName}' has no portable input kind.");
        }

        void CompileActions()
        {
            foreach (ActionProfile profile in m_Model.ActionProfiles)
            {
                if (!profile || string.IsNullOrEmpty(profile.ActionId))
                    continue;
                m_Index.Actions.Add(profile.ActionId);
                m_Index.Behaviors.Add(profile.BehaviorId);
                CharacterSimulationSourceLocation source = AssetSource(profile, $"action:{profile.ActionId}");
                var fields = BehaviorFields(profile, source).ToList();
                fields.Add(m_Builder.ConstantField(source, "TargetRequirement", profile.TargetRequirement));
                AddQueryFields(fields, source, "Required", profile.RequiredTags);
                AddQueryFields(fields, source, "Block", profile.BlockTags);
                AddQueryFields(fields, source, "Cancel", profile.CancelTags);
                m_Builder.DeclareCatalogEntry(ProgramCatalogEntryKind.Action, $"action:{profile.ActionId}", 3, Fields(fields), source);
                m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.ActionActivationRequest, ProgramStateOwnerKind.Action, ProgramStateSemantic.ActionRequestBuffer, $"action:{profile.ActionId}");
                m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.ActionInstance, ProgramStateOwnerKind.Action, ProgramStateSemantic.ActionInstance, $"action:{profile.ActionId}");
            }
        }

        void CompileBehaviors()
        {
            foreach (GameplayBehaviorProfile profile in m_Model.BehaviorProfiles)
            {
                if (!profile || string.IsNullOrEmpty(profile.BehaviorId))
                    continue;
                m_Index.Behaviors.Add(profile.BehaviorId);
                CharacterSimulationSourceLocation source = AssetSource(profile, $"behavior:{profile.BehaviorId}");
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.Behavior,
                    $"behavior:{profile.BehaviorId}",
                    1,
                    BehaviorFields(profile, source),
                    source);
            }
        }

        void CompileGameplayTagsAndAttributes()
        {
            CharacterGameplayEffectProfile profile = m_Model.GameplayEffectProfile;
            if (!profile)
            {
                m_Report.Error("gameplay_effect_profile_missing", DefinitionSource.Identity, "Character Gameplay Effect Profile is missing.");
                return;
            }

            var initialTags = new HashSet<GameplayTagId>(m_Model.InitialTags);
            if (profile.TagCatalog)
            {
                foreach (GameplayTagDefinition tag in m_Model.TagDefinitions)
                {
                    if (tag == null || !tag.TagId.IsValid)
                        continue;
                    m_Index.GameplayTags.Add(tag.TagId.Value);
                    CharacterSimulationSourceLocation source = AssetSource(profile.TagCatalog, $"tag:{tag.TagId.Value}");
                    m_Builder.DeclareCatalogEntry(
                        ProgramCatalogEntryKind.GameplayTag,
                        $"tag:{tag.TagId.Value}",
                        1,
                        Fields(
                            m_Builder.ConstantField(source, "DisplayName", tag.DisplayName),
                            m_Builder.ConstantField(source, "DebugCategory", tag.DebugCategory),
                            m_Builder.ConstantField(source, "Initial", initialTags.Contains(tag.TagId)),
                            m_Builder.IdentityField("Parent", tag.ParentTag.IsValid ? $"tag:{tag.ParentTag.Value}" : string.Empty)),
                        source);
                }
            }

            var initialAttributes = new Dictionary<GameplayAttributeId, float>();
            for (int i = 0; i < m_Model.InitialAttributes.Count; i++)
            {
                InitialGameplayAttributeValue initial = m_Model.InitialAttributes[i];
                if (initial?.Definition && initial.Definition.AttributeId.IsValid)
                    initialAttributes[initial.Definition.AttributeId] = initial.BaseValue;
            }
            foreach (GameplayAttributeDefinition attribute in m_Model.AttributeDefinitions)
            {
                if (!attribute || !attribute.AttributeId.IsValid)
                    continue;
                m_Index.Attributes.Add(attribute.AttributeId.Value);
                CharacterSimulationSourceLocation source = AssetSource(attribute, $"attribute:{attribute.AttributeId.Value}");
                var fields = new List<ProgramCatalogField>
                {
                    m_Builder.ConstantField(source, "DisplayName", attribute.DisplayName),
                    m_Builder.ConstantField(source, "DebugCategory", attribute.DebugCategory),
                    m_Builder.ConstantField(source, "InitialBase", initialAttributes.TryGetValue(attribute.AttributeId, out float initial) ? initial : 0f)
                };
                AddBoundFields(fields, source, "Minimum", attribute.Minimum);
                AddBoundFields(fields, source, "Maximum", attribute.Maximum);
                m_Builder.DeclareCatalogEntry(ProgramCatalogEntryKind.Attribute, $"attribute:{attribute.AttributeId.Value}", 1, Fields(fields), source);
            }
        }

        void CompileGameplayEffects()
        {
            CharacterGameplayEffectProfile profile = m_Model.GameplayEffectProfile;
            if (!profile)
                return;
            foreach (GameplayEffectDefinition effect in m_Model.EffectDefinitions)
            {
                if (!effect || !effect.EffectId.IsValid)
                    continue;
                m_Index.GameplayEffects.Add(effect.EffectId.Value);
                m_Index.Behaviors.Add(effect.BehaviorId);
                CharacterSimulationSourceLocation source = AssetSource(effect, $"effect:{effect.EffectId.Value}");
                SemanticDataDocument definition = EncodeEffect(effect, source);
                var fields = BehaviorFields(effect, source).ToList();
                fields.Add(m_Builder.ConstantField(source, "Definition", definition));
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.GameplayEffect,
                    $"effect:{effect.EffectId.Value}",
                    checked((int)effect.DefinitionRevision),
                    Fields(fields),
                    source);
                for (int i = 0; i < effect.Components.Count; i++)
                {
                    if (effect.Components[i] is not GameplayCueBindingComponentDefinition cue || string.IsNullOrEmpty(cue.CueId))
                        continue;
                    m_Builder.DeclareProducer(
                        $"producer:effect:{effect.EffectId.Value}:cue:{i}:{cue.CueId}",
                        "Cue",
                        $"effect:{effect.EffectId.Value}",
                        ProgramOutputChannelKind.Presentation,
                        source);
                }
            }
        }

        void CompileEquipment()
        {
            if (!m_Model.Definition.EquipmentCapabilityEnabled)
                return;
            CharacterEquipmentProfile profile = m_Model.Definition.EquipmentProfile;
            if (!profile)
            {
                m_Report.Error("equipment_profile_missing", DefinitionSource.Identity, "Equipment capability requires a Gameplay Profile.");
                return;
            }
            CharacterSimulationSourceLocation profileSource = AssetSource(profile, $"equipment:profile:{m_Model.GetAssetGuid(profile)}");
            foreach (EquipmentSlotDefinition slot in profile.Slots.Where(value => value != null).OrderBy(value => value.SlotIdValue, StringComparer.Ordinal))
            {
                m_Index.EquipmentSlots.Add(slot.SlotIdValue);
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.EquipmentSlot,
                    $"equipment:slot:{slot.SlotIdValue}",
                    1,
                    Fields(m_Builder.ConstantField(profileSource, "Requirement", slot.Requirement)),
                    profileSource);
            }
            foreach (EquipmentActionRouteDefinition route in profile.Routes.Where(value => value != null).OrderBy(value => value.RouteIdValue, StringComparer.Ordinal))
            {
                m_Index.EquipmentRoutes.Add(route.RouteIdValue);
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.EquipmentRoute,
                    $"equipment:route:{route.RouteIdValue}",
                    1,
                    Fields(
                        m_Builder.IdentityField("OwnerSlot", $"equipment:slot:{route.OwnerSlotIdValue}"),
                        m_Builder.IdentityField("InputRequest", $"input:request:{route.InputRequestId}"),
                        m_Builder.ConstantField(profileSource, "RequestConsumption", route.RequestConsumption),
                        m_Builder.ConstantField(profileSource, "MissingImplementation", route.MissingImplementation)),
                    profileSource);
            }
            foreach (CharacterEquipmentFeatureDefinition feature in profile.Features.Where(value => value).OrderBy(value => value.FeatureIdValue, StringComparer.Ordinal))
                CompileEquipmentFeature(feature);
            foreach (EquipmentDefinition equipment in profile.Equipment.Where(value => value).OrderBy(value => value.EquipmentIdValue, StringComparer.Ordinal))
                CompileEquipmentDefinition(equipment, profile);
            foreach (InitialEquipmentLoadoutEntry loadout in profile.InitialLoadout.Where(value => value != null).OrderBy(value => value.SlotIdValue, StringComparer.Ordinal))
            {
                var fields = new List<ProgramCatalogField>
                {
                    m_Builder.IdentityField("Slot", $"equipment:slot:{loadout.SlotIdValue}"),
                    m_Builder.ConstantField(profileSource, "HasEquipment", loadout.Equipment != null)
                };
                if (loadout.Equipment)
                    fields.Add(m_Builder.IdentityField("Equipment", $"equipment:item:{loadout.Equipment.EquipmentIdValue}"));
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.EquipmentInitialLoadout,
                    $"equipment:loadout:{loadout.SlotIdValue}",
                    1,
                    Fields(fields),
                    profileSource);
            }
            foreach (CharacterCompositionRoot root in m_Model.Roots)
            {
                CharacterSimulationSourceLocation source = new CharacterSimulationSourceLocation(
                    root.Occurrence.Graph.GetType().FullName,
                    root.Occurrence.Graph.GraphAuthoringId,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    root.SourcePath);
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.CompositionRoot,
                    $"composition-root:{root.Identity}",
                    1,
                    Fields(
                        m_Builder.ConstantField(source, "Role", root.Role),
                        m_Builder.IdentityField("Owner", root.OwnerIdentity),
                        m_Builder.IdentityField("Graph", root.Occurrence.Graph.GraphAuthoringId),
                        m_Builder.IdentityField("Feature", root.FeatureId.IsValid ? $"equipment:feature:{root.FeatureId.Value}" : string.Empty),
                        m_Builder.IdentityField("Route", root.RouteId.IsValid ? $"equipment:route:{root.RouteId.Value}" : string.Empty)),
                    source);
            }
            m_Builder.RequireGameplayCapability("Equipment");
        }

        void CompileEquipmentFeature(CharacterEquipmentFeatureDefinition feature)
        {
            CharacterSimulationSourceLocation source = AssetSource(feature, $"equipment:feature:{feature.FeatureIdValue}");
            m_Index.EquipmentFeatures.Add(feature.FeatureIdValue);
            var fields = new List<ProgramCatalogField>
            {
                m_Builder.ConstantField(source, "FeatureRevision", feature.FeatureRevision.Value),
                m_Builder.ConstantField(source, "RequiredWorldCapabilities", (ulong)feature.RequiredWorldCapabilities)
            };
            for (int i = 0; i < feature.GrantedTags.Count; i++)
                fields.Add(m_Builder.IdentityField($"GrantedTag:{i:D4}", $"tag:{feature.GrantedTags[i].Value}"));
            for (int i = 0; i < feature.PassiveEffects.Count; i++)
                fields.Add(m_Builder.IdentityField($"PassiveEffect:{i:D4}", $"effect:{feature.PassiveEffects[i].EffectId.Value}"));
            for (int i = 0; i < feature.RequiredGameplayCapabilities.Count; i++)
            {
                string capability = EquipmentSlotDefinition.Normalize(feature.RequiredGameplayCapabilities[i]);
                fields.Add(m_Builder.IdentityField($"GameplayCapability:{i:D4}", capability));
                m_Builder.RequireGameplayCapability(capability);
            }
            EquipmentFeaturePresentationRequirement presentation = feature.PresentationRequirement;
            if (presentation != null && presentation.Enabled)
            {
                fields.Add(m_Builder.IdentityField("PresentationLayer", presentation.LayerId));
                fields.Add(m_Builder.ConstantField(source, "PresentationBlendMode", presentation.BlendMode));
                fields.Add(m_Builder.ConstantField(source, "PresentationOutputPolicy", presentation.OutputPolicy));
                for (int i = 0; i < presentation.RequiredProducerIds.Count; i++)
                    fields.Add(m_Builder.IdentityField($"RequiredProducer:{i:D4}", EquipmentSlotDefinition.Normalize(presentation.RequiredProducerIds[i])));
            }
            m_Builder.DeclareCatalogEntry(
                ProgramCatalogEntryKind.EquipmentFeature,
                $"equipment:feature:{feature.FeatureIdValue}",
                1,
                Fields(fields),
                source);
            foreach (EquipmentParameterSchema parameter in feature.Parameters.Where(value => value != null).OrderBy(value => value.ParameterIdValue, StringComparer.Ordinal))
            {
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.EquipmentFeatureParameter,
                    $"equipment:feature:{feature.FeatureIdValue}:parameter:{parameter.ParameterIdValue}",
                    1,
                    Fields(
                        m_Builder.IdentityField("Feature", $"equipment:feature:{feature.FeatureIdValue}"),
                        m_Builder.ConstantField(source, "ValueKind", parameter.ValueKind),
                        m_Builder.ConstantField(source, "Required", parameter.Required)),
                    source);
            }
            foreach (EquipmentLocalStateDeclaration state in feature.LocalStates.Where(value => value != null).OrderBy(value => value.StateIdValue, StringComparer.Ordinal))
            {
                string ownerIdentity = $"equipment:feature:{feature.FeatureIdValue}:state:{state.StateIdValue}";
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.EquipmentFeatureLocalState,
                    ownerIdentity,
                    1,
                    Fields(
                        m_Builder.IdentityField("Feature", $"equipment:feature:{feature.FeatureIdValue}"),
                        m_Builder.ConstantField(source, "ValueKind", state.ValueKind)),
                    source);
                m_Builder.DeclareStandaloneStateSlot(
                    source,
                    state.ValueKind,
                    ProgramStateOwnerKind.Equipment,
                    ProgramStateSemantic.EquipmentLocalState,
                    ownerIdentity,
                    ResolveLocalStateDefault(state));
            }
            foreach (EquipmentFeatureRouteImplementation route in feature.RouteImplementations.Where(value => value != null).OrderBy(value => value.RouteIdValue, StringComparer.Ordinal))
            {
                var routeFields = new List<ProgramCatalogField>
                {
                    m_Builder.IdentityField("Feature", $"equipment:feature:{feature.FeatureIdValue}"),
                    m_Builder.IdentityField("Route", $"equipment:route:{route.RouteIdValue}"),
                    m_Builder.IdentityField("Action", $"action:{route.ActionProfile.ActionId}"),
                    m_Builder.IdentityField("Graph", route.InlineGraph.GraphAuthoringId)
                };
                for (int i = 0; i < route.RequiredParameterIds.Count; i++)
                    routeFields.Add(m_Builder.IdentityField($"RequiredParameter:{i:D4}", $"equipment:feature:{feature.FeatureIdValue}:parameter:{EquipmentSlotDefinition.Normalize(route.RequiredParameterIds[i])}"));
                for (int i = 0; i < route.RequiredProducerIds.Count; i++)
                    routeFields.Add(m_Builder.IdentityField($"RequiredProducer:{i:D4}", EquipmentSlotDefinition.Normalize(route.RequiredProducerIds[i])));
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.EquipmentRouteImplementation,
                    $"equipment:feature:{feature.FeatureIdValue}:route:{route.RouteIdValue}",
                    1,
                    Fields(routeFields),
                    source);
            }
            if (feature.RequiredWorldCapabilities != WorldCapability.None)
                m_Builder.RequireWorldRequest($"EquipmentFeature:{feature.FeatureIdValue}", feature.RequiredWorldCapabilities);
        }

        void CompileEquipmentDefinition(EquipmentDefinition equipment, CharacterEquipmentProfile profile)
        {
            CharacterSimulationSourceLocation source = AssetSource(equipment, $"equipment:item:{equipment.EquipmentIdValue}");
            m_Index.EquipmentItems.Add(equipment.EquipmentIdValue);
            m_Builder.DeclareCatalogEntry(
                ProgramCatalogEntryKind.EquipmentDefinition,
                $"equipment:item:{equipment.EquipmentIdValue}",
                1,
                Fields(
                    m_Builder.IdentityField("Slot", $"equipment:slot:{equipment.SlotIdValue}"),
                    m_Builder.IdentityField("Feature", $"equipment:feature:{equipment.Feature.FeatureIdValue}"),
                    m_Builder.IdentityField("VisualBinding", $"equipment:visual:{equipment.VisualBindingIdValue}")),
                source);
            m_Builder.DeclareCatalogEntry(
                ProgramCatalogEntryKind.EquipmentVisualBinding,
                $"equipment:visual:{equipment.VisualBindingIdValue}",
                1,
                Array.Empty<ProgramCatalogField>(),
                source);
            foreach (EquipmentParameterValue value in equipment.ParameterValues.Where(value => value != null).OrderBy(value => value.ParameterIdValue, StringComparer.Ordinal))
            {
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.EquipmentParameterValue,
                    $"equipment:item:{equipment.EquipmentIdValue}:parameter:{value.ParameterIdValue}",
                    1,
                    Fields(
                        m_Builder.IdentityField("Equipment", $"equipment:item:{equipment.EquipmentIdValue}"),
                        m_Builder.IdentityField("Schema", $"equipment:feature:{equipment.Feature.FeatureIdValue}:parameter:{value.ParameterIdValue}"),
                        m_Builder.ConstantField(source, "ValueKind", value.ValueKind),
                        m_Builder.ConstantField(source, "Value", ResolveParameterValue(value))),
                    source);
            }
        }

        static object ResolveParameterValue(EquipmentParameterValue value)
        {
            return value.ValueKind switch
            {
                EquipmentParameterValueKind.Boolean => value.Boolean,
                EquipmentParameterValueKind.Int32 => value.Int32,
                EquipmentParameterValueKind.Scalar => value.Scalar,
                EquipmentParameterValueKind.Vector2 => value.Vector2,
                EquipmentParameterValueKind.Vector3 => value.Vector3,
                EquipmentParameterValueKind.Yaw => value.YawDegrees,
                EquipmentParameterValueKind.GameplayTag => value.GameplayTag.Value,
                EquipmentParameterValueKind.GameplayEffect => value.GameplayEffect.EffectId.Value,
                EquipmentParameterValueKind.AnimationProducer => value.AnimationProducerId,
                _ => throw new ArgumentOutOfRangeException(nameof(value.ValueKind))
            };
        }

        static object ResolveLocalStateDefault(EquipmentLocalStateDeclaration state)
        {
            EquipmentParameterValue value = state.DefaultValue;
            return state.ValueKind switch
            {
                ProgramStateValueKind.Boolean => value.Boolean,
                ProgramStateValueKind.Int32 => value.Int32,
                ProgramStateValueKind.UInt64 => value.UInt64,
                ProgramStateValueKind.Scalar => value.Scalar,
                ProgramStateValueKind.Vector2 => value.Vector2,
                ProgramStateValueKind.Vector3 => value.Vector3,
                ProgramStateValueKind.Yaw => value.YawDegrees,
                ProgramStateValueKind.Identity => value.Identity,
                _ => throw new InvalidOperationException($"Unsupported Equipment local state kind '{state.ValueKind}'.")
            };
        }

        void DeclareGlobalState()
        {
            CharacterSimulationSourceLocation source = DefinitionSource;
            m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.UInt64, ProgramStateOwnerKind.Action, ProgramStateSemantic.ActionEventSequence, "action:event-sequence");
            m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.GameplayEffectAggregate, ProgramStateOwnerKind.GameplayEffect, ProgramStateSemantic.GameplayEffectAggregate, "gameplay-effect:aggregate");
            if (m_Model.Definition.EquipmentCapabilityEnabled)
                m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.EquipmentAggregate, ProgramStateOwnerKind.Equipment, ProgramStateSemantic.EquipmentAggregate, "equipment:aggregate");
            m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.UInt64, ProgramStateOwnerKind.Random, ProgramStateSemantic.RandomState, "runtime:rng");
            m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.UInt64, ProgramStateOwnerKind.Runtime, ProgramStateSemantic.HandleAllocator, "runtime:handle-allocator");
            m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.UInt64, ProgramStateOwnerKind.Fact, ProgramStateSemantic.FactSequence, "runtime:fact-sequence");
            m_Builder.RequireGameplayCapability("RunnableTree");
            m_Builder.RequireGameplayCapability("StateMachine");
            m_Builder.RequireGameplayCapability("Timeline");
            m_Builder.RequireGameplayCapability("PipelineBlackboard");
            m_Builder.RequireGameplayCapability("Action");
            m_Builder.RequireGameplayCapability("GameplayEffect");
            m_Builder.RequireWorldRequest("CharacterBodyMotion", WorldCapability.BodyMotion | WorldCapability.Grounding | WorldCapability.Collision);
        }

        IEnumerable<ProgramCatalogField> BehaviorFields(IGameplayBehaviorProfile profile, CharacterSimulationSourceLocation source)
        {
            yield return m_Builder.ConstantField(source, "BehaviorKind", profile.BehaviorKind);
            yield return m_Builder.ConstantField(source, "DisplayName", profile.DisplayName);
            yield return m_Builder.ConstantField(source, "DebugCategory", profile.DebugCategory);
            for (int i = 0; i < profile.Tags.Count; i++)
                yield return m_Builder.IdentityField($"Tag:{i:D4}", $"tag:{profile.Tags[i].Value}");
        }

        void AddQueryFields(List<ProgramCatalogField> fields, CharacterSimulationSourceLocation source, string prefix, GameplayTagQuery query)
        {
            if (query == null)
            {
                m_Report.Error("tag_query_missing", source.Identity, $"{prefix} tag query is missing.");
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

        void AddBoundFields(List<ProgramCatalogField> fields, CharacterSimulationSourceLocation source, string prefix, GameplayAttributeBoundDefinition bound)
        {
            fields.Add(m_Builder.ConstantField(source, $"{prefix}:Enabled", bound?.Enabled ?? false));
            if (bound == null || !bound.Enabled)
                return;
            fields.Add(m_Builder.ConstantField(source, $"{prefix}:Source", bound.Source));
            if (bound.Source == GameplayAttributeBoundSource.Constant)
                fields.Add(m_Builder.ConstantField(source, $"{prefix}:Constant", bound.Constant));
            else
                fields.Add(m_Builder.IdentityField($"{prefix}:Attribute", $"attribute:{bound.AttributeId.Value}"));
        }

        SemanticDataDocument EncodeEffect(GameplayEffectDefinition effect, CharacterSimulationSourceLocation source)
        {
            try
            {
                var writer = new SemanticDataWriter();
                writer.WriteInt32(1);
                writer.WriteString(effect.EffectId.Value);
                writer.WriteUInt32(effect.DefinitionRevision);
                writer.WriteInt32((int)effect.DurationPolicy);
                WriteMagnitude(writer, effect.DurationMagnitude, source, "Duration");
                writer.WriteBoolean(effect.HasPeriod);
                WriteMagnitude(writer, effect.PeriodMagnitude, source, "Period");
                writer.WriteBoolean(effect.ExecuteOnApplication);
                writer.WriteInt32((int)effect.StackingPolicy);
                writer.WriteInt32(effect.MaxStacks);
                writer.WriteInt32((int)effect.DurationUpdate);
                writer.WriteInt32((int)effect.PeriodUpdate);
                writer.WriteInt32((int)effect.OverflowPolicy);
                writer.WriteInt32(effect.SetByCallerParameters.Count);
                for (int i = 0; i < effect.SetByCallerParameters.Count; i++)
                    writer.WriteString(effect.SetByCallerParameters[i]?.ParameterId);
                writer.WriteInt32(effect.Components.Count);
                for (int i = 0; i < effect.Components.Count; i++)
                    WriteComponent(writer, effect.Components[i], source, i);
                return writer.Build();
            }
            catch (Exception exception)
            {
                m_Report.Error("gameplay_effect_compile_failed", source.Identity, exception.Message);
                return SemanticDataDocument.Empty;
            }
        }

        void WriteComponent(SemanticDataWriter writer, GameplayEffectComponentDefinition component, CharacterSimulationSourceLocation source, int index)
        {
            if (component == null)
                throw new InvalidOperationException($"Gameplay Effect component #{index} is missing.");
            writer.WriteString(component.GetType().FullName);
            switch (component)
            {
                case GameplayModifierComponentDefinition modifier:
                    writer.WriteString(modifier.AttributeId.Value);
                    writer.WriteInt32((int)modifier.Application);
                    writer.WriteInt32((int)modifier.Operation);
                    WriteMagnitude(writer, modifier.Magnitude, source, $"Component[{index}].Magnitude");
                    writer.WriteInt32(modifier.Priority);
                    writer.WriteInt32((int)modifier.ClampBound);
                    writer.WriteBoolean(modifier.ScaleWithStack);
                    break;
                case GrantedTagsComponentDefinition granted:
                    WriteTags(writer, granted.Tags);
                    break;
                case GameplayTagRequirementsComponentDefinition tags:
                    writer.WriteInt32((int)tags.Phase);
                    WriteQuery(writer, tags.Source);
                    WriteQuery(writer, tags.Target);
                    break;
                case GameplayAttributeRequirementsComponentDefinition attributes:
                    writer.WriteInt32((int)attributes.Phase);
                    writer.WriteInt32((int)attributes.Source);
                    writer.WriteString(attributes.AttributeId.Value);
                    writer.WriteInt32((int)attributes.Comparison);
                    WriteMagnitude(writer, attributes.Threshold, source, $"Component[{index}].Threshold");
                    break;
                case GameplayEffectExecutionComponentDefinition execution:
                    writer.WriteInt32(execution.Mutations.Count);
                    for (int i = 0; i < execution.Mutations.Count; i++)
                    {
                        GameplayExecutionMutationDefinition mutation = execution.Mutations[i] ?? throw new InvalidOperationException($"Execution mutation #{i} is missing.");
                        writer.WriteString(mutation.AttributeId.Value);
                        writer.WriteInt32((int)mutation.Operation);
                        WriteMagnitude(writer, mutation.Magnitude, source, $"Component[{index}].Mutation[{i}]");
                        writer.WriteInt32((int)mutation.ClampBound);
                    }
                    break;
                case AdditionalEffectsComponentDefinition additional:
                    writer.WriteInt32(additional.Effects.Count);
                    for (int i = 0; i < additional.Effects.Count; i++)
                    {
                        GameplayAdditionalEffectDefinition child = additional.Effects[i] ?? throw new InvalidOperationException($"Additional Effect #{i} is missing.");
                        writer.WriteInt32((int)child.Trigger);
                        writer.WriteString(child.Effect ? child.Effect.EffectId.Value : string.Empty);
                        writer.WriteInt32(child.ParameterBindings.Count);
                        for (int bindingIndex = 0; bindingIndex < child.ParameterBindings.Count; bindingIndex++)
                        {
                            GameplayAdditionalEffectParameterBindingDefinition binding = child.ParameterBindings[bindingIndex] ?? throw new InvalidOperationException($"Additional Effect binding #{bindingIndex} is missing.");
                            writer.WriteString(binding.ChildParameterId);
                            writer.WriteInt32((int)binding.Source);
                            writer.WriteString(binding.ParentParameterId);
                            writer.WriteNumber(binding.Constant, $"{source.Identity}/Component[{index}].Effect[{i}].Binding[{bindingIndex}]");
                        }
                    }
                    break;
                case GameplayCueBindingComponentDefinition cue:
                    writer.WriteString(cue.CueId);
                    writer.WriteInt32((int)cue.Trigger);
                    break;
                default:
                    throw new InvalidOperationException($"Gameplay Effect component '{component.GetType().FullName}' has no portable compiler.");
            }
        }

        void WriteMagnitude(SemanticDataWriter writer, GameplayMagnitudeDefinition magnitude, CharacterSimulationSourceLocation source, string field)
        {
            if (magnitude == null)
                throw new InvalidOperationException($"Magnitude '{field}' is missing.");
            writer.WriteInt32((int)magnitude.Source);
            writer.WriteNumber(magnitude.Constant, $"{source.Identity}/{field}.Constant");
            writer.WriteString(magnitude.SetByCallerParameterId);
            writer.WriteString(magnitude.AttributeId.Value);
            writer.WriteNumber(magnitude.Coefficient, $"{source.Identity}/{field}.Coefficient");
            writer.WriteNumber(magnitude.PostAdd, $"{source.Identity}/{field}.PostAdd");
        }

        static void WriteQuery(SemanticDataWriter writer, GameplayTagQuery query)
        {
            if (query == null)
                throw new InvalidOperationException("Gameplay Tag query is missing.");
            WriteTags(writer, query.All);
            WriteTags(writer, query.Any);
            WriteTags(writer, query.None);
        }

        static void WriteTags(SemanticDataWriter writer, IReadOnlyList<GameplayTagId> tags)
        {
            writer.WriteInt32(tags.Count);
            for (int i = 0; i < tags.Count; i++)
                writer.WriteString(tags[i].Value);
        }

        CharacterSimulationSourceLocation DefinitionSource => AssetSource(m_Model.Definition, $"definition:{m_Model.Definition.name}");

        CharacterSimulationSourceLocation AssetSource(UnityEngine.Object asset, string identity)
        {
            string guid = asset ? m_Model.GetAssetGuid(asset) : string.Empty;
            return new CharacterSimulationSourceLocation(
                asset ? asset.GetType().FullName : "MissingAsset",
                m_RootGraphId,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                $"asset:{guid}/{identity}");
        }

        static ProgramCatalogField[] Fields(params ProgramCatalogField[] fields) => Fields((IEnumerable<ProgramCatalogField>)fields);
        static ProgramCatalogField[] Fields(IEnumerable<ProgramCatalogField> fields) => fields?.Where(value => value != null).ToArray() ?? Array.Empty<ProgramCatalogField>();
    }
}
