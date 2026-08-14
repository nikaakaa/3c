using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public interface IAnimationSequenceAnalysisReference
    {
        UnityEngine.Object AnalysisSource { get; }
        string AnalysisIdentity { get; }
    }

    public enum AnimationSequenceCurveValueDomain : byte
    {
        Normalized01 = 1,
        SignedNormalized = 2,
        Unbounded = 3
    }

    public enum AnimationSequenceNotifyKind : byte
    {
        FootstepAudio = 1,
        VisualEffect = 2,
        EditorAnnotation = 3
    }

    [Serializable]
    public abstract class AnimationSequenceNotifyPayload
    {
        public abstract void RequireValid();
    }

    [Serializable]
    public sealed class AnimationSequenceFootstepAudioPayload : AnimationSequenceNotifyPayload
    {
        [SerializeField] string m_CueId = string.Empty;
        [SerializeField] string m_FootId = string.Empty;

        public string CueId => m_CueId ?? string.Empty;
        public string FootId => m_FootId ?? string.Empty;

        public AnimationSequenceFootstepAudioPayload()
        {
        }

        public AnimationSequenceFootstepAudioPayload(string cueId, string footId)
        {
            m_CueId = cueId?.Trim() ?? string.Empty;
            m_FootId = footId?.Trim() ?? string.Empty;
            RequireValid();
        }

        public override void RequireValid()
        {
            if (string.IsNullOrWhiteSpace(CueId) || string.IsNullOrWhiteSpace(FootId))
                throw new InvalidOperationException("Footstep Audio Notify payload is incomplete.");
        }
    }

    [Serializable]
    public sealed class AnimationSequenceVisualEffectPayload : AnimationSequenceNotifyPayload
    {
        [SerializeField] string m_EffectId = string.Empty;
        [SerializeField] string m_SocketId = string.Empty;

        public string EffectId => m_EffectId ?? string.Empty;
        public string SocketId => m_SocketId ?? string.Empty;

        public AnimationSequenceVisualEffectPayload()
        {
        }

        public AnimationSequenceVisualEffectPayload(string effectId, string socketId)
        {
            m_EffectId = effectId?.Trim() ?? string.Empty;
            m_SocketId = socketId?.Trim() ?? string.Empty;
            RequireValid();
        }

        public override void RequireValid()
        {
            if (string.IsNullOrWhiteSpace(EffectId) || string.IsNullOrWhiteSpace(SocketId))
                throw new InvalidOperationException("Visual Effect Notify payload is incomplete.");
        }
    }

    [Serializable]
    public sealed class AnimationSequenceEditorAnnotationPayload : AnimationSequenceNotifyPayload
    {
        [SerializeField] string m_Text = string.Empty;

        public string Text => m_Text ?? string.Empty;

        public AnimationSequenceEditorAnnotationPayload()
        {
        }

        public AnimationSequenceEditorAnnotationPayload(string text)
        {
            m_Text = text?.Trim() ?? string.Empty;
            RequireValid();
        }

        public override void RequireValid()
        {
            if (string.IsNullOrWhiteSpace(Text))
                throw new InvalidOperationException("Editor Annotation Notify payload is incomplete.");
        }
    }

    [Serializable]
    public sealed class AnimationSequenceNotify
    {
        [SerializeField] string m_AuthoringId = string.Empty;
        [SerializeField] AnimationSequenceNotifyKind m_Kind;
        [SerializeField] int m_Frame;
        [SerializeReference] AnimationSequenceNotifyPayload m_Payload;

        public string AuthoringId => m_AuthoringId ?? string.Empty;
        public AnimationSequenceNotifyKind Kind => m_Kind;
        public int Frame => m_Frame;
        public AnimationSequenceNotifyPayload Payload => m_Payload;

        internal AnimationSequenceNotify(
            string authoringId,
            AnimationSequenceNotifyKind kind,
            int frame,
            AnimationSequenceNotifyPayload payload)
        {
            m_AuthoringId = authoringId ?? string.Empty;
            m_Kind = kind;
            m_Frame = frame;
            m_Payload = payload;
            RequireValid();
        }

        internal void Move(int frame) => m_Frame = frame;

        internal void Configure(AnimationSequenceNotifyKind kind, AnimationSequenceNotifyPayload payload)
        {
            m_Kind = kind;
            m_Payload = payload;
            RequireValid();
        }

        internal void RequireValid()
        {
            if (!AuthoringIdentity.IsValid(AuthoringId) ||
                !Enum.IsDefined(typeof(AnimationSequenceNotifyKind), Kind) ||
                Frame < 0 || Payload == null || !PayloadMatchesKind(Kind, Payload))
                throw new InvalidOperationException("Animation Sequence Notify is invalid.");
            Payload.RequireValid();
        }

        static bool PayloadMatchesKind(AnimationSequenceNotifyKind kind, AnimationSequenceNotifyPayload payload) =>
            kind switch
            {
                AnimationSequenceNotifyKind.FootstepAudio => payload is AnimationSequenceFootstepAudioPayload,
                AnimationSequenceNotifyKind.VisualEffect => payload is AnimationSequenceVisualEffectPayload,
                AnimationSequenceNotifyKind.EditorAnnotation => payload is AnimationSequenceEditorAnnotationPayload,
                _ => false
            };
    }

    [Serializable]
    public sealed class AnimationSequenceCurveChannel
    {
        [SerializeField] string m_ChannelId = string.Empty;
        [SerializeField] AnimationSequenceCurveValueDomain m_ValueDomain;
        [SerializeField] AnimationCurve m_Curve = AnimationCurve.Linear(0f, 0f, 1f, 0f);

        public string ChannelId => m_ChannelId ?? string.Empty;
        public AnimationSequenceCurveValueDomain ValueDomain => m_ValueDomain;
        public AnimationCurve Curve => CopyCurve(m_Curve);

        internal AnimationSequenceCurveChannel(
            string channelId,
            AnimationSequenceCurveValueDomain valueDomain,
            AnimationCurve curve)
        {
            m_ChannelId = channelId?.Trim() ?? string.Empty;
            m_ValueDomain = valueDomain;
            m_Curve = CopyCurve(curve);
            RequireValid();
        }

        internal void Set(AnimationSequenceCurveValueDomain valueDomain, AnimationCurve curve)
        {
            m_ValueDomain = valueDomain;
            m_Curve = CopyCurve(curve);
            RequireValid();
        }

        internal void RequireValid()
        {
            if (string.IsNullOrWhiteSpace(ChannelId) ||
                !string.Equals(ChannelId, ChannelId.Trim(), StringComparison.Ordinal) ||
                !Enum.IsDefined(typeof(AnimationSequenceCurveValueDomain), ValueDomain) ||
                m_Curve == null || m_Curve.length == 0)
                throw new InvalidOperationException("Animation Sequence Curve Channel is invalid.");
            Keyframe[] keys = m_Curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                if (!float.IsFinite(key.time) || !float.IsFinite(key.value) ||
                    key.time < 0f || key.time > 1f ||
                    i > 0 && key.time <= keys[i - 1].time ||
                    ValueDomain == AnimationSequenceCurveValueDomain.Normalized01 &&
                    (key.value < 0f || key.value > 1f) ||
                    ValueDomain == AnimationSequenceCurveValueDomain.SignedNormalized &&
                    (key.value < -1f || key.value > 1f))
                    throw new InvalidOperationException($"Animation Sequence Curve '{ChannelId}' key #{i} is invalid.");
            }
        }

        static AnimationCurve CopyCurve(AnimationCurve source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }
    }

    public abstract class AnimationSequenceAsset : ScriptableObject
    {
        [SerializeField] string m_AuthoringId = string.Empty;
        [SerializeField] UnityEngine.AnimationClip m_Clip;
        [SerializeField] bool m_Loop;
        [SerializeField] float m_DefaultPlayRate = 1f;
        [SerializeField] AnimationSyncMode m_SyncMode = AnimationSyncMode.None;
        [SerializeField] AnimationSyncTimeMapping m_TimeMapping;
        [SerializeField] string m_SyncGroupId = string.Empty;
        [SerializeField] AnimationMarkerSequenceTopology m_SequenceTopology;
        [SerializeField] AnimationMarkerSyncRole m_SyncRole;
        [SerializeField] List<AnimationSyncMarker> m_SyncMarkers = new List<AnimationSyncMarker>();
        [SerializeField] List<AnimationSequenceCurveChannel> m_CurveChannels = new List<AnimationSequenceCurveChannel>();
        [SerializeField] List<AnimationSequenceNotify> m_Notifies = new List<AnimationSequenceNotify>();
        [SerializeField] string m_ContentRevision = string.Empty;

        public string AuthoringId => m_AuthoringId ?? string.Empty;
        public UnityEngine.AnimationClip Clip => m_Clip;
        public bool Loop => m_Loop;
        public float DefaultPlayRate => m_DefaultPlayRate;
        public AnimationSyncMode SyncMode => m_SyncMode;
        public AnimationSyncTimeMapping TimeMapping => m_TimeMapping;
        public string SyncGroupId => m_SyncGroupId ?? string.Empty;
        public AnimationMarkerSequenceTopology SequenceTopology => m_SequenceTopology;
        public AnimationMarkerSyncRole SyncRole => m_SyncRole;
        public IReadOnlyList<AnimationSyncMarker> SyncMarkers => m_SyncMarkers;
        public IReadOnlyList<AnimationSequenceCurveChannel> CurveChannels => m_CurveChannels;
        public IReadOnlyList<AnimationSequenceNotify> Notifies => m_Notifies;
        public string ContentRevision => m_ContentRevision ?? string.Empty;
        public int DurationFrame => Clip ? Mathf.Max(1, Mathf.RoundToInt(Clip.length * Clip.frameRate)) : 0;

        protected void ConfigureCore(
            string authoringId,
            UnityEngine.AnimationClip clip,
            bool loop,
            float defaultPlayRate)
        {
            if (!AuthoringIdentity.IsValid(authoringId) || !clip ||
                !float.IsFinite(defaultPlayRate) || defaultPlayRate <= 0f)
                throw new ArgumentException("Animation Sequence core authoring is incomplete.");
            m_AuthoringId = authoringId;
            m_Clip = clip;
            m_Loop = loop;
            m_DefaultPlayRate = defaultPlayRate;
            m_ContentRevision = Guid.NewGuid().ToString("N");
        }

        public void ConfigureNone()
        {
            m_SyncMode = AnimationSyncMode.None;
            m_TimeMapping = AnimationSyncTimeMapping.Unspecified;
            m_SyncGroupId = string.Empty;
            m_SequenceTopology = AnimationMarkerSequenceTopology.Unspecified;
            m_SyncRole = AnimationMarkerSyncRole.Unspecified;
            m_SyncMarkers.Clear();
            Touch();
        }

        public void ConfigureMarkerGroup(
            string syncGroupId,
            AnimationMarkerSequenceTopology topology,
            AnimationMarkerSyncRole syncRole,
            AnimationSyncTimeMapping timeMapping)
        {
            string canonicalGroupId = AnimationMarkerSyncAuthoring.NormalizeId(syncGroupId);
            if (string.IsNullOrEmpty(canonicalGroupId))
                throw new ArgumentException("SyncGroupId is required.", nameof(syncGroupId));
            if (topology != AnimationMarkerSequenceTopology.Finite &&
                topology != AnimationMarkerSequenceTopology.Cyclic)
                throw new ArgumentOutOfRangeException(nameof(topology));
            if (syncRole != AnimationMarkerSyncRole.CanBeLeader &&
                syncRole != AnimationMarkerSyncRole.AlwaysLeader &&
                syncRole != AnimationMarkerSyncRole.AlwaysFollower)
                throw new ArgumentOutOfRangeException(nameof(syncRole));
            if (timeMapping != AnimationSyncTimeMapping.MarkerSegmentFraction &&
                timeMapping != AnimationSyncTimeMapping.GeneratedFootPhase)
                throw new ArgumentOutOfRangeException(nameof(timeMapping));
            m_SyncMode = AnimationSyncMode.MarkerGroup;
            m_TimeMapping = timeMapping;
            m_SyncGroupId = canonicalGroupId;
            m_SequenceTopology = topology;
            m_SyncRole = syncRole;
            Touch();
        }

        public AnimationSyncMarker EnsureMarker(string authoringId, string markerId, int frame)
        {
            if (SyncMode != AnimationSyncMode.MarkerGroup || !AuthoringIdentity.IsValid(authoringId) ||
                string.IsNullOrWhiteSpace(markerId) || frame < 0)
                throw new ArgumentException("Animation Sequence Marker is invalid.");
            AnimationSyncMarker marker = FindMarker(authoringId);
            if (marker == null)
            {
                marker = new AnimationSyncMarker(authoringId, markerId.Trim(), frame);
                m_SyncMarkers.Add(marker);
            }
            else
            {
                marker.Rename(markerId.Trim());
                marker.Move(frame);
            }
            SortMarkers();
            Touch();
            return marker;
        }

        public void MoveMarker(string authoringId, int frame)
        {
            if (frame < 0)
                throw new ArgumentOutOfRangeException(nameof(frame));
            RequireMarker(authoringId).Move(frame);
            SortMarkers();
            Touch();
        }

        public void RenameMarker(string authoringId, string markerId)
        {
            if (string.IsNullOrWhiteSpace(markerId))
                throw new ArgumentException("Marker identity is invalid.", nameof(markerId));
            RequireMarker(authoringId).Rename(markerId.Trim());
            Touch();
        }

        public void DeleteMarker(string authoringId)
        {
            m_SyncMarkers.Remove(RequireMarker(authoringId));
            Touch();
        }

        public void SetCurve(
            string channelId,
            AnimationSequenceCurveValueDomain valueDomain,
            AnimationCurve curve)
        {
            AnimationSequenceCurveChannel channel = FindCurve(channelId);
            if (channel == null)
                m_CurveChannels.Add(new AnimationSequenceCurveChannel(channelId, valueDomain, curve));
            else
                channel.Set(valueDomain, curve);
            m_CurveChannels.Sort((left, right) => string.CompareOrdinal(left?.ChannelId, right?.ChannelId));
            Touch();
        }

        public bool TryGetCurve(string channelId, out AnimationCurve curve)
        {
            AnimationSequenceCurveChannel channel = FindCurve(channelId);
            curve = channel?.Curve;
            return channel != null;
        }

        public void DeleteCurve(string channelId)
        {
            AnimationSequenceCurveChannel channel = FindCurve(channelId) ??
                throw new KeyNotFoundException($"Animation Sequence Curve '{channelId}' was not found.");
            m_CurveChannels.Remove(channel);
            Touch();
        }

        public AnimationSequenceNotify EnsureNotify(
            string authoringId,
            AnimationSequenceNotifyKind kind,
            int frame,
            AnimationSequenceNotifyPayload payload)
        {
            AnimationSequenceNotify notify = FindNotify(authoringId);
            if (notify == null)
            {
                notify = new AnimationSequenceNotify(authoringId, kind, frame, payload);
                m_Notifies.Add(notify);
            }
            else
            {
                notify.Move(frame);
                notify.Configure(kind, payload);
            }
            SortNotifies();
            Touch();
            return notify;
        }

        public void MoveNotify(string authoringId, int frame)
        {
            if (frame < 0)
                throw new ArgumentOutOfRangeException(nameof(frame));
            RequireNotify(authoringId).Move(frame);
            SortNotifies();
            Touch();
        }

        public void DeleteNotify(string authoringId)
        {
            m_Notifies.Remove(RequireNotify(authoringId));
            Touch();
        }

        public virtual void RequireValid()
        {
            if (!AuthoringIdentity.IsValid(AuthoringId) || !Clip ||
                !float.IsFinite(Clip.length) || Clip.length <= 0f ||
                !float.IsFinite(Clip.frameRate) || Clip.frameRate <= 0f ||
                !float.IsFinite(DefaultPlayRate) || DefaultPlayRate <= 0f ||
                string.IsNullOrWhiteSpace(ContentRevision))
                throw new InvalidOperationException($"Animation Sequence '{name}' is incomplete.");
            RequireMarkerSync();
            RequireCurves();
            RequireNotifies();
        }

        void RequireMarkerSync()
        {
            if (SyncMode == AnimationSyncMode.None)
            {
                if (TimeMapping != AnimationSyncTimeMapping.Unspecified ||
                    !string.IsNullOrEmpty(SyncGroupId) ||
                    SequenceTopology != AnimationMarkerSequenceTopology.Unspecified ||
                    SyncRole != AnimationMarkerSyncRole.Unspecified || SyncMarkers.Count != 0)
                    throw new InvalidOperationException($"Animation Sequence '{name}' None sync retains marker data.");
                return;
            }
            if (SyncMode != AnimationSyncMode.MarkerGroup || string.IsNullOrWhiteSpace(SyncGroupId) ||
                SequenceTopology != (Loop ? AnimationMarkerSequenceTopology.Cyclic : AnimationMarkerSequenceTopology.Finite) ||
                SyncRole != AnimationMarkerSyncRole.CanBeLeader &&
                SyncRole != AnimationMarkerSyncRole.AlwaysLeader &&
                SyncRole != AnimationMarkerSyncRole.AlwaysFollower ||
                TimeMapping != AnimationSyncTimeMapping.MarkerSegmentFraction &&
                TimeMapping != AnimationSyncTimeMapping.GeneratedFootPhase || SyncMarkers.Count < 2)
                throw new InvalidOperationException($"Animation Sequence '{name}' Marker Group is incomplete.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < SyncMarkers.Count; i++)
            {
                AnimationSyncMarker marker = SyncMarkers[i];
                if (marker == null || !AuthoringIdentity.IsValid(marker.AuthoringId) ||
                    !ids.Add(marker.AuthoringId) || string.IsNullOrWhiteSpace(marker.MarkerId) ||
                    marker.Frame < 0 || Loop && marker.Frame >= DurationFrame ||
                    !Loop && marker.Frame > DurationFrame ||
                    i > 0 && marker.Frame <= SyncMarkers[i - 1].Frame)
                    throw new InvalidOperationException($"Animation Sequence '{name}' Marker #{i} is invalid.");
            }
            if (!Loop && (SyncMarkers[0].Frame != 0 || SyncMarkers[SyncMarkers.Count - 1].Frame != DurationFrame))
                throw new InvalidOperationException($"Animation Sequence '{name}' Finite Marker coverage is incomplete.");
        }

        void RequireCurves()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < CurveChannels.Count; i++)
            {
                AnimationSequenceCurveChannel channel = CurveChannels[i];
                channel?.RequireValid();
                if (channel == null || !ids.Add(channel.ChannelId))
                    throw new InvalidOperationException($"Animation Sequence '{name}' Curve Channel #{i} is invalid or duplicated.");
            }
        }

        void RequireNotifies()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < Notifies.Count; i++)
            {
                AnimationSequenceNotify notify = Notifies[i];
                notify?.RequireValid();
                if (notify == null || !ids.Add(notify.AuthoringId) || notify.Frame > DurationFrame)
                    throw new InvalidOperationException($"Animation Sequence '{name}' Notify #{i} is invalid or duplicated.");
            }
        }

        AnimationSyncMarker FindMarker(string authoringId)
        {
            for (int i = 0; i < m_SyncMarkers.Count; i++)
                if (m_SyncMarkers[i] != null && string.Equals(m_SyncMarkers[i].AuthoringId, authoringId, StringComparison.Ordinal))
                    return m_SyncMarkers[i];
            return null;
        }

        AnimationSyncMarker RequireMarker(string authoringId) =>
            FindMarker(authoringId) ?? throw new KeyNotFoundException($"Animation Sequence Marker '{authoringId}' was not found.");

        AnimationSequenceCurveChannel FindCurve(string channelId)
        {
            string id = channelId?.Trim() ?? string.Empty;
            for (int i = 0; i < m_CurveChannels.Count; i++)
                if (m_CurveChannels[i] != null && string.Equals(m_CurveChannels[i].ChannelId, id, StringComparison.Ordinal))
                    return m_CurveChannels[i];
            return null;
        }

        AnimationSequenceNotify FindNotify(string authoringId)
        {
            for (int i = 0; i < m_Notifies.Count; i++)
                if (m_Notifies[i] != null && string.Equals(m_Notifies[i].AuthoringId, authoringId, StringComparison.Ordinal))
                    return m_Notifies[i];
            return null;
        }

        AnimationSequenceNotify RequireNotify(string authoringId) =>
            FindNotify(authoringId) ?? throw new KeyNotFoundException($"Animation Sequence Notify '{authoringId}' was not found.");

        void SortMarkers() => m_SyncMarkers.Sort((left, right) =>
        {
            int frame = (left?.Frame ?? int.MaxValue).CompareTo(right?.Frame ?? int.MaxValue);
            return frame != 0 ? frame : string.CompareOrdinal(left?.AuthoringId, right?.AuthoringId);
        });

        void SortNotifies() => m_Notifies.Sort((left, right) =>
        {
            int frame = (left?.Frame ?? int.MaxValue).CompareTo(right?.Frame ?? int.MaxValue);
            return frame != 0 ? frame : string.CompareOrdinal(left?.AuthoringId, right?.AuthoringId);
        });

        protected void Touch() => m_ContentRevision = Guid.NewGuid().ToString("N");

#if UNITY_EDITOR
        public void ApplyModify(Action mutation, string undoName)
        {
            UnityEditor.Undo.RegisterCompleteObjectUndo(this, $"Animation Sequence: {undoName}");
            mutation?.Invoke();
            RequireValid();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        protected virtual void OnValidate()
        {
            if (!AuthoringIdentity.IsValid(m_AuthoringId))
                m_AuthoringId = AuthoringIdentity.Create();
            if (string.IsNullOrWhiteSpace(m_ContentRevision))
                m_ContentRevision = Guid.NewGuid().ToString("N");
        }
#endif
    }
}
