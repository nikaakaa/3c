using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed class CharacterMotionMatchingPoseSourceBinding : CharacterPresentationPoseSourceBinding
    {
        [SerializeField] CharacterMotionMatchingProfile m_Profile;
        [SerializeField] CharacterAnimationRigDefinition m_Rig;
        [SerializeField] string m_FootAnalysisIdentity = string.Empty;
        [SerializeField] string m_SearchDomainId = string.Empty;
        [SerializeField] CharacterMotionMatchingDatabaseDefinition[] m_Databases = Array.Empty<CharacterMotionMatchingDatabaseDefinition>();

        public override PresentationPoseSourceKind SourceKind => PresentationPoseSourceKind.MotionMatching;
        public override UnityEngine.Object SourceAsset => m_Profile;
        public override CharacterAnimationRigDefinition Rig => m_Rig;
        public override string FootAnalysisIdentity => m_FootAnalysisIdentity ?? string.Empty;
        public CharacterMotionMatchingProfile Profile => m_Profile;
        public CharacterMotionMatchingSearchDomainId SearchDomainId =>
            string.IsNullOrWhiteSpace(m_SearchDomainId)
                ? default
                : new CharacterMotionMatchingSearchDomainId(m_SearchDomainId);
        public IReadOnlyList<CharacterMotionMatchingDatabaseDefinition> Databases =>
            m_Databases ?? Array.Empty<CharacterMotionMatchingDatabaseDefinition>();

        public void Configure(
            CharacterMotionMatchingPoseSourceSlot slot,
            CharacterMotionMatchingProfile profile,
            CharacterAnimationRigDefinition rig,
            CharacterMotionMatchingSearchDomainId searchDomainId,
            CharacterMotionMatchingDatabaseDefinition[] databases,
            string footAnalysisIdentity)
        {
            if (!profile || !searchDomainId.IsValid || databases == null || databases.Length == 0)
                throw new ArgumentException("Motion Matching Pose source binding is incomplete.");
            ConfigureCommon(slot);
            m_Profile = profile;
            m_Rig = rig;
            m_FootAnalysisIdentity = footAnalysisIdentity?.Trim() ?? string.Empty;
            m_SearchDomainId = searchDomainId.Value;
            m_Databases = databases;
            RequireValid(rig);
        }

        public override void RequireValid(CharacterAnimationRigDefinition profileRig)
        {
            base.RequireValid(profileRig);
            if (!m_Profile || !SearchDomainId.IsValid || Databases.Count == 0)
                throw new InvalidOperationException($"Motion Matching Pose source binding '{name}' is invalid.");
            m_Profile.RequireRigClosure(profileRig);
            var databaseIds = new HashSet<CharacterMotionMatchingDatabaseId>();
            for (int i = 0; i < Databases.Count; i++)
            {
                CharacterMotionMatchingDatabaseDefinition database = Databases[i];
                if (!database || !m_Profile.ContainsDatabase(database) || !database.SearchDomainId.Equals(SearchDomainId) || !databaseIds.Add(database.DatabaseId))
                    throw new InvalidOperationException($"Motion Matching Pose source binding '{name}' database #{i} is invalid.");
            }
        }
    }
}
