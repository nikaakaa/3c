using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using BTSMTL.Diagnostics;
using ThirdPersonSimulation;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public sealed partial class TimelineData
    {
        public event Action OnValueChanged;

        float m_Time;
        public float Time
        {
            get => m_Time;
            set => m_Time = value;
        }
        public int Frame => Mathf.RoundToInt(Time * TimelineUtility.FrameRate);

        public int MaxFrame { get; private set; }
        public float Duration { get; private set; }

        public void Init()
        {
            MaxFrame = 0;
            foreach (var track in m_Tracks)
            {
                track.Init(this);
                if (track.MaxFrame > MaxFrame)
                    MaxFrame = track.MaxFrame;
            }
            for (int i = 0; i < m_Sections.Count; i++)
            {
                TimelineSection section = m_Sections[i];
                if (section != null && section.Frame > MaxFrame)
                    MaxFrame = section.Frame;
            }
            Duration = (float)MaxFrame / TimelineUtility.FrameRate;
            OnValueChanged?.Invoke();
        }
    }

    [Serializable]
    public abstract partial class Track
    {
        [SerializeField]
        string m_AuthoringId;

        public string Name;
        public string AuthoringId => m_AuthoringId ?? string.Empty;
        public virtual string ContractKind => string.Empty;

        [SerializeField, ShowInInspector, OnValueChanged("RebindTimeline")]
        TimelineExecutionDomain m_ExecutionDomain;

        public TimelineExecutionDomain ExecutionDomain => TimelineExecutionDomains.Normalize(m_ExecutionDomain);
        public bool HasExplicitExecutionDomain => Enum.IsDefined(typeof(TimelineExecutionDomain), m_ExecutionDomain);

        [SerializeField]
        protected bool m_PersistentMuted;
        public bool PersistentMuted
        {
            get => m_PersistentMuted;
            set
            {
                if (m_PersistentMuted != value)
                {
                    m_PersistentMuted = value;
                    OnMutedStateChanged?.Invoke();
                }
            }
        }

        [SerializeReference]
        protected List<Clip> m_Clips = new List<Clip>();
        public List<Clip> Clips => m_Clips;

        [SerializeField]
        List<TimelineMarker> m_Markers = new List<TimelineMarker>();
        public List<TimelineMarker> Markers => m_Markers;

        public Action OnUpdateMix;
        public Action OnMutedStateChanged;

        public TimelineData Timeline { get; protected set; }
        public int MaxFrame { get; protected set; }

        public virtual void Init(TimelineData timeline)
        {
            Timeline = timeline;

            MaxFrame = 0;
            foreach (var clip in m_Clips)
            {
                clip.Init(this);
                if (clip.EndFrame > MaxFrame)
                    MaxFrame = clip.EndFrame;
            }
            foreach (var marker in m_Markers)
            {
                marker.Init(this);
                MaxFrame = Mathf.Max(MaxFrame, Mathf.Max(1, marker.Frame));
            }

        }

#if UNITY_EDITOR
        public void ConfigureExecutionDomain(TimelineExecutionDomain executionDomain)
        {
            if (!Enum.IsDefined(typeof(TimelineExecutionDomain), executionDomain))
                throw new ArgumentOutOfRangeException(nameof(executionDomain));
            m_ExecutionDomain = executionDomain;
        }

        public bool EnsureAuthoringIdentity()
        {
            if (AuthoringIdentity.IsValid(m_AuthoringId))
                return false;
            m_AuthoringId = AuthoringIdentity.Create();
            return true;
        }

        public void RegenerateAuthoringIdentity()
        {
            m_AuthoringId = AuthoringIdentity.Create();
        }

        public TimelineMarker AddMarker(int frame, ScriptableObject graph)
        {
            if (frame < 0)
                throw new ArgumentOutOfRangeException(nameof(frame));
            var marker = new TimelineMarker();
            marker.EnsureAuthoringIdentity();
            marker.Configure(frame, graph);
            marker.Init(this);
            m_Markers.Add(marker);
            return marker;
        }

        public void RemoveMarker(TimelineMarker marker)
        {
            if (marker == null || !m_Markers.Remove(marker))
                throw new ArgumentException("Timeline Marker is not owned by this Track.", nameof(marker));
        }

        public void ConfigureAuthoringIdentity(string authoringId)
        {
            if (!AuthoringIdentity.IsValid(authoringId))
                throw new ArgumentException("Timeline Track authoring identity is invalid.", nameof(authoringId));
            m_AuthoringId = authoringId;
        }
#endif
    }

    [Serializable]
    public abstract partial class Clip
    {
        [SerializeField]
        string m_AuthoringId;

        [SerializeField, ShowInInspector, OnValueChanged("RebindTimeline")]
        TimelineExecutionDomain m_ExecutionDomain;

        #region Frame
        public int StartFrame;
        public int EndFrame;
        public int OtherEaseInFrame;
        public int OtherEaseOutFrame;
        public int SelfEaseInFrame;
        public int SelfEaseOutFrame;
        public int ClipInFrame;

        public int EaseInFrame => OtherEaseInFrame == 0 ? SelfEaseInFrame : OtherEaseInFrame;
        public int EaseOutFrame => OtherEaseOutFrame == 0 ? SelfEaseOutFrame : OtherEaseOutFrame;
        public int Duration => EndFrame - StartFrame;
        public string AuthoringId => m_AuthoringId ?? string.Empty;
        public virtual string ContractKind => string.Empty;
        public bool HasExplicitExecutionDomain => Enum.IsDefined(typeof(TimelineExecutionDomain), m_ExecutionDomain);
        public TimelineExecutionDomain ExecutionDomain => ResolveExecutionDomain(
            Track != null ? Track.ExecutionDomain : TimelineExecutionDomain.Logic);
        #endregion

        #region Time
        public float StartTime { get; private set; }
        public float EndTime { get; private set; }
        public float OtherEaseInTime { get; private set; }
        public float OtherEaseOutTime { get; private set; }
        public float EaseInTime { get; private set; }
        public float EaseOutTime { get; private set; }
        public float ClipInTime { get; private set; }
        public float DurationTime { get; private set; }

        #endregion

        [NonSerialized]
        public Track Track;
        public TimelineData Timeline => Track.Timeline;

        public Action OnNameChanged;
        public Action OnInspectorRepaint;

        public virtual void Init(Track track)
        {
            Track = track;
            FrameToTime();
        }

        public TimelineExecutionDomain ResolveExecutionDomain(TimelineExecutionDomain trackExecutionDomain)
        {
            return HasExplicitExecutionDomain
                ? TimelineExecutionDomains.Normalize(m_ExecutionDomain)
                : TimelineExecutionDomains.Normalize(trackExecutionDomain);
        }

#if UNITY_EDITOR
        public void InheritExecutionDomain()
        {
            m_ExecutionDomain = 0;
        }

        public void ConfigureExecutionDomain(TimelineExecutionDomain executionDomain)
        {
            if (!Enum.IsDefined(typeof(TimelineExecutionDomain), executionDomain))
                throw new ArgumentOutOfRangeException(nameof(executionDomain));
            m_ExecutionDomain = executionDomain;
        }

        public bool EnsureAuthoringIdentity()
        {
            if (AuthoringIdentity.IsValid(m_AuthoringId))
                return false;
            m_AuthoringId = AuthoringIdentity.Create();
            return true;
        }

        public void RegenerateAuthoringIdentity()
        {
            m_AuthoringId = AuthoringIdentity.Create();
        }

        public void ConfigureAuthoringIdentity(string authoringId)
        {
            if (!AuthoringIdentity.IsValid(authoringId))
                throw new ArgumentException("Timeline Clip authoring identity is invalid.", nameof(authoringId));
            m_AuthoringId = authoringId;
        }
#endif
        public void FrameToTime()
        {
            StartTime = StartFrame / (float)TimelineUtility.FrameRate;
            EndTime = EndFrame / (float)TimelineUtility.FrameRate;
            OtherEaseInTime = OtherEaseInFrame / (float)TimelineUtility.FrameRate;
            OtherEaseOutTime = OtherEaseOutFrame / (float)TimelineUtility.FrameRate;
            EaseInTime = EaseInFrame / (float)TimelineUtility.FrameRate;
            EaseOutTime = EaseOutFrame / (float)TimelineUtility.FrameRate;
            ClipInTime = ClipInFrame / (float)TimelineUtility.FrameRate;
            DurationTime = Duration / (float)TimelineUtility.FrameRate;
        }
    }

    public abstract partial class SignalClip : Clip { }

    public readonly struct TimelinePlaybackHandle
    {
        public TimelinePlaybackHandle(ulong value)
        {
            Value = value;
        }

        public ulong Value { get; }
        public bool IsValid => Value != 0;

        public static TimelinePlaybackHandle Invalid => default;
    }

    public readonly struct TimelinePlaybackActionContext
    {
        public TimelinePlaybackActionContext(
            ulong actionInstanceId,
            string actionId,
            ulong predictionKey,
            ulong inputSequence,
            ulong startLocalLogicTick)
        {
            ActionInstanceId = actionInstanceId;
            ActionId = actionId ?? string.Empty;
            PredictionKey = predictionKey;
            InputSequence = inputSequence;
            StartLocalLogicTick = startLocalLogicTick;
        }

        public ulong ActionInstanceId { get; }
        public string ActionId { get; }
        public ulong PredictionKey { get; }
        public ulong InputSequence { get; }
        public ulong StartLocalLogicTick { get; }
        public bool IsValid => ActionInstanceId != 0;
    }

    public enum TimelinePlaybackMode
    {
        Once,
        Loop
    }

    public enum TimelinePlaybackStatus
    {
        None,
        Requested,
        Running,
        Succeeded,
        Failed,
        Cancelled
    }

    public enum TimelinePlaybackStopCause
    {
        SelfAbort,
        LowerPriorityAbort,
        ExplicitParentStop,
        StateTransition,
        Reset,
        Shutdown
    }

    public readonly struct TimelinePlaybackStopContext
    {
        public TimelinePlaybackStopContext(TimelinePlaybackStopCause cause, ulong localLogicTick)
        {
            Cause = cause;
            LocalLogicTick = localLogicTick;
        }

        public TimelinePlaybackStopCause Cause { get; }
        public ulong LocalLogicTick { get; }
    }

    public interface ITimelinePlaybackActionContextSource
    {
        bool TryGetTimelinePlaybackActionContext(ActionContextSlot actionContext, out TimelinePlaybackActionContext playbackActionContext);
    }

