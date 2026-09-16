using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Runtime;
using ThirdPersonSimulation;
using TreeDesigner;
using TimelinePlaybackStatus = BTSMTL.Timeline.TimelinePlaybackStatus;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal sealed class CharacterTimelineDomainBindingResolver : ITimelineDomainBindingResolver
    {
        readonly HashSet<string> m_SupportedTargets;
        int m_NextHandle = 1;

        internal CharacterTimelineDomainBindingResolver(IEnumerable<string> supportedTargets)
        {
            m_SupportedTargets = new HashSet<string>(supportedTargets ?? Array.Empty<string>(), StringComparer.Ordinal);
        }

        public bool TryResolve(
            TimelineBindingDeclaration declaration,
            TimelineBindingValue callValue,
            out TimelineBindingHandle handle,
            out string error)
        {
            handle = default;
            error = string.Empty;
            if (callValue.ValueKind == TimelineBindingValueKind.Target &&
                !string.IsNullOrEmpty(callValue.TargetIdentity) &&
                !m_SupportedTargets.Contains(callValue.TargetIdentity))
            {
                error = $"Timeline target '{callValue.TargetIdentity}' is not supported by this character.";
                return false;
            }
            handle = new TimelineBindingHandle(m_NextHandle++);
            return true;
        }
    }

    internal sealed class CharacterTimelineDependencyResolver : ITimelineRuntimeDependencyResolver
    {
        int m_NextHandle = 1;

        public bool TryResolve(
            TimelineContentDependency dependency,
            TimelineRuntimeNumericTarget numericTarget,
            out TimelineRuntimeDependencyHandle handle,
            out string error)
        {
            handle = new TimelineRuntimeDependencyHandle(m_NextHandle++);
            error = string.Empty;
            return true;
        }
    }

    internal sealed class CharacterTimelineNoOpTreeClipService : ITimelineRuntimeTreeClipService
    {
        public bool Consume(TimelineRuntimeTreeClipRequest request, TimelineRuntimeStepContext context) => false;
        public void Discard(TimelineRuntimeTreeClipRequest request, TimelineRuntimeStepContext context) { }
        public void Commit(TimelineRuntimeStepContext context) { }
        public void DiscardStep(TimelineRuntimeStepContext context) { }
        public bool ConsumeStop(TimelineRuntimeStopRequest request) => false;
        public void CommitStop(TimelineRuntimeStopRequest request) { }
        public void DiscardStop(TimelineRuntimeStopRequest request) { }
    }

    public readonly struct CharacterTimelinePlaybackObservation
    {
        internal CharacterTimelinePlaybackObservation(
            TimelinePlaybackHandle handle,
            TimelinePlaybackStatus status,
            TimelineData timeline,
            string sourceName,
            float clipTime,
            float normalizedTime,
            float weight,
            string clipAuthoringId)
        {
            Handle = handle;
            Status = status;
            Timeline = timeline;
            SourceName = sourceName ?? string.Empty;
            ClipTime = clipTime;
            NormalizedTime = normalizedTime;
            Weight = weight;
            ClipAuthoringId = clipAuthoringId ?? string.Empty;
        }

        public TimelinePlaybackHandle Handle { get; }
        public TimelinePlaybackStatus Status { get; }
        public TimelineData Timeline { get; }
        public string SourceName { get; }
        public float ClipTime { get; }
        public float NormalizedTime { get; }
        public float Weight { get; }
        public string ClipAuthoringId { get; }
    }

    [DisallowMultipleComponent]
    public sealed class CharacterTimelineHost : MonoBehaviour, ITimelinePlaybackService
    {
        struct ActivePlayback
        {
            internal TimelinePlaybackHandle Handle;
            internal TimelineData Timeline;
            internal string SourceName;
        }

        TimelineRuntimeCompositionHost m_Host;
        readonly List<ActivePlayback> m_ActivePlaybacks = new List<ActivePlayback>();
        readonly List<ActivePlayback> m_PlaybackScan = new List<ActivePlayback>();
        ulong m_TickCounter;
        bool m_Initialized;

        internal TimelineRuntimeCompositionHost Host => m_Host;

        internal void Initialize()
        {
            if (m_Initialized)
                return;

            var contractCatalog = new TimelineContractCatalog(Array.Empty<ITimelineContractProvider>());
            var callBindingSource = new TimelineRuntimeCallBindingSource(
                name, "character-timeline", default);
            var domainResolver = new CharacterTimelineDomainBindingResolver(
                new[] { "self", "camera", "main" });
            var dependencyResolver = new CharacterTimelineDependencyResolver();
            var treeClipService = new CharacterTimelineNoOpTreeClipService();

            m_Host = new TimelineRuntimeCompositionHost(
                contractCatalog,
                TimelineRuntimeNumericTarget.Float32,
                callBindingSource,
                domainResolver,
                dependencyResolver,
                Array.Empty<ITimelineRuntimeEvaluationSink>(),
                treeClipService);
            m_Initialized = true;
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
            if (!m_Initialized || m_Host == null)
            {
                handle = TimelinePlaybackHandle.Invalid;
                return false;
            }
            if (!m_Host.RequestTimelinePlayback(
                timeline, sourceId, sourceName, actionContext, playbackMode,
                sourceActivation, sourceRuntimeGraph, out handle))
                return false;
            m_ActivePlaybacks.Add(new ActivePlayback
            {
                Handle = handle,
                Timeline = timeline,
                SourceName = sourceName ?? string.Empty
            });
            return true;
        }

        public TimelinePlaybackStatus GetTimelinePlaybackStatus(TimelinePlaybackHandle handle)
        {
            if (!m_Initialized || m_Host == null || !handle.IsValid)
                return TimelinePlaybackStatus.None;
            return m_Host.Service.GetTimelinePlaybackStatus(handle);
        }

        public void CancelTimelinePlayback(TimelinePlaybackHandle handle, TimelinePlaybackStopContext stopContext)
        {
            if (!m_Initialized || m_Host == null || !handle.IsValid)
                return;
            m_Host.Service.CancelTimelinePlayback(handle, stopContext);
        }

        public void CollectActivePlaybacks(List<CharacterTimelinePlaybackObservation> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));
            results.Clear();
            if (!m_Initialized || m_Host == null)
                return;
            for (int i = 0; i < m_ActivePlaybacks.Count; i++)
            {
                ActivePlayback active = m_ActivePlaybacks[i];
                ReadLatestSample(
                    active.Handle,
                    out float clipTime,
                    out float normalizedTime,
                    out float weight,
                    out string clipAuthoringId);
                results.Add(new CharacterTimelinePlaybackObservation(
                    active.Handle,
                    m_Host.Service.GetTimelinePlaybackStatus(active.Handle),
                    active.Timeline,
                    active.SourceName,
                    clipTime,
                    normalizedTime,
                    weight,
                    clipAuthoringId));
            }
        }

        void ReadLatestSample(
            TimelinePlaybackHandle handle,
            out float clipTime,
            out float normalizedTime,
            out float weight,
            out string clipAuthoringId)
        {
            clipTime = 0f;
            normalizedTime = 0f;
            weight = 0f;
            clipAuthoringId = string.Empty;
            if (!m_Host.TryGetCommittedEvaluationResult(handle, out TimelineRuntimeEvaluationResult result) ||
                result == null || result.AnimationContributions.Count == 0)
                return;
            for (int i = 0; i < result.AnimationContributions.Count; i++)
            {
                TimelineAnimationContribution contribution = result.AnimationContributions[i];
                if (contribution.Weight <= weight)
                    continue;
                weight = contribution.Weight;
                clipTime = contribution.ClipTime;
                normalizedTime = contribution.NormalizedTime;
                clipAuthoringId = contribution.ClipAuthoringId;
            }
        }

        void Update()
        {
            if (!m_Initialized || m_Host == null || m_ActivePlaybacks.Count == 0)
                return;
            m_TickCounter++;
            int deltaFrames = Mathf.Max(1, Mathf.RoundToInt(Time.deltaTime * 60f));
            m_PlaybackScan.Clear();
            m_PlaybackScan.AddRange(m_ActivePlaybacks);
            for (int i = 0; i < m_PlaybackScan.Count; i++)
            {
                ActivePlayback active = m_PlaybackScan[i];
                TimelinePlaybackStatus status = m_Host.Service.GetTimelinePlaybackStatus(active.Handle);
                if (status != TimelinePlaybackStatus.Requested && status != TimelinePlaybackStatus.Running)
                {
                    m_ActivePlaybacks.Remove(active);
                    continue;
                }
                m_Host.Service.Step(active.Handle, m_TickCounter, deltaFrames);
            }
        }

        void OnDestroy()
        {
            m_Host?.Dispose();
        }
    }
}
