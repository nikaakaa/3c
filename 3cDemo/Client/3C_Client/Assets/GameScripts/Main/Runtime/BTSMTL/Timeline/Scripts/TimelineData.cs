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
        List<TimelineSection> m_Sections = new List<TimelineSection>();

        [SerializeField]
        float m_Scale = 1f;

        [NonSerialized]
        UnityEngine.Object m_SerializedOwner;

        [NonSerialized]
        string m_SerializedPropertyPath;

        public string Name { get => string.IsNullOrEmpty(m_Name) ? "Timeline" : m_Name; set => m_Name = value; }
        public string AuthoringId => m_AuthoringId ?? string.Empty;
        public List<Track> Tracks => m_Tracks;
        public IReadOnlyList<TimelineSection> Sections => m_Sections;
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
            for (int i = 0; i < m_Sections.Count; i++)
                changed |= m_Sections[i]?.EnsureAuthoringIdentity() ?? false;
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
            for (int i = 0; i < m_Sections.Count; i++)
                m_Sections[i]?.RegenerateAuthoringIdentity();
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
            var sectionNames = new HashSet<string>(StringComparer.Ordinal);
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
            for (int i = 0; i < m_Sections.Count; i++)
            {
                TimelineSection section = m_Sections[i];
                if (section == null || !section.RequireValid() || !identities.Add(section.AuthoringId) ||
                    !sectionNames.Add(section.Name))
                {
                    errors?.Add($"Timeline '{Name}' section #{i} is invalid or duplicated.");
                    valid = false;
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

    [Serializable]
    public sealed class TimelineSection
    {
        [SerializeField]
        string m_AuthoringId;

        [SerializeField]
        string m_Name;

        [SerializeField]
        int m_Frame;

        public string AuthoringId => m_AuthoringId ?? string.Empty;
        public string Name => m_Name ?? string.Empty;
        public int Frame => m_Frame;

#if UNITY_EDITOR
        public static TimelineSection Create(string name, int frame)
        {
            return Create(AuthoringIdentity.Create(), name, frame);
        }

        public static TimelineSection Create(string authoringId, string name, int frame)
        {
            if (!AuthoringIdentity.IsValid(authoringId))
                throw new ArgumentException("Timeline Section authoring identity is invalid.", nameof(authoringId));
            var section = new TimelineSection
            {
                m_AuthoringId = authoringId
            };
            section.Configure(name, frame);
            return section;
        }

        public void Configure(string name, int frame)
        {
            string value = name?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(value) || frame < 0)
                throw new ArgumentException("Timeline Section requires a name and non-negative frame.");
            m_Name = value;
            m_Frame = frame;
        }

        public bool EnsureAuthoringIdentity()
        {
            if (AuthoringIdentity.IsValid(m_AuthoringId))
                return false;
            m_AuthoringId = AuthoringIdentity.Create();
            return true;
        }

        public void RegenerateAuthoringIdentity()
        {
            m_AuthoringId = AuthoringIdentity.Create();
        }
#endif

        public bool RequireValid() =>
            AuthoringIdentity.IsValid(AuthoringId) &&
            !string.IsNullOrWhiteSpace(Name) &&
            string.Equals(Name, Name.Trim(), StringComparison.Ordinal) &&
            Frame >= 0;
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
