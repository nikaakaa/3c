using System;
using ThirdPersonSimulation.Fixed;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonSimulation;
using UnityEngine;

namespace BTSMTL.Timeline.Runtime
{
    public enum TimelineRuntimeNumericTarget : byte
    {
        Float32 = 0,
        Fixed = 1
    }

    public readonly struct TimelineRuntimeDependencyHandle : IEquatable<TimelineRuntimeDependencyHandle>
    {
        public TimelineRuntimeDependencyHandle(int value)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        public int Value { get; }
        public bool IsValid => Value > 0;
        public bool Equals(TimelineRuntimeDependencyHandle other) => Value == other.Value;
        public override bool Equals(object obj) => obj is TimelineRuntimeDependencyHandle other && Equals(other);
        public override int GetHashCode() => Value;
        public static TimelineRuntimeDependencyHandle Invalid => default;
    }

    public interface ITimelineRuntimeDependencyResolver
    {
        bool TryResolve(
            TimelineContentDependency dependency,
            TimelineRuntimeNumericTarget numericTarget,
            out TimelineRuntimeDependencyHandle handle,
            out string error);
    }

    public sealed class TimelineRuntimePreparedDependencies
    {
        readonly Dictionary<string, TimelineRuntimeDependencyHandle> m_Handles;

        internal TimelineRuntimePreparedDependencies(
            IReadOnlyList<TimelineContentDependency> dependencies,
            IReadOnlyList<TimelineRuntimeDependencyHandle> handles)
        {
            m_Handles = new Dictionary<string, TimelineRuntimeDependencyHandle>(StringComparer.Ordinal);
            for (int index = 0; index < dependencies.Count; index++)
                m_Handles.Add(dependencies[index].Identity, handles[index]);
        }

        public bool TryGetHandle(string dependencyIdentity, out TimelineRuntimeDependencyHandle handle)
        {
            if (m_Handles.TryGetValue(dependencyIdentity ?? string.Empty, out handle))
                return handle.IsValid;
            handle = TimelineRuntimeDependencyHandle.Invalid;
            return false;
        }
    }

