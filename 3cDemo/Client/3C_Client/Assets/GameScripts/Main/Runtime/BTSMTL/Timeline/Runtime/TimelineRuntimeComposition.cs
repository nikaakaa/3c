using System;
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
            PreviousFrame = context.Advance.PreviousFrame;
            Frame = context.Advance.Frame;
            PreviousCycle = context.Advance.PreviousCycle;
            Cycle = context.Advance.Cycle;
            ContentIdentity = context.Playback.Content.Identity;
            ContentRevision = context.Playback.Content.ContentHash;
            ExecutionIdentity = context.Playback.ExecutionIdentity;
            Evaluation = context.Advance.Evaluation;
        }

        public TimelineRuntimePlaybackHandle Handle { get; }
        public ulong Generation { get; }
        public ulong LogicTick { get; }
        public int PreviousFrame { get; }
        public int Frame { get; }
        public int PreviousCycle { get; }
        public int Cycle { get; }
        public string ContentIdentity { get; }
        public string ContentRevision { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public TimelineRuntimeEvaluationResult Evaluation { get; }
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
            if (m_Committed.TryGetValue(handle.Value, out TimelineRuntimeCommittedEvaluation committed))
            {
                result = committed.Evaluation;
                return true;
            }
            result = null;
            return false;
        }

        public bool TryGetCommittedEvaluation(
            TimelineRuntimePlaybackHandle handle,
            out TimelineRuntimeCommittedEvaluation evaluation)
        {
            return m_Committed.TryGetValue(handle.Value, out evaluation);
        }

        public void Clear()
        {
            m_Pending.Clear();
            m_Committed.Clear();
        }
    }

    public sealed class TimelineRuntimePresentationDriver : ITimelineRuntimeEvaluationSink
    {
        static readonly TimelineRuntimePresentationOperations s_EmptyOperations =
            new TimelineRuntimePresentationOperations(
                Array.Empty<TimelineAnimationContribution>(),
                Array.Empty<TimelineCameraStateSample>(),
                Array.Empty<TimelineCameraCueSample>(),
                Array.Empty<TimelineCameraResponseSample>(),
                Array.Empty<TimelineCameraResourceSample>(),
                Array.Empty<TimelineRuntimeScenePresentationSample>());

        readonly Dictionary<ulong, PresentationPlaybackState> m_Playbacks =
            new Dictionary<ulong, PresentationPlaybackState>();

        public bool TryPresent(
            TimelineRuntimeService service,
            TimelineRuntimePlaybackHandle handle,
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

            bool hasState = m_Playbacks.TryGetValue(handle.Value, out PresentationPlaybackState state);
            if (!hasState)
            {
                if (playback.State == TimelineRuntimePlaybackState.Stopping ||
                    playback.State == TimelineRuntimePlaybackState.Stopped ||
                    playback.State == TimelineRuntimePlaybackState.Disposed)
                {
                    frame = default;
                    return false;
                }
                state = new PresentationPlaybackState(playback.Generation);
                m_Playbacks.Add(handle.Value, state);
            }

            var events = new List<TimelineRuntimePresentationEvent>();
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

            if (state.Finished)
            {
                m_Playbacks.Remove(handle.Value);
                frame = default;
                return false;
            }

            int maxFrame = Math.Max(0, playback.Content.MaxFrame);
            bool loop = playback.PlaybackMode == TimelinePlaybackMode.Loop;
            float previousFrame = state.CursorFrame;
            int previousCycle = state.Cycle;
            AdvanceCursor(
                state,
                maxFrame,
                loop,
                playback.Content.FrameRate,
                presentationDeltaSeconds,
                out float currentFrame,
                out int currentCycle);
            TimelineRuntimePresentationOperations operations = TimelineRuntimePresentationEvaluator.Evaluate(
                playback,
                previousFrame,
                previousCycle,
                currentFrame,
                currentCycle,
                loop,
                !state.HasPresented);
            AppendMarkerEvents(
                playback,
                state,
                previousFrame,
                previousCycle,
                currentFrame,
                currentCycle,
                loop,
                !state.HasPresented,
                events);
            state.CursorFrame = currentFrame;
            state.Cycle = currentCycle;
            state.HasPresented = true;
            if (!loop && currentFrame >= maxFrame)
                state.Finished = true;
            frame = new TimelineRuntimePresentationFrame(
                playback,
                presentationFrame,
                presentationDeltaSeconds,
                interpolationAlpha,
                operations,
                events);
            state.Cache(frame);
            return true;
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
            if (m_Playbacks.TryGetValue(request.Handle.Value, out PresentationPlaybackState state))
            {
                state.StopCommitted = true;
                state.ClearCachedFrame();
            }
        }

        public void DiscardStop(TimelineRuntimeStopRequest request)
        {
        }

        public void Clear()
        {
            m_Playbacks.Clear();
        }

        static void AdvanceCursor(
            PresentationPlaybackState state,
            int maxFrame,
            bool loop,
            int frameRate,
            float deltaSeconds,
            out float currentFrame,
            out int currentCycle)
        {
            currentFrame = state.CursorFrame;
            currentCycle = state.Cycle;
            if (maxFrame <= 0)
                return;
            double requested = state.CursorFrame + deltaSeconds * Math.Max(1, frameRate);
            if (loop)
            {
                long cycleDelta = (long)Math.Floor(requested / maxFrame);
                if (cycleDelta > 4096 || cycleDelta > int.MaxValue - state.Cycle)
                    throw new InvalidOperationException("Timeline presentation cursor crossed an unsupported cycle range.");
                currentCycle = state.Cycle + (int)cycleDelta;
                currentFrame = (float)(requested - cycleDelta * maxFrame);
                return;
            }
            currentFrame = (float)Math.Min(requested, maxFrame);
        }

        static void AppendMarkerEvents(
            TimelineRuntimePlayback playback,
            PresentationPlaybackState state,
            float previousFrame,
            int previousCycle,
            float currentFrame,
            int currentCycle,
            bool loop,
            bool includeStartBoundary,
            List<TimelineRuntimePresentationEvent> events)
        {
            int maxFrame = Math.Max(0, playback.Content.MaxFrame);
            if (maxFrame <= 0)
                return;
            double previousAbsolute = previousCycle * (double)maxFrame + previousFrame;
            double currentAbsolute = currentCycle * (double)maxFrame + currentFrame;
            if (currentAbsolute < previousAbsolute)
                throw new InvalidOperationException("Timeline presentation cursor moved backward without a generation reset.");
            int firstCycle = loop ? previousCycle : 0;
            int lastCycle = loop ? currentCycle : 0;
            var transitions = new List<MarkerTransition>();
            for (int markerIndex = 0; markerIndex < playback.Content.Markers.Count; markerIndex++)
            {
                TimelineContentMarker marker = playback.Content.Markers[markerIndex];
                if (!marker.ExecutionPolicy.IsPresentation || marker.TrackMuted)
                    continue;
                for (int cycle = firstCycle; cycle <= lastCycle; cycle++)
                {
                    double absolute = cycle * (double)maxFrame + marker.Frame;
                    if (Crosses(absolute, previousAbsolute, currentAbsolute, includeStartBoundary))
                        transitions.Add(new MarkerTransition(marker, cycle, marker.Frame, absolute));
                }
            }
            transitions.Sort(MarkerTransition.Compare);
            for (int index = 0; index < transitions.Count; index++)
            {
                MarkerTransition transition = transitions[index];
                string markerId = transition.Marker.MarkerId;
                if (!state.MarkerTraversal.TryGetValue(markerId, out ulong traversalIndex))
                    traversalIndex = 1;
                if (traversalIndex == 0)
                    throw new InvalidOperationException($"Timeline presentation marker '{markerId}' traversal identity is exhausted.");
                state.MarkerTraversal[markerId] = traversalIndex + 1;
                events.Add(new TimelineRuntimePresentationEvent(
                    playback.Handle,
                    playback.ExecutionIdentity,
                    playback.Generation,
                    markerId,
                    traversalIndex,
                    transition.Marker.GraphId,
                    transition.Marker.GraphRevision,
                    transition.Frame,
                    transition.Cycle));
            }
        }

        static bool Crosses(
            double position,
            double previousPosition,
            double currentPosition,
            bool includeStartBoundary)
        {
            return includeStartBoundary && position == previousPosition ||
                   position > previousPosition && position <= currentPosition;
        }

        sealed class PresentationPlaybackState
        {
            public PresentationPlaybackState(ulong generation)
            {
                Generation = generation;
            }

            public readonly Dictionary<string, ulong> MarkerTraversal =
                new Dictionary<string, ulong>(StringComparer.Ordinal);
            public ulong Generation { get; }
            public float CursorFrame;
            public int Cycle;
            public bool HasPresented;
            public bool StopCommitted;
            public bool Finished;
            public TimelineRuntimePresentationFrame CachedFrame;
            public ulong LastPresentationFrame;
            public bool HasCachedFrame;

            public void Cache(TimelineRuntimePresentationFrame frame)
            {
                CachedFrame = frame;
                LastPresentationFrame = frame.PresentationFrame;
                HasCachedFrame = true;
            }

            public void ClearCachedFrame()
            {
                CachedFrame = default;
                LastPresentationFrame = 0;
                HasCachedFrame = false;
            }
        }

        readonly struct MarkerTransition
        {
            public MarkerTransition(
                TimelineContentMarker marker,
                int cycle,
                int frame,
                double absolute)
            {
                Marker = marker;
                Cycle = cycle;
                Frame = frame;
                Absolute = absolute;
            }

            public TimelineContentMarker Marker { get; }
            public int Cycle { get; }
            public int Frame { get; }
            public double Absolute { get; }

            public static int Compare(MarkerTransition left, MarkerTransition right)
            {
                int absolute = left.Absolute.CompareTo(right.Absolute);
                if (absolute != 0)
                    return absolute;
                return string.CompareOrdinal(left.Marker.MarkerId, right.Marker.MarkerId);
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
            int tickCount)
        {
            return m_Service.Advance(handle, logicTick, tickCount);

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
