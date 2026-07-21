using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;

namespace ThirdPersonCharacter.Editor.MotionMatching
{
    public static class MotionMatchingAuthoringPayloadCompiler
    {
        public static MotionMatchingFeatureSchemaPayload CompileFeatureSchema(
            CharacterMotionMatchingFeatureSchema schema,
            CharacterMotionMatchingTrajectoryPolicy trajectoryPolicy)
        {
            if (!schema || !trajectoryPolicy)
                throw new ArgumentNullException(!schema ? nameof(schema) : nameof(trajectoryPolicy));
            schema.RequireValid();
            trajectoryPolicy.RequireValid();
            var historyHorizons = new List<float>();
            MotionMatchingFeatureChannel enabled = MotionMatchingFeatureChannel.None;
            for (int i = 0; i < schema.TrajectoryHorizons.Count; i++)
            {
                MotionMatchingFeatureHorizon horizon = schema.TrajectoryHorizons[i];
                enabled |= horizon.Channels;
                if (horizon.TimeOffset <= 0f)
                    historyHorizons.Add(horizon.TimeOffset);
            }
            for (int i = 0; i < schema.BoneFeatures.Count; i++)
            {
                if (!schema.BoneFeatures[i].Position || !schema.BoneFeatures[i].Velocity)
                    throw new InvalidOperationException("First-version Motion Matching dense pose layout requires every declared Bone to provide both position and velocity.");
            }
            var ranges = new List<MotionMatchingFeatureRange>();
            int offset = 0;
            AddRange(enabled, MotionMatchingFeatureChannel.TrajectoryPosition, MotionMatchingCostGroup.TrajectoryPosition, trajectoryPolicy.Points.Count * 2, ranges, ref offset);
            AddRange(enabled, MotionMatchingFeatureChannel.TrajectoryFacing, MotionMatchingCostGroup.TrajectoryFacing, trajectoryPolicy.Points.Count * 2, ranges, ref offset);
            AddRange(enabled, MotionMatchingFeatureChannel.TrajectoryVelocity, MotionMatchingCostGroup.TrajectoryVelocity, trajectoryPolicy.Points.Count * 2, ranges, ref offset);
            AddRange(enabled, MotionMatchingFeatureChannel.PosePosition, MotionMatchingCostGroup.PosePosition, schema.BoneFeatures.Count * historyHorizons.Count * 3, ranges, ref offset);
            AddRange(enabled, MotionMatchingFeatureChannel.PoseVelocity, MotionMatchingCostGroup.PoseVelocity, schema.BoneFeatures.Count * historyHorizons.Count * 3, ranges, ref offset);
            if ((enabled & (MotionMatchingFeatureChannel.LeftFoot | MotionMatchingFeatureChannel.RightFoot)) != 0)
            {
                ranges.Add(new MotionMatchingFeatureRange(MotionMatchingCostGroup.ContactSoft, offset, 8));
                offset += 8;
            }
            if (offset <= 0)
                throw new InvalidOperationException("Motion Matching Feature Schema compiles to an empty dense layout.");
            var initializationMask = new bool[offset];
            for (int i = 0; i < ranges.Count; i++)
            {
                MotionMatchingFeatureRange range = ranges[i];
                MotionMatchingFeatureChannel channel = ToChannel(range.Group);
                bool enabledForInitialization = (schema.InitializationFeatureMask & channel) != 0;
                for (int feature = 0; feature < range.Count; feature++)
                    initializationMask[range.Offset + feature] = enabledForInitialization;
            }
            var boneIds = new string[schema.BoneFeatures.Count];
            for (int i = 0; i < boneIds.Length; i++)
                boneIds[i] = schema.BoneFeatures[i].BoneId.Value;
            return new MotionMatchingFeatureSchemaPayload(
                schema.FeatureSchemaId,
                schema.Revision,
                schema.Rig.RigId,
                schema.Rig.Revision,
                historyHorizons.ToArray(),
                boneIds,
                ranges.ToArray(),
                initializationMask,
                offset);
        }

