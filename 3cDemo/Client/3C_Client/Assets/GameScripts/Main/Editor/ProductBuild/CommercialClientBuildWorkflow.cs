using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThirdPerson.ProductStartup;
using ThirdPersonCharacter.Editor.ProductStartup;
using TEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

namespace ThirdPersonCharacter.Editor.ProductBuild
{
    internal enum CommercialClientBuildMode
    {
        Content = 1,
        Player = 2,
        ContentAndPlayer = 3
    }

    internal readonly struct CommercialClientBuildRequest
    {
        public CommercialClientBuildRequest(BuildTarget target, string resourcePackageVersion, string minimumClientBuildVersion, CommercialClientBuildMode mode)
        {
            Target = target;
            ResourcePackageVersion = resourcePackageVersion;
            MinimumClientBuildVersion = minimumClientBuildVersion;
            Mode = mode;
        }

        public BuildTarget Target { get; }
        public string ResourcePackageVersion { get; }
        public string MinimumClientBuildVersion { get; }
        public CommercialClientBuildMode Mode { get; }
    }

    internal readonly struct CommercialClientBuildResult
    {
        public CommercialClientBuildResult(string contentPath, string playerPath)
        {
            ContentPath = contentPath ?? string.Empty;
            PlayerPath = playerPath ?? string.Empty;
        }

        public string ContentPath { get; }
        public string PlayerPath { get; }
    }

    internal static class CommercialClientBuildWorkflow
    {
        static readonly UTF8Encoding s_Utf8WithoutBom = new UTF8Encoding(false);

        public static CommercialClientBuildResult Build(CommercialClientBuildRequest request)
        {
            ValidatedBuildIdentity identity = ValidateRequest(request);
            string contentPath = string.Empty;
            string playerPath = string.Empty;
            if (request.Mode == CommercialClientBuildMode.Content || request.Mode == CommercialClientBuildMode.ContentAndPlayer)
                contentPath = BuildContent(request, identity);
            if (request.Mode == CommercialClientBuildMode.Player || request.Mode == CommercialClientBuildMode.ContentAndPlayer)
                playerPath = BuildPlayer(request, identity);
            return new CommercialClientBuildResult(contentPath, playerPath);
        }

        static ValidatedBuildIdentity ValidateRequest(CommercialClientBuildRequest request)
        {
            ClientBuildArtifactLayout.ValidateTarget(request.Target);
            ClientBuildArtifactLayout.ValidateIdentity(request.ResourcePackageVersion, nameof(request.ResourcePackageVersion));
            if (!ClientBuildVersion.TryParse(request.MinimumClientBuildVersion, out ClientBuildVersion minimumVersion))
                throw new BuildFailedException("MinimumClientBuildVersion 必须是三段或四段非负数字版本。");
            ProductStartupProfile profile = AssetDatabase.LoadAssetAtPath<ProductStartupProfile>(ClientBuildArtifactLayout.ProductStartupProfilePath);
            if (!profile)
                throw new BuildFailedException($"缺少唯一正式启动配置：{ClientBuildArtifactLayout.ProductStartupProfilePath}");
            if (!profile.TryGetClientBuildVersion(out ClientBuildVersion clientVersion))
                throw new BuildFailedException("ProductStartupProfile 的 ClientBuildVersion 无效。");
            if (minimumVersion > clientVersion)
                throw new BuildFailedException("MinimumClientBuildVersion 不能高于当前 Player 的 ClientBuildVersion。");
            return new ValidatedBuildIdentity(clientVersion.ToString(), minimumVersion.ToString());
        }

