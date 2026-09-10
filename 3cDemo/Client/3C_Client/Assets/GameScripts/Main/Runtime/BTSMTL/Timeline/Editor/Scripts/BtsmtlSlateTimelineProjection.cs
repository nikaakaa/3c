#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using Slate;
using UnityEditor;
using UnityEngine;

namespace BTSMTL.Timeline.Editor
{
    [AddComponentMenu("")]
    sealed class BtsmtlSlateGroup : CutsceneGroup
    {
        [SerializeField] string m_Name = "Timeline";
        [SerializeField] GameObject m_Actor;
        [SerializeField] ActorReferenceMode m_ReferenceMode = ActorReferenceMode.UseOriginal;
        [SerializeField] ActorInitialTransformation m_InitialTransformation = ActorInitialTransformation.UseOriginal;

        public override string name
        {
            get => m_Name;
            set => m_Name = value ?? string.Empty;
        }

        public override GameObject actor
        {
            get => m_Actor;
            set => m_Actor = value;
        }

        public override ActorReferenceMode referenceMode
        {
            get => m_ReferenceMode;
            set => m_ReferenceMode = value;
        }

        public override ActorInitialTransformation initialTransformation
        {
            get => m_InitialTransformation;
            set => m_InitialTransformation = value;
        }

        public override Vector3 initialLocalPosition { get; set; }
        public override Vector3 initialLocalRotation { get; set; }
        public override bool displayVirtualMeshGizmo { get; set; }
    }

    [AddComponentMenu("")]
    sealed class BtsmtlSlateTrack : CutsceneTrack
    {
        [SerializeField] string m_SourceAuthoringId;
        bool m_RuntimeActive = true;

        public string SourceAuthoringId => m_SourceAuthoringId ?? string.Empty;

        public void Configure(string sourceAuthoringId)
        {
            m_SourceAuthoringId = sourceAuthoringId ?? string.Empty;
        }

        public override bool isActive
        {
            get => base.isActive && m_RuntimeActive;
            set => base.isActive = value;
        }

        public void SetRuntimeActive(bool active)
        {
            m_RuntimeActive = active;
        }
    }

    [AddComponentMenu("")]
    sealed class BtsmtlSlateActionClip : ActionClip
    {
        [SerializeField] string m_DisplayName = "Clip";
        [SerializeField] float m_Length = 1f;
        [SerializeField] string m_SourceAuthoringId;
        string m_RuntimeStatus = string.Empty;

        public override float length
        {
            get => Mathf.Max(0f, m_Length);
            set => m_Length = Mathf.Max(0f, value);
        }

        public override string info => string.IsNullOrEmpty(m_RuntimeStatus)
            ? m_DisplayName
            : $"{m_DisplayName} [{m_RuntimeStatus}]";

        public string SourceAuthoringId => m_SourceAuthoringId ?? string.Empty;

        public void Configure(string displayName, float duration)
        {
            m_DisplayName = string.IsNullOrEmpty(displayName) ? "Clip" : displayName;
            length = duration;
            name = m_DisplayName;
        }

        public void ConfigureSource(string sourceAuthoringId)
        {
            m_SourceAuthoringId = sourceAuthoringId ?? string.Empty;
        }

        public void SetRuntimeStatus(string status)
        {
            m_RuntimeStatus = status ?? string.Empty;
        }
    }

    public sealed class BtsmtlSlateTimelineProjection : IDisposable
    {
        readonly TimelineEditorOpenRequest m_Request;
        readonly TimelineEditorSessionContext m_Session;
        readonly Dictionary<string, Clip> m_SourceClips = new Dictionary<string, Clip>(StringComparer.Ordinal);
        readonly Dictionary<string, BtsmtlSlateActionClip> m_ProxyClips =
            new Dictionary<string, BtsmtlSlateActionClip>(StringComparer.Ordinal);
        readonly Dictionary<string, Track> m_SourceTracks = new Dictionary<string, Track>(StringComparer.Ordinal);
        readonly Dictionary<string, BtsmtlSlateTrack> m_ProxyTracks =
            new Dictionary<string, BtsmtlSlateTrack>(StringComparer.Ordinal);
        readonly Dictionary<string, TimelineSection> m_SourceSections =
            new Dictionary<string, TimelineSection>(StringComparer.Ordinal);
        readonly Dictionary<string, Slate.Section> m_ProxySections =
            new Dictionary<string, Slate.Section>(StringComparer.Ordinal);
        ProjectionSnapshot m_BeginSnapshot;
        readonly Func<Cutscene, bool> m_UndoPolicy;
        GameObject m_Host;
        Cutscene m_Cutscene;
        bool m_Disposed;
        bool m_RebuildQueued;
        bool m_EditorClosed;
        bool m_ReadOnly;

