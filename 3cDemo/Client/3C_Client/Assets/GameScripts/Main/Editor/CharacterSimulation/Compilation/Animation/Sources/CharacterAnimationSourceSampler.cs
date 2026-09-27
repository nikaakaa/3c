using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation
{
    internal static class CharacterAnimationSourceSampler
    {
        internal static CharacterAnimationSampleSet SampleToPrecision(
            CharacterAnimationAuthoringSource source,
            float requestedSampleRate,
            CharacterAnimationSamplingQualitySettings settings,
            out CharacterAnimationSamplingQualityEvaluation quality)
        {
            for (int refinement = 0; ; refinement++)
            {
                CharacterAnimationSampleGrid grid = CharacterAnimationSampleGrid.Create(
                    source.DurationSeconds, requestedSampleRate, source.Looping);
                CharacterAnimationSampleSet samples = Sample(source, grid);
                quality = CharacterAnimationSamplingQualityEvaluator.Evaluate(source, samples, settings);
                if (quality.Report.publishable || refinement == 3)
                    return samples;
                requestedSampleRate *= 2f;
            }
        }

        internal static CharacterAnimationSampleSet Sample(
            CharacterAnimationAuthoringSource source,
            CharacterAnimationSampleGrid grid)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (Mathf.Abs(grid.FormalStop - source.DurationSeconds) > 0.000001f ||
                grid.Looping != source.Looping)
                throw new InvalidOperationException("Animation sample grid does not match the authoring source.");
            int transformTrackCount = source.TransformTracks.Length;
            float[] transformSamples = new float[
                checked(grid.SampleCount * transformTrackCount * 10)];
            float[] transformDefaults = new float[checked(transformTrackCount * 10)];
            int[] parentIndices = new int[transformTrackCount];
            for (int bone = 0; bone < source.TransformTracks.Length; bone++)
            {
                CharacterAnimationAuthoringTransformTrack track = source.TransformTracks[bone];
                WritePose(transformDefaults, bone, track.ReferencePose);
                parentIndices[bone] = track.ParentIndex;
                for (int sample = 0; sample < grid.SampleCount; sample++)
                {
                    float time = grid.SampleTime(sample);
                    AnimationLocalBonePose pose;
                    try
                    {
                        pose = track.Sample(time);
                    }
                    catch (InvalidOperationException exception)
                    {
                        throw new InvalidOperationException(
                            $"Animation Clip '{source.ClipIdentity.AssetPath}' transform track " +
                            $"'{track.Path}' (bone '{track.BoneId}') failed at sample {sample} " +
                            $"time {time:R}: {exception.Message}",
                            exception);
                    }
                    WritePose(
                        transformSamples,
                        sample * transformTrackCount + bone,
                        pose);
                }
            }

            var scalarTracks = new CharacterAnimationSampledScalarTrack[source.ScalarTracks.Length];
            var nativeTracks = new List<CharacterAnimationScalarCurveTrack>(source.ScalarTracks.Length);
            for (int trackIndex = 0; trackIndex < source.ScalarTracks.Length; trackIndex++)
            {
                CharacterAnimationAuthoringScalarTrack sourceTrack = source.ScalarTracks[trackIndex];
                var samples = new float[grid.SampleCount];
                for (int sample = 0; sample < samples.Length; sample++)
                {
                    float value = sourceTrack.IsAnimated
                        ? sourceTrack.Curve.Evaluate(grid.SampleTime(sample))
                        : sourceTrack.DefaultValue;
                    if (!float.IsFinite(value))
                        throw new InvalidOperationException(
                            $"Animation scalar curve for parameter '{sourceTrack.ParameterId}' is not finite.");
                    samples[sample] = value;
                }
                scalarTracks[trackIndex] = new CharacterAnimationSampledScalarTrack(
                    sourceTrack,
                    samples);
                nativeTracks.Add(new CharacterAnimationScalarCurveTrack(
                    sourceTrack.ParameterIndex,
                    samples));
            }
            CharacterAnimationScalarCurvePage nativeScalarPage = source.HasAnimatedPropertyChannels &&
                source.ScalarTracks.Length > 0
                ? new CharacterAnimationScalarCurvePage(
                    source.ParameterLayout.Count,
                     grid.ActualSampleRate,
                     source.DurationSeconds,
                     nativeTracks.ToArray())
                : null;
            return new CharacterAnimationSampleSet(
                source,
                grid,
                transformSamples,
                transformDefaults,
                parentIndices,
                scalarTracks,
                nativeScalarPage);
        }

        static void WritePose(
            float[] buffer,
            int trackIndex,
            AnimationLocalBonePose pose)
        {
            int offset = checked(trackIndex * 10);
            buffer[offset] = pose.Position.x;
            buffer[offset + 1] = pose.Position.y;
            buffer[offset + 2] = pose.Position.z;
            buffer[offset + 3] = pose.Rotation.x;
            buffer[offset + 4] = pose.Rotation.y;
            buffer[offset + 5] = pose.Rotation.z;
            buffer[offset + 6] = pose.Rotation.w;
            buffer[offset + 7] = pose.Scale.x;
            buffer[offset + 8] = pose.Scale.y;
            buffer[offset + 9] = pose.Scale.z;
        }
    }
}
