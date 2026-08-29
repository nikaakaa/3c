using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    [Serializable]
    public sealed class CharacterFootLandingPredictionAuthoringSettings
    {
        [SerializeField] LayerMask m_GroundLayerMask;
        [SerializeField] int m_HitCapacity = 16;
        [SerializeField] float m_SphereRadius = 0.08f;
        [SerializeField] float m_CastAbove = 0.35f;
        [SerializeField] float m_CastBelow = 0.75f;
        [SerializeField] float m_MaximumSurfaceSlopeDegrees = 55f;
        [SerializeField] float m_MaximumPredictionTimeSeconds = 2f;
        [SerializeField] float m_PredictionVelocityDeltaThreshold;
        [SerializeField] float m_PredictionVelocitySmoothSpeed;
        [SerializeField] float m_PredictionMaximumSpeed;
        [SerializeField] float m_PredictionInputAccumulationDistance;
        [SerializeField] float m_ComponentUpChangeAngleDegrees;

        internal CharacterFootLandingPredictionSettings Build() =>
            new CharacterFootLandingPredictionSettings(
                m_GroundLayerMask.value,
                m_HitCapacity,
                m_SphereRadius,
                m_CastAbove,
                m_CastBelow,
                m_MaximumSurfaceSlopeDegrees,
                m_MaximumPredictionTimeSeconds,
                m_PredictionVelocityDeltaThreshold,
                m_PredictionVelocitySmoothSpeed,
                m_PredictionMaximumSpeed,
                m_PredictionInputAccumulationDistance,
                m_ComponentUpChangeAngleDegrees);

        internal void ApplyTuning(string fieldPath, CharacterPoseTuningValue value)
        {
            if (!string.Equals(fieldPath, "hit-capacity", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Landing Prediction tuning field '{fieldPath}' is not declared.");
            m_HitCapacity = value.IntegerValue;
            Build().RequireValid();
        }
    }

    internal readonly struct CharacterFootLandingPredictionSettings
    {
        internal CharacterFootLandingPredictionSettings(
            int groundLayerMask,
            int hitCapacity,
            float sphereRadius,
            float castAbove,
            float castBelow,
            float maximumSurfaceSlopeDegrees,
            float maximumPredictionTimeSeconds,
            float predictionVelocityDeltaThreshold,
            float predictionVelocitySmoothSpeed,
            float predictionMaximumSpeed,
            float predictionInputAccumulationDistance,
            float componentUpChangeAngleDegrees)
        {
            GroundLayerMask = groundLayerMask;
            HitCapacity = hitCapacity;
            SphereRadius = sphereRadius;
            CastAbove = castAbove;
            CastBelow = castBelow;
            MaximumSurfaceSlopeDegrees = maximumSurfaceSlopeDegrees;
            MaximumPredictionTimeSeconds = maximumPredictionTimeSeconds;
            PredictionVelocityDeltaThreshold = predictionVelocityDeltaThreshold;
            PredictionVelocitySmoothSpeed = predictionVelocitySmoothSpeed;
            PredictionMaximumSpeed = predictionMaximumSpeed;
            PredictionInputAccumulationDistance =
                predictionInputAccumulationDistance;
            ComponentUpChangeAngleDegrees = componentUpChangeAngleDegrees;
            RequireValid();
        }

        internal int GroundLayerMask { get; }
        internal int HitCapacity { get; }
        internal float SphereRadius { get; }
        internal float CastAbove { get; }
        internal float CastBelow { get; }
        internal float MaximumSurfaceSlopeDegrees { get; }
        internal float MaximumPredictionTimeSeconds { get; }
        internal float PredictionVelocityDeltaThreshold { get; }
        internal float PredictionVelocitySmoothSpeed { get; }
        internal float PredictionMaximumSpeed { get; }
        internal float PredictionInputAccumulationDistance { get; }
        internal float ComponentUpChangeAngleDegrees { get; }
        internal float MinimumGroundNormalDot =>
            Mathf.Cos(MaximumSurfaceSlopeDegrees * Mathf.Deg2Rad);

        internal void RequireValid()
        {
            if (GroundLayerMask == 0 || HitCapacity < 4 || HitCapacity > 32 ||
                !float.IsFinite(SphereRadius) || SphereRadius <= 0f ||
                !float.IsFinite(CastAbove) || CastAbove <= 0f ||
                !float.IsFinite(CastBelow) || CastBelow <= 0f ||
                !float.IsFinite(MaximumSurfaceSlopeDegrees) ||
                MaximumSurfaceSlopeDegrees <= 0f || MaximumSurfaceSlopeDegrees >= 90f ||
                !float.IsFinite(MaximumPredictionTimeSeconds) ||
                MaximumPredictionTimeSeconds <= 0f ||
                !float.IsFinite(PredictionVelocityDeltaThreshold) ||
                PredictionVelocityDeltaThreshold <= 0f ||
                !float.IsFinite(PredictionVelocitySmoothSpeed) ||
                PredictionVelocitySmoothSpeed <= 0f ||
                !float.IsFinite(PredictionMaximumSpeed) ||
                PredictionMaximumSpeed <= PredictionVelocityDeltaThreshold ||
                !float.IsFinite(PredictionInputAccumulationDistance) ||
                PredictionInputAccumulationDistance <= 0f ||
                PredictionInputAccumulationDistance > SphereRadius ||
                !float.IsFinite(ComponentUpChangeAngleDegrees) ||
                ComponentUpChangeAngleDegrees <= 0f ||
                ComponentUpChangeAngleDegrees >= 90f)
            {
                throw new InvalidOperationException(
                    "Foot Landing Prediction settings are invalid.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterFootGroundDetectionAuthoringSettings
    {
        [SerializeField] LayerMask m_GroundLayerMask;
        [SerializeField] int m_SegmentHitCapacity = 16;
        [SerializeField] int m_ContactCapacity = 64;
        [SerializeField] float m_CapsuleRadius = 0.1f;
        [SerializeField] float m_MaximumAxisSegmentLength = 0.18f;
        [SerializeField] float m_CastAbove = 0.45f;
        [SerializeField] float m_CastBelow = 0.85f;
        [SerializeField] float m_MaximumReachableVerticalEdge = 0.3f;

        internal CharacterFootGroundDetectionSettings Build() =>
            new CharacterFootGroundDetectionSettings(
                m_GroundLayerMask.value,
                m_SegmentHitCapacity,
                m_ContactCapacity,
                m_CapsuleRadius,
                m_MaximumAxisSegmentLength,
                m_CastAbove,
                m_CastBelow,
                m_MaximumReachableVerticalEdge);

        internal void ApplyTuning(string fieldPath, CharacterPoseTuningValue value)
        {
            if (!string.Equals(fieldPath, "segment-hit-capacity", StringComparison.Ordinal) &&
                !string.Equals(fieldPath, "contact-capacity", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Ground Detection tuning field '{fieldPath}' is not declared.");
            if (string.Equals(fieldPath, "segment-hit-capacity", StringComparison.Ordinal))
                m_SegmentHitCapacity = value.IntegerValue;
            else
                m_ContactCapacity = value.IntegerValue;
            Build().RequireValid();
        }
    }

    internal readonly struct CharacterFootGroundDetectionSettings
    {
        internal CharacterFootGroundDetectionSettings(
            int groundLayerMask,
            int segmentHitCapacity,
            int contactCapacity,
            float capsuleRadius,
            float maximumAxisSegmentLength,
            float castAbove,
            float castBelow,
            float maximumReachableVerticalEdge)
        {
            GroundLayerMask = groundLayerMask;
            SegmentHitCapacity = segmentHitCapacity;
            ContactCapacity = contactCapacity;
            CapsuleRadius = capsuleRadius;
            MaximumAxisSegmentLength = maximumAxisSegmentLength;
            CastAbove = castAbove;
            CastBelow = castBelow;
            MaximumReachableVerticalEdge = maximumReachableVerticalEdge;
            RequireValid();
        }

        internal int GroundLayerMask { get; }
        internal int SegmentHitCapacity { get; }
        internal int ContactCapacity { get; }
        internal float CapsuleRadius { get; }
        internal float MaximumAxisSegmentLength { get; }
        internal float CastAbove { get; }
        internal float CastBelow { get; }
        internal float MaximumReachableVerticalEdge { get; }

        internal void RequireValid()
        {
            if (GroundLayerMask == 0 || SegmentHitCapacity < 4 || SegmentHitCapacity > 32 ||
                ContactCapacity < 4 || ContactCapacity > 64 ||
                !float.IsFinite(CapsuleRadius) || CapsuleRadius <= 0f ||
                !float.IsFinite(MaximumAxisSegmentLength) ||
                MaximumAxisSegmentLength <= 0f ||
                !float.IsFinite(CastAbove) || CastAbove <= CapsuleRadius ||
                !float.IsFinite(CastBelow) || CastBelow <= 0f ||
                !float.IsFinite(MaximumReachableVerticalEdge) ||
                MaximumReachableVerticalEdge <= 0f)
            {
                throw new InvalidOperationException(
                    "Foot Ground Detection settings are invalid.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterFootMotionAuthoringSettings
    {
        [SerializeField] float m_LandingAcceptanceDistance;
        [SerializeField] float m_PathRevisionDistance;
        [SerializeField] float m_SwingResidualTolerance;
        [SerializeField] float m_ReleaseCompletionTolerance;
        [SerializeField] float m_EffectiveCorrectionHalfLifeSeconds = 0.03f;
        [SerializeField] float m_MaximumVerticalCorrectionSpeed;
        [SerializeField] float m_GroundPenetrationTolerance;
        [SerializeField] float m_LandingLockCompletionTolerance;
        [SerializeField] float m_LockDistance = 0.08f;
        [SerializeField] float m_SlideDistance = 0.2f;
        [SerializeField] float m_PelvisSpringFrequency = 3f;
        [SerializeField] float m_MinimumLandingLegCompressionReserve = 0.02f;

        internal CharacterFootMotionSettings Build() =>
            new CharacterFootMotionSettings(
                m_LandingAcceptanceDistance,
                m_PathRevisionDistance,
                m_SwingResidualTolerance,
                m_ReleaseCompletionTolerance,
                m_EffectiveCorrectionHalfLifeSeconds,
                m_MaximumVerticalCorrectionSpeed,
                m_GroundPenetrationTolerance,
                m_LandingLockCompletionTolerance,
                m_LockDistance,
                m_SlideDistance,
                m_PelvisSpringFrequency,
                m_MinimumLandingLegCompressionReserve);
    }

    internal readonly struct CharacterFootMotionSettings
    {
        internal CharacterFootMotionSettings(
            float landingAcceptanceDistance,
            float pathRevisionDistance,
            float swingResidualTolerance,
            float releaseCompletionTolerance,
            float effectiveCorrectionHalfLifeSeconds,
            float maximumVerticalCorrectionSpeed,
            float groundPenetrationTolerance,
            float landingLockCompletionTolerance,
            float lockDistance,
            float slideDistance,
            float pelvisSpringFrequency,
            float minimumLandingLegCompressionReserve)
        {
            LandingAcceptanceDistance = landingAcceptanceDistance;
            PathRevisionDistance = pathRevisionDistance;
            SwingResidualTolerance = swingResidualTolerance;
            ReleaseCompletionTolerance = releaseCompletionTolerance;
            EffectiveCorrectionHalfLifeSeconds = effectiveCorrectionHalfLifeSeconds;
            MaximumVerticalCorrectionSpeed = maximumVerticalCorrectionSpeed;
            GroundPenetrationTolerance = groundPenetrationTolerance;
            LandingLockCompletionTolerance = landingLockCompletionTolerance;
            LockDistance = lockDistance;
            SlideDistance = slideDistance;
            PelvisSpringFrequency = pelvisSpringFrequency;
            MinimumLandingLegCompressionReserve =
                minimumLandingLegCompressionReserve;
            RequireValid();
        }

        internal float LandingAcceptanceDistance { get; }
        internal float PathRevisionDistance { get; }
        internal float SwingResidualTolerance { get; }
        internal float ReleaseCompletionTolerance { get; }
        internal float EffectiveCorrectionHalfLifeSeconds { get; }
        internal float MaximumVerticalCorrectionSpeed { get; }
        internal float GroundPenetrationTolerance { get; }
        internal float LandingLockCompletionTolerance { get; }
        internal float LockDistance { get; }
        internal float SlideDistance { get; }
        internal float PelvisSpringFrequency { get; }
        internal float MinimumLandingLegCompressionReserve { get; }

        internal void RequireValid()
        {
            if (!float.IsFinite(LandingAcceptanceDistance) ||
                LandingAcceptanceDistance <= 0f ||
                !float.IsFinite(PathRevisionDistance) ||
                PathRevisionDistance <= 0f ||
                !float.IsFinite(SwingResidualTolerance) ||
                SwingResidualTolerance <= 0f ||
                !float.IsFinite(ReleaseCompletionTolerance) ||
                ReleaseCompletionTolerance <= 0f ||
                !float.IsFinite(EffectiveCorrectionHalfLifeSeconds) ||
                EffectiveCorrectionHalfLifeSeconds <= 0f ||
                !float.IsFinite(MaximumVerticalCorrectionSpeed) ||
                MaximumVerticalCorrectionSpeed <= 0f ||
                !float.IsFinite(GroundPenetrationTolerance) ||
                GroundPenetrationTolerance <= 0f ||
                !float.IsFinite(LandingLockCompletionTolerance) ||
                LandingLockCompletionTolerance <= 0f ||
                !float.IsFinite(LockDistance) ||
                LockDistance <= LandingAcceptanceDistance ||
                LockDistance <= PathRevisionDistance ||
                LockDistance <= SwingResidualTolerance ||
                LockDistance <= ReleaseCompletionTolerance ||
                LockDistance <= GroundPenetrationTolerance ||
                LockDistance <= LandingLockCompletionTolerance ||
                !float.IsFinite(SlideDistance) ||
                SlideDistance <= LockDistance ||
                !float.IsFinite(PelvisSpringFrequency) || PelvisSpringFrequency <= 0f ||
                !float.IsFinite(MinimumLandingLegCompressionReserve) ||
                MinimumLandingLegCompressionReserve <= 0f)
            {
                throw new InvalidOperationException(
                    "Foot Motion settings are invalid.");
            }
        }
    }

    [CreateAssetMenu(
        fileName = "CharacterFootPlacementProfile",
        menuName = "Third Person/Character/Pipeline/Presentation/Foot Placement Profile")]
    public sealed class CharacterFootPlacementProfile : ScriptableObject
    {
        public const string SchemaVersion = "character-foot-placement-profile/v30-ground-continuity";

        [SerializeField] string m_ProfileId = string.Empty;
        [SerializeField] CharacterFootLandingPredictionAuthoringSettings m_LandingPrediction =
            new CharacterFootLandingPredictionAuthoringSettings();
        [SerializeField] CharacterFootGroundDetectionAuthoringSettings m_GroundDetection =
            new CharacterFootGroundDetectionAuthoringSettings();
        [SerializeField] CharacterFootMotionAuthoringSettings m_FootMotion =
            new CharacterFootMotionAuthoringSettings();

        public string ProfileId => RequireIdentity(m_ProfileId, nameof(m_ProfileId));
        public string Revision => ComputeRevision();
        public CharacterFootLandingPredictionAuthoringSettings LandingPrediction =>
            m_LandingPrediction ?? throw new InvalidOperationException(
                "Foot Placement Profile has no Landing Prediction settings.");
        public CharacterFootGroundDetectionAuthoringSettings GroundDetection =>
            m_GroundDetection ?? throw new InvalidOperationException(
                "Foot Placement Profile has no Ground Detection settings.");
        public CharacterFootMotionAuthoringSettings FootMotion =>
            m_FootMotion ?? throw new InvalidOperationException(
                "Foot Placement Profile has no Foot Motion settings.");

        public string ComputeRevision() => StableHash.Compute(
            SchemaVersion,
            ProfileId,
            JsonUtility.ToJson(m_LandingPrediction),
            JsonUtility.ToJson(m_GroundDetection),
            JsonUtility.ToJson(m_FootMotion)).ToString();

        public void RequireValid()
        {
            _ = ProfileId;
            LandingPrediction.Build().RequireValid();
            GroundDetection.Build().RequireValid();
            FootMotion.Build().RequireValid();
        }

        internal void ApplyTuning(
            string fieldPath,
            CharacterPoseTuningValue value)
        {
            const string landingPrefix = "landing-prediction/";
            const string groundPrefix = "ground-detection/";
            if (fieldPath != null && fieldPath.StartsWith(landingPrefix, StringComparison.Ordinal))
                LandingPrediction.ApplyTuning(fieldPath.Substring(landingPrefix.Length), value);
            else if (fieldPath != null && fieldPath.StartsWith(groundPrefix, StringComparison.Ordinal))
                GroundDetection.ApplyTuning(fieldPath.Substring(groundPrefix.Length), value);
            else
                throw new InvalidOperationException(
                    $"Foot Placement tuning field '{fieldPath}' is not declared.");
            RequireValid();
        }

        internal CharacterFootPlacementModuleSettings BuildSettings(
            CharacterPresentationProjection projection,
            CharacterFootPlacementPoseRig rig)
        {
            if (projection == null || rig == null)
                throw new ArgumentNullException(projection == null ? nameof(projection) : nameof(rig));
            RequireValid();
            projection.RequirePosePayload();
            rig.RequireValid();
            if (!string.Equals(projection.Rig.RigId, rig.Rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(projection.Rig.RigRevision, rig.Rig.RigRevision, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Foot Placement Profile build Rig identity is stale.");
            }
            return new CharacterFootPlacementModuleSettings(
                ProfileId,
                Revision,
                projection.PosePlan.PlanHash,
                LandingPrediction.Build(),
                GroundDetection.Build(),
                FootMotion.Build());
        }

        static string RequireIdentity(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Foot Placement Profile requires stable identity '{field}'.");
            }
            return value;
        }
    }

    internal sealed class CharacterFootPlacementModuleSettings
    {
        internal CharacterFootPlacementModuleSettings(
            string profileId,
            string profileRevision,
            string posePlanHash,
            CharacterFootLandingPredictionSettings landingPrediction,
            CharacterFootGroundDetectionSettings groundDetection,
            CharacterFootMotionSettings footMotion)
        {
            ProfileId = profileId;
            ProfileRevision = profileRevision;
            PosePlanHash = posePlanHash;
            LandingPrediction = landingPrediction;
            GroundDetection = groundDetection;
            FootMotion = footMotion;
            if (string.IsNullOrWhiteSpace(ProfileId) ||
                string.IsNullOrWhiteSpace(ProfileRevision) ||
                string.IsNullOrWhiteSpace(PosePlanHash))
            {
                throw new ArgumentException("Foot Placement runtime identity is invalid.");
            }
            LandingPrediction.RequireValid();
            GroundDetection.RequireValid();
            FootMotion.RequireValid();
        }

        internal string ProfileId { get; }
        internal string ProfileRevision { get; }
        internal string PosePlanHash { get; }
        internal CharacterFootLandingPredictionSettings LandingPrediction { get; }
        internal CharacterFootGroundDetectionSettings GroundDetection { get; }
        internal CharacterFootMotionSettings FootMotion { get; }
    }
}
