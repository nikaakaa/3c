using System;
using ThirdPersonCharacter.Editor.ProductStartup;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    public sealed class GameplayTestLauncherWindow : EditorWindow
    {
        const string StandalonePrefab = "Assets/Prefabs/Characters/RuntimeProfiles/CorinStandalonePlayer.prefab";
        const string StandaloneScene = "Assets/Scenes/Standalone/StandaloneGameplay.unity";
        const string DeterministicPrefab = "Assets/Prefabs/Characters/RuntimeProfiles/CorinDeterministicRollback.prefab";
        const string DeterministicScene = "Assets/Scenes/DeterministicRollback/DeterministicRollbackPeer.unity";
        const string UnityClientPrefab = "Assets/Prefabs/Characters/RuntimeProfiles/CorinServerAuthoritativeUnityClient.prefab";
        const string UnityWorkerPrefab = "Assets/Prefabs/Characters/RuntimeProfiles/CorinServerAuthoritativeUnityAuthorityBody.prefab";
        const string UnityClientScene = "Assets/Scenes/ServerAuthoritative/ServerAuthoritativeClient.unity";
        const string UnityWorkerScene = "Assets/Scenes/ServerAuthoritative/ServerAuthoritativeAuthorityWorker.unity";
        const string DotRecastClientPrefab = "Assets/Prefabs/Characters/RuntimeProfiles/CorinServerAuthoritativeDotRecastClient.prefab";
        const string DotRecastClientScene = "Assets/Scenes/ServerAuthoritative/DotRecastAuthorityClient.unity";

        [MenuItem("Tools/3C/Test Launcher")]
        public static void Open()
        {
            GameplayTestLauncherWindow window = GetWindow<GameplayTestLauncherWindow>("3C Test Launcher");
            window.minSize = new Vector2(520f, 310f);
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(8f);
            DrawStandalone();
            DrawDeterministic();
            DrawUnityAuthority();
            DrawDotRecastAuthority();
        }

        static void DrawStandalone()
        {
            EditorGUILayout.LabelField("Standalone / Unity CharacterController", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Prefab")) OpenAsset(StandalonePrefab);
                if (GUILayout.Button("Scene")) OpenScene(StandaloneScene);
                if (GUILayout.Button("Play")) Execute(LocalGameplayPlayModeLauncher.PlayStandaloneGameplay);
            }
            EditorGUILayout.Space(8f);
        }

        static void DrawDeterministic()
        {
            EditorGUILayout.LabelField("Deterministic Rollback / Deterministic KCC", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Prefab")) OpenAsset(DeterministicPrefab);
                if (GUILayout.Button("Scene")) OpenScene(DeterministicScene);
                if (GUILayout.Button("Build")) Execute(DeterministicRollbackNetworkTestBuildAndRun.Build);
                if (GUILayout.Button("Run")) Execute(DeterministicRollbackNetworkTestBuildAndRun.Run);
            }
            EditorGUILayout.Space(8f);
        }

        static void DrawUnityAuthority()
        {
            EditorGUILayout.LabelField("Server Authoritative / Unity Solver", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Client Prefab")) OpenAsset(UnityClientPrefab);
                if (GUILayout.Button("Worker Prefab")) OpenAsset(UnityWorkerPrefab);
                if (GUILayout.Button("Client Scene")) OpenScene(UnityClientScene);
                if (GUILayout.Button("Worker Scene")) OpenScene(UnityWorkerScene);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build")) Execute(UnityAuthorityNetworkTestBuildAndRun.Build);
                if (GUILayout.Button("Run")) Execute(UnityAuthorityNetworkTestBuildAndRun.Run);
            }
            EditorGUILayout.Space(8f);
        }

        static void DrawDotRecastAuthority()
        {
            EditorGUILayout.LabelField("Server Authoritative / DotRecast Solver", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Client Prefab")) OpenAsset(DotRecastClientPrefab);
                if (GUILayout.Button("Client Scene")) OpenScene(DotRecastClientScene);
                if (GUILayout.Button("Build")) Execute(DotRecastAuthorityNetworkTestBuildAndRun.Build);
                if (GUILayout.Button("Run")) Execute(DotRecastAuthorityNetworkTestBuildAndRun.Run);
            }
        }

        static void OpenAsset(string path)
        {
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (!asset)
                throw new InvalidOperationException($"Test asset is missing: {path}");
            AssetDatabase.OpenAsset(asset);
        }

        static void OpenScene(string path)
        {
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(path))
                throw new InvalidOperationException($"Test scene is missing: {path}");
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(path);
        }

        static void Execute(Action action)
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("3C Test Launcher", exception.Message, "OK");
            }
        }
    }
}
