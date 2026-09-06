using System;
using System.Collections.Generic;
using UnityEngine;
using TreeDesigner;

namespace BTSMTL.Timeline
{
    public enum TimelineTreeExecutionPhase
    {
        Decision,
        Commit
    }

    public enum TimelineTreeOwnership
    {
        Missing,
        Inline,
        Shared
    }

    [TrackGroup("Base"), ScriptGuid("31085f11443fe1347b871c5d69db3774"), IconGuid("e28acf5dc5b2e3d4a97920bf4e831c87"), Ordered(3), Color(201, 060, 032)]
    public class TreeTrack : Track
    {
        public override string ContractKind => TimelineContractKinds.TreeTrack;

#if UNITY_EDITOR
        public override Type ClipType => typeof(TreeClip);

        public override Clip AddClip(UnityEngine.Object referenceObject, int frame)
        {
            var clip = new TreeClip(this, frame);
            clip.RegenerateAuthoringIdentity();
            if (referenceObject is BaseTreeAsset treeAsset && treeAsset.Tree is TimelineRunningTree)
                clip.SetSharedTreeAsset(treeAsset);
            m_Clips.Add(clip);
            return clip;
        }

        public override bool DragValid()
        {
            return UnityEditor.DragAndDrop.objectReferences.Length == 1 &&
                   UnityEditor.DragAndDrop.objectReferences[0] is BaseTreeAsset treeAsset &&
                   treeAsset.Tree is TimelineRunningTree;
        }
#endif
    }

    [Serializable]
    [ScriptGuid("31085f11443fe1347b871c5d69db3774"), ClipInspectorView("TreeClipInspectorView"), Color(201, 060, 032)]
    public partial class TreeClip : Clip, ITimelineOwnedAuthoringIdentity, ITimelineContentClosureSource, ITimelineClipExecutionPhaseSource, ITimelineNestedSerializedOwner
    {
        public override string ContractKind => TimelineContractKinds.TreeClip;

        [SerializeField, ShowInInspector, OnValueChanged("OnClipChanged", "RepaintInspector")]
        TimelineTreeExecutionPhase m_ExecutionPhase = TimelineTreeExecutionPhase.Commit;

        [SerializeReference]
        TimelineRunningTree m_InlineTree;

        [SerializeField]
        BaseTreeAsset m_SharedTreeAsset;

        public TimelineTreeExecutionPhase ExecutionPhase => m_ExecutionPhase;
        public TimelineClipExecutionPhase TimelineExecutionPhase =>
            m_ExecutionPhase == TimelineTreeExecutionPhase.Decision
                ? TimelineClipExecutionPhase.Decision
                : TimelineClipExecutionPhase.Commit;
        public TimelineRunningTree InlineTree => m_InlineTree;
        public BaseTreeAsset SharedTreeAsset => m_SharedTreeAsset;
        public TimelineRunningTree ResolvedTree => m_SharedTreeAsset ? m_SharedTreeAsset.Tree as TimelineRunningTree : m_InlineTree;
        public TimelineTreeOwnership Ownership => m_SharedTreeAsset
            ? TimelineTreeOwnership.Shared
            : m_InlineTree != null
                ? TimelineTreeOwnership.Inline
                : TimelineTreeOwnership.Missing;

        public void CollectContentClosure(TimelineContentClosureBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (!ResolvedTree)
            {
                builder.AddError("timeline_tree_missing", AuthoringId, "TreeClip has no resolved Timeline tree.");
                return;
            }
            CollectTree(
                ResolvedTree,
                $"clip:{AuthoringId}/tree:{ResolvedTree.GraphAuthoringId}",
                builder,
                new HashSet<string>(StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal));
        }

        public override void Init(Track track)
        {
            base.Init(track);
            BindInlineTree();
        }

        public void SetExecutionPhase(TimelineTreeExecutionPhase phase)
        {
            m_ExecutionPhase = phase;
#if UNITY_EDITOR
            OnClipChanged();
#endif
        }

        public void SetInlineTree(TimelineRunningTree tree)
        {
            m_InlineTree = tree;
            m_SharedTreeAsset = null;
            BindInlineTree();
#if UNITY_EDITOR
            OnClipChanged();
#endif
        }

