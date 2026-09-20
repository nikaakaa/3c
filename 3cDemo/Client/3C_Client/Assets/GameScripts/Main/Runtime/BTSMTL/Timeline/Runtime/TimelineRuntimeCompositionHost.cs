using System;
using ThirdPersonSimulation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BTSMTL.Diagnostics;
using TreeDesigner;

namespace BTSMTL.Timeline.Runtime
{
    public sealed class TimelineRuntimeCompositionHost : ITimelinePlaybackService, IDisposable
    {
        readonly TimelineRuntimeComposition m_Composition;
        readonly TimelineRuntimePresentationDriver m_PresentationDriver =
            new TimelineRuntimePresentationDriver();
        bool m_Disposed;

        public TimelineRuntimeCompositionHost(
            TimelineContractCatalog contractCatalog,
            TimelineRuntimeNumericTarget numericTarget,
            ITimelineRuntimeCallBindingSource callBindingSource,
            ITimelineDomainBindingResolver domainResolver,
            ITimelineRuntimeDependencyResolver dependencyResolver,
            IEnumerable<ITimelineRuntimeEvaluationSink> evaluationSinks,
            ITimelineRuntimeTreeClipService treeClipService,
            ITimelineRuntimeMarkerService markerService,
            int tickRate)
        {
            if (evaluationSinks == null)
                throw new ArgumentNullException(nameof(evaluationSinks));
            var sinks = new List<ITimelineRuntimeEvaluationSink>
            {
                EvaluationBuffer,
                m_PresentationDriver
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
                treeClipService,
                markerService,
                tickRate);
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

        public bool TryPresent(
            TimelineRuntimePlaybackHandle handle,
            in TimelineRuntimePresentationSample sample,
            ulong presentationFrame,
            float presentationDeltaSeconds,
            float interpolationAlpha,
            out TimelineRuntimePresentationFrame frame)
        {
            EnsureAvailable();
            return m_PresentationDriver.TryPresent(
                Service,
                handle,
                in sample,
                presentationFrame,
                presentationDeltaSeconds,
                interpolationAlpha,
                out frame);
        }

        public bool TryPresent(
            TimelinePlaybackHandle handle,
            in TimelineRuntimePresentationSample sample,
            ulong presentationFrame,
            float presentationDeltaSeconds,
            float interpolationAlpha,
            out TimelineRuntimePresentationFrame frame)
        {
            return TryPresent(
                new TimelineRuntimePlaybackHandle(handle.Value),
                in sample,
                presentationFrame,
                presentationDeltaSeconds,
                interpolationAlpha,
                out frame);
        }

        public void CommitPresentationFrame(ulong frame) => m_PresentationDriver.CommitPresentationFrame(frame);
        public void DiscardPresentationFrame(ulong frame) => m_PresentationDriver.DiscardPresentationFrame(frame);
        public void SuspendPresentationPlayback(TimelineRuntimePlaybackHandle handle, ulong generation) => m_PresentationDriver.SuspendPresentationPlayback(handle, generation);
        public void ReleasePresentationPlayback(TimelineRuntimePlaybackHandle handle, ulong generation) => m_PresentationDriver.ReleasePresentationPlayback(handle, generation);

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

        public TimelineRuntimePlaybackHandle CreatePlayback(
            TimelineRuntimePreparationResult preparation)
        {
            EnsureAvailable();
            return m_Composition.CreatePlayback(preparation);
        }

        public bool Start(TimelineRuntimePlaybackHandle handle)
        {
            EnsureAvailable();
            return m_Composition.Start(handle);
        }

        public TimelineRuntimeAdvanceResult Advance(
            TimelineRuntimePlaybackHandle handle,
            ulong logicTick,
            int tickCount,
            AbilityTimelinePlaybackControl control)
        {
            EnsureAvailable();
            return m_Composition.Advance(handle, logicTick, tickCount, control);
        }

        public bool CommitAdvance(
            TimelineRuntimePlaybackHandle handle,
            TimelineRuntimeAdvanceResult advance)
        {
            EnsureAvailable();
            return m_Composition.CommitAdvance(handle, advance);
        }

        public bool DiscardAdvance(
            TimelineRuntimePlaybackHandle handle,
            TimelineRuntimeAdvanceResult advance)
        {
            EnsureAvailable();
            return m_Composition.DiscardAdvance(handle, advance);
        }

        public TimelineRuntimePlaybackSnapshot Capture(
            TimelineRuntimePlaybackHandle handle)
        {
            EnsureAvailable();
            return m_Composition.Capture(handle);
        }

        public TimelineRuntimeRestoreCandidate PrepareRestore(
            TimelineRuntimePlaybackSnapshot snapshot,
            TimelineRuntimePreparationResult preparation)
        {
            EnsureAvailable();
            return m_Composition.PrepareRestore(snapshot, preparation);
        }

        public TimelineRuntimePlaybackHandle ApplyRestore(
            TimelineRuntimeRestoreCandidate candidate)
        {
            EnsureAvailable();
            return m_Composition.ApplyRestore(candidate);
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
            m_PresentationDriver.Clear();
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
