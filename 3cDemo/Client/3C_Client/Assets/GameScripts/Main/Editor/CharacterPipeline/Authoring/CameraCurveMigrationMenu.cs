using System;
using System.Linq;
using ThirdPersonCharacter.ActionSystem;
using FlowCanvas;
using NodeCanvas.Framework;
using ThirdPersonSimulation;
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
                new Vector2(360f, 460f),
                "b55d75dd-5b56-4cd3-90aa-95ff0ad4dd22",
                "0e21a562-15a3-4d02-960f-1f5fa94ef22f",
                "1b6fa94a-33d5-4fbd-96af-4f52f1c1f421",
                "e7c927bb-0706-4106-8d7e-d752fc9df427",
                "d4c2aeb7-5842-448e-9c7f-16558d9dfc53");
            applied += EnsureCameraEffect(
                definition,
                FindGraph(graphs, "47990a9445e2bdec6a74d1f4522332ff"),
                "Corin_Attack_Normal_05_Zoom_Node",
                CameraEffectKind.Zoom,
                "Corin_Attack_Normal_05_CamZoom_01",
                new Vector2(360f, 460f),
                "baa09250-708d-4044-a90e-3e4e018aa926",
                "2248ac76-d9ec-4bd9-a772-a76a09a26f6a",
                "80b7ff9e-2214-4b83-8f68-3198ac36a4c1",
                "863fd5a3-0f36-4ee2-9560-1e6f4f549d90",
                "98df6307-0b02-4348-bdf7-f3b98eb63a7f");

            AssetDatabase.SaveAssets();
            Debug.Log($"[CameraCurveMigration] 相机节点写入 {applied} 处。连线由作者在图窗口完成。");
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
            Vector2 position,
            string nodeAuthoringId,
            string branchAuthoringId,
            string rootEdgeAuthoringId,
            string cameraEdgeAuthoringId,
            string hitEdgeAuthoringId)
        {
            if (graph == null)
                throw new InvalidOperationException("目标子图缺失: " + nodeName);
            RequestCameraEffectNode node = graph.allNodes.OfType<RequestCameraEffectNode>()
                .FirstOrDefault(value => string.Equals(value.name, nodeName, StringComparison.Ordinal));
            if (node != null && !string.Equals(node.UID, nodeAuthoringId, StringComparison.Ordinal))
            {
                graph.RemoveNode(node, false);
                node = null;
            }
            if (node == null)
                node = (RequestCameraEffectNode)CodeGeneration.BtsmtlSkillAuthoringCode.EnsureFlowNode(
                    graph, typeof(RequestCameraEffectNode), nodeAuthoringId, nodeName, position);
            var trigger = graph.allNodes.OfType<BtsmtlSkillRootFlowNode>().Single();
            var originalTargets = trigger.outConnections.OfType<BinderConnection>()
                .Where(value => value.sourcePortID == "Output")
                .Select(value => value.targetNode)
                .OfType<FlowNode>()
                .Where(value => value.UID != branchAuthoringId && value.UID != nodeAuthoringId)
                .ToArray();
            graph.DisconnectPort(trigger.GetOutputPort("Output"));
            var branch = graph.allNodes.OfType<BtsmtlSkillParallelFlowNode>().FirstOrDefault(
                value => string.Equals(value.UID, branchAuthoringId, StringComparison.Ordinal));
            if (branch == null)
                branch = (BtsmtlSkillParallelFlowNode)CodeGeneration.BtsmtlSkillAuthoringCode.EnsureFlowNode(
                    graph, typeof(BtsmtlSkillParallelFlowNode), branchAuthoringId, "相机与命中分支", new Vector2(240f, 260f));
            BtsmtlSkillAuthoringContract.Apply(branch, new[]
            {
                new BtsmtlSkillAuthoringFieldValue("steps", new[]
                {
                    BtsmtlSkillAuthoringContract.CreateStep("camera", "相机", null, 0, ProgramAbortPolicy.None),
                    BtsmtlSkillAuthoringContract.CreateStep("hit", "命中", null, 0, ProgramAbortPolicy.None)
                })
            });
            CodeGeneration.BtsmtlSkillAuthoringCode.EnsureFlowConnection(
                graph, trigger, "Output", branch, "Input", rootEdgeAuthoringId);
            CodeGeneration.BtsmtlSkillAuthoringCode.EnsureFlowConnection(
                graph, branch, branch.Steps[0].Id, node, "Input", cameraEdgeAuthoringId);
            for (int i = 0; i < originalTargets.Length; i++)
                CodeGeneration.BtsmtlSkillAuthoringCode.EnsureFlowConnection(
                    graph, branch, branch.Steps[1].Id, originalTargets[i], "Input", hitEdgeAuthoringId);
            if (node.EffectKind == effectKind &&
                string.Equals(node.ResourceId, resourceId, StringComparison.Ordinal))
                return 0;
            BtsmtlSkillAuthoringContract.Apply(node, new[]
            {
                new BtsmtlSkillAuthoringFieldValue("effectKind", effectKind),
                new BtsmtlSkillAuthoringFieldValue("resourceId", resourceId)
            });
            EditorUtility.SetDirty(definition);
            return 1;
        }

    }
}
