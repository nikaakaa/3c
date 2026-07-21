using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonCharacter.Editor.ProductStartup
{
    [InitializeOnLoad]
    public static class EditorPlayModeSceneLauncher
    {
        const string RestoreSceneKey = "ThirdPerson.Launcher.RestoreScene";
        const string RestorePendingKey = "ThirdPerson.Launcher.RestorePending";

        static EditorPlayModeSceneLauncher()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static void Play(string scenePath, Action<Scene> prepare = null)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("A Scene launch is already in progress.");
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath))
                throw new InvalidOperationException($"Launcher Scene is missing: {scenePath}");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            string previousScene = SceneManager.GetActiveScene().path;
            SessionState.SetString(RestoreSceneKey, previousScene ?? string.Empty);
            SessionState.SetBool(RestorePendingKey, true);
            try
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                prepare?.Invoke(scene);
                EditorApplication.EnterPlaymode();
            }
            catch
            {
                ClearRestoreState();
                RestoreScene(previousScene);
                throw;
            }
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode ||
                !SessionState.GetBool(RestorePendingKey, false))
            {
                return;
            }

            string previousScene = SessionState.GetString(RestoreSceneKey, string.Empty);
            ClearRestoreState();
            RestoreScene(previousScene);
        }

        static void RestoreScene(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath) ||
                !AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) ||
                string.Equals(SceneManager.GetActiveScene().path, scenePath, StringComparison.Ordinal))
            {
                return;
            }
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        static void ClearRestoreState()
        {
            SessionState.SetBool(RestorePendingKey, false);
            SessionState.EraseString(RestoreSceneKey);
        }
    }
}
