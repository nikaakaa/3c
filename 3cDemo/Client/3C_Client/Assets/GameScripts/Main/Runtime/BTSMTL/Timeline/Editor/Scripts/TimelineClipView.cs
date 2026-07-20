using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using BTSMTL.Editor;
using System.Reflection;
using System;

namespace BTSMTL.Timeline.Editor
{
    public class TimelineClipView : VisualElement, ISelectable
    {
        public new class UxmlFactory : UxmlFactory<TimelineClipView, UxmlTraits> { }

        string m_DefaultVisualTreeGuid = "712bee562e7c28a4590e4900d51e6da0";
        protected virtual string VisualTreeGuid => m_DefaultVisualTreeGuid;

        public bool Selected { get; private set; }
        public bool Hovered { get; private set; }
        public ISelection SelectionContainer { get; set; }
        public ClipCapabilities Capabilities => Clip.Capabilities;

        public TimelineFieldView FieldView => SelectionContainer as TimelineFieldView;
        public TimelineEditorView EditorWindow => FieldView.EditorWindow;
        public TimelineData TimelineData => EditorWindow.Timeline;

        public Clip Clip { get; private set; }
        public TimelineTrackView TrackView { get; private set; }

        public int StartFrame => Clip.StartFrame;
        public int EndFrame => Clip.EndFrame;
        public int OtherEaseInFrame => Clip.OtherEaseInFrame;
        public int OtherEaseOutFrame => Clip.OtherEaseOutFrame;
        public int SelfEaseInFrame => Clip.SelfEaseInFrame;
        public int SelfEaseOutFrame => Clip.SelfEaseOutFrame;
        public int EaseInFrame => Clip.EaseInFrame;
        public int EaseOutFrame => Clip.EaseOutFrame;

        public int ClipInFrame => Clip.ClipInFrame;
        public int WidthFrame => EndFrame - StartFrame;

        DragLineManipulator m_LeftResizeDragLine;
        DragLineManipulator m_SelfEaseInDragLine;
        DragLineManipulator m_RightResizeDragLine;
        DragLineManipulator m_SelfEaseOutDragLine;
        DragManipulator m_MoveDrag;
        DropdownMenuHandler m_MenuHandler;
        bool m_RuntimeReadOnly;

        VisualElement m_Content;
        VisualElement m_LeftMixer;
        VisualElement m_RightMixer;
        VisualElement m_Title;
        Label m_ClipName;
        VisualElement m_LeftClipIn;
        VisualElement m_RightClipIn;
        VisualElement m_BottomLine;
        VisualElement m_DrawBox;

        internal VisualElement ContentElement => m_Content;
        internal VisualElement LeftMixerElement => m_LeftMixer;
        internal VisualElement RightMixerElement => m_RightMixer;
        internal VisualElement TitleElement => m_Title;
        internal VisualElement LeftClipInElement => m_LeftClipIn;
        internal VisualElement RightClipInElement => m_RightClipIn;
        internal VisualElement BottomLineElement => m_BottomLine;

        public bool SelfEaseIn
        {
            get
            {
                return m_SelfEaseInDragLine?.Enable ?? false;
            }
            set
            {
                if (m_SelfEaseInDragLine != null)
                {
                    m_SelfEaseInDragLine.Enable = value;
                    m_LeftResizeDragLine.Enable = !value;
                }
            }
        }
        public bool SelfEaseOut
        {
            get
            {
                return m_SelfEaseOutDragLine?.Enable ?? false;
            }
            set
            {
                if (m_SelfEaseOutDragLine != null)
                {
                    m_SelfEaseOutDragLine.Enable = value;
                    m_RightResizeDragLine.Enable = !value;
                }
            }
        }

        public TimelineClipView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(m_DefaultVisualTreeGuid));
            visualTree.CloneTree(this);
            AddToClassList("timelineClip");

            m_Content = this.Q("content");
            m_LeftMixer = this.Q("left-mixer");
            m_RightMixer = this.Q("right-mixer");

            m_Title = this.Q("title");
            m_ClipName = this.Q<Label>("clip-name");
            m_LeftClipIn = this.Q("left-clip-in");
            m_RightClipIn = this.Q("right-clip-in");
            m_BottomLine = this.Q("bottom-line");
            m_DrawBox = this.Q("draw-box");

            m_MoveDrag = new DragManipulator(OnStartDrag, OnStopDrag, OnDragMove);
            m_MoveDrag.enabled = false;
            this.AddManipulator(m_MoveDrag);

            m_MenuHandler = new DropdownMenuHandler(MenuBuilder);

            RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0 && evt.clickCount == 2)
                {
                    EditorWindow.OpenClip(Clip);
                    evt.StopPropagation();
                }
            });


            m_DrawBox.generateVisualContent += OnDrawBoxGenerateVisualContent;
        }

        public void Init(Clip clip, TimelineTrackView trackView)
        {
            Clip = clip;
            Clip.OnNameChanged = () => m_ClipName.text = clip.Name;
            m_ClipName.text = clip.Name;
            TrackView = trackView;
            m_BottomLine.style.backgroundColor = clip.Color();

            if (clip.IsResizable())
            {
                m_LeftResizeDragLine = new DragLineManipulator(DragLineDirection.Left,
                (e) =>
                {
                    FieldView.Interaction.UpdateResize(e.x);
                },
                (e) =>
                {
                    FieldView.Interaction.BeginResize(this, 0);
                    if (!IsSelected())
                    {
                        SelectionContainer.ClearSelection();
                        SelectionContainer.AddToSelection(this);
                    }
                    FieldView.SetEditFrames(StartFrame);
                },
                () =>
                {
                    FieldView.Interaction.CommitEdit("Resize Clip");
                });
                m_LeftResizeDragLine.Size = 4;
                this.AddManipulator(m_LeftResizeDragLine);

                m_RightResizeDragLine = new DragLineManipulator(DragLineDirection.Right,
                (e) =>
                {
                    FieldView.Interaction.UpdateResize(e.x);
                },
                (e) =>
                {
                    FieldView.Interaction.BeginResize(this, 1);
                    if (!IsSelected())
                    {
                        SelectionContainer.ClearSelection();
                        SelectionContainer.AddToSelection(this);
                    }
                    FieldView.SetEditFrames(EndFrame);
                },
                () =>
                {
                    FieldView.Interaction.CommitEdit("Resize Clip");
                });
                m_RightResizeDragLine.Size = 4;
                this.AddManipulator(m_RightResizeDragLine);
            }

            if (clip.IsMixable())
            {
                m_SelfEaseInDragLine = new DragLineManipulator(DragLineDirection.Right,
                (e) =>
                {
                    FieldView.Interaction.UpdateEase(e.x);
                },
                (e) =>
                {
                    FieldView.Interaction.BeginEase(this, 0);
                    FieldView.SetEditFrames(StartFrame + SelfEaseInFrame);
                },
                () =>
                {
                    FieldView.Interaction.CommitEdit("Resize Clip");
                });
                m_SelfEaseInDragLine.Size = 4;
                m_LeftMixer.AddManipulator(m_SelfEaseInDragLine);
                SelfEaseIn = false;

                m_SelfEaseOutDragLine = new DragLineManipulator(DragLineDirection.Left,
                (e) =>
                {
                    FieldView.Interaction.UpdateEase(e.x);
                },
                (e) =>
                {
                    FieldView.Interaction.BeginEase(this, 1);
                    FieldView.SetEditFrames(EndFrame - SelfEaseOutFrame);
                },
                () =>
                {
                    FieldView.Interaction.CommitEdit("Resize Clip");
                });
                m_SelfEaseOutDragLine.Size = 4;
                m_RightMixer.AddManipulator(m_SelfEaseOutDragLine);
                SelfEaseOut = false;
            }

            Refresh();
        }

        public void Resize(int startFrame, int endFrame)
        {
            int deltaStartFrame = startFrame - Clip.StartFrame;
            float easeInRatio = (float)Clip.SelfEaseInFrame / Clip.Duration;
            float easeOutRatio = (float)Clip.SelfEaseOutFrame / Clip.Duration;

            Clip.StartFrame += deltaStartFrame;
            if(Clip.IsClipInable())
                Clip.ClipInFrame += deltaStartFrame;
            Clip.EndFrame = endFrame;

            Clip.SelfEaseInFrame = Mathf.RoundToInt(easeInRatio * Clip.Duration);
            Clip.SelfEaseOutFrame = Mathf.Min(Mathf.RoundToInt(easeOutRatio * Clip.Duration), Clip.Duration - SelfEaseInFrame);

            Clip.Track.UpdateMix();            
        }
        public void AdjustSelfEase(int border, int deltaFrame)
        {
            if (border == 0)
                Clip.SelfEaseInFrame += deltaFrame;
            else
                Clip.SelfEaseOutFrame -= deltaFrame;
            Clip.Track.UpdateMix();
        }
        public void Move(int deltaFrame)
        {
            Clip.StartFrame += deltaFrame;
            Clip.EndFrame += deltaFrame;
        }
        public void ResetMove(int deltaFrame)
        {
            Clip.Invalid = false;
            Clip.StartFrame -= deltaFrame;
            Clip.EndFrame -= deltaFrame;
        }
        public void Refresh()
        {
            FieldView.Rendering.ApplyClipAuthoring(this, new TimelineClipRenderInput(this));
        }

        #region Selectable
        public override bool Overlaps(Rect rectangle)
        {
            return FieldView.Geometry.HitTest(localBound, rectangle);
        }

        public virtual bool IsSelectable()
        {
            return true;
        }
        public bool IsSelected()
        {
            return Selected;
        }
        public void Select()
        {
            Selected = true;
            BringToFront();
            AddToClassList("selected");

            OnHover(false);
            m_DrawBox.MarkDirtyRepaint();
        }
        public void Unselect()
        {
            Selected = false;
            RemoveFromClassList("selected");
            m_DrawBox.MarkDirtyRepaint();
            m_MoveDrag.enabled = false;
        }
        #endregion

        public bool InMiddle(Vector2 worldPosition)
        {
            return m_Content.worldBound.Contains(worldPosition);
        }
        public void OnPointerDown(PointerDownEvent e)
        {
            if (e.button == 0)
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
                if (!m_RuntimeReadOnly)
                {
                    m_MoveDrag.enabled = true;
                    m_MoveDrag.DragBeginForce(e);
                }
            }
            else if (e.button == 1)
            {
                if (m_RuntimeReadOnly)
                    return;
                m_MenuHandler.ShowMenu(e);
                e.StopImmediatePropagation();
            }
        }
        public void OnHover(bool value)
        {
            if (value && !Hovered && !Selected)
            {
                Hovered = true;
                parent.Add(m_DrawBox);
                m_DrawBox.style.left = style.left;
                m_DrawBox.style.width = style.width;
                m_DrawBox.MarkDirtyRepaint();
            }
            else if (!value && Hovered)
            {
                Hovered = false;
                Add(m_DrawBox);
                m_DrawBox.MarkDirtyRepaint();
            }
        }
        void MenuBuilder(DropdownMenu menu)
        {
            if (m_RuntimeReadOnly)
                return;
            menu.AppendAction("Adjust Self Ease In", (e) =>
            {
                SelfEaseIn = !SelfEaseIn;
            },
            (e) =>
            {
                if (!Clip.IsMixable())
                    return DropdownMenuAction.Status.None;
                else if (OtherEaseInFrame > 0)
                    return DropdownMenuAction.Status.Disabled;
                else if (SelfEaseIn)
                    return DropdownMenuAction.Status.Checked;
                else
                    return DropdownMenuAction.Status.Normal;
            });

            menu.AppendAction("Adjust Self Ease Out", (e) =>
            {
                SelfEaseOut = !SelfEaseOut;
            },
            (e) =>
            {
                if (!Clip.IsMixable())
                    return DropdownMenuAction.Status.None;
                else if (OtherEaseOutFrame > 0)
                    return DropdownMenuAction.Status.Disabled;
                else if (SelfEaseOut)
                    return DropdownMenuAction.Status.Checked;
                else
                    return DropdownMenuAction.Status.Normal;
            });

            menu.AppendAction("Remove Clip", (e) =>
            {
                TimelineData.ApplyModify(() =>
                {
                    TimelineData.RemoveClip(Clip);
                }, "Remove Clip");
            });
            menu.AppendAction("Open Script", (e) =>
            {
                Clip.OpenClipScript();
            });

            menu.AppendAction("Copy Properties", (e) =>
            {
                CopyType = Clip.GetType();
                CopyValueMap.Clear();
                foreach (var fieldInfo in Clip.GetAllFields())
                {
                    if(fieldInfo.GetCustomAttribute<ShowInInspectorAttribute>() != null)
                        CopyValueMap.Add(fieldInfo, fieldInfo.GetValue(Clip));
                }
            });
            menu.AppendAction("Paste Properties", (e) =>
            {
                foreach (var fieldInfo in Clip.GetAllFields())
                {
                    if (fieldInfo.GetCustomAttribute<ShowInInspectorAttribute>() != null && CopyValueMap.TryGetValue(fieldInfo,out object value))
                        fieldInfo.SetValue(Clip, value);
                }
            },
            (e)=>
            {
                if (CopyType == null)
                    return DropdownMenuAction.Status.None;
                else if (CopyType != Clip.GetType())
                    return DropdownMenuAction.Status.Disabled;
                else
                    return DropdownMenuAction.Status.Normal;
            });
        }

        public void SetRuntimeReadOnly(bool readOnly)
        {
            m_RuntimeReadOnly = readOnly;
            m_MoveDrag.enabled = false;
            if (m_LeftResizeDragLine != null)
                m_LeftResizeDragLine.Enable = !readOnly && !SelfEaseIn;
            if (m_RightResizeDragLine != null)
                m_RightResizeDragLine.Enable = !readOnly && !SelfEaseOut;
            if (m_SelfEaseInDragLine != null)
                m_SelfEaseInDragLine.Enable = !readOnly && SelfEaseIn;
            if (m_SelfEaseOutDragLine != null)
                m_SelfEaseOutDragLine.Enable = !readOnly && SelfEaseOut;
        }

        void OnStartDrag(PointerDownEvent ev)
        {
            Clip.Invalid = false;
            FieldView.Interaction.BeginMove(this);
        }
        void OnStopDrag()
        {
            Clip.Invalid = false;
            FieldView.Interaction.CommitEdit("Move Clip");
        }
        void OnDragMove(Vector2 deltaPosition)
        {
            FieldView.Interaction.UpdateMove(deltaPosition.x);
        }
        void OnDrawBoxGenerateVisualContent(MeshGenerationContext mgc)
        {
            FieldView.Rendering.DrawClipSelection(this, mgc);
        }


        static Type CopyType;
        static Dictionary<FieldInfo,object> CopyValueMap = new Dictionary<FieldInfo,object>();
    }

}
