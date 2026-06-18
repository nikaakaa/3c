using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacterBehavior.Editor.ActionTimeline
{
    public enum CommittedActionTimelineDragLineDirection
    {
        Left,
        Right,
        Top,
        Bottom
    }

    public sealed class CommittedActionTimelineDragManipulator : IManipulator
    {
        VisualElement targetElement;
        Vector3 offset;
        bool dragging;
        PickingMode previousPickingMode;

        public CommittedActionTimelineDragManipulator(
            Action<PointerDownEvent> onDragStart,
            Action onDragStop,
            Action<Vector2> onDragMove,
            int button = 0)
        {
            OnDragStart = onDragStart;
            OnDragStop = onDragStop;
            OnDragMove = onDragMove;
            Button = button;
        }

        public bool Enabled { get; set; } = true;
        public int Button { get; }
        public Action<PointerDownEvent> OnDragStart { get; set; }
        public Action OnDragStop { get; set; }
        public Action<Vector2> OnDragMove { get; set; }

        public VisualElement target
        {
            get => targetElement;
            set
            {
                if (targetElement == value)
                    return;

                if (targetElement != null)
                {
                    targetElement.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                    targetElement.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                    targetElement.UnregisterCallback<PointerUpEvent>(OnPointerUp);
                    targetElement.RemoveFromClassList("draggable");
                }

                targetElement = value;
                if (targetElement == null)
                    return;

                targetElement.RegisterCallback<PointerDownEvent>(OnPointerDown);
                targetElement.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                targetElement.RegisterCallback<PointerUpEvent>(OnPointerUp);
                targetElement.AddToClassList("draggable");
            }
        }

        public void DragBeginForce(PointerDownEvent evt)
        {
            DragBeginForce(evt, evt.position);
        }

        public void DragBeginForce(PointerDownEvent evt, Vector2 pointerPosition)
        {
            if (!Enabled || targetElement == null)
                return;

            targetElement.AddToClassList("draggable--dragging");
            previousPickingMode = targetElement.pickingMode;
            targetElement.pickingMode = PickingMode.Ignore;
            dragging = true;
            offset = pointerPosition;
            targetElement.CapturePointer(evt.pointerId);
            OnDragStart?.Invoke(evt);
            evt.StopPropagation();
        }

        public static Vector2 ResolveDelta(Vector2 localPosition, Vector2 start)
        {
            return localPosition - start;
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (!Enabled || evt.button != Button)
                return;

            DragBeginForce(evt, evt.position);
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!dragging || targetElement == null || !targetElement.HasPointerCapture(evt.pointerId))
                return;

            if (!Enabled)
            {
                EndDrag(evt);
                return;
            }

            OnDragMove?.Invoke(ResolveDelta(evt.position, offset));
            evt.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (!dragging || evt.button != Button)
                return;

            EndDrag(evt);
            evt.StopPropagation();
        }

        void EndDrag(IPointerEvent evt)
        {
            if (targetElement == null)
                return;

            targetElement.RemoveFromClassList("draggable--dragging");
            if (targetElement.HasPointerCapture(evt.pointerId))
                targetElement.ReleasePointer(evt.pointerId);
            targetElement.pickingMode = previousPickingMode;
            dragging = false;
            OnDragStop?.Invoke();
        }
    }

    public sealed class CommittedActionTimelineDragLineManipulator : PointerManipulator
    {
        readonly CommittedActionTimelineDragLineDirection direction;
        readonly Action<Vector2> onDragMove;
        readonly Action<PointerDownEvent> onDragStart;
        readonly Action onDragStop;
        Vector3 start;

        public CommittedActionTimelineDragLineManipulator(
            CommittedActionTimelineDragLineDirection direction,
            Action<Vector2> onDragMove,
            Action<PointerDownEvent> onDragStart,
            Action onDragStop)
        {
            this.direction = direction;
            this.onDragMove = onDragMove;
            this.onDragStart = onDragStart;
            this.onDragStop = onDragStop;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        public bool Active { get; private set; }
        public bool Enabled { get; set; } = true;
        public float Size { get; set; } = 4f;
        public float Offset { get; set; }
        public IMGUIContainer Handle { get; private set; }

        protected override void RegisterCallbacksOnTarget()
        {
            Handle = new IMGUIContainer(DrawCursor);
            Handle.style.position = Position.Absolute;
            Handle.style.marginTop = 0;
            Handle.style.marginRight = 0;
            Handle.style.marginBottom = 0;
            Handle.style.marginLeft = 0;
            ApplyHandleLayout();
            Handle.RegisterCallback<PointerDownEvent>(OnPointerDown);
            Handle.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            Handle.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.Add(Handle);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            if (Handle == null)
                return;

            Handle.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            Handle.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            Handle.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.Remove(Handle);
            Handle = null;
        }

        public static Vector2 ResolveDelta(Vector2 localPosition, Vector2 start)
        {
            return localPosition - start;
        }

        void ApplyHandleLayout()
        {
            switch (direction)
            {
                case CommittedActionTimelineDragLineDirection.Left:
                    Handle.style.left = Offset;
                    Handle.style.width = Size;
                    Handle.style.height = Length.Percent(100);
                    break;
                case CommittedActionTimelineDragLineDirection.Right:
                    Handle.style.right = Offset;
                    Handle.style.width = Size;
                    Handle.style.height = Length.Percent(100);
                    break;
                case CommittedActionTimelineDragLineDirection.Top:
                    Handle.style.top = Offset;
                    Handle.style.width = Length.Percent(100);
                    Handle.style.height = Size;
                    break;
                case CommittedActionTimelineDragLineDirection.Bottom:
                    Handle.style.bottom = Offset;
                    Handle.style.width = Length.Percent(100);
                    Handle.style.height = Size;
                    break;
            }
        }

        void DrawCursor()
        {
            if (!Enabled || Active)
                return;

            Rect rect = direction == CommittedActionTimelineDragLineDirection.Left ||
                        direction == CommittedActionTimelineDragLineDirection.Right
                ? new Rect(0, 0, Size, target.worldBound.height)
                : new Rect(0, 0, target.worldBound.width, Size);
            MouseCursor cursor = direction == CommittedActionTimelineDragLineDirection.Left ||
                                 direction == CommittedActionTimelineDragLineDirection.Right
                ? MouseCursor.ResizeHorizontal
                : MouseCursor.ResizeVertical;
            EditorGUIUtility.AddCursorRect(rect, cursor);
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (!Enabled || Active || !CanStartManipulation(evt))
                return;

            start = evt.position;
            Active = true;
            Handle.CapturePointer(evt.pointerId);
            onDragStart?.Invoke(evt);
            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!Enabled || !Active || !Handle.HasPointerCapture(evt.pointerId))
                return;

            onDragMove?.Invoke(ResolveDelta(evt.position, start));
            evt.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (!Enabled || !Active || !CanStopManipulation(evt))
                return;

            Active = false;
            if (Handle.HasPointerCapture(evt.pointerId))
                Handle.ReleasePointer(evt.pointerId);
            onDragStop?.Invoke();
            evt.StopPropagation();
        }
    }
}
