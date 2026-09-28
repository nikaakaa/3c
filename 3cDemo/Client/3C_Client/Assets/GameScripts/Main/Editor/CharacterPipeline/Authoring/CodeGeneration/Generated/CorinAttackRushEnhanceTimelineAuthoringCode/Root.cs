using System;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushEnhanceTimelineAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            var asset = context.ResolveExternalAsset<UnityAnimationClip>("Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Enhance_Start_FootMotionTarget.anim", 7400000L);
            var asset1 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinActionMotion/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Enhance_Start_FootMotionTarget.asset", 11400000L);
            parts.timeline = BtsmtlSkillAuthoringCode.EnsureTimelineRoot(context, "fa50d379-5509-1650-85ca-6bc5c062faa3", "CorinAttackRushEnhanceTimeline");
            parts.timelineData = parts.timeline.Data;
            parts.timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(AnimationTrack), "918912fa-40c9-d1d3-b6d1-02cb8f08356e", "Animation", TimelineExecutionDomain.Presentation);
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track, "35ec9f0d-14d9-cb8a-5e1a-276a4f4438bc", 0m, asset, 0.6333333333022892475128173828m, 0m, 0m, 0m);
            var track1 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(MotionCurveTrack), "5fc38dcd-8db4-c11e-31c2-909f6f2d78ad", "Motion Curve", TimelineExecutionDomain.Logic);
            var clip1 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track1, "b59713fa-52bb-e615-cf2e-42422136de74", 0m, asset1, 0.6333333333022892475128173828m, 0m, 0m, 0m);
            parts.track2 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(TreeTrack), "e6368c34-175f-59e9-f2b8-5647633e43bb", "Camera Triggers", TimelineExecutionDomain.Presentation);
            parts.timelineData.Name = "CorinAttack_Rush_EnhanceTimeline";
            BtsmtlSkillAuthoringCode.EnsureSection(parts.timelineData, "017fe590-4234-5099-420e-794651345f58", "Attack_Rush_Enhance", 0m, "");
            ((AnimationTrack)track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip1, new[] { new TimelineAuthoringPropertyValue("curveId", TimelineAuthoringPropertyKind.Text, "Attack_Rush_Enhance"), new TimelineAuthoringPropertyValue("sourceCurve", TimelineAuthoringPropertyKind.Object, asset1), new TimelineAuthoringPropertyValue("sourceEndTime", TimelineAuthoringPropertyKind.Float, 0.6333333f) });
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, Corin_Attack_Rush_Enhance_CamShake_E_01Parts corin_Attack_Rush_Enhance_CamShake_E_01, BtsmtlAuthoringGenerationContext context)
        {
            rootParts.clip2 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track2, "b2b73156-e883-58b4-2c04-977609f3461b", 0.1333333333022892475128173828m, corin_Attack_Rush_Enhance_CamShake_E_01.graph, 0.1499999999068677425384521484m, 0m, 0m, 0m);
            rootParts.clip3 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track2, "9f8f84ce-205e-1427-a73c-7a6338d703ea", 0.3666666666977107524871826172m, corin_Attack_Rush_Enhance_CamShake_E_01.graph1, 0.3833333333022892475128173828m, 0m, 0m, 0m);
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip2, new[] { new TimelineAuthoringPropertyValue("exitSource", TimelineAuthoringPropertyKind.Enum, TimelineClipExitSource.FrameBoundary), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, corin_Attack_Rush_Enhance_CamShake_E_01.graph) });
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip3, new[] { new TimelineAuthoringPropertyValue("exitSource", TimelineAuthoringPropertyKind.Enum, TimelineClipExitSource.FrameBoundary), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, corin_Attack_Rush_Enhance_CamShake_E_01.graph1) });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(corin_Attack_Rush_Enhance_CamShake_E_01.graph, new[] { "ad37b95a-86b8-9e6b-caf0-21d4ded4e374", "cd02877f-f12c-5dbc-c919-28984ce188ef", "660bff09-9dff-fd22-5525-21455130d664", "9b80940e-7eda-447c-9572-ed252a5e8b94", "547a26ef-0aea-4d9d-96af-89ff5eaade12" }, new[] { "c937d290-0a85-92f5-cb85-eafb0edeebc7" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(corin_Attack_Rush_Enhance_CamShake_E_01.graph1, new[] { "38a41c46-4cde-ebe8-6b88-d0bbb6466de3", "d23a26bc-4224-171f-087c-70a91c84c51f", "b42ba62f-2d92-37a1-a283-7900511c0e2d", "abd27f6f-106e-4073-9d37-b56ecd7f2e0b", "bebfeec7-98b7-4c10-bffe-379abea946f1" }, new[] { "8a9e9ba6-e2b0-5727-945f-6fdce673ee58" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(corin_Attack_Rush_Enhance_CamShake_E_01.graph, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(corin_Attack_Rush_Enhance_CamShake_E_01.graph1, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimeline(rootParts.timelineData, new[] { "918912fa-40c9-d1d3-b6d1-02cb8f08356e", "5fc38dcd-8db4-c11e-31c2-909f6f2d78ad", "e6368c34-175f-59e9-f2b8-5647633e43bb" }, new[] { "35ec9f0d-14d9-cb8a-5e1a-276a4f4438bc", "b59713fa-52bb-e615-cf2e-42422136de74", "b2b73156-e883-58b4-2c04-977609f3461b", "9f8f84ce-205e-1427-a73c-7a6338d703ea" }, new[] { "017fe590-4234-5099-420e-794651345f58" }, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimelineMarkers(rootParts.timelineData, Array.Empty<string>());
        }

        sealed class RootParts
        {
            internal TimelineAsset timeline;
            internal TimelineData timelineData;
            internal Track track2;
            internal Clip clip2;
            internal Clip clip3;
            internal TimelineContractCatalog timelineCatalog;
        }
    }
}
