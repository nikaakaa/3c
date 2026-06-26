using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TreeDesigner.Editor
{
    public class CurrentSelectedTree : ScriptableObject
    {
        public BaseTree Tree;

        public static void CreateSelectedTree()
        {
            CurrentSelectedTree currentSelectedTree = CreateInstance<CurrentSelectedTree>();
            currentSelectedTree.hideFlags = HideFlags.NotEditable;
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/CurrentSelectedTree.asset");
            AssetDatabase.CreateAsset(currentSelectedTree, assetPathAndName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        public static CurrentSelectedTree CreateTreeLocations(string path)
        {
            CurrentSelectedTree currentSelectedTree = CreateInstance<CurrentSelectedTree>();
            currentSelectedTree.hideFlags = HideFlags.NotEditable;
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/CurrentSelectedTree.asset");
            AssetDatabase.CreateAsset(currentSelectedTree, assetPathAndName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return currentSelectedTree;
        }
    }
}