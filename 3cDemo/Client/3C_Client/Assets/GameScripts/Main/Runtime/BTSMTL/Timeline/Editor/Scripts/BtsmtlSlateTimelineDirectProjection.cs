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

    sealed class BtsmtlTimelineCurveTarget
    {
        public float AnimationWeight;
        public float AnimationEaseIn;
        public float AnimationEaseOut;
        public float MotionWeight;
        public float MotionPositionX;
        public float MotionPositionY;
        public float MotionPositionZ;
        public float MotionYaw;
        public float MotionEaseIn;
        public float MotionEaseOut;
        public float MotionWarpPositionProgress;
        public float MotionWarpYawProgress;
        public float CameraStateWeight;
        public float CameraStateEaseIn;
        public float CameraStateEaseOut;
        public float CameraResponseWeight;
        public float CameraResponseEaseIn;
        public float CameraResponseEaseOut;
        public float ScenePresentationValue;
    }

    sealed class BtsmtlTimelineDirectorBinding : IDirector
    {
        readonly BtsmtlSlateTimelineDirectProjection m_Owner;

        public BtsmtlTimelineDirectorBinding(BtsmtlSlateTimelineDirectProjection owner)
        {
            m_Owner = owner;
        }

        public IEnumerable<IDirectable> children => m_Owner.Groups.Cast<IDirectable>();
        public GameObject context => null;
        public float length => m_Owner.Length;
        public float currentTime
        {
            get => m_Owner.CurrentFrame / (float)m_Owner.FrameRate;
            set => m_Owner.CurrentFrame = Mathf.RoundToInt(value * m_Owner.FrameRate);
        }
        public float previousTime => currentTime;
        public float playbackSpeed { get; set; } = 1f;
        public bool isActive => false;
        public bool isPaused => false;
        public bool isReSampleFrame => false;
        public IEnumerable<GameObject> GetAffectedActors() => Array.Empty<GameObject>();
        public void Play() { }
        public void Pause() { }
        public void Stop() { }
        public void Sample(float time) => currentTime = time;
        public void ReSample() { }
        public void Validate() { }
        public void SendGlobalMessage(string message, object value) { }
    }

    sealed class BtsmtlTimelineGroupBinding : IEmbeddedTimelineGroupBinding, IDirectable
    {
        readonly BtsmtlSlateTimelineDirectProjection m_Owner;
        readonly List<BtsmtlTimelineTrackBinding> m_Tracks = new List<BtsmtlTimelineTrackBinding>();
        bool m_IsActive = true;
        bool m_IsCollapsed;
        bool m_IsLocked;

        public BtsmtlTimelineGroupBinding(BtsmtlSlateTimelineDirectProjection owner, string name)
        {
            m_Owner = owner;
            DisplayName = string.IsNullOrEmpty(name) ? "Timeline" : name;
        }

        public string AuthoringId => m_Owner.Timeline.AuthoringId;
        public string DisplayName { get; }
        public bool IsActive { get => m_IsActive; set => m_IsActive = value; }
        public bool IsCollapsed { get => m_IsCollapsed; set => m_IsCollapsed = value; }
        public bool IsLocked { get => m_IsLocked; set => m_IsLocked = value; }
        public IReadOnlyList<IEmbeddedTimelineTrackBinding> Tracks => m_Tracks;
        public BtsmtlSlateTimelineDirectProjection Owner => m_Owner;
        public void AddTrack(BtsmtlTimelineTrackBinding track) => m_Tracks.Add(track);

        IDirector IDirectable.root => m_Owner.Director;
        IDirectable IDirectable.parent => null;
        IEnumerable<IDirectable> IDirectable.children => m_Tracks.Cast<IDirectable>();
        GameObject IDirectable.actor => null;
        string IDirectable.name => DisplayName;
        bool IDirectable.isActive => IsActive;
        bool IDirectable.isCollapsed => IsCollapsed;
        bool IDirectable.isLocked => IsLocked;
        float IDirectable.startTime => 0f;
        float IDirectable.endTime => m_Owner.Length;
        float IDirectable.blendIn => 0f;
        float IDirectable.blendOut => 0f;
        bool IDirectable.canCrossBlend => false;
        void IDirectable.Validate(IDirector root, IDirectable parent) { }
        bool IDirectable.Initialize() => true;
        void IDirectable.Enter() { }
        void IDirectable.Exit() { }
        void IDirectable.Update(float time, float previousTime) { }
        void IDirectable.ReverseEnter() { }
        void IDirectable.Reverse() { }
        void IDirectable.RootEnabled() { }
        void IDirectable.RootUpdated(float time, float previousTime) { }
        void IDirectable.RootDisabled() { }
        void IDirectable.RootDestroyed() { }
#if UNITY_EDITOR
        void IDirectable.DrawGizmos(bool selected) { }
        void IDirectable.SceneGUI(bool selected) { }
#endif
    }

    sealed class BtsmtlTimelineTrackBinding : IEmbeddedTimelineTrackBinding, IDirectable
    {
        readonly BtsmtlSlateTimelineDirectProjection m_Owner;
        readonly BtsmtlTimelineGroupBinding m_Group;
        readonly Track m_Source;
        readonly List<BtsmtlTimelineClipBinding> m_Clips = new List<BtsmtlTimelineClipBinding>();
        bool m_IsActive = true;
        bool m_IsLocked;
        bool m_ShowCurves;

        public BtsmtlTimelineTrackBinding(
            BtsmtlSlateTimelineDirectProjection owner,
            BtsmtlTimelineGroupBinding group,
            Track source)
        {
            m_Owner = owner;
            m_Group = group;
            m_Source = source;
            DisplayName = string.IsNullOrEmpty(source.Name) ? source.ContractKind : source.Name;
            IsActive = !source.PersistentMuted;
        }

        public string AuthoringId => m_Source.AuthoringId;
        public string DisplayName { get; }
        public bool IsActive { get => m_IsActive; set => m_IsActive = value; }
        public bool IsLocked { get => m_IsLocked; set => m_IsLocked = value; }
        public bool ShowCurves { get => m_ShowCurves; set => m_ShowCurves = value; }
        public Color Color => m_Source.Color();
        public float StartTime => 0f;
        public float EndTime => m_Owner.Length;
        public float DefaultHeight => 32f;
        public float FinalHeight => ShowCurves ? 250f : DefaultHeight;
        public IReadOnlyList<IEmbeddedTimelineClipBinding> Clips => m_Clips;
        public Track Source => m_Source;
        public BtsmtlTimelineGroupBinding Group => m_Group;
        public void AddClip(BtsmtlTimelineClipBinding clip) => m_Clips.Add(clip);

        IDirector IDirectable.root => m_Owner.Director;
        IDirectable IDirectable.parent => m_Group;
        IEnumerable<IDirectable> IDirectable.children => m_Clips.Cast<IDirectable>();
        GameObject IDirectable.actor => null;
        string IDirectable.name => DisplayName;
        bool IDirectable.isActive => IsActive && m_Group.IsActive;
        bool IDirectable.isCollapsed => m_Group.IsCollapsed;
        bool IDirectable.isLocked => IsLocked || m_Group.IsLocked;
        float IDirectable.startTime => StartTime;
        float IDirectable.endTime => EndTime;
        float IDirectable.blendIn => 0f;
        float IDirectable.blendOut => 0f;
        bool IDirectable.canCrossBlend => false;
        void IDirectable.Validate(IDirector root, IDirectable parent) { }
        bool IDirectable.Initialize() => true;
        void IDirectable.Enter() { }
        void IDirectable.Exit() { }
        void IDirectable.Update(float time, float previousTime) { }
        void IDirectable.ReverseEnter() { }
        void IDirectable.Reverse() { }
        void IDirectable.RootEnabled() { }
        void IDirectable.RootUpdated(float time, float previousTime) { }
        void IDirectable.RootDisabled() { }
        void IDirectable.RootDestroyed() { }
#if UNITY_EDITOR
        void IDirectable.DrawGizmos(bool selected) { }
        void IDirectable.SceneGUI(bool selected) { }
#endif
    }

    sealed class BtsmtlTimelineClipBinding : IEmbeddedTimelineClipBinding, IKeyable
    {
        readonly BtsmtlSlateTimelineDirectProjection m_Owner;
        readonly BtsmtlTimelineTrackBinding m_Track;
        readonly Clip m_Source;
        readonly BtsmtlTimelineCurveTarget m_Target = new BtsmtlTimelineCurveTarget();
        readonly Dictionary<string, TimelineCurveChannelDescriptor> m_Descriptors =
            new Dictionary<string, TimelineCurveChannelDescriptor>(StringComparer.Ordinal);
        readonly Dictionary<string, float> m_CurveDurations =
            new Dictionary<string, float>(StringComparer.Ordinal);
        readonly AnimationDataCollection m_AnimationData;
        bool m_IsLocked;
        bool m_IsCollapsed;
        float m_StartTime;
        float m_EndTime;
        float m_BlendIn;
        float m_BlendOut;
        string m_RuntimeStatus = string.Empty;

        public BtsmtlTimelineClipBinding(
            BtsmtlSlateTimelineDirectProjection owner,
            BtsmtlTimelineTrackBinding track,
            Clip source)
        {
            m_Owner = owner;
            m_Track = track;
            m_Source = source;
            m_StartTime = source.StartFrame / (float)owner.FrameRate;
            m_EndTime = source.EndFrame / (float)owner.FrameRate;
            m_BlendIn = source.EaseInFrame / (float)owner.FrameRate;
            m_BlendOut = source.EaseOutFrame / (float)owner.FrameRate;
            m_AnimationData = new AnimationDataCollection();
            var descriptors = new List<TimelineCurveChannelDescriptor>();
            TimelineCurveChannelCatalog.CollectForTrack(source.Track, descriptors);
            for (int index = 0; index < descriptors.Count; index++)
            {
                TimelineCurveChannelDescriptor descriptor = descriptors[index];
                if (!descriptor.Supports(source))
                    continue;
                string parameterName = ParameterNameFor(descriptor.ChannelId);
                if (!m_AnimationData.TryAddParameter(this, typeof(BtsmtlTimelineCurveTarget), parameterName, string.Empty))
                    continue;
                AnimatedParameter parameter = m_AnimationData.GetParameterOfName(parameterName);
                if (parameter?.curves == null || parameter.curves.Length != 1)
                    continue;
                float duration = CurveDuration(source, descriptor);
                AnimationCurve curve = ConvertCurveTime(descriptor.Read(source), duration, false);
                parameter.curves[0].keys = curve.keys;
                parameter.curves[0].preWrapMode = curve.preWrapMode;
                parameter.curves[0].postWrapMode = curve.postWrapMode;
                m_Descriptors[descriptor.ChannelId.Value] = descriptor;
                m_CurveDurations[descriptor.ChannelId.Value] = duration;
            }
        }

        public Clip Source => m_Source;
        public BtsmtlTimelineTrackBinding Track => m_Track;
        public string AuthoringId => m_Source.AuthoringId;
        public string DisplayName => m_Source.Name;
        public string Info => string.IsNullOrEmpty(m_RuntimeStatus) ? DisplayName : $"{DisplayName} [{m_RuntimeStatus}]";
        public IKeyable Keyable => this;
        public bool IsActive => m_Track.IsActive;
        public bool IsValid => !m_Source.Invalid;
        public bool IsCollapsed { get => m_IsCollapsed; set => m_IsCollapsed = value; }
        public bool IsLocked { get => m_IsLocked; set => m_IsLocked = value; }
        public float StartTime { get => m_StartTime; set => m_StartTime = Mathf.Max(0f, value); }
        public float EndTime { get => m_EndTime; set => m_EndTime = Mathf.Max(StartTime + 1f / m_Owner.FrameRate, value); }
        public float Length => Mathf.Max(0f, EndTime - StartTime);
        public float BlendIn { get => Mathf.Clamp(m_BlendIn, 0f, Length); set => m_BlendIn = Mathf.Clamp(value, 0f, Length); }
        public float BlendOut { get => Mathf.Clamp(m_BlendOut, 0f, Length); set => m_BlendOut = Mathf.Clamp(value, 0f, Length); }
        public bool CanScale => m_Source.IsResizable();
        public bool CanBlendIn => m_Source.IsMixable();
        public bool CanBlendOut => m_Source.IsMixable();
        public AnimationDataCollection AnimationData => m_AnimationData;
        public IReadOnlyDictionary<string, TimelineCurveChannelDescriptor> Descriptors => m_Descriptors;
        public IReadOnlyDictionary<string, float> CurveDurations => m_CurveDurations;
        public void SetRuntimeStatus(string value) => m_RuntimeStatus = value ?? string.Empty;
        public void DrawClipGUI(Rect rect) { }
        public void DrawClipGUIExternal(Rect leftRect, Rect rightRect) { }
        public void ApplyCurveEdits()
        {
            foreach (KeyValuePair<string, AnimationCurve> pair in CaptureCurves())
            {
                if (m_Descriptors.TryGetValue(pair.Key, out TimelineCurveChannelDescriptor descriptor))
                    descriptor.Replace(m_Source, pair.Value);
            }
            m_Source.Track.UpdateMix();
        }
        public bool CanCrossBlend(IEmbeddedTimelineClipBinding other) => other != null && other.GetType() == GetType() && m_Source.IsMixable();
        public void AddIdentityKey(float time) => m_AnimationData.TryKeyIdentity(Mathf.Clamp(time, 0f, Length));
        public void Split(float time) { }
        public void StretchFit()
        {
            StartTime = 0f;
            EndTime = m_Track.EndTime;
        }
        public void CleanKeysOffRange()
        {
            AnimationCurve[] curves = m_AnimationData.GetCurvesAll();
            for (int curveIndex = 0; curveIndex < curves.Length; curveIndex++)
                curves[curveIndex].RemoveKeysOffRange(0f, Length);
        }
        public void ResetAnimation() => m_AnimationData.Reset();
        public Dictionary<string, AnimationCurve> CaptureCurves()
        {
            var result = new Dictionary<string, AnimationCurve>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, TimelineCurveChannelDescriptor> pair in m_Descriptors)
            {
                AnimatedParameter parameter = m_AnimationData.GetParameterOfName(ParameterNameFor(pair.Value.ChannelId));
                if (parameter?.curves != null && parameter.curves.Length == 1)
                    result[pair.Key] = ConvertCurveTime(parameter.curves[0], m_CurveDurations[pair.Key], true);
            }
            return result;
        }

        IDirector IDirectable.root => m_Owner.Director;
        IDirectable IDirectable.parent => m_Track;
        IEnumerable<IDirectable> IDirectable.children => Array.Empty<IDirectable>();
        GameObject IDirectable.actor => null;
        string IDirectable.name => DisplayName;
        bool IDirectable.isActive => IsActive;
        bool IDirectable.isCollapsed => IsCollapsed;
        bool IDirectable.isLocked => IsLocked || m_Track.IsLocked;
        float IDirectable.startTime => StartTime;
        float IDirectable.endTime => EndTime;
        float IDirectable.blendIn => BlendIn;
        float IDirectable.blendOut => BlendOut;
        bool IDirectable.canCrossBlend => CanBlendIn || CanBlendOut;
        void IDirectable.Validate(IDirector root, IDirectable parent) { }
        bool IDirectable.Initialize() => true;
        void IDirectable.Enter() { }
        void IDirectable.Exit() { }
        void IDirectable.Update(float time, float previousTime) { }
        void IDirectable.ReverseEnter() { }
        void IDirectable.Reverse() { }
        void IDirectable.RootEnabled() { }
        void IDirectable.RootUpdated(float time, float previousTime) { }
        void IDirectable.RootDisabled() { }
        void IDirectable.RootDestroyed() { }
