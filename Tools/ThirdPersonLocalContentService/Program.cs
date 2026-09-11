using System.Net.WebSockets;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;

var usage = "usage: ThirdPerson.LocalContentService --root <dir> --pfx <file> --pfx-password <pwd> --https-port <n> --wss-port <n> --auth-upstream <ws-uri>";

if (args.Length != 12)
{
    Console.Error.WriteLine(usage);
    return 2;
}

var values = new Dictionary<string, string>();
for (var i = 0; i + 1 < args.Length; i += 2)
{
    values[args[i]] = args[i + 1];
}

string[] required = ["--root", "--pfx", "--pfx-password", "--https-port", "--wss-port", "--auth-upstream"];
if (required.Any(key => !values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value)))
{
    Console.Error.WriteLine(usage);
    return 2;
}

var root = Path.GetFullPath(values["--root"]);
if (!Directory.Exists(root))
{
    Console.Error.WriteLine($"content root does not exist: {root}");
    return 2;
}

var pfx = Path.GetFullPath(values["--pfx"]);
if (!File.Exists(pfx))
{
    Console.Error.WriteLine($"certificate does not exist: {pfx}");
    return 2;
}

if (!int.TryParse(values["--https-port"], out var httpsPort) ||
    !int.TryParse(values["--wss-port"], out var wssPort))
{
    Console.Error.WriteLine("ports must be integers.");
    return 2;
}

if (!Uri.TryCreate(values["--auth-upstream"], UriKind.Absolute, out var authUpstream) ||
    (authUpstream.Scheme != "ws" && authUpstream.Scheme != "wss"))
{
    Console.Error.WriteLine("auth upstream must be an absolute ws:// or wss:// URI.");
    return 2;
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.ListenLocalhost(httpsPort, listener => listener.UseHttps(pfx, values["--pfx-password"]));
    kestrel.ListenLocalhost(wssPort, listener => listener.UseHttps(pfx, values["--pfx-password"]));
});

var app = builder.Build();
var contentTypes = new FileExtensionContentTypeProvider();

app.UseWebSockets();
app.Run(async context =>
{
    if (context.Connection.LocalPort == wssPort)
    {
        await ProxyAuthWebSocketAsync(context, authUpstream);
        return;
    }

    if (context.Request.Method != HttpMethods.Get && context.Request.Method != HttpMethods.Head)
    {
        context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
        return;
    }

    var relative = (context.Request.Path.Value ?? "/").TrimStart('/');
    if (relative.Length == 0)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var fullPath = Path.GetFullPath(Path.Combine(root, relative));
    if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
        !File.Exists(fullPath))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    if (!contentTypes.TryGetContentType(fullPath, out var contentType))
    {
        contentType = "application/octet-stream";
    }

    await Results.File(fullPath, contentType, enableRangeProcessing: true).ExecuteAsync(context);
});

Console.WriteLine($"content https://localhost:{httpsPort} root={root}");
Console.WriteLine($"auth wss://localhost:{wssPort} -> {authUpstream}");
app.Run();
return 0;

static async Task ProxyAuthWebSocketAsync(HttpContext context, Uri authUpstream)
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    var upstreamBuilder = new UriBuilder(authUpstream)
    {
        Path = context.Request.Path.Value ?? "/",
        Query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value!.TrimStart('?') : string.Empty
    };

    using var upstream = new ClientWebSocket();
    foreach (var protocol in context.Request.Headers.SecWebSocketProtocol)
    {
        if (!string.IsNullOrWhiteSpace(protocol))
        {
            upstream.Options.AddSubProtocol(protocol);
        }
    }

    try
    {
        await upstream.ConnectAsync(upstreamBuilder.Uri, context.RequestAborted);
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"auth upstream connect failed: {exception.Message}");
        context.Response.StatusCode = StatusCodes.Status502BadGateway;
        return;
    }

    using var client = await context.WebSockets.AcceptWebSocketAsync();
    await Task.WhenAll(
        PumpAsync(client, upstream, context.RequestAborted),
        PumpAsync(upstream, client, context.RequestAborted));
}

static async Task PumpAsync(WebSocket from, WebSocket to, CancellationToken cancellation)
{
    var buffer = new byte[16 * 1024];
    try
    {
        while (true)
        {
            var result = await from.ReceiveAsync(new ArraySegment<byte>(buffer), cancellation);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await to.CloseOutputAsync(result.CloseStatus ?? WebSocketCloseStatus.NormalClosure, result.CloseStatusDescription, cancellation);
                return;
            }

            await to.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, cancellation);
        }
    }
    catch (Exception)
    {
        try
        {
            await to.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }
        catch (Exception)
        {
        }
    }
}
