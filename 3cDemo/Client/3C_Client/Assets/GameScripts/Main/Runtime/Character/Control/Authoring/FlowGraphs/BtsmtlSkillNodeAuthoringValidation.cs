#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public enum BtsmtlSkillNodeAuthoringRule : byte
    {
        CompositeSteps,
        TimelineReference,
        ActionLifecycleTransition,
        TargetSnapshotObject,
        BlackboardValueType,
        CharacterStateFieldType
    }

    public enum BtsmtlSkillNodeAuthoringReferenceKind : byte
    {
        ActionRequest,
        InputValue,
        AdmissionProfile,
        Asset
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public sealed class BtsmtlSkillNodeAuthoringRuleAttribute : Attribute
    {
        public BtsmtlSkillNodeAuthoringRuleAttribute(BtsmtlSkillNodeAuthoringRule rule)
        {
            Rule = rule;
        }

        public BtsmtlSkillNodeAuthoringRuleAttribute(
            BtsmtlSkillNodeAuthoringRule rule,
            string fieldId,
            string expectedValue = "")
            : this(rule)
        {
            FieldId = fieldId ?? string.Empty;
            ExpectedValue = expectedValue ?? string.Empty;
        }

        public BtsmtlSkillNodeAuthoringRule Rule { get; }
        public string FieldId { get; }
        public string ExpectedValue { get; }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public sealed class BtsmtlSkillNodeAuthoringReferenceAttribute : Attribute
    {
        public BtsmtlSkillNodeAuthoringReferenceAttribute(
            string fieldId,
            BtsmtlSkillNodeAuthoringReferenceKind kind,
            string errorCode,
            string errorMessage)
        {
            FieldId = fieldId ?? string.Empty;
            Kind = kind;
            ErrorCode = errorCode ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public BtsmtlSkillNodeAuthoringReferenceAttribute(
            string fieldId,
            BtsmtlSkillNodeAuthoringReferenceKind kind,
            string errorCode,
            string errorMessage,
            Type objectType)
            : this(fieldId, kind, errorCode, errorMessage)
        {
            ObjectType = objectType;
        }

        public string FieldId { get; }
        public BtsmtlSkillNodeAuthoringReferenceKind Kind { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
        public Type ObjectType { get; }
        public bool Optional { get; set; }
    }

    public readonly struct BtsmtlSkillNodeAuthoringIssue
    {
        public BtsmtlSkillNodeAuthoringIssue(string fieldId, string code, string message)
        {
            FieldId = fieldId ?? string.Empty;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string FieldId { get; }
        public string Code { get; }
        public string Message { get; }
    }

    public static class BtsmtlSkillNodeAuthoringValidation
    {
        public static bool MatchesValue(JToken value, string type)
        {
            if (!BtsmtlSkillGraphAuthoringMetadata.TryResolveValueType(type, out Type valueType))
                return false;
            if (valueType == typeof(bool) || valueType == typeof(string))
                return value.Type == (valueType == typeof(bool)
                    ? JTokenType.Boolean
                    : JTokenType.String);
            if (valueType == typeof(int) || valueType == typeof(uint) || valueType == typeof(ulong))
                return value.Type == JTokenType.Integer;
            if (valueType == typeof(float))
                return value.Type == JTokenType.Integer || value.Type == JTokenType.Float;
            if (valueType == typeof(Vector2))
                return Vector(value, "x", "y");
            if (valueType == typeof(Vector3))
                return Vector(value, "x", "y", "z");
            if (valueType == typeof(ActionTargetSnapshot))
                return ActionTarget(value);
            return false;
        }

        public static bool HasRule(
            string kind,
            BtsmtlSkillNodeAuthoringRule rule,
            string fieldId = null)
        {
            return TryGetRule(kind, rule, fieldId, out _);
        }

        public static bool TryGetRule(
            string kind,
            BtsmtlSkillNodeAuthoringRule rule,
            string fieldId,
            out BtsmtlSkillNodeAuthoringRuleAttribute result)
        {
            result = null;
            if (!BtsmtlSkillCapabilityCatalog.TryResolveType(kind, out Type type))
                return false;
            result = BtsmtlSkillCapabilityCatalog.AuthoringRules(type)
                .FirstOrDefault(value => value.Rule == rule &&
                    (string.IsNullOrEmpty(fieldId) || value.FieldId == fieldId));
            return result != null;
        }

        public static IReadOnlyList<BtsmtlSkillNodeAuthoringReferenceAttribute> References(string kind)
        {
            if (!BtsmtlSkillCapabilityCatalog.TryResolveType(kind, out Type type))
                return Array.Empty<BtsmtlSkillNodeAuthoringReferenceAttribute>();
            return BtsmtlSkillCapabilityCatalog.AuthoringReferences(type);
        }

        public static IReadOnlyList<BtsmtlSkillNodeAuthoringIssue> Validate(
            string kind,
            JObject properties,
            string controlModuleId,
            string inputProviderOwnerId,
            string gameplayProviderOwnerId)
        {
            if (!BtsmtlSkillCapabilityCatalog.TryResolveType(kind, out Type type))
                return new[] { Issue(string.Empty, "skill_node_kind_invalid", "Skill Node kind未在正式metadata中声明。") };
            var issues = new List<BtsmtlSkillNodeAuthoringIssue>();
            JObject value = properties ?? new JObject();
            foreach (BtsmtlSkillNodeAuthoringRuleAttribute rule in
                     BtsmtlSkillCapabilityCatalog.AuthoringRules(type))
            {
                switch (rule.Rule)
                {
                    case BtsmtlSkillNodeAuthoringRule.CompositeSteps:
                        ValidateSteps(value[rule.FieldId] as JArray, rule.FieldId, issues);
                        break;
                    case BtsmtlSkillNodeAuthoringRule.TimelineReference:
                        if (!IsIdentity(value.Value<string>(rule.FieldId)))
                            issues.Add(Issue(rule.FieldId, "skill_timeline_reference_invalid", "Skill Timeline节点必须引用稳定Timeline identity。"));
                        break;
                    case BtsmtlSkillNodeAuthoringRule.ActionLifecycleTransition:
                        if (value.Value<string>(rule.FieldId) == ActionLifecycleTransitionType.None.ToString())
                            issues.Add(Issue(rule.FieldId, "skill_action_transition_invalid", "Skill Action lifecycle transition不能是None。"));
                        break;
                    case BtsmtlSkillNodeAuthoringRule.TargetSnapshotObject:
                        if (value[rule.FieldId] != null && value[rule.FieldId].Type != JTokenType.Object)
                            issues.Add(Issue(rule.FieldId, "skill_target_snapshot_invalid", "Skill CanActivate节点的targetSnapshot必须是object。"));
                        break;
                    case BtsmtlSkillNodeAuthoringRule.BlackboardValueType:
                        if (value.Value<string>(rule.FieldId) != rule.ExpectedValue)
                            issues.Add(Issue(rule.FieldId, "skill_blackboard_node_type_invalid", $"黑板读取节点必须声明{rule.ExpectedValue}类型。"));
                        break;
                    case BtsmtlSkillNodeAuthoringRule.CharacterStateFieldType:
                        if (!IsCharacterStateField(rule.ExpectedValue, value.Value<string>(rule.FieldId)))
                            issues.Add(Issue(rule.FieldId, "character_state_field_type_invalid", "Character State fieldId与节点输出类型不匹配。"));
                        break;
                }
            }
            BtsmtlSkillProviderKind providerKind =
                BtsmtlSkillCapabilityCatalog.ProviderKind(type);
            if (providerKind != BtsmtlSkillProviderKind.None)
            {
                string owner = value.Value<string>("providerOwnerId");
                if (string.IsNullOrWhiteSpace(owner))
                    issues.Add(Issue("providerOwnerId", BtsmtlSkillProviderContract.MissingCode(providerKind), "Skill外部provider引用必须指定稳定owner。"));
                else if (!BtsmtlSkillProviderContract.Matches(
                             providerKind,
                             owner,
                             controlModuleId,
                             inputProviderOwnerId,
                             gameplayProviderOwnerId))
                    issues.Add(Issue(
                        "providerOwnerId",
                        BtsmtlSkillProviderContract.InvalidCode(providerKind),
                        BtsmtlSkillProviderContract.InvalidMessage(providerKind)));
            }
            return issues;
        }

        static void ValidateSteps(
            JArray value,
            string fieldId,
            ICollection<BtsmtlSkillNodeAuthoringIssue> issues)
        {
            if (value == null)
            {
                issues.Add(Issue(fieldId, "skill_steps_missing", "组合节点必须声明稳定steps集合。"));
                return;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (JToken token in value)
            {
                JObject step = token as JObject;
                string id = step?.Value<string>("id");
                if (step == null || !IsIdentity(id) || !ids.Add(id) ||
                    step.Value<string>("name") == null ||
                    !Enum.TryParse(step.Value<string>("abortPolicy"), false, out ProgramAbortPolicy _))
                    issues.Add(Issue(fieldId, "skill_step_invalid", "Skill step必须有唯一id、name和合法abortPolicy。"));
            }
            if (ids.Count != value.Count)
                issues.Add(Issue(fieldId, "skill_step_invalid", "Skill step必须有唯一id、name和合法abortPolicy。"));
        }

        static bool IsCharacterStateField(string kind, string fieldId)
        {
            return kind switch
            {
                "vector3" => CharacterStateProviderFields.IsVector3(fieldId),
                "scalar" => CharacterStateProviderFields.IsScalar(fieldId),
                "yaw" => CharacterStateProviderFields.IsYaw(fieldId),
                "bool" => CharacterStateProviderFields.IsBoolean(fieldId),
                _ => false
            };
        }

        static bool ActionTarget(JToken value)
        {
            if (value is not JObject target)
                return false;
            string[] coordinates = { "x", "y", "z", "rx", "ry", "rz", "rw" };
            return target.Properties().All(property =>
                       property.Name == "targetId" && property.Value.Type == JTokenType.String ||
                       coordinates.Contains(property.Name) &&
                       (property.Value.Type == JTokenType.Integer || property.Value.Type == JTokenType.Float)) &&
                   coordinates.All(field => target[field] != null);
        }

        static bool Vector(JToken value, params string[] fields)
        {
            if (value is not JObject objectValue ||
                !objectValue.Properties().Select(property => property.Name)
                    .ToHashSet(StringComparer.Ordinal).SetEquals(fields))
                return false;
            return fields.All(field => objectValue[field]?.Type == JTokenType.Integer ||
                                       objectValue[field]?.Type == JTokenType.Float);
        }

        static bool IsIdentity(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsWhiteSpace) ||
                value.StartsWith("@", StringComparison.Ordinal))
                return false;
            if (!value.StartsWith("local:", StringComparison.Ordinal))
                return true;
            string local = value.Substring("local:".Length);
            return local.Length > 0 && local.All(character =>
                char.IsLetterOrDigit(character) || character == '-' || character == '_' || character == '.');
        }

        static BtsmtlSkillNodeAuthoringIssue Issue(string fieldId, string code, string message) =>
            new BtsmtlSkillNodeAuthoringIssue(fieldId, code, message);
    }
}
#endif
