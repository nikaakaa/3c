using System;
using System.Collections.Generic;
using YooAsset;

namespace TEngine
{
    public sealed class ResourcePackageInitializationOptions
    {
        readonly long[] m_ResumeDownloadResponseCodes;

        public ResourcePackageInitializationOptions(
            string packageName,
            string resourceEndpoint,
            EFileVerifyLevel fileVerifyLevel,
            int fileVerifyMaxConcurrency,
            int downloadMaxConcurrency,
            int downloadMaxRequestPerFrame,
            int downloadWatchDogTimeSeconds,
            long resumeDownloadMinimumSize,
            IReadOnlyList<long> resumeDownloadResponseCodes)
        {
            PackageName = packageName?.Trim();
            ResourceEndpoint = resourceEndpoint?.TrimEnd('/');
            FileVerifyLevel = fileVerifyLevel;
            FileVerifyMaxConcurrency = fileVerifyMaxConcurrency;
            DownloadMaxConcurrency = downloadMaxConcurrency;
            DownloadMaxRequestPerFrame = downloadMaxRequestPerFrame;
            DownloadWatchDogTimeSeconds = downloadWatchDogTimeSeconds;
            ResumeDownloadMinimumSize = resumeDownloadMinimumSize;
            m_ResumeDownloadResponseCodes = resumeDownloadResponseCodes == null
                ? Array.Empty<long>()
                : Copy(resumeDownloadResponseCodes);
        }

        public string PackageName { get; }
        public string ResourceEndpoint { get; }
        public EFileVerifyLevel FileVerifyLevel { get; }
        public int FileVerifyMaxConcurrency { get; }
        public int DownloadMaxConcurrency { get; }
        public int DownloadMaxRequestPerFrame { get; }
        public int DownloadWatchDogTimeSeconds { get; }
        public long ResumeDownloadMinimumSize { get; }
        public IReadOnlyList<long> ResumeDownloadResponseCodes => m_ResumeDownloadResponseCodes;

        internal void Validate(EPlayMode playMode)
        {
            if (string.IsNullOrWhiteSpace(PackageName))
            {
                throw new ArgumentException("Package name is required.", nameof(PackageName));
            }

            if (FileVerifyMaxConcurrency <= 0 ||
                DownloadMaxConcurrency <= 0 ||
                DownloadMaxRequestPerFrame <= 0 ||
                DownloadWatchDogTimeSeconds <= 0 ||
                ResumeDownloadMinimumSize <= 0)
            {
                throw new ArgumentException("Resource package limits must be positive.");
            }

            if (m_ResumeDownloadResponseCodes.Length == 0)
            {
                throw new ArgumentException("Resume response codes are required.");
            }

            var uniqueCodes = new HashSet<long>();
            foreach (long responseCode in m_ResumeDownloadResponseCodes)
            {
                if (responseCode < 200 || responseCode > 599 || !uniqueCodes.Add(responseCode))
                {
                    throw new ArgumentException("Resume response codes must be unique HTTP status codes.");
                }
            }

            bool requiresRemoteEndpoint = playMode == EPlayMode.HostPlayMode ||
                                          playMode == EPlayMode.WebPlayMode;
            if (!requiresRemoteEndpoint)
            {
                return;
            }

            if (!Uri.TryCreate(ResourceEndpoint, UriKind.Absolute, out Uri endpoint) ||
                !string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(endpoint.Host) ||
                !string.IsNullOrEmpty(endpoint.UserInfo) ||
                !string.IsNullOrEmpty(endpoint.Query) ||
                !string.IsNullOrEmpty(endpoint.Fragment))
            {
                throw new ArgumentException("Resource endpoint must be an absolute HTTPS URL without credentials, query, or fragment.");
            }
        }

        internal List<long> CopyResumeDownloadResponseCodes()
        {
            return new List<long>(m_ResumeDownloadResponseCodes);
        }

        static long[] Copy(IReadOnlyList<long> source)
        {
            var result = new long[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                result[i] = source[i];
            }
            return result;
        }
    }

    public readonly struct ResourcePackageInitializationResult
    {
        public ResourcePackageInitializationResult(
            InitializationOperation operation,
            int validCacheFileCount,
            int invalidCacheFileCount)
        {
            Operation = operation;
            ValidCacheFileCount = validCacheFileCount;
            InvalidCacheFileCount = invalidCacheFileCount;
        }

        public InitializationOperation Operation { get; }
        public int ValidCacheFileCount { get; }
        public int InvalidCacheFileCount { get; }
    }

    internal sealed class SingleEndpointRemoteServices : IRemoteServices
    {
        readonly string m_Endpoint;

        public SingleEndpointRemoteServices(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("Resource endpoint is required.", nameof(endpoint));
            }
            m_Endpoint = endpoint.TrimEnd('/');
        }

        public string GetRemoteMainURL(string fileName) => $"{m_Endpoint}/{fileName}";
        public string GetRemoteFallbackURL(string fileName) => $"{m_Endpoint}/{fileName}";
    }
}
