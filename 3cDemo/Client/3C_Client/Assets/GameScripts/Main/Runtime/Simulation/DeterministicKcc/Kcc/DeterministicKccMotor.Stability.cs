using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    internal sealed partial class DeterministicKccMotor
    {
        DeterministicKccHitStabilityReport EvaluateHitStability(
            FixedVector3 characterPosition,
            DeterministicKccContact contact,
            DeterministicKccBodyState previousState,
            FixedVector3 movement,
            bool allowStepDetection,
            DeterministicKccStepRejection admissionRejection,
            ref DeterministicKccQuerySummary summary,
            ref DeterministicKccStepDiagnostics diagnostics)
        {
            m_StabilityEvaluationCount++;
            FixedVector3 innerHitDirection = Planar(contact.Normal).Normalized;
            bool baseIsStable = IsStableNormal(contact.SurfaceId, contact.Normal);
            bool isStable = baseIsStable;
            bool foundInner = m_Queries.Raycast(
                contact.WorldPoint + Scale(Up, m_Configuration.SecondaryProbeVerticalDistance) +
                Scale(innerHitDirection, m_Configuration.SecondaryProbeHorizontalDistance),
                Down,
                m_Configuration.MaximumStepHeight + m_Configuration.SecondaryProbeVerticalDistance,
                out DeterministicKccRayHit innerHit,
                out DeterministicKccQuerySummary innerSummary);
            summary = summary.Add(innerSummary);
            bool foundOuter = m_Queries.Raycast(
                contact.WorldPoint + Scale(Up, m_Configuration.SecondaryProbeVerticalDistance) -
                Scale(innerHitDirection, m_Configuration.SecondaryProbeHorizontalDistance),
                Down,
                m_Configuration.MaximumStepHeight + m_Configuration.SecondaryProbeVerticalDistance,
                out DeterministicKccRayHit outerHit,
                out DeterministicKccQuerySummary outerSummary);
            summary = summary.Add(outerSummary);

            FixedVector3 innerNormal = foundInner ? innerHit.Normal : contact.Normal;
            FixedVector3 outerNormal = foundOuter ? outerHit.Normal : contact.Normal;
            bool stableInner = foundInner && IsStableNormal(innerHit.SurfaceId, innerHit.Normal);
            bool stableOuter = foundOuter && IsStableNormal(outerHit.SurfaceId, outerHit.Normal);
            bool ledgeDetected = stableInner != stableOuter;
            bool emptySide = ledgeDetected && stableOuter && !stableInner;
            FixedVector3 ledgeGroundNormal = ledgeDetected
                ? stableOuter ? outerNormal : innerNormal
                : FixedVector3.Zero;
            FixedVector3 ledgeDirection = FixedVector3.Zero;
            FixedScalar distanceFromLedge = FixedScalar.Zero;
            bool movingTowardsEmpty = false;
            if (ledgeDetected)
            {
                FixedVector3 ledgeRight = FixedVector3.Cross(contact.Normal, ledgeGroundNormal).Normalized;
                ledgeDirection = Planar(FixedVector3.Cross(ledgeGroundNormal, ledgeRight)).Normalized;
                distanceFromLedge = Planar(contact.WorldPoint - characterPosition).Magnitude;
                FixedVector3 movementDirection = movement.Normalized;
                movingTowardsEmpty = movementDirection.SqrMagnitude > FixedScalar.Zero &&
                                     FixedVector3.Dot(movementDirection, ledgeDirection) > FixedScalar.Zero;
            }

            bool snappingPrevented = false;
            FixedScalar denivelationNormalDot = FixedScalar.One;
            if (isStable && ledgeDetected &&
                (movingTowardsEmpty || emptySide && distanceFromLedge > m_Configuration.MaximumStableDistanceFromLedge))
            {
                isStable = false;
                snappingPrevented = true;
            }
            if (isStable && previousState.FoundAnyGround &&
                innerNormal.SqrMagnitude > FixedScalar.Zero && outerNormal.SqrMagnitude > FixedScalar.Zero)
            {
                denivelationNormalDot = FixedVector3.Dot(innerNormal.Normalized, outerNormal.Normalized);
                if (previousState.InnerGroundNormal.SqrMagnitude > FixedScalar.Zero)
                {
                    denivelationNormalDot = FixedScalar.Min(
                        denivelationNormalDot,
                        FixedVector3.Dot(previousState.InnerGroundNormal.Normalized, outerNormal.Normalized));
                }
                if (denivelationNormalDot < m_Configuration.MinimumStableDenivelationNormalDot)
                {
                    isStable = false;
                    snappingPrevented = true;
                }
            }

            bool validStep = false;
            int steppedSurfaceId = -1;
            DeterministicKccStepMode stepMode = DeterministicKccStepMode.None;
            DeterministicKccStepCandidate stepCandidate = default;
            DeterministicKccStepRejection rejection = DeterministicKccStepRejection.ExtraSweepAbsent;
            if (!isStable && allowStepDetection && admissionRejection == DeterministicKccStepRejection.None &&
                innerHitDirection.SqrMagnitude > FixedScalar.Zero &&
                TryDetectStep(
                    characterPosition,
                    contact,
                    innerHitDirection,
                    ref summary,
                    out stepCandidate,
                    out rejection))
            {
                validStep = true;
                steppedSurfaceId = stepCandidate.Landing.SurfaceId;
                stepMode = stepCandidate.Mode;
                isStable = true;
                diagnostics = new DeterministicKccStepDiagnostics(
                    stepMode,
                    DeterministicKccStepStage.Detection,
                    DeterministicKccStepRejection.None,
                    steppedSurfaceId,
                    summary);
            }
            else if (!isStable && allowStepDetection && innerHitDirection.SqrMagnitude > FixedScalar.Zero)
            {
                diagnostics = new DeterministicKccStepDiagnostics(
                    DeterministicKccStepMode.Extra,
                    DeterministicKccStepStage.Detection,
                    admissionRejection != DeterministicKccStepRejection.None ? admissionRejection : rejection,
                    -1,
                    summary);
            }

            return new DeterministicKccHitStabilityReport(
                baseIsStable,
                isStable,
                foundInner,
                innerNormal,
                foundOuter,
                outerNormal,
                validStep,
                steppedSurfaceId,
                ledgeDetected,
                emptySide,
                distanceFromLedge,
                movingTowardsEmpty,
                ledgeGroundNormal,
                ledgeDirection,
                denivelationNormalDot,
                snappingPrevented,
                stepMode);
        }

        bool TryDetectStep(
            FixedVector3 characterPosition,
            DeterministicKccContact obstruction,
            FixedVector3 innerHitDirection,
            ref DeterministicKccQuerySummary summary,
            out DeterministicKccStepCandidate candidate,
            out DeterministicKccStepRejection rejection)
        {
            m_StepDetectionAttemptCount++;
            FixedVector3 verticalCharacterToHit = Scale(Up, obstruction.WorldPoint.Y - characterPosition.Y);
            FixedVector3 horizontalCharacterToHit = Planar(obstruction.WorldPoint - characterPosition).Normalized;
            FixedVector3 standardStart = obstruction.WorldPoint - verticalCharacterToHit +
                                         Scale(Up, m_Configuration.MaximumStepHeight) +
                                         Scale(horizontalCharacterToHit, m_Configuration.CollisionOffset * FixedScalar.FromInt64(3));
            FixedScalar standardDistance = m_Configuration.MaximumStepHeight + m_Configuration.CollisionOffset;
            m_StandardStepQueryCount++;
            int standardCount = m_Queries.CastAll(
                standardStart,
                Scale(Down, standardDistance),
                out DeterministicKccQuerySummary standardSummary);
            summary = summary.Add(standardSummary);
            if (CheckStepValidity(
                    standardCount,
                    characterPosition,
                    innerHitDirection,
                    standardStart,
                    standardDistance,
                    DeterministicKccStepMode.Standard,
                    ref summary,
                    out candidate,
                    out rejection))
            {
                return true;
            }

            FixedVector3 extraStart = characterPosition + Scale(Up, m_Configuration.MaximumStepHeight) -
                                      Scale(innerHitDirection, m_Configuration.MinimumRequiredStepDepth);
            FixedScalar extraDistance = m_Configuration.MaximumStepHeight - m_Configuration.CollisionOffset;
            m_ExtraStepQueryCount++;
            int extraCount = m_Queries.CastAll(
                extraStart,
                Scale(Down, extraDistance),
                out DeterministicKccQuerySummary extraSummary);
            summary = summary.Add(extraSummary);
            if (CheckStepValidity(
                    extraCount,
                    characterPosition,
                    innerHitDirection,
                    extraStart,
                    extraDistance,
                    DeterministicKccStepMode.Extra,
                    ref summary,
                    out candidate,
                    out rejection))
            {
                return true;
            }
            rejection = extraCount == 0
                ? DeterministicKccStepRejection.ExtraSweepAbsent
                : rejection;
            return false;
        }

        bool CheckStepValidity(
            int hitCount,
            FixedVector3 characterPosition,
            FixedVector3 innerHitDirection,
            FixedVector3 castStart,
            FixedScalar castDistance,
            DeterministicKccStepMode mode,
            ref DeterministicKccQuerySummary summary,
            out DeterministicKccStepCandidate candidate,
            out DeterministicKccStepRejection rejection)
        {
            candidate = default;
            rejection = mode == DeterministicKccStepMode.Standard
                ? DeterministicKccStepRejection.StandardSweepAbsent
                : DeterministicKccStepRejection.ExtraSweepAbsent;
            int groupEnd = hitCount - 1;
            while (groupEnd >= 0)
            {
                FixedScalar time = m_Queries.AllCastContactAt(groupEnd).TimeOfImpact;
                int groupStart = groupEnd;
                while (groupStart > 0 && m_Queries.AllCastContactAt(groupStart - 1).TimeOfImpact == time)
                    groupStart--;
                for (int i = groupEnd; i >= groupStart; i--)
                {
                    m_StepValidityCandidateCount++;
                    DeterministicKccContact landing = m_Queries.AllCastContactAt(i);
                    FixedVector3 candidatePosition = castStart + Scale(
                        Down,
                        castDistance * landing.TimeOfImpact - m_Configuration.CollisionOffset);
                    if (HasPenetratingOverlap(candidatePosition, ref summary))
                    {
                        rejection = DeterministicKccStepRejection.CandidateOverlap;
                        continue;
                    }

                    bool foundOuter = m_Queries.Raycast(
                        landing.WorldPoint + Scale(Up, m_Configuration.SecondaryProbeVerticalDistance) -
                        Scale(innerHitDirection, m_Configuration.SecondaryProbeHorizontalDistance),
                        Down,
                        m_Configuration.MaximumStepHeight + m_Configuration.SecondaryProbeVerticalDistance,
                        out DeterministicKccRayHit outerHit,
                        out DeterministicKccQuerySummary outerSummary);
                    summary = summary.Add(outerSummary);
                    if (!foundOuter)
                    {
                        rejection = DeterministicKccStepRejection.OuterGroundAbsent;
                        continue;
                    }
                    if (!IsStableNormal(outerHit.SurfaceId, outerHit.Normal))
                    {
                        rejection = DeterministicKccStepRejection.OuterGroundUnstable;
                        continue;
                    }

                    FixedScalar rise = candidatePosition.Y - characterPosition.Y;
                    if (rise > m_Configuration.MinimumMovementDistance)
                    {
                        bool clearanceBlocked = m_Queries.Cast(
                            characterPosition,
                            Scale(Up, rise),
                            out _,
                            out _,
                            out DeterministicKccQuerySummary clearanceSummary);
                        summary = summary.Add(clearanceSummary);
                        if (clearanceBlocked)
                        {
                            rejection = DeterministicKccStepRejection.UpwardClearanceBlocked;
                            continue;
                        }
                    }

                    bool foundInner = m_Queries.Raycast(
                        characterPosition + Scale(Up, rise),
                        Down,
                        m_Configuration.MaximumStepHeight,
                        out DeterministicKccRayHit innerHit,
                        out DeterministicKccQuerySummary innerSummary);
                    summary = summary.Add(innerSummary);
                    bool innerStable = foundInner && IsStableNormal(innerHit.SurfaceId, innerHit.Normal);
                    if (!innerStable)
                    {
                        foundInner = m_Queries.Raycast(
                            landing.WorldPoint + Scale(innerHitDirection, m_Configuration.SecondaryProbeHorizontalDistance),
                            Down,
                            m_Configuration.MaximumStepHeight,
                            out innerHit,
                            out innerSummary);
                        summary = summary.Add(innerSummary);
                        innerStable = foundInner && IsStableNormal(innerHit.SurfaceId, innerHit.Normal);
                    }
                    if (!foundInner)
                    {
                        rejection = DeterministicKccStepRejection.InnerGroundAbsent;
                        continue;
                    }
                    if (!innerStable)
                    {
                        rejection = DeterministicKccStepRejection.InnerGroundUnstable;
                        continue;
                    }

                    candidate = new DeterministicKccStepCandidate(candidatePosition, landing, mode);
                    rejection = DeterministicKccStepRejection.None;
                    return true;
                }
                groupEnd = groupStart - 1;
            }
            return false;
        }

        bool TryCommitStep(
            FixedVector3 safePosition,
            FixedVector3 obstructionNormal,
            DeterministicKccHitStabilityReport stability,
            ref DeterministicKccQuerySummary summary,
            out DeterministicKccStepCandidate candidate,
            out DeterministicKccStepRejection rejection)
        {
            candidate = default;
            FixedVector3 forward = Planar(-obstructionNormal).Normalized;
            FixedVector3 castStart = safePosition + Scale(forward, m_Configuration.SteppingForwardDistance) +
                                     Scale(Up, m_Configuration.MaximumStepHeight);
            int count = m_Queries.CastAll(
                castStart,
                Scale(Down, m_Configuration.MaximumStepHeight),
                out DeterministicKccQuerySummary castSummary);
            summary = summary.Add(castSummary);
            if (count == 0)
            {
                rejection = DeterministicKccStepRejection.CommitLandingAbsent;
                return false;
            }
            for (int i = 0; i < count; i++)
            {
                DeterministicKccContact landing = m_Queries.AllCastContactAt(i);
                if (landing.SurfaceId != stability.SteppedSurfaceId)
                    continue;
                FixedVector3 finalPosition = castStart + Scale(
                    Down,
                    m_Configuration.MaximumStepHeight * landing.TimeOfImpact - m_Configuration.CollisionOffset);
                if (HasPenetratingOverlap(finalPosition, ref summary))
                {
                    rejection = DeterministicKccStepRejection.FinalOverlap;
                    return false;
                }
                candidate = new DeterministicKccStepCandidate(finalPosition, landing, stability.StepMode);
                rejection = DeterministicKccStepRejection.None;
                return true;
            }
            rejection = DeterministicKccStepRejection.CommitSurfaceMismatch;
            return false;
        }

        bool HasPenetratingOverlap(FixedVector3 position, ref DeterministicKccQuerySummary summary)
        {
            int count = m_Queries.Overlap(position, out DeterministicKccQuerySummary overlapSummary);
            summary = summary.Add(overlapSummary);
            for (int i = 0; i < count; i++)
            {
                if (m_Queries.OverlapContactAt(i).Separation < -m_Configuration.QueryTolerance)
                    return true;
            }
            return false;
        }

        bool IsStableNormal(int surfaceId, FixedVector3 normal) =>
            surfaceId >= 0 && surfaceId < m_World.Surfaces.Count &&
            m_World.Surfaces[surfaceId].Walkable &&
            normal.Y >= m_Configuration.MinimumGroundNormalY;
    }
}
