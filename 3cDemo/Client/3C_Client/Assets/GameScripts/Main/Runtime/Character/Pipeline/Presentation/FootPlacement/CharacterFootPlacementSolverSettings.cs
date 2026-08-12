using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    [Serializable]
    public sealed class CharacterLyraCurrentGroundingAuthoringSettings
    {
        [SerializeField] LayerMask m_GroundLayerMask;
        [SerializeField, Range(4, 32)] int m_HitCapacity = 16;
        [SerializeField] float m_TraceAbove = 0.5f;
        [SerializeField] float m_TraceBelow = 0.5f;
        [SerializeField] float m_TraceRadius = 0.05f;
        [SerializeField] float m_HitNormalSpringStrength = 8f;
        [SerializeField] float m_HitNormalCriticalDamping = 1f;
        [SerializeField] float m_FootOffsetSpringStrength = 2.5f;
        [SerializeField] float m_FootOffsetCriticalDamping = 1f;
        [SerializeField, Range(0f, 1f)] float m_FootOffsetTargetVelocityAmount = 0.2f;
        [SerializeField] float m_PelvisOffsetSpringStrength = 2.5f;
        [SerializeField] float m_PelvisOffsetCriticalDamping = 1f;

        public CharacterLyraCurrentGroundingSettings Build()
        {
            var value = new CharacterLyraCurrentGroundingSettings(
                m_GroundLayerMask.value,
                m_HitCapacity,
                m_TraceAbove,
                m_TraceBelow,
                m_TraceRadius,
                m_HitNormalSpringStrength,
                m_HitNormalCriticalDamping,
                m_FootOffsetSpringStrength,
                m_FootOffsetCriticalDamping,
                m_FootOffsetTargetVelocityAmount,
                m_PelvisOffsetSpringStrength,
                m_PelvisOffsetCriticalDamping);
            value.RequireValid();
            return value;
        }

        internal void ApplyTuning(string fieldPath, CharacterPoseTuningValue value)
        {
            if (fieldPath == "hit-capacity")
                throw new InvalidOperationException("Foot Grounding hit capacity is Structural.");
            if (value.Kind != CharacterPoseTuningValueKind.Float)
                throw new InvalidOperationException($"Lyra Current Grounding tuning field '{fieldPath}' requires a float.");
            switch (fieldPath)
            {
                case "trace-above": m_TraceAbove = value.FloatValue; break;
                case "trace-below": m_TraceBelow = value.FloatValue; break;
                case "trace-radius": m_TraceRadius = value.FloatValue; break;
                case "hit-normal-spring-strength": m_HitNormalSpringStrength = value.FloatValue; break;
                case "hit-normal-critical-damping": m_HitNormalCriticalDamping = value.FloatValue; break;
                case "foot-offset-spring-strength": m_FootOffsetSpringStrength = value.FloatValue; break;
                case "foot-offset-critical-damping": m_FootOffsetCriticalDamping = value.FloatValue; break;
                case "foot-offset-target-velocity-amount": m_FootOffsetTargetVelocityAmount = value.FloatValue; break;
                case "pelvis-offset-spring-strength": m_PelvisOffsetSpringStrength = value.FloatValue; break;
                case "pelvis-offset-critical-damping": m_PelvisOffsetCriticalDamping = value.FloatValue; break;
                default: throw new InvalidOperationException($"Lyra Current Grounding tuning field '{fieldPath}' is not declared.");
            }
            _ = Build();
        }
    }

    [Serializable]
    public sealed class CharacterStanceStabilizationAuthoringSettings
    {
        [SerializeField] float m_MaximumSurfaceSlopeDegrees = 55f;
        [SerializeField] float m_MaximumContactSurfaceDistance = 0.12f;
        [SerializeField] float m_PlantSpeedThreshold = 0.6f;
        [SerializeField] float m_UnalignmentSpeedThreshold = 2f;
        [SerializeField, Range(0f, 1f)] float m_PlantConfidenceEnter = 0.65f;
        [SerializeField, Range(0f, 1f)] float m_PlantConfidenceExit = 0.35f;
        [SerializeField] float m_AnchorBlendSpeed = 8f;
        [SerializeField] float m_MaximumAnchorDistance = 0.14f;
        [SerializeField, Range(0.01f, 0.9f)] float m_MinimumLegExtensionRatio = 0.18f;
        [SerializeField, Range(0.5f, 0.999f)] float m_MaximumLegExtensionRatio = 0.98f;
        [SerializeField] float m_MaximumPelvisLowering = 0.32f;
        [SerializeField] float m_MaximumPelvisRaising = 0.18f;

        public CharacterStanceStabilizationSettings Build()
        {
            var value = new CharacterStanceStabilizationSettings(
                m_MaximumSurfaceSlopeDegrees,
                m_MaximumContactSurfaceDistance,
                m_PlantSpeedThreshold,
                m_UnalignmentSpeedThreshold,
                m_PlantConfidenceEnter,
                m_PlantConfidenceExit,
                m_AnchorBlendSpeed,
                m_MaximumAnchorDistance,
                m_MinimumLegExtensionRatio,
                m_MaximumLegExtensionRatio,
                m_MaximumPelvisLowering,
                m_MaximumPelvisRaising);
            value.RequireValid();
            return value;
        }

        internal void ApplyTuning(string fieldPath, CharacterPoseTuningValue value)
        {
            if (value.Kind != CharacterPoseTuningValueKind.Float)
                throw new InvalidOperationException($"Stance Stabilization tuning field '{fieldPath}' requires a float.");
            switch (fieldPath)
            {
                case "maximum-surface-slope-degrees": m_MaximumSurfaceSlopeDegrees = value.FloatValue; break;
                case "maximum-contact-surface-distance": m_MaximumContactSurfaceDistance = value.FloatValue; break;
                case "plant-speed-threshold": m_PlantSpeedThreshold = value.FloatValue; break;
                case "unalignment-speed-threshold": m_UnalignmentSpeedThreshold = value.FloatValue; break;
                case "plant-confidence-enter": m_PlantConfidenceEnter = value.FloatValue; break;
                case "plant-confidence-exit": m_PlantConfidenceExit = value.FloatValue; break;
                case "anchor-blend-speed": m_AnchorBlendSpeed = value.FloatValue; break;
                case "maximum-anchor-distance": m_MaximumAnchorDistance = value.FloatValue; break;
                case "minimum-leg-extension-ratio": m_MinimumLegExtensionRatio = value.FloatValue; break;
                case "maximum-leg-extension-ratio": m_MaximumLegExtensionRatio = value.FloatValue; break;
                case "maximum-pelvis-lowering": m_MaximumPelvisLowering = value.FloatValue; break;
                case "maximum-pelvis-raising": m_MaximumPelvisRaising = value.FloatValue; break;
                default: throw new InvalidOperationException($"Stance Stabilization tuning field '{fieldPath}' is not declared.");
            }
            _ = Build();
        }
    }

    [Serializable]
    public sealed class CharacterPredictiveFootPlacementAuthoringSettings
    {
        [SerializeField] float m_PathSphereRadius = 0.08f;
        [SerializeField] float m_SwingCapsuleRadius = 0.05f;
        [SerializeField] float m_CastAbove = 0.35f;
        [SerializeField] float m_CastBelow = 0.75f;
        [SerializeField, Range(0f, 89f)] float m_MaximumSlopeDegrees = 55f;
        [SerializeField] float m_MaximumStepUp = 0.45f;
        [SerializeField] float m_MaximumStepDown = 0.65f;
        [SerializeField] float m_MaximumHeightDiscontinuity = 0.35f;
        [SerializeField] float m_MaximumEdgeGap = 0.4f;
        [SerializeField, Range(0f, 1f)] float m_MinimumLandingConfidence = 0.25f;
        [SerializeField, Range(0.5f, 1.25f)] float m_MaximumPredictionReachRatio = 0.98f;

        public CharacterPredictiveFootPlacementRuntimeSettings Build()
        {
            var value = new CharacterPredictiveFootPlacementRuntimeSettings(
                m_PathSphereRadius,
                m_SwingCapsuleRadius,
                m_CastAbove,
                m_CastBelow,
                m_MaximumSlopeDegrees,
                m_MaximumStepUp,
                m_MaximumStepDown,
                m_MaximumHeightDiscontinuity,
                m_MaximumEdgeGap,
                m_MinimumLandingConfidence,
                m_MaximumPredictionReachRatio);
            value.RequireValid();
            return value;
        }

        internal void ApplyTuning(string fieldPath, CharacterPoseTuningValue value)
        {
            if (value.Kind != CharacterPoseTuningValueKind.Float)
                throw new InvalidOperationException($"Predictive Extension tuning field '{fieldPath}' requires a float.");
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
                case "minimum-landing-confidence": m_MinimumLandingConfidence = value.FloatValue; break;
                case "maximum-prediction-reach-ratio": m_MaximumPredictionReachRatio = value.FloatValue; break;
                default: throw new InvalidOperationException($"Predictive Extension tuning field '{fieldPath}' is not declared.");
            }
            _ = Build();
        }
    }

    public readonly struct CharacterLyraCurrentGroundingSettings
    {
        public CharacterLyraCurrentGroundingSettings(
            int groundLayerMask,
            int hitCapacity,
            float traceAbove,
            float traceBelow,
            float traceRadius,
            float hitNormalSpringStrength,
            float hitNormalCriticalDamping,
            float footOffsetSpringStrength,
            float footOffsetCriticalDamping,
            float footOffsetTargetVelocityAmount,
            float pelvisOffsetSpringStrength,
            float pelvisOffsetCriticalDamping)
        {
            GroundLayerMask = groundLayerMask;
            HitCapacity = hitCapacity;
            TraceAbove = traceAbove;
            TraceBelow = traceBelow;
            TraceRadius = traceRadius;
            HitNormalSpringStrength = hitNormalSpringStrength;
            HitNormalCriticalDamping = hitNormalCriticalDamping;
            FootOffsetSpringStrength = footOffsetSpringStrength;
            FootOffsetCriticalDamping = footOffsetCriticalDamping;
            FootOffsetTargetVelocityAmount = footOffsetTargetVelocityAmount;
            PelvisOffsetSpringStrength = pelvisOffsetSpringStrength;
            PelvisOffsetCriticalDamping = pelvisOffsetCriticalDamping;
        }

        public int GroundLayerMask { get; }
        public int HitCapacity { get; }
        public float TraceAbove { get; }
        public float TraceBelow { get; }
        public float TraceRadius { get; }
        public float HitNormalSpringStrength { get; }
        public float HitNormalCriticalDamping { get; }
        public float FootOffsetSpringStrength { get; }
        public float FootOffsetCriticalDamping { get; }
        public float FootOffsetTargetVelocityAmount { get; }
        public float PelvisOffsetSpringStrength { get; }
        public float PelvisOffsetCriticalDamping { get; }

        public void RequireValid()
        {
            if (GroundLayerMask == 0 || HitCapacity < 4 || HitCapacity > 32)
                throw new InvalidOperationException("Lyra Current Grounding query workspace is invalid.");
            RequirePositive(TraceAbove, nameof(TraceAbove));
            RequirePositive(TraceBelow, nameof(TraceBelow));
            RequirePositive(TraceRadius, nameof(TraceRadius));
            RequireSpring(HitNormalSpringStrength, HitNormalCriticalDamping, nameof(HitNormalSpringStrength));
            RequireSpring(FootOffsetSpringStrength, FootOffsetCriticalDamping, nameof(FootOffsetSpringStrength));
            RequireSpring(PelvisOffsetSpringStrength, PelvisOffsetCriticalDamping, nameof(PelvisOffsetSpringStrength));
            RequireRange(FootOffsetTargetVelocityAmount, 0f, 1f, nameof(FootOffsetTargetVelocityAmount));
        }

        static void RequireSpring(float strength, float damping, string field)
        {
            RequirePositive(strength, field);
            RequirePositive(damping, field + "CriticalDamping");
        }

        internal static void RequirePositive(float value, string field)
        {
            if (!float.IsFinite(value) || value <= 0f)
                throw new InvalidOperationException($"Foot Placement {field} must be positive.");
        }

        internal static void RequireNonNegative(float value, string field)
        {
            if (!float.IsFinite(value) || value < 0f)
                throw new InvalidOperationException($"Foot Placement {field} must be non-negative.");
        }

        internal static void RequireRange(float value, float minimum, float maximum, string field)
        {
            if (!float.IsFinite(value) || value < minimum || value > maximum)
                throw new InvalidOperationException($"Foot Placement {field} is outside [{minimum}, {maximum}].");
        }
    }

    public readonly struct CharacterStanceStabilizationSettings
    {
        public CharacterStanceStabilizationSettings(
            float maximumSurfaceSlopeDegrees,
            float maximumContactSurfaceDistance,
            float plantSpeedThreshold,
            float unalignmentSpeedThreshold,
            float plantConfidenceEnter,
            float plantConfidenceExit,
            float anchorBlendSpeed,
            float maximumAnchorDistance,
            float minimumLegExtensionRatio,
            float maximumLegExtensionRatio,
            float maximumPelvisLowering,
            float maximumPelvisRaising)
        {
            MaximumSurfaceSlopeDegrees = maximumSurfaceSlopeDegrees;
            MaximumContactSurfaceDistance = maximumContactSurfaceDistance;
            PlantSpeedThreshold = plantSpeedThreshold;
            UnalignmentSpeedThreshold = unalignmentSpeedThreshold;
            PlantConfidenceEnter = plantConfidenceEnter;
            PlantConfidenceExit = plantConfidenceExit;
            AnchorBlendSpeed = anchorBlendSpeed;
            MaximumAnchorDistance = maximumAnchorDistance;
            MinimumLegExtensionRatio = minimumLegExtensionRatio;
            MaximumLegExtensionRatio = maximumLegExtensionRatio;
            MaximumPelvisLowering = maximumPelvisLowering;
            MaximumPelvisRaising = maximumPelvisRaising;
        }

        public float MaximumSurfaceSlopeDegrees { get; }
        public float MaximumContactSurfaceDistance { get; }
        public float PlantSpeedThreshold { get; }
        public float UnalignmentSpeedThreshold { get; }
        public float PlantConfidenceEnter { get; }
        public float PlantConfidenceExit { get; }
        public float AnchorBlendSpeed { get; }
        public float MaximumAnchorDistance { get; }
        public float MinimumLegExtensionRatio { get; }
        public float MaximumLegExtensionRatio { get; }
        public float MaximumPelvisLowering { get; }
        public float MaximumPelvisRaising { get; }

        public void RequireValid()
        {
            CharacterLyraCurrentGroundingSettings.RequireRange(MaximumSurfaceSlopeDegrees, 0f, 89f, nameof(MaximumSurfaceSlopeDegrees));
            CharacterLyraCurrentGroundingSettings.RequirePositive(MaximumContactSurfaceDistance, nameof(MaximumContactSurfaceDistance));
            CharacterLyraCurrentGroundingSettings.RequireNonNegative(PlantSpeedThreshold, nameof(PlantSpeedThreshold));
            CharacterLyraCurrentGroundingSettings.RequirePositive(UnalignmentSpeedThreshold, nameof(UnalignmentSpeedThreshold));
            if (PlantSpeedThreshold >= UnalignmentSpeedThreshold)
                throw new InvalidOperationException("Stance Stabilization speed thresholds are not ordered.");
            CharacterLyraCurrentGroundingSettings.RequireRange(PlantConfidenceEnter, 0f, 1f, nameof(PlantConfidenceEnter));
            CharacterLyraCurrentGroundingSettings.RequireRange(PlantConfidenceExit, 0f, 1f, nameof(PlantConfidenceExit));
            if (PlantConfidenceExit >= PlantConfidenceEnter)
                throw new InvalidOperationException("Stance Stabilization confidence thresholds are not ordered.");
            CharacterLyraCurrentGroundingSettings.RequirePositive(AnchorBlendSpeed, nameof(AnchorBlendSpeed));
            CharacterLyraCurrentGroundingSettings.RequirePositive(MaximumAnchorDistance, nameof(MaximumAnchorDistance));
            CharacterLyraCurrentGroundingSettings.RequireRange(MinimumLegExtensionRatio, 0.01f, 0.9f, nameof(MinimumLegExtensionRatio));
            CharacterLyraCurrentGroundingSettings.RequireRange(MaximumLegExtensionRatio, 0.5f, 0.999f, nameof(MaximumLegExtensionRatio));
            if (MinimumLegExtensionRatio >= MaximumLegExtensionRatio)
                throw new InvalidOperationException("Stance Stabilization leg reach range is invalid.");
            CharacterLyraCurrentGroundingSettings.RequireNonNegative(MaximumPelvisLowering, nameof(MaximumPelvisLowering));
            CharacterLyraCurrentGroundingSettings.RequireNonNegative(MaximumPelvisRaising, nameof(MaximumPelvisRaising));
        }
    }

    public readonly struct CharacterPredictiveFootPlacementRuntimeSettings
    {
        public CharacterPredictiveFootPlacementRuntimeSettings(
            float pathSphereRadius,
            float swingCapsuleRadius,
            float castAbove,
            float castBelow,
            float maximumSlopeDegrees,
            float maximumStepUp,
            float maximumStepDown,
            float maximumHeightDiscontinuity,
            float maximumEdgeGap,
            float minimumLandingConfidence,
            float maximumPredictionReachRatio)
        {
            PathSphereRadius = pathSphereRadius;
            SwingCapsuleRadius = swingCapsuleRadius;
            CastAbove = castAbove;
            CastBelow = castBelow;
            MaximumSlopeDegrees = maximumSlopeDegrees;
            MaximumStepUp = maximumStepUp;
            MaximumStepDown = maximumStepDown;
            MaximumHeightDiscontinuity = maximumHeightDiscontinuity;
            MaximumEdgeGap = maximumEdgeGap;
            MinimumLandingConfidence = minimumLandingConfidence;
            MaximumPredictionReachRatio = maximumPredictionReachRatio;
        }

        public float PathSphereRadius { get; }
        public float SwingCapsuleRadius { get; }
        public float CastAbove { get; }
        public float CastBelow { get; }
        public float MaximumSlopeDegrees { get; }
        public float MaximumStepUp { get; }
        public float MaximumStepDown { get; }
        public float MaximumHeightDiscontinuity { get; }
        public float MaximumEdgeGap { get; }
        public float MinimumLandingConfidence { get; }
        public float MaximumPredictionReachRatio { get; }

        public void RequireValid()
        {
            CharacterLyraCurrentGroundingSettings.RequirePositive(PathSphereRadius, nameof(PathSphereRadius));
            CharacterLyraCurrentGroundingSettings.RequirePositive(SwingCapsuleRadius, nameof(SwingCapsuleRadius));
            CharacterLyraCurrentGroundingSettings.RequirePositive(CastAbove, nameof(CastAbove));
            CharacterLyraCurrentGroundingSettings.RequirePositive(CastBelow, nameof(CastBelow));
            CharacterLyraCurrentGroundingSettings.RequireRange(MaximumSlopeDegrees, 0f, 89f, nameof(MaximumSlopeDegrees));
            CharacterLyraCurrentGroundingSettings.RequireNonNegative(MaximumStepUp, nameof(MaximumStepUp));
            CharacterLyraCurrentGroundingSettings.RequireNonNegative(MaximumStepDown, nameof(MaximumStepDown));
            CharacterLyraCurrentGroundingSettings.RequireNonNegative(MaximumHeightDiscontinuity, nameof(MaximumHeightDiscontinuity));
            CharacterLyraCurrentGroundingSettings.RequireNonNegative(MaximumEdgeGap, nameof(MaximumEdgeGap));
            CharacterLyraCurrentGroundingSettings.RequireRange(MinimumLandingConfidence, 0f, 1f, nameof(MinimumLandingConfidence));
            CharacterLyraCurrentGroundingSettings.RequireRange(MaximumPredictionReachRatio, 0.5f, 1.25f, nameof(MaximumPredictionReachRatio));
        }
    }
}
