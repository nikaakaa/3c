using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Audio;

namespace Taco.Timeline
{
    [AcceptableTrackGroups("Base")]
    public partial class Timeline : ScriptableObject
    {
        [SerializeReference]
        protected List<Track> m_Tracks = new List<Track>();
        public List<Track> Tracks => m_Tracks;

        public event Action OnEvaluated;
        public event Action OnRebind;
        public event Action OnDone;
        public event Action OnBindStateChanged;
        public event Action OnValueChanged;

        float m_Time;
        public float Time
        {
            get => m_Time;
            set
            {
                m_Time = value;
                OnEvaluated?.Invoke();
            }
        }
        public int Frame => Mathf.RoundToInt(Time * TimelineUtility.FrameRate);

        public int MaxFrame { get; protected set; }
        public float Duration { get; protected set; }

        bool m_Binding;
        public bool Binding { get => m_Binding; protected set => m_Binding = value; }

        TimelinePlayer m_TimelinePlayer;
        public TimelinePlayer TimelinePlayer { get => m_TimelinePlayer; protected set=> m_TimelinePlayer = value; }
        public PlayableGraph PlayableGraph { get; protected set; }
        public AnimationLayerMixerPlayable AnimationRootPlayable { get; protected set; }
        public AudioMixerPlayable AudioRootPlayable { get; protected set; }

        public void Init()
        {
            #region Unbind
            bool isBinding = Binding;
            TimelinePlayer timelinePlayer = TimelinePlayer;
            if (isBinding)
            {
                timelinePlayer.Dispose();
            }
            #endregion

            #region Init
            m_Tracks.ForEach(t => t.Init(this));
            MaxFrame = 0;
            foreach (var track in m_Tracks)
            {
                track.Init(this);
                if (track.MaxFrame > MaxFrame)
                    MaxFrame = track.MaxFrame;
            }
            Duration = (float)MaxFrame / TimelineUtility.FrameRate;
            OnValueChanged?.Invoke();
            #endregion

            #region Bind
            if (isBinding)
            {
                timelinePlayer.Init();
                timelinePlayer.AddTimeline(this);
            }
            #endregion
        }
        public void Evaluate(float deltaTime)
        {
            Time += deltaTime;
            Tracks.ForEach(t => t.Evaluate(deltaTime));
            
            if (Time > Duration)
            {
                OnDone?.Invoke();
                OnDone = null;
            }
        }

        public void Bind(TimelinePlayer timelinePlayer)
        {
            Time = 0;
            TimelinePlayer = timelinePlayer;
            PlayableGraph = timelinePlayer.PlayableGraph;
            AnimationRootPlayable = timelinePlayer.AnimationRootPlayable;
            AudioRootPlayable = timelinePlayer.AudioRootPlayable;

            Binding = true;
            OnRebind = null;
            OnValueChanged += RebindAll;

            m_Tracks.ForEach(t => t.Bind());

            OnBindStateChanged?.Invoke();
        }
        public void Unbind()
        {
            m_Tracks.ForEach(t => t.Unbind());

            Binding = false;
            OnRebind = null;
            OnValueChanged -= RebindAll;

            AnimationRootPlayable = AnimationLayerMixerPlayable.Null;
            AudioRootPlayable = default;
            PlayableGraph = default;
            TimelinePlayer = null;

            OnBindStateChanged?.Invoke();
        }
        public void JumpTo(float targetTime)
        {
            float deltaTime = targetTime - Time;
            TimelinePlayer.AdditionalDelta = deltaTime;
        }
        public void RebindAll()
        {
            if (Binding)
            {
                OnRebind?.Invoke();
                OnRebind = null;

                foreach (var track in m_Tracks)
                {
                    track.Rebind();
                    track.SetTime(Time);
                }
                TimelinePlayer.Evaluate(0);
            }
        }
        public void RebindTrack(Track track)
        {
            if (Binding)
            {
                track.Rebind();
                track.SetTime(Time);
                TimelinePlayer.Evaluate(0);
            }
        }
        public void RuntimeMute(int index, bool value)
        {
            if (0 <= index && index < m_Tracks.Count)
                RuntimeMute(m_Tracks[index], value);
        }
        public void RuntimeMute(Track track, bool value)
        {
            track.RuntimeMute(value);
        }
    }

    [Serializable]
    public abstract partial class Track
    {
        public string Name;

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

        protected bool m_RuntimeMuted;
        public bool RuntimeMuted
        {
            get => m_RuntimeMuted;
            set
            {
                if (m_RuntimeMuted != value)
                {
                    m_RuntimeMuted = value;
                    OnMutedStateChanged?.Invoke();
                }
            }
        }

        [SerializeReference]
        protected List<Clip> m_Clips = new List<Clip>();
        public List<Clip> Clips => m_Clips;

        public Action OnUpdateMix;
        public Action OnMutedStateChanged;

