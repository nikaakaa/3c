using System;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    public readonly struct ActionSampleProjectionMutationLease
    {
        internal ActionSampleProjectionMutationLease(ulong identity)
        {
            Identity = identity;
        }

        public ulong Identity { get; }
        public bool IsValid => Identity != 0;
    }

    public sealed class ActionPresentationSampleProjector
    {
        struct Cursor
        {
            internal bool Occupied;
            internal bool Remove;
            internal AnimationPlaybackId PlaybackId;
            internal double ContinuousTime;
            internal bool Initialized;
            internal Projection Projection;
        }

        readonly struct Projection
        {
            readonly ActionCommittedSampleWindow m_Window;
            readonly double m_SampleTick;
            readonly float m_DeltaSeconds;
            readonly float m_SourceDurationSeconds;
            readonly float m_LastSampleTimeSeconds;
            readonly ActionAnimationPlaybackLifecyclePhase m_Phase;

            internal Projection(in ActionCommittedSampleWindow window, double sampleTick,
                float deltaSeconds, float sourceDurationSeconds, float lastSampleTimeSeconds,
                ActionAnimationPlaybackLifecyclePhase phase, ProjectedActionPresentationSample sample)
            {
                m_Window = window;
                m_SampleTick = sampleTick;
                m_DeltaSeconds = deltaSeconds;
                m_SourceDurationSeconds = sourceDurationSeconds;
                m_LastSampleTimeSeconds = lastSampleTimeSeconds;
                m_Phase = phase;
                Sample = sample;
            }

            internal ProjectedActionPresentationSample Sample { get; }

            internal bool Matches(in ActionCommittedSampleWindow window, double sampleTick,
                float deltaSeconds, float sourceDurationSeconds, float lastSampleTimeSeconds,
                ActionAnimationPlaybackLifecyclePhase phase) =>
                m_SampleTick == sampleTick && m_DeltaSeconds == deltaSeconds &&
                m_SourceDurationSeconds == sourceDurationSeconds && m_LastSampleTimeSeconds == lastSampleTimeSeconds &&
                m_Phase == phase && m_Window.HasNext == window.HasNext &&
                SameSample(m_Window.Previous, window.Previous) &&
                (!window.HasNext || SameSample(m_Window.Next, window.Next));

            static bool SameSample(in ActionCommittedRawSample left, in ActionCommittedRawSample right) =>
                left.EventId.Equals(right.EventId) && left.LocalLogicTick == right.LocalLogicTick &&
                left.CommittedSequence == right.CommittedSequence && left.VisualTime == right.VisualTime &&
                left.ContinuousVisualTime == right.ContinuousVisualTime && left.Cycle == right.Cycle &&
                left.Loop == right.Loop && left.VisualTimeScale == right.VisualTimeScale &&
                left.ProducerWeight == right.ProducerWeight;
        }

        readonly Cursor[] m_CommittedCursors;
        readonly Cursor[] m_PendingCursors;
        readonly int[] m_PendingCommittedIndices;
        readonly int[] m_PendingTargetIndices;
        readonly bool[] m_ReservedCommittedSlots;
        int m_PendingCount;
        ulong m_NextLeaseIdentity;
        ActionSampleProjectionMutationLease m_ActiveLease;
        bool m_Validated;

        public ActionPresentationSampleProjector(int playbackCapacity)
        {
            if (playbackCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(playbackCapacity));
            m_CommittedCursors = new Cursor[playbackCapacity];
            m_PendingCursors = new Cursor[playbackCapacity];
            m_PendingCommittedIndices = new int[playbackCapacity];
            m_PendingTargetIndices = new int[playbackCapacity];
            m_ReservedCommittedSlots = new bool[playbackCapacity];
        }

        public ActionSampleProjectionMutationLease BeginMutation()
        {
            if (m_ActiveLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Action sample projector already has an active mutation.");
            }
            m_NextLeaseIdentity++;
            if (m_NextLeaseIdentity == 0)
                m_NextLeaseIdentity++;
            m_PendingCount = 0;
            m_Validated = false;
            m_ActiveLease =
                new ActionSampleProjectionMutationLease(m_NextLeaseIdentity);
            return m_ActiveLease;
        }

        public ProjectedActionPresentationSample Project(
            ActionSampleProjectionMutationLease lease,
            AnimationPlaybackId playbackId,
            in ActionCommittedSampleWindow window,
            double presentationSampleTick,
            float presentationDeltaSeconds,
            float sourceDurationSeconds,
            float lastSampleTimeSeconds,
            ActionAnimationPlaybackLifecyclePhase phase)
        {
            RequireLease(lease);
            if (!playbackId.IsValid ||
                !window.IsValid ||
                !double.IsFinite(presentationSampleTick) ||
                presentationSampleTick < 0d ||
                !float.IsFinite(presentationDeltaSeconds) ||
                presentationDeltaSeconds < 0f ||
                !float.IsFinite(sourceDurationSeconds) ||
                sourceDurationSeconds <= 0f ||
                !float.IsFinite(lastSampleTimeSeconds) ||
                lastSampleTimeSeconds <= 0f ||
                lastSampleTimeSeconds > sourceDurationSeconds ||
                phase != ActionAnimationPlaybackLifecyclePhase.Selected &&
                phase != ActionAnimationPlaybackLifecyclePhase.Retained &&
                phase !=
                    ActionAnimationPlaybackLifecyclePhase.RetirementPermitted)
            {
                throw new ArgumentException(
                    "Action presentation sample projection request is invalid.");
            }

            ref Cursor cursor = ref GetWritable(playbackId);
            if (cursor.Remove)
                throw new InvalidOperationException("Released Action playback cannot be projected in the same frame.");
            if (cursor.Projection.Sample.IsValid)
            {
                if (!cursor.Projection.Matches(in window, presentationSampleTick, presentationDeltaSeconds,
                        sourceDurationSeconds, lastSampleTimeSeconds, phase))
                    throw new InvalidOperationException("Action playback received conflicting projection inputs in one frame.");
                return cursor.Projection.Sample;
            }
            double target = Interpolate(in window, presentationSampleTick);
            bool retention =
                phase == ActionAnimationPlaybackLifecyclePhase.Retained ||
                phase ==
                    ActionAnimationPlaybackLifecyclePhase.RetirementPermitted;
            if (retention && !window.HasNext)
            {
                double anchor = cursor.Initialized
                    ? Math.Max(
                        cursor.ContinuousTime,
                        window.Previous.ContinuousVisualTime)
                    : window.Previous.ContinuousVisualTime;
                target = anchor +
                         presentationDeltaSeconds *
                         window.Previous.VisualTimeScale;
            }
            target = Math.Max(0d, target);
            int cycle = window.Previous.Loop
                ? checked((int)Math.Floor(target / sourceDurationSeconds))
                : 0;
            float localTime = window.Previous.Loop
                ? (float)(target - cycle * (double)sourceDurationSeconds)
                : (float)Math.Min(target, lastSampleTimeSeconds);
            if (window.Previous.Loop && localTime >= sourceDurationSeconds)
            {
                cycle = checked(cycle + 1);
                localTime = 0f;
            }
            if (localTime > lastSampleTimeSeconds)
                localTime = lastSampleTimeSeconds;
            double continuousTime = window.Previous.Loop
                ? cycle * (double)sourceDurationSeconds + localTime
                : localTime;
            cursor.ContinuousTime = continuousTime;
            cursor.Initialized = true;
            cursor.Remove = false;
            var sample = new ProjectedActionPresentationSample(
                playbackId,
                window.HasNext
                    ? window.Next.EventId
                    : window.Previous.EventId,
                new PresentationPoseSampleTime(
                    localTime,
                    continuousTime,
                    cycle,
                    window.Previous.Loop,
                    window.Previous.VisualTimeScale),
                retention && !window.HasNext);
            cursor.Projection = new Projection(in window, presentationSampleTick, presentationDeltaSeconds,
                sourceDurationSeconds, lastSampleTimeSeconds, phase, sample);
            return sample;
        }

        public void RemovePlayback(
            ActionSampleProjectionMutationLease lease,
            AnimationPlaybackId playbackId)
        {
            RequireLease(lease);
            if (!playbackId.IsValid)
            {
                throw new ArgumentException(
                    "Action playback id is invalid.",
                    nameof(playbackId));
            }
            int pendingIndex = FindPending(playbackId);
            int committedIndex = FindCommitted(playbackId);
            if (pendingIndex < 0 && committedIndex < 0)
                return;
            ref Cursor cursor = ref GetWritable(playbackId);
            cursor.Remove = true;
        }

        public void ValidateFrame(ActionSampleProjectionMutationLease lease)
        {
            RequireLease(lease);
            for (int i = 0; i < m_CommittedCursors.Length; i++)
            {
                m_ReservedCommittedSlots[i] = m_CommittedCursors[i].Occupied;
            }
            for (int i = 0; i < m_PendingCount; i++)
            {
                int committedIndex = m_PendingCommittedIndices[i];
                m_PendingTargetIndices[i] = committedIndex;
                if (m_PendingCursors[i].Remove &&
                    committedIndex >= 0)
                {
                    m_ReservedCommittedSlots[committedIndex] = false;
                }
            }
            int nextFreeSlot = 0;
            for (int i = 0; i < m_PendingCount; i++)
            {
                if (m_PendingCursors[i].Remove ||
                    m_PendingTargetIndices[i] >= 0)
                {
                    continue;
                }
                while (nextFreeSlot < m_ReservedCommittedSlots.Length &&
                       m_ReservedCommittedSlots[nextFreeSlot])
                    nextFreeSlot++;
                if (nextFreeSlot == m_ReservedCommittedSlots.Length)
                {
                    throw new InvalidOperationException(
                        "Action sample cursor committed capacity was exceeded.");
                }
                m_PendingTargetIndices[i] = nextFreeSlot;
                m_ReservedCommittedSlots[nextFreeSlot++] = true;
            }
            m_Validated = true;
        }

        public void Commit(ActionSampleProjectionMutationLease lease)
        {
            RequireLease(lease);
            if (!m_Validated)
            {
                throw new InvalidOperationException(
                    "Action sample cursor was not validated before Seal.");
            }
            for (int i = 0; i < m_PendingCount; i++)
            {
                Cursor pending = m_PendingCursors[i];
                int committedIndex = m_PendingCommittedIndices[i];
                if (pending.Remove && committedIndex >= 0)
                    m_CommittedCursors[committedIndex] = default;
            }
            for (int i = 0; i < m_PendingCount; i++)
            {
                Cursor pending = m_PendingCursors[i];
                int committedIndex = m_PendingTargetIndices[i];
                if (pending.Remove)
                    continue;
                pending.Remove = false;
                pending.Projection = default;
                m_CommittedCursors[committedIndex] = pending;
            }
            Close();
        }

        public void Discard(ActionSampleProjectionMutationLease lease)
        {
            RequireLease(lease);
            Close();
        }

        public void Reset()
        {
            if (m_ActiveLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Action sample projector cannot reset during mutation.");
            }
            Array.Clear(
                m_CommittedCursors,
                0,
                m_CommittedCursors.Length);
        }

        ref Cursor GetWritable(AnimationPlaybackId playbackId)
        {
            m_Validated = false;
            int pendingIndex = FindPending(playbackId);
            if (pendingIndex >= 0)
                return ref m_PendingCursors[pendingIndex];
            if (m_PendingCount == m_PendingCursors.Length)
            {
                throw new InvalidOperationException(
                    "Action sample cursor pending capacity was exceeded.");
            }
            int committedIndex = FindCommitted(playbackId);
            Cursor cursor = committedIndex >= 0
                ? m_CommittedCursors[committedIndex]
                : new Cursor
                {
                    Occupied = true,
                    PlaybackId = playbackId
                };
            m_PendingCursors[m_PendingCount] = cursor;
            m_PendingCommittedIndices[m_PendingCount] = committedIndex;
            return ref m_PendingCursors[m_PendingCount++];
        }

        int FindPending(AnimationPlaybackId playbackId)
        {
            for (int i = 0; i < m_PendingCount; i++)
            {
                if (m_PendingCursors[i].PlaybackId.Equals(playbackId))
                    return i;
            }
            return -1;
        }

        int FindCommitted(AnimationPlaybackId playbackId)
        {
            for (int i = 0; i < m_CommittedCursors.Length; i++)
            {
                if (m_CommittedCursors[i].Occupied &&
                    m_CommittedCursors[i].PlaybackId.Equals(playbackId))
                {
                    return i;
                }
            }
            return -1;
        }

        static double Interpolate(
            in ActionCommittedSampleWindow window,
            double presentationSampleTick)
        {
            ActionCommittedRawSample previous = window.Previous;
            if (!window.HasNext ||
                presentationSampleTick <= previous.LocalLogicTick)
            {
                return previous.ContinuousVisualTime;
            }
            ActionCommittedRawSample next = window.Next;
            if (next.LocalLogicTick == previous.LocalLogicTick)
                return next.ContinuousVisualTime;
            double alpha = Math.Clamp(
                (presentationSampleTick - previous.LocalLogicTick) /
                (next.LocalLogicTick - previous.LocalLogicTick),
                0d,
                1d);
            return previous.ContinuousVisualTime +
                   (next.ContinuousVisualTime -
                    previous.ContinuousVisualTime) * alpha;
        }

        void RequireLease(ActionSampleProjectionMutationLease lease)
        {
            if (!lease.IsValid ||
                !m_ActiveLease.IsValid ||
                lease.Identity != m_ActiveLease.Identity)
            {
                throw new InvalidOperationException(
                    "Action sample projection lease is invalid.");
            }
        }

        void Close()
        {
            if (m_PendingCount > 0)
            {
                Array.Clear(
                    m_PendingCursors,
                    0,
                    m_PendingCount);
            }
            m_PendingCount = 0;
            m_ActiveLease = default;
            m_Validated = false;
        }
    }
}
