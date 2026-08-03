using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;

namespace ThirdPersonCharacter.Editor.MotionMatching
{
    public enum CharacterMotionMatchingAuthoringDiagnosticCode : ushort
    {
        InvalidProfile = 1,
        InvalidFeatureSchema = 2,
        InvalidTrajectoryPolicy = 3,
        InvalidCostProfile = 4,
        InvalidSearchPolicy = 5,
        InvalidDatabase = 6,
        InvalidSourceSet = 7,
        OrphanSourceClip = 8,
        CoverageConflict = 9,
        MissingAnalysisSource = 10,
        MissingFootArtifact = 11,
        StaleFootArtifact = 12,
        InvalidFootArtifact = 13,
        SamplingRigMismatch = 14,
        UnsupportedClipRig = 15,
        InputChanged = 16,
        OrphanMotionMatchingProfile = 17,
        MissingMotionMatchingProfile = 18,
        DuplicateMotionMatchingProfileOwner = 19
    }

    public readonly struct CharacterMotionMatchingAuthoringDiagnostic
    {
        public CharacterMotionMatchingAuthoringDiagnostic(CharacterMotionMatchingAuthoringDiagnosticCode code, string owner, string message)
        {
            Code = code;
            Owner = owner ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public CharacterMotionMatchingAuthoringDiagnosticCode Code { get; }
        public string Owner { get; }
        public string Message { get; }
    }

    public static class CharacterMotionMatchingAuthoringValidator
    {
        public static void CollectPresentationOwnershipDiagnostics(
            CharacterAnimationPresentationProfile profile,
            IReadOnlyList<CharacterAnimationPresentationProfile> projectProfiles,
            List<CharacterMotionMatchingAuthoringDiagnostic> diagnostics)
        {
            if (!profile || diagnostics == null)
                return;
            bool hasMotionMatchingBinding = false;
            for (int i = 0; i < profile.PoseSourceBindings.Count; i++)
            {
                if (profile.PoseSourceBindings[i] is CharacterMotionMatchingPoseSourceBinding binding &&
                    binding.Profile == profile.MotionMatchingProfile)
                {
                    hasMotionMatchingBinding = true;
                    break;
                }
            }
            if (profile.MotionMatchingProfile && !hasMotionMatchingBinding)
            {
                diagnostics.Add(new CharacterMotionMatchingAuthoringDiagnostic(
                    CharacterMotionMatchingAuthoringDiagnosticCode.OrphanMotionMatchingProfile,
                    profile.name,
                    "Motion Matching Profile is configured but has no Pose source provider binding."));
            }
            if (!profile.MotionMatchingProfile || projectProfiles == null)
                return;
            int ownerCount = 0;
            for (int i = 0; i < projectProfiles.Count; i++)
            {
                CharacterAnimationPresentationProfile candidate = projectProfiles[i];
                if (candidate && candidate.MotionMatchingProfile == profile.MotionMatchingProfile)
                    ownerCount++;
            }
            if (ownerCount > 1)
            {
                diagnostics.Add(new CharacterMotionMatchingAuthoringDiagnostic(
                    CharacterMotionMatchingAuthoringDiagnosticCode.DuplicateMotionMatchingProfileOwner,
                    profile.MotionMatchingProfile.name,
                    $"Motion Matching Profile resolves to {ownerCount} Animation Presentation Profile owners."));
            }
        }

        public static void RequireProfile(CharacterMotionMatchingProfile profile)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            profile.RequireValid();
            for (int databaseIndex = 0; databaseIndex < profile.Databases.Count; databaseIndex++)
                RequireDatabase(profile, profile.Databases[databaseIndex]);
        }

        public static void RequireDatabase(
            CharacterMotionMatchingProfile profile,
            CharacterMotionMatchingDatabaseDefinition database)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            if (!database)
                throw new ArgumentNullException(nameof(database));
            profile.FeatureSchema.RequireValid();
            profile.TrajectoryPolicy.RequireValid();
            profile.CostProfile.RequireValid();
            profile.SearchPolicy.RequireValid();
            database.RequireValid();
            if (database.FeatureSchema != profile.FeatureSchema)
                throw new InvalidOperationException($"Database '{database.name}' does not use Profile '{profile.name}' Feature Schema.");
            RequireTrajectoryAlignment(profile.FeatureSchema, profile.TrajectoryPolicy);
            RequireCoverageIntervals(database.CoverageRequirements);
            RequireSourceClipClosure(database);
        }

