using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ThirdPersonPerformance.Instrumentation;

namespace ThirdPersonPerformance.Instrumentation.Editor
{
    public sealed class PerformanceInstrumentationBuildInput
    {
        const string InputDefinePrefix = "THIRDPERSON_PERFORMANCE_INPUT_";
        public const string Schema = "third-person-performance-instrumentation-build/2";

        readonly HashSet<string> m_Assemblies;
        readonly Dictionary<string, PerformanceInstrumentationMetricDescriptor> m_Metrics;

        PerformanceInstrumentationBuildInput(
            PerformanceInstrumentationMode mode,
            string catalogRevision,
            IEnumerable<string> assemblies,
            IEnumerable<PerformanceInstrumentationMetricDescriptor> metrics,
            string manifestDirectory)
        {
            Mode = mode;
            CatalogRevision = catalogRevision;
            string[] assemblyValues = assemblies.ToArray();
            m_Assemblies = new HashSet<string>(assemblyValues, StringComparer.Ordinal);
            if (m_Assemblies.Count != assemblyValues.Length)
                throw new ArgumentException("Performance instrumentation assembly set contains duplicates.", nameof(assemblies));
            m_Metrics = metrics.ToDictionary(value => value.MetricId, StringComparer.Ordinal);
            ManifestDirectory = manifestDirectory ?? string.Empty;
            Identity = ComputeIdentity();
        }

        public PerformanceInstrumentationMode Mode { get; }
        public string CatalogRevision { get; }
        public string ManifestDirectory { get; }
        public string Identity { get; }
        public IReadOnlyList<string> Assemblies => m_Assemblies
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        public bool AllowsAssembly(string assemblyName) => m_Assemblies.Contains(assemblyName);

        public bool TryGetMetric(
            string metricId,
            out PerformanceInstrumentationMetricDescriptor descriptor) =>
            m_Metrics.TryGetValue(metricId, out descriptor);

        public static PerformanceInstrumentationBuildInput Create(
            PerformanceInstrumentationMode mode,
            string catalogRevision,
            IEnumerable<string> assemblies,
            IEnumerable<PerformanceInstrumentationMetricDescriptor> metrics)
        {
            var input = new PerformanceInstrumentationBuildInput(
                mode,
                catalogRevision ?? string.Empty,
                assemblies ?? Array.Empty<string>(),
                metrics ?? Array.Empty<PerformanceInstrumentationMetricDescriptor>(),
                string.Empty);
            input.Validate();
            return input;
        }

        public static void Write(
            string path,
            string manifestDirectory,
            PerformanceInstrumentationMode mode,
            string catalogRevision,
            IEnumerable<string> assemblies,
            IEnumerable<PerformanceInstrumentationMetricDescriptor> metrics)
        {
            PerformanceInstrumentationBuildInput input = Create(
                mode,
                catalogRevision,
                assemblies,
                metrics);
            var lines = new List<string>
            {
                "schema=" + Schema,
                "mode=" + input.Mode,
                "catalog_revision=" + input.CatalogRevision,
                "identity=" + input.Identity,
                "manifest_directory=" + Path.GetFullPath(manifestDirectory)
            };
            lines.AddRange(input.m_Assemblies.OrderBy(value => value, StringComparer.Ordinal)
                .Select(value => "assembly=" + value));
            lines.AddRange(input.m_Metrics.Values
                .OrderBy(value => value.MetricId, StringComparer.Ordinal)
                .Select(value => "metric=" + string.Join(
                    "|",
                    value.MetricId,
                    value.ProfilerName,
                    value.ParentId,
                    value.SampleScope,
                    value.Unit,
                    value.Aggregation)));
            string fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, string.Join("\n", lines) + "\n", new UTF8Encoding(false));
        }

