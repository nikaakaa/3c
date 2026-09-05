using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal enum CharacterAclNativeError : int
    {
        Success = 0,
        InvalidArgument = 1,
        InvalidPayload = 2,
        UnsupportedFormat = 3,
        UnsupportedTrackType = 4,
        Capacity = 5,
        NativeFailure = 6,
        NotReady = 7,
        InvalidStruct = 8,
        DatabaseFailure = 9,
        InvalidState = 10,
        ClipIndex = 11
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CharacterAclNativeAbiInfo
    {
        internal int StructSize;
        internal int StructVersion;
        internal int AbiVersion;
        internal int PayloadFormatVersion;
        internal int Capabilities;
        internal int TransformSampleSize;
        internal int ScalarSampleSize;
        internal int PayloadViewSize;
        internal int GroupBuildInputSize;
        internal int GroupBuildOutputSize;
        internal int GroupPayloadInputSize;
        internal int DecoderBindInputSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CharacterAclNativeTransformSample
    {
        internal float PositionX;
        internal float PositionY;
        internal float PositionZ;
        internal float RotationX;
        internal float RotationY;
        internal float RotationZ;
        internal float RotationW;
        internal float ScaleX;
        internal float ScaleY;
        internal float ScaleZ;

        internal AnimationLocalBonePose ToPose() => new AnimationLocalBonePose(
            new Vector3(PositionX, PositionY, PositionZ),
            new Quaternion(RotationX, RotationY, RotationZ, RotationW),
            new Vector3(ScaleX, ScaleY, ScaleZ));
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CharacterAclNativeTransformInput
    {
        internal int StructSize;
        internal int StructVersion;
        internal int TrackCount;
        internal int SampleCount;
        internal float SampleRate;
        internal IntPtr Samples;
        internal IntPtr DefaultValues;
        internal IntPtr ParentIndices;
        internal float Precision;
        internal float ShellDistance;
        internal byte Looping;
        internal byte Reserved0;
        internal byte Reserved1;
        internal byte Reserved2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CharacterAclNativeScalarInput
    {
        internal int StructSize;
        internal int StructVersion;
        internal int TrackCount;
        internal int SampleCount;
        internal float SampleRate;
        internal IntPtr Samples;
        internal float Precision;
        internal byte Looping;
        internal byte Reserved0;
        internal byte Reserved1;
        internal byte Reserved2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CharacterAclNativePayload
    {
        internal IntPtr Data;
        internal int Length;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CharacterAclNativePayloadView
    {
        internal int StructSize;
        internal int StructVersion;
        internal IntPtr Data;
        internal int Length;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CharacterAclNativeDatabaseSettings
    {
        internal int StructSize;
        internal int StructVersion;
        internal float MediumImportanceTierProportion;
        internal float LowImportanceTierProportion;
        internal int MaxChunkSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CharacterAclNativeGroupBuildInput
    {
        internal int StructSize;
        internal int StructVersion;
        internal int ClipCount;
        internal IntPtr TransformInputs;
        internal IntPtr ScalarInputs;
        internal CharacterAclNativeDatabaseSettings DatabaseSettings;
        internal byte EnableDatabase;
        internal byte Reserved0;
        internal byte Reserved1;
        internal byte Reserved2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CharacterAclNativeGroupBuildOutput
    {
        internal int StructSize;
        internal int StructVersion;
        internal int ClipCount;
        internal IntPtr TransformPayloads;
        internal IntPtr ScalarPayloads;
        internal CharacterAclNativePayload DatabaseHeader;
        internal CharacterAclNativePayload BulkMedium;
        internal CharacterAclNativePayload BulkLow;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CharacterAclNativeGroupPayloadInput
    {
        internal int StructSize;
        internal int StructVersion;
        internal int ClipCount;
        internal IntPtr TransformPayloads;
        internal IntPtr ScalarPayloads;
        internal CharacterAclNativePayloadView DatabaseHeader;
        internal CharacterAclNativePayloadView BulkMedium;
        internal CharacterAclNativePayloadView BulkLow;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CharacterAclNativeDecoderBindInput
    {
        internal int StructSize;
        internal int StructVersion;
        internal IntPtr Group;
        internal int ClipIndex;
        internal int Reserved;
    }

    internal readonly struct CharacterAclNativeGroupBuildClip
    {
        internal CharacterAclNativeGroupBuildClip(
            float[] transformSamples,
            float[] transformDefaults,
            int[] parentIndices,
            int transformTrackCount,
            int sampleCount,
            float sampleRate,
            float transformPrecision,
            float shellDistance,
            bool looping,
            float[] scalarSamples,
            int scalarTrackCount,
            float scalarPrecision)
        {
            TransformSamples = transformSamples ?? throw new ArgumentNullException(nameof(transformSamples));
            TransformDefaults = transformDefaults ?? throw new ArgumentNullException(nameof(transformDefaults));
            ParentIndices = parentIndices ?? throw new ArgumentNullException(nameof(parentIndices));
            ScalarSamples = scalarSamples ?? Array.Empty<float>();
            TransformTrackCount = transformTrackCount;
            SampleCount = sampleCount;
            SampleRate = sampleRate;
            TransformPrecision = transformPrecision;
            ShellDistance = shellDistance;
            Looping = looping;
            ScalarTrackCount = scalarTrackCount;
            ScalarPrecision = scalarPrecision;
        }

        internal float[] TransformSamples { get; }
        internal float[] TransformDefaults { get; }
        internal int[] ParentIndices { get; }
        internal int TransformTrackCount { get; }
        internal int SampleCount { get; }
        internal float SampleRate { get; }
        internal float TransformPrecision { get; }
        internal float ShellDistance { get; }
        internal bool Looping { get; }
        internal float[] ScalarSamples { get; }
        internal int ScalarTrackCount { get; }
        internal float ScalarPrecision { get; }
    }
}
