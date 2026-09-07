using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL
{
    internal sealed class CharacterAclNativeArtifactIdentity
    {
        const string ManifestRelativePath =
            "Tools/Native/ACL/acl-native-manifest.json";
        const string ExpectedSchema = "3c-acl-native-artifact/v2";
        const string ExpectedLibrary = "3c_acl_runtime";
        const string ExpectedRtmVersion = "2.2.0";
        const string ExpectedRtmCommit =
            "d7982f2b2524feeb322f424b29cf43df30b5d5b7";
        const string ExpectedPlatform = "Windows-x64";
        const string ExpectedCompiler = "MSVC 19.38.33145.0";
        const string ExpectedFloatingPoint = "precise";
        const string ExpectedCallingConvention = "cdecl";
        const string ExpectedArtifactPath =
            "Assets/Plugins/3C/ACL/Windows/x86_64/3c_acl_runtime.dll";
        const int ExpectedAbiVersion = 2;
        const int ExpectedPayloadFormatVersion = 10;
        const int ExpectedStructVersion = 1;
        const int ExpectedAlignment = 16;
        static readonly string[] ExpectedBuildOptions =
        {
            "ACL_NO_ASSERT_CHECKS",
            "/O2",
            "/EHsc",
            "/MD",
            "/fp:precise",
            "/GR-",
            "/std:c++17",
            "/permissive-"
        };
        static readonly string[] ExpectedCapabilities =
        {
            "transform-compression",
            "scalar-compression",
            "database",
            "groups",
            "decoder",
            "split-bulk"
        };
        static readonly string[] ExpectedExportedSymbols =
        {
            "acl_project_get_abi",
            "acl_project_group_build",
            "acl_project_group_build_get_output",
            "acl_project_group_build_release",
            "acl_project_group_create",
            "acl_project_group_get_clip_count",
            "acl_project_group_release",
            "acl_project_decoder_create",
            "acl_project_decoder_bind",
            "acl_project_decoder_unbind",
            "acl_project_decoder_sample_transform",
            "acl_project_decoder_sample_scalar",
            "acl_project_decoder_destroy"
        };

        [Serializable]
        sealed class ManifestData
        {
            public string schema;
            public string library;
            public string aclVersion;
            public string aclCommit;
            public string rtmVersion;
            public string rtmCommit;
            public int payloadFormatVersion;
            public int abiVersion;
            public int structVersion;
            public string platform;
            public ArtifactData artifact;
            public string[] editor;
            public string[] player;
            public string compiler;
            public string[] buildOptions;
            public string floatingPoint;
            public bool simd;
            public string callingConvention;
            public int alignment;
            public string[] capabilities;
            public AbiStructData abiStructs;
            public string[] exportedSymbols;
        }

        [Serializable]
        sealed class ArtifactData
        {
            public string path;
            public long sizeBytes;
            public string sha256;
        }

        [Serializable]
        sealed class AbiStructData
        {
            public string abiInfo;
            public string transformInput;
            public string scalarInput;
            public string groupBuildInput;
            public string groupBuildOutput;
            public string groupPayloadInput;
            public string decoderBindInput;
        }

        CharacterAclNativeArtifactIdentity(
            string identity,
            string binarySha256,
            string platform,
            int abiVersion,
            int payloadFormatVersion)
        {
            Identity = identity;
            BinarySha256 = binarySha256;
            Platform = platform;
            AbiVersion = abiVersion;
            PayloadFormatVersion = payloadFormatVersion;
        }

        internal string Identity { get; }
        internal string BinarySha256 { get; }
        internal string Platform { get; }
        internal int AbiVersion { get; }
        internal int PayloadFormatVersion { get; }

        internal static CharacterAclNativeArtifactIdentity RequireCurrent()
        {
            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException(
                    "Unity project root is unavailable.");
            string manifestPath = Path.GetFullPath(
                Path.Combine(projectRoot, "..", "..", "..", ManifestRelativePath));
            if (!File.Exists(manifestPath))
                throw new InvalidOperationException(
                    $"ACL native artifact manifest is missing: {manifestPath}");
            ManifestData manifest = JsonUtility.FromJson<ManifestData>(
                File.ReadAllText(manifestPath, Encoding.UTF8));
            RequireManifest(manifest);
            if (EditorUserBuildSettings.activeBuildTarget !=
                BuildTarget.StandaloneWindows64)
            {
                throw new InvalidOperationException(
                    "ACL native artifacts require the StandaloneWindows64 active build target.");
            }
            PluginImporter importer =
                AssetImporter.GetAtPath(manifest.artifact.path) as PluginImporter;
            bool compatibleWithAny = importer?.GetCompatibleWithAnyPlatform() ?? true;
            bool compatibleWithEditor = importer?.GetCompatibleWithEditor() ?? false;
            bool compatibleWithWindows64 = importer?.GetCompatibleWithPlatform(
                BuildTarget.StandaloneWindows64) ?? false;
            bool compatibleWithWindows32 = importer?.GetCompatibleWithPlatform(
                BuildTarget.StandaloneWindows) ?? false;
            bool compatibleWithLinux64 = importer?.GetCompatibleWithPlatform(
                BuildTarget.StandaloneLinux64) ?? false;
            bool compatibleWithOsx = importer?.GetCompatibleWithPlatform(
                BuildTarget.StandaloneOSX) ?? false;
            string editorCpu = importer?.GetEditorData("CPU") ?? string.Empty;
            string editorOs = importer?.GetEditorData("OS") ?? string.Empty;
            string playerCpu = importer == null
                ? string.Empty
                : importer.GetPlatformData(
                    BuildTarget.StandaloneWindows64,
                    "CPU");
            if (compatibleWithAny ||
                !compatibleWithEditor ||
                !compatibleWithWindows64 ||
                compatibleWithWindows32 ||
                compatibleWithLinux64 ||
                compatibleWithOsx ||
                !string.Equals(editorCpu, "x86_64", StringComparison.Ordinal) ||
                !string.Equals(editorOs, "Windows", StringComparison.Ordinal) ||
                !string.Equals(playerCpu, "x86_64", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"ACL native artifact PluginImporter settings do not match the Windows x64 matrix: importer={importer != null}, any={compatibleWithAny}, editor={compatibleWithEditor}, windows64={compatibleWithWindows64}, windows32={compatibleWithWindows32}, linux64={compatibleWithLinux64}, osx={compatibleWithOsx}, editorCpu='{editorCpu}', editorOs='{editorOs}', playerCpu='{playerCpu}'.");
            }
            string artifactPath = Path.GetFullPath(
                Path.Combine(
                    projectRoot,
                    manifest.artifact.path.Replace(
                        '/',
                        Path.DirectorySeparatorChar)));
            if (!File.Exists(artifactPath))
                throw new InvalidOperationException(
                    $"ACL native artifact is missing: {artifactPath}");
            var file = new FileInfo(artifactPath);
            if (file.Length != manifest.artifact.sizeBytes)
                throw new InvalidOperationException(
                    "ACL native artifact size does not match its manifest.");
            string actualHash = ComputeFileHash(artifactPath);
            if (!string.Equals(
                    actualHash,
                    manifest.artifact.sha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "ACL native artifact SHA256 does not match its manifest.");
            }
            string identity = CharacterAclHash.ComputeStrings(
                new[]
                {
                    "acl-native-artifact/v2",
                    manifest.schema,
                    manifest.library,
                    manifest.aclVersion,
                    manifest.aclCommit,
                    manifest.rtmVersion,
                    manifest.rtmCommit,
                    manifest.payloadFormatVersion.ToString(CultureInfo.InvariantCulture),
                    manifest.abiVersion.ToString(CultureInfo.InvariantCulture),
                    manifest.structVersion.ToString(CultureInfo.InvariantCulture),
                    manifest.platform,
                    manifest.artifact.path,
                    manifest.artifact.sizeBytes.ToString(CultureInfo.InvariantCulture),
                    manifest.artifact.sha256,
                    string.Join(",", manifest.editor),
                    string.Join(",", manifest.player),
                    manifest.compiler,
                    string.Join(",", manifest.buildOptions),
                    manifest.floatingPoint,
                    manifest.simd ? "simd" : "no-simd",
                    manifest.callingConvention,
                    manifest.alignment.ToString(CultureInfo.InvariantCulture),
                    string.Join(",", manifest.capabilities),
                    manifest.abiStructs.abiInfo,
                    manifest.abiStructs.transformInput,
                    manifest.abiStructs.scalarInput,
                    manifest.abiStructs.groupBuildInput,
                    manifest.abiStructs.groupBuildOutput,
                    manifest.abiStructs.groupPayloadInput,
                    manifest.abiStructs.decoderBindInput,
                    string.Join(",", manifest.exportedSymbols)
                });
            return new CharacterAclNativeArtifactIdentity(
                identity,
                manifest.artifact.sha256,
                manifest.platform,
                manifest.abiVersion,
                manifest.payloadFormatVersion);
        }

        static void RequireManifest(ManifestData manifest)
        {
            if (manifest == null ||
                manifest.artifact == null ||
                !string.Equals(manifest.schema, ExpectedSchema, StringComparison.Ordinal) ||
                !string.Equals(manifest.library, ExpectedLibrary, StringComparison.Ordinal) ||
                !string.Equals(
                    manifest.aclVersion,
                    CharacterAclAnimationResourceManifest.OfficialAclVersion,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    manifest.aclCommit,
                    CharacterAclAnimationResourceManifest.OfficialAclCommit,
                    StringComparison.Ordinal) ||
                !string.Equals(manifest.rtmVersion, ExpectedRtmVersion, StringComparison.Ordinal) ||
                !string.Equals(manifest.rtmCommit, ExpectedRtmCommit, StringComparison.Ordinal) ||
                manifest.payloadFormatVersion != ExpectedPayloadFormatVersion ||
                manifest.abiVersion != ExpectedAbiVersion ||
                manifest.structVersion != ExpectedStructVersion ||
                !string.Equals(manifest.platform, ExpectedPlatform, StringComparison.Ordinal) ||
                !string.Equals(manifest.artifact.path, ExpectedArtifactPath, StringComparison.Ordinal) ||
                manifest.artifact.sizeBytes <= 0 ||
                !CharacterAclHash.IsSha256(manifest.artifact.sha256) ||
                !string.Equals(manifest.compiler, ExpectedCompiler, StringComparison.Ordinal) ||
                !string.Equals(manifest.floatingPoint, ExpectedFloatingPoint, StringComparison.Ordinal) ||
                !manifest.simd ||
                !string.Equals(manifest.callingConvention, ExpectedCallingConvention, StringComparison.Ordinal) ||
                manifest.alignment != ExpectedAlignment ||
                manifest.abiStructs == null)
            {
                throw new InvalidOperationException(
                    "ACL native artifact manifest does not match the formal Windows contract.");
            }
            RequirePlatformList(manifest.editor, "Mono");
            RequirePlatformList(manifest.player, "IL2CPP");
            RequireList(manifest.buildOptions, ExpectedBuildOptions);
            RequireList(manifest.capabilities, ExpectedCapabilities);
            RequireList(manifest.exportedSymbols, ExpectedExportedSymbols);
            RequireAbiStructs(manifest.abiStructs);
        }

        static void RequirePlatformList(string[] values, string compiler)
        {
            if (values == null ||
                values.Length != 2 ||
                !string.Equals(values[0], compiler, StringComparison.Ordinal) ||
                !string.Equals(values[1], ExpectedPlatform, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "ACL native artifact platform list does not match the formal contract.");
            }
        }

        static void RequireList(
            string[] actual,
            string[] expected)
        {
            if (actual == null || actual.Length != expected.Length)
                throw new InvalidOperationException(
                    "ACL native artifact manifest list length is invalid.");
            for (int i = 0; i < expected.Length; i++)
            {
                if (!string.Equals(
                        actual[i],
                        expected[i],
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "ACL native artifact manifest list content is invalid.");
                }
            }
        }

        static void RequireAbiStructs(AbiStructData actual)
        {
            if (!string.Equals(actual.abiInfo, "acl_project_abi_info", StringComparison.Ordinal) ||
                !string.Equals(actual.transformInput, "acl_project_transform_input", StringComparison.Ordinal) ||
                !string.Equals(actual.scalarInput, "acl_project_scalar_input", StringComparison.Ordinal) ||
                !string.Equals(actual.groupBuildInput, "acl_project_group_build_input", StringComparison.Ordinal) ||
                !string.Equals(actual.groupBuildOutput, "acl_project_group_build_output", StringComparison.Ordinal) ||
                !string.Equals(actual.groupPayloadInput, "acl_project_group_payload_input", StringComparison.Ordinal) ||
                !string.Equals(actual.decoderBindInput, "acl_project_decoder_bind_input", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "ACL native artifact ABI struct names are invalid.");
            }
        }

        static string ComputeFileHash(string path)
        {
            using (var sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] hash = sha.ComputeHash(stream);
                var builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    builder.Append(hash[i].ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
