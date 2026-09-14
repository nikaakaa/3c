using System;
using System.Collections.Generic;

namespace BTSMTL.Timeline.Runtime
{
    public enum TimelineRuntimeNumericTarget : byte
    {
        Float32 = 0,
        Fixed = 1
    }

    public readonly struct TimelineRuntimeDependencyHandle : IEquatable<TimelineRuntimeDependencyHandle>
    {
        public TimelineRuntimeDependencyHandle(int value)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        public int Value { get; }
        public bool IsValid => Value > 0;
        public bool Equals(TimelineRuntimeDependencyHandle other) => Value == other.Value;
        public override bool Equals(object obj) => obj is TimelineRuntimeDependencyHandle other && Equals(other);
        public override int GetHashCode() => Value;
        public static TimelineRuntimeDependencyHandle Invalid => default;
    }

    public interface ITimelineRuntimeDependencyResolver
    {
        bool TryResolve(
            TimelineContentDependency dependency,
            TimelineRuntimeNumericTarget numericTarget,
            out TimelineRuntimeDependencyHandle handle,
            out string error);
    }

    public sealed class TimelineRuntimePreparedDependencies
    {
        readonly Dictionary<string, TimelineRuntimeDependencyHandle> m_Handles;

        internal TimelineRuntimePreparedDependencies(
            IReadOnlyList<TimelineContentDependency> dependencies,
            IReadOnlyList<TimelineRuntimeDependencyHandle> handles)
        {
            m_Handles = new Dictionary<string, TimelineRuntimeDependencyHandle>(StringComparer.Ordinal);
            for (int index = 0; index < dependencies.Count; index++)
                m_Handles.Add(dependencies[index].Identity, handles[index]);
        }

        public bool TryGetHandle(string dependencyIdentity, out TimelineRuntimeDependencyHandle handle)
        {
            if (m_Handles.TryGetValue(dependencyIdentity ?? string.Empty, out handle))
                return handle.IsValid;
            handle = TimelineRuntimeDependencyHandle.Invalid;
            return false;
        }
    }

