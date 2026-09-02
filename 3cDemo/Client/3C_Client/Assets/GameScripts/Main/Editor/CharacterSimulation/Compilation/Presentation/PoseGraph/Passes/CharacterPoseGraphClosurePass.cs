using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class CharacterPoseGraphClosureEntry
    {
        internal CharacterPoseGraphClosureEntry(
            CharacterPresentationPoseGraphAsset owner,
            string ownerIdentity,
            CharacterTypedPoseGraph graph)
        {
            Owner = owner ? owner : throw new ArgumentNullException(nameof(owner));
            OwnerIdentity = PoseIdentity.Require(
                ownerIdentity,
                nameof(ownerIdentity));
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        internal CharacterPresentationPoseGraphAsset Owner { get; }
        internal string OwnerIdentity { get; }
        internal CharacterTypedPoseGraph Graph { get; }
    }

    internal sealed class CharacterPoseGraphClosure
    {
        readonly Dictionary<string, CharacterPoseGraphClosureEntry> m_Entries;

        internal CharacterPoseGraphClosure(
            IReadOnlyList<CharacterPoseGraphClosureEntry> entries)
        {
            Entries = entries?.ToArray() ??
                throw new ArgumentNullException(nameof(entries));
            m_Entries = Entries.ToDictionary(
                value => Key(value.OwnerIdentity, value.Graph.GraphId),
                StringComparer.Ordinal);
        }

        internal IReadOnlyList<CharacterPoseGraphClosureEntry> Entries { get; }

        internal CharacterTypedPoseGraph RequireGraph(
            CharacterPresentationPoseGraphAsset owner,
            PoseGraphId graphId)
        {
            string ownerIdentity =
                CharacterPresentationAssetObjectIdentity.Require(owner);
            string key = Key(ownerIdentity, graphId);
            return m_Entries.TryGetValue(
                    key,
                    out CharacterPoseGraphClosureEntry entry)
                ? entry.Graph
                : throw new InvalidOperationException(
                    $"Pose Graph '{graphId}' is outside the compiled Graph Closure for '{ownerIdentity}'.");
        }

        internal static string Key(
            string ownerIdentity,
            PoseGraphId graphId) =>
            PoseIdentity.Require(ownerIdentity, nameof(ownerIdentity)) +
            "\0" +
            (graphId.IsValid
                ? graphId.Value
                : throw new ArgumentException(
                    "Pose Graph identity is invalid.",
                    nameof(graphId)));
    }

    internal sealed class CharacterPoseGraphClosurePassResult
    {
        internal CharacterPoseGraphClosurePassResult(
            CharacterPoseGraphClosure closure,
            IReadOnlyList<CharacterPoseCompilationDiagnostic> diagnostics)
        {
            Closure = closure;
            Diagnostics = diagnostics?.ToArray() ??
                Array.Empty<CharacterPoseCompilationDiagnostic>();
            IsSuccess = closure != null &&
                Diagnostics.All(value =>
                    value.Severity !=
                    CharacterPoseCompilationDiagnosticSeverity.Error);
        }

        internal bool IsSuccess { get; }
        internal CharacterPoseGraphClosure Closure { get; }
        internal IReadOnlyList<CharacterPoseCompilationDiagnostic>
            Diagnostics { get; }
    }

    internal static class CharacterPoseGraphClosurePass
    {
        sealed class GraphClosureFailure : Exception
        {
            internal GraphClosureFailure(
                string reason,
                string message,
                PoseGraphId graphId,
                PoseNodeId nodeId = default,
                IReadOnlyList<string> relatedIdentities = null)
                : base(message)
            {
                Reason = reason;
                GraphId = graphId;
                NodeId = nodeId;
                RelatedIdentities = relatedIdentities ??
                    Array.Empty<string>();
            }

            internal string Reason { get; }
            internal PoseGraphId GraphId { get; }
            internal PoseNodeId NodeId { get; }
            internal IReadOnlyList<string> RelatedIdentities { get; }
        }

        sealed class Builder
        {
            readonly CharacterPoseCompilationRequest m_Request;
            readonly Dictionary<string,
                Dictionary<PoseGraphId, CharacterTypedPoseGraph>>
                m_Catalogs =
                    new Dictionary<string,
                        Dictionary<PoseGraphId, CharacterTypedPoseGraph>>(
                        StringComparer.Ordinal);
            readonly Dictionary<LinkedPoseGroupId,
                CharacterLinkedPoseGroupBinding> m_LinkedGroups;
            readonly Dictionary<LinkedPoseImplementationId,
                CharacterLinkedPoseImplementationAsset>
                m_LinkedImplementations;
            readonly Dictionary<LinkedPoseGroupId,
                CharacterLinkedPoseCompiledSelectorDescriptor>
                m_LinkedSelectors;
            readonly List<CharacterPoseGraphClosureEntry> m_Entries =
                new List<CharacterPoseGraphClosureEntry>();
            readonly HashSet<string> m_Visited =
                new HashSet<string>(StringComparer.Ordinal);
            readonly List<string> m_CallStack = new List<string>();

            internal Builder(CharacterPoseCompilationRequest request)
            {
                m_Request = request ??
                    throw new ArgumentNullException(nameof(request));
                m_LinkedGroups = UniqueBy(
                    request.Profile.LinkedPoseGroups,
                    value => value.GroupId,
                    "Linked Pose Group");
                m_LinkedImplementations = UniqueBy(
                    request.Profile.LinkedPoseImplementations,
                    value => value.ImplementationId,
                    "Linked Pose Implementation");
                m_LinkedSelectors = UniqueBy(
                    request.LinkedPose.Selectors,
                    value => value.GroupId,
                    "Linked Pose Selector");
            }

            internal CharacterPoseGraphClosure Build()
            {
                CharacterPresentationPoseGraphAsset rootOwner =
                    m_Request.Asset;
                CharacterTypedPoseGraph root = rootOwner.Graph;
                if (root == null || !root.GraphId.IsValid)
                {
                    throw new GraphClosureFailure(
                        "root-graph-invalid",
                        "Pose compiler request has no valid root Graph.",
                        root?.GraphId ?? default);
                }
                Visit(rootOwner, root.GraphId, default);
                return new CharacterPoseGraphClosure(m_Entries);
            }

            void Visit(
                CharacterPresentationPoseGraphAsset owner,
                PoseGraphId graphId,
                PoseNodeId sourceNodeId)
            {
                string ownerIdentity =
                    CharacterPresentationAssetObjectIdentity.Require(owner);
                string key = CharacterPoseGraphClosure.Key(
                    ownerIdentity,
                    graphId);
                int recursiveIndex = m_CallStack.IndexOf(key);
                if (recursiveIndex >= 0)
                {
                    string[] path = m_CallStack
                        .Skip(recursiveIndex)
                        .Concat(new[] { key })
                        .ToArray();
                    throw new GraphClosureFailure(
                        "recursive-graph-call",
                        $"Pose Graph catalog contains a recursive call: {string.Join(" -> ", path)}.",
                        graphId,
                        sourceNodeId,
                        path);
                }
                if (m_Visited.Contains(key))
                    return;
                CharacterTypedPoseGraph graph = RequireCatalogGraph(
                    owner,
                    ownerIdentity,
                    graphId,
                    sourceNodeId);
                m_CallStack.Add(key);
                m_Entries.Add(new CharacterPoseGraphClosureEntry(
                    owner,
                    ownerIdentity,
                    graph));
                for (int nodeIndex = 0;
                     nodeIndex < graph.Nodes.Count;
                     nodeIndex++)
                {
                    CharacterTypedPoseNode node = graph.Nodes[nodeIndex];
                    if (node?.Payload == null)
                        continue;
                    CharacterPoseNodeDefinition definition =
                        CharacterPoseNodeDefinitionModule.Shared.Require(
                            node.Kind);
                    IReadOnlyList<CharacterPoseGraphDependency> dependencies =
                        definition.ProjectGraphDependencies(node.Payload);
                    for (int dependencyIndex = 0;
                         dependencyIndex < dependencies.Count;
                         dependencyIndex++)
                    {
                        CharacterPoseGraphDependency dependency =
                            dependencies[dependencyIndex];
                        if (dependency.Kind ==
                            CharacterPoseGraphDependencyKind.LinkedPoseEntry)
                        {
                            VisitLinkedPoseEntries(owner, graph, node);
                            continue;
                        }
                        if (!dependency.GraphId.IsValid)
                        {
                            throw new GraphClosureFailure(
                                "graph-dependency-invalid",
                                $"Pose Node '{node.NodeId}' has an invalid {dependency.Kind} Graph dependency.",
                                graph.GraphId,
                                node.NodeId,
                                new[] { dependency.OwnerIdentity });
                        }
                        Visit(owner, dependency.GraphId, node.NodeId);
                    }
                }
                m_CallStack.RemoveAt(m_CallStack.Count - 1);
                m_Visited.Add(key);
            }

            void VisitLinkedPoseEntries(
                CharacterPresentationPoseGraphAsset owner,
                CharacterTypedPoseGraph graph,
                CharacterTypedPoseNode node)
            {
                if (node.Payload is not CharacterLinkedPoseCallPayload payload ||
                    !m_LinkedGroups.TryGetValue(
                        payload.GroupId,
                        out CharacterLinkedPoseGroupBinding group) ||
                    !group.Interface ||
                    group.Interface.InterfaceId != payload.InterfaceId ||
                    !m_LinkedSelectors.TryGetValue(
                        payload.GroupId,
                        out CharacterLinkedPoseCompiledSelectorDescriptor selector))
                {
                    throw new GraphClosureFailure(
                        "linked-pose-dependency-invalid",
                        $"Linked Pose Call '{node.NodeId}' has no exact Group, Interface or Selector.",
                        graph.GraphId,
                        node.NodeId);
                }
                group.Interface.RequireEntry(payload.EntryId);
                for (int candidateIndex = 0;
                     candidateIndex < selector.CandidateImplementationIds.Count;
                     candidateIndex++)
                {
                    var implementationId = new LinkedPoseImplementationId(
                        selector.CandidateImplementationIds[candidateIndex]);
                    if (!m_LinkedImplementations.TryGetValue(
                            implementationId,
                            out CharacterLinkedPoseImplementationAsset implementation))
                    {
                        throw new GraphClosureFailure(
                            "linked-pose-implementation-missing",
                            $"Linked Pose Call '{node.NodeId}' candidate '{implementationId}' is absent from authoring.",
                            graph.GraphId,
                            node.NodeId,
                            new[] { implementationId.Value });
                    }
                    CharacterLinkedPoseImplementationEntryBinding entry =
                        implementation.RequireEntry(payload.EntryId);
                    if (!entry.GraphOwner ||
                        !entry.GraphId.IsValid ||
                        !string.Equals(
                            entry.GraphOwnerIdentity,
                            CharacterPresentationAssetObjectIdentity.Require(
                                entry.GraphOwner),
                            StringComparison.Ordinal))
                    {
                        throw new GraphClosureFailure(
                            "linked-pose-entry-binding-invalid",
                            $"Linked Pose Implementation '{implementationId}' Entry '{payload.EntryId}' has an invalid Graph owner.",
                            graph.GraphId,
                            node.NodeId,
                            new[] { implementationId.Value });
                    }
                    Visit(entry.GraphOwner, entry.GraphId, node.NodeId);
                }
            }

            CharacterTypedPoseGraph RequireCatalogGraph(
                CharacterPresentationPoseGraphAsset owner,
                string ownerIdentity,
                PoseGraphId graphId,
                PoseNodeId sourceNodeId)
            {
                if (!m_Catalogs.TryGetValue(
                        ownerIdentity,
                        out Dictionary<PoseGraphId,
                            CharacterTypedPoseGraph> catalog))
                {
                    catalog = BuildCatalog(owner, ownerIdentity);
                    m_Catalogs.Add(ownerIdentity, catalog);
                }
                if (!catalog.TryGetValue(graphId, out CharacterTypedPoseGraph graph))
                {
                    throw new GraphClosureFailure(
                        "graph-dependency-missing",
                        $"Pose Graph '{graphId}' does not exist in '{ownerIdentity}'.",
                        graphId,
                        sourceNodeId,
                        new[] { ownerIdentity });
                }
                return graph;
            }

            static Dictionary<PoseGraphId, CharacterTypedPoseGraph>
                BuildCatalog(
                    CharacterPresentationPoseGraphAsset owner,
                    string ownerIdentity)
            {
                var catalog =
                    new Dictionary<PoseGraphId, CharacterTypedPoseGraph>();
                foreach (CharacterTypedPoseGraph graph in owner.EnumerateGraphs())
                {
                    if (graph == null ||
                        !graph.GraphId.IsValid ||
                        !catalog.TryAdd(graph.GraphId, graph))
                    {
                        throw new GraphClosureFailure(
                            "graph-catalog-invalid",
                            $"Pose Graph catalog '{ownerIdentity}' contains a missing or duplicate Graph identity.",
                            graph?.GraphId ?? default,
                            default,
                            new[] { ownerIdentity });
                    }
                }
                return catalog;
            }

            static Dictionary<TKey, TValue> UniqueBy<TKey, TValue>(
                IEnumerable<TValue> values,
                Func<TValue, TKey> key,
                string label)
            {
                var result = new Dictionary<TKey, TValue>();
                foreach (TValue value in values ?? Array.Empty<TValue>())
                {
                    if (value == null || !result.TryAdd(key(value), value))
                    {
                        throw new GraphClosureFailure(
                            "linked-pose-catalog-invalid",
                            $"{label} catalog contains a missing or duplicate identity.",
                            default);
                    }
                }
                return result;
            }
        }

        internal static CharacterPoseGraphClosurePassResult Run(
            CharacterPoseCompilationRequest request)
        {
            try
            {
                CharacterPoseGraphClosure closure =
                    new Builder(request).Build();
                return new CharacterPoseGraphClosurePassResult(
                    closure,
                    Array.Empty<CharacterPoseCompilationDiagnostic>());
            }
            catch (GraphClosureFailure failure)
            {
                return new CharacterPoseGraphClosurePassResult(
                    null,
                    new[]
                    {
                        new CharacterPoseCompilationDiagnostic(
                            CharacterPoseCompilationPass.GraphClosure,
                            CharacterPoseCompilationDiagnosticSeverity.Error,
                            failure.Reason,
                            failure.Message,
                            failure.GraphId,
                            failure.NodeId,
                            relatedIdentities: failure.RelatedIdentities)
                    });
            }
            catch (Exception exception)
            {
                return new CharacterPoseGraphClosurePassResult(
                    null,
                    new[]
                    {
                        new CharacterPoseCompilationDiagnostic(
                            CharacterPoseCompilationPass.GraphClosure,
                            CharacterPoseCompilationDiagnosticSeverity.Error,
                            "graph-closure-invalid",
                            exception.Message,
                            request?.Asset?.Graph?.GraphId ?? default)
                    });
            }
        }
    }
}
