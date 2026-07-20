using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ThirdPerson.ProductStartup
{
    public enum ProductStartupStage
    {
        None = 0,
        Launch = 10,
        RequestStartupPolicy = 20,
        InitializePackageAndVerifyCache = 30,
        RequestPackageVersion = 40,
        UpdatePackageManifest = 50,
        PlanCoreDownload = 60,
        AwaitCoreDownloadConsent = 70,
        DownloadCore = 80,
        ClearObsoleteCache = 90,
        LoadHotUpdateAssemblies = 100,
        EnterProductRuntime = 110,
        ClientUpdateRequired = 120,
        Completed = 130
    }

    public enum ProductStartupErrorCode
    {
        None = 0,
        ProfileMissing = 1000,
        ProfileInvalid = 1010,
        ResourceEndpointNotConfigured = 1020,
        ResourceEndpointNotHttps = 1030,
        AuthEndpointNotConfigured = 1040,
        AuthEndpointNotWss = 1050,
        StartupPolicyRequestFailed = 2000,
        StartupPolicyInvalidJson = 2010,
        StartupPolicySchemaUnsupported = 2020,
        StartupPolicyMissingField = 2030,
        StartupPolicyUnknownField = 2040,
        StartupPolicyVersionInvalid = 2050,
        ClientUpdateRequired = 2060,
        YooAssetsNotInitialized = 3000,
        PackageInitializationFailed = 3020,
        PackageVersionRequestFailed = 3030,
        ManifestUpdateFailed = 3040,
        CoreDownloadPlanningFailed = 3050,
        CoreDownloadConsentRejected = 3060,
        InsufficientDiskSpace = 3070,
        DiskSpaceQueryFailed = 3080,
        CoreDownloadFailed = 3090,
        CacheCleanupFailed = 3100,
        HotUpdateAssemblyMissing = 4000,
        HotUpdateAssemblyLoadFailed = 4010,
        ProductEntryMissing = 4020,
        ProductEntryInvocationFailed = 4030,
        Cancelled = 9000,
        UnexpectedFailure = 9999
    }

    public readonly struct ClientBuildVersion : IEquatable<ClientBuildVersion>, IComparable<ClientBuildVersion>
    {
        public ClientBuildVersion(int major, int minor, int patch, int revision = 0)
        {
            if (major < 0 || minor < 0 || patch < 0 || revision < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(major));
            }

            Major = major;
            Minor = minor;
            Patch = patch;
            Revision = revision;
        }

        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public int Revision { get; }

        public static bool TryParse(string value, out ClientBuildVersion version)
        {
            version = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var parts = value.Split('.');
            if (parts.Length < 3 || parts.Length > 4)
            {
                return false;
            }

            var numbers = new int[4];
            for (var index = 0; index < parts.Length; index++)
            {
                if (!int.TryParse(parts[index], out numbers[index]) || numbers[index] < 0)
                {
                    return false;
                }
            }

            version = new ClientBuildVersion(numbers[0], numbers[1], numbers[2], numbers[3]);
            return true;
        }

        public int CompareTo(ClientBuildVersion other)
        {
            var result = Major.CompareTo(other.Major);
            if (result != 0) return result;
            result = Minor.CompareTo(other.Minor);
            if (result != 0) return result;
            result = Patch.CompareTo(other.Patch);
            return result != 0 ? result : Revision.CompareTo(other.Revision);
        }

        public bool Equals(ClientBuildVersion other)
        {
            return CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            return obj is ClientBuildVersion other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Major;
                hashCode = (hashCode * 397) ^ Minor;
                hashCode = (hashCode * 397) ^ Patch;
                return (hashCode * 397) ^ Revision;
            }
        }

        public override string ToString()
        {
            return Revision == 0
                ? $"{Major}.{Minor}.{Patch}"
                : $"{Major}.{Minor}.{Patch}.{Revision}";
        }

        public static bool operator <(ClientBuildVersion left, ClientBuildVersion right) => left.CompareTo(right) < 0;
        public static bool operator >(ClientBuildVersion left, ClientBuildVersion right) => left.CompareTo(right) > 0;
        public static bool operator <=(ClientBuildVersion left, ClientBuildVersion right) => left.CompareTo(right) <= 0;
        public static bool operator >=(ClientBuildVersion left, ClientBuildVersion right) => left.CompareTo(right) >= 0;
    }

    public readonly struct AuthProtocolVersion : IEquatable<AuthProtocolVersion>
    {
        public AuthProtocolVersion(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            Value = value;
        }

        public int Value { get; }

        public static bool TryParse(string value, out AuthProtocolVersion version)
        {
            version = default;
            if (!int.TryParse(value, out var number) || number <= 0)
            {
                return false;
            }

            version = new AuthProtocolVersion(number);
            return true;
        }

        public bool Equals(AuthProtocolVersion other) => Value == other.Value;
        public override bool Equals(object obj) => obj is AuthProtocolVersion other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => Value.ToString();
    }

    public readonly struct ProductStartupStageResult
    {
        private ProductStartupStageResult(
            ProductStartupStage stage,
            bool succeeded,
            bool retryable,
            ProductStartupErrorCode errorCode,
            string safeError)
        {
            Stage = stage;
            Succeeded = succeeded;
            Retryable = retryable;
            ErrorCode = errorCode;
            SafeError = safeError ?? string.Empty;
        }

        public ProductStartupStage Stage { get; }
        public bool Succeeded { get; }
        public bool Retryable { get; }
        public ProductStartupErrorCode ErrorCode { get; }
        public string SafeError { get; }

        public static ProductStartupStageResult Success(ProductStartupStage stage)
        {
            return new ProductStartupStageResult(stage, true, false, ProductStartupErrorCode.None, string.Empty);
        }

        public static ProductStartupStageResult Failure(
            ProductStartupStage stage,
            ProductStartupErrorCode errorCode,
            string safeError,
            bool retryable)
        {
            return new ProductStartupStageResult(stage, false, retryable, errorCode, safeError);
        }
    }

    public sealed class ProductStartupHandoff
    {
        public ProductStartupHandoff(
            string packageName,
            string resourcePackageVersion,
            Assembly mainAssembly,
            IReadOnlyList<Assembly> hotUpdateAssemblies)
        {
            PackageName = string.IsNullOrWhiteSpace(packageName)
                ? throw new ArgumentException("Package name is required.", nameof(packageName))
                : packageName;
            ResourcePackageVersion = string.IsNullOrWhiteSpace(resourcePackageVersion)
                ? throw new ArgumentException("Resource package version is required.", nameof(resourcePackageVersion))
                : resourcePackageVersion;
            MainAssembly = mainAssembly ?? throw new ArgumentNullException(nameof(mainAssembly));
            HotUpdateAssemblies = hotUpdateAssemblies ?? throw new ArgumentNullException(nameof(hotUpdateAssemblies));
        }

        public string PackageName { get; }
        public string ResourcePackageVersion { get; }
        public Assembly MainAssembly { get; }
        public IReadOnlyList<Assembly> HotUpdateAssemblies { get; }
    }

    public interface IProductStartupHandoffStage
    {
        UniTask<ProductStartupHandoff> LoadHotUpdateAssembliesAsync(
            string packageName,
            string resourcePackageVersion,
            CancellationToken cancellationToken);

        UniTask EnterProductRuntimeAsync(ProductStartupHandoff handoff, CancellationToken cancellationToken);
    }

    public sealed class ProductStartupException : Exception
    {
        public ProductStartupException(ProductStartupErrorCode errorCode, string safeError, bool retryable)
            : base(safeError)
        {
            ErrorCode = errorCode;
            SafeError = safeError ?? string.Empty;
            Retryable = retryable;
        }

        public ProductStartupErrorCode ErrorCode { get; }
        public string SafeError { get; }
        public bool Retryable { get; }
    }
}
