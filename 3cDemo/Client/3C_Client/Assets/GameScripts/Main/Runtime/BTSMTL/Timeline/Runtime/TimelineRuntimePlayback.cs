using System;
using ThirdPersonSimulation.Fixed;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonSimulation;
using UnityEngine;

namespace BTSMTL.Timeline.Runtime
{
    public sealed class TimelineRuntimePlayback : IDisposable
    {
        readonly List<string> m_ActiveClipIds;
        readonly List<string> m_PendingTreeClipExits;
        readonly List<string> m_ExitedTreeDecisionClips;
        readonly List<string> m_AdvanceInjectedTreeClipExits;
        readonly List<string> m_AdvanceProducedTreeClipExits;
        readonly List<string> m_AdvanceExitedTreeDecisionClips;
        static readonly IComparer<TimelineRuntimeClipBoundary> s_CompareBoundaries =
            TimelineRuntimeClipBoundaryComparer.Instance;
        readonly ReadOnlyCollection<string> m_ActiveClipIdsView;
        TimelineRuntimeEvaluationStorage m_CandidateEvaluation;
        TimelineRuntimeEvaluationStorage m_CommittedEvaluation;
        TimelineRuntimeAdvanceResult m_PendingAdvance;
        ulong m_AdvanceSequence;
        ulong m_StopSequence;
        TimelinePlaybackStopContext m_PendingStopContext;
        bool m_StopPending;
        FixedScalar m_CursorTime;
        int m_Cycle;
        string m_SectionId = string.Empty;
        bool m_InitialBoundaryPending;
        readonly int m_TickRate;
        int m_TimeCarry;

        internal TimelineRuntimePlayback(
            TimelineRuntimePlaybackHandle handle,
            ulong generation,
            TimelineRuntimePreparationResult preparation,
            int tickRate,
            TimelineRuntimeEvaluationStoragePool evaluationStoragePool)
        {
            if (tickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickRate));
            m_TickRate = tickRate;
            Handle = handle;
            Generation = generation;
            Preparation = preparation;
            PlaybackMode = preparation.PlaybackMode;
            Content = preparation.Content;
            int clipCapacity = preparation.Content.Clips.Count;
            m_ActiveClipIds = new List<string>(clipCapacity);
            m_PendingTreeClipExits = new List<string>(clipCapacity);
            m_ExitedTreeDecisionClips = new List<string>(clipCapacity);
            m_AdvanceInjectedTreeClipExits = new List<string>(clipCapacity);
            m_AdvanceProducedTreeClipExits = new List<string>(clipCapacity);
            m_AdvanceExitedTreeDecisionClips = new List<string>(clipCapacity);
            m_ActiveClipIdsView = new ReadOnlyCollection<string>(m_ActiveClipIds);
            m_CandidateEvaluation = evaluationStoragePool.Rent(this);
            m_CommittedEvaluation = evaluationStoragePool.Rent(this);
            State = TimelineRuntimePlaybackState.Disposed;
            ResetForReuse(handle, generation, preparation);
        }

        public TimelineRuntimePlaybackHandle Handle { get; private set; }
        public ulong Generation { get; private set; }
        public string RequestId { get; private set; }
        public TimelineExecutionIdentity ExecutionIdentity { get; private set; }
        public TimelinePlaybackMode PlaybackMode { get; private set; }
        public TimelineRuntimeNumericTarget NumericTarget { get; private set; }
        public TimelineContentUnit Content { get; private set; }
        public TimelineData SourceTimeline { get; private set; }
        internal TimelineRuntimeMotionSampling MotionSampling { get; private set; }
        internal TimelineRuntimePreparationResult Preparation { get; private set; }
        public string ContentRevision => Content.ContentHash;
        public TimelineRuntimePreparedDependencies PreparedDependencies { get; private set; }
        public TimelinePreparedBindings PreparedBindings { get; private set; }
        public TimelineRuntimePlaybackState State { get; private set; }
        public FixedScalar CursorTime => m_CursorTime;
        public int TickRate => m_TickRate;
        internal IReadOnlyList<string> ExitedTreeDecisionClips => m_ExitedTreeDecisionClips;
        internal int TimeCarry => m_TimeCarry;
        public AbilityTimelinePlaybackControl Control { get; private set; } = AbilityTimelinePlaybackControl.Normal;
        public int Cycle => m_Cycle;
        public string SectionId => m_SectionId;
        public IReadOnlyList<string> ActiveClipIds => m_ActiveClipIdsView;
        public bool HasStopContext { get; private set; }
        public TimelinePlaybackStopContext StopContext { get; private set; }

