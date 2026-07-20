using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal sealed class Float32EvaluationWorkspace
    {
        readonly object m_Gate = new object();
        readonly Float32GameplayEffectExecutionScratch m_GameplayEffects =
            new Float32GameplayEffectExecutionScratch();
        readonly Float32MotionExecutionScratch m_Motion = new Float32MotionExecutionScratch();
        readonly ActorExecutionWorkspace<
            GameplayFact,
            PresentationCommand,
            SimulationTraceRecord,
            TimelineSegment<Float32Scalar>,
            Float32GameplayEffectExecutionScratch,
            Float32MotionExecutionScratch> m_Shared;
        bool m_InUse;
        ExecutionWorkspaceLease m_SharedLease;
        ActorOutputWorkspaceLease m_OutputLease;

        public Float32EvaluationWorkspace(ProgramExecutionLayout layout)
        {
            StateTransactions = new Float32CharacterStateTransactionWorkspace(
                layout ?? throw new ArgumentNullException(nameof(layout)));
            m_Shared = new ActorExecutionWorkspace<
                GameplayFact,
                PresentationCommand,
                SimulationTraceRecord,
                TimelineSegment<Float32Scalar>,
                Float32GameplayEffectExecutionScratch,
                Float32MotionExecutionScratch>(m_GameplayEffects, m_Motion);
        }

        public List<GameplayFact> Facts => m_Shared.Facts.Values;
        public Float32CharacterStateTransactionWorkspace StateTransactions { get; }
        public List<PresentationCommand> Presentation => m_Shared.Presentation.Values;
        public List<SimulationTraceRecord> Trace => m_Shared.Trace.Values;
        public NestedExecutionWorkspaceBuffer<TimelineSegment<Float32Scalar>> TimelineSegments =>
            m_Shared.TimelineSegments;
        public Float32GameplayEffectExecutionScratch GameplayEffects => m_GameplayEffects;
        public HashSet<Float32ValueEvaluationKey> ValueStack { get; } = new HashSet<Float32ValueEvaluationKey>();
        public List<Float32ValueInputBuffer> ValueBuffers { get; } = new List<Float32ValueInputBuffer>();
        public List<SimulationMotionContribution> MotionContributions => m_Motion.Contributions;
        public List<MotionWarpSample<Float32Scalar>> MotionWarpSamples => m_Motion.WarpSamples;
        public List<SimulationActionWindowProjectionCandidate> ActionWindowProjections { get; } =
            new List<SimulationActionWindowProjectionCandidate>();
        public HashSet<string> ActionWindowProjectionKeys { get; } = new HashSet<string>(StringComparer.Ordinal);
        public Stack<SimulationTimelineBlackboardContext> TimelineBlackboardContexts { get; } =
            new Stack<SimulationTimelineBlackboardContext>();

        public ActorOutputWorkspaceLease Begin(
            ActorId actorId,
            SimulationTick tick,
            KernelProgramBinding binding)
        {
            lock (m_Gate)
            {
                if (m_InUse)
                    throw new InvalidOperationException("Float32 evaluation workspace is already in use for this Actor.");
                m_SharedLease = m_Shared.BeginEvaluation();
                m_InUse = true;
                ClearTargetScratch();
                m_OutputLease = new ActorOutputWorkspaceLease(actorId, tick, binding, m_SharedLease);
                return m_OutputLease;
            }
        }

        public void Require(ActorOutputWorkspaceLease lease)
        {
            lock (m_Gate)
                RequireCurrent(lease);
        }

        public void End(ActorOutputWorkspaceLease lease)
        {
            lock (m_Gate)
            {
                RequireCurrent(lease);
                ClearTargetScratch();
                m_Shared.EndEvaluation(m_SharedLease);
                m_SharedLease = default;
                m_OutputLease = default;
                m_InUse = false;
            }
        }

        void RequireCurrent(ActorOutputWorkspaceLease lease)
        {
            if (!m_InUse || !lease.IsValid ||
                lease.ActorId != m_OutputLease.ActorId ||
                lease.Tick != m_OutputLease.Tick ||
                !ReferenceEquals(lease.Binding, m_OutputLease.Binding) ||
                lease.Generation != m_OutputLease.Generation)
            {
                throw new InvalidOperationException("Float32 Actor output workspace lease is stale or belongs to another evaluation.");
            }
            m_Shared.Require(lease.WorkspaceLease);
        }

        void ClearTargetScratch()
        {
            ValueStack.Clear();
            for (int i = 0; i < ValueBuffers.Count; i++)
                ValueBuffers[i].Clear();
            ActionWindowProjections.Clear();
            ActionWindowProjectionKeys.Clear();
            TimelineBlackboardContexts.Clear();
        }
    }

    internal sealed class Float32GameplayEffectExecutionScratch : IExecutionWorkspaceScratch
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
        public Dictionary<string, Float32Scalar> AttributeValues { get; } =
            new Dictionary<string, Float32Scalar>(StringComparer.Ordinal);
        public Dictionary<string, Float32Scalar> SuppliedSourceAttributes { get; } =
            new Dictionary<string, Float32Scalar>(StringComparer.Ordinal);
        public HashSet<string> AttributeStack { get; } = new HashSet<string>(StringComparer.Ordinal);
        public List<PortableAttributeChange> RecalculatedAttributeChanges { get; } =
            new List<PortableAttributeChange>();
        public List<PortableAttributeChange> AttributeChanges { get; } =
            new List<PortableAttributeChange>();
        public HashSet<ulong> ActiveHandles { get; } = new HashSet<ulong>();
        public HashSet<ulong> ActiveInstances { get; } = new HashSet<ulong>();
        public List<GameplayEffectActiveIdentity> ActiveIdentities { get; } =
            new List<GameplayEffectActiveIdentity>();
        public SortedSet<string> OwnedTagSet { get; } = new SortedSet<string>(StringComparer.Ordinal);
        public List<string> OwnedTags { get; } = new List<string>();
        public List<string> CanonicalTags { get; } = new List<string>();

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
            OwnedTagSet.Clear();
            OwnedTags.Clear();
            CanonicalTags.Clear();
        }
    }

    internal sealed class Float32MotionExecutionScratch : IExecutionWorkspaceScratch
    {
        public List<SimulationMotionContribution> Contributions { get; } =
            new List<SimulationMotionContribution>();
        public List<MotionWarpSample<Float32Scalar>> WarpSamples { get; } =
            new List<MotionWarpSample<Float32Scalar>>();

        public void Reset()
        {
            Contributions.Clear();
            WarpSamples.Clear();
        }
    }
}
