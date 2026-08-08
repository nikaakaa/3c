using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonRendering.ShapeProjection.Editor
{
    public sealed class CharacterShapeProjectionBakerWindow : EditorWindow
    {
        sealed class RendererSelection
        {
            public SkinnedMeshRenderer Renderer;
            public bool Included = true;
        }

        CharacterShapeProjectionProfile profile;
        GameObject sourceRoot;
        CharacterShapeProjectionArtifact artifact;
        readonly List<RendererSelection> selections = new List<RendererSelection>();
        Vector2 scroll;
        string report;
        MessageType reportType = MessageType.Info;

        [MenuItem("Tools/3C/Rendering/Shape Projection Baker")]
        static void Open()
        {
            GetWindow<CharacterShapeProjectionBakerWindow>("Shape Projection Baker");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Character Shape Projection", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("只有明确按钮会扫描Renderer、Bake或修改Prefab。选择、Repaint和Play Mode不会自动执行。", MessageType.Info);

            profile = (CharacterShapeProjectionProfile)EditorGUILayout.ObjectField("Profile", profile,
                typeof(CharacterShapeProjectionProfile), false);
            sourceRoot = (GameObject)EditorGUILayout.ObjectField("Source Root", sourceRoot, typeof(GameObject), true);
            artifact = (CharacterShapeProjectionArtifact)EditorGUILayout.ObjectField("Artifact", artifact,
                typeof(CharacterShapeProjectionArtifact), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("刷新Renderer列表"))
                    RefreshRenderers();
                if (GUILayout.Button("创建Artifact"))
                    CreateArtifact();
            }

            if (GUILayout.Button("安装Shape Projection到正式URP Renderer"))
                InstallRendererFeature();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("显式Renderer集合", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(160f));
            for (int i = 0; i < selections.Count; i++)
            {
                RendererSelection selection = selections[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    selection.Included = EditorGUILayout.Toggle(selection.Included, GUILayout.Width(18f));
                    EditorGUILayout.ObjectField(selection.Renderer, typeof(SkinnedMeshRenderer), true);
                }
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUI.DisabledScope(profile == null || selections.Count == 0))
            {
                if (GUILayout.Button("同步所选Renderer/Submesh显式规则"))
                    SyncSubmeshRules();
            }

            using (new EditorGUI.DisabledScope(profile == null || artifact == null || selections.Count == 0))
            {
                if (GUILayout.Button("显式Bake并替换Artifact", GUILayout.Height(30f)))
                    Bake();
            }

            using (new EditorGUI.DisabledScope(profile == null || artifact == null || sourceRoot == null))
            {
                if (GUILayout.Button("将Artifact安装到当前Source Root"))
                    InstallSource();
            }

            if (!string.IsNullOrEmpty(report))
                EditorGUILayout.HelpBox(report, reportType);
        }

        void RefreshRenderers()
        {
            selections.Clear();
            if (sourceRoot == null)
            {
                SetReport("请选择Source Root", MessageType.Error);
                return;
            }

            SkinnedMeshRenderer[] renderers = sourceRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Array.Sort(renderers, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            for (int i = 0; i < renderers.Length; i++)
                selections.Add(new RendererSelection { Renderer = renderers[i] });
            SetReport($"已显式读取{selections.Count}个SkinnedMeshRenderer；请取消不参与的项", MessageType.Info);
        }

        void CreateArtifact()
        {
            string path = EditorUtility.SaveFilePanelInProject("创建Shape Projection Artifact",
                "CharacterShapeProjectionArtifact", "asset", "选择Artifact保存路径");
            if (string.IsNullOrEmpty(path))
                return;

            CharacterShapeProjectionArtifact created = CreateInstance<CharacterShapeProjectionArtifact>();
            AssetDatabase.CreateAsset(created, path);
            AssetDatabase.SaveAssets();
            artifact = created;
            Selection.activeObject = created;
            SetReport($"已创建{path}", MessageType.Info);
        }

        void Bake()
        {
            try
            {
                List<SkinnedMeshRenderer> selected = new List<SkinnedMeshRenderer>();
                for (int i = 0; i < selections.Count; i++)
                {
                    if (selections[i].Included && selections[i].Renderer != null)
                        selected.Add(selections[i].Renderer);
                }
                ShapeProjectionBakeReport bakeReport = CharacterShapeProjectionBaker.Bake(profile, selected, artifact);
                SetReport($"Bake完成：Renderer {bakeReport.RendererCount}，Vertex {bakeReport.VertexCount}，Triangle {bakeReport.TriangleCount}，Excluded {bakeReport.ExcludedTriangleCount}，Region {bakeReport.RegionCount}，Chain {bakeReport.SharedChainCount}，Dependency {bakeReport.DependencyCount}，Hash {bakeReport.ContentHash}", MessageType.Info);
            }
            catch (Exception exception)
            {
                SetReport(exception.Message, MessageType.Error);
                Debug.LogException(exception);
            }
        }

        void SyncSubmeshRules()
        {
            try
            {
                List<ShapeProjectionSubmeshRule> rules = new List<ShapeProjectionSubmeshRule>();
                ShapeProjectionSubmeshRule[] existing = profile.SubmeshRules;
                for (int rendererIndex = 0; rendererIndex < selections.Count; rendererIndex++)
                {
                    RendererSelection selection = selections[rendererIndex];
                    if (!selection.Included || selection.Renderer == null)
                        continue;
                    SkinnedMeshRenderer renderer = selection.Renderer;
                    if (renderer.sharedMesh == null || renderer.sharedMesh.subMeshCount != renderer.sharedMaterials.Length)
                        throw new InvalidOperationException($"Renderer {renderer.name}的Mesh/Submesh/Material不完整");
                    for (int submesh = 0; submesh < renderer.sharedMesh.subMeshCount; submesh++)
                    {
                        Material material = renderer.sharedMaterials[submesh];
                        ShapeProjectionSubmeshRule resolved = new ShapeProjectionSubmeshRule(renderer.name, submesh,
                            material, ShapeProjectionMaterialMode.IncludeOpaque, 0.5f, false, Color.white);
                        for (int i = 0; i < existing.Length; i++)
                        {
                            if (existing[i].RendererSlotId == renderer.name && existing[i].SubmeshIndex == submesh
                                && existing[i].Material == material)
                            {
                                resolved = existing[i];
                                break;
                            }
                        }
                        rules.Add(resolved);
                    }
                }
                if (rules.Count == 0)
                    throw new InvalidOperationException("没有选中任何可配置Renderer");
                if (RulesEqual(existing, rules))
                {
                    SetReport("Renderer/Submesh规则已经一致，没有修改Profile", MessageType.Info);
                    return;
                }

                Undo.RecordObject(profile, "Sync Shape Projection Submesh Rules");
                profile.EnsureIdentity();
                profile.ReplaceSubmeshRules(rules.ToArray());
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
                SetReport($"已同步{rules.Count}条精确Renderer/Submesh规则；请明确设置Cutout、Exclude和Alpha阈值后再Bake", MessageType.Info);
            }
            catch (Exception exception)
            {
                SetReport(exception.Message, MessageType.Error);
                Debug.LogException(exception);
            }
        }

        void InstallRendererFeature()
        {
            try
            {
                int count = CharacterShapeProjectionRendererInstaller.InstallFormalRenderers();
                SetReport($"已校验并安装{count}个正式URP Renderer Data", MessageType.Info);
            }
            catch (Exception exception)
            {
                SetReport(exception.Message, MessageType.Error);
                Debug.LogException(exception);
            }
        }

        void InstallSource()
        {
            try
            {
                string prefabPath = AssetDatabase.GetAssetPath(sourceRoot);
                if (!string.IsNullOrEmpty(prefabPath) && PrefabUtility.IsPartOfPrefabAsset(sourceRoot))
                {
                    GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
                    try
                    {
                        CharacterShapeProjectionSourceInstaller.InstallExactRoot(contents, profile, artifact);
                        PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(contents);
                    }
                }
                else
                {
                    CharacterShapeProjectionSourceInstaller.InstallExactRoot(sourceRoot, profile, artifact);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(sourceRoot);
                }
                SetReport("Source、显式Renderer绑定与ShadowsOnly职责已安装", MessageType.Info);
            }
            catch (Exception exception)
            {
                SetReport(exception.Message, MessageType.Error);
                Debug.LogException(exception);
            }
        }

        void SetReport(string message, MessageType type)
        {
            report = message;
            reportType = type;
            Repaint();
        }

        static bool RulesEqual(ShapeProjectionSubmeshRule[] existing, List<ShapeProjectionSubmeshRule> next)
        {
            if (existing == null || existing.Length != next.Count)
                return false;
            for (int i = 0; i < existing.Length; i++)
            {
                ShapeProjectionSubmeshRule left = existing[i];
                ShapeProjectionSubmeshRule right = next[i];
                if (left.RendererSlotId != right.RendererSlotId || left.SubmeshIndex != right.SubmeshIndex
                    || left.Material != right.Material || left.Mode != right.Mode
                    || !Mathf.Approximately(left.AlphaThreshold, right.AlphaThreshold)
                    || left.OverrideRepresentativeColor != right.OverrideRepresentativeColor
                    || left.RepresentativeColor != right.RepresentativeColor)
                    return false;
            }
            return true;
        }
    }
}
