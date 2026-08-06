using RootMotion.FinalIK;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public readonly struct CharacterGroundingQueryDiagnostics
    {
        internal CharacterGroundingQueryDiagnostics(in GroundingQueryRequest request)
        {
            Shape = request.Shape;
            Purpose = request.Purpose;
            FootIndex = request.FootIndex;
            Origin = request.Origin;
            CapsuleEnd = request.CapsuleEnd;
            Direction = request.Direction;
            Radius = request.Radius;
            MaximumDistance = request.MaxDistance;
            IsAvailable = true;
        }

        public GroundingQueryShape Shape { get; }
        public GroundingQueryPurpose Purpose { get; }
        public int FootIndex { get; }
        public Vector3 Origin { get; }
        public Vector3 CapsuleEnd { get; }
        public Vector3 Direction { get; }
        public float Radius { get; }
        public float MaximumDistance { get; }
        public bool IsAvailable { get; }
    }

    public readonly struct CharacterGroundingHitDiagnostics
    {
        internal CharacterGroundingHitDiagnostics(GroundingQueryHit hit)
        {
            HasHit = hit.HasHit;
            SurfaceIdentity = hit.SurfaceIdentity;
            Point = hit.Point;
            Normal = hit.Normal;
            Distance = hit.Distance;
        }

        internal CharacterGroundingHitDiagnostics(FootPlacementSurface surface)
        {
            HasHit = surface.IsValid;
            SurfaceIdentity = surface.Identity;
            Point = surface.Point;
            Normal = surface.Normal;
            Distance = 0f;
        }

        public bool HasHit { get; }
        public int SurfaceIdentity { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public float Distance { get; }
    }

    public readonly struct CharacterGroundEnvelopeDiagnostics
    {
        internal CharacterGroundEnvelopeDiagnostics(in FootPlacementGroundEnvelope envelope)
        {
            SegmentCount = envelope.Count;
            RejectReason = envelope.RejectReason;
            if (envelope.Count == 0)
            {
                Start = Vector3.zero;
                End = Vector3.zero;
                MaximumMinimumSoleHeight = 0f;
                return;
            }
            FootPlacementGroundEnvelopeSegment first = envelope.GetSegment(0);
            FootPlacementGroundEnvelopeSegment last = envelope.GetSegment(envelope.Count - 1);
            Start = first.EdgeStart;
            End = last.EdgeEnd;
            float maximum = first.MinimumSoleHeight;
            for (int i = 1; i < envelope.Count; i++)
                maximum = Mathf.Max(maximum, envelope.GetSegment(i).MinimumSoleHeight);
            MaximumMinimumSoleHeight = maximum;
        }

        public int SegmentCount { get; }
        public FootPlacementGroundEnvelopeRejectReason RejectReason { get; }
        public Vector3 Start { get; }
        public Vector3 End { get; }
        public float MaximumMinimumSoleHeight { get; }
    }

    public readonly struct CharacterPredictiveFootDiagnostics
    {
        internal CharacterPredictiveFootDiagnostics(
            CharacterFootSide side,
            CharacterGroundingQueryDiagnostics heelRequest,
            CharacterGroundingQueryDiagnostics toeRequest,
            CharacterGroundingQueryDiagnostics footCenterRequest,
            in CharacterFinalIkGroundingFootResult grounding,
            AnimationFootFeatureSample feature,
            in FootPlacementSurface currentSupport,
            in FootPlacementSurface futureSupport,
            in FootPlacementGroundEnvelope envelope,
            FootConstraintState constraintState,
            FootConstraintTransitionReason transitionReason,
            Vector3 surfaceLocalAnchor,
            Vector3 surfaceLocalPlantAnchor,
            Quaternion surfaceLocalRotation,
            CharacterFootPlantLockType lockType,
            bool adjustHeelBeforePlanting,
            FootPredictionRejectReason predictionRejectReason,
            float predictionHorizon,
            bool predictionHorizonClamped,
            Vector3 stockVelocityPrediction,
            Vector3 animationSoleVelocity,
            float legExtensionRatio,
            float ankleTwistDegrees,
            float separationCorrection,
            float placementWeight,
            bool plantContact,
            float animationFootSpeed,
            float surfaceDistance,
            float plantSupportWeight,
            float contactWeight,
            float swingClearance,
            int queryCount,
            int rejectedQueryCount,
            CharacterFullBodyIkGoal goal)
        {
            Side = side;
            HeelRequest = heelRequest;
            ToeRequest = toeRequest;
            FootCenterRequest = footCenterRequest;
            HeelHit = new CharacterGroundingHitDiagnostics(grounding.HeelHit);
            ToeHit = new CharacterGroundingHitDiagnostics(grounding.ToeHit);
            FootCenterHit = new CharacterGroundingHitDiagnostics(grounding.FootCenterHit);
            CurrentGroundingHit = new CharacterGroundingHitDiagnostics(grounding.CurrentGroundingHit);
            GroundingComponentPosition = grounding.ComponentPosition;
            GroundingComponentRotation = grounding.ComponentRotation;
            GroundingVerticalOffset = grounding.VerticalOffset;
            Grounded = grounding.Grounded;
            GroundingVelocity = grounding.Velocity;
            StockVelocityPrediction = stockVelocityPrediction;
            FootFeature = feature;
            CurrentSupport = new CharacterGroundingHitDiagnostics(currentSupport);
            FutureSupport = new CharacterGroundingHitDiagnostics(futureSupport);
            GroundEnvelope = new CharacterGroundEnvelopeDiagnostics(in envelope);
            ConstraintState = constraintState;
            TransitionReason = transitionReason;
            SurfaceLocalAnchor = surfaceLocalAnchor;
            SurfaceLocalPlantAnchor = surfaceLocalPlantAnchor;
            SurfaceLocalRotation = surfaceLocalRotation;
            LockType = lockType;
            AdjustHeelBeforePlanting = adjustHeelBeforePlanting;
            PredictionRejectReason = predictionRejectReason;
            PredictionHorizon = predictionHorizon;
            PredictionHorizonClamped = predictionHorizonClamped;
            AnimationSoleVelocity = animationSoleVelocity;
            LegExtensionRatio = legExtensionRatio;
            AnkleTwistDegrees = ankleTwistDegrees;
            SeparationCorrection = separationCorrection;
            PlacementWeight = placementWeight;
            PlantContact = plantContact;
            AnimationFootSpeed = animationFootSpeed;
            SurfaceDistance = surfaceDistance;
            PlantSupportWeight = plantSupportWeight;
            ContactWeight = contactWeight;
            SwingClearance = swingClearance;
            QueryCount = queryCount;
            RejectedQueryCount = rejectedQueryCount;
            Goal = goal;
        }

        public CharacterFootSide Side { get; }
        public CharacterGroundingQueryDiagnostics HeelRequest { get; }
        public CharacterGroundingQueryDiagnostics ToeRequest { get; }
        public CharacterGroundingQueryDiagnostics FootCenterRequest { get; }
        public CharacterGroundingHitDiagnostics HeelHit { get; }
        public CharacterGroundingHitDiagnostics ToeHit { get; }
        public CharacterGroundingHitDiagnostics FootCenterHit { get; }
        public CharacterGroundingHitDiagnostics CurrentGroundingHit { get; }
        public Vector3 GroundingComponentPosition { get; }
        public Quaternion GroundingComponentRotation { get; }
        public float GroundingVerticalOffset { get; }
        public bool Grounded { get; }
        public Vector3 GroundingVelocity { get; }
        public Vector3 StockVelocityPrediction { get; }
        public AnimationFootFeatureSample FootFeature { get; }
        public CharacterGroundingHitDiagnostics CurrentSupport { get; }
        public CharacterGroundingHitDiagnostics FutureSupport { get; }
        public CharacterGroundEnvelopeDiagnostics GroundEnvelope { get; }
        public FootConstraintState ConstraintState { get; }
        public FootConstraintTransitionReason TransitionReason { get; }
        public Vector3 SurfaceLocalAnchor { get; }
        public Vector3 SurfaceLocalPlantAnchor { get; }
        public Quaternion SurfaceLocalRotation { get; }
        public CharacterFootPlantLockType LockType { get; }
        public bool AdjustHeelBeforePlanting { get; }
        public FootPredictionRejectReason PredictionRejectReason { get; }
        public float PredictionHorizon { get; }
        public bool PredictionHorizonClamped { get; }
        public Vector3 AnimationSoleVelocity { get; }
        public float LegExtensionRatio { get; }
        public float AnkleTwistDegrees { get; }
        public float SeparationCorrection { get; }
        public float PlacementWeight { get; }
        public bool PlantContact { get; }
        public float AnimationFootSpeed { get; }
        public float SurfaceDistance { get; }
        public float PlantSupportWeight { get; }
        public float ContactWeight { get; }
        public float SwingClearance { get; }
        public int QueryCount { get; }
        public int RejectedQueryCount { get; }
        public CharacterFullBodyIkGoal Goal { get; }
    }

    public readonly struct CharacterPredictiveFootPlacementDiagnostics
    {
        internal CharacterPredictiveFootPlacementDiagnostics(
            ulong frameSequence,
            ulong completionIdentity,
            ulong resetSequence,
            string backendIdentity,
            CharacterFootPlacementPelvisPlan pelvisPlan,
            CharacterGroundingHitDiagnostics rootHit,
            bool targetGrounded,
            bool groundedBefore,
            bool groundedAfter,
            bool grounded,
            CharacterPredictiveFootDiagnostics left,
            CharacterPredictiveFootDiagnostics right)
        {
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            ResetSequence = resetSequence;
            BackendIdentity = backendIdentity ?? string.Empty;
            PelvisPlan = pelvisPlan;
            PelvisPreSolveTranslation = pelvisPlan.ComponentTranslation;
            RootHit = rootHit;
            TargetGrounded = targetGrounded;
            GroundedBefore = groundedBefore;
            GroundedAfter = groundedAfter;
            Grounded = grounded;
            Left = left;
            Right = right;
        }

        public ulong FrameSequence { get; }
        public ulong CompletionIdentity { get; }
        public ulong ResetSequence { get; }
        public string BackendIdentity { get; }
        public CharacterFootPlacementPelvisPlan PelvisPlan { get; }
        public Vector3 PelvisPreSolveTranslation { get; }
        public CharacterGroundingHitDiagnostics RootHit { get; }
        public bool TargetGrounded { get; }
        public bool GroundedBefore { get; }
        public bool GroundedAfter { get; }
        public bool Grounded { get; }
        public CharacterPredictiveFootDiagnostics Left { get; }
        public CharacterPredictiveFootDiagnostics Right { get; }
        public bool IsCompleted => FrameSequence != 0 && CompletionIdentity != 0;
    }
}
