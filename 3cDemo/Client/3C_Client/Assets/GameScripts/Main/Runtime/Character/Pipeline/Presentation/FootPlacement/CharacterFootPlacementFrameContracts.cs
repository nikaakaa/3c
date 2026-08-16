using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct CharacterFootPlacementFrameInput
    {
        internal CharacterFootPlacementFrameInput(
            ActorId actorId,
            ulong renderFrame,
            float presentationDeltaSeconds,
            CharacterBodyPresentationFrame body,
            in CharacterPresentationFactFrame facts,
            in CharacterFootPlacementPoseInput upstreamPose)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Foot Placement Actor identity is invalid.", nameof(actorId));
            if (renderFrame == 0)
                throw new ArgumentOutOfRangeException(nameof(renderFrame));
            if (!float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(presentationDeltaSeconds));
            if (upstreamPose.CompletionIdentity == 0)
                throw new ArgumentException("Foot Placement upstream completion identity is invalid.", nameof(upstreamPose));
            if (!facts.IsValid)
                throw new ArgumentException("Foot Placement Presentation Facts are invalid.", nameof(facts));
            ActorId = actorId;
            RenderFrame = renderFrame;
            PresentationDeltaSeconds = presentationDeltaSeconds;
            Body = body;
            LocomotionMotionTimeline = facts.LocomotionMotionTimeline;
            MovementPlaybackTime = facts.MovementPlaybackTime;
            TrajectoryCurvatureDegreesPerSecond = facts.TrajectoryCurvatureDegreesPerSecond;
            TrajectoryCurvatureAvailable = facts.TrajectoryCurvatureAvailable;
            MotionPhase = facts.MotionPhase;
            UpstreamPose = upstreamPose;
        }

        internal ActorId ActorId { get; }
        internal ulong RenderFrame { get; }
        internal ulong CompletionIdentity => UpstreamPose.CompletionIdentity;
        internal float PresentationDeltaSeconds { get; }
        internal CharacterBodyPresentationFrame Body { get; }
        internal CommittedLocomotionPlanarMotionTimeline LocomotionMotionTimeline { get; }
        internal double MovementPlaybackTime { get; }
        internal float TrajectoryCurvatureDegreesPerSecond { get; }
        internal bool TrajectoryCurvatureAvailable { get; }
        internal CharacterPresentationMotionPhase MotionPhase { get; }
        internal CharacterFootPlacementPoseInput UpstreamPose { get; }
    }

    internal readonly struct CharacterFootPlacementFrameResult
    {
        internal CharacterFootPlacementFrameResult(
            in CharacterFullBodyIkGoalSetHeader goalSet,
            CharacterFullBodyIkGoal pelvis,
            CharacterFullBodyIkGoal leftFoot,
            CharacterFullBodyIkGoal rightFoot,
            in CharacterFootGroundingDiagnostics groundingDiagnostics,
            in CharacterPredictiveFootPlacementDiagnostics predictionDiagnostics)
        {
            if (goalSet.Availability != CharacterFullBodyIkGoalSetAvailability.Ready ||
                goalSet.GoalCount != 3 ||
                !pelvis.IsValid || !leftFoot.IsValid || !rightFoot.IsValid)
            {
                throw new ArgumentException("Foot Placement frame result is invalid.");
            }
            GoalSet = goalSet;
            Pelvis = pelvis;
            LeftFoot = leftFoot;
            RightFoot = rightFoot;
            GroundingDiagnostics = groundingDiagnostics;
            PredictionDiagnostics = predictionDiagnostics;
        }

        internal CharacterFullBodyIkGoalSetHeader GoalSet { get; }
        internal CharacterFullBodyIkGoal Pelvis { get; }
        internal CharacterFullBodyIkGoal LeftFoot { get; }
        internal CharacterFullBodyIkGoal RightFoot { get; }
        internal CharacterFootGroundingDiagnostics GroundingDiagnostics { get; }
        internal CharacterPredictiveFootPlacementDiagnostics PredictionDiagnostics { get; }

        internal void WriteGoals(NativeSlice<CharacterFullBodyIkGoal> output)
        {
            if (output.Length != 3)
                throw new ArgumentException("Foot Placement requires exactly three Goal slots.", nameof(output));
            output[0] = Pelvis;
            output[1] = LeftFoot;
            output[2] = RightFoot;
        }
    }

    internal readonly struct CharacterFootPlacementFootGoalResolution
    {
        internal CharacterFootPlacementFootGoalResolution(
            CharacterFullBodyIkGoal left,
            CharacterFullBodyIkGoal right)
        {
            if (!left.IsValid || !right.IsValid ||
                left.Slot != CharacterFullBodyIkEffectorSlot.LeftFoot ||
                right.Slot != CharacterFullBodyIkEffectorSlot.RightFoot)
            {
                throw new ArgumentException("Foot Placement goal resolution is invalid.");
            }
            Left = left;
            Right = right;
        }

        internal CharacterFullBodyIkGoal Left { get; }
        internal CharacterFullBodyIkGoal Right { get; }
    }

    internal readonly struct CharacterFootLandingCommit
    {
        internal CharacterFootLandingCommit(
            ulong planSequence,
            ulong landingEventIdentity,
            Vector3 landingSolePosition,
            Quaternion landingSoleRotation,
            FootPlacementSurface support,
            Vector3 anchorLocalPosition,
            Quaternion anchorLocalRotation,
            Vector3 committedSolePosition,
            Vector3 successorOrigin)
        {
            PlanSequence = planSequence;
            LandingEventIdentity = landingEventIdentity;
            LandingSolePosition = landingSolePosition;
            LandingSoleRotation = landingSoleRotation;
            Support = support;
            AnchorLocalPosition = anchorLocalPosition;
            AnchorLocalRotation = anchorLocalRotation;
            CommittedSolePosition = committedSolePosition;
            SuccessorOrigin = successorOrigin;
            if (!IsValid)
                throw new ArgumentException("Foot Landing Commit is invalid.");
        }

        internal ulong PlanSequence { get; }
        internal ulong LandingEventIdentity { get; }
        internal Vector3 LandingSolePosition { get; }
        internal Quaternion LandingSoleRotation { get; }
        internal FootPlacementSurface Support { get; }
        internal Vector3 AnchorLocalPosition { get; }
        internal Quaternion AnchorLocalRotation { get; }
        internal Vector3 CommittedSolePosition { get; }
        internal Vector3 SuccessorOrigin { get; }

        internal bool IsValid =>
            PlanSequence != 0 && LandingEventIdentity != 0 &&
            IsFinite(LandingSolePosition) && IsUnit(LandingSoleRotation) &&
            Support.IsValid && IsFinite(AnchorLocalPosition) && IsUnit(AnchorLocalRotation) &&
            IsFinite(CommittedSolePosition) && IsFinite(SuccessorOrigin);

        internal bool TryResolve(
            ulong landingEventIdentity,
            out Vector3 sole,
            out FootPlacementSurface support)
        {
            bool available = IsValid && LandingEventIdentity == landingEventIdentity;
            sole = available ? CommittedSolePosition : default;
            support = available ? Support.Rebuild() : default;
            return available && support.IsValid;
        }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static bool IsUnit(Quaternion value)
        {
            float magnitude = value.x * value.x + value.y * value.y +
                              value.z * value.z + value.w * value.w;
            return float.IsFinite(magnitude) && Mathf.Abs(magnitude - 1f) <= 0.01f;
        }
    }

    internal readonly struct CharacterPredictiveFootFrameEvaluation
    {
        internal CharacterPredictiveFootFrameEvaluation(
            ulong renderFrame,
            ulong completionIdentity,
            in CharacterPredictiveFootStanceInput left,
            in CharacterPredictiveFootStanceInput right)
        {
            if (renderFrame == 0 || completionIdentity == 0)
                throw new ArgumentException("Predictive Foot frame identity is invalid.");
            RenderFrame = renderFrame;
            CompletionIdentity = completionIdentity;
            Left = left;
            Right = right;
        }

        internal ulong RenderFrame { get; }
        internal ulong CompletionIdentity { get; }
        internal CharacterPredictiveFootStanceInput Left { get; }
        internal CharacterPredictiveFootStanceInput Right { get; }

        internal bool Matches(in CharacterFootPlacementFrameInput frame) =>
            RenderFrame == frame.RenderFrame && CompletionIdentity == frame.CompletionIdentity;
    }
}