        internal void ResetForReuse(
            TimelineRuntimePlaybackHandle handle,
            ulong generation,
            TimelineRuntimePreparationResult preparation)
        {
            if (State != TimelineRuntimePlaybackState.Disposed)
                throw new InvalidOperationException("Timeline playback reuse requires a disposed playback.");
            Handle = handle;
            Generation = generation;
            Preparation = preparation;
            RequestId = preparation.RequestId;
            ExecutionIdentity = preparation.ExecutionIdentity;
            PlaybackMode = preparation.PlaybackMode;
            NumericTarget = preparation.NumericTarget;
            Content = preparation.Content;
            SourceTimeline = preparation.SourceTimeline;
            MotionSampling = preparation.MotionSampling;
            PreparedDependencies = preparation.PreparedDependencies;
            PreparedBindings = preparation.PreparedBindings;
            m_CandidateEvaluation.Clear();
            m_CommittedEvaluation.Clear();
            m_PendingAdvance = default;
            m_AdvanceSequence = 0;
            m_StopSequence = 0;
            m_PendingStopContext = default;
            m_StopPending = false;
            m_CursorTime = FixedScalar.Zero;
            m_Cycle = 0;
            m_SectionId = string.Empty;
            m_InitialBoundaryPending = false;
            m_TimeCarry = 0;
            Control = AbilityTimelinePlaybackControl.Normal;
            HasStopContext = false;
            StopContext = default;
            m_ActiveClipIds.Clear();
            m_PendingTreeClipExits.Clear();
            m_ExitedTreeDecisionClips.Clear();
            m_AdvanceInjectedTreeClipExits.Clear();
            m_AdvanceProducedTreeClipExits.Clear();
            m_AdvanceExitedTreeDecisionClips.Clear();
            State = TimelineRuntimePlaybackState.Prepared;
        }

        public bool TryResolveTargetIdentity(string bindingId, out string targetIdentity)
        {
            targetIdentity = string.Empty;
            string inputBindingId = bindingId ?? string.Empty;
            if (PreparedBindings.Plan.TryGetTargetBindingId(inputBindingId, out string targetBindingId))
                inputBindingId = targetBindingId;
            if (!PreparedBindings.CallInput.TryGet(inputBindingId, out TimelineBindingValue value) ||
                value.ValueKind != TimelineBindingValueKind.Target ||
                string.IsNullOrEmpty(value.TargetIdentity))
                return false;
            targetIdentity = value.TargetIdentity;
            return true;
        }

