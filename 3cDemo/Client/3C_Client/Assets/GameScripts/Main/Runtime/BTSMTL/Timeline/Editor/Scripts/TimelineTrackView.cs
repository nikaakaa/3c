using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using BTSMTL.Editor;
using System;

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
        
        DropdownMenuHandler m_MenuHandler;
        Vector2 m_LocalMousePosition;
        bool m_RuntimeReadOnly;

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
            foreach (var clip in track.Clips)
            {
                TimelineClipView clipView = new TimelineClipView();
                clipView.SelectionContainer = FieldView;
                clipView.Init(clip, this);

                Add(clipView);
                FieldView.Elements.Add(clipView);
                ClipViewMap.Add(clip, clipView);
                ClipViews.Add(clipView);
            }

            DragAndDropManipulator dragAndDropManipulator = new DragAndDropManipulator(this);
            dragAndDropManipulator.DragValid = () => !m_RuntimeReadOnly && Track.DragValid();
            dragAndDropManipulator.DragPerform += (e1, e2) =>
            {
                if (m_RuntimeReadOnly)
                    return;
                int startFrame = FieldView.GetCloestFloorFrame(e2.x);
                if (Track.Clips.Find(i => i.StartFrame == startFrame) == null)
                {
                    TimelineData.ApplyModify(() =>
                    {
                        FieldView.AddClip(e1, Track, startFrame);
                    }, "Add Clip");
                }
            };
            this.AddManipulator(dragAndDropManipulator);

            transform.position = new Vector3(0, TimelineData.Tracks.IndexOf(track) * 40, 0);


            OnMutedStateChanged();
        }

        public void Refresh()
        {
            foreach (var clipViewPair in ClipViewMap)
            {
                clipViewPair.Value.Refresh();
            }
        }

        #region Selectable
        public override bool Overlaps(Rect rectangle)
        {
            return false;
        }
        public bool IsSelectable()
        {
            return false;
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
            int startFrame = FieldView.GetCloestFloorFrame(m_LocalMousePosition.x);
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

        public void SetRuntimeDebugState(bool active, string status)
        {
            style.borderLeftWidth = active ? 3f : 0f;
            style.borderLeftColor = active ? new Color(0.25f, 0.9f, 0.55f, 1f) : Color.clear;
            tooltip = active ? status ?? string.Empty : string.Empty;
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
