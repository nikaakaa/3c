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
        static void LoadTimelines_CorinAttack2TimelineResources(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.asset10 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinAttackGameplayAbilityDefinition/CorinAttack2Timeline/Attack2End.asset", 11400000L);
            generation.asset11 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinAttackGameplayAbilityDefinition/CorinAttack2Timeline/Attack2Main.asset", 11400000L);
            generation.asset12 = context.ResolveExternalAsset<UnityAnimationClip>("Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_Attack2_End_Inplace.anim", 7400000L);
            generation.asset13 = context.ResolveExternalAsset<UnityAnimationClip>("Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_Attack2_Inplace.anim", 7400000L);
        }

        static void BuildCreateTimelines_CorinAttack2Timeline40(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timeline1 = BtsmtlSkillAuthoringCode.EnsureTimeline(generation.graph11, "21349b9d-8c58-4616-b8f3-6df7d560bb74", "CorinAttack2Timeline");
        }

        static void BuildCreateTimelines_CorinAttack2Timeline46(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timelineData1 = generation.timeline1.Data;
            var timelineCatalog1 = TimelineTreeContractComposition.Create();
            generation.track5 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData1, timelineCatalog1, typeof(MotionCurveTrack), "115a275c-e3c1-4f49-a797-730d8740ad60", "Motion Curve");
            generation.clip11 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData1, timelineCatalog1, generation.track5, "7a5caece-fc2f-4860-837c-755f36421dda", 48, generation.asset10, 173, 0, 0, 0);
            generation.clip12 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData1, timelineCatalog1, generation.track5, "a56930a0-7209-427e-977e-81bb4488c2ad", 0, generation.asset11, 48, 0, 0, 0);
            generation.track6 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData1, timelineCatalog1, typeof(AnimationTrack), "5847542b-70e3-49bf-98b9-26bb30d68c01", "Animation");
            generation.clip13 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData1, timelineCatalog1, generation.track6, "2296291d-3c89-4ace-811f-93c19e3cccf8", 42, generation.asset12);
            generation.clip14 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData1, timelineCatalog1, generation.track6, "6a375bc8-4cc1-4786-a496-ce0fa05d6e36", 0, generation.asset13);
            generation.track7 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData1, timelineCatalog1, typeof(ActionCueTrack), "c757d385-68cd-4fda-9ada-9712ef6b3cda", "Action Cue");
            generation.clip15 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData1, timelineCatalog1, generation.track7, "0f0ebfa0-4556-4883-a52d-307a8d715bfd", 18, null);
            generation.clip16 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData1, timelineCatalog1, generation.track7, "f4f35ee7-ee1f-4992-b562-379aebbdc5b1", 24, null);
            generation.track8 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData1, timelineCatalog1, typeof(MotionWarpTrack), "db51f858-48f4-430b-9011-c54a40c3cdcb", "Motion Warp");
            generation.clip17 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData1, timelineCatalog1, generation.track8, "28fc08a4-055f-4a38-9371-77efd7c6914a", 5, null, 29, 0, 0, 0);
            generation.track9 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData1, timelineCatalog1, typeof(TreeTrack), "e2c48df9-bb8e-4aa3-92bf-ab3562310254", "Tree");
            generation.clip18 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData1, timelineCatalog1, generation.track9, "206e350b-009c-44b5-83b8-9f7f3b17b1b5", 42, generation.graph12, 167, 0, 0, 0);
            generation.clip19 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData1, timelineCatalog1, generation.track9, "652e6726-9013-46cf-a691-d075a739da68", 18, generation.graph13, 45, 0, 0, 0);
            generation.clip20 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData1, timelineCatalog1, generation.track9, "d88a8003-2e4d-456f-a9ba-741c5ef42426", 49, generation.graph14, 92, 0, 0, 0);
            generation.clip21 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData1, timelineCatalog1, generation.track9, "e73aea15-2e02-4e95-aa19-bec673b83215", 72, generation.graph15, 167, 0, 0, 0);
        }

        static void BuildConfigureTimelines_CorinAttack2Timeline18(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timelineData1.ConfigureAuthoringIdentity("21349b9d-8c58-4616-b8f3-6df7d560bb74");
            BtsmtlSkillAuthoringCode.ConfigureMotionCurveSource((MotionCurveClip)generation.clip11, generation.asset10, 0f, 2.08333325f);
            ((MotionCurveClip)generation.clip11).CurveId = "Attack2End";
            BtsmtlSkillAuthoringCode.ConfigureMotionCurveSource((MotionCurveClip)generation.clip12, generation.asset11, 0f, 0.8f);
            ((MotionCurveClip)generation.clip12).CurveId = "Attack2Main";
            ((AnimationTrack)generation.track6).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)generation.track6).SetAnimationSlotId("corin.full-body-action");
            ((TimelineAnimationClip)generation.clip13).Clip = generation.asset12;
            ((TimelineAnimationClip)generation.clip13).BlendProfileId = "corin.animation-rig.action-blend-profile";
            ((TimelineAnimationClip)generation.clip14).Clip = generation.asset13;
            ((TimelineAnimationClip)generation.clip14).BlendProfileId = "corin.animation-rig.action-blend-profile";
            TimelineCurveChannelCatalog.Require("animation.weight").Replace(generation.clip14, new AnimationCurve(new[] { new Keyframe(0f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineCurveChannelCatalog.Require("animation.ease-in").Replace(generation.clip14, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineCurveChannelCatalog.Require("animation.ease-out").Replace(generation.clip14, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            ((ActionCueClip)generation.clip15).CueId = "Attack2GameplayCue";
            ((ActionCueClip)generation.clip16).CueId = "Attack2CameraCue";
            ((ActionCueClip)generation.clip16).CueType = "Camera";
            ((MotionWarpClip)generation.clip17).ConfigureAuthoring(MotionWarpTranslationMode.Disabled, MotionWarpTargetOffsetSpace.ApproachDirection, MotionWarpRotationMode.FaceTarget, MotionWarpRotationMethod.ProgressCurve, new Vector2(0f, 1.25f), 0f, 1.5f, 90f, 0f, MotionWarpLimitPolicy.ApplyClamped, null, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.333333f, 0.638016f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.666667f, 0.909659f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineCurveChannelCatalog.Require("motion-warp.yaw-progress").Replace(generation.clip17, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.333333f, 0.638016f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.666667f, 0.909659f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            ((TreeClip)generation.clip18).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((TreeClip)generation.clip19).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((TreeClip)generation.clip20).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((TreeClip)generation.clip21).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
        }

        static void BuildBindTimelines_CorinAttack2Timeline2(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((BtsmtlSkillTimelineFlowNode)generation.node49).Configure(generation.timeline1, BtsmtlSkillTimelineOwnership.Private, null, TimelinePlaybackMode.Once);
        }

        static void BuildBindTimelines_CorinAttack2Timeline12(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            MotionWarpAuthoring.BindSource(generation.timelineData1, (MotionWarpClip)generation.clip17, (MotionCurveClip)generation.clip12);
        }

        static void BuildRootBindingTimelines_CorinAttack2Timeline39(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneTimeline(generation.timelineData1, new[] { "115a275c-e3c1-4f49-a797-730d8740ad60", "5847542b-70e3-49bf-98b9-26bb30d68c01", "c757d385-68cd-4fda-9ada-9712ef6b3cda", "db51f858-48f4-430b-9011-c54a40c3cdcb", "e2c48df9-bb8e-4aa3-92bf-ab3562310254" }, new[] { "7a5caece-fc2f-4860-837c-755f36421dda", "a56930a0-7209-427e-977e-81bb4488c2ad", "2296291d-3c89-4ace-811f-93c19e3cccf8", "6a375bc8-4cc1-4786-a496-ce0fa05d6e36", "0f0ebfa0-4556-4883-a52d-307a8d715bfd", "f4f35ee7-ee1f-4992-b562-379aebbdc5b1", "28fc08a4-055f-4a38-9371-77efd7c6914a", "206e350b-009c-44b5-83b8-9f7f3b17b1b5", "652e6726-9013-46cf-a691-d075a739da68", "d88a8003-2e4d-456f-a9ba-741c5ef42426", "e73aea15-2e02-4e95-aa19-bec673b83215" }, Array.Empty<string>(), Array.Empty<string>());
        }
    }
}
