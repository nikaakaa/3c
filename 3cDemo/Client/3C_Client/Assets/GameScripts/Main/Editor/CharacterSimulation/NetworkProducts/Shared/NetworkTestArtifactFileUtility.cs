using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class NetworkTestArtifactFileUtility
    {
        public static string Sha256(string path)
        {
            if (!File.Exists(path))
                throw new InvalidOperationException($"Product file is missing: {path}");
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2")));
        }

        public static void RequireExactFile(string source, string published)
        {
            if (!File.Exists(source) || !File.Exists(published) ||
                !string.Equals(Sha256(source), Sha256(published), StringComparison.Ordinal))
                throw new InvalidOperationException($"Published runtime artifact is not exact-byte equal to its source: {published}");
        }
    }
}