        public static MotionMatchingTrajectoryPolicyPayload CompileTrajectoryPolicy(CharacterMotionMatchingTrajectoryPolicy source)
        {
            if (!source)
                throw new ArgumentNullException(nameof(source));
            source.RequireValid();
            var points = new MotionMatchingTrajectoryPolicyRuntimePoint[source.Points.Count];
            for (int i = 0; i < points.Length; i++)
            {
                MotionMatchingTrajectoryPolicyPoint point = source.Points[i];
                points[i] = new MotionMatchingTrajectoryPolicyRuntimePoint(
                    point.TimeOffset,
                    point.AcceptedPositionTolerance,
                    point.AcceptedFacingToleranceDegrees,
                    point.AcceptedConfidence,
                    point.SelectedPositionTolerance,
                    point.SelectedFacingToleranceDegrees,
                    point.SelectedConfidence);
            }
            return new MotionMatchingTrajectoryPolicyPayload(
                source.PolicyId,
                source.Revision,
                source.MaximumAcceleration,
                source.MaximumTurnRateDegrees,
                source.SelectedAgePositionTolerancePerSecond,
                source.SelectedAgeFacingTolerancePerSecond,
                source.SelectedAgeConfidenceDecayPerSecond,
                points);
        }

        public static MotionMatchingCostProfilePayload CompileCostProfile(
            CharacterMotionMatchingCostProfile source,
            MotionMatchingFeatureSchemaPayload featureSchema)
        {
            if (!source)
                throw new ArgumentNullException(nameof(source));
            if (featureSchema == null)
                throw new ArgumentNullException(nameof(featureSchema));
            source.RequireValid();
            var dense = new float[featureSchema.DenseFeatureCount];
            for (int i = 0; i < dense.Length; i++)
                dense[i] = 1f;
            var groups = new float[Enum.GetValues(typeof(MotionMatchingCostGroup)).Length + 1];
            for (int i = 0; i < source.Weights.Count; i++)
                groups[(int)source.Weights[i].Group] = source.Weights[i].Weight;
            return new MotionMatchingCostProfilePayload(source.CostProfileId, source.Revision, dense, groups);
        }

        public static MotionMatchingSearchPolicyPayload CompileSearchPolicy(CharacterMotionMatchingSearchPolicy source)
        {
            if (!source)
                throw new ArgumentNullException(nameof(source));
            source.RequireValid();
            return new MotionMatchingSearchPolicyPayload(
                source.SearchPolicyId,
                source.Revision,
                source.TopK,
                source.LeafCapacity,
                source.PlanSampleCount,
                source.PlanSampleInterval,
                source.SearchInterval,
                source.MinimumJumpInterval,
                source.MaximumAdmittedSampleCount,
                source.MaximumTreeDepth,
                source.HistoryCapacity,
                source.DiagnosticDetailCapacity,
                source.ProtectedFootPositionJumpLimit,
                source.ProtectedFootVelocityJumpLimit);
        }

        static void AddRange(
            MotionMatchingFeatureChannel enabled,
            MotionMatchingFeatureChannel channel,
            MotionMatchingCostGroup group,
            int count,
            List<MotionMatchingFeatureRange> ranges,
            ref int offset)
        {
            if ((enabled & channel) == 0)
                return;
            if (count <= 0)
                throw new InvalidOperationException($"Motion Matching feature group '{group}' has no dense values.");
            ranges.Add(new MotionMatchingFeatureRange(group, offset, count));
            offset += count;
        }

        static MotionMatchingFeatureChannel ToChannel(MotionMatchingCostGroup group)
        {
            switch (group)
            {
                case MotionMatchingCostGroup.TrajectoryPosition: return MotionMatchingFeatureChannel.TrajectoryPosition;
                case MotionMatchingCostGroup.TrajectoryFacing: return MotionMatchingFeatureChannel.TrajectoryFacing;
                case MotionMatchingCostGroup.TrajectoryVelocity: return MotionMatchingFeatureChannel.TrajectoryVelocity;
                case MotionMatchingCostGroup.PosePosition: return MotionMatchingFeatureChannel.PosePosition;
                case MotionMatchingCostGroup.PoseVelocity: return MotionMatchingFeatureChannel.PoseVelocity;
                case MotionMatchingCostGroup.ContactSoft: return MotionMatchingFeatureChannel.LeftFoot | MotionMatchingFeatureChannel.RightFoot;
                default: return MotionMatchingFeatureChannel.None;
            }
        }
    }
}
