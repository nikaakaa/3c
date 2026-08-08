using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonRendering.ShapeProjection.Editor
{
    public static class CorinShapeProjectionContentInstaller
    {
        const string ProfilePath = "Assets/Configs/Character/Corin/Rendering/CorinShapeProjectionProfile.asset";
        const string ArtifactPath = "Assets/Configs/Character/Corin/Rendering/CorinShapeProjectionArtifact.asset";
        const string LocalPrefabPath = "Assets/Prefabs/Characters/RuntimeProfiles/Local/CorinStandalonePlayer.prefab";
        const string AiPrefabPath = "Assets/Prefabs/Characters/RuntimeProfiles/AI/CorinStandaloneTrainingEnemy.prefab";

        static readonly string[] FormalPrefabPaths =
        {
            LocalPrefabPath,
            "Assets/Prefabs/Characters/RuntimeProfiles/Rollback/CorinDeterministicRollback.prefab",
            "Assets/Prefabs/Characters/RuntimeProfiles/ServerAuthoritative/DotRecast/CorinServerAuthoritativeDotRecastClient.prefab",
            "Assets/Prefabs/Characters/RuntimeProfiles/ServerAuthoritative/UnityAuthority/CorinServerAuthoritativeUnityClient.prefab",
            "Assets/Prefabs/GameplayLab/GameplayLabLocalFixed.prefab"
        };

        static readonly string[] RendererSlots =
        {
            "Corin_Weapon",
            "Corin_body",
            "Corin_body_02",
            "Corin_face",
            "Corin_hair"
        };

        static readonly ShapeProjectionCapacity FormalCapacity = new ShapeProjectionCapacity
        {
            MaxRenderers = 8,
            MaxVertices = 120000,
            MaxTriangles = 180000,
            MaxRegions = 1024,
            MaxSharedChains = 2048,
            AtlasWidth = 2048,
            AtlasHeight = 2048,
            MaxContourPoints = 262144,
            MaxLoops = 4096,
            MaxIndirectInstances = 1024,
            ReadbackSlots = 3
        };

        [MenuItem("Tools/3C/Rendering/Build Corin Shape Projection Content")]
        public static void BuildFromMenu()
        {
            try
            {
                Debug.Log(BuildFormalContent());
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        public static string BuildFormalContent()
        {
            EnsureFolder("Assets/Configs/Character/Corin/Rendering");
            CharacterShapeProjectionProfile profile = LoadOrCreate<CharacterShapeProjectionProfile>(ProfilePath);
            CharacterShapeProjectionArtifact artifact = LoadOrCreate<CharacterShapeProjectionArtifact>(ArtifactPath);
            SynchronizeCapacity(profile);

            ShapeProjectionBakeReport report;
            GameObject canonical = PrefabUtility.LoadPrefabContents(LocalPrefabPath);
            try
            {
                SkinnedMeshRenderer[] renderers = CharacterShapeProjectionSourceInstaller.FindNamedRenderers(canonical,
                    RendererSlots);
                SynchronizeRules(profile, renderers);
                report = CharacterShapeProjectionBaker.Bake(profile, renderers, artifact);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(canonical);
            }

            int sourceCount = 0;
            for (int i = 0; i < FormalPrefabPaths.Length; i++)
                sourceCount += InstallPrefab(FormalPrefabPaths[i], profile, artifact);
            ValidateInheritedAi(profile, artifact);
            int rendererDataCount = CharacterShapeProjectionRendererInstaller.InstallFormalRenderers();
            AssetDatabase.SaveAssets();

            return $"Corin Shape Projection完成：Renderer {report.RendererCount}，Vertex {report.VertexCount}，Triangle {report.TriangleCount}，Excluded {report.ExcludedTriangleCount}，Region {report.RegionCount}，Chain {report.SharedChainCount}，Source {sourceCount}+AI继承，Renderer Data {rendererDataCount}，Hash {report.ContentHash}";
        }

        static int InstallPrefab(string path, CharacterShapeProjectionProfile profile,
            CharacterShapeProjectionArtifact artifact)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int count = CharacterShapeProjectionSourceInstaller.InstallAllCompleteRoots(contents, profile, artifact);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                return count;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static void ValidateInheritedAi(CharacterShapeProjectionProfile profile,
            CharacterShapeProjectionArtifact artifact)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(AiPrefabPath);
            try
            {
                CharacterShapeProjectionSource[] sources = contents.GetComponentsInChildren<CharacterShapeProjectionSource>(true);
                int matching = 0;
                for (int i = 0; i < sources.Length; i++)
                {
                    if (sources[i].Profile == profile && sources[i].Artifact == artifact)
                        matching++;
                }
                if (matching != 1)
                    throw new InvalidOperationException($"AI Prefab必须从Local基座继承唯一Source，当前为{matching}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static void SynchronizeRules(CharacterShapeProjectionProfile profile,
            SkinnedMeshRenderer[] renderers)
        {
            profile.EnsureIdentity();
            ShapeProjectionSubmeshRule[] existing = profile.SubmeshRules;
            List<ShapeProjectionSubmeshRule> next = new List<ShapeProjectionSubmeshRule>();
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                SkinnedMeshRenderer renderer = renderers[rendererIndex];
                if (renderer.sharedMesh == null || renderer.sharedMesh.subMeshCount != renderer.sharedMaterials.Length)
                    throw new InvalidOperationException($"{renderer.name}的Mesh/Submesh/Material不完整");
                for (int submesh = 0; submesh < renderer.sharedMesh.subMeshCount; submesh++)
                {
                    Material material = renderer.sharedMaterials[submesh];
                    ShapeProjectionSubmeshRule rule = CreateDefaultRule(renderer.name, submesh, material);
                    for (int i = 0; i < existing.Length; i++)
                    {
                        if (existing[i].RendererSlotId == renderer.name && existing[i].SubmeshIndex == submesh
                            && existing[i].Material == material)
                        {
                            rule = existing[i];
                            break;
                        }
                    }
                    next.Add(rule);
                }
            }

            if (!RulesEqual(existing, next))
                profile.ReplaceSubmeshRules(next.ToArray());
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
        }

        static void SynchronizeCapacity(CharacterShapeProjectionProfile profile)
        {
            profile.EnsureIdentity();
            if (!profile.Capacity.Equals(FormalCapacity))
                profile.ReplaceCapacity(FormalCapacity);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
        }

        static ShapeProjectionSubmeshRule CreateDefaultRule(string slot, int submesh, Material material)
        {
            bool alphaClip = material != null && material.HasProperty("_AlphaClip") && material.GetFloat("_AlphaClip") > 0.5f;
            float threshold = material != null && material.HasProperty("_Cutoff") ? material.GetFloat("_Cutoff") : 0.5f;
            return new ShapeProjectionSubmeshRule(slot, submesh, material,
                alphaClip ? ShapeProjectionMaterialMode.IncludeCutout : ShapeProjectionMaterialMode.IncludeOpaque,
                threshold, false, Color.white);
        }

        static bool RulesEqual(ShapeProjectionSubmeshRule[] left, List<ShapeProjectionSubmeshRule> right)
        {
            if (left == null || left.Length != right.Count)
                return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i].RendererSlotId != right[i].RendererSlotId
                    || left[i].SubmeshIndex != right[i].SubmeshIndex
                    || left[i].Material != right[i].Material || left[i].Mode != right[i].Mode
                    || !Mathf.Approximately(left[i].AlphaThreshold, right[i].AlphaThreshold)
                    || left[i].OverrideRepresentativeColor != right[i].OverrideRepresentativeColor
                    || left[i].RepresentativeColor != right[i].RepresentativeColor)
                    return false;
            }
            return true;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }
    }
}
