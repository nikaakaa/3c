using System;
using ThirdPersonSimulation.Fixed;
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

    [TrackGroup("Base"), ScriptGuid("31085f11443fe1347b871c5d69db3774"), IconGuid("e28acf5dc5b2e3d4a97920bf4e831c87"), Ordered(3), Color(201, 060, 032)]
#if UNITY_EDITOR
    [TimelineAuthoringTrackField("executionDomain", "timeline_tree_execution_domain_missing", "Tree Track必须选择Logic或Presentation执行域。")]
#endif
    public class TreeTrack : Track
#if UNITY_EDITOR
        , ITimelineAuthoringTrackFieldSink
#endif
    {
        public override string ContractKind => TimelineContractKinds.TreeTrack;

#if UNITY_EDITOR
        public void ApplyAuthoringField(string fieldId, string value)
        {
            if (fieldId != "executionDomain" ||
                !Enum.TryParse(value, true, out TimelineExecutionDomain executionDomain) ||
                executionDomain != TimelineExecutionDomain.Logic && executionDomain != TimelineExecutionDomain.Presentation)
            {
                throw new ArgumentException("Tree Track执行域必须是Logic或Presentation。", nameof(value));
            }
            ConfigureExecutionDomain(executionDomain);
        }

        public override Type ClipType => typeof(TreeClip);

        public override Clip AddClip(FixedScalar time)
        {
            throw new InvalidOperationException("TreeClip必须绑定正式的Timeline节点图资产。");
        }

        public override Clip AddClip(UnityEngine.Object referenceObject, FixedScalar time)
        {
            if (referenceObject is not ScriptableObject asset || asset is not ITimelineTreeGraphAsset graph || !graph.IsTimelineTree)
                throw new ArgumentException("TreeClip必须绑定正式的Timeline节点图资产。", nameof(referenceObject));
            var clip = new TreeClip(this, time, asset);
            clip.RegenerateAuthoringIdentity();
            if (ExecutionDomain == TimelineExecutionDomain.Presentation)
                clip.SetExitSource(TimelineClipExitSource.FrameBoundary);
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
    public partial class TreeClip : Clip, ITimelineOwnedAuthoringIdentity, ITimelineContentClosureSource, ITimelineClipExecutionPhaseSource, ITimelineClipExitSource, ITimelineTerminalTimeAlignedClip
    {
        public override string ContractKind => TimelineContractKinds.TreeClip;

        [SerializeField, ShowInInspector, OnValueChanged("OnClipChanged", "RepaintInspector")]
        TimelineTreeExecutionPhase m_ExecutionPhase = TimelineTreeExecutionPhase.Commit;

        [SerializeField, ShowInInspector, OnValueChanged("OnClipChanged", "RepaintInspector")]
        TimelineClipExitSource m_ExitSource = TimelineClipExitSource.TreeDecision;

        [SerializeField]
        ScriptableObject m_AssetTree;


        public ScriptableObject AssetTree => m_AssetTree;

        public TimelineTreeExecutionPhase ExecutionPhase => m_ExecutionPhase;
        public TimelineClipExitSource ClipExitSource => m_ExitSource;

        public void SetExitSource(TimelineClipExitSource source)
        {
            if (!Enum.IsDefined(typeof(TimelineClipExitSource), source))
                throw new ArgumentOutOfRangeException(nameof(source), source, "TimelineTree exit source is invalid.");
            if (ExecutionDomain == TimelineExecutionDomain.Logic && source != TimelineClipExitSource.TreeDecision)
                throw new InvalidOperationException("Logic TreeClip必须由节点图决定退出。");
            if (ExecutionDomain == TimelineExecutionDomain.Presentation && source != TimelineClipExitSource.FrameBoundary)
                throw new InvalidOperationException("Presentation TreeClip只能由帧边界结束。");
            m_ExitSource = source;
#if UNITY_EDITOR
            OnClipChanged();
#endif
        }
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
            if (!TimelineClipExecutionPolicy.FromDomain(ExecutionDomain).IsLogic)
                return;
            if (m_AssetTree is not ITimelineTreeGraphAsset graph || !graph.IsTimelineTree)
            {
                builder.AddError("timeline_tree_missing", AuthoringId, "TreeClip没有绑定正式的Timeline节点图资产。");
                return;
            }
            graph.CollectTimelineContentClosure(builder, $"clip:{AuthoringId}/tree:{graph.AuthoringId}", ExecutionDomain);
        }

        public bool AlignTerminalTime(FixedScalar terminalTime)
        {
            if (ExecutionDomain != TimelineExecutionDomain.Logic ||
                m_ExitSource != TimelineClipExitSource.TreeDecision)
            {
                return false;
            }
            FixedScalar endTime = FixedScalar.Max(StartTime + FixedScalar.FromRaw(1), terminalTime);
            if (EndTime == endTime)
                return false;
            ConfigureTimeRange(StartTime, endTime);
            return true;
        }

        public override void Init(Track track)
        {
            base.Init(track);
        }

#if UNITY_EDITOR
        public override string Name => $"{ExecutionDomain} / {m_ExecutionPhase} / {(m_AssetTree ? m_AssetTree.name : "Unbound")}";
        public override ClipCapabilities Capabilities =>
            TimelineClipExecutionPolicy.FromDomain(ExecutionDomain).IsLogic
                ? ClipCapabilities.TickQuantized
                : ClipCapabilities.Resizable | ClipCapabilities.TickQuantized;

        public TreeClip(Track track, FixedScalar time) : base(track, time)
        {
        }

        public TreeClip(Track track, FixedScalar time, ScriptableObject assetTree) : base(track, time)
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
                    TimelineExecutionDomain.Logic,
                    TimelineOutputKind.GameplayFact,
                    TimelineExecutionDomainMask.Logic |
                    TimelineExecutionDomainMask.Presentation,
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
                    true,
                    TimelineExecutionDomain.Logic,
                    TimelineOutputKind.GameplayFact,
                    TimelineExecutionDomainMask.Logic,
                    ValidateClip)
            });

        static void ValidateClip(Clip clip, List<string> errors)
        {
            if (clip is not TreeClip treeClip)
            {
                errors?.Add($"Timeline TreeClip '{clip?.AuthoringId}' is invalid.");
                return;
            }
            bool hasGraph = treeClip.AssetTree is ITimelineTreeGraphAsset graph && graph.IsTimelineTree;
            switch (treeClip.ExecutionDomain)
            {
                case TimelineExecutionDomain.Logic:
                    if (!hasGraph)
                        errors?.Add($"Timeline Logic TreeClip '{treeClip.AuthoringId}' requires a TimelineBody graph.");
                    if (treeClip.ClipExitSource != TimelineClipExitSource.TreeDecision)
                        errors?.Add($"Timeline Logic TreeClip '{treeClip.AuthoringId}' must use TreeDecision exit.");
                    break;
                case TimelineExecutionDomain.Presentation:
                    errors?.Add($"Timeline TreeClip '{treeClip.AuthoringId}' cannot execute a TimelineBody graph in Presentation; use a Timeline Marker with a TimelineTrigger graph.");
                    break;
                default:
                    errors?.Add($"Timeline TreeClip '{treeClip.AuthoringId}' has an invalid execution domain.");
                    break;
            }
        }
    }
}
