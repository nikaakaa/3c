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

        internal CharacterFootLandingPredictionSettings Build() =>
            new CharacterFootLandingPredictionSettings(
                m_GroundLayerMask.value,
                m_HitCapacity,
                m_SphereRadius,
                m_CastAbove,
                m_CastBelow,
                m_MaximumSurfaceSlopeDegrees,
                m_MaximumPredictionTimeSeconds);

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
            float maximumPredictionTimeSeconds)
        {
            GroundLayerMask = groundLayerMask;
            HitCapacity = hitCapacity;
            SphereRadius = sphereRadius;
            CastAbove = castAbove;
            CastBelow = castBelow;
            MaximumSurfaceSlopeDegrees = maximumSurfaceSlopeDegrees;
            MaximumPredictionTimeSeconds = maximumPredictionTimeSeconds;
            RequireValid();
        }

        internal int GroundLayerMask { get; }
        internal int HitCapacity { get; }
        internal float SphereRadius { get; }
        internal float CastAbove { get; }
        internal float CastBelow { get; }
        internal float MaximumSurfaceSlopeDegrees { get; }
        internal float MaximumPredictionTimeSeconds { get; }
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
                MaximumPredictionTimeSeconds <= 0f)
            {
                throw new InvalidOperationException(
                    "Foot Landing Prediction settings are invalid.");
            }
        }
    }

    internal readonly struct CharacterLyraCurrentGroundingSettings
    {
        internal CharacterLyraCurrentGroundingSettings(
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

        internal int GroundLayerMask { get; }
        internal int HitCapacity { get; }
        internal float TraceAbove { get; }
        internal float TraceBelow { get; }
        internal float TraceRadius { get; }
        internal float HitNormalSpringStrength { get; }
        internal float HitNormalCriticalDamping { get; }
        internal float FootOffsetSpringStrength { get; }
        internal float FootOffsetCriticalDamping { get; }
        internal float FootOffsetTargetVelocityAmount { get; }
        internal float PelvisOffsetSpringStrength { get; }
        internal float PelvisOffsetCriticalDamping { get; }

        internal void RequireValid()
        {
            if (GroundLayerMask == 0 || HitCapacity < 4 || HitCapacity > 32 ||
                !Positive(TraceAbove) || !Positive(TraceBelow) || !Positive(TraceRadius) ||
                !Positive(HitNormalSpringStrength) || !Positive(HitNormalCriticalDamping) ||
                !Positive(FootOffsetSpringStrength) || !Positive(FootOffsetCriticalDamping) ||
                !float.IsFinite(FootOffsetTargetVelocityAmount) ||
                FootOffsetTargetVelocityAmount < 0f ||
                !Positive(PelvisOffsetSpringStrength) ||
                !Positive(PelvisOffsetCriticalDamping))
            {
                throw new InvalidOperationException(
                    "Lyra Current Grounding settings are invalid.");
            }
        }

        static bool Positive(float value) => float.IsFinite(value) && value > 0f;
    }

    [CreateAssetMenu(
        fileName = "CharacterFootPlacementProfile",
        menuName = "Third Person/Character/Pipeline/Presentation/Foot Placement Profile")]
    public sealed class CharacterFootPlacementProfile : ScriptableObject
    {
        public const string SchemaVersion = "character-foot-placement-profile/v16-landing-only";

        [SerializeField] string m_ProfileId = string.Empty;
        [SerializeField] CharacterFootLandingPredictionAuthoringSettings m_LandingPrediction =
            new CharacterFootLandingPredictionAuthoringSettings();

        public string ProfileId => RequireIdentity(m_ProfileId, nameof(m_ProfileId));
        public string Revision => ComputeRevision();
        public CharacterFootLandingPredictionAuthoringSettings LandingPrediction =>
            m_LandingPrediction ?? throw new InvalidOperationException(
                "Foot Placement Profile has no Landing Prediction settings.");

        public string ComputeRevision() => StableHash.Compute(
            SchemaVersion,
            ProfileId,
            JsonUtility.ToJson(m_LandingPrediction)).ToString();

        public void RequireValid()
        {
            _ = ProfileId;
            LandingPrediction.Build().RequireValid();
        }

        internal void ApplyTuning(
            string fieldPath,
            CharacterPoseTuningValue value)
        {
            const string prefix = "landing-prediction/";
            if (string.IsNullOrWhiteSpace(fieldPath) ||
                !fieldPath.StartsWith(prefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Foot Placement tuning field '{fieldPath}' is not declared.");
            }
            LandingPrediction.ApplyTuning(fieldPath.Substring(prefix.Length), value);
            RequireValid();
        }

        internal CharacterFootPlacementRuntimeSettings BuildSettings(
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
            return new CharacterFootPlacementRuntimeSettings(
                ProfileId,
                Revision,
                projection.PosePlan.PlanHash,
                LandingPrediction.Build());
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

    internal sealed class CharacterFootPlacementRuntimeSettings
    {
        internal CharacterFootPlacementRuntimeSettings(
            string profileId,
            string profileRevision,
            string posePlanHash,
            CharacterFootLandingPredictionSettings landingPrediction)
        {
            ProfileId = profileId;
            ProfileRevision = profileRevision;
            PosePlanHash = posePlanHash;
            LandingPrediction = landingPrediction;
            if (string.IsNullOrWhiteSpace(ProfileId) ||
                string.IsNullOrWhiteSpace(ProfileRevision) ||
                string.IsNullOrWhiteSpace(PosePlanHash))
            {
                throw new ArgumentException("Foot Placement runtime identity is invalid.");
            }
            LandingPrediction.RequireValid();
        }

        internal string ProfileId { get; }
        internal string ProfileRevision { get; }
        internal string PosePlanHash { get; }
        internal CharacterFootLandingPredictionSettings LandingPrediction { get; }
    }
}
