using System;
using System.Collections.Generic;
using GameLogic.ProductResource;

namespace GameLogic.ProductDiagnostics
{
    public sealed class ResourceScopeSnapshot
    {
        public ResourceScopeSnapshot(ResourceScopeId id, ResourceScopeKind kind, string name, ResourceScopeState state, int leaseCount, int liveInstanceCount)
        {
            Id = id;
            Kind = kind;
            Name = name;
            State = state;
            LeaseCount = leaseCount;
            LiveInstanceCount = liveInstanceCount;
        }

        public ResourceScopeId Id { get; }
        public ResourceScopeKind Kind { get; }
        public string Name { get; }
        public ResourceScopeState State { get; }
        public int LeaseCount { get; }
        public int LiveInstanceCount { get; }
    }

    public sealed class ResourceRuntimeSnapshot
    {
        public ResourceRuntimeSnapshot(
            long sequence,
            DateTimeOffset capturedAt,
            long logicalLoadCount,
            long physicalLoadCount,
            long inFlightJoinCount,
            long cacheHitCount,
            long duplicateDisposeCount,
            int activeLeaseCount,
            int liveInstanceCount,
            int inFlightCount,
            int tEnginePoolCount,
            int tEngineAssetPoolObjectCount,
            int tEngineAssetPoolReleasableCount,
            string packageName,
            string packageVersion,
            IReadOnlyList<string> preparedTags,
            IReadOnlyList<ResourceScopeSnapshot> scopes,
            ResourceMaintenanceSnapshot lastMaintenance)
        {
            Sequence = sequence;
            CapturedAt = capturedAt;
            LogicalLoadCount = logicalLoadCount;
            PhysicalLoadCount = physicalLoadCount;
            InFlightJoinCount = inFlightJoinCount;
            CacheHitCount = cacheHitCount;
            DuplicateDisposeCount = duplicateDisposeCount;
            ActiveLeaseCount = activeLeaseCount;
            LiveInstanceCount = liveInstanceCount;
            InFlightCount = inFlightCount;
            TEnginePoolCount = tEnginePoolCount;
            TEngineAssetPoolObjectCount = tEngineAssetPoolObjectCount;
            TEngineAssetPoolReleasableCount = tEngineAssetPoolReleasableCount;
            PackageName = packageName;
            PackageVersion = packageVersion;
            PreparedTags = preparedTags;
            Scopes = scopes;
            LastMaintenance = lastMaintenance;
        }

        public long Sequence { get; }
        public DateTimeOffset CapturedAt { get; }
        public long LogicalLoadCount { get; }
        public long PhysicalLoadCount { get; }
        public long InFlightJoinCount { get; }
        public long CacheHitCount { get; }
        public long DuplicateDisposeCount { get; }
        public int ActiveLeaseCount { get; }
        public int LiveInstanceCount { get; }
        public int InFlightCount { get; }
        public int TEnginePoolCount { get; }
        public int TEngineAssetPoolObjectCount { get; }
        public int TEngineAssetPoolReleasableCount { get; }
        public string PackageName { get; }
        public string PackageVersion { get; }
        public IReadOnlyList<string> PreparedTags { get; }
        public IReadOnlyList<ResourceScopeSnapshot> Scopes { get; }
        public ResourceMaintenanceSnapshot LastMaintenance { get; }
    }

    public sealed class ResourceMaintenanceSnapshot
    {
        public ResourceMaintenanceSnapshot(ResourceMaintenanceReason reason, DateTimeOffset startedAt, DateTimeOffset completedAt, int poolObjectsBefore, int poolObjectsAfter)
        {
            Reason = reason;
            StartedAt = startedAt;
            CompletedAt = completedAt;
            PoolObjectsBefore = poolObjectsBefore;
            PoolObjectsAfter = poolObjectsAfter;
        }

        public ResourceMaintenanceReason Reason { get; }
        public DateTimeOffset StartedAt { get; }
        public DateTimeOffset CompletedAt { get; }
        public int PoolObjectsBefore { get; }
        public int PoolObjectsAfter { get; }
    }

    public interface IResourceRuntimeSnapshotSource
    {
        ResourceRuntimeSnapshot Current { get; }
        IReadOnlyList<ResourceRuntimeSnapshot> History { get; }
        event Action<ResourceRuntimeSnapshot> Changed;
    }
}
