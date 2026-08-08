using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public static class AgentAuthoringDocumentCodec
    {
        static readonly UTF8Encoding s_Utf8WithoutBom = new UTF8Encoding(false);
        static readonly JsonSerializer s_Serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Culture = CultureInfo.InvariantCulture,
            FloatFormatHandling = FloatFormatHandling.String,
            FloatParseHandling = FloatParseHandling.Double
        });

        static readonly string[] s_IdentityFields =
        {
            "id",
            "kind",
            "graphAuthoringId",
            "stateAuthoringId",
            "edgeAuthoringId",
            "elementAuthoringId",
            "declarationId",
            "declarationAuthoringId",
            "timelineAuthoringId",
            "trackAuthoringId",
            "clipAuthoringId",
            "authoringId",
            "nodeAuthoringId",
            "actionId",
            "requestId",
            "inputValueId",
            "channelId"
        };

        public static bool TryReadFile<T>(string path, AgentCompileReport report, out T value, out JToken raw)
        {
            value = default;
            raw = null;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                report.Error(PackagePath(path), "document_file_missing", $"Document package文件不存在：{path}");
                return false;
            }
            try
            {
                raw = JToken.Parse(File.ReadAllText(path, Encoding.UTF8), new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                    LineInfoHandling = LineInfoHandling.Load,
                    CommentHandling = CommentHandling.Ignore
                });
                ValidateFinite(raw, raw.Path);
                value = raw.ToObject<T>(s_Serializer);
                if (value == null)
                    throw new JsonSerializationException($"{path}内容为空。");
                return true;
            }
            catch (JsonException exception)
            {
                report.Error(PackagePath(path), "document_json_invalid", exception.Message);
                return false;
            }
            catch (IOException exception)
            {
                report.Error(PackagePath(path), "document_read_failed", exception.Message);
                return false;
            }
        }

        public static bool TryConvertToken<T>(
            JToken raw,
            string path,
            AgentCompileReport report,
            out T value)
        {
            value = default;
            if (raw == null)
            {
                report.Error(path, "document_file_missing", $"Document package文件不存在：{path}");
                return false;
            }
            try
            {
                ValidateFinite(raw, path);
                value = raw.ToObject<T>(s_Serializer);
                if (value == null)
                    throw new JsonSerializationException($"{path}内容为空。");
                return true;
            }
            catch (JsonException exception)
            {
                report.Error(path, "document_json_invalid", exception.Message);
                return false;
            }
        }

        public static string ToJson(object value)
        {
            JToken token = value == null ? JValue.CreateNull() : JToken.FromObject(value, s_Serializer);
            return Canonicalize(token).ToString(Formatting.Indented, Array.Empty<JsonConverter>()) + Environment.NewLine;
        }

        public static string ToCanonicalJson(object value)
        {
            JToken token = value == null ? JValue.CreateNull() : JToken.FromObject(value, s_Serializer);
            return Canonicalize(token).ToString(Formatting.None, Array.Empty<JsonConverter>());
        }

        public static T Clone<T>(T value)
        {
            if (value == null)
                return default;
            return JToken.FromObject(value, s_Serializer).ToObject<T>(s_Serializer);
        }

        public static JToken ToToken(object value)
        {
            return Canonicalize(value == null ? JValue.CreateNull() : JToken.FromObject(value, s_Serializer));
        }

        public static string Hash(object value)
        {
            return HashToken(value == null ? JValue.CreateNull() : JToken.FromObject(value, s_Serializer));
        }

        public static string HashToken(JToken token)
        {
            string canonical = Canonicalize(token ?? JValue.CreateNull()).ToString(Formatting.None, Array.Empty<JsonConverter>());
            return HashText(canonical);
        }

        public static string HashFiles(IEnumerable<KeyValuePair<string, JToken>> files)
        {
            var identity = new JArray(
                (files ?? Array.Empty<KeyValuePair<string, JToken>>())
                .OrderBy(pair => NormalizeRelativePath(pair.Key), StringComparer.Ordinal)
                .Select(pair => new JObject
                {
                    ["path"] = NormalizeRelativePath(pair.Key),
                    ["hash"] = HashToken(pair.Value)
                }));
            return HashToken(identity);
        }

        public static string HashDocument(
            AgentAuthoringPackageManifest manifest,
            string editableHash,
            string contextHash)
        {
            return Hash(new
            {
                manifest.schemaVersion,
                manifest.domain,
                manifest.rootIdentity,
                editableHash,
                contextHash
            });
        }

        public static bool ValidateManifest(
            AgentAuthoringPackageManifest manifest,
            string domain,
            string rootIdentity,
            AgentCompileReport report)
        {
            if (manifest == null ||
                !string.Equals(manifest.schemaVersion, AgentAuthoringSchema.Version, StringComparison.Ordinal) ||
                !AgentAuthoringSchema.IsDomain(manifest.domain) ||
                string.IsNullOrWhiteSpace(manifest.rootIdentity) ||
                manifest.files == null)
            {
                report.Error("manifest.json", "document_manifest_invalid", "Document package manifest字段不完整或schema不匹配。");
                return false;
            }
            if (!string.Equals(manifest.domain, domain, StringComparison.Ordinal) ||
                !string.Equals(manifest.rootIdentity, rootIdentity, StringComparison.Ordinal))
            {
                report.Error("manifest.json", "document_root_mismatch", "Document package domain或root identity与当前root不一致。");
                return false;
            }
            var normalized = manifest.files.Select(NormalizeRelativePath).ToList();
            bool invalidPath = manifest.files.Any(path =>
            {
                string raw = (path ?? string.Empty).Replace('\\', '/');
                return string.IsNullOrEmpty(raw) ||
                       raw.StartsWith("/", StringComparison.Ordinal) ||
                       Path.IsPathRooted(path) ||
                       raw.Split('/').Any(segment => segment == "." || segment == ".." || string.IsNullOrEmpty(segment));
            });
            if (invalidPath ||
                normalized.Distinct(StringComparer.Ordinal).Count() != normalized.Count)
            {
                report.Error("manifest.json.files", "document_manifest_file_invalid", "Manifest包含非法或重复相对路径。");
                return false;
            }
            return true;
        }

        public static bool ValidateSync(
            AgentAuthoringPackageSync sync,
            AgentAuthoringPackageManifest manifest,
            string rootAssetPath,
            AgentCompileReport report)
        {
            if (sync == null ||
                !string.Equals(sync.schemaVersion, AgentAuthoringSchema.Version, StringComparison.Ordinal) ||
                !string.Equals(sync.domain, manifest.domain, StringComparison.Ordinal) ||
                !string.Equals(sync.rootIdentity, manifest.rootIdentity, StringComparison.Ordinal) ||
                !string.Equals(sync.rootAssetPath, rootAssetPath, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(sync.baseSourceRevision) ||
                string.IsNullOrWhiteSpace(sync.baseEditableHash) ||
                string.IsNullOrWhiteSpace(sync.baseContextHash))
            {
                report.Error(".sync.json", "document_sync_invalid", "Document package同步基线缺失、被修改或与root不一致。");
                return false;
            }
            return true;
        }

        public static string NormalizeRelativePath(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').TrimStart('/');
        }

        static string PackagePath(string path)
        {
            return string.IsNullOrEmpty(path) ? "document" : Path.GetFileName(path);
        }

        static string HashText(string value)
        {
            using SHA256 algorithm = SHA256.Create();
            return string.Concat(algorithm.ComputeHash(s_Utf8WithoutBom.GetBytes(value ?? string.Empty))
                .Select(valueByte => valueByte.ToString("x2", CultureInfo.InvariantCulture)));
        }

        static void ValidateFinite(JToken token, string path)
        {
            if (token is JValue value && value.Type == JTokenType.Float)
            {
                double number = Convert.ToDouble(value.Value, CultureInfo.InvariantCulture);
                if (double.IsNaN(number) || double.IsInfinity(number))
                    throw new JsonSerializationException($"{path}包含非有限数值。");
            }
            if (token is JContainer container)
            {
                foreach (JToken child in container.Children())
                    ValidateFinite(child, child.Path);
            }
        }

        static JToken Canonicalize(JToken token)
        {
            if (token is JObject sourceObject)
            {
                var result = new JObject();
                foreach (JProperty property in sourceObject.Properties().OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    JToken value = Canonicalize(property.Value);
                    if (value.Type == JTokenType.Null ||
                        value.Type == JTokenType.String && string.IsNullOrEmpty(value.Value<string>()) ||
                        string.Equals(property.Name, "weightedMode", StringComparison.Ordinal) &&
                        string.Equals(value.Value<string>(), UnityEngine.WeightedMode.None.ToString(), StringComparison.Ordinal) ||
                        value is JArray array &&
                        array.Count == 0 &&
                        !string.Equals(
                            property.Name,
                            "elements",
                            StringComparison.Ordinal) ||
                        value is JObject nested &&
                        !nested.HasValues &&
                        !string.Equals(property.Name, "defaultValue", StringComparison.Ordinal))
                        continue;
                    result.Add(property.Name, value);
                }
                return result;
            }
            if (token is JArray sourceArray)
            {
                var values = sourceArray.Select(Canonicalize).ToList();
                if (values.Count > 0 &&
                    values.All(value =>
                        value is JObject &&
                        value["clipId"]?.Type == JTokenType.String &&
                        value["channelId"]?.Type == JTokenType.String))
                {
                    values.Sort((left, right) =>
                    {
                        int clip = string.Compare(left["clipId"]?.Value<string>(), right["clipId"]?.Value<string>(), StringComparison.Ordinal);
                        return clip != 0
                            ? clip
                            : string.Compare(left["channelId"]?.Value<string>(), right["channelId"]?.Value<string>(), StringComparison.Ordinal);
                    });
                    return new JArray(values);
                }
                string identity = ResolveIdentityField(values);
                if (!string.IsNullOrEmpty(identity))
                {
                    values.Sort((left, right) => string.Compare(
                        left[identity]?.Value<string>() ?? string.Empty,
                        right[identity]?.Value<string>() ?? string.Empty,
                        StringComparison.Ordinal));
                }
                return new JArray(values);
            }
            if (token is JValue numericValue && numericValue.Type == JTokenType.Float)
                return new JValue(NormalizeFloatingPoint(numericValue.Value));
            return token.DeepClone();
        }

        static double NormalizeFloatingPoint(object value)
        {
            if (value is float single)
            {
                return double.Parse(
                    single.ToString("R", CultureInfo.InvariantCulture),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture);
            }
            if (value is decimal decimalValue)
            {
                return double.Parse(
                    decimalValue.ToString("G29", CultureInfo.InvariantCulture),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture);
            }
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        static string ResolveIdentityField(IReadOnlyList<JToken> values)
        {
            if (values.Count == 0 || values.Any(value => value is not JObject))
                return string.Empty;
            for (int i = 0; i < s_IdentityFields.Length; i++)
            {
                string candidate = s_IdentityFields[i];
                List<string> identities = values
                    .Select(value => value[candidate]?.Type == JTokenType.String
                        ? value[candidate]?.Value<string>()
                        : null)
                    .ToList();
                if (identities.All(value => !string.IsNullOrWhiteSpace(value)) &&
                    identities.Distinct(StringComparer.Ordinal).Count() == identities.Count)
                    return candidate;
            }
            return string.Empty;
        }
    }
}
