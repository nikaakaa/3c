using System.Text.Json;
using ThirdPerson.Development.Gm;
using ThirdPerson.Development.Gm.Rollback;
using ThirdPersonSimulation.DeterministicRollback;

namespace ThirdPerson.DeterministicRollback.Server;

static class Program
{
    const int InvalidArguments = 2;
    const int InvalidManifest = 3;
    const int RuntimeFailure = 4;

    static int Main(string[] args)
    {
        ServerArguments arguments;
        try
        {
            arguments = ServerArguments.Parse(args);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return InvalidArguments;
        }

        DeterministicRollbackServerManifest manifest;
        RelayQueryManifest queryManifest;
        try
        {
            string json = File.ReadAllText(arguments.ManifestPath);
            manifest = JsonSerializer.Deserialize<DeterministicRollbackServerManifest>(json, new JsonSerializerOptions
            {
                IncludeFields = true,
                PropertyNameCaseInsensitive = false
            }) ?? throw new InvalidDataException("Deterministic Rollback Server manifest is empty.");
            manifest.RequireValidHash();
            queryManifest = GmHttpJson.ReadManifest<RelayQueryManifest>(arguments.QueryManifestPath);
            queryManifest.RequireValid();
            if (queryManifest.buildId != manifest.buildId || queryManifest.sessionId != manifest.sessionId)
                throw new InvalidDataException("Relay 查询配置与 Gameplay manifest 身份不同。");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Manifest validation failed: {exception.Message}");
            return InvalidManifest;
        }

        Directory.CreateDirectory(arguments.LogDirectory);
        string logPath = Path.Combine(arguments.LogDirectory, $"{arguments.RunId}-relay.log");
        using var log = new StreamWriter(logPath, false) { AutoFlush = true };
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        try
        {
            using var runtime = new RollbackInputRelayRuntime(
                manifest.BuildEndpointDefinition(),
                manifest.BuildPolicy(),
                manifest.BuildHandshake(),
                manifest.relayServerPeerId,
                manifest.BuildRoster(),
                manifest.inputRedundancyCount);
            using var query = new RollbackRelayQueryBridge(queryManifest, manifest, runtime, message => Write(log, message));
            query.Start(cancellation.Token);
            Write(log, $"READY run={arguments.RunId} endpoint={runtime.LocalEndPoint} session={manifest.sessionId} peers={manifest.peers.Length}");
            long nextDiagnostics = Environment.TickCount64 + 1000;
            while (!cancellation.IsCancellationRequested)
            {
                try
                {
                    runtime.Pump();
                    query.Pump();
                }
                catch
                {
                    Write(log, BuildDiagnostics(runtime));
                    throw;
                }
                long now = Environment.TickCount64;
                if (now >= nextDiagnostics)
                {
                    Write(log, BuildDiagnostics(runtime));
                    nextDiagnostics = now + 1000;
                }
                Thread.Sleep(1);
            }
            Write(log, "STOP requested");
            return 0;
        }
        catch (Exception exception)
        {
            Write(log, $"FAILED {exception}");
            Console.Error.WriteLine(exception);
            return RuntimeFailure;
        }
    }

    static void Write(StreamWriter log, string message)
    {
        string line = $"{DateTimeOffset.UtcNow:O} {message}";
        lock (log)
            log.WriteLine(line);
        Console.WriteLine(line);
    }

    static string BuildDiagnostics(RollbackInputRelayRuntime runtime)
    {
        IReadOnlyList<RollbackRelayPeerInputFrontier> frontiers = runtime.CapturePeerInputFrontiers();
        string frontierText = string.Join(",", frontiers.Select(value =>
            $"{value.PeerId}/{value.ActorId}:{value.Tick}"));
        return $"relay rx={runtime.TotalReceivedDatagrams} tx={runtime.TotalSentDatagrams} " +
               $"input={runtime.InputBatchCount} forward={runtime.ExplicitRelayBroadcastCount} " +
               $"dedupe={runtime.DeduplicatedInputCount} invalid={runtime.InvalidInputCount} " +
               $"canonical={runtime.CanonicalBundleCount} nextCanonical={runtime.NextCanonicalTick.Value} " +
               $"confirmedFrontier={runtime.ConfirmedCanonicalTick} confirmationBroadcast={runtime.ConfirmationBroadcastCount} " +
               $"explicitFrontiers=[{frontierText}] " +
               $"pendingReliable={runtime.PendingReliableCount} dropped={runtime.DroppedReceivedDatagrams}";
    }

    sealed class ServerArguments
    {
        ServerArguments(string manifestPath, string queryManifestPath, string runId, string logDirectory)
        {
            ManifestPath = manifestPath;
            QueryManifestPath = queryManifestPath;
            RunId = runId;
            LogDirectory = logDirectory;
        }

        public string ManifestPath { get; }
        public string QueryManifestPath { get; }
        public string RunId { get; }
        public string LogDirectory { get; }

        public static ServerArguments Parse(IReadOnlyList<string> args)
        {
            string? manifest = null;
            string? queryManifest = null;
            string? runId = null;
            string? logDirectory = null;
            for (int i = 0; i < args.Count; i++)
            {
                string value = args[i];
                if (value == "--manifest" && ++i < args.Count)
                    manifest = args[i];
                else if (value == "--query-manifest" && ++i < args.Count)
                    queryManifest = args[i];
                else if (value == "--run-id" && ++i < args.Count)
                    runId = args[i];
                else if (value == "--log-directory" && ++i < args.Count)
                    logDirectory = args[i];
                else
                    throw new ArgumentException($"Unknown or incomplete argument '{value}'.");
            }
            if (string.IsNullOrWhiteSpace(manifest) || !File.Exists(manifest))
                throw new ArgumentException("--manifest must name an existing Server manifest.");
            if (string.IsNullOrWhiteSpace(queryManifest) || !File.Exists(queryManifest))
                throw new ArgumentException("--query-manifest must name the published Relay query manifest.");
            if (string.IsNullOrWhiteSpace(runId) || runId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("--run-id is required and must be a valid file name segment.");
            if (string.IsNullOrWhiteSpace(logDirectory))
                throw new ArgumentException("--log-directory is required.");
            return new ServerArguments(Path.GetFullPath(manifest), Path.GetFullPath(queryManifest), runId, Path.GetFullPath(logDirectory));
        }
    }
}
