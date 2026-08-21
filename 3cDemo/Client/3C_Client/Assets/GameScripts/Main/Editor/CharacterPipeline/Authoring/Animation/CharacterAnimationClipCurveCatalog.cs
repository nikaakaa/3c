using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public enum CharacterAnimationClipRegisteredCurveValueDomain : byte
    {
        Normalized01 = 1,
        UnwrappedPhase = 2
    }

    public sealed class CharacterAnimationClipRegisteredCurveDescriptor
    {
        internal CharacterAnimationClipRegisteredCurveDescriptor(
            string channelId,
            string runtimeParameterId,
            EditorCurveBinding binding,
            CharacterAnimationClipRegisteredCurveValueDomain valueDomain,
            bool requireFullSourceCoverage,
            bool requireStrictMonotonic)
        {
            ChannelId = channelId;
            RuntimeParameterId = runtimeParameterId;
            Binding = binding;
            ValueDomain = valueDomain;
            RequireFullSourceCoverage = requireFullSourceCoverage;
            RequireStrictMonotonic = requireStrictMonotonic;
        }

        public string ChannelId { get; }
        public string RuntimeParameterId { get; }
        public EditorCurveBinding Binding { get; }
        public CharacterAnimationClipRegisteredCurveValueDomain ValueDomain { get; }
        public bool RequireFullSourceCoverage { get; }
        public bool RequireStrictMonotonic { get; }
    }

    public readonly struct CharacterAnimationClipContentIdentity
    {
        public CharacterAnimationClipContentIdentity(
            string assetPath,
            string assetGuid,
            long localFileId,
            string fullDependencyHash,
            string analysisInputHash,
            string registeredCurveHash,
            float sourceDurationSeconds,
            bool loop)
        {
            AssetPath = assetPath;
            AssetGuid = assetGuid;
            LocalFileId = localFileId;
            FullDependencyHash = fullDependencyHash;
            AnalysisInputHash = analysisInputHash;
            RegisteredCurveHash = registeredCurveHash;
            SourceDurationSeconds = sourceDurationSeconds;
            Loop = loop;
        }

        public string AssetPath { get; }
        public string AssetGuid { get; }
        public long LocalFileId { get; }
        public string FullDependencyHash { get; }
        public string AnalysisInputHash { get; }
        public string RegisteredCurveHash { get; }
        public float SourceDurationSeconds { get; }
        public bool Loop { get; }
    }

    public static class CharacterAnimationClipRegisteredCurveCatalog
    {
        const float TimeTolerance = 0.00001f;
        const float ValueTolerance = 0.00001f;

        static readonly CharacterAnimationClipRegisteredCurveDescriptor[] Descriptors =
        {
            new CharacterAnimationClipRegisteredCurveDescriptor(
                CharacterAnimationClipRegisteredCurveChannels.LocomotionPhase,
                string.Empty,
                EditorCurveBinding.FloatCurve(
                    string.Empty,
                    typeof(CharacterAnimationClipAuthoringCurveReceiver),
                    CharacterAnimationClipRegisteredCurveChannels.LocomotionPhaseProperty),
                CharacterAnimationClipRegisteredCurveValueDomain.UnwrappedPhase,
                false,
                true),
            new CharacterAnimationClipRegisteredCurveDescriptor(
                CharacterAnimationClipRegisteredCurveChannels.FootPlacementWeight,
                AnimationPoseParameterIds.FootPlacementWeight.Value,
                EditorCurveBinding.FloatCurve(
                    string.Empty,
                    typeof(CharacterAnimationClipAuthoringCurveReceiver),
                    CharacterAnimationClipRegisteredCurveChannels.FootPlacementWeightProperty),
                CharacterAnimationClipRegisteredCurveValueDomain.Normalized01,
                true,
                false)
        };

        public static IReadOnlyList<CharacterAnimationClipRegisteredCurveDescriptor> Channels =>
            Descriptors;

        public static CharacterAnimationClipRegisteredCurveDescriptor Require(string channelId)
        {
            CharacterAnimationClipRegisteredCurveDescriptor descriptor = Descriptors.FirstOrDefault(
                value => string.Equals(value.ChannelId, channelId, StringComparison.Ordinal));
            return descriptor ?? throw new KeyNotFoundException(
                $"AnimationClip registered Curve channel '{channelId}' is not installed.");
        }

        public static CharacterAnimationClipRegisteredCurveDescriptor RequireRuntimeParameter(
            PoseParameterId parameterId)
        {
            if (!parameterId.IsValid)
                throw new ArgumentException("Pose Parameter identity is invalid.", nameof(parameterId));
            CharacterAnimationClipRegisteredCurveDescriptor descriptor = Descriptors.FirstOrDefault(
                value => string.Equals(value.RuntimeParameterId, parameterId.Value, StringComparison.Ordinal));
            return descriptor ?? throw new KeyNotFoundException(
                $"AnimationClip registered Curve for Runtime parameter '{parameterId}' is not installed.");
        }

        public static bool IsRegistered(EditorCurveBinding binding) =>
            Descriptors.Any(value => SameBinding(value.Binding, binding));

        public static bool SameBinding(EditorCurveBinding left, EditorCurveBinding right) =>
            string.Equals(left.path, right.path, StringComparison.Ordinal) &&
            left.type == right.type &&
            string.Equals(left.propertyName, right.propertyName, StringComparison.Ordinal) &&
            left.isPPtrCurve == right.isPPtrCurve;

        public static AnimationCurve ReadRequired(AnimationClip clip, string channelId)
        {
            RequireNativeClip(clip);
            CharacterAnimationClipRegisteredCurveDescriptor descriptor = Require(channelId);
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, descriptor.Binding);
            if (curve == null || curve.length < 2)
                throw new InvalidOperationException(
                    $"AnimationClip '{clip.name}' is missing registered Curve '{channelId}'.");
            Validate(clip, descriptor, curve, ResolveSourceDurationSeconds(clip));
            return Copy(curve);
        }

        public static bool TryRead(AnimationClip clip, string channelId, out AnimationCurve curve)
        {
            curve = null;
            if (!clip)
                return false;
            CharacterAnimationClipRegisteredCurveDescriptor descriptor = Require(channelId);
            AnimationCurve source = AnimationUtility.GetEditorCurve(clip, descriptor.Binding);
            if (source == null || source.length < 2)
                return false;
            Validate(clip, descriptor, source, ResolveSourceDurationSeconds(clip));
            curve = Copy(source);
            return true;
        }

        public static void Replace(AnimationClip clip, string channelId, AnimationCurve curve)
        {
            RequireNativeClip(clip);
            CharacterAnimationClipRegisteredCurveDescriptor descriptor = Require(channelId);
            Validate(clip, descriptor, curve, ResolveSourceDurationSeconds(clip));
            AnimationUtility.SetEditorCurve(clip, descriptor.Binding, Copy(curve));
            EditorUtility.SetDirty(clip);
        }

        public static void Remove(AnimationClip clip, string channelId)
        {
            RequireNativeClip(clip);
            CharacterAnimationClipRegisteredCurveDescriptor descriptor = Require(channelId);
            AnimationUtility.SetEditorCurve(clip, descriptor.Binding, null);
            EditorUtility.SetDirty(clip);
        }

        public static void Validate(AnimationClip clip, string channelId, AnimationCurve curve)
        {
            RequireNativeClip(clip);
            CharacterAnimationClipRegisteredCurveDescriptor descriptor = Require(channelId);
            Validate(clip, descriptor, curve, ResolveSourceDurationSeconds(clip));
        }

        public static CharacterAnimationClipContentIdentity ResolveIdentity(AnimationClip clip)
        {
            string path = RequireNativeClip(clip);
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip, out string guid, out long localFileId) ||
                string.IsNullOrEmpty(guid) || localFileId == 0)
            {
                throw new InvalidOperationException(
                    $"AnimationClip '{clip.name}' does not have a stable object identity.");
            }
            float sourceDuration = ResolveSourceDurationSeconds(clip);
            return new CharacterAnimationClipContentIdentity(
                path,
                guid,
                localFileId,
                AssetDatabase.GetAssetDependencyHash(path).ToString(),
                ComputeAnalysisInputHash(clip, sourceDuration),
                ComputeRegisteredCurveHash(clip),
                sourceDuration,
                clip.isLooping);
        }

        public static float ResolveSourceDurationSeconds(AnimationClip clip)
        {
            RequireNativeClip(clip);
            float duration = 0f;
            EditorCurveBinding[] floatBindings = AnimationUtility.GetCurveBindings(clip);
            for (int i = 0; i < floatBindings.Length; i++)
            {
                EditorCurveBinding binding = floatBindings[i];
                if (IsRegistered(binding))
                    continue;
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null || curve.length == 0)
                    continue;
                duration = Mathf.Max(duration, curve.keys[curve.length - 1].time);
            }
            EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            for (int i = 0; i < objectBindings.Length; i++)
            {
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, objectBindings[i]);
                if (keys != null && keys.Length > 0)
                    duration = Mathf.Max(duration, keys[keys.Length - 1].time);
            }
            if (!float.IsFinite(duration) || duration <= 0f)
                throw new InvalidOperationException(
                    $"AnimationClip '{clip.name}' has no finite non-registered source duration.");
            return duration;
        }

        public static string ComputeAnalysisInputHash(AnimationClip clip) =>
            ComputeAnalysisInputHash(clip, ResolveSourceDurationSeconds(clip));

        public static string ComputeRegisteredCurveHash(AnimationClip clip)
        {
            RequireNativeClip(clip);
            var tokens = new List<string> { "character-animation-clip-registered-curves/v1" };
            for (int i = 0; i < Descriptors.Length; i++)
            {
                CharacterAnimationClipRegisteredCurveDescriptor descriptor = Descriptors[i];
                tokens.Add(descriptor.ChannelId);
                AppendBinding(tokens, descriptor.Binding);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, descriptor.Binding);
                if (curve == null)
                {
                    tokens.Add("missing");
                    continue;
                }
                AppendCurve(tokens, curve);
            }
            return StableHash.Compute(tokens.ToArray()).Value;
        }

        public static AnimationCurve ConvertNormalizedToSeconds(
            AnimationCurve normalized,
            float sourceDurationSeconds)
        {
            if (normalized == null || normalized.length < 2 ||
                !float.IsFinite(sourceDurationSeconds) || sourceDurationSeconds <= 0f)
            {
                throw new ArgumentException("Normalized Curve migration input is invalid.");
            }
            Keyframe[] keys = normalized.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                if (!float.IsFinite(key.time) || key.time < 0f || key.time > 1f)
                    throw new InvalidOperationException("Normalized Curve key time is outside [0, 1].");
                key.time *= sourceDurationSeconds;
                key.inTangent /= sourceDurationSeconds;
                key.outTangent /= sourceDurationSeconds;
                keys[i] = key;
            }
            return new AnimationCurve(keys)
            {
                preWrapMode = normalized.preWrapMode,
                postWrapMode = normalized.postWrapMode
            };
        }

        static string ComputeAnalysisInputHash(AnimationClip clip, float sourceDuration)
        {
            var tokens = new List<string>
            {
                "character-animation-clip-analysis-input/v1",
                clip.isLooping ? "loop" : "finite",
                Bits(sourceDuration)
            };
            EditorCurveBinding[] floatBindings = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => !IsRegistered(binding))
                .OrderBy(binding => binding.path, StringComparer.Ordinal)
                .ThenBy(binding => binding.type?.AssemblyQualifiedName, StringComparer.Ordinal)
                .ThenBy(binding => binding.propertyName, StringComparer.Ordinal)
                .ToArray();
            for (int i = 0; i < floatBindings.Length; i++)
            {
                AppendBinding(tokens, floatBindings[i]);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, floatBindings[i]) ??
                    throw new InvalidOperationException("AnimationClip source Curve binding has no Curve.");
                AppendCurve(tokens, curve);
            }
            EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip)
                .OrderBy(binding => binding.path, StringComparer.Ordinal)
                .ThenBy(binding => binding.type?.AssemblyQualifiedName, StringComparer.Ordinal)
                .ThenBy(binding => binding.propertyName, StringComparer.Ordinal)
                .ToArray();
            for (int i = 0; i < objectBindings.Length; i++)
            {
                EditorCurveBinding binding = objectBindings[i];
                AppendBinding(tokens, binding);
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding) ??
                    Array.Empty<ObjectReferenceKeyframe>();
                tokens.Add(keys.Length.ToString(CultureInfo.InvariantCulture));
                for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
                {
                    tokens.Add(Bits(keys[keyIndex].time));
                    UnityEngine.Object value = keys[keyIndex].value;
                    if (!value)
                    {
                        tokens.Add("null");
                    }
                    else if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                                 value,
                                 out string guid,
                                 out long localFileId))
                    {
                        tokens.Add(guid);
                        tokens.Add(localFileId.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"AnimationClip '{clip.name}' object Curve contains an unstable reference.");
                    }
                }
            }
            return StableHash.Compute(tokens.ToArray()).Value;
        }

        static void Validate(
            AnimationClip clip,
            CharacterAnimationClipRegisteredCurveDescriptor descriptor,
            AnimationCurve curve,
            float sourceDuration)
        {
            if (curve == null || curve.length < 2)
                throw new InvalidOperationException(
                    $"AnimationClip '{clip.name}' Curve '{descriptor.ChannelId}' requires at least two keys.");
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                if (!float.IsFinite(key.time) || !float.IsFinite(key.value) ||
                    !float.IsFinite(key.inTangent) || !float.IsFinite(key.outTangent) ||
                    !float.IsFinite(key.inWeight) || !float.IsFinite(key.outWeight) ||
                    key.time < -TimeTolerance || key.time > sourceDuration + TimeTolerance ||
                    i > 0 && key.time <= keys[i - 1].time)
                {
                    throw new InvalidOperationException(
                        $"AnimationClip '{clip.name}' Curve '{descriptor.ChannelId}' key #{i} is invalid.");
                }
                if (descriptor.ValueDomain == CharacterAnimationClipRegisteredCurveValueDomain.Normalized01 &&
                    (key.value < -ValueTolerance || key.value > 1f + ValueTolerance))
                {
                    throw new InvalidOperationException(
                        $"AnimationClip '{clip.name}' Curve '{descriptor.ChannelId}' key #{i} is outside [0, 1].");
                }
                if (descriptor.RequireStrictMonotonic && key.weightedMode != WeightedMode.None)
                {
                    throw new InvalidOperationException(
                        $"AnimationClip '{clip.name}' Curve '{descriptor.ChannelId}' does not allow weighted tangents.");
                }
            }
            if (descriptor.RequireFullSourceCoverage &&
                (Mathf.Abs(keys[0].time) > TimeTolerance ||
                 Mathf.Abs(keys[keys.Length - 1].time - sourceDuration) > TimeTolerance))
            {
                throw new InvalidOperationException(
                    $"AnimationClip '{clip.name}' Curve '{descriptor.ChannelId}' does not cover the source duration.");
            }
            if (descriptor.RequireStrictMonotonic)
                RequireStrictMonotonic(clip, descriptor, keys);
            if (descriptor.ValueDomain == CharacterAnimationClipRegisteredCurveValueDomain.Normalized01)
                RequireNormalizedRange(clip, descriptor, curve, keys);
        }

        static void RequireStrictMonotonic(
            AnimationClip clip,
            CharacterAnimationClipRegisteredCurveDescriptor descriptor,
            IReadOnlyList<Keyframe> keys)
        {
            for (int i = 0; i < keys.Count - 1; i++)
            {
                Keyframe left = keys[i];
                Keyframe right = keys[i + 1];
                float duration = right.time - left.time;
                if (right.value <= left.value || duration <= 0f)
                    throw new InvalidOperationException(
                        $"AnimationClip '{clip.name}' Curve '{descriptor.ChannelId}' segment #{i} is not increasing.");
                float tangent0 = left.outTangent * duration;
                float tangent1 = right.inTangent * duration;
                float a = 2f * left.value - 2f * right.value + tangent0 + tangent1;
                float b = -3f * left.value + 3f * right.value - 2f * tangent0 - tangent1;
                float c = tangent0;
                if (Derivative(a, b, c, 0f) <= 0f || Derivative(a, b, c, 1f) <= 0f)
                    throw new InvalidOperationException(
                        $"AnimationClip '{clip.name}' Curve '{descriptor.ChannelId}' segment #{i} has a non-positive endpoint slope.");
                if (Mathf.Abs(a) > 0.0000001f)
                {
                    float critical = -b / (3f * a);
                    if (critical > 0f && critical < 1f && Derivative(a, b, c, critical) <= 0f)
                        throw new InvalidOperationException(
                            $"AnimationClip '{clip.name}' Curve '{descriptor.ChannelId}' segment #{i} reverses inside the segment.");
                }
            }
        }

        static void RequireNormalizedRange(
            AnimationClip clip,
            CharacterAnimationClipRegisteredCurveDescriptor descriptor,
            AnimationCurve curve,
            IReadOnlyList<Keyframe> keys)
        {
            for (int i = 0; i < keys.Count - 1; i++)
            {
                float start = keys[i].time;
                float end = keys[i + 1].time;
                for (int sample = 1; sample < 32; sample++)
                {
                    float value = curve.Evaluate(Mathf.Lerp(start, end, sample / 32f));
                    if (!float.IsFinite(value) || value < -ValueTolerance || value > 1f + ValueTolerance)
                        throw new InvalidOperationException(
                            $"AnimationClip '{clip.name}' Curve '{descriptor.ChannelId}' leaves [0, 1] inside segment #{i}.");
                }
            }
        }

        static float Derivative(float a, float b, float c, float time) =>
            3f * a * time * time + 2f * b * time + c;

        static string RequireNativeClip(AnimationClip clip)
        {
            if (!clip)
                throw new ArgumentNullException(nameof(clip));
            string path = AssetDatabase.GetAssetPath(clip);
            if (string.IsNullOrEmpty(path) ||
                !path.EndsWith(".anim", StringComparison.OrdinalIgnoreCase) ||
                AssetDatabase.LoadMainAssetAtPath(path) != clip ||
                !AssetDatabase.IsOpenForEdit(clip, StatusQueryOptions.UseCachedIfPossible))
            {
                throw new InvalidOperationException(
                    $"AnimationClip '{clip.name}' must be an editable native .anim asset.");
            }
            return path.Replace('\\', '/');
        }

        static void AppendBinding(List<string> tokens, EditorCurveBinding binding)
        {
            tokens.Add(binding.path ?? string.Empty);
            tokens.Add(binding.type?.AssemblyQualifiedName ?? string.Empty);
            tokens.Add(binding.propertyName ?? string.Empty);
            tokens.Add(binding.isPPtrCurve ? "object" : "float");
        }

        static void AppendCurve(List<string> tokens, AnimationCurve curve)
        {
            tokens.Add(((int)curve.preWrapMode).ToString(CultureInfo.InvariantCulture));
            tokens.Add(((int)curve.postWrapMode).ToString(CultureInfo.InvariantCulture));
            Keyframe[] keys = curve.keys;
            tokens.Add(keys.Length.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                tokens.Add(Bits(key.time));
                tokens.Add(Bits(key.value));
                tokens.Add(Bits(key.inTangent));
                tokens.Add(Bits(key.outTangent));
                tokens.Add(Bits(key.inWeight));
                tokens.Add(Bits(key.outWeight));
                tokens.Add(((int)key.weightedMode).ToString(CultureInfo.InvariantCulture));
            }
        }

        static AnimationCurve Copy(AnimationCurve source) =>
            new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };

        static string Bits(float value) =>
            BitConverter.SingleToInt32Bits(value).ToString("x8", CultureInfo.InvariantCulture);
    }
}
