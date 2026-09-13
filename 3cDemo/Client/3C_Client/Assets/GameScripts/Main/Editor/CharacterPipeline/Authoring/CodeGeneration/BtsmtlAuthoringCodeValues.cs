using System;
using System.Collections.Generic;
using ThirdPersonCharacter.ActionSystem;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public static class BtsmtlAuthoringCodeValues
    {
        public static string ExternalAsset(
            BtsmtlAuthoringCodeExportContext context,
            UnityEngine.Object asset,
            Type expectedType)
        {
            if (!asset)
                return "null";
            if (expectedType == null || !typeof(UnityEngine.Object).IsAssignableFrom(expectedType))
            {
                context.ReportError("external_dependency_type_invalid", asset.name, "外部资源依赖的正式类型无效。");
                return "null";
            }
            string assetPath = AssetDatabase.GetAssetPath(asset);
            long localFileId = 0L;
            if (string.IsNullOrEmpty(assetPath) ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out _, out localFileId))
            {
                context.ReportError("external_dependency_not_persistent", asset.name, "外部资源必须是已保存的Unity资产。");
                return "null";
            }
            string typeName = BtsmtlAuthoringCodeSyntax.TypeName(expectedType);
            context.AddExternalDependency(assetPath, typeName, localFileId);
            return context.ResolveExternalAsset(assetPath, typeName, localFileId);
        }

        public static string Vector2(Vector2 value) =>
            $"new {BtsmtlAuthoringCodeSyntax.TypeName(typeof(Vector2))}({BtsmtlAuthoringCodeSyntax.FloatLiteral(value.x)}, {BtsmtlAuthoringCodeSyntax.FloatLiteral(value.y)})";

        public static string Vector3(Vector3 value) =>
            $"new {BtsmtlAuthoringCodeSyntax.TypeName(typeof(Vector3))}({BtsmtlAuthoringCodeSyntax.FloatLiteral(value.x)}, {BtsmtlAuthoringCodeSyntax.FloatLiteral(value.y)}, {BtsmtlAuthoringCodeSyntax.FloatLiteral(value.z)})";

        public static string Quaternion(Quaternion value) =>
            $"new {BtsmtlAuthoringCodeSyntax.TypeName(typeof(Quaternion))}({BtsmtlAuthoringCodeSyntax.FloatLiteral(value.x)}, {BtsmtlAuthoringCodeSyntax.FloatLiteral(value.y)}, {BtsmtlAuthoringCodeSyntax.FloatLiteral(value.z)}, {BtsmtlAuthoringCodeSyntax.FloatLiteral(value.w)})";

        public static string AnimationCurve(AnimationCurve value)
        {
            if (value == null)
                return "null";
            var keys = new List<string>();
            foreach (Keyframe key in value.keys)
            {
                keys.Add(
                    "new " + BtsmtlAuthoringCodeSyntax.TypeName(typeof(Keyframe)) + "(" +
                    BtsmtlAuthoringCodeSyntax.FloatLiteral(key.time) + ", " +
                    BtsmtlAuthoringCodeSyntax.FloatLiteral(key.value) + ", " +
                    BtsmtlAuthoringCodeSyntax.FloatLiteral(key.inTangent) + ", " +
                    BtsmtlAuthoringCodeSyntax.FloatLiteral(key.outTangent) + ", " +
                    BtsmtlAuthoringCodeSyntax.FloatLiteral(key.inWeight) + ", " +
                    BtsmtlAuthoringCodeSyntax.FloatLiteral(key.outWeight) +
                    ") { weightedMode = " +
                    BtsmtlAuthoringCodeSyntax.EnumLiteral(
                        BtsmtlAuthoringCodeSyntax.TypeName(typeof(WeightedMode)),
                        key.weightedMode.ToString()) + " }");
            }
            string keyArray = keys.Count == 0
                ? $"new {BtsmtlAuthoringCodeSyntax.TypeName(typeof(Keyframe))}[0]"
                : $"new[] {{ {string.Join(", ", keys)} }}";
            return $"new {BtsmtlAuthoringCodeSyntax.TypeName(typeof(AnimationCurve))}({keyArray}) {{ preWrapMode = " +
                BtsmtlAuthoringCodeSyntax.EnumLiteral(
                    BtsmtlAuthoringCodeSyntax.TypeName(typeof(WrapMode)),
                    value.preWrapMode.ToString()) + ", postWrapMode = " +
                BtsmtlAuthoringCodeSyntax.EnumLiteral(
                    BtsmtlAuthoringCodeSyntax.TypeName(typeof(WrapMode)),
                    value.postWrapMode.ToString()) + " }";
        }

        public static bool TryScalar(
            object value,
            Type type,
            out string expression)
        {
            expression = null;
            if (type == typeof(bool) && value is bool boolean)
            {
                expression = boolean ? "true" : "false";
                return true;
            }
            if (type == typeof(int) && value is int integer)
            {
                expression = integer.ToString(System.Globalization.CultureInfo.InvariantCulture);
                return true;
            }
            if (type == typeof(uint) && value is uint unsignedInteger)
            {
                expression = unsignedInteger.ToString(System.Globalization.CultureInfo.InvariantCulture) + "u";
                return true;
            }
            if (type == typeof(ulong) && value is ulong unsignedLong)
            {
                expression = unsignedLong.ToString(System.Globalization.CultureInfo.InvariantCulture) + "UL";
                return true;
            }
            if (type == typeof(float) && value is float single)
            {
                expression = BtsmtlAuthoringCodeSyntax.FloatLiteral(single);
                return true;
            }
            if (type == typeof(double) && value is double doubleValue)
            {
                expression = BtsmtlAuthoringCodeSyntax.DoubleLiteral(doubleValue);
                return true;
            }
            if (type != null && type.IsEnum && value is Enum enumValue)
            {
                expression = BtsmtlAuthoringCodeSyntax.EnumLiteral(
                    BtsmtlAuthoringCodeSyntax.TypeName(type),
                    enumValue.ToString());
                return true;
            }
            if (type == typeof(string) && value is string text)
            {
                expression = BtsmtlAuthoringCodeSyntax.StringLiteral(text);
                return true;
            }
            if (value == null && !type.IsValueType)
            {
                expression = "null";
                return true;
            }
            return false;
        }

        public static string Value(
            BtsmtlAuthoringCodeExportContext context,
            object value,
            Type type,
            string subject)
        {
            if (TryScalar(value, type, out string scalar))
                return scalar;
            if (type == typeof(Vector2) && value is Vector2 vector2)
                return Vector2(vector2);
            if (type == typeof(Vector3) && value is Vector3 vector3)
                return Vector3(vector3);
            if (type == typeof(Quaternion) && value is Quaternion quaternion)
                return Quaternion(quaternion);
            if (type == typeof(ActionTargetSnapshot) && value is ActionTargetSnapshot target)
                return "new " + BtsmtlAuthoringCodeSyntax.TypeName(typeof(ActionTargetSnapshot)) + "(" +
                    BtsmtlAuthoringCodeSyntax.StringLiteral(target.TargetId) + ", " +
                    Vector3(target.Position) + ", " + Quaternion(target.Rotation) + ")";
            if (type == typeof(AnimationCurve) && value is AnimationCurve curve)
                return AnimationCurve(curve);
            if (value is UnityEngine.Object asset)
                return ExternalAsset(context, asset, type);
            context.ReportError(
                "authoring_value_unsupported",
                subject,
                $"正式值类型无法输出为C#：{type?.FullName ?? "<null>"}。");
            return "null";
        }
    }
}
