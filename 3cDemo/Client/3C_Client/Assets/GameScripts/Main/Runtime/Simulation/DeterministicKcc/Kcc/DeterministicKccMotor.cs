using System;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
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
            QuerySummary = querySummary;
        }

        readonly DeterministicKccContact[] m_BlockingContacts;

        public FixedVector3 Position { get; }
        public FixedVector3 AppliedDisplacement { get; }
        public FixedVector3 RemainingDisplacement { get; }
        public DeterministicKccGroundReport Ground { get; }
        public WorldCollisionSummary Collision { get; }
        public DeterministicKccStepDiagnostics StepDiagnostics { get; }
        public int MovementIterations { get; }
        public int BlockingContactCount { get; }
        public bool HasBlockingContact => BlockingContactCount > 0;
        public DeterministicKccQuerySummary QuerySummary { get; }

        public DeterministicKccContact BlockingContactAt(int index)
        {
            if (index < 0 || index >= BlockingContactCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_BlockingContacts[index];
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
        readonly DeterministicKccConstraintPlane[] m_ConstraintPlanes = new DeterministicKccConstraintPlane[3];

        public DeterministicKccMotor(
            DeterministicCollisionWorldArtifact world,
            DeterministicKccConfiguration configuration)
        {
            m_World = world ?? throw new ArgumentNullException(nameof(world));
            m_Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            m_Queries = new DeterministicCapsuleQueries(world, configuration);
            m_HitContacts = new DeterministicKccContact[configuration.MaximumContacts];
            m_OutputContacts = new DeterministicKccContact[configuration.MaximumContacts];
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
            DeterministicKccStepDiagnostics stepDiagnostics = default;
            for (; movementIterations < m_Configuration.MaximumContactIterations; movementIterations++)
            {
                if (remaining.Magnitude <= m_Configuration.MinimumMovementDistance)
                {
                    remaining = FixedVector3.Zero;
                    break;
                }

                bool hit = m_Queries.Cast(
                    position,
                    remaining,
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

                int selectedIndex = 0;
                DeterministicKccHitStabilityReport selectedStability = default;
                bool selected = false;
                for (int i = 0; i < contactCount; i++)
                {
                    DeterministicKccContact contact = m_HitContacts[i];
                    DeterministicKccHitStabilityReport stability = EvaluateHitStability(
                        position,
                        contact,
                        previousState,
                        requestedDisplacement,
                        !upwardIntent,
                        ref summary,
                        ref stepDiagnostics);
                    if (stability.IsStable)
                        lastMovementFoundAnyGround = true;
                    if (!selected || stability.ValidStepDetected)
                    {
                        selected = true;
                        selectedIndex = i;
                        selectedStability = stability;
                    }
                    if (stability.ValidStepDetected)
                        break;
                }

                DeterministicKccContact selectedContact = m_HitContacts[selectedIndex];
                FixedVector3 obstructionNormal = GetObstructionNormal(
                    selectedContact.Normal,
                    selectedStability.IsStable,
                    previousState);
                bool verticalObstruction = FixedScalar.Abs(obstructionNormal.Y) <= m_Configuration.VerticalObstructionCorrelation;
                DeterministicKccStepRejection commitRejection = previousState.IsStableOnGround
                    ? upwardIntent
                        ? DeterministicKccStepRejection.UpwardIntent
                        : DeterministicKccStepRejection.CommitLandingAbsent
                    : DeterministicKccStepRejection.PreviousStableGroundAbsent;
                if (selectedStability.ValidStepDetected && previousState.IsStableOnGround && !upwardIntent && verticalObstruction &&
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
                    continue;
                }

                if (selectedStability.ValidStepDetected)
                {
                    stepDiagnostics = new DeterministicKccStepDiagnostics(
                        selectedStability.StepMode,
                        DeterministicKccStepStage.Commit,
                        verticalObstruction ? commitRejection : DeterministicKccStepRejection.ObstructionNotVertical,
                        selectedStability.SteppedSurfaceId,
                        stepDiagnostics.QuerySummary);
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
                remaining = ProjectRemaining(remaining, planeCount);
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
                summary);
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
                summary);
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
