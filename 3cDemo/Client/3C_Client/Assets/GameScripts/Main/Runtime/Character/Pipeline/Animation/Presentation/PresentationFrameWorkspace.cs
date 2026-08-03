using System;
using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    public readonly struct PresentationFrameWorkspaceLease
    {
        internal PresentationFrameWorkspaceLease(
            ulong identity,
            ulong presentationFrame,
            int pendingPageIndex)
        {
            Identity = identity;
            PresentationFrame = presentationFrame;
            PendingPageIndex = pendingPageIndex;
        }

        public ulong Identity { get; }
        public ulong PresentationFrame { get; }
        public int PendingPageIndex { get; }
        public bool IsValid =>
            Identity != 0 &&
            PresentationFrame != 0 &&
            (PendingPageIndex == 0 || PendingPageIndex == 1);
    }

    public sealed class PresentationFrameWorkspace
    {
        sealed class Page
        {
            internal Page(
                int providerCapacity,
                int actionCapacity,
                int releaseCompletionCapacity,
                int failureCapacity)
            {
                ProviderDemands =
                    new FixedCapacityFrameBuffer<PoseSourceProviderDemand>(
                        providerCapacity);
                ProviderSamples =
                    new Dictionary<PresentationPoseSourceProviderId,
                        PresentationPoseSourceSample>(providerCapacity);
                ActionFrames =
                    new Dictionary<AnimationPlaybackId,
                        ActionAnimationPlaybackFrame>(actionCapacity);
                ActionUsages =
                    new FixedCapacityFrameBuffer<ActionSlotSourceUsage>(
                        actionCapacity);
                RetirementPermissions =
                    new FixedCapacityFrameBuffer<ActionRetirementPermission>(
                        actionCapacity);
                ReleaseRequests =
                    new FixedCapacityFrameBuffer<ActionBackendReleaseRequest>(
                        actionCapacity);
                ReleaseCompletions =
                    new FixedCapacityFrameBuffer<ActionBackendReleaseCompletion>(
                        releaseCompletionCapacity);
                Failures =
                    new FixedCapacityFrameBuffer<PresentationFrameFailure>(
                        failureCapacity);
                ProviderCapacity = providerCapacity;
                ActionCapacity = actionCapacity;
            }

            internal FixedCapacityFrameBuffer<PoseSourceProviderDemand>
                ProviderDemands { get; }
            internal Dictionary<PresentationPoseSourceProviderId,
                PresentationPoseSourceSample> ProviderSamples { get; }
            internal Dictionary<AnimationPlaybackId,
                ActionAnimationPlaybackFrame> ActionFrames { get; }
            internal FixedCapacityFrameBuffer<ActionSlotSourceUsage>
                ActionUsages { get; }
            internal FixedCapacityFrameBuffer<ActionRetirementPermission>
                RetirementPermissions { get; }
            internal FixedCapacityFrameBuffer<ActionBackendReleaseRequest>
                ReleaseRequests { get; }
            internal FixedCapacityFrameBuffer<ActionBackendReleaseCompletion>
                ReleaseCompletions { get; }
            internal FixedCapacityFrameBuffer<PresentationFrameFailure>
                Failures { get; }
            internal int ProviderCapacity { get; }
            internal int ActionCapacity { get; }

            internal void Clear()
            {
                ProviderDemands.Clear();
                ProviderSamples.Clear();
                ActionFrames.Clear();
                ActionUsages.Clear();
                RetirementPermissions.Clear();
                ReleaseRequests.Clear();
                ReleaseCompletions.Clear();
                Failures.Clear();
            }
        }

        readonly Page[] m_Pages;
        int m_CommittedPageIndex;
        int m_PendingPageIndex;
        PresentationFrameWorkspaceLease m_ActiveLease;

        public PresentationFrameWorkspace(
            int providerCapacity,
            int actionCapacity,
            int releaseCompletionCapacity,
            int failureCapacity)
        {
            if (providerCapacity < 0 ||
                actionCapacity <= 0 ||
                releaseCompletionCapacity <= 0 ||
                failureCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actionCapacity));
            }
            m_Pages = new[]
            {
                new Page(
                    providerCapacity,
                    actionCapacity,
                    releaseCompletionCapacity,
                    failureCapacity),
                new Page(
                    providerCapacity,
                    actionCapacity,
                    releaseCompletionCapacity,
                    failureCapacity)
            };
            m_CommittedPageIndex = 0;
            m_PendingPageIndex = 1;
        }

        public IReadOnlyList<PoseSourceProviderDemand> ProviderDemands =>
            CurrentPage.ProviderDemands;
        public IReadOnlyDictionary<PresentationPoseSourceProviderId,
            PresentationPoseSourceSample> ProviderSamples =>
                CurrentPage.ProviderSamples;
        public IReadOnlyDictionary<AnimationPlaybackId,
            ActionAnimationPlaybackFrame> ActionFrames =>
                CurrentPage.ActionFrames;
        public IReadOnlyList<ActionSlotSourceUsage> ActionUsages =>
            CurrentPage.ActionUsages;
        public IReadOnlyList<ActionRetirementPermission>
            RetirementPermissions => CurrentPage.RetirementPermissions;
        public IReadOnlyList<ActionBackendReleaseRequest> ReleaseRequests =>
            CurrentPage.ReleaseRequests;
        public IReadOnlyList<ActionBackendReleaseCompletion>
            ReleaseCompletions => CurrentPage.ReleaseCompletions;
        public IReadOnlyList<PresentationFrameFailure> Failures =>
            CurrentPage.Failures;
        public bool HasFailure => CurrentPage.Failures.Count != 0;

        public PresentationFrameWorkspaceLease Begin(
            ulong frameIdentity,
            ulong presentationFrame)
        {
            if (m_ActiveLease.IsValid)
                throw new InvalidOperationException(
                    "Presentation frame workspace already has an active frame.");
            if (frameIdentity == 0 || presentationFrame == 0)
                throw new ArgumentOutOfRangeException(nameof(presentationFrame));
            m_PendingPageIndex = 1 - m_CommittedPageIndex;
            PendingPage.Clear();
            m_ActiveLease = new PresentationFrameWorkspaceLease(
                frameIdentity,
                presentationFrame,
                m_PendingPageIndex);
            return m_ActiveLease;
        }

        public void AddProviderDemand(
            PresentationFrameWorkspaceLease lease,
            PoseSourceProviderDemand demand)
        {
            RequireLease(lease);
            if (!demand.IsValid ||
                demand.FrameSequence != lease.PresentationFrame)
            {
                throw new ArgumentException(
                    "Pose source provider demand does not belong to this frame.");
            }
            FixedCapacityFrameBuffer<PoseSourceProviderDemand> demands =
                PendingPage.ProviderDemands;
            for (int i = 0; i < demands.Count; i++)
            {
                if (demands[i].ProviderId == demand.ProviderId)
                    throw new InvalidOperationException(
                        $"Pose provider '{demand.ProviderId}' was demanded more than once.");
            }
            demands.Add(demand);
        }

        public void SetProviderSample(
            PresentationFrameWorkspaceLease lease,
            PresentationPoseSourceSample sample)
        {
            RequireLease(lease);
            if (!sample.IsValid ||
                sample.FrameSequence != lease.PresentationFrame ||
                !HasDemand(sample))
            {
                throw new ArgumentException(
                    "Presentation Pose source sample has no exact frame demand.");
            }
            Dictionary<PresentationPoseSourceProviderId,
                PresentationPoseSourceSample> samples =
                    PendingPage.ProviderSamples;
            if (samples.Count == PendingPage.ProviderCapacity)
            {
                throw new InvalidOperationException(
                    "Pose provider sample capacity was exceeded.");
            }
            if (!samples.TryAdd(sample.ProviderId, sample))
                throw new InvalidOperationException(
                    $"Pose provider '{sample.ProviderId}' published more than once.");
        }

        public void SetActionFrame(
            PresentationFrameWorkspaceLease lease,
            ActionAnimationPlaybackFrame frame)
        {
            RequireLease(lease);
            Dictionary<AnimationPlaybackId,
                ActionAnimationPlaybackFrame> frames =
                    PendingPage.ActionFrames;
            if (!frame.IsValid ||
                frames.Count == PendingPage.ActionCapacity ||
                !frames.TryAdd(frame.PlaybackId, frame))
            {
                throw new InvalidOperationException(
                    "Action playback frame is invalid or duplicated.");
            }
        }

        public void AddActionUsage(
            PresentationFrameWorkspaceLease lease,
            ActionSlotSourceUsage usage)
        {
            RequireLease(lease);
            if (!usage.IsValid)
                throw new ArgumentException(
                    "Action Slot source usage is invalid.");
            FixedCapacityFrameBuffer<ActionSlotSourceUsage> usages =
                PendingPage.ActionUsages;
            for (int i = 0; i < usages.Count; i++)
            {
                ActionSlotSourceUsage current = usages[i];
                if (current.SlotId == usage.SlotId &&
                    current.PlaybackId.Equals(usage.PlaybackId) &&
                    current.Kind == usage.Kind)
                {
                    throw new InvalidOperationException(
                        "Action Slot source usage duplicates an exact consumer.");
                }
            }
            usages.Add(usage);
        }

        public void AddRetirementPermission(
            PresentationFrameWorkspaceLease lease,
            ActionRetirementPermission permission)
        {
            RequireLease(lease);
            if (!permission.IsValid)
                throw new ArgumentException(
                    "Action retirement permission is invalid.");
            PendingPage.RetirementPermissions.Add(permission);
        }

        public void AddReleaseRequest(
            PresentationFrameWorkspaceLease lease,
            ActionBackendReleaseRequest request)
        {
            RequireLease(lease);
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            PendingPage.ReleaseRequests.Add(request);
        }

        public void AddReleaseCompletion(
            PresentationFrameWorkspaceLease lease,
            ActionBackendReleaseCompletion completion)
        {
            RequireLease(lease);
            if (!completion.IsValid)
                throw new ArgumentException(
                    "Action backend release completion is invalid.");
            PendingPage.ReleaseCompletions.Add(completion);
        }

        public void Fail(
            PresentationFrameWorkspaceLease lease,
            PresentationFrameFailure failure)
        {
            RequireLease(lease);
            if (!failure.IsValid)
                throw new ArgumentException(
                    "Presentation frame failure is invalid.");
            PendingPage.Failures.Add(failure);
        }

        public void Commit(PresentationFrameWorkspaceLease lease)
        {
            RequireLease(lease);
            if (HasFailure)
                throw new InvalidOperationException(
                    "Presentation frame workspace with failures cannot commit.");
            m_CommittedPageIndex = m_PendingPageIndex;
            Close();
        }

        public void Discard(PresentationFrameWorkspaceLease lease)
        {
            RequireLease(lease);
            PendingPage.Clear();
            Close();
        }

        public void Reset()
        {
            if (m_ActiveLease.IsValid)
                throw new InvalidOperationException(
                    "Presentation frame workspace cannot reset during an active frame.");
            m_Pages[0].Clear();
            m_Pages[1].Clear();
            m_CommittedPageIndex = 0;
            m_PendingPageIndex = 1;
        }

        bool HasDemand(PresentationPoseSourceSample sample)
        {
            FixedCapacityFrameBuffer<PoseSourceProviderDemand> demands =
                PendingPage.ProviderDemands;
            for (int i = 0; i < demands.Count; i++)
            {
                PoseSourceProviderDemand demand = demands[i];
                if (demand.ProviderId == sample.ProviderId &&
                    demand.PlayerNodeId == sample.PlayerNodeId &&
                    demand.SourceIndex == sample.SourceIndex &&
                    demand.SourceKind == sample.SourceKind)
                {
                    return true;
                }
            }
            return false;
        }

        void RequireLease(PresentationFrameWorkspaceLease lease)
        {
            if (!lease.IsValid ||
                !m_ActiveLease.IsValid ||
                lease.Identity != m_ActiveLease.Identity ||
                lease.PresentationFrame != m_ActiveLease.PresentationFrame ||
                lease.PendingPageIndex != m_PendingPageIndex)
            {
                throw new InvalidOperationException(
                    "Presentation frame workspace lease is invalid.");
            }
        }

        void Close()
        {
            m_ActiveLease = default;
        }

        Page CurrentPage =>
            m_ActiveLease.IsValid
                ? PendingPage
                : m_Pages[m_CommittedPageIndex];

        Page PendingPage => m_Pages[m_PendingPageIndex];
    }
}