        static BtsmtlSlateTimelineProjection s_Current;

        readonly struct ProxyClipSnapshot
        {
            public ProxyClipSnapshot(float startTime, float endTime, string trackAuthoringId)
            {
                StartTime = startTime;
                EndTime = endTime;
                TrackAuthoringId = trackAuthoringId ?? string.Empty;
            }

            public float StartTime { get; }
            public float EndTime { get; }
            public string TrackAuthoringId { get; }
        }

        readonly struct ProxySectionSnapshot
        {
            public ProxySectionSnapshot(string name, float time)
            {
                Name = name ?? string.Empty;
                Time = time;
            }

            public string Name { get; }
            public float Time { get; }
        }

        sealed class ProjectionSnapshot
        {
            public readonly Dictionary<string, ProxyClipSnapshot> Clips =
                new Dictionary<string, ProxyClipSnapshot>(StringComparer.Ordinal);
            public readonly Dictionary<string, ProxySectionSnapshot> Sections =
                new Dictionary<string, ProxySectionSnapshot>(StringComparer.Ordinal);
            public readonly HashSet<string> Tracks = new HashSet<string>(StringComparer.Ordinal);
            public bool Unsupported;
        }

        BtsmtlSlateTimelineProjection(TimelineEditorOpenRequest request)
        {
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
            m_Session = new TimelineEditorSessionContext(request);
            m_UndoPolicy = ShouldRecordUndo;
            BuildProjection();
            CutsceneEditor.OnEditTransactionBegin += OnEditTransactionBegin;
            CutsceneEditor.OnEditTransactionCommit += OnEditTransactionCommit;
            CutsceneEditor.OnEditTransactionCancel += OnEditTransactionCancel;
            CutsceneEditor.OnEditorClosed += OnEditorClosed;
            CutsceneEditor.RecordUndoForCutscene = m_UndoPolicy;
            m_Request.Timeline.OnValueChanged += OnSourceTimelineChanged;
            Undo.undoRedoEvent += OnUndoRedoEvent;
        }

        public Cutscene Cutscene => m_Cutscene;

        public static BtsmtlSlateTimelineProjection Open(TimelineEditorOpenRequest request)
        {
            DisposeCurrent();
            s_Current = new BtsmtlSlateTimelineProjection(request);
            CutsceneEditor.ShowWindow(s_Current.m_Cutscene);
            return s_Current;
        }

        public static void DisposeCurrent()
        {
            s_Current?.Dispose();
            s_Current = null;
        }

        bool ShouldRecordUndo(Cutscene cutscene)
        {
            return !m_ReadOnly && !ReferenceEquals(cutscene, m_Cutscene);
        }

