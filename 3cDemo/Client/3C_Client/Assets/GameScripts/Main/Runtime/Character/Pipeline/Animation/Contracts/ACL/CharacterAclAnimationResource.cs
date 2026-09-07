using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [CreateAssetMenu(fileName = "CharacterAclAnimationResource", menuName = "3C/Character/ACL Animation Resource")]
    public sealed class CharacterAclAnimationResource : ScriptableObject
    {
        [SerializeField] CharacterAclAnimationResourceManifest m_Manifest;
        [SerializeField] CharacterAclAnimationResourceManifest[] m_GroupManifests = Array.Empty<CharacterAclAnimationResourceManifest>();
        [SerializeField] TextAsset[] m_GroupTransformPayloads = Array.Empty<TextAsset>();
        [SerializeField] TextAsset[] m_GroupScalarPayloads = Array.Empty<TextAsset>();
        [SerializeField] TextAsset m_DatabaseHeaderPayload;
        [SerializeField] TextAsset m_BulkMediumPayload;
        [SerializeField] TextAsset m_BulkLowPayload;

        public CharacterAclAnimationResourceManifest Manifest => m_Manifest;
        public TextAsset DatabaseHeaderPayload => m_DatabaseHeaderPayload;
        public TextAsset BulkMediumPayload => m_BulkMediumPayload;
        public TextAsset BulkLowPayload => m_BulkLowPayload;
        public string ResourceIdentity => Manifest?.ResourceIdentity ?? string.Empty;

        internal bool MatchesDescriptor(
            CharacterAnimationCompiledResourceDescriptor descriptor)
        {
            if (descriptor == null)
                return false;
            descriptor.RequireValid();
            if (Manifest == null ||
                !string.Equals(ResourceIdentity, descriptor.RequireManifest(0).ResourceIdentity, StringComparison.Ordinal) ||
                !string.Equals(Manifest.ResourceAddress, descriptor.ResourceAddress, StringComparison.Ordinal) ||
                !string.Equals(Manifest.GroupContentHash, descriptor.GroupContentHash, StringComparison.Ordinal) ||
                descriptor.ClipManifests.Count != GroupClipCount)
            {
                return false;
            }
            for (int i = 0; i < GroupClipCount; i++)
            {
                CharacterAclAnimationResourceManifest actual = GetGroupManifest(i);
                CharacterAclAnimationResourceManifest expected = descriptor.RequireManifest(i);
                if (!string.Equals(actual.ContentHash, expected.ContentHash, StringComparison.Ordinal) ||
                    !string.Equals(actual.FormalClipIdentity, expected.FormalClipIdentity, StringComparison.Ordinal) ||
                    !string.Equals(actual.BindingHash, expected.BindingHash, StringComparison.Ordinal) ||
                    !string.Equals(actual.NativeArtifactIdentity, expected.NativeArtifactIdentity, StringComparison.Ordinal) ||
                    !string.Equals(actual.NativeBinarySha256, expected.NativeBinarySha256, StringComparison.Ordinal) ||
                    !string.Equals(actual.NativePlatform, expected.NativePlatform, StringComparison.Ordinal) ||
                    actual.NativeAbiVersion != expected.NativeAbiVersion ||
                    actual.NativePayloadFormatVersion != expected.NativePayloadFormatVersion)
                {
                    return false;
                }
            }
            return true;
        }

        internal int GroupClipCount => m_GroupManifests?.Length ?? 0;

        internal CharacterAclAnimationResourceManifest GetGroupManifest(
            int groupClipIndex)
        {
            if ((uint)groupClipIndex >= (uint)GroupClipCount)
                throw new ArgumentOutOfRangeException(nameof(groupClipIndex));
            return m_GroupManifests[groupClipIndex];
        }

        CharacterAclResourceReadinessResult m_CachedRuntimeValidation;

        public CharacterAclResourceReadinessResult ValidateRuntime()
        {
            CharacterAclResourceReadinessResult result = ValidateRuntimeOnce();
            if (!result.IsPending)
                m_CachedRuntimeValidation = result;
            return result;
        }

        CharacterAclResourceReadinessResult ValidateRuntimeOnce()
        {
            if (Manifest == null)
                return CharacterAclResourceReadinessResult.Invalid(CharacterAclResourceFailureCode.SchemaMismatch, "ACL resource manifest is missing.");
            try
            {
                Manifest.RequireValid();
            }
            catch (Exception exception)
            {
                return CharacterAclResourceReadinessResult.Invalid(CharacterAclResourceFailureCode.SchemaMismatch, exception.Message);
            }
            if (m_GroupManifests == null || m_GroupManifests.Length == 0 ||
                m_GroupTransformPayloads == null ||
                m_GroupTransformPayloads.Length != m_GroupManifests.Length ||
                m_GroupScalarPayloads == null ||
                m_GroupScalarPayloads.Length != m_GroupManifests.Length)
                return CharacterAclResourceReadinessResult.Invalid(
                    CharacterAclResourceFailureCode.MissingRequiredBlock,
                    "ACL resource group payload arrays are incomplete.");
            bool pending = false;
            for (int i = 0; i < m_GroupManifests.Length; i++)
            {
                CharacterAclAnimationResourceManifest groupManifest =
                    m_GroupManifests[i];
                if (groupManifest == null ||
                    groupManifest.GroupClipIndex != i ||
                    !string.Equals(
                        groupManifest.GroupContentHash,
                        Manifest.GroupContentHash,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        groupManifest.NativeArtifactIdentity,
                        Manifest.NativeArtifactIdentity,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        groupManifest.NativeBinarySha256,
                        Manifest.NativeBinarySha256,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        groupManifest.NativePlatform,
                        Manifest.NativePlatform,
                        StringComparison.Ordinal) ||
                    groupManifest.NativeAbiVersion != Manifest.NativeAbiVersion ||
                    groupManifest.NativePayloadFormatVersion !=
                        Manifest.NativePayloadFormatVersion)
                    return CharacterAclResourceReadinessResult.Invalid(
                        CharacterAclResourceFailureCode.BindingMismatch,
                        "ACL resource group manifest mapping is invalid.");
                groupManifest.RequireValid();
                CharacterAclResourceReadinessResult blockResult = ValidateBlock(
                    groupManifest.Transform,
                    m_GroupTransformPayloads[i],
                    CharacterAclDataBlockKind.Transform,
                    true);
                if (blockResult.IsInvalid)
                    return blockResult;
                pending |= blockResult.IsPending;
                blockResult = ValidateBlock(
                    groupManifest.Scalar,
                    m_GroupScalarPayloads[i],
                    CharacterAclDataBlockKind.Scalar,
                    groupManifest.Scalar.Exists);
                if (blockResult.IsInvalid)
                    return blockResult;
                pending |= blockResult.IsPending;
            }
            CharacterAclResourceReadinessResult result = ValidateBlock(
                Manifest.DatabaseHeader,
                m_DatabaseHeaderPayload,
                CharacterAclDataBlockKind.DatabaseHeader,
                Manifest.DatabaseHeader.Exists);
            if (result.IsInvalid)
                return result;
            pending |= result.IsPending;
            result = ValidateBlock(
                Manifest.BulkMedium,
                m_BulkMediumPayload,
                CharacterAclDataBlockKind.BulkMedium,
                Manifest.BulkMedium.Exists);
            if (result.IsInvalid)
                return result;
            pending |= result.IsPending;
            result = ValidateBlock(
                Manifest.BulkLow,
                m_BulkLowPayload,
                CharacterAclDataBlockKind.BulkLow,
                Manifest.BulkLow.Exists);
            if (result.IsInvalid)
                return result;
            return pending ? CharacterAclResourceReadinessResult.Pending() : CharacterAclResourceReadinessResult.Ready();
        }

        internal byte[][] RequireGroupPayload(
            CharacterAclDataBlockKind kind)
        {
            CharacterAclResourceReadinessResult validation = ValidateRuntime();
            if (!validation.IsReady)
                throw new InvalidOperationException(validation.Message);
            if (kind != CharacterAclDataBlockKind.Transform &&
                kind != CharacterAclDataBlockKind.Scalar)
                throw new ArgumentOutOfRangeException(nameof(kind));
            TextAsset[] payloads = kind == CharacterAclDataBlockKind.Transform
                ? m_GroupTransformPayloads
                : m_GroupScalarPayloads;
            var result = new byte[payloads.Length][];
            for (int i = 0; i < payloads.Length; i++)
                result[i] = payloads[i] ? payloads[i].bytes : Array.Empty<byte>();
            return result;
        }

        public byte[] RequirePayload(
            CharacterAclDataBlockKind kind,
            int groupClipIndex)
        {
            CharacterAclResourceReadinessResult validation = ValidateRuntime();
            if (!validation.IsReady)
                throw new InvalidOperationException(validation.Message);
            if ((uint)groupClipIndex >= (uint)GroupClipCount)
                throw new ArgumentOutOfRangeException(nameof(groupClipIndex));
            TextAsset payload = kind switch
            {
                CharacterAclDataBlockKind.Transform => m_GroupTransformPayloads[groupClipIndex],
                CharacterAclDataBlockKind.Scalar => m_GroupScalarPayloads[groupClipIndex],
                CharacterAclDataBlockKind.DatabaseHeader => DatabaseHeaderPayload,
                CharacterAclDataBlockKind.BulkMedium => BulkMediumPayload,
                CharacterAclDataBlockKind.BulkLow => BulkLowPayload,
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
            return payload ? payload.bytes : Array.Empty<byte>();
        }

        public void ConfigureForBuild(
            CharacterAclAnimationResourceManifest manifest,
            CharacterAclAnimationResourceManifest[] groupManifests,
            TextAsset[] transformPayloads,
            TextAsset[] scalarPayloads,
            TextAsset databaseHeaderPayload,
            TextAsset bulkMediumPayload,
            TextAsset bulkLowPayload)
        {
            m_Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            m_GroupManifests = groupManifests ?? throw new ArgumentNullException(nameof(groupManifests));
            m_GroupTransformPayloads = transformPayloads ?? throw new ArgumentNullException(nameof(transformPayloads));
            m_GroupScalarPayloads = scalarPayloads ?? throw new ArgumentNullException(nameof(scalarPayloads));
            m_DatabaseHeaderPayload = databaseHeaderPayload;
            m_BulkMediumPayload = bulkMediumPayload;
            m_BulkLowPayload = bulkLowPayload;
            CharacterAclResourceReadinessResult result = ValidateRuntime();
            if (!result.IsReady)
                throw new InvalidOperationException(result.Message);
        }

        static CharacterAclResourceReadinessResult ValidateBlock(
            CharacterAclDataBlockDescriptor descriptor,
            TextAsset payload,
            CharacterAclDataBlockKind kind,
            bool required)
        {
            if (descriptor == null)
                return CharacterAclResourceReadinessResult.Invalid(CharacterAclResourceFailureCode.MissingRequiredBlock, $"ACL data block '{kind}' descriptor is missing.");
            if (!descriptor.Exists)
                return required
                    ? CharacterAclResourceReadinessResult.Invalid(CharacterAclResourceFailureCode.MissingRequiredBlock, $"ACL data block '{kind}' is required.")
                    : CharacterAclResourceReadinessResult.Ready();
            if (!payload)
                return CharacterAclResourceReadinessResult.Pending();
            byte[] bytes = payload.bytes;
            if (bytes.Length != descriptor.Length)
                return CharacterAclResourceReadinessResult.Invalid(CharacterAclResourceFailureCode.BlockLengthMismatch, $"ACL data block '{kind}' length is {bytes.Length}, expected {descriptor.Length}.");
            if (!descriptor.Matches(bytes))
                return CharacterAclResourceReadinessResult.Invalid(CharacterAclResourceFailureCode.BlockHashMismatch, $"ACL data block '{kind}' hash does not match its manifest.");
            return CharacterAclResourceReadinessResult.Ready();
        }
    }
}
