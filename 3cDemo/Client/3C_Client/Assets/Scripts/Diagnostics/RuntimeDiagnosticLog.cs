using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace ThirdPersonDiagnostics
{
    public static class RuntimeDiagnosticLog
    {
        static readonly RuntimeDiagnosticLogFilter filter = new RuntimeDiagnosticLogFilter();
        static RuntimeDiagnosticLogLevel minimumUnityLogLevel = RuntimeDiagnosticLogLevel.Info;

        public static event Action<RuntimeDiagnosticLogEvent> EventSubmitted;

        public static RuntimeDiagnosticLogFilter Filter => filter;
        public static RuntimeDiagnosticLogLevel MinimumUnityLogLevel
        {
            get => minimumUnityLogLevel;
            set => minimumUnityLogLevel = value;
        }

        public static void RegisterChannel(string channelKey, bool defaultEnabled = true)
        {
            filter.RegisterChannel(channelKey, defaultEnabled);
        }

        [Conditional("THIRDPERSON_DIAGNOSTIC_LOGS")]
        [Conditional("UNITY_EDITOR")]
        [Conditional("UNITY_INCLUDE_TESTS")]
        public static void Submit(RuntimeDiagnosticLogEvent diagnosticEvent)
        {
            if (!filter.ShouldEmit(in diagnosticEvent))
                return;

            EventSubmitted?.Invoke(diagnosticEvent);
            EmitToUnity(diagnosticEvent);
        }

        public static void ReportWarning(string message, UnityEngine.Object context = null)
        {
            UnityEngine.Debug.LogWarning(message, context);
        }

        public static void ReportError(string message, UnityEngine.Object context = null)
        {
            UnityEngine.Debug.LogError(message, context);
        }

        public static string Format(in RuntimeDiagnosticLogEvent diagnosticEvent)
        {
            StringBuilder builder = new StringBuilder(160);
            builder.Append("[3C-DIAG][");
            builder.Append(diagnosticEvent.Level);
            builder.Append("][");
            builder.Append(diagnosticEvent.Category);
            builder.Append("][");
            builder.Append(diagnosticEvent.ChannelKey);
            builder.Append("] frame=");
            builder.Append(diagnosticEvent.Frame);
            builder.Append(" step=");
            builder.Append(diagnosticEvent.Step);

            if (diagnosticEvent.HasPreviousStatePath)
            {
                builder.Append(" from=");
                builder.Append(diagnosticEvent.PreviousStatePath);
            }

            if (diagnosticEvent.HasStatePath)
            {
                builder.Append(" path=");
                builder.Append(diagnosticEvent.StatePath);
            }

            if (!string.IsNullOrEmpty(diagnosticEvent.Message))
            {
                builder.Append(" message=");
                builder.Append(diagnosticEvent.Message);
            }

            if (diagnosticEvent.HasContext)
            {
                builder.Append(" ");
                builder.Append(diagnosticEvent.Context);
            }

            return builder.ToString();
        }

        public static IDisposable Capture(Action<RuntimeDiagnosticLogEvent> receiver)
        {
            return new CaptureScope(receiver);
        }

        public static void Reset()
        {
            filter.Reset();
            minimumUnityLogLevel = RuntimeDiagnosticLogLevel.Info;
        }

        [Conditional("THIRDPERSON_DIAGNOSTIC_LOGS")]
        [Conditional("UNITY_EDITOR")]
        static void EmitToUnity(RuntimeDiagnosticLogEvent diagnosticEvent)
        {
            if (diagnosticEvent.Level < minimumUnityLogLevel)
                return;

            string formatted = Format(in diagnosticEvent);
            if (diagnosticEvent.Level == RuntimeDiagnosticLogLevel.Error)
            {
                UnityEngine.Debug.LogError(formatted);
                return;
            }

            if (diagnosticEvent.Level == RuntimeDiagnosticLogLevel.Warning)
            {
                UnityEngine.Debug.LogWarning(formatted);
                return;
            }

            UnityEngine.Debug.Log(formatted);
        }

        sealed class CaptureScope : IDisposable
        {
            readonly Action<RuntimeDiagnosticLogEvent> receiver;

            public CaptureScope(Action<RuntimeDiagnosticLogEvent> receiver)
            {
                this.receiver = receiver;
                EventSubmitted += receiver;
            }

            public void Dispose()
            {
                EventSubmitted -= receiver;
            }
        }
    }
}
