using System;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.Resources;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using UnityEngine.Playables;

namespace ThirdPersonCharacter.Pipeline.Presentation.Animancer
{
    internal sealed class CharacterAclPoseSamplingBackend : IAnimationPoseSamplingBackend
    {
        enum FramePhase : byte
        {
            Closed = 0,
            Preparing = 1,
            Validated = 2,
            EvaluateBarrier = 3
        }

        readonly PlayableGraph m_Graph;
        readonly CharacterAclResourceStore m_Store;
        readonly int m_SourceCapacity;
        readonly int m_ClipCapacity;
        readonly CharacterAclPoseSamplingResources m_Resources;
        readonly CharacterAclFrameJournal m_Journal;
        readonly ClipSamplePlan[] m_PendingPlans;
        readonly CharacterAclSourceInstance[] m_DeferredReleases;
        int m_DeferredReleaseCount;
        ulong m_FrameIdentity;
        FramePhase m_FramePhase;
        bool m_FrameApplied;
        bool m_Disposed;

        CharacterAclSourcePool SourcePool => m_Resources.Pool;

        internal CharacterAclPoseSamplingBackend(
            AnimancerComponent animancer,
            CharacterAnimationRigBinding rigBinding,
            CharacterAnimationRigPayload rig,
            int sourceCapacity,
            int clipCapacity,
            CharacterAnimationResourceScope resourceScope,
            int parameterCapacity)
        {
            if (!animancer || !rigBinding || rig == null || resourceScope == null ||
                sourceCapacity <= 0 || clipCapacity <= 0 || parameterCapacity <= 0)
                throw new ArgumentException("ACL pose sampling backend configuration is invalid.");
            rigBinding.RequireValid(rig);
            if (!animancer.Animator || rigBinding.Animator != animancer.Animator)
                throw new ArgumentException("ACL pose sampling Rig Binding does not belong to the Animancer Animator.");
            PlayableGraph graph = animancer.Graph.PlayableGraph;
            if (!graph.IsValid())
                throw new InvalidOperationException("ACL pose sampling graph is unavailable.");
            m_Graph = graph;
            m_Store = resourceScope.Store;
            m_SourceCapacity = sourceCapacity;
            m_ClipCapacity = clipCapacity;
            try
            {
                m_Resources = new CharacterAclPoseSamplingResources(
                    graph,
                    animancer.Animator,
                    rigBinding,
                    rig,
                    m_Store,
                    sourceCapacity,
                    clipCapacity,
                    parameterCapacity);
                m_Journal = new CharacterAclFrameJournal(sourceCapacity);
                m_PendingPlans = new ClipSamplePlan[checked(sourceCapacity * clipCapacity)];
                m_DeferredReleases = new CharacterAclSourceInstance[sourceCapacity];
            }
            catch (Exception exception)
            {
                Exception cleanupFailure = null;
                if (m_Resources != null)
                {
                    try
                    {
                        m_Resources.Dispose();
                    }
                    catch (Exception cleanupException)
                    {
                        RecordFailure(ref cleanupFailure, cleanupException);
                    }
                }
                if (cleanupFailure != null)
                    throw new AggregateException(
                        "ACL pose sampling backend construction cleanup failed.",
                        exception,
                        cleanupFailure);
                throw;
            }
        }

        public int SourceCapacity => m_SourceCapacity;
        public int ClipCapacity => m_ClipCapacity;
        public bool HasOpenFrame => m_FramePhase != FramePhase.Closed;

        public void BeginFrame(CharacterPoseSourceFrameLease lease)
        {
            RequireAvailable();
            if (!lease.IsValid || m_FramePhase != FramePhase.Closed ||
                m_Journal.ReleaseCount != 0 || m_DeferredReleaseCount != 0)
                throw new InvalidOperationException("ACL pose source frame lifecycle is not closed.");
            m_FrameIdentity = lease.FrameIdentity;
            m_FrameApplied = false;
            m_FramePhase = FramePhase.Preparing;
        }

        public void RequireOpenFrame(CharacterPoseSourceFrameLease lease)
        {
            RequireAvailable();
            if (!lease.IsValid || m_FramePhase == FramePhase.Closed ||
                m_FrameIdentity != lease.FrameIdentity)
                throw new InvalidOperationException("ACL pose source frame is not open.");
        }

        public AnimationPoseSourcePrepareResult PrepareOrUpdate(
            in AnimationPoseSampleRequest request,
            AnimationPhysicalSourceIdentity physicalIdentity,
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> clipCatalog,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId playerNodeId)
        {
            if (!request.IsValid || !playerNodeId.IsValid)
                throw new ArgumentException("ACL pose source request is invalid.");
            return PrepareOrUpdate(
                request.SourceId,
                physicalIdentity,
                request.Clips,
                clipCatalog,
                in capture,
                playerNodeId);
        }

