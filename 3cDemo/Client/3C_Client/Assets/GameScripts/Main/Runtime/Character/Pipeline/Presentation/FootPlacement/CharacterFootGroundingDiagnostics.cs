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
            bool hasPreviousSoleSample,
            int previousSoleSurfaceIdentity,
            float previousSoleHeelPlaneDistance,
            float previousSoleToePlaneDistance,
            bool continuousSoleContact,
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
            UnconstrainedOffset = lyra.UnconstrainedOffset;
            SoleConstraintOffset = lyra.SoleConstraintOffset;
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
            HasPreviousSoleSample = hasPreviousSoleSample;
            PreviousSoleSurfaceIdentity = previousSoleSurfaceIdentity;
            PreviousSoleHeelPlaneDistance = previousSoleHeelPlaneDistance;
            PreviousSoleToePlaneDistance = previousSoleToePlaneDistance;
            ContinuousSoleContact = continuousSoleContact;
            Goal = goal;
        }

        public CharacterFootSide Side { get; }
        public CharacterFootGroundingQueryDiagnostics Query { get; }
        public CharacterFootGroundingHitDiagnostics CurrentHit { get; }
        public bool DidTraceHit { get; }
        public float TargetOffset { get; }
        public float SoleClearanceTarget { get; }
        public float OffsetTarget { get; }
        public float UnconstrainedOffset { get; }
        public float SoleConstraintOffset { get; }
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
        public bool HasPreviousSoleSample { get; }
        public int PreviousSoleSurfaceIdentity { get; }
        public float PreviousSoleHeelPlaneDistance { get; }
        public float PreviousSoleToePlaneDistance { get; }
        public bool ContinuousSoleContact { get; }
        public CharacterFullBodyIkGoal Goal { get; }
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
        public CharacterFootGroundingFootDiagnostics Left { get; }
        public CharacterFootGroundingFootDiagnostics Right { get; }
        public bool IsCompleted => FrameSequence != 0 && CompletionIdentity != 0;
    }

    public readonly struct CharacterPredictiveFootPlacementModifierFootDiagnostics
    {
        internal CharacterPredictiveFootPlacementModifierFootDiagnostics(
            CharacterFootSide side,
            bool swingEligible,
            bool selectedForRewrite,
            bool rewritten,
            FootPredictionRejectReason rejectReason,
            CharacterFootGroundingHitDiagnostics futureSupport,
            int groundEnvelopeSegmentCount,
            FootPlacementGroundEnvelopeRejectReason groundEnvelopeRejectReason,
            int queryCount,
            int rejectedQueryCount,
            float predictionHorizon,
            float swingClearance,
            CharacterFullBodyIkGoal baselineGoal,
            CharacterFullBodyIkGoal finalGoal)
        {
            Side = side;
            SwingEligible = swingEligible;
            SelectedForRewrite = selectedForRewrite;
            Rewritten = rewritten;
            RejectReason = rejectReason;
            FutureSupport = futureSupport;
            GroundEnvelopeSegmentCount = groundEnvelopeSegmentCount;
            GroundEnvelopeRejectReason = groundEnvelopeRejectReason;
            QueryCount = queryCount;
            RejectedQueryCount = rejectedQueryCount;
            PredictionHorizon = predictionHorizon;
            SwingClearance = swingClearance;
            BaselineGoal = baselineGoal;
            FinalGoal = finalGoal;
        }

        public CharacterFootSide Side { get; }
        public bool SwingEligible { get; }
        public bool SelectedForRewrite { get; }
        public bool Rewritten { get; }
        public FootPredictionRejectReason RejectReason { get; }
        public CharacterFootGroundingHitDiagnostics FutureSupport { get; }
        public int GroundEnvelopeSegmentCount { get; }
        public FootPlacementGroundEnvelopeRejectReason GroundEnvelopeRejectReason { get; }
        public int QueryCount { get; }
        public int RejectedQueryCount { get; }
        public float PredictionHorizon { get; }
        public float SwingClearance { get; }
        public CharacterFullBodyIkGoal BaselineGoal { get; }
        public CharacterFullBodyIkGoal FinalGoal { get; }
    }

    public readonly struct CharacterPredictiveFootPlacementModifierDiagnostics
    {
        internal CharacterPredictiveFootPlacementModifierDiagnostics(
            ulong frameSequence,
            ulong completionIdentity,
            CharacterFootSide selectedSide,
            in CharacterFullBodyIkGoalSetHeader baselineHeader,
            CharacterPredictiveFootPlacementModifierFootDiagnostics left,
            CharacterPredictiveFootPlacementModifierFootDiagnostics right)
        {
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            SelectedSide = selectedSide;
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
        public CharacterFootSide SelectedSide { get; }
        public int BaselineProducerOperationIndex { get; }
        public int BaselineProducerCallSiteIndex { get; }
        public int BaselineGoalOffset { get; }
        public int BaselineGoalCount { get; }
        public FixedString64Bytes BaselineRigId { get; }
        public FixedString64Bytes BaselineRigRevision { get; }
        public CharacterPredictiveFootPlacementModifierFootDiagnostics Left { get; }
        public CharacterPredictiveFootPlacementModifierFootDiagnostics Right { get; }
        public bool IsCompleted => FrameSequence != 0 && CompletionIdentity != 0;
    }
}
