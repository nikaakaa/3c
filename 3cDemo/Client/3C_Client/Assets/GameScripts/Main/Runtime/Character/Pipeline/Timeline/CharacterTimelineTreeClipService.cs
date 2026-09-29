using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Runtime;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonGameplay.Tick;
using TimelinePlaybackStatus = BTSMTL.Timeline.TimelinePlaybackStatus;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal sealed class CharacterTimelineTreeClipService : ITimelineRuntimeTreeClipService
    {
        readonly CharacterTimelineHost m_Host;
        readonly Dictionary<ulong, List<AbilityTimelineTreeClipState>> m_ActiveClips = new();

        internal CharacterTimelineTreeClipService(CharacterTimelineHost host)
        {
            m_Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        internal IReadOnlyList<AbilityTimelineTreeClipState> Capture(ulong handle) =>
            m_ActiveClips.TryGetValue(handle, out List<AbilityTimelineTreeClipState> clips)
                ? clips : Array.Empty<AbilityTimelineTreeClipState>();

        internal void Restore(ulong handle, TimelineSnapshotItems<AbilityTimelineTreeClipState> restored)
        {
            if (restored.Count == 0)
            {
                m_ActiveClips.Remove(handle);
                return;
            }
            if (!m_ActiveClips.TryGetValue(handle, out List<AbilityTimelineTreeClipState> clips))
            {
                clips = new List<AbilityTimelineTreeClipState>(restored.Count);
                m_ActiveClips.Add(handle, clips);
            }
            clips.Clear();
            for (int index = 0; index < restored.Count; index++)
                clips.Add(restored[index]);
        }

        public bool RequestTreeClipExit(TimelinePlaybackHandle handle, string clipAuthoringId)
        {
            if (!m_Host.IsInitialized)
                return false;
            return m_Host.Host.Service.RequestTreeClipExit(new TimelineRuntimePlaybackHandle(handle.Value), clipAuthoringId);
        }

        public bool Consume(TimelineRuntimeTreeClipRequest request, TimelineRuntimeStepContext context)
        {
            if (!m_Host.IsAbilityRuntimePlayback(context.Playback.Handle))
                return true;
            IAbilityTreeClipInvoker invoker = m_Host.m_ActiveTreeClipInvoker
                ?? throw new InvalidOperationException($"Timeline TreeClip '{request.ClipAuthoringId}' requires an active Ability invoker.");
            if (request.EventKind == TimelineRuntimeTreeClipEventKind.Update && !IsActive(request, context))
                return true;
            var invocation = new AbilityTreeClipInvocation(
                request.ClipAuthoringId,
                request.TreeGraphId,
                request.TreeGraphRevision,
                request.ClipAuthoringId,
                request.EventKind switch
                {
                    TimelineRuntimeTreeClipEventKind.Enter => AbilityTreeClipHook.OnEnable,
                    TimelineRuntimeTreeClipEventKind.Update => AbilityTreeClipHook.Root,
                    TimelineRuntimeTreeClipEventKind.Exit => AbilityTreeClipHook.OnDisable,
                    _ => throw new ArgumentOutOfRangeException(nameof(request))
                },
                request.Time,
                request.Cycle,
                request.Generation,
                request.BranchRevision,
                m_Host.RequireAbilityPlaybackActionInstanceId(context.Playback.Handle),
                checked((int)context.Playback.Handle.Value));
            return invoker.InvokeTreeClip(invocation);
        }

        bool IsActive(TimelineRuntimeTreeClipRequest request, TimelineRuntimeStepContext context)
        {
            TimelineRuntimeSampleView<TimelineRuntimeTreeClipRequest> requests = context.Advance.Evaluation.TreeClips;
            for (int index = requests.Count - 1; index >= 0; index--)
            {
                TimelineRuntimeTreeClipRequest candidate = requests[index];
                if (candidate.EventKind != TimelineRuntimeTreeClipEventKind.Update &&
                    candidate.ClipAuthoringId == request.ClipAuthoringId && candidate.Cycle == request.Cycle)
                    return candidate.EventKind == TimelineRuntimeTreeClipEventKind.Enter;
            }
            if (m_ActiveClips.TryGetValue(context.Playback.Handle.Value, out List<AbilityTimelineTreeClipState> active))
                for (int index = 0; index < active.Count; index++)
                    if (active[index].ClipAuthoringId == request.ClipAuthoringId && active[index].Cycle == request.Cycle)
                        return true;
            return false;
        }

        public void Discard(TimelineRuntimeTreeClipRequest request, TimelineRuntimeStepContext context) { }

        public void Commit(TimelineRuntimeStepContext context)
        {
            if (!m_Host.IsAbilityRuntimePlayback(context.Playback.Handle))
                return;
            TimelineRuntimeSampleView<TimelineRuntimeTreeClipRequest> requests = context.Advance.Evaluation.TreeClips;
            ulong handle = context.Playback.Handle.Value;
            for (int index = 0; index < requests.Count; index++)
            {
                TimelineRuntimeTreeClipRequest request = requests[index];
                if (request.EventKind == TimelineRuntimeTreeClipEventKind.Update)
                    continue;
                RemoveClip(handle, request.ClipAuthoringId, request.Cycle);
                if (request.EventKind != TimelineRuntimeTreeClipEventKind.Enter)
                    continue;
                if (!m_ActiveClips.TryGetValue(handle, out List<AbilityTimelineTreeClipState> clips))
                {
                    clips = new List<AbilityTimelineTreeClipState>(context.Playback.Content.Clips.Count);
                    m_ActiveClips.Add(handle, clips);
                }
                clips.Add(new AbilityTimelineTreeClipState(
                    request.ClipAuthoringId,
                    request.TreeGraphId,
                    request.TreeGraphRevision,
                    request.ClipAuthoringId,
                    request.Generation,
                    request.BranchRevision,
                    request.Cycle));
            }
            if (context.Advance.Completes)
                m_ActiveClips.Remove(handle);
        }

        public void DiscardStep(TimelineRuntimeStepContext context) { }
        public bool ConsumeStop(TimelineRuntimeStopRequest request)
        {
            if (request.Reason.Cause == TimelinePlaybackStopCause.Shutdown)
                return true;
            if (!m_ActiveClips.TryGetValue(request.Handle.Value, out List<AbilityTimelineTreeClipState> clips) || clips.Count == 0)
                return true;
            IAbilityTreeClipInvoker invoker = m_Host.m_ActiveTreeClipInvoker
                ?? throw new InvalidOperationException("Timeline stop requires the current Ability invoker.");
            foreach (AbilityTimelineTreeClipState clip in clips)
            {
                var invocation = new AbilityTreeClipInvocation(
                    clip.ClipAuthoringId,
                    clip.TreeGraphId,
                    clip.TreeGraphRevision,
                    clip.NodeAuthoringId,
                    AbilityTreeClipHook.OnDestroy,
                    FixedScalar.Zero,
                    clip.Cycle,
                    clip.PlaybackGeneration,
                    clip.BranchRevision,
                    m_Host.RequireAbilityPlaybackActionInstanceId(request.Handle),
                    checked((int)request.Handle.Value));
                invoker.InvokeTreeClip(invocation);
            }
            return true;
        }

        public void CommitStop(TimelineRuntimeStopRequest request) =>
            m_ActiveClips.Remove(request.Handle.Value);

        public void DiscardStop(TimelineRuntimeStopRequest request) { }

        void RemoveClip(ulong handle, string clipAuthoringId, int cycle)
        {
            if (!m_ActiveClips.TryGetValue(handle, out List<AbilityTimelineTreeClipState> clips))
                return;
            for (int index = clips.Count - 1; index >= 0; index--)
                if (clips[index].ClipAuthoringId == clipAuthoringId && clips[index].Cycle == cycle)
                    clips.RemoveAt(index);
        }
    }
}
