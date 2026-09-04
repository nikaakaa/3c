using System.Diagnostics;
using System.Text;

namespace ThirdPersonPerformanceCapture.Controller;

internal readonly struct ProcessResult
{
    public ProcessResult(int exitCode, string output)
    {
        ExitCode = exitCode;
        Output = output ?? string.Empty;
    }

    public int ExitCode { get; }
    public string Output { get; }
}

internal static class PerformanceProcessRunner
{
    public static ProcessResult Run(
        string fileName,
        string arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string> environment,
        TimeSpan timeout,
        Action heartbeat = null)
    {
        var start = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (environment != null)
        {
            foreach (KeyValuePair<string, string> pair in environment)
                start.Environment[pair.Key] = pair.Value;
        }
        using Process process = Process.Start(start) ??
            throw new InvalidOperationException($"Process failed to start: {fileName}");
        var output = new StringBuilder();
        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data != null)
                output.AppendLine(eventArgs.Data);
        };
        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data != null)
                output.AppendLine(eventArgs.Data);
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        DateTime deadline = DateTime.UtcNow.Add(timeout);
        while (!process.WaitForExit(250))
        {
            heartbeat?.Invoke();
            if (DateTime.UtcNow < deadline)
                continue;
            process.Kill(true);
            process.WaitForExit();
            throw new TimeoutException($"Process timed out: {fileName} {arguments}");
        }
        process.WaitForExit();
        return new ProcessResult(process.ExitCode, output.ToString());
    }
}
