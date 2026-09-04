using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Diagnostics;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;

namespace ThirdPersonPerformance.Runtime
{
    public static class ThirdPersonRuntimePerformanceMetricCatalog
    {
        public const string MainThreadMetricId = "unity.main-thread";
        public const string GcAllocatedMetricId = "unity.gc-allocated-in-frame";
        public const string MainThreadProfilerName = "Main Thread";
        public const string GcAllocatedProfilerName = "GC Allocated In Frame";

        static readonly PerformanceMetricDefinition[] s_Unity =
        {
            new PerformanceMetricDefinition(
                MainThreadMetricId,
                MainThreadProfilerName,
                PerformanceMetricDomain.Unity,
                string.Empty,
                PerformanceSampleScope.RenderFrame,
                PerformanceMetricUnit.Nanoseconds,
                PerformanceMetricAggregation.InclusiveDuration),
            new PerformanceMetricDefinition(
                GcAllocatedMetricId,
                GcAllocatedProfilerName,
                PerformanceMetricDomain.Unity,
                string.Empty,
                PerformanceSampleScope.Counter,
                PerformanceMetricUnit.Bytes,
                PerformanceMetricAggregation.LastValue)
        };

        static readonly PerformanceMetricDefinition[] s_All =
            PerformanceMetricCatalogValidator.Validate(
                GameplayPerformanceMetrics.All,
                SimulationPerformanceMetrics.All,
                CharacterPerformanceMetrics.All,
                s_Unity);

        public static IReadOnlyList<PerformanceMetricDefinition> All => s_All;
    }
}