        public Timeline Timeline { get; protected set; }
        public int MaxFrame { get; protected set; }

        public virtual void Init(Timeline timeline)
        {
            Timeline = timeline;

            MaxFrame = 0;
            foreach (var clip in m_Clips)
            {
                clip.Init(this);
                if (clip.EndFrame > MaxFrame)
                    MaxFrame = clip.EndFrame;
            }

            RuntimeMuted = false;
        }
        public virtual void Bind()
        {
            m_Clips.ForEach(c => c.Bind());
        }
        public virtual void Unbind()
        {
            m_Clips.ForEach(c => c.Unbind());
        }
        public virtual void Rebind()
        {
            Unbind();
            Bind();
        }

        public virtual void Evaluate(float deltaTime)
        {
            if (m_PersistentMuted || m_RuntimeMuted)
                return;
            m_Clips.ForEach(c => c.Evaluate(deltaTime));
        }
        public virtual void SetTime(float time)
        {
            if (m_PersistentMuted || m_RuntimeMuted)
                return;
            m_Clips.ForEach(c => c.Evaluate(time));
        }
        public virtual void RuntimeMute(bool value)
        {
            if (PersistentMuted)
                return;

            if (value && !RuntimeMuted)
            {
                RuntimeMuted = true;
                Unbind();
            }
            else if (!value && RuntimeMuted)
            {
                RuntimeMuted = false;
                Bind();
                SetTime(Timeline.Time);
            }
        }
    }

    [Serializable]
    public abstract partial class Clip
    {
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

        [ShowInInspector(99), OnValueChanged("RebindTimeline")]
        public bool CanSkip;

        [NonSerialized]
        public Track Track;
        public Timeline Timeline => Track.Timeline;

        public bool Active { get; protected set; }
        public float Time { get; protected set; }
        public float TargetTime { get; protected set; }
        public float OffsetTime => Time - StartTime + ClipInTime;

        public Action OnNameChanged;
        public Action OnInspectorRepaint;

        public virtual void Init(Track track)
        {
            Track = track;
            FrameToTime();
        }
        public virtual void Bind()
        {
            Active = false;
            Time = 0;
        }
        public virtual void Unbind()
        {
            if (Active)
                OnDisable();

            Active = false;
            Time = 0;
        }

        public virtual void Evaluate(float deltaTime)
        {
            TargetTime = Time + deltaTime;

            if (!Active && StartTime <= TargetTime && TargetTime <= EndTime)
            {
                Active = true;
                OnEnable();
            }
            else if (Active && (TargetTime < StartTime || EndTime < TargetTime))
            {
                Active = false;
                OnDisable();
            }

            if (!CanSkip)
            {
                if (!Active && Time < StartTime && EndTime < TargetTime)
                {
                    Active = true;
                    OnEnable();
                    Active = false;
                    OnDisable();
                }
                else if (!Active && TargetTime < StartTime && EndTime < Time)
                {
                    Active = true;
                    OnEnable();
                    Active = false;
                    OnDisable();
                }
            }

            Time = TargetTime;
        }
        public virtual void OnEnable()
        {
            
        }
        public virtual void OnDisable()
        {
            
        }
        
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

    public abstract partial class SignalClip : Clip
    {
        public override void Evaluate(float deltaTime)
        {
            TargetTime = Time + deltaTime;

            if (!Active && StartTime <= TargetTime)
            {
                Active = true;
                OnEnable();
            }
            else if (Active && TargetTime < StartTime)
            {
                Active = false;
                OnDisable();
            }

            Time = TargetTime;
        }
    }

#if UNITY_EDITOR

    public partial class Timeline
    {
        public float Scale = 1;
        public UnityEditor.SerializedObject SerializedTimeline;

        public void AddTrack(Type type)
        {
            Track track = Activator.CreateInstance(type) as Track;
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
            UnityEditor.Undo.RegisterCompleteObjectUndo(this, $"Timeline: {name}");
            SerializedTimeline.Update();
            action?.Invoke();
            UnityEditor.EditorUtility.SetDirty(this);
        }
        public void UpdateSerializedTimeline()
        {
            SerializedTimeline = new UnityEditor.SerializedObject(this);
        }



        [UnityEditor.MenuItem("Assets/Create/Taco/Timeline/Timeline", false, -996)]
        public static void CreateTimeline()
        {
            Timeline timeline = CreateInstance<Timeline>();
            string path = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.Selection.activeObject);
            string assetPathAndName = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(path + "/New Timeline.asset");
            UnityEditor.AssetDatabase.CreateAsset(timeline, assetPathAndName);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            UnityEditor.Selection.activeObject = timeline;
        }
    }

    public abstract partial class Track
    {
        public virtual Type ClipType => typeof(Clip);

        public virtual Clip AddClip(int frame)
        {
            Clip clip = Activator.CreateInstance(ClipType, this, frame) as Clip;
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
            Timeline.RebindTrack(this);
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
