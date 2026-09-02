using System;
using ThirdPersonCharacter.Pipeline.Animation.Sources;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal sealed class CharacterPoseProgramSourceRetirementState
    {
        enum StandaloneOwner : byte
        {
            Direct = 1,
            Clip = 2,
            BlendSpace = 3
        }

        struct StandaloneRetirement
        {
            internal StandaloneOwner Owner;
            internal int PlayerIndex;
            internal AnimationPoseSourceId SourceId;
            internal PoseNodeId NodeId;
            internal CharacterPoseSourceRetirementHandle Source;
            internal AnimationPlayerReleaseToken Player;
        }

        readonly StandaloneRetirement[] m_Standalone;
        int m_StandaloneCount;

        internal CharacterPoseProgramSourceRetirementState(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Standalone = new StandaloneRetirement[capacity];
        }

        internal int StandaloneCapacity => m_Standalone.Length;
        internal bool HasPreparedStandalone => m_StandaloneCount != 0;

        internal void PrepareDirect(
            int playerIndex,
            in CharacterPoseSourceRetirementHandle source,
            in AnimationPlayerReleaseToken player) =>
            Prepare(
                StandaloneOwner.Direct,
                playerIndex,
                in source,
                in player);

        internal void PrepareClip(
            int playerIndex,
            in CharacterPoseSourceRetirementHandle source,
            in AnimationPlayerReleaseToken player) =>
            Prepare(
                StandaloneOwner.Clip,
                playerIndex,
                in source,
                in player);

        internal void PrepareBlendSpace(
            int playerIndex,
            in CharacterPoseSourceRetirementHandle source,
            in AnimationPlayerReleaseToken player) =>
            Prepare(
                StandaloneOwner.BlendSpace,
                playerIndex,
                in source,
                in player);

        internal void ApplyStandalone(
            CharacterPoseSourceModule sourceModule,
            AnimationSelectedPosePlayerRuntime[] directPlayers,
            AnimationClipPlayerRuntime[] clipPlayers,
            AnimationBlendSpacePlayerRuntime[] blendSpacePlayers,
            ulong completionIdentity)
        {
            if (sourceModule == null ||
                directPlayers == null ||
                clipPlayers == null ||
                blendSpacePlayers == null ||
                completionIdentity == 0)
            {
                throw new ArgumentException(
                    "Standalone Pose source retirement input is invalid.");
            }
            for (int i = 0; i < m_StandaloneCount; i++)
            {
                StandaloneRetirement retirement = m_Standalone[i];
                sourceModule.ApplyRetirement(in retirement.Source);
                switch (retirement.Owner)
                {
                    case StandaloneOwner.Direct:
                        directPlayers[retirement.PlayerIndex]
                            .ApplyPreparedRelease(
                                in retirement.Player);
                        break;
                    case StandaloneOwner.Clip:
                        clipPlayers[retirement.PlayerIndex]
                            .ApplyPreparedRelease(
                                in retirement.Player);
                        break;
                    case StandaloneOwner.BlendSpace:
                        blendSpacePlayers[retirement.PlayerIndex]
                            .ApplyPreparedRelease(
                                in retirement.Player);
                        break;
                    default:
                        throw new InvalidOperationException(
                            "Standalone pose source release owner is invalid.");
                }
                sourceModule.RecordRelease(
                    retirement.NodeId,
                    retirement.SourceId,
                    completionIdentity);
                m_Standalone[i] = default;
            }
            m_StandaloneCount = 0;
        }

        internal void ClearStandalone()
        {
            Array.Clear(
                m_Standalone,
                0,
                m_StandaloneCount);
            m_StandaloneCount = 0;
        }

        void Prepare(
            StandaloneOwner owner,
            int playerIndex,
            in CharacterPoseSourceRetirementHandle source,
            in AnimationPlayerReleaseToken player)
        {
            if (owner == 0 ||
                playerIndex < 0 ||
                !source.IsValid ||
                !player.IsValid ||
                m_StandaloneCount >= m_Standalone.Length)
            {
                throw new InvalidOperationException(
                    "Standalone pose source release exceeds its compiled journal.");
            }
            CharacterPoseSourceRetirementPermission permission =
                source.Permission;
            m_Standalone[m_StandaloneCount++] =
                new StandaloneRetirement
                {
                    Owner = owner,
                    PlayerIndex = playerIndex,
                    SourceId = permission.SourceId,
                    NodeId = permission.PoseNodeId,
                    Source = source,
                    Player = player
                };
        }
    }
}
