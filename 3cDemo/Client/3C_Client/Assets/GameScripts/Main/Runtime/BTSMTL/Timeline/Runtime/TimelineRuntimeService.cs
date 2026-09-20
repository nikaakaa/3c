using System;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BTSMTL.Diagnostics;
using TreeDesigner;

namespace BTSMTL.Timeline.Runtime
{
    public interface ITimelineRuntimePlaybackRequestFactory : ITimelinePlaybackActionContextSource
    {
        bool TryCreate(
            TimelineData timeline,
            string sourceId,
            string sourceName,
            TimelinePlaybackActionContext actionContext,
            TimelinePlaybackMode playbackMode,
            TreeExecutionActivationScope sourceActivation,
            BaseGraph sourceRuntimeGraph,
            TimelineRuntimePlaybackHandle playbackHandle,
            ulong generation,
            out TimelineRuntimePrepareRequest request,
            out string error);
    }

    public interface ITimelineRuntimeCallBindingSource : ITimelinePlaybackActionContextSource
    {
        bool TryCreateExecutionIdentity(
            string sourceId,
            string sourceName,
            TimelinePlaybackActionContext actionContext,
            TimelineRuntimePlaybackHandle playbackHandle,
            out TimelineExecutionIdentity identity,
            out string error);

        bool TryGetCallBindings(
            TimelineData timeline,
            string sourceId,
            string sourceName,
            TimelinePlaybackActionContext actionContext,
            out IReadOnlyList<TimelineCallBinding> bindings,
            out string error);
    }

    public sealed class TimelineRuntimePlaybackRequestFactory : ITimelineRuntimePlaybackRequestFactory
    {
        readonly TimelineContractCatalog m_ContractCatalog;
        readonly TimelineRuntimeNumericTarget m_NumericTarget;
        readonly ITimelineDomainBindingResolver m_DomainResolver;
        readonly ITimelineRuntimeDependencyResolver m_DependencyResolver;
        readonly ITimelineRuntimeCallBindingSource m_CallBindingSource;

        public TimelineRuntimePlaybackRequestFactory(
            TimelineContractCatalog contractCatalog,
            TimelineRuntimeNumericTarget numericTarget,
            ITimelineDomainBindingResolver domainResolver,
            ITimelineRuntimeDependencyResolver dependencyResolver,
            ITimelineRuntimeCallBindingSource callBindingSource)
        {
            m_ContractCatalog = contractCatalog ?? throw new ArgumentNullException(nameof(contractCatalog));
            if (!Enum.IsDefined(typeof(TimelineRuntimeNumericTarget), numericTarget))
                throw new ArgumentOutOfRangeException(nameof(numericTarget));
            m_NumericTarget = numericTarget;
            m_DomainResolver = domainResolver ?? throw new ArgumentNullException(nameof(domainResolver));
            m_DependencyResolver = dependencyResolver ?? throw new ArgumentNullException(nameof(dependencyResolver));
            m_CallBindingSource = callBindingSource ?? throw new ArgumentNullException(nameof(callBindingSource));
        }

        public bool TryCreate(
            TimelineData timeline,
            string sourceId,
            string sourceName,
            TimelinePlaybackActionContext actionContext,
            TimelinePlaybackMode playbackMode,
            TreeExecutionActivationScope sourceActivation,
            BaseGraph sourceRuntimeGraph,
            TimelineRuntimePlaybackHandle playbackHandle,
            ulong generation,
            out TimelineRuntimePrepareRequest request,
            out string error)
        {
            request = null;
            error = string.Empty;
            if (timeline == null || !playbackHandle.IsValid || generation == 0)
            {
                error = "timeline_runtime_request_invalid";
                return false;
            }
            if (sourceRuntimeGraph != null)
            {
                if (!sourceActivation.IsValid)
                {
                    error = "timeline_source_activation_invalid";
                    return false;
                }
                if (!string.Equals(
                        sourceActivation.AuthoringRoute.LeafGraphAuthoringId,
                        sourceRuntimeGraph.GraphAuthoringId,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        sourceActivation.Source.GraphAuthoringId,
                        sourceRuntimeGraph.GraphAuthoringId,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        sourceActivation.Source.ElementAuthoringId,
                        sourceId,
                        StringComparison.Ordinal))
                {
                    error = "timeline_source_activation_mismatch";
                    return false;
                }
            }
            if (!m_CallBindingSource.TryCreateExecutionIdentity(
                    sourceId,
                    sourceName,
                    actionContext,
                    playbackHandle,
                    out TimelineExecutionIdentity identity,
                    out error))
                return false;
            if (!m_CallBindingSource.TryGetCallBindings(
                    timeline,
                    sourceId,
                    sourceName,
                    actionContext,
                    out IReadOnlyList<TimelineCallBinding> bindings,
                    out error))
                return false;
            request = new TimelineRuntimePrepareRequest(
                $"timeline:{playbackHandle.Value}",
                timeline,
                m_ContractCatalog,
                identity,
                playbackMode,
                m_NumericTarget,
                bindings,
                m_DomainResolver,
                m_DependencyResolver);
            return true;
        }

        public bool TryGetTimelinePlaybackActionContext(
            ActionContextSlot actionContext,
            out TimelinePlaybackActionContext playbackActionContext)
        {
            return m_CallBindingSource.TryGetTimelinePlaybackActionContext(actionContext, out playbackActionContext);
        }
    }

    public interface ITimelineRuntimeEvaluationSink
    {
        bool Consume(TimelineRuntimeStepContext context);
        void Commit(TimelineRuntimeStepContext context);
        void Discard(TimelineRuntimeStepContext context);
        bool ConsumeStop(TimelineRuntimeStopRequest request);
        void CommitStop(TimelineRuntimeStopRequest request);
        void DiscardStop(TimelineRuntimeStopRequest request);
    }

