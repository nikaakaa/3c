using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ThirdPersonPerformance;

namespace ThirdPersonPerformanceCapture.Controller;

internal readonly struct PerformanceMarkerRange
{
    public PerformanceMarkerRange(long start, long end)
    {
        Start = start;
        End = end;
    }

    public long Start { get; }
    public long End { get; }
}

internal static class PerformanceFileUtility
{
    public static string Sha256(string path)
    {
        using SHA256 sha = SHA256.Create();
        using FileStream stream = File.OpenRead(path);
        return Hex(sha.ComputeHash(stream));
    }

    public static string Sha256(byte[] bytes)
    {
        using SHA256 sha = SHA256.Create();
        return Hex(sha.ComputeHash(bytes));
    }

    public static void PublishWpaContextSwitches(string exportRoot, string stagingRoot, int playerProcessId)
    {
        string[] files = Directory.GetFiles(exportRoot, "*.csv", SearchOption.TopDirectoryOnly);
        if (files.Length == 0)
            throw new InvalidDataException("WPA Exporter did not produce the required Context Switch table.");
        string switches = string.Empty;
        for (int i = 0; i < files.Length; i++)
        {
            string header = (File.ReadLines(files[i], Encoding.UTF8).FirstOrDefault() ?? string.Empty).TrimStart('\ufeff');
            if (header.Contains("New Process", StringComparison.OrdinalIgnoreCase) &&
                header.Contains("New Thread Stack", StringComparison.OrdinalIgnoreCase) &&
                (header.Contains("Wait", StringComparison.OrdinalIgnoreCase) ||
                 header.Contains("Switch", StringComparison.OrdinalIgnoreCase) ||
                 header.Contains("Ready", StringComparison.OrdinalIgnoreCase)))
            {
                switches = AssignUnique(switches, files[i], "Context switch");
            }
        }
        if (string.IsNullOrEmpty(switches))
            throw new InvalidDataException("WPA export cannot be mapped to the Context Switch role.");
        string[] switchLines = FilterProcess(switches, playerProcessId);
        File.WriteAllLines(Path.Combine(stagingRoot, "context-switches.csv"), switchLines, new UTF8Encoding(false));
        File.WriteAllLines(Path.Combine(stagingRoot, "thread-stacks.csv"), switchLines, new UTF8Encoding(false));
    }

    public static PerformanceMarkerRange PublishXperfMarkers(
        string report,
        string stagingRoot,
        string startMarker,
        string stopMarker)
    {
        if (string.IsNullOrWhiteSpace(report))
            throw new InvalidDataException("Xperf marker report is empty.");
        File.WriteAllText(Path.Combine(stagingRoot, "xperf-marks.csv"), report, new UTF8Encoding(false));
        long start = 0;
        long end = 0;
        foreach (string line in report.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] fields = line.Split(',', 3);
            if (fields.Length != 3 ||
                !long.TryParse(fields[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long timestamp))
            {
                continue;
            }
            string label = fields[2].Trim();
            if (string.Equals(label, startMarker, StringComparison.Ordinal))
                start = timestamp;
            else if (string.Equals(label, stopMarker, StringComparison.Ordinal))
                end = timestamp;
        }
        if (start <= 0 || end <= start)
            throw new InvalidDataException("Xperf marker report does not contain the exact Capture boundary.");
        return new PerformanceMarkerRange(start, end);
    }

    public static void PublishXperfStackReport(string report, string stagingRoot, int playerProcessId)
    {
        if (string.IsNullOrWhiteSpace(report))
            throw new InvalidDataException("Xperf stack report is empty.");
        File.WriteAllText(Path.Combine(stagingRoot, "xperf-stack.xhtml"), report, new UTF8Encoding(false));
        const string sectionStart = "<a id='TblSI'>";
        const string sectionEnd = "<a id='TblSN'>";
        int start = report.IndexOf(sectionStart, StringComparison.Ordinal);
        if (start < 0)
            throw new InvalidDataException("Xperf stack report is missing the UniInclusive function table.");
        int end = report.IndexOf(sectionEnd, start + sectionStart.Length, StringComparison.Ordinal);
        if (end <= start)
            throw new InvalidDataException("Xperf stack report is missing the UniInclusive function table.");
        string section = report.Substring(start, end - start);
        var rows = new Regex(
            "<tr><td>(?<identity>.*?)</td><td>(?<inclusive>.*?)</td><td>.*?</td><td>(?<exclusive>.*?)</td><td>.*?</td><td>.*?</td><td>.*?</td></tr>",
            RegexOptions.CultureInvariant | RegexOptions.Singleline);
        var identityPattern = new Regex(
            "<a\\s+href='[^']*'>(?<module>.*?)</a>!<a\\s+href='[^']*'>(?<function>.*?)</a>",
            RegexOptions.CultureInvariant | RegexOptions.Singleline);
        string process = $"ThirdPersonPerformancePlayer.exe ({playerProcessId.ToString(CultureInfo.InvariantCulture)})";
        var output = new List<string> { "Process,Thread,Module,Function,Inclusive,Exclusive" };
        foreach (Match row in rows.Matches(section))
        {
            Match identity = identityPattern.Match(row.Groups["identity"].Value);
            if (!identity.Success)
                throw new InvalidDataException("Xperf function row cannot be mapped to module and function identities.");
            string module = NormalizeHtmlText(identity.Groups["module"].Value);
            string function = NormalizeHtmlText(identity.Groups["function"].Value);
            if (!double.TryParse(NormalizeHtmlText(row.Groups["inclusive"].Value), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double inclusive) ||
                !double.TryParse(NormalizeHtmlText(row.Groups["exclusive"].Value), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double exclusive))
            {
                throw new InvalidDataException("Xperf function row contains invalid sample counts.");
            }
            output.Add(string.Join(",", new[]
            {
                QuoteCsv(process),
                QuoteCsv("All Threads"),
                QuoteCsv(module),
                QuoteCsv(function),
                inclusive.ToString("R", CultureInfo.InvariantCulture),
                exclusive.ToString("R", CultureInfo.InvariantCulture)
            }));
        }
        if (output.Count == 1)
            throw new InvalidDataException("Xperf stack report contains no function hotspots.");
        File.WriteAllLines(Path.Combine(stagingRoot, "cpu-hotspots.csv"), output, new UTF8Encoding(false));
    }

