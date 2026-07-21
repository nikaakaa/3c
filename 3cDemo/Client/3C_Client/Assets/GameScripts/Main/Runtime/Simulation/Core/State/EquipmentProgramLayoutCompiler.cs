using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ThirdPersonSimulation
{
    public enum EquipmentCatalogConstantKind : byte
    {
        Boolean = 1,
        Int32 = 2,
        UInt64 = 3,
        String = 4
    }

    public readonly struct EquipmentCatalogConstant
    {
        public EquipmentCatalogConstant(EquipmentCatalogConstantKind kind, bool boolean, int int32, ulong uint64, string text)
        {
            Kind = kind;
            Boolean = boolean;
            Int32 = int32;
            UInt64 = uint64;
            Text = text ?? string.Empty;
        }

        public EquipmentCatalogConstantKind Kind { get; }
        public bool Boolean { get; }
        public int Int32 { get; }
        public ulong UInt64 { get; }
        public string Text { get; }
    }

    public static class EquipmentProgramLayoutCompiler
    {
        public static EquipmentProgramLayout Compile(
            bool capabilityEnabled,
            IReadOnlyList<ProgramCatalogEntry> catalog,
            IReadOnlyList<ProgramStateSlot> stateSlots,
            IReadOnlyList<ProgramReference> references,
            Func<int, EquipmentCatalogConstant> constant)
        {
            if (!capabilityEnabled)
                return new EquipmentProgramLayout(false, null, null, null, null, null, null, null, null);
            if (catalog == null || stateSlots == null || references == null || constant == null)
                throw new ArgumentNullException();
            var initial = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (ProgramCatalogEntry entry in catalog.Where(value => value.Kind == ProgramCatalogEntryKind.EquipmentInitialLoadout))
            {
                string slot = Trim(Identity(entry, "Slot"), "equipment:slot:");
                bool hasEquipment = Boolean(entry, "HasEquipment", constant);
                string equipment = hasEquipment ? Trim(Identity(entry, "Equipment"), "equipment:item:") : string.Empty;
                if (!initial.TryAdd(slot, equipment))
                    throw new InvalidDataException($"Equipment Initial Loadout Slot '{slot}' is duplicated.");
            }
            var slots = new List<EquipmentProgramSlot>();
            foreach (ProgramCatalogEntry entry in catalog.Where(value => value.Kind == ProgramCatalogEntryKind.EquipmentSlot))
            {
                string slot = Trim(entry.Identity, "equipment:slot:");
                if (!initial.TryGetValue(slot, out string equipment))
                    throw new InvalidDataException($"Equipment Slot '{slot}' has no Initial Loadout entry.");
                slots.Add(new EquipmentProgramSlot(
                    new EquipmentSlotId(slot),
                    (EquipmentSlotRequirement)Int32(entry, "Requirement", constant),
                    OptionalEquipment(equipment)));
            }
            var features = new List<EquipmentProgramFeature>();
            foreach (ProgramCatalogEntry entry in catalog.Where(value => value.Kind == ProgramCatalogEntryKind.EquipmentFeature))
            {
                features.Add(new EquipmentProgramFeature(
                    new EquipmentFeatureId(Trim(entry.Identity, "equipment:feature:")),
                    new EquipmentFeatureRevision(UInt64(entry, "FeatureRevision", constant)),
                    Identities(entry, "GrantedTag:").Select(value => Trim(value, "tag:")),
                    Identities(entry, "PassiveEffect:").Select(value => Trim(value, "effect:")),
                    (WorldCapability)UInt64(entry, "RequiredWorldCapabilities", constant),
                    ResolvePersistentEntry(
                        new EquipmentFeatureId(Trim(entry.Identity, "equipment:feature:")),
                        catalog,
                        references)));
            }
            var items = new List<EquipmentProgramItem>();
            foreach (ProgramCatalogEntry entry in catalog.Where(value => value.Kind == ProgramCatalogEntryKind.EquipmentDefinition))
            {
                items.Add(new EquipmentProgramItem(
                    new EquipmentId(Trim(entry.Identity, "equipment:item:")),
                    new EquipmentSlotId(Trim(Identity(entry, "Slot"), "equipment:slot:")),
                    new EquipmentFeatureId(Trim(Identity(entry, "Feature"), "equipment:feature:")),
                    new EquipmentVisualBindingId(Trim(Identity(entry, "VisualBinding"), "equipment:visual:"))));
            }
            var routes = new List<EquipmentProgramRoute>();
            foreach (ProgramCatalogEntry entry in catalog.Where(value => value.Kind == ProgramCatalogEntryKind.EquipmentRoute))
            {
                routes.Add(new EquipmentProgramRoute(
                    new EquipmentActionRouteId(Trim(entry.Identity, "equipment:route:")),
                    new EquipmentSlotId(Trim(Identity(entry, "OwnerSlot"), "equipment:slot:")),
                    Trim(Identity(entry, "InputRequest"), "input:request:"),
                    (EquipmentRouteRequestConsumption)Int32(entry, "RequestConsumption", constant),
                    (EquipmentRouteMissingImplementation)Int32(entry, "MissingImplementation", constant)));
            }
            var routeImplementations = new List<EquipmentProgramRouteImplementation>();
            foreach (ProgramCatalogEntry entry in catalog.Where(value => value.Kind == ProgramCatalogEntryKind.EquipmentRouteImplementation))
            {
                EquipmentFeatureId featureId = new EquipmentFeatureId(Trim(Identity(entry, "Feature"), "equipment:feature:"));
                EquipmentActionRouteId routeId = new EquipmentActionRouteId(Trim(Identity(entry, "Route"), "equipment:route:"));
                OperationHandle root = ResolveRouteEntry(featureId, routeId, catalog, references);
                routeImplementations.Add(new EquipmentProgramRouteImplementation(
                    featureId,
                    routeId,
                    Trim(Identity(entry, "Action"), "action:"),
                    root));
            }
            var parameters = new List<EquipmentProgramParameter>();
            foreach (ProgramCatalogEntry entry in catalog.Where(value => value.Kind == ProgramCatalogEntryKind.EquipmentParameterValue))
            {
                string equipmentId = Trim(Identity(entry, "Equipment"), "equipment:item:");
                string schema = Identity(entry, "Schema");
                ParseParameterSchema(schema, out EquipmentFeatureId featureId, out EquipmentParameterId parameterId);
                parameters.Add(new EquipmentProgramParameter(
                    new EquipmentId(equipmentId),
                    featureId,
                    parameterId,
                    (EquipmentParameterValueKind)Int32(entry, "ValueKind", constant),
                    Field(entry, "Value", ProgramCatalogFieldKind.Constant).ConstantIndex));
            }
            var localStates = new List<EquipmentProgramLocalState>();
            foreach (ProgramCatalogEntry entry in catalog.Where(value => value.Kind == ProgramCatalogEntryKind.EquipmentFeatureLocalState))
            {
                ParseLocalState(entry.Identity, out EquipmentFeatureId featureId, out EquipmentLocalStateId stateId);
                int slot = -1;
                for (int i = 0; i < stateSlots.Count; i++)
                {
                    if (stateSlots[i].OwnerKind == ProgramStateOwnerKind.Equipment &&
                        stateSlots[i].Semantic == ProgramStateSemantic.EquipmentLocalState &&
                        string.Equals(stateSlots[i].OwnerIdentity, entry.Identity, StringComparison.Ordinal))
                    {
                        if (slot >= 0)
                            throw new InvalidDataException($"Equipment local state '{entry.Identity}' has duplicate state slots.");
                        slot = i;
                    }
                }
                if (slot < 0)
                    throw new InvalidDataException($"Equipment local state '{entry.Identity}' has no typed state slot.");
                localStates.Add(new EquipmentProgramLocalState(featureId, stateId, slot));
            }
            IReadOnlyList<EquipmentProgramOperationBinding> operationBindings = CompileOperationBindings(catalog, references);
            return new EquipmentProgramLayout(true, slots, features, items, routes, routeImplementations, parameters, localStates, operationBindings);
        }

        static IReadOnlyList<EquipmentProgramOperationBinding> CompileOperationBindings(
            IReadOnlyList<ProgramCatalogEntry> catalog,
            IReadOnlyList<ProgramReference> references)
        {
            Dictionary<int, ProgramCatalogEntry> entries = catalog.ToDictionary(value => value.Index);
            var slots = new Dictionary<int, EquipmentSlotId>();
            var routes = new Dictionary<int, EquipmentActionRouteId>();
            var equipment = new Dictionary<int, EquipmentId>();
            var parameters = new Dictionary<int, EquipmentParameterId>();
            for (int i = 0; i < references.Count; i++)
            {
                ProgramReference reference = references[i];
                if (!reference.HasSourceOperation || reference.Kind != ProgramReferenceKind.CatalogEntry)
                    continue;
                if (!entries.TryGetValue(reference.TargetIndex, out ProgramCatalogEntry entry))
                    throw new InvalidDataException($"Equipment operation reference '{reference.Identity}' targets an unknown catalog entry.");
                int operation = reference.SourceOperation.Value;
                switch (entry.Kind)
                {
                    case ProgramCatalogEntryKind.EquipmentSlot:
                        AddUnique(slots, operation, new EquipmentSlotId(Trim(entry.Identity, "equipment:slot:")), "Slot");
                        break;
                    case ProgramCatalogEntryKind.EquipmentRoute:
                        AddUnique(routes, operation, new EquipmentActionRouteId(Trim(entry.Identity, "equipment:route:")), "Route");
                        break;
                    case ProgramCatalogEntryKind.EquipmentDefinition:
                        AddUnique(equipment, operation, new EquipmentId(Trim(entry.Identity, "equipment:item:")), "Equipment");
                        break;
                    case ProgramCatalogEntryKind.EquipmentFeatureParameter:
                        ParseParameterSchema(entry.Identity, out _, out EquipmentParameterId parameterId);
                        AddUnique(parameters, operation, parameterId, "Parameter");
                        break;
                }
            }
            int[] operations = slots.Keys.Concat(routes.Keys).Concat(equipment.Keys).Concat(parameters.Keys).Distinct().OrderBy(value => value).ToArray();
            var result = new EquipmentProgramOperationBinding[operations.Length];
            for (int i = 0; i < operations.Length; i++)
            {
                int operation = operations[i];
                slots.TryGetValue(operation, out EquipmentSlotId slotId);
                routes.TryGetValue(operation, out EquipmentActionRouteId routeId);
                equipment.TryGetValue(operation, out EquipmentId equipmentId);
                parameters.TryGetValue(operation, out EquipmentParameterId parameterId);
                result[i] = new EquipmentProgramOperationBinding(new OperationHandle(operation), slotId, routeId, equipmentId, parameterId);
            }
            return result;
        }

        static void AddUnique<T>(Dictionary<int, T> values, int operation, T value, string kind)
        {
            if (!values.TryAdd(operation, value))
                throw new InvalidDataException($"Equipment operation '{operation}' has duplicate {kind} bindings.");
        }

        static OperationHandle ResolveRouteEntry(
            EquipmentFeatureId featureId,
            EquipmentActionRouteId routeId,
            IReadOnlyList<ProgramCatalogEntry> catalog,
            IReadOnlyList<ProgramReference> references)
        {
            ProgramCatalogEntry root = catalog.SingleOrDefault(entry =>
                entry.Kind == ProgramCatalogEntryKind.CompositionRoot &&
                string.Equals(OptionalIdentity(entry, "Feature"), $"equipment:feature:{featureId.Value}", StringComparison.Ordinal) &&
                string.Equals(OptionalIdentity(entry, "Route"), $"equipment:route:{routeId.Value}", StringComparison.Ordinal));
            if (root == null)
                throw new InvalidDataException($"Equipment Route '{featureId}/{routeId}' has no composition root.");
            string identity = Trim(root.Identity, "composition-root:");
            ProgramReference reference = references.SingleOrDefault(value =>
                !value.HasSourceOperation && value.Kind == ProgramReferenceKind.Operation &&
                string.Equals(value.ExternalIdentity, identity, StringComparison.Ordinal));
            return reference == null
                ? throw new InvalidDataException($"Equipment Route '{featureId}/{routeId}' has no compiled root operation.")
                : new OperationHandle(reference.TargetIndex);
        }

        static OperationHandle ResolvePersistentEntry(
            EquipmentFeatureId featureId,
            IReadOnlyList<ProgramCatalogEntry> catalog,
            IReadOnlyList<ProgramReference> references)
        {
            ProgramCatalogEntry root = catalog.SingleOrDefault(entry =>
                entry.Kind == ProgramCatalogEntryKind.CompositionRoot &&
                string.Equals(OptionalIdentity(entry, "Feature"), $"equipment:feature:{featureId.Value}", StringComparison.Ordinal) &&
                string.IsNullOrEmpty(OptionalIdentity(entry, "Route")));
            if (root == null)
                return default;
            string identity = Trim(root.Identity, "composition-root:");
            ProgramReference reference = references.SingleOrDefault(value =>
                !value.HasSourceOperation && value.Kind == ProgramReferenceKind.Operation &&
                string.Equals(value.ExternalIdentity, identity, StringComparison.Ordinal));
            return reference == null
                ? throw new InvalidDataException($"Equipment Feature '{featureId}' has no compiled persistent root operation.")
                : new OperationHandle(reference.TargetIndex);
        }

        static void ParseParameterSchema(string identity, out EquipmentFeatureId featureId, out EquipmentParameterId parameterId)
        {
            const string prefix = "equipment:feature:";
            const string marker = ":parameter:";
            int index = identity.IndexOf(marker, prefix.Length, StringComparison.Ordinal);
            if (!identity.StartsWith(prefix, StringComparison.Ordinal) || index < 0)
                throw new InvalidDataException($"Equipment Parameter schema identity '{identity}' is invalid.");
            featureId = new EquipmentFeatureId(identity.Substring(prefix.Length, index - prefix.Length));
            parameterId = new EquipmentParameterId(identity.Substring(index + marker.Length));
        }

        static void ParseLocalState(string identity, out EquipmentFeatureId featureId, out EquipmentLocalStateId stateId)
        {
            const string prefix = "equipment:feature:";
            const string marker = ":state:";
            int index = identity.IndexOf(marker, prefix.Length, StringComparison.Ordinal);
            if (!identity.StartsWith(prefix, StringComparison.Ordinal) || index < 0)
                throw new InvalidDataException($"Equipment local state identity '{identity}' is invalid.");
            featureId = new EquipmentFeatureId(identity.Substring(prefix.Length, index - prefix.Length));
            stateId = new EquipmentLocalStateId(identity.Substring(index + marker.Length));
        }

        static ProgramCatalogField Field(ProgramCatalogEntry entry, string name, ProgramCatalogFieldKind kind)
        {
            ProgramCatalogField field = entry.Fields.SingleOrDefault(value => string.Equals(value.Name, name, StringComparison.Ordinal));
            if (field == null || field.Kind != kind)
                throw new InvalidDataException($"Equipment catalog '{entry.Identity}' field '{name}' is missing or has the wrong kind.");
            return field;
        }

        static string Identity(ProgramCatalogEntry entry, string name) => Field(entry, name, ProgramCatalogFieldKind.Identity).Identity;
        static string OptionalIdentity(ProgramCatalogEntry entry, string name) =>
            entry.Fields.FirstOrDefault(value => value.Kind == ProgramCatalogFieldKind.Identity && string.Equals(value.Name, name, StringComparison.Ordinal))?.Identity ?? string.Empty;
        static IEnumerable<string> Identities(ProgramCatalogEntry entry, string prefix) =>
            entry.Fields.Where(value => value.Kind == ProgramCatalogFieldKind.Identity && value.Name.StartsWith(prefix, StringComparison.Ordinal)).OrderBy(value => value.Name, StringComparer.Ordinal).Select(value => value.Identity);
        static bool Boolean(ProgramCatalogEntry entry, string name, Func<int, EquipmentCatalogConstant> constant)
        {
            EquipmentCatalogConstant value = constant(Field(entry, name, ProgramCatalogFieldKind.Constant).ConstantIndex);
            return value.Kind == EquipmentCatalogConstantKind.Boolean ? value.Boolean : throw new InvalidDataException($"Equipment catalog '{entry.Identity}' field '{name}' is not Boolean.");
        }
        static int Int32(ProgramCatalogEntry entry, string name, Func<int, EquipmentCatalogConstant> constant)
        {
            EquipmentCatalogConstant value = constant(Field(entry, name, ProgramCatalogFieldKind.Constant).ConstantIndex);
            return value.Kind == EquipmentCatalogConstantKind.Int32 ? value.Int32 : throw new InvalidDataException($"Equipment catalog '{entry.Identity}' field '{name}' is not Int32.");
        }
        static ulong UInt64(ProgramCatalogEntry entry, string name, Func<int, EquipmentCatalogConstant> constant)
        {
            EquipmentCatalogConstant value = constant(Field(entry, name, ProgramCatalogFieldKind.Constant).ConstantIndex);
            return value.Kind == EquipmentCatalogConstantKind.UInt64 ? value.UInt64 : throw new InvalidDataException($"Equipment catalog '{entry.Identity}' field '{name}' is not UInt64.");
        }
        static string Trim(string value, string prefix) => value != null && value.StartsWith(prefix, StringComparison.Ordinal)
            ? value.Substring(prefix.Length)
            : throw new InvalidDataException($"Equipment identity '{value}' does not start with '{prefix}'.");
        static EquipmentId OptionalEquipment(string value) => string.IsNullOrEmpty(value) ? default : new EquipmentId(value);
    }
}
