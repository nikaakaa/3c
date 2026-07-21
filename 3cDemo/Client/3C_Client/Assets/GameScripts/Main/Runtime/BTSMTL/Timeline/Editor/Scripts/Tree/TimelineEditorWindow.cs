using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor
{
    public enum TimelineWindowMode
    {
        AuthoringPreview,
        LiveDebug
    }

    public sealed class TimelineEditorWindow : EditorWindow
    {
        sealed class TimelineRuntimeDebugBinding : ITimelineEditorRuntimeDebugBinding
        {
            public TimelineRuntimeDebugBinding(string timelineAuthoringId)
            {
                BindingId = string.IsNullOrWhiteSpace(timelineAuthoringId)
                    ? throw new ArgumentException("Runtime Debug Timeline identity is invalid.", nameof(timelineAuthoringId))
                    : timelineAuthoringId;
            }

            public string BindingId { get; }
        }

        [SerializeField]
        UnityEngine.Object m_SerializedOwner;

        [SerializeField]
        string m_SerializedPropertyPath;

        [SerializeField]
        string m_OwnershipLabel;

        [SerializeField]
        string m_SourceNodeGuid;

        [SerializeField]
        BaseTreeWindow m_SourceGraphWindow;

        [SerializeField]
        UnityEngine.Object m_SourceGraphOwner;

        TimelineNode m_SourceNode;
        TimelineEditorView m_View;

        [SerializeField]
        TimelineWindowMode m_Mode;
        ToolbarToggle m_AuthoringToggle;
        ToolbarToggle m_LiveDebugToggle;
        ObjectField m_SharedTimelineField;
        Label m_SourceSummary;
        ToolbarMenu m_TargetMenu;
        ToolbarMenu m_PlaybackMenu;
        ToolbarToggle m_FollowToggle;
        ToolbarToggle m_LiveToggle;
        ToolbarButton m_CaptureButton;
        SliderInt m_HistorySlider;
        Label m_Status;
        ScrollView m_DebugDetails;
        RuntimeDebugViewBinding m_DebugBinding;
        RuntimeDebugTargetRequest m_DebugRequest;
        bool m_HasDebugRequest;
        RuntimeDebugViewModel m_LastDebugView;
        RuntimeInstanceKey m_LastDebugPlayback;
        long m_LastDebugRevision = -1;
        long m_LastDebugMenuTargetRevision = -1;
        long m_LastDebugTimelinePlaybackRevision = -1;

        public TimelineData Timeline => m_View?.Timeline;
        public BaseTreeWindow SourceGraphWindow => m_SourceGraphWindow;

        public bool FocusSource(string trackAuthoringId, string clipAuthoringId)
        {
            return m_View != null && m_View.FocusSource(trackAuthoringId, clipAuthoringId);
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
            if (!asset)
                return null;

            TimelineEditorWindow window = GetWindow<TimelineEditorWindow>();
            window.BindAsset(asset);
            window.Show();
            window.Focus();
            return window;
        }

        void BindAsset(TimelineAsset asset)
        {
            if (!asset)
                throw new ArgumentNullException(nameof(asset));
            Bind(asset.Data, asset, "m_Data", "Shared Asset", null, null, string.Empty);
        }

        public static void RebindIfOpen(TimelineNode node)
        {
            if (node == null)
                return;

            TimelineEditorWindow[] windows = Resources.FindObjectsOfTypeAll<TimelineEditorWindow>();
            for (int i = 0; i < windows.Length; i++)
            {
                TimelineEditorWindow window = windows[i];
                if (!window || !window.MatchesSourceNode(node))
                    continue;
                window.BindNode(window.m_SourceGraphWindow, node);
            }
        }

        public void CreateGUI()
        {
            TryRestoreBinding();
            if (m_View == null)
                BuildUnboundView();
        }

        void OnEnable()
        {
            RuntimeDebugSession.Shared.Changed += OnRuntimeDebugSessionChanged;
        }

        [MenuItem("Tools/TreeDesigner/Timeline Editor", false, 3)]
        public static void OpenStandalone()
        {
            TimelineEditorWindow window = GetWindow<TimelineEditorWindow>();
            if (window.m_View == null)
                window.BuildUnboundView();
            window.Show();
            window.Focus();
        }

        void BindNode(BaseTreeWindow sourceGraphWindow, TimelineNode node)
        {
            m_SourceNode = node;
            m_SourceGraphWindow = sourceGraphWindow;
            m_SourceGraphOwner = node.Owner?.SerializedOwner;
            Bind(
                node.Timeline,
                node.Timeline.SerializedOwner,
                node.Timeline.SerializedPropertyPath,
                node.TimelineOwnership.ToString(),
                sourceGraphWindow,
                node,
                node.GUID);
        }

        void Bind(
            TimelineData timeline,
            UnityEngine.Object serializedOwner,
            string serializedPropertyPath,
            string ownershipLabel,
            BaseTreeWindow sourceGraphWindow,
            TimelineNode sourceNode,
            string sourceNodeGuid)
        {
            if (timeline == null || !serializedOwner || string.IsNullOrEmpty(serializedPropertyPath))
                throw new System.InvalidOperationException("TimelineEditorWindow requires a bound TimelineData owner/path.");

            DisposeView();
            m_HasDebugRequest = false;
            InvalidateLiveDebugOverlay();
            timeline.BindSerializedOwner(serializedOwner, serializedPropertyPath);
            m_SerializedOwner = serializedOwner;
            m_SerializedPropertyPath = serializedPropertyPath;
            m_OwnershipLabel = ownershipLabel;
            m_SourceGraphWindow = sourceGraphWindow;
            m_SourceNode = sourceNode;
            if (sourceNode != null)
                m_SourceGraphOwner = sourceNode.Owner?.SerializedOwner;
            else if (string.IsNullOrEmpty(sourceNodeGuid))
                m_SourceGraphOwner = null;
            m_SourceNodeGuid = sourceNodeGuid ?? string.Empty;
            titleContent = new GUIContent("Timeline Editor");
            m_View = new TimelineEditorView();
            Label ownership = new Label($"Timeline Ownership: {m_OwnershipLabel}");
            ownership.style.unityFontStyleAndWeight = FontStyle.Bold;
            ownership.style.paddingLeft = 8f;
            ownership.style.paddingTop = 4f;
            ownership.style.paddingBottom = 4f;
            m_View.OpenClipRequested += OpenClip;
            m_View.Init(TimelineEditorOpenRequestComposition.Create(
                timeline,
                serializedOwner,
                serializedPropertyPath,
                ownershipLabel,
                sourceGraphWindow,
                new TimelineRuntimeDebugBinding(timeline.AuthoringId)));
            rootVisualElement.Clear();
            rootVisualElement.Add(CreateModeToolbar());
            rootVisualElement.Add(ownership);
            rootVisualElement.Add(m_View);
            m_DebugDetails = new ScrollView();
            m_DebugDetails.style.maxHeight = 150;
            m_DebugDetails.style.minHeight = 80;
            rootVisualElement.Add(m_DebugDetails);
            SetMode(m_Mode);
        }

        void BuildUnboundView()
        {
            titleContent = new GUIContent("Timeline Editor");
            rootVisualElement.Clear();
            rootVisualElement.Add(CreateModeToolbar());
            m_DebugDetails = null;
            SetMode(m_Mode);
        }

        void ClearBinding()
        {
            DisposeView();
            m_DebugBinding?.Dispose(RuntimeDebugSession.Shared);
            m_DebugBinding = null;
            m_HasDebugRequest = false;
            InvalidateLiveDebugOverlay();
            m_SerializedOwner = null;
            m_SerializedPropertyPath = string.Empty;
            m_OwnershipLabel = string.Empty;
            m_SourceNodeGuid = string.Empty;
            m_SourceGraphWindow = null;
            m_SourceGraphOwner = null;
            m_SourceNode = null;
            BuildUnboundView();
        }

        void TryRestoreBinding()
        {
            if (m_View != null || !m_SerializedOwner || string.IsNullOrEmpty(m_SerializedPropertyPath))
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
                m_SourceNodeGuid);
        }

        TimelineData ResolveTimelineData()
        {
            if (m_SerializedOwner is TimelineAsset asset)
                return asset.Data;

            SerializedObject serializedObject = new SerializedObject(m_SerializedOwner);
            SerializedProperty property = serializedObject.FindProperty(m_SerializedPropertyPath);
            return property?.propertyType == SerializedPropertyType.ManagedReference
                ? property.managedReferenceValue as TimelineData
                : null;
        }

        void OpenClip(Clip clip)
        {
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
            RuntimeDebugSession.Shared.Changed -= OnRuntimeDebugSessionChanged;
            m_DebugBinding?.Dispose(RuntimeDebugSession.Shared);
            DisposeView();
        }

        void DisposeView()
        {
            if (m_View == null)
                return;
            m_View.OpenClipRequested -= OpenClip;
            m_View.Dispose();
            m_View.RemoveFromHierarchy();
            m_View = null;
        }

        VisualElement CreateModeToolbar()
        {
            var toolbar = new Toolbar();
            m_AuthoringToggle = new ToolbarToggle { text = "Authoring Preview" };
            m_LiveDebugToggle = new ToolbarToggle { text = "Live Debug" };
            m_SharedTimelineField = new ObjectField("Shared Timeline")
            {
                objectType = typeof(TimelineAsset),
                allowSceneObjects = false
            };
            m_SharedTimelineField.style.width = 280f;
            m_SharedTimelineField.SetValueWithoutNotify(m_SerializedOwner as TimelineAsset);
            m_SharedTimelineField.RegisterValueChangedCallback(OnSharedTimelineChanged);
            m_SourceSummary = new Label(CurrentSourceSummary());
            m_SourceSummary.style.minWidth = 180f;
            m_SourceSummary.style.marginLeft = 6f;
            m_TargetMenu = new ToolbarMenu { text = "Target" };
            m_PlaybackMenu = new ToolbarMenu { text = "Playback" };
            m_FollowToggle = new ToolbarToggle { text = "Follow Timeline" };
            m_LiveToggle = new ToolbarToggle { text = "Freeze" };
            m_CaptureButton = new ToolbarButton(() =>
            {
                RuntimeDebugSession session = RuntimeDebugSession.Shared;
                if (session.IsCaptureRecording)
                    session.EndCapture();
                else
                    session.BeginCapture(
                        RuntimeTraceChannel.Timeline | RuntimeTraceChannel.Animation | RuntimeTraceChannel.Motion,
                        RuntimeDiagnosticsCaptureDetail.Continuous);
            }) { text = "Capture" };
            m_HistorySlider = new SliderInt(0, 511);
            m_HistorySlider.style.width = 110;
            m_Status = new Label();
            m_Status.style.marginLeft = 6;
            m_Status.style.flexGrow = 1;

            m_AuthoringToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    SetMode(TimelineWindowMode.AuthoringPreview);
                else if (m_Mode == TimelineWindowMode.AuthoringPreview)
                    m_AuthoringToggle.SetValueWithoutNotify(true);
            });
            m_LiveDebugToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    SetMode(TimelineWindowMode.LiveDebug);
                else if (m_Mode == TimelineWindowMode.LiveDebug)
                    m_LiveDebugToggle.SetValueWithoutNotify(true);
            });
            m_FollowToggle.RegisterValueChangedCallback(evt =>
            {
                RuntimeDebugViewBinding binding = GetRuntimeDebugBinding(out _);
                if (binding == null)
                    return;
                if (evt.newValue)
                    binding.Follow();
                else
                    binding.Clear();
                RefreshLiveDebug();
            });
            m_LiveToggle.RegisterValueChangedCallback(evt =>
            {
                RuntimeDebugSession session = RuntimeDebugSession.Shared;
                if (session.CanControlLiveTarget)
                    session.FreezeLive();
                else if (session.CanResumeLiveTarget)
                    session.ResumeLive();
            });
            m_HistorySlider.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != RuntimeDebugSession.Shared.HistoryOffset)
                    RuntimeDebugSession.Shared.SetHistoryOffset(evt.newValue);
            });

            toolbar.Add(m_AuthoringToggle);
            toolbar.Add(m_LiveDebugToggle);
            toolbar.Add(m_SharedTimelineField);
            toolbar.Add(m_SourceSummary);
            toolbar.Add(m_TargetMenu);
            toolbar.Add(m_PlaybackMenu);
            toolbar.Add(m_FollowToggle);
            toolbar.Add(m_LiveToggle);
            toolbar.Add(m_CaptureButton);
            toolbar.Add(m_HistorySlider);
            toolbar.Add(m_Status);
            return toolbar;
        }

        void OnSharedTimelineChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            TimelineAsset asset = evt.newValue as TimelineAsset;
            if (asset)
            {
                BindAsset(asset);
                return;
            }

            if (m_SerializedOwner is TimelineAsset)
                ClearBinding();
            else
                m_SharedTimelineField.SetValueWithoutNotify(null);
        }

        string CurrentSourceSummary()
        {
            if (m_View?.Timeline == null)
                return "Source: None";
            string ownership = string.IsNullOrWhiteSpace(m_OwnershipLabel) ? "Timeline" : m_OwnershipLabel;
            return $"Source: {ownership} / {m_View.Timeline.Name}";
        }

        void SetMode(TimelineWindowMode mode)
        {
            m_Mode = mode;
            bool liveDebug = mode == TimelineWindowMode.LiveDebug;
            m_AuthoringToggle?.SetValueWithoutNotify(!liveDebug);
            m_LiveDebugToggle?.SetValueWithoutNotify(liveDebug);
            m_TargetMenu?.SetDisplay(liveDebug);
            m_PlaybackMenu?.SetDisplay(liveDebug);
            m_FollowToggle?.SetDisplay(liveDebug);
            m_LiveToggle?.SetDisplay(liveDebug);
            m_CaptureButton?.SetDisplay(liveDebug);
            m_HistorySlider?.SetDisplay(false);
            m_Status?.SetDisplay(liveDebug);
            if (m_DebugDetails != null)
                m_DebugDetails.style.display = liveDebug ? DisplayStyle.Flex : DisplayStyle.None;
            m_View?.SetLiveDebug(liveDebug);
            if (liveDebug)
            {
                m_HasDebugRequest = false;
                RefreshLiveDebug();
            }
            else
            {
                m_DebugBinding?.Dispose(RuntimeDebugSession.Shared);
                m_View?.ClearRuntimeOverlay();
                InvalidateLiveDebugOverlay();
            }
        }

        void RefreshLiveDebug()
        {
            if (Timeline == null || m_View == null || m_TargetMenu == null)
                return;

            RuntimeDebugSession session = RuntimeDebugSession.Shared;
            RuntimeDebugViewBinding binding = GetRuntimeDebugBinding(out _);
            if (binding == null)
                return;
            RuntimeDebugTargetResolution resolution = binding.Refresh(
                session,
                RuntimeTraceChannel.Timeline | RuntimeTraceChannel.Animation | RuntimeTraceChannel.Motion);
            RuntimeDebugViewModel view = session.ViewModel;
            RefreshMenus(view, binding);
            RefreshLiveDebugControls(session);
            m_FollowToggle.SetValueWithoutNotify(binding.Following);

            if (!resolution.CanReadSnapshot)
            {
                m_View.ClearRuntimeOverlay();
                m_DebugDetails.Clear();
                InvalidateLiveDebugOverlay();
                SetStatus(resolution.Message);
                return;
            }

            if (!view.Valid)
            {
                m_View.ClearRuntimeOverlay();
                m_DebugDetails.Clear();
                InvalidateLiveDebugOverlay();
                SetStatus(!string.IsNullOrEmpty(view.Error) ? view.Error : binding.StatusMessage);
                return;
            }

            RuntimeInstanceKey playback = binding.SelectedInstance;
            if (playback.Kind != RuntimeInstanceKind.TimelinePlayback)
            {
                m_View.ClearRuntimeOverlay();
                m_DebugDetails.Clear();
                InvalidateLiveDebugOverlay();
                SetStatus(binding.StatusMessage);
                return;
            }

            if (!view.TryGetTimelinePlaybackSummary(Timeline.AuthoringId, playback, out RuntimeTimelinePlaybackDebugSummary summary))
            {
                m_View.ClearRuntimeOverlay();
                m_DebugDetails.Clear();
                InvalidateLiveDebugOverlay();
                SetStatus("The selected Timeline playback has no formal Trace summary.");
                return;
            }

            IReadOnlyList<RuntimeDebugEventView> timelineEvents = view.GetTimelineCurrentEvents(Timeline.AuthoringId, playback);
            ulong latestLogic = 0;
            ulong latestPresentation = 0;
            for (int i = 0; i < timelineEvents.Count; i++)
            {
                RuntimeTraceEvent traceEvent = timelineEvents[i].Event;
                if (traceEvent.Domain == RuntimeTraceDomain.Logic)
                    latestLogic = Math.Max(latestLogic, traceEvent.Position);
                else if (traceEvent.Domain == RuntimeTraceDomain.Presentation)
                    latestPresentation = Math.Max(latestPresentation, traceEvent.Position);
            }
            float logicTime = LatestTime(timelineEvents, RuntimeTraceEventKind.TimelineLogicTime, latestLogic);
            float visualTime = LatestTime(timelineEvents, RuntimeTraceEventKind.TimelineVisualTime, latestPresentation);
            bool resetOverlay = !ReferenceEquals(m_LastDebugView, view) ||
                                !m_LastDebugPlayback.Equals(playback) ||
                                view.Changes.FullSync;
            if (resetOverlay || m_LastDebugRevision != view.Revision)
            {
                var tracks = new Dictionary<string, string>(StringComparer.Ordinal);
                var clips = new Dictionary<string, string>(StringComparer.Ordinal);
                for (int i = 0; i < timelineEvents.Count; i++)
                {
                    RuntimeDebugEventView eventView = timelineEvents[i];
                    if (eventView.Event.Domain != RuntimeTraceDomain.Logic || eventView.Event.Position != latestLogic)
                        continue;
                    if (eventView.Event.Kind == RuntimeTraceEventKind.TrackActive)
                        tracks[eventView.Source.TrackAuthoringId] = eventView.Event.Payload.Status;
                    if (eventView.Event.Kind == RuntimeTraceEventKind.ClipActive || eventView.Event.Kind is RuntimeTraceEventKind.TreeClipEntered or RuntimeTraceEventKind.TreeClipUpdated)
                        clips[eventView.Source.ClipAuthoringId] = $"{eventView.Event.Kind}: {eventView.Event.Payload.Status}";
                }
                m_View.ApplyRuntimeOverlay(visualTime, tracks, clips);
                m_DebugDetails.Clear();
                PopulateDebugDetails(
                    timelineEvents,
                    view.GetCurrentEvents(RuntimeTraceChannel.Motion),
                    latestLogic,
                    latestPresentation);
            }

            string terminalText = summary.IsTerminal
                ? $"{summary.Terminal}: {summary.TerminalCause}"
                : $"{summary.Lifecycle}: {summary.LifecycleStatus}";
            m_LastDebugView = view;
            m_LastDebugPlayback = playback;
            m_LastDebugRevision = view.Revision;
            string prefix = session.AttachmentState == RuntimeDebugAttachmentState.Ended ? "Ended | " :
                session.AttachmentState == RuntimeDebugAttachmentState.CaptureHistory ? "Capture | " :
                session.AttachmentState == RuntimeDebugAttachmentState.Frozen ? "Frozen | " : string.Empty;
            SetStatus($"{prefix}{view.Target.DisplayName} | {FormatPlaybackOrigin(summary.Provenance)} | Playback #{playback.TimelinePlaybackId} | logic {logicTime:0.###} | visual {visualTime:0.###} | cycle {LatestCycle(timelineEvents)} | {terminalText}");
        }

        void RefreshLiveDebugControls(RuntimeDebugSession session)
        {
            bool canResume = session.CanControlLiveTarget || session.CanResumeLiveTarget;
            m_LiveToggle.text = session.CanControlLiveTarget ? "Freeze" : "Resume";
            m_LiveToggle.SetValueWithoutNotify(session.AttachmentState == RuntimeDebugAttachmentState.Frozen);
            m_LiveToggle.SetEnabled(canResume);
            m_CaptureButton.text = session.IsCaptureRecording ? "Stop Capture" : "Capture";
            m_CaptureButton.SetEnabled(session.CanStartCapture || session.CanStopCapture);
            bool showHistory = session.HasCaptureHistory;
            m_HistorySlider.SetDisplay(showHistory);
            if (!showHistory)
                return;

            m_HistorySlider.highValue = Math.Max(0, session.CaptureSnapshot.SegmentCount - 1);
            m_HistorySlider.SetValueWithoutNotify(Math.Min(session.HistoryOffset, m_HistorySlider.highValue));
            m_HistorySlider.SetEnabled(true);
        }

        RuntimeDebugViewBinding GetRuntimeDebugBinding(out RuntimeDebugTargetRequest request)
        {
            request = default;
            if (Timeline == null)
                return null;

            if (m_DebugBinding == null)
                m_DebugBinding = new RuntimeDebugViewBinding(RuntimeDebugViewKind.Timeline);
            if (!m_HasDebugRequest)
            {
                m_DebugRequest = new RuntimeDebugTargetRequest(
                    RuntimeSourceElementKey.Timeline(Timeline.AuthoringId),
                    TimelineAuthoringFingerprint.Compute(Timeline));
                m_HasDebugRequest = true;
            }
            request = m_DebugRequest;
            m_DebugBinding.Configure(request);
            return m_DebugBinding;
        }

        void RefreshMenus(RuntimeDebugViewModel view, RuntimeDebugViewBinding binding)
        {
            RuntimeDebugSession session = RuntimeDebugSession.Shared;
            bool rebuild = m_LastDebugMenuTargetRevision != session.TargetRevision ||
                           m_LastDebugTimelinePlaybackRevision != view.GetTimelinePlaybackRevision(Timeline.AuthoringId) ||
                           view.Changes.FullSync;
            if (rebuild)
            {
                m_TargetMenu.menu.MenuItems().Clear();
                IReadOnlyList<RuntimeDebugTargetCandidate> candidates = session.GetTargetCandidates(binding.Request);
                for (int i = 0; i < candidates.Count; i++)
                {
                    RuntimeDebugTargetCandidate candidate = candidates[i];
                    RuntimeDebugTargetInfo target = candidate.Target;
                    m_TargetMenu.menu.AppendAction(
                        TargetLabel(target, candidate.Match),
                        _ => session.AttachToTarget(target.CharacterRuntimeId),
                        _ => candidate.IsExact ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                }

                m_PlaybackMenu.menu.MenuItems().Clear();
                IReadOnlyList<RuntimeTimelinePlaybackDebugSummary> summaries = view.Attached
                    ? view.GetTimelinePlaybackSummaries(Timeline.AuthoringId)
                    : Array.Empty<RuntimeTimelinePlaybackDebugSummary>();
                for (int i = 0; i < summaries.Count; i++)
                {
                    RuntimeTimelinePlaybackDebugSummary summary = summaries[i];
                    m_PlaybackMenu.menu.AppendAction(FormatPlaybackSummary(summary), _ =>
                    {
                        if (binding.Pin(summary.Playback))
                            RefreshLiveDebug();
                    });
                }

                m_LastDebugMenuTargetRevision = session.TargetRevision;
                m_LastDebugTimelinePlaybackRevision = view.GetTimelinePlaybackRevision(Timeline.AuthoringId);
            }
            m_TargetMenu.text = view.Attached
                ? view.Target.DisplayName + (session.AttachmentState == RuntimeDebugAttachmentState.Ended ? " (Ended)" : string.Empty)
                : "Target";
            m_PlaybackMenu.text = binding.SelectedInstance.Kind == RuntimeInstanceKind.TimelinePlayback
                ? $"Playback #{binding.SelectedInstance.TimelinePlaybackId}"
                : "Playback";
        }

        static string TargetLabel(RuntimeDebugTargetInfo target, RuntimeDebugTargetMatch match)
        {
            return match switch
            {
                RuntimeDebugTargetMatch.Exact => target.DisplayName,
                RuntimeDebugTargetMatch.SourceMissing => $"{target.DisplayName} (source missing)",
                RuntimeDebugTargetMatch.RevisionMismatch => $"{target.DisplayName} (revision mismatch)",
                _ => target.DisplayName
            };
        }

        static string FormatPlaybackSummary(RuntimeTimelinePlaybackDebugSummary summary)
        {
            string lifecycle = summary.IsTerminal
                ? $"{summary.Terminal}: {summary.TerminalCause}"
                : $"{summary.Lifecycle}: {summary.LifecycleStatus}";
            return $"Playback #{summary.Playback.TimelinePlaybackId} | {FormatPlaybackOrigin(summary.Provenance)} | {lifecycle}";
        }

        static string FormatPlaybackOrigin(RuntimeTimelinePlaybackProvenance provenance)
        {
            if (!provenance.IsValid)
                return "missing formal origin";

            string state = provenance.HasStateActivation
                ? $" | state {provenance.StateMachineGraphAuthoringId}/{provenance.StateId} #{provenance.StateActivationGeneration}"
                : string.Empty;
            return $"source {provenance.SourceGraphAuthoringId}/{provenance.SourceNodeAuthoringId} #{provenance.SourceActivationGeneration}{state}";
        }

        void PopulateDebugDetails(
            IReadOnlyList<RuntimeDebugEventView> events,
            IReadOnlyList<RuntimeDebugEventView> motionEvents,
            ulong latestLogic,
            ulong latestPresentation)
        {
            for (int i = 0; i < events.Count; i++)
            {
                RuntimeDebugEventView eventView = events[i];
                bool visible =
                    eventView.Event.Domain == RuntimeTraceDomain.Logic &&
                    eventView.Event.Position == latestLogic &&
                    (eventView.Event.Kind is RuntimeTraceEventKind.TreeClipEntered or RuntimeTraceEventKind.TreeClipUpdated or RuntimeTraceEventKind.TreeClipExited) ||
                    eventView.Event.Domain == RuntimeTraceDomain.Lifecycle &&
                    eventView.Event.Position == latestLogic &&
                    eventView.Event.Channel == RuntimeTraceChannel.Animation ||
                    eventView.Event.Domain == RuntimeTraceDomain.Presentation &&
                    eventView.Event.Position == latestPresentation &&
                    eventView.Event.Channel == RuntimeTraceChannel.Animation;
                if (!visible)
                    continue;
                RuntimeTracePayload payload = eventView.Event.Payload;
                string text;
                if (eventView.Event.Kind == RuntimeTraceEventKind.AnimationMarkerSync)
                {
                    text = $"MarkerSync | {payload.Name} | {payload.SecondaryTime:0.000}s -> {payload.Time:0.000}s | " +
                           $"fraction {payload.NormalizedTime:0.###} | cycle {payload.Cycle} | {payload.Status} | {payload.Detail}";
                }
                else
                {
                    text = eventView.Event.Channel == RuntimeTraceChannel.Animation
                        ? $"{eventView.Event.Kind} | {payload.Name} | owner {payload.OwnerId} | P{payload.Priority} | w {payload.Weight:0.###} -> {payload.FinalWeight:0.###}"
                        : $"{eventView.Event.Kind} | {eventView.SourceName} | {payload.Status} | {payload.Cause}";
                }
                m_DebugDetails.Add(new Label(text));
            }

            for (int i = 0; i < motionEvents.Count; i++)
            {
                RuntimeDebugEventView eventView = motionEvents[i];
                if (eventView.Event.Domain != RuntimeTraceDomain.Logic ||
                    eventView.Event.Position != latestLogic ||
                    !IsTimelineMotionTrace(eventView))
                    continue;
                RuntimeTracePayload payload = eventView.Event.Payload;
                m_DebugDetails.Add(new Label(
                    $"{eventView.Event.Kind} | {payload.Name} | {payload.Detail}"));
            }
        }

        bool IsTimelineMotionTrace(RuntimeDebugEventView eventView)
        {
            if (string.Equals(eventView.Source.TimelineAuthoringId, Timeline.AuthoringId, StringComparison.Ordinal))
                return true;
            return string.Equals(eventView.Event.Payload.Name, "world_result_applied", StringComparison.Ordinal);
        }

        void SetStatus(string value)
        {
            m_Status.text = value ?? string.Empty;
            m_Status.tooltip = value ?? string.Empty;
        }

        void OnRuntimeDebugSessionChanged()
        {
            if (m_Mode == TimelineWindowMode.LiveDebug)
                RefreshLiveDebug();
            Repaint();
        }

        void InvalidateLiveDebugOverlay()
        {
            m_LastDebugView = null;
            m_LastDebugPlayback = default;
            m_LastDebugRevision = -1;
            m_LastDebugMenuTargetRevision = -1;
            m_LastDebugTimelinePlaybackRevision = -1;
        }

        static float LatestTime(IReadOnlyList<RuntimeDebugEventView> events, RuntimeTraceEventKind kind, ulong position)
        {
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Event.Kind == kind && events[i].Event.Position == position)
                    return events[i].Event.Payload.Time;
            }
            return 0f;
        }

        static int LatestCycle(IReadOnlyList<RuntimeDebugEventView> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Event.Kind == RuntimeTraceEventKind.TimelineLogicTime)
                    return events[i].Event.Payload.Cycle;
            }
            return 0;
        }
    }

    static class TimelineDebugVisualElementExtensions
    {
        public static void SetDisplay(this VisualElement element, bool visible)
        {
            if (element != null)
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
