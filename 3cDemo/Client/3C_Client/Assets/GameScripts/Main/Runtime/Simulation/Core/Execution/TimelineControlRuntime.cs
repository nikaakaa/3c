using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal sealed class TimelineControlRuntime<TOperationTarget, TTime>
        where TOperationTarget : struct, IOperationControlTarget<TOperationTarget>
        where TTime : struct
    {
        readonly ITimelineControlStatePort m_State;
        readonly ITimelineTargetLeaf<TTime> m_Target;
        readonly NestedExecutionWorkspaceBuffer<TimelineSegment<TTime>> m_Segments;

        public TimelineControlRuntime(
            ITimelineControlStatePort state,
            ITimelineTargetLeaf<TTime> target,
            NestedExecutionWorkspaceBuffer<TimelineSegment<TTime>> segments)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_Target = target ?? throw new ArgumentNullException(nameof(target));
            m_Segments = segments ?? throw new ArgumentNullException(nameof(segments));
        }

        public void PrepareDecisionTimelines(OperationControlCursor<TOperationTarget> cursor)
        {
            for (int i = 0; i < m_Target.TimelineOperationCount; i++)
            {
                OperationExecutionDescriptor timeline = m_Target.TimelineOperationAt(i);
                if (!cursor.IsRunning(timeline.Handle))
                    continue;
                if (m_State.ReadPlayback(timeline.Handle) != TimelinePlaybackStatus.Running)
                    continue;
                if (!IsTimelineActionContextCurrent(timeline.Handle))
                {
                    OperationStopStatus stop = ContinueTimelineStop(
                        cursor,
                        timeline.Handle,
                        OperationStopContext.ActionContextEnded(timeline.Handle));
                    if (stop == OperationStopStatus.Failed)
                    {
                        throw new InvalidOperationException(
                            $"Timeline '{m_Target.SourcePath(timeline.Handle)}' failed while stopping after its Action Context ended.");
                    }
                    continue;
                }
                TTime duration = m_Target.TimelineDuration(timeline.Handle);
                if (LessOrEqual(duration, m_Target.Zero))
                {
                    if (m_Target.IsLoop(timeline.Handle))
                        throw new InvalidOperationException($"Loop Timeline '{timeline.Text0}' has no positive duration.");
                    continue;
                }
                TTime previous = m_Target.ReadLogicTime(timeline.Handle);
                int cycle = m_State.ReadCycle(timeline.Handle);
                if (TryBuildSingleSegment(
                        previous,
                        cycle,
                        m_Target.TickDelta,
                        duration,
                        m_Target.IsLoop(timeline.Handle),
                        out TimelineSegment<TTime> segment,
                        out _,
                        out _))
                {
                    SampleDecisionTreeClips(cursor, timeline.Handle, segment);
                    continue;
                }
                List<TimelineSegment<TTime>> segments = m_Segments.Acquire();
                try
                {
                    BuildSegments(
                        previous,
                        cycle,
                        m_Target.TickDelta,
                        duration,
                        true,
                        segments,
                        out _,
                        out _);
                    for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
                        SampleDecisionTreeClips(cursor, timeline.Handle, segments[segmentIndex]);
                }
                finally
                {
                    m_Segments.Release(segments);
                }
            }
        }

        public OperationExecutionResult TickTimeline(
            OperationControlCursor<TOperationTarget> cursor,
            OperationHandle timeline)
        {
            OperationExecutionDescriptor operation = m_Target.Operation(timeline);
            TTime duration = m_Target.TimelineDuration(timeline);
            bool loop = m_Target.IsLoop(timeline);
            if (loop && LessOrEqual(duration, m_Target.Zero))
                throw new InvalidOperationException($"Loop Timeline '{operation.Text0}' has no positive duration.");

            TimelinePlaybackStatus playback = m_State.ReadPlayback(timeline);
            bool starting = playback == TimelinePlaybackStatus.Dormant;
            if (playback == TimelinePlaybackStatus.Succeeded || playback == TimelinePlaybackStatus.Cancelled)
                return OperationExecutionResult.Success;
            if (playback == TimelinePlaybackStatus.Completing)
                return ToOperationResult(ContinueTimelineCompletion(cursor, timeline));
            if (playback == TimelinePlaybackStatus.Stopping)
            {
                return ToOperationResult(ContinueTimelineStop(
                    cursor,
                    timeline,
                    OperationStopContext.ActionContextEnded(timeline)));
            }
            if (playback != TimelinePlaybackStatus.Dormant && playback != TimelinePlaybackStatus.Running)
            {
                throw new InvalidOperationException(
                    $"Timeline '{m_Target.SourcePath(timeline)}' has unknown playback status '{playback}'.");
            }
            if (starting && !CaptureTimelineActionContext(timeline))
            {
                if (m_Target.DiagnosticsEnabled)
                    Trace(timeline, "timeline_action_context_missing", TimelineTraceSeverity.Error, operation.Text0);
                return OperationExecutionResult.Failure;
            }
            if (!starting && !IsTimelineActionContextCurrent(timeline))
            {
                return ToOperationResult(ContinueTimelineStop(
                    cursor,
                    timeline,
                    OperationStopContext.ActionContextEnded(timeline)));
            }

            m_State.WritePlayback(timeline, TimelinePlaybackStatus.Running);
            m_State.WriteLoop(timeline, loop);
            TTime previous = m_Target.ReadLogicTime(timeline);
            int cycle = m_State.ReadCycle(timeline);
            if (LessOrEqual(duration, m_Target.Zero))
                return BeginTimelineCompletion(cursor, timeline, m_Target.Zero);

            if (TryBuildSingleSegment(
                    previous,
                    cycle,
                    m_Target.TickDelta,
                    duration,
                    loop,
                    out TimelineSegment<TTime> segment,
                    out TTime current,
                    out int currentCycle))
            {
                SampleCommitSegment(cursor, timeline, segment, starting);
            }
            else
            {
                List<TimelineSegment<TTime>> segments = m_Segments.Acquire();
                try
                {
                    BuildSegments(
                        previous,
                        cycle,
                        m_Target.TickDelta,
                        duration,
                        loop,
                        segments,
                        out current,
                        out currentCycle);
                    for (int i = 0; i < segments.Count; i++)
                        SampleCommitSegment(cursor, timeline, segments[i], starting && i == 0);
                }
                finally
                {
                    m_Segments.Release(segments);
                }
            }
            m_Target.WriteLogicTime(timeline, current);
            m_State.WriteCycle(timeline, currentCycle);
            if (m_Target.DiagnosticsEnabled)
                Trace(timeline, "timeline_logic_time", TimelineTraceSeverity.Detail, $"{m_Target.Format(current)}@{currentCycle}");

            if (!loop && GreaterOrEqual(current, duration))
                return BeginTimelineCompletion(cursor, timeline, duration);
            return OperationExecutionResult.Running;
        }

        public OperationStopStatus ContinueTimelineStop(
            OperationControlCursor<TOperationTarget> cursor,
            OperationHandle timeline,
            OperationStopContext context)
        {
            TimelinePlaybackStatus playback = m_State.ReadPlayback(timeline);
            if (playback != TimelinePlaybackStatus.Stopping)
            {
                m_State.WritePlayback(timeline, TimelinePlaybackStatus.Stopping);
                if (context.Cause == OperationStopCause.ActionContextEnded && m_Target.DiagnosticsEnabled)
                    Trace(timeline, "timeline_action_context_ended", TimelineTraceSeverity.Information, m_Target.Operation(timeline).Text0);
                EmitTimelineAnimationTerminal(timeline, TimelinePresentationOutputKind.ReleaseProducer, m_Target.Zero);
                EmitTimelineCameraTerminal(timeline, m_Target.Zero);
            }
            OperationStopStatus result = ContinueTimelineTreeStops(cursor, timeline, context);
            if (result != OperationStopStatus.Completed)
                return result;
            m_State.WritePlayback(timeline, TimelinePlaybackStatus.Cancelled);
            if (m_Target.DiagnosticsEnabled)
                Trace(timeline, "timeline_stopped", TimelineTraceSeverity.Information, m_Target.Operation(timeline).Text0);
            return OperationStopStatus.Completed;
        }

        public void ForceStopTimeline(
            OperationControlCursor<TOperationTarget> cursor,
            OperationHandle timeline,
            OperationStopContext context)
        {
            TimelinePlaybackStatus playback = m_State.TryReadPlayback(timeline, out TimelinePlaybackStatus value)
                ? value
                : TimelinePlaybackStatus.Dormant;
            if (playback != TimelinePlaybackStatus.Dormant &&
                playback != TimelinePlaybackStatus.Cancelled &&
                playback != TimelinePlaybackStatus.Stopping)
            {
                EmitTimelineAnimationTerminal(timeline, TimelinePresentationOutputKind.ReleaseProducer, m_Target.Zero);
                EmitTimelineCameraTerminal(timeline, m_Target.Zero);
            }
            IReadOnlyList<ProgramControlFlowEdge> clips = m_Target.Edges(timeline, ProgramControlFlowKind.Child);
            for (int i = 0; i < clips.Count; i++)
            {
                OperationExecutionDescriptor clip = m_Target.Operation(clips[i].Target);
                if (clip.Code == SimulationOperationCode.TimelineTreeClip)
                    ForceStopTreeClip(cursor, clip.Handle, context);
            }
        }

        OperationExecutionResult BeginTimelineCompletion(
            OperationControlCursor<TOperationTarget> cursor,
            OperationHandle timeline,
            TTime time)
        {
            m_State.WritePlayback(timeline, TimelinePlaybackStatus.Completing);
            EmitTimelineAnimationTerminal(timeline, TimelinePresentationOutputKind.CompleteProducer, time);
            EmitTimelineCameraTerminal(timeline, time);
            return ToOperationResult(ContinueTimelineCompletion(cursor, timeline));
        }

        OperationStopStatus ContinueTimelineCompletion(
            OperationControlCursor<TOperationTarget> cursor,
            OperationHandle timeline)
        {
            OperationStopStatus status = ContinueTimelineTreeStops(
                cursor,
                timeline,
                OperationStopContext.ParentStop(timeline));
            if (status != OperationStopStatus.Completed)
                return status;
            m_State.WritePlayback(timeline, TimelinePlaybackStatus.Succeeded);
            if (m_Target.DiagnosticsEnabled)
                Trace(timeline, "timeline_completed", TimelineTraceSeverity.Information, m_Target.Operation(timeline).Text0);
            return OperationStopStatus.Completed;
        }

        OperationStopStatus ContinueTimelineTreeStops(
            OperationControlCursor<TOperationTarget> cursor,
            OperationHandle timeline,
            OperationStopContext context)
        {
            OperationStopStatus aggregate = OperationStopStatus.Completed;
            IReadOnlyList<ProgramControlFlowEdge> clips = m_Target.Edges(timeline, ProgramControlFlowKind.Child);
            for (int i = 0; i < clips.Count; i++)
            {
                OperationExecutionDescriptor clip = m_Target.Operation(clips[i].Target);
                if (clip.Code != SimulationOperationCode.TimelineTreeClip)
                    continue;
                OperationStopStatus status = RequestTreeClipStop(cursor, clip.Handle, context);
                if (status == OperationStopStatus.Failed)
                    return status;
                if (status == OperationStopStatus.Running)
                    aggregate = status;
            }
            return aggregate;
        }

        void SampleDecisionTreeClips(
            OperationControlCursor<TOperationTarget> cursor,
            OperationHandle timeline,
            TimelineSegment<TTime> segment)
        {
            IReadOnlyList<ProgramControlFlowEdge> clips = m_Target.Edges(timeline, ProgramControlFlowKind.Child);
            for (int i = 0; i < clips.Count; i++)
            {
                OperationExecutionDescriptor clip = m_Target.Operation(clips[i].Target);
                if (clip.Code != SimulationOperationCode.TimelineTreeClip || clip.Integer0 != 0 || m_Target.IsTrackMuted(clip.Handle))
                    continue;
                TTime start = m_Target.ClipTime(clip.Handle, TimelineClipTimePoint.Start);
                TTime end = m_Target.ClipTime(clip.Handle, TimelineClipTimePoint.End);
                if (Less(segment.Current, start) || GreaterOrEqual(segment.Previous, end))
                    continue;
                TTime sample = m_Target.Clamp(segment.Current, start, end);
                m_Target.WriteLogicTime(clip.Handle, sample);
                m_State.WriteCycle(clip.Handle, segment.Cycle);
                ProgramControlFlowEdge root = m_Target.TreeClipEdge(clip.Handle, TimelineTreeClipEdgeKind.Root);
                using (m_Target.PushTimelineContext(
                    timeline,
                    clip.Handle,
                    segment.Cycle,
                    m_State.ReadRetainedActionContext(timeline)))
                {
                    OperationExecutionResult result = cursor.Tick(root.Target);
                    if (result == OperationExecutionResult.Running)
                        throw new InvalidOperationException($"Decision TreeClip '{m_Target.SourcePath(clip.Handle)}' returned Running.");
                    cursor.ForceStop(root.Target, OperationStopContext.Reset(root.Target));
                    if (m_Target.DiagnosticsEnabled)
                        Trace(clip.Handle, "tree_clip_decision", TimelineTraceSeverity.Detail, $"{m_Target.Format(sample)}@{segment.Cycle}:{result}");
                }
            }
        }

        void SampleCommitSegment(
            OperationControlCursor<TOperationTarget> cursor,
            OperationHandle timeline,
            TimelineSegment<TTime> segment,
            bool selectAnimationProducers)
        {
            SampleAnimationProducers(timeline, segment, selectAnimationProducers);
            IReadOnlyList<ProgramControlFlowEdge> clips = m_Target.Edges(timeline, ProgramControlFlowKind.Child);
            for (int i = 0; i < clips.Count; i++)
            {
                OperationExecutionDescriptor clip = m_Target.Operation(clips[i].Target);
                if (m_Target.IsTrackMuted(clip.Handle))
                {
                    if (clip.Code == SimulationOperationCode.TimelineTreeClip)
                    {
                        OperationStopStatus stop = RequestTreeClipStop(
                            cursor,
                            clip.Handle,
                            OperationStopContext.ParentStop(clip.Handle));
                        if (stop == OperationStopStatus.Failed)
                            throw new InvalidOperationException($"Muted TreeClip '{m_Target.SourcePath(clip.Handle)}' failed while stopping.");
                    }
                    continue;
                }
                switch (clip.Code)
                {
                    case SimulationOperationCode.TimelineMotionCurve:
                        m_Target.SampleMotionCurve(clip.Handle, segment);
                        break;
                    case SimulationOperationCode.TimelineMotionWarp:
                        m_Target.SampleMotionWarp(
                            clip.Handle,
                            segment,
                            m_State.ReadRetainedActionContext(timeline));
                        break;
                    case SimulationOperationCode.TimelineTreeClip when clip.Integer0 == 1:
                        SampleCommitTreeClip(cursor, timeline, clip.Handle, segment);
                        break;
                    case SimulationOperationCode.TimelineCue:
                        SampleCue(clip.Handle, segment);
                        break;
                    case SimulationOperationCode.TimelineCameraState:
                    case SimulationOperationCode.TimelineCameraResponse:
                        SampleCameraContinuous(clip.Handle, segment);
                        break;
                    case SimulationOperationCode.TimelineCameraCue:
                        SampleCameraCue(clip.Handle, segment);
                        break;
                }
            }
        }

        void SampleAnimationProducers(
            OperationHandle timeline,
            TimelineSegment<TTime> segment,
            bool select)
        {
            IReadOnlyList<OperationHandle> representatives = m_Target.AnimationProducerRepresentatives(timeline);
            if (representatives.Count == 0)
                return;
            ulong generation = m_Target.ReadActivationGeneration(timeline);
            TimelineActionContextIdentity actionContext = m_State.ReadRetainedActionContext(timeline);
            if (!actionContext.IsValid)
                throw new InvalidOperationException(
                    $"Animation Timeline '{m_Target.SourcePath(timeline)}' has no retained Action context.");
            TTime visualTimeScale = m_Target.Divide(
                m_Target.Subtract(segment.Current, segment.Previous),
                m_Target.TickDelta);
            for (int i = 0; i < representatives.Count; i++)
            {
                OperationHandle representative = representatives[i];
                if (select)
                {
                    EmitPresentation(
                        representative,
                        TimelinePresentationOutputKind.SelectProducer,
                        segment.Current,
                        m_Target.One,
                        generation,
                        segment.Cycle,
                        actionContext.InstanceId,
                        visualTimeScale);
                }
                EmitPresentation(
                    representative,
                    TimelinePresentationOutputKind.SampleProducer,
                    segment.Current,
                    m_Target.One,
                    generation,
                    segment.Cycle,
                    actionContext.InstanceId,
                    visualTimeScale);
            }
        }

        void SampleCommitTreeClip(
            OperationControlCursor<TOperationTarget> cursor,
            OperationHandle timeline,
            OperationHandle clip,
            TimelineSegment<TTime> segment)
        {
            using (m_Target.PushTimelineContext(
                timeline,
                clip,
                segment.Cycle,
                m_State.ReadRetainedActionContext(timeline)))
                SampleCommitTreeClipCore(cursor, clip, segment);
        }

        void SampleCommitTreeClipCore(
            OperationControlCursor<TOperationTarget> cursor,
            OperationHandle clip,
            TimelineSegment<TTime> segment)
        {
            TTime start = m_Target.ClipTime(clip, TimelineClipTimePoint.Start);
            TTime end = m_Target.ClipTime(clip, TimelineClipTimePoint.End);
            TimelineTreeClipStatus playback = m_State.ReadTreeClipStatus(clip);
            if (playback >= TimelineTreeClipStatus.Disabling)
            {
                OperationStopStatus pending = RequestTreeClipStop(cursor, clip, OperationStopContext.ParentStop(clip));
                if (pending == OperationStopStatus.Failed)
                    throw new InvalidOperationException($"Commit TreeClip '{m_Target.SourcePath(clip)}' failed while stopping.");
                return;
            }
            bool currentActive = LessOrEqual(start, segment.Current) && LessOrEqual(segment.Current, end);
            if (playback != TimelineTreeClipStatus.Dormant &&
                (m_State.ReadCycle(clip) != segment.Cycle || !currentActive))
            {
                OperationStopStatus stop = RequestTreeClipStop(cursor, clip, OperationStopContext.ParentStop(clip));
                if (stop == OperationStopStatus.Failed)
                    throw new InvalidOperationException($"Commit TreeClip '{m_Target.SourcePath(clip)}' failed at its segment boundary.");
                return;
            }
            bool crossed = LessOrEqual(segment.Previous, start) && GreaterOrEqual(segment.Current, end);
            bool touches = currentActive || crossed || Less(segment.Previous, end) && Greater(segment.Current, start);
            if (!touches)
                return;
            if (playback == TimelineTreeClipStatus.Dormant)
            {
                m_State.WriteTreeClipStatus(clip, TimelineTreeClipStatus.Enabling);
                m_State.WriteCycle(clip, segment.Cycle);
                playback = TimelineTreeClipStatus.Enabling;
            }
            if (playback == TimelineTreeClipStatus.Enabling)
            {
                OperationExecutionResult enable = TickTreeClipLifecycle(
                    cursor,
                    clip,
                    TimelineTreeClipEdgeKind.Enable);
                if (enable == OperationExecutionResult.Running)
                    return;
                if (enable == OperationExecutionResult.Failure)
                    throw new InvalidOperationException($"Commit TreeClip '{m_Target.SourcePath(clip)}' OnEnable failed.");
                m_State.WriteTreeClipStatus(clip, TimelineTreeClipStatus.Active);
                if (m_Target.DiagnosticsEnabled)
                    Trace(clip, "tree_clip_entered", TimelineTraceSeverity.Detail, segment.Cycle.ToString());
            }
            TTime sample = currentActive ? segment.Current : end;
            m_Target.WriteLogicTime(clip, sample);
            ProgramControlFlowEdge root = m_Target.TreeClipEdge(clip, TimelineTreeClipEdgeKind.Root);
            OperationExecutionResult rootResult = cursor.TickPersistent(root.Target);
            if (rootResult == OperationExecutionResult.Failure)
                throw new InvalidOperationException($"Commit TreeClip '{m_Target.SourcePath(clip)}' root failed.");
            if (m_Target.DiagnosticsEnabled)
                Trace(clip, "tree_clip_updated", TimelineTraceSeverity.Detail, $"{m_Target.Format(sample)}@{segment.Cycle}");
            if (!currentActive || crossed)
            {
                OperationStopStatus stop = RequestTreeClipStop(cursor, clip, OperationStopContext.ParentStop(clip));
                if (stop == OperationStopStatus.Failed)
                    throw new InvalidOperationException($"Commit TreeClip '{m_Target.SourcePath(clip)}' failed after its final sample.");
            }
        }

        OperationStopStatus RequestTreeClipStop(
            OperationControlCursor<TOperationTarget> cursor,
            OperationHandle clip,
            OperationStopContext context)
        {
            OperationHandle timeline = RequireTimelineOwner(clip);
            int cycle = m_State.TryReadCycle(clip, out int value) ? value : 0;
            using (m_Target.PushTimelineContext(
                timeline,
                clip,
                cycle,
                m_State.ReadRetainedActionContext(timeline)))
                return ContinueTreeClipStop(cursor, clip, context);
        }

        OperationStopStatus ContinueTreeClipStop(
            OperationControlCursor<TOperationTarget> cursor,
            OperationHandle clip,
            OperationStopContext context)
        {
            if (!m_State.TryReadTreeClipStatus(clip, out TimelineTreeClipStatus playback))
                return OperationStopStatus.Completed;
            if (playback == TimelineTreeClipStatus.Dormant)
                return OperationStopStatus.Completed;
            if (playback == TimelineTreeClipStatus.Enabling)
            {
                ProgramControlFlowEdge enable = m_Target.TreeClipEdge(clip, TimelineTreeClipEdgeKind.Enable);
                if (cursor.IsActive(enable.Target))
                {
                    OperationStopStatus stopEnable = cursor.RequestStop(enable.Target, context);
                    if (stopEnable != OperationStopStatus.Completed)
                        return stopEnable;
                }
                playback = TimelineTreeClipStatus.Disabling;
                m_State.WriteTreeClipStatus(clip, playback);
            }
            if (playback == TimelineTreeClipStatus.Active)
            {
                playback = TimelineTreeClipStatus.Disabling;
                m_State.WriteTreeClipStatus(clip, playback);
            }
            if (playback == TimelineTreeClipStatus.Disabling)
            {
                OperationExecutionResult disable = TickTreeClipLifecycle(
                    cursor,
                    clip,
                    TimelineTreeClipEdgeKind.Disable);
                if (disable == OperationExecutionResult.Running)
                    return OperationStopStatus.Running;
                if (disable == OperationExecutionResult.Failure)
                    return OperationStopStatus.Failed;
                playback = TimelineTreeClipStatus.StoppingRoot;
                m_State.WriteTreeClipStatus(clip, playback);
            }
            ProgramControlFlowEdge root = m_Target.TreeClipEdge(clip, TimelineTreeClipEdgeKind.Root);
            if (playback == TimelineTreeClipStatus.StoppingRoot)
            {
                OperationStopStatus rootStop = cursor.RequestStop(root.Target, context);
                if (rootStop != OperationStopStatus.Completed)
                    return rootStop;
                playback = TimelineTreeClipStatus.Destroying;
                m_State.WriteTreeClipStatus(clip, playback);
            }
            OperationExecutionResult destroy = TickTreeClipLifecycle(
                cursor,
                clip,
                TimelineTreeClipEdgeKind.Destroy);
            if (destroy == OperationExecutionResult.Running)
                return OperationStopStatus.Running;
            if (destroy == OperationExecutionResult.Failure)
                return OperationStopStatus.Failed;
            m_Target.ResetTreeClipState(clip);
            if (m_Target.DiagnosticsEnabled)
                Trace(clip, "tree_clip_exited", TimelineTraceSeverity.Detail, string.Empty);
            return OperationStopStatus.Completed;
        }

        OperationExecutionResult TickTreeClipLifecycle(
            OperationControlCursor<TOperationTarget> cursor,
            OperationHandle clip,
            TimelineTreeClipEdgeKind kind)
        {
            ProgramControlFlowEdge edge = m_Target.TreeClipEdge(clip, kind);
            OperationExecutionResult result = cursor.Tick(edge.Target);
            if (result != OperationExecutionResult.Running)
                cursor.ForceStop(edge.Target, OperationStopContext.Reset(edge.Target));
            return result;
        }

        void ForceStopTreeClip(
            OperationControlCursor<TOperationTarget> cursor,
            OperationHandle clip,
            OperationStopContext context)
        {
            IReadOnlyList<ProgramControlFlowEdge> entries = m_Target.Edges(clip, ProgramControlFlowKind.Enter);
            for (int i = 0; i < entries.Count; i++)
                cursor.ForceStop(entries[i].Target, context);
            IReadOnlyList<ProgramControlFlowEdge> exits = m_Target.Edges(clip, ProgramControlFlowKind.Exit);
            for (int i = 0; i < exits.Count; i++)
                cursor.ForceStop(exits[i].Target, context);
            m_Target.ResetTreeClipState(clip);
        }

        void SampleCue(OperationHandle clip, TimelineSegment<TTime> segment)
        {
            TTime start = m_Target.ClipTime(clip, TimelineClipTimePoint.Start);
            if (!Crosses(segment, start) && !(segment.StartsCycle && Equal(start, m_Target.Zero)))
                return;
            m_Target.EmitCue(new TimelineCueOutput<TTime>(clip, start, segment.Cycle));
        }

        void SampleCameraContinuous(OperationHandle clip, TimelineSegment<TTime> segment)
        {
            TTime start = m_Target.ClipTime(clip, TimelineClipTimePoint.Start);
            TTime end = m_Target.ClipTime(clip, TimelineClipTimePoint.End);
            if (Less(segment.Current, start) || Greater(segment.Previous, end))
                return;
            TTime duration = m_Target.Max(m_Target.Epsilon, m_Target.Subtract(end, start));
            TTime sample = m_Target.Clamp(segment.Current, start, end);
            TTime self = m_Target.Clamp(m_Target.Subtract(sample, start), m_Target.Zero, duration);
            TTime weight = Greater(segment.Current, end)
                ? m_Target.Zero
                : SampleClipWeight(
                    clip,
                    m_Target.Clamp(m_Target.Divide(self, duration), m_Target.Zero, m_Target.One),
                    self,
                    m_Target.Max(m_Target.Zero, m_Target.Subtract(end, sample)));
            EmitPresentation(clip, TimelinePresentationOutputKind.Camera, sample, weight, 0, segment.Cycle);
        }

        void SampleCameraCue(OperationHandle clip, TimelineSegment<TTime> segment)
        {
            TTime start = m_Target.ClipTime(clip, TimelineClipTimePoint.Start);
            if (!Crosses(segment, start) && !(segment.StartsCycle && Equal(start, m_Target.Zero)))
                return;
            EmitPresentation(
                clip,
                TimelinePresentationOutputKind.Camera,
                start,
                m_Target.ClipScalar(clip, TimelineClipScalarValue.Intensity),
                0,
                segment.Cycle);
        }

        void EmitTimelineAnimationTerminal(
            OperationHandle timeline,
            TimelinePresentationOutputKind kind,
            TTime time)
        {
            IReadOnlyList<OperationHandle> representatives = m_Target.AnimationProducerRepresentatives(timeline);
            if (representatives.Count == 0)
                return;
            int cycle = m_State.TryReadCycle(timeline, out int value) ? value : 0;
            ulong generation = m_Target.ReadActivationGeneration(timeline);
            TimelineActionContextIdentity actionContext = m_State.ReadRetainedActionContext(timeline);
            if (!actionContext.IsValid)
                throw new InvalidOperationException(
                    $"Animation Timeline '{m_Target.SourcePath(timeline)}' has no retained Action context.");
            for (int i = 0; i < representatives.Count; i++)
                EmitPresentation(
                    representatives[i],
                    kind,
                    time,
                    m_Target.Zero,
                    generation,
                    cycle,
                    actionContext.InstanceId,
                    m_Target.Zero);
        }

        void EmitTimelineCameraTerminal(OperationHandle timeline, TTime time)
        {
            IReadOnlyList<ProgramControlFlowEdge> clips = m_Target.Edges(timeline, ProgramControlFlowKind.Child);
            for (int i = 0; i < clips.Count; i++)
            {
                OperationExecutionDescriptor clip = m_Target.Operation(clips[i].Target);
                if ((clip.Code == SimulationOperationCode.TimelineCameraState ||
                     clip.Code == SimulationOperationCode.TimelineCameraResponse) &&
                    !m_Target.IsTrackMuted(clip.Handle))
                {
                    EmitPresentation(
                        clip.Handle,
                        TimelinePresentationOutputKind.Camera,
                        time,
                        m_Target.Zero,
                        0,
                        0,
                        0,
                        m_Target.Zero);
                }
            }
        }

        void EmitPresentation(
            OperationHandle operation,
            TimelinePresentationOutputKind kind,
            TTime time,
            TTime weight,
            ulong generation,
            int cycle,
            ulong sourceActionInstanceId = 0,
            TTime visualTimeScale = default)
        {
            m_Target.EmitPresentation(new TimelinePresentationOutput<TTime>(
                operation,
                kind,
                time,
                weight,
                generation,
                cycle,
                sourceActionInstanceId,
                visualTimeScale));
        }

        bool CaptureTimelineActionContext(OperationHandle timeline)
        {
            if (!m_Target.TryCaptureActionContext(timeline, out TimelineActionContextIdentity identity))
                return false;
            m_State.WriteRetainedActionContext(timeline, identity);
            return true;
        }

        bool IsTimelineActionContextCurrent(OperationHandle timeline)
        {
            return m_Target.IsActionContextCurrent(
                timeline,
                m_State.ReadRetainedActionContext(timeline));
        }

        OperationHandle RequireTimelineOwner(OperationHandle clip)
        {
            return m_Target.TimelineOwner(clip);
        }

        bool TryBuildSingleSegment(
            TTime previous,
            int cycle,
            TTime delta,
            TTime duration,
            bool loop,
            out TimelineSegment<TTime> segment,
            out TTime current,
            out int currentCycle)
        {
            TTime remaining = m_Target.Max(m_Target.Zero, delta);
            if (!loop)
            {
                current = m_Target.Min(m_Target.Add(previous, remaining), duration);
                currentCycle = 0;
                segment = new TimelineSegment<TTime>(previous, current, 0, Equal(previous, m_Target.Zero));
                return true;
            }
            if (Less(remaining, m_Target.Subtract(duration, previous)))
            {
                current = m_Target.Add(previous, remaining);
                currentCycle = cycle;
                segment = new TimelineSegment<TTime>(previous, current, cycle, Equal(previous, m_Target.Zero));
                return true;
            }
            segment = default;
            current = default;
            currentCycle = default;
            return false;
        }

        void BuildSegments(
            TTime previous,
            int cycle,
            TTime delta,
            TTime duration,
            bool loop,
            List<TimelineSegment<TTime>> result,
            out TTime current,
            out int currentCycle)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            result.Clear();
            if (!loop)
            {
                current = m_Target.Min(
                    m_Target.Add(previous, m_Target.Max(m_Target.Zero, delta)),
                    duration);
                currentCycle = 0;
                result.Add(new TimelineSegment<TTime>(previous, current, 0, Equal(previous, m_Target.Zero)));
                return;
            }
            TTime remaining = m_Target.Max(m_Target.Zero, delta);
            TTime cursor = previous;
            int cursorCycle = cycle;
            int guard = 0;
            while (GreaterOrEqual(remaining, m_Target.Subtract(duration, cursor)))
            {
                TTime available = m_Target.Subtract(duration, cursor);
                result.Add(new TimelineSegment<TTime>(cursor, duration, cursorCycle, Equal(cursor, m_Target.Zero)));
                remaining = m_Target.Subtract(remaining, available);
                cursor = m_Target.Zero;
                cursorCycle = checked(cursorCycle + 1);
                guard++;
                if (guard > 4096)
                    throw new InvalidOperationException("Timeline exceeded 4096 loop segments in one Simulation Tick.");
            }
            current = m_Target.Add(cursor, remaining);
            currentCycle = cursorCycle;
            result.Add(new TimelineSegment<TTime>(cursor, current, cursorCycle, Equal(cursor, m_Target.Zero)));
        }

        TTime SampleClipWeight(
            OperationHandle clip,
            TTime normalized,
            TTime selfTime,
            TTime remainTime)
        {
            TTime easeIn = m_Target.ClipTime(clip, TimelineClipTimePoint.EaseIn);
            TTime easeOut = m_Target.ClipTime(clip, TimelineClipTimePoint.EaseOut);
            TTime fadeIn = m_Target.One;
            if (Greater(easeIn, m_Target.Zero) && Less(selfTime, easeIn))
            {
                fadeIn = m_Target.SampleCurve(
                    clip,
                    TimelineCurveChannel.EaseIn,
                    m_Target.Divide(selfTime, easeIn),
                    m_Target.One);
            }
            TTime fadeOut = m_Target.One;
            if (Greater(easeOut, m_Target.Zero) && Less(remainTime, easeOut))
            {
                fadeOut = m_Target.Subtract(
                    m_Target.One,
                    m_Target.SampleCurve(
                        clip,
                        TimelineCurveChannel.EaseOut,
                        m_Target.Subtract(m_Target.One, m_Target.Divide(remainTime, easeOut)),
                        m_Target.Zero));
            }
            return m_Target.Clamp(
                m_Target.Multiply(
                    m_Target.Multiply(
                        m_Target.SampleCurve(clip, TimelineCurveChannel.Weight, normalized, m_Target.One),
                        fadeIn),
                    fadeOut),
                m_Target.Zero,
                m_Target.One);
        }

        void Trace(
            OperationHandle operation,
            string code,
            TimelineTraceSeverity severity,
            string detail)
        {
            m_Target.EmitTrace(new TimelineTraceOutput(operation, code, severity, detail));
        }

        bool Crosses(TimelineSegment<TTime> segment, TTime time) =>
            Less(segment.Previous, time) && LessOrEqual(time, segment.Current);

        bool Equal(TTime left, TTime right) => m_Target.Compare(left, right) == 0;
        bool Less(TTime left, TTime right) => m_Target.Compare(left, right) < 0;
        bool LessOrEqual(TTime left, TTime right) => m_Target.Compare(left, right) <= 0;
        bool Greater(TTime left, TTime right) => m_Target.Compare(left, right) > 0;
        bool GreaterOrEqual(TTime left, TTime right) => m_Target.Compare(left, right) >= 0;

        static OperationExecutionResult ToOperationResult(OperationStopStatus status)
        {
            return status == OperationStopStatus.Running
                ? OperationExecutionResult.Running
                : status == OperationStopStatus.Completed
                    ? OperationExecutionResult.Success
                    : OperationExecutionResult.Failure;
        }
    }
}
