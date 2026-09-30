using System;
using ThirdPersonSimulation.Fixed;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonSimulation;
using UnityEngine;

namespace BTSMTL.Timeline.Runtime
{
    public readonly struct TimelineRuntimePrepareRequest
    {
        public TimelineRuntimePrepareRequest(
            string requestId,
            TimelineRuntimePreparedContent content,
            TimelineExecutionIdentity executionIdentity,
            TimelinePlaybackMode playbackMode)
        {
            RequestId = string.IsNullOrWhiteSpace(requestId)
                ? throw new ArgumentException("Timeline prepare request identity is required.", nameof(requestId))
                : requestId.Trim();
            Content = content;
            if (!executionIdentity.IsValid)
                throw new ArgumentException("Timeline execution identity is invalid.", nameof(executionIdentity));
            ExecutionIdentity = executionIdentity;
            if (playbackMode != TimelinePlaybackMode.Once && playbackMode != TimelinePlaybackMode.Loop &&
                playbackMode != TimelinePlaybackMode.HoldLastFrame)
                throw new ArgumentOutOfRangeException(nameof(playbackMode));
            PlaybackMode = playbackMode;
        }

        public string RequestId { get; }
        public TimelineRuntimePreparedContent Content { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public TimelinePlaybackMode PlaybackMode { get; }
    }

    public enum TimelineRuntimePreparationStatus : byte
    {
        Failed = 0,
        Ready = 1
    }

    public sealed class TimelineRuntimePreparedContent
    {
        internal TimelineRuntimePreparedContent(
            TimelineRuntimePreparationStatus status,
            TimelineRuntimeNumericTarget numericTarget,
            TimelineData sourceTimeline,
            TimelineContentUnit content,
            TimelineBindingPlan bindingPlan,
            TimelineCallInput callInput,
            TimelinePreparedBindings preparedBindings,
            TimelineRuntimePreparedDependencies preparedDependencies,
            IReadOnlyList<string> errors)
        {
            Status = status;
            NumericTarget = numericTarget;
            SourceTimeline = sourceTimeline;
            MotionSampling = status == TimelineRuntimePreparationStatus.Ready
                ? new TimelineRuntimeMotionSampling(sourceTimeline, numericTarget)
                : null;
            Content = content;
            BindingPlan = bindingPlan;
            CallInput = callInput;
            PreparedBindings = preparedBindings;
            PreparedDependencies = preparedDependencies;
            Errors = errors ?? Array.Empty<string>();
        }

        public TimelineRuntimePreparationStatus Status { get; }
        public TimelineRuntimeNumericTarget NumericTarget { get; }
        public TimelineData SourceTimeline { get; }
        internal TimelineRuntimeMotionSampling MotionSampling { get; }
        public TimelineContentUnit Content { get; }
        public string ContentRevision => Content?.ContentHash ?? string.Empty;
        public TimelineBindingPlan BindingPlan { get; }
        public TimelineCallInput CallInput { get; }
        public TimelinePreparedBindings PreparedBindings { get; }
        public TimelineRuntimePreparedDependencies PreparedDependencies { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool IsReady => Status == TimelineRuntimePreparationStatus.Ready;

        internal static TimelineRuntimePreparedContent Failed(
            TimelineRuntimeNumericTarget numericTarget,
            IEnumerable<string> errors)
        {
            return new TimelineRuntimePreparedContent(
                TimelineRuntimePreparationStatus.Failed,
                numericTarget,
                null,
                null,
                null,
                null,
                null,
                null,
                new List<string>(errors ?? Array.Empty<string>()).AsReadOnly());
        }

        internal static TimelineRuntimePreparedContent Ready(
            TimelineRuntimeNumericTarget numericTarget,
            TimelineData sourceTimeline,
            TimelineContentUnit content,
            TimelineBindingPlan bindingPlan,
            TimelineCallInput callInput,
            TimelinePreparedBindings preparedBindings,
            TimelineRuntimePreparedDependencies preparedDependencies)
        {
            return new TimelineRuntimePreparedContent(
                TimelineRuntimePreparationStatus.Ready,
                numericTarget,
                sourceTimeline,
                content,
                bindingPlan,
                callInput,
                preparedBindings,
                preparedDependencies,
                Array.Empty<string>());
        }
    }

    public readonly struct TimelineRuntimePreparationResult
    {
        readonly TimelineRuntimePreparedContent m_Content;

        internal TimelineRuntimePreparationResult(TimelineRuntimePrepareRequest request)
        {
            m_Content = request.Content;
            Status = request.Content.Status;
            RequestId = request.RequestId;
            ExecutionIdentity = request.ExecutionIdentity;
            PlaybackMode = request.PlaybackMode;
        }

        public TimelineRuntimePreparationStatus Status { get; }
        public string RequestId { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public TimelinePlaybackMode PlaybackMode { get; }
        public TimelineRuntimeNumericTarget NumericTarget => m_Content.NumericTarget;
        public TimelineData SourceTimeline => m_Content.SourceTimeline;
        internal TimelineRuntimeMotionSampling MotionSampling => m_Content.MotionSampling;
        public TimelineContentUnit Content => m_Content.Content;
        public string ContentRevision => m_Content.ContentRevision;
        public TimelineBindingPlan BindingPlan => m_Content.BindingPlan;
        public TimelineCallInput CallInput => m_Content.CallInput;
        public TimelinePreparedBindings PreparedBindings => m_Content.PreparedBindings;
        public TimelineRuntimePreparedDependencies PreparedDependencies => m_Content.PreparedDependencies;
        public IReadOnlyList<string> Errors => m_Content.Errors;
        public bool IsReady => Status == TimelineRuntimePreparationStatus.Ready;
    }

    public static class TimelineRuntimePreparation
    {
        public static TimelineRuntimePreparedContent PrepareContent(
            TimelineData sourceTimeline,
            TimelineContentUnit content,
            TimelineRuntimeNumericTarget numericTarget,
            IReadOnlyList<TimelineCallBinding> callBindings,
            ITimelineDomainBindingResolver domainResolver,
            ITimelineRuntimeDependencyResolver dependencyResolver)
        {
            try
            {
                var errors = new List<string>();
                TimelineRuntimeEvaluator.ValidateTreeContracts(sourceTimeline, content, errors);
                if (errors.Count != 0)
                    return TimelineRuntimePreparedContent.Failed(
                        numericTarget,
                        errors);
                var dependencyHandles = new List<TimelineRuntimeDependencyHandle>(content.Dependencies.Count);
                for (int index = 0; index < content.Dependencies.Count; index++)
                {
                    TimelineContentDependency dependency = content.Dependencies[index];
                    if (!dependencyResolver.TryResolve(
                            dependency,
                            numericTarget,
                            out TimelineRuntimeDependencyHandle handle,
                            out string error) || !handle.IsValid)
                    {
                        errors.Add($"timeline_dependency_unresolved:{dependency.Identity}:{error ?? "dependency is unresolved"}");
                        continue;
                    }
                    dependencyHandles.Add(handle);
                }
                if (errors.Count != 0)
                    return TimelineRuntimePreparedContent.Failed(
                        numericTarget,
                        errors);

                TimelineBindingPlan bindingPlan = new TimelineBindingPlan(content);
                TimelineCallInput callInput = new TimelineCallInput(bindingPlan, callBindings);
                TimelinePreparedBindings preparedBindings = TimelineBindingPreparation.Prepare(
                    bindingPlan,
                    callInput,
                    domainResolver,
                    errors);
                if (preparedBindings == null || errors.Count != 0)
                    return TimelineRuntimePreparedContent.Failed(
                        numericTarget,
                        errors);
                return TimelineRuntimePreparedContent.Ready(
                    numericTarget,
                    sourceTimeline,
                    content,
                    bindingPlan,
                    callInput,
                    preparedBindings,
                    new TimelineRuntimePreparedDependencies(content.Dependencies, dependencyHandles));
            }
            catch (Exception exception)
            {
                return TimelineRuntimePreparedContent.Failed(
                    numericTarget,
                    new[] { exception.Message });
            }
        }

        internal static TimelineRuntimePlayback CreatePlayback(
            TimelineRuntimePreparationResult preparation,
            TimelineRuntimePlaybackHandle handle,
            ulong generation,
            int tickRate,
            TimelineRuntimeEvaluationStoragePool evaluationStoragePool)
        {
            if (!preparation.IsReady)
                throw new InvalidOperationException("Timeline playback cannot be created from a failed preparation.");
            if (!handle.IsValid)
                throw new ArgumentOutOfRangeException(nameof(handle));
            if (generation == 0)
                throw new ArgumentOutOfRangeException(nameof(generation));
            return new TimelineRuntimePlayback(
                handle,
                generation,
                preparation,
                tickRate,
                evaluationStoragePool);
        }
    }
}
