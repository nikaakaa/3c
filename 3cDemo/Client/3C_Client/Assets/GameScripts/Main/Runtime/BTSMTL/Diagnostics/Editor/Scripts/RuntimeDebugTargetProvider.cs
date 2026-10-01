using System;
using System.Collections.Generic;

namespace BTSMTL.Diagnostics.Editor
{
    internal sealed class RuntimeDebugTargetProvider
    {
        readonly RuntimeDiagnosticsTarget m_Target;
        readonly RuntimeDebugSourceMapSnapshot m_SourceMap;
        readonly RuntimeDebugViewModel m_LiveModel;
        readonly List<RuntimeLiveStateChange> m_LiveChanges;
        readonly List<RuntimeCaptureChange> m_CaptureChanges;
        long m_LiveCursor;
        Guid m_CaptureId;
        RuntimeExecutionTimelineBuilder.SpanAccumulator m_ExecutionSpans;
        RuntimeExecutionTimeline m_ExecutionTimeline;
        RuntimeTraceChannel m_LastChannels;

        public RuntimeDebugTargetProvider(RuntimeDiagnosticsTarget target)
        {
            m_Target = target;
            m_LiveChanges = new List<RuntimeLiveStateChange>(target.Store.LiveStateCapacity);
            m_CaptureChanges = new List<RuntimeCaptureChange>(target.Store.CaptureEventCapacity);
            m_SourceMap = RuntimeDebugSourceMapSnapshot.Capture(target.SourceMap);
            m_LastChannels = target.Store.EffectiveChannels;
            m_LiveModel = new RuntimeDebugViewModel(new RuntimeDebugTargetInfo(target), m_SourceMap, m_LastChannels, target.Store.LiveStateCapacity);
        }

        public RuntimeDebugViewModel LiveModel => m_LiveModel;
        public RuntimeDiagnosticsTarget Target => m_Target;
        public RuntimeDebugSourceMapSnapshot SourceMap => m_SourceMap;
        public Guid CaptureId => m_CaptureId;
        public long CaptureVersion { get; private set; }

        public bool Refresh(IReadOnlyDictionary<RuntimeContentRevision, RuntimeDebugSourceMapSnapshot> sourceMaps)
        {
            RuntimeLiveStateRead read = m_Target.Store.CopyLiveStateSince(m_LiveCursor, m_LiveChanges);
            var capture = m_Target.Store.CaptureStatus;
            RuntimeTraceChannel channels = m_Target.Store.EffectiveChannels;
            bool stateChanged = read.Version != m_LiveCursor;
            bool captureChanged = capture.CaptureId != m_CaptureId || capture.Version != CaptureVersion;
            bool channelChanged = channels != m_LastChannels;
            if (!stateChanged && !captureChanged && !channelChanged)
                return false;

            m_LiveModel.BeginUpdate(stateChanged && read.RequiresFullSync);
            if (stateChanged)
            {
                for (int i = 0; i < read.Changes.Count; i++)
                {
                    RuntimeLiveStateChange change = read.Changes[i];
                    m_LiveModel.Apply(change.Key, change.TraceEvent, sourceMaps[change.TraceEvent.ContentRevision]);
                }
            }
            m_LiveModel.SetChannels(channels);
            m_LiveModel.SetCoverage(read.EvictedStates, false);
            m_LiveModel.CommitUpdate(captureChanged ? capture.Version : CaptureVersion);
            m_LiveCursor = read.Version;
            m_LastChannels = channels;
            if (captureChanged)
            {
                m_CaptureId = capture.CaptureId;
                CaptureVersion = capture.Version;
            }
            return true;
        }

        public RuntimeExecutionTimeline ReadExecutionTimeline(
            IReadOnlyDictionary<RuntimeContentRevision, RuntimeDebugSourceMapSnapshot> sourceMaps)
        {
            Guid projectionCaptureId = m_ExecutionTimeline?.CaptureId ?? Guid.Empty;
            long projectionVersion = m_ExecutionTimeline?.Version ?? 0;
            RuntimeCaptureRead capture = m_Target.Store.CopyCaptureSince(
                projectionCaptureId, projectionVersion, m_CaptureChanges);
            if (capture.CaptureId == Guid.Empty)
            {
                m_ExecutionSpans = null;
                m_ExecutionTimeline = null;
            }
            else
            {
                if (capture.CaptureId != projectionCaptureId)
                {
                    m_ExecutionSpans = new RuntimeExecutionTimelineBuilder.SpanAccumulator(m_Target.Store.CaptureEventCapacity);
                    m_ExecutionTimeline = new RuntimeExecutionTimeline(
                        capture.CaptureId, capture.Channels, capture.Detail, capture.Version, capture.EvictedEvents,
                        false, 0, 0, 0, m_ExecutionSpans.ClockPositions, m_ExecutionSpans.Spans);
                }
                else if (capture.RequiresFullSync)
                    m_ExecutionSpans.Reset();
                for (int i = 0; i < m_CaptureChanges.Count; i++)
                    m_ExecutionSpans.Append(m_CaptureChanges[i].TraceEvent, m_SourceMap, sourceMaps);
                m_ExecutionTimeline.Update(capture, m_ExecutionSpans.IsComplete(capture.EvictedEvents),
                    m_ExecutionSpans.UnmappedEventCount, m_ExecutionSpans.LatestLogicPosition,
                    m_ExecutionSpans.LatestPresentationPosition);
            }
            return m_ExecutionTimeline;
        }

