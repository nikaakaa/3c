using System;
using System.Collections.Generic;

namespace ThirdPerson.Development.Gm
{
    public sealed class GmConsoleModel : IDisposable
    {
        readonly IGmCommandConnection m_Connection;
        readonly GmConsoleOptions m_Options;
        readonly List<string> m_History = new List<string>();
        readonly List<GmConsoleOutput> m_Output = new List<GmConsoleOutput>();
        readonly Dictionary<string, PendingRequest> m_Pending =
            new Dictionary<string, PendingRequest>(StringComparer.Ordinal);
        readonly List<string> m_Completed = new List<string>();
        int m_HistoryCursor;
        string m_Draft = string.Empty;
        bool m_Disposed;

        public GmConsoleModel(IGmCommandConnection connection, GmConsoleOptions options)
        {
            m_Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            m_Options = options ?? throw new ArgumentNullException(nameof(options));
            Output = m_Output.AsReadOnly();
        }

        public IReadOnlyList<GmConsoleOutput> Output { get; }
        public ulong OutputRevision { get; private set; }
        public int PendingCount => m_Pending.Count;
        public GmConnectionState ConnectionState => m_Connection.State;
        public string Endpoint => m_Connection.Endpoint;
        public string ConnectionMessage => m_Connection.StatusMessage;
        public GmServiceDescription Service => m_Connection.Service;

        public void Connect()
        {
            RequireAlive();
            m_Connection.Connect();
        }

        public void Disconnect()
        {
            RequireAlive();
            m_Connection.Disconnect();
            FailPending(GmConsoleOutputState.Disconnected, "连接已关闭，未完成请求的执行结果未知。");
        }

        public bool Submit(string line, double nowSeconds)
        {
            RequireAlive();
            if (!GmCommandLineParser.TryParse(line, out GmParsedCommand parsed, out string error))
                return LocalError(line, error);
            Remember(line);
            if (ConnectionState != GmConnectionState.Connected)
                return LocalError(line, "GM 服务未连接，命令未发送。");
            if (m_Pending.Count >= m_Options.MaximumPendingRequests)
                return LocalError(line, "在途请求已达上限，命令未发送。");
            var request = new GmCommandRequest
            {
                requestId = Guid.NewGuid().ToString("N"),
                candidateId = Service.candidateId,
                runId = Service.runId,
                serviceInstanceId = Service.serviceInstanceId,
                sessionId = Service.sessionId,
                commandId = parsed.CommandId,
                commandVersion = FindVersion(parsed.CommandId),
                arguments = parsed.Arguments
            };
            if (!m_Connection.TrySend(request, out error))
                return LocalError(line, error);
            GmConsoleOutput output = Append(
                request.requestId, line, GmConsoleOutputState.Sent, "请求已发送，等待服务端执行结果。");
            m_Pending.Add(request.requestId, new PendingRequest(
                request, output, nowSeconds + m_Options.RequestTimeoutSeconds));
            return true;
        }

        public void Pump(double nowSeconds)
        {
            RequireAlive();
            m_Connection.Pump();
            m_Completed.Clear();
            foreach (KeyValuePair<string, PendingRequest> pair in m_Pending)
            {
                PendingRequest pending = pair.Value;
                if (ConnectionState != GmConnectionState.Connected)
                    Complete(pair.Key, pending.Output, GmConsoleOutputState.Disconnected, "服务连接中断，执行结果未知。");
                else if (!MatchesService(
                             pending.Request.candidateId,
                             pending.Request.runId,
                             pending.Request.serviceInstanceId,
                             pending.Request.sessionId))
                    Complete(pair.Key, pending.Output, GmConsoleOutputState.TargetEnded, "服务运行实例已改变，旧请求不会转交新实例。");
                else if (nowSeconds >= pending.Deadline)
                    Complete(pair.Key, pending.Output, GmConsoleOutputState.TimedOut, "请求超时，执行结果未知；不会自动重发。");
            }
            RemoveCompleted();
            for (int i = 0; i < m_Options.MaximumPendingRequests; i++)
            {
                if (!m_Connection.TryDequeueResponse(out GmCommandResponse response))
                    break;
                if (!m_Pending.TryGetValue(response.requestId, out PendingRequest pending))
                    continue;
                if (!string.Equals(response.candidateId, pending.Request.candidateId, StringComparison.Ordinal) ||
                    !string.Equals(response.runId, pending.Request.runId, StringComparison.Ordinal) ||
                    !string.Equals(response.serviceInstanceId, pending.Request.serviceInstanceId, StringComparison.Ordinal) ||
                    !string.Equals(response.sessionId, pending.Request.sessionId, StringComparison.Ordinal) ||
                    response.code == GmResultCode.TargetEnded)
                {
                    Complete(response.requestId, pending.Output, GmConsoleOutputState.TargetEnded, "响应不属于请求指定的服务实例和会话。");
                    m_Pending.Remove(response.requestId);
                    m_Connection.Disconnect();
                    FailPending(GmConsoleOutputState.TargetEnded, "目标运行实例已结束，请显式重新连接。");
                    break;
                }
                Complete(response.requestId, pending.Output,
                    response.code == GmResultCode.Success ? GmConsoleOutputState.Succeeded :
                    response.code == GmResultCode.TimedOut ? GmConsoleOutputState.TimedOut : GmConsoleOutputState.Rejected,
                    GmResultTextFormatter.Format(response, m_Options.MaximumOutputCharacters));
                m_Pending.Remove(response.requestId);
            }
            RemoveCompleted();
        }

