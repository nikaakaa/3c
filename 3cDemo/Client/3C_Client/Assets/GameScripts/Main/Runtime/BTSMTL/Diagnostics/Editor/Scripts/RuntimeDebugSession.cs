using System;
using System.Collections.Generic;
using UnityEditor;

namespace BTSMTL.Diagnostics.Editor
{
    [InitializeOnLoad]
    public sealed class RuntimeDebugSession : IDisposable
    {
        const double CaptureRefreshIntervalSeconds = 0.1d;
        const double LiveRefreshIntervalSeconds = 1d / 30d;

        static readonly RuntimeDebugSession s_Shared;

        readonly Dictionary<object, LiveInterestLease> m_LiveInterests = new Dictionary<object, LiveInterestLease>();
        readonly Dictionary<RuntimeContentRevision, RuntimeDebugSourceMapSnapshot> m_SourceMaps = new Dictionary<RuntimeContentRevision, RuntimeDebugSourceMapSnapshot>();
        RuntimeDiagnosticsTarget m_Target;
        RuntimeDebugTargetProvider m_Provider;
        RuntimeDebugFrozenDiagnostics m_Frozen;
        RuntimeDebugViewModel m_ViewModel = RuntimeDebugViewModel.Detached;
        RuntimeCaptureSnapshot m_CaptureSnapshot;
        RuntimeDebugAttachmentState m_AttachmentState;
        ulong m_HistorySequence = ulong.MaxValue;
        int m_CaptureSegmentLimit;
        long m_TargetRevision;
        double m_NextCaptureRefreshTime;
        double m_NextLiveRefreshTime;
        bool m_Disposed;

        static RuntimeDebugSession()
        {
            s_Shared = new RuntimeDebugSession();
        }

        RuntimeDebugSession()
        {
            RuntimeDiagnosticsTargetRegistry.TargetRegistered += OnTargetChanged;
            RuntimeDiagnosticsTargetRegistry.TargetUnregistered += OnTargetUnregistered;
            EditorApplication.update += Update;
        }

        public static RuntimeDebugSession Shared => s_Shared;
        public event Action Changed;
        public RuntimeDebugViewModel ViewModel => m_ViewModel;
        public RuntimeDebugAttachmentState AttachmentState => m_AttachmentState;
        public long TargetRevision => m_TargetRevision;
        public bool CanControlLiveTarget => m_AttachmentState == RuntimeDebugAttachmentState.Live && m_Target != null;
        public bool CanResumeLiveTarget => m_Target != null && m_AttachmentState != RuntimeDebugAttachmentState.Ended;
        public bool CanStartCapture => CanControlLiveTarget;
        public bool CanStopCapture => m_Target != null && m_Target.Store.IsCaptureRecording;
        public bool IsCaptureRecording => m_Target != null && m_Target.Store.IsCaptureRecording;
        public RuntimeCaptureSnapshot CaptureSnapshot => m_CaptureSnapshot;
        public bool HasCaptureHistory => m_CaptureSnapshot != null;
        public int HistoryOffset => m_CaptureSnapshot?.GetHistoryOffset(m_HistorySequence) ?? 0;
        public ulong HistorySequence => m_HistorySequence;
        public int ExecutionSpanCapacity => m_Target != null
            ? m_Target.Store.CaptureEventCapacity
            : m_CaptureSnapshot != null ? m_CaptureSnapshot.GetEvents(0).Length : 0;
        public Guid CaptureId => m_AttachmentState is RuntimeDebugAttachmentState.CaptureHistory or RuntimeDebugAttachmentState.Ended
            ? m_CaptureSnapshot?.CaptureId ?? Guid.Empty
            : m_Provider?.CaptureId ?? Guid.Empty;
        public long CaptureVersion => m_AttachmentState is RuntimeDebugAttachmentState.CaptureHistory or RuntimeDebugAttachmentState.Ended
            ? m_CaptureSnapshot?.Version ?? 0
            : m_Provider?.CaptureVersion ?? 0;

