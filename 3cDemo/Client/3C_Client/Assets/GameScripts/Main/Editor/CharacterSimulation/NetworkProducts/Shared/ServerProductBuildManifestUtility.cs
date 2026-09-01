using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class ServerProductBuildManifestUtility
    {
        public const string ManifestFileName = "ServerProductBuild.json";

        public static NetworkTestRuntimeArtifactResult Write(
            NetworkTestProductContext context,
            string serverDirectory,
            string candidateId,
            string roleId,
            string configurationIdentity,
            string productId,
            string executable,
            NetworkTestProductManifestField[] fields = null)
        {
            string root = Path.GetFullPath(serverDirectory);
            string executablePath = Path.Combine(root, executable);
            if (!File.Exists(executablePath))
                throw new InvalidOperationException($"Server product executable is missing: {executablePath}");
            context.Processes.Execute(
                    executablePath,
                    $"--write-server-product-manifest {NetworkTestExternalProcessExecutor.Quote(candidateId)}",
                    root)
                .RequireSuccess(productId);
            string path = Path.Combine(root, ManifestFileName);
            if (!File.Exists(path))
                throw new InvalidOperationException($"Server product did not write its manifest: {path}");
            ServerProductManifestIdentity read = JsonUtility.FromJson<ServerProductManifestIdentity>(
                File.ReadAllText(path, Encoding.UTF8));
            if (read == null || read.schemaVersion != 2 ||
                !string.Equals(read.candidateId, candidateId, StringComparison.Ordinal) ||
                !string.Equals(read.serverProductId, productId, StringComparison.Ordinal))
                throw new InvalidOperationException("Server product manifest identity validation failed.");
            return new NetworkTestRuntimeArtifactResult(
                roleId,
                NetworkTestRuntimeArtifactKind.ManagedExecutable,
                productId,
                NormalizeRelative(Path.GetRelativePath(context.ProductRoot, root)),
                executable,
                configurationIdentity,
                NormalizeRelative(Path.GetRelativePath(context.ProductRoot, path)),
                NetworkTestArtifactFileUtility.Sha256(path),
                fields ?? Array.Empty<NetworkTestProductManifestField>());
        }

        static string NormalizeRelative(string path)
        {
            string value = path.Replace('\\', '/');
            if (Path.IsPathRooted(value) || value.StartsWith("../", StringComparison.Ordinal))
                throw new InvalidOperationException("Server product manifest escaped the Product root.");
            return value;
        }

        [Serializable]
        sealed class ServerProductManifestIdentity
        {
            public int schemaVersion = -1;
            public string candidateId = string.Empty;
            public string serverProductId = string.Empty;
        }
    }
}
