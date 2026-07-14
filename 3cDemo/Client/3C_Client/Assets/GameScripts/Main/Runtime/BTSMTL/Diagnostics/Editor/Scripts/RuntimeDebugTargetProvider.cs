using System;
using System.Collections.Generic;

namespace BTSMTL.Diagnostics.Editor
{
    internal sealed class RuntimeDebugTargetProvider
    {
        readonly RuntimeDiagnosticsTarget m_Target;
        readonly RuntimeDebugSourceMapSnapshot m_SourceMap;
        readonly RuntimeDebugViewModel m_LiveModel;
        long m_LiveCursor;
        long m_CaptureCursor;
        RuntimeTraceChannel m_LastChannels;

        public RuntimeDebugTargetProvider(RuntimeDiagnosticsTarget target)
        {
            m_Target = target ?? throw new ArgumentNullException(nameof(target));
            m_SourceMap = RuntimeDebugSourceMapSnapshot.Capture(target.SourceMap);
            m_LastChannels = target.Store.EffectiveChannels;
            m_LiveModel = new RuntimeDebugViewModel(new RuntimeDebugTargetInfo(target), m_SourceMap, m_LastChannels);
        }

        public RuntimeDebugViewModel LiveModel => m_LiveModel;
        public RuntimeDiagnosticsTarget Target => m_Target;
        public long CaptureVersion { get; private set; }
        public int CaptureEventCount { get; private set; }

        public bool Refresh()
        {
            RuntimeLiveStateRead read = m_Target.Store.ReadLiveStateSince(m_LiveCursor);
            RuntimeCaptureRead capture = m_Target.Store.ReadCaptureSince(m_CaptureCursor);
            RuntimeTraceChannel channels = m_Target.Store.EffectiveChannels;
            bool stateChanged = read.Version != m_LiveCursor;
            bool captureChanged = capture.Version != m_CaptureCursor;
            bool channelChanged = channels != m_LastChannels;
            if (!stateChanged && !captureChanged && !channelChanged)
                return false;

            if (stateChanged || channelChanged || captureChanged)
            {
                m_LiveModel.BeginUpdate(stateChanged && read.RequiresFullSync);
                if (stateChanged)
                {
                    for (int i = 0; i < read.Changes.Count; i++)
                    {
                        RuntimeLiveStateChange change = read.Changes[i];
                        m_LiveModel.Apply(change.Key, change.TraceEvent);
                    }
                }
                m_LiveModel.SetChannels(channels);
                m_LiveModel.CommitUpdate(captureChanged ? capture.Version : CaptureVersion);
                m_LiveCursor = read.Version;
            }
            m_LastChannels = channels;
            if (captureChanged)
            {
                m_CaptureCursor = capture.Version;
                CaptureVersion = capture.Version;
                CaptureEventCount = capture.RequiresFullSync
                    ? capture.Changes.Count
                    : CaptureEventCount + capture.Changes.Count;
            }
            return true;
        }

        public RuntimeCaptureSnapshot EndCapture()
        {
            return m_Target.Store.EndCapture();
        }

        public bool BeginCapture(RuntimeTraceChannel channels, RuntimeDiagnosticsCaptureDetail detail, out Guid captureId)
        {
            return m_Target.Store.BeginCapture(channels, detail, out captureId);
        }

        public RuntimeDebugFrozenDiagnostics Freeze()
        {
            Refresh();
            return new RuntimeDebugFrozenDiagnostics(m_LiveModel, m_SourceMap, m_Target.Revision, m_Target.Store.FreezeActiveCapture());
        }

        public RuntimeDebugViewModel BuildCaptureView(RuntimeCaptureSnapshot snapshot, int historyOffset)
        {
            return new RuntimeDebugFrozenDiagnostics(m_LiveModel, m_SourceMap, m_Target.Revision, null)
                .BuildCaptureView(snapshot, historyOffset);
        }
    }

    internal sealed class RuntimeDebugFrozenDiagnostics
    {
        readonly RuntimeDebugViewModel m_LiveModel;
        readonly RuntimeDebugSourceMapSnapshot m_SourceMap;
        readonly RuntimeProgramRevision m_Revision;

        public RuntimeDebugFrozenDiagnostics(
            RuntimeDebugViewModel liveModel,
            RuntimeDebugSourceMapSnapshot sourceMap,
            RuntimeProgramRevision revision,
            RuntimeCaptureSnapshot activeCapture)
        {
            m_LiveModel = liveModel ?? RuntimeDebugViewModel.Detached;
            m_SourceMap = sourceMap ?? RuntimeDebugSourceMapSnapshot.Empty;
            m_Revision = revision;
            ActiveCapture = activeCapture;
        }

        public RuntimeDebugViewModel LiveModel => m_LiveModel;
        public RuntimeCaptureSnapshot ActiveCapture { get; }

        public RuntimeDebugTargetMatch MatchSource(RuntimeDebugTargetRequest request)
        {
            return m_SourceMap.Match(request);
        }

        public RuntimeDebugViewModel BuildCaptureView(RuntimeCaptureSnapshot snapshot, int historyOffset)
        {
            if (snapshot == null)
                return m_LiveModel;

            var view = new RuntimeDebugViewModel(m_LiveModel.Target, m_SourceMap, snapshot.Channels);
            view.BeginUpdate(true);
            IReadOnlyList<RuntimeTraceEvent> events = snapshot.GetEvents(historyOffset);
            for (int i = 0; i < events.Count; i++)
            {
                RuntimeTraceEvent traceEvent = events[i];
                var key = new RuntimeLiveStateKey(traceEvent.Channel, traceEvent.Source, traceEvent.RuntimeInstance, traceEvent.Kind);
                view.Apply(key, traceEvent);
            }
            view.CommitUpdate();
            return view;
        }
    }
}
