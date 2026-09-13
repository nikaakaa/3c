using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor
{
    public sealed class TimelineEditorWindow : EditorWindow
    {
        const float DetailsMinWidth = 380f;
        const float DetailsMaxWidth = 560f;
        const float DetailsInitialWidth = 420f;
        const float DetailsCollapseThreshold = 1080f;

        enum RuntimeObservationSelectionMode : byte
        {
            Automatic = 0,
            FollowLatest = 1,
            Pinned = 2
        }

        public static event Action<TimelineAsset> AssetOpened;
        public static event Action<TimelineAsset, TreeClip> AssetTreeOpened;
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
        BaseTreeWindow m_SourceGraphWindow;

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
        BaseTreeWindow m_NavigationSourceGraphWindow;

        [SerializeField]
        UnityEngine.Object m_NavigationSourceGraphOwner;

        [SerializeField]
        Vector2 m_NavigationViewport;

        [SerializeField]
        string m_ViewTimelineAuthoringId;

        [SerializeField]
        BtsmtlSlateTimelineViewState m_ViewState;

        [SerializeField]
        float m_DetailsWidth = DetailsInitialWidth;

        [SerializeField]
        bool m_DetailsCollapsed;

        [SerializeField]
        RuntimeObservationSelectionMode m_RuntimeObservationSelectionMode;

        TimelineNode m_SourceNode;
        BtsmtlSlateTimelineDirectProjection m_SlateProjection;
        IMGUIContainer m_SlateSurface;
        TimelineData m_Timeline;
        ToolbarButton m_BackButton;
        ObjectField m_SharedTimelineField;
        Label m_SourceSummary;
        Label m_RevisionSummary;
        Label m_Status;
        ToolbarMenu m_RuntimeObservationMenu;
        TwoPaneSplitView m_WorkspaceSplit;
        VisualElement m_DetailsPane;
        VisualElement m_DetailsHost;
        ToolbarButton m_DetailsToggle;
        bool m_NarrowDetailsCollapsed;
        RuntimeInstanceKey m_PinnedRuntimePlayback;
        bool m_HasPinnedRuntimePlayback;

        public TimelineData Timeline => m_Timeline;
        public BaseTreeWindow SourceGraphWindow => m_SourceGraphWindow;
        public string SourceGraphAuthoringId => m_SourceGraphAuthoringId ?? string.Empty;
        public string SourceNodeAuthoringId => m_SourceNodeGuid ?? string.Empty;
        public string AuthoringRevision => m_Timeline == null
            ? string.Empty
            : TimelineAuthoringFingerprint.Compute(m_Timeline);

        internal IReadOnlyList<RuntimeTimelinePlaybackDebugSummary> GetRuntimeObservationSummaries()
        {
            if (m_Timeline == null)
                return Array.Empty<RuntimeTimelinePlaybackDebugSummary>();
            IReadOnlyList<RuntimeTimelinePlaybackDebugSummary> summaries =
                RuntimeDebugSession.Shared.ViewModel.GetTimelinePlaybackSummaries(
                    m_Timeline.AuthoringId,
                    SourceGraphAuthoringId);
            if (string.IsNullOrEmpty(SourceNodeAuthoringId))
                return summaries;
            return summaries
                .Where(value => string.Equals(
                    value.Provenance.SourceNodeAuthoringId,
                    SourceNodeAuthoringId,
                    StringComparison.Ordinal))
                .ToArray();
        }

        internal bool TryResolveRuntimeObservation(
            out RuntimeTimelinePlaybackDebugSummary summary,
            out string message)
        {
            IReadOnlyList<RuntimeTimelinePlaybackDebugSummary> summaries = GetRuntimeObservationSummaries();
            if (m_RuntimeObservationSelectionMode == RuntimeObservationSelectionMode.Pinned)
            {
                for (int index = 0; index < summaries.Count; index++)
                    if (summaries[index].Playback.Equals(m_PinnedRuntimePlayback) && summaries[index].Playback.IsValid)
                    {
                        summary = summaries[index];
                        message = string.Empty;
                        return true;
                    }
                summary = default;
                message = "固定的运行调用已不在当前诊断记录中。";
                return false;
            }
            if (summaries.Count != 0 &&
                (m_RuntimeObservationSelectionMode == RuntimeObservationSelectionMode.FollowLatest || summaries.Count == 1) &&
                summaries[0].Playback.IsValid)
            {
                summary = summaries[0];
                message = string.Empty;
                return true;
            }
            summary = default;
            message = summaries.Count > 1
                ? "当前 Timeline 对应多个运行调用，请选择跟随最新或固定具体实例。"
                : string.Empty;
            return false;
        }

        internal void RefreshRuntimeObservationMenu()
        {
            if (m_RuntimeObservationMenu == null)
                return;
            m_RuntimeObservationMenu.menu.MenuItems().Clear();
            m_RuntimeObservationMenu.menu.AppendAction(
                "自动（仅唯一调用）",
                _ => SetRuntimeObservationAutomatic());
            m_RuntimeObservationMenu.menu.AppendAction(
                "跟随最新调用",
                _ => SetRuntimeObservationFollowLatest());
            IReadOnlyList<RuntimeTimelinePlaybackDebugSummary> summaries = GetRuntimeObservationSummaries();
            for (int index = 0; index < summaries.Count; index++)
            {
                RuntimeTimelinePlaybackDebugSummary candidate = summaries[index];
                RuntimeInstanceKey playback = candidate.Playback;
                m_RuntimeObservationMenu.menu.AppendAction(
                    RuntimeObservationLabel(candidate),
                    _ => SetRuntimeObservationPinned(playback));
            }
            m_RuntimeObservationMenu.text = RuntimeObservationSelectionLabel(summaries);
            m_RuntimeObservationMenu.tooltip = summaries.Count == 0
                ? "没有 Timeline 运行调用"
                : "选择自动、跟随最新或固定具体运行调用";
        }

        void SetRuntimeObservationAutomatic()
        {
            m_RuntimeObservationSelectionMode = RuntimeObservationSelectionMode.Automatic;
            m_HasPinnedRuntimePlayback = false;
            RefreshRuntimeObservationMenu();
            TimelineRuntimeObservationBridge.RefreshWindow(this);
        }

        void SetRuntimeObservationFollowLatest()
        {
            m_RuntimeObservationSelectionMode = RuntimeObservationSelectionMode.FollowLatest;
            m_HasPinnedRuntimePlayback = false;
            RefreshRuntimeObservationMenu();
            TimelineRuntimeObservationBridge.RefreshWindow(this);
        }

        void SetRuntimeObservationPinned(RuntimeInstanceKey playback)
        {
            m_RuntimeObservationSelectionMode = RuntimeObservationSelectionMode.Pinned;
            m_PinnedRuntimePlayback = playback;
            m_HasPinnedRuntimePlayback = playback.IsValid;
            RefreshRuntimeObservationMenu();
            TimelineRuntimeObservationBridge.RefreshWindow(this);
        }

        static string RuntimeObservationLabel(RuntimeTimelinePlaybackDebugSummary summary)
        {
            RuntimeInstanceKey playback = summary.Playback;
            return $"固定 #{playback.TimelinePlaybackId} / Action {playback.ActionInstanceId} / Tick {summary.LatestLogicTick}";
        }

        string RuntimeObservationSelectionLabel(IReadOnlyList<RuntimeTimelinePlaybackDebugSummary> summaries)
        {
            return m_RuntimeObservationSelectionMode switch
            {
                RuntimeObservationSelectionMode.FollowLatest => "Runtime: 跟随最新",
                RuntimeObservationSelectionMode.Pinned when m_HasPinnedRuntimePlayback =>
                    $"Runtime: 固定 #{m_PinnedRuntimePlayback.TimelinePlaybackId}",
                _ => summaries.Count == 1 ? "Runtime: 自动" : "Runtime: 选择调用"
            };
        }

        public bool FocusSource(string trackAuthoringId, string clipAuthoringId)
        {
            return m_SlateProjection != null &&
                   m_SlateProjection.FocusSource(trackAuthoringId, clipAuthoringId);
        }

        public static TimelineEditorWindow Open(BaseTreeWindow sourceGraphWindow, TimelineNode node)
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

        public void ApplyRuntimeObservation(
            float visualTime,
            IReadOnlyDictionary<string, string> activeTracks,
            IReadOnlyDictionary<string, string> activeClips)
        {
            m_SlateProjection?.ApplyRuntimeOverlay(visualTime, activeTracks, activeClips);
        }

        public void ApplyHistoryObservation(
            float visualTime,
            IReadOnlyDictionary<string, string> activeTracks,
            IReadOnlyDictionary<string, string> activeClips)
        {
            m_SlateProjection?.ApplyHistoryOverlay(visualTime, activeTracks, activeClips);
        }

        public void ClearRuntimeObservation()
        {
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
            RefreshRuntimeObservationMenu();
        }

        void BindAsset(TimelineAsset asset, string sourceGraphAuthoringId, string sourceNodeGuid)
        {
            if (!asset)
                throw new ArgumentNullException(nameof(asset));
            m_SourceGraphOwner = null;
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
            rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            TryRestoreBinding();
            if (m_SlateProjection == null)
                BuildUnboundView();
        }

        void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            m_NarrowDetailsCollapsed = evt.newRect.width < DetailsCollapseThreshold || evt.newRect.height < 360f;
            ApplyDetailsVisibility();
        }

        void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
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

        void BindNode(BaseTreeWindow sourceGraphWindow, TimelineNode node)
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
            BaseTreeWindow sourceGraphWindow,
            TimelineNode sourceNode,
            string sourceNodeGuid,
            string sourceGraphAuthoringId = null)
        {
            if (timeline == null || !serializedOwner || string.IsNullOrEmpty(serializedPropertyPath))
                throw new InvalidOperationException("TimelineEditorWindow requires a bound TimelineData owner/path.");

            CaptureViewState();
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
            titleContent = new GUIContent("Timeline Editor");
            m_Timeline = timeline;
            m_RuntimeObservationSelectionMode = RuntimeObservationSelectionMode.Automatic;
            m_HasPinnedRuntimePlayback = false;

            TimelineEditorOpenRequest openRequest = TimelineEditorOpenRequestComposition.Create(
                timeline,
                serializedOwner,
                serializedPropertyPath,
                ownershipLabel,
                sourceGraphWindow);
            if (!BtsmtlSlateTimelineDirectProjection.TryOpen(
                    openRequest,
                    OpenClip,
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

            AssetOpened?.Invoke(serializedOwner as TimelineAsset);
            rootVisualElement.Clear();
            rootVisualElement.Add(CreateAuthoringToolbar());
            m_SlateSurface = new IMGUIContainer(DrawSlateSurface)
            {
                name = "slate-timeline-surface"
            };
            m_SlateSurface.style.flexGrow = 1f;
            m_SlateSurface.style.flexShrink = 1f;
            m_SlateSurface.style.minHeight = 320f;
            m_SlateProjection.ConfigureRepaint(() => m_SlateSurface?.MarkDirtyRepaint());
            m_WorkspaceSplit = new TwoPaneSplitView(
                1,
                Mathf.Clamp(m_DetailsWidth, DetailsMinWidth, DetailsMaxWidth),
                TwoPaneSplitViewOrientation.Horizontal)
            {
                name = "timeline-workspace"
            };
            m_WorkspaceSplit.style.flexGrow = 1f;
            m_WorkspaceSplit.style.flexShrink = 1f;
            m_WorkspaceSplit.style.minHeight = 320f;
            m_WorkspaceSplit.Add(m_SlateSurface);
            m_DetailsPane = new VisualElement { name = "timeline-details-pane" };
            m_DetailsPane.style.flexGrow = 1f;
            m_DetailsPane.style.flexShrink = 0f;
            m_DetailsPane.style.minWidth = DetailsMinWidth;
            m_DetailsPane.style.maxWidth = DetailsMaxWidth;
            m_DetailsPane.Add(CreateDetailsHeader());
            m_DetailsHost = new ScrollView { name = "timeline-details" };
            m_DetailsHost.style.flexGrow = 1f;
            m_DetailsHost.style.paddingLeft = 8f;
            m_DetailsHost.style.paddingRight = 8f;
            m_DetailsHost.style.paddingTop = 4f;
            m_DetailsHost.style.paddingBottom = 4f;
            m_DetailsPane.Add(m_DetailsHost);
            m_WorkspaceSplit.Add(m_DetailsPane);
            m_WorkspaceSplit.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (!m_DetailsCollapsed && m_WorkspaceSplit.fixedPane != null)
                {
                    float width = m_WorkspaceSplit.fixedPane.resolvedStyle.width;
                    if (width > 0f)
                        m_DetailsWidth = Mathf.Clamp(width, DetailsMinWidth, DetailsMaxWidth);
                }
            });
            rootVisualElement.Add(m_WorkspaceSplit);
            ApplyDetailsVisibility();
            m_SlateProjection.SelectionChanged += RebuildDetails;
            m_SlateProjection.AuthoringIssue += OnAuthoringIssue;
            RebuildDetails(m_SlateProjection.Selection);
            if (string.Equals(m_ViewTimelineAuthoringId, timeline.AuthoringId, StringComparison.Ordinal))
                m_SlateProjection.RestoreViewState(m_ViewState);
            WindowOpened?.Invoke(this);
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
            if (clip is AnimationClip animationClip && animationClip.Clip)
            {
                Selection.activeObject = animationClip.Clip;
                EditorGUIUtility.PingObject(animationClip.Clip);
                return;
            }
            if (clip is TreeClip assetClip && assetClip.AssetTree)
            {
                AssetDatabase.OpenAsset(assetClip.AssetTree);
                AssetTreeOpened?.Invoke(m_SerializedOwner as TimelineAsset, assetClip);
                return;
            }
            if (!(clip is TreeClip treeClip) || treeClip.ResolvedTree == null)
                return;

            BaseTreeWindow graphWindow = m_SourceGraphWindow;
            if (!graphWindow)
                graphWindow = TreeWindowUtility.TreeWindowUtilityInstance.OpenBaseTreeWindow();
            if (m_SourceGraphWindow && m_SourceGraphWindow.AuthoringContext != null)
                graphWindow.SetAuthoringContext(m_SourceGraphWindow.AuthoringContext);

            string identity = $"{treeClip.Track?.Name}:{treeClip.StartFrame}:{treeClip.Name}";
            graphWindow.PushTreePage(
                treeClip.ResolvedTree,
                treeClip.SharedTreeAsset,
                treeClip.Name,
                identity,
                "TreeClip",
                AuthoringPageKind.TreeClip);
            graphWindow.Show();
            graphWindow.Focus();
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
            EditorApplication.update -= OnEditorUpdate;
            DisposeView();
        }

        void DisposeView()
        {
            CaptureViewState();
            if (m_Timeline != null)
                m_Timeline.OnValueChanged -= OnTimelineValueChanged;
            if (m_SlateProjection != null)
            {
                m_SlateProjection.SelectionChanged -= RebuildDetails;
                m_SlateProjection.AuthoringIssue -= OnAuthoringIssue;
            }
            m_SlateProjection?.Dispose();
            m_SlateProjection = null;
            m_SlateSurface = null;
            m_WorkspaceSplit = null;
            m_DetailsPane = null;
            m_DetailsHost = null;
            m_RevisionSummary = null;
            m_Timeline = null;
        }

        void OnTimelineValueChanged()
        {
            if (m_RevisionSummary != null)
                m_RevisionSummary.text = AuthoringRevisionLabel();
            AuthoringRevisionChanged?.Invoke(this);
        }

        string AuthoringRevisionLabel()
        {
            string revision = AuthoringRevision;
            return string.IsNullOrEmpty(revision)
                ? "Authoring: -"
                : $"Authoring: {revision.Substring(0, Math.Min(8, revision.Length))}";
        }

        void OnAuthoringIssue(string message)
        {
            SetStatus(string.IsNullOrEmpty(message) ? $"Frame {TimelineUtility.FrameRate}" : message);
        }

        void CaptureViewState()
        {
            if (m_SlateProjection == null || m_Timeline == null)
                return;
            m_ViewTimelineAuthoringId = m_Timeline.AuthoringId;
            m_ViewState = m_SlateProjection.CaptureViewState();
        }

        void OnEditorUpdate()
        {
            m_SlateSurface?.MarkDirtyRepaint();
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

        void RebuildDetails(TimelineEditorSelection selection)
        {
            if (m_DetailsHost == null)
                return;
            m_DetailsHost.Clear();
            if (selection.Kind == TimelineEditorSelectionKind.None)
            {
                m_DetailsHost.Add(new Label("选择 Track、Clip 或关键帧查看属性。"));
                ApplyDetailsVisibility();
                return;
            }

            if (selection.Clip != null)
            {
                Clip clip = selection.Clip;
                m_DetailsHost.Add(new Label(
                    $"{clip.Name}  |  {clip.ContractKind}  |  Frame {clip.StartFrame}..{clip.EndFrame}"));
                m_DetailsHost.Add(new Button(() => OpenClip(clip)) { text = "Open Source" });
                if (clip is MotionWarpClip)
                    m_DetailsHost.Add(new MotionWarpClipInspectorView(clip));
                else if (clip is TreeClip treeClip)
                {
                    var inspector = new TreeClipInspectorView(treeClip);
                    inspector.Initialize(OpenClip);
                    m_DetailsHost.Add(inspector);
                }
                else
                    m_DetailsHost.Add(new TimelineFormalClipDetailsView(clip, m_SlateProjection.ApplyFormalMutation));
                ApplyDetailsVisibility();
                return;
            }

            if (selection.Track != null)
            {
                Track track = selection.Track;
                m_DetailsHost.Add(new Label($"{track.Name}  |  {track.ContractKind}  |  Clips {track.Clips.Count}"));
            }
            ApplyDetailsVisibility();
        }

        VisualElement CreateDetailsHeader()
        {
            var toolbar = new Toolbar { name = "timeline-details-header" };
            var title = new Label("Inspector");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            toolbar.Add(title);
            return toolbar;
        }

        void ApplyDetailsVisibility()
        {
            if (m_DetailsToggle != null)
                m_DetailsToggle.text = m_DetailsCollapsed || m_NarrowDetailsCollapsed
                    ? "Inspector ▸"
                    : "Inspector ▾";
            if (m_WorkspaceSplit == null)
                return;
            if (m_DetailsCollapsed || m_NarrowDetailsCollapsed)
                m_WorkspaceSplit.CollapseChild(1);
            else
                m_WorkspaceSplit.UnCollapse();
        }

        VisualElement CreateAuthoringToolbar()
        {
            var toolbar = new Toolbar();
            m_BackButton = new ToolbarButton(ReturnToTimeline) { text = "‹ Timeline" };
            m_BackButton.style.display = HasTimelineNavigation ? DisplayStyle.Flex : DisplayStyle.None;
            var previewButton = new ToolbarButton(ReturnToPreview) { text = "Preview" };
            previewButton.style.display = m_SourceGraphWindow ? DisplayStyle.Flex : DisplayStyle.None;
            m_SharedTimelineField = new ObjectField("Document")
            {
                objectType = typeof(UnityEngine.Object),
                allowSceneObjects = false
            };
            m_SharedTimelineField.style.width = 280f;
            m_SharedTimelineField.SetValueWithoutNotify(m_SerializedOwner as TimelineAsset);
            m_SharedTimelineField.RegisterValueChangedCallback(OnSharedTimelineChanged);
            m_SourceSummary = new Label(CurrentSourceSummary());
            m_SourceSummary.style.minWidth = 180f;
            m_SourceSummary.style.marginLeft = 6f;
            m_SourceSummary.tooltip = CurrentSourceTooltip();
            m_RevisionSummary = new Label(AuthoringRevisionLabel());
            m_RevisionSummary.style.marginLeft = 6f;
            m_RevisionSummary.tooltip = "TimelineData 当前作者内容指纹；不代表运行时已采用。";
            m_DetailsToggle = new ToolbarButton(() =>
            {
                m_DetailsCollapsed = !m_DetailsCollapsed;
                ApplyDetailsVisibility();
            });
            m_DetailsToggle.style.width = 100f;
            m_RuntimeObservationMenu = new ToolbarMenu { text = "Runtime: 选择调用" };
            m_RuntimeObservationMenu.style.width = 150f;
            m_Status = new Label($"Frame {TimelineUtility.FrameRate}");
            m_Status.style.marginLeft = 6f;
            m_Status.style.flexGrow = 1f;
            m_Status.tooltip = "Timeline 使用正式作者帧编辑。角色 Scene Play、Build、Skill 和运行观察由 Skill Graph / Graph Shell 管理。";
            toolbar.Add(m_BackButton);
            toolbar.Add(previewButton);
            toolbar.Add(m_SharedTimelineField);
            toolbar.Add(m_SourceSummary);
            toolbar.Add(m_RevisionSummary);
            toolbar.Add(m_DetailsToggle);
            toolbar.Add(m_RuntimeObservationMenu);
            toolbar.Add(m_Status);
            RefreshRuntimeObservationMenu();
            return toolbar;
        }

        void ReturnToPreview()
        {
            if (!m_SourceGraphWindow)
            {
                SetStatus("当前 Timeline 没有绑定 Graph Shell 上下文。");
                return;
            }
            m_SourceGraphWindow.Show();
            m_SourceGraphWindow.Focus();
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
            BaseTreeWindow sourceGraphWindow = m_NavigationSourceGraphWindow;
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
            if (m_BackButton != null)
                m_BackButton.style.display = DisplayStyle.None;
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
                m_SharedTimelineField.SetValueWithoutNotify(null);
        }

        string CurrentSourceSummary()
        {
            if (m_Timeline == null)
                return "Source: None";
            string ownership = string.IsNullOrWhiteSpace(m_OwnershipLabel) ? "Timeline" : m_OwnershipLabel;
            return ownership;
        }

        string CurrentSourceTooltip()
        {
            if (m_Timeline == null)
                return "Source: None";
            string ownership = string.IsNullOrWhiteSpace(m_OwnershipLabel) ? "Timeline" : m_OwnershipLabel;
            return $"Source: {ownership} / {m_Timeline.Name}";
        }

        void SetStatus(string value)
        {
            if (m_Status == null)
                return;
            m_Status.text = value ?? string.Empty;
            m_Status.tooltip = value ?? string.Empty;
        }
    }

    [InitializeOnLoad]
    static class TimelineRuntimeObservationBridge
    {
        static TimelineRuntimeObservationBridge()
        {
            RuntimeDebugSession.Shared.Changed += Refresh;
            TimelineEditorWindow.WindowOpened += RefreshWindow;
        }

        static void Refresh()
        {
            TimelineEditorWindow[] windows = Resources.FindObjectsOfTypeAll<TimelineEditorWindow>();
            for (int index = 0; index < windows.Length; index++)
                Refresh(windows[index]);
        }

        internal static void RefreshWindow(TimelineEditorWindow window)
        {
            Refresh(window);
        }

        static void Refresh(TimelineEditorWindow window)
        {
            if (!window || window.Timeline == null)
                return;
            window.RefreshRuntimeObservationMenu();
            if (!window.TryResolveRuntimeObservation(
                    out RuntimeTimelinePlaybackDebugSummary summary,
                    out string observationMessage))
            {
                window.ClearRuntimeObservation();
                if (!string.IsNullOrEmpty(observationMessage))
                    window.SetRuntimeObservationStatus(observationMessage);
                return;
            }
            var activeTracks = new Dictionary<string, string>(StringComparer.Ordinal);
            var activeClips = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (RuntimeDebugEventView item in RuntimeDebugSession.Shared.ViewModel.GetTimelineCurrentEvents(
                         window.Timeline.AuthoringId,
                         summary.Playback,
                         window.SourceGraphAuthoringId))
            {
                RuntimeSourceElementKey source = item.Source;
                string status = !string.IsNullOrEmpty(item.Event.Payload.Status)
                    ? item.Event.Payload.Status
                    : item.Event.Kind.ToString();
                if (source.Kind == RuntimeSourceElementKind.Track && !string.IsNullOrEmpty(source.TrackAuthoringId))
                    activeTracks[source.TrackAuthoringId] = status;
                else if ((source.Kind == RuntimeSourceElementKind.Clip || source.Kind == RuntimeSourceElementKind.TreeClip) &&
                         !string.IsNullOrEmpty(source.ClipAuthoringId))
                    activeClips[source.ClipAuthoringId] = status;
            }
            if (RuntimeDebugSession.Shared.AttachmentState == RuntimeDebugAttachmentState.CaptureHistory)
                window.ApplyHistoryObservation(summary.VisualTime, activeTracks, activeClips);
            else
                window.ApplyRuntimeObservation(summary.VisualTime, activeTracks, activeClips);
        }
    }
}
