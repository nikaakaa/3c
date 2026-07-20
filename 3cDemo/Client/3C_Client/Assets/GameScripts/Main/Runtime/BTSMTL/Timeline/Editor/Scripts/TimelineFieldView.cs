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
        bool m_RuntimeReadOnly;
        float m_RuntimeVisualTime;
        int m_LocatorDragStartFrame;
        TimelineCurveSelection m_CurveSelection;
        TimelineCurveSelection m_PendingCurveSelection;

        public TimelineEditorView EditorWindow;
        public TimelineData TimelineData => EditorWindow.Timeline;

        public BiDictionary<Track, TimelineTrackView> TrackViewMap { get; private set; } = new BiDictionary<Track, TimelineTrackView>();
        public List<TimelineTrackView> TrackViews { get; set; } = new List<TimelineTrackView>();
        public DragManipulator LocatorDragManipulator { get; set; }


        public Action OnPopulatedCallback;
        public Action OnGeometryChangedCallback;

        internal TimelineFrameGeometry Geometry => m_Geometry;
        internal TimelineInteractionState Interaction => m_Interaction;
        internal TimelineRendering Rendering => m_Rendering;
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
            RegisterCallback<WheelEvent>(OnlWheelEvent);
            RegisterCallback<KeyDownEvent>((e) =>
            {
                switch (e.keyCode)
                {
                    case KeyCode.Delete:
                        {
                            if (m_RuntimeReadOnly)
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
                                {
                                    TimelineData.RemoveClip(clipView.Clip);
                                }
                                if (selectable is TimelineAnimationMarkerView markerView)
                                {
                                    markerView.Track.DeleteMarker(markerView.Marker.AuthoringId);
                                }
                                }
                            }, "Remove");
                        }
                        break;
                    case KeyCode.F:
                        {
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
            m_Interaction.ResetViewState();
            TrackViewMap.Clear();
            TrackViews.Clear();
            PopulateInspector(null);
            UpdateBindState();

            if (TimelineData != null)
            {
                TimelineData.UpdateSerializedTimeline();

                int maxFrame = 0;
                foreach (var track in TimelineData.Tracks)
                {
                    foreach (var clip in track.Clips)
                    {
                        if (clip.EndFrame >= maxFrame)
                            maxFrame = clip.EndFrame;
                    }
                }
                maxFrame++;

                m_Geometry.ResetExtent(maxFrame);
                m_Geometry.Scale = TimelineData.Scale;

                ResizeTimeField();
                DrawTimeField();

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
                    else if (target is TimelineAnimationMarkerSelection markerSelection &&
                             TrackViewMap.TryGetValue(markerSelection.Track, out TimelineTrackView markerTrackView) &&
                             markerTrackView.TryGetMarkerView(markerSelection.MarkerAuthoringId, out TimelineAnimationMarkerView markerView))
                        AddToSelection(markerView);
                }
            }

            OnPopulatedCallback?.Invoke();
            RestorePendingCurveSelection();
        }
        public void PopulateInspector(object target)
        {
            ClipInspector.Clear();
            if (target != null)
            {
                switch (target)
                {
                    case Track track:
                        {
                            SerializedProperty serializedProperty = TimelineData.SerializedData.FindPropertyRelative("m_Tracks");
                            serializedProperty = serializedProperty.GetArrayElementAtIndex(TimelineData.Tracks.IndexOf(track));

                            DrawProperties(serializedProperty, target);
                            if (track is AnimationTrack animationTrack)
                                ClipInspector.Add(new AnimationMarkerSyncTrackInspectorView(EditorWindow, animationTrack));
                        }
                        break;
                    case TimelineAnimationMarkerSelection markerSelection:
                        ClipInspector.Add(new AnimationMarkerSyncTrackInspectorView(
                            EditorWindow,
                            markerSelection.Track,
                            markerSelection.MarkerAuthoringId));
                        break;
                    case TimelineCurveSelection curveSelection:
                        m_CurveSelection = curveSelection;
                        ClipInspector.Add(new TimelineCurveInspectorView(this, curveSelection));
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
            else if (TimelineData != null && EditorWindow.PreviewSession.CanPreview)
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

        internal void PresentCurveSelection(TimelineCurveSelection selection)
        {
            m_Interaction.ClearSelection();
            m_CurveSelection = selection;
            PopulateInspector(selection);
        }

        internal void CommitAuthoringMutation(Action mutation, string undoName, object selectionAfter = null)
        {
            if (m_RuntimeReadOnly)
                throw new InvalidOperationException("Live Debug Timeline is read-only.");
            TimelineData.ApplyModify(mutation, undoName);
            if (selectionAfter is TimelineCurveSelection curveSelection)
            {
                m_PendingCurveSelection = new TimelineCurveSelection(
                    curveSelection.Owner,
                    curveSelection.Descriptor,
                    curveSelection.KeyIndices);
            }
            else
            {
                m_PendingCurveSelection = null;
            }
            EditorWindow.RefreshPreview(true);
        }

        void RestorePendingCurveSelection()
        {
            if (m_PendingCurveSelection == null)
                return;
            TimelineCurveSelection selection = m_PendingCurveSelection;
            m_PendingCurveSelection = null;
            if (selection.Owner == null ||
                !string.Equals(selection.Owner.AuthoringId, selection.OwnerAuthoringId, StringComparison.Ordinal) ||
                !selection.Descriptor.Supports(selection.Owner))
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
            m_LocatorDragStartFrame = EditorWindow.PreviewSession.Frame;
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
        void OnlWheelEvent(WheelEvent wheelEvent)
        {
            m_Geometry.Scale = Mathf.Min(MaxFieldScale, m_Geometry.Scale * (1f - wheelEvent.delta.y / 100f));
            TimelineData.Scale = m_Geometry.Scale;

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
        void ITimelineInteractionHost.PresentSelection(object target) => PopulateInspector(target);
        void ITimelineInteractionHost.RefreshPreview() => EditorWindow.RefreshPreview();

        internal void SetEditFrames(params int[] frames)
        {
            m_Rendering.SetEditFrames(frames);
            DrawFrameLineField.MarkDirtyRepaint();
        }

        void ITimelineInteractionHost.SetEditFrames(params int[] frames) => SetEditFrames(frames);
    }
}
