using UnityEditor;
using UnityEngine;

namespace TEngine
{
    [InitializeOnLoad]
    internal static class PlayModeErrorAutoExit
    {
        private static volatile bool _exitRequested;

        static PlayModeErrorAutoExit()
        {
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
            EditorApplication.update += OnUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception)
            {
                return;
            }

            _exitRequested = true;
        }

        private static void OnUpdate()
        {
            if (!_exitRequested || !EditorApplication.isPlaying)
            {
                return;
            }

            _exitRequested = false;
            Debug.Log("[PlayModeErrorAutoExit] 运行时报错，自动退出 Play 模式");
            EditorApplication.ExitPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _exitRequested = false;
            }
        }
    }
}