        public AnimationPoseSourcePrepareResult PrepareOrUpdate(
            AnimationPoseSourceId sourceId,
            AnimationPhysicalSourceIdentity physicalIdentity,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> clipCatalog,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId playerNodeId)
        {
            RequireAvailable();
            RequireFramePhase(FramePhase.Preparing);
            if (!sourceId.IsValid || !physicalIdentity.IsValid || !playerNodeId.IsValid ||
                clips.Count <= 0 || clips.Count > m_ClipCapacity ||
                capture.SourceId != sourceId || capture.CompletionIdentity == 0)
                throw new ArgumentException("ACL pose source request exceeds its compiled contract.");
            CharacterAclSourceKey key = new CharacterAclSourceKey(sourceId, playerNodeId);
            if (m_Journal.FindMutation(key) >= 0 || HasRelease(key))
                throw new InvalidOperationException("ACL pose source has a duplicate frame mutation.");
            bool preparedResource = !SourcePool.ContainsCommitted(sourceId, playerNodeId);
            CharacterAclSourceInstance instance;
            if (preparedResource)
            {
                if (clipCatalog.Count <= 0 || clipCatalog.Count > m_ClipCapacity)
                    throw new ArgumentException("ACL pose source has no compiled resource catalog.");
                ValidateCatalog(clipCatalog);
                instance = SourcePool.Prepare(
                    physicalIdentity,
                    key,
                    clipCatalog,
                    in capture);
            }
            else
            {
                if (clipCatalog.Count != 0)
                    throw new ArgumentException("Committed ACL pose source must not resubmit its catalog.");
                instance = SourcePool.RequireCommitted(physicalIdentity, key);
            }
            int mutationIndex = m_Journal.MutationCount;
            int planOffset = checked(mutationIndex * m_ClipCapacity);
            try
            {
                for (int i = 0; i < clips.Count; i++)
                {
                    ClipSamplePlan plan = clips[i];
                    if (!plan.IsValid || !plan.IsAcl ||
                        !instance.Matches(
                            plan.ClipBindingIndex,
                            plan.ResourceCatalogIndex,
                            plan.GroupClipIndex))
                        throw new InvalidOperationException("ACL pose source sample plan does not match its catalog.");
                    m_PendingPlans[planOffset + i] = plan;
                }
                var mutation = new CharacterAclFrameJournal.Mutation
                {
                    PhysicalIdentity = physicalIdentity,
                    Key = key,
                    Instance = instance,
                    Capture = capture,
                    PlanOffset = planOffset,
                    PlanCount = clips.Count,
                    Kind = preparedResource
                        ? AnimationPoseSourcePrepareKind.PreparedResource
                        : AnimationPoseSourcePrepareKind.CommittedUpdate
                };
                m_Journal.AddMutation(in mutation);
                return new AnimationPoseSourcePrepareResult(
                    sourceId,
                    playerNodeId,
                    m_FrameIdentity,
                    capture.CompletionIdentity,
                    instance.Output,
                    mutation.Kind,
                    new CharacterPoseSourceScalarReadView(
                        physicalIdentity,
                        in capture));
            }
            catch
            {
                Array.Clear(m_PendingPlans, planOffset, clips.Count);
                if (preparedResource)
                    SourcePool.DiscardPending(physicalIdentity);
                throw;
            }
        }

        public AnimationPoseSourceReleaseToken StageRelease(
            AnimationPoseSourceId sourceId,
            PoseNodeId playerNodeId,
            AnimationPhysicalSourceIdentity physicalIdentity)
        {
            RequireAvailable();
            RequireFramePhase(FramePhase.Preparing);
            CharacterAclSourceKey key = new CharacterAclSourceKey(sourceId, playerNodeId);
            if (m_Journal.FindMutation(key) >= 0 || HasRelease(key))
                throw new InvalidOperationException("ACL pose source cannot be updated and released in one frame.");
            CharacterAclSourceInstance instance = SourcePool.RequireCommitted(
                physicalIdentity,
                key);
            return m_Journal.AddRelease(physicalIdentity, key, instance);
        }

