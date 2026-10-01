using System;
using System.Collections;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using Slate;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor
{
    public sealed class BtsmtlExecutionTimelineView : VisualElement, IDisposable
    {
        readonly RuntimeDebugSession m_Session = RuntimeDebugSession.Shared;
        readonly IMGUIContainer m_Surface;
        readonly Label m_Status = new Label();
        readonly Label m_SelectionInfo = new Label();
        readonly Label m_SelectionStatus = new Label { style = { whiteSpace = WhiteSpace.Normal } };
        readonly Action m_BeginWindows;
        readonly Action m_EndWindows;
        readonly Action<RuntimeExecutionSpan> m_OpenSource;
        readonly Action m_SeekRequested;
        readonly ToolbarButton m_Record;
        readonly ToolbarButton m_Live;
        readonly ToolbarButton m_SeekStart;
        readonly ToolbarButton m_SeekEnd;
        CutsceneEditorSurface m_Slate;
        ExecutionBinding m_Binding;
        RuntimeTraceDomain m_Domain;
        long m_Version = -1;
        long m_TargetRevision = -1;
        ulong m_HistorySequence;
        bool m_Visible;
        Guid m_LastCaptureId;
        RuntimeDebugAttachmentState m_LastState;
        RuntimeExecutionSpan? m_SeekSelection;

        public BtsmtlExecutionTimelineView(Action beginWindows, Action endWindows,
            Action<RuntimeExecutionSpan> openSource, Action seekRequested)
        {
            m_BeginWindows = beginWindows;
            m_EndWindows = endWindows;
            m_OpenSource = openSource;
            m_SeekRequested = seekRequested;
            name = "workbench-execution-timeline";
            style.flexGrow = 1;
            style.minHeight = 320;
            var toolbar = new Toolbar();
            var domain = new PopupField<string>(new List<string> { "逻辑 Tick", "表现帧" }, 0);
            domain.RegisterValueChangedCallback(evt =>
            {
                m_Domain = evt.newValue == "逻辑 Tick" ? RuntimeTraceDomain.Logic : RuntimeTraceDomain.Presentation;
                ReleaseSurface();
                RefreshProjection();
            });
            toolbar.Add(domain);
            m_Record = new ToolbarButton(() =>
            {
                if (m_Session.IsCaptureRecording)
                    m_Session.EndCapture();
                else
                    m_Session.BeginCapture(RuntimeTraceChannel.Graph | RuntimeTraceChannel.StateMachine |
                        RuntimeTraceChannel.Timeline | RuntimeTraceChannel.Blackboard, RuntimeDiagnosticsCaptureDetail.Continuous);
            }) { text = "记录执行" };
            m_Live = new ToolbarButton(() => m_Session.ResumeLive()) { text = "返回实时" };
            toolbar.Add(m_Record);
            toolbar.Add(m_Live);
            m_SeekStart = new ToolbarButton(() => SeekSelectedEvent(false)) { text = "查看进入时" };
            m_SeekEnd = new ToolbarButton(() => SeekSelectedEvent(true)) { text = "查看退出时" };
            m_SeekStart.SetEnabled(false);
            m_SeekEnd.SetEnabled(false);
            toolbar.Add(m_SeekStart);
            toolbar.Add(m_SeekEnd);
            toolbar.Add(m_Status);
            Add(toolbar);
            m_SelectionInfo.style.whiteSpace = WhiteSpace.Normal;
            Add(m_SelectionInfo);
            Add(m_SelectionStatus);
            m_Surface = new IMGUIContainer(Draw) { name = "slate-execution-surface" };
            m_Surface.style.flexGrow = 1;
            Add(m_Surface);
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                m_Session.Changed += RefreshProjection;
                RefreshProjection();
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                m_Session.Changed -= RefreshProjection;
            });
        }

        public void SetVisible(bool visible)
        {
            m_Visible = visible;
            style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible)
                RefreshProjection();
            else
                ReleaseSurface();
        }

        void RefreshProjection()
        {
            if (!m_Visible || panel == null)
                return;
            m_Record.text = m_Session.IsCaptureRecording ? "结束记录" : "记录执行";
            m_Record.SetEnabled(m_Session.CanStopCapture || m_Session.CanStartCapture);
            m_Live.SetEnabled(m_Session.CanResumeLiveTarget);
            if (m_Version == m_Session.CaptureVersion && m_TargetRevision == m_Session.TargetRevision &&
                m_HistorySequence == m_Session.HistorySequence && m_LastCaptureId == m_Session.CaptureId &&
                m_LastState == m_Session.AttachmentState)
                return;
            RuntimeExecutionTimeline timeline = m_Session.BuildExecutionTimeline();
            if (timeline == null)
            {
                ReleaseSurface();
                m_Status.text = "尚无执行记录";
                return;
            }
            if (m_Binding == null || m_Binding.CaptureId != timeline.CaptureId)
            {
                ReleaseSurface();
                m_Binding = new ExecutionBinding(m_Session.ExecutionSpanCapacity, m_Domain,
                    m_Surface.MarkDirtyRepaint, m_OpenSource, ShowSelection, Seek);
                m_Slate = ScriptableObject.CreateInstance<CutsceneEditorSurface>();
                m_Slate.InitializeEmbedded(m_Binding, m_Surface.MarkDirtyRepaint);
                m_Slate.ConfigureEmbeddedRuntimeTime(() => m_Session.AttachmentState == RuntimeDebugAttachmentState.Live
                    ? m_Binding.LatestPosition : (float?)null);
                m_Slate.ConfigureEmbeddedHistoryTime(() => m_Session.AttachmentState != RuntimeDebugAttachmentState.Live
                    ? m_Binding.CurrentTime : (float?)null);
            }
            bool structureReset = m_Binding.Apply(timeline, m_Session.AttachmentState == RuntimeDebugAttachmentState.Live);
            if (structureReset)
                m_Slate.InvalidateEmbeddedClipBindings();
            RuntimeExecutionSpan? selected = m_Binding.SelectedSpan;
            if (selected.HasValue)
            {
                m_SeekSelection = selected;
                m_SeekEnd.SetEnabled(selected.Value.Completed);
                m_SelectionStatus.text = m_Binding.SelectedInfo;
                m_SelectionStatus.tooltip = selected.Value.End.Payload.Detail;
            }
            m_Version = m_Session.CaptureVersion;
            m_TargetRevision = m_Session.TargetRevision;
            m_HistorySequence = m_Session.HistorySequence;
            m_LastCaptureId = m_Session.CaptureId;
            m_LastState = m_Session.AttachmentState;
            m_Status.text = timeline.EvictedEvents != 0 ? "较早记录已淘汰" :
                timeline.UnmappedEventCount != 0 ? "部分来源映射缺失" :
                m_Session.AttachmentState == RuntimeDebugAttachmentState.Live ? "实时执行记录" : "历史节点与变量；角色画面尚未恢复";
            m_Surface.MarkDirtyRepaint();
        }

        void ShowSelection(RuntimeExecutionSpan? selection)
        {
            m_SelectionStatus.text = m_Binding.SelectedInfo;
            m_SelectionStatus.tooltip = selection.HasValue ? selection.Value.End.Payload.Detail : string.Empty;
            if (selection.HasValue)
                m_SeekSelection = selection;
            RuntimeExecutionSpan? selected = selection ?? m_SeekSelection;
            m_SeekStart.SetEnabled(selected.HasValue && selected.Value.State != RuntimeExecutionSpanState.MissingStart);
            m_SeekEnd.SetEnabled(selected.HasValue && selected.Value.Completed);
            if (!selected.HasValue)
            {
                m_SelectionInfo.text = string.Empty;
                return;
            }
            RuntimeExecutionSpan span = selected.Value;
            m_SelectionInfo.text = span.Start.Kind == RuntimeTraceEventKind.BlackboardWritten
                ? $"黑板 {span.Source.ElementAuthoringId} = {span.Start.Payload.Value.DisplayValue()} · 采用 Tick {span.StartPosition} · 作用域代次 {span.Start.Payload.BlackboardOwnerGeneration}"
                : span.Kind == RuntimeExecutionSpanKind.Loop
                ? $"Loop 迭代 {span.LoopIteration} · 节点发生 {span.ActivationGeneration} · 起点 {span.StartPosition}"
                : span.Kind == RuntimeExecutionSpanKind.Branch
                    ? $"决策 {span.Start.Payload.Status} · 条件结果 {span.Start.Payload.Flag} · {span.Start.Payload.Detail}"
                    : $"节点发生 {span.ActivationGeneration} · 调用 {span.GraphInvocationGeneration} · 起点 {span.StartPosition}";
        }

        void SeekSelectedEvent(bool end)
        {
            RuntimeExecutionSpan span = m_SeekSelection.Value;
            RuntimeTraceEvent trace = end ? span.End : span.Start;
            if (m_Session.TrySeekExecutionEvent(m_Binding.CaptureId, trace.Sequence))
            {
                m_SeekRequested();
                m_Binding.SetCursor(trace.Position);
            }
            else
                m_Status.text = "该事件已不在保留记录中";
        }

        void Seek(float position)
        {
            RuntimeExecutionSpan? selected = m_Binding.SelectedSpan ?? m_SeekSelection;
            if (!selected.HasValue)
            {
                m_Status.text = "先选择一个片段，以确定历史分支";
                return;
            }
            RuntimeExecutionSpan span = selected.Value;
            m_SeekSelection = selected;
            if (m_Session.TrySeekExecutionPosition(m_Domain, (ulong)Math.Max(0, Math.Floor(position)),
                span.ExecutionBranchId, span.RuntimeEpoch))
            {
                m_SeekRequested();
                m_Binding.SetCursor(position);
            }
            else
                m_Status.text = "此分支在该位置没有保留记录";
        }

        void Draw()
        {
            if (m_Slate != null)
                m_Slate.DrawEmbeddedGUI(Mathf.Max(1, m_Surface.contentRect.width),
                    Mathf.Max(1, m_Surface.contentRect.height), m_BeginWindows, m_EndWindows);
        }

        void ReleaseSurface()
        {
            if (m_Slate != null)
            {
                m_Slate.ClearEmbedded();
                UnityEngine.Object.DestroyImmediate(m_Slate);
            }
            m_Slate = null;
            m_Binding = null;
            m_SelectionInfo.text = string.Empty;
            m_SelectionStatus.text = string.Empty;
            m_SelectionStatus.tooltip = string.Empty;
            m_SeekSelection = null;
            m_SeekStart.SetEnabled(false);
            m_SeekEnd.SetEnabled(false);
            m_Version = -1;
            m_TargetRevision = -1;
            m_HistorySequence = 0;
        }

        public void Dispose()
        {
            m_Session.Changed -= RefreshProjection;
            ReleaseSurface();
        }

        sealed class ExecutionBinding : IEmbeddedTimelineBinding
        {
            static readonly string[] KindNames = Enum.GetNames(typeof(RuntimeExecutionSpanKind));
            readonly RuntimeTraceDomain m_Domain;
            readonly Action m_Repaint;
            readonly Action<RuntimeExecutionSpan> m_Open;
            readonly Action<float> m_Seek;
            readonly Action<RuntimeExecutionSpan?> m_Select;
            readonly Group m_Group;
            readonly IEmbeddedTimelineGroupBinding[] m_Groups;
            readonly Clip[] m_Clips;
            readonly Track[] m_Tracks;
            readonly Dictionary<(RuntimeInstanceKey, RuntimeSourceElementHandle, RuntimeExecutionSpanKind, Guid, ulong, RuntimeContentRevision), Track> m_TrackMap;
            RuntimeExecutionTimeline m_Timeline;
            int m_Processed;
            long m_Evicted;
            float m_Cursor;
            float m_Extent;

            internal ExecutionBinding(int capacity, RuntimeTraceDomain domain, Action repaint,
                Action<RuntimeExecutionSpan> open, Action<RuntimeExecutionSpan?> select, Action<float> seek)
            {
                m_Domain = domain;
                m_Repaint = repaint;
                m_Open = open;
                m_Select = select;
                m_Seek = seek;
                m_Group = new Group(capacity, domain == RuntimeTraceDomain.Logic ? "逻辑执行" : "表现执行");
                m_Groups = new IEmbeddedTimelineGroupBinding[] { m_Group };
                m_Clips = new Clip[capacity];
                m_Tracks = new Track[capacity];
                m_TrackMap = new Dictionary<(RuntimeInstanceKey, RuntimeSourceElementHandle, RuntimeExecutionSpanKind, Guid, ulong, RuntimeContentRevision), Track>(capacity);
                for (int i = 0; i < capacity; i++)
                {
                    string id = i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    m_Clips[i] = new Clip(this, i, id);
                    m_Tracks[i] = new Track(this, id);
                }
            }

            internal Guid CaptureId => m_Timeline?.CaptureId ?? Guid.Empty;
            internal RuntimeExecutionSpan? SelectedSpan => Selected is Clip clip ? clip.Span : null;
            internal string SelectedInfo => Selected is Clip clip ? clip.Info : string.Empty;
            internal float LatestPosition { get; private set; }
            internal bool Apply(RuntimeExecutionTimeline timeline, bool live)
            {
                bool reset = !ReferenceEquals(m_Timeline, timeline) || m_Evicted != timeline.EvictedEvents ||
                    m_Processed > timeline.Spans.Count;
                m_Timeline = timeline;
                if (reset)
                {
                    for (int i = 0; i < m_Processed; i++)
                        m_Clips[i].OwnerTrack = null;
                    m_Processed = 0;
                    m_TrackMap.Clear();
                    m_Group.Items.Clear();
                    Selected = null;
                    m_Select(null);
                }
                m_Evicted = timeline.EvictedEvents;
                for (int i = m_Processed; i < timeline.Spans.Count; i++)
                {
                    RuntimeExecutionSpan span = timeline.Spans[i];
                    RuntimeTraceDomain domain = span.Domain == RuntimeTraceDomain.Lifecycle ? RuntimeTraceDomain.Logic : span.Domain;
                    if (domain != m_Domain)
                        continue;
                    if (span.Kind == RuntimeExecutionSpanKind.Point ||
                        span.Start.Kind is RuntimeTraceEventKind.TimelineLogicTime or RuntimeTraceEventKind.TimelineVisualTime or
                            RuntimeTraceEventKind.TreeClipUpdated or RuntimeTraceEventKind.ClipActive)
                        continue;
                    var key = (span.Instance, span.SourceHandle, span.Kind, span.ExecutionBranchId, span.RuntimeEpoch, span.ContentRevision);
                    if (!m_TrackMap.TryGetValue(key, out Track track))
                    {
                        track = m_Tracks[m_Group.Items.Count];
                        track.Reset(span);
                        m_TrackMap.Add(key, track);
                        m_Group.Items.Add(track);
                    }
                    if (span.State == RuntimeExecutionSpanState.MissingStart && track.EndsWithMissingStart)
                        continue;
                    Clip clip = m_Clips[i];
                    clip.OwnerTrack = track;
                    track.Append(clip);
                }
                m_Processed = timeline.Spans.Count;
                LatestPosition = m_Domain == RuntimeTraceDomain.Logic
                    ? timeline.LatestLogicPosition : timeline.LatestPresentationPosition;
                m_Extent = Mathf.Max(m_Extent, LatestPosition);
                m_Cursor = LatestPosition;
                if (ViewTimeMax < 1)
                    ViewTimeMax = Mathf.Max(60, m_Extent);
                else if (live && LatestPosition > ViewTimeMax)
                {
                    float width = ViewTimeMax - ViewTimeMin;
                    ViewTimeMax = LatestPosition + width * 0.1f;
                    ViewTimeMin = ViewTimeMax - width;
                }
                return reset;
            }

            internal void SetCursor(float value) { m_Cursor = value; m_Repaint(); }
            public string DisplayName => m_Group.DisplayName;
            public int FrameRate => 1;
            public float SnapTime(float time) => Mathf.Round(time);
            public string SnapLabel => m_Domain == RuntimeTraceDomain.Logic ? "Tick" : "Frame";
            public void ShowSnapSettings(Rect rect) => EditorGUI.LabelField(rect, SnapLabel);
            public float Length => Mathf.Max(1, m_Extent);
            public float CurrentTime { get => m_Cursor; set => m_Seek(value); }
            public bool DisplayFrames { get; set; } = true;
            public float SnapInterval => 1;
            public float StepInterval => 1;
            public float ViewTimeMin { get; set; }
            public float ViewTimeMax { get; set; }
            public bool IsReadOnly => true;
            public IReadOnlyList<IEmbeddedTimelineGroupBinding> Groups => m_Groups;
            public IReadOnlyList<IEmbeddedTimelineSectionBinding> Sections => Array.Empty<IEmbeddedTimelineSectionBinding>();
            public IEmbeddedTimelineElementBinding Selected { get; private set; }
            public void Select(IEmbeddedTimelineElementBinding element)
            {
                Selected = element;
                m_Select(SelectedSpan);
                m_Repaint();
            }
            public bool TryGetTrack(string id, out IEmbeddedTimelineTrackBinding track)
            {
                foreach (IEmbeddedTimelineTrackBinding candidate in m_Group.Items)
                    if (candidate.AuthoringId == id) { track = candidate; return true; }
                track = null;
                return false;
            }
            public bool TryGetClip(string id, out IEmbeddedTimelineClipBinding clip)
            {
                for (int i = 0; i < m_Processed; i++)
                    if (m_Clips[i].AuthoringId == id && m_Clips[i].OwnerTrack != null) { clip = m_Clips[i]; return true; }
                clip = null;
                return false;
            }
            public void OpenSource(IEmbeddedTimelineClipBinding clip) => m_Open(((Clip)clip).Span);
            public void RequestRepaint() => m_Repaint();
            public bool CanPasteClip => false;
            static InvalidOperationException ReadOnly() => new InvalidOperationException("执行记录只读，不能修改已发生的调用。");
            public void AddTrack() => throw ReadOnly();
            public void AddClip(IEmbeddedTimelineTrackBinding track, double time) => throw ReadOnly();
            public void AddMarker(IEmbeddedTimelineTrackBinding track, double time) => throw ReadOnly();
            public void DeleteMarker(IEmbeddedTimelineMarkerBinding marker) => throw ReadOnly();
            public void MoveMarker(IEmbeddedTimelineMarkerBinding marker, double time) => throw ReadOnly();
            public void OpenMarker(IEmbeddedTimelineMarkerBinding marker) => throw ReadOnly();
            public void SetTrackActive(IEmbeddedTimelineTrackBinding track, bool active) => throw ReadOnly();
            public void DeleteTrack(IEmbeddedTimelineTrackBinding track) => throw ReadOnly();
            public void DeleteClip(IEmbeddedTimelineClipBinding clip) => throw ReadOnly();
            public void DeleteClips(IReadOnlyList<IEmbeddedTimelineClipBinding> clips) => throw ReadOnly();
            public void SplitClip(IEmbeddedTimelineClipBinding clip, double time) => throw ReadOnly();
            public void MoveTrack(IEmbeddedTimelineTrackBinding track, int index) => throw ReadOnly();
            public void ConfigureSection(IEmbeddedTimelineSectionBinding section, string name, double time) => throw ReadOnly();
            public void DeleteSection(IEmbeddedTimelineSectionBinding section) => throw ReadOnly();
            public void AddSection(double time) => throw ReadOnly();
            public void CopyClip(IEmbeddedTimelineClipBinding clip) => throw ReadOnly();
            public void PasteClip(IEmbeddedTimelineTrackBinding track, double time) => throw ReadOnly();
            public void BeginEdit(string undoName) => throw ReadOnly();
            public void CommitEdit() => throw ReadOnly();
            public void CancelEdit() => throw ReadOnly();

            sealed class Group : IEmbeddedTimelineGroupBinding
            {
                internal readonly List<IEmbeddedTimelineTrackBinding> Items;
                internal Group(int capacity, string name) { Items = new List<IEmbeddedTimelineTrackBinding>(capacity); DisplayName = name; }
                public string AuthoringId => "execution";
                public string DisplayName { get; }
                public bool IsLocked { get => false; set => throw ReadOnly(); }
                public bool IsActive { get => true; set => throw ReadOnly(); }
                public bool IsCollapsed { get; set; }
                public IReadOnlyList<IEmbeddedTimelineTrackBinding> Tracks => Items;
            }

            sealed class Track : IEmbeddedTimelineTrackBinding, IReadOnlyList<IEmbeddedTimelineClipBinding>
            {
                readonly ExecutionBinding m_Owner;
                Clip m_First;
                Clip m_Last;
                Clip m_Cached;
                int m_CachedIndex;
                internal Track(ExecutionBinding owner, string id) { m_Owner = owner; AuthoringId = id; }
                internal void Reset(RuntimeExecutionSpan span)
                {
                    m_First = m_Last = m_Cached = null;
                    m_CachedIndex = 0;
                    Count = 0;
                    if (span.HasSource)
                    {
                        RuntimeDebugSession.Shared.TryResolveHistoricalSource(span.ContentRevision, span.SourceHandle,
                            out _, out DebugSourceMapEntry source);
                        DisplayName = source.DisplayName;
                    }
                    else
                        DisplayName = KindNames[(int)span.Kind];
                }
                internal void Append(Clip clip)
                {
                    clip.Previous = m_Last;
                    clip.Next = null;
                    if (m_Last != null) m_Last.Next = clip;
                    else m_First = m_Cached = clip;
                    m_Last = clip;
                    Count++;
                }
                internal bool EndsWithMissingStart => m_Last != null && m_Last.Span.State == RuntimeExecutionSpanState.MissingStart;
                public int Count { get; private set; }
                public IEmbeddedTimelineClipBinding this[int index]
                {
                    get
                    {
                        if (index == Count - 1) { m_Cached = m_Last; m_CachedIndex = index; }
                        while (m_CachedIndex < index) { m_Cached = m_Cached.Next; m_CachedIndex++; }
                        while (m_CachedIndex > index) { m_Cached = m_Cached.Previous; m_CachedIndex--; }
                        return m_Cached;
                    }
                }
                public IEnumerator<IEmbeddedTimelineClipBinding> GetEnumerator()
                {
                    for (Clip clip = m_First; clip != null; clip = clip.Next) yield return clip;
                }
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                public string AuthoringId { get; }
                public string DisplayName { get; private set; }
                public bool IsLocked { get => false; set => throw ReadOnly(); }
                public bool IsActive { get => true; set => throw ReadOnly(); }
                public bool ShowCurves { get; set; }
                public float CustomHeight { get; set; }
                public Color Color => m_First.Span.Kind switch
                {
                    RuntimeExecutionSpanKind.Wait => new Color(0.8f, 0.65f, 0.25f),
                    RuntimeExecutionSpanKind.Loop => new Color(0.8f, 0.45f, 0.65f),
                    RuntimeExecutionSpanKind.Branch => new Color(0.45f, 0.75f, 0.4f),
                    _ => new Color(0.3f, 0.65f, 0.85f)
                };
                public float StartTime => m_First.StartTime;
                public float EndTime => m_Owner.Length;
                public float DefaultHeight => 24;
                public float FinalHeight => Mathf.Max(DefaultHeight, CustomHeight);
                public float GetFinalHeight(string inspectedParameterId) => FinalHeight;
                public IReadOnlyList<IEmbeddedTimelineClipBinding> Clips => this;
                public IEmbeddedTimelineClipBinding SelectedClip => m_Owner.Selected is Clip clip && clip.OwnerTrack == this ? clip : null;
            }

            sealed class Clip : IEmbeddedTimelineClipBinding
            {
                readonly ExecutionBinding m_Owner;
                readonly int m_Index;
                internal Track OwnerTrack;
                internal Clip Previous;
                internal Clip Next;
                internal Clip(ExecutionBinding owner, int index, string id) { m_Owner = owner; m_Index = index; AuthoringId = id; }
                internal RuntimeExecutionSpan Span => m_Owner.m_Timeline.Spans[m_Index];
                public string AuthoringId { get; }
                public string DisplayName => OwnerTrack.DisplayName;
                public bool IsLocked { get => false; set => throw ReadOnly(); }
                public IEmbeddedTimelineTrackBinding Track => OwnerTrack;
                public string Info => Span.Kind == RuntimeExecutionSpanKind.Branch ? Span.Start.Payload.Detail :
                    Span.State == RuntimeExecutionSpanState.MissingStart ? "缺少进入记录" :
                    Span.IsOpen ? Span.Kind == RuntimeExecutionSpanKind.Wait || Span.End.Kind == RuntimeTraceEventKind.NodeWaiting
                        ? "等待中" : "执行中" :
                    string.IsNullOrEmpty(Span.End.Payload.Status) ? Span.End.Payload.Detail : Span.End.Payload.Status;
                public bool IsActive => true;
                public bool IsTimeQuantized => true;
                public bool IsValid => Span.HasSource;
                public bool IsCollapsed { get; set; }
                public float StartTime { get => Span.StartPosition; set => throw ReadOnly(); }
                public float EndTime { get => m_Owner.m_Timeline.GetObservedEndPosition(Span); set => throw ReadOnly(); }
                public float Length => EndTime - StartTime;
                public float BlendIn { get => 0; set => throw ReadOnly(); }
                public float BlendOut { get => 0; set => throw ReadOnly(); }
                public bool CanScale => false;
                public bool CanClipIn => false;
                public double ClipInTime { get => 0; set => throw ReadOnly(); }
                public bool CanBlendIn => false;
                public bool CanBlendOut => false;
                public IReadOnlyList<IEmbeddedTimelineParameterBinding> Parameters => Array.Empty<IEmbeddedTimelineParameterBinding>();
                public IReadOnlyList<IEmbeddedTimelineCurveBinding> Curves => Array.Empty<IEmbeddedTimelineCurveBinding>();
                public bool CanCrossBlend(IEmbeddedTimelineClipBinding other) => false;
                public void AddIdentityKey(float time) => throw ReadOnly();
                public void Split(float time) => throw ReadOnly();
                public void StretchFit() => throw ReadOnly();
                public void CleanKeysOffRange() => throw ReadOnly();
                public void ResetAnimation() => throw ReadOnly();
            }
        }
    }
}
