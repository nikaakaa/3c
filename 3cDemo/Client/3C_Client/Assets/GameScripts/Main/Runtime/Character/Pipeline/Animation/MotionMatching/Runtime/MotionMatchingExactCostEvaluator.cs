using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public sealed class MotionMatchingExactCostEvaluator
    {
        readonly CharacterMotionMatchingRuntimeDatabase m_Database;

        public MotionMatchingExactCostEvaluator(CharacterMotionMatchingRuntimeDatabase database)
        {
            m_Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public MotionMatchingExactCostComponents Evaluate(MotionMatchingQuery query, int sampleIndex)
        {
            if ((uint)sampleIndex >= (uint)m_Database.SampleCount)
                throw new ArgumentOutOfRangeException(nameof(sampleIndex));
            float trajectoryPosition = 0f;
            float trajectoryFacing = 0f;
            float trajectoryVelocity = 0f;
            float posePosition = 0f;
            float poseVelocity = 0f;
            float contactSoft = 0f;
            for (int rangeIndex = 0; rangeIndex < m_Database.FeatureSchema.FeatureRangeCount; rangeIndex++)
            {
                MotionMatchingFeatureRange range = m_Database.FeatureSchema.GetFeatureRange(rangeIndex);
                float value;
                switch (range.Group)
                {
                    case MotionMatchingCostGroup.TrajectoryPosition:
                        value = EvaluateTrajectoryPosition(query, sampleIndex, range);
                        trajectoryPosition += value;
                        break;
                    case MotionMatchingCostGroup.TrajectoryFacing:
                        value = EvaluateTrajectoryFacing(query, sampleIndex, range);
                        trajectoryFacing += value;
                        break;
                    case MotionMatchingCostGroup.TrajectoryVelocity:
                        trajectoryVelocity += EvaluateSquaredDifference(query, sampleIndex, range);
                        break;
                    case MotionMatchingCostGroup.PosePosition:
                        posePosition += EvaluateSquaredDifference(query, sampleIndex, range);
                        break;
                    case MotionMatchingCostGroup.PoseVelocity:
                        poseVelocity += EvaluateSquaredDifference(query, sampleIndex, range);
                        break;
                    case MotionMatchingCostGroup.ContactSoft:
                        contactSoft += EvaluateSquaredDifference(query, sampleIndex, range);
                        break;
                    default:
                        throw new InvalidOperationException($"Cost group '{range.Group}' cannot own dense exact-cost features.");
                }
            }
            bool continuation = !query.Initialization && query.CurrentSampleIndex >= 0 &&
                m_Database.GetSample(query.CurrentSampleIndex).NextSampleIndex == sampleIndex;
            float continuationCost = continuation ? 0f : m_Database.CostProfile.GetGroupWeight(MotionMatchingCostGroup.Continuation);
            float jumpCost = query.Initialization || continuation ? 0f : m_Database.CostProfile.GetGroupWeight(MotionMatchingCostGroup.Jump);
            return new MotionMatchingExactCostComponents(
                trajectoryPosition,
                trajectoryFacing,
                trajectoryVelocity,
                posePosition,
                poseVelocity,
                contactSoft,
                continuationCost,
                jumpCost);
        }

        float EvaluateTrajectoryPosition(MotionMatchingQuery query, int sampleIndex, MotionMatchingFeatureRange range)
        {
            if (range.Count != query.TrajectoryEnvelope.Count * 2)
                throw new InvalidOperationException("Trajectory Position feature range does not match the query envelope.");
            float cost = 0f;
            for (int pointIndex = 0; pointIndex < query.TrajectoryEnvelope.Count; pointIndex++)
            {
                int featureIndex = range.Offset + pointIndex * 2;
                Vector2 candidate = new Vector2(
                    m_Database.DenormalizeFeature(featureIndex, m_Database.GetNormalizedFeature(sampleIndex, featureIndex)),
                    m_Database.DenormalizeFeature(featureIndex + 1, m_Database.GetNormalizedFeature(sampleIndex, featureIndex + 1)));
                MotionMatchingTrajectoryEnvelopePoint queryPoint = query.TrajectoryEnvelope[pointIndex];
                float outside = Mathf.Max(0f, Vector2.Distance(candidate, queryPoint.LocalPositionCenter) - queryPoint.PositionToleranceRadius);
                float denseWeight = 0.5f * (m_Database.CostProfile.GetDenseFeatureWeight(featureIndex) + m_Database.CostProfile.GetDenseFeatureWeight(featureIndex + 1));
                cost += outside * outside * denseWeight * queryPoint.Confidence;
            }
            return cost * m_Database.CostProfile.GetGroupWeight(MotionMatchingCostGroup.TrajectoryPosition);
        }

        float EvaluateTrajectoryFacing(MotionMatchingQuery query, int sampleIndex, MotionMatchingFeatureRange range)
        {
            if (range.Count != query.TrajectoryEnvelope.Count * 2)
                throw new InvalidOperationException("Trajectory Facing feature range does not match the query envelope.");
            float cost = 0f;
            for (int pointIndex = 0; pointIndex < query.TrajectoryEnvelope.Count; pointIndex++)
            {
                int featureIndex = range.Offset + pointIndex * 2;
                Vector2 candidate = new Vector2(
                    m_Database.DenormalizeFeature(featureIndex, m_Database.GetNormalizedFeature(sampleIndex, featureIndex)),
                    m_Database.DenormalizeFeature(featureIndex + 1, m_Database.GetNormalizedFeature(sampleIndex, featureIndex + 1)));
                if (candidate.sqrMagnitude <= 0f)
                    throw new InvalidOperationException("Motion Matching candidate facing feature is zero.");
                MotionMatchingTrajectoryEnvelopePoint queryPoint = query.TrajectoryEnvelope[pointIndex];
                float outside = Mathf.Max(0f, Mathf.Abs(Vector2.SignedAngle(queryPoint.LocalFacingCenter, candidate.normalized)) - queryPoint.FacingToleranceDegrees);
                float denseWeight = 0.5f * (m_Database.CostProfile.GetDenseFeatureWeight(featureIndex) + m_Database.CostProfile.GetDenseFeatureWeight(featureIndex + 1));
                cost += outside * outside * denseWeight * queryPoint.Confidence;
            }
            return cost * m_Database.CostProfile.GetGroupWeight(MotionMatchingCostGroup.TrajectoryFacing);
        }

        float EvaluateSquaredDifference(MotionMatchingQuery query, int sampleIndex, MotionMatchingFeatureRange range)
        {
            float cost = 0f;
            for (int offset = 0; offset < range.Count; offset++)
            {
                int featureIndex = range.Offset + offset;
                if (!m_Database.IsFeatureActive(featureIndex))
                    continue;
                float difference = m_Database.GetNormalizedFeature(sampleIndex, featureIndex) - query.NormalizedFeatures[featureIndex];
                cost += difference * difference * m_Database.CostProfile.GetDenseFeatureWeight(featureIndex);
            }
            return cost * m_Database.CostProfile.GetGroupWeight(range.Group);
        }
    }
}