        public RuntimeExecutionTimeline BuildExecutionTimeline(RuntimeInstanceKey instance = default)
        {
            if (!instance.IsValid && m_AttachmentState == RuntimeDebugAttachmentState.Live)
            {
                CaptureSourceMaps();
                return m_Provider.ReadExecutionTimeline(m_SourceMaps);
            }
            RuntimeCaptureSnapshot capture = GetExecutionCapture();
            if (capture == null)
                return null;
            RuntimeDebugSourceMapSnapshot currentMap = m_Target != null &&
                m_SourceMaps.TryGetValue(m_Target.Revision, out RuntimeDebugSourceMapSnapshot mapped)
                ? mapped
                : RuntimeDebugSourceMapSnapshot.Empty;
            return RuntimeExecutionTimelineBuilder.Build(
                capture,
                currentMap,
                m_SourceMaps,
                0,
                instance,
                m_HistorySequence);
        }

        public bool TrySeekExecutionPosition(RuntimeTraceDomain domain, ulong position, Guid branch, ulong epoch)
        {
            RuntimeCaptureSnapshot snapshot = GetExecutionCapture();
            if (snapshot == null)
                return false;
            ulong selectedSequence = 0;
            ReadOnlySpan<RuntimeCaptureSegmentSnapshot> segments = snapshot.Segments;
            for (int index = 0; index < segments.Length; index++)
            {
                RuntimeCaptureSegmentSnapshot segment = segments[index];
                RuntimeTraceDomain segmentDomain = segment.Domain == RuntimeTraceDomain.Lifecycle
                    ? RuntimeTraceDomain.Logic : segment.Domain;
                if (segmentDomain != domain || segment.Position > position)
                    continue;
                ReadOnlySpan<RuntimeTraceEvent> events = segment.Events;
                for (int eventIndex = 0; eventIndex < events.Length; eventIndex++)
                {
                    RuntimeTraceEvent trace = events[eventIndex];
                    if (trace.ExecutionBranchId == branch && trace.RuntimeEpoch == epoch)
                        selectedSequence = trace.Sequence;
                }
            }
            if (selectedSequence == 0)
                return false;
            m_CaptureSnapshot = snapshot;
            SetHistorySequence(selectedSequence);
            return true;
        }

        public RuntimeExecutionHistory BuildExecutionHistory(RuntimeInstanceKey instance = default)
        {
            RuntimeCaptureSnapshot capture = GetExecutionCapture();
            if (capture == null)
                return null;
            return m_ViewModel.BuildExecutionHistory(
                capture,
                0,
                instance,
                m_SourceMaps,
                m_HistorySequence);
        }

        public void BuildExecutionProjections(
            RuntimeInstanceKey instance,
            out RuntimeExecutionTimeline timeline,
            out RuntimeExecutionHistory history)
        {
            RuntimeCaptureSnapshot capture = GetExecutionCapture();
            if (capture == null)
            {
                timeline = null;
                history = null;
                return;
            }
            RuntimeDebugSourceMapSnapshot currentMap = m_Target != null &&
                m_SourceMaps.TryGetValue(m_Target.Revision, out RuntimeDebugSourceMapSnapshot mapped)
                ? mapped
                : RuntimeDebugSourceMapSnapshot.Empty;
            timeline = RuntimeExecutionTimelineBuilder.Build(
                capture,
                currentMap,
                m_SourceMaps,
                0,
                instance,
                m_HistorySequence);
            history = RuntimeExecutionTimelineBuilder.BuildHistory(
                capture,
                currentMap,
                m_SourceMaps,
                0,
                instance,
                m_HistorySequence);
        }

        public bool TryResolveHistoricalSource(
            RuntimeContentRevision revision,
            RuntimeSourceElementHandle handle,
            out RuntimeSourceElementKey source,
            out DebugSourceMapEntry entry)
        {
            if (m_SourceMaps.TryGetValue(revision, out RuntimeDebugSourceMapSnapshot sourceMap) &&
                sourceMap.TryGet(handle, out entry))
            {
                source = entry.Source;
                return source.IsValid;
            }
            source = default;
            entry = default;
            return false;
        }

        public int CaptureSegmentCount
        {
            get
            {
                if (m_Target != null && m_Target.Store.IsCaptureRecording)
                    return m_Target.Store.CaptureSegmentCount;
                return m_CaptureSnapshot?.SegmentCount ?? 0;
            }
        }

        public int CaptureSegmentCapacity
        {
            get
            {
                if (m_Target != null && m_Target.Store.IsCaptureRecording)
                    return m_Target.Store.CaptureSegmentCapacity;
                return 0;
            }
        }

