using Mono.Cecil;

namespace ThirdPersonPerformance.Instrumentation.Editor
{
    internal readonly struct PerformanceInstrumentationProbeTarget
    {
        public PerformanceInstrumentationProbeTarget(
            ModuleDefinition module,
            MethodDefinition method,
            CustomAttribute probe)
        {
            Module = module;
            Method = method;
            Probe = probe;
        }

        public ModuleDefinition Module { get; }
        public MethodDefinition Method { get; }
        public CustomAttribute Probe { get; }
    }

    internal readonly struct PerformanceInstrumentationPointDescriptor
    {
        public PerformanceInstrumentationPointDescriptor(
            ulong pointId,
            ulong metricIdHash,
            string metricId,
            string declaringType,
            string method,
            string sourceFile,
            int sourceLine)
        {
            PointId = pointId;
            MetricIdHash = metricIdHash;
            MetricId = metricId;
            DeclaringType = declaringType;
            Method = method;
            SourceFile = sourceFile;
            SourceLine = sourceLine;
        }

        public ulong PointId { get; }
        public ulong MetricIdHash { get; }
        public string MetricId { get; }
        public string DeclaringType { get; }
        public string Method { get; }
        public string SourceFile { get; }
        public int SourceLine { get; }
    }
}
