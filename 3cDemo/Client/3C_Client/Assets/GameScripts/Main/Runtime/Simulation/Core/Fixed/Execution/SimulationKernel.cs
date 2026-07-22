using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class KernelProgramBinding
    {
        internal KernelProgramBinding(
            CharacterSimulationProgram program,
            ProgramExecutionLayout layout,
            SimulationKernel kernel)
        {
            Program = program ?? throw new ArgumentNullException(nameof(program));
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            Specialization = kernel.Specialization;
            Layout.RequireProgram(Program);
            LayoutIdentity = new ProgramLayoutIdentity(
                program.Manifest.ProgramId,
                program.ProgramHash,
                program.LayoutHash,
                program.Manifest.OperationSetVersion,
                program.Manifest.NumericProfile);
            layout.Services.RequireIdentity(LayoutIdentity);
            Specialization.RequireProgram(program);
            SpecializationIdentity = Specialization.Identity;
        }

        public CharacterSimulationProgram Program { get; }
        public ProgramExecutionLayout Layout { get; }
        public ProgramLayoutIdentity LayoutIdentity { get; }
        public StableHash SpecializationIdentity { get; }
        internal SimulationKernelSpecializationManifest Specialization { get; }
        internal SimulationKernel Kernel { get; }

        internal void Require(
            CharacterSimulationProgram program,
            ProgramExecutionLayout layout,
            SimulationKernelSpecializationManifest specialization)
        {
            if (!ReferenceEquals(Program, program) ||
                !ReferenceEquals(Layout, layout) ||
                !ReferenceEquals(Specialization, specialization) ||
                !ReferenceEquals(Kernel.Specialization, specialization) ||
                !SpecializationIdentity.Equals(specialization.Identity))
            {
                throw new InvalidOperationException("Kernel Program binding does not match the active Program, Layout and specialization.");
            }
        }
    }

    public sealed class SimulationKernelSpecializationManifest
    {
        readonly IReadOnlyList<SimulationOperationCode> m_SupportedOperations;

        public SimulationKernelSpecializationManifest(
            string backendIdentity,
            SimulationNumericProfile numericProfile,
            OperationSetVersion operationSetVersion,
            IReadOnlyList<SimulationOperationCode> supportedOperations)
        {
            BackendIdentity = SimulationIdentity.Require(backendIdentity, nameof(backendIdentity));
            if (!numericProfile.IsValid)
                throw new ArgumentException("Kernel specialization Numeric Profile is incomplete.", nameof(numericProfile));
            NumericProfile = numericProfile;
            OperationSetVersion = operationSetVersion;
            var operations = supportedOperations == null
                ? Array.Empty<SimulationOperationCode>()
                : supportedOperations.ToArray();
            CharacterGameplayOperationSet.RequireCompleteBackend(operationSetVersion, operations, BackendIdentity);
            m_SupportedOperations = Array.AsReadOnly(operations);
            Identity = StableHash.Compute(
                BackendIdentity,
                NumericProfile.Id.Value,
                NumericProfile.AbiVersion.Value.ToString(),
                OperationSetVersion.Value);
        }

        public string BackendIdentity { get; }
        public SimulationNumericProfile NumericProfile { get; }
        public OperationSetVersion OperationSetVersion { get; }
        public StableHash Identity { get; }
        public IReadOnlyList<SimulationOperationCode> SupportedOperations => m_SupportedOperations;

        public void RequireProgram(CharacterSimulationProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (program.Manifest.NumericProfile != NumericProfile ||
                !program.Manifest.OperationSetVersion.Equals(OperationSetVersion))
            {
                throw new InvalidOperationException(
                    $"Program '{program.Manifest.ProgramId}' does not match Kernel backend '{BackendIdentity}'.");
            }
            for (int i = 0; i < program.Operations.Count; i++)
                CharacterGameplayOperationSet.RequireOperation(program.Operations[i].Code);
        }
    }

    public sealed class SimulationKernel
    {
        static readonly SimulationKernelSpecializationManifest s_Fixed =
            new SimulationKernelSpecializationManifest(
                "character-kernel/fixed-q32.32/v1",
                FixedSimulationNumericProfile.Value,
                CharacterGameplayOperationSet.Version,
                CharacterGameplayOperationSet.Operations);

        readonly object m_WorkspaceGate = new object();
        readonly HashSet<KernelProgramBinding> m_BoundPrograms = new HashSet<KernelProgramBinding>();
        readonly Dictionary<ActorId, ActorEvaluator> m_Evaluators =
            new Dictionary<ActorId, ActorEvaluator>();
        bool m_ProgramBindingsSealed;

        SimulationKernel(SimulationKernelSpecializationManifest specialization)
        {
            Specialization = specialization ?? throw new ArgumentNullException(nameof(specialization));
            if (specialization.NumericProfile != FixedSimulationNumericProfile.Value)
                throw new InvalidOperationException("This assembly installs only the Fixed Kernel specialization.");
        }

        public SimulationKernelSpecializationManifest Specialization { get; }
        public static SimulationKernelSpecializationManifest SpecializationManifest => s_Fixed;
        public static SimulationKernel CreateFixed() => new SimulationKernel(s_Fixed);

        internal void BindPrograms(IReadOnlyList<KernelProgramBinding> bindings)
        {
            if (m_ProgramBindingsSealed || m_Evaluators.Count != 0)
                throw new InvalidOperationException("Fixed Kernel Program bindings are already active.");
            if (bindings == null || bindings.Count == 0)
                throw new ArgumentException("Fixed Kernel requires at least one Program binding.", nameof(bindings));
            for (int i = 0; i < bindings.Count; i++)
            {
                KernelProgramBinding binding = bindings[i] ??
                    throw new ArgumentException("Fixed Kernel Program binding is missing.", nameof(bindings));
                binding.Require(binding.Program, binding.Layout, Specialization);
                if (!m_BoundPrograms.Add(binding))
                    throw new InvalidOperationException($"Program '{binding.Program.Manifest.ProgramId}' is bound more than once.");
            }
            m_ProgramBindingsSealed = true;
        }

        void RequireBoundProgram(KernelProgramBinding binding)
        {
            if (!m_ProgramBindingsSealed || binding == null || !m_BoundPrograms.Contains(binding))
                throw new InvalidOperationException("Program is not bound to the active Fixed Kernel.");
            binding.Require(binding.Program, binding.Layout, Specialization);
        }

        public PendingCharacterEvaluation Evaluate(SimulationEvaluateRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            using (request.Performance.Measure(SimulationPerformancePhase.KernelEvaluate))
                return EvaluateMeasured(request);
        }

        PendingCharacterEvaluation EvaluateMeasured(SimulationEvaluateRequest request)
        {
            using (request.Performance.Measure(SimulationPerformancePhase.KernelProgramValidation))
                RequireBoundProgram(request.Binding);
            ActorEvaluator actorEvaluator;
            using (request.Performance.Measure(SimulationPerformancePhase.KernelWorkspace))
            {
                actorEvaluator = GetEvaluator(request);
            }
            ActorOutputWorkspaceLease outputLease = actorEvaluator.Workspace.Begin(
                request.ActorId,
                request.Tick,
                request.Binding);
            bool leaseTransferred = false;
            try
            {
                CharacterOperationEvaluation evaluation = actorEvaluator.Evaluator.Evaluate(request);
                try
                {
                    FixedScalar tickDelta = FixedScalar.One / FixedScalar.FromInt64(request.Program.Manifest.TickRate);
                    BodyMotionPrepareResult bodyMotion = CharacterBodyMotionRuntime.Prepare(
                        request.ActorId,
                        request.Tick,
                        request.PreviousBody,
                        evaluation.GameplayMotion,
                        request.Program.BodyMotion,
                        tickDelta);
                    var worldRequest = new CharacterWorldSolveRequest(
                        request.Program.Manifest.NumericProfile,
                        request.ActorId,
                        new WorldRequestId(request.ActorId, request.Tick, 1),
                        request.Tick,
                        request.PreviousBody,
                        bodyMotion.Motion,
                        bodyMotion.Plan,
                        request.Program.Manifest.Capabilities.RequiredWorldCapabilities);
                    using (request.Performance.Measure(SimulationPerformancePhase.KernelPendingLease))
                    {
                        var pending = new PendingCharacterEvaluation(
                            request.Binding,
                            request.ActorId,
                            request.Tick,
                            request.CurrentState,
                            evaluation.Transaction,
                            outputLease,
                            worldRequest,
                            request.DiagnosticsEnabled);
                        leaseTransferred = true;
                        return pending;
                    }
                }
                catch
                {
                    evaluation.Transaction.Dispose();
                    throw;
                }
            }
            finally
            {
                if (!leaseTransferred)
                    actorEvaluator.Workspace.End(outputLease);
            }
        }

        ActorEvaluator GetEvaluator(SimulationEvaluateRequest request)
        {
            lock (m_WorkspaceGate)
            {
                if (!m_Evaluators.TryGetValue(request.ActorId, out ActorEvaluator evaluator) ||
                    !evaluator.Evaluator.Matches(request))
                {
                    evaluator = new ActorEvaluator(request);
                    m_Evaluators[request.ActorId] = evaluator;
                }
                return evaluator;
            }
        }

        sealed class ActorEvaluator
        {
            public ActorEvaluator(SimulationEvaluateRequest request)
            {
                Workspace = new FixedEvaluationWorkspace(request.ExecutionLayout);
                Evaluator = new FixedOperationEvaluator(
                    request.Program,
                    request.ExecutionLayout,
                    request.ActorId,
                    Workspace);
            }

            public FixedEvaluationWorkspace Workspace { get; }
            public FixedOperationEvaluator Evaluator { get; }
        }

        public SimulationActorTickResult Finalize(SimulationFinalizeRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            using (request.Performance.Measure(SimulationPerformancePhase.KernelFinalize))
                return FinalizeMeasured(request);
        }

        SimulationActorTickResult FinalizeMeasured(SimulationFinalizeRequest request)
        {
            PendingCharacterEvaluation pending = request.Pending;
            using (request.Performance.Measure(SimulationPerformancePhase.KernelProgramValidation))
                RequireBoundProgram(pending.Binding);
            ActorEvaluator actorEvaluator;
            using (request.Performance.Measure(SimulationPerformancePhase.KernelWorkspace))
            {
                actorEvaluator = GetEvaluator(pending);
                actorEvaluator.Workspace.Require(pending.OutputLease);
            }
            FixedCharacterStateTransaction transaction = null;
            try
            {
                transaction = pending.ClaimForFinalize(Specialization);
                CharacterWorldSolveResult world = request.WorldResult;
                CharacterWorldSolveRequest expected = pending.WorldRequest;
                if (world.ActorId != pending.ActorId ||
                    world.NumericProfile != pending.Program.Manifest.NumericProfile ||
                    world.ActorId != expected.ActorId ||
                    world.Tick != pending.Tick ||
                    world.Tick != expected.Tick ||
                    !world.RequestId.Equals(expected.RequestId) ||
                    !world.SolverId.Equals(request.ExpectedSolverId) ||
                    world.FinalBody.ActorId != pending.ActorId)
                {
                    throw new InvalidOperationException(
                        $"World result '{world.RequestId}' does not match pending request '{expected.RequestId}' and Solver '{request.ExpectedSolverId.Value}'.");
                }

                List<GameplayFact> facts = actorEvaluator.Workspace.Facts;
                List<SimulationTraceRecord> trace = actorEvaluator.Workspace.Trace;
                facts.Add(CreateMotionFact(transaction, pending, world));
                if (pending.DiagnosticsEnabled)
                {
                    trace.Add(CreateFinalizeTrace(transaction, pending, world, checked((ulong)trace.Count + 1UL)));
                }
                var bodySample = new CharacterBodySample(
                    pending.ActorId,
                    pending.Tick,
                    expected.BeforeBody,
                    world.FinalBody,
                    world.AppliedDisplacement,
                    world.AppliedYawDegrees);
                CharacterSimulationState finalState;
                using (request.Performance.Measure(SimulationPerformancePhase.KernelStateCommit))
                    finalState = transaction.Commit();
                using (request.Performance.Measure(SimulationPerformancePhase.KernelResultFreeze))
                {
                    return new SimulationActorTickResult(
                        pending.ActorId,
                        pending.Tick,
                        finalState,
                        bodySample,
                        expected.Motion,
                        facts,
                        actorEvaluator.Workspace.Presentation,
                        trace);
                }
            }
            catch
            {
                if (transaction != null && transaction.Status == FixedCharacterStateTransactionStatus.Active)
                    transaction.Abort();
                throw;
            }
            finally
            {
                transaction?.Dispose();
                actorEvaluator.Workspace.End(pending.OutputLease);
            }
        }

        internal void Abort(PendingCharacterEvaluation pending)
        {
            if (pending == null)
                throw new ArgumentNullException(nameof(pending));
            RequireBoundProgram(pending.Binding);
            if (!pending.TryClaimForAbort(Specialization, out FixedCharacterStateTransaction transaction))
                return;
            ActorEvaluator actorEvaluator = GetEvaluator(pending);
            bool leaseHeld = false;
            try
            {
                actorEvaluator.Workspace.Require(pending.OutputLease);
                leaseHeld = true;
                if (transaction.Status == FixedCharacterStateTransactionStatus.Active)
                    transaction.Abort();
            }
            finally
            {
                transaction.Dispose();
                if (leaseHeld)
                    actorEvaluator.Workspace.End(pending.OutputLease);
            }
        }

        ActorEvaluator GetEvaluator(PendingCharacterEvaluation pending)
        {
            lock (m_WorkspaceGate)
            {
                if (!m_Evaluators.TryGetValue(pending.ActorId, out ActorEvaluator evaluator) ||
                    !evaluator.Evaluator.Matches(pending))
                {
                    throw new InvalidOperationException(
                        $"Pending evaluation for Actor '{pending.ActorId}' has no matching Actor execution workspace.");
                }
                return evaluator;
            }
        }

        static GameplayFact CreateMotionFact(
            FixedCharacterStateTransaction transaction,
            PendingCharacterEvaluation pending,
            CharacterWorldSolveResult world)
        {
            SimulationEventHeader header = NextFinalizeEvent(transaction, pending, "Gameplay");
            return new GameplayFact(
                header,
                GameplayFactKind.Motion,
                $"world-request:{world.RequestId.Sequence}",
                "Resolved",
                world.AppliedDisplacement.Magnitude);
        }

        static SimulationTraceRecord CreateFinalizeTrace(
            FixedCharacterStateTransaction transaction,
            PendingCharacterEvaluation pending,
            CharacterWorldSolveResult world,
            ulong sequence)
        {
            SimulationEventHeader header = NextFinalizeTrace(transaction, pending, sequence);
            CharacterMotionRequest requested = pending.WorldRequest.Motion;
            BodyMotionIntegrationPlan plan = pending.WorldRequest.BodyMotionPlan;
            return new SimulationTraceRecord(
                header,
                SimulationTraceSeverity.Detail,
                "Kernel.Finalize",
                "world_result_applied",
                $"prepare=descriptor:{plan.DescriptorSourceIdentity}@{plan.SemanticVersion}/{plan.DescriptorContentRevision}," +
                $"gameplayY:{plan.GameplayVerticalDisplacement},previousVertical:{plan.PreviousVerticalVelocity}," +
                $"gravityAcceleration:{pending.Program.BodyMotion.GravityAcceleration},gravityDelta:{plan.GravityDisplacement}," +
                $"candidateVertical:{plan.CandidateVerticalVelocity},requestedY:{plan.RequestedDisplacement.Y};" +
                $"solve=request:{world.RequestId},solver:{world.SolverId.Value},appliedY:{world.AppliedDisplacement.Y}," +
                $"grounded:{world.FinalBody.Grounded},collision:{world.FinalBody.Collision};" +
                $"finalize=committedVertical:{world.FinalBody.VerticalVelocity},actualVelocityY:{world.FinalBody.Velocity.Y}," +
                $"requested=({requested.Displacement.X},{requested.Displacement.Y},{requested.Displacement.Z})/{requested.YawDegrees}," +
                $"applied=({world.AppliedDisplacement.X},{world.AppliedDisplacement.Y},{world.AppliedDisplacement.Z})/{world.AppliedYawDegrees}," +
                $"body=({world.FinalBody.Position.X},{world.FinalBody.Position.Y},{world.FinalBody.Position.Z})/{world.FinalBody.Yaw.Degrees}");
        }

        static SimulationEventHeader NextFinalizeTrace(
            FixedCharacterStateTransaction transaction,
            PendingCharacterEvaluation pending,
            ulong sequence)
        {
            if (sequence == 0)
                throw new ArgumentOutOfRangeException(nameof(sequence));
            OperationHandle root = pending.ExecutionLayout.RootOperation;
            ulong generation = 1;
            SimulationOperation operation = pending.Program.Operations[root.Value];
            int generationSlot = pending.ExecutionLayout.FindOperationStateSlot(
                operation.Handle,
                ProgramStateSemantic.RunnableActivationGeneration);
            if (generationSlot >= 0)
                generation = Math.Max(1UL, transaction.Get(generationSlot).UInt64);
            var activation = new ActivationId(root, generation, "kernel:finalize");
            EventId eventId = EventId.Create(
                pending.Program.ProgramHash,
                pending.ActorId,
                activation,
                pending.Tick,
                sequence,
                "Trace");
            return new SimulationEventHeader(
                pending.Program.Manifest.NumericProfile,
                eventId,
                pending.ActorId,
                pending.Tick,
                activation,
                sequence,
                "Trace");
        }

        static SimulationEventHeader NextFinalizeEvent(
            FixedCharacterStateTransaction transaction,
            PendingCharacterEvaluation pending,
            string channel)
        {
            int sequenceSlot = pending.ExecutionLayout.RequireStateSlot(ProgramStateSemantic.FactSequence);
            ulong sequence = checked(transaction.Get(sequenceSlot).UInt64 + 1);
            if (sequence == 0)
                throw new OverflowException("Simulation event sequence overflowed.");
            transaction.Set(sequenceSlot, CharacterStateValue.FromUInt64(sequence));
            OperationHandle root = pending.ExecutionLayout.RootOperation;
            ulong generation = 1;
            SimulationOperation operation = pending.Program.Operations[root.Value];
            int generationSlot = pending.ExecutionLayout.FindOperationStateSlot(operation.Handle, ProgramStateSemantic.RunnableActivationGeneration);
            if (generationSlot >= 0)
                generation = Math.Max(1UL, transaction.Get(generationSlot).UInt64);
            var activation = new ActivationId(root, generation, "kernel:finalize");
            EventId eventId = EventId.Create(
                pending.Program.ProgramHash,
                pending.ActorId,
                activation,
                pending.Tick,
                sequence,
                channel);
            return new SimulationEventHeader(pending.Program.Manifest.NumericProfile, eventId, pending.ActorId, pending.Tick, activation, sequence, channel);
        }

    }
}

