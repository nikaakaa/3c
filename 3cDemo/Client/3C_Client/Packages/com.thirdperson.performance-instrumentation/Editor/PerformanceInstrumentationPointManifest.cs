using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ThirdPersonPerformance.Instrumentation.Editor
{
    internal static class PerformanceInstrumentationPointManifest
    {
        public static void Write(
            string directory,
            string assemblyName,
            PerformanceInstrumentationBuildInput input,
            IReadOnlyList<PerformanceInstrumentationPointDescriptor> points)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidDataException("Performance instrumentation manifest directory is missing.");
            Directory.CreateDirectory(directory);
            var builder = new StringBuilder(1024);
            builder.Append("{\n")
                .Append("  \"schema\":\"").Append(Escape(PerformanceInstrumentationIdentity.ManifestSchema)).Append("\",\n")
                .Append("  \"assembly\":\"").Append(Escape(assemblyName)).Append("\",\n")
                .Append("  \"mode\":\"").Append(input.Mode).Append("\",\n")
                .Append("  \"catalog_revision\":\"").Append(Escape(input.CatalogRevision)).Append("\",\n")
                .Append("  \"identity\":\"").Append(Escape(input.Identity)).Append("\",\n")
                .Append("  \"weaver_version\":\"").Append(PerformanceInstrumentationIdentity.WeaverVersion).Append("\",\n")
                .Append("  \"span_layout_revision\":").Append(PerformanceInstrumentationIdentity.SpanLayoutRevision).Append(",\n")
                .Append("  \"points\":[\n");
            foreach (PerformanceInstrumentationPointDescriptor point in points
                .OrderBy(value => value.PointId))
            {
                builder.Append("    {\"point_id\":\"")
                    .Append(point.PointId.ToString("x16", CultureInfo.InvariantCulture)).Append("\",\"metric_id\":\"")
                    .Append(Escape(point.MetricId)).Append("\",\"metric_hash\":\"")
                    .Append(point.MetricIdHash.ToString("x16", CultureInfo.InvariantCulture)).Append("\",\"declaring_type\":\"")
                    .Append(Escape(point.DeclaringType)).Append("\",\"method\":\"")
                    .Append(Escape(point.Method)).Append("\",\"source_file\":\"")
                    .Append(Escape(point.SourceFile)).Append("\",\"source_line\":")
                    .Append(point.SourceLine.ToString(CultureInfo.InvariantCulture)).Append("},\n");
            }
            if (points.Count > 0)
                builder.Length -= 2;
            builder.Append("\n  ]\n}\n");
            string fileName = "instrumentation." + StableHash64(assemblyName).ToString("x16", CultureInfo.InvariantCulture) + ".json";
            string path = Path.Combine(directory, fileName);
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, builder.ToString(), new UTF8Encoding(false));
            if (File.Exists(path))
                File.Delete(path);
            File.Move(temporary, path);
        }

        static string Escape(string value) =>
            (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");

        static ulong StableHash64(string value)
        {
            ulong hash = 14695981039346656037UL;
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 1099511628211UL;
            }
            return hash;
        }
    }
}
