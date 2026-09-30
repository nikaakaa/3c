using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonCharacter.Editor.ProductStartup
{
    public enum EditorPlayModeSceneLaunchResultCode : byte
    {
        Started = 0,
        Completed = 1,
        Cancelled = 2,
        RejectedBusy = 3,
        RejectedInvalidRequest = 4,
        Failed = 5
    }

    public enum EditorPlayModeSceneReloadResultCode : byte
    {
        Started = 0,
        RejectedNotPlaying = 1,
        RejectedNoPendingRequest = 2,
        RejectedOwnerMismatch = 3,
        RejectedInvalidRequest = 4,
        Failed = 5
    }

    public readonly struct EditorPlayModeSceneLaunchRequest
    {
        public EditorPlayModeSceneLaunchRequest(
            string scenePath,
            string contextId = "",
            Guid requestId = default,
            Action<Scene> prepare = null,
            bool startPaused = false,
            string ownerId = "")
        {
            ScenePath = Require(scenePath, nameof(scenePath));
            ContextId = contextId?.Trim() ?? string.Empty;
            RequestId = requestId == Guid.Empty ? Guid.NewGuid() : requestId;
            Prepare = prepare;
            StartPaused = startPaused;
            OwnerId = ownerId?.Trim() ?? string.Empty;
        }

        public string ScenePath { get; }
        public string ContextId { get; }
        public Guid RequestId { get; }
        public Action<Scene> Prepare { get; }
        public bool StartPaused { get; }
        public string OwnerId { get; }

        static string Require(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Scene launch path must be non-empty and trimmed.", parameterName);
            return value;
        }
    }

    public readonly struct EditorPlayModeSceneLaunchResult
    {
        public EditorPlayModeSceneLaunchResult(
            EditorPlayModeSceneLaunchResultCode code,
            Guid requestId,
            string scenePath,
            string message)
        {
            Code = code;
            RequestId = requestId;
            ScenePath = scenePath ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public EditorPlayModeSceneLaunchResultCode Code { get; }
        public Guid RequestId { get; }
        public string ScenePath { get; }
        public string Message { get; }
        public bool Accepted => Code == EditorPlayModeSceneLaunchResultCode.Started;
    }

    public readonly struct EditorPlayModeSceneReloadResult
    {
        public EditorPlayModeSceneReloadResult(
            EditorPlayModeSceneReloadResultCode code,
            string scenePath,
            AsyncOperation operation,
            string message)
        {
            Code = code;
            ScenePath = scenePath ?? string.Empty;
            Operation = operation;
            Message = message ?? string.Empty;
        }

        public EditorPlayModeSceneReloadResultCode Code { get; }
        public string ScenePath { get; }
        public AsyncOperation Operation { get; }
        public string Message { get; }
        public bool Accepted => Code == EditorPlayModeSceneReloadResultCode.Started;
    }

    [InitializeOnLoad]
    public static class EditorPlayModeSceneLauncher
    {
        const string PendingKey = "ThirdPerson.Launcher.Request.Pending";
        const string RequestIdKey = "ThirdPerson.Launcher.Request.Id";
        const string ScenePathKey = "ThirdPerson.Launcher.Request.ScenePath";
        const string ContextIdKey = "ThirdPerson.Launcher.Request.ContextId";
        const string StartPausedKey = "ThirdPerson.Launcher.Request.StartPaused";
        const string OwnerIdKey = "ThirdPerson.Launcher.Request.OwnerId";
        const string PlayModeEnteredKey = "ThirdPerson.Launcher.Request.PlayModeEntered";
        const string SceneSetupKey = "ThirdPerson.Launcher.Restore.SceneSetup";
        const string PlayModeStartSceneKey = "ThirdPerson.Launcher.Restore.PlayModeStartScene";

        static EditorPlayModeSceneLauncher()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static event Action<EditorPlayModeSceneLaunchResult> StateChanged;

        public static bool IsPending => SessionState.GetBool(PendingKey, false);

        public static EditorPlayModeSceneLaunchResult Start(
            EditorPlayModeSceneLaunchRequest request)
        {
            if (IsPending || EditorApplication.isPlayingOrWillChangePlaymode)
                return Result(
                    EditorPlayModeSceneLaunchResultCode.RejectedBusy,
                    request,
                    "A Scene launch is already active.");
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(request.ScenePath))
                return Result(
                    EditorPlayModeSceneLaunchResultCode.RejectedInvalidRequest,
                    request,
                    $"Launcher Scene is missing: {request.ScenePath}");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return Result(
                    EditorPlayModeSceneLaunchResultCode.Cancelled,
                    request,
                    "Scene launch was cancelled before saving the current Scene setup.");

            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            string previousPlayModeStartScene = AssetDatabase.GetAssetPath(
                EditorSceneManager.playModeStartScene);
            PersistRequest(request, setup, previousPlayModeStartScene);
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    request.ScenePath,
                    OpenSceneMode.Single);
                request.Prepare?.Invoke(scene);
                EditorSceneManager.playModeStartScene =
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(request.ScenePath);
                EditorApplication.EnterPlaymode();
                EditorPlayModeSceneLaunchResult result = Result(
                    EditorPlayModeSceneLaunchResultCode.Started,
                    request,
                    string.Empty);
                Publish(result);
                return result;
            }
            catch (Exception exception)
            {
                EditorPlayModeSceneLaunchResult result = Result(
                    EditorPlayModeSceneLaunchResultCode.Failed,
                    request,
                    exception.Message);
                try
                {
                    RestoreEnvironment();
                }
                catch (Exception restoreException)
                {
                    result = Result(
                        EditorPlayModeSceneLaunchResultCode.Failed,
                        request,
                        $"{exception.Message} Restore failed: {restoreException.Message}");
                }
                finally
                {
                    ClearRequest();
                }
                Publish(result);
                return result;
            }
        }

        public static EditorPlayModeSceneReloadResult ReloadInPlayMode(
            Guid requestId,
            string ownerId)
        {
            if (!EditorApplication.isPlaying)
                return new EditorPlayModeSceneReloadResult(
                    EditorPlayModeSceneReloadResultCode.RejectedNotPlaying,
                    string.Empty,
                    null,
                    "Scene reload requires an active Play session.");
            if (!IsPending)
                return new EditorPlayModeSceneReloadResult(
                    EditorPlayModeSceneReloadResultCode.RejectedNoPendingRequest,
                    string.Empty,
                    null,
                    "Scene reload has no pending Scene Play request.");
            EditorPlayModeSceneLaunchRequest request;
            try
            {
                request = ReadRequest();
            }
            catch (Exception exception)
            {
                return new EditorPlayModeSceneReloadResult(
                    EditorPlayModeSceneReloadResultCode.RejectedInvalidRequest,
                    string.Empty,
                    null,
                    exception.Message);
            }
            if (requestId == Guid.Empty ||
                request.RequestId != requestId ||
                string.IsNullOrWhiteSpace(ownerId) ||
                !string.Equals(request.OwnerId, ownerId.Trim(), StringComparison.Ordinal))
                return new EditorPlayModeSceneReloadResult(
                    EditorPlayModeSceneReloadResultCode.RejectedOwnerMismatch,
                    request.ScenePath,
                    null,
                    "Scene reload request identity or owner does not match the active request.");
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() ||
                !string.Equals(activeScene.path, request.ScenePath, StringComparison.Ordinal))
                return new EditorPlayModeSceneReloadResult(
                    EditorPlayModeSceneReloadResultCode.RejectedInvalidRequest,
                    request.ScenePath,
                    null,
                    "Scene reload target does not match the active Scene Play request.");
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(request.ScenePath))
                return new EditorPlayModeSceneReloadResult(
                    EditorPlayModeSceneReloadResultCode.RejectedInvalidRequest,
                    request.ScenePath,
                    null,
                    $"Reload Scene is missing: {request.ScenePath}");
            try
            {
                AsyncOperation operation = EditorSceneManager.LoadSceneAsyncInPlayMode(
                    request.ScenePath,
                    new LoadSceneParameters(LoadSceneMode.Single));
                return operation == null
                    ? new EditorPlayModeSceneReloadResult(
                        EditorPlayModeSceneReloadResultCode.Failed,
                        request.ScenePath,
                        null,
                        "Unity did not create a Scene reload operation.")
                    : new EditorPlayModeSceneReloadResult(
                        EditorPlayModeSceneReloadResultCode.Started,
                        request.ScenePath,
                        operation,
                        string.Empty);
            }
            catch (Exception exception)
            {
                return new EditorPlayModeSceneReloadResult(
                    EditorPlayModeSceneReloadResultCode.Failed,
                    request.ScenePath,
                    null,
                    exception.Message);
            }
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!IsPending)
                return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                SessionState.SetBool(PlayModeEnteredKey, true);
                return;
            }
            if (state == PlayModeStateChange.EnteredEditMode)
                CompleteLaunch();
        }

        static void CompleteLaunch()
        {
            EditorPlayModeSceneLaunchRequest request;
            EditorPlayModeSceneLaunchResult result;
            try
            {
                request = ReadRequest();
                RestoreEnvironment();
                bool enteredPlayMode = SessionState.GetBool(PlayModeEnteredKey, false);
                result = Result(
                    enteredPlayMode
                        ? EditorPlayModeSceneLaunchResultCode.Completed
                        : EditorPlayModeSceneLaunchResultCode.Failed,
                    request,
                    enteredPlayMode ? string.Empty : "Unity 未进入 Play Mode，ScenePlay 启动失败。");
            }
            catch (Exception exception)
            {
                request = ReadRequestOrDefault();
                result = Result(
                    EditorPlayModeSceneLaunchResultCode.Failed,
                    request,
                    exception.Message);
            }
            finally
            {
                ClearRequest();
            }
            Publish(result);
        }

        static void PersistRequest(
            EditorPlayModeSceneLaunchRequest request,
            SceneSetup[] sceneSetup,
            string previousPlayModeStartScene)
        {
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(PlayModeEnteredKey, false);
            SessionState.SetString(RequestIdKey, request.RequestId.ToString("N"));
            SessionState.SetString(ScenePathKey, request.ScenePath);
            SessionState.SetString(ContextIdKey, request.ContextId);
            SessionState.SetBool(StartPausedKey, request.StartPaused);
            SessionState.SetString(OwnerIdKey, request.OwnerId);
            SessionState.SetString(SceneSetupKey, SceneSetupJson.Serialize(sceneSetup));
            SessionState.SetString(PlayModeStartSceneKey, previousPlayModeStartScene ?? string.Empty);
        }

        static EditorPlayModeSceneLaunchRequest ReadRequest()
        {
            Guid requestId = Guid.TryParse(
                SessionState.GetString(RequestIdKey, string.Empty),
                out Guid parsed)
                ? parsed
                : Guid.Empty;
            string scenePath = SessionState.GetString(ScenePathKey, string.Empty);
            string contextId = SessionState.GetString(ContextIdKey, string.Empty);
            return new EditorPlayModeSceneLaunchRequest(
                scenePath,
                contextId,
                requestId,
                startPaused: SessionState.GetBool(StartPausedKey, false),
                ownerId: SessionState.GetString(OwnerIdKey, string.Empty));
        }

        public static bool TryGetPendingRequest(
            out EditorPlayModeSceneLaunchRequest request)
        {
            request = default;
            if (!IsPending)
                return false;
            try
            {
                request = ReadRequest();
                return true;
            }
            catch
            {
                return false;
            }
        }

        static EditorPlayModeSceneLaunchRequest ReadRequestOrDefault()
        {
            Guid requestId = Guid.TryParse(
                SessionState.GetString(RequestIdKey, string.Empty),
                out Guid parsed)
                ? parsed
                : Guid.NewGuid();
            string scenePath = SessionState.GetString(ScenePathKey, "missing");
            string contextId = SessionState.GetString(ContextIdKey, "missing");
            return new EditorPlayModeSceneLaunchRequest(
                scenePath,
                contextId,
                requestId,
                startPaused: SessionState.GetBool(StartPausedKey, false),
                ownerId: SessionState.GetString(OwnerIdKey, string.Empty));
        }

        static void RestoreEnvironment()
        {
            string previousPlayModeStartScene = SessionState.GetString(
                PlayModeStartSceneKey,
                string.Empty);
            SceneAsset previousSceneAsset = string.IsNullOrEmpty(previousPlayModeStartScene)
                ? null
                : AssetDatabase.LoadAssetAtPath<SceneAsset>(previousPlayModeStartScene);
            if (!string.IsNullOrEmpty(previousPlayModeStartScene) && !previousSceneAsset)
                throw new InvalidOperationException(
                    $"Previous Play Mode start Scene is missing: {previousPlayModeStartScene}");
            EditorSceneManager.playModeStartScene = previousSceneAsset;
            SceneSetup[] setup = SceneSetupJson.Deserialize(
                SessionState.GetString(SceneSetupKey, string.Empty));
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }

        static void ClearRequest()
        {
            SessionState.SetBool(PendingKey, false);
            SessionState.EraseString(RequestIdKey);
            SessionState.EraseString(ScenePathKey);
            SessionState.EraseString(ContextIdKey);
            SessionState.EraseBool(StartPausedKey);
            SessionState.EraseString(OwnerIdKey);
            SessionState.EraseBool(PlayModeEnteredKey);
            SessionState.EraseString(SceneSetupKey);
            SessionState.EraseString(PlayModeStartSceneKey);
        }

        static EditorPlayModeSceneLaunchResult Result(
            EditorPlayModeSceneLaunchResultCode code,
            EditorPlayModeSceneLaunchRequest request,
            string message) =>
            new EditorPlayModeSceneLaunchResult(
                code,
                request.RequestId,
                request.ScenePath,
                message);

        static void Publish(EditorPlayModeSceneLaunchResult result) =>
            StateChanged?.Invoke(result);

        [Serializable]
        sealed class SceneSetupJson
        {
            public Entry[] entries;

            public static string Serialize(SceneSetup[] setup)
            {
                var value = new SceneSetupJson
                {
                    entries = new Entry[setup?.Length ?? 0]
                };
                for (int i = 0; i < value.entries.Length; i++)
                {
                    SceneSetup current = setup[i];
                    value.entries[i] = new Entry
                    {
                        path = current.path ?? string.Empty,
                        isLoaded = current.isLoaded,
                        isActive = current.isActive
                    };
                }
                return JsonUtility.ToJson(value);
            }

            public static SceneSetup[] Deserialize(string json)
            {
                if (string.IsNullOrEmpty(json))
                    throw new InvalidOperationException("Scene launch restore setup is missing.");
                SceneSetupJson value = JsonUtility.FromJson<SceneSetupJson>(json);
                if (value?.entries == null || value.entries.Length == 0)
                    throw new InvalidOperationException("Scene launch restore setup is empty.");
                var result = new SceneSetup[value.entries.Length];
                for (int i = 0; i < result.Length; i++)
                {
                    Entry current = value.entries[i];
                    result[i] = new SceneSetup
                    {
                        path = current.path,
                        isLoaded = current.isLoaded,
                        isActive = current.isActive
                    };
                }
                return result;
            }

            [Serializable]
            public sealed class Entry
            {
                public string path;
                public bool isLoaded;
                public bool isActive;
            }
        }
    }
}
