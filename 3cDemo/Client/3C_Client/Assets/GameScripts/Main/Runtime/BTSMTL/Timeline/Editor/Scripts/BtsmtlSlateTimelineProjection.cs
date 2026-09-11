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
    readonly struct BtsmtlSlateCurveBinding
    {
        public BtsmtlSlateCurveBinding(string channelId, string parameterName, AnimationCurve curve)
        {
            ChannelId = channelId ?? string.Empty;
            ParameterName = parameterName ?? string.Empty;
            Curve = curve ?? throw new ArgumentNullException(nameof(curve));
        }

        public string ChannelId { get; }
        public string ParameterName { get; }
        public AnimationCurve Curve { get; }
    }

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
            name = m_DisplayName;
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

    public sealed class BtsmtlSlateTimelineProjection : IDisposable
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
        readonly Func<Cutscene, bool> m_UndoPolicy;
        readonly Func<Cutscene, bool> m_PlaybackPolicy;
        GameObject m_Host;
        Cutscene m_Cutscene;
        bool m_Disposed;
        bool m_RebuildQueued;
        bool m_EditorClosed;
        bool m_ReadOnly;

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
                CutsceneEditor.OnEditTransactionBegin += OnEditTransactionBegin;
                CutsceneEditor.OnEditTransactionCommit += OnEditTransactionCommit;
                CutsceneEditor.OnEditTransactionCancel += OnEditTransactionCancel;
                CutsceneEditor.OnEditorClosed += OnEditorClosed;
                CutsceneEditor.OnActionDoubleClick += OnActionDoubleClick;
                CutsceneEditor.RecordUndoForCutscene = m_UndoPolicy;
                CutsceneEditor.AllowPlaybackForCutscene = m_PlaybackPolicy;
                m_Request.Timeline.OnValueChanged += OnSourceTimelineChanged;
                Undo.undoRedoEvent += OnUndoRedoEvent;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public Cutscene Cutscene => m_Cutscene;

        public void FocusEditorWindow()
        {
            if (m_Disposed || m_Cutscene == null)
                return;
            if (CutsceneEditor.current == null ||
                !ReferenceEquals(CutsceneEditor.current.cutscene, m_Cutscene))
                CutsceneEditor.ShowWindow(m_Cutscene);
            else
                CutsceneEditor.current.Focus();
        }

        public bool FocusSource(string trackAuthoringId, string clipAuthoringId)
        {
            if (!string.IsNullOrEmpty(clipAuthoringId) &&
                m_ProxyClips.TryGetValue(clipAuthoringId, out BtsmtlSlateActionClip clip))
            {
                CutsceneUtility.selectedObject = clip;
                CutsceneEditor.current?.Repaint();
                return true;
            }
            if (!string.IsNullOrEmpty(trackAuthoringId) &&
                m_ProxyTracks.TryGetValue(trackAuthoringId, out BtsmtlSlateTrack track))
            {
                CutsceneUtility.selectedObject = track;
                CutsceneEditor.current?.Repaint();
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
            CutsceneEditor.ShowWindow(s_Current.m_Cutscene);
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
            m_Cutscene.length = Mathf.Max(1f, m_Request.Timeline.Duration);
            m_Cutscene.viewTimeMin = 0f;
            m_Cutscene.viewTimeMax = Mathf.Max(m_Cutscene.length + 1f, m_Cutscene.length);

            GameObject groupObject = CreateChild(m_Cutscene.groupsRoot, "BTSMTL Timeline");
            BtsmtlSlateGroup group = groupObject.AddComponent<BtsmtlSlateGroup>();
            group.hideFlags = HideFlags.HideAndDontSave;
            string ownershipLabel = string.IsNullOrWhiteSpace(m_Request.OwnershipLabel)
                ? string.Empty
                : $" [{m_Request.OwnershipLabel}]";
            group.name = $"{m_Request.Timeline.Name}{ownershipLabel}";
            m_Cutscene.groups.Add(group);

            for (int trackIndex = 0; trackIndex < m_Request.Timeline.Tracks.Count; trackIndex++)
            {
                Track sourceTrack = m_Request.Timeline.Tracks[trackIndex];
                if (sourceTrack == null)
                    continue;
                var curveChannels = new List<TimelineCurveChannelDescriptor>();
                TimelineCurveChannelCatalog.CollectForTrack(sourceTrack, curveChannels);
                string trackDisplayName = curveChannels.Count == 0
                    ? sourceTrack.Name
                    : $"{sourceTrack.Name}  [Curves: {curveChannels.Count}]";
                GameObject trackObject = CreateChild(group.transform, trackDisplayName);
                BtsmtlSlateTrack proxyTrack = trackObject.AddComponent<BtsmtlSlateTrack>();
                proxyTrack.hideFlags = HideFlags.HideAndDontSave;
                proxyTrack.name = trackDisplayName;
                proxyTrack.Configure(sourceTrack.AuthoringId);
                group.tracks.Add(proxyTrack);
                proxyTrack.PostCreate(group);
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
                            ToSlateCurve(descriptor.Read(sourceClip), proxyClip.length)));
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

        static AnimationCurve ToSlateCurve(AnimationCurve normalizedCurve, float duration)
        {
            return ConvertCurveTime(normalizedCurve, duration, false);
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

        void OnActionDoubleClick(ActionClip action)
        {
            if (!(action is BtsmtlSlateActionClip proxyClip) || string.IsNullOrEmpty(proxyClip.SourceAuthoringId))
                return;
            if (m_SourceClips.TryGetValue(proxyClip.SourceAuthoringId, out Clip sourceClip))
                m_OpenSourceClip?.Invoke(sourceClip);
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
            if (m_ReadOnly || end.Unsupported)
                return;
            var changes = new List<(Clip Clip, int StartFrame, int EndFrame, int EaseInFrame, int EaseOutFrame)>();
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
                        return;
                    if (!m_ProxyClips.TryGetValue(pair.Key, out BtsmtlSlateActionClip proxyClip))
                        return;
                    curveChanges.Add((sourceClip, channelId, ToAuthoringCurve(curvePair.Value.Curve, proxyClip.length)));
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

        public void ApplyAuthoringPreviewTime(float time)
        {
            if (m_Cutscene == null)
                return;
            m_Cutscene.currentTime = Mathf.Clamp(time, 0f, m_Cutscene.length);
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
            CutsceneEditor.OnActionDoubleClick -= OnActionDoubleClick;
            m_Request.Timeline.OnValueChanged -= OnSourceTimelineChanged;
            Undo.undoRedoEvent -= OnUndoRedoEvent;
            if (ReferenceEquals(CutsceneEditor.RecordUndoForCutscene, m_UndoPolicy))
                CutsceneEditor.RecordUndoForCutscene = null;
            if (ReferenceEquals(CutsceneEditor.AllowPlaybackForCutscene, m_PlaybackPolicy))
                CutsceneEditor.AllowPlaybackForCutscene = null;
            if (!m_EditorClosed && m_Cutscene != null)
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
