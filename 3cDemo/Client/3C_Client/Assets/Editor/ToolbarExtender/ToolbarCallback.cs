#if !UNITY_6000_3_OR_NEWER

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace UnityToolbarExtender
{
    public static class ToolbarCallback
    {
        private static readonly Type ToolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static readonly Type GuiViewType = typeof(Editor).Assembly.GetType("UnityEditor.GUIView");

#if UNITY_2020_1_OR_NEWER
        private static readonly Type WindowBackendType = typeof(Editor).Assembly.GetType("UnityEditor.IWindowBackend");
        private static readonly PropertyInfo WindowBackend = GuiViewType.GetProperty("windowBackend",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly PropertyInfo ViewVisualTree = WindowBackendType.GetProperty("visualTree",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#else
        private static readonly PropertyInfo ViewVisualTree = GuiViewType.GetProperty("visualTree",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#endif

        private static readonly FieldInfo ImguiContainerOnGui = typeof(IMGUIContainer).GetField("m_OnGUIHandler",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private static ScriptableObject _currentToolbar;

        public static Action OnToolbarGUI;
        public static Action OnToolbarGUILeft;
        public static Action OnToolbarGUIRight;

        static ToolbarCallback()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            if (_currentToolbar != null)
            {
                return;
            }

            var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
            _currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
            if (_currentToolbar == null)
            {
                return;
            }

#if UNITY_2021_1_OR_NEWER
            var root = _currentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
            var rawRoot = root.GetValue(_currentToolbar);
            var visualRoot = rawRoot as VisualElement;
            RegisterCallback(visualRoot, "ToolbarZoneLeftAlign", OnToolbarGUILeft);
            RegisterCallback(visualRoot, "ToolbarZoneRightAlign", OnToolbarGUIRight);
#else
#if UNITY_2020_1_OR_NEWER
            var windowBackend = WindowBackend.GetValue(_currentToolbar);
            var visualTree = (VisualElement)ViewVisualTree.GetValue(windowBackend, null);
#else
            var visualTree = (VisualElement)ViewVisualTree.GetValue(_currentToolbar, null);
#endif

            var container = (IMGUIContainer)visualTree[0];
            var handler = (Action)ImguiContainerOnGui.GetValue(container);
            handler -= OnGUI;
            handler += OnGUI;
            ImguiContainerOnGui.SetValue(container, handler);
#endif
        }

#if UNITY_2021_1_OR_NEWER
        private static void RegisterCallback(VisualElement visualRoot, string rootName, Action callback)
        {
            var toolbarZone = visualRoot.Q(rootName);
            var parent = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row
                }
            };
            var container = new IMGUIContainer { style = { flexGrow = 1 } };
            container.onGUIHandler += () => callback?.Invoke();
            parent.Add(container);
            toolbarZone.Add(parent);
        }
#endif

        private static void OnGUI()
        {
            OnToolbarGUI?.Invoke();
        }
    }
}

#endif