        void BuildProjection()
        {
            m_Host = new GameObject("__BTSMTL_SlateTimelineProjection__");
            m_Host.hideFlags = HideFlags.HideAndDontSave;
            m_Cutscene = m_Host.AddComponent<Cutscene>();
            m_Cutscene.hideFlags = HideFlags.HideAndDontSave;
            m_Cutscene.length = Mathf.Max(1f, m_Request.Timeline.Duration);
            m_Cutscene.viewTimeMin = 0f;
            m_Cutscene.viewTimeMax = Mathf.Max(m_Cutscene.length + 1f, m_Cutscene.length);

            GameObject groupObject = CreateChild(m_Cutscene.groupsRoot, "BTSMTL Timeline");
            BtsmtlSlateGroup group = groupObject.AddComponent<BtsmtlSlateGroup>();
            group.hideFlags = HideFlags.HideAndDontSave;
            group.name = m_Request.Timeline.Name;
            m_Cutscene.groups.Add(group);

            for (int trackIndex = 0; trackIndex < m_Request.Timeline.Tracks.Count; trackIndex++)
            {
                Track sourceTrack = m_Request.Timeline.Tracks[trackIndex];
                if (sourceTrack == null)
                    continue;
                GameObject trackObject = CreateChild(group.transform, sourceTrack.Name);
                BtsmtlSlateTrack proxyTrack = trackObject.AddComponent<BtsmtlSlateTrack>();
                proxyTrack.hideFlags = HideFlags.HideAndDontSave;
                proxyTrack.name = sourceTrack.Name;
                proxyTrack.Configure(sourceTrack.AuthoringId);
                group.tracks.Add(proxyTrack);
                m_SourceTracks[sourceTrack.AuthoringId] = sourceTrack;
                m_ProxyTracks[sourceTrack.AuthoringId] = proxyTrack;

                for (int clipIndex = 0; clipIndex < sourceTrack.Clips.Count; clipIndex++)
                {
                    Clip sourceClip = sourceTrack.Clips[clipIndex];
                    if (sourceClip == null)
                        continue;
                    GameObject clipObject = CreateChild(trackObject.transform, sourceClip.Name);
                    BtsmtlSlateActionClip proxyClip = clipObject.AddComponent<BtsmtlSlateActionClip>();
                    proxyClip.hideFlags = HideFlags.HideAndDontSave;
                    string displayName = sourceClip.Name;
                    if (sourceClip is ITimelineOwnedAuthoringIdentity)
                        displayName = $"{displayName} [{sourceClip.ContractKind}]";
                    proxyClip.Configure(displayName, sourceClip.Duration / (float)TimelineUtility.FrameRate);
                    proxyClip.ConfigureSource(sourceClip.AuthoringId);
                    proxyClip.startTime = sourceClip.StartFrame / (float)TimelineUtility.FrameRate;
                    proxyTrack.clips.Add(proxyClip);
                    m_SourceClips[sourceClip.AuthoringId] = sourceClip;
                    m_ProxyClips[sourceClip.AuthoringId] = proxyClip;
                }
            }

            for (int sectionIndex = 0; sectionIndex < m_Request.Timeline.Sections.Count; sectionIndex++)
            {
                TimelineSection section = m_Request.Timeline.Sections[sectionIndex];
                if (section != null)
                {
                    Slate.Section proxySection = new Slate.Section(
                        section.Name,
                        section.Frame / (float)TimelineUtility.FrameRate);
                    group.sections.Add(proxySection);
                    m_SourceSections[section.AuthoringId] = section;
                    m_ProxySections[section.AuthoringId] = proxySection;
                }
            }

            m_Cutscene.Validate();
        }

        static GameObject CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(string.IsNullOrEmpty(name) ? "Timeline" : name);
            child.hideFlags = HideFlags.HideAndDontSave;
            child.transform.SetParent(parent, false);
            return child;
        }

        void OnEditTransactionBegin(Cutscene cutscene, string undoName)
        {
            if (!ReferenceEquals(cutscene, m_Cutscene))
                return;
            m_BeginSnapshot = CaptureSnapshot();
        }

        void OnEditTransactionCommit(Cutscene cutscene)
        {
            if (!ReferenceEquals(cutscene, m_Cutscene) || m_BeginSnapshot == null)
                return;
            ProjectionSnapshot endSnapshot = CaptureSnapshot();
            ApplyDiff(m_BeginSnapshot, endSnapshot);
            m_BeginSnapshot = null;
            QueueRebuildProjection();
        }

        void OnEditTransactionCancel(Cutscene cutscene)
        {
            if (!ReferenceEquals(cutscene, m_Cutscene))
                return;
            m_BeginSnapshot = null;
            QueueRebuildProjection();
        }

        void OnEditorClosed()
        {
            m_EditorClosed = true;
            Dispose();
            if (ReferenceEquals(s_Current, this))
                s_Current = null;
        }

        void OnSourceTimelineChanged()
        {
            QueueRebuildProjection();
        }

        void OnUndoRedoEvent(in UndoRedoInfo info)
        {
            QueueRebuildProjection();
        }

        ProjectionSnapshot CaptureSnapshot()
        {
            var snapshot = new ProjectionSnapshot();
            foreach (CutsceneGroup group in m_Cutscene.groups)
            {
                for (int trackIndex = 0; trackIndex < group.tracks.Count; trackIndex++)
                {
                    if (!(group.tracks[trackIndex] is BtsmtlSlateTrack proxyTrack) ||
                        string.IsNullOrEmpty(proxyTrack.SourceAuthoringId))
                    {
                        snapshot.Unsupported = true;
                        continue;
                    }
                    snapshot.Tracks.Add(proxyTrack.SourceAuthoringId);
                    for (int clipIndex = 0; clipIndex < proxyTrack.clips.Count; clipIndex++)
                    {
                        if (!(proxyTrack.clips[clipIndex] is BtsmtlSlateActionClip proxyClip) ||
                            string.IsNullOrEmpty(proxyClip.SourceAuthoringId))
                        {
                            snapshot.Unsupported = true;
                            continue;
                        }
                        snapshot.Clips[proxyClip.SourceAuthoringId] =
                            new ProxyClipSnapshot(
                                proxyClip.startTime,
                                proxyClip.endTime,
                                proxyTrack.SourceAuthoringId);
                    }
                }
            }
            foreach (CutsceneGroup group in m_Cutscene.groups)
            {
                for (int sectionIndex = 0; sectionIndex < group.sections.Count; sectionIndex++)
                {
                    Slate.Section proxySection = group.sections[sectionIndex];
                    string sourceId = m_ProxySections.FirstOrDefault(pair => ReferenceEquals(pair.Value, proxySection)).Key;
                    if (string.IsNullOrEmpty(sourceId))
                    {
                        snapshot.Unsupported = true;
                        continue;
                    }
                    snapshot.Sections[sourceId] = new ProxySectionSnapshot(proxySection.name, proxySection.time);
                }
            }
            return snapshot;
        }

