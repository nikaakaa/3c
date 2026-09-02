using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Sources;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal sealed class CharacterPoseProgramNodeRuntimeIndex
    {
        readonly Dictionary<PoseNodeId, AnimationBlendStackRuntime>
            m_Stacks =
                new Dictionary<PoseNodeId, AnimationBlendStackRuntime>();
        readonly Dictionary<PoseNodeId,
            CharacterAnimationTransitionRouteRuntime> m_Routes =
                new Dictionary<PoseNodeId,
                    CharacterAnimationTransitionRouteRuntime>();
        readonly Dictionary<PoseNodeId, int> m_PlayerIndices =
            new Dictionary<PoseNodeId, int>();
        readonly Dictionary<PoseNodeId, int> m_SourceOwnerIndices =
            new Dictionary<PoseNodeId, int>();
        readonly Dictionary<PoseNodeId,
            AnimationSelectedPosePlayerRuntime> m_DirectPlayers =
                new Dictionary<PoseNodeId,
                    AnimationSelectedPosePlayerRuntime>();

        internal void AddStack(
            PoseNodeId nodeId,
            AnimationBlendStackRuntime stack,
            CharacterAnimationTransitionRouteRuntime route,
            int playerIndex,
            int sourceOwnerIndex)
        {
            if (!nodeId.IsValid ||
                stack == null ||
                route == null ||
                playerIndex < 0 ||
                sourceOwnerIndex < -1)
            {
                throw new ArgumentException(
                    "Pose Program Stack runtime index input is invalid.");
            }
            m_Stacks.Add(nodeId, stack);
            m_Routes.Add(nodeId, route);
            m_PlayerIndices.Add(nodeId, playerIndex);
            if (sourceOwnerIndex >= 0)
                m_SourceOwnerIndices.Add(nodeId, sourceOwnerIndex);
        }

        internal void AddDirect(
            PoseNodeId nodeId,
            AnimationSelectedPosePlayerRuntime player,
            int playerIndex,
            int sourceOwnerIndex)
        {
            if (!nodeId.IsValid ||
                player == null ||
                playerIndex < 0 ||
                sourceOwnerIndex < 0)
            {
                throw new ArgumentException(
                    "Pose Program Direct Player runtime index input is invalid.");
            }
            m_PlayerIndices.Add(nodeId, playerIndex);
            m_SourceOwnerIndices.Add(nodeId, sourceOwnerIndex);
            m_DirectPlayers.Add(nodeId, player);
        }

        internal void AddPlayer(PoseNodeId nodeId, int playerIndex)
        {
            if (!nodeId.IsValid || playerIndex < 0)
            {
                throw new ArgumentException(
                    "Pose Program Player runtime index input is invalid.");
            }
            m_PlayerIndices.Add(nodeId, playerIndex);
        }

        internal bool TryGetPlayerIndex(
            PoseNodeId nodeId,
            out int playerIndex) =>
            m_PlayerIndices.TryGetValue(nodeId, out playerIndex);

        internal bool TryGetStackRoute(
            PoseNodeId nodeId,
            out AnimationBlendStackRuntime stack,
            out CharacterAnimationTransitionRouteRuntime route)
        {
            if (!m_Stacks.TryGetValue(nodeId, out stack) ||
                !m_Routes.TryGetValue(nodeId, out route))
            {
                stack = null;
                route = null;
                return false;
            }
            return true;
        }

        internal bool TryGetSourceOwnerIndex(
            PoseNodeId nodeId,
            out int sourceOwnerIndex) =>
            m_SourceOwnerIndices.TryGetValue(
                nodeId,
                out sourceOwnerIndex);

        internal void PushMotionMatchingSelection(
            CharacterPoseSourceModule sourceModule,
            PoseNodeId playerNodeId,
            in PresentationPoseSourceSample sample)
        {
            if (sourceModule == null)
                throw new ArgumentNullException(nameof(sourceModule));
            if (m_Stacks.TryGetValue(
                    playerNodeId,
                    out AnimationBlendStackRuntime stack))
            {
                if (sample.PlayerNodeId != playerNodeId ||
                    sample.SourceKind !=
                        AnimationPoseSourceKind.MotionMatching ||
                    !m_SourceOwnerIndices.TryGetValue(
                        playerNodeId,
                        out int sourceOwnerIndex))
                {
                    throw new InvalidOperationException(
                        $"Motion Matching Selection does not belong to Pose State Player '{playerNodeId}'.");
                }
                AnimationResolvedPoseSourceSample resolved =
                    sourceModule.ResolveProviderSample(
                        in sample,
                        sourceOwnerIndex);
                AnimationPoseSampleRequest request = resolved.Request;
                m_Routes[playerNodeId].PushSelection(
                    stack,
                    in request);
                return;
            }
            if (m_DirectPlayers.TryGetValue(
                    playerNodeId,
                    out AnimationSelectedPosePlayerRuntime player))
            {
                player.PushSelection(in sample);
                return;
            }
            throw new InvalidOperationException(
                $"Motion Matching Pose State Player '{playerNodeId}' is not installed in the active Pose Plan.");
        }

        internal bool PlayerUsesSource(
            PoseNodeId playerNodeId,
            AnimationPoseSourceId sourceId)
        {
            if (m_Stacks.TryGetValue(
                    playerNodeId,
                    out AnimationBlendStackRuntime stack))
            {
                for (int i = 0; i < stack.EntryCount; i++)
                {
                    AnimationBlendEntryId entry = stack.GetEntryId(i);
                    if (!entry.SourcePoseTarget &&
                        entry.SourceId.Equals(sourceId))
                    {
                        return true;
                    }
                }
                return false;
            }
            return m_DirectPlayers.TryGetValue(
                       playerNodeId,
                       out AnimationSelectedPosePlayerRuntime player) &&
                   player.HasSelection &&
                   player.SourceId.Equals(sourceId);
        }
    }
}