    public readonly struct TimelineRuntimePlaybackHandle : IEquatable<TimelineRuntimePlaybackHandle>
    {
        public TimelineRuntimePlaybackHandle(ulong value)
        {
            if (value == 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        public ulong Value { get; }
        public bool IsValid => Value != 0;
        public bool Equals(TimelineRuntimePlaybackHandle other) => Value == other.Value;
        public override bool Equals(object obj) => obj is TimelineRuntimePlaybackHandle other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static TimelineRuntimePlaybackHandle Invalid => default;
        public static bool operator ==(TimelineRuntimePlaybackHandle left, TimelineRuntimePlaybackHandle right) => left.Equals(right);
        public static bool operator !=(TimelineRuntimePlaybackHandle left, TimelineRuntimePlaybackHandle right) => !left.Equals(right);
    }

    public sealed class TimelineRuntimePlayback : IDisposable
    {
        internal TimelineRuntimePlayback(
            TimelineRuntimePlaybackHandle handle,
            ulong generation,
            TimelineRuntimePreparationResult preparation)
        {
            Handle = handle;
            Generation = generation;
            RequestId = preparation.RequestId;
            ExecutionIdentity = preparation.ExecutionIdentity;
            NumericTarget = preparation.NumericTarget;
            Content = preparation.Content;
            PreparedDependencies = preparation.PreparedDependencies;
            PreparedBindings = preparation.PreparedBindings;
            State = TimelineRuntimePlaybackState.Prepared;
        }

        public TimelineRuntimePlaybackHandle Handle { get; }
        public ulong Generation { get; }
        public string RequestId { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public TimelineRuntimeNumericTarget NumericTarget { get; }
        public TimelineContentUnit Content { get; }
        public string ContentRevision => Content.ContentHash;
        public TimelineRuntimePreparedDependencies PreparedDependencies { get; }
        public TimelinePreparedBindings PreparedBindings { get; }
        public TimelineRuntimePlaybackState State { get; private set; }

        public void Dispose()
        {
            if (State != TimelineRuntimePlaybackState.Disposed)
                State = TimelineRuntimePlaybackState.Disposed;
        }
    }

    public sealed class TimelineRuntimePrepareRequest
    {
        public TimelineRuntimePrepareRequest(
            string requestId,
            TimelineData timeline,
            TimelineContractCatalog contractCatalog,
            TimelineExecutionIdentity executionIdentity,
            TimelineRuntimeNumericTarget numericTarget,
            IEnumerable<TimelineCallBinding> callBindings,
            ITimelineDomainBindingResolver domainResolver,
            ITimelineRuntimeDependencyResolver dependencyResolver)
        {
            RequestId = string.IsNullOrWhiteSpace(requestId)
                ? throw new ArgumentException("Timeline prepare request identity is required.", nameof(requestId))
                : requestId.Trim();
            Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            ContractCatalog = contractCatalog ?? throw new ArgumentNullException(nameof(contractCatalog));
            if (!executionIdentity.IsValid)
                throw new ArgumentException("Timeline execution identity is invalid.", nameof(executionIdentity));
            ExecutionIdentity = executionIdentity;
            if (!Enum.IsDefined(typeof(TimelineRuntimeNumericTarget), numericTarget))
                throw new ArgumentOutOfRangeException(nameof(numericTarget));
            NumericTarget = numericTarget;
            CallBindings = new List<TimelineCallBinding>(callBindings ?? Array.Empty<TimelineCallBinding>()).AsReadOnly();
            DomainResolver = domainResolver ?? throw new ArgumentNullException(nameof(domainResolver));
            DependencyResolver = dependencyResolver ?? throw new ArgumentNullException(nameof(dependencyResolver));
        }

        public string RequestId { get; }
        public TimelineData Timeline { get; }
        public TimelineContractCatalog ContractCatalog { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public TimelineRuntimeNumericTarget NumericTarget { get; }
        public IReadOnlyList<TimelineCallBinding> CallBindings { get; }
        public ITimelineDomainBindingResolver DomainResolver { get; }
        public ITimelineRuntimeDependencyResolver DependencyResolver { get; }
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
            TimelineRuntimeNumericTarget numericTarget,
            TimelineContentUnit content,
            TimelineBindingPlan bindingPlan,
            TimelineCallInput callInput,
            TimelinePreparedBindings preparedBindings,
            TimelineRuntimePreparedDependencies preparedDependencies,
            IReadOnlyList<string> errors)
        {
            Status = status;
            RequestId = requestId ?? string.Empty;
            ExecutionIdentity = executionIdentity;
            NumericTarget = numericTarget;
            Content = content;
            BindingPlan = bindingPlan;
            CallInput = callInput;
            PreparedBindings = preparedBindings;
            PreparedDependencies = preparedDependencies;
            Errors = errors ?? Array.Empty<string>();
        }

        public TimelineRuntimePreparationStatus Status { get; }
        public string RequestId { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public TimelineRuntimeNumericTarget NumericTarget { get; }
        public TimelineContentUnit Content { get; }
        public string ContentRevision => Content?.ContentHash ?? string.Empty;
        public TimelineBindingPlan BindingPlan { get; }
        public TimelineCallInput CallInput { get; }
        public TimelinePreparedBindings PreparedBindings { get; }
        public TimelineRuntimePreparedDependencies PreparedDependencies { get; }
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
                TimelineRuntimeNumericTarget.Float32,
                null,
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
            TimelinePreparedBindings preparedBindings,
            TimelineRuntimePreparedDependencies preparedDependencies)
        {
            return new TimelineRuntimePreparationResult(
                TimelineRuntimePreparationStatus.Ready,
                request.RequestId,
                request.ExecutionIdentity,
                request.NumericTarget,
                content,
                bindingPlan,
                callInput,
                preparedBindings,
                preparedDependencies,
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
                var errors = new List<string>();
                var dependencyHandles = new List<TimelineRuntimeDependencyHandle>(discovery.Content.Dependencies.Count);
                for (int index = 0; index < discovery.Content.Dependencies.Count; index++)
                {
                    TimelineContentDependency dependency = discovery.Content.Dependencies[index];
                    if (!request.DependencyResolver.TryResolve(
                            dependency,
                            request.NumericTarget,
                            out TimelineRuntimeDependencyHandle handle,
                            out string error) || !handle.IsValid)
                    {
                        errors.Add($"timeline_dependency_unresolved:{dependency.Identity}:{error ?? "dependency is unresolved"}");
                        continue;
                    }
                    dependencyHandles.Add(handle);
                }
                if (errors.Count != 0 || dependencyHandles.Count != discovery.Content.Dependencies.Count)
                    return TimelineRuntimePreparationResult.Failed(
                        request.RequestId,
                        request.ExecutionIdentity,
                        errors);

                TimelineBindingPlan bindingPlan = new TimelineBindingPlan(discovery.Content);
                TimelineCallInput callInput = new TimelineCallInput(bindingPlan, request.CallBindings);
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
                    preparedBindings,
                    new TimelineRuntimePreparedDependencies(discovery.Content.Dependencies, dependencyHandles));
            }
            catch (Exception exception)
            {
                return TimelineRuntimePreparationResult.Failed(
                    request.RequestId,
                    request.ExecutionIdentity,
                    new[] { exception.Message });
            }
        }

        public static TimelineRuntimePlayback CreatePlayback(
            TimelineRuntimePreparationResult preparation,
            ulong generation)
        {
            if (preparation == null)
                throw new ArgumentNullException(nameof(preparation));
            if (!preparation.IsReady)
                throw new InvalidOperationException("Timeline playback cannot be created from a failed preparation.");
            if (generation == 0)
                throw new ArgumentOutOfRangeException(nameof(generation));
            return new TimelineRuntimePlayback(
                new TimelineRuntimePlaybackHandle(preparation.ExecutionIdentity.InstanceId),
                generation,
                preparation);
        }
    }
}
