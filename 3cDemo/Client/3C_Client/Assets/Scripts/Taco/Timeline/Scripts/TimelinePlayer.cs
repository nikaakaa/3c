using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Audio;
using UnityEngine.Playables;

namespace Taco.Timeline
{
    public interface ITimelinePlayerProvider
    {
        TimelinePlayer GetTimelinePlayer();
    }

    [RequireComponent(typeof(Animator))]
    public class TimelinePlayer : MonoBehaviour, ITimelinePlayerProvider
    {
        public RuntimeAnimatorController Controller;
        public bool ApplyRootMotion;

        public TimelinePlayer GetTimelinePlayer()
        {
            return this;
        }

        bool m_IsPlaying;
        public bool IsPlaying
        {
            get => m_IsPlaying;
            set
            {
                if (m_IsPlaying == value)
                    return;

                m_IsPlaying = value;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (m_IsPlaying)
                    {
                        UnityEditor.EditorApplication.update += EditorUpdate;
                    }
                    else
                    {
                        UnityEditor.EditorApplication.update -= EditorUpdate;
                    }
                }
#endif
            }
        }

        double m_PlaySpeed;
        public double PlaySpeed
        {
            get => Math.Round(Math.Max(0.001f, m_PlaySpeed),2);
            set => m_PlaySpeed = value;
        }

        public bool IsValid => PlayableGraph.IsValid();
        public Animator Animator { get; private set; }
        public AudioSource AudioSource { get; private set; }
        public PlayableGraph PlayableGraph { get; private set; }
        public AnimationLayerMixerPlayable AnimationRootPlayable { get; private set; }
        public AnimatorControllerPlayable CtrlPlayable { get; private set; }
        public AudioMixerPlayable AudioRootPlayable { get; private set; }
        public List<Timeline> RunningTimelines { get; private set; }
        public List<RuntimeTrackEaseOut> RuntimeTrackEaseOuts { get; private set; }


        public float AdditionalDelta { get; set; }

        public event Action OnEvaluated;

        protected virtual void OnEnable()
        {
            Init();
            IsPlaying = true;
        }
        protected virtual void OnDisable()
        {
            Dispose();
        }
        protected virtual void Update()
        {
            //if (IsPlaying)
            //{
            //    Evaluate(Time.deltaTime * (float)PlaySpeed);
            //    if (AdditionalDelta > 0)
            //    {
            //        Evaluate(AdditionalDelta);
            //        AdditionalDelta = 0;
            //    }
            //}
        }
        protected virtual void FixedUpdate()
        {
            if (IsPlaying)
            {
                Evaluate(Time.deltaTime * (float)PlaySpeed);
                if (AdditionalDelta != 0)
                {
                    Evaluate(AdditionalDelta);
                    AdditionalDelta = 0;
                }
            }
        }
        private void OnAnimatorMove() { }


        public virtual void Init()
        {
            PlayableGraph = PlayableGraph.Create("Taco.Timeline.PlayableGraph");
            AnimationRootPlayable = AnimationLayerMixerPlayable.Create(PlayableGraph);
            AudioRootPlayable = AudioMixerPlayable.Create(PlayableGraph);

            Animator = GetComponent<Animator>();
            var playableOutput = AnimationPlayableOutput.Create(PlayableGraph, "Animation", Animator);
            playableOutput.SetSourcePlayable(AnimationRootPlayable);

            AudioSource = GetComponent<AudioSource>();
            var audioOutput = AudioPlayableOutput.Create(PlayableGraph, "Audio", GetComponent<AudioSource>());
            audioOutput.SetSourcePlayable(AudioRootPlayable);
            audioOutput.SetEvaluateOnSeek(true);

            CtrlPlayable = AnimatorControllerPlayable.Create(PlayableGraph, Controller);
            AnimationRootPlayable.AddInput(CtrlPlayable, 0, 1);

            RunningTimelines = new List<Timeline>();
            RuntimeTrackEaseOuts = new List<RuntimeTrackEaseOut>();

            IsPlaying = false;
            PlaySpeed = 1;
        }
        public virtual void Dispose()
        {
            if (IsValid)
            {
                for (int i = RunningTimelines.Count - 1; i >= 0; i--)
                {
                    RemoveTimeline(RunningTimelines[i]);
                }
                PlayableGraph.Destroy();
            }
            RunningTimelines = null;
            IsPlaying = false;
            PlaySpeed = 1;
        }
        public virtual void Evaluate(float deltaTime)
        {
            for (int i = RunningTimelines.Count - 1; i >= 0; i--)
            {
                Timeline runningTimelines = RunningTimelines[i];
                runningTimelines.Evaluate(deltaTime);
            }

            EvaluatePlayableGraph(deltaTime);
        }

