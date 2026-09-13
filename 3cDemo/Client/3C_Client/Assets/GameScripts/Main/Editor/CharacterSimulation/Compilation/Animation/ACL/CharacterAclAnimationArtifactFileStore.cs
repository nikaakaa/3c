using System;
using System.IO;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL
{
    internal static class CharacterAclAnimationArtifactFileStore
    {
        internal static string ToProjectFilePath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                throw new ArgumentException(
                    "ACL artifact asset path is required.",
                    nameof(assetPath));
            string normalized = assetPath.Replace('\\', '/').TrimEnd('/');
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Unity project root is unavailable.");
            return Path.GetFullPath(Path.Combine(projectRoot, normalized));
        }
    }
}
