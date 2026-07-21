using System;
using System.Collections.Generic;
using UnityEngine;
using TreeDesigner;

namespace BTSMTL.Timeline
{
    public enum TimelineOwnership
    {
        Missing,
        Inline,
        Shared
    }

    [Serializable]
    public sealed class TimelineOwnershipModule : NodeModule
    {
        [SerializeReference]
        TimelineData m_InlineTimeline;

        [SerializeField]
        TimelineAsset m_SharedTimelineAsset;

        public override string DefaultModuleId => "timelineOwnership";
        public TimelineData InlineTimeline => m_InlineTimeline;
        public TimelineAsset SharedTimelineAsset => m_SharedTimelineAsset;
        public TimelineData ResolvedTimeline => m_SharedTimelineAsset ? m_SharedTimelineAsset.Data : m_InlineTimeline;
        public TimelineOwnership Ownership => m_SharedTimelineAsset ? TimelineOwnership.Shared : m_InlineTimeline != null ? TimelineOwnership.Inline : TimelineOwnership.Missing;

        public void SetInlineTimeline(TimelineData timeline)
        {
            m_InlineTimeline = timeline;
            m_SharedTimelineAsset = null;
            BindInlineTimeline();
        }

        public void SetSharedTimelineAsset(TimelineAsset timelineAsset)
        {
            m_SharedTimelineAsset = timelineAsset;
            if (m_SharedTimelineAsset)
                m_InlineTimeline = null;
        }

#if UNITY_EDITOR
        public void UseInline()
        {
            TimelineData source = ResolvedTimeline;
            SetInlineTimeline(source != null ? source.CloneForAuthoring() : TimelineData.CreateDefault(Owner?.DisplayName));
        }
#endif

        public override void Init(BaseNode owner, string defaultModuleId)
        {
            base.Init(owner, defaultModuleId);
            if (m_InlineTimeline == null && !m_SharedTimelineAsset)
                m_InlineTimeline = TimelineData.CreateDefault(owner?.DisplayName);
            BindInlineTimeline();
        }

        public override void RebindReadOnlyViewOwner(BaseNode owner)
        {
            base.RebindReadOnlyViewOwner(owner);
            BindInlineTimelineOwner();
        }

        public override IEnumerable<NodeAssetReference> GetAssetReferences()
        {
            if (m_SharedTimelineAsset)
                yield return new NodeAssetReference(Owner, $"{ModuleId}.m_SharedTimelineAsset", "Shared Timeline", m_SharedTimelineAsset, false);
        }

        void BindInlineTimeline()
        {
            if (!BindInlineTimelineOwner())
                return;

            m_InlineTimeline.Init();
        }

        bool BindInlineTimelineOwner()
        {
            if (m_InlineTimeline == null || Owner?.Owner == null)
                return false;

            IReadOnlyList<NodeModule> modules = Owner.Modules;
            int moduleIndex = -1;
            for (int i = 0; i < modules.Count; i++)
            {
                if (ReferenceEquals(modules[i], this))
                {
                    moduleIndex = i;
                    break;
                }
            }
            if (moduleIndex < 0)
                return false;

            string nodePath = Owner.Owner.GetNodeSerializedPropertyPath(Owner);
            m_InlineTimeline.BindSerializedOwner(Owner.Owner.SerializedOwner, $"{nodePath}.m_Modules.Array.data[{moduleIndex}].m_InlineTimeline");
            return true;
        }
    }

    [Serializable]
    [NodeName("Timeline")]
    [NodeColor(217, 187, 249)]
    [NodePath("Base/Action/Timeline")]
    [NodeAuthoringCapability(NodeAuthoringCapability.TimelineDecision)]
    [Input("Input")]
    public sealed class TimelineNode : RunnableNode
    {
        [SerializeField]
        string m_InputEdgeGUID;
        public string InputEdgeGUID => m_InputEdgeGUID;

        [NonSerialized]
        RunnableNode m_Parent;
        public RunnableNode Parent => m_Parent;

        [NonSerialized]
        ITimelinePlaybackService m_PlaybackService;

        [SerializeField, ShowInPanel("Action Context")]
        ActionContextSlot m_ActionContext;

        [SerializeField, ShowInPanel("Playback Mode")]
        TimelinePlaybackMode m_PlaybackMode = TimelinePlaybackMode.Once;

        [NonSerialized]
        TimelinePlaybackHandle m_PlaybackHandle;

        [NonSerialized]
        bool m_PlaybackCompleted;

