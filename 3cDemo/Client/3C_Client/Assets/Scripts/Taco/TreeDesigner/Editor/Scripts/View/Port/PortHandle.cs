using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using Taco.Editor;

namespace TreeDesigner.Editor
{
    public class PortHandle : VisualElement, IDragableVisualElement
    {
        PropertyPortView m_PropertyPortView;
        public PropertyPortView PropertyPortView => m_PropertyPortView;

        DragHandle m_DragHandle;

        public PortHandle(PropertyPortView propertyPortView)
        {
            name = "portHandle";
            m_PropertyPortView = propertyPortView;
            m_DragHandle = new DragHandle();
            m_DragHandle.Init(this);
            AddToClassList("portHandle");
            switch (propertyPortView.direction)
            {
                case Direction.Input:
                    AddToClassList("inputPortHandle");
                    break;
                case Direction.Output:
                    AddToClassList("outputPortHandle");
                    break;
            }
        }

        public void StartDrag()
        {
            m_PropertyPortView.AddToClassList("dragged");
        }
        public void StopDrag()
        {
            m_PropertyPortView.RemoveFromClassList("dragged");
        }
        public void UpdateDrag(DragUpdatedEvent e, VisualElement dragArea)
        {

        }
        public void PerformDrag(DragPerformEvent e, VisualElement dragArea)
        {
          
        }
    }
}