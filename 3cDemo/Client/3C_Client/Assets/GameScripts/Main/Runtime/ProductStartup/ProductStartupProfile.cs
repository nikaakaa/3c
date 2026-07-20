using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPerson.ProductStartup
{
    [CreateAssetMenu(fileName = "ProductStartupProfile", menuName = "Third Person/Product Startup/Profile")]
    public sealed class ProductStartupProfile : ScriptableObject
    {
        public const string StartupPolicyFileName = "StartupPolicy.json";

        [SerializeField] string m_ResourceEndpoint = string.Empty;
        [SerializeField] string m_AuthEndpoint = string.Empty;
        [SerializeField] string m_ClientBuildVersion = string.Empty;
        [SerializeField] string m_AuthProtocolVersion = string.Empty;
        [SerializeField, Min(0)] int m_RequestTimeoutSeconds;
        [SerializeField, Range(0, 64)] int m_DownloadMaxConcurrency;
        [SerializeField, Range(0, 10)] int m_DownloadRetryCount;
        [SerializeField, Range(0, 64)] int m_DownloadMaxRequestPerFrame;
        [SerializeField, Min(0)] int m_DownloadWatchDogSeconds;
        [SerializeField, Range(0, 64)] int m_CacheVerifyMaxConcurrency;
        [SerializeField, Min(0)] long m_ResumeDownloadMinimumBytes;
        [SerializeField] long[] m_ResumeDownloadResponseCodes;
        [SerializeField, Min(0)] long m_DiskSafetyMarginBytes;
        [SerializeField] ProductMemoryBudgetProfile m_PlatformMemoryBudget;

        public string ResourceEndpoint => m_ResourceEndpoint;
        public string AuthEndpoint => m_AuthEndpoint;
        public string ClientBuildVersionText => m_ClientBuildVersion;
        public string AuthProtocolVersionText => m_AuthProtocolVersion;
        public int RequestTimeoutSeconds => m_RequestTimeoutSeconds;
        public int DownloadMaxConcurrency => m_DownloadMaxConcurrency;
        public int DownloadRetryCount => m_DownloadRetryCount;
        public int DownloadMaxRequestPerFrame => m_DownloadMaxRequestPerFrame;
        public int DownloadWatchDogSeconds => m_DownloadWatchDogSeconds;
        public int CacheVerifyMaxConcurrency => m_CacheVerifyMaxConcurrency;
        public long ResumeDownloadMinimumBytes => m_ResumeDownloadMinimumBytes;
        public IReadOnlyList<long> ResumeDownloadResponseCodes => m_ResumeDownloadResponseCodes;
        public long DiskSafetyMarginBytes => m_DiskSafetyMarginBytes;
        public ProductMemoryBudgetProfile PlatformMemoryBudget => m_PlatformMemoryBudget;

        public Uri StartupPolicyUri
        {
            get
            {
                var endpoint = new Uri(EnsureTrailingSlash(m_ResourceEndpoint), UriKind.Absolute);
                return new Uri(endpoint, StartupPolicyFileName);
            }
        }

        public bool TryGetClientBuildVersion(out ClientBuildVersion version)
        {
            return ClientBuildVersion.TryParse(m_ClientBuildVersion, out version);
        }

        public bool TryGetAuthProtocolVersion(out AuthProtocolVersion version)
        {
            return AuthProtocolVersion.TryParse(m_AuthProtocolVersion, out version);
        }

        public bool TryValidate(out ProductStartupErrorCode errorCode, out string safeError)
        {
            if (string.IsNullOrWhiteSpace(m_ResourceEndpoint))
            {
                errorCode = ProductStartupErrorCode.ResourceEndpointNotConfigured;
                safeError = "Resource endpoint is not configured.";
                return false;
            }

            if (!TryValidateEndpoint(m_ResourceEndpoint, "https", out safeError))
            {
                errorCode = ProductStartupErrorCode.ResourceEndpointNotHttps;
                return false;
            }

            if (string.IsNullOrWhiteSpace(m_AuthEndpoint))
            {
                errorCode = ProductStartupErrorCode.AuthEndpointNotConfigured;
                safeError = "Auth endpoint is not configured.";
                return false;
            }

            if (!TryValidateEndpoint(m_AuthEndpoint, "wss", out safeError))
            {
                errorCode = ProductStartupErrorCode.AuthEndpointNotWss;
                return false;
            }

            var authEndpoint = new Uri(m_AuthEndpoint, UriKind.Absolute);
            if (!string.IsNullOrEmpty(authEndpoint.AbsolutePath) && authEndpoint.AbsolutePath != "/")
            {
                errorCode = ProductStartupErrorCode.AuthEndpointNotWss;
                safeError = "Auth endpoint path must be '/'.";
                return false;
            }

            if (!TryGetClientBuildVersion(out _))
            {
                errorCode = ProductStartupErrorCode.ProfileInvalid;
                safeError = "Client build version must contain three or four non-negative numeric components.";
                return false;
            }

            if (!TryGetAuthProtocolVersion(out _))
            {
                errorCode = ProductStartupErrorCode.ProfileInvalid;
                safeError = "Auth protocol version must be a positive integer.";
                return false;
            }

            if (m_RequestTimeoutSeconds <= 0 ||
                m_DownloadMaxConcurrency <= 0 ||
                m_DownloadRetryCount < 0 ||
                m_DownloadMaxRequestPerFrame <= 0 ||
                m_DownloadWatchDogSeconds <= 0 ||
                m_CacheVerifyMaxConcurrency <= 0 ||
                m_ResumeDownloadMinimumBytes <= 0 ||
                m_DiskSafetyMarginBytes <= 0)
            {
                errorCode = ProductStartupErrorCode.ProfileInvalid;
                safeError = "Startup download and cache parameters must be positive.";
                return false;
            }

            if (m_ResumeDownloadResponseCodes == null || m_ResumeDownloadResponseCodes.Length == 0)
            {
                errorCode = ProductStartupErrorCode.ProfileInvalid;
                safeError = "Resume response code set is required.";
                return false;
            }

            var responseCodes = new HashSet<long>();
            foreach (var responseCode in m_ResumeDownloadResponseCodes)
            {
                if (responseCode < 200 || responseCode > 599 || !responseCodes.Add(responseCode))
                {
                    errorCode = ProductStartupErrorCode.ProfileInvalid;
                    safeError = "Resume response codes must be unique HTTP status codes.";
                    return false;
                }
            }

            if (m_PlatformMemoryBudget == null ||
                m_PlatformMemoryBudget.HomeBytes <= 0 ||
                m_PlatformMemoryBudget.GameplayBytes <= 0)
            {
                errorCode = ProductStartupErrorCode.ProfileInvalid;
                safeError = "A formal platform memory budget is required.";
                return false;
            }

            errorCode = ProductStartupErrorCode.None;
            safeError = string.Empty;
            return true;
        }

        static bool TryValidateEndpoint(string value, string requiredScheme, out string safeError)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var endpoint) ||
                !string.Equals(endpoint.Scheme, requiredScheme, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(endpoint.Host) ||
                !string.IsNullOrEmpty(endpoint.UserInfo) ||
                !string.IsNullOrEmpty(endpoint.Query) ||
                !string.IsNullOrEmpty(endpoint.Fragment))
            {
                safeError = $"Endpoint must be an absolute {requiredScheme.ToUpperInvariant()} URL without credentials, query, or fragment.";
                return false;
            }

            safeError = string.Empty;
            return true;
        }

        static string EnsureTrailingSlash(string value)
        {
            return value.EndsWith("/", StringComparison.Ordinal) ? value : value + "/";
        }
    }
}