#if UNITY_EDITOR

    public partial class TimelineData
    {
        public UnityEditor.SerializedObject SerializedTimeline;
        public UnityEditor.SerializedProperty SerializedData;

        public void AddTrack(Type type, TimelineContractCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            Track track = Activator.CreateInstance(type) as Track;
            TimelineTrackContract contract = catalog.RequireTrack(track?.ContractKind);
            track.RegenerateAuthoringIdentity();
            track.ConfigureExecutionDomain(contract.ExecutionPolicy.Domain);
            track.Name = type.Name.Replace("Track", string.Empty);
            m_Tracks.Add(track);
            Init();
        }
        public void RemoveTrack(Track track)
        {
            m_Tracks.Remove(track);
            Init();
        }
        public Clip AddClip(TimelineContractCatalog catalog, Track track, int frame)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            Clip clip = track.AddClip(frame);
            try
            {
                catalog.RequireClipPlacement(track, clip);
                clip.ConfigureExecutionDomain(track.ExecutionDomain);
            }
            catch
            {
                track.RemoveClip(clip);
                throw;
            }
            Init();
            return clip;
        }
        public Clip AddClip(TimelineContractCatalog catalog, UnityEngine.Object referenceObject, Track track, int frame)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            Clip clip = track.AddClip(referenceObject, frame);
            try
            {
                catalog.RequireClipPlacement(track, clip);
                clip.ConfigureExecutionDomain(track.ExecutionDomain);
            }
            catch
            {
                if (clip != null)
                    track.RemoveClip(clip);
                throw;
            }
            Init();
            return clip;
        }
        public void RemoveClip(Clip clip)
        {
            clip.Track.RemoveClip(clip);

            Init();
        }
        public TimelineSection AddSection(string name, int frame)
        {
            TimelineSection section = TimelineSection.Create(name, frame);
            if (m_Sections.Exists(value => value != null && string.Equals(value.Name, section.Name, StringComparison.Ordinal)))
                throw new InvalidOperationException($"Timeline Section '{section.Name}' already exists.");
            m_Sections.Add(section);
            SortSections();
            Init();
            return section;
        }
        public TimelineExternalBindingDeclaration AddExternalBinding(
            string bindingId,
            string displayName,
            string domain,
            TimelineBindingValueKind valueKind,
            TimelineBindingAccess access,
            TimelineBindingLifetime lifetime,
            string parameterId = null)
        {
            TimelineExternalBindingDeclaration binding = TimelineExternalBindingDeclaration.Create(
                bindingId,
                displayName,
                domain,
                valueKind,
                access,
                lifetime,
                parameterId);
            if (m_ExternalBindings.Exists(value => value != null && string.Equals(value.BindingId, binding.BindingId, StringComparison.Ordinal)))
                throw new InvalidOperationException($"Timeline external binding '{binding.BindingId}' already exists.");
            m_ExternalBindings.Add(binding);
            OnValueChanged?.Invoke();
            return binding;
        }
        public void RemoveExternalBinding(TimelineExternalBindingDeclaration binding)
        {
            if (binding == null || !m_ExternalBindings.Remove(binding))
                throw new ArgumentException("Timeline external binding is not owned by this Timeline.", nameof(binding));
            OnValueChanged?.Invoke();
        }
