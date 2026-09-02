using System;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseProgramSourceRetirementRuntime
    {
        readonly CharacterPoseActorState m_ActorState;
        readonly CharacterPoseSourceModule m_Source;

        internal CharacterPoseProgramSourceRetirementRuntime(
            CharacterPoseActorState actorState,
            CharacterPoseSourceModule source)
        {
            m_ActorState = actorState ??
                throw new ArgumentNullException(nameof(actorState));
            m_Source = source ??
                throw new ArgumentNullException(nameof(source));
        }

        internal void ExecutePreparedActionBackendReleaseRequests() =>
            m_ActorState.SourceRetirement.ExecutePreparedActions(m_Source);

        internal void StageCompletedSources(ulong completionIdentity)
        {
            if (m_ActorState.SourceRetirement.PendingPoseCount != 0)
            {
                throw new InvalidOperationException(
                    "Pose source releases from the previous committed frame were not finalized.");
            }
            for (int stackIndex = 0;
                 stackIndex < m_ActorState.Stacks.Length;
                 stackIndex++)
            {
                AnimationBlendStackRuntime stack =
                    m_ActorState.Stacks[stackIndex];
                CharacterAnimationTransitionRouteRuntime route =
                    m_ActorState.Routes[stackIndex];
                if (!route.CanReleaseSources)
                    continue;
                int releaseCount = stack.PendingPriorFrameReleaseCount(
                    completionIdentity);
                for (int releaseIndex = 0;
                     releaseIndex < releaseCount;
                     releaseIndex++)
                {
                    AnimationBlendStackSourceReleaseToken stackRelease =
                        stack.PrepareRelease(
                            releaseIndex,
                            completionIdentity);
                    AnimationBlendStackRelease release =
                        stackRelease.Release;
                    AnimationPhysicalSourceIdentity physical =
                        m_Source.RequireIdentity(
                            release.SourceId,
                            release.PoseNodeId);
                    if (route.IsAnimationSlot &&
                        CharacterPoseProgramSourceRetirementState
                            .IsFiniteActionSource(release.SourceId))
                    {
                        m_ActorState.SourceRetirement.StageAction(
                            m_Source,
                            route.SlotId,
                            stack,
                            route,
                            in stackRelease,
                            physical);
                        continue;
                    }
                    m_ActorState.SourceRetirement.StagePose(
                        m_Source,
                        stack,
                        route,
                        in stackRelease,
                        physical);
                }
            }
        }

        internal void Validate(
            CharacterPoseSourceFrameLease sourceLease)
        {
            m_Source.ValidatePhysicalFrame();
            int standaloneReleaseCount = 0;
            for (int i = 0;
                 i < m_ActorState.DirectPlayers.Length;
                 i++)
            {
                AnimationSelectedPosePlayerRuntime player =
                    m_ActorState.DirectPlayers[i];
                int releaseCount = player.PendingReleaseCount;
                standaloneReleaseCount = checked(
                    standaloneReleaseCount + releaseCount);
                for (int releaseIndex = 0;
                     releaseIndex < releaseCount;
                     releaseIndex++)
                {
                    AnimationPlayerReleaseToken playerRelease =
                        player.PrepareRelease(releaseIndex);
                    CharacterPoseSourceRetirementHandle sourceRelease =
                        Prepare(
                            playerRelease.SourceId,
                            player.NodeId,
                            default);
                    m_ActorState.SourceRetirement.PrepareDirect(
                        i,
                        in sourceRelease,
                        in playerRelease);
                }
            }
            for (int i = 0;
                 i < m_ActorState.PoseStateSources.ClipPlayers.Length;
                 i++)
            {
                AnimationClipPlayerRuntime player =
                    m_ActorState.PoseStateSources.ClipPlayers[i];
                int releaseCount = player.PendingReleaseCount;
                standaloneReleaseCount = checked(
                    standaloneReleaseCount + releaseCount);
                for (int releaseIndex = 0;
                     releaseIndex < releaseCount;
                     releaseIndex++)
                {
                    AnimationPlayerReleaseToken playerRelease =
                        player.PrepareRelease(releaseIndex);
                    CharacterPoseSourceRetirementHandle sourceRelease =
                        Prepare(
                            playerRelease.SourceId,
                            player.NodeId,
                            default);
                    m_ActorState.SourceRetirement.PrepareClip(
                        i,
                        in sourceRelease,
                        in playerRelease);
                }
            }
            for (int i = 0;
                 i < m_ActorState.PoseStateSources.BlendSpacePlayers.Length;
                 i++)
            {
                AnimationBlendSpacePlayerRuntime player =
                    m_ActorState.PoseStateSources.BlendSpacePlayers[i];
                int releaseCount = player.PendingReleaseCount;
                standaloneReleaseCount = checked(
                    standaloneReleaseCount + releaseCount);
                for (int releaseIndex = 0;
                     releaseIndex < releaseCount;
                     releaseIndex++)
                {
                    AnimationPlayerReleaseToken playerRelease =
                        player.PrepareRelease(releaseIndex);
                    CharacterPoseSourceRetirementHandle sourceRelease =
                        Prepare(
                            playerRelease.SourceId,
                            player.NodeId,
                            default);
                    m_ActorState.SourceRetirement.PrepareBlendSpace(
                        i,
                        in sourceRelease,
                        in playerRelease);
                }
            }
            int preparedActionReleaseCount =
                m_ActorState.SourceRetirement.PreparePendingRetirements(
                    m_Source);
            m_Source.RequireReleaseDiagnosticsCapacity(
                checked(
                    standaloneReleaseCount +
                    m_ActorState.SourceRetirement.PendingPoseCount +
                    preparedActionReleaseCount));
            m_Source.RequireActionBackendReleaseCompletionCapacity(
                checked(preparedActionReleaseCount * 2));
            m_Source.ValidateFrame(sourceLease);
        }

        internal void FinalizeCommitted(ulong completionIdentity)
        {
            m_ActorState.SourceRetirement.ApplyStandalone(
                m_Source,
                m_ActorState.DirectPlayers,
                m_ActorState.PoseStateSources.ClipPlayers,
                m_ActorState.PoseStateSources.BlendSpacePlayers,
                completionIdentity);
            m_ActorState.SourceRetirement.ApplyPendingPose(m_Source);
        }

        internal void ReleaseCompleted(ulong completionIdentity)
        {
            for (int stackIndex = 0;
                 stackIndex < m_ActorState.Stacks.Length;
                 stackIndex++)
            {
                AnimationBlendStackRuntime stack =
                    m_ActorState.Stacks[stackIndex];
                CharacterAnimationTransitionRouteRuntime route =
                    m_ActorState.Routes[stackIndex];
                if (!route.CanReleaseSources)
                    continue;
                bool releasedAny = false;
                int releaseCount = stack.PendingReleaseCount;
                for (int releaseIndex = 0;
                     releaseIndex < releaseCount;
                     releaseIndex++)
                {
                    AnimationBlendStackSourceReleaseToken stackRelease =
                        stack.PrepareRelease(
                            releaseIndex,
                            completionIdentity);
                    AnimationBlendStackRelease release =
                        stackRelease.Release;
                    CharacterPoseSourceRetirementHandle sourceRelease =
                        Prepare(
                            release.SourceId,
                            release.PoseNodeId,
                            default);
                    m_Source.ApplyRetirement(in sourceRelease);
                    stack.ApplyPreparedRelease(in stackRelease);
                    releasedAny = true;
                }
                if (releasedAny)
                    route.NotifySourcesReleased();
            }
        }

        internal void ReleasePlayers()
        {
            for (int i = 0; i < m_ActorState.DirectPlayers.Length; i++)
                Release(m_ActorState.DirectPlayers[i]);
            for (int i = 0;
                 i < m_ActorState.PoseStateSources.ClipPlayers.Length;
                 i++)
            {
                Release(m_ActorState.PoseStateSources.ClipPlayers[i]);
            }
            for (int i = 0;
                 i < m_ActorState.PoseStateSources.BlendSpacePlayers.Length;
                 i++)
            {
                Release(
                    m_ActorState.PoseStateSources.BlendSpacePlayers[i]);
            }
        }

        void Release(AnimationSelectedPosePlayerRuntime player)
        {
            int releaseCount = player.PendingReleaseCount;
            for (int releaseIndex = 0;
                 releaseIndex < releaseCount;
                 releaseIndex++)
            {
                AnimationPlayerReleaseToken playerRelease =
                    player.PrepareRelease(releaseIndex);
                CharacterPoseSourceRetirementHandle sourceRelease =
                    Prepare(
                        playerRelease.SourceId,
                        player.NodeId,
                        default);
                m_Source.ApplyRetirement(in sourceRelease);
                player.ApplyPreparedRelease(in playerRelease);
            }
        }

        void Release(AnimationClipPlayerRuntime player)
        {
            int releaseCount = player.PendingReleaseCount;
            for (int releaseIndex = 0;
                 releaseIndex < releaseCount;
                 releaseIndex++)
            {
                AnimationPlayerReleaseToken playerRelease =
                    player.PrepareRelease(releaseIndex);
                CharacterPoseSourceRetirementHandle sourceRelease =
                    Prepare(
                        playerRelease.SourceId,
                        player.NodeId,
                        default);
                m_Source.ApplyRetirement(in sourceRelease);
                player.ApplyPreparedRelease(in playerRelease);
            }
        }

        void Release(AnimationBlendSpacePlayerRuntime player)
        {
            int releaseCount = player.PendingReleaseCount;
            for (int releaseIndex = 0;
                 releaseIndex < releaseCount;
                 releaseIndex++)
            {
                AnimationPlayerReleaseToken playerRelease =
                    player.PrepareRelease(releaseIndex);
                CharacterPoseSourceRetirementHandle sourceRelease =
                    Prepare(
                        playerRelease.SourceId,
                        player.NodeId,
                        default);
                m_Source.ApplyRetirement(in sourceRelease);
                player.ApplyPreparedRelease(in playerRelease);
            }
        }

        CharacterPoseSourceRetirementHandle Prepare(
            AnimationPoseSourceId sourceId,
            PoseNodeId poseNodeId,
            AnimationPhysicalSourceIdentity expectedPhysicalIdentity)
        {
            var permission = new CharacterPoseSourceRetirementPermission(
                sourceId,
                poseNodeId,
                expectedPhysicalIdentity);
            return m_Source.PrepareRetirement(in permission);
        }
    }
}
