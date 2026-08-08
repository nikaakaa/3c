using System;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    internal enum DeterministicKccMovementTermination
    {
        Completed = 0,
        BlockedNoProgress = 1
    }

    enum DeterministicKccConstraintCandidateKind
    {
        SinglePlane = 0,
        PlaneCrease = 1,
        Zero = 2
    }

    readonly struct DeterministicKccConstraintPlane
    {
        public DeterministicKccConstraintPlane(
            int primitiveId,
            DeterministicCollisionFeatureId featureId,
            FixedVector3 normal)
        {
            PrimitiveId = primitiveId;
            FeatureId = featureId;
            Normal = normal;
        }

        public int PrimitiveId { get; }
        public DeterministicCollisionFeatureId FeatureId { get; }
        public FixedVector3 Normal { get; }
    }

    internal readonly struct DeterministicKccMotorResult
    {
        public DeterministicKccMotorResult(
            FixedVector3 position,
            FixedVector3 appliedDisplacement,
            FixedVector3 remainingDisplacement,
            DeterministicKccGroundReport ground,
            WorldCollisionSummary collision,
            DeterministicKccStepDiagnostics stepDiagnostics,
            int movementIterations,
            DeterministicKccContact[] blockingContacts,
            int blockingContactCount,
            DeterministicKccMovementTermination termination,
            int noProgressConfirmationCount,
            DeterministicKccContact[] noProgressContacts,
            int noProgressContactCount,
            DeterministicKccQuerySummary querySummary)
        {
            Position = position;
            AppliedDisplacement = appliedDisplacement;
            RemainingDisplacement = remainingDisplacement;
            Ground = ground;
            Collision = collision;
            StepDiagnostics = stepDiagnostics;
            MovementIterations = movementIterations;
            m_BlockingContacts = blockingContacts ?? throw new ArgumentNullException(nameof(blockingContacts));
            BlockingContactCount = blockingContactCount;
            Termination = termination;
            NoProgressConfirmationCount = noProgressConfirmationCount;
            m_NoProgressContacts = noProgressContacts ?? throw new ArgumentNullException(nameof(noProgressContacts));
            NoProgressContactCount = noProgressContactCount;
            QuerySummary = querySummary;
        }

        readonly DeterministicKccContact[] m_BlockingContacts;
        readonly DeterministicKccContact[] m_NoProgressContacts;

        public FixedVector3 Position { get; }
        public FixedVector3 AppliedDisplacement { get; }
        public FixedVector3 RemainingDisplacement { get; }
        public DeterministicKccGroundReport Ground { get; }
        public WorldCollisionSummary Collision { get; }
        public DeterministicKccStepDiagnostics StepDiagnostics { get; }
        public int MovementIterations { get; }
        public int BlockingContactCount { get; }
        public bool HasBlockingContact => BlockingContactCount > 0;
        public DeterministicKccMovementTermination Termination { get; }
        public int NoProgressConfirmationCount { get; }
        public int NoProgressContactCount { get; }
        public DeterministicKccQuerySummary QuerySummary { get; }

        public DeterministicKccContact BlockingContactAt(int index)
        {
            if (index < 0 || index >= BlockingContactCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_BlockingContacts[index];
        }

        public DeterministicKccContact NoProgressContactAt(int index)
        {
            if (index < 0 || index >= NoProgressContactCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_NoProgressContacts[index];
        }
    }

    internal sealed partial class DeterministicKccMotor
    {
        static readonly FixedVector3 Up = new FixedVector3(FixedScalar.Zero, FixedScalar.One, FixedScalar.Zero);
        static readonly FixedVector3 Down = new FixedVector3(FixedScalar.Zero, -FixedScalar.One, FixedScalar.Zero);

        readonly DeterministicCollisionWorldArtifact m_World;
        readonly DeterministicKccConfiguration m_Configuration;
        readonly DeterministicCapsuleQueries m_Queries;
        readonly DeterministicKccContact[] m_HitContacts;
        readonly DeterministicKccContact[] m_OutputContacts;
        readonly DeterministicKccContact[] m_ZeroProgressContacts;
        readonly DeterministicKccConstraintPlane[] m_ConstraintPlanes;

        int m_RepresentativeEvaluationCount;
        int m_StepDetectionAttemptCount;
        int m_StabilityEvaluationCount;
        int m_StandardStepQueryCount;
        int m_ExtraStepQueryCount;
        int m_StepValidityCandidateCount;
        int m_GroundProbeEvaluationCount;

        public DeterministicKccMotor(
            DeterministicCollisionWorldArtifact world,
            DeterministicKccConfiguration configuration)
        {
            m_World = world ?? throw new ArgumentNullException(nameof(world));
            m_Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            m_Queries = new DeterministicCapsuleQueries(world, configuration);
            m_HitContacts = new DeterministicKccContact[configuration.MaximumContacts];
            m_OutputContacts = new DeterministicKccContact[configuration.MaximumContacts];
            m_ZeroProgressContacts = new DeterministicKccContact[configuration.MaximumContacts];
            m_ConstraintPlanes = new DeterministicKccConstraintPlane[configuration.MaximumContacts];
        }

        public DeterministicKccMotorResult Move(
            FixedVector3 startPosition,
            DeterministicKccBodyState previousState,
            FixedVector3 requestedDisplacement)
        {
            FixedScalar requestedMagnitude = requestedDisplacement.Magnitude;
            if (requestedMagnitude > m_Configuration.MaximumMovementDistance)
            {
                throw new DeterministicKccQueryException(
                    DeterministicKccQueryStage.Movement,
                    $"Requested displacement '{requestedMagnitude.Raw}' exceeds the locked movement distance '{m_Configuration.MaximumMovementDistance.Raw}'.");
            }

            FixedVector3 position = startPosition;
            FixedVector3 remaining = requestedDisplacement;
            m_RepresentativeEvaluationCount = 0;
            m_StepDetectionAttemptCount = 0;
            m_StabilityEvaluationCount = 0;
            m_StandardStepQueryCount = 0;
            m_ExtraStepQueryCount = 0;
            m_StepValidityCandidateCount = 0;
            m_GroundProbeEvaluationCount = 0;
            WorldCollisionSummary collision = WorldCollisionSummary.None;
            DeterministicKccQuerySummary summary = default;
            ResolvePenetration(ref position, ref collision, ref summary, DeterministicKccQueryStage.PenetrationRecovery);

            bool upwardIntent = requestedDisplacement.Y > m_Configuration.MinimumMovementDistance;
            if (previousState.IsStableOnGround && !upwardIntent && remaining.SqrMagnitude > FixedScalar.Zero)
            {
                FixedVector3 planar = Planar(remaining);
                FixedScalar planarMagnitude = planar.Magnitude;
                FixedVector3 tangent = TangentToSurface(planar, previousState.GroundNormal);
                FixedVector3 groundMovement = tangent.SqrMagnitude > FixedScalar.Zero
                    ? Scale(tangent, planarMagnitude)
                    : FixedVector3.Zero;
                remaining = groundMovement + Scale(Up, FixedScalar.Max(remaining.Y, FixedScalar.Zero));
            }

            int movementIterations = 0;
            int planeCount = 0;
            int outputContactCount = 0;
            bool lastMovementFoundAnyGround = false;
            bool zeroProgressSignatureValid = false;
            int zeroProgressContactCount = 0;
            int noProgressConfirmationCount = 0;
            DeterministicKccMovementTermination termination = DeterministicKccMovementTermination.Completed;
            DeterministicKccStepDiagnostics stepDiagnostics = default;
            for (; movementIterations < m_Configuration.MaximumContactIterations; movementIterations++)
            {
                if (remaining.Magnitude <= m_Configuration.MinimumMovementDistance)
                {
                    remaining = FixedVector3.Zero;
                    break;
                }

                FixedVector3 positionBeforeCast = position;
                FixedVector3 remainingBeforeCast = remaining;
                bool hit = m_Queries.Cast(
                    positionBeforeCast,
                    remainingBeforeCast,
                    out FixedVector3 safePosition,
                    out int contactCount,
                    out DeterministicKccQuerySummary castSummary);
                summary = summary.Add(castSummary);
                if (!hit)
                {
                    position += remaining;
                    remaining = FixedVector3.Zero;
                    break;
                }

                CopyCastContacts(contactCount);
                if (outputContactCount == 0)
                {
                    CopyOutputContacts(contactCount);
                    outputContactCount = contactCount;
                }
                position = safePosition;
                FixedScalar time = m_HitContacts[0].TimeOfImpact;
                remaining = Scale(remaining, FixedScalar.One - time);

                bool representativeSelected = TrySelectMovementRepresentative(
                    contactCount,
                    remainingBeforeCast,
                    position,
                    previousState,
                    out int selectedIndex,
                    out FixedVector3 obstructionNormal,
                    out DeterministicKccStepRejection admissionRejection,
                    out bool foundStableContact);
                if (foundStableContact)
                    lastMovementFoundAnyGround = true;
                DeterministicKccHitStabilityReport selectedStability = default;
                if (representativeSelected)
                {
                    DeterministicKccContact contact = m_HitContacts[selectedIndex];
                    m_RepresentativeEvaluationCount++;
                    selectedStability = EvaluateHitStability(
                        position,
                        contact,
                        previousState,
                        requestedDisplacement,
                        true,
                        admissionRejection,
                        ref summary,
                        ref stepDiagnostics);
                    if (selectedStability.IsStable)
                        lastMovementFoundAnyGround = true;
                }

                DeterministicKccStepRejection commitRejection = admissionRejection;
                if (representativeSelected && selectedStability.ValidStepDetected &&
                    TryCommitStep(
                        position,
                        obstructionNormal,
                        selectedStability,
                        ref summary,
                        out DeterministicKccStepCandidate committed,
                        out commitRejection))
                {
                    position = committed.Position;
                    FixedScalar remainingMagnitude = remaining.Magnitude;
                    FixedVector3 planar = Planar(remaining);
                    remaining = planar.SqrMagnitude == FixedScalar.Zero
                        ? FixedVector3.Zero
                        : Scale(planar.Normalized, remainingMagnitude);
                    planeCount = 0;
                    AddConstraintPlane(committed.Landing, committed.Landing.Normal, ref planeCount);
                    collision |= WorldCollisionSummary.Below | WorldCollisionSummary.Sides;
                    stepDiagnostics = new DeterministicKccStepDiagnostics(
                        committed.Mode,
                        DeterministicKccStepStage.Commit,
                        DeterministicKccStepRejection.None,
                        committed.Landing.SurfaceId,
                        stepDiagnostics.QuerySummary);
                    zeroProgressSignatureValid = false;
                    zeroProgressContactCount = 0;
                    noProgressConfirmationCount = 0;
                    continue;
                }

                if (representativeSelected && selectedStability.ValidStepDetected)
                {
                    stepDiagnostics = new DeterministicKccStepDiagnostics(
                        selectedStability.StepMode,
                        DeterministicKccStepStage.Commit,
                        commitRejection,
                        selectedStability.SteppedSurfaceId,
                        stepDiagnostics.QuerySummary,
                        m_RepresentativeEvaluationCount,
                        m_StepDetectionAttemptCount);
                }

                for (int i = 0; i < contactCount; i++)
                {
                    DeterministicKccContact contact = m_HitContacts[i];
                    collision |= ClassifyContact(contact);
                    AddConstraintPlane(
                        contact,
                        GetObstructionNormal(
                            contact.Normal,
                            IsStableNormal(contact.SurfaceId, contact.Normal),
                            previousState),
                        ref planeCount);
                }
                FixedVector3 remainingBeforeProjection = remaining;
                FixedVector3 projectedRemaining = ProjectRemaining(remainingBeforeProjection, planeCount);
                bool zeroProgress = time == FixedScalar.Zero &&
                                    safePosition == positionBeforeCast &&
                                    projectedRemaining == remainingBeforeProjection;
                if (zeroProgress && zeroProgressSignatureValid &&
                    MatchesZeroProgressSignature(contactCount, zeroProgressContactCount))
                {
                    remaining = FixedVector3.Zero;
                    termination = DeterministicKccMovementTermination.BlockedNoProgress;
                    noProgressConfirmationCount = 2;
                    break;
                }
                remaining = projectedRemaining;
                if (zeroProgress)
                {
                    SaveZeroProgressSignature(contactCount);
                    zeroProgressSignatureValid = true;
                    zeroProgressContactCount = contactCount;
                    noProgressConfirmationCount = 1;
                }
                else
                {
                    zeroProgressSignatureValid = false;
                    zeroProgressContactCount = 0;
                    noProgressConfirmationCount = 0;
                }
            }
            if (movementIterations >= m_Configuration.MaximumContactIterations &&
                remaining.Magnitude > m_Configuration.MinimumMovementDistance)
            {
                throw new DeterministicKccQueryException(
                    DeterministicKccQueryStage.Movement,
                    $"Movement solving did not converge after '{m_Configuration.MaximumContactIterations}' iterations. Remaining={remaining}.");
            }

            ResolvePenetration(ref position, ref collision, ref summary, DeterministicKccQueryStage.PenetrationRecovery);
            ProbeGround(
                ref position,
                previousState,
                requestedDisplacement,
                upwardIntent,
                lastMovementFoundAnyGround,
                ref summary,
                ref stepDiagnostics,
                out DeterministicKccGroundReport ground);
            stepDiagnostics = stepDiagnostics.WithCounters(
                m_RepresentativeEvaluationCount,
                m_StepDetectionAttemptCount,
                m_StabilityEvaluationCount,
                m_StandardStepQueryCount,
                m_ExtraStepQueryCount,
                m_StepValidityCandidateCount,
                m_GroundProbeEvaluationCount);
            if (ground.IsStableOnGround)
                collision |= WorldCollisionSummary.Below;

            return new DeterministicKccMotorResult(
                position,
                position - startPosition,
                remaining,
                ground,
                collision,
                stepDiagnostics,
                movementIterations,
                m_OutputContacts,
                outputContactCount,
                termination,
                noProgressConfirmationCount,
                m_ZeroProgressContacts,
                termination == DeterministicKccMovementTermination.BlockedNoProgress
                    ? zeroProgressContactCount
                    : 0,
                summary);
        }

        bool TrySelectMovementRepresentative(
            int contactCount,
            FixedVector3 displacement,
            FixedVector3 characterPosition,
            DeterministicKccBodyState previousState,
            out int selectedIndex,
            out FixedVector3 obstructionNormal,
            out DeterministicKccStepRejection admissionRejection,
            out bool foundStableContact)
        {
            selectedIndex = -1;
            obstructionNormal = FixedVector3.Zero;
            admissionRejection = DeterministicKccStepRejection.None;
            foundStableContact = false;
            for (int i = 0; i < contactCount; i++)
            {
                DeterministicKccContact contact = m_HitContacts[i];
                bool baseStable = IsStableNormal(contact.SurfaceId, contact.Normal);
                if (baseStable)
                {
                    foundStableContact = true;
                    continue;
                }
                FixedVector3 effectiveNormal = GetObstructionNormal(contact.Normal, false, previousState);
                if (FixedVector3.Dot(displacement, effectiveNormal) >= -m_Configuration.MinimumMovementDistance)
                    continue;
                if (selectedIndex < 0 || CompareContactIdentity(contact, m_HitContacts[selectedIndex]) < 0)
                {
                    selectedIndex = i;
                    obstructionNormal = effectiveNormal;
                }
            }
            if (selectedIndex < 0)
            {
                admissionRejection = DeterministicKccStepRejection.ObstructionNotClosing;
                return false;
            }

            if (!previousState.IsStableOnGround)
            {
                admissionRejection = DeterministicKccStepRejection.PreviousStableGroundAbsent;
                return true;
            }
            if (displacement.Y > m_Configuration.MinimumMovementDistance)
            {
                admissionRejection = DeterministicKccStepRejection.UpwardIntent;
                return true;
            }
            if (FixedScalar.Abs(obstructionNormal.Y) > m_Configuration.VerticalObstructionCorrelation)
            {
                admissionRejection = DeterministicKccStepRejection.ObstructionNotVertical;
                return true;
            }
            DeterministicCollisionPrimitive primitive = m_World.Primitives[m_HitContacts[selectedIndex].PrimitiveId];
            if (primitive.Bounds.Maximum.Y - characterPosition.Y >
                m_Configuration.MaximumStepHeight + m_Configuration.QueryTolerance)
            {
                admissionRejection = DeterministicKccStepRejection.ObstacleHeightExceeded;
            }
            return true;
        }

        public DeterministicKccMotorResult PlaceInitial(
            FixedVector3 position,
            DeterministicKccBodyState previousState) =>
            Reconstraint(position, previousState, FixedVector3.Zero, false, DeterministicKccQueryStage.StaticReconstraint);

        public DeterministicKccMotorResult ReconstraintAfterMovement(
            FixedVector3 position,
            DeterministicKccBodyState previousState,
            FixedVector3 requestedDisplacement,
            bool lastMovementFoundAnyGround) =>
            Reconstraint(
                position,
                previousState,
                requestedDisplacement,
                lastMovementFoundAnyGround,
                DeterministicKccQueryStage.StaticReconstraint);

        DeterministicKccMotorResult Reconstraint(
            FixedVector3 startPosition,
            DeterministicKccBodyState previousState,
            FixedVector3 requestedDisplacement,
            bool lastMovementFoundAnyGround,
            DeterministicKccQueryStage stage)
        {
            FixedVector3 position = startPosition;
            m_RepresentativeEvaluationCount = 0;
            m_StepDetectionAttemptCount = 0;
            m_StabilityEvaluationCount = 0;
            m_StandardStepQueryCount = 0;
            m_ExtraStepQueryCount = 0;
            m_StepValidityCandidateCount = 0;
            m_GroundProbeEvaluationCount = 0;
            WorldCollisionSummary collision = WorldCollisionSummary.None;
            DeterministicKccQuerySummary summary = default;
            DeterministicKccStepDiagnostics diagnostics = default;
            ResolvePenetration(ref position, ref collision, ref summary, stage);
            bool upwardIntent = requestedDisplacement.Y > m_Configuration.MinimumMovementDistance;
            ProbeGround(
                ref position,
                previousState,
                requestedDisplacement,
                upwardIntent,
                lastMovementFoundAnyGround,
                ref summary,
                ref diagnostics,
                out DeterministicKccGroundReport ground);
            diagnostics = diagnostics.WithCounters(
                m_RepresentativeEvaluationCount,
                m_StepDetectionAttemptCount,
                m_StabilityEvaluationCount,
                m_StandardStepQueryCount,
                m_ExtraStepQueryCount,
                m_StepValidityCandidateCount,
                m_GroundProbeEvaluationCount);
            if (ground.IsStableOnGround)
                collision |= WorldCollisionSummary.Below;
            return new DeterministicKccMotorResult(
                position,
                position - startPosition,
                FixedVector3.Zero,
                ground,
                collision,
                diagnostics,
                0,
                m_OutputContacts,
                0,
                DeterministicKccMovementTermination.Completed,
                0,
                m_ZeroProgressContacts,
                0,
                summary);
        }

        bool MatchesZeroProgressSignature(int contactCount, int previousContactCount)
        {
            if (contactCount <= 0 || contactCount != previousContactCount)
                return false;
            for (int i = 0; i < contactCount; i++)
            {
                DeterministicKccContact current = m_HitContacts[i];
                DeterministicKccContact previous = m_ZeroProgressContacts[i];
                if (current.PrimitiveId != previous.PrimitiveId ||
                    !current.FeatureId.Equals(previous.FeatureId) ||
                    current.Normal != previous.Normal)
                {
                    return false;
                }
            }
            return true;
        }

        void SaveZeroProgressSignature(int contactCount)
        {
            for (int i = 0; i < contactCount; i++)
                m_ZeroProgressContacts[i] = m_HitContacts[i];
        }

        public void ValidatePose(FixedVector3 position, ref DeterministicKccQuerySummary summary)
        {
            int count = m_Queries.Overlap(position, out DeterministicKccQuerySummary overlapSummary);
            summary = summary.Add(overlapSummary);
            for (int i = 0; i < count; i++)
            {
                DeterministicKccContact contact = m_Queries.OverlapContactAt(i);
                if (contact.Separation < -m_Configuration.QueryTolerance)
                {
                    throw new DeterministicKccQueryException(
                        DeterministicKccQueryStage.StaticReconstraint,
                        "Final pose penetrates the static collision world.",
                        contact.PrimitiveId);
                }
            }
        }
    }
}
