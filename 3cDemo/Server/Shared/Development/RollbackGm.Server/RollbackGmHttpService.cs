using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace ThirdPerson.Development.Gm.Rollback;

public sealed class RollbackGmHttpService : IAsyncDisposable
{
    readonly GmServerManifest m_Manifest;
    readonly GmHttpQueryClient m_Relay;
    readonly GmHttpServer m_Server;
    readonly Action<GmOperationRecord> m_Record;
    readonly string m_InstanceId = Guid.NewGuid().ToString("N");

    public RollbackGmHttpService(GmServerManifest manifest, Action<GmOperationRecord> operationLog, Action<string> serviceLog)
    {
        manifest.RequireValid();
        m_Manifest = manifest;
        m_Record = operationLog;
        m_Relay = new GmHttpQueryClient(manifest.relayQueryEndpoint, manifest.relayQueryToken,
            manifest.http.maximumMessageBytes, manifest.relayQueryTimeoutMilliseconds);
        m_Server = new GmHttpServer(manifest.http, serviceLog);
        m_Server.Get(GmHttpProtocol.ServicePath, async context =>
            (await CreateDispatcherAsync(context.RequestAborted)).Describe());
        m_Server.Post(GmHttpProtocol.CommandsPath, async context =>
        {
            if (!context.Request.HasJsonContentType())
                throw new BadHttpRequestException("GM 命令必须使用 JSON。", StatusCodes.Status415UnsupportedMediaType);
            GmCommandRequest request = await JsonSerializer.DeserializeAsync<GmCommandRequest>(
                context.Request.Body, GmHttpJson.Options, context.RequestAborted) ??
                throw new BadHttpRequestException("GM 请求为空。");
            GmCommandDispatcher dispatcher = await CreateDispatcherAsync(context.RequestAborted);
            return await dispatcher.ExecuteAsync(request, "local-development-console", GmPermission.Read, context.RequestAborted);
        });
    }

    public async Task StartAsync(CancellationToken cancellation)
    {
        _ = await CreateDispatcherAsync(cancellation);
        await m_Server.StartAsync(cancellation);
    }

    async Task<GmCommandDispatcher> CreateDispatcherAsync(CancellationToken cancellation)
    {
        RollbackRelayQueryIdentity target = await m_Relay.GetAsync<RollbackRelayQueryIdentity>("/v1/identity", null, cancellation);
        if (target.ProtocolVersion != GmHttpProtocol.Version || target.BuildId != m_Manifest.buildId ||
            target.SessionId != m_Manifest.sessionId || !Guid.TryParseExact(target.InstanceId, "N", out _))
            throw new InvalidDataException("GM 服务连接的 Relay 版本、构建或会话身份不匹配。");
        var source = new HttpRollbackGmQuerySource(m_Relay, target);
        GmCommandRegistry commands = RollbackGmCommandModule.CreateRegistry(source);
        return new GmCommandDispatcher($"{m_InstanceId}.{target.InstanceId}", target.SessionId, target.BuildId, commands, m_Record);
    }

    public async ValueTask DisposeAsync()
    {
        await m_Server.DisposeAsync();
        m_Relay.Dispose();
    }
}
