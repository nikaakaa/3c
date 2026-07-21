using System;
using System.Collections.Generic;
using System.Diagnostics;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    public sealed class DeterministicKccWorldSolver : ICharacterWorldSolver
    {
        public const string SolverId = "thirdperson.simulation.solver.deterministic-kcc";
        public const string SolverVersion = "6";

        static readonly SolverImplementationId s_ImplementationId = new SolverImplementationId(SolverId);
        readonly DeterministicCollisionWorldArtifact m_CollisionWorld;
        readonly DeterministicKccConfiguration m_Configuration;
        readonly DeterministicKccMotor[] m_Motors;
        readonly int m_TickRate;
        readonly ActorBinding[] m_Bindings;
        readonly ActorSolveCandidate[] m_Candidates;
        readonly DeterministicActorContactCandidate[] m_ActorContacts;
        readonly FixedVector3[] m_CandidatePositions;
        readonly DeterministicActorContactWorkspace m_ActorContactWorkspace;
        readonly List<DeterministicActorContactTrace> m_ContactTraces;

        DeterministicKccBodyState[] m_KccStates;
        WorldSimulationState m_Current;
        bool m_Disposed;

        public DeterministicKccWorldSolver(
            int tickRate,
            DeterministicCollisionWorldArtifact collisionWorld,
            DeterministicKccConfiguration configuration,
            IReadOnlyList<ActorBinding> bindings)
        {
            if (tickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickRate));
            m_TickRate = tickRate;
            m_CollisionWorld = collisionWorld ?? throw new ArgumentNullException(nameof(collisionWorld));
            m_Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            if (bindings == null || bindings.Count == 0)
                throw new ArgumentException("Deterministic KCC requires an Actor roster.", nameof(bindings));
            m_Bindings = new ActorBinding[bindings.Count];
            for (int i = 0; i < bindings.Count; i++)
                m_Bindings[i] = bindings[i];
            Array.Sort(m_Bindings, (left, right) => left.ActorId.CompareTo(right.ActorId));
            for (int i = 0; i < m_Bindings.Length; i++)
            {
                if (!m_Bindings[i].ActorId.IsValid || string.IsNullOrEmpty(m_Bindings[i].BindingId) ||
                    i > 0 && m_Bindings[i - 1].ActorId.Equals(m_Bindings[i].ActorId))
                {
                    throw new ArgumentException("Deterministic KCC Actor bindings are invalid or duplicated.", nameof(bindings));
                }
            }
            m_Candidates = new ActorSolveCandidate[m_Bindings.Length];
            m_ActorContacts = new DeterministicActorContactCandidate[m_Bindings.Length];
            m_CandidatePositions = new FixedVector3[m_Bindings.Length];
            m_Motors = new DeterministicKccMotor[m_Bindings.Length];
            for (int i = 0; i < m_Motors.Length; i++)
                m_Motors[i] = new DeterministicKccMotor(collisionWorld, configuration);
            m_ActorContactWorkspace = new DeterministicActorContactWorkspace(
                m_Bindings.Length,
                configuration.MaximumActorPairs,
                configuration.MaximumActorContactIterations);
            int traceCapacity = checked(
                configuration.MaximumActorPairs * (configuration.MaximumActorContactIterations * 5 + 2));
            m_ContactTraces = new List<DeterministicActorContactTrace>(traceCapacity);
            Descriptor = new CharacterWorldSolverDescriptor(
                FixedSimulationNumericProfile.Value,
                s_ImplementationId,
                SolverVersion,
                WorldCapability.BodyMotion |
                WorldCapability.Grounding |
                WorldCapability.Collision |
                WorldCapability.Reconstructible |
                WorldCapability.Snapshotable |
                WorldCapability.DeterministicReplay |
                WorldCapability.AirborneVerticalMotion,
                WorldFeature.Ground |
                WorldFeature.Slope |
                WorldFeature.Step |
                WorldFeature.WallSlide |
                WorldFeature.ActorCollision);
            KccIdentityHash = ComputeIdentity(tickRate, collisionWorld, configuration);
        }

        public CharacterWorldSolverDescriptor Descriptor { get; }
        public StableHash CollisionWorldHash => m_CollisionWorld.ContentHash;
        public StableHash KccIdentityHash { get; }

        public static StableHash ComputeIdentity(
            int tickRate,
            DeterministicCollisionWorldArtifact collisionWorld,
            DeterministicKccConfiguration configuration)
        {
            if (tickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickRate));
            if (collisionWorld == null)
                throw new ArgumentNullException(nameof(collisionWorld));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            return StableHash.Compute(
                "deterministic-kcc/4",
                SolverId,
                SolverVersion,
                DeterministicCollisionWorldArtifact.ArtifactSchema,
                DeterministicKccConfiguration.QuerySemanticVersion,
                DeterministicKccConfiguration.MotorSemanticVersion,
                collisionWorld.ContentHash.Value,
                configuration.ConfigurationHash.Value,
                tickRate.ToString());
        }

        public void RequireBodyBinding(ActorId actorId, string bindingId)
        {
            RequireAlive();
            string expected = SimulationIdentity.Require(bindingId, nameof(bindingId));
            int index = FindBinding(actorId);
            if (index < 0)
                throw new InvalidOperationException($"Deterministic KCC has no body binding for Actor '{actorId}'.");
            if (!string.Equals(m_Bindings[index].BindingId, expected, StringComparison.Ordinal))
                throw new InvalidOperationException($"Actor '{actorId}' expects binding '{expected}', KCC owns '{m_Bindings[index].BindingId}'.");
        }

        public WorldSimulationState Create(
            WorldRevision worldRevision,
            IReadOnlyList<WorldBodyState> orderedInitialBodies)
        {
            RequireAlive();
            ValidateBodies(orderedInitialBodies);
            var bodies = new WorldBodyState[m_Bindings.Length];
            m_KccStates = new DeterministicKccBodyState[m_Bindings.Length];
            for (int i = 0; i < m_Bindings.Length; i++)
            {
                WorldBodyState source = orderedInitialBodies[i];
                DeterministicKccBodyState empty = EmptyState(source.ActorId);
                DeterministicKccMotorResult initial = m_Motors[i].PlaceInitial(source.Position, empty);
                bodies[i] = new WorldBodyState(
                    source.ActorId,
                    initial.Position,
                    source.Yaw,
                    source.Velocity,
                    source.VerticalVelocity,
                    initial.Ground.IsStableOnGround,
                    source.Collision | initial.Collision);
                m_KccStates[i] = CreateKccState(source.ActorId, initial);
            }
            m_Current = CreateState(worldRevision, bodies, m_KccStates);
            return CloneState(m_Current);
        }

        public void Reconstruct(WorldSimulationState state)
        {
            RequireAlive();
            ValidateState(state);
            m_KccStates = DeterministicKccStateCodec.Read(
                state.SolverStatePayload.ToArray(),
                m_CollisionWorld.ContentHash,
                m_Configuration.ConfigurationHash);
            RequireKccRoster(m_KccStates);
            m_Current = CloneState(state);
        }

        public WorldSimulationState Capture(WorldRevision worldRevision)
        {
            RequireAlive();
            RequireCurrent();
            if (!m_Current.WorldRevision.Equals(worldRevision))
                throw new InvalidOperationException("Deterministic KCC cannot capture another WorldRevision.");
            return CloneState(m_Current);
        }

        public void Restore(WorldSimulationState state) => Reconstruct(state);

        public WorldSolveBatchResult ResolveBatch(
            WorldSolveBatchRequest request,
            ISimulationDiagnosticsSink diagnostics)
        {
            RequireAlive();
            RequireCurrent();
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (diagnostics == null)
                throw new ArgumentNullException(nameof(diagnostics));
            if (!StateEquals(request.BeforeWorldState, m_Current))
                throw new InvalidOperationException("Deterministic KCC request does not match current explicit World state.");
            if (!Descriptor.Supports(request.RequiredCapabilities))
                throw new InvalidOperationException($"Deterministic KCC is missing required capability '{request.RequiredCapabilities & ~Descriptor.Capabilities}'.");
            if (request.Requests.Count != m_Bindings.Length)
                throw new InvalidOperationException("Deterministic KCC request roster count is stale.");

            FixedScalar tickDelta = FixedScalar.One / FixedScalar.FromInt64(m_TickRate);
            for (int i = 0; i < request.Requests.Count; i++)
            {
                CharacterWorldSolveRequest actorRequest = request.Requests[i];
                if (!actorRequest.ActorId.Equals(m_Bindings[i].ActorId) ||
                    !m_KccStates[i].ActorId.Equals(actorRequest.ActorId))
                {
                    throw new InvalidOperationException("Deterministic KCC request, binding, and state rosters are inconsistent.");
                }
                WorldBodyState before = actorRequest.BeforeBody;
                FixedVector3 requested = actorRequest.Motion.Space == WorldMotionSpace.ActorLocal
                    ? FixedAngle.RotatePlanar(actorRequest.Motion.Displacement, before.Yaw)
                    : actorRequest.Motion.Displacement;
                long solveStarted = Stopwatch.GetTimestamp();
                DeterministicKccMotorResult motorResult;
                try
                {
                    motorResult = m_Motors[i].Move(before.Position, m_KccStates[i], requested);
                }
                catch (DeterministicKccQueryException exception)
                {
                    PublishFailure(diagnostics, request.Tick, actorRequest.ActorId, requested, exception, Stopwatch.GetTimestamp() - solveStarted);
                    throw;
                }
                m_Candidates[i] = new ActorSolveCandidate(
                    actorRequest,
                    requested,
                    motorResult.Position,
                    motorResult.Ground,
                    motorResult.Collision,
                    motorResult.StepPhase,
                    motorResult.StepRejection,
                    motorResult.RemainingDisplacement,
                    motorResult.MovementIterations,
                    motorResult.HasBlockingContact,
                    motorResult.HasBlockingContact ? motorResult.BlockingContactAt(0) : default,
                    motorResult.BlockingContactCount,
                    motorResult.QuerySummary,
                    m_KccStates[i],
                    Stopwatch.GetTimestamp() - solveStarted);
                m_ActorContacts[i] = new DeterministicActorContactCandidate(
                    actorRequest.ActorId,
                    before.Position,
                    motorResult.Position,
                    m_Configuration.ActorContactShape);
            }

            long contactStarted = Stopwatch.GetTimestamp();
            m_ContactTraces.Clear();
            DeterministicActorContactSummary contactSummary = default;
            try
            {
                DeterministicActorContactBatchResult contactResult = DeterministicActorContactSolver.Resolve(
                    m_ActorContacts,
                    m_ActorContactWorkspace);
                ApplyActorContactResult(contactResult);
                AppendTraces(contactResult.Traces);
                contactSummary = contactSummary.Add(contactResult.Summary);

                bool converged = false;
                for (int iteration = 0; iteration < m_Configuration.MaximumActorContactIterations; iteration++)
                {
                    for (int i = 0; i < m_Candidates.Length; i++)
                        ReapplyStaticWorldConstraints(i);
                    FillCandidatePositions();
                    DeterministicActorContactBatchResult reconciliation =
                        DeterministicActorContactSolver.ResolveFinalPenetrationPass(
                            m_ActorContacts,
                            m_CandidatePositions,
                            m_ActorContactWorkspace,
                            iteration);
                    ApplyActorContactResult(reconciliation);
                    AppendTraces(reconciliation.Traces);
                    contactSummary = contactSummary.Add(reconciliation.Summary);
                    if (!reconciliation.Corrected)
                    {
                        converged = true;
                        break;
                    }
                }
                if (!converged)
                {
                    throw new DeterministicActorContactSolveException(
                        $"Deterministic Actor contact and static world constraints did not converge after '{m_Configuration.MaximumActorContactIterations}' iterations.",
                        Array.Empty<DeterministicActorContactTrace>());
                }

                FillCandidatePositions();
                DeterministicActorContactBatchResult validation =
                    DeterministicActorContactSolver.ValidateFinal(
                        m_ActorContacts,
                        m_CandidatePositions,
                        m_ActorContactWorkspace,
                        m_Configuration.MaximumActorContactIterations);
                AppendTraces(validation.Traces);
                contactSummary = contactSummary.Add(validation.Summary);
                for (int i = 0; i < m_Candidates.Length; i++)
                    ValidateStaticWorldResult(i);
            }
            catch (DeterministicActorContactSolveException exception)
            {
                AppendTraces(exception.Traces);
                PublishActorContactDiagnostics(
                    diagnostics,
                    request.Tick,
                    m_ContactTraces,
                    contactSummary,
                    Stopwatch.GetTimestamp() - contactStarted,
                    false);
                throw;
            }

            PublishActorContactDiagnostics(
                diagnostics,
                request.Tick,
                m_ContactTraces,
                contactSummary,
                Stopwatch.GetTimestamp() - contactStarted,
                true);

            var bodies = new WorldBodyState[m_Candidates.Length];
            var states = new DeterministicKccBodyState[m_Candidates.Length];
            var results = new CharacterWorldSolveResult[m_Candidates.Length];
            DeterministicKccQuerySummary contactQuerySummary = new DeterministicKccQuerySummary(
                0,
                contactSummary.PairChecks,
                checked(contactSummary.SweepCount + contactSummary.DepenetrationCount),
                checked(contactSummary.IterationCount + contactSummary.ValidationCount));
            for (int i = 0; i < m_Candidates.Length; i++)
            {
                ActorSolveCandidate candidate = m_Candidates[i];
                CharacterWorldSolveRequest actorRequest = candidate.Request;
                WorldBodyState before = actorRequest.BeforeBody;
                FixedVector3 applied = candidate.Position - before.Position;
                FixedYaw finalYaw = new FixedYaw(before.Yaw.Degrees + actorRequest.Motion.YawDegrees);
                FixedScalar appliedYaw = FixedAngle.Delta(before.Yaw, finalYaw);
                DeterministicKccQuerySummary querySummary = candidate.QuerySummary.Add(contactQuerySummary);
                bodies[i] = CharacterBodyMotionRuntime.Finalize(
                    before,
                    actorRequest.BodyMotionPlan,
                    candidate.Position,
                    finalYaw,
                    applied,
                    candidate.Ground.IsStableOnGround,
                    candidate.Collision,
                    tickDelta);
                states[i] = new DeterministicKccBodyState(
                    actorRequest.ActorId,
                    candidate.Ground.IsStableOnGround,
                    candidate.Ground.PrimitiveId,
                    candidate.Ground.FeatureId,
                    candidate.Ground.Normal);
                results[i] = new CharacterWorldSolveResult(
                    Descriptor.NumericProfile,
                    actorRequest.ActorId,
                    actorRequest.RequestId,
                    request.Tick,
                    Descriptor.ImplementationId,
                    bodies[i],
                    applied,
                    appliedYaw);
                PublishDiagnostics(
                    diagnostics,
                    request.Tick,
                    actorRequest.ActorId,
                    candidate.Requested,
                    applied,
                    candidate.Ground,
                    candidate.StepPhase,
                    candidate.StepRejection,
                    candidate.Remaining,
                    candidate.MovementIterations,
                    candidate.HasBlockingContact,
                    candidate.BlockingContact,
                    candidate.BlockingContactCount,
                    querySummary,
                    candidate.ElapsedStopwatchTicks);
            }
            m_KccStates = states;
            m_Current = CreateState(request.BeforeWorldState.WorldRevision, bodies, states);
            return new WorldSolveBatchResult(request, Descriptor.ImplementationId, Descriptor.Version, CloneState(m_Current), results);
        }

        void ReapplyStaticWorldConstraints(int index)
        {
            ActorSolveCandidate candidate = m_Candidates[index];
            DeterministicKccMotorResult result;
            try
            {
                result = m_Motors[index].ReconstraintAfterMovement(
                    candidate.Position,
                    candidate.PreviousState,
                    candidate.Requested);
            }
            catch (DeterministicKccQueryException exception)
            {
                throw new DeterministicActorContactSolveException(
                    $"Actor '{candidate.Request.ActorId}' static reconstraint failed: {exception.Message}",
                    Array.Empty<DeterministicActorContactTrace>());
            }
            candidate.QuerySummary = candidate.QuerySummary.Add(result.QuerySummary);
            candidate.Position = result.Position;
            candidate.Ground = result.Ground;
            candidate.Collision |= result.Collision;
            RequireInsideWorld(candidate.Position);
            m_Candidates[index] = candidate;
        }

        void ValidateStaticWorldResult(int index)
        {
            ActorSolveCandidate candidate = m_Candidates[index];
            DeterministicKccQuerySummary summary = default;
            try
            {
                m_Motors[index].ValidatePose(candidate.Position, ref summary);
            }
            catch (DeterministicKccQueryException exception)
            {
                throw new DeterministicActorContactSolveException(
                    $"Actor '{candidate.Request.ActorId}' final static validation failed: {exception.Message}",
                    Array.Empty<DeterministicActorContactTrace>());
            }
            candidate.QuerySummary = candidate.QuerySummary.Add(summary);
            RequireInsideWorld(candidate.Position);
            m_Candidates[index] = candidate;
        }

        void ApplyActorContactResult(DeterministicActorContactBatchResult result)
        {
            if (result.Count != m_Candidates.Length)
                throw new InvalidOperationException("Deterministic Actor contact result roster is invalid.");
            for (int i = 0; i < m_Candidates.Length; i++)
            {
                ActorSolveCandidate candidate = m_Candidates[i];
                candidate.Position = result.PositionAt(i);
                if (result.HadContactAt(i))
                    candidate.Collision |= WorldCollisionSummary.Sides;
                m_Candidates[i] = candidate;
            }
        }

        void FillCandidatePositions()
        {
            for (int i = 0; i < m_Candidates.Length; i++)
                m_CandidatePositions[i] = m_Candidates[i].Position;
        }

        void AppendTraces(IReadOnlyList<DeterministicActorContactTrace> source)
        {
            if (source == null)
                return;
            if (m_ContactTraces.Count + source.Count > m_ContactTraces.Capacity)
            {
                throw new DeterministicActorContactSolveException(
                    $"Deterministic Actor contact trace capacity '{m_ContactTraces.Capacity}' was exceeded.",
                    Array.Empty<DeterministicActorContactTrace>());
            }
            for (int i = 0; i < source.Count; i++)
                m_ContactTraces.Add(source[i]);
        }

        WorldSimulationState CreateState(
            WorldRevision revision,
            IReadOnlyList<WorldBodyState> bodies,
            IReadOnlyList<DeterministicKccBodyState> states)
        {
            byte[] payload = DeterministicKccStateCodec.Write(
                m_CollisionWorld.ContentHash,
                m_Configuration.ConfigurationHash,
                states);
            return new WorldSimulationState(
                Descriptor.NumericProfile,
                Descriptor.ImplementationId,
                Descriptor.Version,
                revision,
                WorldStatePersistenceMode.Snapshot,
                bodies,
                payload);
        }

        void ValidateState(WorldSimulationState state)
        {
            if (state == null || state.NumericProfile != Descriptor.NumericProfile ||
                !state.SolverId.Equals(Descriptor.ImplementationId) ||
                !string.Equals(state.SolverVersion, Descriptor.Version, StringComparison.Ordinal) ||
                state.PersistenceMode != WorldStatePersistenceMode.Snapshot || state.Bodies.Count != m_Bindings.Length)
            {
                throw new InvalidOperationException("World state is incompatible with Deterministic KCC.");
            }
            ValidateBodies(state.Bodies);
        }

        void ValidateBodies(IReadOnlyList<WorldBodyState> bodies)
        {
            if (bodies == null || bodies.Count != m_Bindings.Length)
                throw new ArgumentException("World body roster does not match Deterministic KCC bindings.", nameof(bodies));
            for (int i = 0; i < bodies.Count; i++)
            {
                if (!bodies[i].ActorId.Equals(m_Bindings[i].ActorId))
                    throw new ArgumentException("World body order does not match Deterministic KCC bindings.", nameof(bodies));
                RequireInsideWorld(bodies[i].Position);
            }
        }

        void RequireKccRoster(IReadOnlyList<DeterministicKccBodyState> states)
        {
            if (states.Count != m_Bindings.Length)
                throw new InvalidOperationException("Deterministic KCC state roster count is stale.");
            for (int i = 0; i < states.Count; i++)
            {
                if (!states[i].ActorId.Equals(m_Bindings[i].ActorId))
                    throw new InvalidOperationException("Deterministic KCC state roster order is stale.");
            }
        }

        void RequireInsideWorld(FixedVector3 position)
        {
            DeterministicCollisionBounds bounds = m_CollisionWorld.Bounds;
            if (position.X < bounds.Minimum.X || position.X > bounds.Maximum.X ||
                position.Y < bounds.Minimum.Y || position.Y + m_Configuration.Height > bounds.Maximum.Y ||
                position.Z < bounds.Minimum.Z || position.Z > bounds.Maximum.Z)
            {
                throw new InvalidOperationException("Deterministic KCC body left the collision world bounds.");
            }
        }

        void PublishDiagnostics(
            ISimulationDiagnosticsSink diagnostics,
            SimulationTick tick,
            ActorId actorId,
            FixedVector3 requested,
            FixedVector3 applied,
            DeterministicKccGroundReport ground,
            DeterministicKccStepPhase stepPhase,
            DeterministicKccStepRejection stepRejection,
            FixedVector3 remaining,
            int movementIterations,
            bool hasBlockingContact,
            DeterministicKccContact blockingContact,
            int blockingContactCount,
            DeterministicKccQuerySummary summary,
            long elapsedStopwatchTicks)
        {
            if (!diagnostics.IsEnabled)
                return;
            string feature = ground.FeatureId.IsValid
                ? $"{ground.FeatureId.Kind}:{ground.FeatureId.Index}"
                : "none";
            string hit = hasBlockingContact
                ? $"{blockingContact.PrimitiveId}:{blockingContact.FeatureId.Kind}:{blockingContact.FeatureId.Index};toiRaw={blockingContact.TimeOfImpact.Raw};normalRaw={blockingContact.Normal.X.Raw},{blockingContact.Normal.Y.Raw},{blockingContact.Normal.Z.Raw}"
                : "none";
            diagnostics.PublishWorld(new SimulationWorldTraceRecord(
                SimulationWorldTraceKind.Collision,
                "deterministic_kcc_resolve",
                $"candidates={summary.CandidateCount};contacts={summary.ContactCount};queryIterations={summary.IterationCount};movementIterations={movementIterations};grounded={ground.IsStableOnGround};support={ground.PrimitiveId}:{feature};ledge={ground.LedgeState};step={stepPhase};stepRejection={stepRejection};blockingContacts={blockingContactCount};hit={hit};remainingRaw={remaining.X.Raw},{remaining.Y.Raw},{remaining.Z.Raw}",
                tick,
                actorId,
                Descriptor.ImplementationId,
                Descriptor.Version,
                traversalCount: summary.QueryCount,
                resolveStatus: (uint)summary.IterationCount,
                elapsedStopwatchTicks: elapsedStopwatchTicks,
                requestedDisplacement: requested,
                appliedDisplacement: applied,
                disposition: "resolved"));
        }

        void PublishFailure(
            ISimulationDiagnosticsSink diagnostics,
            SimulationTick tick,
            ActorId actorId,
            FixedVector3 requested,
            DeterministicKccQueryException exception,
            long elapsedStopwatchTicks)
        {
            if (!diagnostics.IsEnabled)
                return;
            diagnostics.PublishWorld(new SimulationWorldTraceRecord(
                SimulationWorldTraceKind.Failure,
                "deterministic_kcc_failure",
                exception.Message,
                tick,
                actorId,
                Descriptor.ImplementationId,
                Descriptor.Version,
                sourceReference: exception.PrimitiveId,
                resultReference: exception.RequiredCapacity,
                region: exception.ConfiguredCapacity,
                localizationStatus: (uint)exception.Stage,
                elapsedStopwatchTicks: elapsedStopwatchTicks,
                requestedDisplacement: requested,
                disposition: exception.Stage.ToString(),
                success: false));
        }

        void PublishActorContactDiagnostics(
            ISimulationDiagnosticsSink diagnostics,
            SimulationTick tick,
            IReadOnlyList<DeterministicActorContactTrace> traces,
            DeterministicActorContactSummary summary,
            long elapsedStopwatchTicks,
            bool success)
        {
            if (!diagnostics.IsEnabled)
                return;
            for (int i = 0; i < m_Bindings.Length; i++)
            {
                diagnostics.PublishWorld(new SimulationWorldTraceRecord(
                    success ? SimulationWorldTraceKind.Collision : SimulationWorldTraceKind.Failure,
                    "deterministic_actor_contact_batch",
                    $"pairs={summary.PairCount};checks={summary.PairChecks};sweeps={summary.SweepCount};clips={summary.NormalClipCount};depenetrations={summary.DepenetrationCount};iterations={summary.IterationCount};validations={summary.ValidationCount}",
                    tick,
                    m_Bindings[i].ActorId,
                    Descriptor.ImplementationId,
                    Descriptor.Version,
                    traversalCount: summary.PairChecks,
                    resolveStatus: (uint)summary.IterationCount,
                    elapsedStopwatchTicks: elapsedStopwatchTicks,
                    disposition: success ? "solid-body-block-resolved" : "solid-body-block-failed",
                    success: success));
            }
            for (int i = 0; i < traces.Count; i++)
            {
                DeterministicActorContactTrace trace = traces[i];
                PublishActorContactTrace(diagnostics, tick, trace, trace.ActorA, trace.ActorB, trace.CorrectionA);
                if (!trace.ActorA.Equals(trace.ActorB))
                    PublishActorContactTrace(diagnostics, tick, trace, trace.ActorB, trace.ActorA, trace.CorrectionB);
            }
        }

        void PublishActorContactTrace(
            ISimulationDiagnosticsSink diagnostics,
            SimulationTick tick,
            DeterministicActorContactTrace trace,
            ActorId actorId,
            ActorId otherActorId,
            FixedVector3 correction)
        {
            bool success = trace.Kind != DeterministicActorContactTraceKind.Failure;
            diagnostics.PublishWorld(new SimulationWorldTraceRecord(
                success ? SimulationWorldTraceKind.Collision : SimulationWorldTraceKind.Failure,
                $"deterministic_actor_contact_{trace.Kind.ToString().ToLowerInvariant()}",
                $"other={otherActorId.Value};pair={trace.PairIndex};iteration={trace.Iteration};toiRaw={trace.TimeOfImpact.Raw};normalRaw={trace.Normal.X.Raw},{trace.Normal.Z.Raw};detail={trace.Detail}",
                tick,
                actorId,
                Descriptor.ImplementationId,
                Descriptor.Version,
                sourceReference: trace.PairIndex,
                resultReference: trace.TimeOfImpact.Raw,
                region: trace.Iteration,
                traversalCount: 1,
                localizationStatus: (uint)trace.Kind,
                requestedDisplacement: trace.Normal,
                appliedDisplacement: correction,
                disposition: trace.Detail,
                success: success));
        }

        int FindBinding(ActorId actorId)
        {
            int low = 0;
            int high = m_Bindings.Length - 1;
            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                int comparison = m_Bindings[middle].ActorId.CompareTo(actorId);
                if (comparison == 0)
                    return middle;
                if (comparison < 0)
                    low = middle + 1;
                else
                    high = middle - 1;
            }
            return -1;
        }

        static DeterministicKccBodyState EmptyState(ActorId actorId) => new DeterministicKccBodyState(
            actorId,
            false,
            -1,
            DeterministicCollisionFeatureId.Invalid,
            FixedVector3.Zero);

        static DeterministicKccBodyState CreateKccState(
            ActorId actorId,
            DeterministicKccMotorResult result) => new DeterministicKccBodyState(
            actorId,
            result.Ground.IsStableOnGround,
            result.Ground.PrimitiveId,
            result.Ground.FeatureId,
            result.Ground.Normal);

        static WorldSimulationState CloneState(WorldSimulationState state) => new WorldSimulationState(
            state.NumericProfile,
            state.SolverId,
            state.SolverVersion,
            state.WorldRevision,
            state.PersistenceMode,
            state.Bodies,
            state.SolverStatePayload.ToArray());

        static bool StateEquals(WorldSimulationState left, WorldSimulationState right)
        {
            if (left == null || right == null || left.NumericProfile != right.NumericProfile ||
                !left.SolverId.Equals(right.SolverId) || !string.Equals(left.SolverVersion, right.SolverVersion, StringComparison.Ordinal) ||
                !left.WorldRevision.Equals(right.WorldRevision) || left.PersistenceMode != right.PersistenceMode ||
                left.Bodies.Count != right.Bodies.Count || !BytesEqual(left.SolverStatePayload.Span, right.SolverStatePayload.Span))
                return false;
            for (int i = 0; i < left.Bodies.Count; i++)
            {
                if (!WorldSolveBatchRequest.BodyEquals(left.Bodies[i], right.Bodies[i]))
                    return false;
            }
            return true;
        }

        static bool BytesEqual(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
        {
            if (left.Length != right.Length)
                return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                    return false;
            }
            return true;
        }

        static FixedVector3 Scale(FixedVector3 value, FixedScalar scale) =>
            new FixedVector3(value.X * scale, value.Y * scale, value.Z * scale);

        void RequireCurrent()
        {
            if (m_Current == null)
                throw new InvalidOperationException("Deterministic KCC has not been created or reconstructed.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(DeterministicKccWorldSolver));
        }

        public void Dispose()
        {
            m_Disposed = true;
            m_Current = null;
            m_KccStates = null;
            m_ContactTraces.Clear();
        }

        public readonly struct ActorBinding
        {
            public ActorBinding(ActorId actorId, string bindingId)
            {
                ActorId = actorId;
                BindingId = SimulationIdentity.Require(bindingId, nameof(bindingId));
            }

            public ActorId ActorId { get; }
            public string BindingId { get; }
        }

        struct ActorSolveCandidate
        {
            public ActorSolveCandidate(
                CharacterWorldSolveRequest request,
                FixedVector3 requested,
                FixedVector3 position,
                DeterministicKccGroundReport ground,
                WorldCollisionSummary collision,
                DeterministicKccStepPhase stepPhase,
                DeterministicKccStepRejection stepRejection,
                FixedVector3 remaining,
                int movementIterations,
                bool hasBlockingContact,
                DeterministicKccContact blockingContact,
                int blockingContactCount,
                DeterministicKccQuerySummary querySummary,
                DeterministicKccBodyState previousState,
                long elapsedStopwatchTicks)
            {
                Request = request ?? throw new ArgumentNullException(nameof(request));
                Requested = requested;
                Position = position;
                Ground = ground;
                Collision = collision;
                StepPhase = stepPhase;
                StepRejection = stepRejection;
                Remaining = remaining;
                MovementIterations = movementIterations;
                HasBlockingContact = hasBlockingContact;
                BlockingContact = blockingContact;
                BlockingContactCount = blockingContactCount;
                QuerySummary = querySummary;
                PreviousState = previousState;
                ElapsedStopwatchTicks = elapsedStopwatchTicks;
            }

            public CharacterWorldSolveRequest Request;
            public FixedVector3 Requested;
            public FixedVector3 Position;
            public DeterministicKccGroundReport Ground;
            public WorldCollisionSummary Collision;
            public DeterministicKccStepPhase StepPhase;
            public DeterministicKccStepRejection StepRejection;
            public FixedVector3 Remaining;
            public int MovementIterations;
            public bool HasBlockingContact;
            public DeterministicKccContact BlockingContact;
            public int BlockingContactCount;
            public DeterministicKccQuerySummary QuerySummary;
            public DeterministicKccBodyState PreviousState;
            public long ElapsedStopwatchTicks;
        }
    }
}
