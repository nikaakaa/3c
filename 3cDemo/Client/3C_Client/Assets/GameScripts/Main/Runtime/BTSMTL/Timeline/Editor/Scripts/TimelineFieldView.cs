using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using BTSMTL.Editor;

namespace BTSMTL.Timeline.Editor
{
    sealed class AnimationTimePointView : VisualElement, ISelectable
    {
        readonly TimelineFieldView m_Field;
        readonly Label m_Label;
        readonly int m_OriginalFrame;
        int m_DraftFrame;
        float m_PointerStart;

        public AnimationTimePointView(
            TimelineFieldView field,
            string laneIdentity,
            AnimationTimePointDescriptor point)
        {
            m_Field = field ?? throw new ArgumentNullException(nameof(field));
            Selection = new AnimationTimeSelection(laneIdentity, point.Identity, point.Kind);
            m_OriginalFrame = point.Frame;
            m_DraftFrame = point.Frame;
            style.position = Position.Absolute;
            style.left = m_Field.Geometry.FrameToPosition(point.Frame) - 6f;
            style.top = 7f;
            style.height = 26f;
            style.width = 12f;
            style.minWidth = 12f;
            style.paddingLeft = 0f;
            style.paddingRight = 0f;
            style.borderTopLeftRadius = 6f;
            style.borderTopRightRadius = 6f;
            style.borderBottomLeftRadius = 6f;
            style.borderBottomRightRadius = 6f;
            style.backgroundColor = ColorFor(point.Kind, false);
            tooltip = $"{point.Label} · {point.Frame}F";
            m_Label = new Label(point.Label);
            m_Label.pickingMode = PickingMode.Ignore;
            m_Label.style.display = DisplayStyle.None;
            Add(m_Label);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        public AnimationTimeSelection Selection { get; }
        public ISelection SelectionContainer { get; set; }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || m_Field.RuntimeReadOnly)
                return;
            if (!IsSelected())
            {
                SelectionContainer.ClearSelection();
                SelectionContainer.AddToSelection(this);
            }
            m_PointerStart = evt.position.x;
            m_DraftFrame = m_OriginalFrame;
            this.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!this.HasPointerCapture(evt.pointerId))
                return;
            m_DraftFrame = Mathf.Clamp(
                m_Field.Geometry.PositionToClosestFrame(
                    m_Field.Geometry.FrameToPosition(m_OriginalFrame) + evt.position.x - m_PointerStart),
                0,
                m_Field.TimeDocument.DurationFrame);
            style.left = m_Field.Geometry.FrameToPosition(m_DraftFrame) - 6f;
            tooltip = $"{m_Label.text} · {m_DraftFrame}F";
            evt.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (!this.HasPointerCapture(evt.pointerId))
                return;
            this.ReleasePointer(evt.pointerId);
            if (m_DraftFrame != m_OriginalFrame)
                m_Field.MoveTimePoint(Selection, m_DraftFrame);
            evt.StopPropagation();
        }

        static Color ColorFor(AnimationTimePointKind kind, bool selected)
        {
            if (kind == AnimationTimePointKind.SyncMarker)
                return selected ? new Color(0.3f, 1f, 0.72f, 1f) : new Color(0.18f, 0.68f, 0.5f, 0.95f);
            return selected ? new Color(1f, 0.7f, 0.24f, 1f) : new Color(0.72f, 0.43f, 0.14f, 0.95f);
        }

        public bool IsSelectable() => true;
        public override bool Overlaps(Rect rectangle) => false;
        public bool IsSelected() => ClassListContains("selected");
        public void Select()
        {
            AddToClassList("selected");
            style.backgroundColor = ColorFor(Selection.PointKind, true);
            BringToFront();
        }
        public void Unselect()
        {
            RemoveFromClassList("selected");
            style.backgroundColor = ColorFor(Selection.PointKind, false);
        }
    }

    sealed class TimelineSectionView : VisualElement, ISelectable
    {
        readonly TimelineFieldView m_Field;
        readonly Label m_Label;
        int m_DraftFrame;
        float m_PointerStart;

        public TimelineSectionView(TimelineFieldView field, TimelineSection section)
        {
            m_Field = field ?? throw new ArgumentNullException(nameof(field));
            Section = section ?? throw new ArgumentNullException(nameof(section));
            style.position = Position.Absolute;
            style.top = 2f;
            style.height = 21f;
            style.minWidth = 20f;
            style.paddingLeft = 4f;
            style.paddingRight = 4f;
            style.backgroundColor = new Color(0.76f, 0.42f, 0.12f, 0.95f);
            style.borderTopLeftRadius = 3f;
            style.borderTopRightRadius = 3f;
            style.borderBottomLeftRadius = 3f;
            style.borderBottomRightRadius = 3f;
            m_Label = new Label(section.Name);
            m_Label.pickingMode = PickingMode.Ignore;
            Add(m_Label);
            Refresh();
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0 && evt.clickCount == 2)
                {
                    m_Field.NavigateToSection(Section);
                    evt.StopImmediatePropagation();
                }
            });
        }

        public TimelineSection Section { get; }
        public ISelection SelectionContainer { get; set; }

        public void Refresh()
        {
            style.left = m_Field.Geometry.FrameToPosition(Section.Frame);
            m_Label.text = Section.Name;
            tooltip = $"{Section.Name} · {Section.Frame}F";
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || m_Field.RuntimeReadOnly)
                return;
            if (!IsSelected())
            {
                SelectionContainer.ClearSelection();
                SelectionContainer.AddToSelection(this);
            }
            m_DraftFrame = Section.Frame;
            m_PointerStart = evt.position.x;
            this.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!this.HasPointerCapture(evt.pointerId))
                return;
            m_DraftFrame = Mathf.Max(0, m_Field.Geometry.PositionToClosestFrame(
                m_Field.Geometry.FrameToPosition(Section.Frame) + evt.position.x - m_PointerStart));
            style.left = m_Field.Geometry.FrameToPosition(m_DraftFrame);
            tooltip = $"{Section.Name} · {m_DraftFrame}F";
            evt.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (!this.HasPointerCapture(evt.pointerId))
                return;
            this.ReleasePointer(evt.pointerId);
            if (m_DraftFrame != Section.Frame)
                m_Field.MoveSection(Section, m_DraftFrame);
            evt.StopPropagation();
        }

        public bool IsSelectable() => true;
        public override bool Overlaps(Rect rectangle) => false;
        public bool IsSelected() => ClassListContains("selected");
        public void Select()
        {
            AddToClassList("selected");
            style.backgroundColor = new Color(1f, 0.62f, 0.16f, 1f);
            BringToFront();
        }
        public void Unselect()
        {
            RemoveFromClassList("selected");
            style.backgroundColor = new Color(0.76f, 0.42f, 0.12f, 0.95f);
        }
    }

    public class TimelineFieldView : VisualElement, ISelection, ITimelineInteractionHost
    {
        public new class UxmlFactory : UxmlFactory<TimelineFieldView, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            public UxmlTraits()
            {
                base.focusIndex.defaultValue = 0;
                base.focusable.defaultValue = true;
            }
        }

        public ScrollView TrackScrollView { get; private set; }
        public VisualElement FieldContent { get; private set; }
        public VisualElement TrackField { get; private set; }
        public VisualElement MarkerField { get; private set; }
        public VisualElement DrawFrameLineField { get; private set; }
        public VisualElement TimeLocator { get; private set; }
        public Label LocaterFrameLabel { get; private set; }
        public ScrollView InspectorScrollView { get; private set; }
        public VisualElement ClipInspector { get; private set; }


        const float MaxFieldScale = 10f;
        const float WheelLerpSpeed = 0.2f;

        #region Style
        static CustomStyleProperty<Color> s_FieldLineColor = new CustomStyleProperty<Color>("--field-line-color");
        static CustomStyleProperty<Font> s_MarkerTextFont = new CustomStyleProperty<Font>("--marker-text-font");
        #endregion

        readonly TimelineFrameGeometry m_Geometry;
        readonly TimelineInteractionState m_Interaction;
        readonly TimelineRendering m_Rendering;
        readonly List<TimelineSectionView> m_SectionViews = new List<TimelineSectionView>();
        bool m_RuntimeReadOnly;
        float m_RuntimeVisualTime;
        int m_LocatorDragStartFrame;
        AnimationCurveSelection m_PendingCurveSelection;

        public TimelineEditorView EditorWindow;
        public TimelineData TimelineData => EditorWindow.Timeline;
        internal IAnimationTimeDocumentAdapter TimeDocument => EditorWindow.TimeDocumentAdapter;

        public BiDictionary<Track, TimelineTrackView> TrackViewMap { get; private set; } = new BiDictionary<Track, TimelineTrackView>();
        public List<TimelineTrackView> TrackViews { get; set; } = new List<TimelineTrackView>();
        public DragManipulator LocatorDragManipulator { get; set; }


        public Action OnPopulatedCallback;
        public Action OnGeometryChangedCallback;
        public event Action<float> VerticalScrollChanged;
        public event Action<object> SelectionChanged;

        internal TimelineFrameGeometry Geometry => m_Geometry;
        internal TimelineInteractionState Interaction => m_Interaction;
        internal TimelineRendering Rendering => m_Rendering;
        internal bool RuntimeReadOnly => m_RuntimeReadOnly;
        public int CurrentMinFrame => m_Geometry.PositionToCeilFrame(ScrollViewContentOffset);
        public int CurrentMaxFrame => m_Geometry.PositionToFloorFrame(ScrollViewContentWidth + ScrollViewContentOffset);
        public float OneFrameWidth => m_Geometry.OneFrameWidth;
        public float ScrollViewContentWidth => TrackScrollView.contentContainer.worldBound.width;
        public float ScrollViewContentOffset => TrackScrollView.scrollOffset.x;
        public float ContentWidth => FieldContent.worldBound.width;

        public TimelineFieldView()
        {
            m_Geometry = new TimelineFrameGeometry();
            m_Interaction = new TimelineInteractionState(this);
            m_Rendering = new TimelineRendering(m_Geometry);
            var visualTree = Resources.Load<VisualTreeAsset>("VisualTree/TimelineFieldView");
            visualTree.CloneTree(this);
            AddToClassList("timelineField");

            m_Rendering.SetMarkerTextFont(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

            TrackScrollView = this.Q<ScrollView>("track-scroll");
            TrackScrollView.mode = ScrollViewMode.VerticalAndHorizontal;
            TrackScrollView.RegisterCallback<PointerDownEvent>((e) =>
            {
                if (e.button == 2)
                {
                    m_Interaction.BeginPan(e.localPosition.x);
                    TrackField.AddToClassList("pan");
                }
            });
            TrackScrollView.RegisterCallback<PointerMoveEvent>((e) =>
            {
                if (m_Interaction.IsPanning)
                {
                    TrackScrollView.scrollOffset = new Vector2(
                        TrackScrollView.scrollOffset.x + m_Interaction.UpdatePan(e.localPosition.x),
                        TrackScrollView.scrollOffset.y);
                }
            });
            TrackScrollView.RegisterCallback<PointerOutEvent>((e) =>
            {
                m_Interaction.EndPan();
                TrackField.RemoveFromClassList("pan");
            });
            TrackScrollView.RegisterCallback<PointerUpEvent>((e) =>
            {
                m_Interaction.EndPan();
                TrackField.RemoveFromClassList("pan");
            });
            TrackScrollView.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            TrackScrollView.horizontalScroller.valueChanged += (e) =>
            {
                if (FieldContent.worldBound.width < ScrollViewContentWidth + ScrollViewContentOffset)
                    FieldContent.style.width = ScrollViewContentWidth + ScrollViewContentOffset;
                DrawTimeField();
            };
            TrackScrollView.verticalScroller.valueChanged += OnVerticalScrollChanged;

            FieldContent = this.Q("field-content");
            FieldContent.RegisterCallback<GeometryChangedEvent>(OnTrackFieldGeometryChanged);

            TrackField = this.Q("track-field");
            TrackField.generateVisualContent += context => m_Rendering.DrawTrackGrid(
                context,
                CurrentMinFrame,
                CurrentMaxFrame,
                TrackScrollView.worldBound.height);

            MarkerField = this.Q("marker-field");
            MarkerField.AddToClassList("droppable");
            MarkerField.generateVisualContent += context => m_Rendering.DrawMarker(
                context,
                CurrentMinFrame,
                CurrentMaxFrame);
            MarkerField.RegisterCallback<PointerDownEvent>((e) =>
            {
                if (e.button == 0)
                {
                    SetTimeLocator(m_Geometry.PositionToClosestFrame(e.localPosition.x));
                    LocatorDragManipulator.DragBeginForce(e);
                }
            });
            MarkerField.RegisterCallback<MouseDownEvent>((e) =>
            {
                if (e.button == 0)
                    e.StopImmediatePropagation();
            });
            MarkerField.SetEnabled(false);

            LocatorDragManipulator = new DragManipulator(OnTimeLocatorStartMove, OnTimeLocatorStopMove, OnTimeLocatorMove);
            TimeLocator = this.Q("time-locater");
            TimeLocator.AddManipulator(LocatorDragManipulator);
            TimeLocator.generateVisualContent += context =>
                m_Rendering.DrawPlayhead(context, TrackScrollView.worldBound.height);
            TimeLocator.SetEnabled(false);

            DrawFrameLineField = this.Q("draw-frame-line-field");
            DrawFrameLineField.generateVisualContent += context =>
                m_Rendering.DrawEditOverlay(context, TrackScrollView.worldBound.height);

            LocaterFrameLabel = this.Q<Label>("time-locater-frame-label");

            InspectorScrollView = this.Q<ScrollView>("inspector-scroll");
            InspectorScrollView.RegisterCallback<WheelEvent>((e) => e.StopImmediatePropagation());
            ClipInspector = this.Q("clip-inspector");
            ClipInspector.focusable = true;
            ClipInspector.RegisterCallback<KeyDownEvent>((e) =>
            {
                if (!e.ctrlKey)
                    e.StopImmediatePropagation();
            });
            ClipInspector.RegisterCallback<PointerDownEvent>((e) => e.StopImmediatePropagation());
            ClipInspector.RegisterCallback<MouseDownEvent>((e) => e.StopImmediatePropagation());

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<WheelEvent>(OnWheelEvent);
            RegisterCallback<KeyDownEvent>((e) =>
            {
                switch (e.keyCode)
                {
                    case KeyCode.Delete:
                        {
                            if (m_RuntimeReadOnly)
                                break;
                            if (TimeDocument != null)
                            {
                                AnimationTimeSelection[] points = Selections
                                    .OfType<AnimationTimePointView>()
                                    .Select(view => view.Selection)
                                    .ToArray();
                                for (int i = 0; i < points.Length; i++)
                                    TimeDocument.DeletePoint(points[i]);
                                if (points.Length > 0)
                                    EditorWindow.RefreshTimeDocument(true);
                                break;
                            }
                            if (TimelineData == null)
                                break;
                            TimelineData.ApplyModify(() =>
                            {
                                var selectableToRemove = Selections.ToList();
                                foreach (var selectable in selectableToRemove)
                                {
                                if (selectable is TimelineTrackView trackView)
                                {
                                    TimelineData.RemoveTrack(trackView.Track);
                                }
                                    if (selectable is TimelineClipView clipView)
                                        TimelineData.RemoveClip(clipView.Clip);
                                    if (selectable is TimelineSectionView sectionView)
                                        TimelineData.RemoveSection(sectionView.Section);
                                }
                            }, "Remove");
                        }
                        break;
                    case KeyCode.F:
                        {
                            if (TimeDocument != null)
                            {
                                TrackScrollView.scrollOffset = new Vector2(
                                    m_Geometry.FrameToPosition(TimeDocument.DurationFrame / 2),
                                    TrackScrollView.scrollOffset.y);
                                break;
                            }
                            if (TimelineData == null)
                                break;
                            int startFrame = int.MaxValue;
                            int endFrame = int.MinValue;
                            foreach (var track in TimelineData.Tracks)
                            {
                                foreach (var clip in track.Clips)
                                {
                                    if (clip.StartFrame < startFrame)
                                        startFrame = clip.StartFrame;
                                    if (clip.EndFrame >= endFrame)
                                        endFrame = clip.EndFrame;
                                }
                            }
                            int middleFrame = (startFrame + endFrame) / 2;
                            TrackScrollView.scrollOffset = new Vector2(middleFrame * OneFrameWidth, TrackScrollView.scrollOffset.y);
                        }
                        break;
                }
            });

            this.AddManipulator(new RectangleSelector(() => -localBound.position));
        }

        public void PopulateView()
        {
            IReadOnlyList<object> selectedTargets = m_Interaction.CaptureSelectedTargets();
            TrackField.Clear();
            for (int i = 0; i < m_SectionViews.Count; i++)
                m_SectionViews[i].RemoveFromHierarchy();
            m_SectionViews.Clear();
            m_Interaction.ResetViewState();
            TrackViewMap.Clear();
            TrackViews.Clear();
            PopulateInspector(null);
            UpdateBindState();

            if (TimeDocument != null)
            {
                TimeDocument.Refresh();
                TimeDocument.RequireValid();
                m_Geometry.ResetExtent(Mathf.Max(1, TimeDocument.DurationFrame + 1));
                m_Geometry.Scale = EditorWindow.DocumentScale;
                ResizeTimeField();
                DrawTimeField();
                TrackField.Add(new AnimationTimeDocumentTrackView(this, TimeDocument));
                TrackField.style.minHeight = AnimationTimeDocumentLayout.ContentHeight(TimeDocument) +
                                             TimelineTrackLayout.VerticalMargin * 2f;
                foreach (object target in selectedTargets)
                {
                    if (target is not AnimationTimeSelection selection)
                        continue;
                    AnimationTimePointView point = Elements.OfType<AnimationTimePointView>().FirstOrDefault(view =>
                        string.Equals(view.Selection.LaneIdentity, selection.LaneIdentity, StringComparison.Ordinal) &&
                        string.Equals(view.Selection.ElementIdentity, selection.ElementIdentity, StringComparison.Ordinal));
                    if (point != null)
                        AddToSelection(point);
                }
                if (Selections.Count == 0)
                    TimeDocument.BuildInspector(ClipInspector, null, () => EditorWindow.RefreshTimeDocument(true));
            }
            else if (TimelineData != null)
            {
                TimelineData.UpdateSerializedTimeline();

                int maxFrame = Mathf.Max(0, TimelineData.MaxFrame) + 1;

                m_Geometry.ResetExtent(maxFrame);
                m_Geometry.Scale = TimelineData.Scale;

                ResizeTimeField();
                DrawTimeField();
                PopulateSectionViews();

                foreach (var track in TimelineData.Tracks)
                {
                    TimelineTrackView trackView = new TimelineTrackView();
                    trackView.SelectionContainer = this;
                    trackView.Init(track);

                    RegisterSelectable(trackView);
                    TrackField.Add(trackView);
                    TrackViewMap.Add(track, trackView);
                    TrackViews.Add(trackView);
                }
                TrackField.style.minHeight = TimelineTrackLayout.TotalHeight(TimelineData.Tracks);

                for (int i = 0; i < TrackViews.Count; i++)
                    TrackViews[i].SetRuntimeReadOnly(m_RuntimeReadOnly);

                foreach (object target in selectedTargets)
                {
                    if (target is Track selectedTrack && TrackViewMap.TryGetValue(selectedTrack, out TimelineTrackView selectedTrackView))
                        AddToSelection(selectedTrackView);
                    else if (target is Clip selectedClip &&
                             TrackViewMap.TryGetValue(selectedClip.Track, out TimelineTrackView ownerTrackView) &&
                             ownerTrackView.ClipViewMap.TryGetValue(selectedClip, out TimelineClipView selectedClipView))
                        AddToSelection(selectedClipView);
                    else if (target is TimelineSection selectedSection)
                    {
                        TimelineSectionView sectionView = m_SectionViews.FirstOrDefault(view =>
                            ReferenceEquals(view.Section, selectedSection));
                        if (sectionView != null)
                            AddToSelection(sectionView);
                    }
                }
            }

            OnPopulatedCallback?.Invoke();
            OnVerticalScrollChanged(TrackScrollView.scrollOffset.y);
            RestorePendingCurveSelection();
        }
        public void PopulateInspector(object target)
        {
            ClipInspector.Clear();
            if (target != null)
            {
                switch (target)
                {
                    case AnimationTimeSelection timeSelection:
                        TimeDocument?.BuildInspector(
                            ClipInspector,
                            timeSelection,
                            () => EditorWindow.RefreshTimeDocument(true));
                        break;
                    case Track track:
                        {
                            SerializedProperty serializedProperty = TimelineData.SerializedData.FindPropertyRelative("m_Tracks");
                            serializedProperty = serializedProperty.GetArrayElementAtIndex(TimelineData.Tracks.IndexOf(track));

                            DrawProperties(serializedProperty, target);
                        }
                        break;
                    case AnimationCurveSelection curveSelection:
                        ClipInspector.Add(new AnimationCurveInspectorView(curveSelection));
                        break;
                    case TimelineSection section:
                        PopulateSectionInspector(section);
                        break;
                    case Clip clip:
                        {
                            clip.OnInspectorRepaint = () => PopulateInspector(clip);

                            SerializedProperty serializedProperty = TimelineData.SerializedData.FindPropertyRelative("m_Tracks");
                            serializedProperty = serializedProperty.GetArrayElementAtIndex(TimelineData.Tracks.IndexOf(clip.Track));
                            serializedProperty = serializedProperty.FindPropertyRelative("m_Clips");
                            serializedProperty = serializedProperty.GetArrayElementAtIndex(clip.Track.Clips.IndexOf(clip));

                            DrawProperties(serializedProperty, target);

                            ClipInspectorView clipViewName = clip.GetAttribute<ClipInspectorView>();
                            if (clipViewName != null)
                            {
                                foreach (var clipInspectorViewScriptPair in TimelineEditorUtility.ClipInspectorViewScriptMap)
                                {
                                    if (clipInspectorViewScriptPair.Key.Name == clipViewName.Name)
                                    {
                                        TimelineClipInspectorView clipInspectorView = Activator.CreateInstance(clipInspectorViewScriptPair.Key, clip) as TimelineClipInspectorView;
                                        clipInspectorView.Initialize(EditorWindow);
                                        ClipInspector.Add(clipInspectorView);
                                        return;
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }
        void PopulateSectionViews()
        {
            for (int i = 0; i < TimelineData.Sections.Count; i++)
            {
                TimelineSection section = TimelineData.Sections[i];
                if (section == null)
                    continue;
                var view = new TimelineSectionView(this, section)
                {
                    SelectionContainer = this
                };
                view.SetEnabled(!m_RuntimeReadOnly);
                RegisterSelectable(view);
                MarkerField.Add(view);
                m_SectionViews.Add(view);
            }
            TimeLocator.BringToFront();
        }
        void PopulateSectionInspector(TimelineSection section)
        {
            var title = new Label("Section") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            var name = new TextField("Name") { value = section.Name };
            name.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (!string.Equals(name.value, section.Name, StringComparison.Ordinal))
                    ConfigureSection(section, name.value, section.Frame);
            });
            var frame = new IntegerField("Frame") { value = section.Frame };
            frame.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (frame.value != section.Frame)
                    ConfigureSection(section, section.Name, frame.value);
            });
            var remove = new Button(() => RemoveSection(section)) { text = "Delete Section" };
            ClipInspector.Add(title);
            ClipInspector.Add(name);
            ClipInspector.Add(frame);
            ClipInspector.Add(remove);
            ClipInspector.SetEnabled(!m_RuntimeReadOnly);
        }
        public void DrawProperties(SerializedProperty serializedProperty, object target)
        {
            #region Base
            if (target is Clip clip)
            {
                VisualElement baseInspector = new VisualElement();
                baseInspector.name = "base-inspector";
                ClipInspector.Add(baseInspector);

                IMGUIContainer baseIMGUIContainer = new IMGUIContainer(() =>
                {
                    DrawGUI("Start", clip.StartFrame);
                    DrawGUI("End", clip.EndFrame);
                    if (clip.IsMixable())
                    {
                        DrawGUI("Ease In", clip.EaseInFrame);
                        DrawGUI("Ease Out", clip.EaseOutFrame);
                        DrawGUI("ClipIn", clip.ClipInFrame);
                    }
                    DrawGUI("Duration", clip.Duration);
                });
                baseInspector.Add(baseIMGUIContainer);
            }
            void DrawGUI(string title, int frame)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{title}", GUILayout.Width(100));
                GUILayout.FlexibleSpace();
                GUILayout.Label($"{(frame / (float)TimelineUtility.FrameRate).ToString("0.00")}S  /  {frame}F", GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
            }
            #endregion

            #region Addition

            VisualElement additionalInspector = new VisualElement();
            additionalInspector.name = "additional-inspector";
            ClipInspector.Add(additionalInspector);

            List<VisualElement> visualElements = new List<VisualElement>();
            Dictionary<string, (VisualElement, List<VisualElement>)> groupMap = new Dictionary<string, (VisualElement, List<VisualElement>)>();

            foreach (var fieldInfo in target.GetAllFields())
            {
                if (fieldInfo.GetCustomAttribute<ShowInInspectorAttribute>() is ShowInInspectorAttribute showInInspectorAttribute)
                {
                    if (!fieldInfo.ShowIf(target))
                        continue;

                    if (fieldInfo.HideIf(target))
                        continue;

                    SerializedProperty sp = serializedProperty.FindPropertyRelative(fieldInfo.Name);
                    if (sp != null)
                    {
                        PropertyField propertyField = new PropertyField(sp);
                        propertyField.name = showInInspectorAttribute.Index * 10 + visualElements.Count.ToString();
                        propertyField.userData = fieldInfo.Name;
                        propertyField.BindProperty(sp);

                        fieldInfo.Group(propertyField, showInInspectorAttribute.Index, ref visualElements, ref groupMap);

                        if (fieldInfo.ReadOnly(target))
                            propertyField.SetEnabled(false);

                        if (fieldInfo.GetCustomAttribute<OnValueChangedAttribute>() is OnValueChangedAttribute onValueChanged)
                        {
                            EditorCoroutineHelper.Delay(() =>
                            {
                                propertyField.RegisterValueChangeCallback((e) =>
                                {
                                    foreach (var method in onValueChanged.Methods)
                                    {
                                        target.GetMethod(method)?.Invoke(target, null);
                                    }
                                    if (target is AnimationClip animationClip &&
                                        TrackViewMap.TryGetValue(animationClip.Track, out TimelineTrackView trackView))
                                        trackView.Refresh();
                                });
                            }, 0.01f);
                        }
                    }
                }
            }
            foreach (var propertyInfo in target.GetAllProperties())
            {
                if (!propertyInfo.ShowIf(target))
                    continue;

                if (propertyInfo.HideIf(target))
                    continue;

                if (propertyInfo.GetCustomAttribute<ShowTextAttribute>() is ShowTextAttribute showTextAttribute)
                {
                    IMGUIContainer container = new IMGUIContainer(() =>
                    {
                        GUILayout.Label(propertyInfo.GetValue(target).ToString());
                    });
                    container.name = showTextAttribute.Index * 10 + visualElements.Count.ToString();
                    propertyInfo.Group(container, showTextAttribute.Index, ref visualElements, ref groupMap);
                }
            }
            foreach (var methodInfo in target.GetAllMethods())
            {
                if (!methodInfo.ShowIf(target))
                    continue;

                if (methodInfo.HideIf(target))
                    continue;

                if (methodInfo.GetCustomAttribute<ShowTextAttribute>() is ShowTextAttribute showTextAttribute)
                {
                    IMGUIContainer container = new IMGUIContainer(() =>
                    {
                        GUILayout.Label(methodInfo.Invoke(target, null).ToString());
                    });
                    container.name = showTextAttribute.Index * 10 + visualElements.Count.ToString();
                    methodInfo.Group(container, showTextAttribute.Index, ref visualElements, ref groupMap);
                }

                if (methodInfo.GetCustomAttribute<ButtonAttribute>() is ButtonAttribute buttonAttribute)
                {
                    Button button = new Button();
                    button.name = buttonAttribute.Index * 10 + visualElements.Count.ToString();
                    button.text = string.IsNullOrEmpty(buttonAttribute.Label) ? methodInfo.Name : buttonAttribute.Label;
                    button.clicked += () => methodInfo.Invoke(target, null);
                    methodInfo.Group(button, buttonAttribute.Index, ref visualElements, ref groupMap);
                }
            }

            foreach (var visualElement in visualElements.OrderBy(i => float.Parse(i.name)))
            {
                visualElement.AddToClassList("inspectorElement");
                additionalInspector.Add(visualElement);
            }
            foreach (var groupPair in groupMap)
            {
                foreach (var groupElement in groupPair.Value.Item2.OrderBy(i => float.Parse(i.name)))
                {
                    groupElement.AddToClassList("inspectorElement");
                    groupPair.Value.Item1.Add(groupElement);
                }
            }
            ClipInspector.SetEnabled(!m_RuntimeReadOnly);
            #endregion
        }
        public void UpdateBindState()
        {
            if (EditorWindow == null) return;

            if (m_RuntimeReadOnly && TimelineData != null)
            {
                MarkerField.SetEnabled(true);
                TimeLocator.SetEnabled(true);
            }
            else if (TimeDocument != null || TimelineData != null && EditorWindow.PreviewSession.CanPreview)
            {
                MarkerField.SetEnabled(true);
                TimeLocator.SetEnabled(true);
            }
            else
            {
                MarkerField.SetEnabled(false);
                TimeLocator.SetEnabled(false);
            }
            UpdateTimeLocator();
        }
        public void ForceScrollViewUpdate(ScrollView view)
        {
            view.schedule.Execute(() =>
            {
                var fakeOldRect = Rect.zero;
                var fakeNewRect = view.layout;

                using var evt = GeometryChangedEvent.GetPooled(fakeOldRect, fakeNewRect);
                evt.target = view.contentContainer;
                view.contentContainer.SendEvent(evt);
            });
        }


        #region Selection
        public VisualElement ContentContainer => TrackField;
        public IReadOnlyList<ISelectable> Elements => m_Interaction.Elements;
        public IReadOnlyList<ISelectable> Selections => m_Interaction.Selections;

        internal void RegisterSelectable(ISelectable selectable)
        {
            m_Interaction.RegisterElement(selectable);
        }

        public void AddToSelection(ISelectable selectable)
        {
            m_Interaction.AddToSelection(selectable);
        }
        public void RemoveFromSelection(ISelectable selectable)
        {
            m_Interaction.RemoveFromSelection(selectable);
        }
        public void ClearSelection()
        {
            m_Interaction.ClearSelection();
        }

        internal void SelectAnimationCurve(TimelineClipView clipView, string propertyName)
        {
            ClearSelection();
            AddToSelection(clipView);
            EditorCoroutineHelper.Delay(() =>
            {
                List<PropertyField> fields = ClipInspector.Query<PropertyField>().ToList();
                for (int i = 0; i < fields.Count; i++)
                {
                    if (!string.Equals(fields[i].userData as string, propertyName, StringComparison.Ordinal))
                        continue;
                    InspectorScrollView.ScrollTo(fields[i]);
                    fields[i].Q<CurveField>()?.Focus();
                    break;
                }
            }, 0.01f);
        }

        internal void CommitAuthoringMutation(Action mutation, string undoName, object selectionAfter = null)
        {
            if (m_RuntimeReadOnly)
                throw new InvalidOperationException("Live Debug Timeline is read-only.");
            m_PendingCurveSelection = selectionAfter is AnimationCurveSelection curveSelection
                ? new AnimationCurveSelection(curveSelection.Binding, curveSelection.Owner, curveSelection.KeyIndices)
                : null;
            TimelineData.ApplyModify(mutation, undoName);
            EditorWindow.RefreshPreview(true);
        }

        internal void CommitTimeDocumentCurveMutation(
            string laneIdentity,
            AnimationCurve curve,
            string undoName,
            AnimationCurveSelection selectionAfter)
        {
            if (m_RuntimeReadOnly || TimeDocument == null)
                return;
            m_PendingCurveSelection = selectionAfter == null
                ? null
                : new AnimationCurveSelection(
                    selectionAfter.Binding,
                    selectionAfter.Owner,
                    selectionAfter.KeyIndices);
            TimeDocument.SetCurve(laneIdentity, curve, undoName);
            EditorWindow.RefreshTimeDocument(true);
        }

        internal void MoveTimePoint(AnimationTimeSelection selection, int frame)
        {
            if (m_RuntimeReadOnly || TimeDocument == null)
                return;
            TimeDocument.MovePoint(selection, frame);
            EditorWindow.RefreshTimeDocument(true, selection);
        }

        internal void SetTimeCurve(string laneIdentity, AnimationCurve curve)
        {
            if (m_RuntimeReadOnly || TimeDocument == null)
                return;
            TimeDocument.SetCurve(laneIdentity, curve, "Edit Curve");
            EditorWindow.RefreshTimeDocument(true);
        }

        internal void AddSectionAtCurrentFrame()
        {
            if (m_RuntimeReadOnly)
                return;
            int index = TimelineData.Sections.Count + 1;
            string name;
            do
            {
                name = $"Section {index++}";
            }
            while (TimelineData.Sections.Any(section => section != null && string.Equals(section.Name, name, StringComparison.Ordinal)));
            TimelineSection created = null;
            int frame = Mathf.Max(0, EditorWindow.PreviewSession.Frame);
            CommitAuthoringMutation(() => created = TimelineData.AddSection(name, frame), "Add Section");
            if (created != null)
                SelectSection(created);
        }

        internal void RemoveSelectedSections()
        {
            TimelineSection[] sections = Selections.OfType<TimelineSectionView>()
                .Select(view => view.Section)
                .Distinct()
                .ToArray();
            if (sections.Length == 0)
                return;
            CommitAuthoringMutation(() =>
            {
                for (int i = 0; i < sections.Length; i++)
                    TimelineData.RemoveSection(sections[i]);
            }, "Delete Section");
        }

        internal void MoveSection(TimelineSection section, int frame) =>
            ConfigureSection(section, section.Name, frame);

        void ConfigureSection(TimelineSection section, string name, int frame)
        {
            CommitAuthoringMutation(
                () => TimelineData.ConfigureSection(section, name, Mathf.Max(0, frame)),
                "Configure Section");
            SelectSection(section);
        }

        void RemoveSection(TimelineSection section)
        {
            CommitAuthoringMutation(() => TimelineData.RemoveSection(section), "Delete Section");
        }

        void SelectSection(TimelineSection section)
        {
            TimelineSectionView view = m_SectionViews.FirstOrDefault(candidate => ReferenceEquals(candidate.Section, section));
            if (view == null)
                return;
            ClearSelection();
            AddToSelection(view);
        }

        internal void NavigateToSection(TimelineSection section)
        {
            float position = m_Geometry.FrameToPosition(section.Frame);
            TrackScrollView.scrollOffset = new Vector2(Mathf.Max(0f, position - ScrollViewContentWidth * 0.35f), TrackScrollView.scrollOffset.y);
            SetTimeLocator(section.Frame);
        }

        internal void PresentCurveSelection(AnimationCurveSelection selection)
        {
            m_Interaction.ClearSelection();
            PopulateInspector(selection);
            SelectionChanged?.Invoke(selection);
        }

        void RestorePendingCurveSelection()
        {
            if (m_PendingCurveSelection == null)
                return;
            AnimationCurveSelection selection = m_PendingCurveSelection;
            m_PendingCurveSelection = null;
            if (selection.Owner == null ||
                !string.Equals(
                    selection.Binding.OwnerIdentity(selection.Owner),
                    selection.OwnerAuthoringId,
                    StringComparison.Ordinal) ||
                !selection.Binding.Supports(selection.Owner))
                return;
            PresentCurveSelection(selection);
        }
        #endregion

        #region TimeField
        public void ResizeTimeField()
        {
            if (FieldContent.worldBound.width < ScrollViewContentWidth + ScrollViewContentOffset)
                FieldContent.style.width = ScrollViewContentWidth + ScrollViewContentOffset;
            m_Geometry.ResizeExtent(FieldContent.worldBound.width, worldBound.width);
            UpdateTimeLocator();
            foreach (var trackViewPair in TrackViewMap)
                trackViewPair.Value.Refresh();
        }
        public void DrawTimeField()
        {
            TrackField.MarkDirtyRepaint();
            MarkerField.MarkDirtyRepaint();
        }
        #endregion

        #region TimeLocator
        public void SetTimeLocator(int targetFrame)
        {
            if (m_RuntimeReadOnly)
                return;
            if (TimeDocument != null)
            {
                EditorWindow.SetAuthoringFrame(Mathf.Clamp(targetFrame, 0, TimeDocument.DurationFrame));
                return;
            }
            EditorWindow.PreviewSession.Pause();
            if (targetFrame == EditorWindow.PreviewSession.Frame)
                return;
            EditorWindow.PreviewSession.SetTime(m_Geometry.FrameToTime(targetFrame));
        }
        public void UpdateTimeLocator()
        {
            if (EditorWindow == null) return;

            if (m_RuntimeReadOnly && TimelineData != null)
            {
                m_Rendering.ApplyPlayhead(new TimelinePlayheadRenderInput(
                    TimelinePlayheadMode.LiveDebug,
                    m_RuntimeVisualTime,
                    Mathf.RoundToInt(m_RuntimeVisualTime * TimelineUtility.FrameRate)),
                    TimeLocator,
                    LocaterFrameLabel);
            }
            else if (TimelineData != null && EditorWindow.PreviewSession.CanPreview)
            {
                m_Rendering.ApplyPlayhead(new TimelinePlayheadRenderInput(
                    TimelinePlayheadMode.AuthoringPreview,
                    EditorWindow.PreviewSession.Time,
                    EditorWindow.PreviewSession.Frame),
                    TimeLocator,
                    LocaterFrameLabel);
            }
            else if (TimeDocument != null)
            {
                m_Rendering.ApplyPlayhead(new TimelinePlayheadRenderInput(
                    TimelinePlayheadMode.AuthoringPreview,
                    EditorWindow.AuthoringTime,
                    EditorWindow.AuthoringFrame),
                    TimeLocator,
                    LocaterFrameLabel);
            }
            else
            {
                m_Rendering.ApplyPlayhead(new TimelinePlayheadRenderInput(
                    TimelinePlayheadMode.Empty,
                    0f,
                    0),
                    TimeLocator,
                    LocaterFrameLabel);
            }
        }
        void OnTimeLocatorStartMove(PointerDownEvent ev)
        {
            if (m_RuntimeReadOnly)
                return;
            m_LocatorDragStartFrame = TimeDocument != null
                ? EditorWindow.AuthoringFrame
                : EditorWindow.PreviewSession.Frame;
            LocaterFrameLabel.style.display = DisplayStyle.Flex;
        }
        void OnTimeLocatorMove(Vector2 deltaPosition)
        {
            if (m_RuntimeReadOnly)
                return;
            int targetFrame = m_Geometry.PositionToClosestFrame(
                m_Geometry.FrameToPosition(m_LocatorDragStartFrame) + deltaPosition.x);
            targetFrame = Mathf.Clamp(targetFrame, CurrentMinFrame, CurrentMaxFrame);

            SetTimeLocator(targetFrame);
        }
        void OnTimeLocatorStopMove()
        {
            if (m_RuntimeReadOnly)
                return;
            LocaterFrameLabel.style.display = DisplayStyle.None;
        }

        public void SetRuntimeReadOnly(bool readOnly)
        {
            m_RuntimeReadOnly = readOnly;
            for (int i = 0; i < TrackViews.Count; i++)
                TrackViews[i].SetRuntimeReadOnly(readOnly);
            ClipInspector.SetEnabled(!readOnly);
            UpdateBindState();
        }

        public void SetRuntimeVisualTime(float time)
        {
            m_RuntimeVisualTime = Mathf.Max(0f, time);
            if (m_RuntimeReadOnly)
                UpdateTimeLocator();
        }

        internal void ApplyRuntimeOverlay(TimelineRuntimeOverlayModel model)
        {
            SetRuntimeVisualTime(model.VisualTime);
            m_Rendering.ApplyRuntimeOverlay(model, TrackViews);
        }

        internal void ClearRuntimeOverlay()
        {
            m_Rendering.ClearRuntimeOverlay(TrackViews);
        }
        #endregion

        #region Add Clip
        public void AddClip(Track track, int startFrame)
        {
            AdjustClip(TimelineData.AddClip(track, startFrame));
        }
        public void AddClip(UnityEngine.Object referenceObject, Track track, int startFrame)
        {
            AdjustClip(TimelineData.AddClip(referenceObject, track, startFrame));
        }
        void AdjustClip(Clip clip)
        {
            Clip closestRightClip = m_Geometry.GetClosestRightClip(clip.Track, clip.StartFrame, clip);
            if (closestRightClip != null && clip.StartFrame + clip.Length > closestRightClip.StartFrame)
            {
                clip.EndFrame = closestRightClip.StartFrame;
                GetClipView(clip).Refresh();
            }
        }
        #endregion

        #region Callback
        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (customStyle.TryGetValue(s_FieldLineColor, out var lineColor))
                m_Rendering.SetFieldLineColor(lineColor);
            if (customStyle.TryGetValue(s_MarkerTextFont, out var textFont))
                m_Rendering.SetMarkerTextFont(textFont);
        }
        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ResizeTimeField();
            DrawTimeField();
            OnGeometryChangedCallback?.Invoke();
        }
        void OnTrackFieldGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.newRect.width > evt.oldRect.width)
                m_Geometry.ResizeExtent(evt.newRect.width, ScrollViewContentWidth);
        }
        void OnVerticalScrollChanged(float offset)
        {
            Vector3 pinnedPosition = new Vector3(0f, offset, 0f);
            MarkerField.transform.position = pinnedPosition;
            DrawFrameLineField.transform.position = pinnedPosition;
            VerticalScrollChanged?.Invoke(offset);
        }

        public void SetVerticalScrollOffset(float offset)
        {
            TrackScrollView.scrollOffset = new Vector2(TrackScrollView.scrollOffset.x, Mathf.Max(0f, offset));
        }

        void OnWheelEvent(WheelEvent wheelEvent)
        {
            if (wheelEvent.shiftKey && !wheelEvent.ctrlKey && !wheelEvent.altKey)
            {
                float delta = Mathf.Abs(wheelEvent.delta.x) > Mathf.Abs(wheelEvent.delta.y)
                    ? wheelEvent.delta.x
                    : wheelEvent.delta.y;
                TrackScrollView.scrollOffset = new Vector2(
                    Mathf.Max(0f, TrackScrollView.scrollOffset.x + delta * 20f),
                    TrackScrollView.scrollOffset.y);
                wheelEvent.StopImmediatePropagation();
                return;
            }
            if (!wheelEvent.ctrlKey || wheelEvent.altKey)
                return;

            m_Geometry.Scale = Mathf.Min(MaxFieldScale, m_Geometry.Scale * (1f - wheelEvent.delta.y / 100f));
            if (TimelineData != null)
                TimelineData.Scale = m_Geometry.Scale;
            else if (TimeDocument != null)
                EditorWindow.DocumentScale = m_Geometry.Scale;

            float targetWidth = Mathf.Max(FieldContent.worldBound.width * (1 - wheelEvent.delta.y / 100), ScrollViewContentWidth);
            if (FieldContent.style.width == targetWidth)
            {
                ResizeTimeField();
                DrawTimeField();
            }
            else
            {
                FieldContent.style.width = targetWidth;

                int ratioInt = Mathf.RoundToInt(wheelEvent.localMousePosition.x / worldBound.width);
                if (ratioInt < .1f)
                {
                    ratioInt = 0;
                }
                else if (ratioInt > .9f)
                {
                    ratioInt = 1;
                }
                float targetOffset = -(ScrollViewContentWidth - targetWidth) * ratioInt;
                targetOffset = Mathf.Lerp(ScrollViewContentOffset, targetOffset, WheelLerpSpeed);
                TrackScrollView.scrollOffset = new Vector2(targetOffset, TrackScrollView.scrollOffset.y);

                ResizeTimeField();
                ForceScrollViewUpdate(TrackScrollView);
            }

            OnGeometryChangedCallback?.Invoke();
            if (TimeDocument != null)
                schedule.Execute(PopulateView);
            wheelEvent.StopImmediatePropagation();
        }
        #endregion

        TimelineClipView GetClipView(Clip clip)
        {
            return TrackViewMap[clip.Track].ClipViewMap[clip];
        }

        TimelineFrameGeometry ITimelineInteractionHost.Geometry => m_Geometry;
        TimelineData ITimelineInteractionHost.TimelineData => TimelineData;
        int ITimelineInteractionHost.MinimumVisibleFrame => CurrentMinFrame;
        int ITimelineInteractionHost.MaximumVisibleFrame => CurrentMaxFrame;
        void ITimelineInteractionHost.PresentSelection(object target)
        {
            PopulateInspector(target);
            SelectionChanged?.Invoke(target);
        }
        void ITimelineInteractionHost.RefreshPreview() => EditorWindow.RefreshPreview();

        internal void SetEditFrames(params int[] frames)
        {
            m_Rendering.SetEditFrames(frames);
            DrawFrameLineField.MarkDirtyRepaint();
        }

        void ITimelineInteractionHost.SetEditFrames(params int[] frames) => SetEditFrames(frames);
    }
}
