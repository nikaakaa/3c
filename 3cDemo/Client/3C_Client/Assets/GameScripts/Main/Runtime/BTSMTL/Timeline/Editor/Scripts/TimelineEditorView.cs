using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using BTSMTL.Editor;

namespace BTSMTL.Timeline.Editor
{
    public sealed class TimelineEditorView : VisualElement, IDisposable
    {
        VisualElement m_Top;
        VisualElement m_LeftPanel;
        VisualElement m_Toolbar;
        VisualElement m_TrackHandleContainer;
        VisualElement m_AddTrackButton;
        ObjectField m_TargetField;
        Button m_PlayButton;
        Button m_PauseButton;
        FloatField m_PlaySpeedField;
        Label m_PreviewErrorLabel;
        TimelineFieldView m_TimelineField;
        IVisualElementScheduledItem m_UpdateSchedule;
        readonly TimelinePreviewSession m_PreviewSession = new TimelinePreviewSession();
        bool m_LiveDebug;
        string m_PendingFocusTrackAuthoringId = string.Empty;
        string m_PendingFocusClipAuthoringId = string.Empty;

        public TimelineEditorView()
        {
            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>("VisualTree/TimelineEditorWindow");
            visualTree.CloneTree(this);
            AddToClassList("timelineEditorWindow");
            style.flexGrow = 1f;
            InitializeView();
        }

        public TimelineData Timeline { get; private set; }
        public TimelinePreviewSession PreviewSession => m_PreviewSession;
        public bool IsLiveDebug => m_LiveDebug;
        public event Action<Clip> OpenClipRequested;

