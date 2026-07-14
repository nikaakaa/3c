using System;
using UnityEngine;
using TreeDesigner;

namespace BTSMTL.Timeline
{
    [AcceptableNodePaths("Base", "Timeline")]
    public partial class TimelineRunningTree : OneRootTree
    {
        [SerializeField]
        protected string m_OnEnableGUID;
        public string OnEnableGUID { get => m_OnEnableGUID; set => m_OnEnableGUID = value; }

        [SerializeField]
        protected string m_OnDisableGUID;
        public string OnDisableGUID { get => m_OnDisableGUID; set => m_OnDisableGUID = value; }

        [SerializeField]
        protected string m_OnDestroyGUID;
        public string OnDestroyGUID { get => m_OnDestroyGUID; set => m_OnDestroyGUID = value; }

        [NonSerialized]
        protected TimelineEnterNode m_OnEnable;

        [NonSerialized]
        protected TimelineEnterNode m_OnDisable;

        [NonSerialized]
        protected TimelineEnterNode m_OnDestroy;

        public TimelineTreeClipRuntimeContext ClipContext { get; private set; }
        public TreeClip Clip => ClipContext?.Clip;
        public TimelineData Timeline => ClipContext?.Timeline;
        public float Duration => Clip != null ? Clip.DurationTime : 0f;

        public void InitTimelineTree(object graphUser, TimelineTreeClipRuntimeContext clipContext)
        {
            if (graphUser == null)
                throw new ArgumentNullException(nameof(graphUser));
            if (clipContext == null)
                throw new ArgumentNullException(nameof(clipContext));
            if (!clipContext.SourceActivation.IsValid || clipContext.SourceRuntimeGraph == null)
                throw new InvalidOperationException("TimelineRunningTree requires a valid source Runnable activation and runtime Graph.");
            if (clipContext.SourceRuntimeGraph.RuntimeId != clipContext.SourceActivation.ActivationId.GraphRuntimeId)
                throw new InvalidOperationException("TimelineRunningTree source activation does not belong to the source runtime Graph.");

            Track track = clipContext.Clip?.Track;
            TimelineData timeline = track?.Timeline;
            if (timeline == null || track == null || clipContext.Clip == null)
                throw new InvalidOperationException("TimelineRunningTree requires complete Timeline, Track, and TreeClip authoring identities.");

            TreeGraphReferenceOwnership ownership = clipContext.Clip.Ownership == TimelineTreeOwnership.Inline
                ? TreeGraphReferenceOwnership.Inline
                : clipContext.Clip.Ownership == TimelineTreeOwnership.Shared
                    ? TreeGraphReferenceOwnership.Shared
                    : throw new InvalidOperationException("TimelineRunningTree cannot initialize a missing TreeClip graph reference.");
            TreeAuthoringRouteId route = clipContext.SourceActivation.AuthoringRoute.Append(
                TreeAuthoringRouteSegment.TimelineTreeClip(
                    clipContext.SourceActivation.Source,
                    "timeline.treeClip",
                    clipContext.Clip.AuthoringId,
                    GraphAuthoringId,
                    ownership,
                    timeline.AuthoringId,
                    track.AuthoringId,
                    clipContext.Clip.AuthoringId));

            ClipContext = clipContext;
            base.InitTree(graphUser, clipContext.SourceRuntimeGraph, route);
        }

        protected override void ValidateInitializationContext(
            object user,
            BaseGraph parentRuntimeGraph,
            TreeAuthoringRouteId authoringRoute)
        {
            base.ValidateInitializationContext(user, parentRuntimeGraph, authoringRoute);
            if (ClipContext == null)
                throw new InvalidOperationException("TimelineRunningTree requires InitTimelineTree with a formal clip runtime context.");
        }

        protected override void OnTreeInitialized()
        {
            base.OnTreeInitialized();
            ResolveLifecycleNodes();
        }

        public override void DisposeTree()
        {
            base.DisposeTree();
            m_OnEnable = null;
            m_OnDisable = null;
            m_OnDestroy = null;
            ClipContext = null;
        }

        public override void OnReset()
        {
            base.OnReset();
            m_OnEnable?.ResetNode();
            m_OnDisable?.ResetNode();
            m_OnDestroy?.ResetNode();
        }

        public override State OnUpdate()
        {
            State result = m_Root != null ? m_Root.UpdateNode() : State.Failure;
            return Clip?.ExecutionPhase == TimelineTreeExecutionPhase.Decision
                ? result
                : State.Running;
        }

        public void OnTreeEnable()
        {
            m_OnEnable?.UpdateNode();
        }

        public void OnTreeDisable()
        {
            m_OnDisable?.UpdateNode();
        }

        public void OnTreeDestroy()
        {
            m_OnDestroy?.UpdateNode();
        }

        void ResolveLifecycleNodes()
        {
            m_OnEnable = ResolveTimelineEnter(m_OnEnableGUID);
            m_OnDisable = ResolveTimelineEnter(m_OnDisableGUID);
            m_OnDestroy = ResolveTimelineEnter(m_OnDestroyGUID);
        }

        TimelineEnterNode ResolveTimelineEnter(string guid)
        {
            return !string.IsNullOrEmpty(guid) && m_GUIDNodeMap.TryGetValue(guid, out BaseNode node)
                ? node as TimelineEnterNode
                : null;
        }

#if UNITY_EDITOR
        public override bool CheckInit()
        {
            bool dirty = base.CheckInit();
            ResolveLifecycleNodes();
            return dirty;
        }

        public static TimelineRunningTree CreateDefault(string treeName)
        {
            var tree = new TimelineRunningTree { name = treeName };
            tree.RootGUID = tree.CreateNode(typeof(RootNode)).GUID;

            var onEnable = tree.CreateNode(typeof(TimelineEnterNode)) as TimelineEnterNode;
            onEnable.EnterType = TimelineEnterNode.NodeEnterType.OnEnable;
            onEnable.Position = new Vector2(0f, 200f);
            tree.OnEnableGUID = onEnable.GUID;

            var onDisable = tree.CreateNode(typeof(TimelineEnterNode)) as TimelineEnterNode;
            onDisable.EnterType = TimelineEnterNode.NodeEnterType.OnDisable;
            onDisable.Position = new Vector2(0f, 400f);
            tree.OnDisableGUID = onDisable.GUID;

            var onDestroy = tree.CreateNode(typeof(TimelineEnterNode)) as TimelineEnterNode;
            onDestroy.EnterType = TimelineEnterNode.NodeEnterType.OnDestroy;
            onDestroy.Position = new Vector2(0f, 600f);
            tree.OnDestroyGUID = onDestroy.GUID;
            return tree;
        }
#endif
    }
}
