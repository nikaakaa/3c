using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Editor;
using TreeDesigner.Authoring;
using TreeDesigner.Editor;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal sealed class AgentGraphPackageProjection
    {
        readonly BtsmtlGraphAuthoringCapabilities m_Catalog =
            new BtsmtlGraphAuthoringCapabilities();

        public IReadOnlyList<AgentPackageNodeKindDescriptor> ExportNodeKinds(
            string domain)
        {
            return m_Catalog.SharedCatalog
                .GetDomain(BtsmtlGraphAuthoringCapabilities.SharedDomain)
                .Where(value =>
                    !value.SystemOwned &&
                    value.AuthoringType != null &&
                    !string.IsNullOrWhiteSpace(value.MutationBindingId) &&
                    AgentAuthoringSchema.IsDomain(domain) &&
                    m_Catalog.IsNodeTypeAllowed(value.AuthoringType))
                .OrderBy(value => value.ExternalKind, StringComparer.Ordinal)
                .Select(value => new AgentPackageNodeKindDescriptor
                {
                    kind = value.ExternalKind,
                    graphKinds = BtsmtlGraphAuthoringRoles.All
                        .Where(graphKind =>
                            AgentAuthoringSchema.IsDomain(domain) &&
                            m_Catalog.IsGraphKindAllowed(graphKind) &&
                            value.Allows(BtsmtlGraphAuthoringCapabilities.SharedRoleId(graphKind)))
                        .OrderBy(graphKind => graphKind, StringComparer.Ordinal)
                        .ToList(),
                    properties = value.Fields
                        .Where(field => field.AuthoringWritable)
                        .Select(field => field.FieldId.Value)
                        .OrderBy(field => field, StringComparer.Ordinal)
                        .ToList(),
                    defaults = Defaults(value.Fields),
                    flowPorts = ToPackagePorts(value.FixedPorts, false),
                    propertyPorts = ToPackagePorts(value.FixedPorts, true),
                    portVariants = ToPackagePortVariants(value.PortVariants),
                    canCreate = true,
                    canConfigure = true,
                    canDelete = true
                })
                .ToList();
        }

        public IReadOnlyList<AgentPackageGraphKindDescriptor> ExportGraphKinds(
            string domain)
        {
            return BtsmtlGraphAuthoringRoles.All
                .Where(value =>
                    AgentAuthoringSchema.IsDomain(domain) &&
                    m_Catalog.IsGraphKindAllowed(value))
                .OrderBy(value => value, StringComparer.Ordinal)
                .Select(value => new AgentPackageGraphKindDescriptor
                {
                    kind = value,
                    ownerSlot = m_Catalog.OwnerSlot(value),
                    nodeKinds = m_Catalog.SharedCatalog
                        .GetDomain(BtsmtlGraphAuthoringCapabilities.SharedDomain)
                        .Where(descriptor =>
                            !descriptor.SystemOwned &&
                            descriptor.AuthoringType != null &&
                            descriptor.Allows(BtsmtlGraphAuthoringCapabilities.SharedRoleId(value)) &&
                            !string.IsNullOrWhiteSpace(descriptor.MutationBindingId) &&
                            AgentAuthoringSchema.IsDomain(domain) &&
                            m_Catalog.IsNodeTypeAllowed(descriptor.AuthoringType))
                        .Select(descriptor => descriptor.ExternalKind)
                        .OrderBy(kind => kind, StringComparer.Ordinal)
                        .ToList(),
                    anchors = ExportAnchors(value).ToList()
                })
                .ToList();
        }

        IReadOnlyList<AgentPackageAnchorDescriptor> ExportAnchors(
            string graphKind)
        {
            IEnumerable<GraphAuthoringCapabilityDescriptor> anchors =
                m_Catalog.SharedCatalog
                    .GetDomain(BtsmtlGraphAuthoringCapabilities.SharedDomain)
                    .Where(value =>
                        value.SystemOwned &&
                        value.Allows(
                            BtsmtlGraphAuthoringCapabilities.SharedRoleId(
                                graphKind)))
                    .OrderBy(value => value.ExternalKind, StringComparer.Ordinal);
            return anchors.Select(anchor =>
            {
                return new AgentPackageAnchorDescriptor
                {
                    anchor = anchor.ExternalKind,
                    flowPorts = ToPackagePorts(anchor.FixedPorts, false),
                    propertyPorts = ToPackagePorts(anchor.FixedPorts, true)
                };
            }).ToArray();
        }

        static List<AgentPackagePortDescriptor> ToPackagePorts(
            IEnumerable<GraphAuthoringPortDescriptor> ports,
            bool property)
        {
            return (ports ?? Array.Empty<GraphAuthoringPortDescriptor>())
                .Where(port =>
                    BtsmtlSharedGraphPort.TryParse(
                        port.PortId,
                        out bool isProperty,
                        out _) &&
                    isProperty == property)
                .OrderBy(port => port.Order)
                .Select(port => new AgentPackagePortDescriptor
                {
                    key = BtsmtlSharedGraphPort.TryParse(
                        port.PortId,
                        out _,
                        out string name)
                        ? name
                        : port.PortId.Value,
                    direction = port.Direction.ToString(),
                    valueType = property
                        ? StableValueType(port.ValueTypeId)
                        : string.Empty,
                    capacity = port.Capacity.ToString(),
                    required = port.Required
                })
                .ToList();
        }

        static List<AgentPackagePortVariantDescriptor> ToPackagePortVariants(
            IReadOnlyList<GraphAuthoringPortVariantDescriptor> variants)
        {
            return (variants ?? Array.Empty<GraphAuthoringPortVariantDescriptor>())
                .Select(variant => new AgentPackagePortVariantDescriptor
                {
                    id = variant.VariantId,
                    when = new AgentPackagePortVariantCondition
                    {
                        field = variant.When.FieldId.Value,
                        valueKind = variant.When.ValueKind.ToString(),
                        equals = variant.When.ExpectedValue
                    },
                    flowPorts = ToPackagePorts(variant.Ports, false),
                    propertyPorts = ToPackagePorts(variant.Ports, true)
                })
                .ToList();
        }

        static string StableValueType(string valueType)
        {
            if (valueType == "btsmtl.property")
                return "property";
            if (valueType == "btsmtl.flow")
                return string.Empty;
            if (string.IsNullOrWhiteSpace(valueType))
                return "object";
            return valueType;
        }

        static JObject Defaults(
            IEnumerable<GraphAuthoringFieldDescriptor> fields)
        {
            var result = new JObject();
            foreach (GraphAuthoringFieldDescriptor field in fields ?? Array.Empty<GraphAuthoringFieldDescriptor>())
                if (field.DefaultValue != null)
                    result[field.FieldId.Value] = JToken.FromObject(field.DefaultValue);
            return result.Count == 0 ? null : result;
        }
    }
}
