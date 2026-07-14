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

        float TopOffset = 5;
        float YminOffset = -77;
        float Interval = 40;
        
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
            TrackView.OnSelected = () =>
            {
                SelectionContainer.AddToSelection(this);
            };
            TrackView.OnUnselected = () =>
            {
                SelectionContainer.RemoveFromSelection(this);
            };

            style.borderLeftColor = Track.Color();

            NameField = this.Q<TextField>();
            SerializedProperty serializedProperty = TimelineData.SerializedData.FindPropertyRelative("m_Tracks");
            serializedProperty = serializedProperty.GetArrayElementAtIndex(TimelineData.Tracks.IndexOf(Track));
            NameField.bindingPath =  serializedProperty.FindPropertyRelative("Name").propertyPath;
            NameField.Bind(TimelineData.SerializedTimeline);

            Icon = this.Q("icon");
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(IconGuidAttribute.Guid(Track.GetType())));
            if (texture)
                Icon.style.backgroundImage = texture;

            FieldView.OnGeometryChangedCallback += OnGeometryChanged;
            this.RegisterCallbackOnce<GeometryChangedEvent>((e) => OnGeometryChanged());
            RegisterCallback<DetachFromPanelEvent>((e) => FieldView.OnGeometryChangedCallback -= OnGeometryChanged);
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
                targetY = Mathf.Clamp(targetY, TopOffset, (TimelineData.Tracks.Count - 1) * Interval + TopOffset);
                transform.position = new Vector3(0, targetY, 0);
                TrackView.transform.position = new Vector3(0, targetY - TopOffset, 0);

                int index = TimelineData.Tracks.IndexOf(Track);
                int targetIndex = Mathf.FloorToInt(targetY / Interval);
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

        void OnGeometryChanged()
        {
            transform.position = new Vector3(0, TrackView.worldBound.yMin + YminOffset, 0);
        }
        void MenuBuilder(DropdownMenu menu)
        {
            menu.AppendAction("Add Clip", (e) =>
            {
                TimelineData.ApplyModify(() =>
                {
                    FieldView.AddClip(Track, FieldView.GetRightEdgeFrame(Track));
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
                        FieldView.RemoveFromSelection(this);
                    }
                }
                if (!m_RuntimeReadOnly)
                    DragManipulator.DragBeginForce(e, this.WorldToLocal(e.position));
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
                trackHandle.NameField.bindingPath = TimelineData.GetSerializedPropertyPath(
                    $"m_Tracks.Array.data[{TimelineData.Tracks.IndexOf(trackHandle.Track)}].Name");
                trackHandle.NameField.Bind(TimelineData.SerializedTimeline);

                if (!trackHandle.Draging)
                {
                    float targetY = TimelineData.Tracks.IndexOf(trackHandle.Track) * Interval + TopOffset;
                    float currentY = trackHandle.transform.position.y;
                    if(Mathf.Abs(currentY - targetY) > 1f)
                    {
                        Tweening = true;
                        targetY = Mathf.Lerp(currentY, targetY, 0.05f);
                    }
                    trackHandle.transform.position = new Vector3(0, targetY, 0);
                    trackHandle.TrackView.transform.position = new Vector3(0, targetY - TopOffset, 0);
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
