using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace ThirdPerson.ProductStartup
{
    public sealed class StartupPolicy
    {
        public const int CurrentSchemaVersion = 1;

        public StartupPolicy(int schemaVersion, ClientBuildVersion minimumClientBuildVersion)
        {
            SchemaVersion = schemaVersion;
            MinimumClientBuildVersion = minimumClientBuildVersion;
        }

        public int SchemaVersion { get; }
        public ClientBuildVersion MinimumClientBuildVersion { get; }
    }

    public readonly struct StartupPolicyResult
    {
        public StartupPolicyResult(
            StartupPolicy policy,
            ProductStartupErrorCode errorCode,
            string safeError,
            bool retryable)
        {
            Policy = policy;
            ErrorCode = errorCode;
            SafeError = safeError ?? string.Empty;
            Retryable = retryable;
        }

        public StartupPolicy Policy { get; }
        public ProductStartupErrorCode ErrorCode { get; }
        public string SafeError { get; }
        public bool Retryable { get; }
        public bool Succeeded => Policy != null && ErrorCode == ProductStartupErrorCode.None;
    }

    public interface IStartupPolicyClient
    {
        UniTask<StartupPolicyResult> RequestAsync(
            Uri policyUri,
            int timeoutSeconds,
            CancellationToken cancellationToken);
    }

    public sealed class StartupPolicyClient : IStartupPolicyClient
    {
        static readonly HashSet<string> s_AllowedFields = new HashSet<string>(StringComparer.Ordinal)
        {
            "schemaVersion",
            "minimumClientBuildVersion"
        };

        public async UniTask<StartupPolicyResult> RequestAsync(
            Uri policyUri,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            if (policyUri == null)
            {
                return Failure(ProductStartupErrorCode.ResourceEndpointNotConfigured, "Startup policy endpoint is missing.", false);
            }

            using var request = UnityWebRequest.Get(policyUri);
            request.timeout = timeoutSeconds;
            var operation = request.SendWebRequest();
            try
            {
                while (!operation.isDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                request.Abort();
                throw;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return Failure(
                    ProductStartupErrorCode.StartupPolicyRequestFailed,
                    $"Startup policy request failed with HTTP {request.responseCode}.",
                    true);
            }

            return Parse(request.downloadHandler.text);
        }

        public static StartupPolicyResult Parse(string json)
        {
            JObject document;
            try
            {
                document = JObject.Parse(json, new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                });
            }
            catch (JsonException)
            {
                return Failure(ProductStartupErrorCode.StartupPolicyInvalidJson, "Startup policy JSON is invalid.", true);
            }

            foreach (var property in document.Properties())
            {
                if (!s_AllowedFields.Contains(property.Name))
                {
                    return Failure(
                        ProductStartupErrorCode.StartupPolicyUnknownField,
                        $"Startup policy contains unknown field '{property.Name}'.",
                        true);
                }
            }

            if (!document.TryGetValue("schemaVersion", StringComparison.Ordinal, out var schemaToken) ||
                !document.TryGetValue("minimumClientBuildVersion", StringComparison.Ordinal, out var minimumToken))
            {
                return Failure(ProductStartupErrorCode.StartupPolicyMissingField, "Startup policy is missing a required field.", true);
            }

            if (schemaToken.Type != JTokenType.Integer || minimumToken.Type != JTokenType.String)
            {
                return Failure(ProductStartupErrorCode.StartupPolicyInvalidJson, "Startup policy field types are invalid.", true);
            }

            var schemaVersion = schemaToken.Value<int>();
            if (schemaVersion != StartupPolicy.CurrentSchemaVersion)
            {
                return Failure(
                    ProductStartupErrorCode.StartupPolicySchemaUnsupported,
                    $"Startup policy schema {schemaVersion} is not supported.",
                    false);
            }

            if (!ClientBuildVersion.TryParse(minimumToken.Value<string>(), out var minimumVersion))
            {
                return Failure(
                    ProductStartupErrorCode.StartupPolicyVersionInvalid,
                    "Minimum client build version is invalid.",
                    true);
            }

            return new StartupPolicyResult(
                new StartupPolicy(schemaVersion, minimumVersion),
                ProductStartupErrorCode.None,
                string.Empty,
                false);
        }

        static StartupPolicyResult Failure(ProductStartupErrorCode code, string message, bool retryable)
        {
            return new StartupPolicyResult(null, code, message, retryable);
        }
    }
}
