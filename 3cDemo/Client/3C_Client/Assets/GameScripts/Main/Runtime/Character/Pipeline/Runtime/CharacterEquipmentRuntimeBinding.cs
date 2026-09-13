using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Equipment;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline
{
    public sealed partial class CharacterPipelineDefinition
    {
        public CharacterEquipmentRuntimeBinding BuildEquipmentRuntimeBinding()
        {
            if (!EquipmentCapabilityEnabled)
                return null;
            CharacterEquipmentProfile profile = EquipmentProfile ??
                throw new InvalidOperationException("Character Pipeline Definition Equipment profile is missing.");
            var errors = new List<string>();
            if (!profile.CollectConfigurationErrors(this, errors))
                throw new InvalidOperationException(string.Join(" ", errors));

            using var writer = new CanonicalWriter();
            writer.WriteInt32(CharacterEquipmentRuntimeBinding.CatalogFormatVersion);
            IReadOnlyList<EquipmentSlotDefinition> slots = profile.Slots
                .Where(value => value != null)
                .OrderBy(value => value.SlotIdValue, StringComparer.Ordinal)
                .ToArray();
            var initialLoadout = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < profile.InitialLoadout.Count; i++)
            {
                InitialEquipmentLoadoutEntry entry = profile.InitialLoadout[i];
                initialLoadout.Add(entry.SlotIdValue, entry.Equipment ? entry.Equipment.EquipmentIdValue : string.Empty);
            }
            writer.WriteInt32(slots.Count);
            for (int i = 0; i < slots.Count; i++)
            {
                EquipmentSlotDefinition slot = slots[i];
                writer.WriteString(slot.SlotIdValue);
                writer.WriteByte((byte)slot.Requirement);
                writer.WriteString(initialLoadout[slot.SlotIdValue]);
            }

            IReadOnlyList<CharacterEquipmentFeatureDefinition> features = profile.Features
                .Where(value => value)
                .OrderBy(value => value.FeatureIdValue, StringComparer.Ordinal)
                .ToArray();
            writer.WriteInt32(features.Count);
            for (int i = 0; i < features.Count; i++)
            {
                CharacterEquipmentFeatureDefinition feature = features[i];
                writer.WriteString(feature.FeatureIdValue);
                writer.WriteUInt64(feature.FeatureRevision.Value);
                writer.WriteString(feature.CodeBindingIdValue);
                WriteIdentities(writer, feature.GrantedTags.Select(value => value.Value));
                WriteIdentities(writer, feature.PassiveEffects.Select(value => value.EffectId.Value));
                writer.WriteUInt64((ulong)feature.RequiredWorldCapabilities);
            }

            IReadOnlyList<EquipmentDefinition> equipment = profile.Equipment
                .Where(value => value)
                .OrderBy(value => value.EquipmentIdValue, StringComparer.Ordinal)
                .ToArray();
            writer.WriteInt32(equipment.Count);
            for (int i = 0; i < equipment.Count; i++)
            {
                EquipmentDefinition item = equipment[i];
                writer.WriteString(item.EquipmentIdValue);
                writer.WriteString(item.SlotIdValue);
                writer.WriteString(item.Feature.FeatureIdValue);
                writer.WriteString(item.VisualBindingIdValue);
            }

            IReadOnlyList<EquipmentActionRouteDefinition> routes = profile.Routes
                .Where(value => value != null)
                .OrderBy(value => value.RouteIdValue, StringComparer.Ordinal)
                .ToArray();
            writer.WriteInt32(routes.Count);
            for (int i = 0; i < routes.Count; i++)
            {
                EquipmentActionRouteDefinition route = routes[i];
                writer.WriteString(route.RouteIdValue);
                writer.WriteString(route.OwnerSlotIdValue);
                writer.WriteString(route.InputRequestId);
                writer.WriteByte((byte)route.RequestConsumption);
                writer.WriteByte((byte)route.MissingImplementation);
            }

            var routeImplementations = new List<(CharacterEquipmentFeatureDefinition Feature, EquipmentFeatureRouteImplementation Route)>();
            for (int i = 0; i < features.Count; i++)
            {
                IReadOnlyList<EquipmentFeatureRouteImplementation> values = features[i].RouteImplementations;
                for (int routeIndex = 0; routeIndex < values.Count; routeIndex++)
                    if (values[routeIndex] != null)
                        routeImplementations.Add((features[i], values[routeIndex]));
            }
            routeImplementations.Sort((left, right) =>
            {
                int feature = string.CompareOrdinal(left.Feature.FeatureIdValue, right.Feature.FeatureIdValue);
                return feature != 0
                    ? feature
                    : string.CompareOrdinal(left.Route.RouteIdValue, right.Route.RouteIdValue);
            });
            writer.WriteInt32(routeImplementations.Count);
            for (int i = 0; i < routeImplementations.Count; i++)
            {
                EquipmentFeatureRouteImplementation implementation = routeImplementations[i].Route;
                writer.WriteString(routeImplementations[i].Feature.FeatureIdValue);
                writer.WriteString(implementation.RouteIdValue);
                writer.WriteString(implementation.AbilityIdValue);
                WriteIdentities(writer, implementation.RequiredParameterIds);
                WriteIdentities(writer, implementation.RequiredProducerIds);
            }

            var parameterValues = new List<(EquipmentDefinition Equipment, EquipmentParameterValue Value)>();
            for (int i = 0; i < equipment.Count; i++)
            {
                IReadOnlyList<EquipmentParameterValue> values = equipment[i].ParameterValues;
                for (int parameterIndex = 0; parameterIndex < values.Count; parameterIndex++)
                    if (values[parameterIndex] != null)
                        parameterValues.Add((equipment[i], values[parameterIndex]));
            }
            parameterValues.Sort((left, right) =>
            {
                int equipmentId = string.CompareOrdinal(left.Equipment.EquipmentIdValue, right.Equipment.EquipmentIdValue);
                return equipmentId != 0
                    ? equipmentId
                    : string.CompareOrdinal(left.Value.ParameterIdValue, right.Value.ParameterIdValue);
            });
            writer.WriteInt32(parameterValues.Count);
            for (int i = 0; i < parameterValues.Count; i++)
            {
                EquipmentParameterValue value = parameterValues[i].Value;
                writer.WriteString(parameterValues[i].Equipment.EquipmentIdValue);
                writer.WriteString(parameterValues[i].Equipment.Feature.FeatureIdValue);
                writer.WriteString(value.ParameterIdValue);
                writer.WriteByte((byte)value.ValueKind);
                WriteParameterValue(writer, value);
            }

            var localStates = new List<(CharacterEquipmentFeatureDefinition Feature, EquipmentLocalStateDeclaration State)>();
            for (int i = 0; i < features.Count; i++)
            {
                IReadOnlyList<EquipmentLocalStateDeclaration> values = features[i].LocalStates;
                for (int stateIndex = 0; stateIndex < values.Count; stateIndex++)
                    if (values[stateIndex] != null)
                        localStates.Add((features[i], values[stateIndex]));
            }
            localStates.Sort((left, right) =>
            {
                int feature = string.CompareOrdinal(left.Feature.FeatureIdValue, right.Feature.FeatureIdValue);
                return feature != 0
                    ? feature
                    : string.CompareOrdinal(left.State.StateIdValue, right.State.StateIdValue);
            });
            writer.WriteInt32(localStates.Count);
            for (int i = 0; i < localStates.Count; i++)
            {
                writer.WriteString(localStates[i].Feature.FeatureIdValue);
                writer.WriteString(localStates[i].State.StateIdValue);
                writer.WriteByte((byte)ToRuntimeStateValueKind(localStates[i].State.ValueKind));
                WriteStateValue(writer, localStates[i].State);
            }

            byte[] catalogBytes = writer.ToArray();
            return new CharacterEquipmentRuntimeBinding(
                $"character-equipment-profile:{profile.name}",
                SimulationCanonicalPayloadHash.Compute(catalogBytes),
                CharacterEquipmentRuntimeBinding.ContractSemanticVersion,
                catalogBytes);
        }

        static void WriteIdentities(CanonicalWriter writer, IEnumerable<string> values)
        {
            string[] identities = (values ?? Array.Empty<string>()).Select(value => value ?? string.Empty).ToArray();
            writer.WriteInt32(identities.Length);
            for (int i = 0; i < identities.Length; i++)
                writer.WriteString(identities[i]);
        }

        static void WriteParameterValue(CanonicalWriter writer, EquipmentParameterValue value)
        {
            writer.WriteBoolean(value.Boolean);
            writer.WriteInt32(value.Int32);
            writer.WriteUInt64(value.UInt64);
            writer.WriteDouble(value.Scalar);
            writer.WriteDouble(value.Vector2.x);
            writer.WriteDouble(value.Vector2.y);
            writer.WriteDouble(value.Vector3.x);
            writer.WriteDouble(value.Vector3.y);
            writer.WriteDouble(value.Vector3.z);
            writer.WriteDouble(value.YawDegrees);
            string identity = value.ValueKind switch
            {
                EquipmentParameterValueKind.GameplayTag => value.GameplayTag.Value,
                EquipmentParameterValueKind.GameplayEffect => value.GameplayEffect ? value.GameplayEffect.EffectId.Value : string.Empty,
                EquipmentParameterValueKind.AnimationProducer => value.AnimationProducerId,
                _ => value.Identity
            };
            writer.WriteString(identity);
        }

        static EquipmentRuntimeStateValueKind ToRuntimeStateValueKind(ProgramStateValueKind kind) => kind switch
        {
            ProgramStateValueKind.Boolean => EquipmentRuntimeStateValueKind.Boolean,
            ProgramStateValueKind.Int32 => EquipmentRuntimeStateValueKind.Int32,
            ProgramStateValueKind.UInt64 => EquipmentRuntimeStateValueKind.UInt64,
            ProgramStateValueKind.Scalar => EquipmentRuntimeStateValueKind.Scalar,
            ProgramStateValueKind.Vector2 => EquipmentRuntimeStateValueKind.Vector2,
            ProgramStateValueKind.Vector3 => EquipmentRuntimeStateValueKind.Vector3,
            ProgramStateValueKind.Yaw => EquipmentRuntimeStateValueKind.Yaw,
            ProgramStateValueKind.Identity => EquipmentRuntimeStateValueKind.Identity,
            _ => throw new InvalidOperationException($"Unsupported Equipment local state kind '{kind}'.")
        };

        static void WriteStateValue(CanonicalWriter writer, EquipmentLocalStateDeclaration state)
        {
            EquipmentParameterValue value = state.DefaultValue ?? throw new InvalidOperationException($"Equipment local state '{state.StateIdValue}' has no default value.");
            writer.WriteBoolean(value.Boolean);
            writer.WriteInt32(value.Int32);
            writer.WriteUInt64(value.UInt64);
            writer.WriteDouble(state.ValueKind == ProgramStateValueKind.Scalar ? value.Scalar :
                state.ValueKind == ProgramStateValueKind.Vector2 ? value.Vector2.x :
                state.ValueKind == ProgramStateValueKind.Vector3 ? value.Vector3.x :
                state.ValueKind == ProgramStateValueKind.Yaw ? value.YawDegrees : 0d);
            writer.WriteDouble(state.ValueKind == ProgramStateValueKind.Vector2 ? value.Vector2.y :
                state.ValueKind == ProgramStateValueKind.Vector3 ? value.Vector3.y : 0d);
            writer.WriteDouble(state.ValueKind == ProgramStateValueKind.Vector3 ? value.Vector3.z : 0d);
            writer.WriteString(state.ValueKind == ProgramStateValueKind.Identity ? value.Identity : string.Empty);
        }
    }
}
