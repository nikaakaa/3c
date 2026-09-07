using System.Collections.Generic;
using ThirdPersonPerformance;

namespace ThirdPersonCharacter.Pipeline.Diagnostics
{
    public static class CharacterPerformanceMetrics
    {
        public const string SessionInputName = "ThirdPerson.Session.Input";
        public const string SessionLogicName = "ThirdPerson.Session.LogicTick";
        public const string AnimationName = "ThirdPerson.Presentation.Animation";
        public const string EquipmentName = "ThirdPerson.Presentation.Equipment";
        public const string FactProjectionName = "ThirdPerson.Presentation.FactProjection";
        public const string FinalPoseName = "ThirdPerson.Presentation.FinalPose";
        public const string CameraName = "ThirdPerson.Presentation.Camera";
        public const string BodyName = "ThirdPerson.Presentation.Body";
        public const string ActionLifecycleName = "ThirdPerson.Presentation.Animation.ActionLifecycle";
        public const string TransactionBeginName = "ThirdPerson.Presentation.Animation.TransactionBegin";
        public const string ActionSamplingName = "ThirdPerson.Presentation.Animation.ActionSampling";
        public const string PoseRoutingName = "ThirdPerson.Presentation.Animation.PoseRouting";
        public const string MotionMatchingName = "ThirdPerson.Presentation.Animation.MotionMatching";
        public const string ReleaseProtocolName = "ThirdPerson.Presentation.Animation.ReleaseProtocol";
        public const string FrameCommitName = "ThirdPerson.Presentation.Animation.FrameCommit";
        public const string PostCommitName = "ThirdPerson.Presentation.Animation.PostCommit";
        public const string PrepareName = "ThirdPerson.Presentation.Animation.Prepare";
        public const string PrepareWorkspaceName = "ThirdPerson.Presentation.Animation.Prepare.Workspace";
        public const string PrepareStackName = "ThirdPerson.Presentation.Animation.Prepare.Stack";
        public const string PrepareDirectName = "ThirdPerson.Presentation.Animation.Prepare.Direct";
        public const string PrepareClipName = "ThirdPerson.Presentation.Animation.Prepare.Clip";
        public const string PrepareBlendSpaceName = "ThirdPerson.Presentation.Animation.Prepare.BlendSpace";
        public const string ValidateName = "ThirdPerson.Presentation.Animation.Validate";
        public const string GraphEvaluateName = "ThirdPerson.Presentation.Animation.GraphEvaluate";
        public const string PoseGraphExecuteName = "ThirdPerson.Presentation.Animation.PoseGraphExecute";
        public const string FinalWriteName = "ThirdPerson.Presentation.Animation.FinalWrite";
        public const string SealName = "ThirdPerson.Presentation.Animation.Seal";
        public const string DiagnosticsName = "ThirdPerson.Presentation.Animation.Diagnostics";
        public const string ValueResetName = "ThirdPerson.Presentation.Animation.PoseGraph.ValueReset";
        public const string PlayerInputName = "ThirdPerson.Presentation.Animation.PoseGraph.PlayerInput";
        public const string SlotName = "ThirdPerson.Presentation.Animation.PoseGraph.Slot";
        public const string StateName = "ThirdPerson.Presentation.Animation.PoseGraph.State";
        public const string InertializationName = "ThirdPerson.Presentation.Animation.PoseGraph.Inertialization";
        public const string IkGoalName = "ThirdPerson.Presentation.Animation.PoseGraph.IKGoal";
        public const string LinkedPoseName = "ThirdPerson.Presentation.Animation.PoseGraph.LinkedPose";
        public const string FullBodyIkName = "ThirdPerson.Presentation.Animation.PoseGraph.FinalIKFullBody";
        public const string OutputName = "ThirdPerson.Presentation.Animation.PoseGraph.Output";
        public const string ValueValidationName = "ThirdPerson.Presentation.Animation.PoseGraph.ValueValidation";

