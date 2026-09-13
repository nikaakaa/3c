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
    [Serializable]
    public struct BtsmtlSlateTimelineViewState
    {
        [SerializeField] float m_ViewTimeMin;
        [SerializeField] float m_ViewTimeMax;
        [SerializeField] float m_CurrentTime;
        [SerializeField] Vector2 m_ScrollPosition;
        [SerializeField] string m_TrackAuthoringId;
        [SerializeField] string m_ClipAuthoringId;
        [SerializeField] bool m_GroupCollapsed;

        public BtsmtlSlateTimelineViewState(
            float viewTimeMin,
            float viewTimeMax,
            float currentTime,
            Vector2 scrollPosition,
            string trackAuthoringId,
            string clipAuthoringId,
            bool groupCollapsed)
        {
            m_ViewTimeMin = viewTimeMin;
            m_ViewTimeMax = viewTimeMax;
            m_CurrentTime = currentTime;
            m_ScrollPosition = scrollPosition;
            m_TrackAuthoringId = trackAuthoringId ?? string.Empty;
            m_ClipAuthoringId = clipAuthoringId ?? string.Empty;
            m_GroupCollapsed = groupCollapsed;
        }

        public float ViewTimeMin => m_ViewTimeMin;
        public float ViewTimeMax => m_ViewTimeMax;
        public float CurrentTime => m_CurrentTime;
        public Vector2 ScrollPosition => m_ScrollPosition;
        public string TrackAuthoringId => m_TrackAuthoringId ?? string.Empty;
        public string ClipAuthoringId => m_ClipAuthoringId ?? string.Empty;
        public bool GroupCollapsed => m_GroupCollapsed;
        public bool HasSelection => !string.IsNullOrEmpty(TrackAuthoringId) || !string.IsNullOrEmpty(ClipAuthoringId);
    }

    public sealed class BtsmtlSlateTimelineEditorAdapter :
        IDisposable,
        ISlateTimelineEditorCommandPort,
        ISlateTimelineEditorHost,
        ITimelineAuthoringClipResolver
    {
        readonly TimelineEditorOpenRequest m_Request;
        readonly TimelineEditorSessionContext m_Session;
        readonly SlateTimelineEditorSurface m_Surface = new SlateTimelineEditorSurface();
        readonly Action<Clip> m_OpenSourceClip;
        readonly Action m_RequestRepaint;
        readonly Dictionary<string, Track> m_SourceTracks = new Dictionary<string, Track>(StringComparer.Ordinal);
        readonly Dictionary<string, Clip> m_SourceClips = new Dictionary<string, Clip>(StringComparer.Ordinal);
        readonly HashSet<string> m_ActiveTrackIds = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> m_ActiveClipIds = new HashSet<string>(StringComparer.Ordinal);
        string m_SourceRevision;
        int m_CurrentFrame;
        int m_RuntimeFrame = -1;
        bool m_GroupCollapsed;
        bool m_ReadOnly = false;
        bool m_Disposed;

        public BtsmtlSlateTimelineEditorAdapter(
            TimelineEditorOpenRequest request,
            Action<Clip> openSourceClip,
            Action requestRepaint)
        {
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
            m_OpenSourceClip = openSourceClip;
            m_RequestRepaint = requestRepaint ?? throw new ArgumentNullException(nameof(requestRepaint));
            m_Session = new TimelineEditorSessionContext(request);
            m_Session.BindReadOnly(() => m_ReadOnly);
            m_Request.Timeline.OnValueChanged += OnSourceTimelineChanged;
            m_Surface.Bind(BuildContent(), this, this);
        }

        public TimelineEditorSelection Selection => m_Session.Selection;
        public event Action<TimelineEditorSelection> SelectionChanged;
        public event Action<string> AuthoringIssue;

        public static BtsmtlSlateTimelineEditorAdapter Open(
            TimelineEditorOpenRequest request,
            Action<Clip> openSourceClip,
            Action requestRepaint)
        {
            return new BtsmtlSlateTimelineEditorAdapter(request, openSourceClip, requestRepaint);
        }

        public static bool TryOpen(
            TimelineEditorOpenRequest request,
            Action<Clip> openSourceClip,
            Action requestRepaint,
            out BtsmtlSlateTimelineEditorAdapter adapter,
            out string unavailableReason)
        {
            try
            {
                adapter = Open(request, openSourceClip, requestRepaint);
                unavailableReason = string.Empty;
                return true;
            }
            catch (Exception exception) when (
                exception is TypeLoadException ||
                exception is MissingMethodException ||
                exception is InvalidOperationException)
            {
                adapter = null;
                unavailableReason = exception.Message;
                return false;
            }
        }

        public void DrawEmbeddedGUI(float width, float height)
        {
            if (!m_Disposed)
                m_Surface.DrawGUI(width, height);
        }

        public void ApplyFormalMutation(Action mutation, string undoName)
        {
            if (m_ReadOnly)
            {
                ReportIssue("Timeline 当前只读，不能修改正式字段。");
                return;
            }
            try
            {
                m_Session.Apply(() =>
                {
                    mutation?.Invoke();
                    m_Request.Timeline.Init();
                }, undoName);
                RebuildContent();
            }
            catch (Exception exception)
            {
                ReportIssue($"字段修改失败：{exception.Message}");
            }
        }

        public BtsmtlSlateTimelineViewState CaptureViewState()
        {
            return new BtsmtlSlateTimelineViewState(
                m_Surface.ViewStartFrame / (float)m_Session.FrameRate,
                m_Surface.ViewEndFrame / (float)m_Session.FrameRate,
                m_CurrentFrame / (float)m_Session.FrameRate,
                m_Surface.ScrollPosition,
                Selection.Track?.AuthoringId,
                Selection.Clip?.AuthoringId,
                m_GroupCollapsed);
        }

        public void RestoreViewState(BtsmtlSlateTimelineViewState state)
        {
            m_CurrentFrame = Mathf.Clamp(
                Mathf.RoundToInt(state.CurrentTime * m_Session.FrameRate),
                0,
                m_Request.Timeline.MaxFrame);
            m_GroupCollapsed = state.GroupCollapsed;
            m_Surface.SetView(
                Mathf.RoundToInt(state.ViewTimeMin * m_Session.FrameRate),
                Mathf.RoundToInt(state.ViewTimeMax * m_Session.FrameRate),
                state.ScrollPosition);
            FocusSource(state.TrackAuthoringId, state.ClipAuthoringId);
            RebuildContent();
        }

        public bool FocusSource(string trackAuthoringId, string clipAuthoringId)
        {
            if (!string.IsNullOrEmpty(clipAuthoringId) && m_SourceClips.TryGetValue(clipAuthoringId, out Clip clip))
            {
                m_Session.SetSelection(clip);
                m_Surface.SetSelection(new SlateTimelineEditorSelection(
                    SlateTimelineEditorElementKind.Clip,
                    string.Empty,
                    clip.Track.AuthoringId,
                    clip.AuthoringId,
                    string.Empty,
                    string.Empty,
                    -1));
                SelectionChanged?.Invoke(Selection);
                return true;
            }
            if (!string.IsNullOrEmpty(trackAuthoringId) && m_SourceTracks.TryGetValue(trackAuthoringId, out Track track))
            {
                m_Session.SetSelection(track);
                m_Surface.SetSelection(new SlateTimelineEditorSelection(
                    SlateTimelineEditorElementKind.Track,
                    string.Empty,
                    track.AuthoringId,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    -1));
                SelectionChanged?.Invoke(Selection);
                return true;
            }
            return false;
        }

        public void ApplyRuntimeOverlay(
            float visualTime,
            IReadOnlyDictionary<string, string> activeTracks,
            IReadOnlyDictionary<string, string> activeClips)
        {
            m_RuntimeFrame = Mathf.RoundToInt(visualTime * m_Session.FrameRate);
            SetRuntimeIds(activeTracks, activeClips);
            RebuildContent();
        }

        public void ApplyHistoryOverlay(
            float visualTime,
            IReadOnlyDictionary<string, string> activeTracks,
            IReadOnlyDictionary<string, string> activeClips)
        {
            m_RuntimeFrame = Mathf.RoundToInt(visualTime * m_Session.FrameRate);
            SetRuntimeIds(activeTracks, activeClips);
            RebuildContent();
        }

        public void ClearRuntimeOverlay()
        {
            m_RuntimeFrame = -1;
            m_ActiveTrackIds.Clear();
            m_ActiveClipIds.Clear();
            RebuildContent();
        }

        void SetRuntimeIds(
            IReadOnlyDictionary<string, string> activeTracks,
            IReadOnlyDictionary<string, string> activeClips)
        {
            m_ActiveTrackIds.Clear();
            m_ActiveClipIds.Clear();
            if (activeTracks != null)
                foreach (string id in activeTracks.Keys)
                    m_ActiveTrackIds.Add(id);
            if (activeClips != null)
                foreach (string id in activeClips.Keys)
                    m_ActiveClipIds.Add(id);
        }

        void OnSourceTimelineChanged()
        {
            RebuildContent();
        }

        void RebuildContent()
        {
            if (m_Disposed)
                return;
            SlateTimelineEditorContentView content = BuildContent();
            m_Surface.SetContent(content);
            SelectionChanged?.Invoke(Selection);
            m_RequestRepaint();
        }

        SlateTimelineEditorContentView BuildContent()
        {
            m_SourceRevision = TimelineAuthoringFingerprint.Compute(m_Request.Timeline);
            m_SourceTracks.Clear();
            m_SourceClips.Clear();
            var tracks = new List<SlateTimelineEditorTrackView>();
            for (int trackIndex = 0; trackIndex < m_Request.Timeline.Tracks.Count; trackIndex++)
            {
                Track sourceTrack = m_Request.Timeline.Tracks[trackIndex];
                if (sourceTrack == null)
                    continue;
                m_SourceTracks[sourceTrack.AuthoringId] = sourceTrack;
                var descriptors = new List<TimelineCurveChannelDescriptor>();
                TimelineCurveChannelCatalog.CollectForTrack(sourceTrack, descriptors);
                var clips = new List<SlateTimelineEditorClipView>();
                for (int clipIndex = 0; clipIndex < sourceTrack.Clips.Count; clipIndex++)
                {
                    Clip sourceClip = sourceTrack.Clips[clipIndex];
                    if (sourceClip == null)
                        continue;
                    m_SourceClips[sourceClip.AuthoringId] = sourceClip;
                    var curves = new List<SlateTimelineEditorCurveView>();
                    for (int curveIndex = 0; curveIndex < descriptors.Count; curveIndex++)
                    {
                        TimelineCurveChannelDescriptor descriptor = descriptors[curveIndex];
                        if (!descriptor.Supports(sourceClip))
                            continue;
                        curves.Add(new SlateTimelineEditorCurveView(
                            descriptor.ChannelId.Value,
                            descriptor.DisplayName,
                            TimelineCurveAuthoring.CopyCurve(descriptor.Read(sourceClip)),
                            sourceClip.StartFrame,
                            CurveEndFrame(sourceClip, descriptor)));
                    }
                    clips.Add(new SlateTimelineEditorClipView(
                        sourceClip.AuthoringId,
                        sourceTrack.AuthoringId,
                        sourceClip.Name,
                        sourceClip.ContractKind,
                        sourceClip.StartFrame,
                        sourceClip.EndFrame,
                        sourceClip.SelfEaseInFrame,
                        sourceClip.SelfEaseOutFrame,
                        curves,
                        m_ActiveClipIds.Contains(sourceClip.AuthoringId),
                        m_ActiveClipIds.Contains(sourceClip.AuthoringId) ? sourceClip.Name : string.Empty));
                }
                tracks.Add(new SlateTimelineEditorTrackView(
                    sourceTrack.AuthoringId,
                    "timeline",
                    sourceTrack.Name,
                    sourceTrack.ContractKind,
                    Color.white,
                    !sourceTrack.PersistentMuted,
                    false,
                    false,
                    descriptors.Count != 0,
                    clips,
                    m_ActiveTrackIds.Contains(sourceTrack.AuthoringId)));
            }
            var groups = new List<SlateTimelineEditorGroupView>
            {
                new SlateTimelineEditorGroupView(
                    "timeline",
                    m_Request.Timeline.Name,
                    m_GroupCollapsed,
                    tracks,
                    m_Request.Timeline.Sections
                        .Where(section => section != null)
                        .Select(section => new SlateTimelineEditorSectionView(
                            section.AuthoringId,
                            section.Name,
                            section.Frame))
                        .ToArray())
            };
            return new SlateTimelineEditorContentView(
                m_Request.Timeline.Name,
                m_Session.FrameRate,
                Mathf.Max(1, m_Request.Timeline.MaxFrame),
                0,
                Mathf.Max(1, m_Request.Timeline.MaxFrame),
                m_CurrentFrame,
                groups,
                m_RuntimeFrame,
                m_ActiveTrackIds,
                m_ActiveClipIds);
        }

        static int CurveEndFrame(Clip clip, TimelineCurveChannelDescriptor descriptor)
        {
            if (clip is MotionCurveClip motion &&
                (descriptor.ChannelId == TimelineCurveChannelCatalog.MotionPositionX ||
                 descriptor.ChannelId == TimelineCurveChannelCatalog.MotionPositionY ||
                 descriptor.ChannelId == TimelineCurveChannelCatalog.MotionPositionZ ||
                 descriptor.ChannelId == TimelineCurveChannelCatalog.MotionYaw))
                return Mathf.Max(motion.StartFrame + 1, motion.CurveEndFrame);
            return clip.EndFrame;
        }

        public void BeginGesture(string undoName)
        {
        }

        public void CommitGesture()
        {
        }

        public void CancelGesture()
        {
        }

        public void SetCurrentFrame(int frame)
        {
            m_CurrentFrame = Mathf.Clamp(frame, 0, m_Request.Timeline.MaxFrame);
            RebuildContent();
        }

        public void SetClipRange(string clipId, int startFrame, int endFrame, int blendInFrame, int blendOutFrame)
        {
            if (m_ReadOnly || !IsSourceCurrent() || !m_SourceClips.TryGetValue(clipId, out Clip clip))
                return;
            m_Session.Apply(() =>
            {
                clip.StartFrame = Mathf.Max(0, startFrame);
                clip.EndFrame = Mathf.Max(clip.StartFrame + 1, endFrame);
                clip.SelfEaseInFrame = Mathf.Clamp(blendInFrame, 0, clip.Duration);
                clip.SelfEaseOutFrame = Mathf.Clamp(blendOutFrame, 0, clip.Duration - clip.SelfEaseInFrame);
                clip.Track.UpdateMix();
                m_Request.Timeline.Init();
            }, "Move Timeline Clip");
            RebuildContent();
        }

        public void ReplaceCurve(string clipId, string curveId, AnimationCurve curve)
        {
            if (m_ReadOnly || !IsSourceCurrent() || !m_SourceClips.TryGetValue(clipId, out Clip clip))
                return;
            if (!TimelineCurveChannelCatalog.TryGet(curveId, out TimelineCurveChannelDescriptor descriptor) ||
                !descriptor.Supports(clip))
                return;
            float duration = Mathf.Max(1f / m_Session.FrameRate, (CurveEndFrame(clip, descriptor) - clip.StartFrame) / (float)m_Session.FrameRate);
            AnimationCurve normalized = ConvertCurveTime(curve, duration, true);
            m_Session.Apply(() =>
            {
                TimelineCurveAuthoring.Replace(clip, descriptor.ChannelId, normalized);
                clip.Track.UpdateMix();
                m_Request.Timeline.Init();
            }, "Edit Timeline Curve");
            RebuildContent();
        }

        public void SetGroupCollapsed(string groupId, bool collapsed)
        {
            m_GroupCollapsed = collapsed;
            RebuildContent();
        }

        public void SetTrackActive(string trackId, bool active)
        {
        }

        public void SetTrackLocked(string trackId, bool locked)
        {
        }

        public void RequestAddTrack(int frame)
        {
            if (m_ReadOnly)
                return;
            GenericMenu menu = new GenericMenu();
            foreach (TimelineTrackContract contract in m_Request.ContractCatalog.Tracks)
            {
                TimelineTrackContract candidate = contract;
                Type trackType = TimelineAuthoringTypeCatalog.RequireTrackType(candidate.Kind);
                bool requiresFields = trackType.GetCustomAttributes(typeof(TimelineAuthoringTrackFieldAttribute), true).Length != 0;
                menu.AddItem(new GUIContent(DisplayKind(candidate.Kind)), false, () =>
                    PopupWindow.Show(new Rect(0, 0, 1, 1), new TimelineTrackCreationPopup(
                        candidate.Kind,
                        DisplayKind(candidate.Kind),
                        requiresFields,
                        (name, channel, slot) => AddTrack(candidate.Kind, name, channel, slot))));
            }
            menu.ShowAsContext();
        }

        public void RequestAddClip(string trackId, int frame)
        {
            if (m_ReadOnly)
                return;
            if (!m_SourceTracks.TryGetValue(trackId, out Track track) ||
                !m_Request.ContractCatalog.TryGetTrack(track.ContractKind, out TimelineTrackContract trackContract))
                return;
            GenericMenu menu = new GenericMenu();
            for (int index = 0; index < trackContract.AllowedClipKinds.Count; index++)
            {
                string kind = trackContract.AllowedClipKinds[index];
                menu.AddItem(new GUIContent(DisplayKind(kind)), false, () => ShowClipPopup(trackId, kind, frame));
            }
            menu.ShowAsContext();
        }

        public void SetSectionFrame(string sectionId, int frame)
        {
            if (m_ReadOnly || !IsSourceCurrent())
                return;
            TimelineSection section = m_Request.Timeline.Sections.FirstOrDefault(value =>
                value != null && string.Equals(value.AuthoringId, sectionId, StringComparison.Ordinal));
            if (section == null)
                return;
            m_Session.Apply(() =>
            {
                m_Request.Timeline.ConfigureSection(section, section.Name, Mathf.Max(0, frame));
                m_Request.Timeline.Init();
            }, "Move Timeline Section");
            RebuildContent();
        }

        void ShowClipPopup(string trackId, string kind, int frame)
        {
            var request = new TimelineClipCreationRequest
            {
                TrackAuthoringId = trackId,
                Kind = kind,
                StartFrame = frame,
                EndFrame = frame + Mathf.Max(1, m_Session.FrameRate / 20),
                CurveEndFrame = frame + Mathf.Max(1, m_Session.FrameRate / 20)
            };
            var motionIds = m_Request.Timeline.Tracks.SelectMany(value => value.Clips).OfType<MotionCurveClip>().Select(value => value.AuthoringId).ToArray();
            PopupWindow.Show(new Rect(0, 0, 1, 1), new TimelineClipCreationPopup(
                request,
                motionIds,
                m_Request.Timeline.ExternalBindings,
                AddClip));
        }

        public void CopyClip(string clipId)
        {
        }

        public void OpenSource(string clipId)
        {
            if (m_SourceClips.TryGetValue(clipId, out Clip clip))
                m_OpenSourceClip?.Invoke(clip);
        }

        public void Select(SlateTimelineEditorSelection selection)
        {
            if (selection.Kind == SlateTimelineEditorElementKind.Clip && m_SourceClips.TryGetValue(selection.ClipId, out Clip clip))
                m_Session.SetSelection(clip);
            else if ((selection.Kind == SlateTimelineEditorElementKind.Key ||
                      selection.Kind == SlateTimelineEditorElementKind.Curve) &&
                     m_SourceClips.TryGetValue(selection.ClipId, out clip))
                m_Session.SetSelection(clip);
            else if (selection.Kind == SlateTimelineEditorElementKind.Track && m_SourceTracks.TryGetValue(selection.TrackId, out Track track))
                m_Session.SetSelection(track);
            else if (selection.Kind == SlateTimelineEditorElementKind.Section)
                m_Session.SetSelection(m_Request.Timeline.Sections.FirstOrDefault(value =>
                    value != null && string.Equals(value.AuthoringId, selection.SectionId, StringComparison.Ordinal)));
            else
                m_Session.SetSelection(null);
            SelectionChanged?.Invoke(Selection);
        }

        bool AddTrack(string kind, string name, string channelId, string slotId)
        {
            if (m_ReadOnly || !IsSourceCurrent())
                return false;
            try
            {
                m_Session.Apply(() =>
                {
                    Type trackType = TimelineAuthoringTypeCatalog.RequireTrackType(kind);
                    int count = m_Request.Timeline.Tracks.Count;
                    m_Request.Timeline.AddTrack(trackType, m_Request.ContractCatalog);
                    Track track = m_Request.Timeline.Tracks[count];
                    track.Name = string.IsNullOrWhiteSpace(name) ? DisplayKind(kind) : name.Trim();
                    TimelineAuthoringTrackBinding.Apply(track, channelId, slotId);
                }, "Add Timeline Track");
                RebuildContent();
                return true;
            }
            catch (Exception exception)
            {
                ReportIssue($"新增 Track 失败：{exception.Message}");
                return false;
            }
        }

        bool AddClip(TimelineClipCreationRequest request)
        {
            if (m_ReadOnly || !IsSourceCurrent() || !m_SourceTracks.TryGetValue(request.TrackAuthoringId, out Track track))
                return false;
            try
            {
                m_Session.Apply(() =>
                {
                    Clip clip = request.Kind == TimelineContractKinds.AnimationClip
                        ? TimelineAuthoringTrackBinding.CreateClip(m_Request.Timeline, m_Request.ContractCatalog, track, request.Resource as UnityEngine.AnimationClip, request.StartFrame)
                        : request.Resource != null
                            ? m_Request.Timeline.AddClip(m_Request.ContractCatalog, request.Resource, track, request.StartFrame)
                            : m_Request.Timeline.AddClip(m_Request.ContractCatalog, track, request.StartFrame);
                    clip.EndFrame = Mathf.Max(request.StartFrame + 1, request.EndFrame);
                    TimelineAuthoringClipBinding.Configure(m_Request.Timeline, clip, BuildClipConfiguration(clip, request), this);
                    clip.Track.UpdateMix();
                    m_Request.Timeline.Init();
                }, "Add Timeline Clip");
                RebuildContent();
                return true;
            }
            catch (Exception exception)
            {
                ReportIssue($"新增 Clip 失败：{exception.Message}");
                return false;
            }
        }

        TimelineAuthoringClipConfiguration BuildClipConfiguration(Clip clip, TimelineClipCreationRequest request)
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
                configuration.CurveEndFrame = request.CurveEndFrame;
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

        public bool TryResolveMotionClip(TimelineData timeline, string identity, out MotionCurveClip clip)
        {
            clip = timeline?.Tracks.SelectMany(value => value.Clips).OfType<MotionCurveClip>().FirstOrDefault(value => value.AuthoringId == identity);
            return clip != null;
        }

        public void SetRuntimeReadOnly(bool readOnly)
        {
            m_ReadOnly = readOnly;
        }

        bool IsSourceCurrent()
        {
            if (string.Equals(m_SourceRevision, TimelineAuthoringFingerprint.Compute(m_Request.Timeline), StringComparison.Ordinal))
                return true;
            ReportIssue("Timeline owner 已在外部修改，当前编辑已取消。");
            RebuildContent();
            return false;
        }

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

        static string DisplayKind(string kind)
        {
            int separator = kind.LastIndexOf('.');
            return (separator >= 0 ? kind.Substring(0, separator) : kind).Replace('-', ' ');
        }

        public void RequestRepaint()
        {
            m_RequestRepaint();
        }

        public void ShowNotification(string message)
        {
            ReportIssue(message);
        }

        public void SetTitle(string title)
        {
        }

        void ReportIssue(string message)
        {
            AuthoringIssue?.Invoke(message ?? string.Empty);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Request.Timeline.OnValueChanged -= OnSourceTimelineChanged;
            m_Surface.Dispose();
            m_Session.Dispose();
            SelectionChanged = null;
            AuthoringIssue = null;
            m_SourceTracks.Clear();
            m_SourceClips.Clear();
            m_ActiveTrackIds.Clear();
            m_ActiveClipIds.Clear();
        }
    }
}
#endif
