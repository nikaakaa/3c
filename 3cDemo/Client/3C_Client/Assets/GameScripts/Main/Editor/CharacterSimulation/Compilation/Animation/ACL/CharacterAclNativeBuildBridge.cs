using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL
{
    internal static class CharacterAclNativeBuildBridge
    {
        const string LibraryName = "3c_acl_runtime";
        const int StructVersion = 1;

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern CharacterAclNativeError acl_project_group_build(
            ref CharacterAclNativeGroupBuildInput input,
            out IntPtr build);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern CharacterAclNativeError acl_project_group_build_get_output(
            IntPtr build,
            ref CharacterAclNativeGroupBuildOutput output);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern CharacterAclNativeError acl_project_group_build_release(
            IntPtr build);

        internal static CharacterAclNativeError BuildGroup(
            IReadOnlyList<CharacterAclNativeGroupBuildClip> clips,
            bool enableDatabase,
            float mediumImportanceTierProportion,
            float lowImportanceTierProportion,
            int maxChunkSize,
            out byte[][] transformPayloads,
            out byte[][] scalarPayloads,
            out byte[] databaseHeaderPayload,
            out byte[] bulkMediumPayload,
            out byte[] bulkLowPayload)
        {
            transformPayloads = null;
            scalarPayloads = null;
            databaseHeaderPayload = Array.Empty<byte>();
            bulkMediumPayload = Array.Empty<byte>();
            bulkLowPayload = Array.Empty<byte>();
            if (clips == null || clips.Count == 0)
                return CharacterAclNativeError.InvalidArgument;
            int clipCount = clips.Count;
            var transformInputs = new CharacterAclNativeTransformInput[clipCount];
            var scalarInputs = new CharacterAclNativeScalarInput[clipCount];
            var transformSampleHandles = new GCHandle[clipCount];
            var transformDefaultHandles = new GCHandle[clipCount];
            var parentIndexHandles = new GCHandle[clipCount];
            var scalarSampleHandles = new GCHandle[clipCount];
            GCHandle transformInputsHandle = default;
            GCHandle scalarInputsHandle = default;
            IntPtr build = IntPtr.Zero;
            try
            {
                for (int i = 0; i < clipCount; i++)
                {
                    CharacterAclNativeGroupBuildClip clip = clips[i];
                    if (clip.TransformTrackCount <= 0 || clip.SampleCount < 2 ||
                        !float.IsFinite(clip.SampleRate) || clip.SampleRate <= 0f ||
                        !float.IsFinite(clip.TransformPrecision) || clip.TransformPrecision < 0f ||
                        !float.IsFinite(clip.ShellDistance) || clip.ShellDistance < 0f ||
                        clip.TransformSamples.Length != checked(clip.TransformTrackCount * clip.SampleCount * 10) ||
                        clip.TransformDefaults.Length != checked(clip.TransformTrackCount * 10) ||
                        clip.ParentIndices.Length != clip.TransformTrackCount ||
                        clip.ScalarTrackCount < 0 ||
                        clip.ScalarTrackCount > 0 &&
                        clip.ScalarSamples.Length != checked(clip.ScalarTrackCount * clip.SampleCount))
                        return CharacterAclNativeError.InvalidArgument;
                    transformSampleHandles[i] = GCHandle.Alloc(
                        clip.TransformSamples,
                        GCHandleType.Pinned);
                    transformDefaultHandles[i] = GCHandle.Alloc(
                        clip.TransformDefaults,
                        GCHandleType.Pinned);
                    parentIndexHandles[i] = GCHandle.Alloc(
                        clip.ParentIndices,
                        GCHandleType.Pinned);
                    transformInputs[i] = new CharacterAclNativeTransformInput
                    {
                        StructSize = SizeOf<CharacterAclNativeTransformInput>(),
                        StructVersion = StructVersion,
                        TrackCount = clip.TransformTrackCount,
                        SampleCount = clip.SampleCount,
                        SampleRate = clip.SampleRate,
                        Samples = transformSampleHandles[i].AddrOfPinnedObject(),
                        DefaultValues = transformDefaultHandles[i].AddrOfPinnedObject(),
                        ParentIndices = parentIndexHandles[i].AddrOfPinnedObject(),
                        Precision = clip.TransformPrecision,
                        ShellDistance = clip.ShellDistance,
                        Looping = clip.Looping ? (byte)1 : (byte)0
                    };
                    scalarInputs[i] = new CharacterAclNativeScalarInput
                    {
                        StructSize = SizeOf<CharacterAclNativeScalarInput>(),
                        StructVersion = StructVersion,
                        TrackCount = clip.ScalarTrackCount,
                        SampleCount = clip.SampleCount,
                        SampleRate = clip.SampleRate,
                        Precision = clip.ScalarPrecision,
                        Looping = clip.Looping ? (byte)1 : (byte)0
                    };
                    if (clip.ScalarTrackCount > 0)
                    {
                        scalarSampleHandles[i] = GCHandle.Alloc(
                            clip.ScalarSamples,
                            GCHandleType.Pinned);
                        scalarInputs[i].Samples =
                            scalarSampleHandles[i].AddrOfPinnedObject();
                    }
                }
                transformInputsHandle = GCHandle.Alloc(
                    transformInputs,
                    GCHandleType.Pinned);
                scalarInputsHandle = GCHandle.Alloc(
                    scalarInputs,
                    GCHandleType.Pinned);
                var input = new CharacterAclNativeGroupBuildInput
                {
                    StructSize = SizeOf<CharacterAclNativeGroupBuildInput>(),
                    StructVersion = StructVersion,
                    ClipCount = clipCount,
                    TransformInputs = transformInputsHandle.AddrOfPinnedObject(),
                    ScalarInputs = scalarInputsHandle.AddrOfPinnedObject(),
                    DatabaseSettings = new CharacterAclNativeDatabaseSettings
                    {
                        StructSize = SizeOf<CharacterAclNativeDatabaseSettings>(),
                        StructVersion = StructVersion,
                        MediumImportanceTierProportion = mediumImportanceTierProportion,
                        LowImportanceTierProportion = lowImportanceTierProportion,
                        MaxChunkSize = maxChunkSize
                    },
                    EnableDatabase = enableDatabase ? (byte)1 : (byte)0
                };
                CharacterAclNativeError error = acl_project_group_build(
                    ref input,
                    out build);
                if (error != CharacterAclNativeError.Success)
                    return error;
                var output = new CharacterAclNativeGroupBuildOutput
                {
                    StructSize = SizeOf<CharacterAclNativeGroupBuildOutput>(),
                    StructVersion = StructVersion
                };
                error = acl_project_group_build_get_output(
                    build,
                    ref output);
                if (error != CharacterAclNativeError.Success ||
                    output.ClipCount != clipCount)
                    return error == CharacterAclNativeError.Success
                        ? CharacterAclNativeError.InvalidPayload
                        : error;
                transformPayloads = new byte[clipCount][];
                scalarPayloads = new byte[clipCount][];
                for (int i = 0; i < clipCount; i++)
                {
                    transformPayloads[i] = CopyPayload(
                        ReadPayload(output.TransformPayloads, i));
                    scalarPayloads[i] = CopyPayload(
                        ReadPayload(output.ScalarPayloads, i));
                    if (transformPayloads[i] == null || scalarPayloads[i] == null)
                        return CharacterAclNativeError.InvalidPayload;
                }
                databaseHeaderPayload = CopyPayload(output.DatabaseHeader);
                bulkMediumPayload = CopyPayload(output.BulkMedium);
                bulkLowPayload = CopyPayload(output.BulkLow);
                if (databaseHeaderPayload == null ||
                    bulkMediumPayload == null ||
                    bulkLowPayload == null)
                    return CharacterAclNativeError.InvalidPayload;
                return CharacterAclNativeError.Success;
            }
            catch (DllNotFoundException)
            {
                return CharacterAclNativeError.NativeFailure;
            }
            catch (EntryPointNotFoundException)
            {
                return CharacterAclNativeError.NativeFailure;
            }
            finally
            {
                if (build != IntPtr.Zero)
                    acl_project_group_build_release(build);
                if (scalarInputsHandle.IsAllocated)
                    scalarInputsHandle.Free();
                if (transformInputsHandle.IsAllocated)
                    transformInputsHandle.Free();
                for (int i = 0; i < clipCount; i++)
                {
                    if (scalarSampleHandles[i].IsAllocated)
                        scalarSampleHandles[i].Free();
                    if (parentIndexHandles[i].IsAllocated)
                        parentIndexHandles[i].Free();
                    if (transformDefaultHandles[i].IsAllocated)
                        transformDefaultHandles[i].Free();
                    if (transformSampleHandles[i].IsAllocated)
                        transformSampleHandles[i].Free();
                }
            }
        }

        static CharacterAclNativePayload ReadPayload(IntPtr payloads, int index)
        {
            if (payloads == IntPtr.Zero || index < 0)
                return default;
            int stride = SizeOf<CharacterAclNativePayload>();
            return Marshal.PtrToStructure<CharacterAclNativePayload>(
                IntPtr.Add(payloads, checked(index * stride)));
        }

        static byte[] CopyPayload(CharacterAclNativePayload payload)
        {
            if (payload.Length == 0)
                return Array.Empty<byte>();
            if (payload.Data == IntPtr.Zero || payload.Length < 0)
                return null;
            var result = new byte[payload.Length];
            Marshal.Copy(payload.Data, result, 0, result.Length);
            return result;
        }

        static int SizeOf<T>() => Marshal.SizeOf(typeof(T));
    }
}
