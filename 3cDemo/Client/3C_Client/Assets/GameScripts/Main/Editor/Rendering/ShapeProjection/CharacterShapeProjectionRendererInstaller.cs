using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering.ShapeProjection.Editor
{
    public static class CharacterShapeProjectionRendererInstaller
    {
        const string ComputePath = "Assets/Shader/CharacterShapeProjection/CharacterShapeProjectionMask.compute";
        const string MaterialPath = "Assets/Shader/CharacterShapeProjection/CharacterShapeProjectionComposite.mat";

        static readonly string[] RendererPaths =
        {
            "Assets/Settings/URP-HighFidelity-Renderer.asset",
            "Assets/Settings/URP-Balanced-Renderer.asset",
            "Assets/Settings/URP-Performant-Renderer.asset"
        };
        static bool installQueued;
        static double installNotBefore;

        [MenuItem("Tools/3C/Rendering/Install Shape Projection Renderer Feature")]
        public static void InstallFromMenu()
        {
            if (installQueued)
                return;
            installQueued = true;
            installNotBefore = EditorApplication.timeSinceStartup + 3.0;
            EditorApplication.update += PollInstall;
        }

        static void PollInstall()
        {
            if (EditorApplication.timeSinceStartup < installNotBefore)
                return;
            EditorApplication.update -= PollInstall;
            installQueued = false;
            try
            {
                int count = InstallFormalRenderers();
                Debug.Log($"Shape Projection已安装到{count}个正式URP Renderer Data");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public static int InstallFormalRenderers()
        {
            ComputeShader compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (compute == null || material == null)
                throw new InvalidOperationException("Shape Projection Compute或Composite Material尚未成功导入");

            for (int pathIndex = 0; pathIndex < RendererPaths.Length; pathIndex++)
            {
                string path = RendererPaths[pathIndex];
                ScriptableRendererData data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
                if (data == null)
                    throw new InvalidOperationException($"缺少正式URP Renderer Data：{path}");

                CharacterShapeProjectionRendererFeature feature = null;
                int count = 0;
                for (int i = 0; i < data.rendererFeatures.Count; i++)
                {
                    if (data.rendererFeatures[i] is CharacterShapeProjectionRendererFeature candidate)
                    {
                        feature = candidate;
                        count++;
                    }
                }
                if (count > 1)
                    throw new InvalidOperationException($"{path}存在重复Shape Projection Renderer Feature");
                if (feature == null)
                {
                    feature = ScriptableObject.CreateInstance<CharacterShapeProjectionRendererFeature>();
                    feature.name = "Character Shape Projection";
                    AssetDatabase.AddObjectToAsset(feature, data);
                    data.rendererFeatures.Add(feature);
                }

                SerializedObject serialized = new SerializedObject(feature);
                serialized.FindProperty("maskCompute").objectReferenceValue = compute;
                serialized.FindProperty("compositeMaterial").objectReferenceValue = material;
                serialized.FindProperty("maxCameraSourceWorkspaces").intValue = 16;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                feature.SetActive(true);
                EditorUtility.SetDirty(feature);
                data.SetDirty();
                AssetDatabase.SaveAssetIfDirty(data);
            }
            return RendererPaths.Length;
        }
    }
}
