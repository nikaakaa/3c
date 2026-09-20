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
        public TimelineRuntimeAdvanceRequest(ulong logicTick, FixedScalar previousTime, FixedScalar targetTime, int timeCarry)
        {
            if (logicTick == 0)
                throw new ArgumentOutOfRangeException(nameof(logicTick));
            if (previousTime < FixedScalar.Zero || targetTime < previousTime)
                throw new ArgumentOutOfRangeException(nameof(targetTime));
            LogicTick = logicTick;
            PreviousTime = previousTime;
            TargetTime = targetTime;
            TimeCarry = timeCarry;
        }

        public ulong LogicTick { get; }
        public FixedScalar PreviousTime { get; }
        public FixedScalar TargetTime { get; }
        public int TimeCarry { get; }
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
            Evaluation = evaluation ?? throw new ArgumentNullException(nameof(evaluation));
            Completes = completes;
        }

        internal TimelineRuntimePlayback Owner { get; }
        internal TimelineRuntimeAdvanceRequest Request { get; }
        internal int TimeCarry { get; }
        public ulong Generation { get; }
        public ulong LogicTick => Request.LogicTick;
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
        readonly List<string> m_ActiveClipIds = new List<string>();
        readonly List<string> m_PendingTreeClipExits = new List<string>();
        readonly List<string> m_ExitedTreeDecisionClips = new List<string>();
        readonly List<string> m_AdvanceInjectedTreeClipExits = new List<string>();
        readonly List<string> m_AdvanceProducedTreeClipExits = new List<string>();
        readonly ReadOnlyCollection<string> m_ActiveClipIdsView;
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
            m_ActiveClipIdsView = new ReadOnlyCollection<string>(m_ActiveClipIds);
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
        public int Cycle => m_Cycle;
        public string SectionId => m_SectionId;
        public IReadOnlyList<string> ActiveClipIds => m_ActiveClipIdsView;
        public IReadOnlyList<TimelineRuntimeTreeClipAssociation> ActiveTreeClipAssociations => CreateActiveTreeClipAssociations(m_CursorTime, m_Cycle);
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
            FixedScalar duration = Content.Duration;
            bool loop = PlaybackMode == TimelinePlaybackMode.Loop;
            if (request.PreviousTime != m_CursorTime)
                throw new InvalidOperationException("Timeline interval does not begin at the committed cursor.");
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

            var activeClipIds = new List<string>();
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
            var evaluationExitedTreeDecisionClips = new List<string>(m_ExitedTreeDecisionClips);
            for (int index = 0; index < m_PendingTreeClipExits.Count; index++)
            {
                if (!evaluationExitedTreeDecisionClips.Contains(m_PendingTreeClipExits[index]))
                    evaluationExitedTreeDecisionClips.Add(m_PendingTreeClipExits[index]);
            }
            TimelineRuntimeEvaluationResult evaluation = TimelineRuntimeEvaluator.Evaluate(
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
            m_CursorTime = advance.Time;
            m_Cycle = advance.Cycle;
            m_TimeCarry = advance.TimeCarry;
            m_LastCommittedLogicTick = advance.LogicTick;
            m_SectionId = advance.SectionId;
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
            State = TimelineRuntimePlaybackState.Stopped;
            return true;
        }

        public void Dispose()
        {
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
            string sectionId,
            IReadOnlyList<string> activeClipIds,
            IReadOnlyList<TimelineRuntimeTreeClipAssociation> activeTreeClipAssociations,
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
                    exitedTreeDecisionClips,
                    pendingTreeDecisionClips))
                return false;
            if (!ValidateRestoredTreeClipAssociations(
                    cursorTime,
                    cycle,
                    activeTreeClipAssociations,
                    exitedTreeDecisionClips,
                    pendingTreeDecisionClips))
                return false;
            m_CursorTime = cursorTime;
            m_Cycle = cycle;
            m_SectionId = sectionId ?? string.Empty;
            m_InitialBoundaryPending = initialBoundaryPending;
            m_TimeCarry = timeCarry;
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

        bool ValidateRestoredTreeClipAssociations(
            FixedScalar cursorTime,
            int cycle,
            IReadOnlyList<TimelineRuntimeTreeClipAssociation> associations,
            IReadOnlyList<string> exitedTreeDecisionClips,
            IReadOnlyList<string> pendingTreeDecisionClips)
        {
            List<TimelineRuntimeTreeClipAssociation> expected = CreateActiveTreeClipAssociations(
                cursorTime,
                cycle,
                exitedTreeDecisionClips,
                pendingTreeDecisionClips);
            if ((associations?.Count ?? 0) != expected.Count)
                return false;
            for (int index = 0; index < expected.Count; index++)
            {
                TimelineRuntimeTreeClipAssociation actual = associations[index];
                TimelineRuntimeTreeClipAssociation required = expected[index];
                if (!string.Equals(actual.CallId, required.CallId, StringComparison.Ordinal) ||
                    !string.Equals(actual.ClipAuthoringId, required.ClipAuthoringId, StringComparison.Ordinal) ||
                    !string.Equals(actual.TrackAuthoringId, required.TrackAuthoringId, StringComparison.Ordinal) ||
                    !string.Equals(actual.TreeGraphId, required.TreeGraphId, StringComparison.Ordinal) ||
                    !string.Equals(actual.TreeGraphRevision, required.TreeGraphRevision, StringComparison.Ordinal) ||
                    actual.Phase != required.Phase ||
                    actual.Cycle != required.Cycle)
                    return false;
            }
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
            IReadOnlyList<string> exitedTreeDecisionClips,
            IReadOnlyList<string> pendingTreeDecisionClips)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < (activeClipIds?.Count ?? 0); index++)
            {
                string clipId = activeClipIds[index] ?? string.Empty;
                if (!seen.Add(clipId))
                    return false;
                bool found = false;
                for (int clipIndex = 0; clipIndex < Content.Clips.Count; clipIndex++)
                {
                    TimelineContentClip clip = Content.Clips[clipIndex];
                    if (!string.Equals(clip.AuthoringId, clipId, StringComparison.Ordinal))
                        continue;
                    found = true;
                    bool treeDecision = clip.ExitSource == TimelineClipExitSource.TreeDecision;
                    bool exited = treeDecision &&
                                  (exitedTreeDecisionClips.Contains(clip.AuthoringId) ||
                                   pendingTreeDecisionClips.Contains(clip.AuthoringId));
                    if (clip.TrackMuted || !clip.ExecutionPolicy.IsLogic ||
                        clip.StartTime > cursorTime || exited ||
                        !treeDecision && cursorTime >= clip.EndTime)
                        return false;
                    break;
                }
                if (!found)
                    return false;
            }
            if (state == TimelineRuntimePlaybackState.Prepared ||
                state == TimelineRuntimePlaybackState.Stopping ||
                state == TimelineRuntimePlaybackState.Completed ||
                state == TimelineRuntimePlaybackState.Stopped)
                return (activeClipIds?.Count ?? 0) == 0;
            return true;
        }

        void RequirePendingAdvance(TimelineRuntimeAdvanceResult advance)
        {
            if (advance == null || !ReferenceEquals(advance.Owner, this) || !ReferenceEquals(m_PendingAdvance, advance))
                throw new InvalidOperationException("Timeline Advance result does not belong to the active playback.");
        }

        List<TimelineRuntimeTreeClipAssociation> CreateActiveTreeClipAssociations(
            FixedScalar cursorTime,
            int cycle,
            IReadOnlyList<string> exitedTreeDecisionClips = null,
            IReadOnlyList<string> pendingTreeDecisionClips = null)
        {
            exitedTreeDecisionClips ??= m_ExitedTreeDecisionClips;
            pendingTreeDecisionClips ??= m_PendingTreeClipExits;
            var associations = new List<TimelineRuntimeTreeClipAssociation>();
            for (int index = 0; index < Content.Clips.Count; index++)
            {
                TimelineContentClip clip = Content.Clips[index];
                bool treeDecision = clip.ExitSource == TimelineClipExitSource.TreeDecision;
                bool exited = treeDecision &&
                              (exitedTreeDecisionClips.Contains(clip.AuthoringId) ||
                               pendingTreeDecisionClips.Contains(clip.AuthoringId));
                if (clip.TrackMuted || !clip.ExecutionPolicy.IsLogic ||
                    clip.StartTime > cursorTime || exited ||
                    !treeDecision && cursorTime >= clip.EndTime)
                    continue;
                if (!TimelineRuntimeEvaluator.TryResolveTreeClip(SourceTimeline, clip.AuthoringId, out TreeClip treeClip) ||
                    !TimelineRuntimeEvaluator.TryGetTreeContract(Content, treeClip, out string treeGraphId, out string treeGraphRevision))
                    continue;
                associations.Add(new TimelineRuntimeTreeClipAssociation(
                    TimelineRuntimeEvaluator.CreateTreeClipCallId(
                        ExecutionIdentity, Generation, cycle, clip.AuthoringId),
                    clip.AuthoringId,
                    clip.TrackAuthoringId,
                    treeGraphId,
                    treeGraphRevision,
                    treeClip.ExecutionPhase,
                    cycle));
            }
            return associations;
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
            var result = new List<TimelineRuntimeClipBoundary>();
            if (duration <= FixedScalar.Zero)
                return result;
            FixedScalar previousAbsolute = duration * FixedScalar.FromInt64(previousCycle) + previousTime;
            FixedScalar nextAbsolute = duration * FixedScalar.FromInt64(nextCycle) + nextTime;
            if (nextAbsolute <= previousAbsolute)
                return result;
            if (loop && nextCycle - previousCycle > 4096)
                throw new InvalidOperationException("Timeline playback crossed more than 4096 cycles in one Advance.");

            int firstCycle = loop ? previousCycle : 0;
            int lastCycle = loop ? nextCycle : 0;
            for (int clipIndex = 0; clipIndex < Content.Clips.Count; clipIndex++)
            {
                TimelineContentClip clip = Content.Clips[clipIndex];
                if (clip.TrackMuted || !clip.ExecutionPolicy.IsLogic)
                    continue;
                bool treeDecision = clip.ExitSource == TimelineClipExitSource.TreeDecision;
                if (treeDecision && System.Linq.Enumerable.Contains(exitedTreeDecisionClips, clip.AuthoringId))
                    continue;
                if (treeDecision && pendingTreeClipExits.Contains(clip.AuthoringId))
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
            result.Sort((left, right) =>
            {
                int position = (duration * FixedScalar.FromInt64(left.Cycle) + left.Time).CompareTo(
                    duration * FixedScalar.FromInt64(right.Cycle) + right.Time);
                if (position != 0)
                    return position;
                int kind = left.Kind.CompareTo(right.Kind);
                return kind != 0
                    ? kind
                    : string.CompareOrdinal(left.AuthoringId, right.AuthoringId);
            });
            return result;
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

    public readonly struct TimelineRuntimeTreeClipAssociation
    {
        public TimelineRuntimeTreeClipAssociation(
            string callId,
            string clipAuthoringId,
            string trackAuthoringId,
            string treeGraphId,
            string treeGraphRevision,
            TimelineTreeExecutionPhase phase,
            int cycle)
        {
            CallId = string.IsNullOrWhiteSpace(callId)
                ? throw new ArgumentException("TreeClip call identity is required.", nameof(callId))
                : callId.Trim();
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
            Cycle = cycle;
        }

        public string CallId { get; }
        public string ClipAuthoringId { get; }
        public string TrackAuthoringId { get; }
        public string TreeGraphId { get; }
        public string TreeGraphRevision { get; }
        public TimelineTreeExecutionPhase Phase { get; }
        public int Cycle { get; }
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

    public sealed class TimelineRuntimeEvaluationResult
    {
        internal TimelineRuntimeEvaluationResult(
            IReadOnlyList<TimelineAnimationContribution> animations,
            IReadOnlyList<TimelineMotionCurveContribution> motions,
            IReadOnlyList<TimelineCameraStateSample> cameraStates,
            IReadOnlyList<TimelineCameraCueSample> cameraCues,
            IReadOnlyList<TimelineCameraResponseSample> cameraResponses,
            IReadOnlyList<TimelineCameraResourceSample> cameraResources,
            IReadOnlyList<TimelineActionCueSample> actionCues,
            IReadOnlyList<TimelineRuntimeTreeClipRequest> treeClips,
            IReadOnlyList<TimelineRuntimeMarkerRequest> markers,
            IReadOnlyList<TimelineRuntimeScenePresentationSample> scenePresentation,
            IReadOnlyList<TimelineRuntimeMotionWarpRequest> motionWarps,
            IReadOnlyList<TimelineRuntimeClipSample> clipSamples,
            IReadOnlyList<TimelineRuntimeTraceOutput> traces)
        {
            AnimationContributions = Copy(animations);
            MotionContributions = Copy(motions);
            CameraStates = Copy(cameraStates);
            CameraCues = Copy(cameraCues);
            CameraResponses = Copy(cameraResponses);
            CameraResources = Copy(cameraResources);
            ActionCues = Copy(actionCues);
            TreeClips = Copy(treeClips);
            Markers = Copy(markers);
            ScenePresentation = Copy(scenePresentation);
            MotionWarps = Copy(motionWarps);
            ClipSamples = Copy(clipSamples);
            Traces = Copy(traces);
            LogicOperations = new TimelineRuntimeLogicOperations(
                MotionContributions,
                MotionWarps,
                ActionCues,
                TreeClips,
                Markers,
                ClipSamples,
                Traces);
        }

        public IReadOnlyList<TimelineAnimationContribution> AnimationContributions { get; }
        public IReadOnlyList<TimelineMotionCurveContribution> MotionContributions { get; }
        public IReadOnlyList<TimelineCameraStateSample> CameraStates { get; }
        public IReadOnlyList<TimelineCameraCueSample> CameraCues { get; }
        public IReadOnlyList<TimelineCameraResponseSample> CameraResponses { get; }
        public IReadOnlyList<TimelineCameraResourceSample> CameraResources { get; }
        public IReadOnlyList<TimelineActionCueSample> ActionCues { get; }
        public IReadOnlyList<TimelineRuntimeTreeClipRequest> TreeClips { get; }
        public IReadOnlyList<TimelineRuntimeMarkerRequest> Markers { get; }
        public IReadOnlyList<TimelineRuntimeScenePresentationSample> ScenePresentation { get; }
        public IReadOnlyList<TimelineRuntimeMotionWarpRequest> MotionWarps { get; }
        public IReadOnlyList<TimelineRuntimeClipSample> ClipSamples { get; }
        public IReadOnlyList<TimelineRuntimeTraceOutput> Traces { get; }
        public TimelineRuntimeLogicOperations LogicOperations { get; }

        static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> values)
        {
            return new ReadOnlyCollection<T>(
                new List<T>(values ?? Array.Empty<T>()));
        }
    }

    public sealed class TimelineRuntimeLogicOperations
    {
        internal TimelineRuntimeLogicOperations(
            IReadOnlyList<TimelineMotionCurveContribution> motionContributions,
            IReadOnlyList<TimelineRuntimeMotionWarpRequest> motionWarps,
            IReadOnlyList<TimelineActionCueSample> actionCues,
            IReadOnlyList<TimelineRuntimeTreeClipRequest> treeClips,
            IReadOnlyList<TimelineRuntimeMarkerRequest> markers,
            IReadOnlyList<TimelineRuntimeClipSample> clipSamples,
            IReadOnlyList<TimelineRuntimeTraceOutput> traces)
        {
            MotionContributions = motionContributions;
            MotionWarps = motionWarps;
            ActionCues = actionCues;
            TreeClips = treeClips;
            Markers = markers;
            ClipSamples = clipSamples;
            Traces = traces;
        }

        public IReadOnlyList<TimelineMotionCurveContribution> MotionContributions { get; }
        public IReadOnlyList<TimelineRuntimeMotionWarpRequest> MotionWarps { get; }
        public IReadOnlyList<TimelineActionCueSample> ActionCues { get; }
        public IReadOnlyList<TimelineRuntimeTreeClipRequest> TreeClips { get; }
        public IReadOnlyList<TimelineRuntimeMarkerRequest> Markers { get; }
        public IReadOnlyList<TimelineRuntimeClipSample> ClipSamples { get; }
        public IReadOnlyList<TimelineRuntimeTraceOutput> Traces { get; }
    }

    public sealed class TimelineRuntimePresentationOperations
    {
        internal TimelineRuntimePresentationOperations(
            IReadOnlyList<TimelineAnimationContribution> animationContributions,
            IReadOnlyList<TimelineCameraStateSample> cameraStates,
            IReadOnlyList<TimelineCameraCueSample> cameraCues,
            IReadOnlyList<TimelineCameraResponseSample> cameraResponses,
            IReadOnlyList<TimelineCameraResourceSample> cameraResources,
            IReadOnlyList<TimelineRuntimeScenePresentationSample> scenePresentation)
        {
            AnimationContributions = Copy(animationContributions);
            CameraStates = Copy(cameraStates);
            CameraCues = Copy(cameraCues);
            CameraResponses = Copy(cameraResponses);
            CameraResources = Copy(cameraResources);
            ScenePresentation = Copy(scenePresentation);
        }

        public IReadOnlyList<TimelineAnimationContribution> AnimationContributions { get; }
        public IReadOnlyList<TimelineCameraStateSample> CameraStates { get; }
        public IReadOnlyList<TimelineCameraCueSample> CameraCues { get; }
        public IReadOnlyList<TimelineCameraResponseSample> CameraResponses { get; }
        public IReadOnlyList<TimelineCameraResourceSample> CameraResources { get; }
        public IReadOnlyList<TimelineRuntimeScenePresentationSample> ScenePresentation { get; }
        static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> values)
        {
            return new ReadOnlyCollection<T>(new List<T>(values ?? Array.Empty<T>()));
        }
    }

    public readonly struct TimelineRuntimePresentationFrame
    {
        internal TimelineRuntimePresentationFrame(
            TimelineRuntimePlayback playback,
            ulong presentationFrame,
            float presentationDeltaSeconds,
            float interpolationAlpha,
            TimelineRuntimePresentationOperations operations,
            IReadOnlyList<TimelineRuntimePresentationEvent> events)
        {
            if (playback == null || !playback.Handle.IsValid || playback.Generation == 0 || presentationFrame == 0 ||
                !float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f ||
                !float.IsFinite(interpolationAlpha) || interpolationAlpha < 0f || interpolationAlpha > 1f ||
                operations == null)
                throw new ArgumentException("Timeline presentation frame is invalid.");
            Handle = playback.Handle;
            Generation = playback.Generation;
            LogicTick = playback.LastCommittedLogicTick;
            PresentationFrame = presentationFrame;
            PresentationDeltaSeconds = presentationDeltaSeconds;
            InterpolationAlpha = interpolationAlpha;
            ExecutionIdentity = playback.ExecutionIdentity;
            Operations = operations;
            Events = new ReadOnlyCollection<TimelineRuntimePresentationEvent>(
                new List<TimelineRuntimePresentationEvent>(events ?? Array.Empty<TimelineRuntimePresentationEvent>()));
        }

        public TimelineRuntimePlaybackHandle Handle { get; }
        public ulong Generation { get; }
        public ulong LogicTick { get; }
        public ulong PresentationFrame { get; }
        public float PresentationDeltaSeconds { get; }
        public float InterpolationAlpha { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public TimelineRuntimePresentationOperations Operations { get; }
        public IReadOnlyList<TimelineRuntimePresentationEvent> Events { get; }
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
            var animations = new List<TimelineAnimationContribution>();
            var motions = new List<TimelineMotionCurveContribution>();
            var cameraStates = new List<TimelineCameraStateSample>();
            var cameraCues = new List<TimelineCameraCueSample>();
            var cameraResponses = new List<TimelineCameraResponseSample>();
            var cameraResources = new List<TimelineCameraResourceSample>();
            var actionCues = new List<TimelineActionCueSample>();
            var treeClips = new List<TimelineRuntimeTreeClipRequest>();
            var markers = new List<TimelineRuntimeMarkerRequest>();
            var scenePresentation = new List<TimelineRuntimeScenePresentationSample>();
            var motionWarps = new List<TimelineRuntimeMotionWarpRequest>();
            var clipSamples = new List<TimelineRuntimeClipSample>();
            var traces = new List<TimelineRuntimeTraceOutput>();
            List<TimelineRuntimeEvaluationSegment> segments = BuildSegments(
                previousPosition,
                previousCycle,
                currentPosition,
                currentCycle,
                content.Duration,
                loop,
                frameRate);
            Func<Clip, bool> logicClipFilter = clip =>
                HasProjection(content, clip, TimelineExecutionDomain.Logic);
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
                    if (track == null)
                        continue;
                    if (track is AnimationTrack animationTrack)
                    {
                        animationTrack.Sample(
                            segment.PreviousTime.ToSingle(),
                            segment.CurrentTime.ToSingle(),
                            trackIndex,
                            timeline.AuthoringId,
                            timeline.Name,
                            animations,
                            loop,
                            segment.Cycle,
                            logicClipFilter);
                    }
                    else if (track is MotionCurveTrack motionTrack)
                    {
                        motionTrack.Sample(
                            segment.PreviousTime.ToSingle(),
                            segment.CurrentTime.ToSingle(),
                            timeline.AuthoringId,
                            timeline.Name,
                            motions,
                            logicClipFilter);
                    }
                    else if (track is CameraCueTrack cameraCueTrack)
                    {
                        cameraCueTrack.Sample(
                            segment.PreviousTime,
                            segment.CurrentTime,
                            timeline.AuthoringId,
                            timeline.Name,
                            cameraCues,
                            segmentIndex > 0 || includeStartBoundary && segment.PreviousTime.Raw == 0,
                            logicClipFilter);
                    }
                    else if (track is ActionCueTrack actionCueTrack)
                    {
                        actionCueTrack.Sample(
                            segment.PreviousTime,
                            segment.CurrentTime,
                            timeline.AuthoringId,
                            timeline.Name,
                            actionCues,
                            segmentIndex > 0 || includeStartBoundary && segment.PreviousTime.Raw == 0,
                            logicClipFilter);
                    }
                    else if (track is MotionWarpTrack motionWarpTrack && !track.PersistentMuted)
                    {
                        for (int clipIndex = 0; clipIndex < motionWarpTrack.Clips.Count; clipIndex++)
                        {
                            if (motionWarpTrack.Clips[clipIndex] is not MotionWarpClip motionWarpClip ||
                                !logicClipFilter(motionWarpClip) ||
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
            float currentTime = currentPosition.ToSingle();
            for (int boundaryIndex = 0; boundaryIndex < (boundaries?.Count ?? 0); boundaryIndex++)
            {
                TimelineRuntimeClipBoundary boundary = boundaries[boundaryIndex];
                if (!TryResolveClip(timeline, boundary.AuthoringId, out Clip boundaryClip) ||
                    !logicClipFilter(boundaryClip))
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
                    $"Clip boundary {boundary.Kind}.",
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
                if (timeline.Tracks[trackIndex] is not ScenePresentationParameterTrack track)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    if (track.Clips[clipIndex] is not ScenePresentationParameterCurveClip clip ||
                        !logicClipFilter(clip) ||
                        currentTime < clip.StartTime.ToSingle() || currentTime > clip.EndTime.ToSingle())
                        continue;
                    float duration = Mathf.Max(0.0001f, clip.DurationTime.ToSingle());
                    float local = Mathf.Clamp01((currentTime - clip.StartTime.ToSingle()) / duration);
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
                if (track == null)
                    continue;
                if (track is CameraStateTrack cameraStateTrack)
                    cameraStateTrack.Sample(
                        currentTime,
                        timeline.AuthoringId,
                        timeline.Name,
                        cameraStates,
                        logicClipFilter);
                else if (track is CameraResponseTrack cameraResponseTrack)
                    cameraResponseTrack.Sample(
                        currentTime,
                        timeline.AuthoringId,
                        timeline.Name,
                        cameraResponses,
                        logicClipFilter);
                else if (track is CameraEffectTrack cameraEffectTrack)
                    cameraEffectTrack.Sample(
                        currentTime,
                        timeline.AuthoringId,
                        timeline.Name,
                        cameraResources,
                        logicClipFilter);
            }
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                if (timeline.Tracks[trackIndex] is not TreeTrack treeTrack || treeTrack.PersistentMuted)
                    continue;
                for (int clipIndex = 0; clipIndex < treeTrack.Clips.Count; clipIndex++)
                {
                    if (treeTrack.Clips[clipIndex] is not TreeClip treeClip ||
                        treeTrack.PersistentMuted ||
                        !logicClipFilter(treeClip))
                        continue;
                    bool treeDecisionExit =
                        treeClip.ClipExitSource == TimelineClipExitSource.TreeDecision;
                    if (System.Linq.Enumerable.Contains(exitedTreeDecisionClips, treeClip.AuthoringId))
                        continue;
                    if (currentTime <= treeClip.StartTime.ToSingle())
                        continue;
                    if (!treeDecisionExit && currentTime >= treeClip.EndTime.ToSingle())
                        continue;
                    if (!TryGetTreeContract(
                            content,
                            treeClip,
                            out string treeGraphId,
                            out string treeGraphRevision))
                        continue;
                    float duration = Mathf.Max(0.0001f, treeClip.DurationTime.ToSingle());
                    float local = Mathf.Clamp01((currentTime - treeClip.StartTime.ToSingle()) / duration);
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
                if (track == null || track.PersistentMuted)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip == null ||
                        !logicClipFilter(clip) ||
                        currentTime <= clip.StartTime.ToSingle() || currentTime >= clip.EndTime.ToSingle())
                        continue;
                    float duration = Mathf.Max(0.0001f, clip.DurationTime.ToSingle());
                    clipSamples.Add(new TimelineRuntimeClipSample(
                        clip.AuthoringId,
                        track.AuthoringId,
                        clip.ContractKind,
                        TimelineRuntimeTreeClipEventKind.Update,
                        currentPosition,
                        currentCycle,
                        (currentTime - clip.StartTime.ToSingle()) / duration));
                }
            }
            return new TimelineRuntimeEvaluationResult(
                animations,
                motions,
                cameraStates,
                cameraCues,
                cameraResponses,
                cameraResources,
                actionCues,
                treeClips,
                markers,
                scenePresentation,
                motionWarps,
                clipSamples,
                traces);
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
            List<TimelineRuntimeMarkerRequest> markers)
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
            for (int index = 0; index < content.Markers.Count; index++)
            {
                TimelineContentMarker contentMarker = content.Markers[index];
                if (!contentMarker.ExecutionPolicy.IsLogic || contentMarker.TrackMuted)
                    continue;
                for (int cycle = firstCycle; cycle <= lastCycle; cycle++)
                {
                    bool initial = includeStartBoundary && cycle == previousCycle && contentMarker.Time == previousTime;
                    bool afterPrevious = cycle > previousCycle || cycle == previousCycle && contentMarker.Time > previousTime;
                    bool beforeCurrent = cycle < currentCycle || cycle == currentCycle && contentMarker.Time <= currentTime;
                    if ((!initial && !afterPrevious) || !beforeCurrent)
                        continue;
                    Track track = FindTrack(content, contentMarker.TrackAuthoringId);
                    TimelineMarker marker = track?.Markers.FirstOrDefault(candidate =>
                        candidate != null && candidate.AuthoringId == contentMarker.MarkerId);
                    if (marker?.Graph is not ITimelineTreeGraphAsset graph ||
                        !TryGetTreeGraphContract(content, graph, out string graphId, out string graphRevision))
                        throw new InvalidOperationException($"Timeline Marker '{contentMarker.MarkerId}' graph contract is missing.");
                    markers.Add(new TimelineRuntimeMarkerRequest(
                        contentMarker.MarkerId,
                        contentMarker.TrackAuthoringId,
                        graphId,
                        graphRevision,
                        contentMarker.Time,
                        cycle,
                        generation,
                        CreateMarkerCallId(executionIdentity, generation, cycle, contentMarker.MarkerId)));
                }
            }
            markers.Sort((left, right) =>
            {
                int cycleOrder = left.Cycle.CompareTo(right.Cycle);
                if (cycleOrder != 0)
                    return cycleOrder;
                int frameOrder = left.Time.CompareTo(right.Time);
                return frameOrder != 0 ? frameOrder : string.CompareOrdinal(left.MarkerAuthoringId, right.MarkerAuthoringId);
            });
        }

        static Track FindTrack(TimelineContentUnit content, string authoringId)
        {
            for (int index = 0; index < content.Tracks.Count; index++)
            {
                if (string.Equals(content.Tracks[index].AuthoringId, authoringId, StringComparison.Ordinal))
                    return content.SourceTracks[index];
            }
            return null;
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

        internal static bool HasProjection(
            TimelineContentUnit content,
            Clip clip,
            TimelineExecutionDomain projectionDomain)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            if (clip == null)
                return false;
            for (int index = 0; index < content.Clips.Count; index++)
            {
                TimelineContentClip contentClip = content.Clips[index];
                if (!string.Equals(contentClip.AuthoringId, clip.AuthoringId, StringComparison.Ordinal))
                    continue;
                return projectionDomain switch
                {
                    TimelineExecutionDomain.Logic => contentClip.ExecutionPolicy.IsLogic,
                    TimelineExecutionDomain.Presentation => contentClip.ExecutionPolicy.IsPresentation,
                    _ => throw new ArgumentOutOfRangeException(nameof(projectionDomain))
                };
            }
            throw new InvalidOperationException(
                $"Timeline content does not contain clip '{clip.AuthoringId}'.");
        }

        static bool TryResolveClip(
            TimelineData timeline,
            string authoringId,
            out Clip clip)
        {
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null)
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

        static List<TimelineRuntimeEvaluationSegment> BuildSegments(
            FixedScalar previousPosition,
            int previousCycle,
            FixedScalar currentPosition,
            int currentCycle,
            FixedScalar duration,
            bool loop,
            int frameRate)
        {
            var result = new List<TimelineRuntimeEvaluationSegment>();
            if (!loop || currentCycle == previousCycle || duration <= FixedScalar.Zero)
            {
                result.Add(new TimelineRuntimeEvaluationSegment(
                    previousPosition,
                    currentPosition,
                    currentCycle));
                return result;
            }
            if (currentCycle - previousCycle > 4096)
                throw new InvalidOperationException("Timeline evaluation crossed more than 4096 cycles in one Advance.");
            result.Add(new TimelineRuntimeEvaluationSegment(
                previousPosition,
                duration,
                previousCycle));
            for (int cycle = previousCycle + 1; cycle < currentCycle; cycle++)
                result.Add(new TimelineRuntimeEvaluationSegment(
                    FixedScalar.Zero,
                    duration,
                    cycle));
            result.Add(new TimelineRuntimeEvaluationSegment(
                FixedScalar.Zero,
                currentPosition,
                currentCycle));
            return result;
        }
    }

    internal static class TimelineRuntimePresentationEvaluator
    {
        public static TimelineRuntimePresentationOperations Evaluate(
            TimelineRuntimePlayback playback,
            float previousFrame,
            int previousCycle,
            float currentFrame,
            int currentCycle,
            bool loop,
            bool includeStartBoundary)
        {
            if (playback == null)
                throw new ArgumentNullException(nameof(playback));
            TimelineData timeline = playback.SourceTimeline;
            int frameRate = Math.Max(1, playback.Content.FrameRate);
            int maxFrame = TimelineTimeGrid.CeilingIndex(playback.Content.Duration, playback.Content.FrameRate);
            var animations = new List<TimelineAnimationContribution>();
            var cameraStates = new List<TimelineCameraStateSample>();
            var cameraCues = new List<TimelineCameraCueSample>();
            var cameraResponses = new List<TimelineCameraResponseSample>();
            var cameraResources = new List<TimelineCameraResourceSample>();
            var scenePresentation = new List<TimelineRuntimeScenePresentationSample>();
            List<TimelineRuntimeEvaluationSegment> segments = BuildSegments(
                previousFrame,
                previousCycle,
                currentFrame,
                currentCycle,
                maxFrame,
                loop,
                frameRate);
            Func<Clip, bool> presentationClipFilter = clip =>
                TimelineRuntimeEvaluator.HasProjection(
                    playback.Content,
                    clip,
                    TimelineExecutionDomain.Presentation);
            for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                TimelineRuntimeEvaluationSegment segment = segments[segmentIndex];
                for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
                {
                    Track track = timeline.Tracks[trackIndex];
                    if (track == null)
                        continue;
                    if (track is AnimationTrack animationTrack)
                    {
                        animationTrack.Sample(
                            segment.PreviousTime.ToSingle(),
                            segment.CurrentTime.ToSingle(),
                            trackIndex,
                            timeline.AuthoringId,
                            timeline.Name,
                            animations,
                            loop,
                            segment.Cycle,
                            presentationClipFilter);
                    }
                    else if (track is CameraCueTrack cameraCueTrack)
                    {
                        cameraCueTrack.Sample(
                            segment.PreviousTime,
                            segment.CurrentTime,
                            timeline.AuthoringId,
                            timeline.Name,
                            cameraCues,
                            segmentIndex > 0 || includeStartBoundary && segment.PreviousTime.Raw == 0,
                            presentationClipFilter);
                    }
                }
            }
            float currentTime = currentFrame / frameRate;
            int sampledFrame = Mathf.Clamp(Mathf.FloorToInt(currentFrame), 0, maxFrame);
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null)
                    continue;
                if (track is CameraStateTrack cameraStateTrack)
                    cameraStateTrack.Sample(
                        currentTime,
                        timeline.AuthoringId,
                        timeline.Name,
                        cameraStates,
                        presentationClipFilter);
                else if (track is CameraResponseTrack cameraResponseTrack)
                    cameraResponseTrack.Sample(
                        currentTime,
                        timeline.AuthoringId,
                        timeline.Name,
                        cameraResponses,
                        presentationClipFilter);
                else if (track is CameraEffectTrack cameraEffectTrack)
                    cameraEffectTrack.Sample(
                        currentTime,
                        timeline.AuthoringId,
                        timeline.Name,
                        cameraResources,
                        presentationClipFilter);
                else if (track is ScenePresentationParameterTrack sceneTrack && !track.PersistentMuted)
                {
                    for (int clipIndex = 0; clipIndex < sceneTrack.Clips.Count; clipIndex++)
                    {
                        if (sceneTrack.Clips[clipIndex] is not ScenePresentationParameterCurveClip clip ||
                            !presentationClipFilter(clip) ||
                            currentTime < clip.StartTime.ToSingle() || currentTime > clip.EndTime.ToSingle())
                        {
                            continue;
                        }
                        float duration = Mathf.Max(0.0001f, clip.DurationTime.ToSingle());
                        float local = Mathf.Clamp01((currentTime - clip.StartTime.ToSingle()) / duration);
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
            return new TimelineRuntimePresentationOperations(
                animations,
                cameraStates,
                cameraCues,
                cameraResponses,
                cameraResources,
                scenePresentation);
        }

        static List<TimelineRuntimeEvaluationSegment> BuildSegments(
            float previousFrame,
            int previousCycle,
            float currentFrame,
            int currentCycle,
            int maxFrame,
            bool loop,
            int frameRate)
        {
            var result = new List<TimelineRuntimeEvaluationSegment>();
            if (!loop || currentCycle == previousCycle || maxFrame <= 0)
            {
                result.Add(new TimelineRuntimeEvaluationSegment(
                    FixedScalar.FromDouble(previousFrame / (double)frameRate),
                    FixedScalar.FromDouble(currentFrame / (double)frameRate),
                    currentCycle));
                return result;
            }
            if (currentCycle < previousCycle || currentCycle - previousCycle > 4096)
                throw new InvalidOperationException("Timeline presentation evaluation crossed an invalid cycle range.");
            FixedScalar maxTime = FixedScalar.FromRatio(maxFrame, frameRate);
            result.Add(new TimelineRuntimeEvaluationSegment(
                FixedScalar.FromDouble(previousFrame / (double)frameRate),
                maxTime,
                previousCycle));
            for (int cycle = previousCycle + 1; cycle < currentCycle; cycle++)
                result.Add(new TimelineRuntimeEvaluationSegment(FixedScalar.Zero, maxTime, cycle));
            result.Add(new TimelineRuntimeEvaluationSegment(FixedScalar.Zero, FixedScalar.FromDouble(currentFrame / (double)frameRate), currentCycle));
            return result;
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
