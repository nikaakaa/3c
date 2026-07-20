using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Detour.Io;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.DotRecast
{
    public readonly struct DotRecastBodyBindingDescriptor
    {
        public DotRecastBodyBindingDescriptor(
            string bindingId,
            WorldBodyState initialBody,
            ActorContactShape contactShape)
        {
            BindingId = NavigationGeometrySource.RequireIdentity(bindingId, nameof(bindingId));
            if (!initialBody.ActorId.IsValid)
                throw new ArgumentException("Initial body ActorId is invalid.", nameof(initialBody));
            InitialBody = initialBody;
            ContactShape = contactShape;
            ConfigurationHash = StableHash.Compute(
                "thirdperson.dotrecast.body-binding/2",
                BindingId,
                initialBody.ActorId.Value,
                contactShape.ConfigurationHash.Value);
        }

        public string BindingId { get; }
        public ActorId ActorId => InitialBody.ActorId;
        public WorldBodyState InitialBody { get; }
        public ActorContactShape ContactShape { get; }
        public StableHash ConfigurationHash { get; }
    }

    public sealed class DotRecastWorldSolver : ICharacterWorldSolver, IObservedWorldConstraintProfileProvider
    {
        readonly struct SurfaceCandidate
        {
            public SurfaceCandidate(
                CharacterWorldSolveRequest request,
                Float32Vector3 requestedDisplacement,
                Float32Vector3 position,
                Float32Yaw yaw,
                Float32Scalar appliedYaw,
                bool boundaryClamped,
                long startPolygon,
                int visitedCount,
                DtStatus localizationStatus,
                DtStatus moveStatus,
                long startedAt)
            {
                Request = request;
                RequestedDisplacement = requestedDisplacement;
                Position = position;
                Yaw = yaw;
                AppliedYaw = appliedYaw;
                BoundaryClamped = boundaryClamped;
                StartPolygon = startPolygon;
                VisitedCount = visitedCount;
                LocalizationStatus = localizationStatus;
                MoveStatus = moveStatus;
                StartedAt = startedAt;
            }

            public CharacterWorldSolveRequest Request { get; }
            public Float32Vector3 RequestedDisplacement { get; }
            public Float32Vector3 Position { get; }
            public Float32Yaw Yaw { get; }
            public Float32Scalar AppliedYaw { get; }
            public bool BoundaryClamped { get; }
            public long StartPolygon { get; }
            public int VisitedCount { get; }
            public DtStatus LocalizationStatus { get; }
            public DtStatus MoveStatus { get; }
            public long StartedAt { get; }
        }

        readonly struct SurfaceReconstraint
        {
            public SurfaceReconstraint(Float32Vector3 position, long polygon, int area, DtStatus status)
            {
                Position = position;
                Polygon = polygon;
                Area = area;
                Status = status;
            }

            public Float32Vector3 Position { get; }
            public long Polygon { get; }
            public int Area { get; }
            public DtStatus Status { get; }
        }

        public const string ImplementationIdentity = "DotRecast.NavigationSurface.WorldSolver";
        public const string SolverVersion = "4";

        static readonly SolverImplementationId s_ImplementationId = new SolverImplementationId(ImplementationIdentity);
        static readonly CharacterWorldSolverDescriptor s_Descriptor = new CharacterWorldSolverDescriptor(
            Float32SimulationNumericProfile.Value,
            s_ImplementationId,
            SolverVersion,
            WorldCapability.BodyMotion | WorldCapability.Grounding | WorldCapability.Collision | WorldCapability.Reconstructible,
            WorldFeature.Ground |
            WorldFeature.Slope |
            WorldFeature.Step |
            WorldFeature.ActorCollision |
            WorldFeature.NavigationSurface |
            WorldFeature.ObservedKinematicActorContact);
        readonly int m_TickRate;
        readonly NavigationSurfaceArtifact m_Surface;
        readonly DotRecastBodyBindingDescriptor[] m_Bindings;
        readonly ActorContactShape m_ContactShape;
        readonly ActorContactSolverConfiguration m_ContactConfiguration;
        readonly DtNavMesh m_NavMesh;
        readonly DtNavMeshQuery m_Query;
        readonly DtQueryDefaultFilter m_Filter;
        WorldSimulationState m_Current;
        bool m_Disposed;

        public DotRecastWorldSolver(
            int tickRate,
            byte[] surfaceBytes,
            ActorContactSolverConfiguration contactConfiguration,
            IEnumerable<DotRecastBodyBindingDescriptor> bindings)
        {
            if (tickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickRate));
            m_TickRate = tickRate;
            m_Surface = NavigationSurfaceArtifactCodec.Read(surfaceBytes);
            m_Bindings = bindings == null ? Array.Empty<DotRecastBodyBindingDescriptor>() : new List<DotRecastBodyBindingDescriptor>(bindings).ToArray();
            if (m_Bindings.Length == 0)
                throw new ArgumentException("DotRecast World Solver requires an explicit Actor roster.", nameof(bindings));
            Array.Sort(m_Bindings, (left, right) => left.ActorId.CompareTo(right.ActorId));
            for (int i = 1; i < m_Bindings.Length; i++)
            {
                if (m_Bindings[i - 1].ActorId == m_Bindings[i].ActorId ||
                    string.Equals(m_Bindings[i - 1].BindingId, m_Bindings[i].BindingId, StringComparison.Ordinal))
                    throw new ArgumentException("DotRecast World Solver contains duplicate ActorId or binding identity.", nameof(bindings));
            }
            m_ContactShape = m_Bindings[0].ContactShape;
            for (int i = 1; i < m_Bindings.Length; i++)
            {
                if (m_Bindings[i].ContactShape != m_ContactShape)
                    throw new ArgumentException("DotRecast World Solver requires one canonical contact shape for the locked roster.", nameof(bindings));
            }
            m_ContactConfiguration = contactConfiguration;
            using (var stream = new MemoryStream(m_Surface.NavMeshBytes, false))
            using (var reader = new BinaryReader(stream))
                m_NavMesh = new DtMeshSetReader().Read(reader);
            m_Query = new DtNavMeshQuery(m_NavMesh);
            var areaCosts = new float[m_Surface.QueryProfile.AreaCosts.Count];
            for (int i = 0; i < areaCosts.Length; i++)
                areaCosts[i] = CheckedFloat(m_Surface.QueryProfile.AreaCosts[i], $"area-cost/{i}");
            m_Filter = new DtQueryDefaultFilter(
                m_Surface.QueryProfile.IncludeFlags,
                m_Surface.QueryProfile.ExcludeFlags,
                areaCosts);
            Descriptor = s_Descriptor;
        }

        public static CharacterWorldSolverDescriptor DescriptorDefinition => s_Descriptor;
        public CharacterWorldSolverDescriptor Descriptor { get; }
        public NavigationSurfaceArtifact Surface => m_Surface;
        public ActorContactShape ContactShape => m_ContactShape;
        public ActorContactSolverConfiguration ContactConfiguration => m_ContactConfiguration;
        public StableHash ObservedContactShapeConfigurationHash => m_ContactShape.ConfigurationHash;
        public StableHash WorldConfigurationHash => DotRecastWorldConfigurationIdentity.Compute(
            m_Surface.WorldConfigurationHash,
            m_ContactShape,
            m_ContactConfiguration);

        public void RequireBodyBinding(ActorId actorId, string bindingId)
        {
            RequireAlive();
            string required = NavigationGeometrySource.RequireIdentity(bindingId, nameof(bindingId));
            int index = FindBinding(actorId);
            if (index < 0)
                throw new InvalidOperationException($"DotRecast World Solver has no binding for Actor '{actorId}'.");
            if (!string.Equals(m_Bindings[index].BindingId, required, StringComparison.Ordinal))
                throw new InvalidOperationException($"Actor '{actorId}' expects World binding '{required}', but Solver owns '{m_Bindings[index].BindingId}'.");
        }

        public WorldSimulationState Create(WorldRevision worldRevision, IReadOnlyList<WorldBodyState> orderedInitialBodies)
        {
            RequireAlive();
            RequireWorldRevision(worldRevision);
            if (orderedInitialBodies == null || orderedInitialBodies.Count != m_Bindings.Length)
                throw new ArgumentException("Initial body roster does not match DotRecast bindings.", nameof(orderedInitialBodies));
            for (int i = 0; i < orderedInitialBodies.Count; i++)
            {
                if (orderedInitialBodies[i].ActorId != m_Bindings[i].ActorId)
                    throw new ArgumentException("Initial body order does not match DotRecast bindings.", nameof(orderedInitialBodies));
                RequireLocalized(orderedInitialBodies[i], "initial");
            }
            m_Current = CreateState(worldRevision, orderedInitialBodies);
            return CloneState(m_Current);
        }

        public void Reconstruct(WorldSimulationState state)
        {
            RequireAlive();
            ValidateState(state);
            for (int i = 0; i < state.Bodies.Count; i++)
                RequireLocalized(state.Bodies[i], "reconstruct");
            m_Current = CloneState(state);
        }

        public WorldSimulationState Capture(WorldRevision worldRevision)
        {
            RequireAlive();
            RequireCurrent();
            if (!worldRevision.Equals(m_Current.WorldRevision))
                throw new InvalidOperationException("DotRecast World Solver cannot capture another WorldRevision.");
            return CloneState(m_Current);
        }

        public void Restore(WorldSimulationState state)
        {
            Reconstruct(state);
        }

        public WorldSolveBatchResult ResolveBatch(WorldSolveBatchRequest request, ISimulationDiagnosticsSink diagnostics)
        {
            RequireAlive();
            RequireCurrent();
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (diagnostics == null)
                throw new ArgumentNullException(nameof(diagnostics));
            if (!StateEquals(request.BeforeWorldState, m_Current))
                throw new InvalidOperationException("DotRecast batch does not match the committed World state.");
            if (!Descriptor.Supports(request.RequiredCapabilities))
                throw new InvalidOperationException($"DotRecast World Solver is missing required capabilities '{request.RequiredCapabilities & ~Descriptor.Capabilities}'.");
            if (request.Requests.Count != m_Bindings.Length)
                throw new InvalidOperationException("DotRecast batch Actor roster does not match its locked bindings.");
            if (diagnostics.IsEnabled)
            {
                diagnostics.PublishWorld(new SimulationWorldTraceRecord(
                    SimulationWorldTraceKind.Query,
                    "dotrecast.observed-world-request",
                    $"observedFrame={request.ObservedWorldConstraints.FrameHash};request={request.RequestHash};observedCount={request.ObservedWorldConstraints.Constraints.Count}",
                    request.Tick,
                    request.Requests[0].ActorId,
                    Descriptor.ImplementationId,
                    Descriptor.Version));
            }
            var surfaceCandidates = new SurfaceCandidate[request.Requests.Count];
            var contactCandidates = new List<ActorContactCandidate>(
                request.Requests.Count + request.ObservedWorldConstraints.Constraints.Count);
            for (int i = 0; i < request.Requests.Count; i++)
            {
                CharacterWorldSolveRequest actorRequest = request.Requests[i];
                if (actorRequest.ActorId != m_Bindings[i].ActorId || !BodyEquals(actorRequest.BeforeBody, m_Current.Bodies[i]))
                    throw new InvalidOperationException("DotRecast Actor request does not match its locked binding and before-body state.");
                surfaceCandidates[i] = SolveSurfaceCandidate(actorRequest, diagnostics);
                contactCandidates.Add(new ActorContactCandidate(
                    actorRequest.ActorId,
                    actorRequest.BeforeBody.Position,
                    surfaceCandidates[i].Position,
                    m_Bindings[i].ContactShape,
                    ActorContactMobility.ActiveSimulated));
            }
            for (int i = 0; i < request.ObservedWorldConstraints.Constraints.Count; i++)
            {
                ObservedWorldConstraint observed = request.ObservedWorldConstraints.Constraints[i];
                if (observed.TargetTick != request.Tick ||
                    observed.ContactShapeConfigurationHash != m_ContactShape.ConfigurationHash)
                {
                    throw new InvalidOperationException(
                        $"Observed Actor '{observed.ActorId}' does not match the DotRecast batch Tick or canonical contact shape.");
                }
                contactCandidates.Add(new ActorContactCandidate(
                    observed.ActorId,
                    observed.BeforeBody.Position,
                    observed.FinalBody.Position,
                    m_ContactShape,
                    ActorContactMobility.ObservedKinematic));
            }
            contactCandidates.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));

            ActorContactBatchResult contactResult;
            try
            {
                contactResult = ActorContactSolver.Resolve(contactCandidates, m_ContactConfiguration);
            }
            catch (ActorContactSolveException exception)
            {
                PublishContactTraces(request, exception.Traces, diagnostics, false);
                throw;
            }

            var finalPositions = new Float32Vector3[contactCandidates.Count];
            for (int i = 0; i < finalPositions.Length; i++)
                finalPositions[i] = contactResult.PositionAt(i);
            var reconstraints = new SurfaceReconstraint[request.Requests.Count];
            var activeContactIndexes = new int[request.Requests.Count];
            for (int i = 0; i < request.Requests.Count; i++)
            {
                int contactIndex = FindContactCandidate(contactCandidates, request.Requests[i].ActorId);
                activeContactIndexes[i] = contactIndex;
                reconstraints[i] = ReconstraintToSurface(
                    request.Requests[i].ActorId,
                    contactResult.PositionAt(contactIndex),
                    request.Requests[i].Tick,
                    diagnostics);
                finalPositions[contactIndex] = reconstraints[i].Position;
            }
            IReadOnlyList<ActorContactTrace> finalValidationTraces;
            try
            {
                finalValidationTraces = ActorContactSolver.ValidateFinal(
                    contactCandidates,
                    finalPositions,
                    m_ContactConfiguration);
            }
            catch (ActorContactSolveException exception)
            {
                PublishContactTraces(request, exception.Traces, diagnostics, false);
                throw;
            }
            PublishContactTraces(request, contactResult.Traces, diagnostics, true);
            PublishContactTraces(request, finalValidationTraces, diagnostics, true);

            var bodies = new WorldBodyState[request.Requests.Count];
            var results = new CharacterWorldSolveResult[request.Requests.Count];
            for (int i = 0; i < request.Requests.Count; i++)
            {
                BuildFinalResult(
                    surfaceCandidates[i],
                    reconstraints[i],
                    contactResult.HadContactAt(activeContactIndexes[i]),
                    diagnostics,
                    out bodies[i],
                    out results[i]);
            }
            m_Current = CreateState(request.BeforeWorldState.WorldRevision, bodies);
            return new WorldSolveBatchResult(request, Descriptor.ImplementationId, Descriptor.Version, CloneState(m_Current), results);
        }

        static int FindContactCandidate(IReadOnlyList<ActorContactCandidate> candidates, ActorId actorId)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].ActorId == actorId)
                    return i;
            }
            throw new InvalidOperationException($"Actor contact candidate '{actorId}' is missing from the stable roster.");
        }

        SurfaceCandidate SolveSurfaceCandidate(
            CharacterWorldSolveRequest request,
            ISimulationDiagnosticsSink diagnostics)
        {
            long startedAt = diagnostics.IsEnabled ? Stopwatch.GetTimestamp() : 0;
            WorldBodyState before = request.BeforeBody;
            Float32Vector3 requested = request.Motion.Space == WorldMotionSpace.ActorLocal
                ? Float32Angle.RotatePlanar(request.Motion.Displacement, before.Yaw)
                : request.Motion.Displacement;
            if (!request.Motion.HasMotion)
                requested = Float32Vector3.Zero;
            if (requested.Y != Float32Scalar.Zero)
            {
                throw new InvalidOperationException(
                    $"DotRecast Actor '{request.ActorId}' received vertical displacement '{requested.Y}', " +
                    $"but Solver '{Descriptor.ImplementationId}@{Descriptor.Version}' does not support '{WorldCapability.AirborneVerticalMotion}'.");
            }
            if (requested.Magnitude.ToDouble() > m_Surface.QueryProfile.MaximumDisplacement)
                throw new InvalidOperationException($"DotRecast Actor '{request.ActorId}' exceeded maximum per-tick displacement.");

            RcVec3f beforePosition = ToRecast(before.Position);
            RcVec3f extents = new RcVec3f(
                CheckedFloat(m_Surface.QueryProfile.NearestExtentX, "nearest-extent/x"),
                CheckedFloat(m_Surface.QueryProfile.NearestExtentY, "nearest-extent/y"),
                CheckedFloat(m_Surface.QueryProfile.NearestExtentZ, "nearest-extent/z"));
            DtStatus nearestStatus = m_Query.FindNearestPoly(beforePosition, extents, m_Filter, out long startPolygon, out RcVec3f nearestPoint, out _);
            if (nearestStatus.Failed() || startPolygon == 0)
                throw new InvalidOperationException($"DotRecast could not localize Actor '{request.ActorId}' on the navigation surface.");
            if (Distance(beforePosition, nearestPoint) > m_Surface.QueryProfile.ProjectionTolerance)
                throw new InvalidOperationException($"DotRecast Actor '{request.ActorId}' exceeded projection tolerance.");

            RcVec3f requestedEnd = new RcVec3f(
                beforePosition.X + requested.X.ToSingle(),
                beforePosition.Y + requested.Y.ToSingle(),
                beforePosition.Z + requested.Z.ToSingle());
            var visited = new long[m_Surface.QueryProfile.MaximumVisitedPolygons];
            DtStatus moveStatus = m_Query.MoveAlongSurface(
                startPolygon,
                nearestPoint,
                requestedEnd,
                m_Filter,
                out RcVec3f moved,
                visited,
                out int visitedCount,
                visited.Length);
            if (moveStatus.Failed() || moveStatus.Has(DtStatus.DT_BUFFER_TOO_SMALL) || visitedCount <= 0)
                throw new InvalidOperationException($"DotRecast surface move failed for Actor '{request.ActorId}'.");
            Float32Vector3 unresolvedPosition = ToSimulation(moved);
            Float32Vector3 unresolvedApplied = unresolvedPosition - before.Position;
            Float32Scalar planarError = new Float32Vector2(requested.X - unresolvedApplied.X, requested.Z - unresolvedApplied.Z).Magnitude;
            bool boundaryClamped = planarError.ToDouble() > m_Surface.QueryProfile.BoundaryInset;
            RcVec3f projectionCandidate = moved;
            if (boundaryClamped)
            {
                double directionX = nearestPoint.X - moved.X;
                double directionZ = nearestPoint.Z - moved.Z;
                double directionLength = Math.Sqrt(directionX * directionX + directionZ * directionZ);
                if (directionLength > 0d)
                {
                    double inset = Math.Min(m_Surface.QueryProfile.BoundaryInset, directionLength * 0.5d);
                    projectionCandidate.X += CheckedFloat(directionX / directionLength * inset, "boundary-inset/x");
                    projectionCandidate.Z += CheckedFloat(directionZ / directionLength * inset, "boundary-inset/z");
                }
            }
            DtStatus projectionStatus = m_Query.FindNearestPoly(
                projectionCandidate,
                extents,
                m_Filter,
                out long finalPolygon,
                out RcVec3f projectedPoint,
                out _);
            if (projectionStatus.Failed() || finalPolygon == 0)
                throw new InvalidOperationException($"DotRecast final surface localization failed for Actor '{request.ActorId}'.");
            if (PlanarDistance(projectionCandidate, projectedPoint) > m_Surface.QueryProfile.BoundaryInset ||
                Math.Abs(projectedPoint.Y - projectionCandidate.Y) > m_Surface.QueryProfile.HeightTolerance)
            {
                throw new InvalidOperationException($"DotRecast final surface localization exceeded tolerance for Actor '{request.ActorId}'.");
            }

            Float32Vector3 finalPosition = ToSimulation(projectedPoint);
            Float32Yaw finalYaw = request.Motion.HasMotion
                ? new Float32Yaw(before.Yaw.Degrees + request.Motion.YawDegrees)
                : before.Yaw;
            Float32Scalar appliedYaw = Float32Angle.Delta(before.Yaw, finalYaw);
            return new SurfaceCandidate(
                request,
                requested,
                finalPosition,
                finalYaw,
                appliedYaw,
                boundaryClamped,
                startPolygon,
                visitedCount,
                nearestStatus,
                moveStatus,
                startedAt);
        }

        SurfaceReconstraint ReconstraintToSurface(
            ActorId actorId,
            Float32Vector3 contactPosition,
            SimulationTick tick,
            ISimulationDiagnosticsSink diagnostics)
        {
            RcVec3f candidate = ToRecast(contactPosition);
            RcVec3f extents = QueryExtents();
            DtStatus status = m_Query.FindNearestPoly(candidate, extents, m_Filter, out long polygon, out RcVec3f projected, out _);
            if (status.Failed() || polygon == 0)
                throw new InvalidOperationException($"DotRecast contact surface reconstraint failed for Actor '{actorId}'.");
            double maximumPlanarCorrection = Math.Max(
                m_Surface.QueryProfile.BoundaryInset,
                m_ContactConfiguration.MaximumDepenetrationDistance.ToDouble() +
                m_ContactConfiguration.ContactTolerance.ToDouble());
            if (PlanarDistance(candidate, projected) > maximumPlanarCorrection ||
                Math.Abs(projected.Y - candidate.Y) > m_Surface.QueryProfile.HeightTolerance)
            {
                throw new InvalidOperationException($"DotRecast contact surface reconstraint exceeded tolerance for Actor '{actorId}'.");
            }
            DtStatus referenceStatus = m_NavMesh.GetTileAndPolyByRef(polygon, out _, out DtPoly poly);
            if (referenceStatus.Failed() || poly == null)
                throw new InvalidOperationException($"DotRecast contact surface polygon metadata failed for Actor '{actorId}'.");
            Float32Vector3 position = ToSimulation(projected);
            if (diagnostics.IsEnabled)
            {
                diagnostics.PublishWorld(new SimulationWorldTraceRecord(
                    SimulationWorldTraceKind.Projection,
                    "dotrecast.surface-reconstraint",
                    $"actor={actorId}",
                    tick,
                    actorId,
                    Descriptor.ImplementationId,
                    Descriptor.Version,
                    0,
                    polygon,
                    poly.GetArea(),
                    1,
                    m_Filter.GetIncludeFlags(),
                    m_Filter.GetExcludeFlags(),
                    0,
                    0,
                    status.Value,
                    0,
                    Float32Vector3.Zero,
                    position - contactPosition,
                    "actor-contact-surface-reconstraint"));
            }
            return new SurfaceReconstraint(position, polygon, poly.GetArea(), status);
        }

        void BuildFinalResult(
            SurfaceCandidate candidate,
            SurfaceReconstraint reconstraint,
            bool actorContact,
            ISimulationDiagnosticsSink diagnostics,
            out WorldBodyState finalBody,
            out CharacterWorldSolveResult result)
        {
            CharacterWorldSolveRequest request = candidate.Request;
            Float32Vector3 applied = reconstraint.Position - request.BeforeBody.Position;
            Float32Vector3 velocity = applied * Float32Scalar.FromInt64(m_TickRate);
            WorldCollisionSummary collision = WorldCollisionSummary.Below;
            if (candidate.BoundaryClamped || actorContact)
                collision |= WorldCollisionSummary.Sides;
            finalBody = new WorldBodyState(
                request.ActorId,
                reconstraint.Position,
                candidate.Yaw,
                velocity,
                request.BeforeBody.VerticalVelocity,
                true,
                collision);
            result = new CharacterWorldSolveResult(
                Descriptor.NumericProfile,
                request.ActorId,
                request.RequestId,
                request.Tick,
                Descriptor.ImplementationId,
                finalBody,
                applied,
                candidate.AppliedYaw);
            if (diagnostics.IsEnabled)
            {
                diagnostics.PublishWorld(new SimulationWorldTraceRecord(
                    SimulationWorldTraceKind.Query,
                    "dotrecast.surface-query",
                    m_Surface.WorldRevision,
                    request.Tick,
                    request.ActorId,
                    Descriptor.ImplementationId,
                    Descriptor.Version,
                    candidate.StartPolygon,
                    reconstraint.Polygon,
                    reconstraint.Area,
                    candidate.VisitedCount,
                    m_Filter.GetIncludeFlags(),
                    m_Filter.GetExcludeFlags(),
                    candidate.LocalizationStatus.Value,
                    candidate.MoveStatus.Value,
                    reconstraint.Status.Value,
                    Stopwatch.GetTimestamp() - candidate.StartedAt,
                    candidate.RequestedDisplacement,
                    applied,
                    actorContact
                        ? "actor-contact"
                        : candidate.BoundaryClamped ? "surface-boundary" : string.Empty));
            }
        }

        void PublishContactTraces(
            WorldSolveBatchRequest request,
            IReadOnlyList<ActorContactTrace> traces,
            ISimulationDiagnosticsSink diagnostics,
            bool success)
        {
            if (!diagnostics.IsEnabled)
                return;
            for (int i = 0; i < traces.Count; i++)
            {
                ActorContactTrace trace = traces[i];
                PublishContactTrace(
                    request.Requests[0].Tick,
                    trace,
                    trace.ActorA,
                    trace.ActorB,
                    trace.Normal,
                    trace.CorrectionA,
                    diagnostics,
                    success);
                if (trace.ActorB != trace.ActorA)
                {
                    PublishContactTrace(
                        request.Requests[0].Tick,
                        trace,
                        trace.ActorB,
                        trace.ActorA,
                        new Float32Vector3(-trace.Normal.X, -trace.Normal.Y, -trace.Normal.Z),
                        trace.CorrectionB,
                        diagnostics,
                        success);
                }
            }
        }

        void PublishContactTrace(
            SimulationTick tick,
            ActorContactTrace trace,
            ActorId actorId,
            ActorId otherActorId,
            Float32Vector3 normal,
            Float32Vector3 correction,
            ISimulationDiagnosticsSink diagnostics,
            bool success)
        {
            diagnostics.PublishWorld(new SimulationWorldTraceRecord(
                success ? SimulationWorldTraceKind.Collision : SimulationWorldTraceKind.Failure,
                "dotrecast.actor-contact",
                $"pair={trace.ActorA}/{trace.ActorB};mobility={trace.MobilityA}/{trace.MobilityB};actor={actorId};other={otherActorId};kind={trace.Kind};detail={trace.Detail}",
                tick,
                actorId,
                Descriptor.ImplementationId,
                Descriptor.Version,
                trace.Iteration,
                trace.TimeOfImpact.Bits,
                (int)trace.Kind,
                1,
                0,
                0,
                0,
                0,
                0,
                0,
                normal,
                correction,
                trace.Detail,
                success));
        }

        void RequireLocalized(WorldBodyState body, string phase)
        {
            RcVec3f position = ToRecast(body.Position);
            var profile = m_Surface.QueryProfile;
            var extents = new RcVec3f(CheckedFloat(profile.NearestExtentX, "nearest/x"), CheckedFloat(profile.NearestExtentY, "nearest/y"), CheckedFloat(profile.NearestExtentZ, "nearest/z"));
            DtStatus status = m_Query.FindNearestPoly(position, extents, m_Filter, out long polygon, out RcVec3f nearest, out _);
            if (status.Failed() || polygon == 0 || Distance(position, nearest) > profile.ProjectionTolerance)
                throw new InvalidOperationException($"DotRecast {phase} body '{body.ActorId}' is outside the configured navigation surface tolerance.");
        }

        WorldSimulationState CreateState(WorldRevision worldRevision, IReadOnlyList<WorldBodyState> bodies)
        {
            return new WorldSimulationState(
                Descriptor.NumericProfile,
                Descriptor.ImplementationId,
                Descriptor.Version,
                worldRevision,
                WorldStatePersistenceMode.Reconstruct,
                bodies,
                Array.Empty<byte>());
        }

        void ValidateState(WorldSimulationState state)
        {
            if (state == null || state.NumericProfile != Descriptor.NumericProfile || !state.SolverId.Equals(Descriptor.ImplementationId) ||
                !string.Equals(state.SolverVersion, Descriptor.Version, StringComparison.Ordinal) ||
                state.PersistenceMode != WorldStatePersistenceMode.Reconstruct || state.SolverStatePayload.Length != 0 ||
                state.Bodies.Count != m_Bindings.Length)
                throw new InvalidOperationException("World state is incompatible with DotRecast World Solver.");
            RequireWorldRevision(state.WorldRevision);
            for (int i = 0; i < state.Bodies.Count; i++)
            {
                if (state.Bodies[i].ActorId != m_Bindings[i].ActorId)
                    throw new InvalidOperationException("World state Actor order does not match DotRecast bindings.");
            }
        }

        void RequireWorldRevision(WorldRevision revision)
        {
            if (!string.Equals(revision.Value, m_Surface.WorldRevision, StringComparison.Ordinal))
                throw new InvalidOperationException($"DotRecast surface WorldRevision '{m_Surface.WorldRevision}' does not match Session WorldRevision '{revision.Value}'.");
        }

        int FindBinding(ActorId actorId)
        {
            for (int i = 0; i < m_Bindings.Length; i++)
            {
                if (m_Bindings[i].ActorId == actorId)
                    return i;
            }
            return -1;
        }

        RcVec3f QueryExtents()
        {
            return new RcVec3f(
                CheckedFloat(m_Surface.QueryProfile.NearestExtentX, "nearest-extent/x"),
                CheckedFloat(m_Surface.QueryProfile.NearestExtentY, "nearest-extent/y"),
                CheckedFloat(m_Surface.QueryProfile.NearestExtentZ, "nearest-extent/z"));
        }

        static RcVec3f ToRecast(Float32Vector3 value)
        {
            return new RcVec3f(value.X.ToSingle(), value.Y.ToSingle(), value.Z.ToSingle());
        }

        static Float32Vector3 ToSimulation(RcVec3f value)
        {
            if (!value.IsFinite())
                throw new InvalidDataException("DotRecast returned a non-finite position.");
            return new Float32Vector3(Float32Scalar.FromSingle(value.X), Float32Scalar.FromSingle(value.Y), Float32Scalar.FromSingle(value.Z));
        }

        static double Distance(RcVec3f left, RcVec3f right)
        {
            double x = left.X - right.X;
            double y = left.Y - right.Y;
            double z = left.Z - right.Z;
            return Math.Sqrt(x * x + y * y + z * z);
        }

        static double PlanarDistance(RcVec3f left, RcVec3f right)
        {
            double x = left.X - right.X;
            double z = left.Z - right.Z;
            return Math.Sqrt(x * x + z * z);
        }

        static float CheckedFloat(double value, string identity)
        {
            NavigationGeometrySource.RequireFinite(value, identity);
            float result = (float)value;
            if (float.IsNaN(result) || float.IsInfinity(result))
                throw new InvalidDataException($"DotRecast value '{identity}' is outside Float32 range.");
            return result;
        }

        static WorldSimulationState CloneState(WorldSimulationState state)
        {
            return new WorldSimulationState(
                state.NumericProfile,
                state.SolverId,
                state.SolverVersion,
                state.WorldRevision,
                state.PersistenceMode,
                state.Bodies,
                state.SolverStatePayload.ToArray());
        }

        static bool StateEquals(WorldSimulationState left, WorldSimulationState right)
        {
            if (left == null || right == null || left.NumericProfile != right.NumericProfile || !left.SolverId.Equals(right.SolverId) ||
                !string.Equals(left.SolverVersion, right.SolverVersion, StringComparison.Ordinal) || !left.WorldRevision.Equals(right.WorldRevision) ||
                left.PersistenceMode != right.PersistenceMode || left.Bodies.Count != right.Bodies.Count ||
                left.SolverStatePayload.Length != right.SolverStatePayload.Length)
                return false;
            for (int i = 0; i < left.Bodies.Count; i++)
            {
                if (!BodyEquals(left.Bodies[i], right.Bodies[i]))
                    return false;
            }
            return true;
        }

        static bool BodyEquals(WorldBodyState left, WorldBodyState right)
        {
            return left.ActorId == right.ActorId && left.Position == right.Position && left.Yaw == right.Yaw &&
                   left.Velocity == right.Velocity && left.VerticalVelocity == right.VerticalVelocity &&
                   left.Grounded == right.Grounded && left.Collision == right.Collision;
        }

        void RequireCurrent()
        {
            if (m_Current == null)
                throw new InvalidOperationException("DotRecast World Solver has not been created or reconstructed.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(DotRecastWorldSolver));
        }

        public void Dispose()
        {
            m_Disposed = true;
            m_Current = null;
        }
    }
}