        static string BuildContent(CommercialClientBuildRequest request, ValidatedBuildIdentity identity)
        {
            string destination = ClientBuildArtifactLayout.GetContentVersionRoot(request.Target, request.ResourcePackageVersion);
            RejectExistingVersion(destination);
            string candidate = ClientBuildArtifactLayout.CreateContentCandidate();
            try
            {
                string rawVersionPath = Path.Combine(ClientBuildArtifactLayout.ContentRawRoot, request.Target.ToString(), ClientBuildArtifactLayout.DefaultPackageName, request.ResourcePackageVersion);
                ClientBuildArtifactLayout.DeleteWorkspaceCandidate(rawVersionPath);
                var config = CreateContentConfig(request);
                TEngineContentBuildResult build = ReleaseTools.BuildContent(new TEngineContentBuildRequest(config));
                if (!build.Success)
                    throw new BuildFailedException($"Content 构建失败：{build.Error}");
                var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    YooAssetSettingsData.GetPackageVersionFileName(config.PackageName),
                    YooAssetSettingsData.GetManifestBinaryFileName(config.PackageName, config.PackageVersion),
                    YooAssetSettingsData.GetManifestJsonFileName(config.PackageName, config.PackageVersion),
                    YooAssetSettingsData.GetPackageHashFileName(config.PackageName, config.PackageVersion)
                };
                string manifestJsonPath = Path.Combine(build.OutputPackageDirectory, YooAssetSettingsData.GetManifestJsonFileName(config.PackageName, config.PackageVersion));
                foreach (string bundleFileName in ReadManifestBundleFiles(manifestJsonPath, build.Report, config.PackageName, config.PackageVersion))
                {
                    if (!required.Add(bundleFileName))
                        throw new BuildFailedException($"YooAsset Manifest 包含重复运行时文件：{bundleFileName}");
                }
                foreach (string relativePath in required.OrderBy(value => value, StringComparer.Ordinal))
                    CopyRequiredFile(build.OutputPackageDirectory, candidate, relativePath);

                string policyJson = JsonConvert.SerializeObject(new
                {
                    schemaVersion = StartupPolicy.CurrentSchemaVersion,
                    minimumClientBuildVersion = identity.MinimumClientBuildVersion
                }, Formatting.Indented);
                StartupPolicyResult policyResult = StartupPolicyClient.Parse(policyJson);
                if (!policyResult.Succeeded)
                    throw new BuildFailedException($"StartupPolicy 生成后校验失败：{policyResult.ErrorCode} {policyResult.SafeError}");
                File.WriteAllText(Path.Combine(candidate, ProductStartupProfile.StartupPolicyFileName), policyJson, s_Utf8WithoutBom);

                var manifest = new CommercialContentReleaseManifest
                {
                    SchemaVersion = CommercialContentReleaseManifest.CurrentSchemaVersion,
                    BuildTarget = request.Target.ToString(),
                    PackageName = config.PackageName,
                    ResourcePackageVersion = request.ResourcePackageVersion,
                    MinimumClientBuildVersion = identity.MinimumClientBuildVersion,
                    Files = BuildFileRecords(candidate, ClientBuildArtifactLayout.ContentReleaseManifestFileName)
                };
                WriteJson(Path.Combine(candidate, ClientBuildArtifactLayout.ContentReleaseManifestFileName), manifest);
                ValidateContentCandidate(candidate, manifest, request.Target, request.ResourcePackageVersion);
                ClientBuildArtifactLayout.PublishCandidate(candidate, destination);
                return destination;
            }
            catch
            {
                ClientBuildArtifactLayout.DeleteWorkspaceCandidate(candidate);
                throw;
            }
        }

        static BuildConfig CreateContentConfig(CommercialClientBuildRequest request)
        {
            return new BuildConfig
            {
                BuildTarget = request.Target,
                BuildPipeline = EBuildPipeline.ScriptableBuildPipeline,
                CompressOption = ECompressOption.LZ4,
                EncryptionType = EncryptionType.None,
                PackageName = ClientBuildArtifactLayout.DefaultPackageName,
                PackageVersion = request.ResourcePackageVersion,
                BuildOutputRoot = ClientBuildArtifactLayout.ContentRawRoot,
                MinimalPackage = true,
                RetainTags = string.Empty,
                EnableSharePackRule = true,
                UseAssetDependencyDB = true,
                ClearBuildCache = false,
                VerifyBuildingResult = true,
                BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll,
                FileNameStyle = EFileNameStyle.BundleName_HashName,
                BuildHotFixDll = true
            };
        }

