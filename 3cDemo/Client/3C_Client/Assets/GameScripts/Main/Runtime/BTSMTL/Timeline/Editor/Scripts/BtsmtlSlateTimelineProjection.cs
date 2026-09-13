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
        [SerializeField] string m_CurveTrackAuthoringId;
        [SerializeField] bool m_GroupCollapsed;

        public BtsmtlSlateTimelineViewState(
            float viewTimeMin,
            float viewTimeMax,
            float currentTime,
            Vector2 scrollPosition,
            string trackAuthoringId,
            string clipAuthoringId,
            bool groupCollapsed,
            string curveTrackAuthoringId = null)
        {
            m_ViewTimeMin = viewTimeMin;
            m_ViewTimeMax = viewTimeMax;
            m_CurrentTime = currentTime;
            m_ScrollPosition = scrollPosition;
            m_TrackAuthoringId = trackAuthoringId ?? string.Empty;
            m_ClipAuthoringId = clipAuthoringId ?? string.Empty;
            m_CurveTrackAuthoringId = curveTrackAuthoringId ?? string.Empty;
            m_GroupCollapsed = groupCollapsed;
        }

        public float ViewTimeMin => m_ViewTimeMin;
        public float ViewTimeMax => m_ViewTimeMax;
        public float CurrentTime => m_CurrentTime;
        public Vector2 ScrollPosition => m_ScrollPosition;
        public string TrackAuthoringId => m_TrackAuthoringId ?? string.Empty;
        public string ClipAuthoringId => m_ClipAuthoringId ?? string.Empty;
        public string CurveTrackAuthoringId => m_CurveTrackAuthoringId ?? string.Empty;
        public bool GroupCollapsed => m_GroupCollapsed;
        public bool HasSelection => !string.IsNullOrEmpty(TrackAuthoringId) || !string.IsNullOrEmpty(ClipAuthoringId);
    }

    public sealed class BtsmtlSlateTimelineProjection : IDisposable, ITimelineAuthoringClipResolver
    {
        readonly TimelineEditorOpenRequest m_Request;
        readonly TimelineEditorSessionContext m_Session;
        readonly BtsmtlSlateTimelineBinding m_Binding;
        readonly CutsceneEditorSurface m_EmbeddedEditor;
        readonly HashSet<string> m_RuntimeActiveTracks = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<string, string> m_RuntimeClipStatuses = new Dictionary<string, string>(StringComparer.Ordinal);
        bool m_RuntimeOverlayVisible;
        bool m_Disposed;
        bool m_RebuildQueued;
        bool m_ReadOnly;
        float? m_RuntimeVisualTime;
        float? m_HistoryVisualTime;

        static BtsmtlSlateTimelineProjection s_Current;

        BtsmtlSlateTimelineProjection(TimelineEditorOpenRequest request, Action<Clip> openSourceClip)
        {
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
            m_Session = new TimelineEditorSessionContext(request);
            m_Binding = new BtsmtlSlateTimelineBinding(request, m_Session, openSourceClip);
            Selection.activeObject = request.SerializedOwner;
            m_EmbeddedEditor = ScriptableObject.CreateInstance<CutsceneEditorSurface>();
            m_EmbeddedEditor.InitializeEmbedded(m_Binding, null);
            m_EmbeddedEditor.ConfigureEmbeddedRuntimeTime(() => m_RuntimeVisualTime);
            m_EmbeddedEditor.ConfigureEmbeddedHistoryTime(() => m_HistoryVisualTime);
            m_EmbeddedEditor.ConfigureEmbeddedRuntimeState(IsRuntimeTrackActive, RuntimeClipStatus);
            m_Session.SelectionChanged += OnSelectionChanged;
            m_Request.Timeline.OnValueChanged += OnSourceTimelineChanged;
            Undo.undoRedoEvent += OnUndoRedoEvent;
        }

        public TimelineEditorSelection Selection => m_Session.Selection;
        public event Action<TimelineEditorSelection> SelectionChanged;
        public event Action<string> AuthoringIssue;

        public void ApplyFormalMutation(Action mutation, string undoName)
        {
            if (m_ReadOnly)
            {
                ReportIssue("Timeline 当前只读，不能修改正式字段。");
                return;
            }
            try
            {
                m_Binding.Apply(mutation, undoName);
                m_EmbeddedEditor.RequestEmbeddedRepaint();
            }
            catch (Exception exception)
            {
                ReportIssue($"字段修改失败：{exception.Message}");
            }
        }

        public BtsmtlSlateTimelineViewState CaptureViewState()
        {
            string trackId = string.Empty;
            string clipId = string.Empty;
            if (m_Binding.Selected is IEmbeddedTimelineClipBinding clip)
            {
                clipId = clip.AuthoringId;
                trackId = clip.Track?.AuthoringId ?? string.Empty;
            }
            else if (m_Binding.Selected is IEmbeddedTimelineTrackBinding track)
            {
                trackId = track.AuthoringId;
            }
            string curveTrackId = m_Binding.Groups
                .SelectMany(group => group.Tracks)
                .FirstOrDefault(track => track.ShowCurves)?.AuthoringId ?? string.Empty;
            bool collapsed = m_Binding.Groups.Count != 0 && m_Binding.Groups[0].IsCollapsed;
            return new BtsmtlSlateTimelineViewState(
                m_Binding.ViewTimeMin,
                m_Binding.ViewTimeMax,
                m_Binding.CurrentFrame / (float)Mathf.Max(1, m_Binding.FrameRate),
                m_EmbeddedEditor.EmbeddedScrollPosition,
                trackId,
                clipId,
                collapsed,
                curveTrackId);
        }

        public void RestoreViewState(BtsmtlSlateTimelineViewState state)
        {
            if (state.ViewTimeMax > state.ViewTimeMin)
            {
                m_Binding.ViewTimeMin = state.ViewTimeMin;
                m_Binding.ViewTimeMax = state.ViewTimeMax;
            }
            m_Binding.CurrentFrame = Mathf.RoundToInt(state.CurrentTime * Mathf.Max(1, m_Binding.FrameRate));
            if (m_Binding.Groups.Count != 0)
                m_Binding.Groups[0].IsCollapsed = state.GroupCollapsed;
            foreach (IEmbeddedTimelineGroupBinding group in m_Binding.Groups)
                foreach (IEmbeddedTimelineTrackBinding track in group.Tracks)
                    track.ShowCurves = string.Equals(track.AuthoringId, state.CurveTrackAuthoringId, StringComparison.Ordinal);
            m_EmbeddedEditor.EmbeddedScrollPosition = state.ScrollPosition;
            if (state.HasSelection)
                FocusSource(state.TrackAuthoringId, state.ClipAuthoringId);
        }

        public void DrawEmbeddedGUI(float width, float height)
        {
            if (!m_Disposed)
                m_EmbeddedEditor.DrawEmbeddedGUI(width, height);
        }

        public void DrawEmbeddedGUI(float width, float height, Action beginWindows, Action endWindows)
        {
            if (!m_Disposed)
                m_EmbeddedEditor.DrawEmbeddedGUI(width, height, beginWindows, endWindows);
        }

        public bool FocusSource(string trackAuthoringId, string clipAuthoringId)
        {
            if (!string.IsNullOrEmpty(clipAuthoringId) &&
                m_Binding.TryGetClipBinding(clipAuthoringId, out IEmbeddedTimelineClipBinding clip))
            {
                m_Binding.Select(clip);
                m_EmbeddedEditor.RequestEmbeddedRepaint();
                return true;
            }
            if (!string.IsNullOrEmpty(trackAuthoringId) &&
                m_Binding.TryGetTrackBinding(trackAuthoringId, out IEmbeddedTimelineTrackBinding track))
            {
                m_Binding.Select(track);
                m_EmbeddedEditor.RequestEmbeddedRepaint();
                return true;
            }
            return false;
        }

        public static BtsmtlSlateTimelineProjection Open(
            TimelineEditorOpenRequest request,
            Action<Clip> openSourceClip = null)
        {
            DisposeCurrent();
            s_Current = new BtsmtlSlateTimelineProjection(request, openSourceClip);
            return s_Current;
        }

        public static bool TryOpen(
            TimelineEditorOpenRequest request,
            Action<Clip> openSourceClip,
            out BtsmtlSlateTimelineProjection projection,
            out string unavailableReason)
        {
            try
            {
                projection = Open(request, openSourceClip);
                unavailableReason = string.Empty;
                return true;
            }
            catch (Exception exception) when (
                exception is TypeLoadException ||
                exception is MissingMethodException ||
                exception is InvalidOperationException)
            {
                DisposeCurrent();
                projection = null;
                unavailableReason = exception.Message;
                return false;
            }
        }

        public static void DisposeCurrent()
        {
            s_Current?.Dispose();
            s_Current = null;
        }

        public bool TryResolveMotionClip(TimelineData timeline, string identity, out MotionCurveClip clip)
        {
            clip = timeline?.Tracks
                .SelectMany(track => track.Clips)
                .OfType<MotionCurveClip>()
                .FirstOrDefault(value => string.Equals(value.AuthoringId, identity, StringComparison.Ordinal));
            return clip != null;
        }

        public void SetRuntimeReadOnly(bool readOnly)
        {
            m_ReadOnly = readOnly;
            m_Binding.SetReadOnly(readOnly);
            m_EmbeddedEditor.RequestEmbeddedRepaint();
        }

        public void ApplyRuntimeOverlay(
            float visualTime,
            IReadOnlyDictionary<string, string> activeTracks,
            IReadOnlyDictionary<string, string> activeClips)
        {
            m_RuntimeVisualTime = Mathf.Max(0f, visualTime);
            m_HistoryVisualTime = null;
            SetRuntimeState(activeTracks, activeClips);
            m_EmbeddedEditor.RequestEmbeddedRepaint();
        }

        public void ClearRuntimeOverlay()
        {
            m_RuntimeVisualTime = null;
            m_HistoryVisualTime = null;
            m_RuntimeOverlayVisible = false;
            m_RuntimeActiveTracks.Clear();
            m_RuntimeClipStatuses.Clear();
            m_EmbeddedEditor.RequestEmbeddedRepaint();
        }

        public void ApplyHistoryOverlay(
            float visualTime,
            IReadOnlyDictionary<string, string> activeTracks,
            IReadOnlyDictionary<string, string> activeClips)
        {
            m_HistoryVisualTime = Mathf.Max(0f, visualTime);
            m_RuntimeVisualTime = null;
            SetRuntimeState(activeTracks, activeClips);
            m_EmbeddedEditor.RequestEmbeddedRepaint();
        }

        public void ApplyAuthoringPreviewTime(float time)
        {
            m_Binding.CurrentFrame = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Max(0f, time) * m_Binding.FrameRate),
                0,
                m_Request.Timeline.MaxFrame);
            m_EmbeddedEditor.RequestEmbeddedRepaint();
        }

        void SetRuntimeState(
            IReadOnlyDictionary<string, string> activeTracks,
            IReadOnlyDictionary<string, string> activeClips)
        {
            m_RuntimeOverlayVisible = true;
            m_RuntimeActiveTracks.Clear();
            m_RuntimeClipStatuses.Clear();
            if (activeTracks != null)
                foreach (string identity in activeTracks.Keys)
                    m_RuntimeActiveTracks.Add(identity);
            if (activeClips != null)
                foreach (KeyValuePair<string, string> pair in activeClips)
                    m_RuntimeClipStatuses[pair.Key] = pair.Value ?? string.Empty;
        }

        bool IsRuntimeTrackActive(string authoringId)
        {
            return !m_RuntimeOverlayVisible || m_RuntimeActiveTracks.Contains(authoringId ?? string.Empty);
        }

        string RuntimeClipStatus(string authoringId)
        {
            return m_RuntimeClipStatuses.TryGetValue(authoringId ?? string.Empty, out string status)
                ? status
                : string.Empty;
        }

        void OnSelectionChanged(TimelineEditorSelection selection)
        {
            SelectionChanged?.Invoke(selection);
        }

        void OnSourceTimelineChanged()
        {
            QueueRebuildBinding();
        }

        void OnUndoRedoEvent(in UndoRedoInfo info)
        {
            QueueRebuildBinding();
        }

        void QueueRebuildBinding()
        {
            if (m_RebuildQueued || m_Disposed)
                return;
            m_RebuildQueued = true;
            EditorApplication.delayCall += RebuildBinding;
        }

        void RebuildBinding()
        {
            m_RebuildQueued = false;
            if (m_Disposed)
                return;
            BtsmtlSlateTimelineViewState state = CaptureViewState();
            m_Binding.Rebuild();
            RestoreViewState(state);
            m_EmbeddedEditor.RequestEmbeddedRepaint();
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
            if (m_RebuildQueued)
                EditorApplication.delayCall -= RebuildBinding;
            m_RebuildQueued = false;
            m_Session.SelectionChanged -= OnSelectionChanged;
            m_Request.Timeline.OnValueChanged -= OnSourceTimelineChanged;
            Undo.undoRedoEvent -= OnUndoRedoEvent;
            m_EmbeddedEditor.ClearEmbedded();
            UnityEngine.Object.DestroyImmediate(m_EmbeddedEditor);
            m_Session.Dispose();
            SelectionChanged = null;
            AuthoringIssue = null;
            if (ReferenceEquals(s_Current, this))
                s_Current = null;
        }
    }
}
#endif
