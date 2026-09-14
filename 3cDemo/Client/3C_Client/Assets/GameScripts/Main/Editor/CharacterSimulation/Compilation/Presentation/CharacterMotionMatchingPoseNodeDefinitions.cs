using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Editor;
using TreeDesigner.Authoring;
using static ThirdPersonCharacter.Editor.CharacterSimulation.CharacterPoseCapabilityDeclarations;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class CharacterMotionMatchingPoseNodeDefinition :
        CharacterPoseNodeDefinition<CharacterMotionMatchingPosePayload>
    {
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.MotionMatchingPose;
        public override GraphAuthoringCapabilityDescriptor Declare() =>
            Node<CharacterMotionMatchingPosePayload>(CharacterPoseNodeKind.MotionMatchingPose, new[] { CharacterPoseGraphAuthoringCapabilities.StatePoseGraph }, "Motion Matching Pose", "Sources", SourceColor,
                Fields(ResourceField("binding", "Motion Matching Binding"), ResourceField("jump-blend-policy", "Jump Blend Policy"), ReferenceIdentityField("entry-graph-id", "Entry Processing Graph", "pose-graph"), TypedEnumField("relevance-reset-policy", "Relevance Reset", typeof(CharacterMotionMatchingRelevanceResetPolicy)), TypedEnumField("search-cadence-policy", "Search Cadence", typeof(CharacterMotionMatchingSearchCadencePolicy))),
                Ports(In("history.pose", "Previous Pose History", "pose.history"), OptionalIn("trajectory.query", "Trajectory", "motion-matching.trajectory"), OptionalIn("presentation.facts", "Presentation Facts", "presentation.facts"), OptionalIn("motion-matching.binding", "Binding", "motion-matching.binding"), Out("pose.local", "Local Pose", "pose.local")),
                childSurfaces: new[] { Child("open-entry-processing-graph", "Open Entry Processing Graph", CharacterPoseGraphAuthoringCapabilities.Subgraph) },
                executionDomain: CharacterPoseExecutionDomain.SourceCapture);

        public override CharacterPoseNodePayload CreatePayload(CharacterPoseAuthoringPayloadInput input) =>
            new CharacterMotionMatchingPosePayload(
                input.Require<CharacterPoseResourceSlot>("binding"),
                input.Require<CharacterPoseResourceSlot>("jump-blend-policy"),
                new PoseGraphId(input.Require<string>("entry-graph-id")),
                Enum.Parse<CharacterMotionMatchingRelevanceResetPolicy>(input.Require<string>("relevance-reset-policy"), false),
                Enum.Parse<CharacterMotionMatchingSearchCadencePolicy>(input.Require<string>("search-cadence-policy"), false));

        protected override object ReadField(CharacterMotionMatchingPosePayload payload, string field) => field switch
        {
            "binding" => payload.BindingSlot,
            "jump-blend-policy" => payload.JumpBlendPolicySlot,
            "entry-graph-id" => payload.EntryGraph?.PoseGraphId.Value ?? string.Empty,
            "relevance-reset-policy" => payload.RelevanceResetPolicy.ToString(),
            "search-cadence-policy" => payload.SearchCadencePolicy.ToString(),
            _ => base.ReadField(payload, field)
        };

        protected override void Validate(CharacterMotionMatchingPosePayload payload, string sourcePath)
        {
            CharacterPoseNodeDefinitionValidation.Require(payload.BindingSlot && payload.BindingSlot.Kind == CharacterPoseResourceKind.MotionMatchingBinding, sourcePath, "Motion Matching Binding Resource Slot is missing or incompatible.");
            CharacterPoseNodeDefinitionValidation.Require(payload.JumpBlendPolicySlot && payload.JumpBlendPolicySlot.Kind == CharacterPoseResourceKind.BlendPolicy, sourcePath, "Motion Matching Jump Blend Policy Resource Slot is missing or incompatible.");
            CharacterPoseNodeDefinitionValidation.Require(payload.EntryGraph != null && payload.EntryGraph.PoseGraphId.IsValid, sourcePath, "Motion Matching entry graph identity is missing.");
            CharacterPoseNodeDefinitionValidation.Require(Enum.IsDefined(typeof(CharacterMotionMatchingRelevanceResetPolicy), payload.RelevanceResetPolicy), sourcePath, "Motion Matching relevance reset policy is invalid.");
            CharacterPoseNodeDefinitionValidation.Require(Enum.IsDefined(typeof(CharacterMotionMatchingSearchCadencePolicy), payload.SearchCadencePolicy), sourcePath, "Motion Matching search cadence policy is invalid.");
        }

        protected override IReadOnlyList<CharacterPoseGraphDependency>
            GetGraphDependencies(CharacterMotionMatchingPosePayload payload) =>
            payload.EntryGraph?.PoseGraphId.IsValid == true
                ? new[]
                {
                    new CharacterPoseGraphDependency(
                        CharacterPoseGraphDependencyKind.MotionMatchingEntry,
                        payload.EntryGraph.PoseGraphId,
                        payload.EntryGraph.PoseGraphId.Value)
                }
                : Array.Empty<CharacterPoseGraphDependency>();

        protected override string GetChildDocumentId(
            CharacterMotionMatchingPosePayload payload) =>
            payload.EntryGraph?.PoseGraphId.Value ?? string.Empty;

        protected override void ValidateRig(CharacterMotionMatchingPosePayload payload, CharacterAnimationRigDefinition rig, string sourcePath)
        {
            Validate(payload, sourcePath);
            payload.RequireValid();
        }
    }

    internal sealed class CharacterPoseHistoryCollectorNodeDefinition :
        CharacterPoseNodeDefinition<CharacterPoseHistoryCollectorPayload>
    {
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.PoseHistoryCollector;
        public override GraphAuthoringCapabilityDescriptor Declare() =>
            Node<CharacterPoseHistoryCollectorPayload>(CharacterPoseNodeKind.PoseHistoryCollector, new[] { CharacterPoseGraphAuthoringCapabilities.StatePoseGraph }, "Pose History Collector", "Sources", SourceColor,
                Fields(Field("history-id", "History", GraphAuthoringFieldValueKind.IdentityReference, "pose-history")),
                Ports(In("pose.local.input", "Local Pose", "pose.local"), Out("pose.local", "Local Pose", "pose.local"), Out("history.pose", "Previous Pose History", "pose.history")),
                executionDomain: CharacterPoseExecutionDomain.SourceCapture);

        public override CharacterPoseNodePayload CreatePayload(CharacterPoseAuthoringPayloadInput input) =>
            new CharacterPoseHistoryCollectorPayload(
                new CharacterPoseHistoryId(input.Require<string>("history-id")));

        protected override object ReadField(CharacterPoseHistoryCollectorPayload payload, string field) =>
            field == "history-id"
                ? payload.HistoryId.Value
                : base.ReadField(payload, field);

        protected override void Validate(CharacterPoseHistoryCollectorPayload payload, string sourcePath) =>
            CharacterPoseNodeDefinitionValidation.Require(
                payload.HistoryId.IsValid,
                sourcePath,
                "Pose History identity is missing.");
    }

    internal sealed class CharacterEntryPoseInputNodeDefinition :
        CharacterPoseNodeDefinition<CharacterEntryPoseInputPayload>
    {
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.EntryPoseInput;
        public override GraphAuthoringCapabilityDescriptor Declare() =>
            Node<CharacterEntryPoseInputPayload>(CharacterPoseNodeKind.EntryPoseInput, new[] { CharacterPoseGraphAuthoringCapabilities.Subgraph }, "Entry Pose Input", "Inputs", InputColor,
                Array.Empty<GraphAuthoringFieldDescriptor>(),
                Ports(InterfaceOut("pose.local", "Local Pose", "pose.local", "entry.pose")),
                executionDomain: CharacterPoseExecutionDomain.SourceCapture);
    }
}
