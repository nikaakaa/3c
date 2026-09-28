using System;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushEnhanceEndTimelineAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            var asset = context.ResolveExternalAsset<UnityAnimationClip>("Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Explode_FootMotionTarget.anim", 7400000L);
            var asset1 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinActionMotion/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Explode_FootMotionTarget.asset", 11400000L);
            parts.timeline = BtsmtlSkillAuthoringCode.EnsureTimelineRoot(context, "a8e1b908-009a-6920-2a28-13cb003dafe2", "CorinAttackRushEnhanceEndTimeline");
            parts.timelineData = parts.timeline.Data;
            parts.timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(AnimationTrack), "793211ed-ee3c-093b-6583-02c0d927b3ac", "Animation", TimelineExecutionDomain.Presentation);
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track, "e66db61a-247d-a9cf-56c5-ac53e6de6926", 0m, asset, 1.166666666744276881217956543m, 0m, 0m, 0m);
            parts.track1 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(TreeTrack), "f551fa6f-f97c-940c-0463-3ddf9a327f63", "Action Windows", TimelineExecutionDomain.Logic);
            parts.track2 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(TreeTrack), "c4ba676f-4a6a-1ac5-aea8-56f575b676a5", "Logic / Decision / RushMoveExit", TimelineExecutionDomain.Logic);
            var track3 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(MotionCurveTrack), "48855bd7-6f6f-5e97-45a2-2dd5b2cbc4dc", "Motion Curve", TimelineExecutionDomain.Logic);
            var clip3 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track3, "50de4831-18d8-a8e0-acaa-8b563d4f8b68", 0m, asset1, 1.166666666744276881217956543m, 0m, 0m, 0m);
            parts.timelineData.Name = "CorinAttack_Rush_Enhance_EndTimeline";
            BtsmtlSkillAuthoringCode.EnsureSection(parts.timelineData, "43a899a3-042c-644d-130e-673ca2a9d5d3", "Attack_Rush_Enhance_End", 0m, "");
            ((AnimationTrack)track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip3, new[] { new TimelineAuthoringPropertyValue("curveId", TimelineAuthoringPropertyKind.Text, "Attack_Rush_Enhance_End"), new TimelineAuthoringPropertyValue("sourceCurve", TimelineAuthoringPropertyKind.Object, asset1), new TimelineAuthoringPropertyValue("sourceEndTime", TimelineAuthoringPropertyKind.Float, 1.16666663f) });
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, RushAttackHandoffParts rushAttackHandoff, RushMoveExitParts rushMoveExit, BtsmtlAuthoringGenerationContext context)
        {
            rootParts.clip1 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track1, "2e71f7f6-8ddf-3cfa-06f6-edc7dd57c462", 0m, rushAttackHandoff.graph);
            rootParts.clip2 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track2, "a8545342-6fc7-b720-66af-a3664b15d4e3", 0.1999999999534338712692260742m, rushMoveExit.graph2);
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip1, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, rushAttackHandoff.graph) });
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip2, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, rushMoveExit.graph2) });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushAttackHandoff.graph, new[] { "ccacfe56-a344-4ac2-8968-005110cbd57c", "9904e3fb-99d7-929b-a468-8cc773453365", "34dae8cb-d042-465c-77f7-2b07d7507ee1", "9e572b24-266d-9908-1ee1-b49ab75f8f67", "83661d24-1ff4-1f1c-90c1-b45328a13085", "75018d06-9729-47e7-adb1-b66bbfdcb500", "da417f10-4e44-44c6-a3b1-376b0f39ac48" }, new[] { "4f2341ca-54c0-622e-a6f0-05f2c792363d", "55e72816-4ea4-73c2-aa38-7d2e5ff5971d", "feec0baf-b6da-24a1-7947-a56185c3972b" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushAttackHandoff.graph1, new[] { "56fab701-120d-0628-4304-65997d9e8849", "36a37aa2-c7f8-f4a9-0bc2-c61b7968b458", "efd26db6-4ced-c06e-62f8-16989f1a6791" }, new[] { "1fd12e9a-fa60-81c3-8845-2d7027c6c94e", "6e85fe77-dd92-19f3-ce1e-d3c5b3c6fa0f" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushMoveExit.graph2, new[] { "5446bb15-7882-47d9-9e70-5c7ee2bd4bfb", "0d264a6f-3ca4-5651-1dff-da1d099fa635", "7b831b01-625d-cb9b-3c1b-169801483309", "50210f64-a973-d4fa-0723-cf91e018091d", "e50c656c-a24d-4a8d-635f-587aa0ad5bee", "d4373416-a1af-4fad-8220-ef97a1b0f5b2", "61a1ec0c-bd25-442d-a95e-ff9a3a7b3568" }, new[] { "16b96421-edf3-46d6-34a3-c28ffdf9bb0e", "61557c61-bf29-e974-4881-4d0e5800ee1a", "524c11b5-c32c-d5d4-c53a-326e25751e46" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushMoveExit.graph3, new[] { "b62ee8d7-d24a-8528-11a3-f075e05bb7c6", "3bd314ef-0d4a-7cc0-8d3a-5600b06cdf5c", "851346b9-712d-34eb-d22e-7c2223ffcc26" }, new[] { "73307dca-92ed-3c2a-883f-97fc7bc1116e", "0415922d-1613-b129-b886-5838a63db5f7" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushAttackHandoff.graph, new[] { "9ac1b3d7-1e9f-2633-808f-a4634d3f22f2" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushAttackHandoff.graph1, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushMoveExit.graph2, new[] { "71b7fb59-94b0-f237-3fbc-f3ee2474661f" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushMoveExit.graph3, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimeline(rootParts.timelineData, new[] { "793211ed-ee3c-093b-6583-02c0d927b3ac", "f551fa6f-f97c-940c-0463-3ddf9a327f63", "c4ba676f-4a6a-1ac5-aea8-56f575b676a5", "48855bd7-6f6f-5e97-45a2-2dd5b2cbc4dc" }, new[] { "e66db61a-247d-a9cf-56c5-ac53e6de6926", "2e71f7f6-8ddf-3cfa-06f6-edc7dd57c462", "a8545342-6fc7-b720-66af-a3664b15d4e3", "50de4831-18d8-a8e0-acaa-8b563d4f8b68" }, new[] { "43a899a3-042c-644d-130e-673ca2a9d5d3" }, Array.Empty<string>());
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
            internal TimelineContractCatalog timelineCatalog;
        }
    }
}
