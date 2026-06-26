using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Taco.Editor;

namespace Taco.Timeline.Editor
{
    public class TimelineFieldView : VisualElement, ISelection
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


        #region Param
        float m_MaxFieldScale = 10;
        float m_FieldOffsetX = 6;
        float m_MarkerWidth = 50;
        float m_WheelLerpSpeed = 0.2f;
        int m_TimeTextFontSize = 14;
        #endregion

        #region Style
        static CustomStyleProperty<Color> s_FieldLineColor = new CustomStyleProperty<Color>("--field-line-color");
        Color m_FiedlLineColor;
        static CustomStyleProperty<Color> s_LocatorLineColor = new CustomStyleProperty<Color>("--locator-line-color");
        Color m_LocatorLineColor;
        static CustomStyleProperty<Font> s_MarkerTextFont = new CustomStyleProperty<Font>("--marker-text-font");
        Font m_MarkerTextFont;
        #endregion


        float m_FieldScale = 1;
        int m_MaxFrame = 60;
        bool m_DrawTimeText;
        bool m_ScrollViewPan;
        float m_ScrollViewPanDelta;

        public TimelineEditorWindow EditorWindow;
        public Timeline Timeline => EditorWindow.Timeline;

        public BiDictionary<Track, TimelineTrackView> TrackViewMap { get; private set; } = new BiDictionary<Track, TimelineTrackView>();
        public List<TimelineTrackView> TrackViews { get; set; } = new List<TimelineTrackView>();
        public Dictionary<int, float> FramePosMap { get; set; } = new Dictionary<int, float>();
        public DragManipulator LocatorDragManipulator { get; set; }


        public Action OnPopulatedCallback;
        public Action OnGeometryChangedCallback;

        public int CurrentMinFrame => GetCloestCeilFrame(ScrollViewContentOffset);
        public int CurrentMaxFrame => GetCloestFloorFrame(ScrollViewContentWidth + ScrollViewContentOffset);
        public float OneFrameWidth => m_MarkerWidth * m_FieldScale;
        public float ScrollViewContentWidth => TrackScrollView.contentContainer.worldBound.width;
        public float ScrollViewContentOffset => TrackScrollView.scrollOffset.x;
        public float ContentWidth => FieldContent.worldBound.width;

