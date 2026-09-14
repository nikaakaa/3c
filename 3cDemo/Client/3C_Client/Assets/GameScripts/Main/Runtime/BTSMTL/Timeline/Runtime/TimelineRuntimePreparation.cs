using System;
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
        public TimelineRuntimeAdvanceRequest(ulong logicTick, int deltaFrames)
        {
            if (logicTick == 0)
                throw new ArgumentOutOfRangeException(nameof(logicTick));
            if (deltaFrames < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaFrames));
            LogicTick = logicTick;
            DeltaFrames = deltaFrames;
        }

        public ulong LogicTick { get; }
        public int DeltaFrames { get; }
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
            int frame,
            int cycle,
            TimelineRuntimeClipBoundaryKind kind)
        {
            AuthoringId = string.IsNullOrWhiteSpace(authoringId)
                ? throw new ArgumentException("Timeline Clip identity is required.", nameof(authoringId))
                : authoringId.Trim();
            TrackAuthoringId = string.IsNullOrWhiteSpace(trackAuthoringId)
                ? throw new ArgumentException("Timeline Track identity is required.", nameof(trackAuthoringId))
                : trackAuthoringId.Trim();
            if (frame < 0)
                throw new ArgumentOutOfRangeException(nameof(frame));
            if (cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(cycle));
            if (!Enum.IsDefined(typeof(TimelineRuntimeClipBoundaryKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            Frame = frame;
            Cycle = cycle;
            Kind = kind;
        }

        public string AuthoringId { get; }
        public string TrackAuthoringId { get; }
        public int Frame { get; }
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
            ulong logicTick,
            int previousFrame,
            int frame,
            int previousCycle,
            int cycle,
            string sectionId,
            IReadOnlyList<string> activeClipIds,
            IReadOnlyList<TimelineRuntimeClipBoundary> boundaries,
            TimelineRuntimeEvaluationResult evaluation,
            bool completes)
        {
            Owner = owner;
            Generation = generation;
            LogicTick = logicTick;
            PreviousFrame = previousFrame;
            Frame = frame;
            PreviousCycle = previousCycle;
            Cycle = cycle;
            SectionId = sectionId ?? string.Empty;
            m_ActiveClipIds = new ReadOnlyCollection<string>(new List<string>(activeClipIds ?? Array.Empty<string>()));
            m_Boundaries = new ReadOnlyCollection<TimelineRuntimeClipBoundary>(
                new List<TimelineRuntimeClipBoundary>(boundaries ?? Array.Empty<TimelineRuntimeClipBoundary>()));
            Evaluation = evaluation ?? throw new ArgumentNullException(nameof(evaluation));
            Completes = completes;
        }

        internal TimelineRuntimePlayback Owner { get; }
        public ulong Generation { get; }
        public ulong LogicTick { get; }
        public int PreviousFrame { get; }
        public int Frame { get; }
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
        readonly ReadOnlyCollection<string> m_ActiveClipIdsView;
        TimelineRuntimeAdvanceResult m_PendingAdvance;
        TimelinePlaybackStopContext m_PendingStopContext;
        bool m_StopPending;
        int m_CursorFrame;
        int m_Cycle;
        string m_SectionId = string.Empty;
        bool m_InitialBoundaryPending;

        internal TimelineRuntimePlayback(
            TimelineRuntimePlaybackHandle handle,
            ulong generation,
            TimelineRuntimePreparationResult preparation)
        {
            Handle = handle;
            Generation = generation;
            RequestId = preparation.RequestId;
            ExecutionIdentity = preparation.ExecutionIdentity;
            PlaybackMode = preparation.PlaybackMode;
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
        public TimelineData SourceTimeline { get; }
        public string ContentRevision => Content.ContentHash;
        public TimelineRuntimePreparedDependencies PreparedDependencies { get; }
        public TimelinePreparedBindings PreparedBindings { get; }
        public TimelineRuntimePlaybackState State { get; private set; }
        public int CursorFrame => m_CursorFrame;
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

        public TimelineRuntimeAdvanceResult Advance(TimelineRuntimeAdvanceRequest request)
        {
            if (State != TimelineRuntimePlaybackState.Running)
                throw new InvalidOperationException("Timeline playback must be running before Advance.");
            if (m_PendingAdvance != null || m_StopPending)
                throw new InvalidOperationException("Timeline playback has an uncommitted Advance result.");
            int maxFrame = Math.Max(0, Content.MaxFrame);
            bool loop = PlaybackMode == TimelinePlaybackMode.Loop;
            long requestedFrame = (long)m_CursorFrame + request.DeltaFrames;
            int nextFrame;
            int nextCycle = m_Cycle;
            if (loop && maxFrame > 0)
            {
                long cycleDelta = requestedFrame / maxFrame;
                if (cycleDelta > int.MaxValue - nextCycle)
                    throw new InvalidOperationException("Timeline playback cycle exceeds the supported range.");
                nextCycle += (int)cycleDelta;
                nextFrame = (int)(requestedFrame % maxFrame);
            }
            else
            {
                nextFrame = (int)Math.Min(requestedFrame, maxFrame);
            }

            var activeClipIds = new List<string>();
            for (int index = 0; index < Content.Clips.Count; index++)
            {
                TimelineContentClip clip = Content.Clips[index];
                if (!clip.TrackMuted && clip.StartFrame <= nextFrame && nextFrame < clip.EndFrame)
                    activeClipIds.Add(clip.AuthoringId);
            }
            string sectionId = string.Empty;
            for (int index = 0; index < Content.Sections.Count; index++)
            {
                TimelineContentSection section = Content.Sections[index];
                if (section.Frame > nextFrame)
                    break;
                sectionId = section.AuthoringId;
            }
            List<TimelineRuntimeClipBoundary> boundaries = CollectBoundaries(
                m_CursorFrame,
                m_Cycle,
                nextFrame,
                nextCycle,
                maxFrame,
                loop,
                m_InitialBoundaryPending);
            TimelineRuntimeEvaluationResult evaluation = TimelineRuntimeEvaluator.Evaluate(
                SourceTimeline,
                m_CursorFrame,
                m_Cycle,
                nextFrame,
                nextCycle,
                loop,
                boundaries,
                ExecutionIdentity,
                Generation);
            bool completes = !loop && nextFrame >= maxFrame;
            m_PendingAdvance = new TimelineRuntimeAdvanceResult(
                this,
                Generation,
                request.LogicTick,
                m_CursorFrame,
                nextFrame,
                m_Cycle,
                nextCycle,
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
            m_CursorFrame = advance.Frame;
            m_Cycle = advance.Cycle;
            m_SectionId = advance.SectionId;
            m_InitialBoundaryPending = false;
            m_ActiveClipIds.Clear();
            for (int index = 0; index < advance.ActiveClipIds.Count; index++)
                m_ActiveClipIds.Add(advance.ActiveClipIds[index]);
            m_PendingAdvance = null;
            if (advance.Completes)
                State = TimelineRuntimePlaybackState.Completed;
            return true;
        }

        public bool Discard(TimelineRuntimeAdvanceResult advance)
        {
            RequirePendingAdvance(advance);
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
            StopContext = context;
            HasStopContext = true;
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

        internal bool RestoreCommittedState(
            TimelineRuntimePlaybackState state,
            int cursorFrame,
            int cycle,
            string sectionId,
            IReadOnlyList<string> activeClipIds,
            bool hasStopContext,
            TimelinePlaybackStopContext stopContext,
            bool initialBoundaryPending)
        {
            if (m_PendingAdvance != null || m_StopPending)
                return false;
            if (cursorFrame < 0 || cycle < 0 || cursorFrame > Math.Max(0, Content.MaxFrame))
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
            if (!ValidateRestoredSection(sectionId, cursorFrame) ||
                !ValidateRestoredActiveClips(activeClipIds, state, cursorFrame))
                return false;
            m_CursorFrame = cursorFrame;
            m_Cycle = cycle;
            m_SectionId = sectionId ?? string.Empty;
            m_InitialBoundaryPending = initialBoundaryPending;
            m_ActiveClipIds.Clear();
            for (int index = 0; index < (activeClipIds?.Count ?? 0); index++)
                m_ActiveClipIds.Add(activeClipIds[index]);
            HasStopContext = hasStopContext;
            StopContext = stopContext;
            State = state;
            return true;
        }

        bool ValidateRestoredSection(string sectionId, int cursorFrame)
        {
            if (string.IsNullOrEmpty(sectionId))
                return true;
            for (int index = 0; index < Content.Sections.Count; index++)
            {
                TimelineContentSection section = Content.Sections[index];
                if (string.Equals(section.AuthoringId, sectionId, StringComparison.Ordinal))
                    return section.Frame <= cursorFrame;
            }
            return false;
        }

        bool ValidateRestoredActiveClips(
            IReadOnlyList<string> activeClipIds,
            TimelineRuntimePlaybackState state,
            int cursorFrame)
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
                    if (clip.TrackMuted || clip.StartFrame > cursorFrame || cursorFrame >= clip.EndFrame)
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

        void RefreshActiveState()
        {
            m_ActiveClipIds.Clear();
            for (int index = 0; index < Content.Clips.Count; index++)
            {
                TimelineContentClip clip = Content.Clips[index];
                if (!clip.TrackMuted && clip.StartFrame <= m_CursorFrame && m_CursorFrame < clip.EndFrame)
                    m_ActiveClipIds.Add(clip.AuthoringId);
            }
            m_SectionId = string.Empty;
            for (int index = 0; index < Content.Sections.Count; index++)
            {
                TimelineContentSection section = Content.Sections[index];
                if (section.Frame > m_CursorFrame)
                    break;
                m_SectionId = section.AuthoringId;
            }
        }

        List<TimelineRuntimeClipBoundary> CollectBoundaries(
            int previousFrame,
            int previousCycle,
            int nextFrame,
            int nextCycle,
            int maxFrame,
            bool loop,
            bool initialBoundaryPending)
        {
            var result = new List<TimelineRuntimeClipBoundary>();
            if (maxFrame <= 0)
                return result;
            long previousAbsolute = (long)previousCycle * maxFrame + previousFrame;
            long nextAbsolute = (long)nextCycle * maxFrame + nextFrame;
            if (nextAbsolute <= previousAbsolute)
                return result;
            if (loop && nextCycle - previousCycle > 4096)
                throw new InvalidOperationException("Timeline playback crossed more than 4096 cycles in one Advance.");

            int firstCycle = loop ? previousCycle : 0;
            int lastCycle = loop ? nextCycle : 0;
            for (int clipIndex = 0; clipIndex < Content.Clips.Count; clipIndex++)
            {
                TimelineContentClip clip = Content.Clips[clipIndex];
                if (clip.TrackMuted)
                    continue;
                for (int cycle = firstCycle; cycle <= lastCycle; cycle++)
                {
                    AddBoundary(
                        result,
                        clip,
                        clip.StartFrame,
                        cycle,
                        TimelineRuntimeClipBoundaryKind.Enter,
                        previousAbsolute,
                        nextAbsolute,
                        maxFrame,
                        initialBoundaryPending);
                    AddBoundary(
                        result,
                        clip,
                        clip.EndFrame,
                        cycle,
                        TimelineRuntimeClipBoundaryKind.Exit,
                        previousAbsolute,
                        nextAbsolute,
                        maxFrame);
                }
            }
            result.Sort((left, right) =>
            {
                int position = ((long)left.Cycle * maxFrame + left.Frame).CompareTo(
                    (long)right.Cycle * maxFrame + right.Frame);
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
            int frame,
            int cycle,
            TimelineRuntimeClipBoundaryKind kind,
            long previousAbsolute,
            long nextAbsolute,
            int maxFrame,
            bool initialBoundaryPending)
        {
            long absolute = (long)cycle * maxFrame + frame;
            bool initialEnter = initialBoundaryPending &&
                                kind == TimelineRuntimeClipBoundaryKind.Enter &&
                                absolute == previousAbsolute;
            if ((!initialEnter && absolute <= previousAbsolute) || absolute > nextAbsolute)
                return;
            result.Add(new TimelineRuntimeClipBoundary(
                clip.AuthoringId,
                clip.TrackAuthoringId,
                frame,
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
        public TimelineData SourceTimeline { get; }
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
            ulong generation)
        {
            if (preparation == null)
                throw new ArgumentNullException(nameof(preparation));
            if (!preparation.IsReady)
                throw new InvalidOperationException("Timeline playback cannot be created from a failed preparation.");
            return CreatePlayback(
                preparation,
                new TimelineRuntimePlaybackHandle(preparation.ExecutionIdentity.InstanceId),
                generation);
        }

        public static TimelineRuntimePlayback CreatePlayback(
            TimelineRuntimePreparationResult preparation,
            TimelineRuntimePlaybackHandle handle,
            ulong generation)
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
                preparation);
        }
    }

    public readonly struct TimelineRuntimeTreeClipRequest
    {
        public TimelineRuntimeTreeClipRequest(
            string clipAuthoringId,
            string trackAuthoringId,
            TimelineTreeExecutionPhase phase,
            TimelineRuntimeTreeClipEventKind eventKind,
            int frame,
            int cycle,
            float normalizedTime)
        {
            ClipAuthoringId = string.IsNullOrWhiteSpace(clipAuthoringId)
                ? throw new ArgumentException("TreeClip identity is required.", nameof(clipAuthoringId))
                : clipAuthoringId.Trim();
            TrackAuthoringId = string.IsNullOrWhiteSpace(trackAuthoringId)
                ? throw new ArgumentException("Tree Track identity is required.", nameof(trackAuthoringId))
                : trackAuthoringId.Trim();
            Phase = phase;
            EventKind = eventKind;
            Frame = frame;
            Cycle = cycle;
            NormalizedTime = Mathf.Clamp01(normalizedTime);
        }

        public string ClipAuthoringId { get; }
        public string TrackAuthoringId { get; }
        public TimelineTreeExecutionPhase Phase { get; }
        public TimelineRuntimeTreeClipEventKind EventKind { get; }
        public int Frame { get; }
        public int Cycle { get; }
        public float NormalizedTime { get; }
    }

    public enum TimelineRuntimeTreeClipEventKind : byte
    {
        Enter = 1,
        Update = 2,
        Exit = 3
    }

    public readonly struct TimelineRuntimeClipSample
    {
        public TimelineRuntimeClipSample(
            string clipAuthoringId,
            string trackAuthoringId,
            string contractKind,
            TimelineRuntimeTreeClipEventKind eventKind,
            int frame,
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
            Frame = frame;
            Cycle = cycle;
            NormalizedTime = Mathf.Clamp01(normalizedTime);
        }

        public string ClipAuthoringId { get; }
        public string TrackAuthoringId { get; }
        public string ContractKind { get; }
        public TimelineRuntimeTreeClipEventKind EventKind { get; }
        public int Frame { get; }
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
            IReadOnlyList<TimelineRuntimeScenePresentationSample> scenePresentation,
            IReadOnlyList<TimelineRuntimeMotionWarpRequest> motionWarps,
            IReadOnlyList<TimelineRuntimeClipSample> clipSamples)
        {
            AnimationContributions = Copy(animations);
            MotionContributions = Copy(motions);
            CameraStates = Copy(cameraStates);
            CameraCues = Copy(cameraCues);
            CameraResponses = Copy(cameraResponses);
            CameraResources = Copy(cameraResources);
            ActionCues = Copy(actionCues);
            TreeClips = Copy(treeClips);
            ScenePresentation = Copy(scenePresentation);
            MotionWarps = Copy(motionWarps);
            ClipSamples = Copy(clipSamples);
        }

        public IReadOnlyList<TimelineAnimationContribution> AnimationContributions { get; }
        public IReadOnlyList<TimelineMotionCurveContribution> MotionContributions { get; }
        public IReadOnlyList<TimelineCameraStateSample> CameraStates { get; }
        public IReadOnlyList<TimelineCameraCueSample> CameraCues { get; }
        public IReadOnlyList<TimelineCameraResponseSample> CameraResponses { get; }
        public IReadOnlyList<TimelineCameraResourceSample> CameraResources { get; }
        public IReadOnlyList<TimelineActionCueSample> ActionCues { get; }
        public IReadOnlyList<TimelineRuntimeTreeClipRequest> TreeClips { get; }
        public IReadOnlyList<TimelineRuntimeScenePresentationSample> ScenePresentation { get; }
        public IReadOnlyList<TimelineRuntimeMotionWarpRequest> MotionWarps { get; }
        public IReadOnlyList<TimelineRuntimeClipSample> ClipSamples { get; }

        static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> values)
        {
            return new ReadOnlyCollection<T>(
                new List<T>(values ?? Array.Empty<T>()));
        }
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
            int previousFrame,
            int previousCycle,
            int currentFrame,
            int currentCycle,
            bool loop,
            IReadOnlyList<TimelineRuntimeClipBoundary> boundaries,
            TimelineExecutionIdentity executionIdentity,
            ulong generation)
        {
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            int frameRate = Math.Max(1, TimelineUtility.FrameRate);
            var animations = new List<TimelineAnimationContribution>();
            var motions = new List<TimelineMotionCurveContribution>();
            var cameraStates = new List<TimelineCameraStateSample>();
            var cameraCues = new List<TimelineCameraCueSample>();
            var cameraResponses = new List<TimelineCameraResponseSample>();
            var cameraResources = new List<TimelineCameraResourceSample>();
            var actionCues = new List<TimelineActionCueSample>();
            var treeClips = new List<TimelineRuntimeTreeClipRequest>();
            var scenePresentation = new List<TimelineRuntimeScenePresentationSample>();
            var motionWarps = new List<TimelineRuntimeMotionWarpRequest>();
            var clipSamples = new List<TimelineRuntimeClipSample>();
            List<TimelineRuntimeEvaluationSegment> segments = BuildSegments(
                previousFrame,
                previousCycle,
                currentFrame,
                currentCycle,
                Math.Max(0, timeline.MaxFrame),
                loop,
                frameRate);
            for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                TimelineRuntimeEvaluationSegment segment = segments[segmentIndex];
                for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
                {
                    Track track = timeline.Tracks[trackIndex];
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
                            cameraCues);
                    }
                    else if (track is ActionCueTrack actionCueTrack)
                    {
                        actionCueTrack.Sample(
                            segment.PreviousTime,
                            segment.CurrentTime,
                            timeline.AuthoringId,
                            timeline.Name,
                            actionCues);
                    }
                    else if (track is MotionWarpTrack motionWarpTrack && !track.PersistentMuted)
                    {
                        for (int clipIndex = 0; clipIndex < motionWarpTrack.Clips.Count; clipIndex++)
                        {
                            if (motionWarpTrack.Clips[clipIndex] is not MotionWarpClip motionWarpClip ||
                                segment.CurrentTime <= motionWarpClip.StartTime ||
                                segment.PreviousTime >= motionWarpClip.EndTime)
                                continue;
                            float duration = Mathf.Max(0.0001f, motionWarpClip.DurationTime);
                            float previousNormalized = Mathf.Clamp01(
                                (segment.PreviousTime - motionWarpClip.StartTime) / duration);
                            float normalized = Mathf.Clamp01(
                                (segment.CurrentTime - motionWarpClip.StartTime) / duration);
                            motionWarps.Add(new TimelineRuntimeMotionWarpRequest(
                                motionWarpClip.AuthoringId,
                                motionWarpClip.SourceMotionClipId,
                                Mathf.RoundToInt(segment.CurrentTime * frameRate),
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
            float currentTime = currentFrame / (float)frameRate;
            for (int boundaryIndex = 0; boundaryIndex < (boundaries?.Count ?? 0); boundaryIndex++)
            {
                TimelineRuntimeClipBoundary boundary = boundaries[boundaryIndex];
                clipSamples.Add(new TimelineRuntimeClipSample(
                    boundary.AuthoringId,
                    boundary.TrackAuthoringId,
                    ResolveContractKind(timeline, boundary.AuthoringId),
                    boundary.Kind == TimelineRuntimeClipBoundaryKind.Enter
                        ? TimelineRuntimeTreeClipEventKind.Enter
                        : TimelineRuntimeTreeClipEventKind.Exit,
                    boundary.Frame,
                    boundary.Cycle,
                    boundary.Kind == TimelineRuntimeClipBoundaryKind.Enter ? 0f : 1f));
                if (!TryResolveTreeClip(
                        timeline,
                        boundary.AuthoringId,
                        out TreeClip treeClip))
                    continue;
                treeClips.Add(new TimelineRuntimeTreeClipRequest(
                    treeClip.AuthoringId,
                    treeClip.Track.AuthoringId,
                    treeClip.ExecutionPhase,
                    boundary.Kind == TimelineRuntimeClipBoundaryKind.Enter
                        ? TimelineRuntimeTreeClipEventKind.Enter
                        : TimelineRuntimeTreeClipEventKind.Exit,
                    boundary.Frame,
                    boundary.Cycle,
                    boundary.Kind == TimelineRuntimeClipBoundaryKind.Enter ? 0f : 1f));
            }
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                if (timeline.Tracks[trackIndex] is not ScenePresentationParameterTrack track)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    if (track.Clips[clipIndex] is not ScenePresentationParameterCurveClip clip ||
                        currentTime < clip.StartTime || currentTime > clip.EndTime)
                        continue;
                    float duration = Mathf.Max(0.0001f, clip.DurationTime);
                    float local = Mathf.Clamp01((currentTime - clip.StartTime) / duration);
                    scenePresentation.Add(new TimelineRuntimeScenePresentationSample(
                        clip.AuthoringId,
                        clip.TargetBindingId,
                        clip.ParameterBindingId,
                        clip.ParameterValueKind,
                        clip.ValueCurve.Evaluate(local),
                        local,
                        currentFrame,
                        currentCycle,
                        executionIdentity,
                        generation));
                }
            }
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track is CameraStateTrack cameraStateTrack)
                    cameraStateTrack.Sample(currentTime, timeline.AuthoringId, timeline.Name, cameraStates);
                else if (track is CameraResponseTrack cameraResponseTrack)
                    cameraResponseTrack.Sample(currentTime, timeline.AuthoringId, timeline.Name, cameraResponses);
                else if (track is CameraResourceTrack cameraResourceTrack)
                    cameraResourceTrack.Sample(currentTime, timeline.AuthoringId, timeline.Name, cameraResources);
            }
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                if (timeline.Tracks[trackIndex] is not TreeTrack treeTrack || treeTrack.PersistentMuted)
                    continue;
                for (int clipIndex = 0; clipIndex < treeTrack.Clips.Count; clipIndex++)
                {
                    if (treeTrack.Clips[clipIndex] is not TreeClip treeClip ||
                        currentTime <= treeClip.StartTime || currentTime >= treeClip.EndTime)
                        continue;
                    float duration = Mathf.Max(0.0001f, treeClip.DurationTime);
                    float local = Mathf.Clamp01((currentTime - treeClip.StartTime) / duration);
                    treeClips.Add(new TimelineRuntimeTreeClipRequest(
                        treeClip.AuthoringId,
                        treeTrack.AuthoringId,
                        treeClip.ExecutionPhase,
                        TimelineRuntimeTreeClipEventKind.Update,
                        currentFrame,
                        currentCycle,
                        local));
                }
            }
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track.PersistentMuted)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip == null || currentTime <= clip.StartTime || currentTime >= clip.EndTime)
                        continue;
                    float duration = Mathf.Max(0.0001f, clip.DurationTime);
                    clipSamples.Add(new TimelineRuntimeClipSample(
                        clip.AuthoringId,
                        track.AuthoringId,
                        clip.ContractKind,
                        TimelineRuntimeTreeClipEventKind.Update,
                        currentFrame,
                        currentCycle,
                        (currentTime - clip.StartTime) / duration));
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
                scenePresentation,
                motionWarps,
                clipSamples);
        }

        static bool TryResolveTreeClip(
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
            int previousFrame,
            int previousCycle,
            int currentFrame,
            int currentCycle,
            int maxFrame,
            bool loop,
            int frameRate)
        {
            var result = new List<TimelineRuntimeEvaluationSegment>();
            if (!loop || currentCycle == previousCycle || maxFrame <= 0)
            {
                result.Add(new TimelineRuntimeEvaluationSegment(
                    previousFrame / (float)frameRate,
                    currentFrame / (float)frameRate,
                    currentCycle));
                return result;
            }
            if (currentCycle - previousCycle > 4096)
                throw new InvalidOperationException("Timeline evaluation crossed more than 4096 cycles in one Advance.");
            result.Add(new TimelineRuntimeEvaluationSegment(
                previousFrame / (float)frameRate,
                maxFrame / (float)frameRate,
                previousCycle));
            for (int cycle = previousCycle + 1; cycle < currentCycle; cycle++)
                result.Add(new TimelineRuntimeEvaluationSegment(
                    0f,
                    maxFrame / (float)frameRate,
                    cycle));
            result.Add(new TimelineRuntimeEvaluationSegment(
                0f,
                currentFrame / (float)frameRate,
                currentCycle));
            return result;
        }
    }

    readonly struct TimelineRuntimeEvaluationSegment
    {
        public TimelineRuntimeEvaluationSegment(float previousTime, float currentTime, int cycle)
        {
            PreviousTime = previousTime;
            CurrentTime = currentTime;
            Cycle = cycle;
        }

        public float PreviousTime { get; }
        public float CurrentTime { get; }
        public int Cycle { get; }
    }
}
