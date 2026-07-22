using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public readonly struct FootPlacementFootFrameSnapshot
    {
        public FootPlacementFootFrameSnapshot(
            CharacterFootSide side,
            FootConstraintState constraintState,
            FootConstraintTransitionReason transitionReason,
            Vector3 relativeAnkleVelocity,
            Vector3 relativeToeVelocity,
            bool descending,
            float surfaceDistance,
            Vector3 predictedFootprint,
            float predictionHorizon,
            bool predictionHorizonClamped,
            FootPredictionRejectReason predictionRejectReason,
            int queryCount,
            int candidateCount,
            int rejectedCandidateCount,
            int surfaceIdentity,
            float lockError,
            float replantError,
            float policyWeight,
            float solverWeight,
            Vector3 generatedSoleLocalVelocity,
            Vector3 generatedSoleWorldVelocity,
            float generatedSoleHeight,
            float generatedPlantConfidence,
            float generatedLandingConfidence,
            float generatedLandingDelay,
            Vector2 generatedLandingOffset,
            int heelSupportIdentity,
            int toeSupportIdentity,
            int currentSupportIdentity,
            int futureSupportIdentity,
            int groundEnvelopeSegmentCount,
            FootPlacementGroundEnvelopeRejectReason groundEnvelopeRejectReason,
            float ankleTwistDegrees,
            float heelLiftDistance,
            float separationCorrection,
            Vector3 targetPosition,
            Quaternion targetRotation)
        {
            Side = side;
            ConstraintState = constraintState;
            TransitionReason = transitionReason;
            RelativeAnkleVelocity = relativeAnkleVelocity;
            RelativeToeVelocity = relativeToeVelocity;
            Descending = descending;
            SurfaceDistance = surfaceDistance;
            PredictedFootprint = predictedFootprint;
            PredictionHorizon = predictionHorizon;
            PredictionHorizonClamped = predictionHorizonClamped;
            PredictionRejectReason = predictionRejectReason;
            QueryCount = queryCount;
            CandidateCount = candidateCount;
            RejectedCandidateCount = rejectedCandidateCount;
            SurfaceIdentity = surfaceIdentity;
            LockError = lockError;
            ReplantError = replantError;
            PolicyWeight = policyWeight;
            SolverWeight = solverWeight;
            GeneratedSoleLocalVelocity = generatedSoleLocalVelocity;
            GeneratedSoleWorldVelocity = generatedSoleWorldVelocity;
            GeneratedSoleHeight = generatedSoleHeight;
            GeneratedPlantConfidence = generatedPlantConfidence;
            GeneratedLandingConfidence = generatedLandingConfidence;
            GeneratedLandingDelay = generatedLandingDelay;
            GeneratedLandingOffset = generatedLandingOffset;
            HeelSupportIdentity = heelSupportIdentity;
            ToeSupportIdentity = toeSupportIdentity;
            CurrentSupportIdentity = currentSupportIdentity;
            FutureSupportIdentity = futureSupportIdentity;
            GroundEnvelopeSegmentCount = groundEnvelopeSegmentCount;
            GroundEnvelopeRejectReason = groundEnvelopeRejectReason;
            AnkleTwistDegrees = ankleTwistDegrees;
            HeelLiftDistance = heelLiftDistance;
            SeparationCorrection = separationCorrection;
            TargetPosition = targetPosition;
            TargetRotation = targetRotation;
        }

        public CharacterFootSide Side { get; }
        public FootConstraintState ConstraintState { get; }
        public FootConstraintTransitionReason TransitionReason { get; }
        public Vector3 RelativeAnkleVelocity { get; }
        public Vector3 RelativeToeVelocity { get; }
        public bool Descending { get; }
        public float SurfaceDistance { get; }
        public Vector3 PredictedFootprint { get; }
        public float PredictionHorizon { get; }
        public bool PredictionHorizonClamped { get; }
        public FootPredictionRejectReason PredictionRejectReason { get; }
        public int QueryCount { get; }
        public int CandidateCount { get; }
        public int RejectedCandidateCount { get; }
        public int SurfaceIdentity { get; }
        public float LockError { get; }
        public float ReplantError { get; }
        public float PolicyWeight { get; }
        public float SolverWeight { get; }
        public Vector3 GeneratedSoleLocalVelocity { get; }
        public Vector3 GeneratedSoleWorldVelocity { get; }
        public float GeneratedSoleHeight { get; }
        public float GeneratedPlantConfidence { get; }
        public float GeneratedLandingConfidence { get; }
        public float GeneratedLandingDelay { get; }
        public Vector2 GeneratedLandingOffset { get; }
        public int HeelSupportIdentity { get; }
        public int ToeSupportIdentity { get; }
        public int CurrentSupportIdentity { get; }
        public int FutureSupportIdentity { get; }
        public int GroundEnvelopeSegmentCount { get; }
        public FootPlacementGroundEnvelopeRejectReason GroundEnvelopeRejectReason { get; }
        public float AnkleTwistDegrees { get; }
        public float HeelLiftDistance { get; }
        public float SeparationCorrection { get; }
        public Vector3 TargetPosition { get; }
        public Quaternion TargetRotation { get; }
    }

    public readonly struct CharacterFootPlacementFrameSnapshot
    {
        readonly AnimationPoseSourceContribution[] m_Contributions;

        internal CharacterFootPlacementFrameSnapshot(
            ActorId actorId,
            ulong renderFrame,
            ulong previousBodyTick,
            ulong currentBodyTick,
            ulong resetSequence,
            string poseProgramHash,
            ulong completionIdentity,
            ulong poseContinuityIdentity,
            string footPlacementWeightParameterId,
            int footPlacementWeightParameterIndex,
            float footPlacementWeight,
            string calibrationId,
            string calibrationRevision,
            string analysisSourceId,
            int analysisVersion,
            string analysisAlgorithmVersion,
            AnimationPoseSourceContribution[] contributions,
            int contributionCount,
            FootPlacementFootFrameSnapshot left,
            FootPlacementFootFrameSnapshot right,
            FootPlacementActorMovementCompensationMode actorMovementCompensationMode,
            Vector3 bodySourceTranslationDelta,
            Vector3 bodyVisibleTranslationDelta,
            bool bodyGroundedBefore,
            bool bodyGroundedAfter,
            float pelvisReachTargetOffset,
            float pelvisReachCurrentOffset,
            float actorMovementCompensationTargetOffset,
            float actorMovementCompensationCurrentOffset,
            float actorMovementCompensationVelocity,
            float pelvisTargetOffset,
            float pelvisCurrentOffset,
            FootPlacementPelvisHeightMode pelvisHeightMode,
            FootPlacementPelvisHeightDecision pelvisHeightDecision,
            FootPlacementPelvisHeightReason pelvisHeightReason,
            float pelvisDirectionalSpeed,
            float pelvisFootLeadDistance,
            float pelvisSlopeHeightDifference,
            FootPlacementSupportFoot supportFoot,
            CharacterFootPlacementSolverResult solverResult)
        {
            ActorId = actorId;
            RenderFrame = renderFrame;
            PreviousBodyTick = previousBodyTick;
            CurrentBodyTick = currentBodyTick;
            ResetSequence = resetSequence;
            PoseProgramHash = poseProgramHash ?? string.Empty;
            CompletionIdentity = completionIdentity;
            PoseContinuityIdentity = poseContinuityIdentity;
            FootPlacementWeightParameterId = footPlacementWeightParameterId ?? string.Empty;
            FootPlacementWeightParameterIndex = footPlacementWeightParameterIndex;
            FootPlacementWeight = footPlacementWeight;
            CalibrationId = calibrationId ?? string.Empty;
            CalibrationRevision = calibrationRevision ?? string.Empty;
            AnalysisSourceId = analysisSourceId ?? string.Empty;
            AnalysisVersion = analysisVersion;
            AnalysisAlgorithmVersion = analysisAlgorithmVersion ?? string.Empty;
            m_Contributions = contributions;
            ContributionCount = contributionCount;
            Left = left;
            Right = right;
            ActorMovementCompensationMode = actorMovementCompensationMode;
            BodySourceTranslationDelta = bodySourceTranslationDelta;
            BodyVisibleTranslationDelta = bodyVisibleTranslationDelta;
            BodyGroundedBefore = bodyGroundedBefore;
            BodyGroundedAfter = bodyGroundedAfter;
            PelvisReachTargetOffset = pelvisReachTargetOffset;
            PelvisReachCurrentOffset = pelvisReachCurrentOffset;
            ActorMovementCompensationTargetOffset = actorMovementCompensationTargetOffset;
            ActorMovementCompensationCurrentOffset = actorMovementCompensationCurrentOffset;
            ActorMovementCompensationVelocity = actorMovementCompensationVelocity;
            PelvisTargetOffset = pelvisTargetOffset;
            PelvisCurrentOffset = pelvisCurrentOffset;
            PelvisHeightMode = pelvisHeightMode;
            PelvisHeightDecision = pelvisHeightDecision;
            PelvisHeightReason = pelvisHeightReason;
            PelvisDirectionalSpeed = pelvisDirectionalSpeed;
            PelvisFootLeadDistance = pelvisFootLeadDistance;
            PelvisSlopeHeightDifference = pelvisSlopeHeightDifference;
            SupportFoot = supportFoot;
            SolverResult = solverResult;
        }

        public ActorId ActorId { get; }
        public ulong RenderFrame { get; }
        public ulong PreviousBodyTick { get; }
        public ulong CurrentBodyTick { get; }
        public ulong ResetSequence { get; }
        public string PoseProgramHash { get; }
        public ulong CompletionIdentity { get; }
        public ulong PoseContinuityIdentity { get; }
        public string FootPlacementWeightParameterId { get; }
        public int FootPlacementWeightParameterIndex { get; }
        public float FootPlacementWeight { get; }
        public string CalibrationId { get; }
        public string CalibrationRevision { get; }
        public string AnalysisSourceId { get; }
        public int AnalysisVersion { get; }
        public string AnalysisAlgorithmVersion { get; }
        public int ContributionCount { get; }
        public FootPlacementFootFrameSnapshot Left { get; }
        public FootPlacementFootFrameSnapshot Right { get; }
        public FootPlacementActorMovementCompensationMode ActorMovementCompensationMode { get; }
        public Vector3 BodySourceTranslationDelta { get; }
        public Vector3 BodyVisibleTranslationDelta { get; }
        public bool BodyGroundedBefore { get; }
        public bool BodyGroundedAfter { get; }
        public float PelvisReachTargetOffset { get; }
        public float PelvisReachCurrentOffset { get; }
        public float ActorMovementCompensationTargetOffset { get; }
        public float ActorMovementCompensationCurrentOffset { get; }
        public float ActorMovementCompensationVelocity { get; }
        public float PelvisTargetOffset { get; }
        public float PelvisCurrentOffset { get; }
        public FootPlacementPelvisHeightMode PelvisHeightMode { get; }
        public FootPlacementPelvisHeightDecision PelvisHeightDecision { get; }
        public FootPlacementPelvisHeightReason PelvisHeightReason { get; }
        public float PelvisDirectionalSpeed { get; }
        public float PelvisFootLeadDistance { get; }
        public float PelvisSlopeHeightDifference { get; }
        public FootPlacementSupportFoot SupportFoot { get; }
        public CharacterFootPlacementSolverResult SolverResult { get; }
        public bool IsValid => RenderFrame != 0 && ActorId.IsValid;

        public AnimationPoseSourceContribution GetContribution(int index)
        {
            if (index < 0 || index >= ContributionCount || m_Contributions == null)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Contributions[index];
        }

        internal static void CopyContributions(
            AnimationReadOnlyBuffer<AnimationPoseSourceContribution> source,
            AnimationPoseSourceContribution[] destination)
        {
            int count = Math.Min(source.Count, destination.Length);
            for (int i = 0; i < count; i++)
                destination[i] = source[i];
            for (int i = count; i < destination.Length; i++)
                destination[i] = default;
        }
    }
}
