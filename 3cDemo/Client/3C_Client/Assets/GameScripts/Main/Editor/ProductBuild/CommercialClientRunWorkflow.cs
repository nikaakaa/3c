using System;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.Build;

namespace ThirdPersonCharacter.Editor.ProductBuild
{
    internal readonly struct CommercialPublishedPlayer
    {
        public CommercialPublishedPlayer(string root, string entryPath)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            EntryPath = entryPath ?? throw new ArgumentNullException(nameof(entryPath));
        }

        public string Root { get; }
        public string EntryPath { get; }
    }

    internal static class CommercialClientRunWorkflow
    {
        public static void Run(BuildTarget target, string clientBuildVersion)
        {
            if (target != BuildTarget.StandaloneWindows64 &&
                target != BuildTarget.StandaloneLinux64 &&
                target != BuildTarget.StandaloneOSX)
            {
                throw new BuildFailedException($"当前平台不支持从Editor直接运行正式Player：{target}");
            }

            CommercialPublishedPlayer player = CommercialClientBuildWorkflow.ValidatePublishedPlayer(
                target,
                clientBuildVersion);
            var startInfo = new ProcessStartInfo
            {
                FileName = player.EntryPath,
                WorkingDirectory = player.Root,
                UseShellExecute = true
            };
            using Process process = Process.Start(startInfo) ??
                throw new BuildFailedException($"正式Player启动失败：{player.EntryPath}");
        }
    }
}
