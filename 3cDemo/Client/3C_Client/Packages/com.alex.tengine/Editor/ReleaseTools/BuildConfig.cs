using UnityEditor;
using YooAsset;
using YooAsset.Editor;

namespace TEngine
{
    public sealed class BuildConfig
    {
        public BuildTarget BuildTarget;
        public EBuildPipeline BuildPipeline = EBuildPipeline.ScriptableBuildPipeline;
        public ECompressOption CompressOption = ECompressOption.LZ4;
        public EncryptionType EncryptionType = EncryptionType.None;
        public string PackageName = string.Empty;
        public string PackageVersion = string.Empty;
        public string BuildOutputRoot = string.Empty;
        public bool MinimalPackage;
        public string RetainTags = string.Empty;
        public bool EnableSharePackRule = true;
        public bool UseAssetDependencyDB = true;
        public bool ClearBuildCache;
        public bool VerifyBuildingResult = true;
        public EBuildinFileCopyOption BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll;
        public EFileNameStyle FileNameStyle = EFileNameStyle.BundleName_HashName;
        public bool BuildHotFixDll = true;
    }
}
