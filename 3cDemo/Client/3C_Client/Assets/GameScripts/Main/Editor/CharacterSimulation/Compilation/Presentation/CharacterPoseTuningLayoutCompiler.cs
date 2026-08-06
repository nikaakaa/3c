using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterPoseTuningCompilationResult
    {
        public CharacterPoseTuningCompilationResult(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock defaultBlock,
            string publishedParameterRevision)
        {
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            DefaultBlock = defaultBlock ?? throw new ArgumentNullException(nameof(defaultBlock));
            PublishedParameterRevision = string.IsNullOrWhiteSpace(publishedParameterRevision)
                ? throw new ArgumentException("Published parameter revision is required.", nameof(publishedParameterRevision))
                : publishedParameterRevision;
        }

        public CharacterPoseTuningLayout Layout { get; }
        public CharacterPoseTuningParameterBlock DefaultBlock { get; }
        public string PublishedParameterRevision { get; }
    }

    internal static class CharacterPoseTuningLayoutCompiler
    {
        sealed class FieldValue
        {
            public CharacterPoseTuningLayoutEntry Entry;
            public float FloatValue;
            public int IntegerValue;
            public byte BooleanValue;
            public int EnumValue;
        }

        public static CharacterPoseTuningCompilationResult Compile(
            string programId,
            CharacterPresentationProjection projection)
        {
            if (string.IsNullOrWhiteSpace(programId))
                throw new ArgumentException("Program identity is required.", nameof(programId));
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            projection.PosePlan.RequireValid();
            projection.Rig.RequireValid();

            var fields = new List<FieldValue>();
            var consumerOwners = new HashSet<string>(StringComparer.Ordinal);
            AddOperationWeights(projection.PosePlan, fields);
            AddSequencePlayRates(projection.PosePlan, fields);
            AddStateMachineTransitionDurations(projection.PosePlan, fields);
            AddBlendStackPolicies(projection.PosePlan, fields);
            AddInertializationPolicies(projection.PosePlan, fields);
            for (int i = 0; i < projection.PosePlan.FullBodyIks.Count; i++)
            {
                CharacterFullBodyIkProfile profile = projection.PosePlan.FullBodyIks[i].Profile;
                string ownerId = $"full-body-ik-profile:{profile.ProfileId}";
                if (consumerOwners.Add(ownerId))
                    AddFullBodyIkFields(profile, ownerId, fields);
            }
            for (int i = 0; i < projection.PosePlan.PredictiveFootPlacements.Count; i++)
            {
                CharacterFootPlacementProfile profile =
                    projection.PosePlan.PredictiveFootPlacements[i].Profile;
                string ownerId = $"foot-placement-profile:{profile.ProfileId}";
                if (consumerOwners.Add(ownerId))
                    AddFootPlacementFields(profile, ownerId, fields);
            }
            if (fields.Count == 0)
                throw new InvalidOperationException("Pose tuning compiler found no classified fields.");

            fields.Sort((left, right) =>
            {
                int consumer = string.CompareOrdinal(left.Entry.ConsumerId, right.Entry.ConsumerId);
                return consumer != 0
                    ? consumer
                    : string.CompareOrdinal(left.Entry.FieldId, right.Entry.FieldId);
            });
            var entries = fields.Select(value => value.Entry).ToArray();
            var consumers = BuildConsumerRanges(entries);
            CharacterPoseTuningLayout layout = CharacterPoseTuningLayout.Create(
                programId,
                projection.ProjectionRevision,
                projection.PosePlan.PlanHash,
                projection.Rig.RigId,
                projection.Rig.RigRevision,
                entries,
                consumers);
            var floats = new float[fields.Count(value => value.Entry.ValueKind == CharacterPoseTuningValueKind.Float)];
            var integers = new int[fields.Count(value => value.Entry.ValueKind == CharacterPoseTuningValueKind.Integer)];
            var booleans = new byte[fields.Count(value => value.Entry.ValueKind == CharacterPoseTuningValueKind.Boolean)];
            var enums = new int[fields.Count(value => value.Entry.ValueKind == CharacterPoseTuningValueKind.Enum)];
            for (int i = 0; i < fields.Count; i++)
            {
                FieldValue field = fields[i];
                switch (field.Entry.ValueKind)
                {
                    case CharacterPoseTuningValueKind.Float:
                        floats[field.Entry.ValueIndex] = field.FloatValue;
                        break;
                    case CharacterPoseTuningValueKind.Integer:
                        integers[field.Entry.ValueIndex] = field.IntegerValue;
                        break;
                    case CharacterPoseTuningValueKind.Boolean:
                        booleans[field.Entry.ValueIndex] = field.BooleanValue;
                        break;
                    case CharacterPoseTuningValueKind.Enum:
                        enums[field.Entry.ValueIndex] = field.EnumValue;
                        break;
                }
            }
            var defaultBlock = new CharacterPoseTuningParameterBlock(
                layout.LayoutHash,
                floats,
                integers,
                booleans,
                enums);
            defaultBlock.RequireValid(layout);
            string publishedRevision = StableHash.Compute(
                "character-pose-tuning-parameters",
                projection.PosePlan.ContentRevision,
                layout.LayoutHash,
                Describe(defaultBlock)).ToString();
            return new CharacterPoseTuningCompilationResult(layout, defaultBlock, publishedRevision);
        }

        static void AddOperationWeights(
            CharacterPresentationPosePlan plan,
            List<FieldValue> fields)
        {
            int nextFloat = 0;
            for (int i = 0; i < plan.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = plan.Operations[i];
                if (!IsTunableOperationWeight(operation.Code))
                    continue;
                AddFloat(
                    fields,
                    $"pose-node:{operation.NodeId.Value}",
                    $"pose-node:{operation.NodeId.Value}/weight",
                    "Default Weight",
                    operation.Weight,
                    0f,
                    1f,
                    "normalized",
                    "pose-plan",
                    CharacterPoseTuningApplyTiming.NextFrame,
                    CharacterPoseTuningStatePolicy.PreserveState,
                    ref nextFloat);
            }
        }

        static bool IsTunableOperationWeight(CharacterPoseOperationCode code) =>
            code == CharacterPoseOperationCode.BlendPose ||
            code == CharacterPoseOperationCode.LayeredBoneBlend ||
            code == CharacterPoseOperationCode.AdditivePose;

        static void AddSequencePlayRates(
            CharacterPresentationPosePlan plan,
            List<FieldValue> fields)
        {
            int nextFloat = NextIndex(fields, CharacterPoseTuningValueKind.Float);
            for (int i = 0; i < plan.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = plan.Operations[i];
                if (operation.Code != CharacterPoseOperationCode.SequencePlayer)
                    continue;
                CharacterPresentationSequencePlayerDescriptor player =
                    plan.SequencePlayers[operation.SequencePlayerIndex];
                string ownerId = $"pose-node:{operation.NodeId.Value}";
                AddFloat(
                    fields,
                    ownerId,
                    $"{ownerId}/play-rate",
                    "Sequence Play Rate",
                    player.PlayRate,
                    0.01f,
                    8f,
                    "multiplier",
                    "pose-plan",
                    CharacterPoseTuningApplyTiming.NextFrame,
                    CharacterPoseTuningStatePolicy.PreserveState,
                    ref nextFloat);
            }
        }

        static void AddStateMachineTransitionDurations(
            CharacterPresentationPosePlan plan,
            List<FieldValue> fields)
        {
            int nextFloat = NextIndex(fields, CharacterPoseTuningValueKind.Float);
            for (int machineIndex = 0;
                 machineIndex < plan.StateMachines.Count;
                 machineIndex++)
            {
                CharacterPoseStateMachineDescriptor machine =
                    plan.StateMachines[machineIndex];
                string ownerId = $"pose-state-machine:{machine.StateMachineId.Value}";
                for (int transitionIndex = 0;
                     transitionIndex < machine.Transitions.Count;
                     transitionIndex++)
                {
                    CharacterPoseStateTransitionDescriptor transition =
                        machine.Transitions[transitionIndex];
                    string prefix =
                        $"{ownerId}/transition:{transition.TransitionId.Value}";
                    AddFloat(
                        fields,
                        ownerId,
                        $"{prefix}/duration",
                        "Transition Duration",
                        transition.DurationSeconds,
                        0f,
                        60f,
                        "seconds",
                        ownerId,
                        CharacterPoseTuningApplyTiming.NextActivation,
                        CharacterPoseTuningStatePolicy.PreserveState,
                        ref nextFloat);
                }
            }
        }

        static void AddBlendStackPolicies(
            CharacterPresentationPosePlan plan,
            List<FieldValue> fields)
        {
            var owners = new HashSet<string>(StringComparer.Ordinal);
            int nextFloat = NextIndex(fields, CharacterPoseTuningValueKind.Float);
            int nextInteger = NextIndex(fields, CharacterPoseTuningValueKind.Integer);
            int nextEnum = NextIndex(fields, CharacterPoseTuningValueKind.Enum);
            for (int i = 0; i < plan.BlendNodes.Count; i++)
            {
                AnimationBlendNodePayload blend = plan.BlendNodes[i];
                string ownerId = $"animation-blend-policy:{blend.PolicyId}";
                if (!owners.Add(ownerId))
                    continue;
                AddInteger(
                    fields,
                    ownerId,
                    $"{ownerId}/max-active-source-entries",
                    "Max Active Sources",
                    blend.StackPolicy.MaxActiveSourceEntries,
                    2,
                    64,
                    "count",
                    ref nextInteger,
                    CharacterPoseTuningInteractionPolicy.Structural);
                AddEnum(
                    fields,
                    ownerId,
                    $"{ownerId}/stored-pose-policy",
                    "Stored Pose Policy",
                    (int)blend.StackPolicy.StoredPosePolicy,
                    (int)AnimationStoredPosePolicy.Disabled,
                    (int)AnimationStoredPosePolicy.CompressOldest,
                    ref nextEnum,
                    CharacterPoseTuningInteractionPolicy.Structural);
                AddFloat(
                    fields,
                    ownerId,
                    $"{ownerId}/max-blend-in-time-to-replace-newest",
                    "Replace Newest Window",
                    blend.StackPolicy.MaxBlendInTimeToReplaceNewest,
                    0f,
                    10f,
                    "seconds",
                    ownerId,
                    CharacterPoseTuningApplyTiming.NextActivation,
                    CharacterPoseTuningStatePolicy.PreserveState,
                    ref nextFloat);
                AddFloat(
                    fields,
                    ownerId,
                    $"{ownerId}/depth-blend-time-multiplier",
                    "Depth Blend Time Multiplier",
                    blend.StackPolicy.DepthBlendTimeMultiplier,
                    0.01f,
                    100f,
                    "multiplier",
                    ownerId,
                    CharacterPoseTuningApplyTiming.NextActivation,
                    CharacterPoseTuningStatePolicy.PreserveState,
                    ref nextFloat);
            }
        }

        static void AddInertializationPolicies(
            CharacterPresentationPosePlan plan,
            List<FieldValue> fields)
        {
            var owners = new HashSet<string>(StringComparer.Ordinal);
            int nextFloat = NextIndex(fields, CharacterPoseTuningValueKind.Float);
            for (int i = 0; i < plan.Inertializations.Count; i++)
            {
                CharacterPresentationInertializationDescriptor descriptor =
                    plan.Inertializations[i];
                if (descriptor.TemporalOwnerKind !=
                    PoseInertializationTemporalOwnerKind.DirectPlayerPolicy)
                {
                    continue;
                }
                string ownerId =
                    $"pose-inertialization-policy:{descriptor.PolicyId}";
                if (!owners.Add(ownerId))
                    continue;
                CharacterPresentationInertializationRuleDescriptor rule =
                    descriptor.Rules.Single();
                if (rule.Mode != PoseInertializationMode.Inertialize)
                    continue;
                AddFloat(
                    fields,
                    ownerId,
                    $"{ownerId}/duration-seconds",
                    "Inertialization Duration",
                    rule.DurationSeconds,
                    0.001f,
                    10f,
                    "seconds",
                    ownerId,
                    CharacterPoseTuningApplyTiming.NextActivation,
                    CharacterPoseTuningStatePolicy.PreserveState,
                    ref nextFloat);
            }
        }

        static void AddFullBodyIkFields(
            CharacterFullBodyIkProfile profile,
            string ownerId,
            List<FieldValue> fields)
        {
            int nextFloat = NextIndex(fields, CharacterPoseTuningValueKind.Float);
            int nextInteger = NextIndex(fields, CharacterPoseTuningValueKind.Integer);
            int nextBoolean = NextIndex(fields, CharacterPoseTuningValueKind.Boolean);
            int nextEnum = NextIndex(fields, CharacterPoseTuningValueKind.Enum);
            AddInteger(fields, ownerId, $"{ownerId}/iterations", "Iterations", profile.Iterations, 0, 10, "count", ref nextInteger);
            AddBoolean(fields, ownerId, $"{ownerId}/fabrik-pass", "FABRIK Pass", profile.FabrikPass, ref nextBoolean);
            AddFloat(fields, ownerId, $"{ownerId}/spine-stiffness", "Spine Stiffness", profile.SpineStiffness, 0f, 1f, "normalized", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/pull-body-vertical", "Pull Body Vertical", profile.PullBodyVertical, -1f, 1f, "normalized", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/pull-body-horizontal", "Pull Body Horizontal", profile.PullBodyHorizontal, -1f, 1f, "normalized", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/node-weight", "Node Weight", profile.NodeWeight, 0f, 1f, "normalized", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddLimbFields(fields, ownerId, "left-arm", "Left Arm", profile.LeftArm, ref nextFloat, ref nextEnum);
            AddLimbFields(fields, ownerId, "right-arm", "Right Arm", profile.RightArm, ref nextFloat, ref nextEnum);
            AddLimbFields(fields, ownerId, "left-leg", "Left Leg", profile.LeftLeg, ref nextFloat, ref nextEnum);
            AddLimbFields(fields, ownerId, "right-leg", "Right Leg", profile.RightLeg, ref nextFloat, ref nextEnum);
        }

        static void AddLimbFields(
            List<FieldValue> fields,
            string ownerId,
            string limbId,
            string displayName,
            CharacterFullBodyIkLimbSettings limb,
            ref int nextFloat,
            ref int nextEnum)
        {
            AddFloat(fields, ownerId, $"{ownerId}/{limbId}/pin", $"{displayName} Pin", limb.Pin, 0f, 1f, "normalized", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/{limbId}/pull", $"{displayName} Pull", limb.Pull, 0f, 1f, "normalized", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/{limbId}/push", $"{displayName} Push", limb.Push, 0f, 1f, "normalized", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/{limbId}/push-parent", $"{displayName} Push Parent", limb.PushParent, -1f, 1f, "normalized", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/{limbId}/reach", $"{displayName} Reach", limb.Reach, 0f, 1f, "normalized", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddEnum(fields, ownerId, $"{ownerId}/{limbId}/reach-smoothing", $"{displayName} Reach Smoothing", (int)limb.ReachSmoothing, 0, 2, ref nextEnum);
            AddEnum(fields, ownerId, $"{ownerId}/{limbId}/push-smoothing", $"{displayName} Push Smoothing", (int)limb.PushSmoothing, 0, 2, ref nextEnum);
            AddFloat(fields, ownerId, $"{ownerId}/{limbId}/mapping-weight", $"{displayName} Mapping Weight", limb.MappingWeight, 0f, 1f, "normalized", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/{limbId}/maintain-rotation-weight", $"{displayName} Maintain Rotation", limb.MaintainRotationWeight, 0f, 1f, "normalized", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/{limbId}/bend-constraint-weight", $"{displayName} Bend Constraint", limb.BendConstraintWeight, 0f, 1f, "normalized", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/{limbId}/bend-clamp", $"{displayName} Bend Clamp", limb.BendClamp, 0f, 1f, "normalized", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
        }

        static void AddFootPlacementFields(
            CharacterFootPlacementProfile profile,
            string ownerId,
            List<FieldValue> fields)
        {
            CharacterFinalIkGroundingSettings grounding = profile.FinalIkGrounding.Build();
            CharacterPredictiveFootPlacementRuntimeSettings predictive = profile.PredictiveExtension.Build();
            int nextFloat = NextIndex(fields, CharacterPoseTuningValueKind.Float);
            int nextInteger = NextIndex(fields, CharacterPoseTuningValueKind.Integer);
            AddInteger(fields, ownerId, $"{ownerId}/predictive/hit-capacity", "Hit Capacity", predictive.HitCapacity, 4, 32, "count", ref nextInteger, CharacterPoseTuningInteractionPolicy.Structural);
            AddInteger(fields, ownerId, $"{ownerId}/predictive/path-sample-count", "Path Sample Capacity", predictive.PathSampleCount, 1, 6, "count", ref nextInteger, CharacterPoseTuningInteractionPolicy.Structural);
            AddFloat(fields, ownerId, $"{ownerId}/grounding/maximum-step", "Maximum Step", grounding.MaximumStep, 0f, 10f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/grounding/height-offset", "Height Offset", grounding.HeightOffset, -10f, 10f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/grounding/foot-height-speed", "Foot Height Speed", grounding.FootHeightSpeed, 0f, 100f, "meters/second", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/grounding/foot-radius", "Foot Radius", grounding.FootRadius, 0f, 2f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/grounding/velocity-prediction", "Velocity Prediction", grounding.VelocityPrediction, 0f, 10f, "seconds", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/grounding/foot-rotation-weight", "Foot Rotation Weight", grounding.FootRotationWeight, 0f, 1f, "normalized", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/grounding/foot-rotation-speed", "Foot Rotation Speed", grounding.FootRotationSpeed, 0f, 100f, "degrees/second", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/grounding/maximum-foot-rotation-angle", "Maximum Foot Rotation", grounding.MaximumFootRotationAngle, 0f, 90f, "degrees", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/path-sphere-radius", "Path Sphere Radius", predictive.PathSphereRadius, 0f, 2f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/swing-capsule-radius", "Swing Capsule Radius", predictive.SwingCapsuleRadius, 0f, 2f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/cast-above", "Cast Above", predictive.CastAbove, 0f, 10f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/cast-below", "Cast Below", predictive.CastBelow, 0f, 10f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/maximum-slope-degrees", "Maximum Slope", predictive.MaximumSlopeDegrees, 0f, 89f, "degrees", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/maximum-step-up", "Maximum Step Up", predictive.MaximumStepUp, 0f, 10f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/maximum-step-down", "Maximum Step Down", predictive.MaximumStepDown, 0f, 10f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/maximum-height-discontinuity", "Maximum Height Discontinuity", predictive.MaximumHeightDiscontinuity, 0f, 10f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/maximum-edge-gap", "Maximum Edge Gap", predictive.MaximumEdgeGap, 0f, 10f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/maximum-swing-clearance", "Maximum Swing Clearance", predictive.MaximumSwingClearance, 0f, 10f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/plant-speed-threshold", "Plant Speed Threshold", predictive.PlantSpeedThreshold, 0f, 10f, "meters/second", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/unalignment-speed-threshold", "Unalignment Speed Threshold", predictive.UnalignmentSpeedThreshold, 0f, 10f, "meters/second", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/plant-confidence-enter", "Plant Confidence Enter", predictive.PlantConfidenceEnter, 0f, 1f, "normalized", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/plant-confidence-exit", "Plant Confidence Exit", predictive.PlantConfidenceExit, 0f, 1f, "normalized", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/minimum-look-ahead-seconds", "Minimum Look Ahead", predictive.MinimumLookAheadSeconds, 0f, 10f, "seconds", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/maximum-look-ahead-seconds", "Maximum Look Ahead", predictive.MaximumLookAheadSeconds, 0f, 10f, "seconds", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/maximum-yaw-velocity", "Maximum Yaw Velocity", predictive.MaximumYawVelocityDegreesPerSecond, 0f, 3600f, "degrees/second", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/maximum-prediction-distance", "Maximum Prediction Distance", predictive.MaximumPredictionDistance, 0f, 10f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/maximum-prediction-reach-ratio", "Maximum Prediction Reach", predictive.MaximumPredictionReachRatio, 0.5f, 1.25f, "ratio", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/slide-start-distance", "Slide Start Distance", predictive.SlideStartDistance, 0f, 10f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/slide-stop-distance", "Slide Stop Distance", predictive.SlideStopDistance, 0f, 10f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/maximum-slide-distance", "Maximum Slide Distance", predictive.MaximumSlideDistance, 0f, 10f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/slide-speed", "Slide Speed", predictive.SlideSpeed, 0f, 10f, "meters/second", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/replant-distance", "Replant Distance", predictive.ReplantDistance, 0f, 10f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/replant-angle-degrees", "Replant Angle", predictive.ReplantAngleDegrees, 0f, 180f, "degrees", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/minimum-foot-separation", "Minimum Foot Separation", predictive.MinimumFootSeparation, 0f, 2f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/maximum-ankle-twist-degrees", "Maximum Ankle Twist", predictive.MaximumAnkleTwistDegrees, 0f, 180f, "degrees", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/heel-lift-ratio", "Heel Lift Ratio", predictive.HeelLiftRatio, 0f, 1f, "ratio", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/minimum-leg-extension-ratio", "Minimum Leg Extension", predictive.MinimumLegExtensionRatio, 0.01f, 0.9f, "ratio", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/maximum-leg-extension-ratio", "Maximum Leg Extension", predictive.MaximumLegExtensionRatio, 0.5f, 0.999f, "ratio", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/maximum-pelvis-lowering", "Maximum Pelvis Lowering", predictive.MaximumPelvisLowering, 0f, 1f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/maximum-pelvis-raising", "Maximum Pelvis Raising", predictive.MaximumPelvisRaising, 0f, 1f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/pelvis-interpolation-speed", "Pelvis Interpolation Speed", predictive.PelvisInterpolationSpeed, 0.01f, 100f, "response", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/pelvis-height-dead-zone", "Pelvis Height Dead Zone", predictive.PelvisHeightDeadZone, 0f, 0.05f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/maximum-horizontal-foot-adjustment", "Maximum Horizontal Foot Adjustment", predictive.MaximumHorizontalFootAdjustment, 0f, 1f, "meters", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
            AddFloat(fields, ownerId, $"{ownerId}/predictive/minimum-source-contribution", "Minimum Source Contribution", predictive.MinimumSourceContribution, 0f, 1f, "normalized", ownerId, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, ref nextFloat);
        }

        static CharacterPoseTuningConsumerRange[] BuildConsumerRanges(
            IReadOnlyList<CharacterPoseTuningLayoutEntry> entries)
        {
            var owners = entries.Select(value => value.ConsumerId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var result = new List<CharacterPoseTuningConsumerRange>(owners.Length);
            for (int i = 0; i < owners.Length; i++)
            {
                int first = -1;
                int count = 0;
                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    if (!string.Equals(entries[entryIndex].ConsumerId, owners[i], StringComparison.Ordinal))
                        continue;
                    if (first < 0)
                        first = entryIndex;
                    count++;
                }
                result.Add(new CharacterPoseTuningConsumerRange(owners[i], first, count));
            }
            return result.ToArray();
        }

        static int NextIndex(
            IReadOnlyList<FieldValue> fields,
            CharacterPoseTuningValueKind kind)
        {
            int count = 0;
            for (int i = 0; i < fields.Count; i++)
                if (fields[i].Entry.ValueKind == kind)
                    count = Math.Max(count, fields[i].Entry.ValueIndex + 1);
            return count;
        }

        static void AddFloat(
            List<FieldValue> fields,
            string ownerId,
            string fieldId,
            string displayName,
            float value,
            float minimum,
            float maximum,
            string unit,
            string consumerId,
            CharacterPoseTuningApplyTiming applyTiming,
            CharacterPoseTuningStatePolicy statePolicy,
            ref int index,
            CharacterPoseTuningInteractionPolicy interaction = CharacterPoseTuningInteractionPolicy.TunableDefault)
        {
            fields.Add(new FieldValue
            {
                Entry = new CharacterPoseTuningLayoutEntry(ownerId, fieldId, displayName, interaction, CharacterPoseTuningValueKind.Float, unit, minimum, maximum, true, applyTiming, statePolicy, index, consumerId),
                FloatValue = value
            });
            index++;
        }

        static void AddInteger(
            List<FieldValue> fields,
            string ownerId,
            string fieldId,
            string displayName,
            int value,
            int minimum,
            int maximum,
            string unit,
            ref int index,
            CharacterPoseTuningInteractionPolicy interaction = CharacterPoseTuningInteractionPolicy.TunableDefault)
        {
            fields.Add(new FieldValue
            {
                Entry = new CharacterPoseTuningLayoutEntry(ownerId, fieldId, displayName, interaction, CharacterPoseTuningValueKind.Integer, unit, minimum, maximum, false, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, index, ownerId),
                IntegerValue = value
            });
            index++;
        }

        static void AddBoolean(
            List<FieldValue> fields,
            string ownerId,
            string fieldId,
            string displayName,
            bool value,
            ref int index)
        {
            fields.Add(new FieldValue
            {
                Entry = new CharacterPoseTuningLayoutEntry(ownerId, fieldId, displayName, CharacterPoseTuningInteractionPolicy.TunableDefault, CharacterPoseTuningValueKind.Boolean, string.Empty, 0f, 1f, false, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, index, ownerId),
                BooleanValue = value ? (byte)1 : (byte)0
            });
            index++;
        }

        static void AddEnum(
            List<FieldValue> fields,
            string ownerId,
            string fieldId,
            string displayName,
            int value,
            int minimum,
            int maximum,
            ref int index,
            CharacterPoseTuningInteractionPolicy interaction =
                CharacterPoseTuningInteractionPolicy.TunableDefault)
        {
            fields.Add(new FieldValue
            {
                Entry = new CharacterPoseTuningLayoutEntry(ownerId, fieldId, displayName, interaction, CharacterPoseTuningValueKind.Enum, string.Empty, minimum, maximum, false, CharacterPoseTuningApplyTiming.NextFrame, CharacterPoseTuningStatePolicy.PreserveState, index, ownerId),
                EnumValue = value
            });
            index++;
        }

        static string Describe(CharacterPoseTuningParameterBlock block)
        {
            var values = new List<string>(block.Floats.Length + block.Integers.Length + block.Booleans.Length + block.Enums.Length);
            values.AddRange(block.Floats.Select(value => value.ToString("R", CultureInfo.InvariantCulture)));
            values.AddRange(block.Integers.Select(value => value.ToString(CultureInfo.InvariantCulture)));
            values.AddRange(block.Booleans.Select(value => value.ToString(CultureInfo.InvariantCulture)));
            values.AddRange(block.Enums.Select(value => value.ToString(CultureInfo.InvariantCulture)));
            return string.Join("|", values);
        }
    }
}