        static string BuildPlayer(CommercialClientBuildRequest request, ValidatedBuildIdentity identity)
        {
            string contentPath = ClientBuildArtifactLayout.GetContentVersionRoot(request.Target, request.ResourcePackageVersion);
            string contentManifestPath = Path.Combine(contentPath, ClientBuildArtifactLayout.ContentReleaseManifestFileName);
            CommercialContentReleaseManifest contentManifest = ReadJson<CommercialContentReleaseManifest>(contentManifestPath);
            ValidateContentCandidate(contentPath, contentManifest, request.Target, request.ResourcePackageVersion);
            string contentManifestHash = ComputeSha256(contentManifestPath);
            string destination = ClientBuildArtifactLayout.GetPlayerVersionRoot(request.Target, identity.ClientBuildVersion);
            RejectExistingVersion(destination);
            PrepareBuiltInContent(contentPath, request.ResourcePackageVersion);
            string candidate = ClientBuildArtifactLayout.CreatePlayerCandidate();
            try
            {
                PlayerOutput output = GetPlayerOutput(request.Target, candidate);
                string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
                TEnginePlayerBuildResult build;
                using (ProductBuildValidationContext.Enter(ProductBuildKind.CommercialClient))
                    build = ReleaseTools.BuildPlayer(new TEnginePlayerBuildRequest(request.Target, output.BuildLocation, scenes, BuildOptions.None));
                if (!build.Success)
                    throw new BuildFailedException($"Player 构建失败：{build.Error}");
                ValidatePlayerEntry(candidate, request.Target, output.EntryPath);
                var manifest = new CommercialPlayerReleaseManifest
                {
                    SchemaVersion = CommercialPlayerReleaseManifest.CurrentSchemaVersion,
                    BuildTarget = request.Target.ToString(),
                    ClientBuildVersion = identity.ClientBuildVersion,
                    PackageName = ClientBuildArtifactLayout.DefaultPackageName,
                    ResourcePackageVersion = request.ResourcePackageVersion,
                    ContentReleaseManifestSha256 = contentManifestHash,
                    EntryPath = output.EntryPath,
                    Files = BuildFileRecords(candidate, ClientBuildArtifactLayout.PlayerReleaseManifestFileName)
                };
                WriteJson(Path.Combine(candidate, ClientBuildArtifactLayout.PlayerReleaseManifestFileName), manifest);
                ValidatePlayerCandidate(candidate, manifest, request.Target, identity.ClientBuildVersion, request.ResourcePackageVersion, contentManifestHash);
                ClientBuildArtifactLayout.PublishCandidate(candidate, destination);
                return destination;
            }
            catch
            {
                ClientBuildArtifactLayout.DeleteWorkspaceCandidate(candidate);
                throw;
            }
        }

        internal static CommercialPublishedPlayer ValidatePublishedPlayer(BuildTarget target, string clientBuildVersion)
        {
            ClientBuildArtifactLayout.ValidateTarget(target);
            ClientBuildArtifactLayout.ValidateIdentity(clientBuildVersion, nameof(clientBuildVersion));
            string playerRoot = ClientBuildArtifactLayout.GetPlayerVersionRoot(target, clientBuildVersion);
            string playerManifestPath = Path.Combine(playerRoot, ClientBuildArtifactLayout.PlayerReleaseManifestFileName);
            CommercialPlayerReleaseManifest playerManifest = ReadJson<CommercialPlayerReleaseManifest>(playerManifestPath);
            if (playerManifest == null || playerManifest.SchemaVersion != CommercialPlayerReleaseManifest.CurrentSchemaVersion ||
                playerManifest.BuildTarget != target.ToString() || playerManifest.ClientBuildVersion != clientBuildVersion ||
                playerManifest.PackageName != ClientBuildArtifactLayout.DefaultPackageName)
            {
                throw new BuildFailedException("Player 发布清单身份无效。");
            }
            ClientBuildArtifactLayout.ValidateIdentity(playerManifest.ResourcePackageVersion, nameof(playerManifest.ResourcePackageVersion));
            string contentRoot = ClientBuildArtifactLayout.GetContentVersionRoot(target, playerManifest.ResourcePackageVersion);
            string contentManifestPath = Path.Combine(contentRoot, ClientBuildArtifactLayout.ContentReleaseManifestFileName);
            CommercialContentReleaseManifest contentManifest = ReadJson<CommercialContentReleaseManifest>(contentManifestPath);
            ValidateContentCandidate(contentRoot, contentManifest, target, playerManifest.ResourcePackageVersion);
            string contentManifestHash = ComputeSha256(contentManifestPath);
            ValidatePlayerCandidate(
                playerRoot,
                playerManifest,
                target,
                clientBuildVersion,
                playerManifest.ResourcePackageVersion,
                contentManifestHash);
            return new CommercialPublishedPlayer(playerRoot, ResolveContainedPath(playerRoot, playerManifest.EntryPath));
        }

