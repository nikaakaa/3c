using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;
using PlayerBuildResult = UnityEditor.Build.Reporting.BuildResult;

namespace TEngine
{
    public static class ReleaseTools
    {
        public static TEngineContentBuildResult BuildContent(TEngineContentBuildRequest request)
        {
            try
            {
                BuildConfig config = ValidateContentRequest(request);
                BuildTargetGroup group = GetBuildTargetGroup(config.BuildTarget);
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(group, config.BuildTarget))
                    throw new InvalidOperationException($"Unable to switch active build target to {config.BuildTarget}.");
                if (config.BuildHotFixDll)
                    BuildDLLCommand.BuildAndCopyDlls(config.BuildTarget);

                AssetDatabase.Refresh();
                YooAsset.Editor.BuildResult result = RunContentBuild(config);
                if (!result.Success)
                    return new TEngineContentBuildResult(false, result.OutputPackageDirectory, string.Empty, null, result.ErrorInfo);

                string reportPath = Path.Combine(
                    result.OutputPackageDirectory,
                    YooAssetSettingsData.GetBuildReportFileName(config.PackageName, config.PackageVersion));
                if (!File.Exists(reportPath))
                    throw new FileNotFoundException("YooAsset build report was not produced.", reportPath);

                YooAsset.Editor.BuildReport report = YooAsset.Editor.BuildReport.Deserialize(File.ReadAllText(reportPath, System.Text.Encoding.UTF8));
                if (config.MinimalPackage)
                    ProcessMinimalPackage(config, report);

                AssetDatabase.Refresh();
                return new TEngineContentBuildResult(true, result.OutputPackageDirectory, reportPath, report, string.Empty);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return new TEngineContentBuildResult(false, string.Empty, string.Empty, null, exception.Message);
            }
        }

