using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class BtsmtlUnityAuthoringGenerationContext : BtsmtlAuthoringGenerationContext
    {
        readonly BtsmtlAuthoringGenerationRequest m_Request;
        readonly List<BtsmtlAuthoringCodeDiagnostic> m_Diagnostics = new();
        readonly List<string> m_DeletionRequests = new();
        readonly List<string> m_DeletedAssetPaths = new();
        readonly bool m_OutputExisted;

        public BtsmtlUnityAuthoringGenerationContext(BtsmtlAuthoringGenerationRequest request)
        {
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
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
            if (!m_DeletionRequests.Contains(normalized))
                m_DeletionRequests.Add(normalized);
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
                AssetDatabase.SaveAssets();
                foreach (string path in m_DeletionRequests)
                {
                    if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                        continue;
                    if (!AssetDatabase.DeleteAsset(path))
                        throw new InvalidOperationException($"正式生成入口无法删除旧资产 '{path}'。");
                    m_DeletedAssetPaths.Add(path);
                }
                AssetDatabase.SaveAssets();
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
    }
}
