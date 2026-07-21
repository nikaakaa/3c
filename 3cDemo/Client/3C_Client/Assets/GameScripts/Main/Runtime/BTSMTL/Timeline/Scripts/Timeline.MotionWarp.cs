using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public enum MotionWarpTranslationMode : byte
    {
        Disabled = 0,
        ScaleToTarget = 1,
        SkewToTarget = 2,
        LinearToTarget = 3
    }

    public enum MotionWarpTargetOffsetSpace : byte
    {
        TargetLocal = 0,
        ApproachDirection = 1,
        ActorStartLocal = 2,
        World = 3
    }

    public enum MotionWarpRotationMode : byte
    {
        Disabled = 0,
        FaceTarget = 1,
        MatchTargetYaw = 2
    }

    public enum MotionWarpRotationMethod : byte
    {
        ProgressCurve = 0,
        ConstantRate = 1,
        ScaleSourceYaw = 2
    }

    public enum MotionWarpLimitPolicy : byte
    {
        ApplyClamped = 0,
        PreserveSource = 1
    }

    [TrackGroup("Base"), ScriptGuid("79b8da4acfeb4d1994d019eacf6d5de3"), Ordered(1), Color(248, 177, 91)]
    public sealed class MotionWarpTrack : Track
    {
#if UNITY_EDITOR
        public override Type ClipType => typeof(MotionWarpClip);
#endif
    }

    [ScriptGuid("79b8da4acfeb4d1994d019eacf6d5de3"), ClipInspectorView("MotionWarpClipInspectorView"), Color(248, 177, 91)]
    public sealed class MotionWarpClip : Clip
    {
        [SerializeField, ShowInInspector, ReadOnly]
        string m_SourceMotionClipId;

        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public MotionWarpTranslationMode TranslationMode = MotionWarpTranslationMode.SkewToTarget;

        [ShowInInspector, ShowIf(nameof(HasPositionWarp)), OnValueChanged("RebindTimeline")]
        public MotionWarpTargetOffsetSpace TargetOffsetSpace = MotionWarpTargetOffsetSpace.ApproachDirection;

        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public MotionWarpRotationMode RotationMode = MotionWarpRotationMode.FaceTarget;

        [ShowInInspector, ShowIf(nameof(HasYawWarp)), OnValueChanged("RebindTimeline")]
        public MotionWarpRotationMethod RotationMethod = MotionWarpRotationMethod.ProgressCurve;

        [ShowInInspector, ShowIf(nameof(HasPositionWarp)), OnValueChanged("RebindTimeline")]
        public Vector2 TargetPlanarOffset;

        [ShowInInspector, ShowIf(nameof(HasYawWarp)), OnValueChanged("RebindTimeline")]
        public float TargetYawOffsetDegrees;

        [ShowInInspector, ShowIf(nameof(HasPositionWarp)), OnValueChanged("RebindTimeline")]
        public float MaxTotalPositionCorrection = 1f;

        [ShowInInspector, ShowIf(nameof(HasYawWarp)), OnValueChanged("RebindTimeline")]
        public float MaxTotalYawCorrectionDegrees = 45f;

        [ShowInInspector, ShowIf(nameof(UsesMaximumYawRate)), OnValueChanged("RebindTimeline")]
        public float MaximumYawRateDegreesPerSecond = 360f;

        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public MotionWarpLimitPolicy LimitPolicy = MotionWarpLimitPolicy.ApplyClamped;

        [ShowInInspector, ShowIf(nameof(UsesPositionProgress)), OnValueChanged("RebindTimeline")]
        public AnimationCurve PositionProgressCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [ShowInInspector, ShowIf(nameof(UsesYawProgress)), OnValueChanged("RebindTimeline")]
        public AnimationCurve YawProgressCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public string SourceMotionClipId => m_SourceMotionClipId ?? string.Empty;
        public bool HasPositionWarp => TranslationMode != MotionWarpTranslationMode.Disabled;
        public bool HasYawWarp => RotationMode != MotionWarpRotationMode.Disabled;
        public bool UsesPositionProgress => TranslationMode == MotionWarpTranslationMode.SkewToTarget || TranslationMode == MotionWarpTranslationMode.LinearToTarget;
        public bool UsesYawProgress => HasYawWarp && RotationMethod == MotionWarpRotationMethod.ProgressCurve;
        public bool UsesMaximumYawRate => HasYawWarp && RotationMethod == MotionWarpRotationMethod.ConstantRate;

#if UNITY_EDITOR
        public override string Name
        {
            get
            {
                if (MotionWarpAuthoring.TryResolveSource(Timeline, SourceMotionClipId, out MotionCurveClip source))
                    return $"Motion Warp -> {source.CurveId}";
                return string.IsNullOrEmpty(SourceMotionClipId) ? "Motion Warp" : "Motion Warp -> Missing Source";
            }
        }

        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable;

        public MotionWarpClip(Track track, int frame) : base(track, frame)
        {
        }

        public void ConfigureAuthoring(
            MotionWarpTranslationMode translationMode,
            MotionWarpTargetOffsetSpace targetOffsetSpace,
            MotionWarpRotationMode rotationMode,
            MotionWarpRotationMethod rotationMethod,
            Vector2 targetPlanarOffset,
            float targetYawOffsetDegrees,
            float maxTotalPositionCorrection,
            float maxTotalYawCorrectionDegrees,
            float maximumYawRateDegreesPerSecond,
            MotionWarpLimitPolicy limitPolicy,
            AnimationCurve positionProgressCurve,
            AnimationCurve yawProgressCurve)
        {
            TranslationMode = translationMode;
            TargetOffsetSpace = targetOffsetSpace;
            RotationMode = rotationMode;
            RotationMethod = rotationMethod;
            TargetPlanarOffset = targetPlanarOffset;
            TargetYawOffsetDegrees = targetYawOffsetDegrees;
            MaxTotalPositionCorrection = maxTotalPositionCorrection;
            MaxTotalYawCorrectionDegrees = maxTotalYawCorrectionDegrees;
            MaximumYawRateDegreesPerSecond = maximumYawRateDegreesPerSecond;
            LimitPolicy = limitPolicy;
            PositionProgressCurve = CloneCurve(positionProgressCurve);
            YawProgressCurve = CloneCurve(yawProgressCurve);
            RebindTimeline();
        }

        internal void SetSourceMotionClipId(string sourceMotionClipId)
        {
            m_SourceMotionClipId = sourceMotionClipId ?? string.Empty;
            RebindTimeline();
        }

        static AnimationCurve CloneCurve(AnimationCurve curve)
        {
            if (curve == null)
                return null;
            return new AnimationCurve(curve.keys)
            {
                preWrapMode = curve.preWrapMode,
                postWrapMode = curve.postWrapMode
            };
        }
#endif
    }

    public readonly struct MotionWarpAuthoringIssue
    {
        public MotionWarpAuthoringIssue(string code, string message, MotionWarpClip clip, MotionCurveClip source)
        {
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            Clip = clip;
            Source = source;
        }

        public string Code { get; }
        public string Message { get; }
        public MotionWarpClip Clip { get; }
        public MotionCurveClip Source { get; }
    }

    public static class MotionWarpAuthoring
    {
        const float Epsilon = 0.0001f;
        const int CurveValidationSegments = 256;

        public static void CollectMotionCurveSources(TimelineData timeline, List<MotionCurveClip> output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            output.Clear();
            if (timeline == null)
                return;
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    if (track.Clips[clipIndex] is MotionCurveClip source)
                        output.Add(source);
                }
            }
        }

        public static bool TryResolveSource(TimelineData timeline, string sourceMotionClipId, out MotionCurveClip source)
        {
            source = null;
            if (!TryResolveClip(timeline, sourceMotionClipId, out Clip clip))
                return false;
            source = clip as MotionCurveClip;
            return source != null;
        }

        public static bool TryResolveClip(TimelineData timeline, string authoringId, out Clip clip)
        {
            clip = null;
            if (timeline == null || !AuthoringIdentity.IsValid(authoringId))
                return false;
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip candidate = track.Clips[clipIndex];
                    if (candidate != null && string.Equals(candidate.AuthoringId, authoringId, StringComparison.Ordinal))
                    {
                        clip = candidate;
                        return true;
                    }
                }
            }
            return false;
        }