        static void ValidateContentCandidate(string root, CommercialContentReleaseManifest manifest, BuildTarget target, string resourceVersion)
        {
            if (manifest == null || manifest.SchemaVersion != CommercialContentReleaseManifest.CurrentSchemaVersion || manifest.BuildTarget != target.ToString() ||
                manifest.PackageName != ClientBuildArtifactLayout.DefaultPackageName || manifest.ResourcePackageVersion != resourceVersion ||
                !ClientBuildVersion.TryParse(manifest.MinimumClientBuildVersion, out _))
                throw new BuildFailedException("Content 发布清单身份无效。");
            ValidateClosure(root, ClientBuildArtifactLayout.ContentReleaseManifestFileName, manifest.Files);
            foreach (CommercialReleaseFile file in manifest.Files)
            {
                string[] segments = NormalizeRelativePath(file.Path).Split('/');
                if (segments.Any(segment => segment.Equals("OutputCache", StringComparison.OrdinalIgnoreCase) || segment.StartsWith("Simulate", StringComparison.OrdinalIgnoreCase)) ||
                    file.Path.EndsWith(".report", StringComparison.OrdinalIgnoreCase))
                    throw new BuildFailedException($"Content 发布闭包包含 Editor 构建文件：{file.Path}");
            }
            string policyPath = Path.Combine(root, ProductStartupProfile.StartupPolicyFileName);
            if (!File.Exists(policyPath))
                throw new BuildFailedException("Content 缺少 StartupPolicy.json。");
            StartupPolicyResult parsed = StartupPolicyClient.Parse(File.ReadAllText(policyPath, Encoding.UTF8));
            if (!parsed.Succeeded || parsed.Policy.MinimumClientBuildVersion.ToString() != manifest.MinimumClientBuildVersion)
                throw new BuildFailedException("StartupPolicy 与 Content 发布清单不一致。");
        }

        static void ValidatePlayerCandidate(string root, CommercialPlayerReleaseManifest manifest, BuildTarget target, string clientVersion, string resourceVersion, string contentManifestHash)
        {
            if (manifest == null || manifest.SchemaVersion != CommercialPlayerReleaseManifest.CurrentSchemaVersion || manifest.BuildTarget != target.ToString() ||
                manifest.ClientBuildVersion != clientVersion || manifest.PackageName != ClientBuildArtifactLayout.DefaultPackageName ||
                manifest.ResourcePackageVersion != resourceVersion || !string.Equals(manifest.ContentReleaseManifestSha256, contentManifestHash, StringComparison.OrdinalIgnoreCase))
                throw new BuildFailedException("Player 发布清单身份无效。");
            ValidatePlayerEntry(root, target, manifest.EntryPath);
            ValidateClosure(root, ClientBuildArtifactLayout.PlayerReleaseManifestFileName, manifest.Files);
        }