        public static TEnginePlayerBuildResult BuildPlayer(TEnginePlayerBuildRequest request)
        {
            try
            {
                ValidatePlayerRequest(request);
                BuildTargetGroup group = GetBuildTargetGroup(request.Target);
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(group, request.Target))
                    throw new InvalidOperationException($"Unable to switch active build target to {request.Target}.");

                string parent = Path.GetDirectoryName(request.OutputPath);
                if (!string.IsNullOrEmpty(parent))
                    Directory.CreateDirectory(parent);
                AssetDatabase.Refresh();

                var options = new BuildPlayerOptions
                {
                    scenes = request.Scenes,
                    locationPathName = request.OutputPath,
                    targetGroup = group,
                    target = request.Target,
                    options = request.Options
                };
                UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
                bool success = report.summary.result == PlayerBuildResult.Succeeded;
                return new TEnginePlayerBuildResult(success, report, success ? string.Empty : report.summary.result.ToString());
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return new TEnginePlayerBuildResult(false, null, exception.Message);
            }
        }

        static BuildConfig ValidateContentRequest(TEngineContentBuildRequest request)
        {
            BuildConfig config = request?.Config ?? throw new ArgumentNullException(nameof(request));
            if (config.BuildTarget == BuildTarget.NoTarget)
                throw new ArgumentException("Build target is required.");
            if (string.IsNullOrWhiteSpace(config.PackageName))
                throw new ArgumentException("Package name is required.");
            if (string.IsNullOrWhiteSpace(config.PackageVersion))
                throw new ArgumentException("Package version is required.");
            if (string.IsNullOrWhiteSpace(config.BuildOutputRoot) || !Path.IsPathRooted(config.BuildOutputRoot))
                throw new ArgumentException("Build output root must be an explicit absolute path.");
            return config;
        }

        static void ValidatePlayerRequest(TEnginePlayerBuildRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.Target == BuildTarget.NoTarget)
                throw new ArgumentException("Build target is required.");
            if (string.IsNullOrWhiteSpace(request.OutputPath) || !Path.IsPathRooted(request.OutputPath))
                throw new ArgumentException("Player output path must be an explicit absolute path.");
            if (request.Scenes == null || request.Scenes.Length == 0 || request.Scenes.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("At least one explicit scene is required.");
        }

        static YooAsset.Editor.BuildResult RunContentBuild(BuildConfig config)
        {
            IBuildPipeline pipeline;
            BuildParameters parameters;
            if (config.BuildPipeline == EBuildPipeline.BuiltinBuildPipeline)
            {
                var builtin = new BuiltinBuildParameters { CompressOption = config.CompressOption };
                pipeline = new BuiltinBuildPipeline();
                parameters = builtin;
            }
            else
            {
                var scriptable = new ScriptableBuildParameters
                {
                    CompressOption = config.CompressOption,
                    BuiltinShadersBundleName = GetBuiltinShaderBundleName(config.PackageName),
                    ReplaceAssetPathWithAddress = Settings.UpdateSetting.GetReplaceAssetPathWithAddress()
                };
                pipeline = new ScriptableBuildPipeline();
                parameters = scriptable;
            }

            parameters.BuildOutputRoot = Path.GetFullPath(config.BuildOutputRoot).Replace('\\', '/');
            parameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            parameters.BuildPipeline = config.BuildPipeline.ToString();
            parameters.BuildTarget = config.BuildTarget;
            parameters.BuildBundleType = (int)EBuildBundleType.AssetBundle;
            parameters.PackageName = config.PackageName;
            parameters.PackageVersion = config.PackageVersion;
            parameters.VerifyBuildingResult = config.VerifyBuildingResult;
            parameters.EnableSharePackRule = config.EnableSharePackRule;
            parameters.FileNameStyle = config.FileNameStyle;
            parameters.BuildinFileCopyOption = config.BuildinFileCopyOption;
            parameters.BuildinFileCopyParams = string.Empty;
            parameters.EncryptionServices = GetEncryption(config.EncryptionType);
            parameters.ClearBuildCacheFiles = config.ClearBuildCache;
            parameters.UseAssetDependencyDB = config.UseAssetDependencyDB;
            return pipeline.Run(parameters, true);
        }

        static void ProcessMinimalPackage(BuildConfig config, YooAsset.Editor.BuildReport report)
        {
            string streamingRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            if (!Directory.Exists(streamingRoot))
                return;

            string[] retainTags = string.IsNullOrWhiteSpace(config.RetainTags)
                ? Array.Empty<string>()
                : config.RetainTags.Split(',', '，').Select(value => value.Trim()).Where(value => value.Length > 0).ToArray();
            var retained = new HashSet<string>(
                report.BundleInfos
                    .Where(bundle => bundle.Tags != null && bundle.Tags.Any(tag => retainTags.Contains(tag, StringComparer.Ordinal)))
                    .Select(bundle => bundle.FileName),
                StringComparer.Ordinal);

            foreach (string path in Directory.GetFiles(streamingRoot, "*.bundle", SearchOption.AllDirectories))
            {
                if (!retained.Contains(Path.GetFileName(path)))
                    File.Delete(path);
            }
            DeleteEmptyDirectories(streamingRoot);
        }

        static void DeleteEmptyDirectories(string root)
        {
            foreach (string directory in Directory.GetDirectories(root))
            {
                DeleteEmptyDirectories(directory);
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                    Directory.Delete(directory);
            }
        }

        static BuildTargetGroup GetBuildTargetGroup(BuildTarget target)
        {
            return target switch
            {
                BuildTarget.Android => BuildTargetGroup.Android,
                BuildTarget.iOS => BuildTargetGroup.iOS,
                BuildTarget.WebGL => BuildTargetGroup.WebGL,
                BuildTarget.Switch => BuildTargetGroup.Switch,
                BuildTarget.PS4 => BuildTargetGroup.PS4,
                BuildTarget.PS5 => BuildTargetGroup.PS5,
                _ => BuildTargetGroup.Standalone
            };
        }

        static string GetBuiltinShaderBundleName(string packageName)
        {
            var rule = DefaultPackRule.CreateShadersPackRuleResult();
            return rule.GetBundleName(packageName, AssetBundleCollectorSettingData.Setting.UniqueBundleName);
        }

        static IEncryptionServices GetEncryption(EncryptionType type)
        {
            return type switch
            {
                EncryptionType.FileOffSet => new FileOffsetEncryption(),
                EncryptionType.FileStream => new FileStreamEncryption(),
                _ => null
            };
        }
    }
}
