using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterPresentationPoseSourceCompilationEntry
    {
        public CharacterPresentationPoseSourceCompilationEntry(
            PresentationPoseSourceIndex sourceIndex,
            CharacterPresentationPoseSourceSlot slot,
            CharacterPresentationPoseSourceBinding binding)
        {
            SourceIndex = sourceIndex;
            Slot = slot;
            Binding = binding;
        }

        public PresentationPoseSourceIndex SourceIndex { get; }
        public CharacterPresentationPoseSourceSlot Slot { get; }
        public CharacterPresentationPoseSourceBinding Binding { get; }
    }

    internal sealed class CharacterPresentationPoseSourceCompilationCatalog
    {
        readonly Dictionary<CharacterPresentationPoseSourceSlot,
            CharacterPresentationPoseSourceCompilationEntry> m_BySlot;

        public CharacterPresentationPoseSourceCompilationCatalog(
            CharacterPresentationPoseSourceCompilationEntry[] entries)
        {
            Entries = entries ??
                Array.Empty<CharacterPresentationPoseSourceCompilationEntry>();
            m_BySlot = Entries.ToDictionary(value => value.Slot);
        }

        public IReadOnlyList<CharacterPresentationPoseSourceCompilationEntry> Entries { get; }
        public IReadOnlyDictionary<CharacterPresentationPoseSourceSlot, PresentationPoseSourceIndex>
            SourceIndices =>
            m_BySlot.ToDictionary(value => value.Key, value => value.Value.SourceIndex);

        public bool TryGet(
            CharacterPresentationPoseSourceSlot slot,
            out CharacterPresentationPoseSourceCompilationEntry entry) =>
            m_BySlot.TryGetValue(slot, out entry);
    }

    internal sealed class CharacterPresentationPoseSourceCompilationResult
    {
        public CharacterPresentationPoseSourceCompilationResult(
            CharacterPresentationPoseSourceCompilationCatalog catalog,
            IReadOnlyList<string> diagnostics)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Diagnostics = diagnostics ?? Array.Empty<string>();
        }

        public CharacterPresentationPoseSourceCompilationCatalog Catalog { get; }
        public IReadOnlyList<string> Diagnostics { get; }
    }

    internal static class CharacterPresentationPoseSourceCompiler
    {
        public static CharacterPresentationPoseSourceCompilationResult Compile(
            CharacterAnimationPresentationProfile profile)
        {
            var diagnostics = new List<string>();
            CharacterPresentationPoseSourceCompilationCatalog catalog =
                CompileCatalog(profile, diagnostics);
            return new CharacterPresentationPoseSourceCompilationResult(
                catalog,
                diagnostics);
        }

        static CharacterPresentationPoseSourceCompilationCatalog CompileCatalog(
            CharacterAnimationPresentationProfile profile,
            List<string> diagnostics)
        {
            if (!profile || !profile.PoseGraph)
            {
                diagnostics.Add(
                    "Presentation Profile has no Pose Graph for Pose Source compilation.");
                return new CharacterPresentationPoseSourceCompilationCatalog(
                    Array.Empty<CharacterPresentationPoseSourceCompilationEntry>());
            }

            string profilePath = AssetDatabase.GetAssetPath(profile);
            var ownedSlots = new HashSet<CharacterPresentationPoseSourceSlot>();
            CharacterPresentationPoseGraphAsset[] graphOwners =
                EnumeratePoseGraphOwners(profile)
                    .Distinct()
                    .OrderBy(
                        CharacterPresentationAssetObjectIdentity.Require,
                        StringComparer.Ordinal)
                    .ToArray();
            for (int ownerIndex = 0; ownerIndex < graphOwners.Length; ownerIndex++)
            {
                CharacterPresentationPoseGraphAsset graphOwner = graphOwners[ownerIndex];
                string graphPath = AssetDatabase.GetAssetPath(graphOwner);
                for (int slotIndex = 0;
                     slotIndex < graphOwner.SourceSlots.Count;
                     slotIndex++)
                {
                    CharacterPresentationPoseSourceSlot slot =
                        graphOwner.SourceSlots[slotIndex];
                    if (!slot || !ownedSlots.Add(slot) ||
                        !string.Equals(
                            AssetDatabase.GetAssetPath(slot),
                            graphPath,
                            StringComparison.Ordinal))
                    {
                        diagnostics.Add(
                            $"Pose Graph '{graphOwner.name}' Source Slot #{slotIndex} is missing, duplicated or not owned by that asset.");
                        continue;
                    }
                    try
                    {
                        slot.RequireValid();
                    }
                    catch (Exception exception)
                    {
                        diagnostics.Add(
                            $"Pose Graph Source Slot '{slot.name}' is invalid: {exception.Message}");
                    }
                }
            }

            CharacterPresentationPoseSourceSlot[] reachable =
                EnumerateReachablePoseGraphs(profile)
                    .Where(value => value != null)
                    .SelectMany(value => value.Nodes)
                    .Where(value => value != null && value.PresentationPoseSourceSlot)
                    .Select(value => value.PresentationPoseSourceSlot)
                    .Distinct()
                    .ToArray();
            Array.Sort(
                reachable,
                (left, right) => string.CompareOrdinal(
                    CharacterPresentationAssetObjectIdentity.Require(left),
                    CharacterPresentationAssetObjectIdentity.Require(right)));

            var bindingsBySlot =
                new Dictionary<CharacterPresentationPoseSourceSlot,
                    CharacterPresentationPoseSourceBinding>();
            for (int i = 0; i < profile.PoseSourceBindings.Count; i++)
            {
                CharacterPresentationPoseSourceBinding binding =
                    profile.PoseSourceBindings[i];
                if (!binding || !binding.Slot ||
                    !string.Equals(
                        AssetDatabase.GetAssetPath(binding),
                        profilePath,
                        StringComparison.Ordinal) ||
                    !bindingsBySlot.TryAdd(binding.Slot, binding))
                {
                    diagnostics.Add(
                        $"Presentation Profile Pose Source binding #{i} is missing, duplicated or not owned by the Profile asset.");
                    continue;
                }
                if (!ownedSlots.Contains(binding.Slot) ||
                    !binding.Slot.Accepts(binding))
                {
                    diagnostics.Add(
                        $"Pose Source binding '{binding.name}' references a foreign or type-incompatible Slot.");
                }
                try
                {
                    binding.RequireValid(profile.RigDefinition);
                }
                catch (Exception exception)
                {
                    diagnostics.Add(
                        $"Pose Source binding '{binding.name}' is invalid: {exception.Message}");
                }
            }

            var entries =
                new CharacterPresentationPoseSourceCompilationEntry[reachable.Length];
            for (int i = 0; i < reachable.Length; i++)
            {
                CharacterPresentationPoseSourceSlot slot = reachable[i];
                if (!ownedSlots.Contains(slot))
                    diagnostics.Add(
                        $"Pose Player references Source Slot '{slot.name}' outside the Pose Graph owner.");
                if (!bindingsBySlot.TryGetValue(
                        slot,
                        out CharacterPresentationPoseSourceBinding binding))
                {
                    diagnostics.Add(
                        $"Pose Source Slot '{slot.name}' has no Profile binding.");
                }
                entries[i] = new CharacterPresentationPoseSourceCompilationEntry(
                    new PresentationPoseSourceIndex(i),
                    slot,
                    binding);
            }

            foreach (KeyValuePair<CharacterPresentationPoseSourceSlot,
                         CharacterPresentationPoseSourceBinding> pair in bindingsBySlot)
            {
                if (!reachable.Contains(pair.Key))
                    diagnostics.Add(
                        $"Pose Source binding '{pair.Value.name}' is orphaned from every reachable Pose Player.");
            }
            return new CharacterPresentationPoseSourceCompilationCatalog(entries);
        }

        static IEnumerable<CharacterPresentationPoseGraphAsset>
            EnumeratePoseGraphOwners(CharacterAnimationPresentationProfile profile)
        {
            yield return profile.PoseGraph;
            for (int implementationIndex = 0;
                 implementationIndex < profile.LinkedPoseImplementations.Count;
                 implementationIndex++)
            {
                CharacterLinkedPoseImplementationAsset implementation =
                    profile.LinkedPoseImplementations[implementationIndex];
                if (!implementation)
                    continue;
                for (int entryIndex = 0;
                     entryIndex < implementation.Entries.Count;
                     entryIndex++)
                {
                    CharacterPresentationPoseGraphAsset graphOwner =
                        implementation.Entries[entryIndex]?.GraphOwner;
                    if (graphOwner)
                        yield return graphOwner;
                }
            }
        }

        static IEnumerable<CharacterPoseCanvasGraph>
            EnumerateReachablePoseGraphs(CharacterAnimationPresentationProfile profile)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (CharacterPoseCanvasGraph graph in
                     EnumerateReachablePoseGraphs(
                         profile.PoseGraph,
                         profile.PoseGraph.Graph,
                         visited))
            {
                yield return graph;
            }
            for (int implementationIndex = 0;
                 implementationIndex < profile.LinkedPoseImplementations.Count;
                 implementationIndex++)
            {
                CharacterLinkedPoseImplementationAsset implementation =
                    profile.LinkedPoseImplementations[implementationIndex];
                if (!implementation)
                    continue;
                for (int entryIndex = 0;
                     entryIndex < implementation.Entries.Count;
                     entryIndex++)
                {
                    CharacterLinkedPoseImplementationEntryBinding entry =
                        implementation.Entries[entryIndex];
                    if (entry == null || !entry.GraphOwner)
                        continue;
                    CharacterPoseCanvasGraph entryGraph =
                        entry.GraphOwner.RequireGraph(entry.GraphId);
                    foreach (CharacterPoseCanvasGraph graph in
                             EnumerateReachablePoseGraphs(
                                 entry.GraphOwner,
                                 entryGraph,
                                 visited))
                    {
                        yield return graph;
                    }
                }
            }
        }

        static IEnumerable<CharacterPoseCanvasGraph>
            EnumerateReachablePoseGraphs(
                CharacterPresentationPoseGraphAsset owner,
                CharacterPoseCanvasGraph graph,
                HashSet<string> visited)
        {
            string key = CharacterPresentationAssetObjectIdentity.Require(owner) +
                         "\0" +
                         graph.GraphId.Value;
            if (!visited.Add(key))
                yield break;
            yield return graph;
            for (int nodeIndex = 0; nodeIndex < graph.Nodes.Count; nodeIndex++)
            {
                CharacterPoseCanvasNode node = graph.Nodes[nodeIndex];
                if (node?.Payload is CharacterPoseSubgraphPayload subgraph &&
                    subgraph.Subgraph != null &&
                    subgraph.Subgraph.PoseGraphId.IsValid)
                {
                    foreach (CharacterPoseCanvasGraph child in
                             EnumerateReachablePoseGraphs(
                                 owner,
                                 owner.RequireGraph(
                                     subgraph.Subgraph.PoseGraphId),
                                 visited))
                    {
                        yield return child;
                    }
                    continue;
                }
                if (node?.Payload is not CharacterPoseStateMachineNodePayload stateMachine ||
                    stateMachine.StateMachine == null)
                    continue;
                for (int stateIndex = 0;
                     stateIndex < stateMachine.StateMachine.States.Count;
                     stateIndex++)
                {
                    CharacterPoseStateDefinition state =
                        stateMachine.StateMachine.States[stateIndex];
                    if (state == null || !state.PoseGraphId.IsValid)
                        continue;
                    foreach (CharacterPoseCanvasGraph child in
                             EnumerateReachablePoseGraphs(
                                 owner,
                                 owner.RequireGraph(state.PoseGraphId),
                                 visited))
                    {
                        yield return child;
                    }
                }
            }
        }
    }
}
