#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
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

    public sealed class TimelineAuthoringClipExport
    {
        internal TimelineAuthoringClipExport(JObject properties, UnityEngine.AnimationClip animationAsset)
        {
            Properties = properties;
            AnimationAsset = animationAsset;
        }

        public JObject Properties { get; }
        public UnityEngine.AnimationClip AnimationAsset { get; }
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

        public static TimelineAuthoringClipExport Export(Clip clip)
        {
            if (clip == null)
                throw new ArgumentNullException(nameof(clip));
            var properties = new JObject();
            UnityEngine.AnimationClip animationAsset = null;
            if (clip is AnimationClip animation)
            {
                animationAsset = animation.Clip;
                properties["extraPolationMode"] = animation.ExtraPolationMode.ToString();
                properties["blendProfileId"] = animation.BlendProfileId;
            }
            if (clip is MotionCurveClip motion)
            {
                properties["curveId"] = motion.CurveId;
                properties["curveEndFrame"] = motion.CurveEndFrame;
                properties["space"] = motion.Space.ToString();
                properties["channel"] = motion.Channel.ToString();
                properties["blendMode"] = motion.BlendMode.ToString();
                properties["priority"] = motion.Priority;
                properties["consumeLowerChannels"] = motion.ConsumeLowerChannels;
            }
            if (clip is MotionWarpClip warp)
            {
                properties["sourceMotionClipId"] = warp.SourceMotionClipId;
                properties["translationMode"] = warp.TranslationMode.ToString();
                properties["targetOffsetSpace"] = warp.TargetOffsetSpace.ToString();
                properties["rotationMode"] = warp.RotationMode.ToString();
                properties["rotationMethod"] = warp.RotationMethod.ToString();
                properties["targetPlanarOffset"] = new JObject
                {
                    ["x"] = warp.TargetPlanarOffset.x,
                    ["y"] = warp.TargetPlanarOffset.y
                };
                properties["targetYawOffsetDegrees"] = warp.TargetYawOffsetDegrees;
                properties["maxTotalPositionCorrection"] = warp.MaxTotalPositionCorrection;
                properties["maxTotalYawCorrectionDegrees"] = warp.MaxTotalYawCorrectionDegrees;
                properties["maximumYawRateDegreesPerSecond"] = warp.MaximumYawRateDegreesPerSecond;
                properties["limitPolicy"] = warp.LimitPolicy.ToString();
            }
            if (clip is ActionCueClip actionCue)
            {
                properties["cueId"] = actionCue.CueId;
                properties["cueType"] = actionCue.CueType;
            }
            if (clip is CameraStateClip cameraState)
            {
                properties["mode"] = cameraState.Mode.ToString();
                properties["priority"] = cameraState.Priority;
                properties["blendInSeconds"] = cameraState.BlendInSeconds;
                properties["blendOutSeconds"] = cameraState.BlendOutSeconds;
                properties["targetKey"] = cameraState.TargetKey;
                properties["interruptPolicy"] = cameraState.InterruptPolicy.ToString();
            }
            if (clip is CameraCueClip cameraCue)
            {
                properties["cueId"] = cameraCue.CueId;
                properties["cueKind"] = cameraCue.CueKind.ToString();
                properties["cueType"] = cameraCue.CueType;
                properties["intensity"] = cameraCue.Intensity;
                properties["durationSeconds"] = cameraCue.DurationSeconds;
                properties["priority"] = cameraCue.Priority;
            }
            if (clip is CameraResponseClip cameraResponse)
            {
                properties["lookResponse"] = cameraResponse.LookResponse.ToString();
                properties["manualOrbitWeight"] = cameraResponse.ManualOrbitWeight;
                properties["pitchResponseWeight"] = cameraResponse.PitchResponseWeight;
                properties["yawResponseWeight"] = cameraResponse.YawResponseWeight;
                properties["priority"] = cameraResponse.Priority;
            }
            if (clip is ScenePresentationParameterCurveClip sceneParameter)
            {
                properties["targetBindingId"] = sceneParameter.TargetBindingId;
                properties["parameterBindingId"] = sceneParameter.ParameterBindingId;
                properties["valueCurve"] = CurveToken(sceneParameter.ValueCurve);
            }
            return new TimelineAuthoringClipExport(properties, animationAsset);
        }

        public static bool ValidateProperties(
            string kind,
            JObject properties,
            int startFrame,
            int endFrame,
            Action<string, string> report)
        {
            Type clipType = TimelineAuthoringTypeCatalog.RequireClipType(kind);
            var descriptors = clipType
                .GetCustomAttributes(typeof(TimelineAuthoringPropertyAttribute), true)
                .OfType<TimelineAuthoringPropertyAttribute>()
                .GroupBy(value => value.PropertyId, StringComparer.Ordinal)
                .Select(value => value.First())
                .ToArray();
            var fields = descriptors.ToDictionary(value => value.PropertyId, StringComparer.Ordinal);
            bool valid = true;
            foreach (JProperty property in properties?.Properties() ?? Enumerable.Empty<JProperty>())
            {
                if (!fields.TryGetValue(property.Name, out TimelineAuthoringPropertyAttribute descriptor))
                {
                    report(property.Name, "Timeline Clip property未在正式Clip metadata中声明。");
                    valid = false;
                    continue;
                }
                if (!ValidateValue(descriptor, property.Value))
                {
                    report(property.Name, "Timeline Clip property类型或约束不符合正式metadata。");
                    valid = false;
                }
            }
            foreach (TimelineAuthoringPropertyAttribute descriptor in descriptors)
            {
                if (descriptor.Optional || HasValue(properties?[descriptor.PropertyId]))
                    continue;
                report(descriptor.PropertyId, "Timeline Clip必须声明该正式属性。");
                valid = false;
            }
            if (fields.ContainsKey("curveEndFrame") &&
                properties?.Value<int?>("curveEndFrame") is int curveEnd &&
                (curveEnd <= startFrame || curveEnd > endFrame))
            {
                report("curveEndFrame", "MotionCurve curveEndFrame必须位于clip范围内。");
                valid = false;
            }
            return valid;
        }

        public static void Apply(
            TimelineData timeline,
            Clip clip,
            JObject properties,
            JToken curves,
            ITimelineAuthoringClipResolver resolver)
        {
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            if (clip == null)
                throw new ArgumentNullException(nameof(clip));
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));
            JObject value = properties ?? new JObject();
            if (clip is AnimationClip animation)
            {
                animation.ExtraPolationMode = Enum.Parse<ExtraPolationMode>(
                    value.Value<string>("extraPolationMode"), false);
                animation.BlendProfileId = value.Value<string>("blendProfileId");
            }
            if (clip is MotionCurveClip motion)
            {
                motion.CurveId = value.Value<string>("curveId") ?? motion.CurveId;
                motion.CurveEndFrame = value.Value<int?>("curveEndFrame") ?? motion.CurveEndFrame;
                motion.Space = Enum.Parse<TimelineMotionContributionSpace>(
                    value.Value<string>("space") ?? motion.Space.ToString(), false);
                motion.Channel = Enum.Parse<TimelineMotionChannel>(
                    value.Value<string>("channel") ?? motion.Channel.ToString(), false);
                motion.BlendMode = Enum.Parse<TimelineMotionBlendMode>(
                    value.Value<string>("blendMode") ?? motion.BlendMode.ToString(), false);
                motion.Priority = value.Value<int?>("priority") ?? motion.Priority;
                motion.ConsumeLowerChannels = value.Value<bool?>("consumeLowerChannels") ?? motion.ConsumeLowerChannels;
            }
            if (clip is MotionWarpClip warp)
            {
                warp.ConfigureAuthoring(
                    Enum.Parse<MotionWarpTranslationMode>(
                        value.Value<string>("translationMode") ?? warp.TranslationMode.ToString(), false),
                    Enum.Parse<MotionWarpTargetOffsetSpace>(
                        value.Value<string>("targetOffsetSpace") ?? warp.TargetOffsetSpace.ToString(), false),
                    Enum.Parse<MotionWarpRotationMode>(
                        value.Value<string>("rotationMode") ?? warp.RotationMode.ToString(), false),
                    Enum.Parse<MotionWarpRotationMethod>(
                        value.Value<string>("rotationMethod") ?? warp.RotationMethod.ToString(), false),
                    value["targetPlanarOffset"] == null
                        ? warp.TargetPlanarOffset
                        : new Vector2(
                            value["targetPlanarOffset"].Value<float>("x"),
                            value["targetPlanarOffset"].Value<float>("y")),
                    value.Value<float?>("targetYawOffsetDegrees") ?? warp.TargetYawOffsetDegrees,
                    value.Value<float?>("maxTotalPositionCorrection") ?? warp.MaxTotalPositionCorrection,
                    value.Value<float?>("maxTotalYawCorrectionDegrees") ?? warp.MaxTotalYawCorrectionDegrees,
                    value.Value<float?>("maximumYawRateDegreesPerSecond") ?? warp.MaximumYawRateDegreesPerSecond,
                    Enum.Parse<MotionWarpLimitPolicy>(
                        value.Value<string>("limitPolicy") ?? warp.LimitPolicy.ToString(), false),
                    warp.PositionProgressCurve,
                    warp.YawProgressCurve);
                string sourceId = value.Value<string>("sourceMotionClipId");
                if (!string.IsNullOrEmpty(sourceId) &&
                    resolver.TryResolveMotionClip(timeline, sourceId, out MotionCurveClip source))
                    MotionWarpAuthoring.BindSource(timeline, warp, source);
            }
            if (clip is ActionCueClip actionCue)
            {
                actionCue.CueId = value.Value<string>("cueId") ?? actionCue.CueId;
                actionCue.CueType = value.Value<string>("cueType") ?? actionCue.CueType;
            }
            if (clip is CameraStateClip cameraState)
            {
                cameraState.Mode = Enum.Parse<TimelineCameraMode>(
                    value.Value<string>("mode") ?? cameraState.Mode.ToString(), false);
                cameraState.Priority = value.Value<int?>("priority") ?? cameraState.Priority;
                cameraState.BlendInSeconds = value.Value<float?>("blendInSeconds") ?? cameraState.BlendInSeconds;
                cameraState.BlendOutSeconds = value.Value<float?>("blendOutSeconds") ?? cameraState.BlendOutSeconds;
                cameraState.TargetKey = value.Value<string>("targetKey") ?? cameraState.TargetKey;
                cameraState.InterruptPolicy = Enum.Parse<TimelineCameraInterruptPolicy>(
                    value.Value<string>("interruptPolicy") ?? cameraState.InterruptPolicy.ToString(), false);
            }
            if (clip is CameraCueClip cameraCue)
            {
                cameraCue.CueId = value.Value<string>("cueId") ?? cameraCue.CueId;
                cameraCue.CueKind = Enum.Parse<TimelineCameraCueKind>(
                    value.Value<string>("cueKind") ?? cameraCue.CueKind.ToString(), false);
                cameraCue.CueType = value.Value<string>("cueType") ?? cameraCue.CueType;
                cameraCue.Intensity = value.Value<float?>("intensity") ?? cameraCue.Intensity;
                cameraCue.DurationSeconds = value.Value<float?>("durationSeconds") ?? cameraCue.DurationSeconds;
                cameraCue.Priority = value.Value<int?>("priority") ?? cameraCue.Priority;
            }
            if (clip is CameraResponseClip cameraResponse)
            {
                cameraResponse.LookResponse = Enum.Parse<TimelineCameraLookResponseMode>(
                    value.Value<string>("lookResponse") ?? cameraResponse.LookResponse.ToString(), false);
                cameraResponse.ManualOrbitWeight = value.Value<float?>("manualOrbitWeight") ?? cameraResponse.ManualOrbitWeight;
                cameraResponse.PitchResponseWeight = value.Value<float?>("pitchResponseWeight") ?? cameraResponse.PitchResponseWeight;
                cameraResponse.YawResponseWeight = value.Value<float?>("yawResponseWeight") ?? cameraResponse.YawResponseWeight;
                cameraResponse.Priority = value.Value<int?>("priority") ?? cameraResponse.Priority;
            }
            if (clip is ScenePresentationParameterCurveClip sceneParameter)
                sceneParameter.ConfigureBindings(
                    value.Value<string>("targetBindingId"),
                    value.Value<string>("parameterBindingId"),
                    ToCurve(value["valueCurve"]));
            foreach (JObject curve in curves?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                TimelineCurveChannelCatalog.Require(curve.Value<string>("channelId"))
                    .Replace(clip, ToCurve(curve));
        }

        static AnimationCurve ToCurve(JToken value)
        {
            JObject curveValue = value as JObject ?? throw new InvalidOperationException("Timeline curve payload缺失。");
            var keys = new List<Keyframe>();
            foreach (JObject key in curveValue["keys"]?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
            {
                keys.Add(new Keyframe(
                    key.Value<float>("time"),
                    key.Value<float>("value"),
                    key.Value<float>("inTangent"),
                    key.Value<float>("outTangent"),
                    key.Value<float>("inWeight"),
                    key.Value<float>("outWeight"))
                {
                    weightedMode = Enum.Parse<WeightedMode>(
                        key.Value<string>("weightedMode") ?? WeightedMode.None.ToString(), false)
                });
            }
            return new AnimationCurve(keys.ToArray())
            {
                preWrapMode = Enum.Parse<WrapMode>(curveValue.Value<string>("preWrapMode"), false),
                postWrapMode = Enum.Parse<WrapMode>(curveValue.Value<string>("postWrapMode"), false)
            };
        }

        static JObject CurveToken(AnimationCurve curve)
        {
            return new JObject
            {
                ["preWrapMode"] = curve.preWrapMode.ToString(),
                ["postWrapMode"] = curve.postWrapMode.ToString(),
                ["keys"] = new JArray(curve.keys.Select(value => new JObject
                {
                    ["time"] = value.time,
                    ["value"] = value.value,
                    ["inTangent"] = value.inTangent,
                    ["outTangent"] = value.outTangent,
                    ["inWeight"] = value.inWeight,
                    ["outWeight"] = value.outWeight,
                    ["weightedMode"] = value.weightedMode.ToString()
                }))
            };
        }

        static bool ValidateValue(
            TimelineAuthoringPropertyAttribute descriptor,
            JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return descriptor.Optional;
            bool valid = descriptor.Kind switch
            {
                TimelineAuthoringPropertyKind.Text or TimelineAuthoringPropertyKind.Enum =>
                    token.Type == JTokenType.String,
                TimelineAuthoringPropertyKind.Boolean => token.Type == JTokenType.Boolean,
                TimelineAuthoringPropertyKind.Integer => token.Type == JTokenType.Integer,
                TimelineAuthoringPropertyKind.Float =>
                    token.Type == JTokenType.Integer || token.Type == JTokenType.Float,
                TimelineAuthoringPropertyKind.Vector2 => Vector(token),
                TimelineAuthoringPropertyKind.Object => token.Type == JTokenType.Object,
                _ => false
            };
            if (!valid)
                return false;
            if (descriptor.Trimmed &&
                token.Type == JTokenType.String &&
                token.Value<string>() != token.Value<string>()?.Trim())
                return false;
            if (descriptor.EnumType != null &&
                (!descriptor.EnumType.IsEnum ||
                 !Enum.IsDefined(descriptor.EnumType, token.Value<string>())))
                return false;
            if (descriptor.Kind == TimelineAuthoringPropertyKind.Float ||
                descriptor.Kind == TimelineAuthoringPropertyKind.Integer)
            {
                double value = token.Value<double>();
                if (descriptor.Finite && (double.IsNaN(value) || double.IsInfinity(value)))
                    return false;
                if (descriptor.HasMinimum && value < descriptor.Minimum ||
                    descriptor.HasMaximum && value > descriptor.Maximum)
                    return false;
            }
            return true;
        }

        static bool HasValue(JToken token) =>
            token != null &&
            token.Type != JTokenType.Null &&
            (token.Type != JTokenType.String || !string.IsNullOrWhiteSpace(token.Value<string>()));

        static bool Vector(JToken token)
        {
            if (token is not JObject value ||
                !value.Properties().Select(property => property.Name)
                    .ToHashSet(StringComparer.Ordinal)
                    .SetEquals(new[] { "x", "y" }))
                return false;
            return value["x"]?.Type is JTokenType.Integer or JTokenType.Float &&
                   value["y"]?.Type is JTokenType.Integer or JTokenType.Float;
        }
    }
}
#endif
