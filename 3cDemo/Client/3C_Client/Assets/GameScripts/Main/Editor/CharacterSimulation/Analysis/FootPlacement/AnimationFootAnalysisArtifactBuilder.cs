using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class AnimationFootAnalysisArtifactBuilder
    {
        public static AnimationFootAnalysisArtifactIdentity GetExpectedIdentity(
            UnityEngine.AnimationClip clip,
            CharacterFootPlacementAnalysisSource source,
            AnimationFootContactSchedule contactSchedule = null)
        {
            CharacterFootPlacementRigGeometryValidationPublisher.RequireCurrent(source);
            return AnimationFootAnalysisArtifactIdentityBuilder.Build(
                clip,
                source,
                contactSchedule ?? AnimationFootContactSchedule.Inferred);
        }

        public static AnimationFootAnalysisArtifactInspection Inspect(
            UnityEngine.AnimationClip clip,
            CharacterFootPlacementAnalysisSource source,
            AnimationFootContactSchedule contactSchedule = null)
        {
            return AnimationFootAnalysisArtifactStore.Inspect(
                GetExpectedIdentity(clip, source, contactSchedule));
        }

        public static AnimationFootAnalysisArtifact Build(
            UnityEngine.AnimationClip clip,
            CharacterFootPlacementAnalysisSource source,
            AnimationFootContactSchedule contactSchedule = null)
        {
            if (!clip)
                throw new ArgumentNullException(nameof(clip));
            if (!source)
                throw new ArgumentNullException(nameof(source));
            AnimationFootContactSchedule schedule = contactSchedule ?? AnimationFootContactSchedule.Inferred;
            AnimationFootAnalysisArtifactIdentity identity = GetExpectedIdentity(clip, source, schedule);
            AnimationFootAnalysisBuildResult result = CharacterFootPlacementAnimationAnalyzer.Analyze(clip, source, schedule);
            return AnimationFootAnalysisArtifactStore.Write(
                identity,
                result.Features,
                result.Synchronization);
        }
    }
}
