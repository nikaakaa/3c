using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using BTSMTL.Editor;
using System;
using System.Linq;

namespace BTSMTL.Timeline.Editor
{
    public class TimelineTrackView : VisualElement, ISelectable
    {
        public new class UxmlFactory : UxmlFactory<TimelineTrackView, UxmlTraits> { }

        bool m_Selected;
        public ISelection SelectionContainer { get; set; }

        public TimelineFieldView FieldView => SelectionContainer as TimelineFieldView;
        public TimelineEditorView EditorWindow => FieldView.EditorWindow;
        public TimelineData TimelineData => EditorWindow.Timeline;

        public Track Track { get; private set; }
        public BiDictionary<Clip, TimelineClipView> ClipViewMap { get; private set; }
        public List<TimelineClipView> ClipViews { get; set; }
        public Action OnSelected;
        public Action OnUnselected;
        internal event Action<string> MarkerSyncSummaryChanged;
        
        DropdownMenuHandler m_MenuHandler;
        Vector2 m_LocalMousePosition;
        bool m_RuntimeReadOnly;
        VisualElement m_MarkerHeader;
        VisualElement m_MarkerLane;
        Label m_MarkerLaneSummary;
        VisualElement m_CurveHeader;
        readonly List<TimelineCurveChannelDescriptor> m_CurveChannels = new List<TimelineCurveChannelDescriptor>();
        internal bool RuntimeReadOnly => m_RuntimeReadOnly;

        public TimelineTrackView()
        {
            var visualTree = Resources.Load<VisualTreeAsset>("VisualTree/TimelineTrackView");
            visualTree.CloneTree(this);
            AddToClassList("timelineTrack");


            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerOutEvent>(OnPointerOut);

            m_MenuHandler = new DropdownMenuHandler(MenuBuilder);
        }
        public void Init(Track track)
        {
            Track = track;
            Track.OnUpdateMix = Refresh;
            Track.OnMutedStateChanged = OnMutedStateChanged;
            ClipViewMap = new BiDictionary<Clip, TimelineClipView>();
            ClipViews = new List<TimelineClipView>();
            PopulateMarkerLane();
            PopulateCurveLanes();
            foreach (var clip in track.Clips)
            {
                TimelineClipView clipView = new TimelineClipView();
                clipView.SelectionContainer = FieldView;
                clipView.Init(clip, this);

                Add(clipView);
                FieldView.RegisterSelectable(clipView);
                ClipViewMap.Add(clip, clipView);
                ClipViews.Add(clipView);
            }
            DragAndDropManipulator dragAndDropManipulator = new DragAndDropManipulator(this);
            dragAndDropManipulator.DragValid = () => !m_RuntimeReadOnly && Track.DragValid();
            dragAndDropManipulator.DragPerform += (e1, e2) =>
            {
                if (m_RuntimeReadOnly)
                    return;
                int startFrame = FieldView.Geometry.PositionToFloorFrame(e2.x);
                if (Track.Clips.Find(i => i.StartFrame == startFrame) == null)
                {
                    TimelineData.ApplyModify(() =>
                    {
                        FieldView.AddClip(e1, Track, startFrame);
                    }, "Add Clip");
                }
            };
            this.AddManipulator(dragAndDropManipulator);

            FieldView.Rendering.ApplyTrackAuthoring(
                this,
                new TimelineTrackRenderInput(
                    TimelineTrackLayout.Top(TimelineData.Tracks, TimelineData.Tracks.IndexOf(track)),
                    TimelineTrackLayout.ContentHeight(track)));


            OnMutedStateChanged();
        }

        public void Refresh()
        {
            foreach (var clipViewPair in ClipViewMap)
            {
                clipViewPair.Value.Refresh();
            }
            RefreshMarkerLane();
        }

        #region Selectable
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
            return m_Selected;
        }
        public void Select()
        {
            m_Selected = true;
            AddToClassList("selected");
            BringToFront();
            OnSelected?.Invoke();
        }
        public void Unselect()
        {
            m_Selected = false;
            RemoveFromClassList("selected");

            OnUnselected?.Invoke();
        }
        #endregion

