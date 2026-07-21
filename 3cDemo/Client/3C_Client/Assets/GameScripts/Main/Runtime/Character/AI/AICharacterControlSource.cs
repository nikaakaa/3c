using System;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Diagnostics;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.AI
{
    [DisallowMultipleComponent]
    public sealed class AICharacterControlSource : CharacterControlSource
    {
        [SerializeField] AIControllerDefinition m_Controller;

        public AIControllerDefinition Controller => m_Controller;
        public override string SourceIdentity => m_Controller
            ? $"ai-controller/{m_Controller.ControllerId}"
            : "ai-controller/unbound";

        public override IUnityCharacterControlSourceRuntime Create(CharacterControlSourceContext context)
        {
            if (!m_Controller)
                throw new InvalidOperationException("AI Character Control Source requires an AI Controller Definition.");
            if (m_Controller.ControlledCharacter != context.Definition)
                throw new InvalidOperationException("AI Controller controlled Character Definition does not match its Character host.");
            if (!m_Controller.PerceptionProfile)
                throw new InvalidOperationException("AI Controller has no Perception Profile.");
            if (!m_Controller.IntentProgram)
                throw new InvalidOperationException("AI Controller has no generated AI Intent Program.");
            AIIntentProgram aiProgram = m_Controller.IntentProgram.Load();
            string[] configured = new string[m_Controller.PerceptionProfile.CandidateActorIds.Count];
            for (int i = 0; i < configured.Length; i++)
                configured[i] = m_Controller.PerceptionProfile.CandidateActorIds[i];
            var actorIds = new ActorId[configured.Length];
            for (int i = 0; i < actorIds.Length; i++)
                actorIds[i] = new ActorId(configured[i]);
            var perception = new AIPerceptionDescriptor(
                actorIds,
                m_Controller.PerceptionProfile.Ordering == AICandidateOrdering.DistanceThenActorId);
            string expectedSourceRevision = AIControllerSourceRevision.Compute(
                m_Controller,
                context.Program.Manifest.ProgramId,
                context.Program.ProgramHash,
                perception.SchemaHash);
            if (!string.Equals(aiProgram.SemanticIr.SourceRevision, expectedSourceRevision, StringComparison.Ordinal))
                throw new InvalidOperationException("AI Controller generated Program is stale for its current Tree, Perception or Character Program.");
            CharacterRuntimeDebugProgram debugProgram = CharacterRuntimeDebugProgramBuilder.Build(
                aiProgram.ProgramId.Value,
                aiProgram.SemanticIr.SourceRevision,
                aiProgram.ProgramHash.ToString(),
                aiProgram.SourceMap);
            var diagnosticsContext = new RuntimeDiagnosticsContext(
                Guid.NewGuid(),
                Guid.NewGuid(),
                debugProgram.Revision,
                debugProgram.SourceMap,
                new RuntimeDiagnosticsStore());
            var diagnosticsTarget = new RuntimeDiagnosticsTarget(
                $"{context.Owner.name} / AI {m_Controller.ControllerId}",
                context.Owner.GetInstanceID(),
                diagnosticsContext);
            return new UnityAICharacterControlSourceRuntime(
                new Float32AIControlSourceRuntime(
                    new ActorId(context.Owner.ActorId),
                    aiProgram,
                    context.Program,
                    perception),
                new AIControllerRuntimeDiagnostics(diagnosticsContext, aiProgram),
                diagnosticsTarget);
        }
    }

    sealed class UnityAICharacterControlSourceRuntime :
        IUnityCharacterControlSourceRuntime,
        ICharacterControlSourceStateRuntime,
        ICharacterControlSourceTransactionObserver,
        ICharacterControlSourceRosterRuntime
    {
        readonly Float32AIControlSourceRuntime m_Runtime;
        readonly AIControllerRuntimeDiagnostics m_Diagnostics;
        readonly RuntimeDiagnosticsTarget m_DiagnosticsTarget;
        bool m_DiagnosticsRegistered;
        bool m_Active;
        bool m_Disposed;

        public UnityAICharacterControlSourceRuntime(
            Float32AIControlSourceRuntime runtime,
            AIControllerRuntimeDiagnostics diagnostics,
            RuntimeDiagnosticsTarget diagnosticsTarget)
        {
            m_Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            m_Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            m_DiagnosticsTarget = diagnosticsTarget ?? throw new ArgumentNullException(nameof(diagnosticsTarget));
        }

        public string SourceIdentity => m_Runtime.SourceIdentity;
        public SimulationNumericProfile NumericProfile => m_Runtime.NumericProfile;
        public ProgramId CharacterProgramId => m_Runtime.CharacterProgramId;
        public ProgramHash CharacterProgramHash => m_Runtime.CharacterProgramHash;
        public CharacterControlSourceCapability Capabilities => m_Runtime.Capabilities;
        public string StateSchemaId => m_Runtime.StateSchemaId;
        public int StateSchemaVersion => m_Runtime.StateSchemaVersion;

        public void Activate()
        {
            RequireAlive();
            if (m_Active)
                return;
            RuntimeDiagnosticsTargetRegistry.Register(m_DiagnosticsTarget);
            m_DiagnosticsRegistered = true;
            m_Active = true;
        }

        public void Deactivate()
        {
            m_Active = false;
            if (m_DiagnosticsRegistered)
            {
                RuntimeDiagnosticsTargetRegistry.Unregister(m_DiagnosticsTarget);
                m_DiagnosticsRegistered = false;
            }
        }

        public void CaptureRenderFrame(ulong renderFrame)
        {
            RequireAlive();
            if (!m_Active || renderFrame == 0)
                throw new InvalidOperationException("AI Control Source requires an active render-frame capture lifecycle.");
        }

        public CharacterSimulationInput BuildInput(SimulationInputBuildContext context)
        {
            RequireAlive();
            if (!m_Active)
                throw new InvalidOperationException("AI Control Source is not active.");
            CharacterSimulationInput input = m_Runtime.BuildInput(context);
            m_Diagnostics.Publish(m_Runtime.LatestDiagnostics);
            return input;
        }

        public byte[] CaptureState() => m_Runtime.CaptureState();
        public void RestoreState(byte[] state)
        {
            m_Runtime.RestoreState(state);
            PublishDiagnostics();
        }

        public void NotifyStateDisposition(CharacterControlSourceStateDisposition disposition)
        {
            m_Runtime.NotifyStateDisposition(disposition);
            PublishDiagnostics();
        }

        public void ValidateRoster(
            ActorId actorId,
            System.Collections.Generic.IReadOnlyList<ActorId> roster,
            StableHash committedObservationCapability)
        {
            m_Runtime.ValidateRoster(actorId, roster, committedObservationCapability);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Deactivate();
            m_DiagnosticsTarget.Terminate();
            m_DiagnosticsTarget.Dispose();
            m_Disposed = true;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(UnityAICharacterControlSourceRuntime));
        }

        void PublishDiagnostics()
        {
            if (m_Active)
                m_Diagnostics.Publish(m_Runtime.LatestDiagnostics);
        }
    }

    sealed class AIControllerRuntimeDiagnostics
    {
        readonly RuntimeDiagnosticsContext m_Context;
        readonly AIIntentProgram m_Program;

        public AIControllerRuntimeDiagnostics(RuntimeDiagnosticsContext context, AIIntentProgram program)
        {
            m_Context = context ?? throw new ArgumentNullException(nameof(context));
            m_Program = program ?? throw new ArgumentNullException(nameof(program));
        }

        public void Publish(AIDiagnosticsSnapshot snapshot)
        {
            if (snapshot == null || snapshot.ActiveOperation < 0)
                return;
            var target = new RuntimeSourceTarget(RuntimeSourceTargetKind.Operation, snapshot.ActiveOperation);
            if (!m_Context.SourceMap.TryGetProgramTarget(target, out RuntimeSourceElementHandle source))
                throw new InvalidOperationException($"AI diagnostics operation '{snapshot.ActiveOperation}' is absent from its Source Map.");
            m_Context.BeginLogicTick(snapshot.SourceTick);
            m_Context.Publish(
                RuntimeTraceChannel.Graph,
                RuntimeTraceDomain.Logic,
                RuntimeTraceEventKind.NodeStatus,
                source,
                RuntimeInstanceKey.Runnable(
                    m_Context.CharacterRuntimeId,
                    m_Context.SessionId,
                    snapshot.ActiveOperation.ToString(),
                    snapshot.InputSequence == 0 ? 1UL : snapshot.InputSequence),
                new RuntimeTracePayload
                {
                    Status = snapshot.StateDisposition,
                    Name = snapshot.ActiveNodePath,
                    Detail = $"observation={snapshot.ObservationTick}; candidates={snapshot.CandidateSummary}; target={snapshot.SelectedTarget}; reads={snapshot.MemoryReads}; writes={snapshot.MemoryWrites}; inputs={snapshot.WrittenInputs}; requests={snapshot.SubmittedRequests}",
                    OwnerId = snapshot.ActorId.Value,
                    RelatedElementId = m_Program.ProgramId.Value,
                    Flag = true,
                    Value = DebugValueSnapshot.Capture(snapshot.InputSequence)
                });
        }
    }
}
