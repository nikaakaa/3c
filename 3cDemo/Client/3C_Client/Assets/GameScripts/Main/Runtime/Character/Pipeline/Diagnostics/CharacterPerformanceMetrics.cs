using System.Collections.Generic;
using ThirdPersonPerformance;

namespace ThirdPersonCharacter.Pipeline.Diagnostics
{
    public static class CharacterPerformanceMetrics
    {
        public const string SessionInputName = "ThirdPerson.Session.Input";
        public const string SessionLogicName = "ThirdPerson.Session.LogicTick";
        public const string AnimationName = "ThirdPerson.Presentation.Animation";
        public const string FactProjectionName = "ThirdPerson.Presentation.FactProjection";
        public const string BodyName = "ThirdPerson.Presentation.Body";
        public const string PoseGraphPrepareName = "ThirdPerson.Presentation.Animation.PoseGraph.Prepare";
        public const string PoseGraphEvaluateName = "ThirdPerson.Presentation.Animation.PoseGraph.Evaluate";
        public const string PoseGraphCommitName = "ThirdPerson.Presentation.Animation.PoseGraph.Commit";

        static readonly PerformanceMetricDefinition[] s_All =
        {
            Metric("session.input", SessionInputName, string.Empty, PerformanceMetricDomain.Session, PerformanceSampleScope.RenderFrame),
            Metric("session.logic-tick", SessionLogicName, string.Empty, PerformanceMetricDomain.Session, PerformanceSampleScope.LogicTick),
            Metric("presentation.animation", AnimationName, "gameplay.presentation"),
            Metric("presentation.event-graph", "ThirdPerson.Presentation.EventGraph", "presentation.animation"),
            Metric("presentation.timeline", "ThirdPerson.Presentation.Timeline", "presentation.animation"),
            Metric("presentation.timeline.commit", "ThirdPerson.Presentation.Timeline.Commit", "presentation.animation"),
            Metric("presentation.camera", "ThirdPerson.Presentation.Camera", "presentation.animation"),
            Metric("presentation.equipment", "ThirdPerson.Presentation.Equipment", "presentation.animation"),
            Metric("presentation.fact-projection", FactProjectionName, "gameplay.presentation"),
            Metric("presentation.body", BodyName, "gameplay.presentation"),
            Metric("presentation.animation.pose-graph.prepare", PoseGraphPrepareName, "presentation.animation"),
            Metric("presentation.animation.pose-graph.evaluate", PoseGraphEvaluateName, "presentation.animation"),
            Metric("presentation.animation.pose-graph.commit", PoseGraphCommitName, "presentation.animation"),
            Metric("presentation.animation.source-barrier", "ThirdPerson.Presentation.Animation.SourceBarrier", "presentation.animation"),
            Metric("presentation.animation.foot-placement", "ThirdPerson.Presentation.Animation.FootPlacement", "presentation.animation.pose-graph.evaluate"),
            Metric("presentation.animation.full-body-ik", "ThirdPerson.Presentation.Animation.FullBodyIK", "presentation.animation.pose-graph.evaluate")
        };

        public static IReadOnlyList<PerformanceMetricDefinition> All => s_All;

        static PerformanceMetricDefinition Metric(
            string id,
            string name,
            string parent,
            PerformanceMetricDomain domain = PerformanceMetricDomain.Presentation,
            PerformanceSampleScope scope = PerformanceSampleScope.RenderFrame) =>
            new PerformanceMetricDefinition(
                id,
                name,
                domain,
                parent,
                scope,
                PerformanceMetricUnit.Nanoseconds,
                PerformanceMetricAggregation.InclusiveDuration);
    }
}
