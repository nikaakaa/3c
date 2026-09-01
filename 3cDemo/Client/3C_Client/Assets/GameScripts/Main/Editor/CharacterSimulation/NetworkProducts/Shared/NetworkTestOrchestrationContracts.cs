using System;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    [Serializable]
    internal sealed class NetworkTestProductBuildManifest
    {
        public int schemaVersion = -1;
        public string candidateId = string.Empty;
        public string candidateLabel = string.Empty;
        public string sourceCommit = string.Empty;
        public string sourceTreeHash = string.Empty;
        public string builtAtUtc = string.Empty;
        public string productId = string.Empty;
        public string programIdentity = string.Empty;
        public string pipelineIdentity = string.Empty;
        public string networkModelIdentity = string.Empty;
        public string runtimeTopologyIdentity = string.Empty;
        public NetworkTestRuntimeArtifactManifest[] artifacts = Array.Empty<NetworkTestRuntimeArtifactManifest>();
        public NetworkTestToolBundleManifest[] toolBundles = Array.Empty<NetworkTestToolBundleManifest>();
        public NetworkTestSessionPlanManifest sessionPlan = new NetworkTestSessionPlanManifest();
        public NetworkTestProductManifestField[] fields = Array.Empty<NetworkTestProductManifestField>();
        public NetworkTestProductManifestFile[] files = Array.Empty<NetworkTestProductManifestFile>();
    }

    [Serializable]
    internal sealed class NetworkTestRuntimeArtifactManifest
    {
        public string roleId = string.Empty;
        public string kind = string.Empty;
        public string productId = string.Empty;
        public string root = string.Empty;
        public string entryPoint = string.Empty;
        public string configurationIdentity = string.Empty;
        public string manifestPath = string.Empty;
        public string manifestHash = string.Empty;
        public NetworkTestProductManifestField[] fields = Array.Empty<NetworkTestProductManifestField>();
    }

    [Serializable]
    internal sealed class NetworkTestToolBundleManifest
    {
        public string toolId = string.Empty;
        public string toolVersion = string.Empty;
        public int contractVersion = -1;
        public string root = string.Empty;
        public string entryPoint = string.Empty;
        public string configurationIdentity = string.Empty;
        public string bundleHash = string.Empty;
    }

    [Serializable]
    internal sealed class NetworkTestSessionPlanManifest
    {
        public int schemaVersion = -1;
        public string adapterId = string.Empty;
        public string adapterPath = string.Empty;
        public string adapterHash = string.Empty;
        public string[] supportedSlotIds = Array.Empty<string>();
        public string[] allowedRunFields = Array.Empty<string>();
        public NetworkTestSessionRoleManifest[] roles = Array.Empty<NetworkTestSessionRoleManifest>();
        public string[] cleanupRoleIds = Array.Empty<string>();
    }

    [Serializable]
    internal sealed class NetworkTestSessionRoleManifest
    {
        public string roleId = string.Empty;
        public string launchSourceKind = string.Empty;
        public string launchSourceId = string.Empty;
        public bool required = false;
        public string visibility = string.Empty;
        public string readyCondition = string.Empty;
        public string[] dependsOnRoleIds = Array.Empty<string>();
        public string[] endpointKeys = Array.Empty<string>();
        public string windowRoleId = string.Empty;
    }

    [Serializable]
    internal sealed class NetworkTestSessionSlotCatalogDocument
    {
        public int schemaVersion = -1;
        public NetworkTestSessionSlotDocument[] slots = Array.Empty<NetworkTestSessionSlotDocument>();
    }

    [Serializable]
    internal sealed class NetworkTestSessionSlotDocument
    {
        public string slotId = string.Empty;
        public NetworkTestSessionEndpointDocument[] endpoints = Array.Empty<NetworkTestSessionEndpointDocument>();
        public NetworkTestSessionWindowDocument[] windows = Array.Empty<NetworkTestSessionWindowDocument>();
    }

    [Serializable]
    internal sealed class NetworkTestSessionEndpointDocument
    {
        public string key = string.Empty;
        public string address = string.Empty;
        public int port = -1;
    }

    [Serializable]
    internal sealed class NetworkTestSessionWindowDocument
    {
        public string roleId = string.Empty;
        public int x = -1;
        public int y = -1;
        public int width = -1;
        public int height = -1;
    }

    [Serializable]
    internal sealed class NetworkTestRunManifestDocument
    {
        public int schemaVersion = -1;
        public string runId = string.Empty;
        public string sessionId = string.Empty;
        public string candidateId = string.Empty;
        public string productId = string.Empty;
        public string candidateRoot = string.Empty;
        public string candidateManifestPath = string.Empty;
        public string candidateManifestHash = string.Empty;
        public string runtimeTopologyIdentity = string.Empty;
        public string slotId = string.Empty;
        public NetworkTestSessionEndpointDocument[] endpoints = Array.Empty<NetworkTestSessionEndpointDocument>();
        public NetworkTestSessionWindowDocument[] windows = Array.Empty<NetworkTestSessionWindowDocument>();
        public NetworkTestToolBundleManifest[] toolBundles = Array.Empty<NetworkTestToolBundleManifest>();
        public NetworkTestProductManifestFile[] configFiles = Array.Empty<NetworkTestProductManifestFile>();
        public NetworkTestRunProcessDocument[] processes = Array.Empty<NetworkTestRunProcessDocument>();
        public string runRoot = string.Empty;
    }

    [Serializable]
    internal sealed class NetworkTestRunStatusDocument
    {
        public int schemaVersion = -1;
        public string runId = string.Empty;
        public string state = string.Empty;
        public string message = string.Empty;
        public int orchestratorProcessId = -1;
        public NetworkTestRunProcessDocument[] processes = Array.Empty<NetworkTestRunProcessDocument>();
    }

    [Serializable]
    internal sealed class NetworkTestRunProcessDocument
    {
        public string roleId = string.Empty;
        public int processId = -1;
        public long processStartTimeUtcTicks = -1;
    }

    [Serializable]
    internal sealed class NetworkTestProductManifestField
    {
        public string key = string.Empty;
        public string value = string.Empty;
    }

    [Serializable]
    internal sealed class NetworkTestProductManifestFile
    {
        public string path = string.Empty;
        public long length = -1;
        public string sha256 = string.Empty;
    }
}
