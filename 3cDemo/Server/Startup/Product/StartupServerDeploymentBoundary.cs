namespace ThirdPerson.Startup.Server;

internal static class StartupServerDeploymentBoundary
{
    const string PublicEndpointVariable = "THIRDPERSON_STARTUP_PUBLIC_WSS_ENDPOINT";
    const string TlsTerminationVariable = "THIRDPERSON_STARTUP_TLS_TERMINATION";
    const string PrivateWebSocketPrefix = "http://+:21000/";

    public static void Validate()
    {
        string? endpointValue = Environment.GetEnvironmentVariable(PublicEndpointVariable);
        if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out Uri? endpoint) ||
            !string.Equals(endpoint.Scheme, "wss", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(endpoint.Host))
        {
            throw new InvalidOperationException($"A valid wss:// '{PublicEndpointVariable}' is required.");
        }

        string? termination = Environment.GetEnvironmentVariable(TlsTerminationVariable);
        if (!string.Equals(termination, "reverse-proxy", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"'{TlsTerminationVariable}' must be 'reverse-proxy'; Fantasy listens on the private WebSocket upstream only.");
        }

        try
        {
            using var listener = new System.Net.HttpListener();
            listener.Prefixes.Add(PrivateWebSocketPrefix);
            listener.Start();
            listener.Stop();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"The private WebSocket prefix '{PrivateWebSocketPrefix}' is not reserved or available.",
                exception);
        }
    }
}