        void MenuBuilder(DropdownMenu menu)
        {
            if (m_RuntimeReadOnly)
                return;
            int startFrame = FieldView.Geometry.PositionToFloorFrame(m_LocalMousePosition.x);
            if (Track.Clips.Find(i => i.StartFrame == startFrame) == null)
            {
                menu.AppendAction("Add Clip", (e) =>
                {
                    TimelineData.ApplyModify(() =>
                    {
                        FieldView.AddClip(Track, startFrame);
                    }, "Add Clip");
                });
            }
            menu.AppendAction("Remove Track", (e) =>
            {
                TimelineData.ApplyModify(() =>
                {
                    TimelineData.RemoveTrack(Track);
                }, "Remove Track");
            });
            menu.AppendAction("Open Script", (e) =>
            {
                Track.OpenTrackScript();
            });
        }
        void OnPointerDown(PointerDownEvent e)
        {
            foreach (var clipViewPair in ClipViewMap)
            {
                if (clipViewPair.Value.InMiddle(e.position))
                {
                    clipViewPair.Value.OnPointerDown(e);
                    e.StopImmediatePropagation();
                    return;
                }
            }
            if (e.button == 0 && IsSelectable())
            {
                if (!IsSelected())
                {
                    if (e.actionKey)
                    {
                        SelectionContainer.AddToSelection(this);
                    }
                    else
                    {
                        SelectionContainer.ClearSelection();
                        SelectionContainer.AddToSelection(this);
                    }
                }
                else
                {
                    if (e.actionKey)
                    {
                        SelectionContainer.RemoveFromSelection(this);
                    }
                }
                e.StopImmediatePropagation();
            }
            else if (e.button == 1)
            {
                if (m_RuntimeReadOnly)
                    return;
                m_LocalMousePosition = e.localPosition;
                m_MenuHandler.ShowMenu(e);
                SelectionContainer.ClearSelection();
                SelectionContainer.AddToSelection(this);
                e.StopImmediatePropagation();
            }
        }

        public void SetRuntimeReadOnly(bool readOnly)
        {
            m_RuntimeReadOnly = readOnly;
            for (int i = 0; i < ClipViews.Count; i++)
                ClipViews[i].SetRuntimeReadOnly(readOnly);
        }

