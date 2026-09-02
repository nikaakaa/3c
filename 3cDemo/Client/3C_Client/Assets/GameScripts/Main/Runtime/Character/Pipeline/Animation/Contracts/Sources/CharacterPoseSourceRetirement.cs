using System;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct CharacterPoseSourceRetirementPermission
    {
        internal CharacterPoseSourceRetirementPermission(
            AnimationPoseSourceId sourceId,
            PoseNodeId poseNodeId,
            AnimationPhysicalSourceIdentity expectedPhysicalIdentity)
        {
            if (!sourceId.IsValid || !poseNodeId.IsValid)
            {
                throw new ArgumentException(
                    "Character Pose source retirement permission is invalid.");
            }
            SourceId = sourceId;
            PoseNodeId = poseNodeId;
            ExpectedPhysicalIdentity = expectedPhysicalIdentity;
        }

        internal AnimationPoseSourceId SourceId { get; }
        internal PoseNodeId PoseNodeId { get; }
        internal AnimationPhysicalSourceIdentity ExpectedPhysicalIdentity
        {
            get;
        }
        internal bool IsValid =>
            SourceId.IsValid &&
            PoseNodeId.IsValid;

        internal bool Matches(
            in CharacterPoseSourceRetirementPermission other) =>
            SourceId.Equals(other.SourceId) &&
            PoseNodeId == other.PoseNodeId &&
            ExpectedPhysicalIdentity ==
                other.ExpectedPhysicalIdentity;
    }

    internal readonly struct CharacterPoseSourceRetirementHandle
    {
        internal CharacterPoseSourceRetirementHandle(
            int index,
            ulong generation,
            in CharacterPoseSourceRetirementPermission permission)
        {
            if (index < 0 ||
                generation == 0 ||
                !permission.IsValid)
            {
                throw new ArgumentException(
                    "Character Pose source retirement handle is invalid.");
            }
            m_EncodedIndex = checked(index + 1);
            Generation = generation;
            Permission = permission;
        }

        readonly int m_EncodedIndex;
        internal int Index => m_EncodedIndex - 1;
        internal ulong Generation { get; }
        internal CharacterPoseSourceRetirementPermission Permission
        {
            get;
        }
        internal bool IsValid =>
            m_EncodedIndex > 0 &&
            Generation != 0 &&
            Permission.IsValid;
    }
}
