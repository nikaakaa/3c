using System;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    public enum DeterministicKccLedgeState : byte
    {
        None = 0,
        SupportedSeam = 1,
        UnsupportedEdge = 2
    }

    internal enum DeterministicKccStepRejection : byte
    {
        None = 0,
        InsufficientForwardMovement = 1,
        UpwardClearanceBlocked = 2,
        ForwardClearanceBlocked = 3,
        StableLandingAbsent = 4,
        MaximumHeightExceeded = 5,
        FinalPoseInvalid = 6
    }

    internal readonly struct DeterministicKccGroundReport
    {
        public DeterministicKccGroundReport(
            bool foundAnyGround,
            bool isStableOnGround,
            int primitiveId,
            DeterministicCollisionFeatureId featureId,
            FixedVector3 normal,
            FixedScalar distance,
            DeterministicKccLedgeState ledgeState)
        {
            FoundAnyGround = foundAnyGround;
            IsStableOnGround = isStableOnGround;
            PrimitiveId = primitiveId;
            FeatureId = featureId;
            Normal = normal;
            Distance = distance;
            LedgeState = ledgeState;
        }

        public bool FoundAnyGround { get; }
        public bool IsStableOnGround { get; }
        public int PrimitiveId { get; }
        public DeterministicCollisionFeatureId FeatureId { get; }
        public FixedVector3 Normal { get; }
        public FixedScalar Distance { get; }
        public DeterministicKccLedgeState LedgeState { get; }

        public static DeterministicKccGroundReport None => new DeterministicKccGroundReport(
            false,
            false,
            -1,
            DeterministicCollisionFeatureId.Invalid,
            FixedVector3.Zero,
            FixedScalar.Zero,
            DeterministicKccLedgeState.None);
    }

    internal readonly struct DeterministicKccMotorResult
    {
        public DeterministicKccMotorResult(
            FixedVector3 position,
            FixedVector3 appliedDisplacement,
            FixedVector3 remainingDisplacement,
            DeterministicKccGroundReport ground,
            WorldCollisionSummary collision,
            DeterministicKccStepPhase stepPhase,
            DeterministicKccStepRejection stepRejection,
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
            StepPhase = stepPhase;
            StepRejection = stepRejection;
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
        public DeterministicKccStepPhase StepPhase { get; }
        public DeterministicKccStepRejection StepRejection { get; }
        public int MovementIterations { get; }
        public int BlockingContactCount { get; }
        public bool HasBlockingContact => BlockingContactCount > 0;
        public DeterministicKccContact BlockingContactAt(int index)
        {
            if (index < 0 || index >= BlockingContactCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_BlockingContacts[index];
        }
        public DeterministicKccQuerySummary QuerySummary { get; }
    }

    internal sealed class DeterministicKccMotor
    {
        readonly DeterministicCollisionWorldArtifact m_World;
        readonly DeterministicKccConfiguration m_Configuration;
        readonly DeterministicCapsuleQueries m_Queries;
        readonly DeterministicKccContact[] m_HitContacts;
        readonly DeterministicKccContact[] m_OutputContacts;
        readonly FixedVector3[] m_ConstraintPlanes = new FixedVector3[3];

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

            FixedVector3 displacement = ProjectAlongStableGround(requestedDisplacement, previousState);
            FixedVector3 position = startPosition;
            WorldCollisionSummary collision = WorldCollisionSummary.None;
            DeterministicKccQuerySummary summary = default;
            ResolvePenetration(ref position, ref collision, ref summary, DeterministicKccQueryStage.PenetrationRecovery);

            DeterministicKccStepPhase stepPhase = DeterministicKccStepPhase.None;
            DeterministicKccStepRejection stepRejection = DeterministicKccStepRejection.None;
            int outputContactCount = 0;
            int movementIterations = 0;
            FixedVector3 remaining = displacement;
            int planeCount = 0;
            for (int iteration = 0; iteration < m_Configuration.MaximumSweepIterations; iteration++)
            {
                movementIterations++;
                if (remaining.Magnitude <= m_Configuration.MinimumMovementDistance)
                {
                    remaining = FixedVector3.Zero;
                    break;
                }

                FixedVector3 movementStart = position;
                if (!m_Queries.Cast(
                        position,
                        remaining,
                        out FixedVector3 safePosition,
                        out int hitCount,
                        out DeterministicKccQuerySummary castSummary))
                {
                    position += remaining;
                    remaining = FixedVector3.Zero;
                    summary = summary.Add(castSummary);
                    break;
                }
                summary = summary.Add(castSummary);
                CopyCastContacts(hitCount);
                CopyOutputContacts(hitCount);
                outputContactCount = hitCount;
                bool hasSide = false;
                for (int i = 0; i < hitCount; i++)
                {
                    WorldCollisionSummary classification = ClassifyContact(m_HitContacts[i]);
                    collision |= classification;
                    hasSide |= (classification & WorldCollisionSummary.Sides) != 0;
                }

                if (hasSide && previousState.Grounded && HasPlanarMovement(remaining) &&
                    m_Configuration.MaximumStepHeight > FixedScalar.Zero)
                {
                    bool stepped = TryStep(
                        movementStart,
                        remaining,
                        previousState,
                        out DeterministicKccMotorResult stepResult,
                        out stepRejection,
                        out DeterministicKccQuerySummary stepSummary);
                    summary = summary.Add(stepSummary);
                    if (stepped)
                    {
                        position = stepResult.Position;
                        collision |= stepResult.Collision | WorldCollisionSummary.Below;
                        stepPhase = DeterministicKccStepPhase.SteppedUp;
                        remaining = FixedVector3.Zero;
                        break;
                    }
                }

                FixedVector3 travelled = safePosition - position;
                position = safePosition;
                FixedVector3 nextRemaining = remaining - travelled;
                for (int i = 0; i < hitCount; i++)
                    AddConstraintPlane(m_HitContacts[i].Normal, ref planeCount);
                FixedVector3 projected = ProjectRemaining(nextRemaining, planeCount);
                if ((projected - nextRemaining).Magnitude <= m_Configuration.MinimumMovementDistance &&
                    travelled.Magnitude <= m_Configuration.MinimumMovementDistance)
                {
                    remaining = FixedVector3.Zero;
                    break;
                }
                remaining = projected;
            }
            if (remaining.Magnitude > m_Configuration.MinimumMovementDistance)
            {
                throw new DeterministicKccQueryException(
                    DeterministicKccQueryStage.Movement,
                    $"Collide-and-slide did not converge after '{m_Configuration.MaximumSweepIterations}' iterations.");
            }

            ResolvePenetration(ref position, ref collision, ref summary, DeterministicKccQueryStage.PenetrationRecovery);
            DeterministicKccGroundReport ground = DeterministicKccGroundReport.None;
            DeterministicKccGroundReport groundReport = DeterministicKccGroundReport.None;
            DeterministicKccQuerySummary groundSummary = default;
            if (TryGetMovementGroundProbeDistance(previousState, requestedDisplacement, out FixedScalar probeDistance) &&
                TryGround(position, probeDistance, previousState, out FixedVector3 groundedPosition,
                    out groundReport, out groundSummary))
            {
                if (groundedPosition.Y < position.Y - m_Configuration.QueryTolerance && stepPhase == DeterministicKccStepPhase.None)
                    stepPhase = DeterministicKccStepPhase.SteppedDown;
                position = groundedPosition;
                ground = groundReport;
                collision |= WorldCollisionSummary.Below;
                summary = summary.Add(groundSummary);
            }
            else
            {
                summary = summary.Add(groundSummary);
                ground = groundReport;
            }
            ValidatePose(position, ref summary);
            return new DeterministicKccMotorResult(
                position,
                position - startPosition,
                remaining,
                ground,
                collision,
                stepPhase,
                stepRejection,
                movementIterations,
                m_OutputContacts,
                outputContactCount,
                summary);
        }

        public DeterministicKccMotorResult PlaceInitial(
            FixedVector3 position,
            DeterministicKccBodyState previousState) => Reconstraint(
                position,
                previousState,
                true,
                m_Configuration.GroundSnapDistance);

        public DeterministicKccMotorResult ReconstraintAfterMovement(
            FixedVector3 position,
            DeterministicKccBodyState previousState,
            FixedVector3 requestedDisplacement)
        {
            bool allowGroundProbe = TryGetMovementGroundProbeDistance(
                previousState,
                requestedDisplacement,
                out FixedScalar probeDistance);
            return Reconstraint(position, previousState, allowGroundProbe, probeDistance);
        }

        DeterministicKccMotorResult Reconstraint(
            FixedVector3 position,
            DeterministicKccBodyState previousState,
            bool allowGroundProbe,
            FixedScalar probeDistance)
        {
            WorldCollisionSummary collision = WorldCollisionSummary.None;
            DeterministicKccQuerySummary summary = default;
            ResolvePenetration(ref position, ref collision, ref summary, DeterministicKccQueryStage.StaticReconstraint);
            DeterministicKccGroundReport ground = DeterministicKccGroundReport.None;
            DeterministicKccGroundReport groundReport = DeterministicKccGroundReport.None;
            DeterministicKccQuerySummary groundSummary = default;
            if (allowGroundProbe && TryGround(
                    position,
                    probeDistance,
                    previousState,
                    out FixedVector3 groundedPosition,
                    out groundReport,
                    out groundSummary))
            {
                position = groundedPosition;
                ground = groundReport;
                collision |= WorldCollisionSummary.Below;
            }
            else
            {
                ground = groundReport;
            }
            summary = summary.Add(groundSummary);
            ValidatePose(position, ref summary);
            return new DeterministicKccMotorResult(
                position,
                FixedVector3.Zero,
                FixedVector3.Zero,
                ground,
                collision,
                DeterministicKccStepPhase.None,
                DeterministicKccStepRejection.None,
                0,
                m_OutputContacts,
                0,
                summary);
        }

        bool TryGetMovementGroundProbeDistance(
            DeterministicKccBodyState previousState,
            FixedVector3 requestedDisplacement,
            out FixedScalar probeDistance)
        {
            if (requestedDisplacement.Y > FixedScalar.Zero)
            {
                probeDistance = FixedScalar.Zero;
                return false;
            }
            probeDistance = previousState.Grounded
                ? m_Configuration.GroundSnapDistance
                : m_Configuration.SkinWidth + m_Configuration.QueryTolerance;
            return probeDistance > FixedScalar.Zero;
        }

        public void ValidatePose(FixedVector3 position, ref DeterministicKccQuerySummary summary)
        {
            if (TryValidatePose(position, ref summary, out DeterministicKccContact penetration))
                return;
            throw new DeterministicKccQueryException(
                DeterministicKccQueryStage.StaticReconstraint,
                $"Final capsule remains penetrated by '{penetration.Penetration.Raw}'.",
                penetration.PrimitiveId);
        }

        bool TryValidatePose(
            FixedVector3 position,
            ref DeterministicKccQuerySummary summary,
            out DeterministicKccContact penetration)
        {
            penetration = default;
            int count = m_Queries.Overlap(position, out DeterministicKccQuerySummary overlapSummary);
            summary = summary.Add(overlapSummary);
            for (int i = 0; i < count; i++)
            {
                DeterministicKccContact contact = m_Queries.OverlapContactAt(i);
                if (contact.Separation < -m_Configuration.QueryTolerance)
                {
                    penetration = contact;
                    return false;
                }
            }
            return true;
        }

        FixedVector3 ProjectAlongStableGround(
            FixedVector3 displacement,
            DeterministicKccBodyState previousState)
        {
            if (!previousState.Grounded || previousState.GroundNormal.SqrMagnitude == FixedScalar.Zero ||
                previousState.GroundNormal.Y < m_Configuration.MinimumGroundNormalY)
            {
                return displacement;
            }
            FixedVector3 planar = new FixedVector3(displacement.X, FixedScalar.Zero, displacement.Z);
            FixedScalar planarMagnitude = planar.Magnitude;
            if (planarMagnitude <= m_Configuration.MinimumMovementDistance)
                return displacement;
            FixedVector3 projected = planar - Scale(previousState.GroundNormal, FixedVector3.Dot(planar, previousState.GroundNormal));
            FixedScalar projectedMagnitude = projected.Magnitude;
            if (projectedMagnitude <= m_Configuration.QueryTolerance)
                throw new InvalidOperationException("Stable ground projection collapsed the planar displacement.");
            projected = Scale(projected, planarMagnitude / projectedMagnitude);
            return new FixedVector3(projected.X, displacement.Y + projected.Y, projected.Z);
        }

        bool TryStep(
            FixedVector3 position,
            FixedVector3 displacement,
            DeterministicKccBodyState previousState,
            out DeterministicKccMotorResult result,
            out DeterministicKccStepRejection rejection,
            out DeterministicKccQuerySummary summary)
        {
            result = default;
            rejection = DeterministicKccStepRejection.None;
            summary = default;
            FixedVector3 planar = new FixedVector3(displacement.X, FixedScalar.Zero, displacement.Z);
            if (planar.Magnitude < m_Configuration.MinimumStepForwardDistance)
            {
                rejection = DeterministicKccStepRejection.InsufficientForwardMovement;
                return false;
            }

            FixedVector3 up = new FixedVector3(FixedScalar.Zero, m_Configuration.MaximumStepHeight, FixedScalar.Zero);
            if (m_Queries.Cast(position, up, out _, out _, out DeterministicKccQuerySummary upSummary))
            {
                summary = summary.Add(upSummary);
                rejection = DeterministicKccStepRejection.UpwardClearanceBlocked;
                return false;
            }
            summary = summary.Add(upSummary);
            FixedVector3 raised = position + up;
            if (m_Queries.Cast(raised, planar, out FixedVector3 forwardPosition, out _, out DeterministicKccQuerySummary forwardSummary))
            {
                summary = summary.Add(forwardSummary);
                rejection = DeterministicKccStepRejection.ForwardClearanceBlocked;
                return false;
            }
            summary = summary.Add(forwardSummary);
            forwardPosition = raised + planar;
            FixedVector3 progress = new FixedVector3(forwardPosition.X - position.X, FixedScalar.Zero, forwardPosition.Z - position.Z);
            if (progress.Magnitude < m_Configuration.MinimumStepForwardDistance)
            {
                rejection = DeterministicKccStepRejection.InsufficientForwardMovement;
                return false;
            }

            FixedScalar downDistance = m_Configuration.MaximumStepHeight + m_Configuration.GroundSnapDistance;
            if (!TryGround(forwardPosition, downDistance, previousState, out FixedVector3 landed,
                    out DeterministicKccGroundReport ground, out DeterministicKccQuerySummary groundSummary))
            {
                summary = summary.Add(groundSummary);
                rejection = DeterministicKccStepRejection.StableLandingAbsent;
                return false;
            }
            summary = summary.Add(groundSummary);
            if (landed.Y - position.Y > m_Configuration.MaximumStepHeight + m_Configuration.QueryTolerance)
            {
                rejection = DeterministicKccStepRejection.MaximumHeightExceeded;
                return false;
            }
            if (!TryValidatePose(landed, ref summary, out _))
            {
                rejection = DeterministicKccStepRejection.FinalPoseInvalid;
                return false;
            }
            FixedScalar supportProbe = m_Configuration.SkinWidth + m_Configuration.QueryTolerance;
            if (!TryGround(
                    landed,
                    supportProbe,
                    previousState,
                    out FixedVector3 validatedLanding,
                    out DeterministicKccGroundReport validatedGround,
                    out DeterministicKccQuerySummary validationSummary))
            {
                summary = summary.Add(validationSummary);
                rejection = DeterministicKccStepRejection.StableLandingAbsent;
                return false;
            }
            summary = summary.Add(validationSummary);
            landed = validatedLanding;
            ground = validatedGround;
            WorldCollisionSummary collision = WorldCollisionSummary.Below;
            result = new DeterministicKccMotorResult(
                landed,
                landed - position,
                FixedVector3.Zero,
                ground,
                collision,
                DeterministicKccStepPhase.SteppedUp,
                DeterministicKccStepRejection.None,
                0,
                m_OutputContacts,
                0,
                summary);
            return true;
        }

        bool TryGround(
            FixedVector3 position,
            FixedScalar distance,
            DeterministicKccBodyState previousState,
            out FixedVector3 groundedPosition,
            out DeterministicKccGroundReport report,
            out DeterministicKccQuerySummary summary)
        {
            groundedPosition = position;
            report = DeterministicKccGroundReport.None;
            summary = default;
            if (distance <= FixedScalar.Zero)
                return false;
            FixedVector3 down = new FixedVector3(FixedScalar.Zero, -distance, FixedScalar.Zero);
            if (!m_Queries.Cast(position, down, out FixedVector3 safePosition, out int contactCount,
                    out DeterministicKccQuerySummary castSummary))
            {
                summary = castSummary;
                return false;
            }
            summary = castSummary;
            DeterministicKccContact best = default;
            bool foundAny = false;
            bool foundStable = false;
            DeterministicKccLedgeState ledge = DeterministicKccLedgeState.None;
            FixedVector3 bestGroundNormal = FixedVector3.Zero;
            for (int i = 0; i < contactCount; i++)
            {
                DeterministicKccContact contact = m_Queries.CastContactAt(i);
                if (contact.Normal.Y <= FixedScalar.Zero || !IsBottomSupport(safePosition, contact))
                    continue;
                foundAny = true;
                if (!TryGetStableGroundNormal(
                        contact,
                        out FixedVector3 groundNormal,
                        out DeterministicKccLedgeState contactLedge))
                {
                    if (ledge == DeterministicKccLedgeState.None)
                        ledge = contactLedge;
                    continue;
                }
                if (!foundStable || CompareGround(contact, groundNormal, best, bestGroundNormal, previousState) < 0)
                {
                    best = contact;
                    bestGroundNormal = groundNormal;
                    ledge = contactLedge;
                    foundStable = true;
                }
            }
            if (!foundStable)
            {
                report = new DeterministicKccGroundReport(
                    foundAny,
                    false,
                    -1,
                    DeterministicCollisionFeatureId.Invalid,
                    FixedVector3.Zero,
                    distance,
                    ledge);
                return false;
            }
            groundedPosition = safePosition;
            report = new DeterministicKccGroundReport(
                true,
                true,
                best.PrimitiveId,
                best.FeatureId,
                bestGroundNormal,
                distance * best.TimeOfImpact,
                ledge);
            return true;
        }

        bool TryGetStableGroundNormal(
            DeterministicKccContact contact,
            out FixedVector3 groundNormal,
            out DeterministicKccLedgeState ledgeState)
        {
            groundNormal = FixedVector3.Zero;
            ledgeState = DeterministicKccLedgeState.None;
            DeterministicCollisionSurface surface = m_World.Surfaces[contact.SurfaceId];
            if (!surface.Walkable)
                return false;
            DeterministicCollisionPrimitive primitive = m_World.Primitives[contact.PrimitiveId];
            if (primitive.Kind != DeterministicCollisionPrimitiveKind.Triangle)
            {
                if (contact.Normal.Y < m_Configuration.MinimumGroundNormalY)
                    return false;
                groundNormal = contact.Normal;
                return true;
            }
            if (contact.FeatureId.Kind == DeterministicCollisionFeatureKind.TriangleFace)
            {
                if (primitive.Normal.Y < m_Configuration.MinimumGroundNormalY)
                    return false;
                groundNormal = primitive.Normal;
                return true;
            }
            if (contact.FeatureId.Kind == DeterministicCollisionFeatureKind.TriangleEdge)
            {
                int adjacent = primitive.AdjacentPrimitiveAt(contact.FeatureId.Index);
                if (adjacent >= 0)
                {
                    DeterministicCollisionPrimitive adjacentPrimitive = m_World.Primitives[adjacent];
                    DeterministicCollisionSurface adjacentSurface = m_World.Surfaces[adjacentPrimitive.SurfaceId];
                    if (adjacentSurface.Walkable &&
                        primitive.Normal.Y >= m_Configuration.MinimumGroundNormalY &&
                        adjacentPrimitive.Normal.Y >= m_Configuration.MinimumGroundNormalY)
                    {
                        FixedVector3 combined = primitive.Normal + adjacentPrimitive.Normal;
                        FixedScalar magnitude = combined.Magnitude;
                        if (magnitude > m_Configuration.QueryTolerance)
                        {
                            groundNormal = Scale(combined, FixedScalar.One / magnitude);
                            ledgeState = DeterministicKccLedgeState.SupportedSeam;
                            return groundNormal.Y >= m_Configuration.MinimumGroundNormalY;
                        }
                    }
                }
                ledgeState = DeterministicKccLedgeState.UnsupportedEdge;
                return false;
            }
            ledgeState = DeterministicKccLedgeState.UnsupportedEdge;
            return false;
        }

        bool IsBottomSupport(FixedVector3 position, DeterministicKccContact contact) =>
            contact.CharacterPoint.Y <= position.Y + m_Configuration.Radius + m_Configuration.QueryTolerance;

        bool IsPreviousSupportContinuous(DeterministicKccBodyState previousState, int primitiveId)
        {
            if (!previousState.Grounded || previousState.GroundPrimitiveId < 0)
                return false;
            return previousState.GroundPrimitiveId == primitiveId ||
                   m_World.AreAdjacent(previousState.GroundPrimitiveId, primitiveId);
        }

        int CompareGround(
            DeterministicKccContact left,
            FixedVector3 leftGroundNormal,
            DeterministicKccContact right,
            FixedVector3 rightGroundNormal,
            DeterministicKccBodyState previousState)
        {
            bool leftContinuous = IsPreviousSupportContinuous(previousState, left.PrimitiveId);
            bool rightContinuous = IsPreviousSupportContinuous(previousState, right.PrimitiveId);
            if (leftContinuous != rightContinuous)
                return leftContinuous ? -1 : 1;
            int normal = rightGroundNormal.Y.CompareTo(leftGroundNormal.Y);
            if (normal != 0)
                return normal;
            int primitive = left.PrimitiveId.CompareTo(right.PrimitiveId);
            return primitive != 0 ? primitive : left.FeatureId.CompareTo(right.FeatureId);
        }

        void ResolvePenetration(
            ref FixedVector3 position,
            ref WorldCollisionSummary collision,
            ref DeterministicKccQuerySummary summary,
            DeterministicKccQueryStage stage)
        {
            for (int iteration = 0; iteration < m_Configuration.MaximumContactIterations; iteration++)
            {
                int contactCount = m_Queries.Overlap(position, out DeterministicKccQuerySummary overlapSummary);
                summary = summary.Add(new DeterministicKccQuerySummary(
                    overlapSummary.QueryCount,
                    overlapSummary.CandidateCount,
                    overlapSummary.ContactCount,
                    1));
                bool foundPenetration = false;
                DeterministicKccContact deepest = default;
                for (int i = 0; i < contactCount; i++)
                {
                    DeterministicKccContact contact = m_Queries.OverlapContactAt(i);
                    if (contact.Separation >= -m_Configuration.QueryTolerance)
                        continue;
                    if (!foundPenetration || contact.Separation < deepest.Separation ||
                        contact.Separation == deepest.Separation && CompareContactIdentity(contact, deepest) < 0)
                    {
                        deepest = contact;
                        foundPenetration = true;
                    }
                }
                if (!foundPenetration)
                    return;
                position += Scale(deepest.Normal, deepest.Penetration + m_Configuration.SkinWidth);
                collision |= ClassifyContact(deepest);
            }
            throw new DeterministicKccQueryException(
                stage,
                $"Penetration recovery did not converge after '{m_Configuration.MaximumContactIterations}' iterations.");
        }

        void CopyCastContacts(int count)
        {
            if (count > m_HitContacts.Length)
            {
                throw new DeterministicKccQueryException(
                    DeterministicKccQueryStage.Movement,
                    "Movement contact buffer capacity was exceeded.",
                    requiredCapacity: count,
                    configuredCapacity: m_HitContacts.Length);
            }
            for (int i = 0; i < count; i++)
                m_HitContacts[i] = m_Queries.CastContactAt(i);
        }

        void CopyOutputContacts(int count)
        {
            if (count > m_OutputContacts.Length)
            {
                throw new DeterministicKccQueryException(
                    DeterministicKccQueryStage.Movement,
                    "Motor output contact capacity was exceeded.",
                    requiredCapacity: count,
                    configuredCapacity: m_OutputContacts.Length);
            }
            for (int i = 0; i < count; i++)
                m_OutputContacts[i] = m_HitContacts[i];
        }

        void AddConstraintPlane(FixedVector3 normal, ref int planeCount)
        {
            for (int i = 0; i < planeCount; i++)
            {
                if (FixedVector3.Dot(m_ConstraintPlanes[i], normal) >= m_Configuration.NormalMergeDot)
                    return;
            }
            if (planeCount < m_ConstraintPlanes.Length)
                m_ConstraintPlanes[planeCount++] = normal;
        }

        FixedVector3 ProjectRemaining(FixedVector3 remaining, int planeCount)
        {
            if (planeCount <= 0)
                return remaining;
            if (planeCount == 1)
                return ClipAgainstPlane(remaining, m_ConstraintPlanes[0]);
            FixedVector3 crease = FixedVector3.Cross(m_ConstraintPlanes[0], m_ConstraintPlanes[1]);
            FixedScalar creaseLength = crease.Magnitude;
            if (creaseLength <= m_Configuration.QueryTolerance)
                return ClipAgainstPlane(ClipAgainstPlane(remaining, m_ConstraintPlanes[0]), m_ConstraintPlanes[1]);
            FixedVector3 direction = Scale(crease, FixedScalar.One / creaseLength);
            FixedVector3 projected = Scale(direction, FixedVector3.Dot(remaining, direction));
            if (planeCount >= 3 && FixedVector3.Dot(projected, m_ConstraintPlanes[2]) < FixedScalar.Zero)
                return FixedVector3.Zero;
            return projected;
        }

        static FixedVector3 ClipAgainstPlane(FixedVector3 value, FixedVector3 normal)
        {
            FixedScalar intoSurface = FixedVector3.Dot(value, normal);
            return intoSurface < FixedScalar.Zero ? value - Scale(normal, intoSurface) : value;
        }

        WorldCollisionSummary ClassifyContact(DeterministicKccContact contact)
        {
            DeterministicCollisionSurface surface = m_World.Surfaces[contact.SurfaceId];
            if (surface.Walkable && contact.Normal.Y >= m_Configuration.MinimumGroundNormalY)
                return WorldCollisionSummary.Below;
            if (contact.Normal.Y <= -m_Configuration.MinimumGroundNormalY)
                return WorldCollisionSummary.Above;
            return WorldCollisionSummary.Sides;
        }

        static int CompareContactIdentity(DeterministicKccContact left, DeterministicKccContact right)
        {
            int primitive = left.PrimitiveId.CompareTo(right.PrimitiveId);
            return primitive != 0 ? primitive : left.FeatureId.CompareTo(right.FeatureId);
        }

        static bool HasPlanarMovement(FixedVector3 value) =>
            value.X != FixedScalar.Zero || value.Z != FixedScalar.Zero;

        static FixedVector3 Scale(FixedVector3 value, FixedScalar scale) =>
            new FixedVector3(value.X * scale, value.Y * scale, value.Z * scale);
    }
}
