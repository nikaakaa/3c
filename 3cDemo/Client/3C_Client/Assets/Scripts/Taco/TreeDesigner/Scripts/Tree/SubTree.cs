using System;
using UnityEngine;

namespace TreeDesigner
{
    [TreeWindow("OpenSubTreeWindow")]
    public partial class SubTree : OneRootTree
    {
        [SerializeField]
        protected string m_EndGUID;
        public string EndGUID { get => m_EndGUID; set => m_EndGUID = value; }

        [NonSerialized]
        protected BaseNode m_End;
        public BaseNode End => m_End;

        public RunnableTree RunnableTree { get; private set; }

        public virtual void Init(BaseTree tree, object user)
        {
            InitTree(user);
            RunnableTree = tree as RunnableTree;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Assets/Create/TreeDesigner/SubTree", false, -998)]
        public static void CreateSubTree()
        {
            SubTree tree = ScriptableObject.CreateInstance<SubTree>();
            tree.RootGUID = tree.CreateNode(typeof(RootNode)).GUID;

            string path = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.Selection.activeObject);
            string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(path + "/New SubTree.asset");
            if (UnityEditor.Selection.activeObject is BaseTree)
                UnityEditor.AssetDatabase.AddObjectToAsset(tree, path);
            else
                UnityEditor.AssetDatabase.CreateAsset(tree, assetPathAndName);

            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            UnityEditor.Selection.activeObject = tree;
        }
#endif
    }
}
