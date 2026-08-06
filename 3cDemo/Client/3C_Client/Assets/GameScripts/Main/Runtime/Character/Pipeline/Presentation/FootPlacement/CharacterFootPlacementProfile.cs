using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    [CreateAssetMenu(
        fileName = "CharacterFootPlacementProfile",
        menuName = "Third Person/Character/Pipeline/Presentation/Foot Placement Profile")]
    public sealed class CharacterFootPlacementProfile : ScriptableObject
    {
        public const string SchemaVersion = "character-foot-placement-profile/v11";

        [SerializeField] string m_ProfileId = string.Empty;
        [SerializeField] string m_Revision = string.Empty;
        [SerializeField] CharacterFinalIkGroundingAuthoringSettings m_FinalIkGrounding = new CharacterFinalIkGroundingAuthoringSettings();
        [SerializeField] CharacterPredictiveFootPlacementAuthoringSettings m_PredictiveExtension = new CharacterPredictiveFootPlacementAuthoringSettings();

        public string ProfileId => RequireIdentity(m_ProfileId, nameof(m_ProfileId));
        public string Revision => RequireIdentity(m_Revision, nameof(m_Revision));
        public CharacterFinalIkGroundingAuthoringSettings FinalIkGrounding =>
            m_FinalIkGrounding ?? throw new InvalidOperationException("Foot Placement Profile has no FinalIK Grounding settings.");
        public CharacterPredictiveFootPlacementAuthoringSettings PredictiveExtension =>
            m_PredictiveExtension ?? throw new InvalidOperationException("Foot Placement Profile has no Predictive Extension settings.");

        void OnValidate()
        {
            m_ProfileId = m_ProfileId?.Trim() ?? string.Empty;
            m_Revision = string.IsNullOrEmpty(m_ProfileId)
                ? string.Empty
                : ComputeRevision();
        }

        internal void ApplyTuning(
            string fieldPath,
            CharacterPoseTuningValue value)
        {
            if (string.IsNullOrWhiteSpace(fieldPath))
                throw new ArgumentException("Foot Placement tuning field is missing.", nameof(fieldPath));
            if (fieldPath.StartsWith("grounding/", StringComparison.Ordinal))
                FinalIkGrounding.ApplyTuning(fieldPath.Substring("grounding/".Length), value);
            else if (fieldPath.StartsWith("predictive/", StringComparison.Ordinal))
                PredictiveExtension.ApplyTuning(fieldPath.Substring("predictive/".Length), value);
            else
                throw new InvalidOperationException($"Foot Placement tuning field '{fieldPath}' is not declared.");
            m_Revision = ComputeRevision();
            RequireValid();
        }

        public string ComputeRevision() => StableHash.Compute(
            SchemaVersion,
            ProfileId,
            JsonUtility.ToJson(FinalIkGrounding),
            JsonUtility.ToJson(PredictiveExtension)).ToString();

        public void RequireValid()
        {
            _ = ProfileId;
            string revision = Revision;
            string computedRevision = ComputeRevision();
            if (!string.Equals(revision, computedRevision, StringComparison.Ordinal))
                throw new InvalidOperationException($"Foot Placement Profile revision is stale: {revision}/{computedRevision}.");
            _ = FinalIkGrounding.Build();
            _ = PredictiveExtension.Build();
        }

        public CharacterFootPlacementRuntimeSettings BuildSettings(
            CharacterPresentationProjection projection,
            CharacterFootPlacementPoseRig rig)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            RequireValid();
            projection.RequirePosePayload();
            projection.RequireTuningPayload();
            rig.RequireValid();
            if (!string.Equals(projection.Rig.RigId, rig.Rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(projection.Rig.RigRevision, rig.Rig.RigRevision, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Foot Placement Profile build Rig identity is stale.");
            }
            return new CharacterFootPlacementRuntimeSettings(
                ProfileId,
                Revision,
                projection.PosePlan.PlanHash,
                FinalIkGrounding.Build(),
                PredictiveExtension.Build());
        }

        static string RequireIdentity(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Foot Placement Profile requires stable identity '{field}'.");
            }
            return value;
        }
    }

    public sealed class CharacterFootPlacementRuntimeSettings
    {
        internal CharacterFootPlacementRuntimeSettings(
            string profileId,
            string profileRevision,
            string posePlanHash,
            CharacterFinalIkGroundingSettings grounding,
            CharacterPredictiveFootPlacementRuntimeSettings predictive)
        {
            ProfileId = RequireIdentity(profileId, nameof(profileId));
            ProfileRevision = RequireIdentity(profileRevision, nameof(profileRevision));
            PosePlanHash = RequireIdentity(posePlanHash, nameof(posePlanHash));
            grounding.RequireValid();
            predictive.RequireValid();
            Grounding = grounding;
            Predictive = predictive;
        }

        public string ProfileId { get; }
        public string ProfileRevision { get; }
        public string PosePlanHash { get; }
        public CharacterFinalIkGroundingSettings Grounding { get; private set; }
        public CharacterPredictiveFootPlacementRuntimeSettings Predictive { get; private set; }

        internal void ApplyTuning(
            CharacterFinalIkGroundingSettings grounding,
            CharacterPredictiveFootPlacementRuntimeSettings predictive)
        {
            grounding.RequireValid();
            predictive.RequireValid();
            if (predictive.HitCapacity != Predictive.HitCapacity ||
                predictive.PathSampleCount != Predictive.PathSampleCount)
                throw new InvalidOperationException("Foot Placement tuning cannot change published workspace capacity.");
            Grounding = grounding;
            Predictive = predictive;
        }

        static string RequireIdentity(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Foot Placement runtime requires stable identity '{field}'.");
            }
            return value;
        }
    }
}
