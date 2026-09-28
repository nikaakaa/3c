using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterPoseAuthoringPersistence
    {
        internal static void Save(CharacterPresentationPoseGraphAsset asset, CharacterAnimationPresentationProfile profile)
        {
            foreach (CharacterPoseCanvasGraph graph in asset.EnumerateGraphs())
            {
                graph.SelfSerialize();
                AssetDatabase.SaveAssetIfDirty(graph);
            }
            AssetDatabase.SaveAssetIfDirty(asset);
            if (!profile)
                return;
            AssetDatabase.SaveAssetIfDirty(profile);
            if (profile.FullBodyIkProfile)
                AssetDatabase.SaveAssetIfDirty(profile.FullBodyIkProfile);
            var owners = profile.PoseResourceBindings
                .Where(value => value?.Resource)
                .Select(value => value.Resource)
                .Where(value => value is CharacterFootPlacementProfile ||
                                value is CharacterAnimationBlendPolicy ||
                                value is CharacterPoseInertializationPolicy);
            foreach (UnityEngine.Object owner in owners.Distinct())
                AssetDatabase.SaveAssetIfDirty(owner);
        }
    }
}
