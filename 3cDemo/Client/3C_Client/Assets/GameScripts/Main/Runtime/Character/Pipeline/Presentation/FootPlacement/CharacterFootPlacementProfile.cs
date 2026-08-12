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
        public const string SchemaVersion = "character-foot-placement-profile/v15";

        [SerializeField] string m_ProfileId = string.Empty;
        [SerializeField] string m_Revision = string.Empty;
        [SerializeField] CharacterLyraCurrentGroundingAuthoringSettings m_LyraCurrentGrounding = new CharacterLyraCurrentGroundingAuthoringSettings();
        [SerializeField] CharacterStanceStabilizationAuthoringSettings m_StanceStabilization = new CharacterStanceStabilizationAuthoringSettings();
        [SerializeField] CharacterPredictiveFootPlacementAuthoringSettings m_PredictiveExtension = new CharacterPredictiveFootPlacementAuthoringSettings();

        public string ProfileId => RequireIdentity(m_ProfileId, nameof(m_ProfileId));
        public string Revision => RequireIdentity(m_Revision, nameof(m_Revision));
        public CharacterLyraCurrentGroundingAuthoringSettings LyraCurrentGrounding =>
            m_LyraCurrentGrounding ?? throw new InvalidOperationException("Foot Placement Profile has no Lyra Current Grounding settings.");
        public CharacterStanceStabilizationAuthoringSettings StanceStabilization =>
            m_StanceStabilization ?? throw new InvalidOperationException("Foot Placement Profile has no Stance Stabilization settings.");
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
            if (fieldPath.StartsWith("lyra-current-grounding/", StringComparison.Ordinal))
                LyraCurrentGrounding.ApplyTuning(fieldPath.Substring("lyra-current-grounding/".Length), value);
            else if (fieldPath.StartsWith("stance-stabilization/", StringComparison.Ordinal))
                StanceStabilization.ApplyTuning(fieldPath.Substring("stance-stabilization/".Length), value);
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
            JsonUtility.ToJson(LyraCurrentGrounding),
            JsonUtility.ToJson(StanceStabilization),
            JsonUtility.ToJson(PredictiveExtension)).ToString();

        public void RequireValid()
        {
            _ = ProfileId;
            string revision = Revision;
            string computedRevision = ComputeRevision();
            if (!string.Equals(revision, computedRevision, StringComparison.Ordinal))
                throw new InvalidOperationException($"Foot Placement Profile revision is stale: {revision}/{computedRevision}.");
            _ = LyraCurrentGrounding.Build();
            _ = StanceStabilization.Build();
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
                LyraCurrentGrounding.Build(),
                StanceStabilization.Build(),
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
            CharacterLyraCurrentGroundingSettings currentGrounding,
            CharacterStanceStabilizationSettings stanceStabilization,
            CharacterPredictiveFootPlacementRuntimeSettings predictive)
        {
            ProfileId = RequireIdentity(profileId, nameof(profileId));
            ProfileRevision = RequireIdentity(profileRevision, nameof(profileRevision));
            PosePlanHash = RequireIdentity(posePlanHash, nameof(posePlanHash));
            currentGrounding.RequireValid();
            stanceStabilization.RequireValid();
            predictive.RequireValid();
            CurrentGrounding = currentGrounding;
            StanceStabilization = stanceStabilization;
            PredictiveExtension = predictive;
        }

        public string ProfileId { get; }
        public string ProfileRevision { get; }
        public string PosePlanHash { get; }
        public CharacterLyraCurrentGroundingSettings CurrentGrounding { get; private set; }
        public CharacterStanceStabilizationSettings StanceStabilization { get; private set; }
        public CharacterPredictiveFootPlacementRuntimeSettings PredictiveExtension { get; private set; }

        internal void ApplyTuning(
            CharacterLyraCurrentGroundingSettings currentGrounding,
            CharacterStanceStabilizationSettings stanceStabilization,
            CharacterPredictiveFootPlacementRuntimeSettings predictive)
        {
            currentGrounding.RequireValid();
            stanceStabilization.RequireValid();
            predictive.RequireValid();
            if (currentGrounding.HitCapacity != CurrentGrounding.HitCapacity)
                throw new InvalidOperationException("Foot Placement tuning cannot change published workspace capacity.");
            CurrentGrounding = currentGrounding;
            StanceStabilization = stanceStabilization;
            PredictiveExtension = predictive;
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