        void ApplyDiff(
            ProjectionSnapshot begin,
            ProjectionSnapshot end)
        {
            if (m_ReadOnly || end.Unsupported)
                return;
            var changes = new List<(Clip Clip, int StartFrame, int EndFrame)>();
            foreach (KeyValuePair<string, ProxyClipSnapshot> pair in end.Clips)
            {
                if (!begin.Clips.TryGetValue(pair.Key, out ProxyClipSnapshot before) ||
                    !m_SourceClips.TryGetValue(pair.Key, out Clip sourceClip))
                    continue;
                if (!string.Equals(before.TrackAuthoringId, FindProxyTrackAuthoringId(pair.Key), StringComparison.Ordinal))
                    return;
                int startFrame = Mathf.Max(0, Mathf.RoundToInt(pair.Value.StartTime * TimelineUtility.FrameRate));
                int endFrame = Mathf.Max(startFrame + 1, Mathf.RoundToInt(pair.Value.EndTime * TimelineUtility.FrameRate));
                int beforeStartFrame = Mathf.RoundToInt(before.StartTime * TimelineUtility.FrameRate);
                int beforeEndFrame = Mathf.RoundToInt(before.EndTime * TimelineUtility.FrameRate);
                if (startFrame != beforeStartFrame || endFrame != beforeEndFrame)
                    changes.Add((sourceClip, startFrame, endFrame));
            }

            var sectionChanges = new List<(TimelineSection Section, string Name, int Frame)>();
            foreach (KeyValuePair<string, ProxySectionSnapshot> pair in end.Sections)
            {
                if (!begin.Sections.TryGetValue(pair.Key, out ProxySectionSnapshot before) ||
                    !m_SourceSections.TryGetValue(pair.Key, out TimelineSection sourceSection))
                    continue;
                int frame = Mathf.Max(0, Mathf.RoundToInt(pair.Value.Time * TimelineUtility.FrameRate));
                int beforeFrame = Mathf.RoundToInt(before.Time * TimelineUtility.FrameRate);
                if (!string.Equals(before.Name, pair.Value.Name, StringComparison.Ordinal) || frame != beforeFrame)
                    sectionChanges.Add((sourceSection, pair.Value.Name, frame));
            }

            var removedTracks = m_SourceTracks
                .Where(pair => !end.Tracks.Contains(pair.Key))
                .Select(pair => pair.Value)
                .ToArray();
            var removedClips = m_SourceClips
                .Where(pair => !end.Clips.ContainsKey(pair.Key))
                .Select(pair => pair.Value)
                .ToArray();
            var removedSections = m_SourceSections
                .Where(pair => !end.Sections.ContainsKey(pair.Key))
                .Select(pair => pair.Value)
                .ToArray();

            if (changes.Count == 0 && sectionChanges.Count == 0 &&
                removedTracks.Length == 0 && removedClips.Length == 0 && removedSections.Length == 0)
                return;
            m_Session.Apply(() =>
            {
                for (int index = 0; index < removedTracks.Length; index++)
                    m_Request.Timeline.RemoveTrack(removedTracks[index]);
                for (int index = 0; index < removedClips.Length; index++)
                {
                    if (!removedTracks.Contains(removedClips[index].Track))
                        m_Request.Timeline.RemoveClip(removedClips[index]);
                }
                for (int index = 0; index < removedSections.Length; index++)
                    m_Request.Timeline.RemoveSection(removedSections[index]);
                for (int index = 0; index < changes.Count; index++)
                {
                    (Clip clip, int startFrame, int endFrame) = changes[index];
                    clip.StartFrame = startFrame;
                    clip.EndFrame = endFrame;
                    clip.Track.UpdateMix();
                }
                for (int index = 0; index < sectionChanges.Count; index++)
                {
                    (TimelineSection section, string name, int frame) = sectionChanges[index];
                    m_Request.Timeline.ConfigureSection(section, name, frame);
                }
            }, "Slate Timeline Edit");
        }

