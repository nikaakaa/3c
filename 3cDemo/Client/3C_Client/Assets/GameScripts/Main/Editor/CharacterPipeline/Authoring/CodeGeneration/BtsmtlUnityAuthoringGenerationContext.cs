using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BTSMTL.Timeline;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class BtsmtlUnityAuthoringGenerationContext : BtsmtlAuthoringGenerationContext
    {
        sealed class AssetSnapshot
        {
            public string Path;
            public string AbsolutePath;
            public byte[] Content;
            public byte[] Meta;
        }

        readonly BtsmtlAuthoringGenerationRequest m_Request;
        readonly List<BtsmtlAuthoringCodeDiagnostic> m_Diagnostics = new();
        readonly List<string> m_DeletionRequests = new();
        readonly List<string> m_DeletedAssetPaths = new();
        readonly Dictionary<string, AssetSnapshot> m_AssetSnapshots = new(StringComparer.Ordinal);
        readonly bool m_OutputExisted;

        public BtsmtlUnityAuthoringGenerationContext(BtsmtlAuthoringGenerationRequest request)
        {
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
            SnapshotAsset(m_Request.OutputAssetPath);
            SnapshotAsset(m_Request.DefinitionAssetPath);
            m_OutputExisted = AssetDatabase.LoadMainAssetAtPath(m_Request.OutputAssetPath) != null;
        }

        public override string DefinitionAssetPath => m_Request.DefinitionAssetPath;
        public override string OutputAssetPath => m_Request.OutputAssetPath;

        public override T ResolveExternalAsset<T>(string assetPath, long localFileId)
        {
            string normalized = NormalizeAssetPath(assetPath);
            UnityEngine.Object asset = localFileId == 0L
                ? AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(normalized)
                : AssetDatabase.LoadAllAssetsAtPath(normalized)
                    .FirstOrDefault(value =>
                        value &&
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out _, out long currentId) &&
                        currentId == localFileId);
            if (!asset || asset is not T)
                throw new InvalidOperationException(
                    $"External authoring asset '{normalized}' with local id {localFileId} is not a {typeof(T).FullName}.");
            return (T)(object)asset;
        }

        public override void DeleteAsset(string assetPath)
        {
            string normalized = NormalizeAssetPath(assetPath);
            if (string.Equals(normalized, OutputAssetPath, StringComparison.Ordinal))
                throw new InvalidOperationException("正式生成入口不能删除当前输出资产。");
            SnapshotAsset(normalized);
            if (!m_DeletionRequests.Contains(normalized))
                m_DeletionRequests.Add(normalized);
        }

        public void Rollback()
        {
            foreach (AssetSnapshot snapshot in m_AssetSnapshots.Values)
            {
                foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(snapshot.Path))
                    if (asset)
                        EditorUtility.ClearDirty(asset);
                if (snapshot.Content == null)
                {
                    if (AssetDatabase.LoadMainAssetAtPath(snapshot.Path) != null)
                    {
                        if (!AssetDatabase.DeleteAsset(snapshot.Path))
                            throw new InvalidOperationException($"无法回退新建资产 '{snapshot.Path}'。");
                    }
                    else
                    {
                        if (File.Exists(snapshot.AbsolutePath))
                            File.Delete(snapshot.AbsolutePath);
                        if (File.Exists(snapshot.AbsolutePath + ".meta"))
                            File.Delete(snapshot.AbsolutePath + ".meta");
                    }
                    continue;
                }
                File.WriteAllBytes(snapshot.AbsolutePath, snapshot.Content);
                if (snapshot.Meta == null)
                {
                    if (File.Exists(snapshot.AbsolutePath + ".meta"))
                        File.Delete(snapshot.AbsolutePath + ".meta");
                }
                else
                    File.WriteAllBytes(snapshot.AbsolutePath + ".meta", snapshot.Meta);
            }
            foreach (AssetSnapshot snapshot in m_AssetSnapshots.Values)
                if (snapshot.Content != null)
                    AssetDatabase.ImportAsset(snapshot.Path,
                        ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        public override BtsmtlAuthoringGenerationResult Complete(object rootOutput)
        {
            if (rootOutput == null)
                return Fail(new BtsmtlAuthoringCodeDiagnostic(
                    BtsmtlAuthoringCodeDiagnosticSeverity.Error,
                    "generation_root_missing",
                    OutputAssetPath,
                    "正式生成入口没有返回根输出。"));
            try
            {
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(OutputAssetPath));
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(DefinitionAssetPath));
                foreach (string path in m_DeletionRequests)
                {
                    if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                        continue;
                    if (!AssetDatabase.DeleteAsset(path))
                        throw new InvalidOperationException($"正式生成入口无法删除旧资产 '{path}'。");
                    m_DeletedAssetPaths.Add(path);
                }
                return new BtsmtlAuthoringGenerationResult(
                    true,
                    rootOutput,
                    OutputAssetPath,
                    m_OutputExisted ? Array.Empty<string>() : new[] { OutputAssetPath },
                    m_OutputExisted ? new[] { OutputAssetPath } : Array.Empty<string>(),
                    m_DeletedAssetPaths,
                    m_Diagnostics,
                    true);
            }
            catch (Exception error)
            {
                return Fail(new BtsmtlAuthoringCodeDiagnostic(
                    BtsmtlAuthoringCodeDiagnosticSeverity.Error,
                    "generation_save_failed",
                    OutputAssetPath,
                    error.Message));
            }
        }

        public override BtsmtlAuthoringGenerationResult Fail(BtsmtlAuthoringCodeDiagnostic diagnostic)
        {
            m_Diagnostics.Add(diagnostic);
            return new BtsmtlAuthoringGenerationResult(
                false,
                null,
                OutputAssetPath,
                null,
                null,
                null,
                m_Diagnostics,
                false);
        }

        static string NormalizeAssetPath(string path)
        {
            string normalized = (path ?? string.Empty).Trim().Replace('\\', '/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal) || normalized.Contains(".."))
                throw new InvalidOperationException($"Asset path '{normalized}' is invalid.");
            return normalized;
        }

        void SnapshotAsset(string assetPath)
        {
            string path = NormalizeAssetPath(assetPath);
            if (m_AssetSnapshots.ContainsKey(path))
                return;
            if (!string.Equals(Path.GetExtension(path), ".asset", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"正式生成事务只能修改资产文件：'{path}'。");
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset && EditorUtility.IsDirty(asset))
                    throw new InvalidOperationException($"资产 '{path}' 有未保存的编辑器修改，不能开始正式生成。");
            string absolutePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                path.Replace('/', Path.DirectorySeparatorChar));
            m_AssetSnapshots.Add(path, new AssetSnapshot
            {
                Path = path,
                AbsolutePath = absolutePath,
                Content = File.Exists(absolutePath) ? File.ReadAllBytes(absolutePath) : null,
                Meta = File.Exists(absolutePath + ".meta") ? File.ReadAllBytes(absolutePath + ".meta") : null
            });
        }
    }
}
