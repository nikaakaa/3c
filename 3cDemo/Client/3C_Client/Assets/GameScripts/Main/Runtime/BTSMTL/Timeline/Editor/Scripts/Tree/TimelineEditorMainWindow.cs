using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using BTSMTL.Timeline.Runtime;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor
{
    public enum TimelineWorkspaceMode : byte
    {
        Authoring = 0,
        Preview = 1,
        RuntimeDebug = 2
    }

    public interface ITimelineWorkspaceModeController
    {
        VisualElement CreateControls(TimelineEditorWindow window);
        void ApplyToWindow(TimelineEditorWindow window);
        void OnWindowClosed(TimelineEditorWindow window);
    }

    public static class TimelineWorkspaceModeBridge
    {
        static ITimelineWorkspaceModeController s_Controller;

        public static TimelineWorkspaceMode ActiveMode { get; private set; } = TimelineWorkspaceMode.Authoring;

        public static void Register(ITimelineWorkspaceModeController controller)
        {
            s_Controller = controller;
        }

        public static void SetActiveMode(TimelineWorkspaceMode mode)
        {
            ActiveMode = mode;
        }

        public static VisualElement CreateControls(TimelineEditorWindow window) =>
            s_Controller?.CreateControls(window);

        public static void ApplyToWindow(TimelineEditorWindow window) =>
            s_Controller?.ApplyToWindow(window);

        public static void OnWindowClosed(TimelineEditorWindow window) =>
            s_Controller?.OnWindowClosed(window);
    }

    public sealed class TimelineEditorWindow : EditorWindow
    {
        public static event Action<TimelineAsset> AssetOpened;
        public static event Action<TimelineAsset, TreeClip> AssetTreeOpened;
        public static event Func<TreeClip, bool> TreeClipOpenRequested;
        internal static event Action<TimelineEditorWindow> WindowOpened;
        public static event Action<TimelineEditorWindow> WindowClosed;
        public static event Action<TimelineEditorWindow> AuthoringRevisionChanged;

        [SerializeField]
        UnityEngine.Object m_SerializedOwner;

        [SerializeField]
        string m_SerializedPropertyPath;

        [SerializeField]
        string m_OwnershipLabel;

        [SerializeField]
        string m_SourceNodeGuid;

        [SerializeField]
        string m_SourceGraphAuthoringId;

        [SerializeField]
        UnityEngine.Object m_SourceGraphWindow;

        [SerializeField]
        UnityEngine.Object m_SourceGraphOwner;

        [SerializeField]
        UnityEngine.Object m_NavigationOwner;

        [SerializeField]
        string m_NavigationPropertyPath;

        [SerializeField]
        string m_NavigationOwnershipLabel;

        [SerializeField]
        string m_NavigationTrackAuthoringId;

        [SerializeField]
        string m_NavigationClipAuthoringId;

        [SerializeField]
        string m_NavigationSourceNodeGuid;

        [SerializeField]
        UnityEngine.Object m_NavigationSourceGraphWindow;

        [SerializeField]
        UnityEngine.Object m_NavigationSourceGraphOwner;

        [SerializeField]
        Vector2 m_NavigationViewport;

        TimelineNode m_SourceNode;
        BtsmtlSlateTimelineProjection m_SlateProjection;
        IMGUIContainer m_SlateSurface;
        TimelineData m_Timeline;
        TimelineEditorToolbarControls m_Toolbar;
        VisualElement m_WorkspaceModeControls;
        bool m_RuntimeObservationReadOnly;
        RuntimeInstanceKey m_RuntimeObservationScope;
        RuntimeInstanceKey m_RuntimeObservationPlayback;
        bool m_RuntimeObservationPinned;

        public TimelineData Timeline => m_Timeline;
        public UnityEngine.Object SourceGraphWindow => m_SourceGraphWindow;
        public string SourceGraphAuthoringId => m_SourceGraphAuthoringId ?? string.Empty;
        public string SourceNodeAuthoringId => m_SourceNodeGuid ?? string.Empty;
        public TimelineAsset SourceAsset => m_SerializedOwner as TimelineAsset;
        public bool RuntimeObservationReadOnly => m_RuntimeObservationReadOnly;
        public RuntimeInstanceKey RuntimeObservationScope => m_RuntimeObservationScope;
        public RuntimeInstanceKey RuntimeObservationPlayback => m_RuntimeObservationPlayback;
        public string AuthoringRevision => m_Timeline == null
            ? string.Empty
            : TimelineAuthoringFingerprint.Compute(m_Timeline);

        public IReadOnlyList<RuntimeTimelinePlaybackDebugSummary> GetRuntimeObservationSummaries()
        {
            if (m_Timeline == null)
                return Array.Empty<RuntimeTimelinePlaybackDebugSummary>();
            IReadOnlyList<RuntimeTimelinePlaybackDebugSummary> summaries =
                RuntimeDebugSession.Shared.ViewModel.GetTimelinePlaybackSummaries(
                    m_Timeline.AuthoringId,
                    SourceGraphAuthoringId);
            if (string.IsNullOrEmpty(SourceNodeAuthoringId))
                return FilterRuntimeScope(summaries);
            return FilterRuntimeScope(summaries
                .Where(value => string.Equals(
                    value.Provenance.SourceNodeAuthoringId,
                    SourceNodeAuthoringId,
                    StringComparison.Ordinal))
                .ToArray());
        }

        internal bool TryResolveRuntimeObservation(
            out RuntimeTimelinePlaybackDebugSummary summary,
            out string message)
        {
            IReadOnlyList<RuntimeTimelinePlaybackDebugSummary> summaries = GetRuntimeObservationSummaries();
            if (!m_RuntimeObservationPinned)
            {
                RuntimeInstanceKey activePlayback = default;
                int activeCount = 0;
                for (int index = 0; index < summaries.Count; index++)
                {
                    if (summaries[index].IsTerminal || !summaries[index].Playback.IsValid)
                        continue;
                    activePlayback = summaries[index].Playback;
                    activeCount++;
                }
                if (activeCount == 1 && !m_RuntimeObservationPlayback.Equals(activePlayback))
                {
                    m_RuntimeObservationPlayback = activePlayback;
                    ClearRuntimeTimelineObservation();
                }
            }
            if (m_RuntimeObservationPlayback.IsValid)
            {
                for (int index = 0; index < summaries.Count; index++)
                {
                    if (!summaries[index].Playback.Equals(m_RuntimeObservationPlayback))
                        continue;
                    summary = summaries[index];
                    message = string.Empty;
                    return true;
                }
                summary = default;
                message = "已选择的运行 Timeline 实例不在当前调用路径中。";
                return false;
            }
            if (summaries.Count == 1 && summaries[0].Playback.IsValid)
            {
                summary = summaries[0];
                message = string.Empty;
                return true;
            }
            summary = default;
            message = summaries.Count > 1
                ? "当前 Timeline 对应多个运行调用，请从 SkillGraph 选择具体实例。"
                : m_Timeline == null
                    ? "当前未打开 Timeline。"
                    : string.IsNullOrEmpty(SourceGraphAuthoringId)
                        ? "当前 Timeline 没有来源图绑定。"
                        : "当前 Timeline 没有匹配的运行调用。";
            return false;
        }

        public void SetRuntimeObservationScope(RuntimeInstanceKey scope)
        {
            if (m_RuntimeObservationScope.Equals(scope))
                return;
            m_RuntimeObservationScope = scope;
            m_RuntimeObservationPlayback = default;
            m_RuntimeObservationPinned = false;
            ClearRuntimeTimelineObservation();
        }

        public bool SelectRuntimeObservationPlayback(RuntimeInstanceKey playback, bool pin = false)
        {
            if (playback.Kind != RuntimeInstanceKind.TimelinePlayback)
                return false;
            IReadOnlyList<RuntimeTimelinePlaybackDebugSummary> summaries = GetRuntimeObservationSummaries();
            for (int index = 0; index < summaries.Count; index++)
            {
                if (!summaries[index].Playback.Equals(playback))
                    continue;
                if (m_RuntimeObservationPlayback.Equals(playback))
                {
                    m_RuntimeObservationPinned = pin;
                    return true;
                }
                m_RuntimeObservationPlayback = playback;
                m_RuntimeObservationPinned = pin;
                ClearRuntimeTimelineObservation();
                return true;
            }
            m_RuntimeObservationPlayback = playback;
            m_RuntimeObservationPinned = pin;
            ClearRuntimeTimelineObservation();
            SetRuntimeObservationStatus("当前 Timeline 播放实例与作者来源不一致。");
            return false;
        }

        internal void ClearRuntimeObservationSelection()
        {
            m_RuntimeObservationScope = default;
            m_RuntimeObservationPlayback = default;
            m_RuntimeObservationPinned = false;
        }

        IReadOnlyList<RuntimeTimelinePlaybackDebugSummary> FilterRuntimeScope(
            IReadOnlyList<RuntimeTimelinePlaybackDebugSummary> summaries)
        {
            if (!m_RuntimeObservationScope.IsValid || summaries.Count == 0)
                return summaries;
            var filtered = new List<RuntimeTimelinePlaybackDebugSummary>(summaries.Count);
            for (int index = 0; index < summaries.Count; index++)
            {
                RuntimeTimelinePlaybackDebugSummary summary = summaries[index];
                RuntimeTimelinePlaybackProvenance provenance = summary.Provenance;
                if (!provenance.IsValid)
                    continue;
                if (m_RuntimeObservationScope.Kind == RuntimeInstanceKind.SkillExecution)
                {
                    if (!provenance.HasProgramInvocation ||
                        string.IsNullOrEmpty(m_RuntimeObservationScope.CallSiteId) ||
                        m_RuntimeObservationScope.ActionInstanceId == 0 ||
                        m_RuntimeObservationScope.ActivationGeneration == 0 ||
                        m_RuntimeObservationScope.InvocationGeneration == 0 ||
                        summary.Playback.ActionInstanceId != m_RuntimeObservationScope.ActionInstanceId ||
                        provenance.SkillExecutionGeneration != m_RuntimeObservationScope.ActivationGeneration ||
                        provenance.SourceActivationGeneration != m_RuntimeObservationScope.InvocationGeneration ||
                        !string.Equals(provenance.SourceInvocationPath, m_RuntimeObservationScope.CallSiteId, StringComparison.Ordinal))
                    {
                        continue;
                    }
                    filtered.Add(summary);
                    continue;
                }
                if (m_RuntimeObservationScope.GraphRuntimeId != Guid.Empty &&
                    provenance.SourceGraphRuntimeId != m_RuntimeObservationScope.GraphRuntimeId)
                    continue;
                ulong scopeGeneration = m_RuntimeObservationScope.InvocationGeneration != 0
                    ? m_RuntimeObservationScope.InvocationGeneration
                    : m_RuntimeObservationScope.ActivationGeneration;
                if (scopeGeneration != 0 && provenance.SourceActivationGeneration != scopeGeneration)
                    continue;
                filtered.Add(summary);
            }
            return filtered;
        }

        public bool FocusSource(string trackAuthoringId, string clipAuthoringId)
        {
            return m_SlateProjection != null &&
                   m_SlateProjection.FocusSource(trackAuthoringId, clipAuthoringId);
        }

        public static TimelineEditorWindow Open(UnityEngine.Object sourceGraphWindow, TimelineNode node)
        {
            if (node?.Timeline == null)
                return null;

            TimelineEditorWindow window = GetWindow<TimelineEditorWindow>();
            window.ClearNavigation();
            window.BindNode(sourceGraphWindow, node);
            window.Show();
            window.Focus();
            return window;
        }

        public static TimelineEditorWindow Open(TimelineAsset asset)
        {
            return Open(asset, string.Empty, string.Empty);
        }

        public static TimelineEditorWindow Open(
            TimelineAsset asset,
            string sourceGraphAuthoringId,
            string sourceNodeGuid)
        {
            if (!asset)
                return null;

            TimelineEditorWindow window = GetWindow<TimelineEditorWindow>();
            window.ClearNavigation();
            window.BindAsset(asset, sourceGraphAuthoringId, sourceNodeGuid);
            window.Show();
            window.Focus();
            return window;
        }

        public static TimelineEditorWindow FindOpen(TimelineAsset asset)
        {
            if (!asset)
                return null;
            TimelineEditorWindow[] windows = Resources.FindObjectsOfTypeAll<TimelineEditorWindow>();
            for (int index = 0; index < windows.Length; index++)
                if (windows[index] && ReferenceEquals(windows[index].m_SerializedOwner, asset))
                    return windows[index];
            return null;
        }

        public void ApplyRuntimeTimelineObservation(
            TimelineData runtimeTimeline,
            bool structureChanged,
            float visualTime,
            IReadOnlyDictionary<string, string> activeTracks,
            IReadOnlyDictionary<string, string> activeClips)
        {
            if (!m_RuntimeObservationReadOnly)
                return;
            m_SlateProjection?.ApplyRuntimeTimeline(runtimeTimeline, structureChanged, visualTime, activeTracks, activeClips);
        }

        public void ApplyHistoryTimelineObservation(
            TimelineData runtimeTimeline,
            bool structureChanged,
            float visualTime,
            IReadOnlyDictionary<string, string> activeTracks,
            IReadOnlyDictionary<string, string> activeClips)
        {
            if (!m_RuntimeObservationReadOnly)
                return;
            m_SlateProjection?.ApplyHistoryTimeline(runtimeTimeline, structureChanged, visualTime, activeTracks, activeClips);
        }

        public void ClearRuntimeTimelineObservation()
        {
            m_SlateProjection?.ClearRuntimeTimeline();
            m_SlateProjection?.ClearRuntimeOverlay();
        }

        public void SetRuntimeObservationReadOnly(bool readOnly)
        {
            if (m_RuntimeObservationReadOnly == readOnly)
                return;
            m_RuntimeObservationReadOnly = readOnly;
            m_SlateProjection?.SetRuntimeReadOnly(readOnly);
            if (!readOnly)
                ClearRuntimeObservationSelection();
            if (!readOnly)
                m_SlateProjection?.ClearRuntimeTimeline();
            if (!readOnly)
                m_SlateProjection?.ClearRuntimeOverlay();
        }

        public void SetRuntimeObservationStatus(string message)
        {
            SetStatus(message);
        }

        public void ApplyRuntimeLocator(string graphAuthoringId, string sourceNodeAuthoringId)
        {
            m_SourceGraphAuthoringId = graphAuthoringId ?? string.Empty;
            m_SourceNodeGuid = sourceNodeAuthoringId ?? string.Empty;
            ClearRuntimeObservationSelection();
        }

        void BindAsset(TimelineAsset asset, string sourceGraphAuthoringId, string sourceNodeGuid)
        {
            if (!asset)
                throw new ArgumentNullException(nameof(asset));
            m_SourceGraphOwner = null;
            m_RuntimeObservationReadOnly = false;
            ClearRuntimeObservationSelection();
            Bind(
                asset.Data,
                asset,
                "m_Data",
                AssetDatabase.IsSubAsset(asset) ? "Private Asset" : "Shared Asset",
                null,
                null,
                sourceNodeGuid ?? string.Empty,
                sourceGraphAuthoringId ?? string.Empty);
        }

        public static void RebindIfOpen(TimelineNode node)
        {
            if (node == null)
                return;

            TimelineEditorWindow[] windows = Resources.FindObjectsOfTypeAll<TimelineEditorWindow>();
            for (int index = 0; index < windows.Length; index++)
            {
                TimelineEditorWindow window = windows[index];
                if (!window || !window.MatchesSourceNode(node))
                    continue;
                window.BindNode(window.m_SourceGraphWindow, node);
            }
        }

        public void CreateGUI()
        {
            TryRestoreBinding();
            if (m_SlateProjection == null)
                BuildUnboundView();
        }

        [MenuItem("Tools/TreeDesigner/Timeline Editor", false, 3)]
        public static void OpenStandalone()
        {
            TimelineEditorWindow window = GetWindow<TimelineEditorWindow>();
            if (window.m_SlateProjection == null)
                window.BuildUnboundView();
            window.Show();
            window.Focus();
        }

        void BindNode(UnityEngine.Object sourceGraphWindow, TimelineNode node)
        {
            m_SourceNode = node;
            m_SourceGraphWindow = sourceGraphWindow;
            m_SourceGraphOwner = node.Owner?.SerializedOwner;
            m_SourceGraphAuthoringId = node.Owner?.GraphAuthoringId ?? string.Empty;
            Bind(
                node.Timeline,
                node.Timeline.SerializedOwner,
                node.Timeline.SerializedPropertyPath,
                node.TimelineOwnership.ToString(),
                sourceGraphWindow,
                node,
                node.GUID,
                m_SourceGraphAuthoringId);
        }

        void Bind(
            TimelineData timeline,
            UnityEngine.Object serializedOwner,
            string serializedPropertyPath,
            string ownershipLabel,
            UnityEngine.Object sourceGraphWindow,
            TimelineNode sourceNode,
            string sourceNodeGuid,
            string sourceGraphAuthoringId = null)
        {
            if (timeline == null || !serializedOwner || string.IsNullOrEmpty(serializedPropertyPath))
                throw new InvalidOperationException("TimelineEditorWindow requires a bound TimelineData owner/path.");

            DisposeView();
            timeline.BindSerializedOwner(serializedOwner, serializedPropertyPath);
            timeline.OnValueChanged += OnTimelineValueChanged;
            m_SerializedOwner = serializedOwner;
            m_SerializedPropertyPath = serializedPropertyPath;
            m_OwnershipLabel = ownershipLabel;
            m_SourceGraphWindow = sourceGraphWindow;
            m_SourceNode = sourceNode;
            if (sourceNode != null)
            {
                m_SourceGraphOwner = sourceNode.Owner?.SerializedOwner;
                m_SourceGraphAuthoringId = sourceNode.Owner?.GraphAuthoringId ?? string.Empty;
            }
            else if (string.IsNullOrEmpty(sourceNodeGuid))
            {
                m_SourceGraphOwner = null;
                m_SourceGraphAuthoringId = string.Empty;
            }
            if (sourceGraphAuthoringId != null)
                m_SourceGraphAuthoringId = sourceGraphAuthoringId;
            m_SourceNodeGuid = sourceNodeGuid ?? string.Empty;
            ClearRuntimeObservationSelection();
            titleContent = new GUIContent("Timeline Editor");
            m_Timeline = timeline;

            TimelineEditorOpenRequest openRequest = TimelineEditorOpenRequestComposition.Create(
                timeline,
                serializedOwner,
                serializedPropertyPath,
                ownershipLabel);
            if (!BtsmtlSlateTimelineProjection.TryOpen(
                    openRequest,
                    OpenClip,
                    () =>
                    {
                        if (m_SlateSurface != null)
                            m_SlateSurface.MarkDirtyRepaint();
                    },
                    out m_SlateProjection,
                    out string unavailableReason))
            {
                rootVisualElement.Clear();
                rootVisualElement.Add(CreateAuthoringToolbar());
                rootVisualElement.Add(new HelpBox(
                    $"Slate Timeline unavailable: {unavailableReason}",
                    HelpBoxMessageType.Error));
                return;
            }

            m_SlateProjection.AuthoringIssue += OnAuthoringIssue;
            string savedView = SessionState.GetString(TimelineViewStateKey(), string.Empty);
            if (!string.IsNullOrEmpty(savedView))
                m_SlateProjection.RestoreViewState(JsonUtility.FromJson<BtsmtlSlateTimelineViewState>(savedView));
            TimelineWorkspaceModeBridge.ApplyToWindow(this);

            AssetOpened?.Invoke(serializedOwner as TimelineAsset);
            WindowOpened?.Invoke(this);
            rootVisualElement.Clear();
            rootVisualElement.Add(CreateAuthoringToolbar());
            Label ownership = new Label($"Timeline Ownership: {m_OwnershipLabel}");
            ownership.style.unityFontStyleAndWeight = FontStyle.Bold;
            ownership.style.paddingLeft = 8f;
            ownership.style.paddingTop = 4f;
            ownership.style.paddingBottom = 4f;
            rootVisualElement.Add(ownership);
            m_SlateSurface = new IMGUIContainer(DrawSlateSurface)
            {
                name = "slate-timeline-surface"
            };
            m_SlateSurface.style.flexGrow = 1f;
            m_SlateSurface.style.flexShrink = 1f;
            m_SlateSurface.style.minHeight = 320f;
            rootVisualElement.Add(m_SlateSurface);
        }

        void BuildUnboundView()
        {
            titleContent = new GUIContent("Timeline Editor");
            rootVisualElement.Clear();
            rootVisualElement.Add(CreateAuthoringToolbar());
            SetStatus("选择一个 Timeline 资产或从 Skill Graph 打开 Timeline。");
        }

        void ClearBinding()
        {
            ClearNavigation();
            DisposeView();
            m_SerializedOwner = null;
            m_SerializedPropertyPath = string.Empty;
            m_OwnershipLabel = string.Empty;
            m_SourceNodeGuid = string.Empty;
            m_SourceGraphAuthoringId = string.Empty;
            m_SourceGraphWindow = null;
            m_SourceGraphOwner = null;
            m_SourceNode = null;
            ClearRuntimeObservationSelection();
            BuildUnboundView();
        }

        void TryRestoreBinding()
        {
            if (m_SlateProjection != null || !m_SerializedOwner || string.IsNullOrEmpty(m_SerializedPropertyPath))
                return;

            TimelineData timeline = ResolveTimelineData();
            if (timeline == null)
                return;

            Bind(
                timeline,
                m_SerializedOwner,
                m_SerializedPropertyPath,
                m_OwnershipLabel,
                m_SourceGraphWindow,
                null,
                m_SourceNodeGuid,
                m_SourceGraphAuthoringId);
        }

        TimelineData ResolveTimelineData()
        {
            return ResolveTimelineData(m_SerializedOwner, m_SerializedPropertyPath);
        }

        static TimelineData ResolveTimelineData(UnityEngine.Object owner, string propertyPath)
        {
            if (owner is TimelineAsset asset)
                return asset.Data;
            if (!owner || string.IsNullOrWhiteSpace(propertyPath))
                return null;

            SerializedObject serializedObject = new SerializedObject(owner);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            return property?.propertyType == SerializedPropertyType.ManagedReference
                ? property.managedReferenceValue as TimelineData
                : null;
        }

        void OpenClip(Clip clip)
        {
            UnityEngine.Object sourceAsset = TimelineAuthoringClipBinding.SourceAsset(clip);
            if (sourceAsset)
            {
                Selection.activeObject = sourceAsset;
                EditorGUIUtility.PingObject(sourceAsset);
                return;
            }
            if (clip is TreeClip assetClip && assetClip.AssetTree)
            {
                if (OpenAssetTree(assetClip))
                {
                    AssetTreeOpened?.Invoke(m_SerializedOwner as TimelineAsset, assetClip);
                    return;
                }
                ShowNotification(new GUIContent("TreeClip 的正式 FlowCanvas SkillGraph 打开失败。"));
                return;
            }
            if (clip is not TreeClip treeClip)
                return;
            ShowNotification(new GUIContent(
                treeClip.AssetTree
                    ? "TreeClip 的正式 FlowCanvas SkillGraph 打开失败。"
                    : "TreeClip 必须绑定正式 FlowCanvas SkillGraph。"));
        }

        static bool OpenAssetTree(TreeClip clip)
        {
            if (TreeClipOpenRequested == null)
                return false;
            foreach (Func<TreeClip, bool> handler in TreeClipOpenRequested.GetInvocationList())
                if (handler(clip))
                    return true;
            return false;
        }

        bool MatchesSourceNode(TimelineNode node)
        {
            if (ReferenceEquals(m_SourceNode, node))
                return true;
            return m_SourceGraphOwner == node.Owner?.SerializedOwner &&
                   !string.IsNullOrEmpty(m_SourceNodeGuid) &&
                   m_SourceNodeGuid == node.GUID;
        }

        void OnDisable()
        {
            WindowClosed?.Invoke(this);
            TimelineWorkspaceModeBridge.OnWindowClosed(this);
            DisposeView();
        }

        void DisposeView()
        {
            if (m_Timeline != null)
                m_Timeline.OnValueChanged -= OnTimelineValueChanged;
            if (m_SlateProjection != null)
            {
                if (m_SerializedOwner)
                    SessionState.SetString(TimelineViewStateKey(), JsonUtility.ToJson(m_SlateProjection.CaptureViewState()));
                m_SlateProjection.AuthoringIssue -= OnAuthoringIssue;
            }
            m_SlateProjection?.Dispose();
            m_SlateProjection = null;
            m_SlateSurface = null;
            m_Toolbar = null;
            m_WorkspaceModeControls = null;
            m_Timeline = null;
        }

        string TimelineViewStateKey() => "BTSMTL.Timeline.View." +
            GlobalObjectId.GetGlobalObjectIdSlow(m_SerializedOwner) + ":" + m_SerializedPropertyPath;

        void OnAuthoringIssue(string message)
        {
            if (!string.IsNullOrEmpty(message))
                ShowNotification(new GUIContent(message));
        }

        void DrawSlateSurface()
        {
            if (m_SlateProjection == null || m_SlateSurface == null)
                return;
            Rect rect = m_SlateSurface.contentRect;
            m_SlateProjection.DrawEmbeddedGUI(
                Mathf.Max(1f, rect.width),
                Mathf.Max(1f, rect.height),
                BeginWindows,
                EndWindows);
        }

        TimelineEditorBindingState BindingState => new TimelineEditorBindingState(
            m_Timeline,
            m_SerializedOwner,
            m_SerializedPropertyPath,
            m_OwnershipLabel,
            m_SourceGraphWindow,
            m_SourceGraphOwner,
            m_SourceNode,
            m_SourceNodeGuid,
            m_SourceGraphAuthoringId);

        VisualElement CreateAuthoringToolbar()
        {
            m_WorkspaceModeControls = TimelineWorkspaceModeBridge.CreateControls(this);
            m_Toolbar = TimelineEditorToolbarView.Create(
                BindingState,
                HasTimelineNavigation,
                ReturnToTimeline,
                OnSharedTimelineChanged,
                m_WorkspaceModeControls);
            return m_Toolbar.Toolbar;
        }

        bool HasTimelineNavigation => m_NavigationOwner && !string.IsNullOrWhiteSpace(m_NavigationPropertyPath);

        void CaptureTimelineNavigation(AnimationClip clip)
        {
            if (m_SlateProjection == null || !m_SerializedOwner || string.IsNullOrWhiteSpace(m_SerializedPropertyPath))
                throw new InvalidOperationException("Sequence navigation requires a bound Action Timeline.");
            m_NavigationOwner = m_SerializedOwner;
            m_NavigationPropertyPath = m_SerializedPropertyPath;
            m_NavigationOwnershipLabel = m_OwnershipLabel;
            m_NavigationTrackAuthoringId = clip.Track?.AuthoringId ?? string.Empty;
            m_NavigationClipAuthoringId = clip.AuthoringId;
            m_NavigationSourceNodeGuid = m_SourceNodeGuid;
            m_NavigationSourceGraphWindow = m_SourceGraphWindow;
            m_NavigationSourceGraphOwner = m_SourceGraphOwner;
            m_NavigationViewport = Vector2.zero;
        }

        void ReturnToTimeline()
        {
            if (!HasTimelineNavigation)
                return;
            UnityEngine.Object owner = m_NavigationOwner;
            string propertyPath = m_NavigationPropertyPath;
            string ownershipLabel = m_NavigationOwnershipLabel;
            string trackAuthoringId = m_NavigationTrackAuthoringId;
            string clipAuthoringId = m_NavigationClipAuthoringId;
            string sourceNodeGuid = m_NavigationSourceNodeGuid;
            UnityEngine.Object sourceGraphWindow = m_NavigationSourceGraphWindow;
            UnityEngine.Object sourceGraphOwner = m_NavigationSourceGraphOwner;
            TimelineData timeline = ResolveTimelineData(owner, propertyPath);
            if (timeline == null)
                throw new InvalidOperationException("The source Action Timeline can no longer be resolved.");
            ClearNavigation();
            Bind(timeline, owner, propertyPath, ownershipLabel, sourceGraphWindow, null, sourceNodeGuid);
            m_SourceGraphOwner = sourceGraphOwner;
            m_SlateProjection?.FocusSource(trackAuthoringId, clipAuthoringId);
        }

        void ClearNavigation()
        {
            m_NavigationOwner = null;
            m_NavigationPropertyPath = string.Empty;
            m_NavigationOwnershipLabel = string.Empty;
            m_NavigationTrackAuthoringId = string.Empty;
            m_NavigationClipAuthoringId = string.Empty;
            m_NavigationSourceNodeGuid = string.Empty;
            m_NavigationSourceGraphWindow = null;
            m_NavigationSourceGraphOwner = null;
            m_NavigationViewport = Vector2.zero;
            m_Toolbar?.SetBackVisible(false);
        }

        void OnSharedTimelineChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            TimelineAsset asset = evt.newValue as TimelineAsset;
            if (asset)
            {
                ClearNavigation();
                BindAsset(asset, m_SourceGraphAuthoringId, m_SourceNodeGuid);
                return;
            }

            if (m_SerializedOwner is TimelineAsset)
                ClearBinding();
            else
                m_Toolbar?.SetDocumentWithoutNotify(null);
        }

        void SetStatus(string value) => m_Toolbar?.SetStatus(value);

        void OnTimelineValueChanged()
        {
            m_Toolbar?.SetSourceSummary(BindingState.CreateSourceSummary());
            AuthoringRevisionChanged?.Invoke(this);
        }
    }

    [InitializeOnLoad]
    public static class TimelineRuntimeObservationBridge
    {
        static readonly Dictionary<TimelineEditorWindow, RuntimeTimelinePlaybackProjection> s_Projections =
            new Dictionary<TimelineEditorWindow, RuntimeTimelinePlaybackProjection>();

        static TimelineRuntimeObservationBridge()
        {
            RuntimeDebugSession.Shared.Changed += Refresh;
            TimelineEditorWindow.WindowOpened += RefreshWindow;
            TimelineEditorWindow.WindowClosed += ReleaseWindow;
        }

        static void Refresh()
        {
            TimelineEditorWindow[] windows = Resources.FindObjectsOfTypeAll<TimelineEditorWindow>();
            for (int index = 0; index < windows.Length; index++)
                Refresh(windows[index]);
        }

        public static void RefreshWindow(TimelineEditorWindow window)
        {
            Refresh(window);
        }

        static void ReleaseWindow(TimelineEditorWindow window)
        {
            if (window != null)
                s_Projections.Remove(window);
        }

        static void Refresh(TimelineEditorWindow window)
        {
            if (!window || window.Timeline == null)
                return;
            if (!window.RuntimeObservationReadOnly)
            {
                ReleaseWindow(window);
                window.ClearRuntimeTimelineObservation();
                return;
            }
            if (window.TryResolveRuntimeObservation(
                    out RuntimeTimelinePlaybackDebugSummary summary,
                    out string observationMessage))
            {
                ApplyRuntimeTimelineObservation(window, summary);
                return;
            }
            ReleaseWindow(window);
            window.ClearRuntimeTimelineObservation();
            if (!string.IsNullOrEmpty(observationMessage))
                window.SetRuntimeObservationStatus(observationMessage);
        }

        static void ApplyRuntimeTimelineObservation(
            TimelineEditorWindow window,
            RuntimeTimelinePlaybackDebugSummary summary)
        {
            if (!TimelineRuntimePlaybackSnapshotRegistry.TryGet(summary.Playback, out TimelineData sourceTimeline))
            {
                window.ClearRuntimeTimelineObservation();
                window.SetRuntimeObservationStatus("当前 Timeline 实例没有冻结运行内容。");
                return;
            }
            var activeTracks = new Dictionary<string, string>(StringComparer.Ordinal);
            var activeClips = new Dictionary<string, string>(StringComparer.Ordinal);
            IReadOnlyList<RuntimeDebugEventView> events = RuntimeDebugSession.Shared.ViewModel.GetTimelineCurrentEvents(
                sourceTimeline.AuthoringId,
                summary.Playback,
                summary.Provenance.SourceGraphAuthoringId);
            for (int index = 0; index < events.Count; index++)
            {
                RuntimeDebugEventView item = events[index];
                RuntimeSourceElementKey source = item.Source;
                bool presentation = item.Event.Domain == RuntimeTraceDomain.Presentation;
                ulong latestPosition = presentation ? summary.LatestPresentationFrame : summary.LatestLogicTick;
                int cycle = presentation ? summary.VisualCycle : summary.LogicCycle;
                bool terminal = !presentation && summary.IsTerminal;
                string status = !string.IsNullOrEmpty(item.Event.Payload.Status)
                    ? item.Event.Payload.Status
                    : item.Event.Kind.ToString();
                if (source.Kind == RuntimeSourceElementKind.Track && !string.IsNullOrEmpty(source.TrackAuthoringId))
                {
                    if (!terminal && item.Event.Position == latestPosition &&
                        item.Event.Payload.Cycle == cycle)
                        activeTracks.TryAdd(source.TrackAuthoringId, status);
                }
                else if ((source.Kind == RuntimeSourceElementKind.Clip || source.Kind == RuntimeSourceElementKind.TreeClip) &&
                         !string.IsNullOrEmpty(source.ClipAuthoringId))
                {
                    if ((terminal || item.Event.Position < latestPosition ||
                         item.Event.Payload.Cycle != cycle) &&
                        item.Event.Kind is RuntimeTraceEventKind.ClipActive or RuntimeTraceEventKind.TreeClipEntered or RuntimeTraceEventKind.TreeClipUpdated)
                        status = "已执行";
                    activeClips.TryAdd(source.ClipAuthoringId, status);
                }
            }
            RuntimeTimelinePlaybackProjection projection = GetProjection(window);
            TimelineData runtimeTimeline = projection.Update(sourceTimeline, summary.Playback, events, RuntimeDebugSession.Shared.ViewModel, summary);
            MarkOpenTreeClips(runtimeTimeline, activeClips);
            if (RuntimeDebugSession.Shared.AttachmentState is RuntimeDebugAttachmentState.CaptureHistory or RuntimeDebugAttachmentState.Ended)
                window.ApplyHistoryTimelineObservation(runtimeTimeline, projection.StructureChanged, summary.VisualTime, activeTracks, activeClips);
            else
                window.ApplyRuntimeTimelineObservation(runtimeTimeline, projection.StructureChanged, summary.VisualTime, activeTracks, activeClips);
        }

        static RuntimeTimelinePlaybackProjection GetProjection(TimelineEditorWindow window)
        {
            if (!s_Projections.TryGetValue(window, out RuntimeTimelinePlaybackProjection projection))
            {
                projection = new RuntimeTimelinePlaybackProjection();
                s_Projections.Add(window, projection);
            }
            return projection;
        }

        static void MarkOpenTreeClips(
            TimelineData timeline,
            IDictionary<string, string> activeClips)
        {
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                if (timeline.Tracks[trackIndex] is not TreeTrack track)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    if (track.Clips[clipIndex] is TreeClip treeClip &&
                        treeClip.ClipExitSource == TimelineClipExitSource.TreeDecision &&
                        activeClips.TryGetValue(treeClip.AuthoringId, out string status) &&
                        status is "Active" or "Enter" or "Update")
                        activeClips[treeClip.AuthoringId] = "open";
                }
            }
        }

    }
}

