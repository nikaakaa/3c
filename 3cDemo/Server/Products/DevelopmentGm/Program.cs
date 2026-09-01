using System.Text.Json;
using System.Security.Cryptography;
using ThirdPerson.Development.Gm;
using ThirdPerson.Development.Gm.Rollback;

namespace ThirdPerson.Development.Gm.Service;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 2 && args[0] == "--write-tool-manifest")
            {
                GmCommandRegistry registry = RollbackGmCommandModule.CreateRegistry(new ManifestQuerySource());
                var tool = new GmToolManifest
                {
                    schemaVersion = 1,
                    toolId = GmToolCatalog.ToolId,
                    toolVersion = GmToolCatalog.ToolVersion,
                    protocolVersion = GmHttpProtocol.Version,
                    commandCatalogHash = registry.CommandCatalogHash
                };
                tool.RequireValid();
                File.WriteAllText(args[1], JsonSerializer.Serialize(tool, GmHttpJson.Options));
                return 0;
            }
            if (args.Length == 5 && args[0] == "--write-run-manifests")
            {
                WriteRunManifests(args[1], args[2], args[3], args[4]);
                return 0;
            }
            var arguments = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < args.Length; i += 2)
            {
                if (i + 1 >= args.Length || args[i] is not ("--manifest" or "--console-manifest" or "--run-id" or "--log-directory") ||
                    !arguments.TryAdd(args[i], args[i + 1]))
                    throw new ArgumentException("GM 服务参数未知、不完整或重复。");
            }
            if (arguments.Count != 4)
                throw new ArgumentException("GM 服务需要 --manifest、--console-manifest、--run-id 和 --log-directory。");
            string runId = arguments["--run-id"];
            if (string.IsNullOrWhiteSpace(runId) || runId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("GM RunId 不是合法文件名片段。");
            GmServerManifest manifest = GmHttpJson.ReadManifest<GmServerManifest>(arguments["--manifest"]);
            manifest.RequireValid();
            GmClientManifest console = GmHttpJson.ReadManifest<GmClientManifest>(arguments["--console-manifest"]);
            console.RequireValid();
            if (console.candidateId != manifest.candidateId || console.runId != manifest.runId ||
                console.sessionId != manifest.sessionId || console.slotId != manifest.slotId ||
                !SameTool(console.tool, manifest.tool) || console.endpoint != manifest.http.Endpoint ||
                console.accessToken != manifest.http.accessToken)
                throw new InvalidDataException("GM 控制台和服务配置不匹配。");
            string directory = Path.GetFullPath(arguments["--log-directory"]);
            Directory.CreateDirectory(directory);
            using TextWriter log = TextWriter.Synchronized(new StreamWriter(Path.Combine(directory, $"{runId}-gm.log"), false)
            {
                AutoFlush = true
            });
            void Write(string value)
            {
                string line = $"{DateTimeOffset.UtcNow:O} {value}";
                log.WriteLine(line);
            }
            using var cancellation = new CancellationTokenSource();
            Console.CancelKeyPress += (_, value) => { value.Cancel = true; cancellation.Cancel(); };
            await using var service = new RollbackGmHttpService(manifest,
                operation => Write(JsonSerializer.Serialize(operation, GmHttpJson.Options)), Write);
            await service.StartAsync(cancellation.Token);
            Write($"READY candidate={manifest.candidateId} run={runId} endpoint={manifest.http.Endpoint} session={manifest.sessionId}");
            var options = new GmConsoleOptions(console.historyCapacity, console.outputCapacity,
                console.maximumOutputCharacters, console.maximumPendingRequests, console.requestTimeoutMilliseconds / 1000d);
            using var model = new GmConsoleModel(new GmHttpConsoleConnection(console), options);
            await new GmTerminalConsole(model).RunAsync(cancellation.Token);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"GM 服务启动或运行失败：{exception.GetType().Name}: {exception.Message}");
            return 1;
        }
    }

    static void WriteRunManifests(
        string toolManifestPath,
        string policyPath,
        string requestPath,
        string outputDirectory)
    {
        GmToolManifest toolManifest = GmHttpJson.ReadManifest<GmToolManifest>(toolManifestPath);
        GmToolPolicy policy = GmHttpJson.ReadManifest<GmToolPolicy>(policyPath);
        GmRunRequest request = GmHttpJson.ReadManifest<GmRunRequest>(requestPath);
        toolManifest.RequireValid();
        policy.RequireValid();
        request.RequireValid();
        string clientToken = CreateToken();
        string relayToken = CreateToken();
        var tool = new GmToolIdentity
        {
            toolId = toolManifest.toolId,
            toolVersion = toolManifest.toolVersion,
            protocolVersion = toolManifest.protocolVersion,
            commandCatalogHash = toolManifest.commandCatalogHash,
            bundleHash = request.toolBundleHash
        };
        tool.RequireValid();
        var server = new GmServerManifest
        {
            schemaVersion = GmHttpProtocol.Version,
            candidateId = request.candidateId,
            runId = request.runId,
            sessionId = request.sessionId,
            slotId = request.slotId,
            tool = tool,
            http = BuildHttp(request.gmAddress, request.gmPort, clientToken, policy),
            relayQueryEndpoint = $"http://{request.relayQueryAddress}:{request.relayQueryPort}/",
            relayQueryToken = relayToken,
            relayQueryTimeoutMilliseconds = policy.relayTimeoutMilliseconds
        };
        var relay = new RelayQueryManifest
        {
            schemaVersion = GmHttpProtocol.Version,
            candidateId = request.candidateId,
            runId = request.runId,
            sessionId = request.sessionId,
            slotId = request.slotId,
            tool = tool,
            http = BuildHttp(request.relayQueryAddress, request.relayQueryPort, relayToken, policy),
            maximumQueuedQueries = policy.maximumQueuedQueries,
            maximumQueriesPerPump = policy.maximumQueriesPerPump
        };
        var client = new GmClientManifest
        {
            schemaVersion = GmHttpProtocol.Version,
            candidateId = request.candidateId,
            runId = request.runId,
            sessionId = request.sessionId,
            slotId = request.slotId,
            tool = tool,
            endpoint = server.http.Endpoint,
            accessToken = clientToken,
            maximumMessageBytes = policy.maximumMessageBytes,
            maximumPendingRequests = policy.maximumClientRequests,
            requestTimeoutMilliseconds = policy.clientTimeoutMilliseconds,
            historyCapacity = policy.historyCapacity,
            outputCapacity = policy.outputCapacity,
            maximumOutputCharacters = policy.maximumOutputCharacters
        };
        server.RequireValid();
        relay.RequireValid();
        client.RequireValid();
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(outputDirectory, "GmServerRunManifest.json"), JsonSerializer.Serialize(server, GmHttpJson.Options));
        File.WriteAllText(Path.Combine(outputDirectory, "RelayQueryRunManifest.json"), JsonSerializer.Serialize(relay, GmHttpJson.Options));
        File.WriteAllText(Path.Combine(outputDirectory, "GmConsoleRunManifest.json"), JsonSerializer.Serialize(client, GmHttpJson.Options));
    }

    static GmHttpServerConfiguration BuildHttp(
        string address,
        int port,
        string token,
        GmToolPolicy policy) => new()
    {
        listenAddress = address,
        listenPort = port,
        accessToken = token,
        maximumMessageBytes = policy.maximumMessageBytes,
        maximumConcurrentRequests = policy.maximumServerRequests,
        requestTimeoutMilliseconds = policy.serverTimeoutMilliseconds
    };

    static string CreateToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    static bool SameTool(GmToolIdentity left, GmToolIdentity right) =>
        left != null && right != null && left.toolId == right.toolId && left.toolVersion == right.toolVersion &&
        left.protocolVersion == right.protocolVersion && left.commandCatalogHash == right.commandCatalogHash &&
        left.bundleHash == right.bundleHash;

    sealed class ManifestQuerySource : IRollbackGmQuerySource
    {
        public Task<RollbackGmSessionSnapshot> CaptureSessionAsync(CancellationToken cancellation) =>
            Task.FromException<RollbackGmSessionSnapshot>(new InvalidOperationException());
        public Task<RollbackGmActorSnapshot[]> CaptureActorsAsync(CancellationToken cancellation) =>
            Task.FromException<RollbackGmActorSnapshot[]>(new InvalidOperationException());
        public Task<RollbackGmRuntimeSnapshot> CaptureRuntimeAsync(CancellationToken cancellation) =>
            Task.FromException<RollbackGmRuntimeSnapshot>(new InvalidOperationException());
    }
}