        public TimelineFieldView()
        {
            var visualTree = Resources.Load<VisualTreeAsset>("VisualTree/TimelineFieldView");
            visualTree.CloneTree(this);
            AddToClassList("timelineField");

            m_MarkerTextFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            TrackScrollView = this.Q<ScrollView>("track-scroll");
            TrackScrollView.RegisterCallback<PointerDownEvent>((e) =>
            {
                if (e.button == 2)
                {
                    m_ScrollViewPan = true;
                    m_ScrollViewPanDelta = e.localPosition.x;
                    TrackField.AddToClassList("pan");
                }
            });
            TrackScrollView.RegisterCallback<PointerMoveEvent>((e) =>
            {
                if (m_ScrollViewPan)
                {
                    TrackScrollView.scrollOffset = new Vector2(TrackScrollView.scrollOffset.x + m_ScrollViewPanDelta - e.localPosition.x, TrackScrollView.scrollOffset.y);
                    m_ScrollViewPanDelta = e.localPosition.x;
                }
            });
            TrackScrollView.RegisterCallback<PointerOutEvent>((e) =>
            {
                m_ScrollViewPan = false;
                TrackField.RemoveFromClassList("pan");
            });
            TrackScrollView.RegisterCallback<PointerUpEvent>((e) =>
            {
                m_ScrollViewPan = false;
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
            TrackField.generateVisualContent += OnTrackFieldGenerateVisualContent;

            MarkerField = this.Q("marker-field");
            MarkerField.AddToClassList("droppable");
            MarkerField.generateVisualContent += OnMarkerFieldGenerateVisualContent;
            MarkerField.RegisterCallback<PointerDownEvent>((e) =>
            {
                if (e.button == 0)
                {
                    SetTimeLocator(GetCloestFrame(e.localPosition.x));
                    LocatorDragManipulator.DragBeginForce(e);
                }
            });
            MarkerField.SetEnabled(false);

            LocatorDragManipulator = new DragManipulator(OnTimeLocatorStartMove, OnTimeLocatorStopMove, OnTimeLocatorMove);
            TimeLocator = this.Q("time-locater");
            TimeLocator.AddManipulator(LocatorDragManipulator);
            TimeLocator.generateVisualContent += OnTimeLocatorGenerateVisualContent;
            TimeLocator.SetEnabled(false);

            DrawFrameLineField = this.Q("draw-frame-line-field");
            DrawFrameLineField.generateVisualContent += OnDrawFrameLineFieldGenerateVisualContent;

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

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<WheelEvent>(OnlWheelEvent);
            RegisterCallback<KeyDownEvent>((e) =>
            {
                switch (e.keyCode)
                {
                    case KeyCode.Delete:
                        {
                            Timeline.ApplyModify(() =>
                            {
                                var selectableToRemove = Selections.ToList();
                                foreach (var selectable in selectableToRemove)
                                {
                                    if (selectable is TimelineTrackView trackView)
                                    {
                                        Timeline.RemoveTrack(trackView.Track);
                                    }
                                    if (selectable is TimelineClipView clipView)
                                    {
                                        Timeline.RemoveClip(clipView.Clip);
                                    }
                                }
                            }, "Remove");
                        }
                        break;
                    case KeyCode.F:
                        {
                            int startFrame = int.MaxValue;
                            int endFrame = int.MinValue;
                            foreach (var track in Timeline.Tracks)
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
            TrackField.Clear();
            m_Selections.Clear();
            m_Elements.Clear();
            TrackViewMap.Clear();
            TrackViews.Clear();
            PopulateInspector(null);
            UpdateBindState();

            if (Timeline)
            {
                Timeline.UpdateSerializedTimeline();

                int maxFrame = 0;
                foreach (var track in Timeline.Tracks)
                {
                    foreach (var clip in track.Clips)
                    {
                        if (clip.EndFrame >= maxFrame)
                            maxFrame = clip.EndFrame;
                    }
                }
                maxFrame++;

                m_MaxFrame = Mathf.Max(m_MaxFrame, maxFrame);
                m_FieldScale = Timeline.Scale;

                ResizeTimeField();
                DrawTimeField();

                foreach (var track in Timeline.Tracks)
                {
                    TimelineTrackView trackView = new TimelineTrackView();
                    trackView.SelectionContainer = this;
                    trackView.Init(track);

                    Elements.Add(trackView);
                    TrackField.Add(trackView);
                    TrackViewMap.Add(track, trackView);
                    TrackViews.Add(trackView);
                }
            }

            OnPopulatedCallback?.Invoke();
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
                            SerializedProperty serializedProperty = Timeline.SerializedTimeline.FindProperty("m_Tracks");
                            serializedProperty = serializedProperty.GetArrayElementAtIndex(Timeline.Tracks.IndexOf(track));

                            DrawProperties(serializedProperty, target);
                        }
                        break;
                    case Clip clip:
                        {
                            clip.OnInspectorRepaint = () => PopulateInspector(clip);

                            SerializedProperty serializedProperty = Timeline.SerializedTimeline.FindProperty("m_Tracks");
                            serializedProperty = serializedProperty.GetArrayElementAtIndex(Timeline.Tracks.IndexOf(clip.Track));
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
                        propertyField.Bind(Timeline.SerializedTimeline);

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
            #endregion
        }
        public void UpdateBindState()
        {
            if (EditorWindow == null) return;

            if (Timeline && Timeline.TimelinePlayer)
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
            PopulateInspector(Timeline);
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

        List<ISelectable> m_Elements = new List<ISelectable>();
        public List<ISelectable> Elements => m_Elements;

        List<ISelectable> m_Selections = new List<ISelectable>();
        public List<ISelectable> Selections => m_Selections;

        public void AddToSelection(ISelectable selectable)
        {
            m_Selections.Add(selectable);
            selectable.Select();

            if (selectable is TimelineTrackView trackView)
                PopulateInspector(trackView.Track);
            if (selectable is TimelineClipView clipView)
                PopulateInspector(clipView.Clip);
        }
        public void RemoveFromSelection(ISelectable selectable)
        {
            m_Selections.Remove(selectable);
            selectable.Unselect();
        }
        public void ClearSelection()
        {
            m_Selections.ForEach(i => i.Unselect());
            Selections.Clear();

            PopulateInspector(null);
        }
        #endregion

        #region TimeField
        public void ResizeTimeField()
        {
            FramePosMap.Clear();

            if (FieldContent.worldBound.width < ScrollViewContentWidth + ScrollViewContentOffset)
                FieldContent.style.width = ScrollViewContentWidth + ScrollViewContentOffset;

            int interval = Mathf.CeilToInt(Mathf.Max(FieldContent.worldBound.width, worldBound.width) / OneFrameWidth);

            if (m_MaxFrame < interval)
                m_MaxFrame = interval;
            for (int i = 0; i < m_MaxFrame; i++)
            {
                FramePosMap.Add(i, OneFrameWidth * i + m_FieldOffsetX);
            }

            float maxTextWidth = TextWidth(m_MaxFrame.ToString(), m_MarkerTextFont, m_TimeTextFontSize);
            m_DrawTimeText = OneFrameWidth > maxTextWidth * 1.5f;

            UpdateTimeLocator();

            foreach (var trackViewPair in TrackViewMap)
            {
                trackViewPair.Value.Refresh();
            }
        }
        public void DrawTimeField()
        {
            TrackField.MarkDirtyRepaint();
            MarkerField.MarkDirtyRepaint();
        }
        void OnMarkerFieldGenerateVisualContent(MeshGenerationContext mgc)
        {
            var paint2D = mgc.painter2D;
            paint2D.strokeColor = Color.white;
            paint2D.BeginPath();

            int showInterval = Mathf.CeilToInt(1 / m_FieldScale);
            int startFrame = CurrentMinFrame;
            int endFrame = CurrentMaxFrame;

            for (int i = startFrame; i <= endFrame; i++)
            {
                if (i % (showInterval * 5) == 0)
                {
                    paint2D.MoveTo(new Vector2(FramePosMap[i], 10));
                    paint2D.LineTo(new Vector2(FramePosMap[i], 25));

                    mgc.DrawText(i.ToString(), new Vector2(FramePosMap[i] + 5, 5), m_TimeTextFontSize, Color.white);
                }
                else if (i % showInterval == 0)
                {
                    paint2D.MoveTo(new Vector2(FramePosMap[i], 20));
                    paint2D.LineTo(new Vector2(FramePosMap[i], 25));

                    if (m_DrawTimeText)
                        mgc.DrawText(i.ToString(), new Vector2(FramePosMap[i] + 5, 5), m_TimeTextFontSize, Color.white);
                }
            }
            paint2D.Stroke();
        }
        void OnTrackFieldGenerateVisualContent(MeshGenerationContext mgc)
        {
            var paint2D = mgc.painter2D;
            paint2D.strokeColor = m_FiedlLineColor;
            paint2D.BeginPath();

            int showInterval = Mathf.CeilToInt(1 / m_FieldScale);
            int startFrame = CurrentMinFrame;
            int endFrame = CurrentMaxFrame;

            for (int i = startFrame; i <= endFrame; i++)
            {
                if (i % (showInterval * 5) == 0)
                {
                    paint2D.MoveTo(new Vector2(FramePosMap[i], 0));
                    paint2D.LineTo(new Vector2(FramePosMap[i], TrackScrollView.worldBound.height));
                }
            }
            paint2D.Stroke();
        }
        #endregion

        #region TimeLocator
        public void SetTimeLocator(int targetFrame)
        {
            Timeline.TimelinePlayer.IsPlaying = false;
            float deltaTime = targetFrame / 60f - Timeline.Time;
            Timeline.TimelinePlayer.Evaluate(deltaTime);
        }
        public void UpdateTimeLocator()
        {
            if (EditorWindow == null) return;

            if (Timeline != null && Timeline.Binding)
            {
                TimeLocator.style.left = Timeline.Time * TimelineUtility.FrameRate * OneFrameWidth + m_FieldOffsetX;
                TimeLocator.MarkDirtyRepaint();
                LocaterFrameLabel.text = Timeline.Frame.ToString();
            }
            else
            {
                TimeLocator.style.left = m_FieldOffsetX;
                TimeLocator.MarkDirtyRepaint();
                LocaterFrameLabel.text = string.Empty;
            }
        }
        void OnTimeLocatorStartMove(PointerDownEvent ev)
        {
            LocaterFrameLabel.style.display = DisplayStyle.Flex;
        }
        void OnTimeLocatorMove(Vector2 deltaPosition)
        {
            int targetFrame = GetCloestFrame(FramePosMap[Timeline.Frame] + deltaPosition.x);
            targetFrame = Mathf.Clamp(targetFrame, CurrentMinFrame, CurrentMaxFrame);

            SetTimeLocator(targetFrame);
        }
        void OnTimeLocatorStopMove()
        {
            LocaterFrameLabel.style.display = DisplayStyle.None;
        }
        void OnTimeLocatorGenerateVisualContent(MeshGenerationContext mgc)
        {
            var paint2D = mgc.painter2D;
            paint2D.strokeColor = Color.white;
            paint2D.BeginPath();
            paint2D.MoveTo(new Vector2(0, 25));
            paint2D.LineTo(new Vector2(0, TrackScrollView.worldBound.height));
            paint2D.Stroke();
        }
        #endregion

        #region DrawFrameLine
        int[] m_DrawFrameLines = new int[0];
        public void DrawFrameLine(params int[] frames)
        {
            m_DrawFrameLines = frames;
            DrawFrameLineField.MarkDirtyRepaint();
        }
        void OnDrawFrameLineFieldGenerateVisualContent(MeshGenerationContext mgc)
        {
            var paint2D = mgc.painter2D;
            paint2D.strokeColor = new Color(1f, 0.6f, 0f, 1f);
            paint2D.BeginPath();
            foreach (var drawFrame in m_DrawFrameLines)
            {
                int count = Mathf.CeilToInt(TrackScrollView.worldBound.height / 5);
                for (int i = 0; i < count; i += 2)
                {
                    paint2D.MoveTo(new Vector2(FramePosMap[drawFrame], i * 5));
                    paint2D.LineTo(new Vector2(FramePosMap[drawFrame], i * 5 + 5));
                }
                mgc.DrawText(drawFrame.ToString(), new Vector2(FramePosMap[drawFrame] + 5, 5), m_TimeTextFontSize, Color.white);
            }
            paint2D.Stroke();
        }
        #endregion

        #region AdjustClip
        public void ResizeClip(TimelineClipView clipView, int border, float deltaPosition)
        {
            if (border == 0)
            {
                int targetFrame = GetCloestFrame(FramePosMap[clipView.StartFrame] + deltaPosition);
                if (clipView.Clip.IsClipInable())
                    targetFrame = Mathf.Clamp(targetFrame, Mathf.Max(CurrentMinFrame, clipView.StartFrame - clipView.ClipInFrame), Mathf.Min(clipView.EndFrame - 1, CurrentMaxFrame));
                else
                    targetFrame = Mathf.Clamp(targetFrame, CurrentMinFrame, Mathf.Min(clipView.EndFrame - 1, CurrentMaxFrame));

                if (!clipView.Clip.IsMixable())
                {
                    Clip cloestLeftClip = GetCloestLeftClip(clipView.Clip);
                    if (cloestLeftClip != null)
                        targetFrame = Mathf.Max(targetFrame, cloestLeftClip.EndFrame);
                }
                else
                {
                    targetFrame = Mathf.Min(targetFrame, clipView.EndFrame - clipView.OtherEaseOutFrame);

                    Clip overlipClip = GetOverlapClip(clipView.Clip);
                    if (overlipClip != null && targetFrame <= overlipClip.StartFrame)
                    {
                        return;
                    }

                    Clip cloestLeftClip = GetCloestLeftClip(clipView.Clip);
                    if (cloestLeftClip != null)
                    {
                        targetFrame = Mathf.Max(targetFrame, cloestLeftClip.StartFrame + cloestLeftClip.OtherEaseInFrame);
                    }
                }

                if (targetFrame != clipView.StartFrame)
                {
                    Timeline.ApplyModify(() =>
                    {
                        clipView.Resize(targetFrame, clipView.EndFrame);
                    }, "Resize Clip");
                    Timeline.RebindTrack(clipView.TrackView.Track);
                }
            }
            else
            {
                int targetFrame = GetCloestFrame(FramePosMap[clipView.EndFrame] + deltaPosition);
                targetFrame = Mathf.Clamp(targetFrame, Mathf.Max(clipView.StartFrame + 1, CurrentMinFrame), CurrentMaxFrame);

                if (!clipView.Clip.IsMixable())
                {
                    Clip cloestRightClip = GetCloestRightClip(clipView.Clip);
                    if (cloestRightClip != null)
                        targetFrame = Mathf.Min(targetFrame, cloestRightClip.StartFrame);
                }
                else
                {
                    targetFrame = Mathf.Max(targetFrame, clipView.StartFrame + clipView.OtherEaseInFrame);

                    Clip cloestRightClip = GetCloestRightClip(clipView.Clip);
                    if (cloestRightClip != null)
                    {
                        targetFrame = Mathf.Min(targetFrame, cloestRightClip.EndFrame - cloestRightClip.OtherEaseOutFrame);
                    }
                }

                if (targetFrame != clipView.EndFrame)
                {
                    Timeline.ApplyModify(() =>
                    {
                        clipView.Resize(clipView.StartFrame, targetFrame);
                    }, "Resize Clip");
                    Timeline.RebindTrack(clipView.TrackView.Track);
                }
            }
        }
        public void AdjustSelfEase(TimelineClipView clipView, int border, float deltaPosition)
        {
            int deltaFrame = 0;
            if (border == 0)
            {
                int targetFrame = GetCloestFrame(FramePosMap[clipView.StartFrame + clipView.SelfEaseInFrame] + deltaPosition);
                targetFrame = Mathf.Clamp(targetFrame, Mathf.Max(clipView.StartFrame, CurrentMinFrame), Mathf.Min(clipView.EndFrame - clipView.EaseOutFrame, CurrentMaxFrame));
                deltaFrame = targetFrame - (clipView.StartFrame + clipView.SelfEaseInFrame);
            }
            else
            {
                int targetFrame = GetCloestFrame(FramePosMap[clipView.EndFrame - clipView.SelfEaseOutFrame] + deltaPosition);
                targetFrame = Mathf.Clamp(targetFrame, Mathf.Max(clipView.StartFrame + clipView.EaseInFrame, CurrentMinFrame), Mathf.Min(clipView.EndFrame, CurrentMaxFrame));
                deltaFrame = targetFrame - (clipView.EndFrame - clipView.SelfEaseOutFrame);
            }

            if (deltaFrame != 0)
            {
                Timeline.ApplyModify(() =>
                {
                    clipView.AdjustSelfEase(border, deltaFrame);
                }, "Resize Clip");
                Timeline.RebindTrack(clipView.TrackView.Track);
            }
        }

        TimelineClipView m_MoveLeader;
        int m_MoveStartFrame;
        public void StartMove(TimelineClipView moveLeader)
        {
            m_MoveLeader = moveLeader;
            m_MoveStartFrame = moveLeader.StartFrame;
        }
        public void MoveClips(float deltaPosition)
        {
            int startFrame = int.MaxValue;
            int endFrame = int.MinValue;
            List<TimelineClipView> moveClips = new List<TimelineClipView>();
            foreach (var selectable in Selections)
            {
                if (selectable is TimelineClipView clipView)
                {
                    moveClips.Add(clipView);
                    if (clipView.StartFrame < startFrame)
                        startFrame = clipView.StartFrame;
                    if (clipView.EndFrame > endFrame)
                        endFrame = clipView.EndFrame;
                }
            }

            int targetStartFrame = GetCloestFrame(FramePosMap[startFrame] + deltaPosition);
            targetStartFrame = Mathf.Clamp(targetStartFrame, CurrentMinFrame, CurrentMaxFrame);

            if (targetStartFrame != startFrame)
            {
                int deltaFrame = targetStartFrame - startFrame;
                if (deltaFrame + endFrame >= m_MaxFrame)
                {
                    for (int i = m_MaxFrame; i <= deltaFrame + endFrame; i++)
                    {
                        FramePosMap.Add(i, OneFrameWidth * i + m_FieldOffsetX);
                    }
                    m_MaxFrame = deltaFrame + endFrame + 1;
                }

                foreach (var moveClip in moveClips)
                {
                    moveClip.Move(deltaFrame);
                }
                foreach (var moveClip in moveClips)
                {
                    moveClip.Clip.Invalid = !GetMoveValid(moveClip);
                }
                Timeline.UpdateMix();

                DrawFrameLine(startFrame + deltaFrame, endFrame + deltaFrame);
            }

        }
        public void ApplyMove()
        {
            bool valid = true;
            List<TimelineClipView> moveClips = new List<TimelineClipView>();
            foreach (var selectable in Selections)
            {
                if (selectable is TimelineClipView clipView)
                {
                    moveClips.Add(clipView);
                    if (!GetMoveValid(clipView))
                        valid = false;
                }
            }

            int deltaFrame = m_MoveLeader.StartFrame - m_MoveStartFrame;
            if (deltaFrame != 0)
            {
                foreach (var clipView in moveClips)
                {
                    clipView.ResetMove(deltaFrame);
                }
                Timeline.UpdateMix();
                if (valid)
                {
                    Timeline.ApplyModify(() =>
                    {
                        foreach (var clipView in moveClips)
                        {
                            clipView.Move(deltaFrame);
                        }
                        Timeline.UpdateMix();
                    }, "Move Clip");
                }
                Timeline.RebindAll();
            }

            DrawFrameLine();
        }
        public bool GetMoveValid(TimelineClipView clipView)
        {
            if (!clipView.Clip.IsMixable())
            {
                foreach (var clip in clipView.Clip.Track.Clips)
                {
                    if (clip != clipView.Clip && clip.EndFrame > clipView.StartFrame && clip.StartFrame < clipView.EndFrame)
                        return false;
                }
            }
            else
            {
                foreach (var clip in clipView.Clip.Track.Clips)
                {
                    if (clip != clipView.Clip)
                    {
                        if (clip.StartFrame < clipView.StartFrame && clip.EndFrame > clipView.EndFrame)
                            return false;
                        else if (clip.StartFrame > clipView.StartFrame && clip.EndFrame < clipView.EndFrame)
                            return false;
                    }
                }

                for (float i = clipView.StartFrame; i <= clipView.EndFrame; i += 0.5f)
                {
                    int overlapCount = 0;
                    foreach (var clip in clipView.Clip.Track.Clips)
                    {
                        if (clip != clipView.Clip)
                        {
                            if (clip.Contains(i))
                                overlapCount++;
                        }
                    }
                    if (overlapCount > 1)
                        return false;
                }
            }
            return true;
        }
        #endregion

        #region Add Clip
        public void AddClip(Track track, int startFrame)
        {
            AdjustClip(Timeline.AddClip(track, startFrame));
        }
        public void AddClip(UnityEngine.Object referenceObject, Track track, int startFrame)
        {
            AdjustClip(Timeline.AddClip(referenceObject, track, startFrame));
        }
        void AdjustClip(Clip clip)
        {
            Clip cloestRightClip = GetCloestRightClip(clip.Track, clip.StartFrame);
            if (cloestRightClip != null && clip.StartFrame + clip.Length > cloestRightClip.StartFrame)
            {
                clip.EndFrame = cloestRightClip.StartFrame;
                GetClipView(clip).Refresh();
            }
        }
        #endregion

        #region Callback
        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (customStyle.TryGetValue(s_FieldLineColor, out var lineColor))
                m_FiedlLineColor = lineColor;
            if (customStyle.TryGetValue(s_MarkerTextFont, out var textFont))
                m_MarkerTextFont = textFont;
        }
        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ResizeTimeField();
            DrawTimeField();
            OnGeometryChangedCallback?.Invoke();
        }
        void OnTrackFieldGeometryChanged(GeometryChangedEvent evt)
        {
            float delta = evt.newRect.width - evt.oldRect.width;
            if (delta > 0)
            {
                int count = Mathf.CeilToInt(delta / OneFrameWidth);
                for (int i = 0; i < count; i++)
                {
                    FramePosMap.Add(m_MaxFrame, OneFrameWidth * m_MaxFrame + m_FieldOffsetX);
                    m_MaxFrame++;
                }
            }
        }
        void OnlWheelEvent(WheelEvent wheelEvent)
        {
            m_FieldScale *= (1 - wheelEvent.delta.y / 100);
            m_FieldScale = Mathf.Min(m_MaxFieldScale, m_FieldScale);
            Timeline.Scale = m_FieldScale;

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
                targetOffset = Mathf.Lerp(ScrollViewContentOffset, targetOffset, m_WheelLerpSpeed);
                TrackScrollView.scrollOffset = new Vector2(targetOffset, TrackScrollView.scrollOffset.y);

                ResizeTimeField();
                ForceScrollViewUpdate(TrackScrollView);
            }

            OnGeometryChangedCallback?.Invoke();
        }
        #endregion

        #region Helper
        public int GetCloestFrame(float position)
        {
            int frame = 0;
            foreach (var framePosPair in FramePosMap)
            {
                if (Mathf.Abs(framePosPair.Value - position) < Mathf.Abs(FramePosMap[frame] - position))
                    frame = framePosPair.Key;
            }
            return frame;
        }
        public int GetCloestFrameWithin(float position, float minPosition, float maxPosition)
        {
            int frame = 0;
            foreach (var framePosPair in FramePosMap)
            {
                if (Mathf.Abs(framePosPair.Value - position) < Mathf.Abs(FramePosMap[frame] - position) && minPosition <= framePosPair.Value && framePosPair.Value <= maxPosition)
                    frame = framePosPair.Key;
            }
            return frame;
        }
        public int GetCloestFloorFrame(float position)
        {
            int frame = 0;
            foreach (var framePosPair in FramePosMap)
            {
                if (Mathf.Abs(framePosPair.Value - position) < Mathf.Abs(FramePosMap[frame] - position) && framePosPair.Value <= position)
                    frame = framePosPair.Key;
            }
            return frame;
        }
        public int GetCloestCeilFrame(float position)
        {
            int frame = 0;
            foreach (var framePosPair in FramePosMap)
            {
                if (Mathf.Abs(framePosPair.Value - position) < Mathf.Abs(FramePosMap[frame] - position) && position <= framePosPair.Value)
                    frame = framePosPair.Key;
            }
            return frame;
        }
        public int RoundToNearestInt(int value, int targetInt)
        {
            int remainder = value % targetInt;
            if (remainder >= (float)targetInt / 2)
            {
                return value + (targetInt - remainder);
            }
            else
            {
                return value - remainder;
            }
        }
        public int GetRightEdgeFrame(Track track)
        {
            int frame = 0;
            foreach (var clip in track.Clips)
            {
                if (clip.EndFrame > frame)
                    frame = clip.EndFrame;
            }
            return frame;
        }
        public TimelineClipView GetClipView(Clip clip)
        {
            return TrackViewMap[clip.Track].ClipViewMap[clip];
        }
        public TimelineClipView[] GetSameTrackClipViews(TimelineClipView clipView)
        {
            return clipView.TrackView.ClipViews.ToArray();
        }
        public Clip GetCloestLeftClip(Clip targetClip)
        {
            int targetFrame = int.MinValue;
            Clip cloestClip = null;
            foreach (var clip in targetClip.Track.Clips)
            {
                if (clip != targetClip && clip.StartFrame < targetClip.StartFrame && clip.StartFrame > targetFrame)
                {
                    targetFrame = clip.StartFrame;
                    cloestClip = clip;
                }
            }
            return cloestClip;
        }
        public Clip GetCloestRightClip(Clip targetClip)
        {
            int targetFrame = int.MaxValue;
            Clip cloestClip = null;
            foreach (var clip in targetClip.Track.Clips)
            {
                if (clip != targetClip && clip.StartFrame > targetClip.StartFrame && clip.StartFrame < targetFrame)
                {
                    targetFrame = clip.StartFrame;
                    cloestClip = clip;
                }
            }
            return cloestClip;
        }
        public Clip GetCloestRightClip(Track track, int startFrame)
        {
            List<Clip> rightClips = new List<Clip>();
            foreach (var clip in track.Clips)
            {
                if (clip.StartFrame > startFrame)
                    rightClips.Add(clip);
            }
            rightClips = rightClips.OrderBy(x => x.StartFrame).ToList();
            return rightClips.Count > 0 ? rightClips[0] : null;
        }
        public Clip GetOverlapClip(Clip targetClip)
        {
            foreach (var clip in targetClip.Track.Clips)
            {
                if (clip != targetClip && clip.StartFrame == targetClip.StartFrame)
                {
                    return clip;
                }
            }
            return null;
        }
        public int TextWidth(string s, Font font, int fontsize, FontStyle fontstyle = FontStyle.Normal)
        {
            if (string.IsNullOrEmpty(s))
                return 0;

            int w = 0;
            font.RequestCharactersInTexture(s, fontsize, fontstyle);

            foreach (char c in s)
            {
                font.GetCharacterInfo(c, out CharacterInfo cInfo, fontsize);
                w += cInfo.advance;
            }

            return w;
        }
        #endregion
    }
}