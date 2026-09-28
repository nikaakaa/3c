using System;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushEnhanceExplodeTimelineAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            var asset = context.ResolveExternalAsset<UnityAnimationClip>("Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Enhance_Explode_FootMotionTarget.anim", 7400000L);
            var asset1 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinActionMotion/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Enhance_Explode_FootMotionTarget.asset", 11400000L);
            parts.timeline = BtsmtlSkillAuthoringCode.EnsureTimelineRoot(context, "a35691a4-9f77-8559-628a-bcfa4adfabfd", "CorinAttackRushEnhanceExplodeTimeline");
            parts.timelineData = parts.timeline.Data;
            parts.timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(AnimationTrack), "7b7a68c6-9478-966a-af3a-64f5749f882f", "Animation", TimelineExecutionDomain.Presentation);
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track, "1b299eb7-465b-d18e-8a31-cc6ab154fe0c", 0m, asset, 1.0500000000465661287307739258m, 0m, 0m, 0m);
            parts.track1 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(TreeTrack), "994c1e63-20fc-13a5-8705-2f1401a730dc", "Action Windows", TimelineExecutionDomain.Logic);
            parts.track2 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(TreeTrack), "4be9fda1-354c-7841-fb5d-b4d90f56cc0d", "Logic / Decision / RushMoveExit", TimelineExecutionDomain.Logic);
            var track3 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(MotionCurveTrack), "b31a477d-c875-1531-17e2-8c7574fdee65", "Motion Curve", TimelineExecutionDomain.Logic);
            var clip3 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track3, "39f3c856-86e0-881a-15c6-eb398e905529", 0m, asset1, 1.0500000000465661287307739258m, 0m, 0m, 0m);
            parts.track4 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(TreeTrack), "4a0b5f93-0251-310a-4fae-7c8b42e18760", "Camera Triggers", TimelineExecutionDomain.Presentation);
            parts.timelineData.Name = "CorinAttack_Rush_Enhance_ExplodeTimeline";
            BtsmtlSkillAuthoringCode.EnsureSection(parts.timelineData, "d33002d9-8bfb-578a-8c7d-c2878aa366c6", "Attack_Rush_Enhance_Explode", 0m, "");
            ((AnimationTrack)track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip3, new[] { new TimelineAuthoringPropertyValue("curveId", TimelineAuthoringPropertyKind.Text, "Attack_Rush_Enhance_Explode"), new TimelineAuthoringPropertyValue("sourceCurve", TimelineAuthoringPropertyKind.Object, asset1), new TimelineAuthoringPropertyValue("sourceEndTime", TimelineAuthoringPropertyKind.Float, 1.05f) });
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, RushAttackHandoffParts rushAttackHandoff, RushMoveExitParts rushMoveExit, Corin_Attack_Rush_CamShake_E_02Parts corin_Attack_Rush_CamShake_E_02, BtsmtlAuthoringGenerationContext context)
        {
            rootParts.clip1 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track1, "afca8ad0-9696-c1df-4d46-ddb9ecbccb40", 0.2666666666045784950256347656m, rushAttackHandoff.graph);
            rootParts.clip2 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track2, "702ad961-e0c5-d52a-bf9d-878150e7b96a", 0.3999999999068677425384521484m, rushMoveExit.graph2);
            rootParts.clip4 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track4, "e26eb63e-0bf0-3591-c3bc-45ff8c19527d", 0.0166666666045784950256347656m, corin_Attack_Rush_CamShake_E_02.graph4, 0.0333333334419876337051391602m, 0m, 0m, 0m);
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip1, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, rushAttackHandoff.graph) });
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip2, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, rushMoveExit.graph2) });
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip4, new[] { new TimelineAuthoringPropertyValue("exitSource", TimelineAuthoringPropertyKind.Enum, TimelineClipExitSource.FrameBoundary), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, corin_Attack_Rush_CamShake_E_02.graph4) });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushAttackHandoff.graph, new[] { "61ed466c-1298-45d3-b8d1-b7eff176a382", "b7865d97-eda7-34be-4019-7cb85a57acfe", "7c625660-113f-5c02-1e42-8556dd67478b", "4d8615bd-0330-2aab-79b8-8e2f6e53a853", "75082ff4-d534-1242-e8f6-c8f590e7021f", "0e62c8a6-bb81-4207-88bc-4ccb6714632e", "93ecb0b2-4cf1-4fc6-a1eb-8958c2ba1313" }, new[] { "96579de8-20ab-5d67-e8e7-10e7ee94ca8f", "8455162c-2a42-3a79-1bf5-5645d86a2fbc", "7557fed9-6d74-ae3f-9d98-52d5e0c9ce80" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushAttackHandoff.graph1, new[] { "1d3fb0e0-0dd5-03fd-f120-d8f156f96c1f", "d1d4ee2e-29e2-a6ee-9ff3-94a693721c41", "1cdbf314-9f26-3b55-78f0-3f74282c67b5" }, new[] { "c2c6b1e0-53b4-1ea1-778b-669db6796389", "1d13e6ad-fd23-5a1f-4d92-c711efc3c58e" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushMoveExit.graph2, new[] { "eca3e9bc-8c2a-46ac-acee-96679e56a65b", "e89aef47-d825-e250-549b-a869f9b012e9", "af0b877d-cae8-7a45-b039-62873ed41e01", "91d2300b-7ff7-cb37-a5f2-66df24446028", "8dcb6041-2fe0-b234-069f-8670ce68e1f9", "adf69153-ee9a-4829-a756-a1e0b1785d16", "3fe012fe-04cd-4867-925f-a48ede90dddf" }, new[] { "a53f7e04-6620-c7e4-1765-468b70a76891", "416d23f3-79cd-2a36-a500-b94d04f8568e", "19411d22-c9cd-0fc0-bf84-e2ed27f8116c" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushMoveExit.graph3, new[] { "7a45ffcb-c5c4-b406-c28e-0a449396e24d", "70000659-506d-20a5-140b-57ce82caecd7", "6fa2bc67-2dd0-f758-f9c3-934d24c73d7d" }, new[] { "22baf6d0-926e-c1ee-5e89-c30a10960f56", "c2dc942b-b08e-7351-6be6-550b2f7d0755" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(corin_Attack_Rush_CamShake_E_02.graph4, new[] { "b19ad106-3846-c3fe-1c5e-41a2f82e53f2", "442164a1-974e-c750-d965-c6e83e26c72f", "f6a4c3b6-dabd-147e-5241-c6f700162d12", "dab66851-2f42-4716-a0ba-ffb1d1447ef0", "a7b8bd6d-bbb7-4083-96c3-014717783044" }, new[] { "69ad20a6-7c41-246b-d22d-b0df82c4dda7" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushAttackHandoff.graph, new[] { "c07839bc-5804-2762-ddb8-7942f2d9c46a" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushAttackHandoff.graph1, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushMoveExit.graph2, new[] { "30e756a5-027f-7657-193c-233abd586e6b" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushMoveExit.graph3, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(corin_Attack_Rush_CamShake_E_02.graph4, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimeline(rootParts.timelineData, new[] { "7b7a68c6-9478-966a-af3a-64f5749f882f", "994c1e63-20fc-13a5-8705-2f1401a730dc", "4be9fda1-354c-7841-fb5d-b4d90f56cc0d", "b31a477d-c875-1531-17e2-8c7574fdee65", "4a0b5f93-0251-310a-4fae-7c8b42e18760" }, new[] { "1b299eb7-465b-d18e-8a31-cc6ab154fe0c", "afca8ad0-9696-c1df-4d46-ddb9ecbccb40", "702ad961-e0c5-d52a-bf9d-878150e7b96a", "39f3c856-86e0-881a-15c6-eb398e905529", "e26eb63e-0bf0-3591-c3bc-45ff8c19527d" }, new[] { "d33002d9-8bfb-578a-8c7d-c2878aa366c6" }, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimelineMarkers(rootParts.timelineData, Array.Empty<string>());
        }

        sealed class RootParts
        {
            internal TimelineAsset timeline;
            internal TimelineData timelineData;
            internal Track track1;
            internal Clip clip1;
            internal Track track2;
            internal Clip clip2;
            internal Track track4;
            internal Clip clip4;
            internal TimelineContractCatalog timelineCatalog;
        }
    }
}
