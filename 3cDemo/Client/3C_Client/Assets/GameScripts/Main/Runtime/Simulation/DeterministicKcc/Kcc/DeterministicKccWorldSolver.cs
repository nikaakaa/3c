using System;
using System.Collections.Generic;
using System.Diagnostics;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    public sealed partial class DeterministicKccWorldSolver : ICharacterWorldSolver
    {
        public const string SolverId = "thirdperson.simulation.solver.deterministic-kcc";
        public const string SolverVersion = "10";

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
                "deterministic-kcc/8",
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
                PublishNoProgressDiagnostics(
                    diagnostics,
                    request.Tick,
                    actorRequest.ActorId,
                    motorResult);
                m_Candidates[i] = new ActorSolveCandidate(
                    actorRequest,
                    requested,
                    motorResult.Position,
                    motorResult.Ground,
                    motorResult.Collision,
                    motorResult.StepDiagnostics,
                    motorResult.RemainingDisplacement,
                    motorResult.MovementIterations,
                    motorResult.HasBlockingContact,
                    motorResult.HasBlockingContact ? motorResult.BlockingContactAt(0) : default,
                    motorResult.BlockingContactCount,
                    motorResult.Termination,
                    motorResult.NoProgressConfirmationCount,
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
                    candidate.Ground.FoundAnyGround,
                    candidate.Ground.IsStableOnGround,
                    candidate.Ground.SurfaceId,
                    candidate.Ground.PrimitiveId,
                    candidate.Ground.FeatureId,
                    candidate.Ground.GroundNormal,
                    candidate.Ground.InnerNormal,
                    candidate.Ground.OuterNormal,
                    candidate.Ground.SnappingPrevented,
                    candidate.Ground.LedgeState,
                    candidate.Ground.LastMovementIterationFoundAnyGround);
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
                    candidate.StepDiagnostics,
                    candidate.Remaining,
                    candidate.MovementIterations,
                    candidate.HasBlockingContact,
                    candidate.BlockingContact,
                    candidate.BlockingContactCount,
                    candidate.Termination,
                    candidate.NoProgressConfirmationCount,
                    querySummary,
                    candidate.ElapsedStopwatchTicks);
            }
            m_KccStates = states;
            m_Current = CreateState(request.BeforeWorldState.WorldRevision, bodies, states);
            return new WorldSolveBatchResult(request, Descriptor.ImplementationId, Descriptor.Version, CloneState(m_Current), results);
        }

    }
}
