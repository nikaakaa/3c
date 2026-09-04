using System.Collections.Generic;
using ThirdPersonPerformance;

namespace ThirdPersonGameplay.Tick
{
    public static class GameplayPerformanceMetrics
    {
        public const string InputName = "ThirdPerson.Gameplay.Input";
        public const string LogicName = "ThirdPerson.Gameplay.Logic";
        public const string PresentationName = "ThirdPerson.Gameplay.Presentation";

        static readonly PerformanceMetricDefinition[] s_All =
        {
            new PerformanceMetricDefinition(
                "gameplay.input",
                InputName,
                PerformanceMetricDomain.Gameplay,
                string.Empty,
                PerformanceSampleScope.RenderFrame,
                PerformanceMetricUnit.Nanoseconds,
                PerformanceMetricAggregation.InclusiveDuration),
            new PerformanceMetricDefinition(
                "gameplay.logic",
                LogicName,
                PerformanceMetricDomain.Gameplay,
                string.Empty,
                PerformanceSampleScope.RenderFrame,
                PerformanceMetricUnit.Nanoseconds,
                PerformanceMetricAggregation.InclusiveDuration),
            new PerformanceMetricDefinition(
                "gameplay.presentation",
                PresentationName,
                PerformanceMetricDomain.Gameplay,
                string.Empty,
                PerformanceSampleScope.RenderFrame,
                PerformanceMetricUnit.Nanoseconds,
                PerformanceMetricAggregation.InclusiveDuration)
        };

        public static IReadOnlyList<PerformanceMetricDefinition> All => s_All;
    }
}
