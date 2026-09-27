using BTSMTL.Authoring.Blackboard;
using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityAnimationClip = UnityEngine.AnimationClip;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    internal static class CorinRushTimelineAuthoringBuilder
    {
        internal readonly struct Window
        {
            internal Window(string id, int startFrame, int endFrame, ulong digest)
            {
                Id = id;
                StartFrame = startFrame;
                EndFrame = endFrame;
                Digest = digest;
            }

            internal string Id { get; }
            internal int StartFrame { get; }
            internal int EndFrame { get; }
            internal ulong Digest { get; }
        }

        internal static TimelineAsset Build(
            BtsmtlAuthoringGenerationContext context,
            string stateId,
            string branchId,
            string animationPath,
            int totalFrame,
            params Window[] windows)
        {
            string timelineId = Id($"corin.rush.timeline:{stateId}");
            string sectionId = Id($"corin.rush.section:{stateId}");
            string animationTrackId = Id($"corin.rush.track.animation:{stateId}");
            string treeTrackId = Id($"corin.rush.track.decision:{stateId}");
            string animationClipId = Id($"corin.rush.clip.animation:{stateId}");
            var animation = context.ResolveExternalAsset<UnityAnimationClip>(animationPath, 7400000L);
            var timeline = BtsmtlSkillAuthoringCode.EnsureTimelineRoot(context, timelineId, $"Corin{stateId}Timeline");
            var catalog = TimelineTreeContractComposition.Create();
            var section = BtsmtlSkillAuthoringCode.EnsureSection(timeline.Data, sectionId, stateId, 0, string.Empty);
            section.ConfigureBranch(branchId);
            var animationTrack = BtsmtlSkillAuthoringCode.EnsureTrack(timeline.Data, catalog, typeof(AnimationTrack), animationTrackId, "Animation", TimelineExecutionDomain.Presentation);
            var animationClip = BtsmtlSkillAuthoringCode.EnsureClip(timeline.Data, catalog, animationTrack, animationClipId, 0, animation, Seconds(totalFrame), 0, 0, 0);
            ((AnimationTrack)animationTrack).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)animationTrack).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(timeline.Data, animationClip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            var trackIds = new List<string> { animationTrackId };
            var clipIds = new List<string> { animationClipId };
            string motionName = System.IO.Path.GetFileNameWithoutExtension(animationPath);
            var motion = context.ResolveExternalAsset<RootMotionCurveAsset>(
                "Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinActionMotion/" + motionName + ".asset", 11400000L);
            string motionTrackId = Id($"corin.rush.track.motion:{stateId}");
            string motionClipId = Id($"corin.rush.clip.motion:{stateId}");
            var motionTrack = BtsmtlSkillAuthoringCode.EnsureTrack(timeline.Data, catalog, typeof(MotionCurveTrack), motionTrackId, "Motion Curve", TimelineExecutionDomain.Logic);
            var motionClip = BtsmtlSkillAuthoringCode.EnsureClip(timeline.Data, catalog, motionTrack, motionClipId, 0m, motion, Seconds(totalFrame), 0m, 0m, 0m);
            float sourceEndTime = (float)Seconds(totalFrame);
            if ((double)sourceEndTime > (double)Seconds(totalFrame))
                sourceEndTime = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(sourceEndTime) - 1);
            TimelineAuthoringPropertyContract.Apply(timeline.Data, motionClip, new[]
            {
                new TimelineAuthoringPropertyValue("curveId", TimelineAuthoringPropertyKind.Text, stateId),
                new TimelineAuthoringPropertyValue("sourceCurve", TimelineAuthoringPropertyKind.Object, motion),
                new TimelineAuthoringPropertyValue("sourceEndTime", TimelineAuthoringPropertyKind.Float, sourceEndTime)
            });
            trackIds.Add(motionTrackId);
            clipIds.Add(motionClipId);
            if (windows.Length > 0)
            {
                var track = BtsmtlSkillAuthoringCode.EnsureTrack(timeline.Data, catalog, typeof(TreeTrack), treeTrackId, "Action Windows", TimelineExecutionDomain.Logic);
                trackIds.Add(treeTrackId);
                foreach (Window window in windows)
                    clipIds.Add(AddWindow(timeline, catalog, track, stateId, window));
            }
            BtsmtlSkillAuthoringCode.PruneTimeline(timeline.Data, trackIds, clipIds, new[] { sectionId }, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimelineMarkers(timeline.Data, Array.Empty<string>());
            BtsmtlSkillOwnedAssets.ReleaseOrphaned(timeline);
            CorinCameraTimelineAuthoring.Apply(timeline.Data, "Corin_" + stateId, 0m, Seconds(totalFrame));
            return timeline;
        }

        static string AddWindow(TimelineAsset timeline, TimelineContractCatalog catalog, Track track, string state, Window window)
        {
            string seed = $"corin.rush.window:{state}:{window.Id}";
            string ownerId = Id(seed + ":graph");
            string variableId = Id(seed + ":declaration");
            string clipId = Id(seed + ":clip");
            var graph = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(timeline, ownerId, window.Id, BtsmtlSkillFlowGraphRole.TimelineBody);
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(timeline.Data, catalog, track, clipId, Seconds(window.StartFrame), graph);
            TimelineAuthoringPropertyContract.Apply(timeline.Data, clip, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, graph) });
            var endRule = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(graph, Id(seed + ":end-rule"), typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "窗口结束");
            var root = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillRootFlowNode), Id(seed + ":root"), "技能入口", new Vector2(0f, 0f));
            var selector = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillSelectorFlowNode), Id(seed + ":selector"), "窗口执行或结束", new Vector2(200f, 0f));
            var set = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillBlackboardSetFlowNode), Id(seed + ":set"), window.Id, new Vector2(400f, 80f));
            var exit = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillTimelineExitRequestFlowNode), Id(seed + ":exit"), "结束片段", new Vector2(400f, -80f));
            var time = BtsmtlSkillAuthoringCode.EnsureFlowNode(endRule, typeof(BtsmtlSkillTimelineTimeFlowNode), Id(seed + ":time"), "Timeline时间", new Vector2(0f, 0f));
            var reached = BtsmtlSkillAuthoringCode.EnsureFlowNode(endRule, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), Id(seed + ":reached"), "结束时间", new Vector2(200f, 0f));
            var result = BtsmtlSkillAuthoringCode.EnsureFlowNode(endRule, typeof(BtsmtlSkillConditionResultFlowNode), Id(seed + ":result"), "条件结果", new Vector2(400f, 0f));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(graph, variableId, window.Id, typeof(bool), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Rush/Windows", null,
                new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, window.Id, window.Id, window.Digest));
            BtsmtlSkillAuthoringContract.Apply(set, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", variableId), new BtsmtlSkillAuthoringFieldValue("ownerId", ownerId), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(set, "m_Value", true);
            BtsmtlSkillAuthoringCode.SetValue(reached, "b", (float)Seconds(window.EndFrame));
            BtsmtlSkillAuthoringContract.Apply(selector, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", endRule, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            var entryEdge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, root, "Output", selector, "Input", Id(seed + ":entry"));
            var endEdge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, selector, "end", exit, "Input", Id(seed + ":end"));
            var bodyEdge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, selector, "body", set, "Input", Id(seed + ":body"));
            var timeEdge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(endRule, time, "m_Output", reached, "a", Id(seed + ":time-edge"));
            var resultEdge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(endRule, reached, "Value", result, "m_Result", Id(seed + ":result-edge"));
            BtsmtlSkillAuthoringCode.PruneFlowGraph(graph, new[] { root.UID, selector.UID, set.UID, exit.UID }, new[] { entryEdge.UID, endEdge.UID, bodyEdge.UID });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(endRule, new[] { time.UID, reached.UID, result.UID }, new[] { timeEdge.UID, resultEdge.UID });
            BtsmtlSkillAuthoringCode.PruneBlackboard(graph, new[] { variableId });
            BtsmtlSkillAuthoringCode.PruneBlackboard(endRule, Array.Empty<string>());
            return clipId;
        }

        static decimal Seconds(int frame) => frame / (decimal)TimelineUtility.FrameRate;
        static string Id(string seed) => new Guid(BtsmtlSkillGraphAssetFactory.StableIdentity(seed)).ToString("D");
    }
}