        [NonSerialized]
        TimelinePlaybackStatus m_LastPlaybackStatus;

#if UNITY_EDITOR
        public override void RegenerateOwnedAuthoringIdentities()
        {
            base.RegenerateOwnedAuthoringIdentities();
            TimelineOwnershipModule ownership = GetModule<TimelineOwnershipModule>();
            if (ownership?.InlineTimeline != null)
                ownership.SetInlineTimeline(ownership.InlineTimeline.CloneForAuthoring());
        }
#endif

        public TimelineData Timeline => GetModule<TimelineOwnershipModule>()?.ResolvedTimeline;
        public TimelineData InlineTimeline => GetModule<TimelineOwnershipModule>()?.InlineTimeline;
        public TimelineAsset SharedTimelineAsset => GetModule<TimelineOwnershipModule>()?.SharedTimelineAsset;
        public TimelineOwnership TimelineOwnership => GetModule<TimelineOwnershipModule>()?.Ownership ?? TimelineOwnership.Missing;
        public ActionContextSlot ActionContext => m_ActionContext;
        public TimelinePlaybackMode PlaybackMode => m_PlaybackMode;
        [TreeDesigner.ShowInInspector("Timeline Source")]
        public string TimelineSourceDebug => TimelineOwnership == TimelineOwnership.Shared
            ? $"Shared:{(SharedTimelineAsset ? SharedTimelineAsset.name : "Missing")}"
            : TimelineOwnership == TimelineOwnership.Inline
                ? $"Inline:{Timeline?.Name ?? "Missing"}"
                : "Missing";
        [TreeDesigner.ShowInInspector("Playback Status")]
        public TimelinePlaybackStatus LastPlaybackStatus => m_LastPlaybackStatus;

#if UNITY_EDITOR
        public void ConfigureAuthoring(TimelineData timeline, ActionContextSlot actionContext, TimelinePlaybackMode playbackMode = TimelinePlaybackMode.Once)
        {
            GetModule<TimelineOwnershipModule>()?.SetInlineTimeline(timeline ?? TimelineData.CreateDefault(DisplayName));
            m_ActionContext = actionContext;
            m_PlaybackMode = playbackMode;
            OnNodeChangedCallback();
        }

        public void ConfigureSharedAuthoring(TimelineAsset timelineAsset, ActionContextSlot actionContext, TimelinePlaybackMode playbackMode = TimelinePlaybackMode.Once)
        {
            GetModule<TimelineOwnershipModule>()?.SetSharedTimelineAsset(timelineAsset);
            m_ActionContext = actionContext;
            m_PlaybackMode = playbackMode;
            OnNodeChangedCallback();
        }
#endif

        protected override IEnumerable<NodeModule> CreateDefaultModules()
        {
            yield return new TimelineOwnershipModule();
        }

        public override void Init(BaseGraph tree)
        {
            base.Init(tree);
            ResolveFlowLinks();
        }

        public override void Dispose()
        {
            CancelActivePlayback(NodeStopContext.Create(NodeStopOriginCause.Shutdown, ResolveLocalLogicTick(), this));
            base.Dispose();
            m_Parent = null;
            m_PlaybackService = null;
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_InputEdgeGUID = string.Empty;
            m_Parent = null;
            m_PlaybackService = null;
            m_PlaybackHandle = TimelinePlaybackHandle.Invalid;
            m_PlaybackCompleted = false;
            m_LastPlaybackStatus = TimelinePlaybackStatus.None;
        }

        protected override void OnStart()
        {
            base.OnStart();
            m_PlaybackCompleted = false;
            m_LastPlaybackStatus = TimelinePlaybackStatus.Requested;
            m_PlaybackHandle = TimelinePlaybackHandle.Invalid;
            m_PlaybackService = null;

            TimelineData timeline = Timeline;
            if (timeline == null)
            {
                m_State = State.Failure;
                Debug.LogError($"TimelineNode missing resolved TimelineData: {Owner?.name}/{GUID}");
                return;
            }

            if (Owner == null || !Owner.TryGetUser(out m_PlaybackService) || m_PlaybackService == null)
            {
                m_State = State.Failure;
                Debug.LogError($"TimelineNode missing ITimelinePlaybackService context: {Owner?.name}/{GUID}");
                return;
            }

            if (!TryResolveActionContext(out TimelinePlaybackActionContext actionContext))
            {
                m_State = State.Failure;
                Debug.LogError($"TimelineNode failed to resolve action context: {Owner?.name}/{GUID}");
                return;
            }

            if (!m_PlaybackService.RequestTimelinePlayback(
                    timeline,
                    GUID,
                    Owner != null ? Owner.name : string.Empty,
                    actionContext,
                    m_PlaybackMode,
                    ActivationScope,
                    Owner,
                    out m_PlaybackHandle))
            {
                m_State = State.Failure;
                Debug.LogError($"TimelineNode failed to request Timeline playback: {Owner?.name}/{GUID}");
                return;
            }
        }

