#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

namespace BTSMTL.Timeline.Editor
{
    sealed class TimelineClipCreationRequest
    {
        public string TrackAuthoringId;
        public string Kind;
        public int FrameRate;
        public int StartFrame;
        public int EndFrame;
        public int DefaultEndFrame;
        public UnityEngine.Object Resource;
        public RootMotionCurveAsset SourceCurve;
        public float SourceStartTime;
        public float SourceEndTime;
        public ExtraPolationMode Extrapolation = ExtraPolationMode.None;
        public string BlendProfileId = string.Empty;
        public string CurveId = "MotionCurve";
        public TimelineMotionContributionSpace Space = TimelineMotionContributionSpace.Local;
        public TimelineMotionChannel Channel = TimelineMotionChannel.Action;
        public TimelineMotionBlendMode BlendMode = TimelineMotionBlendMode.Override;
        public int Priority = 100;
        public bool ConsumeLowerChannels = true;
        public string SourceMotionClipId;
        public string CueId = "Cue";
        public string CueType = "Cue";
        public TimelineCameraMode CameraMode = TimelineCameraMode.SkillCloseup;
        public int CameraPriority = 100;
        public float CameraBlendInSeconds = 0.15f;
        public float CameraBlendOutSeconds = 0.2f;
        public string CameraTargetKey = string.Empty;
        public TimelineCameraInterruptPolicy CameraInterruptPolicy = TimelineCameraInterruptPolicy.BlendOut;
        public TimelineCameraCueKind CameraCueKind = TimelineCameraCueKind.Shake;
        public float CameraIntensity = 1f;
        public float CameraDurationSeconds = 0.2f;
        public TimelineCameraLookResponseMode CameraLookResponse = TimelineCameraLookResponseMode.Suppressed;
        public float ManualOrbitWeight;
        public float PitchResponseWeight = 1f;
        public float YawResponseWeight = 1f;
        public string TargetBindingId = string.Empty;
        public string ParameterBindingId = string.Empty;
    }

    sealed class TimelineClipCreationPopup : PopupWindowContent
    {
        readonly TimelineClipCreationRequest m_Request;
        readonly IReadOnlyList<string> m_MotionClipIds;
        readonly IReadOnlyList<TimelineExternalBindingDeclaration> m_Bindings;
        readonly Func<TimelineClipCreationRequest, string> m_Create;
        string m_Error;
        string m_SubmitError;

        public TimelineClipCreationPopup(
            TimelineClipCreationRequest request,
            IReadOnlyList<string> motionClipIds,
            IReadOnlyList<TimelineExternalBindingDeclaration> bindings,
            Func<TimelineClipCreationRequest, string> create)
        {
            m_Request = request;
            m_MotionClipIds = motionClipIds ?? Array.Empty<string>();
            m_Bindings = bindings ?? Array.Empty<TimelineExternalBindingDeclaration>();
            m_Create = create;
        }

        public override Vector2 GetWindowSize() => new Vector2(380, 460);

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField("Add Clip", EditorStyles.boldLabel);
            m_Request.StartFrame = EditorGUILayout.IntField("Start Frame", m_Request.StartFrame);
            m_Request.EndFrame = EditorGUILayout.IntField("End Frame", m_Request.EndFrame);

            if (m_Request.Kind == TimelineContractKinds.AnimationClip)
            {
                UnityEngine.Object previousResource = m_Request.Resource;
                m_Request.Resource = EditorGUILayout.ObjectField("Animation Clip", m_Request.Resource, typeof(UnityEngine.AnimationClip), false);
                if (!ReferenceEquals(previousResource, m_Request.Resource) && m_Request.Resource is UnityEngine.AnimationClip animation)
                {
                    int resourceEndFrame = m_Request.StartFrame + Mathf.Max(1, Mathf.RoundToInt(animation.length * m_Request.FrameRate));
                    if (m_Request.EndFrame == m_Request.DefaultEndFrame)
                        m_Request.EndFrame = resourceEndFrame;
                    m_Request.DefaultEndFrame = resourceEndFrame;
                }
                m_Request.Extrapolation = (ExtraPolationMode)EditorGUILayout.EnumPopup("Extrapolation", m_Request.Extrapolation);
                m_Request.BlendProfileId = EditorGUILayout.TextField("Blend Profile", m_Request.BlendProfileId);
            }
            else if (m_Request.Kind == TimelineContractKinds.TreeClip)
                m_Request.Resource = EditorGUILayout.ObjectField("Graph / Tree", m_Request.Resource, typeof(UnityEngine.Object), false);

