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
            SelectionIdentity = new MotionMatchingSelectionIdentity(
                plan.DatabaseIdentity,
                generation,
                plan.PlanId,
                plan.EntrySampleId,
                sampleIndex);
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
            SelectionIdentity = default;
        }

        public MotionMatchingSelectionDecisionKind Kind { get; }
        public MotionMatchingSelectionGeneration Generation { get; }
        public MotionMatchingSelectionPlan Plan { get; }
        public int SampleIndex { get; }
        public MotionMatchingPoseTimePlan PoseTime { get; }
        public MotionMatchingSearchTriggerReason TriggerReason { get; }
        public MotionMatchingInvalidReason InvalidReason { get; }
        public MotionMatchingSelectionIdentity SelectionIdentity { get; }
        public bool IsValid => Kind != MotionMatchingSelectionDecisionKind.Invalid && Generation.IsValid && Plan.IsValid && SampleIndex >= 0 && PoseTime.IsValid;
    }

    public sealed class CharacterMotionMatchingSelectionRuntime
    {
        readonly struct FramePage
        {
            internal FramePage(
                MotionMatchingSelectionGeneration generation,
                MotionMatchingSelectionPlan currentPlan,
                int currentSampleIndex,
                int planCursor,
                float sampleAccumulator,
                float searchAccumulator,
                float secondsSinceLastJump,
                int loopCycle,
                ulong resetSequence,
                bool domainActive,
                MotionMatchingSearchResult lastSearchResult,
                MotionMatchingPlanEvaluationResult lastPlanResult,
                MotionMatchingSelectionDecision lastDecision)
            {
                Generation = generation;
                CurrentPlan = currentPlan;
                CurrentSampleIndex = currentSampleIndex;
                PlanCursor = planCursor;
                SampleAccumulator = sampleAccumulator;
                SearchAccumulator = searchAccumulator;
                SecondsSinceLastJump = secondsSinceLastJump;
                LoopCycle = loopCycle;
                ResetSequence = resetSequence;
                DomainActive = domainActive;
                LastSearchResult = lastSearchResult;
                LastPlanResult = lastPlanResult;
                LastDecision = lastDecision;
            }

            internal MotionMatchingSelectionGeneration Generation { get; }
            internal MotionMatchingSelectionPlan CurrentPlan { get; }
            internal int CurrentSampleIndex { get; }
            internal int PlanCursor { get; }
            internal float SampleAccumulator { get; }
            internal float SearchAccumulator { get; }
            internal float SecondsSinceLastJump { get; }
            internal int LoopCycle { get; }
            internal ulong ResetSequence { get; }
            internal bool DomainActive { get; }
            internal MotionMatchingSearchResult LastSearchResult { get; }
            internal MotionMatchingPlanEvaluationResult LastPlanResult { get; }
            internal MotionMatchingSelectionDecision LastDecision { get; }
        }

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
        FramePage m_CommittedPage;
        bool m_FrameOpen;

        public CharacterMotionMatchingSelectionRuntime(CharacterMotionMatchingRuntimeDatabase database)
        {
            m_Database = database ?? throw new ArgumentNullException(nameof(database));
            m_Search = new MotionMatchingExactSearch(database);
            m_PlanEvaluator = new MotionMatchingPlanEvaluator(database);
            m_CommittedPage = ReadPage();
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

        FramePage ReadPage() =>
            new FramePage(
                m_Generation,
                m_CurrentPlan,
                m_CurrentSampleIndex,
                m_PlanCursor,
                m_SampleAccumulator,
                m_SearchAccumulator,
                m_SecondsSinceLastJump,
                m_LoopCycle,
                m_ResetSequence,
                m_DomainActive,
                LastSearchResult,
                LastPlanResult,
                LastDecision);

        void LoadPage(in FramePage state)
        {
            m_Generation = state.Generation;
            m_CurrentPlan = state.CurrentPlan;
            m_CurrentSampleIndex = state.CurrentSampleIndex;
            m_PlanCursor = state.PlanCursor;
            m_SampleAccumulator = state.SampleAccumulator;
            m_SearchAccumulator = state.SearchAccumulator;
            m_SecondsSinceLastJump = state.SecondsSinceLastJump;
            m_LoopCycle = state.LoopCycle;
            m_ResetSequence = state.ResetSequence;
            m_DomainActive = state.DomainActive;
            LastSearchResult = state.LastSearchResult;
            LastPlanResult = state.LastPlanResult;
            LastDecision = state.LastDecision;
        }

        internal void BeginFrame()
        {
            if (m_FrameOpen)
                throw new InvalidOperationException("Motion Matching Selection frame is already open.");
            LoadPage(in m_CommittedPage);
            m_FrameOpen = true;
        }

        internal void CommitFrame()
        {
            RequireOpenFrame();
            m_CommittedPage = ReadPage();
            m_FrameOpen = false;
        }

        internal void DiscardFrame()
        {
            RequireOpenFrame();
            LoadPage(in m_CommittedPage);
            m_FrameOpen = false;
        }

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
            MotionMatchingPlanEvaluationResult evaluated = SearchAndEvaluate(query);
            MotionMatchingSelectionGeneration generation = m_Generation;
            MotionMatchingSelectionDecisionKind kind;
            if (!generation.IsValid || query.Initialization)
            {
                generation = NextGeneration(generation);
                kind = MotionMatchingSelectionDecisionKind.Initialize;
            }
            else if (evaluated.IsValid && evaluated.Plan.ContinueCurrent)
            {
                kind = MotionMatchingSelectionDecisionKind.Continue;
            }
            else
            {
                generation = NextGeneration(generation);
                kind = MotionMatchingSelectionDecisionKind.Jump;
            }
            return CommitSelection(query, triggerReason, evaluated, generation, kind);
        }

        public MotionMatchingPlanEvaluationResult SearchAndEvaluate(MotionMatchingQuery query)
        {
            LastSearchResult = m_Search.Search(query);
            LastPlanResult = m_PlanEvaluator.Evaluate(query, LastSearchResult);
            return LastPlanResult;
        }

        public MotionMatchingSelectionDecision CommitSelection(
            MotionMatchingQuery query,
            MotionMatchingSearchTriggerReason triggerReason,
            MotionMatchingPlanEvaluationResult evaluated,
            MotionMatchingSelectionGeneration generation,
            MotionMatchingSelectionDecisionKind kind)
        {
            if (!m_DomainActive)
                PrepareDomain(query.ResetSequence);
            if (query.ResetSequence != m_ResetSequence)
                throw new InvalidOperationException("Motion Matching Query reset identity does not match Selection Runtime.");
            if (kind == MotionMatchingSelectionDecisionKind.Invalid || !generation.IsValid)
                throw new ArgumentException("Motion Matching committed Selection identity is invalid.");
            LastPlanResult = evaluated;
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
            m_Generation = generation;
            if (kind != MotionMatchingSelectionDecisionKind.Continue)
                m_SecondsSinceLastJump = 0f;
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

        public void PrepareDomain(ulong resetSequence)
        {
            if (m_DomainActive && m_ResetSequence == resetSequence)
                return;
            Reset(resetSequence);
            m_DomainActive = true;
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
            PersistClosedState();
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
            PersistClosedState();
        }

        void PersistClosedState()
        {
            if (!m_FrameOpen)
                m_CommittedPage = ReadPage();
        }

        void RequireOpenFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException("Motion Matching Selection has no open frame.");
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
                if (current.SampleId.Equals(m_CurrentPlan.HorizonEndSampleId))
                {
                    m_PlanCursor = m_Database.SearchPolicy.PlanSampleCount;
                    break;
                }
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
