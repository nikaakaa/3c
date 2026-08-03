using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum FootPlacementActorMovementCompensationMode : byte
    {
        ComponentSpace = 0,
        WorldSpace = 1,
        SuddenMotionOnly = 2
    }

    [Serializable]
    public sealed class FootPlacementTraceSettings
    {
        [SerializeField] LayerMask m_GroundLayerMask;
        [SerializeField] int m_CharacterLayer;
        [SerializeField] float m_SphereRadius = 0.08f;
        [SerializeField] float m_CapsuleRadius = 0.05f;
        [SerializeField] float m_CastAbove = 0.35f;
        [SerializeField] float m_CastBelow = 0.75f;
        [SerializeField, Range(1, 6)] int m_PathSampleCount = 3;
        [SerializeField, Range(4, 32)] int m_HitCapacity = 16;
        [SerializeField, Range(4, 64)] int m_CandidateCapacity = 24;
        [SerializeField] float m_MaximumSlopeDegrees = 55f;
        [SerializeField] float m_MaximumStepUp = 0.45f;
        [SerializeField] float m_MaximumStepDown = 0.65f;
        [SerializeField] float m_MaximumHeightDiscontinuity = 0.35f;
        [SerializeField] float m_MaximumEdgeGap = 0.4f;
        [SerializeField] float m_MaximumSwingClearance = 0.16f;

        internal FootPlacementTraceRuntimeSettings Build(int rigCharacterLayer)
        {
            var value = new FootPlacementTraceRuntimeSettings(
                m_GroundLayerMask.value,
                m_CharacterLayer,
                m_SphereRadius,
                m_CapsuleRadius,
                m_CastAbove,
                m_CastBelow,
                m_PathSampleCount,
                m_HitCapacity,
                m_CandidateCapacity,
                m_MaximumSlopeDegrees,
                m_MaximumStepUp,
                m_MaximumStepDown,
                m_MaximumHeightDiscontinuity,
                m_MaximumEdgeGap,
                m_MaximumSwingClearance);
            value.RequireValid(rigCharacterLayer);
            return value;
        }
    }

    [Serializable]
    public sealed class FootPlacementContactSettings
    {
        [SerializeField] float m_PlantDistance = 0.08f;
        [SerializeField] float m_ReleaseDistance = 0.18f;
        [SerializeField] float m_PlantPlanarSpeed = 0.14f;
        [SerializeField] float m_ReleasePlanarSpeed = 0.42f;
        [SerializeField] float m_PlantVerticalSpeed = 0.14f;
        [SerializeField] float m_ReleaseVerticalSpeed = 0.48f;
        [SerializeField] float m_DescendingTolerance = 0.04f;
        [SerializeField, Range(0f, 1f)] float m_MinimumPlacementWeight = 0.05f;
        [SerializeField, Range(0f, 1f)] float m_PlantConfidenceEnter = 0.65f;
        [SerializeField, Range(0f, 1f)] float m_PlantConfidenceExit = 0.35f;

        internal FootPlacementContactRuntimeSettings Build()
        {
            var value = new FootPlacementContactRuntimeSettings(
                m_PlantDistance,
                m_ReleaseDistance,
                m_PlantPlanarSpeed,
                m_ReleasePlanarSpeed,
                m_PlantVerticalSpeed,
                m_ReleaseVerticalSpeed,
                m_DescendingTolerance,
                m_MinimumPlacementWeight,
                m_PlantConfidenceEnter,
                m_PlantConfidenceExit);
            value.RequireValid();
            return value;
        }
    }

    [Serializable]
    public sealed class FootPlacementPredictionSettings
    {
        [SerializeField] float m_MinimumLookAheadSeconds = 0.04f;
        [SerializeField] float m_MaximumLookAheadSeconds = 0.22f;
        [SerializeField] float m_MaximumYawVelocityDegreesPerSecond = 540f;
        [SerializeField] float m_MaximumPredictionDistance = 0.65f;
        [SerializeField, Range(0.5f, 1.25f)] float m_MaximumReachRatio = 0.98f;

        internal FootPlacementPredictionRuntimeSettings Build()
        {
            var value = new FootPlacementPredictionRuntimeSettings(
                m_MinimumLookAheadSeconds,
                m_MaximumLookAheadSeconds,
                m_MaximumYawVelocityDegreesPerSecond,
                m_MaximumPredictionDistance,
                m_MaximumReachRatio);
            value.RequireValid();
            return value;
        }
    }

    [Serializable]
    public sealed class FootPlacementConstraintSettings
    {
        [SerializeField] float m_SlideStartDistance = 0.07f;
        [SerializeField] float m_SlideStopDistance = 0.025f;
        [SerializeField] float m_MaximumSlideDistance = 0.14f;
        [SerializeField] float m_SlideSpeed = 0.45f;
        [SerializeField] float m_ReplantDistance = 0.3f;
        [SerializeField] float m_ReplantAngleDegrees = 32f;
        [SerializeField] float m_MinimumFootSeparation = 0.12f;
        [SerializeField] float m_MaximumHeelLiftDegrees = 20f;
        [SerializeField] float m_MaximumHeelLiftDistance = 0.06f;
        [SerializeField] float m_MaximumAnkleTwistDegrees = 35f;

        internal FootPlacementConstraintRuntimeSettings Build()
        {
            var value = new FootPlacementConstraintRuntimeSettings(
                m_SlideStartDistance,
                m_SlideStopDistance,
                m_MaximumSlideDistance,
                m_SlideSpeed,
                m_ReplantDistance,
                m_ReplantAngleDegrees,
                m_MinimumFootSeparation,
                m_MaximumHeelLiftDegrees,
                m_MaximumHeelLiftDistance,
                m_MaximumAnkleTwistDegrees);
            value.RequireValid();
            return value;
        }
    }

    [Serializable]
    public sealed class FootPlacementLimbSettings
    {
        [SerializeField, Range(0.01f, 0.9f)] float m_MinimumLegExtensionRatio = 0.18f;
        [SerializeField, Range(0.5f, 0.999f)] float m_BendStabilizationStartRatio = 0.82f;
        [SerializeField, Range(0.5f, 0.999f)] float m_BendStabilizationFullRatio = 0.93f;
        [SerializeField, Range(0.5f, 0.999f)] float m_MaximumLegExtensionRatio = 0.98f;
        [SerializeField, Range(0f, 1f)] float m_MaximumBendStabilizationWeight = 0.65f;

        internal FootPlacementLimbRuntimeSettings Build()
        {
            var value = new FootPlacementLimbRuntimeSettings(
                m_MinimumLegExtensionRatio,
                m_BendStabilizationStartRatio,
                m_BendStabilizationFullRatio,
                m_MaximumLegExtensionRatio,
                m_MaximumBendStabilizationWeight);
            value.RequireValid();
            return value;
        }
    }

    [Serializable]
    public sealed class FootPlacementPelvisSettings
    {
        [SerializeField] FootPlacementPelvisHeightMode m_HeightMode = FootPlacementPelvisHeightMode.AllPlantedFeet;
        [SerializeField] float m_MinimumDirectionalSpeed = 0.15f;
        [SerializeField] float m_MinimumFootLeadDistance = 0.04f;
        [SerializeField] float m_MinimumSlopeHeightDifference = 0.025f;
        [SerializeField] float m_MaximumUpOffset = 0.18f;
        [SerializeField] float m_MaximumDownOffset = 0.32f;
        [SerializeField] float m_ReachSlack = 0.025f;
        [SerializeField] float m_HalfLifeSeconds = 0.08f;
        [SerializeField] float m_MaximumSpeed = 2f;
        [SerializeField] FootPlacementActorMovementCompensationMode m_ActorMovementCompensationMode = FootPlacementActorMovementCompensationMode.SuddenMotionOnly;
        [SerializeField] float m_SuddenVerticalThreshold = 0.05f;
        [SerializeField] float m_MaximumActorMovementCompensation = 0.3f;
        [SerializeField] float m_ActorMovementCompensationHalfLifeSeconds = 0.12f;
        [SerializeField] float m_ActorMovementCompensationMaximumSpeed = 2f;

        internal FootPlacementPelvisRuntimeSettings Build()
        {
            var value = new FootPlacementPelvisRuntimeSettings(
                m_HeightMode,
                m_MinimumDirectionalSpeed,
                m_MinimumFootLeadDistance,
                m_MinimumSlopeHeightDifference,
                m_MaximumUpOffset,
                m_MaximumDownOffset,
                m_ReachSlack,
                m_HalfLifeSeconds,
                m_MaximumSpeed,
                m_ActorMovementCompensationMode,
                m_SuddenVerticalThreshold,
                m_MaximumActorMovementCompensation,
                m_ActorMovementCompensationHalfLifeSeconds,
                m_ActorMovementCompensationMaximumSpeed);
            value.RequireValid();
            return value;
        }
    }

    [Serializable]
    public sealed class FootPlacementRotationSettings
    {
        [SerializeField] float m_MaximumPitchDegrees = 35f;
        [SerializeField] float m_MaximumRollDegrees = 28f;
        [SerializeField, Range(0f, 1f)] float m_AscentSurfaceAlignment = 0.35f;
        [SerializeField, Range(0f, 1f)] float m_DescentSurfaceAlignment = 0.85f;
        [SerializeField] float m_MaximumResponseSpeed = 6f;
        [SerializeField] AnimationCurve m_PositionResponseBySpeed = AnimationCurve.Linear(0f, 1f, 1f, 0.35f);
        [SerializeField] AnimationCurve m_RotationResponseBySpeed = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);

        internal FootPlacementRotationRuntimeSettings Build()
        {
            var value = new FootPlacementRotationRuntimeSettings(
                m_MaximumPitchDegrees,
                m_MaximumRollDegrees,
                m_AscentSurfaceAlignment,
                m_DescentSurfaceAlignment,
                m_MaximumResponseSpeed,
                m_PositionResponseBySpeed,
                m_RotationResponseBySpeed);
            value.RequireValid();
            return value;
        }
    }

    [Serializable]
    public sealed class FootPlacementSmoothingSettings
    {
        [SerializeField] float m_PlantHalfLifeSeconds = 0.04f;
        [SerializeField] float m_ReleaseHalfLifeSeconds = 0.075f;
        [SerializeField] float m_RotationHalfLifeSeconds = 0.055f;
        [SerializeField] float m_ClearanceHalfLifeSeconds = 0.05f;

        internal FootPlacementSmoothingRuntimeSettings Build()
        {
            var value = new FootPlacementSmoothingRuntimeSettings(
                m_PlantHalfLifeSeconds,
                m_ReleaseHalfLifeSeconds,
                m_RotationHalfLifeSeconds,
                m_ClearanceHalfLifeSeconds);
            value.RequireValid();
            return value;
        }
    }

    [CreateAssetMenu(fileName = "CharacterFootPlacementProfile", menuName = "3C/Presentation/Foot Placement Profile")]
    public sealed class CharacterFootPlacementProfile : ScriptableObject
    {
        [SerializeField] FootPlacementTraceSettings m_Trace = new FootPlacementTraceSettings();
        [SerializeField] FootPlacementContactSettings m_Contact = new FootPlacementContactSettings();
        [SerializeField] FootPlacementPredictionSettings m_Prediction = new FootPlacementPredictionSettings();
        [SerializeField] FootPlacementConstraintSettings m_Constraint = new FootPlacementConstraintSettings();
        [SerializeField] FootPlacementLimbSettings m_Limb = new FootPlacementLimbSettings();
        [SerializeField] FootPlacementPelvisSettings m_Pelvis = new FootPlacementPelvisSettings();
        [SerializeField] FootPlacementRotationSettings m_Rotation = new FootPlacementRotationSettings();
        [SerializeField] FootPlacementSmoothingSettings m_Smoothing = new FootPlacementSmoothingSettings();

        public void RequireConfiguration(CharacterFootPlacementPoseRig rig)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            RequireSections();
            m_Trace.Build(rig.CharacterLayer);
            m_Contact.Build();
            m_Prediction.Build();
            m_Constraint.Build();
            m_Limb.Build();
            m_Pelvis.Build();
            m_Rotation.Build();
            m_Smoothing.Build();
        }

        public CharacterFootPlacementRuntimeSettings BuildSettings(
            CharacterPresentationProjection projection,
            CharacterFootPlacementPoseRig rig)
        {
            if (projection == null || !projection.IsValid)
                throw new ArgumentException("Foot Placement requires a valid Presentation Projection.", nameof(projection));
            RequireConfiguration(rig);
            projection.RequirePosePayload();
            AnimationFootAnalysisProjectionIdentity footAnalysis = projection.FootAnalysis;
            if (footAnalysis == null || !footAnalysis.IsEnabled)
                throw new InvalidOperationException("Foot Placement requires generated Foot Analysis in the Presentation Projection.");
            footAnalysis.RequireValid();
            if (footAnalysis.CalibrationId != rig.CalibrationId ||
                footAnalysis.CalibrationSchemaVersion != rig.CalibrationSchemaVersion ||
                !string.Equals(footAnalysis.CalibrationRevision, rig.CalibrationRevision, StringComparison.Ordinal))
                throw new InvalidOperationException("Foot Placement Runtime Rig Calibration does not match the Presentation Projection.");
            CharacterPresentationPosePlan poseProgram = projection.PosePlan;
            int footPlacementWeightParameterIndex = poseProgram.RequireParameterIndex(
                AnimationPoseParameterIds.FootPlacementWeight);
            if (poseProgram.ContributionWorkspaceCount <= 0)
                throw new InvalidOperationException("Foot Placement requires a positive final pose contribution capacity.");

            return new CharacterFootPlacementRuntimeSettings(
                poseProgram.PlanHash,
                AnimationPoseParameterIds.FootPlacementWeight,
                footPlacementWeightParameterIndex,
                poseProgram.ContributionWorkspaceCount,
                footAnalysis,
                m_Trace.Build(rig.CharacterLayer),
                m_Contact.Build(),
                m_Prediction.Build(),
                m_Constraint.Build(),
                m_Limb.Build(),
                m_Pelvis.Build(),
                m_Rotation.Build(),
                m_Smoothing.Build());
        }

        void RequireSections()
        {
            if (m_Trace == null || m_Contact == null || m_Prediction == null || m_Constraint == null || m_Limb == null ||
                m_Pelvis == null || m_Rotation == null || m_Smoothing == null)
                throw new InvalidOperationException("Foot Placement Profile settings are incomplete.");
        }
    }

    public sealed class CharacterFootPlacementRuntimeSettings
    {
        internal CharacterFootPlacementRuntimeSettings(
            string posePlanHash,
            PoseParameterId footPlacementWeightParameterId,
            int footPlacementWeightParameterIndex,
            int contributionCapacity,
            AnimationFootAnalysisProjectionIdentity footAnalysis,
            FootPlacementTraceRuntimeSettings trace,
            FootPlacementContactRuntimeSettings contact,
            FootPlacementPredictionRuntimeSettings prediction,
            FootPlacementConstraintRuntimeSettings constraint,
            FootPlacementLimbRuntimeSettings limb,
            FootPlacementPelvisRuntimeSettings pelvis,
            FootPlacementRotationRuntimeSettings rotation,
            FootPlacementSmoothingRuntimeSettings smoothing)
        {
            PosePlanHash = CharacterFootPlacementValidation.RequireIdentity(
                posePlanHash,
                nameof(posePlanHash));
            if (!footPlacementWeightParameterId.IsValid)
                throw new ArgumentException("Foot Placement Pose Parameter identity is invalid.", nameof(footPlacementWeightParameterId));
            if (footPlacementWeightParameterIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(footPlacementWeightParameterIndex));
            if (contributionCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(contributionCapacity));
            FootPlacementWeightParameterId = footPlacementWeightParameterId;
            FootPlacementWeightParameterIndex = footPlacementWeightParameterIndex;
            ContributionCapacity = contributionCapacity;
            FootAnalysis = footAnalysis ?? throw new ArgumentNullException(nameof(footAnalysis));
            FootAnalysis.RequireValid();
            Trace = trace;
            Contact = contact;
            Prediction = prediction;
            Constraint = constraint;
            Limb = limb;
            Pelvis = pelvis;
            Rotation = rotation;
            Smoothing = smoothing;
        }

        public string PosePlanHash { get; }
        public PoseParameterId FootPlacementWeightParameterId { get; }
        public int FootPlacementWeightParameterIndex { get; }
        public int ContributionCapacity { get; }
        public AnimationFootAnalysisProjectionIdentity FootAnalysis { get; }
        public FootPlacementTraceRuntimeSettings Trace { get; }
        public FootPlacementContactRuntimeSettings Contact { get; }
        public FootPlacementPredictionRuntimeSettings Prediction { get; }
        public FootPlacementConstraintRuntimeSettings Constraint { get; }
        public FootPlacementLimbRuntimeSettings Limb { get; }
        public FootPlacementPelvisRuntimeSettings Pelvis { get; }
        public FootPlacementRotationRuntimeSettings Rotation { get; }
        public FootPlacementSmoothingRuntimeSettings Smoothing { get; }
    }

    public readonly struct FootPlacementTraceRuntimeSettings
    {
        public FootPlacementTraceRuntimeSettings(
            int groundLayerMask,
            int characterLayer,
            float sphereRadius,
            float capsuleRadius,
            float castAbove,
            float castBelow,
            int pathSampleCount,
            int hitCapacity,
            int candidateCapacity,
            float maximumSlopeDegrees,
            float maximumStepUp,
            float maximumStepDown,
            float maximumHeightDiscontinuity,
            float maximumEdgeGap,
            float maximumSwingClearance)
        {
            GroundLayerMask = groundLayerMask;
            CharacterLayer = characterLayer;
            SphereRadius = sphereRadius;
            CapsuleRadius = capsuleRadius;
            CastAbove = castAbove;
            CastBelow = castBelow;
            PathSampleCount = pathSampleCount;
            HitCapacity = hitCapacity;
            CandidateCapacity = candidateCapacity;
            MaximumSlopeDegrees = maximumSlopeDegrees;
            MaximumStepUp = maximumStepUp;
            MaximumStepDown = maximumStepDown;
            MaximumHeightDiscontinuity = maximumHeightDiscontinuity;
            MaximumEdgeGap = maximumEdgeGap;
            MaximumSwingClearance = maximumSwingClearance;
        }

        public int GroundLayerMask { get; }
        public int CharacterLayer { get; }
        public float SphereRadius { get; }
        public float CapsuleRadius { get; }
        public float CastAbove { get; }
        public float CastBelow { get; }
        public int PathSampleCount { get; }
        public int HitCapacity { get; }
        public int CandidateCapacity { get; }
        public float MaximumSlopeDegrees { get; }
        public float MaximumStepUp { get; }
        public float MaximumStepDown { get; }
        public float MaximumHeightDiscontinuity { get; }
        public float MaximumEdgeGap { get; }
        public float MaximumSwingClearance { get; }

        public void RequireValid(int rigCharacterLayer)
        {
            if (GroundLayerMask == 0)
                throw new InvalidOperationException("Foot Placement Ground LayerMask is empty.");
            if (CharacterLayer < 0 || CharacterLayer > 31 || CharacterLayer != rigCharacterLayer)
                throw new InvalidOperationException("Foot Placement Character layer does not match the explicit rig root.");
            if ((GroundLayerMask & (1 << CharacterLayer)) != 0)
                throw new InvalidOperationException("Foot Placement Ground LayerMask contains the Character layer.");
            CharacterFootPlacementValidation.RequirePositive(SphereRadius, nameof(SphereRadius));
            CharacterFootPlacementValidation.RequirePositive(CapsuleRadius, nameof(CapsuleRadius));
            CharacterFootPlacementValidation.RequirePositive(CastAbove, nameof(CastAbove));
            CharacterFootPlacementValidation.RequirePositive(CastBelow, nameof(CastBelow));
            CharacterFootPlacementValidation.RequireRange(PathSampleCount, 1, 6, nameof(PathSampleCount));
            CharacterFootPlacementValidation.RequireRange(HitCapacity, 4, 32, nameof(HitCapacity));
            CharacterFootPlacementValidation.RequireRange(CandidateCapacity, 4, 64, nameof(CandidateCapacity));
            CharacterFootPlacementValidation.RequireOrdered(0f, MaximumSlopeDegrees, 89f, nameof(MaximumSlopeDegrees));
            CharacterFootPlacementValidation.RequirePositive(MaximumStepUp, nameof(MaximumStepUp));
            CharacterFootPlacementValidation.RequirePositive(MaximumStepDown, nameof(MaximumStepDown));
            CharacterFootPlacementValidation.RequirePositive(MaximumHeightDiscontinuity, nameof(MaximumHeightDiscontinuity));
            CharacterFootPlacementValidation.RequirePositive(MaximumEdgeGap, nameof(MaximumEdgeGap));
            CharacterFootPlacementValidation.RequirePositive(MaximumSwingClearance, nameof(MaximumSwingClearance));
        }
    }

    public readonly struct FootPlacementContactRuntimeSettings
    {
        public FootPlacementContactRuntimeSettings(float plantDistance, float releaseDistance, float plantPlanarSpeed, float releasePlanarSpeed, float plantVerticalSpeed, float releaseVerticalSpeed, float descendingTolerance, float minimumPlacementWeight, float plantConfidenceEnter, float plantConfidenceExit)
        { PlantDistance = plantDistance; ReleaseDistance = releaseDistance; PlantPlanarSpeed = plantPlanarSpeed; ReleasePlanarSpeed = releasePlanarSpeed; PlantVerticalSpeed = plantVerticalSpeed; ReleaseVerticalSpeed = releaseVerticalSpeed; DescendingTolerance = descendingTolerance; MinimumPlacementWeight = minimumPlacementWeight; PlantConfidenceEnter = plantConfidenceEnter; PlantConfidenceExit = plantConfidenceExit; }
        public float PlantDistance { get; }
        public float ReleaseDistance { get; }
        public float PlantPlanarSpeed { get; }
        public float ReleasePlanarSpeed { get; }
        public float PlantVerticalSpeed { get; }
        public float ReleaseVerticalSpeed { get; }
        public float DescendingTolerance { get; }
        public float MinimumPlacementWeight { get; }
        public float PlantConfidenceEnter { get; }
        public float PlantConfidenceExit { get; }
        public void RequireValid()
        {
            CharacterFootPlacementValidation.RequireOrdered(0f, PlantDistance, ReleaseDistance, nameof(ReleaseDistance));
            CharacterFootPlacementValidation.RequireOrdered(0f, PlantPlanarSpeed, ReleasePlanarSpeed, nameof(ReleasePlanarSpeed));
            CharacterFootPlacementValidation.RequireOrdered(0f, PlantVerticalSpeed, ReleaseVerticalSpeed, nameof(ReleaseVerticalSpeed));
            CharacterFootPlacementValidation.RequireNonNegative(DescendingTolerance, nameof(DescendingTolerance));
            CharacterFootPlacementValidation.RequireWeight(MinimumPlacementWeight, nameof(MinimumPlacementWeight));
            CharacterFootPlacementValidation.RequireOrdered(0f, PlantConfidenceExit, PlantConfidenceEnter, nameof(PlantConfidenceEnter));
            CharacterFootPlacementValidation.RequireWeight(PlantConfidenceEnter, nameof(PlantConfidenceEnter));
        }
    }

    public readonly struct FootPlacementPredictionRuntimeSettings
    {
        public FootPlacementPredictionRuntimeSettings(float minimumLookAheadSeconds, float maximumLookAheadSeconds, float maximumYawVelocityDegreesPerSecond, float maximumPredictionDistance, float maximumReachRatio)
        { MinimumLookAheadSeconds = minimumLookAheadSeconds; MaximumLookAheadSeconds = maximumLookAheadSeconds; MaximumYawVelocityDegreesPerSecond = maximumYawVelocityDegreesPerSecond; MaximumPredictionDistance = maximumPredictionDistance; MaximumReachRatio = maximumReachRatio; }
        public float MinimumLookAheadSeconds { get; }
        public float MaximumLookAheadSeconds { get; }
        public float MaximumYawVelocityDegreesPerSecond { get; }
        public float MaximumPredictionDistance { get; }
        public float MaximumReachRatio { get; }
        public void RequireValid()
        {
            CharacterFootPlacementValidation.RequireOrdered(0f, MinimumLookAheadSeconds, MaximumLookAheadSeconds, nameof(MaximumLookAheadSeconds));
            CharacterFootPlacementValidation.RequirePositive(MaximumYawVelocityDegreesPerSecond, nameof(MaximumYawVelocityDegreesPerSecond));
            CharacterFootPlacementValidation.RequirePositive(MaximumPredictionDistance, nameof(MaximumPredictionDistance));
            CharacterFootPlacementValidation.RequireOrdered(0f, MaximumReachRatio, 1.25f, nameof(MaximumReachRatio));
        }
    }

    public readonly struct FootPlacementConstraintRuntimeSettings
    {
        public FootPlacementConstraintRuntimeSettings(float slideStartDistance, float slideStopDistance, float maximumSlideDistance, float slideSpeed, float replantDistance, float replantAngleDegrees, float minimumFootSeparation, float maximumHeelLiftDegrees, float maximumHeelLiftDistance, float maximumAnkleTwistDegrees)
        { SlideStartDistance = slideStartDistance; SlideStopDistance = slideStopDistance; MaximumSlideDistance = maximumSlideDistance; SlideSpeed = slideSpeed; ReplantDistance = replantDistance; ReplantAngleDegrees = replantAngleDegrees; MinimumFootSeparation = minimumFootSeparation; MaximumHeelLiftDegrees = maximumHeelLiftDegrees; MaximumHeelLiftDistance = maximumHeelLiftDistance; MaximumAnkleTwistDegrees = maximumAnkleTwistDegrees; }
        public float SlideStartDistance { get; }
        public float SlideStopDistance { get; }
        public float MaximumSlideDistance { get; }
        public float SlideSpeed { get; }
        public float ReplantDistance { get; }
        public float ReplantAngleDegrees { get; }
        public float MinimumFootSeparation { get; }
        public float MaximumHeelLiftDegrees { get; }
        public float MaximumHeelLiftDistance { get; }
        public float MaximumAnkleTwistDegrees { get; }
        public void RequireValid()
        {
            CharacterFootPlacementValidation.RequireOrdered(0f, SlideStopDistance, SlideStartDistance, nameof(SlideStartDistance));
            CharacterFootPlacementValidation.RequireOrdered(SlideStartDistance, MaximumSlideDistance, ReplantDistance, nameof(ReplantDistance));
            CharacterFootPlacementValidation.RequirePositive(SlideSpeed, nameof(SlideSpeed));
            CharacterFootPlacementValidation.RequireOrdered(0f, ReplantAngleDegrees, 180f, nameof(ReplantAngleDegrees));
            CharacterFootPlacementValidation.RequireNonNegative(MinimumFootSeparation, nameof(MinimumFootSeparation));
            CharacterFootPlacementValidation.RequireOrdered(0f, MaximumHeelLiftDegrees, 60f, nameof(MaximumHeelLiftDegrees));
            CharacterFootPlacementValidation.RequireNonNegative(MaximumHeelLiftDistance, nameof(MaximumHeelLiftDistance));
            CharacterFootPlacementValidation.RequireOrdered(0f, MaximumAnkleTwistDegrees, 180f, nameof(MaximumAnkleTwistDegrees));
        }
    }

    public readonly struct FootPlacementLimbRuntimeSettings
    {
        public FootPlacementLimbRuntimeSettings(
            float minimumLegExtensionRatio,
            float bendStabilizationStartRatio,
            float bendStabilizationFullRatio,
            float maximumLegExtensionRatio,
            float maximumBendStabilizationWeight)
        {
            MinimumLegExtensionRatio = minimumLegExtensionRatio;
            BendStabilizationStartRatio = bendStabilizationStartRatio;
            BendStabilizationFullRatio = bendStabilizationFullRatio;
            MaximumLegExtensionRatio = maximumLegExtensionRatio;
            MaximumBendStabilizationWeight = maximumBendStabilizationWeight;
        }

        public float MinimumLegExtensionRatio { get; }
        public float BendStabilizationStartRatio { get; }
        public float BendStabilizationFullRatio { get; }
        public float MaximumLegExtensionRatio { get; }
        public float MaximumBendStabilizationWeight { get; }

        public void RequireValid()
        {
            CharacterFootPlacementValidation.RequireOrdered(0f, MinimumLegExtensionRatio, 1f, nameof(MinimumLegExtensionRatio), false);
            CharacterFootPlacementValidation.RequireOrdered(MinimumLegExtensionRatio, BendStabilizationStartRatio, 1f, nameof(BendStabilizationStartRatio), false);
            CharacterFootPlacementValidation.RequireOrdered(BendStabilizationStartRatio, BendStabilizationFullRatio, 1f, nameof(BendStabilizationFullRatio), false);
            CharacterFootPlacementValidation.RequireOrdered(BendStabilizationFullRatio, MaximumLegExtensionRatio, 1f, nameof(MaximumLegExtensionRatio), false);
            if (MaximumLegExtensionRatio >= 1f)
                throw new InvalidOperationException("Foot Placement maximum leg extension ratio must be less than one.");
            CharacterFootPlacementValidation.RequireWeight(MaximumBendStabilizationWeight, nameof(MaximumBendStabilizationWeight));
        }
    }

    public readonly struct FootPlacementPelvisRuntimeSettings
    {
        public FootPlacementPelvisRuntimeSettings(
            FootPlacementPelvisHeightMode heightMode,
            float minimumDirectionalSpeed,
            float minimumFootLeadDistance,
            float minimumSlopeHeightDifference,
            float maximumUpOffset,
            float maximumDownOffset,
            float reachSlack,
            float halfLifeSeconds,
            float maximumSpeed,
            FootPlacementActorMovementCompensationMode actorMovementCompensationMode,
            float suddenVerticalThreshold,
            float maximumActorMovementCompensation,
            float actorMovementCompensationHalfLifeSeconds,
            float actorMovementCompensationMaximumSpeed)
        {
            HeightMode = heightMode;
            MinimumDirectionalSpeed = minimumDirectionalSpeed;
            MinimumFootLeadDistance = minimumFootLeadDistance;
            MinimumSlopeHeightDifference = minimumSlopeHeightDifference;
            MaximumUpOffset = maximumUpOffset;
            MaximumDownOffset = maximumDownOffset;
            ReachSlack = reachSlack;
            HalfLifeSeconds = halfLifeSeconds;
            MaximumSpeed = maximumSpeed;
            ActorMovementCompensationMode = actorMovementCompensationMode;
            SuddenVerticalThreshold = suddenVerticalThreshold;
            MaximumActorMovementCompensation = maximumActorMovementCompensation;
            ActorMovementCompensationHalfLifeSeconds = actorMovementCompensationHalfLifeSeconds;
            ActorMovementCompensationMaximumSpeed = actorMovementCompensationMaximumSpeed;
        }
        public FootPlacementPelvisHeightMode HeightMode { get; }
        public float MinimumDirectionalSpeed { get; }
        public float MinimumFootLeadDistance { get; }
        public float MinimumSlopeHeightDifference { get; }
        public float MaximumUpOffset { get; }
        public float MaximumDownOffset { get; }
        public float ReachSlack { get; }
        public float HalfLifeSeconds { get; }
        public float MaximumSpeed { get; }
        public FootPlacementActorMovementCompensationMode ActorMovementCompensationMode { get; }
        public float SuddenVerticalThreshold { get; }
        public float MaximumActorMovementCompensation { get; }
        public float ActorMovementCompensationHalfLifeSeconds { get; }
        public float ActorMovementCompensationMaximumSpeed { get; }
        public void RequireValid()
        {
            if (!Enum.IsDefined(typeof(FootPlacementPelvisHeightMode), HeightMode))
                throw new InvalidOperationException("Foot Placement Pelvis Height mode is invalid.");
            CharacterFootPlacementValidation.RequirePositive(MinimumDirectionalSpeed, nameof(MinimumDirectionalSpeed));
            CharacterFootPlacementValidation.RequirePositive(MinimumFootLeadDistance, nameof(MinimumFootLeadDistance));
            CharacterFootPlacementValidation.RequirePositive(MinimumSlopeHeightDifference, nameof(MinimumSlopeHeightDifference));
            CharacterFootPlacementValidation.RequireNonNegative(MaximumUpOffset, nameof(MaximumUpOffset));
            CharacterFootPlacementValidation.RequireNonNegative(MaximumDownOffset, nameof(MaximumDownOffset));
            CharacterFootPlacementValidation.RequireNonNegative(ReachSlack, nameof(ReachSlack));
            CharacterFootPlacementValidation.RequirePositive(HalfLifeSeconds, nameof(HalfLifeSeconds));
            CharacterFootPlacementValidation.RequirePositive(MaximumSpeed, nameof(MaximumSpeed));
            if (!Enum.IsDefined(typeof(FootPlacementActorMovementCompensationMode), ActorMovementCompensationMode))
                throw new InvalidOperationException("Foot Placement Actor Movement Compensation mode is invalid.");
            CharacterFootPlacementValidation.RequirePositive(SuddenVerticalThreshold, nameof(SuddenVerticalThreshold));
            CharacterFootPlacementValidation.RequireNonNegative(MaximumActorMovementCompensation, nameof(MaximumActorMovementCompensation));
            CharacterFootPlacementValidation.RequirePositive(ActorMovementCompensationHalfLifeSeconds, nameof(ActorMovementCompensationHalfLifeSeconds));
            CharacterFootPlacementValidation.RequirePositive(ActorMovementCompensationMaximumSpeed, nameof(ActorMovementCompensationMaximumSpeed));
        }
    }

    public readonly struct FootPlacementRotationRuntimeSettings
    {
        public FootPlacementRotationRuntimeSettings(float maximumPitchDegrees, float maximumRollDegrees, float ascentSurfaceAlignment, float descentSurfaceAlignment, float maximumResponseSpeed, AnimationCurve positionResponseBySpeed, AnimationCurve rotationResponseBySpeed)
        { MaximumPitchDegrees = maximumPitchDegrees; MaximumRollDegrees = maximumRollDegrees; AscentSurfaceAlignment = ascentSurfaceAlignment; DescentSurfaceAlignment = descentSurfaceAlignment; MaximumResponseSpeed = maximumResponseSpeed; PositionResponseBySpeed = CopyCurve(positionResponseBySpeed); RotationResponseBySpeed = CopyCurve(rotationResponseBySpeed); }
        public float MaximumPitchDegrees { get; }
        public float MaximumRollDegrees { get; }
        public float AscentSurfaceAlignment { get; }
        public float DescentSurfaceAlignment { get; }
        public float MaximumResponseSpeed { get; }
        public AnimationCurve PositionResponseBySpeed { get; }
        public AnimationCurve RotationResponseBySpeed { get; }
        public void RequireValid()
        {
            CharacterFootPlacementValidation.RequireOrdered(0f, MaximumPitchDegrees, 90f, nameof(MaximumPitchDegrees));
            CharacterFootPlacementValidation.RequireOrdered(0f, MaximumRollDegrees, 90f, nameof(MaximumRollDegrees));
            CharacterFootPlacementValidation.RequireWeight(AscentSurfaceAlignment, nameof(AscentSurfaceAlignment));
            CharacterFootPlacementValidation.RequireWeight(DescentSurfaceAlignment, nameof(DescentSurfaceAlignment));
            CharacterFootPlacementValidation.RequirePositive(MaximumResponseSpeed, nameof(MaximumResponseSpeed));
            RequireCurve(PositionResponseBySpeed, nameof(PositionResponseBySpeed));
            RequireCurve(RotationResponseBySpeed, nameof(RotationResponseBySpeed));
        }

        public float SamplePositionResponse(float speed) =>
            Mathf.Clamp01(PositionResponseBySpeed.Evaluate(Mathf.Clamp01(speed / MaximumResponseSpeed)));

        public float SampleRotationResponse(float speed) =>
            Mathf.Clamp01(RotationResponseBySpeed.Evaluate(Mathf.Clamp01(speed / MaximumResponseSpeed)));

        static AnimationCurve CopyCurve(AnimationCurve source)
        {
            if (source == null)
                return null;
            return new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }

        static void RequireCurve(AnimationCurve curve, string field)
        {
            if (curve == null || curve.length < 2)
                throw new InvalidOperationException($"Foot Placement '{field}' requires a response curve.");
            for (int i = 0; i < curve.length; i++)
            {
                Keyframe key = curve.keys[i];
                CharacterFootPlacementValidation.RequireWeight(key.time, field);
                CharacterFootPlacementValidation.RequireWeight(key.value, field);
            }
        }
    }

    public readonly struct FootPlacementSmoothingRuntimeSettings
    {
        public FootPlacementSmoothingRuntimeSettings(float plantHalfLifeSeconds, float releaseHalfLifeSeconds, float rotationHalfLifeSeconds, float clearanceHalfLifeSeconds)
        { PlantHalfLifeSeconds = plantHalfLifeSeconds; ReleaseHalfLifeSeconds = releaseHalfLifeSeconds; RotationHalfLifeSeconds = rotationHalfLifeSeconds; ClearanceHalfLifeSeconds = clearanceHalfLifeSeconds; }
        public float PlantHalfLifeSeconds { get; }
        public float ReleaseHalfLifeSeconds { get; }
        public float RotationHalfLifeSeconds { get; }
        public float ClearanceHalfLifeSeconds { get; }
        public void RequireValid()
        {
            CharacterFootPlacementValidation.RequirePositive(PlantHalfLifeSeconds, nameof(PlantHalfLifeSeconds));
            CharacterFootPlacementValidation.RequirePositive(ReleaseHalfLifeSeconds, nameof(ReleaseHalfLifeSeconds));
            CharacterFootPlacementValidation.RequirePositive(RotationHalfLifeSeconds, nameof(RotationHalfLifeSeconds));
            CharacterFootPlacementValidation.RequirePositive(ClearanceHalfLifeSeconds, nameof(ClearanceHalfLifeSeconds));
        }
    }

    static class CharacterFootPlacementValidation
    {
        public static string RequireIdentity(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException($"Foot Placement requires stable identity '{field}'.");
            return value;
        }

        public static void RequireWeight(float value, string field) => RequireOrdered(0f, value, 1f, field);
        public static void RequirePositive(float value, string field) => RequireOrdered(0f, value, float.MaxValue, field, false);
        public static void RequireNonNegative(float value, string field) => RequireOrdered(0f, value, float.MaxValue, field);
        public static void RequireRange(int value, int minimum, int maximum, string field)
        {
            if (value < minimum || value > maximum)
                throw new InvalidOperationException($"Foot Placement '{field}' is outside [{minimum}, {maximum}].");
        }

        public static void RequireOrdered(float minimum, float value, float maximum, string field, bool inclusiveMinimum = true)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) ||
                (inclusiveMinimum ? value < minimum : value <= minimum) || value > maximum)
                throw new InvalidOperationException($"Foot Placement '{field}' is outside its valid range.");
        }
    }
}
