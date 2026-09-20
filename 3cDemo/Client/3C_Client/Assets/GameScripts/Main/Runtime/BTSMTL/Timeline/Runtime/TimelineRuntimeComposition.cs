using System;
using ThirdPersonSimulation.Fixed;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BTSMTL.Diagnostics;
using ThirdPersonSimulation;
using TreeDesigner;

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

        public bool TryGetCallBindings(
            TimelineData timeline,
            string sourceId,
            string sourceName,
            TimelinePlaybackActionContext actionContext,
            out IReadOnlyList<TimelineCallBinding> bindings,
            out string error)
        {
            bindings = m_CallBindings;
            error = string.Empty;
            if (m_ActionContext.IsValid && actionContext.IsValid &&
                m_ActionContext.ActionInstanceId != actionContext.ActionInstanceId)
            {
                bindings = Array.Empty<TimelineCallBinding>();
                error = "timeline_action_context_mismatch";
                return false;
            }
            return true;
        }

        public bool TryGetTimelinePlaybackActionContext(
            ActionContextSlot actionContext,
            out TimelinePlaybackActionContext playbackActionContext)
        {
            playbackActionContext = m_ActionContext;
            return playbackActionContext.IsValid;
        }
    }

    public sealed class TimelineRuntimeBindingTable :
        ITimelineDomainBindingResolver,
        ITimelineRuntimeDependencyResolver
    {
        readonly Dictionary<string, TimelineBindingHandle> m_Bindings =
            new Dictionary<string, TimelineBindingHandle>(StringComparer.Ordinal);
        readonly Dictionary<string, TimelineRuntimeDependencyHandle> m_Dependencies =
            new Dictionary<string, TimelineRuntimeDependencyHandle>(StringComparer.Ordinal);

        public void Bind(string bindingId, TimelineBindingHandle handle)
        {
            if (!handle.IsValid)
                throw new ArgumentOutOfRangeException(nameof(handle));
            m_Bindings[TimelineRuntimeIdentity.Require(bindingId, nameof(bindingId))] = handle;
        }

        public void BindDependency(string dependencyIdentity, TimelineRuntimeDependencyHandle handle)
        {
            if (!handle.IsValid)
                throw new ArgumentOutOfRangeException(nameof(handle));
            m_Dependencies[TimelineRuntimeIdentity.Require(dependencyIdentity, nameof(dependencyIdentity))] = handle;
        }

        public bool TryResolve(
            TimelineBindingDeclaration declaration,
            TimelineBindingValue callValue,
            out TimelineBindingHandle handle,
            out string error)
        {
            if (!m_Bindings.TryGetValue(declaration.BindingId, out handle))
            {
                error = $"binding '{declaration.BindingId}' is not installed";
                return false;
            }
            error = string.Empty;
            return true;
        }

        public bool TryResolve(
            TimelineContentDependency dependency,
            TimelineRuntimeNumericTarget numericTarget,
            out TimelineRuntimeDependencyHandle handle,
            out string error)
        {
            if (!m_Dependencies.TryGetValue(dependency.Identity, out handle))
            {
                error = $"dependency '{dependency.Identity}' is not installed for '{numericTarget}'";
                return false;
            }
            error = string.Empty;
            return true;
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
                state.Clear();
                m_Playbacks.Remove(handle.Value);
                hasState = false;
            }
            if (sample.Reason == TimelinePresentationSampleReason.Stopped || sample.Reason == TimelinePresentationSampleReason.Withdrawn)
            {
                frame = default;
                return false;
            }
            if (!hasState)
            {
                state = new PresentationPlaybackState(playback);
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

            if (state.Finished && sample.Time == state.CursorTime && sample.Cycle == state.Cycle)
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
            TimelineRuntimePresentationOperations operations = TimelineRuntimePresentationEvaluator.Evaluate(
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
            state.PendingTime = currentTime;
            state.PendingCycle = currentCycle;
            state.PendingFinished = sample.Reason == TimelinePresentationSampleReason.Completed;
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

        public void CommitPresentationFrame(ulong presentationFrame)
        {
            foreach (PresentationPlaybackState state in m_Playbacks.Values)
            {
                if (!state.HasPendingFrame || state.PendingFrame.PresentationFrame != presentationFrame)
                    continue;
                Array.Copy(state.PendingMarkerLastTraversal, state.MarkerLastTraversal, state.MarkerLastTraversal.Length);
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
                state.Clear();
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

        sealed class PresentationPlaybackState
        {
            public PresentationPlaybackState(TimelineRuntimePlayback playback)
            {
                Generation = playback.Generation;
                int markerCount = playback.Content.Markers.Count;
                Candidate = new TimelineRuntimePresentationBuffer(playback);
                Accepted = new TimelineRuntimePresentationBuffer(playback);
                MarkerLastTraversal = new ulong[markerCount];
                PendingMarkerLastTraversal = new ulong[markerCount];
            }

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
            public ulong Generation { get; }
            public FixedScalar CursorTime;
            public int Cycle;
            public bool InitialBoundaryConsumed;
            public bool Finished;
            public TimelineRuntimePresentationFrame CachedFrame;
            public ulong LastPresentationFrame;
            public bool HasCachedFrame;

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

    public sealed class TimelineRuntimeComposition : ITimelinePlaybackService, IDisposable
    {
        readonly TimelineContractCatalog m_ContractCatalog;
        readonly TimelineRuntimeNumericTarget m_NumericTarget;
        readonly ITimelineDomainBindingResolver m_DomainResolver;
        readonly ITimelineRuntimeDependencyResolver m_DependencyResolver;
        readonly TimelineRuntimeService m_Service;

        public TimelineRuntimeComposition(
            TimelineContractCatalog contractCatalog,
            TimelineRuntimeNumericTarget numericTarget,
            ITimelineRuntimeCallBindingSource callBindingSource,
            ITimelineDomainBindingResolver domainResolver,
            ITimelineRuntimeDependencyResolver dependencyResolver,
            ITimelineRuntimeEvaluationSink evaluationSink,
            ITimelineRuntimeTreeClipService treeClipService,
            ITimelineRuntimeMarkerService markerService,
            int tickRate)
        {
            if (contractCatalog == null)
                throw new ArgumentNullException(nameof(contractCatalog));
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
            m_ContractCatalog = contractCatalog;
            m_NumericTarget = numericTarget;
            m_DomainResolver = domainResolver;
            m_DependencyResolver = dependencyResolver;
            var requestFactory = new TimelineRuntimePlaybackRequestFactory(
                contractCatalog,
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
            TimelinePlaybackMode playbackMode,
            IEnumerable<TimelineCallBinding> callBindings)
        {
            var request = new TimelineRuntimePrepareRequest(
                requestId,
                timeline,
                m_ContractCatalog,
                executionIdentity,
                playbackMode,
                m_NumericTarget,
                callBindings,
                m_DomainResolver,
                m_DependencyResolver);
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
            TreeExecutionActivationScope sourceActivation,
            BaseGraph sourceRuntimeGraph,
            out TimelinePlaybackHandle handle)
        {
            return m_Service.RequestTimelinePlayback(
                timeline,
                sourceId,
                sourceName,
                actionContext,
                playbackMode,
                sourceActivation,
                sourceRuntimeGraph,
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
