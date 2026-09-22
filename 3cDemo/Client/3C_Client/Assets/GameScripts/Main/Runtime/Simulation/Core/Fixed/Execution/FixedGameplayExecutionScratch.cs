using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedGameplayEffectExecutionScratch : IExecutionWorkspaceScratch
    {
        public List<PortableEffectRuntimeChange> Changes { get; } = new List<PortableEffectRuntimeChange>();
        public Dictionary<ulong, PortableEffectCause> Causes { get; } =
            new Dictionary<ulong, PortableEffectCause>();
        public NestedExecutionWorkspaceBuffer<PortableActiveEffectState> ActiveEffects { get; } =
            new NestedExecutionWorkspaceBuffer<PortableActiveEffectState>();
        public NestedExecutionWorkspaceBuffer<ulong> PredictionKeys { get; } =
            new NestedExecutionWorkspaceBuffer<ulong>();
        public NestedExecutionWorkspaceBuffer<string> PredictionAttributes { get; } =
            new NestedExecutionWorkspaceBuffer<string>();
        public NestedExecutionWorkspaceBuffer<SimulationSetByCallerValue> AdditionalSetByCallerValues { get; } =
            new NestedExecutionWorkspaceBuffer<SimulationSetByCallerValue>();
        public NestedExecutionWorkspaceBuffer<SimulationAttributeCapture> AdditionalSourceAttributes { get; } =
            new NestedExecutionWorkspaceBuffer<SimulationAttributeCapture>();
        public Dictionary<string, PortableAttributeBefore> AttributeBefore { get; } =
            new Dictionary<string, PortableAttributeBefore>(StringComparer.Ordinal);
        public Dictionary<string, FixedScalar> AttributeValues { get; } =
            new Dictionary<string, FixedScalar>(StringComparer.Ordinal);
        public Dictionary<string, FixedScalar> SuppliedSourceAttributes { get; } =
            new Dictionary<string, FixedScalar>(StringComparer.Ordinal);
        public HashSet<string> AttributeStack { get; } = new HashSet<string>(StringComparer.Ordinal);
        public List<PortableAttributeChange> RecalculatedAttributeChanges { get; } =
            new List<PortableAttributeChange>();
        public List<PortableAttributeChange> AttributeChanges { get; } =
            new List<PortableAttributeChange>();
        public HashSet<ulong> ActiveHandles { get; } = new HashSet<ulong>();
        public HashSet<ulong> ActiveInstances { get; } = new HashSet<ulong>();
        public List<GameplayEffectActiveIdentity> ActiveIdentities { get; } =
            new List<GameplayEffectActiveIdentity>();
        public List<string> OwnedTags { get; } = new List<string>();
        public List<string> CanonicalTags { get; } = new List<string>();

        FixedGameplayEffectTarget m_Target;

        internal FixedGameplayEffectTarget Target => m_Target ??= new FixedGameplayEffectTarget(this);

        public void Reset()
        {
            Changes.Clear();
            Causes.Clear();
            ActiveEffects.Reset();
            PredictionKeys.Reset();
            PredictionAttributes.Reset();
            AdditionalSetByCallerValues.Reset();
            AdditionalSourceAttributes.Reset();
            AttributeBefore.Clear();
            AttributeValues.Clear();
            SuppliedSourceAttributes.Clear();
            AttributeStack.Clear();
            RecalculatedAttributeChanges.Clear();
            AttributeChanges.Clear();
            ActiveHandles.Clear();
            ActiveInstances.Clear();
            ActiveIdentities.Clear();
            OwnedTags.Clear();
            CanonicalTags.Clear();
        }
    }

}
