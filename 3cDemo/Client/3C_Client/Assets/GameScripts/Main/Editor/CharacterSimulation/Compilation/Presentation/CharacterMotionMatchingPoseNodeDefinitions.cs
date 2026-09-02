using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Editor;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class CharacterMotionMatchingPoseNodeDefinition :
        CharacterPoseNodeDefinition<CharacterMotionMatchingPosePayload>
    {
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.MotionMatchingPose;
        public override CharacterPoseOperationCode OperationCode => CharacterPoseOperationCode.MotionMatchingPose;

        public override CharacterPoseNodePayload CreatePayload(CharacterPoseAuthoringPayloadInput input) =>
            new CharacterMotionMatchingPosePayload(
                input.Require<CharacterMotionMatchingBinding>("binding"),
                input.Require<CharacterAnimationBlendPolicy>("jump-blend-policy"),
                new PoseGraphId(input.Require<string>("entry-graph-id")),
                Enum.Parse<CharacterMotionMatchingRelevanceResetPolicy>(input.Require<string>("relevance-reset-policy"), false),
                Enum.Parse<CharacterMotionMatchingSearchCadencePolicy>(input.Require<string>("search-cadence-policy"), false));

        protected override object ReadField(CharacterMotionMatchingPosePayload payload, string field) => field switch
        {
            "binding" => payload.Binding,
            "jump-blend-policy" => payload.JumpBlendPolicy,
            "entry-graph-id" => payload.EntryGraph?.PoseGraphId.Value ?? string.Empty,
            "relevance-reset-policy" => payload.RelevanceResetPolicy.ToString(),
            "search-cadence-policy" => payload.SearchCadencePolicy.ToString(),
            _ => base.ReadField(payload, field)
        };

        protected override void Validate(CharacterMotionMatchingPosePayload payload, string sourcePath)
        {
            CharacterPoseNodeDefinitionValidation.Require(payload.Binding, sourcePath, "Motion Matching Binding is missing.");
            CharacterPoseNodeDefinitionValidation.Require(payload.JumpBlendPolicy, sourcePath, "Motion Matching Jump Blend Policy is missing.");
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
            payload.RequireValid(rig);
        }
    }

    internal sealed class CharacterPoseHistoryCollectorNodeDefinition :
        CharacterPoseNodeDefinition<CharacterPoseHistoryCollectorPayload>
    {
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.PoseHistoryCollector;
        public override CharacterPoseOperationCode OperationCode => CharacterPoseOperationCode.PoseHistoryRead;

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
        public override CharacterPoseNativeNodeRole NativeRole => CharacterPoseNativeNodeRole.GraphInput;
    }
}
