using TreeDesigner.Authoring;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BTSMTL.Timeline;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;
using AnimationClip = UnityEngine.AnimationClip;


using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation
{
    public sealed class AgentPresentationMutationPlan
    {
        internal AgentPresentationMutationPlan(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseGraphAsset poseGraph,
            string profileId,
            string poseGraphOwnerId,
            CharacterPresentationMutationTransaction graphTransaction,
            CharacterPresentationMutationTransaction profileTransaction,
            IReadOnlyList<AgentLinkedPoseGraphMutationPlan> linkedPoseGraphs,
            IReadOnlyList<AgentAnimationClipCurveMutationPlan> animationClips,
            CharacterLocomotionSyncGroup[] locomotionSyncGroups,
            bool setLocomotionSyncGroups)
        {
            Profile = profile;
            PoseGraph = poseGraph;
            ProfileId = profileId;
            PoseGraphOwnerId = poseGraphOwnerId;
            GraphTransaction = graphTransaction;
            ProfileTransaction = profileTransaction;
            LinkedPoseGraphs = linkedPoseGraphs ??
                               Array.Empty<AgentLinkedPoseGraphMutationPlan>();
            AnimationClips = animationClips ?? Array.Empty<AgentAnimationClipCurveMutationPlan>();
            LocomotionSyncGroups = locomotionSyncGroups ?? Array.Empty<CharacterLocomotionSyncGroup>();
            SetLocomotionSyncGroups = setLocomotionSyncGroups;
        }

        public CharacterAnimationPresentationProfile Profile { get; }
        public CharacterPresentationPoseGraphAsset PoseGraph { get; }
        public string ProfileId { get; }
        public string PoseGraphOwnerId { get; }
        public CharacterPresentationMutationTransaction GraphTransaction { get; }
        public CharacterPresentationMutationTransaction ProfileTransaction { get; }
        public IReadOnlyList<AgentLinkedPoseGraphMutationPlan> LinkedPoseGraphs { get; }
        public IReadOnlyList<AgentAnimationClipCurveMutationPlan> AnimationClips { get; }
        public CharacterLocomotionSyncGroup[] LocomotionSyncGroups { get; }
        public bool SetLocomotionSyncGroups { get; }
        public bool IsEmpty =>
            GraphTransaction.Mutations.Count == 0 &&
            ProfileTransaction.Mutations.Count == 0 &&
            LinkedPoseGraphs.All(value => value.Transaction.Mutations.Count == 0) &&
            AnimationClips.Count == 0 &&
            !SetLocomotionSyncGroups;
    }

    public sealed class AgentAnimationClipCurveMutationPlan
    {
        internal AgentAnimationClipCurveMutationPlan(
            AnimationClip clip,
            AgentPackageAnimationClipCurvesFile target)
        {
            Clip = clip ? clip : throw new ArgumentNullException(nameof(clip));
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public AnimationClip Clip { get; }
        public AgentPackageAnimationClipCurvesFile Target { get; }
    }

    public sealed class AgentLinkedPoseGraphMutationPlan
    {
        internal AgentLinkedPoseGraphMutationPlan(
            CharacterLinkedPoseImplementationAsset implementation,
            CharacterPresentationPoseGraphAsset graphOwner,
            string graphOwnerId,
            CharacterPresentationMutationTransaction transaction)
        {
            Implementation = implementation;
            GraphOwner = graphOwner;
            GraphOwnerId = graphOwnerId;
            Transaction = transaction;
        }

        public CharacterLinkedPoseImplementationAsset Implementation { get; }
        public CharacterPresentationPoseGraphAsset GraphOwner { get; }
        public string GraphOwnerId { get; }
        public CharacterPresentationMutationTransaction Transaction { get; }
    }
}
