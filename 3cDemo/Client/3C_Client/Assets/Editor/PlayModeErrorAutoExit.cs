using UnityEditor;
using UnityEngine;

namespace TEngine
{
    [InitializeOnLoad]
    internal static class PlayModeErrorAutoExit
    {
        private const string ExitLog = "[PlayModeErrorAutoExit] 运行时报错，自动退出 Play 模式";
        private static volatile bool _exitRequested;
        private static volatile bool _inPlayMode;
        private static volatile bool _exitStarted;

        static PlayModeErrorAutoExit()
        {
            _inPlayMode = EditorApplication.isPlayingOrWillChangePlaymode;
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
            EditorApplication.update += OnUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (!_inPlayMode || _exitStarted || (type != LogType.Error && type != LogType.Exception))
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
            if (!_exitRequested || !_inPlayMode || _exitStarted || !EditorApplication.isPlaying)
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
