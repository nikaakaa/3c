using System;
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
        readonly ReadOnlyCollection<string> m_ActiveClipIds;

        internal TimelineRuntimeStopRequest(
            TimelineRuntimePlayback playback,
            TimelinePlaybackStopContext reason)
        {
            Playback = playback ?? throw new ArgumentNullException(nameof(playback));
            Reason = reason;
            m_ActiveClipIds = new ReadOnlyCollection<string>(
                new List<string>(playback.ActiveClipIds));
        }

        public TimelineRuntimePlayback Playback { get; }
        public TimelinePlaybackStopContext Reason { get; }
        public TimelineRuntimePlaybackHandle Handle => Playback.Handle;
        public TimelineExecutionIdentity ExecutionIdentity => Playback.ExecutionIdentity;
        public ulong Generation => Playback.Generation;
        public IReadOnlyList<string> ActiveClipIds => m_ActiveClipIds;
    }

    public sealed class TimelineRuntimeExecutionConsumer :
        ITimelineRuntimeStepConsumer,
        ITimelineRuntimeStepCommitConsumer,
        ITimelineRuntimeStopConsumer,
        ITimelineRuntimeStopCommitConsumer
    {
        readonly ITimelineRuntimeEvaluationSink m_EvaluationSink;
        readonly ITimelineRuntimeTreeClipService m_TreeClipService;

        public TimelineRuntimeExecutionConsumer(
            ITimelineRuntimeEvaluationSink evaluationSink,
            ITimelineRuntimeTreeClipService treeClipService)
        {
            m_EvaluationSink = evaluationSink ?? throw new ArgumentNullException(nameof(evaluationSink));
            m_TreeClipService = treeClipService ?? throw new ArgumentNullException(nameof(treeClipService));
        }

        public TimelineRuntimeStepDecision Consume(TimelineRuntimeStepContext context)
        {
            for (int index = 0; index < context.Advance.Evaluation.TreeClips.Count; index++)
            {
                TimelineRuntimeTreeClipRequest request = context.Advance.Evaluation.TreeClips[index];
                if (!m_TreeClipService.Consume(request, context))
                    return TimelineRuntimeStepDecision.Discard;
            }
            return m_EvaluationSink.Consume(context)
                ? TimelineRuntimeStepDecision.Commit
                : TimelineRuntimeStepDecision.Discard;
        }

        public bool ConsumeStop(TimelineRuntimeStopRequest request)
        {
            return m_TreeClipService.ConsumeStop(request) &&
                   m_EvaluationSink.ConsumeStop(request);
        }

        public void Commit(TimelineRuntimeStepContext context)
        {
            m_TreeClipService.Commit(context);
            m_EvaluationSink.Commit(context);
        }

        public void Discard(TimelineRuntimeStepContext context)
        {
            m_TreeClipService.DiscardStep(context);
            m_EvaluationSink.Discard(context);
        }

        public void CommitStop(TimelineRuntimeStopRequest request)
        {
            m_TreeClipService.CommitStop(request);
            m_EvaluationSink.CommitStop(request);
        }

        public void DiscardStop(TimelineRuntimeStopRequest request)
        {
            m_TreeClipService.DiscardStop(request);
            m_EvaluationSink.DiscardStop(request);
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
            NumericTarget = playback.NumericTarget;
            ContentRevision = playback.ContentRevision;
            State = playback.State;
        }

        public TimelineRuntimePlaybackHandle Handle { get; }
        public ulong Generation { get; }
        public string RequestId { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public TimelineRuntimeNumericTarget NumericTarget { get; }
        public string ContentRevision { get; }
        public TimelineRuntimePlaybackState State { get; }
        public bool IsValid => Handle.IsValid && Generation != 0 && ExecutionIdentity.IsValid;
    }

    public sealed class TimelineRuntimePlaybackSnapshot
    {
        public const string CurrentSchema = "btsmtl.timeline.direct-runtime.v1";

        internal TimelineRuntimePlaybackSnapshot(TimelineRuntimePlayback playback)
        {
            Schema = CurrentSchema;
            Handle = playback.Handle;
            Generation = playback.Generation;
            RequestId = playback.RequestId;
            ExecutionIdentity = playback.ExecutionIdentity;
            NumericTarget = playback.NumericTarget;
            ContentRevision = playback.ContentRevision;
            State = playback.State;
            CursorFrame = playback.CursorFrame;
            Cycle = playback.Cycle;
            SectionId = playback.SectionId;
            ActiveClipIds = new ReadOnlyCollection<string>(
                new List<string>(playback.ActiveClipIds));
            HasStopContext = playback.HasStopContext;
            StopContext = playback.StopContext;
        }

        public string Schema { get; }
        public TimelineRuntimePlaybackHandle Handle { get; }
        public ulong Generation { get; }
        public string RequestId { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public TimelineRuntimeNumericTarget NumericTarget { get; }
        public string ContentRevision { get; }
        public TimelineRuntimePlaybackState State { get; }
        public int CursorFrame { get; }
        public int Cycle { get; }
        public string SectionId { get; }
        public IReadOnlyList<string> ActiveClipIds { get; }
        public bool HasStopContext { get; }
        public TimelinePlaybackStopContext StopContext { get; }
    }

    public sealed class TimelineRuntimeService : ITimelinePlaybackService, ITimelinePlaybackActionContextSource, IDisposable
    {
        readonly ITimelineRuntimePlaybackRequestFactory m_RequestFactory;
        readonly ITimelineRuntimeStepConsumer m_StepConsumer;
        readonly ITimelineRuntimeStopConsumer m_StopConsumer;
        readonly Dictionary<ulong, TimelineRuntimePlayback> m_Playbacks =
            new Dictionary<ulong, TimelineRuntimePlayback>();
        ulong m_NextPlaybackHandle = 1;
        ulong m_NextGeneration = 1;
        bool m_Disposed;

        public event Action<TimelineRuntimePlaybackDescriptor> PlaybackChanged;

        public TimelineRuntimeService(
            ITimelineRuntimePlaybackRequestFactory requestFactory,
            ITimelineRuntimeStepConsumer stepConsumer,
            ITimelineRuntimeStopConsumer stopConsumer)
        {
            m_RequestFactory = requestFactory ?? throw new ArgumentNullException(nameof(requestFactory));
            m_StepConsumer = stepConsumer ?? throw new ArgumentNullException(nameof(stepConsumer));
            m_StopConsumer = stopConsumer ?? throw new ArgumentNullException(nameof(stopConsumer));
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
                    out _))
                return false;
            TimelineRuntimePreparationResult preparation = TimelineRuntimePreparation.Prepare(request);
            if (!preparation.IsReady)
                return false;
            TimelineRuntimePlayback playback = TimelineRuntimePreparation.CreatePlayback(
                preparation,
                runtimeHandle,
                generation);
            if (!playback.Start())
                return false;
            m_Playbacks.Add(runtimeHandle.Value, playback);
            handle = new TimelinePlaybackHandle(runtimeHandle.Value);
            Publish(playback);
            return true;
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
            if (!playback.RequestStop(stopContext))
                throw new InvalidOperationException($"Timeline playback '{handle.Value}' rejected Stop.");
            TimelineRuntimeStopRequest request = new TimelineRuntimeStopRequest(playback, stopContext);
            bool stopCommitted = false;
            try
            {
                if (!m_StopConsumer.ConsumeStop(request))
                {
                    playback.DiscardStop();
                    throw new InvalidOperationException($"Timeline playback '{handle.Value}' Stop was rejected by the runtime owner.");
                }
                if (!playback.CommitStop())
                    throw new InvalidOperationException($"Timeline playback '{handle.Value}' Stop could not be committed.");
                stopCommitted = true;
                if (m_StopConsumer is ITimelineRuntimeStopCommitConsumer committedStopConsumer)
                    committedStopConsumer.CommitStop(request);
                if (!playback.CompleteStop())
                    throw new InvalidOperationException($"Timeline playback '{handle.Value}' Stop could not complete.");
                Publish(playback);
            }
            catch
            {
                if (playback.HasPendingStop)
                    playback.DiscardStop();
                if (!stopCommitted && m_StopConsumer is ITimelineRuntimeStopCommitConsumer failedStopConsumer)
                    failedStopConsumer.DiscardStop(request);
                throw;
            }
        }

        public TimelineRuntimePreparationResult Prepare(TimelineRuntimePrepareRequest request)
        {
            EnsureAvailable();
            return TimelineRuntimePreparation.Prepare(request);
        }

        public TimelineRuntimePlaybackHandle CreatePlayback(TimelineRuntimePreparationResult preparation)
        {
            EnsureAvailable();
            TimelineRuntimePlaybackHandle handle = NextHandle();
            ulong generation = NextGeneration();
            TimelineRuntimePlayback playback = TimelineRuntimePreparation.CreatePlayback(
                preparation,
                handle,
                generation);
            m_Playbacks.Add(handle.Value, playback);
            Publish(playback);
            return handle;
        }

        public bool Start(TimelineRuntimePlaybackHandle handle)
        {
            return Require(handle).Start();
        }

        public bool Start(TimelinePlaybackHandle handle)
        {
            return Require(handle).Start();
        }

        public TimelineRuntimeAdvanceResult Step(
            TimelineRuntimePlaybackHandle handle,
            ulong logicTick,
            int deltaFrames)
        {
            EnsureAvailable();
            TimelineRuntimePlayback playback = Require(handle);
            TimelineRuntimeAdvanceResult result = TimelineRuntimeStepCoordinator.Step(
                playback,
                new TimelineRuntimeAdvanceRequest(logicTick, deltaFrames),
                m_StepConsumer);
            Publish(playback);
            return result;
        }

        public TimelineRuntimeAdvanceResult Step(
            TimelinePlaybackHandle handle,
            ulong logicTick,
            int deltaFrames)
        {
            EnsureAvailable();
            TimelineRuntimeAdvanceResult result = TimelineRuntimeStepCoordinator.Step(
                Require(handle),
                new TimelineRuntimeAdvanceRequest(logicTick, deltaFrames),
                m_StepConsumer);
            Publish(Require(handle));
            return result;
        }

        public TimelineRuntimePlaybackSnapshot Capture(TimelineRuntimePlaybackHandle handle)
        {
            EnsureAvailable();
            TimelineRuntimePlayback playback = Require(handle);
            if (playback.HasPendingAdvance || playback.HasPendingStop)
                throw new InvalidOperationException("Timeline playback Capture requires a committed step boundary.");
            return new TimelineRuntimePlaybackSnapshot(playback);
        }

        public TimelineRuntimePlayback Restore(
            TimelineRuntimePlaybackSnapshot snapshot,
            TimelineRuntimePreparationResult preparation)
        {
            EnsureAvailable();
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (preparation == null || !preparation.IsReady)
                throw new InvalidOperationException("Timeline playback Restore requires a ready preparation.");
            if (!string.Equals(snapshot.Schema, TimelineRuntimePlaybackSnapshot.CurrentSchema, StringComparison.Ordinal) ||
                !snapshot.Handle.IsValid ||
                snapshot.Generation == 0 ||
                snapshot.State == TimelineRuntimePlaybackState.Disposed ||
                snapshot.State == TimelineRuntimePlaybackState.Failed ||
                snapshot.ExecutionIdentity != preparation.ExecutionIdentity ||
                snapshot.NumericTarget != preparation.NumericTarget ||
                !string.Equals(snapshot.ContentRevision, preparation.ContentRevision, StringComparison.Ordinal))
                throw new InvalidOperationException("Timeline playback Restore snapshot does not match the prepared content.");
            if (m_Playbacks.ContainsKey(snapshot.Handle.Value))
                throw new InvalidOperationException($"Timeline playback handle '{snapshot.Handle.Value}' is already active.");
            TimelineRuntimePlayback playback = TimelineRuntimePreparation.CreatePlayback(
                preparation,
                snapshot.Handle,
                snapshot.Generation);
            if (!playback.RestoreCommittedState(
                    snapshot.State,
                    snapshot.CursorFrame,
                    snapshot.Cycle,
                    snapshot.SectionId,
                    snapshot.ActiveClipIds,
                    snapshot.HasStopContext,
                    snapshot.StopContext))
                throw new InvalidOperationException("Timeline playback Restore state is not a committed state.");
            m_Playbacks.Add(snapshot.Handle.Value, playback);
            Publish(playback);
            return playback;
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

        bool TryGet(TimelineRuntimePlaybackHandle handle, out TimelineRuntimePlayback playback)
        {
            if (handle.IsValid && m_Playbacks.TryGetValue(handle.Value, out playback))
                return true;
            playback = null;
            return false;
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
        }
    }
}
