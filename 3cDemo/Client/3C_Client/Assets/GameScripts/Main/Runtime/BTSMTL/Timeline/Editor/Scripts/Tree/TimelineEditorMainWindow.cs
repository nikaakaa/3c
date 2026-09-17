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
        ToolbarButton m_BackButton;
        ObjectField m_SharedTimelineField;
        Label m_SourceSummary;
        Label m_Status;

        public TimelineData Timeline => m_Timeline;
        public UnityEngine.Object SourceGraphWindow => m_SourceGraphWindow;
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
            if (summaries.Count == 1 && summaries[0].Playback.IsValid)
            {
                summary = summaries[0];
                message = string.Empty;
                return true;
            }
            summary = default;
            message = summaries.Count > 1
                ? "当前 Timeline 对应多个运行调用，请从 SkillGraph 选择具体实例。"
                : string.Empty;
            return false;
        }

        internal bool TryResolveDirectRuntimeObservation(
            out TimelineRuntimePlaybackDescriptor descriptor,
            out string message)
        {
            IReadOnlyList<TimelineRuntimePlaybackDescriptor> descriptors =
                TimelineRuntimeService.GetActivePlaybackDescriptors($"timeline:{m_Timeline?.AuthoringId ?? string.Empty}");
            if (descriptors.Count == 1 && descriptors[0].IsValid &&
                (descriptors[0].State == TimelineRuntimePlaybackState.Running ||
                 descriptors[0].State == TimelineRuntimePlaybackState.Stopping))
            {
                descriptor = descriptors[0];
                message = string.Empty;
                return true;
            }
            descriptor = default;
            message = descriptors.Count > 1
                ? "当前 Timeline 对应多个 direct runtime 调用，请从 Graph Shell 选择具体实例。"
                : string.Empty;
            return false;
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
            DisposeView();
        }

        void DisposeView()
        {
            if (m_Timeline != null)
                m_Timeline.OnValueChanged -= OnTimelineValueChanged;
            if (m_SlateProjection != null)
                m_SlateProjection.AuthoringIssue -= OnAuthoringIssue;
            m_SlateProjection?.Dispose();
            m_SlateProjection = null;
            m_SlateSurface = null;
            m_Timeline = null;
        }

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
            m_Status = new Label("运行控制：Skill Graph / Graph Shell");
            m_Status.style.marginLeft = 6f;
            m_Status.style.flexGrow = 1f;
            m_Status.tooltip = "Timeline 只负责作者编辑；Scene Play、Build、Skill 和运行观察由 Graph Shell 管理。";
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
            return $"Source: {ownership} / {m_Timeline.Name}";
        }

        void SetStatus(string value)
        {
            if (m_Status == null)
                return;
            m_Status.text = value ?? string.Empty;
            m_Status.tooltip = value ?? string.Empty;
        }

        void OnTimelineValueChanged()
        {
            if (m_SourceSummary != null)
                m_SourceSummary.text = CurrentSourceSummary();
            AuthoringRevisionChanged?.Invoke(this);
        }
    }

    [InitializeOnLoad]
    static class TimelineRuntimeObservationBridge
    {
        static TimelineRuntimeObservationBridge()
        {
            RuntimeDebugSession.Shared.Changed += Refresh;
            TimelineRuntimeService.ObservationChanged += Refresh;
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
            if (!window.TryResolveRuntimeObservation(
                    out RuntimeTimelinePlaybackDebugSummary summary,
                    out string observationMessage))
            {
                if (window.TryResolveDirectRuntimeObservation(
                        out TimelineRuntimePlaybackDescriptor direct,
                        out string directMessage))
                {
                    ApplyDirectRuntimeObservation(window, direct);
                    return;
                }
                window.ClearRuntimeObservation();
                string message = !string.IsNullOrEmpty(observationMessage)
                    ? observationMessage
                    : directMessage;
                if (!string.IsNullOrEmpty(message))
                    window.SetRuntimeObservationStatus(message);
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
            window.ApplyRuntimeObservation(summary.VisualTime, activeTracks, activeClips);
        }

        static void ApplyDirectRuntimeObservation(
            TimelineEditorWindow window,
            TimelineRuntimePlaybackDescriptor descriptor)
        {
            var activeClips = new Dictionary<string, string>(StringComparer.Ordinal);
            var activeClipIds = new HashSet<string>(descriptor.ActiveClipIds, StringComparer.Ordinal);
            for (int index = 0; index < descriptor.ActiveClipIds.Count; index++)
                activeClips[descriptor.ActiveClipIds[index]] = descriptor.State.ToString();

            var activeTracks = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int trackIndex = 0; trackIndex < window.Timeline.Tracks.Count; trackIndex++)
            {
                Track track = window.Timeline.Tracks[trackIndex];
                if (track == null)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip != null && activeClipIds.Contains(clip.AuthoringId))
                    {
                        activeTracks[track.AuthoringId] = descriptor.State.ToString();
                        break;
                    }
                }
            }

            float visualTime = descriptor.CursorFrame /
                (float)Mathf.Max(1, TimelineUtility.FrameRate);
            window.ApplyRuntimeObservation(visualTime, activeTracks, activeClips);
        }
    }
}