            if (m_Request.Kind == TimelineContractKinds.MotionCurveClip)
            {
                RootMotionCurveAsset previousSource = m_Request.SourceCurve;
                m_Request.SourceCurve = (RootMotionCurveAsset)EditorGUILayout.ObjectField(
                    "Root Motion Curve",
                    m_Request.SourceCurve,
                    typeof(RootMotionCurveAsset),
                    false);
                if (!ReferenceEquals(previousSource, m_Request.SourceCurve) && m_Request.SourceCurve)
                {
                    m_Request.SourceStartTime = 0f;
                    m_Request.SourceEndTime = m_Request.SourceCurve.Duration;
                    int sourceEndFrame = m_Request.StartFrame + Mathf.Max(
                        1,
                        Mathf.RoundToInt(m_Request.SourceCurve.Duration * m_Request.FrameRate));
                    if (m_Request.EndFrame == m_Request.DefaultEndFrame)
                        m_Request.EndFrame = sourceEndFrame;
                    m_Request.DefaultEndFrame = sourceEndFrame;
                }
                m_Request.CurveId = EditorGUILayout.TextField("Curve Id", m_Request.CurveId);
                m_Request.SourceStartTime = EditorGUILayout.FloatField("Source Start (s)", m_Request.SourceStartTime);
                m_Request.SourceEndTime = EditorGUILayout.FloatField("Source End (s)", m_Request.SourceEndTime);
                m_Request.Space = (TimelineMotionContributionSpace)EditorGUILayout.EnumPopup("Space", m_Request.Space);
                m_Request.Channel = (TimelineMotionChannel)EditorGUILayout.EnumPopup("Channel", m_Request.Channel);
                m_Request.BlendMode = (TimelineMotionBlendMode)EditorGUILayout.EnumPopup("Blend Mode", m_Request.BlendMode);
                m_Request.Priority = EditorGUILayout.IntField("Priority", m_Request.Priority);
                m_Request.ConsumeLowerChannels = EditorGUILayout.Toggle("Consume Lower", m_Request.ConsumeLowerChannels);
            }
            else if (m_Request.Kind == TimelineContractKinds.MotionWarpClip)
            {
                var labels = new string[m_MotionClipIds.Count + 1];
                labels[0] = "Select source";
                int selected = 0;
                for (int index = 0; index < m_MotionClipIds.Count; index++)
                {
                    labels[index + 1] = m_MotionClipIds[index];
                    if (string.Equals(m_Request.SourceMotionClipId, m_MotionClipIds[index], StringComparison.Ordinal))
                        selected = index + 1;
                }
                int next = EditorGUILayout.Popup("Source Motion", selected, labels);
                m_Request.SourceMotionClipId = next > 0 ? m_MotionClipIds[next - 1] : string.Empty;
            }
            else if (m_Request.Kind == TimelineContractKinds.ActionCueClip)
            {
                m_Request.CueId = EditorGUILayout.TextField("Cue Id", m_Request.CueId);
                m_Request.CueType = EditorGUILayout.TextField("Cue Type", m_Request.CueType);
            }
            else if (m_Request.Kind == TimelineContractKinds.CameraStateClip)
            {
                m_Request.CameraMode = (TimelineCameraMode)EditorGUILayout.EnumPopup("Mode", m_Request.CameraMode);
                m_Request.CameraPriority = EditorGUILayout.IntField("Priority", m_Request.CameraPriority);
                m_Request.CameraBlendInSeconds = EditorGUILayout.FloatField("Blend In", m_Request.CameraBlendInSeconds);
                m_Request.CameraBlendOutSeconds = EditorGUILayout.FloatField("Blend Out", m_Request.CameraBlendOutSeconds);
                m_Request.CameraTargetKey = EditorGUILayout.TextField("Target Key", m_Request.CameraTargetKey);
                m_Request.CameraInterruptPolicy = (TimelineCameraInterruptPolicy)EditorGUILayout.EnumPopup("Interrupt", m_Request.CameraInterruptPolicy);
            }
            else if (m_Request.Kind == TimelineContractKinds.CameraCueClip)
            {
                m_Request.CueId = EditorGUILayout.TextField("Cue Id", m_Request.CueId);
                m_Request.CameraCueKind = (TimelineCameraCueKind)EditorGUILayout.EnumPopup("Cue Kind", m_Request.CameraCueKind);
                m_Request.CueType = EditorGUILayout.TextField("Cue Type", m_Request.CueType);
                m_Request.CameraIntensity = EditorGUILayout.FloatField("Intensity", m_Request.CameraIntensity);
                m_Request.CameraDurationSeconds = EditorGUILayout.FloatField("Duration", m_Request.CameraDurationSeconds);
                m_Request.CameraPriority = EditorGUILayout.IntField("Priority", m_Request.CameraPriority);
            }
            else if (m_Request.Kind == TimelineContractKinds.CameraResponseClip)
            {
                m_Request.CameraLookResponse = (TimelineCameraLookResponseMode)EditorGUILayout.EnumPopup("Look Response", m_Request.CameraLookResponse);
                m_Request.ManualOrbitWeight = EditorGUILayout.Slider("Manual Orbit", m_Request.ManualOrbitWeight, 0f, 1f);
                m_Request.PitchResponseWeight = EditorGUILayout.Slider("Pitch Response", m_Request.PitchResponseWeight, 0f, 1f);
                m_Request.YawResponseWeight = EditorGUILayout.Slider("Yaw Response", m_Request.YawResponseWeight, 0f, 1f);
                m_Request.CameraPriority = EditorGUILayout.IntField("Priority", m_Request.CameraPriority);
            }
            else if (m_Request.Kind == TimelineContractKinds.CameraOverrideClip)
                m_Request.Resource = EditorGUILayout.ObjectField("Override Track", m_Request.Resource, typeof(CameraOverrideTrackAsset), false);
            else if (m_Request.Kind == TimelineContractKinds.CameraZoomClip)
                m_Request.Resource = EditorGUILayout.ObjectField("Zoom", m_Request.Resource, typeof(CameraZoomAsset), false);
            else if (m_Request.Kind == TimelineContractKinds.CameraStretchClip)
                m_Request.Resource = EditorGUILayout.ObjectField("Stretch", m_Request.Resource, typeof(CameraStretchAsset), false);
            else if (m_Request.Kind == TimelineContractKinds.CameraShotClip)
                m_Request.Resource = EditorGUILayout.ObjectField("Shot", m_Request.Resource, typeof(CameraShotAsset), false);
            else if (m_Request.Kind == TimelineContractKinds.ScenePresentationParameterCurveClip)
            {
                BuildBindingPopup("Target Binding", true, ref m_Request.TargetBindingId);
                BuildBindingPopup("Parameter Binding", false, ref m_Request.ParameterBindingId);
            }

