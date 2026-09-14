using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BTSMTL.Diagnostics;
using TreeDesigner;

namespace BTSMTL.Timeline.Runtime
{
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
            m_OwnerIdentity = SimulationIdentity.Require(ownerIdentity, nameof(ownerIdentity));
            m_CallIdentity = SimulationIdentity.Require(callIdentity, nameof(callIdentity));
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
            m_Bindings[SimulationIdentity.Require(bindingId, nameof(bindingId))] = handle;
        }

        public void BindDependency(string dependencyIdentity, TimelineRuntimeDependencyHandle handle)
        {
            if (!handle.IsValid)
                throw new ArgumentOutOfRangeException(nameof(handle));
            m_Dependencies[SimulationIdentity.Require(dependencyIdentity, nameof(dependencyIdentity))] = handle;
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

    public sealed class TimelineRuntimeEvaluationBuffer : ITimelineRuntimeEvaluationSink
    {
        readonly Dictionary<ulong, TimelineRuntimeEvaluationResult> m_Pending =
            new Dictionary<ulong, TimelineRuntimeEvaluationResult>();
        readonly Dictionary<ulong, TimelineRuntimeEvaluationResult> m_Committed =
            new Dictionary<ulong, TimelineRuntimeEvaluationResult>();

        public event Action<TimelineRuntimeStepContext> Committed;

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
                m_Committed[handle] = result;
                m_Pending.Remove(handle);
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
        }

        public void DiscardStop(TimelineRuntimeStopRequest request)
        {
            m_Pending.Remove(request.Handle.Value);
        }

        public bool TryGetCommitted(
            TimelineRuntimePlaybackHandle handle,
            out TimelineRuntimeEvaluationResult result)
        {
            return m_Committed.TryGetValue(handle.Value, out result);
        }

        public void Clear()
        {
            m_Pending.Clear();
            m_Committed.Clear();
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
            ITimelineRuntimeTreeClipService treeClipService)
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
                treeClipService);
            m_Service = new TimelineRuntimeService(requestFactory, consumer, consumer);
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

        public TimelineRuntimePlaybackHandle CreateStartedPlayback(
            TimelineRuntimePreparationResult preparation)
        {
            TimelineRuntimePlaybackHandle handle = m_Service.CreatePlayback(preparation);
            if (!m_Service.Start(handle))
                throw new InvalidOperationException($"Timeline playback '{handle.Value}' could not start.");
            return handle;
        }

        public TimelineRuntimeAdvanceResult Step(
            TimelineRuntimePlaybackHandle handle,
            ulong logicTick,
            int deltaFrames)
        {
            return m_Service.Step(handle, logicTick, deltaFrames);
        }

        public void Stop(
            TimelineRuntimePlaybackHandle handle,
            TimelinePlaybackStopContext stopContext)
        {
            m_Service.CancelTimelinePlayback(
                new TimelinePlaybackHandle(handle.Value),
                stopContext);
        }

        public TimelineRuntimePlaybackSnapshot Capture(TimelineRuntimePlaybackHandle handle)
        {
            return m_Service.Capture(handle);
        }

        public TimelineRuntimePlayback Restore(
            TimelineRuntimePlaybackSnapshot snapshot,
            TimelineRuntimePreparationResult preparation)
        {
            return m_Service.Restore(snapshot, preparation);
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
