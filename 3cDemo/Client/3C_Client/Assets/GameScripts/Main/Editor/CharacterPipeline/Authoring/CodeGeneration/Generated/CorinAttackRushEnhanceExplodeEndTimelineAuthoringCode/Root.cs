using System;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushEnhanceExplodeEndTimelineAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            var asset = context.ResolveExternalAsset<UnityAnimationClip>("Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Enhance_End_FootMotionTarget.anim", 7400000L);
            var asset1 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinActionMotion/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Enhance_End_FootMotionTarget.asset", 11400000L);
            parts.timeline = BtsmtlSkillAuthoringCode.EnsureTimelineRoot(context, "dd7314a9-4fcc-26c0-ca7c-1abc4fc6e7ce", "CorinAttackRushEnhanceExplodeEndTimeline");
            parts.timelineData = parts.timeline.Data;
            parts.timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(AnimationTrack), "0a0f6328-40b3-6e77-574a-3ded9c09d101", "Animation", TimelineExecutionDomain.Presentation);
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track, "93b344ea-7256-d1f6-aaac-ddb3d689ccf8", 0m, asset, 1.9666666665580123662948608398m, 0m, 0m, 0m);
            parts.track1 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(TreeTrack), "fe37805f-c308-e2a1-fa3b-f5bc90da7db9", "Action Windows", TimelineExecutionDomain.Logic);
            var track2 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(MotionCurveTrack), "ca0b03cc-30bf-22f1-39c9-ebb5c07e1ef2", "Motion Curve", TimelineExecutionDomain.Logic);
            var clip2 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track2, "1ea108be-0bb5-461f-7a4e-a82624912322", 0m, asset1, 1.9666666665580123662948608398m, 0m, 0m, 0m);
            parts.timelineData.Name = "CorinAttack_Rush_Enhance_Explode_EndTimeline";
            BtsmtlSkillAuthoringCode.EnsureSection(parts.timelineData, "58bc6ed1-4905-144e-2abc-7a341aabec10", "Attack_Rush_Enhance_Explode_End", 0m, "");
            ((AnimationTrack)track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip2, new[] { new TimelineAuthoringPropertyValue("curveId", TimelineAuthoringPropertyKind.Text, "Attack_Rush_Enhance_Explode_End"), new TimelineAuthoringPropertyValue("sourceCurve", TimelineAuthoringPropertyKind.Object, asset1), new TimelineAuthoringPropertyValue("sourceEndTime", TimelineAuthoringPropertyKind.Float, 1.96666658f) });
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, RushMoveExitParts rushMoveExit, BtsmtlAuthoringGenerationContext context)
        {
            rootParts.clip1 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track1, "c7ec4414-0540-fff3-10ed-8fb2bbbc29d7", 0.2666666666045784950256347656m, rushMoveExit.graph);
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip1, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, rushMoveExit.graph) });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushMoveExit.graph, new[] { "27c44ecf-3fcc-4150-a8ca-9ef935c9af40", "cc02cac5-0392-4883-5cc7-56ae982b885f", "57893ebb-db34-e079-7028-b985721e563e", "aa7a31c7-1f5b-d0af-9a8d-0b702d651701", "4d7298fe-c064-c062-62e6-4ac12997a124", "6080099d-5ee6-468f-aece-b4d4394a61fd", "9e474e2b-551f-45c1-841f-c0d5d259a568" }, new[] { "8c986a81-2585-9a44-d96e-653678e4a997", "e03d854d-b8f6-d963-c2bc-2fcbf17fd3ba", "7df0fc7d-2734-b148-f4a3-f630eb21fb8f" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushMoveExit.graph1, new[] { "f148ecc5-61cf-f1e5-ab8e-e86fc9a31aa9", "7227283a-2a7b-3dd1-5eab-a90464d732c4", "231d8e07-9f0b-22eb-6477-057851b5a331" }, new[] { "8b83013d-28ab-19d6-a675-dc84548d739d", "427d635a-4259-3445-862b-ab66bb940a5f" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushMoveExit.graph, new[] { "7530d1f8-64fb-efdc-2966-bc1b42dd8b32" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushMoveExit.graph1, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimeline(rootParts.timelineData, new[] { "0a0f6328-40b3-6e77-574a-3ded9c09d101", "fe37805f-c308-e2a1-fa3b-f5bc90da7db9", "ca0b03cc-30bf-22f1-39c9-ebb5c07e1ef2" }, new[] { "93b344ea-7256-d1f6-aaac-ddb3d689ccf8", "c7ec4414-0540-fff3-10ed-8fb2bbbc29d7", "1ea108be-0bb5-461f-7a4e-a82624912322" }, new[] { "58bc6ed1-4905-144e-2abc-7a341aabec10" }, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimelineMarkers(rootParts.timelineData, Array.Empty<string>());
        }

        sealed class RootParts
        {
            internal TimelineAsset timeline;
            internal TimelineData timelineData;
            internal Track track1;
            internal Clip clip1;
            internal TimelineContractCatalog timelineCatalog;
        }
    }
}
