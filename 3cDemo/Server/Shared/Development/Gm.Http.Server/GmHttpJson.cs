using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThirdPerson.Development.Gm;

public static class GmHttpJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 16
    };

    public static T ReadManifest<T>(string path) where T : class =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path, System.Text.Encoding.UTF8), Options) ??
        throw new InvalidDataException("开发工具 manifest 为空。");

    public static async Task<byte[]> ReadBoundedAsync(Stream stream, int maximumBytes, CancellationToken cancellation)
    {
        using var result = new MemoryStream();
        var buffer = new byte[4096];
        int count;
        while ((count = await stream.ReadAsync(buffer.AsMemory(), cancellation)) != 0)
        {
            if (result.Length + count > maximumBytes)
                throw new InvalidDataException("工具响应超过正式消息容量。");
            result.Write(buffer, 0, count);
        }
        return result.ToArray();
    }
}

public sealed class GmRemoteQueryException : Exception
{
    public GmRemoteQueryException(HttpStatusCode status) : base($"Relay 工具查询失败，HTTP {(int)status}。") => Status = status;
    public HttpStatusCode Status { get; }
}

public sealed class GmHttpQueryClient : IDisposable
{
    readonly HttpClient m_Client;
    readonly string m_Token;
    readonly int m_MaximumBytes;
    readonly int m_TimeoutMilliseconds;

    public GmHttpQueryClient(string endpoint, string token, int maximumBytes, int timeoutMilliseconds)
    {
        GmHttpProtocol.RequireEndpoint(endpoint);
        GmHttpProtocol.RequireToken(token);
        m_Client = new HttpClient(new SocketsHttpHandler { UseProxy = false, AllowAutoRedirect = false })
        {
            BaseAddress = new Uri(endpoint),
            Timeout = Timeout.InfiniteTimeSpan
        };
        m_Token = token;
        m_MaximumBytes = maximumBytes;
        m_TimeoutMilliseconds = timeoutMilliseconds;
    }

    public async Task<T> GetAsync<T>(string path, string? relayInstanceId, CancellationToken cancellation)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(m_TimeoutMilliseconds);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", m_Token);
        if (relayInstanceId != null)
            request.Headers.Add(GmHttpProtocol.RelayInstanceHeader, relayInstanceId);
        using HttpResponseMessage response = await m_Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        if (!response.IsSuccessStatusCode)
            throw new GmRemoteQueryException(response.StatusCode);
        using Stream stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        byte[] payload = await GmHttpJson.ReadBoundedAsync(stream, m_MaximumBytes, timeout.Token);
        return JsonSerializer.Deserialize<T>(payload, GmHttpJson.Options) ?? throw new InvalidDataException("Relay 查询响应为空。");
    }

    public void Dispose() => m_Client.Dispose();
}
