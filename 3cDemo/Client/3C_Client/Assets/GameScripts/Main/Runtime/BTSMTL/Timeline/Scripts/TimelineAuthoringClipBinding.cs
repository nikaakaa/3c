#if UNITY_EDITOR
using System;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public enum TimelineAuthoringPropertyKind : byte
    {
        Text,
        Boolean,
        Integer,
        Float,
        Enum,
        Vector2,
        Object
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public sealed class TimelineAuthoringPropertyAttribute : Attribute
    {
        public TimelineAuthoringPropertyAttribute(
            string propertyId,
            TimelineAuthoringPropertyKind kind)
        {
            PropertyId = propertyId ?? string.Empty;
            Kind = kind;
        }

        public TimelineAuthoringPropertyAttribute(string propertyId, Type enumType)
            : this(propertyId, TimelineAuthoringPropertyKind.Enum)
        {
            EnumType = enumType;
        }

        public string PropertyId { get; }
        public TimelineAuthoringPropertyKind Kind { get; }
        public Type EnumType { get; }
        public bool Optional { get; set; }
        public bool Trimmed { get; set; }
        public bool HasMinimum { get; set; }
        public double Minimum { get; set; }
        public bool HasMaximum { get; set; }
        public double Maximum { get; set; }
        public bool Finite { get; set; }
    }

    public interface ITimelineAuthoringClipResolver
    {
        bool TryResolveMotionClip(TimelineData timeline, string identity, out MotionCurveClip clip);
    }

    public sealed class TimelineAuthoringClipConfiguration
    {
        public ExtraPolationMode Extrapolation { get; set; }
        public string BlendProfileId { get; set; }
        public string CurveId { get; set; }
        public int CurveEndFrame { get; set; }
        public TimelineMotionContributionSpace Space { get; set; }
        public TimelineMotionChannel Channel { get; set; }
        public TimelineMotionBlendMode BlendMode { get; set; }
        public int Priority { get; set; }
        public bool ConsumeLowerChannels { get; set; }
        public string SourceMotionClipId { get; set; }
        public MotionWarpTranslationMode TranslationMode { get; set; }
        public MotionWarpTargetOffsetSpace TargetOffsetSpace { get; set; }
        public MotionWarpRotationMode RotationMode { get; set; }
        public MotionWarpRotationMethod RotationMethod { get; set; }
        public Vector2 TargetPlanarOffset { get; set; }
        public float TargetYawOffsetDegrees { get; set; }
        public float MaxTotalPositionCorrection { get; set; }
        public float MaxTotalYawCorrectionDegrees { get; set; }
        public float MaximumYawRateDegreesPerSecond { get; set; }
        public MotionWarpLimitPolicy LimitPolicy { get; set; }
        public AnimationCurve PositionProgressCurve { get; set; }
        public AnimationCurve YawProgressCurve { get; set; }
        public string CueId { get; set; }
        public string CueType { get; set; }
        public TimelineCameraMode CameraMode { get; set; }
        public float CameraBlendInSeconds { get; set; }
        public float CameraBlendOutSeconds { get; set; }
        public string CameraTargetKey { get; set; }
        public TimelineCameraInterruptPolicy CameraInterruptPolicy { get; set; }
        public TimelineCameraCueKind CameraCueKind { get; set; }
        public float CameraIntensity { get; set; }
        public float CameraDurationSeconds { get; set; }
        public TimelineCameraLookResponseMode CameraLookResponse { get; set; }
        public float ManualOrbitWeight { get; set; }
        public float PitchResponseWeight { get; set; }
        public float YawResponseWeight { get; set; }
        public string TargetBindingId { get; set; }
        public string ParameterBindingId { get; set; }
        public AnimationCurve ValueCurve { get; set; }
    }

    public static class TimelineAuthoringClipBinding
    {
        public static TimelineAuthoringClipConfiguration Read(Clip clip)
        {
            if (clip == null)
                throw new ArgumentNullException(nameof(clip));
            var result = new TimelineAuthoringClipConfiguration();
            if (clip is AnimationClip animation)
            {
                result.Extrapolation = animation.ExtraPolationMode;
                result.BlendProfileId = animation.BlendProfileId;
            }
            if (clip is MotionCurveClip motion)
            {
                result.CurveId = motion.CurveId;
                result.CurveEndFrame = motion.CurveEndFrame;
                result.Space = motion.Space;
                result.Channel = motion.Channel;
                result.BlendMode = motion.BlendMode;
                result.Priority = motion.Priority;
                result.ConsumeLowerChannels = motion.ConsumeLowerChannels;
            }
            if (clip is MotionWarpClip warp)
            {
                result.SourceMotionClipId = warp.SourceMotionClipId;
                result.TranslationMode = warp.TranslationMode;
                result.TargetOffsetSpace = warp.TargetOffsetSpace;
                result.RotationMode = warp.RotationMode;
                result.RotationMethod = warp.RotationMethod;
                result.TargetPlanarOffset = warp.TargetPlanarOffset;
                result.TargetYawOffsetDegrees = warp.TargetYawOffsetDegrees;
                result.MaxTotalPositionCorrection = warp.MaxTotalPositionCorrection;
                result.MaxTotalYawCorrectionDegrees = warp.MaxTotalYawCorrectionDegrees;
                result.MaximumYawRateDegreesPerSecond = warp.MaximumYawRateDegreesPerSecond;
                result.LimitPolicy = warp.LimitPolicy;
                result.PositionProgressCurve = warp.PositionProgressCurve;
                result.YawProgressCurve = warp.YawProgressCurve;
            }
            if (clip is ActionCueClip actionCue)
            {
                result.CueId = actionCue.CueId;
                result.CueType = actionCue.CueType;
            }
            if (clip is CameraStateClip cameraState)
            {
                result.CameraMode = cameraState.Mode;
                result.Priority = cameraState.Priority;
                result.CameraBlendInSeconds = cameraState.BlendInSeconds;
                result.CameraBlendOutSeconds = cameraState.BlendOutSeconds;
                result.CameraTargetKey = cameraState.TargetKey;
                result.CameraInterruptPolicy = cameraState.InterruptPolicy;
            }
            if (clip is CameraCueClip cameraCue)
            {
                result.CueId = cameraCue.CueId;
                result.CameraCueKind = cameraCue.CueKind;
                result.CueType = cameraCue.CueType;
                result.CameraIntensity = cameraCue.Intensity;
                result.CameraDurationSeconds = cameraCue.DurationSeconds;
                result.Priority = cameraCue.Priority;
            }
            if (clip is CameraResponseClip cameraResponse)
            {
                result.CameraLookResponse = cameraResponse.LookResponse;
                result.ManualOrbitWeight = cameraResponse.ManualOrbitWeight;
                result.PitchResponseWeight = cameraResponse.PitchResponseWeight;
                result.YawResponseWeight = cameraResponse.YawResponseWeight;
                result.Priority = cameraResponse.Priority;
            }
            if (clip is ScenePresentationParameterCurveClip sceneParameter)
            {
                result.TargetBindingId = sceneParameter.TargetBindingId;
                result.ParameterBindingId = sceneParameter.ParameterBindingId;
                result.ValueCurve = sceneParameter.ValueCurve;
            }
            return result;
        }

        public static void Configure(
            TimelineData timeline,
            Clip clip,
            TimelineAuthoringClipConfiguration configuration,
            ITimelineAuthoringClipResolver resolver)
        {
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            if (clip == null)
                throw new ArgumentNullException(nameof(clip));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (clip is AnimationClip animation)
            {
                animation.ExtraPolationMode = configuration.Extrapolation;
                animation.BlendProfileId = configuration.BlendProfileId;
            }
            if (clip is MotionCurveClip motion)
            {
                motion.CurveId = configuration.CurveId;
                motion.CurveEndFrame = configuration.CurveEndFrame;
                motion.Space = configuration.Space;
                motion.Channel = configuration.Channel;
                motion.BlendMode = configuration.BlendMode;
                motion.Priority = configuration.Priority;
                motion.ConsumeLowerChannels = configuration.ConsumeLowerChannels;
            }
            if (clip is MotionWarpClip warp)
            {
                warp.ConfigureAuthoring(
                    configuration.TranslationMode,
                    configuration.TargetOffsetSpace,
                    configuration.RotationMode,
                    configuration.RotationMethod,
                    configuration.TargetPlanarOffset,
                    configuration.TargetYawOffsetDegrees,
                    configuration.MaxTotalPositionCorrection,
                    configuration.MaxTotalYawCorrectionDegrees,
                    configuration.MaximumYawRateDegreesPerSecond,
                    configuration.LimitPolicy,
                    configuration.PositionProgressCurve,
                    configuration.YawProgressCurve);
                if (!string.IsNullOrEmpty(configuration.SourceMotionClipId) &&
                    resolver != null &&
                    resolver.TryResolveMotionClip(timeline, configuration.SourceMotionClipId, out MotionCurveClip source))
                    MotionWarpAuthoring.BindSource(timeline, warp, source);
            }
            if (clip is ActionCueClip actionCue)
            {
                actionCue.CueId = configuration.CueId;
                actionCue.CueType = configuration.CueType;
            }
            if (clip is CameraStateClip cameraState)
            {
                cameraState.Mode = configuration.CameraMode;
                cameraState.Priority = configuration.Priority;
                cameraState.BlendInSeconds = configuration.CameraBlendInSeconds;
                cameraState.BlendOutSeconds = configuration.CameraBlendOutSeconds;
                cameraState.TargetKey = configuration.CameraTargetKey;
                cameraState.InterruptPolicy = configuration.CameraInterruptPolicy;
            }
            if (clip is CameraCueClip cameraCue)
            {
                cameraCue.CueId = configuration.CueId;
                cameraCue.CueKind = configuration.CameraCueKind;
                cameraCue.CueType = configuration.CueType;
                cameraCue.Intensity = configuration.CameraIntensity;
                cameraCue.DurationSeconds = configuration.CameraDurationSeconds;
                cameraCue.Priority = configuration.Priority;
            }
            if (clip is CameraResponseClip cameraResponse)
            {
                cameraResponse.LookResponse = configuration.CameraLookResponse;
                cameraResponse.ManualOrbitWeight = configuration.ManualOrbitWeight;
                cameraResponse.PitchResponseWeight = configuration.PitchResponseWeight;
                cameraResponse.YawResponseWeight = configuration.YawResponseWeight;
                cameraResponse.Priority = configuration.Priority;
            }
            if (clip is ScenePresentationParameterCurveClip sceneParameter)
                sceneParameter.ConfigureBindings(
                    configuration.TargetBindingId,
                    configuration.ParameterBindingId,
                    configuration.ValueCurve);
        }
    }
}
#endif
