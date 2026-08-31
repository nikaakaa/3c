using Microsoft.AspNetCore.Http;
using ThirdPersonSimulation.DeterministicRollback;

namespace ThirdPerson.Development.Gm.Rollback;

public sealed class RollbackRelayQueryBridge : IDisposable
{
    readonly GmHttpServer m_Server;
    readonly RelayQueryQueue m_Queue;
    readonly RollbackRelayQueryIdentity m_Identity;

    public RollbackRelayQueryBridge(RelayQueryManifest configuration, DeterministicRollbackServerManifest manifest,
        RollbackInputRelayRuntime runtime, Action<string> record)
    {
        configuration.RequireValid();
        if (configuration.buildId != manifest.buildId || configuration.sessionId != manifest.sessionId)
            throw new ArgumentException("Relay 查询配置与 Gameplay manifest 身份不同。");
        m_Identity = new RollbackRelayQueryIdentity(GmHttpProtocol.Version, manifest.buildId, manifest.sessionId, Guid.NewGuid().ToString("N"));
        var source = new RollbackRelayQuerySource(manifest, runtime);
        m_Queue = new RelayQueryQueue(configuration.maximumQueuedQueries, configuration.maximumQueriesPerPump);
        m_Server = new GmHttpServer(configuration.http, record);
        m_Server.Get("/v1/identity", _ => Task.FromResult<object>(m_Identity));
        Map("/v1/session", source.CaptureSession);
        Map("/v1/actors", source.CaptureActors);
        Map("/v1/runtime", source.CaptureRuntime);
    }

    public string InstanceId => m_Identity.InstanceId;

    void Map<T>(string path, Func<T> read)
    {
        m_Server.Get(path, async context =>
        {
            if (context.Request.Headers[GmHttpProtocol.RelayInstanceHeader].ToString() != m_Identity.InstanceId)
                throw new BadHttpRequestException("Relay 运行实例已经改变。", StatusCodes.Status409Conflict);
            T value = await m_Queue.ReadAsync(read, context.RequestAborted);
            return new RollbackRelayQueryResult<T>(m_Identity.InstanceId, m_Identity.SessionId, value);
        });
    }

    public void Start(CancellationToken cancellation) => m_Server.StartAsync(cancellation).GetAwaiter().GetResult();
    public void Pump() => m_Queue.Pump();
    public void Dispose() => m_Server.DisposeAsync().AsTask().GetAwaiter().GetResult();
}
