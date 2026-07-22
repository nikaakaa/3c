using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public readonly struct MotionMatchingPoseTimePlan
    {
        readonly bool m_IsSpecified;

        public MotionMatchingPoseTimePlan(
            float sampleTime,
            double continuousVisualTime,
            int cycle,
            float visualTimeScale,
            bool looping)
        {
            if (!float.IsFinite(sampleTime) || sampleTime < 0f ||
                double.IsNaN(continuousVisualTime) || double.IsInfinity(continuousVisualTime) || continuousVisualTime < sampleTime ||
                cycle < 0 || !float.IsFinite(visualTimeScale) || visualTimeScale < 0f ||
                !looping && (cycle != 0 || continuousVisualTime != sampleTime))
                throw new ArgumentException("Motion Matching Pose Time Plan is invalid.");
            m_IsSpecified = true;
            SampleTime = sampleTime;
            ContinuousVisualTime = continuousVisualTime;
            Cycle = cycle;
            VisualTimeScale = visualTimeScale;
            Looping = looping;
        }

        public float SampleTime { get; }
        public double ContinuousVisualTime { get; }
        public int Cycle { get; }
        public float VisualTimeScale { get; }
        public bool Looping { get; }
        public float AnimatorStateSpeed => 0f;
        public bool IsValid => m_IsSpecified && float.IsFinite(SampleTime) && SampleTime >= 0f &&
                               !double.IsNaN(ContinuousVisualTime) && !double.IsInfinity(ContinuousVisualTime) && ContinuousVisualTime >= SampleTime &&
                               Cycle >= 0 && float.IsFinite(VisualTimeScale) && VisualTimeScale >= 0f &&
                               (Looping || Cycle == 0 && ContinuousVisualTime == SampleTime);
    }

    public readonly struct MotionMatchingSelectionDecision
    {
        public MotionMatchingSelectionDecision(
            MotionMatchingSelectionDecisionKind kind,
            MotionMatchingSelectionGeneration generation,
            MotionMatchingSelectionPlan plan,
            int sampleIndex,
            MotionMatchingPoseTimePlan poseTime,
            MotionMatchingSearchTriggerReason triggerReason)
        {
            if (kind == MotionMatchingSelectionDecisionKind.Invalid || !generation.IsValid || !plan.IsValid || sampleIndex < 0 || !poseTime.IsValid)
                throw new ArgumentException("Valid Motion Matching Selection decision is incomplete.");
            Kind = kind;
            Generation = generation;
            Plan = plan;
            SampleIndex = sampleIndex;
            PoseTime = poseTime;
            TriggerReason = triggerReason;
            InvalidReason = MotionMatchingInvalidReason.None;
        }

        public MotionMatchingSelectionDecision(MotionMatchingInvalidReason invalidReason, MotionMatchingSearchTriggerReason triggerReason)
        {
            if (invalidReason == MotionMatchingInvalidReason.None)
                throw new ArgumentException("Invalid Motion Matching Selection decision requires a reason.", nameof(invalidReason));
            Kind = MotionMatchingSelectionDecisionKind.Invalid;
            Generation = default;
            Plan = default;
            SampleIndex = -1;
            PoseTime = default;
            TriggerReason = triggerReason;
            InvalidReason = invalidReason;
        }

        public MotionMatchingSelectionDecisionKind Kind { get; }
        public MotionMatchingSelectionGeneration Generation { get; }
        public MotionMatchingSelectionPlan Plan { get; }
        public int SampleIndex { get; }
        public MotionMatchingPoseTimePlan PoseTime { get; }
        public MotionMatchingSearchTriggerReason TriggerReason { get; }
        public MotionMatchingInvalidReason InvalidReason { get; }
        public bool IsValid => Kind != MotionMatchingSelectionDecisionKind.Invalid && Generation.IsValid && Plan.IsValid && SampleIndex >= 0 && PoseTime.IsValid;
    }

    public sealed class CharacterMotionMatchingSelectionRuntime
    {
        readonly CharacterMotionMatchingRuntimeDatabase m_Database;
        readonly MotionMatchingExactSearch m_Search;
        readonly MotionMatchingPlanEvaluator m_PlanEvaluator;
        MotionMatchingSelectionGeneration m_Generation;
        MotionMatchingSelectionPlan m_CurrentPlan;
        int m_CurrentSampleIndex = -1;
        int m_PlanCursor;
        float m_SampleAccumulator;
        float m_SearchAccumulator;
        float m_SecondsSinceLastJump;
        int m_LoopCycle;
        ulong m_ResetSequence;
        bool m_DomainActive;

        public CharacterMotionMatchingSelectionRuntime(CharacterMotionMatchingRuntimeDatabase database)
        {
            m_Database = database ?? throw new ArgumentNullException(nameof(database));
            m_Search = new MotionMatchingExactSearch(database);
            m_PlanEvaluator = new MotionMatchingPlanEvaluator(database);
        }

        public bool HasSelection => m_CurrentPlan.IsValid && m_CurrentSampleIndex >= 0 && m_Generation.IsValid;
        public MotionMatchingSelectionGeneration Generation => m_Generation;
        public MotionMatchingSelectionPlan CurrentPlan => m_CurrentPlan;
        public int CurrentSampleIndex => m_CurrentSampleIndex;
        public int PlanCursor => m_PlanCursor;
        public float SecondsSinceLastJump => m_SecondsSinceLastJump;
        public MotionMatchingSearchResult LastSearchResult { get; private set; }
        public MotionMatchingPlanEvaluationResult LastPlanResult { get; private set; }
        public MotionMatchingSelectionDecision LastDecision { get; private set; }

        public bool RequiresSearch(
            float presentationDelta,
            ulong resetSequence,
            bool domainActive,
            out MotionMatchingSearchTriggerReason triggerReason)
        {
            if (!float.IsFinite(presentationDelta) || presentationDelta < 0f)
                throw new ArgumentOutOfRangeException(nameof(presentationDelta));
            if (!domainActive)
            {
                ReleaseDomain();
                triggerReason = default;
                return false;
            }
            if (!m_DomainActive)
            {
                Reset(resetSequence);
                m_DomainActive = true;
                triggerReason = MotionMatchingSearchTriggerReason.DomainActivated;
                return true;
            }
            if (resetSequence != m_ResetSequence)
            {
                Reset(resetSequence);
                m_DomainActive = true;
                triggerReason = MotionMatchingSearchTriggerReason.PresentationReset;
                return true;
            }
            m_SearchAccumulator += presentationDelta;
            m_SecondsSinceLastJump += presentationDelta;
            AdvancePlan(presentationDelta);
            if (!HasSelection)
            {
                triggerReason = MotionMatchingSearchTriggerReason.Initialization;
                return true;
            }
            if (m_PlanCursor >= m_Database.SearchPolicy.PlanSampleCount ||
                m_SearchAccumulator >= m_CurrentPlan.NextMandatorySearchTime)
            {
                triggerReason = MotionMatchingSearchTriggerReason.MandatoryBoundary;
                return true;
            }
            if (m_SearchAccumulator >= m_Database.SearchPolicy.SearchInterval)
            {
                triggerReason = MotionMatchingSearchTriggerReason.Cadence;
                return true;
            }
            triggerReason = default;
            return false;
        }

        public MotionMatchingSelectionDecision SearchAndSelect(MotionMatchingQuery query, MotionMatchingSearchTriggerReason triggerReason)
        {
            if (!m_DomainActive)
                throw new InvalidOperationException("Motion Matching Search cannot run while its Domain is inactive.");
            if (query.ResetSequence != m_ResetSequence)
                throw new InvalidOperationException("Motion Matching Query reset identity does not match Selection Runtime.");
            LastSearchResult = m_Search.Search(query);
            LastPlanResult = m_PlanEvaluator.Evaluate(query, LastSearchResult);
            m_SearchAccumulator = 0f;
            if (!LastPlanResult.IsValid)
            {
                m_CurrentPlan = default;
                m_CurrentSampleIndex = -1;
                m_PlanCursor = 0;
                m_SampleAccumulator = 0f;
                m_LoopCycle = 0;
                LastDecision = new MotionMatchingSelectionDecision(LastPlanResult.InvalidReason, triggerReason);
                return LastDecision;
            }
            MotionMatchingSelectionPlan plan = LastPlanResult.Plan;
            MotionMatchingSelectionDecisionKind kind;
            if (!m_Generation.IsValid)
            {
                m_Generation = NextGeneration(m_Generation);
                kind = MotionMatchingSelectionDecisionKind.Initialize;
                m_SecondsSinceLastJump = 0f;
            }
            else if (triggerReason == MotionMatchingSearchTriggerReason.DomainActivated)
            {
                m_Generation = NextGeneration(m_Generation);
                kind = MotionMatchingSelectionDecisionKind.Jump;
                m_SecondsSinceLastJump = 0f;
            }
            else if (query.Initialization)
            {
                m_Generation = NextGeneration(m_Generation);
                kind = MotionMatchingSelectionDecisionKind.Initialize;
                m_SecondsSinceLastJump = 0f;
            }
            else if (plan.ContinueCurrent)
            {
                kind = MotionMatchingSelectionDecisionKind.Continue;
            }
            else
            {
                m_Generation = NextGeneration(m_Generation);
                kind = MotionMatchingSelectionDecisionKind.Jump;
                m_SecondsSinceLastJump = 0f;
            }
            UpdateCycleForSelection(kind, plan.EntrySampleIndex);
            m_CurrentPlan = plan;
            m_CurrentSampleIndex = plan.EntrySampleIndex;
            m_PlanCursor = 0;
            m_SampleAccumulator = 0f;
            MotionMatchingPoseTimePlan poseTime = BuildPoseTime();
            if (!Mathf.Approximately(poseTime.VisualTimeScale, plan.EntryVisualAdvanceRate))
                throw new InvalidOperationException("Motion Matching Selection Plan visual advance rate does not match its selected sample link.");
            LastDecision = new MotionMatchingSelectionDecision(kind, m_Generation, plan, m_CurrentSampleIndex, poseTime, triggerReason);
            return LastDecision;
        }

        public MotionMatchingSelectionDecision GetContinuationDecision()
        {
            if (!HasSelection)
            {
                m_SampleAccumulator = 0f;
                m_LoopCycle = 0;
                LastDecision = new MotionMatchingSelectionDecision(MotionMatchingInvalidReason.NoValidPlan, MotionMatchingSearchTriggerReason.PlanInvalidated);
                return LastDecision;
            }
            return new MotionMatchingSelectionDecision(
                MotionMatchingSelectionDecisionKind.Continue,
                m_Generation,
                m_CurrentPlan,
                m_CurrentSampleIndex,
                BuildPoseTime(),
                MotionMatchingSearchTriggerReason.Cadence);
        }

        public void Reset(ulong resetSequence)
        {
            m_CurrentPlan = default;
            m_CurrentSampleIndex = -1;
            m_PlanCursor = 0;
            m_SampleAccumulator = 0f;
            m_SearchAccumulator = 0f;
            m_SecondsSinceLastJump = 0f;
            m_LoopCycle = 0;
            m_ResetSequence = resetSequence;
            LastSearchResult = default;
            LastPlanResult = default;
            LastDecision = default;
        }

        public void ReleaseDomain()
        {
            m_CurrentPlan = default;
            m_CurrentSampleIndex = -1;
            m_PlanCursor = 0;
            m_SampleAccumulator = 0f;
            m_SearchAccumulator = 0f;
            m_LoopCycle = 0;
            m_DomainActive = false;
            LastDecision = default;
        }

        void AdvancePlan(float presentationDelta)
        {
            if (!HasSelection || presentationDelta <= 0f)
                return;
            m_SampleAccumulator += presentationDelta;
            float sampleDuration = 1f / m_Database.SampleRate;
            while (m_SampleAccumulator >= sampleDuration && m_PlanCursor < m_Database.SearchPolicy.PlanSampleCount)
            {
                MotionMatchingSamplePayload current = m_Database.GetSample(m_CurrentSampleIndex);
                if (current.NextSampleIndex < 0)
                {
                    m_PlanCursor = m_Database.SearchPolicy.PlanSampleCount;
                    break;
                }
                MotionMatchingSamplePayload next = m_Database.GetSample(current.NextSampleIndex);
                if (next.SegmentId.Equals(current.SegmentId) && next.SampleTime < current.SampleTime)
                    m_LoopCycle = checked(m_LoopCycle + 1);
                else if (!next.SegmentId.Equals(current.SegmentId))
                    m_LoopCycle = 0;
                m_CurrentSampleIndex = current.NextSampleIndex;
                m_PlanCursor++;
                m_SampleAccumulator -= sampleDuration;
            }
        }

        void UpdateCycleForSelection(MotionMatchingSelectionDecisionKind kind, int entrySampleIndex)
        {
            if (kind != MotionMatchingSelectionDecisionKind.Continue || m_CurrentSampleIndex < 0)
            {
                m_LoopCycle = 0;
                return;
            }
            MotionMatchingSamplePayload previous = m_Database.GetSample(m_CurrentSampleIndex);
            MotionMatchingSamplePayload entry = m_Database.GetSample(entrySampleIndex);
            if (entry.SegmentId.Equals(previous.SegmentId) && entry.SampleTime < previous.SampleTime)
                m_LoopCycle = checked(m_LoopCycle + 1);
            else if (!entry.SegmentId.Equals(previous.SegmentId))
                m_LoopCycle = 0;
        }

        MotionMatchingPoseTimePlan BuildPoseTime()
        {
            MotionMatchingSamplePayload sample = m_Database.GetSample(m_CurrentSampleIndex);
            MotionMatchingSegmentPayload segment = FindSegment(sample.SegmentId);
            float visualTimeScale = ResolveVisualAdvanceRate(sample, segment);
            float sampleTime = Mathf.Min(segment.EndTime, sample.SampleTime + m_SampleAccumulator * visualTimeScale);
            bool looping = segment.LoopMode == MotionMatchingSegmentLoopMode.Loop;
            int cycle = looping ? m_LoopCycle : 0;
            double continuous = looping ? sampleTime + cycle * (double)segment.Duration : sampleTime;
            return new MotionMatchingPoseTimePlan(sampleTime, continuous, cycle, visualTimeScale, looping);
        }

        float ResolveVisualAdvanceRate(MotionMatchingSamplePayload sample, MotionMatchingSegmentPayload segment)
        {
            if (sample.NextSampleIndex >= 0)
            {
                MotionMatchingSamplePayload next = m_Database.GetSample(sample.NextSampleIndex);
                if (next.SegmentId.Equals(sample.SegmentId) && next.SampleTime > sample.SampleTime)
                    return (next.SampleTime - sample.SampleTime) * m_Database.SampleRate;
            }
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
            throw new InvalidOperationException($"Motion Matching selected sample references missing Segment '{segmentId}'.");
        }

        static MotionMatchingSelectionGeneration NextGeneration(MotionMatchingSelectionGeneration current) =>
            current.IsValid ? current.Next() : new MotionMatchingSelectionGeneration(1);
    }
}
