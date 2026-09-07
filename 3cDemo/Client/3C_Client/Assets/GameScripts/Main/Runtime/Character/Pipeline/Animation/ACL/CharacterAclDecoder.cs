using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed unsafe class CharacterAclDecoder : IDisposable
    {
        const int StructVersion = 1;
        int m_TransformTrackCount;
        int m_ScalarTrackCount;
        IntPtr m_Decoder;
        IntPtr m_BoundGroup;
        IntPtr m_OwnedGroup;

        internal CharacterAclDecoder()
        {
            CreateDecoder();
        }

        internal CharacterAclDecoder(
            int transformTrackCount,
            int scalarTrackCount)
        {
            if (transformTrackCount <= 0 || scalarTrackCount < 0)
                throw new ArgumentException("ACL decoder track counts are invalid.");
            CreateDecoder();
            m_TransformTrackCount = transformTrackCount;
            m_ScalarTrackCount = scalarTrackCount;
        }

        void CreateDecoder()
        {
            CharacterAclNativeError error = CharacterAclNativeBridge.DecoderCreate(
                out IntPtr decoder);
            if (error == CharacterAclNativeError.Success && decoder != IntPtr.Zero)
            {
                m_Decoder = decoder;
                return;
            }
            Exception failure = new InvalidOperationException(
                $"ACL decoder creation failed: {error}.");
            if (decoder != IntPtr.Zero)
            {
                CharacterAclNativeError cleanupError =
                    CharacterAclNativeBridge.DecoderDestroy(decoder);
                if (cleanupError != CharacterAclNativeError.Success)
                    throw new AggregateException(
                        "ACL decoder creation cleanup failed.",
                        failure,
                        new InvalidOperationException(
                            $"ACL decoder cleanup failed: {cleanupError}."));
            }
            throw failure;
        }

        internal int TransformTrackCount => m_TransformTrackCount;
        internal int ScalarTrackCount => m_ScalarTrackCount;
        internal bool IsValid =>
            m_Decoder != IntPtr.Zero && m_BoundGroup != IntPtr.Zero;

        internal void Bind(
            IntPtr group,
            int clipIndex,
            int transformTrackCount,
            int scalarTrackCount)
        {
            if (transformTrackCount <= 0 || scalarTrackCount < 0)
                throw new ArgumentException("ACL decoder track counts are invalid.");
            BindCore(group, clipIndex);
            m_TransformTrackCount = transformTrackCount;
            m_ScalarTrackCount = scalarTrackCount;
        }

        internal void Bind(IntPtr group, int clipIndex)
        {
            if (m_TransformTrackCount <= 0 || m_ScalarTrackCount < 0)
                throw new InvalidOperationException("ACL decoder track counts are not configured.");
            BindCore(group, clipIndex);
        }

        void BindCore(IntPtr group, int clipIndex)
        {
            if (m_Decoder == IntPtr.Zero || group == IntPtr.Zero || clipIndex < 0 ||
                m_BoundGroup != IntPtr.Zero)
                throw new InvalidOperationException("ACL decoder bind state is invalid.");
            var input = new CharacterAclNativeDecoderBindInput
            {
                StructSize = Marshal.SizeOf(typeof(CharacterAclNativeDecoderBindInput)),
                StructVersion = StructVersion,
                Group = group,
                ClipIndex = clipIndex,
                Reserved = 0
            };
            CharacterAclNativeError error = CharacterAclNativeBridge.DecoderBind(
                m_Decoder,
                group,
                ref input);
            if (error != CharacterAclNativeError.Success)
                throw new InvalidOperationException($"ACL decoder binding failed: {error}.");
            m_BoundGroup = group;
        }

        internal void Unbind()
        {
            if (m_BoundGroup == IntPtr.Zero)
                return;
            if (m_Decoder == IntPtr.Zero)
                throw new InvalidOperationException("ACL decoder binding has no native decoder.");
            CharacterAclNativeError error =
                CharacterAclNativeBridge.DecoderUnbind(m_Decoder);
            if (error != CharacterAclNativeError.Success)
                throw new InvalidOperationException($"ACL decoder unbinding failed: {error}.");
            m_BoundGroup = IntPtr.Zero;
            m_TransformTrackCount = 0;
            m_ScalarTrackCount = 0;
        }

        internal static CharacterAclDecoder CreateGroup(
            byte[][] transformPayloads,
            byte[][] scalarPayloads,
            byte[] databaseHeaderPayload,
            byte[] bulkMediumPayload,
            byte[] bulkLowPayload,
            int transformTrackCount,
            int scalarTrackCount,
            int clipIndex)
        {
            IntPtr group = IntPtr.Zero;
            CharacterAclDecoder decoder = null;
            try
            {
                CharacterAclNativeError error = CharacterAclNativeBridge.GroupCreate(
                    transformPayloads,
                    scalarPayloads,
                    databaseHeaderPayload,
                    bulkMediumPayload,
                    bulkLowPayload,
                    out group);
                if (error != CharacterAclNativeError.Success || group == IntPtr.Zero)
                    throw new InvalidOperationException($"ACL group creation failed: {error}.");
                decoder = new CharacterAclDecoder(
                    transformTrackCount,
                    scalarTrackCount);
                decoder.Bind(group, clipIndex);
                decoder.m_OwnedGroup = group;
                group = IntPtr.Zero;
                return decoder;
            }
            catch (Exception exception)
            {
                Exception cleanupFailure = null;
                if (decoder != null)
                {
                    try
                    {
                        decoder.Dispose();
                    }
                    catch (Exception cleanupException)
                    {
                        RecordFailure(ref cleanupFailure, cleanupException);
                    }
                }
                if (group != IntPtr.Zero)
                {
                    try
                    {
                        CharacterAclNativeError cleanupError =
                            CharacterAclNativeBridge.GroupRelease(group);
                        if (cleanupError != CharacterAclNativeError.Success)
                            throw new InvalidOperationException(
                                $"ACL group cleanup failed: {cleanupError}.");
                    }
                    catch (Exception cleanupException)
                    {
                        RecordFailure(ref cleanupFailure, cleanupException);
                    }
                }
                if (cleanupFailure != null)
                    throw new AggregateException(
                        "ACL group creation cleanup failed.",
                        exception,
                        cleanupFailure);
                throw;
            }
        }

        internal void SampleTransforms(
            float sampleTime,
            NativeArray<CharacterAclNativeTransformSample> output)
        {
            RequireBound();
            if (!output.IsCreated || output.Length != m_TransformTrackCount)
                throw new InvalidOperationException("ACL transform decoder output is invalid.");
            CharacterAclNativeError error = CharacterAclNativeBridge.DecoderSampleTransform(
                m_Decoder,
                sampleTime,
                (CharacterAclNativeTransformSample*)NativeArrayUnsafeUtility.GetUnsafePtr(output),
                output.Length);
            if (error != CharacterAclNativeError.Success)
                throw new InvalidOperationException($"ACL transform decoder sampling failed: {error}.");
        }

        internal void SampleScalars(
            float sampleTime,
            NativeArray<float> output)
        {
            RequireBound();
            if (m_ScalarTrackCount == 0)
                return;
            if (!output.IsCreated || output.Length < m_ScalarTrackCount)
                throw new InvalidOperationException("ACL scalar decoder output is invalid.");
            CharacterAclNativeError error = CharacterAclNativeBridge.DecoderSampleScalar(
                m_Decoder,
                sampleTime,
                (float*)NativeArrayUnsafeUtility.GetUnsafePtr(output),
                m_ScalarTrackCount);
            if (error != CharacterAclNativeError.Success)
                throw new InvalidOperationException($"ACL scalar decoder sampling failed: {error}.");
        }

        public void Dispose()
        {
            Unbind();
            if (m_Decoder != IntPtr.Zero)
            {
                CharacterAclNativeError error =
                    CharacterAclNativeBridge.DecoderDestroy(m_Decoder);
                if (error != CharacterAclNativeError.Success)
                    throw new InvalidOperationException(
                        $"ACL decoder destruction failed: {error}.");
                m_Decoder = IntPtr.Zero;
                m_TransformTrackCount = 0;
                m_ScalarTrackCount = 0;
            }
            if (m_OwnedGroup != IntPtr.Zero)
            {
                CharacterAclNativeError error =
                    CharacterAclNativeBridge.GroupRelease(m_OwnedGroup);
                if (error != CharacterAclNativeError.Success)
                    throw new InvalidOperationException(
                        $"ACL owned group release failed: {error}.");
                m_OwnedGroup = IntPtr.Zero;
            }
        }

        void RequireBound()
        {
            if (!IsValid)
                throw new InvalidOperationException("ACL decoder is not bound to a native group.");
        }

        static void RecordFailure(ref Exception failure, Exception exception)
        {
            failure = failure == null
                ? exception
                : new AggregateException(failure, exception);
        }
    }
}
