using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterAnimationBlendSpaceAuthoringService
    {
        public static void Initialize(CharacterAnimationBlendSpaceAsset asset)
        {
            RequireAsset(asset);
            if (asset.BlendSpaceId.IsValid)
                return;
            Initialize(asset, new CharacterAnimationBlendSpaceId($"blend-space.{Guid.NewGuid():N}"));
        }

        public static void Initialize(
            CharacterAnimationBlendSpaceAsset asset,
            CharacterAnimationBlendSpaceId blendSpaceId)
        {
            RequireAsset(asset);
            if (asset.BlendSpaceId.IsValid)
                throw new InvalidOperationException($"Blend Space '{asset.name}' is already initialized as '{asset.BlendSpaceId}'.");
            if (!blendSpaceId.IsValid)
                throw new ArgumentException("Blend Space identity is invalid.", nameof(blendSpaceId));
            Undo.RecordObject(asset, "Initialize Animation Blend Space");
            asset.Initialize(blendSpaceId);
            Finish(asset);
        }

        public static void SetRig(CharacterAnimationBlendSpaceAsset asset, CharacterAnimationRigDefinition rig)
        {
            RequireAsset(asset);
            Undo.RecordObject(asset, "Set Blend Space Rig");
            asset.SetRig(rig);
            Finish(asset);
        }

        public static void SetMode(CharacterAnimationBlendSpaceAsset asset, CharacterAnimationBlendSpaceMode mode)
        {
            RequireAsset(asset);
            bool axisShapeMatches = mode == CharacterAnimationBlendSpaceMode.Linear1D
                ? asset.YAxis == null
                : asset.YAxis != null;
            if (asset.Mode == mode && axisShapeMatches)
                return;
            Undo.RecordObject(asset, "Set Blend Space Mode");
            asset.SetMode(mode);
            Finish(asset);
        }

        public static void SetAxis(
            CharacterAnimationBlendSpaceAsset asset,
            int axisIndex,
            PoseParameterId parameterId,
            string unit,
            float minimum,
            float maximum)
        {
            RequireAsset(asset);
            Undo.RecordObject(asset, "Set Blend Space Axis");
            asset.SetAxis(axisIndex, parameterId, unit, minimum, maximum);
            Finish(asset);
        }

        public static CharacterAnimationBlendSpaceSampleId CreateSample(CharacterAnimationBlendSpaceAsset asset, Vector2 position)
        {
            var sampleId = new CharacterAnimationBlendSpaceSampleId($"sample.{Guid.NewGuid():N}");
            CreateSample(asset, sampleId, position);
            return sampleId;
        }

        public static void CreateSample(
            CharacterAnimationBlendSpaceAsset asset,
            CharacterAnimationBlendSpaceSampleId sampleId,
            Vector2 position)
        {
            RequireAsset(asset);
            if (!sampleId.IsValid)
                throw new ArgumentException("Blend Space Sample identity is invalid.", nameof(sampleId));
            if (asset.FindSample(sampleId) != null)
                throw new InvalidOperationException($"Blend Space Sample '{sampleId}' already exists in '{asset.name}'.");
            Undo.RecordObject(asset, "Create Blend Space Sample");
            var sample = new CharacterAnimationBlendSpaceSample();
            sample.Initialize(sampleId, NormalizePosition(asset, position));
            CharacterAnimationBlendSpaceSample[] samples = asset.Samples.Concat(new[] { sample }).ToArray();
            asset.SetSamples(samples);
            Finish(asset);
        }

        public static CharacterAnimationBlendSpaceSampleId DuplicateSample(
            CharacterAnimationBlendSpaceAsset asset,
            CharacterAnimationBlendSpaceSampleId sourceId,
            Vector2 position)
        {
            RequireAsset(asset);
            CharacterAnimationBlendSpaceSample source = RequireSample(asset, sourceId);
            Undo.RecordObject(asset, "Duplicate Blend Space Sample");
            var sampleId = new CharacterAnimationBlendSpaceSampleId($"sample.{Guid.NewGuid():N}");
            CharacterAnimationBlendSpaceSample clone = source.Clone(sampleId);
            clone.SetPosition(NormalizePosition(asset, position));
            CharacterAnimationBlendSpaceSample[] samples = asset.Samples.Concat(new[] { clone }).ToArray();
            asset.SetSamples(samples);
            Finish(asset);
            return sampleId;
        }

        public static void DeleteSample(CharacterAnimationBlendSpaceAsset asset, CharacterAnimationBlendSpaceSampleId sampleId)
        {
            RequireAsset(asset);
            RequireSample(asset, sampleId);
            Undo.RecordObject(asset, "Delete Blend Space Sample");
            CharacterAnimationBlendSpaceSample[] samples = asset.Samples.Where(sample => sample != null && !sample.SampleId.Equals(sampleId)).ToArray();
            asset.SetSamples(samples);
            Finish(asset);
        }

        public static void DeleteSamples(
            CharacterAnimationBlendSpaceAsset asset,
            IReadOnlyCollection<CharacterAnimationBlendSpaceSampleId> sampleIds)
        {
            RequireAsset(asset);
            if (sampleIds == null || sampleIds.Count == 0)
                return;
            var removed = new HashSet<CharacterAnimationBlendSpaceSampleId>(sampleIds);
            foreach (CharacterAnimationBlendSpaceSampleId sampleId in removed)
                RequireSample(asset, sampleId);
            Undo.RecordObject(asset, "Delete Blend Space Samples");
            CharacterAnimationBlendSpaceSample[] samples = asset.Samples
                .Where(sample => sample != null && !removed.Contains(sample.SampleId))
                .ToArray();
            asset.SetSamples(samples);
            Finish(asset);
        }

        public static void SetSamplePosition(
            CharacterAnimationBlendSpaceAsset asset,
            CharacterAnimationBlendSpaceSampleId sampleId,
            Vector2 position)
        {
            RequireAsset(asset);
            CharacterAnimationBlendSpaceSample sample = RequireSample(asset, sampleId);
            Undo.RecordObject(asset, "Move Blend Space Sample");
            sample.SetPosition(NormalizePosition(asset, position));
            asset.TouchContentRevision();
            Finish(asset);
        }

        public static void SetSamplePositions(
            CharacterAnimationBlendSpaceAsset asset,
            IReadOnlyDictionary<CharacterAnimationBlendSpaceSampleId, Vector2> positions)
        {
            RequireAsset(asset);
            if (positions == null || positions.Count == 0)
                return;
            foreach (KeyValuePair<CharacterAnimationBlendSpaceSampleId, Vector2> pair in positions)
                RequireSample(asset, pair.Key);
            Undo.RecordObject(asset, "Move Blend Space Samples");
            foreach (KeyValuePair<CharacterAnimationBlendSpaceSampleId, Vector2> pair in positions)
                asset.FindSample(pair.Key).SetPosition(NormalizePosition(asset, pair.Value));
            asset.TouchContentRevision();
            Finish(asset);
        }

        public static void SetSampleClip(
            CharacterAnimationBlendSpaceAsset asset,
            CharacterAnimationBlendSpaceSampleId sampleId,
            AnimationClip clip)
        {
            RequireAsset(asset);
            CharacterAnimationBlendSpaceSample sample = RequireSample(asset, sampleId);
            string path = clip ? AssetDatabase.GetAssetPath(clip) : string.Empty;
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Blend Space Sample AnimationClip is not a persistent asset.", nameof(clip));
            string guid = AssetDatabase.AssetPathToGUID(path);
            string dependency = AssetDatabase.GetAssetDependencyHash(path).ToString();
            Undo.RecordObject(asset, "Set Blend Space Sample Clip");
            sample.SetClip(clip, $"{guid}:{dependency}");
            asset.TouchContentRevision();
            Finish(asset);
        }

        public static void SetSampleRole(
            CharacterAnimationBlendSpaceAsset asset,
            CharacterAnimationBlendSpaceSampleId sampleId,
            CharacterAnimationBlendSpaceSampleRole role,
            float stationaryNormalizedTime)
        {
            RequireAsset(asset);
            CharacterAnimationBlendSpaceSample sample = RequireSample(asset, sampleId);
            Undo.RecordObject(asset, "Set Blend Space Sample Role");
            sample.SetRole(role, stationaryNormalizedTime);
            if (asset.PhaseReferenceSampleId.Equals(sampleId) && role != CharacterAnimationBlendSpaceSampleRole.DynamicCycle)
                asset.SetPhase(CharacterAnimationBlendSpacePhasePolicy.SharedNormalizedPhase, default);
            else
                asset.TouchContentRevision();
            Finish(asset);
        }

        public static void SetPhase(
            CharacterAnimationBlendSpaceAsset asset,
            CharacterAnimationBlendSpacePhasePolicy policy,
            CharacterAnimationBlendSpaceSampleId referenceSampleId)
        {
            RequireAsset(asset);
            if (policy == CharacterAnimationBlendSpacePhasePolicy.MarkerSynchronizedPhase)
            {
                CharacterAnimationBlendSpaceSample reference = RequireSample(asset, referenceSampleId);
                if (reference.Role != CharacterAnimationBlendSpaceSampleRole.DynamicCycle)
                    throw new InvalidOperationException("Blend Space Phase Reference Sample must be DynamicCycle.");
            }
            Undo.RecordObject(asset, "Set Blend Space Phase");
            asset.SetPhase(policy, referenceSampleId);
            if (policy == CharacterAnimationBlendSpacePhasePolicy.SharedNormalizedPhase)
            {
                for (int i = 0; i < asset.Samples.Count; i++)
                    asset.Samples[i]?.SetMarkers(Array.Empty<CharacterAnimationBlendSpaceMarker>());
            }
            Finish(asset);
        }

        public static void SetSampleMarkers(
            CharacterAnimationBlendSpaceAsset asset,
            CharacterAnimationBlendSpaceSampleId sampleId,
            CharacterAnimationBlendSpaceMarker[] markers)
        {
            RequireAsset(asset);
            if (asset.PhasePolicy != CharacterAnimationBlendSpacePhasePolicy.MarkerSynchronizedPhase)
                throw new InvalidOperationException("Marker bindings require MarkerSynchronizedPhase.");
            CharacterAnimationBlendSpaceSample sample = RequireSample(asset, sampleId);
            if (sample.Role != CharacterAnimationBlendSpaceSampleRole.DynamicCycle)
                throw new InvalidOperationException("Stationary Blend Space Samples cannot own marker bindings.");
            CharacterAnimationBlendSpaceMarker[] ordered = markers == null
                ? Array.Empty<CharacterAnimationBlendSpaceMarker>()
                : markers.OrderBy(value => value?.NormalizedTime ?? float.PositiveInfinity).ToArray();
            Undo.RecordObject(asset, "Set Blend Space Sample Markers");
            sample.SetMarkers(ordered);
            asset.TouchContentRevision();
            Finish(asset);
        }

        public static void SetSampleParameters(
            CharacterAnimationBlendSpaceAsset asset,
            CharacterAnimationBlendSpaceSampleId sampleId,
            CharacterAnimationBlendSpaceSampleParameter[] parameters)
        {
            RequireAsset(asset);
            CharacterAnimationBlendSpaceSample sample = RequireSample(asset, sampleId);
            CharacterAnimationBlendSpaceSampleParameter[] ordered = parameters == null
                ? Array.Empty<CharacterAnimationBlendSpaceSampleParameter>()
                : parameters.OrderBy(value => value?.ParameterId ?? default).ToArray();
            Undo.RecordObject(asset, "Set Blend Space Sample Parameters");
            sample.SetParameters(ordered);
            asset.TouchContentRevision();
            Finish(asset);
        }

        public static void ReplacePoseParameterPolicies(
            CharacterAnimationBlendSpaceAsset asset,
            CharacterAnimationBlendSpacePoseParameterPolicy[] policies)
        {
            RequireAsset(asset);
            Undo.RecordObject(asset, "Set Blend Space Parameter Policies");
            CharacterAnimationBlendSpacePoseParameterPolicy[] values = policies ?? Array.Empty<CharacterAnimationBlendSpacePoseParameterPolicy>();
            var writable = new HashSet<PoseParameterId>(values
                .Where(value => value != null && value.Policy != CharacterAnimationBlendSpaceParameterPolicy.Unavailable)
                .Select(value => value.ParameterId));
            for (int i = 0; i < asset.Samples.Count; i++)
            {
                CharacterAnimationBlendSpaceSample sample = asset.Samples[i];
                if (sample == null)
                    continue;
                sample.SetParameters(sample.Parameters
                    .Where(value => value != null && writable.Contains(value.ParameterId))
                    .ToArray());
            }
            asset.SetPoseParameterPolicies(values);
            Finish(asset);
        }

        public static void SetPreview(CharacterAnimationBlendSpaceAsset asset, Vector2 parameter, float normalizedTime)
        {
            RequireAsset(asset);
            Undo.RecordObject(asset, "Set Blend Space Preview");
            asset.SetPreview(parameter, normalizedTime);
            Finish(asset);
        }

        static Vector2 NormalizePosition(CharacterAnimationBlendSpaceAsset asset, Vector2 position)
        {
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y))
                throw new ArgumentOutOfRangeException(nameof(position));
            if (asset.AxisCount == 1)
                position.y = 0f;
            return position;
        }

        static CharacterAnimationBlendSpaceSample RequireSample(
            CharacterAnimationBlendSpaceAsset asset,
            CharacterAnimationBlendSpaceSampleId sampleId)
        {
            if (!sampleId.IsValid)
                throw new ArgumentException("Blend Space Sample identity is invalid.", nameof(sampleId));
            return asset.FindSample(sampleId) ?? throw new InvalidOperationException($"Blend Space Sample '{sampleId}' is not part of '{asset.name}'.");
        }

        static void RequireAsset(CharacterAnimationBlendSpaceAsset asset)
        {
            if (!asset)
                throw new ArgumentNullException(nameof(asset));
        }

        static void Finish(CharacterAnimationBlendSpaceAsset asset)
        {
            EditorUtility.SetDirty(asset);
        }
    }
}
