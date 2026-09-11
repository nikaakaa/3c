#if UNITY_EDITOR
using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using TreeDesigner.Authoring;

namespace ThirdPersonCharacter.Control.Authoring
{
    public static class BtsmtlAuthoringFieldValueValidation
    {
        public static bool Matches(
            GraphAuthoringFieldDescriptor field,
            JToken value)
        {
            if (field == null)
                return false;
            if (value == null || value.Type == JTokenType.Null)
                return field.Optional;
            bool valid = field.ValueKind switch
            {
                GraphAuthoringFieldValueKind.String or
                    GraphAuthoringFieldValueKind.Enum =>
                    value.Type == JTokenType.String,
                GraphAuthoringFieldValueKind.IdentityReference =>
                    value.Type == JTokenType.String || value.Type == JTokenType.Object,
                GraphAuthoringFieldValueKind.Boolean =>
                    value.Type == JTokenType.Boolean,
                GraphAuthoringFieldValueKind.Integer =>
                    value.Type == JTokenType.Integer,
                GraphAuthoringFieldValueKind.Float =>
                    value.Type == JTokenType.Integer || value.Type == JTokenType.Float,
                GraphAuthoringFieldValueKind.Vector2 =>
                    Vector(value, "x", "y"),
                GraphAuthoringFieldValueKind.Vector3 =>
                    Vector(value, "x", "y", "z"),
                GraphAuthoringFieldValueKind.Quaternion =>
                    Vector(value, "x", "y", "z", "w"),
                GraphAuthoringFieldValueKind.AssetReference or
                    GraphAuthoringFieldValueKind.Object =>
                    value.Type == JTokenType.Object || value.Type == JTokenType.Array,
                _ => false
            };
            if (!valid)
                return false;
            if (value.Type == JTokenType.String)
            {
                string text = value.Value<string>();
                if (field.Constraint.NonEmpty && string.IsNullOrWhiteSpace(text))
                    return false;
                if (field.Constraint.AllowedValues.Count > 0 &&
                    !field.Constraint.AllowedValues.Contains(text, StringComparer.Ordinal))
                    return false;
            }
            if (value.Type != JTokenType.Integer && value.Type != JTokenType.Float)
                return true;
            double number = value.Value<double>();
            if (field.Constraint.Finite &&
                (double.IsNaN(number) || double.IsInfinity(number)))
                return false;
            return (!field.Constraint.Minimum.HasValue ||
                    number >= field.Constraint.Minimum.Value) &&
                   (!field.Constraint.Maximum.HasValue ||
                    number <= field.Constraint.Maximum.Value);
        }

        static bool Vector(JToken value, params string[] fields)
        {
            if (value is not JObject objectValue ||
                !objectValue.Properties().Select(property => property.Name)
                    .ToHashSet(StringComparer.Ordinal)
                    .SetEquals(fields))
                return false;
            return fields.All(field =>
                objectValue[field]?.Type is JTokenType.Integer or JTokenType.Float);
        }
    }
}
#endif
