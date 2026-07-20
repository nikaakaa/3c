using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.ProductBuild
{
    internal static class ClientBuildArtifactLayout
    {
        public const string DefaultPackageName = "DefaultPackage";
        public const string ProductStartupProfilePath = "Assets/Configs/ProductStartup/ProductStartupProfile.asset";
        public const string ContentReleaseManifestFileName = "CommercialContentRelease.manifest.json";
        public const string PlayerReleaseManifestFileName = "CommercialPlayerRelease.manifest.json";

        public static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        public static string BuildRoot => Path.Combine(ProjectRoot, "Build");
        public static string WorkspaceRoot => Path.Combine(BuildRoot, ".Workspace");
        public static string ContentRoot => Path.Combine(BuildRoot, "Content");
        public static string PlayersRoot => Path.Combine(BuildRoot, "Players");
        public static string NetworkRoot => Path.Combine(BuildRoot, "Network");
        public static string ContentRawRoot => Path.Combine(WorkspaceRoot, "Content", "Raw");
        public static string ContentCandidatesRoot => Path.Combine(WorkspaceRoot, "Content", "Candidates");
        public static string PlayerCandidatesRoot => Path.Combine(WorkspaceRoot, "Players", "Candidates");

        public static string GetContentVersionRoot(BuildTarget target, string packageVersion)
        {
            ValidateTarget(target);
            ValidateIdentity(packageVersion, nameof(packageVersion));
            return RequireDescendant(Path.Combine(ContentRoot, target.ToString(), DefaultPackageName, packageVersion), ContentRoot);
        }

        public static string GetPlayerVersionRoot(BuildTarget target, string clientBuildVersion)
        {
            ValidateTarget(target);
            ValidateIdentity(clientBuildVersion, nameof(clientBuildVersion));
            return RequireDescendant(Path.Combine(PlayersRoot, target.ToString(), clientBuildVersion), PlayersRoot);
        }

        public static string CreateContentCandidate()
        {
            return CreateCandidate(ContentCandidatesRoot);
        }

        public static string CreatePlayerCandidate()
        {
            return CreateCandidate(PlayerCandidatesRoot);
        }

        public static void DeleteWorkspaceCandidate(string path)
        {
            string fullPath = Path.GetFullPath(path);
            string workspace = Path.GetFullPath(WorkspaceRoot) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(workspace, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"拒绝删除工作区之外的路径：{fullPath}");
            if (Directory.Exists(fullPath))
                Directory.Delete(fullPath, true);
        }

        public static void PublishCandidate(string candidate, string destination)
        {
            RequireDescendant(candidate, WorkspaceRoot);
            string finalPath = RequireDescendant(destination, BuildRoot);
            if (finalPath.StartsWith(Path.GetFullPath(WorkspaceRoot) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Workspace 不能作为正式发布目录。");
            if (Directory.Exists(destination) || File.Exists(destination))
                throw new InvalidOperationException($"版本已经发布，禁止覆盖：{destination}");
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            Directory.Move(candidate, destination);
        }

        public static void ValidateIdentity(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "." || value == ".." ||
                value.IndexOf('/') >= 0 || value.IndexOf('\\') >= 0 ||
                value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException("版本只能是单个非空路径段。", parameterName);
            }
        }

        public static void ValidateTarget(BuildTarget target)
        {
            if (!Enum.IsDefined(typeof(BuildTarget), target) || target == BuildTarget.NoTarget)
                throw new ArgumentException("必须显式提供有效 BuildTarget。", nameof(target));
        }

        static string CreateCandidate(string root)
        {
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        static string RequireDescendant(string path, string root)
        {
            string fullPath = Path.GetFullPath(path);
            string fullRoot = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"路径离开允许的构建分区：{fullPath}");
            return fullPath;
        }
    }
}