        public void SetSharedTreeAsset(BaseTreeAsset treeAsset)
        {
            if (treeAsset && !(treeAsset.Tree is TimelineRunningTree))
                throw new ArgumentException("Shared Tree asset must contain TimelineRunningTree.", nameof(treeAsset));

            m_SharedTreeAsset = treeAsset;
            if (m_SharedTreeAsset)
                m_InlineTree = null;
#if UNITY_EDITOR
            OnClipChanged();
#endif
        }

        void BindInlineTree()
        {
            if (m_InlineTree == null || Track?.Timeline == null)
                return;

            int trackIndex = Track.Timeline.Tracks.IndexOf(Track);
            int clipIndex = Track.Clips.IndexOf(this);
            if (trackIndex < 0 || clipIndex < 0)
                return;

            BindNestedSerializedOwner(
                Track.Timeline.SerializedOwner,
                Track.Timeline.GetSerializedPropertyPath($"m_Tracks.Array.data[{trackIndex}].m_Clips.Array.data[{clipIndex}]"));
        }

        public void BindNestedSerializedOwner(UnityEngine.Object owner, string propertyPath)
        {
            if (m_InlineTree != null)
                m_InlineTree.BindSerializedOwner(owner, $"{propertyPath}.m_InlineTree");
        }

        static void CollectTree(
            BaseTree tree,
            string sourcePath,
            TimelineContentClosureBuilder builder,
            HashSet<string> active,
            HashSet<string> visited)
        {
            string identity = $"tree:{tree.GraphAuthoringId}";
            if (!active.Add(identity))
            {
                builder.AddError("timeline_tree_recursive", sourcePath, $"Tree reference '{identity}' is recursive.");
                return;
            }
            if (!visited.Add(identity))
            {
                active.Remove(identity);
                return;
            }

            builder.AddDependency(identity, "timeline.tree", sourcePath, GraphAuthoringFingerprint.Compute(tree));
            for (int nodeIndex = 0; nodeIndex < tree.Nodes.Count; nodeIndex++)
            {
                BaseNode node = tree.Nodes[nodeIndex];
                if (node == null)
                    continue;
                foreach (NodeGraphReference reference in node.GetGraphReferences())
                {
                    if (reference.Tree == null)
                    {
                        if (reference.Required)
                            builder.AddError(
                                "timeline_tree_reference_missing",
                                $"{sourcePath}/node:{node.GUID}/reference:{reference.Key}",
                                "Required tree reference is missing.");
                        continue;
                    }
                    CollectTree(
                        reference.Tree,
                        $"{sourcePath}/node:{node.GUID}/reference:{reference.Key}/tree:{reference.Tree.GraphAuthoringId}",
                        builder,
                        active,
                        visited);
                }
            }
            active.Remove(identity);
        }

#if UNITY_EDITOR
        public override string Name => $"{m_ExecutionPhase} / {(ResolvedTree ? ResolvedTree.name : "Missing Tree")}";
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable;

        public TreeClip(Track track, int frame) : base(track, frame)
        {
            SetInlineTree(TimelineRunningTree.CreateDefault("Timeline Tree"));
        }

        public void EnsureInlineTree()
        {
            if (!m_SharedTreeAsset && m_InlineTree == null)
                SetInlineTree(TimelineRunningTree.CreateDefault("Timeline Tree"));
        }

        public void RegenerateOwnedAuthoringIdentity()
        {
            if (m_InlineTree != null)
                m_InlineTree = m_InlineTree.CloneForAuthoring();
        }

        void OnClipChanged()
        {
            OnNameChanged?.Invoke();
        }
#endif
    }

    internal static class TreeTimelineContracts
    {
        public static readonly ITimelineContractProvider Provider = new TimelineContractProvider(
            new[]
            {
                new TimelineTrackContract(
                    TimelineContractKinds.TreeTrack,
                    TimelineTrackOverlapPolicy.Parallel,
                    TimelineCapability.Tree,
                    TimelineContractKinds.TreeClip)
            },
            new[]
            {
                new TimelineClipContract(
                    TimelineContractKinds.TreeClip,
                    TimelineContractKinds.TreeTrack,
                    TimelineClipExecutionPhase.DecisionAndCommit,
                    TimelineCapability.Tree,
                    true,
                    true)
            });
    }
}
