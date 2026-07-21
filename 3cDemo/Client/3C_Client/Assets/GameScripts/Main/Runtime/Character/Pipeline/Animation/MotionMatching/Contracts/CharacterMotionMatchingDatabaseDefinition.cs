using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    [Serializable]
    public sealed class CharacterMotionMatchingSegmentDefinition
    {
        [SerializeField] string m_SegmentId = string.Empty;
        [SerializeField] string m_SourceClipId = string.Empty;
        [SerializeField] float m_StartTime;
        [SerializeField] float m_EndTime;
        [SerializeField] MotionMatchingSegmentLoopMode m_LoopMode;
        [SerializeField] bool m_CanInitialize;
        [SerializeField] bool m_CanJumpInto;
        [SerializeField] float m_EntryExclusion;
        [SerializeField] float m_ExitExclusion;
        [SerializeField] string m_ContinuationTargetSegmentId = string.Empty;
        [SerializeField] bool m_Terminal;

        public CharacterMotionMatchingSegmentId SegmentId => string.IsNullOrWhiteSpace(m_SegmentId) ? default : new CharacterMotionMatchingSegmentId(m_SegmentId);
        public CharacterMotionMatchingSourceClipId SourceClipId => string.IsNullOrWhiteSpace(m_SourceClipId) ? default : new CharacterMotionMatchingSourceClipId(m_SourceClipId);
        public float StartTime => m_StartTime;
        public float EndTime => m_EndTime;
        public MotionMatchingSegmentLoopMode LoopMode => m_LoopMode;
        public bool CanInitialize => m_CanInitialize;
        public bool CanJumpInto => m_CanJumpInto;
        public float EntryExclusion => m_EntryExclusion;
        public float ExitExclusion => m_ExitExclusion;
        public CharacterMotionMatchingSegmentId ContinuationTargetSegmentId => string.IsNullOrWhiteSpace(m_ContinuationTargetSegmentId) ? default : new CharacterMotionMatchingSegmentId(m_ContinuationTargetSegmentId);
        public bool Terminal => m_Terminal;

        public void RequireValid()
        {
            if (!SegmentId.IsValid || !SourceClipId.IsValid || !float.IsFinite(StartTime) || !float.IsFinite(EndTime) || StartTime < 0f || EndTime <= StartTime)
                throw new InvalidOperationException("Motion Matching Segment identity, source, or range is invalid.");
            if (!Enum.IsDefined(typeof(MotionMatchingSegmentLoopMode), LoopMode))
                throw new InvalidOperationException($"Motion Matching Segment '{SegmentId}' has no explicit loop mode.");
            if (!float.IsFinite(EntryExclusion) || !float.IsFinite(ExitExclusion) || EntryExclusion < 0f || ExitExclusion < 0f || EntryExclusion + ExitExclusion >= EndTime - StartTime)
                throw new InvalidOperationException($"Motion Matching Segment '{SegmentId}' has invalid exclusion ranges.");
            if (LoopMode == MotionMatchingSegmentLoopMode.Loop)
            {
                if (ContinuationTargetSegmentId.IsValid || Terminal)
                    throw new InvalidOperationException($"Loop Segment '{SegmentId}' must only loop to itself.");
            }
            else if (Terminal == ContinuationTargetSegmentId.IsValid)
            {
                throw new InvalidOperationException($"Finite Segment '{SegmentId}' must declare exactly one Continuation target or Terminal.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterMotionMatchingCoverageRequirement
    {
        [SerializeField] string m_RequirementId = string.Empty;
        [SerializeField] float m_MinimumSpeed;
        [SerializeField] float m_MaximumSpeed;
        [SerializeField] float m_MinimumFacingChangeDegrees;
        [SerializeField] float m_MaximumFacingChangeDegrees;
        [SerializeField] bool m_RequireInitialization;
        [SerializeField] MotionMatchingFootContactMask[] m_RequiredContactCombinations = Array.Empty<MotionMatchingFootContactMask>();
        [SerializeField] float m_MinimumPlanHorizon;

        public string RequirementId => m_RequirementId ?? string.Empty;
        public float MinimumSpeed => m_MinimumSpeed;
        public float MaximumSpeed => m_MaximumSpeed;
        public float MinimumFacingChangeDegrees => m_MinimumFacingChangeDegrees;
        public float MaximumFacingChangeDegrees => m_MaximumFacingChangeDegrees;
        public bool RequireInitialization => m_RequireInitialization;
        public IReadOnlyList<MotionMatchingFootContactMask> RequiredContactCombinations => m_RequiredContactCombinations ?? Array.Empty<MotionMatchingFootContactMask>();
        public float MinimumPlanHorizon => m_MinimumPlanHorizon;

        public void RequireValid()
        {
            MotionMatchingAuthoringValidation.RequireIdentity(RequirementId, nameof(RequirementId));
            if (!float.IsFinite(MinimumSpeed) || !float.IsFinite(MaximumSpeed) || MinimumSpeed < 0f || MaximumSpeed < MinimumSpeed ||
                !float.IsFinite(MinimumFacingChangeDegrees) || !float.IsFinite(MaximumFacingChangeDegrees) ||
                MinimumFacingChangeDegrees < 0f || MaximumFacingChangeDegrees < MinimumFacingChangeDegrees)
                throw new InvalidOperationException($"Motion Matching Coverage Requirement '{RequirementId}' has invalid speed or facing intervals.");
            MotionMatchingAuthoringValidation.RequireFinitePositive(MinimumPlanHorizon, nameof(MinimumPlanHorizon));
            if (RequiredContactCombinations.Count == 0)
                throw new InvalidOperationException($"Motion Matching Coverage Requirement '{RequirementId}' has no required contact combinations.");
            var combinations = new HashSet<MotionMatchingFootContactMask>();
            for (int i = 0; i < RequiredContactCombinations.Count; i++)
            {
                MotionMatchingFootContactMask value = RequiredContactCombinations[i];
                if ((value & ~MotionMatchingFootContactMask.Both) != 0 || !combinations.Add(value))
                    throw new InvalidOperationException($"Motion Matching Coverage Requirement '{RequirementId}' contact combination #{i} is invalid or duplicated.");
            }
        }
    }

    [CreateAssetMenu(fileName = "CharacterMotionMatchingDatabaseDefinition", menuName = "3C/Character/Motion Matching/Database Definition")]
    public sealed class CharacterMotionMatchingDatabaseDefinition : ScriptableObject
    {
        public const string SchemaVersion = "character-motion-matching-database-definition/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_DatabaseId = string.Empty;
        [SerializeField] int m_Revision;
        [SerializeField] string m_SearchDomainId = string.Empty;
        [SerializeField] CharacterAnimationRigDefinition m_TargetRig;
        [SerializeField] CharacterMotionMatchingFeatureSchema m_FeatureSchema;
        [SerializeField] float m_SampleRate;
        [SerializeField] CharacterMotionMatchingSourceSet[] m_SourceSets = Array.Empty<CharacterMotionMatchingSourceSet>();
        [SerializeField] CharacterMotionMatchingSegmentDefinition[] m_Segments = Array.Empty<CharacterMotionMatchingSegmentDefinition>();
        [SerializeField] CharacterMotionMatchingCoverageRequirement[] m_CoverageRequirements = Array.Empty<CharacterMotionMatchingCoverageRequirement>();

        public string Schema => m_Schema ?? string.Empty;
        public CharacterMotionMatchingDatabaseId DatabaseId => string.IsNullOrWhiteSpace(m_DatabaseId) ? default : new CharacterMotionMatchingDatabaseId(m_DatabaseId);
        public int Revision => m_Revision;
        public CharacterMotionMatchingSearchDomainId SearchDomainId => string.IsNullOrWhiteSpace(m_SearchDomainId) ? default : new CharacterMotionMatchingSearchDomainId(m_SearchDomainId);
        public CharacterAnimationRigDefinition TargetRig => m_TargetRig;
        public CharacterMotionMatchingFeatureSchema FeatureSchema => m_FeatureSchema;
        public float SampleRate => m_SampleRate;
        public IReadOnlyList<CharacterMotionMatchingSourceSet> SourceSets => m_SourceSets ?? Array.Empty<CharacterMotionMatchingSourceSet>();
        public IReadOnlyList<CharacterMotionMatchingSegmentDefinition> Segments => m_Segments ?? Array.Empty<CharacterMotionMatchingSegmentDefinition>();
        public IReadOnlyList<CharacterMotionMatchingCoverageRequirement> CoverageRequirements => m_CoverageRequirements ?? Array.Empty<CharacterMotionMatchingCoverageRequirement>();

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) || !DatabaseId.IsValid || !SearchDomainId.IsValid)
                throw new InvalidOperationException($"Motion Matching Database '{name}' has an invalid schema or identity.");
            MotionMatchingAuthoringValidation.RequireRevision(Revision, nameof(Revision));
            MotionMatchingAuthoringValidation.RequireFinitePositive(SampleRate, nameof(SampleRate));
            if (!TargetRig || !FeatureSchema)
                throw new InvalidOperationException($"Motion Matching Database '{name}' has no Target Rig or Feature Schema.");
            TargetRig.RequireValid();
            FeatureSchema.RequireValid();
            if (!string.Equals(TargetRig.RigId, FeatureSchema.Rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(TargetRig.Revision, FeatureSchema.Rig.Revision, StringComparison.Ordinal))
                throw new InvalidOperationException($"Motion Matching Database '{name}' Rig does not match its Feature Schema.");
            if (SourceSets.Count == 0 || Segments.Count == 0 || CoverageRequirements.Count == 0)
                throw new InvalidOperationException($"Motion Matching Database '{name}' is missing Source Sets, Segments, or Coverage Requirements.");

            var sourceSetIds = new HashSet<CharacterMotionMatchingSourceSetId>();
            var sourceClips = new HashSet<CharacterMotionMatchingSourceClipId>();
            for (int i = 0; i < SourceSets.Count; i++)
            {
                CharacterMotionMatchingSourceSet sourceSet = SourceSets[i];
                if (!sourceSet)
                    throw new InvalidOperationException($"Motion Matching Database '{name}' Source Set #{i} is missing.");
                sourceSet.RequireValid();
                if (!sourceSetIds.Add(sourceSet.SourceSetId) ||
                    !string.Equals(sourceSet.TargetRig.RigId, TargetRig.RigId, StringComparison.Ordinal) ||
                    !string.Equals(sourceSet.TargetRig.Revision, TargetRig.Revision, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Motion Matching Database '{name}' Source Set #{i} has a duplicate identity or mismatched Target Rig.");
                for (int clipIndex = 0; clipIndex < sourceSet.SourceClips.Count; clipIndex++)
                {
                    CharacterMotionMatchingSourceClipId sourceClipId = sourceSet.SourceClips[clipIndex].SourceClipId;
                    if (!sourceClips.Add(sourceClipId))
                        throw new InvalidOperationException($"Motion Matching Database '{name}' duplicates SourceClipId '{sourceClipId}' across Source Sets.");
                }
            }

            var segments = new Dictionary<CharacterMotionMatchingSegmentId, CharacterMotionMatchingSegmentDefinition>();
            for (int i = 0; i < Segments.Count; i++)
            {
                CharacterMotionMatchingSegmentDefinition segment = Segments[i];
                if (segment == null)
                    throw new InvalidOperationException($"Motion Matching Database '{name}' Segment #{i} is missing.");
                segment.RequireValid();
                if (!sourceClips.Contains(segment.SourceClipId) || !segments.TryAdd(segment.SegmentId, segment))
                    throw new InvalidOperationException($"Motion Matching Database '{name}' Segment '{segment.SegmentId}' has an orphan SourceClipId or duplicate identity.");
            }
            foreach (KeyValuePair<CharacterMotionMatchingSegmentId, CharacterMotionMatchingSegmentDefinition> pair in segments)
            {
                CharacterMotionMatchingSegmentDefinition segment = pair.Value;
                if (segment.ContinuationTargetSegmentId.IsValid && !segments.ContainsKey(segment.ContinuationTargetSegmentId))
                    throw new InvalidOperationException($"Motion Matching Segment '{segment.SegmentId}' has an orphan Continuation target '{segment.ContinuationTargetSegmentId}'.");
            }
            bool hasInitialization = false;
            for (int i = 0; i < Segments.Count; i++)
                hasInitialization |= Segments[i].CanInitialize;
            if (!hasInitialization)
                throw new InvalidOperationException($"Motion Matching Database '{name}' has no Initialization-capable Segment.");

            var coverageIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < CoverageRequirements.Count; i++)
            {
                CharacterMotionMatchingCoverageRequirement requirement = CoverageRequirements[i];
                if (requirement == null)
                    throw new InvalidOperationException($"Motion Matching Database '{name}' Coverage Requirement #{i} is missing.");
                requirement.RequireValid();
                if (!coverageIds.Add(requirement.RequirementId))
                    throw new InvalidOperationException($"Motion Matching Database '{name}' duplicates Coverage Requirement '{requirement.RequirementId}'.");
            }
        }
    }
}