        static void RequireTrajectoryAlignment(
            CharacterMotionMatchingFeatureSchema schema,
            CharacterMotionMatchingTrajectoryPolicy policy)
        {
            var schemaFuture = new List<float>();
            for (int i = 0; i < schema.TrajectoryHorizons.Count; i++)
            {
                float time = schema.TrajectoryHorizons[i].TimeOffset;
                if (time >= 0f)
                    schemaFuture.Add(time);
            }
            if (schemaFuture.Count != policy.Points.Count)
                throw new InvalidOperationException("Feature Schema future horizons and Trajectory Policy points have different counts.");
            for (int i = 0; i < schemaFuture.Count; i++)
            {
                if (schemaFuture[i] != policy.Points[i].TimeOffset)
                    throw new InvalidOperationException($"Feature Schema horizon #{i} does not exactly match Trajectory Policy point #{i}.");
            }
        }

        static void RequireCoverageIntervals(IReadOnlyList<CharacterMotionMatchingCoverageRequirement> requirements)
        {
            for (int i = 0; i < requirements.Count; i++)
            {
                CharacterMotionMatchingCoverageRequirement left = requirements[i];
                for (int j = i + 1; j < requirements.Count; j++)
                {
                    CharacterMotionMatchingCoverageRequirement right = requirements[j];
                    bool speedOverlap = left.MinimumSpeed < right.MaximumSpeed && right.MinimumSpeed < left.MaximumSpeed;
                    bool facingOverlap = left.MinimumFacingChangeDegrees < right.MaximumFacingChangeDegrees && right.MinimumFacingChangeDegrees < left.MaximumFacingChangeDegrees;
                    if (speedOverlap && facingOverlap && ContactsOverlap(left, right))
                        throw new InvalidOperationException($"Coverage Requirements '{left.RequirementId}' and '{right.RequirementId}' overlap ambiguously.");
                }
            }
        }

        static bool ContactsOverlap(CharacterMotionMatchingCoverageRequirement left, CharacterMotionMatchingCoverageRequirement right)
        {
            for (int i = 0; i < left.RequiredContactCombinations.Count; i++)
            {
                for (int j = 0; j < right.RequiredContactCombinations.Count; j++)
                {
                    if (left.RequiredContactCombinations[i] == right.RequiredContactCombinations[j])
                        return true;
                }
            }
            return false;
        }

        static void RequireSourceClipClosure(CharacterMotionMatchingDatabaseDefinition database)
        {
            var sourceClips = new HashSet<CharacterMotionMatchingSourceClipId>();
            for (int sourceSetIndex = 0; sourceSetIndex < database.SourceSets.Count; sourceSetIndex++)
            {
                CharacterMotionMatchingSourceSet sourceSet = database.SourceSets[sourceSetIndex];
                sourceSet.RequireValid();
                for (int clipIndex = 0; clipIndex < sourceSet.SourceClips.Count; clipIndex++)
                    sourceClips.Add(sourceSet.SourceClips[clipIndex].SourceClipId);
            }
            var referenced = new HashSet<CharacterMotionMatchingSourceClipId>();
            for (int segmentIndex = 0; segmentIndex < database.Segments.Count; segmentIndex++)
                referenced.Add(database.Segments[segmentIndex].SourceClipId);
            foreach (CharacterMotionMatchingSourceClipId sourceClip in sourceClips)
            {
                if (!referenced.Contains(sourceClip))
                    throw new InvalidOperationException($"Database '{database.name}' SourceClipId '{sourceClip}' has no Segment and would be orphaned.");
            }
        }
    }
}
