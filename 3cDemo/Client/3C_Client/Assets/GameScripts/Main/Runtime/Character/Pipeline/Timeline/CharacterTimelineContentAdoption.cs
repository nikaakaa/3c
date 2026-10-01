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
                new List<CharacterTimelineContentSnapshot>(snapshots));
            AuthoringRevision = authoringRevision;
            ContentRevision = contentRevision;
        }

        public IReadOnlyList<CharacterTimelineContentSnapshot> Snapshots => m_Snapshots;
        public string AuthoringRevision { get; }
        public string ContentRevision { get; }
    }

    public sealed class CharacterTimelineContentAdoptionPlan
    {
        internal CharacterTimelineContentAdoptionPlan(
            CharacterTimelineContentExport export,
            Guid sessionIdentity,
            ulong sessionContentGeneration,
            string message)
        {
            Export = export;
            SessionIdentity = sessionIdentity;
            SessionContentGeneration = sessionContentGeneration;
            Message = message;
        }

        public CharacterTimelineContentExport Export { get; }
        public IReadOnlyList<CharacterTimelineContentSnapshot> Snapshots => Export.Snapshots;
        public string AuthoringRevision => Export.AuthoringRevision;
        public string ContentRevision => Export.ContentRevision;
        public Guid SessionIdentity { get; }
        public ulong SessionContentGeneration { get; }
        public string Message { get; }
    }

    public sealed class CharacterTimelineContentPublication
    {
        internal CharacterTimelineContentPublication(
            CharacterTimelineContentAdoptionPlan plan,
            string message)
        {
            Plan = plan;
            AuthoringRevision = plan.AuthoringRevision;
            ContentRevision = plan.ContentRevision;
            Message = message;
        }

        public CharacterTimelineContentAdoptionPlan Plan { get; }
        public string AuthoringRevision { get; }
        public string ContentRevision { get; }
        public string Message { get; }
    }
}
