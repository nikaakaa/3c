#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using FlowCanvas.Nodes;
using NodeCanvas.Framework;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner.Authoring;
using UnityEngine;

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
        public TimelineExecutionDomainMask TimelineDomains { get; set; } = TimelineExecutionDomainMask.Logic;
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
        public bool HasDefaultValue { get; set; }
        public string DefaultValue { get; set; }
        public string PickerKind { get; set; }
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
            CreateKindIndex(s_ByType);
        static readonly IReadOnlyDictionary<string, Type> s_ByVariant = CreateVariants(s_ByType);

        public static IReadOnlyList<Type> All => s_ByType.Keys
            .OrderBy(value => value.FullName, StringComparer.Ordinal)
            .ToArray();

        public static IReadOnlyList<string> Kinds => s_ByKind.Keys
            .OrderBy(value => value, StringComparer.Ordinal)
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

        public static TimelineExecutionDomainMask TimelineDomains(Type type)
        {
            if (!TryGetKind(type, out _))
                return TimelineExecutionDomainMask.None;
            if (BtsmtlSkillNativeNodeCatalog.TryGet(type, out _) ||
                type == typeof(MacroNodeWrapper) ||
                typeof(MacroInputNode).IsAssignableFrom(type) ||
                typeof(MacroOutputNode).IsAssignableFrom(type))
                return TimelineExecutionDomainMask.Logic | TimelineExecutionDomainMask.Presentation;
            return ((BtsmtlSkillNodeKindAttribute)Attribute.GetCustomAttribute(
                type, typeof(BtsmtlSkillNodeKindAttribute), true)).TimelineDomains;
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
                .OrderBy(value => value.FieldId, StringComparer.Ordinal)
                .ThenBy(value => value.Rule)
                .ToArray() ??
            Array.Empty<BtsmtlSkillNodeAuthoringRuleAttribute>();

        public static IReadOnlyList<BtsmtlSkillNodeAuthoringReferenceAttribute>
            AuthoringReferences(Type type) =>
            type?.GetCustomAttributes(
                    typeof(BtsmtlSkillNodeAuthoringReferenceAttribute),
                    true)
                .OfType<BtsmtlSkillNodeAuthoringReferenceAttribute>()
                .OrderBy(value => value.FieldId, StringComparer.Ordinal)
                .ToArray() ??
            Array.Empty<BtsmtlSkillNodeAuthoringReferenceAttribute>();

        public static IReadOnlyList<BtsmtlSkillGraphReferenceAttribute>
            GraphReferences(Type type) =>
            type?.GetCustomAttributes(
                    typeof(BtsmtlSkillGraphReferenceAttribute),
                    true)
                .OfType<BtsmtlSkillGraphReferenceAttribute>()
                .OrderBy(value => value.FieldId, StringComparer.Ordinal)
                .ThenBy(value => value.Role)
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
            var attributes = type
                .GetCustomAttributes(typeof(BtsmtlSkillAuthoringFieldAttribute), true)
                .OfType<BtsmtlSkillAuthoringFieldAttribute>()
                .GroupBy(value => value.FieldId, StringComparer.Ordinal)
                .Select(value => ResolveField(type, value.Key, value.ToArray()))
                .ToArray();
            var fields = attributes
                .Select(value => CreateField(type, value))
                .ToList();
            if (fields.Count == 0 && type == typeof(MacroNodeWrapper))
            {
                fields.Add(new GraphAuthoringFieldDescriptor(
                    new GraphAuthoringFieldId("graphId"),
                    "graphId",
                    GraphAuthoringFieldValueKind.IdentityReference,
                    GraphAuthoringFieldAccess.AuthoringRead |
                    GraphAuthoringFieldAccess.AuthoringWrite,
                    GraphAuthoringDetailsSection.Authoring,
                    string.Empty,
                    new GraphAuthoringFieldConstraint(null, null, false, true),
                    "graph"));
            }
            return fields;
        }

        public static IReadOnlyList<GraphAuthoringDynamicPortProjection> ProjectPorts(Type type)
        {
            if (!TryGetKind(type, out _))
                throw new InvalidOperationException($"技能节点类型未登记：{type?.FullName}");
            var node = (FlowNode)Activator.CreateInstance(type);
            node.GatherPorts();
            return ProjectPorts(node);
        }

        public static IReadOnlyList<GraphAuthoringDynamicPortProjection> ProjectPorts(FlowNode node)
        {
            if (!TryGetKind(node, out _))
                throw new InvalidOperationException("技能节点类型未登记，不能投影端口。");
            return ProjectPorts(node, true)
                .Concat(ProjectPorts(node, false))
                .OrderBy(value => value.PortId.Value, StringComparer.Ordinal)
                .ThenBy(value => value.Direction == GraphAuthoringPortDirection.Input ? 0 : 1)
                .Select((value, index) => new GraphAuthoringDynamicPortProjection(
                    value.PortId,
                    value.DisplayName,
                    value.ValueTypeId,
                    value.Direction,
                    value.Capacity,
                    value.Required,
                    index,
                    value.InterfacePortId))
                .ToArray();
        }

        static IEnumerable<GraphAuthoringDynamicPortProjection> ProjectPorts(FlowNode node, bool flow)
        {
            IEnumerable<Port> ports = flow
                ? node.GetInputFlowPorts().Cast<Port>().Concat(node.GetOutputFlowPorts().Cast<Port>())
                : node.GetInputValuePorts().Cast<Port>().Concat(node.GetOutputValuePorts().Cast<Port>());
            foreach (Port port in ports)
            {
                bool multiple = port is FlowInput || port is ValueOutput ||
                    node is IBtsmtlSkillStateStructureNode &&
                    port is FlowOutput &&
                    string.Equals(port.ID, "Transfer", StringComparison.Ordinal);
                bool required = port is ValueInput input && input.isRequired;
                yield return new GraphAuthoringDynamicPortProjection(
                    new GraphAuthoringPortId(port.ID),
                    port.ID,
                    flow ? "flow" : BtsmtlSkillGraphAuthoringMetadata.ValueType(port.type),
                    port.IsInputPort()
                        ? GraphAuthoringPortDirection.Input
                        : GraphAuthoringPortDirection.Output,
                    multiple
                        ? GraphAuthoringPortCapacity.Multiple
                        : GraphAuthoringPortCapacity.Single,
                    required,
                    0);
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

        static IReadOnlyDictionary<string, Type> CreateKindIndex(
            IReadOnlyDictionary<Type, string> capabilities)
        {
            var result = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (IGrouping<string, KeyValuePair<Type, string>> group in
                     capabilities.GroupBy(value => value.Value, StringComparer.Ordinal))
            {
                KeyValuePair<Type, string>[] types = group.ToArray();
                if (types.Length > 1)
                {
                    var variants = new HashSet<string>(StringComparer.Ordinal);
                    foreach (KeyValuePair<Type, string> value in types)
                    {
                        BtsmtlSkillNodeVariantAttribute[] declared = value.Key
                            .GetCustomAttributes(typeof(BtsmtlSkillNodeVariantAttribute), false)
                            .OfType<BtsmtlSkillNodeVariantAttribute>()
                            .ToArray();
                        if (declared.Length == 0)
                            throw new InvalidOperationException(
                                $"技能节点kind '{group.Key}' 对应多个类型但未声明variant：{value.Key.FullName}");
                        foreach (BtsmtlSkillNodeVariantAttribute variant in declared)
                            if (!variants.Add(VariantKey(group.Key, variant.FieldId, variant.Value)))
                                throw new InvalidOperationException(
                                    $"技能节点kind '{group.Key}' 的variant重复：{variant.FieldId}={variant.Value}");
                    }
                }
                result.Add(
                    group.Key,
                    types.OrderBy(value => value.Key.FullName, StringComparer.Ordinal).First().Key);
            }
            return result;
        }

        static string VariantKey(string kind, string fieldId, string value) =>
            (kind ?? string.Empty) + "\0" + (fieldId ?? string.Empty) + "\0" + (value ?? string.Empty);

        static BtsmtlSkillAuthoringFieldAttribute ResolveField(
            Type type,
            string fieldId,
            IReadOnlyList<BtsmtlSkillAuthoringFieldAttribute> fields)
        {
            BtsmtlSkillAuthoringFieldAttribute first = fields.First();
            if (fields.Any(value =>
                    value.ValueKind != first.ValueKind ||
                    value.EnumType != first.EnumType ||
                    value.ObjectType != first.ObjectType ||
                    value.Optional != first.Optional ||
                    value.HasMinimum != first.HasMinimum ||
                    value.HasMinimum && value.Minimum != first.Minimum ||
                    value.HasMaximum != first.HasMaximum ||
                    value.HasMaximum && value.Maximum != first.Maximum ||
                    value.Finite != first.Finite ||
                    value.NonEmpty != first.NonEmpty ||
                    !string.Equals(value.DefaultValue, first.DefaultValue, StringComparison.Ordinal) ||
                    value.HasDefaultValue != first.HasDefaultValue ||
                    !string.Equals(value.PickerKind, first.PickerKind, StringComparison.Ordinal) ||
                    !(value.AllowedValues ?? Array.Empty<string>()).SequenceEqual(
                        first.AllowedValues ?? Array.Empty<string>(),
                        StringComparer.Ordinal)))
            {
                throw new InvalidOperationException(
                    $"技能节点类型 '{type.FullName}' 的字段 '{fieldId}' 定义冲突。");
            }
            return first;
        }

        static GraphAuthoringFieldDescriptor CreateField(
            Type type,
            BtsmtlSkillAuthoringFieldAttribute field)
        {
            IReadOnlyList<string> allowedValues = field.AllowedValues;
            if ((allowedValues == null || allowedValues.Count == 0) &&
                field.EnumType != null &&
                field.EnumType.IsEnum)
                allowedValues = Enum.GetNames(field.EnumType);
            BtsmtlSkillNodeAuthoringReferenceAttribute reference =
                AuthoringReferences(type).FirstOrDefault(value => value.FieldId == field.FieldId);
            bool required =
                field.NonEmpty ||
                reference != null && !reference.Optional ||
                GraphReferences(type).Any(value => value.FieldId == field.FieldId && !field.Optional) ||
                field.FieldId == "providerOwnerId" && ProviderKind(type) != BtsmtlSkillProviderKind.None ||
                typeof(IBtsmtlSkillBlackboardAccessNode).IsAssignableFrom(type) &&
                (field.FieldId == "declarationId" || field.FieldId == "ownerId") ||
                AuthoringRules(type).Any(value =>
                    value.Rule == BtsmtlSkillNodeAuthoringRule.CharacterStateFieldType &&
                    value.FieldId == field.FieldId);
            GraphAuthoringFieldConstraint constraint = new GraphAuthoringFieldConstraint(
                field.HasMinimum ? field.Minimum : (double?)null,
                field.HasMaximum ? field.Maximum : (double?)null,
                field.Finite,
                required,
                allowedValues);
            return new GraphAuthoringFieldDescriptor(
                new GraphAuthoringFieldId(field.FieldId),
                string.IsNullOrWhiteSpace(field.DisplayName)
                    ? field.FieldId
                    : field.DisplayName,
                field.ValueKind,
                GraphAuthoringFieldAccess.AuthoringRead |
                GraphAuthoringFieldAccess.AuthoringWrite,
                defaultValue: DefaultValue(type, field),
                constraint: constraint,
                pickerKind: PickerKind(field, reference),
                optional: field.Optional,
                objectType: field.ObjectType ?? reference?.ObjectType);
        }

        static object DefaultValue(
            Type type,
            BtsmtlSkillAuthoringFieldAttribute field)
        {
            string variant = type
                .GetCustomAttributes(typeof(BtsmtlSkillNodeVariantAttribute), false)
                .OfType<BtsmtlSkillNodeVariantAttribute>()
                .FirstOrDefault(value => value.FieldId == field.FieldId)?.Value;
            if (field.HasDefaultValue)
                return ParseDefault(field, field.DefaultValue);
            if (!string.IsNullOrEmpty(variant))
                return variant;
            if (field.FieldId == "valueType")
            {
                if (type == typeof(BtsmtlSkillBlackboardBooleanFlowNode))
                    return "bool";
                if (type == typeof(BtsmtlSkillBlackboardScalarFlowNode) ||
                    typeof(BtsmtlSkillBlackboardAccessFlowNode).IsAssignableFrom(type))
                    return "float";
            }
            if (field.EnumType != null && field.EnumType.IsEnum)
                return Enum.GetValues(field.EnumType).GetValue(0);
            return field.ValueKind switch
            {
                GraphAuthoringFieldValueKind.String => string.Empty,
                GraphAuthoringFieldValueKind.IdentityReference => string.Empty,
                GraphAuthoringFieldValueKind.Boolean => false,
                GraphAuthoringFieldValueKind.Integer => 0,
                GraphAuthoringFieldValueKind.Float => 0f,
                _ => null
            };
        }

        static object ParseDefault(
            BtsmtlSkillAuthoringFieldAttribute field,
            string value)
        {
            if (value == null)
                throw new ArgumentException($"技能字段 '{field.FieldId}' 的默认值缺失。");
            try
            {
                return field.ValueKind switch
                {
                    GraphAuthoringFieldValueKind.String => value,
                    GraphAuthoringFieldValueKind.IdentityReference => value,
                    GraphAuthoringFieldValueKind.Boolean => bool.Parse(value),
                    GraphAuthoringFieldValueKind.Integer => int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture),
                    GraphAuthoringFieldValueKind.Float => float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture),
                    GraphAuthoringFieldValueKind.Enum when field.EnumType != null =>
                        Enum.Parse(field.EnumType, value, false),
                    GraphAuthoringFieldValueKind.Enum => value,
                    _ => throw new ArgumentException($"技能字段 '{field.FieldId}' 不支持字符串默认值。")
                };
            }
            catch (Exception error) when (error is FormatException || error is OverflowException || error is ArgumentException)
            {
                throw new ArgumentException(
                    $"技能字段 '{field.FieldId}' 的默认值 '{value}' 与 {field.ValueKind} 不匹配。",
                    error);
            }
        }

        static string PickerKind(
            BtsmtlSkillAuthoringFieldAttribute field,
            BtsmtlSkillNodeAuthoringReferenceAttribute reference)
        {
            if (!string.IsNullOrWhiteSpace(field.PickerKind))
                return field.PickerKind;
            if (reference != null)
                return reference.Kind switch
                {
                    BtsmtlSkillNodeAuthoringReferenceKind.InputValue => "input-value",
                    BtsmtlSkillNodeAuthoringReferenceKind.ActionRequest => "action-request",
                    BtsmtlSkillNodeAuthoringReferenceKind.AdmissionProfile => "admission-profile",
                    BtsmtlSkillNodeAuthoringReferenceKind.Asset => "asset",
                    _ => string.Empty
                };
            return field.ValueKind switch
            {
                GraphAuthoringFieldValueKind.IdentityReference => "identity",
                GraphAuthoringFieldValueKind.AssetReference => "asset",
                GraphAuthoringFieldValueKind.Object => "object",
                _ => string.Empty
            };
        }
    }

    public readonly struct BtsmtlSkillTargetSnapshotReference
    {
        public BtsmtlSkillTargetSnapshotReference(string declarationId, string ownerId)
        {
            DeclarationId = declarationId ?? string.Empty;
            OwnerId = ownerId ?? string.Empty;
        }

        public string DeclarationId { get; }
        public string OwnerId { get; }
        public bool IsEmpty => string.IsNullOrEmpty(DeclarationId) && string.IsNullOrEmpty(OwnerId);
    }

    public readonly struct BtsmtlSkillStepAuthoringValue
    {
        public BtsmtlSkillStepAuthoringValue(BtsmtlSkillStepPort step)
        {
            if (step == null)
                throw new ArgumentNullException(nameof(step));
            Id = step.Id;
            Name = step.Name;
            Condition = step.Condition;
            Priority = step.Priority;
            AbortPolicy = step.AbortPolicy;
        }

        public string Id { get; }
        public string Name { get; }
        public BtsmtlSkillFlowGraph Condition { get; }
        public int Priority { get; }
        public ProgramAbortPolicy AbortPolicy { get; }
    }

    public readonly struct BtsmtlSkillAuthoringReferenceValue
    {
        public BtsmtlSkillAuthoringReferenceValue(
            BtsmtlSkillNodeAuthoringReferenceAttribute definition,
            object target)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Target = target;
            Identity = BtsmtlSkillAuthoringValues.IdentityOf(target);
        }

        public BtsmtlSkillNodeAuthoringReferenceAttribute Definition { get; }
        public object Target { get; }
        public string Identity { get; }
        public bool IsMissing => Target == null || Target is UnityEngine.Object asset && !asset;
    }

    public readonly struct BtsmtlSkillAuthoringGraphReferenceValue
    {
        public BtsmtlSkillAuthoringGraphReferenceValue(
            BtsmtlSkillGraphReferenceAttribute definition,
            object target)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Target = target;
            Identity = BtsmtlSkillAuthoringValues.IdentityOf(target);
        }

        public BtsmtlSkillGraphReferenceAttribute Definition { get; }
        public object Target { get; }
        public string Identity { get; }
        public bool IsMissing => Target == null || Target is UnityEngine.Object asset && !asset;
    }

    public static class BtsmtlSkillAuthoringValues
    {
        public static IReadOnlyList<GraphAuthoringFieldValue> ReadFields(FlowNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (!BtsmtlSkillCapabilityCatalog.TryGetKind(node, out string kind))
                throw new InvalidOperationException($"技能节点类型未登记：{node.GetType().FullName}");
            return BtsmtlSkillGraphAuthoringMetadata.Fields(kind)
                .Select(field => new GraphAuthoringFieldValue(
                    field,
                    ReadField(node, field.FieldId.Value)))
                .ToArray();
        }

        public static IReadOnlyList<BtsmtlSkillStepAuthoringValue> ReadSteps(FlowNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            return ((IReadOnlyList<BtsmtlSkillStepPort>)ReadField(node, "steps"))
                .Select(value => new BtsmtlSkillStepAuthoringValue(value))
                .ToArray();
        }

        public static IReadOnlyList<BtsmtlSkillStepPort> ReadStepPorts(FlowNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            return (IReadOnlyList<BtsmtlSkillStepPort>)ReadField(node, "steps");
        }

        public static object ReadField(FlowNode node, string fieldId)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(fieldId))
                throw new ArgumentException("技能字段身份缺失。", nameof(fieldId));
            if (!BtsmtlSkillCapabilityCatalog.TryGetKind(node, out string kind))
                throw new InvalidOperationException($"技能节点类型未登记：{node.GetType().FullName}");
            BtsmtlSkillGraphAuthoringMetadata.RequireField(kind, fieldId);
            return ReadDeclaredField(node, fieldId);
        }

        public static IReadOnlyList<BtsmtlSkillAuthoringReferenceValue> ReadReferences(
            FlowNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (!BtsmtlSkillCapabilityCatalog.TryGetKind(node, out _))
                throw new InvalidOperationException($"技能节点类型未登记：{node.GetType().FullName}");
            return BtsmtlSkillCapabilityCatalog.AuthoringReferences(node.GetType())
                .Select(definition => new BtsmtlSkillAuthoringReferenceValue(
                    definition,
                    ReadReferenceTarget(node, definition)))
                .ToArray();
        }

        public static IReadOnlyList<BtsmtlSkillAuthoringGraphReferenceValue> ReadGraphReferences(
            FlowNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (!BtsmtlSkillGraphAuthoringMetadata.TryGetKind(node.GetType(), out string kind))
                throw new InvalidOperationException($"技能节点类型未登记：{node.GetType().FullName}");
            return BtsmtlSkillGraphAuthoringMetadata.GraphReferences(kind)
                .Select(definition => new BtsmtlSkillAuthoringGraphReferenceValue(
                    definition,
                    ReadGraphReferenceTarget(node, definition)))
                .ToArray();
        }

        static object ReadReferenceTarget(
            FlowNode node,
            BtsmtlSkillNodeAuthoringReferenceAttribute definition)
        {
            if (definition.FieldId == "timelineId" && node is BtsmtlSkillTimelineFlowNode timeline)
                return timeline.TimelineAsset;
            return ReadField(node, definition.FieldId);
        }

        static object ReadGraphReferenceTarget(
            FlowNode node,
            BtsmtlSkillGraphReferenceAttribute definition)
        {
            if (definition.FieldId == "graphId" && node is BtsmtlSkillStateMachineFlowNode stateMachine)
                return stateMachine.StateMachine;
            if (definition.FieldId == "bodyGraphId" && node is BtsmtlSkillStateFlowNode state)
                return state.Body;
            if (definition.FieldId == "graphId" && node is MacroNodeWrapper macro)
                return macro.macro as BtsmtlSkillMacroGraph;
            return ReadField(node, definition.FieldId);
        }

        public static string IdentityOf(object value)
        {
            if (value == null)
                return string.Empty;
            if (value is string text)
                return text ?? string.Empty;
            if (value is BtsmtlSkillTargetSnapshotReference snapshot)
                return snapshot.DeclarationId;
            if (value is BtsmtlSkillBlackboardReference blackboard)
                return blackboard.DeclarationId;
            if (value is GameplayAbilityAdmissionProfile profile)
                return profile.ActionId ?? string.Empty;
            if (value is GameplayEffectDefinition effect)
                return effect.EffectId.Value ?? string.Empty;
            if (value is GameplayTagId tag)
                return tag.Value ?? string.Empty;
            if (value is GameplayAttributeId attribute)
                return attribute.Value ?? string.Empty;
            if (value is IBtsmtlSkillAuthoringGraph graph)
                return graph.AuthoringId ?? string.Empty;
            if (value is TimelineAsset timeline)
                return timeline.Data?.AuthoringId ?? string.Empty;
            if (value is UnityEngine.Object asset)
                return asset ? asset.name : string.Empty;
            return value.ToString() ?? string.Empty;
        }

        static object ReadDeclaredField(FlowNode node, string fieldId)
        {
            switch (fieldId)
            {
                case "steps" when node is BtsmtlSkillCompositeFlowNode composite:
                    return composite.Steps;
                case "stopType" when node is BtsmtlSkillLoopFlowNode loop:
                    return loop.StopType;
                case "mode" when node is BtsmtlSkillParallelFlowNode parallel:
                    return parallel.Mode;
                case "cause" when node is BtsmtlSkillStateExitCauseFlowNode cause:
                    return cause.Cause;
                case "inputId" when node is IBtsmtlSkillInputNode input:
                    return input.InputId;
                case "providerOwnerId" when node is IBtsmtlSkillInputNode input:
                    return input.ProviderOwnerId;
                case "providerOwnerId" when node is BtsmtlSkillMoveFacingAngleFlowNode move:
                    return move.ProviderOwnerId;
                case "providerOwnerId" when node is IBtsmtlSkillCharacterStateNode state:
                    return state.ProviderOwnerId;
                case "fieldId" when node is IBtsmtlSkillCharacterStateNode state:
                    return state.FieldId;
                case "tagId" when node is IGameplayTagAuthoring tag:
                    return tag.Tag.Value;
                case "providerOwnerId" when node is BtsmtlSkillGameplayTagFlowNode tag:
                    return tag.ProviderOwnerId;
                case "query" when node is IGameplayTagQueryAuthoring query:
                    return query.Query;
                case "providerOwnerId" when node is BtsmtlSkillGameplayTagQueryFlowNode query:
                    return query.ProviderOwnerId;
                case "attributeId" when node is IGameplayAttributeAuthoring attribute:
                    return attribute.Attribute.Value;
                case "providerOwnerId" when node is BtsmtlSkillGameplayAttributeFlowNode attribute:
                    return attribute.ProviderOwnerId;
                case "effect" when node is IGameplayEffectApplicationAuthoring apply:
                    return apply.Effect;
                case "actionContext" when node is IActionContextAuthoring context:
                    return context.ActionContext;
                case "predicted" when node is IGameplayEffectApplicationAuthoring apply:
                    return apply.Predicted;
                case "providerOwnerId" when node is BtsmtlSkillApplyGameplayEffectFlowNode apply:
                    return apply.ProviderOwnerId;
                case "selector" when node is IGameplayEffectRemovalAuthoring remove:
                    return remove.Selector;
                case "effect" when node is IGameplayEffectRemovalAuthoring remove:
                    return remove.Effect;
                case "query" when node is IGameplayEffectRemovalAuthoring remove:
                    return remove.EffectTagQuery;
                case "providerOwnerId" when node is BtsmtlSkillRemoveGameplayEffectFlowNode remove:
                    return remove.ProviderOwnerId;
                case "activationEntryId" when node is BtsmtlSkillActivationEntryFlowNode entry:
                    return entry.ActivationEntryId;
                case "windowType" when node is IActionWindowAuthoring window:
                    return window.WindowType;
                case "admissionProfile" when node is BtsmtlSkillCanActivateActionFlowNode admission:
                    return admission.AdmissionProfile;
                case "targetSnapshot" when node is BtsmtlSkillCanActivateActionFlowNode admission:
                    return new BtsmtlSkillTargetSnapshotReference(
                        admission.TargetSnapshotDeclarationId,
                        admission.TargetSnapshotOwnerId);
                case "declarationId" when node is IBtsmtlSkillBlackboardAccessNode blackboard:
                    return blackboard.Variable.DeclarationId;
                case "ownerId" when node is IBtsmtlSkillBlackboardAccessNode blackboard:
                    return blackboard.Variable.OwnerId;
                case "valueType" when node is IBtsmtlSkillBlackboardAccessNode blackboard:
                    return BtsmtlSkillGraphAuthoringMetadata.ValueType(blackboard.ValueType);
                case "accessMode" when node is IBtsmtlSkillBlackboardAccessNode blackboard:
                    return blackboard.Writes ? "set" : "get";
                case "factContext" when node is BtsmtlSkillBlackboardAccessFlowNode blackboard:
                    return blackboard.FactContext;
                case "graphId" when node is BtsmtlSkillStateMachineFlowNode stateMachine:
                    return stateMachine.StateMachine;
                case "bodyGraphId" when node is BtsmtlSkillStateFlowNode state:
                    return state.Body;
                case "timelineId" when node is BtsmtlSkillTimelineFlowNode timeline:
                    return timeline.TimelineAsset;
                case "timelineOwnership" when node is BtsmtlSkillTimelineFlowNode timeline:
                    return timeline.Ownership;
                case "actionContext" when node is BtsmtlSkillTimelineFlowNode timeline:
                    return timeline.ActionContext;
                case "playbackMode" when node is BtsmtlSkillTimelineFlowNode timeline:
                    return timeline.PlaybackMode;
                case "moveSpeed" when node is BtsmtlSkillLocomotionFlowNode locomotion:
                    return locomotion.MoveSpeed;
                case "displacementMode" when node is BtsmtlSkillLocomotionFlowNode locomotion:
                    return locomotion.DisplacementMode;
                case "actionMotionCurve" when node is BtsmtlSkillLocomotionFlowNode locomotion:
                    return locomotion.ActionMotionCurve;
                case "turnSpeedDegrees" when node is BtsmtlSkillLocomotionFlowNode locomotion:
                    return locomotion.TurnSpeedDegrees;
                case "cameraRelative" when node is BtsmtlSkillLocomotionFlowNode locomotion:
                    return locomotion.CameraRelative;
                case "executionMode" when node is BtsmtlSkillLocomotionFlowNode locomotion:
                    return locomotion.ExecutionMode;
                case "durationSeconds" when node is BtsmtlSkillLocomotionFlowNode locomotion:
                    return locomotion.DurationSeconds;
                case "graphId" when node is MacroNodeWrapper macro:
                    return macro.macro is BtsmtlSkillMacroGraph graph
                        ? graph.AuthoringId
                        : string.Empty;
                case "mode" when node is RequestCameraStateNode cameraState:
                    return cameraState.Mode;
                case "sequenceId" when node is RequestCameraStateNode cameraState:
                    return cameraState.SequenceId;
                case "priority" when node is RequestCameraStateNode cameraState:
                    return cameraState.Priority;
                case "weight" when node is RequestCameraStateNode cameraState:
                    return cameraState.Weight;
                case "blendInSeconds" when node is RequestCameraStateNode cameraState:
                    return cameraState.BlendInSeconds;
                case "blendOutSeconds" when node is RequestCameraStateNode cameraState:
                    return cameraState.BlendOutSeconds;
                case "targetKey" when node is RequestCameraStateNode cameraState:
                    return cameraState.TargetKey;
                case "interruptPolicy" when node is RequestCameraStateNode cameraState:
                    return cameraState.InterruptPolicy;
                case "actionContext" when node is RequestCameraStateNode cameraState:
                    return cameraState.ActionContext;
                case "requestId" when node is RequestCameraEffectNode cameraEffect:
                    return cameraEffect.RequestId;
                case "effectKind" when node is RequestCameraEffectNode cameraEffect:
                    return cameraEffect.EffectKind;
                case "resourceId" when node is RequestCameraEffectNode cameraEffect:
                    return cameraEffect.ResourceId;
                case "weight" when node is RequestCameraEffectNode cameraEffect:
                    return cameraEffect.Weight;
                case "priority" when node is RequestCameraEffectNode cameraEffect:
                    return cameraEffect.Priority;
                case "actionContext" when node is RequestCameraEffectNode cameraEffect:
                    return cameraEffect.ActionContext;
                default:
                    throw new InvalidOperationException(
                        $"Skill节点 '{node.GetType().Name}' 没有字段 '{fieldId}' 的正式读取入口。");
            }
        }
    }
}
#endif
