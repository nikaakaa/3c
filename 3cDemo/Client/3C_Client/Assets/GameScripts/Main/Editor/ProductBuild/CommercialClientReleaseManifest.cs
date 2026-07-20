using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ThirdPersonCharacter.Editor.ProductBuild
{
    [Serializable]
    internal sealed class CommercialReleaseFile
    {
        [JsonProperty("path", Order = 1)] public string Path;
        [JsonProperty("length", Order = 2)] public long Length;
        [JsonProperty("sha256", Order = 3)] public string Sha256;
    }

    [Serializable]
    internal sealed class CommercialContentReleaseManifest
    {
        public const int CurrentSchemaVersion = 1;
        [JsonProperty("schemaVersion", Order = 1)] public int SchemaVersion;
        [JsonProperty("buildTarget", Order = 2)] public string BuildTarget;
        [JsonProperty("packageName", Order = 3)] public string PackageName;
        [JsonProperty("resourcePackageVersion", Order = 4)] public string ResourcePackageVersion;
        [JsonProperty("minimumClientBuildVersion", Order = 5)] public string MinimumClientBuildVersion;
        [JsonProperty("files", Order = 6)] public List<CommercialReleaseFile> Files;
    }

    [Serializable]
    internal sealed class CommercialPlayerReleaseManifest
    {
        public const int CurrentSchemaVersion = 1;
        [JsonProperty("schemaVersion", Order = 1)] public int SchemaVersion;
        [JsonProperty("buildTarget", Order = 2)] public string BuildTarget;
        [JsonProperty("clientBuildVersion", Order = 3)] public string ClientBuildVersion;
        [JsonProperty("packageName", Order = 4)] public string PackageName;
        [JsonProperty("resourcePackageVersion", Order = 5)] public string ResourcePackageVersion;
        [JsonProperty("contentReleaseManifestSha256", Order = 6)] public string ContentReleaseManifestSha256;
        [JsonProperty("entryPath", Order = 7)] public string EntryPath;
        [JsonProperty("files", Order = 8)] public List<CommercialReleaseFile> Files;
    }
}
