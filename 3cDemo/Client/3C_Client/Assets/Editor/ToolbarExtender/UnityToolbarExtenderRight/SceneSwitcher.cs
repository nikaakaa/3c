#if !UNITY_6000_3_OR_NEWER

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TEngine
{
    public partial class UnityToolbarExtenderRight
    {
        private static List<(string sceneName, string scenePath)> _initScenes;
        private static List<(string sceneName, string scenePath)> _defaultScenes;
        private static List<(string sceneName, string scenePath)> _otherScenes;

        private const string InitScenePath = "Assets/Scenes";
        private const string DefaultScenePath = "Assets/AssetRaw/Scenes";

        private static void UpdateScenes()
        {
            _initScenes = SceneSwitcher.GetScenesInPath(InitScenePath);
            _defaultScenes = SceneSwitcher.GetScenesInPath(DefaultScenePath);

            var allScenes = SceneSwitcher.GetAllScenes();
            _otherScenes = new List<(string sceneName, string scenePath)>(allScenes);
            _otherScenes.RemoveAll(scene =>
                _initScenes.Exists(init => init.scenePath == scene.scenePath) ||
                _defaultScenes.Exists(defaultScene => defaultScene.scenePath == scene.scenePath));
        }

        private static void OnToolbarGUI_SceneSwitch()
        {
            if (_initScenes.Count == 0 && _defaultScenes.Count == 0 && _otherScenes.Count == 0)
            {
                return;
            }

            var currentSceneName = SceneManager.GetActiveScene().name;
            EditorGUILayout.LabelField("当前场景:", GUILayout.Width(52));

            var content = new GUIContent(currentSceneName);
            var textSize = GUI.skin.button.CalcSize(content);
            var buttonWidth = Mathf.Clamp(textSize.x + 16, 70, 180);

            var menu = new GenericMenu();
            AddScenesToMenu(_initScenes, "初始化场景", menu);
            AddScenesToMenu(_defaultScenes, "默认场景", menu);
            AddScenesToMenu(_otherScenes, "其他场景", menu);

            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft
            };

            if (GUILayout.Button(currentSceneName, buttonStyle, GUILayout.Width(buttonWidth)))
            {
                menu.ShowAsContext();
            }
        }

        private static void AddScenesToMenu(List<(string sceneName, string scenePath)> scenes, string category, GenericMenu menu)
        {
            foreach (var scene in scenes)
            {
                menu.AddItem(new GUIContent($"{category}/{scene.sceneName}"), false, () => SwitchScene(scene.scenePath));
            }
        }

        private static void SwitchScene(string scenePath)
        {
            if (SceneSwitcher.PromptSaveCurrentScene())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }
    }

    internal static class SceneSwitcher
    {
        public static bool PromptSaveCurrentScene()
        {
            if (!SceneManager.GetActiveScene().isDirty)
            {
                return true;
            }

            var saveScene = EditorUtility.DisplayDialog(
                "是否保存当前场景",
                "当前场景有未保存的更改，是否保存？",
                "保存",
                "取消");

            if (!saveScene)
            {
                return false;
            }

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            return true;
        }

        public static List<(string sceneName, string scenePath)> GetScenesInPath(string path)
        {
            var scenes = new List<(string sceneName, string scenePath)>();
            if (!AssetDatabase.IsValidFolder(path))
            {
                return scenes;
            }

            var guids = AssetDatabase.FindAssets("t:Scene", new[] { path });
            foreach (var guid in guids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                scenes.Add((sceneName, scenePath));
            }

            return scenes;
        }

        public static List<(string sceneName, string scenePath)> GetAllScenes()
        {
            var allScenes = new List<(string sceneName, string scenePath)>();
            var guids = AssetDatabase.FindAssets("t:Scene");
            foreach (var guid in guids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                allScenes.Add((sceneName, scenePath));
            }

            return allScenes;
        }
    }
}

#endif
