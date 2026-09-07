using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation;
using ThirdPersonCharacter.Pipeline.Animation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL
{
    internal static class CharacterAclAnimationQualityEvaluator
    {
        internal static CharacterAclAnimationQualityReport Evaluate(
            CharacterAnimationAuthoringSource source,
            CharacterAnimationSampleSet samples,
            CharacterAclAnimationCompressionResult compression,
            CharacterAnimationSamplingQualityEvaluation samplingQuality,
            CharacterAclCompressionSettings settings,
            IReadOnlyList<CharacterAclAnimationCompressionResult> groupCompressions = null,
            int groupClipIndex = 0)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (samples == null || samples.Source != source)
                throw new ArgumentException("ACL quality sample input does not match its source.", nameof(samples));
            if (compression == null)
                throw new ArgumentNullException(nameof(compression));
            if (samplingQuality == null)
                throw new ArgumentNullException(nameof(samplingQuality));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            settings.RequireValid();
            if (!source.HasTransformChannels || source.TransformTracks.Length != source.SourceRig.PhysicalBoneCount)
                throw new InvalidOperationException("ACL quality evaluation requires the transform channel set.");

            var errors = new List<string>();
            if (!samplingQuality.Report.publishable)
                errors.AddRange(samplingQuality.Report.errors);
            var tracks = CreateTrackReports(source, samplingQuality);
            int dynamicScalarTrackCount = samples.CompressedScalarTrackCount;
            byte[][] transformPayloads;
            byte[][] scalarPayloads;
            if (groupCompressions == null)
            {
                transformPayloads = new[] { compression.TransformPayload };
                scalarPayloads = new[] { compression.ScalarPayload };
                groupClipIndex = 0;
            }
            else
            {
                if (groupClipIndex < 0 || groupClipIndex >= groupCompressions.Count)
                    throw new ArgumentOutOfRangeException(nameof(groupClipIndex));
                transformPayloads = new byte[groupCompressions.Count][];
                scalarPayloads = new byte[groupCompressions.Count][];
                for (int i = 0; i < groupCompressions.Count; i++)
                {
                    transformPayloads[i] = groupCompressions[i].TransformPayload;
                    scalarPayloads[i] = groupCompressions[i].ScalarPayload;
                }
            }
            using (CharacterAclDecoder decoder = CharacterAclDecoder.CreateGroup(
                       transformPayloads,
                       scalarPayloads,
                       compression.DatabaseHeaderPayload,
                       compression.BulkMediumPayload,
                       compression.BulkLowPayload,
                       samples.PhysicalBoneCount,
                       dynamicScalarTrackCount,
                       groupClipIndex))
            using (var decodedTransforms = new NativeArray<CharacterAclNativeTransformSample>(
                       samples.PhysicalBoneCount,
                       Allocator.Temp,
                       NativeArrayOptions.UninitializedMemory))
            using (var decodedScalars = dynamicScalarTrackCount == 0
                       ? default
                       : new NativeArray<float>(
                           dynamicScalarTrackCount,
                           Allocator.Temp,
                           NativeArrayOptions.UninitializedMemory))
            {
                for (int timeIndex = 0;
                     timeIndex < samplingQuality.ValidationTimes.Length;
                     timeIndex++)
                {
                    float time = samplingQuality.ValidationTimes[timeIndex];
                    decoder.SampleTransforms(time, decodedTransforms);
                    if (dynamicScalarTrackCount > 0)
                        decoder.SampleScalars(time, decodedScalars);
                    EvaluateTransforms(source, samples, decodedTransforms, time, tracks);
                    EvaluateScalars(source, samples, decodedScalars, time, tracks);
                }
            }

            float maxClipToSamplingPositionError = 0f;
            float maxClipToSamplingRotationError = 0f;
            float maxClipToSamplingScaleError = 0f;
            float maxSamplingToAclPositionError = 0f;
            float maxSamplingToAclRotationError = 0f;
            float maxSamplingToAclScaleError = 0f;
            float maxClipToSamplingScalarError = 0f;
            float maxSamplingToAclScalarError = 0f;
            float maxClipToAclPositionError = 0f;
            float maxClipToAclRotationError = 0f;
            float maxClipToAclScaleError = 0f;
            float maxClipToAclScalarError = 0f;
            for (int i = 0; i < tracks.Length; i++)
            {
                CharacterAclAnimationTrackQualityReport report = tracks[i];
                maxClipToSamplingPositionError = Mathf.Max(
                    maxClipToSamplingPositionError,
                    report.maxClipToSamplingPositionError);
                maxClipToSamplingRotationError = Mathf.Max(
                    maxClipToSamplingRotationError,
                    report.maxClipToSamplingRotationError);
                maxClipToSamplingScaleError = Mathf.Max(
                    maxClipToSamplingScaleError,
                    report.maxClipToSamplingScaleError);
                maxSamplingToAclPositionError = Mathf.Max(
                    maxSamplingToAclPositionError,
                    report.maxSamplingToAclPositionError);
                maxSamplingToAclRotationError = Mathf.Max(
                    maxSamplingToAclRotationError,
                    report.maxSamplingToAclRotationError);
                maxSamplingToAclScaleError = Mathf.Max(
                    maxSamplingToAclScaleError,
                    report.maxSamplingToAclScaleError);
                maxClipToSamplingScalarError = Mathf.Max(
                    maxClipToSamplingScalarError,
                    report.maxClipToSamplingScalarError);
                maxSamplingToAclScalarError = Mathf.Max(
                    maxSamplingToAclScalarError,
                    report.maxSamplingToAclScalarError);
                maxClipToAclPositionError = Mathf.Max(
                    maxClipToAclPositionError,
                    report.maxClipToAclPositionError);
                maxClipToAclRotationError = Mathf.Max(
                    maxClipToAclRotationError,
                    report.maxClipToAclRotationError);
                maxClipToAclScaleError = Mathf.Max(
                    maxClipToAclScaleError,
                    report.maxClipToAclScaleError);
                maxClipToAclScalarError = Mathf.Max(
                    maxClipToAclScalarError,
                    report.maxClipToAclScalarError);
                var trackErrors = new List<string>();
                if (report.physicalBoneIndex >= 0 &&
                    (report.maxSamplingToAclPositionError > settings.TransformPrecision + 0.0001f ||
                     report.maxClipToAclPositionError > settings.TransformPrecision + 0.0001f))
                    trackErrors.Add("ACL position error exceeds the configured precision gate.");
                if (report.physicalBoneIndex >= 0 &&
                    (report.maxSamplingToAclScaleError > settings.ScalePrecision + 0.0001f ||
                     report.maxClipToAclScaleError > settings.ScalePrecision + 0.0001f))
                    trackErrors.Add("ACL scale error exceeds the configured precision gate.");
                if (report.physicalBoneIndex >= 0 &&
                    (report.maxSamplingToAclRotationError > settings.RotationPrecisionDegrees + 0.0001f ||
                     report.maxClipToAclRotationError > settings.RotationPrecisionDegrees + 0.0001f))
                    trackErrors.Add("ACL rotation error exceeds the configured degree gate.");
                if (report.scalarParameterIndex >= 0 &&
                    (report.maxSamplingToAclScalarError > settings.ScalarPrecision + 0.000001f ||
                     report.maxClipToAclScalarError > settings.ScalarPrecision + 0.000001f))
                    trackErrors.Add("ACL scalar error exceeds the configured precision gate.");
                report.errors = trackErrors.ToArray();
                if (trackErrors.Count > 0)
                    errors.Add($"{report.trackIdentity}: {string.Join("; ", trackErrors)}");
            }

            CharacterAnimationSamplingQualityReport sampling = samplingQuality.Report;
            return new CharacterAclAnimationQualityReport
            {
                formalClipIdentity = sampling.formalClipIdentity,
                sourceIdentity = source.SourceIdentity,
                sampleCount = samples.Grid.SampleCount,
                sampleRate = samples.Grid.ActualSampleRate,
                maxClipToSamplingPositionError = maxClipToSamplingPositionError,
                maxClipToSamplingRotationError = maxClipToSamplingRotationError,
                maxClipToSamplingScaleError = maxClipToSamplingScaleError,
                maxSamplingToAclPositionError = maxSamplingToAclPositionError,
                maxSamplingToAclRotationError = maxSamplingToAclRotationError,
                maxSamplingToAclScaleError = maxSamplingToAclScaleError,
                maxClipToSamplingScalarError = maxClipToSamplingScalarError,
                maxSamplingToAclScalarError = maxSamplingToAclScalarError,
                maxClipToAclPositionError = maxClipToAclPositionError,
                maxClipToAclRotationError = maxClipToAclRotationError,
                maxClipToAclScaleError = maxClipToAclScaleError,
                maxClipToAclScalarError = maxClipToAclScalarError,
                zzzRestorationEvaluated = false,
                zzzRestorationError = 0f,
                publishable = errors.Count == 0,
                errors = errors.ToArray(),
                tracks = tracks,
                sampling = sampling
            };
        }

        static CharacterAclAnimationTrackQualityReport[] CreateTrackReports(
            CharacterAnimationAuthoringSource source,
            CharacterAnimationSamplingQualityEvaluation samplingQuality)
        {
            var tracks = new CharacterAclAnimationTrackQualityReport[
                source.TransformTracks.Length + source.ScalarTracks.Length];
            for (int i = 0; i < source.TransformTracks.Length; i++)
            {
                CharacterAnimationSamplingTrackQualityReport sampling =
                    samplingQuality.Report.tracks[i];
                tracks[i] = new CharacterAclAnimationTrackQualityReport
                {
                    trackIdentity = source.TransformTracks[i].BoneId,
                    physicalBoneIndex = i,
                    maxClipToSamplingPositionError = sampling.maxClipToSamplingPositionError,
                    maxClipToSamplingRotationError = sampling.maxClipToSamplingRotationError,
                    maxClipToSamplingScaleError = sampling.maxClipToSamplingScaleError
                };
            }
            for (int i = 0; i < source.ScalarTracks.Length; i++)
            {
                CharacterAnimationSamplingTrackQualityReport sampling =
                    samplingQuality.Report.tracks[source.TransformTracks.Length + i];
                tracks[source.TransformTracks.Length + i] = new CharacterAclAnimationTrackQualityReport
                {
                    trackIdentity = $"scalar/{source.ScalarTracks[i].ParameterId}",
                    scalarParameterIndex = source.ScalarTracks[i].ParameterIndex,
                    maxClipToSamplingScalarError = sampling.maxClipToSamplingScalarError
                };
            }
            return tracks;
        }

        static void EvaluateTransforms(
            CharacterAnimationAuthoringSource source,
            CharacterAnimationSampleSet samples,
            NativeArray<CharacterAclNativeTransformSample> decoded,
            float time,
            CharacterAclAnimationTrackQualityReport[] reports)
        {
            for (int bone = 0; bone < source.TransformTracks.Length; bone++)
            {
                AnimationLocalBonePose clip = source.TransformTracks[bone].Sample(time);
                AnimationLocalBonePose sampled = samples.SampleTransform(bone, time);
                AnimationLocalBonePose acl = decoded[bone].ToPose();
                CharacterAclAnimationTrackQualityReport report = reports[bone];
                report.maxSamplingToAclPositionError = Mathf.Max(
                    report.maxSamplingToAclPositionError,
                    Vector3.Distance(sampled.Position, acl.Position));
                report.maxSamplingToAclRotationError = Mathf.Max(
                    report.maxSamplingToAclRotationError,
                    Quaternion.Angle(sampled.Rotation, acl.Rotation));
                report.maxSamplingToAclScaleError = Mathf.Max(
                    report.maxSamplingToAclScaleError,
                    Vector3.Distance(sampled.Scale, acl.Scale));
                report.maxClipToAclPositionError = Mathf.Max(
                    report.maxClipToAclPositionError,
                    Vector3.Distance(clip.Position, acl.Position));
                report.maxClipToAclRotationError = Mathf.Max(
                    report.maxClipToAclRotationError,
                    Quaternion.Angle(clip.Rotation, acl.Rotation));
                report.maxClipToAclScaleError = Mathf.Max(
                    report.maxClipToAclScaleError,
                    Vector3.Distance(clip.Scale, acl.Scale));
            }
        }

        static void EvaluateScalars(
            CharacterAnimationAuthoringSource source,
            CharacterAnimationSampleSet samples,
            NativeArray<float> decoded,
            float time,
            CharacterAclAnimationTrackQualityReport[] reports)
        {
            int dynamicTrackIndex = 0;
            for (int scalar = 0; scalar < source.ScalarTracks.Length; scalar++)
            {
                CharacterAnimationAuthoringScalarTrack sourceTrack = source.ScalarTracks[scalar];
                float clip = sourceTrack.IsAnimated
                    ? sourceTrack.Curve.Evaluate(time)
                    : sourceTrack.DefaultValue;
                float sampled = samples.SampleScalar(scalar, time);
                float acl = sourceTrack.IsAnimated
                    ? decoded[dynamicTrackIndex++]
                    : sourceTrack.DefaultValue;
                CharacterAclAnimationTrackQualityReport report = reports[
                    source.TransformTracks.Length + scalar];
                report.maxSamplingToAclScalarError = Mathf.Max(
                    report.maxSamplingToAclScalarError,
                    Mathf.Abs(sampled - acl));
                report.maxClipToAclScalarError = Mathf.Max(
                    report.maxClipToAclScalarError,
                    Mathf.Abs(clip - acl));
            }
        }
    }
}