#if UNITY_EDITOR
        public TimelineSection EnsureSection(string authoringId, string name, int frame)
        {
            TimelineSection section = m_Sections.SingleOrDefault(value =>
                value != null && string.Equals(value.AuthoringId, authoringId, StringComparison.Ordinal));
            if (section == null)
            {
                section = TimelineSection.Create(authoringId, name, frame);
                m_Sections.Add(section);
            }
            ConfigureSection(section, name, frame);
            return section;
        }
#endif
        public void ConfigureSection(TimelineSection section, string name, int frame)
        {
            if (section == null || !m_Sections.Contains(section))
                throw new ArgumentException("Timeline Section is not owned by this Timeline.", nameof(section));
            string value = name?.Trim() ?? string.Empty;
            if (m_Sections.Exists(candidate => candidate != null && !ReferenceEquals(candidate, section) &&
                string.Equals(candidate.Name, value, StringComparison.Ordinal)))
                throw new InvalidOperationException($"Timeline Section '{value}' already exists.");
            section.Configure(value, frame);
            SortSections();
            Init();
        }

        public void ConfigureSectionNext(TimelineSection section, string nextSectionId)
        {
            if (section == null || !m_Sections.Contains(section))
                throw new ArgumentException("Timeline Section is not owned by this Timeline.", nameof(section));
            string value = nextSectionId?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(value) &&
                !m_Sections.Exists(candidate => candidate != null && candidate.AuthoringId == value))
                throw new InvalidOperationException($"Timeline Section '{section.Name}' references unknown next Section '{value}'.");
            section.SetNextSection(value);
            Init();
        }
        public void RemoveSection(TimelineSection section)
        {
            if (section == null || !m_Sections.Remove(section))
                throw new ArgumentException("Timeline Section is not owned by this Timeline.", nameof(section));
            Init();
        }
        void SortSections()
        {
            m_Sections.Sort((left, right) =>
            {
                int frame = (left?.Frame ?? int.MaxValue).CompareTo(right?.Frame ?? int.MaxValue);
                return frame != 0 ? frame : string.CompareOrdinal(left?.AuthoringId, right?.AuthoringId);
            });
        }
        public void UpdateMix()
        {
            m_Tracks.ForEach(track => track.UpdateMix());
        }
        public void Resort()
        {
            OnValueChanged?.Invoke();
        }

        public void ApplyModify(Action action, string name)
        {
            if (!SerializedOwner || string.IsNullOrEmpty(SerializedPropertyPath))
                throw new InvalidOperationException($"TimelineData {Name} is missing serialized owner/path.");
            UnityEditor.Undo.IncrementCurrentGroup();
            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            string undoName = $"Timeline: {name}";
            UnityEditor.Undo.SetCurrentGroupName(undoName);
            UnityEditor.Undo.RegisterCompleteObjectUndo(SerializedOwner, undoName);
            try
            {
                UpdateSerializedTimeline();
                action?.Invoke();
                Init();
                if (AlignTerminalFrameClips())
                    Init();
                UnityEditor.EditorUtility.SetDirty(SerializedOwner);
                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                UnityEditor.Undo.FlushUndoRecordObjects();
                UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                UpdateSerializedTimeline();
                throw;
            }
        }

        bool AlignTerminalFrameClips()
        {
            var terminalClips = new List<ITimelineTerminalFrameAlignedClip>();
            int terminalFrame = 0;
            for (int trackIndex = 0; trackIndex < m_Tracks.Count; trackIndex++)
            {
                Track track = m_Tracks[trackIndex];
                if (track == null)
                    continue;
                foreach (TimelineMarker marker in track.Markers)
                    terminalFrame = Mathf.Max(terminalFrame, Mathf.Max(1, marker.Frame));
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip == null)
                        continue;
                    if (clip is ITimelineTerminalFrameAlignedClip terminalClip)
                    {
                        terminalClips.Add(terminalClip);
                        terminalFrame = Mathf.Max(terminalFrame, clip.StartFrame + 1);
                    }
                    else
                        terminalFrame = Mathf.Max(terminalFrame, clip.EndFrame);
                }
            }
            for (int sectionIndex = 0; sectionIndex < m_Sections.Count; sectionIndex++)
            {
                TimelineSection section = m_Sections[sectionIndex];
                if (section != null)
                    terminalFrame = Mathf.Max(terminalFrame, section.Frame);
            }
            bool changed = false;
            for (int clipIndex = 0; clipIndex < terminalClips.Count; clipIndex++)
                changed |= terminalClips[clipIndex].AlignTerminalFrame(terminalFrame);
            return changed;
        }

        public void UpdateSerializedTimeline()
        {
            if (!SerializedOwner || string.IsNullOrEmpty(SerializedPropertyPath))
                throw new InvalidOperationException($"TimelineData {Name} is missing serialized owner/path.");
            SerializedTimeline = new UnityEditor.SerializedObject(SerializedOwner);
            SerializedData = SerializedTimeline.FindProperty(SerializedPropertyPath);
            if (SerializedData == null)
                throw new InvalidOperationException($"TimelineData {Name} serialized path is invalid: {SerializedPropertyPath}");
        }
    }

    public abstract partial class Track
    {
        public virtual Type ClipType => typeof(Clip);

        public virtual Clip AddClip(int frame)
        {
            Clip clip = Activator.CreateInstance(ClipType, this, frame) as Clip;
            clip.RegenerateAuthoringIdentity();
            clip.ConfigureExecutionDomain(ExecutionDomain);
            m_Clips.Add(clip);
            return clip;
        }
        public virtual Clip AddClip(UnityEngine.Object referenceObject, int frame)
        {
            return null;
        }

        public void RemoveClip(Clip clip)
        {
            m_Clips.Remove(clip);
            UpdateMix();
        }
        public void UpdateMix()
        {
            Clips.ForEach(c => 
            {
                c.UpdateMix();
                c.FrameToTime();
            });
            OnUpdateMix?.Invoke();
        }
        public Color Color()
        {
            var colorAttributes = GetType().GetCustomAttributes<ColorAttribute>().ToArray();
            return colorAttributes[colorAttributes.Length - 1].Color / 255;
        }
        public virtual bool DragValid()
        {
            return false;
        }

        public void RebindTimeline()
        {
            Timeline.Init();
        }
    }

    public abstract partial class Clip
    {
        [NonSerialized]
        public bool Invalid;

        public virtual string Name => GetType().Name;
        public virtual int Length => EndFrame - StartFrame;
        public virtual ClipCapabilities Capabilities => ClipCapabilities.None;

        public Clip() { }
        public Clip(Track track, int frame)
        {
            Track = track;
            StartFrame = frame;
            EndFrame = StartFrame + 3;
        }

        public void UpdateMix()
        {
            OtherEaseInFrame = 0;
            OtherEaseOutFrame = 0;

            if (Invalid)
                return;

            foreach (var clip in Track.Clips)
            {
                if (clip != this && !clip.Invalid)
                {
                    if (clip.StartFrame < StartFrame && clip.EndFrame > EndFrame)
                    {
                        return;
                    }
                    else if (clip.StartFrame > StartFrame && clip.EndFrame < EndFrame)
                    {
                        return;
                    }

                    if (clip.StartFrame < StartFrame && clip.EndFrame > StartFrame)
                    {
                        OtherEaseInFrame = clip.EndFrame - StartFrame;
                    }
                    if (clip.StartFrame > StartFrame && clip.StartFrame < EndFrame)
                    {
                        OtherEaseOutFrame = EndFrame - clip.StartFrame;
                    }
                    if (clip.StartFrame == StartFrame)
                    {
                        if (clip.EndFrame < EndFrame)
                        {
                            OtherEaseInFrame = clip.EndFrame - StartFrame;
                        }
                        else if (clip.EndFrame > EndFrame)
                        {
                            OtherEaseOutFrame = EndFrame - StartFrame;
                        }
                    }
                    SelfEaseInFrame = Mathf.Min(SelfEaseInFrame, Duration - OtherEaseOutFrame);
                    SelfEaseOutFrame = Mathf.Min(SelfEaseOutFrame, Duration - OtherEaseInFrame);
                }
            }
        }
        public bool Contains(float halfFrame)
        {
            return StartFrame < halfFrame && halfFrame < EndFrame;
        }

        public Color Color()
        {
            var colorAttributes = GetType().GetCustomAttributes<ColorAttribute>().ToArray();
            return colorAttributes[colorAttributes.Length - 1].Color / 255;
        }

        public string StartTimeText()
        {
            return $"StartTime:  {StartFrame.ToString("0.00")}S  /  StartFrame:  {StartFrame}F";
        }
        public string EndTimeText()
        {
            return $"EndTime:  {EndTime.ToString("0.00")}S  /  EndFrame:  {EndFrame}F";
        }
        public string DurationText()
        {
            return $"Duration:  {DurationTime.ToString("0.00")}S  /  {Duration}F";
        }

        public virtual void RebindTimeline()
        {
            Track.RebindTimeline();
        }
        public virtual void RepaintInspector()
        {
            OnInspectorRepaint?.Invoke();
        }

        public virtual bool IsResizable()
        {
            return (Capabilities & ClipCapabilities.Resizable) == ClipCapabilities.Resizable;
        }
        public virtual bool IsMixable()
        {
            return (Capabilities & ClipCapabilities.Mixable) == ClipCapabilities.Mixable;
        }
        public virtual bool IsClipInable()
        {
            return (Capabilities & ClipCapabilities.ClipInable) == ClipCapabilities.ClipInable;
        }
        public bool IsTickQuantized()
        {
            return (Capabilities & ClipCapabilities.TickQuantized) == ClipCapabilities.TickQuantized;
        }
    }

    public abstract partial class SignalClip
    {
        protected SignalClip(Track track, int frame) : base(track, frame) 
        {
            EndFrame = StartFrame + 1;
        }
    } 
#endif
}
