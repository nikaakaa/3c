using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ThirdPersonPerformance;
using ThirdPersonPerformance.Instrumentation;
using ThirdPersonPerformance.Instrumentation.Editor;
using ThirdPersonPerformance.Runtime;

namespace ThirdPersonPerformance.Editor
{
    internal static class ThirdPersonPerformanceInstrumentationCatalog
    {
        static readonly string[] s_AssemblyNames =
        {
            "ThirdPersonGameplay",
            "ThirdPersonClient.Runtime",
            "ThirdPersonSimulation.Core",
            "ThirdPersonSimulation.Fixed",
            "ThirdPersonSimulation.Float32",
            "ThirdPersonSimulation.Unity"
        };

        public static IReadOnlyList<string> AssemblyNames => s_AssemblyNames;

        public static IReadOnlyList<PerformanceInstrumentationMetricDescriptor> Metrics
        {
            get
            {
                IReadOnlyList<PerformanceMetricDefinition> metrics =
                    ThirdPersonRuntimePerformanceMetricCatalog.All;
                var values = new PerformanceInstrumentationMetricDescriptor[metrics.Count];
                for (int i = 0; i < metrics.Count; i++)
                {
                    PerformanceMetricDefinition metric = metrics[i];
                    values[i] = new PerformanceInstrumentationMetricDescriptor(
                        metric.MetricId,
                        metric.ProfilerName,
                        metric.ParentId,
                        metric.SampleScope.ToString(),
                        metric.Unit.ToString(),
                        metric.Aggregation.ToString());
                }
                return values;
            }
        }

        public static string Revision
        {
            get
            {
                IReadOnlyList<PerformanceInstrumentationMetricDescriptor> metrics = Metrics;
                var builder = new StringBuilder(1024);
                for (int i = 0; i < metrics.Count; i++)
                {
                    PerformanceInstrumentationMetricDescriptor metric = metrics[i];
                    builder.Append(metric.MetricId).Append('|')
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

        public static void WriteBuildInput(
            string path,
            string manifestDirectory,
            PerformanceInstrumentationMode mode,
            IReadOnlyList<string> assemblies) =>
            PerformanceInstrumentationBuildInput.Write(
                path,
                manifestDirectory,
                mode,
                Revision,
                assemblies,
                Metrics);

        public static PerformanceInstrumentationBuildInput CreateBuildInput(
            PerformanceInstrumentationMode mode,
            IReadOnlyList<string> assemblies) =>
            PerformanceInstrumentationBuildInput.Create(
                mode,
                Revision,
                assemblies,
                Metrics);
    }
}
