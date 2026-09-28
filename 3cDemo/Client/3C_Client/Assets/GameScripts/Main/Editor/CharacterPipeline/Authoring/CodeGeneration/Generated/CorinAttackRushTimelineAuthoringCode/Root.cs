using System;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushTimelineAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            var asset = context.ResolveExternalAsset<UnityAnimationClip>("Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Rush_FootMotionTarget.anim", 7400000L);
            parts.timeline = BtsmtlSkillAuthoringCode.EnsureTimelineRoot(context, "606351d5-2e51-5730-3083-9e61df7814be", "CorinAttackRushTimeline");
            parts.timelineData = parts.timeline.Data;
            parts.timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(AnimationTrack), "96b6f5ae-9018-e3a7-4bb9-06cb7613ce62", "Animation", TimelineExecutionDomain.Presentation);
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track, "a00919f2-e3a4-c306-f4d8-a6ffaa805aa7", 0m, asset, 1.166666666744276881217956543m, 0m, 0m, 0m);
            parts.track1 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(TreeTrack), "a1c374d2-f94a-8eeb-0b57-aef78168d0e9", "Open RushRelease", TimelineExecutionDomain.Logic);
            parts.track2 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(TreeTrack), "e61b344f-07a7-cc70-8699-53e94fbef4cf", "Camera Triggers", TimelineExecutionDomain.Presentation);
            BtsmtlSkillAuthoringCode.EnsureSection(parts.timelineData, "86b751b3-24d8-fc3c-3f0c-1d6dfa88f07e", "Attack_Rush", 0m, "");
            ((AnimationTrack)track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, Open_RushRelease__13Parts open_RushRelease__13, Corin_Attack_Rush_CamShake_E_01Parts corin_Attack_Rush_CamShake_E_01, BtsmtlAuthoringGenerationContext context)
        {
            rootParts.clip1 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track1, "afd9b28a-5319-5f6a-37d7-7d1600dda9da", 0.1999999999534338712692260742m, open_RushRelease__13.graph);
            rootParts.clip2 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track2, "56831f22-ceb3-c8c2-f56a-e778a5b80b89", 0.1333333333022892475128173828m, corin_Attack_Rush_CamShake_E_01.graph2, 0.1499999999068677425384521484m, 0m, 0m, 0m);
            rootParts.clip3 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track2, "22b62699-b954-9c6b-01d7-85a137844e58", 0.3666666666977107524871826172m, corin_Attack_Rush_CamShake_E_01.graph3, 0.3833333333022892475128173828m, 0m, 0m, 0m);
            rootParts.clip4 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track2, "7fa0ae52-8784-f77c-7578-412329dcacf9", 0.6333333333022892475128173828m, corin_Attack_Rush_CamShake_E_01.graph4, 0.6499999999068677425384521484m, 0m, 0m, 0m);
            rootParts.clip5 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track2, "93357c47-c723-4d69-0e9d-9b22ab198ee6", 0.8999999999068677425384521484m, corin_Attack_Rush_CamShake_E_01.graph5, 0.916666666744276881217956543m, 0m, 0m, 0m);
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip1, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, open_RushRelease__13.graph) });
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip2, new[] { new TimelineAuthoringPropertyValue("exitSource", TimelineAuthoringPropertyKind.Enum, TimelineClipExitSource.FrameBoundary), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, corin_Attack_Rush_CamShake_E_01.graph2) });
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip3, new[] { new TimelineAuthoringPropertyValue("exitSource", TimelineAuthoringPropertyKind.Enum, TimelineClipExitSource.FrameBoundary), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, corin_Attack_Rush_CamShake_E_01.graph3) });
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip4, new[] { new TimelineAuthoringPropertyValue("exitSource", TimelineAuthoringPropertyKind.Enum, TimelineClipExitSource.FrameBoundary), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, corin_Attack_Rush_CamShake_E_01.graph4) });
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip5, new[] { new TimelineAuthoringPropertyValue("exitSource", TimelineAuthoringPropertyKind.Enum, TimelineClipExitSource.FrameBoundary), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, corin_Attack_Rush_CamShake_E_01.graph5) });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(open_RushRelease__13.graph, new[] { "67fc193c-1f8a-459f-99be-c2674e0f807d", "824479d8-fe60-97da-b837-7057b4ffb3c4", "a1801a530b3621874f90fcc30c1c2980", "588176fd56347925b53c0e1ce37b96bf", "5ff7e22c-25ab-4ff1-da33-d305fd403eb7", "fdfe95d8-c235-4b5d-9442-5c824dd83988", "6b4dca86-bbd8-46b4-a5cd-d046ea7c4dc0" }, new[] { "6f1a8f7714e164cf8e97702130fb84bd", "a017b7ed23355b5d3f1e44326e2e4c78", "fb4a7a8e524dad90c3ea2318e796e560" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(open_RushRelease__13.graph1, new[] { "240d2a8b7d752774a688c297ae71d41d", "9e1c2a215105285fc3e17f3f53cb4541", "febbbc66-4d44-4b28-85d4-be0def2ef40a" }, new[] { "4c3793fff2fc498c75f8bd420a6e5166", "83b8cf6cf59ace8c57bfa762ea002fea" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(corin_Attack_Rush_CamShake_E_01.graph2, new[] { "202b8259-91b1-9d35-d581-68bc3d0fe3c4", "cdbffe94-a856-9297-8935-b1bfc2746bf5", "0c2ffdf9-fc1e-2fd6-536b-d485d3fdd56c", "660d3958-8c3b-4645-bddf-ea25abb94522", "1e12267f-8472-4690-a488-7f32162b2e8a" }, new[] { "a7da3379-ca62-ad46-a61f-b5a4aa15c5f5" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(corin_Attack_Rush_CamShake_E_01.graph3, new[] { "22677920-f939-e5c6-70a2-3bb9297a49de", "7e7372d8-3855-5653-5de1-fe518b1f804e", "ffea03cf-4f6a-6103-a18d-7edb6b63423e", "eb9dde8a-4d0a-4ac7-b5f0-fc9551d93507", "8beab8de-ef02-4e75-a094-2106adb641cc" }, new[] { "d11d6392-9d00-927a-e51a-5b4c9e42257b" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(corin_Attack_Rush_CamShake_E_01.graph4, new[] { "6ce19b42-5e9f-2fbd-fa45-1b2c2f0a4cdd", "9fe45e8c-70fc-b9bf-71d2-ba978d4349d7", "388d278d-70e2-2549-9a42-992ca9dbf386", "c3e7f001-44f0-421b-af43-a3e77224b162", "4267d576-c00f-4f3f-8391-d9a4c752df26" }, new[] { "29cfd37c-f349-155b-9866-7df6062b2060" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(corin_Attack_Rush_CamShake_E_01.graph5, new[] { "f19c7a3e-dbfd-119b-a13b-5c592d87f856", "118770cc-a637-f401-32c6-db5910133e4e", "b4adb882-a4da-08e6-dcdb-4c116c1321a0", "4f3b565b-be74-4402-be62-7c5e7371edbd", "18cefd00-a29c-4b7f-ab72-c30bc6ee9bc0" }, new[] { "9cd95102-5e96-4dd3-c0d4-025df1a9e741" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(open_RushRelease__13.graph, new[] { "2087927f-c32b-0c89-864a-fbf269fba556" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(open_RushRelease__13.graph1, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(corin_Attack_Rush_CamShake_E_01.graph2, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(corin_Attack_Rush_CamShake_E_01.graph3, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(corin_Attack_Rush_CamShake_E_01.graph4, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(corin_Attack_Rush_CamShake_E_01.graph5, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimeline(rootParts.timelineData, new[] { "96b6f5ae-9018-e3a7-4bb9-06cb7613ce62", "a1c374d2-f94a-8eeb-0b57-aef78168d0e9", "e61b344f-07a7-cc70-8699-53e94fbef4cf" }, new[] { "a00919f2-e3a4-c306-f4d8-a6ffaa805aa7", "afd9b28a-5319-5f6a-37d7-7d1600dda9da", "56831f22-ceb3-c8c2-f56a-e778a5b80b89", "22b62699-b954-9c6b-01d7-85a137844e58", "7fa0ae52-8784-f77c-7578-412329dcacf9", "93357c47-c723-4d69-0e9d-9b22ab198ee6" }, new[] { "86b751b3-24d8-fc3c-3f0c-1d6dfa88f07e" }, Array.Empty<string>());
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
            internal Clip clip3;
            internal Clip clip4;
            internal Clip clip5;
            internal TimelineContractCatalog timelineCatalog;
        }
    }
}
