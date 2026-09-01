namespace ThirdPerson.Development.Gm;

public readonly record struct GmOperationRecord(
    string RequestId,
    string CallerId,
    string CommandId,
    GmResultCode Result,
    string Stage);

public sealed class GmCommandDispatcher
{
    readonly GmCommandRegistry m_Registry;
    readonly Action<GmOperationRecord> m_Record;

    public GmCommandDispatcher(
        string candidateId,
        string runId,
        string serviceInstanceId,
        string sessionId,
        GmToolIdentity tool,
        GmCommandRegistry registry,
        Action<GmOperationRecord> record)
    {
        if (string.IsNullOrWhiteSpace(candidateId) || string.IsNullOrWhiteSpace(runId) ||
            string.IsNullOrWhiteSpace(serviceInstanceId) || string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("GM 服务运行身份不完整。");
        if (tool == null)
            throw new ArgumentNullException(nameof(tool));
        tool.RequireValid();
        ServiceInstanceId = serviceInstanceId;
        CandidateId = candidateId;
        RunId = runId;
        SessionId = sessionId;
        Tool = tool;
        m_Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        if (!string.Equals(m_Registry.CommandCatalogHash, tool.commandCatalogHash, StringComparison.Ordinal))
            throw new ArgumentException("GM Tool Identity与命令目录不匹配。", nameof(tool));
        m_Record = record ?? throw new ArgumentNullException(nameof(record));
    }

    public string ServiceInstanceId { get; }
    public string CandidateId { get; }
    public string RunId { get; }
    public string SessionId { get; }
    public GmToolIdentity Tool { get; }

    public GmServiceDescription Describe() => new()
    {
        protocolVersion = GmHttpProtocol.Version,
        candidateId = CandidateId,
        runId = RunId,
        serviceInstanceId = ServiceInstanceId,
        sessionId = SessionId,
        tool = Tool,
        commands = m_Registry.Definitions.ToArray()
    };

    public async Task<GmCommandResponse> ExecuteAsync(
        GmCommandRequest request, string callerId, GmPermission permission, CancellationToken cancellation)
    {
        ArgumentNullException.ThrowIfNull(request);
        GmCommandResult result;
        if (!GmCommandSyntax.IsValidRequest(request))
            result = Reject(GmResultCode.InvalidRequest, "请求身份、命令或参数格式无效。");
        else if (string.IsNullOrWhiteSpace(callerId) || permission == GmPermission.None)
            result = Reject(GmResultCode.Unauthorized, "没有 GM 查询权限。");
        else if (!string.Equals(request.candidateId, CandidateId, StringComparison.Ordinal) ||
                 !string.Equals(request.runId, RunId, StringComparison.Ordinal) ||
                 !string.Equals(request.serviceInstanceId, ServiceInstanceId, StringComparison.Ordinal) ||
                 !string.Equals(request.sessionId, SessionId, StringComparison.Ordinal))
            result = Reject(GmResultCode.TargetEnded, "目标服务运行实例或会话已改变，请重新连接。");
        else if (!SameTool(request.tool, Tool))
            result = Reject(GmResultCode.ToolVersionMismatch, "GM工具版本、协议或命令目录与服务端不匹配。");
        else if (!m_Registry.TryGetHandler(request.commandId, out IGmCommandHandler handler))
            result = Reject(GmResultCode.UnknownCommand, $"未安装命令：{request.commandId}");
        else if (request.commandVersion != handler.Definition.version)
            result = Reject(GmResultCode.VersionMismatch, "命令版本与服务端目录不匹配。");
        else if ((permission & handler.Definition.permission) != handler.Definition.permission)
            result = Reject(GmResultCode.Unauthorized, "没有该命令所需权限。");
        else if (!ValidArguments(handler.Definition, request.arguments))
            result = Reject(GmResultCode.InvalidArguments, $"参数不匹配，用法：{handler.Definition.usage}");
        else
        {
            try
            {
                m_Record(new GmOperationRecord(request.requestId, callerId, request.commandId, GmResultCode.Unspecified, "accepted"));
                result = await handler.ExecuteAsync(request.arguments, cancellation);
            }
            catch (GmCommandFailureException exception)
            {
                result = Reject(exception.Code, exception.Message);
            }
            catch (OperationCanceledException)
            {
                result = Reject(GmResultCode.TimedOut, "服务端查询超时或请求已取消。");
            }
            catch (Exception exception)
            {
                result = Reject(GmResultCode.ExecutionFailed, $"命令执行失败：{exception.Message}");
            }
        }
        m_Record(new GmOperationRecord(request.requestId, callerId, request.commandId, result.Code, "completed"));
        return new GmCommandResponse
        {
            requestId = request.requestId,
            candidateId = CandidateId,
            runId = RunId,
            serviceInstanceId = ServiceInstanceId,
            sessionId = SessionId,
            tool = Tool,
            code = result.Code,
            completedAtUtc = DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            message = result.Message,
            sections = result.Sections
        };
    }

    static GmCommandResult Reject(GmResultCode code, string message) => new(code, message);

    static bool SameTool(GmToolIdentity left, GmToolIdentity right) =>
        left != null && right != null && left.toolId == right.toolId && left.toolVersion == right.toolVersion &&
        left.protocolVersion == right.protocolVersion && left.commandCatalogHash == right.commandCatalogHash &&
        left.bundleHash == right.bundleHash;

    static bool ValidArguments(GmCommandDefinition definition, string[] arguments)
    {
        int required = definition.arguments.Count(argument => !argument.optional);
        if (arguments.Length < required || arguments.Length > definition.arguments.Length)
            return false;
        foreach (string argument in arguments)
        {
            if (string.IsNullOrWhiteSpace(argument))
                return false;
        }
        return true;
    }
}
