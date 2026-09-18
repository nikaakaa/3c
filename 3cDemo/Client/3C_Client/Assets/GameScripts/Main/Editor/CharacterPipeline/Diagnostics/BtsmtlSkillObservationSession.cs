using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Editor;
using FlowCanvas;
using NodeCanvas.Editor;
using NodeCanvas.Framework;
using ThirdPersonCharacter.Control.Authoring;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class BtsmtlSkillObservationSession : IDisposable
    {
        sealed class TimelineCaller
        {
            internal TimelineAsset Asset;
            internal string NodeId;
            internal string GraphAuthoringId;
            internal Scope Scope;
        }
        sealed class Scope
        {
            internal Scope(RuntimeInstanceKey root, IEnumerable<string> calls)
            {
                Root = root;
                Calls = calls.ToArray();
            }
            internal RuntimeInstanceKey Root { get; }
            internal string[] Calls { get; }
            internal string Path => Calls.Length == 0 ? Root.CallSiteId : Calls[Calls.Length - 1];
            internal Scope Append(string path) => new(Root, Calls.Append(path));
            internal Scope Parent() => new(Root, Calls.Take(Calls.Length - 1));

            internal RuntimeInstanceKey Resolve(RuntimeDebugViewModel view)
            {
                RuntimeInstanceKey current = Root;
                foreach (string path in Calls)
                {
                    if (!view.TryGetInvocation(path, out RuntimeGraphInvocation invocation))
                        return default;
                    RuntimeInstanceKey next = default;
                    ulong latest = 0;
                    foreach (RuntimeInstanceKey candidate in view.GetGraphInstances(invocation.GraphId))
                    {
                        if (!SameRelease(candidate, Root) || candidate.CallSiteId != path ||
                            !view.TryGetParentGeneration(candidate, out ulong parent) || parent != current.InvocationGeneration)
                            continue;
                        ulong sequence = view.InvocationSequence(candidate);
                        if (!next.IsValid || sequence > latest)
                        {
                            next = candidate;
                            latest = sequence;
                        }
                    }
                    if (!next.IsValid)
                        return default;
                    current = next;
                }
                return current;
            }
        }

        static BtsmtlSkillObservationSession s_Current;
        readonly CharacterPipelineDefinition m_Definition;
        readonly RuntimeDebugSession m_Session;
        readonly FlowGraph m_RootGraph;
        readonly Scope m_RootScope;
        Scope m_PageScope;
        string m_PageGraphAuthoringId = string.Empty;
        BtsmtlSkillFlowObservation m_Observation;
        bool m_NavigationDirty = true;
        bool m_Disposed;
        bool m_CaptureValues;
        TimelineCaller m_PendingTimeline;
        TimelineCaller m_ActiveTimeline;
        readonly HashSet<string> m_RuntimeSeenTracks = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> m_RuntimeSeenClips = new HashSet<string>(StringComparer.Ordinal);

        BtsmtlSkillObservationSession(CharacterPipelineDefinition definition, FlowGraph graph, RuntimeDebugSession session, Scope scope)
        {
            m_Definition = definition;
            m_RootGraph = graph;
            m_Session = session;
            m_RootScope = scope;
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            GraphEditor.onEditorClosed += OnEditorClosed;
            GraphEditor.onEditorNavigationChanged += OnNavigationChanged;
            m_Session.Changed += OnRuntimeChanged;
            TimelineEditorWindow.AssetOpened += OnTimelineAssetOpened;
            TimelineEditorWindow.AssetTreeOpened += OnTimelineTreeOpened;
            TimelineEditorWindow.WindowClosed += OnTimelineWindowClosed;
        }

        public static void Open(CharacterPipelineDefinition definition, FlowGraph graph, RuntimeDebugSession session, RuntimeInstanceKey instance) =>
            OpenScope(definition, graph, session, new Scope(instance, Array.Empty<string>()));

        public static void Close() => s_Current?.Dispose();

        static void OpenScope(CharacterPipelineDefinition definition, FlowGraph graph, RuntimeDebugSession session, Scope scope)
        {
            bool capture = s_Current?.m_Observation?.CaptureValues ?? false;
            s_Current?.Dispose();
            s_Current = new BtsmtlSkillObservationSession(definition, graph, session, scope) { m_CaptureValues = capture };
            s_Current.Update();
        }

        void OnNavigationChanged() => m_NavigationDirty = true;

        void OnRuntimeChanged() => UpdateTimelineOverlay();

        void Update()
        {
            if (m_Disposed)
                return;
            if (!Application.isPlaying || !GraphEditor.current || GraphEditor.rootGraph != m_RootGraph)
            {
                Dispose();
                return;
            }
            if (!m_NavigationDirty)
            {
                UpdateTimelineOverlay();
                return;
            }
            m_NavigationDirty = false;
            m_CaptureValues = m_Observation?.CaptureValues ?? m_CaptureValues;
            m_Observation?.Dispose();
            m_Observation = null;
            try
            {
                var graph = m_RootGraph;
                Scope scope = m_RootScope;
                while (graph.GetCurrentChildGraph() is FlowGraph child && child is IBtsmtlSkillFlowGraph childAuthoring)
                {
                    IGraphElement caller = graph.GetCurrentChildGraphSource();
                    RuntimeSourceElementKind kind = caller is Connection ? RuntimeSourceElementKind.Edge : RuntimeSourceElementKind.Node;
                    RuntimeGraphInvocation[] matches = m_Session.ViewModel.GraphInvocations.Where(value =>
                        value.ParentPath == scope.Path && value.GraphId == childAuthoring.AuthoringId &&
                        value.Caller.Kind == kind && value.Caller.ElementAuthoringId == caller.UID &&
                        string.IsNullOrEmpty(value.CallerClipId)).ToArray();
                    if (matches.Length != 1)
                        throw new InvalidOperationException("当前页面没有唯一的同版本运行调用路径。");
                    scope = scope.Append(matches[0].Path);
                    graph = child;
                }
                m_PageScope = scope;
                m_PageGraphAuthoringId = ((IBtsmtlSkillFlowGraph)graph).AuthoringId;
                var request = new RuntimeDebugTargetRequest(RuntimeSourceElementKey.Graph(((IBtsmtlSkillFlowGraph)graph).AuthoringId),
                    new BtsmtlSkillGraphFingerprint().Compute(graph));
                m_Observation = BtsmtlSkillFlowObservation.ForScope(graph, m_Session, request, scope.Root.CharacterRuntimeId, scope.Resolve);
                m_Observation.CaptureValues = m_CaptureValues;
                m_Observation.SetParentNavigation(CanNavigateParent, NavigateParent, OnTimelineOpening);
                m_Observation.SetInstanceSelection(() => BtsmtlSkillHostEntry.ShowInstances(
                    m_Definition, graph, m_Session, scope.Root.CharacterRuntimeId, m_Observation.Instance));
                UpdateTimelineOverlay();
            }
            catch (InvalidOperationException error)
            {
                ClearTimelineOverlay();
                GraphEditor.current.ShowNotification(new GUIContent(error.Message));
            }
        }

        void UpdateTimelineOverlay()
        {
            if (m_ActiveTimeline?.Asset == null || string.IsNullOrEmpty(m_ActiveTimeline.GraphAuthoringId))
                return;
            TimelineEditorWindow window = TimelineEditorWindow.FindOpen(m_ActiveTimeline.Asset);
            if (window == null)
                return;
            TimelineData timeline = m_ActiveTimeline.Asset.Data;
            IReadOnlyList<RuntimeTimelinePlaybackDebugSummary> summaries =
                m_Session.ViewModel.GetTimelinePlaybackSummaries(
                    timeline.AuthoringId,
                    m_ActiveTimeline.GraphAuthoringId);
            RuntimeTimelinePlaybackDebugSummary[] matches = summaries
                .Where(value => string.Equals(
                    value.Provenance.SourceNodeAuthoringId,
                    m_ActiveTimeline.NodeId,
                    StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1 || !matches[0].Playback.IsValid)
            {
                window.ClearRuntimeTimelineObservation();
                if (matches.Length > 1)
                    window.SetRuntimeObservationStatus("当前 Timeline 对应多个运行调用，请从 SkillGraph 选择具体实例。");
                return;
            }
            RuntimeTimelinePlaybackDebugSummary summary = matches[0];

            var activeTracks = new Dictionary<string, string>(StringComparer.Ordinal);
            var activeClips = new Dictionary<string, string>(StringComparer.Ordinal);
            IReadOnlyList<RuntimeDebugEventView> events = m_Session.ViewModel.GetTimelineCurrentEvents(
                timeline.AuthoringId,
                summary.Playback,
                m_ActiveTimeline.GraphAuthoringId);
            foreach (RuntimeDebugEventView item in events)
            {
                RuntimeSourceElementKey source = item.Source;
                if (source.Kind == RuntimeSourceElementKind.Track && !string.IsNullOrEmpty(source.TrackAuthoringId))
                {
                    m_RuntimeSeenTracks.Add(source.TrackAuthoringId);
                    activeTracks[source.TrackAuthoringId] = item.Event.Payload.Status;
                }
                else if ((source.Kind == RuntimeSourceElementKind.Clip || source.Kind == RuntimeSourceElementKind.TreeClip) &&
                         !string.IsNullOrEmpty(source.ClipAuthoringId))
                {
                    m_RuntimeSeenClips.Add(source.ClipAuthoringId);
                    if (!string.IsNullOrEmpty(source.TrackAuthoringId))
                        m_RuntimeSeenTracks.Add(source.TrackAuthoringId);
                    activeClips[source.ClipAuthoringId] = item.Event.Payload.Status;
                }
            }
            foreach (Track track in timeline.Tracks)
            {
                if (track is not TreeTrack treeTrack)
                    continue;
                foreach (Clip clip in treeTrack.Clips)
                {
                    if (clip is TreeClip treeClip &&
                        treeClip.ClipExitSource == TimelineClipExitSource.TreeDecision &&
                        activeClips.ContainsKey(clip.AuthoringId))
                        activeClips[clip.AuthoringId] = "open";
                }
            }
            TimelineData runtimeTimeline = BuildRuntimeTimeline(
                timeline,
                m_RuntimeSeenTracks,
                m_RuntimeSeenClips);
            if (m_Session.AttachmentState == RuntimeDebugAttachmentState.CaptureHistory)
                window.ApplyHistoryTimelineObservation(runtimeTimeline, summary.VisualTime, activeTracks, activeClips);
            else
                window.ApplyRuntimeTimelineObservation(runtimeTimeline, summary.VisualTime, activeTracks, activeClips);
        }

        static TimelineData BuildRuntimeTimeline(
            TimelineData source,
            ISet<string> seenTracks,
            ISet<string> seenClips)
        {
            TimelineData runtime = source.Clone();
            runtime.Name = $"{source.Name} [Runtime]";
            for (int trackIndex = runtime.Tracks.Count - 1; trackIndex >= 0; trackIndex--)
            {
                Track track = runtime.Tracks[trackIndex];
                if (track == null)
                {
                    runtime.Tracks.RemoveAt(trackIndex);
                    continue;
                }
                for (int clipIndex = track.Clips.Count - 1; clipIndex >= 0; clipIndex--)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip == null || !seenClips.Contains(clip.AuthoringId))
                        track.Clips.RemoveAt(clipIndex);
                }
                if (!seenTracks.Contains(track.AuthoringId) && track.Clips.Count == 0)
                    runtime.Tracks.RemoveAt(trackIndex);
            }
            runtime.Init();
            return runtime;
        }

        void ClearTimelineOverlay()
        {
            if (m_ActiveTimeline?.Asset != null)
            {
                TimelineEditorWindow window = TimelineEditorWindow.FindOpen(m_ActiveTimeline.Asset);
                window?.ClearRuntimeTimelineObservation();
                window?.SetRuntimeObservationReadOnly(false);
            }
        }

        bool CanNavigateParent()
        {
            if (m_PageScope == null || !m_Session.ViewModel.TryGetInvocation(m_PageScope.Path, out RuntimeGraphInvocation invocation) ||
                string.IsNullOrEmpty(invocation.ParentPath))
                return false;
            return m_PageScope.Calls.Length != 0 ||
                m_Session.ViewModel.TryGetParentGeneration(m_PageScope.Root, out ulong generation) && generation != 0;
        }

        void NavigateParent()
        {
            RuntimeDebugViewModel view = m_Session.ViewModel;
            if (!CanNavigateParent() || !view.TryGetInvocation(m_PageScope.Path, out RuntimeGraphInvocation invocation) ||
                !view.TryGetInvocation(invocation.ParentPath, out RuntimeGraphInvocation parent))
                return;
            Scope scope;
            if (m_PageScope.Calls.Length != 0)
                scope = m_PageScope.Parent();
            else
            {
                view.TryGetParentGeneration(m_PageScope.Root, out ulong generation);
                RuntimeInstanceKey root = m_PageScope.Root;
                scope = new Scope(RuntimeInstanceKey.SkillExecution(root.CharacterRuntimeId, root.GraphRuntimeId, root.StateId,
                    root.ActionInstanceId, parent.Path, root.ActivationGeneration, generation), Array.Empty<string>());
            }
            FlowGraph graph = FindGraph(parent.GraphId);
            graph.SetCurrentChildGraphAssignable(null);
            GraphEditor.OpenWindow(graph);
            OpenScope(m_Definition, graph, m_Session, scope);
            IGraphElement caller = invocation.Caller.Kind == RuntimeSourceElementKind.Edge
                ? graph.allNodes.SelectMany(node => node.outConnections).SingleOrDefault(edge => edge.UID == invocation.Caller.ElementAuthoringId)
                : graph.allNodes.SingleOrDefault(node => node.UID == invocation.Caller.ElementAuthoringId);
            if (caller != null)
                GraphEditor.FocusElement(caller, true);
            if (!string.IsNullOrEmpty(invocation.CallerClipId) && caller is BtsmtlSkillTimelineFlowNode timeline)
            {
                var track = timeline.Timeline.Tracks.Single(value => value.Clips.Any(clip => clip.AuthoringId == invocation.CallerClipId));
                s_Current.OnTimelineOpening(timeline);
                TimelineEditorWindow.Open(
                    timeline.TimelineAsset,
                    ((IBtsmtlSkillFlowGraph)graph).AuthoringId,
                    timeline.UID).FocusSource(track.AuthoringId, invocation.CallerClipId);
                s_Current.OnTimelineOpening(null);
            }
        }

        void OnTimelineOpening(BtsmtlSkillTimelineFlowNode node)
        {
            m_PendingTimeline = node == null ? null : new TimelineCaller
            {
                Asset = node.TimelineAsset,
                NodeId = node.UID,
                GraphAuthoringId = m_PageGraphAuthoringId,
                Scope = m_PageScope
            };
        }

        void OnTimelineAssetOpened(TimelineAsset asset)
        {
            ClearTimelineOverlay();
            m_RuntimeSeenTracks.Clear();
            m_RuntimeSeenClips.Clear();
            m_ActiveTimeline = m_PendingTimeline?.Asset == asset ? m_PendingTimeline : null;
            m_PendingTimeline = null;
            if (m_ActiveTimeline != null)
            {
                TimelineEditorWindow window = TimelineEditorWindow.FindOpen(asset);
                window?.ApplyRuntimeLocator(
                    m_ActiveTimeline.GraphAuthoringId,
                    m_ActiveTimeline.NodeId);
                window?.SetRuntimeObservationReadOnly(true);
            }
            UpdateTimelineOverlay();
        }

        void OnTimelineTreeOpened(TimelineAsset asset, TreeClip clip)
        {
            if (m_ActiveTimeline == null || m_ActiveTimeline.Asset != asset || clip.AssetTree is not BtsmtlSkillFlowGraph graph)
                return;
            RuntimeGraphInvocation[] matches = m_Session.ViewModel.GraphInvocations.Where(value =>
                value.ParentPath == m_ActiveTimeline.Scope.Path && value.GraphId == graph.AuthoringId &&
                value.Caller.ElementAuthoringId == m_ActiveTimeline.NodeId && value.CallerClipId == clip.AuthoringId &&
                value.CallerId == "Root").ToArray();
            if (matches.Length != 1)
            {
                GraphEditor.current?.ShowNotification(new GUIContent("TreeClip缺少唯一的同版本调用路径。"));
                return;
            }
            Scope scope = m_ActiveTimeline.Scope.Append(matches[0].Path);
            graph.SetCurrentChildGraphAssignable(null);
            GraphEditor.OpenWindow(graph);
            OpenScope(m_Definition, graph, m_Session, scope);
        }

        void OnTimelineWindowClosed(TimelineEditorWindow window)
        {
            if (m_ActiveTimeline?.Asset == null || window == null || window.Timeline != m_ActiveTimeline.Asset.Data)
                return;
            ClearTimelineOverlay();
            m_ActiveTimeline = null;
        }

        FlowGraph FindGraph(string identity)
        {
            IReadOnlyList<BtsmtlSkillFlowGraph> roots = m_Definition.AbilityGraphs;
            return roots.Where(graph => graph != null)
            .SelectMany(graph => BtsmtlSkillGraphClosure.Validate(graph, false)).Distinct()
            .Single(graph => ((IBtsmtlSkillFlowGraph)graph).AuthoringId == identity);
        }

        static bool SameRelease(RuntimeInstanceKey left, RuntimeInstanceKey right) =>
            left.Kind == RuntimeInstanceKind.SkillExecution && left.CharacterRuntimeId == right.CharacterRuntimeId &&
            left.GraphRuntimeId == right.GraphRuntimeId && left.StateId == right.StateId &&
            left.ActionInstanceId == right.ActionInstanceId && left.ActivationGeneration == right.ActivationGeneration;

        void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                Dispose();
        }

        void OnEditorClosed() => Dispose();

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Observation?.Dispose();
            EditorApplication.update -= Update;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            GraphEditor.onEditorClosed -= OnEditorClosed;
            GraphEditor.onEditorNavigationChanged -= OnNavigationChanged;
            m_Session.Changed -= OnRuntimeChanged;
            TimelineEditorWindow.AssetOpened -= OnTimelineAssetOpened;
            TimelineEditorWindow.AssetTreeOpened -= OnTimelineTreeOpened;
            TimelineEditorWindow.WindowClosed -= OnTimelineWindowClosed;
            ClearTimelineOverlay();
            if (ReferenceEquals(s_Current, this))
                s_Current = null;
        }
    }
}