    public readonly struct TimelineRuntimePlaybackHandle : IEquatable<TimelineRuntimePlaybackHandle>
    {
        public TimelineRuntimePlaybackHandle(ulong value)
        {
            if (value == 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        public ulong Value { get; }
        public bool IsValid => Value != 0;
        public bool Equals(TimelineRuntimePlaybackHandle other) => Value == other.Value;
        public override bool Equals(object obj) => obj is TimelineRuntimePlaybackHandle other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static TimelineRuntimePlaybackHandle Invalid => default;
        public static bool operator ==(TimelineRuntimePlaybackHandle left, TimelineRuntimePlaybackHandle right) => left.Equals(right);
        public static bool operator !=(TimelineRuntimePlaybackHandle left, TimelineRuntimePlaybackHandle right) => !left.Equals(right);
    }

    public readonly struct TimelineRuntimeAdvanceRequest
    {
        public TimelineRuntimeAdvanceRequest(ulong logicTick, FixedScalar previousTime, FixedScalar targetTime, int timeCarry, AbilityTimelinePlaybackControl control)
        {
            if (logicTick == 0)
                throw new ArgumentOutOfRangeException(nameof(logicTick));
            if (previousTime < FixedScalar.Zero || targetTime < previousTime)
                throw new ArgumentOutOfRangeException(nameof(targetTime));
            LogicTick = logicTick;
            PreviousTime = previousTime;
            TargetTime = targetTime;
            TimeCarry = timeCarry;
            Control = control;
        }

        public ulong LogicTick { get; }
        public FixedScalar PreviousTime { get; }
        public FixedScalar TargetTime { get; }
        public int TimeCarry { get; }
        public AbilityTimelinePlaybackControl Control { get; }
    }

    public enum TimelineRuntimeClipBoundaryKind : byte
    {
        Exit = 0,
        Enter = 1
    }

    public readonly struct TimelineRuntimeClipBoundary
    {
        public TimelineRuntimeClipBoundary(
            string authoringId,
            string trackAuthoringId,
            FixedScalar time,
            int cycle,
            TimelineRuntimeClipBoundaryKind kind)
        {
            AuthoringId = string.IsNullOrWhiteSpace(authoringId)
                ? throw new ArgumentException("Timeline Clip identity is required.", nameof(authoringId))
                : authoringId.Trim();
            TrackAuthoringId = string.IsNullOrWhiteSpace(trackAuthoringId)
                ? throw new ArgumentException("Timeline Track identity is required.", nameof(trackAuthoringId))
                : trackAuthoringId.Trim();
            if (time < FixedScalar.Zero)
                throw new ArgumentOutOfRangeException(nameof(time));
            if (cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(cycle));
            if (!Enum.IsDefined(typeof(TimelineRuntimeClipBoundaryKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            Time = time;
            Cycle = cycle;
            Kind = kind;
        }

        public string AuthoringId { get; }
        public string TrackAuthoringId { get; }
        public FixedScalar Time { get; }
        public int Cycle { get; }
        public TimelineRuntimeClipBoundaryKind Kind { get; }
    }

    public sealed class TimelineRuntimeAdvanceResult
    {
        readonly ReadOnlyCollection<string> m_ActiveClipIds;
        readonly ReadOnlyCollection<TimelineRuntimeClipBoundary> m_Boundaries;

        internal TimelineRuntimeAdvanceResult(
            TimelineRuntimePlayback owner,
            ulong generation,
            TimelineRuntimeAdvanceRequest request,
            FixedScalar previousTime,
            FixedScalar time,
            int previousCycle,
            int cycle,
            int timeCarry,
            string sectionId,
            IReadOnlyList<string> activeClipIds,
            IReadOnlyList<TimelineRuntimeClipBoundary> boundaries,
            TimelineRuntimeEvaluationResult evaluation,
            bool completes)
        {
            Owner = owner;
            Generation = generation;
            Request = request;
            PreviousTime = previousTime;
            Time = time;
            PreviousCycle = previousCycle;
            Cycle = cycle;
            TimeCarry = timeCarry;
            SectionId = sectionId ?? string.Empty;
            m_ActiveClipIds = new ReadOnlyCollection<string>(new List<string>(activeClipIds ?? Array.Empty<string>()));
            m_Boundaries = new ReadOnlyCollection<TimelineRuntimeClipBoundary>(
                new List<TimelineRuntimeClipBoundary>(boundaries ?? Array.Empty<TimelineRuntimeClipBoundary>()));
            Evaluation = evaluation;
            Completes = completes;
        }

        internal TimelineRuntimePlayback Owner { get; }
        internal TimelineRuntimeAdvanceRequest Request { get; }
        internal int TimeCarry { get; }
        public ulong Generation { get; }
        public string ContentIdentity => Owner.Content.Identity;
        public string ContentRevision => Owner.Content.ContentHash;
        public FixedScalar Duration => Owner.Content.Duration;
        public TimelinePlaybackMode PlaybackMode => Owner.PlaybackMode;
        public ulong LogicTick => Request.LogicTick;
        public AbilityTimelinePlaybackControl Control => Request.Control;
        public FixedScalar PreviousTime { get; }
        public FixedScalar Time { get; }
        public int PreviousCycle { get; }
        public int Cycle { get; }
        public string SectionId { get; }
        public IReadOnlyList<string> ActiveClipIds => m_ActiveClipIds;
        public IReadOnlyList<TimelineRuntimeClipBoundary> Boundaries => m_Boundaries;
        public TimelineRuntimeEvaluationResult Evaluation { get; }
        public bool Completes { get; }
    }

    public sealed class TimelineRuntimePlayback : IDisposable
    {
        readonly List<string> m_ActiveClipIds;
        readonly List<string> m_PendingTreeClipExits;
        readonly List<string> m_ExitedTreeDecisionClips;
        readonly List<string> m_AdvanceInjectedTreeClipExits;
        readonly List<string> m_AdvanceProducedTreeClipExits;
        readonly List<string> m_AdvanceActiveClipIds;
        readonly List<string> m_AdvanceExitedTreeDecisionClips;
        readonly List<TimelineRuntimeClipBoundary> m_AdvanceBoundaries;
        readonly Comparison<TimelineRuntimeClipBoundary> m_CompareBoundaries;
        readonly ReadOnlyCollection<string> m_ActiveClipIdsView;
        TimelineRuntimeEvaluationStorage m_CandidateEvaluation;
        TimelineRuntimeEvaluationStorage m_CommittedEvaluation;
        TimelineRuntimeAdvanceResult m_PendingAdvance;
        TimelinePlaybackStopContext m_PendingStopContext;
        bool m_StopPending;
        FixedScalar m_CursorTime;
        int m_Cycle;
        string m_SectionId = string.Empty;
        bool m_InitialBoundaryPending;
        ulong m_LastCommittedLogicTick;
        readonly int m_TickRate;
        int m_TimeCarry;

        internal TimelineRuntimePlayback(
            TimelineRuntimePlaybackHandle handle,
            ulong generation,
            TimelineRuntimePreparationResult preparation,
            int tickRate)
        {
            Handle = handle;
            Generation = generation;
            RequestId = preparation.RequestId;
            ExecutionIdentity = preparation.ExecutionIdentity;
            PlaybackMode = preparation.PlaybackMode;
            if (tickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickRate));
            m_TickRate = tickRate;
            NumericTarget = preparation.NumericTarget;
            Content = preparation.Content;
            SourceTimeline = preparation.SourceTimeline;
            PreparedDependencies = preparation.PreparedDependencies;
            PreparedBindings = preparation.PreparedBindings;
            int clipCapacity = Content.Clips.Count;
            m_ActiveClipIds = new List<string>(clipCapacity);
            m_PendingTreeClipExits = new List<string>(clipCapacity);
            m_ExitedTreeDecisionClips = new List<string>(clipCapacity);
            m_AdvanceInjectedTreeClipExits = new List<string>(clipCapacity);
            m_AdvanceProducedTreeClipExits = new List<string>(clipCapacity);
            m_AdvanceActiveClipIds = new List<string>(clipCapacity);
            m_AdvanceExitedTreeDecisionClips = new List<string>(clipCapacity);
            int boundaryCapacity = 0;
            int traversals = PlaybackMode == TimelinePlaybackMode.Loop
                ? TimelineRuntimeEvaluationSegments.MaximumCycleAdvance + 1 : 1;
            for (int index = 0; index < Content.Clips.Count; index++)
            {
                TimelineContentClip clip = Content.Clips[index];
                if (!clip.TrackMuted && clip.ExecutionPolicy.IsLogic)
                    boundaryCapacity = checked(boundaryCapacity + 2 * traversals);
            }
            m_AdvanceBoundaries = new List<TimelineRuntimeClipBoundary>(boundaryCapacity);
            m_CompareBoundaries = CompareBoundaries;
            m_ActiveClipIdsView = new ReadOnlyCollection<string>(m_ActiveClipIds);
            m_CandidateEvaluation = new TimelineRuntimeEvaluationStorage(this);
            m_CommittedEvaluation = new TimelineRuntimeEvaluationStorage(this);
            State = TimelineRuntimePlaybackState.Prepared;
        }

        public TimelineRuntimePlaybackHandle Handle { get; }
        public ulong Generation { get; }
        public string RequestId { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public TimelinePlaybackMode PlaybackMode { get; }
        public TimelineRuntimeNumericTarget NumericTarget { get; }
        public TimelineContentUnit Content { get; }
        internal TimelineData SourceTimeline { get; }
        public string ContentRevision => Content.ContentHash;
        public TimelineRuntimePreparedDependencies PreparedDependencies { get; }
        public TimelinePreparedBindings PreparedBindings { get; }
        public TimelineRuntimePlaybackState State { get; private set; }
        public FixedScalar CursorTime => m_CursorTime;
        public int TickRate => m_TickRate;
        public ulong LastCommittedLogicTick => m_LastCommittedLogicTick;
        internal IReadOnlyList<string> ExitedTreeDecisionClips => m_ExitedTreeDecisionClips;
        internal int TimeCarry => m_TimeCarry;
        public AbilityTimelinePlaybackControl Control { get; private set; } = AbilityTimelinePlaybackControl.Normal;
        public int Cycle => m_Cycle;
        public string SectionId => m_SectionId;
        public IReadOnlyList<string> ActiveClipIds => m_ActiveClipIdsView;
        public bool HasStopContext { get; private set; }
        public TimelinePlaybackStopContext StopContext { get; private set; }

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
            if (m_PendingAdvance != null)
                m_AdvanceProducedTreeClipExits.Add(trimmed);
            else
                m_PendingTreeClipExits.Add(trimmed);
            return true;
        }

        public TimelineRuntimeAdvanceResult Advance(TimelineRuntimeAdvanceRequest request)
        {
            if (State != TimelineRuntimePlaybackState.Running)
                throw new InvalidOperationException("Timeline playback must be running before Advance.");
            if (m_PendingAdvance != null || m_StopPending)
                throw new InvalidOperationException("Timeline playback has an uncommitted Advance result.");
            if (request.PreviousTime != m_CursorTime)
                throw new InvalidOperationException("Timeline interval does not begin at the committed cursor.");
            m_CandidateEvaluation.Clear();
            if (request.Control.IsPaused)
            {
                if (request.TargetTime != m_CursorTime || request.TimeCarry != m_TimeCarry)
                    throw new InvalidOperationException("A paused Timeline interval must preserve the committed cursor and time carry.");
                m_PendingAdvance = new TimelineRuntimeAdvanceResult(this, Generation, request,
                    m_CursorTime, m_CursorTime, m_Cycle, m_Cycle, m_TimeCarry, m_SectionId,
                    m_ActiveClipIds, Array.Empty<TimelineRuntimeClipBoundary>(), new TimelineRuntimeEvaluationResult(m_CandidateEvaluation), false);
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
            else
                nextTime = FixedScalar.Min(requestedTime, duration);

            List<string> activeClipIds = m_AdvanceActiveClipIds;
            activeClipIds.Clear();
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
            List<TimelineRuntimeClipBoundary> boundaries = CollectBoundaries(
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
            bool completes = !loop && nextTime >= duration && !hasUnexitedTreeDecisionClip;
            m_PendingAdvance = new TimelineRuntimeAdvanceResult(
                this,
                Generation,
                request,
                m_CursorTime,
                nextTime,
                m_Cycle,
                nextCycle,
                nextTimeCarry,
                sectionId,
                activeClipIds,
                boundaries,
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
            m_LastCommittedLogicTick = advance.LogicTick;
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
            m_PendingAdvance = null;
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
            m_PendingAdvance = null;
            return true;
        }

        public bool RequestStop(TimelinePlaybackStopContext context)
        {
            if (State == TimelineRuntimePlaybackState.Disposed ||
                State == TimelineRuntimePlaybackState.Completed ||
                State == TimelineRuntimePlaybackState.Stopped ||
                State == TimelineRuntimePlaybackState.Failed)
                return false;
            if (m_StopPending || m_PendingAdvance != null)
                return false;
            m_PendingStopContext = context;
            m_StopPending = true;
            return true;
        }

        public bool CommitStop()
        {
            if (!m_StopPending || m_PendingAdvance != null)
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
            if (!m_StopPending || m_PendingAdvance != null)
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
            m_CandidateEvaluation.Clear();
            m_CommittedEvaluation.Clear();
            m_PendingAdvance = null;
            m_StopPending = false;
            m_ActiveClipIds.Clear();
            if (State != TimelineRuntimePlaybackState.Disposed)
                State = TimelineRuntimePlaybackState.Disposed;
        }

        internal bool HasPendingStop => m_StopPending;
        internal bool HasPendingAdvance => m_PendingAdvance != null;
        internal bool InitialBoundaryPending => m_InitialBoundaryPending;
        internal IReadOnlyList<string> PendingTreeDecisionClips => m_PendingTreeClipExits;

        internal bool RestoreCommittedState(
            TimelineRuntimePlaybackState state,
            FixedScalar cursorTime,
            int cycle,
            int timeCarry,
            AbilityTimelinePlaybackControl control,
            string sectionId,
            IReadOnlyList<string> activeClipIds,
            IReadOnlyList<string> exitedTreeDecisionClips,
            IReadOnlyList<string> pendingTreeDecisionClips,
            bool hasStopContext,
            TimelinePlaybackStopContext stopContext,
            bool initialBoundaryPending)
        {
            if (m_PendingAdvance != null || m_StopPending)
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
            m_CursorTime = cursorTime;
            m_Cycle = cycle;
            m_SectionId = sectionId ?? string.Empty;
            m_InitialBoundaryPending = initialBoundaryPending;
            m_TimeCarry = timeCarry;
            Control = control;
            m_ActiveClipIds.Clear();
            for (int index = 0; index < (activeClipIds?.Count ?? 0); index++)
                m_ActiveClipIds.Add(activeClipIds[index]);
            m_ExitedTreeDecisionClips.Clear();
            for (int index = 0; index < (exitedTreeDecisionClips?.Count ?? 0); index++)
                m_ExitedTreeDecisionClips.Add(exitedTreeDecisionClips[index]);
            m_PendingTreeClipExits.Clear();
            for (int index = 0; index < (pendingTreeDecisionClips?.Count ?? 0); index++)
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
            IReadOnlyList<string> activeClipIds,
            TimelineRuntimePlaybackState state,
            FixedScalar cursorTime,
            IReadOnlyList<string> exitedTreeDecisionClips)
        {
            int suppliedCount = activeClipIds?.Count ?? 0;
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

        internal static bool ContainsClip(IReadOnlyList<string> clips, string clipId)
        {
            for (int index = 0; index < clips.Count; index++)
                if (string.Equals(clips[index], clipId, StringComparison.Ordinal))
                    return true;
            return false;
        }

        void RequirePendingAdvance(TimelineRuntimeAdvanceResult advance)
        {
            if (advance == null || !ReferenceEquals(advance.Owner, this) || !ReferenceEquals(m_PendingAdvance, advance))
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

        List<TimelineRuntimeClipBoundary> CollectBoundaries(
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
            List<TimelineRuntimeClipBoundary> result = m_AdvanceBoundaries;
            result.Clear();
            if (duration <= FixedScalar.Zero)
                return result;
            FixedScalar previousAbsolute = duration * FixedScalar.FromInt64(previousCycle) + previousTime;
            FixedScalar nextAbsolute = duration * FixedScalar.FromInt64(nextCycle) + nextTime;
            if (nextAbsolute <= previousAbsolute)
                return result;
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
            result.Sort(m_CompareBoundaries);
            return result;
        }

        int CompareBoundaries(TimelineRuntimeClipBoundary left, TimelineRuntimeClipBoundary right)
        {
            FixedScalar duration = Content.Duration;
            int position = (duration * FixedScalar.FromInt64(left.Cycle) + left.Time).CompareTo(
                duration * FixedScalar.FromInt64(right.Cycle) + right.Time);
            if (position != 0)
                return position;
            int kind = left.Kind.CompareTo(right.Kind);
            return kind != 0 ? kind : string.CompareOrdinal(left.AuthoringId, right.AuthoringId);
        }

        static void AddBoundary(
            List<TimelineRuntimeClipBoundary> result,
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

    public sealed class TimelineRuntimePrepareRequest
    {
        public TimelineRuntimePrepareRequest(
            string requestId,
            TimelineData timeline,
            TimelineContractCatalog contractCatalog,
            TimelineExecutionIdentity executionIdentity,
            TimelinePlaybackMode playbackMode,
            TimelineRuntimeNumericTarget numericTarget,
            IEnumerable<TimelineCallBinding> callBindings,
            ITimelineDomainBindingResolver domainResolver,
            ITimelineRuntimeDependencyResolver dependencyResolver)
        {
            RequestId = string.IsNullOrWhiteSpace(requestId)
                ? throw new ArgumentException("Timeline prepare request identity is required.", nameof(requestId))
                : requestId.Trim();
            Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            ContractCatalog = contractCatalog ?? throw new ArgumentNullException(nameof(contractCatalog));
            if (!executionIdentity.IsValid)
                throw new ArgumentException("Timeline execution identity is invalid.", nameof(executionIdentity));
            ExecutionIdentity = executionIdentity;
            if (!Enum.IsDefined(typeof(TimelinePlaybackMode), playbackMode))
                throw new ArgumentOutOfRangeException(nameof(playbackMode));
            PlaybackMode = playbackMode;
            if (!Enum.IsDefined(typeof(TimelineRuntimeNumericTarget), numericTarget))
                throw new ArgumentOutOfRangeException(nameof(numericTarget));
            NumericTarget = numericTarget;
            CallBindings = new List<TimelineCallBinding>(callBindings ?? Array.Empty<TimelineCallBinding>()).AsReadOnly();
            DomainResolver = domainResolver ?? throw new ArgumentNullException(nameof(domainResolver));
            DependencyResolver = dependencyResolver ?? throw new ArgumentNullException(nameof(dependencyResolver));
        }

        public string RequestId { get; }
        public TimelineData Timeline { get; }
        public TimelineContractCatalog ContractCatalog { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public TimelinePlaybackMode PlaybackMode { get; }
        public TimelineRuntimeNumericTarget NumericTarget { get; }
        public IReadOnlyList<TimelineCallBinding> CallBindings { get; }
        public ITimelineDomainBindingResolver DomainResolver { get; }
        public ITimelineRuntimeDependencyResolver DependencyResolver { get; }
    }

    public enum TimelineRuntimePreparationStatus : byte
    {
        Failed = 0,
        Ready = 1
    }

    public sealed class TimelineRuntimePreparationResult
    {
        TimelineRuntimePreparationResult(
            TimelineRuntimePreparationStatus status,
            string requestId,
            TimelineExecutionIdentity executionIdentity,
            TimelinePlaybackMode playbackMode,
            TimelineRuntimeNumericTarget numericTarget,
            TimelineData sourceTimeline,
            TimelineContentUnit content,
            TimelineBindingPlan bindingPlan,
            TimelineCallInput callInput,
            TimelinePreparedBindings preparedBindings,
            TimelineRuntimePreparedDependencies preparedDependencies,
            IReadOnlyList<string> errors)
        {
            Status = status;
            RequestId = requestId ?? string.Empty;
            ExecutionIdentity = executionIdentity;
            PlaybackMode = playbackMode;
            NumericTarget = numericTarget;
            SourceTimeline = sourceTimeline;
            Content = content;
            BindingPlan = bindingPlan;
            CallInput = callInput;
            PreparedBindings = preparedBindings;
            PreparedDependencies = preparedDependencies;
            Errors = errors ?? Array.Empty<string>();
        }

        public TimelineRuntimePreparationStatus Status { get; }
        public string RequestId { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public TimelinePlaybackMode PlaybackMode { get; }
        public TimelineRuntimeNumericTarget NumericTarget { get; }
        internal TimelineData SourceTimeline { get; }
        public TimelineContentUnit Content { get; }
        public string ContentRevision => Content?.ContentHash ?? string.Empty;
        public TimelineBindingPlan BindingPlan { get; }
        public TimelineCallInput CallInput { get; }
        public TimelinePreparedBindings PreparedBindings { get; }
        public TimelineRuntimePreparedDependencies PreparedDependencies { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool IsReady => Status == TimelineRuntimePreparationStatus.Ready &&
                               Content != null &&
                               BindingPlan != null &&
                               CallInput != null &&
                               PreparedBindings != null &&
                               PreparedDependencies != null &&
                               Errors.Count == 0;

        internal static TimelineRuntimePreparationResult Failed(
            string requestId,
            TimelineExecutionIdentity executionIdentity,
            TimelineRuntimeNumericTarget numericTarget,
            IEnumerable<string> errors)
        {
            return new TimelineRuntimePreparationResult(
                TimelineRuntimePreparationStatus.Failed,
                requestId,
                executionIdentity,
                TimelinePlaybackMode.Once,
                numericTarget,
                null,
                null,
                null,
                null,
                null,
                null,
                new List<string>(errors ?? Array.Empty<string>()).AsReadOnly());
        }

        internal static TimelineRuntimePreparationResult Ready(
            TimelineRuntimePrepareRequest request,
            TimelineData sourceTimeline,
            TimelineContentUnit content,
            TimelineBindingPlan bindingPlan,
            TimelineCallInput callInput,
            TimelinePreparedBindings preparedBindings,
            TimelineRuntimePreparedDependencies preparedDependencies)
        {
            return new TimelineRuntimePreparationResult(
                TimelineRuntimePreparationStatus.Ready,
                request.RequestId,
                request.ExecutionIdentity,
                request.PlaybackMode,
                request.NumericTarget,
                sourceTimeline,
                content,
                bindingPlan,
                callInput,
                preparedBindings,
                preparedDependencies,
                Array.Empty<string>());
        }
    }

    public static class TimelineRuntimePreparation
    {
        public static TimelineRuntimePreparationResult Prepare(TimelineRuntimePrepareRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            TimelineContentDiscoveryResult discovery = TimelineContentDiscovery.Discover(
                request.Timeline,
                request.ContractCatalog);
            if (!discovery.IsValid)
                return TimelineRuntimePreparationResult.Failed(
                    request.RequestId,
                    request.ExecutionIdentity,
                    request.NumericTarget,
                    discovery.Errors);

            try
            {
                var errors = new List<string>();
                TimelineRuntimeEvaluator.ValidateTreeContracts(request.Timeline, discovery.Content, errors);
                if (errors.Count != 0)
                    return TimelineRuntimePreparationResult.Failed(
                        request.RequestId,
                        request.ExecutionIdentity,
                        request.NumericTarget,
                        errors);
                var dependencyHandles = new List<TimelineRuntimeDependencyHandle>(discovery.Content.Dependencies.Count);
                for (int index = 0; index < discovery.Content.Dependencies.Count; index++)
                {
                    TimelineContentDependency dependency = discovery.Content.Dependencies[index];
                    if (!request.DependencyResolver.TryResolve(
                            dependency,
                            request.NumericTarget,
                            out TimelineRuntimeDependencyHandle handle,
                            out string error) || !handle.IsValid)
                    {
                        errors.Add($"timeline_dependency_unresolved:{dependency.Identity}:{error ?? "dependency is unresolved"}");
                        continue;
                    }
                    dependencyHandles.Add(handle);
                }
                if (errors.Count != 0 || dependencyHandles.Count != discovery.Content.Dependencies.Count)
                    return TimelineRuntimePreparationResult.Failed(
                        request.RequestId,
                        request.ExecutionIdentity,
                        request.NumericTarget,
                        errors);

                TimelineBindingPlan bindingPlan = new TimelineBindingPlan(discovery.Content);
                TimelineCallInput callInput = new TimelineCallInput(bindingPlan, request.CallBindings);
                TimelinePreparedBindings preparedBindings = TimelineBindingPreparation.Prepare(
                    bindingPlan,
                    callInput,
                    request.DomainResolver,
                    errors);
                if (preparedBindings == null || errors.Count != 0)
                    return TimelineRuntimePreparationResult.Failed(
                        request.RequestId,
                        request.ExecutionIdentity,
                        request.NumericTarget,
                        errors);
                TimelineData sourceTimeline = request.Timeline.Clone();
                sourceTimeline.Init();
                return TimelineRuntimePreparationResult.Ready(
                    request,
                    sourceTimeline,
                    discovery.Content,
                    bindingPlan,
                    callInput,
                    preparedBindings,
                    new TimelineRuntimePreparedDependencies(discovery.Content.Dependencies, dependencyHandles));
            }
            catch (Exception exception)
            {
                return TimelineRuntimePreparationResult.Failed(
                    request.RequestId,
                    request.ExecutionIdentity,
                    request.NumericTarget,
                    new[] { exception.Message });
            }
        }

        public static TimelineRuntimePlayback CreatePlayback(
            TimelineRuntimePreparationResult preparation,
            ulong generation,
            int tickRate)
        {
            if (preparation == null)
                throw new ArgumentNullException(nameof(preparation));
            if (!preparation.IsReady)
                throw new InvalidOperationException("Timeline playback cannot be created from a failed preparation.");
            return CreatePlayback(
                preparation,
                new TimelineRuntimePlaybackHandle(preparation.ExecutionIdentity.InstanceId),
                generation,
                tickRate);
        }

        public static TimelineRuntimePlayback CreatePlayback(
            TimelineRuntimePreparationResult preparation,
            TimelineRuntimePlaybackHandle handle,
            ulong generation,
            int tickRate)
        {
            if (preparation == null)
                throw new ArgumentNullException(nameof(preparation));
            if (!preparation.IsReady)
                throw new InvalidOperationException("Timeline playback cannot be created from a failed preparation.");
            if (!handle.IsValid)
                throw new ArgumentOutOfRangeException(nameof(handle));
            if (generation == 0)
                throw new ArgumentOutOfRangeException(nameof(generation));
            return new TimelineRuntimePlayback(
                handle,
                generation,
                preparation,
                tickRate);
        }
    }

    public readonly struct TimelineRuntimeTreeClipRequest
    {
        public TimelineRuntimeTreeClipRequest(
            string clipAuthoringId,
            string trackAuthoringId,
            string treeGraphId,
            string treeGraphRevision,
            TimelineTreeExecutionPhase phase,
            TimelineRuntimeTreeClipEventKind eventKind,
            FixedScalar time,
            int cycle,
            float normalizedTime,
            ulong generation,
            string callId)
        {
            ClipAuthoringId = string.IsNullOrWhiteSpace(clipAuthoringId)
                ? throw new ArgumentException("TreeClip identity is required.", nameof(clipAuthoringId))
                : clipAuthoringId.Trim();
            TrackAuthoringId = string.IsNullOrWhiteSpace(trackAuthoringId)
                ? throw new ArgumentException("Tree Track identity is required.", nameof(trackAuthoringId))
                : trackAuthoringId.Trim();
            TreeGraphId = string.IsNullOrWhiteSpace(treeGraphId)
                ? throw new ArgumentException("Tree graph identity is required.", nameof(treeGraphId))
                : treeGraphId.Trim();
            TreeGraphRevision = string.IsNullOrWhiteSpace(treeGraphRevision)
                ? throw new ArgumentException("Tree graph revision is required.", nameof(treeGraphRevision))
                : treeGraphRevision.Trim();
            Phase = phase;
            EventKind = eventKind;
            Time = time;
            Cycle = cycle;
            NormalizedTime = Mathf.Clamp01(normalizedTime);
            Generation = generation == 0
                ? throw new ArgumentOutOfRangeException(nameof(generation))
                : generation;
            CallId = string.IsNullOrWhiteSpace(callId)
                ? throw new ArgumentException("TreeClip call identity is required.", nameof(callId))
                : callId.Trim();
        }

        public string ClipAuthoringId { get; }
        public string TrackAuthoringId { get; }
        public string TreeGraphId { get; }
        public string TreeGraphRevision { get; }
        public TimelineTreeExecutionPhase Phase { get; }
        public TimelineRuntimeTreeClipEventKind EventKind { get; }
        public FixedScalar Time { get; }
        public int Cycle { get; }
        public float NormalizedTime { get; }
        public ulong Generation { get; }
        public string CallId { get; }
    }

    public readonly struct TimelineRuntimeMarkerRequest
    {
        public TimelineRuntimeMarkerRequest(
            string markerAuthoringId,
            string trackAuthoringId,
            string graphId,
            string graphRevision,
            FixedScalar time,
            int cycle,
            ulong generation,
            string callId)
        {
            MarkerAuthoringId = string.IsNullOrWhiteSpace(markerAuthoringId)
                ? throw new ArgumentException("Timeline Marker identity is required.", nameof(markerAuthoringId))
                : markerAuthoringId.Trim();
            TrackAuthoringId = string.IsNullOrWhiteSpace(trackAuthoringId)
                ? throw new ArgumentException("Timeline Track identity is required.", nameof(trackAuthoringId))
                : trackAuthoringId.Trim();
            GraphId = string.IsNullOrWhiteSpace(graphId)
                ? throw new ArgumentException("Timeline Marker graph identity is required.", nameof(graphId))
                : graphId.Trim();
            GraphRevision = string.IsNullOrWhiteSpace(graphRevision)
                ? throw new ArgumentException("Timeline Marker graph revision is required.", nameof(graphRevision))
                : graphRevision.Trim();
            if (time < FixedScalar.Zero || cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(time));
            Generation = generation == 0
                ? throw new ArgumentOutOfRangeException(nameof(generation))
                : generation;
            CallId = string.IsNullOrWhiteSpace(callId)
                ? throw new ArgumentException("Timeline Marker call identity is required.", nameof(callId))
                : callId.Trim();
            Time = time;
            Cycle = cycle;
        }

        public string MarkerAuthoringId { get; }
        public string TrackAuthoringId { get; }
        public string GraphId { get; }
        public string GraphRevision { get; }
        public FixedScalar Time { get; }
        public int Cycle { get; }
        public ulong Generation { get; }
        public string CallId { get; }
    }

    public enum TimelineRuntimeTreeClipEventKind : byte
    {
        Enter = 1,
        Update = 2,
        Exit = 3
    }

    public readonly struct TimelineRuntimePresentationEvent
    {
        public TimelineRuntimePresentationEvent(
            TimelineRuntimePlaybackHandle playbackHandle,
            TimelineExecutionIdentity executionIdentity,
            ulong generation,
            string markerAuthoringId,
            ulong traversalIndex,
            string graphId,
            string graphRevision,
            FixedScalar time,
            int cycle)
        {
            if (!playbackHandle.IsValid || !executionIdentity.IsValid || generation == 0 || traversalIndex == 0)
                throw new ArgumentException("Timeline presentation event execution identity is invalid.", nameof(executionIdentity));
            PlaybackHandle = playbackHandle;
            ExecutionIdentity = executionIdentity;
            Generation = generation;
            MarkerAuthoringId = string.IsNullOrWhiteSpace(markerAuthoringId)
                ? throw new ArgumentException("Timeline presentation event marker identity is required.", nameof(markerAuthoringId))
                : markerAuthoringId.Trim();
            GraphId = string.IsNullOrWhiteSpace(graphId)
                ? throw new ArgumentException("Timeline presentation marker graph identity is required.", nameof(graphId))
                : graphId.Trim();
            GraphRevision = string.IsNullOrWhiteSpace(graphRevision)
                ? throw new ArgumentException("Timeline presentation marker graph revision is required.", nameof(graphRevision))
                : graphRevision.Trim();
            if (time < FixedScalar.Zero || cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(time));
            TraversalIndex = traversalIndex;
            Time = time;
            Cycle = cycle;
            Identity = $"{PlaybackHandle.Value}:{Generation}:{MarkerAuthoringId}:{TraversalIndex}";
        }

        public string Identity { get; }
        public EventId EventId => new(StableHash.Compute("btsmtl-timeline-presentation-marker", Identity));
        public TimelineRuntimePlaybackHandle PlaybackHandle { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public ulong Generation { get; }
        public string MarkerAuthoringId { get; }
        public ulong TraversalIndex { get; }
        public string GraphId { get; }
        public string GraphRevision { get; }
        public FixedScalar Time { get; }
        public int Cycle { get; }
    }

    public readonly struct TimelineRuntimeClipSample
    {
        public TimelineRuntimeClipSample(
            string clipAuthoringId,
            string trackAuthoringId,
            string contractKind,
            TimelineRuntimeTreeClipEventKind eventKind,
            FixedScalar time,
            int cycle,
            float normalizedTime)
        {
            ClipAuthoringId = string.IsNullOrWhiteSpace(clipAuthoringId)
                ? throw new ArgumentException("Timeline Clip identity is required.", nameof(clipAuthoringId))
                : clipAuthoringId.Trim();
            TrackAuthoringId = string.IsNullOrWhiteSpace(trackAuthoringId)
                ? throw new ArgumentException("Timeline Track identity is required.", nameof(trackAuthoringId))
                : trackAuthoringId.Trim();
            ContractKind = string.IsNullOrWhiteSpace(contractKind)
                ? throw new ArgumentException("Timeline Clip contract kind is required.", nameof(contractKind))
                : contractKind.Trim();
            EventKind = eventKind;
            Time = time;
            Cycle = cycle;
            NormalizedTime = Mathf.Clamp01(normalizedTime);
        }

        public string ClipAuthoringId { get; }
        public string TrackAuthoringId { get; }
        public string ContractKind { get; }
        public TimelineRuntimeTreeClipEventKind EventKind { get; }
        public FixedScalar Time { get; }
        public int Cycle { get; }
        public float NormalizedTime { get; }
    }

    public readonly struct TimelineRuntimeScenePresentationSample
    {
        public TimelineRuntimeScenePresentationSample(
            string clipAuthoringId,
            string targetBindingId,
            string parameterBindingId,
            TimelineBindingValueKind valueKind,
            float value,
            float normalizedTime,
            int frame,
            int cycle,
            TimelineExecutionIdentity executionIdentity,
            ulong generation)
        {
            ClipAuthoringId = clipAuthoringId ?? string.Empty;
            TargetBindingId = targetBindingId ?? string.Empty;
            ParameterBindingId = parameterBindingId ?? string.Empty;
            ValueKind = valueKind;
            Value = value;
            NormalizedTime = Mathf.Clamp01(normalizedTime);
            Frame = frame;
            Cycle = cycle;
            ExecutionIdentity = executionIdentity;
            Generation = generation;
        }

        public string ClipAuthoringId { get; }
        public string TargetBindingId { get; }
        public string ParameterBindingId { get; }
        public TimelineBindingValueKind ValueKind { get; }
        public float Value { get; }
        public float NormalizedTime { get; }
        public int Frame { get; }
        public int Cycle { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public ulong Generation { get; }
    }

    public readonly struct TimelineRuntimeMotionWarpRequest
    {
        public TimelineRuntimeMotionWarpRequest(
            string clipAuthoringId,
            string sourceMotionClipId,
            int frame,
            int cycle,
            float previousNormalizedTime,
            float normalizedTime,
            MotionWarpTranslationMode translationMode,
            MotionWarpTargetOffsetSpace targetOffsetSpace,
            MotionWarpRotationMode rotationMode,
            MotionWarpRotationMethod rotationMethod,
            Vector2 targetPlanarOffset,
            float targetYawOffsetDegrees,
            float maxTotalPositionCorrection,
            float maxTotalYawCorrectionDegrees,
            float maximumYawRateDegreesPerSecond,
            MotionWarpLimitPolicy limitPolicy)
        {
            ClipAuthoringId = clipAuthoringId ?? string.Empty;
            SourceMotionClipId = sourceMotionClipId ?? string.Empty;
            Frame = frame;
            Cycle = cycle;
            PreviousNormalizedTime = Mathf.Clamp01(previousNormalizedTime);
            NormalizedTime = Mathf.Clamp01(normalizedTime);
            TranslationMode = translationMode;
            TargetOffsetSpace = targetOffsetSpace;
            RotationMode = rotationMode;
            RotationMethod = rotationMethod;
            TargetPlanarOffset = targetPlanarOffset;
            TargetYawOffsetDegrees = targetYawOffsetDegrees;
            MaxTotalPositionCorrection = Mathf.Max(0f, maxTotalPositionCorrection);
            MaxTotalYawCorrectionDegrees = Mathf.Max(0f, maxTotalYawCorrectionDegrees);
            MaximumYawRateDegreesPerSecond = Mathf.Max(0f, maximumYawRateDegreesPerSecond);
            LimitPolicy = limitPolicy;
        }

        public string ClipAuthoringId { get; }
        public string SourceMotionClipId { get; }
        public int Frame { get; }
        public int Cycle { get; }
        public float PreviousNormalizedTime { get; }
        public float NormalizedTime { get; }
        public MotionWarpTranslationMode TranslationMode { get; }
        public MotionWarpTargetOffsetSpace TargetOffsetSpace { get; }
        public MotionWarpRotationMode RotationMode { get; }
        public MotionWarpRotationMethod RotationMethod { get; }
        public Vector2 TargetPlanarOffset { get; }
        public float TargetYawOffsetDegrees { get; }
        public float MaxTotalPositionCorrection { get; }
        public float MaxTotalYawCorrectionDegrees { get; }
        public float MaximumYawRateDegreesPerSecond { get; }
        public MotionWarpLimitPolicy LimitPolicy { get; }
    }

    public readonly struct TimelineRuntimeTraceOutput
    {
        public TimelineRuntimeTraceOutput(
            string timelineAuthoringId,
            string trackAuthoringId,
            string clipAuthoringId,
            string code,
            TimelineTraceSeverity severity,
            string detail,
            TimelineExecutionIdentity executionIdentity,
            ulong generation,
            ulong logicTick,
            FixedScalar time,
            int cycle)
        {
            TimelineAuthoringId = string.IsNullOrWhiteSpace(timelineAuthoringId)
                ? throw new ArgumentException("Timeline identity is required.", nameof(timelineAuthoringId))
                : timelineAuthoringId.Trim();
            TrackAuthoringId = trackAuthoringId?.Trim() ?? string.Empty;
            ClipAuthoringId = clipAuthoringId?.Trim() ?? string.Empty;
            Code = string.IsNullOrWhiteSpace(code)
                ? throw new ArgumentException("Timeline trace code is required.", nameof(code))
                : code.Trim();
            if (!executionIdentity.IsValid)
                throw new ArgumentException("Timeline trace execution identity is required.", nameof(executionIdentity));
            if (generation == 0)
                throw new ArgumentOutOfRangeException(nameof(generation));
            if (logicTick == 0)
                throw new ArgumentOutOfRangeException(nameof(logicTick));
            if (time < FixedScalar.Zero)
                throw new ArgumentOutOfRangeException(nameof(time));
            if (cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(cycle));
            Severity = severity;
            Detail = detail ?? string.Empty;
            ExecutionIdentity = executionIdentity;
            Generation = generation;
            LogicTick = logicTick;
            Time = time;
            Cycle = cycle;
        }

        public string TimelineAuthoringId { get; }
        public string TrackAuthoringId { get; }
        public string ClipAuthoringId { get; }
        public string Code { get; }
        public TimelineTraceSeverity Severity { get; }
        public string Detail { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public ulong Generation { get; }
        public ulong LogicTick { get; }
        public FixedScalar Time { get; }
        public int Cycle { get; }
    }

    public readonly struct TimelineRuntimeEvaluationResult
    {
        internal bool IsCurrent => AnimationContributions.IsCurrent;

        internal TimelineRuntimeEvaluationResult(TimelineRuntimeEvaluationStorage storage)
        {
            AnimationContributions = storage.AnimationContributions.View;
            MotionContributions = storage.MotionContributions.View;
            CameraStates = storage.CameraStates.View;
            CameraCues = storage.CameraCues.View;
            CameraResponses = storage.CameraResponses.View;
            CameraResources = storage.CameraResources.View;
            ActionCues = storage.ActionCues.View;
            TreeClips = storage.TreeClips.View;
            Markers = storage.Markers.View;
            ScenePresentation = storage.ScenePresentation.View;
            MotionWarps = storage.MotionWarps.View;
            ClipSamples = storage.ClipSamples.View;
            Traces = storage.Traces.View;
        }

        public TimelineRuntimeSampleView<TimelineAnimationContribution> AnimationContributions { get; }
        public TimelineRuntimeSampleView<TimelineMotionCurveContribution> MotionContributions { get; }
        public TimelineRuntimeSampleView<TimelineCameraStateSample> CameraStates { get; }
        public TimelineRuntimeSampleView<TimelineCameraCueSample> CameraCues { get; }
        public TimelineRuntimeSampleView<TimelineCameraResponseSample> CameraResponses { get; }
        public TimelineRuntimeSampleView<TimelineCameraResourceSample> CameraResources { get; }
        public TimelineRuntimeSampleView<TimelineActionCueSample> ActionCues { get; }
        public TimelineRuntimeSampleView<TimelineRuntimeTreeClipRequest> TreeClips { get; }
        public TimelineRuntimeSampleView<TimelineRuntimeMarkerRequest> Markers { get; }
        public TimelineRuntimeSampleView<TimelineRuntimeScenePresentationSample> ScenePresentation { get; }
        public TimelineRuntimeSampleView<TimelineRuntimeMotionWarpRequest> MotionWarps { get; }
        public TimelineRuntimeSampleView<TimelineRuntimeClipSample> ClipSamples { get; }
        public TimelineRuntimeSampleView<TimelineRuntimeTraceOutput> Traces { get; }
    }

    sealed class TimelineRuntimeEvaluationStorage
    {
        public TimelineRuntimeEvaluationStorage(TimelineRuntimePlayback playback)
        {
            int animations = 0, motions = 0, cameraStates = 0, cameraCues = 0;
            int cameraResponses = 0, cameraResources = 0, actionCues = 0, treeClips = 0;
            int markers = 0, scenePresentation = 0, motionWarps = 0, clips = 0;
            TimelineData timeline = playback.SourceTimeline;
            for (int index = 0; index < timeline.Tracks.Count; index++)
            {
                Track track = timeline.Tracks[index];
                if (track == null || track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Logic)
                    continue;
                int count = track.Clips.Count;
                clips = checked(clips + count);
                switch (track)
                {
                    case AnimationTrack: animations = checked(animations + count); break;
                    case MotionCurveTrack: motions = checked(motions + count); break;
                    case CameraStateTrack: cameraStates = checked(cameraStates + count); break;
                    case CameraCueTrack: cameraCues = checked(cameraCues + count); break;
                    case CameraResponseTrack: cameraResponses = checked(cameraResponses + count); break;
                    case CameraEffectTrack: cameraResources = checked(cameraResources + count); break;
                    case ActionCueTrack: actionCues = checked(actionCues + count); break;
                    case TreeTrack: treeClips = checked(treeClips + count); break;
                    case ScenePresentationParameterTrack: scenePresentation = checked(scenePresentation + count); break;
                    case MotionWarpTrack: motionWarps = checked(motionWarps + count); break;
                }
            }
            for (int index = 0; index < playback.Content.Markers.Count; index++)
            {
                TimelineContentMarker marker = playback.Content.Markers[index];
                if (!marker.TrackMuted && marker.ExecutionPolicy.IsLogic)
                    markers++;
            }
            int traversals = playback.PlaybackMode == TimelinePlaybackMode.Loop
                ? TimelineRuntimeEvaluationSegments.MaximumCycleAdvance + 1 : 1;
            int lifecycleSamples = checked(2 * traversals + 1);
            AnimationContributions = new(checked(animations * traversals));
            MotionContributions = new(checked(motions * traversals));
            CameraStates = new(checked(cameraStates));
            CameraCues = new(checked(cameraCues * traversals));
            CameraResponses = new(checked(cameraResponses));
            CameraResources = new(checked(cameraResources));
            ActionCues = new(checked(actionCues * traversals));
            TreeClips = new(checked(treeClips * lifecycleSamples));
            Markers = new(checked(markers * traversals));
            ScenePresentation = new(checked(scenePresentation));
            MotionWarps = new(checked(motionWarps * traversals));
            ClipSamples = new(checked(clips * lifecycleSamples));
            Traces = new(checked(2 * clips * traversals + treeClips));
        }

        public readonly TimelineRuntimeSampleBuffer<TimelineAnimationContribution> AnimationContributions;
        public readonly TimelineRuntimeSampleBuffer<TimelineMotionCurveContribution> MotionContributions;
        public readonly TimelineRuntimeSampleBuffer<TimelineCameraStateSample> CameraStates;
        public readonly TimelineRuntimeSampleBuffer<TimelineCameraCueSample> CameraCues;
        public readonly TimelineRuntimeSampleBuffer<TimelineCameraResponseSample> CameraResponses;
        public readonly TimelineRuntimeSampleBuffer<TimelineCameraResourceSample> CameraResources;
        public readonly TimelineRuntimeSampleBuffer<TimelineActionCueSample> ActionCues;
        public readonly TimelineRuntimeSampleBuffer<TimelineRuntimeTreeClipRequest> TreeClips;
        public readonly TimelineRuntimeSampleBuffer<TimelineRuntimeMarkerRequest> Markers;
        public readonly TimelineRuntimeSampleBuffer<TimelineRuntimeScenePresentationSample> ScenePresentation;
        public readonly TimelineRuntimeSampleBuffer<TimelineRuntimeMotionWarpRequest> MotionWarps;
        public readonly TimelineRuntimeSampleBuffer<TimelineRuntimeClipSample> ClipSamples;
        public readonly TimelineRuntimeSampleBuffer<TimelineRuntimeTraceOutput> Traces;

        public void Clear()
        {
            AnimationContributions.Clear();
            MotionContributions.Clear();
            CameraStates.Clear();
            CameraCues.Clear();
            CameraResponses.Clear();
            CameraResources.Clear();
            ActionCues.Clear();
            TreeClips.Clear();
            Markers.Clear();
            ScenePresentation.Clear();
            MotionWarps.Clear();
            ClipSamples.Clear();
            Traces.Clear();
        }
    }

    public readonly struct TimelineRuntimeSampleView<T>
    {
        readonly TimelineRuntimeSampleBuffer<T> m_Buffer;
        readonly ulong m_Version;

        internal TimelineRuntimeSampleView(TimelineRuntimeSampleBuffer<T> buffer)
        {
            m_Buffer = buffer;
            m_Version = buffer.Version;
        }

        internal bool IsCurrent => m_Buffer != null && m_Buffer.Version == m_Version;

        public int Count
        {
            get
            {
                if (m_Buffer == null)
                    return 0;
                RequireCurrent();
                return m_Buffer.Count;
            }
        }

        public T this[int index]
        {
            get
            {
                RequireCurrent();
                return m_Buffer[index];
            }
        }

        void RequireCurrent()
        {
            if (m_Buffer == null || m_Buffer.Version != m_Version)
                throw new InvalidOperationException("Timeline sample view no longer belongs to a live frame.");
        }
    }

    sealed class TimelineRuntimeSampleBuffer<T> : ICollection<T>
    {
        readonly T[] m_Values;

        public TimelineRuntimeSampleBuffer(int capacity)
        {
            m_Values = capacity == 0 ? Array.Empty<T>() : new T[capacity];
        }

        public int Count { get; private set; }
        public ulong Version { get; private set; }
        public bool IsReadOnly => false;
        public T this[int index] => (uint)index < (uint)Count
            ? m_Values[index] : throw new ArgumentOutOfRangeException(nameof(index));
        public TimelineRuntimeSampleView<T> View => new(this);

        public void Add(T value)
        {
            if (Count == m_Values.Length)
                throw new InvalidOperationException("Timeline sample count exceeds the prepared content capacity.");
            m_Values[Count++] = value;
        }

        public void Clear()
        {
            Array.Clear(m_Values, 0, Count);
            Count = 0;
            Version = checked(Version + 1);
        }

        public bool Contains(T value) => Array.IndexOf(m_Values, value, 0, Count) >= 0;
        public void CopyTo(T[] array, int arrayIndex) => Array.Copy(m_Values, 0, array, arrayIndex, Count);
        public bool Remove(T value) => throw new NotSupportedException();
        public IEnumerator<T> GetEnumerator() => throw new NotSupportedException("Use the indexed Timeline sample view.");
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    sealed class TimelineRuntimePresentationBuffer
    {
        public TimelineRuntimePresentationBuffer(TimelineRuntimePlayback playback)
        {
            int animations = 0, states = 0, cues = 0, responses = 0, resources = 0, scene = 0, markers = 0;
            TimelineData timeline = playback.SourceTimeline;
            for (int index = 0; index < timeline.Tracks.Count; index++)
            {
                Track track = timeline.Tracks[index];
                if (track == null || track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Presentation)
                    continue;
                int count = track.Clips.Count;
                switch (track)
                {
                    case AnimationTrack: animations = checked(animations + count); break;
                    case CameraStateTrack: states = checked(states + count); break;
                    case CameraCueTrack: cues = checked(cues + count); break;
                    case CameraResponseTrack: responses = checked(responses + count); break;
                    case CameraEffectTrack: resources = checked(resources + count); break;
                    case ScenePresentationParameterTrack: scene = checked(scene + count); break;
                }
            }
            for (int index = 0; index < playback.Content.Markers.Count; index++)
            {
                TimelineContentMarker marker = playback.Content.Markers[index];
                if (marker.ExecutionPolicy.IsPresentation && !marker.TrackMuted)
                    markers++;
            }
            int traversals = playback.PlaybackMode == TimelinePlaybackMode.Loop
                ? TimelineRuntimeEvaluationSegments.MaximumCycleAdvance + 1 : 1;
            Animations = new(animations);
            CameraStates = new(states);
            CameraCues = new(checked(cues * traversals));
            CameraResponses = new(responses);
            CameraResources = new(resources);
            ScenePresentation = new(scene);
            Events = new(checked(markers * traversals));
        }

        public readonly TimelineRuntimeSampleBuffer<TimelineAnimationContribution> Animations;
        public readonly TimelineRuntimeSampleBuffer<TimelineCameraStateSample> CameraStates;
        public readonly TimelineRuntimeSampleBuffer<TimelineCameraCueSample> CameraCues;
        public readonly TimelineRuntimeSampleBuffer<TimelineCameraResponseSample> CameraResponses;
        public readonly TimelineRuntimeSampleBuffer<TimelineCameraResourceSample> CameraResources;
        public readonly TimelineRuntimeSampleBuffer<TimelineRuntimeScenePresentationSample> ScenePresentation;
        public readonly TimelineRuntimeSampleBuffer<TimelineRuntimePresentationEvent> Events;

        public void Clear()
        {
            Animations.Clear();
            CameraStates.Clear();
            CameraCues.Clear();
            CameraResponses.Clear();
            CameraResources.Clear();
            ScenePresentation.Clear();
            Events.Clear();
        }
    }

    public readonly struct TimelineRuntimePresentationOperations
    {
        internal TimelineRuntimePresentationOperations(TimelineRuntimePresentationBuffer buffer)
        {
            AnimationContributions = buffer.Animations.View;
            CameraStates = buffer.CameraStates.View;
            CameraCues = buffer.CameraCues.View;
            CameraResponses = buffer.CameraResponses.View;
            CameraResources = buffer.CameraResources.View;
            ScenePresentation = buffer.ScenePresentation.View;
        }

        public TimelineRuntimeSampleView<TimelineAnimationContribution> AnimationContributions { get; }
        public TimelineRuntimeSampleView<TimelineCameraStateSample> CameraStates { get; }
        public TimelineRuntimeSampleView<TimelineCameraCueSample> CameraCues { get; }
        public TimelineRuntimeSampleView<TimelineCameraResponseSample> CameraResponses { get; }
        public TimelineRuntimeSampleView<TimelineCameraResourceSample> CameraResources { get; }
        public TimelineRuntimeSampleView<TimelineRuntimeScenePresentationSample> ScenePresentation { get; }
    }

    public readonly struct TimelineRuntimePresentationFrame
    {
        internal TimelineRuntimePresentationFrame(
            TimelineRuntimePlayback playback,
            ulong logicTick,
            ulong presentationFrame,
            float presentationDeltaSeconds,
            float interpolationAlpha,
            TimelineRuntimePresentationOperations operations,
            TimelineRuntimeSampleView<TimelineRuntimePresentationEvent> events,
            TimelinePresentationSampleReason reason,
            FixedScalar time,
            int cycle)
        {
            if (playback == null || !playback.Handle.IsValid || playback.Generation == 0 || presentationFrame == 0 ||
                !float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f ||
                !float.IsFinite(interpolationAlpha) || interpolationAlpha < 0f || interpolationAlpha > 1f)
                throw new ArgumentException("Timeline presentation frame is invalid.");
            Handle = playback.Handle;
            Generation = playback.Generation;
            LogicTick = logicTick;
            PresentationFrame = presentationFrame;
            PresentationDeltaSeconds = presentationDeltaSeconds;
            InterpolationAlpha = interpolationAlpha;
            ExecutionIdentity = playback.ExecutionIdentity;
            Operations = operations;
            Reason = reason;
            Time = time;
            Cycle = cycle;
            Events = events;
        }

        public TimelineRuntimePlaybackHandle Handle { get; }
        public ulong Generation { get; }
        public ulong LogicTick { get; }
        public ulong PresentationFrame { get; }
        public float PresentationDeltaSeconds { get; }
        public float InterpolationAlpha { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public TimelineRuntimePresentationOperations Operations { get; }
        public TimelinePresentationSampleReason Reason { get; }
        public FixedScalar Time { get; }
        public int Cycle { get; }
        public TimelineRuntimeSampleView<TimelineRuntimePresentationEvent> Events { get; }
    }

    public enum TimelineRuntimeStepDecision : byte
    {
        Commit = 1,
        Discard = 2
    }

    public readonly struct TimelineRuntimeStepContext
    {
        internal TimelineRuntimeStepContext(
            TimelineRuntimePlayback playback,
            TimelineRuntimeAdvanceRequest request,
            TimelineRuntimeAdvanceResult advance)
        {
            Playback = playback;
            Request = request;
            Advance = advance;
        }

        public TimelineRuntimePlayback Playback { get; }
        public TimelineRuntimeAdvanceRequest Request { get; }
        public TimelineRuntimeAdvanceResult Advance { get; }
    }

    public interface ITimelineRuntimeStepConsumer
    {
        TimelineRuntimeStepDecision Consume(TimelineRuntimeStepContext context);
    }

    public static class TimelineRuntimeStepCoordinator
    {
        public static TimelineRuntimeAdvanceResult Step(
            TimelineRuntimePlayback playback,
            TimelineRuntimeAdvanceRequest request,
            ITimelineRuntimeStepConsumer consumer)
        {
            if (playback == null)
                throw new ArgumentNullException(nameof(playback));
            if (consumer == null)
                throw new ArgumentNullException(nameof(consumer));
            TimelineRuntimeAdvanceResult advance = playback.Advance(request);
            var context = new TimelineRuntimeStepContext(playback, request, advance);
            TimelineRuntimeStepDecision decision;
            try
            {
                decision = consumer.Consume(context);
            }
            catch
            {
                playback.Discard(advance);
                if (consumer is ITimelineRuntimeStepCommitConsumer commitConsumer)
                    commitConsumer.Discard(context);
                throw;
            }
            if (decision == TimelineRuntimeStepDecision.Commit)
            {
                playback.Commit(advance);
                if (consumer is ITimelineRuntimeStepCommitConsumer commitConsumer)
                    commitConsumer.Commit(context);
                return advance;
            }
            if (decision == TimelineRuntimeStepDecision.Discard)
            {
                playback.Discard(advance);
                if (consumer is ITimelineRuntimeStepCommitConsumer commitConsumer)
                    commitConsumer.Discard(context);
                return advance;
            }
            playback.Discard(advance);
            if (consumer is ITimelineRuntimeStepCommitConsumer invalidDecisionConsumer)
                invalidDecisionConsumer.Discard(context);
            throw new InvalidOperationException("Timeline Step consumer returned an invalid decision.");
        }
    }

    internal static class TimelineRuntimeEvaluator
    {
        public static TimelineRuntimeEvaluationResult Evaluate(
            TimelineRuntimeEvaluationStorage storage,
            TimelineData timeline,
            TimelineContentUnit content,
            FixedScalar previousPosition,
            int previousCycle,
            FixedScalar currentPosition,
            int currentCycle,
            bool loop,
            IReadOnlyList<TimelineRuntimeClipBoundary> boundaries,
            TimelineExecutionIdentity executionIdentity,
            ulong generation,
            ulong logicTick,
            bool includeStartBoundary,
            IReadOnlyList<string> exitedTreeDecisionClips)
        {
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            int frameRate = Math.Max(1, content.FrameRate);
            TimelineRuntimeSampleBuffer<TimelineAnimationContribution> animations = storage.AnimationContributions;
            TimelineRuntimeSampleBuffer<TimelineMotionCurveContribution> motions = storage.MotionContributions;
            TimelineRuntimeSampleBuffer<TimelineCameraStateSample> cameraStates = storage.CameraStates;
            TimelineRuntimeSampleBuffer<TimelineCameraCueSample> cameraCues = storage.CameraCues;
            TimelineRuntimeSampleBuffer<TimelineCameraResponseSample> cameraResponses = storage.CameraResponses;
            TimelineRuntimeSampleBuffer<TimelineCameraResourceSample> cameraResources = storage.CameraResources;
            TimelineRuntimeSampleBuffer<TimelineActionCueSample> actionCues = storage.ActionCues;
            TimelineRuntimeSampleBuffer<TimelineRuntimeTreeClipRequest> treeClips = storage.TreeClips;
            TimelineRuntimeSampleBuffer<TimelineRuntimeMarkerRequest> markers = storage.Markers;
            TimelineRuntimeSampleBuffer<TimelineRuntimeScenePresentationSample> scenePresentation = storage.ScenePresentation;
            TimelineRuntimeSampleBuffer<TimelineRuntimeMotionWarpRequest> motionWarps = storage.MotionWarps;
            TimelineRuntimeSampleBuffer<TimelineRuntimeClipSample> clipSamples = storage.ClipSamples;
            TimelineRuntimeSampleBuffer<TimelineRuntimeTraceOutput> traces = storage.Traces;
            var segments = new TimelineRuntimeEvaluationSegments(
                previousPosition,
                previousCycle,
                currentPosition,
                currentCycle,
                content.Duration,
                loop);
            AppendMarkerRequests(
                content,
                executionIdentity,
                generation,
                previousPosition,
                previousCycle,
                currentPosition,
                currentCycle,
                loop,
                includeStartBoundary,
                markers);
            for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                TimelineRuntimeEvaluationSegment segment = segments[segmentIndex];
                for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
                {
                    Track track = timeline.Tracks[trackIndex];
                    if (track == null || track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Logic)
                        continue;
                    if (track is AnimationTrack animationTrack)
                    {
                        animationTrack.Sample(
                            segment.PreviousTime,
                            segment.CurrentTime,
                            trackIndex,
                            timeline.AuthoringId,
                            timeline.Name,
                            animations,
                            loop,
                            segment.Cycle);
                    }
                    else if (track is MotionCurveTrack motionTrack)
                    {
                        motionTrack.Sample(
                            segment.PreviousTime,
                            segment.CurrentTime,
                            timeline.AuthoringId,
                            timeline.Name,
                            motions);
                    }
                    else if (track is CameraCueTrack cameraCueTrack)
                    {
                        cameraCueTrack.Sample(
                            segment.PreviousTime,
                            segment.CurrentTime,
                            timeline.AuthoringId,
                            timeline.Name,
                            cameraCues,
                            segment.Cycle,
                            segmentIndex > 0 || includeStartBoundary && segment.PreviousTime.Raw == 0);
                    }
                    else if (track is ActionCueTrack actionCueTrack)
                    {
                        actionCueTrack.Sample(
                            segment.PreviousTime,
                            segment.CurrentTime,
                            timeline.AuthoringId,
                            timeline.Name,
                            actionCues,
                            segmentIndex > 0 || includeStartBoundary && segment.PreviousTime.Raw == 0);
                    }
                    else if (track is MotionWarpTrack motionWarpTrack && !track.PersistentMuted)
                    {
                        for (int clipIndex = 0; clipIndex < motionWarpTrack.Clips.Count; clipIndex++)
                        {
                            if (motionWarpTrack.Clips[clipIndex] is not MotionWarpClip motionWarpClip ||
                                motionWarpClip.ExecutionDomain != TimelineExecutionDomain.Logic ||
                                segment.CurrentTime <= motionWarpClip.StartTime ||
                                segment.PreviousTime >= motionWarpClip.EndTime)
                                continue;
                            float duration = Mathf.Max(0.0001f, motionWarpClip.DurationTime.ToSingle());
                            float previousNormalized = Mathf.Clamp01(
                                (segment.PreviousTime - motionWarpClip.StartTime).ToSingle() / duration);
                            float normalized = Mathf.Clamp01(
                                (segment.CurrentTime - motionWarpClip.StartTime).ToSingle() / duration);
                            motionWarps.Add(new TimelineRuntimeMotionWarpRequest(
                                motionWarpClip.AuthoringId,
                                motionWarpClip.SourceMotionClipId,
                                TimelineTimeGrid.NearestIndex(segment.CurrentTime, frameRate),
                                segment.Cycle,
                                previousNormalized,
                                normalized,
                                motionWarpClip.TranslationMode,
                                motionWarpClip.TargetOffsetSpace,
                                motionWarpClip.RotationMode,
                                motionWarpClip.RotationMethod,
                                motionWarpClip.TargetPlanarOffset,
                                motionWarpClip.TargetYawOffsetDegrees,
                                motionWarpClip.MaxTotalPositionCorrection,
                                motionWarpClip.MaxTotalYawCorrectionDegrees,
                                motionWarpClip.MaximumYawRateDegreesPerSecond,
                                motionWarpClip.LimitPolicy));
                        }
                    }
                }
            }
            FixedScalar currentTime = currentPosition;
            for (int boundaryIndex = 0; boundaryIndex < (boundaries?.Count ?? 0); boundaryIndex++)
            {
                TimelineRuntimeClipBoundary boundary = boundaries[boundaryIndex];
                if (!TryResolveClip(timeline, boundary.AuthoringId, out Clip boundaryClip) ||
                    boundaryClip.ExecutionDomain != TimelineExecutionDomain.Logic)
                {
                    continue;
                }
                clipSamples.Add(new TimelineRuntimeClipSample(
                    boundary.AuthoringId,
                    boundary.TrackAuthoringId,
                    ResolveContractKind(timeline, boundary.AuthoringId),
                    boundary.Kind == TimelineRuntimeClipBoundaryKind.Enter
                        ? TimelineRuntimeTreeClipEventKind.Enter
                        : TimelineRuntimeTreeClipEventKind.Exit,
                    boundary.Time,
                    boundary.Cycle,
                    boundary.Kind == TimelineRuntimeClipBoundaryKind.Enter ? 0f : 1f));
                traces.Add(new TimelineRuntimeTraceOutput(
                    timeline.AuthoringId,
                    boundary.TrackAuthoringId,
                    boundary.AuthoringId,
                    boundary.Kind == TimelineRuntimeClipBoundaryKind.Enter
                        ? "timeline.clip.enter"
                        : "timeline.clip.exit",
                    TimelineTraceSeverity.Detail,
                    boundary.Kind == TimelineRuntimeClipBoundaryKind.Enter
                        ? "Clip boundary Enter." : "Clip boundary Exit.",
                    executionIdentity,
                    generation,
                    logicTick,
                    boundary.Time,
                    boundary.Cycle));
                if (!TryResolveTreeClip(
                        timeline,
                        boundary.AuthoringId,
                        out TreeClip treeClip))
                    continue;
                if (!TimelineClipExecutionPolicy.FromDomain(treeClip.ExecutionDomain).IsLogic)
                    continue;
                if (!TryGetTreeContract(
                        content,
                        treeClip,
                        out string treeGraphId,
                        out string treeGraphRevision))
                    continue;
                treeClips.Add(new TimelineRuntimeTreeClipRequest(
                    treeClip.AuthoringId,
                    treeClip.Track.AuthoringId,
                    treeGraphId,
                    treeGraphRevision,
                    treeClip.ExecutionPhase,
                    boundary.Kind == TimelineRuntimeClipBoundaryKind.Enter
                        ? TimelineRuntimeTreeClipEventKind.Enter
                        : TimelineRuntimeTreeClipEventKind.Exit,
                    boundary.Time,
                    boundary.Cycle,
                    boundary.Kind == TimelineRuntimeClipBoundaryKind.Enter ? 0f : 1f,
                    generation,
                    CreateTreeClipCallId(
                        executionIdentity,
                        generation,
                        boundary.Cycle,
                        treeClip.AuthoringId)));
            }
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                if (timeline.Tracks[trackIndex] is not ScenePresentationParameterTrack track ||
                    track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Logic)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    if (track.Clips[clipIndex] is not ScenePresentationParameterCurveClip clip ||
                        clip.ExecutionDomain != TimelineExecutionDomain.Logic ||
                        currentTime < clip.StartTime || currentTime > clip.EndTime)
                        continue;
                    float duration = Mathf.Max(0.0001f, clip.DurationTime.ToSingle());
                    float local = Mathf.Clamp01((currentTime - clip.StartTime).ToSingle() / duration);
                    scenePresentation.Add(new TimelineRuntimeScenePresentationSample(
                        clip.AuthoringId,
                        clip.TargetBindingId,
                        clip.ParameterBindingId,
                        clip.ParameterValueKind,
                        clip.ValueCurve.Evaluate(local),
                        local,
                        TimelineTimeGrid.NearestIndex(currentPosition, frameRate),
                        currentCycle,
                        executionIdentity,
                        generation));
                }
            }
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null || track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Logic)
                    continue;
                if (track is CameraStateTrack cameraStateTrack)
                    cameraStateTrack.Sample(
                        currentTime,
                        timeline.AuthoringId,
                        timeline.Name,
                        cameraStates);
                else if (track is CameraResponseTrack cameraResponseTrack)
                    cameraResponseTrack.Sample(
                        currentTime,
                        timeline.AuthoringId,
                        timeline.Name,
                        cameraResponses);
                else if (track is CameraEffectTrack cameraEffectTrack)
                    cameraEffectTrack.Sample(
                        currentTime,
                        timeline.AuthoringId,
                        timeline.Name,
                        cameraResources);
            }
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                if (timeline.Tracks[trackIndex] is not TreeTrack treeTrack || treeTrack.PersistentMuted)
                    continue;
                for (int clipIndex = 0; clipIndex < treeTrack.Clips.Count; clipIndex++)
                {
                    if (treeTrack.Clips[clipIndex] is not TreeClip treeClip ||
                        treeTrack.PersistentMuted ||
                        treeClip.ExecutionDomain != TimelineExecutionDomain.Logic)
                        continue;
                    bool treeDecisionExit =
                        treeClip.ClipExitSource == TimelineClipExitSource.TreeDecision;
                    if (TimelineRuntimePlayback.ContainsClip(exitedTreeDecisionClips, treeClip.AuthoringId))
                        continue;
                    if (currentTime <= treeClip.StartTime)
                        continue;
                    if (!treeDecisionExit && currentTime >= treeClip.EndTime)
                        continue;
                    if (!TryGetTreeContract(
                            content,
                            treeClip,
                            out string treeGraphId,
                            out string treeGraphRevision))
                        continue;
                    float duration = Mathf.Max(0.0001f, treeClip.DurationTime.ToSingle());
                    float local = Mathf.Clamp01((currentTime - treeClip.StartTime).ToSingle() / duration);
                    treeClips.Add(new TimelineRuntimeTreeClipRequest(
                        treeClip.AuthoringId,
                        treeTrack.AuthoringId,
                        treeGraphId,
                        treeGraphRevision,
                        treeClip.ExecutionPhase,
                        TimelineRuntimeTreeClipEventKind.Update,
                        currentPosition,
                        currentCycle,
                        local,
                        generation,
                        CreateTreeClipCallId(
                            executionIdentity,
                            generation,
                            currentCycle,
                            treeClip.AuthoringId)));
                    traces.Add(new TimelineRuntimeTraceOutput(
                        timeline.AuthoringId,
                        treeTrack.AuthoringId,
                        treeClip.AuthoringId,
                        "timeline.treeclip.update",
                        TimelineTraceSeverity.Detail,
                        "TreeClip update candidate.",
                        executionIdentity,
                        generation,
                        logicTick,
                        currentPosition,
                        currentCycle));
                }
            }
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null || track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Logic)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip == null ||
                        clip.ExecutionDomain != TimelineExecutionDomain.Logic ||
                        currentTime <= clip.StartTime || currentTime >= clip.EndTime)
                        continue;
                    float duration = Mathf.Max(0.0001f, clip.DurationTime.ToSingle());
                    clipSamples.Add(new TimelineRuntimeClipSample(
                        clip.AuthoringId,
                        track.AuthoringId,
                        clip.ContractKind,
                        TimelineRuntimeTreeClipEventKind.Update,
                        currentPosition,
                        currentCycle,
                        (currentTime - clip.StartTime).ToSingle() / duration));
                }
            }
            return new TimelineRuntimeEvaluationResult(storage);
        }

        static void AppendMarkerRequests(
            TimelineContentUnit content,
            TimelineExecutionIdentity executionIdentity,
            ulong generation,
            FixedScalar previousPosition,
            int previousCycle,
            FixedScalar currentPosition,
            int currentCycle,
            bool loop,
            bool includeStartBoundary,
            TimelineRuntimeSampleBuffer<TimelineRuntimeMarkerRequest> markers)
        {
            FixedScalar duration = content.Duration;
            if (duration <= FixedScalar.Zero)
                return;
            FixedScalar previousTime = previousPosition;
            FixedScalar currentTime = currentPosition;
            if (currentCycle < previousCycle || currentCycle == previousCycle && currentTime < previousTime)
                throw new InvalidOperationException("Timeline logic cursor moved backward without a generation reset.");
            int firstCycle = loop ? previousCycle : 0;
            int lastCycle = loop ? currentCycle : 0;
            for (int cycle = firstCycle; cycle <= lastCycle; cycle++)
            {
                for (int index = 0; index < content.Markers.Count; index++)
                {
                    TimelineContentMarker marker = content.Markers[index];
                    if (!marker.ExecutionPolicy.IsLogic || marker.TrackMuted)
                        continue;
                    bool initial = includeStartBoundary && cycle == previousCycle && marker.Time == previousTime;
                    bool afterPrevious = cycle > previousCycle || cycle == previousCycle && marker.Time > previousTime;
                    bool beforeCurrent = cycle < currentCycle || cycle == currentCycle && marker.Time <= currentTime;
                    if ((!initial && !afterPrevious) || !beforeCurrent)
                        continue;
                    markers.Add(new TimelineRuntimeMarkerRequest(
                        marker.MarkerId,
                        marker.TrackAuthoringId,
                        marker.GraphId,
                        marker.GraphRevision,
                        marker.Time,
                        cycle,
                        generation,
                        CreateMarkerCallId(executionIdentity, generation, cycle, marker.MarkerId)));
                }
            }
        }

        internal static string CreateMarkerCallId(
            TimelineExecutionIdentity executionIdentity,
            ulong generation,
            int cycle,
            string markerAuthoringId)
        {
            return $"{executionIdentity.OwnerIdentity}:{executionIdentity.CallIdentity}:{executionIdentity.InstanceId}:{generation}:{cycle}:{markerAuthoringId}";
        }

        internal static string CreateTreeClipCallId(
            TimelineExecutionIdentity executionIdentity,
            ulong generation,
            int cycle,
            string clipAuthoringId)
        {
            return $"{executionIdentity.OwnerIdentity}:{executionIdentity.CallIdentity}:{executionIdentity.InstanceId}:{generation}:{cycle}:{clipAuthoringId}";
        }

        internal static void ValidateTreeContracts(
            TimelineData timeline,
            TimelineContentUnit content,
            List<string> errors)
        {
            for (int clipIndex = 0; clipIndex < content.Clips.Count; clipIndex++)
            {
                TimelineContentClip contentClip = content.Clips[clipIndex];
                if (!contentClip.ExecutionPolicy.IsLogic ||
                    !TimelineRuntimeEvaluator.TryResolveTreeClip(
                        timeline,
                        contentClip.AuthoringId,
                        out TreeClip treeClip))
                    continue;
                if (!TimelineRuntimeEvaluator.TryGetTreeContract(
                        content,
                        treeClip,
                        out string treeGraphId,
                        out string treeGraphRevision))
                    errors.Add(
                        $"timeline_tree_contract_missing:{treeClip.AuthoringId}");
            }
        }

        internal static bool TryGetTreeContract(
            TimelineContentUnit content,
            TreeClip treeClip,
            out string treeGraphId,
            out string treeGraphRevision)
        {
            treeGraphId = string.Empty;
            treeGraphRevision = string.Empty;
            return treeClip?.AssetTree is ITimelineTreeGraphAsset treeGraph &&
                   TryGetTreeGraphContract(content, treeGraph, out treeGraphId, out treeGraphRevision);
        }

        internal static bool TryGetTreeGraphContract(
            TimelineContentUnit content,
            ITimelineTreeGraphAsset graph,
            out string treeGraphId,
            out string treeGraphRevision)
        {
            treeGraphId = string.Empty;
            treeGraphRevision = string.Empty;
            if (content == null || graph == null)
                return false;
            var treeGraph = graph;
            string identity = $"tree:{treeGraph.AuthoringId}";
            for (int index = 0; index < content.Dependencies.Count; index++)
            {
                TimelineContentDependency dependency = content.Dependencies[index];
                if (dependency.Kind != "timeline.tree" ||
                    !string.Equals(dependency.Identity, identity, StringComparison.Ordinal))
                    continue;
                treeGraphId = identity;
                treeGraphRevision = dependency.ContentHash;
                return true;
            }
            return false;
        }

        internal static bool TryResolveTreeClip(
            TimelineData timeline,
            string authoringId,
            out TreeClip treeClip)
        {
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    if (track.Clips[clipIndex] is TreeClip candidate &&
                        string.Equals(candidate.AuthoringId, authoringId, StringComparison.Ordinal))
                    {
                        treeClip = candidate;
                        return true;
                    }
                }
            }
            treeClip = null;
            return false;
        }

        static bool TryResolveClip(
            TimelineData timeline,
            string authoringId,
            out Clip clip)
        {
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null || track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Logic)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip candidate = track.Clips[clipIndex];
                    if (candidate != null &&
                        string.Equals(candidate.AuthoringId, authoringId, StringComparison.Ordinal))
                    {
                        clip = candidate;
                        return true;
                    }
                }
            }
            clip = null;
            return false;
        }

        static string ResolveContractKind(TimelineData timeline, string authoringId)
        {
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip != null && string.Equals(clip.AuthoringId, authoringId, StringComparison.Ordinal))
                        return clip.ContractKind;
                }
            }
            return string.Empty;
        }


    }

    internal static class TimelineRuntimePresentationEvaluator
    {
        public static TimelineRuntimePresentationOperations Evaluate(
            TimelineRuntimePlayback playback,
            FixedScalar previousPosition,
            int previousCycle,
            FixedScalar currentPosition,
            int currentCycle,
            bool loop,
            bool includeStartBoundary,
            TimelineRuntimePresentationBuffer buffer)
        {
            if (playback == null)
                throw new ArgumentNullException(nameof(playback));
            TimelineData timeline = playback.SourceTimeline;
            int frameRate = Math.Max(1, playback.Content.FrameRate);
            FixedScalar contentDuration = playback.Content.Duration;
            var animations = buffer.Animations;
            var cameraStates = buffer.CameraStates;
            var cameraCues = buffer.CameraCues;
            var cameraResponses = buffer.CameraResponses;
            var cameraResources = buffer.CameraResources;
            var scenePresentation = buffer.ScenePresentation;
            var segments = new TimelineRuntimeEvaluationSegments(
                previousPosition,
                previousCycle,
                currentPosition,
                currentCycle,
                contentDuration,
                loop);
            for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                TimelineRuntimeEvaluationSegment segment = segments[segmentIndex];
                for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
                {
                    Track track = timeline.Tracks[trackIndex];
                    if (track == null || track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Presentation)
                        continue;
                    if (track is CameraCueTrack cameraCueTrack)
                    {
                        cameraCueTrack.Sample(
                            segment.PreviousTime,
                            segment.CurrentTime,
                            timeline.AuthoringId,
                            timeline.Name,
                            cameraCues,
                            segment.Cycle,
                            segmentIndex > 0 || includeStartBoundary && segment.PreviousTime.Raw == 0);
                    }
                }
            }
            FixedScalar currentTime = currentPosition;
            int sampledFrame = TimelineTimeGrid.NearestIndex(currentPosition, frameRate);
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null || track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Presentation)
                    continue;
                if (track is AnimationTrack animationTrack)
                    animationTrack.Sample(
                        currentTime,
                        currentTime,
                        trackIndex,
                        timeline.AuthoringId,
                        timeline.Name,
                        animations,
                        loop,
                        currentCycle);
                else if (track is CameraStateTrack cameraStateTrack)
                    cameraStateTrack.Sample(
                        currentTime,
                        timeline.AuthoringId,
                        timeline.Name,
                        cameraStates);
                else if (track is CameraResponseTrack cameraResponseTrack)
                    cameraResponseTrack.Sample(
                        currentTime,
                        timeline.AuthoringId,
                        timeline.Name,
                        cameraResponses);
                else if (track is CameraEffectTrack cameraEffectTrack)
                    cameraEffectTrack.Sample(
                        currentTime,
                        timeline.AuthoringId,
                        timeline.Name,
                        cameraResources);
                else if (track is ScenePresentationParameterTrack sceneTrack && !track.PersistentMuted)
                {
                    for (int clipIndex = 0; clipIndex < sceneTrack.Clips.Count; clipIndex++)
                    {
                        if (sceneTrack.Clips[clipIndex] is not ScenePresentationParameterCurveClip clip ||
                            clip.ExecutionDomain != TimelineExecutionDomain.Presentation ||
                            currentTime < clip.StartTime || currentTime > clip.EndTime)
                        {
                            continue;
                        }
                        float duration = Mathf.Max(0.0001f, clip.DurationTime.ToSingle());
                        float local = Mathf.Clamp01((currentTime - clip.StartTime).ToSingle() / duration);
                        scenePresentation.Add(new TimelineRuntimeScenePresentationSample(
                            clip.AuthoringId,
                            clip.TargetBindingId,
                            clip.ParameterBindingId,
                            clip.ParameterValueKind,
                            clip.ValueCurve.Evaluate(local),
                            local,
                            sampledFrame,
                            currentCycle,
                            playback.ExecutionIdentity,
                            playback.Generation));
                    }
                }
            }
            return new TimelineRuntimePresentationOperations(buffer);
        }


    }

    readonly struct TimelineRuntimeEvaluationSegments
    {
        public const int MaximumCycleAdvance = 4096;
        readonly FixedScalar m_Previous;
        readonly FixedScalar m_Current;
        readonly FixedScalar m_Duration;
        readonly int m_PreviousCycle;
        readonly int m_CurrentCycle;

        public TimelineRuntimeEvaluationSegments(FixedScalar previous, int previousCycle,
            FixedScalar current, int currentCycle, FixedScalar duration, bool loop)
        {
            if (currentCycle < previousCycle || currentCycle - previousCycle > MaximumCycleAdvance ||
                currentCycle == previousCycle && current < previous)
                throw new InvalidOperationException("Timeline interval has an invalid traversal range.");
            m_Previous = previous;
            m_Current = current;
            m_Duration = duration;
            m_PreviousCycle = previousCycle;
            m_CurrentCycle = currentCycle;
            Count = loop && duration > FixedScalar.Zero ? currentCycle - previousCycle + 1 : 1;
        }

        public int Count { get; }
        public TimelineRuntimeEvaluationSegment this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return new TimelineRuntimeEvaluationSegment(
                    index == 0 ? m_Previous : FixedScalar.Zero,
                    index == Count - 1 ? m_Current : m_Duration,
                    Count == 1 ? m_CurrentCycle : m_PreviousCycle + index);
            }
        }
    }

    readonly struct TimelineRuntimeEvaluationSegment
    {
        public TimelineRuntimeEvaluationSegment(FixedScalar previousTime, FixedScalar currentTime, int cycle)
        {
            PreviousTime = previousTime;
            CurrentTime = currentTime;
            Cycle = cycle;
        }

        public FixedScalar PreviousTime { get; }
        public FixedScalar CurrentTime { get; }
        public int Cycle { get; }
    }
}
