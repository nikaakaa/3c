using System;
using System.Linq;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCamera;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring
{
    internal static class CameraCurveMigrationMenu
    {
        const string DefinitionPath = "Assets/Configs/Character/Corin/Pipeline/Abilities/CorinAttackGameplayAbilityDefinition.asset";

        [MenuItem("3C/Camera/Apply Corin Curve Migration")]
        public static void Apply()
        {
            var definition = AssetDatabase.LoadAssetAtPath<GameplayAbilityDefinition>(DefinitionPath);
            if (definition == null)
                throw new InvalidOperationException("Definition 缺失: " + DefinitionPath);
            if (definition.AbilityGraph == null)
                throw new InvalidOperationException("Definition 缺少 AbilityGraph。");

            var graphs = AssetDatabase.LoadAllAssetsAtPath(DefinitionPath)
                .OfType<BtsmtlSkillFlowGraph>()
                .ToArray();

            int applied = 0;
            applied += EnsureCameraEffect(
                definition,
                FindGraph(graphs, "1eac26e4ad67ccfd6cfe9342d2ea92d7"),
                "Corin_Attack_Normal_01_Shake_Node",
                CameraEffectKind.Shake,
                "Corin_Attack_Normal_01_CamShake_A_01",
                new Vector2(360f, 460f));
            applied += EnsureCameraEffect(
                definition,
                FindGraph(graphs, "47990a9445e2bdec6a74d1f4522332ff"),
                "Corin_Attack_Normal_05_Zoom_Node",
                CameraEffectKind.Zoom,
                "Corin_Attack_Normal_05_CamZoom_01",
                new Vector2(360f, 460f));

            RemoveLegacyCue("Attack1CameraCue");
            RemoveLegacyCue("Attack5CameraCue");

            AssetDatabase.SaveAssets();
            Debug.Log($"[CameraCurveMigration] 相机节点写入 {applied} 处；旧 Attack1/Attack5 CameraCue 已删除。连线由作者在图窗口完成。");
        }

        static BtsmtlSkillFlowGraph FindGraph(BtsmtlSkillFlowGraph[] graphs, string authoringId)
        {
            foreach (BtsmtlSkillFlowGraph graph in graphs)
                if (string.Equals(graph.AuthoringId, authoringId, StringComparison.Ordinal))
                    return graph;
            throw new InvalidOperationException("未找到子图 AuthoringId: " + authoringId);
        }

        static int EnsureCameraEffect(
            GameplayAbilityDefinition definition,
            BtsmtlSkillFlowGraph graph,
            string nodeName,
            CameraEffectKind effectKind,
            string resourceId,
            Vector2 position)
        {
            if (graph == null)
                throw new InvalidOperationException("目标子图缺失: " + nodeName);
            RequestCameraEffectNode existing = graph.allNodes.OfType<RequestCameraEffectNode>()
                .FirstOrDefault(node => string.Equals(node.name, nodeName, StringComparison.Ordinal));
            if (existing != null)
            {
                if (existing.EffectKind == effectKind &&
                    string.Equals(existing.ResourceId, resourceId, StringComparison.Ordinal))
                    return 0;
                BtsmtlSkillAuthoringContract.Apply(existing, new[]
                {
                    new BtsmtlSkillAuthoringFieldValue("effectKind", effectKind),
                    new BtsmtlSkillAuthoringFieldValue("resourceId", resourceId)
                });
                EditorUtility.SetDirty(definition);
                return 1;
            }
            var node = (RequestCameraEffectNode)CodeGeneration.BtsmtlSkillAuthoringCode.EnsureFlowNode(
                graph, typeof(RequestCameraEffectNode), Guid.NewGuid().ToString("D"), nodeName, position);
            BtsmtlSkillAuthoringContract.Apply(node, new[]
            {
                new BtsmtlSkillAuthoringFieldValue("effectKind", effectKind),
                new BtsmtlSkillAuthoringFieldValue("resourceId", resourceId)
            });
            EditorUtility.SetDirty(definition);
            return 1;
        }

        static void RemoveLegacyCue(string cueId)
        {
            var timelines = AssetDatabase.FindAssets("t:TimelineAsset", new[] { "Assets/Configs/Character/Corin" })
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Concat(new[] { DefinitionPath })
                .Distinct(StringComparer.Ordinal);
            foreach (string path in timelines)
            {
                foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is not BTSMTL.Timeline.TimelineAsset timeline || timeline.Data == null)
                        continue;
                foreach (BTSMTL.Timeline.Track track in timeline.Data.Tracks)
                {
                    if (track is not BTSMTL.Timeline.ActionCueTrack cueTrack)
                        continue;
                    for (int i = cueTrack.Clips.Count - 1; i >= 0; i--)
                    {
                        if (cueTrack.Clips[i] is BTSMTL.Timeline.ActionCueClip cue &&
                            string.Equals(cue.CueId, cueId, StringComparison.Ordinal))
                        {
                            cueTrack.Clips.RemoveAt(i);
                            EditorUtility.SetDirty(timeline);
                            Debug.Log($"[CameraCurveMigration] 删除旧 Cue {cueId} @ {path}");
                        }
                    }
                }
            }
            }
        }
    }
}