        public void ValidateFrame(CharacterPoseSourceFrameLease lease)
        {
            RequireAvailable();
            RequireFrame(lease, FramePhase.Preparing);
            for (int i = 0; i < m_Journal.MutationCount; i++)
            {
                CharacterAclFrameJournal.Mutation mutation =
                    m_Journal.RequireMutation(i);
                if (!mutation.IsValid)
                    throw new InvalidOperationException("ACL source mutation journal is invalid.");
            }
            if (m_Journal.UnconsumedReleaseCount != m_Journal.ReleaseCount &&
                m_Journal.UnconsumedReleaseCount != 0)
                throw new InvalidOperationException("ACL source release journal is invalid.");
            m_FramePhase = FramePhase.Validated;
        }

        public void EnterEvaluateBarrier(CharacterPoseSourceFrameLease lease)
        {
            RequireAvailable();
            RequireFrame(lease, FramePhase.Validated);
            m_FramePhase = FramePhase.EvaluateBarrier;
            for (int i = 0; i < m_Journal.MutationCount; i++)
            {
                CharacterAclFrameJournal.Mutation mutation =
                    m_Journal.RequireMutation(i);
                var plans = new AnimationReadOnlyBuffer<ClipSamplePlan>(
                    m_PendingPlans,
                    mutation.PlanOffset,
                    mutation.PlanCount);
                mutation.Instance.ApplySamples(plans, in mutation.Capture);
            }
        }

        public void ApplyFrame(CharacterPoseSourceFrameLease lease)
        {
            RequireAvailable();
            RequireFrame(lease, FramePhase.EvaluateBarrier);
            if (m_FrameApplied)
                throw new InvalidOperationException("ACL pose source frame was already applied.");
            m_FrameApplied = true;
            for (int i = 0; i < m_Journal.MutationCount; i++)
            {
                CharacterAclFrameJournal.Mutation mutation =
                    m_Journal.RequireMutation(i);
                if (mutation.Kind == AnimationPoseSourcePrepareKind.PreparedResource)
                    SourcePool.Commit(mutation.PhysicalIdentity);
            }
        }

        public void ValidateAppliedFrame(CharacterPoseSourceFrameLease lease)
        {
            RequireAvailable();
            RequireFrame(lease, FramePhase.EvaluateBarrier);
            if (!m_FrameApplied)
                throw new InvalidOperationException("ACL pose source frame was not applied.");
        }

        public void FinalizeAppliedFrame(CharacterPoseSourceFrameLease lease)
        {
            if (m_FramePhase != FramePhase.EvaluateBarrier || !m_FrameApplied)
                return;
            Array.Clear(m_PendingPlans, 0, checked(m_Journal.MutationCount * m_ClipCapacity));
            m_Journal.ClearMutations();
            m_FrameIdentity = 0;
            m_FrameApplied = false;
            m_FramePhase = FramePhase.Closed;
        }

        public void RollbackAppliedFrame(CharacterPoseSourceFrameLease lease)
        {
            RequireAvailable();
            if (m_FramePhase == FramePhase.Closed)
                return;
            RequireFrame(lease, FramePhase.EvaluateBarrier);
            for (int i = m_Journal.MutationCount - 1; i >= 0; i--)
            {
                CharacterAclFrameJournal.Mutation mutation =
                    m_Journal.RequireMutation(i);
                Array.Clear(m_PendingPlans, mutation.PlanOffset, mutation.PlanCount);
                if (mutation.Kind == AnimationPoseSourcePrepareKind.PreparedResource)
                {
                    if (m_FrameApplied)
                        SourcePool.RollbackCommit(mutation.PhysicalIdentity);
                    SourcePool.DiscardPending(mutation.PhysicalIdentity);
                }
            }
            m_Journal.ClearMutations();
            m_Journal.ClearReleases();
            m_FrameIdentity = 0;
            m_FrameApplied = false;
            m_FramePhase = FramePhase.Closed;
        }

        public void DiscardFrame(CharacterPoseSourceFrameLease lease)
        {
            RequireAvailable();
            if (m_FramePhase == FramePhase.EvaluateBarrier)
            {
                RollbackAppliedFrame(lease);
                return;
            }
            if ((m_FramePhase != FramePhase.Preparing && m_FramePhase != FramePhase.Validated) ||
                m_FrameIdentity != lease.FrameIdentity)
                throw new InvalidOperationException("ACL pose source frame cannot be discarded.");
            for (int i = m_Journal.MutationCount - 1; i >= 0; i--)
            {
                CharacterAclFrameJournal.Mutation mutation =
                    m_Journal.RequireMutation(i);
                Array.Clear(m_PendingPlans, mutation.PlanOffset, mutation.PlanCount);
                if (mutation.Kind == AnimationPoseSourcePrepareKind.PreparedResource)
                    SourcePool.DiscardPending(mutation.PhysicalIdentity);
            }
            m_Journal.ClearMutations();
            m_Journal.ClearReleases();
            m_FrameIdentity = 0;
            m_FrameApplied = false;
            m_FramePhase = FramePhase.Closed;
        }

