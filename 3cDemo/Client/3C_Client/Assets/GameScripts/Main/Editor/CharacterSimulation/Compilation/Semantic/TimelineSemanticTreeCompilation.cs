using System;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public readonly struct TimelineSemanticTreeCompilation
    {
        readonly Func<string, OperationHandle> m_LifecycleResolver;

        public TimelineSemanticTreeCompilation(
            string route,
            OperationHandle entry,
            Func<string, OperationHandle> lifecycleResolver)
        {
            Route = route ?? string.Empty;
            Entry = entry;
            m_LifecycleResolver = lifecycleResolver;
        }

        public string Route { get; }
        public OperationHandle Entry { get; }
        public bool IsValid => Entry.IsValid;

        public OperationHandle Lifecycle(string port)
        {
            return m_LifecycleResolver == null
                ? OperationHandle.Invalid
                : m_LifecycleResolver(port);
        }
    }
}
