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
        static void LoadTimelines_CorinAttack5TimelineResources(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.asset22 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinAttackGameplayAbilityDefinition/CorinAttack5Timeline/Attack5End.asset", 11400000L);
            generation.asset23 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinAttackGameplayAbilityDefinition/CorinAttack5Timeline/Attack5Main.asset", 11400000L);
            generation.asset24 = context.ResolveExternalAsset<UnityAnimationClip>("Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_Attack5_Inplace.anim", 7400000L);
            generation.asset25 = context.ResolveExternalAsset<UnityAnimationClip>("Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_Attack5_End_Inplace.anim", 7400000L);
        }

        static void BuildCreateTimelines_CorinAttack5Timeline43(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timeline4 = BtsmtlSkillAuthoringCode.EnsureTimeline(generation.graph38, "cf8c408a-1d33-4368-ac5f-17c6bf4f1783", "CorinAttack5Timeline");
        }

        static void BuildCreateTimelines_CorinAttack5Timeline49(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timelineData4 = generation.timeline4.Data;
            var timelineCatalog4 = TimelineTreeContractComposition.Create();
            generation.track20 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData4, timelineCatalog4, typeof(MotionWarpTrack), "120a9675-9ebe-4cef-bcd1-a268e85eb38e", "Motion Warp");
            generation.clip44 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData4, timelineCatalog4, generation.track20, "86240efe-6475-4499-bc61-9a80a91c84b8", 10, null, 40, 0, 0, 0);
            generation.track21 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData4, timelineCatalog4, typeof(MotionCurveTrack), "206e763b-8a5e-46cd-ab6d-a233689dd878", "Motion Curve");
            generation.clip45 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData4, timelineCatalog4, generation.track21, "20077501-0b2e-42d9-9a55-82bac742453d", 125, generation.asset22, 212, 0, 0, 0);
            generation.clip46 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData4, timelineCatalog4, generation.track21, "765bebe5-8d5d-4ce3-92a5-dc8a51dc1664", 0, generation.asset23, 125, 0, 0, 0);
            generation.track22 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData4, timelineCatalog4, typeof(TreeTrack), "708f3104-14ef-463f-b4f7-131423ccb582", "Tree");
            generation.clip47 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData4, timelineCatalog4, generation.track22, "2e4177f6-76fe-4d0a-bbd7-56e308dfbd8f", 18, generation.graph39, 45, 0, 0, 0);
            generation.clip48 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData4, timelineCatalog4, generation.track22, "5e9a19b0-82aa-43f4-9775-36ae151985ce", 119, generation.graph40, 206, 0, 0, 0);
            generation.clip49 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData4, timelineCatalog4, generation.track22, "c6eed989-9343-468e-aff2-4174451b29c4", 149, generation.graph41, 206, 0, 0, 0);
            generation.track23 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData4, timelineCatalog4, typeof(ActionCueTrack), "c3022685-aa54-48b3-85b6-1211ab1f6779", "Action Cue");
            generation.clip50 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData4, timelineCatalog4, generation.track23, "3b26dcef-1d55-478e-a59a-5312a72f9bb4", 24, null);
            generation.clip51 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData4, timelineCatalog4, generation.track23, "d9f6c7d9-4b72-4197-ba9e-9b43da28ca6b", 18, null);
            generation.track24 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData4, timelineCatalog4, typeof(AnimationTrack), "dc4fde81-bab5-405d-ad63-6bf864fd536d", "Animation");
            generation.clip52 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData4, timelineCatalog4, generation.track24, "990ad51a-09bb-4b0e-9f66-c156cb4dcf20", 0, generation.asset24);
            generation.clip53 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData4, timelineCatalog4, generation.track24, "ae42d57e-8a2c-4cfc-958b-c2d14718a142", 119, generation.asset25);
        }

        static void BuildConfigureTimelines_CorinAttack5Timeline21(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timelineData4.ConfigureAuthoringIdentity("cf8c408a-1d33-4368-ac5f-17c6bf4f1783");
            ((MotionWarpClip)generation.clip44).ConfigureAuthoring(MotionWarpTranslationMode.Disabled, MotionWarpTargetOffsetSpace.ApproachDirection, MotionWarpRotationMode.FaceTarget, MotionWarpRotationMethod.ProgressCurve, new Vector2(0f, 1.25f), 0f, 1.5f, 90f, 0f, MotionWarpLimitPolicy.ApplyClamped, null, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.333333f, 0.581287f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.666667f, 0.857153f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineCurveChannelCatalog.Require("motion-warp.yaw-progress").Replace(generation.clip44, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.333333f, 0.581287f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.666667f, 0.857153f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            BtsmtlSkillAuthoringCode.ConfigureMotionCurveSource((MotionCurveClip)generation.clip45, generation.asset22, 0f, 1.45f);
            ((MotionCurveClip)generation.clip45).CurveId = "Attack5End";
            BtsmtlSkillAuthoringCode.ConfigureMotionCurveSource((MotionCurveClip)generation.clip46, generation.asset23, 0f, 2.08333325f);
            ((MotionCurveClip)generation.clip46).CurveId = "Attack5Main";
            ((TreeClip)generation.clip47).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((TreeClip)generation.clip48).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((TreeClip)generation.clip49).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((ActionCueClip)generation.clip50).CueId = "Attack5CameraCue";
            ((ActionCueClip)generation.clip50).CueType = "Camera";
            ((ActionCueClip)generation.clip51).CueId = "Attack5GameplayCue";
            ((AnimationTrack)generation.track24).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)generation.track24).SetAnimationSlotId("corin.full-body-action");
            ((TimelineAnimationClip)generation.clip52).Clip = generation.asset24;
            ((TimelineAnimationClip)generation.clip52).BlendProfileId = "corin.animation-rig.action-blend-profile";
            TimelineCurveChannelCatalog.Require("animation.weight").Replace(generation.clip52, new AnimationCurve(new[] { new Keyframe(0f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineCurveChannelCatalog.Require("animation.ease-in").Replace(generation.clip52, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineCurveChannelCatalog.Require("animation.ease-out").Replace(generation.clip52, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            ((TimelineAnimationClip)generation.clip53).Clip = generation.asset25;
            ((TimelineAnimationClip)generation.clip53).BlendProfileId = "corin.animation-rig.action-blend-profile";
        }

        static void BuildBindTimelines_CorinAttack5Timeline5(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((BtsmtlSkillTimelineFlowNode)generation.node181).Configure(generation.timeline4, BtsmtlSkillTimelineOwnership.Private, null, TimelinePlaybackMode.Once);
        }

        static void BuildBindTimelines_CorinAttack5Timeline15(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            MotionWarpAuthoring.BindSource(generation.timelineData4, (MotionWarpClip)generation.clip44, (MotionCurveClip)generation.clip46);
        }

        static void BuildRootBindingTimelines_CorinAttack5Timeline42(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneTimeline(generation.timelineData4, new[] { "120a9675-9ebe-4cef-bcd1-a268e85eb38e", "206e763b-8a5e-46cd-ab6d-a233689dd878", "708f3104-14ef-463f-b4f7-131423ccb582", "c3022685-aa54-48b3-85b6-1211ab1f6779", "dc4fde81-bab5-405d-ad63-6bf864fd536d" }, new[] { "86240efe-6475-4499-bc61-9a80a91c84b8", "20077501-0b2e-42d9-9a55-82bac742453d", "765bebe5-8d5d-4ce3-92a5-dc8a51dc1664", "2e4177f6-76fe-4d0a-bbd7-56e308dfbd8f", "5e9a19b0-82aa-43f4-9775-36ae151985ce", "c6eed989-9343-468e-aff2-4174451b29c4", "3b26dcef-1d55-478e-a59a-5312a72f9bb4", "d9f6c7d9-4b72-4197-ba9e-9b43da28ca6b", "990ad51a-09bb-4b0e-9f66-c156cb4dcf20", "ae42d57e-8a2c-4cfc-958b-c2d14718a142" }, Array.Empty<string>(), Array.Empty<string>());
        }
    }
}
