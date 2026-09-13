using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterPresentationProjectionRevisionCompiler
    {
        internal static string Compute(
            CharacterAnimationPresentationProfile animationProfile,
            IReadOnlyList<CharacterAnimationSourceResourceBinding> sourceResourceBindings,
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
            if (animationProfile?.EventGraph)
            {
                values.Add($"event-graph:{animationProfile.EventGraph.AuthoringId}:{animationProfile.EventGraph.ContentRevision}");
                AddAssetRevision(animationProfile.EventGraph, values);
            }
            AddAssetRevision(equipmentPresentationProfile, values);
            if (sourceResourceBindings != null)
            {
                for (int i = 0; i < sourceResourceBindings.Count; i++)
                {
                    CharacterAnimationSourceResourceBinding binding = sourceResourceBindings[i];
                    values.Add($"source-resource:{binding?.AuthoringClip?.name}:{binding?.Backend}");
                }
            }
            if (animationProfile != null)
            {
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
            values.Add(rootPath);
            values.Add(AssetDatabase.AssetPathToGUID(rootPath));
            values.Add(AssetDatabase.GetAssetDependencyHash(rootPath).ToString());
        }
    }
}