        void InitializeView()
        {
            m_Top = this.Q("top");
            m_Top.SetEnabled(false);
            m_TargetField = this.Q<ObjectField>("target-field");
            m_TargetField.objectType = typeof(TimelinePreviewTarget);
            m_TargetField.allowSceneObjects = true;
            m_TargetField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == null)
                    m_PreviewSession.SetTarget(null);
                else if (!EditorUtility.IsPersistent(evt.newValue) && evt.newValue is TimelinePreviewTarget target)
                    m_PreviewSession.SetTarget(target);
                else
                    m_TargetField.SetValueWithoutNotify(null);
                UpdateBindState();
                m_TimelineField.UpdateBindState();
            });
            m_TargetField.SetEnabled(!Application.isPlaying);

            m_PlayButton = this.Q<Button>("play-button");
            m_PlayButton.clicked += () =>
            {
                m_PreviewSession.Play();
                UpdateBindState();
            };
            m_PauseButton = this.Q<Button>("pause-button");
            m_PauseButton.clicked += () =>
            {
                m_PreviewSession.Pause();
                UpdateBindState();
            };
            m_PlaySpeedField = this.Q<FloatField>("play-speed-field");
            m_PlaySpeedField.RegisterValueChangedCallback(evt =>
            {
                m_PreviewSession.PlaySpeed = evt.newValue;
                m_PlaySpeedField.SetValueWithoutNotify(m_PreviewSession.PlaySpeed);
            });
            m_PreviewErrorLabel = new Label();
            m_PreviewErrorLabel.style.marginLeft = 6f;
            m_PreviewErrorLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            m_PreviewErrorLabel.style.color = new Color(1f, 0.48f, 0.36f);
            m_PreviewErrorLabel.style.flexGrow = 1f;
            m_Top.Add(m_PreviewErrorLabel);

            m_LeftPanel = this.Q("left-panel");
            m_LeftPanel.SetEnabled(false);
            m_Toolbar = this.Q("tool-bar");
            m_TrackHandleContainer = this.Q("track-handle-container");
            m_TrackHandleContainer.focusable = true;
            m_TrackHandleContainer.RegisterCallback<KeyDownEvent>(OnTrackKeyDown);
            m_TrackHandleContainer.RegisterCallback<PointerDownEvent>(OnTrackPointerDown);

            m_AddTrackButton = this.Q("add-track-button");
            m_AddTrackButton.AddManipulator(new DropdownMenuManipulator(BuildTrackMenu, MouseButton.LeftMouse));

            m_TimelineField = this.Q<TimelineFieldView>();
            m_TimelineField.SetEnabled(false);
            m_TimelineField.EditorWindow = this;
            m_TimelineField.OnPopulatedCallback += OnTimelineFieldPopulated;
            m_TimelineField.OnGeometryChangedCallback += () => m_Toolbar.style.top = m_TimelineField.MarkerField.worldBound.yMin - 47;
            m_PreviewSession.Evaluated += m_TimelineField.UpdateTimeLocator;

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Undo.undoRedoEvent += OnUndoRedoEvent;
            m_UpdateSchedule = schedule.Execute(TickEditor).Every(16);
            UpdateBindState();
        }

        public void Init(TimelineData timeline, bool resetTime = true)
        {
            if (ReferenceEquals(Timeline, timeline))
                return;
            DetachTimeline();
            Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            Timeline.Init();
            Timeline.UpdateSerializedTimeline();
            Timeline.OnValueChanged += OnTimelineValueChanged;
            m_PreviewSession.SetTimeline(Timeline, resetTime);
            m_Top.SetEnabled(true);
            m_LeftPanel.SetEnabled(true);
            m_TimelineField.SetEnabled(true);
            UpdateBindState();
            EditorCoroutineHelper.WaitWhile(m_TimelineField.PopulateView, () => m_TimelineField.ContentWidth == 0);
        }

        public void OpenClip(Clip clip)
        {
            if (clip != null)
                OpenClipRequested?.Invoke(clip);
        }

        public void AddTrack(Type type)
        {
            if (m_LiveDebug)
                return;
            Timeline.ApplyModify(() => Timeline.AddTrack(type), "Add Track");
        }

        public void RefreshPreview(bool refreshView = false)
        {
            if (Timeline == null)
                return;
            Timeline.Init();
            Timeline.UpdateSerializedTimeline();
            m_PreviewSession.RefreshTimeline(false);
            if (refreshView)
                m_TimelineField.PopulateView();
            m_TimelineField.UpdateBindState();
            UpdateBindState();
        }

        public void PopulateView()
        {
            m_TrackHandleContainer.Clear();
            if (Timeline == null)
                return;
            foreach (TimelineTrackView trackView in m_TimelineField.TrackViews)
            {
                TimelineTrackHandle trackHandle = new TimelineTrackHandle(trackView) { SelectionContainer = m_TimelineField };
                trackHandle.SetRuntimeReadOnly(m_LiveDebug);
                m_TrackHandleContainer.Add(trackHandle);
            }
        }

        void OnTrackKeyDown(KeyDownEvent evt)
        {
            if (m_LiveDebug || evt.keyCode != KeyCode.Delete || Timeline == null)
                return;
            Timeline.ApplyModify(() =>
            {
                foreach (TimelineTrackView trackView in m_TimelineField.Selections.OfType<TimelineTrackView>().ToArray())
                    Timeline.RemoveTrack(trackView.Track);
            }, "Remove");
        }

        void OnTrackPointerDown(PointerDownEvent evt)
        {
            foreach (TimelineTrackHandle trackHandle in m_TrackHandleContainer.Query<TimelineTrackHandle>().ToList())
            {
                if (!trackHandle.worldBound.Contains(evt.position))
                    continue;
                trackHandle.OnPointerDown(evt);
                evt.StopImmediatePropagation();
                return;
            }
            if (evt.button == 0)
            {
                m_TimelineField.ClearSelection();
                evt.StopImmediatePropagation();
            }
        }

        void BuildTrackMenu(DropdownMenu menu)
        {
            if (m_LiveDebug || Timeline == null)
                return;
            string[] acceptableGroups = Timeline.GetAttribute<AcceptableTrackGroups>()?.Groups ?? Array.Empty<string>();
            IEnumerable<Type> types = TimelineEditorUtility.TrackScriptMap.Keys
                .Where(type => acceptableGroups.Contains(type.GetAttribute<TrackGroup>()?.Group ?? string.Empty))
                .OrderBy(type => type.GetAttribute<OrderedAttribute>()?.Index ?? 0f);
            foreach (Type type in types)
                menu.AppendAction(type.Name, _ => AddTrack(type));
        }

        void TickEditor()
        {
            if (m_PlaySpeedField == null)
                return;
            if (!m_LiveDebug && m_PreviewSession.IsPlaying)
                m_PreviewSession.Tick((float)BTSMTLEditorUtility.DeltaTime);
            m_PlaySpeedField.SetValueWithoutNotify(m_PreviewSession.PlaySpeed);
            UpdateBindState();
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            m_TargetField?.SetEnabled(!m_LiveDebug && !Application.isPlaying);
            m_PreviewSession.Pause();
            m_PreviewSession.SetTarget(null);
        }

        void OnUndoRedoEvent(in UndoRedoInfo info)
        {
            if (Timeline != null && info.undoName.StartsWith("Timeline:", StringComparison.Ordinal))
                RefreshPreview(true);
        }

        void OnTimelineValueChanged()
        {
            if (Timeline == null)
                return;
            Timeline.UpdateSerializedTimeline();
            if (!m_LiveDebug)
                m_PreviewSession.RefreshTimeline(false);
            m_TimelineField.PopulateView();
            m_TimelineField.UpdateBindState();
            UpdateBindState();
        }

        void UpdateBindState()
        {
            if (m_TargetField == null || m_PlayButton == null || m_PauseButton == null || m_PlaySpeedField == null)
                return;
            if (Timeline != null)
                m_TargetField.SetValueWithoutNotify(m_PreviewSession.Target);
            bool canPreview = !m_LiveDebug && m_PreviewSession.CanPreview;
            m_PlayButton.SetEnabled(canPreview);
            m_PauseButton.SetEnabled(canPreview);
            m_PlaySpeedField.SetEnabled(canPreview);
            m_TargetField.SetEnabled(!m_LiveDebug && !Application.isPlaying);
            string previewStatus = m_PreviewSession.Status;
            m_PreviewErrorLabel.text = previewStatus;
            m_PreviewErrorLabel.tooltip = previewStatus;
            m_PreviewErrorLabel.style.color = string.IsNullOrEmpty(m_PreviewSession.Error)
                ? new Color(0.72f, 0.72f, 0.72f)
                : new Color(1f, 0.48f, 0.36f);
            m_PreviewErrorLabel.style.display = string.IsNullOrEmpty(previewStatus)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            m_AddTrackButton.SetEnabled(!m_LiveDebug);
        }

        public void SetLiveDebug(bool liveDebug)
        {
            if (m_LiveDebug == liveDebug)
                return;
            if (liveDebug)
            {
                m_PreviewSession.Pause();
                m_PreviewSession.SetTarget(null);
            }
            m_LiveDebug = liveDebug;
            m_TimelineField.SetRuntimeReadOnly(liveDebug);
            foreach (TimelineTrackHandle trackHandle in m_TrackHandleContainer.Query<TimelineTrackHandle>().ToList())
                trackHandle.SetRuntimeReadOnly(liveDebug);
            UpdateBindState();
            if (!liveDebug)
                ClearRuntimeOverlay();
        }

        public void ApplyRuntimeOverlay(
            float visualTime,
            IReadOnlyDictionary<string, string> activeTracks,
            IReadOnlyDictionary<string, string> activeClips)
        {
            if (!m_LiveDebug)
                return;
            m_TimelineField.ApplyRuntimeOverlay(
                new TimelineRuntimeOverlayModel(visualTime, activeTracks, activeClips));
        }

        public void ClearRuntimeOverlay()
        {
            m_TimelineField.ClearRuntimeOverlay();
        }

        public bool FocusSource(string trackAuthoringId, string clipAuthoringId)
        {
            if (TryFocusSource(trackAuthoringId, clipAuthoringId))
                return true;
            if (!ContainsSource(trackAuthoringId, clipAuthoringId))
                return false;

            m_PendingFocusTrackAuthoringId = trackAuthoringId ?? string.Empty;
            m_PendingFocusClipAuthoringId = clipAuthoringId ?? string.Empty;
            return true;
        }

        bool TryFocusSource(string trackAuthoringId, string clipAuthoringId)
        {
            if (string.IsNullOrEmpty(trackAuthoringId))
                return Timeline != null;
            for (int trackIndex = 0; trackIndex < m_TimelineField.TrackViews.Count; trackIndex++)
            {
                TimelineTrackView trackView = m_TimelineField.TrackViews[trackIndex];
                if (!string.Equals(trackView.Track.AuthoringId, trackAuthoringId, StringComparison.Ordinal))
                    continue;
                if (string.IsNullOrEmpty(clipAuthoringId))
                {
                    m_TimelineField.ClearSelection();
                    m_TimelineField.AddToSelection(trackView);
                    return true;
                }
                for (int clipIndex = 0; clipIndex < trackView.ClipViews.Count; clipIndex++)
                {
                    TimelineClipView clipView = trackView.ClipViews[clipIndex];
                    if (!string.Equals(clipView.Clip.AuthoringId, clipAuthoringId, StringComparison.Ordinal))
                        continue;
                    m_TimelineField.ClearSelection();
                    m_TimelineField.AddToSelection(clipView);
                    return true;
                }
            }
            return false;
        }

        bool ContainsSource(string trackAuthoringId, string clipAuthoringId)
        {
            if (Timeline == null)
                return false;
            if (string.IsNullOrEmpty(trackAuthoringId))
                return true;
            for (int trackIndex = 0; trackIndex < Timeline.Tracks.Count; trackIndex++)
            {
                Track track = Timeline.Tracks[trackIndex];
                if (!string.Equals(track.AuthoringId, trackAuthoringId, StringComparison.Ordinal))
                    continue;
                if (string.IsNullOrEmpty(clipAuthoringId))
                    return true;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    if (string.Equals(track.Clips[clipIndex].AuthoringId, clipAuthoringId, StringComparison.Ordinal))
                        return true;
                }
                return false;
            }
            return false;
        }

        void OnTimelineFieldPopulated()
        {
            PopulateView();
            if (string.IsNullOrEmpty(m_PendingFocusTrackAuthoringId))
                return;
            if (!TryFocusSource(m_PendingFocusTrackAuthoringId, m_PendingFocusClipAuthoringId))
                return;

            m_PendingFocusTrackAuthoringId = string.Empty;
            m_PendingFocusClipAuthoringId = string.Empty;
        }

        void DetachTimeline()
        {
            if (Timeline != null)
                Timeline.OnValueChanged -= OnTimelineValueChanged;
            Timeline = null;
            m_PendingFocusTrackAuthoringId = string.Empty;
            m_PendingFocusClipAuthoringId = string.Empty;
            m_PreviewSession.SetTimeline(null);
        }

        public void Dispose()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Undo.undoRedoEvent -= OnUndoRedoEvent;
            m_UpdateSchedule?.Pause();
            if (m_TimelineField != null)
                m_PreviewSession.Evaluated -= m_TimelineField.UpdateTimeLocator;
            DetachTimeline();
            m_PreviewSession.Dispose();
            OpenClipRequested = null;
        }


    }
}

