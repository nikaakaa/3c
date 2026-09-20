using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal readonly struct AnimationPhysicalSourceIndex : IEquatable<AnimationPhysicalSourceIndex>
    {
        readonly int m_EncodedValue;

        internal AnimationPhysicalSourceIndex(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            m_EncodedValue = checked(value + 1);
        }

        internal int Value => m_EncodedValue - 1;
        internal bool IsValid => m_EncodedValue > 0;

        public bool Equals(AnimationPhysicalSourceIndex other) => m_EncodedValue == other.m_EncodedValue;
        public override bool Equals(object obj) => obj is AnimationPhysicalSourceIndex other && Equals(other);
        public override int GetHashCode() => m_EncodedValue;
        public static bool operator ==(AnimationPhysicalSourceIndex left, AnimationPhysicalSourceIndex right) => left.Equals(right);
        public static bool operator !=(AnimationPhysicalSourceIndex left, AnimationPhysicalSourceIndex right) => !left.Equals(right);
    }

    internal readonly struct AnimationPhysicalSourceIdentity : IEquatable<AnimationPhysicalSourceIdentity>
    {
        internal AnimationPhysicalSourceIdentity(AnimationPhysicalSourceIndex index, ulong generation)
        {
            if (!index.IsValid)
                throw new ArgumentException("Animation physical source index is invalid.", nameof(index));
            if (generation == 0)
                throw new ArgumentOutOfRangeException(nameof(generation));
            Index = index;
            Generation = generation;
        }

        internal AnimationPhysicalSourceIndex Index { get; }
        internal ulong Generation { get; }
        internal bool IsValid => Index.IsValid && Generation != 0;

        public bool Equals(AnimationPhysicalSourceIdentity other) =>
            Index.Equals(other.Index) && Generation == other.Generation;

        public override bool Equals(object obj) => obj is AnimationPhysicalSourceIdentity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Index, Generation);
        public static bool operator ==(AnimationPhysicalSourceIdentity left, AnimationPhysicalSourceIdentity right) =>
            left.Equals(right);
        public static bool operator !=(AnimationPhysicalSourceIdentity left, AnimationPhysicalSourceIdentity right) =>
            !left.Equals(right);
    }

    internal readonly struct AnimationPhysicalSourceReleaseToken
    {
        internal AnimationPhysicalSourceReleaseToken(
            AnimationPhysicalSourceIndex sourceIndex,
            ulong generation,
            AnimationPoseSourceId sourceId,
            CharacterAnimationSamplingBackendKind backend,
            int resourceCatalogIndex)
        {
            if (!sourceIndex.IsValid || generation == 0 || !sourceId.IsValid ||
                !PhysicalPoseSourceMetadata.IsValid(backend, resourceCatalogIndex))
                throw new ArgumentException("Animation physical source release token is invalid.");
            SourceIndex = sourceIndex;
            Generation = generation;
            SourceId = sourceId;
            Backend = backend;
            ResourceCatalogIndex = resourceCatalogIndex;
        }

        internal AnimationPhysicalSourceIndex SourceIndex { get; }
        internal ulong Generation { get; }
        internal AnimationPoseSourceId SourceId { get; }
        internal CharacterAnimationSamplingBackendKind Backend { get; }
        internal int ResourceCatalogIndex { get; }
        internal bool IsValid => SourceIndex.IsValid && Generation != 0 && SourceId.IsValid &&
                                 PhysicalPoseSourceMetadata.IsValid(Backend, ResourceCatalogIndex);
    }

    static class PhysicalPoseSourceMetadata
    {
        internal static bool IsValid(
            CharacterAnimationSamplingBackendKind backend,
            int resourceCatalogIndex) =>
            backend == CharacterAnimationSamplingBackendKind.NativeClip
                ? resourceCatalogIndex == -1
                : backend == CharacterAnimationSamplingBackendKind.Acl &&
                  resourceCatalogIndex >= 0;
    }

    internal readonly struct CharacterPoseSourceCommittedIdentity
    {
        internal CharacterPoseSourceCommittedIdentity(
            AnimationPhysicalSourceIdentity physicalIdentity,
            AnimationPoseSourceId sourceId,
            PoseNodeId poseNodeId,
            int sourceOwnerIndex,
            CharacterAnimationSamplingBackendKind backend,
            int resourceCatalogIndex)
        {
            PhysicalIdentity = physicalIdentity;
            SourceId = sourceId;
            PoseNodeId = poseNodeId;
            SourceOwnerIndex = sourceOwnerIndex;
            Backend = backend;
            ResourceCatalogIndex = resourceCatalogIndex;
        }

        internal AnimationPhysicalSourceIdentity PhysicalIdentity { get; }
        internal AnimationPoseSourceId SourceId { get; }
        internal PoseNodeId PoseNodeId { get; }
        internal int SourceOwnerIndex { get; }
        internal CharacterAnimationSamplingBackendKind Backend { get; }
        internal int ResourceCatalogIndex { get; }
        internal bool IsValid =>
            PhysicalIdentity.IsValid &&
            SourceId.IsValid &&
            PoseNodeId.IsValid &&
            SourceOwnerIndex >= 0 &&
            PhysicalPoseSourceMetadata.IsValid(Backend, ResourceCatalogIndex);
    }

    internal readonly struct CharacterPoseSourceCommittedDiagnosticsView
    {
        internal CharacterPoseSourceCommittedDiagnosticsView(
            PhysicalPoseSourceRegistry.CommittedDiagnosticsPage page)
        {
            m_Page = page ?? throw new ArgumentNullException(nameof(page));
            m_Identity = page.Identity;
            if (!IsValid)
            {
                throw new ArgumentException(
                    "Pose Source committed diagnostics are invalid.",
                    nameof(page));
            }
        }

        readonly PhysicalPoseSourceRegistry.CommittedDiagnosticsPage m_Page;
        readonly ulong m_Identity;
        internal bool IsValid =>
            m_Page != null &&
            m_Identity != 0 &&
            m_Page.Identity == m_Identity &&
            m_Page.Result.IsCommitted;
        internal CharacterPoseSourceCommittedResult Result
        {
            get
            {
                RequireValid();
                return m_Page.Result;
            }
        }
        internal int ReleaseCount
        {
            get
            {
                RequireValid();
                return m_Page.ReleaseCount;
            }
        }

        internal void CopyReleases(
            AnimationReleasedPoseSourceSnapshot[] releases)
        {
            RequireValid();
            if (releases == null || releases.Length < m_Page.ReleaseCount)
            {
                throw new ArgumentException(
                    "Pose Source release diagnostics destination is invalid.");
            }
            Array.Copy(
                m_Page.Releases,
                0,
                releases,
                0,
                m_Page.ReleaseCount);
        }

        internal CharacterPoseSourceCommittedIdentity Resolve(
            AnimationPhysicalSourceIdentity identity)
        {
            RequireValid();
            if (!identity.IsValid ||
                (uint)identity.Index.Value >=
                (uint)m_Page.Generations.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(identity));
            }
            int index = identity.Index.Value;
            if (m_Page.Generations[index] != identity.Generation ||
                !m_Page.SourceIds[index].IsValid ||
                !m_Page.PoseNodeIds[index].IsValid ||
                m_Page.SourceOwnerIndices[index] < 0 ||
                !PhysicalPoseSourceMetadata.IsValid(
                    m_Page.Backends[index],
                    m_Page.ResourceCatalogIndices[index]))
            {
                throw new InvalidOperationException(
                    "Pose Source committed physical identity is stale.");
            }
            return new CharacterPoseSourceCommittedIdentity(
                identity,
                m_Page.SourceIds[index],
                m_Page.PoseNodeIds[index],
                m_Page.SourceOwnerIndices[index],
                m_Page.Backends[index],
                m_Page.ResourceCatalogIndices[index]);
        }

        void RequireValid()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException(
                    "Pose Source committed diagnostics lease is stale.");
            }
        }
    }

    internal sealed class PhysicalPoseSourceRegistry : IDisposable
    {
        internal sealed class CommittedDiagnosticsPage
        {
            internal CommittedDiagnosticsPage(int capacity)
            {
                SourceIds = new AnimationPoseSourceId[capacity];
                PoseNodeIds = new PoseNodeId[capacity];
                SourceOwnerIndices = new int[capacity];
                Generations = new ulong[capacity];
                Backends = new CharacterAnimationSamplingBackendKind[capacity];
                ResourceCatalogIndices = new int[capacity];
                Releases =
                    new AnimationReleasedPoseSourceSnapshot[capacity];
                for (int i = 0; i < SourceOwnerIndices.Length; i++)
                {
                    SourceOwnerIndices[i] = -1;
                    ResourceCatalogIndices[i] = -1;
                }
            }

            internal ulong Identity;
            internal CharacterPoseSourceCommittedResult Result;
            internal int ReleaseCount;
            internal readonly AnimationPoseSourceId[] SourceIds;
            internal readonly PoseNodeId[] PoseNodeIds;
            internal readonly int[] SourceOwnerIndices;
            internal readonly ulong[] Generations;
            internal readonly CharacterAnimationSamplingBackendKind[] Backends;
            internal readonly int[] ResourceCatalogIndices;
            internal readonly AnimationReleasedPoseSourceSnapshot[] Releases;
        }

        AnimationPoseSourceId[] m_SourceIds;
        PoseNodeId[] m_PoseNodeIds;
        int[] m_SourceOwnerIndices;
        CharacterAnimationSamplingBackendKind[] m_Backends;
        int[] m_ResourceCatalogIndices;
        ulong[] m_Generations;
        AnimationPoseSourceId[] m_PendingSourceIds;
        PoseNodeId[] m_PendingPoseNodeIds;
        int[] m_PendingSourceOwnerIndices;
        CharacterAnimationSamplingBackendKind[] m_PendingBackends;
        int[] m_PendingResourceCatalogIndices;
        ulong[] m_PendingGenerations;
        byte[] m_PreparedReleaseSlots;
        readonly AnimationReleasedPoseSourceSnapshot[]
            m_ReleaseDiagnostics;
        readonly CommittedDiagnosticsPage m_CommittedDiagnostics;
        int m_Count;
        int m_PendingCount;
        int m_PreparedReleaseCount;
        int m_ReleaseDiagnosticsCount;
        ulong m_LastGeneration;
        ulong m_NextDiagnosticsIdentity = 1;
        bool m_FrameOpen;
        bool m_FrameValidated;
        bool m_RecordReleaseDiagnostics;
        bool m_ReleaseDiagnosticsOpen;
        bool m_Disposed;

        internal PhysicalPoseSourceRegistry(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_SourceIds = new AnimationPoseSourceId[capacity];
            m_PoseNodeIds = new PoseNodeId[capacity];
            m_SourceOwnerIndices = new int[capacity];
            m_Backends = new CharacterAnimationSamplingBackendKind[capacity];
            m_ResourceCatalogIndices = new int[capacity];
            m_Generations = new ulong[capacity];
            m_PendingSourceIds = new AnimationPoseSourceId[capacity];
            m_PendingPoseNodeIds = new PoseNodeId[capacity];
            m_PendingSourceOwnerIndices = new int[capacity];
            m_PendingBackends = new CharacterAnimationSamplingBackendKind[capacity];
            m_PendingResourceCatalogIndices = new int[capacity];
            m_PendingGenerations = new ulong[capacity];
            m_PreparedReleaseSlots = new byte[capacity];
            m_ReleaseDiagnostics =
                new AnimationReleasedPoseSourceSnapshot[capacity];
            m_CommittedDiagnostics = new CommittedDiagnosticsPage(capacity);
            for (int i = 0; i < m_SourceOwnerIndices.Length; i++)
            {
                m_SourceOwnerIndices[i] = -1;
                m_PendingSourceOwnerIndices[i] = -1;
                m_ResourceCatalogIndices[i] = -1;
                m_PendingResourceCatalogIndices[i] = -1;
            }
        }

        internal int Capacity
        {
            get
            {
                RequireAlive();
                return m_SourceIds.Length;
            }
        }
        internal int ReleaseDiagnosticsCapacity =>
            m_ReleaseDiagnostics.Length;

        internal int Count
        {
            get
            {
                RequireAlive();
                return checked(m_Count + m_PendingCount);
            }
        }

        internal bool HasOpenFrame => m_FrameOpen;
        internal int PendingRegistrationCount
        {
            get
            {
                RequireAlive();
                RequireOpenFrame();
                return m_PendingCount;
            }
        }

        internal void BeginFrame()
        {
            RequireAlive();
            if (m_FrameOpen || m_ReleaseDiagnosticsOpen)
                throw new InvalidOperationException("Physical Pose Source frame is already open.");
            if (m_PreparedReleaseCount != 0)
                throw new InvalidOperationException("Physical Pose Source prepared releases were not applied.");
            ClearPending();
            m_CommittedDiagnostics.Identity = 0;
            m_ReleaseDiagnosticsCount = 0;
            m_FrameOpen = true;
            m_FrameValidated = false;
        }

        internal void BeginReleaseDiagnostics(bool recordDiagnostics)
        {
            RequireAlive();
            RequireOpenFrame();
            if (m_ReleaseDiagnosticsOpen || m_ReleaseDiagnosticsCount != 0)
            {
                throw new InvalidOperationException(
                    "Pose Source release diagnostics are already open.");
            }
            m_RecordReleaseDiagnostics = recordDiagnostics;
            m_ReleaseDiagnosticsOpen = true;
        }

        internal void RecordRelease(
            PoseNodeId poseNodeId,
            AnimationPoseSourceId sourceId,
            ulong completionIdentity)
        {
            RequireAlive();
            if (!m_ReleaseDiagnosticsOpen)
            {
                throw new InvalidOperationException(
                    "Pose Source release diagnostics are not open.");
            }
            if (!m_RecordReleaseDiagnostics)
                return;
            if (!poseNodeId.IsValid ||
                !sourceId.IsValid ||
                completionIdentity == 0)
            {
                throw new ArgumentException(
                    "Pose Source release diagnostics are invalid.");
            }
            if (m_ReleaseDiagnosticsCount >=
                m_ReleaseDiagnostics.Length)
            {
                throw new InvalidOperationException(
                    "Animation diagnostics release capacity was exceeded.");
            }
            m_ReleaseDiagnostics[m_ReleaseDiagnosticsCount++] =
                new AnimationReleasedPoseSourceSnapshot(
                    poseNodeId,
                    sourceId,
                    completionIdentity);
        }

        internal void CompleteReleaseDiagnostics()
        {
            RequireAlive();
            if (!m_ReleaseDiagnosticsOpen)
            {
                throw new InvalidOperationException(
                    "Pose Source release diagnostics are not open.");
            }
            m_RecordReleaseDiagnostics = false;
            m_ReleaseDiagnosticsOpen = false;
        }

        internal void CancelReleaseDiagnostics()
        {
            RequireAlive();
            m_RecordReleaseDiagnostics = false;
            m_ReleaseDiagnosticsOpen = false;
            m_ReleaseDiagnosticsCount = 0;
        }

        internal void ValidateFrame()
        {
            RequireAlive();
            RequireOpenFrame();
            if (checked(m_Count + m_PendingCount) > m_SourceIds.Length)
                throw new InvalidOperationException("Animation physical source capacity was exceeded.");
            m_FrameValidated = true;
        }

        internal void CommitFrame()
        {
            RequireAlive();
            RequireOpenFrame();
            if (!m_FrameValidated)
                throw new InvalidOperationException("Physical Pose Source frame was not validated.");
            for (int i = 0; i < m_PendingSourceIds.Length; i++)
            {
                if (!m_PendingSourceIds[i].IsValid)
                    continue;
                m_SourceIds[i] = m_PendingSourceIds[i];
                m_PoseNodeIds[i] = m_PendingPoseNodeIds[i];
                m_SourceOwnerIndices[i] = m_PendingSourceOwnerIndices[i];
                m_Backends[i] = m_PendingBackends[i];
                m_ResourceCatalogIndices[i] = m_PendingResourceCatalogIndices[i];
                m_Generations[i] = m_PendingGenerations[i];
            }
            m_Count = checked(m_Count + m_PendingCount);
            ClearPending();
            m_FrameOpen = false;
            m_FrameValidated = false;
        }

        internal void DiscardFrame()
        {
            RequireAlive();
            RequireOpenFrame();
            ClearPending();
            ClearPreparedReleases();
            CancelReleaseDiagnostics();
            m_FrameOpen = false;
            m_FrameValidated = false;
        }

        internal AnimationPhysicalSourceIdentity GetPendingRegistration(int ordinal)
        {
            RequireAlive();
            RequireOpenFrame();
            if ((uint)ordinal >= (uint)m_PendingCount)
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            int found = 0;
            for (int i = 0; i < m_PendingGenerations.Length; i++)
            {
                if (m_PendingGenerations[i] == 0)
                    continue;
                if (found++ == ordinal)
                    return new AnimationPhysicalSourceIdentity(
                        new AnimationPhysicalSourceIndex(i),
                        m_PendingGenerations[i]);
            }
            throw new InvalidOperationException("Physical Pose Source pending journal is inconsistent.");
        }

        internal AnimationPhysicalSourceIdentity Register(
            AnimationPoseSourceId sourceId,
            PoseNodeId poseNodeId,
            int sourceOwnerIndex,
            CharacterAnimationSamplingBackendKind backend,
            int resourceCatalogIndex)
        {
            RequireAlive();
            RequireOpenFrame();
            if (!sourceId.IsValid || !poseNodeId.IsValid || sourceOwnerIndex < 0 ||
                !PhysicalPoseSourceMetadata.IsValid(backend, resourceCatalogIndex))
                throw new ArgumentException("Animation physical source identity is invalid.");
            if (TryFind(sourceId, poseNodeId, out int existing))
            {
                int existingOwner = PendingIsOccupied(existing)
                    ? m_PendingSourceOwnerIndices[existing]
                    : m_SourceOwnerIndices[existing];
                CharacterAnimationSamplingBackendKind existingBackend =
                    PendingIsOccupied(existing)
                        ? m_PendingBackends[existing]
                        : m_Backends[existing];
                int existingResourceCatalogIndex = PendingIsOccupied(existing)
                    ? m_PendingResourceCatalogIndices[existing]
                    : m_ResourceCatalogIndices[existing];
                if (existingOwner != sourceOwnerIndex ||
                    existingBackend != backend ||
                    existingResourceCatalogIndex != resourceCatalogIndex)
                {
                    throw new InvalidOperationException(
                        $"Animation pose source '{sourceId}' is already registered with different physical metadata.");
                }
                return CreateIdentity(existing);
            }

            int index = FindFreeIndex();
            ulong generation = AllocateGeneration();
            m_PendingSourceIds[index] = sourceId;
            m_PendingPoseNodeIds[index] = poseNodeId;
            m_PendingSourceOwnerIndices[index] = sourceOwnerIndex;
            m_PendingBackends[index] = backend;
            m_PendingResourceCatalogIndices[index] = resourceCatalogIndex;
            m_PendingGenerations[index] = generation;
            m_PendingCount++;
            m_FrameValidated = false;
            return new AnimationPhysicalSourceIdentity(new AnimationPhysicalSourceIndex(index), generation);
        }

        internal AnimationPhysicalSourceIdentity RequireIdentity(AnimationPoseSourceId sourceId, PoseNodeId nodeId)
        {
            RequireAlive();
            if (!sourceId.IsValid || !nodeId.IsValid)
                throw new ArgumentException("Animation pose source identity is invalid.");
            if (!TryFind(sourceId, nodeId, out int index))
                throw new InvalidOperationException($"Animation pose source '{sourceId}' has no physical identity for Player '{nodeId}'.");
            return CreateIdentity(index);
        }

        internal bool ContainsCommitted(
            AnimationPoseSourceId sourceId,
            PoseNodeId nodeId)
        {
            RequireAlive();
            if (!sourceId.IsValid || !nodeId.IsValid ||
                !TryFind(sourceId, nodeId, out int index))
                return false;
            return !PendingIsOccupied(index);
        }

        internal void FreezeCommittedDiagnostics(
            in CharacterPoseSourceFrameResult sourceFrame)
        {
            RequireAlive();
            if (m_FrameOpen ||
                m_PreparedReleaseCount != 0 ||
                m_ReleaseDiagnosticsOpen ||
                !sourceFrame.IsReady)
            {
                throw new InvalidOperationException(
                    "Pose Source committed diagnostics request is invalid.");
            }
            CommittedDiagnosticsPage page = m_CommittedDiagnostics;
            page.Identity = 0;
            page.Result = new CharacterPoseSourceCommittedResult(
                in sourceFrame);
            Array.Copy(m_SourceIds, page.SourceIds, m_SourceIds.Length);
            Array.Copy(m_PoseNodeIds, page.PoseNodeIds, m_PoseNodeIds.Length);
            Array.Copy(
                m_SourceOwnerIndices,
                page.SourceOwnerIndices,
                m_SourceOwnerIndices.Length);
            Array.Copy(m_Backends, page.Backends, m_Backends.Length);
            Array.Copy(
                m_ResourceCatalogIndices,
                page.ResourceCatalogIndices,
                m_ResourceCatalogIndices.Length);
            Array.Copy(m_Generations, page.Generations, m_Generations.Length);
            Array.Copy(
                m_ReleaseDiagnostics,
                0,
                page.Releases,
                0,
                m_ReleaseDiagnosticsCount);
            Array.Clear(
                page.Releases,
                m_ReleaseDiagnosticsCount,
                page.Releases.Length - m_ReleaseDiagnosticsCount);
            page.ReleaseCount = m_ReleaseDiagnosticsCount;
            page.Identity = m_NextDiagnosticsIdentity++;
        }

        internal CharacterPoseSourceCommittedDiagnosticsView
            CaptureCommittedDiagnostics(
            in CharacterPoseSourceFrameResult sourceFrame)
        {
            RequireAlive();
            if (!sourceFrame.IsReady ||
                !m_CommittedDiagnostics.Result.IsCommitted ||
                m_CommittedDiagnostics.Result.Lineage != sourceFrame.Lineage)
            {
                throw new InvalidOperationException(
                    "Pose Source committed diagnostics are unavailable.");
            }
            return new CharacterPoseSourceCommittedDiagnosticsView(
                m_CommittedDiagnostics);
        }

        internal AnimationPoseSourceId RequireSourceId(AnimationPhysicalSourceIdentity identity)
        {
            int value = RequireOccupied(identity);
            return PendingIsOccupied(value)
                ? m_PendingSourceIds[value]
                : m_SourceIds[value];
        }

        internal PoseNodeId RequirePoseNodeId(AnimationPhysicalSourceIdentity identity)
        {
            int value = RequireOccupied(identity);
            return PendingIsOccupied(value)
                ? m_PendingPoseNodeIds[value]
                : m_PoseNodeIds[value];
        }

        internal int RequireSourceOwnerIndex(AnimationPhysicalSourceIdentity identity)
        {
            int value = RequireOccupied(identity);
            return PendingIsOccupied(value)
                ? m_PendingSourceOwnerIndices[value]
                : m_SourceOwnerIndices[value];
        }

        internal CharacterAnimationSamplingBackendKind RequireBackendKind(
            AnimationPhysicalSourceIdentity identity)
        {
            int value = RequireOccupied(identity);
            return PendingIsOccupied(value)
                ? m_PendingBackends[value]
                : m_Backends[value];
        }

        internal int RequireResourceCatalogIndex(
            AnimationPhysicalSourceIdentity identity)
        {
            int value = RequireOccupied(identity);
            return PendingIsOccupied(value)
                ? m_PendingResourceCatalogIndices[value]
                : m_ResourceCatalogIndices[value];
        }

        internal AnimationPhysicalSourceReleaseToken PrepareRelease(
            AnimationPhysicalSourceIdentity identity,
            AnimationPoseSourceId sourceId)
        {
            RequireAlive();
            if (!identity.IsValid ||
                identity.Index.Value < 0 ||
                identity.Index.Value >= m_SourceIds.Length ||
                !sourceId.IsValid)
            {
                throw new ArgumentException("Animation physical source release identity is invalid.");
            }
            int index = identity.Index.Value;
            if (!m_SourceIds[index].IsValid ||
                !m_PoseNodeIds[index].IsValid ||
                m_SourceOwnerIndices[index] < 0 ||
                m_Generations[index] == 0)
            {
                throw new InvalidOperationException(
                    $"Animation physical source identity ({index}, {identity.Generation}) is no longer committed.");
            }
            if (m_Generations[index] != identity.Generation)
            {
                throw new InvalidOperationException(
                    $"Animation physical source identity ({index}, {identity.Generation}) is stale; current generation is {m_Generations[index]}.");
            }
            if (!m_SourceIds[index].Equals(sourceId))
            {
                throw new InvalidOperationException(
                    $"Animation physical source identity does not belong to source '{sourceId}'.");
            }
            if (m_PreparedReleaseSlots[index] != 0)
                throw new InvalidOperationException("Animation physical source release was prepared twice.");
            m_PreparedReleaseSlots[index] = 1;
            m_PreparedReleaseCount++;
            return new AnimationPhysicalSourceReleaseToken(
                identity.Index,
                identity.Generation,
                sourceId,
                m_Backends[index],
                m_ResourceCatalogIndices[index]);
        }

        internal void ApplyPreparedRelease(
            in AnimationPhysicalSourceReleaseToken token)
        {
            int index = token.SourceIndex.Value;
            Clear(index);
            m_PreparedReleaseSlots[index] = 0;
            m_Count--;
            m_PreparedReleaseCount--;
        }

        internal void Reset()
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException("Physical Pose Source frame is open.");
            CancelReleaseDiagnostics();
            Array.Clear(m_SourceIds, 0, m_SourceIds.Length);
            Array.Clear(m_PoseNodeIds, 0, m_PoseNodeIds.Length);
            Array.Clear(m_Backends, 0, m_Backends.Length);
            Array.Clear(m_ResourceCatalogIndices, 0, m_ResourceCatalogIndices.Length);
            Array.Clear(m_Generations, 0, m_Generations.Length);
            for (int i = 0; i < m_SourceOwnerIndices.Length; i++)
                m_SourceOwnerIndices[i] = -1;
            m_Count = 0;
            ClearPending();
            ClearPreparedReleases();
            m_CommittedDiagnostics.Identity = 0;
            m_CommittedDiagnostics.Result = default;
            m_CommittedDiagnostics.ReleaseCount = 0;
            Array.Clear(
                m_CommittedDiagnostics.SourceIds,
                0,
                m_CommittedDiagnostics.SourceIds.Length);
            Array.Clear(
                m_CommittedDiagnostics.PoseNodeIds,
                0,
                m_CommittedDiagnostics.PoseNodeIds.Length);
            Array.Clear(
                m_CommittedDiagnostics.Backends,
                0,
                m_CommittedDiagnostics.Backends.Length);
            Array.Clear(
                m_CommittedDiagnostics.ResourceCatalogIndices,
                0,
                m_CommittedDiagnostics.ResourceCatalogIndices.Length);
            Array.Clear(
                m_CommittedDiagnostics.Generations,
                0,
                m_CommittedDiagnostics.Generations.Length);
            Array.Clear(
                m_CommittedDiagnostics.Releases,
                0,
                m_CommittedDiagnostics.Releases.Length);
            for (int i = 0;
                 i < m_CommittedDiagnostics.SourceOwnerIndices.Length;
                 i++)
            {
                m_CommittedDiagnostics.SourceOwnerIndices[i] = -1;
            }
        }

        AnimationPhysicalSourceIdentity CreateIdentity(int index)
        {
            ulong generation = PendingIsOccupied(index)
                ? m_PendingGenerations[index]
                : m_Generations[index];
            if (generation == 0)
                throw new InvalidOperationException($"Animation physical source slot {index} has no occupancy generation.");
            return new AnimationPhysicalSourceIdentity(
                new AnimationPhysicalSourceIndex(index),
                generation);
        }

        int RequireOccupied(AnimationPhysicalSourceIdentity identity)
        {
            RequireAlive();
            if (!identity.IsValid || identity.Index.Value < 0 || identity.Index.Value >= m_SourceIds.Length)
                throw new ArgumentOutOfRangeException(nameof(identity));
            int index = identity.Index.Value;
            bool pending = PendingIsOccupied(index);
            AnimationPoseSourceId sourceId = pending ? m_PendingSourceIds[index] : m_SourceIds[index];
            PoseNodeId nodeId = pending ? m_PendingPoseNodeIds[index] : m_PoseNodeIds[index];
            int ownerIndex = pending ? m_PendingSourceOwnerIndices[index] : m_SourceOwnerIndices[index];
            CharacterAnimationSamplingBackendKind backend =
                pending ? m_PendingBackends[index] : m_Backends[index];
            int resourceCatalogIndex = pending
                ? m_PendingResourceCatalogIndices[index]
                : m_ResourceCatalogIndices[index];
            ulong generation = pending ? m_PendingGenerations[index] : m_Generations[index];
            if (!sourceId.IsValid || !nodeId.IsValid || ownerIndex < 0 ||
                generation == 0 ||
                !PhysicalPoseSourceMetadata.IsValid(backend, resourceCatalogIndex))
            {
                throw new InvalidOperationException(
                    $"Animation physical source identity ({index}, {identity.Generation}) is no longer occupied.");
            }
            if (generation != identity.Generation)
            {
                throw new InvalidOperationException(
                    $"Animation physical source identity ({index}, {identity.Generation}) is stale; current generation is {generation}.");
            }
            return index;
        }

        bool TryFind(AnimationPoseSourceId sourceId, PoseNodeId nodeId, out int index)
        {
            for (int i = 0; i < m_PendingSourceIds.Length; i++)
            {
                if (!m_PendingSourceIds[i].Equals(sourceId) ||
                    !m_PendingPoseNodeIds[i].Equals(nodeId))
                    continue;
                index = i;
                return true;
            }
            for (int i = 0; i < m_SourceIds.Length; i++)
            {
                if (!m_SourceIds[i].Equals(sourceId) || !m_PoseNodeIds[i].Equals(nodeId))
                    continue;
                index = i;
                return true;
            }
            index = -1;
            return false;
        }

        int FindFreeIndex()
        {
            for (int i = 0; i < m_SourceIds.Length; i++)
            {
                if (!m_SourceIds[i].IsValid && !m_PendingSourceIds[i].IsValid)
                    return i;
            }
            throw new InvalidOperationException("Animation physical source capacity was exceeded.");
        }

        ulong AllocateGeneration()
        {
            if (m_LastGeneration == ulong.MaxValue)
                throw new InvalidOperationException("Animation physical source generation was exhausted.");
            m_LastGeneration++;
            return m_LastGeneration;
        }

        void Clear(int index)
        {
            m_SourceIds[index] = default;
            m_PoseNodeIds[index] = default;
            m_SourceOwnerIndices[index] = -1;
            m_Backends[index] = default;
            m_ResourceCatalogIndices[index] = -1;
            m_Generations[index] = 0;
        }

        bool PendingIsOccupied(int index) =>
            m_FrameOpen && m_PendingGenerations[index] != 0;

        void ClearPending()
        {
            Array.Clear(m_PendingSourceIds, 0, m_PendingSourceIds.Length);
            Array.Clear(m_PendingPoseNodeIds, 0, m_PendingPoseNodeIds.Length);
            Array.Clear(m_PendingBackends, 0, m_PendingBackends.Length);
            Array.Clear(
                m_PendingResourceCatalogIndices,
                0,
                m_PendingResourceCatalogIndices.Length);
            Array.Clear(m_PendingGenerations, 0, m_PendingGenerations.Length);
            for (int i = 0; i < m_PendingSourceOwnerIndices.Length; i++)
            {
                m_PendingSourceOwnerIndices[i] = -1;
                m_PendingResourceCatalogIndices[i] = -1;
            }
            m_PendingCount = 0;
        }

        void ClearPreparedReleases()
        {
            Array.Clear(
                m_PreparedReleaseSlots,
                0,
                m_PreparedReleaseSlots.Length);
            m_PreparedReleaseCount = 0;
        }

        void RequireOpenFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException("Physical Pose Source frame is not open.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(PhysicalPoseSourceRegistry));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Reset();
            m_SourceIds = null;
            m_PoseNodeIds = null;
            m_SourceOwnerIndices = null;
            m_Backends = null;
            m_ResourceCatalogIndices = null;
            m_Generations = null;
            m_PendingSourceIds = null;
            m_PendingPoseNodeIds = null;
            m_PendingSourceOwnerIndices = null;
            m_PendingBackends = null;
            m_PendingResourceCatalogIndices = null;
            m_PendingGenerations = null;
            m_PreparedReleaseSlots = null;
            m_Disposed = true;
        }
    }
}
