using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    internal enum DeterministicKccStepMode : byte
    {
        None = 0,
        Standard = 1,
        Extra = 2
    }

    internal enum DeterministicKccStepStage : byte
    {
        None = 0,
        Detection = 1,
        Commit = 2
    }

    internal enum DeterministicKccStepRejection : byte
    {
        None = 0,
        PreviousStableGroundAbsent = 1,
        UpwardIntent = 2,
        ObstructionNotVertical = 3,
        StandardSweepAbsent = 4,
        ExtraSweepAbsent = 5,
        CandidateOverlap = 6,
        OuterGroundAbsent = 7,
        OuterGroundUnstable = 8,
        UpwardClearanceBlocked = 9,
        InnerGroundAbsent = 10,
        InnerGroundUnstable = 11,
        CommitLandingAbsent = 12,
        CommitSurfaceMismatch = 13,
        FinalOverlap = 14,
        ObstructionNotClosing = 15,
        ObstacleHeightExceeded = 16
    }

    internal readonly struct DeterministicKccGroundReport
    {
        public DeterministicKccGroundReport(
            bool foundAnyGround,
            bool baseIsStable,
            bool isStableOnGround,
            int surfaceId,
            int primitiveId,
            DeterministicCollisionFeatureId featureId,
            FixedVector3 groundNormal,
            FixedVector3 innerNormal,
            FixedVector3 outerNormal,
            FixedScalar distance,
            FixedScalar probeDistance,
            FixedScalar denivelationNormalDot,
            bool snappingPrevented,
            DeterministicKccLedgeState ledgeState,
            bool lastMovementIterationFoundAnyGround)
        {
            FoundAnyGround = foundAnyGround;
            BaseIsStable = baseIsStable;
            IsStableOnGround = isStableOnGround;
            SurfaceId = surfaceId;
            PrimitiveId = primitiveId;
            FeatureId = featureId;
            GroundNormal = groundNormal;
            InnerNormal = innerNormal;
            OuterNormal = outerNormal;
            Distance = distance;
            ProbeDistance = probeDistance;
            DenivelationNormalDot = denivelationNormalDot;
            SnappingPrevented = snappingPrevented;
            LedgeState = ledgeState;
            LastMovementIterationFoundAnyGround = lastMovementIterationFoundAnyGround;
        }

        public bool FoundAnyGround { get; }
        public bool BaseIsStable { get; }
        public bool IsStableOnGround { get; }
        public int SurfaceId { get; }
        public int PrimitiveId { get; }
        public DeterministicCollisionFeatureId FeatureId { get; }
        public FixedVector3 GroundNormal { get; }
        public FixedVector3 InnerNormal { get; }
        public FixedVector3 OuterNormal { get; }
        public FixedScalar Distance { get; }
        public FixedScalar ProbeDistance { get; }
        public FixedScalar DenivelationNormalDot { get; }
        public bool SnappingPrevented { get; }
        public DeterministicKccLedgeState LedgeState { get; }
        public bool LastMovementIterationFoundAnyGround { get; }

        public static DeterministicKccGroundReport NoGround(bool lastMovementIterationFoundAnyGround) =>
            new DeterministicKccGroundReport(
            false,
            false,
            false,
            -1,
            -1,
            DeterministicCollisionFeatureId.Invalid,
            FixedVector3.Zero,
            FixedVector3.Zero,
            FixedVector3.Zero,
            FixedScalar.Zero,
            FixedScalar.Zero,
            FixedScalar.One,
            false,
            DeterministicKccLedgeState.None,
            lastMovementIterationFoundAnyGround);
    }

    internal readonly struct DeterministicKccHitStabilityReport
    {
        public DeterministicKccHitStabilityReport(
            bool baseIsStable,
            bool isStable,
            bool foundInnerNormal,
            FixedVector3 innerNormal,
            bool foundOuterNormal,
            FixedVector3 outerNormal,
            bool validStepDetected,
            int steppedSurfaceId,
            bool ledgeDetected,
            bool isOnEmptySideOfLedge,
            FixedScalar distanceFromLedge,
            bool isMovingTowardsEmptySideOfLedge,
            FixedVector3 ledgeGroundNormal,
            FixedVector3 ledgeDirection,
            FixedScalar denivelationNormalDot,
            bool snappingPrevented,
            DeterministicKccStepMode stepMode)
        {
            BaseIsStable = baseIsStable;
            IsStable = isStable;
            FoundInnerNormal = foundInnerNormal;
            InnerNormal = innerNormal;
            FoundOuterNormal = foundOuterNormal;
            OuterNormal = outerNormal;
            ValidStepDetected = validStepDetected;
            SteppedSurfaceId = steppedSurfaceId;
            LedgeDetected = ledgeDetected;
            IsOnEmptySideOfLedge = isOnEmptySideOfLedge;
            DistanceFromLedge = distanceFromLedge;
            IsMovingTowardsEmptySideOfLedge = isMovingTowardsEmptySideOfLedge;
            LedgeGroundNormal = ledgeGroundNormal;
            LedgeDirection = ledgeDirection;
            DenivelationNormalDot = denivelationNormalDot;
            SnappingPrevented = snappingPrevented;
            StepMode = stepMode;
        }

        public bool BaseIsStable { get; }
        public bool IsStable { get; }
        public bool FoundInnerNormal { get; }
        public FixedVector3 InnerNormal { get; }
        public bool FoundOuterNormal { get; }
        public FixedVector3 OuterNormal { get; }
        public bool ValidStepDetected { get; }
        public int SteppedSurfaceId { get; }
        public bool LedgeDetected { get; }
        public bool IsOnEmptySideOfLedge { get; }
        public FixedScalar DistanceFromLedge { get; }
        public bool IsMovingTowardsEmptySideOfLedge { get; }
        public FixedVector3 LedgeGroundNormal { get; }
        public FixedVector3 LedgeDirection { get; }
        public FixedScalar DenivelationNormalDot { get; }
        public bool SnappingPrevented { get; }
        public DeterministicKccStepMode StepMode { get; }
    }

    internal readonly struct DeterministicKccStepDiagnostics
    {
        public DeterministicKccStepDiagnostics(
            DeterministicKccStepMode mode,
            DeterministicKccStepStage stage,
            DeterministicKccStepRejection rejection,
            int steppedSurfaceId,
            DeterministicKccQuerySummary querySummary,
            int representativeEvaluationCount = 0,
            int stepDetectionAttemptCount = 0,
            int stabilityEvaluationCount = 0,
            int standardStepQueryCount = 0,
            int extraStepQueryCount = 0,
            int stepValidityCandidateCount = 0,
            int groundProbeEvaluationCount = 0)
        {
            Mode = mode;
            Stage = stage;
            Rejection = rejection;
            SteppedSurfaceId = steppedSurfaceId;
            QuerySummary = querySummary;
            RepresentativeEvaluationCount = representativeEvaluationCount;
            StepDetectionAttemptCount = stepDetectionAttemptCount;
            StabilityEvaluationCount = stabilityEvaluationCount;
            StandardStepQueryCount = standardStepQueryCount;
            ExtraStepQueryCount = extraStepQueryCount;
            StepValidityCandidateCount = stepValidityCandidateCount;
            GroundProbeEvaluationCount = groundProbeEvaluationCount;
        }

        public DeterministicKccStepMode Mode { get; }
        public DeterministicKccStepStage Stage { get; }
        public DeterministicKccStepRejection Rejection { get; }
        public int SteppedSurfaceId { get; }
        public DeterministicKccQuerySummary QuerySummary { get; }
        public int RepresentativeEvaluationCount { get; }
        public int StepDetectionAttemptCount { get; }
        public int StabilityEvaluationCount { get; }
        public int StandardStepQueryCount { get; }
        public int ExtraStepQueryCount { get; }
        public int StepValidityCandidateCount { get; }
        public int GroundProbeEvaluationCount { get; }

        public DeterministicKccStepDiagnostics WithCounters(
            int representativeEvaluationCount,
            int stepDetectionAttemptCount,
            int stabilityEvaluationCount,
            int standardStepQueryCount,
            int extraStepQueryCount,
            int stepValidityCandidateCount,
            int groundProbeEvaluationCount) => new DeterministicKccStepDiagnostics(
                Mode,
                Stage,
                Rejection,
                SteppedSurfaceId,
                QuerySummary,
                representativeEvaluationCount,
                stepDetectionAttemptCount,
                stabilityEvaluationCount,
                standardStepQueryCount,
                extraStepQueryCount,
                stepValidityCandidateCount,
                groundProbeEvaluationCount);
    }

    internal readonly struct DeterministicKccStepCandidate
    {
        public DeterministicKccStepCandidate(
            FixedVector3 position,
            DeterministicKccContact landing,
            DeterministicKccStepMode mode)
        {
            Position = position;
            Landing = landing;
            Mode = mode;
        }

        public FixedVector3 Position { get; }
        public DeterministicKccContact Landing { get; }
        public DeterministicKccStepMode Mode { get; }
    }
}
