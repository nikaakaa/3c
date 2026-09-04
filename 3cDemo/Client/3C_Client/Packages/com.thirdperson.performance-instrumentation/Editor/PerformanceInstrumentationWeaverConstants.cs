namespace ThirdPersonPerformance.Instrumentation.Editor
{
    internal static class PerformanceInstrumentationWeaverConstants
    {
        public const string ProbeAttributeName =
            "ThirdPersonPerformance.Instrumentation.PerformanceProbeAttribute";
        public const string RuntimeNamespace = "ThirdPersonPerformance.Instrumentation";
        public const string RuntimeAssemblyName =
            "ThirdPerson.Performance.Instrumentation.Runtime";
        public const string RuntimeTypeName = "PerformanceInstrumentationRuntime";
        public const string ScopeTypeName = "PerformanceProbeScope";
        public const string EndStateTypeName = "PerformanceProbeEndState";
        public const string MarkerNamespace = "Unity.Profiling";
        public const string MarkerTypeName = "ProfilerMarker";
        public const string GeneratedFieldPrefix = "__thirdPersonPerformanceMarker_";
        public const string GeneratedMethodPrefix = "__thirdPersonPerformanceBody_";

        public static ulong Hash64(string value)
        {
            ulong hash = 14695981039346656037UL;
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 1099511628211UL;
            }
            return hash;
        }
    }
}
