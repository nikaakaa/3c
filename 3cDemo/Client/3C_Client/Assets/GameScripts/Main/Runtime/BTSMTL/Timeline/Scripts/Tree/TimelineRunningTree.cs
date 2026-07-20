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

        protected override void ValidateInitializationContext(
            object user,
            BaseGraph parentRuntimeGraph,
            TreeAuthoringRouteId authoringRoute)
        {
            throw new InvalidOperationException(
                "TimelineRunningTree is authoring-only and must execute through a compiled Character Simulation Program.");
        }

#if UNITY_EDITOR
        public override bool CheckInit()
        {
            return base.CheckInit();
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
