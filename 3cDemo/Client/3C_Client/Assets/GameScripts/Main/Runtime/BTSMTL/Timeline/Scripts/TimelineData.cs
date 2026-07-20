using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using UnityEngine;

namespace BTSMTL.Timeline
{
    [Serializable]
    [AcceptableTrackGroups("Base")]
    public sealed partial class TimelineData
    {
        [SerializeField]
        string m_AuthoringId;

        [SerializeField]
        string m_Name = "Timeline";

        [SerializeReference]
        List<Track> m_Tracks = new List<Track>();

        [SerializeField]
        float m_Scale = 1f;

        [NonSerialized]
        UnityEngine.Object m_SerializedOwner;

        [NonSerialized]
        string m_SerializedPropertyPath;

        public string Name { get => string.IsNullOrEmpty(m_Name) ? "Timeline" : m_Name; set => m_Name = value; }
        public string AuthoringId => m_AuthoringId ?? string.Empty;
        public List<Track> Tracks => m_Tracks;
        public float Scale { get => m_Scale; set => m_Scale = value; }
        public UnityEngine.Object SerializedOwner => m_SerializedOwner;
        public string SerializedPropertyPath => m_SerializedPropertyPath ?? string.Empty;

        public static TimelineData CreateDefault(string name)
        {
            return new TimelineData
            {
                m_AuthoringId = AuthoringIdentity.Create(),
                Name = string.IsNullOrEmpty(name) ? "Timeline" : name
            };
        }

        public TimelineData Clone()
        {
            TimelineData clone = ManagedReferenceCloneUtility.Clone(this);
            clone.m_SerializedOwner = null;
            clone.m_SerializedPropertyPath = string.Empty;
            return clone;
        }

#if UNITY_EDITOR
        public TimelineData CloneForAuthoring()
        {
            TimelineData clone = Clone();
            clone.RegenerateAuthoringIdentities();
            return clone;
        }

        public bool EnsureAuthoringIdentities()
        {
            bool changed = false;
            if (!AuthoringIdentity.IsValid(m_AuthoringId))
            {
                m_AuthoringId = AuthoringIdentity.Create();
                changed = true;
            }
            for (int i = 0; i < m_Tracks.Count; i++)
            {
                Track track = m_Tracks[i];
                if (track == null)
                    continue;
                changed |= track.EnsureAuthoringIdentity();
                if (track is ITimelineTrackOwnedAuthoringIdentity identityOwner)
                    changed |= identityOwner.EnsureOwnedAuthoringIdentities();
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                    changed |= track.Clips[clipIndex]?.EnsureAuthoringIdentity() ?? false;
            }
            return changed;
        }

        public void RegenerateAuthoringIdentities()
        {
            m_AuthoringId = AuthoringIdentity.Create();
            for (int i = 0; i < m_Tracks.Count; i++)
            {
                Track track = m_Tracks[i];
                if (track == null)
                    continue;
                track.RegenerateAuthoringIdentity();
                if (track is ITimelineTrackOwnedAuthoringIdentity identityOwner)
                    identityOwner.RegenerateOwnedAuthoringIdentities();
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    clip?.RegenerateAuthoringIdentity();
                    if (clip is ITimelineOwnedAuthoringIdentity owner)
                        owner.RegenerateOwnedAuthoringIdentity();
                }
            }
        }
#endif

        public bool ValidateAuthoringIdentities(List<string> errors)
        {
            bool valid = true;
            if (!AuthoringIdentity.IsValid(m_AuthoringId))
            {
                errors?.Add($"Timeline '{Name}' has an invalid authoring identity.");
                valid = false;
            }
            var identities = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_Tracks.Count; i++)
            {
                Track track = m_Tracks[i];
                if (track == null || !AuthoringIdentity.IsValid(track.AuthoringId) || !identities.Add(track.AuthoringId))
                {
                    errors?.Add($"Timeline '{Name}' track #{i} has a missing or duplicate authoring identity.");
                    valid = false;
                    continue;
                }
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip == null || !AuthoringIdentity.IsValid(clip.AuthoringId) || !identities.Add(clip.AuthoringId))
                    {
                        errors?.Add($"Timeline '{Name}' clip #{i}:{clipIndex} has a missing or duplicate authoring identity.");
                        valid = false;
                    }
                }
            }
            return valid;
        }

        public void BindSerializedOwner(UnityEngine.Object owner, string propertyPath)
        {
            m_SerializedOwner = owner;
            m_SerializedPropertyPath = propertyPath ?? string.Empty;
        }

        public string GetSerializedPropertyPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return SerializedPropertyPath;
            return string.IsNullOrEmpty(SerializedPropertyPath) ? relativePath : $"{SerializedPropertyPath}.{relativePath}";
        }

    }

    public interface ITimelineOwnedAuthoringIdentity
    {
#if UNITY_EDITOR
        void RegenerateOwnedAuthoringIdentity();
#endif
    }

    public interface ITimelineTrackOwnedAuthoringIdentity
    {
#if UNITY_EDITOR
        bool EnsureOwnedAuthoringIdentities();
        void RegenerateOwnedAuthoringIdentities();
#endif
    }
}
