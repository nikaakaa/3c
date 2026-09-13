using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using Slate;
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
        string m_ViewTimelineAuthoringId;

        [SerializeField]
        BtsmtlSlateTimelineViewState m_ViewState;

        TimelineNode m_SourceNode;
        BtsmtlSlateTimelineDirectProjection m_SlateProjection;
        IMGUIContainer m_SlateSurface;
        TimelineData m_Timeline;

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
            Selection.activeObject = null;

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
                rootVisualElement.Add(new HelpBox(
                    $"Slate Timeline unavailable: {unavailableReason}",
                    HelpBoxMessageType.Error));
                return;
            }

            AssetOpened?.Invoke(serializedOwner as TimelineAsset);
            rootVisualElement.Clear();
            m_SlateSurface = new IMGUIContainer(DrawSlateSurface)
            {
                name = "slate-timeline-surface"
            };
            m_SlateSurface.style.flexGrow = 1f;
            m_SlateSurface.style.flexShrink = 1f;
            m_SlateSurface.style.minHeight = 320f;
            m_SlateProjection.ConfigureRepaint(() => m_SlateSurface?.MarkDirtyRepaint());
            m_SlateProjection.AuthoringIssue += OnAuthoringIssue;
            rootVisualElement.Add(m_SlateSurface);
            if (string.Equals(m_ViewTimelineAuthoringId, timeline.AuthoringId, StringComparison.Ordinal))
                m_SlateProjection.RestoreViewState(m_ViewState);
            WindowOpened?.Invoke(this);
        }

        void BuildUnboundView()
        {
            titleContent = new GUIContent("Timeline Editor");
            rootVisualElement.Clear();
        }

        void ClearBinding()
        {
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
                m_SlateProjection.AuthoringIssue -= OnAuthoringIssue;
            }
            m_SlateProjection?.Dispose();
            m_SlateProjection = null;
            m_SlateSurface = null;
            m_Timeline = null;
        }

        void OnTimelineValueChanged()
        {
            AuthoringRevisionChanged?.Invoke(this);
        }

        void OnAuthoringIssue(string message)
        {
            SetStatus(message);
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

        void SetStatus(string value)
        {
            if (!string.IsNullOrEmpty(value))
                ShowNotification(new GUIContent(value));
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