#if UNITY_EDITOR
        public static void BindSource(TimelineData timeline, MotionWarpClip warp, MotionCurveClip source)
        {
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            if (warp == null)
                throw new ArgumentNullException(nameof(warp));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (!ContainsClip(timeline, warp) || !ContainsClip(timeline, source))
                throw new InvalidOperationException("MotionWarp source and destination must belong to the same Timeline.");
            if (!AuthoringIdentity.IsValid(source.AuthoringId))
                throw new InvalidOperationException("MotionCurve source requires a stable authoring identity.");
            warp.SetSourceMotionClipId(source.AuthoringId);
        }

        public static void ClearSource(TimelineData timeline, MotionWarpClip warp)
        {
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            if (warp == null || !ContainsClip(timeline, warp))
                throw new InvalidOperationException("MotionWarp clip does not belong to the Timeline.");
            warp.SetSourceMotionClipId(string.Empty);
        }
#endif

        public static bool Validate(TimelineData timeline, List<MotionWarpAuthoringIssue> issues)
        {
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            bool valid = true;
            var warps = new List<MotionWarpClip>();
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    if (track.Clips[clipIndex] is MotionWarpClip warp)
                        warps.Add(warp);
                }
            }
            for (int i = 0; i < warps.Count; i++)
                valid &= ValidateClip(timeline, warps[i], issues);
            for (int i = 0; i < warps.Count; i++)
            {
                MotionWarpClip left = warps[i];
                if (string.IsNullOrEmpty(left.SourceMotionClipId))
                    continue;
                for (int j = i + 1; j < warps.Count; j++)
                {
                    MotionWarpClip right = warps[j];
                    if (!string.Equals(left.SourceMotionClipId, right.SourceMotionClipId, StringComparison.Ordinal) ||
                        left.EndFrame <= right.StartFrame || right.EndFrame <= left.StartFrame)
                        continue;
                    Add(issues, "motion_warp_window_overlap", $"MotionWarp windows '{left.AuthoringId}' and '{right.AuthoringId}' overlap on source '{left.SourceMotionClipId}'.", left, null);
                    valid = false;
                }
            }
            return valid;
        }

        public static bool ValidateClip(TimelineData timeline, MotionWarpClip warp, List<MotionWarpAuthoringIssue> issues)
        {
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            if (warp == null)
                throw new ArgumentNullException(nameof(warp));
            bool valid = true;
            if (!ContainsClip(timeline, warp))
            {
                Add(issues, "motion_warp_owner_invalid", "MotionWarp clip does not belong to the validated Timeline.", warp, null);
                return false;
            }
            MotionCurveClip source = null;
            if (string.IsNullOrEmpty(warp.SourceMotionClipId))
            {
                Add(issues, "motion_warp_source_missing", $"MotionWarp '{warp.AuthoringId}' has no source MotionCurve clip.", warp, null);
                valid = false;
            }
            else if (!TryResolveClip(timeline, warp.SourceMotionClipId, out Clip referenced))
            {
                Add(issues, "motion_warp_source_dangling", $"MotionWarp '{warp.AuthoringId}' references missing MotionCurve '{warp.SourceMotionClipId}'.", warp, null);
                valid = false;
            }
            else if (referenced is not MotionCurveClip motionSource)
            {
                Add(issues, "motion_warp_source_type_invalid", $"MotionWarp '{warp.AuthoringId}' source '{warp.SourceMotionClipId}' is not a MotionCurve clip.", warp, null);
                valid = false;
            }
            else
            {
                source = motionSource;
            }
            if (source != null)
            {
                if (source.Channel != TimelineMotionChannel.Action)
                {
                    Add(issues, "motion_warp_source_channel_invalid", $"MotionWarp source '{source.AuthoringId}' must use the Action channel.", warp, source);
                    valid = false;
                }
                if (source.BlendMode != TimelineMotionBlendMode.Override)
                {
                    Add(issues, "motion_warp_source_blend_invalid", $"MotionWarp source '{source.AuthoringId}' must use Override blend mode.", warp, source);
                    valid = false;
                }
                if (source.Space != TimelineMotionContributionSpace.Local)
                {
                    Add(issues, "motion_warp_source_space_invalid", $"MotionWarp source '{source.AuthoringId}' must use ActorLocal contribution space.", warp, source);
                    valid = false;
                }
                if (!HasUnitGameplayWeight(source))
                {
                    Add(issues, "motion_warp_source_weight_invalid", $"MotionWarp source '{source.AuthoringId}' must use unit Gameplay weight with no ease. Animation blending belongs to Presentation.", warp, source);
                    valid = false;
                }
                if (warp.StartFrame < source.StartFrame || warp.EndFrame > source.CurveEndFrame)
                {
                    Add(issues, "motion_warp_window_outside_source", $"MotionWarp '{warp.AuthoringId}' must stay inside source frames {source.StartFrame}..{source.CurveEndFrame}.", warp, source);
                    valid = false;
                }
            }
            if (warp.StartFrame >= warp.EndFrame)
            {
                Add(issues, "motion_warp_window_invalid", $"MotionWarp '{warp.AuthoringId}' requires StartFrame < EndFrame.", warp, source);
                valid = false;
            }
            valid &= ValidateConfiguration(
                warp.TranslationMode,
                warp.TargetOffsetSpace,
                warp.RotationMode,
                warp.RotationMethod,
                warp.TargetPlanarOffset,
                warp.TargetYawOffsetDegrees,
                warp.MaxTotalPositionCorrection,
                warp.MaxTotalYawCorrectionDegrees,
                warp.MaximumYawRateDegreesPerSecond,
                warp.LimitPolicy,
                warp.PositionProgressCurve,
                warp.YawProgressCurve,
                issues,
                warp,
                source);
            return valid;
        }

        public static bool ValidateConfiguration(
            MotionWarpTranslationMode translationMode,
            MotionWarpTargetOffsetSpace targetOffsetSpace,
            MotionWarpRotationMode rotationMode,
            MotionWarpRotationMethod rotationMethod,
            Vector2 targetPlanarOffset,
            float targetYawOffsetDegrees,
            float maxTotalPositionCorrection,
            float maxTotalYawCorrectionDegrees,
            float maximumYawRateDegreesPerSecond,
            MotionWarpLimitPolicy limitPolicy,
            AnimationCurve positionProgressCurve,
            AnimationCurve yawProgressCurve,
            List<MotionWarpAuthoringIssue> issues,
            MotionWarpClip warp = null,
            MotionCurveClip source = null)
        {
            string identity = warp?.AuthoringId ?? "pending";
            bool valid = true;
            bool translationDefined = Enum.IsDefined(typeof(MotionWarpTranslationMode), translationMode);
            bool offsetSpaceDefined = Enum.IsDefined(typeof(MotionWarpTargetOffsetSpace), targetOffsetSpace);
            bool rotationDefined = Enum.IsDefined(typeof(MotionWarpRotationMode), rotationMode);
            bool rotationMethodDefined = Enum.IsDefined(typeof(MotionWarpRotationMethod), rotationMethod);
            bool limitPolicyDefined = Enum.IsDefined(typeof(MotionWarpLimitPolicy), limitPolicy);
            if (!translationDefined || !offsetSpaceDefined || !rotationDefined || !rotationMethodDefined || !limitPolicyDefined)
            {
                Add(issues, "motion_warp_mode_invalid", $"MotionWarp '{identity}' contains an unknown mode.", warp, source);
                valid = false;
            }
            else if (translationMode == MotionWarpTranslationMode.Disabled && rotationMode == MotionWarpRotationMode.Disabled)
            {
                Add(issues, "motion_warp_mode_disabled", $"MotionWarp '{identity}' has both position and rotation disabled.", warp, source);
                valid = false;
            }
            if (translationDefined && translationMode != MotionWarpTranslationMode.Disabled &&
                (!Finite(targetPlanarOffset.x) || !Finite(targetPlanarOffset.y)))
            {
                Add(issues, "motion_warp_target_offset_invalid", $"MotionWarp '{identity}' target planar offset must be finite.", warp, source);
                valid = false;
            }
            if (rotationDefined && rotationMode != MotionWarpRotationMode.Disabled && !Finite(targetYawOffsetDegrees))
            {
                Add(issues, "motion_warp_target_yaw_invalid", $"MotionWarp '{identity}' target yaw offset must be finite.", warp, source);
                valid = false;
            }
            if (translationDefined && translationMode != MotionWarpTranslationMode.Disabled &&
                (!Finite(maxTotalPositionCorrection) || maxTotalPositionCorrection < 0f))
            {
                Add(issues, "motion_warp_position_clamp_invalid", $"MotionWarp '{identity}' position correction limit must be finite and non-negative.", warp, source);
                valid = false;
            }
            if (rotationDefined && rotationMode != MotionWarpRotationMode.Disabled &&
                (!Finite(maxTotalYawCorrectionDegrees) || maxTotalYawCorrectionDegrees < 0f || maxTotalYawCorrectionDegrees > 180f))
            {
                Add(issues, "motion_warp_yaw_clamp_invalid", $"MotionWarp '{identity}' yaw correction limit must be in [0,180].", warp, source);
                valid = false;
            }
            if (rotationDefined && rotationMode != MotionWarpRotationMode.Disabled &&
                rotationMethodDefined && rotationMethod == MotionWarpRotationMethod.ConstantRate &&
                (!Finite(maximumYawRateDegreesPerSecond) || maximumYawRateDegreesPerSecond <= 0f))
            {
                Add(issues, "motion_warp_yaw_rate_invalid", $"MotionWarp '{identity}' maximum yaw rate must be finite and positive.", warp, source);
                valid = false;
            }
            if (translationDefined && (translationMode == MotionWarpTranslationMode.SkewToTarget || translationMode == MotionWarpTranslationMode.LinearToTarget))
                valid &= ValidateProgressCurve(positionProgressCurve, "position", warp, source, issues);
            if (rotationDefined && rotationMode != MotionWarpRotationMode.Disabled &&
                rotationMethodDefined && rotationMethod == MotionWarpRotationMethod.ProgressCurve)
                valid &= ValidateProgressCurve(yawProgressCurve, "yaw", warp, source, issues);
            if (warp != null && source != null && translationDefined && translationMode == MotionWarpTranslationMode.ScaleToTarget &&
                SourceWindowPlanarMagnitude(source, warp) <= Epsilon)
            {
                Add(issues, "motion_warp_scale_source_zero", $"MotionWarp '{identity}' ScaleToTarget requires a non-zero source window planar endpoint.", warp, source);
                valid = false;
            }
            if (warp != null && source != null && rotationDefined && rotationMode != MotionWarpRotationMode.Disabled &&
                rotationMethodDefined && rotationMethod == MotionWarpRotationMethod.ScaleSourceYaw &&
                Mathf.Abs(SourceWindowYaw(source, warp)) <= Epsilon)
            {
                Add(issues, "motion_warp_scale_yaw_source_zero", $"MotionWarp '{identity}' ScaleSourceYaw requires non-zero source window yaw.", warp, source);
                valid = false;
            }
            return valid;
        }

        static bool ValidateProgressCurve(
            AnimationCurve curve,
            string name,
            MotionWarpClip warp,
            MotionCurveClip source,
            List<MotionWarpAuthoringIssue> issues)
        {
            if (curve == null || curve.length < 2)
            {
                Add(issues, "motion_warp_progress_curve_missing", $"MotionWarp '{warp?.AuthoringId ?? "pending"}' {name} progress curve requires at least two keys.", warp, source);
                return false;
            }
            bool valid = true;
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                if (!Finite(key.time) || !Finite(key.value) || !Finite(key.inTangent) || !Finite(key.outTangent) ||
                    !Finite(key.inWeight) || !Finite(key.outWeight) || key.time < -Epsilon || key.time > 1f + Epsilon)
                {
                    Add(issues, "motion_warp_progress_curve_key_invalid", $"MotionWarp '{warp?.AuthoringId ?? "pending"}' {name} progress key #{i} is not finite or outside [0,1].", warp, source);
                    valid = false;
                }
            }
            if (Mathf.Abs(keys[0].time) > Epsilon || Mathf.Abs(keys[0].value) > Epsilon ||
                Mathf.Abs(keys[keys.Length - 1].time - 1f) > Epsilon || Mathf.Abs(keys[keys.Length - 1].value - 1f) > Epsilon)
            {
                Add(issues, "motion_warp_progress_curve_endpoints_invalid", $"MotionWarp '{warp?.AuthoringId ?? "pending"}' {name} progress curve must start at (0,0) and end at (1,1).", warp, source);
                valid = false;
            }
            float previous = curve.Evaluate(0f);
            for (int i = 1; i <= CurveValidationSegments; i++)
            {
                float value = curve.Evaluate(i / (float)CurveValidationSegments);
                if (!Finite(value) || value < -Epsilon || value > 1f + Epsilon || value + Epsilon < previous)
                {
                    Add(issues, "motion_warp_progress_curve_not_monotonic", $"MotionWarp '{warp?.AuthoringId ?? "pending"}' {name} progress curve must remain finite and monotonic in [0,1].", warp, source);
                    valid = false;
                    break;
                }
                previous = value;
            }
            return valid;
        }

        static bool ContainsClip(TimelineData timeline, Clip clip)
        {
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track != null && track.Clips.Contains(clip))
                    return true;
            }
            return false;
        }

        static float SourceWindowPlanarMagnitude(MotionCurveClip source, MotionWarpClip warp)
        {
            Vector2 start = SourcePosition(source, warp.StartFrame);
            Vector2 end = SourcePosition(source, warp.EndFrame);
            return (end - start).magnitude;
        }

        static float SourceWindowYaw(MotionCurveClip source, MotionWarpClip warp) =>
            source.Yaw.Evaluate(SourceNormalizedTime(source, warp.EndFrame)) -
            source.Yaw.Evaluate(SourceNormalizedTime(source, warp.StartFrame));

        static Vector2 SourcePosition(MotionCurveClip source, int frame)
        {
            float normalized = SourceNormalizedTime(source, frame);
            return new Vector2(source.PositionX.Evaluate(normalized), source.PositionZ.Evaluate(normalized));
        }

        static float SourceNormalizedTime(MotionCurveClip source, int frame)
        {
            int duration = source.CurveEndFrame - source.StartFrame;
            return duration <= 0 ? 0f : Mathf.Clamp01((frame - source.StartFrame) / (float)duration);
        }

        static bool HasUnitGameplayWeight(MotionCurveClip source)
        {
            if (source.EaseInFrame != 0 || source.EaseOutFrame != 0 || source.WeightCurve == null || source.WeightCurve.length == 0)
                return false;
            Keyframe[] keys = source.WeightCurve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                if (!Finite(keys[i].value) || Mathf.Abs(keys[i].value - 1f) > Epsilon ||
                    !Finite(keys[i].inTangent) || Mathf.Abs(keys[i].inTangent) > Epsilon ||
                    !Finite(keys[i].outTangent) || Mathf.Abs(keys[i].outTangent) > Epsilon ||
                    keys[i].weightedMode != WeightedMode.None)
                    return false;
            }
            return true;
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        static void Add(
            List<MotionWarpAuthoringIssue> issues,
            string code,
            string message,
            MotionWarpClip warp,
            MotionCurveClip source)
        {
            issues?.Add(new MotionWarpAuthoringIssue(code, message, warp, source));
        }
    }
}
