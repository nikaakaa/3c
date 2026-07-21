using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    [Serializable]
    public sealed class CharacterMotionMatchingSourceClipEntry
    {
        [SerializeField] string m_SourceClipId = string.Empty;
        [SerializeField] string m_AnimationClipAssetGuid = string.Empty;
        [SerializeField] long m_AnimationClipLocalFileId;

        public CharacterMotionMatchingSourceClipId SourceClipId => string.IsNullOrWhiteSpace(m_SourceClipId) ? default : new CharacterMotionMatchingSourceClipId(m_SourceClipId);
        public string AnimationClipAssetGuid => m_AnimationClipAssetGuid ?? string.Empty;
        public long AnimationClipLocalFileId => m_AnimationClipLocalFileId;

        public CharacterMotionMatchingSourceClipEntry() { }

        public CharacterMotionMatchingSourceClipEntry(
            CharacterMotionMatchingSourceClipId sourceClipId,
            string animationClipAssetGuid,
            long animationClipLocalFileId)
        {
            if (!sourceClipId.IsValid)
                throw new ArgumentException("Source Clip identity is invalid.", nameof(sourceClipId));
            if (!MotionMatchingAuthoringValidation.IsAssetGuid(animationClipAssetGuid))
                throw new ArgumentException("Animation Clip asset GUID is invalid.", nameof(animationClipAssetGuid));
            if (animationClipLocalFileId == 0)
                throw new ArgumentOutOfRangeException(nameof(animationClipLocalFileId));
            m_SourceClipId = sourceClipId.Value;
            m_AnimationClipAssetGuid = animationClipAssetGuid;
            m_AnimationClipLocalFileId = animationClipLocalFileId;
        }

        public void RequireValid()
        {
            if (!SourceClipId.IsValid || !MotionMatchingAuthoringValidation.IsAssetGuid(AnimationClipAssetGuid) || AnimationClipLocalFileId == 0)
                throw new InvalidOperationException("Motion Matching Source Clip entry is incomplete.");
        }
    }

    [CreateAssetMenu(fileName = "CharacterMotionMatchingSourceSet", menuName = "3C/Character/Motion Matching/Source Set")]
    public sealed class CharacterMotionMatchingSourceSet : ScriptableObject
    {
        public const string SchemaVersion = "character-motion-matching-source-set/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_SourceSetId = string.Empty;
        [SerializeField] int m_Revision;
        [SerializeField] CharacterAnimationRigDefinition m_TargetRig;
        [SerializeField] MotionMatchingSamplingCompatibilityMode m_SamplingCompatibilityMode;
        [SerializeField] string m_MotionRootBoneId = string.Empty;
        [SerializeField] CharacterMotionMatchingSourceClipEntry[] m_SourceClips = Array.Empty<CharacterMotionMatchingSourceClipEntry>();

        public string Schema => m_Schema ?? string.Empty;
        public CharacterMotionMatchingSourceSetId SourceSetId => string.IsNullOrWhiteSpace(m_SourceSetId) ? default : new CharacterMotionMatchingSourceSetId(m_SourceSetId);
        public int Revision => m_Revision;
        public CharacterAnimationRigDefinition TargetRig => m_TargetRig;
        public MotionMatchingSamplingCompatibilityMode SamplingCompatibilityMode => m_SamplingCompatibilityMode;
        public AnimationBoneId MotionRootBoneId => string.IsNullOrWhiteSpace(m_MotionRootBoneId) ? default : new AnimationBoneId(m_MotionRootBoneId);
        public IReadOnlyList<CharacterMotionMatchingSourceClipEntry> SourceClips => m_SourceClips ?? Array.Empty<CharacterMotionMatchingSourceClipEntry>();

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) || !SourceSetId.IsValid)
                throw new InvalidOperationException($"Motion Matching Source Set '{name}' has an invalid schema or identity.");
            MotionMatchingAuthoringValidation.RequireRevision(Revision, nameof(Revision));
            if (!TargetRig)
                throw new InvalidOperationException($"Motion Matching Source Set '{name}' has no Target Rig.");
            TargetRig.RequireValid();
            if (!Enum.IsDefined(typeof(MotionMatchingSamplingCompatibilityMode), SamplingCompatibilityMode))
                throw new InvalidOperationException($"Motion Matching Source Set '{name}' has no explicit sampling compatibility mode.");
            if (!MotionRootBoneId.IsValid)
                throw new InvalidOperationException($"Motion Matching Source Set '{name}' has no Motion Root Bone identity.");
            TargetRig.RequireBoneIndex(MotionRootBoneId);
            if (SourceClips.Count == 0)
                throw new InvalidOperationException($"Motion Matching Source Set '{name}' contains no registered clips.");

            var sourceClipIds = new HashSet<CharacterMotionMatchingSourceClipId>();
            var assetKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < SourceClips.Count; i++)
            {
                CharacterMotionMatchingSourceClipEntry sourceClip = SourceClips[i];
                if (sourceClip == null)
                    throw new InvalidOperationException($"Motion Matching Source Set '{name}' clip #{i} is missing.");
                sourceClip.RequireValid();
                if (!sourceClipIds.Add(sourceClip.SourceClipId))
                    throw new InvalidOperationException($"Motion Matching Source Set '{name}' duplicates SourceClipId '{sourceClip.SourceClipId}'.");
                string assetKey = sourceClip.AnimationClipAssetGuid + ":" + sourceClip.AnimationClipLocalFileId.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (!assetKeys.Add(assetKey))
                    throw new InvalidOperationException($"Motion Matching Source Set '{name}' registers Animation Clip '{assetKey}' more than once.");
            }
        }

        public bool TryGetSourceClip(CharacterMotionMatchingSourceClipId sourceClipId, out CharacterMotionMatchingSourceClipEntry sourceClip)
        {
            for (int i = 0; i < SourceClips.Count; i++)
            {
                CharacterMotionMatchingSourceClipEntry candidate = SourceClips[i];
                if (candidate != null && candidate.SourceClipId.Equals(sourceClipId))
                {
                    sourceClip = candidate;
                    return true;
                }
            }
            sourceClip = null;
            return false;
        }
    }
}
