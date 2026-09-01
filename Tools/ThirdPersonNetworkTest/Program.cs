using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using ThirdPersonCharacter.Editor.CharacterSimulation;

namespace ThirdPerson.NetworkTest.Orchestrator;

static class Program
{
    const int CandidateSchema = 3;
    const int SlotSchema = 1;
    const int RunSchema = 1;
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = false,
        WriteIndented = true
    };

    static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 5 && args[0] == "start" && args[1] == "--candidate" && args[3] == "--slot")
            {
                await StartAsync(args[2], args[4]);
                return 0;
            }
            if (args.Length == 3 && args[0] == "stop" && args[1] == "--run")
            {
                Stop(args[2]);
                return 0;
            }
            if (args.Length == 3 && args[0] == "validate" && args[1] == "--candidate")
            {
                Validate(args[2]);
                return 0;
            }
            throw new ArgumentException(
                "Usage: start --candidate <manifest> --slot <slot-id> | stop --run <run-root> | validate --candidate <manifest>");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    static async Task StartAsync(string candidateManifestPath, string slotId)
    {
        string manifestPath = Path.GetFullPath(candidateManifestPath);
        string candidateRoot = Path.GetDirectoryName(manifestPath) ?? throw new InvalidDataException("Candidate manifest has no root.");
        NetworkTestProductBuildManifest candidate = Read<NetworkTestProductBuildManifest>(manifestPath);
        RequireCandidate(candidateRoot, manifestPath, candidate);
        NetworkTestToolBundleManifest orchestrator = RequireTool(candidate, "thirdperson.network-test-orchestrator");
        NetworkTestSessionSlotCatalogDocument catalog = Read<NetworkTestSessionSlotCatalogDocument>(
            RequireContained(candidateRoot, Path.Combine(orchestrator.root, "SessionSlots.json")));
        if (catalog.schemaVersion != SlotSchema)
            throw new InvalidDataException("Session Slot Catalog schema is invalid.");
        NetworkTestSessionSlotDocument slot = (catalog.slots ?? Array.Empty<NetworkTestSessionSlotDocument>())
            .SingleOrDefault(value => value != null && value.slotId == slotId) ??
            throw new InvalidDataException($"Session Slot '{slotId}' is not installed.");
        if (!(candidate.sessionPlan?.supportedSlotIds ?? Array.Empty<string>()).Contains(slotId, StringComparer.Ordinal))
            throw new InvalidDataException($"Candidate does not support Session Slot '{slotId}'.");
        RequireSlot(slot);

        string networkRoot = RequireNetworkRoot(candidateRoot);
        string productDirectory = new DirectoryInfo(Path.GetDirectoryName(candidateRoot) ?? string.Empty).Name;
        string runId = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";
        string runRoot = Path.GetFullPath(Path.Combine(networkRoot, "RunLogs", productDirectory, runId));
        string leaseRoot = Path.Combine(networkRoot, "RunLogs", ".slots");
        Directory.CreateDirectory(leaseRoot);
        string leasePath = Path.Combine(leaseRoot, slot.slotId + ".lease");
        FileStream slotLease = AcquireSlotLease(leasePath, runId, candidate.candidateId, slot.slotId);
        try
        {
            Directory.CreateDirectory(runRoot);
            Directory.CreateDirectory(Path.Combine(runRoot, "Config"));
            Directory.CreateDirectory(Path.Combine(runRoot, "Logs"));
            string runManifestPath = Path.Combine(runRoot, "RunManifest.json");
            string statusPath = Path.Combine(runRoot, "RunStatus.json");
            string stopPath = Path.Combine(runRoot, "Stop.request");
            string processPath = Path.Combine(runRoot, "Processes.json");
            var run = new NetworkTestRunManifestDocument
            {
                schemaVersion = RunSchema,
                runId = runId,
                sessionId = $"{candidate.candidateId}.{runId}",
                candidateId = candidate.candidateId,
                productId = candidate.productId,
                candidateRoot = candidateRoot,
                candidateManifestPath = manifestPath,
                candidateManifestHash = Sha256(manifestPath),
                runtimeTopologyIdentity = candidate.runtimeTopologyIdentity,
                slotId = slot.slotId,
                endpoints = slot.endpoints,
                windows = slot.windows,
                toolBundles = candidate.toolBundles,
                runRoot = runRoot
            };
            Write(runManifestPath, run);
            Write(statusPath, Status(runId, "Starting", string.Empty, Array.Empty<NetworkTestRunProcessDocument>()));

            NetworkTestRunProcessDocument[] processes = Array.Empty<NetworkTestRunProcessDocument>();
            using var job = new WindowsProcessJob();
            try
            {
                string adapterPath = RequireContained(candidateRoot, candidate.sessionPlan.adapterPath);
                if (!File.Exists(adapterPath) || Sha256(adapterPath) != candidate.sessionPlan.adapterHash)
                    throw new InvalidDataException("Candidate Session adapter identity is invalid.");
                RequirePortsAvailable(slot.endpoints);

                using Process adapter = StartAdapter(adapterPath, runManifestPath, runRoot);
                job.Add(adapter);
                await adapter.WaitForExitAsync();
                if (adapter.ExitCode != 0)
                    throw new InvalidOperationException($"Session adapter failed with exit code {adapter.ExitCode}.");

                processes = File.Exists(processPath)
                    ? Read<NetworkTestRunProcessDocument[]>(processPath)
                    : Array.Empty<NetworkTestRunProcessDocument>();
                RequireProcesses(processes);
                Write(statusPath, Status(runId, "Running", string.Empty, processes));
                while (!File.Exists(stopPath))
                {
                    if (processes.All(HasExited))
                    {
                        Write(statusPath, Status(runId, "Completed", string.Empty, processes));
                        return;
                    }
                    await Task.Delay(500);
                }
                Write(statusPath, Status(runId, "Stopping", string.Empty, processes));
                job.Dispose();
                await RequireStoppedAsync(processes);
                Write(statusPath, Status(runId, "Completed", string.Empty, processes));
            }
            catch (Exception exception)
            {
                Write(statusPath, Status(runId, "Faulted", exception.Message, processes));
                throw;
            }
        }
        finally
        {
            slotLease.Dispose();
            DeleteOwnedLease(leasePath, runId);
        }
    }

    static void Stop(string runRoot)
    {
        string root = Path.GetFullPath(runRoot);
        NetworkTestRunManifestDocument run = Read<NetworkTestRunManifestDocument>(Path.Combine(root, "RunManifest.json"));
        if (run.schemaVersion != RunSchema || !string.Equals(Path.GetFullPath(run.runRoot), root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Run identity is invalid.");
        File.WriteAllText(Path.Combine(root, "Stop.request"), run.runId);
    }

    static void Validate(string candidateManifestPath)
    {
        string manifestPath = Path.GetFullPath(candidateManifestPath);
        string candidateRoot = Path.GetDirectoryName(manifestPath) ??
            throw new InvalidDataException("Candidate manifest has no root.");
        NetworkTestProductBuildManifest candidate = Read<NetworkTestProductBuildManifest>(manifestPath);
        RequireCandidate(candidateRoot, manifestPath, candidate);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            candidate.candidateId,
            candidate.productId,
            candidate.sourceCommit,
            candidate.sourceTreeHash,
            candidate.builtAtUtc,
            candidate.programIdentity,
            candidate.pipelineIdentity,
            tools = candidate.toolBundles.Select(value => $"{value.toolId}/{value.toolVersion}").ToArray(),
            slots = candidate.sessionPlan.supportedSlotIds
        }, JsonOptions));
    }

    static NetworkTestRunStatusDocument Status(
        string runId,
        string state,
        string message,
        NetworkTestRunProcessDocument[] processes) => new()
    {
        schemaVersion = RunSchema,
        runId = runId,
        state = state,
        message = message,
        orchestratorProcessId = Environment.ProcessId,
        processes = processes
    };

    static Process StartAdapter(string path, string runManifestPath, string runRoot)
    {
        string output = Path.Combine(runRoot, "Logs", "adapter.stdout.log");
        string error = Path.Combine(runRoot, "Logs", "adapter.stderr.log");
        var info = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File {Quote(path)} -RunManifest {Quote(runManifestPath)}",
            WorkingDirectory = Path.GetDirectoryName(path) ?? runRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        Process process = Process.Start(info) ?? throw new InvalidOperationException("Session adapter could not start.");
        _ = PumpAsync(process.StandardOutput, output);
        _ = PumpAsync(process.StandardError, error);
        return process;
    }

    static async Task PumpAsync(StreamReader reader, string path)
    {
        await using var writer = new StreamWriter(path, false);
        while (await reader.ReadLineAsync() is { } line)
            await writer.WriteLineAsync(line);
    }

    static void RequireCandidate(
        string candidateRoot,
        string manifestPath,
        NetworkTestProductBuildManifest candidate)
    {
        if (candidate.schemaVersion != CandidateSchema ||
            string.IsNullOrWhiteSpace(candidate.candidateId) ||
            string.IsNullOrWhiteSpace(candidate.candidateLabel) ||
            string.IsNullOrWhiteSpace(candidate.productId) ||
            !string.Equals(new DirectoryInfo(candidateRoot).Name, candidate.candidateId, StringComparison.Ordinal) ||
            !IsLowerHex(candidate.sourceCommit, 40) ||
            !IsLowerHex(candidate.sourceTreeHash, 40) ||
            !string.Equals(candidate.candidateId, $"{candidate.candidateLabel}-{candidate.sourceCommit[..12]}",
                StringComparison.Ordinal) ||
            candidate.sessionPlan == null ||
            candidate.sessionPlan.schemaVersion != 1)
            throw new InvalidDataException("Candidate identity is invalid.");
        NetworkTestProductManifestFile[] files = candidate.files ?? Array.Empty<NetworkTestProductManifestFile>();
        var declaredPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (NetworkTestProductManifestFile file in files)
        {
            if (file == null || string.IsNullOrWhiteSpace(file.path) || !declaredPaths.Add(file.path))
                throw new InvalidDataException("Candidate exact closure contains an invalid or duplicate path.");
            string path = RequireContained(candidateRoot, file.path);
            var info = new FileInfo(path);
            if (!info.Exists || info.Length != file.length || Sha256(path) != file.sha256)
                throw new InvalidDataException($"Candidate exact closure mismatch: {file.path}");
        }
        string[] actualPaths = Directory.GetFiles(candidateRoot, "*", SearchOption.AllDirectories)
            .Where(path => !string.Equals(Path.GetFullPath(path), Path.GetFullPath(manifestPath), StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(candidateRoot, path).Replace('\\', '/'))
            .ToArray();
        if (actualPaths.Length != files.Length || actualPaths.Any(path => !declaredPaths.Contains(path)))
            throw new InvalidDataException("Candidate exact closure count is invalid.");

        NetworkTestToolBundleManifest[] bundles = candidate.toolBundles ?? Array.Empty<NetworkTestToolBundleManifest>();
        if (bundles.Length < 2 || bundles.Any(value => value == null) ||
            bundles.Select(value => value.toolId).Distinct(StringComparer.Ordinal).Count() != bundles.Length)
            throw new InvalidDataException("Candidate Tool Bundle catalog is invalid.");
        foreach (NetworkTestToolBundleManifest bundle in bundles)
        {
            string entryPoint = RequireContained(candidateRoot, bundle.entryPoint);
            if (string.IsNullOrWhiteSpace(bundle.toolId) || string.IsNullOrWhiteSpace(bundle.toolVersion) ||
                bundle.contractVersion != 1 || string.IsNullOrWhiteSpace(bundle.configurationIdentity) ||
                !File.Exists(entryPoint) || ComputeDirectoryHash(candidateRoot, bundle.root) != bundle.bundleHash)
                throw new InvalidDataException($"Candidate Tool Bundle '{bundle.toolId}' is invalid.");
        }

        string[] adapterIdentity = candidate.sessionPlan.adapterId?.Split('/') ?? Array.Empty<string>();
        if (adapterIdentity.Length != 2 || string.IsNullOrWhiteSpace(candidate.sessionPlan.adapterPath) ||
            string.IsNullOrWhiteSpace(candidate.sessionPlan.adapterHash) ||
            candidate.sessionPlan.supportedSlotIds == null || candidate.sessionPlan.supportedSlotIds.Length == 0 ||
            candidate.sessionPlan.supportedSlotIds.Distinct(StringComparer.Ordinal).Count() !=
            candidate.sessionPlan.supportedSlotIds.Length)
            throw new InvalidDataException("Candidate Session Plan is invalid.");
        NetworkTestToolBundleManifest adapterBundle = RequireTool(candidate, adapterIdentity[0]);
        if (adapterBundle.toolVersion != adapterIdentity[1] || adapterBundle.entryPoint != candidate.sessionPlan.adapterPath ||
            Sha256(RequireContained(candidateRoot, candidate.sessionPlan.adapterPath)) != candidate.sessionPlan.adapterHash)
            throw new InvalidDataException("Candidate Session adapter identity is invalid.");

        NetworkTestToolBundleManifest orchestrator = RequireTool(candidate, "thirdperson.network-test-orchestrator");
        string catalogPath = RequireContained(candidateRoot, Path.Combine(orchestrator.root, "SessionSlots.json"));
        if (orchestrator.toolVersion != "1" || Path.GetFileName(orchestrator.entryPoint) !=
            "ThirdPerson.NetworkTest.Orchestrator.exe" || Sha256(catalogPath) != orchestrator.configurationIdentity)
            throw new InvalidDataException("Candidate Orchestrator identity is invalid.");
        string currentExecutable = Environment.ProcessPath ?? string.Empty;
        if (!string.Equals(Path.GetFullPath(currentExecutable), Path.GetFullPath(RequireContained(candidateRoot, orchestrator.entryPoint)),
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Candidate is not being validated by its owned Orchestrator.");
    }

    static NetworkTestToolBundleManifest RequireTool(NetworkTestProductBuildManifest candidate, string toolId)
    {
        NetworkTestToolBundleManifest[] values = (candidate.toolBundles ?? Array.Empty<NetworkTestToolBundleManifest>())
            .Where(value => value != null && value.toolId == toolId)
            .ToArray();
        return values.Length == 1 ? values[0] : throw new InvalidDataException($"Candidate requires tool '{toolId}'.");
    }

    static void RequireSlot(NetworkTestSessionSlotDocument slot)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var ports = new HashSet<int>();
        foreach (NetworkTestSessionEndpointDocument endpoint in slot.endpoints ?? Array.Empty<NetworkTestSessionEndpointDocument>())
        {
            if (endpoint == null || string.IsNullOrWhiteSpace(endpoint.key) || endpoint.address != "127.0.0.1" ||
                endpoint.port is <= 0 or > 65535 || !keys.Add(endpoint.key) || !ports.Add(endpoint.port))
                throw new InvalidDataException($"Session Slot '{slot.slotId}' endpoint catalog is invalid.");
        }
    }

    static void RequirePortsAvailable(NetworkTestSessionEndpointDocument[] endpoints)
    {
        HashSet<int> ports = (endpoints ?? Array.Empty<NetworkTestSessionEndpointDocument>())
            .Select(value => value.port)
            .ToHashSet();
        foreach (IPGlobalProperties properties in new[] { IPGlobalProperties.GetIPGlobalProperties() })
        {
            if (properties.GetActiveTcpListeners().Any(value => ports.Contains(value.Port)) ||
                properties.GetActiveUdpListeners().Any(value => ports.Contains(value.Port)))
                throw new InvalidOperationException("Session Slot contains an occupied endpoint.");
        }
    }

    static void RequireProcesses(NetworkTestRunProcessDocument[] processes)
    {
        if (processes.Length == 0 || processes.Any(value => value == null || string.IsNullOrWhiteSpace(value.roleId) ||
            value.processId <= 0 || value.processStartTimeUtcTicks <= 0) ||
            processes.Select(value => value.roleId).Distinct(StringComparer.Ordinal).Count() != processes.Length)
            throw new InvalidDataException("Session adapter process ownership is invalid.");
    }

    static FileStream AcquireSlotLease(string path, string runId, string candidateId, string slotId)
    {
        FileStream stream;
        try
        {
            stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        }
        catch (IOException exception)
        {
            string owner = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
            throw new InvalidOperationException($"Session Slot '{slotId}' is already owned. {owner}", exception);
        }
        stream.SetLength(0);
        JsonSerializer.Serialize(stream, new SlotLeaseDocument
        {
            schemaVersion = 1,
            slotId = slotId,
            runId = runId,
            candidateId = candidateId,
            orchestratorProcessId = Environment.ProcessId
        }, JsonOptions);
        stream.Flush(true);
        stream.Position = 0;
        return stream;
    }

    static void DeleteOwnedLease(string path, string runId)
    {
        try
        {
            SlotLeaseDocument lease = Read<SlotLeaseDocument>(path);
            if (lease.runId == runId)
                File.Delete(path);
        }
        catch (FileNotFoundException)
        {
        }
    }

    static bool HasExited(NetworkTestRunProcessDocument value)
    {
        try
        {
            Process process = Process.GetProcessById(value.processId);
            return process.StartTime.ToUniversalTime().Ticks != value.processStartTimeUtcTicks || process.HasExited;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    static async Task RequireStoppedAsync(NetworkTestRunProcessDocument[] processes)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(10);
        while (processes.Any(value => !HasExited(value)) && DateTime.UtcNow < deadline)
            await Task.Delay(100);
        if (processes.Any(value => !HasExited(value)))
            throw new InvalidOperationException("Owned Session processes did not exit after the Job Object closed.");
    }

    static string RequireNetworkRoot(string candidateRoot)
    {
        DirectoryInfo candidate = new(candidateRoot);
        DirectoryInfo product = candidate.Parent ?? throw new InvalidDataException("Candidate has no Product root.");
        DirectoryInfo network = product.Parent ?? throw new InvalidDataException("Product has no Network root.");
        if (!string.Equals(network.Name, "Network", StringComparison.Ordinal))
            throw new InvalidDataException("Candidate Network root is invalid.");
        return network.FullName;
    }

    static string RequireContained(string root, string relative)
    {
        string fullRoot = Path.GetFullPath(root);
        string full = Path.GetFullPath(Path.Combine(fullRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Candidate path escaped its root.");
        return full;
    }

    static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions) ??
        throw new InvalidDataException($"JSON document is invalid: {path}");

    static void Write<T>(string path, T value)
    {
        string fullPath = Path.GetFullPath(path);
        string temporary = fullPath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(value, JsonOptions));
            File.Move(temporary, fullPath, true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    static string Sha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    static string ComputeDirectoryHash(string candidateRoot, string relativeRoot)
    {
        string root = RequireContained(candidateRoot, relativeRoot);
        if (!Directory.Exists(root))
            throw new InvalidDataException($"Candidate Tool Bundle root is missing: {relativeRoot}");
        string joined = string.Join("|", Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/') + ":" + Sha256(path))
            .OrderBy(value => value, StringComparer.Ordinal));
        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
            "network-test-tool-bundle/1\u001f" + joined))).ToLowerInvariant();
    }

    static bool IsLowerHex(string value, int length) => value?.Length == length &&
        value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";

    sealed class SlotLeaseDocument
    {
        public int schemaVersion;
        public string slotId = string.Empty;
        public string runId = string.Empty;
        public string candidateId = string.Empty;
        public int orchestratorProcessId;
    }
}
