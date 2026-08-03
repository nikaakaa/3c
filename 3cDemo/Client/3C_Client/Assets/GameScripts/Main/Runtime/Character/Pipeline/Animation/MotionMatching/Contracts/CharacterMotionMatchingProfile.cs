using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    [CreateAssetMenu(fileName = "CharacterMotionMatchingProfile", menuName = "3C/Character/Motion Matching/Profile")]
    public sealed class CharacterMotionMatchingProfile : ScriptableObject
    {
        public const string SchemaVersion = "character-motion-matching-profile/v3";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_ProfileId = string.Empty;
        [SerializeField] int m_Revision;
        [SerializeField] CharacterMotionMatchingFeatureSchema m_FeatureSchema;
        [SerializeField] CharacterMotionMatchingTrajectoryPolicy m_TrajectoryPolicy;
        [SerializeField] CharacterMotionMatchingCostProfile m_CostProfile;
        [SerializeField] CharacterMotionMatchingSearchPolicy m_SearchPolicy;
        [SerializeField] CharacterMotionMatchingDatabaseDefinition[] m_Databases = Array.Empty<CharacterMotionMatchingDatabaseDefinition>();

        public string Schema => m_Schema ?? string.Empty;
        public CharacterMotionMatchingProfileId ProfileId => string.IsNullOrWhiteSpace(m_ProfileId) ? default : new CharacterMotionMatchingProfileId(m_ProfileId);
        public int Revision => m_Revision;
        public CharacterMotionMatchingFeatureSchema FeatureSchema => m_FeatureSchema;
        public CharacterMotionMatchingTrajectoryPolicy TrajectoryPolicy => m_TrajectoryPolicy;
        public CharacterMotionMatchingCostProfile CostProfile => m_CostProfile;
        public CharacterMotionMatchingSearchPolicy SearchPolicy => m_SearchPolicy;
        public IReadOnlyList<CharacterMotionMatchingDatabaseDefinition> Databases => m_Databases ?? Array.Empty<CharacterMotionMatchingDatabaseDefinition>();

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) || !ProfileId.IsValid)
                throw new InvalidOperationException($"Motion Matching Profile '{name}' has an invalid schema or identity.");
            MotionMatchingAuthoringValidation.RequireRevision(Revision, nameof(Revision));
            if (!FeatureSchema || !TrajectoryPolicy || !CostProfile || !SearchPolicy)
                throw new InvalidOperationException($"Motion Matching Profile '{name}' has an incomplete policy closure.");
            FeatureSchema.RequireValid();
            TrajectoryPolicy.RequireValid();
            CostProfile.RequireValid();
            SearchPolicy.RequireValid();
            if (Databases.Count == 0)
                throw new InvalidOperationException($"Motion Matching Profile '{name}' has no Database.");

            var databaseIds = new HashSet<CharacterMotionMatchingDatabaseId>();
            for (int i = 0; i < Databases.Count; i++)
            {
                CharacterMotionMatchingDatabaseDefinition database = Databases[i];
                if (!database)
                    throw new InvalidOperationException($"Motion Matching Profile '{name}' Database #{i} is missing.");
                database.RequireValid();
                if (!databaseIds.Add(database.DatabaseId) || database.FeatureSchema != FeatureSchema)
                    throw new InvalidOperationException($"Motion Matching Profile '{name}' Database #{i} is duplicated or uses a different Feature Schema.");
            }
        }
    }
}
