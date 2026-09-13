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
        static void LoadTimelines_CorinAttack1TimelineResources(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.asset5 = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Graphs/SharedTimelines/CorinAttack1Timeline.asset", 11400000L);
            generation.asset6 = context.ResolveExternalAsset<UnityAnimationClip>("Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_Attack1_Inplace.anim", 7400000L);
            generation.asset7 = context.ResolveExternalAsset<UnityAnimationClip>("Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_Attack1_End_Inplace.anim", 7400000L);
            generation.asset8 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinAttack1Timeline/CorinAttack1Timeline/Attack1Main.asset", 11400000L);
            generation.asset9 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinAttack1Timeline/CorinAttack1Timeline/Attack1End.asset", 11400000L);
        }

        static void BuildCreateTimelines_CorinAttack1Timeline39(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timeline = generation.asset5;
        }

        static void BuildCreateTimelines_CorinAttack1Timeline45(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timelineData = generation.timeline.Data;
            var timelineCatalog = TimelineTreeContractComposition.Create();
            generation.track = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData, timelineCatalog, typeof(AnimationTrack), "0811fba7-c4c7-4cc3-9714-f93b9da4d4ab", "Animation");
            generation.clip = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData, timelineCatalog, generation.track, "e34b7999-c4fa-4f8b-8425-5d7ed8de8159", 0, generation.asset6);
            generation.clip1 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData, timelineCatalog, generation.track, "74f288a4-3126-453a-9858-109479d652e6", 43, generation.asset7);
            generation.track1 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData, timelineCatalog, typeof(MotionWarpTrack), "244e81ee-5fc6-498f-b813-a103023881c5", "Motion Warp");
            generation.clip2 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData, timelineCatalog, generation.track1, "eb07c8dd-3527-4470-8bc4-c44f6665d937", 7, null, 32, 0, 0, 0);
            generation.track2 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData, timelineCatalog, typeof(MotionCurveTrack), "9673f395-2c0b-46b8-bcc3-f10062581bab", "Motion Curve");
            generation.clip3 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData, timelineCatalog, generation.track2, "142218e6-6644-479b-b721-19d91be03a15", 0, generation.asset8, 49, 0, 0, 0);
            generation.clip4 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData, timelineCatalog, generation.track2, "7df37ded-43d4-4c6c-83a4-0227e64ccb8a", 49, generation.asset9, 168, 0, 0, 0);
            generation.track3 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData, timelineCatalog, typeof(ActionCueTrack), "e0242e32-6b30-43e8-a125-06cd47f45318", "Action Cue");
            generation.clip5 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData, timelineCatalog, generation.track3, "d662b1f3-3254-4436-9fd2-17a016248d6f", 18, null);
            generation.clip6 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData, timelineCatalog, generation.track3, "a50fcf55-d4cd-4ce2-b3af-86b9d2209813", 24, null);
            generation.track4 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData, timelineCatalog, typeof(TreeTrack), "ec923db2-74bc-40a3-8826-039fc541c948", "Tree");
            generation.clip7 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData, timelineCatalog, generation.track4, "d7c8216a-4bdf-4676-809f-f4828d6e986a", 18, generation.graph3, 45, 0, 0, 0);
            generation.clip8 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData, timelineCatalog, generation.track4, "ffc20627-9e1a-4ab0-be42-8c4af9e6f1b0", 50, generation.graph4, 93, 0, 0, 0);
            generation.clip9 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData, timelineCatalog, generation.track4, "de283c70-27e9-453b-b188-24736779ffa3", 73, generation.graph5, 162, 0, 0, 0);
            generation.clip10 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData, timelineCatalog, generation.track4, "3509c34c-84ff-4a89-8100-cc549a0f8c85", 43, generation.graph6, 162, 0, 0, 0);
        }

        static void BuildConfigureTimelines_CorinAttack1Timeline17(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timelineData.ConfigureAuthoringIdentity("10f4cb90-8b9a-4944-b77c-14efc9a3124d");
            generation.timelineData.Scale = 0.117767066f;
            BtsmtlSkillAuthoringCode.EnsureSection(generation.timelineData, "c196a52f-4b06-4310-8a8a-8699ef6a5620", "Attack", 0, "");
            ((AnimationTrack)generation.track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)generation.track).SetAnimationSlotId("corin.full-body-action");
            ((TimelineAnimationClip)generation.clip).Clip = generation.asset6;
            ((TimelineAnimationClip)generation.clip).BlendProfileId = "corin.animation-rig.action-blend-profile";
            TimelineCurveChannelCatalog.Require("animation.weight").Replace(generation.clip, new AnimationCurve(new[] { new Keyframe(0f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineCurveChannelCatalog.Require("animation.ease-in").Replace(generation.clip, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineCurveChannelCatalog.Require("animation.ease-out").Replace(generation.clip, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            ((TimelineAnimationClip)generation.clip1).Clip = generation.asset7;
            ((TimelineAnimationClip)generation.clip1).BlendProfileId = "corin.animation-rig.action-blend-profile";
            ((MotionWarpClip)generation.clip2).ConfigureAuthoring(MotionWarpTranslationMode.Disabled, MotionWarpTargetOffsetSpace.ApproachDirection, MotionWarpRotationMode.FaceTarget, MotionWarpRotationMethod.ProgressCurve, new Vector2(0f, 1.25f), 0f, 1.5f, 90f, 0f, MotionWarpLimitPolicy.ApplyClamped, null, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.333333f, 0.571906f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.666667f, 0.842683f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineCurveChannelCatalog.Require("motion-warp.yaw-progress").Replace(generation.clip2, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.333333f, 0.571906f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.666667f, 0.842683f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            BtsmtlSkillAuthoringCode.ConfigureMotionCurveSource((MotionCurveClip)generation.clip3, generation.asset8, 0f, 0.816666663f);
            ((MotionCurveClip)generation.clip3).CurveId = "Attack1Main";
            BtsmtlSkillAuthoringCode.ConfigureMotionCurveSource((MotionCurveClip)generation.clip4, generation.asset9, 0f, 1.98333335f);
            ((MotionCurveClip)generation.clip4).CurveId = "Attack1End";
            ((ActionCueClip)generation.clip5).CueId = "Attack1GameplayCue";
            ((ActionCueClip)generation.clip6).CueId = "Attack1CameraCue";
            ((ActionCueClip)generation.clip6).CueType = "Camera";
            ((TreeClip)generation.clip7).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((TreeClip)generation.clip8).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((TreeClip)generation.clip9).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((TreeClip)generation.clip10).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
        }

        static void BuildBindTimelines_CorinAttack1Timeline1(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((BtsmtlSkillTimelineFlowNode)generation.node5).Configure(generation.timeline, BtsmtlSkillTimelineOwnership.Shared, null, TimelinePlaybackMode.Once);
        }

        static void BuildBindTimelines_CorinAttack1Timeline11(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            MotionWarpAuthoring.BindSource(generation.timelineData, (MotionWarpClip)generation.clip2, (MotionCurveClip)generation.clip3);
        }

        static void BuildRootBindingTimelines_CorinAttack1Timeline38(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneTimeline(generation.timelineData, new[] { "0811fba7-c4c7-4cc3-9714-f93b9da4d4ab", "244e81ee-5fc6-498f-b813-a103023881c5", "9673f395-2c0b-46b8-bcc3-f10062581bab", "e0242e32-6b30-43e8-a125-06cd47f45318", "ec923db2-74bc-40a3-8826-039fc541c948" }, new[] { "e34b7999-c4fa-4f8b-8425-5d7ed8de8159", "74f288a4-3126-453a-9858-109479d652e6", "eb07c8dd-3527-4470-8bc4-c44f6665d937", "142218e6-6644-479b-b721-19d91be03a15", "7df37ded-43d4-4c6c-83a4-0227e64ccb8a", "d662b1f3-3254-4436-9fd2-17a016248d6f", "a50fcf55-d4cd-4ce2-b3af-86b9d2209813", "d7c8216a-4bdf-4676-809f-f4828d6e986a", "ffc20627-9e1a-4ab0-be42-8c4af9e6f1b0", "de283c70-27e9-453b-b188-24736779ffa3", "3509c34c-84ff-4a89-8100-cc549a0f8c85" }, new[] { "c196a52f-4b06-4310-8a8a-8699ef6a5620" }, Array.Empty<string>());
        }
    }
}
