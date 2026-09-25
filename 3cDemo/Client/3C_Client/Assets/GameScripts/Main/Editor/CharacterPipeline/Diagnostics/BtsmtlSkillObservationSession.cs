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
            internal RuntimeInstanceKey GraphInstance;
        }
        sealed class Scope
        {
            static readonly List<RuntimeInstanceKey> s_Instances = new List<RuntimeInstanceKey>();

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
                    if (!view.TryGetInvocation(Root, path, out RuntimeGraphInvocation invocation))
                        return default;
                    RuntimeInstanceKey next = default;
                    ulong latest = 0;
                    s_Instances.Clear();
                    view.CopyGraphInstances(invocation.GraphId, s_Instances);
                    for (int i = 0; i < s_Instances.Count; i++)
                    {
                        RuntimeInstanceKey candidate = s_Instances[i];
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
        readonly List<RuntimeInstanceKey> m_ParentInstances = new();
        Scope m_PageScope;
        string m_PageGraphAuthoringId = string.Empty;
        BtsmtlSkillFlowObservation m_Observation;
        bool m_NavigationDirty = true;
        bool m_Disposed;
        bool m_CaptureValues;
        TimelineCaller m_PendingTimeline;
        TimelineCaller m_ActiveTimeline;

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

        internal static void ExpectTimelineOpening(BtsmtlSkillTimelineFlowNode node) =>
            s_Current?.OnTimelineOpening(node);

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
                    RuntimeGraphInvocation[] matches = m_Session.ViewModel.GetGraphInvocations(scope.Root).Where(value =>
                        BtsmtlRuntimeInvocationPath.MatchesParent(m_Session.ViewModel, scope.Root,
                            value.ParentPath, scope.Path) && value.GraphId == childAuthoring.AuthoringId &&
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
            RuntimeInstanceKey scope = m_ActiveTimeline.GraphInstance.IsValid
                ? m_ActiveTimeline.GraphInstance
                : m_ActiveTimeline.Scope?.Resolve(m_Session.ViewModel) ?? default;
            if (!scope.IsValid)
            {
                window.ClearRuntimeTimelineObservation();
                window.SetRuntimeObservationStatus("当前 Timeline 缺少唯一的运行调用路径。");
                return;
            }
            window.SetRuntimeObservationScope(scope);
            TimelineRuntimeObservationBridge.RefreshWindow(window);
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
            if (m_PageScope == null || !m_Session.ViewModel.TryGetInvocation(m_PageScope.Root, m_PageScope.Path, out RuntimeGraphInvocation invocation) ||
                string.IsNullOrEmpty(invocation.ParentPath))
                return false;
            return m_PageScope.Calls.Length != 0 ||
                m_Session.ViewModel.TryGetParentGeneration(m_PageScope.Root, out ulong generation) && generation != 0;
        }

        void NavigateParent()
        {
            RuntimeDebugViewModel view = m_Session.ViewModel;
            if (!CanNavigateParent() ||
                !view.TryGetInvocation(m_PageScope.Root, m_PageScope.Path, out RuntimeGraphInvocation invocation))
                return;
            Scope scope;
            RuntimeGraphInvocation parent;
            if (m_PageScope.Calls.Length != 0)
            {
                scope = m_PageScope.Parent();
                if (!BtsmtlRuntimeInvocationPath.MatchesParent(view, scope.Root, invocation.ParentPath, scope.Path) ||
                    !view.TryGetInvocation(scope.Root, scope.Path, out parent))
                    return;
            }
            else
            {
                if (!TryResolveParent(view, invocation, out RuntimeInstanceKey instance, out parent))
                    return;
                scope = new Scope(instance, Array.Empty<string>());
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
                Scope = m_PageScope,
                GraphInstance = m_Observation?.Instance ?? default
            };
        }

        bool TryResolveParent(RuntimeDebugViewModel view, RuntimeGraphInvocation child,
            out RuntimeInstanceKey parentInstance, out RuntimeGraphInvocation parentInvocation)
        {
            parentInstance = default;
            parentInvocation = default;
            RuntimeInstanceKey root = m_PageScope.Root;
            if (!view.TryGetParentGeneration(root, out ulong generation) || generation == 0)
                return false;
            foreach (RuntimeGraphInvocation candidate in view.GetGraphInvocations(root))
            {
                if (!BtsmtlRuntimeInvocationPath.MatchesParent(view, root, child.ParentPath, candidate.Path))
                    continue;
                view.CopyGraphInstances(candidate.GraphId, m_ParentInstances);
                for (int i = 0; i < m_ParentInstances.Count; i++)
                {
                    RuntimeInstanceKey instance = m_ParentInstances[i];
                    if (!SameRelease(instance, root) ||
                        instance.InvocationGeneration != generation ||
                        !string.Equals(instance.CallSiteId, candidate.Path, StringComparison.Ordinal))
                        continue;
                    if (parentInstance.IsValid)
                        return false;
                    parentInstance = instance;
                    parentInvocation = candidate;
                }
            }
            return parentInstance.IsValid;
        }

        void OnTimelineAssetOpened(TimelineAsset asset)
        {
            ClearTimelineOverlay();
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
            RuntimeGraphInvocation[] matches = m_Session.ViewModel.GetGraphInvocations(m_ActiveTimeline.Scope.Root).Where(value =>
                BtsmtlRuntimeInvocationPath.MatchesParent(m_Session.ViewModel,
                    m_ActiveTimeline.Scope.Root, value.ParentPath, m_ActiveTimeline.Scope.Path) &&
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
