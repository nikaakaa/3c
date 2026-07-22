using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public readonly struct MotionMatchingPlanCostComponents
    {
        public MotionMatchingPlanCostComponents(
            float trajectoryPosition,
            float trajectoryFacing,
            float contact,
            float segmentEnd,
            float velocityChange)
        {
            if (!Valid(trajectoryPosition) || !Valid(trajectoryFacing) || !Valid(contact) || !Valid(segmentEnd) || !Valid(velocityChange))
                throw new ArgumentException("Motion Matching Plan cost contains an invalid component.");
            TrajectoryPosition = trajectoryPosition;
            TrajectoryFacing = trajectoryFacing;
            Contact = contact;
            SegmentEnd = segmentEnd;
            VelocityChange = velocityChange;
        }

        public float TrajectoryPosition { get; }
        public float TrajectoryFacing { get; }
        public float Contact { get; }
        public float SegmentEnd { get; }
        public float VelocityChange { get; }
        public float Total => TrajectoryPosition + TrajectoryFacing + Contact + SegmentEnd + VelocityChange;
        static bool Valid(float value) => float.IsFinite(value) && value >= 0f;
    }

    public readonly struct MotionMatchingSelectionPlan
    {
        public MotionMatchingSelectionPlan(
            CharacterMotionMatchingDatabaseArtifactIdentity databaseIdentity,
            CharacterMotionMatchingPlanId planId,
            int entrySampleIndex,
            CharacterMotionMatchingSampleId entrySampleId,
            CharacterMotionMatchingSegmentId segmentId,
            float entryTime,
            int horizonEndSampleIndex,
            CharacterMotionMatchingSampleId horizonEndSampleId,
            MotionMatchingExactCostComponents exactEntryCost,
            MotionMatchingPlanCostComponents horizonCost,
            bool continueCurrent,
            float entryVisualAdvanceRate,
            float nextMandatorySearchTime)
        {
            if (databaseIdentity == null || !planId.IsValid || entrySampleIndex < 0 || !entrySampleId.IsValid || !segmentId.IsValid ||
                !float.IsFinite(entryTime) || entryTime < 0f || horizonEndSampleIndex < 0 || !horizonEndSampleId.IsValid ||
                !float.IsFinite(entryVisualAdvanceRate) || entryVisualAdvanceRate < 0f ||
                !float.IsFinite(nextMandatorySearchTime) || nextMandatorySearchTime <= 0f)
                throw new ArgumentException("Motion Matching Selection Plan is invalid.");
            DatabaseIdentity = databaseIdentity;
            PlanId = planId;
            EntrySampleIndex = entrySampleIndex;
            EntrySampleId = entrySampleId;
            SegmentId = segmentId;
            EntryTime = entryTime;
            HorizonEndSampleIndex = horizonEndSampleIndex;
            HorizonEndSampleId = horizonEndSampleId;
            ExactEntryCost = exactEntryCost;
            HorizonCost = horizonCost;
            ContinueCurrent = continueCurrent;
            EntryVisualAdvanceRate = entryVisualAdvanceRate;
            NextMandatorySearchTime = nextMandatorySearchTime;
        }

        public CharacterMotionMatchingDatabaseArtifactIdentity DatabaseIdentity { get; }
        public CharacterMotionMatchingPlanId PlanId { get; }
        public int EntrySampleIndex { get; }
        public CharacterMotionMatchingSampleId EntrySampleId { get; }
        public CharacterMotionMatchingSegmentId SegmentId { get; }
        public float EntryTime { get; }
        public int HorizonEndSampleIndex { get; }
        public CharacterMotionMatchingSampleId HorizonEndSampleId { get; }
        public MotionMatchingExactCostComponents ExactEntryCost { get; }
        public MotionMatchingPlanCostComponents HorizonCost { get; }
        public float TotalCost => ExactEntryCost.Total + HorizonCost.Total;
        public bool ContinueCurrent { get; }
        public float EntryVisualAdvanceRate { get; }
        public float NextMandatorySearchTime { get; }
        public bool IsValid => DatabaseIdentity != null && PlanId.IsValid && EntrySampleId.IsValid && HorizonEndSampleId.IsValid;
    }

    public readonly struct MotionMatchingPlanEvaluationResult
    {
        public MotionMatchingPlanEvaluationResult(MotionMatchingSelectionPlan plan, int validPlanCount)
        {
            if (!plan.IsValid || validPlanCount <= 0)
                throw new ArgumentException("Motion Matching Plan evaluation result is invalid.");
            Plan = plan;
            ValidPlanCount = validPlanCount;
            InvalidReason = MotionMatchingInvalidReason.None;
        }

        public MotionMatchingPlanEvaluationResult(MotionMatchingInvalidReason invalidReason)
        {
            if (invalidReason == MotionMatchingInvalidReason.None)
                throw new ArgumentException("Invalid Motion Matching Plan result requires a reason.", nameof(invalidReason));
            Plan = default;
            ValidPlanCount = 0;
            InvalidReason = invalidReason;
        }

        public MotionMatchingSelectionPlan Plan { get; }
        public int ValidPlanCount { get; }
        public MotionMatchingInvalidReason InvalidReason { get; }
        public bool IsValid => InvalidReason == MotionMatchingInvalidReason.None && Plan.IsValid;
    }

    public sealed class MotionMatchingPlanEvaluator
    {
        readonly CharacterMotionMatchingRuntimeDatabase m_Database;

        public MotionMatchingPlanEvaluator(CharacterMotionMatchingRuntimeDatabase database)
        {
            m_Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public MotionMatchingPlanEvaluationResult Evaluate(MotionMatchingQuery query, MotionMatchingSearchResult search)
        {
            if (!search.IsValid)
                return new MotionMatchingPlanEvaluationResult(MotionMatchingInvalidReason.NoAdmittedCandidate);
            MotionMatchingSelectionPlan winner = default;
            int validCount = 0;
            for (int candidateIndex = 0; candidateIndex < search.TopKCount; candidateIndex++)
            {
                MotionMatchingExactCandidate candidate = search.GetCandidate(candidateIndex);
                if (!TryBuildPlan(query, candidate, candidateIndex, out MotionMatchingSelectionPlan plan))
                    continue;
                validCount++;
                if (!winner.IsValid || Compare(plan, winner) < 0)
                    winner = plan;
            }
            return winner.IsValid
                ? new MotionMatchingPlanEvaluationResult(winner, validCount)
                : new MotionMatchingPlanEvaluationResult(MotionMatchingInvalidReason.NoValidPlan);
        }

        bool TryBuildPlan(
            MotionMatchingQuery query,
            MotionMatchingExactCandidate candidate,
            int candidateIndex,
            out MotionMatchingSelectionPlan plan)
        {
            int[] workspace = m_Database.PlanSamples;
            int workspaceOffset = candidateIndex * m_Database.SearchPolicy.PlanSampleCount;
            for (int i = 0; i < m_Database.SearchPolicy.PlanSampleCount; i++)
                workspace[workspaceOffset + i] = -1;
            int sampleIndex = candidate.SampleIndex;
            int horizonEndSampleIndex = sampleIndex;
            Vector2 accumulatedPosition = Vector2.zero;
            float accumulatedFacingDegrees = 0f;
            float trajectoryPosition = 0f;
            float trajectoryFacing = 0f;
            float contact = 0f;
            float segmentEnd = 0f;
            float velocityChange = 0f;
            int evaluatedSampleCount = 0;
            MotionMatchingSamplePayload previous = default;
            bool hasPrevious = false;

            for (int step = 0; step < m_Database.SearchPolicy.PlanSampleCount; step++)
            {
                if ((uint)sampleIndex >= (uint)m_Database.SampleCount)
                {
                    plan = default;
                    return false;
                }
                workspace[workspaceOffset + step] = sampleIndex;
                MotionMatchingSamplePayload sample = m_Database.GetSample(sampleIndex);
                evaluatedSampleCount++;
                horizonEndSampleIndex = sampleIndex;
                float time = step * m_Database.SearchPolicy.PlanSampleInterval;
                if (step > 0)
                {
                    accumulatedPosition += sample.RootPlanarVelocity * m_Database.SearchPolicy.PlanSampleInterval;
                    accumulatedFacingDegrees += sample.RootYawVelocityDegrees * m_Database.SearchPolicy.PlanSampleInterval;
                }
                MotionMatchingTrajectoryEnvelopePoint target = FindEnvelopePoint(query.TrajectoryEnvelope, time);
                float positionOutside = Mathf.Max(0f, Vector2.Distance(accumulatedPosition, target.LocalPositionCenter) - target.PositionToleranceRadius);
                trajectoryPosition += positionOutside * positionOutside * target.Confidence *
                    m_Database.CostProfile.GetGroupWeight(MotionMatchingCostGroup.PlanTrajectoryPosition);
                Vector2 facing = Rotate(Vector2.up, accumulatedFacingDegrees);
                float facingOutside = Mathf.Max(0f, Mathf.Abs(Vector2.SignedAngle(target.LocalFacingCenter, facing)) - target.FacingToleranceDegrees);
                trajectoryFacing += facingOutside * facingOutside * target.Confidence *
                    m_Database.CostProfile.GetGroupWeight(MotionMatchingCostGroup.PlanTrajectoryFacing);
                MotionMatchingFootContactMask missingProtected = query.ContactProtection.ProtectedMask & ~sample.ContactMask;
                if ((missingProtected & MotionMatchingFootContactMask.Left) != 0)
                    contact += m_Database.CostProfile.GetGroupWeight(MotionMatchingCostGroup.PlanContact);
                if ((missingProtected & MotionMatchingFootContactMask.Right) != 0)
                    contact += m_Database.CostProfile.GetGroupWeight(MotionMatchingCostGroup.PlanContact);
                if (hasPrevious)
                {
                    velocityChange += (sample.RootPlanarVelocity - previous.RootPlanarVelocity).sqrMagnitude *
                        m_Database.CostProfile.GetGroupWeight(MotionMatchingCostGroup.PlanVelocityChange);
                    float yawDelta = sample.RootYawVelocityDegrees - previous.RootYawVelocityDegrees;
                    velocityChange += yawDelta * yawDelta * m_Database.CostProfile.GetGroupWeight(MotionMatchingCostGroup.PlanVelocityChange);
                    if (!sample.SegmentId.Equals(previous.SegmentId))
                        segmentEnd += m_Database.CostProfile.GetGroupWeight(MotionMatchingCostGroup.PlanSegmentEnd);
                }
                previous = sample;
                hasPrevious = true;
                if (step == m_Database.SearchPolicy.PlanSampleCount - 1)
                    break;
                if (sample.NextSampleIndex < 0)
                {
                    if (!sample.Terminal)
                    {
                        plan = default;
                        return false;
                    }
                    break;
                }
                sampleIndex = sample.NextSampleIndex;
            }
            MotionMatchingSamplePayload entry = m_Database.GetSample(candidate.SampleIndex);
            MotionMatchingSamplePayload end = m_Database.GetSample(horizonEndSampleIndex);
            bool continueCurrent = !query.Initialization && query.CurrentSelectionInDatabase && query.CurrentSampleIndex >= 0 &&
                m_Database.GetSample(query.CurrentSampleIndex).NextSampleIndex == candidate.SampleIndex;
            plan = new MotionMatchingSelectionPlan(
                m_Database.ArtifactIdentity,
                new CharacterMotionMatchingPlanId(query.QueryId.Value),
                candidate.SampleIndex,
                entry.SampleId,
                entry.SegmentId,
                entry.SampleTime,
                horizonEndSampleIndex,
                end.SampleId,
                candidate.Cost,
                new MotionMatchingPlanCostComponents(trajectoryPosition, trajectoryFacing, contact, segmentEnd, velocityChange),
                continueCurrent,
                ResolveVisualAdvanceRate(candidate.SampleIndex),
                ResolveNextMandatorySearchTime(evaluatedSampleCount, end));
            return true;
        }

        float ResolveNextMandatorySearchTime(int evaluatedSampleCount, MotionMatchingSamplePayload horizonEnd)
        {
            if (evaluatedSampleCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(evaluatedSampleCount));
            bool terminalBoundary = horizonEnd.Terminal && horizonEnd.NextSampleIndex < 0;
            int safeSampleIntervals = terminalBoundary
                ? evaluatedSampleCount
                : Math.Max(1, evaluatedSampleCount - 1);
            float value = safeSampleIntervals / m_Database.SampleRate;
            if (!float.IsFinite(value) || value <= 0f)
                throw new InvalidOperationException("Motion Matching plan produced an invalid mandatory search time.");
            return value;
        }

        float ResolveVisualAdvanceRate(int sampleIndex)
        {
            MotionMatchingSamplePayload sample = m_Database.GetSample(sampleIndex);
            if (sample.NextSampleIndex >= 0)
            {
                MotionMatchingSamplePayload next = m_Database.GetSample(sample.NextSampleIndex);
                if (next.SegmentId.Equals(sample.SegmentId) && next.SampleTime > sample.SampleTime)
                    return (next.SampleTime - sample.SampleTime) * m_Database.SampleRate;
            }
            MotionMatchingSegmentPayload segment = FindSegment(sample.SegmentId);
            return Mathf.Max(0f, (segment.EndTime - sample.SampleTime) * m_Database.SampleRate);
        }

        MotionMatchingSegmentPayload FindSegment(CharacterMotionMatchingSegmentId segmentId)
        {
            for (int i = 0; ; i++)
            {
                MotionMatchingSegmentPayload segment;
                try
                {
                    segment = m_Database.GetSegment(i);
                }
                catch (ArgumentOutOfRangeException)
                {
                    break;
                }
                if (segment.SegmentId.Equals(segmentId))
                    return segment;
            }
            throw new InvalidOperationException($"Motion Matching sample references missing Segment '{segmentId}'.");
        }

        static MotionMatchingTrajectoryEnvelopePoint FindEnvelopePoint(MotionMatchingTrajectoryEnvelope envelope, float time)
        {
            int best = 0;
            float bestDistance = Mathf.Abs(envelope[0].TimeOffset - time);
            for (int i = 1; i < envelope.Count; i++)
            {
                float distance = Mathf.Abs(envelope[i].TimeOffset - time);
                if (distance >= bestDistance)
                    continue;
                best = i;
                bestDistance = distance;
            }
            return envelope[best];
        }

        static Vector2 Rotate(Vector2 value, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos).normalized;
        }

        internal static int Compare(MotionMatchingSelectionPlan left, MotionMatchingSelectionPlan right)
        {
            int total = left.TotalCost.CompareTo(right.TotalCost);
            if (total != 0)
                return total;
            int database = left.DatabaseIdentity.DatabaseId.CompareTo(right.DatabaseIdentity.DatabaseId);
            return database != 0 ? database : left.EntrySampleId.CompareTo(right.EntrySampleId);
        }
    }
}
