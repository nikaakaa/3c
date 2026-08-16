using ThirdPersonCharacter.Pipeline.Animation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public readonly struct CharacterFootGroundingQueryDiagnostics
    {
        internal CharacterFootGroundingQueryDiagnostics(
            in CharacterFootPlacementQueryRequest request)
        {
            Shape = request.Shape;
            Purpose = request.Purpose;
            FootIndex = request.FootIndex;
            Origin = request.Origin;
            CapsuleEnd = request.CapsuleEnd;
            Direction = request.Direction;
            Radius = request.Radius;
            MaximumDistance = request.MaximumDistance;
            LayerMask = request.LayerMask;
            MinimumGroundNormalDot = request.MinimumGroundNormalDot;
            IsAvailable = true;
        }

        public CharacterFootPlacementQueryShape Shape { get; }
        public CharacterFootPlacementQueryPurpose Purpose { get; }
        public int FootIndex { get; }
        public Vector3 Origin { get; }
        public Vector3 CapsuleEnd { get; }
        public Vector3 Direction { get; }
        public float Radius { get; }
        public float MaximumDistance { get; }
        public int LayerMask { get; }
        public float MinimumGroundNormalDot { get; }
        public bool IsAvailable { get; }
    }

    public readonly struct CharacterFootGroundingHitDiagnostics
    {
        internal CharacterFootGroundingHitDiagnostics(
            CharacterFootPlacementQueryHit hit)
        {
            HasHit = hit.HasHit;
            SurfaceIdentity = hit.SurfaceIdentity;
            Location = hit.Location;
            Point = hit.Point;
            Normal = hit.Normal;
            Distance = hit.Distance;
        }

        internal CharacterFootGroundingHitDiagnostics(
            FootPlacementSurface surface)
        {
            HasHit = surface.IsValid;
            SurfaceIdentity = surface.Identity;
            Location = surface.Point;
            Point = surface.Point;
            Normal = surface.Normal;
            Distance = 0f;
        }

        public bool HasHit { get; }
        public int SurfaceIdentity { get; }
        public Vector3 Location { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public float Distance { get; }
    }

    public readonly struct CharacterFootGroundingFootDiagnostics
    {
        internal CharacterFootGroundingFootDiagnostics(
            CharacterFootSide side,
            in CharacterLyraCurrentGroundingFootResult lyra,
            AnimationFootFeatureSample footFeature,
            CharacterFootContactState contactState,
            FootConstraintTransitionReason transitionReason,
            CharacterFootContactDecision contactDecision,
            bool contactSurfaceValid,
            bool contactSurfaceDistanceAccepted,
            bool contactCaptureSpeedAccepted,
            bool contactRetentionSpeedAccepted,
            bool contactConfidenceAccepted,
            float maximumContactSurfaceDistance,
            float plantSpeedThreshold,
            float unalignmentSpeedThreshold,
            float plantConfidenceEnter,
            float plantConfidenceExit,
            float anchorDistance,
            bool anchorDistanceAccepted,
            float maximumAnchorDistance,
            float anchorBlendSpeed,
            FootPlacementSurface currentSurface,
            Vector3 surfaceLocalAnchor,
            Quaternion surfaceLocalRotation,
            Vector3 anchorWorldPosition,
            Quaternion anchorWorldRotation,
            bool hasSurfaceAnchor,
            float anchorBlendWeight,
            float placementWeight,
            bool plantContact,
            float animationFootSpeed,
            float surfaceDistance,
            FootPlacementSurface soleSupport,
            Vector3 soleAnklePosition,
            CharacterFootPlacementSoleContactPose soleContacts,
            float soleHeelPlaneDistance,
            float soleToePlaneDistance,
            float residualSolePenetration,
            Vector3 soleClearanceTargetTranslation,
            float animatedAnkleComponentY,
            Vector3 baselineComponentPosition,
            Quaternion baselineComponentRotation,
            CharacterFullBodyIkGoal goal)
        {
            Side = side;
            Query = lyra.Trace.Request.MaximumDistance > 0f
                ? new CharacterFootGroundingQueryDiagnostics(lyra.Trace.Request)
                : default;
            CurrentHit = new CharacterFootGroundingHitDiagnostics(lyra.Trace.Hit);
            DidTraceHit = lyra.Trace.DidTraceHit;
            TargetOffset = lyra.Trace.TargetOffset;
            SoleClearanceTarget = lyra.SoleClearanceTarget;
            OffsetTarget = lyra.OffsetTarget;
            CurrentOffset = lyra.CurrentOffset;
            OffsetSpringVelocity = lyra.OffsetVelocity;
            PreviousOffsetTarget = lyra.PreviousOffsetTarget;
            OffsetSpringInitialized = lyra.OffsetSpringInitialized;
            TargetNormal = lyra.Trace.DidTraceHit ? lyra.Trace.Hit.Normal : Vector3.up;
            CurrentNormal = lyra.CurrentHitNormal;
            NormalSpringVelocity = lyra.NormalVelocity;
            PreviousNormalTarget = lyra.PreviousNormalTarget;
            NormalSpringInitialized = lyra.NormalSpringInitialized;
            CurrentGroundingComponentPosition = lyra.ComponentPosition;
            CurrentGroundingComponentRotation = lyra.ComponentRotation;
            BaselineComponentPosition = baselineComponentPosition;
            BaselineComponentRotation = baselineComponentRotation;
            FootFeature = footFeature;
            ContactState = contactState;
            TransitionReason = transitionReason;
            ContactDecision = contactDecision;
            ContactSurfaceValid = contactSurfaceValid;
            ContactSurfaceDistanceAccepted = contactSurfaceDistanceAccepted;
            ContactCaptureSpeedAccepted = contactCaptureSpeedAccepted;
            ContactRetentionSpeedAccepted = contactRetentionSpeedAccepted;
            ContactConfidenceAccepted = contactConfidenceAccepted;
            MaximumContactSurfaceDistance = maximumContactSurfaceDistance;
            PlantSpeedThreshold = plantSpeedThreshold;
            UnalignmentSpeedThreshold = unalignmentSpeedThreshold;
            PlantConfidenceEnter = plantConfidenceEnter;
            PlantConfidenceExit = plantConfidenceExit;
            AnchorDistance = anchorDistance;
            AnchorDistanceAccepted = anchorDistanceAccepted;
            MaximumAnchorDistance = maximumAnchorDistance;
            AnchorBlendSpeed = anchorBlendSpeed;
            CurrentSurface = new CharacterFootGroundingHitDiagnostics(currentSurface);
            SurfaceLocalAnchor = surfaceLocalAnchor;
            SurfaceLocalRotation = surfaceLocalRotation;
            AnchorWorldPosition = anchorWorldPosition;
            AnchorWorldRotation = anchorWorldRotation;
            HasSurfaceAnchor = hasSurfaceAnchor;
            AnchorBlendWeight = anchorBlendWeight;
            PlacementWeight = placementWeight;
            PlantContact = plantContact;
            AnimationFootSpeed = animationFootSpeed;
            SurfaceDistance = surfaceDistance;
            SoleSupport = new CharacterFootGroundingHitDiagnostics(soleSupport);
            SoleAnklePosition = soleAnklePosition;
            SoleHeelPosition = soleContacts.HeelPosition;
            SoleToePosition = soleContacts.ToePosition;
            SoleHeelPlaneDistance = soleHeelPlaneDistance;
            SoleToePlaneDistance = soleToePlaneDistance;
            ResidualSolePenetration = residualSolePenetration;
            SoleClearanceTargetTranslation = soleClearanceTargetTranslation;
            AnimatedAnkleComponentY = animatedAnkleComponentY;
            Goal = goal;
        }

        public CharacterFootSide Side { get; }
        public CharacterFootGroundingQueryDiagnostics Query { get; }
        public CharacterFootGroundingHitDiagnostics CurrentHit { get; }
        public bool DidTraceHit { get; }
        public float TargetOffset { get; }
        public float SoleClearanceTarget { get; }
        public float OffsetTarget { get; }
        public float CurrentOffset { get; }
        public float OffsetSpringVelocity { get; }
        public float PreviousOffsetTarget { get; }
        public bool OffsetSpringInitialized { get; }
        public Vector3 TargetNormal { get; }
        public Vector3 CurrentNormal { get; }
        public Vector3 NormalSpringVelocity { get; }
        public Vector3 PreviousNormalTarget { get; }
        public bool NormalSpringInitialized { get; }
        public Vector3 CurrentGroundingComponentPosition { get; }
        public Quaternion CurrentGroundingComponentRotation { get; }
        public Vector3 BaselineComponentPosition { get; }
        public Quaternion BaselineComponentRotation { get; }
        public AnimationFootFeatureSample FootFeature { get; }
        public CharacterFootContactState ContactState { get; }
        public FootConstraintTransitionReason TransitionReason { get; }
        public CharacterFootContactDecision ContactDecision { get; }
        public bool ContactSurfaceValid { get; }
        public bool ContactSurfaceDistanceAccepted { get; }
        public bool ContactCaptureSpeedAccepted { get; }
        public bool ContactRetentionSpeedAccepted { get; }
        public bool ContactConfidenceAccepted { get; }
        public float MaximumContactSurfaceDistance { get; }
        public float PlantSpeedThreshold { get; }
        public float UnalignmentSpeedThreshold { get; }
        public float PlantConfidenceEnter { get; }
        public float PlantConfidenceExit { get; }
        public float AnchorDistance { get; }
        public bool AnchorDistanceAccepted { get; }
        public float MaximumAnchorDistance { get; }
        public float AnchorBlendSpeed { get; }
        public CharacterFootGroundingHitDiagnostics CurrentSurface { get; }
        public Vector3 SurfaceLocalAnchor { get; }
        public Quaternion SurfaceLocalRotation { get; }
        public Vector3 AnchorWorldPosition { get; }
        public Quaternion AnchorWorldRotation { get; }
        public bool HasSurfaceAnchor { get; }
        public float AnchorBlendWeight { get; }
        public float PlacementWeight { get; }
        public bool PlantContact { get; }
        public float AnimationFootSpeed { get; }
        public float SurfaceDistance { get; }
        public CharacterFootGroundingHitDiagnostics SoleSupport { get; }
        public Vector3 SoleAnklePosition { get; }
        public Vector3 SoleHeelPosition { get; }
        public Vector3 SoleToePosition { get; }
        public float SoleHeelPlaneDistance { get; }
        public float SoleToePlaneDistance { get; }
        public float ResidualSolePenetration { get; }
        public Vector3 SoleClearanceTargetTranslation { get; }
        public float AnimatedAnkleComponentY { get; }
        public CharacterFullBodyIkGoal Goal { get; }
    }

    public readonly struct CharacterFootPlacementPelvisSupportDiagnostics
    {
        internal CharacterFootPlacementPelvisSupportDiagnostics(
            bool hasSelectedSupport,
            CharacterFootSide selectedSide,
            bool supportSwitched,
            ulong selectedPlanSequence,
            float currentTarget,
            float resolvedTarget,
            bool leftHasActionConstraint,
            AnimationFootConstraintMode leftConstraintMode,
            AnimationFootSupportPhase leftSupportPhase,
            AnimationBodyRotationPivotMode leftBodyPivotMode,
            bool leftCandidate,
            ulong leftPlanSequence,
            float leftDisplacement,
            bool rightHasActionConstraint,
            AnimationFootConstraintMode rightConstraintMode,
            AnimationFootSupportPhase rightSupportPhase,
            AnimationBodyRotationPivotMode rightBodyPivotMode,
            bool rightCandidate,
            ulong rightPlanSequence,
            float rightDisplacement)
        {
            HasSelectedSupport = hasSelectedSupport;
            SelectedSide = selectedSide;
            SupportSwitched = supportSwitched;
            SelectedPlanSequence = selectedPlanSequence;
            CurrentTarget = currentTarget;
            ResolvedTarget = resolvedTarget;
            LeftHasActionConstraint = leftHasActionConstraint;
            LeftConstraintMode = leftConstraintMode;
            LeftSupportPhase = leftSupportPhase;
            LeftBodyPivotMode = leftBodyPivotMode;
            LeftCandidate = leftCandidate;
            LeftPlanSequence = leftPlanSequence;
            LeftDisplacement = leftDisplacement;
            RightHasActionConstraint = rightHasActionConstraint;
            RightConstraintMode = rightConstraintMode;
            RightSupportPhase = rightSupportPhase;
            RightBodyPivotMode = rightBodyPivotMode;
            RightCandidate = rightCandidate;
            RightPlanSequence = rightPlanSequence;
            RightDisplacement = rightDisplacement;
        }

        public bool HasSelectedSupport { get; }
        public CharacterFootSide SelectedSide { get; }
        public bool SupportSwitched { get; }
        public ulong SelectedPlanSequence { get; }
        public float CurrentTarget { get; }
        public float ResolvedTarget { get; }
        public bool LeftHasActionConstraint { get; }
        public AnimationFootConstraintMode LeftConstraintMode { get; }
        public AnimationFootSupportPhase LeftSupportPhase { get; }
        public AnimationBodyRotationPivotMode LeftBodyPivotMode { get; }
        public bool LeftCandidate { get; }
        public ulong LeftPlanSequence { get; }
        public float LeftDisplacement { get; }
        public bool RightHasActionConstraint { get; }
        public AnimationFootConstraintMode RightConstraintMode { get; }
        public AnimationFootSupportPhase RightSupportPhase { get; }
        public AnimationBodyRotationPivotMode RightBodyPivotMode { get; }
        public bool RightCandidate { get; }
        public ulong RightPlanSequence { get; }
        public float RightDisplacement { get; }
    }

    public readonly struct CharacterFootGroundingDiagnostics
    {
        internal CharacterFootGroundingDiagnostics(
            ulong frameSequence,
            ulong completionIdentity,
            ulong resetSequence,
            float presentationDeltaSeconds,
            float poseRootVerticalDelta,
            bool bodyGrounded,
            float placementAlpha,
            CharacterFootPlacementPelvisPlan pelvisPlan,
            CharacterFootPlacementPelvisSupportDiagnostics pelvisSupport,
            in CharacterLyraCurrentGroundingResult lyra,
            CharacterFullBodyIkGoal pelvisGoal,
            CharacterFootPlacementRuntimeSettings settings,
            CharacterFootPlacementPoseRig rig,
            int physicsSceneIdentity,
            int selfFilterIdentity,
            CharacterFootGroundingFootDiagnostics left,
            CharacterFootGroundingFootDiagnostics right)
        {
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            ResetSequence = resetSequence;
            PresentationDeltaSeconds = presentationDeltaSeconds;
            PoseRootVerticalDelta = poseRootVerticalDelta;
            NodeExecuted = true;
            BodyGrounded = bodyGrounded;
            PlacementAlpha = placementAlpha;
            PelvisPlan = pelvisPlan;
            PelvisSupport = pelvisSupport;
            TargetPelvisOffset = lyra.TargetPelvisOffset;
            CurrentPelvisOffset = lyra.CurrentPelvisOffset;
            PelvisSpringVelocity = lyra.PelvisVelocity;
            PreviousPelvisTarget = lyra.PreviousPelvisTarget;
            PelvisSpringInitialized = lyra.PelvisSpringInitialized;
            PelvisPreSolveTranslation = pelvisGoal.ComponentPosition;
            PelvisGoal = pelvisGoal;
            LyraSourceIdentity = new FixedString128Bytes(CharacterLyraCurrentGroundingSolver.SourceIdentity);
            SpringIdentity = new FixedString128Bytes(CharacterLyraCurrentGroundingSolver.SpringIdentity);
            RigId = new FixedString64Bytes(rig.Rig.RigId);
            RigRevision = new FixedString64Bytes(rig.Rig.RigRevision);
            ProfileId = new FixedString128Bytes(settings.ProfileId);
            ProfileRevision = new FixedString128Bytes(settings.ProfileRevision);
            PosePlanHash = new FixedString128Bytes(settings.PosePlanHash);
            CalibrationId = new FixedString128Bytes(rig.CalibrationId.Value);
            CalibrationRevision = new FixedString128Bytes(rig.CalibrationRevision);
            PhysicsSceneIdentity = physicsSceneIdentity;
            SelfFilterIdentity = selfFilterIdentity;
            PoseRootWorldPosition = rig.PoseRoot.position;
            PoseRootWorldRotation = rig.PoseRoot.rotation;
            PoseRootWorldScale = rig.PoseRoot.lossyScale;
            Left = left;
            Right = right;
        }

        public ulong FrameSequence { get; }
        public ulong CompletionIdentity { get; }
        public ulong ResetSequence { get; }
        public float PresentationDeltaSeconds { get; }
        public float PoseRootVerticalDelta { get; }
        public bool NodeExecuted { get; }
        public bool BodyGrounded { get; }
        public float PlacementAlpha { get; }
        public CharacterFootPlacementPelvisPlan PelvisPlan { get; }
        public CharacterFootPlacementPelvisSupportDiagnostics PelvisSupport { get; }
        public float TargetPelvisOffset { get; }
        public float CurrentPelvisOffset { get; }
        public float PelvisSpringVelocity { get; }
        public float PreviousPelvisTarget { get; }
        public bool PelvisSpringInitialized { get; }
        public Vector3 PelvisPreSolveTranslation { get; }
        public CharacterFullBodyIkGoal PelvisGoal { get; }
        public FixedString128Bytes LyraSourceIdentity { get; }
        public FixedString128Bytes SpringIdentity { get; }
        public FixedString64Bytes RigId { get; }
        public FixedString64Bytes RigRevision { get; }
        public FixedString128Bytes ProfileId { get; }
        public FixedString128Bytes ProfileRevision { get; }
        public FixedString128Bytes PosePlanHash { get; }
        public FixedString128Bytes CalibrationId { get; }
        public FixedString128Bytes CalibrationRevision { get; }
        public int PhysicsSceneIdentity { get; }
        public int SelfFilterIdentity { get; }
        public Vector3 PoseRootWorldPosition { get; }
        public Quaternion PoseRootWorldRotation { get; }
        public Vector3 PoseRootWorldScale { get; }
        public CharacterFootGroundingFootDiagnostics Left { get; }
        public CharacterFootGroundingFootDiagnostics Right { get; }
        public bool IsCompleted => FrameSequence != 0 && CompletionIdentity != 0;
    }

    public enum CharacterPredictiveFootPlanState : byte
    {
        Inactive = 0,
        Planned = 1,
        Executing = 2,
        Rejected = 3,
        Completed = 4
    }

    public enum CharacterPredictiveFootPlanTransitionReason : byte
    {
        None = 0,
        PlanGenerated = 1,
        PlanRejected = 2,
        PlanExecutionStarted = 3,
        PlanEnded = 4
    }

    public enum CharacterPredictiveFootPlanEndReason : byte
    {
        None = 0,
        EventReplaced = 1,
        PresentationReset = 2,
        ActionCompleted = 3,
        ActionClockInvalid = 4,
        LandingTransactionUnavailable = 5,
        MotionDeviationExceeded = 6,
        TargetReachExceeded = 7,
        TargetEvaluationInvalid = 8
    }

    public readonly struct CharacterPredictiveFootPathSampleDiagnostics
    {
        internal CharacterPredictiveFootPathSampleDiagnostics(
            float fraction,
            Vector3 position,
            Vector3 normal,
            int surfaceInstanceId,
            Vector3 animationRootPosition,
            Vector3 hipPosition)
        {
            Fraction = fraction;
            Position = position;
            Normal = normal;
            SurfaceInstanceId = surfaceInstanceId;
            AnimationRootPosition = animationRootPosition;
            HipPosition = hipPosition;
        }

        public float Fraction { get; }
        public Vector3 Position { get; }
        public Vector3 Normal { get; }
        public int SurfaceInstanceId { get; }
        public Vector3 AnimationRootPosition { get; }
        public Vector3 HipPosition { get; }
    }

    public struct CharacterPredictiveFootQueryRejectCounts
    {
        public int NoCandidate { get; private set; }
        public int HeightDiscontinuity { get; private set; }
        public int EdgeGap { get; private set; }
        public int SurfaceDiscontinuity { get; private set; }
        public int ReachExceeded { get; private set; }
        public int SlopeExceeded { get; private set; }
        public int StepExceeded { get; private set; }
        public int InvalidCandidate { get; private set; }
        public int UnsupportedCenter { get; private set; }
        public int Total => NoCandidate + HeightDiscontinuity + EdgeGap +
                            SurfaceDiscontinuity + ReachExceeded + SlopeExceeded +
                            StepExceeded + InvalidCandidate + UnsupportedCenter;

        internal void Add(FootPlacementGroundEnvelopeRejectReason reason)
        {
            switch (reason)
            {
                case FootPlacementGroundEnvelopeRejectReason.NoCandidate: NoCandidate++; break;
                case FootPlacementGroundEnvelopeRejectReason.HeightDiscontinuity: HeightDiscontinuity++; break;
                case FootPlacementGroundEnvelopeRejectReason.EdgeGap: EdgeGap++; break;
                case FootPlacementGroundEnvelopeRejectReason.SurfaceDiscontinuity: SurfaceDiscontinuity++; break;
                case FootPlacementGroundEnvelopeRejectReason.ReachExceeded: ReachExceeded++; break;
                case FootPlacementGroundEnvelopeRejectReason.SlopeExceeded: SlopeExceeded++; break;
                case FootPlacementGroundEnvelopeRejectReason.StepExceeded: StepExceeded++; break;
                case FootPlacementGroundEnvelopeRejectReason.InvalidCandidate: InvalidCandidate++; break;
                case FootPlacementGroundEnvelopeRejectReason.UnsupportedCenter: UnsupportedCenter++; break;
            }
        }
    }

    public readonly struct CharacterPredictiveFootQueryDiagnostics
    {
        internal CharacterPredictiveFootQueryDiagnostics(
            CharacterPredictiveFootPlanExecution plan)
        {
            FutureLandingQuery = plan.FutureLandingRequest.MaximumDistance > 0f
                ? new CharacterFootGroundingQueryDiagnostics(plan.FutureLandingRequest)
                : default;
            QueryCount = plan.QueryCount;
            RawHitCount = plan.RawHitCount;
            AcceptedHitCount = plan.AcceptedHitCount;
            EdgePlaneCandidateCount = plan.EdgePlaneCandidateCount;
            AcceptedEdgePlaneCount = plan.AcceptedEdgePlaneCount;
            RejectedHitCount = plan.RejectedQueryCount;
            RejectCounts = plan.QueryRejectCounts;
            RouteSampleCount = plan.RouteSampleCount;
            GroundEnvelopeSegmentCount = plan.GroundEnvelopeSegmentCount;
            GroundEnvelopeRejectReason = plan.GroundEnvelopeRejectReason;
        }

        public CharacterFootGroundingQueryDiagnostics FutureLandingQuery { get; }
        public int QueryCount { get; }
        public int RawHitCount { get; }
        public int AcceptedHitCount { get; }
        public int EdgePlaneCandidateCount { get; }
        public int AcceptedEdgePlaneCount { get; }
        public int RejectedHitCount { get; }
        public CharacterPredictiveFootQueryRejectCounts RejectCounts { get; }
        public int RouteSampleCount { get; }
        public int GroundEnvelopeSegmentCount { get; }
        public FootPlacementGroundEnvelopeRejectReason GroundEnvelopeRejectReason { get; }
    }

    public readonly struct CharacterPredictiveFootEventDiagnostics
    {
        internal CharacterPredictiveFootEventDiagnostics(
            CharacterFootSide side,
            in AnimationFootFeatureSample feature)
            : this(side, feature.IsValid, feature.PredictedStep)
        {
        }

        internal CharacterPredictiveFootEventDiagnostics(
            CharacterFootSide side,
            bool footFeatureValid,
            AnimationPredictedFootStepSample step)
        {
            FootFeatureValid = footFeatureValid;
            PredictedStepValid = step.IsValid;
            HasLandingEvent = step.HasLandingEvent;
            IsSourceBound = step.IsSourceBound;
            IsAuthoritative = step.IsAuthoritative;
            ExpectedLandingEventIdentity = step.ResolveExpectedLandingEventIdentity(side);
            LandingEventIdentityValid = step.HasConsistentLandingEventIdentity(side);
            IsPreSwing = step.IsPreSwing;
            IsSwing = step.IsSwing;
            LandingEventIdentity = step.LandingEventIdentity;
            SourceSampleIdentity = step.SourceSampleIdentity;
            SourceSampleCycle = step.SourceSampleCycle;
            EventOrdinal = step.EventOrdinal;
            ContributionContinuityIdentity = step.ContributionContinuityIdentity;
            Confidence = step.Confidence;
            TimeToLandingSeconds = step.TimeToLandingSeconds;
            EventPhase = step.EventPhase;
            LiftOffPhase = step.LiftOffPhase;
            RootLocalFootRoute = step.RootLocalFootRoute;
            AuthoredFootRouteStart = step.AuthoredFootPlanarRoute.Length > 0
                ? step.AuthoredFootPlanarRoute[0]
                : Vector3.zero;
            AuthoredFootRouteLanding = step.AuthoredFootPlanarRoute.Length > 0
                ? step.AuthoredFootPlanarRoute[step.AuthoredFootPlanarRoute.Length - 1]
                : Vector3.zero;
        }

        public bool FootFeatureValid { get; }
        public bool PredictedStepValid { get; }
        public bool HasLandingEvent { get; }
        public bool IsSourceBound { get; }
        public bool IsAuthoritative { get; }
        public ulong ExpectedLandingEventIdentity { get; }
        public bool LandingEventIdentityValid { get; }
        public bool IsPreSwing { get; }
        public bool IsSwing { get; }
        public ulong LandingEventIdentity { get; }
        public ulong SourceSampleIdentity { get; }
        public int SourceSampleCycle { get; }
        public int EventOrdinal { get; }
        public ulong ContributionContinuityIdentity { get; }
        public float Confidence { get; }
        public float TimeToLandingSeconds { get; }
        public float EventPhase { get; }
        public float LiftOffPhase { get; }
        public FixedList512Bytes<Vector3> RootLocalFootRoute { get; }
        public Vector3 AuthoredFootRouteStart { get; }
        public Vector3 AuthoredFootRouteLanding { get; }
    }

    public readonly struct CharacterPredictiveFootPlanLifecycleDiagnostics
    {
        internal CharacterPredictiveFootPlanLifecycleDiagnostics(
            CharacterPredictiveFootPlanExecution plan)
        {
            Sequence = plan.Sequence;
            GeneratedFrame = plan.GeneratedFrame;
            GenerationPhase = plan.EventPhaseAtGeneration;
            State = plan.State;
            TransitionReason = plan.TransitionReason;
            EndReason = plan.EndReason;
            LandingEventIdentity = plan.LandingEventIdentity;
            SourceSampleIdentity = plan.SourceSampleIdentity;
            SourceSampleCycle = plan.SourceSampleCycle;
            EventOrdinal = plan.EventOrdinal;
            ContributionContinuityIdentity = plan.ContributionContinuityIdentity;
            ElapsedSeconds = plan.ActionStepPhase * plan.ActionStepDurationSeconds;
            SecondsToLiftOff = plan.LiftOffPhase * plan.ActionStepDurationSeconds;
            SwingDuration = (1f - plan.LiftOffPhase) * plan.ActionStepDurationSeconds;
            ExecutionProgress = plan.ActionProgress;
            HasPathGeometry = plan.HasPathGeometry;
            HasExecutablePath = plan.HasExecutablePath && plan.HasPathGeometry;
            FrozenPlanarVelocity = plan.RootTrajectory.FrozenPlanarVelocity;
            FrozenTrajectoryCurvatureDegreesPerSecond =
                plan.RootTrajectory.FrozenTrajectoryCurvatureDegreesPerSecond;
            FrozenTrajectoryCurvatureAvailable =
                plan.RootTrajectory.FrozenTrajectoryCurvatureAvailable;
            FrozenYawVelocityDegreesPerSecond = plan.RootTrajectory.FrozenYawVelocityDegreesPerSecond;
            FrozenMaximumYawVelocityDegreesPerSecond =
                plan.RootTrajectory.FrozenMaximumYawVelocityDegreesPerSecond;
            MotionLinearLandingError = plan.MotionLinearLandingError;
            MotionAngularLandingError = plan.MotionAngularLandingError;
            MotionLandingError = plan.MotionLandingError;
            MotionLandingTolerance = plan.MotionLandingTolerance;
        }

        public ulong Sequence { get; }
        public ulong GeneratedFrame { get; }
        public float GenerationPhase { get; }
        public CharacterPredictiveFootPlanState State { get; }
        public CharacterPredictiveFootPlanTransitionReason TransitionReason { get; }
        public CharacterPredictiveFootPlanEndReason EndReason { get; }
        public ulong LandingEventIdentity { get; }
        public ulong SourceSampleIdentity { get; }
        public int SourceSampleCycle { get; }
        public int EventOrdinal { get; }
        public ulong ContributionContinuityIdentity { get; }
        public float ElapsedSeconds { get; }
        public float SecondsToLiftOff { get; }
        public float SwingDuration { get; }
        public float ExecutionProgress { get; }
        public bool HasPathGeometry { get; }
        public bool HasExecutablePath { get; }
        public Vector3 FrozenPlanarVelocity { get; }
        public float FrozenTrajectoryCurvatureDegreesPerSecond { get; }
        public bool FrozenTrajectoryCurvatureAvailable { get; }
        public float FrozenYawVelocityDegreesPerSecond { get; }
        public float FrozenMaximumYawVelocityDegreesPerSecond { get; }
        public float MotionLinearLandingError { get; }
        public float MotionAngularLandingError { get; }
        public float MotionLandingError { get; }
        public float MotionLandingTolerance { get; }
    }

    public readonly struct CharacterPredictiveFootPlacementFootDiagnostics
    {
        internal CharacterPredictiveFootPlacementFootDiagnostics(
            CharacterFootSide side,
            bool rewritten,
            FootPredictionRejectReason rejectReason,
            CharacterFootGroundingHitDiagnostics futureSupport,
            in CharacterPredictiveFootQueryDiagnostics query,
            in CharacterPredictiveFootEventDiagnostics currentEvent,
            in CharacterPredictiveFootEventDiagnostics incomingEvent,
            float currentEventFootPoseWeight,
            float trajectoryCurvatureDegreesPerSecond,
            bool trajectoryCurvatureAvailable,
            float planPredictionBlend,
            float authoritativePredictionBlend,
            bool hasPlanRevision,
            ulong revisionPlanSequence,
            float planRevisionBlendWeight,
            CharacterFootPlanTransitionKind planTransitionKind,
            bool planFadingOut,
            float planRetentionWeight,
            float intentLandingDisplacementError,
            float intentLandingDisplacementThreshold,
            float predictionHorizon,
            float predictionDistance,
            in CharacterPredictiveFootPlanLifecycleDiagnostics plan,
            Vector3 currentSoleWorldPosition,
            Vector3 fixedPathStartWorldPosition,
            Vector3 fixedLandingWorldPosition,
            Vector3 currentPathWorldPosition,
            Vector3 currentPathRootWorldPosition,
            Vector3 currentPathHipWorldPosition,
            Vector3 predictedHipWorldPosition,
            Vector3 frozenRootStartWorldPosition,
            Quaternion frozenRootStartWorldRotation,
            Vector3 frozenRootLandingWorldPosition,
            Quaternion frozenRootLandingWorldRotation,
            Vector3 predictionUp,
            float minimumLandingConfidence,
            float maximumPredictionReachRatio,
            float predictionReachRatio,
            float castAbove,
            float castBelow,
            float pathSphereRadius,
            float swingCapsuleRadius,
            float soleSupportRadius,
            CharacterFootGroundingHitDiagnostics currentPathSupport,
            float preClearanceHeelPathDistance,
            float preClearanceToePathDistance,
            float postClearanceHeelPathDistance,
            float postClearanceToePathDistance,
            bool clearanceEvaluated,
            bool predictiveOwnsSoleClearance,
            float predictiveResidualPenetration,
            float authoredAnimationClearance,
            float animationClearanceContinuityOffset,
            float animationClearanceContinuityContribution,
            float reachClearance,
            float compositeAnimationClearance,
            float requiredLift,
            float appliedLift,
            in FixedList128Bytes<Vector3> plannedFootRouteWorld,
            in FixedList512Bytes<CharacterPredictiveFootPathSampleDiagnostics> pathSamples,
            Vector3 baselineGoalWorldPosition,
            Vector3 finalGoalWorldPosition,
            CharacterFullBodyIkGoal baselineGoal,
            CharacterFullBodyIkGoal finalGoal)
        {
            Side = side;
            Rewritten = rewritten;
            RejectReason = rejectReason;
            FutureSupport = futureSupport;
            Query = query;
            CurrentEvent = currentEvent;
            IncomingEvent = incomingEvent;
            CurrentEventFootPoseWeight = currentEventFootPoseWeight;
            TrajectoryCurvatureDegreesPerSecond = trajectoryCurvatureDegreesPerSecond;
            TrajectoryCurvatureAvailable = trajectoryCurvatureAvailable;
            PlanPredictionBlend = planPredictionBlend;
            AuthoritativePredictionBlend = authoritativePredictionBlend;
            HasPlanRevision = hasPlanRevision;
            RevisionPlanSequence = revisionPlanSequence;
            PlanRevisionBlendWeight = planRevisionBlendWeight;
            PlanTransitionKind = planTransitionKind;
            PlanFadingOut = planFadingOut;
            PlanRetentionWeight = planRetentionWeight;
            IntentLandingDisplacementError = intentLandingDisplacementError;
            IntentLandingDisplacementThreshold = intentLandingDisplacementThreshold;
            PredictionHorizon = predictionHorizon;
            PredictionDistance = predictionDistance;
            Plan = plan;
            CurrentSoleWorldPosition = currentSoleWorldPosition;
            FixedPathStartWorldPosition = fixedPathStartWorldPosition;
            FixedLandingWorldPosition = fixedLandingWorldPosition;
            CurrentPathWorldPosition = currentPathWorldPosition;
            CurrentPathRootWorldPosition = currentPathRootWorldPosition;
            CurrentPathHipWorldPosition = currentPathHipWorldPosition;
            PredictedHipWorldPosition = predictedHipWorldPosition;
            FrozenRootStartWorldPosition = frozenRootStartWorldPosition;
            FrozenRootStartWorldRotation = frozenRootStartWorldRotation;
            FrozenRootLandingWorldPosition = frozenRootLandingWorldPosition;
            FrozenRootLandingWorldRotation = frozenRootLandingWorldRotation;
            PredictionUp = predictionUp;
            MinimumLandingConfidence = minimumLandingConfidence;
            MaximumPredictionReachRatio = maximumPredictionReachRatio;
            PredictionReachRatio = predictionReachRatio;
            CastAbove = castAbove;
            CastBelow = castBelow;
            PathSphereRadius = pathSphereRadius;
            SwingCapsuleRadius = swingCapsuleRadius;
            SoleSupportRadius = soleSupportRadius;
            CurrentPathSupport = currentPathSupport;
            PreClearanceHeelPathDistance = preClearanceHeelPathDistance;
            PreClearanceToePathDistance = preClearanceToePathDistance;
            PostClearanceHeelPathDistance = postClearanceHeelPathDistance;
            PostClearanceToePathDistance = postClearanceToePathDistance;
            ClearanceEvaluated = clearanceEvaluated;
            PredictiveOwnsSoleClearance = predictiveOwnsSoleClearance;
            PredictiveResidualPenetration = predictiveResidualPenetration;
            AuthoredAnimationClearance = authoredAnimationClearance;
            AnimationClearanceContinuityOffset = animationClearanceContinuityOffset;
            AnimationClearanceContinuityContribution = animationClearanceContinuityContribution;
            ReachClearance = reachClearance;
            CompositeAnimationClearance = compositeAnimationClearance;
            RequiredLift = requiredLift;
            AppliedLift = appliedLift;
            PlannedFootRouteWorld = plannedFootRouteWorld;
            PathSamples = pathSamples;
            BaselineGoalWorldPosition = baselineGoalWorldPosition;
            FinalGoalWorldPosition = finalGoalWorldPosition;
            BaselineGoal = baselineGoal;
            FinalGoal = finalGoal;
        }

        public CharacterFootSide Side { get; }
        public bool Rewritten { get; }
        public FootPredictionRejectReason RejectReason { get; }
        public CharacterFootGroundingHitDiagnostics FutureSupport { get; }
        public CharacterPredictiveFootQueryDiagnostics Query { get; }
        public CharacterFootGroundingQueryDiagnostics FutureLandingQuery => Query.FutureLandingQuery;
        public int GroundEnvelopeSegmentCount => Query.GroundEnvelopeSegmentCount;
        public FootPlacementGroundEnvelopeRejectReason GroundEnvelopeRejectReason => Query.GroundEnvelopeRejectReason;
        public int QueryCount => Query.QueryCount;
        public int RawHitCount => Query.RawHitCount;
        public int RejectedQueryCount => Query.RejectedHitCount;
        public CharacterPredictiveFootQueryRejectCounts QueryRejectCounts => Query.RejectCounts;
        public CharacterPredictiveFootEventDiagnostics CurrentEvent { get; }
        public CharacterPredictiveFootEventDiagnostics IncomingEvent { get; }
        public float CurrentEventFootPoseWeight { get; }
        public float TrajectoryCurvatureDegreesPerSecond { get; }
        public bool TrajectoryCurvatureAvailable { get; }
        public float PlanPredictionBlend { get; }
        public float AuthoritativePredictionBlend { get; }
        public bool HasPlanRevision { get; }
        public ulong RevisionPlanSequence { get; }
        public float PlanRevisionBlendWeight { get; }
        public CharacterFootPlanTransitionKind PlanTransitionKind { get; }
        public bool PlanFadingOut { get; }
        public float PlanRetentionWeight { get; }
        public float IntentLandingDisplacementError { get; }
        public float IntentLandingDisplacementThreshold { get; }
        public bool HasAuthoritativeLandingEvent => CurrentEvent.IsAuthoritative;
        public ulong LandingEventIdentity => CurrentEvent.LandingEventIdentity;
        public ulong SourceSampleIdentity => CurrentEvent.SourceSampleIdentity;
        public int SourceSampleCycle => CurrentEvent.SourceSampleCycle;
        public int EventOrdinal => CurrentEvent.EventOrdinal;
        public ulong ContributionContinuityIdentity => CurrentEvent.ContributionContinuityIdentity;
        public float LandingConfidence => CurrentEvent.Confidence;
        public float AuthoredLandingDelaySeconds => CurrentEvent.TimeToLandingSeconds;
        public float EventPhase => CurrentEvent.EventPhase;
        public float LiftOffPhase => CurrentEvent.LiftOffPhase;
        public FixedList512Bytes<Vector3> RootLocalFootRoute => CurrentEvent.RootLocalFootRoute;
        public Vector3 RootLocalLanding => RootLocalFootRoute.Length > 0
            ? RootLocalFootRoute[RootLocalFootRoute.Length - 1]
            : Vector3.zero;
        public float PredictionHorizon { get; }
        public float PredictionDistance { get; }
        public CharacterPredictiveFootPlanLifecycleDiagnostics Plan { get; }
        public ulong PlanSequence => Plan.Sequence;
        public ulong PlanGeneratedFrame => Plan.GeneratedFrame;
        public float PlanGenerationPhase => Plan.GenerationPhase;
        public CharacterPredictiveFootPlanState PlanState => Plan.State;
        public CharacterPredictiveFootPlanTransitionReason PlanTransitionReason => Plan.TransitionReason;
        public CharacterPredictiveFootPlanEndReason PlanEndReason => Plan.EndReason;
        public float PlanExecutionProgress => Plan.ExecutionProgress;
        public Vector3 FrozenPlanarVelocity => Plan.FrozenPlanarVelocity;
        public float FrozenTrajectoryCurvatureDegreesPerSecond =>
            Plan.FrozenTrajectoryCurvatureDegreesPerSecond;
        public bool FrozenTrajectoryCurvatureAvailable =>
            Plan.FrozenTrajectoryCurvatureAvailable;
        public float FrozenYawVelocityDegreesPerSecond => Plan.FrozenYawVelocityDegreesPerSecond;
        public float FrozenMaximumYawVelocityDegreesPerSecond =>
            Plan.FrozenMaximumYawVelocityDegreesPerSecond;
        public float MotionLinearLandingError => Plan.MotionLinearLandingError;
        public float MotionAngularLandingError => Plan.MotionAngularLandingError;
        public float MotionLandingError => Plan.MotionLandingError;
        public float MotionLandingTolerance => Plan.MotionLandingTolerance;
        public Vector3 CurrentSoleWorldPosition { get; }
        public Vector3 FixedPathStartWorldPosition { get; }
        public Vector3 FixedLandingWorldPosition { get; }
        public Vector3 CurrentPathWorldPosition { get; }
        public Vector3 CurrentPathRootWorldPosition { get; }
        public Vector3 CurrentPathHipWorldPosition { get; }
        public Vector3 PredictedHipWorldPosition { get; }
        public Vector3 FrozenRootStartWorldPosition { get; }
        public Quaternion FrozenRootStartWorldRotation { get; }
        public Vector3 FrozenRootLandingWorldPosition { get; }
        public Quaternion FrozenRootLandingWorldRotation { get; }
        public Vector3 PredictionUp { get; }
        public float MinimumLandingConfidence { get; }
        public float MaximumPredictionReachRatio { get; }
        public float PredictionReachRatio { get; }
        public float CastAbove { get; }
        public float CastBelow { get; }
        public int RouteSampleCount => Query.RouteSampleCount;
        public int AcceptedHitCount => Query.AcceptedHitCount;
        public int EdgePlaneCandidateCount => Query.EdgePlaneCandidateCount;
        public int AcceptedEdgePlaneCount => Query.AcceptedEdgePlaneCount;
        public float PathSphereRadius { get; }
        public float SwingCapsuleRadius { get; }
        public float SoleSupportRadius { get; }
        public CharacterFootGroundingHitDiagnostics CurrentPathSupport { get; }
        public float PreClearanceHeelPathDistance { get; }
        public float PreClearanceToePathDistance { get; }
        public float PostClearanceHeelPathDistance { get; }
        public float PostClearanceToePathDistance { get; }
        public bool ClearanceEvaluated { get; }
        public bool PredictiveOwnsSoleClearance { get; }
        public float PredictiveResidualPenetration { get; }
        public float AuthoredAnimationClearance { get; }
        public float AnimationClearanceContinuityOffset { get; }
        public float AnimationClearanceContinuityContribution { get; }
        public float ReachClearance { get; }
        public float CompositeAnimationClearance { get; }
        public float RequiredLift { get; }
        public float AppliedLift { get; }
        public FixedList128Bytes<Vector3> PlannedFootRouteWorld { get; }
        public FixedList512Bytes<CharacterPredictiveFootPathSampleDiagnostics> PathSamples { get; }
        public Vector3 BaselineGoalWorldPosition { get; }
        public Vector3 FinalGoalWorldPosition { get; }
        public CharacterFullBodyIkGoal BaselineGoal { get; }
        public CharacterFullBodyIkGoal FinalGoal { get; }
    }

    public readonly struct CharacterPredictiveFootPlacementDiagnostics
    {
        internal CharacterPredictiveFootPlacementDiagnostics(
            ulong frameSequence,
            ulong completionIdentity,
            in CharacterFullBodyIkGoalSetHeader baselineHeader,
            in CharacterPredictiveFootPlacementFootDiagnostics left,
            in CharacterPredictiveFootPlacementFootDiagnostics right)
        {
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            BaselineProducerOperationIndex = baselineHeader.ProducerOperationIndex;
            BaselineProducerCallSiteIndex = baselineHeader.ProducerCallSiteIndex;
            BaselineGoalOffset = baselineHeader.GoalOffset;
            BaselineGoalCount = baselineHeader.GoalCount;
            BaselineRigId = baselineHeader.RigId;
            BaselineRigRevision = baselineHeader.RigRevision;
            Left = left;
            Right = right;
        }

        public ulong FrameSequence { get; }
        public ulong CompletionIdentity { get; }
        public int BaselineProducerOperationIndex { get; }
        public int BaselineProducerCallSiteIndex { get; }
        public int BaselineGoalOffset { get; }
        public int BaselineGoalCount { get; }
        public FixedString64Bytes BaselineRigId { get; }
        public FixedString64Bytes BaselineRigRevision { get; }
        public CharacterPredictiveFootPlacementFootDiagnostics Left { get; }
        public CharacterPredictiveFootPlacementFootDiagnostics Right { get; }
        public bool IsCompleted => FrameSequence != 0 && CompletionIdentity != 0;
    }
}
