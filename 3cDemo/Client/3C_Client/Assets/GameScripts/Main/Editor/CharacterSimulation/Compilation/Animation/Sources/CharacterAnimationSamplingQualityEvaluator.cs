using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation
{
    internal static class CharacterAnimationSamplingQualityEvaluator
    {
        const int SubsamplesPerInterval = 4;
        const float TimeTolerance = 0.000001f;

        internal static CharacterAnimationSamplingQualityEvaluation Evaluate(
            CharacterAnimationAuthoringSource source,
            CharacterAnimationSampleSet samples,
            CharacterAnimationSamplingQualitySettings settings)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (samples == null)
                throw new ArgumentNullException(nameof(samples));
            if (samples.Source != source)
                throw new InvalidOperationException("Animation quality input source and sample set do not match.");
            float[] validationTimes = BuildValidationTimes(source, samples.Grid);
            var tracks = new CharacterAnimationSamplingTrackQualityReport[
                source.TransformTracks.Length + source.ScalarTracks.Length];
            for (int i = 0; i < source.TransformTracks.Length; i++)
            {
                tracks[i] = new CharacterAnimationSamplingTrackQualityReport
                {
                    trackIdentity = source.TransformTracks[i].BoneId,
                    physicalBoneIndex = i
                };
            }
            for (int i = 0; i < source.ScalarTracks.Length; i++)
            {
                tracks[source.TransformTracks.Length + i] = new CharacterAnimationSamplingTrackQualityReport
                {
                    trackIdentity = $"scalar/{source.ScalarTracks[i].ParameterId}",
                    scalarParameterIndex = source.ScalarTracks[i].ParameterIndex
                };
            }

            float maxPositionError = 0f;
            float maxRotationError = 0f;
            float maxScaleError = 0f;
            float maxScalarError = 0f;
            for (int timeIndex = 0; timeIndex < validationTimes.Length; timeIndex++)
            {
                float time = validationTimes[timeIndex];
                for (int bone = 0; bone < source.TransformTracks.Length; bone++)
                {
                    AnimationLocalBonePose expected = source.TransformTracks[bone].Sample(time);
                    AnimationLocalBonePose actual = samples.SampleTransform(bone, time);
                    CharacterAnimationSamplingTrackQualityReport report = tracks[bone];
                    report.maxClipToSamplingPositionError = Mathf.Max(
                        report.maxClipToSamplingPositionError,
                        Vector3.Distance(expected.Position, actual.Position));
                    report.maxClipToSamplingRotationError = Mathf.Max(
                        report.maxClipToSamplingRotationError,
                        Quaternion.Angle(expected.Rotation, actual.Rotation));
                    report.maxClipToSamplingScaleError = Mathf.Max(
                        report.maxClipToSamplingScaleError,
                        Vector3.Distance(expected.Scale, actual.Scale));
                }
                for (int scalar = 0; scalar < source.ScalarTracks.Length; scalar++)
                {
                    CharacterAnimationAuthoringScalarTrack sourceTrack = source.ScalarTracks[scalar];
                    float expected = sourceTrack.IsAnimated
                        ? sourceTrack.Curve.Evaluate(time)
                        : sourceTrack.DefaultValue;
                    float actual = samples.SampleScalar(scalar, time);
                    CharacterAnimationSamplingTrackQualityReport report = tracks[
                        source.TransformTracks.Length + scalar];
                    report.maxClipToSamplingScalarError = Mathf.Max(
                        report.maxClipToSamplingScalarError,
                        Mathf.Abs(expected - actual));
                }
            }

            var errors = new List<string>();
            for (int i = 0; i < source.TransformTracks.Length; i++)
            {
                CharacterAnimationSamplingTrackQualityReport report = tracks[i];
                maxPositionError = Mathf.Max(maxPositionError, report.maxClipToSamplingPositionError);
                maxRotationError = Mathf.Max(maxRotationError, report.maxClipToSamplingRotationError);
                maxScaleError = Mathf.Max(maxScaleError, report.maxClipToSamplingScaleError);
                var trackErrors = new List<string>();
                if (report.maxClipToSamplingPositionError > settings.TransformPrecision + 0.000001f)
                    trackErrors.Add("Clip-to-sampling position error exceeds the configured precision gate.");
                if (report.maxClipToSamplingScaleError > settings.ScalePrecision + 0.000001f)
                    trackErrors.Add("Clip-to-sampling scale error exceeds the configured precision gate.");
                if (report.maxClipToSamplingRotationError > settings.RotationPrecisionDegrees + 0.0001f)
                    trackErrors.Add("Clip-to-sampling rotation error exceeds the configured degree gate.");
                report.errors = trackErrors.ToArray();
                if (trackErrors.Count > 0)
                    errors.Add($"{report.trackIdentity}: {string.Join("; ", trackErrors)}");
            }
            for (int i = 0; i < source.ScalarTracks.Length; i++)
            {
                CharacterAnimationSamplingTrackQualityReport report = tracks[
                    source.TransformTracks.Length + i];
                maxScalarError = Mathf.Max(maxScalarError, report.maxClipToSamplingScalarError);
                var trackErrors = new List<string>();
                if (report.maxClipToSamplingScalarError > settings.ScalarPrecision + 0.000001f)
                    trackErrors.Add("Clip-to-sampling scalar error exceeds the configured precision gate.");
                report.errors = trackErrors.ToArray();
                if (trackErrors.Count > 0)
                    errors.Add($"{report.trackIdentity}: {string.Join("; ", trackErrors)}");
            }

            var quality = new CharacterAnimationSamplingQualityReport
            {
                formalClipIdentity = source.FormalClipIdentity,
                sourceIdentity = source.SourceIdentity,
                sampleCount = samples.Grid.SampleCount,
                sampleRate = samples.Grid.ActualSampleRate,
                maxClipToSamplingPositionError = maxPositionError,
                maxClipToSamplingRotationError = maxRotationError,
                maxClipToSamplingScaleError = maxScaleError,
                maxClipToSamplingScalarError = maxScalarError,
                publishable = errors.Count == 0,
                errors = errors.ToArray(),
                tracks = tracks
            };
            return new CharacterAnimationSamplingQualityEvaluation(quality, validationTimes);
        }

        internal static float[] BuildValidationTimes(
            CharacterAnimationAuthoringSource source,
            CharacterAnimationSampleGrid grid)
        {
            var endpoints = new List<float>(grid.SampleCount + 2)
            {
                grid.FormalStart,
                grid.FormalStop
            };
            for (int sample = 0; sample < grid.SampleCount; sample++)
                endpoints.Add(grid.SampleTime(sample));
            for (int i = 0; i < source.TransformTracks.Length; i++)
                AddKeyTimes(endpoints, source.TransformTracks[i].Curves, grid);
            for (int i = 0; i < source.ScalarTracks.Length; i++)
            {
                AnimationCurve curve = source.ScalarTracks[i].Curve;
                if (curve != null)
                    AddKeyTimes(endpoints, curve, grid);
            }
            endpoints.Sort();
            var unique = new List<float>(endpoints.Count);
            for (int i = 0; i < endpoints.Count; i++)
            {
                if (unique.Count == 0 || Mathf.Abs(unique[unique.Count - 1] - endpoints[i]) > TimeTolerance)
                    unique.Add(endpoints[i]);
            }
            var validation = new List<float>(unique.Count * SubsamplesPerInterval);
            for (int interval = 0; interval < unique.Count - 1; interval++)
            {
                for (int sub = 0; sub <= SubsamplesPerInterval; sub++)
                {
                    validation.Add(Mathf.LerpUnclamped(
                        unique[interval],
                        unique[interval + 1],
                        sub / (float)SubsamplesPerInterval));
                }
            }
            return validation.ToArray();
        }

        static void AddKeyTimes(
            List<float> times,
            IReadOnlyList<AnimationCurve> curves,
            CharacterAnimationSampleGrid grid)
        {
            for (int i = 0; i < curves.Count; i++)
            {
                if (curves[i] != null)
                    AddKeyTimes(times, curves[i], grid);
            }
        }

        static void AddKeyTimes(
            List<float> times,
            AnimationCurve curve,
            CharacterAnimationSampleGrid grid)
        {
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
                times.Add(Mathf.Clamp(keys[i].time, grid.FormalStart, grid.FormalStop));
        }
    }
}
