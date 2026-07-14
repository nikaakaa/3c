using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using BTSMTL.Diagnostics;
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

        }

#if UNITY_EDITOR
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
#endif
    }

    [Serializable]
    public abstract partial class Clip
    {
        [SerializeField]
        string m_AuthoringId;

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

#if UNITY_EDITOR
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

        public void AddTrack(Type type)
        {
            Track track = Activator.CreateInstance(type) as Track;
            track.RegenerateAuthoringIdentity();
            track.Name = type.Name.Replace("Track", string.Empty);
            m_Tracks.Add(track);
            Init();
        }
        public void RemoveTrack(Track track)
        {
            m_Tracks.Remove(track);
            Init();
        }
        public Clip AddClip(Track track, int frame)
        {
            Clip clip = track.AddClip(frame);

            Init();
            return clip;
        }
        public Clip AddClip(UnityEngine.Object referenceObject, Track track, int frame)
        {
            Clip clip = track.AddClip(referenceObject, frame);

            Init();
            return clip;
        }
        public void RemoveClip(Clip clip)
        {
            clip.Track.RemoveClip(clip);

            Init();
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
            UnityEditor.Undo.RegisterCompleteObjectUndo(SerializedOwner, $"Timeline: {name}");
            SerializedTimeline.Update();
            action?.Invoke();
            UnityEditor.EditorUtility.SetDirty(SerializedOwner);
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
