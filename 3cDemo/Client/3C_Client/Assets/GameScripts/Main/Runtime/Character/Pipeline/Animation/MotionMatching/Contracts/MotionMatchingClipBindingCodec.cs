using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    internal static class MotionMatchingClipBindingCodec
    {
        internal static void Write(
            BinaryWriter writer,
            MotionMatchingClipBindingPayload value,
            List<AnimationClip> nativeClipTable)
        {
            value.RequireValid();
            writer.Write(value.SourceClipId.Value);
            writer.Write(value.AssetGuid);
            writer.Write(value.LocalFileId);
            writer.Write(value.DurationSeconds);
            writer.Write(value.IsLooping);
            writer.Write(value.RootLocked);
            writer.Write((byte)value.Backend);
            writer.Write(value.ResourceCatalogIndex);
            writer.Write(value.GroupClipIndex);
            int clipIndex = -1;
            if (value.Backend == CharacterAnimationSamplingBackendKind.NativeClip)
            {
                clipIndex = nativeClipTable.IndexOf(value.Clip);
                if (clipIndex < 0)
                {
                    clipIndex = nativeClipTable.Count;
                    nativeClipTable.Add(value.Clip);
                }
            }
            writer.Write(clipIndex);
            MotionMatchingPoseParameterCurvePayload curve =
                value.FootPlacementWeightCurve;
            writer.Write(curve.ParameterId.Value);
            writer.Write(curve.KeyCount);
            for (int i = 0; i < curve.KeyCount; i++)
            {
                writer.Write(curve.GetNormalizedTime(i));
                writer.Write(curve.GetValue(i));
            }
            bool hasScalarPage = value.NativeScalarPage != null;
            writer.Write(hasScalarPage);
            if (hasScalarPage)
                WriteScalarPage(writer, value.NativeScalarPage);
        }

        internal static MotionMatchingClipBindingPayload Read(
            BinaryReader reader,
            AnimationClip[] nativeClipTable)
        {
            var sourceClipId = new CharacterMotionMatchingSourceClipId(
                reader.ReadString());
            string assetGuid = reader.ReadString();
            long localFileId = reader.ReadInt64();
            float durationSeconds = reader.ReadSingle();
            bool isLooping = reader.ReadBoolean();
            bool rootLocked = reader.ReadBoolean();
            CharacterAnimationSamplingBackendKind backend =
                (CharacterAnimationSamplingBackendKind)reader.ReadByte();
            if (!Enum.IsDefined(
                    typeof(CharacterAnimationSamplingBackendKind),
                    backend))
                throw new InvalidOperationException(
                    "Motion Matching Projection contains an unknown Clip binding backend.");
            int resourceCatalogIndex = reader.ReadInt32();
            int groupClipIndex = reader.ReadInt32();
            int clipIndex = reader.ReadInt32();
            AnimationClip clip = null;
            if (backend == CharacterAnimationSamplingBackendKind.NativeClip)
            {
                if (nativeClipTable == null ||
                    (uint)clipIndex >= (uint)nativeClipTable.Length ||
                    !nativeClipTable[clipIndex])
                    throw new InvalidOperationException(
                        $"Motion Matching Projection native Clip reference #{clipIndex} is missing.");
                clip = nativeClipTable[clipIndex];
                if (resourceCatalogIndex != -1 || groupClipIndex != -1 ||
                    durationSeconds != clip.length ||
                    isLooping != clip.isLooping)
                    throw new InvalidOperationException(
                        "Motion Matching Projection native Clip binding metadata is inconsistent.");
            }
            else if (clipIndex != -1)
            {
                throw new InvalidOperationException(
                    "Motion Matching Projection ACL Clip binding contains a native Clip reference.");
            }
            var parameterId = new PoseParameterId(reader.ReadString());
            int curveCount = RequireCount(
                reader.ReadInt32(),
                "parameter curve key",
                false);
            var times = new float[curveCount];
            var values = new float[curveCount];
            for (int i = 0; i < curveCount; i++)
            {
                times[i] = reader.ReadSingle();
                values[i] = reader.ReadSingle();
            }
            CharacterAnimationScalarCurvePage nativeScalarPage = null;
            if (reader.ReadBoolean())
            {
                if (backend != CharacterAnimationSamplingBackendKind.NativeClip)
                    throw new InvalidOperationException(
                        "Motion Matching Projection ACL Clip binding contains a scalar page.");
                nativeScalarPage = ReadScalarPage(reader);
            }
            MotionMatchingPoseParameterCurvePayload footCurve =
                new MotionMatchingPoseParameterCurvePayload(
                    parameterId,
                    times,
                    values);
            return backend == CharacterAnimationSamplingBackendKind.NativeClip
                ? MotionMatchingClipBindingPayload.CreateNative(
                    sourceClipId,
                    assetGuid,
                    localFileId,
                    clip,
                    rootLocked,
                    footCurve,
                    nativeScalarPage)
                : MotionMatchingClipBindingPayload.CreateAcl(
                    sourceClipId,
                    assetGuid,
                    localFileId,
                    rootLocked,
                    footCurve,
                    durationSeconds,
                    isLooping,
                    resourceCatalogIndex,
                    groupClipIndex);
        }

        static void WriteScalarPage(
            BinaryWriter writer,
            CharacterAnimationScalarCurvePage page)
        {
            page.RequireValid();
            writer.Write(page.ParameterCount);
            writer.Write(page.SampleRate);
            writer.Write(page.DurationSeconds);
            writer.Write(page.Tracks.Count);
            for (int i = 0; i < page.Tracks.Count; i++)
            {
                CharacterAnimationScalarCurveTrack track = page.Tracks[i];
                track.RequireValid();
                writer.Write(track.ParameterIndex);
                writer.Write(track.Samples.Count);
                for (int sampleIndex = 0;
                     sampleIndex < track.Samples.Count;
                     sampleIndex++)
                    writer.Write(track.Samples[sampleIndex]);
            }
        }

        static CharacterAnimationScalarCurvePage ReadScalarPage(
            BinaryReader reader)
        {
            int parameterCount = RequireCount(
                reader.ReadInt32(),
                "scalar page parameter",
                false);
            float sampleRate = reader.ReadSingle();
            float durationSeconds = reader.ReadSingle();
            int trackCount = RequireCount(
                reader.ReadInt32(),
                "scalar page track",
                true);
            var tracks = new CharacterAnimationScalarCurveTrack[trackCount];
            for (int i = 0; i < tracks.Length; i++)
            {
                int parameterIndex = reader.ReadInt32();
                int sampleCount = RequireCount(
                    reader.ReadInt32(),
                    "scalar page sample",
                    false);
                var samples = new float[sampleCount];
                for (int sampleIndex = 0;
                     sampleIndex < samples.Length;
                     sampleIndex++)
                    samples[sampleIndex] = reader.ReadSingle();
                tracks[i] = new CharacterAnimationScalarCurveTrack(
                    parameterIndex,
                    samples);
            }
            return new CharacterAnimationScalarCurvePage(
                parameterCount,
                sampleRate,
                durationSeconds,
                tracks);
        }

        static int RequireCount(int value, string name, bool allowZero)
        {
            if (value < 0 || !allowZero && value == 0)
                throw new InvalidOperationException(
                    $"Motion Matching Projection {name} count is invalid.");
            return value;
        }
    }
}
