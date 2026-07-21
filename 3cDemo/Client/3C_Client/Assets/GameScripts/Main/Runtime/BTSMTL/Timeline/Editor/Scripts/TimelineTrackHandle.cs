using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using BTSMTL.Editor;

namespace BTSMTL.Timeline.Editor
{
    public class TimelineTrackHandle : VisualElement, ISelectable
    {
        public new class UxmlFactory : UxmlFactory<TimelineTrackHandle, UxmlTraits> { }
        public TextField NameField { get; private set; }
        public VisualElement Icon { get; private set; }


        public TimelineTrackView TrackView { get; private set; }
        public TimelineEditorView EditorWindow => TrackView.EditorWindow;
        public TimelineFieldView FieldView => TrackView.FieldView;
        public Track Track => TrackView.Track;
        public TimelineData TimelineData => Track.Timeline;


        DropdownMenuHandler MenuHandler;
        bool m_RuntimeReadOnly;
        Label m_MarkerSyncSummary;
        VisualElement m_AnimationCurvesHeader;
        VisualElement m_CurveChannelLabels;
        Label m_AnimationCurvesFold;
        readonly System.Collections.Generic.List<TimelineCurveChannelDescriptor> m_CurveChannels =
            new System.Collections.Generic.List<TimelineCurveChannelDescriptor>();
        
        public TimelineTrackHandle()
        {
            var visualTree = Resources.Load<VisualTreeAsset>("VisualTree/TimelineTrackHandle");
            visualTree.CloneTree(this);
            AddToClassList("timelineTrackHandle");
            pickingMode = PickingMode.Ignore;
        }
        public TimelineTrackHandle(TimelineTrackView trackView) : this()
        {
            TrackView = trackView;
            TrackView.OnSelected = Select;
            TrackView.OnUnselected = Unselect;
            if (TrackView.IsSelected())
                Select();

            style.borderLeftColor = Track.Color();

            NameField = this.Q<TextField>();
            SerializedProperty serializedProperty = TimelineData.SerializedData.FindPropertyRelative("m_Tracks");
            serializedProperty = serializedProperty.GetArrayElementAtIndex(TimelineData.Tracks.IndexOf(Track));
            NameField.BindProperty(serializedProperty.FindPropertyRelative("Name"));

            Icon = this.Q("icon");
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(IconGuidAttribute.Guid(Track.GetType())));
            if (texture)
                Icon.style.backgroundImage = texture;

