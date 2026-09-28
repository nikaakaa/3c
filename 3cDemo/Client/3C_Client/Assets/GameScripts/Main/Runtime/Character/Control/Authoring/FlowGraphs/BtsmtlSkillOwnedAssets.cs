#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using ThirdPersonCharacter.ActionSystem;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public static class BtsmtlSkillOwnedAssets
    {
        public static HashSet<UnityEngine.Object> SnapshotPrivateSubAssets(UnityEngine.Object owner)
        {
            string path = AssetDatabase.GetAssetPath(owner);
            var result = new HashSet<UnityEngine.Object>();
            if (string.IsNullOrEmpty(path))
                return result;
            UnityEngine.Object root = AssetDatabase.LoadMainAssetAtPath(path);
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset != root && (asset is FlowGraph && asset is IBtsmtlSkillFlowGraph ||
                                      asset is BtsmtlSkillNativeStateMachine || asset is TimelineAsset))
                    result.Add(asset);
            return result;
        }

        public static HashSet<UnityEngine.Object> Collect(UnityEngine.Object owner)
        {
            string path = AssetDatabase.GetAssetPath(owner);
            var result = new HashSet<UnityEngine.Object>();
            if (string.IsNullOrEmpty(path))
                return result;
            UnityEngine.Object root = AssetDatabase.LoadMainAssetAtPath(path);
            var visited = new HashSet<UnityEngine.Object>();
            Visit(root);
            return result;

            void Visit(UnityEngine.Object asset)
            {
                if (!asset || !visited.Add(asset))
                    return;
                if (AssetDatabase.GetAssetPath(asset) != path)
                    return;
                if (asset != root)
                    result.Add(asset);
                if (asset is GameplayAbilityDefinition ability)
                {
                    Visit(ability.AbilityGraph);
                }
                else if (asset is FlowGraph graph && graph is IBtsmtlSkillFlowGraph)
                {
                    foreach (FlowNode node in graph.allNodes)
                    {
                        if (node is MacroNodeWrapper macro)
                            Visit(macro.macro);
                        if (node is BtsmtlSkillStateMachineFlowNode machineNode)
                            Visit(machineNode.StateMachine);
                        if (node is BtsmtlSkillStateFlowNode state)
                            Visit(state.Body);
                        if (node is BtsmtlSkillTimelineFlowNode timelineNode)
                            Visit(timelineNode.TimelineAsset);
                        if (node is BtsmtlSkillCompositeFlowNode composite)
                            foreach (BtsmtlSkillStepPort step in composite.Steps)
                                Visit(step.Condition);
                        foreach (var connection in node.outConnections)
                            if (connection is BtsmtlSkillFlowConnection transfer)
                                Visit(transfer.Condition);
                    }
                }
                else if (asset is BtsmtlSkillNativeStateMachine machine)
                {
                    foreach (BtsmtlSkillFlowGraph child in BtsmtlSkillNativeStateMachineContract.References(machine))
                        Visit(child);
                }
                else if (asset is TimelineAsset timeline)
                {
                    foreach (TreeClip clip in timeline.Data.Tracks.SelectMany(track => track.Clips).OfType<TreeClip>())
                        Visit(clip.AssetTree);
                    foreach (TimelineMarker marker in timeline.Data.Tracks.SelectMany(track => track.Markers))
                        Visit(marker.Graph);
                }
                else
                    throw new InvalidOperationException("私有技能内容必须由正式技能图或Timeline资产拥有。");
            }
        }

        public static void ReleaseOrphaned(UnityEngine.Object owner)
        {
            ReleaseUnreferenced(owner, SnapshotPrivateSubAssets(owner));
        }

        public static void ReleaseUnreferenced(UnityEngine.Object owner, HashSet<UnityEngine.Object> previous)
        {
            previous.ExceptWith(Collect(owner));
            previous.RemoveWhere(asset => !asset);
            foreach (UnityEngine.Object asset in previous)
                Undo.DestroyObjectImmediate(asset);
        }
    }
}
#endif
