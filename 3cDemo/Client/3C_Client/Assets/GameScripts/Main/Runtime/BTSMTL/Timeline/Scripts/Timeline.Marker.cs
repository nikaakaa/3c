using System;
using BTSMTL.Diagnostics;
using UnityEngine;

namespace BTSMTL.Timeline
{
    [Serializable]
    public sealed class TimelineMarker
    {
        [SerializeField]
        string m_AuthoringId;

        [SerializeField]
        int m_Frame;

        [SerializeField]
        ScriptableObject m_Graph;

        Track m_Track;

        public string AuthoringId => m_AuthoringId ?? string.Empty;
        public int Frame => m_Frame;
        public ScriptableObject Graph => m_Graph;
        public Track Track => m_Track;
        public TimelineExecutionDomain ExecutionDomain =>
            m_Track != null ? m_Track.ExecutionDomain : TimelineExecutionDomains.Normalize(default);

        public void Init(Track track)
        {
            m_Track = track;
            if (m_Frame < 0)
                throw new InvalidOperationException($"Timeline Marker '{AuthoringId}' frame is invalid.");
        }

        public void Configure(int frame, ScriptableObject graph)
        {
            if (frame < 0)
                throw new ArgumentOutOfRangeException(nameof(frame));
            if (graph is not ITimelineTreeGraphAsset markerGraph || !markerGraph.IsTimelineTrigger)
                throw new ArgumentException("Timeline Marker需要正式的Timeline触发图资产。", nameof(graph));
            m_Frame = frame;
            m_Graph = graph;
        }

#if UNITY_EDITOR
        public void ConfigureAuthoringIdentity(string authoringId)
        {
            if (!AuthoringIdentity.IsValid(authoringId))
                throw new ArgumentException("Timeline Marker authoring identity is invalid.", nameof(authoringId));
            m_AuthoringId = authoringId;
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
    }
}