        public IReadOnlyList<RuntimeDebugTargetInfo> Targets
        {
            get
            {
                IReadOnlyList<RuntimeDiagnosticsTarget> targets = RuntimeDiagnosticsTargetRegistry.Targets;
                RuntimeDebugTargetInfo[] result = targets.Count == 0
                    ? Array.Empty<RuntimeDebugTargetInfo>()
                    : new RuntimeDebugTargetInfo[targets.Count];
                for (int i = 0; i < targets.Count; i++)
                    result[i] = new RuntimeDebugTargetInfo(targets[i]);
                return result;
            }
        }

        public bool AttachToTarget(Guid characterRuntimeId)
        {
            if (!RuntimeDiagnosticsTargetRegistry.TryGet(characterRuntimeId, out RuntimeDiagnosticsTarget target))
                return false;
            Attach(target);
            return true;
        }

        public bool AttachToHost(int hostInstanceId)
        {
            if (!TryFindTargetByHost(hostInstanceId, out RuntimeDiagnosticsTarget target))
                return false;
            Attach(target);
            return true;
        }

        public void ClearTarget()
        {
            ReleaseLiveHandles();
            m_Target = null;
            m_Provider = null;
            m_SourceMaps.Clear();
            m_Frozen = null;
            m_ViewModel = RuntimeDebugViewModel.Detached;
            m_CaptureSnapshot = null;
            m_HistorySequence = ulong.MaxValue;
            m_CaptureSegmentLimit = 0;
            m_NextCaptureRefreshTime = 0d;
            m_NextLiveRefreshTime = 0d;
            m_AttachmentState = RuntimeDebugAttachmentState.Detached;
            m_TargetRevision++;
            NotifyChanged();
        }

        public void EnsureLiveInterest(object owner, RuntimeTraceChannel channels)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            channels &= RuntimeTraceChannel.All;
            if (channels == RuntimeTraceChannel.None)
            {
                ReleaseLiveInterest(owner);
                return;
            }

            if (!m_LiveInterests.TryGetValue(owner, out LiveInterestLease lease))
            {
                lease = new LiveInterestLease();
                m_LiveInterests.Add(owner, lease);
            }

            bool changed = lease.Channels != channels;
            if (changed)
            {
                ReleaseLiveHandle(lease);
                lease.Channels = channels;
            }

            if (CanControlLiveTarget && !lease.Handle.IsValid)
            {
                lease.Handle = m_Target.Store.AcquireInterest(new RuntimeDiagnosticsInterest(RuntimeDiagnosticsInterestKind.LiveState, lease.Channels));
                changed = true;
            }

            if (!changed)
                return;

            if (!RefreshProvider())
                NotifyChanged();
        }

        public void ReleaseLiveInterest(object owner)
        {
            if (owner == null || !m_LiveInterests.TryGetValue(owner, out LiveInterestLease lease))
                return;

            ReleaseLiveHandle(lease);
            m_LiveInterests.Remove(owner);
            if (!RefreshProvider())
                NotifyChanged();
        }

        public void FreezeLive()
        {
            if (!CanControlLiveTarget)
                return;

            RefreshProvider();
            ReleaseLiveHandles();
            m_ViewModel = m_Provider.LiveModel;
            m_AttachmentState = RuntimeDebugAttachmentState.Frozen;
            NotifyChanged();
        }

        public void ResumeLive()
        {
            if (!CanResumeLiveTarget)
                return;

            m_AttachmentState = RuntimeDebugAttachmentState.Live;
            m_HistorySequence = ulong.MaxValue;
            m_CaptureSegmentLimit = 0;
            m_NextCaptureRefreshTime = 0d;
            m_NextLiveRefreshTime = 0d;
            m_ViewModel = m_Provider.LiveModel;
            RebindLiveInterests();
            RefreshProvider();
            NotifyChanged();
        }

        RuntimeCaptureSnapshot GetExecutionCapture()
        {
            if (m_AttachmentState is RuntimeDebugAttachmentState.CaptureHistory or RuntimeDebugAttachmentState.Ended)
                return m_CaptureSnapshot;
            CaptureSourceMaps();
            return m_Target?.Store.FreezeActiveCapture();
        }

