using System;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal readonly struct ActionPresentationSampleWorkspaceRow
    {
        internal ActionPresentationSampleWorkspaceRow(
            AnimationPlaybackId playbackId,
            int rowIndex,
            ulong leaseIdentity,
            ClipSamplePlan[] clips,
            int clipOffset,
            int clipCapacity,
            float[] poseParameters,
            byte[] poseParameterAvailability,
            int parameterOffset,
            int parameterCount)
        {
            PlaybackId = playbackId;
            RowIndex = rowIndex;
            LeaseIdentity = leaseIdentity;
            Clips = clips;
            ClipOffset = clipOffset;
            ClipCapacity = clipCapacity;
            PoseParameters = poseParameters;
            PoseParameterAvailability = poseParameterAvailability;
            ParameterOffset = parameterOffset;
            ParameterCount = parameterCount;
        }

        internal AnimationPlaybackId PlaybackId { get; }
        internal int RowIndex { get; }
        internal ulong LeaseIdentity { get; }
        internal ClipSamplePlan[] Clips { get; }
        internal int ClipOffset { get; }
        internal int ClipCapacity { get; }
        internal float[] PoseParameters { get; }
        internal byte[] PoseParameterAvailability { get; }
        internal int ParameterOffset { get; }
        internal int ParameterCount { get; }
    }

    internal sealed class ActionPresentationSampleWorkspace :
        IAnimationReadOnlyBufferLease
    {
        readonly int m_PlaybackCapacity;
        readonly int m_ClipStride;
        readonly int m_ParameterStride;
        readonly AnimationPlaybackId[] m_PlaybackIds;
        readonly ClipSamplePlan[] m_Clips;
        readonly float[] m_PoseParameters;
        readonly byte[] m_PoseParameterAvailability;
        ulong m_LeaseIdentity;
        ulong m_LastPresentationFrame;
        ulong m_PreviousPresentationFrame;
        bool m_FrameActive;

        internal ActionPresentationSampleWorkspace(
            int playbackCapacity,
            int clipStride,
            int parameterStride)
        {
            if (playbackCapacity <= 0 || clipStride <= 0 || parameterStride <= 0)
                throw new ArgumentOutOfRangeException(nameof(playbackCapacity));
            m_PlaybackCapacity = playbackCapacity;
            m_ClipStride = clipStride;
            m_ParameterStride = parameterStride;
            m_PlaybackIds = new AnimationPlaybackId[playbackCapacity];
            m_Clips = new ClipSamplePlan[checked(playbackCapacity * clipStride)];
            m_PoseParameters = new float[checked(playbackCapacity * parameterStride)];
            m_PoseParameterAvailability = new byte[m_PoseParameters.Length];
        }

        internal ulong LeaseIdentity => m_LeaseIdentity;

        internal void BeginFrame(ulong presentationFrame)
        {
            if (m_FrameActive)
                throw new InvalidOperationException(
                    "Action presentation sample workspace already has an active frame.");
            if (presentationFrame == 0 ||
                presentationFrame <= m_LastPresentationFrame)
            {
                throw new ArgumentOutOfRangeException(nameof(presentationFrame));
            }
            Array.Clear(m_PlaybackIds, 0, m_PlaybackIds.Length);
            Array.Clear(m_Clips, 0, m_Clips.Length);
            Array.Clear(m_PoseParameters, 0, m_PoseParameters.Length);
            Array.Clear(
                m_PoseParameterAvailability,
                0,
                m_PoseParameterAvailability.Length);
            m_PreviousPresentationFrame = m_LastPresentationFrame;
            m_LastPresentationFrame = presentationFrame;
            m_LeaseIdentity++;
            if (m_LeaseIdentity == 0)
                m_LeaseIdentity++;
            m_FrameActive = true;
        }

        internal ActionPresentationSampleWorkspaceRow Prepare(
            AnimationPlaybackId playbackId)
        {
            if (m_LeaseIdentity == 0 || !playbackId.IsValid)
                throw new InvalidOperationException(
                    "Action presentation sample workspace has no active frame.");
            int rowIndex = -1;
            for (int i = 0; i < m_PlaybackIds.Length; i++)
            {
                if (m_PlaybackIds[i].Equals(playbackId))
                    throw new InvalidOperationException(
                        $"Action playback '{playbackId}' was sampled twice in one frame.");
                if (rowIndex < 0 && !m_PlaybackIds[i].IsValid)
                    rowIndex = i;
            }
            if (rowIndex < 0)
                throw new InvalidOperationException(
                    $"Action presentation sample workspace capacity '{m_PlaybackCapacity}' was exceeded by playback '{playbackId}'.");
            m_PlaybackIds[rowIndex] = playbackId;
            int clipOffset = checked(rowIndex * m_ClipStride);
            int parameterOffset = checked(rowIndex * m_ParameterStride);
            return new ActionPresentationSampleWorkspaceRow(
                playbackId,
                rowIndex,
                m_LeaseIdentity,
                m_Clips,
                clipOffset,
                m_ClipStride,
                m_PoseParameters,
                m_PoseParameterAvailability,
                parameterOffset,
                m_ParameterStride);
        }

        internal void DiscardFrame()
        {
            if (!m_FrameActive)
                return;
            Array.Clear(m_PlaybackIds, 0, m_PlaybackIds.Length);
            m_LastPresentationFrame = m_PreviousPresentationFrame;
            m_PreviousPresentationFrame = 0;
            m_LeaseIdentity++;
            if (m_LeaseIdentity == 0)
                m_LeaseIdentity++;
            m_FrameActive = false;
        }

        internal void CommitFrame()
        {
            if (!m_FrameActive)
                throw new InvalidOperationException(
                    "Action presentation sample workspace has no active frame.");
            m_PreviousPresentationFrame = 0;
            m_FrameActive = false;
        }

        internal void Reset()
        {
            Array.Clear(m_PlaybackIds, 0, m_PlaybackIds.Length);
            Array.Clear(m_Clips, 0, m_Clips.Length);
            Array.Clear(m_PoseParameters, 0, m_PoseParameters.Length);
            Array.Clear(
                m_PoseParameterAvailability,
                0,
                m_PoseParameterAvailability.Length);
            m_LastPresentationFrame = 0;
            m_PreviousPresentationFrame = 0;
            m_FrameActive = false;
        }

        void IAnimationReadOnlyBufferLease.RequireValid(ulong leaseIdentity)
        {
            if (leaseIdentity == 0 || leaseIdentity != m_LeaseIdentity)
                throw new InvalidOperationException(
                    "Action presentation sample buffer lease is stale.");
        }
    }
}
