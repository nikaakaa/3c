using UnityEditor;
using UnityEngine;

namespace TEngine
{
    [InitializeOnLoad]
    internal static class PlayModeErrorAutoExit
    {
        private const string ExitLog = "[PlayModeErrorAutoExit] 运行时报错，自动退出 Play 模式";
        private const string MenuPath = "Tools/3C/调试/报错时自动退出 Play";
        private static readonly string PreferenceKey = "TEngine.PlayModeErrorAutoExit.Enabled:" + Application.dataPath;
        private static volatile bool _enabled;
        private static volatile bool _exitRequested;
        private static volatile bool _inPlayMode;
        private static volatile bool _exitStarted;

        static PlayModeErrorAutoExit()
        {
            _enabled = EditorPrefs.GetBool(PreferenceKey, true);
            _inPlayMode = EditorApplication.isPlayingOrWillChangePlaymode;
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
            EditorApplication.update += OnUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem(MenuPath)]
        private static void ToggleEnabled()
        {
            _enabled = !_enabled;
            _exitRequested = false;
            EditorPrefs.SetBool(PreferenceKey, _enabled);
            Menu.SetChecked(MenuPath, _enabled);
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateToggleEnabled()
        {
            Menu.SetChecked(MenuPath, _enabled);
            return true;
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (!_enabled || !_inPlayMode || _exitStarted || (type != LogType.Error && type != LogType.Exception))
            {
                return;
            }

            if (IsTransportLog(condition, stackTrace))
            {
                return;
            }

            _exitRequested = true;
        }

        private static void OnUpdate()
        {
            if (!_enabled || !_exitRequested || !_inPlayMode || _exitStarted || !EditorApplication.isPlaying)
            {
                return;
            }

            _exitRequested = false;
            _exitStarted = true;
            Debug.Log(ExitLog);
            EditorApplication.ExitPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                _exitRequested = false;
                _exitStarted = false;
                _inPlayMode = true;
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _inPlayMode = true;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode ||
                     state == PlayModeStateChange.EnteredEditMode)
            {
                _exitRequested = false;
                _exitStarted = false;
                _inPlayMode = false;
            }
        }

        private static bool IsTransportLog(string condition, string stackTrace)
        {
            return ContainsTransportMarker(condition) || ContainsTransportMarker(stackTrace);
        }

        private static bool ContainsTransportMarker(string value)
        {
            return !string.IsNullOrEmpty(value) &&
                (value.Contains("MCPForUnity") ||
                 value.Contains("MCP-FOR-UNITY") ||
                 value.Contains("WebSocketTransportClient") ||
                 value.Contains("HttpBridgeReloadHandler"));
        }
    }
}
