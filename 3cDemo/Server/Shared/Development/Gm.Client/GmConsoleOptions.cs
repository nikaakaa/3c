using System;

namespace ThirdPerson.Development.Gm
{
    public sealed class GmConsoleOptions
    {
        public GmConsoleOptions(
            int historyCapacity,
            int outputCapacity,
            int maximumOutputCharacters,
            int maximumPendingRequests,
            double requestTimeoutSeconds)
        {
            if (historyCapacity <= 0 || outputCapacity <= 0 || maximumOutputCharacters < 256 ||
                maximumPendingRequests <= 0 || requestTimeoutSeconds <= 0 ||
                double.IsNaN(requestTimeoutSeconds) || double.IsInfinity(requestTimeoutSeconds))
                throw new ArgumentException("GM 控制台容量和超时配置无效。");
            HistoryCapacity = historyCapacity;
            OutputCapacity = outputCapacity;
            MaximumOutputCharacters = maximumOutputCharacters;
            MaximumPendingRequests = maximumPendingRequests;
            RequestTimeoutSeconds = requestTimeoutSeconds;
        }

        public int HistoryCapacity { get; }
        public int OutputCapacity { get; }
        public int MaximumOutputCharacters { get; }
        public int MaximumPendingRequests { get; }
        public double RequestTimeoutSeconds { get; }
    }

    public enum GmConsoleOutputState
    {
        Sent,
        Succeeded,
        Rejected,
        TimedOut,
        Disconnected,
        TargetEnded,
        LocalError
    }

    public sealed class GmConsoleOutput
    {
        internal GmConsoleOutput(string requestId, string commandLine, GmConsoleOutputState state, string text)
        {
            RequestId = requestId;
            CommandLine = commandLine;
            State = state;
            Text = text;
        }

        public string RequestId { get; }
        public string CommandLine { get; }
        public GmConsoleOutputState State { get; internal set; }
        public string Text { get; internal set; }
    }
}