        public static string[] CreateBuildDefines(string path)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(Path.GetFullPath(path));
            var builder = new StringBuilder(InputDefinePrefix);
            for (int i = 0; i < bytes.Length; i++)
                builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
            return new[] { PerformanceInstrumentationIdentity.Define, builder.ToString() };
        }

        public static bool TryLoad(
            string[] defines,
            out PerformanceInstrumentationBuildInput input,
            out string error)
        {
            input = null;
            error = string.Empty;
            string[] inputDefines = defines.Where(value =>
                value.StartsWith(InputDefinePrefix, StringComparison.Ordinal)).ToArray();
            if (inputDefines.Length != 1)
            {
                error = "Performance compilation requires exactly one explicit build input define.";
                return false;
            }
            string encodedPath = inputDefines[0].Substring(InputDefinePrefix.Length);
            var pathBytes = new byte[encodedPath.Length / 2];
            for (int i = 0; i < pathBytes.Length; i++)
                pathBytes[i] = byte.Parse(encodedPath.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            string path = Encoding.UTF8.GetString(pathBytes);
            if (!File.Exists(path))
            {
                error = $"Performance instrumentation build input does not exist: {path}";
                return false;
            }

            string schema = string.Empty;
            string mode = string.Empty;
            string catalogRevision = string.Empty;
            string identity = string.Empty;
            string manifestDirectory = string.Empty;
            var assemblies = new List<string>();
            var metrics = new List<PerformanceInstrumentationMetricDescriptor>();
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                    continue;
                int separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    error = $"Performance instrumentation input line {i + 1} is invalid.";
                    return false;
                }
                string key = line.Substring(0, separator);
                string value = line.Substring(separator + 1);
                switch (key)
                {
                    case "schema":
                        schema = value;
                        break;
                    case "mode":
                        mode = value;
                        break;
                    case "catalog_revision":
                        catalogRevision = value;
                        break;
                    case "identity":
                        identity = value;
                        break;
                    case "manifest_directory":
                        manifestDirectory = value;
                        break;
                    case "assembly":
                        assemblies.Add(value);
                        break;
                    case "metric":
                        string[] parts = value.Split('|');
                        if (parts.Length != 6)
                        {
                            error = $"Performance instrumentation metric line {i + 1} is invalid.";
                            return false;
                        }
                        metrics.Add(new PerformanceInstrumentationMetricDescriptor(
                            parts[0],
                            parts[1],
                            parts[2],
                            parts[3],
                            parts[4],
                            parts[5]));
                        break;
                    default:
                        error = $"Performance instrumentation input line {i + 1} is unknown.";
                        return false;
                }
            }
            if (!string.Equals(schema, Schema, StringComparison.Ordinal))
            {
                error = "Performance instrumentation build input schema is invalid.";
                return false;
            }
            if (!Enum.TryParse(mode, true, out PerformanceInstrumentationMode parsedMode) ||
                !Enum.IsDefined(typeof(PerformanceInstrumentationMode), parsedMode) ||
                parsedMode == PerformanceInstrumentationMode.Disabled)
            {
                error = "Performance instrumentation build input mode must be MarkerOnly or Span.";
                return false;
            }
            try
            {
                input = new PerformanceInstrumentationBuildInput(
                    parsedMode,
                    catalogRevision,
                    assemblies,
                    metrics,
                    manifestDirectory);
                input.Validate();
                if (string.IsNullOrWhiteSpace(input.ManifestDirectory))
                {
                    error = "Performance build input manifest directory is missing.";
                    input = null;
                    return false;
                }
                if (!string.Equals(identity, input.Identity, StringComparison.Ordinal))
                {
                    error = "Performance instrumentation build input identity is invalid.";
                    input = null;
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        void Validate()
        {
            if (!Enum.IsDefined(typeof(PerformanceInstrumentationMode), Mode) ||
                Mode == PerformanceInstrumentationMode.Disabled)
                throw new ArgumentException("Performance instrumentation build input cannot use Disabled mode.", nameof(Mode));
            if (string.IsNullOrWhiteSpace(CatalogRevision))
                throw new ArgumentException("Performance instrumentation catalog revision is missing.", nameof(CatalogRevision));
            if (m_Assemblies.Count == 0)
                throw new ArgumentException("Performance instrumentation assembly set is empty.", nameof(m_Assemblies));
            if (m_Assemblies.Any(value => string.IsNullOrWhiteSpace(value)))
                throw new ArgumentException("Performance instrumentation assembly set contains an empty name.", nameof(m_Assemblies));
            if (m_Metrics.Count == 0)
                throw new ArgumentException("Performance instrumentation metric catalog is empty.", nameof(m_Metrics));
            foreach (PerformanceInstrumentationMetricDescriptor metric in m_Metrics.Values)
                metric.Validate();
            foreach (PerformanceInstrumentationMetricDescriptor metric in m_Metrics.Values)
            {
                if (!string.IsNullOrEmpty(metric.ParentId) && !m_Metrics.ContainsKey(metric.ParentId))
                    throw new ArgumentException(
                        $"Performance instrumentation metric '{metric.MetricId}' has unknown parent '{metric.ParentId}'.");
            }
        }

        string ComputeIdentity()
        {
            var builder = new StringBuilder(512);
            builder.Append(Schema).Append('|')
                .Append(PerformanceInstrumentationIdentity.WeaverVersion).Append('|')
                .Append(PerformanceInstrumentationIdentity.SpanLayoutRevision).Append('|')
                .Append(Mode).Append('|')
                .Append(CatalogRevision).Append('\n');
            foreach (string assembly in m_Assemblies.OrderBy(value => value, StringComparer.Ordinal))
                builder.Append("assembly|").Append(assembly).Append('\n');
            foreach (PerformanceInstrumentationMetricDescriptor metric in m_Metrics.Values
                .OrderBy(value => value.MetricId, StringComparer.Ordinal))
            {
                builder.Append("metric|").Append(metric.MetricId).Append('|')
                    .Append(metric.ProfilerName).Append('|')
                    .Append(metric.ParentId).Append('|')
                    .Append(metric.SampleScope).Append('|')
                    .Append(metric.Unit).Append('|')
                    .Append(metric.Aggregation).Append('\n');
            }
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
                var result = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++)
                    result.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }
    }

    public sealed class PerformanceInstrumentationMetricDescriptor
    {
        public PerformanceInstrumentationMetricDescriptor(
            string metricId,
            string profilerName,
            string parentId,
            string sampleScope,
            string unit,
            string aggregation)
        {
            MetricId = metricId?.Trim() ?? string.Empty;
            ProfilerName = profilerName?.Trim() ?? string.Empty;
            ParentId = parentId?.Trim() ?? string.Empty;
            SampleScope = sampleScope?.Trim() ?? string.Empty;
            Unit = unit?.Trim() ?? string.Empty;
            Aggregation = aggregation?.Trim() ?? string.Empty;
        }

        public string MetricId { get; }
        public string ProfilerName { get; }
        public string ParentId { get; }
        public string SampleScope { get; }
        public string Unit { get; }
        public string Aggregation { get; }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(MetricId) || string.IsNullOrWhiteSpace(ProfilerName) ||
                string.IsNullOrWhiteSpace(SampleScope) || string.IsNullOrWhiteSpace(Unit) ||
                string.IsNullOrWhiteSpace(Aggregation))
            {
                throw new ArgumentException(
                    $"Performance instrumentation metric '{MetricId}' is incomplete.");
            }
        }
    }
}