        void PopulateMarkerLane()
        {
            if (Track is not AnimationTrack animationTrack)
                return;
            m_MarkerHeader = new VisualElement { name = "animation-marker-sync-header" };
            m_MarkerHeader.AddToClassList("animationMarkerSyncHeader");
            m_MarkerHeader.style.top = TimelineTrackLayout.MarkerHeaderTop;
            m_MarkerHeader.style.height = TimelineTrackLayout.MarkerHeaderHeight;
            m_MarkerHeader.pickingMode = PickingMode.Position;
            m_MarkerLaneSummary = new Label();
            m_MarkerLaneSummary.AddToClassList("animationMarkerSyncLaneSummary");
            m_MarkerLaneSummary.pickingMode = PickingMode.Ignore;
            m_MarkerHeader.Add(m_MarkerLaneSummary);
            m_MarkerHeader.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;
                SelectTrackForMarkerAuthoring();
                evt.StopImmediatePropagation();
            });
            Add(m_MarkerHeader);
            if (TimelineTrackLayout.MarkersExpanded(Track))
            {
                m_MarkerLane = new VisualElement { name = "animation-marker-sync-lane" };
                m_MarkerLane.AddToClassList("animationMarkerSyncLane");
                m_MarkerLane.style.top = TimelineTrackLayout.MarkerLaneTop;
                m_MarkerLane.style.height = TimelineTrackLayout.MarkerLaneHeight;
                m_MarkerLane.pickingMode = PickingMode.Position;
                m_MarkerLane.generateVisualContent += DrawMarkerCoverage;
                m_MarkerLane.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 0)
                    {
                        SelectTrackForMarkerAuthoring();
                        evt.StopImmediatePropagation();
                    }
                });
                Add(m_MarkerLane);
            }
            RefreshMarkerLane();
        }

        void RefreshMarkerLane()
        {
            if (m_MarkerLaneSummary == null || Track is not AnimationTrack animationTrack)
                return;
            string summary = MarkerSyncSummary(animationTrack);
            m_MarkerLaneSummary.text = $"{(TimelineTrackLayout.MarkersExpanded(Track) ? "v" : ">") }  SYNC MARKERS   {summary}";
            m_MarkerHeader.EnableInClassList(
                "animationMarkerSyncLane--enabled",
                animationTrack.SyncMode == AnimationSyncMode.MarkerGroup);
            MarkerSyncSummaryChanged?.Invoke(summary);
            m_MarkerLane?.MarkDirtyRepaint();
        }

        void PopulateCurveLanes()
        {
            TimelineCurveChannelCatalog.CollectForTrack(Track, m_CurveChannels);
            if (m_CurveChannels.Count == 0)
                return;
            m_CurveHeader = new VisualElement { name = "animation-curves-header" };
            m_CurveHeader.AddToClassList("animationCurvesHeader");
            m_CurveHeader.style.top = TimelineTrackLayout.CurveHeaderTop(Track);
            m_CurveHeader.pickingMode = PickingMode.Position;
            var fold = new Label("·");
            fold.AddToClassList("animationCurvesHeaderFold");
            fold.pickingMode = PickingMode.Ignore;
            var headerLabel = new Label("TYPED CURVES · EDIT IN SOURCE TIME INSPECTOR");
            headerLabel.AddToClassList("animationCurvesHeaderLabel");
            headerLabel.pickingMode = PickingMode.Ignore;
            var rangeLabel = new Label($"{TimelineTrackLayout.VisibleCurveChannelCount(Track)}/{m_CurveChannels.Count}");
            rangeLabel.AddToClassList("animationCurvesHeaderRange");
            rangeLabel.pickingMode = PickingMode.Ignore;
            m_CurveHeader.Add(fold);
            m_CurveHeader.Add(headerLabel);
            m_CurveHeader.Add(rangeLabel);
            m_CurveHeader.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;
                SelectTrackForMarkerAuthoring();
                evt.StopImmediatePropagation();
            });
            Add(m_CurveHeader);
        }

        internal void ToggleCurveLanes()
        {
            TimelineTrackLayout.ToggleCurves(Track);
            FieldView.schedule.Execute(FieldView.PopulateView);
        }

        internal void ToggleCurveChannel(TimelineCurveChannelId channelId)
        {
            TimelineCurveEditorSession.ToggleChannel(Track, channelId);
            FieldView.schedule.Execute(FieldView.PopulateView);
        }

        internal void ToggleMarkerLane()
        {
            TimelineTrackLayout.ToggleMarkers(Track);
            FieldView.schedule.Execute(FieldView.PopulateView);
        }

        internal static string MarkerSyncSummary(AnimationTrack track)
        {
            if (track == null || track.SyncMode == AnimationSyncMode.Unspecified)
                return "Unspecified · 0 markers";
            if (track.SyncMode == AnimationSyncMode.None)
                return "None · 0 markers";
            return $"{track.SyncGroupId} · {track.SequenceTopology} · {track.SyncRole} · {track.SyncMarkers.Count} markers";
        }

        void SelectTrackForMarkerAuthoring()
        {
            if (IsSelected())
            {
                FieldView.PopulateInspector(Track);
                return;
            }
            FieldView.ClearSelection();
            FieldView.AddToSelection(this);
        }

        void DrawMarkerCoverage(MeshGenerationContext context)
        {
            if (Track is not AnimationTrack track || track.SyncMode != AnimationSyncMode.MarkerGroup || track.SyncMarkers.Count == 0)
                return;
            Painter2D painter = context.painter2D;
            string activeFrom = string.Empty;
            string activeTo = string.Empty;
            if (EditorWindow.PreviewSession.TryGetMarkerSyncPreviewState(out TimelineAnimationMarkerSyncPreviewState state) &&
                string.Equals(state.TargetProducerId, $"{track.Timeline.AuthoringId}/{track.AuthoringId}", StringComparison.Ordinal))
            {
                activeFrom = state.PreviousMarkerId;
                activeTo = state.NextMarkerId;
            }
            for (int i = 1; i < track.SyncMarkers.Count; i++)
                DrawMarkerSegment(painter, track.SyncMarkers[i - 1], track.SyncMarkers[i], activeFrom, activeTo);
            painter.strokeColor = new Color(0.55f, 0.75f, 0.8f, 0.3f);
            painter.lineWidth = 1f;
            if (track.SequenceTopology == AnimationMarkerSequenceTopology.Cyclic && track.SyncMarkers.Count > 1)
            {
                AnimationSyncMarker last = track.SyncMarkers[track.SyncMarkers.Count - 1];
                AnimationSyncMarker first = track.SyncMarkers[0];
                bool active = string.Equals(last.MarkerId, activeFrom, StringComparison.Ordinal) &&
                              string.Equals(first.MarkerId, activeTo, StringComparison.Ordinal);
                painter.strokeColor = active ? new Color(1f, 0.68f, 0.1f, 0.95f) : new Color(0.35f, 0.85f, 0.9f, 0.45f);
                painter.lineWidth = active ? 2.5f : 1f;
                float y = TimelineTrackLayout.MarkerLaneHeight - 5f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(FieldView.Geometry.FrameToPosition(last.Frame), y));
                painter.LineTo(new Vector2(contentRect.width, y));
                painter.MoveTo(new Vector2(0f, y));
                painter.LineTo(new Vector2(FieldView.Geometry.FrameToPosition(first.Frame), y));
                painter.Stroke();
            }
            else
            {
                float y = TimelineTrackLayout.MarkerLaneHeight - 5f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(FieldView.Geometry.FrameToPosition(0), y));
                painter.LineTo(new Vector2(FieldView.Geometry.FrameToPosition(track.SyncMarkers[0].Frame), y));
                painter.MoveTo(new Vector2(FieldView.Geometry.FrameToPosition(track.SyncMarkers[track.SyncMarkers.Count - 1].Frame), y));
                painter.LineTo(new Vector2(FieldView.Geometry.FrameToPosition(track.Timeline.MaxFrame), y));
                painter.Stroke();
            }
        }

        void DrawMarkerSegment(
            Painter2D painter,
            AnimationSyncMarker from,
            AnimationSyncMarker to,
            string activeFrom,
            string activeTo)
        {
            bool active = string.Equals(from.MarkerId, activeFrom, StringComparison.Ordinal) &&
                          string.Equals(to.MarkerId, activeTo, StringComparison.Ordinal);
            painter.strokeColor = active ? new Color(1f, 0.68f, 0.1f, 0.95f) : new Color(0.35f, 0.85f, 0.9f, 0.45f);
            painter.lineWidth = active ? 2.5f : 1f;
            float y = TimelineTrackLayout.MarkerLaneHeight - 5f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(FieldView.Geometry.FrameToPosition(from.Frame), y));
            painter.LineTo(new Vector2(FieldView.Geometry.FrameToPosition(to.Frame), y));
            painter.Stroke();
        }

        void OnPointerMove(PointerMoveEvent e)
        {
            foreach (var clipViewPair in ClipViewMap)
            {
                clipViewPair.Value.OnHover(false);
                if (clipViewPair.Value.InMiddle(e.position))
                {
                    clipViewPair.Value.OnHover(true);
                    e.StopImmediatePropagation();
                }
            }
        }
        void OnPointerOut(PointerOutEvent e)
        {
            foreach (var clipViewPair in ClipViewMap)
            {
                clipViewPair.Value.OnHover(false);
            }
        }

        void OnMutedStateChanged()
        {
            SetEnabled(!Track.PersistentMuted);
        }

        class DragAndDropManipulator : PointerManipulator
        {
            // The Label in the window that shows the stored asset, if any.
            Label dropLabel;

            public Func<bool> DragValid;
            public Action<UnityEngine.Object, Vector2> DragPerform;

            public DragAndDropManipulator(VisualElement root)
            {
                // The target of the manipulator, the object to which to register all callbacks, is the drop area.
                target = root.Q<VisualElement>(className: "drop-area");
                dropLabel = root.Q<Label>(className: "drop-area__label");
            }

            protected override void RegisterCallbacksOnTarget()
            {
                // Register callbacks for various stages in the drag process.
                target.RegisterCallback<DragEnterEvent>(OnDragEnter);
                target.RegisterCallback<DragLeaveEvent>(OnDragLeave);
                target.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
                target.RegisterCallback<DragPerformEvent>(OnDragPerform);
            }
            protected override void UnregisterCallbacksFromTarget()
            {
                // Unregister all callbacks that you registered in RegisterCallbacksOnTarget().
                target.UnregisterCallback<DragEnterEvent>(OnDragEnter);
                target.UnregisterCallback<DragLeaveEvent>(OnDragLeave);
                target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdate);
                target.UnregisterCallback<DragPerformEvent>(OnDragPerform);
            }

            // This method runs if a user brings the pointer over the target while a drag is in progress.
            void OnDragEnter(DragEnterEvent _)
            {
                // Get the name of the object the user is dragging.
                var draggedName = string.Empty;
                if (DragAndDrop.objectReferences.Length > 0)
                {
                    draggedName = DragAndDrop.objectReferences[0].name;
                }

                // Change the appearance of the drop area if the user drags something over the drop area and holds it
                // there.
                //dropLabel.text = $"Dropping '{draggedName}'...";
                target.AddToClassList("drop-area--dropping");
            }

            // This method runs if a user makes the pointer leave the bounds of the target while a drag is in progress.
            void OnDragLeave(DragLeaveEvent _)
            {
                //dropLabel.text = "Drag an asset here...";
                target.RemoveFromClassList("drop-area--dropping");
            }

            // This method runs every frame while a drag is in progress.
            void OnDragUpdate(DragUpdatedEvent _)
            {
                if(DragValid())
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                else
                    DragAndDrop.visualMode = DragAndDropVisualMode.None;
            }

            // This method runs when a user drops a dragged object onto the target.
            void OnDragPerform(DragPerformEvent _)
            {
                var draggedName = string.Empty;
                if (DragAndDrop.objectReferences.Length > 0)
                {
                    draggedName = DragAndDrop.objectReferences[0].name;
                    DragPerform?.Invoke(DragAndDrop.objectReferences[0], _.localMousePosition);
                }
                // Visually update target to indicate that it now stores an asset.
                //dropLabel.text = $"Containing '{draggedName}'";
                target.RemoveFromClassList("drop-area--dropping");
            }
        }
    }
}
