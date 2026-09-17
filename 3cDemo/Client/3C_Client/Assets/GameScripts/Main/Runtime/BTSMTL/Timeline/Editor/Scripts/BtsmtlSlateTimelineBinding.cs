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
        string m_SourceRevision;
        IEmbeddedTimelineElementBinding m_Selected;
        Clip m_CopiedClip;
        bool m_EditActive;
        bool m_ReadOnly;
        string m_EditUndoName;
        string m_EditSourceRevision;
        int m_CurrentFrame;
        float m_ViewTimeMin;
        float m_ViewTimeMax;
        Vector2 m_PopupPosition;

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
            set
            {
                int nextFrame = Mathf.Clamp(value, 0, Timeline.MaxFrame);
                if (m_CurrentFrame == nextFrame)
                    return;
                m_CurrentFrame = nextFrame;
                RequestRepaint();
            }
        }
        public float ViewTimeMin
        {
            get => m_ViewTimeMin;
            set
            {
                float nextValue = Mathf.Min(value, ViewTimeMax - 1f / FrameRate);
                if (Mathf.Approximately(m_ViewTimeMin, nextValue))
                    return;
                m_ViewTimeMin = nextValue;
                RequestRepaint();
            }
        }
        public float ViewTimeMax
        {
            get => m_ViewTimeMax;
            set
            {
                float nextValue = Mathf.Max(value, ViewTimeMin + 1f / FrameRate);
                if (Mathf.Approximately(m_ViewTimeMax, nextValue))
                    return;
                m_ViewTimeMax = nextValue;
                RequestRepaint();
            }
        }
        public bool IsReadOnly => m_ReadOnly || m_Session.IsReadOnly;
        public IReadOnlyList<IEmbeddedTimelineGroupBinding> Groups => m_Groups;
        public IReadOnlyList<IEmbeddedTimelineSectionBinding> Sections => m_Sections;
        public IEmbeddedTimelineElementBinding Selected => m_Selected;

        public void SetReadOnly(bool readOnly)
        {
            m_ReadOnly = readOnly;
        }

        public bool IsSourceCurrent()
        {
            return string.Equals(
                m_SourceRevision,
                TimelineAuthoringFingerprint.Compute(Timeline),
                StringComparison.Ordinal);
        }

        public void Select(IEmbeddedTimelineElementBinding element)
        {
            bool changed = !ReferenceEquals(m_Selected, element);
            m_Selected = element;
            Selection.activeObject = m_Request.SerializedOwner;
            object source = element is BtsmtlTimelineClipBinding clipSource
                ? clipSource.Source
                : element is BtsmtlTimelineTrackBinding trackSource
                    ? trackSource.Source
                    : element is BtsmtlTimelineSectionBinding sectionSource
                        ? sectionSource.Source
                        : null;
            if (source != null && Timeline.TryGetSerializedPropertyPath(source, out string propertyPath))
                TimelineInspectorSelection.Set(m_Request.SerializedOwner, propertyPath, source);
            else
                TimelineInspectorSelection.Clear(m_Request.SerializedOwner);
            m_Session.SetSelection(
                element is BtsmtlTimelineClipBinding selectedClip
                    ? selectedClip.Source
                    : element is BtsmtlTimelineTrackBinding selectedTrack
                        ? selectedTrack.Source
                        : element is BtsmtlTimelineSectionBinding selectedSection
                            ? selectedSection.Source
                        : null);
            if (changed)
                RequestRepaint();
        }

        public void BeginEdit(string undoName)
        {
            if (m_EditActive || IsReadOnly)
                return;
            m_EditActive = true;
            m_EditUndoName = string.IsNullOrWhiteSpace(undoName) ? "Timeline Edit" : undoName;
            m_EditSourceRevision = m_SourceRevision;
        }

        public void CommitEdit()
        {
            if (!m_EditActive)
                return;
            float previousLength = Length;
            m_EditActive = false;
            if (!string.Equals(m_EditSourceRevision, m_SourceRevision, StringComparison.Ordinal) ||
                !IsSourceCurrent())
            {
                m_EditSourceRevision = string.Empty;
                Rebuild();
                ExpandViewToLength(previousLength);
                ReportIssue("Timeline 内容已被外部修改，当前编辑已丢弃。");
                return;
            }
            if (!m_Clips.Values.Any(clip => clip.HasChanges()) &&
                !m_SectionsById.Values.Any(section => section.HasChanges()))
            {
                m_EditSourceRevision = string.Empty;
                Rebuild();
                ExpandViewToLength(previousLength);
                return;
            }
            try
            {
                m_Session.Apply(() =>
                {
                    bool changed = false;
                    foreach (BtsmtlTimelineClipBinding clip in m_Clips.Values)
                        changed |= clip.CommitSource();
                    foreach (BtsmtlTimelineSectionBinding section in m_SectionsById.Values)
                        changed |= section.CommitSource();
                    if (!changed)
                        return;
                    Timeline.Init();
                    ValidateTimeline();
                }, m_EditUndoName);
            }
            catch (Exception exception)
            {
                m_EditSourceRevision = string.Empty;
                Rebuild();
                ExpandViewToLength(previousLength);
                ReportIssue(exception.Message);
            }
            m_EditSourceRevision = string.Empty;
            Rebuild();
            ExpandViewToLength(previousLength);
        }

        public void CancelEdit()
        {
            if (!m_EditActive)
                return;
            m_EditActive = false;
            m_EditSourceRevision = string.Empty;
            Rebuild();
        }

        public event Action RepaintRequested;

        public void RequestRepaint()
        {
            RepaintRequested?.Invoke();
        }

        public void AddTrack()
        {
            if (IsReadOnly)
                return;
            CapturePopupPosition();
            var menu = new GenericMenu();
            foreach (TimelineTrackContract contract in ContractCatalog.Tracks)
            {
                TimelineTrackContract candidate = contract;
                IReadOnlyList<TimelineAuthoringTrackFieldAttribute> fields = TimelineAuthoringTrackBinding.GetFields(candidate.Kind);
                menu.AddItem(new GUIContent(DisplayKind(candidate.Kind)), false, () => ShowTrackCreationPopup(candidate.Kind, fields));
            }
            menu.ShowAsContext();
        }

        public void AddClip(IEmbeddedTimelineTrackBinding track, int frame)
        {
            if (IsReadOnly)
                return;
            if (!(track is BtsmtlTimelineTrackBinding formalTrack))
            {
                ReportIssue("Add Clip 需要正式 Timeline Track。");
                return;
            }
            if (formalTrack.IsLocked)
            {
                ReportIssue("当前 Track 已锁定，不能添加 Clip。");
                return;
            }
            CapturePopupPosition();
            if (ContractCatalog.TryGetTrack(formalTrack.Source.ContractKind, out TimelineTrackContract contract) &&
                contract.AllowedClipKinds.Count == 1 &&
                string.Equals(contract.AllowedClipKinds[0], TimelineContractKinds.TreeClip, StringComparison.Ordinal))
            {
                ShowClipCreationPopup(formalTrack.Source.AuthoringId, TimelineContractKinds.TreeClip, frame);
                return;
            }
            ShowAddClipMenu(formalTrack.Source.AuthoringId, frame);
        }

        public void SetTrackActive(IEmbeddedTimelineTrackBinding track, bool active)
        {
            if (IsReadOnly || !(track is BtsmtlTimelineTrackBinding formalTrack))
                return;
            if (formalTrack.IsLocked)
            {
                ReportIssue("当前 Track 已锁定，不能修改 Active 状态。");
                return;
            }
            ApplyImmediate(() => formalTrack.Source.PersistentMuted = !active, "Track Active");
        }

        public void AddClipAt(string trackAuthoringId, int frame)
        {
            if (m_Tracks.TryGetValue(trackAuthoringId ?? string.Empty, out BtsmtlTimelineTrackBinding track))
            {
                AddClip(track, frame);
                return;
            }
            ReportIssue("Add Clip 的目标 Track 已失效。");
        }

        public void DeleteTrack(IEmbeddedTimelineTrackBinding track)
        {
            if (IsReadOnly || !(track is BtsmtlTimelineTrackBinding formalTrack))
                return;
            if (formalTrack.IsLocked)
            {
                ReportIssue("当前 Track 已锁定，不能删除。");
                return;
            }
            ApplyImmediate(() => Timeline.RemoveTrack(formalTrack.Source), "Delete Timeline Track");
        }

        public void DeleteClip(IEmbeddedTimelineClipBinding clip)
        {
            if (IsReadOnly || !(clip is BtsmtlTimelineClipBinding formalClip))
                return;
            if (formalClip.IsLocked)
            {
                ReportIssue("当前 Clip 或所属 Track 已锁定，不能删除。");
                return;
            }
            ApplyImmediate(() => Timeline.RemoveClip(formalClip.Source), "Delete Timeline Clip");
        }

        public void DeleteClips(IReadOnlyList<IEmbeddedTimelineClipBinding> clips)
        {
            if (clips == null || IsReadOnly)
                return;
            for (int index = 0; index < clips.Count; index++)
            {
                if (clips[index] is BtsmtlTimelineClipBinding formalClip && formalClip.IsLocked)
                {
                    ReportIssue("选中的 Clip 或所属 Track 已锁定，不能删除。");
                    return;
                }
            }
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
            if (formalClip.IsLocked)
            {
                ReportIssue("当前 Clip 或所属 Track 已锁定，不能切分。");
                return;
            }
            if (frame <= formalClip.Source.StartFrame || frame >= formalClip.Source.EndFrame)
                return;
            if (formalClip.Source is not AnimationClip && formalClip.Source is not MotionCurveClip)
            {
                ReportIssue($"Clip kind '{formalClip.Source.ContractKind}' does not provide a formal split source operation.");
                return;
            }
            ApplyImmediate(() =>
            {
                int originalStartFrame = formalClip.Source.StartFrame;
                int originalEndFrame = formalClip.Source.EndFrame;
                Clip copy = ManagedReferenceCloneUtility.Clone(formalClip.Source);
                copy.RegenerateAuthoringIdentity();
                if (copy is ITimelineOwnedAuthoringIdentity owned)
                    owned.RegenerateOwnedAuthoringIdentity();
                copy.StartFrame = frame;
                copy.EndFrame = originalEndFrame;
                if (formalClip.Source is AnimationClip animation && copy is AnimationClip animationCopy)
                    animationCopy.ClipInFrame = animation.ClipInFrame + frame - originalStartFrame;
                if (formalClip.Source is MotionCurveClip motion && copy is MotionCurveClip motionCopy)
                {
                    float splitSourceTime = motion.SourceStartTime +
                        (frame - originalStartFrame) / (float)FrameRate;
                    float originalSourceEnd = motion.SourceEndTime;
                    motion.ConfigureSource(motion.SourceCurve, motion.SourceStartTime, splitSourceTime);
                    motionCopy.ConfigureSource(motionCopy.SourceCurve, splitSourceTime, originalSourceEnd);
                }
                formalClip.Source.EndFrame = frame;
                formalClip.Source.SelfEaseOutFrame = 0;
                copy.SelfEaseInFrame = 0;
                copy.Track = formalClip.Source.Track;
                formalClip.Source.Track.Clips.Add(copy);
                SplitClipCurves(
                    formalClip.Source,
                    copy,
                    (frame - originalStartFrame) / (float)Mathf.Max(1, originalEndFrame - originalStartFrame));
                formalClip.Source.Track.UpdateMix();
            }, "Split Timeline Clip");
        }

        public void MoveTrack(IEmbeddedTimelineTrackBinding track, int index)
        {
            if (IsReadOnly || !(track is BtsmtlTimelineTrackBinding formalTrack))
                return;
            if (formalTrack.IsLocked)
            {
                ReportIssue("当前 Track 已锁定，不能排序。");
                return;
            }
            ApplyImmediate(() =>
            {
                List<Track> tracks = Timeline.Tracks;
                tracks.Remove(formalTrack.Source);
                tracks.Insert(Mathf.Clamp(index, 0, tracks.Count), formalTrack.Source);
            }, "Reorder Timeline Track");
        }

        public void ConfigureSection(IEmbeddedTimelineSectionBinding section, string name, int frame)
        {
            if (IsReadOnly || !(section is BtsmtlTimelineSectionBinding formalSection))
                return;
            if (formalSection.IsLocked)
            {
                ReportIssue("当前 Section 已锁定，不能修改。");
                return;
            }
            formalSection.Name = name ?? string.Empty;
            formalSection.Time = Mathf.Max(0, frame) / (float)FrameRate;
        }

        public void DeleteSection(IEmbeddedTimelineSectionBinding section)
        {
            if (IsReadOnly || !(section is BtsmtlTimelineSectionBinding formalSection))
                return;
            if (formalSection.IsLocked)
            {
                ReportIssue("当前 Section 已锁定，不能删除。");
                return;
            }
            ApplyImmediate(() => Timeline.RemoveSection(formalSection.Source), "Delete Timeline Section");
        }

        public void AddSection(int frame)
        {
            if (IsReadOnly)
                return;
            ApplyImmediate(() => Timeline.AddSection("Section", Mathf.Max(0, frame)), "Add Timeline Section");
        }

        public bool CanPasteClip => m_CopiedClip != null;
        public event Action<string> AuthoringIssue;

        public void CopyClip(IEmbeddedTimelineClipBinding clip)
        {
            m_CopiedClip = clip is BtsmtlTimelineClipBinding formalClip
                ? ManagedReferenceCloneUtility.Clone(formalClip.Source)
                : null;
        }

        public void CopySourceClip(Clip clip)
        {
            m_CopiedClip = clip != null && m_Clips.TryGetValue(clip.AuthoringId, out BtsmtlTimelineClipBinding value)
                ? ManagedReferenceCloneUtility.Clone(value.Source)
                : null;
        }

        public void PasteClip(IEmbeddedTimelineTrackBinding track, int frame)
        {
            if (IsReadOnly || m_CopiedClip == null || !(track is BtsmtlTimelineTrackBinding formalTrack))
                return;
            if (formalTrack.IsLocked)
            {
                ReportIssue("当前 Track 已锁定，不能粘贴 Clip。");
                return;
            }
            ApplyImmediate(() =>
            {
                Clip clone = ManagedReferenceCloneUtility.Clone(m_CopiedClip);
                clone.RegenerateAuthoringIdentity();
                if (clone is ITimelineOwnedAuthoringIdentity owned)
                    owned.RegenerateOwnedAuthoringIdentity();
                clone.StartFrame = Mathf.Max(0, frame);
                clone.EndFrame = clone.StartFrame + Mathf.Max(1, m_CopiedClip.Duration);
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

        public bool TryGetSectionBinding(string authoringId, out IEmbeddedTimelineSectionBinding binding)
        {
            if (m_SectionsById.TryGetValue(authoringId ?? string.Empty, out BtsmtlTimelineSectionBinding value))
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

        bool IEmbeddedTimelineBinding.TryGetClip(string authoringId, out IEmbeddedTimelineClipBinding clip)
        {
            if (m_Clips.TryGetValue(authoringId ?? string.Empty, out BtsmtlTimelineClipBinding value))
            {
                clip = value;
                return true;
            }
            clip = null;
            return false;
        }

        public bool TryGetClip(string authoringId, out Clip clip)
        {
            clip = null;
            return m_Clips.TryGetValue(authoringId ?? string.Empty, out BtsmtlTimelineClipBinding binding) &&
                   (clip = binding.Source) != null;
        }

        public bool TryGetClipBinding(string authoringId, out IEmbeddedTimelineClipBinding binding)
        {
            if (m_Clips.TryGetValue(authoringId ?? string.Empty, out BtsmtlTimelineClipBinding value))
            {
                binding = value;
                return true;
            }
            binding = null;
            return false;
        }

        public void Apply(Action mutation, string undoName)
        {
            if (!IsSourceCurrent())
            {
                Rebuild();
                ReportIssue("Timeline 内容已被外部修改，当前字段修改未提交。");
                return;
            }
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
            else if (m_SectionsById.TryGetValue(selectedId, out BtsmtlTimelineSectionBinding section))
                Select(section);
            else
                Select(null);
        }

        bool ApplyImmediate(Action mutation, string undoName)
        {
            if (!IsSourceCurrent())
            {
                Rebuild();
                ReportIssue("Timeline 内容已被外部修改，当前操作未提交。");
                return false;
            }
            float previousLength = Length;
            try
            {
                m_Session.Apply(() =>
                {
                    mutation();
                    Timeline.Init();
                    ValidateTimeline();
                }, undoName);
            }
            catch (Exception exception)
            {
                Rebuild();
                ReportIssue(exception.Message);
                return false;
            }
            Rebuild();
            ExpandViewToLength(previousLength);
            return true;
        }

        void ExpandViewToLength(float previousLength)
        {
            if (Length > previousLength + 1f / FrameRate)
                m_ViewTimeMax = Mathf.Max(m_ViewTimeMax, Length);
        }

        void ValidateTimeline()
        {
            var errors = new List<string>();
            if (!Timeline.ValidateContent(ContractCatalog, errors))
                throw new InvalidOperationException(string.Join("\n", errors));
        }

        void BuildBindings()
        {
            var expandedTracks = new HashSet<string>(
                m_Tracks.Values
                    .Where(track => track.ShowCurves)
                    .Select(track => track.AuthoringId),
                StringComparer.Ordinal);
            var lockedTracks = new HashSet<string>(
                m_Tracks.Values
                    .Where(track => track.IsLocked)
                    .Select(track => track.AuthoringId),
                StringComparer.Ordinal);
            var lockedClips = new HashSet<string>(
                m_Clips.Values
                    .Where(clip => clip.IsLocked)
                    .Select(clip => clip.AuthoringId),
                StringComparer.Ordinal);
            var customHeights = m_Tracks.Values.ToDictionary(
                track => track.AuthoringId,
                track => track.CustomHeight,
                StringComparer.Ordinal);
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
                track.ShowCurves = expandedTracks.Contains(source.AuthoringId);
                track.IsLocked = lockedTracks.Contains(source.AuthoringId);
                if (customHeights.TryGetValue(source.AuthoringId, out float customHeight))
                    track.CustomHeight = customHeight;
                group.AddTrack(track);
                m_Tracks[source.AuthoringId] = track;
                for (int clipIndex = 0; clipIndex < source.Clips.Count; clipIndex++)
                {
                    Clip sourceClip = source.Clips[clipIndex];
                    if (sourceClip == null)
                        continue;
                    var clip = new BtsmtlTimelineClipBinding(this, track, sourceClip);
                    clip.IsLocked = lockedClips.Contains(sourceClip.AuthoringId);
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
            m_SourceRevision = TimelineAuthoringFingerprint.Compute(Timeline);
            m_CurrentFrame = Mathf.Clamp(m_CurrentFrame, 0, Timeline.MaxFrame);
        }

        void ShowTrackCreationPopup(string kind, IReadOnlyList<TimelineAuthoringTrackFieldAttribute> fields)
        {
            PopupWindow.Show(new Rect(m_PopupPosition, Vector2.zero), new TimelineTrackCreationPopup(
                kind,
                DisplayKind(kind),
                fields,
                (name, values) => CreateTrack(kind, name, values)));
        }

        string CreateTrack(string kind, string name, IReadOnlyDictionary<string, string> values)
        {
            if (IsReadOnly)
                return "Timeline 当前只读，不能添加 Track。";
            if (!IsSourceCurrent())
            {
                Rebuild();
                ReportIssue("Timeline 内容已被外部修改，Add Track 已取消。");
                return "Timeline 内容已被外部修改，Add Track 已取消。";
            }
            try
            {
                Track added = null;
                if (!ApplyImmediate(() =>
                {
                    Type trackType = TimelineAuthoringTypeCatalog.RequireTrackType(kind);
                    int count = Timeline.Tracks.Count;
                    Timeline.AddTrack(trackType, ContractCatalog);
                    added = Timeline.Tracks[count];
                    added.Name = string.IsNullOrWhiteSpace(name) ? DisplayKind(kind) : name.Trim();
                    TimelineAuthoringTrackBinding.Apply(added, values);
                }, "Add Timeline Track"))
                    return "正式 Timeline Track 提交失败。";
                if (added != null && m_Tracks.TryGetValue(added.AuthoringId, out BtsmtlTimelineTrackBinding addedBinding))
                    Select(addedBinding);
                return string.Empty;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ReportIssue(exception.Message);
                return exception.Message;
            }
        }

        void ShowAddClipMenu(string trackAuthoringId, int frame)
        {
            if (!m_Tracks.TryGetValue(trackAuthoringId ?? string.Empty, out BtsmtlTimelineTrackBinding track) ||
                !ContractCatalog.TryGetTrack(track.Source.ContractKind, out TimelineTrackContract contract))
            {
                ReportIssue("Add Clip 找不到正式 Track contract。");
                return;
            }
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
            PopupWindow.Show(new Rect(m_PopupPosition, Vector2.zero), new TimelineClipCreationPopup(
                request,
                motionClipIds,
                Timeline.ExternalBindings,
                CreateClip));
        }

        void CapturePopupPosition()
        {
            Event currentEvent = Event.current;
            if (currentEvent != null)
                m_PopupPosition = GUIUtility.GUIToScreenPoint(currentEvent.mousePosition);
        }

        string CreateClip(TimelineClipCreationRequest request)
        {
            if (IsReadOnly)
                return "Timeline 当前只读，不能添加 Clip。";
            if (!IsSourceCurrent())
            {
                Rebuild();
                ReportIssue("Timeline 内容已被外部修改，Add Clip 已取消。");
                return "Timeline 内容已被外部修改，Add Clip 已取消。";
            }
            if (!m_Tracks.TryGetValue(request.TrackAuthoringId, out BtsmtlTimelineTrackBinding track))
            {
                ReportIssue("Add Clip 的目标 Track 已失效。");
                return "Add Clip 的目标 Track 已失效。";
            }
            if (track.IsLocked)
            {
                ReportIssue("Add Clip 的目标 Track 已锁定。");
                return "Add Clip 的目标 Track 已锁定。";
            }
            try
            {
                Clip added = null;
                if (!ApplyImmediate(() =>
                {
                    UnityEngine.Object treeGraph = null;
                    if (request.Kind == TimelineContractKinds.TreeClip)
                    {
                        treeGraph = request.TreeGraph;
                        if (treeGraph == null)
                            treeGraph = TreeClipGraphCreation.CreateSubAsset(m_Request.SerializedOwner, request.NewTreeGraphName);
                    }
                    added = request.Kind == TimelineContractKinds.AnimationClip
                        ? TimelineAuthoringTrackBinding.CreateClip(Timeline, ContractCatalog, track.Source, request.Resource as UnityEngine.AnimationClip, request.StartFrame)
                        : request.Kind == TimelineContractKinds.MotionCurveClip
                            ? Timeline.AddClip(ContractCatalog, request.SourceCurve, track.Source, request.StartFrame)
                        : treeGraph != null
                            ? Timeline.AddClip(ContractCatalog, treeGraph, track.Source, request.StartFrame)
                        : request.Resource != null
                            ? Timeline.AddClip(ContractCatalog, request.Resource, track.Source, request.StartFrame)
                            : Timeline.AddClip(ContractCatalog, track.Source, request.StartFrame);
                    added.EndFrame = Mathf.Max(request.StartFrame + 1, request.EndFrame);
                    TimelineAuthoringClipBinding.Configure(Timeline, added, ReadConfiguration(added, request), this);
                    added.Track.UpdateMix();
                }, "Add Timeline Clip"))
                    return "正式 Timeline Clip 提交失败。";
                if (added != null && m_Clips.TryGetValue(added.AuthoringId, out BtsmtlTimelineClipBinding addedBinding))
                    Select(addedBinding);
                return string.Empty;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ReportIssue(exception.Message);
                return exception.Message;
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

        static void SplitClipCurves(Clip first, Clip second, float split)
        {
            if (first?.Track == null || second == null)
                return;
            var descriptors = new List<TimelineCurveChannelDescriptor>();
            TimelineCurveChannelCatalog.CollectForTrack(first.Track, descriptors);
            float normalizedSplit = Mathf.Clamp(split, 0.0001f, 0.9999f);
            for (int index = 0; index < descriptors.Count; index++)
            {
                TimelineCurveChannelDescriptor descriptor = descriptors[index];
                if (!descriptor.Supports(first) || !descriptor.Supports(second))
                    continue;
                AnimationCurve source = descriptor.Read(first);
                descriptor.Replace(first, SplitNormalizedCurve(source, 0f, normalizedSplit));
                descriptor.Replace(second, SplitNormalizedCurve(source, normalizedSplit, 1f));
            }
        }

        static AnimationCurve SplitNormalizedCurve(AnimationCurve source, float start, float end)
        {
            if (source == null)
                return new AnimationCurve();
            float duration = Mathf.Max(0.0001f, end - start);
            var keys = new List<Keyframe>();
            Keyframe[] sourceKeys = source.keys;
            bool hasStart = false;
            bool hasEnd = false;
            for (int index = 0; index < sourceKeys.Length; index++)
            {
                Keyframe key = sourceKeys[index];
                if (key.time < start - 0.0001f || key.time > end + 0.0001f)
                    continue;
                key.time = Mathf.Clamp01((key.time - start) / duration);
                key.inTangent *= duration;
                key.outTangent *= duration;
                hasStart |= Mathf.Abs(key.time) <= 0.0001f;
                hasEnd |= Mathf.Abs(key.time - 1f) <= 0.0001f;
                keys.Add(key);
            }
            if (!hasStart)
                keys.Add(CreateBoundaryKey(source, start, 0f, duration));
            if (!hasEnd)
                keys.Add(CreateBoundaryKey(source, end, 1f, duration));
            keys.Sort((left, right) => left.time.CompareTo(right.time));
            var result = new AnimationCurve(keys.ToArray())
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
            return result;
        }

        static Keyframe CreateBoundaryKey(AnimationCurve source, float sourceTime, float normalizedTime, float duration)
        {
            float tangent = BoundaryTangent(source, sourceTime) * duration;
            return new Keyframe(normalizedTime, source.Evaluate(sourceTime), tangent, tangent);
        }

        static float BoundaryTangent(AnimationCurve source, float sourceTime)
        {
            Keyframe[] keys = source.keys;
            if (keys.Length == 0)
                return 0f;
            if (sourceTime <= keys[0].time)
                return keys[0].outTangent;
            if (sourceTime >= keys[keys.Length - 1].time)
                return keys[keys.Length - 1].inTangent;
            for (int index = 1; index < keys.Length; index++)
                if (sourceTime <= keys[index].time)
                    return keys[index - 1].outTangent;
            return 0f;
        }

        static string DisplayKind(string kind)
        {
            int separator = kind.LastIndexOf('.');
            return (separator >= 0 ? kind.Substring(0, separator) : kind).Replace('-', ' ');
        }

        void ReportIssue(string message)
        {
            AuthoringIssue?.Invoke(message ?? string.Empty);
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
            float m_CustomHeight;
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
            public float CustomHeight
            {
                get => m_CustomHeight > 0f
                    ? m_CustomHeight
                    : ShowCurves
                        ? GetNaturalExpandedHeight(string.Empty)
                        : 0f;
                set => m_CustomHeight = Mathf.Clamp(value, DefaultHeight + 32f, 600f);
            }
            public Color Color => Source.Color();
            public float StartTime => 0f;
            public float EndTime => Owner.Length;
            public float DefaultHeight => 32f;
            public float FinalHeight => GetFinalHeight(string.Empty);
            public float GetFinalHeight(string inspectedParameterId)
            {
                float naturalHeight = GetNaturalExpandedHeight(inspectedParameterId);
                if (!ShowCurves || string.IsNullOrEmpty(inspectedParameterId))
                    return naturalHeight;
                return Mathf.Max(naturalHeight, m_CustomHeight);
            }

            float GetNaturalExpandedHeight(string inspectedParameterId)
            {
                if (!ShowCurves)
                    return DefaultHeight;
                IEmbeddedTimelineClipBinding clip = SelectedClip;
                if (clip == null || clip.Parameters == null || clip.Parameters.Count == 0)
                    return DefaultHeight + 50f;
                bool hasInspectedParameter = false;
                for (int index = 0; index < clip.Parameters.Count; index++)
                {
                    if (string.Equals(clip.Parameters[index].ParameterId, inspectedParameterId, StringComparison.Ordinal))
                    {
                        hasInspectedParameter = true;
                        break;
                    }
                }
                float parameterHeight = DefaultHeight + 10f + clip.Parameters.Count * 20f;
                if (!hasInspectedParameter)
                    return Mathf.Max(parameterHeight, DefaultHeight + 50f);
                return parameterHeight + 65f;
            }
            public IReadOnlyList<IEmbeddedTimelineClipBinding> Clips => m_Clips;
            public IEmbeddedTimelineClipBinding SelectedClip =>
                Owner.Selected is BtsmtlTimelineClipBinding selected &&
                ReferenceEquals(selected.Track, this)
                    ? selected
                    : null;
            public void AddClip(BtsmtlTimelineClipBinding clip) => m_Clips.Add(clip);
        }

        sealed class BtsmtlTimelineClipBinding : IEmbeddedTimelineClipBinding, IEmbeddedTimelineSourceRangeBinding
        {
            readonly List<IEmbeddedTimelineCurveBinding> m_Curves = new List<IEmbeddedTimelineCurveBinding>();
            readonly List<IEmbeddedTimelineParameterBinding> m_Parameters = new List<IEmbeddedTimelineParameterBinding>();
            readonly BtsmtlSlateTimelineBinding m_Owner;
            readonly BtsmtlTimelineTrackBinding m_Track;
            float m_StartTime;
            float m_EndTime;
            float m_BlendIn;
            float m_BlendOut;
            int m_ClipInFrame;
            float m_SourceStartTime;
            float m_SourceEndTime;
            bool m_IsCollapsed;
            bool m_IsLocked;

            public BtsmtlTimelineClipBinding(BtsmtlSlateTimelineBinding owner, BtsmtlTimelineTrackBinding track, Clip source)
            {
                m_Owner = owner;
                m_Track = track;
                Source = source;
                m_StartTime = source.StartFrame / (float)owner.FrameRate;
                m_EndTime = source.EndFrame / (float)owner.FrameRate;
                m_BlendIn = source.SelfEaseInFrame / (float)owner.FrameRate;
                m_BlendOut = source.SelfEaseOutFrame / (float)owner.FrameRate;
                m_ClipInFrame = source.ClipInFrame;
                if (source is MotionCurveClip motion)
                {
                    m_SourceStartTime = motion.SourceStartTime;
                    m_SourceEndTime = motion.SourceEndTime;
                }
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
                if (source is MotionCurveClip sourceMotion && sourceMotion.SourceCurve != null)
                {
                    AddReferenceParameter(sourceMotion, "source.position-x", "Position X", sourceMotion.SourcePositionX);
                    AddReferenceParameter(sourceMotion, "source.position-y", "Position Y", sourceMotion.SourcePositionY);
                    AddReferenceParameter(sourceMotion, "source.position-z", "Position Z", sourceMotion.SourcePositionZ);
                    AddReferenceParameter(sourceMotion, "source.yaw", "Yaw", sourceMotion.SourceYaw);
                }
            }

            public Clip Source { get; }
            internal BtsmtlSlateTimelineBinding Owner => m_Owner;
            public BtsmtlTimelineTrackBinding Track => m_Track;
            IEmbeddedTimelineTrackBinding IEmbeddedTimelineClipBinding.Track => m_Track;
            public string AuthoringId => Source.AuthoringId;
            public string DisplayName => Source.Name;
            public string Info => Source is MotionCurveClip motion && motion.SourceCurve != null
                ? string.Concat(Source.Name, "  [Ref: ", motion.SourceCurve.name, "]")
                : Source.Name;
            public bool IsActive => m_Track.IsActive;
            public bool IsValid => !Source.Invalid;
            public bool IsCollapsed { get => m_IsCollapsed; set => m_IsCollapsed = value; }
            public bool IsLocked { get => m_IsLocked || m_Track.IsLocked; set => m_IsLocked = value; }
            public float StartTime
            {
                get => m_StartTime;
                set
                {
                    float nextStart = SnapTime(value);
                    float delta = nextStart - m_StartTime;
                    m_StartTime = nextStart;
                    m_EndTime = Mathf.Max(m_StartTime + 1f / m_Owner.FrameRate, m_EndTime + delta);
                    RefreshReferenceCurves();
                }
            }
            public float EndTime
            {
                get => m_EndTime;
                set
                {
                    m_EndTime = Mathf.Max(StartTime + 1f / m_Owner.FrameRate, SnapTime(value));
                    RefreshReferenceCurves();
                }
            }
            public float Length => Mathf.Max(0f, EndTime - StartTime);
            public float BlendIn
            {
                get => Mathf.Clamp(m_BlendIn, 0f, Length);
                set => m_BlendIn = Mathf.Clamp(SnapTime(value), 0f, Length);
            }
            public float BlendOut
            {
                get => Mathf.Clamp(m_BlendOut, 0f, Length);
                set => m_BlendOut = Mathf.Clamp(SnapTime(value), 0f, Length);
            }
            public bool CanScale => Source.IsResizable();
            public bool CanClipIn => Source.IsClipInable();
            public int ClipInFrame
            {
                get => m_ClipInFrame;
                set => m_ClipInFrame = Mathf.Max(0, value);
            }
            public bool CanBlendIn => Source.IsMixable();
            public bool CanBlendOut => Source.IsMixable();
            public IReadOnlyList<IEmbeddedTimelineParameterBinding> Parameters => m_Parameters;
            public IReadOnlyList<IEmbeddedTimelineCurveBinding> Curves => m_Curves;

            public void AdjustSourceRange(
                int originalStartFrame,
                int originalEndFrame,
                int currentStartFrame,
                int currentEndFrame,
                bool trimStart)
            {
                if (Source is not MotionCurveClip motion || motion.SourceCurve == null)
                    return;
                float duration = Mathf.Max(1f / m_Owner.FrameRate, (currentEndFrame - currentStartFrame) / (float)m_Owner.FrameRate);
                if (trimStart)
                    m_SourceStartTime = Mathf.Clamp(
                        motion.SourceStartTime + (currentStartFrame - originalStartFrame) / (float)m_Owner.FrameRate,
                        0f,
                        motion.SourceEndTime - 1f / m_Owner.FrameRate);
                m_SourceEndTime = Mathf.Clamp(
                    m_SourceStartTime + duration,
                    m_SourceStartTime + 1f / m_Owner.FrameRate,
                    motion.SourceCurve.Duration);
                RefreshReferenceCurves();
            }

            public bool CanCrossBlend(IEmbeddedTimelineClipBinding other)
            {
                return other is BtsmtlTimelineClipBinding formal && formal.Source.GetType() == Source.GetType() && Source.IsMixable();
            }

            public void AddIdentityKey(float time)
            {
                for (int index = 0; index < m_Curves.Count; index++)
                {
                    AnimationCurve curve = m_Curves[index].Curve;
                    BtsmtlTimelineCurveBinding binding = (BtsmtlTimelineCurveBinding)m_Curves[index];
                    float snappedTime = binding.SnapTime(time);
                    curve.AddKey(snappedTime, curve.Evaluate(snappedTime));
                }
            }

            void AddReferenceParameter(MotionCurveClip motion, string channelId, string displayName, AnimationCurve source)
            {
                m_Parameters.Add(new BtsmtlTimelineReferenceParameterBinding(
                    this,
                    channelId,
                    displayName,
                    motion.SourceCurve.name,
                    () => motion.CreateSourceDisplayCurve(source, m_SourceStartTime, m_SourceEndTime, Length)));
            }

            void RefreshReferenceCurves()
            {
                for (int index = 0; index < m_Parameters.Count; index++)
                    if (m_Parameters[index] is BtsmtlTimelineReferenceParameterBinding reference)
                        reference.Refresh();
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

            public bool CommitSource()
            {
                int startFrame = Mathf.Max(0, Mathf.RoundToInt(StartTime * m_Owner.FrameRate));
                int endFrame = Mathf.Max(startFrame + 1, Mathf.RoundToInt(EndTime * m_Owner.FrameRate));
                int selfEaseInFrame = Mathf.Clamp(Mathf.RoundToInt(BlendIn * m_Owner.FrameRate), 0, endFrame - startFrame - 1);
                int selfEaseOutFrame = Mathf.Clamp(Mathf.RoundToInt(BlendOut * m_Owner.FrameRate), 0, endFrame - startFrame - selfEaseInFrame - 1);
                bool changed = Source.StartFrame != startFrame ||
                               Source.EndFrame != endFrame ||
                               Source.SelfEaseInFrame != selfEaseInFrame ||
                               Source.SelfEaseOutFrame != selfEaseOutFrame ||
                               (CanClipIn && Source.ClipInFrame != m_ClipInFrame);
                if (Source is MotionCurveClip motion &&
                    (!Mathf.Approximately(motion.SourceStartTime, m_SourceStartTime) ||
                     !Mathf.Approximately(motion.SourceEndTime, m_SourceEndTime)))
                    changed = true;
                for (int index = 0; index < m_Curves.Count; index++)
                    changed |= ((BtsmtlTimelineCurveBinding)m_Curves[index]).CommitSource();
                if (!changed)
                    return false;
                Source.StartFrame = startFrame;
                Source.EndFrame = endFrame;
                Source.SelfEaseInFrame = selfEaseInFrame;
                Source.SelfEaseOutFrame = selfEaseOutFrame;
                if (CanClipIn)
                    Source.ClipInFrame = m_ClipInFrame;
                if (Source is MotionCurveClip sourceMotion &&
                    (!Mathf.Approximately(sourceMotion.SourceStartTime, m_SourceStartTime) ||
                     !Mathf.Approximately(sourceMotion.SourceEndTime, m_SourceEndTime)))
                    sourceMotion.ConfigureSource(sourceMotion.SourceCurve, m_SourceStartTime, m_SourceEndTime);
                Source.Track.UpdateMix();
                return true;
            }

            public bool HasChanges()
            {
                int startFrame = Mathf.Max(0, Mathf.RoundToInt(StartTime * m_Owner.FrameRate));
                int endFrame = Mathf.Max(startFrame + 1, Mathf.RoundToInt(EndTime * m_Owner.FrameRate));
                int selfEaseInFrame = Mathf.Clamp(Mathf.RoundToInt(BlendIn * m_Owner.FrameRate), 0, endFrame - startFrame - 1);
                int selfEaseOutFrame = Mathf.Clamp(Mathf.RoundToInt(BlendOut * m_Owner.FrameRate), 0, endFrame - startFrame - selfEaseInFrame - 1);
                if (Source.StartFrame != startFrame || Source.EndFrame != endFrame ||
                    Source.SelfEaseInFrame != selfEaseInFrame || Source.SelfEaseOutFrame != selfEaseOutFrame ||
                    (CanClipIn && Source.ClipInFrame != m_ClipInFrame))
                    return true;
                if (Source is MotionCurveClip motion &&
                    (!Mathf.Approximately(motion.SourceStartTime, m_SourceStartTime) ||
                     !Mathf.Approximately(motion.SourceEndTime, m_SourceEndTime)))
                    return true;
                for (int index = 0; index < m_Curves.Count; index++)
                    if (((BtsmtlTimelineCurveBinding)m_Curves[index]).HasChanges())
                        return true;
                return false;
            }

            float SnapTime(float value)
            {
                int frame = Mathf.Max(0, Mathf.RoundToInt(value * m_Owner.FrameRate));
                return frame / (float)m_Owner.FrameRate;
            }
        }

        sealed class BtsmtlTimelineCurveBinding : IEmbeddedTimelineCurveBinding
        {
            readonly BtsmtlTimelineClipBinding m_Clip;
            readonly TimelineCurveChannelDescriptor m_Descriptor;
            AnimationCurve m_Curve;

            public BtsmtlTimelineCurveBinding(BtsmtlTimelineClipBinding clip, TimelineCurveChannelDescriptor descriptor)
            {
                m_Clip = clip;
                m_Descriptor = descriptor;
                m_Curve = ConvertCurveTime(
                    descriptor.Read(clip.Source),
                    CurveDuration(clip.Source, descriptor, clip.Owner.FrameRate),
                    false);
            }
            public string ChannelId => m_Descriptor.ChannelId.Value;
            public string DisplayName => m_Descriptor.DisplayName;
            public TimelineCurveValueDomain ValueDomain => m_Descriptor.ValueDomain;
            public AnimationCurve Curve => m_Curve;
            public int StartFrame => Mathf.RoundToInt(m_Clip.StartTime * m_Clip.Owner.FrameRate);
            public int EndFrame => Mathf.RoundToInt(m_Clip.EndTime * m_Clip.Owner.FrameRate);
            public float Duration => Mathf.Max(1f / m_Clip.Owner.FrameRate, m_Clip.Length);
            public void Replace(AnimationCurve curve) => m_Curve = SnapCurve(curve);
            public void Trim(float min, float max)
            {
                for (int index = m_Curve.length - 1; index >= 0; index--)
                    if (m_Curve[index].time < min || m_Curve[index].time > max)
                        m_Curve.RemoveKey(index);
            }
            public bool CommitSource()
            {
                AnimationCurve source = m_Descriptor.Read(m_Clip.Source);
                AnimationCurve converted = ConvertCurveTime(m_Curve, Duration, true);
                if (TimelineCurveAuthoring.AreEquivalent(source, converted))
                    return false;
                m_Descriptor.Replace(m_Clip.Source, converted);
                return true;
            }

            public bool HasChanges()
            {
                AnimationCurve source = m_Descriptor.Read(m_Clip.Source);
                AnimationCurve converted = ConvertCurveTime(m_Curve, Duration, true);
                return !TimelineCurveAuthoring.AreEquivalent(source, converted);
            }

            public float SnapTime(float time)
            {
                int frame = Mathf.Clamp(
                    Mathf.RoundToInt(time * m_Clip.Owner.FrameRate),
                    0,
                    Mathf.RoundToInt(Duration * m_Clip.Owner.FrameRate));
                return frame / (float)m_Clip.Owner.FrameRate;
            }

            AnimationCurve SnapCurve(AnimationCurve source)
            {
                AnimationCurve result = TimelineCurveAuthoring.CopyCurve(source);
                Keyframe[] keys = result.keys;
                for (int index = 0; index < keys.Length; index++)
                {
                    Keyframe key = keys[index];
                    key.time = SnapTime(key.time);
                    keys[index] = key;
                }
                result.keys = keys;
                return result;
            }

            static float CurveDuration(Clip clip, TimelineCurveChannelDescriptor descriptor, int frameRate) =>
                Mathf.Max(1f / frameRate, clip.Duration / (float)frameRate);

            static AnimationCurve ConvertCurveTime(AnimationCurve source, float duration, bool toNormalized)
            {
                AnimationCurve result = TimelineCurveAuthoring.CopyCurve(source);
                float safeDuration = Mathf.Max(0.0001f, duration);
                if (toNormalized)
                    result = ClipCurveToDuration(result, safeDuration);
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

            static AnimationCurve ClipCurveToDuration(AnimationCurve source, float duration)
            {
                var keys = new List<Keyframe>();
                Keyframe[] sourceKeys = source.keys;
                for (int index = 0; index < sourceKeys.Length; index++)
                {
                    Keyframe key = sourceKeys[index];
                    if (key.time < -0.0001f || key.time > duration + 0.0001f)
                        continue;
                    key.time = Mathf.Clamp(key.time, 0f, duration);
                    keys.Add(key);
                }
                AddBoundaryKey(source, keys, 0f, duration);
                AddBoundaryKey(source, keys, duration, duration);
                keys.Sort((left, right) => left.time.CompareTo(right.time));
                return new AnimationCurve(keys.ToArray())
                {
                    preWrapMode = source.preWrapMode,
                    postWrapMode = source.postWrapMode
                };
            }

            static void AddBoundaryKey(AnimationCurve source, List<Keyframe> keys, float time, float duration)
            {
                for (int index = 0; index < keys.Count; index++)
                    if (Mathf.Abs(keys[index].time - time) <= 0.0001f)
                        return;
                float tangent = BoundaryTangent(source, time);
                keys.Add(new Keyframe(time, source.Evaluate(time), tangent, tangent));
            }

            static float BoundaryTangent(AnimationCurve source, float time)
            {
                Keyframe[] keys = source.keys;
                if (keys.Length == 0)
                    return 0f;
                if (time <= keys[0].time)
                    return keys[0].outTangent;
                if (time >= keys[keys.Length - 1].time)
                    return keys[keys.Length - 1].inTangent;
                for (int index = 1; index < keys.Length; index++)
                    if (time <= keys[index].time)
                        return keys[index - 1].outTangent;
                return 0f;
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
            public string ValueDomainSummary => m_Curve.ValueDomain.Summary;
            public bool Enabled { get; set; } = true;
            public float CurrentValue => m_Curve.Curve.Evaluate(Mathf.Clamp(
                m_Clip.Owner.CurrentFrame / (float)m_Clip.Owner.FrameRate - m_Clip.StartTime,
                0f,
                m_Curve.Duration));
            public IReadOnlyList<IEmbeddedTimelineCurveBinding> Curves => new[] { m_Curve };
            public void AddKey(float localTime)
            {
                float snappedTime = m_Curve.SnapTime(localTime);
                m_Curve.Curve.AddKey(snappedTime, m_Curve.Curve.Evaluate(snappedTime));
            }
            public void RemoveKey(float localTime)
            {
                float snappedTime = m_Curve.SnapTime(localTime);
                for (int index = m_Curve.Curve.length - 1; index >= 0; index--)
                    if (Mathf.Abs(m_Curve.Curve[index].time - snappedTime) <= 0.0001f)
                        m_Curve.Curve.RemoveKey(index);
            }
            public void SelectPreviousKey(float localTime)
            {
                float? previous = null;
                Keyframe[] keys = m_Curve.Curve.keys;
                for (int index = 0; index < keys.Length; index++)
                    if (keys[index].time < localTime - 0.0001f && (!previous.HasValue || keys[index].time > previous.Value))
                        previous = keys[index].time;
                if (previous.HasValue)
                    m_Clip.Owner.CurrentFrame = Mathf.RoundToInt(
                        (m_Clip.StartTime + previous.Value) * m_Clip.Owner.FrameRate);
            }

            public void SelectNextKey(float localTime)
            {
                float? next = null;
                Keyframe[] keys = m_Curve.Curve.keys;
                for (int index = 0; index < keys.Length; index++)
                    if (keys[index].time > localTime + 0.0001f && (!next.HasValue || keys[index].time < next.Value))
                        next = keys[index].time;
                if (next.HasValue)
                    m_Clip.Owner.CurrentFrame = Mathf.RoundToInt(
                        (m_Clip.StartTime + next.Value) * m_Clip.Owner.FrameRate);
            }
        }

        sealed class BtsmtlTimelineReferenceParameterBinding : IEmbeddedTimelineParameterBinding, IEmbeddedTimelineReferenceParameterBinding
        {
            readonly BtsmtlTimelineClipBinding m_Clip;
            readonly BtsmtlTimelineReferenceCurveBinding m_Curve;

            public BtsmtlTimelineReferenceParameterBinding(
                BtsmtlTimelineClipBinding clip,
                string channelId,
                string displayName,
                string referenceLabel,
                Func<AnimationCurve> createCurve)
            {
                m_Clip = clip;
                m_Curve = new BtsmtlTimelineReferenceCurveBinding(clip, channelId, displayName, createCurve);
                ReferenceLabel = referenceLabel ?? string.Empty;
            }

            public string ParameterId => m_Curve.ChannelId;
            public string DisplayName => m_Curve.DisplayName;
            public string ValueDomainSummary => "Source Reference";
            public string ReferenceLabel { get; }
            public bool Enabled { get => true; set { } }
            public float CurrentValue => m_Curve.Curve.Evaluate(Mathf.Clamp(
                m_Clip.Owner.CurrentFrame / (float)m_Clip.Owner.FrameRate - m_Clip.StartTime,
                0f,
                m_Curve.Duration));
            public IReadOnlyList<IEmbeddedTimelineCurveBinding> Curves => new[] { m_Curve };
            public void AddKey(float localTime) { }
            public void RemoveKey(float localTime) { }
            public void SelectPreviousKey(float localTime) { }
            public void SelectNextKey(float localTime) { }
            public void Refresh() => m_Curve.Refresh();
        }

        sealed class BtsmtlTimelineReferenceCurveBinding : IEmbeddedTimelineCurveBinding
        {
            readonly BtsmtlTimelineClipBinding m_Clip;
            readonly Func<AnimationCurve> m_CreateCurve;
            AnimationCurve m_Curve;

            public BtsmtlTimelineReferenceCurveBinding(
                BtsmtlTimelineClipBinding clip,
                string channelId,
                string displayName,
                Func<AnimationCurve> createCurve)
            {
                m_Clip = clip;
                ChannelId = channelId;
                DisplayName = displayName;
                m_CreateCurve = createCurve ?? throw new ArgumentNullException(nameof(createCurve));
                m_Curve = m_CreateCurve();
            }

            public string ChannelId { get; }
            public string DisplayName { get; }
            public AnimationCurve Curve => m_Curve;
            public int StartFrame => Mathf.RoundToInt(m_Clip.StartTime * m_Clip.Owner.FrameRate);
            public int EndFrame => Mathf.RoundToInt(m_Clip.EndTime * m_Clip.Owner.FrameRate);
            public float Duration => Mathf.Max(1f / m_Clip.Owner.FrameRate, m_Clip.Length);
            public void Replace(AnimationCurve curve) { }
            public void Refresh() => m_Curve = m_CreateCurve();
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
            public bool CommitSource()
            {
                int frame = Mathf.Max(0, Mathf.RoundToInt(Time * m_Owner.FrameRate));
                if (string.Equals(Source.Name, Name, StringComparison.Ordinal) && Source.Frame == frame)
                    return false;
                Source.Configure(Name, frame);
                return true;
            }

            public bool HasChanges()
            {
                int frame = Mathf.Max(0, Mathf.RoundToInt(Time * m_Owner.FrameRate));
                return !string.Equals(Source.Name, Name, StringComparison.Ordinal) || Source.Frame != frame;
            }
        }
    }
}
#endif
