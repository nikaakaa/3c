#if UNITY_EDITOR
using System;
using BTSMTL.Timeline;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor
{
    sealed class TimelineFormalClipDetailsView : VisualElement
    {
        readonly Clip m_Clip;
        readonly Action<Action, string> m_Apply;

        public TimelineFormalClipDetailsView(Clip clip, Action<Action, string> apply)
        {
            m_Clip = clip ?? throw new ArgumentNullException(nameof(clip));
            m_Apply = apply ?? throw new ArgumentNullException(nameof(apply));
            name = "timeline-formal-clip-details";
            Build();
        }

        void Build()
        {
            Add(new Label("Formal Timeline Fields"));
            AddInteger("Start Frame", m_Clip.StartFrame, value =>
                Modify("Set Clip Start Frame", () =>
                {
                    m_Clip.StartFrame = Mathf.Max(0, value);
                    m_Clip.EndFrame = Mathf.Max(m_Clip.StartFrame + 1, m_Clip.EndFrame);
                }));
            AddInteger("End Frame", m_Clip.EndFrame, value =>
                Modify("Set Clip End Frame", () =>
                {
                    m_Clip.EndFrame = Mathf.Max(m_Clip.StartFrame + 1, value);
                    if (m_Clip is MotionCurveClip motion)
                        motion.CurveEndFrame = Mathf.Clamp(motion.CurveEndFrame, motion.StartFrame + 1, motion.EndFrame);
                }));

            if (m_Clip is MotionCurveClip motionCurve)
                BuildMotionCurve(motionCurve);
            else if (m_Clip is AnimationClip animation)
                BuildAnimation(animation);
            else if (m_Clip is CameraStateClip cameraState)
                BuildCameraState(cameraState);
            else if (m_Clip is CameraCueClip cameraCue)
                BuildCameraCue(cameraCue);
            else if (m_Clip is CameraResponseClip cameraResponse)
                BuildCameraResponse(cameraResponse);
            else if (m_Clip is ActionCueClip actionCue)
                BuildActionCue(actionCue);
        }

        void BuildMotionCurve(MotionCurveClip clip)
        {
            AddText("Curve Id", clip.CurveId, value => Modify("Set Motion Curve Id", () => clip.CurveId = value));
            AddInteger("Curve End Frame", clip.CurveEndFrame, value => Modify("Set Motion Curve End Frame", () => clip.CurveEndFrame = Mathf.Clamp(value, clip.StartFrame + 1, clip.EndFrame)));
            AddEnum("Space", clip.Space, value => Modify("Set Motion Space", () => clip.Space = (TimelineMotionContributionSpace)value));
            AddEnum("Channel", clip.Channel, value => Modify("Set Motion Channel", () => clip.Channel = (TimelineMotionChannel)value));
            AddEnum("Blend Mode", clip.BlendMode, value => Modify("Set Motion Blend Mode", () => clip.BlendMode = (TimelineMotionBlendMode)value));
            AddInteger("Priority", clip.Priority, value => Modify("Set Motion Priority", () => clip.Priority = value));
            AddToggle("Consume Lower Channels", clip.ConsumeLowerChannels, value => Modify("Set Motion Consume Lower", () => clip.ConsumeLowerChannels = value));
        }

        void BuildAnimation(AnimationClip clip)
        {
            var resource = new ObjectField("Animation Clip")
            {
                objectType = typeof(UnityEngine.AnimationClip),
                allowSceneObjects = false,
                value = clip.Clip
            };
            resource.RegisterValueChangedCallback(evt => Modify("Set Animation Clip", () =>
            {
                clip.Clip = evt.newValue as UnityEngine.AnimationClip;
                clip.EndFrame = clip.StartFrame + Mathf.Max(1, Mathf.RoundToInt((clip.Clip ? clip.Clip.length : 0.05f) * TimelineUtility.FrameRate));
            }));
            Add(resource);
            AddEnum("Extrapolation", clip.ExtraPolationMode, value => Modify("Set Animation Extrapolation", () => clip.ExtraPolationMode = (ExtraPolationMode)value));
            AddText("Blend Profile", clip.BlendProfileId, value => Modify("Set Animation Blend Profile", () => clip.BlendProfileId = value));
        }

        void BuildCameraState(CameraStateClip clip)
        {
            AddEnum("Mode", clip.Mode, value => Modify("Set Camera Mode", () => clip.Mode = (TimelineCameraMode)value));
            AddInteger("Priority", clip.Priority, value => Modify("Set Camera Priority", () => clip.Priority = value));
            AddFloat("Blend In Seconds", clip.BlendInSeconds, value => Modify("Set Camera Blend In", () => clip.BlendInSeconds = Mathf.Max(0f, value)));
            AddFloat("Blend Out Seconds", clip.BlendOutSeconds, value => Modify("Set Camera Blend Out", () => clip.BlendOutSeconds = Mathf.Max(0f, value)));
            AddText("Target Key", clip.TargetKey, value => Modify("Set Camera Target", () => clip.TargetKey = value));
            AddEnum("Interrupt Policy", clip.InterruptPolicy, value => Modify("Set Camera Interrupt Policy", () => clip.InterruptPolicy = (TimelineCameraInterruptPolicy)value));
        }

        void BuildCameraCue(CameraCueClip clip)
        {
            AddText("Cue Id", clip.CueId, value => Modify("Set Camera Cue Id", () => clip.CueId = value));
            AddEnum("Cue Kind", clip.CueKind, value => Modify("Set Camera Cue Kind", () => clip.CueKind = (TimelineCameraCueKind)value));
            AddText("Cue Type", clip.CueType, value => Modify("Set Camera Cue Type", () => clip.CueType = value));
            AddFloat("Intensity", clip.Intensity, value => Modify("Set Camera Cue Intensity", () => clip.Intensity = Mathf.Max(0f, value)));
            AddFloat("Duration Seconds", clip.DurationSeconds, value => Modify("Set Camera Cue Duration", () => clip.DurationSeconds = Mathf.Max(0f, value)));
            AddInteger("Priority", clip.Priority, value => Modify("Set Camera Cue Priority", () => clip.Priority = value));
        }

        void BuildCameraResponse(CameraResponseClip clip)
        {
            AddEnum("Look Response", clip.LookResponse, value => Modify("Set Camera Response Mode", () => clip.LookResponse = (TimelineCameraLookResponseMode)value));
            AddFloat("Manual Orbit Weight", clip.ManualOrbitWeight, value => Modify("Set Manual Orbit Weight", () => clip.ManualOrbitWeight = Mathf.Clamp01(value)));
            AddFloat("Pitch Response Weight", clip.PitchResponseWeight, value => Modify("Set Pitch Response Weight", () => clip.PitchResponseWeight = Mathf.Clamp01(value)));
            AddFloat("Yaw Response Weight", clip.YawResponseWeight, value => Modify("Set Yaw Response Weight", () => clip.YawResponseWeight = Mathf.Clamp01(value)));
            AddInteger("Priority", clip.Priority, value => Modify("Set Camera Response Priority", () => clip.Priority = value));
        }

        void BuildActionCue(ActionCueClip clip)
        {
            AddText("Cue Id", clip.CueId, value => Modify("Set Action Cue Id", () => clip.CueId = value));
            AddText("Cue Type", clip.CueType, value => Modify("Set Action Cue Type", () => clip.CueType = value));
        }

        void Modify(string name, Action action)
        {
            m_Apply(() =>
            {
                action();
                m_Clip.Track.UpdateMix();
                m_Clip.Timeline.Init();
            }, name);
        }

        void AddText(string label, string value, Action<string> changed)
        {
            var field = new TextField(label) { value = value ?? string.Empty };
            field.RegisterValueChangedCallback(evt => changed(evt.newValue));
            Add(field);
        }

        void AddInteger(string label, int value, Action<int> changed)
        {
            var field = new IntegerField(label) { value = value };
            field.RegisterValueChangedCallback(evt => changed(evt.newValue));
            Add(field);
        }

        void AddFloat(string label, float value, Action<float> changed)
        {
            var field = new FloatField(label) { value = value };
            field.RegisterValueChangedCallback(evt => changed(evt.newValue));
            Add(field);
        }

        void AddToggle(string label, bool value, Action<bool> changed)
        {
            var field = new Toggle(label) { value = value };
            field.RegisterValueChangedCallback(evt => changed(evt.newValue));
            Add(field);
        }

        void AddEnum(string label, Enum value, Action<Enum> changed)
        {
            var field = new EnumField(label, value);
            field.RegisterValueChangedCallback(evt => changed(evt.newValue));
            Add(field);
        }
    }
}
#endif
