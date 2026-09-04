using System;
using System.Diagnostics;
using Unity.Profiling;

namespace ThirdPersonPerformance.Instrumentation
{
    public struct PerformanceProbeScope
    {
        internal ProfilerMarker Marker;
        internal PerformanceInstrumentationContext Context;
        internal ulong PointId;
        internal ulong MetricId;
        internal long StartedAt;
        internal int ThreadId;
        internal bool MarkerActive;
        internal bool SpanActive;

        internal bool IsActive => MarkerActive || SpanActive;
    }

    public static class PerformanceInstrumentationRuntime
    {
        public static PerformanceInstrumentationMode Mode => PerformanceInstrumentationSpanRuntime.Mode;
        public static bool IsCapturing => PerformanceInstrumentationSpanRuntime.IsCapturing;
        public static bool Faulted => PerformanceInstrumentationSpanRuntime.Faulted;
        public static PerformanceSpanBuffer SpanBuffer => PerformanceInstrumentationSpanRuntime.SpanBuffer;

        public static void Configure(
            PerformanceInstrumentationMode mode,
            PerformanceSpanBuffer buffer) =>
            PerformanceInstrumentationSpanRuntime.Configure(mode, buffer);

        public static void BeginCapture(int contextDepth = 32) =>
            PerformanceInstrumentationSpanRuntime.BeginCapture(contextDepth);

        public static PerformanceSpanRecord[] EndCapture() =>
            PerformanceInstrumentationSpanRuntime.EndCapture();

        public static void CancelCapture() =>
            PerformanceInstrumentationSpanRuntime.CancelCapture();

        public static PerformanceProbeScope EnterMarkerOnly(
            ulong pointId,
            ProfilerMarker marker)
        {
            if (!IsCapturing || Mode != PerformanceInstrumentationMode.MarkerOnly)
                return default(PerformanceProbeScope);
            marker.Begin();
            return new PerformanceProbeScope
            {
                Marker = marker,
                MarkerActive = true,
                PointId = pointId
            };
        }

        public static PerformanceProbeScope EnterSpan(
            ulong pointId,
            ulong metricId,
            ProfilerMarker marker)
        {
            if (!IsCapturing || Mode != PerformanceInstrumentationMode.Span)
                return default(PerformanceProbeScope);
            marker.Begin();
            return new PerformanceProbeScope
            {
                Marker = marker,
                Context = PerformanceInstrumentationContextRuntime.Current,
                PointId = pointId,
                MetricId = metricId,
                StartedAt = Stopwatch.GetTimestamp(),
                ThreadId = Environment.CurrentManagedThreadId,
                MarkerActive = true,
                SpanActive = true
            };
        }

        public static void Exit(ref PerformanceProbeScope scope, PerformanceProbeEndState endState)
        {
            if (!scope.IsActive)
                return;
            long completedAt = scope.SpanActive ? Stopwatch.GetTimestamp() : 0L;
            if (scope.MarkerActive)
                scope.Marker.End();
            if (scope.SpanActive)
            {
                PerformanceInstrumentationSpanRuntime.TryWrite(
                    scope.PointId,
                    scope.MetricId,
                    in scope.Context,
                    scope.StartedAt,
                    completedAt - scope.StartedAt,
                    scope.ThreadId,
                    endState);
            }
            scope = default(PerformanceProbeScope);
        }
    }
}
