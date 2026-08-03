using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace TreeDesigner.Editor
{
    public class TreeRectangleSelector : MouseManipulator
    {
        readonly List<ISelectable> m_BaseSelection = new List<ISelectable>();
        GraphView m_GraphView;
        VisualElement m_Rectangle;
        Vector2 m_Start;
        Vector2 m_End;
        bool m_Active;
        bool m_Additive;

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (m_Active || evt.button != 0 || evt.altKey || target is not GraphView graphView)
                return;

            if (!CanStartFrom(evt.target as VisualElement, graphView))
                return;

            m_GraphView = graphView;
            m_Start = m_End = ToGraphLocal(evt);
            m_Additive = evt.shiftKey || evt.actionKey;
            m_BaseSelection.Clear();

            if (m_Additive)
                m_BaseSelection.AddRange(graphView.selection);

            EnsureRectangle();
            UpdateRectangle();

            m_Active = true;
            target.CaptureMouse();
            ApplySelection();
            evt.StopPropagation();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (!m_Active || !target.HasMouseCapture())
                return;

            m_End = ToGraphLocal(evt);
            UpdateRectangle();
            ApplySelection();
            evt.StopPropagation();
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (!m_Active || evt.button != 0)
                return;

            m_End = ToGraphLocal(evt);
            UpdateRectangle();
            ApplySelection();
            EndSelection();
            evt.StopPropagation();
        }

        void OnMouseCaptureOut(MouseCaptureOutEvent evt)
        {
            if (m_Active)
                EndSelection(false);
        }

        void ApplySelection()
        {
            Rect selectionRect = ToWorldRect(m_Start, m_End);
            List<GraphElement> selectedElements = m_GraphView.graphElements
                .Where(CanSelect)
                .Where(i => selectionRect.Overlaps(i.worldBound))
                .ToList();

            m_GraphView.ClearSelection();

            if (m_Additive)
            {
                foreach (ISelectable selectable in m_BaseSelection.Where(i => i != null))
                    m_GraphView.AddToSelection(selectable);
            }

            foreach (GraphElement element in selectedElements)
            {
                if (!m_GraphView.selection.Contains(element))
                    m_GraphView.AddToSelection(element);
            }
        }

        void EndSelection(bool releaseMouse = true)
        {
            m_Active = false;
            m_BaseSelection.Clear();
            m_Rectangle.style.display = DisplayStyle.None;

            if (releaseMouse && target.HasMouseCapture())
                target.ReleaseMouse();
        }

        void EnsureRectangle()
        {
            if (m_Rectangle != null)
                return;

            m_Rectangle = new VisualElement
            {
                name = "tree-rectangle-selector",
                pickingMode = PickingMode.Ignore
            };
            m_Rectangle.style.position = Position.Absolute;
            m_Rectangle.style.borderLeftWidth = 1;
            m_Rectangle.style.borderRightWidth = 1;
            m_Rectangle.style.borderTopWidth = 1;
            m_Rectangle.style.borderBottomWidth = 1;
            m_Rectangle.style.borderLeftColor = new Color(0.35f, 0.65f, 1f, 0.95f);
            m_Rectangle.style.borderRightColor = new Color(0.35f, 0.65f, 1f, 0.95f);
            m_Rectangle.style.borderTopColor = new Color(0.35f, 0.65f, 1f, 0.95f);
            m_Rectangle.style.borderBottomColor = new Color(0.35f, 0.65f, 1f, 0.95f);
            m_Rectangle.style.backgroundColor = new Color(0.35f, 0.65f, 1f, 0.12f);
            m_Rectangle.style.display = DisplayStyle.None;

            target.hierarchy.Add(m_Rectangle);
        }

        void UpdateRectangle()
        {
            Rect rect = ToLocalRect(m_Start, m_End);
            m_Rectangle.style.display = DisplayStyle.Flex;
            m_Rectangle.style.left = rect.xMin;
            m_Rectangle.style.top = rect.yMin;
            m_Rectangle.style.width = rect.width;
            m_Rectangle.style.height = rect.height;
            m_Rectangle.BringToFront();
        }

        Vector2 ToGraphLocal(IMouseEvent evt)
        {
            return target.WorldToLocal(evt.mousePosition);
        }

        Rect ToWorldRect(Vector2 start, Vector2 end)
        {
            Vector2 worldStart = target.LocalToWorld(start);
            Vector2 worldEnd = target.LocalToWorld(end);
            return ToRect(worldStart, worldEnd);
        }

        static Rect ToLocalRect(Vector2 start, Vector2 end)
        {
            return ToRect(start, end);
        }

        static Rect ToRect(Vector2 start, Vector2 end)
        {
            return Rect.MinMaxRect(
                Mathf.Min(start.x, end.x),
                Mathf.Min(start.y, end.y),
                Mathf.Max(start.x, end.x),
                Mathf.Max(start.y, end.y));
        }

        static bool CanSelect(GraphElement element)
        {
            if (element == null || !element.visible || element.resolvedStyle.display == DisplayStyle.None)
                return false;

            return (element.capabilities & Capabilities.Selectable) == Capabilities.Selectable;
        }

        static bool CanStartFrom(VisualElement element, GraphView graphView)
        {
            if (element == null)
                return false;

            if (element == graphView || element == graphView.contentViewContainer || element == graphView.contentContainer)
                return true;

            if (element is GraphElement || element.GetFirstAncestorOfType<GraphElement>() != null)
                return false;

            return element.GetFirstAncestorOfType<IMGUIContainer>() == null;
        }
    }
}
