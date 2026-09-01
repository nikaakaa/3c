using System.Net;

namespace ThirdPerson.Development.Gm.Rollback;

public sealed class HttpRollbackGmQuerySource : IRollbackGmQuerySource
{
    readonly GmHttpClient m_Client;
    readonly RollbackRelayQueryIdentity m_Target;

    public HttpRollbackGmQuerySource(GmHttpClient client, RollbackRelayQueryIdentity target)
    {
        m_Client = client;
        m_Target = target;
    }

    public Task<RollbackGmSessionSnapshot> CaptureSessionAsync(CancellationToken cancellation) =>
        QueryAsync<RollbackGmSessionSnapshot>("/v1/session", cancellation);
    public Task<RollbackGmActorSnapshot[]> CaptureActorsAsync(CancellationToken cancellation) =>
        QueryAsync<RollbackGmActorSnapshot[]>("/v1/actors", cancellation);
    public Task<RollbackGmRuntimeSnapshot> CaptureRuntimeAsync(CancellationToken cancellation) =>
        QueryAsync<RollbackGmRuntimeSnapshot>("/v1/runtime", cancellation);

    async Task<T> QueryAsync<T>(string path, CancellationToken cancellation)
    {
        try
        {
            RollbackRelayQueryResult<T> result = await m_Client.GetAsync<RollbackRelayQueryResult<T>>(
                path, m_Target.InstanceId, cancellation);
            if (result.RelayInstanceId != m_Target.InstanceId ||
                result.CandidateId != m_Target.CandidateId || result.RunId != m_Target.RunId ||
                result.SessionId != m_Target.SessionId)
                throw new GmCommandFailureException(GmResultCode.TargetEnded, "Relay 查询响应属于其它运行实例。");
            return result.Value;
        }
        catch (GmHttpResponseException exception)
        {
            throw new GmCommandFailureException(
                exception.Status == HttpStatusCode.Conflict ? GmResultCode.TargetEnded : GmResultCode.TargetUnavailable,
                exception.Message);
        }
        catch (HttpRequestException)
        {
            throw new GmCommandFailureException(GmResultCode.TargetUnavailable, "Relay 查询连接不可用。");
        }
    }
}
