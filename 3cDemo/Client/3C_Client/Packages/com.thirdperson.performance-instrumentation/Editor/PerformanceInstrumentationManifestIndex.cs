using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ThirdPersonPerformance.Instrumentation.Editor
{
    public static class PerformanceInstrumentationManifestIndex
    {
        public static string Write(
            string fragmentDirectory,
            string outputPath,
            PerformanceInstrumentationBuildInput input)
        {
            if (string.IsNullOrWhiteSpace(fragmentDirectory))
                throw new InvalidDataException("Performance instrumentation fragment directory is missing.");
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new InvalidDataException("Performance instrumentation manifest path is missing.");
            Directory.CreateDirectory(fragmentDirectory);
            string[] fragments = Directory.GetFiles(
                    fragmentDirectory,
                    "instrumentation.*.json",
                    SearchOption.TopDirectoryOnly)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var fragmentTexts = new string[fragments.Length];
            var identityBuilder = new StringBuilder(1024);
            identityBuilder.Append(input.Identity).Append('\n');
            for (int i = 0; i < fragments.Length; i++)
            {
                fragmentTexts[i] = File.ReadAllText(fragments[i], Encoding.UTF8).Trim();
                if (fragmentTexts[i].Length == 0 || !fragmentTexts[i].Contains(
                    $"\"identity\":\"{input.Identity}\"",
                    StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        $"Performance instrumentation fragment identity mismatch: {fragments[i]}");
                }
                identityBuilder.Append(fragmentTexts[i]).Append('\n');
            }
            string identity = Hash(identityBuilder.ToString());
            var builder = new StringBuilder(1024);
            builder.Append("{\n")
                .Append("  \"schema\":\"").Append(PerformanceInstrumentationIdentity.ManifestSchema).Append("\",\n")
                .Append("  \"mode\":\"").Append(input.Mode).Append("\",\n")
                .Append("  \"catalog_revision\":\"").Append(Escape(input.CatalogRevision)).Append("\",\n")
                .Append("  \"identity\":\"").Append(identity).Append("\",\n")
                .Append("  \"input_identity\":\"").Append(Escape(input.Identity)).Append("\",\n")
                .Append("  \"weaver_version\":\"").Append(PerformanceInstrumentationIdentity.WeaverVersion).Append("\",\n")
                .Append("  \"span_layout_revision\":").Append(PerformanceInstrumentationIdentity.SpanLayoutRevision).Append(",\n")
                .Append("  \"assembly_scope\":[");
            IReadOnlyList<string> assemblies = input.Assemblies;
            for (int i = 0; i < assemblies.Count; i++)
            {
                if (i != 0)
                    builder.Append(',');
                builder.Append("\"").Append(Escape(assemblies[i])).Append("\"");
            }
            builder.Append("],\n")
                .Append("  \"assemblies\":[\n");
            for (int i = 0; i < fragments.Length; i++)
            {
                builder.Append("    ").Append(fragmentTexts[i]);
                if (i + 1 < fragments.Length)
                    builder.Append(',');
                builder.Append('\n');
            }
            builder.Append("  ]\n}\n");
            string fullPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            string temporary = fullPath + ".tmp";
            File.WriteAllText(temporary, builder.ToString(), new UTF8Encoding(false));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
            File.Move(temporary, fullPath);
            return identity;
        }

        static string Hash(string value)
        {
            using SHA256 sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
            var result = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                result.Append(bytes[i].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
            return result.ToString();
        }

        static string Escape(string value) =>
            (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
    }
}
