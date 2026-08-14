#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public enum TimelineAuthoringContentKind : byte
    {
        SpanClip,
        PointMarker,
        ContinuousCurve
    }

    public enum TimelineCurveTimeDomain : byte
    {
        ClipNormalized
    }

    public readonly struct TimelineCurveChannelId : IEquatable<TimelineCurveChannelId>
    {
        public TimelineCurveChannelId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value != value.Trim())
                throw new ArgumentException("Timeline curve channel id must be non-empty and trimmed.", nameof(value));
            Value = value;
        }

        public string Value { get; }
        public bool Equals(TimelineCurveChannelId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is TimelineCurveChannelId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(TimelineCurveChannelId left, TimelineCurveChannelId right) => left.Equals(right);
        public static bool operator !=(TimelineCurveChannelId left, TimelineCurveChannelId right) => !left.Equals(right);
    }

    public readonly struct TimelineCurveValueDomain
    {
        TimelineCurveValueDomain(bool bounded, float minimum, float maximum, float zero, string unit)
        {
            IsBounded = bounded;
            Minimum = minimum;
            Maximum = maximum;
            Zero = zero;
            Unit = unit ?? string.Empty;
        }

        public bool IsBounded { get; }
        public float Minimum { get; }
        public float Maximum { get; }
        public float Zero { get; }
        public string Unit { get; }
        public string Summary => IsBounded
            ? $"{Minimum:0.###} - {Maximum:0.###}{UnitSuffix()}"
            : $"Auto{UnitSuffix()}";

        public static TimelineCurveValueDomain Bounded(float minimum, float maximum, string unit = "")
        {
            if (!TimelineCurveAuthoring.IsFinite(minimum) || !TimelineCurveAuthoring.IsFinite(maximum) || minimum >= maximum)
                throw new ArgumentException("Bounded Timeline curve domain requires finite minimum < maximum.");
            return new TimelineCurveValueDomain(true, minimum, maximum, Mathf.Clamp(0f, minimum, maximum), unit);
        }

        public static TimelineCurveValueDomain Unbounded(float zero, string unit)
        {
            if (!TimelineCurveAuthoring.IsFinite(zero))
                throw new ArgumentException("Unbounded Timeline curve domain requires a finite zero line.");
            return new TimelineCurveValueDomain(false, 0f, 0f, zero, unit);
        }

        string UnitSuffix() => string.IsNullOrEmpty(Unit) ? string.Empty : $" {Unit}";
    }

    public sealed class TimelineCurveChannelDescriptor
    {
        readonly Func<AnimationCurve> m_DefaultFactory;
        readonly Func<Clip, bool> m_Availability;

        public TimelineCurveChannelDescriptor(
            TimelineCurveChannelId channelId,
            Type ownerType,
            string displayName,
            Color color,
            TimelineCurveValueDomain valueDomain,
            Func<AnimationCurve> defaultFactory,
            Func<Clip, bool> availability = null)
        {
            if (ownerType == null || !typeof(Clip).IsAssignableFrom(ownerType))
                throw new ArgumentException("Timeline curve owner must be a Clip type.", nameof(ownerType));
            ChannelId = channelId;
            OwnerType = ownerType;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? throw new ArgumentException(nameof(displayName)) : displayName;
            Color = color;
            TimeDomain = TimelineCurveTimeDomain.ClipNormalized;
            ValueDomain = valueDomain;
            m_DefaultFactory = defaultFactory ?? throw new ArgumentNullException(nameof(defaultFactory));
            m_Availability = availability;
        }

        public TimelineCurveChannelId ChannelId { get; }
        public Type OwnerType { get; }
        public string DisplayName { get; }
        public Color Color { get; }
        public TimelineCurveTimeDomain TimeDomain { get; }
        public TimelineCurveValueDomain ValueDomain { get; }
        public bool Supports(Clip owner) => owner != null && OwnerType.IsInstanceOfType(owner) && (m_Availability?.Invoke(owner) ?? true);
        public AnimationCurve CreateDefaultCurve() => TimelineCurveAuthoring.CopyCurve(m_DefaultFactory());

        public AnimationCurve Read(Clip owner)
        {
            RequireOwner(owner);
            return TimelineCurveAuthoring.Read(owner, ChannelId);
        }

        public void Replace(Clip owner, AnimationCurve curve)
        {
            RequireOwner(owner);
            TimelineCurveAuthoring.Replace(owner, ChannelId, curve);
        }

        public void Validate(Clip owner, AnimationCurve curve)
        {
            RequireOwner(owner);
            TimelineCurveAuthoring.Validate(owner, ChannelId, curve);
        }

        void RequireOwner(Clip owner)
        {
            if (!Supports(owner))
                throw new InvalidOperationException($"Curve channel '{ChannelId}' does not support owner '{owner?.GetType().FullName ?? "null"}'.");
        }
    }

    public static class TimelineCurveChannelCatalog
    {
        public static readonly TimelineCurveChannelId AnimationWeight = Id("animation.weight");
        public static readonly TimelineCurveChannelId AnimationEaseIn = Id("animation.ease-in");
        public static readonly TimelineCurveChannelId AnimationEaseOut = Id("animation.ease-out");
        public static readonly TimelineCurveChannelId MotionWeight = Id("motion.weight");
        public static readonly TimelineCurveChannelId MotionPositionX = Id("motion.position-x");
        public static readonly TimelineCurveChannelId MotionPositionY = Id("motion.position-y");
        public static readonly TimelineCurveChannelId MotionPositionZ = Id("motion.position-z");
        public static readonly TimelineCurveChannelId MotionYaw = Id("motion.yaw");
        public static readonly TimelineCurveChannelId MotionEaseIn = Id("motion.ease-in");
        public static readonly TimelineCurveChannelId MotionEaseOut = Id("motion.ease-out");
        public static readonly TimelineCurveChannelId MotionWarpPositionProgress = Id("motion-warp.position-progress");
        public static readonly TimelineCurveChannelId MotionWarpYawProgress = Id("motion-warp.yaw-progress");
        public static readonly TimelineCurveChannelId CameraStateWeight = Id("camera-state.weight");
        public static readonly TimelineCurveChannelId CameraStateEaseIn = Id("camera-state.ease-in");
        public static readonly TimelineCurveChannelId CameraStateEaseOut = Id("camera-state.ease-out");
        public static readonly TimelineCurveChannelId CameraResponseWeight = Id("camera-response.weight");
        public static readonly TimelineCurveChannelId CameraResponseEaseIn = Id("camera-response.ease-in");
        public static readonly TimelineCurveChannelId CameraResponseEaseOut = Id("camera-response.ease-out");

        static readonly TimelineCurveValueDomain Unit = TimelineCurveValueDomain.Bounded(0f, 1f);
        static readonly TimelineCurveValueDomain Meters = TimelineCurveValueDomain.Unbounded(0f, "m");
        static readonly TimelineCurveValueDomain Degrees = TimelineCurveValueDomain.Unbounded(0f, "deg");
        static readonly List<TimelineCurveChannelDescriptor> Descriptors = BuildDescriptors();
        static readonly Dictionary<string, TimelineCurveChannelDescriptor> ById = BuildIndex();

        public static IReadOnlyList<TimelineCurveChannelDescriptor> All => Descriptors;

        public static bool TryGet(string channelId, out TimelineCurveChannelDescriptor descriptor) =>
            ById.TryGetValue(channelId ?? string.Empty, out descriptor);

        public static TimelineCurveChannelDescriptor Require(string channelId)
        {
            if (!TryGet(channelId, out TimelineCurveChannelDescriptor descriptor))
                throw new InvalidOperationException($"Unknown Timeline curve channel '{channelId}'.");
            return descriptor;
        }

        public static void CollectForTrack(Track track, List<TimelineCurveChannelDescriptor> output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            output.Clear();
            if (track == null)
                return;
            Type clipType = track.ClipType;
            for (int descriptorIndex = 0; descriptorIndex < Descriptors.Count; descriptorIndex++)
            {
                TimelineCurveChannelDescriptor descriptor = Descriptors[descriptorIndex];
                bool supports = descriptor.OwnerType.IsAssignableFrom(clipType);
                for (int clipIndex = 0; !supports && clipIndex < track.Clips.Count; clipIndex++)
                    supports = descriptor.Supports(track.Clips[clipIndex]);
                if (supports)
                    output.Add(descriptor);
            }
        }

        static List<TimelineCurveChannelDescriptor> BuildDescriptors() => new List<TimelineCurveChannelDescriptor>
        {
            D(AnimationWeight, typeof(AnimationClip), "Weight", C(92, 205, 235), Unit, One),
            D(AnimationEaseIn, typeof(AnimationClip), "Ease In", C(91, 187, 137), Unit, ZeroOne),
            D(AnimationEaseOut, typeof(AnimationClip), "Ease Out", C(226, 165, 79), Unit, ZeroOne),
            D(MotionWeight, typeof(MotionCurveClip), "Weight", C(120, 211, 151), Unit, One),
            D(MotionPositionX, typeof(MotionCurveClip), "Position X", C(235, 100, 91), Meters, Zero),
            D(MotionPositionY, typeof(MotionCurveClip), "Position Y", C(109, 210, 116), Meters, Zero),
            D(MotionPositionZ, typeof(MotionCurveClip), "Position Z", C(94, 157, 235), Meters, Zero),
            D(MotionYaw, typeof(MotionCurveClip), "Yaw", C(230, 199, 93), Degrees, Zero),
            D(MotionEaseIn, typeof(MotionCurveClip), "Ease In", C(93, 189, 141), Unit, ZeroOne),
            D(MotionEaseOut, typeof(MotionCurveClip), "Ease Out", C(226, 165, 79), Unit, ZeroOne),
            D(MotionWarpPositionProgress, typeof(MotionWarpClip), "Position Progress", C(238, 156, 72), Unit, ZeroOne,
                clip => ((MotionWarpClip)clip).UsesPositionProgress),
            D(MotionWarpYawProgress, typeof(MotionWarpClip), "Yaw Progress", C(232, 110, 101), Unit, ZeroOne,
                clip => ((MotionWarpClip)clip).UsesYawProgress),
            D(CameraStateWeight, typeof(CameraStateClip), "Weight", C(184, 161, 252), Unit, One),
            D(CameraStateEaseIn, typeof(CameraStateClip), "Ease In", C(102, 191, 153), Unit, ZeroOne),
            D(CameraStateEaseOut, typeof(CameraStateClip), "Ease Out", C(226, 165, 79), Unit, ZeroOne),
            D(CameraResponseWeight, typeof(CameraResponseClip), "Weight", C(110, 201, 240), Unit, One),
            D(CameraResponseEaseIn, typeof(CameraResponseClip), "Ease In", C(102, 191, 153), Unit, ZeroOne),
            D(CameraResponseEaseOut, typeof(CameraResponseClip), "Ease Out", C(226, 165, 79), Unit, ZeroOne)
        };

        static Dictionary<string, TimelineCurveChannelDescriptor> BuildIndex()
        {
            var result = new Dictionary<string, TimelineCurveChannelDescriptor>(StringComparer.Ordinal);
            for (int i = 0; i < Descriptors.Count; i++)
            {
                TimelineCurveChannelDescriptor descriptor = Descriptors[i];
                if (!result.TryAdd(descriptor.ChannelId.Value, descriptor))
                    throw new InvalidOperationException($"Duplicate Timeline curve channel '{descriptor.ChannelId}'.");
            }
            return result;
        }

        static TimelineCurveChannelDescriptor D(TimelineCurveChannelId id, Type owner, string name, Color color,
            TimelineCurveValueDomain domain, Func<AnimationCurve> factory, Func<Clip, bool> availability = null) =>
            new TimelineCurveChannelDescriptor(id, owner, name, color, domain, factory, availability);
        static TimelineCurveChannelId Id(string value) => new TimelineCurveChannelId(value);
        static Color C(byte r, byte g, byte b) => new Color32(r, g, b, 255);
        static AnimationCurve One() => AnimationCurve.Linear(0f, 1f, 1f, 1f);
        static AnimationCurve Zero() => AnimationCurve.Linear(0f, 0f, 1f, 0f);
        static AnimationCurve ZeroOne() => AnimationCurve.Linear(0f, 0f, 1f, 1f);
    }

    public static class TimelineCurveAuthoring
    {
        public static AnimationCurve Read(Clip owner, TimelineCurveChannelId channelId)
        {
            AnimationCurve curve = owner switch
            {
                AnimationClip clip when channelId == TimelineCurveChannelCatalog.AnimationWeight => clip.WeightCurve,
                AnimationClip clip when channelId == TimelineCurveChannelCatalog.AnimationEaseIn => clip.EaseInCurve,
                AnimationClip clip when channelId == TimelineCurveChannelCatalog.AnimationEaseOut => clip.EaseOutCurve,
                MotionCurveClip clip when channelId == TimelineCurveChannelCatalog.MotionWeight => clip.WeightCurve,
                MotionCurveClip clip when channelId == TimelineCurveChannelCatalog.MotionPositionX => clip.PositionX,
                MotionCurveClip clip when channelId == TimelineCurveChannelCatalog.MotionPositionY => clip.PositionY,
                MotionCurveClip clip when channelId == TimelineCurveChannelCatalog.MotionPositionZ => clip.PositionZ,
                MotionCurveClip clip when channelId == TimelineCurveChannelCatalog.MotionYaw => clip.Yaw,
                MotionCurveClip clip when channelId == TimelineCurveChannelCatalog.MotionEaseIn => clip.EaseInCurve,
                MotionCurveClip clip when channelId == TimelineCurveChannelCatalog.MotionEaseOut => clip.EaseOutCurve,
                MotionWarpClip clip when channelId == TimelineCurveChannelCatalog.MotionWarpPositionProgress && clip.UsesPositionProgress => clip.PositionProgressCurve,
                MotionWarpClip clip when channelId == TimelineCurveChannelCatalog.MotionWarpYawProgress && clip.UsesYawProgress => clip.YawProgressCurve,
                CameraStateClip clip when channelId == TimelineCurveChannelCatalog.CameraStateWeight => clip.WeightCurve,
                CameraStateClip clip when channelId == TimelineCurveChannelCatalog.CameraStateEaseIn => clip.EaseInCurve,
                CameraStateClip clip when channelId == TimelineCurveChannelCatalog.CameraStateEaseOut => clip.EaseOutCurve,
                CameraResponseClip clip when channelId == TimelineCurveChannelCatalog.CameraResponseWeight => clip.WeightCurve,
                CameraResponseClip clip when channelId == TimelineCurveChannelCatalog.CameraResponseEaseIn => clip.EaseInCurve,
                CameraResponseClip clip when channelId == TimelineCurveChannelCatalog.CameraResponseEaseOut => clip.EaseOutCurve,
                _ => throw Unknown(owner, channelId)
            };
            return CopyCurve(curve);
        }

        public static void Replace(Clip owner, TimelineCurveChannelId channelId, AnimationCurve curve)
        {
            Validate(owner, channelId, curve);
            AnimationCurve copy = CopyCurve(curve);
            switch (owner)
            {
                case AnimationClip clip when channelId == TimelineCurveChannelCatalog.AnimationWeight: clip.WeightCurve = copy; break;
                case AnimationClip clip when channelId == TimelineCurveChannelCatalog.AnimationEaseIn: clip.EaseInCurve = copy; break;
                case AnimationClip clip when channelId == TimelineCurveChannelCatalog.AnimationEaseOut: clip.EaseOutCurve = copy; break;
                case MotionCurveClip clip when channelId == TimelineCurveChannelCatalog.MotionWeight: clip.WeightCurve = copy; break;
                case MotionCurveClip clip when channelId == TimelineCurveChannelCatalog.MotionPositionX: clip.PositionX = copy; break;
                case MotionCurveClip clip when channelId == TimelineCurveChannelCatalog.MotionPositionY: clip.PositionY = copy; break;
                case MotionCurveClip clip when channelId == TimelineCurveChannelCatalog.MotionPositionZ: clip.PositionZ = copy; break;
                case MotionCurveClip clip when channelId == TimelineCurveChannelCatalog.MotionYaw: clip.Yaw = copy; break;
                case MotionCurveClip clip when channelId == TimelineCurveChannelCatalog.MotionEaseIn: clip.EaseInCurve = copy; break;
                case MotionCurveClip clip when channelId == TimelineCurveChannelCatalog.MotionEaseOut: clip.EaseOutCurve = copy; break;
                case MotionWarpClip clip when channelId == TimelineCurveChannelCatalog.MotionWarpPositionProgress: clip.PositionProgressCurve = copy; break;
                case MotionWarpClip clip when channelId == TimelineCurveChannelCatalog.MotionWarpYawProgress: clip.YawProgressCurve = copy; break;
                case CameraStateClip clip when channelId == TimelineCurveChannelCatalog.CameraStateWeight: clip.WeightCurve = copy; break;
                case CameraStateClip clip when channelId == TimelineCurveChannelCatalog.CameraStateEaseIn: clip.EaseInCurve = copy; break;
                case CameraStateClip clip when channelId == TimelineCurveChannelCatalog.CameraStateEaseOut: clip.EaseOutCurve = copy; break;
                case CameraResponseClip clip when channelId == TimelineCurveChannelCatalog.CameraResponseWeight: clip.WeightCurve = copy; break;
                case CameraResponseClip clip when channelId == TimelineCurveChannelCatalog.CameraResponseEaseIn: clip.EaseInCurve = copy; break;
                case CameraResponseClip clip when channelId == TimelineCurveChannelCatalog.CameraResponseEaseOut: clip.EaseOutCurve = copy; break;
                default: throw Unknown(owner, channelId);
            }
            owner.RebindTimeline();
        }

        public static void Validate(Clip owner, TimelineCurveChannelId channelId, AnimationCurve curve)
        {
            TimelineCurveChannelDescriptor descriptor = TimelineCurveChannelCatalog.Require(channelId.Value);
            if (!descriptor.Supports(owner))
                throw Unknown(owner, channelId);
            if (owner is MotionWarpClip warp)
            {
                AnimationCurve position = channelId == TimelineCurveChannelCatalog.MotionWarpPositionProgress ? curve : warp.PositionProgressCurve;
                AnimationCurve yaw = channelId == TimelineCurveChannelCatalog.MotionWarpYawProgress ? curve : warp.YawProgressCurve;
                if (!MotionWarpAuthoring.ValidateConfiguration(
                        warp.TranslationMode, warp.TargetOffsetSpace, warp.RotationMode, warp.RotationMethod,
                        warp.TargetPlanarOffset, warp.TargetYawOffsetDegrees,
                        warp.MaxTotalPositionCorrection, warp.MaxTotalYawCorrectionDegrees,
                        warp.MaximumYawRateDegreesPerSecond, warp.LimitPolicy,
                        position, yaw, null, warp))
                    throw new InvalidOperationException($"MotionWarp curve '{channelId}' violates its owner validation contract.");
                return;
            }
            ValidateNormalized(curve, descriptor.ValueDomain, 1);
        }

        public static AnimationCurve CopyCurve(AnimationCurve source)
        {
            if (source == null)
                throw new InvalidOperationException("Timeline curve cannot be null.");
            return new AnimationCurve(source.keys) { preWrapMode = source.preWrapMode, postWrapMode = source.postWrapMode };
        }

        public static ulong Revision(AnimationCurve curve)
        {
            if (curve == null)
                return 0UL;
            unchecked
            {
                ulong hash = 1469598103934665603UL;
                Mix(ref hash, (int)curve.preWrapMode);
                Mix(ref hash, (int)curve.postWrapMode);
                Keyframe[] keys = curve.keys;
                Mix(ref hash, keys.Length);
                for (int i = 0; i < keys.Length; i++)
                {
                    Keyframe key = keys[i];
                    Mix(ref hash, BitConverter.SingleToInt32Bits(key.time));
                    Mix(ref hash, BitConverter.SingleToInt32Bits(key.value));
                    Mix(ref hash, BitConverter.SingleToInt32Bits(key.inTangent));
                    Mix(ref hash, BitConverter.SingleToInt32Bits(key.outTangent));
                    Mix(ref hash, BitConverter.SingleToInt32Bits(key.inWeight));
                    Mix(ref hash, BitConverter.SingleToInt32Bits(key.outWeight));
                    Mix(ref hash, (int)key.weightedMode);
                }
                return hash;
            }
        }

        public static void ValidateNormalized(AnimationCurve curve, TimelineCurveValueDomain domain, int minimumKeys)
        {
            if (curve == null || curve.length < minimumKeys)
                throw new InvalidOperationException($"Timeline curve requires at least {minimumKeys} key(s).");
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                if (!IsFinite(key.time) || !IsFinite(key.value) || !IsFinite(key.inWeight) || !IsFinite(key.outWeight) ||
                    key.time < 0f || key.time > 1f || i > 0 && key.time <= keys[i - 1].time)
                    throw new InvalidOperationException($"Timeline curve key {i} has an invalid normalized time or non-finite payload.");
                if (domain.IsBounded && (key.value < domain.Minimum || key.value > domain.Maximum))
                    throw new InvalidOperationException($"Timeline curve key {i} value is outside [{domain.Minimum}, {domain.Maximum}].");
            }
        }

        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        static InvalidOperationException Unknown(Clip owner, TimelineCurveChannelId id) =>
            new InvalidOperationException($"Clip '{owner?.GetType().FullName ?? "null"}' does not own active Timeline curve channel '{id}'.");

        static void Mix(ref ulong hash, int value)
        {
            hash ^= (uint)value;
            hash *= 1099511628211UL;
        }
    }
}
#endif
