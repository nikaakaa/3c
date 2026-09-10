using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation
{
    [Flags]
    internal enum CharacterAnimationBuildChannel : byte
    {
        None = 0,
        Transform = 1,
        AnimatedProperty = 2
    }

    internal readonly struct CharacterAnimationSampleGrid
    {
        internal CharacterAnimationSampleGrid(
            float requestedSampleRate,
            float actualSampleRate,
            int sampleCount,
            float formalStart,
            float formalStop,
            float compressedStart,
            float compressedStop,
            bool looping)
        {
            if (!float.IsFinite(requestedSampleRate) || requestedSampleRate <= 0f ||
                !float.IsFinite(actualSampleRate) || actualSampleRate <= 0f ||
                sampleCount < 2 ||
                !float.IsFinite(formalStart) || !float.IsFinite(formalStop) ||
                formalStart < 0f || formalStop <= formalStart ||
                !float.IsFinite(compressedStart) || !float.IsFinite(compressedStop) ||
                compressedStart != formalStart || compressedStop != formalStop)
                throw new ArgumentException("Animation sample grid is invalid.");
            RequestedSampleRate = requestedSampleRate;
            ActualSampleRate = actualSampleRate;
            SampleCount = sampleCount;
            FormalStart = formalStart;
            FormalStop = formalStop;
            CompressedStart = compressedStart;
            CompressedStop = compressedStop;
            Looping = looping;
        }

        internal static CharacterAnimationSampleGrid Create(
            float duration,
            float requestedSampleRate,
            bool looping)
        {
            if (!float.IsFinite(duration) || duration <= 0f ||
                !float.IsFinite(requestedSampleRate) || requestedSampleRate <= 0f)
                throw new ArgumentException("Animation sample grid input is invalid.");
            int intervalCount = Mathf.Max(1, Mathf.RoundToInt(duration * requestedSampleRate));
            return new CharacterAnimationSampleGrid(
                requestedSampleRate,
                intervalCount / duration,
                checked(intervalCount + 1),
                0f,
                duration,
                0f,
                duration,
                looping);
        }

        internal float RequestedSampleRate { get; }
        internal float ActualSampleRate { get; }
        internal int SampleCount { get; }
        internal float FormalStart { get; }
        internal float FormalStop { get; }
        internal float CompressedStart { get; }
        internal float CompressedStop { get; }
        internal bool Looping { get; }

        internal float SampleTime(int index)
        {
            if ((uint)index >= (uint)SampleCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (index == 0)
                return FormalStart;
            if (index == SampleCount - 1)
                return FormalStop;
            return FormalStart + index / ActualSampleRate;
        }

        internal float SamplePosition(float time)
        {
            if (!float.IsFinite(time))
                throw new ArgumentOutOfRangeException(nameof(time));
            int lastIndex = SampleCount - 1;
            if (time <= FormalStart)
                return 0f;
            if (time >= FormalStop)
                return lastIndex;
            float position = (time - FormalStart) * ActualSampleRate;
            return Mathf.Clamp(position, 0f, lastIndex);
        }
    }

    internal sealed class CharacterAnimationParameterLayout
    {
        internal CharacterAnimationParameterLayout(
            CharacterPoseParameterDeclaration[] declarations)
        {
            if (declarations == null)
                throw new ArgumentNullException(nameof(declarations));
            Declarations = declarations;
            var ids = new HashSet<PoseParameterId>();
            var hashParts = new List<string>(declarations.Length * 5 + 1)
            {
                "character-animation-parameter-layout/v1"
            };
            for (int i = 0; i < declarations.Length; i++)
            {
                CharacterPoseParameterDeclaration declaration = declarations[i] ??
                    throw new ArgumentException("Animation parameter layout contains a missing declaration.", nameof(declarations));
                if (!declaration.ParameterId.IsValid || !ids.Add(declaration.ParameterId))
                    throw new ArgumentException("Animation parameter layout contains a duplicate identity.", nameof(declarations));
                hashParts.Add(i.ToString(CultureInfo.InvariantCulture));
                hashParts.Add(declaration.ParameterId.Value);
                hashParts.Add(((int)declaration.ValueType).ToString(CultureInfo.InvariantCulture));
                hashParts.Add(((int)declaration.Usage).ToString(CultureInfo.InvariantCulture));
                hashParts.Add(declaration.Unit ?? string.Empty);
                hashParts.Add(declaration.DefaultValue.ToString("R", CultureInfo.InvariantCulture));
            }
            Count = declarations.Length;
            Hash = StableHash.Compute(hashParts.ToArray()).Value;
        }

        internal CharacterPoseParameterDeclaration[] Declarations { get; }
        internal int Count { get; }
        internal string Hash { get; }

        internal int RequireIndex(PoseParameterId parameterId)
        {
            for (int i = 0; i < Declarations.Length; i++)
            {
                if (Declarations[i].ParameterId.Equals(parameterId))
                    return i;
            }
            throw new InvalidOperationException($"Animation parameter '{parameterId}' is absent from the supplied layout.");
        }
    }

    internal static class CharacterAnimationParameterLayoutCompiler
    {
        internal static CharacterAnimationParameterLayout Build(
            CharacterAnimationInputContract contract)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            return new CharacterAnimationParameterLayout(
                contract.Parameters.ToArray());
        }

        internal static CharacterAnimationParameterLayout Build(CharacterPoseCanvasGraph graph)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            CharacterPoseParameterDeclaration[] declarations = graph.Parameters
                .Where(value => value != null)
                .OrderBy(value => value.ParameterId)
                .ToArray();
            return new CharacterAnimationParameterLayout(declarations);
        }
    }

    internal sealed class CharacterAnimationBuildInput
    {
        internal static CharacterAclNativeArtifactIdentity RequireNativeArtifactIdentity() =>
            CharacterAclNativeArtifactIdentity.RequireCurrent();

        internal CharacterAnimationBuildInput(
            string ownerAssetGuid,
            CharacterAclNativeArtifactIdentity nativeArtifactIdentity,
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationSourceRig sourceRig,
            CharacterAnimationParameterLayout parameterLayout,
            CharacterAclCompressionSettings compression,
            IReadOnlyList<CharacterAnimationSourceResourceBinding> sourceResourceBindings)
        {
            OwnerAssetGuid = ownerAssetGuid ?? throw new ArgumentNullException(nameof(ownerAssetGuid));
            Profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
            SourceRig = sourceRig ?? throw new ArgumentNullException(nameof(sourceRig));
            ParameterLayout = parameterLayout ?? throw new ArgumentNullException(nameof(parameterLayout));
            Compression = compression ?? throw new ArgumentNullException(nameof(compression));
            NativeArtifactIdentity = nativeArtifactIdentity ??
                throw new ArgumentNullException(nameof(nativeArtifactIdentity));
            SourceResourceBindings = sourceResourceBindings ??
                throw new ArgumentNullException(nameof(sourceResourceBindings));
            SourceRig.RequireValid();
            Compression.RequireValid();
            AnimationCatalog = new CharacterAnimationBuildCatalogCompiler(this);
        }

        internal string OwnerAssetGuid { get; }
        internal CharacterAclNativeArtifactIdentity NativeArtifactIdentity { get; }
        internal CharacterAnimationPresentationProfile Profile { get; }
        internal CharacterAnimationSourceRig SourceRig { get; }
        internal CharacterAnimationParameterLayout ParameterLayout { get; }
        internal CharacterAclCompressionSettings Compression { get; }
        internal IReadOnlyList<CharacterAnimationSourceResourceBinding> SourceResourceBindings { get; }
        internal bool HasSourceRig => SourceRig != null;
        internal CharacterAnimationBuildCatalogCompiler AnimationCatalog { get; }

        internal CharacterAnimationBuildInput CreatePoseOnlyInput() =>
            new CharacterAnimationBuildInput(
                OwnerAssetGuid,
                NativeArtifactIdentity,
                Profile,
                SourceRig,
                ParameterLayout,
                Compression,
                Array.Empty<CharacterAnimationSourceResourceBinding>());
    }

    internal sealed class CharacterAnimationAuthoringReadRequest
    {
        internal CharacterAnimationAuthoringReadRequest(
            AnimationClip clip,
            CharacterAnimationClipContentIdentity clipIdentity,
            CharacterAnimationSourceRig sourceRig,
            CharacterAnimationParameterLayout parameterLayout,
            IReadOnlyList<CharacterAnimationPropertyAuthoringBinding> propertyBindings,
            IReadOnlyList<CharacterAnimationParameterCurveSourceBinding> parameterCurveBindings,
            string sourceIdentity,
            CharacterAnimationBuildChannel channels)
        {
            Clip = clip ? clip : throw new ArgumentNullException(nameof(clip));
            ClipIdentity = clipIdentity;
            SourceRig = sourceRig ?? throw new ArgumentNullException(nameof(sourceRig));
            ParameterLayout = parameterLayout ?? throw new ArgumentNullException(nameof(parameterLayout));
            PropertyBindings = propertyBindings ?? Array.Empty<CharacterAnimationPropertyAuthoringBinding>();
            ParameterCurveBindings = parameterCurveBindings ??
                Array.Empty<CharacterAnimationParameterCurveSourceBinding>();
            SourceIdentity = string.IsNullOrWhiteSpace(sourceIdentity)
                ? throw new ArgumentException("Animation source identity is required.", nameof(sourceIdentity))
                : sourceIdentity.Trim();
            if (channels == CharacterAnimationBuildChannel.None ||
                (channels & ~(CharacterAnimationBuildChannel.Transform |
                    CharacterAnimationBuildChannel.AnimatedProperty)) != 0)
                throw new ArgumentException("Animation build channel set is invalid.", nameof(channels));
            Channels = channels;
        }

        internal AnimationClip Clip { get; }
        internal CharacterAnimationClipContentIdentity ClipIdentity { get; }
        internal CharacterAnimationSourceRig SourceRig { get; }
        internal CharacterAnimationParameterLayout ParameterLayout { get; }
        internal IReadOnlyList<CharacterAnimationPropertyAuthoringBinding> PropertyBindings { get; }
        internal IReadOnlyList<CharacterAnimationParameterCurveSourceBinding> ParameterCurveBindings { get; }
        internal string SourceIdentity { get; }
        internal CharacterAnimationBuildChannel Channels { get; }
    }

    internal sealed class CharacterAnimationAuthoringSource
    {
        internal CharacterAnimationAuthoringSource(
            CharacterAnimationClipContentIdentity clipIdentity,
            string sourceIdentity,
            CharacterAnimationSourceRig sourceRig,
            CharacterAnimationParameterLayout parameterLayout,
            CharacterAnimationAuthoringTransformTrack[] transformTracks,
            CharacterAnimationAuthoringScalarTrack[] scalarTracks,
            CharacterAnimationBuildChannel channels)
        {
            ClipIdentity = clipIdentity;
            SourceIdentity = string.IsNullOrWhiteSpace(sourceIdentity)
                ? throw new ArgumentException("Animation source identity is required.", nameof(sourceIdentity))
                : sourceIdentity.Trim();
            SourceRig = sourceRig ?? throw new ArgumentNullException(nameof(sourceRig));
            ParameterLayout = parameterLayout ?? throw new ArgumentNullException(nameof(parameterLayout));
            TransformTracks = transformTracks ?? throw new ArgumentNullException(nameof(transformTracks));
            ScalarTracks = scalarTracks ?? throw new ArgumentNullException(nameof(scalarTracks));
            Channels = channels;
            if (channels == CharacterAnimationBuildChannel.None ||
                (channels & ~(CharacterAnimationBuildChannel.Transform |
                    CharacterAnimationBuildChannel.AnimatedProperty)) != 0 ||
                (channels.HasFlag(CharacterAnimationBuildChannel.Transform)
                    ? TransformTracks.Length != sourceRig.PhysicalBoneCount
                    : TransformTracks.Length != 0) ||
                (!channels.HasFlag(CharacterAnimationBuildChannel.AnimatedProperty) &&
                    ScalarTracks.Length != 0))
                throw new ArgumentException("Animation authoring transform track count does not match the physical Rig.");
            for (int i = 0; i < TransformTracks.Length; i++)
            {
                if (TransformTracks[i] == null || TransformTracks[i].PhysicalBoneIndex != i)
                    throw new ArgumentException("Animation authoring transform tracks are not dense.");
            }
            for (int i = 0; i < ScalarTracks.Length; i++)
            {
                if (ScalarTracks[i] == null)
                    throw new ArgumentException("Animation authoring scalar tracks contain a missing entry.");
            }
        }

        internal CharacterAnimationClipContentIdentity ClipIdentity { get; }
        internal string FormalClipIdentity => string.Concat(
            ClipIdentity.AssetGuid,
            "/",
            ClipIdentity.LocalFileId.ToString(CultureInfo.InvariantCulture));
        internal string SourceIdentity { get; }
        internal CharacterAnimationSourceRig SourceRig { get; }
        internal CharacterAnimationParameterLayout ParameterLayout { get; }
        internal CharacterAnimationAuthoringTransformTrack[] TransformTracks { get; }
        internal CharacterAnimationAuthoringScalarTrack[] ScalarTracks { get; }
        internal CharacterAnimationBuildChannel Channels { get; }
        internal bool HasTransformChannels =>
            (Channels & CharacterAnimationBuildChannel.Transform) != 0;
        internal bool HasAnimatedPropertyChannels =>
            (Channels & CharacterAnimationBuildChannel.AnimatedProperty) != 0;
        internal float DurationSeconds => ClipIdentity.SourceDurationSeconds;
        internal bool Looping => ClipIdentity.Loop;
    }

    internal sealed class CharacterAnimationAuthoringTransformTrack
    {
        internal CharacterAnimationAuthoringTransformTrack(
            string boneId,
            int physicalBoneIndex,
            int parentIndex,
            AnimationLocalBonePose referencePose,
            AnimationCurve[] curves,
            string path)
        {
            if (string.IsNullOrWhiteSpace(boneId) || physicalBoneIndex < 0 ||
                parentIndex < -1 || !referencePose.IsValid ||
                curves == null || curves.Length != 10 || path == null)
                throw new ArgumentException("Animation authoring Transform track is invalid.");
            BoneId = boneId;
            PhysicalBoneIndex = physicalBoneIndex;
            ParentIndex = parentIndex;
            ReferencePose = referencePose;
            Curves = curves;
            Path = path;
        }

        internal string BoneId { get; }
        internal int PhysicalBoneIndex { get; }
        internal int ParentIndex { get; }
        internal AnimationLocalBonePose ReferencePose { get; }
        internal AnimationCurve[] Curves { get; }
        internal string Path { get; }

        internal AnimationLocalBonePose Sample(float time)
        {
            Vector3 position = new Vector3(
                Evaluate(Curves[0], time, ReferencePose.Position.x),
                Evaluate(Curves[1], time, ReferencePose.Position.y),
                Evaluate(Curves[2], time, ReferencePose.Position.z));
            Quaternion rotation = new Quaternion(
                Evaluate(Curves[3], time, ReferencePose.Rotation.x),
                Evaluate(Curves[4], time, ReferencePose.Rotation.y),
                Evaluate(Curves[5], time, ReferencePose.Rotation.z),
                Evaluate(Curves[6], time, ReferencePose.Rotation.w));
            Vector3 scale = new Vector3(
                Evaluate(Curves[7], time, ReferencePose.Scale.x),
                Evaluate(Curves[8], time, ReferencePose.Scale.y),
                Evaluate(Curves[9], time, ReferencePose.Scale.z));
            if (!float.IsFinite(rotation.x) || !float.IsFinite(rotation.y) ||
                !float.IsFinite(rotation.z) || !float.IsFinite(rotation.w) ||
                Quaternion.Dot(rotation, rotation) <= 0.00000001f)
                throw new InvalidOperationException(
                    $"Transform rotation sample at {time:R} is invalid: " +
                    $"quaternion=({rotation.x:R}, {rotation.y:R}, {rotation.z:R}, {rotation.w:R}).");
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z) ||
                !float.IsFinite(scale.x) || !float.IsFinite(scale.y) || !float.IsFinite(scale.z))
                throw new InvalidOperationException(
                    $"Transform sample at {time:R} is not finite: " +
                    $"position=({position.x:R}, {position.y:R}, {position.z:R}), " +
                    $"scale=({scale.x:R}, {scale.y:R}, {scale.z:R}).");
            return new AnimationLocalBonePose(position, rotation.normalized, scale);
        }

        static float Evaluate(AnimationCurve curve, float time, float fallback) =>
            curve == null ? fallback : curve.Evaluate(time);
    }

    internal sealed class CharacterAnimationAuthoringScalarTrack
    {
        internal CharacterAnimationAuthoringScalarTrack(
            PoseParameterId parameterId,
            int parameterIndex,
            float defaultValue,
            string unit,
            EditorCurveBinding curveBinding,
            AnimationCurve curve)
        {
            if (!parameterId.IsValid || parameterIndex < 0 ||
                !float.IsFinite(defaultValue) || unit == null ||
                curveBinding.type == null ||
                string.IsNullOrWhiteSpace(curveBinding.propertyName))
                throw new ArgumentException("Animation authoring Scalar track is invalid.");
            ParameterId = parameterId;
            ParameterIndex = parameterIndex;
            DefaultValue = defaultValue;
            Unit = unit;
            CurveBinding = curveBinding;
            Curve = curve;
        }

        internal PoseParameterId ParameterId { get; }
        internal int ParameterIndex { get; }
        internal float DefaultValue { get; }
        internal string Unit { get; }
        internal EditorCurveBinding CurveBinding { get; }
        internal AnimationCurve Curve { get; }
        internal bool IsAnimated => Curve != null;
    }

    internal sealed class CharacterAnimationSampledScalarTrack
    {
        internal CharacterAnimationSampledScalarTrack(
            CharacterAnimationAuthoringScalarTrack source,
            float[] samples)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Samples = samples ?? throw new ArgumentNullException(nameof(samples));
            if (source.IsAnimated && samples.Length < 2)
                throw new ArgumentException("Animated scalar track has no samples.", nameof(samples));
            for (int i = 0; i < samples.Length; i++)
            {
                if (!float.IsFinite(samples[i]))
                    throw new ArgumentException("Scalar sample is not finite.", nameof(samples));
            }
        }

        internal CharacterAnimationAuthoringScalarTrack Source { get; }
        internal float[] Samples { get; }
    }

    internal sealed class CharacterAnimationSampleSet
    {
        internal CharacterAnimationSampleSet(
            CharacterAnimationAuthoringSource source,
            CharacterAnimationSampleGrid grid,
            float[] transformSamples,
            float[] transformDefaults,
            int[] parentIndices,
            CharacterAnimationSampledScalarTrack[] scalarTracks,
            CharacterAnimationScalarCurvePage nativeScalarPage)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Grid = grid;
            TransformSamples = transformSamples ?? throw new ArgumentNullException(nameof(transformSamples));
            TransformDefaults = transformDefaults ?? throw new ArgumentNullException(nameof(transformDefaults));
            ParentIndices = parentIndices ?? throw new ArgumentNullException(nameof(parentIndices));
            ScalarTracks = scalarTracks ?? throw new ArgumentNullException(nameof(scalarTracks));
            NativeScalarPage = nativeScalarPage;
            PhysicalBoneCount = source.TransformTracks.Length;
            ParameterCount = source.ParameterLayout.Count;
            if (TransformSamples.Length != checked(grid.SampleCount * PhysicalBoneCount * 10) ||
                TransformDefaults.Length != checked(PhysicalBoneCount * 10) ||
                ParentIndices.Length != PhysicalBoneCount ||
                ScalarTracks.Length != source.ScalarTracks.Length)
                throw new ArgumentException("Animation sample set is invalid.");
            if (nativeScalarPage != null)
                nativeScalarPage.RequireValid();
        }

        internal CharacterAnimationAuthoringSource Source { get; }
        internal CharacterAnimationSampleGrid Grid { get; }
        internal int PhysicalBoneCount { get; }
        internal int ParameterCount { get; }
        internal int CompressedScalarTrackCount => ScalarTracks.Count(value => value.Source.IsAnimated);
        internal float[] TransformSamples { get; }
        internal float[] TransformDefaults { get; }
        internal int[] ParentIndices { get; }
        internal CharacterAnimationSampledScalarTrack[] ScalarTracks { get; }
        internal CharacterAnimationScalarCurvePage NativeScalarPage { get; }

        internal AnimationLocalBonePose SampleTransform(int physicalBoneIndex, float time)
        {
            if ((uint)physicalBoneIndex >= (uint)PhysicalBoneCount)
                throw new ArgumentOutOfRangeException(nameof(physicalBoneIndex));
            float position = Grid.SamplePosition(time);
            int lastIndex = Grid.SampleCount - 1;
            if (position <= 0f)
                return ReadPose(physicalBoneIndex);
            if (position >= lastIndex)
                return ReadPose(lastIndex * PhysicalBoneCount + physicalBoneIndex);
            int lower = Mathf.FloorToInt(position);
            float factor = position - lower;
            return InterpolatePose(
                ReadPose(lower * PhysicalBoneCount + physicalBoneIndex),
                ReadPose((lower + 1) * PhysicalBoneCount + physicalBoneIndex),
                factor);
        }

        internal float SampleScalar(int scalarTrackIndex, float time)
        {
            if ((uint)scalarTrackIndex >= (uint)ScalarTracks.Length)
                throw new ArgumentOutOfRangeException(nameof(scalarTrackIndex));
            CharacterAnimationSampledScalarTrack track = ScalarTracks[scalarTrackIndex];
            if (!track.Source.IsAnimated)
                return track.Source.DefaultValue;
            float position = Grid.SamplePosition(time);
            int lastIndex = track.Samples.Length - 1;
            if (position <= 0f)
                return track.Samples[0];
            if (position >= lastIndex)
                return track.Samples[lastIndex];
            int lower = Mathf.FloorToInt(position);
            return InterpolateLinear(
                track.Samples[lower],
                track.Samples[lower + 1],
                position - lower);
        }

        internal float[] BuildCompressedScalarSamples()
        {
            CharacterAnimationSampledScalarTrack[] dynamicTracks =
                ScalarTracks.Where(value => value.Source.IsAnimated).ToArray();
            var result = new float[checked(Grid.SampleCount * dynamicTracks.Length)];
            for (int sample = 0; sample < Grid.SampleCount; sample++)
            {
                for (int track = 0; track < dynamicTracks.Length; track++)
                    result[sample * dynamicTracks.Length + track] = dynamicTracks[track].Samples[sample];
            }
            return result;
        }

        AnimationLocalBonePose ReadPose(int sampleTrackIndex)
        {
            int offset = checked(sampleTrackIndex * 10);
            return new AnimationLocalBonePose(
                new Vector3(
                    TransformSamples[offset],
                    TransformSamples[offset + 1],
                    TransformSamples[offset + 2]),
                new Quaternion(
                    TransformSamples[offset + 3],
                    TransformSamples[offset + 4],
                    TransformSamples[offset + 5],
                    TransformSamples[offset + 6]),
                new Vector3(
                    TransformSamples[offset + 7],
                    TransformSamples[offset + 8],
                    TransformSamples[offset + 9]));
        }

        static AnimationLocalBonePose InterpolatePose(
            AnimationLocalBonePose left,
            AnimationLocalBonePose right,
            float factor)
        {
            if (factor <= 0f)
                return left;
            if (factor >= 1f)
                return right;
            Quaternion rightRotation = right.Rotation;
            if (Quaternion.Dot(left.Rotation, rightRotation) < 0f)
                rightRotation = new Quaternion(
                    -rightRotation.x,
                    -rightRotation.y,
                    -rightRotation.z,
                    -rightRotation.w);
            Quaternion rotation = new Quaternion(
                InterpolateLinear(left.Rotation.x, rightRotation.x, factor),
                InterpolateLinear(left.Rotation.y, rightRotation.y, factor),
                InterpolateLinear(left.Rotation.z, rightRotation.z, factor),
                InterpolateLinear(left.Rotation.w, rightRotation.w, factor));
            return new AnimationLocalBonePose(
                new Vector3(
                    InterpolateLinear(left.Position.x, right.Position.x, factor),
                    InterpolateLinear(left.Position.y, right.Position.y, factor),
                    InterpolateLinear(left.Position.z, right.Position.z, factor)),
                rotation.normalized,
                new Vector3(
                    InterpolateLinear(left.Scale.x, right.Scale.x, factor),
                    InterpolateLinear(left.Scale.y, right.Scale.y, factor),
                    InterpolateLinear(left.Scale.z, right.Scale.z, factor)));
        }

        static float InterpolateLinear(float left, float right, float factor) =>
            (left - factor * left) + factor * right;
    }

    [Serializable]
    public sealed class CharacterAnimationSamplingQualityReport
    {
        public string schema = "character-animation-sampling-quality-report/v1";
        public string formalClipIdentity = string.Empty;
        public string sourceIdentity = string.Empty;
        public int sampleCount;
        public float sampleRate;
        public float maxClipToSamplingPositionError;
        public float maxClipToSamplingRotationError;
        public float maxClipToSamplingScaleError;
        public float maxClipToSamplingScalarError;
        public bool publishable;
        public string[] errors = Array.Empty<string>();
        public CharacterAnimationSamplingTrackQualityReport[] tracks =
            Array.Empty<CharacterAnimationSamplingTrackQualityReport>();
    }

    [Serializable]
    public sealed class CharacterAnimationSamplingTrackQualityReport
    {
        public string trackIdentity = string.Empty;
        public int physicalBoneIndex = -1;
        public int scalarParameterIndex = -1;
        public float maxClipToSamplingPositionError;
        public float maxClipToSamplingRotationError;
        public float maxClipToSamplingScaleError;
        public float maxClipToSamplingScalarError;
        public string[] errors = Array.Empty<string>();
    }

    internal sealed class CharacterAnimationSamplingQualityEvaluation
    {
        internal CharacterAnimationSamplingQualityEvaluation(
            CharacterAnimationSamplingQualityReport report,
            float[] validationTimes)
        {
            Report = report ?? throw new ArgumentNullException(nameof(report));
            ValidationTimes = validationTimes ?? throw new ArgumentNullException(nameof(validationTimes));
        }

        internal CharacterAnimationSamplingQualityReport Report { get; }
        internal float[] ValidationTimes { get; }
    }

    internal readonly struct CharacterAnimationSamplingQualitySettings
    {
        internal CharacterAnimationSamplingQualitySettings(
            float transformPrecision,
            float scalePrecision,
            float rotationPrecisionDegrees,
            float scalarPrecision)
        {
            if (!float.IsFinite(transformPrecision) || transformPrecision < 0f ||
                !float.IsFinite(scalePrecision) || scalePrecision < 0f ||
                !float.IsFinite(rotationPrecisionDegrees) || rotationPrecisionDegrees <= 0f ||
                !float.IsFinite(scalarPrecision) || scalarPrecision < 0f)
                throw new ArgumentException("Animation sampling quality settings are invalid.");
            TransformPrecision = transformPrecision;
            ScalePrecision = scalePrecision;
            RotationPrecisionDegrees = rotationPrecisionDegrees;
            ScalarPrecision = scalarPrecision;
        }

        internal float TransformPrecision { get; }
        internal float ScalePrecision { get; }
        internal float RotationPrecisionDegrees { get; }
        internal float ScalarPrecision { get; }
    }
}