        static readonly PerformanceMetricDefinition[] s_All =
        {
            Metric("session.input", SessionInputName, string.Empty, PerformanceMetricDomain.Session, PerformanceSampleScope.RenderFrame),
            Metric("session.logic-tick", SessionLogicName, string.Empty, PerformanceMetricDomain.Session, PerformanceSampleScope.LogicTick),
            Metric("presentation.animation", AnimationName, "gameplay.presentation"),
            Metric("presentation.equipment", EquipmentName, "gameplay.presentation"),
            Metric("presentation.fact-projection", FactProjectionName, "gameplay.presentation"),
            Metric("presentation.final-pose", FinalPoseName, "gameplay.presentation"),
            Metric("presentation.camera", CameraName, "gameplay.presentation"),
            Metric("presentation.body", BodyName, "gameplay.presentation"),
            Metric("presentation.animation.action-lifecycle", ActionLifecycleName, "presentation.animation"),
            Metric("presentation.animation.transaction-begin", TransactionBeginName, "presentation.animation"),
            Metric("presentation.animation.action-sampling", ActionSamplingName, "presentation.animation"),
            Metric("presentation.animation.pose-routing", PoseRoutingName, "presentation.animation"),
            Metric("presentation.animation.motion-matching", MotionMatchingName, "presentation.animation"),
            Metric("presentation.animation.release-protocol", ReleaseProtocolName, "presentation.animation"),
            Metric("presentation.animation.frame-commit", FrameCommitName, "presentation.animation"),
            Metric("presentation.animation.post-commit", PostCommitName, "presentation.animation"),
            Metric("presentation.animation.prepare", PrepareName, "presentation.animation"),
            Metric("presentation.animation.prepare.workspace", PrepareWorkspaceName, "presentation.animation.prepare"),
            Metric("presentation.animation.prepare.stack", PrepareStackName, "presentation.animation.prepare"),
            Metric("presentation.animation.prepare.direct", PrepareDirectName, "presentation.animation.prepare"),
            Metric("presentation.animation.prepare.clip", PrepareClipName, "presentation.animation.prepare"),
            Metric("presentation.animation.prepare.blend-space", PrepareBlendSpaceName, "presentation.animation.prepare"),
            Metric("presentation.animation.validate", ValidateName, "presentation.animation"),
            Metric("presentation.animation.graph-evaluate", GraphEvaluateName, "presentation.animation"),
            Metric("presentation.animation.pose-graph", PoseGraphExecuteName, "presentation.animation"),
            Metric("presentation.animation.final-write", FinalWriteName, "presentation.animation"),
            Metric("presentation.animation.seal", SealName, "presentation.animation"),
            Metric("presentation.animation.diagnostics", DiagnosticsName, "presentation.animation"),
            Metric("presentation.animation.pose-graph.value-reset", ValueResetName, "presentation.animation.pose-graph"),
            Metric("presentation.animation.pose-graph.player-input", PlayerInputName, "presentation.animation.pose-graph"),
            Metric("presentation.animation.pose-graph.slot", SlotName, "presentation.animation.pose-graph"),
            Metric("presentation.animation.pose-graph.state", StateName, "presentation.animation.pose-graph"),
            Metric("presentation.animation.pose-graph.inertialization", InertializationName, "presentation.animation.pose-graph"),
            Metric("presentation.animation.pose-graph.ik-goal", IkGoalName, "presentation.animation.pose-graph"),
            Metric("presentation.animation.pose-graph.linked-pose", LinkedPoseName, "presentation.animation.pose-graph"),
            Metric("presentation.animation.pose-graph.full-body-ik", FullBodyIkName, "presentation.animation.pose-graph"),
            Metric("presentation.animation.pose-graph.output", OutputName, "presentation.animation.pose-graph"),
            Metric("presentation.animation.pose-graph.value-validation", ValueValidationName, "presentation.animation.pose-graph")
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
