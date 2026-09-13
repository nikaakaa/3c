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
    public sealed partial class CorinDodgeBackGameplayAbilityAuthoringCode
    {
        static void LoadTimelines_CorinDodgeBackTimelineResources(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.asset1 = context.ResolveExternalAsset<UnityAnimationClip>("Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_DodgeBack_Inplace.anim", 7400000L);
            generation.asset2 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinDodgeBackGameplayAbilityDefinition/CorinDodgeBackTimeline/DodgeBack.asset", 11400000L);
        }

        static void BuildCreateTimelines_CorinDodgeBackTimeline7(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timeline = BtsmtlSkillAuthoringCode.EnsureTimeline(generation.graph1, "fdf10e49-c270-46d9-bd2e-5e45e13c7a97", "CorinDodgeBackTimeline");
        }

        static void BuildCreateTimelines_CorinDodgeBackTimeline9(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timelineData = generation.timeline.Data;
            var timelineCatalog = TimelineTreeContractComposition.Create();
            generation.track = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData, timelineCatalog, typeof(AnimationTrack), "82f04395-f39f-487a-8112-e45882a37deb", "Animation");
            generation.clip = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData, timelineCatalog, generation.track, "b0ee4319-922b-410e-8c0c-b4fe70eb7504", 0, generation.asset1);
            generation.track1 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData, timelineCatalog, typeof(MotionCurveTrack), "8f2a9050-893a-41e1-be05-57312ab21153", "Motion Curve");
            generation.clip1 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData, timelineCatalog, generation.track1, "ca9b83e7-d1c4-4224-b62d-a51b4a4b9e0d", 0, generation.asset2, 141, 0, 0, 0);
            generation.track2 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData, timelineCatalog, typeof(TreeTrack), "a636f440-208c-47db-88c1-afe9792423df", "Decision");
            generation.clip2 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData, timelineCatalog, generation.track2, "180ec5ff-8aca-43b2-93aa-32d2354b4851", 6, generation.graph2, 45, 0, 0, 0);
            generation.clip3 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData, timelineCatalog, generation.track2, "b88aeaf5-d4a4-4a9d-a706-d6da6cb8072a", 45, generation.graph3, 141, 0, 0, 0);
        }

        static void BuildConfigureTimelines_CorinDodgeBackTimeline2(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timelineData.ConfigureAuthoringIdentity("fdf10e49-c270-46d9-bd2e-5e45e13c7a97");
            ((AnimationTrack)generation.track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)generation.track).SetAnimationSlotId("corin.full-body-action");
            ((TimelineAnimationClip)generation.clip).Clip = generation.asset1;
            ((TimelineAnimationClip)generation.clip).BlendProfileId = "corin.animation-rig.action-blend-profile";
            BtsmtlSkillAuthoringCode.ConfigureMotionCurveSource((MotionCurveClip)generation.clip1, generation.asset2, 0f, 2.35f);
            ((MotionCurveClip)generation.clip1).CurveId = "DodgeBack";
            ((TreeClip)generation.clip2).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((TreeClip)generation.clip3).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
        }

        static void BuildBindTimelines_CorinDodgeBackTimeline1(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((BtsmtlSkillTimelineFlowNode)generation.node4).Configure(generation.timeline, BtsmtlSkillTimelineOwnership.Private, null, TimelinePlaybackMode.Once);
        }

        static void BuildRootBindingTimelines_CorinDodgeBackTimeline6(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneTimeline(generation.timelineData, new[] { "82f04395-f39f-487a-8112-e45882a37deb", "8f2a9050-893a-41e1-be05-57312ab21153", "a636f440-208c-47db-88c1-afe9792423df" }, new[] { "b0ee4319-922b-410e-8c0c-b4fe70eb7504", "ca9b83e7-d1c4-4224-b62d-a51b4a4b9e0d", "180ec5ff-8aca-43b2-93aa-32d2354b4851", "b88aeaf5-d4a4-4a9d-a706-d6da6cb8072a" }, Array.Empty<string>(), Array.Empty<string>());
        }
    }
}
