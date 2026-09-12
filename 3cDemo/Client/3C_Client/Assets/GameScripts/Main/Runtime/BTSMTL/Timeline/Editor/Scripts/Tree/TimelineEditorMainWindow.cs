using System;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor
{
    public sealed class TimelineEditorWindow : EditorWindow
    {
        public static event Action<TimelineAsset> AssetOpened;
        public static event Action<TimelineAsset, TreeClip> AssetTreeOpened;

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

        TimelineNode m_SourceNode;
        BtsmtlSlateTimelineProjection m_SlateProjection;
        IMGUIContainer m_SlateSurface;
        TimelineData m_Timeline;
        ToolbarButton m_BackButton;
        ObjectField m_SharedTimelineField;
        Label m_SourceSummary;
        Label m_Status;
        VisualElement m_DetailsHost;

        public TimelineData Timeline => m_Timeline;
        public BaseTreeWindow SourceGraphWindow => m_SourceGraphWindow;

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
            TryRestoreBinding();
            if (m_SlateProjection == null)
                BuildUnboundView();
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

            TimelineEditorOpenRequest openRequest = TimelineEditorOpenRequestComposition.Create(
                timeline,
                serializedOwner,
                serializedPropertyPath,
                ownershipLabel,
                sourceGraphWindow);
            if (!BtsmtlSlateTimelineProjection.TryOpen(
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
            rootVisualElement.Add(m_SlateSurface);
            m_DetailsHost = new VisualElement { name = "timeline-details" };
            m_DetailsHost.style.flexShrink = 0f;
            m_DetailsHost.style.maxHeight = 260f;
            m_DetailsHost.style.paddingLeft = 8f;
            m_DetailsHost.style.paddingRight = 8f;
            m_DetailsHost.style.paddingTop = 4f;
            m_DetailsHost.style.paddingBottom = 4f;
            rootVisualElement.Add(m_DetailsHost);
            m_SlateProjection.SelectionChanged += RebuildDetails;
            RebuildDetails(m_SlateProjection.Selection);
            if (string.Equals(m_ViewTimelineAuthoringId, timeline.AuthoringId, StringComparison.Ordinal))
                m_SlateProjection.RestoreViewState(m_ViewState);
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
            EditorApplication.update -= OnEditorUpdate;
            DisposeView();
        }

        void DisposeView()
        {
            CaptureViewState();
            if (m_SlateProjection != null)
                m_SlateProjection.SelectionChanged -= RebuildDetails;
            m_SlateProjection?.Dispose();
            m_SlateProjection = null;
            m_SlateSurface = null;
            m_DetailsHost = null;
            m_Timeline = null;
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
                m_DetailsHost.style.display = DisplayStyle.None;
                return;
            }

            m_DetailsHost.style.display = DisplayStyle.Flex;
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
                return;
            }

            if (selection.Track != null)
            {
                Track track = selection.Track;
                m_DetailsHost.Add(new Label($"{track.Name}  |  {track.ContractKind}  |  Clips {track.Clips.Count}"));
            }
        }

        VisualElement CreateAuthoringToolbar()
        {
            var toolbar = new Toolbar();
            m_BackButton = new ToolbarButton(ReturnToTimeline) { text = "‹ Timeline" };
            m_BackButton.style.display = HasTimelineNavigation ? DisplayStyle.Flex : DisplayStyle.None;
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
            m_Status = new Label($"Frame {TimelineUtility.FrameRate}");
            m_Status.style.marginLeft = 6f;
            m_Status.style.flexGrow = 1f;
            m_Status.tooltip = "Timeline 使用正式作者帧编辑。角色 Scene Play、Build、Skill 和运行观察由 Skill Graph / Graph Shell 管理。";
            toolbar.Add(m_BackButton);
            toolbar.Add(m_SharedTimelineField);
            toolbar.Add(m_SourceSummary);
            toolbar.Add(m_Status);
            return toolbar;
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

        void SetStatus(string value)
        {
            if (m_Status == null)
                return;
            m_Status.text = value ?? string.Empty;
            m_Status.tooltip = value ?? string.Empty;
        }
    }
}
