using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ThirdPerson.Development.Gm;

public sealed class GmHttpServer : IAsyncDisposable
{
    readonly WebApplication m_App;
    readonly SemaphoreSlim m_Requests;
    readonly byte[] m_Credential;
    readonly GmHttpServerConfiguration m_Configuration;
    readonly Action<string> m_Record;

    public GmHttpServer(GmHttpServerConfiguration configuration, Action<string> record)
    {
        configuration.RequireValid();
        m_Configuration = configuration;
        m_Record = record;
        m_Credential = Encoding.UTF8.GetBytes("Bearer " + configuration.accessToken);
        m_Requests = new SemaphoreSlim(configuration.maximumConcurrentRequests);
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            Args = Array.Empty<string>(),
            ContentRootPath = AppContext.BaseDirectory,
            EnvironmentName = "DevelopmentTools"
        });
        builder.Configuration.Sources.Clear();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(server =>
        {
            server.AddServerHeader = false;
            server.Listen(IPAddress.Loopback, configuration.listenPort);
            server.Limits.MaxRequestBodySize = configuration.maximumMessageBytes;
            server.Limits.MaxConcurrentConnections = configuration.maximumConcurrentRequests * 2;
            server.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(5);
            server.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(15);
        });
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.IncludeFields = true;
            options.SerializerOptions.PropertyNamingPolicy = null;
            options.SerializerOptions.PropertyNameCaseInsensitive = false;
            options.SerializerOptions.UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow;
            options.SerializerOptions.MaxDepth = 16;
        });
        m_App = builder.Build();
        m_App.Use(async (context, next) =>
        {
            byte[] credential = Encoding.UTF8.GetBytes(context.Request.Headers.Authorization.ToString());
            if (context.Connection.RemoteIpAddress == null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) ||
                !CryptographicOperations.FixedTimeEquals(credential, m_Credential))
            {
                m_Record("access=denied");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            if (!m_Requests.Wait(0))
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                return;
            }
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            timeout.CancelAfter(configuration.requestTimeoutMilliseconds);
            context.RequestAborted = timeout.Token;
            try
            {
                await next(context);
            }
            catch (OperationCanceledException)
            {
                if (!context.Response.HasStarted)
                    context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
            }
            catch (GmRemoteQueryException exception)
            {
                m_Record($"query=failed status={(int)exception.Status}");
                if (!context.Response.HasStarted)
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            }
            catch (BadHttpRequestException exception)
            {
                if (!context.Response.HasStarted)
                    context.Response.StatusCode = exception.StatusCode;
            }
            catch (Exception exception)
            {
                m_Record($"request=failed type={exception.GetType().Name}");
                if (!context.Response.HasStarted)
                    context.Response.StatusCode = exception is System.Text.Json.JsonException
                        ? StatusCodes.Status400BadRequest : StatusCodes.Status503ServiceUnavailable;
            }
            finally
            {
                m_Requests.Release();
            }
        });
    }

    public void Get(string path, Func<HttpContext, Task<object>> handler) => Map(path, "GET", handler);
    public void Post(string path, Func<HttpContext, Task<object>> handler) => Map(path, "POST", handler);

    void Map(string path, string method, Func<HttpContext, Task<object>> handler)
    {
        m_App.MapMethods(path, new[] { method }, async context =>
        {
            object value = await handler(context);
            byte[] payload = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(value, value.GetType(), GmHttpJson.Options);
            if (payload.Length > m_Configuration.maximumMessageBytes)
                throw new InvalidDataException("工具结果超过正式消息容量。");
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength = payload.Length;
            await context.Response.Body.WriteAsync(payload, context.RequestAborted);
        });
    }

    public Task StartAsync(CancellationToken cancellation) => m_App.StartAsync(cancellation);

    public async ValueTask DisposeAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await m_App.StopAsync(timeout.Token);
        await m_App.DisposeAsync();
        m_Requests.Dispose();
    }
}
