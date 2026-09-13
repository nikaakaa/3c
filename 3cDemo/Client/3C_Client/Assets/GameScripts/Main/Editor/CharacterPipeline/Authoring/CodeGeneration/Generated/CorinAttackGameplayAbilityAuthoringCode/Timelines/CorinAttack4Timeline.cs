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
        static void LoadTimelines_CorinAttack4TimelineResources(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.asset18 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinAttackGameplayAbilityDefinition/CorinAttack4Timeline/Attack4End.asset", 11400000L);
            generation.asset19 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinAttackGameplayAbilityDefinition/CorinAttack4Timeline/Attack4Main.asset", 11400000L);
            generation.asset20 = context.ResolveExternalAsset<UnityAnimationClip>("Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_Attack4_Inplace.anim", 7400000L);
            generation.asset21 = context.ResolveExternalAsset<UnityAnimationClip>("Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_Attack4_End_Inplace.anim", 7400000L);
        }

        static void BuildCreateTimelines_CorinAttack4Timeline42(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timeline3 = BtsmtlSkillAuthoringCode.EnsureTimeline(generation.graph29, "3a57e427-7c35-4910-99ac-68fca87b055e", "CorinAttack4Timeline");
        }

        static void BuildCreateTimelines_CorinAttack4Timeline48(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timelineData3 = generation.timeline3.Data;
            var timelineCatalog3 = TimelineTreeContractComposition.Create();
            generation.track15 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData3, timelineCatalog3, typeof(MotionCurveTrack), "34c03a9a-c089-425d-8a1d-b3a1eb588fa7", "Motion Curve");
            generation.clip33 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData3, timelineCatalog3, generation.track15, "2173d615-dd8c-4d5b-a550-58a798c0d69e", 89, generation.asset18, 282, 0, 0, 0);
            generation.clip34 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData3, timelineCatalog3, generation.track15, "bd920a87-f138-4df5-aad9-c3763973bedb", 0, generation.asset19, 89, 0, 0, 0);
            generation.track16 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData3, timelineCatalog3, typeof(MotionWarpTrack), "bfc839f6-52a8-4628-84a7-b6c6657ed6c4", "Motion Warp");
            generation.clip35 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData3, timelineCatalog3, generation.track16, "e28c94f9-544b-4d3c-8280-ac6cfb9f0a42", 9, null, 42, 0, 0, 0);
            generation.track17 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData3, timelineCatalog3, typeof(TreeTrack), "c450e49e-25c8-4a16-ad3f-acabb28a4a48", "Tree");
            generation.clip36 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData3, timelineCatalog3, generation.track17, "096aa081-0595-4592-b34d-a8521aca903b", 18, generation.graph30, 45, 0, 0, 0);
            generation.clip37 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData3, timelineCatalog3, generation.track17, "61799f08-5e23-4524-93ed-769aec46757a", 113, generation.graph31, 276, 0, 0, 0);
            generation.clip38 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData3, timelineCatalog3, generation.track17, "629ecaef-ed68-4ef8-bf6e-793719b5efd8", 83, generation.graph32, 276, 0, 0, 0);
            generation.clip39 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData3, timelineCatalog3, generation.track17, "8fefb458-0d9c-4568-8bc9-51e578f50a62", 90, generation.graph33, 133, 0, 0, 0);
            generation.track18 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData3, timelineCatalog3, typeof(ActionCueTrack), "d719f2f0-2c59-4289-bbf0-916f00a88cfb", "Action Cue");
            generation.clip40 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData3, timelineCatalog3, generation.track18, "0327a31c-e074-45a0-acb8-8a1d32fb5524", 24, null);
            generation.clip41 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData3, timelineCatalog3, generation.track18, "6d7b49fa-c06f-4f82-bc71-6066f64be835", 18, null);
            generation.track19 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData3, timelineCatalog3, typeof(AnimationTrack), "eff79947-1ee6-452b-91b0-6582c93e648e", "Animation");
            generation.clip42 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData3, timelineCatalog3, generation.track19, "2a6f8273-f664-4948-8aa3-9fdedd24b8d2", 0, generation.asset20);
            generation.clip43 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData3, timelineCatalog3, generation.track19, "8a0b2cd5-0900-4584-bdaf-2789f7288743", 83, generation.asset21);
        }

        static void BuildConfigureTimelines_CorinAttack4Timeline20(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timelineData3.ConfigureAuthoringIdentity("3a57e427-7c35-4910-99ac-68fca87b055e");
            BtsmtlSkillAuthoringCode.ConfigureMotionCurveSource((MotionCurveClip)generation.clip33, generation.asset18, 0f, 3.2166667f);
            ((MotionCurveClip)generation.clip33).CurveId = "Attack4End";
            BtsmtlSkillAuthoringCode.ConfigureMotionCurveSource((MotionCurveClip)generation.clip34, generation.asset19, 0f, 1.48333335f);
            ((MotionCurveClip)generation.clip34).CurveId = "Attack4Main";
            ((MotionWarpClip)generation.clip35).ConfigureAuthoring(MotionWarpTranslationMode.Disabled, MotionWarpTargetOffsetSpace.ApproachDirection, MotionWarpRotationMode.FaceTarget, MotionWarpRotationMethod.ProgressCurve, new Vector2(0f, 1.25f), 0f, 1.5f, 90f, 0f, MotionWarpLimitPolicy.ApplyClamped, null, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.333333f, 0.548504f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.666667f, 0.836836f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineCurveChannelCatalog.Require("motion-warp.yaw-progress").Replace(generation.clip35, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.333333f, 0.548504f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.666667f, 0.836836f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            ((TreeClip)generation.clip36).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((TreeClip)generation.clip37).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((TreeClip)generation.clip38).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((TreeClip)generation.clip39).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((ActionCueClip)generation.clip40).CueId = "Attack4CameraCue";
            ((ActionCueClip)generation.clip40).CueType = "Camera";
            ((ActionCueClip)generation.clip41).CueId = "Attack4GameplayCue";
            ((AnimationTrack)generation.track19).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)generation.track19).SetAnimationSlotId("corin.full-body-action");
            ((TimelineAnimationClip)generation.clip42).Clip = generation.asset20;
            ((TimelineAnimationClip)generation.clip42).BlendProfileId = "corin.animation-rig.action-blend-profile";
            TimelineCurveChannelCatalog.Require("animation.weight").Replace(generation.clip42, new AnimationCurve(new[] { new Keyframe(0f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineCurveChannelCatalog.Require("animation.ease-in").Replace(generation.clip42, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineCurveChannelCatalog.Require("animation.ease-out").Replace(generation.clip42, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            ((TimelineAnimationClip)generation.clip43).Clip = generation.asset21;
            ((TimelineAnimationClip)generation.clip43).BlendProfileId = "corin.animation-rig.action-blend-profile";
        }

        static void BuildBindTimelines_CorinAttack4Timeline4(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((BtsmtlSkillTimelineFlowNode)generation.node137).Configure(generation.timeline3, BtsmtlSkillTimelineOwnership.Private, null, TimelinePlaybackMode.Once);
        }

        static void BuildBindTimelines_CorinAttack4Timeline14(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            MotionWarpAuthoring.BindSource(generation.timelineData3, (MotionWarpClip)generation.clip35, (MotionCurveClip)generation.clip34);
        }

        static void BuildRootBindingTimelines_CorinAttack4Timeline41(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneTimeline(generation.timelineData3, new[] { "34c03a9a-c089-425d-8a1d-b3a1eb588fa7", "bfc839f6-52a8-4628-84a7-b6c6657ed6c4", "c450e49e-25c8-4a16-ad3f-acabb28a4a48", "d719f2f0-2c59-4289-bbf0-916f00a88cfb", "eff79947-1ee6-452b-91b0-6582c93e648e" }, new[] { "2173d615-dd8c-4d5b-a550-58a798c0d69e", "bd920a87-f138-4df5-aad9-c3763973bedb", "e28c94f9-544b-4d3c-8280-ac6cfb9f0a42", "096aa081-0595-4592-b34d-a8521aca903b", "61799f08-5e23-4524-93ed-769aec46757a", "629ecaef-ed68-4ef8-bf6e-793719b5efd8", "8fefb458-0d9c-4568-8bc9-51e578f50a62", "0327a31c-e074-45a0-acb8-8a1d32fb5524", "6d7b49fa-c06f-4f82-bc71-6066f64be835", "2a6f8273-f664-4948-8aa3-9fdedd24b8d2", "8a0b2cd5-0900-4584-bdaf-2789f7288743" }, Array.Empty<string>(), Array.Empty<string>());
        }
    }
}
