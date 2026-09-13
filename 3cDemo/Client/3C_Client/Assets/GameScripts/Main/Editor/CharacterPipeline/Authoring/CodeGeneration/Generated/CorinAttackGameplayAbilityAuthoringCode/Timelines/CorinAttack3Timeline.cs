using System;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration;
using TimelineAnimationClip = BTSMTL.Timeline.AnimationClip;
using UnityObject = UnityEngine.Object;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using FlowCanvas.Nodes;
using NodeCanvas.Framework;
using ThirdPersonCamera;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackGameplayAbilityAuthoringCode
    {
        static void LoadTimelines_CorinAttack3TimelineResources(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.asset14 = context.ResolveExternalAsset<UnityAnimationClip>("Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_Attack3_Inplace.anim", 7400000L);
            generation.asset15 = context.ResolveExternalAsset<UnityAnimationClip>("Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_Attack3_End_Inplace.anim", 7400000L);
            generation.asset16 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinAttackGameplayAbilityDefinition/CorinAttack3Timeline/Attack3End.asset", 11400000L);
            generation.asset17 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinAttackGameplayAbilityDefinition/CorinAttack3Timeline/Attack3Main.asset", 11400000L);
        }

        static void BuildCreateTimelines_CorinAttack3Timeline41(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timeline2 = BtsmtlSkillAuthoringCode.EnsureTimeline(generation.graph20, "c5761f3c-7517-4803-9e3b-019b66f52d41", "CorinAttack3Timeline");
        }

        static void BuildCreateTimelines_CorinAttack3Timeline47(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timelineData2 = generation.timeline2.Data;
            var timelineCatalog2 = TimelineTreeContractComposition.Create();
            generation.track10 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData2, timelineCatalog2, typeof(MotionWarpTrack), "afefc7cb-1b63-459d-8028-5797d89ef71b", "Motion Warp");
            generation.clip22 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData2, timelineCatalog2, generation.track10, "b7cc37e0-f20d-40a9-afd5-c6d2e659d99d", 6, null, 39, 0, 0, 0);
            generation.track11 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData2, timelineCatalog2, typeof(AnimationTrack), "c1c596d2-39fd-4c8b-825e-149b852e700c", "Animation");
            generation.clip23 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData2, timelineCatalog2, generation.track11, "012703eb-3a08-40ed-ad50-8a9b43a395c3", 0, generation.asset14);
            generation.clip24 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData2, timelineCatalog2, generation.track11, "722274ab-789c-4400-b6d4-eb1ab715d68f", 75, generation.asset15);
            generation.track12 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData2, timelineCatalog2, typeof(MotionCurveTrack), "c3098ce4-4b36-4dc9-8151-947f0406590c", "Motion Curve");
            generation.clip25 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData2, timelineCatalog2, generation.track12, "2c0107ca-fde7-4043-8dd4-1939f2663551", 81, generation.asset16, 206, 0, 0, 0);
            generation.clip26 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData2, timelineCatalog2, generation.track12, "373e6fc6-7fc7-490d-88ef-b4a48b87fcfd", 0, generation.asset17, 81, 0, 0, 0);
            generation.track13 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData2, timelineCatalog2, typeof(ActionCueTrack), "e638bdfb-9178-4723-99e2-688a04b0194e", "Action Cue");
            generation.clip27 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData2, timelineCatalog2, generation.track13, "4dcfd64b-50d3-4b42-befd-85ccbc5b3829", 24, null);
            generation.clip28 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData2, timelineCatalog2, generation.track13, "69479831-dd75-420d-85bf-2ccffe7fc331", 18, null);
            generation.track14 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData2, timelineCatalog2, typeof(TreeTrack), "e82bc011-e089-491e-8138-47499074b343", "Tree");
            generation.clip29 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData2, timelineCatalog2, generation.track14, "173a1e60-144d-4d42-8e97-d942787f4993", 75, generation.graph21, 200, 0, 0, 0);
            generation.clip30 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData2, timelineCatalog2, generation.track14, "5d3d23e0-5e31-4100-bb8e-b62b8b36d626", 105, generation.graph22, 200, 0, 0, 0);
            generation.clip31 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData2, timelineCatalog2, generation.track14, "7ec2401d-c627-41b5-85d9-d0abec3f7344", 18, generation.graph23, 45, 0, 0, 0);
            generation.clip32 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData2, timelineCatalog2, generation.track14, "c390cfd6-7d02-423f-98ec-c5c60f81a8ed", 82, generation.graph24, 125, 0, 0, 0);
        }

        static void BuildConfigureTimelines_CorinAttack3Timeline19(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timelineData2.ConfigureAuthoringIdentity("c5761f3c-7517-4803-9e3b-019b66f52d41");
            ((MotionWarpClip)generation.clip22).ConfigureAuthoring(MotionWarpTranslationMode.Disabled, MotionWarpTargetOffsetSpace.ApproachDirection, MotionWarpRotationMode.FaceTarget, MotionWarpRotationMethod.ProgressCurve, new Vector2(0f, 1.25f), 0f, 1.5f, 90f, 0f, MotionWarpLimitPolicy.ApplyClamped, null, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.333333f, 0.450166f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.666667f, 0.552825f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineCurveChannelCatalog.Require("motion-warp.yaw-progress").Replace(generation.clip22, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.333333f, 0.450166f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.666667f, 0.552825f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            ((AnimationTrack)generation.track11).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)generation.track11).SetAnimationSlotId("corin.full-body-action");
            ((TimelineAnimationClip)generation.clip23).Clip = generation.asset14;
            ((TimelineAnimationClip)generation.clip23).BlendProfileId = "corin.animation-rig.action-blend-profile";
            TimelineCurveChannelCatalog.Require("animation.weight").Replace(generation.clip23, new AnimationCurve(new[] { new Keyframe(0f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineCurveChannelCatalog.Require("animation.ease-in").Replace(generation.clip23, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineCurveChannelCatalog.Require("animation.ease-out").Replace(generation.clip23, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            ((TimelineAnimationClip)generation.clip24).Clip = generation.asset15;
            ((TimelineAnimationClip)generation.clip24).BlendProfileId = "corin.animation-rig.action-blend-profile";
            BtsmtlSkillAuthoringCode.ConfigureMotionCurveSource((MotionCurveClip)generation.clip25, generation.asset16, 0f, 2.08333325f);
            ((MotionCurveClip)generation.clip25).CurveId = "Attack3End";
            BtsmtlSkillAuthoringCode.ConfigureMotionCurveSource((MotionCurveClip)generation.clip26, generation.asset17, 0f, 1.35f);
            ((MotionCurveClip)generation.clip26).CurveId = "Attack3Main";
            ((ActionCueClip)generation.clip27).CueId = "Attack3CameraCue";
            ((ActionCueClip)generation.clip27).CueType = "Camera";
            ((ActionCueClip)generation.clip28).CueId = "Attack3GameplayCue";
            ((TreeClip)generation.clip29).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((TreeClip)generation.clip30).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((TreeClip)generation.clip31).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((TreeClip)generation.clip32).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
        }

        static void BuildBindTimelines_CorinAttack3Timeline3(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((BtsmtlSkillTimelineFlowNode)generation.node93).Configure(generation.timeline2, BtsmtlSkillTimelineOwnership.Private, null, TimelinePlaybackMode.Once);
        }

        static void BuildBindTimelines_CorinAttack3Timeline13(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            MotionWarpAuthoring.BindSource(generation.timelineData2, (MotionWarpClip)generation.clip22, (MotionCurveClip)generation.clip26);
        }

        static void BuildRootBindingTimelines_CorinAttack3Timeline40(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneTimeline(generation.timelineData2, new[] { "afefc7cb-1b63-459d-8028-5797d89ef71b", "c1c596d2-39fd-4c8b-825e-149b852e700c", "c3098ce4-4b36-4dc9-8151-947f0406590c", "e638bdfb-9178-4723-99e2-688a04b0194e", "e82bc011-e089-491e-8138-47499074b343" }, new[] { "b7cc37e0-f20d-40a9-afd5-c6d2e659d99d", "012703eb-3a08-40ed-ad50-8a9b43a395c3", "722274ab-789c-4400-b6d4-eb1ab715d68f", "2c0107ca-fde7-4043-8dd4-1939f2663551", "373e6fc6-7fc7-490d-88ef-b4a48b87fcfd", "4dcfd64b-50d3-4b42-befd-85ccbc5b3829", "69479831-dd75-420d-85bf-2ccffe7fc331", "173a1e60-144d-4d42-8e97-d942787f4993", "5d3d23e0-5e31-4100-bb8e-b62b8b36d626", "7ec2401d-c627-41b5-85d9-d0abec3f7344", "c390cfd6-7d02-423f-98ec-c5c60f81a8ed" }, Array.Empty<string>(), Array.Empty<string>());
        }
    }
}
