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
        const string CameraProfilePath = "Assets/Configs/Character/Corin/Pipeline/Presentation/Camera/CorinCharacterCameraProfile.asset";
        const string AttackNormal01ShakePath = "Assets/Configs/Character/Corin/Pipeline/Presentation/Camera/Corin_Attack_Normal_01_CamShake_A_01.asset";
        const string DefaultCurve02Path = "Assets/Configs/Character/Corin/Pipeline/Presentation/Camera/Camera_Default_Curve_02.asset";

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

        [MenuItem("3C/Camera/Validate Corin Camera Profile")]
        public static void ValidateCorinCameraProfile()
        {
            CharacterCameraProfile profile = AssetDatabase.LoadAssetAtPath<CharacterCameraProfile>(CameraProfilePath);
            if (profile == null)
                throw new InvalidOperationException("相机 Profile 缺失: " + CameraProfilePath);
            foreach (CameraShakeAsset shake in profile.Shakes)
            {
                try
                {
                    shake.RequireValid();
                    Debug.Log($"[CameraProfileValidation] Shake OK: {shake.name}");
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"[CameraProfileValidation] Shake invalid: {shake.name}; " +
                        $"schema={shake.Schema}; id={shake.ShakeId}; " +
                        $"fadeIn={(shake.FadeInCurve ? shake.FadeInCurve.name : "<null>")}; " +
                        $"fadeOut={(shake.FadeOutCurve ? shake.FadeOutCurve.name : "<null>")}; " +
                        $"curve={(shake.Curve ? shake.Curve.name : "<null>")}; " +
                        $"space={shake.ShakeCenterSpace}; stacking={shake.PlayStackingType}; " +
                        $"total={shake.ShakeTotalTime}; frequency={shake.Frequency}; reason={exception.Message}");
                }
            }
            try
            {
                profile.RequireValid();
                Debug.Log($"[CameraProfileValidation] Profile OK: {profile.name}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[CameraProfileValidation] Profile invalid: {exception.Message}");
            }
        }

        [MenuItem("3C/Camera/Repair Corin Attack Normal 01 Shake")]
        public static void RepairCorinAttackNormal01Shake()
        {
            CameraShakeAsset shake = AssetDatabase.LoadAssetAtPath<CameraShakeAsset>(AttackNormal01ShakePath);
            CameraCurveAsset curve = AssetDatabase.LoadAssetAtPath<CameraCurveAsset>(DefaultCurve02Path);
            if (shake == null || curve == null)
                throw new InvalidOperationException("Corin Attack Normal 01 Shake 或默认曲线缺失。");
            var serialized = new SerializedObject(shake);
            serialized.FindProperty("m_Schema").stringValue = CameraShakeAsset.SchemaVersion;
            serialized.FindProperty("m_ShakeId").stringValue = "Corin_Attack_Normal_01_CamShake_A_01";
            serialized.FindProperty("m_ShakeType").intValue = 0;
            serialized.FindProperty("m_CameraShakePropertyConfig").intValue = 0;
            serialized.FindProperty("m_AngleVertical").floatValue = 0.6f;
            serialized.FindProperty("m_NoiseAngle").floatValue = 1.2f;
            serialized.FindProperty("m_RadiusLength").floatValue = 0f;
            serialized.FindProperty("m_DistanceToPlane").floatValue = 0f;
            serialized.FindProperty("m_NoiseRatio").floatValue = 0.5f;
            serialized.FindProperty("m_ShakeTotalTime").floatValue = 0.25f;
            serialized.FindProperty("m_Frequency").floatValue = 12f;
            serialized.FindProperty("m_RollAmplitude").floatValue = 0.5f;
            serialized.FindProperty("m_PitchAmplitude").floatValue = 0.4f;
            serialized.FindProperty("m_YawAmplitude").floatValue = 0.6f;
            serialized.FindProperty("m_ShakeCenterSpace").intValue = (int)CameraSpace.LocalAvatar;
            serialized.FindProperty("m_RealtimeVibration").boolValue = true;
            serialized.FindProperty("m_DissipationMode").intValue = 0;
            serialized.FindProperty("m_ImpactRadius").floatValue = 0f;
            serialized.FindProperty("m_DissipationDistance").floatValue = 0f;
            serialized.FindProperty("m_CustomCurveKey").stringValue = string.Empty;
            serialized.FindProperty("m_FadeInDuration").floatValue = 0.02f;
            serialized.FindProperty("m_FadeInCurve").objectReferenceValue = curve;
            serialized.FindProperty("m_FadeOutDuration").floatValue = 0.08f;
            serialized.FindProperty("m_FadeOutCurve").objectReferenceValue = curve;
            serialized.FindProperty("m_Curve").objectReferenceValue = curve;
            serialized.FindProperty("m_IgnoreTimeScale").boolValue = false;
            serialized.FindProperty("m_PlayStackingType").intValue = (int)CameraEffectStackingType.Replace;
            serialized.FindProperty("m_PlayPriority").intValue = 0;
            serialized.FindProperty("m_DataPriority").intValue = 0;
            serialized.FindProperty("m_StandardConfigKey").stringValue = "Corin_Attack_Normal_01_CamShake_A_01";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(shake);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AttackNormal01ShakePath, ImportAssetOptions.ForceUpdate);
            shake.RequireValid();
            Debug.Log("[CameraCurveMigration] 已重新序列化 Corin Attack Normal 01 Shake 资源。");
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