        public bool BeginCapture(RuntimeTraceChannel channels, RuntimeDiagnosticsCaptureDetail detail)
        {
            return BeginCaptureCore(channels, detail, 0);
        }

        public bool BeginBoundedCapture(
            RuntimeTraceChannel channels,
            RuntimeDiagnosticsCaptureDetail detail,
            int maximumSegments)
        {
            if (maximumSegments <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumSegments));
            return BeginCaptureCore(channels, detail, maximumSegments);
        }

        bool BeginCaptureCore(
            RuntimeTraceChannel channels,
            RuntimeDiagnosticsCaptureDetail detail,
            int maximumSegments)
        {
            if (!CanStartCapture)
                return false;

            if (!m_Provider.BeginCapture(channels, detail, out _))
                return false;

            m_CaptureSnapshot = null;
            m_HistorySequence = ulong.MaxValue;
            m_CaptureSegmentLimit = maximumSegments;
            m_NextCaptureRefreshTime = 0d;
            if (!RefreshProvider())
                NotifyChanged();
            return true;
        }

        public bool EndCapture()
        {
            if (!CanStopCapture)
                return false;

            CaptureSourceMaps();
            RuntimeCaptureSnapshot snapshot = m_Provider.EndCapture();
            if (snapshot == null)
                return false;

            m_CaptureSnapshot = snapshot;
            m_HistorySequence = ulong.MaxValue;
            m_CaptureSegmentLimit = 0;
            m_NextCaptureRefreshTime = 0d;
            ReleaseLiveHandles();
            m_ViewModel = m_Provider.BuildCaptureView(snapshot, 0, m_SourceMaps);
            m_AttachmentState = RuntimeDebugAttachmentState.CaptureHistory;
            NotifyChanged();
            return true;
        }

        public bool TrySeekExecutionEvent(Guid captureId, ulong sequence)
        {
            RuntimeCaptureSnapshot snapshot = GetExecutionCapture();
            if (snapshot == null || snapshot.CaptureId != captureId)
                return false;
            ReadOnlySpan<RuntimeTraceEvent> events = snapshot.GetEvents(0, sequence);
            if (events.Length == 0 || events[events.Length - 1].Sequence != sequence)
                return false;
            m_CaptureSnapshot = snapshot;
            SetHistorySequence(sequence);
            return true;
        }

        public void SetHistoryOffset(int offset)
        {
            if (m_CaptureSnapshot == null)
                return;
            int maxOffset = Math.Max(0, m_CaptureSnapshot.SegmentCount - 1);
            int clampedOffset = Math.Max(0, Math.Min(offset, maxOffset));
            ReadOnlySpan<RuntimeTraceEvent> events = m_CaptureSnapshot.GetEvents(clampedOffset);
            SetHistorySequence(events.Length == 0 ? ulong.MaxValue : events[events.Length - 1].Sequence);
        }

        void SetHistorySequence(ulong sequence)
        {
            if (m_AttachmentState is RuntimeDebugAttachmentState.CaptureHistory or RuntimeDebugAttachmentState.Ended &&
                m_HistorySequence == sequence)
                return;

            ReleaseLiveHandles();
            m_HistorySequence = sequence;
            m_ViewModel = m_Provider != null
                ? m_Provider.BuildCaptureView(m_CaptureSnapshot, 0, m_SourceMaps, sequence)
                : m_Frozen?.BuildCaptureView(m_CaptureSnapshot, 0, sequence) ?? RuntimeDebugViewModel.Detached;
            m_AttachmentState = m_Target == null ? RuntimeDebugAttachmentState.Ended : RuntimeDebugAttachmentState.CaptureHistory;
            NotifyChanged();
        }

        public IReadOnlyList<RuntimeDebugTargetCandidate> GetTargetCandidates(RuntimeDebugTargetRequest request)
        {
            IReadOnlyList<RuntimeDiagnosticsTarget> targets = RuntimeDiagnosticsTargetRegistry.Targets;
            RuntimeDebugTargetCandidate[] candidates = targets.Count == 0
                ? Array.Empty<RuntimeDebugTargetCandidate>()
                : new RuntimeDebugTargetCandidate[targets.Count];
            for (int i = 0; i < targets.Count; i++)
                candidates[i] = new RuntimeDebugTargetCandidate(new RuntimeDebugTargetInfo(targets[i]), MatchTarget(targets[i], request));
            return candidates;
        }

        public RuntimeDebugTargetResolution ResolveTarget(RuntimeDebugTargetRequest request)
        {
            if (!request.IsValid)
                return new RuntimeDebugTargetResolution(RuntimeDebugTargetResolutionStatus.InvalidSource);

            RuntimeDebugSceneSelection explicitSelection = RuntimeDebugSceneSelectionRegistry.Resolve();
            if (m_AttachmentState == RuntimeDebugAttachmentState.Ended)
            {
                if (explicitSelection.HasExplicitHost && TryFindTargetByHost(explicitSelection.HostInstanceId, out RuntimeDiagnosticsTarget replacement))
                {
                    RuntimeDebugTargetMatch replacementMatch = MatchTarget(replacement, request);
                    if (replacementMatch == RuntimeDebugTargetMatch.Exact)
                    {
                        Attach(replacement);
                        return new RuntimeDebugTargetResolution(RuntimeDebugTargetResolutionStatus.Attached);
                    }
                    return CreateExplicitMismatchResolution(replacementMatch, GetTargetCandidates(request));
                }

                return CreateEndedResolution(request);
            }

            if (explicitSelection.HasExplicitHost)
            {
                if (!TryFindTargetByHost(explicitSelection.HostInstanceId, out RuntimeDiagnosticsTarget selectedTarget))
                    return new RuntimeDebugTargetResolution(RuntimeDebugTargetResolutionStatus.ExplicitHostUnregistered, GetTargetCandidates(request));

                RuntimeDebugTargetMatch selectedMatch = MatchTarget(selectedTarget, request);
                if (selectedMatch != RuntimeDebugTargetMatch.Exact)
                    return CreateExplicitMismatchResolution(selectedMatch, GetTargetCandidates(request));

                Attach(selectedTarget);
                return new RuntimeDebugTargetResolution(RuntimeDebugTargetResolutionStatus.Attached);
            }

            if (m_Target != null && MatchTarget(m_Target, request) == RuntimeDebugTargetMatch.Exact)
                return new RuntimeDebugTargetResolution(RuntimeDebugTargetResolutionStatus.Attached);

            IReadOnlyList<RuntimeDebugTargetCandidate> candidates = GetTargetCandidates(request);
            RuntimeDiagnosticsTarget exactTarget = null;
            int exactCount = 0;
            IReadOnlyList<RuntimeDiagnosticsTarget> targets = RuntimeDiagnosticsTargetRegistry.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (MatchTarget(targets[i], request) != RuntimeDebugTargetMatch.Exact)
                    continue;
                exactTarget = targets[i];
                exactCount++;
            }

            if (exactCount == 1)
            {
                Attach(exactTarget);
                return new RuntimeDebugTargetResolution(RuntimeDebugTargetResolutionStatus.Attached, candidates);
            }

            return new RuntimeDebugTargetResolution(
                exactCount == 0
                    ? RuntimeDebugTargetResolutionStatus.NoExactTarget
                    : RuntimeDebugTargetResolutionStatus.MultipleExactTargets,
                candidates);
        }

        public RuntimeDebugTargetResolution ResolvePinnedTarget(RuntimeDebugTargetRequest request, Guid characterRuntimeId)
        {
            if (!request.IsValid)
                return new RuntimeDebugTargetResolution(RuntimeDebugTargetResolutionStatus.InvalidSource);
            if (characterRuntimeId == Guid.Empty || m_ViewModel.Target.CharacterRuntimeId != characterRuntimeId)
                return new RuntimeDebugTargetResolution(RuntimeDebugTargetResolutionStatus.PinnedTargetNotAttached);
            if (m_AttachmentState == RuntimeDebugAttachmentState.Ended)
                return CreateEndedResolution(request);
            if (m_Target == null || m_Target.CharacterRuntimeId != characterRuntimeId)
                return new RuntimeDebugTargetResolution(RuntimeDebugTargetResolutionStatus.PinnedTargetNotAttached);
            RuntimeDebugTargetMatch match = MatchTarget(m_Target, request);
            return match == RuntimeDebugTargetMatch.Exact
                ? new RuntimeDebugTargetResolution(RuntimeDebugTargetResolutionStatus.Attached)
                : CreateExplicitMismatchResolution(match, GetTargetCandidates(request));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            RuntimeDiagnosticsTargetRegistry.TargetRegistered -= OnTargetChanged;
            RuntimeDiagnosticsTargetRegistry.TargetUnregistered -= OnTargetUnregistered;
            EditorApplication.update -= Update;
            ClearTarget();
            m_LiveInterests.Clear();
        }

        void Attach(RuntimeDiagnosticsTarget target)
        {
            if (target == null || ReferenceEquals(m_Target, target))
                return;

            ReleaseLiveHandles();
            m_Target = target;
            m_Provider = new RuntimeDebugTargetProvider(target);
            m_SourceMaps.Clear();
            m_SourceMaps[target.Revision] = RuntimeDebugSourceMapSnapshot.Capture(target.SourceMap);
            m_Frozen = null;
            m_ViewModel = m_Provider.LiveModel;
            m_CaptureSnapshot = null;
            m_HistorySequence = ulong.MaxValue;
            m_AttachmentState = RuntimeDebugAttachmentState.Live;
            m_NextLiveRefreshTime = 0d;
            RebindLiveInterests();
            RefreshProvider();
            m_TargetRevision++;
            NotifyChanged();
        }

        void Update()
        {
            if (m_Disposed || !CanControlLiveTarget)
                return;

            double now = EditorApplication.timeSinceStartup;
            if (IsCaptureRecording)
            {
                if (m_CaptureSegmentLimit > 0 && CaptureSegmentCount >= m_CaptureSegmentLimit)
                {
                    EndCapture();
                    return;
                }

                if (now < m_NextCaptureRefreshTime)
                    return;
                m_NextCaptureRefreshTime = now + CaptureRefreshIntervalSeconds;
            }
            else
            {
                if (now < m_NextLiveRefreshTime)
                    return;
                m_NextLiveRefreshTime = now + LiveRefreshIntervalSeconds;
            }
            if (RefreshProvider())
                NotifyChanged();
        }

        bool RefreshProvider()
        {
            if (m_Provider == null)
                return false;

            if (m_Target != null &&
                !m_Provider.LiveModel.Target.Revision.Equals(m_Target.Revision))
            {
                m_SourceMaps[m_Provider.LiveModel.Target.Revision] = m_Provider.SourceMap;
                m_Provider = new RuntimeDebugTargetProvider(m_Target);
                m_SourceMaps[m_Target.Revision] = m_Provider.SourceMap;
                m_ViewModel = m_Provider.LiveModel;
                m_TargetRevision++;
                NotifyChanged();
            }

            CaptureSourceMaps();
            bool changed = m_Provider.Refresh(m_SourceMaps);
            if (m_AttachmentState == RuntimeDebugAttachmentState.Live)
                m_ViewModel = m_Provider.LiveModel;
            return changed;
        }

        void CaptureSourceMaps()
        {
            if (m_Target == null || m_SourceMaps.Count == m_Target.Context.SourceMaps.Count)
                return;
            foreach (IDebugSourceMap sourceMap in m_Target.Context.SourceMaps)
                if (!m_SourceMaps.ContainsKey(sourceMap.Revision))
                    m_SourceMaps.Add(sourceMap.Revision, RuntimeDebugSourceMapSnapshot.Capture(sourceMap));
        }

        void RebindLiveInterests()
        {
            if (!CanControlLiveTarget)
                return;

            foreach (LiveInterestLease lease in m_LiveInterests.Values)
            {
                if (lease.Channels != RuntimeTraceChannel.None && !lease.Handle.IsValid)
                    lease.Handle = m_Target.Store.AcquireInterest(new RuntimeDiagnosticsInterest(RuntimeDiagnosticsInterestKind.LiveState, lease.Channels));
            }
        }

        void ReleaseLiveHandles()
        {
            foreach (LiveInterestLease lease in m_LiveInterests.Values)
                ReleaseLiveHandle(lease);
        }

        void ReleaseLiveHandle(LiveInterestLease lease)
        {
            if (lease == null || !lease.Handle.IsValid)
                return;

            if (m_Target != null)
                m_Target.Store.ReleaseInterest(lease.Handle);
            lease.Handle = default;
        }

        void FreezeTarget(RuntimeDiagnosticsTarget target)
        {
            if (target == null)
                return;

            ReleaseLiveHandles();
            CaptureSourceMaps();
            m_Frozen = m_Provider?.Freeze(m_SourceMaps);
            RuntimeCaptureSnapshot activeCapture = m_Frozen?.ActiveCapture;
            if (m_AttachmentState != RuntimeDebugAttachmentState.CaptureHistory && activeCapture != null)
                m_CaptureSnapshot = activeCapture;
            m_ViewModel = m_CaptureSnapshot != null
                ? m_Frozen?.BuildCaptureView(m_CaptureSnapshot, 0, m_HistorySequence) ?? RuntimeDebugViewModel.Detached
                : m_Frozen?.LiveModel ?? RuntimeDebugViewModel.Detached;
            m_Target = null;
            m_Provider = null;
            m_AttachmentState = RuntimeDebugAttachmentState.Ended;
            m_TargetRevision++;
            NotifyChanged();
        }

        RuntimeDebugTargetResolution CreateEndedResolution(RuntimeDebugTargetRequest request)
        {
            RuntimeDebugTargetMatch match = m_Frozen?.MatchSource(request) ?? RuntimeDebugTargetMatch.SourceMissing;
            if (match == RuntimeDebugTargetMatch.Exact)
                return new RuntimeDebugTargetResolution(RuntimeDebugTargetResolutionStatus.Ended);
            return new RuntimeDebugTargetResolution(
                match == RuntimeDebugTargetMatch.SourceMissing
                    ? RuntimeDebugTargetResolutionStatus.SourceMissing
                    : RuntimeDebugTargetResolutionStatus.RevisionMismatch);
        }

        static RuntimeDebugTargetResolution CreateExplicitMismatchResolution(
            RuntimeDebugTargetMatch match,
            IReadOnlyList<RuntimeDebugTargetCandidate> candidates)
        {
            return new RuntimeDebugTargetResolution(
                match == RuntimeDebugTargetMatch.SourceMissing
                    ? RuntimeDebugTargetResolutionStatus.ExplicitHostSourceMissing
                    : RuntimeDebugTargetResolutionStatus.ExplicitHostRevisionMismatch,
                candidates);
        }

        static RuntimeDebugTargetMatch MatchTarget(RuntimeDiagnosticsTarget target, RuntimeDebugTargetRequest request)
        {
            if (target == null || !request.IsValid)
                return RuntimeDebugTargetMatch.SourceMissing;

            RuntimeDebugTargetMatch match = RuntimeDebugTargetMatch.SourceMissing;
            foreach (IDebugSourceMap sourceMap in target.Context.SourceMaps)
            {
                IReadOnlyList<RuntimeSourceElementHandle> handles = sourceMap.FindHandles(request.Source);
                if (handles.Count != 0)
                    match = RuntimeDebugTargetMatch.RevisionMismatch;
                for (int i = 0; i < handles.Count; i++)
                    if (sourceMap.TryGet(handles[i], out DebugSourceMapEntry entry) &&
                        string.Equals(entry.ContentHash, request.ContentHash, StringComparison.Ordinal))
                        return RuntimeDebugTargetMatch.Exact;
            }
            return match;
        }

        static bool TryFindTargetByHost(int hostInstanceId, out RuntimeDiagnosticsTarget target)
        {
            IReadOnlyList<RuntimeDiagnosticsTarget> targets = RuntimeDiagnosticsTargetRegistry.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].HostInstanceId == hostInstanceId)
                {
                    target = targets[i];
                    return true;
                }
            }

            target = null;
            return false;
        }

        void OnTargetChanged(RuntimeDiagnosticsTarget target)
        {
            m_TargetRevision++;
            NotifyChanged();
        }

        void OnTargetUnregistered(RuntimeDiagnosticsTarget target)
        {
            if (ReferenceEquals(m_Target, target))
                FreezeTarget(target);
            else
            {
                m_TargetRevision++;
                NotifyChanged();
            }
        }

        void NotifyChanged()
        {
            Changed?.Invoke();
        }

        sealed class LiveInterestLease
        {
            public RuntimeTraceChannel Channels;
            public RuntimeDiagnosticsInterestHandle Handle;
        }
    }
}
