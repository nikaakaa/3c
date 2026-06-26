#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TreeDesigner
{
    public partial class BaseTree
    {
        [MenuItem("Assets/Create/TreeDesigner/State Machine Graph", false, -1000)]
        public static void CreateStateMachineGraph()
        {
            StateMachineGraph tree = CreateInstance<StateMachineGraph>();
            CreateNode(tree, typeof(StateMachineEnterNode), new Vector2(0, 0));
            CreateNode(tree, typeof(StateMachineAnyStateNode), new Vector2(0, 180));
            CreateNode(tree, typeof(StateMachineExitNode), new Vector2(360, 0));
            CreateGraphAsset(tree, "New State Machine Graph.asset");
        }

        [MenuItem("Assets/Create/TreeDesigner/Transition Rule Graph", false, -999)]
        public static void CreateTransitionRuleGraph()
        {
            TransitionRuleGraph tree = CreateInstance<TransitionRuleGraph>();
            CreateNode(tree, typeof(TransitionRuleResultNode), new Vector2(360, 0));
            CreateGraphAsset(tree, "New Transition Rule Graph.asset");
        }

        static BaseNode CreateNode(BaseTree tree, System.Type type, Vector2 position)
        {
            BaseNode node = tree.CreateNode(type);
            node.Position = position;
            node.Refresh();
            return node;
        }

        static void CreateGraphAsset(BaseTree tree, string fileName)
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + fileName);
            AssetDatabase.CreateAsset(tree, assetPathAndName);
            EditorUtility.SetDirty(tree);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = tree;
        }
    }
}
#endif
