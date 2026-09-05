using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterAnimationResourceClosureEntry
    {
        internal CharacterAnimationResourceClosureEntry(
            AnimationClip clip,
            CharacterAnimationClipContentIdentity identity,
            string sourceCategory,
            CharacterAnimationSamplingBackendKind currentBackend)
        {
            Clip = clip ? clip : throw new ArgumentNullException(nameof(clip));
            Identity = identity;
            SourceCategory = sourceCategory ?? string.Empty;
            CurrentBackend = currentBackend;
        }

        internal AnimationClip Clip { get; }
        internal CharacterAnimationClipContentIdentity Identity { get; }
        internal string SourceCategory { get; }
        internal CharacterAnimationSamplingBackendKind CurrentBackend { get; }
    }

    internal static class CharacterAnimationResourceClosureAnalyzer
    {
        internal static IReadOnlyList<CharacterAnimationResourceClosureEntry> Analyze(
            CharacterPipelineDefinition definition,
            CharacterAnimationPresentationProfile profile)
        {
            CharacterSemanticFrontendResult frontend = CharacterSemanticFrontendCompiler.Compile(definition);
            if (!frontend.IsValid)
            {
                throw new InvalidOperationException(
                    "Character Definition Semantic Frontend is invalid:\n" +
                    string.Join("\n", frontend.Report.Messages.Select(value => value.ToString())));
            }
            if (frontend.CompilationModel.AnimationPresentationProfile != profile)
            {
                throw new InvalidOperationException(
                    "Character Definition Semantic Frontend uses a different AnimationPresentationProfile.");
            }

            var categories = new Dictionary<AnimationClip, HashSet<string>>();
            foreach (CharacterTypedPoseGraph graph in EnumerateReachablePoseGraphs(profile))
            {
                foreach (CharacterTypedPoseNode node in graph.Nodes)
                {
                    CharacterPresentationPoseSourceSlot slot = node?.PresentationPoseSourceSlot;
                    if (!slot)
                        continue;
                    CharacterPresentationPoseSourceBinding binding =
                        profile.FindPoseSourceBinding(slot) ??
                        throw new InvalidOperationException(
                            $"Pose Source Slot '{slot.name}' has no Profile binding.");
                    var clips = new HashSet<AnimationClip>();
                    clips.GatherFromSource(binding.SourceAsset);
                    if (clips.Count == 0 && binding.SourceAsset is AnimationClip singleClip)
                        clips.Add(singleClip);
                    if (clips.Count == 0)
                        throw new InvalidOperationException(
                            $"Pose Source binding '{binding.name}' has no AnimationClip in its source closure.");
                    foreach (AnimationClip clip in clips)
                        AddCategory(categories, clip, "pose-source");
                }
            }

            foreach (AnimationProducerAuthoringEntry producer in
                     CharacterAnimationPresentationAuthoringService.DiscoverProducerTracks(definition))
            {
                foreach (AnimationProducerSourceClipAuthoringEntry source in producer.SourceClips)
                    AddCategory(categories, source.Clip, "producer");
            }

            var clipsByIdentity = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
            var categoriesByIdentity = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (KeyValuePair<AnimationClip, HashSet<string>> pair in categories)
            {
                string identity = ClipIdentity(pair.Key);
                if (!clipsByIdentity.TryAdd(identity, pair.Key))
                    throw new InvalidOperationException(
                        $"Authoring animation closure contains duplicate Clip identity '{identity}'.");
                categoriesByIdentity.Add(identity, pair.Value);
            }

            var result = new List<CharacterAnimationResourceClosureEntry>(clipsByIdentity.Count);
            foreach (KeyValuePair<string, AnimationClip> pair in clipsByIdentity)
            {
                AnimationClip clip = pair.Value;
                string path = RequireAssetPath(clip, ".anim", "AnimationClip");
                if (AssetDatabase.LoadMainAssetAtPath(path) != clip)
                    throw new InvalidOperationException(
                        $"AnimationClip '{path}' must be the persisted native main asset.");
                CharacterAnimationClipContentIdentity identity =
                    CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(clip);
                CharacterAnimationSourceResourceBinding resource =
                    profile.FindSourceResourceBinding(clip);
                result.Add(new CharacterAnimationResourceClosureEntry(
                    clip,
                    identity,
                    string.Join(
                        "+",
                        categoriesByIdentity[pair.Key].OrderBy(value => value, StringComparer.Ordinal)),
                    resource?.Backend ?? CharacterAnimationSamplingBackendKind.NativeClip));
            }
            result.Sort((left, right) =>
            {
                int compare = string.CompareOrdinal(left.Identity.AssetGuid, right.Identity.AssetGuid);
                return compare != 0
                    ? compare
                    : left.Identity.LocalFileId.CompareTo(right.Identity.LocalFileId);
            });
            return result.ToArray();
        }

        static string ClipIdentity(AnimationClip clip)
        {
            CharacterAnimationClipContentIdentity identity =
                CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(clip);
            return identity.AssetGuid + ":" + identity.LocalFileId;
        }

        static void AddCategory(
            IDictionary<AnimationClip, HashSet<string>> categories,
            AnimationClip clip,
            string category)
        {
            if (!clip)
                throw new InvalidOperationException("Animation source closure contains a missing AnimationClip.");
            if (!categories.TryGetValue(clip, out HashSet<string> values))
            {
                values = new HashSet<string>(StringComparer.Ordinal);
                categories.Add(clip, values);
            }
            values.Add(category);
        }

        static IReadOnlyList<CharacterTypedPoseGraph> EnumerateReachablePoseGraphs(
            CharacterAnimationPresentationProfile profile)
        {
            var result = new List<CharacterTypedPoseGraph>();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            if (profile.PoseGraph?.Graph != null)
                CollectReachablePoseGraphs(profile.PoseGraph, profile.PoseGraph.Graph, visited, result);
            foreach (CharacterLinkedPoseImplementationAsset implementation in profile.LinkedPoseImplementations)
            {
                if (!implementation)
                    continue;
                foreach (CharacterLinkedPoseImplementationEntryBinding entry in implementation.Entries)
                {
                    if (entry?.GraphOwner && entry.GraphId.IsValid)
                    {
                        CollectReachablePoseGraphs(
                            entry.GraphOwner,
                            entry.GraphOwner.RequireGraph(entry.GraphId),
                            visited,
                            result);
                    }
                }
            }
            return result;
        }

        static void CollectReachablePoseGraphs(
            CharacterPresentationPoseGraphAsset owner,
            CharacterTypedPoseGraph graph,
            ISet<string> visited,
            ICollection<CharacterTypedPoseGraph> result)
        {
            string key = AssetDatabase.GetAssetPath(owner) + "\0" + graph.GraphId.Value;
            if (!visited.Add(key))
                return;
            result.Add(graph);
            foreach (CharacterTypedPoseNode node in graph.Nodes)
            {
                if (node?.Payload is CharacterPoseSubgraphPayload subgraph &&
                    subgraph.Subgraph != null &&
                    subgraph.Subgraph.PoseGraphId.IsValid)
                {
                    CollectReachablePoseGraphs(
                        owner,
                        owner.RequireGraph(subgraph.Subgraph.PoseGraphId),
                        visited,
                        result);
                    continue;
                }
                if (node?.Payload is not CharacterPoseStateMachineNodePayload stateMachine ||
                    stateMachine.StateMachine == null)
                    continue;
                foreach (CharacterPoseStateDefinition state in stateMachine.StateMachine.States)
                {
                    if (state != null && state.PoseGraphId.IsValid)
                    {
                        CollectReachablePoseGraphs(
                            owner,
                            owner.RequireGraph(state.PoseGraphId),
                            visited,
                            result);
                    }
                }
            }
        }

        static string RequireAssetPath(
            UnityEngine.Object asset,
            string extension,
            string typeName)
        {
            if (!asset)
                throw new ArgumentNullException(typeName);
            string path = AssetDatabase.GetAssetPath(asset)?.Replace('\\', '/') ?? string.Empty;
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ||
                path.Contains(".."))
            {
                throw new InvalidOperationException(
                    $"{typeName} path '{path}' must be a persisted Assets/...{extension} asset.");
            }
            return path;
        }
    }
}
