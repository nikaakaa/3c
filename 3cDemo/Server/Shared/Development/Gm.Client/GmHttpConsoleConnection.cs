using System.Net;

namespace ThirdPerson.Development.Gm;

public sealed class GmHttpConsoleConnection : IGmCommandConnection
{
    readonly GmClientManifest m_Manifest;
    readonly GmHttpClient m_Client;
    readonly List<PendingRequest> m_Pending = new();
    readonly Queue<GmCommandResponse> m_Responses = new();
    CancellationTokenSource m_Connection;
    Task<GmServiceDescription> m_Description;

    public GmHttpConsoleConnection(GmClientManifest manifest)
    {
        manifest.RequireValid();
        m_Manifest = manifest;
        m_Client = new GmHttpClient(manifest.endpoint, manifest.accessToken,
            manifest.maximumMessageBytes, manifest.requestTimeoutMilliseconds);
    }

    public GmConnectionState State { get; private set; }
    public string Endpoint => m_Manifest.endpoint;
    public string StatusMessage { get; private set; } = "尚未连接";
    public GmServiceDescription Service { get; private set; }

    public void Connect()
    {
        if (State != GmConnectionState.Disconnected)
            throw new InvalidOperationException("GM 连接正在使用，须先断开。");
        m_Connection = new CancellationTokenSource();
        State = GmConnectionState.Connecting;
        StatusMessage = "正在绑定服务和目标会话";
        m_Description = m_Client.GetAsync<GmServiceDescription>(GmHttpProtocol.ServicePath, null, m_Connection.Token);
    }

    public void Disconnect()
    {
        m_Connection?.Cancel();
        m_Connection?.Dispose();
        m_Connection = null;
        m_Description = null;
        m_Pending.Clear();
        m_Responses.Clear();
        Service = null;
        State = GmConnectionState.Disconnected;
        StatusMessage = "已断开";
    }

    public bool TrySend(GmCommandRequest request, out string error)
    {
        error = string.Empty;
        if (State != GmConnectionState.Connected || m_Pending.Count + m_Responses.Count >= m_Manifest.maximumPendingRequests)
        {
            error = "GM 未连接或在途请求达到容量。";
            return false;
        }
        m_Pending.Add(new PendingRequest(request, ExecuteAsync(request, m_Connection.Token)));
        return true;
    }

    async Task<GmCommandResponse> ExecuteAsync(GmCommandRequest request, CancellationToken cancellation)
    {
        try
        {
            return await m_Client.PostAsync<GmCommandResponse>(GmHttpProtocol.CommandsPath, request, cancellation);
        }
        catch (GmHttpResponseException exception)
        {
            return Failure(request, exception.Status == HttpStatusCode.Unauthorized ? GmResultCode.Unauthorized :
                exception.Status == HttpStatusCode.GatewayTimeout ? GmResultCode.TimedOut : GmResultCode.TargetUnavailable, exception.Message);
        }
        catch (OperationCanceledException)
        {
            return Failure(request, GmResultCode.TimedOut, "请求超时或已取消，执行结果未知。");
        }
    }

    static GmCommandResponse Failure(GmCommandRequest request, GmResultCode code, string message) => new()
    {
        requestId = request.requestId, candidateId = request.candidateId, runId = request.runId,
        serviceInstanceId = request.serviceInstanceId,
        sessionId = request.sessionId, tool = request.tool, code = code, message = message
    };

    public void Pump()
    {
        try
        {
            if (m_Description?.IsCompleted == true)
            {
                GmServiceDescription service = m_Description.GetAwaiter().GetResult();
                m_Description = null;
                if (service.protocolVersion != GmHttpProtocol.Version ||
                    service.candidateId != m_Manifest.candidateId || service.runId != m_Manifest.runId ||
                    service.sessionId != m_Manifest.sessionId || !SameTool(service.tool, m_Manifest.tool) ||
                    string.IsNullOrWhiteSpace(service.serviceInstanceId) ||
                    service.commands == null || service.commands.Length == 0 || service.commands.Length > 64)
                    throw new InvalidDataException("GM 服务协议、构建或目标会话不匹配。");
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (GmCommandDefinition command in service.commands)
                {
                    if (command == null || !GmCommandSyntax.IsValidCommandId(command.id) || command.version <= 0 || !ids.Add(command.id))
                        throw new InvalidDataException("GM 命令目录无效。");
                }
                Service = service;
                State = GmConnectionState.Connected;
                StatusMessage = "已绑定服务和目标会话";
            }
            for (int i = m_Pending.Count - 1; i >= 0; i--)
            {
                PendingRequest pending = m_Pending[i];
                if (!pending.Result.IsCompleted)
                    continue;
                m_Pending.RemoveAt(i);
                GmCommandResponse response = pending.Result.GetAwaiter().GetResult();
                if (response.requestId != pending.Request.requestId || response.code == GmResultCode.Unspecified ||
                    !SameTool(response.tool, pending.Request.tool) ||
                    !Enum.IsDefined(typeof(GmResultCode), response.code) || response.sections == null || response.sections.Length > 64)
                    throw new InvalidDataException("GM 请求关联或结果格式无效。");
                foreach (GmResultSection section in response.sections)
                {
                    if (section?.fields == null || section.fields.Length > 128 || section.fields.Any(field => field == null))
                        throw new InvalidDataException("GM 结果段格式无效。");
                }
                m_Responses.Enqueue(response);
            }
        }
        catch (Exception exception)
        {
            Disconnect();
            StatusMessage = exception.Message;
        }
    }

    public bool TryDequeueResponse(out GmCommandResponse response) => m_Responses.TryDequeue(out response);

    public void Dispose()
    {
        Disconnect();
        m_Client.Dispose();
    }

    static bool SameTool(GmToolIdentity left, GmToolIdentity right) =>
        left != null && right != null && left.toolId == right.toolId && left.toolVersion == right.toolVersion &&
        left.protocolVersion == right.protocolVersion && left.commandCatalogHash == right.commandCatalogHash &&
        left.bundleHash == right.bundleHash;

    sealed record PendingRequest(GmCommandRequest Request, Task<GmCommandResponse> Result);
}
