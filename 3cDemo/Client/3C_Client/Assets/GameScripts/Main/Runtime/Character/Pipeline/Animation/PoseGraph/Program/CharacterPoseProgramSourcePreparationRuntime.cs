using System;
using System.Collections.Generic;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseProgramSourcePreparationRuntime
    {
        readonly AnimancerComponent m_Animancer;
        readonly CharacterPoseActorState m_ActorState;
        readonly CharacterPoseProgramFramePages m_FramePages;
        readonly CharacterPoseSourceModule m_Source;
        readonly AnimationSlotBlendJob[] m_SlotJobs;
        readonly AnimationSelectedPosePlayerJob[] m_DirectPlayerJobs;
        readonly AnimationSelectedPosePlayerJob[] m_ClipPlayerJobs;
        readonly AnimationSelectedPosePlayerJob[] m_BlendSpacePlayerJobs;
        AnimationScriptPlayable[] m_SlotPlayables;
        AnimationScriptPlayable[] m_DirectPlayerPlayables;
        AnimationScriptPlayable[] m_ClipPlayerPlayables;
        AnimationScriptPlayable[] m_BlendSpacePlayerPlayables;
        int m_SequencePreviewPlayerIndex = -1;
        int m_SequencePreviewOperationIndex = -1;
        double m_SequencePreviewTime;
        bool m_SequencePreviewReset;
        bool m_HasSequencePreview;
        bool m_JobsInstalled;

        internal CharacterPoseProgramSourcePreparationRuntime(
            AnimancerComponent animancer,
            CharacterPoseActorState actorState,
            CharacterPoseProgramFramePages framePages,
            CharacterPoseSourceModule source)
        {
            m_Animancer = animancer ? animancer :
                throw new ArgumentNullException(nameof(animancer));
            m_ActorState = actorState ??
                throw new ArgumentNullException(nameof(actorState));
            m_FramePages = framePages ??
                throw new ArgumentNullException(nameof(framePages));
            m_Source = source ??
                throw new ArgumentNullException(nameof(source));
            m_SlotJobs =
                new AnimationSlotBlendJob[m_ActorState.Stacks.Length];
            m_DirectPlayerJobs =
                new AnimationSelectedPosePlayerJob[
                    m_ActorState.DirectPlayers.Length];
            m_ClipPlayerJobs =
                new AnimationSelectedPosePlayerJob[
                    m_ActorState.PoseStateSources.ClipPlayers.Length];
            m_BlendSpacePlayerJobs =
                new AnimationSelectedPosePlayerJob[
                    m_ActorState.PoseStateSources.BlendSpacePlayers.Length];
        }

        internal bool HasSequencePreview => m_HasSequencePreview;
        internal int SequencePreviewOperationIndex =>
            m_SequencePreviewOperationIndex;

        internal void SetSequencePreview(
            CharacterPoseProgramImage image,
            PresentationPoseSourceIndex sourceIndex,
            double sampleTime,
            bool resetContinuity)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (!sourceIndex.IsValid ||
                !double.IsFinite(sampleTime) ||
                sampleTime < 0d)
            {
                throw new ArgumentException(
                    "Clip Preview sample is invalid.");
            }
            int playerIndex = -1;
            for (int i = 0;
                 i < m_ActorState.PoseStateSources.ClipPlayers.Length;
                 i++)
            {
                if (m_ActorState.PoseStateSources.ClipPlayers[i].SourceIndex !=
                    sourceIndex)
                {
                    continue;
                }
                playerIndex = i;
                break;
            }
            if (playerIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Clip Preview source #{sourceIndex.Value} has no compiled Clip Player.");
            }
            int operationIndex = -1;
            for (int i = 0; i < image.OperationHeaders.Count; i++)
            {
                CharacterPoseOperationHeader operation =
                    image.OperationHeaders[i];
                if (operation.Code != CharacterPoseOperationCode.ClipPlayer ||
                    ((CharacterPosePlayerOperationPayload)
                        image.OperationPages.RequirePayload(operation))
                    .ClipPlayerIndex != playerIndex)
                {
                    continue;
                }
                operationIndex = operation.Index;
                break;
            }
            if (operationIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Clip Preview source #{sourceIndex.Value} has no compiled Pose operation.");
            }
            m_SequencePreviewPlayerIndex = playerIndex;
            m_SequencePreviewOperationIndex = operationIndex;
            m_SequencePreviewTime = sampleTime;
            m_SequencePreviewReset = resetContinuity;
            m_HasSequencePreview = true;
        }

        internal void ClearSequencePreview()
        {
            m_SequencePreviewPlayerIndex = -1;
            m_SequencePreviewOperationIndex = -1;
            m_SequencePreviewTime = 0d;
            m_SequencePreviewReset = false;
            m_HasSequencePreview = false;
        }

        internal bool ApplySequencePreview()
        {
            if (!m_HasSequencePreview)
                return false;
            for (int i = 0;
                 i < m_ActorState.PoseStateSources.ClipPlayers.Length;
                 i++)
            {
                AnimationClipPlayerRuntime player =
                    m_ActorState.PoseStateSources.ClipPlayers[i];
                bool selected = i == m_SequencePreviewPlayerIndex;
                player.SetRelevant(selected);
                if (selected)
                {
                    player.SetPreviewTime(
                        m_SequencePreviewTime,
                        m_SequencePreviewReset);
                }
            }
            for (int i = 0;
                 i < m_ActorState.PoseStateSources.BlendSpacePlayers.Length;
                 i++)
            {
                m_ActorState.PoseStateSources.BlendSpacePlayers[i]
                    .SetRelevant(false);
            }
            return true;
        }

        internal bool IsSequencePreviewPlayer(int playerIndex) =>
            m_HasSequencePreview &&
            playerIndex == m_SequencePreviewPlayerIndex;

        internal void BeginFrame(ulong completionIdentity)
        {
            for (int i = 0; i < m_ActorState.Stacks.Length; i++)
                m_ActorState.Stacks[i].BeginSourceFrame(completionIdentity);
            for (int i = 0; i < m_ActorState.DirectPlayers.Length; i++)
                m_ActorState.DirectPlayers[i].BeginFrame(completionIdentity);
            for (int i = 0;
                 i < m_ActorState.PoseStateSources.ClipPlayers.Length;
                 i++)
            {
                m_ActorState.PoseStateSources.ClipPlayers[i]
                    .BeginFrame(completionIdentity);
            }
            for (int i = 0;
                 i < m_ActorState.PoseStateSources.BlendSpacePlayers.Length;
                 i++)
            {
                m_ActorState.PoseStateSources.BlendSpacePlayers[i]
                    .BeginFrame(completionIdentity);
            }
        }

        internal void PrepareStacks(
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourcePreparationView preparations,
            float presentationDeltaSeconds,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> actionSourceSamples,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                PresentationPoseSourceSample> providerSourceSamples)
        {
            for (int stackIndex = 0;
                 stackIndex < m_ActorState.Stacks.Length;
                 stackIndex++)
            {
                AnimationBlendStackRuntime stack =
                    m_ActorState.Stacks[stackIndex];
                if (!m_ActorState.LinkedFragments.IsPlayerActive(
                        stack.PlayerIndex) ||
                    !stack.HasCurrentSelectionSample)
                {
                    continue;
                }
                for (int entryIndex = 0;
                     entryIndex < stack.EntryCount;
                     entryIndex++)
                {
                    AnimationBlendEntryId entry =
                        stack.GetEntryId(entryIndex);
                    if (entry.SourcePoseTarget ||
                        HasEarlierSource(
                            stack,
                            entryIndex,
                            entry.SourceId))
                    {
                        continue;
                    }
                    PrepareStackSource(
                        stack,
                        sourceLease,
                        in preparations,
                        entry.SourceId,
                        presentationDeltaSeconds,
                        actionSourceSamples,
                        providerSourceSamples);
                }
            }
        }

        internal void PrepareDirectPlayers(
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourcePreparationView preparations,
            float presentationDeltaSeconds,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                PresentationPoseSourceSample> sourceSamples)
        {
            for (int playerIndex = 0;
                 playerIndex < m_ActorState.DirectPlayers.Length;
                 playerIndex++)
            {
                AnimationSelectedPosePlayerRuntime player =
                    m_ActorState.DirectPlayers[playerIndex];
                if (!RequiresDirectSource(player))
                    continue;
                var key = new AnimationPlayerSourceSampleKey(
                    player.NodeId,
                    player.SourceId);
                if (!sourceSamples.TryGetValue(
                        key,
                        out PresentationPoseSourceSample sample))
                {
                    throw new InvalidOperationException(
                        $"Animation Pose Source '{player.SourceId}' has no current resolved request.");
                }
                AnimationPoseSourceCaptureBinding capture =
                    player.PrepareCapture(
                        in sample,
                        presentationDeltaSeconds);
                CharacterPoseSourcePreparation preparation =
                    CharacterPoseSourcePreparation.DirectPlayer(
                        playerIndex,
                        player.SourceId,
                        player.SourceOwnerIndex,
                        sample.Clips,
                        in sample,
                        in capture,
                        player.NodeId);
                Submit(sourceLease, in preparations, in preparation);
            }
        }

        internal void PrepareClipPlayers(
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourcePreparationView preparations,
            float presentationDeltaSeconds,
            in CharacterPoseSourceTuningView sourceTuning)
        {
            for (int playerIndex = 0;
                 playerIndex < m_ActorState.PoseStateSources.ClipPlayers.Length;
                 playerIndex++)
            {
                AnimationClipPlayerRuntime player =
                    m_ActorState.PoseStateSources.ClipPlayers[playerIndex];
                if (!RequiresClipSource(playerIndex, player))
                    continue;
                AnimationPoseSourceCaptureBinding capture =
                    player.PrepareCapture(
                        presentationDeltaSeconds,
                        sourceTuning.RequireClipPlayRate(playerIndex));
                CharacterPoseSourcePreparation preparation =
                    CharacterPoseSourcePreparation.ClipPlayer(
                        playerIndex,
                        player.SourceId,
                        player.PlayerIndex,
                        player.ClipSamples,
                        in capture,
                        player.NodeId);
                Submit(sourceLease, in preparations, in preparation);
            }
        }

        internal void PrepareBlendSpacePlayers(
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourcePreparationView preparations,
            float presentationDeltaSeconds)
        {
            for (int playerIndex = 0;
                 playerIndex <
                 m_ActorState.PoseStateSources.BlendSpacePlayers.Length;
                 playerIndex++)
            {
                AnimationBlendSpacePlayerRuntime player =
                    m_ActorState.PoseStateSources.BlendSpacePlayers[
                        playerIndex];
                if (!RequiresBlendSpaceSource(player))
                    continue;
                AnimationPoseSourceCaptureBinding capture =
                    player.PrepareCapture(presentationDeltaSeconds);
                CharacterPoseSourcePreparation preparation =
                    CharacterPoseSourcePreparation.BlendSpacePlayer(
                        playerIndex,
                        player.SourceId,
                        player.PlayerIndex,
                        player.ClipSamples,
                        in capture,
                        player.NodeId);
                Submit(sourceLease, in preparations, in preparation);
            }
        }

        internal void PrepareJobs(
            in CharacterPoseSourcePreparedResources preparedSources,
            ulong completionIdentity)
        {
            for (int slotIndex = 0;
                 slotIndex < m_ActorState.Stacks.Length;
                 slotIndex++)
            {
                AnimationBlendStackRuntime stack =
                    m_ActorState.Stacks[slotIndex];
                AnimationPlayerPoseNativeWriteBinding write =
                    m_FramePages.RequirePlayerWriteBinding(
                        stack.PlayerIndex,
                        completionIdentity);
                m_SlotJobs[slotIndex] = stack.PrepareSlotJob(
                    completionIdentity,
                    in write,
                    m_Source);
            }
            for (int slotIndex = 0;
                 slotIndex < m_ActorState.Stacks.Length;
                 slotIndex++)
            {
                m_ActorState.Stacks[slotIndex].PrepareCompletion(
                    completionIdentity);
            }
            for (int playerIndex = 0;
                 playerIndex < m_ActorState.DirectPlayers.Length;
                 playerIndex++)
            {
                AnimationSelectedPosePlayerRuntime player =
                    m_ActorState.DirectPlayers[playerIndex];
                AnimationPlayerPoseNativeWriteBinding write =
                    m_FramePages.RequirePlayerWriteBinding(
                        player.PlayerIndex,
                        completionIdentity);
                CharacterPoseSourceBinding sourceBinding =
                    RequiresDirectSource(player)
                        ? preparedSources.RequireDirectBinding(playerIndex)
                        : default;
                m_DirectPlayerJobs[playerIndex] = player.PrepareJob(
                    completionIdentity,
                    in write,
                    sourceBinding.PhysicalIdentity,
                    sourceBinding.SourceIndex);
            }
            for (int playerIndex = 0;
                 playerIndex < m_ActorState.PoseStateSources.ClipPlayers.Length;
                 playerIndex++)
            {
                AnimationClipPlayerRuntime player =
                    m_ActorState.PoseStateSources.ClipPlayers[playerIndex];
                AnimationPlayerPoseNativeWriteBinding write =
                    m_FramePages.RequirePlayerWriteBinding(
                        player.PlayerIndex,
                        completionIdentity);
                CharacterPoseSourceBinding sourceBinding =
                    RequiresClipSource(playerIndex, player)
                        ? preparedSources.RequireClipBinding(playerIndex)
                        : default;
                m_ClipPlayerJobs[playerIndex] = player.PrepareJob(
                    completionIdentity,
                    in write,
                    sourceBinding.PhysicalIdentity,
                    sourceBinding.SourceIndex);
            }
            for (int playerIndex = 0;
                 playerIndex <
                 m_ActorState.PoseStateSources.BlendSpacePlayers.Length;
                 playerIndex++)
            {
                AnimationBlendSpacePlayerRuntime player =
                    m_ActorState.PoseStateSources.BlendSpacePlayers[
                        playerIndex];
                AnimationPlayerPoseNativeWriteBinding write =
                    m_FramePages.RequirePlayerWriteBinding(
                        player.PlayerIndex,
                        completionIdentity);
                CharacterPoseSourceBinding sourceBinding =
                    RequiresBlendSpaceSource(player)
                        ? preparedSources.RequireBlendSpaceBinding(playerIndex)
                        : default;
                m_BlendSpacePlayerJobs[playerIndex] = player.PrepareJob(
                    completionIdentity,
                    in write,
                    sourceBinding.PhysicalIdentity,
                    sourceBinding.SourceIndex);
            }
        }

        internal void InstallOrUpdateJobs()
        {
            if (!m_JobsInstalled)
            {
                m_BlendSpacePlayerPlayables =
                    new AnimationScriptPlayable[
                        m_BlendSpacePlayerJobs.Length];
                for (int i = 0;
                     i < m_BlendSpacePlayerJobs.Length;
                     i++)
                {
                    m_BlendSpacePlayerPlayables[i] =
                        m_Animancer.Graph.InsertOutputJob(
                            m_BlendSpacePlayerJobs[i]);
                    m_BlendSpacePlayerPlayables[i].SetProcessInputs(true);
                }
                m_ClipPlayerPlayables =
                    new AnimationScriptPlayable[m_ClipPlayerJobs.Length];
                for (int i = 0; i < m_ClipPlayerJobs.Length; i++)
                {
                    m_ClipPlayerPlayables[i] =
                        m_Animancer.Graph.InsertOutputJob(
                            m_ClipPlayerJobs[i]);
                    m_ClipPlayerPlayables[i].SetProcessInputs(true);
                }
                m_DirectPlayerPlayables =
                    new AnimationScriptPlayable[m_DirectPlayerJobs.Length];
                for (int i = 0; i < m_DirectPlayerJobs.Length; i++)
                {
                    m_DirectPlayerPlayables[i] =
                        m_Animancer.Graph.InsertOutputJob(
                            m_DirectPlayerJobs[i]);
                    m_DirectPlayerPlayables[i].SetProcessInputs(true);
                }
                m_SlotPlayables =
                    new AnimationScriptPlayable[m_SlotJobs.Length];
                for (int i = 0; i < m_SlotJobs.Length; i++)
                {
                    m_SlotPlayables[i] =
                        m_Animancer.Graph.InsertOutputJob(m_SlotJobs[i]);
                    m_SlotPlayables[i].SetProcessInputs(true);
                }
                m_JobsInstalled = true;
                return;
            }
            for (int i = 0;
                 i < m_BlendSpacePlayerJobs.Length;
                 i++)
            {
                m_BlendSpacePlayerPlayables[i].SetJobData(
                    m_BlendSpacePlayerJobs[i]);
            }
            for (int i = 0; i < m_ClipPlayerJobs.Length; i++)
                m_ClipPlayerPlayables[i].SetJobData(m_ClipPlayerJobs[i]);
            for (int i = 0; i < m_DirectPlayerJobs.Length; i++)
                m_DirectPlayerPlayables[i].SetJobData(m_DirectPlayerJobs[i]);
            for (int i = 0; i < m_SlotJobs.Length; i++)
                m_SlotPlayables[i].SetJobData(m_SlotJobs[i]);
        }

        bool RequiresDirectSource(
            AnimationSelectedPosePlayerRuntime player) =>
            m_ActorState.LinkedFragments.IsPlayerActive(
                player.PlayerIndex) &&
            player.HasCurrentSample;

        bool RequiresClipSource(
            int playerIndex,
            AnimationClipPlayerRuntime player) =>
            (IsSequencePreviewPlayer(playerIndex) ||
             m_ActorState.LinkedFragments.IsPlayerActive(
                 player.PlayerIndex)) &&
            player.IsRelevant;

        bool RequiresBlendSpaceSource(
            AnimationBlendSpacePlayerRuntime player) =>
            m_ActorState.LinkedFragments.IsPlayerActive(
                player.PlayerIndex) &&
            player.IsRelevant;

        internal void DetachJobs()
        {
            if (!m_JobsInstalled ||
                !m_Animancer ||
                !m_Animancer.IsGraphInitialized)
            {
                return;
            }
            for (int i = m_SlotPlayables.Length - 1; i >= 0; i--)
                AnimancerUtilities.RemovePlayable(m_SlotPlayables[i]);
            for (int i = m_DirectPlayerPlayables.Length - 1; i >= 0; i--)
                AnimancerUtilities.RemovePlayable(m_DirectPlayerPlayables[i]);
            for (int i = m_ClipPlayerPlayables.Length - 1; i >= 0; i--)
                AnimancerUtilities.RemovePlayable(m_ClipPlayerPlayables[i]);
            for (int i = m_BlendSpacePlayerPlayables.Length - 1;
                 i >= 0;
                 i--)
            {
                AnimancerUtilities.RemovePlayable(
                    m_BlendSpacePlayerPlayables[i]);
            }
            m_JobsInstalled = false;
        }

        void PrepareStackSource(
            AnimationBlendStackRuntime stack,
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourcePreparationView preparations,
            AnimationPoseSourceId sourceId,
            float presentationDeltaSeconds,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                AnimationResolvedPoseSourceSample> actionSourceSamples,
            IReadOnlyDictionary<AnimationPlayerSourceSampleKey,
                PresentationPoseSourceSample> providerSourceSamples)
        {
            var key = new AnimationPlayerSourceSampleKey(
                stack.PoseNodeId,
                sourceId);
            if (sourceId.SourceKind == AnimationPoseSourceKind.Timeline)
            {
                if (!actionSourceSamples.TryGetValue(
                        key,
                        out AnimationResolvedPoseSourceSample sourceSample))
                {
                    throw new InvalidOperationException(
                        $"Action Pose Source '{sourceId}' has no current resolved request.");
                }
                AnimationPoseSampleRequest request = sourceSample.Request;
                AnimationPoseSourceCaptureBinding capture =
                    stack.PrepareCapture(
                        sourceSample,
                        presentationDeltaSeconds);
                CharacterPoseSourcePreparation preparation =
                    CharacterPoseSourcePreparation.Action(
                        in request,
                        in capture,
                        stack.PoseNodeId);
                Submit(sourceLease, in preparations, in preparation);
                return;
            }
            if (!providerSourceSamples.TryGetValue(
                    key,
                    out PresentationPoseSourceSample providerSample) ||
                !m_ActorState.NodeRuntimeIndex.TryGetSourceOwnerIndex(
                    stack.PoseNodeId,
                    out int sourceOwnerIndex))
            {
                throw new InvalidOperationException(
                    $"Presentation Pose Source '{sourceId}' has no current resolved request.");
            }
            AnimationResolvedPoseSourceSample resolved =
                m_Source.ResolveProviderSample(
                    in providerSample,
                    sourceOwnerIndex);
            AnimationPoseSampleRequest providerRequest = resolved.Request;
            AnimationPoseSourceCaptureBinding providerCapture =
                stack.PrepareCapture(
                    resolved,
                    presentationDeltaSeconds);
            CharacterPoseSourcePreparation providerPreparation =
                CharacterPoseSourcePreparation.Provider(
                    in providerRequest,
                    in providerSample,
                    in providerCapture,
                    stack.PoseNodeId);
            Submit(sourceLease, in preparations, in providerPreparation);
        }

        void Submit(
            CharacterPoseSourceFrameLease sourceLease,
            in CharacterPoseSourcePreparationView preparations,
            in CharacterPoseSourcePreparation preparation)
        {
            int index = m_FramePages.AddSourcePreparation(in preparation);
            m_Source.Prepare(sourceLease, in preparations, index);
        }

        static bool HasEarlierSource(
            AnimationBlendStackRuntime stack,
            int entryIndex,
            AnimationPoseSourceId sourceId)
        {
            for (int i = 0; i < entryIndex; i++)
            {
                AnimationBlendEntryId candidate = stack.GetEntryId(i);
                if (!candidate.SourcePoseTarget &&
                    candidate.SourceId.Equals(sourceId))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
