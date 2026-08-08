using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    public sealed partial class DeterministicKccWorldSolver
    {
        void PublishNoProgressDiagnostics(
            ISimulationDiagnosticsSink diagnostics,
            SimulationTick tick,
            ActorId actorId,
            DeterministicKccMotorResult result)
        {
            if (!diagnostics.IsEnabled ||
                result.Termination != DeterministicKccMovementTermination.BlockedNoProgress)
            {
                return;
            }
            var contacts = new StringBuilder();
            for (int i = 0; i < result.NoProgressContactCount; i++)
            {
                if (i > 0)
                    contacts.Append('|');
                DeterministicKccContact contact = result.NoProgressContactAt(i);
                contacts
                    .Append(contact.SurfaceId).Append(':')
                    .Append(contact.PrimitiveId).Append(':')
                    .Append(contact.FeatureId.Kind).Append(':')
                    .Append(contact.FeatureId.Index).Append(':')
                    .Append(contact.Normal.X.Raw).Append(',')
                    .Append(contact.Normal.Y.Raw).Append(',')
                    .Append(contact.Normal.Z.Raw);
            }
            diagnostics.PublishWorld(new SimulationWorldTraceRecord(
                SimulationWorldTraceKind.Collision,
                "deterministic_kcc_blocked_no_progress",
                $"confirmations={result.NoProgressConfirmationCount};contacts={contacts}",
                tick,
                actorId,
                Descriptor.ImplementationId,
                Descriptor.Version,
                traversalCount: result.NoProgressContactCount,
                resolveStatus: (uint)result.MovementIterations,
                disposition: "blocked-no-progress"));
        }

        void PublishDiagnostics(
            ISimulationDiagnosticsSink diagnostics,
            SimulationTick tick,
            ActorId actorId,
            FixedVector3 requested,
            FixedVector3 applied,
            DeterministicKccGroundReport ground,
            DeterministicKccStepDiagnostics stepDiagnostics,
            FixedVector3 remaining,
            int movementIterations,
            bool hasBlockingContact,
            DeterministicKccContact blockingContact,
            int blockingContactCount,
            DeterministicKccMovementTermination termination,
            int noProgressConfirmationCount,
            DeterministicKccQuerySummary summary,
            long elapsedStopwatchTicks)
        {
            if (!diagnostics.IsEnabled)
                return;
            string feature = ground.FeatureId.IsValid
                ? $"{ground.FeatureId.Kind}:{ground.FeatureId.Index}"
                : "none";
            string hit = hasBlockingContact
                ? $"{blockingContact.SurfaceId}:{blockingContact.PrimitiveId}:{blockingContact.FeatureId.Kind}:{blockingContact.FeatureId.Index};toiRaw={blockingContact.TimeOfImpact.Raw};normalRaw={blockingContact.Normal.X.Raw},{blockingContact.Normal.Y.Raw},{blockingContact.Normal.Z.Raw}"
                : "none";
            diagnostics.PublishWorld(new SimulationWorldTraceRecord(
                SimulationWorldTraceKind.Collision,
                "deterministic_kcc_resolve",
                $"candidates={summary.CandidateCount};contacts={summary.ContactCount};queryIterations={summary.IterationCount};movementIterations={movementIterations};representativeEvaluations={stepDiagnostics.RepresentativeEvaluationCount};stabilityEvaluations={stepDiagnostics.StabilityEvaluationCount};stepAttempts={stepDiagnostics.StepDetectionAttemptCount};standardStepQueries={stepDiagnostics.StandardStepQueryCount};extraStepQueries={stepDiagnostics.ExtraStepQueryCount};stepValidityCandidates={stepDiagnostics.StepValidityCandidateCount};groundProbeEvaluations={stepDiagnostics.GroundProbeEvaluationCount};termination={termination};noProgressConfirmations={noProgressConfirmationCount};foundAnyGround={ground.FoundAnyGround};baseStable={ground.BaseIsStable};grounded={ground.IsStableOnGround};support={ground.SurfaceId}:{ground.PrimitiveId}:{feature};groundNormalRaw={ground.GroundNormal.X.Raw},{ground.GroundNormal.Y.Raw},{ground.GroundNormal.Z.Raw};innerNormalRaw={ground.InnerNormal.X.Raw},{ground.InnerNormal.Y.Raw},{ground.InnerNormal.Z.Raw};outerNormalRaw={ground.OuterNormal.X.Raw},{ground.OuterNormal.Y.Raw},{ground.OuterNormal.Z.Raw};probeDistanceRaw={ground.ProbeDistance.Raw};denivelationDotRaw={ground.DenivelationNormalDot.Raw};snappingPrevented={ground.SnappingPrevented};ledge={ground.LedgeState};lastMovementGround={ground.LastMovementIterationFoundAnyGround};stepMode={stepDiagnostics.Mode};stepStage={stepDiagnostics.Stage};stepRejection={stepDiagnostics.Rejection};steppedSurface={stepDiagnostics.SteppedSurfaceId};blockingContacts={blockingContactCount};hit={hit};remainingRaw={remaining.X.Raw},{remaining.Y.Raw},{remaining.Z.Raw}",
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
            false,
            -1,
            -1,
            DeterministicCollisionFeatureId.Invalid,
            FixedVector3.Zero,
            FixedVector3.Zero,
            FixedVector3.Zero,
            false,
            DeterministicKccLedgeState.None,
            false);

        static DeterministicKccBodyState CreateKccState(
            ActorId actorId,
            DeterministicKccMotorResult result) => new DeterministicKccBodyState(
            actorId,
            result.Ground.FoundAnyGround,
            result.Ground.IsStableOnGround,
            result.Ground.SurfaceId,
            result.Ground.PrimitiveId,
            result.Ground.FeatureId,
            result.Ground.GroundNormal,
            result.Ground.InnerNormal,
            result.Ground.OuterNormal,
            result.Ground.SnappingPrevented,
            result.Ground.LedgeState,
            result.Ground.LastMovementIterationFoundAnyGround);

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
                DeterministicKccStepDiagnostics stepDiagnostics,
                FixedVector3 remaining,
                int movementIterations,
                bool hasBlockingContact,
                DeterministicKccContact blockingContact,
                int blockingContactCount,
                DeterministicKccMovementTermination termination,
                int noProgressConfirmationCount,
                DeterministicKccQuerySummary querySummary,
                DeterministicKccBodyState previousState,
                long elapsedStopwatchTicks)
            {
                Request = request ?? throw new ArgumentNullException(nameof(request));
                Requested = requested;
                Position = position;
                Ground = ground;
                Collision = collision;
                StepDiagnostics = stepDiagnostics;
                Remaining = remaining;
                MovementIterations = movementIterations;
                HasBlockingContact = hasBlockingContact;
                BlockingContact = blockingContact;
                BlockingContactCount = blockingContactCount;
                Termination = termination;
                NoProgressConfirmationCount = noProgressConfirmationCount;
                QuerySummary = querySummary;
                PreviousState = previousState;
                ElapsedStopwatchTicks = elapsedStopwatchTicks;
            }

            public CharacterWorldSolveRequest Request;
            public FixedVector3 Requested;
            public FixedVector3 Position;
            public DeterministicKccGroundReport Ground;
            public WorldCollisionSummary Collision;
            public DeterministicKccStepDiagnostics StepDiagnostics;
            public FixedVector3 Remaining;
            public int MovementIterations;
            public bool HasBlockingContact;
            public DeterministicKccContact BlockingContact;
            public int BlockingContactCount;
            public DeterministicKccMovementTermination Termination;
            public int NoProgressConfirmationCount;
            public DeterministicKccQuerySummary QuerySummary;
            public DeterministicKccBodyState PreviousState;
            public long ElapsedStopwatchTicks;
        }
    }
}
