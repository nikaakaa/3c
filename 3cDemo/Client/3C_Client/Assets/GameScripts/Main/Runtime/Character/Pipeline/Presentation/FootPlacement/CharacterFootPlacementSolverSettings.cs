using System;
using RootMotion.FinalIK;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    [Serializable]
    public sealed class CharacterFinalIkGroundingAuthoringSettings
    {
        [SerializeField] Grounding.Quality m_Quality = Grounding.Quality.Best;
        [SerializeField] LayerMask m_GroundLayerMask;
        [SerializeField] float m_MaximumStep = 0.45f;
        [SerializeField] float m_HeightOffset;
        [SerializeField] float m_FootHeightSpeed = 2.5f;
        [SerializeField] float m_FootRadius = 0.08f;
        [SerializeField] float m_VelocityPrediction = 0.05f;
        [SerializeField, Range(0f, 1f)] float m_FootRotationWeight = 1f;
        [SerializeField] float m_FootRotationSpeed = 7f;
        [SerializeField, Range(0f, 90f)] float m_MaximumFootRotationAngle = 45f;
        [SerializeField] bool m_RotateSolver;
        [SerializeField] float m_RootCastRadius = 0.2f;
        [SerializeField] bool m_OverstepFallsDown;

        public int GroundLayerMask => m_GroundLayerMask.value;

        internal void ApplyTuning(
            string fieldPath,
            CharacterPoseTuningValue value)
        {
            if (value.Kind != CharacterPoseTuningValueKind.Float)
                throw new InvalidOperationException($"FinalIK Grounding tuning field '{fieldPath}' requires a float.");
            switch (fieldPath)
            {
                case "maximum-step": m_MaximumStep = value.FloatValue; break;
                case "height-offset": m_HeightOffset = value.FloatValue; break;
                case "foot-height-speed": m_FootHeightSpeed = value.FloatValue; break;
                case "foot-radius": m_FootRadius = value.FloatValue; break;
                case "velocity-prediction": m_VelocityPrediction = value.FloatValue; break;
                case "foot-rotation-weight": m_FootRotationWeight = value.FloatValue; break;
                case "foot-rotation-speed": m_FootRotationSpeed = value.FloatValue; break;
                case "maximum-foot-rotation-angle": m_MaximumFootRotationAngle = value.FloatValue; break;
                default:
                    throw new InvalidOperationException($"FinalIK Grounding tuning field '{fieldPath}' is not declared.");
            }
            _ = Build();
        }

        public CharacterFinalIkGroundingSettings Build()
        {
            return new CharacterFinalIkGroundingSettings(
                m_Quality,
                m_GroundLayerMask.value,
                m_MaximumStep,
                m_HeightOffset,
                m_FootHeightSpeed,
                m_FootRadius,
                m_VelocityPrediction,
                m_FootRotationWeight,
                m_FootRotationSpeed,
                m_MaximumFootRotationAngle,
                m_RotateSolver,
                m_RootCastRadius,
                m_OverstepFallsDown);
        }
    }

    [Serializable]
    public sealed class CharacterPredictiveFootPlacementAuthoringSettings
    {
        [SerializeField, Range(4, 32)] int m_HitCapacity = 16;
        [SerializeField] float m_PathSphereRadius = 0.08f;
        [SerializeField] float m_SwingCapsuleRadius = 0.05f;
        [SerializeField] float m_CastAbove = 0.35f;
        [SerializeField] float m_CastBelow = 0.75f;
        [SerializeField, Range(1, 6)] int m_PathSampleCount = 3;
        [SerializeField, Range(0f, 89f)] float m_MaximumSlopeDegrees = 55f;
        [SerializeField] float m_MaximumStepUp = 0.45f;
        [SerializeField] float m_MaximumStepDown = 0.65f;
        [SerializeField] float m_MaximumHeightDiscontinuity = 0.35f;
        [SerializeField] float m_MaximumEdgeGap = 0.4f;
        [SerializeField] float m_MaximumSwingClearance = 0.16f;
        [SerializeField] float m_PlantSpeedThreshold = 0.6f;
        [SerializeField] float m_UnalignmentSpeedThreshold = 2f;
        [SerializeField, Range(0f, 1f)] float m_PlantConfidenceEnter = 0.65f;
        [SerializeField, Range(0f, 1f)] float m_PlantConfidenceExit = 0.35f;
        [SerializeField] float m_MinimumLookAheadSeconds = 0.04f;
        [SerializeField] float m_MaximumLookAheadSeconds = 0.22f;
        [SerializeField] float m_MaximumYawVelocityDegreesPerSecond = 540f;
        [SerializeField] float m_MaximumPredictionDistance = 0.65f;
        [SerializeField, Range(0.5f, 1.25f)] float m_MaximumPredictionReachRatio = 0.98f;
        [SerializeField] float m_SlideStartDistance = 0.07f;
        [SerializeField] float m_SlideStopDistance = 0.025f;
        [SerializeField] float m_MaximumSlideDistance = 0.14f;
        [SerializeField] float m_SlideSpeed = 0.45f;
        [SerializeField] float m_ReplantDistance = 0.3f;
        [SerializeField, Range(0f, 180f)] float m_ReplantAngleDegrees = 32f;
        [SerializeField] float m_MinimumFootSeparation = 0.12f;
        [SerializeField, Range(0f, 180f)] float m_MaximumAnkleTwistDegrees = 35f;
        [SerializeField] CharacterFootPlantLockType m_LockType = CharacterFootPlantLockType.PivotAroundToe;
        [SerializeField] bool m_AdjustHeelBeforePlanting = true;
        [SerializeField, Range(0f, 1f)] float m_HeelLiftRatio = 1f;
        [SerializeField, Range(0.01f, 0.9f)] float m_MinimumLegExtensionRatio = 0.18f;
        [SerializeField, Range(0.5f, 0.999f)] float m_MaximumLegExtensionRatio = 0.98f;
        [SerializeField] CharacterFootPlacementPelvisHeightMode m_PelvisHeightMode = CharacterFootPlacementPelvisHeightMode.AllPlantedFeet;
        [SerializeField] CharacterFootPlacementActorMovementCompensationMode m_ActorMovementCompensationMode = CharacterFootPlacementActorMovementCompensationMode.FollowBody;
        [SerializeField] float m_MaximumPelvisLowering = 0.32f;
        [SerializeField] float m_MaximumPelvisRaising = 0.18f;
        [SerializeField] float m_PelvisInterpolationSpeed = 14f;
        [SerializeField] float m_PelvisHeightDeadZone = 0.003f;
        [SerializeField] float m_MaximumHorizontalFootAdjustment = 0.25f;
        [SerializeField, Range(0f, 1f)] float m_MinimumSourceContribution = 0.05f;

        public CharacterPredictiveFootPlacementRuntimeSettings Build()
        {
            var value = new CharacterPredictiveFootPlacementRuntimeSettings(
                m_HitCapacity,
                m_PathSphereRadius,
                m_SwingCapsuleRadius,
                m_CastAbove,
                m_CastBelow,
                m_PathSampleCount,
                m_MaximumSlopeDegrees,
                m_MaximumStepUp,
                m_MaximumStepDown,
                m_MaximumHeightDiscontinuity,
                m_MaximumEdgeGap,
                m_MaximumSwingClearance,
                m_PlantSpeedThreshold,
                m_UnalignmentSpeedThreshold,
                m_PlantConfidenceEnter,
                m_PlantConfidenceExit,
                m_MinimumLookAheadSeconds,
                m_MaximumLookAheadSeconds,
                m_MaximumYawVelocityDegreesPerSecond,
                m_MaximumPredictionDistance,
                m_MaximumPredictionReachRatio,
                m_SlideStartDistance,
                m_SlideStopDistance,
                m_MaximumSlideDistance,
                m_SlideSpeed,
                m_ReplantDistance,
                m_ReplantAngleDegrees,
                m_MinimumFootSeparation,
                m_MaximumAnkleTwistDegrees,
                m_LockType,
                m_AdjustHeelBeforePlanting,
                m_HeelLiftRatio,
                m_MinimumLegExtensionRatio,
                m_MaximumLegExtensionRatio,
                m_PelvisHeightMode,
                m_ActorMovementCompensationMode,
                m_MaximumPelvisLowering,
                m_MaximumPelvisRaising,
                m_PelvisInterpolationSpeed,
                m_PelvisHeightDeadZone,
                m_MaximumHorizontalFootAdjustment,
                m_MinimumSourceContribution);
            value.RequireValid();
            return value;
        }

        internal void ApplyTuning(
            string fieldPath,
            CharacterPoseTuningValue value)
        {
            if (fieldPath == "hit-capacity" || fieldPath == "path-sample-count")
                throw new InvalidOperationException("Foot Placement workspace capacity is Structural.");
            if (value.Kind != CharacterPoseTuningValueKind.Float)
                throw new InvalidOperationException($"Predictive Foot Placement tuning field '{fieldPath}' requires a float.");
            switch (fieldPath)
            {
                case "path-sphere-radius": m_PathSphereRadius = value.FloatValue; break;
                case "swing-capsule-radius": m_SwingCapsuleRadius = value.FloatValue; break;
                case "cast-above": m_CastAbove = value.FloatValue; break;
                case "cast-below": m_CastBelow = value.FloatValue; break;
                case "maximum-slope-degrees": m_MaximumSlopeDegrees = value.FloatValue; break;
                case "maximum-step-up": m_MaximumStepUp = value.FloatValue; break;
                case "maximum-step-down": m_MaximumStepDown = value.FloatValue; break;
                case "maximum-height-discontinuity": m_MaximumHeightDiscontinuity = value.FloatValue; break;
                case "maximum-edge-gap": m_MaximumEdgeGap = value.FloatValue; break;
                case "maximum-swing-clearance": m_MaximumSwingClearance = value.FloatValue; break;
                case "plant-speed-threshold": m_PlantSpeedThreshold = value.FloatValue; break;
                case "unalignment-speed-threshold": m_UnalignmentSpeedThreshold = value.FloatValue; break;
                case "plant-confidence-enter": m_PlantConfidenceEnter = value.FloatValue; break;
                case "plant-confidence-exit": m_PlantConfidenceExit = value.FloatValue; break;
                case "minimum-look-ahead-seconds": m_MinimumLookAheadSeconds = value.FloatValue; break;
                case "maximum-look-ahead-seconds": m_MaximumLookAheadSeconds = value.FloatValue; break;
                case "maximum-yaw-velocity": m_MaximumYawVelocityDegreesPerSecond = value.FloatValue; break;
                case "maximum-prediction-distance": m_MaximumPredictionDistance = value.FloatValue; break;
                case "maximum-prediction-reach-ratio": m_MaximumPredictionReachRatio = value.FloatValue; break;
                case "slide-start-distance": m_SlideStartDistance = value.FloatValue; break;
                case "slide-stop-distance": m_SlideStopDistance = value.FloatValue; break;
                case "maximum-slide-distance": m_MaximumSlideDistance = value.FloatValue; break;
                case "slide-speed": m_SlideSpeed = value.FloatValue; break;
                case "replant-distance": m_ReplantDistance = value.FloatValue; break;
                case "replant-angle-degrees": m_ReplantAngleDegrees = value.FloatValue; break;
                case "minimum-foot-separation": m_MinimumFootSeparation = value.FloatValue; break;
                case "maximum-ankle-twist-degrees": m_MaximumAnkleTwistDegrees = value.FloatValue; break;
                case "heel-lift-ratio": m_HeelLiftRatio = value.FloatValue; break;
                case "minimum-leg-extension-ratio": m_MinimumLegExtensionRatio = value.FloatValue; break;
                case "maximum-leg-extension-ratio": m_MaximumLegExtensionRatio = value.FloatValue; break;
                case "maximum-pelvis-lowering": m_MaximumPelvisLowering = value.FloatValue; break;
                case "maximum-pelvis-raising": m_MaximumPelvisRaising = value.FloatValue; break;
                case "pelvis-interpolation-speed": m_PelvisInterpolationSpeed = value.FloatValue; break;
                case "pelvis-height-dead-zone": m_PelvisHeightDeadZone = value.FloatValue; break;
                case "maximum-horizontal-foot-adjustment": m_MaximumHorizontalFootAdjustment = value.FloatValue; break;
                case "minimum-source-contribution": m_MinimumSourceContribution = value.FloatValue; break;
                default:
                    throw new InvalidOperationException($"Predictive Foot Placement tuning field '{fieldPath}' is not declared.");
            }
            _ = Build();
        }
    }

    public readonly struct CharacterPredictiveFootPlacementRuntimeSettings
    {
        public CharacterPredictiveFootPlacementRuntimeSettings(
            int hitCapacity,
            float pathSphereRadius,
            float swingCapsuleRadius,
            float castAbove,
            float castBelow,
            int pathSampleCount,
            float maximumSlopeDegrees,
            float maximumStepUp,
            float maximumStepDown,
            float maximumHeightDiscontinuity,
            float maximumEdgeGap,
            float maximumSwingClearance,
            float plantSpeedThreshold,
            float unalignmentSpeedThreshold,
            float plantConfidenceEnter,
            float plantConfidenceExit,
            float minimumLookAheadSeconds,
            float maximumLookAheadSeconds,
            float maximumYawVelocityDegreesPerSecond,
            float maximumPredictionDistance,
            float maximumPredictionReachRatio,
            float slideStartDistance,
            float slideStopDistance,
            float maximumSlideDistance,
            float slideSpeed,
            float replantDistance,
            float replantAngleDegrees,
            float minimumFootSeparation,
            float maximumAnkleTwistDegrees,
            CharacterFootPlantLockType lockType,
            bool adjustHeelBeforePlanting,
            float heelLiftRatio,
            float minimumLegExtensionRatio,
            float maximumLegExtensionRatio,
            CharacterFootPlacementPelvisHeightMode pelvisHeightMode,
            CharacterFootPlacementActorMovementCompensationMode actorMovementCompensationMode,
            float maximumPelvisLowering,
            float maximumPelvisRaising,
            float pelvisInterpolationSpeed,
            float pelvisHeightDeadZone,
            float maximumHorizontalFootAdjustment,
            float minimumSourceContribution)
        {
            HitCapacity = hitCapacity;
            PathSphereRadius = pathSphereRadius;
            SwingCapsuleRadius = swingCapsuleRadius;
            CastAbove = castAbove;
            CastBelow = castBelow;
            PathSampleCount = pathSampleCount;
            MaximumSlopeDegrees = maximumSlopeDegrees;
            MaximumStepUp = maximumStepUp;
            MaximumStepDown = maximumStepDown;
            MaximumHeightDiscontinuity = maximumHeightDiscontinuity;
            MaximumEdgeGap = maximumEdgeGap;
            MaximumSwingClearance = maximumSwingClearance;
            PlantSpeedThreshold = plantSpeedThreshold;
            UnalignmentSpeedThreshold = unalignmentSpeedThreshold;
            PlantConfidenceEnter = plantConfidenceEnter;
            PlantConfidenceExit = plantConfidenceExit;
            MinimumLookAheadSeconds = minimumLookAheadSeconds;
            MaximumLookAheadSeconds = maximumLookAheadSeconds;
            MaximumYawVelocityDegreesPerSecond = maximumYawVelocityDegreesPerSecond;
            MaximumPredictionDistance = maximumPredictionDistance;
            MaximumPredictionReachRatio = maximumPredictionReachRatio;
            SlideStartDistance = slideStartDistance;
            SlideStopDistance = slideStopDistance;
            MaximumSlideDistance = maximumSlideDistance;
            SlideSpeed = slideSpeed;
            ReplantDistance = replantDistance;
            ReplantAngleDegrees = replantAngleDegrees;
            MinimumFootSeparation = minimumFootSeparation;
            MaximumAnkleTwistDegrees = maximumAnkleTwistDegrees;
            LockType = lockType;
            AdjustHeelBeforePlanting = adjustHeelBeforePlanting;
            HeelLiftRatio = heelLiftRatio;
            MinimumLegExtensionRatio = minimumLegExtensionRatio;
            MaximumLegExtensionRatio = maximumLegExtensionRatio;
            PelvisHeightMode = pelvisHeightMode;
            ActorMovementCompensationMode = actorMovementCompensationMode;
            MaximumPelvisLowering = maximumPelvisLowering;
            MaximumPelvisRaising = maximumPelvisRaising;
            PelvisInterpolationSpeed = pelvisInterpolationSpeed;
            PelvisHeightDeadZone = pelvisHeightDeadZone;
            MaximumHorizontalFootAdjustment = maximumHorizontalFootAdjustment;
            MinimumSourceContribution = minimumSourceContribution;
        }

        public int HitCapacity { get; }
        public float PathSphereRadius { get; }
        public float SwingCapsuleRadius { get; }
        public float CastAbove { get; }
        public float CastBelow { get; }
        public int PathSampleCount { get; }
        public float MaximumSlopeDegrees { get; }
        public float MaximumStepUp { get; }
        public float MaximumStepDown { get; }
        public float MaximumHeightDiscontinuity { get; }
        public float MaximumEdgeGap { get; }
        public float MaximumSwingClearance { get; }
        public float PlantSpeedThreshold { get; }
        public float UnalignmentSpeedThreshold { get; }
        public float PlantConfidenceEnter { get; }
        public float PlantConfidenceExit { get; }
        public float MinimumLookAheadSeconds { get; }
        public float MaximumLookAheadSeconds { get; }
        public float MaximumYawVelocityDegreesPerSecond { get; }
        public float MaximumPredictionDistance { get; }
        public float MaximumPredictionReachRatio { get; }
        public float SlideStartDistance { get; }
        public float SlideStopDistance { get; }
        public float MaximumSlideDistance { get; }
        public float SlideSpeed { get; }
        public float ReplantDistance { get; }
        public float ReplantAngleDegrees { get; }
        public float MinimumFootSeparation { get; }
        public float MaximumAnkleTwistDegrees { get; }
        public CharacterFootPlantLockType LockType { get; }
        public bool AdjustHeelBeforePlanting { get; }
        public float HeelLiftRatio { get; }
        public float MinimumLegExtensionRatio { get; }
        public float MaximumLegExtensionRatio { get; }
        public CharacterFootPlacementPelvisHeightMode PelvisHeightMode { get; }
        public CharacterFootPlacementActorMovementCompensationMode ActorMovementCompensationMode { get; }
        public float MaximumPelvisLowering { get; }
        public float MaximumPelvisRaising { get; }
        public float PelvisInterpolationSpeed { get; }
        public float PelvisHeightDeadZone { get; }
        public float MaximumHorizontalFootAdjustment { get; }
        public float MinimumSourceContribution { get; }

        public void RequireValid()
        {
            RequireRange(HitCapacity, 4, 32, nameof(HitCapacity));
            RequireRange(PathSampleCount, 1, 6, nameof(PathSampleCount));
            RequirePositive(PathSphereRadius, nameof(PathSphereRadius));
            RequirePositive(SwingCapsuleRadius, nameof(SwingCapsuleRadius));
            RequirePositive(CastAbove, nameof(CastAbove));
            RequirePositive(CastBelow, nameof(CastBelow));
            RequireRange(MaximumSlopeDegrees, 0f, 89f, nameof(MaximumSlopeDegrees));
            RequireNonNegative(MaximumStepUp, nameof(MaximumStepUp));
            RequireNonNegative(MaximumStepDown, nameof(MaximumStepDown));
            RequireNonNegative(MaximumHeightDiscontinuity, nameof(MaximumHeightDiscontinuity));
            RequireNonNegative(MaximumEdgeGap, nameof(MaximumEdgeGap));
            RequireNonNegative(MaximumSwingClearance, nameof(MaximumSwingClearance));
            RequireOrdered(PlantSpeedThreshold, UnalignmentSpeedThreshold, nameof(PlantSpeedThreshold), nameof(UnalignmentSpeedThreshold));
            RequireRange(PlantConfidenceExit, 0f, 1f, nameof(PlantConfidenceExit));
            RequireRange(PlantConfidenceEnter, 0f, 1f, nameof(PlantConfidenceEnter));
            if (PlantConfidenceExit >= PlantConfidenceEnter)
                throw new InvalidOperationException("Predictive Foot Placement contact hysteresis is invalid.");
            RequireOrdered(MinimumLookAheadSeconds, MaximumLookAheadSeconds, nameof(MinimumLookAheadSeconds), nameof(MaximumLookAheadSeconds));
            RequirePositive(MaximumYawVelocityDegreesPerSecond, nameof(MaximumYawVelocityDegreesPerSecond));
            RequirePositive(MaximumPredictionDistance, nameof(MaximumPredictionDistance));
            RequireRange(MaximumPredictionReachRatio, 0.5f, 1.25f, nameof(MaximumPredictionReachRatio));
            RequireOrdered(SlideStopDistance, SlideStartDistance, nameof(SlideStopDistance), nameof(SlideStartDistance));
            RequirePositive(MaximumSlideDistance, nameof(MaximumSlideDistance));
            RequirePositive(SlideSpeed, nameof(SlideSpeed));
            RequirePositive(ReplantDistance, nameof(ReplantDistance));
            RequireRange(ReplantAngleDegrees, 0f, 180f, nameof(ReplantAngleDegrees));
            RequireNonNegative(MinimumFootSeparation, nameof(MinimumFootSeparation));
            RequireRange(MaximumAnkleTwistDegrees, 0f, 180f, nameof(MaximumAnkleTwistDegrees));
            if (!Enum.IsDefined(typeof(CharacterFootPlantLockType), LockType))
                throw new InvalidOperationException("Predictive Foot Placement LockType is invalid.");
            RequireRange(HeelLiftRatio, 0f, 1f, nameof(HeelLiftRatio));
            RequireRange(MinimumLegExtensionRatio, 0.01f, 0.9f, nameof(MinimumLegExtensionRatio));
            RequireRange(MaximumLegExtensionRatio, 0.5f, 0.999f, nameof(MaximumLegExtensionRatio));
            if (MinimumLegExtensionRatio >= MaximumLegExtensionRatio)
                throw new InvalidOperationException("Predictive Foot Placement leg reach range is invalid.");
            if (!Enum.IsDefined(typeof(CharacterFootPlacementPelvisHeightMode), PelvisHeightMode))
                throw new InvalidOperationException("Predictive Foot Placement PelvisHeightMode is invalid.");
            if (!Enum.IsDefined(typeof(CharacterFootPlacementActorMovementCompensationMode), ActorMovementCompensationMode))
                throw new InvalidOperationException("Predictive Foot Placement ActorMovementCompensationMode is invalid.");
            RequireNonNegative(MaximumPelvisLowering, nameof(MaximumPelvisLowering));
            RequireNonNegative(MaximumPelvisRaising, nameof(MaximumPelvisRaising));
            RequirePositive(PelvisInterpolationSpeed, nameof(PelvisInterpolationSpeed));
            RequireNonNegative(PelvisHeightDeadZone, nameof(PelvisHeightDeadZone));
            RequireNonNegative(MaximumHorizontalFootAdjustment, nameof(MaximumHorizontalFootAdjustment));
            RequireRange(MinimumSourceContribution, 0f, 1f, nameof(MinimumSourceContribution));
        }

        static void RequireRange(int value, int minimum, int maximum, string field)
        {
            if (value < minimum || value > maximum)
                throw new InvalidOperationException($"Predictive Foot Placement {field} is outside [{minimum}, {maximum}].");
        }

        static void RequireRange(float value, float minimum, float maximum, string field)
        {
            if (!float.IsFinite(value) || value < minimum || value > maximum)
                throw new InvalidOperationException($"Predictive Foot Placement {field} is outside [{minimum}, {maximum}].");
        }

        static void RequirePositive(float value, string field)
        {
            if (!float.IsFinite(value) || value <= 0f)
                throw new InvalidOperationException($"Predictive Foot Placement {field} must be positive.");
        }

        static void RequireNonNegative(float value, string field)
        {
            if (!float.IsFinite(value) || value < 0f)
                throw new InvalidOperationException($"Predictive Foot Placement {field} must be non-negative.");
        }

        static void RequireOrdered(float minimum, float maximum, string minimumField, string maximumField)
        {
            RequireNonNegative(minimum, minimumField);
            RequirePositive(maximum, maximumField);
            if (minimum >= maximum)
                throw new InvalidOperationException($"Predictive Foot Placement {minimumField}/{maximumField} ordering is invalid.");
        }
    }
}
