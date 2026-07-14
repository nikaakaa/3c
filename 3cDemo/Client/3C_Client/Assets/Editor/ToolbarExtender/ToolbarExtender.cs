#if !UNITY_6000_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityToolbarExtender
{
    [InitializeOnLoad]
    public static class ToolbarExtender
    {
        public const float Space = 8;
        public const float LargeSpace = 20;
        public const float ButtonWidth = 32;
        public const float DropdownWidth = 80;
        public const float PlayPauseStopWidth = 140;

        public static readonly List<Action> LeftToolbarGUI = new List<Action>();
        public static readonly List<Action> RightToolbarGUI = new List<Action>();

        private static readonly int ToolCount;
        private static GUIStyle _commandStyle;

        static ToolbarExtender()
        {
            var toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
            var toolIcons = toolbarType.GetField("k_ToolCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            ToolCount = toolIcons != null ? (int)toolIcons.GetValue(null) : 8;

            ToolbarCallback.OnToolbarGUI = OnGUI;
            ToolbarCallback.OnToolbarGUILeft = GUILeft;
            ToolbarCallback.OnToolbarGUIRight = GUIRight;
        }

        private static void OnGUI()
        {
            _commandStyle ??= new GUIStyle("CommandLeft");

            var screenWidth = EditorGUIUtility.currentViewWidth;
            var playButtonsPosition = Mathf.RoundToInt((screenWidth - PlayPauseStopWidth) / 2);

            var leftRect = new Rect(0, 0, screenWidth, Screen.height);
            leftRect.xMin += Space;
            leftRect.xMin += ButtonWidth * ToolCount;
            leftRect.xMin += Space;
            leftRect.xMin += 64 * 2;
            leftRect.xMax = playButtonsPosition;

            var rightRect = new Rect(0, 0, screenWidth, Screen.height);
            rightRect.xMin = playButtonsPosition + _commandStyle.fixedWidth * 3;
            rightRect.xMax = screenWidth - Space - DropdownWidth - Space - DropdownWidth - Space - DropdownWidth - Space - ButtonWidth - Space - 78;

            leftRect.xMin += Space;
            leftRect.xMax -= Space;
            rightRect.xMin += Space;
            rightRect.xMax -= Space;

            leftRect.y = 4;
            leftRect.height = 22;
            rightRect.y = 4;
            rightRect.height = 22;

            if (leftRect.width > 0)
            {
                GUILayout.BeginArea(leftRect);
                GUILeft();
                GUILayout.EndArea();
            }

            if (rightRect.width > 0)
            {
                GUILayout.BeginArea(rightRect);
                GUIRight();
                GUILayout.EndArea();
            }
        }

        public static void GUILeft()
        {
            GUILayout.BeginHorizontal();
            foreach (var handler in LeftToolbarGUI)
            {
                handler();
            }
            GUILayout.EndHorizontal();
        }

        public static void GUIRight()
        {
            GUILayout.BeginHorizontal();
            foreach (var handler in RightToolbarGUI)
            {
                handler();
            }
            GUILayout.EndHorizontal();
        }
    }
}

#endif
