using BTSMTL.Timeline;
using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;
using UnityAnimationClip = UnityEngine.AnimationClip;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    internal static class CorinBranchTimelineAuthoring
    {
        internal static BtsmtlAuthoringGenerationResult Generate(
            BtsmtlAuthoringGenerationContext context,
            string stage,
            string clipSuffix,
            decimal duration,
            bool loop)
        {
            var animation = context.ResolveExternalAsset<UnityAnimationClip>(
                "Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Branch_" +
                clipSuffix + "_FootMotionTarget.anim", 7400000L);
            TimelineAsset timeline = BtsmtlSkillAuthoringCode.EnsureTimelineRoot(
                context, Id(stage, "timeline"), "CorinBranch" + stage + "Timeline");
            TimelineData data = timeline.Data;
            data.Loop = loop;
            TimelineContractCatalog catalog = TimelineTreeContractComposition.Create();
            Track track = BtsmtlSkillAuthoringCode.EnsureTrack(
                data, catalog, typeof(AnimationTrack), Id(stage, "animation-track"),
                "Animation", TimelineExecutionDomain.Presentation);
            Clip clip = BtsmtlSkillAuthoringCode.EnsureClip(
                data, catalog, track, Id(stage, "animation-clip"),
                0m, animation, duration, 0m, 0m, 0m);
            BtsmtlSkillAuthoringCode.EnsureSection(data, Id(stage, "section"), "Attack_Branch_02_" + stage, 0m, "");
            ((AnimationTrack)track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(data, clip, new[]
            {
                new TimelineAuthoringPropertyValue(
                    "blendProfileId", TimelineAuthoringPropertyKind.Text,
                    "corin.animation-rig.action-blend-profile")
            });
            var motion = context.ResolveExternalAsset<RootMotionCurveAsset>(
                "Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinActionMotion/Avatar_Female_Size01_Corin_Ani_Attack_Branch_" + clipSuffix + "_FootMotionTarget.asset", 11400000L);
            var motionTrack = BtsmtlSkillAuthoringCode.EnsureTrack(data, catalog, typeof(MotionCurveTrack), Id(stage, "motion-track"), "Motion Curve", TimelineExecutionDomain.Logic);
            var motionClip = BtsmtlSkillAuthoringCode.EnsureClip(data, catalog, motionTrack, Id(stage, "motion-clip"), 0m, motion, duration, 0m, 0m, 0m);
            float sourceEndTime = (float)duration;
            if ((double)sourceEndTime > (double)duration)
                sourceEndTime = System.BitConverter.Int32BitsToSingle(System.BitConverter.SingleToInt32Bits(sourceEndTime) - 1);
            TimelineAuthoringPropertyContract.Apply(data, motionClip, new[]
            {
                new TimelineAuthoringPropertyValue("curveId", TimelineAuthoringPropertyKind.Text, "Branch" + stage),
                new TimelineAuthoringPropertyValue("sourceCurve", TimelineAuthoringPropertyKind.Object, motion),
                new TimelineAuthoringPropertyValue("sourceEndTime", TimelineAuthoringPropertyKind.Float, sourceEndTime)
            });
            bool hasReleaseWindow = stage == "Start";
            if (hasReleaseWindow)
                AddReleaseWindow(timeline, catalog, stage, duration, sourceEndTime);
            BtsmtlSkillAuthoringCode.PruneTimeline(
                data, hasReleaseWindow ? new[] { Id(stage, "animation-track"), Id(stage, "motion-track"), Id(stage, "release-track") } : new[] { Id(stage, "animation-track"), Id(stage, "motion-track") },
                hasReleaseWindow ? new[] { Id(stage, "animation-clip"), Id(stage, "motion-clip"), Id(stage, "release-clip") } : new[] { Id(stage, "animation-clip"), Id(stage, "motion-clip") },
                new[] { Id(stage, "section") }, System.Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimelineMarkers(data, System.Array.Empty<string>());
            string pattern = stage == "Start" ? "Corin_Attack_Branch_02" : "Corin_Attack_Branch_02_" + stage;
            CorinCameraTimelineAuthoring.Apply(data, pattern, 0m, duration);
            return context.Complete(timeline);
        }

        static void AddReleaseWindow(TimelineAsset timeline, TimelineContractCatalog catalog, string stage, decimal duration, float endTime)
        {
            string ownerId = Id(stage, "release-graph");
            string variableId = Id(stage, "release-window");
            var graph = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(timeline, ownerId, "Branch Release Window");
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(timeline.Data, catalog, typeof(TreeTrack), Id(stage, "release-track"), "Branch Release", TimelineExecutionDomain.Logic);
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(timeline.Data, catalog, track, Id(stage, "release-clip"), 64m / 60m, graph, duration, 0m, 0m, 0m);
            TimelineAuthoringPropertyContract.Apply(timeline.Data, clip, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, graph) });
            var endRule = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(
                graph, Id(stage, "release-end-rule"), typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Release Window End");
            var root = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillRootFlowNode), Id(stage, "release-root"), "技能入口", new Vector2(0f, 0f));
            var selector = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillSelectorFlowNode), Id(stage, "release-selector"), "窗口执行或结束", new Vector2(200f, 0f));
            var set = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillBlackboardSetFlowNode), Id(stage, "release-set"), "Open BranchRelease", new Vector2(400f, 80f));
            var exit = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillTimelineExitRequestFlowNode), Id(stage, "release-exit"), "结束片段", new Vector2(400f, -80f));
            var time = BtsmtlSkillAuthoringCode.EnsureFlowNode(endRule, typeof(BtsmtlSkillTimelineTimeFlowNode), Id(stage, "release-time"), "Timeline时间", new Vector2(0f, 0f));
            var reached = BtsmtlSkillAuthoringCode.EnsureFlowNode(endRule, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), Id(stage, "release-reached"), "结束时间", new Vector2(200f, 0f));
            var result = BtsmtlSkillAuthoringCode.EnsureFlowNode(endRule, typeof(BtsmtlSkillConditionResultFlowNode), Id(stage, "release-result"), "条件结果", new Vector2(400f, 0f));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(graph, variableId, "BranchRelease", typeof(bool), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Branch/Windows", null,
                new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "BranchRelease", "BranchRelease", 9001UL));
            BtsmtlSkillAuthoringContract.Apply(set, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", variableId), new BtsmtlSkillAuthoringFieldValue("ownerId", ownerId), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(set, "m_Value", true);
            BtsmtlSkillAuthoringCode.SetValue(reached, "b", endTime);
            BtsmtlSkillAuthoringContract.Apply(selector, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", endRule, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, root, "Output", selector, "Input", Id(stage, "release-root-selector"));
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, selector, "end", exit, "Input", Id(stage, "release-selector-exit"));
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, selector, "body", set, "Input", Id(stage, "release-selector-set"));
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(endRule, time, "m_Output", reached, "a", Id(stage, "release-time-reached"));
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(endRule, reached, "Value", result, "m_Result", Id(stage, "release-reached-result"));

        }

        static string Id(string stage, string part) =>
            new System.Guid(BtsmtlSkillGraphAssetFactory.StableIdentity(
                "corin.branch.02." + stage + "." + part)).ToString("D");
    }
}