        public bool TryResolveClip(string authoringId, out Clip clip)
        {
            for (int trackIndex = 0; trackIndex < SourceTimeline.Tracks.Count; trackIndex++)
            {
                Track track = SourceTimeline.Tracks[trackIndex];
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip candidate = track.Clips[clipIndex];
                    if (candidate != null && string.Equals(candidate.AuthoringId, authoringId, StringComparison.Ordinal))
                    {
                        clip = candidate;
                        return true;
                    }
                }
            }
            clip = null;
            return false;
        }

        public bool Start()
        {
            if (State != TimelineRuntimePlaybackState.Prepared)
                return false;
            State = TimelineRuntimePlaybackState.Running;
            m_InitialBoundaryPending = true;
            RefreshActiveState();
            return true;
        }

        public bool RequestTreeClipExit(string clipAuthoringId)
        {
            if (string.IsNullOrWhiteSpace(clipAuthoringId))
                throw new ArgumentException("TreeClip exit identity is required.", nameof(clipAuthoringId));
            string trimmed = clipAuthoringId.Trim();
            if (m_PendingTreeClipExits.Contains(trimmed) ||
                m_ExitedTreeDecisionClips.Contains(trimmed) ||
                m_AdvanceProducedTreeClipExits.Contains(trimmed))
                return false;
            bool found = false;
            for (int index = 0; index < Content.Clips.Count; index++)
            {
                if (Content.Clips[index].AuthoringId == trimmed)
                {
                    found = Content.Clips[index].ExecutionPolicy.IsLogic &&
                            Content.Clips[index].ExitSource == TimelineClipExitSource.TreeDecision;
                    break;
                }
            }
            if (!found)
                return false;
            if (m_PendingAdvance.IsValid)
                m_AdvanceProducedTreeClipExits.Add(trimmed);
            else
                m_PendingTreeClipExits.Add(trimmed);
            return true;
        }

        public TimelineRuntimeAdvanceResult Advance(TimelineRuntimeAdvanceRequest request)
        {
            if (State != TimelineRuntimePlaybackState.Running)
                throw new InvalidOperationException("Timeline playback must be running before Advance.");
            if (m_PendingAdvance.IsValid || m_StopPending)
                throw new InvalidOperationException("Timeline playback has an uncommitted Advance result.");
            if (request.PreviousTime != m_CursorTime)
                throw new InvalidOperationException("Timeline interval does not begin at the committed cursor.");
            m_CandidateEvaluation.Clear();
            ulong advanceSequence = checked(++m_AdvanceSequence);
            if (request.Control.IsPaused)
            {
                if (request.TargetTime != m_CursorTime || request.TimeCarry != m_TimeCarry)
                    throw new InvalidOperationException("A paused Timeline interval must preserve the committed cursor and time carry.");
                for (int index = 0; index < m_ActiveClipIds.Count; index++)
                    m_CandidateEvaluation.ActiveClipIds.Add(m_ActiveClipIds[index]);
                m_PendingAdvance = new TimelineRuntimeAdvanceResult(this, advanceSequence, Generation, request,
                    m_CursorTime, m_CursorTime, m_Cycle, m_Cycle, m_TimeCarry, m_SectionId,
                    m_CandidateEvaluation.ActiveClipIds.View, m_CandidateEvaluation.Boundaries.View, new TimelineRuntimeEvaluationResult(m_CandidateEvaluation), false);
                return m_PendingAdvance;
            }
            FixedScalar duration = Content.Duration;
            bool loop = PlaybackMode == TimelinePlaybackMode.Loop;
            int nextTimeCarry = request.TimeCarry;
            FixedScalar requestedTime = request.TargetTime;
            FixedScalar nextTime;
            int nextCycle = m_Cycle;
            if (loop && duration > FixedScalar.Zero)
            {
                long cycleDelta = requestedTime.Raw / duration.Raw;
                nextCycle = checked(nextCycle + (int)cycleDelta);
                nextTime = FixedScalar.FromRaw(requestedTime.Raw % duration.Raw);
            }
            else if (PlaybackMode == TimelinePlaybackMode.HoldLastFrame)
            {
                FixedScalar lastSample = FixedScalar.FromRaw(Math.Max(0L, duration.Raw - 1L));
                nextTime = FixedScalar.Min(requestedTime, lastSample);
                if (nextTime == lastSample)
                    nextTimeCarry = 0;
            }
            else
                nextTime = FixedScalar.Min(requestedTime, duration);

            TimelineRuntimeSampleBuffer<string> activeClipIds = m_CandidateEvaluation.ActiveClipIds;
            for (int index = 0; index < Content.Clips.Count; index++)
            {
                TimelineContentClip clip = Content.Clips[index];
                if (clip.TrackMuted || !clip.ExecutionPolicy.IsLogic)
                    continue;
                if (clip.ExitSource == TimelineClipExitSource.TreeDecision)
                {
                    if (!m_ExitedTreeDecisionClips.Contains(clip.AuthoringId) &&
                        !m_PendingTreeClipExits.Contains(clip.AuthoringId) &&
                        clip.StartTime <= nextTime)
                        activeClipIds.Add(clip.AuthoringId);
                    continue;
                }
                if (clip.StartTime <= nextTime && nextTime < clip.EndTime)
                    activeClipIds.Add(clip.AuthoringId);
            }
            string sectionId = string.Empty;
            for (int index = 0; index < Content.Sections.Count; index++)
            {
                TimelineContentSection section = Content.Sections[index];
                if (section.Time > nextTime)
                    break;
                sectionId = section.AuthoringId;
            }
            TimelineRuntimeSampleBuffer<TimelineRuntimeClipBoundary> boundaries = CollectBoundaries(
                m_CursorTime,
                m_Cycle,
                nextTime,
                nextCycle,
                duration,
                loop,
                m_InitialBoundaryPending,
                m_ExitedTreeDecisionClips,
                m_PendingTreeClipExits);
            m_AdvanceInjectedTreeClipExits.Clear();
            m_AdvanceInjectedTreeClipExits.AddRange(m_PendingTreeClipExits);
            List<string> evaluationExitedTreeDecisionClips = m_AdvanceExitedTreeDecisionClips;
            evaluationExitedTreeDecisionClips.Clear();
            evaluationExitedTreeDecisionClips.AddRange(m_ExitedTreeDecisionClips);
            for (int index = 0; index < m_PendingTreeClipExits.Count; index++)
            {
                if (!evaluationExitedTreeDecisionClips.Contains(m_PendingTreeClipExits[index]))
                    evaluationExitedTreeDecisionClips.Add(m_PendingTreeClipExits[index]);
            }
            TimelineRuntimeEvaluationResult evaluation = TimelineRuntimeEvaluator.Evaluate(
                m_CandidateEvaluation,
                SourceTimeline,
                MotionSampling,
                Content,
                m_CursorTime,
                m_Cycle,
                nextTime,
                nextCycle,
                loop,
                boundaries,
                ExecutionIdentity,
                Generation,
                request.LogicTick,
                m_InitialBoundaryPending,
                evaluationExitedTreeDecisionClips);
            bool hasUnexitedTreeDecisionClip = false;
            for (int index = 0; index < activeClipIds.Count; index++)
            {
                for (int clipIndex = 0; clipIndex < Content.Clips.Count; clipIndex++)
                {
                    TimelineContentClip clip = Content.Clips[clipIndex];
                    if (!string.Equals(clip.AuthoringId, activeClipIds[index], StringComparison.Ordinal) ||
                        clip.ExitSource != TimelineClipExitSource.TreeDecision)
                        continue;
                    hasUnexitedTreeDecisionClip = true;
                    break;
                }
                if (hasUnexitedTreeDecisionClip)
                    break;
            }
            bool completes = PlaybackMode == TimelinePlaybackMode.Once && nextTime >= duration && !hasUnexitedTreeDecisionClip;
            m_PendingAdvance = new TimelineRuntimeAdvanceResult(
                this,
                advanceSequence,
                Generation,
                request,
                m_CursorTime,
                nextTime,
                m_Cycle,
                nextCycle,
                nextTimeCarry,
                sectionId,
                activeClipIds.View,
                boundaries.View,
                evaluation,
                completes);
            return m_PendingAdvance;
        }

        public bool Commit(TimelineRuntimeAdvanceResult advance)
        {
            RequirePendingAdvance(advance);
            TimelineRuntimeEvaluationStorage previous = m_CommittedEvaluation;
            m_CommittedEvaluation = m_CandidateEvaluation;
            m_CandidateEvaluation = previous;
            m_CandidateEvaluation.Clear();
            m_CursorTime = advance.Time;
            m_Cycle = advance.Cycle;
            m_TimeCarry = advance.TimeCarry;
            Control = advance.Request.Control;
            m_SectionId = advance.SectionId;
            if (!advance.Request.Control.IsPaused)
                m_InitialBoundaryPending = false;
            m_ActiveClipIds.Clear();
            for (int index = 0; index < advance.ActiveClipIds.Count; index++)
                m_ActiveClipIds.Add(advance.ActiveClipIds[index]);
            for (int index = 0; index < m_AdvanceInjectedTreeClipExits.Count; index++)
            {
                string exitedClipId = m_AdvanceInjectedTreeClipExits[index];
                m_PendingTreeClipExits.Remove(exitedClipId);
                if (!m_ExitedTreeDecisionClips.Contains(exitedClipId))
                    m_ExitedTreeDecisionClips.Add(exitedClipId);
            }
            m_PendingAdvance = default;
            for (int index = 0; index < m_AdvanceProducedTreeClipExits.Count; index++)
                m_PendingTreeClipExits.Add(m_AdvanceProducedTreeClipExits[index]);
            m_AdvanceProducedTreeClipExits.Clear();
            if (advance.Completes)
                State = TimelineRuntimePlaybackState.Completed;
            return true;
        }

        public bool Discard(TimelineRuntimeAdvanceResult advance)
        {
            RequirePendingAdvance(advance);
            m_AdvanceInjectedTreeClipExits.Clear();
            m_AdvanceProducedTreeClipExits.Clear();
            m_PendingAdvance = default;
            return true;
        }

        public bool RequestStop(TimelinePlaybackStopContext context)
        {
            if (State == TimelineRuntimePlaybackState.Disposed ||
                State == TimelineRuntimePlaybackState.Completed ||
                State == TimelineRuntimePlaybackState.Stopped ||
                State == TimelineRuntimePlaybackState.Failed)
                return false;
            if (m_StopPending)
                return false;
            m_StopSequence = checked(m_StopSequence + 1);
            m_PendingStopContext = context;
            m_StopPending = true;
            return true;
        }

        public bool CommitStop()
        {
            if (!m_StopPending || m_PendingAdvance.IsValid)
                return false;
            StopContext = m_PendingStopContext;
            HasStopContext = true;
            m_StopPending = false;
            State = TimelineRuntimePlaybackState.Stopping;
            m_ActiveClipIds.Clear();
            return true;
        }

        public bool DiscardStop()
        {
            if (!m_StopPending)
                return false;
            m_StopPending = false;
            return true;
        }

        public bool CompleteStop()
        {
            if (State != TimelineRuntimePlaybackState.Stopping)
                return false;
            m_ActiveClipIds.Clear();
            m_CandidateEvaluation.Clear();
            m_CommittedEvaluation.Clear();
            State = TimelineRuntimePlaybackState.Stopped;
            return true;
        }

        public void Dispose()
        {
            if (State != TimelineRuntimePlaybackState.Disposed)
            {
                m_CandidateEvaluation.Clear();
                m_CommittedEvaluation.Clear();
                m_PendingAdvance = default;
                m_StopPending = false;
                m_ActiveClipIds.Clear();
                m_CandidateEvaluation.Recycle();
                m_CommittedEvaluation.Recycle();
                State = TimelineRuntimePlaybackState.Disposed;
            }
        }

        internal void DiscardEvaluation()
        {
            m_CandidateEvaluation.Clear();
            m_CommittedEvaluation.Clear();
        }

        internal bool HasPendingStop => m_StopPending;
        internal ulong PendingStopSequence => m_StopPending ? m_StopSequence : 0;
        internal bool HasPendingAdvance => m_PendingAdvance.IsValid;
        internal bool InitialBoundaryPending => m_InitialBoundaryPending;
        internal IReadOnlyList<string> PendingTreeDecisionClips => m_PendingTreeClipExits;

        internal bool RestoreCommittedState(
            TimelineRuntimePlaybackState state,
            FixedScalar cursorTime,
            int cycle,
            int timeCarry,
            AbilityTimelinePlaybackControl control,
            string sectionId,
            ReadOnlySpan<string> activeClipIds,
            ReadOnlySpan<string> exitedTreeDecisionClips,
            ReadOnlySpan<string> pendingTreeDecisionClips,
            bool hasStopContext,
            TimelinePlaybackStopContext stopContext,
            bool initialBoundaryPending)
        {
            if (m_PendingAdvance.IsValid || m_StopPending)
                return false;
            if (cursorTime < FixedScalar.Zero || cycle < 0 || cursorTime > Content.Duration)
                return false;
            if (Math.Abs((long)timeCarry) * 2 > m_TickRate)
                return false;
            if (state != TimelineRuntimePlaybackState.Prepared &&
                state != TimelineRuntimePlaybackState.Running &&
                state != TimelineRuntimePlaybackState.Stopping &&
                state != TimelineRuntimePlaybackState.Completed &&
                state != TimelineRuntimePlaybackState.Stopped)
                return false;
            bool requiresStopContext = state == TimelineRuntimePlaybackState.Stopping ||
                                       state == TimelineRuntimePlaybackState.Stopped;
            if (requiresStopContext != hasStopContext)
                return false;
            if (!ValidateRestoredSection(sectionId, cursorTime) ||
                !ValidateRestoredActiveClips(
                    activeClipIds,
                    state,
                    cursorTime,
                    exitedTreeDecisionClips))
                return false;
            m_CandidateEvaluation.Clear();
            m_CommittedEvaluation.Clear();
            m_CursorTime = cursorTime;
            m_Cycle = cycle;
            m_SectionId = sectionId ?? string.Empty;
            m_InitialBoundaryPending = initialBoundaryPending;
            m_TimeCarry = timeCarry;
            Control = control;
            m_ActiveClipIds.Clear();
            for (int index = 0; index < activeClipIds.Length; index++)
                m_ActiveClipIds.Add(activeClipIds[index]);
            m_ExitedTreeDecisionClips.Clear();
            for (int index = 0; index < exitedTreeDecisionClips.Length; index++)
                m_ExitedTreeDecisionClips.Add(exitedTreeDecisionClips[index]);
            m_PendingTreeClipExits.Clear();
            for (int index = 0; index < pendingTreeDecisionClips.Length; index++)
                m_PendingTreeClipExits.Add(pendingTreeDecisionClips[index]);
            HasStopContext = hasStopContext;
            StopContext = stopContext;
            State = state;
            return true;
        }

        bool ValidateRestoredSection(string sectionId, FixedScalar cursorTime)
        {
            if (string.IsNullOrEmpty(sectionId))
                return true;
            for (int index = 0; index < Content.Sections.Count; index++)
            {
                TimelineContentSection section = Content.Sections[index];
                if (string.Equals(section.AuthoringId, sectionId, StringComparison.Ordinal))
                    return section.Time <= cursorTime;
            }
            return false;
        }

        bool ValidateRestoredActiveClips(
            ReadOnlySpan<string> activeClipIds,
            TimelineRuntimePlaybackState state,
            FixedScalar cursorTime,
            ReadOnlySpan<string> exitedTreeDecisionClips)
        {
            int suppliedCount = activeClipIds.Length;
            if (state != TimelineRuntimePlaybackState.Running)
                return suppliedCount == 0;
            int expectedCount = 0;
            for (int clipIndex = 0; clipIndex < Content.Clips.Count; clipIndex++)
            {
                TimelineContentClip clip = Content.Clips[clipIndex];
                bool active = !clip.TrackMuted && clip.ExecutionPolicy.IsLogic && clip.StartTime <= cursorTime &&
                    (clip.ExitSource == TimelineClipExitSource.TreeDecision
                        ? !ContainsClip(exitedTreeDecisionClips, clip.AuthoringId)
                        : cursorTime < clip.EndTime);
                if (!active)
                    continue;
                expectedCount++;
                int occurrences = 0;
                for (int index = 0; index < suppliedCount; index++)
                    if (string.Equals(activeClipIds[index], clip.AuthoringId, StringComparison.Ordinal))
                        occurrences++;
                if (occurrences != 1)
                    return false;
            }
            return expectedCount == suppliedCount;
        }

        static bool ContainsClip(ReadOnlySpan<string> clips, string clipId)
        {
            for (int index = 0; index < clips.Length; index++)
                if (string.Equals(clips[index], clipId, StringComparison.Ordinal))
                    return true;
            return false;
        }

        internal static bool ContainsClip(IReadOnlyList<string> clips, string clipId)
        {
            for (int index = 0; index < clips.Count; index++)
                if (string.Equals(clips[index], clipId, StringComparison.Ordinal))
                    return true;
            return false;
        }

        void RequirePendingAdvance(TimelineRuntimeAdvanceResult advance)
        {
            if (!m_PendingAdvance.IsValid || !ReferenceEquals(advance.Owner, this) ||
                advance.AdvanceSequence != m_PendingAdvance.AdvanceSequence)
                throw new InvalidOperationException("Timeline Advance result does not belong to the active playback.");
        }

        void RefreshActiveState()
        {
            m_ActiveClipIds.Clear();
            for (int index = 0; index < Content.Clips.Count; index++)
            {
                TimelineContentClip clip = Content.Clips[index];
                bool treeDecision = clip.ExitSource == TimelineClipExitSource.TreeDecision;
                if (!clip.TrackMuted && clip.ExecutionPolicy.IsLogic &&
                    clip.StartTime <= m_CursorTime &&
                    (treeDecision
                        ? !m_ExitedTreeDecisionClips.Contains(clip.AuthoringId) &&
                          !m_PendingTreeClipExits.Contains(clip.AuthoringId)
                        : m_CursorTime < clip.EndTime))
                    m_ActiveClipIds.Add(clip.AuthoringId);
            }
            m_SectionId = string.Empty;
            for (int index = 0; index < Content.Sections.Count; index++)
            {
                TimelineContentSection section = Content.Sections[index];
                if (section.Time > m_CursorTime)
                    break;
                m_SectionId = section.AuthoringId;
            }
        }

        TimelineRuntimeSampleBuffer<TimelineRuntimeClipBoundary> CollectBoundaries(
            FixedScalar previousTime,
            int previousCycle,
            FixedScalar nextTime,
            int nextCycle,
            FixedScalar duration,
            bool loop,
            bool initialBoundaryPending,
            IReadOnlyList<string> exitedTreeDecisionClips,
            IReadOnlyList<string> pendingTreeClipExits)
        {
            TimelineRuntimeSampleBuffer<TimelineRuntimeClipBoundary> result = m_CandidateEvaluation.Boundaries;
            if (duration <= FixedScalar.Zero)
                return result;
            FixedScalar previousAbsolute = duration * FixedScalar.FromInt64(previousCycle) + previousTime;
            FixedScalar nextAbsolute = duration * FixedScalar.FromInt64(nextCycle) + nextTime;
            if (loop && nextCycle - previousCycle > TimelineRuntimeEvaluationSegments.MaximumCycleAdvance)
                throw new InvalidOperationException("Timeline playback crossed more than 4096 cycles in one Advance.");

            int firstCycle = loop ? previousCycle : 0;
            int lastCycle = loop ? nextCycle : 0;
            for (int clipIndex = 0; clipIndex < Content.Clips.Count; clipIndex++)
            {
                TimelineContentClip clip = Content.Clips[clipIndex];
                if (clip.TrackMuted || !clip.ExecutionPolicy.IsLogic)
                    continue;
                bool treeDecision = clip.ExitSource == TimelineClipExitSource.TreeDecision;
                if (treeDecision && ContainsClip(exitedTreeDecisionClips, clip.AuthoringId))
                    continue;
                if (treeDecision && ContainsClip(pendingTreeClipExits, clip.AuthoringId))
                {
                    result.Add(new TimelineRuntimeClipBoundary(
                        clip.AuthoringId,
                        clip.TrackAuthoringId,
                        nextTime,
                        nextCycle,
                        TimelineRuntimeClipBoundaryKind.Exit));
                    continue;
                }
                if (nextAbsolute <= previousAbsolute)
                    continue;
                for (int cycle = firstCycle; cycle <= lastCycle; cycle++)
                {
                    AddBoundary(
                        result,
                        clip,
                        clip.StartTime,
                        cycle,
                        TimelineRuntimeClipBoundaryKind.Enter,
                        previousAbsolute,
                        nextAbsolute,
                        duration,
                        initialBoundaryPending);
                    if (treeDecision)
                        continue;
                    AddBoundary(
                        result,
                        clip,
                        clip.EndTime,
                        cycle,
                        TimelineRuntimeClipBoundaryKind.Exit,
                        previousAbsolute,
                        nextAbsolute,
                        duration,
                        false);
                }
            }
            result.Sort(s_CompareBoundaries);
            return result;
        }

        sealed class TimelineRuntimeClipBoundaryComparer : IComparer<TimelineRuntimeClipBoundary>
        {
            public static readonly TimelineRuntimeClipBoundaryComparer Instance =
                new TimelineRuntimeClipBoundaryComparer();

            public int Compare(TimelineRuntimeClipBoundary left, TimelineRuntimeClipBoundary right)
            {
                int position = left.Cycle.CompareTo(right.Cycle);
                if (position == 0)
                    position = left.Time.CompareTo(right.Time);
                if (position != 0)
                    return position;
                int kind = left.Kind.CompareTo(right.Kind);
                return kind != 0 ? kind : string.CompareOrdinal(left.AuthoringId, right.AuthoringId);
            }
        }

        internal void ReleaseForReuse()
        {
            if (State == TimelineRuntimePlaybackState.Disposed)
                return;
            m_CandidateEvaluation.Clear();
            m_CommittedEvaluation.Clear();
            m_PendingAdvance = default;
            m_StopPending = false;
            m_ActiveClipIds.Clear();
            m_PendingTreeClipExits.Clear();
            m_ExitedTreeDecisionClips.Clear();
            m_AdvanceInjectedTreeClipExits.Clear();
            m_AdvanceProducedTreeClipExits.Clear();
            m_AdvanceExitedTreeDecisionClips.Clear();
            State = TimelineRuntimePlaybackState.Disposed;
        }

        static void AddBoundary(
            TimelineRuntimeSampleBuffer<TimelineRuntimeClipBoundary> result,
            TimelineContentClip clip,
            FixedScalar time,
            int cycle,
            TimelineRuntimeClipBoundaryKind kind,
            FixedScalar previousAbsolute,
            FixedScalar nextAbsolute,
            FixedScalar duration,
            bool initialBoundaryPending)
        {
            FixedScalar absolute = duration * FixedScalar.FromInt64(cycle) + time;
            bool initialEnter = initialBoundaryPending &&
                                kind == TimelineRuntimeClipBoundaryKind.Enter &&
                                absolute == previousAbsolute;
            if ((!initialEnter && absolute <= previousAbsolute) || absolute > nextAbsolute)
                return;
            result.Add(new TimelineRuntimeClipBoundary(
                clip.AuthoringId,
                clip.TrackAuthoringId,
                time,
                cycle,
                kind));
        }
    }
}
