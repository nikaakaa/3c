using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class CharacterFootGroundingPoseCompilerHandler :
        CharacterPoseCompilerHandler<CharacterFootGroundingPosePayload>
    {
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.FootGrounding;
        public override CharacterPoseOperationCode Code => CharacterPoseOperationCode.FootGrounding;

        public override CharacterPoseNodePayload CreatePayload(CharacterPoseAuthoringPayloadInput input) =>
            new CharacterFootGroundingPosePayload(
                input.Require<CharacterFootPlacementProfile>("profile"),
                input.Require<CharacterFootPlacementRigCalibration>("calibration"));

        protected override object ReadField(CharacterFootGroundingPosePayload payload, string field) =>
            field switch
            {
                "profile" => payload.Profile,
                "calibration" => payload.Calibration,
                _ => base.ReadField(payload, field)
            };

        protected override void Validate(CharacterFootGroundingPosePayload payload, string sourcePath)
        {
            CharacterPoseCompilerHandlerValidation.Require(
                payload.Profile && payload.Calibration,
                sourcePath,
                "Foot Grounding profile or calibration is missing.");
            payload.Profile.RequireValid();
            CharacterPoseCompilerHandlerValidation.Require(
                string.Equals(
                    payload.Profile.Revision,
                    payload.Profile.ComputeRevision(),
                    StringComparison.Ordinal),
                sourcePath,
                "Foot Grounding profile revision is stale.");
        }

        protected override void ValidateRig(
            CharacterFootGroundingPosePayload payload,
            CharacterAnimationRigDefinition rig,
            string sourcePath)
        {
            try
            {
                payload.Calibration.RequireRig(rig);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"{sourcePath}: {exception.Message}", exception);
            }
        }
    }

    internal sealed class CharacterPredictiveFootPlacementModifierPoseCompilerHandler :
        CharacterPoseCompilerHandler<CharacterPredictiveFootPlacementModifierPosePayload>
    {
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.PredictiveFootPlacementModifier;
        public override CharacterPoseOperationCode Code => CharacterPoseOperationCode.PredictiveFootPlacementModifier;

        public override CharacterPoseNodePayload CreatePayload(CharacterPoseAuthoringPayloadInput input) =>
            new CharacterPredictiveFootPlacementModifierPosePayload();
    }

    internal sealed class CharacterPoseBoneIkGoalsCompilerHandler :
        CharacterPoseCompilerHandler<CharacterPoseBoneIkGoalsPayload>
    {
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.PoseBoneIKGoals;
        public override CharacterPoseOperationCode Code => CharacterPoseOperationCode.PoseBoneIKGoals;

        public override CharacterPoseNodePayload CreatePayload(CharacterPoseAuthoringPayloadInput input) =>
            new CharacterPoseBoneIkGoalsPayload(
                input.Require<CharacterPoseBoneIkGoalBinding[]>("bindings"));

        protected override object ReadField(CharacterPoseBoneIkGoalsPayload payload, string field) =>
            field == "bindings"
                ? payload.Bindings.ToArray()
                : base.ReadField(payload, field);

        protected override void Validate(CharacterPoseBoneIkGoalsPayload payload, string sourcePath)
        {
            CharacterPoseCompilerHandlerValidation.Require(
                payload.Bindings.Count > 0 &&
                payload.Bindings.Count <= CharacterFullBodyIkGoalSetHeader.MaximumGoalCount,
                sourcePath,
                "Pose Bone IK Goals requires one to ten bindings.");
            var slots = new HashSet<CharacterFullBodyIkEffectorSlot>();
            for (int i = 0; i < payload.Bindings.Count; i++)
            {
                CharacterPoseBoneIkGoalBinding binding = payload.Bindings[i];
                CharacterPoseCompilerHandlerValidation.Require(
                    binding != null &&
                    binding.EffectorSlot >= CharacterFullBodyIkEffectorSlot.Body &&
                    binding.EffectorSlot <= CharacterFullBodyIkEffectorSlot.RightFoot &&
                    binding.TargetPoseBoneId.IsValid &&
                    CharacterPoseCompilerHandlerValidation.Finite(binding.PositionOffset) &&
                    CharacterPoseCompilerHandlerValidation.Finite(binding.RotationOffset) &&
                    slots.Add(binding.EffectorSlot),
                    sourcePath,
                    $"Pose Bone IK Goal binding #{i} is invalid or duplicates an Effector Slot.");
                CharacterPoseCompilerHandlerValidation.RequireWeight(binding.PositionWeight, sourcePath);
                CharacterPoseCompilerHandlerValidation.RequireWeight(binding.RotationWeight, sourcePath);
            }
        }

        protected override void ValidateRig(
            CharacterPoseBoneIkGoalsPayload payload,
            CharacterAnimationRigDefinition rig,
            string sourcePath)
        {
            try
            {
                for (int i = 0; i < payload.Bindings.Count; i++)
                    rig.RequirePoseBoneIndex(payload.Bindings[i].TargetPoseBoneId);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"{sourcePath}: {exception.Message}", exception);
            }
        }
    }

    internal sealed class CharacterFullBodyIkPoseCompilerHandler :
        CharacterPoseCompilerHandler<CharacterFullBodyIkPosePayload>
    {
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.FullBodyIK;
        public override CharacterPoseOperationCode Code => CharacterPoseOperationCode.FullBodyIK;

        public override CharacterPoseNodePayload CreatePayload(CharacterPoseAuthoringPayloadInput input) =>
            new CharacterFullBodyIkPosePayload(
                input.Require<CharacterFullBodyIkProfile>("profile"));

        protected override object ReadField(CharacterFullBodyIkPosePayload payload, string field) =>
            field switch
            {
                "profile" => payload.Profile,
                "backend" => CharacterFinalIkPoseBufferBackend.SourceIdentity,
                _ => base.ReadField(payload, field)
            };

        protected override void Validate(CharacterFullBodyIkPosePayload payload, string sourcePath)
        {
            CharacterPoseCompilerHandlerValidation.Require(
                payload.Profile,
                sourcePath,
                "FinalIK FBBIK Profile is missing.");
            payload.Profile.RequireValid();
        }

        protected override void ValidateRig(
            CharacterFullBodyIkPosePayload payload,
            CharacterAnimationRigDefinition rig,
            string sourcePath)
        {
            try
            {
                rig.RequireValid();
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"{sourcePath}: {exception.Message}", exception);
            }
        }
    }
}