            style.height = TimelineTrackLayout.ContentHeight(Track);
            m_MarkerSyncSummary = this.Q<Label>("marker-sync-summary");
            m_MarkerSyncSummary.pickingMode = PickingMode.Ignore;
            m_AnimationCurvesHeader = this.Q("animation-curves-header");
            m_AnimationCurvesHeader.pickingMode = PickingMode.Position;
            m_AnimationCurvesFold = this.Q<Label>("animation-curves-fold");
            m_AnimationCurvesFold.pickingMode = PickingMode.Ignore;
            m_CurveChannelLabels = this.Q("curve-channel-labels");
            m_CurveChannelLabels.pickingMode = PickingMode.Ignore;
            if (Track is AnimationTrack animationTrack)
            {
                m_MarkerSyncSummary.style.display = DisplayStyle.Flex;
                m_MarkerSyncSummary.text = MarkerHeaderText(animationTrack);
                m_MarkerSyncSummary.style.top = TimelineTrackLayout.MarkerHeaderTop;
                m_MarkerSyncSummary.style.height = TimelineTrackLayout.MarkerHeaderHeight;
                m_MarkerSyncSummary.pickingMode = PickingMode.Position;
                TrackView.MarkerSyncSummaryChanged += RefreshMarkerSyncSummary;
                m_MarkerSyncSummary.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button != 0)
                        return;
                    TrackView.ToggleMarkerLane();
                    evt.StopImmediatePropagation();
                });
            }
            TimelineCurveChannelCatalog.CollectForTrack(Track, m_CurveChannels);
            if (m_CurveChannels.Count > 0)
            {
                m_AnimationCurvesHeader.style.display = DisplayStyle.Flex;
                m_AnimationCurvesHeader.style.top = TimelineTrackLayout.CurveHeaderTop(Track);
                bool expanded = TimelineTrackLayout.CurvesExpanded(Track);
                m_AnimationCurvesFold.text = expanded ? "v" : ">";
                m_AnimationCurvesHeader.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 0)
                        TrackView.ToggleCurveLanes();
                    else if (evt.button == 1)
                        ShowCurveChannelMenu();
                    else
                        return;
                    evt.StopImmediatePropagation();
                });
                m_CurveChannelLabels.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
                m_CurveChannelLabels.style.top = TimelineTrackLayout.CurveHeaderTop(Track) + TimelineTrackLayout.CurveHeaderHeight;
                PopulateCurveChannelLabels();
            }
            FieldView.OnGeometryChangedCallback += OnGeometryChanged;
            this.RegisterCallbackOnce<GeometryChangedEvent>((e) => OnGeometryChanged());
            RegisterCallback<DetachFromPanelEvent>((e) =>
            {
                FieldView.OnGeometryChangedCallback -= OnGeometryChanged;
                TrackView.MarkerSyncSummaryChanged -= RefreshMarkerSyncSummary;
            });
            //RegisterCallback<PointerDownEvent>(OnPointerDown);

            MenuHandler = new DropdownMenuHandler(MenuBuilder);
            DragManipulator = new DragManipulator(
            (e) =>
            {
                Draging = true;
                OriginalIndex = TimelineData.Tracks.IndexOf(Track);
                e.StopImmediatePropagation();
            },
            () =>
            {
                Draging = false;
                Tweening = false;
                EditorApplication.update -= TweenTrackHandles;

                int currentIndex = TimelineData.Tracks.IndexOf(Track);
                TimelineData.Tracks.Remove(Track);
                TimelineData.Tracks.Insert(OriginalIndex, Track);

                if(OriginalIndex != currentIndex)
                {
                    TimelineData.ApplyModify(() =>
                    {
                        TimelineData.Tracks.Remove(Track);
                        TimelineData.Tracks.Insert(currentIndex, Track);
                        TimelineData.Resort();
                    }, "Resort");
                }
            },
            (e) =>
            {
                float targetY = transform.position.y + e.y;
                float maximum = Mathf.Max(
                    TimelineTrackLayout.VerticalMargin,
                    TimelineTrackLayout.TotalHeight(TimelineData.Tracks) -
                    TimelineTrackLayout.Stride(Track) +
                    TimelineTrackLayout.VerticalMargin);
                targetY = Mathf.Clamp(targetY, TimelineTrackLayout.VerticalMargin, maximum);
                transform.position = new Vector3(0, targetY, 0);
                TrackView.transform.position = new Vector3(0, targetY - TimelineTrackLayout.VerticalMargin, 0);

                int index = TimelineData.Tracks.IndexOf(Track);
                int targetIndex = TimelineTrackLayout.IndexAt(
                    TimelineData.Tracks,
                    targetY + TimelineTrackLayout.ContentHeight(Track) * 0.5f);
                if(index != targetIndex)
                {
                    TimelineData.Tracks.Remove(Track);
                    TimelineData.Tracks.Insert(targetIndex, Track);
                }
                if (!Tweening)
                {
                    EditorApplication.update += TweenTrackHandles;
                }
            });
            this.AddManipulator(DragManipulator);
        }

        void RefreshMarkerSyncSummary(string summary)
        {
            if (m_MarkerSyncSummary != null && Track is AnimationTrack animationTrack)
                m_MarkerSyncSummary.text = MarkerHeaderText(animationTrack);
        }

        string MarkerHeaderText(AnimationTrack track) =>
            $"{(TimelineTrackLayout.MarkersExpanded(track) ? "v" : ">") } SYNC MARKERS  {TimelineTrackView.MarkerSyncSummary(track)}";

        void PopulateCurveChannelLabels()
        {
            m_CurveChannelLabels.Clear();
            for (int i = 0; i < m_CurveChannels.Count; i++)
            {
                TimelineCurveChannelDescriptor descriptor = m_CurveChannels[i];
                if (!TimelineCurveEditorSession.IsChannelVisible(Track, descriptor.ChannelId))
                    continue;
                var row = new VisualElement();
                row.AddToClassList("curveChannelRow");
                row.pickingMode = PickingMode.Ignore;
                var swatch = new VisualElement();
                swatch.AddToClassList("curveChannelSwatch");
                swatch.style.backgroundColor = descriptor.Color;
                swatch.pickingMode = PickingMode.Ignore;
                var label = new Label(descriptor.DisplayName);
                label.AddToClassList("curveChannelLabel");
                label.pickingMode = PickingMode.Ignore;
                var range = new Label(descriptor.ValueDomain.Summary);
                range.AddToClassList("curveChannelRange");
                range.pickingMode = PickingMode.Ignore;
                row.Add(swatch);
                row.Add(label);
                row.Add(range);
                m_CurveChannelLabels.Add(row);
            }
        }

        void ShowCurveChannelMenu()
        {
            var menu = new GenericMenu();
            for (int i = 0; i < m_CurveChannels.Count; i++)
            {
                TimelineCurveChannelDescriptor descriptor = m_CurveChannels[i];
                bool visible = TimelineCurveEditorSession.IsChannelVisible(Track, descriptor.ChannelId);
                menu.AddItem(new GUIContent(descriptor.DisplayName), visible, () => TrackView.ToggleCurveChannel(descriptor.ChannelId));
            }
            menu.ShowAsContext();
        }

        void OnGeometryChanged()
        {
            if (parent == null || TrackView == null)
                return;

            float targetY = parent.WorldToLocal(
                new Vector2(parent.worldBound.xMin, TrackView.worldBound.yMin)).y;
            float currentY = parent.WorldToLocal(
                new Vector2(parent.worldBound.xMin, worldBound.yMin)).y;
            Vector3 position = transform.position;
            position.y += targetY - currentY;
            transform.position = position;
        }
        void MenuBuilder(DropdownMenu menu)
        {
            menu.AppendAction("Add Clip", (e) =>
            {
                TimelineData.ApplyModify(() =>
                {
                    FieldView.AddClip(Track, FieldView.Geometry.GetRightEdgeFrame(Track));
                }, "Add Clip");
            });
            menu.AppendAction("Remove Track", (e) =>
            {
                TimelineData.ApplyModify(() =>
                {
                    TimelineData.RemoveTrack(Track);
                }, "Remove Track");
            });
            menu.AppendAction("Mute Track", (e) =>
            {
                TimelineData.ApplyModify(() =>
                {
                    Track.PersistentMuted = !Track.PersistentMuted;
                }, "Mute Track");
                EditorWindow.RefreshPreview();
            },
            (e) =>
            {
                return Track.PersistentMuted ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
            });
            menu.AppendAction("Open Script", (e) =>
            {
                Track.OpenTrackScript();
            });
        }
        public void OnPointerDown(PointerDownEvent e)
        {
            if (e.button == 0 && IsSelectable())
            {
                if (!IsSelected())
                {
                    if (e.actionKey)
                    {
                        FieldView.AddToSelection(TrackView);
                    }
                    else
                    {
                        FieldView.ClearSelection();
                        FieldView.AddToSelection(TrackView);
                    }
                }
                else
                {
                    if (e.actionKey)
                    {
                        FieldView.RemoveFromSelection(TrackView);
                    }
                }
                if (!m_RuntimeReadOnly)
                    DragManipulator.DragBeginForce(e);
                e.StopImmediatePropagation();
            }
            else if (e.button == 1)
            {
                if (m_RuntimeReadOnly)
                    return;
                FieldView.ClearSelection();
                FieldView.AddToSelection(TrackView);
                MenuHandler.ShowMenu(e);
                e.StopImmediatePropagation();
            }
        }

        public void SetRuntimeReadOnly(bool readOnly)
        {
            m_RuntimeReadOnly = readOnly;
            DragManipulator.enabled = !readOnly;
            NameField.SetEnabled(!readOnly);
        }

        #region Drag
        bool Draging;
        int OriginalIndex;
        DragManipulator DragManipulator;
        
        static bool Tweening;
        void TweenTrackHandles()
        {
            Tweening = false;
            EditorApplication.update -= TweenTrackHandles;
            var trackHandles = parent.Query<TimelineTrackHandle>().ToList();
            foreach (var trackHandle in trackHandles)
            {
                SerializedProperty nameProperty = TimelineData.SerializedData.FindPropertyRelative("m_Tracks")
                    .GetArrayElementAtIndex(TimelineData.Tracks.IndexOf(trackHandle.Track))
                    .FindPropertyRelative("Name");
                trackHandle.NameField.Unbind();
                trackHandle.NameField.BindProperty(nameProperty);

                if (!trackHandle.Draging)
                {
                    float targetY = TimelineTrackLayout.Top(
                        TimelineData.Tracks,
                        TimelineData.Tracks.IndexOf(trackHandle.Track)) + TimelineTrackLayout.VerticalMargin;
                    float currentY = trackHandle.transform.position.y;
                    if(Mathf.Abs(currentY - targetY) > 1f)
                    {
                        Tweening = true;
                        targetY = Mathf.Lerp(currentY, targetY, 0.05f);
                    }
                    trackHandle.transform.position = new Vector3(0, targetY, 0);
                    trackHandle.TrackView.transform.position = new Vector3(
                        0,
                        targetY - TimelineTrackLayout.VerticalMargin,
                        0);
                }
            }
            if (Tweening)
                EditorApplication.update += TweenTrackHandles;
        }
        #endregion

        #region Selectable
        public bool Selected { get; private set; }
        public ISelection SelectionContainer { get; set; }
        public override bool Overlaps(Rect rectangle)
        {
            return false;
        }
        public bool IsSelectable()
        {
            return true;
        }
        public bool IsSelected()
        {
            return TrackView.IsSelected();
        }
        public void Select()
        {
            AddToClassList("selected");
            BringToFront();
        }
        public void Unselect()
        {
            RemoveFromClassList("selected");
        }
        #endregion
    }
}
