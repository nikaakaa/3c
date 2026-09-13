using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL
{
    internal static class CharacterAclAnimationArtifactFileStore
    {
        internal static void EnsureFolder(string folder)
        {
            folder = NormalizeAssetPath(folder);
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        internal static void CreateTransactionFolder(
            string parent,
            string folder)
        {
            parent = NormalizeAssetPath(parent);
            folder = NormalizeAssetPath(folder);
            if (!folder.StartsWith(parent + "/", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "ACL transaction folder is outside its parent.");
            EnsureFolder(parent);
            if (AssetDatabase.IsValidFolder(folder) ||
                Directory.Exists(ToProjectFilePath(folder)))
                throw new InvalidOperationException(
                    $"ACL transaction folder already exists: {folder}");
            string name = Path.GetFileName(folder);
            AssetDatabase.CreateFolder(parent, name);
            Refresh();
            if (!AssetDatabase.IsValidFolder(folder))
                throw new InvalidOperationException(
                    $"ACL transaction folder could not be created: {folder}");
        }

        internal static bool Exists(string assetPath) =>
            LogicalPathExists(assetPath);

        internal static bool LogicalPathExists(string assetPath) =>
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) ||
            File.Exists(ToProjectFilePath(assetPath)) ||
            File.Exists(ToProjectFilePath(assetPath) + ".meta");

        internal static void Move(
            string sourcePath,
            string destinationPath)
        {
            if (!LogicalPathExists(sourcePath))
                throw new InvalidOperationException(
                    $"ACL output move source is missing: {sourcePath}.");
            if (LogicalPathExists(destinationPath))
                throw new InvalidOperationException(
                    $"ACL output destination already exists: {destinationPath}.");
            string error = AssetDatabase.MoveAsset(
                sourcePath,
                destinationPath);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException(error);
            Refresh();
            if (LogicalPathExists(sourcePath) ||
                !LogicalPathExists(destinationPath))
            {
                throw new InvalidOperationException(
                    $"ACL output move did not complete: {sourcePath} -> {destinationPath}.");
            }
        }

        internal static void Delete(string assetPath)
        {
            if (!LogicalPathExists(assetPath))
                return;
            if (!AssetDatabase.DeleteAsset(assetPath))
                throw new InvalidOperationException(
                    $"ACL output delete failed: {assetPath}.");
            Refresh();
            if (LogicalPathExists(assetPath))
                throw new InvalidOperationException(
                    $"ACL output remained after delete: {assetPath}.");
        }

        internal static void DeleteFolderIfPresent(
            string folder,
            string prefix)
        {
            if (!string.IsNullOrEmpty(folder))
                DeleteFolder(folder, prefix, false);
        }

        internal static void DeleteFolder(
            string folder,
            string prefix,
            bool required = true)
        {
            folder = NormalizeAssetPath(folder);
            string parent = Path.GetDirectoryName(folder)?.Replace(
                '\\',
                '/') ?? string.Empty;
            if (string.IsNullOrEmpty(parent) ||
                !folder.StartsWith(parent + "/" + prefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "ACL transaction folder escaped its output root.");
            }
            bool exists = AssetDatabase.IsValidFolder(folder) ||
                Directory.Exists(ToProjectFilePath(folder));
            if (!exists)
            {
                if (required)
                    throw new InvalidOperationException(
                        $"ACL transaction folder is missing: {folder}");
                return;
            }
            if (AssetDatabase.IsValidFolder(folder) &&
                !AssetDatabase.DeleteAsset(folder))
            {
                throw new InvalidOperationException(
                    $"ACL transaction folder delete failed: {folder}");
            }
            Refresh();
            if (AssetDatabase.IsValidFolder(folder))
                throw new InvalidOperationException(
                    $"ACL transaction folder remained after delete: {folder}");
            string fullPath = ToProjectFilePath(folder);
            if (Directory.Exists(fullPath))
                Directory.Delete(fullPath, true);
            if (Directory.Exists(fullPath))
                throw new InvalidOperationException(
                    $"ACL transaction folder remained on disk: {folder}");
            Refresh();
        }

        internal static void TryDeleteFolder(
            string folder,
            string prefix,
            List<string> failures)
        {
            try
            {
                DeleteFolderIfPresent(folder, prefix);
            }
            catch (Exception exception)
            {
                failures.Add($"{folder}: {exception.Message}");
            }
        }

        internal static void Refresh() =>
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        internal static string ReadAllTextUtf8(string assetPath) =>
            File.ReadAllText(ToProjectFilePath(assetPath), Encoding.UTF8);

        internal static byte[] ReadAllBytes(string assetPath) =>
            File.ReadAllBytes(ToProjectFilePath(assetPath));

        internal static void WriteAllTextUtf8(
            string assetPath,
            string content) =>
            File.WriteAllText(
                ToProjectFilePath(assetPath),
                content,
                Encoding.UTF8);

        internal static void WriteAllBytes(
            string assetPath,
            byte[] content) =>
            File.WriteAllBytes(ToProjectFilePath(assetPath), content);

        internal static string ToProjectFilePath(string assetPath) =>
            Path.GetFullPath(
                Path.Combine(
                    ProjectRoot(),
                    NormalizeAssetPath(assetPath)));

        internal static string ToAssetPath(string fullPath)
        {
            string root = ProjectRoot().TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            string normalizedRoot = root.Replace('\\', '/');
            string normalizedPath = Path.GetFullPath(fullPath)
                .Replace('\\', '/');
            if (!normalizedPath.StartsWith(
                    normalizedRoot + "/",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "ACL artifact path is outside the Unity project.");
            }
            return normalizedPath.Substring(normalizedRoot.Length + 1);
        }

        static string NormalizeAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                throw new ArgumentException(
                    "ACL artifact asset path is required.",
                    nameof(assetPath));
            return assetPath.Replace('\\', '/').TrimEnd('/');
        }

        static string ProjectRoot() =>
            Directory.GetParent(Application.dataPath)?.FullName ??
            throw new InvalidOperationException(
                "Unity project root is unavailable.");
    }
}
