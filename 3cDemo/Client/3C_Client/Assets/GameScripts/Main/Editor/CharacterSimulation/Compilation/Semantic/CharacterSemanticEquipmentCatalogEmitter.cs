using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Equipment;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterSemanticEquipmentCatalogEmitter
    {
        readonly CharacterAuthoringCompilationModel m_Model;
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;
        readonly CharacterSimulationCatalogIndex m_Index;

        public CharacterSemanticEquipmentCatalogEmitter(
            CharacterAuthoringCompilationModel model,
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report,
            CharacterSimulationCatalogIndex index)
        {
            m_Model = model ?? throw new ArgumentNullException(nameof(model));
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
            m_Index = index ?? throw new ArgumentNullException(nameof(index));
        }

        public void Emit()
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
                EmitFeature(feature);
            foreach (EquipmentDefinition equipment in profile.Equipment.Where(value => value).OrderBy(value => value.EquipmentIdValue, StringComparer.Ordinal))
                EmitDefinition(equipment);
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

        void EmitFeature(CharacterEquipmentFeatureDefinition feature)
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
            m_Builder.DeclareCatalogEntry(
                ProgramCatalogEntryKind.EquipmentFeature,
                $"equipment:feature:{feature.FeatureIdValue}",
                1,
                Fields(fields),
                source);
            foreach (EquipmentParameterSchema parameter in feature.Parameters.Where(value => value != null).OrderBy(value => value.ParameterIdValue, StringComparer.Ordinal))
            {
                m_Index.EquipmentParameters.Add($"{feature.FeatureIdValue}:{parameter.ParameterIdValue}");
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

        void EmitDefinition(EquipmentDefinition equipment)
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

        CharacterSimulationSourceLocation DefinitionSource => AssetSource(m_Model.Definition, $"definition:{m_Model.Definition.name}");

        CharacterSimulationSourceLocation AssetSource(UnityEngine.Object asset, string identity) =>
            CharacterSemanticSourceFactory.Asset(m_Model, asset, identity);

        static ProgramCatalogField[] Fields(params ProgramCatalogField[] fields) => Fields((IEnumerable<ProgramCatalogField>)fields);
        static ProgramCatalogField[] Fields(IEnumerable<ProgramCatalogField> fields) =>
            fields?.Where(value => value != null).ToArray() ?? Array.Empty<ProgramCatalogField>();
    }
}
