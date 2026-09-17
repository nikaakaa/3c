using System;
using UnityEngine;
using TreeDesigner;

namespace BTSMTL.Timeline
{
    public enum TimelineTreeExecutionPhase
    {
        Decision,
        Commit
    }

    public interface ITimelineTreeGraphAsset
    {
        string AuthoringId { get; }
        bool IsTimelineTree { get; }
        void CollectTimelineContentClosure(TimelineContentClosureBuilder builder, string sourcePath);
    }

    [TrackGroup("Base"), ScriptGuid("31085f11443fe1347b871c5d69db3774"), IconGuid("e28acf5dc5b2e3d4a97920bf4e831c87"), Ordered(3), Color(201, 060, 032)]
    public class TreeTrack : Track
    {
        public override string ContractKind => TimelineContractKinds.TreeTrack;

#if UNITY_EDITOR
        public override Type ClipType => typeof(TreeClip);

        public override Clip AddClip(UnityEngine.Object referenceObject, int frame)
        {
            if (referenceObject is not ScriptableObject asset || asset is not ITimelineTreeGraphAsset graph || !graph.IsTimelineTree)
                throw new ArgumentException("TreeClip必须绑定正式的Timeline节点图资产。", nameof(referenceObject));
            var clip = new TreeClip(this, frame, asset);
            clip.RegenerateAuthoringIdentity();
            m_Clips.Add(clip);
            return clip;
        }

        public override bool DragValid()
        {
            return UnityEditor.DragAndDrop.objectReferences.Length == 1 &&
                   UnityEditor.DragAndDrop.objectReferences[0] is ScriptableObject asset &&
                   asset is ITimelineTreeGraphAsset graph && graph.IsTimelineTree;
        }
#endif
    }

    [Serializable]
    [ScriptGuid("31085f11443fe1347b871c5d69db3774"), Color(201, 060, 032)]
    public partial class TreeClip : Clip, ITimelineOwnedAuthoringIdentity, ITimelineContentClosureSource, ITimelineClipExecutionPhaseSource
    {
        public override string ContractKind => TimelineContractKinds.TreeClip;

        [SerializeField, ShowInInspector, OnValueChanged("OnClipChanged", "RepaintInspector")]
        TimelineTreeExecutionPhase m_ExecutionPhase = TimelineTreeExecutionPhase.Commit;

        [SerializeField]
        ScriptableObject m_AssetTree;

        public ScriptableObject AssetTree => m_AssetTree;

        public TimelineTreeExecutionPhase ExecutionPhase => m_ExecutionPhase;
        public TimelineClipExecutionPhase TimelineExecutionPhase =>
            m_ExecutionPhase == TimelineTreeExecutionPhase.Decision
                ? TimelineClipExecutionPhase.Decision
                : TimelineClipExecutionPhase.Commit;

        public void SetExecutionPhase(TimelineTreeExecutionPhase phase)
        {
            if (!Enum.IsDefined(typeof(TimelineTreeExecutionPhase), phase))
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "TimelineTree execution phase is invalid.");
            m_ExecutionPhase = phase;
#if UNITY_EDITOR
            OnClipChanged();
#endif
        }

        public void CollectContentClosure(TimelineContentClosureBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (m_AssetTree is not ITimelineTreeGraphAsset graph || !graph.IsTimelineTree)
            {
                builder.AddError("timeline_tree_missing", AuthoringId, "TreeClip没有绑定正式的Timeline节点图资产。");
                return;
            }
            graph.CollectTimelineContentClosure(builder, $"clip:{AuthoringId}/tree:{graph.AuthoringId}");
        }

        public override void Init(Track track)
        {
            base.Init(track);
        }

#if UNITY_EDITOR
        public override string Name => $"{m_ExecutionPhase} / {(m_AssetTree ? m_AssetTree.name : "Missing Graph")}";
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable;

        public TreeClip(Track track, int frame) : base(track, frame)
        {
        }

        public TreeClip(Track track, int frame, ScriptableObject assetTree) : base(track, frame)
        {
            SetAssetTree(assetTree);
        }

        public void RegenerateOwnedAuthoringIdentity()
        {
        }

        public void SetAssetTree(ScriptableObject asset)
        {
            if (asset is not ITimelineTreeGraphAsset graph || !graph.IsTimelineTree)
                throw new ArgumentException("TreeClip需要支持Timeline生命周期入口的正式节点图资产。", nameof(asset));
            m_AssetTree = asset;
            OnClipChanged();
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
