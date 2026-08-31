using System.Text.Json;
using ThirdPerson.Development.Gm;
using ThirdPerson.Development.Gm.Rollback;

namespace ThirdPerson.Development.Gm.Service;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            var arguments = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < args.Length; i += 2)
            {
                if (i + 1 >= args.Length || args[i] is not ("--manifest" or "--run-id" or "--log-directory") ||
                    !arguments.TryAdd(args[i], args[i + 1]))
                    throw new ArgumentException("GM 服务参数未知、不完整或重复。");
            }
            if (arguments.Count != 3)
                throw new ArgumentException("GM 服务需要 --manifest、--run-id 和 --log-directory。");
            string runId = arguments["--run-id"];
            if (string.IsNullOrWhiteSpace(runId) || runId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("GM RunId 不是合法文件名片段。");
            GmServerManifest manifest = GmHttpJson.ReadManifest<GmServerManifest>(arguments["--manifest"]);
            manifest.RequireValid();
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
                Console.WriteLine(line);
            }
            using var cancellation = new CancellationTokenSource();
            Console.CancelKeyPress += (_, value) => { value.Cancel = true; cancellation.Cancel(); };
            await using var service = new RollbackGmHttpService(manifest,
                operation => Write(JsonSerializer.Serialize(operation, GmHttpJson.Options)), Write);
            await service.StartAsync(cancellation.Token);
            Write($"READY run={runId} endpoint={manifest.http.Endpoint} session={manifest.sessionId}");
            try { await Task.Delay(Timeout.Infinite, cancellation.Token); }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"GM 服务启动或运行失败：{exception.GetType().Name}: {exception.Message}");
            return 1;
        }
    }
}
