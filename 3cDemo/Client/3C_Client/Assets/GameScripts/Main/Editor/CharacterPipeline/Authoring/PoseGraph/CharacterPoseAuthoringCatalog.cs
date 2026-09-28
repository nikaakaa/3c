using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Authoring.Graph;
using BTSMTL.Authoring.Editor;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterPoseAuthoringCatalog
    {
        internal static GraphAuthoringDocumentRoleId ResolveRole(CharacterPresentationPoseGraphAsset asset, CharacterPoseCanvasGraph graph)
        {
            if (ReferenceEquals(graph, asset.Graph))
                return CharacterPoseGraphAuthoringCapabilities.RootGraph;
            if (graph.Role != CharacterPoseAuthoringGraphRole.AnimGraph)
                return CharacterPoseGraphAuthoringCapabilities.GetRole(graph.Role);
            bool stateOwned = asset.EnumerateGraphs()
                .SelectMany(value => value.Nodes)
                .Select(value => value?.Payload)
                .OfType<CharacterPoseStateMachineNodePayload>()
                .Any(value => value.StateMachine != null && value.StateMachine.States.Any(state => state.PoseGraphId == graph.GraphId));
            return stateOwned
                ? CharacterPoseGraphAuthoringCapabilities.StatePoseGraph
                : CharacterPoseGraphAuthoringCapabilities.Subgraph;
        }

        internal static string ResolveGraphDisplayName(CharacterPresentationPoseGraphAsset asset, CharacterPoseCanvasGraph graph)
        {
            if (ReferenceEquals(graph, asset.Graph))
                return "Root Pose Graph";
            if (graph.Role == CharacterPoseAuthoringGraphRole.AnimationLayer)
                return "Animation Layer";
            if (graph.Role == CharacterPoseAuthoringGraphRole.ControlRig)
                return "Control Rig";
            if (graph.Role == CharacterPoseAuthoringGraphRole.TransitionRule)
                return "Transition Rule";
            string[] stateNames = asset.EnumerateStateMachines()
                .SelectMany(value => value.States)
                .Where(value => value.PoseGraphId == graph.GraphId)
                .Select(value => CharacterPoseAuthoringDisplayNames.ForIdentity(value.DisplayName))
                .Where(value => !string.IsNullOrWhiteSpace(value) && value != "Unnamed")
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (stateNames.Length == 1)
                return $"{stateNames[0]} Pose Graph";
            if (stateNames.Length > 1)
                return "Shared State Pose Graph";
            string[] subgraphOwners = asset.EnumerateGraphs()
                .Where(value => value != null)
                .SelectMany(value => value.Nodes)
                .Where(value =>
                    value?.Payload is CharacterPoseSubgraphPayload payload &&
                    payload.Subgraph != null &&
                    payload.Subgraph.PoseGraphId == graph.GraphId)
                .Select(value => CharacterPoseAuthoringDisplayNames.ForIdentity(value.DisplayName))
                .Where(value => !string.IsNullOrWhiteSpace(value) && value != "Unnamed")
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (subgraphOwners.Length == 1)
                return $"{subgraphOwners[0]} Subgraph";
            if (subgraphOwners.Length > 1)
                return "Shared Pose Subgraph";
            CharacterPoseCanvasGraph[] graphs = asset.EnumerateGraphs()
                .Where(value => value != null &&
                                !ReferenceEquals(value, asset.Graph))
                .OrderBy(value => value.GraphId)
                .ToArray();
            int index = Array.IndexOf(graphs, graph);
            return $"Pose Graph {Math.Max(index, 0) + 1}";
        }

        internal static (
            CharacterPoseStateMachineDefinition Machine,
            CharacterPoseStateTransition Transition)
            FindTransitionRuleOwner(CharacterPresentationPoseGraphAsset asset, string ruleGraphId)
        {
            var matches = asset.EnumerateGraphs()
                .Where(value => value != null)
                .SelectMany(value => value.Nodes)
                .Select(value => value?.Payload)
                .OfType<CharacterPoseStateMachineNodePayload>()
                .Select(value => value.StateMachine)
                .Where(value => value != null)
                .SelectMany(machine =>
                    machine.Transitions.Select(transition =>
                        (Machine: machine, Transition: transition)))
                .Where(value =>
                    string.Equals(
                        value.Transition.Rule.GraphId.Value,
                        ruleGraphId,
                        StringComparison.Ordinal))
                .ToArray();
            return matches.Length == 1
                ? matches[0]
                : throw new InvalidOperationException(
                    $"Transition Rule graph '{ruleGraphId}' must have exactly one owning Transition.");
        }

        internal static IReadOnlyList<GraphAuthoringNavigatorItem> GetItems(
            CharacterPresentationPoseGraphAsset asset, CharacterAnimationPresentationProfile profile, string linkedPoseStatus)
        {
            var items = asset.EnumerateGraphs()
                .Select(graph => new GraphAuthoringNavigatorItem(
                    new GraphAuthoringElementId(graph.GraphId.Value),
                    ReferenceEquals(graph, asset.Graph)
                        ? "Graphs"
                        : "Graphs / Pose Graphs",
                    ResolveGraphDisplayName(asset, graph),
                    asset.name,
                    graph.ContentRevision,
                    new GraphAuthoringCommandId("open-owner"),
                    string.Join(
                        " ",
                        graph.Nodes.Select(node => node.DisplayName))))
                .ToList();
            foreach (CharacterPoseStateMachineDefinition machine in
                     asset.EnumerateStateMachines()
                         .Where(value => value != null)
                         .OrderBy(
                             value => value.StateMachineId.Value,
                             StringComparer.Ordinal))
            {
                items.Add(new GraphAuthoringNavigatorItem(
                    new GraphAuthoringElementId(
                        "state-machine:" +
                        machine.StateMachineId.Value),
                    "State Machines",
                    CharacterPoseAuthoringDisplayNames.StateMachine(
                        machine),
                    asset.name,
                    machine.ContentRevision,
                    new GraphAuthoringCommandId("open-owner"),
                    string.Join(
                        " ",
                        machine.States.Select(value =>
                            value.DisplayName))));
            }
            AppendLinkedPoseItems(asset, profile, linkedPoseStatus, items);
            return items;
        }

        static void AppendLinkedPoseItems(CharacterPresentationPoseGraphAsset asset, CharacterAnimationPresentationProfile profile, string linkedPoseStatus, List<GraphAuthoringNavigatorItem> items)
        {
            if (!profile)
                return;
            if (profile.LinkedPoseGroups.Count == 0 &&
                profile.LinkedPoseImplementations.Count == 0 &&
                profile.LinkedPoseSelectors.Count == 0 &&
                CharacterLinkedPoseAuthoringService.EnumerateInterfaces(profile).Count == 0)
            {
                items.Add(new GraphAuthoringNavigatorItem(
                    new GraphAuthoringElementId("linked-empty"),
                    "Linked Pose",
                    "Empty · create Interface first · " + linkedPoseStatus,
                    profile.name,
                    string.Empty,
                    new GraphAuthoringCommandId("open-owner"),
                    "Interface → Group → Implementation → Call"));
                return;
            }
            var boundInterfaces = new HashSet<CharacterLinkedPoseInterfaceAsset>(
                profile.LinkedPoseGroups
                    .Where(value => value?.Interface)
                    .Select(value => value.Interface));
            foreach (CharacterLinkedPoseInterfaceAsset linkedInterface in
                     CharacterLinkedPoseAuthoringService.EnumerateInterfaces(profile)
                         .Where(value => !boundInterfaces.Contains(value)))
                items.Add(new GraphAuthoringNavigatorItem(
                    new GraphAuthoringElementId("linked-interface:" + linkedInterface.InterfaceId.Value),
                    "Linked Pose / Contracts",
                    linkedInterface.name + " · " + linkedPoseStatus,
                    profile.name,
                    string.Empty,
                    new GraphAuthoringCommandId("open-owner"),
                    "Unbound Interface · create Group to attach"));
            int groupIndex = 0;
            foreach (CharacterLinkedPoseGroupBinding group in profile.LinkedPoseGroups
                         .Where(value => value != null)
                         .OrderBy(value => value.GroupId))
            {
                string groupId = group.GroupId.Value;
                string groupLabel = group.Interface
                    ? group.Interface.name
                    : $"Group {++groupIndex}";
                string groupStatus = group.Interface && group.Interface.IsStale
                    ? "Stale"
                    : linkedPoseStatus;
                items.Add(new GraphAuthoringNavigatorItem(
                    new GraphAuthoringElementId("linked-group:" + groupId),
                    "Linked Pose / Groups",
                    groupLabel + " · " + groupStatus,
                    profile.name,
                    string.Empty,
                    new GraphAuthoringCommandId("open-owner"),
                    group.Interface ? group.Interface.name : "Missing Interface"));
                if (group.Interface)
                {
                    CharacterLinkedPoseInterfaceAsset linkedInterface = group.Interface;
                    items.Add(new GraphAuthoringNavigatorItem(
                        new GraphAuthoringElementId("linked-interface:" + linkedInterface.InterfaceId.Value),
                        "Linked Pose / " + groupLabel + " / Contract",
                        linkedInterface.name,
                        groupId,
                        linkedInterface.InterfaceId.Value,
                        new GraphAuthoringCommandId("open-owner"),
                        $"{linkedInterface.InterfaceId} {linkedInterface.SignatureHash}"));
                }
                foreach (CharacterLinkedPoseSelectorBindingAsset selector in profile.LinkedPoseSelectors
                             .Where(value => value && value.GroupId == group.GroupId))
                    items.Add(new GraphAuthoringNavigatorItem(
                        new GraphAuthoringElementId("linked-selector:" + selector.SelectorId.Value),
                        "Linked Pose / " + groupLabel + " / Selection",
                        selector.name,
                        groupId,
                        selector.SelectorId.Value,
                        new GraphAuthoringCommandId("open-owner"),
                        string.Join(" ", selector.CandidateImplementationIds.Select(value => value.Value))));
                foreach (CharacterLinkedPoseImplementationAsset implementation in profile.LinkedPoseImplementations
                             .Where(value => value && (!group.Interface || value.Interface == group.Interface)))
                {
                    items.Add(new GraphAuthoringNavigatorItem(
                        new GraphAuthoringElementId("linked-implementation:" + implementation.ImplementationId.Value),
                        "Linked Pose / " + groupLabel + " / Implementations",
                        implementation.name + " · " + (implementation.IsStale ? "Stale" : linkedPoseStatus),
                        groupId,
                        implementation.ImplementationId.Value,
                        new GraphAuthoringCommandId("open-owner"),
                        $"{implementation.ImplementationId} {implementation.Interface?.name}"));
                    foreach (CharacterLinkedPoseInterfaceEntryDescriptor requiredEntry in (implementation.Interface?.Entries ?? Array.Empty<CharacterLinkedPoseInterfaceEntryDescriptor>()).Where(value => value != null))
                    {
                        CharacterLinkedPoseImplementationEntryBinding entry = implementation.Entries
                            .FirstOrDefault(value => value != null && value.EntryId == requiredEntry.EntryId);
                        items.Add(new GraphAuthoringNavigatorItem(
                            new GraphAuthoringElementId("linked-entry:" + implementation.ImplementationId.Value + ":" + requiredEntry.EntryId.Value),
                            "Linked Pose / " + groupLabel + " / Implementations / Entry",
                            (entry == null ? "Missing · " : string.Empty) + EntryDisplayName(requiredEntry.EntryId),
                            implementation.ImplementationId.Value,
                            requiredEntry.EntryId.Value,
                            new GraphAuthoringCommandId("open-owner"),
                            entry == null
                                ? "Required Entry binding is missing."
                                : $"{entry.GraphOwner?.name} {entry.GraphId} {entry.GraphOwnerIdentity}"));
                    }
                }
                foreach (CharacterPoseCanvasNode call in (asset.Graph?.Nodes ?? Array.Empty<CharacterPoseCanvasNode>())
                             .Where(value => value?.Payload is CharacterLinkedPoseCallPayload payload && payload.GroupId == group.GroupId))
                    items.Add(new GraphAuthoringNavigatorItem(
                        new GraphAuthoringElementId("linked-call:" + asset.Graph.GraphId.Value + ":" + call.NodeId.Value),
                        "Linked Pose / " + groupLabel + " / Host Calls",
                        call.DisplayName,
                        asset.Graph.GraphId.Value,
                        call.NodeId.Value,
                        new GraphAuthoringCommandId("open-owner"),
                        call.LinkedPoseEntryId.Value));
                if (group.Interface)
                {
                    foreach (CharacterLinkedPoseInterfaceEntryDescriptor requiredEntry in group.Interface.Entries.Where(value => value != null))
                    {
                        int callCount = (asset.Graph?.Nodes ?? Array.Empty<CharacterPoseCanvasNode>())
                            .Count(value => value?.Payload is CharacterLinkedPoseCallPayload payload &&
                                            payload.GroupId == group.GroupId &&
                                            payload.EntryId == requiredEntry.EntryId);
                        if (callCount == 1)
                            continue;
                        string coverage = callCount == 0 ? "Missing" : "Duplicate";
                        items.Add(new GraphAuthoringNavigatorItem(
                            new GraphAuthoringElementId("linked-call-missing:" + group.GroupId.Value + ":" + requiredEntry.EntryId.Value),
                            "Linked Pose / " + groupLabel + " / Host Calls",
                            coverage + " · " + EntryDisplayName(requiredEntry.EntryId),
                            group.GroupId.Value,
                            requiredEntry.EntryId.Value,
                            new GraphAuthoringCommandId("open-owner"),
                            $"Required Call coverage is {coverage.ToLowerInvariant()} ({callCount})."));
                    }
                }
            }
        }

        static string EntryDisplayName(LinkedPoseEntryId entryId)
        {
            string value = entryId.Value ?? string.Empty;
            int separator = Math.Max(
                value.LastIndexOf('.'),
                Math.Max(value.LastIndexOf('/'), value.LastIndexOf(':')));
            string leaf = separator >= 0 && separator + 1 < value.Length
                ? value.Substring(separator + 1)
                : value;
            leaf = leaf.Replace('-', ' ').Replace('_', ' ').Trim();
            return string.IsNullOrEmpty(leaf)
                ? "Entry"
                : char.ToUpperInvariant(leaf[0]) + leaf.Substring(1);
        }

    }
}