    public interface ITimelineRuntimeTreeClipService
    {
        bool Consume(TimelineRuntimeTreeClipRequest request, TimelineRuntimeStepContext context);
        void Discard(TimelineRuntimeTreeClipRequest request, TimelineRuntimeStepContext context);
        void Commit(TimelineRuntimeStepContext context);
        void DiscardStep(TimelineRuntimeStepContext context);
        bool ConsumeStop(TimelineRuntimeStopRequest request);
        void CommitStop(TimelineRuntimeStopRequest request);
        void DiscardStop(TimelineRuntimeStopRequest request);
    }

    public interface ITimelineRuntimeMarkerService
    {
        bool Consume(TimelineRuntimeMarkerRequest request, TimelineRuntimeStepContext context);
        void Discard(TimelineRuntimeMarkerRequest request, TimelineRuntimeStepContext context);
        void Commit(TimelineRuntimeStepContext context);
        void DiscardStep(TimelineRuntimeStepContext context);
        bool ConsumeStop(TimelineRuntimeStopRequest request);
        void CommitStop(TimelineRuntimeStopRequest request);
        void DiscardStop(TimelineRuntimeStopRequest request);
    }

    public interface ITimelineRuntimeStopConsumer
    {
        bool ConsumeStop(TimelineRuntimeStopRequest request);
    }

    public interface ITimelineRuntimeStepCommitConsumer
    {
        void Commit(TimelineRuntimeStepContext context);
        void Discard(TimelineRuntimeStepContext context);
    }

    public interface ITimelineRuntimeStopCommitConsumer
    {
        void CommitStop(TimelineRuntimeStopRequest request);
        void DiscardStop(TimelineRuntimeStopRequest request);
    }

    public readonly struct TimelineRuntimeStopRequest
    {
        readonly ulong m_Sequence;

        internal TimelineRuntimeStopRequest(
            TimelineRuntimePlayback playback,
            TimelinePlaybackStopContext reason)
        {
            Playback = playback ?? throw new ArgumentNullException(nameof(playback));
            Reason = reason;
            m_Sequence = playback.PendingStopSequence;
        }

        public TimelineRuntimePlayback Playback { get; }
        public TimelinePlaybackStopContext Reason { get; }
        public TimelineRuntimePlaybackHandle Handle => Playback.Handle;
        public TimelineExecutionIdentity ExecutionIdentity => Playback.ExecutionIdentity;
        public ulong Generation => Playback.Generation;
        internal bool IsPendingFor(TimelineRuntimePlayback playback) =>
            ReferenceEquals(Playback, playback) && m_Sequence != 0 &&
            m_Sequence == playback.PendingStopSequence;
    }

    public sealed class TimelineRuntimeExecutionConsumer :
        ITimelineRuntimeStepConsumer,
        ITimelineRuntimeStepCommitConsumer,
        ITimelineRuntimeStopConsumer,
        ITimelineRuntimeStopCommitConsumer
    {
        readonly ITimelineRuntimeEvaluationSink m_EvaluationSink;
        readonly ITimelineRuntimeTreeClipService m_TreeClipService;
        readonly ITimelineRuntimeMarkerService m_MarkerService;
        readonly Dictionary<ulong, int> m_ConsumedTreeClipCounts =
            new Dictionary<ulong, int>();
        readonly Dictionary<ulong, int> m_ConsumedMarkerCounts =
            new Dictionary<ulong, int>();

        public TimelineRuntimeExecutionConsumer(
            ITimelineRuntimeEvaluationSink evaluationSink,
            ITimelineRuntimeTreeClipService treeClipService,
            ITimelineRuntimeMarkerService markerService)
        {
            m_EvaluationSink = evaluationSink ?? throw new ArgumentNullException(nameof(evaluationSink));
            m_TreeClipService = treeClipService ?? throw new ArgumentNullException(nameof(treeClipService));
            m_MarkerService = markerService ?? throw new ArgumentNullException(nameof(markerService));
        }

        public TimelineRuntimeStepDecision Consume(TimelineRuntimeStepContext context)
        {
            int consumedMarkerCount = 0;
            m_ConsumedMarkerCounts[context.Playback.Handle.Value] = 0;
            for (; consumedMarkerCount < context.Advance.Evaluation.Markers.Count; consumedMarkerCount++)
            {
                TimelineRuntimeMarkerRequest marker = context.Advance.Evaluation.Markers[consumedMarkerCount];
                if (!m_MarkerService.Consume(marker, context))
                    break;
                m_ConsumedMarkerCounts[context.Playback.Handle.Value] = consumedMarkerCount + 1;
            }
            if (consumedMarkerCount != context.Advance.Evaluation.Markers.Count)
                return TimelineRuntimeStepDecision.Discard;

            int consumedTreeClipCount = 0;
            m_ConsumedTreeClipCounts[context.Playback.Handle.Value] = 0;
            for (; consumedTreeClipCount < context.Advance.Evaluation.TreeClips.Count; consumedTreeClipCount++)
            {
                TimelineRuntimeTreeClipRequest request = context.Advance.Evaluation.TreeClips[consumedTreeClipCount];
                if (!m_TreeClipService.Consume(request, context))
                    break;
                m_ConsumedTreeClipCounts[context.Playback.Handle.Value] = consumedTreeClipCount + 1;
            }
            if (consumedTreeClipCount != context.Advance.Evaluation.TreeClips.Count)
                return TimelineRuntimeStepDecision.Discard;
            return m_EvaluationSink.Consume(context)
                ? TimelineRuntimeStepDecision.Commit
                : TimelineRuntimeStepDecision.Discard;
        }

        public bool ConsumeStop(TimelineRuntimeStopRequest request)
        {
            return m_MarkerService.ConsumeStop(request) &&
                   m_TreeClipService.ConsumeStop(request) &&
                   m_EvaluationSink.ConsumeStop(request);
        }

        public void Commit(TimelineRuntimeStepContext context)
        {
            m_MarkerService.Commit(context);
            m_TreeClipService.Commit(context);
            m_EvaluationSink.Commit(context);
            m_ConsumedTreeClipCounts.Remove(context.Playback.Handle.Value);
            m_ConsumedMarkerCounts.Remove(context.Playback.Handle.Value);
        }

        public void Discard(TimelineRuntimeStepContext context)
        {
            if (!m_ConsumedTreeClipCounts.TryGetValue(context.Playback.Handle.Value, out int count))
                count = 0;
            if (m_ConsumedMarkerCounts.TryGetValue(context.Playback.Handle.Value, out int markerCount))
                for (int index = 0; index < markerCount; index++)
                    m_MarkerService.Discard(context.Advance.Evaluation.Markers[index], context);
            for (int index = 0; index < count; index++)
                m_TreeClipService.Discard(context.Advance.Evaluation.TreeClips[index], context);
            m_MarkerService.DiscardStep(context);
            m_TreeClipService.DiscardStep(context);
            m_EvaluationSink.Discard(context);
            m_ConsumedTreeClipCounts.Remove(context.Playback.Handle.Value);
            m_ConsumedMarkerCounts.Remove(context.Playback.Handle.Value);
        }

        public void CommitStop(TimelineRuntimeStopRequest request)
        {
            m_MarkerService.CommitStop(request);
            m_TreeClipService.CommitStop(request);
            m_EvaluationSink.CommitStop(request);
            m_ConsumedTreeClipCounts.Remove(request.Handle.Value);
            m_ConsumedMarkerCounts.Remove(request.Handle.Value);
        }

        public void DiscardStop(TimelineRuntimeStopRequest request)
        {
            m_MarkerService.DiscardStop(request);
            m_TreeClipService.DiscardStop(request);
            m_EvaluationSink.DiscardStop(request);
            m_ConsumedTreeClipCounts.Remove(request.Handle.Value);
            m_ConsumedMarkerCounts.Remove(request.Handle.Value);
        }
    }

