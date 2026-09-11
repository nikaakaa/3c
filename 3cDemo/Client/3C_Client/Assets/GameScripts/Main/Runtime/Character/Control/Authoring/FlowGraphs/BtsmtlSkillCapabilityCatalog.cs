#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using FlowCanvas.Nodes;
using NodeCanvas.Framework;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonGameplay.Effects;
using ThirdPersonSimulation;
using TreeDesigner.Authoring;

namespace ThirdPersonCharacter.Control.Authoring
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class BtsmtlSkillNodeKindAttribute : Attribute
    {
        public BtsmtlSkillNodeKindAttribute(string kind)
        {
            Kind = kind ?? string.Empty;
        }

        public string Kind { get; }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class BtsmtlSkillNodeVariantAttribute : Attribute
    {
        public BtsmtlSkillNodeVariantAttribute(string fieldId, string value)
        {
            FieldId = fieldId ?? string.Empty;
            Value = value ?? string.Empty;
        }

        public string FieldId { get; }
        public string Value { get; }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public sealed class BtsmtlSkillGraphReferenceAttribute : Attribute
    {
        public BtsmtlSkillGraphReferenceAttribute(
            string fieldId,
            BtsmtlSkillFlowGraphRole role)
        {
            FieldId = fieldId ?? string.Empty;
            Role = role;
        }

        public string FieldId { get; }
        public BtsmtlSkillFlowGraphRole Role { get; }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public sealed class BtsmtlSkillAuthoringFieldAttribute : Attribute
    {
        public BtsmtlSkillAuthoringFieldAttribute(
            string fieldId,
            GraphAuthoringFieldValueKind valueKind)
        {
            FieldId = fieldId ?? string.Empty;
            ValueKind = valueKind;
        }

        public BtsmtlSkillAuthoringFieldAttribute(string fieldId, Type enumType)
            : this(fieldId, GraphAuthoringFieldValueKind.Enum)
        {
            EnumType = enumType;
        }

        public BtsmtlSkillAuthoringFieldAttribute(
            string fieldId,
            GraphAuthoringFieldValueKind valueKind,
            params string[] allowedValues)
            : this(fieldId, valueKind)
        {
            AllowedValues = allowedValues;
        }

        public string FieldId { get; }
        public GraphAuthoringFieldValueKind ValueKind { get; }
        public string DisplayName { get; set; }
        public Type EnumType { get; set; }
        public Type ObjectType { get; set; }
        public bool Optional { get; set; }
        public bool HasMinimum { get; set; }
        public double Minimum { get; set; }
        public bool HasMaximum { get; set; }
        public double Maximum { get; set; }
        public bool Finite { get; set; }
        public bool NonEmpty { get; set; }
        public string[] AllowedValues { get; set; }
    }

    public readonly struct BtsmtlSkillPortShape
    {
        internal BtsmtlSkillPortShape(string id, Type valueType, bool isFlow, bool isInput, bool multiple, bool required)
        {
            Id = id;
            ValueType = valueType;
            IsFlow = isFlow;
            IsInput = isInput;
            Multiple = multiple;
            Required = required;
        }

        public string Id { get; }
        public Type ValueType { get; }
        public bool IsFlow { get; }
        public bool IsInput { get; }
        public bool Multiple { get; }
        public bool Required { get; }
    }

    public enum BtsmtlSkillProviderKind : byte
    {
        None,
        CharacterControlModule,
        InputProfile,
        GameplayEffectProfile
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class BtsmtlSkillProviderAttribute : Attribute
    {
        public BtsmtlSkillProviderAttribute(BtsmtlSkillProviderKind kind)
        {
            Kind = kind;
        }

        public BtsmtlSkillProviderKind Kind { get; }
    }

    public static class BtsmtlSkillProviderContract
    {
        public static BtsmtlSkillProviderKind Resolve(FlowNode node) =>
            node != null &&
            BtsmtlSkillCapabilityCatalog.TryGetKind(node.GetType(), out _)
                ? BtsmtlSkillCapabilityCatalog.ProviderKind(node.GetType())
                : BtsmtlSkillProviderKind.None;

        public static bool Matches(
            string capability,
            string owner,
            string controlModuleId,
            string inputProviderOwnerId,
            string gameplayProviderOwnerId)
        {
            BtsmtlSkillProviderKind providerKind =
                BtsmtlSkillCapabilityCatalog.TryResolveType(capability, out Type type)
                    ? BtsmtlSkillCapabilityCatalog.ProviderKind(type)
                    : BtsmtlSkillProviderKind.None;
            return Matches(
                providerKind,
                owner,
                controlModuleId,
                inputProviderOwnerId,
                gameplayProviderOwnerId);
        }

        public static bool Matches(
            BtsmtlSkillProviderKind kind,
            string owner,
            string controlModuleId,
            string inputProviderOwnerId,
            string gameplayProviderOwnerId)
        {
            return kind switch
            {
                BtsmtlSkillProviderKind.None => true,
                BtsmtlSkillProviderKind.CharacterControlModule =>
                    CharacterStateProviderFields.IsOwnerForModule(owner, controlModuleId),
                BtsmtlSkillProviderKind.InputProfile =>
                    CharacterSkillProviderOwners.IsAssetOwner(owner, inputProviderOwnerId),
                BtsmtlSkillProviderKind.GameplayEffectProfile =>
                    CharacterSkillProviderOwners.IsAssetOwner(owner, gameplayProviderOwnerId),
                _ => false
            };
        }

        public static string MissingCode(BtsmtlSkillProviderKind kind) =>
            kind == BtsmtlSkillProviderKind.CharacterControlModule
                ? "character_state_owner_missing"
                : "skill_provider_owner_missing";

        public static string InvalidCode(BtsmtlSkillProviderKind kind) => kind switch
        {
            BtsmtlSkillProviderKind.CharacterControlModule => "character_state_owner_invalid",
            BtsmtlSkillProviderKind.InputProfile => "skill_input_provider_owner_invalid",
            BtsmtlSkillProviderKind.GameplayEffectProfile => "skill_ability_provider_owner_invalid",
            _ => "skill_provider_owner_invalid"
        };

        public static string InvalidMessage(BtsmtlSkillProviderKind kind) => kind switch
        {
            BtsmtlSkillProviderKind.CharacterControlModule => "Character State provider owner必须是当前ControlModule。",
            BtsmtlSkillProviderKind.InputProfile => "Skill Input provider owner必须是当前CharacterInputProfile资产。",
            BtsmtlSkillProviderKind.GameplayEffectProfile => "Ability provider owner必须是当前CharacterGameplayEffectProfile资产。",
            _ => "Skill外部provider owner无效。"
        };
    }

    public static class BtsmtlSkillCapabilityCatalog
    {
        static readonly IReadOnlyDictionary<Type, string> s_ByType = CreateTypes();
        static readonly IReadOnlyDictionary<string, Type> s_ByKind =
            s_ByType.GroupBy(value => value.Value, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First().Key, StringComparer.Ordinal);
        static readonly IReadOnlyDictionary<string, Type> s_ByVariant = CreateVariants(s_ByType);

        public static IReadOnlyList<Type> All => s_ByType.Keys
            .OrderBy(value => value.FullName, StringComparer.Ordinal)
            .ToArray();

        public static bool TryGetKind(Type type, out string kind)
        {
            if (type != null && s_ByType.TryGetValue(type, out kind))
                return true;
            kind = null;
            return false;
        }

        public static bool TryGetKind(FlowNode node, out string kind) =>
            TryGetKind(node?.GetType(), out kind);

        public static bool TryResolveType(string kind, out Type type)
        {
            if (s_ByKind.TryGetValue(kind ?? string.Empty, out type))
                return true;
            type = null;
            return false;
        }

        public static bool TryResolveType(
            string kind,
            string fieldId,
            string value,
            out Type type)
        {
            if (!string.IsNullOrEmpty(fieldId) &&
                s_ByVariant.TryGetValue(VariantKey(kind, fieldId, value), out type))
                return true;
            return TryResolveType(kind, out type);
        }

        public static bool IsAnchor(string kind) =>
            s_ByKind.TryGetValue(kind ?? string.Empty, out Type type) &&
            IsAnchor(type);

        public static bool IsAnchor(Type type) =>
            typeof(IBtsmtlSkillSystemNode).IsAssignableFrom(type) ||
            typeof(MacroInputNode).IsAssignableFrom(type) ||
            typeof(MacroOutputNode).IsAssignableFrom(type);

        public static IReadOnlyList<BtsmtlSkillNodeAuthoringRuleAttribute>
            AuthoringRules(Type type) =>
            type?.GetCustomAttributes(
                    typeof(BtsmtlSkillNodeAuthoringRuleAttribute),
                    true)
                .OfType<BtsmtlSkillNodeAuthoringRuleAttribute>()
                .ToArray() ??
            Array.Empty<BtsmtlSkillNodeAuthoringRuleAttribute>();

        public static IReadOnlyList<BtsmtlSkillNodeAuthoringReferenceAttribute>
            AuthoringReferences(Type type) =>
            type?.GetCustomAttributes(
                    typeof(BtsmtlSkillNodeAuthoringReferenceAttribute),
                    true)
                .OfType<BtsmtlSkillNodeAuthoringReferenceAttribute>()
                .ToArray() ??
            Array.Empty<BtsmtlSkillNodeAuthoringReferenceAttribute>();

        public static IReadOnlyList<BtsmtlSkillGraphReferenceAttribute>
            GraphReferences(Type type) =>
            type?.GetCustomAttributes(
                    typeof(BtsmtlSkillGraphReferenceAttribute),
                    true)
                .OfType<BtsmtlSkillGraphReferenceAttribute>()
                .ToArray() ??
            Array.Empty<BtsmtlSkillGraphReferenceAttribute>();

        public static BtsmtlSkillProviderKind ProviderKind(Type type) =>
            type?.GetCustomAttributes(
                    typeof(BtsmtlSkillProviderAttribute),
                    true)
                .OfType<BtsmtlSkillProviderAttribute>()
                .Select(value => value.Kind)
                .FirstOrDefault() ?? BtsmtlSkillProviderKind.None;

        public static IReadOnlyList<GraphAuthoringFieldDescriptor> Fields(
            Type type)
        {
            var fields = type
                .GetCustomAttributes(typeof(BtsmtlSkillAuthoringFieldAttribute), true)
                .OfType<BtsmtlSkillAuthoringFieldAttribute>()
                .GroupBy(value => value.FieldId, StringComparer.Ordinal)
                .Select(value => value.First())
                .Select(CreateField)
                .ToList();
            if (fields.Count == 0 && type == typeof(MacroNodeWrapper))
            {
                fields.Add(new GraphAuthoringFieldDescriptor(
                    new GraphAuthoringFieldId("graphId"),
                    "graphId",
                    GraphAuthoringFieldValueKind.IdentityReference,
                    GraphAuthoringFieldAccess.AuthoringRead |
                    GraphAuthoringFieldAccess.AuthoringWrite));
            }
            return fields;
        }

        public static IReadOnlyList<BtsmtlSkillPortShape> ProjectPorts(Type type)
        {
            if (!TryGetKind(type, out _))
                throw new InvalidOperationException($"技能节点类型未登记：{type?.FullName}");
            var node = (FlowNode)Activator.CreateInstance(type);
            node.GatherPorts();
            return ProjectPorts(node);
        }

        public static IReadOnlyList<BtsmtlSkillPortShape> ProjectPorts(FlowNode node)
        {
            if (!TryGetKind(node, out _))
                throw new InvalidOperationException("技能节点类型未登记，不能投影端口。");
            return ProjectPorts(node, true)
                .Concat(ProjectPorts(node, false))
                .OrderBy(value => value.Id, StringComparer.Ordinal)
                .ThenBy(value => value.IsInput ? 0 : 1)
                .ToArray();
        }

        static IEnumerable<BtsmtlSkillPortShape> ProjectPorts(FlowNode node, bool flow)
        {
            IEnumerable<Port> ports = flow
                ? node.GetInputFlowPorts().Cast<Port>().Concat(node.GetOutputFlowPorts().Cast<Port>())
                : node.GetInputValuePorts().Cast<Port>().Concat(node.GetOutputValuePorts().Cast<Port>());
            foreach (Port port in ports)
            {
                bool multiple = port is FlowInput || port is ValueOutput;
                bool required = port is ValueInput input && input.isRequired;
                yield return new BtsmtlSkillPortShape(
                    port.ID,
                    flow ? typeof(Flow) : port.type,
                    flow,
                    port.IsInputPort(),
                    multiple,
                    required);
            }
        }

        static IReadOnlyDictionary<Type, string> CreateTypes()
        {
            var result = new Dictionary<Type, string>();
            foreach (Type type in BtsmtlSkillNodeCatalog.All)
            {
                string kind;
                if (BtsmtlSkillNativeNodeCatalog.TryGet(type, out BtsmtlSkillNativeNodeContract native))
                    kind = native.Kind;
                else if (type == typeof(MacroNodeWrapper))
                    kind = "macro-call";
                else if (typeof(MacroInputNode).IsAssignableFrom(type))
                    kind = "@input";
                else if (typeof(MacroOutputNode).IsAssignableFrom(type))
                    kind = "@output";
                else if (!TryGetDeclaredKind(type, out kind))
                    continue;
                if (!result.TryAdd(type, kind))
                    throw new InvalidOperationException(
                        $"技能节点类型 '{type.FullName}' 的authoring kind重复。");
            }
            return result;
        }

        static bool TryGetDeclaredKind(Type type, out string kind)
        {
            BtsmtlSkillNodeKindAttribute attribute = type
                .GetCustomAttributes(typeof(BtsmtlSkillNodeKindAttribute), true)
                .OfType<BtsmtlSkillNodeKindAttribute>()
                .FirstOrDefault();
            kind = attribute?.Kind;
            return !string.IsNullOrWhiteSpace(kind);
        }

        static IReadOnlyDictionary<string, Type> CreateVariants(
            IReadOnlyDictionary<Type, string> capabilities)
        {
            var result = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (KeyValuePair<Type, string> capability in capabilities)
                foreach (BtsmtlSkillNodeVariantAttribute variant in capability.Key
                             .GetCustomAttributes(typeof(BtsmtlSkillNodeVariantAttribute), true)
                             .OfType<BtsmtlSkillNodeVariantAttribute>())
                    result.Add(VariantKey(capability.Value, variant.FieldId, variant.Value), capability.Key);
            return result;
        }

        static string VariantKey(string kind, string fieldId, string value) =>
            (kind ?? string.Empty) + "\0" + (fieldId ?? string.Empty) + "\0" + (value ?? string.Empty);

        static GraphAuthoringFieldDescriptor CreateField(
            BtsmtlSkillAuthoringFieldAttribute field)
        {
            IReadOnlyList<string> allowedValues = field.AllowedValues;
            if ((allowedValues == null || allowedValues.Count == 0) &&
                field.EnumType != null &&
                field.EnumType.IsEnum)
                allowedValues = Enum.GetNames(field.EnumType);
            GraphAuthoringFieldConstraint constraint = new GraphAuthoringFieldConstraint(
                field.HasMinimum ? field.Minimum : (double?)null,
                field.HasMaximum ? field.Maximum : (double?)null,
                field.Finite,
                field.NonEmpty,
                allowedValues);
            return new GraphAuthoringFieldDescriptor(
                new GraphAuthoringFieldId(field.FieldId),
                string.IsNullOrWhiteSpace(field.DisplayName)
                    ? field.FieldId
                    : field.DisplayName,
                field.ValueKind,
                GraphAuthoringFieldAccess.AuthoringRead |
                GraphAuthoringFieldAccess.AuthoringWrite,
                constraint: constraint,
                optional: field.Optional,
                objectType: field.ObjectType);
        }
    }
}
#endif
