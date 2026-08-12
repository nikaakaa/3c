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
            bool hasContactTarget,
            AnimationFootConstraintMode constraintMode,
            AnimationFootSupportPhase supportPhase,
            AnimationBodyRotationPivotMode bodyPivotMode,
            float plantConfidence,
            float progress,
            float remainingSeconds,
            FootPlacementSurface contactSurface,
            Vector3 contactAnklePosition,
            Quaternion contactAnkleRotation,
            Vector3 pathPosition,
            Vector3 pathRoot,
            Vector3 pathRootStart,
            Vector3 pathHip)
        {
            HasActionConstraint = hasActionConstraint;
            HasExecutablePlan = hasExecutablePlan;
            IsExecuting = isExecuting;
            HasContactTarget = hasContactTarget;
            ConstraintMode = constraintMode;
            SupportPhase = supportPhase;
            BodyPivotMode = bodyPivotMode;
            PlantConfidence = plantConfidence;
            Progress = progress;
            RemainingSeconds = remainingSeconds;
            ContactSurface = contactSurface;
            ContactAnklePosition = contactAnklePosition;
            ContactAnkleRotation = contactAnkleRotation;
            PathPosition = pathPosition;
            PathRoot = pathRoot;
            PathRootStart = pathRootStart;
            PathHip = pathHip;
        }

        internal bool HasActionConstraint { get; }
        internal bool HasExecutablePlan { get; }
        internal bool IsExecuting { get; }
        internal bool HasContactTarget { get; }
        internal AnimationFootConstraintMode ConstraintMode { get; }
        internal AnimationFootSupportPhase SupportPhase { get; }
        internal AnimationBodyRotationPivotMode BodyPivotMode { get; }
        internal float PlantConfidence { get; }
        internal float Progress { get; }
        internal float RemainingSeconds { get; }
        internal FootPlacementSurface ContactSurface { get; }
        internal Vector3 ContactAnklePosition { get; }
        internal Quaternion ContactAnkleRotation { get; }
        internal Vector3 PathPosition { get; }
        internal Vector3 PathRoot { get; }
        internal Vector3 PathRootStart { get; }
        internal Vector3 PathHip { get; }
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
            float toePlaneDistance)
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
    }
}
