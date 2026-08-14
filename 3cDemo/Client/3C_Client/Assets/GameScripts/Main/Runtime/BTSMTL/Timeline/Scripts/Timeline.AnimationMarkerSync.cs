using System;
using BTSMTL.Diagnostics;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public enum AnimationSyncMode
    {
        Unspecified = 0,
        None = 1,
        MarkerGroup = 2
    }

    public enum AnimationSyncTimeMapping
    {
        Unspecified = 0,
        MarkerSegmentFraction = 1,
        GeneratedFootPhase = 2
    }

    public enum AnimationMarkerSequenceTopology
    {
        Unspecified = 0,
        Finite = 1,
        Cyclic = 2
    }

    public enum AnimationMarkerSyncRole
    {
        Unspecified = 0,
        CanBeLeader = 1,
        AlwaysLeader = 2,
        AlwaysFollower = 3
    }

    [Serializable]
    public sealed class AnimationSyncMarker
    {
        [SerializeField] string m_AuthoringId;
        [SerializeField] string m_MarkerId;
        [SerializeField] int m_Frame;

        public string AuthoringId => m_AuthoringId ?? string.Empty;
        public string MarkerId => m_MarkerId ?? string.Empty;
        public int Frame => m_Frame;

        internal AnimationSyncMarker(string authoringId, string markerId, int frame)
        {
            m_AuthoringId = authoringId ?? string.Empty;
            m_MarkerId = markerId ?? string.Empty;
            m_Frame = frame;
        }

#if UNITY_EDITOR
        internal bool EnsureAuthoringIdentity()
        {
            if (AuthoringIdentity.IsValid(m_AuthoringId))
                return false;
            m_AuthoringId = AuthoringIdentity.Create();
            return true;
        }

        internal void RegenerateAuthoringIdentity() => m_AuthoringId = AuthoringIdentity.Create();
        internal void Rename(string markerId) => m_MarkerId = markerId;
        internal void Move(int frame) => m_Frame = frame;
#endif
    }

    public static class AnimationMarkerSyncAuthoring
    {
        public static string NormalizeId(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        public static string PairKey(string previousMarkerId, string nextMarkerId) =>
            $"{NormalizeId(previousMarkerId)}\u001f{NormalizeId(nextMarkerId)}";
    }

    public readonly struct AnimationMarkerSyncCallSite
    {
        public AnimationMarkerSyncCallSite(string authoringIdentity, TimelinePlaybackMode playbackMode)
        {
            AuthoringIdentity = authoringIdentity ?? string.Empty;
            PlaybackMode = playbackMode;
        }

        public string AuthoringIdentity { get; }
        public TimelinePlaybackMode PlaybackMode { get; }
    }
}
