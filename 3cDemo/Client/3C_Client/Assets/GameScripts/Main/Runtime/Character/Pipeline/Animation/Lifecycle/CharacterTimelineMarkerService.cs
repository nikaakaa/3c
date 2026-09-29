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
    internal sealed class CharacterTimelineMarkerService : ITimelineRuntimeMarkerService
    {
        readonly CharacterTimelineHost m_Host;

        internal CharacterTimelineMarkerService(CharacterTimelineHost host)
        {
            m_Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public bool Consume(TimelineRuntimeMarkerRequest request, TimelineRuntimeStepContext context)
        {
            if (!m_Host.IsAbilityRuntimePlayback(context.Playback.Handle))
                return true;
            IAbilityTreeClipInvoker invoker = m_Host.m_ActiveTreeClipInvoker
                ?? throw new InvalidOperationException($"Timeline Marker '{request.MarkerAuthoringId}' requires an active Ability invoker.");
            var invocation = new AbilityTreeClipInvocation(
                request.MarkerAuthoringId,
                request.GraphId,
                request.GraphRevision,
                request.MarkerAuthoringId,
                AbilityTreeClipHook.OnEnable,
                request.Time,
                request.Cycle,
                request.Generation,
                MotionWarpRuntimeSemantics.ComposePlaybackGeneration(request.Generation, request.Cycle),
                m_Host.RequireAbilityPlaybackActionInstanceId(context.Playback.Handle),
                checked((int)context.Playback.Handle.Value));
            return invoker.InvokeTreeClip(invocation);
        }

        public void Discard(TimelineRuntimeMarkerRequest request, TimelineRuntimeStepContext context)
        {
        }

        public void Commit(TimelineRuntimeStepContext context)
        {
        }

        public void DiscardStep(TimelineRuntimeStepContext context)
        {
        }

        public bool ConsumeStop(TimelineRuntimeStopRequest request) => true;

        public void CommitStop(TimelineRuntimeStopRequest request)
        {
        }

        public void DiscardStop(TimelineRuntimeStopRequest request)
        {
        }
    }
}
