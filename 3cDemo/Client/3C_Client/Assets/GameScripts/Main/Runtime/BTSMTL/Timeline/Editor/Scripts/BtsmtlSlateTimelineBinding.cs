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
    sealed class BtsmtlSlateTimelineBinding : IEmbeddedTimelineBinding, ITimelineAuthoringClipResolver
    {
        readonly TimelineEditorOpenRequest m_Request;
        readonly TimelineEditorSessionContext m_Session;
        readonly Action<Clip> m_OpenSourceClip;
        readonly List<IEmbeddedTimelineGroupBinding> m_Groups = new List<IEmbeddedTimelineGroupBinding>();
        readonly List<IEmbeddedTimelineSectionBinding> m_Sections = new List<IEmbeddedTimelineSectionBinding>();
        readonly Dictionary<string, BtsmtlTimelineTrackBinding> m_Tracks = new Dictionary<string, BtsmtlTimelineTrackBinding>(StringComparer.Ordinal);
        readonly Dictionary<string, BtsmtlTimelineClipBinding> m_Clips = new Dictionary<string, BtsmtlTimelineClipBinding>(StringComparer.Ordinal);
        readonly Dictionary<string, BtsmtlTimelineSectionBinding> m_SectionsById = new Dictionary<string, BtsmtlTimelineSectionBinding>(StringComparer.Ordinal);
        readonly string m_SourceRevision;
        IEmbeddedTimelineElementBinding m_Selected;
        BtsmtlTimelineClipBinding m_CopiedClip;
        bool m_EditActive;
        bool m_ReadOnly;
        string m_EditUndoName;
        int m_CurrentFrame;
        float m_ViewTimeMin;
        float m_ViewTimeMax;

        public BtsmtlSlateTimelineBinding(
            TimelineEditorOpenRequest request,
            TimelineEditorSessionContext session,
            Action<Clip> openSourceClip = null)
        {
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
            m_Session = session ?? throw new ArgumentNullException(nameof(session));
            m_OpenSourceClip = openSourceClip;
            m_SourceRevision = TimelineAuthoringFingerprint.Compute(request.Timeline);
            m_ViewTimeMin = 0f;
            m_ViewTimeMax = Mathf.Max(1f / FrameRate, Timeline.Duration);
            BuildBindings();
        }

        public TimelineData Timeline => m_Request.Timeline;
        public TimelineContractCatalog ContractCatalog => m_Request.ContractCatalog;
        public TimelineEditorSessionContext Session => m_Session;
        public int FrameRate => Mathf.Max(1, m_Session.FrameRate);
        public string DisplayName => Timeline.Name;
        public float Length => Timeline.Duration;
        public float Duration => Timeline.Duration;
        public IReadOnlyList<Track> Tracks => Timeline.Tracks;
        public IReadOnlyList<TimelineSection> SourceSections => Timeline.Sections;
        public int CurrentFrame
        {
            get => m_CurrentFrame;
            set => m_CurrentFrame = Mathf.Clamp(value, 0, Timeline.MaxFrame);
        }
        public float ViewTimeMin
        {
            get => m_ViewTimeMin;
            set => m_ViewTimeMin = Mathf.Min(value, ViewTimeMax - 1f / FrameRate);
        }
        public float ViewTimeMax
        {
            get => m_ViewTimeMax;
            set => m_ViewTimeMax = Mathf.Max(value, ViewTimeMin + 1f / FrameRate);
        }
        public bool IsReadOnly => m_ReadOnly || m_Session.IsReadOnly;
        public IReadOnlyList<IEmbeddedTimelineGroupBinding> Groups => m_Groups;
        public IReadOnlyList<IEmbeddedTimelineSectionBinding> Sections => m_Sections;
        public IEmbeddedTimelineElementBinding Selected => m_Selected;

        public bool IsSourceCurrent()
        {
            return string.Equals(
                m_SourceRevision,
                TimelineAuthoringFingerprint.Compute(Timeline),
                StringComparison.Ordinal);
        }

        public void Select(IEmbeddedTimelineElementBinding element)
        {
            m_Selected = element;
            foreach (BtsmtlTimelineTrackBinding track in m_Tracks.Values)
                track.SetSelectedClip(null);
            if (element is BtsmtlTimelineClipBinding clip)
                clip.Track.SetSelectedClip(clip);
            if (element is BtsmtlTimelineTrackBinding trackElement)
                trackElement.SetSelectedClip(null);
            m_Session.SetSelection(
                element is BtsmtlTimelineClipBinding selectedClip
                    ? selectedClip.Source
                    : element is BtsmtlTimelineTrackBinding selectedTrack
                        ? selectedTrack.Source
                        : element is BtsmtlTimelineSectionBinding selectedSection
                            ? selectedSection.Source
                            : null);
        }

        public void BeginEdit(string undoName)
        {
            if (m_EditActive || IsReadOnly)
                return;
            m_EditActive = true;
            m_EditUndoName = string.IsNullOrWhiteSpace(undoName) ? "Timeline Edit" : undoName;
        }

        public void CommitEdit()
        {
            if (!m_EditActive)
                return;
            m_EditActive = false;
            if (!IsSourceCurrent())
            {
                BuildBindings();
                return;
            }
            try
            {
                m_Session.Apply(() =>
                {
                    foreach (BtsmtlTimelineClipBinding clip in m_Clips.Values)
                        clip.CommitSource();
                    foreach (BtsmtlTimelineSectionBinding section in m_SectionsById.Values)
                        section.CommitSource();
                    Timeline.Init();
                }, m_EditUndoName);
            }
            catch (Exception)
            {
                BuildBindings();
                throw;
            }
            BuildBindings();
        }

        public void CancelEdit()
        {
            if (!m_EditActive)
                return;
            m_EditActive = false;
            BuildBindings();
        }

        public void RequestRepaint() { }

        public void AddTrack()
        {
            if (IsReadOnly)
                return;
            var menu = new GenericMenu();
            foreach (TimelineTrackContract contract in ContractCatalog.Tracks)
            {
                TimelineTrackContract candidate = contract;
                Type type = TimelineAuthoringTypeCatalog.RequireTrackType(candidate.Kind);
                bool requiresFields = type.GetCustomAttributes(typeof(TimelineAuthoringTrackFieldAttribute), true).Length > 0;
                menu.AddItem(new GUIContent(DisplayKind(candidate.Kind)), false, () => ShowTrackCreationPopup(candidate.Kind, requiresFields));
            }
            menu.ShowAsContext();
        }

        public void AddClip(IEmbeddedTimelineTrackBinding track, int frame)
        {
            if (IsReadOnly || !(track is BtsmtlTimelineTrackBinding formalTrack))
                return;
            ShowAddClipMenu(formalTrack.Source.AuthoringId, frame);
        }

        public void AddClipAt(string trackAuthoringId, int frame)
        {
            if (m_Tracks.TryGetValue(trackAuthoringId ?? string.Empty, out BtsmtlTimelineTrackBinding track))
                AddClip(track, frame);
        }

        public void DeleteTrack(IEmbeddedTimelineTrackBinding track)
        {
            if (IsReadOnly || !(track is BtsmtlTimelineTrackBinding formalTrack))
                return;
            ApplyImmediate(() => Timeline.RemoveTrack(formalTrack.Source), "Delete Timeline Track");
        }

        public void DeleteClip(IEmbeddedTimelineClipBinding clip)
        {
            if (IsReadOnly || !(clip is BtsmtlTimelineClipBinding formalClip))
                return;
            ApplyImmediate(() => Timeline.RemoveClip(formalClip.Source), "Delete Timeline Clip");
        }

        public void DeleteClips(IReadOnlyList<IEmbeddedTimelineClipBinding> clips)
        {
            if (clips == null || IsReadOnly)
                return;
            ApplyImmediate(() =>
            {
                for (int index = 0; index < clips.Count; index++)
                    if (clips[index] is BtsmtlTimelineClipBinding formalClip)
                        Timeline.RemoveClip(formalClip.Source);
            }, "Delete Timeline Clips");
        }

        public void SplitClip(IEmbeddedTimelineClipBinding clip, int frame)
        {
            if (IsReadOnly || !(clip is BtsmtlTimelineClipBinding formalClip))
                return;
            if (frame <= formalClip.Source.StartFrame || frame >= formalClip.Source.EndFrame)
                return;
            ApplyImmediate(() =>
            {
                Clip copy = ManagedReferenceCloneUtility.Clone(formalClip.Source);
                copy.RegenerateAuthoringIdentity();
                if (copy is ITimelineOwnedAuthoringIdentity owned)
                    owned.RegenerateOwnedAuthoringIdentity();
                copy.StartFrame = frame;
                copy.EndFrame = formalClip.Source.EndFrame;
                formalClip.Source.EndFrame = frame;
                formalClip.Source.Track.Clips.Add(copy);
                formalClip.Source.Track.UpdateMix();
            }, "Split Timeline Clip");
        }

        public void MoveTrack(IEmbeddedTimelineTrackBinding track, int index)
        {
            if (IsReadOnly || !(track is BtsmtlTimelineTrackBinding formalTrack))
                return;
            ApplyImmediate(() =>
            {
                List<Track> tracks = Timeline.Tracks;
                tracks.Remove(formalTrack.Source);
                tracks.Insert(Mathf.Clamp(index, 0, tracks.Count), formalTrack.Source);
            }, "Reorder Timeline Track");
        }

        public void MoveClip(IEmbeddedTimelineClipBinding clip, int startFrame)
        {
            if (!(clip is BtsmtlTimelineClipBinding formalClip))
                return;
            formalClip.StartTime = Mathf.Max(0, startFrame) / (float)FrameRate;
        }

        public void ConfigureSection(IEmbeddedTimelineSectionBinding section, string name, int frame)
        {
            if (IsReadOnly || !(section is BtsmtlTimelineSectionBinding formalSection))
                return;
            formalSection.Name = name ?? string.Empty;
            formalSection.Time = Mathf.Max(0, frame) / (float)FrameRate;
        }

        public void DeleteSection(IEmbeddedTimelineSectionBinding section)
        {
            if (IsReadOnly || !(section is BtsmtlTimelineSectionBinding formalSection))
                return;
            ApplyImmediate(() => Timeline.RemoveSection(formalSection.Source), "Delete Timeline Section");
        }

        public void AddSection(int frame)
        {
            if (IsReadOnly)
                return;
            ApplyImmediate(() => Timeline.AddSection("Section", Mathf.Max(0, frame)), "Add Timeline Section");
        }

        public void CopyClip(IEmbeddedTimelineClipBinding clip)
        {
            m_CopiedClip = clip as BtsmtlTimelineClipBinding;
        }

        public void CopySourceClip(Clip clip)
        {
            m_CopiedClip = clip != null && m_Clips.TryGetValue(clip.AuthoringId, out BtsmtlTimelineClipBinding value)
                ? value
                : null;
        }

        public void PasteClip(IEmbeddedTimelineTrackBinding track, int frame)
        {
            if (IsReadOnly || m_CopiedClip == null || !(track is BtsmtlTimelineTrackBinding formalTrack))
                return;
            ApplyImmediate(() =>
            {
                Clip clone = ManagedReferenceCloneUtility.Clone(m_CopiedClip.Source);
                clone.RegenerateAuthoringIdentity();
                if (clone is ITimelineOwnedAuthoringIdentity owned)
                    owned.RegenerateOwnedAuthoringIdentity();
                clone.StartFrame = Mathf.Max(0, frame);
                clone.EndFrame = clone.StartFrame + Mathf.Max(1, m_CopiedClip.Source.Duration);
                ContractCatalog.RequireClipPlacement(formalTrack.Source, clone);
                formalTrack.Source.Clips.Add(clone);
                formalTrack.Source.UpdateMix();
            }, "Paste Timeline Clip");
        }

        public void OpenSource(IEmbeddedTimelineClipBinding clip)
        {
            if (clip is BtsmtlTimelineClipBinding formalClip)
                m_OpenSourceClip?.Invoke(formalClip.Source);
        }

        public bool TryResolveMotionClip(TimelineData timeline, string identity, out MotionCurveClip clip)
        {
            clip = timeline?.Tracks
                .SelectMany(track => track.Clips)
                .OfType<MotionCurveClip>()
                .FirstOrDefault(value => string.Equals(value.AuthoringId, identity, StringComparison.Ordinal));
            return clip != null;
        }

        public bool TryGetTrack(string authoringId, out Track track)
        {
            track = null;
            return m_Tracks.TryGetValue(authoringId ?? string.Empty, out BtsmtlTimelineTrackBinding binding) &&
                   (track = binding.Source) != null;
        }

        public bool TryGetTrackBinding(string authoringId, out IEmbeddedTimelineTrackBinding binding)
        {
            if (m_Tracks.TryGetValue(authoringId ?? string.Empty, out BtsmtlTimelineTrackBinding value))
            {
                binding = value;
                return true;
            }
            binding = null;
            return false;
        }

        bool IEmbeddedTimelineBinding.TryGetTrack(string authoringId, out IEmbeddedTimelineTrackBinding track)
        {
            return TryGetTrackBinding(authoringId, out track);
        }

        public bool TryGetClip(string authoringId, out Clip clip)
        {
            clip = null;
            return m_Clips.TryGetValue(authoringId ?? string.Empty, out BtsmtlTimelineClipBinding binding) &&
                   (clip = binding.Source) != null;
        }

        public void Apply(Action mutation, string undoName)
        {
            m_Session.Apply(mutation, undoName);
        }

        public List<TimelineCurveChannelDescriptor> CollectCurveChannels(Track track)
        {
            var result = new List<TimelineCurveChannelDescriptor>();
            if (track != null)
                TimelineCurveChannelCatalog.CollectForTrack(track, result);
            return result;
        }

        public void Rebuild()
        {
            string selectedId = m_Selected?.AuthoringId ?? string.Empty;
            BuildBindings();
            if (m_Clips.TryGetValue(selectedId, out BtsmtlTimelineClipBinding clip))
                Select(clip);
            else if (m_Tracks.TryGetValue(selectedId, out BtsmtlTimelineTrackBinding track))
                Select(track);
        }

        void ApplyImmediate(Action mutation, string undoName)
        {
            if (!IsSourceCurrent())
            {
                Rebuild();
                return;
            }
            m_Session.Apply(() =>
            {
                mutation();
                Timeline.Init();
            }, undoName);
            Rebuild();
        }

        void BuildBindings()
        {
            m_Groups.Clear();
            m_Sections.Clear();
            m_Tracks.Clear();
            m_Clips.Clear();
            m_SectionsById.Clear();
            var group = new BtsmtlTimelineGroupBinding(this, Timeline.Name);
            m_Groups.Add(group);
            for (int trackIndex = 0; trackIndex < Timeline.Tracks.Count; trackIndex++)
            {
                Track source = Timeline.Tracks[trackIndex];
                if (source == null)
                    continue;
                var track = new BtsmtlTimelineTrackBinding(this, group, source);
                group.AddTrack(track);
                m_Tracks[source.AuthoringId] = track;
                for (int clipIndex = 0; clipIndex < source.Clips.Count; clipIndex++)
                {
                    Clip sourceClip = source.Clips[clipIndex];
                    if (sourceClip == null)
                        continue;
                    var clip = new BtsmtlTimelineClipBinding(this, track, sourceClip);
                    track.AddClip(clip);
                    m_Clips[sourceClip.AuthoringId] = clip;
                }
            }
            for (int sectionIndex = 0; sectionIndex < Timeline.Sections.Count; sectionIndex++)
            {
                TimelineSection source = Timeline.Sections[sectionIndex];
                if (source == null)
                    continue;
                var section = new BtsmtlTimelineSectionBinding(this, source);
                m_Sections.Add(section);
                m_SectionsById[source.AuthoringId] = section;
            }
        }

        void ShowTrackCreationPopup(string kind, bool requiresFields)
        {
            PopupWindow.Show(new Rect(0, 0, 1, 1), new TimelineTrackCreationPopup(
                kind,
                DisplayKind(kind),
                requiresFields,
                (name, channelId, slotId) => CreateTrack(kind, name, channelId, slotId)));
        }

        bool CreateTrack(string kind, string name, string channelId, string slotId)
        {
            if (IsReadOnly || !IsSourceCurrent())
                return false;
            try
            {
                ApplyImmediate(() =>
                {
                    Type trackType = TimelineAuthoringTypeCatalog.RequireTrackType(kind);
                    int count = Timeline.Tracks.Count;
                    Timeline.AddTrack(trackType, ContractCatalog);
                    Track added = Timeline.Tracks[count];
                    added.Name = string.IsNullOrWhiteSpace(name) ? DisplayKind(kind) : name.Trim();
                    TimelineAuthoringTrackBinding.Apply(added, channelId, slotId);
                }, "Add Timeline Track");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        void ShowAddClipMenu(string trackAuthoringId, int frame)
        {
            if (!m_Tracks.TryGetValue(trackAuthoringId ?? string.Empty, out BtsmtlTimelineTrackBinding track) ||
                !ContractCatalog.TryGetTrack(track.Source.ContractKind, out TimelineTrackContract contract))
                return;
            var menu = new GenericMenu();
            for (int index = 0; index < contract.AllowedClipKinds.Count; index++)
            {
                string kind = contract.AllowedClipKinds[index];
                menu.AddItem(new GUIContent(DisplayKind(kind)), false, () => ShowClipCreationPopup(trackAuthoringId, kind, frame));
            }
            menu.ShowAsContext();
        }

        void ShowClipCreationPopup(string trackAuthoringId, string kind, int frame)
        {
            var motionClipIds = Timeline.Tracks.SelectMany(track => track.Clips).OfType<MotionCurveClip>().Select(clip => clip.AuthoringId).ToArray();
            var request = new TimelineClipCreationRequest
            {
                TrackAuthoringId = trackAuthoringId,
                Kind = kind,
                FrameRate = FrameRate,
                StartFrame = frame,
                EndFrame = frame + Mathf.Max(1, FrameRate / 20),
                DefaultEndFrame = frame + Mathf.Max(1, FrameRate / 20)
            };
            PopupWindow.Show(new Rect(0, 0, 1, 1), new TimelineClipCreationPopup(
                request,
                motionClipIds,
                Timeline.ExternalBindings,
                CreateClip));
        }

        bool CreateClip(TimelineClipCreationRequest request)
        {
            if (IsReadOnly || !IsSourceCurrent() || !m_Tracks.TryGetValue(request.TrackAuthoringId, out BtsmtlTimelineTrackBinding track))
                return false;
            try
            {
                ApplyImmediate(() =>
                {
                    Clip added = request.Kind == TimelineContractKinds.AnimationClip
                        ? TimelineAuthoringTrackBinding.CreateClip(Timeline, ContractCatalog, track.Source, request.Resource as UnityEngine.AnimationClip, request.StartFrame)
                        : request.Kind == TimelineContractKinds.MotionCurveClip
                            ? Timeline.AddClip(ContractCatalog, request.SourceCurve, track.Source, request.StartFrame)
                        : request.Resource != null
                            ? Timeline.AddClip(ContractCatalog, request.Resource, track.Source, request.StartFrame)
                            : Timeline.AddClip(ContractCatalog, track.Source, request.StartFrame);
                    added.EndFrame = Mathf.Max(request.StartFrame + 1, request.EndFrame);
                    TimelineAuthoringClipBinding.Configure(Timeline, added, ReadConfiguration(added, request), this);
                    added.Track.UpdateMix();
                }, "Add Timeline Clip");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        static TimelineAuthoringClipConfiguration ReadConfiguration(Clip clip, TimelineClipCreationRequest request)
        {
            TimelineAuthoringClipConfiguration configuration = TimelineAuthoringClipBinding.Read(clip);
            if (request.Kind == TimelineContractKinds.AnimationClip)
            {
                configuration.Extrapolation = request.Extrapolation;
                configuration.BlendProfileId = request.BlendProfileId;
            }
            if (request.Kind == TimelineContractKinds.MotionCurveClip)
            {
                configuration.CurveId = request.CurveId;
                configuration.SourceCurve = request.SourceCurve;
                configuration.SourceStartTime = request.SourceStartTime;
                configuration.SourceEndTime = request.SourceEndTime;
                configuration.Space = request.Space;
                configuration.Channel = request.Channel;
                configuration.BlendMode = request.BlendMode;
                configuration.Priority = request.Priority;
                configuration.ConsumeLowerChannels = request.ConsumeLowerChannels;
            }
            if (request.Kind == TimelineContractKinds.MotionWarpClip)
                configuration.SourceMotionClipId = request.SourceMotionClipId;
            if (request.Kind == TimelineContractKinds.ActionCueClip)
            {
                configuration.CueId = request.CueId;
                configuration.CueType = request.CueType;
            }
            if (request.Kind == TimelineContractKinds.CameraStateClip)
            {
                configuration.CameraMode = request.CameraMode;
                configuration.Priority = request.CameraPriority;
                configuration.CameraBlendInSeconds = request.CameraBlendInSeconds;
                configuration.CameraBlendOutSeconds = request.CameraBlendOutSeconds;
                configuration.CameraTargetKey = request.CameraTargetKey;
                configuration.CameraInterruptPolicy = request.CameraInterruptPolicy;
            }
            if (request.Kind == TimelineContractKinds.CameraCueClip)
            {
                configuration.CueId = request.CueId;
                configuration.CameraCueKind = request.CameraCueKind;
                configuration.CueType = request.CueType;
                configuration.CameraIntensity = request.CameraIntensity;
                configuration.CameraDurationSeconds = request.CameraDurationSeconds;
                configuration.Priority = request.CameraPriority;
            }
            if (request.Kind == TimelineContractKinds.CameraResponseClip)
            {
                configuration.CameraLookResponse = request.CameraLookResponse;
                configuration.ManualOrbitWeight = request.ManualOrbitWeight;
                configuration.PitchResponseWeight = request.PitchResponseWeight;
                configuration.YawResponseWeight = request.YawResponseWeight;
                configuration.Priority = request.CameraPriority;
            }
            if (request.Kind == TimelineContractKinds.ScenePresentationParameterCurveClip)
            {
                configuration.TargetBindingId = request.TargetBindingId;
                configuration.ParameterBindingId = request.ParameterBindingId;
            }
            return configuration;
        }

        static string DisplayKind(string kind)
        {
            int separator = kind.LastIndexOf('.');
            return (separator >= 0 ? kind.Substring(0, separator) : kind).Replace('-', ' ');
        }

        sealed class BtsmtlTimelineGroupBinding : IEmbeddedTimelineGroupBinding
        {
            readonly List<IEmbeddedTimelineTrackBinding> m_Tracks = new List<IEmbeddedTimelineTrackBinding>();
            public BtsmtlTimelineGroupBinding(BtsmtlSlateTimelineBinding owner, string name)
            {
                Owner = owner;
                DisplayName = string.IsNullOrEmpty(name) ? "Timeline" : name;
            }
            public BtsmtlSlateTimelineBinding Owner { get; }
            public string AuthoringId => Owner.Timeline.AuthoringId;
            public string DisplayName { get; }
            public bool IsActive { get; set; } = true;
            public bool IsCollapsed { get; set; }
            public bool IsLocked { get; set; }
            public IReadOnlyList<IEmbeddedTimelineTrackBinding> Tracks => m_Tracks;
            public void AddTrack(BtsmtlTimelineTrackBinding track) => m_Tracks.Add(track);
        }

        sealed class BtsmtlTimelineTrackBinding : IEmbeddedTimelineTrackBinding
        {
            readonly List<IEmbeddedTimelineClipBinding> m_Clips = new List<IEmbeddedTimelineClipBinding>();
            IEmbeddedTimelineClipBinding m_SelectedClip;
            public BtsmtlTimelineTrackBinding(BtsmtlSlateTimelineBinding owner, BtsmtlTimelineGroupBinding group, Track source)
            {
                Owner = owner;
                Group = group;
                Source = source;
            }
            public BtsmtlSlateTimelineBinding Owner { get; }
            public BtsmtlTimelineGroupBinding Group { get; }
            public Track Source { get; }
            public string AuthoringId => Source.AuthoringId;
            public string DisplayName => string.IsNullOrEmpty(Source.Name) ? Source.ContractKind : Source.Name;
            public bool IsActive { get => !Source.PersistentMuted; set => Source.PersistentMuted = !value; }
            public bool IsLocked { get; set; }
            public bool ShowCurves { get; set; }
            public Color Color => Source.Color();
            public float StartTime => 0f;
            public float EndTime => Owner.Length;
            public float DefaultHeight => 32f;
            public float FinalHeight => ShowCurves ? 250f : DefaultHeight;
            public IReadOnlyList<IEmbeddedTimelineClipBinding> Clips => m_Clips;
            public IEmbeddedTimelineClipBinding SelectedClip => m_SelectedClip;
            public void AddClip(BtsmtlTimelineClipBinding clip) => m_Clips.Add(clip);
            public void SetSelectedClip(BtsmtlTimelineClipBinding clip) => m_SelectedClip = clip;
        }

        sealed class BtsmtlTimelineClipBinding : IEmbeddedTimelineClipBinding
        {
            readonly List<IEmbeddedTimelineCurveBinding> m_Curves = new List<IEmbeddedTimelineCurveBinding>();
            readonly List<IEmbeddedTimelineParameterBinding> m_Parameters = new List<IEmbeddedTimelineParameterBinding>();
            readonly BtsmtlSlateTimelineBinding m_Owner;
            readonly BtsmtlTimelineTrackBinding m_Track;
            float m_StartTime;
            float m_EndTime;
            float m_BlendIn;
            float m_BlendOut;
            bool m_IsCollapsed;
            bool m_IsLocked;

            public BtsmtlTimelineClipBinding(BtsmtlSlateTimelineBinding owner, BtsmtlTimelineTrackBinding track, Clip source)
            {
                m_Owner = owner;
                m_Track = track;
                Source = source;
                m_StartTime = source.StartFrame / (float)owner.FrameRate;
                m_EndTime = source.EndFrame / (float)owner.FrameRate;
                m_BlendIn = source.EaseInFrame / (float)owner.FrameRate;
                m_BlendOut = source.EaseOutFrame / (float)owner.FrameRate;
                var descriptors = owner.CollectCurveChannels(source.Track);
                for (int index = 0; index < descriptors.Count; index++)
                {
                    TimelineCurveChannelDescriptor descriptor = descriptors[index];
                    if (!descriptor.Supports(source))
                        continue;
                    var curve = new BtsmtlTimelineCurveBinding(this, descriptor);
                    m_Curves.Add(curve);
                    m_Parameters.Add(new BtsmtlTimelineParameterBinding(this, curve));
                }
            }

            public Clip Source { get; }
            internal BtsmtlSlateTimelineBinding Owner => m_Owner;
            public BtsmtlTimelineTrackBinding Track => m_Track;
            public string AuthoringId => Source.AuthoringId;
            public string DisplayName => Source.Name;
            public string Info => Source.Name;
            public bool IsActive => m_Track.IsActive;
            public bool IsValid => !Source.Invalid;
            public bool IsCollapsed { get => m_IsCollapsed; set => m_IsCollapsed = value; }
            public bool IsLocked { get => m_IsLocked; set => m_IsLocked = value; }
            public float StartTime { get => m_StartTime; set => m_StartTime = Mathf.Max(0f, value); }
            public float EndTime { get => m_EndTime; set => m_EndTime = Mathf.Max(StartTime + 1f / m_Owner.FrameRate, value); }
            public float Length => Mathf.Max(0f, EndTime - StartTime);
            public float BlendIn { get => Mathf.Clamp(m_BlendIn, 0f, Length); set => m_BlendIn = Mathf.Clamp(value, 0f, Length); }
            public float BlendOut { get => Mathf.Clamp(m_BlendOut, 0f, Length); set => m_BlendOut = Mathf.Clamp(value, 0f, Length); }
            public bool CanScale => Source.IsResizable();
            public bool CanBlendIn => Source.IsMixable();
            public bool CanBlendOut => Source.IsMixable();
            public IReadOnlyList<IEmbeddedTimelineParameterBinding> Parameters => m_Parameters;
            public IReadOnlyList<IEmbeddedTimelineCurveBinding> Curves => m_Curves;

            public bool CanCrossBlend(IEmbeddedTimelineClipBinding other)
            {
                return other is BtsmtlTimelineClipBinding formal && formal.Source.GetType() == Source.GetType() && Source.IsMixable();
            }

            public void AddIdentityKey(float time)
            {
                for (int index = 0; index < m_Curves.Count; index++)
                {
                    AnimationCurve curve = m_Curves[index].Curve;
                    curve.AddKey(time, curve.Evaluate(time));
                }
            }

            public void Split(float time) => m_Owner.SplitClip(this, Mathf.RoundToInt(time * m_Owner.FrameRate));

            public void StretchFit()
            {
                StartTime = 0f;
                EndTime = m_Track.EndTime;
            }

            public void CleanKeysOffRange()
            {
                for (int curveIndex = 0; curveIndex < m_Curves.Count; curveIndex++)
                    ((BtsmtlTimelineCurveBinding)m_Curves[curveIndex]).Trim(0f, Length);
            }

            public void ResetAnimation()
            {
                for (int index = 0; index < m_Curves.Count; index++)
                    ((BtsmtlTimelineCurveBinding)m_Curves[index]).Replace(new AnimationCurve());
            }

            public void CommitSource()
            {
                Source.StartFrame = Mathf.Max(0, Mathf.RoundToInt(StartTime * m_Owner.FrameRate));
                Source.EndFrame = Mathf.Max(Source.StartFrame + 1, Mathf.RoundToInt(EndTime * m_Owner.FrameRate));
                Source.SelfEaseInFrame = Mathf.Clamp(Mathf.RoundToInt(BlendIn * m_Owner.FrameRate), 0, Source.Duration - 1);
                Source.SelfEaseOutFrame = Mathf.Clamp(Mathf.RoundToInt(BlendOut * m_Owner.FrameRate), 0, Source.Duration - Source.SelfEaseInFrame - 1);
                for (int index = 0; index < m_Curves.Count; index++)
                    ((BtsmtlTimelineCurveBinding)m_Curves[index]).CommitSource();
                Source.Track.UpdateMix();
            }
        }

        sealed class BtsmtlTimelineCurveBinding : IEmbeddedTimelineCurveBinding
        {
            readonly BtsmtlTimelineClipBinding m_Clip;
            readonly TimelineCurveChannelDescriptor m_Descriptor;
            readonly float m_Duration;
            AnimationCurve m_Curve;

            public BtsmtlTimelineCurveBinding(BtsmtlTimelineClipBinding clip, TimelineCurveChannelDescriptor descriptor)
            {
                m_Clip = clip;
                m_Descriptor = descriptor;
                m_Duration = CurveDuration(clip.Source, descriptor, clip.Owner.FrameRate);
                m_Curve = ConvertCurveTime(descriptor.Read(clip.Source), m_Duration, false);
            }
            public string ChannelId => m_Descriptor.ChannelId.Value;
            public string DisplayName => m_Descriptor.DisplayName;
            public AnimationCurve Curve => m_Curve;
            public int StartFrame => m_Clip.Source.StartFrame;
            public int EndFrame => m_Clip.Source.EndFrame;
            public float Duration => m_Duration;
            public void Replace(AnimationCurve curve) => m_Curve = TimelineCurveAuthoring.CopyCurve(curve);
            public void Trim(float min, float max)
            {
                for (int index = m_Curve.length - 1; index >= 0; index--)
                    if (m_Curve[index].time < min || m_Curve[index].time > max)
                        m_Curve.RemoveKey(index);
            }
            public void CommitSource() => m_Descriptor.Replace(m_Clip.Source, ConvertCurveTime(m_Curve, m_Duration, true));

            static float CurveDuration(Clip clip, TimelineCurveChannelDescriptor descriptor, int frameRate) =>
                Mathf.Max(1f / frameRate, clip.Duration / (float)frameRate);

            static AnimationCurve ConvertCurveTime(AnimationCurve source, float duration, bool toNormalized)
            {
                AnimationCurve result = TimelineCurveAuthoring.CopyCurve(source);
                float safeDuration = Mathf.Max(0.0001f, duration);
                Keyframe[] keys = result.keys;
                for (int index = 0; index < keys.Length; index++)
                {
                    Keyframe key = keys[index];
                    if (toNormalized)
                    {
                        key.time /= safeDuration;
                        key.inTangent *= safeDuration;
                        key.outTangent *= safeDuration;
                    }
                    else
                    {
                        key.time *= safeDuration;
                        key.inTangent /= safeDuration;
                        key.outTangent /= safeDuration;
                    }
                    keys[index] = key;
                }
                result.keys = keys;
                return result;
            }
        }

        sealed class BtsmtlTimelineParameterBinding : IEmbeddedTimelineParameterBinding
        {
            readonly BtsmtlTimelineClipBinding m_Clip;
            readonly BtsmtlTimelineCurveBinding m_Curve;
            public BtsmtlTimelineParameterBinding(BtsmtlTimelineClipBinding clip, BtsmtlTimelineCurveBinding curve)
            {
                m_Clip = clip;
                m_Curve = curve;
            }
            public string ParameterId => m_Curve.ChannelId;
            public string DisplayName => m_Curve.DisplayName;
            public bool Enabled { get; set; } = true;
            public float CurrentValue => m_Curve.Curve.Evaluate(Mathf.Clamp(
                (m_Clip.Owner.CurrentFrame - m_Clip.Source.StartFrame) / (float)m_Clip.Owner.FrameRate,
                0f,
                m_Curve.Duration));
            public IReadOnlyList<IEmbeddedTimelineCurveBinding> Curves => new[] { m_Curve };
            public void AddKey(float localTime) => m_Curve.Curve.AddKey(localTime, m_Curve.Curve.Evaluate(localTime));
            public void RemoveKey(float localTime)
            {
                for (int index = m_Curve.Curve.length - 1; index >= 0; index--)
                    if (Mathf.Abs(m_Curve.Curve[index].time - localTime) <= 0.0001f)
                        m_Curve.Curve.RemoveKey(index);
            }
            public void SelectPreviousKey(float localTime) { }
            public void SelectNextKey(float localTime) { }
        }

        sealed class BtsmtlTimelineSectionBinding : IEmbeddedTimelineSectionBinding
        {
            readonly BtsmtlSlateTimelineBinding m_Owner;
            public BtsmtlTimelineSectionBinding(BtsmtlSlateTimelineBinding owner, TimelineSection source)
            {
                m_Owner = owner;
                Source = source;
                Name = source.Name;
                Time = source.Frame / (float)owner.FrameRate;
            }
            public TimelineSection Source { get; }
            public string AuthoringId => Source.AuthoringId;
            public string DisplayName => Name;
            public bool IsLocked { get; set; }
            public string Name { get; set; }
            public float Time { get; set; }
            public Color Color => Color.white;
            public void CommitSource() => Source.Configure(Name, Mathf.RoundToInt(Time * m_Owner.FrameRate));
        }
    }
}
#endif
