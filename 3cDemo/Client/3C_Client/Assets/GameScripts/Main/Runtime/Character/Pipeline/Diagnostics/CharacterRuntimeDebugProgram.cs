using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Graph;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Diagnostics
{
    public sealed class CharacterRuntimeDebugProgram : IRuntimeDebugProgram
    {
        public CharacterRuntimeDebugProgram(RuntimeProgramRevision revision, DebugSourceMap sourceMap)
        {
            Revision = revision;
            SourceMap = sourceMap;
        }

        public RuntimeProgramRevision Revision { get; }
        public IDebugSourceMap SourceMap { get; }
    }

    public static class CharacterRuntimeDebugProgramBuilder
    {
        public const ulong InterpreterCompilationRevision = 1;

        public static CharacterRuntimeDebugProgram Build(CharacterPipelineDefinition definition)
        {
            BaseTree root = definition != null ? definition.RootTree : null;
            if (root == null)
                throw new InvalidOperationException("Character diagnostics requires a RootTree source.");

            var projectionErrors = new List<string>();
            CharacterAuthoringTopologyProjection projection = CharacterAuthoringTopologyProjection.Build(root, projectionErrors);
            if (!projection.IsValid)
                throw new InvalidOperationException(string.Join("\n", projectionErrors));

            var collector = new SourceCollector(projection);
            collector.Collect();
            string sourceHash = collector.ComputeProgramHash();
            var revision = new RuntimeProgramRevision(
                $"{definition.name}:{root.GraphAuthoringId}",
                InterpreterCompilationRevision,
                sourceHash);
            var sourceMap = new DebugSourceMap(revision);
            collector.BuildMap(sourceMap);
            sourceMap.Seal();
            return new CharacterRuntimeDebugProgram(revision, sourceMap);
        }

        sealed class SourceCollector
        {
            readonly CharacterAuthoringTopologyProjection m_Projection;
            readonly List<Descriptor> m_Descriptors = new List<Descriptor>();
            readonly Dictionary<TimelineData, int> m_TimelineDescriptors = new Dictionary<TimelineData, int>();
            readonly Dictionary<object, int> m_OwnerDescriptors = new Dictionary<object, int>();
            readonly Dictionary<RuntimeSourceElementKey, object> m_SourceOwners = new Dictionary<RuntimeSourceElementKey, object>();

            public SourceCollector(CharacterAuthoringTopologyProjection projection)
            {
                m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            }

            public void Collect()
            {
                for (int i = 0; i < m_Projection.Graphs.Count; i++)
                {
                    CharacterAuthoringGraphEntry entry = m_Projection.Graphs[i];
                    int parent = entry.ParentOwner != null && m_OwnerDescriptors.TryGetValue(entry.ParentOwner, out int found)
                        ? found
                        : -1;
                    CollectGraph(entry.Graph, parent, entry.FirstOccurrence);
                }
            }

            void CollectGraph(BaseTree graph, int parent, bool firstOccurrence)
            {
                ValidateIdentity(graph.GraphAuthoringId, $"Graph '{graph.name}'");
                if (!firstOccurrence)
                {
                    AddDescriptor(RuntimeSourceElementKey.Graph(graph.GraphAuthoringId), parent, graph.name, GraphAuthoringFingerprint.Compute(graph), graph, true);
                    return;
                }

                int graphDescriptor = AddDescriptor(
                    RuntimeSourceElementKey.Graph(graph.GraphAuthoringId),
                    parent,
                    graph.name,
                    GraphAuthoringFingerprint.Compute(graph),
                    graph,
                    false);
                AddOwnerDescriptor(graph, graphDescriptor);

                for (int i = 0; i < graph.ExposedProperties.Count; i++)
                {
                    BaseExposedProperty declaration = graph.ExposedProperties[i];
                    if (declaration == null)
                        throw new InvalidOperationException($"Graph '{graph.name}' contains a null Blackboard declaration.");
                    ValidateIdentity(declaration.DeclarationId, $"Blackboard declaration '{declaration.BlackboardKey}'");
                    int declarationDescriptor = AddDescriptor(
                        RuntimeSourceElementKey.Declaration(graph.GraphAuthoringId, declaration.DeclarationId),
                        graphDescriptor,
                        declaration.BlackboardKey,
                        string.Empty,
                        declaration,
                        false);
                    AddOwnerDescriptor(declaration, declarationDescriptor);
                }

                for (int i = 0; i < graph.Nodes.Count; i++)
                {
                    BaseNode node = graph.Nodes[i];
                    if (node == null)
                        throw new InvalidOperationException($"Graph '{graph.name}' contains a null Node.");
                    ValidateIdentity(node.GUID, $"Node '{node.ResolvedDisplayName}'");
                    int nodeDescriptor = AddDescriptor(
                        RuntimeSourceElementKey.Node(graph.GraphAuthoringId, node.GUID),
                        graphDescriptor,
                        node.ResolvedDisplayName,
                        string.Empty,
                        node,
                        false);
                    AddOwnerDescriptor(node, nodeDescriptor);

                    if (node is TimelineNode timelineNode && timelineNode.Timeline != null)
                        CollectTimeline(timelineNode.Timeline, nodeDescriptor);
                }

                for (int i = 0; i < graph.Edges.Count; i++)
                {
                    BaseEdge edge = graph.Edges[i];
                    if (edge == null)
                        throw new InvalidOperationException($"Graph '{graph.name}' contains a null Edge.");
                    ValidateIdentity(edge.GUID, $"Edge '{graph.name}/{i}'");
                    int edgeDescriptor = AddDescriptor(
                        RuntimeSourceElementKey.Edge(graph.GraphAuthoringId, edge.GUID),
                        graphDescriptor,
                        $"{edge.StartNodeGUID}->{edge.EndNodeGUID}",
                        string.Empty,
                        edge,
                        false);
                    AddOwnerDescriptor(edge, edgeDescriptor);
                }

                for (int i = 0; i < graph.PropertyEdges.Count; i++)
                {
                    PropertyEdge edge = graph.PropertyEdges[i];
                    if (edge == null)
                        throw new InvalidOperationException($"Graph '{graph.name}' contains a null PropertyEdge.");
                    ValidateIdentity(edge.GUID, $"PropertyEdge '{graph.name}/{i}'");
                    int edgeDescriptor = AddDescriptor(
                        RuntimeSourceElementKey.Edge(graph.GraphAuthoringId, edge.GUID),
                        graphDescriptor,
                        $"{edge.StartNodeGUID}:{edge.StartPortName}->{edge.EndNodeGUID}:{edge.EndPortName}",
                        string.Empty,
                        edge,
                        false);
                    AddOwnerDescriptor(edge, edgeDescriptor);
                }
            }

            int CollectTimeline(TimelineData timeline, int parent)
            {
                var errors = new List<string>();
                if (!timeline.ValidateAuthoringIdentities(errors))
                    throw new InvalidOperationException(string.Join("\n", errors));
                if (m_TimelineDescriptors.TryGetValue(timeline, out int existing))
                {
                    AddDescriptor(RuntimeSourceElementKey.Timeline(timeline.AuthoringId), parent, timeline.Name, TimelineAuthoringFingerprint.Compute(timeline), timeline, true);
                    return existing;
                }

                int timelineDescriptor = AddDescriptor(
                    RuntimeSourceElementKey.Timeline(timeline.AuthoringId),
                    parent,
                    timeline.Name,
                    TimelineAuthoringFingerprint.Compute(timeline),
                    timeline,
                    false);
                m_TimelineDescriptors.Add(timeline, timelineDescriptor);
                AddOwnerDescriptor(timeline, timelineDescriptor);
                for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
                {
                    Track track = timeline.Tracks[trackIndex];
                    int trackDescriptor = AddDescriptor(
                        RuntimeSourceElementKey.Track(timeline.AuthoringId, track.AuthoringId),
                        timelineDescriptor,
                        track.Name,
                        string.Empty,
                        track,
                        false);
                    AddOwnerDescriptor(track, trackDescriptor);
                    for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                    {
                        Clip clip = track.Clips[clipIndex];
                        bool treeClip = clip is TreeClip;
                        int clipDescriptor = AddDescriptor(
                            RuntimeSourceElementKey.Clip(timeline.AuthoringId, track.AuthoringId, clip.AuthoringId, treeClip),
                            trackDescriptor,
                            clip.GetType().Name,
                            string.Empty,
                            clip,
                            false);
                        AddOwnerDescriptor(clip, clipDescriptor);
                    }
                }
                return timelineDescriptor;
            }

            void AddOwnerDescriptor(object owner, int descriptor)
            {
                if (owner != null && !m_OwnerDescriptors.ContainsKey(owner))
                    m_OwnerDescriptors.Add(owner, descriptor);
            }

            int AddDescriptor(
                RuntimeSourceElementKey source,
                int parent,
                string displayName,
                string contentHash,
                object owner,
                bool allowAlias)
            {
                if (!allowAlias && m_SourceOwners.TryGetValue(source, out object existingOwner) && !ReferenceEquals(existingOwner, owner))
                    throw new InvalidOperationException($"Duplicate source identity: {source.Kind}/{source.GraphAuthoringId}/{source.ElementAuthoringId}/{source.TimelineAuthoringId}/{source.TrackAuthoringId}/{source.ClipAuthoringId}.");
                if (!m_SourceOwners.ContainsKey(source))
                    m_SourceOwners.Add(source, owner);
                m_Descriptors.Add(new Descriptor(source, parent, displayName, contentHash));
                return m_Descriptors.Count - 1;
            }

            public string ComputeProgramHash()
            {
                var values = new string[m_Descriptors.Count];
                for (int i = 0; i < m_Descriptors.Count; i++)
                {
                    Descriptor descriptor = m_Descriptors[i];
                    RuntimeSourceElementKey source = descriptor.Source;
                    values[i] = $"{source.Kind}|{source.GraphAuthoringId}|{source.ElementAuthoringId}|{source.TimelineAuthoringId}|{source.TrackAuthoringId}|{source.ClipAuthoringId}|{descriptor.ContentHash}";
                }
                return SourceContentHasher.Hash(values);
            }

            public void BuildMap(DebugSourceMap map)
            {
                var handles = new RuntimeSourceElementHandle[m_Descriptors.Count];
                for (int i = 0; i < m_Descriptors.Count; i++)
                {
                    Descriptor descriptor = m_Descriptors[i];
                    RuntimeSourceElementHandle parent = descriptor.Parent >= 0 ? handles[descriptor.Parent] : default;
                    handles[i] = map.Add(descriptor.Source, parent, descriptor.DisplayName, descriptor.ContentHash);
                }
            }

            static void ValidateIdentity(string identity, string label)
            {
                if (!AuthoringIdentity.IsValid(identity))
                    throw new InvalidOperationException($"{label} has an invalid authoring identity.");
            }

            readonly struct Descriptor
            {
                public Descriptor(RuntimeSourceElementKey source, int parent, string displayName, string contentHash)
                {
                    Source = source;
                    Parent = parent;
                    DisplayName = displayName ?? string.Empty;
                    ContentHash = contentHash ?? string.Empty;
                }

                public RuntimeSourceElementKey Source { get; }
                public int Parent { get; }
                public string DisplayName { get; }
                public string ContentHash { get; }
            }
        }
    }
}