    public static PerformanceFileDocument[] BuildClosure(string root, params string[] excludedNames)
    {
        var excluded = new HashSet<string>(excludedNames ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        return Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => !excluded.Contains(Path.GetRelativePath(root, path).Replace('\\', '/')))
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => new PerformanceFileDocument
            {
                role = Role(path),
                path = Path.GetRelativePath(root, path).Replace('\\', '/'),
                size = new FileInfo(path).Length,
                sha256 = Sha256(path)
            })
            .ToArray();
    }

    public static void WriteJson<T>(string path, T value, System.Text.Json.JsonSerializerOptions options) =>
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(value, options), new UTF8Encoding(false));

    static string AssignUnique(string current, string path, string role)
    {
        if (!string.IsNullOrEmpty(current))
            throw new InvalidDataException($"WPA Exporter produced multiple {role} tables.");
        return path;
    }

    static string[] FilterProcess(string path, int processId)
    {
        string[] lines = File.ReadAllLines(path, Encoding.UTF8);
        if (lines.Length < 2)
            throw new InvalidDataException($"WPA export '{Path.GetFileName(path)}' is empty.");
        string pid = $"({processId.ToString(CultureInfo.InvariantCulture)})";
        var result = new List<string> { lines[0].TrimStart('\ufeff') };
        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = Csv(lines[i]);
            if (fields.Any(value => value.Contains(pid, StringComparison.Ordinal)))
                result.Add(lines[i]);
        }
        if (result.Count == 1)
            throw new InvalidDataException($"WPA export '{Path.GetFileName(path)}' contains no rows for Player PID {processId}.");
        return result.ToArray();
    }

    static string NormalizeHtmlText(string value) =>
        WebUtility.HtmlDecode(value ?? string.Empty).Trim().Replace('\u00a0', ' ');

    static string[] Csv(string line)
    {
        var values = new List<string>();
        var value = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            char character = line[i];
            if (character == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                {
                    value.Append('"');
                    i++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (character == ',' && !quoted)
            {
                values.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(character);
            }
        }
        if (quoted)
            throw new InvalidDataException("WPA CSV contains an unterminated quote.");
        values.Add(value.ToString());
        return values.ToArray();
    }

    static string QuoteCsv(string value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";

    static string Role(string path) => Path.GetFileName(path).ToLowerInvariant() switch
    {
        "summary.json" => "summary",
        "comparison.json" => "comparison",
        "metric-samples.csv" => "metric-samples",
        "metric-catalog.json" => "metric-catalog",
        "instrumentation-spans.bin" => "instrumentation-spans",
        "unity-profiler.raw" => "unity-profiler",
        "windows-cpu.etl" => "windows-cpu",
        "windows-cpu-fault.etl" => "windows-cpu-fault",
        "cpu-hotspots.csv" => "cpu-hotspots",
        "thread-stacks.csv" => "thread-stacks",
        "context-switches.csv" => "context-switches",
        "wpa-exporter.json" => "wpa-exporter-config",
        "xperf-marks.csv" => "xperf-marks",
        "xperf-stack.xhtml" => "xperf-stack-report",
        "player.log" => "player-log",
        "controller.log" => "controller-log",
        "process.json" => "process",
        "runtime-result.json" => "runtime-result",
        "request.json" => "request",
        "status.json" => "status",
        "cancel.request" => "cancel-request",
        _ => "run-file"
    };

    static string Hex(byte[] bytes)
    {
        var builder = new StringBuilder(bytes.Length * 2);
        for (int i = 0; i < bytes.Length; i++)
            builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
        return builder.ToString();
    }
}