        public void Release(in AnimationPoseSourceReleaseToken token)
        {
            RequireAvailable();
            RequireFramePhase(FramePhase.Closed);
            CharacterAclFrameJournal.ReleasePermission permission =
                m_Journal.ConsumeRelease(in token);
            if (m_DeferredReleaseCount >= m_DeferredReleases.Length)
                throw new InvalidOperationException("ACL deferred release capacity was exceeded.");
            CharacterAclSourceInstance instance = SourcePool.Retire(
                permission.PhysicalIdentity,
                permission.Key);
            if (!ReferenceEquals(instance, permission.Instance))
                throw new InvalidOperationException("ACL source release instance does not match its physical identity.");
            m_DeferredReleases[m_DeferredReleaseCount++] = instance;
        }

        public bool ContainsCommitted(AnimationPoseSourceId sourceId, PoseNodeId playerNodeId)
        {
            RequireAvailable();
            return SourcePool.ContainsCommitted(sourceId, playerNodeId);
        }

        public ClipSamplePlan RequireDominantClipSample(
            AnimationPoseSourceId sourceId,
            PoseNodeId playerNodeId,
            ulong completionIdentity)
        {
            RequireAvailable();
            RequireFramePhase(FramePhase.EvaluateBarrier);
            if (completionIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));
            CharacterAclSourceKey key = new CharacterAclSourceKey(sourceId, playerNodeId);
            int mutationIndex = m_Journal.FindMutation(key);
            if (mutationIndex < 0)
                throw new InvalidOperationException("ACL source has no sample in the current Evaluate Barrier.");
            CharacterAclFrameJournal.Mutation mutation =
                m_Journal.RequireMutation(mutationIndex);
            if (mutation.Capture.CompletionIdentity != completionIdentity)
                throw new InvalidOperationException("ACL dominant sample completion is stale.");
            return mutation.Instance.Samples.RequireDominant();
        }

        public void ExecuteDeferredReleases()
        {
            RequireAvailable();
            RequireFramePhase(FramePhase.Closed);
            if (m_Journal.UnconsumedReleaseCount != 0)
                throw new InvalidOperationException("ACL source release permissions were not fully consumed.");
            RecycleDeferredReleases();
            m_Journal.ClearReleases();
        }

        public void Clear()
        {
            RequireAvailable();
            RequireFramePhase(FramePhase.Closed);
            if (m_Journal.ReleaseCount != 0)
                throw new InvalidOperationException("ACL source releases are not finalized.");
            RecycleDeferredReleases();
            SourcePool.Clear();
            m_Journal.ClearMutations();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            if (m_FramePhase != FramePhase.Closed)
                throw new InvalidOperationException("ACL pose sampling backend cannot dispose during a frame.");
            RecycleDeferredReleases();
            m_Journal.ClearReleases();
            m_Resources.Dispose();
            m_Disposed = true;
        }

        void RecycleDeferredReleases()
        {
            while (m_DeferredReleaseCount > 0)
            {
                int index = m_DeferredReleaseCount - 1;
                SourcePool.Recycle(m_DeferredReleases[index]);
                m_DeferredReleases[index] = null;
                m_DeferredReleaseCount = index;
            }
        }

        bool HasRelease(CharacterAclSourceKey key)
            => m_Journal.ContainsRelease(key);

        static void ValidateCatalog(
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> catalog)
        {
            for (int i = 0; i < catalog.Count; i++)
            {
                AnimationPoseSourceClipBinding binding = catalog[i];
                if (!binding.IsValid || !binding.IsAcl || binding.ClipBindingIndex != i)
                    throw new InvalidOperationException("ACL source catalog is invalid.");
            }
        }

        void RequireFrame(
            CharacterPoseSourceFrameLease lease,
            FramePhase phase)
        {
            RequireFramePhase(phase);
            if (!lease.IsValid || m_FrameIdentity != lease.FrameIdentity)
                throw new InvalidOperationException("ACL pose source frame identity is stale.");
        }

        void RequireFramePhase(FramePhase phase)
        {
            if (m_FramePhase != phase)
                throw new InvalidOperationException($"ACL pose source frame phase must be {phase}, actual {m_FramePhase}.");
        }

        void RequireAvailable()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterAclPoseSamplingBackend));
            if (!m_Graph.IsValid())
                throw new InvalidOperationException("ACL pose sampling graph is unavailable.");
        }

        static void RecordFailure(ref Exception failure, Exception exception)
        {
            failure = failure == null
                ? exception
                : new AggregateException(failure, exception);
        }
    }
}
