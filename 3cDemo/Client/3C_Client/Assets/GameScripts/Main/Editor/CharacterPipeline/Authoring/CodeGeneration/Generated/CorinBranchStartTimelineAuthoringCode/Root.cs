using System;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchStartTimelineAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            var asset = context.ResolveExternalAsset<UnityAnimationClip>("Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Branch_02_FootMotionTarget.anim", 7400000L);
            var asset1 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinActionMotion/Avatar_Female_Size01_Corin_Ani_Attack_Branch_02_FootMotionTarget.asset", 11400000L);
            var asset2 = context.ResolveExternalAsset<CameraStretchAsset>("Assets/Configs/Character/Corin/Pipeline/Presentation/Camera/Corin_Attack_Branch_02_CamStretch_01.asset", 11400000L);
            var asset3 = context.ResolveExternalAsset<CameraZoomAsset>("Assets/Configs/Character/Corin/Pipeline/Presentation/Camera/Corin_Attack_Branch_02_CamZoom_01.asset", 11400000L);
            parts.timeline = BtsmtlSkillAuthoringCode.EnsureTimelineRoot(context, "b011652c-fe6a-cf58-340a-97e7b7fd0bc0", "CorinBranchStartTimeline");
            parts.timelineData = parts.timeline.Data;
            parts.timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(AnimationTrack), "5eb4e806-c0c8-69f6-c111-7fd06434d45f", "Animation", TimelineExecutionDomain.Presentation);
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track, "ef5e9c89-341a-5e85-b931-e9d0ce45dab3", 0m, asset, 1.1000000000931322574615478516m, 0m, 0m, 0m);
            parts.track1 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(TreeTrack), "8dc1c45e-4c50-d274-bb1f-140f34d61be4", "Branch Release", TimelineExecutionDomain.Logic);
            var track2 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(MotionCurveTrack), "0d1c90bf-ce93-d9ab-d217-49e0e639376b", "Motion Curve", TimelineExecutionDomain.Logic);
            var clip2 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track2, "fdce0b22-e9ee-804e-4071-e9ea13042ca1", 0m, asset1, 1.1000000000931322574615478516m, 0m, 0m, 0m);
            parts.track3 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(TreeTrack), "f0500e62-f340-cea9-c31b-199606077561", "Camera Triggers", TimelineExecutionDomain.Presentation);
            var track4 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(CameraEffectTrack), "b57e0b30-085b-02ba-6dbe-04efde3ee8be", "Camera Effects", TimelineExecutionDomain.Presentation);
            var clip4 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track4, "e113b356-137c-8789-0cc3-8a812dff5294", 0m, null, 1.1000000000931322574615478516m, 0m, 0m, 0m);
            var clip5 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track4, "98466951-99db-f8e7-1b46-18999145c1c0", 0m, null, 1.1000000000931322574615478516m, 0m, 0m, 0m);
            BtsmtlSkillAuthoringCode.EnsureSection(parts.timelineData, "f0165367-71ea-1d2b-f406-773e924b706e", "Attack_Branch_02_Start", 0m, "");
            ((AnimationTrack)track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip2, new[] { new TimelineAuthoringPropertyValue("curveId", TimelineAuthoringPropertyKind.Text, "BranchStart"), new TimelineAuthoringPropertyValue("sourceCurve", TimelineAuthoringPropertyKind.Object, asset1), new TimelineAuthoringPropertyValue("sourceEndTime", TimelineAuthoringPropertyKind.Float, 1.0999999f) });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip4, new[] { new TimelineAuthoringPropertyValue("effect", TimelineAuthoringPropertyKind.Object, asset2) });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip5, new[] { new TimelineAuthoringPropertyValue("effect", TimelineAuthoringPropertyKind.Object, asset3) });
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, Branch_Release_WindowParts branch_Release_Window, Corin_Attack_Branch_02_CamShake_E_01Parts corin_Attack_Branch_02_CamShake_E_01, BtsmtlAuthoringGenerationContext context)
        {
            rootParts.clip1 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track1, "30bae1bb-860d-e73d-97ce-c53da5f56185", 1.0666666666511446237564086914m, branch_Release_Window.graph);
            rootParts.clip3 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track3, "364c4fca-0610-95e0-5675-9e096082f2b8", 0.166666666744276881217956543m, corin_Attack_Branch_02_CamShake_E_01.graph2, 0.1833333333488553762435913086m, 0m, 0m, 0m);
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip1, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, branch_Release_Window.graph) });
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip3, new[] { new TimelineAuthoringPropertyValue("exitSource", TimelineAuthoringPropertyKind.Enum, TimelineClipExitSource.FrameBoundary), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, corin_Attack_Branch_02_CamShake_E_01.graph2) });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(branch_Release_Window.graph, new[] { "42cab1d2-1c08-4f0e-a1d7-87fae9e1c9a8", "dc7b3393-ff63-4759-271b-4130d0af5642", "5a054e09-4e58-7bb2-05d7-3eab16ed528f", "3ad7a2c9-813f-c445-7041-89a55df20ded", "4a7f4ef0-e213-83ca-e368-356a789aa8f5", "646a00f5-3b57-4c5b-8239-3a163e429ff5", "65677a73-3269-4d58-bbee-19358e887343" }, new[] { "646715ea-fd58-5c23-8e58-2bafc854f101", "0d43a83a-2ea0-f922-6631-e71fdf3ef065", "e095dc9c-49a3-8c0a-f56b-5c0b67bc6162" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(branch_Release_Window.graph1, new[] { "522ee7fa-5aed-53d1-5b3a-720d09f3d847", "8f5667e3-92d3-0007-eab3-57ad3f28297f", "cd129aa0-f07c-6a18-1b00-a73c7fc47eb7" }, new[] { "d7679108-7a17-f7c2-3fa7-7d83ea48215c", "9943d62c-5a44-394b-2e62-7ac8d79cadfb" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(corin_Attack_Branch_02_CamShake_E_01.graph2, new[] { "754bdd42-9f10-022d-f593-70850d200138", "a478bbcd-93fd-ff4e-6ee6-763c51e4db85", "b69eea7e-172a-55d4-61cd-b9fba87c0b88", "7daf819d-5aa6-43d7-9861-dd3bde0ae040", "569a98b3-4b47-42a7-bbca-0dd2e8d53644" }, new[] { "5a56b4d4-cc67-633f-08ee-4be3c958fb30" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(branch_Release_Window.graph, new[] { "8253d70c-2187-afdf-2057-8805682fd389" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(branch_Release_Window.graph1, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(corin_Attack_Branch_02_CamShake_E_01.graph2, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimeline(rootParts.timelineData, new[] { "5eb4e806-c0c8-69f6-c111-7fd06434d45f", "8dc1c45e-4c50-d274-bb1f-140f34d61be4", "0d1c90bf-ce93-d9ab-d217-49e0e639376b", "f0500e62-f340-cea9-c31b-199606077561", "b57e0b30-085b-02ba-6dbe-04efde3ee8be" }, new[] { "ef5e9c89-341a-5e85-b931-e9d0ce45dab3", "30bae1bb-860d-e73d-97ce-c53da5f56185", "fdce0b22-e9ee-804e-4071-e9ea13042ca1", "364c4fca-0610-95e0-5675-9e096082f2b8", "e113b356-137c-8789-0cc3-8a812dff5294", "98466951-99db-f8e7-1b46-18999145c1c0" }, new[] { "f0165367-71ea-1d2b-f406-773e924b706e" }, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimelineMarkers(rootParts.timelineData, Array.Empty<string>());
        }

        sealed class RootParts
        {
            internal TimelineAsset timeline;
            internal TimelineData timelineData;
            internal Track track1;
            internal Clip clip1;
            internal Track track3;
            internal Clip clip3;
            internal TimelineContractCatalog timelineCatalog;
        }
    }
}
