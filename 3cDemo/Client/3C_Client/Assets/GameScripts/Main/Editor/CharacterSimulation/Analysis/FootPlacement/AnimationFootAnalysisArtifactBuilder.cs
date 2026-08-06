using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class AnimationFootAnalysisArtifactBuilder
    {
        public static AnimationFootAnalysisArtifactIdentity GetExpectedIdentity(
            UnityEngine.AnimationClip clip,
            CharacterFootPlacementAnalysisSource source)
        {
            CharacterFootPlacementRigGeometryValidationPublisher.RequireCurrent(source);
            return AnimationFootAnalysisArtifactIdentityBuilder.Build(clip, source);
        }

        public static AnimationFootAnalysisArtifactInspection Inspect(
            UnityEngine.AnimationClip clip,
            CharacterFootPlacementAnalysisSource source)
        {
            return AnimationFootAnalysisArtifactStore.Inspect(GetExpectedIdentity(clip, source));
        }

        public static AnimationFootAnalysisArtifact Build(
            UnityEngine.AnimationClip clip,
            CharacterFootPlacementAnalysisSource source)
        {
            if (!clip)
                throw new ArgumentNullException(nameof(clip));
            if (!source)
                throw new ArgumentNullException(nameof(source));
            AnimationFootAnalysisArtifactIdentity identity = GetExpectedIdentity(clip, source);
            AnimationFootFeaturePair features = CharacterFootPlacementAnimationAnalyzer.Analyze(clip, source);
            return AnimationFootAnalysisArtifactStore.Write(identity, features);
        }
    }
}
