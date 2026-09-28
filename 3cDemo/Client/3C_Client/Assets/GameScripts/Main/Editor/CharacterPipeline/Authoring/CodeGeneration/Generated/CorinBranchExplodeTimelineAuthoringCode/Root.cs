using System;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchExplodeTimelineAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            var asset = context.ResolveExternalAsset<UnityAnimationClip>("Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Branch_02_Explode_FootMotionTarget.anim", 7400000L);
            var asset1 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinActionMotion/Avatar_Female_Size01_Corin_Ani_Attack_Branch_02_Explode_FootMotionTarget.asset", 11400000L);
            var asset2 = context.ResolveExternalAsset<CameraStretchAsset>("Assets/Configs/Character/Corin/Pipeline/Presentation/Camera/Corin_Attack_Branch_02_CamStretch_02.asset", 11400000L);
            var asset3 = context.ResolveExternalAsset<CameraZoomAsset>("Assets/Configs/Character/Corin/Pipeline/Presentation/Camera/Corin_Attack_Branch_02_CamZoom_02.asset", 11400000L);
            parts.timeline = BtsmtlSkillAuthoringCode.EnsureTimelineRoot(context, "262f4cc3-9ad0-e572-aba8-4d133b32bf80", "CorinBranchExplodeTimeline");
            parts.timelineData = parts.timeline.Data;
            parts.timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(AnimationTrack), "9f818205-3225-8862-14ea-f557459f6b66", "Animation", TimelineExecutionDomain.Presentation);
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track, "8ba098b5-ae76-3a6a-4daf-a3082783834d", 0m, asset, 1.0500000698957592248916625977m, 0m, 0m, 0m);
            var track1 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(MotionCurveTrack), "e0867ac0-e6d1-05f4-a290-e6599d401b40", "Motion Curve", TimelineExecutionDomain.Logic);
            var clip1 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track1, "f60ca9ea-2098-0da8-4c41-284431256408", 0m, asset1, 1.0500000698957592248916625977m, 0m, 0m, 0m);
            parts.track2 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(TreeTrack), "58cc8b23-34fc-945a-bcb2-a8f2531e1201", "Camera Triggers", TimelineExecutionDomain.Presentation);
            var track3 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(CameraEffectTrack), "d6ce7242-94a8-c01c-7e78-463359084698", "Camera Effects", TimelineExecutionDomain.Presentation);
            var clip4 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track3, "9392f966-b201-1d4f-7a1b-d3701542f6b2", 0m, null, 1.0500000698957592248916625977m, 0m, 0m, 0m);
            var clip5 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track3, "419e7a71-a444-71d5-c369-1b1410019cec", 0m, null, 1.0500000698957592248916625977m, 0m, 0m, 0m);
            BtsmtlSkillAuthoringCode.EnsureSection(parts.timelineData, "e1950e89-ec94-cc7a-3a5b-f459ea18127d", "Attack_Branch_02_Explode", 0m, "");
            ((AnimationTrack)track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip1, new[] { new TimelineAuthoringPropertyValue("curveId", TimelineAuthoringPropertyKind.Text, "BranchExplode"), new TimelineAuthoringPropertyValue("sourceCurve", TimelineAuthoringPropertyKind.Object, asset1), new TimelineAuthoringPropertyValue("sourceEndTime", TimelineAuthoringPropertyKind.Float, 1.05f) });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip4, new[] { new TimelineAuthoringPropertyValue("effect", TimelineAuthoringPropertyKind.Object, asset2) });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip5, new[] { new TimelineAuthoringPropertyValue("effect", TimelineAuthoringPropertyKind.Object, asset3) });
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, Corin_Attack_Branch_02_CamShake_E_02Parts corin_Attack_Branch_02_CamShake_E_02, Corin_Attack_Branch_02_CamShake_E_03Parts corin_Attack_Branch_02_CamShake_E_03, BtsmtlAuthoringGenerationContext context)
        {
            rootParts.clip2 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track2, "5ea2c15a-4c0e-cdcf-e15d-070e579ebddc", 0.1166666666977107524871826172m, corin_Attack_Branch_02_CamShake_E_02.graph, 0.1333333333022892475128173828m, 0m, 0m, 0m);
            rootParts.clip3 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track2, "63ce4cf5-4d0f-08ef-45c0-32673fcc415f", 0.3000000000465661287307739258m, corin_Attack_Branch_02_CamShake_E_03.graph1, 0.3166666666511446237564086914m, 0m, 0m, 0m);
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip2, new[] { new TimelineAuthoringPropertyValue("exitSource", TimelineAuthoringPropertyKind.Enum, TimelineClipExitSource.FrameBoundary), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, corin_Attack_Branch_02_CamShake_E_02.graph) });
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip3, new[] { new TimelineAuthoringPropertyValue("exitSource", TimelineAuthoringPropertyKind.Enum, TimelineClipExitSource.FrameBoundary), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, corin_Attack_Branch_02_CamShake_E_03.graph1) });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(corin_Attack_Branch_02_CamShake_E_02.graph, new[] { "2e40c8da-9101-e6ec-9ddc-507e42a4aefe", "b8e45e73-2f21-cbc4-5c5d-c00636ffe918", "bf92bf54-bc51-6704-7421-7d7c65b9fe58", "c1e5b641-ef9c-4a49-933f-aace124b6579", "63d7a4c3-c967-41ad-81ca-88dd1d6d5ca3" }, new[] { "abc6accb-3c31-edf8-f36a-bc9bafd6769c" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(corin_Attack_Branch_02_CamShake_E_03.graph1, new[] { "6d9c1772-7f84-632b-07e1-d2a3a420f827", "fe2380c6-da19-4686-3b60-50c00d49c54b", "a3643331-1c55-cd09-8f6c-744993e9201d", "da7b8f33-2c3b-470b-b9a6-0100b01ea066", "58febc79-d50c-43a4-a59a-29640d5ec3f8" }, new[] { "fb056843-ce40-79ba-f2a6-a2b0730da52c" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(corin_Attack_Branch_02_CamShake_E_02.graph, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(corin_Attack_Branch_02_CamShake_E_03.graph1, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimeline(rootParts.timelineData, new[] { "9f818205-3225-8862-14ea-f557459f6b66", "e0867ac0-e6d1-05f4-a290-e6599d401b40", "58cc8b23-34fc-945a-bcb2-a8f2531e1201", "d6ce7242-94a8-c01c-7e78-463359084698" }, new[] { "8ba098b5-ae76-3a6a-4daf-a3082783834d", "f60ca9ea-2098-0da8-4c41-284431256408", "5ea2c15a-4c0e-cdcf-e15d-070e579ebddc", "63ce4cf5-4d0f-08ef-45c0-32673fcc415f", "9392f966-b201-1d4f-7a1b-d3701542f6b2", "419e7a71-a444-71d5-c369-1b1410019cec" }, new[] { "e1950e89-ec94-cc7a-3a5b-f459ea18127d" }, Array.Empty<string>());
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
