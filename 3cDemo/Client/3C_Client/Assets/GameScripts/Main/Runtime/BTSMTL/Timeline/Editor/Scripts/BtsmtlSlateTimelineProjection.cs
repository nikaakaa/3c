#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using Newtonsoft.Json.Linq;
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

    readonly struct BtsmtlSlateCurveBinding
    {
        public BtsmtlSlateCurveBinding(string channelId, string parameterName, AnimationCurve curve, float duration)
        {
            ChannelId = channelId ?? string.Empty;
            ParameterName = parameterName ?? string.Empty;
            Curve = curve ?? throw new ArgumentNullException(nameof(curve));
            Duration = Mathf.Max(0.0001f, duration);
        }

        public string ChannelId { get; }
        public string ParameterName { get; }
        public AnimationCurve Curve { get; }
        public float Duration { get; }
    }

    [AddComponentMenu("")]
    sealed class BtsmtlSlateGroup : CutsceneGroup
    {
        [SerializeField] string m_DisplayName = "Timeline";
        [SerializeField] GameObject m_Actor;
        [SerializeField] ActorReferenceMode m_ReferenceMode = ActorReferenceMode.UseOriginal;
        [SerializeField] ActorInitialTransformation m_InitialTransformation = ActorInitialTransformation.UseOriginal;

        public override string name
        {
            get => m_DisplayName;
            set => m_DisplayName = value ?? string.Empty;
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
        Action<float> m_AddClip;

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

        public void ConfigureAuthoringMenu(Action<float> addClip)
        {
            m_AddClip = addClip;
        }

#if UNITY_EDITOR
        public override void OnTrackTimelineGUI(
            Rect posRect,
            Rect timeRect,
            float cursorTime,
            Func<float, float> timeToPosition)
        {
            Event e = Event.current;
            Rect clipsRect = Rect.MinMaxRect(posRect.xMin, posRect.yMin, posRect.xMax, posRect.yMin + defaultHeight);
            if (m_AddClip != null && e.type == EventType.ContextClick && clipsRect.Contains(e.mousePosition))
            {
                m_AddClip(cursorTime);
                e.Use();
            }
            base.OnTrackTimelineGUI(posRect, timeRect, cursorTime, timeToPosition);
        }
#endif
    }

    [AddComponentMenu("")]
    sealed class BtsmtlSlateActionClip : ActionClip
    {
        [SerializeField] string m_DisplayName = "Clip";
        [SerializeField] float m_Length = 1f;
        [SerializeField] float m_BlendIn;
        [SerializeField] float m_BlendOut;
        [SerializeField] string m_SourceAuthoringId;
        string m_RuntimeStatus = string.Empty;
        readonly Dictionary<string, BtsmtlSlateCurveBinding> m_CurveBindings =
            new Dictionary<string, BtsmtlSlateCurveBinding>(StringComparer.Ordinal);
        readonly Dictionary<string, BtsmtlSlateCurveBinding> m_CurveBindingsByParameter =
            new Dictionary<string, BtsmtlSlateCurveBinding>(StringComparer.Ordinal);

        [AnimatableParameter("Animation Weight", 0f, 1f)]
        public float AnimationWeight;
        [AnimatableParameter("Animation Ease In", 0f, 1f)]
        public float AnimationEaseIn;
        [AnimatableParameter("Animation Ease Out", 0f, 1f)]
        public float AnimationEaseOut;
        [AnimatableParameter("Motion Weight", 0f, 1f)]
        public float MotionWeight;
        [AnimatableParameter("Motion Position X")]
        public float MotionPositionX;
        [AnimatableParameter("Motion Position Y")]
        public float MotionPositionY;
        [AnimatableParameter("Motion Position Z")]
        public float MotionPositionZ;
        [AnimatableParameter("Motion Yaw")]
        public float MotionYaw;
        [AnimatableParameter("Motion Ease In", 0f, 1f)]
        public float MotionEaseIn;
        [AnimatableParameter("Motion Ease Out", 0f, 1f)]
        public float MotionEaseOut;
        [AnimatableParameter("Motion Warp Position Progress", 0f, 1f)]
        public float MotionWarpPositionProgress;
        [AnimatableParameter("Motion Warp Yaw Progress", 0f, 1f)]
        public float MotionWarpYawProgress;
        [AnimatableParameter("Camera State Weight", 0f, 1f)]
        public float CameraStateWeight;
        [AnimatableParameter("Camera State Ease In", 0f, 1f)]
        public float CameraStateEaseIn;
        [AnimatableParameter("Camera State Ease Out", 0f, 1f)]
        public float CameraStateEaseOut;
        [AnimatableParameter("Camera Response Weight", 0f, 1f)]
        public float CameraResponseWeight;
        [AnimatableParameter("Camera Response Ease In", 0f, 1f)]
        public float CameraResponseEaseIn;
        [AnimatableParameter("Camera Response Ease Out", 0f, 1f)]
        public float CameraResponseEaseOut;

        public override float length
        {
            get => Mathf.Max(0f, m_Length);
            set => m_Length = Mathf.Max(0f, value);
        }

        public override string info => string.IsNullOrEmpty(m_RuntimeStatus)
            ? m_DisplayName
            : $"{m_DisplayName} [{m_RuntimeStatus}]";

        public override float blendIn
        {
            get => Mathf.Clamp(m_BlendIn, 0f, length);
            set => m_BlendIn = Mathf.Clamp(value, 0f, length);
        }

        public override float blendOut
        {
            get => Mathf.Clamp(m_BlendOut, 0f, length);
            set => m_BlendOut = Mathf.Clamp(value, 0f, length);
        }

        public string SourceAuthoringId => m_SourceAuthoringId ?? string.Empty;

        public override bool isValid => true;

        public IReadOnlyCollection<string> CurveChannelIds => m_CurveBindings.Keys;

        public void Configure(string displayName, float duration, float blendIn = 0f, float blendOut = 0f)
        {
            m_DisplayName = string.IsNullOrEmpty(displayName) ? "Clip" : displayName;
            length = duration;
            this.blendIn = blendIn;
            this.blendOut = blendOut;
        }

        public void ConfigureSource(string sourceAuthoringId)
        {
            m_SourceAuthoringId = sourceAuthoringId ?? string.Empty;
        }

        public void ConfigureCurves(IReadOnlyList<BtsmtlSlateCurveBinding> bindings)
        {
            m_CurveBindings.Clear();
            m_CurveBindingsByParameter.Clear();
            if (bindings == null)
                return;
            for (int index = 0; index < bindings.Count; index++)
            {
                BtsmtlSlateCurveBinding binding = bindings[index];
                if (string.IsNullOrEmpty(binding.ChannelId) || string.IsNullOrEmpty(binding.ParameterName))
                    continue;
                m_CurveBindings[binding.ChannelId] = binding;
                m_CurveBindingsByParameter[binding.ParameterName] = binding;
            }
        }

        public bool TryGetCurve(string channelId, out AnimationCurve curve)
        {
            curve = null;
            if (!m_CurveBindings.TryGetValue(channelId ?? string.Empty, out BtsmtlSlateCurveBinding binding))
                return false;
            AnimatedParameter parameter = GetParameter(binding.ParameterName);
            if (parameter == null || parameter.curves == null || parameter.curves.Length != 1)
                return false;
            curve = parameter.curves[0];
            return curve != null;
        }

        public bool TryGetCurveDuration(string channelId, out float duration)
        {
            duration = length;
            return m_CurveBindings.TryGetValue(channelId ?? string.Empty, out BtsmtlSlateCurveBinding binding) &&
                   (duration = binding.Duration) > 0f;
        }

        protected override void OnCreate()
        {
            SyncConfiguredCurves(true);
        }

        protected override void OnAfterValidate()
        {
            SyncConfiguredCurves(false);
        }

        void SyncConfiguredCurves(bool copyCurves)
        {
            if (animationData == null || animationData.animatedParameters == null)
                return;
            for (int index = animationData.animatedParameters.Count - 1; index >= 0; index--)
            {
                AnimatedParameter parameter = animationData.animatedParameters[index];
                if (!m_CurveBindingsByParameter.TryGetValue(parameter.parameterName, out BtsmtlSlateCurveBinding binding))
                {
                    animationData.RemoveParameter(parameter);
                    continue;
                }
                if (parameter.curves == null || parameter.curves.Length != 1 || parameter.curves[0] == null)
                    continue;
                if (copyCurves)
                {
                    AnimationCurve target = parameter.curves[0];
                    target.keys = binding.Curve.keys;
                    target.preWrapMode = binding.Curve.preWrapMode;
                    target.postWrapMode = binding.Curve.postWrapMode;
                }
            }
        }

        public void SetRuntimeStatus(string status)
        {
            m_RuntimeStatus = status ?? string.Empty;
        }

        public static string ParameterNameFor(TimelineCurveChannelId channelId)
        {
            if (channelId == TimelineCurveChannelCatalog.AnimationWeight)
                return nameof(AnimationWeight);
            if (channelId == TimelineCurveChannelCatalog.AnimationEaseIn)
                return nameof(AnimationEaseIn);
            if (channelId == TimelineCurveChannelCatalog.AnimationEaseOut)
                return nameof(AnimationEaseOut);
            if (channelId == TimelineCurveChannelCatalog.MotionWeight)
                return nameof(MotionWeight);
            if (channelId == TimelineCurveChannelCatalog.MotionPositionX)
                return nameof(MotionPositionX);
            if (channelId == TimelineCurveChannelCatalog.MotionPositionY)
                return nameof(MotionPositionY);
            if (channelId == TimelineCurveChannelCatalog.MotionPositionZ)
                return nameof(MotionPositionZ);
            if (channelId == TimelineCurveChannelCatalog.MotionYaw)
                return nameof(MotionYaw);
            if (channelId == TimelineCurveChannelCatalog.MotionEaseIn)
                return nameof(MotionEaseIn);
            if (channelId == TimelineCurveChannelCatalog.MotionEaseOut)
                return nameof(MotionEaseOut);
            if (channelId == TimelineCurveChannelCatalog.MotionWarpPositionProgress)
                return nameof(MotionWarpPositionProgress);
            if (channelId == TimelineCurveChannelCatalog.MotionWarpYawProgress)
                return nameof(MotionWarpYawProgress);
            if (channelId == TimelineCurveChannelCatalog.CameraStateWeight)
                return nameof(CameraStateWeight);
            if (channelId == TimelineCurveChannelCatalog.CameraStateEaseIn)
                return nameof(CameraStateEaseIn);
            if (channelId == TimelineCurveChannelCatalog.CameraStateEaseOut)
                return nameof(CameraStateEaseOut);
            if (channelId == TimelineCurveChannelCatalog.CameraResponseWeight)
                return nameof(CameraResponseWeight);
            if (channelId == TimelineCurveChannelCatalog.CameraResponseEaseIn)
                return nameof(CameraResponseEaseIn);
            if (channelId == TimelineCurveChannelCatalog.CameraResponseEaseOut)
                return nameof(CameraResponseEaseOut);
            throw new InvalidOperationException($"Timeline curve channel '{channelId}' has no Slate proxy field.");
        }
    }

    public sealed class BtsmtlSlateTimelineProjection : IDisposable, ITimelineAuthoringClipResolver
    {
        readonly TimelineEditorOpenRequest m_Request;
        readonly TimelineEditorSessionContext m_Session;
        readonly Action<Clip> m_OpenSourceClip;
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
        string m_BeginSourceRevision;
        readonly Func<Cutscene, bool> m_UndoPolicy;
        readonly Func<Cutscene, bool> m_PlaybackPolicy;
        GameObject m_Host;
        Cutscene m_Cutscene;
        CutsceneEditorSurface m_EmbeddedEditor;
        bool m_Disposed;
        bool m_RebuildQueued;
        bool m_ReadOnly;
        string m_PendingTrackFocus;
        string m_PendingClipFocus;
        BtsmtlSlateTimelineViewState? m_PendingViewState;

        static BtsmtlSlateTimelineProjection s_Current;

        readonly struct ProxyClipSnapshot
        {
            public ProxyClipSnapshot(
                float startTime,
                float endTime,
                float blendIn,
                float blendOut,
                string trackAuthoringId)
            {
                StartTime = startTime;
                EndTime = endTime;
                BlendIn = blendIn;
                BlendOut = blendOut;
                TrackAuthoringId = trackAuthoringId ?? string.Empty;
            }

            public float StartTime { get; }
            public float EndTime { get; }
            public float BlendIn { get; }
            public float BlendOut { get; }
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

        readonly struct ProxyCurveSnapshot
        {
            public ProxyCurveSnapshot(AnimationCurve curve)
            {
                Curve = TimelineCurveAuthoring.CopyCurve(curve);
                Revision = TimelineCurveAuthoring.Revision(curve);
            }

            public AnimationCurve Curve { get; }
            public ulong Revision { get; }
        }

        sealed class ProjectionSnapshot
        {
            public readonly Dictionary<string, ProxyClipSnapshot> Clips =
                new Dictionary<string, ProxyClipSnapshot>(StringComparer.Ordinal);
            public readonly Dictionary<string, ProxySectionSnapshot> Sections =
                new Dictionary<string, ProxySectionSnapshot>(StringComparer.Ordinal);
            public readonly Dictionary<string, Dictionary<string, ProxyCurveSnapshot>> Curves =
                new Dictionary<string, Dictionary<string, ProxyCurveSnapshot>>(StringComparer.Ordinal);
            public readonly HashSet<string> Tracks = new HashSet<string>(StringComparer.Ordinal);
            public readonly List<string> TrackOrder = new List<string>();
            public bool Unsupported;
        }

        BtsmtlSlateTimelineProjection(TimelineEditorOpenRequest request, Action<Clip> openSourceClip)
        {
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
            m_Session = new TimelineEditorSessionContext(request);
            m_OpenSourceClip = openSourceClip;
            m_UndoPolicy = ShouldRecordUndo;
            m_PlaybackPolicy = cutscene => !ReferenceEquals(cutscene, m_Cutscene);
            try
            {
                BuildProjection();
                CreateEmbeddedEditor();
                CutsceneEditor.OnEditTransactionBegin += OnEditTransactionBegin;
                CutsceneEditor.OnEditTransactionCommit += OnEditTransactionCommit;
                CutsceneEditor.OnEditTransactionCancel += OnEditTransactionCancel;
                CutsceneEditor.OnActionDoubleClick += OnActionDoubleClick;
                CutsceneEditor.RecordUndoForCutscene = m_UndoPolicy;
                CutsceneEditor.AllowPlaybackForCutscene = m_PlaybackPolicy;
                m_Request.Timeline.OnValueChanged += OnSourceTimelineChanged;
                CutsceneUtility.onSelectionChange += OnSlateSelectionChanged;
                Undo.undoRedoEvent += OnUndoRedoEvent;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public Cutscene Cutscene => m_Cutscene;
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
                m_Session.Apply(() =>
                {
                    mutation?.Invoke();
                    m_Request.Timeline.Init();
                }, undoName);
                QueueRebuildProjection();
            }
            catch (Exception exception)
            {
                ReportIssue($"字段修改失败：{exception.Message}");
            }
        }

        public BtsmtlSlateTimelineViewState CaptureViewState()
        {
            TimelineEditorSelection selection = m_Session.Selection;
            return new BtsmtlSlateTimelineViewState(
                m_EmbeddedEditor != null ? m_EmbeddedEditor.viewTimeMin : 0f,
                m_EmbeddedEditor != null ? m_EmbeddedEditor.viewTimeMax : 0f,
                m_EmbeddedEditor != null && m_EmbeddedEditor.cutscene != null ? m_EmbeddedEditor.cutscene.currentTime : 0f,
                m_EmbeddedEditor != null ? m_EmbeddedEditor.EmbeddedScrollPosition : Vector2.zero,
                selection.Track?.AuthoringId,
                selection.Clip?.AuthoringId,
                m_Cutscene != null && m_Cutscene.groups.Count != 0 && m_Cutscene.groups[0].isCollapsed);
        }

        public void RestoreViewState(BtsmtlSlateTimelineViewState state)
        {
            if (m_EmbeddedEditor == null)
                return;
            if (state.ViewTimeMax > state.ViewTimeMin)
            {
                m_EmbeddedEditor.viewTimeMin = state.ViewTimeMin;
                m_EmbeddedEditor.viewTimeMax = state.ViewTimeMax;
            }
            if (m_EmbeddedEditor.cutscene != null)
                m_EmbeddedEditor.cutscene.currentTime = Mathf.Clamp(state.CurrentTime, 0f, m_EmbeddedEditor.cutscene.length);
            if (m_Cutscene != null && m_Cutscene.groups.Count != 0)
                m_Cutscene.groups[0].isCollapsed = state.GroupCollapsed;
            m_EmbeddedEditor.EmbeddedScrollPosition = state.ScrollPosition;
            if (state.HasSelection)
                FocusSource(state.TrackAuthoringId, state.ClipAuthoringId);
        }

        public void DrawEmbeddedGUI(float width, float height)
        {
            if (!m_Disposed)
                m_EmbeddedEditor?.DrawEmbeddedGUI(width, height);
        }

        public void DrawEmbeddedGUI(
            float width,
            float height,
            Action beginWindows,
            Action endWindows)
        {
            if (!m_Disposed)
                m_EmbeddedEditor?.DrawEmbeddedGUI(width, height, beginWindows, endWindows);
        }

        public bool FocusSource(string trackAuthoringId, string clipAuthoringId)
        {
            if (!string.IsNullOrEmpty(clipAuthoringId) &&
                m_ProxyClips.TryGetValue(clipAuthoringId, out BtsmtlSlateActionClip clip))
            {
                CutsceneUtility.selectedObject = clip;
                if (m_SourceClips.TryGetValue(clipAuthoringId, out Clip sourceClip))
                {
                    m_Session.SetSelection(sourceClip);
                    SelectionChanged?.Invoke(m_Session.Selection);
                }
                m_EmbeddedEditor?.RequestEmbeddedRepaint();
                return true;
            }
            if (!string.IsNullOrEmpty(trackAuthoringId) &&
                m_ProxyTracks.TryGetValue(trackAuthoringId, out BtsmtlSlateTrack track))
            {
                CutsceneUtility.selectedObject = track;
                if (m_SourceTracks.TryGetValue(trackAuthoringId, out Track sourceTrack))
                {
                    m_Session.SetSelection(sourceTrack);
                    SelectionChanged?.Invoke(m_Session.Selection);
                }
                m_EmbeddedEditor?.RequestEmbeddedRepaint();
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
            CutsceneGroup[] defaultGroups = m_Cutscene.groups.ToArray();
            m_Cutscene.groups.Clear();
            for (int index = 0; index < defaultGroups.Length; index++)
            {
                if (defaultGroups[index] != null)
                    UnityEngine.Object.DestroyImmediate(defaultGroups[index].gameObject);
            }
            float frameDuration = 1f / Mathf.Max(1, m_Session.FrameRate);
            m_Cutscene.length = Mathf.Max(frameDuration, m_Request.Timeline.Duration);
            m_Cutscene.viewTimeMin = 0f;
            m_Cutscene.viewTimeMax = Mathf.Max(m_Cutscene.length + frameDuration, frameDuration);

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
                var curveChannels = new List<TimelineCurveChannelDescriptor>();
                TimelineCurveChannelCatalog.CollectForTrack(sourceTrack, curveChannels);
                string trackDisplayName = sourceTrack.Name;
                GameObject trackObject = CreateChild(group.transform, trackDisplayName);
                BtsmtlSlateTrack proxyTrack = trackObject.AddComponent<BtsmtlSlateTrack>();
                proxyTrack.hideFlags = HideFlags.HideAndDontSave;
                proxyTrack.name = trackDisplayName;
                proxyTrack.Configure(sourceTrack.AuthoringId);
                proxyTrack.ConfigureAuthoringMenu(time => ShowAddClipMenu(sourceTrack.AuthoringId, time));
                group.tracks.Add(proxyTrack);
                proxyTrack.PostCreate(group);
                m_SourceTracks[sourceTrack.AuthoringId] = sourceTrack;
                m_ProxyTracks[sourceTrack.AuthoringId] = proxyTrack;

                for (int clipIndex = 0; clipIndex < sourceTrack.Clips.Count; clipIndex++)
                {
                    Clip sourceClip = sourceTrack.Clips[clipIndex];
                    if (sourceClip == null)
                        continue;
                    BtsmtlSlateActionClip proxyClip = trackObject.AddComponent<BtsmtlSlateActionClip>();
                    proxyClip.hideFlags = HideFlags.HideAndDontSave;
                    string displayName = sourceClip.Name;
                    proxyClip.Configure(
                        displayName,
                        sourceClip.Duration / (float)TimelineUtility.FrameRate,
                        sourceClip.SelfEaseInFrame / (float)TimelineUtility.FrameRate,
                        sourceClip.SelfEaseOutFrame / (float)TimelineUtility.FrameRate);
                    proxyClip.ConfigureSource(sourceClip.AuthoringId);
                    var curveBindings = new List<BtsmtlSlateCurveBinding>();
                    for (int curveIndex = 0; curveIndex < curveChannels.Count; curveIndex++)
                    {
                        TimelineCurveChannelDescriptor descriptor = curveChannels[curveIndex];
                        if (!descriptor.Supports(sourceClip))
                            continue;
                        curveBindings.Add(new BtsmtlSlateCurveBinding(
                            descriptor.ChannelId.Value,
                            BtsmtlSlateActionClip.ParameterNameFor(descriptor.ChannelId),
                            ToSlateCurve(descriptor.Read(sourceClip), CurveDuration(sourceClip, descriptor)),
                            CurveDuration(sourceClip, descriptor)));
                    }
                    proxyClip.ConfigureCurves(curveBindings);
                    proxyClip.startTime = sourceClip.StartFrame / (float)TimelineUtility.FrameRate;
                    proxyTrack.clips.Add(proxyClip);
                    proxyClip.PostCreate(proxyTrack);
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

        void CreateEmbeddedEditor()
        {
            m_EmbeddedEditor = ScriptableObject.CreateInstance<CutsceneEditorSurface>();
            m_EmbeddedEditor.InitializeEmbedded(m_Cutscene, null, () => m_Session.FrameRate, ShowAddTrackMenu);
            if (!string.IsNullOrEmpty(m_PendingTrackFocus) || !string.IsNullOrEmpty(m_PendingClipFocus))
            {
                FocusSource(m_PendingTrackFocus, m_PendingClipFocus);
                m_PendingTrackFocus = string.Empty;
                m_PendingClipFocus = string.Empty;
            }
        }

        void ShowAddTrackMenu()
        {
            GenericMenu menu = new GenericMenu();
            foreach (TimelineTrackContract contract in m_Request.ContractCatalog.Tracks)
            {
                TimelineTrackContract candidate = contract;
                Type trackType = TimelineAuthoringTypeCatalog.RequireTrackType(candidate.Kind);
                bool requiresFields = trackType
                    .GetCustomAttributes(typeof(TimelineAuthoringTrackFieldAttribute), true)
                    .Length > 0;
                string label = DisplayKind(candidate.Kind);
                menu.AddItem(new GUIContent(label), false, () => ShowTrackCreationPopup(candidate.Kind, requiresFields));
            }
            menu.ShowAsContext();
        }

        void ShowTrackCreationPopup(string kind, bool requiresFields)
        {
            PopupWindow.Show(
                new Rect(0, 0, 1, 1),
                new TimelineTrackCreationPopup(
                    kind,
                    DisplayKind(kind),
                    requiresFields,
                    (name, channelId, slotId) => AddTrack(kind, name, channelId, slotId)));
        }

        void AddTrack(string kind, string name, string channelId, string slotId)
        {
            Type trackType = TimelineAuthoringTypeCatalog.RequireTrackType(kind);
            string authoringId = string.Empty;
            Track addedTrack = null;
            try
            {
                m_Session.Apply(() =>
                {
                    int count = m_Request.Timeline.Tracks.Count;
                    m_Request.Timeline.AddTrack(trackType, m_Request.ContractCatalog);
                    addedTrack = m_Request.Timeline.Tracks[count];
                    authoringId = addedTrack.AuthoringId;
                    addedTrack.Name = string.IsNullOrWhiteSpace(name) ? DisplayKind(kind) : name.Trim();
                    TimelineAuthoringTrackBinding.Apply(addedTrack, channelId, slotId);
                }, "Add Timeline Track");
            }
            catch (Exception exception)
            {
                if (addedTrack != null)
                    m_Request.Timeline.RemoveTrack(addedTrack);
                ReportIssue($"新增 Track 失败：{exception.Message}");
                return;
            }
            m_PendingTrackFocus = authoringId;
            QueueRebuildProjection();
        }

        void ShowAddClipMenu(string trackAuthoringId, float time)
        {
            if (!m_SourceTracks.TryGetValue(trackAuthoringId, out Track track))
                return;
            if (!m_Request.ContractCatalog.TryGetTrack(track.ContractKind, out TimelineTrackContract trackContract))
                return;

            GenericMenu menu = new GenericMenu();
            int frame = Mathf.Max(0, Mathf.RoundToInt(time * m_Session.FrameRate));
            for (int index = 0; index < trackContract.AllowedClipKinds.Count; index++)
            {
                string kind = trackContract.AllowedClipKinds[index];
                if (kind == TimelineContractKinds.ScenePresentationParameterCurveClip)
                {
                    menu.AddDisabledItem(new GUIContent($"{DisplayKind(kind)} (需要正式绑定)"));
                    continue;
                }
                string clipKind = kind;
                menu.AddItem(new GUIContent(DisplayKind(clipKind)), false, () => ShowClipCreationPopup(trackAuthoringId, clipKind, frame));
            }
            menu.ShowAsContext();
        }

        void ShowClipCreationPopup(string trackAuthoringId, string kind, int frame)
        {
            var motionClipIds = m_Request.Timeline.Tracks
                .SelectMany(track => track.Clips)
                .OfType<MotionCurveClip>()
                .Select(clip => clip.AuthoringId)
                .ToArray();
            var request = new TimelineClipCreationRequest
            {
                TrackAuthoringId = trackAuthoringId,
                Kind = kind,
                StartFrame = frame,
                EndFrame = frame + Mathf.Max(1, m_Session.FrameRate / 20),
                CurveEndFrame = frame + Mathf.Max(1, m_Session.FrameRate / 20)
            };
            PopupWindow.Show(
                new Rect(0, 0, 1, 1),
                new TimelineClipCreationPopup(request, motionClipIds, AddClip));
        }

        void AddClip(TimelineClipCreationRequest request)
        {
            if (!m_SourceTracks.TryGetValue(request.TrackAuthoringId, out Track track))
                return;
            string authoringId = string.Empty;
            Clip addedClip = null;
            try
            {
                m_Session.Apply(() =>
                {
                    if (request.Kind == TimelineContractKinds.AnimationClip)
                    {
                        addedClip = TimelineAuthoringTrackBinding.CreateClip(
                            m_Request.Timeline,
                            m_Request.ContractCatalog,
                            track,
                            request.Resource as UnityEngine.AnimationClip,
                            request.StartFrame);
                    }
                    else
                    {
                        addedClip = request.Resource != null
                            ? m_Request.Timeline.AddClip(m_Request.ContractCatalog, request.Resource, track, request.StartFrame)
                            : m_Request.Timeline.AddClip(m_Request.ContractCatalog, track, request.StartFrame);
                    }
                    authoringId = addedClip.AuthoringId;
                    addedClip.EndFrame = Mathf.Max(request.StartFrame + 1, request.EndFrame);
                    if (addedClip is MotionCurveClip motion)
                    {
                        motion.CurveEndFrame = Mathf.Clamp(request.CurveEndFrame, motion.StartFrame + 1, motion.EndFrame);
                    }
                    TimelineAuthoringClipBinding.Apply(
                        m_Request.Timeline,
                        addedClip,
                        BuildClipProperties(request),
                        null,
                        this);
                    addedClip.Track.UpdateMix();
                    m_Request.Timeline.Init();
                }, "Add Timeline Clip");
            }
            catch (Exception exception)
            {
                if (addedClip != null)
                    m_Request.Timeline.RemoveClip(addedClip);
                ReportIssue($"新增 Clip 失败：{exception.Message}");
                return;
            }
            m_PendingTrackFocus = request.TrackAuthoringId;
            m_PendingClipFocus = authoringId;
            QueueRebuildProjection();
        }

        static JObject BuildClipProperties(TimelineClipCreationRequest request)
        {
            var properties = new JObject();
            if (request.Kind == TimelineContractKinds.AnimationClip)
            {
                properties["extraPolationMode"] = ExtraPolationMode.None.ToString();
                properties["blendProfileId"] = string.Empty;
            }
            if (request.Kind == TimelineContractKinds.MotionCurveClip)
            {
                properties["curveId"] = request.CurveId;
                properties["curveEndFrame"] = request.CurveEndFrame;
                properties["space"] = request.Space.ToString();
                properties["channel"] = request.Channel.ToString();
                properties["blendMode"] = request.BlendMode.ToString();
                properties["priority"] = request.Priority;
                properties["consumeLowerChannels"] = request.ConsumeLowerChannels;
            }
            if (request.Kind == TimelineContractKinds.MotionWarpClip)
                properties["sourceMotionClipId"] = request.SourceMotionClipId;
            if (request.Kind == TimelineContractKinds.ActionCueClip)
            {
                properties["cueId"] = request.CueId;
                properties["cueType"] = request.CueType;
            }
            return properties;
        }

        public bool TryResolveMotionClip(TimelineData timeline, string identity, out MotionCurveClip clip)
        {
            clip = timeline?.Tracks
                .SelectMany(track => track.Clips)
                .OfType<MotionCurveClip>()
                .FirstOrDefault(value => string.Equals(value.AuthoringId, identity, StringComparison.Ordinal));
            return clip != null;
        }

        static string DisplayKind(string kind)
        {
            int separator = kind.LastIndexOf('.');
            string value = separator >= 0 ? kind.Substring(0, separator) : kind;
            return value.Replace('-', ' ');
        }

        static AnimationCurve ToSlateCurve(AnimationCurve normalizedCurve, float duration)
        {
            return ConvertCurveTime(normalizedCurve, duration, false);
        }

        static float CurveDuration(Clip sourceClip, TimelineCurveChannelDescriptor descriptor)
        {
            if (sourceClip is MotionCurveClip motion &&
                (descriptor.ChannelId == TimelineCurveChannelCatalog.MotionPositionX ||
                 descriptor.ChannelId == TimelineCurveChannelCatalog.MotionPositionY ||
                 descriptor.ChannelId == TimelineCurveChannelCatalog.MotionPositionZ ||
                 descriptor.ChannelId == TimelineCurveChannelCatalog.MotionYaw))
            {
                return Mathf.Max(1f / TimelineUtility.FrameRate,
                    (motion.CurveEndFrame - motion.StartFrame) / (float)TimelineUtility.FrameRate);
            }
            return Mathf.Max(1f / TimelineUtility.FrameRate, sourceClip.Duration / (float)TimelineUtility.FrameRate);
        }

        static AnimationCurve ToAuthoringCurve(AnimationCurve slateCurve, float duration)
        {
            return ConvertCurveTime(slateCurve, duration, true);
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
            m_BeginSourceRevision = TimelineAuthoringFingerprint.Compute(m_Request.Timeline);
        }

        void OnEditTransactionCommit(Cutscene cutscene)
        {
            if (!ReferenceEquals(cutscene, m_Cutscene) || m_BeginSnapshot == null)
                return;
            if (!string.Equals(
                    m_BeginSourceRevision,
                    TimelineAuthoringFingerprint.Compute(m_Request.Timeline),
                    StringComparison.Ordinal))
            {
                m_BeginSnapshot = null;
                m_BeginSourceRevision = string.Empty;
                QueueRebuildProjection();
                return;
            }
            ProjectionSnapshot endSnapshot = CaptureSnapshot();
            ApplyDiff(m_BeginSnapshot, endSnapshot);
            m_BeginSnapshot = null;
            m_BeginSourceRevision = string.Empty;
            QueueRebuildProjection();
        }

        void OnEditTransactionCancel(Cutscene cutscene)
        {
            if (!ReferenceEquals(cutscene, m_Cutscene))
                return;
            m_BeginSnapshot = null;
            m_BeginSourceRevision = string.Empty;
            QueueRebuildProjection();
        }

        void OnActionDoubleClick(ActionClip action)
        {
            if (!(action is BtsmtlSlateActionClip proxyClip) || string.IsNullOrEmpty(proxyClip.SourceAuthoringId))
                return;
            if (m_SourceClips.TryGetValue(proxyClip.SourceAuthoringId, out Clip sourceClip))
                m_OpenSourceClip?.Invoke(sourceClip);
        }

        void OnSlateSelectionChanged(IDirectable directable)
        {
            if (directable is BtsmtlSlateActionClip proxyClip &&
                m_SourceClips.TryGetValue(proxyClip.SourceAuthoringId, out Clip sourceClip))
            {
                m_Session.SetSelection(sourceClip);
                SelectionChanged?.Invoke(m_Session.Selection);
                return;
            }
            if (directable is BtsmtlSlateTrack proxyTrack &&
                m_SourceTracks.TryGetValue(proxyTrack.SourceAuthoringId, out Track sourceTrack))
            {
                m_Session.SetSelection(sourceTrack);
                SelectionChanged?.Invoke(m_Session.Selection);
                return;
            }
            m_Session.SetSelection(null);
            SelectionChanged?.Invoke(m_Session.Selection);
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
                    if (!snapshot.Tracks.Add(proxyTrack.SourceAuthoringId))
                    {
                        snapshot.Unsupported = true;
                        continue;
                    }
                    snapshot.TrackOrder.Add(proxyTrack.SourceAuthoringId);
                    for (int clipIndex = 0; clipIndex < proxyTrack.clips.Count; clipIndex++)
                    {
                        if (!(proxyTrack.clips[clipIndex] is BtsmtlSlateActionClip proxyClip) ||
                            string.IsNullOrEmpty(proxyClip.SourceAuthoringId))
                        {
                            snapshot.Unsupported = true;
                            continue;
                        }
                        if (snapshot.Clips.ContainsKey(proxyClip.SourceAuthoringId))
                        {
                            snapshot.Unsupported = true;
                            continue;
                        }
                        snapshot.Clips[proxyClip.SourceAuthoringId] =
                            new ProxyClipSnapshot(
                                proxyClip.startTime,
                                proxyClip.endTime,
                                proxyClip.blendIn,
                                proxyClip.blendOut,
                                proxyTrack.SourceAuthoringId);
                        var clipCurves = new Dictionary<string, ProxyCurveSnapshot>(StringComparer.Ordinal);
                        foreach (string channelId in proxyClip.CurveChannelIds)
                        {
                            if (!proxyClip.TryGetCurve(channelId, out AnimationCurve curve))
                            {
                                snapshot.Unsupported = true;
                                continue;
                            }
                            if (!clipCurves.TryAdd(channelId, new ProxyCurveSnapshot(curve)))
                                snapshot.Unsupported = true;
                        }
                        snapshot.Curves[proxyClip.SourceAuthoringId] = clipCurves;
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
                    if (snapshot.Sections.ContainsKey(sourceId))
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
            if (m_ReadOnly)
            {
                ReportIssue("Timeline 当前只读，不能提交作者修改。");
                return;
            }
            if (end.Unsupported)
            {
                ReportIssue("该 Slate 操作没有正式 Timeline 映射，已丢弃。");
                return;
            }
            var changes = new List<(Clip Clip, int StartFrame, int EndFrame, int EaseInFrame, int EaseOutFrame)>();
            foreach (KeyValuePair<string, ProxyClipSnapshot> pair in end.Clips)
            {
                if (!begin.Clips.TryGetValue(pair.Key, out ProxyClipSnapshot before) ||
                    !m_SourceClips.TryGetValue(pair.Key, out Clip sourceClip))
                    continue;
                if (!string.Equals(before.TrackAuthoringId, FindProxyTrackAuthoringId(pair.Key), StringComparison.Ordinal))
                {
                    ReportIssue("Clip 所属 Track 已过期，已丢弃本次修改。");
                    return;
                }
                int startFrame = Mathf.Max(0, Mathf.RoundToInt(pair.Value.StartTime * TimelineUtility.FrameRate));
                int endFrame = Mathf.Max(startFrame + 1, Mathf.RoundToInt(pair.Value.EndTime * TimelineUtility.FrameRate));
                int beforeStartFrame = Mathf.RoundToInt(before.StartTime * TimelineUtility.FrameRate);
                int beforeEndFrame = Mathf.RoundToInt(before.EndTime * TimelineUtility.FrameRate);
                int easeInFrame = Mathf.Clamp(
                    Mathf.RoundToInt(pair.Value.BlendIn * TimelineUtility.FrameRate),
                    0,
                    Mathf.Max(0, endFrame - startFrame - 1));
                int easeOutFrame = Mathf.Clamp(
                    Mathf.RoundToInt(pair.Value.BlendOut * TimelineUtility.FrameRate),
                    0,
                    Mathf.Max(0, endFrame - startFrame - easeInFrame - 1));
                int beforeEaseInFrame = Mathf.RoundToInt(before.BlendIn * TimelineUtility.FrameRate);
                int beforeEaseOutFrame = Mathf.RoundToInt(before.BlendOut * TimelineUtility.FrameRate);
                if (startFrame != beforeStartFrame || endFrame != beforeEndFrame ||
                    easeInFrame != beforeEaseInFrame || easeOutFrame != beforeEaseOutFrame)
                    changes.Add((sourceClip, startFrame, endFrame, easeInFrame, easeOutFrame));
            }

            var curveChanges = new List<(Clip Clip, TimelineCurveChannelId ChannelId, AnimationCurve Curve)>();
            foreach (KeyValuePair<string, Dictionary<string, ProxyCurveSnapshot>> pair in end.Curves)
            {
                if (!begin.Curves.TryGetValue(pair.Key, out Dictionary<string, ProxyCurveSnapshot> beforeCurves) ||
                    !m_SourceClips.TryGetValue(pair.Key, out Clip sourceClip))
                    continue;
                foreach (KeyValuePair<string, ProxyCurveSnapshot> curvePair in pair.Value)
                {
                    if (!beforeCurves.TryGetValue(curvePair.Key, out ProxyCurveSnapshot beforeCurve) ||
                        beforeCurve.Revision == curvePair.Value.Revision)
                        continue;
                    TimelineCurveChannelId channelId = new TimelineCurveChannelId(curvePair.Key);
                    if (!TimelineCurveChannelCatalog.TryGet(curvePair.Key, out TimelineCurveChannelDescriptor descriptor) ||
                        !descriptor.Supports(sourceClip))
                    {
                        ReportIssue($"曲线通道 '{curvePair.Key}' 没有正式映射，已丢弃本次修改。");
                        return;
                    }
                    if (!m_ProxyClips.TryGetValue(pair.Key, out BtsmtlSlateActionClip proxyClip))
                    {
                        ReportIssue("曲线所属 Clip 已过期，已丢弃本次修改。");
                        return;
                    }
                    float curveDuration = proxyClip.TryGetCurveDuration(channelId.Value, out float mappedDuration)
                        ? mappedDuration
                        : proxyClip.length;
                    curveChanges.Add((sourceClip, channelId, ToAuthoringCurve(curvePair.Value.Curve, curveDuration)));
                }
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
            bool trackOrderChanged = !begin.TrackOrder.SequenceEqual(end.TrackOrder);

            if (changes.Count == 0 && curveChanges.Count == 0 && sectionChanges.Count == 0 &&
                removedTracks.Length == 0 && removedClips.Length == 0 && removedSections.Length == 0 &&
                !trackOrderChanged)
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
                    (Clip clip, int startFrame, int endFrame, int easeInFrame, int easeOutFrame) = changes[index];
                    clip.StartFrame = startFrame;
                    clip.EndFrame = endFrame;
                    clip.SelfEaseInFrame = easeInFrame;
                    clip.SelfEaseOutFrame = easeOutFrame;
                    clip.Track.UpdateMix();
                }
                for (int index = 0; index < curveChanges.Count; index++)
                {
                    (Clip clip, TimelineCurveChannelId channelId, AnimationCurve curve) = curveChanges[index];
                    TimelineCurveAuthoring.Replace(clip, channelId, curve);
                }
                for (int index = 0; index < sectionChanges.Count; index++)
                {
                    (TimelineSection section, string name, int frame) = sectionChanges[index];
                    m_Request.Timeline.ConfigureSection(section, name, frame);
                }
                if (trackOrderChanged)
                {
                    var orderedTracks = new List<Track>(end.TrackOrder.Count);
                    for (int index = 0; index < end.TrackOrder.Count; index++)
                    {
                        if (m_SourceTracks.TryGetValue(end.TrackOrder[index], out Track track) &&
                            !removedTracks.Contains(track))
                            orderedTracks.Add(track);
                    }
                    if (orderedTracks.Count != m_Request.Timeline.Tracks.Count)
                        throw new InvalidOperationException("Slate Timeline track identity is stale.");
                    m_Request.Timeline.Tracks.Clear();
                    m_Request.Timeline.Tracks.AddRange(orderedTracks);
                }
                m_Request.Timeline.Init();
            }, "Slate Timeline Edit");
        }

        void ReportIssue(string message)
        {
            AuthoringIssue?.Invoke(message ?? string.Empty);
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
            m_EmbeddedEditor?.RequestEmbeddedRepaint();
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
            m_EmbeddedEditor?.RequestEmbeddedRepaint();
        }

        public void ClearRuntimeOverlay()
        {
            foreach (BtsmtlSlateTrack track in m_ProxyTracks.Values)
                track.SetRuntimeActive(true);
            foreach (BtsmtlSlateActionClip clip in m_ProxyClips.Values)
                clip.SetRuntimeStatus(string.Empty);
            m_EmbeddedEditor?.RequestEmbeddedRepaint();
        }

        public void ApplyAuthoringPreviewTime(float time)
        {
            if (m_Cutscene == null)
                return;
            m_Cutscene.currentTime = Mathf.Clamp(time, 0f, m_Cutscene.length);
            m_EmbeddedEditor?.RequestEmbeddedRepaint();
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
            BtsmtlSlateTimelineViewState? viewState = m_EmbeddedEditor != null
                ? CaptureViewState()
                : m_PendingViewState;
            m_SourceClips.Clear();
            m_ProxyClips.Clear();
            m_SourceTracks.Clear();
            m_ProxyTracks.Clear();
            m_SourceSections.Clear();
            m_ProxySections.Clear();
            if (m_EmbeddedEditor != null)
            {
                m_EmbeddedEditor.ClearEmbedded();
                UnityEngine.Object.DestroyImmediate(m_EmbeddedEditor);
                m_EmbeddedEditor = null;
            }
            foreach (Transform child in m_Host.transform.Cast<Transform>().ToArray())
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            if (m_Cutscene != null)
                UnityEngine.Object.DestroyImmediate(m_Cutscene);
            m_Host = null;
            m_Cutscene = null;
            BuildProjection();
            CreateEmbeddedEditor();
            if (viewState.HasValue)
                RestoreViewState(viewState.Value);
            m_PendingViewState = null;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            CutsceneEditor.OnEditTransactionBegin -= OnEditTransactionBegin;
            CutsceneEditor.OnEditTransactionCommit -= OnEditTransactionCommit;
            CutsceneEditor.OnEditTransactionCancel -= OnEditTransactionCancel;
            CutsceneEditor.OnActionDoubleClick -= OnActionDoubleClick;
            m_Request.Timeline.OnValueChanged -= OnSourceTimelineChanged;
            CutsceneUtility.onSelectionChange -= OnSlateSelectionChanged;
            Undo.undoRedoEvent -= OnUndoRedoEvent;
            if (ReferenceEquals(CutsceneEditor.RecordUndoForCutscene, m_UndoPolicy))
                CutsceneEditor.RecordUndoForCutscene = null;
            if (ReferenceEquals(CutsceneEditor.AllowPlaybackForCutscene, m_PlaybackPolicy))
                CutsceneEditor.AllowPlaybackForCutscene = null;
            if (m_EmbeddedEditor != null)
            {
                m_EmbeddedEditor.ClearEmbedded();
                UnityEngine.Object.DestroyImmediate(m_EmbeddedEditor);
                m_EmbeddedEditor = null;
            }
            m_Session.Dispose();
            SelectionChanged = null;
            AuthoringIssue = null;
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
