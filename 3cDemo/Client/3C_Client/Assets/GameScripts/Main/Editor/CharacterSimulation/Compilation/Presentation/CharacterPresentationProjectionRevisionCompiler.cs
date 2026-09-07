using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterPresentationProjectionRevisionCompiler
    {
        internal static string Compute(
            CharacterAnimationPresentationProfile animationProfile,
            UnityEngine.Object equipmentPresentationProfile,
            StableHash contractHash,
            IReadOnlyList<string> footAnalysisTokens,
            MotionMatchingProjectionPayload motionMatching,
            IReadOnlyList<CharacterAnimationCompiledResourceDescriptor> animationResources)
        {
            var values = new List<string>
            {
                CharacterPresentationProjection.CurrentAbiVersion,
                contractHash.ToString(),
                CharacterAnimationCompiledResourceRevision.Compute(animationResources)
            };
            AddAssetRevision(animationProfile, values);
            AddAssetRevision(equipmentPresentationProfile, values);
            if (animationProfile != null)
            {
                for (int i = 0; i < animationProfile.SourceResourceBindings.Count; i++)
                {
                    CharacterAnimationSourceResourceBinding binding = animationProfile.SourceResourceBindings[i];
                    values.Add($"source-resource:{binding?.AuthoringClip?.name}:{binding?.Backend}");
                }
                for (int i = 0; i < animationProfile.AnimationPropertyBindings.Count; i++)
                {
                    CharacterAnimationPropertyAuthoringBinding binding = animationProfile.AnimationPropertyBindings[i];
                    values.Add($"animation-property:{binding?.ParameterId}:{binding?.RendererBindingId}:{binding?.AnimationCurvePath}:{binding?.BlendShapeName}:{binding?.BlendShapeIndex}:{binding?.MeshContentHash}");
                }
            }
            CharacterPresentationMotionMatchingCompiler.AppendRevisionValues(
                motionMatching,
                values);
            if (footAnalysisTokens != null)
            {
                for (int i = 0; i < footAnalysisTokens.Count; i++)
                    values.Add(footAnalysisTokens[i]);
            }
            return StableHash.Compute(values.ToArray()).ToString();
        }

        static void AddAssetRevision(UnityEngine.Object root, List<string> values)
        {
            if (!root)
            {
                values.Add("none");
                return;
            }
            string rootPath = AssetDatabase.GetAssetPath(root);
            string[] dependencies = AssetDatabase.GetDependencies(rootPath, true)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            values.Add(rootPath);
            for (int i = 0; i < dependencies.Length; i++)
            {
                string path = dependencies[i];
                values.Add(path);
                values.Add(AssetDatabase.AssetPathToGUID(path));
                values.Add(AssetDatabase.GetAssetDependencyHash(path).ToString());
            }
        }
    }
}
