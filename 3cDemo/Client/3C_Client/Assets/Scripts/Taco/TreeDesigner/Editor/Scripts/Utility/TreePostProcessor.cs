using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TreeDesigner.Editor
{
    public class TreePostProcessor : AssetPostprocessor
    {
        //static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        //{
        //    foreach (var importedAsset in importedAssets)
        //    {
        //        if(importedAsset == AssetDatabase.GetAssetPath(TreeModificationProcessor.TreeLocations))
        //        {
        //            EditorApplication.delayCall += () => TreeModificationProcessor.FindLocations("Assets");
        //            return;
        //        }
        //    }
        //}
    }
}