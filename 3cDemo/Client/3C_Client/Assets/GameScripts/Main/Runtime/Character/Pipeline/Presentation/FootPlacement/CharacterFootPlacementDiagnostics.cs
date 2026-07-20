using System;
using System.Collections.Generic;
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
        public Vector3 TargetPosition { get; }
        public Quaternion TargetRotation { get; }
    }

    public readonly struct CharacterFootPlacementFrameSnapshot
    {
        readonly AnimationPoseContribution[] m_Contributions;

        internal CharacterFootPlacementFrameSnapshot(
            ActorId actorId,
            ulong renderFrame,
            ulong previousBodyTick,
            ulong currentBodyTick,
            ulong resetSequence,
            string poseSourceLayerId,
            AnimationPoseContribution[] contributions,
            int contributionCount,
            FootPlacementFootFrameSnapshot left,
            FootPlacementFootFrameSnapshot right,
            float pelvisTargetOffset,
            float pelvisCurrentOffset,
            FootPlacementSupportFoot supportFoot,
            CharacterFootPlacementSolverResult solverResult)
        {
            ActorId = actorId;
            RenderFrame = renderFrame;
            PreviousBodyTick = previousBodyTick;
            CurrentBodyTick = currentBodyTick;
            ResetSequence = resetSequence;
            PoseSourceLayerId = poseSourceLayerId ?? string.Empty;
            m_Contributions = contributions;
            ContributionCount = contributionCount;
            Left = left;
            Right = right;
            PelvisTargetOffset = pelvisTargetOffset;
            PelvisCurrentOffset = pelvisCurrentOffset;
            SupportFoot = supportFoot;
            SolverResult = solverResult;
        }

        public ActorId ActorId { get; }
        public ulong RenderFrame { get; }
        public ulong PreviousBodyTick { get; }
        public ulong CurrentBodyTick { get; }
        public ulong ResetSequence { get; }
        public string PoseSourceLayerId { get; }
        public int ContributionCount { get; }
        public FootPlacementFootFrameSnapshot Left { get; }
        public FootPlacementFootFrameSnapshot Right { get; }
        public float PelvisTargetOffset { get; }
        public float PelvisCurrentOffset { get; }
        public FootPlacementSupportFoot SupportFoot { get; }
        public CharacterFootPlacementSolverResult SolverResult { get; }
        public bool IsValid => ActorId.IsValid && RenderFrame != 0;

        public AnimationPoseContribution GetContribution(int index)
        {
            if (index < 0 || index >= ContributionCount || m_Contributions == null)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Contributions[index];
        }

        internal static void CopyContributions(
            IReadOnlyList<AnimationPoseContribution> source,
            AnimationPoseContribution[] destination)
        {
            int count = Math.Min(source.Count, destination.Length);
            for (int i = 0; i < count; i++)
                destination[i] = source[i];
            for (int i = count; i < destination.Length; i++)
                destination[i] = default;
        }
    }
}
