using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace ThirdPersonSimulation
{
    public sealed class EquipmentProgramSlot
    {
        public EquipmentProgramSlot(EquipmentSlotId slotId, EquipmentSlotRequirement requirement, EquipmentId initialEquipmentId)
        {
            if (!slotId.IsValid || !Enum.IsDefined(typeof(EquipmentSlotRequirement), requirement))
                throw new ArgumentException("Equipment Program Slot is invalid.");
            if (requirement == EquipmentSlotRequirement.Required && !initialEquipmentId.IsValid)
                throw new ArgumentException("Required Equipment Program Slot has no initial Equipment.");
            SlotId = slotId;
            Requirement = requirement;
            InitialEquipmentId = initialEquipmentId;
        }

        public EquipmentSlotId SlotId { get; }
        public EquipmentSlotRequirement Requirement { get; }
        public EquipmentId InitialEquipmentId { get; }
    }

    public sealed class EquipmentProgramFeature
    {
        readonly ReadOnlyCollection<string> m_GrantedTags;
        readonly ReadOnlyCollection<string> m_PassiveEffects;

        public EquipmentProgramFeature(
            EquipmentFeatureId featureId,
            EquipmentFeatureRevision revision,
            string codeBindingId,
            IEnumerable<string> grantedTags,
            IEnumerable<string> passiveEffects,
            WorldCapability requiredWorldCapabilities)
        {
            if (!featureId.IsValid || !revision.IsValid || string.IsNullOrEmpty(codeBindingId))
                throw new ArgumentException("Equipment Program Feature identity is invalid.");
            FeatureId = featureId;
            Revision = revision;
            CodeBindingId = SimulationIdentity.Require(codeBindingId, nameof(codeBindingId));
            m_GrantedTags = StableIdentities(grantedTags, "Equipment granted Tag");
            m_PassiveEffects = StableIdentities(passiveEffects, "Equipment passive Effect");
            RequiredWorldCapabilities = requiredWorldCapabilities;
        }

        public EquipmentFeatureId FeatureId { get; }
        public EquipmentFeatureRevision Revision { get; }
        public string CodeBindingId { get; }
        public IReadOnlyList<string> GrantedTags => m_GrantedTags;
        public IReadOnlyList<string> PassiveEffects => m_PassiveEffects;
        public WorldCapability RequiredWorldCapabilities { get; }

        static ReadOnlyCollection<string> StableIdentities(IEnumerable<string> source, string label)
        {
            string[] values = (source ?? Array.Empty<string>()).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            for (int i = 0; i < values.Length; i++)
            {
                SimulationIdentity.Require(values[i], label);
                if (i > 0 && string.Equals(values[i - 1], values[i], StringComparison.Ordinal))
                    throw new InvalidDataException($"{label} '{values[i]}' is duplicated.");
            }
            return Array.AsReadOnly(values);
        }
    }

    public sealed class EquipmentProgramItem
    {
        public EquipmentProgramItem(
            EquipmentId equipmentId,
            EquipmentSlotId slotId,
            EquipmentFeatureId featureId,
            EquipmentVisualBindingId visualBindingId)
        {
            if (!equipmentId.IsValid || !slotId.IsValid || !featureId.IsValid || !visualBindingId.IsValid)
                throw new ArgumentException("Equipment Program Item is invalid.");
            EquipmentId = equipmentId;
            SlotId = slotId;
            FeatureId = featureId;
            VisualBindingId = visualBindingId;
        }

        public EquipmentId EquipmentId { get; }
        public EquipmentSlotId SlotId { get; }
        public EquipmentFeatureId FeatureId { get; }
        public EquipmentVisualBindingId VisualBindingId { get; }
    }

    public sealed class EquipmentProgramRoute
    {
        public EquipmentProgramRoute(
            EquipmentActionRouteId routeId,
            EquipmentSlotId ownerSlotId,
            string inputRequestId,
            EquipmentRouteRequestConsumption requestConsumption,
            EquipmentRouteMissingImplementation missingImplementation)
        {
            if (!routeId.IsValid || !ownerSlotId.IsValid || string.IsNullOrEmpty(inputRequestId) ||
                !Enum.IsDefined(typeof(EquipmentRouteRequestConsumption), requestConsumption) ||
                !Enum.IsDefined(typeof(EquipmentRouteMissingImplementation), missingImplementation))
            {
                throw new ArgumentException("Equipment Program Route is invalid.");
            }
            RouteId = routeId;
            OwnerSlotId = ownerSlotId;
            InputRequestId = inputRequestId;
            RequestConsumption = requestConsumption;
            MissingImplementation = missingImplementation;
        }

        public EquipmentActionRouteId RouteId { get; }
        public EquipmentSlotId OwnerSlotId { get; }
        public string InputRequestId { get; }
        public EquipmentRouteRequestConsumption RequestConsumption { get; }
        public EquipmentRouteMissingImplementation MissingImplementation { get; }
    }

    public sealed class EquipmentProgramRouteImplementation
    {
        readonly ReadOnlyCollection<EquipmentParameterId> m_RequiredParameterIds;
        readonly ReadOnlyCollection<string> m_RequiredProducerIds;

        public EquipmentProgramRouteImplementation(
            EquipmentFeatureId featureId,
            EquipmentActionRouteId routeId,
            CharacterSkillId abilityId,
            IEnumerable<EquipmentParameterId> requiredParameterIds = null,
            IEnumerable<string> requiredProducerIds = null)
        {
            if (!featureId.IsValid || !routeId.IsValid || !abilityId.IsValid)
                throw new ArgumentException("Equipment Route implementation is invalid.");
            FeatureId = featureId;
            RouteId = routeId;
            AbilityId = abilityId;
            m_RequiredParameterIds = StableParameters(requiredParameterIds);
            m_RequiredProducerIds = StableIdentities(requiredProducerIds, "Equipment required Producer");
        }

        public EquipmentFeatureId FeatureId { get; }
        public EquipmentActionRouteId RouteId { get; }
        public CharacterSkillId AbilityId { get; }
        public IReadOnlyList<EquipmentParameterId> RequiredParameterIds => m_RequiredParameterIds;
        public IReadOnlyList<string> RequiredProducerIds => m_RequiredProducerIds;

        static ReadOnlyCollection<EquipmentParameterId> StableParameters(IEnumerable<EquipmentParameterId> source)
        {
            EquipmentParameterId[] values = (source ?? Array.Empty<EquipmentParameterId>()).OrderBy(value => value.Value, StringComparer.Ordinal).ToArray();
            for (int i = 0; i < values.Length; i++)
            {
                if (!values[i].IsValid || i > 0 && values[i - 1] == values[i])
                    throw new InvalidDataException("Equipment required Parameter identities are invalid or duplicated.");
            }
            return Array.AsReadOnly(values);
        }

        static ReadOnlyCollection<string> StableIdentities(IEnumerable<string> source, string label)
        {
            string[] values = (source ?? Array.Empty<string>()).Select(value => SimulationIdentity.Require(value, label)).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            for (int i = 1; i < values.Length; i++)
                if (string.Equals(values[i - 1], values[i], StringComparison.Ordinal))
                    throw new InvalidDataException($"{label} '{values[i]}' is duplicated.");
            return Array.AsReadOnly(values);
        }
    }

    public sealed class EquipmentProgramParameter
    {
        public EquipmentProgramParameter(
            EquipmentId equipmentId,
            EquipmentFeatureId featureId,
            EquipmentParameterId parameterId,
            EquipmentParameterValueKind valueKind,
            EquipmentRuntimeParameterValue value)
        {
            if (!equipmentId.IsValid || !featureId.IsValid || !parameterId.IsValid ||
                !Enum.IsDefined(typeof(EquipmentParameterValueKind), valueKind) || value == null || value.Kind != valueKind)
            {
                throw new ArgumentException("Equipment Program Parameter is invalid.");
            }
            EquipmentId = equipmentId;
            FeatureId = featureId;
            ParameterId = parameterId;
            ValueKind = valueKind;
            Value = value;
        }

        public EquipmentId EquipmentId { get; }
        public EquipmentFeatureId FeatureId { get; }
        public EquipmentParameterId ParameterId { get; }
        public EquipmentParameterValueKind ValueKind { get; }
        public EquipmentRuntimeParameterValue Value { get; }
    }

    public sealed class EquipmentProgramLocalState
    {
        public EquipmentProgramLocalState(
            EquipmentFeatureId featureId,
            EquipmentLocalStateId stateId,
            EquipmentRuntimeStateValueKind valueKind,
            EquipmentRuntimeStateValue defaultValue)
        {
            if (!featureId.IsValid || !stateId.IsValid || !EquipmentRuntimeStateValue.IsValidKind(valueKind) ||
                defaultValue == null || defaultValue.Kind != valueKind)
                throw new ArgumentException("Equipment Program local state is invalid.");
            FeatureId = featureId;
            StateId = stateId;
            ValueKind = valueKind;
            DefaultValue = defaultValue;
        }

        public EquipmentFeatureId FeatureId { get; }
        public EquipmentLocalStateId StateId { get; }
        public EquipmentRuntimeStateValueKind ValueKind { get; }
        public EquipmentRuntimeStateValue DefaultValue { get; }
    }

    public sealed class EquipmentLocalStateValue
    {
        public EquipmentLocalStateValue(
            EquipmentFeatureId featureId,
            EquipmentLocalStateId stateId,
            EquipmentRuntimeStateValue value)
        {
            if (!featureId.IsValid || !stateId.IsValid || value == null)
                throw new ArgumentException("Equipment local state value is invalid.");
            FeatureId = featureId;
            StateId = stateId;
            Value = value;
        }

        public EquipmentFeatureId FeatureId { get; }
        public EquipmentLocalStateId StateId { get; }
        public EquipmentRuntimeStateValue Value { get; }
    }

    public sealed class EquipmentProgramOperationBinding
    {
        public EquipmentProgramOperationBinding(
            OperationHandle operation,
            EquipmentSlotId slotId,
            EquipmentActionRouteId routeId,
            EquipmentId equipmentId,
            EquipmentParameterId parameterId)
        {
            if (!operation.IsValid || !slotId.IsValid && !routeId.IsValid && !equipmentId.IsValid && !parameterId.IsValid)
                throw new ArgumentException("Equipment Program operation binding is invalid.");
            Operation = operation;
            SlotId = slotId;
            RouteId = routeId;
            EquipmentId = equipmentId;
            ParameterId = parameterId;
        }

        public OperationHandle Operation { get; }
        public EquipmentSlotId SlotId { get; }
        public EquipmentActionRouteId RouteId { get; }
        public EquipmentId EquipmentId { get; }
        public EquipmentParameterId ParameterId { get; }
    }

    public sealed class EquipmentProgramLayout
    {
        readonly ReadOnlyCollection<EquipmentProgramSlot> m_Slots;
        readonly ReadOnlyCollection<EquipmentProgramFeature> m_Features;
        readonly ReadOnlyCollection<EquipmentProgramItem> m_Items;
        readonly ReadOnlyCollection<EquipmentProgramRoute> m_Routes;
        readonly ReadOnlyCollection<EquipmentProgramRouteImplementation> m_RouteImplementations;
        readonly ReadOnlyCollection<EquipmentProgramParameter> m_Parameters;
        readonly ReadOnlyCollection<EquipmentProgramLocalState> m_LocalStates;
        readonly ReadOnlyCollection<EquipmentProgramOperationBinding> m_OperationBindings;
        readonly Dictionary<EquipmentSlotId, EquipmentProgramSlot> m_SlotById;
        readonly Dictionary<EquipmentFeatureId, EquipmentProgramFeature> m_FeatureById;
        readonly Dictionary<string, EquipmentProgramFeature> m_FeatureByCodeBinding;
        readonly Dictionary<EquipmentId, EquipmentProgramItem> m_ItemById;
        readonly Dictionary<EquipmentActionRouteId, EquipmentProgramRoute> m_RouteById;
        readonly Dictionary<(EquipmentFeatureId, EquipmentActionRouteId), EquipmentProgramRouteImplementation> m_RouteImplementationByKey;
        readonly Dictionary<(EquipmentId, EquipmentParameterId), EquipmentProgramParameter> m_ParameterByKey;
        readonly Dictionary<(EquipmentFeatureId, EquipmentLocalStateId), EquipmentProgramLocalState> m_LocalStateByKey;
        readonly Dictionary<int, EquipmentProgramOperationBinding> m_OperationBindingByHandle;

        public EquipmentProgramLayout(
            bool capabilityEnabled,
            IEnumerable<EquipmentProgramSlot> slots,
            IEnumerable<EquipmentProgramFeature> features,
            IEnumerable<EquipmentProgramItem> items,
            IEnumerable<EquipmentProgramRoute> routes,
            IEnumerable<EquipmentProgramRouteImplementation> routeImplementations,
            IEnumerable<EquipmentProgramParameter> parameters,
            IEnumerable<EquipmentProgramLocalState> localStates,
            IEnumerable<EquipmentProgramOperationBinding> operationBindings)
        {
            CapabilityEnabled = capabilityEnabled;
            m_Slots = Canonical(slots, value => value.SlotId.Value, "Equipment Slot");
            m_Features = Canonical(features, value => value.FeatureId.Value, "Equipment Feature");
            m_Items = Canonical(items, value => value.EquipmentId.Value, "Equipment Item");
            m_Routes = Canonical(routes, value => value.RouteId.Value, "Equipment Route");
            m_RouteImplementations = Canonical(routeImplementations, value => $"{value.FeatureId.Value}:{value.RouteId.Value}", "Equipment Route implementation");
            m_Parameters = Canonical(parameters, value => $"{value.EquipmentId.Value}:{value.ParameterId.Value}", "Equipment Parameter");
            m_LocalStates = Canonical(localStates, value => $"{value.FeatureId.Value}:{value.StateId.Value}", "Equipment local state");
            m_OperationBindings = Canonical(operationBindings, value => value.Operation.Value.ToString("D10", System.Globalization.CultureInfo.InvariantCulture), "Equipment operation binding");
            if (!capabilityEnabled && (m_Slots.Count != 0 || m_Features.Count != 0 || m_Items.Count != 0 || m_Routes.Count != 0))
                throw new InvalidDataException("Equipment catalog exists while capability is disabled.");
            if (capabilityEnabled && m_Slots.Count == 0)
                throw new InvalidDataException("Equipment capability has no Slot catalog.");
            m_SlotById = m_Slots.ToDictionary(value => value.SlotId);
            m_FeatureById = m_Features.ToDictionary(value => value.FeatureId);
            m_FeatureByCodeBinding = new Dictionary<string, EquipmentProgramFeature>(StringComparer.Ordinal);
            for (int i = 0; i < m_Features.Count; i++)
            {
                EquipmentProgramFeature feature = m_Features[i];
                if (!m_FeatureByCodeBinding.TryAdd(feature.CodeBindingId, feature))
                    throw new InvalidDataException($"Equipment Feature code binding '{feature.CodeBindingId}' is duplicated.");
            }
            m_ItemById = m_Items.ToDictionary(value => value.EquipmentId);
            m_RouteById = m_Routes.ToDictionary(value => value.RouteId);
            m_RouteImplementationByKey = m_RouteImplementations.ToDictionary(value => (value.FeatureId, value.RouteId));
            m_ParameterByKey = m_Parameters.ToDictionary(value => (value.EquipmentId, value.ParameterId));
            m_LocalStateByKey = m_LocalStates.ToDictionary(value => (value.FeatureId, value.StateId));
            m_OperationBindingByHandle = m_OperationBindings.ToDictionary(value => value.Operation.Value);
            ValidateClosure();
            CatalogHash = ComputeHash();
        }

        public bool CapabilityEnabled { get; }
        public IReadOnlyList<EquipmentProgramSlot> Slots => m_Slots;
        public IReadOnlyList<EquipmentProgramFeature> Features => m_Features;
        public IReadOnlyList<EquipmentProgramItem> Items => m_Items;
        public IReadOnlyList<EquipmentProgramRoute> Routes => m_Routes;
        public IReadOnlyList<EquipmentProgramRouteImplementation> RouteImplementations => m_RouteImplementations;
        public IReadOnlyList<EquipmentProgramParameter> Parameters => m_Parameters;
        public IReadOnlyList<EquipmentProgramLocalState> LocalStates => m_LocalStates;
        public IReadOnlyList<EquipmentProgramOperationBinding> OperationBindings => m_OperationBindings;
        public StableHash CatalogHash { get; }

        public EquipmentProgramSlot RequireSlot(EquipmentSlotId slotId) =>
            m_SlotById.TryGetValue(slotId, out EquipmentProgramSlot value) ? value : throw new InvalidOperationException($"Equipment Slot '{slotId}' is absent from Program.");
        public EquipmentProgramFeature RequireFeature(EquipmentFeatureId featureId) =>
            m_FeatureById.TryGetValue(featureId, out EquipmentProgramFeature value) ? value : throw new InvalidOperationException($"Equipment Feature '{featureId}' is absent from Program.");
        public EquipmentProgramFeature RequireFeatureByCodeBinding(string codeBindingId) =>
            m_FeatureByCodeBinding.TryGetValue(SimulationIdentity.Require(codeBindingId, nameof(codeBindingId)), out EquipmentProgramFeature value)
                ? value
                : throw new InvalidOperationException($"Equipment Feature code binding '{codeBindingId}' is absent from Program.");
        public EquipmentProgramItem RequireItem(EquipmentId equipmentId) =>
            m_ItemById.TryGetValue(equipmentId, out EquipmentProgramItem value) ? value : throw new InvalidOperationException($"Equipment '{equipmentId}' is absent from Program.");
        public EquipmentProgramRoute RequireRoute(EquipmentActionRouteId routeId) =>
            m_RouteById.TryGetValue(routeId, out EquipmentProgramRoute value) ? value : throw new InvalidOperationException($"Equipment Route '{routeId}' is absent from Program.");
        public bool TryGetRouteImplementation(EquipmentFeatureId featureId, EquipmentActionRouteId routeId, out EquipmentProgramRouteImplementation value) =>
            m_RouteImplementationByKey.TryGetValue((featureId, routeId), out value);
        public EquipmentProgramParameter RequireParameter(EquipmentId equipmentId, EquipmentParameterId parameterId) =>
            m_ParameterByKey.TryGetValue((equipmentId, parameterId), out EquipmentProgramParameter value) ? value : throw new InvalidOperationException($"Equipment Parameter '{equipmentId}/{parameterId}' is absent from Program.");
        public EquipmentProgramLocalState RequireLocalState(EquipmentFeatureId featureId, EquipmentLocalStateId stateId) =>
            m_LocalStateByKey.TryGetValue((featureId, stateId), out EquipmentProgramLocalState value) ? value : throw new InvalidOperationException($"Equipment local state '{featureId}/{stateId}' is absent from Program.");
        public EquipmentSlotId RequireOperationSlot(OperationHandle operation)
        {
            EquipmentProgramOperationBinding binding = RequireOperationBinding(operation);
            return binding.SlotId.IsValid ? binding.SlotId : throw new InvalidOperationException($"Equipment operation '{operation}' has no Slot binding.");
        }
        public EquipmentActionRouteId RequireOperationRoute(OperationHandle operation)
        {
            EquipmentProgramOperationBinding binding = RequireOperationBinding(operation);
            return binding.RouteId.IsValid ? binding.RouteId : throw new InvalidOperationException($"Equipment operation '{operation}' has no Route binding.");
        }
        public EquipmentParameterId RequireOperationParameter(OperationHandle operation)
        {
            EquipmentProgramOperationBinding binding = RequireOperationBinding(operation);
            return binding.ParameterId.IsValid ? binding.ParameterId : throw new InvalidOperationException($"Equipment operation '{operation}' has no Parameter binding.");
        }
        public bool TryGetOperationEquipment(OperationHandle operation, out EquipmentId equipmentId)
        {
            if (m_OperationBindingByHandle.TryGetValue(operation.Value, out EquipmentProgramOperationBinding binding) && binding.EquipmentId.IsValid)
            {
                equipmentId = binding.EquipmentId;
                return true;
            }
            equipmentId = default;
            return false;
        }

        EquipmentProgramOperationBinding RequireOperationBinding(OperationHandle operation) =>
            m_OperationBindingByHandle.TryGetValue(operation.Value, out EquipmentProgramOperationBinding value)
                ? value
                : throw new InvalidOperationException($"Equipment operation '{operation}' has no compiled binding.");

        void ValidateClosure()
        {
            for (int i = 0; i < m_Items.Count; i++)
            {
                EquipmentProgramItem item = m_Items[i];
                if (!m_SlotById.ContainsKey(item.SlotId) || !m_FeatureById.ContainsKey(item.FeatureId))
                    throw new InvalidDataException($"Equipment Item '{item.EquipmentId}' has a dangling Slot or Feature.");
            }
            for (int i = 0; i < m_Slots.Count; i++)
            {
                EquipmentProgramSlot slot = m_Slots[i];
                if (!slot.InitialEquipmentId.IsValid)
                    continue;
                EquipmentProgramItem item = RequireItem(slot.InitialEquipmentId);
                if (item.SlotId != slot.SlotId)
                    throw new InvalidDataException($"Equipment Slot '{slot.SlotId}' initial item targets another Slot.");
            }
            for (int i = 0; i < m_Routes.Count; i++)
            {
                if (!m_SlotById.ContainsKey(m_Routes[i].OwnerSlotId))
                    throw new InvalidDataException($"Equipment Route '{m_Routes[i].RouteId}' owner Slot is absent.");
            }
            for (int i = 0; i < m_LocalStates.Count; i++)
                if (!m_FeatureById.ContainsKey(m_LocalStates[i].FeatureId))
                    throw new InvalidDataException($"Equipment local state '{m_LocalStates[i].FeatureId}/{m_LocalStates[i].StateId}' feature is absent.");
            for (int i = 0; i < m_RouteImplementations.Count; i++)
            {
                EquipmentProgramRouteImplementation implementation = m_RouteImplementations[i];
                if (!m_FeatureById.ContainsKey(implementation.FeatureId) || !m_RouteById.ContainsKey(implementation.RouteId))
                    throw new InvalidDataException($"Equipment Route implementation '{implementation.FeatureId}/{implementation.RouteId}' has a dangling Feature or Route.");
                if (m_RouteById[implementation.RouteId].OwnerSlotId.IsValid &&
                    !m_SlotById.ContainsKey(m_RouteById[implementation.RouteId].OwnerSlotId))
                    throw new InvalidDataException($"Equipment Route implementation '{implementation.FeatureId}/{implementation.RouteId}' has a dangling owner Slot.");
                for (int parameterIndex = 0; parameterIndex < implementation.RequiredParameterIds.Count; parameterIndex++)
                {
                    EquipmentParameterId parameterId = implementation.RequiredParameterIds[parameterIndex];
                    if (!m_Parameters.Any(value => value.FeatureId == implementation.FeatureId && value.ParameterId == parameterId))
                        throw new InvalidDataException($"Equipment Route implementation '{implementation.FeatureId}/{implementation.RouteId}' requires unknown Parameter '{parameterId}'.");
                    for (int itemIndex = 0; itemIndex < m_Items.Count; itemIndex++)
                    {
                        EquipmentProgramItem item = m_Items[itemIndex];
                        if (item.FeatureId == implementation.FeatureId && !m_ParameterByKey.ContainsKey((item.EquipmentId, parameterId)))
                            throw new InvalidDataException($"Equipment Route implementation '{implementation.FeatureId}/{implementation.RouteId}' requires Parameter '{parameterId}' on Equipment '{item.EquipmentId}'.");
                    }
                }
            }
        }

        internal void ValidateProducerBindings(IReadOnlyList<ProgramProducer> producers)
        {
            if (producers == null)
                throw new ArgumentNullException(nameof(producers));
            var identities = new HashSet<string>(producers.Select(value => value.Identity), StringComparer.Ordinal);
            for (int i = 0; i < m_RouteImplementations.Count; i++)
                for (int producerIndex = 0; producerIndex < m_RouteImplementations[i].RequiredProducerIds.Count; producerIndex++)
                    if (!identities.Contains(m_RouteImplementations[i].RequiredProducerIds[producerIndex]))
                        throw new InvalidDataException($"Equipment Route implementation '{m_RouteImplementations[i].FeatureId}/{m_RouteImplementations[i].RouteId}' requires unknown Producer '{m_RouteImplementations[i].RequiredProducerIds[producerIndex]}'.");
        }

        StableHash ComputeHash()
        {
            using var writer = new CanonicalWriter();
            writer.WriteString("equipment-program-layout/v3");
            writer.WriteBoolean(CapabilityEnabled);
            writer.WriteInt32(m_Slots.Count);
            for (int i = 0; i < m_Slots.Count; i++)
            {
                writer.WriteString(m_Slots[i].SlotId.Value);
                writer.WriteByte((byte)m_Slots[i].Requirement);
                writer.WriteString(m_Slots[i].InitialEquipmentId.Value);
            }
            writer.WriteInt32(m_Features.Count);
            for (int i = 0; i < m_Features.Count; i++)
            {
                EquipmentProgramFeature feature = m_Features[i];
                writer.WriteString(feature.FeatureId.Value);
                writer.WriteUInt64(feature.Revision.Value);
                writer.WriteString(feature.CodeBindingId);
                writer.WriteUInt64((ulong)feature.RequiredWorldCapabilities);
                writer.WriteInt32(feature.GrantedTags.Count);
                for (int tag = 0; tag < feature.GrantedTags.Count; tag++) writer.WriteString(feature.GrantedTags[tag]);
                writer.WriteInt32(feature.PassiveEffects.Count);
                for (int effect = 0; effect < feature.PassiveEffects.Count; effect++) writer.WriteString(feature.PassiveEffects[effect]);
            }
            writer.WriteInt32(m_Items.Count);
            for (int i = 0; i < m_Items.Count; i++)
            {
                writer.WriteString(m_Items[i].EquipmentId.Value);
                writer.WriteString(m_Items[i].SlotId.Value);
                writer.WriteString(m_Items[i].FeatureId.Value);
                writer.WriteString(m_Items[i].VisualBindingId.Value);
            }
            writer.WriteInt32(m_Routes.Count);
            for (int i = 0; i < m_Routes.Count; i++)
            {
                writer.WriteString(m_Routes[i].RouteId.Value);
                writer.WriteString(m_Routes[i].OwnerSlotId.Value);
                writer.WriteString(m_Routes[i].InputRequestId);
                writer.WriteByte((byte)m_Routes[i].RequestConsumption);
                writer.WriteByte((byte)m_Routes[i].MissingImplementation);
            }
            writer.WriteInt32(m_RouteImplementations.Count);
            for (int i = 0; i < m_RouteImplementations.Count; i++)
            {
                writer.WriteString(m_RouteImplementations[i].FeatureId.Value);
                writer.WriteString(m_RouteImplementations[i].RouteId.Value);
                writer.WriteString(m_RouteImplementations[i].AbilityId.Value);
                writer.WriteInt32(m_RouteImplementations[i].RequiredParameterIds.Count);
                for (int parameter = 0; parameter < m_RouteImplementations[i].RequiredParameterIds.Count; parameter++)
                    writer.WriteString(m_RouteImplementations[i].RequiredParameterIds[parameter].Value);
                writer.WriteInt32(m_RouteImplementations[i].RequiredProducerIds.Count);
                for (int producer = 0; producer < m_RouteImplementations[i].RequiredProducerIds.Count; producer++)
                    writer.WriteString(m_RouteImplementations[i].RequiredProducerIds[producer]);
            }
            writer.WriteInt32(m_Parameters.Count);
            for (int i = 0; i < m_Parameters.Count; i++)
            {
                writer.WriteString(m_Parameters[i].EquipmentId.Value);
                writer.WriteString(m_Parameters[i].FeatureId.Value);
                writer.WriteString(m_Parameters[i].ParameterId.Value);
                writer.WriteByte((byte)m_Parameters[i].ValueKind);
                EquipmentRuntimeParameterValue value = m_Parameters[i].Value;
                writer.WriteBoolean(value.Boolean);
                writer.WriteInt32(value.Int32);
                writer.WriteUInt64(value.UInt64);
                writer.WriteDouble(value.X);
                writer.WriteDouble(value.Y);
                writer.WriteDouble(value.Z);
                writer.WriteString(value.Identity);
            }
            writer.WriteInt32(m_LocalStates.Count);
            for (int i = 0; i < m_LocalStates.Count; i++)
            {
                writer.WriteString(m_LocalStates[i].FeatureId.Value);
                writer.WriteString(m_LocalStates[i].StateId.Value);
                writer.WriteByte((byte)m_LocalStates[i].ValueKind);
                WriteStateValue(writer, m_LocalStates[i].DefaultValue);
            }
            writer.WriteInt32(m_OperationBindings.Count);
            for (int i = 0; i < m_OperationBindings.Count; i++)
            {
                EquipmentProgramOperationBinding binding = m_OperationBindings[i];
                writer.WriteInt32(binding.Operation.Value);
                writer.WriteString(binding.SlotId.Value);
                writer.WriteString(binding.RouteId.Value);
                writer.WriteString(binding.EquipmentId.Value);
                writer.WriteString(binding.ParameterId.Value);
            }
            return writer.ComputeHash();
        }

        static void WriteStateValue(CanonicalWriter writer, EquipmentRuntimeStateValue value)
        {
            writer.WriteBoolean(value.Boolean);
            writer.WriteInt32(value.Int32);
            writer.WriteUInt64(value.UInt64);
            writer.WriteDouble(value.X);
            writer.WriteDouble(value.Y);
            writer.WriteDouble(value.Z);
            writer.WriteString(value.Identity);
        }

        static ReadOnlyCollection<T> Canonical<T>(IEnumerable<T> source, Func<T, string> identity, string label)
        {
            T[] values = (source ?? Array.Empty<T>()).OrderBy(identity, StringComparer.Ordinal).ToArray();
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == null)
                    throw new InvalidDataException($"{label} contains null.");
                if (i > 0 && string.Equals(identity(values[i - 1]), identity(values[i]), StringComparison.Ordinal))
                    throw new InvalidDataException($"{label} identity '{identity(values[i])}' is duplicated.");
            }
            return Array.AsReadOnly(values);
        }
    }

    public enum PendingEquipmentChangeState : byte
    {
        Pending = 1,
        Committed = 2,
        Cancelled = 3
    }

    public readonly struct PendingEquipmentChange
    {
        public PendingEquipmentChange(
            EquipmentChangeId changeId,
            EquipmentSlotId slotId,
            EquipmentId fromEquipmentId,
            EquipmentId toEquipmentId,
            ulong sourceActionInstanceId,
            ulong beginTick,
            ulong resolvedTick,
            PendingEquipmentChangeState state)
        {
            if (!changeId.IsValid || !slotId.IsValid || beginTick == 0 || !IsValidState(state))
                throw new ArgumentException("Pending Equipment Change is invalid.");
            if (state == PendingEquipmentChangeState.Pending && resolvedTick != 0 ||
                state != PendingEquipmentChangeState.Pending && resolvedTick < beginTick)
            {
                throw new ArgumentException("Equipment Change resolution tick is inconsistent.");
            }
            ChangeId = changeId;
            SlotId = slotId;
            FromEquipmentId = fromEquipmentId;
            ToEquipmentId = toEquipmentId;
            SourceActionInstanceId = sourceActionInstanceId;
            BeginTick = beginTick;
            ResolvedTick = resolvedTick;
            State = state;
        }

        public EquipmentChangeId ChangeId { get; }
        public EquipmentSlotId SlotId { get; }
        public EquipmentId FromEquipmentId { get; }
        public EquipmentId ToEquipmentId { get; }
        public ulong SourceActionInstanceId { get; }
        public ulong BeginTick { get; }
        public ulong ResolvedTick { get; }
        public PendingEquipmentChangeState State { get; }
        internal static bool IsValidState(PendingEquipmentChangeState state) =>
            state is PendingEquipmentChangeState.Pending or PendingEquipmentChangeState.Committed or PendingEquipmentChangeState.Cancelled;
        public bool IsValid => ChangeId.IsValid;
        public bool IsPending => IsValid && State == PendingEquipmentChangeState.Pending;

        public PendingEquipmentChange Resolve(PendingEquipmentChangeState state, ulong resolvedTick)
        {
            if (!IsPending || state == PendingEquipmentChangeState.Pending)
                throw new InvalidOperationException("Only an active Equipment Change can be resolved.");
            return new PendingEquipmentChange(
                ChangeId,
                SlotId,
                FromEquipmentId,
                ToEquipmentId,
                SourceActionInstanceId,
                BeginTick,
                resolvedTick,
                state);
        }
    }

    public readonly struct EquipmentSlotState
    {
        readonly ulong[] m_PassiveEffectHandles;

        public EquipmentSlotState(
            EquipmentSlotId slotId,
            EquipmentId equipmentId,
            EquipmentFeatureId featureId,
            EquipmentFeatureRevision featureRevision,
            EquipmentVisualBindingId visualBindingId,
            ulong revision,
            ulong generation,
            bool contributionsInstalled,
            string tagSource,
            ulong[] ownedPassiveEffectHandles)
        {
            if (!slotId.IsValid || revision == 0 || generation == 0)
                throw new ArgumentException("Equipment Slot state is invalid.");
            bool empty = !equipmentId.IsValid;
            bool hasAnyContributionIdentity = featureId.IsValid || featureRevision.IsValid || visualBindingId.IsValid;
            bool hasCompleteContributionIdentity = featureId.IsValid && featureRevision.IsValid && visualBindingId.IsValid;
            if (empty && hasAnyContributionIdentity || !empty && !hasCompleteContributionIdentity || empty && contributionsInstalled)
            {
                throw new ArgumentException("Equipment Slot state contribution is inconsistent.");
            }
            SlotId = slotId;
            EquipmentId = equipmentId;
            FeatureId = featureId;
            FeatureRevision = featureRevision;
            VisualBindingId = visualBindingId;
            Revision = revision;
            Generation = generation;
            ContributionsInstalled = contributionsInstalled;
            TagSource = tagSource ?? string.Empty;
            m_PassiveEffectHandles = ownedPassiveEffectHandles ?? Array.Empty<ulong>();
            for (int i = 0; i < m_PassiveEffectHandles.Length; i++)
            {
                if (m_PassiveEffectHandles[i] == 0)
                    throw new ArgumentException("Equipment Slot passive Effect handle is zero.", nameof(ownedPassiveEffectHandles));
                for (int prior = 0; prior < i; prior++)
                {
                    if (m_PassiveEffectHandles[prior] == m_PassiveEffectHandles[i])
                        throw new ArgumentException("Equipment Slot passive Effect handles are duplicated.", nameof(ownedPassiveEffectHandles));
                }
            }
            if (empty && (TagSource.Length != 0 || m_PassiveEffectHandles.Length != 0))
                throw new ArgumentException("Empty Equipment Slot cannot own contributions.");
        }

        public EquipmentSlotId SlotId { get; }
        public EquipmentId EquipmentId { get; }
        public EquipmentFeatureId FeatureId { get; }
        public EquipmentFeatureRevision FeatureRevision { get; }
        public EquipmentVisualBindingId VisualBindingId { get; }
        public ulong Revision { get; }
        public ulong Generation { get; }
        public bool ContributionsInstalled { get; }
        public string TagSource { get; }
        public IReadOnlyList<ulong> PassiveEffectHandles => m_PassiveEffectHandles ?? Array.Empty<ulong>();
        public bool IsEquipped => EquipmentId.IsValid;
        public EquipmentActionContext ActionContext(EquipmentActionRouteId routeId) => IsEquipped
            ? new EquipmentActionContext(SlotId, EquipmentId, FeatureId, Revision, routeId)
            : default;
        public EquipmentVisualSelection CreateVisualSelection(ActorId actorId, ulong sourceTick) =>
            new EquipmentVisualSelection(actorId, SlotId, EquipmentId, VisualBindingId, Revision, sourceTick);

        public bool Equals(EquipmentSlotState other) =>
            SlotId == other.SlotId &&
            EquipmentId == other.EquipmentId &&
            FeatureId == other.FeatureId &&
            FeatureRevision == other.FeatureRevision &&
            VisualBindingId == other.VisualBindingId &&
            Revision == other.Revision &&
            Generation == other.Generation &&
            ContributionsInstalled == other.ContributionsInstalled &&
            string.Equals(TagSource, other.TagSource, StringComparison.Ordinal) &&
            SameHandles(PassiveEffectHandles, other.PassiveEffectHandles);

        static bool SameHandles(IReadOnlyList<ulong> left, IReadOnlyList<ulong> right)
        {
            if (left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                    return false;
            }
            return true;
        }
    }

    public sealed class EquipmentStateAggregate
    {
        readonly EquipmentSlotState[] m_Slots;
        readonly EquipmentLocalStateValue[] m_LocalStates;

        static readonly Comparison<EquipmentSlotState> s_CompareSlots =
            (left, right) => string.CompareOrdinal(left.SlotId.Value, right.SlotId.Value);
        static readonly Comparison<EquipmentLocalStateValue> s_CompareLocalStates =
            (left, right) =>
                CompareJoinedIdentity(
                    left.FeatureId.Value,
                    left.StateId.Value,
                    right.FeatureId.Value,
                    right.StateId.Value);

        public EquipmentStateAggregate(
            StableHash catalogHash,
            IEnumerable<EquipmentSlotState> slots,
            IEnumerable<EquipmentLocalStateValue> localStates,
            PendingEquipmentChange pendingChange,
            PendingEquipmentChange lastResolvedChange)
        {
            if (!catalogHash.IsValid)
                throw new ArgumentException("Equipment state catalog hash is invalid.", nameof(catalogHash));
            CatalogHash = catalogHash;
            EquipmentSlotState[] stable = (slots ?? Array.Empty<EquipmentSlotState>()).ToArray();
            Array.Sort(stable, s_CompareSlots);
            for (int i = 1; i < stable.Length; i++)
            {
                if (stable[i - 1].SlotId == stable[i].SlotId)
                    throw new InvalidDataException($"Equipment state Slot '{stable[i].SlotId}' is duplicated.");
            }
            m_Slots = stable;
            EquipmentLocalStateValue[] localStateValues = (localStates ?? Array.Empty<EquipmentLocalStateValue>()).ToArray();
            Array.Sort(localStateValues, s_CompareLocalStates);
            for (int i = 0; i < localStateValues.Length; i++)
            {
                if (localStateValues[i] == null)
                    throw new InvalidDataException("Equipment aggregate contains a missing local state.");
                if (i > 0 && localStateValues[i - 1].FeatureId == localStateValues[i].FeatureId &&
                    localStateValues[i - 1].StateId == localStateValues[i].StateId)
                {
                    throw new InvalidDataException($"Equipment local state '{localStateValues[i].FeatureId}/{localStateValues[i].StateId}' is duplicated.");
                }
            }
            m_LocalStates = localStateValues;
            if (pendingChange.IsValid && !pendingChange.IsPending)
                throw new ArgumentException("Equipment aggregate pending record is already resolved.", nameof(pendingChange));
            if (lastResolvedChange.IsValid && lastResolvedChange.IsPending)
                throw new ArgumentException("Equipment aggregate resolved record is still pending.", nameof(lastResolvedChange));
            PendingChange = pendingChange;
            LastResolvedChange = lastResolvedChange;
        }

        EquipmentStateAggregate(
            StableHash catalogHash,
            EquipmentSlotState[] slots,
            EquipmentLocalStateValue[] localStates,
            PendingEquipmentChange pendingChange,
            PendingEquipmentChange lastResolvedChange)
        {
            if (!catalogHash.IsValid)
                throw new ArgumentException("Equipment state catalog hash is invalid.", nameof(catalogHash));
            CatalogHash = catalogHash;
            m_Slots = slots;
            m_LocalStates = localStates;
            if (pendingChange.IsValid && !pendingChange.IsPending)
                throw new ArgumentException("Equipment aggregate pending record is already resolved.", nameof(pendingChange));
            if (lastResolvedChange.IsValid && lastResolvedChange.IsPending)
                throw new ArgumentException("Equipment aggregate resolved record is still pending.", nameof(lastResolvedChange));
            PendingChange = pendingChange;
            LastResolvedChange = lastResolvedChange;
        }

        public StableHash CatalogHash { get; }
        public IReadOnlyList<EquipmentSlotState> Slots => m_Slots;
        public IReadOnlyList<EquipmentLocalStateValue> LocalStates => m_LocalStates;
        public PendingEquipmentChange PendingChange { get; }
        public PendingEquipmentChange LastResolvedChange { get; }

        internal static EquipmentStateAggregate AdoptPrepared(
            StableHash catalogHash,
            EquipmentSlotState[] slots,
            EquipmentLocalStateValue[] localStates,
            PendingEquipmentChange pendingChange,
            PendingEquipmentChange lastResolvedChange)
        {
            if (!catalogHash.IsValid)
                throw new ArgumentException("Equipment state catalog hash is invalid.", nameof(catalogHash));
            Array.Sort(slots, s_CompareSlots);
            for (int i = 1; i < slots.Length; i++)
            {
                if (slots[i - 1].SlotId == slots[i].SlotId)
                    throw new InvalidDataException($"Equipment state Slot '{slots[i].SlotId}' is duplicated.");
            }
            Array.Sort(localStates, s_CompareLocalStates);
            for (int i = 0; i < localStates.Length; i++)
            {
                if (localStates[i] == null)
                    throw new InvalidDataException("Equipment aggregate contains a missing local state.");
                if (i > 0 && localStates[i - 1].FeatureId == localStates[i].FeatureId &&
                    localStates[i - 1].StateId == localStates[i].StateId)
                {
                    throw new InvalidDataException($"Equipment local state '{localStates[i].FeatureId}/{localStates[i].StateId}' is duplicated.");
                }
            }
            return new EquipmentStateAggregate(
                catalogHash,
                slots,
                localStates,
                pendingChange,
                lastResolvedChange);
        }

        static int CompareJoinedIdentity(
            string leftFeature,
            string leftState,
            string rightFeature,
            string rightState)
        {
            int leftLength = leftFeature.Length + 1 + leftState.Length;
            int rightLength = rightFeature.Length + 1 + rightState.Length;
            int length = Math.Max(leftLength, rightLength);
            for (int i = 0; i < length; i++)
            {
                char leftValue = i < leftFeature.Length
                    ? leftFeature[i]
                    : i == leftFeature.Length
                        ? ':'
                        : leftState[i - leftFeature.Length - 1];
                char rightValue = i < rightFeature.Length
                    ? rightFeature[i]
                    : i == rightFeature.Length
                        ? ':'
                        : rightState[i - rightFeature.Length - 1];
                if (leftValue != rightValue)
                    return leftValue < rightValue ? -1 : 1;
            }
            return 0;
        }

        public static EquipmentStateAggregate CreateInitial(EquipmentProgramLayout layout)
        {
            if (layout == null || !layout.CapabilityEnabled)
                throw new ArgumentException("Enabled Equipment Program layout is required.", nameof(layout));
            var slots = new EquipmentSlotState[layout.Slots.Count];
            for (int i = 0; i < slots.Length; i++)
            {
                EquipmentProgramSlot slot = layout.Slots[i];
                if (!slot.InitialEquipmentId.IsValid)
                {
                    slots[i] = new EquipmentSlotState(slot.SlotId, default, default, default, default, 1, 1, false, string.Empty, Array.Empty<ulong>());
                    continue;
                }
                EquipmentProgramItem item = layout.RequireItem(slot.InitialEquipmentId);
                EquipmentProgramFeature feature = layout.RequireFeature(item.FeatureId);
                slots[i] = new EquipmentSlotState(slot.SlotId, item.EquipmentId, item.FeatureId, feature.Revision, item.VisualBindingId, 1, 1, false, string.Empty, Array.Empty<ulong>());
            }
            var localStates = new EquipmentLocalStateValue[layout.LocalStates.Count];
            for (int i = 0; i < localStates.Length; i++)
            {
                EquipmentProgramLocalState localState = layout.LocalStates[i];
                localStates[i] = new EquipmentLocalStateValue(
                    localState.FeatureId,
                    localState.StateId,
                    localState.DefaultValue);
            }
            return AdoptPrepared(layout.CatalogHash, slots, localStates, default, default);
        }

        public EquipmentSlotState RequireSlot(EquipmentSlotId slotId)
        {
            for (int i = 0; i < m_Slots.Length; i++)
            {
                if (m_Slots[i].SlotId == slotId)
                    return m_Slots[i];
            }
            throw new InvalidOperationException($"Equipment state Slot '{slotId}' is absent.");
        }

        public EquipmentRuntimeStateValue RequireLocalState(EquipmentFeatureId featureId, EquipmentLocalStateId stateId)
        {
            for (int i = 0; i < m_LocalStates.Length; i++)
                if (m_LocalStates[i].FeatureId == featureId && m_LocalStates[i].StateId == stateId)
                    return m_LocalStates[i].Value;
            throw new InvalidOperationException($"Equipment local state '{featureId}/{stateId}' is absent.");
        }

        public EquipmentStateAggregate WithSlot(EquipmentSlotState slot)
        {
            for (int i = 0; i < m_Slots.Length; i++)
            {
                if (m_Slots[i].SlotId != slot.SlotId)
                    continue;
                if (m_Slots[i].Equals(slot))
                    return this;
                var values = new EquipmentSlotState[m_Slots.Length];
                Array.Copy(m_Slots, values, m_Slots.Length);
                values[i] = slot;
                return new EquipmentStateAggregate(
                    CatalogHash,
                    values,
                    m_LocalStates,
                    PendingChange,
                    LastResolvedChange);
            }
            throw new InvalidOperationException($"Equipment state Slot '{slot.SlotId}' is absent.");
        }

        public EquipmentStateAggregate WithLocalState(
            EquipmentFeatureId featureId,
            EquipmentLocalStateId stateId,
            EquipmentRuntimeStateValue value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            for (int i = 0; i < m_LocalStates.Length; i++)
            {
                if (m_LocalStates[i].FeatureId != featureId || m_LocalStates[i].StateId != stateId)
                    continue;
                if (m_LocalStates[i].Value.Equals(value))
                    return this;
                if (m_LocalStates[i].Value.Kind != value.Kind)
                    throw new InvalidOperationException($"Equipment local state '{featureId}/{stateId}' value kind changed.");
                var values = new EquipmentLocalStateValue[m_LocalStates.Length];
                Array.Copy(m_LocalStates, values, m_LocalStates.Length);
                values[i] = new EquipmentLocalStateValue(featureId, stateId, value);
                return new EquipmentStateAggregate(
                    CatalogHash,
                    m_Slots,
                    values,
                    PendingChange,
                    LastResolvedChange);
            }
            throw new InvalidOperationException($"Equipment local state '{featureId}/{stateId}' is absent.");
        }

        public EquipmentStateAggregate WithPending(PendingEquipmentChange pending) =>
            new EquipmentStateAggregate(
                CatalogHash,
                m_Slots,
                m_LocalStates,
                pending,
                LastResolvedChange);

        public EquipmentStateAggregate ResolvePending(PendingEquipmentChangeState state, ulong resolvedTick)
        {
            if (!PendingChange.IsPending)
                throw new InvalidOperationException("Equipment aggregate has no active pending change.");
            return new EquipmentStateAggregate(
                CatalogHash,
                m_Slots,
                m_LocalStates,
                default,
                PendingChange.Resolve(state, resolvedTick));
        }

        public EquipmentStateAggregate WithSlotAndResolvedPending(
            EquipmentSlotState slot,
            PendingEquipmentChangeState state,
            ulong resolvedTick)
        {
            if (!PendingChange.IsPending)
                throw new InvalidOperationException("Equipment aggregate has no active pending change.");
            if (slot.SlotId != PendingChange.SlotId)
                throw new InvalidOperationException($"Equipment pending change targets '{PendingChange.SlotId.Value}', not '{slot.SlotId.Value}'.");
            for (int i = 0; i < m_Slots.Length; i++)
            {
                if (m_Slots[i].SlotId != slot.SlotId)
                    continue;
                EquipmentSlotState[] values = m_Slots;
                if (!m_Slots[i].Equals(slot))
                {
                    values = new EquipmentSlotState[m_Slots.Length];
                    Array.Copy(m_Slots, values, m_Slots.Length);
                    values[i] = slot;
                }
                return new EquipmentStateAggregate(
                    CatalogHash,
                    values,
                    m_LocalStates,
                    default,
                    PendingChange.Resolve(state, resolvedTick));
            }
            throw new InvalidOperationException($"Equipment state Slot '{slot.SlotId}' is absent.");
        }
    }

    public static class EquipmentStateAggregateCodec
    {
        public static void Write(CanonicalWriter writer, EquipmentStateAggregate state)
        {
            if (writer == null || state == null)
                throw new ArgumentNullException();
            writer.WriteString(state.CatalogHash.ToString());
            writer.WriteInt32(state.Slots.Count);
            for (int i = 0; i < state.Slots.Count; i++)
            {
                EquipmentSlotState slot = state.Slots[i];
                writer.WriteString(slot.SlotId.Value);
                writer.WriteString(slot.EquipmentId.Value);
                writer.WriteString(slot.FeatureId.Value);
                writer.WriteUInt64(slot.FeatureRevision.Value);
                writer.WriteString(slot.VisualBindingId.Value);
                writer.WriteUInt64(slot.Revision);
                writer.WriteUInt64(slot.Generation);
                writer.WriteBoolean(slot.ContributionsInstalled);
                writer.WriteString(slot.TagSource);
                writer.WriteInt32(slot.PassiveEffectHandles.Count);
                for (int handle = 0; handle < slot.PassiveEffectHandles.Count; handle++)
                    writer.WriteUInt64(slot.PassiveEffectHandles[handle]);
            }
            writer.WriteInt32(state.LocalStates.Count);
            for (int i = 0; i < state.LocalStates.Count; i++)
            {
                EquipmentLocalStateValue localState = state.LocalStates[i];
                writer.WriteString(localState.FeatureId.Value);
                writer.WriteString(localState.StateId.Value);
                writer.WriteByte((byte)localState.Value.Kind);
                WriteStateValue(writer, localState.Value);
            }
            WriteChange(writer, state.PendingChange);
            WriteChange(writer, state.LastResolvedChange);
        }

        static void WriteChange(CanonicalWriter writer, PendingEquipmentChange pending)
        {
            writer.WriteBoolean(pending.IsValid);
            if (!pending.IsValid)
                return;
            writer.WriteUInt64(pending.ChangeId.Value);
            writer.WriteString(pending.SlotId.Value);
            writer.WriteString(pending.FromEquipmentId.Value);
            writer.WriteString(pending.ToEquipmentId.Value);
            writer.WriteUInt64(pending.SourceActionInstanceId);
            writer.WriteUInt64(pending.BeginTick);
            writer.WriteUInt64(pending.ResolvedTick);
            writer.WriteByte((byte)pending.State);
        }

        public static EquipmentStateAggregate Read(CanonicalReader reader, EquipmentProgramLayout layout)
        {
            if (reader == null || layout == null)
                throw new ArgumentNullException();
            StableHash hash = new StableHash(reader.ReadString());
            if (!hash.Equals(layout.CatalogHash))
                throw new InvalidDataException("Equipment state catalog identity does not match Program layout.");
            int count = reader.ReadInt32();
            if (count != layout.Slots.Count)
                throw new InvalidDataException("Equipment state Slot count does not match Program layout.");
            var slots = new EquipmentSlotState[count];
            for (int i = 0; i < count; i++)
            {
                var slotId = new EquipmentSlotId(reader.ReadString());
                var equipmentId = ReadOptionalEquipment(reader.ReadString());
                var featureId = ReadOptionalFeature(reader.ReadString());
                ulong featureRevisionValue = reader.ReadUInt64();
                var featureRevision = featureRevisionValue == 0 ? default : new EquipmentFeatureRevision(featureRevisionValue);
                var visualBindingId = ReadOptionalVisual(reader.ReadString());
                ulong revision = reader.ReadUInt64();
                ulong generation = reader.ReadUInt64();
                bool installed = reader.ReadBoolean();
                string tagSource = reader.ReadString();
                int handleCount = reader.ReadInt32();
                if (handleCount < 0)
                    throw new InvalidDataException("Equipment passive Effect handle count is invalid.");
                var handles = new ulong[handleCount];
                for (int handle = 0; handle < handles.Length; handle++) handles[handle] = reader.ReadUInt64();
                layout.RequireSlot(slotId);
                if (equipmentId.IsValid)
                {
                    EquipmentProgramItem item = layout.RequireItem(equipmentId);
                    EquipmentProgramFeature feature = layout.RequireFeature(featureId);
                    if (item.SlotId != slotId || item.FeatureId != featureId || item.VisualBindingId != visualBindingId || feature.Revision != featureRevision)
                        throw new InvalidDataException($"Equipment state Slot '{slotId}' contribution does not match Program catalog.");
                }
                slots[i] = new EquipmentSlotState(slotId, equipmentId, featureId, featureRevision, visualBindingId, revision, generation, installed, tagSource, handles);
            }
            int localStateCount = reader.ReadInt32();
            if (localStateCount != layout.LocalStates.Count)
                throw new InvalidDataException("Equipment state local state count does not match the runtime layout.");
            var localStates = new EquipmentLocalStateValue[localStateCount];
            for (int i = 0; i < localStates.Length; i++)
            {
                EquipmentFeatureId featureId = new EquipmentFeatureId(reader.ReadString());
                EquipmentLocalStateId stateId = new EquipmentLocalStateId(reader.ReadString());
                EquipmentRuntimeStateValueKind valueKind = ReadStateValueKind(reader.ReadByte());
                EquipmentProgramLocalState definition = layout.RequireLocalState(featureId, stateId);
                if (definition.ValueKind != valueKind)
                    throw new InvalidDataException($"Equipment local state '{featureId}/{stateId}' value kind does not match the runtime layout.");
                localStates[i] = new EquipmentLocalStateValue(featureId, stateId, ReadStateValue(reader, valueKind));
            }
            PendingEquipmentChange pending = ReadChange(reader, layout);
            PendingEquipmentChange resolved = ReadChange(reader, layout);
            return EquipmentStateAggregate.AdoptPrepared(hash, slots, localStates, pending, resolved);
        }

        static void WriteStateValue(CanonicalWriter writer, EquipmentRuntimeStateValue value)
        {
            writer.WriteBoolean(value.Boolean);
            writer.WriteInt32(value.Int32);
            writer.WriteUInt64(value.UInt64);
            writer.WriteDouble(value.X);
            writer.WriteDouble(value.Y);
            writer.WriteDouble(value.Z);
            writer.WriteString(value.Identity);
        }

        static EquipmentRuntimeStateValue ReadStateValue(CanonicalReader reader, EquipmentRuntimeStateValueKind kind) =>
            new EquipmentRuntimeStateValue(
                kind,
                reader.ReadBoolean(),
                reader.ReadInt32(),
                reader.ReadUInt64(),
                reader.ReadDouble(),
                reader.ReadDouble(),
                reader.ReadDouble(),
                reader.ReadString());

        static PendingEquipmentChange ReadChange(CanonicalReader reader, EquipmentProgramLayout layout)
        {
            if (!reader.ReadBoolean())
                return default;
            var change = new PendingEquipmentChange(
                new EquipmentChangeId(reader.ReadUInt64()),
                new EquipmentSlotId(reader.ReadString()),
                ReadOptionalEquipment(reader.ReadString()),
                ReadOptionalEquipment(reader.ReadString()),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                ReadChangeState(reader.ReadByte()));
            layout.RequireSlot(change.SlotId);
            if (change.FromEquipmentId.IsValid) layout.RequireItem(change.FromEquipmentId);
            if (change.ToEquipmentId.IsValid) layout.RequireItem(change.ToEquipmentId);
            return change;
        }

        static EquipmentId ReadOptionalEquipment(string value) => string.IsNullOrEmpty(value) ? default : new EquipmentId(value);
        static EquipmentFeatureId ReadOptionalFeature(string value) => string.IsNullOrEmpty(value) ? default : new EquipmentFeatureId(value);
        static EquipmentVisualBindingId ReadOptionalVisual(string value) => string.IsNullOrEmpty(value) ? default : new EquipmentVisualBindingId(value);
        static EquipmentRuntimeStateValueKind ReadStateValueKind(byte value)
        {
            var result = (EquipmentRuntimeStateValueKind)value;
            return EquipmentRuntimeStateValue.IsValidKind(result) ? result :
                throw new InvalidDataException($"Equipment state enum '{nameof(EquipmentRuntimeStateValueKind)}' value '{value}' is invalid.");
        }

        static PendingEquipmentChangeState ReadChangeState(byte value)
        {
            var result = (PendingEquipmentChangeState)value;
            return PendingEquipmentChange.IsValidState(result)
                ? result
                : throw new InvalidDataException($"Equipment state enum '{nameof(PendingEquipmentChangeState)}' value '{value}' is invalid.");
        }
    }

    public static class EquipmentActionContextCodec
    {
        public static void Write(CanonicalWriter writer, EquipmentActionContext context)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            writer.WriteBoolean(context.IsValid);
            if (!context.IsValid)
                return;
            writer.WriteString(context.SlotId.Value);
            writer.WriteString(context.EquipmentId.Value);
            writer.WriteString(context.FeatureId.Value);
            writer.WriteUInt64(context.EquipmentRevision);
            writer.WriteString(context.RouteId.Value);
        }

        public static EquipmentActionContext Read(CanonicalReader reader, EquipmentProgramLayout layout)
        {
            if (reader == null || layout == null)
                throw new ArgumentNullException();
            if (!reader.ReadBoolean())
                return default;
            var context = new EquipmentActionContext(
                new EquipmentSlotId(reader.ReadString()),
                new EquipmentId(reader.ReadString()),
                new EquipmentFeatureId(reader.ReadString()),
                reader.ReadUInt64(),
                new EquipmentActionRouteId(reader.ReadString()));
            EquipmentProgramItem item = layout.RequireItem(context.EquipmentId);
            EquipmentProgramFeature feature = layout.RequireFeature(context.FeatureId);
            EquipmentProgramRoute route = layout.RequireRoute(context.RouteId);
            if (!layout.TryGetRouteImplementation(context.FeatureId, context.RouteId, out _))
                throw new InvalidDataException($"Equipment Action Context '{context}' has no Feature route implementation.");
            if (item.SlotId != context.SlotId || item.FeatureId != context.FeatureId ||
                route.OwnerSlotId != context.SlotId || feature.FeatureId != context.FeatureId)
            {
                throw new InvalidDataException($"Equipment Action Context '{context}' does not match Program layout.");
            }
            return context;
        }
    }
}
