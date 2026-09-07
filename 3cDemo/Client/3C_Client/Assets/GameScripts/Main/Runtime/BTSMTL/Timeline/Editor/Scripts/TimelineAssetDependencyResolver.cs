using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;

namespace BTSMTL.Timeline.Editor
{
    public sealed class TimelineAssetDependencyReport
    {
        internal TimelineAssetDependencyReport(
            string rootAssetPath,
            TimelineContentDiscoveryResult content,
            IReadOnlyList<string> assetDependencies)
        {
            RootAssetPath = rootAssetPath ?? string.Empty;
            Content = content;
            AssetDependencies = new ReadOnlyCollection<string>(new List<string>(assetDependencies ?? Array.Empty<string>()));
        }

        public string RootAssetPath { get; }
        public TimelineContentDiscoveryResult Content { get; }
        public IReadOnlyList<string> AssetDependencies { get; }
        public bool IsValid => Content != null && Content.IsValid && AssetDependencies.Count > 0;
    }

    public static class TimelineAssetDependencyResolver
    {
        public static TimelineAssetDependencyReport Resolve(string rootAssetPath, TimelineContractCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (string.IsNullOrWhiteSpace(rootAssetPath))
                throw new ArgumentException("Timeline root asset path is required.", nameof(rootAssetPath));
            string path = rootAssetPath.Trim();
            TimelineAsset asset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if (!asset)
                throw new InvalidOperationException($"Timeline root asset '{path}' does not exist or is not a TimelineAsset.");
            string[] dependencies = AssetDatabase.GetDependencies(path, true);
            Array.Sort(dependencies, StringComparer.Ordinal);
            return new TimelineAssetDependencyReport(
                path,
                TimelineContentDiscovery.Discover(asset, catalog),
                dependencies);
        }
    }
}
