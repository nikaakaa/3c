using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static unsafe class CharacterAclNativeBridge
    {
        const string LibraryName = "3c_acl_runtime";
        const int StructVersion = 1;

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern CharacterAclNativeError acl_project_get_abi(
            ref CharacterAclNativeAbiInfo info);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern CharacterAclNativeError acl_project_group_create(
            ref CharacterAclNativeGroupPayloadInput input,
            out IntPtr group);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern CharacterAclNativeError acl_project_group_get_clip_count(
            IntPtr group,
            out int clipCount);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern CharacterAclNativeError acl_project_group_release(
            IntPtr group);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern CharacterAclNativeError acl_project_decoder_create(
            out IntPtr decoder);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern CharacterAclNativeError acl_project_decoder_bind(
            IntPtr decoder,
            IntPtr group,
            ref CharacterAclNativeDecoderBindInput input);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern CharacterAclNativeError acl_project_decoder_unbind(
            IntPtr decoder);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern CharacterAclNativeError acl_project_decoder_sample_transform(
            IntPtr decoder,
            float sampleTime,
            CharacterAclNativeTransformSample* output,
            int outputCount);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern CharacterAclNativeError acl_project_decoder_sample_scalar(
            IntPtr decoder,
            float sampleTime,
            float* output,
            int outputCount);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern CharacterAclNativeError acl_project_decoder_destroy(
            IntPtr decoder);

        internal static bool TryGetAbi(out CharacterAclNativeAbiInfo info)
        {
            info = new CharacterAclNativeAbiInfo
            {
                StructSize = SizeOf<CharacterAclNativeAbiInfo>(),
                StructVersion = StructVersion
            };
            try
            {
                return acl_project_get_abi(ref info) == CharacterAclNativeError.Success &&
                    info.AbiVersion == 2 &&
                    info.PayloadFormatVersion == CharacterAclAnimationResourceManifest.PayloadFormatVersion &&
                    info.TransformSampleSize == SizeOf<CharacterAclNativeTransformSample>();
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }

        internal static CharacterAclNativeError GroupCreate(
            ref CharacterAclNativeGroupPayloadInput input,
            out IntPtr group)
        {
            try
            {
                return acl_project_group_create(ref input, out group);
            }
            catch (DllNotFoundException)
            {
                group = IntPtr.Zero;
                return CharacterAclNativeError.NativeFailure;
            }
            catch (EntryPointNotFoundException)
            {
                group = IntPtr.Zero;
                return CharacterAclNativeError.NativeFailure;
            }
        }

        internal static CharacterAclNativeError GroupCreate(
            byte[][] transformPayloads,
            byte[][] scalarPayloads,
            byte[] databaseHeaderPayload,
            byte[] bulkMediumPayload,
            byte[] bulkLowPayload,
            out IntPtr group)
        {
            group = IntPtr.Zero;
            if (transformPayloads == null || transformPayloads.Length == 0 ||
                scalarPayloads == null || scalarPayloads.Length != transformPayloads.Length ||
                databaseHeaderPayload == null || bulkMediumPayload == null ||
                bulkLowPayload == null)
                return CharacterAclNativeError.InvalidArgument;
            for (int i = 0; i < transformPayloads.Length; i++)
            {
                if (transformPayloads[i] == null || transformPayloads[i].Length == 0 ||
                    scalarPayloads[i] == null)
                    return CharacterAclNativeError.InvalidArgument;
            }
            var transformViews = new CharacterAclNativePayloadView[transformPayloads.Length];
            var scalarViews = new CharacterAclNativePayloadView[scalarPayloads.Length];
            var transformHandles = new GCHandle[transformPayloads.Length];
            var scalarHandles = new GCHandle[scalarPayloads.Length];
            GCHandle databaseHandle = default;
            GCHandle mediumHandle = default;
            GCHandle lowHandle = default;
            GCHandle transformViewsHandle = default;
            GCHandle scalarViewsHandle = default;
            try
            {
                for (int i = 0; i < transformPayloads.Length; i++)
                {
                    transformViews[i] = PinPayload(
                        transformPayloads[i],
                        ref transformHandles[i]);
                    scalarViews[i] = PinPayload(
                        scalarPayloads[i],
                        ref scalarHandles[i]);
                }
                CharacterAclNativePayloadView databaseView = PinPayload(
                    databaseHeaderPayload,
                    ref databaseHandle);
                CharacterAclNativePayloadView mediumView = PinPayload(
                    bulkMediumPayload,
                    ref mediumHandle);
                CharacterAclNativePayloadView lowView = PinPayload(
                    bulkLowPayload,
                    ref lowHandle);
                transformViewsHandle = GCHandle.Alloc(
                    transformViews,
                    GCHandleType.Pinned);
                scalarViewsHandle = GCHandle.Alloc(
                    scalarViews,
                    GCHandleType.Pinned);
                var input = new CharacterAclNativeGroupPayloadInput
                {
                    StructSize = SizeOf<CharacterAclNativeGroupPayloadInput>(),
                    StructVersion = StructVersion,
                    ClipCount = transformPayloads.Length,
                    TransformPayloads = transformViewsHandle.AddrOfPinnedObject(),
                    ScalarPayloads = scalarViewsHandle.AddrOfPinnedObject(),
                    DatabaseHeader = databaseView,
                    BulkMedium = mediumView,
                    BulkLow = lowView
                };
                return GroupCreate(ref input, out group);
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
                if (transformViewsHandle.IsAllocated)
                    transformViewsHandle.Free();
                if (scalarViewsHandle.IsAllocated)
                    scalarViewsHandle.Free();
                if (lowHandle.IsAllocated)
                    lowHandle.Free();
                if (mediumHandle.IsAllocated)
                    mediumHandle.Free();
                if (databaseHandle.IsAllocated)
                    databaseHandle.Free();
                for (int i = 0; i < scalarHandles.Length; i++)
                {
                    if (scalarHandles[i].IsAllocated)
                        scalarHandles[i].Free();
                    if (transformHandles[i].IsAllocated)
                        transformHandles[i].Free();
                }
            }
        }

        internal static CharacterAclNativeError GroupGetClipCount(
            IntPtr group,
            out int clipCount) =>
            acl_project_group_get_clip_count(group, out clipCount);

        internal static CharacterAclNativeError GroupRelease(IntPtr group) =>
            acl_project_group_release(group);

        internal static CharacterAclNativeError DecoderCreate(
            out IntPtr decoder) =>
            acl_project_decoder_create(out decoder);

        internal static CharacterAclNativeError DecoderBind(
            IntPtr decoder,
            IntPtr group,
            ref CharacterAclNativeDecoderBindInput input) =>
            acl_project_decoder_bind(decoder, group, ref input);

        internal static CharacterAclNativeError DecoderUnbind(IntPtr decoder) =>
            acl_project_decoder_unbind(decoder);

        internal static CharacterAclNativeError DecoderSampleTransform(
            IntPtr decoder,
            float sampleTime,
            CharacterAclNativeTransformSample* output,
            int outputCount) =>
            acl_project_decoder_sample_transform(decoder, sampleTime, output, outputCount);

        internal static CharacterAclNativeError DecoderSampleScalar(
            IntPtr decoder,
            float sampleTime,
            float* output,
            int outputCount) =>
            acl_project_decoder_sample_scalar(decoder, sampleTime, output, outputCount);

        internal static CharacterAclNativeError DecoderDestroy(IntPtr decoder) =>
            acl_project_decoder_destroy(decoder);

        static CharacterAclNativePayloadView PinPayload(
            byte[] payload,
            ref GCHandle handle)
        {
            if (payload.Length == 0)
                return new CharacterAclNativePayloadView
                {
                    StructSize = SizeOf<CharacterAclNativePayloadView>(),
                    StructVersion = StructVersion,
                    Data = IntPtr.Zero,
                    Length = 0
                };
            handle = GCHandle.Alloc(payload, GCHandleType.Pinned);
            return new CharacterAclNativePayloadView
            {
                StructSize = SizeOf<CharacterAclNativePayloadView>(),
                StructVersion = StructVersion,
                Data = handle.AddrOfPinnedObject(),
                Length = payload.Length
            };
        }

        static int SizeOf<T>() => Marshal.SizeOf(typeof(T));
    }
}