        public void ClearOutput()
        {
            RequireAlive();
            m_Output.Clear();
            OutputRevision++;
        }

        public string PreviousHistory(string draft)
        {
            RequireAlive();
            if (m_History.Count == 0)
                return draft;
            if (m_HistoryCursor == m_History.Count)
                m_Draft = draft;
            m_HistoryCursor = Math.Max(0, m_HistoryCursor - 1);
            return m_History[m_HistoryCursor];
        }

        public string NextHistory()
        {
            RequireAlive();
            m_HistoryCursor = Math.Min(m_History.Count, m_HistoryCursor + 1);
            return m_HistoryCursor == m_History.Count ? m_Draft : m_History[m_HistoryCursor];
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Disconnect();
            m_Connection.Dispose();
            m_Disposed = true;
        }

        void Remember(string line)
        {
            if (m_History.Count == 0 || !string.Equals(m_History[m_History.Count - 1], line, StringComparison.Ordinal))
            {
                if (m_History.Count == m_Options.HistoryCapacity)
                    m_History.RemoveAt(0);
                m_History.Add(line);
            }
            m_HistoryCursor = m_History.Count;
            m_Draft = string.Empty;
        }

        int FindVersion(string commandId)
        {
            foreach (GmCommandDefinition command in Service.commands)
            {
                if (string.Equals(command.id, commandId, StringComparison.Ordinal))
                    return command.version;
            }
            return 0;
        }

        bool MatchesService(string candidateId, string runId, string instanceId, string sessionId) =>
            string.Equals(Service.candidateId, candidateId, StringComparison.Ordinal) &&
            string.Equals(Service.runId, runId, StringComparison.Ordinal) &&
            string.Equals(Service.serviceInstanceId, instanceId, StringComparison.Ordinal) &&
            string.Equals(Service.sessionId, sessionId, StringComparison.Ordinal);

        bool LocalError(string line, string error)
        {
            Append(string.Empty, line, GmConsoleOutputState.LocalError, error);
            return false;
        }

        GmConsoleOutput Append(string requestId, string line, GmConsoleOutputState state, string text)
        {
            var output = new GmConsoleOutput(requestId, Limit(line), state, Limit(text));
            AddOutput(output);
            OutputRevision++;
            return output;
        }

        void AddOutput(GmConsoleOutput output)
        {
            if (m_Output.Count == m_Options.OutputCapacity)
                m_Output.RemoveAt(0);
            m_Output.Add(output);
        }

        void Complete(string requestId, GmConsoleOutput output, GmConsoleOutputState state, string text)
        {
            output.State = state;
            output.Text = Limit(text);
            if (!m_Output.Contains(output))
                AddOutput(output);
            m_Completed.Add(requestId);
            OutputRevision++;
        }

        void RemoveCompleted()
        {
            foreach (string requestId in m_Completed)
                m_Pending.Remove(requestId);
            m_Completed.Clear();
        }

        void FailPending(GmConsoleOutputState state, string message)
        {
            foreach (KeyValuePair<string, PendingRequest> pair in m_Pending)
                Complete(pair.Key, pair.Value.Output, state, message);
            RemoveCompleted();
        }

        string Limit(string text) => string.IsNullOrEmpty(text) ? string.Empty :
            text.Length <= m_Options.MaximumOutputCharacters ? text : text.Substring(0, m_Options.MaximumOutputCharacters);

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(GmConsoleModel));
        }

        sealed class PendingRequest
        {
            public PendingRequest(GmCommandRequest request, GmConsoleOutput output, double deadline)
            {
                Request = request;
                Output = output;
                Deadline = deadline;
            }

            public GmCommandRequest Request { get; }
            public GmConsoleOutput Output { get; }
            public double Deadline { get; }
        }
    }
}
