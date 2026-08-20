using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    readonly struct ActionCommittedPresentationDiagnostic
    {
        internal ActionCommittedPresentationDiagnostic(
            ulong presentationFrame,
            in ActionAnimationPlaybackLifecycleFrame lifecycle,
            in ProjectedActionPresentationSample projected,
            in ActionCommittedSampleWindow window)
        {
            PresentationFrame = presentationFrame;
            Lifecycle = lifecycle;
            Projected = projected;
            Window = window;
        }

        internal ulong PresentationFrame { get; }
        internal ActionAnimationPlaybackLifecycleFrame Lifecycle { get; }
        internal ProjectedActionPresentationSample Projected { get; }
        internal ActionCommittedSampleWindow Window { get; }
    }

    public sealed class ActionPresentationSamplingFrameTransaction
    {
        struct SampleEntry
        {
            internal AnimationPlaybackId PlaybackId;
            internal ProjectedActionPresentationSample Projected;
            internal ActionCommittedSampleWindow Window;
        }

        readonly ActionAnimationPlaybackLifecycleFrame[] m_LifecycleFrames;
        readonly SampleEntry[] m_Samples;
        int m_LifecycleCount;
        int m_SampleCount;

        internal ActionPresentationSamplingFrameTransaction(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_LifecycleFrames =
                new ActionAnimationPlaybackLifecycleFrame[capacity];
            m_Samples = new SampleEntry[capacity];
            Closed = true;
        }

        public ulong Identity { get; private set; }
        public ulong PresentationFrame { get; private set; }
        internal ActionSampleProjectionMutationLease ProjectionLease
        {
            get;
            private set;
        }
        internal bool CaptureDiagnostics { get; private set; }
        internal int LifecycleCount => m_LifecycleCount;
        internal int SampleCount => m_SampleCount;
        internal ActionAnimationPlaybackLifecycleFrame LifecycleAt(int index) =>
            (uint)index < (uint)m_LifecycleCount
                ? m_LifecycleFrames[index]
                : throw new ArgumentOutOfRangeException(nameof(index));
        internal bool Closed { get; private set; }
        public bool IsValid =>
            Identity != 0 &&
            PresentationFrame != 0 &&
            ProjectionLease.IsValid &&
            !Closed;

        internal void Begin(
            ulong identity,
            ulong presentationFrame,
            ActionSampleProjectionMutationLease projectionLease,
            bool captureDiagnostics)
        {
            if (!Closed ||
                identity == 0 ||
                presentationFrame == 0 ||
                !projectionLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Action presentation sampling transaction cannot begin.");
            }
            Identity = identity;
            PresentationFrame = presentationFrame;
            ProjectionLease = projectionLease;
            CaptureDiagnostics = captureDiagnostics;
            m_LifecycleCount = 0;
            m_SampleCount = 0;
            Closed = false;
        }

        internal void AddLifecycle(
            in ActionAnimationPlaybackLifecycleFrame lifecycle)
        {
            if (m_LifecycleCount == m_LifecycleFrames.Length)
            {
                throw new InvalidOperationException(
                    "Action sampling lifecycle capacity was exceeded.");
            }
            m_LifecycleFrames[m_LifecycleCount++] = lifecycle;
        }

        internal void AddSample(
            AnimationPlaybackId playbackId,
            in ProjectedActionPresentationSample projected,
            in ActionCommittedSampleWindow window)
        {
            if (FindSample(playbackId) >= 0)
            {
                throw new InvalidOperationException(
                    $"Action playback '{playbackId}' produced more than one projected sample.");
            }
            if (m_SampleCount == m_Samples.Length)
            {
                throw new InvalidOperationException(
                    "Action sampling projected sample capacity was exceeded.");
            }
            m_Samples[m_SampleCount++] = new SampleEntry
            {
                PlaybackId = playbackId,
                Projected = projected,
                Window = window
            };
        }

        internal bool TryGetSample(
            AnimationPlaybackId playbackId,
            out ProjectedActionPresentationSample projected,
            out ActionCommittedSampleWindow window)
        {
            int index = FindSample(playbackId);
            if (index >= 0)
            {
                projected = m_Samples[index].Projected;
                window = m_Samples[index].Window;
                return true;
            }
            projected = default;
            window = default;
            return false;
        }

        internal bool HasProjectedSample(AnimationPlaybackId playbackId) =>
            FindSample(playbackId) >= 0;

        internal bool TryGetLifecycle(
            AnimationPlaybackId playbackId,
            out ActionAnimationPlaybackLifecycleFrame lifecycle)
        {
            for (int i = 0; i < m_LifecycleCount; i++)
            {
                if (!m_LifecycleFrames[i].PlaybackId.Equals(playbackId))
                    continue;
                lifecycle = m_LifecycleFrames[i];
                return true;
            }
            lifecycle = default;
            return false;
        }

        internal void GetSample(
            int index,
            out AnimationPlaybackId playbackId,
            out ProjectedActionPresentationSample projected,
            out ActionCommittedSampleWindow window)
        {
            if ((uint)index >= (uint)m_SampleCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            SampleEntry entry = m_Samples[index];
            playbackId = entry.PlaybackId;
            projected = entry.Projected;
            window = entry.Window;
        }

        internal void Close()
        {
            if (m_LifecycleCount > 0)
            {
                Array.Clear(
                    m_LifecycleFrames,
                    0,
                    m_LifecycleCount);
            }
            if (m_SampleCount > 0)
                Array.Clear(m_Samples, 0, m_SampleCount);
            Identity = 0;
            PresentationFrame = 0;
            ProjectionLease = default;
            CaptureDiagnostics = false;
            m_LifecycleCount = 0;
            m_SampleCount = 0;
            Closed = true;
        }

        int FindSample(AnimationPlaybackId playbackId)
        {
            for (int i = 0; i < m_SampleCount; i++)
            {
                if (m_Samples[i].PlaybackId.Equals(playbackId))
                    return i;
            }
            return -1;
        }
    }

    public sealed class ActionPresentationSamplingRuntime
    {
        readonly ActionAnimationBindingIndex m_Bindings;
        readonly ActionPresentationSampleProjector m_SampleProjector;
        readonly ActionPresentationSampleWorkspace m_SampleWorkspace;
        readonly ActionPresentationSamplingFrameTransaction m_Transaction;
        readonly ActionCommittedPresentationDiagnostic[]
            m_CommittedDiagnostics;
        readonly int m_FootPlacementWeightParameterIndex;
        int m_CommittedDiagnosticCount;
        ActionPresentationSamplingFrameTransaction m_ActiveTransaction;

        public ActionPresentationSamplingRuntime(
            ActionAnimationBindingIndex bindings)
        {
            m_Bindings = bindings ??
                throw new ArgumentNullException(nameof(bindings));
            CharacterPresentationPosePlan plan = bindings.Projection.PosePlan;
            int sourceCapacity = 0;
            for (int i = 0; i < plan.AnimationSlots.Count; i++)
            {
                sourceCapacity = checked(
                    sourceCapacity +
                    plan.AnimationSlots[i].BlendStackWorkspace.Capacity);
            }
            int playbackCapacity = Math.Max(
                1,
                checked(sourceCapacity + bindings.Bindings.Count));
            int clipStride = 0;
            foreach (KeyValuePair<AnimationProducerId,
                         ResolvedActionAnimationBinding> pair in
                     bindings.Bindings)
            {
                clipStride = Math.Max(
                    clipStride,
                    pair.Value.AuthoredClipCount);
            }
            if (plan.Parameters.Count <= 0)
            {
                throw new InvalidOperationException(
                    "Action presentation sampling has no compiled workspace capacity.");
            }
            if (bindings.Bindings.Count == 0)
            {
                if (sourceCapacity != 0 ||
                    plan.AnimationSlots.Count != 0)
                {
                    throw new InvalidOperationException(
                        "Animation Slot plan exists without finite Action bindings.");
                }
            }
            else
            {
                if (sourceCapacity <= 0 || clipStride <= 0)
                {
                    throw new InvalidOperationException(
                        "Finite Action bindings have no Animation Slot workspace.");
                }
                m_SampleWorkspace =
                    new ActionPresentationSampleWorkspace(
                        playbackCapacity,
                        clipStride,
                        plan.Parameters.Count);
            }
            m_SampleProjector =
                new ActionPresentationSampleProjector(playbackCapacity);
            m_Transaction =
                new ActionPresentationSamplingFrameTransaction(
                    playbackCapacity);
            m_CommittedDiagnostics =
                new ActionCommittedPresentationDiagnostic[playbackCapacity];
            m_FootPlacementWeightParameterIndex =
                plan.RequireParameterIndex(
                    AnimationPoseParameterIds.FootPlacementWeight);
        }

        public bool HasActiveFrameTransaction =>
            m_ActiveTransaction != null &&
            m_ActiveTransaction.IsValid;
        internal int JournalCapacity =>
            m_CommittedDiagnostics.Length;

        public ActionPresentationSamplingFrameTransaction BeginFrame(
            ulong frameIdentity,
            ulong presentationFrame,
            bool captureDiagnostics)
        {
            if (HasActiveFrameTransaction)
            {
                throw new InvalidOperationException(
                    "Action presentation sampling already has an active frame transaction.");
            }
            if (frameIdentity == 0 || presentationFrame == 0)
                throw new ArgumentOutOfRangeException(nameof(presentationFrame));
            ActionSampleProjectionMutationLease projectionLease = default;
            try
            {
                m_SampleWorkspace?.BeginFrame(presentationFrame);
                projectionLease = m_SampleProjector.BeginMutation();
                m_Transaction.Begin(
                    frameIdentity,
                    presentationFrame,
                    projectionLease,
                    captureDiagnostics);
                m_ActiveTransaction = m_Transaction;
                return m_Transaction;
            }
            catch
            {
                if (projectionLease.IsValid)
                    m_SampleProjector.Discard(projectionLease);
                m_SampleWorkspace?.DiscardFrame();
                if (m_Transaction.IsValid)
                    m_Transaction.Close();
                throw;
            }
        }

        internal void ProjectPresentationSamples(
            ActionPresentationSamplingFrameTransaction transaction,
            CharacterActionPlaybackRuntime actionRuntime,
            CharacterActionPlaybackFrameTransaction actionTransaction,
            IReadOnlyList<ActionAnimationPlaybackLifecycleFrame>
                lifecycleFrames,
            double presentationSampleTick,
            float presentationDeltaSeconds)
        {
            RequireTransaction(transaction);
            if (actionRuntime == null ||
                actionTransaction == null ||
                !actionTransaction.IsValid ||
                actionTransaction.PresentationFrame !=
                    transaction.PresentationFrame ||
                lifecycleFrames == null ||
                !double.IsFinite(presentationSampleTick) ||
                presentationSampleTick < 0d ||
                !float.IsFinite(presentationDeltaSeconds) ||
                presentationDeltaSeconds < 0f)
            {
                throw new ArgumentException(
                    "Action presentation projection input is invalid.");
            }
            if (transaction.LifecycleCount != 0)
            {
                throw new InvalidOperationException(
                    "Action presentation samples were already projected for this frame.");
            }
            for (int i = 0; i < lifecycleFrames.Count; i++)
            {
                ActionAnimationPlaybackLifecycleFrame lifecycle =
                    lifecycleFrames[i];
                transaction.AddLifecycle(in lifecycle);
                if (lifecycle.Phase ==
                    ActionAnimationPlaybackLifecyclePhase.Retired)
                {
                    m_SampleProjector.RemovePlayback(
                        transaction.ProjectionLease,
                        lifecycle.PlaybackId);
                    continue;
                }
                if (lifecycle.Phase ==
                    ActionAnimationPlaybackLifecyclePhase.PendingFirstSample)
                {
                    continue;
                }
                if (!lifecycle.HasCommittedRawSample)
                {
                    if (lifecycle.FirstSampleReadiness ==
                        ActionFirstSampleReadiness.Unavailable)
                    {
                        continue;
                    }
                    throw new InvalidOperationException(
                        $"Action playback '{lifecycle.PlaybackId}' has no committed presentation sample.");
                }
                if (!m_Bindings.TryGet(
                        lifecycle.PlaybackId.ProducerId,
                        out ResolvedActionAnimationBinding binding) ||
                    !actionRuntime.TryGetCommittedSampleWindow(
                        actionTransaction,
                        lifecycle.PlaybackId,
                        presentationSampleTick,
                        out ActionCommittedSampleWindow window))
                {
                    throw new InvalidOperationException(
                        $"Action playback '{lifecycle.PlaybackId}' cannot produce an exact presentation frame.");
                }
                ProjectedActionPresentationSample projected =
                    m_SampleProjector.Project(
                        transaction.ProjectionLease,
                        lifecycle.PlaybackId,
                        in window,
                        presentationSampleTick,
                        presentationDeltaSeconds,
                        binding.Animation.DurationSeconds,
                        binding.Animation.LastSampleTimeSeconds,
                        lifecycle.Phase);
                transaction.AddSample(
                    lifecycle.PlaybackId,
                    in projected,
                    in window);
            }
        }

        public void ResolvePresentationFrames(
            ActionPresentationSamplingFrameTransaction transaction,
            PresentationFrameWorkspace frameWorkspace,
            PresentationFrameWorkspaceLease frameLease)
        {
            RequireTransaction(transaction);
            if (frameWorkspace == null ||
                !frameLease.IsValid ||
                frameLease.PresentationFrame != transaction.PresentationFrame)
            {
                throw new ArgumentException(
                    "Action presentation frame workspace is invalid.");
            }
            for (int i = 0; i < transaction.LifecycleCount; i++)
            {
                ActionAnimationPlaybackLifecycleFrame lifecycle =
                    transaction.LifecycleAt(i);
                if (lifecycle.Phase ==
                        ActionAnimationPlaybackLifecyclePhase.PendingFirstSample ||
                    lifecycle.Phase ==
                        ActionAnimationPlaybackLifecyclePhase.Retired)
                {
                    continue;
                }
                ResolvedActionAnimationBinding binding =
                    RequireProjectedBinding(
                        transaction,
                        lifecycle.PlaybackId,
                        out ProjectedActionPresentationSample projected);
                if (m_SampleWorkspace == null)
                {
                    throw new InvalidOperationException(
                        "Action presentation frame has no compiled sample workspace.");
                }
                ActionAnimationPlaybackFrame frame =
                    ActionPresentationSampleResolver.Resolve(
                        m_SampleWorkspace,
                        m_Bindings,
                        in binding,
                        in lifecycle,
                        in projected,
                        m_FootPlacementWeightParameterIndex);
                frameWorkspace.SetActionFrame(frameLease, in frame);
            }
        }

        internal void ValidateFrame(
            ActionPresentationSamplingFrameTransaction transaction)
        {
            RequireTransaction(transaction);
            m_SampleProjector.ValidateFrame(
                transaction.ProjectionLease);
        }

        internal void BuildCommittedTimeSnapshots(
            FixedCapacityFrameBuffer<ActionPresentationTimeSnapshot>
                destination)
        {
            if (HasActiveFrameTransaction)
            {
                throw new InvalidOperationException(
                    "Action time diagnostics cannot read an active frame.");
            }
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            for (int i = 0; i < m_CommittedDiagnosticCount; i++)
            {
                ActionCommittedPresentationDiagnostic diagnostic =
                    m_CommittedDiagnostics[i];
                ActionAnimationPlaybackLifecycleFrame lifecycle =
                    diagnostic.Lifecycle;
                ProjectedActionPresentationSample projected =
                    diagnostic.Projected;
                destination.Add(
                    new ActionPresentationTimeSnapshot(
                        lifecycle.PlaybackId,
                        lifecycle.ActionInstanceId,
                        diagnostic.PresentationFrame,
                        lifecycle.Phase,
                        diagnostic.Window,
                        projected.ProjectedRawSample,
                        projected.RetentionProjection));
            }
        }

        public void SealFrame(
            ActionPresentationSamplingFrameTransaction transaction)
        {
            RequireTransaction(transaction);
            m_SampleProjector.Commit(transaction.ProjectionLease);
            m_SampleWorkspace?.CommitFrame();
            CaptureCommittedDiagnostics(transaction);
            Close(transaction);
        }

        public void DiscardFrame(
            ActionPresentationSamplingFrameTransaction transaction)
        {
            RequireTransaction(transaction);
            m_SampleProjector.Discard(transaction.ProjectionLease);
            m_SampleWorkspace?.DiscardFrame();
            Close(transaction);
        }

        public void Reset()
        {
            if (HasActiveFrameTransaction)
            {
                throw new InvalidOperationException(
                    "Action presentation sampling cannot reset during a frame transaction.");
            }
            m_SampleProjector.Reset();
            m_SampleWorkspace?.Reset();
            if (m_CommittedDiagnosticCount > 0)
            {
                Array.Clear(
                    m_CommittedDiagnostics,
                    0,
                    m_CommittedDiagnosticCount);
                m_CommittedDiagnosticCount = 0;
            }
        }

        void CaptureCommittedDiagnostics(
            ActionPresentationSamplingFrameTransaction transaction)
        {
            if (m_CommittedDiagnosticCount > 0)
            {
                Array.Clear(
                    m_CommittedDiagnostics,
                    0,
                    m_CommittedDiagnosticCount);
            }
            m_CommittedDiagnosticCount = 0;
            if (!transaction.CaptureDiagnostics)
                return;
            for (int i = 0; i < transaction.SampleCount; i++)
            {
                transaction.GetSample(
                    i,
                    out AnimationPlaybackId playbackId,
                    out ProjectedActionPresentationSample projected,
                    out ActionCommittedSampleWindow window);
                if (!transaction.TryGetLifecycle(
                        playbackId,
                        out ActionAnimationPlaybackLifecycleFrame lifecycle) ||
                    lifecycle.Phase ==
                        ActionAnimationPlaybackLifecyclePhase.PendingFirstSample ||
                    lifecycle.Phase ==
                        ActionAnimationPlaybackLifecyclePhase.Retired)
                {
                    continue;
                }
                if (m_CommittedDiagnosticCount ==
                    m_CommittedDiagnostics.Length)
                {
                    throw new InvalidOperationException(
                        "Committed Action diagnostics capacity was exceeded after validation.");
                }
                m_CommittedDiagnostics[m_CommittedDiagnosticCount++] =
                    new ActionCommittedPresentationDiagnostic(
                        transaction.PresentationFrame,
                        in lifecycle,
                        in projected,
                        in window);
            }
        }

        ResolvedActionAnimationBinding RequireProjectedBinding(
            ActionPresentationSamplingFrameTransaction transaction,
            AnimationPlaybackId playbackId,
            out ProjectedActionPresentationSample projected)
        {
            if (!playbackId.IsValid ||
                !transaction.TryGetSample(
                    playbackId,
                    out projected,
                    out _) ||
                !m_Bindings.TryGet(
                    playbackId.ProducerId,
                    out ResolvedActionAnimationBinding binding))
            {
                throw new InvalidOperationException(
                    $"Action playback '{playbackId}' has no projected sample or compiled binding.");
            }
            return binding;
        }

        void RequireTransaction(
            ActionPresentationSamplingFrameTransaction transaction)
        {
            if (transaction == null ||
                !transaction.IsValid ||
                !ReferenceEquals(transaction, m_ActiveTransaction))
            {
                throw new InvalidOperationException(
                    "Action presentation sampling transaction is invalid.");
            }
        }

        void Close(ActionPresentationSamplingFrameTransaction transaction)
        {
            transaction.Close();
            m_ActiveTransaction = null;
        }
    }
}
