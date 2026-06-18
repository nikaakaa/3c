using System;
using System.Collections.Generic;
using ThirdPersonAction;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacterBehavior.Editor.ActionTimeline
{
    public sealed class CommittedActionRefPortedTimelineView : VisualElement
    {
        internal enum PaneResizeAbsoluteMode
        {
            ParentLocalX,
            ParentWidthMinusLocalX
        }

        const string EditorWindowUxmlPath = "Assets/Editor/Character/Action/Timeline/RefPortedResources/VisualTree/CommittedActionTimelineEditorWindow.uxml";
        const string EditorStylePath = "Assets/Editor/Character/Action/Timeline/RefPortedResources/StyleSheet/CommittedActionTimelineEditorWindow.uss";
        const string FieldStylePath = "Assets/Editor/Character/Action/Timeline/RefPortedResources/StyleSheet/CommittedActionTimelineFieldView.uss";
        const string TrackStylePath = "Assets/Editor/Character/Action/Timeline/RefPortedResources/StyleSheet/CommittedActionTimelineTrackView.uss";
        const string ClipStylePath = "Assets/Editor/Character/Action/Timeline/RefPortedResources/StyleSheet/CommittedActionTimelineClipView.uss";
        const string ClipEditDebugPrefix = "[DEBUG-TL-FLASH]";
        static readonly bool ClipEditDebugLogsEnabled = true;

        const float TrackHeight = 30f;
        const float TrackSpacing = 10f;
        const float TrackTopOffset = 5f;
        const float FieldOffsetX = 6f;
        const int TimeTextFontSize = 14;

        static void LogClipEdit(string message)
        {
            if (!ClipEditDebugLogsEnabled)
                return;

            Debug.Log($"{ClipEditDebugPrefix} {message}");
        }

        readonly VisualElement trackHandleContainer;
        readonly VisualElement leftPanel;
        readonly VisualElement fieldContent;
        readonly VisualElement inspectorScroll;
        readonly VisualElement trackField;
        readonly VisualElement markerField;
        readonly VisualElement drawFrameLineField;
        readonly VisualElement clipInspector;
        readonly VisualElement timeLocator;
        readonly Label locatorFrameLabel;
        readonly Label previewSummary;
        readonly Button playButton;
        readonly Button pauseButton;
        readonly FloatField playSpeedField;
        readonly ScrollView trackScrollView;
        readonly VisualElement addTrackButton;
        readonly ToolbarMenu addTrackMenu;
        readonly VisualElement leftPanelResizer;
        readonly VisualElement inspectorResizer;
        readonly ToolbarMenu timelineSelector;
        readonly VisualElement selectionBox;
        readonly CommittedActionTimelinePreviewAdapter previewAdapter = new CommittedActionTimelinePreviewAdapter();
        readonly ICommittedActionTimelineAnimationResolver animationResolver = new CommittedActionTimelineAnimancerLibraryResolver();
        readonly List<CommittedActionTimelineClipView> clipViews = new List<CommittedActionTimelineClipView>();
        readonly List<CommittedActionTimelineClipSelection> selectedClips = new List<CommittedActionTimelineClipSelection>();
        readonly Dictionary<int, float> framePosMap = new Dictionary<int, float>();
        readonly Dictionary<string, int> clipMoveStartTicks = new Dictionary<string, int>();
        readonly Dictionary<string, int> clipMoveEndTicks = new Dictionary<string, int>();
        int clipMoveAppliedDeltaFrames;
        bool clipMoveChanged;
        string clipResizeSelectionKey = string.Empty;
        int clipResizeStartTick;
        int clipResizeEndTick;
        int clipResizeAppliedStartTick;
        int clipResizeAppliedEndTick;
        bool clipResizeChanged;

        CommittedActionTimelineSerializedAdapter adapter;
        CommittedActionTimelineEditorModel timelineModel;
        SerializedObject serializedAsset;
        CommittedActionTimelineVariant activeVariant;
        string selectedClipPath = string.Empty;
        string selectedTrackStableId = string.Empty;
        string selectedClipStableId = string.Empty;
        CommittedActionTimelineVariant selectedVariant;
        int selectedTrackIndex = -1;
        int selectedClipIndex = -1;
        int maxPreviewFrame = 21;
        string lastPreviewLogSignature = string.Empty;
        bool markerDragging;
        bool panning;
        bool rectangleSelecting;
        bool paneResizing;
        bool inspectorRefreshQueued;
        float fieldScale = 1f;
        float panStartX;
        float paneResizeMinWidth;
        float paneResizeMaxWidth;
        PaneResizeAbsoluteMode paneResizeAbsoluteMode;
        Vector2 rectangleStart;
        VisualElement paneResizeHandle;
        VisualElement paneResizeTarget;
        int currentPreviewFrame;
        GameObject scenePreviewTarget;
        CommittedActionTimelinePlayablePreviewSession scenePreviewSession;
        CommittedActionTimelineScenePreviewBinding scenePreviewBinding =
            CommittedActionTimelineScenePreviewBinding.FromTarget(null);

        public CommittedActionRefPortedTimelineView()
        {
            name = "committed-action-ref-ported-timeline-window";
            AddToClassList("timelineEditorWindow");
            focusable = true;
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EditorWindowUxmlPath);
            if (visualTree == null)
                throw new InvalidOperationException($"timeline-uxml-missing:{EditorWindowUxmlPath}");

            visualTree.CloneTree(this);
            AddStyle(EditorStylePath);
            AddStyle(FieldStylePath);
            AddStyle(TrackStylePath);
            AddStyle(ClipStylePath);

            VisualElement top = this.Q("top");
            timelineSelector = new ToolbarMenu { text = "Directional" };
            top.Insert(1, timelineSelector);
            leftPanel = this.Q("left-panel");
            trackHandleContainer = this.Q("track-handle-container");
            fieldContent = this.Q("field-content");
            inspectorScroll = this.Q("inspector-scroll");
            trackField = this.Q("track-field");
            markerField = this.Q("marker-field");
            drawFrameLineField = this.Q("draw-frame-line-field");
            clipInspector = this.Q("clip-inspector");
            timeLocator = this.Q("time-locater");
            locatorFrameLabel = this.Q<Label>("time-locater-frame-label");
            previewSummary = this.Q<Label>("preview-summary");
            playButton = this.Q<Button>("play-button");
            pauseButton = this.Q<Button>("pause-button");
            playSpeedField = this.Q<FloatField>("play-speed-field");
            trackScrollView = this.Q<ScrollView>("track-scroll");
            addTrackButton = this.Q("add-track-button");
            addTrackMenu = new ToolbarMenu { name = "add-track-menu", text = "+ Track" };
            addTrackButton.Clear();
            addTrackButton.Add(new Label("Tracks") { name = "track-list-title" });
            addTrackButton.Add(addTrackMenu);
            leftPanelResizer = this.Q("left-panel-resizer");
            inspectorResizer = this.Q("inspector-resizer");
            selectionBox = new VisualElement { name = "rectangle-selector" };

            playButton.clicked += () => IsPreviewPlaying = true;
            pauseButton.clicked += () => IsPreviewPlaying = false;
            playSpeedField.RegisterValueChangedCallback(evt => PreviewSpeed = Mathf.Max(0.1f, evt.newValue));
            PreviewSpeed = 1f;
            RegisterInteractions();
        }

        public bool IsPreviewPlaying { get; private set; }
        public float PreviewSpeed { get; private set; }
        public int MaxPreviewFrame => maxPreviewFrame;
        static ActionTimelineCompileContext PreviewCompileContext => ActionTimelineCompileContext.FromTickRate(SimulationTickRate.Default);
        static float PreviewFixedTickSeconds => PreviewCompileContext.FixedTickSeconds;

        public CommittedActionTimelineScenePreviewBinding ScenePreviewBinding => scenePreviewBinding;

        public void Populate(SerializedObject serializedObject)
        {
            CharacterActionDefinitionSO definition = serializedObject != null
                ? serializedObject.targetObject as CharacterActionDefinitionSO
                : null;
            Populate(definition != null
                ? new CommittedActionTimelineSerializedAdapter(definition, serializedObject)
                : null);
        }

        public void Populate(CommittedActionTimelineSerializedAdapter timelineAdapter)
        {
            adapter = timelineAdapter;
            timelineModel = adapter != null && adapter.IsValid ? new CommittedActionTimelineEditorModel(adapter) : null;
            serializedAsset = adapter?.SerializedObject;
            trackHandleContainer.Clear();
            trackField.Clear();
            trackField.Add(selectionBox);
            HideSelectionBox();
            markerField.Clear();
            markerField.Add(timeLocator);
            clipInspector.Clear();
            clipViews.Clear();
            selectedClips.Clear();
            selectedClipPath = string.Empty;
            selectedTrackStableId = string.Empty;
            selectedClipStableId = string.Empty;
            selectedTrackIndex = -1;
            selectedClipIndex = -1;
            if (adapter == null || !adapter.IsValid)
            {
                addTrackMenu.menu.MenuItems().Clear();
                addTrackMenu.menu.AppendAction("No Action Definition", _ => { }, DropdownMenuAction.AlwaysDisabled);
                previewSummary.text = "Preview unavailable | action definition missing";
                DisposeScenePreview();
                return;
            }

            EnsureActiveVariant();
            PopulateTimelineSelector();
            PopulateAddTrackMenu();
            maxPreviewFrame = ResolveMaxFrame();
            BuildRuler(maxPreviewFrame);
            BuildTimeline(activeVariant);
            SetPreviewFrame(0);
        }

        public void SetScenePreviewTarget(GameObject target)
        {
            if (scenePreviewTarget != target)
            {
                DisposeScenePreviewSession();
                scenePreviewTarget = target;
                lastPreviewLogSignature = string.Empty;
            }

            scenePreviewBinding = CommittedActionTimelineScenePreviewBinding.FromTarget(scenePreviewTarget);
            LogPreviewBinding("target set");
            SetPreviewFrame(currentPreviewFrame);
        }

        public void DisposeScenePreview()
        {
            DisposeScenePreviewSession();
            CommittedActionTimelineMotionPreviewOverlay.Clear();
            scenePreviewTarget = null;
            scenePreviewBinding = CommittedActionTimelineScenePreviewBinding.FromTarget(null);
            lastPreviewLogSignature = string.Empty;
            LogPreviewBinding("target cleared");
        }

        public void SuspendScenePreview()
        {
            DisposeScenePreviewSession();
            CommittedActionTimelineMotionPreviewOverlay.Clear();
        }

        void RegisterInteractions()
        {
            addTrackButton.AddManipulator(new ContextualMenuManipulator(evt => BuildAddTrackMenu(evt.menu)));
            RegisterPaneResizer(leftPanelResizer, leftPanel, 160f, 420f, PaneResizeAbsoluteMode.ParentLocalX);
            RegisterPaneResizer(inspectorResizer, inspectorScroll, 260f, 960f, PaneResizeAbsoluteMode.ParentWidthMinusLocalX);
            markerField.generateVisualContent += DrawMarkerField;
            trackField.generateVisualContent += DrawTrackField;
            timeLocator.generateVisualContent += DrawTimeLocator;
            drawFrameLineField.generateVisualContent += DrawFrameLineField;
            trackScrollView.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                ResizeTimeField();
                RepaintTimelineField();
            });
            trackScrollView.horizontalScroller.valueChanged += _ =>
            {
                ResizeTimeField();
                RepaintTimelineField();
            };
            markerField.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;

                markerDragging = true;
                SetPreviewFrame(FrameFromLocalX(evt.localPosition.x));
                MouseCaptureController.CaptureMouse(markerField);
                evt.StopPropagation();
            });
            markerField.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!markerDragging)
                    return;

                SetPreviewFrame(FrameFromLocalX(evt.localPosition.x));
                evt.StopPropagation();
            });
            markerField.RegisterCallback<PointerUpEvent>(evt =>
            {
                markerDragging = false;
                if (MouseCaptureController.HasMouseCapture(markerField))
                    MouseCaptureController.ReleaseMouse(markerField);
                evt.StopPropagation();
            });
            trackScrollView.RegisterCallback<WheelEvent>(evt =>
            {
                float nextScale = Mathf.Clamp(fieldScale * (1f - evt.delta.y / 100f), 0.15f, 10f);
                if (Mathf.Approximately(nextScale, fieldScale))
                    return;

                fieldScale = nextScale;
                Populate(adapter);
                evt.StopPropagation();
            });
            trackScrollView.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 2)
                    return;

                panning = true;
                panStartX = evt.localPosition.x;
                MouseCaptureController.CaptureMouse(trackScrollView);
                evt.StopPropagation();
            });
            trackScrollView.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!panning)
                    return;

                float delta = panStartX - evt.localPosition.x;
                panStartX = evt.localPosition.x;
                trackScrollView.scrollOffset = new Vector2(trackScrollView.scrollOffset.x + delta, trackScrollView.scrollOffset.y);
                evt.StopPropagation();
            });
            trackScrollView.RegisterCallback<PointerUpEvent>(evt =>
            {
                panning = false;
                if (MouseCaptureController.HasMouseCapture(trackScrollView))
                    MouseCaptureController.ReleaseMouse(trackScrollView);
                evt.StopPropagation();
            });
            trackField.RegisterCallback<PointerDownEvent>(BeginRectangleSelection);
            trackField.RegisterCallback<PointerMoveEvent>(UpdateRectangleSelection);
            trackField.RegisterCallback<PointerUpEvent>(EndRectangleSelection);
            RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Delete)
                {
                    DeleteSelection();
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.F)
                {
                    FocusSelection();
                    evt.StopPropagation();
                }
            });
        }

        void RegisterPaneResizer(VisualElement handle, VisualElement target, float minWidth, float maxWidth, PaneResizeAbsoluteMode absoluteMode)
        {
            if (handle == null || target == null)
                return;

            handle.style.flexGrow = 0f;
            handle.style.flexShrink = 0f;
            target.style.flexGrow = 0f;
            target.style.flexShrink = 0f;
            target.style.minWidth = minWidth;
            target.style.maxWidth = maxWidth;

            handle.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;

                paneResizing = true;
                paneResizeHandle = handle;
                paneResizeTarget = target;
                paneResizeMinWidth = minWidth;
                paneResizeMaxWidth = maxWidth;
                paneResizeAbsoluteMode = absoluteMode;
                handle.AddToClassList("resizing");
                MouseCaptureController.CaptureMouse(handle);
                evt.StopPropagation();
            });
            handle.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!paneResizing || paneResizeHandle != handle || paneResizeTarget == null)
                    return;

                float width = ResolvePaneResizeWidthFromPanelPosition(
                    paneResizeTarget,
                    evt.position,
                    paneResizeAbsoluteMode,
                    paneResizeMinWidth,
                    paneResizeMaxWidth);
                paneResizeTarget.style.width = width;
                ResizeTimeField();
                RepaintTimelineField();
                evt.StopPropagation();
            });
            handle.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!paneResizing || paneResizeHandle != handle)
                    return;

                paneResizing = false;
                paneResizeTarget = null;
                paneResizeHandle = null;
                handle.RemoveFromClassList("resizing");
                if (MouseCaptureController.HasMouseCapture(handle))
                    MouseCaptureController.ReleaseMouse(handle);
                evt.StopPropagation();
            });
        }

        internal static float ResolvePaneResizeWidthFromParentLocalX(
            float parentWidth,
            float pointerParentLocalX,
            PaneResizeAbsoluteMode absoluteMode,
            float minWidth,
            float maxWidth)
        {
            float targetWidth = absoluteMode == PaneResizeAbsoluteMode.ParentLocalX
                ? pointerParentLocalX
                : parentWidth - pointerParentLocalX;
            return Mathf.Clamp(targetWidth, minWidth, maxWidth);
        }

        static float ResolvePaneResizeWidthFromPanelPosition(
            VisualElement target,
            Vector2 pointerPanelPosition,
            PaneResizeAbsoluteMode absoluteMode,
            float minWidth,
            float maxWidth)
        {
            VisualElement parent = target?.parent;
            if (parent == null)
                return minWidth;

            float parentWidth = parent.resolvedStyle.width > 0f ? parent.resolvedStyle.width : parent.layout.width;
            float pointerParentLocalX = parent.WorldToLocal(pointerPanelPosition).x;
            return ResolvePaneResizeWidthFromParentLocalX(
                parentWidth,
                pointerParentLocalX,
                absoluteMode,
                minWidth,
                maxWidth);
        }

        void PopulateAddTrackMenu()
        {
            BuildAddTrackMenu(addTrackMenu.menu);
        }

        void BuildAddTrackMenu(DropdownMenu menu)
        {
            menu.MenuItems().Clear();
            if (adapter == null || !adapter.IsValid)
            {
                menu.AppendAction("No Action Definition", _ => { }, DropdownMenuAction.AlwaysDisabled);
                return;
            }

            IReadOnlyList<CommittedActionTimelineVariant> variants = adapter.Variants;
            for (int i = 0; i < variants.Count; i++)
            {
                CommittedActionTimelineVariant variant = variants[i];
                for (int kindValue = 1; kindValue <= (int)ActionTimelineTrackKind.Cue; kindValue++)
                {
                    ActionTimelineTrackKind kind = (ActionTimelineTrackKind)kindValue;
                    menu.AppendAction($"{VariantLabel(variant)}/{kind}", _ =>
                    {
                        adapter.AddTrack(variant, kind, out string ignored);
                        Populate(adapter);
                    });
                }
            }
        }

        void PopulateTimelineSelector()
        {
            timelineSelector.menu.MenuItems().Clear();
            IReadOnlyList<CommittedActionTimelineVariant> variants = adapter.Variants;
            for (int i = 0; i < variants.Count; i++)
            {
                CommittedActionTimelineVariant variant = variants[i];
                timelineSelector.menu.AppendAction(VariantLabel(variant), _ =>
                {
                    activeVariant = variant;
                    timelineSelector.text = VariantLabel(activeVariant);
                    Populate(adapter);
                    SetPreviewFrame(0);
                }, _ => activeVariant == variant ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }

            timelineSelector.text = VariantLabel(activeVariant);
        }

        void EnsureActiveVariant()
        {
            IReadOnlyList<CommittedActionTimelineVariant> variants = adapter.Variants;
            for (int i = 0; i < variants.Count; i++)
            {
                if (variants[i] == activeVariant)
                    return;
            }

            activeVariant = variants.Count > 0 ? variants[0] : CommittedActionTimelineVariant.Directional;
        }

        void BuildTimeline(CommittedActionTimelineVariant variant)
        {
            CommittedActionTimelineEditorSnapshot snapshot = timelineModel?.Capture(variant);
            if (snapshot == null)
                return;

            int duration = Mathf.Max(1, SecondsToTick(snapshot.DurationSeconds));

            for (int i = 0; i < snapshot.Tracks.Count; i++)
            {
                CommittedActionTimelineTrackSnapshot trackSnapshot = snapshot.Tracks[i];
                SerializedProperty track = serializedAsset.FindProperty(trackSnapshot.PropertyPath);
                if (track == null)
                    continue;

                CommittedActionTimelineTrackHandle handle = new CommittedActionTimelineTrackHandle(
                    adapter,
                    variant,
                    trackSnapshot.Index,
                    track,
                    i,
                    SelectTrack,
                    RefreshTimelineFromSerializedPreservingSelection);
                CommittedActionTimelineTrackView trackView = new CommittedActionTimelineTrackView(
                    adapter,
                    variant,
                    trackSnapshot.Index,
                    track,
                    duration,
                    i,
                    SelectTrack,
                    SelectClip,
                    IsClipSelected,
                    BeginClipMove,
                    MoveClipSelection,
                    EndClipMove,
                    BeginClipResize,
                    ResizeClip,
                    EndClipResize,
                    RefreshTimelineFromSerializedPreservingSelection,
                    clipViews);
                trackHandleContainer.Add(handle);
                trackField.Add(trackView);
            }
        }

        void BuildRuler(int duration)
        {
            CommittedActionTimelineClipView.SetFrameWidth(50f * fieldScale);
            RebuildFramePositions(Mathf.Max(1, duration + 1));
            ResizeTimeField();
            RepaintTimelineField();
        }

        void SelectTrack(CommittedActionTimelineVariant variant, int trackIndex, string trackPropertyPath)
        {
            selectedVariant = variant;
            selectedTrackIndex = trackIndex;
            selectedClipIndex = -1;
            selectedClipPath = string.Empty;
            selectedClipStableId = string.Empty;
            ClearClipSelection();
            clipInspector.Clear();
            SerializedProperty track = serializedAsset.FindProperty(trackPropertyPath);
            if (track == null)
            {
                selectedTrackStableId = string.Empty;
                ApplyTrackSelectionStyles();
                return;
            }

            selectedTrackStableId = CommittedActionTimelineSerializedAdapter.ReadStableId(track);
            ApplyTrackSelectionStyles();
            BuildTrackInspector(variant, track);
        }

        void SelectClip(
            CommittedActionTimelineVariant variant,
            int trackIndex,
            int clipIndex,
            string clipPropertyPath)
        {
            selectedVariant = variant;
            selectedTrackIndex = trackIndex;
            selectedClipIndex = clipIndex;
            selectedClipPath = clipPropertyPath ?? string.Empty;
            if (adapter.TryGetClipIdentity(variant, trackIndex, clipIndex, out CommittedActionTimelineClipIdentity identity, out _))
            {
                selectedTrackStableId = identity.TrackStableId;
                selectedClipStableId = identity.ClipStableId;
            }
            else
            {
                selectedTrackStableId = string.Empty;
                selectedClipStableId = string.Empty;
            }
            ClearClipSelection();
            SelectClipView(selectedClipPath);
            ApplyTrackSelectionStyles();
            clipInspector.Clear();
            SerializedProperty clip = serializedAsset.FindProperty(selectedClipPath);
            if (clip == null)
                return;

            TryFindTrackProperty(variant, trackIndex, out SerializedProperty track);
            BuildClipInspector(variant, track, clip);
        }

        void BuildTrackInspector(CommittedActionTimelineVariant variant, SerializedProperty track)
        {
            VisualElement identity = CreateInspectorSection($"{VariantLabel(variant)} Track");
            AddBoundField(identity, track, "stableId", "Stable Id", false);
            AddBoundField(identity, track, "kind", "Kind");
            SerializedProperty clips = track.FindPropertyRelative("clips");
            AddReadonlyText(identity, "Clip Count", clips == null ? "0" : clips.arraySize.ToString());
            clipInspector.Add(identity);
        }

        void BuildClipInspector(CommittedActionTimelineVariant variant, SerializedProperty track, SerializedProperty clip)
        {
            ActionTimelineClipKind clipKind = CommittedActionTimelineSerializedAdapter.ReadClipKind(clip);
            ActionTimelineTrackKind trackKind = CommittedActionTimelineSerializedAdapter.ReadTrackKind(track);

            VisualElement identity = CreateInspectorSection($"{VariantLabel(variant)} Clip");
            AddBoundField(identity, clip, "stableId", "Stable Id", false);
            AddBoundField(identity, clip, "kind", "Kind", false);
            AddReadonlyText(identity, "Track Kind", trackKind.ToString());
            if (!CommittedActionTimelineSerializedAdapter.IsClipKindAllowed(trackKind, clipKind))
            {
                identity.Add(new HelpBox(
                    $"Clip kind {clipKind} does not match track kind {trackKind}.",
                    HelpBoxMessageType.Warning));
            }
            clipInspector.Add(identity);

            VisualElement timing = CreateInspectorSection("Timing");
            AddClipTimingFields(timing, clip);
            float start = clip.FindPropertyRelative("startSeconds").floatValue;
            float end = clip.FindPropertyRelative("endSeconds").floatValue;
            int startTick = CommittedActionTimelineClipView.SecondsToTick(start);
            int endTick = CommittedActionTimelineClipView.SecondsToTick(end);
            AddReadonlyText(
                timing,
                "Duration",
                $"{Mathf.Max(0f, end - start):0.###}s / {Mathf.Max(0, endTick - startTick)} ticks");
            clipInspector.Add(timing);

            SerializedProperty payload = clip.FindPropertyRelative("payload");
            if (payload != null)
                BuildClipPayloadInspector(clipKind, payload);
        }

        void AddClipTimingFields(VisualElement parent, SerializedProperty clip)
        {
            string clipPropertyPath = clip.propertyPath;
            float start = clip.FindPropertyRelative("startSeconds").floatValue;
            float end = clip.FindPropertyRelative("endSeconds").floatValue;
            AddDelayedInspectorFloatField(parent, "clip-start-seconds-field", "Start Seconds", start, value =>
            {
                SerializedProperty liveClip = serializedAsset.FindProperty(clipPropertyPath);
                if (liveClip == null)
                    return;

                SerializedProperty liveStart = liveClip.FindPropertyRelative("startSeconds");
                SerializedProperty liveEnd = liveClip.FindPropertyRelative("endSeconds");
                ResolveStartEdit(liveStart.floatValue, liveEnd.floatValue, value, out float nextStart, out float nextEnd);
                liveStart.floatValue = nextStart;
                liveEnd.floatValue = nextEnd;
                RefreshTimelineFromSerializedPreservingSelection();
            });
            AddDelayedInspectorFloatField(parent, "clip-end-seconds-field", "End Seconds", end, value =>
            {
                SerializedProperty liveClip = serializedAsset.FindProperty(clipPropertyPath);
                if (liveClip == null)
                    return;

                SerializedProperty liveStart = liveClip.FindPropertyRelative("startSeconds");
                SerializedProperty liveEnd = liveClip.FindPropertyRelative("endSeconds");
                float nextEnd = ResolveEndEdit(liveStart.floatValue, value);
                liveEnd.floatValue = nextEnd;
                RefreshTimelineFromSerializedPreservingSelection();
            });
            AddDelayedInspectorFloatField(parent, "clip-duration-seconds-field", "Duration Seconds", Mathf.Max(0f, end - start), value =>
            {
                SerializedProperty liveClip = serializedAsset.FindProperty(clipPropertyPath);
                if (liveClip == null)
                    return;

                SerializedProperty liveStart = liveClip.FindPropertyRelative("startSeconds");
                SerializedProperty liveEnd = liveClip.FindPropertyRelative("endSeconds");
                float nextEnd = ResolveDurationEdit(liveStart.floatValue, value);
                liveEnd.floatValue = nextEnd;
                RefreshTimelineFromSerializedPreservingSelection();
            });
        }

        void AddDelayedInspectorFloatField(
            VisualElement parent,
            string name,
            string label,
            float value,
            Action<float> apply)
        {
            FloatField field = new FloatField(label)
            {
                name = name,
                isDelayed = true,
                value = value
            };
            field.AddToClassList("timelineInspectorField");
            field.RegisterValueChangedCallback(evt => apply(evt.newValue));
            parent.Add(field);
        }

        internal static void ResolveStartEdit(float currentStart, float currentEnd, float requestedStart, out float start, out float end)
        {
            float duration = Mathf.Max(0f, currentEnd - currentStart);
            start = Mathf.Max(0f, requestedStart);
            end = start + duration;
        }

        internal static float ResolveEndEdit(float start, float requestedEnd)
        {
            return Mathf.Max(Mathf.Max(0f, start), requestedEnd);
        }

        internal static float ResolveDurationEdit(float start, float requestedDuration)
        {
            return Mathf.Max(0f, start) + Mathf.Max(0f, requestedDuration);
        }

        void BuildClipPayloadInspector(ActionTimelineClipKind clipKind, SerializedProperty payload)
        {
            VisualElement payloadSection = CreateInspectorSection("Payload");
            switch (clipKind)
            {
                case ActionTimelineClipKind.AnimationKey:
                    AddBoundField(payloadSection, payload, "animationKey", "Animation Key");
                    break;
                case ActionTimelineClipKind.Motion:
                    AddBoundField(payloadSection, payload, "motionSourceStateId", "Motion Source State Id");
                    AddBoundField(payloadSection, payload, "motionVariant", "Motion Variant");
                    AddBoundField(payloadSection, payload, "motionDuration", "Motion Duration");
                    AddBoundField(payloadSection, payload, "motionDistance", "Motion Distance");
                    AddBoundField(payloadSection, payload, "rotateToDirection", "Rotate To Direction");
                    AddBoundField(payloadSection, payload, "setRunLatchOnComplete", "Set Run Latch On Complete");
                    AddMotionWarpInspector(payloadSection, payload);
                    break;
                case ActionTimelineClipKind.HitboxWindow:
                    AddBoundField(payloadSection, payload, "factId", "Hitbox Fact Id");
                    break;
                case ActionTimelineClipKind.CancelWindow:
                    AddBoundField(payloadSection, payload, "factId", "Cancel Fact Id");
                    break;
                case ActionTimelineClipKind.Cue:
                    AddBoundField(payloadSection, payload, "cueId", "Cue Id");
                    break;
                default:
                    payloadSection.Add(new HelpBox("Select a concrete clip kind.", HelpBoxMessageType.Info));
                    break;
            }
            clipInspector.Add(payloadSection);
        }

        void AddMotionWarpInspector(VisualElement payloadSection, SerializedProperty payload)
        {
            Foldout foldout = new Foldout
            {
                text = "Motion Warp",
                value = HasMotionWarpPayload(payload)
            };
            foldout.AddToClassList("timelineInspectorFoldout");
            AddBoundField(foldout, payload, "warpPolicyId", "Warp Policy Id");
            AddBoundField(foldout, payload, "warpTargetBindingId", "Warp Target Binding Id");
            AddBoundField(foldout, payload, "warpMotionProfileId", "Warp Motion Profile Id");
            AddBoundField(foldout, payload, "warpAttackMagnet", "Warp Attack Magnet");
            AddBoundField(foldout, payload, "warpFacingCorrection", "Warp Facing Correction");
            AddBoundField(foldout, payload, "warpRequireTarget", "Warp Require Target");
            AddBoundField(foldout, payload, "warpRequireMotionProfile", "Warp Require Motion Profile");
            AddBoundField(foldout, payload, "warpAxisMask", "Warp Axis Mask");
            AddBoundField(foldout, payload, "warpRotationPolicy", "Warp Rotation Policy");
            AddBoundField(foldout, payload, "warpMaxPlanarDelta", "Warp Max Planar Delta");
            AddBoundField(foldout, payload, "warpStoppingDistance", "Warp Stopping Distance");
            AddBoundField(foldout, payload, "warpMaxYawDeltaDegrees", "Warp Max Yaw Delta Degrees");
            AddBoundField(foldout, payload, "warpTranslationWeight", "Warp Translation Weight");
            AddBoundField(foldout, payload, "warpRotationWeight", "Warp Rotation Weight");
            payloadSection.Add(foldout);
        }

        VisualElement CreateInspectorSection(string title)
        {
            VisualElement section = new VisualElement();
            section.AddToClassList("timelineInspectorSection");
            Label titleLabel = new Label(title);
            titleLabel.AddToClassList("timelineInspectorSectionTitle");
            section.Add(titleLabel);
            return section;
        }

        void AddBoundField(
            VisualElement parent,
            SerializedProperty owner,
            string relativeName,
            string label,
            bool enabled = true)
        {
            SerializedProperty property = owner?.FindPropertyRelative(relativeName);
            if (property == null)
                return;

            PropertyField field = new PropertyField(property, label);
            field.AddToClassList("timelineInspectorField");
            field.SetEnabled(enabled);
            field.Bind(serializedAsset);
            if (enabled)
                field.RegisterCallback<FocusOutEvent>(_ => QueueInspectorTimelineRefresh());
            parent.Add(field);
        }

        void QueueInspectorTimelineRefresh()
        {
            if (inspectorRefreshQueued)
                return;

            inspectorRefreshQueued = true;
            schedule.Execute(() =>
            {
                inspectorRefreshQueued = false;
                RefreshTimelineFromSerializedPreservingSelection();
            }).ExecuteLater(0);
        }

        void RefreshTimelineFromSerializedPreservingSelection()
        {
            if (adapter == null || !adapter.IsValid)
            {
                LogClipEdit("refresh invalid-adapter populate");
                Populate(adapter);
                return;
            }

            CommittedActionTimelineVariant restoreVariant = selectedVariant;
            string restoreTrackStableId = selectedTrackStableId;
            string restoreClipStableId = selectedClipStableId;
            bool restoreClip = !string.IsNullOrWhiteSpace(restoreTrackStableId) &&
                               !string.IsNullOrWhiteSpace(restoreClipStableId);
            bool restoreTrack = !restoreClip && !string.IsNullOrWhiteSpace(restoreTrackStableId);
            int restorePreviewFrame = currentPreviewFrame;

            LogClipEdit(
                $"refresh begin selectedClips={selectedClips.Count} restoreClip={restoreClip} restoreTrack={restoreTrack} " +
                $"trackId={restoreTrackStableId} clipId={restoreClipStableId} frame={restorePreviewFrame}");

            serializedAsset.ApplyModifiedProperties();
            serializedAsset.UpdateIfRequiredOrScript();
            timelineModel = new CommittedActionTimelineEditorModel(adapter);
            trackHandleContainer.Clear();
            trackField.Clear();
            trackField.Add(selectionBox);
            HideSelectionBox();
            clipViews.Clear();
            selectedClips.Clear();
            maxPreviewFrame = ResolveMaxFrame();
            BuildRuler(maxPreviewFrame);
            BuildTimeline(activeVariant);

            if (restoreClip)
            {
                CommittedActionTimelineClipIdentity identity = new CommittedActionTimelineClipIdentity(
                    restoreVariant,
                    restoreTrackStableId,
                    restoreClipStableId);
                if (timelineModel.TryResolveClip(identity, out int trackIndex, out int clipIndex, out string clipPath))
                {
                    SelectClip(restoreVariant, trackIndex, clipIndex, clipPath);
                    SetPreviewFrame(Mathf.Min(restorePreviewFrame, maxPreviewFrame));
                    LogClipEdit(
                        $"refresh restore-clip track={trackIndex} clip={clipIndex} path={clipPath} frame={currentPreviewFrame}");
                    return;
                }

                LogClipEdit(
                    $"refresh missing-clip trackId={restoreTrackStableId} clipId={restoreClipStableId}");
            }

            if (restoreTrack &&
                TryResolveTrackByStableId(restoreVariant, restoreTrackStableId, out int restoredTrackIndex, out string restoredTrackPath))
            {
                SelectTrack(restoreVariant, restoredTrackIndex, restoredTrackPath);
                SetPreviewFrame(Mathf.Min(restorePreviewFrame, maxPreviewFrame));
                LogClipEdit(
                    $"refresh restore-track track={restoredTrackIndex} path={restoredTrackPath} frame={currentPreviewFrame}");
                return;
            }

            selectedTrackIndex = -1;
            selectedClipIndex = -1;
            selectedClipPath = string.Empty;
            selectedTrackStableId = string.Empty;
            selectedClipStableId = string.Empty;
            clipInspector.Clear();
            ApplyTrackSelectionStyles();
            SetPreviewFrame(Mathf.Min(restorePreviewFrame, maxPreviewFrame));
            LogClipEdit($"refresh clear-selection frame={currentPreviewFrame}");
        }

        bool TryResolveTrackByStableId(
            CommittedActionTimelineVariant variant,
            string stableId,
            out int trackIndex,
            out string trackPath)
        {
            trackIndex = -1;
            trackPath = string.Empty;
            if (string.IsNullOrWhiteSpace(stableId) ||
                !adapter.TryGetTimelineProperty(variant, out SerializedProperty timeline, out _) ||
                timeline == null)
                return false;

            SerializedProperty tracks = timeline.FindPropertyRelative("tracks");
            for (int i = 0; tracks != null && i < tracks.arraySize; i++)
            {
                SerializedProperty track = tracks.GetArrayElementAtIndex(i);
                if (!string.Equals(CommittedActionTimelineSerializedAdapter.ReadStableId(track), stableId, StringComparison.Ordinal))
                    continue;

                trackIndex = i;
                trackPath = track.propertyPath;
                return true;
            }

            return false;
        }

        static void AddReadonlyText(VisualElement parent, string label, string value)
        {
            TextField field = new TextField(label);
            field.SetValueWithoutNotify(value ?? string.Empty);
            field.SetEnabled(false);
            field.AddToClassList("timelineInspectorReadonly");
            parent.Add(field);
        }

        static bool HasMotionWarpPayload(SerializedProperty payload)
        {
            return !string.IsNullOrWhiteSpace(payload.FindPropertyRelative("warpPolicyId")?.stringValue) ||
                   !string.IsNullOrWhiteSpace(payload.FindPropertyRelative("warpTargetBindingId")?.stringValue) ||
                   !string.IsNullOrWhiteSpace(payload.FindPropertyRelative("warpMotionProfileId")?.stringValue) ||
                   (payload.FindPropertyRelative("warpAttackMagnet")?.boolValue ?? false) ||
                   (payload.FindPropertyRelative("warpFacingCorrection")?.boolValue ?? false);
        }

        bool TryFindTrackProperty(
            CommittedActionTimelineVariant variant,
            int trackIndex,
            out SerializedProperty track)
        {
            track = null;
            if (adapter == null ||
                !adapter.TryGetTimelineProperty(variant, out SerializedProperty timeline, out _) ||
                timeline == null)
                return false;

            SerializedProperty tracks = timeline.FindPropertyRelative("tracks");
            if (tracks == null || trackIndex < 0 || trackIndex >= tracks.arraySize)
                return false;

            track = tracks.GetArrayElementAtIndex(trackIndex);
            return true;
        }

        public void SetPreviewFrame(int frame)
        {
            int clampedFrame = Mathf.Clamp(frame, 0, maxPreviewFrame);
            currentPreviewFrame = clampedFrame;
            timeLocator.style.left = FramePosition(clampedFrame);
            float localTimeSeconds = TickToSeconds(clampedFrame);
            locatorFrameLabel.text = $"{localTimeSeconds:0.###}s";

            int activeCount = 0;
            for (int i = 0; i < clipViews.Count; i++)
            {
                bool active = clipViews[i].ContainsTick(clampedFrame);
                clipViews[i].SetPreviewActive(active);
                if (active)
                    activeCount++;
            }

            if (adapter == null || !adapter.IsValid)
            {
                CommittedActionTimelineMotionPreviewOverlay.Clear();
                previewSummary.text = $"Preview {localTimeSeconds:0.###}s / tick {clampedFrame} | active clips {activeCount}";
                return;
            }

            CommittedActionTimelinePreviewResult preview = previewAdapter.Preview(
                adapter.ActionDefinition,
                activeVariant,
                localTimeSeconds,
                clampedFrame);
            preview = preview.WithVisualPreview(RefreshVisualPreview(preview));
            RefreshMotionOverlay(preview);
            string motion = preview.MotionSpec.HasSpec
                ? $"motion ghost/path {preview.MotionSpec.Distance:0.##}m/{preview.MotionSpec.Duration:0.##}s"
                : "motion diagnostic none";
            string animation = preview.AnimationKey.IsValid ? preview.AnimationKey.Value : "animation none";
            string node = preview.SelectedNodeId.IsValid ? preview.SelectedNodeId.Value : "node none";
            string visualClip = string.IsNullOrWhiteSpace(preview.ResolvedClipName)
                ? "clip none"
                : $"clip {preview.ResolvedClipName}";
            string visual = preview.VisualPreviewSampled
                ? $"visual sampled {preview.VisualClipTimeSeconds:0.###}s"
                : $"visual {preview.VisualPreviewStatus}";
            previewSummary.text =
                $"Preview {preview.LocalTimeSeconds:0.###}s / tick {preview.LocalTick} | {preview.SceneBindingStatus} | {visual} | {visualClip} | {VariantLabel(activeVariant)} | active {activeCount} | {node} | {animation} | {motion} | windows {preview.ActiveWindowFacts.Count} | cues {preview.CueIds.Count}";
            LogPreviewResult(preview, activeCount);
        }

        CommittedActionTimelineVisualPreviewResult RefreshVisualPreview(CommittedActionTimelinePreviewResult preview)
        {
            scenePreviewBinding = CommittedActionTimelineScenePreviewBinding.FromTarget(scenePreviewTarget);
            if (!scenePreviewBinding.CanSample)
            {
                DisposeScenePreviewSession();
                return CommittedActionTimelineVisualPreviewResult.NotSampled(scenePreviewBinding.Status, scenePreviewBinding.Status);
            }

            if (!preview.HasPreview)
            {
                DisposeScenePreviewSession();
                return CommittedActionTimelineVisualPreviewResult.NotSampled(scenePreviewBinding.Status, "preview-data-unavailable");
            }

            CommittedActionTimelineAnimationResolveResult animation = animationResolver.Resolve(scenePreviewBinding, preview.AnimationKey);
            if (!animation.CanSample)
            {
                DisposeScenePreviewSession();
                return CommittedActionTimelineVisualPreviewResult.NotSampled(scenePreviewBinding.Status, animation.Status);
            }

            scenePreviewSession ??= new CommittedActionTimelinePlayablePreviewSession();
            return scenePreviewSession.Sample(scenePreviewBinding, animation, preview.LocalTimeSeconds);
        }

        void DisposeScenePreviewSession()
        {
            scenePreviewSession?.Dispose();
            scenePreviewSession = null;
        }

        void RefreshMotionOverlay(CommittedActionTimelinePreviewResult preview)
        {
            if (scenePreviewTarget == null || !preview.MotionSpec.HasSpec)
            {
                CommittedActionTimelineMotionPreviewOverlay.Clear();
                return;
            }

            CommittedActionTimelineMotionPreviewOverlay.Show(
                scenePreviewTarget,
                preview.MotionSpec,
                preview.LocalTimeSeconds);
        }

        void LogPreviewBinding(string reason)
        {
            string targetName = scenePreviewTarget != null ? scenePreviewTarget.name : "none";
            string message = $"{reason} target={targetName} status={scenePreviewBinding.Status}";
            if (scenePreviewBinding.CanSample)
                CommittedActionTimelinePreviewLogger.Log(message, scenePreviewTarget);
            else
                CommittedActionTimelinePreviewLogger.Warning(message, scenePreviewTarget);
        }

        void LogPreviewResult(CommittedActionTimelinePreviewResult preview, int activeCount)
        {
            string animation = preview.AnimationKey.IsValid ? preview.AnimationKey.Value : "none";
            string node = preview.SelectedNodeId.IsValid ? preview.SelectedNodeId.Value : "none";
            string signature =
                $"{preview.LocalTick}|{preview.SceneBindingStatus}|{preview.VisualPreviewStatus}|{preview.ResolvedClipName}|{animation}|{activeCount}";
            if (signature == lastPreviewLogSignature)
                return;

            lastPreviewLogSignature = signature;
            string targetName = scenePreviewTarget != null ? scenePreviewTarget.name : "none";
            string motionDiagnostic = preview.MotionSpec.HasSpec
                ? $"{preview.MotionSpec.Distance:0.##}m/{preview.MotionSpec.Duration:0.##}s"
                : "none";
            string message =
                $"tick={preview.LocalTick} time={preview.LocalTimeSeconds:0.###} target={targetName} binding={preview.SceneBindingStatus} visual={preview.VisualPreviewStatus} clip={preview.ResolvedClipName} animation={animation} node={node} active={activeCount} motionDiagnostic={motionDiagnostic}";
            if (preview.VisualPreviewSampled)
                CommittedActionTimelinePreviewLogger.Log(message, scenePreviewTarget);
            else
                CommittedActionTimelinePreviewLogger.Warning(message, scenePreviewTarget);
        }

        int ResolveMaxFrame()
        {
            int max = 1;
            IReadOnlyList<CommittedActionTimelineVariant> variants = adapter.Variants;
            for (int i = 0; i < variants.Count; i++)
            {
                if (adapter.TryGetTimelineProperty(variants[i], out SerializedProperty timeline, out _))
                    max = Mathf.Max(max, ResolveTimelineMaxFrame(timeline));
            }

            return max;
        }

        int ResolveTimelineMaxFrame(SerializedProperty timeline)
        {
            if (timeline == null)
                return 1;

            SerializedProperty duration = timeline.FindPropertyRelative("durationSeconds");
            int max = duration != null ? SecondsToTick(duration.floatValue) : 1;
            SerializedProperty tracks = timeline.FindPropertyRelative("tracks");
            for (int i = 0; tracks != null && i < tracks.arraySize; i++)
            {
                SerializedProperty clips = tracks.GetArrayElementAtIndex(i).FindPropertyRelative("clips");
                for (int j = 0; clips != null && j < clips.arraySize; j++)
                {
                    SerializedProperty end = clips.GetArrayElementAtIndex(j).FindPropertyRelative("endSeconds");
                    if (end != null)
                        max = Mathf.Max(max, SecondsToTick(end.floatValue));
                }
            }

            return Mathf.Max(1, max);
        }

        void DeleteSelection()
        {
            if (adapter == null || !adapter.IsValid)
                return;

            if (selectedClips.Count > 0)
            {
                List<CommittedActionTimelineClipSelection> sorted = new List<CommittedActionTimelineClipSelection>(selectedClips);
                sorted.Sort((left, right) =>
                {
                    int variantCompare = ((int)right.Variant).CompareTo((int)left.Variant);
                    if (variantCompare != 0)
                        return variantCompare;
                    int trackCompare = right.TrackIndex.CompareTo(left.TrackIndex);
                    return trackCompare != 0 ? trackCompare : right.ClipIndex.CompareTo(left.ClipIndex);
                });
                for (int i = 0; i < sorted.Count; i++)
                    adapter.RemoveClip(sorted[i].Variant, sorted[i].TrackIndex, sorted[i].ClipIndex, out _);
            }
            else if (selectedClipIndex >= 0)
            {
                adapter.RemoveClip(selectedVariant, selectedTrackIndex, selectedClipIndex, out _);
            }
            else if (selectedTrackIndex >= 0)
            {
                adapter.RemoveTrack(selectedVariant, selectedTrackIndex, out _);
            }
            Populate(adapter);
        }

        void FocusSelection()
        {
            int targetFrame = 0;
            string focusPath = selectedClips.Count > 0 ? selectedClips[0].ClipPath : selectedClipPath;
            if (!string.IsNullOrWhiteSpace(focusPath))
            {
                SerializedProperty clip = serializedAsset.FindProperty(focusPath);
                if (clip != null)
                    targetFrame = SecondsToTick(clip.FindPropertyRelative("startSeconds").floatValue);
            }

            trackScrollView.scrollOffset = new Vector2(
                Mathf.Max(0f, targetFrame * CommittedActionTimelineClipView.FrameWidth - 120f),
                trackScrollView.scrollOffset.y);
        }

        void BeginRectangleSelection(PointerDownEvent evt)
        {
            if (evt.button != 0 || adapter == null || !adapter.IsValid)
                return;

            rectangleSelecting = true;
            rectangleStart = evt.localPosition;
            ClearClipSelection();
            selectionBox.BringToFront();
            UpdateSelectionBox(rectangleStart);
            MouseCaptureController.CaptureMouse(trackField);
            evt.StopPropagation();
        }

        void UpdateRectangleSelection(PointerMoveEvent evt)
        {
            if (!rectangleSelecting)
                return;

            UpdateSelectionBox(evt.localPosition);
            evt.StopPropagation();
        }

        void EndRectangleSelection(PointerUpEvent evt)
        {
            if (!rectangleSelecting)
                return;

            rectangleSelecting = false;
            UpdateSelectionBox(evt.localPosition);
            SelectClipsInRect(CreateRect(rectangleStart, evt.localPosition));
            HideSelectionBox();
            if (MouseCaptureController.HasMouseCapture(trackField))
                MouseCaptureController.ReleaseMouse(trackField);
            evt.StopPropagation();
        }

        void UpdateSelectionBox(Vector2 end)
        {
            Rect rect = CreateRect(rectangleStart, end);
            selectionBox.style.display = DisplayStyle.Flex;
            selectionBox.style.left = rect.xMin;
            selectionBox.style.top = rect.yMin;
            selectionBox.style.width = rect.width;
            selectionBox.style.height = rect.height;
        }

        void HideSelectionBox()
        {
            rectangleSelecting = false;
            selectionBox.style.display = DisplayStyle.None;
            selectionBox.style.left = 0;
            selectionBox.style.top = 0;
            selectionBox.style.width = 0;
            selectionBox.style.height = 0;
        }

        void SelectClipsInRect(Rect localRect)
        {
            if (localRect.width < 3f && localRect.height < 3f)
                return;

            ClearClipSelection();
            for (int i = 0; i < clipViews.Count; i++)
            {
                CommittedActionTimelineClipView clipView = clipViews[i];
                if (!localRect.Overlaps(ToTrackFieldRect(clipView)))
                    continue;

                clipView.SetSelected(true);
                selectedClips.Add(clipView.Selection);
            }

            if (selectedClips.Count == 1)
            {
                SelectClip(selectedClips[0].Variant, selectedClips[0].TrackIndex, selectedClips[0].ClipIndex, selectedClips[0].ClipPath);
            }
            else if (selectedClips.Count > 1)
            {
                selectedClipPath = string.Empty;
                selectedClipIndex = -1;
                selectedTrackIndex = -1;
                selectedTrackStableId = string.Empty;
                selectedClipStableId = string.Empty;
                ApplyTrackSelectionStyles();
                clipInspector.Clear();
                clipInspector.Add(new Label($"{selectedClips.Count} clips selected"));
            }
            else
            {
                selectedTrackStableId = string.Empty;
                selectedClipStableId = string.Empty;
                ApplyTrackSelectionStyles();
            }
        }

        void ClearClipSelection()
        {
            selectedClips.Clear();
            for (int i = 0; i < clipViews.Count; i++)
                clipViews[i].SetSelected(false);
        }

        bool IsClipSelected(CommittedActionTimelineClipSelection selection)
        {
            for (int i = 0; i < selectedClips.Count; i++)
            {
                if (selectedClips[i].Identity.Equals(selection.Identity))
                    return true;
            }

            return false;
        }

        void BeginClipMove(CommittedActionTimelineClipSelection leader)
        {
            if (!IsClipSelected(leader))
                SelectClip(leader.Variant, leader.TrackIndex, leader.ClipIndex, leader.ClipPath);

            clipMoveStartTicks.Clear();
            clipMoveEndTicks.Clear();
            clipMoveAppliedDeltaFrames = 0;
            clipMoveChanged = false;
            for (int i = 0; i < selectedClips.Count; i++)
            {
                SerializedProperty clip = serializedAsset.FindProperty(selectedClips[i].ClipPath);
                if (clip == null)
                {
                    LogClipEdit($"move-begin missing-clip key={selectedClips[i].SelectionKey} path={selectedClips[i].ClipPath}");
                    continue;
                }

                clipMoveStartTicks[selectedClips[i].SelectionKey] = SecondsToTick(clip.FindPropertyRelative("startSeconds").floatValue);
                clipMoveEndTicks[selectedClips[i].SelectionKey] = SecondsToTick(clip.FindPropertyRelative("endSeconds").floatValue);
            }

            LogClipEdit(
                $"move-begin leader={leader.SelectionKey} selected={selectedClips.Count} tracked={clipMoveStartTicks.Count}");
        }

        void MoveClipSelection(CommittedActionTimelineClipSelection leader, float deltaPosition)
        {
            if (clipMoveStartTicks.Count == 0)
                BeginClipMove(leader);

            int clampedDeltaFrames = ResolveSelectionMoveDeltaFrames(deltaPosition);
            if (clampedDeltaFrames == clipMoveAppliedDeltaFrames)
                return;

            int previousDeltaFrames = clipMoveAppliedDeltaFrames;
            clipMoveAppliedDeltaFrames = clampedDeltaFrames;
            clipMoveChanged = true;
            LogClipEdit(
                $"move-apply leader={leader.SelectionKey} deltaPx={deltaPosition:0.###} " +
                $"previousFrames={previousDeltaFrames} frames={clampedDeltaFrames} selected={selectedClips.Count}");
            for (int i = 0; i < selectedClips.Count; i++)
            {
                CommittedActionTimelineClipSelection selection = selectedClips[i];
                if (!clipMoveStartTicks.TryGetValue(selection.SelectionKey, out int startTick))
                {
                    LogClipEdit($"move-apply missing-start key={selection.SelectionKey}");
                    continue;
                }
                if (!clipMoveEndTicks.TryGetValue(selection.SelectionKey, out int endTick))
                {
                    LogClipEdit($"move-apply missing-end key={selection.SelectionKey}");
                    continue;
                }

                adapter.MoveClipRange(
                    selection.Variant,
                    selection.TrackIndex,
                    selection.ClipIndex,
                    TickToSeconds(startTick + clampedDeltaFrames),
                    TickToSeconds(endTick + clampedDeltaFrames),
                    out _);
            }

            for (int i = 0; i < clipViews.Count; i++)
                clipViews[i].RefreshFromSerialized();
        }

        int ResolveSelectionMoveDeltaFrames(float deltaPosition)
        {
            if (clipMoveStartTicks.Count == 0)
                return 0;

            int minStartTick = int.MaxValue;
            int maxEndTick = int.MinValue;
            foreach (int startTick in clipMoveStartTicks.Values)
                minStartTick = Mathf.Min(minStartTick, startTick);
            foreach (int endTick in clipMoveEndTicks.Values)
                maxEndTick = Mathf.Max(maxEndTick, endTick);

            if (minStartTick == int.MaxValue || maxEndTick == int.MinValue)
                return 0;

            int targetStartFrame = GetClosestFrame(FramePosition(minStartTick) + deltaPosition);
            int span = Mathf.Max(1, maxEndTick - minStartTick);
            int currentFrameCount = Mathf.Max(maxPreviewFrame, maxEndTick);
            int maxStartFrame = Mathf.Max(0, currentFrameCount - span);
            targetStartFrame = Mathf.Clamp(targetStartFrame, 0, maxStartFrame);
            return targetStartFrame - minStartTick;
        }

        void EndClipMove()
        {
            bool changed = clipMoveChanged;
            int appliedDeltaFrames = clipMoveAppliedDeltaFrames;
            int tracked = clipMoveStartTicks.Count;
            LogClipEdit($"move-end changed={changed} appliedFrames={appliedDeltaFrames} tracked={tracked} refresh={changed}");
            clipMoveStartTicks.Clear();
            clipMoveEndTicks.Clear();
            clipMoveAppliedDeltaFrames = 0;
            if (changed)
                RefreshTimelineFromSerializedPreservingSelection();
            clipMoveChanged = false;
        }

        void BeginClipResize(CommittedActionTimelineClipSelection selection)
        {
            if (!IsClipSelected(selection))
                SelectClip(selection.Variant, selection.TrackIndex, selection.ClipIndex, selection.ClipPath);

            SerializedProperty clip = serializedAsset.FindProperty(selection.ClipPath);
            if (clip == null)
            {
                LogClipEdit($"resize-begin missing-clip key={selection.SelectionKey} path={selection.ClipPath}");
                clipResizeSelectionKey = string.Empty;
                clipResizeStartTick = 0;
                clipResizeEndTick = 1;
                clipResizeAppliedStartTick = 0;
                clipResizeAppliedEndTick = 1;
                clipResizeChanged = false;
                return;
            }

            clipResizeSelectionKey = selection.SelectionKey;
            clipResizeStartTick = SecondsToTick(clip.FindPropertyRelative("startSeconds").floatValue);
            clipResizeEndTick = SecondsToTick(clip.FindPropertyRelative("endSeconds").floatValue);
            if (clipResizeEndTick <= clipResizeStartTick)
                clipResizeEndTick = clipResizeStartTick + 1;
            clipResizeAppliedStartTick = clipResizeStartTick;
            clipResizeAppliedEndTick = clipResizeEndTick;
            clipResizeChanged = false;
            LogClipEdit(
                $"resize-begin key={selection.SelectionKey} selected={selectedClips.Count} " +
                $"start={clipResizeStartTick} end={clipResizeEndTick}");
        }

        void ResizeClip(CommittedActionTimelineClipSelection selection, int border, float deltaPosition)
        {
            if (clipResizeSelectionKey != selection.SelectionKey)
                BeginClipResize(selection);

            if (string.IsNullOrWhiteSpace(clipResizeSelectionKey))
                return;

            int targetStartTick = clipResizeStartTick;
            int targetEndTick = clipResizeEndTick;
            if (border == 0)
            {
                targetStartTick = GetClosestFrame(FramePosition(clipResizeStartTick) + deltaPosition);
                targetStartTick = Mathf.Clamp(targetStartTick, 0, Mathf.Max(0, clipResizeEndTick - 1));
            }
            else
            {
                targetEndTick = GetClosestFrame(FramePosition(clipResizeEndTick) + deltaPosition);
                targetEndTick = Mathf.Max(clipResizeStartTick + 1, targetEndTick);
                EnsureFrameMapCovers(targetEndTick + 1);
            }

            if (targetStartTick == clipResizeAppliedStartTick &&
                targetEndTick == clipResizeAppliedEndTick)
                return;

            clipResizeAppliedStartTick = targetStartTick;
            clipResizeAppliedEndTick = targetEndTick;
            clipResizeChanged = true;
            LogClipEdit(
                $"resize-apply key={selection.SelectionKey} border={border} deltaPx={deltaPosition:0.###} " +
                $"start={targetStartTick} end={targetEndTick}");
            adapter.ResizeClip(
                selection.Variant,
                selection.TrackIndex,
                selection.ClipIndex,
                TickToSeconds(targetStartTick),
                TickToSeconds(targetEndTick),
                out _);
            for (int i = 0; i < clipViews.Count; i++)
                clipViews[i].RefreshFromSerialized();
            RepaintTimelineField();
        }

        void EndClipResize()
        {
            bool changed = clipResizeChanged;
            string key = clipResizeSelectionKey;
            int startTick = clipResizeAppliedStartTick;
            int endTick = clipResizeAppliedEndTick;
            LogClipEdit($"resize-end key={key} changed={changed} start={startTick} end={endTick} refresh={changed}");
            clipResizeSelectionKey = string.Empty;
            clipResizeStartTick = 0;
            clipResizeEndTick = 1;
            clipResizeAppliedStartTick = 0;
            clipResizeAppliedEndTick = 1;
            if (changed)
                RefreshTimelineFromSerializedPreservingSelection();
            clipResizeChanged = false;
        }

        void SelectClipView(string clipPropertyPath)
        {
            for (int i = 0; i < clipViews.Count; i++)
            {
                if (clipViews[i].ClipPath != clipPropertyPath)
                    continue;

                clipViews[i].SetSelected(true);
                selectedClips.Add(clipViews[i].Selection);
                return;
            }
        }

        void ApplyTrackSelectionStyles()
        {
            ApplyTrackSelectionStyles(trackHandleContainer, selectedTrackStableId);
            ApplyTrackSelectionStyles(trackField, selectedTrackStableId);
        }

        static void ApplyTrackSelectionStyles(VisualElement root, string selectedStableId)
        {
            if (root == null)
                return;

            foreach (VisualElement child in root.Children())
            {
                string stableId = child.userData as string;
                if (!string.IsNullOrWhiteSpace(stableId) &&
                    string.Equals(stableId, selectedStableId, StringComparison.Ordinal))
                    child.AddToClassList("selected");
                else
                    child.RemoveFromClassList("selected");
            }
        }

        Rect ToTrackFieldRect(VisualElement element)
        {
            Vector2 min = trackField.WorldToLocal(element.worldBound.min);
            Vector2 max = trackField.WorldToLocal(element.worldBound.max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static Rect CreateRect(Vector2 start, Vector2 end)
        {
            return Rect.MinMaxRect(
                Mathf.Min(start.x, end.x),
                Mathf.Min(start.y, end.y),
                Mathf.Max(start.x, end.x),
                Mathf.Max(start.y, end.y));
        }

        int FrameFromLocalX(float x)
        {
            return Mathf.Clamp(GetClosestFrame(x), 0, maxPreviewFrame);
        }

        void RebuildFramePositions(int maxFrame)
        {
            framePosMap.Clear();
            for (int i = 0; i <= maxFrame; i++)
                framePosMap[i] = i * CommittedActionTimelineClipView.FrameWidth + FieldOffsetX;
        }

        void EnsureFrameMapCovers(int frame)
        {
            if (frame <= maxPreviewFrame && framePosMap.ContainsKey(frame))
                return;

            maxPreviewFrame = Mathf.Max(maxPreviewFrame, frame);
            for (int i = 0; i <= maxPreviewFrame + 1; i++)
            {
                if (!framePosMap.ContainsKey(i))
                    framePosMap[i] = i * CommittedActionTimelineClipView.FrameWidth + FieldOffsetX;
            }
            ResizeTimeField();
        }

        void ResizeTimeField()
        {
            int requiredFrame = Mathf.Max(maxPreviewFrame + 1, Mathf.CeilToInt(trackScrollView.contentViewport.layout.width / CommittedActionTimelineClipView.FrameWidth) + 1);
            for (int i = framePosMap.Count; i <= requiredFrame; i++)
                framePosMap[i] = i * CommittedActionTimelineClipView.FrameWidth + FieldOffsetX;

            float contentWidth = Mathf.Max(
                720f,
                FramePosition(Mathf.Max(maxPreviewFrame + 1, requiredFrame)) + 160f,
                trackScrollView.contentViewport.layout.width + trackScrollView.scrollOffset.x);
            fieldContent.style.width = contentWidth;
            trackField.style.height = Mathf.Max(trackScrollView.contentViewport.layout.height, TrackTopOffset + ActiveTrackCount() * (TrackHeight + TrackSpacing));
            drawFrameLineField.style.height = trackField.style.height;
        }

        void RepaintTimelineField()
        {
            markerField.MarkDirtyRepaint();
            trackField.MarkDirtyRepaint();
            timeLocator.MarkDirtyRepaint();
            drawFrameLineField.MarkDirtyRepaint();
        }

        void DrawMarkerField(MeshGenerationContext context)
        {
            DrawRuler(context, true);
        }

        void DrawTrackField(MeshGenerationContext context)
        {
            var paint = context.painter2D;
            paint.strokeColor = new Color(0.35f, 0.35f, 0.35f, 0.45f);
            paint.BeginPath();
            int interval = Mathf.Max(1, Mathf.CeilToInt(1f / Mathf.Max(0.01f, fieldScale)));
            int startTick = GetClosestCeilFrame(trackScrollView.scrollOffset.x);
            int endTick = GetClosestFloorFrame(trackScrollView.scrollOffset.x + trackScrollView.contentViewport.layout.width);
            for (int tick = startTick; tick <= endTick; tick++)
            {
                if (tick % (interval * 5) != 0)
                    continue;

                float x = FramePosition(tick);
                paint.MoveTo(new Vector2(x, 0));
                paint.LineTo(new Vector2(x, Mathf.Max(trackField.layout.height, trackScrollView.contentViewport.layout.height)));
            }
            paint.Stroke();
        }

        void DrawRuler(MeshGenerationContext context, bool drawText)
        {
            var paint = context.painter2D;
            paint.strokeColor = Color.white;
            paint.BeginPath();
            int interval = Mathf.Max(1, Mathf.CeilToInt(1f / Mathf.Max(0.01f, fieldScale)));
            int startTick = GetClosestCeilFrame(trackScrollView.scrollOffset.x);
            int endTick = GetClosestFloorFrame(trackScrollView.scrollOffset.x + trackScrollView.contentViewport.layout.width);
            for (int tick = startTick; tick <= endTick; tick++)
            {
                bool major = tick % (interval * 5) == 0;
                if (!major && tick % interval != 0)
                    continue;

                float x = FramePosition(tick);
                paint.MoveTo(new Vector2(x, major ? 10 : 20));
                paint.LineTo(new Vector2(x, 25));
                if (drawText && major)
                    context.DrawText(TickToSeconds(tick).ToString("0.##"), new Vector2(x + 5, 5), TimeTextFontSize, Color.white);
            }
            paint.Stroke();
        }

        void DrawTimeLocator(MeshGenerationContext context)
        {
            var paint = context.painter2D;
            paint.strokeColor = Color.white;
            paint.BeginPath();
            paint.MoveTo(new Vector2(0, 25));
            paint.LineTo(new Vector2(0, Mathf.Max(trackField.layout.height, trackScrollView.contentViewport.layout.height)));
            paint.Stroke();
        }

        void DrawFrameLineField(MeshGenerationContext context)
        {
        }

        float FramePosition(int frame)
        {
            if (!framePosMap.TryGetValue(frame, out float x))
            {
                x = frame * CommittedActionTimelineClipView.FrameWidth + FieldOffsetX;
                framePosMap[frame] = x;
            }

            return x;
        }

        int GetClosestFrame(float position)
        {
            int frame = Mathf.RoundToInt((position - FieldOffsetX) / CommittedActionTimelineClipView.FrameWidth);
            return Mathf.Max(0, frame);
        }

        int GetClosestFloorFrame(float position)
        {
            int frame = Mathf.FloorToInt((position - FieldOffsetX) / CommittedActionTimelineClipView.FrameWidth);
            return Mathf.Max(0, frame);
        }

        int GetClosestCeilFrame(float position)
        {
            int frame = Mathf.CeilToInt((position - FieldOffsetX) / CommittedActionTimelineClipView.FrameWidth);
            return Mathf.Max(0, frame);
        }

        static int SecondsToTick(float seconds)
        {
            ActionTimelineCompileContext context = PreviewCompileContext;
            return ActionTimelineQuantizer.QuantizeSecondsToTick(Mathf.Max(0f, seconds), in context);
        }

        static float TickToSeconds(int tick)
        {
            return tick * PreviewFixedTickSeconds;
        }

        int ActiveTrackCount()
        {
            if (adapter == null || !adapter.TryGetTimelineProperty(activeVariant, out SerializedProperty timeline, out _))
                return 0;

            SerializedProperty tracks = timeline.FindPropertyRelative("tracks");
            return tracks != null ? tracks.arraySize : 0;
        }

        void AddStyle(string path)
        {
            StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (style != null)
                styleSheets.Add(style);
        }

        static string VariantLabel(CommittedActionTimelineVariant variant)
        {
            return variant == CommittedActionTimelineVariant.Backstep
                ? "Backstep"
                : variant == CommittedActionTimelineVariant.Generic
                    ? "Committed Action"
                    : "Directional";
        }

    }

    public readonly struct CommittedActionTimelineClipSelection
    {
        public readonly CommittedActionTimelineVariant Variant;
        public readonly int TrackIndex;
        public readonly int ClipIndex;
        public readonly string TrackStableId;
        public readonly string ClipStableId;
        public readonly string ClipPath;

        public CommittedActionTimelineClipSelection(
            CommittedActionTimelineVariant variant,
            int trackIndex,
            int clipIndex,
            string trackStableId,
            string clipStableId,
            string clipPath)
        {
            Variant = variant;
            TrackIndex = trackIndex;
            ClipIndex = clipIndex;
            TrackStableId = trackStableId ?? string.Empty;
            ClipStableId = clipStableId ?? string.Empty;
            ClipPath = clipPath ?? string.Empty;
        }

        public CommittedActionTimelineClipIdentity Identity =>
            new CommittedActionTimelineClipIdentity(Variant, TrackStableId, ClipStableId);

        public string SelectionKey => $"{Variant}:{TrackStableId}:{ClipStableId}";
    }

    public sealed class CommittedActionTimelineTrackHandle : VisualElement
    {
        const float TrackHeight = 30f;
        const float TrackSpacing = 10f;
        const float TrackTopOffset = 5f;

        readonly CommittedActionTimelineSerializedAdapter adapter;
        readonly CommittedActionTimelineVariant variant;
        readonly int trackIndex;
        readonly Action<CommittedActionTimelineVariant, int, string> onSelected;
        readonly Action onChanged;
        readonly string trackPath;
        bool dragging;
        float dragStartY;
        int dragStartIndex;
        int dragTargetIndex;

        public CommittedActionTimelineTrackHandle(
            CommittedActionTimelineSerializedAdapter adapter,
            CommittedActionTimelineVariant variant,
            int trackIndex,
            SerializedProperty track,
            int displayIndex,
            Action<CommittedActionTimelineVariant, int, string> onSelected,
            Action onChanged)
        {
            this.adapter = adapter;
            this.variant = variant;
            this.trackIndex = trackIndex;
            this.onSelected = onSelected;
            this.onChanged = onChanged;
            trackPath = track.propertyPath;
            userData = CommittedActionTimelineSerializedAdapter.ReadStableId(track);
            name = "timeline-track-handle";
            AddToClassList("timelineTrackHandle");
            style.top = TrackTopOffset + displayIndex * (TrackHeight + TrackSpacing);
            VisualElement icon = new VisualElement { name = "icon" };
            TextField nameField = new TextField { name = "name-field", isReadOnly = true };
            Button deleteButton = new Button(Delete) { name = "delete-track-button", text = "x", tooltip = "Delete Track" };
            deleteButton.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
            Add(icon);
            Add(nameField);
            Add(deleteButton);

            nameField.SetValueWithoutNotify(ResolveTrackName(track));
            RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0)
                    BeginDrag(evt);
            });
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            this.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Select Track", _ => Select());
                evt.menu.AppendAction("Move Up", _ => Move(-1));
                evt.menu.AppendAction("Move Down", _ => Move(1));
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Delete Track", _ => Delete());
            }));
        }

        void Select()
        {
            onSelected?.Invoke(variant, trackIndex, trackPath);
            AddToClassList("selected");
        }

        void BeginDrag(PointerDownEvent evt)
        {
            Select();
            dragging = true;
            dragStartY = evt.position.y;
            dragStartIndex = trackIndex;
            dragTargetIndex = trackIndex;
            MouseCaptureController.CaptureMouse(this);
            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!dragging || !MouseCaptureController.HasMouseCapture(this))
                return;

            float rowHeight = TrackHeight + TrackSpacing;
            int deltaRows = Mathf.RoundToInt((evt.position.y - dragStartY) / rowHeight);
            int nextIndex = Mathf.Clamp(dragStartIndex + deltaRows, 0, ResolveTrackCount() - 1);
            dragTargetIndex = nextIndex;
            style.top = TrackTopOffset + nextIndex * rowHeight;
            evt.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (!dragging)
                return;

            dragging = false;
            if (MouseCaptureController.HasMouseCapture(this))
                MouseCaptureController.ReleaseMouse(this);

            if (dragTargetIndex != trackIndex)
                adapter.ReorderTrack(variant, trackIndex, dragTargetIndex, out _);
            onChanged?.Invoke();
            evt.StopPropagation();
        }

        void Move(int delta)
        {
            adapter.ReorderTrack(variant, trackIndex, trackIndex + delta, out _);
            onChanged?.Invoke();
        }

        void Delete()
        {
            adapter.RemoveTrack(variant, trackIndex, out string ignored);
            onChanged?.Invoke();
        }

        int ResolveTrackCount()
        {
            if (!adapter.TryGetTimelineProperty(variant, out SerializedProperty timeline, out _))
                return 1;

            SerializedProperty tracks = timeline.FindPropertyRelative("tracks");
            return Mathf.Max(1, tracks != null ? tracks.arraySize : 1);
        }

        static string ResolveTrackName(SerializedProperty track)
        {
            SerializedProperty kind = track.FindPropertyRelative("kind");
            return kind == null
                ? "Track"
                : ((ActionTimelineTrackKind)kind.enumValueIndex).ToString();
        }
    }

    public sealed class CommittedActionTimelineTrackView : VisualElement
    {
        const float TrackHeight = 30f;
        const float TrackSpacing = 10f;
        const float TrackTopOffset = 5f;

        readonly CommittedActionTimelineSerializedAdapter adapter;
        readonly CommittedActionTimelineVariant variant;
        readonly int trackIndex;
        readonly SerializedObject serializedObject;
        readonly string trackPath;
        readonly Action<CommittedActionTimelineVariant, int, string> onTrackSelected;
        readonly Action onChanged;

        public CommittedActionTimelineTrackView(
            CommittedActionTimelineSerializedAdapter adapter,
            CommittedActionTimelineVariant variant,
            int trackIndex,
            SerializedProperty track,
            int durationTicks,
            int displayIndex,
            Action<CommittedActionTimelineVariant, int, string> onTrackSelected,
            Action<CommittedActionTimelineVariant, int, int, string> onClipSelected,
            Func<CommittedActionTimelineClipSelection, bool> isClipSelected,
            Action<CommittedActionTimelineClipSelection> onClipMoveStarted,
            Action<CommittedActionTimelineClipSelection, float> onClipMove,
            Action onClipMoveEnded,
            Action<CommittedActionTimelineClipSelection> onClipResizeStarted,
            Action<CommittedActionTimelineClipSelection, int, float> onClipResize,
            Action onClipResizeEnded,
            Action onChanged,
            List<CommittedActionTimelineClipView> clipViews)
        {
            this.adapter = adapter;
            this.variant = variant;
            this.trackIndex = trackIndex;
            this.onTrackSelected = onTrackSelected;
            this.onChanged = onChanged;
            serializedObject = track.serializedObject;
            trackPath = track.propertyPath;
            userData = CommittedActionTimelineSerializedAdapter.ReadStableId(track);
            name = "timeline-track-view";
            AddToClassList("timelineTrack");
            pickingMode = PickingMode.Position;
            style.top = TrackTopOffset + displayIndex * (TrackHeight + TrackSpacing);
            style.width = Mathf.Max(24, durationTicks) * CommittedActionTimelineClipView.FrameWidth + 160;
            VisualElement dropArea = new VisualElement { name = "drop-area", pickingMode = PickingMode.Position };
            dropArea.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            Add(dropArea);

            SerializedProperty clips = track.FindPropertyRelative("clips");
            for (int i = 0; clips != null && i < clips.arraySize; i++)
            {
                CommittedActionTimelineClipView clipView = new CommittedActionTimelineClipView(
                    adapter,
                    variant,
                    trackIndex,
                    i,
                    track.propertyPath,
                    clips.GetArrayElementAtIndex(i),
                    onClipSelected,
                    isClipSelected,
                    onClipMoveStarted,
                    onClipMove,
                    onClipMoveEnded,
                    onClipResizeStarted,
                    onClipResize,
                    onClipResizeEnded,
                    onChanged);
                clipViews.Add(clipView);
                Add(clipView);
            }

            this.AddManipulator(new ContextualMenuManipulator(BuildMenu));
            RegisterCallback<PointerDownEvent>(OnPointerDown);
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
                return;

            onTrackSelected?.Invoke(variant, trackIndex, trackPath);
            AddToClassList("selected");
            evt.StopPropagation();
        }

        void BuildMenu(ContextualMenuPopulateEvent evt)
        {
            SerializedProperty track = serializedObject.FindProperty(trackPath);
            ActionTimelineTrackKind trackKind = track != null
                ? (ActionTimelineTrackKind)track.FindPropertyRelative("kind").enumValueIndex
                : ActionTimelineTrackKind.None;
            ActionTimelineClipKind clipKind = CommittedActionTimelineSerializedAdapter.DefaultClipKind(trackKind);
            int startTick = Mathf.Max(0, Mathf.RoundToInt((evt.localMousePosition.x - CommittedActionTimelineClipView.FieldOffsetX) / CommittedActionTimelineClipView.FrameWidth));
            evt.menu.AppendAction($"Add {clipKind}", _ =>
            {
                adapter.AddClip(
                    variant,
                    trackIndex,
                    clipKind,
                    CommittedActionTimelineClipView.TickToSeconds(startTick),
                    CommittedActionTimelineClipView.TickToSeconds(startTick + 3),
                    out string ignored);
                onChanged?.Invoke();
            });
            evt.menu.AppendAction("Delete Track", _ =>
            {
                adapter.RemoveTrack(variant, trackIndex, out string ignored);
                onChanged?.Invoke();
            });
        }
    }

    public sealed class CommittedActionTimelineClipView : VisualElement
    {
        const string ClipUxmlPath = "Assets/Editor/Character/Action/Timeline/RefPortedResources/VisualTree/CommittedActionTimelineClipView.uxml";

        static float frameWidth = 22f;
        const float TrackHeight = 30f;
        public const float FieldOffsetX = 6f;

        public enum ResizeMode
        {
            Move,
            Left,
            Right
        }

        readonly CommittedActionTimelineSerializedAdapter adapter;
        readonly CommittedActionTimelineVariant variant;
        readonly int trackIndex;
        readonly int clipIndex;
        readonly SerializedObject serializedObject;
        readonly string trackPath;
        readonly string clipPath;
        readonly string trackStableId;
        readonly string clipStableId;
        readonly Action<CommittedActionTimelineVariant, int, int, string> onSelected;
        readonly Func<CommittedActionTimelineClipSelection, bool> isSelected;
        readonly Action<CommittedActionTimelineClipSelection> onMoveStarted;
        readonly Action<CommittedActionTimelineClipSelection, float> onMove;
        readonly Action onMoveEnded;
        readonly Action<CommittedActionTimelineClipSelection> onResizeStarted;
        readonly Action<CommittedActionTimelineClipSelection, int, float> onResize;
        readonly Action onResizeEnded;
        readonly Action onChanged;
        readonly Label clipName;
        readonly VisualElement leftResizeHandle;
        readonly VisualElement rightResizeHandle;
        readonly CommittedActionTimelineDragManipulator moveDrag;
        readonly CommittedActionTimelineDragLineManipulator leftResizeDragLine;
        readonly CommittedActionTimelineDragLineManipulator rightResizeDragLine;

        public CommittedActionTimelineClipView(
            CommittedActionTimelineSerializedAdapter adapter,
            CommittedActionTimelineVariant variant,
            int trackIndex,
            int clipIndex,
            string trackPath,
            SerializedProperty clip,
            Action<CommittedActionTimelineVariant, int, int, string> onSelected,
            Func<CommittedActionTimelineClipSelection, bool> isSelected,
            Action<CommittedActionTimelineClipSelection> onMoveStarted,
            Action<CommittedActionTimelineClipSelection, float> onMove,
            Action onMoveEnded,
            Action<CommittedActionTimelineClipSelection> onResizeStarted,
            Action<CommittedActionTimelineClipSelection, int, float> onResize,
            Action onResizeEnded,
            Action onChanged)
        {
            this.adapter = adapter;
            this.variant = variant;
            this.trackIndex = trackIndex;
            this.clipIndex = clipIndex;
            this.trackPath = trackPath;
            serializedObject = clip.serializedObject;
            clipPath = clip.propertyPath;
            SerializedProperty track = serializedObject.FindProperty(trackPath);
            trackStableId = CommittedActionTimelineSerializedAdapter.ReadStableId(track);
            clipStableId = CommittedActionTimelineSerializedAdapter.ReadStableId(clip);
            this.onSelected = onSelected;
            this.isSelected = isSelected;
            this.onMoveStarted = onMoveStarted;
            this.onMove = onMove;
            this.onMoveEnded = onMoveEnded;
            this.onResizeStarted = onResizeStarted;
            this.onResize = onResize;
            this.onResizeEnded = onResizeEnded;
            this.onChanged = onChanged;
            name = "timeline-clip-view";
            AddToClassList("timelineClip");
            pickingMode = PickingMode.Position;
            focusable = true;
            style.position = Position.Absolute;
            style.height = TrackHeight;

            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ClipUxmlPath);
            if (visualTree == null)
                throw new InvalidOperationException($"timeline-clip-uxml-missing:{ClipUxmlPath}");

            visualTree.CloneTree(this);
            clipName = this.Q<Label>("clip-name");
            leftResizeHandle = this.Q("left-mixer");
            rightResizeHandle = this.Q("right-mixer");
            EnsurePointerTarget(this.Q("content"));
            EnsurePointerTarget(this.Q("title"));
            EnsurePointerTarget(leftResizeHandle);
            EnsurePointerTarget(rightResizeHandle);
            moveDrag = new CommittedActionTimelineDragManipulator(OnStartDrag, OnStopDrag, OnDragMove)
            {
                Enabled = false
            };
            this.AddManipulator(moveDrag);
            leftResizeDragLine = new CommittedActionTimelineDragLineManipulator(
                CommittedActionTimelineDragLineDirection.Left,
                delta => OnResizeDrag(0, delta),
                OnResizeStart,
                OnResizeStop);
            leftResizeDragLine.Size = 4f;
            this.AddManipulator(leftResizeDragLine);
            rightResizeDragLine = new CommittedActionTimelineDragLineManipulator(
                CommittedActionTimelineDragLineDirection.Right,
                delta => OnResizeDrag(1, delta),
                OnResizeStart,
                OnResizeStop);
            rightResizeDragLine.Size = 4f;
            this.AddManipulator(rightResizeDragLine);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            this.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Select Clip", _ => Select());
                evt.menu.AppendAction("Delete Clip", _ =>
                {
                    adapter.RemoveClip(variant, trackIndex, clipIndex, out string ignored);
                    onChanged?.Invoke();
                });
            }));
            RefreshFromSerialized();
        }

        public static float FrameWidth => frameWidth;
        public string ClipPath => clipPath;
        public CommittedActionTimelineClipSelection Selection => new CommittedActionTimelineClipSelection(
            variant,
            trackIndex,
            clipIndex,
            trackStableId,
            clipStableId,
            clipPath);

        public static void SetFrameWidth(float value)
        {
            frameWidth = Mathf.Clamp(value, 8f, 80f);
        }

        public bool ContainsTick(int tick)
        {
            SerializedProperty clip = serializedObject.FindProperty(clipPath);
            if (clip == null)
                return false;

            int start = SecondsToTick(clip.FindPropertyRelative("startSeconds").floatValue);
            int end = SecondsToTick(clip.FindPropertyRelative("endSeconds").floatValue);
            return start <= tick && tick < Mathf.Max(start + 1, end);
        }

        public void SetPreviewActive(bool active)
        {
            if (active)
                AddToClassList("previewActive");
            else
                RemoveFromClassList("previewActive");
        }

        public void SetSelected(bool selected)
        {
            if (selected)
                AddToClassList("selected");
            else
                RemoveFromClassList("selected");
        }

        void Select()
        {
            onSelected?.Invoke(variant, trackIndex, clipIndex, clipPath);
        }

        public void RefreshFromSerialized()
        {
            SerializedProperty clip = serializedObject.FindProperty(clipPath);
            SerializedProperty track = serializedObject.FindProperty(trackPath);
            if (clip == null)
                return;

            float start = clip.FindPropertyRelative("startSeconds").floatValue;
            float end = clip.FindPropertyRelative("endSeconds").floatValue;
            SerializedProperty kind = clip.FindPropertyRelative("kind");
            string label = kind != null ? ((ActionTimelineClipKind)kind.enumValueIndex).ToString() : "Clip";

            clipName.text = label;
            style.left = SecondsToTick(start) * FrameWidth + FieldOffsetX;
            style.width = Mathf.Max(1, SecondsToTick(end) - SecondsToTick(start)) * FrameWidth;
            if (CommittedActionTimelineEditorValidator.IsClipInvalid(track, clip))
                AddToClassList("invalid");
            else
                RemoveFromClassList("invalid");
        }

        static void EnsurePointerTarget(VisualElement target)
        {
            if (target == null)
                return;

            target.pickingMode = PickingMode.Position;
        }

        public static ResizeMode ResolvePointerModeFromHandleBounds(Vector2 pointerPosition, Rect leftHandleWorldBound, Rect rightHandleWorldBound)
        {
            if (leftHandleWorldBound.Contains(pointerPosition))
                return ResizeMode.Left;
            if (rightHandleWorldBound.Contains(pointerPosition))
                return ResizeMode.Right;
            return ResizeMode.Move;
        }

        public static bool ShouldApplyPointerDelta(int previousFrame, int nextFrame, bool resizing)
        {
            return previousFrame != nextFrame;
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
                return;

            if (isSelected == null || !isSelected(Selection))
                Select();
            moveDrag.Enabled = true;
            moveDrag.DragBeginForce(evt, evt.position);
        }

        void OnStartDrag(PointerDownEvent evt)
        {
            onMoveStarted?.Invoke(Selection);
        }

        void OnDragMove(Vector2 deltaPosition)
        {
            onMove?.Invoke(Selection, deltaPosition.x);
        }

        void OnStopDrag()
        {
            moveDrag.Enabled = false;
            onMoveEnded?.Invoke();
        }

        void OnResizeStart(PointerDownEvent evt)
        {
            if (isSelected == null || !isSelected(Selection))
                Select();
            onResizeStarted?.Invoke(Selection);
        }

        void OnResizeDrag(int border, Vector2 deltaPosition)
        {
            onResize?.Invoke(Selection, border, deltaPosition.x);
        }

        void OnResizeStop()
        {
            onResizeEnded?.Invoke();
        }

        internal static float FramePosition(int frame, float frameWidth, float fieldOffsetX)
        {
            return frame * Mathf.Max(0.01f, frameWidth) + fieldOffsetX;
        }

        internal static int GetClosestFrame(float position, float frameWidth, float fieldOffsetX)
        {
            return Mathf.Max(0, Mathf.RoundToInt((position - fieldOffsetX) / Mathf.Max(0.01f, frameWidth)));
        }

        internal static int ResolveFrameFromPixelDelta(int anchorFrame, float deltaPosition, float frameWidth, float fieldOffsetX)
        {
            return GetClosestFrame(FramePosition(anchorFrame, frameWidth, fieldOffsetX) + deltaPosition, frameWidth, fieldOffsetX);
        }

        internal static int ResolveFrameDeltaFromPixelDelta(int anchorFrame, float deltaPosition, float frameWidth, float fieldOffsetX)
        {
            return ResolveFrameFromPixelDelta(anchorFrame, deltaPosition, frameWidth, fieldOffsetX) - anchorFrame;
        }

        internal static int ClampLeftResizeTargetFrame(int targetFrame, int endFrame)
        {
            return Mathf.Clamp(targetFrame, 0, Mathf.Max(0, endFrame - 1));
        }

        internal static int ClampRightResizeTargetFrame(int targetFrame, int startFrame)
        {
            return Mathf.Max(startFrame + 1, targetFrame);
        }

        public static int SecondsToTick(float seconds)
        {
            ActionTimelineCompileContext context = ActionTimelineCompileContext.FromTickRate(SimulationTickRate.Default);
            return ActionTimelineQuantizer.QuantizeSecondsToTick(Mathf.Max(0f, seconds), in context);
        }

        public static float TickToSeconds(int tick)
        {
            return tick * ActionTimelineCompileContext.FromTickRate(SimulationTickRate.Default).FixedTickSeconds;
        }
    }
}