        protected override State OnUpdate()
        {
            if (!CanRunFromParent())
                return State.None;

            if (!m_PlaybackCompleted)
            {
                if (m_PlaybackService == null || !m_PlaybackHandle.IsValid)
                    return State.Failure;

                TimelinePlaybackStatus status = m_PlaybackService.GetTimelinePlaybackStatus(m_PlaybackHandle);
                m_LastPlaybackStatus = status;
                switch (status)
                {
                    case TimelinePlaybackStatus.Requested:
                    case TimelinePlaybackStatus.Running:
                        return State.Running;
                    case TimelinePlaybackStatus.Succeeded:
                        m_PlaybackCompleted = true;
                        m_PlaybackHandle = TimelinePlaybackHandle.Invalid;
                        m_PlaybackService = null;
                        break;
                    case TimelinePlaybackStatus.Failed:
                    case TimelinePlaybackStatus.Cancelled:
                    case TimelinePlaybackStatus.None:
                    default:
                        m_PlaybackHandle = TimelinePlaybackHandle.Invalid;
                        m_PlaybackService = null;
                        return State.Failure;
                }
            }

            return State.Success;
        }

        protected override NodeStopStatus OnStopRequested(NodeStopContext context)
        {
            CancelActivePlayback(context);
            return NodeStopStatus.Completed;
        }

        protected override void OnForceStopped(NodeStopContext context)
        {
            CancelActivePlayback(context);
        }

        protected override void OnReset()
        {
            base.OnReset();
            m_PlaybackCompleted = false;
            m_LastPlaybackStatus = TimelinePlaybackStatus.None;
        }

        bool CanRunFromParent()
        {
            return m_Parent == null || m_Parent.State == State.Running;
        }

        void ResolveFlowLinks()
        {
            m_InputEdgeGUID = string.Empty;
            m_Parent = null;

            foreach (var inputEdge in m_Owner.GetInputEdges(this, "Input"))
            {
                m_InputEdgeGUID = inputEdge.GUID;
                m_Parent = inputEdge.StartNode as RunnableNode;
                break;
            }
        }

        void CancelActivePlayback(NodeStopContext stopContext)
        {
            if (m_PlaybackService != null && m_PlaybackHandle.IsValid)
            {
                TimelinePlaybackStatus status = m_PlaybackService.GetTimelinePlaybackStatus(m_PlaybackHandle);
                if (status == TimelinePlaybackStatus.Requested || status == TimelinePlaybackStatus.Running)
                {
                    m_PlaybackService.CancelTimelinePlayback(m_PlaybackHandle, ToTimelineStopContext(stopContext));
                    m_LastPlaybackStatus = TimelinePlaybackStatus.Cancelled;
                }
            }

            m_PlaybackHandle = TimelinePlaybackHandle.Invalid;
            m_PlaybackService = null;
        }

        ulong ResolveLocalLogicTick()
        {
            return Owner?.User is INodeStopTickSource tickSource
                ? tickSource.NodeStopLocalLogicTick
                : 0;
        }

        static TimelinePlaybackStopContext ToTimelineStopContext(NodeStopContext context)
        {
            return new TimelinePlaybackStopContext((TimelinePlaybackStopCause)context.OriginCause, context.LocalLogicTick);
        }

        bool TryResolveActionContext(out TimelinePlaybackActionContext actionContext)
        {
            actionContext = default;
            if (!m_ActionContext)
                return true;

            if (m_PlaybackService is ITimelinePlaybackActionContextSource contextSource &&
                contextSource.TryGetTimelinePlaybackActionContext(m_ActionContext, out actionContext))
                return true;

            actionContext = default;
            return false;
        }

#if UNITY_EDITOR
        public override IEnumerable<NodeAssetReference> GetAssetReferences()
        {
            foreach (var reference in base.GetAssetReferences())
                yield return reference;

            yield return new NodeAssetReference(this, "m_ActionContext", "Action Context", m_ActionContext, false);
        }

        public override void OnInputLinked(BaseEdge edge)
        {
            base.OnInputLinked(edge);
            m_InputEdgeGUID = edge.GUID;
            m_Parent = edge.StartNode as RunnableNode;
        }

        public override void OnInputUnlinked(BaseEdge edge)
        {
            base.OnInputUnlinked(edge);
            m_InputEdgeGUID = string.Empty;
            m_Parent = null;
        }

        public override void OnMoved()
        {
            base.OnMoved();
            if (m_Parent is CompositeNode compositeNode)
                compositeNode.OrderChildren();
        }
#endif
    }
}
