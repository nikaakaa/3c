using System;
using ThirdPerson.NetworkTest.Contracts;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdPerson.Development.Gm;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class RollbackGmProductBuild
    {
        public const string ProfilePath = "Assets/Configs/Development/Gm/RollbackGmToolProfile.asset";
        public const string Topology = "thirdperson.runtime-topology.deterministic-rollback.gm-relay-two-peers.v1";
        public const string RoleId = "development-gm-server";
        public const string ProductId = "thirdperson.server-product.development-gm";
        public const string Executable = "ThirdPerson.Development.Gm.Service.exe";
        public const string ToolId = GmToolCatalog.ToolId;
        public const string ToolVersion = GmToolCatalog.ToolVersion;
        public const string ToolManifestPath = "Gm/GmToolManifest.json";
        public const string ToolPolicyPath = "Gm/GmToolPolicy.json";

        public static RollbackGmToolProfile RequireProfile()
        {
            RollbackGmToolProfile profile =
                NetworkTestProductAdapterUtility.RequireAsset<RollbackGmToolProfile>(ProfilePath);
            profile.RequireValid();
            return profile;
        }

        public static NetworkTestRuntimeArtifactResult Publish(NetworkTestProductContext context, string root)
        {
            RollbackGmToolProfile profile = RequireProfile();
            string directory = Path.Combine(root, "Gm");
            Directory.CreateDirectory(directory);
            string project = Path.Combine(
                context.RepositoryRoot,
                "3cDemo",
                "Server",
                "Products",
                "DevelopmentGm",
                "ThirdPerson.Development.Gm.Service.csproj");
            context.Processes.ExecuteDotNetBuild(
                ProductId,
                $"publish {NetworkTestExternalProcessExecutor.Quote(project)} --configuration Debug --output {NetworkTestExternalProcessExecutor.Quote(directory)}",
                context.RepositoryRoot);
            string executable = Path.Combine(directory, Executable);
            if (!File.Exists(executable))
                throw new InvalidOperationException("GM Service executable was not published.");
            string toolManifest = Path.Combine(root, ToolManifestPath.Replace('/', Path.DirectorySeparatorChar));
            context.Processes.Execute(
                    executable,
                    $"--write-tool-manifest {NetworkTestExternalProcessExecutor.Quote(toolManifest)}",
                    directory)
                .RequireSuccess(ProductId);
            GmToolManifest tool = Read<GmToolManifest>(toolManifest);
            tool.RequireValid();
            GmToolPolicy policy = profile.BuildPolicy();
            policy.RequireValid();
            Write(root, ToolPolicyPath, policy);
            string toolHash = NetworkTestArtifactFileUtility.Sha256(toolManifest);
            string policyHash = NetworkTestArtifactFileUtility.Sha256(Path.Combine(root, ToolPolicyPath));
            string configuration = StableHash.Compute(
                "rollback-gm-tool-configuration/1",
                toolHash,
                policyHash).Value;
            return new NetworkTestRuntimeArtifactResult(
                RoleId,
                NetworkTestRuntimeArtifactKind.ManagedExecutable,
                ProductId,
                "Gm",
                Executable,
                configuration,
                ToolManifestPath,
                toolHash,
                new[]
                {
                    NetworkTestProductAdapterUtility.Field("toolManifestHash", toolHash),
                    NetworkTestProductAdapterUtility.Field("toolPolicyHash", policyHash),
                    NetworkTestProductAdapterUtility.Field("commandCatalogHash", tool.commandCatalogHash)
                });
        }

        public static IReadOnlyList<NetworkTestToolBundleManifest> BuildToolBundles(NetworkTestProductContext context)
        {
            string toolManifestPath = Path.Combine(context.ProductRoot, ToolManifestPath.Replace('/', Path.DirectorySeparatorChar));
            GmToolManifest tool = Read<GmToolManifest>(toolManifestPath);
            tool.RequireValid();
            return new[]
            {
                NetworkTestToolBundlePublisher.BuildBundle(
                    context.ProductRoot,
                    ToolId,
                    ToolVersion,
                    "Gm",
                    "Gm/" + Executable,
                    NetworkTestArtifactFileUtility.Sha256(toolManifestPath))
            };
        }

        public static void Validate(NetworkTestProductContext context, NetworkTestProductBuildManifest product)
        {
            NetworkTestRuntimeArtifactManifest artifact = NetworkTestProductAdapterUtility.RequireManagedArtifact(
                product,
                RoleId,
                ProductId,
                context.ProductRoot);
            if (artifact.entryPoint != "Gm/" + Executable || artifact.manifestPath != ToolManifestPath ||
                product.artifacts.Length != 3)
                throw new InvalidOperationException("Rollback GM artifact identity or product count is invalid.");
            string toolPath = Path.Combine(context.ProductRoot, ToolManifestPath.Replace('/', Path.DirectorySeparatorChar));
            string policyPath = Path.Combine(context.ProductRoot, ToolPolicyPath.Replace('/', Path.DirectorySeparatorChar));
            GmToolManifest tool = Read<GmToolManifest>(toolPath);
            GmToolPolicy policy = Read<GmToolPolicy>(policyPath);
            tool.RequireValid();
            policy.RequireValid();
            RequireField(artifact, "toolManifestHash", NetworkTestArtifactFileUtility.Sha256(toolPath));
            RequireField(artifact, "toolPolicyHash", NetworkTestArtifactFileUtility.Sha256(policyPath));
            RequireField(artifact, "commandCatalogHash", tool.commandCatalogHash);
        }

        static void RequireField(NetworkTestRuntimeArtifactManifest artifact, string name, string expected)
        {
            foreach (NetworkTestProductManifestField field in artifact.fields)
            {
                if (field.key == name && field.value == expected)
                    return;
            }
            throw new InvalidOperationException($"GM artifact field '{name}' is invalid.");
        }

        static void Write(string root, string path, object value)
        {
            string target = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.WriteAllText(target, JsonUtility.ToJson(value, true), new UTF8Encoding(false));
        }

        static T Read<T>(string path) => JsonUtility.FromJson<T>(File.ReadAllText(path, Encoding.UTF8));
    }
}
