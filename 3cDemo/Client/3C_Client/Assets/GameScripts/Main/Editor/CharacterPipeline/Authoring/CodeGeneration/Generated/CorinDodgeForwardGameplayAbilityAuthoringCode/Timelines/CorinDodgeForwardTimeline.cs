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
    public sealed partial class CorinDodgeForwardGameplayAbilityAuthoringCode
    {
        static void LoadTimelines_CorinDodgeForwardTimelineResources(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.asset1 = context.ResolveExternalAsset<UnityAnimationClip>("Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_DodgeForward_Inplace.anim", 7400000L);
            generation.asset2 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinDodgeForwardGameplayAbilityDefinition/CorinDodgeForwardTimeline/DodgeForward.asset", 11400000L);
        }

        static void BuildCreateTimelines_CorinDodgeForwardTimeline7(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timeline = BtsmtlSkillAuthoringCode.EnsureTimeline(generation.graph1, "b871bfc9-f182-473b-8c7f-be176b620394", "CorinDodgeForwardTimeline");
        }

        static void BuildCreateTimelines_CorinDodgeForwardTimeline9(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timelineData = generation.timeline.Data;
            var timelineCatalog = TimelineTreeContractComposition.Create();
            generation.track = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData, timelineCatalog, typeof(TreeTrack), "1fabe64a-9df6-4bb3-a395-ebfa32d9874b", "Decision");
            generation.clip = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData, timelineCatalog, generation.track, "9080e5fb-1e6c-414a-bd9f-94cab1cf7c5c", 6, generation.graph2, 45, 0, 0, 0);
            generation.clip1 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData, timelineCatalog, generation.track, "913539bd-df57-4f13-b965-feafda823f36", 46, generation.graph3, 142, 0, 0, 0);
            generation.track1 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData, timelineCatalog, typeof(AnimationTrack), "8e4b0f0d-829b-4818-9a8b-39d9246447ea", "Animation");
            generation.clip2 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData, timelineCatalog, generation.track1, "e9d429f1-595e-475a-b647-444a6e069c92", 0, generation.asset1);
            generation.track2 = BtsmtlSkillAuthoringCode.EnsureTrack(generation.timelineData, timelineCatalog, typeof(MotionCurveTrack), "a0c961a6-0179-4f4d-8b75-76fa3a8bcd0a", "Motion Curve");
            generation.clip3 = BtsmtlSkillAuthoringCode.EnsureClip(generation.timelineData, timelineCatalog, generation.track2, "ea9a052b-5407-4a22-b1c2-f5bbce6e31bc", 0, generation.asset2, 141, 0, 0, 0);
        }

        static void BuildConfigureTimelines_CorinDodgeForwardTimeline2(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.timelineData.ConfigureAuthoringIdentity("b871bfc9-f182-473b-8c7f-be176b620394");
            ((TreeClip)generation.clip).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((TreeClip)generation.clip1).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((AnimationTrack)generation.track1).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)generation.track1).SetAnimationSlotId("corin.full-body-action");
            ((TimelineAnimationClip)generation.clip2).Clip = generation.asset1;
            ((TimelineAnimationClip)generation.clip2).ExtraPolationMode = ExtraPolationMode.Hold;
            ((TimelineAnimationClip)generation.clip2).BlendProfileId = "corin.animation-rig.action-blend-profile";
            BtsmtlSkillAuthoringCode.ConfigureMotionCurveSource((MotionCurveClip)generation.clip3, generation.asset2, 0f, 2.35f);
            ((MotionCurveClip)generation.clip3).CurveId = "DodgeForward";
        }

        static void BuildBindTimelines_CorinDodgeForwardTimeline1(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((BtsmtlSkillTimelineFlowNode)generation.node4).Configure(generation.timeline, BtsmtlSkillTimelineOwnership.Private, null, TimelinePlaybackMode.Once);
        }

        static void BuildRootBindingTimelines_CorinDodgeForwardTimeline6(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneTimeline(generation.timelineData, new[] { "1fabe64a-9df6-4bb3-a395-ebfa32d9874b", "8e4b0f0d-829b-4818-9a8b-39d9246447ea", "a0c961a6-0179-4f4d-8b75-76fa3a8bcd0a" }, new[] { "9080e5fb-1e6c-414a-bd9f-94cab1cf7c5c", "913539bd-df57-4f13-b965-feafda823f36", "e9d429f1-595e-475a-b647-444a6e069c92", "ea9a052b-5407-4a22-b1c2-f5bbce6e31bc" }, Array.Empty<string>(), Array.Empty<string>());
        }
    }
}
