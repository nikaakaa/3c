#if !UNITY_6000_3_OR_NEWER

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TEngine
{
    public partial class UnityToolbarExtenderLeft
    {
        private const string PreviousSceneKey = "TEngine_PreviousScenePath";
        private const string IsLauncherBtn = "TEngine_IsLauncher";
        private const string BootstrapScene = "Bootstrap";
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
            if (GUILayout.Button(new GUIContent("Launcher", EditorGUIUtility.FindTexture("PlayButton"), "Start TEngine Bootstrap"), _buttonGuiStyle))
            {
                SceneHelper.StartScene(BootstrapScene);
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            var previousScenePath = EditorPrefs.GetString(PreviousSceneKey, string.Empty);
            if (!string.IsNullOrEmpty(previousScenePath) && EditorPrefs.GetBool(IsLauncherBtn))
            {
                EditorApplication.delayCall += () =>
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(previousScenePath);
                    }
                };
            }

            EditorPrefs.SetBool(IsLauncherBtn, false);
        }

        private static void OnEditorQuit()
        {
            EditorPrefs.SetString(PreviousSceneKey, string.Empty);
            EditorPrefs.SetBool(IsLauncherBtn, false);
        }

        private static class SceneHelper
        {
            private static string _sceneToOpen;

            public static void StartScene(string sceneName)
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = false;
                }

                var activeScene = SceneManager.GetActiveScene();
                if (activeScene.isLoaded && activeScene.name != sceneName && !string.IsNullOrEmpty(activeScene.path))
                {
                    EditorPrefs.SetString(PreviousSceneKey, activeScene.path);
                    EditorPrefs.SetBool(IsLauncherBtn, true);
                }

                _sceneToOpen = sceneName;
                EditorApplication.update += OnUpdate;
            }

            private static void OnUpdate()
            {
                if (string.IsNullOrEmpty(_sceneToOpen) ||
                    EditorApplication.isPlaying ||
                    EditorApplication.isPaused ||
                    EditorApplication.isCompiling ||
                    EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                EditorApplication.update -= OnUpdate;

                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    var scenePath = FindScenePath(_sceneToOpen);
                    if (string.IsNullOrEmpty(scenePath))
                    {
                        Debug.LogWarning("Couldn't find scene file");
                    }
                    else
                    {
                        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                        EditorApplication.isPlaying = true;
                    }
                }

                _sceneToOpen = null;
            }

            private static string FindScenePath(string sceneName)
            {
                var guids = AssetDatabase.FindAssets("t:scene " + sceneName, null);
                foreach (var guid in guids)
                {
                    var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                    if (scenePath.EndsWith("/" + sceneName + ".unity"))
                    {
                        return scenePath;
                    }
                }

                return guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : null;
            }
        }
    }
}

#endif
