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
            StateMachineGraph tree = new StateMachineGraph();
            CreateNode(tree, typeof(StateMachineEnterNode), new Vector2(0, 0));
            CreateNode(tree, typeof(StateMachineAnyStateNode), new Vector2(0, 180));
            CreateNode(tree, typeof(StateMachineExitNode), new Vector2(360, 0));
            CreateGraphAsset(tree, "New State Machine Graph.asset");
        }

        [MenuItem("Assets/Create/TreeDesigner/Condition Rule Graph", false, -999)]
        public static void CreateConditionRuleGraph()
        {
            ConditionRuleGraph tree = ConditionRuleGraph.CreateDefaultGraph("Condition Rule");
            CreateGraphAsset(tree, "New Condition Rule Graph.asset");
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
            BaseTreeAsset asset = ScriptableObject.CreateInstance<BaseTreeAsset>();
            asset.SetTree(tree);
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + fileName);
            AssetDatabase.CreateAsset(asset, assetPathAndName);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = asset;
        }
    }
}
#endif
