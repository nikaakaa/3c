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

        public BtsmtlUnityAuthoringGenerationContext(BtsmtlAuthoringGenerationRequest request)
        {
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
        }

        public override string SourceCodePath => m_Request.SourceCodePath;
        public override string RecipeType => m_Request.RecipeType;
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
                return new BtsmtlAuthoringGenerationResult(
                    true,
                    rootOutput,
                    OutputAssetPath,
                    new[] { OutputAssetPath },
                    new[] { OutputAssetPath },
                    Array.Empty<string>(),
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
