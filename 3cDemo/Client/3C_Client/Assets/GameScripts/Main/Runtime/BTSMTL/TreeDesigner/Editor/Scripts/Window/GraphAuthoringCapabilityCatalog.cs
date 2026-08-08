using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TreeDesigner.Editor
{
    public enum GraphAuthoringFieldValueKind : byte
    {
        String = 1,
        Boolean = 2,
        Integer = 3,
        Float = 4,
        Vector2 = 5,
        Vector3 = 6,
        Quaternion = 7,
        Enum = 8,
        AssetReference = 9,
        IdentityReference = 10,
        Object = 11
    }

    [Flags]
    public enum GraphAuthoringFieldAccess : byte
    {
        None = 0,
        AuthoringRead = 1,
        AuthoringWrite = 2,
        ReferenceRead = 4,
        DiagnosticRead = 8
    }

    public enum GraphAuthoringDetailsSection : byte
    {
        Authoring = 1,
        RuntimeInputs = 2,
        AppliedValues = 3,
        References = 4,
        Diagnostics = 5
    }

    public enum GraphAuthoringFieldInteractionPolicy : byte
    {
        Unclassified = 0,
        Structural = 1,
        TunableDefault = 2,
        RuntimeInput = 3,
        DerivedReadOnly = 4
    }

    public enum GraphAuthoringFieldApplyTiming : byte
    {
        NextFrame = 0,
        NextActivation = 1
    }

    public enum GraphAuthoringFieldStatePolicy : byte
    {
        PreserveState = 0,
        ResetOwnerState = 1
    }

    public sealed class GraphAuthoringFieldTuningMetadata
    {
        public GraphAuthoringFieldTuningMetadata(
            GraphAuthoringFieldInteractionPolicy interaction,
            GraphAuthoringFieldValueKind typedValueKind,
            string unit,
            double minimum,
            double maximum,
            bool finite,
            GraphAuthoringFieldApplyTiming applyTiming,
            GraphAuthoringFieldStatePolicy statePolicy,
            string consumerId,
            int consumerOperationStart,
            int consumerOperationCount)
        {
            if (minimum > maximum || consumerOperationStart < 0 || consumerOperationCount < 0)
                throw new ArgumentException("Graph authoring tuning metadata range is invalid.");
            Interaction = interaction;
            TypedValueKind = typedValueKind;
            Unit = unit ?? string.Empty;
            Minimum = minimum;
            Maximum = maximum;
            Finite = finite;
            ApplyTiming = applyTiming;
            StatePolicy = statePolicy;
            ConsumerId = consumerId ?? string.Empty;
            ConsumerOperationStart = consumerOperationStart;
            ConsumerOperationCount = consumerOperationCount;
        }

        public GraphAuthoringFieldInteractionPolicy Interaction { get; }
        public GraphAuthoringFieldValueKind TypedValueKind { get; }
        public string Unit { get; }
        public double Minimum { get; }
        public double Maximum { get; }
        public bool Finite { get; }
        public GraphAuthoringFieldApplyTiming ApplyTiming { get; }
        public GraphAuthoringFieldStatePolicy StatePolicy { get; }
        public string ConsumerId { get; }
        public int ConsumerOperationStart { get; }
        public int ConsumerOperationCount { get; }
    }

    public enum GraphAuthoringDynamicPortPolicy : byte
    {
        None = 0,
        OrderedInputs = 1,
        OrderedOutputs = 2,
        OrderedBidirectional = 3
    }

    public enum GraphAuthoringNodePresentationKind : byte
    {
        Standard = 0,
        StateMachineEntry = 1,
        State = 2,
        StateAlias = 3,
        TransitionRule = 4
    }

    public sealed class GraphAuthoringFieldConstraint
    {
        public GraphAuthoringFieldConstraint(
            double? minimum = null,
            double? maximum = null,
            bool finite = false,
            bool nonEmpty = false,
            IReadOnlyList<string> allowedValues = null)
        {
            if (minimum.HasValue && maximum.HasValue && minimum.Value > maximum.Value)
                throw new ArgumentException("Graph authoring field constraint range is invalid.");
            Minimum = minimum;
            Maximum = maximum;
            Finite = finite;
            NonEmpty = nonEmpty;
            AllowedValues = allowedValues ?? Array.Empty<string>();
        }

        public double? Minimum { get; }
        public double? Maximum { get; }
        public bool Finite { get; }
        public bool NonEmpty { get; }
        public IReadOnlyList<string> AllowedValues { get; }
    }

    public sealed class GraphAuthoringFieldVisibilityCondition
    {
        public GraphAuthoringFieldVisibilityCondition(
            GraphAuthoringFieldId controllerFieldId,
            string expectedValue)
        {
            ControllerFieldId = controllerFieldId.IsValid
                ? controllerFieldId
                : throw new ArgumentException("Visibility controller field identity is missing.", nameof(controllerFieldId));
            ExpectedValue = GraphAuthoringIdentity.Require(expectedValue, nameof(expectedValue));
        }

        public GraphAuthoringFieldId ControllerFieldId { get; }
        public string ExpectedValue { get; }

        public bool IsVisible(Func<GraphAuthoringFieldId, object> readField) =>
            string.Equals(
                readField?.Invoke(ControllerFieldId)?.ToString(),
                ExpectedValue,
                StringComparison.Ordinal);
    }

    public sealed class GraphAuthoringFieldDescriptor
    {
        public GraphAuthoringFieldDescriptor(
            GraphAuthoringFieldId fieldId,
            string displayName,
            GraphAuthoringFieldValueKind valueKind,
            GraphAuthoringFieldAccess access,
            GraphAuthoringDetailsSection section = GraphAuthoringDetailsSection.Authoring,
            object defaultValue = null,
            GraphAuthoringFieldConstraint constraint = null,
            string pickerKind = "",
            bool optional = false,
            Type objectType = null,
            GraphAuthoringFieldVisibilityCondition visibility = null,
            GraphAuthoringFieldTuningMetadata tuning = null)
        {
            FieldId = fieldId.IsValid ? fieldId : throw new ArgumentException("Field identity is missing.", nameof(fieldId));
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? throw new ArgumentException("Field display name is missing.", nameof(displayName)) : displayName;
            ValueKind = valueKind;
            Access = access;
            Section = section;
            DefaultValue = defaultValue;
            Constraint = constraint ?? new GraphAuthoringFieldConstraint();
            PickerKind = pickerKind ?? string.Empty;
            Optional = optional;
            ObjectType = objectType;
            Visibility = visibility;
            Tuning = tuning;
        }

        public GraphAuthoringFieldId FieldId { get; }
        public string DisplayName { get; }
        public GraphAuthoringFieldValueKind ValueKind { get; }
        public GraphAuthoringFieldAccess Access { get; }
        public GraphAuthoringDetailsSection Section { get; }
        public object DefaultValue { get; }
        public GraphAuthoringFieldConstraint Constraint { get; }
        public string PickerKind { get; }
        public bool Optional { get; }
        public Type ObjectType { get; }
        public GraphAuthoringFieldVisibilityCondition Visibility { get; }
        public GraphAuthoringFieldTuningMetadata Tuning { get; }
        public GraphAuthoringFieldInteractionPolicy Interaction =>
            Tuning?.Interaction ?? GraphAuthoringFieldInteractionPolicy.Unclassified;
        public bool AuthoringVisible => (Access & GraphAuthoringFieldAccess.AuthoringRead) != 0;
        public bool AuthoringWritable => (Access & GraphAuthoringFieldAccess.AuthoringWrite) != 0;
        public bool IsVisible(Func<GraphAuthoringFieldId, object> readField) =>
            Visibility == null || Visibility.IsVisible(readField);
    }

    public sealed class GraphAuthoringPortDescriptor
    {
        public GraphAuthoringPortDescriptor(
            GraphAuthoringPortId portId,
            string displayName,
            string valueTypeId,
            GraphAuthoringPortDirection direction,
            GraphAuthoringPortCapacity capacity,
            bool required,
            int order,
            string interfacePortId = "")
        {
            PortId = portId.IsValid ? portId : throw new ArgumentException("Port identity is missing.", nameof(portId));
            DisplayName = displayName ?? string.Empty;
            ValueTypeId = GraphAuthoringIdentity.Require(valueTypeId, nameof(valueTypeId));
            Direction = direction;
            Capacity = capacity;
            Required = required;
            Order = order;
            InterfacePortId = interfacePortId ?? string.Empty;
        }

        public GraphAuthoringPortId PortId { get; }
        public string DisplayName { get; }
        public string ValueTypeId { get; }
        public GraphAuthoringPortDirection Direction { get; }
        public GraphAuthoringPortCapacity Capacity { get; }
        public bool Required { get; }
        public int Order { get; }
        public string InterfacePortId { get; }
    }

    public readonly struct GraphAuthoringTypedPropertyValue
    {
        public GraphAuthoringTypedPropertyValue(
            GraphAuthoringFieldId fieldId,
            GraphAuthoringFieldValueKind valueKind,
            string canonicalValue)
        {
            FieldId = fieldId.IsValid
                ? fieldId
                : throw new ArgumentException("Typed property field identity is missing.", nameof(fieldId));
            ValueKind = valueKind;
            CanonicalValue = GraphAuthoringIdentity.Require(canonicalValue, nameof(canonicalValue));
        }

        public GraphAuthoringFieldId FieldId { get; }
        public GraphAuthoringFieldValueKind ValueKind { get; }
        public string CanonicalValue { get; }
    }

    public sealed class GraphAuthoringPortVariantCondition
    {
        public GraphAuthoringPortVariantCondition(
            GraphAuthoringFieldId fieldId,
            GraphAuthoringFieldValueKind valueKind,
            string equals)
        {
            FieldId = fieldId.IsValid
                ? fieldId
                : throw new ArgumentException("Port variant discriminator field identity is missing.", nameof(fieldId));
            ValueKind = valueKind;
            ExpectedValue = GraphAuthoringIdentity.Require(equals, nameof(equals));
        }

        public GraphAuthoringFieldId FieldId { get; }
        public GraphAuthoringFieldValueKind ValueKind { get; }
        public string ExpectedValue { get; }

        public bool Matches(GraphAuthoringTypedPropertyValue value) =>
            value.FieldId.Equals(FieldId) &&
            value.ValueKind == ValueKind &&
            string.Equals(value.CanonicalValue, ExpectedValue, StringComparison.Ordinal);
    }

    public sealed class GraphAuthoringPortVariantDescriptor
    {
        readonly Dictionary<GraphAuthoringPortId, GraphAuthoringPortDescriptor> m_Ports;

        public GraphAuthoringPortVariantDescriptor(
            string variantId,
            GraphAuthoringPortVariantCondition when,
            IReadOnlyList<GraphAuthoringPortDescriptor> ports)
        {
            VariantId = GraphAuthoringIdentity.Require(variantId, nameof(variantId));
            When = when ?? throw new ArgumentNullException(nameof(when));
            m_Ports = new Dictionary<GraphAuthoringPortId, GraphAuthoringPortDescriptor>();
            foreach (GraphAuthoringPortDescriptor port in ports ?? Array.Empty<GraphAuthoringPortDescriptor>())
            {
                if (port == null)
                    throw new ArgumentException($"Port variant '{VariantId}' contains a missing port.", nameof(ports));
                if (!m_Ports.TryAdd(port.PortId, port))
                    throw new InvalidOperationException($"Port variant '{VariantId}' contains duplicate port identity '{port.PortId}'.");
            }
        }

        public string VariantId { get; }
        public GraphAuthoringPortVariantCondition When { get; }
        public IReadOnlyCollection<GraphAuthoringPortDescriptor> Ports => m_Ports.Values;
    }

    public sealed class GraphAuthoringPortShapeException : InvalidOperationException
    {
        public GraphAuthoringPortShapeException(string code, string message)
            : base(message)
        {
            Code = GraphAuthoringIdentity.Require(code, nameof(code));
        }

        public string Code { get; }
    }

    public static class GraphAuthoringNodePortShapeProjector
    {
        public static IReadOnlyList<GraphAuthoringDynamicPortProjection> Project(
            GraphAuthoringCapabilityDescriptor capability,
            IReadOnlyList<GraphAuthoringTypedPropertyValue> properties,
            IReadOnlyList<GraphAuthoringDynamicPortProjection> authoredDynamicPorts = null)
        {
            if (capability == null)
                throw new ArgumentNullException(nameof(capability));

            GraphAuthoringPortVariantDescriptor variant = ResolveVariant(
                capability,
                properties ?? Array.Empty<GraphAuthoringTypedPropertyValue>());
            var occupied = new HashSet<GraphAuthoringPortId>(
                capability.FixedPorts.Select(value => value.PortId));
            var result = new List<GraphAuthoringDynamicPortProjection>();
            if (variant != null)
            {
                foreach (GraphAuthoringPortDescriptor port in variant.Ports.OrderBy(value => value.Order))
                {
                    AddProjectedPort(
                        result,
                        occupied,
                        new GraphAuthoringDynamicPortProjection(
                            port.PortId,
                            port.DisplayName,
                            port.ValueTypeId,
                            port.Direction,
                            port.Capacity,
                            port.Required,
                            port.Order),
                        capability.CapabilityId);
                }
            }
            foreach (GraphAuthoringDynamicPortProjection port in
                     authoredDynamicPorts ?? Array.Empty<GraphAuthoringDynamicPortProjection>())
            {
                AddProjectedPort(result, occupied, port, capability.CapabilityId);
            }
            return result.OrderBy(value => value.Order).ToArray();
        }

        static GraphAuthoringPortVariantDescriptor ResolveVariant(
            GraphAuthoringCapabilityDescriptor capability,
            IReadOnlyList<GraphAuthoringTypedPropertyValue> properties)
        {
            if (capability.PortVariants.Count == 0)
                return null;

            GraphAuthoringPortVariantCondition discriminator = capability.PortVariants[0].When;
            GraphAuthoringTypedPropertyValue[] values = properties
                .Where(value => value.FieldId.Equals(discriminator.FieldId))
                .ToArray();
            if (values.Length != 1 || values[0].ValueKind != discriminator.ValueKind)
            {
                throw new GraphAuthoringPortShapeException(
                    "port_shape_discriminator_unknown",
                    $"Capability '{capability.CapabilityId}' requires one '{discriminator.ValueKind}' discriminator '{discriminator.FieldId}'.");
            }

            GraphAuthoringPortVariantDescriptor[] matches = capability.PortVariants
                .Where(value => value.When.Matches(values[0]))
                .ToArray();
            if (matches.Length == 0)
            {
                throw new GraphAuthoringPortShapeException(
                    "port_shape_variant_missing",
                    $"Capability '{capability.CapabilityId}' has no port variant for '{discriminator.FieldId}={values[0].CanonicalValue}'.");
            }
            if (matches.Length > 1)
            {
                throw new GraphAuthoringPortShapeException(
                    "port_shape_variant_ambiguous",
                    $"Capability '{capability.CapabilityId}' has multiple port variants for '{discriminator.FieldId}={values[0].CanonicalValue}'.");
            }
            return matches[0];
        }

        static void AddProjectedPort(
            ICollection<GraphAuthoringDynamicPortProjection> result,
            ISet<GraphAuthoringPortId> occupied,
            GraphAuthoringDynamicPortProjection port,
            GraphAuthoringCapabilityId capabilityId)
        {
            if (!occupied.Add(port.PortId))
            {
                throw new GraphAuthoringPortShapeException(
                    "port_shape_identity_duplicate",
                    $"Capability '{capabilityId}' projects duplicate port identity '{port.PortId}'.");
            }
            result.Add(port);
        }
    }

    public enum GraphAuthoringCommandPresentationKind : byte
    {
        Button = 1,
        Custom = 2
    }

    public sealed class GraphAuthoringCommandDescriptor
    {
        public GraphAuthoringCommandDescriptor(
            GraphAuthoringCommandId commandId,
            string displayName,
            bool destructive,
            GraphAuthoringCommandPresentationKind presentationKind =
                GraphAuthoringCommandPresentationKind.Button)
        {
            CommandId = commandId.IsValid ? commandId : throw new ArgumentException("Command identity is missing.", nameof(commandId));
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? throw new ArgumentException("Command display name is missing.", nameof(displayName)) : displayName;
            Destructive = destructive;
            PresentationKind = presentationKind;
        }

        public GraphAuthoringCommandId CommandId { get; }
        public string DisplayName { get; }
        public bool Destructive { get; }
        public GraphAuthoringCommandPresentationKind PresentationKind
        {
            get;
        }
    }

    public sealed class GraphAuthoringChildSurfaceDescriptor
    {
        public GraphAuthoringChildSurfaceDescriptor(GraphAuthoringCommandId commandId, GraphAuthoringDocumentRoleId documentRoleId, string displayName)
        {
            CommandId = commandId.IsValid ? commandId : throw new ArgumentException("Child surface command identity is missing.", nameof(commandId));
            DocumentRoleId = documentRoleId.IsValid ? documentRoleId : throw new ArgumentException("Child surface role identity is missing.", nameof(documentRoleId));
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? throw new ArgumentException("Child surface display name is missing.", nameof(displayName)) : displayName;
        }

        public GraphAuthoringCommandId CommandId { get; }
        public GraphAuthoringDocumentRoleId DocumentRoleId { get; }
        public string DisplayName { get; }
    }

    public sealed class GraphAuthoringCapabilityDescriptor
    {
        readonly HashSet<GraphAuthoringDocumentRoleId> m_AllowedDocumentRoles;
        readonly Dictionary<GraphAuthoringFieldId, GraphAuthoringFieldDescriptor> m_Fields;
        readonly Dictionary<GraphAuthoringPortId, GraphAuthoringPortDescriptor> m_FixedPorts;
        readonly IReadOnlyList<GraphAuthoringPortVariantDescriptor> m_PortVariants;

        public GraphAuthoringCapabilityDescriptor(
            GraphAuthoringCapabilityId capabilityId,
            GraphAuthoringDomainId domainId,
            IReadOnlyList<GraphAuthoringDocumentRoleId> allowedDocumentRoles,
            string displayName,
            string category,
            Color color,
            IReadOnlyList<GraphAuthoringFieldDescriptor> fields = null,
            IReadOnlyList<GraphAuthoringPortDescriptor> fixedPorts = null,
            GraphAuthoringDynamicPortPolicy dynamicPortPolicy = GraphAuthoringDynamicPortPolicy.None,
            IReadOnlyList<GraphAuthoringChildSurfaceDescriptor> childSurfaces = null,
            IReadOnlyList<GraphAuthoringCommandDescriptor> commands = null,
            string iconName = "",
            GraphAuthoringNodePresentationKind presentationKind = GraphAuthoringNodePresentationKind.Standard,
            string mutationBindingId = "",
            string validationBindingId = "",
            string compilerBindingId = "",
            string documentCodecId = "",
            Type authoringType = null,
            string externalKind = "",
            bool systemOwned = false,
            string anchorId = "",
            string executionDomainId = "",
            IReadOnlyList<GraphAuthoringPortVariantDescriptor> portVariants = null)
        {
            CapabilityId = capabilityId.IsValid ? capabilityId : throw new ArgumentException("Capability identity is missing.", nameof(capabilityId));
            DomainId = domainId.IsValid ? domainId : throw new ArgumentException("Capability domain identity is missing.", nameof(domainId));
            if (allowedDocumentRoles == null || allowedDocumentRoles.Count == 0)
                throw new ArgumentException("Capability must allow at least one document role.", nameof(allowedDocumentRoles));
            m_AllowedDocumentRoles = new HashSet<GraphAuthoringDocumentRoleId>(allowedDocumentRoles);
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? throw new ArgumentException("Capability display name is missing.", nameof(displayName)) : displayName;
            Category = category ?? string.Empty;
            Color = color;
            m_Fields = Index(fields ?? Array.Empty<GraphAuthoringFieldDescriptor>(), value => value.FieldId, "field");
            m_FixedPorts = Index(fixedPorts ?? Array.Empty<GraphAuthoringPortDescriptor>(), value => value.PortId, "port");
            DynamicPortPolicy = dynamicPortPolicy;
            m_PortVariants = ValidatePortVariants(
                portVariants ?? Array.Empty<GraphAuthoringPortVariantDescriptor>());
            ChildSurfaces = childSurfaces ?? Array.Empty<GraphAuthoringChildSurfaceDescriptor>();
            Commands = commands ?? Array.Empty<GraphAuthoringCommandDescriptor>();
            IconName = iconName ?? string.Empty;
            PresentationKind = presentationKind;
            MutationBindingId = mutationBindingId ?? string.Empty;
            ValidationBindingId = validationBindingId ?? string.Empty;
            CompilerBindingId = compilerBindingId ?? string.Empty;
            DocumentCodecId = documentCodecId ?? string.Empty;
            AuthoringType = authoringType;
            ExternalKind = externalKind ?? string.Empty;
            SystemOwned = systemOwned;
            AnchorId = anchorId ?? string.Empty;
            ExecutionDomainId = executionDomainId ?? string.Empty;
            if (SystemOwned &&
                string.IsNullOrWhiteSpace(AnchorId))
            {
                throw new ArgumentException(
                    "System-owned capability requires an anchor identity.",
                    nameof(anchorId));
            }
            if (!SystemOwned &&
                !string.IsNullOrEmpty(AnchorId))
            {
                throw new ArgumentException(
                    "Only system-owned capability can declare an anchor identity.",
                    nameof(anchorId));
            }
        }

        public GraphAuthoringCapabilityId CapabilityId { get; }
        public GraphAuthoringDomainId DomainId { get; }
        public IReadOnlyCollection<GraphAuthoringDocumentRoleId> AllowedDocumentRoles => m_AllowedDocumentRoles;
        public string DisplayName { get; }
        public string Category { get; }
        public Color Color { get; }
        public IReadOnlyCollection<GraphAuthoringFieldDescriptor> Fields => m_Fields.Values;
        public IReadOnlyCollection<GraphAuthoringPortDescriptor> FixedPorts => m_FixedPorts.Values;
        public GraphAuthoringDynamicPortPolicy DynamicPortPolicy { get; }
        public IReadOnlyList<GraphAuthoringPortVariantDescriptor> PortVariants => m_PortVariants;
        public IReadOnlyList<GraphAuthoringChildSurfaceDescriptor> ChildSurfaces { get; }
        public IReadOnlyList<GraphAuthoringCommandDescriptor> Commands { get; }
        public string IconName { get; }
        public GraphAuthoringNodePresentationKind PresentationKind { get; }
        public string MutationBindingId { get; }
        public string ValidationBindingId { get; }
        public string CompilerBindingId { get; }
        public string DocumentCodecId { get; }
        public Type AuthoringType { get; }
        public string ExternalKind { get; }
        public bool SystemOwned { get; }
        public string AnchorId { get; }
        public string ExecutionDomainId { get; }

        public bool Allows(GraphAuthoringDocumentRoleId documentRoleId) => m_AllowedDocumentRoles.Contains(documentRoleId);
        public bool TryGetField(GraphAuthoringFieldId fieldId, out GraphAuthoringFieldDescriptor descriptor) => m_Fields.TryGetValue(fieldId, out descriptor);
        public bool TryGetFixedPort(GraphAuthoringPortId portId, out GraphAuthoringPortDescriptor descriptor) => m_FixedPorts.TryGetValue(portId, out descriptor);

        static Dictionary<TKey, TValue> Index<TKey, TValue>(IReadOnlyList<TValue> values, Func<TValue, TKey> key, string label)
            where TValue : class
        {
            var result = new Dictionary<TKey, TValue>();
            for (int i = 0; i < values.Count; i++)
            {
                TValue value = values[i] ?? throw new ArgumentException($"Capability {label} descriptor is missing.");
                TKey identity = key(value);
                if (!result.TryAdd(identity, value))
                    throw new InvalidOperationException($"Capability contains duplicate {label} identity '{identity}'.");
            }
            return result;
        }

        IReadOnlyList<GraphAuthoringPortVariantDescriptor> ValidatePortVariants(
            IReadOnlyList<GraphAuthoringPortVariantDescriptor> variants)
        {
            if (variants.Count == 0)
                return Array.Empty<GraphAuthoringPortVariantDescriptor>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            GraphAuthoringPortVariantCondition discriminator = variants[0]?.When ??
                throw new ArgumentException("Capability port variant is missing.", nameof(variants));
            foreach (GraphAuthoringPortVariantDescriptor variant in variants)
            {
                if (variant == null || !ids.Add(variant.VariantId))
                    throw new InvalidOperationException($"Capability '{CapabilityId}' contains a missing or duplicate port variant identity.");
                if (!variant.When.FieldId.Equals(discriminator.FieldId) ||
                    variant.When.ValueKind != discriminator.ValueKind)
                {
                    throw new InvalidOperationException($"Capability '{CapabilityId}' port variants must use one typed discriminator.");
                }
                foreach (GraphAuthoringPortDescriptor port in variant.Ports)
                {
                    if (m_FixedPorts.ContainsKey(port.PortId))
                    {
                        throw new InvalidOperationException(
                            $"Capability '{CapabilityId}' port variant '{variant.VariantId}' overlaps fixed port '{port.PortId}'.");
                    }
                }
            }
            return variants.ToArray();
        }
    }

    public sealed class GraphAuthoringCapabilityCatalog
    {
        readonly Dictionary<GraphAuthoringCapabilityId, GraphAuthoringCapabilityDescriptor> m_Descriptors =
            new Dictionary<GraphAuthoringCapabilityId, GraphAuthoringCapabilityDescriptor>();

        public IReadOnlyCollection<GraphAuthoringCapabilityDescriptor> Descriptors => m_Descriptors.Values;

        public void Register(GraphAuthoringCapabilityDescriptor descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            if (descriptor.AuthoringType != null &&
                m_Descriptors.Values.Any(value =>
                    value.DomainId.Equals(descriptor.DomainId) &&
                    value.AuthoringType == descriptor.AuthoringType))
            {
                throw new InvalidOperationException(
                    $"Graph authoring type '{descriptor.AuthoringType.FullName}' is already registered in domain '{descriptor.DomainId}'.");
            }
            if (!string.IsNullOrEmpty(descriptor.ExternalKind) &&
                m_Descriptors.Values.Any(value =>
                    value.DomainId.Equals(descriptor.DomainId) &&
                    string.Equals(
                        value.ExternalKind,
                        descriptor.ExternalKind,
                        StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    $"Graph authoring external kind '{descriptor.ExternalKind}' is already registered in domain '{descriptor.DomainId}'.");
            }
            if (!m_Descriptors.TryAdd(descriptor.CapabilityId, descriptor))
                throw new InvalidOperationException($"Graph authoring capability '{descriptor.CapabilityId}' is already registered.");
        }

        public bool TryGetByAuthoringType(
            GraphAuthoringDomainId domainId,
            Type authoringType,
            out GraphAuthoringCapabilityDescriptor descriptor)
        {
            descriptor = authoringType == null
                ? null
                : m_Descriptors.Values.SingleOrDefault(value =>
                    value.DomainId.Equals(domainId) &&
                    value.AuthoringType == authoringType);
            return descriptor != null;
        }

        public bool TryGetByExternalKind(
            GraphAuthoringDomainId domainId,
            string externalKind,
            out GraphAuthoringCapabilityDescriptor descriptor)
        {
            descriptor = string.IsNullOrWhiteSpace(externalKind)
                ? null
                : m_Descriptors.Values.SingleOrDefault(value =>
                    value.DomainId.Equals(domainId) &&
                    string.Equals(
                        value.ExternalKind,
                        externalKind,
                        StringComparison.Ordinal));
            return descriptor != null;
        }

        public IReadOnlyList<GraphAuthoringCapabilityDescriptor>
            GetDomain(GraphAuthoringDomainId domainId) =>
            m_Descriptors.Values
                .Where(value =>
                    value.DomainId.Equals(domainId))
                .OrderBy(
                    value => value.CapabilityId.Value,
                    StringComparer.Ordinal)
                .ToArray();

        public GraphAuthoringCapabilityDescriptor Require(GraphAuthoringCapabilityId capabilityId)
        {
            if (!m_Descriptors.TryGetValue(capabilityId, out GraphAuthoringCapabilityDescriptor descriptor))
                throw new InvalidOperationException($"Graph authoring capability '{capabilityId}' is not registered.");
            return descriptor;
        }

        public GraphAuthoringCapabilityDescriptor Require(
            GraphAuthoringCapabilityId capabilityId,
            GraphAuthoringDomainId domainId,
            GraphAuthoringDocumentRoleId documentRoleId)
        {
            GraphAuthoringCapabilityDescriptor descriptor = Require(capabilityId);
            if (!descriptor.DomainId.Equals(domainId) || !descriptor.Allows(documentRoleId))
                throw new InvalidOperationException($"Capability '{capabilityId}' is not allowed in domain '{domainId}' role '{documentRoleId}'.");
            return descriptor;
        }

        public IReadOnlyList<GraphAuthoringCapabilityDescriptor> GetAllowed(
            GraphAuthoringDomainId domainId,
            GraphAuthoringDocumentRoleId documentRoleId)
        {
            return m_Descriptors.Values
                .Where(value => value.DomainId.Equals(domainId) && value.Allows(documentRoleId))
                .OrderBy(value => value.Category, StringComparer.Ordinal)
                .ThenBy(value => value.DisplayName, StringComparer.Ordinal)
                .ThenBy(value => value.CapabilityId.Value, StringComparer.Ordinal)
                .ToArray();
        }

        public GraphAuthoringFieldDescriptor RequireField(
            GraphAuthoringCapabilityId capabilityId,
            GraphAuthoringFieldId fieldId,
            bool requireWritable)
        {
            GraphAuthoringCapabilityDescriptor descriptor = Require(capabilityId);
            if (!descriptor.TryGetField(fieldId, out GraphAuthoringFieldDescriptor field))
                throw new InvalidOperationException($"Capability '{capabilityId}' does not declare field '{fieldId}'.");
            if (requireWritable && !field.AuthoringWritable)
                throw new InvalidOperationException($"Capability field '{capabilityId}.{fieldId}' is read-only.");
            return field;
        }

        public GraphAuthoringPortDescriptor RequireFixedPort(
            GraphAuthoringCapabilityId capabilityId,
            GraphAuthoringPortId portId)
        {
            GraphAuthoringCapabilityDescriptor descriptor = Require(capabilityId);
            if (!descriptor.TryGetFixedPort(portId, out GraphAuthoringPortDescriptor port))
                throw new InvalidOperationException($"Capability '{capabilityId}' does not declare fixed port '{portId}'.");
            return port;
        }
    }

    public static class GraphAuthoringCapabilityRegistrationRoot
    {
        static readonly GraphAuthoringCapabilityCatalog s_Catalog = new GraphAuthoringCapabilityCatalog();
        static readonly HashSet<string> s_Registrations = new HashSet<string>(StringComparer.Ordinal);

        public static GraphAuthoringCapabilityCatalog Catalog => s_Catalog;

        public static void RegisterDomain(string registrationId, Action<GraphAuthoringCapabilityCatalog> register)
        {
            string identity = GraphAuthoringIdentity.Require(registrationId, nameof(registrationId));
            if (register == null)
                throw new ArgumentNullException(nameof(register));
            if (!s_Registrations.Add(identity))
                throw new InvalidOperationException($"Graph authoring domain registration '{identity}' already ran.");
            register(s_Catalog);
        }
    }
}