#if UNITY_EDITOR
        void IDirectable.DrawGizmos(bool selected) { }
        void IDirectable.SceneGUI(bool selected) { }
#endif
        AnimationDataCollection IKeyable.animationData => m_AnimationData;
        object IKeyable.animatedParametersTarget => m_Target;

        public static string ParameterNameFor(TimelineCurveChannelId channelId)
        {
            if (channelId == TimelineCurveChannelCatalog.AnimationWeight) return nameof(BtsmtlTimelineCurveTarget.AnimationWeight);
            if (channelId == TimelineCurveChannelCatalog.AnimationEaseIn) return nameof(BtsmtlTimelineCurveTarget.AnimationEaseIn);
            if (channelId == TimelineCurveChannelCatalog.AnimationEaseOut) return nameof(BtsmtlTimelineCurveTarget.AnimationEaseOut);
            if (channelId == TimelineCurveChannelCatalog.MotionWeight) return nameof(BtsmtlTimelineCurveTarget.MotionWeight);
            if (channelId == TimelineCurveChannelCatalog.MotionPositionX) return nameof(BtsmtlTimelineCurveTarget.MotionPositionX);
            if (channelId == TimelineCurveChannelCatalog.MotionPositionY) return nameof(BtsmtlTimelineCurveTarget.MotionPositionY);
            if (channelId == TimelineCurveChannelCatalog.MotionPositionZ) return nameof(BtsmtlTimelineCurveTarget.MotionPositionZ);
            if (channelId == TimelineCurveChannelCatalog.MotionYaw) return nameof(BtsmtlTimelineCurveTarget.MotionYaw);
            if (channelId == TimelineCurveChannelCatalog.MotionEaseIn) return nameof(BtsmtlTimelineCurveTarget.MotionEaseIn);
            if (channelId == TimelineCurveChannelCatalog.MotionEaseOut) return nameof(BtsmtlTimelineCurveTarget.MotionEaseOut);
            if (channelId == TimelineCurveChannelCatalog.MotionWarpPositionProgress) return nameof(BtsmtlTimelineCurveTarget.MotionWarpPositionProgress);
            if (channelId == TimelineCurveChannelCatalog.MotionWarpYawProgress) return nameof(BtsmtlTimelineCurveTarget.MotionWarpYawProgress);
            if (channelId == TimelineCurveChannelCatalog.CameraStateWeight) return nameof(BtsmtlTimelineCurveTarget.CameraStateWeight);
            if (channelId == TimelineCurveChannelCatalog.CameraStateEaseIn) return nameof(BtsmtlTimelineCurveTarget.CameraStateEaseIn);
            if (channelId == TimelineCurveChannelCatalog.CameraStateEaseOut) return nameof(BtsmtlTimelineCurveTarget.CameraStateEaseOut);
            if (channelId == TimelineCurveChannelCatalog.CameraResponseWeight) return nameof(BtsmtlTimelineCurveTarget.CameraResponseWeight);
            if (channelId == TimelineCurveChannelCatalog.CameraResponseEaseIn) return nameof(BtsmtlTimelineCurveTarget.CameraResponseEaseIn);
            if (channelId == TimelineCurveChannelCatalog.CameraResponseEaseOut) return nameof(BtsmtlTimelineCurveTarget.CameraResponseEaseOut);
            if (channelId == TimelineCurveChannelCatalog.ScenePresentationValue) return nameof(BtsmtlTimelineCurveTarget.ScenePresentationValue);
            throw new InvalidOperationException($"Timeline curve channel '{channelId}' has no editor field.");
        }

        static float CurveDuration(Clip sourceClip, TimelineCurveChannelDescriptor descriptor)
        {
            if (sourceClip is MotionCurveClip motion &&
                (descriptor.ChannelId == TimelineCurveChannelCatalog.MotionPositionX ||
                 descriptor.ChannelId == TimelineCurveChannelCatalog.MotionPositionY ||
                 descriptor.ChannelId == TimelineCurveChannelCatalog.MotionPositionZ ||
                 descriptor.ChannelId == TimelineCurveChannelCatalog.MotionYaw))
                return Mathf.Max(1f / TimelineUtility.FrameRate, (motion.CurveEndFrame - motion.StartFrame) / (float)TimelineUtility.FrameRate);
            return Mathf.Max(1f / TimelineUtility.FrameRate, sourceClip.Duration / (float)TimelineUtility.FrameRate);
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
    }

    sealed class BtsmtlTimelineSectionBinding : IEmbeddedTimelineSectionBinding
    {
        readonly TimelineSection m_Source;
        public BtsmtlTimelineSectionBinding(TimelineSection source)
        {
            m_Source = source;
            Name = source.Name;
            Time = source.Frame / (float)TimelineUtility.FrameRate;
        }
        public string AuthoringId => m_Source.AuthoringId;
        public string DisplayName => Name;
        public bool IsLocked { get; set; }
        public string Name { get; set; }
        public float Time { get; set; }
        public Color Color => Color.white;
        public TimelineSection Source => m_Source;
    }

    public sealed class BtsmtlSlateTimelineDirectProjection : IDisposable, IEmbeddedTimelineBinding, ITimelineAuthoringClipResolver
    {
        readonly TimelineEditorOpenRequest m_Request;
        readonly TimelineEditorSessionContext m_Session;
        readonly Action<Clip> m_OpenSourceClip;
        readonly List<BtsmtlTimelineGroupBinding> m_Groups = new List<BtsmtlTimelineGroupBinding>();
        readonly List<BtsmtlTimelineSectionBinding> m_Sections = new List<BtsmtlTimelineSectionBinding>();
        readonly Dictionary<string, BtsmtlTimelineTrackBinding> m_Tracks = new Dictionary<string, BtsmtlTimelineTrackBinding>(StringComparer.Ordinal);
        readonly Dictionary<string, BtsmtlTimelineClipBinding> m_Clips = new Dictionary<string, BtsmtlTimelineClipBinding>(StringComparer.Ordinal);
        readonly Dictionary<string, BtsmtlTimelineSectionBinding> m_SectionsById = new Dictionary<string, BtsmtlTimelineSectionBinding>(StringComparer.Ordinal);
        readonly BtsmtlTimelineDirectorBinding m_Director;
        CutsceneEditorSurface m_EmbeddedEditor;
        TimelineEditorSelection m_Selection;
        BtsmtlTimelineClipBinding m_CopiedClip;
        string m_SourceRevision = string.Empty;
        string m_BeginSelectionId = string.Empty;
        DirectSnapshot m_BeginSnapshot;
        bool m_EditActive;
        bool m_Disposed;
        bool m_RebuildQueued;
        bool m_ReadOnly;
        float? m_RuntimeVisualTime;
        float? m_HistoryVisualTime;
        int m_CurrentFrame;
        float m_ViewTimeMin;
        float m_ViewTimeMax;

        sealed class DirectSnapshot
        {
            public readonly Dictionary<string, DirectClipSnapshot> Clips = new Dictionary<string, DirectClipSnapshot>(StringComparer.Ordinal);
            public readonly Dictionary<string, Dictionary<string, AnimationCurve>> Curves = new Dictionary<string, Dictionary<string, AnimationCurve>>(StringComparer.Ordinal);
            public readonly Dictionary<string, DirectSectionSnapshot> Sections = new Dictionary<string, DirectSectionSnapshot>(StringComparer.Ordinal);
            public readonly List<string> TrackOrder = new List<string>();
        }

        readonly struct DirectClipSnapshot
        {
            public DirectClipSnapshot(float startTime, float endTime, float blendIn, float blendOut, string trackId)
            {
                StartTime = startTime;
                EndTime = endTime;
                BlendIn = blendIn;
                BlendOut = blendOut;
                TrackId = trackId ?? string.Empty;
            }
            public float StartTime { get; }
            public float EndTime { get; }
            public float BlendIn { get; }
            public float BlendOut { get; }
            public string TrackId { get; }
        }

        readonly struct DirectSectionSnapshot
        {
            public DirectSectionSnapshot(string name, float time)
            {
                Name = name ?? string.Empty;
                Time = time;
            }
            public string Name { get; }
            public float Time { get; }
        }

        BtsmtlSlateTimelineDirectProjection(TimelineEditorOpenRequest request, Action<Clip> openSourceClip)
        {
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
            m_Session = new TimelineEditorSessionContext(request);
            m_OpenSourceClip = openSourceClip;
            m_Director = new BtsmtlTimelineDirectorBinding(this);
            m_ViewTimeMin = 0f;
            m_ViewTimeMax = Mathf.Max(1f / FrameRate, request.Timeline.Duration);
            try
            {
                BuildBindings();
                m_EmbeddedEditor = ScriptableObject.CreateInstance<CutsceneEditorSurface>();
                m_EmbeddedEditor.InitializeEmbedded(this, null);
                m_EmbeddedEditor.ConfigureEmbeddedRuntimeTime(() => m_RuntimeVisualTime);
                m_EmbeddedEditor.ConfigureEmbeddedHistoryTime(() => m_HistoryVisualTime);
                m_Request.Timeline.OnValueChanged += OnSourceTimelineChanged;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public TimelineData Timeline => m_Request.Timeline;
        internal BtsmtlTimelineDirectorBinding Director => m_Director;
        internal IReadOnlyList<BtsmtlTimelineGroupBinding> Groups => m_Groups;
        public string DisplayName => Timeline.Name;
        public int FrameRate => Mathf.Max(1, m_Session.FrameRate);
        public float Length => Timeline.Duration;
        public int CurrentFrame
        {
            get => m_CurrentFrame;
            set
            {
                m_CurrentFrame = Mathf.Clamp(value, 0, Timeline.MaxFrame);
                m_EmbeddedEditor?.RequestEmbeddedRepaint();
            }
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
        public IReadOnlyList<IEmbeddedTimelineGroupBinding> GroupsForSurface => m_Groups;
        public IReadOnlyList<IEmbeddedTimelineSectionBinding> Sections => m_Sections;
        public IEmbeddedTimelineElementBinding Selected =>
            m_Selection.Clip != null ? m_Clips.TryGetValue(m_Selection.Clip.AuthoringId, out BtsmtlTimelineClipBinding clip) ? clip : null :
            m_Selection.Track != null ? m_Tracks.TryGetValue(m_Selection.Track.AuthoringId, out BtsmtlTimelineTrackBinding track) ? track : null :
            m_Selection.Section != null ? m_SectionsById.TryGetValue(m_Selection.Section.AuthoringId, out BtsmtlTimelineSectionBinding section) ? section : null : null;
        IReadOnlyList<IEmbeddedTimelineGroupBinding> IEmbeddedTimelineBinding.Groups => m_Groups;
        IReadOnlyList<IEmbeddedTimelineSectionBinding> IEmbeddedTimelineBinding.Sections => m_Sections;

        public event Action<TimelineEditorSelection> SelectionChanged;
        public event Action<string> AuthoringIssue;

        public static bool TryOpen(
            TimelineEditorOpenRequest request,
            Action<Clip> openSourceClip,
            out BtsmtlSlateTimelineDirectProjection projection,
            out string unavailableReason)
        {
            projection = null;
            unavailableReason = string.Empty;
            try
            {
                projection = new BtsmtlSlateTimelineDirectProjection(request, openSourceClip);
                return true;
            }
            catch (Exception exception)
            {
                unavailableReason = exception.Message;
                return false;
            }
        }

        public TimelineEditorSelection Selection => m_Selection;

        public void DrawEmbeddedGUI(float width, float height, Action beginWindows, Action endWindows)
        {
            if (!m_Disposed)
                m_EmbeddedEditor?.DrawEmbeddedGUI(width, height, beginWindows, endWindows);
        }

        public void ConfigureRepaint(Action repaint)
        {
            m_EmbeddedEditor?.ConfigureEmbeddedRepaint(repaint);
        }

        public BtsmtlSlateTimelineViewState CaptureViewState()
        {
            return new BtsmtlSlateTimelineViewState(
                m_ViewTimeMin,
                m_ViewTimeMax,
                CurrentFrame / (float)FrameRate,
                m_EmbeddedEditor?.EmbeddedScrollPosition ?? Vector2.zero,
                m_Selection.Track?.AuthoringId,
                m_Selection.Clip?.AuthoringId,
                m_Groups.Count != 0 && m_Groups[0].IsCollapsed);
        }

        public void RestoreViewState(BtsmtlSlateTimelineViewState state)
        {
            if (state.ViewTimeMax > state.ViewTimeMin)
            {
                m_ViewTimeMin = state.ViewTimeMin;
                m_ViewTimeMax = state.ViewTimeMax;
            }
            CurrentFrame = Mathf.RoundToInt(state.CurrentTime * FrameRate);
            if (m_Groups.Count != 0)
                m_Groups[0].IsCollapsed = state.GroupCollapsed;
            if (m_EmbeddedEditor != null)
                m_EmbeddedEditor.EmbeddedScrollPosition = state.ScrollPosition;
            if (state.HasSelection)
                FocusSource(state.TrackAuthoringId, state.ClipAuthoringId);
        }

        public bool FocusSource(string trackAuthoringId, string clipAuthoringId)
        {
            if (!string.IsNullOrEmpty(clipAuthoringId) && m_Clips.TryGetValue(clipAuthoringId, out BtsmtlTimelineClipBinding clip))
            {
                Select(clip);
                return true;
            }
            if (!string.IsNullOrEmpty(trackAuthoringId) && m_Tracks.TryGetValue(trackAuthoringId, out BtsmtlTimelineTrackBinding track))
            {
                Select(track);
                return true;
            }
            return false;
        }

        public void ApplyFormalMutation(Action mutation, string undoName)
        {
            if (IsReadOnly)
            {
                ReportIssue("Timeline 当前只读，不能修改正式字段。");
                return;
            }
            try
            {
                m_Session.Apply(() =>
                {
                    mutation?.Invoke();
                    Timeline.Init();
                }, undoName);
                RebuildBindings();
            }
            catch (Exception exception)
            {
                ReportIssue($"字段修改失败：{exception.Message}");
            }
        }

        public void SetRuntimeReadOnly(bool readOnly)
        {
            m_ReadOnly = readOnly;
            m_EmbeddedEditor?.RequestEmbeddedRepaint();
        }

        public void ApplyRuntimeOverlay(float visualTime, IReadOnlyDictionary<string, string> activeTracks, IReadOnlyDictionary<string, string> activeClips)
        {
            m_RuntimeVisualTime = Mathf.Max(0f, visualTime);
            m_HistoryVisualTime = null;
            foreach (BtsmtlTimelineClipBinding clip in m_Clips.Values)
            {
                string status = string.Empty;
                activeClips?.TryGetValue(clip.AuthoringId, out status);
                clip.SetRuntimeStatus(status);
            }
            m_EmbeddedEditor?.RequestEmbeddedRepaint();
        }

        public void ClearRuntimeOverlay()
        {
            m_RuntimeVisualTime = null;
            m_HistoryVisualTime = null;
            foreach (BtsmtlTimelineClipBinding clip in m_Clips.Values)
                clip.SetRuntimeStatus(string.Empty);
            m_EmbeddedEditor?.RequestEmbeddedRepaint();
        }

        public void ApplyHistoryOverlay(float visualTime, IReadOnlyDictionary<string, string> activeTracks, IReadOnlyDictionary<string, string> activeClips)
        {
            m_HistoryVisualTime = Mathf.Max(0f, visualTime);
            m_RuntimeVisualTime = null;
            foreach (BtsmtlTimelineClipBinding clip in m_Clips.Values)
            {
                string status = string.Empty;
                activeClips?.TryGetValue(clip.AuthoringId, out status);
                clip.SetRuntimeStatus(status);
            }
            m_EmbeddedEditor?.RequestEmbeddedRepaint();
        }

        public void ApplyAuthoringPreviewTime(float time)
        {
            CurrentFrame = Mathf.RoundToInt(Mathf.Clamp(time, 0f, Length) * FrameRate);
        }

        public bool TryResolveMotionClip(TimelineData timeline, string identity, out MotionCurveClip clip)
        {
            clip = timeline?.Tracks.SelectMany(track => track.Clips).OfType<MotionCurveClip>().FirstOrDefault(value => string.Equals(value.AuthoringId, identity, StringComparison.Ordinal));
            return clip != null;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            if (m_RebuildQueued)
                EditorApplication.delayCall -= RebuildBindings;
            m_RebuildQueued = false;
            m_Request.Timeline.OnValueChanged -= OnSourceTimelineChanged;
            m_EmbeddedEditor?.ClearEmbedded();
            if (m_EmbeddedEditor != null)
                UnityEngine.Object.DestroyImmediate(m_EmbeddedEditor);
            m_EmbeddedEditor = null;
            m_Session.Dispose();
            SelectionChanged = null;
            AuthoringIssue = null;
            m_Groups.Clear();
            m_Tracks.Clear();
            m_Clips.Clear();
            m_Sections.Clear();
            m_SectionsById.Clear();
        }

        public void Select(IEmbeddedTimelineElementBinding element)
        {
            object target = element switch
            {
                BtsmtlTimelineClipBinding clip => clip.Source,
                BtsmtlTimelineTrackBinding track => track.Source,
                BtsmtlTimelineSectionBinding section => section.Source,
                _ => null
            };
            m_Session.SetSelection(target);
            m_Selection = m_Session.Selection;
            if (element is BtsmtlTimelineClipBinding selectedClip)
            {
                foreach (BtsmtlTimelineTrackBinding track in m_Tracks.Values)
                    track.ShowCurves = ReferenceEquals(track, selectedClip.Track);
            }
            SelectionChanged?.Invoke(m_Selection);
            m_EmbeddedEditor?.RequestEmbeddedRepaint();
        }

        public void AddTrack()
        {
            if (IsReadOnly)
                return;
            GenericMenu menu = new GenericMenu();
            foreach (TimelineTrackContract contract in m_Request.ContractCatalog.Tracks)
            {
                TimelineTrackContract candidate = contract;
                Type trackType = TimelineAuthoringTypeCatalog.RequireTrackType(candidate.Kind);
                bool requiresFields = trackType.GetCustomAttributes(typeof(TimelineAuthoringTrackFieldAttribute), true).Length > 0;
                menu.AddItem(new GUIContent(DisplayKind(candidate.Kind)), false, () => ShowTrackCreationPopup(candidate.Kind, requiresFields));
            }
            menu.ShowAsContext();
        }

        public void AddClip(IEmbeddedTimelineTrackBinding track, int frame)
        {
            if (IsReadOnly || !(track is BtsmtlTimelineTrackBinding directTrack))
                return;
            ShowAddClipMenu(directTrack.Source.AuthoringId, frame / (float)FrameRate);
        }

        public void DeleteTrack(IEmbeddedTimelineTrackBinding track)
        {
            if (IsReadOnly || !(track is BtsmtlTimelineTrackBinding directTrack))
                return;
            if (!m_Request.Timeline.Tracks.Contains(directTrack.Source))
                return;
            m_Session.Apply(() =>
            {
                m_Request.Timeline.RemoveTrack(directTrack.Source);
                Timeline.Init();
            }, "Delete Timeline Track");
            RebuildBindings();
        }

        public void DeleteClip(IEmbeddedTimelineClipBinding clip)
        {
            if (IsReadOnly || !(clip is BtsmtlTimelineClipBinding directClip))
                return;
            m_Session.Apply(() =>
            {
                m_Request.Timeline.RemoveClip(directClip.Source);
                Timeline.Init();
            }, "Delete Timeline Clip");
            RebuildBindings();
        }

        public void DeleteClips(IReadOnlyList<IEmbeddedTimelineClipBinding> clips)
        {
            if (IsReadOnly || clips == null)
                return;
            BtsmtlTimelineClipBinding[] directClips = clips
                .OfType<BtsmtlTimelineClipBinding>()
                .Where(value => value?.Source != null)
                .ToArray();
            if (directClips.Length == 0)
                return;
            m_Session.Apply(() =>
            {
                for (int index = 0; index < directClips.Length; index++)
                    Timeline.RemoveClip(directClips[index].Source);
                Timeline.Init();
            }, "Delete Timeline Clips");
            m_BeginSelectionId = string.Empty;
            RebuildBindings();
        }

        public void SplitClip(IEmbeddedTimelineClipBinding clip, int frame)
        {
            if (IsReadOnly || !(clip is BtsmtlTimelineClipBinding directClip))
                return;
            Clip source = directClip.Source;
            if (frame <= source.StartFrame || frame >= source.EndFrame || source.Track == null)
                return;
            try
            {
                Clip clone = ManagedReferenceCloneUtility.Clone(source);
                clone.RegenerateAuthoringIdentity();
                if (clone is ITimelineOwnedAuthoringIdentity owned)
                    owned.RegenerateOwnedAuthoringIdentity();
                int originalStartFrame = source.StartFrame;
                int originalEndFrame = source.EndFrame;
                float splitRatio = Mathf.Clamp01(
                    (frame - originalStartFrame) / (float)Mathf.Max(1, originalEndFrame - originalStartFrame));
                m_Session.Apply(() =>
                {
                    source.EndFrame = frame;
                    source.SelfEaseOutFrame = 0;
                    clone.StartFrame = frame;
                    clone.EndFrame = originalEndFrame;
                    clone.SelfEaseInFrame = 0;
                    clone.SelfEaseOutFrame = 0;
                    if (clone is MotionCurveClip cloneMotion)
                        cloneMotion.CurveEndFrame = Mathf.Clamp(cloneMotion.CurveEndFrame, clone.StartFrame + 1, clone.EndFrame);
                    SplitClipCurves(source, clone, splitRatio);
                    m_Request.ContractCatalog.RequireClipPlacement(source.Track, clone);
                    source.Track.Clips.Add(clone);
                    source.Track.UpdateMix();
                    Timeline.Init();
                }, "Split Timeline Clip");
                m_BeginSelectionId = clone.AuthoringId;
                RebuildBindings();
            }
            catch (Exception exception)
            {
                ReportIssue($"Split Clip 失败：{exception.Message}");
                RebuildBindings();
            }
        }

        static void SplitClipCurves(Clip source, Clip clone, float splitRatio)
        {
            if (splitRatio <= 0f || splitRatio >= 1f)
                return;
            var descriptors = new List<TimelineCurveChannelDescriptor>();
            TimelineCurveChannelCatalog.CollectForTrack(source.Track, descriptors);
            for (int index = 0; index < descriptors.Count; index++)
            {
                TimelineCurveChannelDescriptor descriptor = descriptors[index];
                if (!descriptor.Supports(source) || !descriptor.Supports(clone))
                    continue;
                AnimationCurve sourceCurve = descriptor.Read(source);
                AnimationCurve first = SplitNormalizedCurve(sourceCurve, splitRatio, true);
                AnimationCurve second = SplitNormalizedCurve(sourceCurve, splitRatio, false);
                descriptor.Replace(source, first);
                descriptor.Replace(clone, second);
            }
        }

        static AnimationCurve SplitNormalizedCurve(AnimationCurve source, float splitRatio, bool firstHalf)
        {
            AnimationCurve result = new AnimationCurve
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
            var keys = new List<Keyframe>();
            Keyframe[] sourceKeys = source.keys;
            for (int index = 0; index < sourceKeys.Length; index++)
            {
                Keyframe key = sourceKeys[index];
                if (firstHalf ? key.time <= splitRatio : key.time >= splitRatio)
                {
                    float duration = firstHalf ? splitRatio : 1f - splitRatio;
                    float time = firstHalf ? key.time / duration : (key.time - splitRatio) / duration;
                    key.time = Mathf.Clamp01(time);
                    key.inTangent *= duration;
                    key.outTangent *= duration;
                    keys.Add(key);
                }
            }
            float splitValue = source.Evaluate(splitRatio);
            if (firstHalf)
                keys.Add(new Keyframe(1f, splitValue));
            else
                keys.Insert(0, new Keyframe(0f, splitValue));
            result.keys = keys.OrderBy(key => key.time).ToArray();
            result.UpdateTangentsFromMode();
            return result;
        }

        public void MoveTrack(IEmbeddedTimelineTrackBinding track, int index)
        {
            if (IsReadOnly || !(track is BtsmtlTimelineTrackBinding directTrack))
                return;
            int currentIndex = Timeline.Tracks.IndexOf(directTrack.Source);
            if (currentIndex < 0 || index < 0 || index >= Timeline.Tracks.Count || currentIndex == index)
                return;
            m_Session.Apply(() =>
            {
                Timeline.Tracks.RemoveAt(currentIndex);
                Timeline.Tracks.Insert(index, directTrack.Source);
                Timeline.Init();
            }, "Move Timeline Track");
            m_BeginSelectionId = directTrack.AuthoringId;
            RebuildBindings();
        }

        public void MoveClip(IEmbeddedTimelineClipBinding clip, int startFrame)
        {
            if (IsReadOnly || !(clip is BtsmtlTimelineClipBinding directClip))
                return;
            int duration = Mathf.Max(1, directClip.Source.Duration);
            m_Session.Apply(() =>
            {
                directClip.Source.StartFrame = Mathf.Max(0, startFrame);
                directClip.Source.EndFrame = directClip.Source.StartFrame + duration;
                directClip.Source.Track.UpdateMix();
                Timeline.Init();
            }, "Move Timeline Clip");
            m_BeginSelectionId = directClip.AuthoringId;
            RebuildBindings();
        }

        public void ConfigureSection(IEmbeddedTimelineSectionBinding section, string name, int frame)
        {
            if (IsReadOnly || !(section is BtsmtlTimelineSectionBinding directSection))
                return;
            m_Session.Apply(() =>
            {
                directSection.Source.Configure(name, Mathf.Max(0, frame));
                Timeline.Init();
            }, "Edit Timeline Section");
            m_BeginSelectionId = directSection.AuthoringId;
            RebuildBindings();
        }

        public void DeleteSection(IEmbeddedTimelineSectionBinding section)
        {
            if (IsReadOnly || !(section is BtsmtlTimelineSectionBinding directSection))
                return;
            m_Session.Apply(() =>
            {
                Timeline.RemoveSection(directSection.Source);
                Timeline.Init();
            }, "Delete Timeline Section");
            m_BeginSelectionId = string.Empty;
            RebuildBindings();
        }

        public void AddSection(int frame)
        {
            if (IsReadOnly)
                return;
            try
            {
                string name = $"Section {Timeline.Sections.Count + 1}";
                m_Session.Apply(() =>
                {
                    Timeline.AddSection(name, Mathf.Max(0, frame));
                    Timeline.Init();
                }, "Add Timeline Section");
                m_BeginSelectionId = string.Empty;
                RebuildBindings();
            }
            catch (Exception exception)
            {
                ReportIssue($"新增 Section 失败：{exception.Message}");
            }
        }

        public void CopyClip(IEmbeddedTimelineClipBinding clip)
        {
            if (clip is BtsmtlTimelineClipBinding directClip)
                m_CopiedClip = directClip;
        }

        public void PasteClip(IEmbeddedTimelineTrackBinding track, int frame)
        {
            if (m_CopiedClip == null || !(track is BtsmtlTimelineTrackBinding directTrack))
                return;
            PasteClip(directTrack.Source.AuthoringId, frame);
        }

        public void OpenSource(IEmbeddedTimelineClipBinding clip)
        {
            if (clip is BtsmtlTimelineClipBinding directClip)
                m_OpenSourceClip?.Invoke(directClip.Source);
        }

        public void BeginEdit(string undoName)
        {
            if (m_EditActive || IsReadOnly)
                return;
            m_EditActive = true;
            m_BeginSnapshot = CaptureSnapshot();
            m_BeginSelectionId = Selected?.AuthoringId ?? string.Empty;
        }

        public void CommitEdit()
        {
            if (!m_EditActive)
                return;
            m_EditActive = false;
            DirectSnapshot end = CaptureSnapshot();
            if (!HasChanges(m_BeginSnapshot, end))
                return;
            try
            {
                ApplySnapshotDiff(m_BeginSnapshot, end);
            }
            catch (Exception exception)
            {
                ReportIssue($"Timeline 编辑提交失败：{exception.Message}");
                RebuildBindings();
            }
        }

        public void CancelEdit()
        {
            if (!m_EditActive)
                return;
            m_EditActive = false;
            RebuildBindings();
        }

        public void RequestRepaint() => m_EmbeddedEditor?.RequestEmbeddedRepaint();

        void BuildBindings()
        {
            m_SourceRevision = TimelineAuthoringFingerprint.Compute(Timeline);
            m_Groups.Clear();
            m_Tracks.Clear();
            m_Clips.Clear();
            m_Sections.Clear();
            m_SectionsById.Clear();
            var group = new BtsmtlTimelineGroupBinding(this, Timeline.Name);
            m_Groups.Add(group);
            for (int trackIndex = 0; trackIndex < Timeline.Tracks.Count; trackIndex++)
            {
                Track sourceTrack = Timeline.Tracks[trackIndex];
                if (sourceTrack == null)
                    continue;
                var directTrack = new BtsmtlTimelineTrackBinding(this, group, sourceTrack);
                group.AddTrack(directTrack);
                m_Tracks[sourceTrack.AuthoringId] = directTrack;
                for (int clipIndex = 0; clipIndex < sourceTrack.Clips.Count; clipIndex++)
                {
                    Clip sourceClip = sourceTrack.Clips[clipIndex];
                    if (sourceClip == null)
                        continue;
                    var directClip = new BtsmtlTimelineClipBinding(this, directTrack, sourceClip);
                    directTrack.AddClip(directClip);
                    m_Clips[sourceClip.AuthoringId] = directClip;
                }
            }
            for (int sectionIndex = 0; sectionIndex < Timeline.Sections.Count; sectionIndex++)
            {
                TimelineSection sourceSection = Timeline.Sections[sectionIndex];
                if (sourceSection == null)
                    continue;
                var directSection = new BtsmtlTimelineSectionBinding(sourceSection);
                m_Sections.Add(directSection);
                m_SectionsById[sourceSection.AuthoringId] = directSection;
            }
            if (!string.IsNullOrEmpty(m_BeginSelectionId))
            {
                if (m_Clips.TryGetValue(m_BeginSelectionId, out BtsmtlTimelineClipBinding clip))
                    Select(clip);
                else if (m_Tracks.TryGetValue(m_BeginSelectionId, out BtsmtlTimelineTrackBinding track))
                    Select(track);
            }
        }

        DirectSnapshot CaptureSnapshot()
        {
            var snapshot = new DirectSnapshot();
            for (int trackIndex = 0; trackIndex < Timeline.Tracks.Count; trackIndex++)
            {
                Track sourceTrack = Timeline.Tracks[trackIndex];
                if (sourceTrack == null)
                    continue;
                snapshot.TrackOrder.Add(sourceTrack.AuthoringId);
            }
            foreach (KeyValuePair<string, BtsmtlTimelineClipBinding> pair in m_Clips)
            {
                BtsmtlTimelineClipBinding clip = pair.Value;
                snapshot.Clips[pair.Key] = new DirectClipSnapshot(clip.StartTime, clip.EndTime, clip.BlendIn, clip.BlendOut, clip.Source.Track.AuthoringId);
                snapshot.Curves[pair.Key] = clip.CaptureCurves();
            }
            foreach (BtsmtlTimelineSectionBinding section in m_Sections)
                snapshot.Sections[section.AuthoringId] = new DirectSectionSnapshot(section.Name, section.Time);
            return snapshot;
        }

        bool HasChanges(DirectSnapshot before, DirectSnapshot after)
        {
            if (!before.TrackOrder.SequenceEqual(after.TrackOrder))
                return true;
            if (!before.Clips.Keys.SequenceEqual(after.Clips.Keys) || !before.Sections.Keys.SequenceEqual(after.Sections.Keys))
                return true;
            foreach (KeyValuePair<string, DirectClipSnapshot> pair in after.Clips)
            {
                if (!before.Clips.TryGetValue(pair.Key, out DirectClipSnapshot oldValue) ||
                    Mathf.Abs(oldValue.StartTime - pair.Value.StartTime) > 0.00001f ||
                    Mathf.Abs(oldValue.EndTime - pair.Value.EndTime) > 0.00001f ||
                    Mathf.Abs(oldValue.BlendIn - pair.Value.BlendIn) > 0.00001f ||
                    Mathf.Abs(oldValue.BlendOut - pair.Value.BlendOut) > 0.00001f)
                    return true;
                if (CurvesChanged(before.Curves[pair.Key], after.Curves[pair.Key]))
                    return true;
            }
            foreach (KeyValuePair<string, DirectSectionSnapshot> pair in after.Sections)
            {
                if (!before.Sections.TryGetValue(pair.Key, out DirectSectionSnapshot oldValue) ||
                    !string.Equals(oldValue.Name, pair.Value.Name, StringComparison.Ordinal) ||
                    Mathf.Abs(oldValue.Time - pair.Value.Time) > 0.00001f)
                    return true;
            }
            return false;
        }

        static bool CurvesChanged(Dictionary<string, AnimationCurve> before, Dictionary<string, AnimationCurve> after)
        {
            if (before.Count != after.Count)
                return true;
            foreach (KeyValuePair<string, AnimationCurve> pair in after)
            {
                if (!before.TryGetValue(pair.Key, out AnimationCurve oldCurve) ||
                    TimelineCurveAuthoring.Revision(oldCurve) != TimelineCurveAuthoring.Revision(pair.Value))
                    return true;
            }
            return false;
        }

        void ApplySnapshotDiff(DirectSnapshot before, DirectSnapshot after)
        {
            if (!string.Equals(m_SourceRevision, TimelineAuthoringFingerprint.Compute(Timeline), StringComparison.Ordinal))
            {
                ReportIssue("Timeline owner 已在外部修改，本次 Slate 编辑已取消。");
                RebuildBindings();
                return;
            }
            m_Session.Apply(() =>
            {
                foreach (KeyValuePair<string, DirectClipSnapshot> pair in after.Clips)
                {
                    if (!before.Clips.TryGetValue(pair.Key, out DirectClipSnapshot oldValue) ||
                        !m_Clips.TryGetValue(pair.Key, out BtsmtlTimelineClipBinding directClip))
                        continue;
                    Clip source = directClip.Source;
                    source.StartFrame = Mathf.Max(0, Mathf.RoundToInt(pair.Value.StartTime * FrameRate));
                    source.EndFrame = Mathf.Max(source.StartFrame + 1, Mathf.RoundToInt(pair.Value.EndTime * FrameRate));
                    source.SelfEaseInFrame = Mathf.Clamp(Mathf.RoundToInt(pair.Value.BlendIn * FrameRate), 0, source.Duration - 1);
                    source.SelfEaseOutFrame = Mathf.Clamp(Mathf.RoundToInt(pair.Value.BlendOut * FrameRate), 0, source.Duration - source.SelfEaseInFrame - 1);
                    if (CurvesChanged(before.Curves[pair.Key], after.Curves[pair.Key]))
                    {
                        foreach (KeyValuePair<string, AnimationCurve> curvePair in after.Curves[pair.Key])
                        {
                            if (directClip.Descriptors.TryGetValue(curvePair.Key, out TimelineCurveChannelDescriptor descriptor))
                                descriptor.Replace(source, curvePair.Value);
                        }
                    }
                    source.Track.UpdateMix();
                }
                Timeline.Init();
            }, "Slate Timeline Edit");
            m_BeginSelectionId = Selected?.AuthoringId ?? string.Empty;
            RebuildBindings();
        }

        void QueueRebuildBindings()
        {
            if (m_RebuildQueued || m_Disposed)
                return;
            m_RebuildQueued = true;
            EditorApplication.delayCall += RebuildBindings;
        }

        void RebuildBindings()
        {
            m_RebuildQueued = false;
            if (m_Disposed)
                return;
            string selectedId = Selected?.AuthoringId ?? m_BeginSelectionId;
            m_BeginSelectionId = selectedId;
            BuildBindings();
            m_EmbeddedEditor?.RequestEmbeddedRepaint();
        }

        void OnSourceTimelineChanged()
        {
            if (!m_EditActive)
                QueueRebuildBindings();
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

        bool AddTrack(string kind, string name, string channelId, string slotId)
        {
            if (IsReadOnly || !IsSourceCurrent())
                return false;
            try
            {
                string authoringId = string.Empty;
                m_Session.Apply(() =>
                {
                    int count = Timeline.Tracks.Count;
                    Type trackType = TimelineAuthoringTypeCatalog.RequireTrackType(kind);
                    Timeline.AddTrack(trackType, m_Request.ContractCatalog);
                    Track track = Timeline.Tracks[count];
                    track.Name = string.IsNullOrWhiteSpace(name) ? DisplayKind(kind) : name.Trim();
                    TimelineAuthoringTrackBinding.Apply(track, channelId, slotId);
                    authoringId = track.AuthoringId;
                    Timeline.Init();
                }, "Add Timeline Track");
                m_BeginSelectionId = authoringId;
                RebuildBindings();
                return true;
            }
            catch (Exception exception)
            {
                ReportIssue($"新增 Track 失败：{exception.Message}");
                return false;
            }
        }

        void ShowAddClipMenu(string trackAuthoringId, float time)
        {
            if (!m_Tracks.TryGetValue(trackAuthoringId, out BtsmtlTimelineTrackBinding track) ||
                !m_Request.ContractCatalog.TryGetTrack(track.Source.ContractKind, out TimelineTrackContract contract))
                return;
            int frame = Mathf.Max(0, Mathf.RoundToInt(time * FrameRate));
            GenericMenu menu = new GenericMenu();
            for (int index = 0; index < contract.AllowedClipKinds.Count; index++)
            {
                string kind = contract.AllowedClipKinds[index];
                menu.AddItem(new GUIContent(DisplayKind(kind)), false, () => ShowClipCreationPopup(trackAuthoringId, kind, frame));
            }
            if (m_CopiedClip != null && contract.AllowsClip(m_CopiedClip.Source.ContractKind))
            {
                menu.AddSeparator("/");
                menu.AddItem(new GUIContent("Paste Formal Clip"), false, () => PasteClip(trackAuthoringId, frame));
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
                StartFrame = frame,
                EndFrame = frame + Mathf.Max(1, FrameRate / 20),
                CurveEndFrame = frame + Mathf.Max(1, FrameRate / 20)
            };
            PopupWindow.Show(
                new Rect(0, 0, 1, 1),
                new TimelineClipCreationPopup(request, motionClipIds, Timeline.ExternalBindings, AddClip));
        }

        bool AddClip(TimelineClipCreationRequest request)
        {
            if (IsReadOnly || !IsSourceCurrent() || !m_Tracks.TryGetValue(request.TrackAuthoringId, out BtsmtlTimelineTrackBinding directTrack))
                return false;
            Clip addedClip = null;
            try
            {
                string authoringId = string.Empty;
                m_Session.Apply(() =>
                {
                    Track track = directTrack.Source;
                    if (request.Kind == TimelineContractKinds.AnimationClip)
                        addedClip = TimelineAuthoringTrackBinding.CreateClip(Timeline, m_Request.ContractCatalog, track, request.Resource as UnityEngine.AnimationClip, request.StartFrame);
                    else
                        addedClip = request.Resource != null
                            ? Timeline.AddClip(m_Request.ContractCatalog, request.Resource, track, request.StartFrame)
                            : Timeline.AddClip(m_Request.ContractCatalog, track, request.StartFrame);
                    addedClip.EndFrame = Mathf.Max(request.StartFrame + 1, request.EndFrame);
                    if (addedClip is MotionCurveClip motion)
                        motion.CurveEndFrame = Mathf.Clamp(request.CurveEndFrame, motion.StartFrame + 1, motion.EndFrame);
                    TimelineAuthoringClipBinding.Configure(Timeline, addedClip, BuildClipConfiguration(addedClip, request), this);
                    addedClip.Track.UpdateMix();
                    Timeline.Init();
                    authoringId = addedClip.AuthoringId;
                }, "Add Timeline Clip");
                m_BeginSelectionId = authoringId;
                RebuildBindings();
                return true;
            }
            catch (Exception exception)
            {
                if (addedClip != null)
                    Timeline.RemoveClip(addedClip);
                ReportIssue($"新增 Clip 失败：{exception.Message}");
                return false;
            }
        }

        void PasteClip(string trackAuthoringId, int frame)
        {
            if (m_CopiedClip == null || !m_Tracks.TryGetValue(trackAuthoringId, out BtsmtlTimelineTrackBinding targetTrack) || IsReadOnly)
                return;
            m_Session.Apply(() =>
            {
                Clip clone = ManagedReferenceCloneUtility.Clone(m_CopiedClip.Source);
                clone.RegenerateAuthoringIdentity();
                if (clone is ITimelineOwnedAuthoringIdentity owned)
                    owned.RegenerateOwnedAuthoringIdentity();
                clone.StartFrame = Mathf.Max(0, frame);
                clone.EndFrame = clone.StartFrame + Mathf.Max(1, m_CopiedClip.Source.Duration);
                m_Request.ContractCatalog.RequireClipPlacement(targetTrack.Source, clone);
                targetTrack.Source.Clips.Add(clone);
                Timeline.Init();
            }, "Paste Timeline Clip");
            RebuildBindings();
        }

        bool IsSourceCurrent()
        {
            if (string.Equals(m_SourceRevision, TimelineAuthoringFingerprint.Compute(Timeline), StringComparison.Ordinal))
                return true;
            ReportIssue("Timeline owner 已在外部修改，当前操作已取消。");
            QueueRebuildBindings();
            return false;
        }

        static string DisplayKind(string kind)
        {
            int separator = kind.LastIndexOf('.');
            string value = separator >= 0 ? kind.Substring(0, separator) : kind;
            return value.Replace('-', ' ');
        }

        static TimelineAuthoringClipConfiguration BuildClipConfiguration(Clip clip, TimelineClipCreationRequest request)
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

        void ReportIssue(string message) => AuthoringIssue?.Invoke(message ?? string.Empty);
    }
}
#endif
