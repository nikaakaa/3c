using System;
using System.Collections.Generic;

namespace BTSMTL.Timeline.Runtime
{
    public sealed class TimelineRuntimePrepareRequest
    {
        public TimelineRuntimePrepareRequest(
            string requestId,
            TimelineData timeline,
            TimelineContractCatalog contractCatalog,
            TimelineExecutionIdentity executionIdentity,
            IEnumerable<TimelineCallBinding> callBindings,
            ITimelineDomainBindingResolver domainResolver)
        {
            RequestId = string.IsNullOrWhiteSpace(requestId)
                ? throw new ArgumentException("Timeline prepare request identity is required.", nameof(requestId))
                : requestId.Trim();
            Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            ContractCatalog = contractCatalog ?? throw new ArgumentNullException(nameof(contractCatalog));
            if (!executionIdentity.IsValid)
                throw new ArgumentException("Timeline execution identity is invalid.", nameof(executionIdentity));
            ExecutionIdentity = executionIdentity;
            CallBindings = new List<TimelineCallBinding>(callBindings ?? Array.Empty<TimelineCallBinding>()).AsReadOnly();
            DomainResolver = domainResolver ?? throw new ArgumentNullException(nameof(domainResolver));
        }

        public string RequestId { get; }
        public TimelineData Timeline { get; }
        public TimelineContractCatalog ContractCatalog { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public IReadOnlyList<TimelineCallBinding> CallBindings { get; }
        public ITimelineDomainBindingResolver DomainResolver { get; }
    }

    public enum TimelineRuntimePreparationStatus : byte
    {
        Failed = 0,
        Ready = 1
    }

    public sealed class TimelineRuntimePreparationResult
    {
        TimelineRuntimePreparationResult(
            TimelineRuntimePreparationStatus status,
            string requestId,
            TimelineExecutionIdentity executionIdentity,
            TimelineContentUnit content,
            TimelineBindingPlan bindingPlan,
            TimelineCallInput callInput,
            TimelinePreparedBindings preparedBindings,
            IReadOnlyList<string> errors)
        {
            Status = status;
            RequestId = requestId ?? string.Empty;
            ExecutionIdentity = executionIdentity;
            Content = content;
            BindingPlan = bindingPlan;
            CallInput = callInput;
            PreparedBindings = preparedBindings;
            Errors = errors ?? Array.Empty<string>();
        }

        public TimelineRuntimePreparationStatus Status { get; }
        public string RequestId { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public TimelineContentUnit Content { get; }
        public string ContentRevision => Content?.ContentHash ?? string.Empty;
        public TimelineBindingPlan BindingPlan { get; }
        public TimelineCallInput CallInput { get; }
        public TimelinePreparedBindings PreparedBindings { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool IsReady => Status == TimelineRuntimePreparationStatus.Ready &&
                               Content != null &&
                               BindingPlan != null &&
                               CallInput != null &&
                               PreparedBindings != null &&
                               Errors.Count == 0;

        internal static TimelineRuntimePreparationResult Failed(
            string requestId,
            TimelineExecutionIdentity executionIdentity,
            IEnumerable<string> errors)
        {
            return new TimelineRuntimePreparationResult(
                TimelineRuntimePreparationStatus.Failed,
                requestId,
                executionIdentity,
                null,
                null,
                null,
                null,
                new List<string>(errors ?? Array.Empty<string>()).AsReadOnly());
        }

        internal static TimelineRuntimePreparationResult Ready(
            TimelineRuntimePrepareRequest request,
            TimelineContentUnit content,
            TimelineBindingPlan bindingPlan,
            TimelineCallInput callInput,
            TimelinePreparedBindings preparedBindings)
        {
            return new TimelineRuntimePreparationResult(
                TimelineRuntimePreparationStatus.Ready,
                request.RequestId,
                request.ExecutionIdentity,
                content,
                bindingPlan,
                callInput,
                preparedBindings,
                Array.Empty<string>());
        }
    }

    public static class TimelineRuntimePreparation
    {
        public static TimelineRuntimePreparationResult Prepare(TimelineRuntimePrepareRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            TimelineContentDiscoveryResult discovery = TimelineContentDiscovery.Discover(
                request.Timeline,
                request.ContractCatalog);
            if (!discovery.IsValid)
                return TimelineRuntimePreparationResult.Failed(
                    request.RequestId,
                    request.ExecutionIdentity,
                    discovery.Errors);

            try
            {
                TimelineBindingPlan bindingPlan = new TimelineBindingPlan(discovery.Content);
                TimelineCallInput callInput = new TimelineCallInput(bindingPlan, request.CallBindings);
                var errors = new List<string>();
                TimelinePreparedBindings preparedBindings = TimelineBindingPreparation.Prepare(
                    bindingPlan,
                    callInput,
                    request.DomainResolver,
                    errors);
                if (preparedBindings == null || errors.Count != 0)
                    return TimelineRuntimePreparationResult.Failed(
                        request.RequestId,
                        request.ExecutionIdentity,
                        errors);
                return TimelineRuntimePreparationResult.Ready(
                    request,
                    discovery.Content,
                    bindingPlan,
                    callInput,
                    preparedBindings);
            }
            catch (Exception exception)
            {
                return TimelineRuntimePreparationResult.Failed(
                    request.RequestId,
                    request.ExecutionIdentity,
                    new[] { exception.Message });
            }
        }
    }
}
