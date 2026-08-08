using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TreeDesigner.Editor;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public static class AgentPackageNodeCatalogValidator
    {
        public static bool Validate(
            AgentPackageNodeCatalogFile catalog,
            AgentCompileReport report,
            string path = "context/node-catalog.json")
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));
            if (catalog?.kinds == null)
            {
                report.Error(path, "node_catalog_missing", "Node Catalog 缺少 kinds。");
                return false;
            }

            bool valid = true;
            var kindIds = new HashSet<string>(StringComparer.Ordinal);
            for (int kindIndex = 0; kindIndex < catalog.kinds.Count; kindIndex++)
            {
                AgentPackageNodeKindDescriptor kind = catalog.kinds[kindIndex];
                string kindPath = $"{path}.kinds[{kindIndex}]";
                if (kind == null || string.IsNullOrWhiteSpace(kind.kind) || !kindIds.Add(kind.kind))
                {
                    report.Error(kindPath, "node_catalog_kind_invalid", "Node kind identity 缺失或重复。");
                    valid = false;
                    continue;
                }

                var properties = new HashSet<string>(StringComparer.Ordinal);
                foreach (string property in kind.properties ?? new List<string>())
                {
                    if (string.IsNullOrWhiteSpace(property) || !properties.Add(property))
                    {
                        report.Error(kindPath + ".properties", "node_catalog_property_invalid", "Node property identity 缺失或重复。");
                        valid = false;
                    }
                }

                var fixedPorts = new HashSet<string>(StringComparer.Ordinal);
                valid &= ValidatePorts(kind.flowPorts, false, fixedPorts, kindPath + ".flowPorts", report);
                valid &= ValidatePorts(kind.propertyPorts, true, fixedPorts, kindPath + ".propertyPorts", report);
                valid &= ValidateVariants(kind, properties, fixedPorts, kindPath, report);
            }
            return valid;
        }

        static bool ValidateVariants(
            AgentPackageNodeKindDescriptor kind,
            ISet<string> properties,
            ISet<string> fixedPorts,
            string path,
            AgentCompileReport report)
        {
            if (kind.portVariants == null)
            {
                report.Error(path + ".portVariants", "node_catalog_port_variants_missing", "portVariants 不能为 null。");
                return false;
            }
            if (kind.portVariants.Count == 0)
                return true;

            bool valid = true;
            var variantIds = new HashSet<string>(StringComparer.Ordinal);
            var conditions = new HashSet<string>(StringComparer.Ordinal);
            string discriminatorField = null;
            GraphAuthoringFieldValueKind discriminatorKind = default;
            for (int variantIndex = 0; variantIndex < kind.portVariants.Count; variantIndex++)
            {
                AgentPackagePortVariantDescriptor variant = kind.portVariants[variantIndex];
                string variantPath = $"{path}.portVariants[{variantIndex}]";
                if (variant == null || string.IsNullOrWhiteSpace(variant.id) || !variantIds.Add(variant.id))
                {
                    report.Error(variantPath, "node_catalog_port_variant_identity_invalid", "Port variant identity 缺失或重复。");
                    valid = false;
                    continue;
                }
                if (!TryValidateCondition(variant.when, properties, variantPath + ".when", report, out GraphAuthoringFieldValueKind valueKind))
                {
                    valid = false;
                    continue;
                }
                if (discriminatorField == null)
                {
                    discriminatorField = variant.when.field;
                    discriminatorKind = valueKind;
                }
                else if (!string.Equals(discriminatorField, variant.when.field, StringComparison.Ordinal) ||
                         discriminatorKind != valueKind)
                {
                    report.Error(variantPath + ".when", "node_catalog_port_variant_discriminator_mismatch", "同一 node kind 的 portVariants 必须使用同一个 typed discriminator。");
                    valid = false;
                }
                string conditionKey = variant.when.field + "\0" + variant.when.valueKind + "\0" + variant.when.equals;
                if (!conditions.Add(conditionKey))
                {
                    report.Error(variantPath + ".when", "node_catalog_port_variant_ambiguous", "多个 port variant 使用了相同匹配条件。");
                    valid = false;
                }

                var variantPorts = new HashSet<string>(StringComparer.Ordinal);
                valid &= ValidatePorts(variant.flowPorts, false, variantPorts, variantPath + ".flowPorts", report);
                valid &= ValidatePorts(variant.propertyPorts, true, variantPorts, variantPath + ".propertyPorts", report);
                foreach (string port in variantPorts)
                {
                    if (fixedPorts.Contains(port))
                    {
                        report.Error(variantPath, "node_catalog_port_variant_fixed_overlap", $"条件端口与固定端口 identity 重复：{port}");
                        valid = false;
                    }
                }
            }
            return valid;
        }

        static bool TryValidateCondition(
            AgentPackagePortVariantCondition condition,
            ISet<string> properties,
            string path,
            AgentCompileReport report,
            out GraphAuthoringFieldValueKind valueKind)
        {
            valueKind = default;
            string rootField = condition?.field?.Split('.')[0];
            if (condition == null ||
                string.IsNullOrWhiteSpace(condition.field) ||
                string.IsNullOrWhiteSpace(rootField) ||
                !properties.Contains(rootField) ||
                !Enum.TryParse(condition.valueKind, false, out valueKind) ||
                !Enum.IsDefined(typeof(GraphAuthoringFieldValueKind), valueKind) ||
                !IsDiscriminatorKind(valueKind) ||
                !IsConditionValue(valueKind, condition.equals))
            {
                report.Error(path, "node_catalog_port_variant_condition_invalid", "Port variant condition 的 field、valueKind 或 equals 不合法。");
                return false;
            }
            return true;
        }

        static bool ValidatePorts(
            IReadOnlyList<AgentPackagePortDescriptor> ports,
            bool property,
            ISet<string> identities,
            string path,
            AgentCompileReport report)
        {
            if (ports == null)
            {
                report.Error(path, "node_catalog_ports_missing", "Port 列表不能为 null。");
                return false;
            }
            bool valid = true;
            for (int index = 0; index < ports.Count; index++)
            {
                AgentPackagePortDescriptor port = ports[index];
                string portPath = $"{path}[{index}]";
                GraphAuthoringPortDirection direction = default;
                bool directionValid = port != null &&
                    Enum.TryParse(port.direction, false, out direction) &&
                    Enum.IsDefined(typeof(GraphAuthoringPortDirection), direction);
                GraphAuthoringPortCapacity capacity = default;
                bool capacityValid = port != null &&
                    Enum.TryParse(port.capacity, false, out capacity) &&
                    Enum.IsDefined(typeof(GraphAuthoringPortCapacity), capacity);
                string identity = (property ? "property:" : "flow:") + port?.key;
                if (port == null ||
                    string.IsNullOrWhiteSpace(port.key) ||
                    !directionValid ||
                    !capacityValid ||
                    property && string.IsNullOrWhiteSpace(port.valueType) ||
                    !property && !string.IsNullOrEmpty(port.valueType) ||
                    port.required && direction != GraphAuthoringPortDirection.Input ||
                    !identities.Add(identity))
                {
                    report.Error(portPath, "node_catalog_port_invalid", "Port 的 identity、direction、capacity、required 或 valueType 不合法。");
                    valid = false;
                }
            }
            return valid;
        }

        static bool IsDiscriminatorKind(GraphAuthoringFieldValueKind valueKind) =>
            valueKind == GraphAuthoringFieldValueKind.String ||
            valueKind == GraphAuthoringFieldValueKind.Boolean ||
            valueKind == GraphAuthoringFieldValueKind.Integer ||
            valueKind == GraphAuthoringFieldValueKind.Float ||
            valueKind == GraphAuthoringFieldValueKind.Enum ||
            valueKind == GraphAuthoringFieldValueKind.IdentityReference;

        static bool IsConditionValue(GraphAuthoringFieldValueKind valueKind, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            if (valueKind == GraphAuthoringFieldValueKind.Boolean)
                return string.Equals(value, bool.TrueString, StringComparison.Ordinal) ||
                       string.Equals(value, bool.FalseString, StringComparison.Ordinal);
            if (valueKind == GraphAuthoringFieldValueKind.Integer)
                return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
            if (valueKind == GraphAuthoringFieldValueKind.Float)
                return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) &&
                       !double.IsNaN(number) &&
                       !double.IsInfinity(number);
            return true;
        }
    }
}