        public virtual void EvaluatePlayableGraph(float deltaTime)
        {
            PlayableGraph.Evaluate(deltaTime);

            for (int i = RuntimeTrackEaseOuts.Count - 1; i >= 0; i--)
            {
                RuntimeTrackEaseOut runtimeTrackEaseOut = RuntimeTrackEaseOuts[i];
                runtimeTrackEaseOut.Evaluate(deltaTime);
                if (runtimeTrackEaseOut.Timer >= runtimeTrackEaseOut.EaseOutTime)
                {
                    AnimationRootPlayable.DisconnectInput(runtimeTrackEaseOut.Index);
                    runtimeTrackEaseOut.Track.Destroy();
                    RuntimeTrackEaseOuts.Remove(runtimeTrackEaseOut);

                    CollapseAnimationRootInputsIfIdle();
                }
            }

            OnRootMotion();

            OnEvaluated?.Invoke();
        }
        protected virtual void OnRootMotion()
        {
            if (ApplyRootMotion)
                transform.position += Animator.deltaPosition;
        }

        #region Aniamtor
        public virtual void SetFloat(string name, float value)
        {
            CtrlPlayable.SetFloat(name, value);
        }
        public virtual float GetFloat(string name)
        {
            return CtrlPlayable.GetFloat(name);
        }

        public virtual void SetBool(string name, bool value)
        {
            CtrlPlayable.SetBool(name, value);
        }
        public virtual bool GetBool(string name)
        {
            return CtrlPlayable.GetBool(name);
        }

        public virtual void SetTrigger(string name)
        {
            CtrlPlayable.SetTrigger(name);
        }

        public virtual void SetStateTime(string name, float time, int layer)
        {
            CtrlPlayable.CrossFade(name, 0, layer, time);
        }
        #endregion

        public virtual void AddTimeline(Timeline timeline)
        {
            timeline.Init();
            timeline.Bind(this);
            RunningTimelines.Add(timeline);
            Evaluate(0);
        }
        public virtual void RemoveTimeline(Timeline timeline)
        {
            UnbindTimeline(timeline);
        }

        public virtual void UnbindTimeline(Timeline timeline)
        {
            if (timeline == null)
                return;

            timeline.Unbind();
            RunningTimelines?.Remove(timeline);
            CollapseAnimationRootInputsIfIdle();
        }

        public virtual void AddAnimationEaseOut(AnimationTrack animationTrack)
        {
            RuntimeTrackEaseOut runtimeTrackEaseOut = new RuntimeTrackEaseOut(AnimationRootPlayable, animationTrack);
            RuntimeTrackEaseOuts.Add(runtimeTrackEaseOut);
        }

        protected virtual void CollapseAnimationRootInputsIfIdle()
        {
            if (!IsValid || RunningTimelines == null || RuntimeTrackEaseOuts == null)
                return;

            if (RunningTimelines.Count == 0 && RuntimeTrackEaseOuts.Count == 0)
                AnimationRootPlayable.SetInputCount(1);
        }

#if UNITY_EDITOR

        public void EditorUpdate()
        {
            Evaluate((float)(Editor.TacoEditorUtility.DeltaTime * PlaySpeed));
            if (AdditionalDelta != 0)
            {
                Evaluate(AdditionalDelta);
                AdditionalDelta = 0;
            }
        }
#endif
    }

    public class RuntimeTrackEaseOut
    {
        public Playable Root;
        public Playable Track;
        public int Index;
        public float EaseOutTime;

        public float Timer;
        public float OriginalWeight;

        public RuntimeTrackEaseOut(Playable root, AnimationTrack animationTrack)
        {
            Root = root;
            Track = animationTrack.TrackPlayable.Handle;
            if(!animationTrack.PlayWhenEaseOut)
                Track.Pause();
            Index = animationTrack.PlayableIndex;
            EaseOutTime = animationTrack.EaseOutTime;
            
            OriginalWeight = Root.GetInputWeight(Index);
            Timer = 0;
        }
        public void Evaluate(float deltaTime)
        {
            Timer += deltaTime;
            Root.SetInputWeight(Index, Mathf.Lerp(OriginalWeight, 0, Timer / EaseOutTime));
        }
    }
}
