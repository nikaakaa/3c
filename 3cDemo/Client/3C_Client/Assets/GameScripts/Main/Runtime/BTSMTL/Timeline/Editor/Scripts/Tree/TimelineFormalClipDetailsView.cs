#if UNITY_EDITOR
using System;
using BTSMTL.Timeline;
using Slate;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor
{
    sealed class TimelineFormalClipDetailsView : VisualElement
    {
        readonly Clip m_Clip;
        readonly Action<Action, string> m_Apply;
        readonly IEmbeddedTimelineClipBinding m_SlateClip;
        readonly int m_FrameRate;

        public TimelineFormalClipDetailsView(Clip clip, Action<Action, string> apply)
            : this(clip, apply, null, TimelineUtility.FrameRate)
        {
        }

        public TimelineFormalClipDetailsView(
            Clip clip,
            Action<Action, string> apply,
            IEmbeddedTimelineClipBinding slateClip,
            int frameRate)
        {
            m_Clip = clip ?? throw new ArgumentNullException(nameof(clip));
            m_Apply = apply ?? throw new ArgumentNullException(nameof(apply));
            m_SlateClip = slateClip;
            m_FrameRate = Mathf.Max(1, frameRate);
            name = "timeline-formal-clip-details";
            Build();
        }

        void Build()
        {
            Add(new Label("Formal Timeline Fields"));
            Add(new Label($"Authoring Id: {m_Clip.AuthoringId}"));
            Add(new Label($"Contract: {m_Clip.ContractKind}  |  Length: {m_Clip.Duration}F ({m_Clip.Duration / (float)m_FrameRate:0.###}s)"));
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
            AddInteger("Blend In", m_Clip.SelfEaseInFrame, value => Modify("Set Clip Blend In", () =>
                m_Clip.SelfEaseInFrame = Mathf.Clamp(value, 0, Mathf.Max(0, m_Clip.Duration - 1))));
            AddInteger("Blend Out", m_Clip.SelfEaseOutFrame, value => Modify("Set Clip Blend Out", () =>
                m_Clip.SelfEaseOutFrame = Mathf.Clamp(value, 0, Mathf.Max(0, m_Clip.Duration - m_Clip.SelfEaseInFrame - 1))));
            AddInteger("Clip In", m_Clip.ClipInFrame, value => Modify("Set Clip In", () =>
                m_Clip.ClipInFrame = Mathf.Max(0, value)));

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
            else if (m_Clip is CameraResourceClip cameraResource)
                BuildCameraResource(cameraResource);
            else if (m_Clip is ActionCueClip actionCue)
                BuildActionCue(actionCue);
            else if (m_Clip is ScenePresentationParameterCurveClip sceneParameter)
                BuildScenePresentation(sceneParameter);

            if (m_SlateClip?.Keyable?.animationData?.animatedParameters != null &&
                m_SlateClip.Keyable.animationData.animatedParameters.Count > 0)
            {
                Add(new Label("Slate Curve Parameters"));
                var parameterView = new IMGUIContainer(DrawSlateCurveParameters);
                parameterView.style.minHeight = 28f;
                Add(parameterView);
            }
        }

        void DrawSlateCurveParameters()
        {
            IKeyable keyable = m_SlateClip?.Keyable;
            if (keyable?.animationData?.animatedParameters == null)
                return;

            EditorGUI.BeginChangeCheck();
            for (int index = 0; index < keyable.animationData.animatedParameters.Count; index++)
            {
                AnimatedParameter parameter = keyable.animationData.animatedParameters[index];
                if (parameter != null)
                    AnimatableParameterEditor.ShowParameter(parameter, keyable);
            }
            if (EditorGUI.EndChangeCheck())
                m_Apply(m_SlateClip.ApplyCurveEdits, "Edit Timeline Curve Parameters");
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
            AddText("Sequence Id", clip.SequenceId, value => Modify("Set Camera Sequence", () => clip.SequenceId = value));
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
            AddText("Resource Id", clip.ResourceId, value => Modify("Set Camera Cue Resource", () => clip.ResourceId = value));
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

        void BuildCameraResource(CameraResourceClip clip)
        {
            switch (clip)
            {
                case CameraOverrideClip cameraOverride:
                    AddObject(
                        "Override Track",
                        typeof(CameraOverrideTrackAsset),
                        cameraOverride.OverrideTrack,
                        value => Modify("Set Camera Override Track", () => cameraOverride.OverrideTrack = value as CameraOverrideTrackAsset));
                    break;
                case CameraZoomClip cameraZoom:
                    AddObject(
                        "Zoom",
                        typeof(CameraZoomAsset),
                        cameraZoom.Zoom,
                        value => Modify("Set Camera Zoom", () => cameraZoom.Zoom = value as CameraZoomAsset));
                    break;
                case CameraStretchClip cameraStretch:
                    AddObject(
                        "Stretch",
                        typeof(CameraStretchAsset),
                        cameraStretch.Stretch,
                        value => Modify("Set Camera Stretch", () => cameraStretch.Stretch = value as CameraStretchAsset));
                    break;
                case CameraShotClip cameraShot:
                    AddObject(
                        "Shot",
                        typeof(CameraShotAsset),
                        cameraShot.Shot,
                        value => Modify("Set Camera Shot", () => cameraShot.Shot = value as CameraShotAsset));
                    break;
            }
        }

        void BuildActionCue(ActionCueClip clip)
        {
            AddText("Cue Id", clip.CueId, value => Modify("Set Action Cue Id", () => clip.CueId = value));
            AddText("Cue Type", clip.CueType, value => Modify("Set Action Cue Type", () => clip.CueType = value));
        }

        void BuildScenePresentation(ScenePresentationParameterCurveClip clip)
        {
            AddText("Target Binding", clip.TargetBindingId, value => Modify("Set Scene Target Binding", () =>
                clip.ConfigureBindings(value, clip.ParameterBindingId, clip.ValueCurve)));
            AddText("Parameter Binding", clip.ParameterBindingId, value => Modify("Set Scene Parameter Binding", () =>
                clip.ConfigureBindings(clip.TargetBindingId, value, clip.ValueCurve)));
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

        void AddObject(
            string label,
            Type type,
            UnityEngine.Object value,
            Action<UnityEngine.Object> changed)
        {
            var field = new ObjectField(label)
            {
                objectType = type,
                allowSceneObjects = false,
                value = value
            };
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
