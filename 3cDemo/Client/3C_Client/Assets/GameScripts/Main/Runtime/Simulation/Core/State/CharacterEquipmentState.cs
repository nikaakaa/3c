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
            IEnumerable<string> grantedTags,
            IEnumerable<string> passiveEffects,
            WorldCapability requiredWorldCapabilities,
            OperationHandle persistentEntry)
        {
            if (!featureId.IsValid || !revision.IsValid)
                throw new ArgumentException("Equipment Program Feature identity is invalid.");
            FeatureId = featureId;
            Revision = revision;
            m_GrantedTags = StableIdentities(grantedTags, "Equipment granted Tag");
            m_PassiveEffects = StableIdentities(passiveEffects, "Equipment passive Effect");
            RequiredWorldCapabilities = requiredWorldCapabilities;
            PersistentEntry = persistentEntry;
        }

        public EquipmentFeatureId FeatureId { get; }
        public EquipmentFeatureRevision Revision { get; }
        public IReadOnlyList<string> GrantedTags => m_GrantedTags;
        public IReadOnlyList<string> PassiveEffects => m_PassiveEffects;
        public WorldCapability RequiredWorldCapabilities { get; }
        public OperationHandle PersistentEntry { get; }

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
        public EquipmentProgramRouteImplementation(
            EquipmentFeatureId featureId,
            EquipmentActionRouteId routeId,
            string actionId,
            OperationHandle entryOperation)
        {
            if (!featureId.IsValid || !routeId.IsValid || string.IsNullOrEmpty(actionId) || !entryOperation.IsValid)
                throw new ArgumentException("Equipment Route implementation is invalid.");
            FeatureId = featureId;
            RouteId = routeId;
            ActionId = actionId;
            EntryOperation = entryOperation;
        }

        public EquipmentFeatureId FeatureId { get; }
        public EquipmentActionRouteId RouteId { get; }
        public string ActionId { get; }
        public OperationHandle EntryOperation { get; }
    }

    public sealed class EquipmentProgramParameter
    {
        public EquipmentProgramParameter(
            EquipmentId equipmentId,
            EquipmentFeatureId featureId,
            EquipmentParameterId parameterId,
            EquipmentParameterValueKind valueKind,
            int constantIndex)
        {
            if (!equipmentId.IsValid || !featureId.IsValid || !parameterId.IsValid ||
                !Enum.IsDefined(typeof(EquipmentParameterValueKind), valueKind) || constantIndex < 0)
            {
                throw new ArgumentException("Equipment Program Parameter is invalid.");
            }
            EquipmentId = equipmentId;
            FeatureId = featureId;
            ParameterId = parameterId;
            ValueKind = valueKind;
            ConstantIndex = constantIndex;
        }

        public EquipmentId EquipmentId { get; }
        public EquipmentFeatureId FeatureId { get; }
        public EquipmentParameterId ParameterId { get; }
        public EquipmentParameterValueKind ValueKind { get; }
        public int ConstantIndex { get; }
    }

    public sealed class EquipmentProgramLocalState
    {
        public EquipmentProgramLocalState(EquipmentFeatureId featureId, EquipmentLocalStateId stateId, int stateSlotIndex)
        {
            if (!featureId.IsValid || !stateId.IsValid || stateSlotIndex < 0)
                throw new ArgumentException("Equipment Program local state is invalid.");
            FeatureId = featureId;
            StateId = stateId;
            StateSlotIndex = stateSlotIndex;
        }

        public EquipmentFeatureId FeatureId { get; }
        public EquipmentLocalStateId StateId { get; }
        public int StateSlotIndex { get; }
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
        readonly Dictionary<EquipmentId, EquipmentProgramItem> m_ItemById;
        readonly Dictionary<EquipmentActionRouteId, EquipmentProgramRoute> m_RouteById;
        readonly Dictionary<(EquipmentFeatureId, EquipmentActionRouteId), EquipmentProgramRouteImplementation> m_RouteImplementationByKey;
        readonly Dictionary<(EquipmentId, EquipmentParameterId), EquipmentProgramParameter> m_ParameterByKey;
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
            m_ItemById = m_Items.ToDictionary(value => value.EquipmentId);
            m_RouteById = m_Routes.ToDictionary(value => value.RouteId);
            m_RouteImplementationByKey = m_RouteImplementations.ToDictionary(value => (value.FeatureId, value.RouteId));
            m_ParameterByKey = m_Parameters.ToDictionary(value => (value.EquipmentId, value.ParameterId));
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
        public EquipmentProgramItem RequireItem(EquipmentId equipmentId) =>
            m_ItemById.TryGetValue(equipmentId, out EquipmentProgramItem value) ? value : throw new InvalidOperationException($"Equipment '{equipmentId}' is absent from Program.");
        public EquipmentProgramRoute RequireRoute(EquipmentActionRouteId routeId) =>
            m_RouteById.TryGetValue(routeId, out EquipmentProgramRoute value) ? value : throw new InvalidOperationException($"Equipment Route '{routeId}' is absent from Program.");
        public bool TryGetRouteImplementation(EquipmentFeatureId featureId, EquipmentActionRouteId routeId, out EquipmentProgramRouteImplementation value) =>
            m_RouteImplementationByKey.TryGetValue((featureId, routeId), out value);
        public EquipmentProgramParameter RequireParameter(EquipmentId equipmentId, EquipmentParameterId parameterId) =>
            m_ParameterByKey.TryGetValue((equipmentId, parameterId), out EquipmentProgramParameter value) ? value : throw new InvalidOperationException($"Equipment Parameter '{equipmentId}/{parameterId}' is absent from Program.");
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
        }

        StableHash ComputeHash()
        {
            using var writer = new CanonicalWriter();
            writer.WriteString("equipment-program-layout/v1");
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
                writer.WriteUInt64((ulong)feature.RequiredWorldCapabilities);
                writer.WriteInt32(feature.PersistentEntry.IsValid ? feature.PersistentEntry.Value : -1);
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
                writer.WriteString(m_RouteImplementations[i].ActionId);
                writer.WriteInt32(m_RouteImplementations[i].EntryOperation.Value);
            }
            writer.WriteInt32(m_Parameters.Count);
            for (int i = 0; i < m_Parameters.Count; i++)
            {
                writer.WriteString(m_Parameters[i].EquipmentId.Value);
                writer.WriteString(m_Parameters[i].FeatureId.Value);
                writer.WriteString(m_Parameters[i].ParameterId.Value);
                writer.WriteByte((byte)m_Parameters[i].ValueKind);
                writer.WriteInt32(m_Parameters[i].ConstantIndex);
            }
            writer.WriteInt32(m_LocalStates.Count);
            for (int i = 0; i < m_LocalStates.Count; i++)
            {
                writer.WriteString(m_LocalStates[i].FeatureId.Value);
                writer.WriteString(m_LocalStates[i].StateId.Value);
                writer.WriteInt32(m_LocalStates[i].StateSlotIndex);
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
            if (!changeId.IsValid || !slotId.IsValid || beginTick == 0 || !Enum.IsDefined(typeof(PendingEquipmentChangeState), state))
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
            ulong hostGeneration,
            bool contributionsInstalled,
            string tagSource,
            IEnumerable<ulong> passiveEffectHandles)
        {
            if (!slotId.IsValid || revision == 0 || hostGeneration == 0)
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
            HostGeneration = hostGeneration;
            ContributionsInstalled = contributionsInstalled;
            TagSource = tagSource ?? string.Empty;
            m_PassiveEffectHandles = (passiveEffectHandles ?? Array.Empty<ulong>()).ToArray();
            if (m_PassiveEffectHandles.Any(value => value == 0) || m_PassiveEffectHandles.Distinct().Count() != m_PassiveEffectHandles.Length)
                throw new ArgumentException("Equipment Slot passive Effect handles are invalid.");
            if (empty && (TagSource.Length != 0 || m_PassiveEffectHandles.Length != 0))
                throw new ArgumentException("Empty Equipment Slot cannot own contributions.");
        }

        public EquipmentSlotId SlotId { get; }
        public EquipmentId EquipmentId { get; }
        public EquipmentFeatureId FeatureId { get; }
        public EquipmentFeatureRevision FeatureRevision { get; }
        public EquipmentVisualBindingId VisualBindingId { get; }
        public ulong Revision { get; }
        public ulong HostGeneration { get; }
        public bool ContributionsInstalled { get; }
        public string TagSource { get; }
        public IReadOnlyList<ulong> PassiveEffectHandles => m_PassiveEffectHandles ?? Array.Empty<ulong>();
        public bool IsEquipped => EquipmentId.IsValid;
        public EquipmentActionContext ActionContext(EquipmentActionRouteId routeId) => IsEquipped
            ? new EquipmentActionContext(SlotId, EquipmentId, FeatureId, Revision, routeId)
            : default;
        public EquipmentVisualSelection CreateVisualSelection(ActorId actorId, ulong sourceTick) =>
            new EquipmentVisualSelection(actorId, SlotId, EquipmentId, VisualBindingId, Revision, sourceTick);
    }

    public sealed class EquipmentStateAggregate
    {
        readonly ReadOnlyCollection<EquipmentSlotState> m_Slots;

        public EquipmentStateAggregate(
            StableHash catalogHash,
            IEnumerable<EquipmentSlotState> slots,
            PendingEquipmentChange pendingChange,
            PendingEquipmentChange lastResolvedChange)
        {
            if (!catalogHash.IsValid)
                throw new ArgumentException("Equipment state catalog hash is invalid.", nameof(catalogHash));
            CatalogHash = catalogHash;
            EquipmentSlotState[] stable = (slots ?? Array.Empty<EquipmentSlotState>()).OrderBy(value => value.SlotId.Value, StringComparer.Ordinal).ToArray();
            for (int i = 1; i < stable.Length; i++)
            {
                if (stable[i - 1].SlotId == stable[i].SlotId)
                    throw new InvalidDataException($"Equipment state Slot '{stable[i].SlotId}' is duplicated.");
            }
            m_Slots = Array.AsReadOnly(stable);
            if (pendingChange.IsValid && !pendingChange.IsPending)
                throw new ArgumentException("Equipment aggregate pending record is already resolved.", nameof(pendingChange));
            if (lastResolvedChange.IsValid && lastResolvedChange.IsPending)
                throw new ArgumentException("Equipment aggregate resolved record is still pending.", nameof(lastResolvedChange));
            PendingChange = pendingChange;
            LastResolvedChange = lastResolvedChange;
        }

        public StableHash CatalogHash { get; }
        public IReadOnlyList<EquipmentSlotState> Slots => m_Slots;
        public PendingEquipmentChange PendingChange { get; }
        public PendingEquipmentChange LastResolvedChange { get; }

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
            return new EquipmentStateAggregate(layout.CatalogHash, slots, default, default);
        }

        public EquipmentSlotState RequireSlot(EquipmentSlotId slotId)
        {
            for (int i = 0; i < m_Slots.Count; i++)
            {
                if (m_Slots[i].SlotId == slotId)
                    return m_Slots[i];
            }
            throw new InvalidOperationException($"Equipment state Slot '{slotId}' is absent.");
        }

        public EquipmentStateAggregate WithSlot(EquipmentSlotState slot)
        {
            var values = m_Slots.ToArray();
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].SlotId != slot.SlotId)
                    continue;
                values[i] = slot;
                return new EquipmentStateAggregate(CatalogHash, values, PendingChange, LastResolvedChange);
            }
            throw new InvalidOperationException($"Equipment state Slot '{slot.SlotId}' is absent.");
        }

        public EquipmentStateAggregate WithPending(PendingEquipmentChange pending) =>
            new EquipmentStateAggregate(CatalogHash, m_Slots, pending, LastResolvedChange);

        public EquipmentStateAggregate ResolvePending(PendingEquipmentChangeState state, ulong resolvedTick)
        {
            if (!PendingChange.IsPending)
                throw new InvalidOperationException("Equipment aggregate has no active pending change.");
            return new EquipmentStateAggregate(
                CatalogHash,
                m_Slots,
                default,
                PendingChange.Resolve(state, resolvedTick));
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
                writer.WriteUInt64(slot.HostGeneration);
                writer.WriteBoolean(slot.ContributionsInstalled);
                writer.WriteString(slot.TagSource);
                writer.WriteInt32(slot.PassiveEffectHandles.Count);
                for (int handle = 0; handle < slot.PassiveEffectHandles.Count; handle++)
                    writer.WriteUInt64(slot.PassiveEffectHandles[handle]);
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
            PendingEquipmentChange pending = ReadChange(reader, layout);
            PendingEquipmentChange resolved = ReadChange(reader, layout);
            return new EquipmentStateAggregate(hash, slots, pending, resolved);
        }

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
                ReadEnum<PendingEquipmentChangeState>(reader.ReadByte()));
            layout.RequireSlot(change.SlotId);
            if (change.FromEquipmentId.IsValid) layout.RequireItem(change.FromEquipmentId);
            if (change.ToEquipmentId.IsValid) layout.RequireItem(change.ToEquipmentId);
            return change;
        }

        static EquipmentId ReadOptionalEquipment(string value) => string.IsNullOrEmpty(value) ? default : new EquipmentId(value);
        static EquipmentFeatureId ReadOptionalFeature(string value) => string.IsNullOrEmpty(value) ? default : new EquipmentFeatureId(value);
        static EquipmentVisualBindingId ReadOptionalVisual(string value) => string.IsNullOrEmpty(value) ? default : new EquipmentVisualBindingId(value);
        static T ReadEnum<T>(byte value) where T : struct, Enum
        {
            T result = (T)Enum.ToObject(typeof(T), value);
            return Enum.IsDefined(typeof(T), result) ? result : throw new InvalidDataException($"Equipment state enum '{typeof(T).Name}' value '{value}' is invalid.");
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
            if (item.SlotId != context.SlotId || item.FeatureId != context.FeatureId ||
                route.OwnerSlotId != context.SlotId || feature.FeatureId != context.FeatureId)
            {
                throw new InvalidDataException($"Equipment Action Context '{context}' does not match Program layout.");
            }
            return context;
        }
    }
}
