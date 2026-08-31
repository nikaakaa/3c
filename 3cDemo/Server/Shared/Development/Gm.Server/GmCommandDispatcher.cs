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
        string serviceInstanceId,
        string sessionId,
        string buildId,
        GmCommandRegistry registry,
        Action<GmOperationRecord> record)
    {
        if (string.IsNullOrWhiteSpace(serviceInstanceId) || string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("GM 服务运行身份不完整。");
        ServiceInstanceId = serviceInstanceId;
        SessionId = sessionId;
        BuildId = buildId;
        m_Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        m_Record = record ?? throw new ArgumentNullException(nameof(record));
    }

    public string ServiceInstanceId { get; }
    public string SessionId { get; }
    public string BuildId { get; }

    public GmServiceDescription Describe() => new()
    {
        protocolVersion = GmHttpProtocol.Version,
        buildId = BuildId,
        serviceInstanceId = ServiceInstanceId,
        sessionId = SessionId,
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
        else if (!string.Equals(request.serviceInstanceId, ServiceInstanceId, StringComparison.Ordinal) ||
                 !string.Equals(request.sessionId, SessionId, StringComparison.Ordinal))
            result = Reject(GmResultCode.TargetEnded, "目标服务运行实例或会话已改变，请重新连接。");
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
            serviceInstanceId = ServiceInstanceId,
            sessionId = SessionId,
            code = result.Code,
            completedAtUtc = DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            message = result.Message,
            sections = result.Sections
        };
    }

    static GmCommandResult Reject(GmResultCode code, string message) => new(code, message);

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
