using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Graph;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Logic
{
    sealed class TimelineTreeRuntimeSet : IDisposable
    {
        readonly TimelineData m_Timeline;
        readonly CharacterGraphContext m_GraphContext;
        readonly TimelinePlaybackHandle m_Handle;
        readonly TimelinePlaybackActionContext m_ActionContext;
        readonly TreeExecutionActivationScope m_SourceActivation;
        readonly BaseGraph m_SourceRuntimeGraph;
        readonly List<TreeClipEntry> m_Entries = new List<TreeClipEntry>();
        readonly List<TreeRuntime> m_StoppingRuntimes = new List<TreeRuntime>();
        readonly HashSet<DecisionEvaluationIdentity> m_DecisionEvaluations = new HashSet<DecisionEvaluationIdentity>();

        bool m_Disposed;

        public TimelineTreeRuntimeSet(
            TimelineData timeline,
            CharacterGraphContext graphContext,
            TimelinePlaybackHandle handle,
            TimelinePlaybackActionContext actionContext,
            TreeExecutionActivationScope sourceActivation,
            BaseGraph sourceRuntimeGraph)
        {
            m_Timeline = timeline;
            m_GraphContext = graphContext;
            m_Handle = handle;
            m_ActionContext = actionContext;
            m_SourceActivation = sourceActivation;
            m_SourceRuntimeGraph = sourceRuntimeGraph;
            CollectEntries();
        }

        public bool HasNaturalStoppingRuntimes => m_StoppingRuntimes.Count > 0;

        public void BeginDecisionTick()
        {
            m_DecisionEvaluations.Clear();
        }

        public bool EvaluateDecisionSegment(
            float previousTime,
            float currentTime,
            int cycle,
            float deltaTime,
            ulong localLogicTick)
        {
            for (int i = 0; i < m_Entries.Count; i++)
            {
                TreeClipEntry entry = m_Entries[i];
                if (entry.Clip.ExecutionPhase != TimelineTreeExecutionPhase.Decision ||
                    entry.Track.PersistentMuted ||
                    !TryResolveDecisionSampleTime(entry.Clip, previousTime, currentTime, out float sampleTime))
                    continue;

                var identity = new DecisionEvaluationIdentity(entry.TrackIndex, entry.ClipIndex, cycle);
                if (!m_DecisionEvaluations.Add(identity))
                    continue;

                if (entry.DecisionRuntime == null || entry.DecisionRuntime.Cycle != cycle)
                {
                    entry.DecisionRuntime?.DisposeDormant();
                    entry.DecisionRuntime = CreateRuntime(entry, cycle, true);
                }

                if (entry.DecisionRuntime == null || !entry.DecisionRuntime.Valid)
                    return false;

                if (!entry.DecisionRuntime.EvaluateDecision(sampleTime, deltaTime, localLogicTick))
                    return false;
            }

            return true;
        }

        public bool CommitSegment(float previousTime, float currentTime, int cycle, float deltaTime, ulong localLogicTick)
        {
            for (int i = 0; i < m_Entries.Count; i++)
            {
                TreeClipEntry entry = m_Entries[i];
                if (entry.Clip.ExecutionPhase != TimelineTreeExecutionPhase.Commit)
                    continue;

                if (entry.Track.PersistentMuted)
                {
                    StopActiveRuntime(entry, localLogicTick);
                    continue;
                }

                bool currentActive = Contains(entry.Clip, currentTime);
                if (entry.ActiveCommitRuntime != null && entry.ActiveCommitRuntime.Cycle != cycle)
                    StopActiveRuntime(entry, localLogicTick);
                else if (entry.ActiveCommitRuntime != null && !currentActive)
                {
                    StopActiveRuntime(entry, localLogicTick);
                    continue;
                }

                bool crossedClip = previousTime <= entry.Clip.StartTime && currentTime >= entry.Clip.EndTime;
                bool touchesClip = currentActive || crossedClip ||
                                   previousTime < entry.Clip.EndTime && currentTime > entry.Clip.StartTime;
                if (!touchesClip)
                    continue;

                if (entry.ActiveCommitRuntime == null)
                {
                    TreeRuntime runtime = CreateRuntime(entry, cycle, false);
                    if (runtime == null || !runtime.Valid)
                        return false;
                    entry.ActiveCommitRuntime = runtime;
                    runtime.Enter(crossedClip ? entry.Clip.StartTime : currentTime, deltaTime, localLogicTick);
                }

                float sampleTime = currentActive
                    ? currentTime
                    : Mathf.Clamp(entry.Clip.EndTime, entry.Clip.StartTime, entry.Clip.EndTime);
                if (!entry.ActiveCommitRuntime.Tick(sampleTime, deltaTime, localLogicTick))
                    return false;

                if (!currentActive || crossedClip)
                    StopActiveRuntime(entry, localLogicTick);
            }

            return true;
        }

        public bool BeginCommitTick(float deltaTime, ulong localLogicTick)
        {
            return UpdateStoppingRuntimes(deltaTime, localLogicTick);
        }

        public void EndLoopCycle(int cycle, ulong localLogicTick)
        {
            for (int i = 0; i < m_Entries.Count; i++)
            {
                TreeClipEntry entry = m_Entries[i];
                if (entry.ActiveCommitRuntime != null && entry.ActiveCommitRuntime.Cycle == cycle)
                    StopActiveRuntime(entry, localLogicTick);
            }
        }

        public void BeginTimelineCompletion(ulong localLogicTick)
        {
            for (int i = 0; i < m_Entries.Count; i++)
                StopActiveRuntime(m_Entries[i], localLogicTick);
        }

        public bool UpdateNaturalStopping(float deltaTime, ulong localLogicTick)
        {
            return UpdateStoppingRuntimes(deltaTime, localLogicTick);
        }

        public void ForceStopAll(NodeStopContext stopContext)
        {
            for (int i = 0; i < m_Entries.Count; i++)
            {
                TreeClipEntry entry = m_Entries[i];
                entry.DecisionRuntime?.DisposeDormant();
                entry.DecisionRuntime = null;
                entry.ActiveCommitRuntime?.ForceStop(stopContext);
                entry.ActiveCommitRuntime = null;
            }

            for (int i = 0; i < m_StoppingRuntimes.Count; i++)
                m_StoppingRuntimes[i].ForceStop(stopContext);
            m_StoppingRuntimes.Clear();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Disposed = true;
            var stopContext = NodeStopContext.Create(
                NodeStopOriginCause.Shutdown,
                m_GraphContext != null ? m_GraphContext.LocalLogicTick : 0,
                null);
            ForceStopAll(stopContext);
            m_Entries.Clear();
            m_DecisionEvaluations.Clear();
        }

        void CollectEntries()
        {
            if (m_Timeline == null)
                return;

            for (int trackIndex = 0; trackIndex < m_Timeline.Tracks.Count; trackIndex++)
            {
                if (!(m_Timeline.Tracks[trackIndex] is TreeTrack treeTrack))
                    continue;

                for (int clipIndex = 0; clipIndex < treeTrack.Clips.Count; clipIndex++)
                {
                    if (treeTrack.Clips[clipIndex] is TreeClip treeClip)
                        m_Entries.Add(new TreeClipEntry(treeTrack, treeClip, trackIndex, clipIndex));
                }
            }
        }

        TreeRuntime CreateRuntime(TreeClipEntry entry, int cycle, bool decision)
        {
            TimelineRunningTree template = entry.Clip.ResolvedTree;
            if (template == null)
            {
                Debug.LogError($"TreeClip is missing graph data: timeline={TimelineName()} track={entry.TrackIndex} clip={entry.ClipIndex}.");
                return null;
            }

            TimelineRunningTree tree = template.Clone();
            var context = new TimelineTreeClipRuntimeContext(
                entry.Clip,
                m_Handle.Value,
                entry.TrackIndex,
                entry.ClipIndex,
                cycle,
                m_ActionContext,
                m_SourceActivation,
                m_SourceRuntimeGraph);
            tree.name = $"{(decision ? "Decision" : "Commit")}TreeClip:{context.PlaybackIdentity}/{context.TrackIndex}/{context.ClipIndex}/{context.Cycle}";
            var runtime = new TreeRuntime(tree, context, m_GraphContext, decision, TimelineName());
            return runtime.Valid ? runtime : null;
        }

        void StopActiveRuntime(TreeClipEntry entry, ulong localLogicTick)
        {
            TreeRuntime runtime = entry.ActiveCommitRuntime;
            if (runtime == null)
                return;

            entry.ActiveCommitRuntime = null;
            if (runtime.BeginNaturalStop(localLogicTick) == NodeStopStatus.Running)
            {
                m_StoppingRuntimes.Add(runtime);
                Debug.LogWarning($"Timeline waits for TreeClip graceful stop: {runtime.DebugIdentity}.");
            }
        }

        bool UpdateStoppingRuntimes(float deltaTime, ulong localLogicTick)
        {
            for (int i = m_StoppingRuntimes.Count - 1; i >= 0; i--)
            {
                NodeStopStatus status = m_StoppingRuntimes[i].UpdateStopping(deltaTime, localLogicTick);
                if (status == NodeStopStatus.Running)
                    continue;

                if (status == NodeStopStatus.Failed)
                {
                    Debug.LogError($"TreeClip graceful stop failed: {m_StoppingRuntimes[i].DebugIdentity}.");
                    return false;
                }

                m_StoppingRuntimes[i].CompleteNaturalStop();
                m_StoppingRuntimes.RemoveAt(i);
            }

            return true;
        }

        string TimelineName()
        {
            return m_Timeline != null ? m_Timeline.Name : "<missing>";
        }

        static bool Contains(TreeClip clip, float timelineTime)
        {
            return clip != null && clip.StartTime <= timelineTime && timelineTime <= clip.EndTime;
        }

        static bool TryResolveDecisionSampleTime(
            TreeClip clip,
            float previousTime,
            float currentTime,
            out float sampleTime)
        {
            sampleTime = 0f;
            if (clip == null || currentTime < previousTime || currentTime < clip.StartTime || previousTime >= clip.EndTime)
                return false;

            sampleTime = Mathf.Clamp(currentTime, clip.StartTime, clip.EndTime);
            return true;
        }

        sealed class TreeClipEntry
        {
            public TreeClipEntry(TreeTrack track, TreeClip clip, int trackIndex, int clipIndex)
            {
                Track = track;
                Clip = clip;
                TrackIndex = trackIndex;
                ClipIndex = clipIndex;
            }

            public TreeTrack Track { get; }
            public TreeClip Clip { get; }
            public int TrackIndex { get; }
            public int ClipIndex { get; }
            public TreeRuntime DecisionRuntime { get; set; }
            public TreeRuntime ActiveCommitRuntime { get; set; }

        }

        sealed class TreeRuntime
        {
            readonly TimelineRunningTree m_Tree;
            readonly TimelineTreeClipRuntimeContext m_Context;
            readonly CharacterGraphContext m_GraphContext;
            readonly bool m_Decision;

            Guid m_RuntimeId;
            bool m_Disposed;
            bool m_Enabled;

            public TreeRuntime(
                TimelineRunningTree tree,
                TimelineTreeClipRuntimeContext context,
                CharacterGraphContext graphContext,
                bool decision,
                string timelineName)
            {
                m_Tree = tree;
                m_Context = context;
                m_GraphContext = graphContext;
                m_Decision = decision;
                try
                {
                    m_Tree.InitTimelineTree(m_GraphContext, m_Context);
                    m_RuntimeId = m_Tree.RuntimeId;
                    if (decision)
                    {
                        var errors = new List<string>();
                        if (!TimelineTreeDecisionValidation.Validate(m_Tree, errors))
                        {
                            for (int i = 0; i < errors.Count; i++)
                                Debug.LogError($"{timelineName}: {errors[i]}");
                            DisposeDormant();
                            return;
                        }
                    }

                    Valid = true;
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Failed to initialize TreeClip runtime: {exception.Message}");
                    DisposeDormant();
                }
            }

            public int Cycle => m_Context.Cycle;
            public bool Valid { get; private set; }
            public string DebugIdentity => $"playback={m_Context.PlaybackIdentity} track={m_Context.TrackIndex} clip={m_Context.ClipIndex} cycle={m_Context.Cycle}";

            public bool EvaluateDecision(float timelineTime, float deltaTime, ulong localLogicTick)
            {
                if (!Valid || !m_Decision)
                    return false;

                m_Context.Update(timelineTime, deltaTime, localLogicTick);
                Publish(RuntimeTraceEventKind.TreeClipEntered, "Decision", string.Empty);
                State state;
                PushInstance();
                try
                {
                    m_Tree.ResetTree();
                    state = m_Tree.UpdateTree(deltaTime);
                }
                finally
                {
                    PopInstance();
                }
                Publish(RuntimeTraceEventKind.TreeClipUpdated, state.ToString(), string.Empty);

                if (state == State.Running)
                {
                    Debug.LogError($"Decision TreeClip returned Running: {DebugIdentity}.");
                    PushInstance();
                    try
                    {
                        m_Tree.ForceStop(m_Context.CreateStopContext(NodeStopOriginCause.Reset));
                        m_Tree.ResetTree();
                    }
                    finally
                    {
                        PopInstance();
                    }
                    Publish(RuntimeTraceEventKind.TreeClipExited, "Failed", "DecisionReturnedRunning");
                    return false;
                }

                m_Tree.ResetTree();
                Publish(RuntimeTraceEventKind.TreeClipExited, state.ToString(), string.Empty);
                return true;
            }

            public void Enter(float timelineTime, float deltaTime, ulong localLogicTick)
            {
                m_Context.Update(timelineTime, deltaTime, localLogicTick);
                PushInstance();
                try
                {
                    m_Tree.OnTreeEnable();
                }
                finally
                {
                    PopInstance();
                }
                m_Enabled = true;
                Publish(RuntimeTraceEventKind.TreeClipEntered, "Commit", string.Empty);
            }

            public bool Tick(float timelineTime, float deltaTime, ulong localLogicTick)
            {
                if (!Valid || m_Decision)
                    return false;

                m_Context.Update(timelineTime, deltaTime, localLogicTick);
                PushInstance();
                try
                {
                    m_Tree.UpdateTree(deltaTime);
                }
                finally
                {
                    PopInstance();
                }
                Publish(RuntimeTraceEventKind.TreeClipUpdated, "Running", string.Empty);
                return true;
            }

            public NodeStopStatus BeginNaturalStop(ulong localLogicTick)
            {
                if (!Valid)
                    return NodeStopStatus.Failed;

                m_Context.Update(m_Context.TimelineTime, 0f, localLogicTick);
                Disable();
                NodeStopStatus status;
                PushInstance();
                try
                {
                    status = m_Tree.RequestStop(m_Context.CreateStopContext(NodeStopOriginCause.ExplicitParentStop));
                }
                finally
                {
                    PopInstance();
                }
                if (status != NodeStopStatus.Running)
                    CompleteNaturalStop();
                return status;
            }

            public NodeStopStatus UpdateStopping(float deltaTime, ulong localLogicTick)
            {
                if (!Valid)
                    return NodeStopStatus.Failed;

                m_Context.Update(m_Context.TimelineTime, deltaTime, localLogicTick);
                m_Tree.SetDeltaTime(deltaTime);
                PushInstance();
                try
                {
                    NodeStopStatus status = m_Tree.UpdateStopping();
                    Publish(RuntimeTraceEventKind.TreeClipUpdated, status.ToString(), "GracefulStop");
                    return status;
                }
                finally
                {
                    PopInstance();
                }
            }

            public void CompleteNaturalStop()
            {
                DisposeTree(true);
            }

            public void ForceStop(NodeStopContext stopContext)
            {
                if (!Valid || m_Disposed)
                    return;

                PushInstance();
                try
                {
                    m_Tree.ForceStop(stopContext);
                }
                finally
                {
                    PopInstance();
                }
                Publish(RuntimeTraceEventKind.TreeClipExited, "ForceStopped", stopContext.OriginCause.ToString());
                m_Enabled = false;
                DisposeTree(false);
            }

            public void DisposeDormant()
            {
                if (m_Disposed)
                    return;

                if (Valid && (m_Tree.Running || m_Tree.LifecyclePhase != NodeLifecyclePhase.Dormant))
                {
                    PushInstance();
                    try
                    {
                        m_Tree.ForceStop(m_Context.CreateStopContext(NodeStopOriginCause.Shutdown));
                    }
                    finally
                    {
                        PopInstance();
                    }
                }
                DisposeTree(false);
            }

            void Disable()
            {
                if (!m_Enabled)
                    return;

                PushInstance();
                try
                {
                    m_Tree.OnTreeDisable();
                }
                finally
                {
                    PopInstance();
                }
                m_Enabled = false;
                Publish(RuntimeTraceEventKind.TreeClipExited, "Stopped", string.Empty);
            }

            void DisposeTree(bool runDestroyLifecycle)
            {
                if (m_Disposed)
                    return;

                m_Disposed = true;
                PushInstance();
                try
                {
                    if (Valid && runDestroyLifecycle)
                        m_Tree.OnTreeDestroy();
                    if (m_Tree.IsValid)
                        m_Tree.DisposeTree();
                }
                finally
                {
                    PopInstance();
                }
                Valid = false;
                Publish(RuntimeTraceEventKind.TreeClipDestroyed, "Destroyed", string.Empty);
            }

            void Publish(RuntimeTraceEventKind kind, string status, string cause)
            {
                RuntimeDiagnosticsContext diagnostics = m_GraphContext?.RuntimeDiagnostics;
                Track track = m_Context.Clip?.Track;
                TimelineData timeline = track?.Timeline;
                if (diagnostics == null || !diagnostics.ShouldPublish(RuntimeTraceChannel.Timeline, kind) || timeline == null || track == null)
                    return;
                diagnostics.Publish(
                    RuntimeTraceChannel.Timeline,
                    RuntimeTraceDomain.Logic,
                    kind,
                    RuntimeSourceElementKey.Clip(timeline.AuthoringId, track.AuthoringId, m_Context.Clip.AuthoringId, true),
                    RuntimeInstanceKey.TreeClip(diagnostics.CharacterRuntimeId, m_RuntimeId, m_Context.PlaybackIdentity, m_Context.Cycle),
                    new RuntimeTracePayload
                    {
                        Name = m_Context.Clip.GetType().Name,
                        Status = status,
                        Cause = cause,
                        Time = m_Context.TimelineTime,
                        Cycle = m_Context.Cycle,
                        TrackIndex = m_Context.TrackIndex,
                        ClipIndex = m_Context.ClipIndex,
                        Detail = m_Decision ? "Decision" : "Commit"
                    });
            }

            void PushInstance()
            {
                m_GraphContext?.TreeExecutionContext.PushActivation(m_Context.SourceActivation);
                RuntimeDiagnosticsContext diagnostics = m_GraphContext?.RuntimeDiagnostics;
                if (diagnostics != null && m_RuntimeId != Guid.Empty)
                    diagnostics.PushRuntimeInstance(RuntimeInstanceKey.TreeClip(diagnostics.CharacterRuntimeId, m_RuntimeId, m_Context.PlaybackIdentity, m_Context.Cycle));
            }

            void PopInstance()
            {
                RuntimeDiagnosticsContext diagnostics = m_GraphContext?.RuntimeDiagnostics;
                if (diagnostics != null && m_RuntimeId != Guid.Empty)
                    diagnostics.PopRuntimeInstance(RuntimeInstanceKey.TreeClip(diagnostics.CharacterRuntimeId, m_RuntimeId, m_Context.PlaybackIdentity, m_Context.Cycle));
                m_GraphContext?.TreeExecutionContext.PopActivation(m_Context.SourceActivation);
            }
        }

        readonly struct DecisionEvaluationIdentity : IEquatable<DecisionEvaluationIdentity>
        {
            public DecisionEvaluationIdentity(int trackIndex, int clipIndex, int cycle)
            {
                TrackIndex = trackIndex;
                ClipIndex = clipIndex;
                Cycle = cycle;
            }

            int TrackIndex { get; }
            int ClipIndex { get; }
            int Cycle { get; }

            public bool Equals(DecisionEvaluationIdentity other)
            {
                return TrackIndex == other.TrackIndex && ClipIndex == other.ClipIndex && Cycle == other.Cycle;
            }

            public override bool Equals(object obj)
            {
                return obj is DecisionEvaluationIdentity other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = TrackIndex;
                    hash = hash * 397 ^ ClipIndex;
                    hash = hash * 397 ^ Cycle;
                    return hash;
                }
            }
        }
    }
}
