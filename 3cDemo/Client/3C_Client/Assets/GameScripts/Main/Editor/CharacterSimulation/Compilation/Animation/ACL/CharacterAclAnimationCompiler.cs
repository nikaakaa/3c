using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL
{
    internal static class CharacterAclAnimationCompiler
    {
        internal static CharacterAclAnimationCompressionResult[] CompileGroup(
            IReadOnlyList<CharacterAnimationSampleSet> sampleSets,
            CharacterAclCompressionSettings settings,
            CharacterAclNativeArtifactIdentity nativeArtifactIdentity)
        {
            if (sampleSets == null || sampleSets.Count == 0)
                throw new ArgumentNullException(nameof(sampleSets));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            settings.RequireValid();
            nativeArtifactIdentity = nativeArtifactIdentity ??
                throw new ArgumentNullException(nameof(nativeArtifactIdentity));
            if (!CharacterAclNativeBridge.TryGetAbi(
                    out CharacterAclNativeAbiInfo abiInfo) ||
                abiInfo.AbiVersion != nativeArtifactIdentity.AbiVersion ||
                abiInfo.PayloadFormatVersion != nativeArtifactIdentity.PayloadFormatVersion)
                throw new InvalidOperationException("ACL native bridge ABI does not match the formal payload contract.");

            var nativeClips = new CharacterAclNativeGroupBuildClip[sampleSets.Count];
            var transformBindings = new CharacterAclTransformTrackBinding[sampleSets.Count][];
            var scalarBindings = new CharacterAclScalarTrackBinding[sampleSets.Count][];
            for (int i = 0; i < sampleSets.Count; i++)
            {
                CharacterAnimationSampleSet samples = sampleSets[i] ??
                    throw new ArgumentException(
                        "ACL animation sample group contains a missing sample set.",
                        nameof(sampleSets));
                nativeClips[i] = new CharacterAclNativeGroupBuildClip(
                    samples.TransformSamples,
                    samples.TransformDefaults,
                    samples.ParentIndices,
                    samples.PhysicalBoneCount,
                    samples.Grid.SampleCount,
                    samples.Grid.ActualSampleRate,
                    settings.NativeTransformPrecision,
                    settings.ShellDistance,
                    samples.Grid.Looping,
                    samples.BuildCompressedScalarSamples(),
                    samples.CompressedScalarTrackCount,
                    settings.NativeScalarPrecision);
                transformBindings[i] = CreateTransformBindings(samples.Source.SourceRig);
                scalarBindings[i] = CreateScalarBindings(samples);
            }
            CharacterAclNativeError error = CharacterAclNativeBuildBridge.BuildGroup(
                nativeClips,
                settings.EnableDatabase,
                settings.MediumImportanceTierProportion,
                settings.LowImportanceTierProportion,
                settings.MaxDatabaseChunkSize,
                out byte[][] transformPayloads,
                out byte[][] scalarPayloads,
                out byte[] databaseHeaderPayload,
                out byte[] bulkMediumPayload,
                out byte[] bulkLowPayload);
            if (error != CharacterAclNativeError.Success || transformPayloads == null ||
                scalarPayloads == null || transformPayloads.Length != sampleSets.Count)
                throw new InvalidOperationException($"ACL animation compression failed: {error}.");
            var results = new CharacterAclAnimationCompressionResult[sampleSets.Count];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = new CharacterAclAnimationCompressionResult(
                    abiInfo.AbiVersion,
                    abiInfo.PayloadFormatVersion,
                    nativeArtifactIdentity.Identity,
                    transformPayloads[i],
                    scalarPayloads[i],
                    databaseHeaderPayload,
                    bulkMediumPayload,
                    bulkLowPayload,
                    transformBindings[i],
                    scalarBindings[i],
                    i);
            }
            return results;
        }

        static CharacterAclTransformTrackBinding[] CreateTransformBindings(
            CharacterAnimationSourceRig rig)
        {
            var result = new CharacterAclTransformTrackBinding[rig.PhysicalBoneCount];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new CharacterAclTransformTrackBinding(
                    rig.PhysicalBones[i].BoneId,
                    i,
                    i,
                    CharacterAclAnimationIdentity.ComputePoseBoneReferenceIdentity(
                        rig.RigId,
                        rig.RigRevision,
                        i,
                        rig.PhysicalBones[i].BoneId.Value,
                        rig.PhysicalBones[i].ParentPhysicalIndex,
                        rig.PhysicalBones[i].ReferenceLocalPose));
            }
            return result;
        }

        static CharacterAclScalarTrackBinding[] CreateScalarBindings(
            CharacterAnimationSampleSet samples)
        {
            int scalarTrackCount = 0;
            var result = new List<CharacterAclScalarTrackBinding>(samples.ScalarTracks.Length);
            for (int i = 0; i < samples.ScalarTracks.Length; i++)
            {
                CharacterAnimationAuthoringScalarTrack track = samples.ScalarTracks[i].Source;
                result.Add(new CharacterAclScalarTrackBinding(
                    track.ParameterId,
                    track.ParameterIndex,
                    track.IsAnimated ? scalarTrackCount++ : -1,
                    !track.IsAnimated,
                    track.DefaultValue,
                    track.Unit));
            }
            return result.ToArray();
        }
    }
}
