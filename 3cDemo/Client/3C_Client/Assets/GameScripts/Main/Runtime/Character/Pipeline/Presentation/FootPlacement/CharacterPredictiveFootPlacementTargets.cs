using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct CharacterPredictiveFootStanceInput
    {
        internal CharacterPredictiveFootStanceInput(
            bool hasActionConstraint,
            bool hasExecutablePlan,
            bool isExecuting,
            ulong planSequence,
            ulong landingEventIdentity,
            bool hasContactTarget,
            ulong contactPlanSequence,
            ulong contactLandingEventIdentity,
            AnimationFootConstraintMode constraintMode,
            AnimationFootSupportPhase supportPhase,
            AnimationBodyRotationPivotMode bodyPivotMode,
            float constraintWeight,
            float supportWeight,
            float plantConfidence,
            float actionProgress,
            float remainingSeconds,
            FootPlacementSurface contactSurface,
            Vector3 contactAnklePosition,
            Quaternion contactAnkleRotation,
            Vector3 pathPosition,
            Vector3 pathRoot,
            Vector3 pathRootStart,
            Vector3 pathHip,
            ulong bodySupportSequence,
            Vector3 currentHip,
            Vector3 targetAnklePosition,
            float predictiveOutputWeight,
            float supportLegLength,
            float supportLegCompressionReserve,
            Vector3 supportKneeBendPlane,
            Vector3 supportFootPivotPosition,
            float supportFootPivotWeight)
        {
            HasActionConstraint = hasActionConstraint;
            HasExecutablePlan = hasExecutablePlan;
            IsExecuting = isExecuting;
            PlanSequence = planSequence;
            LandingEventIdentity = landingEventIdentity;
            HasContactTarget = hasContactTarget;
            ContactPlanSequence = contactPlanSequence;
            ContactLandingEventIdentity = contactLandingEventIdentity;
            ConstraintMode = constraintMode;
            SupportPhase = supportPhase;
            BodyPivotMode = bodyPivotMode;
            ConstraintWeight = Mathf.Clamp01(constraintWeight);
            SupportWeight = Mathf.Clamp01(supportWeight);
            PlantConfidence = plantConfidence;
            ActionProgress = actionProgress;
            RemainingSeconds = remainingSeconds;
            ContactSurface = contactSurface;
            ContactAnklePosition = contactAnklePosition;
            ContactAnkleRotation = contactAnkleRotation;
            PathPosition = pathPosition;
            PathRoot = pathRoot;
            PathRootStart = pathRootStart;
            PathHip = pathHip;
            BodySupportSequence = bodySupportSequence;
            CurrentHip = currentHip;
            TargetAnklePosition = targetAnklePosition;
            PredictiveOutputWeight = Mathf.Clamp01(predictiveOutputWeight);
            SupportLegLength = supportLegLength;
            SupportLegCompressionReserve = supportLegCompressionReserve;
            SupportKneeBendPlane = supportKneeBendPlane;
            SupportFootPivotPosition = supportFootPivotPosition;
            SupportFootPivotWeight = Mathf.Clamp01(supportFootPivotWeight);
        }

        internal bool HasActionConstraint { get; }
        internal bool HasExecutablePlan { get; }
        internal bool IsExecuting { get; }
        internal ulong PlanSequence { get; }
        internal ulong LandingEventIdentity { get; }
        internal bool HasContactTarget { get; }
        internal ulong ContactPlanSequence { get; }
        internal ulong ContactLandingEventIdentity { get; }
        internal AnimationFootConstraintMode ConstraintMode { get; }
        internal AnimationFootSupportPhase SupportPhase { get; }
        internal AnimationBodyRotationPivotMode BodyPivotMode { get; }
        internal float ConstraintWeight { get; }
        internal float SupportWeight { get; }
        internal float PlantConfidence { get; }
        internal float ActionProgress { get; }
        internal float RemainingSeconds { get; }
        internal FootPlacementSurface ContactSurface { get; }
        internal Vector3 ContactAnklePosition { get; }
        internal Quaternion ContactAnkleRotation { get; }
        internal Vector3 PathPosition { get; }
        internal Vector3 PathRoot { get; }
        internal Vector3 PathRootStart { get; }
        internal Vector3 PathHip { get; }
        internal ulong BodySupportSequence { get; }
        internal Vector3 CurrentHip { get; }
        internal Vector3 TargetAnklePosition { get; }
        internal float PredictiveOutputWeight { get; }
        internal float SupportLegLength { get; }
        internal float SupportLegCompressionReserve { get; }
        internal Vector3 SupportKneeBendPlane { get; }
        internal Vector3 SupportFootPivotPosition { get; }
        internal float SupportFootPivotWeight { get; }
    }

    internal readonly struct CharacterPredictiveFootTarget
    {
        internal CharacterPredictiveFootTarget(
            Vector3 pathPosition,
            Vector3 pathRoot,
            Vector3 pathHip,
            FootPlacementSurface support,
            Vector3 anklePosition,
            Quaternion ankleRotation,
            CharacterFootPlacementSoleContactPose contacts,
            float heelPlaneDistance,
            float toePlaneDistance,
            float authoredAnimationClearance,
            float animationClearanceContinuityOffset,
            float animationClearanceContinuityContribution,
            float reachClearance,
            float compositeAnimationClearance)
        {
            PathPosition = pathPosition;
            PathRoot = pathRoot;
            PathHip = pathHip;
            Support = support;
            AnklePosition = anklePosition;
            AnkleRotation = ankleRotation;
            Contacts = contacts;
            HeelPlaneDistance = heelPlaneDistance;
            ToePlaneDistance = toePlaneDistance;
            AuthoredAnimationClearance = authoredAnimationClearance;
            AnimationClearanceContinuityOffset = animationClearanceContinuityOffset;
            AnimationClearanceContinuityContribution = animationClearanceContinuityContribution;
            ReachClearance = reachClearance;
            CompositeAnimationClearance = compositeAnimationClearance;
        }

        internal Vector3 PathPosition { get; }
        internal Vector3 PathRoot { get; }
        internal Vector3 PathHip { get; }
        internal FootPlacementSurface Support { get; }
        internal Vector3 AnklePosition { get; }
        internal Quaternion AnkleRotation { get; }
        internal CharacterFootPlacementSoleContactPose Contacts { get; }
        internal float HeelPlaneDistance { get; }
        internal float ToePlaneDistance { get; }
        internal float AuthoredAnimationClearance { get; }
        internal float AnimationClearanceContinuityOffset { get; }
        internal float AnimationClearanceContinuityContribution { get; }
        internal float ReachClearance { get; }
        internal float CompositeAnimationClearance { get; }
    }
}
