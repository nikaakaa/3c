using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityAnimationClip = UnityEngine.AnimationClip;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    internal static class CorinRushTimelineAuthoringBuilder
    {
        internal sealed class Boundary
        {
            internal string WindowType;
            internal string WindowId;
            internal ulong Digest;
            internal int SourceFrame;
        }

        internal static TimelineAsset Build(
            BtsmtlAuthoringGenerationContext context,
            string stateId,
            string branchId,
            string animationPath,
            int sourceDurationFrame,
            IReadOnlyList<Boundary> boundaries)
        {
            string timelineId = BtsmtlRushStableIdentity($"corin.rush.timeline:{stateId}");
            string sectionId = BtsmtlRushStableIdentity($"corin.rush.section:{stateId}");
            string animationTrackId = BtsmtlRushStableIdentity($"corin.rush.track.animation:{stateId}");
            string animationClipId = BtsmtlRushStableIdentity($"corin.rush.clip.animation:{stateId}");
            var animation = context.ResolveExternalAsset<UnityAnimationClip>(animationPath, 7400000L);
            var timeline = BtsmtlSkillAuthoringCode.EnsureTimelineRoot(context, timelineId, $"Corin{stateId.Replace("_", string.Empty)}Timeline");
            var catalog = TimelineTreeContractComposition.Create();
            var section = BtsmtlSkillAuthoringCode.EnsureSection(timeline.Data, sectionId, stateId, 0, string.Empty);
            section.ConfigureBranch(branchId);
            var animationTrack = BtsmtlSkillAuthoringCode.EnsureTrack(timeline.Data, catalog, typeof(AnimationTrack), animationTrackId, "Animation", TimelineExecutionDomain.Presentation);
            var animationClip = BtsmtlSkillAuthoringCode.EnsureClip(timeline.Data, catalog, animationTrack, animationClipId, 0m, animation, sourceDurationFrame / (decimal)TimelineUtility.FrameRate, 0, 0, 0);
            ((AnimationTrack)animationTrack).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)animationTrack).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(timeline.Data, animationClip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            var trackIds = new List<string> { animationTrackId };
            var treeClipIds = new List<string>();
            foreach (var boundary in boundaries)
            {
                string graphId = BtsmtlRushStableIdentity($"corin.rush.window.graph:{stateId}:{boundary.WindowId}:{boundary.SourceFrame}");
                string graphName = $"Open {boundary.WindowType} @{boundary.SourceFrame}";
                string treeClipId = BtsmtlRushStableIdentity($"corin.rush.window.clip:{stateId}:{boundary.WindowId}:{boundary.SourceFrame}");
                string treeTrackId = BtsmtlRushStableIdentity($"corin.rush.window.track:{stateId}:{boundary.WindowId}:{boundary.SourceFrame}");
                var treeTrack = BtsmtlSkillAuthoringCode.EnsureTrack(timeline.Data, catalog, typeof(TreeTrack), treeTrackId,
                    $"Open {boundary.WindowType}", TimelineExecutionDomain.Logic);
                trackIds.Add(treeTrackId);
                var graph = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(timeline, graphId, graphName);
                string declarationId = BtsmtlRushStableIdentity($"corin.rush.window.declaration:{stateId}:{boundary.WindowId}");
                string rootId = BtsmtlRushStableIdentity($"corin.rush.window.node.root:{stateId}:{boundary.WindowId}");
                string setId = BtsmtlRushStableIdentity($"corin.rush.window.node.set:{stateId}:{boundary.WindowId}");
                string edgeId = BtsmtlRushStableIdentity($"corin.rush.window.edge:{stateId}:{boundary.WindowId}");
                var root = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillRootFlowNode), rootId, "技能入口", new Vector2(120f, 260f));
                var set = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillBlackboardSetFlowNode), setId, $"Open {boundary.WindowType}", new Vector2(360f, 260f));
                BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(graph, declarationId, boundary.WindowId, typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Rush/Windows", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, boundary.WindowType, boundary.WindowId, boundary.Digest));
                BtsmtlSkillAuthoringContract.Apply(set, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", declarationId), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", graphId), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
                BtsmtlSkillAuthoringCode.SetValue(set, "m_Value", true);
                BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, root, "Output", set, "Input", edgeId);
                BtsmtlSkillAuthoringCode.PruneFlowGraph(graph, new[] { rootId, setId }, new[] { edgeId });
                BtsmtlSkillAuthoringCode.PruneBlackboard(graph, new[] { declarationId });
                var treeClip = BtsmtlSkillAuthoringCode.EnsureClip(timeline.Data, catalog, treeTrack, treeClipId, (boundary.SourceFrame - 1) / (decimal)TimelineUtility.FrameRate, graph);
                TimelineAuthoringPropertyContract.Apply(timeline.Data, treeClip, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, graph) });
                treeClipIds.Add(treeClipId);
            }
            var clipIds = new List<string> { animationClipId };
            clipIds.AddRange(treeClipIds);
            BtsmtlSkillAuthoringCode.PruneTimeline(timeline.Data, trackIds, clipIds, new[] { sectionId }, Array.Empty<string>());
            return timeline;
        }

        static string BtsmtlRushStableIdentity(string seed) => new Guid(BtsmtlSkillGraphAssetFactory.StableIdentity(seed)).ToString("D");
    }
}
