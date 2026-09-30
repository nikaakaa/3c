using System;
using ThirdPersonSimulation.Fixed;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BTSMTL.Diagnostics;
using ThirdPersonSimulation;

namespace BTSMTL.Timeline.Runtime
{
    static class TimelineRuntimeIdentity
    {
        public static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Timeline runtime identity is required.", name);
            return value.Trim();
        }
    }

    public sealed class TimelineRuntimeCallBindingSource : ITimelineRuntimeCallBindingSource
    {
        readonly string m_OwnerIdentity;
        readonly string m_CallIdentity;
        readonly TimelinePlaybackActionContext m_ActionContext;
        readonly ReadOnlyCollection<TimelineCallBinding> m_CallBindings;

        public TimelineRuntimeCallBindingSource(
            string ownerIdentity,
            string callIdentity,
            TimelinePlaybackActionContext actionContext,
            IEnumerable<TimelineCallBinding> callBindings = null)
        {
            m_OwnerIdentity = TimelineRuntimeIdentity.Require(ownerIdentity, nameof(ownerIdentity));
            m_CallIdentity = TimelineRuntimeIdentity.Require(callIdentity, nameof(callIdentity));
            m_ActionContext = actionContext;
            m_CallBindings = new ReadOnlyCollection<TimelineCallBinding>(
                new List<TimelineCallBinding>(callBindings ?? Array.Empty<TimelineCallBinding>()));
        }

        public bool TryCreateExecutionIdentity(
            string sourceId,
            string sourceName,
            TimelinePlaybackActionContext actionContext,
            TimelineRuntimePlaybackHandle playbackHandle,
            out TimelineExecutionIdentity identity,
            out string error)
        {
            identity = default;
            error = string.Empty;
            if (!playbackHandle.IsValid)
            {
                error = "timeline_execution_handle_invalid";
                return false;
            }
            if (m_ActionContext.IsValid && actionContext.IsValid &&
                m_ActionContext.ActionInstanceId != actionContext.ActionInstanceId)
            {
                error = "timeline_action_context_mismatch";
                return false;
            }
            identity = new TimelineExecutionIdentity(
                m_OwnerIdentity,
                $"{m_CallIdentity}:{sourceId?.Trim() ?? string.Empty}:{sourceName?.Trim() ?? string.Empty}",
                playbackHandle.Value);
            return true;
        }

        public IReadOnlyList<TimelineCallBinding> CallBindings => m_CallBindings;

        public bool TryGetTimelinePlaybackActionContext(
            ActionContextSlot actionContext,
            out TimelinePlaybackActionContext playbackActionContext)
        {
            playbackActionContext = m_ActionContext;
            return playbackActionContext.IsValid;
        }
    }

    public readonly struct TimelineRuntimeCommittedEvaluation
    {
        internal TimelineRuntimeCommittedEvaluation(TimelineRuntimeStepContext context)
        {
            Handle = context.Playback.Handle;
            Generation = context.Playback.Generation;
            LogicTick = context.Request.LogicTick;
            PreviousTime = context.Advance.PreviousTime;
            Time = context.Advance.Time;
            PreviousCycle = context.Advance.PreviousCycle;
            Cycle = context.Advance.Cycle;
            ContentIdentity = context.Playback.Content.Identity;
            ContentRevision = context.Playback.Content.ContentHash;
            ExecutionIdentity = context.Playback.ExecutionIdentity;
            Evaluation = context.Advance.Evaluation;
            ActiveClipIds = context.Advance.ActiveClipIds;
            Completes = context.Advance.Completes;
            Control = context.Advance.Control;
        }

        public TimelineRuntimePlaybackHandle Handle { get; }
        public ulong Generation { get; }
        public ulong LogicTick { get; }
        public FixedScalar PreviousTime { get; }
        public FixedScalar Time { get; }
        public int PreviousCycle { get; }
        public int Cycle { get; }
        public string ContentIdentity { get; }
        public string ContentRevision { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public TimelineRuntimeEvaluationResult Evaluation { get; }
        public TimelineRuntimeSampleView<string> ActiveClipIds { get; }
        public bool Completes { get; }
        public AbilityTimelinePlaybackControl Control { get; }
    }

    public sealed class TimelineRuntimeEvaluationBuffer : ITimelineRuntimeEvaluationSink
    {
        readonly Dictionary<ulong, TimelineRuntimeEvaluationResult> m_Pending =
            new Dictionary<ulong, TimelineRuntimeEvaluationResult>();
        readonly Dictionary<ulong, TimelineRuntimeCommittedEvaluation> m_Committed =
            new Dictionary<ulong, TimelineRuntimeCommittedEvaluation>();

        public event Action<TimelineRuntimeStepContext> Committed;
        public event Action<TimelineRuntimeCommittedEvaluation> CommittedEvaluation;
        public event Action<TimelineRuntimeStopRequest> StopCommitted;

        public bool Consume(TimelineRuntimeStepContext context)
        {
            m_Pending[context.Playback.Handle.Value] = context.Advance.Evaluation;
            return true;
        }

        public void Commit(TimelineRuntimeStepContext context)
        {
            ulong handle = context.Playback.Handle.Value;
            if (m_Pending.TryGetValue(handle, out TimelineRuntimeEvaluationResult result))
            {
                TimelineRuntimeCommittedEvaluation committed = new TimelineRuntimeCommittedEvaluation(context);
                m_Committed[handle] = committed;
                m_Pending.Remove(handle);
                CommittedEvaluation?.Invoke(committed);
            }
            Committed?.Invoke(context);
        }

        public void Discard(TimelineRuntimeStepContext context)
        {
            m_Pending.Remove(context.Playback.Handle.Value);
        }

        public bool ConsumeStop(TimelineRuntimeStopRequest request) => true;

        public void CommitStop(TimelineRuntimeStopRequest request)
        {
            m_Pending.Remove(request.Handle.Value);
            m_Committed.Remove(request.Handle.Value);
            StopCommitted?.Invoke(request);
        }

        public void DiscardStop(TimelineRuntimeStopRequest request)
        {
            m_Pending.Remove(request.Handle.Value);
        }

        public bool TryGetCommitted(
            TimelineRuntimePlaybackHandle handle,
            out TimelineRuntimeEvaluationResult result)
        {
            if (TryGetCommittedEvaluation(handle, out TimelineRuntimeCommittedEvaluation committed))
            {
                result = committed.Evaluation;
                return true;
            }
            result = default;
            return false;
        }

        public bool TryGetCommittedEvaluation(
            TimelineRuntimePlaybackHandle handle,
            out TimelineRuntimeCommittedEvaluation evaluation)
        {
            if (m_Committed.TryGetValue(handle.Value, out evaluation) && evaluation.Evaluation.IsCurrent)
                return true;
            evaluation = default;
            return false;
        }

        public void ReleasePlayback(TimelineRuntimePlaybackHandle handle)
        {
            m_Pending.Remove(handle.Value);
            m_Committed.Remove(handle.Value);
        }

        public void Clear()
        {
            m_Pending.Clear();
            m_Committed.Clear();
        }
    }

    public enum TimelinePresentationSampleReason : byte
    {
        Advance = 0,
        Correction = 1,
        Completed = 2,
        Stopped = 3,
        Withdrawn = 4,
        Paused = 5
    }

    public readonly struct TimelineRuntimePresentationSample
    {
        public TimelineRuntimePresentationSample(ulong generation, ulong logicTick, string contentRevision, FixedScalar time, int cycle, TimelinePresentationSampleReason reason, bool retainForCorrection)
        {
            if (generation == 0 || logicTick == 0 || time < FixedScalar.Zero || cycle < 0 ||
                retainForCorrection && reason != TimelinePresentationSampleReason.Withdrawn)
                throw new ArgumentException("Timeline presentation sample is invalid.");
            Generation = generation;
            LogicTick = logicTick;
            ContentRevision = contentRevision;
            Time = time;
            Cycle = cycle;
            Reason = reason;
            RetainForCorrection = retainForCorrection;
        }

        public ulong Generation { get; }
        public ulong LogicTick { get; }
        public FixedScalar Time { get; }
        public int Cycle { get; }
        public string ContentRevision { get; }
        public TimelinePresentationSampleReason Reason { get; }
        public bool RetainForCorrection { get; }
        public bool AllowTraversal => Reason == TimelinePresentationSampleReason.Advance || Reason == TimelinePresentationSampleReason.Completed;
        public bool EndsPlayback => Reason == TimelinePresentationSampleReason.Completed || Reason == TimelinePresentationSampleReason.Stopped || Reason == TimelinePresentationSampleReason.Withdrawn;
        public bool IsValid => Generation != 0;
    }

    public sealed class TimelineRuntimePresentationDriver : ITimelineRuntimeEvaluationSink
    {
        readonly Dictionary<ulong, PresentationPlaybackState> m_Playbacks =
            new Dictionary<ulong, PresentationPlaybackState>();
        readonly Dictionary<(string ContentRevision, TimelinePlaybackMode Mode), Stack<PresentationPlaybackState>> m_RecycledPlaybacks = new();

        public void Prepare(TimelineRuntimePreparedContent content, TimelinePlaybackMode mode)
        {
            var key = (content.ContentRevision, mode);
            if (m_RecycledPlaybacks.ContainsKey(key))
                return;
            var recycled = new Stack<PresentationPlaybackState>(1);
            recycled.Push(new PresentationPlaybackState(content.SourceTimeline, content.Content, mode, recycled));
            m_RecycledPlaybacks.Add(key, recycled);
        }

        public bool TryPresent(
            TimelineRuntimeService service,
            TimelineRuntimePlaybackHandle handle,
            in TimelineRuntimePresentationSample sample,
            ulong presentationFrame,
            float presentationDeltaSeconds,
            float interpolationAlpha,
            out TimelineRuntimePresentationFrame frame)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));
            if (!handle.IsValid || presentationFrame == 0 ||
                !float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f ||
                !float.IsFinite(interpolationAlpha) || interpolationAlpha < 0f || interpolationAlpha > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(presentationFrame));
            }
            if (!service.TryGetPlayback(handle, out TimelineRuntimePlayback playback))
            {
                frame = default;
                return false;
            }

            if (!sample.IsValid || sample.Generation != playback.Generation || sample.Time > playback.Content.Duration ||
                !string.Equals(sample.ContentRevision, playback.ContentRevision, StringComparison.Ordinal))
                throw new InvalidOperationException("Timeline presentation sample does not match the prepared playback.");
            bool hasState = m_Playbacks.TryGetValue(handle.Value, out PresentationPlaybackState state);
            if (hasState && state.Generation != playback.Generation)
            {
                state.Recycle();
                m_Playbacks.Remove(handle.Value);
                hasState = false;
            }
            if (!hasState)
            {
                var key = (playback.ContentRevision, playback.PlaybackMode);
                Stack<PresentationPlaybackState> recycled = m_RecycledPlaybacks[key];
                if (recycled.Count == 0)
                    state = new PresentationPlaybackState(playback.SourceTimeline, playback.Content, playback.PlaybackMode, recycled);
                else
                    state = recycled.Pop();
                state.Restart(playback.Generation);
                m_Playbacks.Add(handle.Value, state);
            }

            if (state.HasPendingFrame)
            {
                if (state.PendingFrame.PresentationFrame != presentationFrame)
                    throw new InvalidOperationException("Timeline presentation candidate has not been accepted or discarded.");
                frame = state.PendingFrame;
                return false;
            }
            if (state.HasCachedFrame)
            {
                if (presentationFrame == state.LastPresentationFrame)
                {
                    frame = state.CachedFrame;
                    return false;
                }
                if (presentationFrame < state.LastPresentationFrame)
                    throw new InvalidOperationException("Timeline presentation frame moved backward without a generation reset.");
            }

            if (state.Finished && sample.EndsPlayback && sample.Time == state.CursorTime && sample.Cycle == state.Cycle)
            {
                frame = default;
                return false;
            }

            Array.Copy(state.MarkerLastTraversal, state.PendingMarkerLastTraversal, state.MarkerLastTraversal.Length);
            state.Candidate.Clear();
            var events = state.Candidate.Events;
            bool loop = playback.PlaybackMode == TimelinePlaybackMode.Loop;
            FixedScalar previousTime = state.CursorTime;
            int previousCycle = state.Cycle;
            FixedScalar currentTime = sample.Time;
            int currentCycle = sample.Cycle;
            bool allowTraversal = sample.AllowTraversal &&
                (currentCycle > previousCycle || currentCycle == previousCycle && currentTime >= previousTime);
            if (!allowTraversal)
            {
                previousTime = currentTime;
                previousCycle = currentCycle;
            }
            bool stopped = sample.Reason == TimelinePresentationSampleReason.Stopped || sample.Reason == TimelinePresentationSampleReason.Withdrawn;
            if (!stopped)
                TimelineRuntimePresentationEvaluator.Evaluate(
                playback,
                previousTime,
                previousCycle,
                currentTime,
                currentCycle,
                loop,
                allowTraversal && !state.InitialBoundaryConsumed,
                state.Candidate);
            if (allowTraversal)
                AppendMarkerEvents(
                playback,
                state,
                previousTime,
                previousCycle,
                currentTime,
                currentCycle,
                loop,
                !state.InitialBoundaryConsumed,
                events);
            AppendTreeClips(playback, state, sample, allowTraversal);
            var operations = new TimelineRuntimePresentationOperations(state.Candidate);
            state.PendingTime = currentTime;
            state.PendingCycle = currentCycle;
            state.PendingFinished = sample.EndsPlayback;
            state.PendingInitialBoundaryConsumed = state.InitialBoundaryConsumed || allowTraversal ||
                sample.Reason == TimelinePresentationSampleReason.Correction;
            frame = new TimelineRuntimePresentationFrame(
                playback,
                sample.LogicTick,
                presentationFrame,
                presentationDeltaSeconds,
                interpolationAlpha,
                operations,
                events.View,
                !allowTraversal && sample.Reason == TimelinePresentationSampleReason.Advance
                    ? TimelinePresentationSampleReason.Correction : sample.Reason,
                currentTime,
                currentCycle);
            state.PendingFrame = frame;
            state.HasPendingFrame = true;
            return true;
        }

        public bool RequestTreeClipExit(in TimelineRuntimePresentationFrame frame, in TimelineRuntimeTreeClipRequest request)
        {
            if (!m_Playbacks.TryGetValue(frame.Handle.Value, out PresentationPlaybackState state) ||
                state.Generation != frame.Generation || !state.HasPendingFrame || state.PendingFrame.PresentationFrame != frame.PresentationFrame)
                throw new InvalidOperationException("Presentation TreeClip exit requires its current candidate frame.");
            for (int index = 0; index < state.Trees.Length; index++)
            {
                PresentationTree tree = state.Trees[index];
                if (tree.Clip.AuthoringId != request.ClipAuthoringId)
                    continue;
                if (tree.Clip.ClipExitSource != TimelineClipExitSource.TreeDecision)
                    throw new InvalidOperationException("A fixed-interval Presentation TreeClip cannot request a graph-controlled exit.");
                if (state.PendingTreeCycles[index] != request.Cycle)
                    return false;
                state.PendingTreeCycles[index] = -1;
                state.PendingTreeExitCycles[index] = request.Cycle;
                for (int activeIndex = state.Candidate.ActiveTreeClips.Count - 1; activeIndex >= 0; activeIndex--)
                    if (state.Candidate.ActiveTreeClips[activeIndex].ClipAuthoringId == request.ClipAuthoringId)
                        state.Candidate.ActiveTreeClips.RemoveAt(activeIndex);
                if (request.EventKind == TimelineRuntimeTreeClipEventKind.Enter)
                    for (int pendingIndex = state.Candidate.TreeClips.Count - 1; pendingIndex >= 0; pendingIndex--)
                    {
                        TimelineRuntimeTreeClipRequest pending = state.Candidate.TreeClips[pendingIndex];
                        if (pending.ClipAuthoringId == request.ClipAuthoringId && pending.Cycle == request.Cycle &&
                            pending.EventKind == TimelineRuntimeTreeClipEventKind.Update)
                            state.Candidate.TreeClips.RemoveAt(pendingIndex);
                    }
                state.Candidate.TreeClips.Add(tree.Request(TimelineRuntimeTreeClipEventKind.Exit, frame.Time, request.Cycle, frame.Generation));
                return true;
            }
            throw new InvalidOperationException("Presentation TreeClip exit has no prepared clip.");
        }

        public void CommitPresentationFrame(ulong presentationFrame)
        {
            foreach (PresentationPlaybackState state in m_Playbacks.Values)
            {
                if (!state.HasPendingFrame || state.PendingFrame.PresentationFrame != presentationFrame)
                    continue;
                Array.Copy(state.PendingMarkerLastTraversal, state.MarkerLastTraversal, state.MarkerLastTraversal.Length);
                Array.Copy(state.PendingTreeCycles, state.TreeCycles, state.TreeCycles.Length);
                Array.Copy(state.PendingTreeExitCycles, state.TreeExitCycles, state.TreeExitCycles.Length);
                state.CursorTime = state.PendingTime;
                state.Cycle = state.PendingCycle;
                state.Finished = state.PendingFinished;
                state.InitialBoundaryConsumed = state.PendingInitialBoundaryConsumed;
                state.Cache(state.PendingFrame);
                state.PendingFrame = default;
                state.HasPendingFrame = false;
            }
        }

        public void DiscardPresentationFrame(ulong presentationFrame)
        {
            foreach (PresentationPlaybackState state in m_Playbacks.Values)
            {
                if (!state.HasPendingFrame || state.PendingFrame.PresentationFrame != presentationFrame)
                    continue;
                state.Candidate.Clear();
                state.PendingFrame = default;
                state.HasPendingFrame = false;
            }
        }

        public void SuspendPresentationPlayback(TimelineRuntimePlaybackHandle handle, ulong generation)
        {
            if (!m_Playbacks.TryGetValue(handle.Value, out PresentationPlaybackState state) || state.Generation != generation)
                return;
            state.Clear();
            state.PendingFrame = default;
            state.CachedFrame = default;
            state.HasPendingFrame = false;
            state.HasCachedFrame = false;
            state.Finished = false;
        }

        public void ReleasePresentationPlayback(TimelineRuntimePlaybackHandle handle, ulong generation)
        {
            if (m_Playbacks.TryGetValue(handle.Value, out PresentationPlaybackState state) && state.Generation == generation)
            {
                state.Recycle();
                m_Playbacks.Remove(handle.Value);
            }
        }

        public bool Consume(TimelineRuntimeStepContext context) => true;

        public void Commit(TimelineRuntimeStepContext context)
        {
        }

        public void Discard(TimelineRuntimeStepContext context)
        {
        }

        public bool ConsumeStop(TimelineRuntimeStopRequest request) => true;

        public void CommitStop(TimelineRuntimeStopRequest request)
        {
        }

        public void DiscardStop(TimelineRuntimeStopRequest request)
        {
        }

        public void Clear()
        {
            foreach (PresentationPlaybackState state in m_Playbacks.Values)
                state.Clear();
            m_Playbacks.Clear();
            m_RecycledPlaybacks.Clear();
        }

        static void AppendMarkerEvents(
            TimelineRuntimePlayback playback,
            PresentationPlaybackState state,
            FixedScalar previousTime,
            int previousCycle,
            FixedScalar currentTime,
            int currentCycle,
            bool loop,
            bool includeStartBoundary,
            TimelineRuntimeSampleBuffer<TimelineRuntimePresentationEvent> events)
        {
            FixedScalar duration = playback.Content.Duration;
            if (duration <= FixedScalar.Zero)
                return;
            if (currentCycle < previousCycle || currentCycle == previousCycle && currentTime < previousTime)
                throw new InvalidOperationException("Timeline presentation cursor moved backward without a generation reset.");
            int firstCycle = loop ? previousCycle : 0;
            int lastCycle = loop ? currentCycle : 0;
            for (int cycle = firstCycle; cycle <= lastCycle; cycle++)
            {
                for (int markerIndex = 0; markerIndex < playback.Content.Markers.Count; markerIndex++)
                {
                    TimelineContentMarker marker = playback.Content.Markers[markerIndex];
                    if (!marker.ExecutionPolicy.IsPresentation || marker.TrackMuted)
                        continue;
                    bool initial = includeStartBoundary && cycle == previousCycle && marker.Time == previousTime;
                    bool afterPrevious = cycle > previousCycle || cycle == previousCycle && marker.Time > previousTime;
                    bool beforeCurrent = cycle < currentCycle || cycle == currentCycle && marker.Time <= currentTime;
                    if ((!initial && !afterPrevious) || !beforeCurrent)
                        continue;
                    string markerId = marker.MarkerId;
                    ulong traversalIndex = checked((ulong)cycle + 1);
                    if (state.PendingMarkerLastTraversal[markerIndex] >= traversalIndex)
                        continue;
                    state.PendingMarkerLastTraversal[markerIndex] = traversalIndex;
                    events.Add(new TimelineRuntimePresentationEvent(
                        playback.Handle,
                        playback.ExecutionIdentity,
                        playback.Generation,
                        markerId,
                        traversalIndex,
                        marker.GraphId,
                        marker.GraphRevision,
                        marker.Time,
                        cycle));
                }
            }
        }

        readonly struct PresentationTree
        {
            public PresentationTree(TreeClip clip, string graphId, string revision)
            {
                Clip = clip;
                GraphId = graphId;
                Revision = revision;
            }
            public readonly TreeClip Clip;
            public readonly string GraphId;
            public readonly string Revision;
            public TimelineRuntimeTreeClipRequest Request(TimelineRuntimeTreeClipEventKind kind, FixedScalar time, int cycle, ulong generation) =>
                new(Clip.AuthoringId, Clip.Track.AuthoringId, GraphId, Revision, TimelineTreeExecutionPhase.Commit,
                    Clip.ClipExitSource, kind, time, cycle,
                    ((time - Clip.StartTime) / (Clip.EndTime - Clip.StartTime)).ToSingle(), generation,
                    TimelineRuntimeTreeClipRequest.ComposeBranchRevision(generation, cycle));
        }

        static void AppendTreeClips(TimelineRuntimePlayback playback, PresentationPlaybackState state,
            in TimelineRuntimePresentationSample sample, bool traverse)
        {
            if (sample.Cycle - state.Cycle > TimelineRuntimeEvaluationSegments.MaximumCycleAdvance)
                throw new InvalidOperationException("Timeline presentation exceeded its prepared cycle capacity.");
            bool stopped = sample.Reason == TimelinePresentationSampleReason.Stopped || sample.Reason == TimelinePresentationSampleReason.Withdrawn;
            bool correction = sample.Reason == TimelinePresentationSampleReason.Correction ||
                sample.Reason == TimelinePresentationSampleReason.Advance && !traverse;
            var output = state.Candidate.TreeClips;
            for (int index = 0; index < state.Trees.Length; index++)
            {
                PresentationTree tree = state.Trees[index];
                int activeCycle = state.TreeCycles[index];
                int exitCycle = correction ? -1 : state.TreeExitCycles[index];
                bool dynamic = tree.Clip.ClipExitSource == TimelineClipExitSource.TreeDecision;
                if ((stopped || correction) && activeCycle >= 0)
                {
                    output.Add(tree.Request(TimelineRuntimeTreeClipEventKind.Destroy, state.CursorTime, activeCycle, playback.Generation));
                    activeCycle = -1;
                }
                if (!stopped)
                {
                    int firstCycle = traverse ? state.Cycle : sample.Cycle;
                    for (int cycle = firstCycle; cycle <= sample.Cycle; cycle++)
                    {
                        FixedScalar from = cycle == state.Cycle && traverse ? state.CursorTime : FixedScalar.Zero;
                        FixedScalar to = cycle == sample.Cycle ? sample.Time : playback.Content.Duration;
                        bool crossesStart = tree.Clip.StartTime > from ||
                            tree.Clip.StartTime == from && (!state.InitialBoundaryConsumed || cycle > state.Cycle);
                        bool align = !traverse && cycle == sample.Cycle &&
                            to >= tree.Clip.StartTime && (dynamic || to < tree.Clip.EndTime);
                        if (activeCycle < 0 && cycle > exitCycle && to >= tree.Clip.StartTime && (crossesStart && traverse || align))
                        {
                            activeCycle = cycle;
                            output.Add(tree.Request(TimelineRuntimeTreeClipEventKind.Enter, tree.Clip.StartTime, cycle, playback.Generation));
                        }
                        if (activeCycle >= 0 && (activeCycle < cycle || !dynamic && to >= tree.Clip.EndTime ||
                            cycle < sample.Cycle || sample.EndsPlayback && cycle == sample.Cycle))
                        {
                            FixedScalar exitTime = dynamic ? to : FixedScalar.Min(to, tree.Clip.EndTime);
                            output.Add(tree.Request(TimelineRuntimeTreeClipEventKind.Exit, exitTime, activeCycle, playback.Generation));
                            exitCycle = activeCycle;
                            activeCycle = -1;
                        }
                    }
                    if (activeCycle >= 0)
                    {
                        var update = tree.Request(TimelineRuntimeTreeClipEventKind.Update, sample.Time, activeCycle, playback.Generation);
                        if (sample.Reason != TimelinePresentationSampleReason.Paused)
                            output.Add(update);
                        state.Candidate.ActiveTreeClips.Add(update);
                    }
                }
                state.PendingTreeCycles[index] = activeCycle;
                state.PendingTreeExitCycles[index] = exitCycle;
            }
        }

        sealed class PresentationPlaybackState
        {
            readonly Stack<PresentationPlaybackState> m_Recycled;

            public PresentationPlaybackState(
                TimelineData timeline,
                TimelineContentUnit content,
                TimelinePlaybackMode mode,
                Stack<PresentationPlaybackState> recycled)
            {
                m_Recycled = recycled;
                int markerCount = content.Markers.Count;
                Candidate = new TimelineRuntimePresentationBuffer(timeline, content, mode);
                Accepted = new TimelineRuntimePresentationBuffer(timeline, content, mode);
                MarkerLastTraversal = new ulong[markerCount];
                PendingMarkerLastTraversal = new ulong[markerCount];
                var trees = new List<PresentationTree>();
                for (int index = 0; index < content.Clips.Count; index++)
                {
                    TimelineContentClip clip = content.Clips[index];
                    if (!clip.ExecutionPolicy.IsPresentation || clip.TrackMuted ||
                        !TimelineRuntimeEvaluator.TryResolveTreeClip(timeline, clip.AuthoringId, out TreeClip tree))
                        continue;
                    if (!TimelineRuntimeEvaluator.TryGetTreeContract(content, tree, out string graphId, out string revision))
                        throw new InvalidOperationException("Presentation TreeClip has no prepared graph dependency.");
                    trees.Add(new PresentationTree(tree, graphId, revision));
                }
                Trees = trees.ToArray();
                TreeCycles = new int[Trees.Length];
                PendingTreeCycles = new int[Trees.Length];
                TreeExitCycles = new int[Trees.Length];
                PendingTreeExitCycles = new int[Trees.Length];
                Array.Fill(TreeCycles, -1);
                Array.Fill(PendingTreeCycles, -1);
                Array.Fill(TreeExitCycles, -1);
                Array.Fill(PendingTreeExitCycles, -1);
            }

            public readonly PresentationTree[] Trees;
            public readonly int[] TreeCycles;
            public readonly int[] PendingTreeCycles;
            public readonly int[] TreeExitCycles;
            public readonly int[] PendingTreeExitCycles;
            public TimelineRuntimePresentationBuffer Candidate;
            public TimelineRuntimePresentationBuffer Accepted;
            public readonly ulong[] MarkerLastTraversal;
            public readonly ulong[] PendingMarkerLastTraversal;
            public FixedScalar PendingTime;
            public int PendingCycle;
            public bool PendingFinished;
            public bool PendingInitialBoundaryConsumed;
            public TimelineRuntimePresentationFrame PendingFrame;
            public bool HasPendingFrame;
            public ulong Generation { get; private set; }
            public FixedScalar CursorTime;
            public int Cycle;
            public bool InitialBoundaryConsumed;
            public bool Finished;
            public TimelineRuntimePresentationFrame CachedFrame;
            public ulong LastPresentationFrame;
            public bool HasCachedFrame;

            public void Restart(ulong generation)
            {
                Generation = generation;
                Array.Clear(MarkerLastTraversal, 0, MarkerLastTraversal.Length);
                Array.Clear(PendingMarkerLastTraversal, 0, PendingMarkerLastTraversal.Length);
                PendingTime = default;
                PendingCycle = 0;
                PendingFinished = false;
                PendingInitialBoundaryConsumed = false;
                PendingFrame = default;
                HasPendingFrame = false;
                CursorTime = default;
                Cycle = 0;
                InitialBoundaryConsumed = false;
                Finished = false;
                CachedFrame = default;
                LastPresentationFrame = 0;
                HasCachedFrame = false;
            }

            public void Recycle()
            {
                Clear();
                m_Recycled.Push(this);
            }

            public void Cache(TimelineRuntimePresentationFrame frame)
            {
                TimelineRuntimePresentationBuffer recycled = Accepted;
                Accepted = Candidate;
                Candidate = recycled;
                Candidate.Clear();
                CachedFrame = frame;
                LastPresentationFrame = frame.PresentationFrame;
                HasCachedFrame = true;
            }

            public void Clear()
            {
                Candidate.Clear();
                Accepted.Clear();
                Array.Fill(TreeCycles, -1);
                Array.Fill(PendingTreeCycles, -1);
                Array.Fill(TreeExitCycles, -1);
                Array.Fill(PendingTreeExitCycles, -1);
            }

        }


    }

    public sealed class TimelineRuntimeEvaluationFanout : ITimelineRuntimeEvaluationSink
    {
        readonly ReadOnlyCollection<ITimelineRuntimeEvaluationSink> m_Sinks;

        public TimelineRuntimeEvaluationFanout(IEnumerable<ITimelineRuntimeEvaluationSink> sinks)
        {
            var values = new List<ITimelineRuntimeEvaluationSink>(sinks ?? throw new ArgumentNullException(nameof(sinks)));
            if (values.Count == 0)
                throw new ArgumentException("Timeline evaluation fanout requires at least one sink.", nameof(sinks));
            for (int index = 0; index < values.Count; index++)
                if (values[index] == null)
                    throw new ArgumentException("Timeline evaluation fanout contains a null sink.", nameof(sinks));
            m_Sinks = new ReadOnlyCollection<ITimelineRuntimeEvaluationSink>(values);
        }

        public bool Consume(TimelineRuntimeStepContext context)
        {
            bool accepted = true;
            for (int index = 0; index < m_Sinks.Count; index++)
                accepted &= m_Sinks[index].Consume(context);
            return accepted;
        }

        public void Commit(TimelineRuntimeStepContext context)
        {
            for (int index = 0; index < m_Sinks.Count; index++)
                m_Sinks[index].Commit(context);
        }

        public void Discard(TimelineRuntimeStepContext context)
        {
            for (int index = 0; index < m_Sinks.Count; index++)
                m_Sinks[index].Discard(context);
        }

        public bool ConsumeStop(TimelineRuntimeStopRequest request)
        {
            bool accepted = true;
            for (int index = 0; index < m_Sinks.Count; index++)
                accepted &= m_Sinks[index].ConsumeStop(request);
            return accepted;
        }

        public void CommitStop(TimelineRuntimeStopRequest request)
        {
            for (int index = 0; index < m_Sinks.Count; index++)
                m_Sinks[index].CommitStop(request);
        }

        public void DiscardStop(TimelineRuntimeStopRequest request)
        {
            for (int index = 0; index < m_Sinks.Count; index++)
                m_Sinks[index].DiscardStop(request);
        }
    }

    public sealed class TimelineRuntimeComposition : IDisposable
    {
        readonly TimelineRuntimeService m_Service;

        public TimelineRuntimeComposition(
            TimelineRuntimeNumericTarget numericTarget,
            ITimelineRuntimeCallBindingSource callBindingSource,
            ITimelineDomainBindingResolver domainResolver,
            ITimelineRuntimeDependencyResolver dependencyResolver,
            ITimelineRuntimeEvaluationSink evaluationSink,
            ITimelineRuntimeTreeClipService treeClipService,
            ITimelineRuntimeMarkerService markerService,
            int tickRate)
        {
            if (callBindingSource == null)
                throw new ArgumentNullException(nameof(callBindingSource));
            if (domainResolver == null)
                throw new ArgumentNullException(nameof(domainResolver));
            if (dependencyResolver == null)
                throw new ArgumentNullException(nameof(dependencyResolver));
            if (evaluationSink == null)
                throw new ArgumentNullException(nameof(evaluationSink));
            if (treeClipService == null)
                throw new ArgumentNullException(nameof(treeClipService));
            if (markerService == null)
                throw new ArgumentNullException(nameof(markerService));
            var requestFactory = new TimelineRuntimePlaybackRequestFactory(
                numericTarget,
                domainResolver,
                dependencyResolver,
                callBindingSource);
            var consumer = new TimelineRuntimeExecutionConsumer(
                evaluationSink,
                treeClipService,
                markerService);
            m_Service = new TimelineRuntimeService(requestFactory, consumer, consumer, tickRate);
        }

        public TimelineRuntimeService Service => m_Service;

        public TimelineRuntimePreparationResult Prepare(
            string requestId,
            TimelineData timeline,
            TimelineExecutionIdentity executionIdentity,
            TimelinePlaybackMode playbackMode)
        {
            var request = new TimelineRuntimePrepareRequest(
                requestId,
                m_Service.GetContent(timeline),
                executionIdentity,
                playbackMode);
            return m_Service.Prepare(request);
        }

        public TimelineRuntimePlaybackHandle CreatePlayback(
            TimelineRuntimePreparationResult preparation)
        {
            return m_Service.CreatePlayback(preparation);
        }

        public bool Start(TimelineRuntimePlaybackHandle handle)
        {
            return m_Service.Start(handle);
        }

        public TimelineRuntimeAdvanceResult Advance(
            TimelineRuntimePlaybackHandle handle,
            ulong logicTick,
            int tickCount,
            AbilityTimelinePlaybackControl control)
        {
            return m_Service.Advance(handle, logicTick, tickCount, control);

        }

        public bool CommitAdvance(
            TimelineRuntimePlaybackHandle handle,
            TimelineRuntimeAdvanceResult advance)
        {
            return m_Service.CommitAdvance(handle, advance);
        }

        public bool DiscardAdvance(
            TimelineRuntimePlaybackHandle handle,
            TimelineRuntimeAdvanceResult advance)
        {
            return m_Service.DiscardAdvance(handle, advance);
        }

        public void Stop(
            TimelineRuntimePlaybackHandle handle,
            TimelinePlaybackStopContext stopContext)
        {
            m_Service.CancelTimelinePlayback(
                new TimelinePlaybackHandle(handle.Value),
                stopContext);
        }

        public TimelineRuntimePlaybackSnapshot Capture(
            TimelineRuntimePlaybackHandle handle)
        {
            return m_Service.Capture(handle);
        }

        public TimelineRuntimeRestoreCandidate PrepareRestore(
            TimelineRuntimePlaybackSnapshot snapshot,
            TimelineRuntimePreparationResult preparation)
        {
            return m_Service.PrepareRestore(snapshot, preparation);
        }

        public TimelineRuntimePlaybackHandle ApplyRestore(
            TimelineRuntimeRestoreCandidate candidate)
        {
            return m_Service.ApplyRestore(candidate);
        }
        public bool RequestTimelinePlayback(
            TimelineData timeline,
            string sourceId,
            string sourceName,
            TimelinePlaybackActionContext actionContext,
            TimelinePlaybackMode playbackMode,
            out TimelinePlaybackHandle handle)
        {
            return m_Service.RequestTimelinePlayback(
                timeline,
                sourceId,
                sourceName,
                actionContext,
                playbackMode,
                out handle);
        }

        public TimelinePlaybackStatus GetTimelinePlaybackStatus(TimelinePlaybackHandle handle)
        {
            return m_Service.GetTimelinePlaybackStatus(handle);
        }

        public void CancelTimelinePlayback(
            TimelinePlaybackHandle handle,
            TimelinePlaybackStopContext stopContext)
        {
            m_Service.CancelTimelinePlayback(handle, stopContext);
        }

        public void Dispose()
        {
            m_Service.Dispose();
        }
    }
}
