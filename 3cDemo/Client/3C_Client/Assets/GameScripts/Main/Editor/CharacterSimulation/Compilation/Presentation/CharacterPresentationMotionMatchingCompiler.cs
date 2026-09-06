using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Editor.MotionMatching;
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
            CharacterAnimationPresentationProfile profile)
        {
            var diagnostics = new List<string>();
            if (!profile || !profile.PoseGraph)
                return new CharacterPresentationMotionMatchingCompilationResult(
                    null,
                    diagnostics);
            CharacterMotionMatchingBinding[] bindings = profile.PoseGraph
                .EnumerateGraphs()
                .SelectMany(value => value.Nodes)
                .Select(value =>
                    (value?.Payload as CharacterMotionMatchingPosePayload)?.Binding)
                .Where(value => value)
                .Distinct()
                .ToArray();
            if (bindings.Length == 0)
                return new CharacterPresentationMotionMatchingCompilationResult(
                    null,
                    diagnostics);
            CharacterMotionMatchingProfile[] profiles = bindings
                .Select(value => value.Profile)
                .Where(value => value)
                .Distinct()
                .ToArray();
            if (profiles.Length != 1)
            {
                diagnostics.Add(
                    "Motion Matching Pose nodes must resolve one exact Motion Matching Profile.");
                return new CharacterPresentationMotionMatchingCompilationResult(
                    null,
                    diagnostics);
            }
            if (profile.FootPlacementAnalysisMode !=
                    CharacterFootPlacementAnalysisMode.GeneratedPerFootFeatures ||
                !CharacterFootPlacementAnalysisSource.IsAssetGuid(
                    profile.FootPlacementAnalysisSourceAssetGuid))
            {
                diagnostics.Add(
                    "Motion Matching Projection requires the Presentation Profile generated Foot Analysis Source.");
                return new CharacterPresentationMotionMatchingCompilationResult(
                    null,
                    diagnostics);
            }
            string path = AssetDatabase.GUIDToAssetPath(
                profile.FootPlacementAnalysisSourceAssetGuid);
            CharacterFootPlacementAnalysisSource analysisSource =
                AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(path);
            if (!analysisSource)
            {
                diagnostics.Add(
                    "Motion Matching Projection Foot Analysis Source is missing.");
                return new CharacterPresentationMotionMatchingCompilationResult(
                    null,
                    diagnostics);
            }
            try
            {
                MotionMatchingProjectionPayload payload =
                    MotionMatchingProjectionPayloadCompiler.Compile(
                        profiles[0],
                        profile.PoseGraph,
                        profile.RigDefinition,
                        analysisSource,
                        AnimationClipMotionMatchingParameterCurveResolver.Instance);
                return new CharacterPresentationMotionMatchingCompilationResult(
                    payload,
                    diagnostics);
            }
            catch (Exception exception)
            {
                diagnostics.Add(exception.Message);
                return new CharacterPresentationMotionMatchingCompilationResult(
                    null,
                    diagnostics);
            }
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
