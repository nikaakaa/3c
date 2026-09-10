using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class CharacterPoseTopologyGraph
    {
        internal CharacterPoseTopologyGraph(
            CharacterPoseTypedIrGraph typedIr,
            CharacterPoseIrGraphRole role,
            CharacterPoseIrGraph orderedIr)
        {
            TypedIr = typedIr ??
                throw new ArgumentNullException(nameof(typedIr));
            if (!Enum.IsDefined(typeof(CharacterPoseIrGraphRole), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            Role = role;
            OrderedIr = orderedIr ??
                throw new ArgumentNullException(nameof(orderedIr));
            if (OrderedIr.GraphId != TypedIr.Source.GraphId ||
                !string.Equals(
                    OrderedIr.SourceRevision,
                    TypedIr.Source.ContentRevision,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pose Topology Graph does not match Typed IR.");
            }
        }

        internal CharacterPoseTypedIrGraph TypedIr { get; }
        internal CharacterPoseIrGraphRole Role { get; }
        internal CharacterPoseIrGraph OrderedIr { get; }
    }

    internal sealed class CharacterPoseTopologyCatalog
    {
        readonly Dictionary<string, CharacterPoseTopologyGraph> m_Graphs;

        internal CharacterPoseTopologyCatalog(
            IReadOnlyList<CharacterPoseTopologyGraph> graphs)
        {
            Graphs = graphs?.ToArray() ??
                throw new ArgumentNullException(nameof(graphs));
            m_Graphs = Graphs.ToDictionary(
                value => Key(
                    value.TypedIr.Closure.OwnerIdentity,
                    value.TypedIr.Source.GraphId,
                    value.Role),
                StringComparer.Ordinal);
        }

        internal IReadOnlyList<CharacterPoseTopologyGraph> Graphs { get; }

        internal CharacterPoseIrGraph RequireGraph(
            CharacterPresentationPoseGraphAsset owner,
            PoseGraphId graphId,
            CharacterPoseIrGraphRole role)
        {
            string ownerIdentity =
                CharacterPresentationAssetObjectIdentity.Require(owner);
            string key = Key(ownerIdentity, graphId, role);
            return m_Graphs.TryGetValue(key, out CharacterPoseTopologyGraph graph)
                ? graph.OrderedIr
                : throw new InvalidOperationException(
                    $"Pose Topology catalog has no {role} Graph '{graphId}' for '{ownerIdentity}'.");
        }

        static string Key(
            string ownerIdentity,
            PoseGraphId graphId,
            CharacterPoseIrGraphRole role) =>
            CharacterPoseGraphClosure.Key(ownerIdentity, graphId) +
            "\0" +
            ((byte)role).ToString();
    }

    internal sealed class CharacterPoseTopologyPassResult
    {
        internal CharacterPoseTopologyPassResult(
            CharacterPoseTopologyCatalog catalog,
            IReadOnlyList<CharacterPoseCompilationDiagnostic> diagnostics)
        {
            Catalog = catalog;
            Diagnostics = diagnostics?.ToArray() ??
                Array.Empty<CharacterPoseCompilationDiagnostic>();
            IsSuccess = catalog != null &&
                Diagnostics.All(value =>
                    value.Severity !=
                    CharacterPoseCompilationDiagnosticSeverity.Error);
        }

        internal bool IsSuccess { get; }
        internal CharacterPoseTopologyCatalog Catalog { get; }
        internal IReadOnlyList<CharacterPoseCompilationDiagnostic>
            Diagnostics { get; }
    }

    internal static class CharacterPoseTopologyPass
    {
        internal static CharacterPoseTopologyPassResult Run(
            CharacterPoseCompilationRequest request,
            CharacterPoseGraphClosure closure,
            CharacterPoseTypedIrCatalog typedIr)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (closure == null)
                throw new ArgumentNullException(nameof(closure));
            if (typedIr == null)
                throw new ArgumentNullException(nameof(typedIr));
            try
            {
                Dictionary<string, HashSet<CharacterPoseIrGraphRole>> roles =
                    BuildRoles(request, closure);
                var graphs = new List<CharacterPoseTopologyGraph>();
                var diagnostics =
                    new List<CharacterPoseCompilationDiagnostic>();
                AnimationChannelId[] reachableAnimationChannels =
                    request.AnimationInputContract.Slots
                        .Select(value => value.AnimationChannelId)
                        .Distinct()
                        .OrderBy(value => value)
                        .ToArray();
                CharacterPresentationPoseSourceSlot[] reachableSources =
                    request.SourceIndices.Keys.ToArray();
                for (int entryIndex = 0;
                     entryIndex < closure.Entries.Count;
                     entryIndex++)
                {
                    CharacterPoseGraphClosureEntry entry =
                        closure.Entries[entryIndex];
                    string key = CharacterPoseGraphClosure.Key(
                        entry.OwnerIdentity,
                        entry.Graph.GraphId);
                    if (!roles.TryGetValue(
                            key,
                            out HashSet<CharacterPoseIrGraphRole> graphRoles) ||
                        graphRoles.Count == 0)
                    {
                        throw new InvalidOperationException(
                            $"Graph Closure entry '{key}' has no Graph Role.");
                    }
                    CharacterPoseTypedIrGraph typedGraph =
                        typedIr.RequireGraph(
                            entry.Owner,
                            entry.Graph.GraphId);
                    foreach (CharacterPoseIrGraphRole role in
                             graphRoles.OrderBy(value => (byte)value))
                    {
                        CharacterPoseGraphValidationReport validation =
                            CharacterPoseTopologyValidator
                                .ValidateClosedGraph(
                                    entry.Owner,
                                    entry.Graph,
                                    request.Rig,
                                    CharacterPoseAuthoringPortProjection.Get,
                                    closure,
                                    role,
                                    role == CharacterPoseIrGraphRole.Root
                                        ? reachableAnimationChannels
                                        : null,
                                    role ==
                                    CharacterPoseIrGraphRole.MotionMatchingEntry
                                        ? null
                                        : reachableSources);
                        for (int issueIndex = 0;
                             issueIndex < validation.Issues.Count;
                             issueIndex++)
                        {
                            CharacterPoseGraphValidationIssue issue =
                                validation.Issues[issueIndex];
                            diagnostics.Add(
                                new CharacterPoseCompilationDiagnostic(
                                    CharacterPoseCompilationPass.Topology,
                                    CharacterPoseCompilationDiagnosticSeverity.Error,
                                    issue.Code.ToString(),
                                    issue.Message,
                                    string.IsNullOrWhiteSpace(issue.GraphId)
                                        ? default
                                        : new PoseGraphId(issue.GraphId),
                                    issue.NodeId,
                                    issue.PortId));
                        }
                        if (!validation.IsValid)
                            continue;
                        CharacterPoseIrGraph ordered =
                            new CharacterPoseIrTopologyCompiler().Compile(
                                typedGraph,
                                role);
                        graphs.Add(new CharacterPoseTopologyGraph(
                            typedGraph,
                            role,
                            ordered));
                    }
                }
                if (diagnostics.Count != 0)
                {
                    return new CharacterPoseTopologyPassResult(
                        null,
                        diagnostics);
                }
                RequireProgramBoundaries(request, typedIr);
                return new CharacterPoseTopologyPassResult(
                    new CharacterPoseTopologyCatalog(graphs),
                    Array.Empty<CharacterPoseCompilationDiagnostic>());
            }
            catch (Exception exception)
            {
                return new CharacterPoseTopologyPassResult(
                    null,
                    new[]
                    {
                        new CharacterPoseCompilationDiagnostic(
                            CharacterPoseCompilationPass.Topology,
                            CharacterPoseCompilationDiagnosticSeverity.Error,
                            "topology-invalid",
                            exception.Message,
                            request.AuthoringView.RootGraph.GraphId)
                    });
            }
        }

        static Dictionary<string, HashSet<CharacterPoseIrGraphRole>>
            BuildRoles(
                CharacterPoseCompilationRequest request,
                CharacterPoseGraphClosure closure)
        {
            var roles = new Dictionary<string,
                HashSet<CharacterPoseIrGraphRole>>(StringComparer.Ordinal);
            string rootOwner =
                CharacterPresentationAssetObjectIdentity.Require(
                    request.AuthoringView.OwnerAsset);
            Add(
                CharacterPoseGraphClosure.Key(
                    rootOwner,
                    request.AuthoringView.RootGraph.GraphId),
                CharacterPoseIrGraphRole.Root);
            foreach (CharacterPoseGraphClosureEntry entry in closure.Entries)
            {
                if (entry.Graph.Role == CharacterPoseAuthoringGraphRole.AnimationLayer)
                    Add(
                        CharacterPoseGraphClosure.Key(
                            entry.OwnerIdentity,
                            entry.Graph.GraphId),
                        CharacterPoseIrGraphRole.AnimationLayer);
                else if (entry.Graph.Role == CharacterPoseAuthoringGraphRole.ControlRig)
                    Add(
                        CharacterPoseGraphClosure.Key(
                            entry.OwnerIdentity,
                            entry.Graph.GraphId),
                        CharacterPoseIrGraphRole.ControlRig);
            }
            for (int referenceIndex = 0;
                 referenceIndex < closure.References.Count;
                 referenceIndex++)
            {
                CharacterPoseGraphClosureReference reference =
                    closure.References[referenceIndex];
                CharacterPoseGraphClosureEntry explicitTarget =
                    closure.Entries.FirstOrDefault(value =>
                        value.OwnerIdentity == reference.TargetOwnerIdentity &&
                        value.Graph.GraphId == reference.TargetGraphId);
                if (explicitTarget != null &&
                    explicitTarget.Graph.Role != CharacterPoseAuthoringGraphRole.AnimGraph)
                    continue;
                CharacterPoseIrGraphRole role = reference.Kind switch
                {
                    CharacterPoseGraphDependencyKind.StatePose =>
                        CharacterPoseIrGraphRole.StateLocal,
                    CharacterPoseGraphDependencyKind.Subgraph =>
                        CharacterPoseIrGraphRole.Subgraph,
                    CharacterPoseGraphDependencyKind.MotionMatchingEntry =>
                        CharacterPoseIrGraphRole.MotionMatchingEntry,
                    CharacterPoseGraphDependencyKind.LinkedPoseEntry =>
                        CharacterPoseIrGraphRole.LinkedPoseEntry,
                    _ => throw new InvalidOperationException(
                        $"Graph dependency kind '{reference.Kind}' has no Graph Role.")
                };
                Add(
                    CharacterPoseGraphClosure.Key(
                        reference.TargetOwnerIdentity,
                        reference.TargetGraphId),
                    role);
            }
            return roles;

            void Add(string key, CharacterPoseIrGraphRole role)
            {
                if (!roles.TryGetValue(
                        key,
                        out HashSet<CharacterPoseIrGraphRole> values))
                {
                    values = new HashSet<CharacterPoseIrGraphRole>();
                    roles.Add(key, values);
                }
                values.Add(role);
            }
        }

        static void RequireProgramBoundaries(
            CharacterPoseCompilationRequest request,
            CharacterPoseTypedIrCatalog typedIr)
        {
            CharacterPoseTypedIrGraph root = typedIr.RequireGraph(
                request.AuthoringView.OwnerAsset,
                request.AuthoringView.RootGraph.GraphId);
            int finalPublicationCount = root.AuthoredNodes.Values.Count(
                value => value.Kind == CharacterPoseNodeKind.OutputPose);
            int fullBodyIkCount = typedIr.Graphs.Sum(graph =>
                graph.AuthoredNodes.Values.Count(value =>
                    value.Kind == CharacterPoseNodeKind.FullBodyIK));
            if (finalPublicationCount != 1)
            {
                throw new InvalidOperationException(
                    "Pose Program requires exactly one root Final Publication boundary.");
            }
            if (fullBodyIkCount != 1)
            {
                throw new InvalidOperationException(
                    "Pose Program requires exactly one Full Body IK node.");
            }
        }
    }
}
