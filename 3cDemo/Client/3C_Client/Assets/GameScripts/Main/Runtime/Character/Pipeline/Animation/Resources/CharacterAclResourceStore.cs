using System;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Animation.Resources
{
    internal sealed class CharacterAclResourceStore : IDisposable
    {
        readonly CharacterAclResourceStoreCatalog m_Catalog;
        readonly ICharacterAnimationAssetLoader m_Loader;
        readonly long m_ResidentBudgetBytes;
        long m_ResidentBytes;
        ulong m_UseClock = 1;
        ulong m_ResourceGeneration = 1;
        bool m_Closing;
        bool m_Disposed;

        internal CharacterAclResourceStore(
            ICharacterAnimationAssetLoader loader,
            CharacterAnimationResourceSettings settings)
        {
            m_Loader = loader ?? throw new ArgumentNullException(nameof(loader));
            m_ResidentBudgetBytes = settings.ResidentBudgetBytes;
            m_Catalog = new CharacterAclResourceStoreCatalog();
        }

        internal int Count => m_Catalog.Count;
        internal long ResidentBytes => m_ResidentBytes;
        internal long ResidentBudgetBytes => m_ResidentBudgetBytes;

        internal int RequireIndex(CharacterAnimationCompiledResourceDescriptor descriptor)
        {
            RequireAlive();
            return m_Catalog.RequireIndex(descriptor);
        }

        internal int Register(CharacterAnimationCompiledResourceDescriptor descriptor)
        {
            RequireOpen();
            descriptor?.RequireValid();
            string identity = descriptor?.ResourceIdentity;
            if (string.IsNullOrWhiteSpace(identity))
                throw new InvalidOperationException("ACL resource has no content identity.");
            if (m_Catalog.TryGetResourceIdentity(identity, out int existingIndex))
            {
                CharacterAnimationCompiledResourceDescriptor existingDescriptor =
                    m_Catalog.RequireEntry(existingIndex).Descriptor;
                if (!string.Equals(
                        existingDescriptor.GroupContentHash,
                        descriptor.GroupContentHash,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"ACL resource identity '{identity}' resolves to different assets.");
                if (existingDescriptor.ResourceIndex != descriptor.ResourceIndex)
                    throw new InvalidOperationException(
                        $"ACL resource identity '{identity}' has conflicting catalog indices.");
                return existingIndex;
            }
            long residentBytes = ResourceBytes(descriptor);
            if (residentBytes > m_ResidentBudgetBytes)
                throw new InvalidOperationException(
                    "ACL resource exceeds the configured resident budget.");
            var preparation = new CharacterAclAnimationResourcePreparation(descriptor, m_Loader);
            var entry = new CharacterAclResourceStoreEntry(descriptor, residentBytes, preparation)
            {
                Generation = NextResourceGeneration()
            };
            int index = m_Catalog.Count;
            m_Catalog.Add(identity, descriptor.ResourceIndex, entry);
            return index;
        }

        internal int RequireStoreIndex(int resourceCatalogIndex)
        {
            RequireAlive();
            return m_Catalog.RequireIndex(resourceCatalogIndex);
        }

        internal void Request(int resourceIndex)
        {
            RequireOpen();
            CharacterAclResourceStoreEntry entry =
                m_Catalog.RequireEntry(resourceIndex);
            if (entry.Readiness.IsInvalid || entry.Readiness.IsReady)
                return;
            if (!entry.Requested)
                entry.Requested = true;
        }

        internal CharacterAclResourceReadinessResult GetReadiness(int resourceIndex)
        {
            return GetReadiness(resourceIndex, out _);
        }

        internal CharacterAclResourceReadinessResult GetReadiness(
            int resourceIndex,
            out ulong generation)
        {
            RequireAlive();
            CharacterAclResourceStoreEntry entry =
                m_Catalog.RequireEntry(resourceIndex);
            generation = entry.Generation;
            if (generation == 0)
                throw new InvalidOperationException(
                    "ACL resource readiness generation is not initialized.");
            return entry.Readiness;
        }

        internal CharacterAclResourceReadinessResult TryAcquire(
            int resourceIndex,
            CharacterAclResourceLeaseTable leaseTable,
            out CharacterAclResourceLease lease)
        {
            RequireOpen();
            lease = default;
            CharacterAclResourceStoreEntry entry =
                m_Catalog.RequireEntry(resourceIndex);
            if (!entry.Readiness.IsReady || entry.Group == IntPtr.Zero)
                return entry.Readiness;
            if (m_ResidentBytes > m_ResidentBudgetBytes)
                return CharacterAclResourceReadinessResult.Invalid(
                    CharacterAclResourceFailureCode.CapacityExceeded,
                    "ACL resident budget is exceeded.");
            if (leaseTable == null)
                throw new ArgumentNullException(nameof(leaseTable));
            if (!leaseTable.TryAcquire(
                    resourceIndex,
                    entry.Generation,
                    out int slotIndex,
                    out ulong slotGeneration))
                return CharacterAclResourceReadinessResult.Invalid(
                    CharacterAclResourceFailureCode.CapacityExceeded,
                    "ACL resource lease table capacity is exceeded.");
            entry.LeaseCount++;
            entry.LastUsed = NextUse();
            lease = new CharacterAclResourceLease(
                this,
                leaseTable,
                slotIndex,
                slotGeneration,
                resourceIndex,
                entry.Generation);
            return CharacterAclResourceReadinessResult.Ready();
        }

        internal void AdvancePreparation()
        {
            RequireOpen();
            for (int i = 0; i < m_Catalog.Count; i++)
            {
                CharacterAclResourceStoreEntry entry =
                    m_Catalog.RequireEntry(i);
                bool readyForAdmission = false;
                if (entry.Preparation.HasPendingCleanup)
                {
                    try
                    {
                        entry.Preparation.Advance();
                    }
                    finally
                    {
                        entry.Readiness = entry.Preparation.Readiness;
                    }
                    if (entry.Preparation.HasPendingCleanup)
                        continue;
                    if (entry.Readiness.IsInvalid)
                        continue;
                    readyForAdmission = entry.Readiness.IsReady;
                }
                if (!entry.Requested || entry.Readiness.IsInvalid ||
                    (!readyForAdmission && entry.Readiness.IsReady))
                    continue;
                if (!readyForAdmission)
                {
                    if (!entry.Preparation.IsRequested)
                    {
                        entry.Preparation.Request();
                        entry.Readiness = entry.Preparation.Readiness;
                        if (entry.Readiness.IsInvalid || !entry.Preparation.IsRequested)
                            continue;
                    }
                    try
                    {
                        entry.Preparation.Advance();
                    }
                    finally
                    {
                        entry.Readiness = entry.Preparation.Readiness;
                    }
                }
                if (!entry.Readiness.IsReady)
                    continue;
                if (entry.Preparation.Group == IntPtr.Zero)
                    throw new InvalidOperationException("ACL resource preparation completed without a native group.");
                if (m_ResidentBytes + entry.ResidentBytes > m_ResidentBudgetBytes)
                    TrimFor(entry.ResidentBytes);
                if (m_ResidentBytes + entry.ResidentBytes > m_ResidentBudgetBytes)
                {
                    try
                    {
                        entry.Preparation.Reject(
                            CharacterAclResourceFailureCode.CapacityExceeded,
                            "ACL resident budget cannot admit the resource.");
                    }
                    finally
                    {
                        entry.Readiness = entry.Preparation.Readiness;
                    }
                    continue;
                }
                entry.Group = entry.Preparation.TakeGroup();
                m_ResidentBytes += entry.ResidentBytes;
                entry.LastUsed = NextUse();
            }
            Trim();
        }

        internal CharacterAnimationCompiledResourceDescriptor RequireDescriptor(
            int resourceIndex)
        {
            RequireAlive();
            CharacterAclResourceStoreEntry entry =
                m_Catalog.RequireEntry(resourceIndex);
            entry.LastUsed = NextUse();
            return entry.Descriptor;
        }

        internal CharacterAnimationCompiledResourceDescriptor
            RequireDescriptorByCatalogIndex(int resourceCatalogIndex) =>
            RequireDescriptor(RequireStoreIndex(resourceCatalogIndex));

        internal IntPtr RequireGroup(in CharacterAclResourceLease lease)
        {
            CharacterAclResourceStoreEntry entry = RequireLease(in lease);
            if (entry.Group == IntPtr.Zero)
                throw new InvalidOperationException("ACL resource lease has no native group.");
            entry.LastUsed = NextUse();
            return entry.Group;
        }

        internal void Release(in CharacterAclResourceLease lease)
        {
            RequireAliveOrClosing();
            CharacterAclResourceStoreEntry entry = RequireLease(in lease);
            lease.LeaseTable.Release(
                lease.SlotIndex,
                lease.SlotGeneration,
                lease.ResourceIndex,
                lease.ResourceGeneration);
            entry.LeaseCount--;
            entry.LastUsed = NextUse();
            CompleteCloseIfIdle();
        }

        internal void Trim()
        {
            RequireOpen();
            TrimFor(0);
        }

        void TrimFor(long requiredBytes)
        {
            while (m_ResidentBytes + requiredBytes > m_ResidentBudgetBytes)
            {
                int candidateIndex = -1;
                ulong candidateUse = ulong.MaxValue;
                for (int i = 0; i < m_Catalog.Count; i++)
                {
                    CharacterAclResourceStoreEntry entry =
                        m_Catalog.RequireEntry(i);
                    if (entry.Group == IntPtr.Zero || entry.LeaseCount != 0 ||
                        entry.Preparation.IsInProgress || entry.LastUsed >= candidateUse)
                        continue;
                    candidateIndex = i;
                    candidateUse = entry.LastUsed;
                }
                if (candidateIndex < 0)
                    return;
                CharacterAclResourceStoreEntry candidate =
                    m_Catalog.RequireEntry(candidateIndex);
                ReleaseResidentGroup(candidate);
                candidate.Preparation.Dispose();
                candidate.Preparation = new CharacterAclAnimationResourcePreparation(
                    candidate.Descriptor,
                    m_Loader);
                candidate.Readiness = CharacterAclResourceReadinessResult.Pending();
                candidate.Generation = NextResourceGeneration();
                candidate.Requested = false;
            }
        }

        internal void Clear()
        {
            RequireAlive();
            for (int i = 0; i < m_Catalog.Count; i++)
            {
                CharacterAclResourceStoreEntry entry =
                    m_Catalog.RequireEntry(i);
                if (entry.LeaseCount != 0 || entry.Preparation.IsInProgress)
                    throw new InvalidOperationException("ACL resource store still has active work.");
            }
            for (int i = 0; i < m_Catalog.Count; i++)
            {
                CharacterAclResourceStoreEntry entry =
                    m_Catalog.RequireEntry(i);
                ReleaseResidentGroup(entry);
                entry.Preparation.Dispose();
            }
            m_Catalog.Clear();
            m_ResidentBytes = 0;
        }

        internal void BeginClose()
        {
            RequireAliveOrClosing();
            m_Closing = true;
            Exception failure = null;
            for (int i = 0; i < m_Catalog.Count; i++)
            {
                CharacterAclResourceStoreEntry entry =
                    m_Catalog.RequireEntry(i);
                entry.Requested = false;
                try
                {
                    entry.Preparation.Dispose();
                }
                catch (Exception exception)
                {
                    RecordFailure(ref failure, exception);
                }
                if (entry.Group == IntPtr.Zero)
                    entry.Readiness = CharacterAclResourceReadinessResult.Pending();
            }
            try
            {
                CompleteCloseIfIdle();
            }
            catch (Exception exception)
            {
                RecordFailure(ref failure, exception);
            }
            if (failure != null && !m_Disposed)
                throw new AggregateException(
                    "ACL resource store close failed.",
                    failure);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            BeginClose();
        }

        CharacterAclResourceStoreEntry RequireLease(in CharacterAclResourceLease lease)
        {
            if (!lease.IsValid || !ReferenceEquals(lease.Owner, this) ||
                (uint)lease.ResourceIndex >= (uint)m_Catalog.Count)
                throw new InvalidOperationException("ACL resource lease does not belong to this store.");
            CharacterAclResourceStoreEntry entry =
                m_Catalog.RequireEntry(lease.ResourceIndex);
            if (entry.Generation != lease.ResourceGeneration || entry.LeaseCount <= 0 ||
                entry.Group == IntPtr.Zero)
                throw new InvalidOperationException("ACL resource lease is stale.");
            lease.LeaseTable.Require(
                lease.SlotIndex,
                lease.SlotGeneration,
                lease.ResourceIndex,
                lease.ResourceGeneration);
            return entry;
        }

        void ReleaseResidentGroup(CharacterAclResourceStoreEntry entry)
        {
            if (entry.Group == IntPtr.Zero)
                return;
            CharacterAclNativeError error =
                CharacterAclNativeBridge.GroupRelease(entry.Group);
            if (error != CharacterAclNativeError.Success)
                throw new InvalidOperationException(
                    $"ACL resource group release failed: {error}.");
            entry.Group = IntPtr.Zero;
            m_ResidentBytes -= entry.ResidentBytes;
        }

        ulong NextUse()
        {
            ulong value = m_UseClock++;
            if (value == 0)
                throw new InvalidOperationException("ACL resource LRU clock was exhausted.");
            return value;
        }

        ulong NextResourceGeneration()
        {
            if (m_ResourceGeneration == ulong.MaxValue)
                throw new InvalidOperationException("ACL resource generation was exhausted.");
            return m_ResourceGeneration++;
        }

        void CompleteCloseIfIdle()
        {
            if (!m_Closing || m_Disposed)
                return;
            for (int i = 0; i < m_Catalog.Count; i++)
            {
                if (m_Catalog.RequireEntry(i).LeaseCount != 0)
                    return;
            }
            Exception failure = null;
            for (int i = 0; i < m_Catalog.Count; i++)
            {
                CharacterAclResourceStoreEntry entry =
                    m_Catalog.RequireEntry(i);
                try
                {
                    ReleaseResidentGroup(entry);
                }
                catch (Exception exception)
                {
                    RecordFailure(ref failure, exception);
                }
                try
                {
                    entry.Preparation.Dispose();
                }
                catch (Exception exception)
                {
                    RecordFailure(ref failure, exception);
                }
            }
            if (failure != null)
                throw new AggregateException(
                    "ACL resource store resource cleanup failed.",
                    failure);
            m_Catalog.Clear();
            m_ResidentBytes = 0;
            m_Loader.Dispose();
            m_Disposed = true;
        }

        static void RecordFailure(ref Exception failure, Exception exception)
        {
            failure = failure == null
                ? exception
                : new AggregateException(failure, exception);
        }

        static long ResourceBytes(CharacterAnimationCompiledResourceDescriptor descriptor)
        {
            long total = 0;
            for (int i = 0; i < descriptor.ClipManifests.Count; i++)
            {
                CharacterAclAnimationResourceManifest manifest =
                    descriptor.RequireManifest(i);
                total += manifest.Transform.Length;
                total += manifest.Scalar.Length;
            }
            CharacterAclAnimationResourceManifest groupManifest = descriptor.RequireManifest(0);
            total += groupManifest.DatabaseHeader.Length;
            total += groupManifest.BulkMedium.Length;
            total += groupManifest.BulkLow.Length;
            return total;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterAclResourceStore));
        }

        void RequireAliveOrClosing()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterAclResourceStore));
        }

        void RequireOpen()
        {
            RequireAlive();
            if (m_Closing)
                throw new ObjectDisposedException(nameof(CharacterAclResourceStore));
        }
    }
}
