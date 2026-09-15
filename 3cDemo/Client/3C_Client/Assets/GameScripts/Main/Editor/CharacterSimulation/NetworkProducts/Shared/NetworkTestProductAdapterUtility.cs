using System;
using ThirdPerson.NetworkTest.Contracts;
using System.IO;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class NetworkTestProductAdapterUtility
    {
        public static string ProgramIdentity(CharacterSimulationProgram program) =>
            $"program={program.Manifest.ProgramId.Value};compiler={program.Manifest.CompilerVersion};operations={program.Manifest.OperationSetVersion};numeric={program.Manifest.NumericProfile.Id.Value};abi={program.Manifest.NumericProfile.AbiVersion.Value};programHash={program.ProgramHash};layoutHash={program.LayoutHash};stateCodec={CharacterSimulationStateCodec.CodecIdentity};source={program.Manifest.SourceRevision.Value}";

        public static NetworkTestProductManifestField Field(string key, string value) =>
            new NetworkTestProductManifestField { key = key, value = value };

        public static NetworkTestSessionRoleManifest SessionRole(
            string roleId,
            string launchSourceKind,
            string launchSourceId,
            bool required,
            string visibility,
            string readyCondition,
            string[] dependsOnRoleIds,
            string[] endpointKeys,
            string windowRoleId = "") => new NetworkTestSessionRoleManifest
        {
            roleId = roleId,
            launchSourceKind = launchSourceKind,
            launchSourceId = launchSourceId,
            required = required,
            visibility = visibility,
            readyCondition = readyCondition,
            dependsOnRoleIds = dependsOnRoleIds ?? Array.Empty<string>(),
            endpointKeys = endpointKeys ?? Array.Empty<string>(),
            windowRoleId = windowRoleId ?? string.Empty
        };

        public static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            return value ? value : throw new InvalidOperationException($"Network Test Product asset is missing: {path}");
        }

        public static void PublishFantasyConfig(string serverProject, string serverDirectory)
        {
            string source = Path.Combine(Path.GetDirectoryName(serverProject) ?? string.Empty, "Fantasy.config");
            string target = Path.Combine(serverDirectory, "Fantasy.config");
            File.Copy(source, target, true);
            NetworkTestArtifactFileUtility.RequireExactFile(source, target);
        }

        public static NetworkTestRuntimeArtifactManifest RequireManagedArtifact(
            NetworkTestProductBuildManifest manifest,
            string roleId,
            string productId,
            string productRoot)
        {
            NetworkTestRuntimeArtifactManifest artifact = NetworkTestProductBuildWorkflow.RequireArtifact(manifest, roleId);
            if (!string.Equals(artifact.kind, NetworkTestRuntimeArtifactKind.ManagedExecutable.ToString(), StringComparison.Ordinal) ||
                !string.Equals(artifact.productId, productId, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(artifact.manifestPath) ||
                string.IsNullOrWhiteSpace(artifact.manifestHash))
                throw new InvalidOperationException("Network Test Product managed artifact manifest identity is invalid.");
            string path = Path.Combine(productRoot, artifact.manifestPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path) || !string.Equals(
                    NetworkTestArtifactFileUtility.Sha256(path),
                    artifact.manifestHash,
                    StringComparison.Ordinal))
                throw new InvalidOperationException("Network Test Product managed artifact manifest hash is invalid.");
            return artifact;
        }
    }
}
