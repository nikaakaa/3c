using System;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushEnhanceLoopTimelineAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            var asset = context.ResolveExternalAsset<UnityAnimationClip>("Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Enhance_Loop_FootMotionTarget.anim", 7400000L);
            var asset1 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinActionMotion/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Enhance_Loop_FootMotionTarget.asset", 11400000L);
            parts.timeline = BtsmtlSkillAuthoringCode.EnsureTimelineRoot(context, "c255d747-8381-27cc-9a61-7d9b64a6c4f4", "CorinAttackRushEnhanceLoopTimeline");
            parts.timelineData = parts.timeline.Data;
            parts.timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(AnimationTrack), "e55385b7-70e5-70fd-3fbc-042997c24905", "Animation", TimelineExecutionDomain.Presentation);
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track, "e61a4995-623d-3651-a7f8-d585ea74de4e", 0m, asset, 1.333333333255723118782043457m, 0m, 0m, 0m);
            var track1 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(MotionCurveTrack), "e4263f60-bbc2-b4bf-4f87-171a86c4c879", "Motion Curve", TimelineExecutionDomain.Logic);
            var clip1 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track1, "be9bc72e-d950-88e2-400e-156b6017c7f3", 0m, asset1, 1.333333333255723118782043457m, 0m, 0m, 0m);
            parts.track2 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(TreeTrack), "f75abe2f-f8d1-7f44-ba0c-7e7db35fa783", "Camera Triggers", TimelineExecutionDomain.Presentation);
            parts.timelineData.Name = "CorinAttack_Rush_Enhance_LoopTimeline";
            BtsmtlSkillAuthoringCode.EnsureSection(parts.timelineData, "a4fd678b-681d-0c35-65f8-48af5017c8f9", "Attack_Rush_Enhance_Loop", 0m, "");
            ((AnimationTrack)track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip1, new[] { new TimelineAuthoringPropertyValue("curveId", TimelineAuthoringPropertyKind.Text, "Attack_Rush_Enhance_Loop"), new TimelineAuthoringPropertyValue("sourceCurve", TimelineAuthoringPropertyKind.Object, asset1), new TimelineAuthoringPropertyValue("sourceEndTime", TimelineAuthoringPropertyKind.Float, 1.33333325f) });
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, Corin_Attack_Rush_Enhance_CamShake_E_01Parts corin_Attack_Rush_Enhance_CamShake_E_01, BtsmtlAuthoringGenerationContext context)
        {
            rootParts.clip2 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track2, "8e584076-8fb5-8153-7034-169b21a548eb", 0.1333333333022892475128173828m, corin_Attack_Rush_Enhance_CamShake_E_01.graph, 0.1499999999068677425384521484m, 0m, 0m, 0m);
            rootParts.clip3 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track2, "c96e4620-56b5-a8e4-9d99-03c4b2cdfd18", 0.5m, corin_Attack_Rush_Enhance_CamShake_E_01.graph1, 0.5166666666045784950256347656m, 0m, 0m, 0m);
            rootParts.clip4 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track2, "f99c3ab3-fe07-37c3-7ca9-56863a5f380b", 0.8666666666977107524871826172m, corin_Attack_Rush_Enhance_CamShake_E_01.graph2, 0.8833333333022892475128173828m, 0m, 0m, 0m);
            rootParts.clip5 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track2, "60760a42-f11f-89df-f664-9f19fd50d1be", 1.2333333333954215049743652344m, corin_Attack_Rush_Enhance_CamShake_E_01.graph3, 1.25m, 0m, 0m, 0m);
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip2, new[] { new TimelineAuthoringPropertyValue("exitSource", TimelineAuthoringPropertyKind.Enum, TimelineClipExitSource.FrameBoundary), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, corin_Attack_Rush_Enhance_CamShake_E_01.graph) });
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip3, new[] { new TimelineAuthoringPropertyValue("exitSource", TimelineAuthoringPropertyKind.Enum, TimelineClipExitSource.FrameBoundary), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, corin_Attack_Rush_Enhance_CamShake_E_01.graph1) });
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip4, new[] { new TimelineAuthoringPropertyValue("exitSource", TimelineAuthoringPropertyKind.Enum, TimelineClipExitSource.FrameBoundary), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, corin_Attack_Rush_Enhance_CamShake_E_01.graph2) });
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip5, new[] { new TimelineAuthoringPropertyValue("exitSource", TimelineAuthoringPropertyKind.Enum, TimelineClipExitSource.FrameBoundary), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, corin_Attack_Rush_Enhance_CamShake_E_01.graph3) });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(corin_Attack_Rush_Enhance_CamShake_E_01.graph, new[] { "4c025e43-35ef-990e-5041-91fd84e0b637", "c5f0e07e-b894-421b-7a59-88a62803793a", "3f341caf-01ae-bcc8-eb05-3b45318dade4", "05d6f449-bcf8-4ebf-8f8e-14efec5f50c4", "d014cb1d-d999-4d0c-ad53-8100ff1e4b01" }, new[] { "572b764c-0313-883a-b567-a1e10ad2853e" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(corin_Attack_Rush_Enhance_CamShake_E_01.graph1, new[] { "f391756e-d006-9b7c-28fb-b7dac82ba941", "a4f77c6d-9402-d6be-aefb-4ff781e7acc1", "9d001bbc-3a15-5aff-7c37-e5b1b9caf469", "ab774f7d-1213-4939-aebd-c1f71b71e630", "450e58b4-d897-45c4-a320-5e15a50a416b" }, new[] { "9afc422a-11e0-f55e-dcf0-575043c7c410" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(corin_Attack_Rush_Enhance_CamShake_E_01.graph2, new[] { "88898e67-f32a-e4a4-c8f1-74e6170f2803", "76aaa465-8f79-13de-b8ed-faa35df57e50", "56e2cf30-7f6a-2d7a-1542-43cd309b6a5f", "33200d86-10f4-4364-bc16-b7792b21a882", "50af946c-8f88-4e55-ad7c-7a2c216e60d6" }, new[] { "a499caea-d41c-2e75-02eb-729eed54b488" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(corin_Attack_Rush_Enhance_CamShake_E_01.graph3, new[] { "f8407dca-bce9-c703-940d-dc5c10db0478", "e5bf0b68-ab10-71ff-b4b3-bb7bd12f85df", "f3b1efbd-4dcb-277a-d68a-ae71d40be2f4", "5f80c5a8-2949-4050-8f59-16fd1367a7ce", "7b25388d-07ef-4174-a52c-ae2c064ee310" }, new[] { "d735e91e-d2d0-d1f5-0501-32089c8bb854" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(corin_Attack_Rush_Enhance_CamShake_E_01.graph, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(corin_Attack_Rush_Enhance_CamShake_E_01.graph1, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(corin_Attack_Rush_Enhance_CamShake_E_01.graph2, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(corin_Attack_Rush_Enhance_CamShake_E_01.graph3, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimeline(rootParts.timelineData, new[] { "e55385b7-70e5-70fd-3fbc-042997c24905", "e4263f60-bbc2-b4bf-4f87-171a86c4c879", "f75abe2f-f8d1-7f44-ba0c-7e7db35fa783" }, new[] { "e61a4995-623d-3651-a7f8-d585ea74de4e", "be9bc72e-d950-88e2-400e-156b6017c7f3", "8e584076-8fb5-8153-7034-169b21a548eb", "c96e4620-56b5-a8e4-9d99-03c4b2cdfd18", "f99c3ab3-fe07-37c3-7ca9-56863a5f380b", "60760a42-f11f-89df-f664-9f19fd50d1be" }, new[] { "a4fd678b-681d-0c35-65f8-48af5017c8f9" }, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimelineMarkers(rootParts.timelineData, Array.Empty<string>());
        }

        sealed class RootParts
        {
            internal TimelineAsset timeline;
            internal TimelineData timelineData;
            internal Track track2;
            internal Clip clip2;
            internal Clip clip3;
            internal Clip clip4;
            internal Clip clip5;
            internal TimelineContractCatalog timelineCatalog;
        }
    }
}
