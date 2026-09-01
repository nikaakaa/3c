using System;
using ThirdPerson.NetworkTest.Contracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class NetworkTestToolPublication
    {
        public NetworkTestToolPublication(
            NetworkTestToolBundleManifest[] toolBundles,
            NetworkTestSessionPlanManifest sessionPlan)
        {
            ToolBundles = toolBundles ?? throw new ArgumentNullException(nameof(toolBundles));
            SessionPlan = sessionPlan ?? throw new ArgumentNullException(nameof(sessionPlan));
        }

        public NetworkTestToolBundleManifest[] ToolBundles { get; }
        public NetworkTestSessionPlanManifest SessionPlan { get; }
    }

    internal static class NetworkTestToolBundlePublisher
    {
        public const string OrchestratorToolId = "thirdperson.network-test-orchestrator";
        public const string OrchestratorToolVersion = "1";
        public const string SlotProfilePath =
            "Assets/Configs/Development/NetworkTest/NetworkTestSessionSlotProfile.asset";
        const string OrchestratorRoot = "Tools/Orchestrator";
        const string AdapterRoot = "Tools/Adapter";
        const string OrchestratorExecutable = "ThirdPerson.NetworkTest.Orchestrator.exe";

        public static NetworkTestToolPublication Publish(
            NetworkTestProductContext context,
            NetworkTestProductDescriptor descriptor)
        {
            string orchestratorRoot = Path.Combine(context.ProductRoot, OrchestratorRoot.Replace('/', Path.DirectorySeparatorChar));
            string project = Path.Combine(
                context.RepositoryRoot,
                "Tools",
                "ThirdPersonNetworkTest",
                "ThirdPerson.NetworkTest.Orchestrator.csproj");
            context.Processes.ExecuteDotNetBuild(
                OrchestratorToolId,
                $"publish {NetworkTestExternalProcessExecutor.Quote(project)} --configuration Debug --output {NetworkTestExternalProcessExecutor.Quote(orchestratorRoot)}",
                context.RepositoryRoot);
            string executable = Path.Combine(orchestratorRoot, OrchestratorExecutable);
            if (!File.Exists(executable))
                throw new InvalidOperationException("Network Test Orchestrator executable was not published.");

            NetworkTestSessionSlotProfile slotProfile =
                AssetDatabase.LoadAssetAtPath<NetworkTestSessionSlotProfile>(SlotProfilePath);
            if (!slotProfile)
                throw new InvalidOperationException($"Network Test Session Slot Profile is missing: {SlotProfilePath}");
            NetworkTestSessionSlotCatalogDocument slotCatalog = slotProfile.BuildCatalog();
            RequireSlotContract(descriptor, slotCatalog);
            string slotCatalogPath = Path.Combine(orchestratorRoot, "SessionSlots.json");
            File.WriteAllText(
                slotCatalogPath,
                JsonUtility.ToJson(slotCatalog, true),
                new UTF8Encoding(false));

            string sourceAdapter = Path.Combine(
                context.RepositoryRoot,
                descriptor.LaunchScriptRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(sourceAdapter))
                throw new InvalidOperationException($"Network Test Session adapter is missing: {sourceAdapter}");
            string adapterRoot = Path.Combine(context.ProductRoot, AdapterRoot.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(adapterRoot);
            string adapterName = Path.GetFileName(sourceAdapter);
            string adapterPath = Path.Combine(adapterRoot, adapterName);
            File.Copy(sourceAdapter, adapterPath, false);
            foreach (string support in Directory.GetFiles(
                         Path.GetDirectoryName(sourceAdapter) ?? context.RepositoryRoot,
                         "Assert-*.ps1",
                         SearchOption.TopDirectoryOnly))
            {
                File.Copy(support, Path.Combine(adapterRoot, Path.GetFileName(support)), false);
            }
            string commonAssert = Path.Combine(
                context.RepositoryRoot,
                "3cDemo",
                "Tools",
                "NetworkTest",
                "Assert-NetworkTestProductBuild.ps1");
            string commonTarget = Path.Combine(adapterRoot, Path.GetFileName(commonAssert));
            if (!File.Exists(commonTarget))
                File.Copy(commonAssert, commonTarget, false);

            var bundles = new List<NetworkTestToolBundleManifest>
            {
                BuildBundle(
                    context.ProductRoot,
                    OrchestratorToolId,
                    OrchestratorToolVersion,
                    OrchestratorRoot,
                    $"{OrchestratorRoot}/{OrchestratorExecutable}",
                    NetworkTestArtifactFileUtility.Sha256(slotCatalogPath)),
                BuildBundle(
                    context.ProductRoot,
                    descriptor.ProductId + ".session-adapter",
                    "1",
                    AdapterRoot,
                    $"{AdapterRoot}/{adapterName}",
                    NetworkTestArtifactFileUtility.Sha256(adapterPath))
            };
            IReadOnlyList<NetworkTestToolBundleManifest> additional = descriptor.AdditionalToolBundles?.Invoke(context);
            if (additional != null)
                bundles.AddRange(additional);
            NetworkTestToolBundleManifest[] validated = RequireBundles(context.ProductRoot, bundles);
            return new NetworkTestToolPublication(
                validated,
                new NetworkTestSessionPlanManifest
                {
                    schemaVersion = 1,
                    adapterId = descriptor.ProductId + ".session-adapter/1",
                    adapterPath = $"{AdapterRoot}/{adapterName}",
                    adapterHash = NetworkTestArtifactFileUtility.Sha256(adapterPath),
                    supportedSlotIds = descriptor.SupportedSlotIds.ToArray(),
                    allowedRunFields = new[]
                    {
                        "candidateId",
                        "candidateManifestHash",
                        "candidateManifestPath",
                        "candidateRoot",
                        "endpoints",
                        "productId",
                        "runId",
                        "runRoot",
                        "runtimeTopologyIdentity",
                        "sessionId",
                        "slotId",
                        "toolBundles",
                        "windows"
                    },
                    roles = descriptor.SessionRoles.ToArray(),
                    cleanupRoleIds = descriptor.SessionRoles.Reverse().Select(value => value.roleId).ToArray()
                });
        }

        internal static NetworkTestToolBundleManifest BuildBundle(
            string candidateRoot,
            string toolId,
            string toolVersion,
            string root,
            string entryPoint,
            string configurationIdentity) => new NetworkTestToolBundleManifest
        {
            toolId = toolId,
            toolVersion = toolVersion,
            contractVersion = 1,
            root = root,
            entryPoint = entryPoint,
            configurationIdentity = configurationIdentity,
            bundleHash = ComputeDirectoryHash(candidateRoot, root)
        };

        static NetworkTestToolBundleManifest[] RequireBundles(
            string candidateRoot,
            IEnumerable<NetworkTestToolBundleManifest> source)
        {
            NetworkTestToolBundleManifest[] values = source?.ToArray() ?? Array.Empty<NetworkTestToolBundleManifest>();
            Array.Sort(values, (left, right) => string.CompareOrdinal(left?.toolId, right?.toolId));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (NetworkTestToolBundleManifest value in values)
            {
                if (value == null || string.IsNullOrWhiteSpace(value.toolId) ||
                    string.IsNullOrWhiteSpace(value.toolVersion) || value.contractVersion <= 0 ||
                    !ids.Add(value.toolId) || !File.Exists(RequireContained(candidateRoot, value.entryPoint)) ||
                    !string.Equals(value.bundleHash, ComputeDirectoryHash(candidateRoot, value.root), StringComparison.Ordinal))
                    throw new InvalidOperationException("Network Test Tool Bundle identity is invalid.");
            }
            return values;
        }

        static void RequireSlotContract(
            NetworkTestProductDescriptor descriptor,
            NetworkTestSessionSlotCatalogDocument catalog)
        {
            NetworkTestSessionSlotDocument[] slots = catalog.slots ?? Array.Empty<NetworkTestSessionSlotDocument>();
            foreach (string slotId in descriptor.SupportedSlotIds)
            {
                NetworkTestSessionSlotDocument slot = slots.SingleOrDefault(value =>
                    value != null && string.Equals(value.slotId, slotId, StringComparison.Ordinal)) ??
                    throw new InvalidOperationException($"Network Test Session Slot ''{slotId}'' is not installed.");
                var endpointKeys = new HashSet<string>(
                    (slot.endpoints ?? Array.Empty<NetworkTestSessionEndpointDocument>()).Select(value => value.key),
                    StringComparer.Ordinal);
                var windowRoles = new HashSet<string>(
                    (slot.windows ?? Array.Empty<NetworkTestSessionWindowDocument>()).Select(value => value.roleId),
                    StringComparer.Ordinal);
                foreach (NetworkTestSessionRoleManifest role in descriptor.SessionRoles)
                {
                    if (role.endpointKeys.Any(key => !endpointKeys.Contains(key)) ||
                        role.windowRoleId.Length > 0 && !windowRoles.Contains(role.windowRoleId))
                        throw new InvalidOperationException(
                            $"Network Test Session Slot ''{slotId}'' does not satisfy role ''{role.roleId}''.");
                }
            }
        }

        internal static string ComputeDirectoryHash(string candidateRoot, string relativeRoot)
        {
            string root = RequireContained(candidateRoot, relativeRoot);
            if (!Directory.Exists(root))
                throw new InvalidOperationException($"Network Test Tool Bundle root is missing: {relativeRoot}");
            string[] entries = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                .Select(path =>
                    Path.GetRelativePath(root, path).Replace('\\', '/') + ":" + NetworkTestArtifactFileUtility.Sha256(path))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            return StableHash.Compute("network-test-tool-bundle/1", string.Join("|", entries)).Value;
        }

        static string RequireContained(string candidateRoot, string relative)
        {
            string root = Path.GetFullPath(candidateRoot);
            string full = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Network Test Tool Bundle path escaped Candidate Root.");
            return full;
        }
    }
}
