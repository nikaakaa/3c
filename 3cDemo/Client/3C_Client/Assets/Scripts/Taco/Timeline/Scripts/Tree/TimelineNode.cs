using System;
using System.Collections.Generic;
using UnityEngine;
using TreeDesigner;

namespace Taco.Timeline
{
    [Serializable]
    public sealed class TimelineReferenceModule : NodeModule
    {
        [SerializeField, ShowInPanel("Timeline")]
        Timeline m_Timeline;

        public override string DefaultModuleId => "timelineReference";
        public Timeline Timeline => m_Timeline;

        public override IEnumerable<NodeAssetReference> GetAssetReferences()
        {
            yield return new NodeAssetReference(Owner, $"{ModuleId}.m_Timeline", "Timeline", m_Timeline, true);
        }
    }

    [Serializable]
    [NodeName("Timeline")]
    [NodeColor(217, 187, 249)]
    [NodePath("Base/Action/Timeline")]
    [Input("Input")]
    public sealed class TimelineNode : RunnableNode
    {
        [SerializeField]
        string m_InputEdgeGUID;
        public string InputEdgeGUID => m_InputEdgeGUID;

        [SerializeField]
        string m_OutputEdgeGUID;
        public string OutputGUID => m_OutputEdgeGUID;

        [NonSerialized]
        RunnableNode m_Parent;
        public RunnableNode Parent => m_Parent;

        [NonSerialized]
        RunnableNode m_Child;
        public RunnableNode Child => m_Child;

        [NonSerialized]
        Timeline m_RuntimeTimeline;

        [NonSerialized]
        TimelinePlayer m_TimelinePlayer;

        [NonSerialized]
        bool m_TimelineCompleted;

        public Timeline Timeline => GetModule<TimelineReferenceModule>()?.Timeline;

        protected override IEnumerable<NodeModule> CreateDefaultModules()
        {
            yield return new TimelineReferenceModule();
        }

        public override void Init(BaseGraph tree)
        {
            base.Init(tree);
            ResolveFlowLinks();
        }

        public override void Dispose()
        {
            DisposeRuntimeTimeline();
            base.Dispose();
            m_Parent = null;
            m_Child = null;
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_InputEdgeGUID = string.Empty;
            m_OutputEdgeGUID = string.Empty;
            m_Parent = null;
            m_Child = null;
            m_RuntimeTimeline = null;
            m_TimelinePlayer = null;
            m_TimelineCompleted = false;
        }

        protected override void OnStart()
        {
            base.OnStart();
            DisposeRuntimeTimeline();
            m_TimelineCompleted = false;

            Timeline timelineAsset = Timeline;
            if (!timelineAsset)
            {
                m_State = State.Failure;
                Debug.LogError($"TimelineNode missing Timeline asset: {Owner?.name}/{GUID}", Owner);
                return;
            }

            if (!TryGetTimelinePlayer(out m_TimelinePlayer))
            {
                m_State = State.Failure;
                Debug.LogError($"TimelineNode missing ITimelinePlayerProvider context: {Owner?.name}/{GUID}", Owner);
                return;
            }

            m_RuntimeTimeline = UnityEngine.Object.Instantiate(timelineAsset);
            m_RuntimeTimeline.Init();
            m_RuntimeTimeline.Bind(m_TimelinePlayer);
            m_TimelinePlayer.EvaluatePlayableGraph(0);
        }

        protected override State OnUpdate()
        {
            if (!CanRunFromParent())
                return State.None;

            if (!m_TimelineCompleted)
            {
                if (!m_RuntimeTimeline || m_TimelinePlayer == null)
                    return State.Failure;

                if (Owner == null)
                    return State.Failure;

                float deltaTime = Owner.DeltaTime;
                m_RuntimeTimeline.Evaluate(deltaTime);
                m_TimelinePlayer.EvaluatePlayableGraph(deltaTime);

                if (m_RuntimeTimeline.Time < m_RuntimeTimeline.Duration)
                    return State.Running;

                m_TimelineCompleted = true;
                DisposeRuntimeTimeline();
            }

            return m_Child ? m_Child.UpdateNode() : State.Success;
        }

        protected override void OnStop()
        {
            DisposeRuntimeTimeline();
            if (m_Child != null && m_Child.State == State.Running)
                m_Child.StopNode();
        }

        protected override void OnReset()
        {
            base.OnReset();
            m_TimelineCompleted = false;
            DisposeRuntimeTimeline();
            m_Child?.ResetNode();
        }

        bool TryGetTimelinePlayer(out TimelinePlayer timelinePlayer)
        {
            timelinePlayer = null;

            if (Owner != null && Owner.TryGetUser(out ITimelinePlayerProvider provider))
                timelinePlayer = provider.GetTimelinePlayer();

            return timelinePlayer != null && timelinePlayer.IsValid;
        }

        bool CanRunFromParent()
        {
            return m_Parent == null || m_Parent.State == State.Running;
        }

        void ResolveFlowLinks()
        {
            m_InputEdgeGUID = string.Empty;
            m_OutputEdgeGUID = string.Empty;
            m_Parent = null;
            m_Child = null;

            foreach (var inputEdge in m_Owner.GetInputEdges(this, "Input"))
            {
                m_InputEdgeGUID = inputEdge.GUID;
                m_Parent = inputEdge.StartNode as RunnableNode;
                break;
            }

            foreach (var outputEdge in m_Owner.GetOutputEdges(this, "Output"))
            {
                m_OutputEdgeGUID = outputEdge.GUID;
                m_Child = outputEdge.EndNode as RunnableNode;
                break;
            }
        }

        void DisposeRuntimeTimeline()
        {
            if (!m_RuntimeTimeline)
            {
                m_TimelinePlayer = null;
                return;
            }

            if (m_RuntimeTimeline.Binding)
            {
                if (m_TimelinePlayer != null)
                    m_TimelinePlayer.UnbindTimeline(m_RuntimeTimeline);
                else
                    m_RuntimeTimeline.Unbind();
            }

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(m_RuntimeTimeline);
            else
                UnityEngine.Object.DestroyImmediate(m_RuntimeTimeline);

            m_RuntimeTimeline = null;
            m_TimelinePlayer = null;
        }

#if UNITY_EDITOR
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

        public override void OnOutputLinked(BaseEdge edge)
        {
            base.OnOutputLinked(edge);
            m_OutputEdgeGUID = edge.GUID;
            m_Child = edge.EndNode as RunnableNode;
        }

        public override void OnOutputUnlinked(BaseEdge edge)
        {
            base.OnOutputUnlinked(edge);
            m_OutputEdgeGUID = string.Empty;
            m_Child = null;
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

