using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Runtime;
using BTSMTL.Timeline.Tree;
using ThirdPersonSimulation;
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

    [DisallowMultipleComponent]
    public sealed class CharacterTimelineHost : MonoBehaviour, ITimelinePlaybackService
    {
        TimelineRuntimeCompositionHost m_Host;
        TimelineToActionCommandBridge m_Bridge;
        ActionPlaybackCommandInbox m_Inbox;
        bool m_Initialized;

        internal TimelineRuntimeCompositionHost Host => m_Host;

        internal void Initialize(ActionPlaybackCommandInbox inbox)
        {
            if (m_Initialized)
                return;
            m_Inbox = inbox ?? throw new ArgumentNullException(nameof(inbox));

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

            var publisher = new CharacterPoseActionCommandPublisher(
                m_Inbox);
            m_Bridge = new TimelineToActionCommandBridge(m_Host, publisher, default);

            m_Initialized = true;
        }

        internal void SetActorId(ThirdPersonSimulation.ActorId actorId)
        {
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
            var executionIdentity = new TimelineExecutionIdentity(sourceId, sourceName, (ulong)sourceId.GetHashCode());
            var preparation = m_Host.Prepare(
                $"{sourceId}/{sourceName}",
                timeline,
                executionIdentity,
                playbackMode,
                Array.Empty<TimelineCallBinding>());
            if (preparation == null)
            {
                handle = TimelinePlaybackHandle.Invalid;
                return false;
            }
            handle = preparation.Handle;
            return preparation.Handle.IsValid;
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

        void OnDestroy()
        {
            m_Bridge?.Dispose();
            m_Host?.Dispose();
        }
    }
}
