using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Editor;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class CharacterMotionMatchingPoseCompilerHandler :
        CharacterPoseCompilerHandler<CharacterMotionMatchingPosePayload>
    {
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.MotionMatchingPose;
        public override CharacterPoseOperationCode Code => CharacterPoseOperationCode.MotionMatchingPose;

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
            CharacterPoseCompilerHandlerValidation.Require(payload.Binding, sourcePath, "Motion Matching Binding is missing.");
            CharacterPoseCompilerHandlerValidation.Require(payload.JumpBlendPolicy, sourcePath, "Motion Matching Jump Blend Policy is missing.");
            CharacterPoseCompilerHandlerValidation.Require(payload.EntryGraph != null && payload.EntryGraph.PoseGraphId.IsValid, sourcePath, "Motion Matching entry graph identity is missing.");
            CharacterPoseCompilerHandlerValidation.Require(Enum.IsDefined(typeof(CharacterMotionMatchingRelevanceResetPolicy), payload.RelevanceResetPolicy), sourcePath, "Motion Matching relevance reset policy is invalid.");
            CharacterPoseCompilerHandlerValidation.Require(Enum.IsDefined(typeof(CharacterMotionMatchingSearchCadencePolicy), payload.SearchCadencePolicy), sourcePath, "Motion Matching search cadence policy is invalid.");
        }

        protected override void ValidateRig(CharacterMotionMatchingPosePayload payload, CharacterAnimationRigDefinition rig, string sourcePath)
        {
            Validate(payload, sourcePath);
            payload.RequireValid(rig);
        }
    }

    internal sealed class CharacterPoseHistoryCollectorCompilerHandler :
        CharacterPoseCompilerHandler<CharacterPoseHistoryCollectorPayload>
    {
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.PoseHistoryCollector;
        public override CharacterPoseOperationCode Code => CharacterPoseOperationCode.PoseHistoryRead;

        public override CharacterPoseNodePayload CreatePayload(CharacterPoseAuthoringPayloadInput input) =>
            new CharacterPoseHistoryCollectorPayload(
                new CharacterPoseHistoryId(input.Require<string>("history-id")));

        protected override object ReadField(CharacterPoseHistoryCollectorPayload payload, string field) =>
            field == "history-id"
                ? payload.HistoryId.Value
                : base.ReadField(payload, field);

        protected override void Validate(CharacterPoseHistoryCollectorPayload payload, string sourcePath) =>
            CharacterPoseCompilerHandlerValidation.Require(
                payload.HistoryId.IsValid,
                sourcePath,
                "Pose History identity is missing.");
    }

    internal sealed class CharacterEntryPoseInputCompilerHandler :
        CharacterPoseCompilerHandler<CharacterEntryPoseInputPayload>
    {
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.EntryPoseInput;
        public override CharacterPoseNativeNodeRole NativeRole => CharacterPoseNativeNodeRole.GraphInput;
    }
}
