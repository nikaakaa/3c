using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BTSMTL.Diagnostics;
using TreeDesigner;

namespace BTSMTL.Timeline.Runtime
{
    public sealed class TimelineRuntimeCompositionHost : ITimelinePlaybackService, IDisposable
    {
        readonly TimelineRuntimeComposition m_Composition;
        bool m_Disposed;

        public TimelineRuntimeCompositionHost(
            TimelineContractCatalog contractCatalog,
            TimelineRuntimeNumericTarget numericTarget,
            ITimelineRuntimeCallBindingSource callBindingSource,
            ITimelineDomainBindingResolver domainResolver,
            ITimelineRuntimeDependencyResolver dependencyResolver,
            IEnumerable<ITimelineRuntimeEvaluationSink> evaluationSinks,
            ITimelineRuntimeTreeClipService treeClipService)
        {
            if (evaluationSinks == null)
                throw new ArgumentNullException(nameof(evaluationSinks));
            var sinks = new List<ITimelineRuntimeEvaluationSink>
            {
                EvaluationBuffer
            };
            foreach (ITimelineRuntimeEvaluationSink sink in evaluationSinks)
            {
                if (sink == null)
                    throw new ArgumentException("Timeline runtime composition contains a null evaluation sink.", nameof(evaluationSinks));
                if (ReferenceEquals(sink, EvaluationBuffer))
                    continue;
                sinks.Add(sink);
            }
            var fanout = new TimelineRuntimeEvaluationFanout(
                new ReadOnlyCollection<ITimelineRuntimeEvaluationSink>(sinks));
            m_Composition = new TimelineRuntimeComposition(
                contractCatalog,
                numericTarget,
                callBindingSource,
                domainResolver,
                dependencyResolver,
                fanout,
                treeClipService);
        }

        public TimelineRuntimeEvaluationBuffer EvaluationBuffer { get; } =
            new TimelineRuntimeEvaluationBuffer();

        public TimelineRuntimeComposition Composition => m_Composition;
        public TimelineRuntimeService Service => m_Composition.Service;
        public string LastFailure => Service.LastFailure;

        public event Action<TimelineRuntimePlaybackDescriptor> PlaybackChanged
        {
            add => Service.PlaybackChanged += value;
            remove => Service.PlaybackChanged -= value;
        }

        public event Action<TimelineRuntimeCommittedEvaluation> CommittedEvaluation
        {
            add => EvaluationBuffer.CommittedEvaluation += value;
            remove => EvaluationBuffer.CommittedEvaluation -= value;
        }

        public event Action<TimelineRuntimeStopRequest> StopCommitted
        {
            add => EvaluationBuffer.StopCommitted += value;
            remove => EvaluationBuffer.StopCommitted -= value;
        }

        public TimelineRuntimePreparationResult Prepare(
            string requestId,
            TimelineData timeline,
            TimelineExecutionIdentity executionIdentity,
            TimelinePlaybackMode playbackMode,
            IEnumerable<TimelineCallBinding> callBindings)
        {
            EnsureAvailable();
            return m_Composition.Prepare(
                requestId,
                timeline,
                executionIdentity,
                playbackMode,
                callBindings);
        }

        public TimelineRuntimePlaybackHandle CreateStartedPlayback(
            TimelineRuntimePreparationResult preparation)
        {
            EnsureAvailable();
            return m_Composition.CreateStartedPlayback(preparation);
        }

        public TimelineRuntimeAdvanceResult Step(
            TimelineRuntimePlaybackHandle handle,
            ulong logicTick,
            int deltaFrames)
        {
            EnsureAvailable();
            return m_Composition.Step(handle, logicTick, deltaFrames);
        }

        public TimelineRuntimePlaybackSnapshot Capture(TimelineRuntimePlaybackHandle handle)
        {
            EnsureAvailable();
            return m_Composition.Capture(handle);
        }

        public TimelineRuntimePlayback Restore(
            TimelineRuntimePlaybackSnapshot snapshot,
            TimelineRuntimePreparationResult preparation)
        {
            EnsureAvailable();
            return m_Composition.Restore(snapshot, preparation);
        }

        public void Stop(
            TimelineRuntimePlaybackHandle handle,
            TimelinePlaybackStopContext stopContext)
        {
            EnsureAvailable();
            m_Composition.Stop(handle, stopContext);
        }

        public bool TryGetPlaybackDescriptor(
            TimelineRuntimePlaybackHandle handle,
            out TimelineRuntimePlaybackDescriptor descriptor)
        {
            EnsureAvailable();
            return Service.TryGetDescriptor(handle, out descriptor);
        }

        public bool TryGetPlaybackDescriptor(
            TimelinePlaybackHandle handle,
            out TimelineRuntimePlaybackDescriptor descriptor)
        {
            EnsureAvailable();
            return Service.TryGetDescriptor(handle, out descriptor);
        }

        public IReadOnlyList<TimelineRuntimePlaybackDescriptor> GetPlaybackDescriptors()
        {
            EnsureAvailable();
            return Service.GetPlaybackDescriptors();
        }

        public bool TryGetCommittedEvaluation(
            TimelineRuntimePlaybackHandle handle,
            out TimelineRuntimeCommittedEvaluation evaluation)
        {
            EnsureAvailable();
            return EvaluationBuffer.TryGetCommittedEvaluation(handle, out evaluation);
        }

        public bool TryGetCommittedEvaluation(
            TimelinePlaybackHandle handle,
            out TimelineRuntimeCommittedEvaluation evaluation)
        {
            EnsureAvailable();
            if (!handle.IsValid)
            {
                evaluation = default;
                return false;
            }
            return EvaluationBuffer.TryGetCommittedEvaluation(new TimelineRuntimePlaybackHandle(handle.Value), out evaluation);
        }

        public bool TryGetCommittedEvaluationResult(
            TimelineRuntimePlaybackHandle handle,
            out TimelineRuntimeEvaluationResult result)
        {
            EnsureAvailable();
            return EvaluationBuffer.TryGetCommitted(handle, out result);
        }

        public bool TryGetCommittedEvaluationResult(
            TimelinePlaybackHandle handle,
            out TimelineRuntimeEvaluationResult result)
        {
            EnsureAvailable();
            if (!handle.IsValid)
            {
                result = null;
                return false;
            }
            return EvaluationBuffer.TryGetCommitted(new TimelineRuntimePlaybackHandle(handle.Value), out result);
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
            EnsureAvailable();
            return m_Composition.RequestTimelinePlayback(
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
            EnsureAvailable();
            return m_Composition.GetTimelinePlaybackStatus(handle);
        }

        public void CancelTimelinePlayback(
            TimelinePlaybackHandle handle,
            TimelinePlaybackStopContext stopContext)
        {
            EnsureAvailable();
            m_Composition.CancelTimelinePlayback(handle, stopContext);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Composition.Dispose();
            EvaluationBuffer.Clear();
        }

        void EnsureAvailable()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(TimelineRuntimeCompositionHost));
        }
    }
}