            m_Error = Validate();
            if (!string.IsNullOrEmpty(m_Error))
                EditorGUILayout.HelpBox(m_Error, MessageType.Error);
            if (!string.IsNullOrEmpty(m_SubmitError))
                EditorGUILayout.HelpBox(m_SubmitError, MessageType.Error);
            using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(m_Error)))
            {
                if (GUILayout.Button("Create"))
                {
                    string error = m_Create(m_Request);
                    if (string.IsNullOrEmpty(error))
                        editorWindow.Close();
                    else
                        m_SubmitError = error;
                }
            }
        }

        string Validate()
        {
            if (m_Request.StartFrame < 0 || m_Request.EndFrame <= m_Request.StartFrame)
                return "帧范围必须满足 Start < End。";
            if (m_Request.Kind == TimelineContractKinds.AnimationClip && m_Request.Resource is not UnityEngine.AnimationClip)
                return "必须选择已有 AnimationClip。";
            if (m_Request.Kind == TimelineContractKinds.TreeClip &&
                m_Request.Resource is not BaseTreeAsset &&
                m_Request.Resource is not ITimelineTreeGraphAsset)
                return "必须选择正式 Timeline Tree 来源。";
            if (m_Request.Kind == TimelineContractKinds.MotionCurveClip)
            {
                if (!m_Request.SourceCurve)
                    return "必须选择正式 RootMotionCurveAsset。";
                if (!m_Request.SourceCurve.TryValidate(out string sourceError))
                    return sourceError;
                if (m_Request.SourceStartTime < 0f ||
                    m_Request.SourceEndTime <= m_Request.SourceStartTime ||
                    m_Request.SourceEndTime > m_Request.SourceCurve.Duration)
                    return "Source 时间范围必须位于 RootMotionCurveAsset 内。";
                int sourceEndFrame = m_Request.StartFrame + Mathf.Max(
                    1,
                    Mathf.RoundToInt((m_Request.SourceEndTime - m_Request.SourceStartTime) * m_Request.FrameRate));
                if (sourceEndFrame > m_Request.EndFrame)
                    return "Clip End Frame 必须覆盖 Source 时间范围。";
            }
            if (m_Request.Kind == TimelineContractKinds.MotionWarpClip && string.IsNullOrEmpty(m_Request.SourceMotionClipId))
                return "MotionWarp 必须选择已有 MotionCurve 来源。";
            if (m_Request.Kind == TimelineContractKinds.CameraOverrideClip && m_Request.Resource is not CameraOverrideTrackAsset)
                return "Camera Override 必须选择正式 Override Track。";
            if (m_Request.Kind == TimelineContractKinds.CameraZoomClip && m_Request.Resource is not CameraZoomAsset)
                return "Camera Zoom 必须选择正式 Zoom 资源。";
            if (m_Request.Kind == TimelineContractKinds.CameraStretchClip && m_Request.Resource is not CameraStretchAsset)
                return "Camera Stretch 必须选择正式 Stretch 资源。";
            if (m_Request.Kind == TimelineContractKinds.CameraShotClip && m_Request.Resource is not CameraShotAsset)
                return "Camera Shot 必须选择正式 Shot 资源。";
            if (m_Request.Kind == TimelineContractKinds.ActionCueClip &&
                (string.IsNullOrWhiteSpace(m_Request.CueId) || string.IsNullOrWhiteSpace(m_Request.CueType)))
                return "Cue Id 和 Cue Type 必须填写。";
            if (m_Request.Kind == TimelineContractKinds.ScenePresentationParameterCurveClip &&
                (string.IsNullOrEmpty(m_Request.TargetBindingId) || string.IsNullOrEmpty(m_Request.ParameterBindingId)))
                return "Scene 参数 Clip 必须选择 Target 和 Parameter binding。";
            return string.Empty;
        }

        void BuildBindingPopup(string label, bool target, ref string value)
        {
            var options = new List<string> { "Select binding" };
            for (int index = 0; index < m_Bindings.Count; index++)
            {
                TimelineExternalBindingDeclaration binding = m_Bindings[index];
                bool valid = target
                    ? binding.ValueKind == TimelineBindingValueKind.Target && binding.Access == TimelineBindingAccess.Input && binding.Lifetime == TimelineBindingLifetime.Call
                    : (binding.ValueKind == TimelineBindingValueKind.Scalar || binding.ValueKind == TimelineBindingValueKind.Boolean) && binding.Access == TimelineBindingAccess.Write && binding.Lifetime == TimelineBindingLifetime.Tick;
                if (valid)
                    options.Add(binding.BindingId);
            }
            int selected = Mathf.Max(0, options.IndexOf(value) >= 0 ? options.IndexOf(value) : 0);
            int next = EditorGUILayout.Popup(label, selected, options.ToArray());
            value = next > 0 ? options[next] : string.Empty;
        }
    }
}
#endif
