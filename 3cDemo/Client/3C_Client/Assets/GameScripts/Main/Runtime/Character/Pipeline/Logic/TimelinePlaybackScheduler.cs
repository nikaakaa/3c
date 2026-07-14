using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Network;
using ThirdPersonCamera;
using UnityEngine;
using ThirdPersonGameplay.Tick;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Logic
{
    sealed class TimelinePlaybackScheduler : IDisposable
    {
        readonly CharacterGraphContext m_GraphContext;
        readonly IAnimationPlaybackCommandSink m_AnimationCommands;
        readonly CharacterAnimationPresentationBindingIndex m_AnimationBindings;
        readonly List<ResolvedAnimationLayer> m_AnimationLayers = new List<ResolvedAnimationLayer>();
        readonly List<CharacterTimelinePlaybackRequest> m_Requests = new List<CharacterTimelinePlaybackRequest>();
        readonly List<ActiveTimeline> m_ActiveTimelines = new List<ActiveTimeline>();
        readonly List<ActiveTimeline> m_TerminalTimelines = new List<ActiveTimeline>();
        readonly List<TimelineAnimationContribution> m_AnimationSamples = new List<TimelineAnimationContribution>();
        readonly List<CharacterAnimationSelectionCandidate> m_SelectionCandidates = new List<CharacterAnimationSelectionCandidate>();
        readonly List<CharacterAnimationSelectionCandidate> m_ValidSelectionCandidates = new List<CharacterAnimationSelectionCandidate>();
        readonly List<CharacterAnimationLayerDecision> m_SelectionDecisions = new List<CharacterAnimationLayerDecision>();
        readonly CharacterAnimationSelectionResolver m_SelectionResolver = new CharacterAnimationSelectionResolver();
        readonly List<TimelineMotionCurveContribution> m_MotionCurveSamples = new List<TimelineMotionCurveContribution>();
        readonly List<TimelineMotionWarpWindow> m_MotionWarpSamples = new List<TimelineMotionWarpWindow>();
        readonly List<TimelineActionCueSample> m_ActionCueSamples = new List<TimelineActionCueSample>();
        readonly List<TimelineCameraStateSample> m_CameraStateSamples = new List<TimelineCameraStateSample>();
        readonly List<TimelineCameraCueSample> m_CameraCueSamples = new List<TimelineCameraCueSample>();
        readonly List<TimelineCameraResponseSample> m_CameraResponseSamples = new List<TimelineCameraResponseSample>();

        public TimelinePlaybackScheduler(
            CharacterGraphContext graphContext,
            IAnimationPlaybackCommandSink animationCommands,
            CharacterAnimationPresentationBindingIndex animationBindings)
        {
            m_GraphContext = graphContext;
            m_AnimationCommands = animationCommands;
            m_AnimationBindings = animationBindings ?? throw new ArgumentNullException(nameof(animationBindings));
            foreach (ResolvedAnimationLayer layer in animationBindings.Layers.Values)
                m_AnimationLayers.Add(layer);
            m_AnimationLayers.Sort((left, right) => left.Order.CompareTo(right.Order));
        }

        public void PrepareDecisionFacts(GameplayLogicTickContext context)
        {
            if (m_GraphContext == null)
                return;

            for (int i = 0; i < m_ActiveTimelines.Count; i++)
            {
                ActiveTimeline activeTimeline = m_ActiveTimelines[i];
                TimelinePlaybackStatus status = m_GraphContext.GetTimelinePlaybackStatus(activeTimeline.Handle);
                if (status != TimelinePlaybackStatus.Requested && status != TimelinePlaybackStatus.Running)
                    continue;

                if (!activeTimeline.PrepareDecisionFacts(context))
                {
                    Debug.LogError($"TimelinePlaybackScheduler failed to prepare decision facts: handle={HandleLabel(activeTimeline.Handle)}.");
                    m_GraphContext.SetTimelinePlaybackStatus(activeTimeline.Handle, TimelinePlaybackStatus.Failed);
                    continue;
                }

            }
        }

        public void Commit(GameplayLogicTickContext context, CharacterPipelineFrame frame)
        {
            if (m_GraphContext == null || frame == null)
                return;

            for (int i = m_ActiveTimelines.Count - 1; i >= 0; i--)
            {
                ActiveTimeline activeTimeline = m_ActiveTimelines[i];
                if (m_GraphContext.GetTimelinePlaybackStatus(activeTimeline.Handle) == TimelinePlaybackStatus.Cancelled)
                {
                    activeTimeline.ForceStopTrees(ConsumeStopContext(activeTimeline.Handle, context.LocalLogicTick));
                    TerminateTimeline(i, activeTimeline, context.LocalLogicTick, false);
                }
            }

            m_Requests.Clear();
            m_GraphContext.ConsumeTimelinePlaybackRequests(m_Requests);
            for (int i = 0; i < m_Requests.Count; i++)
                StartRequest(m_Requests[i]);

            for (int i = m_ActiveTimelines.Count - 1; i >= 0; i--)
            {
                ActiveTimeline activeTimeline = m_ActiveTimelines[i];
                UpdateActiveTimeline(i, context, frame);
            }

            SubmitLayerSelections(context.LocalLogicTick);
        }

        public void SamplePresentation(
            GameplayPresentationFrameContext context,
            CharacterPipelineFrame frame,
            IReadOnlyCollection<AnimationPlaybackId> demandedPlaybacks)
        {
            if (frame == null || demandedPlaybacks == null || demandedPlaybacks.Count == 0)
                return;

            for (int i = 0; i < m_ActiveTimelines.Count; i++)
                m_ActiveTimelines[i].SamplePresentation(context, m_AnimationCommands, m_AnimationSamples, demandedPlaybacks, false);

            for (int i = 0; i < m_TerminalTimelines.Count; i++)
                m_TerminalTimelines[i].SamplePresentation(context, m_AnimationCommands, m_AnimationSamples, demandedPlaybacks, true);
        }

        public void CompletePresentationFrame(IReadOnlyCollection<AnimationPlaybackId> retiredPlaybacks)
        {
            for (int i = m_TerminalTimelines.Count - 1; i >= 0; i--)
            {
                ActiveTimeline terminal = m_TerminalTimelines[i];
                terminal.ApplyRetiredPlaybacks(retiredPlaybacks);
                if (!terminal.AllAnimationPlaybacksRetired)
                    continue;

                terminal.Dispose();
                m_TerminalTimelines.RemoveAt(i);
            }
        }

        public void Deactivate()
        {
            for (int i = m_ActiveTimelines.Count - 1; i >= 0; i--)
            {
                ActiveTimeline activeTimeline = m_ActiveTimelines[i];
                m_GraphContext?.SetTimelinePlaybackStatus(activeTimeline.Handle, TimelinePlaybackStatus.Cancelled);
                activeTimeline.ForceStopTrees(NodeStopContext.Create(NodeStopOriginCause.Shutdown, m_GraphContext != null ? m_GraphContext.LocalLogicTick : 0, null));
                activeTimeline.Dispose();
            }

            for (int i = m_TerminalTimelines.Count - 1; i >= 0; i--)
            {
                ActiveTimeline terminal = m_TerminalTimelines[i];
                terminal.Dispose();
            }

            m_ActiveTimelines.Clear();
            m_TerminalTimelines.Clear();
            m_Requests.Clear();
            m_AnimationSamples.Clear();
            m_MotionCurveSamples.Clear();
            m_MotionWarpSamples.Clear();
            m_ActionCueSamples.Clear();
            m_CameraStateSamples.Clear();
            m_CameraCueSamples.Clear();
            m_CameraResponseSamples.Clear();
            ClearSelectionCandidates();
            m_SelectionResolver.Reset();
        }

        public void Dispose()
        {
            Deactivate();
        }

        void StartRequest(CharacterTimelinePlaybackRequest request)
        {
            if (!request.Handle.IsValid || request.Timeline == null)
            {
                Debug.LogError($"TimelinePlaybackScheduler failed to start invalid request: handle={HandleLabel(request.Handle)} timeline={TimelineName(request.Timeline)} mode={request.PlaybackMode}");
                m_GraphContext.SetTimelinePlaybackStatus(request.Handle, TimelinePlaybackStatus.Failed);
                return;
            }

            TimelinePlaybackStatus status = m_GraphContext.GetTimelinePlaybackStatus(request.Handle);
            if (status == TimelinePlaybackStatus.Cancelled)
                return;

            TimelineData runtimeTimeline = request.Timeline.Clone();
            runtimeTimeline.Init();
            m_GraphContext.SetTimelinePlaybackStatus(request.Handle, TimelinePlaybackStatus.Running);
            var activeTimeline = new ActiveTimeline(request, runtimeTimeline, m_GraphContext);
            m_ActiveTimelines.Add(activeTimeline);
            PublishPlayback(activeTimeline, RuntimeTraceEventKind.TimelineStarted, RuntimeTraceDomain.Logic, "Running", string.Empty);
        }

        void UpdateActiveTimeline(int index, GameplayLogicTickContext context, CharacterPipelineFrame frame)
        {
            ActiveTimeline activeTimeline = m_ActiveTimelines[index];
            TimelinePlaybackStatus status = m_GraphContext.GetTimelinePlaybackStatus(activeTimeline.Handle);
            if (status == TimelinePlaybackStatus.Cancelled)
            {
                activeTimeline.ForceStopTrees(ConsumeStopContext(activeTimeline.Handle, context.LocalLogicTick));
                TerminateTimeline(index, activeTimeline, context.LocalLogicTick, false);
                return;
            }

            if (!activeTimeline.Update(
                    m_GraphContext,
                    context,
                    frame,
                    context.FixedDeltaSeconds,
                    m_AnimationSamples,
                    m_MotionCurveSamples,
                    m_MotionWarpSamples,
                    m_ActionCueSamples,
                    m_CameraStateSamples,
                    m_CameraCueSamples,
                    m_CameraResponseSamples))
            {
                Debug.LogError($"TimelinePlaybackScheduler failed to update handle={HandleLabel(activeTimeline.Handle)}.");
                m_GraphContext.SetTimelinePlaybackStatus(activeTimeline.Handle, TimelinePlaybackStatus.Failed);
                activeTimeline.ForceStopTrees(NodeStopContext.Create(NodeStopOriginCause.Shutdown, context.LocalLogicTick, null));
                TerminateTimeline(index, activeTimeline, context.LocalLogicTick, false);
                return;
            }

            PublishPlayback(activeTimeline, RuntimeTraceEventKind.TimelineLogicTime, RuntimeTraceDomain.Logic, "Running", string.Empty);
            PublishMembership(activeTimeline);

            if (activeTimeline.Completed)
            {
                m_GraphContext.SetTimelinePlaybackStatus(activeTimeline.Handle, TimelinePlaybackStatus.Succeeded);
                TerminateTimeline(index, activeTimeline, context.LocalLogicTick, true);
            }
        }

        void TerminateTimeline(int index, ActiveTimeline activeTimeline, ulong localLogicTick, bool completed)
        {
            m_ActiveTimelines.RemoveAt(index);
            PublishPlayback(
                activeTimeline,
                completed ? RuntimeTraceEventKind.TimelineCompleted : RuntimeTraceEventKind.TimelineStopped,
                RuntimeTraceDomain.Logic,
                completed ? "Succeeded" : "Stopped",
                completed ? string.Empty : "CancelledOrFailed");
            if (completed)
            {
                activeTimeline.EnqueueCompletion(m_AnimationCommands, localLogicTick);
            }
            else
            {
                activeTimeline.EnqueueRelease(m_AnimationCommands, localLogicTick);
            }

            activeTimeline.BeginPresentationRetention();
            if (activeTimeline.HasAnimationPlaybacks)
                m_TerminalTimelines.Add(activeTimeline);
            else
                activeTimeline.Dispose();
        }

        void SubmitLayerSelections(ulong localLogicTick)
        {
            ClearSelectionCandidates();
            for (int i = 0; i < m_ActiveTimelines.Count; i++)
                m_ActiveTimelines[i].CollectSelectionCandidates(m_SelectionCandidates);

            bool valid = true;
            for (int i = 0; i < m_SelectionCandidates.Count; i++)
            {
                CharacterAnimationSelectionCandidate candidate = m_SelectionCandidates[i];
                if (!m_AnimationBindings.TryGetBinding(candidate.PlaybackId.ProducerId, out ResolvedAnimationProducerBinding binding) ||
                    !string.Equals(binding.LayerId, candidate.LayerId, StringComparison.Ordinal))
                {
                    m_GraphContext.ReportAnimationLayerSelectionError(
                        $"Animation logic selection references unknown or mismatched producer '{candidate.PlaybackId.ProducerId}' " +
                        $"on layer '{candidate.LayerId}'.");
                    valid = false;
                    continue;
                }
                m_ValidSelectionCandidates.Add(candidate);
            }

            if (!m_SelectionResolver.Resolve(
                    m_AnimationLayers,
                    m_ValidSelectionCandidates,
                    m_GraphContext.ActiveActionInstanceId,
                    m_SelectionDecisions))
            {
                for (int i = 0; i < m_SelectionResolver.Errors.Count; i++)
                    m_GraphContext.ReportAnimationLayerSelectionError(m_SelectionResolver.Errors[i]);
                valid = false;
            }

            for (int i = 0; valid && i < m_SelectionDecisions.Count; i++)
            {
                CharacterAnimationLayerDecision decision = m_SelectionDecisions[i];
                if (!decision.Submit)
                    continue;

                ulong sequence = m_GraphContext.NextAnimationSelectionSequence();
                CharacterAnimationSelectionCandidate selected = decision.Candidate;
                AnimationLayerSelection selection = decision.HasPlayback
                    ? AnimationLayerSelection.Select(decision.LayerId, selected.PlaybackId, localLogicTick, sequence)
                    : AnimationLayerSelection.Empty(decision.LayerId, localLogicTick, sequence);
                valid &= m_GraphContext.SubmitAnimationLayerSelection(
                    selection,
                    decision.HasPlayback ? selected.SourceId : string.Empty,
                    decision.HasPlayback ? selected.SourceName : "None");
            }

            if (!valid)
            {
                m_GraphContext.CommitAnimationLayerSelections(m_AnimationCommands);
                return;
            }

            if (!m_GraphContext.CommitAnimationLayerSelections(m_AnimationCommands))
                Debug.LogError($"Animation logic selection batch was rejected at logic tick {localLogicTick}.");
        }

        void ClearSelectionCandidates()
        {
            m_SelectionCandidates.Clear();
            m_ValidSelectionCandidates.Clear();
            m_SelectionDecisions.Clear();
        }

        NodeStopContext ConsumeStopContext(TimelinePlaybackHandle handle, ulong localLogicTick)
        {
            if (m_GraphContext.TryConsumeTimelinePlaybackStopContext(handle, out TimelinePlaybackStopContext stopContext))
                return NodeStopContext.Create((NodeStopOriginCause)stopContext.Cause, stopContext.LocalLogicTick, null);

            throw new InvalidOperationException(
                $"TimelineData cancellation is missing NodeStopContext: handle={HandleLabel(handle)} tick={localLogicTick}.");
        }

        static string HandleLabel(TimelinePlaybackHandle handle)
        {
            return handle.IsValid ? handle.Value.ToString() : "invalid";
        }

        static string TimelineName(TimelineData timeline)
        {
            return timeline != null ? timeline.Name : "<null>";
        }

        void PublishPlayback(
            ActiveTimeline activeTimeline,
            RuntimeTraceEventKind kind,
            RuntimeTraceDomain domain,
            string status,
            string cause)
        {
            RuntimeDiagnosticsContext diagnostics = m_GraphContext?.RuntimeDiagnostics;
            if (activeTimeline == null || diagnostics == null || !diagnostics.ShouldPublish(RuntimeTraceChannel.Timeline, kind))
                return;
            diagnostics.Publish(
                RuntimeTraceChannel.Timeline,
                domain,
                kind,
                RuntimeSourceElementKey.Timeline(activeTimeline.Timeline.AuthoringId),
                RuntimeInstanceKey.Timeline(diagnostics.CharacterRuntimeId, activeTimeline.Handle.Value),
                new RuntimeTracePayload
                {
                    Name = activeTimeline.Timeline.Name,
                    Status = status,
                    Cause = cause,
                    Time = domain == RuntimeTraceDomain.Presentation ? activeTimeline.VisualTime : activeTimeline.Time,
                    SecondaryTime = activeTimeline.Time,
                    Cycle = domain == RuntimeTraceDomain.Presentation ? activeTimeline.VisualCycle : activeTimeline.Cycle,
                    RelatedElementId = activeTimeline.SourceId,
                    TimelinePlayback = activeTimeline.DiagnosticsProvenance
                });
        }

        void PublishMembership(ActiveTimeline activeTimeline)
        {
            RuntimeDiagnosticsContext diagnostics = m_GraphContext?.RuntimeDiagnostics;
            if (activeTimeline == null || diagnostics == null || !diagnostics.ShouldPublish(RuntimeTraceChannel.Timeline, RuntimeTraceEventKind.TrackActive))
                return;
            TimelineData timeline = activeTimeline.Timeline;
            RuntimeInstanceKey instance = RuntimeInstanceKey.Timeline(diagnostics.CharacterRuntimeId, activeTimeline.Handle.Value);
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track.PersistentMuted)
                    continue;
                diagnostics.Publish(
                    RuntimeTraceChannel.Timeline,
                    RuntimeTraceDomain.Logic,
                    RuntimeTraceEventKind.TrackActive,
                    RuntimeSourceElementKey.Track(timeline.AuthoringId, track.AuthoringId),
                    instance,
                    new RuntimeTracePayload
                    {
                        Name = track.Name,
                        Time = activeTimeline.Time,
                        Cycle = activeTimeline.Cycle,
                        TrackIndex = trackIndex,
                        TimelinePlayback = activeTimeline.DiagnosticsProvenance
                    });
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (activeTimeline.Time < clip.StartTime || activeTimeline.Time > clip.EndTime)
                        continue;
                    diagnostics.Publish(
                        RuntimeTraceChannel.Timeline,
                        RuntimeTraceDomain.Logic,
                        RuntimeTraceEventKind.ClipActive,
                        RuntimeSourceElementKey.Clip(timeline.AuthoringId, track.AuthoringId, clip.AuthoringId, clip is TreeClip),
                        instance,
                        new RuntimeTracePayload
                        {
                            Name = clip.GetType().Name,
                            Time = activeTimeline.Time,
                            Cycle = activeTimeline.Cycle,
                            TrackIndex = trackIndex,
                            ClipIndex = clipIndex,
                            TimelinePlayback = activeTimeline.DiagnosticsProvenance
                        });
                }
            }
        }

        sealed class ActiveTimeline : IDisposable
        {
            readonly CharacterTimelinePlaybackRequest m_Request;
            readonly TimelineData m_Timeline;
            readonly TimelineTreeRuntimeSet m_TreeRuntimes;
            readonly CharacterGraphContext m_GraphContext;
            readonly RuntimeDiagnosticsContext m_Diagnostics;
            readonly List<AnimationTrackPlayback> m_AnimationPlaybacks = new List<AnimationTrackPlayback>();
            readonly HashSet<AnimationPlaybackId> m_RetiredPlaybacks = new HashSet<AnimationPlaybackId>();
            readonly List<AnimationClipSample> m_ClipSamples = new List<AnimationClipSample>();

            float m_Time;
            int m_CycleIndex;
            float m_PreviousPresentationTime;
            float m_CurrentPresentationTime;
            int m_PreviousPresentationCycleIndex;
            int m_CurrentPresentationCycleIndex;
            bool m_HasPresentationSegment;

            bool m_DurationReached;
            bool m_GameplayDisposed;
            bool m_PresentationRetained;
            float m_VisualTime;
            int m_VisualCycle;

            public ActiveTimeline(
                CharacterTimelinePlaybackRequest request,
                TimelineData timeline,
                CharacterGraphContext graphContext)
            {
                m_Request = request;
                m_Timeline = timeline;
                m_GraphContext = graphContext;
                m_Diagnostics = graphContext?.RuntimeDiagnostics;
                m_TreeRuntimes = new TimelineTreeRuntimeSet(
                    timeline,
                    graphContext,
                    request.Handle,
                    request.ActionContext,
                    request.SourceActivation,
                    request.SourceRuntimeGraph);
                CollectAnimationPlaybacks();
            }

            public TimelinePlaybackHandle Handle => m_Request.Handle;
            public TimelinePlaybackActionContext ActionContext => m_Request.ActionContext;
            public TimelineData Timeline => m_Timeline;
            public float Time => m_Time;
            public int Cycle => m_CycleIndex;
            public float VisualTime => m_VisualTime;
            public int VisualCycle => m_VisualCycle;
            public string SourceId => m_Request.SourceId;
            public RuntimeTimelinePlaybackProvenance DiagnosticsProvenance => m_Request.DiagnosticsProvenance;
            public bool Completed { get; private set; }
            public bool HasAnimationPlaybacks => m_AnimationPlaybacks.Count > 0;
            public bool AllAnimationPlaybacksRetired =>
                m_AnimationPlaybacks.Count == 0 || m_RetiredPlaybacks.Count == m_AnimationPlaybacks.Count;

            public void ForceStopTrees(NodeStopContext stopContext)
            {
                m_TreeRuntimes.ForceStopAll(stopContext);
            }

            public void BeginPresentationRetention()
            {
                DisposeGameplay();
                m_PresentationRetained = true;
            }

            public void EnqueueCompletion(IAnimationPlaybackCommandSink commands, ulong localLogicTick)
            {
                for (int i = 0; i < m_AnimationPlaybacks.Count; i++)
                    commands?.EnqueuePlaybackComplete(localLogicTick, m_AnimationPlaybacks[i].PlaybackId);
            }

            public void EnqueueRelease(IAnimationPlaybackCommandSink commands, ulong localLogicTick)
            {
                for (int i = 0; i < m_AnimationPlaybacks.Count; i++)
                    commands?.EnqueuePlaybackRelease(localLogicTick, m_AnimationPlaybacks[i].PlaybackId);
            }

            public void ApplyRetiredPlaybacks(IReadOnlyCollection<AnimationPlaybackId> retiredPlaybacks)
            {
                if (retiredPlaybacks == null)
                    return;
                foreach (AnimationPlaybackId playbackId in retiredPlaybacks)
                {
                    if (ContainsPlayback(playbackId))
                        m_RetiredPlaybacks.Add(playbackId);
                }
            }

            public void CollectSelectionCandidates(List<CharacterAnimationSelectionCandidate> destination)
            {
                if (destination == null)
                    return;
                for (int i = 0; i < m_AnimationPlaybacks.Count; i++)
                {
                    AnimationTrackPlayback playback = m_AnimationPlaybacks[i];
                    destination.Add(new CharacterAnimationSelectionCandidate(
                        playback.PlaybackId,
                        playback.LayerId,
                        SourceId,
                        $"{m_Request.SourceName}/{playback.Track.Name}",
                        ActionContext.ActionInstanceId));
                }
            }

            public bool Update(
                CharacterGraphContext graphContext,
                GameplayLogicTickContext context,
                CharacterPipelineFrame frame,
                float deltaTime,
                List<TimelineAnimationContribution> animationSamples,
                List<TimelineMotionCurveContribution> motionCurveSamples,
                List<TimelineMotionWarpWindow> motionWarpSamples,
                List<TimelineActionCueSample> actionCueSamples,
                List<TimelineCameraStateSample> cameraStateSamples,
                List<TimelineCameraCueSample> cameraCueSamples,
                List<TimelineCameraResponseSample> cameraResponseSamples)
            {
                if (m_Timeline == null)
                    return false;

                animationSamples.Clear();
                motionCurveSamples.Clear();
                motionWarpSamples.Clear();
                actionCueSamples.Clear();
                cameraStateSamples.Clear();
                cameraCueSamples.Clear();
                cameraResponseSamples.Clear();

                float duration = Mathf.Max(0f, m_Timeline.Duration);
                float previousPresentationTime = m_Time;
                int previousPresentationCycleIndex = m_CycleIndex;
                if (m_Request.PlaybackMode == TimelinePlaybackMode.Once && m_DurationReached)
                {
                    if (!m_TreeRuntimes.UpdateNaturalStopping(deltaTime, context.LocalLogicTick))
                        return false;

                    Completed = !m_TreeRuntimes.HasNaturalStoppingRuntimes;
                    return true;
                }

                if (!m_TreeRuntimes.BeginCommitTick(deltaTime, context.LocalLogicTick))
                    return false;

                if (m_Request.PlaybackMode == TimelinePlaybackMode.Loop)
                {
                    if (duration <= 0f)
                    {
                        Debug.LogError($"Loop Timeline requires duration greater than 0: handle={HandleLabel(Handle)} timeline={TimelineName(m_Timeline)} duration={duration:F3}");
                        return false;
                    }

                    if (!SampleLoop(
                            duration,
                            Mathf.Max(0f, deltaTime),
                            context.LocalLogicTick,
                            animationSamples,
                            motionCurveSamples,
                            motionWarpSamples,
                            actionCueSamples,
                            cameraStateSamples,
                            cameraCueSamples,
                            cameraResponseSamples))
                        return false;
                    Completed = false;
                }
                else
                {
                    float previousTime = m_Time;
                    m_Time = duration > 0f ? Mathf.Min(m_Time + Mathf.Max(0f, deltaTime), duration) : 0f;
                    SampleSegment(previousTime, m_Time, false, false, 0, animationSamples, motionCurveSamples, motionWarpSamples, actionCueSamples, cameraStateSamples, cameraCueSamples, cameraResponseSamples);
                    if (!m_TreeRuntimes.CommitSegment(previousTime, m_Time, 0, deltaTime, context.LocalLogicTick))
                        return false;

                    m_DurationReached = duration <= 0f || m_Time >= duration;
                    if (m_DurationReached)
                        m_TreeRuntimes.BeginTimelineCompletion(context.LocalLogicTick);
                    Completed = m_DurationReached && !m_TreeRuntimes.HasNaturalStoppingRuntimes;
                }

                m_Timeline.Time = m_Time;
                SetPresentationSegment(previousPresentationTime, previousPresentationCycleIndex, m_Time, m_CycleIndex);

                TimelinePlaybackActionContext actionContext = m_Request.ActionContext;

                for (int i = 0; i < motionCurveSamples.Count; i++)
                    SubmitMotionCurve(graphContext, actionContext, context, frame, motionCurveSamples[i]);

                for (int i = 0; i < motionWarpSamples.Count; i++)
                    SubmitMotionWarp(graphContext, actionContext, context, frame, motionWarpSamples[i]);

                HashSet<string> submittedActionCueKeys = null;
                for (int i = 0; i < actionCueSamples.Count; i++)
                {
                    if (!TryAddActionCueSampleKey(actionCueSamples[i], ref submittedActionCueKeys))
                        continue;

                    SubmitActionCue(graphContext, actionContext, actionCueSamples[i], context.LocalLogicTick);
                }

                for (int i = 0; i < cameraStateSamples.Count; i++)
                    SubmitCameraState(frame, actionContext, cameraStateSamples[i]);

                for (int i = 0; i < cameraCueSamples.Count; i++)
                    SubmitCameraCue(frame, actionContext, cameraCueSamples[i]);

                for (int i = 0; i < cameraResponseSamples.Count; i++)
                    SubmitCameraResponse(frame, actionContext, cameraResponseSamples[i]);

                return true;
            }

            public bool PrepareDecisionFacts(GameplayLogicTickContext context)
            {
                if (m_Timeline == null)
                    return false;

                float deltaTime = context.FixedDeltaSeconds;
                m_TreeRuntimes.BeginDecisionTick();
                float duration = Mathf.Max(0f, m_Timeline.Duration);
                if (m_Request.PlaybackMode != TimelinePlaybackMode.Loop)
                {
                    float targetTime = duration > 0f
                        ? Mathf.Min(m_Time + Mathf.Max(0f, deltaTime), duration)
                        : 0f;
                    return m_TreeRuntimes.EvaluateDecisionSegment(
                        m_Time,
                        targetTime,
                        0,
                        deltaTime,
                        context.LocalLogicTick);
                }

                if (duration <= 0f)
                    return false;

                return EvaluateLoopDecisionSegments(duration, Mathf.Max(0f, deltaTime), context.LocalLogicTick);
            }

            bool EvaluateLoopDecisionSegments(float duration, float deltaTime, ulong localLogicTick)
            {
                float remaining = deltaTime;
                float previousTime = m_Time;
                int cycle = m_CycleIndex;
                float available = duration - previousTime;
                if (remaining < available)
                {
                    return m_TreeRuntimes.EvaluateDecisionSegment(
                        previousTime,
                        previousTime + remaining,
                        cycle,
                        deltaTime,
                        localLogicTick);
                }

                if (!m_TreeRuntimes.EvaluateDecisionSegment(previousTime, duration, cycle, deltaTime, localLogicTick))
                    return false;
                remaining -= available;
                cycle++;

                while (remaining >= duration)
                {
                    if (!m_TreeRuntimes.EvaluateDecisionSegment(0f, duration, cycle, deltaTime, localLogicTick))
                        return false;
                    remaining -= duration;
                    cycle++;
                }

                return m_TreeRuntimes.EvaluateDecisionSegment(0f, remaining, cycle, deltaTime, localLogicTick);
            }

            public void SamplePresentation(
                GameplayPresentationFrameContext context,
                IAnimationPlaybackCommandSink animationCommands,
                List<TimelineAnimationContribution> animationSamples,
                IReadOnlyCollection<AnimationPlaybackId> demandedPlaybacks,
                bool retained)
            {
                if (m_Timeline == null || animationSamples == null || animationCommands == null ||
                    demandedPlaybacks == null)
                    return;

                float duration = Mathf.Max(0f, m_Timeline.Duration);
                if (duration <= 0f || !m_HasPresentationSegment)
                    return;

                if (retained || m_PresentationRetained)
                    AdvanceRetainedPresentation(duration, context.ScaledDeltaSeconds);
                else
                    ResolvePresentationTime(
                        duration,
                        Mathf.Clamp01(context.InterpolationAlpha),
                        out m_VisualTime,
                        out m_VisualCycle);

                bool isLooping = m_Request.PlaybackMode == TimelinePlaybackMode.Loop;
                bool sampled = false;
                for (int i = 0; i < m_AnimationPlaybacks.Count; i++)
                {
                    AnimationTrackPlayback playback = m_AnimationPlaybacks[i];
                    if (m_RetiredPlaybacks.Contains(playback.PlaybackId) ||
                        !ContainsPlayback(demandedPlaybacks, playback.PlaybackId))
                        continue;

                    SampleProducer(
                        playback,
                        context.LocalLogicTick,
                        isLooping,
                        animationCommands,
                        animationSamples);
                    sampled = true;
                }

                if (sampled)
                    PublishTimelineVisualTime();
            }

            void SampleProducer(
                AnimationTrackPlayback playback,
                ulong localLogicTick,
                bool isLooping,
                IAnimationPlaybackCommandSink animationCommands,
                List<TimelineAnimationContribution> animationSamples)
            {
                animationSamples.Clear();
                m_ClipSamples.Clear();
                playback.Track.Sample(
                    m_VisualTime,
                    m_VisualTime,
                    playback.TrackIndex,
                    m_Request.SourceId,
                    m_Request.SourceName,
                    animationSamples,
                    isLooping,
                    m_VisualCycle);
                for (int i = 0; i < animationSamples.Count; i++)
                {
                    TimelineAnimationContribution sample = animationSamples[i];
                    if (!string.Equals(sample.TrackAuthoringId, playback.PlaybackId.ProducerId.TrackAuthoringId, StringComparison.Ordinal))
                        continue;
                    AnimationClipSample clipSample = ToAnimationClipSample(sample);
                    if (!clipSample.IsValid)
                        continue;
                    m_ClipSamples.Add(clipSample);
                }

                animationCommands.EnqueueSample(localLogicTick, new AnimationProducerSample(
                    playback.PlaybackId,
                    playback.LayerId,
                    m_Request.SourceId,
                    m_Request.SourceName,
                    playback.Track.Name,
                    m_VisualTime,
                    m_VisualCycle,
                    m_ClipSamples));
            }

            void SetPresentationSegment(float previousTime, int previousCycleIndex, float currentTime, int currentCycleIndex)
            {
                m_PreviousPresentationTime = previousTime;
                m_PreviousPresentationCycleIndex = previousCycleIndex;
                m_CurrentPresentationTime = currentTime;
                m_CurrentPresentationCycleIndex = currentCycleIndex;
                m_HasPresentationSegment = true;
            }

            void ResolvePresentationTime(float duration, float alpha, out float visualTime, out int cycleIndex)
            {
                if (m_Request.PlaybackMode != TimelinePlaybackMode.Loop)
                {
                    visualTime = Mathf.Lerp(m_PreviousPresentationTime, m_CurrentPresentationTime, alpha);
                    cycleIndex = 0;
                    return;
                }

                float previousTotal = m_PreviousPresentationCycleIndex * duration + m_PreviousPresentationTime;
                float currentTotal = m_CurrentPresentationCycleIndex * duration + m_CurrentPresentationTime;
                float visualTotal = Mathf.Lerp(previousTotal, currentTotal, alpha);
                cycleIndex = Mathf.Max(0, Mathf.FloorToInt(visualTotal / duration));
                visualTime = visualTotal - cycleIndex * duration;
                if (visualTime >= duration)
                    visualTime = 0f;
            }

            void AdvanceRetainedPresentation(float duration, float deltaSeconds)
            {
                float delta = Mathf.Max(0f, deltaSeconds);
                if (m_Request.PlaybackMode != TimelinePlaybackMode.Loop)
                {
                    m_VisualTime = Mathf.Min(duration, m_VisualTime + delta);
                    m_VisualCycle = 0;
                    return;
                }

                float total = m_VisualCycle * duration + m_VisualTime + delta;
                m_VisualCycle = Mathf.Max(0, Mathf.FloorToInt(total / duration));
                m_VisualTime = total - m_VisualCycle * duration;
            }

            bool SampleLoop(
                float duration,
                float deltaTime,
                ulong localLogicTick,
                List<TimelineAnimationContribution> animationSamples,
                List<TimelineMotionCurveContribution> motionCurveSamples,
                List<TimelineMotionWarpWindow> motionWarpSamples,
                List<TimelineActionCueSample> actionCueSamples,
                List<TimelineCameraStateSample> cameraStateSamples,
                List<TimelineCameraCueSample> cameraCueSamples,
                List<TimelineCameraResponseSample> cameraResponseSamples)
            {
                float previousTime = m_Time;
                float nextTime = m_Time + deltaTime;
                if (nextTime < duration)
                {
                    m_Time = nextTime;
                    SampleSegment(previousTime, m_Time, false, true, m_CycleIndex, animationSamples, motionCurveSamples, motionWarpSamples, actionCueSamples, cameraStateSamples, cameraCueSamples, cameraResponseSamples);
                    return m_TreeRuntimes.CommitSegment(previousTime, m_Time, m_CycleIndex, deltaTime, localLogicTick);
                }

                SampleSegment(previousTime, duration, false, true, m_CycleIndex, animationSamples, motionCurveSamples, motionWarpSamples, actionCueSamples, cameraStateSamples, cameraCueSamples, cameraResponseSamples);
                if (!m_TreeRuntimes.CommitSegment(previousTime, duration, m_CycleIndex, deltaTime, localLogicTick))
                    return false;
                m_TreeRuntimes.EndLoopCycle(m_CycleIndex, localLogicTick);
                nextTime -= duration;
                m_CycleIndex++;

                while (nextTime >= duration)
                {
                    SampleSegment(0f, duration, false, true, m_CycleIndex, animationSamples, motionCurveSamples, motionWarpSamples, actionCueSamples, cameraStateSamples, cameraCueSamples, cameraResponseSamples);
                    if (!m_TreeRuntimes.CommitSegment(0f, duration, m_CycleIndex, deltaTime, localLogicTick))
                        return false;
                    m_TreeRuntimes.EndLoopCycle(m_CycleIndex, localLogicTick);
                    nextTime -= duration;
                    m_CycleIndex++;
                }

                m_Time = nextTime;
                SampleSegment(0f, m_Time, false, true, m_CycleIndex, animationSamples, motionCurveSamples, motionWarpSamples, actionCueSamples, cameraStateSamples, cameraCueSamples, cameraResponseSamples);
                return m_TreeRuntimes.CommitSegment(0f, m_Time, m_CycleIndex, deltaTime, localLogicTick);
            }

            void SampleSegment(
                float previousTime,
                float currentTime,
                bool includeAnimationPose,
                bool isLooping,
                int cycleIndex,
                List<TimelineAnimationContribution> animationSamples,
                List<TimelineMotionCurveContribution> motionCurveSamples,
                List<TimelineMotionWarpWindow> motionWarpSamples,
                List<TimelineActionCueSample> actionCueSamples,
                List<TimelineCameraStateSample> cameraStateSamples,
                List<TimelineCameraCueSample> cameraCueSamples,
                List<TimelineCameraResponseSample> cameraResponseSamples)
            {
                for (int i = 0; i < m_Timeline.Tracks.Count; i++)
                {
                    if (m_Timeline.Tracks[i] is AnimationTrack animationTrack)
                        animationTrack.Sample(previousTime, currentTime, i, m_Request.SourceId, m_Request.SourceName, includeAnimationPose ? animationSamples : null, isLooping, cycleIndex);
                    else if (m_Timeline.Tracks[i] is MotionCurveTrack motionCurveTrack)
                        motionCurveTrack.Sample(previousTime, currentTime, m_Request.SourceId, m_Request.SourceName, motionCurveSamples);
                    else if (m_Timeline.Tracks[i] is MotionWarpTrack motionWarpTrack)
                        motionWarpTrack.Sample(currentTime, m_Request.SourceId, m_Request.SourceName, motionWarpSamples);
                    else if (m_Timeline.Tracks[i] is ActionCueTrack actionCueTrack)
                        actionCueTrack.Sample(previousTime, currentTime, m_Request.SourceId, m_Request.SourceName, actionCueSamples);
                    else if (m_Timeline.Tracks[i] is CameraStateTrack cameraStateTrack)
                        cameraStateTrack.Sample(currentTime, m_Request.SourceId, m_Request.SourceName, cameraStateSamples);
                    else if (m_Timeline.Tracks[i] is CameraCueTrack cameraCueTrack)
                        cameraCueTrack.Sample(previousTime, currentTime, m_Request.SourceId, m_Request.SourceName, cameraCueSamples);
                    else if (m_Timeline.Tracks[i] is CameraResponseTrack cameraResponseTrack)
                        cameraResponseTrack.Sample(currentTime, m_Request.SourceId, m_Request.SourceName, cameraResponseSamples);
                }
            }

            public void Dispose()
            {
                DisposeGameplay();
                m_AnimationPlaybacks.Clear();
                m_RetiredPlaybacks.Clear();
                m_ClipSamples.Clear();
            }

            void PublishTimelineVisualTime()
            {
                if (m_Diagnostics == null || !m_Diagnostics.ShouldPublish(RuntimeTraceChannel.Timeline, RuntimeTraceEventKind.TimelineVisualTime))
                    return;
                m_Diagnostics.Publish(
                    RuntimeTraceChannel.Timeline,
                    RuntimeTraceDomain.Presentation,
                    RuntimeTraceEventKind.TimelineVisualTime,
                    RuntimeSourceElementKey.Timeline(m_Timeline.AuthoringId),
                    RuntimeInstanceKey.Timeline(m_Diagnostics.CharacterRuntimeId, Handle.Value),
                    new RuntimeTracePayload
                    {
                        Name = m_Timeline.Name,
                        Status = Completed ? "Terminal" : "Running",
                        Time = m_VisualTime,
                        SecondaryTime = m_Time,
                        Cycle = m_VisualCycle,
                        TimelinePlayback = m_Request.DiagnosticsProvenance
                    });
            }

            AnimationClipSample ToAnimationClipSample(TimelineAnimationContribution contribution)
            {
                return new AnimationClipSample(
                    contribution.ClipAuthoringId,
                    ResolveAnimationSourceHandle(contribution.TrackIndex, contribution.ClipIndex),
                    contribution.Clip,
                    contribution.ClipTime,
                    contribution.NormalizedTime,
                    contribution.Weight,
                    contribution.IsLooping,
                    contribution.ClipLoopStartTime,
                    contribution.ClipLoopDuration,
                    contribution.ContinuousClipTime);
            }

            void CollectAnimationPlaybacks()
            {
                for (int trackIndex = 0; trackIndex < m_Timeline.Tracks.Count; trackIndex++)
                {
                    if (m_Timeline.Tracks[trackIndex] is not AnimationTrack track)
                        continue;
                    var producerId = new AnimationProducerId(m_Timeline.AuthoringId, track.AuthoringId);
                    m_AnimationPlaybacks.Add(new AnimationTrackPlayback(
                        trackIndex,
                        track,
                        new AnimationPlaybackId(producerId, Handle.Value),
                        track.LayerId));
                }
            }

            bool ContainsPlayback(AnimationPlaybackId playbackId)
            {
                for (int i = 0; i < m_AnimationPlaybacks.Count; i++)
                {
                    if (m_AnimationPlaybacks[i].PlaybackId.Equals(playbackId))
                        return true;
                }
                return false;
            }

            static bool ContainsPlayback(
                IReadOnlyCollection<AnimationPlaybackId> playbacks,
                AnimationPlaybackId playbackId)
            {
                foreach (AnimationPlaybackId value in playbacks)
                {
                    if (value.Equals(playbackId))
                        return true;
                }
                return false;
            }

            void DisposeGameplay()
            {
                if (m_GameplayDisposed)
                    return;
                m_GameplayDisposed = true;
                m_TreeRuntimes.Dispose();
            }

            RuntimeSourceElementHandle ResolveAnimationSourceHandle(int trackIndex, int clipIndex)
            {
                if (m_Diagnostics == null)
                    return RuntimeSourceElementHandle.Invalid;
                if (trackIndex < 0 || trackIndex >= m_Timeline.Tracks.Count)
                    throw new InvalidOperationException($"Animation contribution track index is outside Timeline '{m_Timeline.Name}'.");
                Track track = m_Timeline.Tracks[trackIndex];
                if (clipIndex < 0 || clipIndex >= track.Clips.Count)
                    throw new InvalidOperationException($"Animation contribution clip index is outside Track '{track.Name}'.");
                Clip clip = track.Clips[clipIndex];
                return m_Diagnostics.ResolveSourceHandle(RuntimeSourceElementKey.Clip(
                    m_Timeline.AuthoringId,
                    track.AuthoringId,
                    clip.AuthoringId,
                    clip is TreeClip));
            }

            readonly struct AnimationTrackPlayback
            {
                public AnimationTrackPlayback(
                    int trackIndex,
                    AnimationTrack track,
                    AnimationPlaybackId playbackId,
                    string layerId)
                {
                    TrackIndex = trackIndex;
                    Track = track;
                    PlaybackId = playbackId;
                    LayerId = layerId ?? string.Empty;
                }

                public int TrackIndex { get; }
                public AnimationTrack Track { get; }
                public AnimationPlaybackId PlaybackId { get; }
                public string LayerId { get; }
            }

            static bool TryAddActionCueSampleKey(TimelineActionCueSample sample, ref HashSet<string> keys)
            {
                keys ??= new HashSet<string>();
                return keys.Add($"{sample.SourceId}:{sample.TrackName}:{sample.CueId}:{sample.CueType}");
            }

            static void SubmitMotionCurve(
                CharacterGraphContext graphContext,
                TimelinePlaybackActionContext actionContext,
                GameplayLogicTickContext context,
                CharacterPipelineFrame frame,
                TimelineMotionCurveContribution contribution)
            {
                if (frame == null || !contribution.CanResolve)
                    return;

                frame.Output.StrictGameplay.MotionContributions.Add(MotionContribution.TimelineMotionCurve(
                    contribution.SourceId,
                    contribution.SourceName,
                    contribution.Displacement,
                    contribution.YawDegrees,
                    ToMotionSpace(contribution.Space),
                    contribution.Weight,
                    contribution.Priority,
                    ToMotionChannel(contribution.Channel),
                    ToMotionBlendMode(contribution.BlendMode),
                    contribution.ConsumeLowerChannels,
                    BuildMotionCurveDebugSource(actionContext, contribution)));

                SubmitActionMotion(graphContext, actionContext, context, ActionMotionSourceType.MotionCurve);
            }

            static string BuildMotionCurveDebugSource(TimelinePlaybackActionContext actionContext, TimelineMotionCurveContribution contribution)
            {
                string curveId = string.IsNullOrEmpty(contribution.CurveId) ? "MotionCurve" : contribution.CurveId;
                string motionCurveSource = $"MotionCurve:{curveId}:{contribution.SourceId}:{contribution.TrackName}";
                if (!actionContext.IsValid)
                    return motionCurveSource;

                return $"{actionContext.ActionInstanceId}:{motionCurveSource}";
            }

            static void SubmitMotionWarp(
                CharacterGraphContext graphContext,
                TimelinePlaybackActionContext actionContext,
                GameplayLogicTickContext context,
                CharacterPipelineFrame frame,
                TimelineMotionWarpWindow window)
            {
                if (frame == null)
                    return;

                frame.Output.StrictGameplay.MotionWarpWindows.Add(new MotionWarpWindow(
                    window.SourceId,
                    window.SourceName,
                    actionContext.ActionInstanceId,
                    window.TargetKey,
                    window.NormalizedTime,
                    window.Weight,
                    window.PositionWeight,
                    window.YawWeight,
                    window.MaxPositionCorrection,
                    window.MaxYawCorrectionDegrees,
                    BuildMotionWarpDebugSource(actionContext, window)));

                SubmitActionMotion(graphContext, actionContext, context, ActionMotionSourceType.MotionWarp);
            }

            static string BuildMotionWarpDebugSource(TimelinePlaybackActionContext actionContext, TimelineMotionWarpWindow window)
            {
                string motionWarpSource = $"MotionWarp:{window.SourceId}:{window.TrackName}:{window.TargetKey}";
                if (!actionContext.IsValid)
                    return motionWarpSource;

                return $"{actionContext.ActionInstanceId}:{motionWarpSource}";
            }

            static MotionContributionSpace ToMotionSpace(TimelineMotionContributionSpace space)
            {
                switch (space)
                {
                    case TimelineMotionContributionSpace.World:
                        return MotionContributionSpace.World;
                    default:
                        return MotionContributionSpace.Local;
                }
            }

            static MotionChannel ToMotionChannel(TimelineMotionChannel channel)
            {
                switch (channel)
                {
                    case TimelineMotionChannel.Locomotion:
                        return MotionChannel.Locomotion;
                    case TimelineMotionChannel.GameplayResult:
                        return MotionChannel.GameplayResult;
                    default:
                        return MotionChannel.Action;
                }
            }

            static MotionBlendMode ToMotionBlendMode(TimelineMotionBlendMode blendMode)
            {
                switch (blendMode)
                {
                    case TimelineMotionBlendMode.Additive:
                        return MotionBlendMode.Additive;
                    case TimelineMotionBlendMode.WeightedBlend:
                        return MotionBlendMode.WeightedBlend;
                    default:
                        return MotionBlendMode.Override;
                }
            }

            static void SubmitActionMotion(
                CharacterGraphContext graphContext,
                TimelinePlaybackActionContext actionContext,
                GameplayLogicTickContext context,
                ActionMotionSourceType sourceType)
            {
                if (graphContext == null || !actionContext.IsValid)
                    return;

                graphContext.SubmitActionMotionSample(new ActionMotionSample(
                    actionContext.ActionInstanceId,
                    actionContext.InputSequence,
                    context.LocalLogicTick,
                    sourceType));
            }

            static void SubmitActionCue(
                CharacterGraphContext graphContext,
                TimelinePlaybackActionContext actionContext,
                TimelineActionCueSample sample,
                ulong localLogicTick)
            {
                if (graphContext == null || !actionContext.IsValid)
                    return;

                graphContext.SubmitGameplayCue(new GameplayCueFact(
                    actionContext.ActionId,
                    sample.CueId,
                    sample.CueType,
                    actionContext.ActionInstanceId,
                    default,
                    default,
                    default,
                    localLogicTick));
            }

            static void SubmitCameraState(
                CharacterPipelineFrame frame,
                TimelinePlaybackActionContext actionContext,
                TimelineCameraStateSample sample)
            {
                if (frame == null)
                    return;

                frame.Output.Presentation.CameraStateRequests.Add(new CameraStateRequest(
                    ToCameraMode(sample.Mode),
                    sample.Priority,
                    sample.Weight,
                    sample.BlendInSeconds,
                    sample.BlendOutSeconds,
                    sample.TargetKey,
                    BuildCameraDebugSource(actionContext, sample.SourceId, sample.TrackName),
                    sample.TrackName,
                    actionContext.ActionInstanceId,
                    ToCameraInterruptPolicy(sample.InterruptPolicy)));
            }

            static void SubmitCameraCue(
                CharacterPipelineFrame frame,
                TimelinePlaybackActionContext actionContext,
                TimelineCameraCueSample sample)
            {
                if (frame == null)
                    return;

                frame.Output.Presentation.CameraCues.Add(new CameraCue(
                    sample.CueId,
                    ToCameraCueKind(sample.CueKind),
                    sample.CueType,
                    sample.Intensity,
                    sample.DurationSeconds,
                    sample.Priority,
                    BuildCameraDebugSource(actionContext, sample.SourceId, sample.TrackName),
                    sample.TrackName,
                    actionContext.ActionInstanceId));
            }

            static void SubmitCameraResponse(
                CharacterPipelineFrame frame,
                TimelinePlaybackActionContext actionContext,
                TimelineCameraResponseSample sample)
            {
                if (frame == null)
                    return;

                frame.Output.Presentation.CameraResponsePolicies.Add(new CameraResponsePolicy(
                    ToCameraLookResponseMode(sample.LookResponse),
                    sample.ManualOrbitWeight,
                    sample.PitchResponseWeight,
                    sample.YawResponseWeight,
                    sample.Priority,
                    sample.Weight,
                    BuildCameraDebugSource(actionContext, sample.SourceId, sample.TrackName),
                    actionContext.ActionInstanceId));
            }

            static string BuildCameraDebugSource(TimelinePlaybackActionContext actionContext, string sourceId, string trackName)
            {
                return actionContext.IsValid
                    ? $"{actionContext.ActionInstanceId}:{sourceId}:{trackName}"
                    : $"{sourceId}:{trackName}";
            }

            static CameraMode ToCameraMode(TimelineCameraMode mode)
            {
                switch (mode)
                {
                    case TimelineCameraMode.Aim:
                        return CameraMode.Aim;
                    case TimelineCameraMode.LockOn:
                        return CameraMode.LockOn;
                    case TimelineCameraMode.ActionFocus:
                        return CameraMode.ActionFocus;
                    case TimelineCameraMode.SkillCloseup:
                        return CameraMode.SkillCloseup;
                    default:
                        return CameraMode.FreeLook;
                }
            }

            static CameraInterruptPolicy ToCameraInterruptPolicy(TimelineCameraInterruptPolicy policy)
            {
                switch (policy)
                {
                    case TimelineCameraInterruptPolicy.Cut:
                        return CameraInterruptPolicy.Cut;
                    case TimelineCameraInterruptPolicy.HoldUntilSourceEnds:
                        return CameraInterruptPolicy.HoldUntilSourceEnds;
                    default:
                        return CameraInterruptPolicy.BlendOut;
                }
            }

            static CameraCueKind ToCameraCueKind(TimelineCameraCueKind cueKind)
            {
                switch (cueKind)
                {
                    case TimelineCameraCueKind.FovKick:
                        return CameraCueKind.FovKick;
                    case TimelineCameraCueKind.Recoil:
                        return CameraCueKind.Recoil;
                    case TimelineCameraCueKind.CollisionCorrection:
                        return CameraCueKind.CollisionCorrection;
                    case TimelineCameraCueKind.Custom:
                        return CameraCueKind.Custom;
                    default:
                        return CameraCueKind.Shake;
                }
            }

            static CameraLookResponseMode ToCameraLookResponseMode(TimelineCameraLookResponseMode mode)
            {
                switch (mode)
                {
                    case TimelineCameraLookResponseMode.Suppressed:
                        return CameraLookResponseMode.Suppressed;
                    case TimelineCameraLookResponseMode.Weighted:
                        return CameraLookResponseMode.Weighted;
                    default:
                        return CameraLookResponseMode.Full;
                }
            }
        }
    }
}
