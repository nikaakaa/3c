using System;
using Unity.Profiling;
using UnityEngine;
using ThirdPerson.ProductStartup;

namespace GameLogic.ProductDiagnostics
{
    public sealed class MemoryRuntimeSnapshot
    {
        public MemoryRuntimeSnapshot(DateTimeOffset capturedAt, bool countersValid, long totalUsedBytes, long totalReservedBytes, long gcUsedBytes, long gcReservedBytes, long textureBytes, long meshBytes, int activeScopeCount, int activeLeaseCount, int liveInstanceCount, string budgetName, long budgetBytes, string configurationError)
        {
            CapturedAt = capturedAt;
            CountersValid = countersValid;
            TotalUsedBytes = totalUsedBytes;
            TotalReservedBytes = totalReservedBytes;
            GcUsedBytes = gcUsedBytes;
            GcReservedBytes = gcReservedBytes;
            TextureBytes = textureBytes;
            MeshBytes = meshBytes;
            ActiveScopeCount = activeScopeCount;
            ActiveLeaseCount = activeLeaseCount;
            LiveInstanceCount = liveInstanceCount;
            BudgetName = budgetName;
            BudgetBytes = budgetBytes;
            ConfigurationError = configurationError;
        }

        public DateTimeOffset CapturedAt { get; }
        public bool CountersValid { get; }
        public long TotalUsedBytes { get; }
        public long TotalReservedBytes { get; }
        public long GcUsedBytes { get; }
        public long GcReservedBytes { get; }
        public long TextureBytes { get; }
        public long MeshBytes { get; }
        public int ActiveScopeCount { get; }
        public int ActiveLeaseCount { get; }
        public int LiveInstanceCount { get; }
        public string BudgetName { get; }
        public long BudgetBytes { get; }
        public string ConfigurationError { get; }
        public bool IsOverBudget => string.IsNullOrEmpty(ConfigurationError) && BudgetBytes > 0 && TotalUsedBytes > BudgetBytes;
    }

    public enum ProductMemoryBudgetKind
    {
        Home = 0,
        Gameplay = 1
    }

    public sealed class ProductMemorySampler : IDisposable
    {
        private ProfilerRecorder _totalUsed;
        private ProfilerRecorder _totalReserved;
        private ProfilerRecorder _gcUsed;
        private ProfilerRecorder _gcReserved;
        private ProfilerRecorder _texture;
        private ProfilerRecorder _mesh;

        public ProductMemorySampler()
        {
            _totalUsed = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
            _totalReserved = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Reserved Memory");
            _gcUsed = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Used Memory");
            _gcReserved = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
            _texture = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Texture Memory");
            _mesh = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Mesh Memory");
        }

        public MemoryRuntimeSnapshot Capture(ResourceRuntimeSnapshot resources, ProductMemoryBudgetProfile profile, ProductMemoryBudgetKind budgetKind)
        {
            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            string error = string.Empty;
            long budget = 0;
            string budgetName = budgetKind.ToString();
            if (profile == null)
            {
                error = "Product memory budget profile is missing.";
            }
            else
            {
                budget = budgetKind == ProductMemoryBudgetKind.Home ? profile.HomeBytes : profile.GameplayBytes;
                if (budget <= 0)
                {
                    error = $"Memory budget is missing for {budgetKind}.";
                }
            }

            bool valid = _totalUsed.Valid && _totalReserved.Valid && _gcUsed.Valid && _gcReserved.Valid && _texture.Valid && _mesh.Valid;
            return new MemoryRuntimeSnapshot(
                DateTimeOffset.UtcNow,
                valid,
                Read(_totalUsed),
                Read(_totalReserved),
                Read(_gcUsed),
                Read(_gcReserved),
                Read(_texture),
                Read(_mesh),
                resources.Scopes.Count,
                resources.ActiveLeaseCount,
                resources.LiveInstanceCount,
                budgetName,
                budget,
                error);
        }

        public void Dispose()
        {
            _totalUsed.Dispose();
            _totalReserved.Dispose();
            _gcUsed.Dispose();
            _gcReserved.Dispose();
            _texture.Dispose();
            _mesh.Dispose();
        }

        private static long Read(ProfilerRecorder recorder) => recorder.Valid ? recorder.LastValue : 0;
    }
}