    public readonly struct TimelineRuntimePlaybackDescriptor
    {
        internal TimelineRuntimePlaybackDescriptor(TimelineRuntimePlayback playback)
        {
            Handle = playback.Handle;
            Generation = playback.Generation;
            RequestId = playback.RequestId;
            ExecutionIdentity = playback.ExecutionIdentity;
            PlaybackMode = playback.PlaybackMode;
            NumericTarget = playback.NumericTarget;
            ContentIdentity = playback.Content.Identity;
            ContentRevision = playback.ContentRevision;
            State = playback.State;
            CursorTime = playback.CursorTime;
            Cycle = playback.Cycle;
            SectionId = playback.SectionId;
            ActiveClipIds = new ReadOnlyCollection<string>(new List<string>(playback.ActiveClipIds));
        }

        public TimelineRuntimePlaybackHandle Handle { get; }
        public ulong Generation { get; }
        public string RequestId { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public TimelinePlaybackMode PlaybackMode { get; }
        public TimelineRuntimeNumericTarget NumericTarget { get; }
        public string ContentIdentity { get; }
        public string ContentRevision { get; }
        public TimelineRuntimePlaybackState State { get; }
        public FixedScalar CursorTime { get; }
        public int Cycle { get; }
        public string SectionId { get; }
        public IReadOnlyList<string> ActiveClipIds { get; }
        public bool IsValid => Handle.IsValid && Generation != 0 && ExecutionIdentity.IsValid;
    }

    public readonly struct TimelineRuntimePlaybackSnapshot
    {
        public const string CurrentSchema = "btsmtl.timeline.direct-runtime.v7";
        public bool IsValid => Handle.IsValid && Generation != 0;

        internal TimelineRuntimePlaybackSnapshot(TimelineRuntimePlayback playback)
        {
            Schema = CurrentSchema;
            TickRate = playback.TickRate;
            Handle = playback.Handle;
            Generation = playback.Generation;
            RequestId = playback.RequestId;
            ExecutionIdentity = playback.ExecutionIdentity;
            PlaybackMode = playback.PlaybackMode;
            NumericTarget = playback.NumericTarget;
            ContentRevision = playback.ContentRevision;
            State = playback.State;
            CursorTime = playback.CursorTime;
            Cycle = playback.Cycle;
            SectionId = playback.SectionId;
            ActiveClipIds = TimelineSnapshotItems<string>.CopyFrom(playback.ActiveClipIds);
            HasStopContext = playback.HasStopContext;
            StopContext = playback.StopContext;
            InitialBoundaryPending = playback.InitialBoundaryPending;
            TimeCarry = playback.TimeCarry;
            Control = playback.Control;
            TreeDecisionExits = TimelineSnapshotItems<string>.CopyFrom(playback.ExitedTreeDecisionClips);
            PendingTreeDecisionExits = TimelineSnapshotItems<string>.CopyFrom(playback.PendingTreeDecisionClips);
        }

        public TimelineRuntimePlaybackSnapshot(
            TimelineRuntimePlaybackHandle handle,
            ulong generation,
            string requestId,
            TimelineExecutionIdentity executionIdentity,
            TimelinePlaybackMode playbackMode,
            TimelineRuntimeNumericTarget numericTarget,
            string contentRevision,
            TimelineRuntimePlaybackState state,
            FixedScalar cursorTime,
            int cycle,
            string sectionId,
            TimelineSnapshotItems<string> activeClipIds,
            bool hasStopContext,
            TimelinePlaybackStopContext stopContext,
            bool initialBoundaryPending,
            int timeCarry,
            AbilityTimelinePlaybackControl control,
            TimelineSnapshotItems<string> treeDecisionExits,
            TimelineSnapshotItems<string> pendingTreeDecisionExits,
            int tickRate)
        {
            TickRate = tickRate;
            Schema = CurrentSchema;
            Handle = handle;
            Generation = generation;
            RequestId = requestId ?? string.Empty;
            ExecutionIdentity = executionIdentity;
            PlaybackMode = playbackMode;
            NumericTarget = numericTarget;
            ContentRevision = contentRevision ?? string.Empty;
            State = state;
            CursorTime = cursorTime;
            Cycle = cycle;
            SectionId = sectionId ?? string.Empty;
            ActiveClipIds = activeClipIds;
            HasStopContext = hasStopContext;
            StopContext = stopContext;
            InitialBoundaryPending = initialBoundaryPending;
            TimeCarry = timeCarry;
            Control = control;
            TreeDecisionExits = treeDecisionExits;
            PendingTreeDecisionExits = pendingTreeDecisionExits;
        }

        public string Schema { get; }
        public TimelineRuntimePlaybackHandle Handle { get; }
        public ulong Generation { get; }
        public string RequestId { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public TimelinePlaybackMode PlaybackMode { get; }
        public TimelineRuntimeNumericTarget NumericTarget { get; }
        public string ContentRevision { get; }
        public TimelineRuntimePlaybackState State { get; }
        public FixedScalar CursorTime { get; }
        public int Cycle { get; }
        public string SectionId { get; }
        public TimelineSnapshotItems<string> ActiveClipIds { get; }
        public bool HasStopContext { get; }
        public TimelinePlaybackStopContext StopContext { get; }
        public bool InitialBoundaryPending { get; }
        public int TimeCarry { get; }
        public AbilityTimelinePlaybackControl Control { get; }
        public TimelineSnapshotItems<string> TreeDecisionExits { get; }
        public TimelineSnapshotItems<string> PendingTreeDecisionExits { get; }
        public int TickRate { get; }


    }

    public readonly struct TimelineRuntimeRestoreCandidate
    {
        readonly TimelineRuntimePlaybackSnapshot m_Snapshot;
        readonly TimelineRuntimePreparationResult m_Preparation;
        readonly TimelineRuntimeService m_Service;

        internal TimelineRuntimeRestoreCandidate(
            TimelineRuntimeService service,
            TimelineRuntimePlaybackSnapshot snapshot,
            TimelineRuntimePreparationResult preparation)
        {
            m_Service = service ?? throw new ArgumentNullException(nameof(service));
            if (!snapshot.IsValid)
                throw new ArgumentException("Timeline restore snapshot is invalid.", nameof(snapshot));
            m_Snapshot = snapshot;
            m_Preparation = preparation ?? throw new ArgumentNullException(nameof(preparation));
            Validate();
        }

        public string Schema => m_Snapshot.Schema;
        public TimelineRuntimePlaybackHandle Handle => m_Snapshot.Handle;
        public ulong Generation => m_Snapshot.Generation;
        public TimelineExecutionIdentity ExecutionIdentity => m_Snapshot.ExecutionIdentity;
        public TimelineRuntimeNumericTarget NumericTarget => m_Snapshot.NumericTarget;
        public string ContentRevision => m_Snapshot.ContentRevision;

        internal TimelineRuntimePlaybackSnapshot Snapshot => m_Snapshot;
        internal TimelineRuntimePreparationResult Preparation => m_Preparation;

        internal void ValidateOwnedBy(TimelineRuntimeService service)
        {
            if (!ReferenceEquals(m_Service, service))
                throw new InvalidOperationException("Timeline restore candidate belongs to another runtime service.");
            Validate();
        }

        internal TimelineRuntimePlayback CreatePlayback()
        {
            TimelineRuntimePlayback playback = TimelineRuntimePreparation.CreatePlayback(
                m_Preparation,
                m_Snapshot.Handle,
                m_Snapshot.Generation,
                m_Service.TickRate);
            ApplyTo(playback);
            return playback;
        }

        internal void ApplyTo(TimelineRuntimePlayback playback)
        {
            if (playback.Handle != m_Snapshot.Handle || playback.Generation != m_Snapshot.Generation ||
                playback.ExecutionIdentity != m_Snapshot.ExecutionIdentity || playback.PlaybackMode != m_Snapshot.PlaybackMode ||
                playback.NumericTarget != m_Snapshot.NumericTarget || playback.TickRate != m_Snapshot.TickRate ||
                !string.Equals(playback.RequestId, m_Snapshot.RequestId, StringComparison.Ordinal) ||
                !string.Equals(playback.ContentRevision, m_Snapshot.ContentRevision, StringComparison.Ordinal))
                throw new InvalidOperationException("Timeline restore targets a different playback identity or content.");
            if (!playback.RestoreCommittedState(
                    m_Snapshot.State,
                    m_Snapshot.CursorTime,
                    m_Snapshot.Cycle,
                    m_Snapshot.TimeCarry,
                    m_Snapshot.Control,
                    m_Snapshot.SectionId,
                    m_Snapshot.ActiveClipIds.Span,
                    m_Snapshot.TreeDecisionExits.Span,
                    m_Snapshot.PendingTreeDecisionExits.Span,
                    m_Snapshot.HasStopContext,
                    m_Snapshot.StopContext,
                    m_Snapshot.InitialBoundaryPending))
                throw new InvalidOperationException("Timeline restore candidate is not a committed state.");
        }

        void Validate()
        {
            if (!string.Equals(m_Snapshot.Schema, TimelineRuntimePlaybackSnapshot.CurrentSchema, StringComparison.Ordinal) ||
                !m_Snapshot.Handle.IsValid ||
                m_Snapshot.Generation == 0 ||
                m_Snapshot.State == TimelineRuntimePlaybackState.Disposed ||
                m_Snapshot.State == TimelineRuntimePlaybackState.Failed ||
                !m_Preparation.IsReady ||
                m_Snapshot.ExecutionIdentity != m_Preparation.ExecutionIdentity ||
                !string.Equals(m_Snapshot.RequestId, m_Preparation.RequestId, StringComparison.Ordinal) ||
                m_Snapshot.PlaybackMode != m_Preparation.PlaybackMode ||
                m_Snapshot.NumericTarget != m_Preparation.NumericTarget ||
                m_Snapshot.TickRate != m_Service.TickRate ||
                !string.Equals(m_Snapshot.ContentRevision, m_Preparation.ContentRevision, StringComparison.Ordinal))
                throw new InvalidOperationException("Timeline restore snapshot does not match the prepared content.");
        }
    }
    public sealed class TimelineRuntimeService : ITimelinePlaybackService, ITimelinePlaybackActionContextSource, IDisposable
    {
        static readonly List<TimelineRuntimeService> s_ActiveServices =
            new List<TimelineRuntimeService>();

        readonly ITimelineRuntimePlaybackRequestFactory m_RequestFactory;
        readonly ITimelineRuntimeStepConsumer m_StepConsumer;
        readonly ITimelineRuntimeStopConsumer m_StopConsumer;
        readonly int m_TickRate;
        readonly Dictionary<ulong, TimelineRuntimePlayback> m_Playbacks =
            new Dictionary<ulong, TimelineRuntimePlayback>();
        ulong m_NextPlaybackHandle = 1;
        ulong m_NextGeneration = 1;
        bool m_Disposed;

        public event Action<TimelineRuntimePlaybackDescriptor> PlaybackChanged;
        public static event Action ObservationChanged;
        public string LastFailure { get; private set; } = string.Empty;
        internal int TickRate => m_TickRate;

        public TimelineRuntimeService(
            ITimelineRuntimePlaybackRequestFactory requestFactory,
            ITimelineRuntimeStepConsumer stepConsumer,
            ITimelineRuntimeStopConsumer stopConsumer,
            int tickRate)
        {
            m_RequestFactory = requestFactory ?? throw new ArgumentNullException(nameof(requestFactory));
            m_StepConsumer = stepConsumer ?? throw new ArgumentNullException(nameof(stepConsumer));
            m_StopConsumer = stopConsumer ?? throw new ArgumentNullException(nameof(stopConsumer));
            if (tickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickRate));
            m_TickRate = tickRate;
            s_ActiveServices.Add(this);
        }

        public IReadOnlyList<TimelineRuntimePlaybackDescriptor> GetPlaybackDescriptors()
        {
            var result = new List<TimelineRuntimePlaybackDescriptor>();
            foreach (TimelineRuntimePlayback playback in m_Playbacks.Values)
                result.Add(new TimelineRuntimePlaybackDescriptor(playback));
            return new ReadOnlyCollection<TimelineRuntimePlaybackDescriptor>(result);
        }

        public static IReadOnlyList<TimelineRuntimePlaybackDescriptor> GetActivePlaybackDescriptors(
            string contentIdentity)
        {
            var result = new List<TimelineRuntimePlaybackDescriptor>();
            string identity = contentIdentity ?? string.Empty;
            for (int serviceIndex = 0; serviceIndex < s_ActiveServices.Count; serviceIndex++)
            {
                TimelineRuntimeService service = s_ActiveServices[serviceIndex];
                foreach (TimelineRuntimePlayback playback in service.m_Playbacks.Values)
                {
                    if (string.Equals(playback.Content.Identity, identity, StringComparison.Ordinal))
                        result.Add(new TimelineRuntimePlaybackDescriptor(playback));
                }
            }
            return new ReadOnlyCollection<TimelineRuntimePlaybackDescriptor>(result);
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
            handle = TimelinePlaybackHandle.Invalid;
            EnsureAvailable();
            LastFailure = string.Empty;
            TimelineRuntimePlaybackHandle runtimeHandle = NextHandle();
            ulong generation = NextGeneration();
            if (!m_RequestFactory.TryCreate(
                    timeline,
                    sourceId,
                    sourceName,
                    actionContext,
                    playbackMode,
                    sourceActivation,
                    sourceRuntimeGraph,
                    runtimeHandle,
                    generation,
                    out TimelineRuntimePrepareRequest request,
                    out string requestError))
            {
                LastFailure = requestError ?? "timeline_runtime_request_failed";
                return false;
            }
            TimelineRuntimePreparationResult preparation = TimelineRuntimePreparation.Prepare(request);
            if (!preparation.IsReady)
            {
                LastFailure = string.Join(" | ", preparation.Errors);
                return false;
            }
            TimelineRuntimePlayback playback = TimelineRuntimePreparation.CreatePlayback(
                preparation,
                runtimeHandle,
                generation,
                m_TickRate);
            bool registered = false;
            bool accepted = false;
            try
            {
                if (!playback.Start())
                    return false;
                m_Playbacks.Add(runtimeHandle.Value, playback);
                registered = true;
                Publish(playback);
                handle = new TimelinePlaybackHandle(runtimeHandle.Value);
                accepted = true;
                return true;
            }
            finally
            {
                if (!accepted)
                {
                    if (registered)
                        m_Playbacks.Remove(runtimeHandle.Value);
                    playback.Dispose();
                }
            }
        }

        public TimelinePlaybackStatus GetTimelinePlaybackStatus(TimelinePlaybackHandle handle)
        {
            if (!TryGet(handle, out TimelineRuntimePlayback playback))
                return TimelinePlaybackStatus.None;
            return playback.State switch
            {
                TimelineRuntimePlaybackState.Prepared => TimelinePlaybackStatus.Requested,
                TimelineRuntimePlaybackState.Running => TimelinePlaybackStatus.Running,
                TimelineRuntimePlaybackState.Stopping => TimelinePlaybackStatus.Running,
                TimelineRuntimePlaybackState.Completed => TimelinePlaybackStatus.Succeeded,
                TimelineRuntimePlaybackState.Stopped => TimelinePlaybackStatus.Cancelled,
                TimelineRuntimePlaybackState.Failed => TimelinePlaybackStatus.Failed,
                _ => TimelinePlaybackStatus.None
            };
        }

        public void CancelTimelinePlayback(
            TimelinePlaybackHandle handle,
            TimelinePlaybackStopContext stopContext)
        {
            EnsureAvailable();
            TimelineRuntimePlayback playback = Require(handle);
            if (playback.State == TimelineRuntimePlaybackState.Completed ||
                playback.State == TimelineRuntimePlaybackState.Stopped ||
                playback.State == TimelineRuntimePlaybackState.Disposed)
                return;
            if (!RequestStopTimelinePlayback(handle, stopContext, out TimelineRuntimeStopRequest request))
                throw new InvalidOperationException($"Timeline playback '{handle.Value}' rejected Stop.");
            try
            {
                if (!CommitStopTimelinePlayback(handle, request))
                    throw new InvalidOperationException($"Timeline playback '{handle.Value}' Stop could not be committed.");
            }
            catch
            {
                if (playback.HasPendingStop)
                    DiscardStopTimelinePlayback(handle, request);
                throw;
            }
        }

        public bool RequestStopTimelinePlayback(
            TimelinePlaybackHandle handle,
            TimelinePlaybackStopContext stopContext,
            out TimelineRuntimeStopRequest request)
        {
            EnsureAvailable();
            TimelineRuntimePlayback playback = Require(handle);
            request = default;
            if (!playback.RequestStop(stopContext))
                return false;
            request = new TimelineRuntimeStopRequest(playback, stopContext);
            bool accepted = false;
            try
            {
                accepted = m_StopConsumer.ConsumeStop(request);
                return accepted;
            }
            finally
            {
                if (!accepted)
                {
                    playback.DiscardStop();
                    if (m_StopConsumer is ITimelineRuntimeStopCommitConsumer discardConsumer)
                        discardConsumer.DiscardStop(request);
                    request = default;
                }
            }
        }

        public bool CommitStopTimelinePlayback(TimelinePlaybackHandle handle, TimelineRuntimeStopRequest request)
        {
            EnsureAvailable();
            TimelineRuntimePlayback playback = Require(handle);
            if (!request.IsPendingFor(playback) || !playback.CommitStop())
                return false;
            if (m_StopConsumer is ITimelineRuntimeStopCommitConsumer commitConsumer)
                commitConsumer.CommitStop(request);
            if (!playback.CompleteStop())
                throw new InvalidOperationException($"Timeline playback '{handle.Value}' Stop could not complete.");
            Publish(playback);
            return true;
        }

        public bool DiscardStopTimelinePlayback(TimelinePlaybackHandle handle, TimelineRuntimeStopRequest request)
        {
            EnsureAvailable();
            TimelineRuntimePlayback playback = Require(handle);
            if (!request.IsPendingFor(playback) || !playback.DiscardStop())
                return false;
            if (m_StopConsumer is ITimelineRuntimeStopCommitConsumer discardConsumer)
                discardConsumer.DiscardStop(request);
            Publish(playback);
            return true;
        }
        public TimelineRuntimePreparationResult Prepare(TimelineRuntimePrepareRequest request)
        {
            EnsureAvailable();
            TimelineRuntimePreparationResult result = TimelineRuntimePreparation.Prepare(request);
            LastFailure = result.IsReady ? string.Empty : string.Join(" | ", result.Errors);
            return result;
        }

        public TimelineRuntimePlaybackHandle CreatePlayback(TimelineRuntimePreparationResult preparation)
        {
            EnsureAvailable();
            TimelineRuntimePlaybackHandle handle = NextHandle();
            ulong generation = NextGeneration();
            TimelineRuntimePlayback playback = TimelineRuntimePreparation.CreatePlayback(
                preparation,
                handle,
                generation,
                m_TickRate);
            bool registered = false;
            bool accepted = false;
            try
            {
                m_Playbacks.Add(handle.Value, playback);
                registered = true;
                Publish(playback);
                accepted = true;
                return handle;
            }
            finally
            {
                if (!accepted)
                {
                    if (registered)
                        m_Playbacks.Remove(handle.Value);
                    playback.Dispose();
                }
            }
        }

        public bool Start(TimelineRuntimePlaybackHandle handle)
        {
            TimelineRuntimePlayback playback = Require(handle);
            bool started = playback.Start();
            if (started)
                Publish(playback);
            return started;
        }

        public bool Start(TimelinePlaybackHandle handle)
        {
            TimelineRuntimePlayback playback = Require(handle);
            bool started = playback.Start();
            if (started)
                Publish(playback);
            return started;
        }

        public TimelineRuntimeAdvanceResult Step(
            TimelineRuntimePlaybackHandle handle,
            ulong logicTick,
            int tickCount)
        {
            EnsureAvailable();
            TimelineRuntimePlayback playback = Require(handle);
            TimelineRuntimeAdvanceResult result = TimelineRuntimeStepCoordinator.Step(
                playback,
                CreateAdvanceRequest(playback, logicTick, tickCount, playback.Control),
                m_StepConsumer);
            Publish(playback);
            return result;
        }

        public TimelineRuntimeAdvanceResult Step(
            TimelinePlaybackHandle handle,
            ulong logicTick,
            int tickCount)
        {
            EnsureAvailable();
            TimelineRuntimePlayback playback = Require(handle);
            TimelineRuntimeAdvanceResult result = TimelineRuntimeStepCoordinator.Step(
                playback,
                CreateAdvanceRequest(playback, logicTick, tickCount, playback.Control),
                m_StepConsumer);
            Publish(Require(handle));
            return result;
        }

        public TimelineRuntimeAdvanceResult Step(
            TimelinePlaybackHandle handle,
            ulong logicTick,
            FixedScalar elapsedSeconds)
        {
            EnsureAvailable();
            if (elapsedSeconds < FixedScalar.Zero)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            TimelineRuntimePlayback playback = Require(handle);
            var request = new TimelineRuntimeAdvanceRequest(
                logicTick,
                playback.CursorTime,
                FixedScalar.FromRaw(checked(playback.CursorTime.Raw + (playback.Control.IsPaused ? 0L :
                    (long)decimal.Round((decimal)elapsedSeconds.Raw * playback.Control.Rate.Raw / FixedScalar.OneRaw, 0, MidpointRounding.ToEven)))),
                playback.TimeCarry,
                playback.Control);
            TimelineRuntimeAdvanceResult result = TimelineRuntimeStepCoordinator.Step(
                playback,
                request,
                m_StepConsumer);
            Publish(playback);
            return result;
        }

        static TimelineRuntimeAdvanceRequest CreateAdvanceRequest(TimelineRuntimePlayback playback, ulong logicTick, int tickCount, AbilityTimelinePlaybackControl control)
        {
            if (tickCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickCount));
            decimal numerator = (decimal)playback.CursorTime.Raw * playback.TickRate + playback.TimeCarry +
                (decimal)tickCount * (control.IsPaused ? 0L : control.Rate.Raw);
            long parityOffset = (playback.Cycle & 1) * (playback.Content.Duration.Raw & 1);
            int remainder = checked((int)(numerator % playback.TickRate));
            long targetRaw = checked((long)((numerator - remainder) / playback.TickRate));
            long doubledRemainder = (long)remainder * 2;
            if (doubledRemainder > playback.TickRate ||
                doubledRemainder == playback.TickRate && ((targetRaw ^ parityOffset) & 1) != 0)
            {
                targetRaw = checked(targetRaw + 1);
                remainder -= playback.TickRate;
            }
            return new TimelineRuntimeAdvanceRequest(logicTick, playback.CursorTime, FixedScalar.FromRaw(targetRaw), remainder, control);
        }

        public bool RequestTreeClipExit(TimelineRuntimePlaybackHandle handle, string clipAuthoringId)
        {
            EnsureAvailable();
            return Require(handle).RequestTreeClipExit(clipAuthoringId);
        }

        public TimelineRuntimeAdvanceResult Advance(
            TimelineRuntimePlaybackHandle handle,
            ulong logicTick,
            int tickCount,
            AbilityTimelinePlaybackControl control)
        {
            EnsureAvailable();
            TimelineRuntimePlayback playback = Require(handle);
            var request = CreateAdvanceRequest(playback, logicTick, tickCount, control);
            TimelineRuntimeAdvanceResult result = playback.Advance(request);
            var context = new TimelineRuntimeStepContext(playback, request, result);
            try
            {
                if (m_StepConsumer.Consume(context) != TimelineRuntimeStepDecision.Commit)
                    throw new InvalidOperationException("Timeline advance was rejected by its execution consumer.");
            }
            catch
            {
                playback.Discard(result);
                if (m_StepConsumer is ITimelineRuntimeStepCommitConsumer commitConsumer)
                    commitConsumer.Discard(context);
                throw;
            }
            Publish(playback);
            return result;
        }

        public bool CommitAdvance(
            TimelineRuntimePlaybackHandle handle,
            TimelineRuntimeAdvanceResult advance)
        {
            EnsureAvailable();
            TimelineRuntimePlayback playback = Require(handle);
            if (!playback.Commit(advance))
                return false;
            if (m_StepConsumer is ITimelineRuntimeStepCommitConsumer commitConsumer)
                commitConsumer.Commit(new TimelineRuntimeStepContext(playback, advance.Request, advance));
            Publish(playback);
            return true;
        }

        public bool DiscardAdvance(
            TimelineRuntimePlaybackHandle handle,
            TimelineRuntimeAdvanceResult advance)
        {
            EnsureAvailable();
            TimelineRuntimePlayback playback = Require(handle);
            if (!playback.Discard(advance))
                return false;
            if (m_StepConsumer is ITimelineRuntimeStepCommitConsumer commitConsumer)
                commitConsumer.Discard(new TimelineRuntimeStepContext(playback, advance.Request, advance));
            Publish(playback);
            return true;
        }

        internal void DiscardEvaluation(TimelineRuntimePlaybackHandle handle)
        {
            EnsureAvailable();
            TimelineRuntimePlayback playback = Require(handle);
            if (playback.HasPendingAdvance || playback.HasPendingStop)
                throw new InvalidOperationException("Timeline evaluation discard requires no pending candidates.");
            playback.DiscardEvaluation();
        }

        internal void ReleasePlayback(TimelineRuntimePlaybackHandle handle)
        {
            EnsureAvailable();
            TimelineRuntimePlayback playback = Require(handle);
            if (playback.HasPendingAdvance || playback.HasPendingStop)
                throw new InvalidOperationException("Timeline playback release requires no pending candidates.");
            playback.Dispose();
            m_Playbacks.Remove(handle.Value);
        }

        public TimelineRuntimePlaybackSnapshot Capture(TimelineRuntimePlaybackHandle handle)
        {
            EnsureAvailable();
            TimelineRuntimePlayback playback = Require(handle);
            if (playback.HasPendingAdvance || playback.HasPendingStop)
                throw new InvalidOperationException("Timeline playback Capture requires a committed step boundary.");
            return new TimelineRuntimePlaybackSnapshot(playback);
        }

        public bool TryGetPreparation(TimelineRuntimePlaybackHandle handle, out TimelineRuntimePreparationResult preparation)
        {
            EnsureAvailable();
            if (TryGet(handle, out TimelineRuntimePlayback playback))
            {
                preparation = playback.Preparation;
                return true;
            }
            preparation = null;
            return false;
        }

        public TimelineRuntimeRestoreCandidate PrepareRestore(
            TimelineRuntimePlaybackSnapshot snapshot,
            TimelineRuntimePreparationResult preparation)
        {
            EnsureAvailable();
            return new TimelineRuntimeRestoreCandidate(this, snapshot, preparation);
        }

        public TimelineRuntimePlaybackHandle ApplyRestore(
            TimelineRuntimeRestoreCandidate candidate)
        {
            EnsureAvailable();
            candidate.ValidateOwnedBy(this);
            if (m_Playbacks.TryGetValue(candidate.Handle.Value, out TimelineRuntimePlayback playback))
                candidate.ApplyTo(playback);
            else
            {
                playback = candidate.CreatePlayback();
                m_Playbacks.Add(playback.Handle.Value, playback);
            }
            if (m_NextPlaybackHandle <= playback.Handle.Value)
                m_NextPlaybackHandle = checked(playback.Handle.Value + 1);
            if (m_NextGeneration <= playback.Generation)
                m_NextGeneration = checked(playback.Generation + 1);
            Publish(playback);
            return playback.Handle;
        }
        public bool TryGetDescriptor(
            TimelineRuntimePlaybackHandle handle,
            out TimelineRuntimePlaybackDescriptor descriptor)
        {
            if (TryGet(handle, out TimelineRuntimePlayback playback))
            {
                descriptor = new TimelineRuntimePlaybackDescriptor(playback);
                return true;
            }
            descriptor = default;
            return false;
        }

        public bool TryGetDescriptor(
            TimelinePlaybackHandle handle,
            out TimelineRuntimePlaybackDescriptor descriptor)
        {
            if (TryGet(handle, out TimelineRuntimePlayback playback))
            {
                descriptor = new TimelineRuntimePlaybackDescriptor(playback);
                return true;
            }
            descriptor = default;
            return false;
        }

        public bool TryGetTimelinePlaybackActionContext(
            ActionContextSlot actionContext,
            out TimelinePlaybackActionContext playbackActionContext)
        {
            return m_RequestFactory.TryGetTimelinePlaybackActionContext(actionContext, out playbackActionContext);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            try
            {
                foreach (TimelineRuntimePlayback playback in m_Playbacks.Values)
                {
                    try
                    {
                        if (playback.State == TimelineRuntimePlaybackState.Prepared ||
                            playback.State == TimelineRuntimePlaybackState.Running)
                        {
                            CancelTimelinePlayback(
                                new TimelinePlaybackHandle(playback.Handle.Value),
                                new TimelinePlaybackStopContext(TimelinePlaybackStopCause.Shutdown, 0));
                        }
                    }
                    finally
                    {
                        playback.Dispose();
                    }
                }
            }
            finally
            {
                m_Disposed = true;
                m_Playbacks.Clear();
                s_ActiveServices.Remove(this);
                ObservationChanged?.Invoke();
            }
        }

        TimelineRuntimePlaybackHandle NextHandle()
        {
            if (m_NextPlaybackHandle == 0)
                throw new InvalidOperationException("Timeline playback handle space is exhausted.");
            return new TimelineRuntimePlaybackHandle(m_NextPlaybackHandle++);
        }

        ulong NextGeneration()
        {
            if (m_NextGeneration == 0)
                throw new InvalidOperationException("Timeline playback generation space is exhausted.");
            return m_NextGeneration++;
        }

        bool TryGet(TimelinePlaybackHandle handle, out TimelineRuntimePlayback playback)
        {
            if (handle.IsValid && m_Playbacks.TryGetValue(handle.Value, out playback))
                return true;
            playback = null;
            return false;
        }

        internal bool TryGetPlayback(TimelineRuntimePlaybackHandle handle, out TimelineRuntimePlayback playback)
        {
            if (handle.IsValid && m_Playbacks.TryGetValue(handle.Value, out playback))
                return true;
            playback = null;
            return false;
        }

        bool TryGet(TimelineRuntimePlaybackHandle handle, out TimelineRuntimePlayback playback)
        {
            return TryGetPlayback(handle, out playback);
        }

        TimelineRuntimePlayback Require(TimelineRuntimePlaybackHandle handle)
        {
            EnsureAvailable();
            return TryGet(handle, out TimelineRuntimePlayback playback)
                ? playback
                : throw new InvalidOperationException($"Timeline playback '{handle.Value}' is not registered.");
        }

        TimelineRuntimePlayback Require(TimelinePlaybackHandle handle)
        {
            EnsureAvailable();
            return TryGet(handle, out TimelineRuntimePlayback playback)
                ? playback
                : throw new InvalidOperationException($"Timeline playback '{handle.Value}' is not registered.");
        }

        void EnsureAvailable()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(TimelineRuntimeService));
        }

        void Publish(TimelineRuntimePlayback playback)
        {
            PlaybackChanged?.Invoke(new TimelineRuntimePlaybackDescriptor(playback));
            ObservationChanged?.Invoke();
        }
    }
}

