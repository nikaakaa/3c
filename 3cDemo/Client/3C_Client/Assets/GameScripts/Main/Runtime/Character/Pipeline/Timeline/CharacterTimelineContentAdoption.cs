using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    public enum CharacterTimelineContentAdoptionState : byte
    {
        None = 0,
        Exported = 1,
        Prepared = 2,
        Published = 3,
        Adopted = 4,
        Rejected = 5,
        Failed = 6
    }

    public readonly struct CharacterTimelineContentAdoptionReport
    {
        public CharacterTimelineContentAdoptionReport(
            CharacterTimelineContentAdoptionState state,
            string authoringRevision,
            string contentRevision,
            string message)
        {
            State = state;
            AuthoringRevision = authoringRevision ?? string.Empty;
            ContentRevision = contentRevision ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public CharacterTimelineContentAdoptionState State { get; }
        public string AuthoringRevision { get; }
        public string ContentRevision { get; }
        public string Message { get; }
        public bool Succeeded => State == CharacterTimelineContentAdoptionState.Exported ||
                                 State == CharacterTimelineContentAdoptionState.Prepared ||
                                 State == CharacterTimelineContentAdoptionState.Published ||
                                 State == CharacterTimelineContentAdoptionState.Adopted;
    }

    public sealed class CharacterTimelineContentSnapshot
    {
        readonly TimelineData m_Data;

        internal CharacterTimelineContentSnapshot(TimelineData data, TimelineContentUnit content)
        {
            m_Data = data;
            Content = content;
        }

        public string TimelineAuthoringId => m_Data.AuthoringId;
        public string TimelineName => m_Data.Name;
        internal TimelineContentUnit Content { get; }

        internal TimelineData CloneData() => m_Data.Clone();
    }

    public sealed class CharacterTimelineContentExport
    {
        readonly IReadOnlyList<CharacterTimelineContentSnapshot> m_Snapshots;

        internal CharacterTimelineContentExport(
            IReadOnlyList<CharacterTimelineContentSnapshot> snapshots,
            string authoringRevision,
            string contentRevision)
        {
            m_Snapshots = new ReadOnlyCollection<CharacterTimelineContentSnapshot>(
                new List<CharacterTimelineContentSnapshot>(snapshots ?? Array.Empty<CharacterTimelineContentSnapshot>()));
            AuthoringRevision = authoringRevision ?? string.Empty;
            ContentRevision = contentRevision ?? string.Empty;
        }

        public IReadOnlyList<CharacterTimelineContentSnapshot> Snapshots => m_Snapshots;
        public string AuthoringRevision { get; }
        public string ContentRevision { get; }
        public bool IsValid => m_Snapshots.Count != 0 &&
                               !string.IsNullOrEmpty(AuthoringRevision) &&
                               !string.IsNullOrEmpty(ContentRevision);
    }

    public sealed class CharacterTimelineContentAdoptionPlan
    {
        internal CharacterTimelineContentAdoptionPlan(
            CharacterTimelineContentExport export,
            Guid sessionIdentity,
            ulong sessionContentGeneration,
            bool compatible,
            string message)
        {
            Export = export;
            SessionIdentity = sessionIdentity;
            SessionContentGeneration = sessionContentGeneration;
            IsCompatible = compatible;
            Message = message ?? string.Empty;
        }

        public CharacterTimelineContentExport Export { get; }
        public IReadOnlyList<CharacterTimelineContentSnapshot> Snapshots => Export?.Snapshots ?? Array.Empty<CharacterTimelineContentSnapshot>();
        public string AuthoringRevision => Export?.AuthoringRevision ?? string.Empty;
        public string ContentRevision => Export?.ContentRevision ?? string.Empty;
        public Guid SessionIdentity { get; }
        public ulong SessionContentGeneration { get; }
        public bool IsCompatible { get; }
        public string Message { get; }
        public bool IsValid => Export != null && Export.IsValid &&
                               SessionIdentity != Guid.Empty &&
                               SessionContentGeneration != 0;
    }

    public sealed class CharacterTimelineContentPublication
    {
        internal CharacterTimelineContentPublication(
            CharacterTimelineContentAdoptionPlan plan,
            string message)
        {
            Plan = plan;
            AuthoringRevision = plan?.AuthoringRevision ?? string.Empty;
            ContentRevision = plan?.ContentRevision ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public CharacterTimelineContentAdoptionPlan Plan { get; }
        public string AuthoringRevision { get; }
        public string ContentRevision { get; }
        public string Message { get; }
        public bool IsValid => Plan != null && Plan.IsValid &&
                               !string.IsNullOrEmpty(AuthoringRevision) &&
                               !string.IsNullOrEmpty(ContentRevision);
    }
}
