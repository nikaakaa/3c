using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using UnityAnimationClip = UnityEngine.AnimationClip;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    internal static class CorinRushTimelineAuthoringBuilder
    {
        internal sealed class Boundary
        {
            internal string Target;
            internal string Condition;
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
            string treeTrackId = BtsmtlRushStableIdentity($"corin.rush.track.decision:{stateId}");
            string animationClipId = BtsmtlRushStableIdentity($"corin.rush.clip.animation:{stateId}");
            var animation = context.ResolveExternalAsset<UnityAnimationClip>(animationPath, 7400000L);
            var timeline = BtsmtlSkillAuthoringCode.EnsureTimelineRoot(context, timelineId, $"Corin{stateId}Timeline");
            var catalog = TimelineTreeContractComposition.Create();
            var section = BtsmtlSkillAuthoringCode.EnsureSection(timeline.Data, sectionId, stateId, 0, string.Empty);
            section.ConfigureBranch(branchId);
            var animationTrack = BtsmtlSkillAuthoringCode.EnsureTrack(timeline.Data, catalog, typeof(AnimationTrack), animationTrackId, "Animation", TimelineExecutionDomain.Presentation);
            var animationClip = BtsmtlSkillAuthoringCode.EnsureClip(timeline.Data, catalog, animationTrack, animationClipId, 0m, animation, sourceDurationFrame / (decimal)TimelineUtility.FrameRate, 0, 0, 0);
            ((AnimationTrack)animationTrack).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)animationTrack).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(timeline.Data, animationClip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            var treeTrack = BtsmtlSkillAuthoringCode.EnsureTrack(timeline.Data, catalog, typeof(TreeTrack), treeTrackId, "StateBoundary", TimelineExecutionDomain.Logic);
            var treeClipIds = new List<string>();
            foreach (var boundary in boundaries)
            {
                string graphId = BtsmtlRushStableIdentity($"corin.rush.decision.graph:{stateId}:{boundary.Target}:{boundary.Condition}:{boundary.SourceFrame}");
                string graphName = $"Boundary {stateId} {boundary.Target} {boundary.Condition} @{boundary.SourceFrame}";
                string treeClipId = BtsmtlRushStableIdentity($"corin.rush.decision.clip:{stateId}:{boundary.Target}:{boundary.Condition}:{boundary.SourceFrame}");
                var graph = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(timeline, graphId, graphName);
                var treeClip = BtsmtlSkillAuthoringCode.EnsureClip(timeline.Data, catalog, treeTrack, treeClipId, (boundary.SourceFrame - 1) / (decimal)TimelineUtility.FrameRate, graph);
                TimelineAuthoringPropertyContract.Apply(timeline.Data, treeClip, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, graph) });
                treeClipIds.Add(treeClipId);
            }
            var trackIds = new[] { animationTrackId, treeTrackId };
            var clipIds = new List<string> { animationClipId };
            clipIds.AddRange(treeClipIds);
            BtsmtlSkillAuthoringCode.PruneTimeline(timeline.Data, trackIds, clipIds, new[] { sectionId }, Array.Empty<string>());
            return timeline;
        }

        static string BtsmtlRushStableIdentity(string seed) => new Guid(BtsmtlSkillGraphAssetFactory.StableIdentity(seed)).ToString("D");
    }
}
