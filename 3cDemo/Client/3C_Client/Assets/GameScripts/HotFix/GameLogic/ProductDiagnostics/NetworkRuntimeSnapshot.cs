using System;

namespace GameLogic.ProductDiagnostics
{
    public sealed class NetworkRuntimeSnapshot
    {
        public NetworkRuntimeSnapshot(DateTimeOffset capturedAt, string productId, string transport, bool tlsEnabled, string redactedEndpoint, string connectionState, string redactedAccountId, string redactedClientInstanceId, long sessionGeneration, DateTimeOffset? tokenExpiresAt, long roundTripMilliseconds, string lastErrorCode)
        {
            CapturedAt = capturedAt;
            ProductId = productId ?? string.Empty;
            Transport = transport ?? string.Empty;
            TlsEnabled = tlsEnabled;
            RedactedEndpoint = redactedEndpoint ?? string.Empty;
            ConnectionState = connectionState ?? string.Empty;
            RedactedAccountId = redactedAccountId ?? string.Empty;
            RedactedClientInstanceId = redactedClientInstanceId ?? string.Empty;
            SessionGeneration = sessionGeneration;
            TokenExpiresAt = tokenExpiresAt;
            RoundTripMilliseconds = roundTripMilliseconds;
            LastErrorCode = lastErrorCode ?? string.Empty;
        }

        public DateTimeOffset CapturedAt { get; }
        public string ProductId { get; }
        public string Transport { get; }
        public bool TlsEnabled { get; }
        public string RedactedEndpoint { get; }
        public string ConnectionState { get; }
        public string RedactedAccountId { get; }
        public string RedactedClientInstanceId { get; }
        public long SessionGeneration { get; }
        public DateTimeOffset? TokenExpiresAt { get; }
        public long RoundTripMilliseconds { get; }
        public string LastErrorCode { get; }
    }

    public interface INetworkRuntimeSnapshotSource
    {
        NetworkRuntimeSnapshot Current { get; }
        event Action<NetworkRuntimeSnapshot> Changed;
    }

    public static class ProductDiagnosticRedaction
    {
        public static string EndpointHost(string endpoint)
        {
            return Uri.TryCreate(endpoint, UriKind.Absolute, out Uri uri) ? uri.Host : "invalid-endpoint";
        }

        public static string Identity(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.Length <= 4)
            {
                return "****";
            }

            return $"{value.Substring(0, 2)}***{value.Substring(value.Length - 2, 2)}";
        }
    }
}
