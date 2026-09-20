using System;
using ThirdPersonSimulation.Fixed;
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

        public int MaxFrame => TimelineTimeGrid.CeilingIndex(DurationTime, TimelineUtility.FrameRate);
        public FixedScalar DurationTime { get; private set; }
        public float Duration => DurationTime.ToSingle();

        public void Init()
        {
            DurationTime = FixedScalar.Zero;
            foreach (var track in m_Tracks)
            {
                track.Init(this);
                DurationTime = FixedScalar.Max(DurationTime, track.DurationTime);
            }
            for (int i = 0; i < m_Sections.Count; i++)
            {
                TimelineSection section = m_Sections[i];
                if (section != null)
                    DurationTime = FixedScalar.Max(DurationTime, section.Time);
            }
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

        public TimelineExecutionDomain ExecutionDomain => m_ExecutionDomain;
        public bool HasExplicitExecutionDomain => TimelineExecutionDomains.IsValid(m_ExecutionDomain);

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
        public FixedScalar DurationTime { get; protected set; }

        public virtual void Init(TimelineData timeline)
        {
            Timeline = timeline;

            DurationTime = FixedScalar.Zero;
            foreach (var clip in m_Clips)
            {
                clip.Init(this);
                DurationTime = FixedScalar.Max(DurationTime, clip.EndTime);
            }
            foreach (var marker in m_Markers)
            {
                marker.Init(this);
                DurationTime = FixedScalar.Max(DurationTime, FixedScalar.Max(FixedScalar.FromRaw(1), marker.Time));
            }

        }

#if UNITY_EDITOR
        public void ConfigureExecutionDomain(TimelineExecutionDomain executionDomain)
        {
            if (!TimelineExecutionDomains.IsValid(executionDomain))
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

        public TimelineMarker AddMarker(FixedScalar time, ScriptableObject graph)
        {
            if (time < FixedScalar.Zero)
                throw new ArgumentOutOfRangeException(nameof(time));
            var marker = new TimelineMarker();
            marker.EnsureAuthoringIdentity();
            marker.Configure(time, graph);
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

        #region Frame
        [SerializeField]
        long m_StartTimeRaw;
        [SerializeField]
        long m_EndTimeRaw;
        [SerializeField]
        long m_OtherEaseInTimeRaw;
        [SerializeField]
        long m_OtherEaseOutTimeRaw;
        [SerializeField]
        long m_SelfEaseInTimeRaw;
        [SerializeField]
        long m_SelfEaseOutTimeRaw;
        [SerializeField]
        long m_ClipInTimeRaw;

        public string AuthoringId => m_AuthoringId ?? string.Empty;
        public virtual string ContractKind => string.Empty;
        public TimelineExecutionDomain ExecutionDomain => Track != null
            ? Track.ExecutionDomain
            : throw new InvalidOperationException("Timeline Clip has no owning Track.");
        #endregion

        #region Time
        public FixedScalar StartTime => FixedScalar.FromRaw(m_StartTimeRaw);
        public FixedScalar EndTime => FixedScalar.FromRaw(m_EndTimeRaw);
        public FixedScalar SelfEaseInTime => FixedScalar.FromRaw(m_SelfEaseInTimeRaw);
        public FixedScalar SelfEaseOutTime => FixedScalar.FromRaw(m_SelfEaseOutTimeRaw);
        public FixedScalar OtherEaseInTime => FixedScalar.FromRaw(m_OtherEaseInTimeRaw);
        public FixedScalar OtherEaseOutTime => FixedScalar.FromRaw(m_OtherEaseOutTimeRaw);
        public FixedScalar EaseInTime => OtherEaseInTime.Raw == 0 ? SelfEaseInTime : OtherEaseInTime;
        public FixedScalar EaseOutTime => OtherEaseOutTime.Raw == 0 ? SelfEaseOutTime : OtherEaseOutTime;
        public FixedScalar ClipInTime => FixedScalar.FromRaw(m_ClipInTimeRaw);
        public FixedScalar DurationTime => EndTime - StartTime;

        #endregion

        [NonSerialized]
        public Track Track;
        public TimelineData Timeline => Track.Timeline;

        public Action OnNameChanged;
        public Action OnInspectorRepaint;

        public virtual void Init(Track track)
        {
            Track = track;
        }

#if UNITY_EDITOR
        public void ConfigureTimeRange(FixedScalar startTime, FixedScalar endTime)
        {
            if (startTime < FixedScalar.Zero || endTime < startTime)
                throw new ArgumentOutOfRangeException(nameof(startTime));
            m_StartTimeRaw = startTime.Raw;
            m_EndTimeRaw = endTime.Raw;
        }

        public void ConfigureEase(FixedScalar easeIn, FixedScalar easeOut)
        {
            if (easeIn < FixedScalar.Zero || easeOut < FixedScalar.Zero)
                throw new ArgumentOutOfRangeException(nameof(easeIn));
            m_SelfEaseInTimeRaw = easeIn.Raw;
            m_SelfEaseOutTimeRaw = easeOut.Raw;
        }

        public void ConfigureClipIn(FixedScalar time)
        {
            if (time < FixedScalar.Zero)
                throw new ArgumentOutOfRangeException(nameof(time));
            m_ClipInTimeRaw = time.Raw;
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
        public Clip AddClip(TimelineContractCatalog catalog, Track track, FixedScalar time)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            Clip clip = track.AddClip(time);
            try
            {
                catalog.RequireClipPlacement(track, clip);
            }
            catch
            {
                track.RemoveClip(clip);
                throw;
            }
            Init();
            return clip;
        }
        public Clip AddClip(TimelineContractCatalog catalog, UnityEngine.Object referenceObject, Track track, FixedScalar time)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            Clip clip = track.AddClip(referenceObject, time);
            try
            {
                catalog.RequireClipPlacement(track, clip);
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
        public TimelineSection AddSection(string name, FixedScalar time)
        {
            TimelineSection section = TimelineSection.Create(name, time);
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
        public TimelineSection EnsureSection(string authoringId, string name, FixedScalar time)
        {
            TimelineSection section = m_Sections.SingleOrDefault(value =>
                value != null && string.Equals(value.AuthoringId, authoringId, StringComparison.Ordinal));
            if (section == null)
            {
                section = TimelineSection.Create(authoringId, name, time);
                m_Sections.Add(section);
            }
            ConfigureSection(section, name, time);
            return section;
        }
#endif
        public void ConfigureSection(TimelineSection section, string name, FixedScalar time)
        {
            if (section == null || !m_Sections.Contains(section))
                throw new ArgumentException("Timeline Section is not owned by this Timeline.", nameof(section));
            string value = name?.Trim() ?? string.Empty;
            if (m_Sections.Exists(candidate => candidate != null && !ReferenceEquals(candidate, section) &&
                string.Equals(candidate.Name, value, StringComparison.Ordinal)))
                throw new InvalidOperationException($"Timeline Section '{value}' already exists.");
            section.Configure(value, time);
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
                int frame = (left?.Time ?? FixedScalar.MaxValue).CompareTo(right?.Time ?? FixedScalar.MaxValue);
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
                if (AlignTerminalTimeClips())
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

        bool AlignTerminalTimeClips()
        {
            var terminalClips = new List<ITimelineTerminalTimeAlignedClip>();
            FixedScalar terminalTime = FixedScalar.Zero;
            for (int trackIndex = 0; trackIndex < m_Tracks.Count; trackIndex++)
            {
                Track track = m_Tracks[trackIndex];
                if (track == null)
                    continue;
                foreach (TimelineMarker marker in track.Markers)
                    terminalTime = FixedScalar.Max(terminalTime, FixedScalar.Max(FixedScalar.FromRaw(1), marker.Time));
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip == null)
                        continue;
                    if (clip is ITimelineTerminalTimeAlignedClip terminalClip)
                    {
                        terminalClips.Add(terminalClip);
                        terminalTime = FixedScalar.Max(terminalTime, clip.StartTime + FixedScalar.FromRaw(1));
                    }
                    else
                        terminalTime = FixedScalar.Max(terminalTime, clip.EndTime);
                }
            }
            for (int sectionIndex = 0; sectionIndex < m_Sections.Count; sectionIndex++)
            {
                TimelineSection section = m_Sections[sectionIndex];
                if (section != null)
                    terminalTime = FixedScalar.Max(terminalTime, section.Time);
            }
            bool changed = false;
            for (int clipIndex = 0; clipIndex < terminalClips.Count; clipIndex++)
                changed |= terminalClips[clipIndex].AlignTerminalTime(terminalTime);
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

        public virtual Clip AddClip(FixedScalar time)
        {
            Clip clip = Activator.CreateInstance(ClipType, this, time) as Clip;
            clip.RegenerateAuthoringIdentity();
            m_Clips.Add(clip);
            return clip;
        }
        public virtual Clip AddClip(UnityEngine.Object referenceObject, FixedScalar time)
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
        public virtual FixedScalar Length => DurationTime;
        public virtual ClipCapabilities Capabilities => ClipCapabilities.None;

        public Clip() { }
        public Clip(Track track, FixedScalar time)
        {
            Track = track;
            ConfigureTimeRange(time, time + FixedScalar.FromDecimal(0.05m));
        }

        public void UpdateMix()
        {
            m_OtherEaseInTimeRaw = 0;
            m_OtherEaseOutTimeRaw = 0;

            if (Invalid || !IsMixable())
                return;

            foreach (var clip in Track.Clips)
            {
                if (clip != this && !clip.Invalid)
                {
                    if (clip.StartTime < StartTime && clip.EndTime > EndTime)
                    {
                        return;
                    }
                    else if (clip.StartTime > StartTime && clip.EndTime < EndTime)
                    {
                        return;
                    }

                    if (clip.StartTime < StartTime && clip.EndTime > StartTime)
                    {
                        m_OtherEaseInTimeRaw = (clip.EndTime - StartTime).Raw;
                    }
                    if (clip.StartTime > StartTime && clip.StartTime < EndTime)
                    {
                        m_OtherEaseOutTimeRaw = (EndTime - clip.StartTime).Raw;
                    }
                    if (clip.StartTime == StartTime)
                    {
                        if (clip.EndTime < EndTime)
                        {
                            m_OtherEaseInTimeRaw = (clip.EndTime - StartTime).Raw;
                        }
                        else if (clip.EndTime > EndTime)
                        {
                            m_OtherEaseOutTimeRaw = (EndTime - StartTime).Raw;
                        }
                    }
                    m_SelfEaseInTimeRaw = FixedScalar.Min(SelfEaseInTime, DurationTime - OtherEaseOutTime).Raw;
                    m_SelfEaseOutTimeRaw = FixedScalar.Min(SelfEaseOutTime, DurationTime - OtherEaseInTime).Raw;
                }
            }
        }
        public bool Contains(FixedScalar time)
        {
            return StartTime < time && time < EndTime;
        }

        public Color Color()
        {
            var colorAttributes = GetType().GetCustomAttributes<ColorAttribute>().ToArray();
            return colorAttributes[colorAttributes.Length - 1].Color / 255;
        }

        public string StartTimeText()
        {
            return $"StartTime:  {StartTime.ToDouble().ToString("0.00")}S";
        }
        public string EndTimeText()
        {
            return $"EndTime:  {EndTime.ToDouble().ToString("0.00")}S";
        }
        public string DurationText()
        {
            return $"Duration:  {DurationTime.ToDouble().ToString("0.00")}S";
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
        protected SignalClip(Track track, FixedScalar time) : base(track, time)
        {
            ConfigureTimeRange(time, time + FixedScalar.FromRatio(1, TimelineUtility.FrameRate));
        }
    } 
#endif
}
