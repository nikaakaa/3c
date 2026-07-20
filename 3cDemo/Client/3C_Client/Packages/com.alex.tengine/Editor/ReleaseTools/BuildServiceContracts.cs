using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;
using UnityPlayerBuildReport = UnityEditor.Build.Reporting.BuildReport;

namespace TEngine
{
    public sealed class TEngineContentBuildRequest
    {
        public TEngineContentBuildRequest(BuildConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            if (!Enum.IsDefined(typeof(BuildTarget), config.BuildTarget) || config.BuildTarget == BuildTarget.NoTarget)
                throw new ArgumentException("Build target is required.", nameof(config));
            if (string.IsNullOrWhiteSpace(config.PackageName) || string.IsNullOrWhiteSpace(config.PackageVersion))
                throw new ArgumentException("Package name and version are required.", nameof(config));
            if (string.IsNullOrWhiteSpace(config.BuildOutputRoot) || !Path.IsPathRooted(config.BuildOutputRoot))
                throw new ArgumentException("Build output root must be an explicit absolute path.", nameof(config));
        }

        public BuildConfig Config { get; }
    }

    public sealed class TEngineContentBuildResult
    {
        internal TEngineContentBuildResult(bool success, string outputPackageDirectory, string reportPath, BuildReport report, string error)
        {
            Success = success;
            OutputPackageDirectory = outputPackageDirectory ?? string.Empty;
            ReportPath = reportPath ?? string.Empty;
            Report = report;
            Error = error ?? string.Empty;
        }

        public bool Success { get; }
        public string OutputPackageDirectory { get; }
        public string ReportPath { get; }
        public BuildReport Report { get; }
        public string Error { get; }
    }

    public sealed class TEnginePlayerBuildRequest
    {
        public TEnginePlayerBuildRequest(BuildTarget target, string outputPath, string[] scenes, BuildOptions options)
        {
            if (!Enum.IsDefined(typeof(BuildTarget), target) || target == BuildTarget.NoTarget)
                throw new ArgumentException("Build target is required.", nameof(target));
            if (string.IsNullOrWhiteSpace(outputPath) || !Path.IsPathRooted(outputPath))
                throw new ArgumentException("Player output path must be an explicit absolute path.", nameof(outputPath));
            if (scenes == null || scenes.Length == 0)
                throw new ArgumentException("At least one explicit scene is required.", nameof(scenes));
            Target = target;
            OutputPath = outputPath;
            Scenes = scenes;
            Options = options;
        }

        public BuildTarget Target { get; }
        public string OutputPath { get; }
        public string[] Scenes { get; }
        public BuildOptions Options { get; }
    }

    public sealed class TEnginePlayerBuildResult
    {
        internal TEnginePlayerBuildResult(bool success, UnityPlayerBuildReport report, string error)
        {
            Success = success;
            Report = report;
            Error = error ?? string.Empty;
        }

        public bool Success { get; }
        public UnityPlayerBuildReport Report { get; }
        public string Error { get; }
    }
}