        static void ValidateClosure(string root, string manifestName, IReadOnlyCollection<CommercialReleaseFile> records)
        {
            if (records == null)
                throw new BuildFailedException("发布清单缺少文件闭包。");
            var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { manifestName };
            foreach (CommercialReleaseFile record in records)
            {
                string relative = NormalizeRelativePath(record.Path);
                if (!expected.Add(relative))
                    throw new BuildFailedException($"发布清单包含重复路径：{relative}");
                string fullPath = ResolveContainedFile(root, relative);
                var file = new FileInfo(fullPath);
                if (!file.Exists || file.Length != record.Length || !string.Equals(ComputeSha256(fullPath), record.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new BuildFailedException($"发布文件校验失败：{relative}");
            }
            var actual = new HashSet<string>(Directory.GetFiles(root, "*", SearchOption.AllDirectories).Select(path => GetRelativePath(root, path)), StringComparer.OrdinalIgnoreCase);
            if (!actual.SetEquals(expected))
                throw new BuildFailedException("发布目录存在清单外文件或缺少清单内文件。");
        }

        static List<CommercialReleaseFile> BuildFileRecords(string root, string excludedManifest)
        {
            return Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                .Select(path => new { FullPath = path, RelativePath = GetRelativePath(root, path) })
                .Where(file => !string.Equals(file.RelativePath, excludedManifest, StringComparison.OrdinalIgnoreCase))
                .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
                .Select(file => new CommercialReleaseFile { Path = file.RelativePath, Length = new FileInfo(file.FullPath).Length, Sha256 = ComputeSha256(file.FullPath) })
                .ToList();
        }

        static void CopyRequiredFile(string sourceRoot, string destinationRoot, string relativePath)
        {
            string relative = NormalizeRelativePath(relativePath);
            string source = ResolveContainedFile(sourceRoot, relative);
            if (!File.Exists(source))
                throw new FileNotFoundException("YooAsset 运行时闭包缺少文件。", source);
            string destination = ResolveContainedPath(destinationRoot, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.Copy(source, destination, false);
        }

        static IReadOnlyList<string> ReadManifestBundleFiles(string manifestPath, BuildReport report, string packageName, string packageVersion)
        {
            JObject document;
            try
            {
                document = JObject.Parse(File.ReadAllText(manifestPath, Encoding.UTF8), new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                });
            }
            catch (Exception exception) when (exception is IOException || exception is JsonException)
            {
                throw new BuildFailedException($"YooAsset Manifest 无法读取：{exception.Message}");
            }
            if (document.Value<string>("PackageName") != packageName || document.Value<string>("PackageVersion") != packageVersion ||
                document["BundleList"] is not JArray bundles)
                throw new BuildFailedException("YooAsset Manifest 身份或 BundleList 无效。");

            var reportBundles = report.BundleInfos.ToDictionary(bundle => bundle.BundleName, StringComparer.Ordinal);
            var files = new List<string>(bundles.Count);
            foreach (JToken token in bundles)
            {
                if (token is not JObject bundle || bundle["BundleName"]?.Type != JTokenType.String || bundle["FileHash"]?.Type != JTokenType.String)
                    throw new BuildFailedException("YooAsset Manifest Bundle 记录无效。");
                string bundleName = bundle.Value<string>("BundleName");
                string fileHash = bundle.Value<string>("FileHash");
                if (!reportBundles.TryGetValue(bundleName, out ReportBundleInfo reportBundle) || !string.Equals(reportBundle.FileHash, fileHash, StringComparison.Ordinal))
                    throw new BuildFailedException($"YooAsset Manifest 与 BuildReport 不一致：{bundleName}");
                files.Add(reportBundle.FileName);
            }
            if (files.Count != reportBundles.Count)
                throw new BuildFailedException("YooAsset Manifest 与 BuildReport 的 Bundle 数量不一致。");
            return files;
        }

        static void ValidatePlayerEntry(string root, BuildTarget target, string entryPath)
        {
            string entry = ResolveContainedPath(root, entryPath);
            if (!File.Exists(entry) && !Directory.Exists(entry))
                throw new BuildFailedException($"Player 缺少平台入口：{entryPath}");
            if (target == BuildTarget.StandaloneWindows64 && !Directory.Exists(Path.Combine(root, "3C_Client_Data")))
                throw new BuildFailedException("Windows Player 缺少 3C_Client_Data。");
        }

        static void PrepareBuiltInContent(string contentPath, string packageVersion)
        {
            string root = Path.GetFullPath(AssetBundleBuilderHelper.GetStreamingAssetsRoot());
            string streamingAssets = Path.GetFullPath(Application.streamingAssetsPath);
            if (!root.StartsWith(streamingAssets + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new BuildFailedException($"YooAsset 内置资源目录不在 StreamingAssets 子目录：{root}");
            if (Directory.Exists(root))
                Directory.Delete(root, true);
            Directory.CreateDirectory(root);
            string packageName = ClientBuildArtifactLayout.DefaultPackageName;
            string[] files =
            {
                YooAssetSettingsData.GetPackageVersionFileName(packageName),
                YooAssetSettingsData.GetManifestBinaryFileName(packageName, packageVersion),
                YooAssetSettingsData.GetPackageHashFileName(packageName, packageVersion)
            };
            foreach (string fileName in files)
                CopyRequiredFile(contentPath, root, fileName);
            AssetDatabase.Refresh();
        }

        static PlayerOutput GetPlayerOutput(BuildTarget target, string candidate)
        {
            return target switch
            {
                BuildTarget.StandaloneWindows64 => new PlayerOutput(Path.Combine(candidate, "3C_Client.exe"), "3C_Client.exe"),
                BuildTarget.StandaloneLinux64 => new PlayerOutput(Path.Combine(candidate, "3C_Client"), "3C_Client"),
                BuildTarget.StandaloneOSX => new PlayerOutput(Path.Combine(candidate, "3C_Client.app"), "3C_Client.app"),
                BuildTarget.Android => new PlayerOutput(Path.Combine(candidate, "3C_Client.apk"), "3C_Client.apk"),
                BuildTarget.WebGL => new PlayerOutput(candidate, "index.html"),
                BuildTarget.iOS => new PlayerOutput(candidate, "Unity-iPhone.xcodeproj"),
                _ => throw new BuildFailedException($"正式客户端构建尚未定义平台入口：{target}")
            };
        }

        static void RejectExistingVersion(string destination)
        {
            if (Directory.Exists(destination) || File.Exists(destination))
                throw new BuildFailedException($"版本已经发布，禁止覆盖：{destination}");
        }

        static string ResolveContainedFile(string root, string relativePath)
        {
            string path = ResolveContainedPath(root, relativePath);
            if (Directory.Exists(path))
                throw new BuildFailedException($"清单路径必须指向文件：{relativePath}");
            return path;
        }

        static string ResolveContainedPath(string root, string relativePath)
        {
            string fullRoot = Path.GetFullPath(root);
            string fullPath = Path.GetFullPath(Path.Combine(fullRoot, NormalizeRelativePath(relativePath).Replace('/', Path.DirectorySeparatorChar)));
            if (!fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new BuildFailedException($"路径逃逸发布目录：{relativePath}");
            return fullPath;
        }

        static string NormalizeRelativePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
                throw new BuildFailedException("发布清单路径必须是非空相对路径。");
            string normalized = relativePath.Replace('\\', '/').TrimStart('/');
            if (normalized.Split('/').Any(segment => segment.Length == 0 || segment == "." || segment == ".."))
                throw new BuildFailedException($"发布清单路径无效：{relativePath}");
            return normalized;
        }

        static string GetRelativePath(string root, string fullPath)
        {
            string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string path = Path.GetFullPath(fullPath);
            if (!path.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                throw new BuildFailedException($"文件不在发布目录内：{path}");
            return path.Substring(fullRoot.Length).Replace('\\', '/');
        }

        static string ComputeSha256(string path)
        {
            using SHA256 sha256 = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
        }

        static void WriteJson<T>(string path, T value)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(value, Formatting.Indented), s_Utf8WithoutBom);
        }

        static T ReadJson<T>(string path)
        {
            if (!File.Exists(path))
                throw new BuildFailedException($"缺少发布清单：{path}");
            try
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(path, Encoding.UTF8));
            }
            catch (JsonException exception)
            {
                throw new BuildFailedException($"发布清单 JSON 无效：{exception.Message}");
            }
        }

        readonly struct ValidatedBuildIdentity
        {
            public ValidatedBuildIdentity(string clientBuildVersion, string minimumClientBuildVersion)
            {
                ClientBuildVersion = clientBuildVersion;
                MinimumClientBuildVersion = minimumClientBuildVersion;
            }
            public string ClientBuildVersion { get; }
            public string MinimumClientBuildVersion { get; }
        }

        readonly struct PlayerOutput
        {
            public PlayerOutput(string buildLocation, string entryPath)
            {
                BuildLocation = buildLocation;
                EntryPath = entryPath;
            }
            public string BuildLocation { get; }
            public string EntryPath { get; }
        }
    }
}
