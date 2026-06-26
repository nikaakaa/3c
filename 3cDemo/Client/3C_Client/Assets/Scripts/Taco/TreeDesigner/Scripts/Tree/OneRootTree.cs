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

        public override void InitTree(object user)
        {
            base.InitTree(user);
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
        public override void OnStop()
        {
            m_Running = false;
            OnStopCallback?.Invoke();
        }
        public override void OnReset()
        {
            m_Root.ResetNode();
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
            OneRootTree tree = CreateInstance<OneRootTree>();
            tree.RootGUID = tree.CreateNode(typeof(RootNode)).GUID;

            string path = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.Selection.activeObject);
            string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(path + "/New OneRootTree.asset");
            UnityEditor.AssetDatabase.CreateAsset(tree, assetPathAndName);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            UnityEditor.Selection.activeObject = tree;
        }
#endif
    }
}
