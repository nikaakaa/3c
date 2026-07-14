#if !UNITY_6000_3_OR_NEWER

using UnityEditor;
using UnityEngine;

namespace TEngine
{
    public partial class UnityToolbarExtenderRight
    {
        private const string EditorPlayModeKey = "EditorPlayMode";
        private const string ButtonStyleName = "Tab middle";

        private static readonly string[] _resourceModeNames =
        {
            "EditorMode (编辑器下的模拟模式)",
            "OfflinePlayMode (单机模式)",
            "HostPlayMode (联机运行模式)",
            "WebPlayMode (WebGL运行模式)"
        };

        private static int _resourceModeIndex;

        public static int ResourceModeIndex => _resourceModeIndex;

        private static class ToolbarStyles
        {
            public static readonly GUIStyle PopupStyle = new GUIStyle(EditorStyles.popup)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(6, 6, 0, 0)
            };
        }

        private static void OnToolbarGUI_EditorPlayMode()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                GUILayout.Space(8);

                var maxWidth = 0f;
                foreach (var mode in _resourceModeNames)
                {
                    var size = ToolbarStyles.PopupStyle.CalcSize(new GUIContent(mode));
                    if (size.x > maxWidth)
                    {
                        maxWidth = size.x;
                    }
                }

                var popupWidth = Mathf.Clamp(maxWidth + 20, 100, 220);

                GUILayout.BeginHorizontal();
                var selectedIndex = EditorGUILayout.Popup(
                    _resourceModeIndex,
                    _resourceModeNames,
                    ToolbarStyles.PopupStyle,
                    GUILayout.Width(popupWidth));

                if (selectedIndex != _resourceModeIndex)
                {
                    _resourceModeIndex = selectedIndex;
                    EditorPrefs.SetInt(EditorPlayModeKey, selectedIndex);
                    Debug.Log($"更改编辑器资源运行模式：{_resourceModeNames[selectedIndex]}");
                }

                GUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}

#endif