        public RuntimeCaptureSnapshot EndCapture()
        {
            return m_Target.Store.EndCapture();
        }

        public bool BeginCapture(RuntimeTraceChannel channels, RuntimeDiagnosticsCaptureDetail detail, out Guid captureId)
        {
            return m_Target.Store.BeginCapture(channels, detail, out captureId);
        }

        public RuntimeDebugFrozenDiagnostics Freeze(
            IReadOnlyDictionary<RuntimeContentRevision, RuntimeDebugSourceMapSnapshot> sourceMaps)
        {
            Refresh(sourceMaps);
            return new RuntimeDebugFrozenDiagnostics(
                m_LiveModel,
                m_SourceMap,
                m_Target.Store.FreezeActiveCapture(),
                sourceMaps);
        }

        public RuntimeDebugViewModel BuildCaptureView(
            RuntimeCaptureSnapshot snapshot,
            int historyOffset,
            IReadOnlyDictionary<RuntimeContentRevision, RuntimeDebugSourceMapSnapshot> sourceMaps,
            ulong throughSequence = ulong.MaxValue)
        {
            return new RuntimeDebugFrozenDiagnostics(
                m_LiveModel,
                m_SourceMap,
                null,
                sourceMaps)
                .BuildCaptureView(snapshot, historyOffset, throughSequence);
        }
    }

    internal sealed class RuntimeDebugFrozenDiagnostics
    {
        readonly RuntimeDebugViewModel m_LiveModel;
        readonly RuntimeDebugSourceMapSnapshot m_SourceMap;
        readonly IReadOnlyDictionary<RuntimeContentRevision, RuntimeDebugSourceMapSnapshot> m_SourceMaps;

        public RuntimeDebugFrozenDiagnostics(
            RuntimeDebugViewModel liveModel,
            RuntimeDebugSourceMapSnapshot sourceMap,
            RuntimeCaptureSnapshot activeCapture,
            IReadOnlyDictionary<RuntimeContentRevision, RuntimeDebugSourceMapSnapshot> sourceMaps)
        {
            m_LiveModel = liveModel;
            m_SourceMap = sourceMap;
            ActiveCapture = activeCapture;
            m_SourceMaps = sourceMaps;
        }

        public RuntimeDebugViewModel LiveModel => m_LiveModel;
        public RuntimeCaptureSnapshot ActiveCapture { get; }

        public RuntimeDebugTargetMatch MatchSource(RuntimeDebugTargetRequest request)
        {
            RuntimeDebugTargetMatch current = m_SourceMap.Match(request);
            if (current == RuntimeDebugTargetMatch.Exact)
                return current;
            foreach (RuntimeDebugSourceMapSnapshot sourceMap in m_SourceMaps.Values)
                if (sourceMap.Match(request) == RuntimeDebugTargetMatch.Exact)
                    return RuntimeDebugTargetMatch.Exact;
            return current;
        }

        public RuntimeDebugViewModel BuildCaptureView(RuntimeCaptureSnapshot snapshot, int historyOffset, ulong throughSequence = ulong.MaxValue)
        {
            if (snapshot == null)
                return m_LiveModel;

            ReadOnlySpan<RuntimeTraceEvent> events = snapshot.GetEvents(historyOffset, throughSequence);
            var view = new RuntimeDebugViewModel(m_LiveModel.Target, m_SourceMap, snapshot.Channels, events.Length);
            view.BeginUpdate(true);
            view.SetCoverage(0, snapshot.EvictedEvents != 0);
            for (int i = 0; i < events.Length; i++)
            {
                RuntimeTraceEvent traceEvent = events[i];
                var key = new RuntimeLiveStateKey(traceEvent);
                view.Apply(key, traceEvent, m_SourceMaps[traceEvent.ContentRevision]);
            }
            view.CommitUpdate();
            return view;
        }
    }
}
