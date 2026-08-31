using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ThirdPerson.Development.Gm;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class RollbackGmProductBuild
    {
        public const string ProfilePath = "Assets/Configs/Development/Gm/RollbackGmBuildProfile.asset";
        public const string Topology = "thirdperson.runtime-topology.deterministic-rollback.gm-relay-two-peers.v1";
        public const string RoleId = "development-gm-server";
        public const string ProductId = "thirdperson.server-product.development-gm";
        public const string Executable = "ThirdPerson.Development.Gm.Service.exe";
        public const string GmManifestPath = "Gm/GmServerManifest.json";
        public const string RelayManifestPath = "Server/RelayQueryManifest.json";
        public const string ConsoleManifestPath = "Gm/GmConsoleManifest.json";

        public static RollbackGmBuildProfile RequireProfile()
        {
            RollbackGmBuildProfile profile = NetworkTestProductAdapterUtility.RequireAsset<RollbackGmBuildProfile>(ProfilePath);
            profile.RequireValid();
            return profile;
        }

        public static NetworkTestRuntimeArtifactResult Publish(NetworkTestProductContext context, string root, string buildId, string sessionId)
        {
            RollbackGmBuildProfile profile = RequireProfile();
            string clientToken = CreateToken();
            string relayToken = CreateToken();
            GmServerManifest gm = profile.BuildServerManifest(buildId, sessionId, clientToken, relayToken);
            RelayQueryManifest relay = profile.BuildRelayManifest(buildId, sessionId, relayToken);
            GmClientManifest client = profile.BuildClientManifest(buildId, sessionId, clientToken);
            string directory = Path.Combine(root, "Gm");
            Directory.CreateDirectory(directory);
            string project = Path.Combine(context.RepositoryRoot, "3cDemo", "Server", "Products", "DevelopmentGm",
                "ThirdPerson.Development.Gm.Service.csproj");
            context.Processes.ExecuteDotNetBuild(ProductId,
                $"publish {NetworkTestExternalProcessExecutor.Quote(project)} --configuration Debug --output {NetworkTestExternalProcessExecutor.Quote(directory)}",
                context.RepositoryRoot);
            File.Copy(Path.Combine(context.RepositoryRoot, "3cDemo", "Tools", "DeterministicRollback", "RollbackGmProductRuntime.ps1"),
                Path.Combine(directory, "RollbackGmProductRuntime.ps1"), true);
            Write(root, GmManifestPath, gm);
            Write(root, RelayManifestPath, relay);
            Write(root, ConsoleManifestPath, client);
            if (!File.Exists(Path.Combine(directory, Executable)))
                throw new InvalidOperationException("GM 服务 executable 未发布。");
            string hash = NetworkTestArtifactFileUtility.Sha256(Path.Combine(root, GmManifestPath));
            return new NetworkTestRuntimeArtifactResult(RoleId, NetworkTestRuntimeArtifactKind.ManagedExecutable,
                ProductId, "Gm", Executable, hash, GmManifestPath, hash, new[]
                {
                    NetworkTestProductAdapterUtility.Field("endpoint", gm.http.Endpoint),
                    NetworkTestProductAdapterUtility.Field("relayQueryManifestHash", NetworkTestArtifactFileUtility.Sha256(Path.Combine(root, RelayManifestPath))),
                    NetworkTestProductAdapterUtility.Field("consoleManifestHash", NetworkTestArtifactFileUtility.Sha256(Path.Combine(root, ConsoleManifestPath)))
                });
        }

        public static void Validate(NetworkTestProductContext context, NetworkTestProductBuildManifest product, string sessionId)
        {
            RollbackGmBuildProfile profile = RequireProfile();
            NetworkTestRuntimeArtifactManifest artifact = NetworkTestProductAdapterUtility.RequireManagedArtifact(
                product, RoleId, ProductId, context.ProductRoot);
            if (artifact.entryPoint != "Gm/" + Executable || artifact.manifestPath != GmManifestPath ||
                artifact.configurationIdentity != artifact.manifestHash || product.artifacts.Length != 3)
                throw new InvalidOperationException("Rollback GM artifact 身份或产品数量不匹配。");
            GmServerManifest gm = Read<GmServerManifest>(context.ProductRoot, GmManifestPath);
            RelayQueryManifest relay = Read<RelayQueryManifest>(context.ProductRoot, RelayManifestPath);
            GmClientManifest client = Read<GmClientManifest>(context.ProductRoot, ConsoleManifestPath);
            NetworkTestArtifactFileUtility.RequireExactFile(
                Path.Combine(context.RepositoryRoot, "3cDemo", "Tools", "DeterministicRollback", "RollbackGmProductRuntime.ps1"),
                Path.Combine(context.ProductRoot, "Gm", "RollbackGmProductRuntime.ps1"));
            gm.RequireValid();
            relay.RequireValid();
            client.RequireValid();
            RequireEqual(gm, profile.BuildServerManifest(product.buildId, sessionId, gm.http.accessToken, gm.relayQueryToken));
            RequireEqual(relay, profile.BuildRelayManifest(product.buildId, sessionId, gm.relayQueryToken));
            RequireEqual(client, profile.BuildClientManifest(product.buildId, sessionId, gm.http.accessToken));
            RequireField(artifact, "relayQueryManifestHash", NetworkTestArtifactFileUtility.Sha256(Path.Combine(context.ProductRoot, RelayManifestPath)));
            RequireField(artifact, "consoleManifestHash", NetworkTestArtifactFileUtility.Sha256(Path.Combine(context.ProductRoot, ConsoleManifestPath)));
            RequireField(artifact, "endpoint", gm.http.Endpoint);
        }

        static void RequireField(NetworkTestRuntimeArtifactManifest artifact, string name, string expected)
        {
            foreach (NetworkTestProductManifestField field in artifact.fields)
            {
                if (field.key == name && field.value == expected)
                    return;
            }
            throw new InvalidOperationException($"GM artifact 字段 '{name}' 不匹配。");
        }

        static void RequireEqual<T>(T actual, T expected)
        {
            if (!string.Equals(JsonUtility.ToJson(actual), JsonUtility.ToJson(expected), StringComparison.Ordinal))
                throw new InvalidOperationException("GM 发布配置与正式 Profile、Build 或 Session 不匹配。");
        }

        static void Write(string root, string path, object value)
        {
            string target = Path.Combine(root, path);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.WriteAllText(target, JsonUtility.ToJson(value, true), new UTF8Encoding(false));
        }

        static T Read<T>(string root, string path) => JsonUtility.FromJson<T>(File.ReadAllText(Path.Combine(root, path), Encoding.UTF8));

        static string CreateToken()
        {
            var bytes = new byte[32];
            using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
                generator.GetBytes(bytes);
            return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
