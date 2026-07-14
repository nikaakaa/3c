using System;
using UnityEngine;

namespace TreeDesigner
{
    public class OneRootTree : RunnableTree
    {
        [SerializeField]
        protected string m_RootGUID;
        public string RootGUID { get => m_RootGUID; set => m_RootGUID = value; }

        [NonSerialized]
        protected RootNode m_Root;

        protected override void OnTreeInitialized()
        {
            base.OnTreeInitialized();
            if (!string.IsNullOrEmpty(m_RootGUID))
                m_Root = m_GUIDNodeMap[m_RootGUID] as RootNode;
        }
        public override void DisposeTree()
        {
            base.DisposeTree();
            m_Root = null;
        }

        public override void OnStart()
        {
            m_Running = true;
            m_State = State.Running;
        }
        public override State OnUpdate()
        {
            return m_Root.UpdateNode();
        }
        protected override NodeStopStatus OnStopRequested(NodeStopContext context)
        {
            return m_Root != null ? m_Root.RequestStop(context.Propagate(null, null, m_Root)) : NodeStopStatus.Completed;
        }
        protected override NodeStopStatus OnStopping(NodeStopContext context)
        {
            return m_Root != null ? m_Root.UpdateStopping() : NodeStopStatus.Completed;
        }
        protected override void OnForceStopped(NodeStopContext context)
        {
            m_Root?.ForceStop(context.Propagate(null, null, m_Root));
        }
        public override void OnReset()
        {
            m_Root?.ResetNode();
        }

#if UNITY_EDITOR

        public override bool CheckInit()
        {
            bool dirty = base.CheckInit();
            if (!string.IsNullOrEmpty(m_RootGUID))
                m_Root = m_GUIDNodeMap[m_RootGUID] as RootNode;
            return dirty;
        }

        [UnityEditor.MenuItem("Assets/Create/TreeDesigner/OneRootTree", false, -999)]
        public static void CreateOneRootTree()
        {
            OneRootTree tree = new OneRootTree();
            tree.RootGUID = tree.CreateNode(typeof(RootNode)).GUID;

            BaseTreeAsset asset = ScriptableObject.CreateInstance<BaseTreeAsset>();
            asset.SetTree(tree);
            string path = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.Selection.activeObject);
            string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(path + "/New OneRootTree.asset");
            UnityEditor.AssetDatabase.CreateAsset(asset, assetPathAndName);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            UnityEditor.Selection.activeObject = asset;
        }
#endif
    }
}