        string FindProxyTrackAuthoringId(string clipAuthoringId)
        {
            foreach (CutsceneGroup group in m_Cutscene.groups)
            {
                for (int trackIndex = 0; trackIndex < group.tracks.Count; trackIndex++)
                {
                    if (!(group.tracks[trackIndex] is BtsmtlSlateTrack track))
                        continue;
                    for (int clipIndex = 0; clipIndex < track.clips.Count; clipIndex++)
                    {
                        if (track.clips[clipIndex] is BtsmtlSlateActionClip clip &&
                            string.Equals(clip.SourceAuthoringId, clipAuthoringId, StringComparison.Ordinal))
                            return track.SourceAuthoringId;
                    }
                }
            }
            return string.Empty;
        }

        public void SetRuntimeReadOnly(bool readOnly)
        {
            m_ReadOnly = readOnly;
            CutsceneEditor.current?.Repaint();
        }

        public void ApplyRuntimeOverlay(
            float visualTime,
            IReadOnlyDictionary<string, string> activeTracks,
            IReadOnlyDictionary<string, string> activeClips)
        {
            if (m_Cutscene == null)
                return;
            m_Cutscene.currentTime = Mathf.Max(0f, visualTime);
            foreach (KeyValuePair<string, BtsmtlSlateTrack> pair in m_ProxyTracks)
            {
                bool active = activeTracks != null && activeTracks.ContainsKey(pair.Key);
                pair.Value.SetRuntimeActive(active);
            }
            foreach (KeyValuePair<string, BtsmtlSlateActionClip> pair in m_ProxyClips)
            {
                string status = string.Empty;
                if (activeClips != null)
                    activeClips.TryGetValue(pair.Key, out status);
                pair.Value.SetRuntimeStatus(status);
            }
            CutsceneEditor.current?.Repaint();
        }

        public void ClearRuntimeOverlay()
        {
            foreach (BtsmtlSlateTrack track in m_ProxyTracks.Values)
                track.SetRuntimeActive(true);
            foreach (BtsmtlSlateActionClip clip in m_ProxyClips.Values)
                clip.SetRuntimeStatus(string.Empty);
            CutsceneEditor.current?.Repaint();
        }

        void QueueRebuildProjection()
        {
            if (m_RebuildQueued || m_Disposed)
                return;
            m_RebuildQueued = true;
            EditorApplication.delayCall += RebuildProjection;
        }

        void RebuildProjection()
        {
            m_RebuildQueued = false;
            if (m_Disposed)
                return;
            m_SourceClips.Clear();
            m_ProxyClips.Clear();
            m_SourceTracks.Clear();
            m_ProxyTracks.Clear();
            m_SourceSections.Clear();
            m_ProxySections.Clear();
            foreach (Transform child in m_Host.transform.Cast<Transform>().ToArray())
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            if (m_Cutscene != null)
                UnityEngine.Object.DestroyImmediate(m_Cutscene);
            m_Host = null;
            m_Cutscene = null;
            BuildProjection();
            CutsceneEditor.ShowWindow(m_Cutscene);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            CutsceneEditor.OnEditTransactionBegin -= OnEditTransactionBegin;
            CutsceneEditor.OnEditTransactionCommit -= OnEditTransactionCommit;
            CutsceneEditor.OnEditTransactionCancel -= OnEditTransactionCancel;
            CutsceneEditor.OnEditorClosed -= OnEditorClosed;
            m_Request.Timeline.OnValueChanged -= OnSourceTimelineChanged;
            Undo.undoRedoEvent -= OnUndoRedoEvent;
            if (ReferenceEquals(CutsceneEditor.RecordUndoForCutscene, m_UndoPolicy))
                CutsceneEditor.RecordUndoForCutscene = null;
            if (!m_EditorClosed)
                CutsceneEditor.ClearCutscene(m_Cutscene);
            m_Session.Dispose();
            if (m_Host != null)
                UnityEngine.Object.DestroyImmediate(m_Host);
            m_Host = null;
            m_Cutscene = null;
            m_SourceClips.Clear();
            m_ProxyClips.Clear();
            m_SourceTracks.Clear();
            m_ProxyTracks.Clear();
            m_SourceSections.Clear();
            m_ProxySections.Clear();
            m_BeginSnapshot = null;
        }
    }
}
#endif
