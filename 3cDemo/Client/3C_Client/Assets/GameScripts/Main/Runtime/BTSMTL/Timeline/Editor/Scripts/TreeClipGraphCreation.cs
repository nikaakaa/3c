using System;
using ThirdPersonCharacter.Control.Authoring;
using UnityEditor;
using UnityEngine;

namespace BTSMTL.Timeline.Editor
{
    internal static class TreeClipGraphCreation
    {
        public static BtsmtlSkillFlowGraph CreateSubAsset(UnityEngine.Object container, string graphName)
        {
            if (container == null)
                throw new InvalidOperationException("Timeline 缺少序列化资产容器。");
            string path = AssetDatabase.GetAssetPath(container);
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("Timeline 容器不是持久资产，无法创建图子资产。");
            var graph = ScriptableObject.CreateInstance<BtsmtlSkillFlowGraph>();
            graph.name = string.IsNullOrWhiteSpace(graphName) ? "Timeline Tree" : graphName.Trim();
            graph.ConfigureIdentity(Guid.NewGuid().ToString("N"), BtsmtlSkillFlowGraphRole.TimelineBody);
            BtsmtlSkillGraphAssetFactory.PopulateAnchors(graph);
            AssetDatabase.AddObjectToAsset(graph, path);
            Undo.RegisterCreatedObjectUndo(graph, "创建TreeClip节点图");
            EditorUtility.SetDirty(container);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return graph;
        }
    }
}