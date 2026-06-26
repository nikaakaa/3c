using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace TreeDesigner.Editor
{
    public class CustomContextualMenuManipulator : MouseManipulator
    {
        Action<ContextualMenuPopulateEvent> m_MenuBuilder;

        public CustomContextualMenuManipulator(Action<ContextualMenuPopulateEvent> menuBuilder, MouseButton mouseButton)
        {
            m_MenuBuilder = menuBuilder;
            activators.Add(new ManipulatorActivationFilter
            {
                button = mouseButton
            });
        }

        //
        // ժҪ:
        //     RegisterUndo the event callbacks on the manipulator target.
        protected override void RegisterCallbacksOnTarget()
        {
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                base.target.RegisterCallback<MouseDownEvent>(OnMouseUpDownEvent);
            }
            else
            {
                base.target.RegisterCallback<MouseUpEvent>(OnMouseUpDownEvent);
            }

            base.target.RegisterCallback<KeyUpEvent>(OnKeyUpEvent);
            base.target.RegisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuEvent);
        }

        //
        // ժҪ:
        //     UnregisterUndo the event callbacks from the manipulator target.
        protected override void UnregisterCallbacksFromTarget()
        {
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                base.target.UnregisterCallback<MouseDownEvent>(OnMouseUpDownEvent);
            }
            else
            {
                base.target.UnregisterCallback<MouseUpEvent>(OnMouseUpDownEvent);
            }

            base.target.UnregisterCallback<KeyUpEvent>(OnKeyUpEvent);
            base.target.UnregisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuEvent);
        }

        private void OnMouseUpDownEvent(IMouseEvent evt)
        {
            if (CanStartManipulation(evt) && base.target.panel != null && base.target.panel.contextualMenuManager != null)
            {
                EventBase eventBase = evt as EventBase;
                base.target.panel.contextualMenuManager.DisplayMenu(eventBase, base.target);
                eventBase.StopPropagation();
            }
        }

        private void OnKeyUpEvent(KeyUpEvent evt)
        {
            if (evt.keyCode == KeyCode.Menu && base.target.panel != null && base.target.panel.contextualMenuManager != null)
            {
                base.target.panel.contextualMenuManager.DisplayMenu(evt, base.target);
                evt.StopPropagation();
            }
        }

        private void OnContextualMenuEvent(ContextualMenuPopulateEvent evt)
        {
            if (m_MenuBuilder != null)
            {
                m_MenuBuilder(evt);
            }
        }
    }
}