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

    public interface ITimelineTreeGraphAsset
    {
        string AuthoringId { get; }
        bool IsTimelineTree { get; }
        void CollectTimelineContentClosure(TimelineContentClosureBuilder builder, string sourcePath);
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

        public override Clip AddClip(int frame)
        {
            if (ExecutionDomain == TimelineExecutionDomain.Logic)
                throw new InvalidOperationException("Logic TreeClip必须绑定正式的Timeline节点图资产。");
            return base.AddClip(frame);
        }

        public override Clip AddClip(UnityEngine.Object referenceObject, int frame)
        {
            if (ExecutionDomain == TimelineExecutionDomain.Presentation)
                throw new InvalidOperationException("Presentation TreeClip只能通过Marker创建，不能绑定Timeline节点图资产。");
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
    public partial class TreeClip : Clip, ITimelineOwnedAuthoringIdentity, ITimelineContentClosureSource, ITimelineClipExecutionPhaseSource, ITimelineClipExitSource, ITimelineTerminalFrameAlignedClip, ITimelinePresentationMarkerSource
    {
        public override string ContractKind => TimelineContractKinds.TreeClip;

        [SerializeField, ShowInInspector, OnValueChanged("OnClipChanged", "RepaintInspector")]
        TimelineTreeExecutionPhase m_ExecutionPhase = TimelineTreeExecutionPhase.Commit;

        [SerializeField, ShowInInspector, OnValueChanged("OnClipChanged", "RepaintInspector")]
        TimelineClipExitSource m_ExitSource = TimelineClipExitSource.TreeDecision;

        [SerializeField]
        ScriptableObject m_AssetTree;

        [SerializeField, ShowInInspector, OnValueChanged("OnClipChanged", "RepaintInspector")]
        List<TimelinePresentationMarker> m_PresentationMarkers = new List<TimelinePresentationMarker>();

        public ScriptableObject AssetTree => m_AssetTree;
        public IReadOnlyList<TimelinePresentationMarker> PresentationMarkers => m_PresentationMarkers;

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
            graph.CollectTimelineContentClosure(builder, $"clip:{AuthoringId}/tree:{graph.AuthoringId}");
        }

        public TimelinePresentationMarker AddPresentationMarker(
            int frame,
            TimelinePresentationMarkerLifetime lifetime,
            int endFrame,
            TimelineExternalBindingUse payloadBinding)
        {
            if (!TimelineClipExecutionPolicy.FromDomain(ExecutionDomain).IsPresentation)
                throw new InvalidOperationException("Timeline TreeClip does not have a Presentation projection.");
            TimelinePresentationMarker marker = TimelinePresentationMarker.Create(
                frame,
                lifetime,
                endFrame,
                payloadBinding);
            m_PresentationMarkers.Add(marker);
#if UNITY_EDITOR
            OnClipChanged();
#endif
            return marker;
        }

        public void RemovePresentationMarker(TimelinePresentationMarker marker)
        {
            if (marker == null || !m_PresentationMarkers.Remove(marker))
                throw new ArgumentException("Timeline presentation marker is not owned by this TreeClip.", nameof(marker));
#if UNITY_EDITOR
            OnClipChanged();
#endif
        }

        public bool AlignTerminalFrame(int terminalFrame)
        {
            if (ExecutionDomain != TimelineExecutionDomain.Logic ||
                m_ExitSource != TimelineClipExitSource.TreeDecision)
            {
                return false;
            }
            int endFrame = Mathf.Max(StartFrame + 1, terminalFrame);
            if (EndFrame == endFrame)
                return false;
            EndFrame = endFrame;
            return true;
        }

        public override void Init(Track track)
        {
            base.Init(track);
        }

#if UNITY_EDITOR
        public override string Name => $"{ExecutionDomain} / {m_ExecutionPhase} / {(m_AssetTree ? m_AssetTree.name : "Markers")}";
        public override ClipCapabilities Capabilities =>
            TimelineClipExecutionPolicy.FromDomain(ExecutionDomain).IsLogic
                ? ClipCapabilities.TickQuantized
                : ClipCapabilities.Resizable | ClipCapabilities.TickQuantized;

        public TreeClip(Track track, int frame) : base(track, frame)
        {
        }

        public TreeClip(Track track, int frame, ScriptableObject assetTree) : base(track, frame)
        {
            SetAssetTree(assetTree);
        }

        public void RegenerateOwnedAuthoringIdentity()
        {
            for (int index = 0; index < m_PresentationMarkers.Count; index++)
                m_PresentationMarkers[index]?.RegenerateAuthoringIdentity();
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
                    TimelineExecutionDomainMask.Presentation |
                    TimelineExecutionDomainMask.DualProjection,
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
                    TimelineExecutionDomainMask.Logic |
                    TimelineExecutionDomainMask.Presentation |
                    TimelineExecutionDomainMask.DualProjection,
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
            bool hasMarkers = treeClip.PresentationMarkers != null && treeClip.PresentationMarkers.Count > 0;
            switch (treeClip.ExecutionDomain)
            {
                case TimelineExecutionDomain.Logic:
                    if (!hasGraph)
                        errors?.Add($"Timeline Logic TreeClip '{treeClip.AuthoringId}' requires a TimelineBody graph.");
                    if (hasMarkers)
                        errors?.Add($"Timeline Logic TreeClip '{treeClip.AuthoringId}' cannot contain Presentation Markers.");
                    if (treeClip.ClipExitSource != TimelineClipExitSource.TreeDecision)
                        errors?.Add($"Timeline Logic TreeClip '{treeClip.AuthoringId}' must use TreeDecision exit.");
                    break;
                case TimelineExecutionDomain.Presentation:
                    if (treeClip.AssetTree != null)
                        errors?.Add($"Timeline Presentation TreeClip '{treeClip.AuthoringId}' cannot bind a TimelineBody graph.");
                    if (!hasMarkers)
                        errors?.Add($"Timeline Presentation TreeClip '{treeClip.AuthoringId}' requires Presentation Markers.");
                    if (treeClip.ClipExitSource == TimelineClipExitSource.TreeDecision)
                        errors?.Add($"Timeline Presentation TreeClip '{treeClip.AuthoringId}' cannot use TreeDecision exit.");
                    break;
                case TimelineExecutionDomain.DualProjection:
                    if (!hasGraph)
                        errors?.Add($"Timeline DualProjection TreeClip '{treeClip.AuthoringId}' requires a TimelineBody graph.");
                    if (!hasMarkers)
                        errors?.Add($"Timeline DualProjection TreeClip '{treeClip.AuthoringId}' requires Presentation Markers.");
                    break;
                default:
                    errors?.Add($"Timeline TreeClip '{treeClip.AuthoringId}' has an invalid execution domain.");
                    break;
            }
        }
    }
}

