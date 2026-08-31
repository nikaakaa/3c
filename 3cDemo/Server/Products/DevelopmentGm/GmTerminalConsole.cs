using System.Diagnostics;
using System.Text;

namespace ThirdPerson.Development.Gm.Service;

sealed class GmTerminalConsole
{
    readonly GmConsoleModel m_Model;
    readonly Stopwatch m_Clock = Stopwatch.StartNew();
    string m_Input = string.Empty;
    string m_LastConnection = string.Empty;
    ulong m_OutputRevision = ulong.MaxValue;
    int m_ScrollOffset;
    int m_Width;
    int m_Height;
    bool m_Dirty = true;

    public GmTerminalConsole(GmConsoleModel model) => m_Model = model;

    public async Task RunAsync(CancellationToken cancellation)
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
            throw new InvalidOperationException("GM 需要独立交互控制台，不能重定向标准输入输出。");
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "3C Rollback GM";
        m_Model.Connect();
        while (!cancellation.IsCancellationRequested)
        {
            m_Model.Pump(m_Clock.Elapsed.TotalSeconds);
            for (int i = 0; i < 64 && Console.KeyAvailable; i++)
                Handle(Console.ReadKey(true));
            string connection = $"{m_Model.ConnectionState} {m_Model.ConnectionMessage}";
            if (m_OutputRevision != m_Model.OutputRevision || m_LastConnection != connection ||
                m_Width != Console.WindowWidth || m_Height != Console.WindowHeight)
                m_Dirty = true;
            if (m_Dirty)
            {
                m_OutputRevision = m_Model.OutputRevision;
                m_LastConnection = connection;
                Draw();
                m_Dirty = false;
            }
            try { await Task.Delay(25, cancellation); }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        }
    }

    void Handle(ConsoleKeyInfo key)
    {
        m_Dirty = true;
        switch (key.Key)
        {
            case ConsoleKey.Enter:
                if (m_Model.Submit(m_Input, m_Clock.Elapsed.TotalSeconds))
                    m_Input = string.Empty;
                m_ScrollOffset = 0;
                return;
            case ConsoleKey.Backspace:
                if (m_Input.Length > 0)
                    m_Input = m_Input.Substring(0, m_Input.Length - 1);
                return;
            case ConsoleKey.UpArrow:
                m_Input = m_Model.PreviousHistory(m_Input);
                return;
            case ConsoleKey.DownArrow:
                m_Input = m_Model.NextHistory();
                return;
            case ConsoleKey.PageUp:
                m_ScrollOffset += Math.Max(1, m_Height - 7);
                return;
            case ConsoleKey.PageDown:
                m_ScrollOffset = Math.Max(0, m_ScrollOffset - Math.Max(1, m_Height - 7));
                return;
            case ConsoleKey.F5:
                m_Model.Disconnect();
                m_Model.Connect();
                return;
            case ConsoleKey.L when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                m_Model.ClearOutput();
                m_ScrollOffset = 0;
                return;
        }
        if (!char.IsControl(key.KeyChar) && m_Input.Length < GmCommandLineParser.MaximumLineLength)
            m_Input += key.KeyChar;
    }

    void Draw()
    {
        m_Width = Math.Max(20, Console.WindowWidth);
        m_Height = Math.Max(10, Console.WindowHeight);
        int columns = m_Width - 1;
        Console.Clear();
        WriteLine("3C Rollback GM | Enter 发送 | ↑↓ 历史 | PgUp/PgDn 结果翻页", columns);
        WriteLine("F5 重连 | Ctrl+L 清屏 | Ctrl+C 退出 | 输入 help 查看命令", columns);
        WriteLine($"{m_LastConnection} | 在途 {m_Model.PendingCount}", columns);
        WriteLine($"Session: {m_Model.Service?.sessionId} | {m_Model.Endpoint}", columns);
        WriteLine($"Service: {m_Model.Service?.serviceInstanceId}", columns);
        var lines = new List<string>();
        foreach (GmConsoleOutput output in m_Model.Output)
        {
            lines.Add($"> {output.CommandLine} [{output.State}] {output.RequestId}");
            lines.AddRange(output.Text.Split('\n'));
            lines.Add(string.Empty);
        }
        int visible = m_Height - 7;
        m_ScrollOffset = Math.Min(m_ScrollOffset, Math.Max(0, lines.Count - visible));
        int start = Math.Max(0, lines.Count - visible - m_ScrollOffset);
        for (int i = 0; i < visible; i++)
            WriteLine(start + i < lines.Count ? lines[start + i] : string.Empty, columns);
        Console.Write("> " + (m_Input.Length > columns - 2 ? m_Input.Substring(m_Input.Length - columns + 2) : m_Input));
    }

    static void WriteLine(string text, int columns)
    {
        int width = 0;
        var line = new StringBuilder();
        foreach (char value in text.TrimEnd('\r'))
        {
            int cells = value > 255 ? 2 : 1;
            if (width + cells > columns)
                break;
            line.Append(value);
            width += cells;
        }
        Console.WriteLine(line.ToString());
    }
}
