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

    internal sealed class CharacterTimelineMissingTreeClipService : ITimelineRuntimeTreeClipService
    {
        public bool Consume(TimelineRuntimeTreeClipRequest request, TimelineRuntimeStepContext context) =>
            throw new InvalidOperationException($"Timeline TreeClip '{request.ClipAuthoringId}' requires a composed TreeClip service.");
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


    public sealed class CharacterTimelinePendingAdvance : IAbilityTimelinePending
    {
        internal CharacterTimelinePendingAdvance(
            TimelinePlaybackHandle handle,
            int runtimeHandle,
            TimelineRuntimeAdvanceResult result,
            AbilityTimelineRuntimeStatus status)
        {
            Handle = handle;
            RuntimeHandle = runtimeHandle;
            Result = result;
            Status = status;
        }

        public TimelinePlaybackHandle Handle { get; }
        public int RuntimeHandle { get; }
        public AbilityTimelineRuntimeStatus Status { get; }
        internal TimelineRuntimeAdvanceResult Result { get; }
    }
    public sealed class CharacterTimelinePendingStop : IAbilityTimelineStopPending
    {
        internal CharacterTimelinePendingStop(
            TimelinePlaybackHandle handle,
            int runtimeHandle,
            TimelineRuntimeStopRequest request,
            AbilityTimelineRuntimeStatus status)
        {
            Handle = handle;
            RuntimeHandle = runtimeHandle;
            Request = request;
            Status = status;
        }

        public TimelinePlaybackHandle Handle { get; }
        public int RuntimeHandle { get; }
        internal TimelineRuntimeStopRequest Request { get; }
        public AbilityTimelineRuntimeStatus Status { get; }
    }
    [DisallowMultipleComponent]
    public sealed class CharacterTimelineHost : MonoBehaviour, ITimelinePlaybackService
    {
        struct ActivePlayback
        {
            internal TimelinePlaybackHandle Handle;
            internal TimelineData Timeline;
            internal string SourceName;
            internal bool CoreDriven;
        }

        TimelineRuntimeCompositionHost m_Host;
        readonly List<ActivePlayback> m_ActivePlaybacks = new List<ActivePlayback>();
        readonly List<ActivePlayback> m_PlaybackScan = new List<ActivePlayback>();
        readonly Dictionary<string, TimelineData> m_TimelineContent = new Dictionary<string, TimelineData>(StringComparer.Ordinal);
        readonly Dictionary<ulong, CharacterTimelinePendingAdvance> m_PendingAdvances =
            new Dictionary<ulong, CharacterTimelinePendingAdvance>();
        readonly Dictionary<ulong, CharacterTimelinePendingStop> m_PendingStops =
            new Dictionary<ulong, CharacterTimelinePendingStop>();
        ulong m_TickCounter;
        TimelinePlaybackHandle m_PreviewHandle;
        bool m_Initialized;

        internal TimelineRuntimeCompositionHost Host => m_Host;
        public bool IsInitialized => m_Initialized && m_Host != null;

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
            var treeClipService = new CharacterTimelineMissingTreeClipService();

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
                SourceName = sourceName ?? string.Empty,
                CoreDriven = false
            });
            return true;
        }

        public void SetTimelineContent(IReadOnlyList<TimelineAsset> timelines)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline content requires an initialized CharacterTimelineHost.");
            if (timelines == null)
                throw new ArgumentNullException(nameof(timelines));
            m_TimelineContent.Clear();
            for (int i = 0; i < timelines.Count; i++)
            {
                TimelineAsset asset = timelines[i];
                if (!asset || asset.Data == null)
                    throw new InvalidOperationException("Timeline content list contains an invalid Timeline asset.");
                if (!m_TimelineContent.TryAdd(asset.Data.AuthoringId, asset.Data))
                    throw new InvalidOperationException($"Timeline content identity '{asset.Data.AuthoringId}' is duplicated.");
            }
        }

        public bool RequestAbilityTimelinePlayback(
            string timelineId,
            TimelinePlaybackActionContext actionContext,
            bool loop,
            out TimelinePlaybackHandle handle)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Ability Timeline requires an initialized CharacterTimelineHost.");
            if (!actionContext.IsValid)
                throw new ArgumentException("Ability Timeline Action context is invalid.", nameof(actionContext));
            if (!m_TimelineContent.TryGetValue(timelineId, out TimelineData timeline))
                throw new KeyNotFoundException($"Ability Timeline content '{timelineId}' is not installed.");
            bool requested = RequestTimelinePlayback(
                timeline,
                actionContext.ActionId,
                timeline.Name,
                actionContext,
                loop ? TimelinePlaybackMode.Loop : TimelinePlaybackMode.Once,
                default,
                null,
                out handle);
            if (requested)
            {
                for (int i = 0; i < m_ActivePlaybacks.Count; i++)
                {
                    if (m_ActivePlaybacks[i].Handle.Value == handle.Value)
                    {
                        ActivePlayback updated = m_ActivePlaybacks[i];
                        updated.CoreDriven = true;
                        m_ActivePlaybacks[i] = updated;
                    }
                }
            }
            return requested;
        }

        public CharacterTimelinePendingAdvance AdvanceTimelinePlayback(
            TimelinePlaybackHandle handle,
            ulong logicTick,
            int deltaFrames)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline advancement requires an initialized CharacterTimelineHost.");
            TimelinePlaybackStatus status = m_Host.Service.GetTimelinePlaybackStatus(handle);
            if (status != TimelinePlaybackStatus.Requested && status != TimelinePlaybackStatus.Running)
                return new CharacterTimelinePendingAdvance(handle, 0, null, MapTerminalStatus(status));
            TimelineRuntimeAdvanceResult advance = m_Host.Advance(new TimelineRuntimePlaybackHandle(handle.Value), logicTick, deltaFrames);
            var pending = new CharacterTimelinePendingAdvance(handle, (int)handle.Value, advance, AbilityTimelineRuntimeStatus.Running);
            m_PendingAdvances[handle.Value] = pending;
            return pending;
        }

        public void CommitTimelinePlayback(CharacterTimelinePendingAdvance pending)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline commit requires an initialized CharacterTimelineHost.");
            if (pending == null || !m_PendingAdvances.Remove(pending.Handle.Value))
                return;
            if (pending.Result != null && !m_Host.CommitAdvance(new TimelineRuntimePlaybackHandle(pending.Handle.Value), pending.Result))
                throw new InvalidOperationException($"Timeline advance '{pending.Handle.Value}' could not be committed.");
        }

        public void DiscardTimelinePlayback(CharacterTimelinePendingAdvance pending)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline discard requires an initialized CharacterTimelineHost.");
            if (pending == null || !m_PendingAdvances.Remove(pending.Handle.Value))
                return;
            if (pending.Result != null && !m_Host.DiscardAdvance(new TimelineRuntimePlaybackHandle(pending.Handle.Value), pending.Result))
                throw new InvalidOperationException($"Timeline advance '{pending.Handle.Value}' could not be discarded.");
        }


        public bool RequestPreviewTimelinePlayback(
            TimelineData timeline,
            string sourceName,
            TimelinePlaybackMode playbackMode,
            out TimelinePlaybackHandle handle)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline preview requires an initialized CharacterTimelineHost.");
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            if (m_PreviewHandle.IsValid)
                throw new InvalidOperationException("Timeline preview already has an active playback.");
            bool requested = RequestTimelinePlayback(
                timeline,
                "sceneplay.preview",
                sourceName ?? timeline.Name,
                default,
                playbackMode,
                default,
                null,
                out handle);
            if (requested)
                m_PreviewHandle = handle;
            return requested;
        }

        public bool CancelPreviewTimelinePlayback()
        {
            if (!IsInitialized || !m_PreviewHandle.IsValid)
                return false;
            m_Host.Service.CancelTimelinePlayback(
                m_PreviewHandle,
                new TimelinePlaybackStopContext(TimelinePlaybackStopCause.SelfAbort, 0));
            m_PreviewHandle = TimelinePlaybackHandle.Invalid;
            return true;
        }

        public bool PreviewIsActive
        {
            get
            {
                if (!m_PreviewHandle.IsValid || m_Host == null)
                    return false;
                TimelinePlaybackStatus status = m_Host.Service.GetTimelinePlaybackStatus(m_PreviewHandle);
                return status == TimelinePlaybackStatus.Requested || status == TimelinePlaybackStatus.Running;
            }
        }

        public TimelinePlaybackStatus GetTimelinePlaybackStatus(TimelinePlaybackHandle handle)
        {
            if (!m_Initialized || m_Host == null || !handle.IsValid)
                return TimelinePlaybackStatus.None;
            return m_Host.Service.GetTimelinePlaybackStatus(handle);
        }

        public CharacterTimelinePendingStop RequestStopTimelinePlayback(
            TimelinePlaybackHandle handle,
            TimelinePlaybackStopContext stopContext)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline stop requires an initialized CharacterTimelineHost.");
            TimelinePlaybackStatus status = m_Host.Service.GetTimelinePlaybackStatus(handle);
            if (status != TimelinePlaybackStatus.Requested && status != TimelinePlaybackStatus.Running)
                return new CharacterTimelinePendingStop(handle, 0, default, MapTerminalStatus(status));
            if (!m_Host.Service.RequestStopTimelinePlayback(
                    handle,
                    stopContext,
                    out TimelineRuntimeStopRequest request))
                throw new InvalidOperationException($"Timeline stop '{handle.Value}' was rejected.");
            var pending = new CharacterTimelinePendingStop(handle, (int)handle.Value, request, AbilityTimelineRuntimeStatus.Running);
            m_PendingStops[handle.Value] = pending;
            return pending;
        }

        public void CommitStopTimelinePlayback(CharacterTimelinePendingStop pending)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline stop commit requires an initialized CharacterTimelineHost.");
            if (pending == null || !m_PendingStops.Remove(pending.Handle.Value))
                return;
            if (pending.Request.Playback != null && !m_Host.Service.CommitStopTimelinePlayback(pending.Handle, pending.Request))
                throw new InvalidOperationException($"Timeline stop '{pending.Handle.Value}' could not be committed.");
        }

        public void DiscardStopTimelinePlayback(CharacterTimelinePendingStop pending)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline stop discard requires an initialized CharacterTimelineHost.");
            if (pending == null || !m_PendingStops.Remove(pending.Handle.Value))
                return;
            if (pending.Request.Playback != null && !m_Host.Service.DiscardStopTimelinePlayback(pending.Handle, pending.Request))
                throw new InvalidOperationException($"Timeline stop '{pending.Handle.Value}' could not be discarded.");
        }
        public void CancelTimelinePlayback(
            TimelinePlaybackHandle handle,
            TimelinePlaybackStopContext stopContext)
        {
            if (!m_Initialized || m_Host == null || !handle.IsValid)
                return;
            if (m_PendingStops.TryGetValue(handle.Value, out CharacterTimelinePendingStop pendingStop))
                CommitStopTimelinePlayback(pendingStop);
            if (m_PendingAdvances.TryGetValue(handle.Value, out CharacterTimelinePendingAdvance pending))
                DiscardTimelinePlayback(pending);
            m_Host.Service.CancelTimelinePlayback(handle, stopContext);
        }

        static AbilityTimelineRuntimeStatus MapTerminalStatus(TimelinePlaybackStatus status)
        {
            return status switch
            {
                TimelinePlaybackStatus.Succeeded => AbilityTimelineRuntimeStatus.Succeeded,
                TimelinePlaybackStatus.Failed => AbilityTimelineRuntimeStatus.Failed,
                TimelinePlaybackStatus.Cancelled => AbilityTimelineRuntimeStatus.Cancelled,
                _ => AbilityTimelineRuntimeStatus.Running
            };
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
                if (active.CoreDriven)
                    continue;
                m_Host.Service.Step(active.Handle, m_TickCounter, deltaFrames);
            }
        }

        void OnDestroy()
        {
            m_Host?.Dispose();
        }
    }
}
