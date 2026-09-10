using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterPresentationMotionMatchingCompilationResult
    {
        public CharacterPresentationMotionMatchingCompilationResult(
            MotionMatchingProjectionPayload payload,
            IReadOnlyList<string> diagnostics)
        {
            Payload = payload;
            Diagnostics = diagnostics ?? Array.Empty<string>();
        }

        public MotionMatchingProjectionPayload Payload { get; }
        public IReadOnlyList<string> Diagnostics { get; }
    }

    internal static class CharacterPresentationMotionMatchingCompiler
    {
        public static CharacterPresentationMotionMatchingCompilationResult Compile(
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationBuildInput animationBuildInput,
            CharacterPresentationPoseResourceCompilationCatalog resources)
        {
            var diagnostics = new List<string>();
            if (!profile || !profile.PoseGraph)
                return new CharacterPresentationMotionMatchingCompilationResult(
                    null,
                    diagnostics);
            MotionMatchingProjectionPayload payload =
                CharacterMotionMatchingResourceProjectionCompiler.Compile(
                    profile,
                    animationBuildInput,
                    resources,
                    diagnostics);
            return new CharacterPresentationMotionMatchingCompilationResult(
                payload,
                diagnostics);
        }

        internal static void AppendRevisionValues(
            MotionMatchingProjectionPayload payload,
            List<string> values)
        {
            if (payload == null)
            {
                values.Add("motion-matching:none");
                return;
            }
            values.Add($"motion-matching:{payload.ProfileId.Value}:{payload.ProfileRevision}");
            for (int i = 0; i < payload.DatabaseCount; i++)
            {
                CharacterMotionMatchingDatabaseArtifactIdentity identity =
                    payload.GetDatabase(i).ArtifactIdentity;
                values.Add(
                    $"{identity.DatabaseId.Value}:{identity.DatabaseRevision}:" +
                    $"{identity.AnalysisInputHash}:{identity.OrderedClipDependencyHash}:" +
                    identity.ContentHash);
            }
        }
    }
}
