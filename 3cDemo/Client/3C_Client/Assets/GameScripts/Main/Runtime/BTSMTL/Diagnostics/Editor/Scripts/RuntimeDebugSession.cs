using System;
using System.Collections.Generic;
using UnityEditor;

namespace BTSMTL.Diagnostics.Editor
{
    [InitializeOnLoad]
    public sealed class RuntimeDebugSession : IDisposable
    {
        const double CaptureRefreshIntervalSeconds = 0.1d;

        static readonly RuntimeDebugSession s_Shared;

        readonly Dictionary<object, LiveInterestLease> m_LiveInterests = new Dictionary<object, LiveInterestLease>();
        RuntimeDiagnosticsTarget m_Target;
        RuntimeDebugTargetProvider m_Provider;
        RuntimeDebugFrozenDiagnostics m_Frozen;
        RuntimeDebugViewModel m_ViewModel = RuntimeDebugViewModel.Detached;
        RuntimeCaptureSnapshot m_CaptureSnapshot;
        RuntimeDebugAttachmentState m_AttachmentState;
        int m_HistoryOffset;
        int m_CaptureSegmentLimit;
        long m_TargetRevision;
        double m_NextCaptureRefreshTime;
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
        public int HistoryOffset => m_HistoryOffset;

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
                var result = new List<RuntimeDebugTargetInfo>(targets.Count);
                for (int i = 0; i < targets.Count; i++)
                    result.Add(new RuntimeDebugTargetInfo(targets[i]));
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
            m_Frozen = null;
            m_ViewModel = RuntimeDebugViewModel.Detached;
            m_CaptureSnapshot = null;
            m_HistoryOffset = 0;
            m_CaptureSegmentLimit = 0;
            m_NextCaptureRefreshTime = 0d;
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
            m_HistoryOffset = 0;
            m_ViewModel = m_Provider.LiveModel;
            RebindLiveInterests();
            if (!RefreshProvider())
                NotifyChanged();
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
            m_HistoryOffset = 0;
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

            RuntimeCaptureSnapshot snapshot = m_Provider.EndCapture();
            if (snapshot == null)
                return false;

            m_CaptureSnapshot = snapshot;
            m_HistoryOffset = 0;
            m_CaptureSegmentLimit = 0;
            m_NextCaptureRefreshTime = 0d;
            ReleaseLiveHandles();
            m_ViewModel = m_Provider.BuildCaptureView(snapshot, m_HistoryOffset);
            m_AttachmentState = RuntimeDebugAttachmentState.CaptureHistory;
            NotifyChanged();
            return true;
        }

        public void SetHistoryOffset(int offset)
        {
            if (m_CaptureSnapshot == null)
                return;

            int maxOffset = Math.Max(0, m_CaptureSnapshot.SegmentCount - 1);
            int clampedOffset = Math.Max(0, Math.Min(offset, maxOffset));
            if (m_AttachmentState == RuntimeDebugAttachmentState.CaptureHistory && m_HistoryOffset == clampedOffset)
                return;

            ReleaseLiveHandles();
            m_HistoryOffset = clampedOffset;
            m_ViewModel = m_Provider != null
                ? m_Provider.BuildCaptureView(m_CaptureSnapshot, m_HistoryOffset)
                : m_Frozen?.BuildCaptureView(m_CaptureSnapshot, m_HistoryOffset) ?? RuntimeDebugViewModel.Detached;
            m_AttachmentState = m_Target == null ? RuntimeDebugAttachmentState.Ended : RuntimeDebugAttachmentState.CaptureHistory;
            NotifyChanged();
        }

        public IReadOnlyList<RuntimeDebugTargetCandidate> GetTargetCandidates(RuntimeDebugTargetRequest request)
        {
            IReadOnlyList<RuntimeDiagnosticsTarget> targets = RuntimeDiagnosticsTargetRegistry.Targets;
            var candidates = new List<RuntimeDebugTargetCandidate>(targets.Count);
            for (int i = 0; i < targets.Count; i++)
                candidates.Add(new RuntimeDebugTargetCandidate(new RuntimeDebugTargetInfo(targets[i]), MatchTarget(targets[i], request)));
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

            if (!EditorApplication.isPlaying)
                return new RuntimeDebugTargetResolution(RuntimeDebugTargetResolutionStatus.NotPlaying, GetTargetCandidates(request));

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
            m_Frozen = null;
            m_ViewModel = m_Provider.LiveModel;
            m_CaptureSnapshot = null;
            m_HistoryOffset = 0;
            m_AttachmentState = RuntimeDebugAttachmentState.Live;
            RebindLiveInterests();
            RefreshProvider();
            m_TargetRevision++;
            NotifyChanged();
        }

        void Update()
        {
            if (m_Disposed || !CanControlLiveTarget)
                return;

            if (IsCaptureRecording)
            {
                if (m_CaptureSegmentLimit > 0 && CaptureSegmentCount >= m_CaptureSegmentLimit)
                {
                    EndCapture();
                    return;
                }

                double now = EditorApplication.timeSinceStartup;
                if (now < m_NextCaptureRefreshTime)
                    return;
                m_NextCaptureRefreshTime = now + CaptureRefreshIntervalSeconds;
            }

            if (RefreshProvider())
                NotifyChanged();
        }

        bool RefreshProvider()
        {
            if (m_Provider == null)
                return false;

            bool changed = m_Provider.Refresh();
            if (m_AttachmentState == RuntimeDebugAttachmentState.Live)
                m_ViewModel = m_Provider.LiveModel;
            return changed;
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
            m_Frozen = m_Provider?.Freeze();
            RuntimeCaptureSnapshot activeCapture = m_Frozen?.ActiveCapture;
            if (activeCapture != null)
                m_CaptureSnapshot = activeCapture;
            m_ViewModel = m_CaptureSnapshot != null
                ? m_Frozen?.BuildCaptureView(m_CaptureSnapshot, 0) ?? RuntimeDebugViewModel.Detached
                : m_Frozen?.LiveModel ?? RuntimeDebugViewModel.Detached;
            m_Target = null;
            m_Provider = null;
            m_HistoryOffset = 0;
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

            IReadOnlyList<RuntimeSourceElementHandle> handles = target.SourceMap.FindHandles(request.Source);
            if (handles.Count == 0)
                return RuntimeDebugTargetMatch.SourceMissing;

            for (int i = 0; i < handles.Count; i++)
            {
                if (target.SourceMap.TryGet(handles[i], out DebugSourceMapEntry entry) &&
                    string.Equals(entry.ContentHash, request.ContentHash, StringComparison.Ordinal))
                    return RuntimeDebugTargetMatch.Exact;
            }
            return RuntimeDebugTargetMatch.RevisionMismatch;
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
