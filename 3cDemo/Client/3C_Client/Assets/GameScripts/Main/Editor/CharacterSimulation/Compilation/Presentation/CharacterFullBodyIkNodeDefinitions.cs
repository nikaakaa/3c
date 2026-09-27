using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Presentation;
using BTSMTL.Authoring.Graph;
using static ThirdPersonCharacter.Editor.CharacterSimulation.CharacterPoseCapabilityDeclarations;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class CharacterFootPlacementPoseNodeDefinition :
        CharacterPoseNodeDefinition<CharacterFootPlacementPosePayload>
    {
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.FootPlacement;
        public override GraphAuthoringCapabilityDescriptor Declare() =>
            Node<CharacterFootPlacementPosePayload>(CharacterPoseNodeKind.FootPlacement, AllPoseGraphs, "Foot Placement", "Goal Sources", ConstraintColor,
                Fields(ResourceField("profile", "Profile"), ResourceField("calibration", "Calibration")),
                Ports(In("pose", "Component Pose", "pose.component"), OptionalIn("weight", "Weight Override", "pose.parameter"), Out("contribution", "Goal Contribution", "component.full-body-ik-goal-contribution")),
                executionDomain: CharacterPoseExecutionDomain.WorldAwareValue);

        public override CharacterPoseNodePayload CreatePayload(CharacterPoseAuthoringPayloadInput input) =>
            new CharacterFootPlacementPosePayload(
                input.Require<CharacterPoseResourceSlot>("profile"),
                input.Require<CharacterPoseResourceSlot>("calibration"));

        protected override object ReadField(CharacterFootPlacementPosePayload payload, string field) =>
            field switch
            {
                "profile" => payload.ProfileSlot,
                "calibration" => payload.CalibrationSlot,
                _ => base.ReadField(payload, field)
            };

        protected override void Validate(CharacterFootPlacementPosePayload payload, string sourcePath)
        {
            CharacterPoseNodeDefinitionValidation.Require(
                payload.ProfileSlot && payload.CalibrationSlot,
                sourcePath,
                "Foot Placement profile or calibration is missing.");
        }

        protected override void ValidateRig(
            CharacterFootPlacementPosePayload payload,
            CharacterAnimationRigDefinition rig,
            string sourcePath)
        {
        }
    }

    internal sealed class CharacterPoseBoneIkGoalsNodeDefinition :
        CharacterPoseNodeDefinition<CharacterPoseBoneIkGoalsPayload>
    {
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.PoseBoneIKGoals;
        public override GraphAuthoringCapabilityDescriptor Declare() =>
            Node<CharacterPoseBoneIkGoalsPayload>(CharacterPoseNodeKind.PoseBoneIKGoals, AllPoseGraphsWithLinkedEntry, "Pose Bone IK Goals", "Goal Sources", ConstraintColor,
                Fields(Field("bindings", "Effector Bindings", GraphAuthoringFieldValueKind.Object, "full-body-ik-goal-binding")),
                Ports(In("pose", "Component Pose", "pose.component"), Out("contribution", "Goal Contribution", "component.full-body-ik-goal-contribution")),
                executionDomain: CharacterPoseExecutionDomain.ManagedConstraint);

        public override CharacterPoseNodePayload CreatePayload(CharacterPoseAuthoringPayloadInput input) =>
            new CharacterPoseBoneIkGoalsPayload(
                input.Require<CharacterPoseBoneIkGoalBinding[]>("bindings"));

        protected override object ReadField(CharacterPoseBoneIkGoalsPayload payload, string field) =>
            field == "bindings"
                ? payload.Bindings.ToArray()
                : base.ReadField(payload, field);

        protected override void Validate(CharacterPoseBoneIkGoalsPayload payload, string sourcePath)
        {
            CharacterPoseNodeDefinitionValidation.Require(
                payload.Bindings.Count > 0 &&
                payload.Bindings.Count <= CharacterFullBodyIkGoalSetHeader.MaximumGoalCount,
                sourcePath,
                "Pose Bone IK Goals requires one to ten bindings.");
            var slots = new HashSet<CharacterFullBodyIkEffectorSlot>();
            for (int i = 0; i < payload.Bindings.Count; i++)
            {
                CharacterPoseBoneIkGoalBinding binding = payload.Bindings[i];
                CharacterPoseNodeDefinitionValidation.Require(
                    binding != null &&
                    binding.EffectorSlot >= CharacterFullBodyIkEffectorSlot.Body &&
                    binding.EffectorSlot <= CharacterFullBodyIkEffectorSlot.RightFoot &&
                    binding.TargetPoseBoneId.IsValid &&
                    CharacterPoseNodeDefinitionValidation.Finite(binding.PositionOffset) &&
                    CharacterPoseNodeDefinitionValidation.Finite(binding.RotationOffset) &&
                    slots.Add(binding.EffectorSlot),
                    sourcePath,
                    $"Pose Bone IK Goal binding #{i} is invalid or duplicates an Effector Slot.");
                CharacterPoseNodeDefinitionValidation.RequireWeight(binding.PositionWeight, sourcePath);
                CharacterPoseNodeDefinitionValidation.RequireWeight(binding.RotationWeight, sourcePath);
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

    internal sealed class CharacterFullBodyIkPoseNodeDefinition :
        CharacterPoseNodeDefinition<CharacterFullBodyIkPosePayload>
    {
        public override CharacterPoseNodeKind Kind => CharacterPoseNodeKind.FullBodyIK;
        public override GraphAuthoringCapabilityDescriptor Declare() =>
            Node<CharacterFullBodyIkPosePayload>(CharacterPoseNodeKind.FullBodyIK, AllPoseGraphs, "Full Body IK", "Constraints", ConstraintColor,
                Fields(ReadOnlyField("backend", "Solver Backend", GraphAuthoringFieldValueKind.String)),
                Ports(In("pose", "Component Pose", "pose.component"), OptionalIn("goals", "Full Body IK Goals", "component.full-body-ik-goals"), Out("result", "Solved Component Pose", "pose.component")),
                GraphAuthoringDynamicPortPolicy.OrderedInputs,
                commands: new[]
                {
                    new GraphAuthoringCommandDescriptor(CharacterPoseGraphAuthoringCapabilities.OpenFullBodyIkProfile, "Edit FinalIK FBBIK Profile", false)
                },
                executionDomain: CharacterPoseExecutionDomain.ManagedConstraint);

        public override CharacterPoseNodePayload CreatePayload(CharacterPoseAuthoringPayloadInput input) =>
            new CharacterFullBodyIkPosePayload();

        protected override object ReadField(CharacterFullBodyIkPosePayload payload, string field) =>
            field == "backend"
                ? CharacterFinalIkPoseBufferBackend.SourceIdentity
                : base.ReadField(payload, field);

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

    internal sealed class CharacterFullBodyIkGoalAssemblerNodeDefinition :
        CharacterPoseNodeDefinition<CharacterFullBodyIkGoalAssemblerPayload>
    {
        public override CharacterPoseNodeKind Kind =>
            CharacterPoseNodeKind.FullBodyIkGoalAssembler;
        public override GraphAuthoringCapabilityDescriptor Declare() =>
            Node<CharacterFullBodyIkGoalAssemblerPayload>(CharacterPoseNodeKind.FullBodyIkGoalAssembler, AllPoseGraphs, "Goal Assembler", "Goal Sources", ConstraintColor,
                Array.Empty<GraphAuthoringFieldDescriptor>(),
                Ports(Out("goals", "Full Body IK Goals", "component.full-body-ik-goals")),
                GraphAuthoringDynamicPortPolicy.OrderedInputs,
                executionDomain: CharacterPoseExecutionDomain.ManagedConstraint,
                systemOwned: true);

        public override CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            new CharacterFullBodyIkGoalAssemblerPayload();
    }
}
