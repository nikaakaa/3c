using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    internal sealed partial class DeterministicKccMotor
    {
        void ProbeGround(
            ref FixedVector3 position,
            DeterministicKccBodyState previousState,
            FixedVector3 requestedDisplacement,
            bool upwardIntent,
            bool lastMovementFoundAnyGround,
            ref DeterministicKccQuerySummary summary,
            ref DeterministicKccStepDiagnostics diagnostics,
            out DeterministicKccGroundReport report)
        {
            FixedScalar probeDistance = m_Configuration.MinimumGroundProbingDistance;
            if (!upwardIntent && !previousState.SnappingPrevented &&
                (previousState.IsStableOnGround || lastMovementFoundAnyGround))
            {
                probeDistance = FixedScalar.Max(m_Configuration.Radius, m_Configuration.MaximumStepHeight) +
                                m_Configuration.GroundDetectionExtraDistance;
            }
            probeDistance = FixedScalar.Max(probeDistance, m_Configuration.MinimumGroundProbingDistance);

            FixedVector3 sweepPosition = position;
            FixedVector3 sweepDirection = Down;
            FixedScalar remainingDistance = probeDistance;
            report = DeterministicKccGroundReport.NoGround(lastMovementFoundAnyGround);
            for (int iteration = 0; iteration < 3 && remainingDistance > FixedScalar.Zero; iteration++)
            {
                bool hit = m_Queries.Cast(
                    sweepPosition,
                    Scale(sweepDirection, remainingDistance),
                    out FixedVector3 safePosition,
                    out int contactCount,
                    out DeterministicKccQuerySummary castSummary);
                summary = summary.Add(castSummary);
                if (!hit || contactCount == 0)
                    return;

                DeterministicKccContact contact = m_Queries.CastContactAt(0);
                DeterministicKccHitStabilityReport stability = EvaluateHitStability(
                    safePosition,
                    contact,
                    previousState,
                    requestedDisplacement,
                    !upwardIntent,
                    ref summary,
                    ref diagnostics);
                FixedScalar distance = remainingDistance * contact.TimeOfImpact;
                DeterministicKccLedgeState ledgeState = stability.LedgeDetected
                    ? stability.IsOnEmptySideOfLedge
                        ? DeterministicKccLedgeState.EmptySide
                        : DeterministicKccLedgeState.StableSide
                    : DeterministicKccLedgeState.None;
                bool stableGround = stability.IsStable && !stability.SnappingPrevented;
                report = new DeterministicKccGroundReport(
                    true,
                    stability.BaseIsStable,
                    stableGround,
                    contact.SurfaceId,
                    contact.PrimitiveId,
                    contact.FeatureId,
                    contact.Normal,
                    stability.FoundInnerNormal ? stability.InnerNormal : FixedVector3.Zero,
                    stability.FoundOuterNormal ? stability.OuterNormal : FixedVector3.Zero,
                    distance,
                    probeDistance,
                    stability.DenivelationNormalDot,
                    stability.SnappingPrevented,
                    ledgeState,
                    lastMovementFoundAnyGround);
                if (stableGround)
                {
                    if (!upwardIntent && !stability.SnappingPrevented)
                        position = safePosition;
                    return;
                }

                FixedVector3 sweepMovement = Scale(sweepDirection, distance) +
                                             Scale(Up, FixedScalar.Max(m_Configuration.CollisionOffset, distance));
                sweepPosition += sweepMovement;
                FixedScalar consumed = sweepMovement.Magnitude;
                remainingDistance = FixedScalar.Min(
                    m_Configuration.GroundProbeReboundDistance,
                    FixedScalar.Max(remainingDistance - consumed, FixedScalar.Zero));
                sweepDirection = ProjectOnPlane(sweepDirection, contact.Normal).Normalized;
                if (sweepDirection.SqrMagnitude == FixedScalar.Zero)
                    return;
            }
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
                    if (contact.Separation >= m_Configuration.CollisionOffset - m_Configuration.QueryTolerance)
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
                position += Scale(deepest.Normal, m_Configuration.CollisionOffset - deepest.Separation);
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

        void AddConstraintPlane(
            DeterministicKccContact contact,
            FixedVector3 normal,
            ref int planeCount)
        {
            for (int i = 0; i < planeCount; i++)
            {
                DeterministicKccConstraintPlane plane = m_ConstraintPlanes[i];
                if (plane.PrimitiveId == contact.PrimitiveId && plane.FeatureId.Equals(contact.FeatureId))
                {
                    m_ConstraintPlanes[i] = new DeterministicKccConstraintPlane(
                        contact.PrimitiveId,
                        contact.FeatureId,
                        normal);
                    return;
                }
            }
            for (int i = 0; i < planeCount; i++)
            {
                if (FixedVector3.Dot(m_ConstraintPlanes[i].Normal, normal) >= m_Configuration.NormalMergeDot)
                    return;
            }
            if (planeCount < m_ConstraintPlanes.Length)
            {
                m_ConstraintPlanes[planeCount++] = new DeterministicKccConstraintPlane(
                    contact.PrimitiveId,
                    contact.FeatureId,
                    normal);
                return;
            }
            throw new DeterministicKccQueryException(
                DeterministicKccQueryStage.Movement,
                "Movement constraint plane capacity was exceeded.",
                contact.PrimitiveId,
                planeCount + 1,
                m_ConstraintPlanes.Length);
        }

        FixedVector3 ProjectRemaining(FixedVector3 remaining, int planeCount)
        {
            if (planeCount <= 0)
                return remaining;

            if (IsConstraintCandidateFeasible(remaining, planeCount))
                return remaining;

            FixedVector3 best = FixedVector3.Zero;
            FixedScalar bestDistance = FixedScalar.Zero;
            DeterministicKccConstraintCandidateKind bestKind = default;
            int bestFirstIndex = 0;
            int bestSecondIndex = 0;
            bool hasBest = false;
            for (int i = 0; i < planeCount; i++)
            {
                FixedVector3 candidate = ProjectOntoConstraintPlane(remaining, m_ConstraintPlanes[i].Normal);
                SelectConstraintCandidate(
                    remaining,
                    candidate,
                    planeCount,
                    DeterministicKccConstraintCandidateKind.SinglePlane,
                    i,
                    -1,
                    ref best,
                    ref bestDistance,
                    ref bestKind,
                    ref bestFirstIndex,
                    ref bestSecondIndex,
                    ref hasBest);
            }

            for (int i = 0; i < planeCount; i++)
            {
                for (int j = i + 1; j < planeCount; j++)
                {
                    FixedVector3 crease = FixedVector3.Cross(
                        m_ConstraintPlanes[i].Normal,
                        m_ConstraintPlanes[j].Normal);
                    FixedScalar creaseLength = crease.Magnitude;
                    if (creaseLength <= m_Configuration.QueryTolerance)
                        continue;
                    FixedVector3 direction = Scale(crease, FixedScalar.One / creaseLength);
                    FixedVector3 candidate = Scale(direction, FixedVector3.Dot(remaining, direction));
                    SelectConstraintCandidate(
                        remaining,
                        candidate,
                        planeCount,
                        DeterministicKccConstraintCandidateKind.PlaneCrease,
                        i,
                        j,
                        ref best,
                        ref bestDistance,
                        ref bestKind,
                        ref bestFirstIndex,
                        ref bestSecondIndex,
                        ref hasBest);
                }
            }
            SelectConstraintCandidate(
                remaining,
                FixedVector3.Zero,
                planeCount,
                DeterministicKccConstraintCandidateKind.Zero,
                -1,
                -1,
                ref best,
                ref bestDistance,
                ref bestKind,
                ref bestFirstIndex,
                ref bestSecondIndex,
                ref hasBest);
            return best;
        }

        void SelectConstraintCandidate(
            FixedVector3 original,
            FixedVector3 candidate,
            int planeCount,
            DeterministicKccConstraintCandidateKind kind,
            int firstIndex,
            int secondIndex,
            ref FixedVector3 best,
            ref FixedScalar bestDistance,
            ref DeterministicKccConstraintCandidateKind bestKind,
            ref int bestFirstIndex,
            ref int bestSecondIndex,
            ref bool hasBest)
        {
            if (!IsConstraintCandidateFeasible(candidate, planeCount))
                return;
            FixedScalar distance = (candidate - original).SqrMagnitude;
            if (!hasBest || distance < bestDistance ||
                distance == bestDistance && IsConstraintCandidateBefore(
                    kind,
                    firstIndex,
                    secondIndex,
                    candidate,
                    bestKind,
                    bestFirstIndex,
                    bestSecondIndex,
                    best))
            {
                best = candidate;
                bestDistance = distance;
                bestKind = kind;
                bestFirstIndex = firstIndex;
                bestSecondIndex = secondIndex;
                hasBest = true;
            }
        }

        static bool IsConstraintCandidateBefore(
            DeterministicKccConstraintCandidateKind kind,
            int firstIndex,
            int secondIndex,
            FixedVector3 candidate,
            DeterministicKccConstraintCandidateKind bestKind,
            int bestFirstIndex,
            int bestSecondIndex,
            FixedVector3 best)
        {
            if (kind != bestKind)
                return (int)kind < (int)bestKind;
            if (firstIndex != bestFirstIndex)
                return firstIndex < bestFirstIndex;
            if (secondIndex != bestSecondIndex)
                return secondIndex < bestSecondIndex;
            if (candidate.X.Raw != best.X.Raw)
                return candidate.X.Raw < best.X.Raw;
            if (candidate.Y.Raw != best.Y.Raw)
                return candidate.Y.Raw < best.Y.Raw;
            return candidate.Z.Raw < best.Z.Raw;
        }

        bool IsConstraintCandidateFeasible(FixedVector3 candidate, int planeCount)
        {
            FixedScalar minimum = -m_Configuration.MinimumMovementDistance;
            for (int i = 0; i < planeCount; i++)
            {
                if (FixedVector3.Dot(candidate, m_ConstraintPlanes[i].Normal) < minimum)
                    return false;
            }
            return true;
        }

        static FixedVector3 ProjectOntoConstraintPlane(FixedVector3 value, FixedVector3 normal)
        {
            FixedScalar normalSqrMagnitude = normal.SqrMagnitude;
            return normalSqrMagnitude > FixedScalar.Zero
                ? value - Scale(normal, FixedVector3.Dot(value, normal) / normalSqrMagnitude)
                : FixedVector3.Zero;
        }

        WorldCollisionSummary ClassifyContact(DeterministicKccContact contact)
        {
            if (IsStableNormal(contact.SurfaceId, contact.Normal))
                return WorldCollisionSummary.Below;
            if (contact.Normal.Y <= -m_Configuration.MinimumGroundNormalY)
                return WorldCollisionSummary.Above;
            return WorldCollisionSummary.Sides;
        }

        static int CompareContactIdentity(DeterministicKccContact left, DeterministicKccContact right)
        {
            int surface = left.SurfaceId.CompareTo(right.SurfaceId);
            if (surface != 0)
                return surface;
            int primitive = left.PrimitiveId.CompareTo(right.PrimitiveId);
            return primitive != 0 ? primitive : left.FeatureId.CompareTo(right.FeatureId);
        }

        static FixedVector3 TangentToSurface(FixedVector3 direction, FixedVector3 surfaceNormal)
        {
            FixedVector3 right = FixedVector3.Cross(direction, Up);
            return FixedVector3.Cross(surfaceNormal, right).Normalized;
        }

        FixedVector3 GetObstructionNormal(
            FixedVector3 hitNormal,
            bool stableOnHit,
            DeterministicKccBodyState previousState)
        {
            if (!previousState.IsStableOnGround ||
                stableOnHit && hitNormal.Y >= m_Configuration.MinimumGroundNormalY)
                return hitNormal;
            FixedVector3 obstructionLeft = FixedVector3.Cross(previousState.GroundNormal, hitNormal).Normalized;
            FixedVector3 obstructionNormal = FixedVector3.Cross(obstructionLeft, Up).Normalized;
            return obstructionNormal.SqrMagnitude == FixedScalar.Zero ? hitNormal : obstructionNormal;
        }

        static FixedVector3 ProjectOnPlane(FixedVector3 value, FixedVector3 normal) =>
            value - Scale(normal, FixedVector3.Dot(value, normal));

        static FixedVector3 Planar(FixedVector3 value) =>
            new FixedVector3(value.X, FixedScalar.Zero, value.Z);

        static FixedVector3 Scale(FixedVector3 value, FixedScalar scale) =>
            new FixedVector3(value.X * scale, value.Y * scale, value.Z * scale);
    }
}
