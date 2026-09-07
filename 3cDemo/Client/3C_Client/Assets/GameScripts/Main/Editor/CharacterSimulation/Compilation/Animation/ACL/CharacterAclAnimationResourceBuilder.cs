using System;
using System.Collections.Generic;
using System.Globalization;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL
{
    internal static class CharacterAclAnimationResourceBuilder
    {
        internal static CharacterAclAnimationGroupArtifact BuildGroup(
            IReadOnlyList<CharacterAclAnimationBuildRequest> requests,
            int groupIndex,
            string ownerAssetGuid,
            CharacterAclNativeArtifactIdentity nativeArtifactIdentity,
            string buildInputIdentity,
            string sourceInputIdentity = null)
        {
            if (requests == null || requests.Count == 0)
                throw new ArgumentNullException(nameof(requests));
            if (groupIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(groupIndex));
            if (string.IsNullOrWhiteSpace(buildInputIdentity))
                throw new ArgumentException(
                    "ACL build input identity is required.",
                    nameof(buildInputIdentity));
            ownerAssetGuid = CharacterAclAnimationArtifactIdentity.RequireOwnerAssetGuid(ownerAssetGuid);
            nativeArtifactIdentity = nativeArtifactIdentity ??
                throw new ArgumentNullException(nameof(nativeArtifactIdentity));
            sourceInputIdentity = RequireOptionalSourceInputIdentity(sourceInputIdentity);
            var sources = new CharacterAnimationAuthoringSource[requests.Count];
            var samples = new CharacterAnimationSampleSet[requests.Count];
            var samplingQuality = new CharacterAnimationSamplingQualityEvaluation[requests.Count];
            CharacterAclCompressionSettings settings = requests[0]?.Settings ??
                throw new ArgumentException("ACL animation group contains a missing request.", nameof(requests));
            settings.RequireValid();
            for (int i = 0; i < requests.Count; i++)
            {
                CharacterAclAnimationBuildRequest request = requests[i] ??
                    throw new ArgumentException("ACL animation group contains a missing request.", nameof(requests));
                request.Settings.RequireValid();
                if (request.Settings.Revision != settings.Revision ||
                    request.Settings.CompilerOptions != settings.CompilerOptions ||
                    request.Settings.SampleRate != settings.SampleRate ||
                    request.Settings.TransformPrecision != settings.TransformPrecision ||
                    request.Settings.ScalePrecision != settings.ScalePrecision ||
                    request.Settings.RotationPrecisionDegrees != settings.RotationPrecisionDegrees ||
                    request.Settings.ScalarPrecision != settings.ScalarPrecision ||
                    request.Settings.EnableDatabase != settings.EnableDatabase ||
                    request.Settings.MediumImportanceTierProportion != settings.MediumImportanceTierProportion ||
                    request.Settings.LowImportanceTierProportion != settings.LowImportanceTierProportion ||
                    request.Settings.MaxDatabaseChunkSize != settings.MaxDatabaseChunkSize)
                    throw new InvalidOperationException("ACL animation group requests use different compression settings.");
                Debug.Log(
                    $"[ACL] Read/sample clip {i + 1}/{requests.Count}: " +
                    $"{request.AuthoringRequest.ClipIdentity.AssetPath}");
                sources[i] = CharacterAnimationAuthoringReader.Read(request.AuthoringRequest);
                CharacterAnimationSampleGrid grid = CharacterAnimationSampleGrid.Create(
                    sources[i].DurationSeconds,
                    settings.SampleRate,
                    sources[i].Looping);
                samples[i] = CharacterAnimationSourceSampler.Sample(sources[i], grid);
                samplingQuality[i] = CharacterAnimationSamplingQualityEvaluator.Evaluate(
                    sources[i],
                    samples[i],
                    new CharacterAnimationSamplingQualitySettings(
                        settings.TransformPrecision,
                        settings.ScalePrecision,
                        settings.RotationPrecisionDegrees,
                        settings.ScalarPrecision));
                if (i > 0 &&
                    (sources[i].SourceRig.RigId != sources[0].SourceRig.RigId ||
                     sources[i].SourceRig.RigRevision != sources[0].SourceRig.RigRevision ||
                     sources[i].ParameterLayout.Hash != sources[0].ParameterLayout.Hash))
                    throw new InvalidOperationException("ACL animation group requests use different Rig or Parameter layouts.");
            }
            CharacterAclAnimationCompressionResult[] compressions =
                CharacterAclAnimationCompiler.CompileGroup(
                    samples,
                    settings,
                    nativeArtifactIdentity);
            string groupContentHash = ComputeGroupContentHash(
                sources,
                samples,
                compressions,
                settings,
                ownerAssetGuid,
                nativeArtifactIdentity,
                sourceInputIdentity);
            var manifests = new CharacterAclAnimationResourceManifest[requests.Count];
            var qualityReports = new CharacterAclAnimationQualityReport[requests.Count];
            for (int i = 0; i < requests.Count; i++)
            {
                CharacterAclAnimationQualityReport quality =
                    CharacterAclAnimationQualityEvaluator.Evaluate(
                        sources[i],
                        samples[i],
                        compressions[i],
                        samplingQuality[i],
                        settings,
                        compressions,
                        i);
                if (!quality.publishable)
                    throw new InvalidOperationException(
                        $"ACL resource quality gate failed for '{sources[i].ClipIdentity.AssetPath}' " +
                        $"[{FormatQualitySummary(quality)}]: {string.Join(" | ", quality.errors)}");
                CharacterAclAnimationResourceManifest manifest = BuildManifest(
                    sources[i],
                    samples[i],
                    compressions[i],
                    quality,
                    settings,
                    ownerAssetGuid,
                    i,
                    groupContentHash,
                    nativeArtifactIdentity,
                    sourceInputIdentity);
                manifests[i] = manifest;
                qualityReports[i] = quality;
            }
            var groupTransformPayloads = new byte[requests.Count][];
            var groupScalarPayloads = new byte[requests.Count][];
            for (int i = 0; i < requests.Count; i++)
            {
                groupTransformPayloads[i] = compressions[i].TransformPayload;
                groupScalarPayloads[i] = compressions[i].ScalarPayload;
            }
            return new CharacterAclAnimationGroupArtifact(
                groupIndex,
                groupContentHash,
                buildInputIdentity,
                manifests,
                groupTransformPayloads,
                groupScalarPayloads,
                compressions[0].DatabaseHeaderPayload,
                compressions[0].BulkMediumPayload,
                compressions[0].BulkLowPayload,
                qualityReports);
        }

        static string ComputeGroupContentHash(
            CharacterAnimationAuthoringSource[] sources,
            CharacterAnimationSampleSet[] samples,
            CharacterAclAnimationCompressionResult[] compressions,
            CharacterAclCompressionSettings settings,
            string ownerAssetGuid,
            CharacterAclNativeArtifactIdentity nativeArtifactIdentity,
            string sourceInputIdentity)
        {
            string normalizedOwnerAssetGuid =
                CharacterAclAnimationArtifactIdentity.RequireOwnerAssetGuid(
                    ownerAssetGuid);
            var values = new List<string>(compressions.Length * 8 + 12)
            {
                "acl-animation-group/v3",
                normalizedOwnerAssetGuid,
                sources[0].SourceRig.RigId,
                sources[0].SourceRig.RigRevision,
                sources[0].ParameterLayout.Hash,
                nativeArtifactIdentity.Identity,
                compressions[0].AbiVersion.ToString(CultureInfo.InvariantCulture),
                compressions[0].PayloadFormatVersion.ToString(CultureInfo.InvariantCulture),
                settings.Revision,
                CharacterAclCompressionSettings.NativeEncodingAlgorithmVersion,
                settings.CompilerOptions,
                settings.SampleRate.ToString(CultureInfo.InvariantCulture),
                settings.TransformPrecision.ToString("R", CultureInfo.InvariantCulture),
                settings.ScalePrecision.ToString("R", CultureInfo.InvariantCulture),
                settings.RotationPrecisionDegrees.ToString("R", CultureInfo.InvariantCulture),
                settings.ScalarPrecision.ToString("R", CultureInfo.InvariantCulture),
                settings.NativeTransformPrecision.ToString("R", CultureInfo.InvariantCulture),
                settings.NativeScalarPrecision.ToString("R", CultureInfo.InvariantCulture),
                settings.EnableDatabase ? "database" : "tracks"
            };
            if (!string.IsNullOrEmpty(sourceInputIdentity))
                values.Add(sourceInputIdentity);
            for (int i = 0; i < compressions.Length; i++)
            {
                values.Add(sources[i].ClipIdentity.AssetGuid);
                values.Add(sources[i].ClipIdentity.LocalFileId.ToString(CultureInfo.InvariantCulture));
                values.Add(samples[i].Grid.SampleCount.ToString(CultureInfo.InvariantCulture));
                values.Add(compressions[i].TransformPayloadHash);
                values.Add(compressions[i].ScalarPayloadHash);
            }
            values.Add(compressions[0].DatabaseHeaderPayload.Length.ToString(CultureInfo.InvariantCulture));
            values.Add(compressions[0].BulkMediumPayload.Length.ToString(CultureInfo.InvariantCulture));
            values.Add(compressions[0].BulkLowPayload.Length.ToString(CultureInfo.InvariantCulture));
            values.Add(compressions[0].DatabaseHeaderPayload.Length == 0
                ? string.Empty
                : CharacterAclHash.Compute(compressions[0].DatabaseHeaderPayload));
            values.Add(compressions[0].BulkMediumPayload.Length == 0
                ? string.Empty
                : CharacterAclHash.Compute(compressions[0].BulkMediumPayload));
            values.Add(compressions[0].BulkLowPayload.Length == 0
                ? string.Empty
                : CharacterAclHash.Compute(compressions[0].BulkLowPayload));
            return CharacterAclHash.ComputeStrings(values);
        }

        static CharacterAclAnimationResourceManifest BuildManifest(
            CharacterAnimationAuthoringSource source,
            CharacterAnimationSampleSet samples,
            CharacterAclAnimationCompressionResult compression,
            CharacterAclAnimationQualityReport quality,
            CharacterAclCompressionSettings settings,
            string ownerAssetGuid,
            int groupClipIndex,
            string groupContentHash,
            CharacterAclNativeArtifactIdentity nativeArtifactIdentity,
            string sourceInputIdentity)
        {
            string referenceIdentity = source.SourceRig.ReferencePoseIdentity;
            string bindingHash = CharacterAclAnimationIdentity.ComputeTransformBindingHash(
                source.SourceRig.RigId,
                source.SourceRig.RigRevision,
                compression.TransformBindings);
            string transformHash = compression.TransformPayloadHash;
            string scalarHash = compression.ScalarPayloadHash;
            string formalClipIdentity = source.FormalClipIdentity;
            var contentValues = new List<string>
            {
                "acl-animation-content/v4",
                formalClipIdentity,
                source.SourceIdentity,
                source.SourceRig.RigId,
                source.SourceRig.RigRevision,
                source.ParameterLayout.Hash,
                referenceIdentity,
                bindingHash,
                samples.Grid.RequestedSampleRate.ToString("R", CultureInfo.InvariantCulture),
                samples.Grid.ActualSampleRate.ToString("R", CultureInfo.InvariantCulture),
                samples.Grid.SampleCount.ToString(CultureInfo.InvariantCulture),
                samples.Grid.FormalStart.ToString("R", CultureInfo.InvariantCulture),
                samples.Grid.FormalStop.ToString("R", CultureInfo.InvariantCulture),
                samples.Grid.CompressedStart.ToString("R", CultureInfo.InvariantCulture),
                samples.Grid.CompressedStop.ToString("R", CultureInfo.InvariantCulture),
                samples.Grid.Looping ? "loop" : "once",
                settings.Revision,
                CharacterAclCompressionSettings.NativeEncodingAlgorithmVersion,
                settings.CompilerOptions,
                settings.ShellDistance.ToString("R", CultureInfo.InvariantCulture),
                settings.TransformPrecision.ToString("R", CultureInfo.InvariantCulture),
                settings.ScalePrecision.ToString("R", CultureInfo.InvariantCulture),
                settings.RotationPrecisionDegrees.ToString("R", CultureInfo.InvariantCulture),
                settings.ScalarPrecision.ToString("R", CultureInfo.InvariantCulture),
                settings.NativeTransformPrecision.ToString("R", CultureInfo.InvariantCulture),
                settings.NativeScalarPrecision.ToString("R", CultureInfo.InvariantCulture),
                settings.MediumImportanceTierProportion.ToString("R", CultureInfo.InvariantCulture),
                settings.LowImportanceTierProportion.ToString("R", CultureInfo.InvariantCulture),
                settings.MaxDatabaseChunkSize.ToString(CultureInfo.InvariantCulture),
                transformHash,
                scalarHash,
                compression.AbiVersion.ToString(CultureInfo.InvariantCulture),
                compression.PayloadFormatVersion.ToString(CultureInfo.InvariantCulture),
                groupContentHash
            };
            if (!string.IsNullOrEmpty(sourceInputIdentity))
                contentValues.Insert(2, sourceInputIdentity);
            string contentHash = CharacterAclHash.ComputeStrings(contentValues);
            string resourceIdentity = CharacterAclAnimationArtifactIdentity.GetResourceIdentity(
                ownerAssetGuid,
                groupContentHash);
            CharacterAclDataBlockDescriptor database = new CharacterAclDataBlockDescriptor(
                compression.DatabaseHeaderPayload.Length > 0,
                compression.DatabaseHeaderPayload.Length,
                compression.DatabaseHeaderPayload.Length > 0
                    ? compression.PayloadFormatVersion
                    : 0,
                compression.DatabaseHeaderPayload.Length > 0
                    ? CharacterAclHash.Compute(compression.DatabaseHeaderPayload)
                    : string.Empty);
            CharacterAclDataBlockDescriptor medium = new CharacterAclDataBlockDescriptor(
                compression.BulkMediumPayload.Length > 0,
                compression.BulkMediumPayload.Length,
                compression.BulkMediumPayload.Length > 0
                    ? compression.PayloadFormatVersion
                    : 0,
                compression.BulkMediumPayload.Length > 0
                    ? CharacterAclHash.Compute(compression.BulkMediumPayload)
                    : string.Empty);
            CharacterAclDataBlockDescriptor low = new CharacterAclDataBlockDescriptor(
                compression.BulkLowPayload.Length > 0,
                compression.BulkLowPayload.Length,
                compression.BulkLowPayload.Length > 0
                    ? compression.PayloadFormatVersion
                    : 0,
                compression.BulkLowPayload.Length > 0
                    ? CharacterAclHash.Compute(compression.BulkLowPayload)
                    : string.Empty);
            var manifest = new CharacterAclAnimationResourceManifest(
                resourceIdentity,
                CharacterAclAnimationArtifactIdentity.GetAssetStem(
                    groupContentHash),
                source.SourceIdentity,
                formalClipIdentity,
                source.ClipIdentity.FullDependencyHash,
                source.SourceRig.RigId,
                source.SourceRig.RigRevision,
                referenceIdentity,
                bindingHash,
                contentHash,
                $"acl-build/v3/{contentHash}",
                groupContentHash,
                nativeArtifactIdentity.Identity,
                nativeArtifactIdentity.BinarySha256,
                nativeArtifactIdentity.Platform,
                nativeArtifactIdentity.AbiVersion,
                nativeArtifactIdentity.PayloadFormatVersion,
                groupClipIndex,
                source.SourceRig.PoseBoneCount,
                samples.PhysicalBoneCount,
                samples.PhysicalBoneCount,
                samples.CompressedScalarTrackCount,
                samples.Grid.ActualSampleRate,
                samples.Grid.CompressedStart,
                samples.Grid.CompressedStop,
                samples.Grid.Looping,
                settings.EnableDatabase,
                settings,
                new CharacterAclDataBlockDescriptor(
                    true,
                    compression.TransformPayload.Length,
                    compression.PayloadFormatVersion,
                    transformHash),
                new CharacterAclDataBlockDescriptor(
                    compression.ScalarPayload.Length > 0,
                    compression.ScalarPayload.Length,
                    compression.ScalarPayload.Length > 0
                        ? compression.PayloadFormatVersion
                        : 0,
                    scalarHash),
                database,
                medium,
                low,
                compression.TransformBindings,
                compression.ScalarBindings);
            manifest.RequireValid();
            quality.formalClipIdentity = formalClipIdentity;
            quality.transformPayloadHash = transformHash;
            quality.scalarPayloadHash = scalarHash;
            return manifest;
        }

        static string FormatQualitySummary(CharacterAclAnimationQualityReport quality) =>
            string.Join(
                ", ",
                $"clip_to_sampling_position={quality.maxClipToSamplingPositionError.ToString("R", CultureInfo.InvariantCulture)}",
                $"clip_to_sampling_rotation={quality.maxClipToSamplingRotationError.ToString("R", CultureInfo.InvariantCulture)}",
                $"clip_to_sampling_scale={quality.maxClipToSamplingScaleError.ToString("R", CultureInfo.InvariantCulture)}",
                $"sampling_to_acl_position={quality.maxSamplingToAclPositionError.ToString("R", CultureInfo.InvariantCulture)}",
                $"sampling_to_acl_rotation={quality.maxSamplingToAclRotationError.ToString("R", CultureInfo.InvariantCulture)}",
                $"sampling_to_acl_scale={quality.maxSamplingToAclScaleError.ToString("R", CultureInfo.InvariantCulture)}",
                $"clip_to_sampling_scalar={quality.maxClipToSamplingScalarError.ToString("R", CultureInfo.InvariantCulture)}",
                $"sampling_to_acl_scalar={quality.maxSamplingToAclScalarError.ToString("R", CultureInfo.InvariantCulture)}",
                $"clip_to_acl_position={quality.maxClipToAclPositionError.ToString("R", CultureInfo.InvariantCulture)}",
                $"clip_to_acl_rotation={quality.maxClipToAclRotationError.ToString("R", CultureInfo.InvariantCulture)}",
                $"clip_to_acl_scale={quality.maxClipToAclScaleError.ToString("R", CultureInfo.InvariantCulture)}",
                $"clip_to_acl_scalar={quality.maxClipToAclScalarError.ToString("R", CultureInfo.InvariantCulture)}");

        static string RequireOptionalSourceInputIdentity(string value)
        {
            string normalized = value?.Trim() ?? string.Empty;
            if (normalized.Length != 0 && !CharacterAclHash.IsSha256(normalized))
                throw new ArgumentException(
                    "ACL animation source input identity is invalid.",
                    nameof(value));
            return normalized;
        }
    }
}
