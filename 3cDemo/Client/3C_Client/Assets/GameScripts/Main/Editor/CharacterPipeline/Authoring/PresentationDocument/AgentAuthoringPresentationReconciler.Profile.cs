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
    public sealed partial class AgentAuthoringPresentationReconciler
    {
        void BuildProfilePlan(
            AgentPackagePresentationProfileFile current,
            AgentPackagePresentationProfileFile target,
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseGraphAsset poseGraph,
            CharacterAnimationRigDefinition rig,
            PlanBuilder builder,
            AgentCompileReport report)
        {
            string path = "editable/presentation/profile.json";
            if (!Same(current.poseGraph, target.poseGraph) ||
                !Same(current.rig, target.rig))
            {
                builder.Profile(
                    path,
                    new SetPresentationGraphMutation(
                        target.id,
                        poseGraph,
                        rig));
            }

            CharacterMotionMatchingProfile motionMatching =
                ResolveOptional<CharacterMotionMatchingProfile>(
                    target.policy.motionMatchingProfile,
                    path + ".policy.motionMatchingProfile",
                    report);
            if (!Same(
                    current.policy.motionMatchingProfile,
                    target.policy.motionMatchingProfile))
            {
                builder.Profile(
                    path + ".policy.motionMatchingProfile",
                    new SetMotionMatchingProfileMutation(
                        target.id,
                        motionMatching));
            }
            if (!string.Equals(
                    current.policy.footPlacementAnalysisMode,
                    target.policy.footPlacementAnalysisMode,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    current.policy.footPlacementAnalysisSourceAssetGuid,
                    target.policy.footPlacementAnalysisSourceAssetGuid,
                    StringComparison.Ordinal))
            {
                builder.Profile(
                    path + ".policy.footPlacement",
                    new SetFootPlacementAnalysisMutation(
                        target.id,
                        Enum.Parse<CharacterFootPlacementAnalysisMode>(
                            target.policy.footPlacementAnalysisMode,
                            false),
                        target.policy.footPlacementAnalysisSourceAssetGuid));
            }

            Dictionary<string, AgentPackagePoseSourceBinding> oldSources =
                Index(current.poseSources, value =>
                    ReferenceIdentity(value.binding));
            Dictionary<string, AgentPackagePoseSourceBinding> newSources =
                Index(target.poseSources, value =>
                    ReferenceIdentity(value.binding));
            foreach (AgentPackagePoseSourceBinding removed in
                     current.poseSources.Where(value =>
                         !newSources.ContainsKey(
                             ReferenceIdentity(value.binding))))
            {
                CharacterPresentationPoseSourceBinding binding =
                    Resolve<CharacterPresentationPoseSourceBinding>(
                        removed.binding,
                        path + $".poseSources[{removed.name}].binding",
                        report);
                if (!binding)
                {
                    report.Error(
                        path + $".poseSources[{removed.name}]",
                        "presentation_pose_source_binding_missing",
                        "待删除Pose source不能解析到Profile-owned typed binding子资产。");
                    continue;
                }
                builder.Profile(
                    path + $".poseSources[{removed.name}]",
                    new RemoveProfileSourceBindingMutation(
                        target.id,
                        binding));
            }
            foreach (AgentPackagePoseSourceBinding source in target.poseSources
                         .Where(value =>
                             !oldSources.TryGetValue(
                                 ReferenceIdentity(value.binding),
                                 out AgentPackagePoseSourceBinding previous) ||
                             !Same(previous, value)))
            {
                CharacterPresentationPoseSourceBinding binding =
                    ConvertSource(source, poseGraph, rig, report);
                if (binding)
                {
                    bool replace = oldSources.ContainsKey(
                        ReferenceIdentity(source.binding));
                    builder.Profile(
                        path + $".poseSources[{source.name}]",
                        replace
                            ? new SetProfileSourceBindingMutation(
                                target.id,
                                binding)
                            : new CreateProfileSourceBindingMutation(
                                target.id,
                                binding));
                }
            }

            if (!Same(current.poseResources, target.poseResources))
            {
                builder.Profile(
                    path + ".poseResources",
                    new SetProfilePoseResourceBindingsMutation(
                        target.id,
                        ConvertResourceBindings(
                            target.poseResources,
                            poseGraph,
                            path + ".poseResources",
                            report)));
            }

            Dictionary<string, AgentPackageAnimationProducerBinding>
                oldProducers = Index(
                    current.actionProducers,
                    ProducerKey);
            Dictionary<string, AgentPackageAnimationProducerBinding>
                newProducers = Index(
                    target.actionProducers,
                    ProducerKey);
            foreach (AgentPackageAnimationProducerBinding removed in
                     current.actionProducers.Where(value =>
                         !newProducers.ContainsKey(ProducerKey(value))))
            {
                builder.Profile(
                    path + $".actionProducers[{ProducerKey(removed)}]",
                    new RemoveProfileProducerBindingMutation(
                        target.id,
                        new AnimationProducerId(
                            removed.timelineId,
                            removed.trackId)));
            }
            foreach (AgentPackageAnimationProducerBinding producer in
                     target.actionProducers.Where(value =>
                         !oldProducers.TryGetValue(
                             ProducerKey(value),
                             out AgentPackageAnimationProducerBinding previous) ||
                         !Same(previous, value)))
            {
                AnimationProducerPresentationBinding binding =
                    ConvertProducer(producer, report);
                if (binding != null)
                {
                    builder.Profile(
                        path + $".actionProducers[{ProducerKey(producer)}]",
                        new SetProfileProducerBindingMutation(
                            target.id,
                            binding));
                }
            }
        }
    }
}
