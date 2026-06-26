using System;
using UnityEngine;
using TreeDesigner;

namespace Taco.Timeline 
{
    [AcceptableNodePaths("Timeline")]
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

        public TreeClip Clip { get; private set; }
        public Timeline Timeline => Clip.Timeline;
        public TimelinePlayer TimelinePlayer => Timeline.TimelinePlayer;

        public float Duration => Clip.Duration;

        public override void InitTree(object user)
        {
            base.InitTree(user);
            Clip = user as TreeClip;
            if (!string.IsNullOrEmpty(m_OnEnableGUID))
                m_OnEnable = m_GUIDNodeMap[m_OnEnableGUID] as TimelineEnterNode;
            if (!string.IsNullOrEmpty(m_OnDisableGUID))
                m_OnDisable = m_GUIDNodeMap[m_OnDisableGUID] as TimelineEnterNode;
            if (!string.IsNullOrEmpty(m_OnDestroyGUID))
                m_OnDestroy = m_GUIDNodeMap[m_OnDestroyGUID] as TimelineEnterNode;
        }
        public override void DisposeTree()
        {
            base.DisposeTree();
            m_OnEnable = null;
            m_OnDisable = null;
            m_OnDestroy = null;
            Clip = null;
        }
        public override void OnReset()
        {
            base.OnReset();
            m_OnEnable.ResetNode();
            m_OnDisable.ResetNode();
            m_OnDestroy.ResetNode();
        }
        public override State OnUpdate()
        {
            m_Root.UpdateNode();
            return State.Running;
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

#if UNITY_EDITOR

        public override bool CheckInit()
        {
            bool dirty = base.CheckInit();
            if (!string.IsNullOrEmpty(m_OnEnableGUID))
                m_OnEnable = m_GUIDNodeMap[m_OnEnableGUID] as TimelineEnterNode;
            if (!string.IsNullOrEmpty(m_OnDisableGUID))
                m_OnDisable = m_GUIDNodeMap[m_OnDisableGUID] as TimelineEnterNode;
            if (!string.IsNullOrEmpty(m_OnDestroyGUID))
                m_OnDestroy = m_GUIDNodeMap[m_OnDestroyGUID] as TimelineEnterNode;
            return dirty;
        }

        [UnityEditor.MenuItem("Assets/Create/Taco/Tree/TimelineRunningTree", false, -997)]
        public static void CreateTimelineRunningTree()
        {
            TimelineRunningTree tree = CreateInstance<TimelineRunningTree>();
            tree.RootGUID = tree.CreateNode(typeof(RootNode)).GUID;

            var OnEnable = tree.CreateNode(typeof(TimelineEnterNode)) as TimelineEnterNode;
            OnEnable.EnterType = TimelineEnterNode.NodeEnterType.OnEnable;
            OnEnable.Position = new Vector2(0, 200);
            tree.OnEnableGUID = OnEnable.GUID;

            var OnDisable = tree.CreateNode(typeof(TimelineEnterNode)) as TimelineEnterNode;
            OnDisable.EnterType = TimelineEnterNode.NodeEnterType.OnDisable;
            OnDisable.Position = new Vector2(0, 400);
            tree.OnDisableGUID = OnDisable.GUID;

            var OnDestroy = tree.CreateNode(typeof(TimelineEnterNode)) as TimelineEnterNode;
            OnDestroy.EnterType = TimelineEnterNode.NodeEnterType.OnDestroy;
            OnDestroy.Position = new Vector2(0, 600);
            tree.OnDestroyGUID = OnDestroy.GUID;

            string path = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.Selection.activeObject);
            string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(path + "/New TimelineRunningTree.asset");
            UnityEditor.AssetDatabase.CreateAsset(tree, assetPathAndName);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            UnityEditor.Selection.activeObject = tree;
        }
#endif
    }
}
