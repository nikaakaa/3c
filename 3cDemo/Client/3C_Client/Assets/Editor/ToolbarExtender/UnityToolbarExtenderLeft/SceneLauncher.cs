#if !UNITY_6000_3_OR_NEWER

using ThirdPersonCharacter.Editor.CharacterSimulation;
using UnityEditor;
using UnityEngine;

namespace TEngine
{
    public partial class UnityToolbarExtenderLeft
    {
        private const string ButtonStyleName = "Tab middle";

        private static GUIStyle _buttonGuiStyle;

        private static void OnToolbarGUI_SceneLauncher()
        {
            _buttonGuiStyle ??= new GUIStyle(ButtonStyleName)
            {
                padding = new RectOffset(2, 8, 2, 2),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("Launcher", EditorGUIUtility.FindTexture("PlayButton"), "Open gameplay test launcher"), _buttonGuiStyle))
            {
                GameplayTestLauncherWindow.Open();
            }
        }
    }
}

#endif
