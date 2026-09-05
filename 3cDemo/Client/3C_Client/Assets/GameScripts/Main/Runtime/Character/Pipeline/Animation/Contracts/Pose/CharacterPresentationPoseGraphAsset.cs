using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterPoseStateMachineLayoutElement
    {
        [SerializeField] string m_ElementId = string.Empty;
        [SerializeField] Vector2 m_Position;

        public string ElementId => m_ElementId ?? string.Empty;
        public Vector2 Position => m_Position;

        public CharacterPoseStateMachineLayoutElement() { }

        public CharacterPoseStateMachineLayoutElement(
            string elementId,
            Vector2 position)
        {
            m_ElementId = string.IsNullOrWhiteSpace(elementId)
                ? throw new ArgumentException(
                    "Pose StateMachine layout element identity is missing.",
                    nameof(elementId))
                : elementId.Trim();
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y))
                throw new ArgumentException(
                    "Pose StateMachine layout position must be finite.",
                    nameof(position));
            m_Position = position;
        }
    }

    [Serializable]
    public sealed class CharacterPoseStateMachineLayout
    {
        [SerializeField] string m_StateMachineId = string.Empty;
        [SerializeField] CharacterPoseStateMachineLayoutElement[] m_Elements =
            Array.Empty<CharacterPoseStateMachineLayoutElement>();

        public PoseStateMachineId StateMachineId =>
            string.IsNullOrWhiteSpace(m_StateMachineId)
                ? default
                : new PoseStateMachineId(m_StateMachineId);
        public IReadOnlyList<CharacterPoseStateMachineLayoutElement> Elements =>
            m_Elements ?? Array.Empty<CharacterPoseStateMachineLayoutElement>();

        public CharacterPoseStateMachineLayout() { }

        public CharacterPoseStateMachineLayout(
            PoseStateMachineId stateMachineId,
            CharacterPoseStateMachineLayoutElement[] elements)
        {
            m_StateMachineId = stateMachineId.IsValid
                ? stateMachineId.Value
                : throw new ArgumentException(
                    "Pose StateMachine layout owner identity is invalid.",
                    nameof(stateMachineId));
            m_Elements = (elements ??
                          Array.Empty<CharacterPoseStateMachineLayoutElement>())
                .OrderBy(value => value?.ElementId, StringComparer.Ordinal)
                .ToArray();
            RequireValidElements(m_Elements);
        }

        internal static void RequireValidElements(
            IReadOnlyList<CharacterPoseStateMachineLayoutElement> elements)
        {
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (CharacterPoseStateMachineLayoutElement element in
                     elements ??
                     Array.Empty<CharacterPoseStateMachineLayoutElement>())
            {
                if (element == null ||
                    string.IsNullOrWhiteSpace(element.ElementId) ||
                    !identities.Add(element.ElementId) ||
                    !float.IsFinite(element.Position.x) ||
                    !float.IsFinite(element.Position.y))
                {
                    throw new InvalidOperationException(
                        "Pose StateMachine layout contains a missing, duplicate or non-finite element.");
                }
            }
        }
    }

    [CreateAssetMenu(fileName = "CharacterPresentationPoseGraph", menuName = "3C/Character/Presentation Pose Graph")]
    public sealed class CharacterPresentationPoseGraphAsset : ScriptableObject
    {
        [SerializeField] CharacterPoseCanvasGraph m_Graph;
        [SerializeField] CharacterPoseCanvasGraph[] m_GraphCatalog = Array.Empty<CharacterPoseCanvasGraph>();
        [SerializeField] CharacterPoseStateMachineLayout[] m_StateMachineLayouts =
            Array.Empty<CharacterPoseStateMachineLayout>();
        [SerializeField] CharacterPresentationPoseSourceSlot[] m_SourceSlots =
            Array.Empty<CharacterPresentationPoseSourceSlot>();
#if UNITY_EDITOR
        [Serializable]
        internal sealed class LegacyPoseNode
        {
            [SerializeField] string m_NodeId = string.Empty;
            [SerializeField] string m_DisplayName = string.Empty;
            [SerializeReference] CharacterPoseNodePayload m_Payload;
            [SerializeField] CharacterPoseDynamicPort[] m_DynamicPorts =
                Array.Empty<CharacterPoseDynamicPort>();

            public PoseNodeId NodeId => string.IsNullOrWhiteSpace(m_NodeId)
                ? default
                : new PoseNodeId(m_NodeId);
            public string DisplayName => m_DisplayName ?? string.Empty;
            public CharacterPoseNodePayload Payload => m_Payload;
            public IReadOnlyList<CharacterPoseDynamicPort> DynamicPorts =>
                m_DynamicPorts ?? Array.Empty<CharacterPoseDynamicPort>();
        }

        [Serializable]
        internal sealed class LegacyPoseEdge
        {
            [SerializeField] string m_EdgeId = string.Empty;
            [SerializeField] string m_SourceNodeId = string.Empty;
            [SerializeField] string m_SourcePortId = string.Empty;
            [SerializeField] string m_TargetNodeId = string.Empty;
            [SerializeField] string m_TargetPortId = string.Empty;

            public string EdgeId => m_EdgeId ?? string.Empty;
            public PoseNodeId SourceNodeId => string.IsNullOrWhiteSpace(m_SourceNodeId)
                ? default
                : new PoseNodeId(m_SourceNodeId);
            public PosePortId SourcePortId => string.IsNullOrWhiteSpace(m_SourcePortId)
                ? default
                : new PosePortId(m_SourcePortId);
            public PoseNodeId TargetNodeId => string.IsNullOrWhiteSpace(m_TargetNodeId)
                ? default
                : new PoseNodeId(m_TargetNodeId);
            public PosePortId TargetPortId => string.IsNullOrWhiteSpace(m_TargetPortId)
                ? default
                : new PosePortId(m_TargetPortId);
        }

        [Serializable]
        internal sealed class LegacyPoseGraph
        {
            [SerializeField] string m_GraphId = string.Empty;
            [SerializeField] string m_ContentRevision = string.Empty;
            [SerializeField] CharacterPoseParameterDeclaration[] m_Parameters =
                Array.Empty<CharacterPoseParameterDeclaration>();
            [SerializeField] LegacyPoseNode[] m_Nodes = Array.Empty<LegacyPoseNode>();
            [SerializeField] LegacyPoseEdge[] m_Edges = Array.Empty<LegacyPoseEdge>();
            [SerializeField] CharacterPoseGraphLayoutEntry[] m_Layout =
                Array.Empty<CharacterPoseGraphLayoutEntry>();

            public PoseGraphId GraphId => string.IsNullOrWhiteSpace(m_GraphId)
                ? default
                : new PoseGraphId(m_GraphId);
            public string ContentRevision => m_ContentRevision ?? string.Empty;
            public IReadOnlyList<CharacterPoseParameterDeclaration> Parameters =>
                m_Parameters ?? Array.Empty<CharacterPoseParameterDeclaration>();
            public IReadOnlyList<LegacyPoseNode> Nodes =>
                m_Nodes ?? Array.Empty<LegacyPoseNode>();
            public IReadOnlyList<LegacyPoseEdge> Edges =>
                m_Edges ?? Array.Empty<LegacyPoseEdge>();
            public IReadOnlyList<CharacterPoseGraphLayoutEntry> Layout =>
                m_Layout ?? Array.Empty<CharacterPoseGraphLayoutEntry>();
        }

        [SerializeField] LegacyPoseGraph m_TypedGraph;
        [SerializeField] LegacyPoseGraph[] m_TypedGraphCatalog =
            Array.Empty<LegacyPoseGraph>();

        internal sealed class LegacyCanvasMigrationState
        {
            internal LegacyCanvasMigrationState(
                LegacyPoseGraph legacyRoot,
                LegacyPoseGraph[] legacyCatalog,
                CharacterPoseCanvasGraph canvasRoot,
                CharacterPoseCanvasGraph[] canvasCatalog)
            {
                m_LegacyRoot = legacyRoot;
                m_LegacyCatalog = legacyCatalog;
                m_CanvasRoot = canvasRoot;
                m_CanvasCatalog = canvasCatalog;
            }

            internal void RestoreTo(CharacterPresentationPoseGraphAsset asset)
            {
                asset.m_Graph = m_CanvasRoot;
                asset.m_GraphCatalog = m_CanvasCatalog;
                asset.m_TypedGraph = m_LegacyRoot;
                asset.m_TypedGraphCatalog = m_LegacyCatalog;
            }
        }

        internal sealed class LegacyCanvasMigrationSource
        {
            internal LegacyCanvasMigrationSource(
                LegacyCanvasMigrationGraph root,
                LegacyCanvasMigrationGraph[] catalog)
            {
                Root = root;
                Catalog = catalog ?? Array.Empty<LegacyCanvasMigrationGraph>();
            }

            internal LegacyCanvasMigrationGraph Root { get; }
            internal IReadOnlyList<LegacyCanvasMigrationGraph> Catalog { get; }
        }

        internal sealed class LegacyCanvasMigrationGraph
        {
            internal LegacyCanvasMigrationGraph(LegacyPoseGraph graph)
            {
                GraphId = graph.GraphId;
                ContentRevision = graph.ContentRevision;
                Parameters = graph.Parameters.ToArray();
                Nodes = graph.Nodes
                    .Select(value => new LegacyCanvasMigrationNode(value))
                    .ToArray();
                Edges = graph.Edges
                    .Select(value => new LegacyCanvasMigrationEdge(value))
                    .ToArray();
                Layout = graph.Layout.ToArray();
            }

            internal PoseGraphId GraphId { get; }
            internal string ContentRevision { get; }
            internal IReadOnlyList<CharacterPoseParameterDeclaration> Parameters { get; }
            internal IReadOnlyList<LegacyCanvasMigrationNode> Nodes { get; }
            internal IReadOnlyList<LegacyCanvasMigrationEdge> Edges { get; }
            internal IReadOnlyList<CharacterPoseGraphLayoutEntry> Layout { get; }
        }

        internal sealed class LegacyCanvasMigrationNode
        {
            internal LegacyCanvasMigrationNode(LegacyPoseNode node)
            {
                NodeId = node.NodeId;
                DisplayName = node.DisplayName;
                Payload = node.Payload;
                DynamicPorts = node.DynamicPorts.ToArray();
            }

            internal PoseNodeId NodeId { get; }
            internal string DisplayName { get; }
            internal CharacterPoseNodePayload Payload { get; }
            internal IReadOnlyList<CharacterPoseDynamicPort> DynamicPorts { get; }
        }

        internal sealed class LegacyCanvasMigrationEdge
        {
            internal LegacyCanvasMigrationEdge(LegacyPoseEdge edge)
            {
                EdgeId = edge.EdgeId;
                SourceNodeId = edge.SourceNodeId;
                SourcePortId = edge.SourcePortId;
                TargetNodeId = edge.TargetNodeId;
                TargetPortId = edge.TargetPortId;
            }

            internal string EdgeId { get; }
            internal PoseNodeId SourceNodeId { get; }
            internal PosePortId SourcePortId { get; }
            internal PoseNodeId TargetNodeId { get; }
            internal PosePortId TargetPortId { get; }
        }
#endif

        public CharacterPoseCanvasGraph Graph => m_Graph;
        public IReadOnlyList<CharacterPoseCanvasGraph> GraphCatalog => m_GraphCatalog ?? Array.Empty<CharacterPoseCanvasGraph>();
        public IReadOnlyList<CharacterPoseStateMachineLayout> StateMachineLayouts =>
            m_StateMachineLayouts ?? Array.Empty<CharacterPoseStateMachineLayout>();
        public IReadOnlyList<CharacterPresentationPoseSourceSlot> SourceSlots =>
            m_SourceSlots ?? Array.Empty<CharacterPresentationPoseSourceSlot>();

        internal void SetGraph(CharacterPoseCanvasGraph graph)
        {
            graph = graph ?? throw new ArgumentNullException(nameof(graph));
            AttachGraph(graph);
            m_Graph = graph;
        }

        internal void SetSourceSlots(CharacterPresentationPoseSourceSlot[] slots)
        {
            CharacterPresentationPoseSourceSlot[] values = slots ??
                Array.Empty<CharacterPresentationPoseSourceSlot>();
            var references = new HashSet<CharacterPresentationPoseSourceSlot>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < values.Length; i++)
            {
                CharacterPresentationPoseSourceSlot slot = values[i];
                if (!slot || !references.Add(slot))
                    throw new InvalidOperationException($"Pose Source Slot #{i} is missing or duplicated.");
                slot.RequireValid();
                if (!names.Add(slot.name.Trim()))
                    throw new InvalidOperationException($"Pose Source Slot name '{slot.name}' is duplicated.");
            }
            m_SourceSlots = values;
        }

        public CharacterPoseCanvasGraph RequireGraph(PoseGraphId graphId)
        {
            if (!TryGetGraph(graphId, out CharacterPoseCanvasGraph graph))
                throw new InvalidOperationException($"Pose Graph '{graphId}' does not exist in '{name}'.");
            return graph;
        }

        public bool TryGetGraph(PoseGraphId graphId, out CharacterPoseCanvasGraph graph)
        {
            graph = null;
            if (!graphId.IsValid)
                return false;
            if (m_Graph != null && m_Graph.GraphId == graphId)
            {
                graph = m_Graph;
                return true;
            }
            CharacterPoseCanvasGraph[] catalog = m_GraphCatalog ?? Array.Empty<CharacterPoseCanvasGraph>();
            for (int i = 0; i < catalog.Length; i++)
            {
                CharacterPoseCanvasGraph candidate = catalog[i];
                if (candidate != null && candidate.GraphId == graphId)
                {
                    graph = candidate;
                    return true;
                }
            }
            return false;
        }

        internal void AddGraph(CharacterPoseCanvasGraph graph)
        {
            if (graph == null || !graph.GraphId.IsValid)
                throw new ArgumentException("Pose Graph catalog record is invalid.", nameof(graph));
            if (TryGetGraph(graph.GraphId, out _))
                throw new InvalidOperationException($"Pose Graph '{graph.GraphId}' already exists in '{name}'.");
            AttachGraph(graph);
            m_GraphCatalog = (m_GraphCatalog ?? Array.Empty<CharacterPoseCanvasGraph>()).Concat(new[] { graph }).ToArray();
        }

        internal void ReplaceGraph(CharacterPoseCanvasGraph graph)
        {
            if (graph == null || !graph.GraphId.IsValid)
                throw new ArgumentException("Pose Graph replacement is invalid.", nameof(graph));
            AttachGraph(graph);
            if (m_Graph != null && m_Graph.GraphId == graph.GraphId)
            {
                m_Graph = graph;
                return;
            }
            CharacterPoseCanvasGraph[] catalog = m_GraphCatalog ?? Array.Empty<CharacterPoseCanvasGraph>();
            int index = Array.FindIndex(catalog, value => value != null && value.GraphId == graph.GraphId);
            if (index < 0)
                throw new InvalidOperationException($"Pose Graph '{graph.GraphId}' does not exist in '{name}'.");
            catalog[index] = graph;
            m_GraphCatalog = catalog;
        }

        internal void RemoveGraph(PoseGraphId graphId)
        {
            if (!graphId.IsValid)
                throw new ArgumentException("Pose Graph identity is invalid.", nameof(graphId));
            if (m_Graph != null && m_Graph.GraphId == graphId)
                throw new InvalidOperationException("The root Pose Graph cannot be removed.");
            CharacterPoseCanvasGraph[] catalog = m_GraphCatalog ?? Array.Empty<CharacterPoseCanvasGraph>();
            CharacterPoseCanvasGraph[] next = catalog.Where(value => value != null && value.GraphId != graphId).ToArray();
            if (next.Length == catalog.Length)
                throw new InvalidOperationException($"Pose Graph '{graphId}' does not exist in '{name}'.");
            m_GraphCatalog = next;
        }

        public IEnumerable<CharacterPoseCanvasGraph> EnumerateGraphs()
        {
            if (m_Graph != null)
                yield return m_Graph;
            CharacterPoseCanvasGraph[] catalog = m_GraphCatalog ?? Array.Empty<CharacterPoseCanvasGraph>();
            for (int i = 0; i < catalog.Length; i++)
                yield return catalog[i];
        }

        void AttachGraph(CharacterPoseCanvasGraph graph)
        {
#if UNITY_EDITOR
            string ownerPath = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(ownerPath))
                return;
            string graphPath = AssetDatabase.GetAssetPath(graph);
            if (string.IsNullOrEmpty(graphPath))
            {
                AssetDatabase.AddObjectToAsset(graph, this);
                return;
            }
            if (!string.Equals(ownerPath, graphPath, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Pose Canvas graph '{graph.GraphId}' belongs to another asset.");
#endif
        }

#if UNITY_EDITOR
        internal LegacyCanvasMigrationState CaptureLegacyCanvasMigrationState()
        {
            RequireLegacyCanvasMigrationInput();
            return new LegacyCanvasMigrationState(
                m_TypedGraph,
                m_TypedGraphCatalog,
                m_Graph,
                m_GraphCatalog);
        }

        internal LegacyCanvasMigrationSource CaptureLegacyCanvasMigrationSource()
        {
            RequireLegacyCanvasMigrationInput();
            LegacyPoseGraph[] catalog = m_TypedGraphCatalog ??
                Array.Empty<LegacyPoseGraph>();
            return new LegacyCanvasMigrationSource(
                new LegacyCanvasMigrationGraph(m_TypedGraph),
                catalog.Select(value => new LegacyCanvasMigrationGraph(value))
                    .ToArray());
        }

        internal void RequireLegacyCanvasMigrationInput()
        {
            string assetPath = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrWhiteSpace(assetPath))
                throw new InvalidOperationException(
                    $"Pose asset '{name}' must be saved before migration.");
            if (m_TypedGraph == null)
                throw new InvalidOperationException(
                    $"Pose asset '{name}' has no legacy root graph to migrate.");
            if (m_Graph != null ||
                (m_GraphCatalog != null && m_GraphCatalog.Length != 0))
            {
                throw new InvalidOperationException(
                    $"Pose asset '{name}' already contains Canvas Graph references; mixed migration is rejected.");
            }
            if (AssetDatabase.LoadAllAssetsAtPath(assetPath)
                    .OfType<CharacterPoseCanvasGraph>()
                    .Any())
            {
                throw new InvalidOperationException(
                    $"Pose asset '{name}' already contains Canvas Graph subassets; migration is rejected.");
            }

            var identities = new HashSet<PoseGraphId>();
            RequireLegacyGraph(m_TypedGraph, "root", identities);
            LegacyPoseGraph[] catalog = m_TypedGraphCatalog ??
                Array.Empty<LegacyPoseGraph>();
            for (int i = 0; i < catalog.Length; i++)
                RequireLegacyGraph(catalog[i], $"catalog[{i}]", identities);
        }

        static void RequireLegacyGraph(
            LegacyPoseGraph graph,
            string identity,
            HashSet<PoseGraphId> graphIdentities)
        {
            if (graph == null || !graph.GraphId.IsValid ||
                string.IsNullOrWhiteSpace(graph.ContentRevision) ||
                graph.m_Parameters == null || graph.m_Nodes == null ||
                graph.m_Edges == null || graph.m_Layout == null ||
                !graphIdentities.Add(graph.GraphId))
            {
                throw new InvalidOperationException(
                    $"Legacy Pose Graph '{identity}' is missing, invalid or duplicated.");
            }
            for (int i = 0; i < graph.m_Nodes.Length; i++)
            {
                LegacyPoseNode node = graph.m_Nodes[i];
                if (node == null || string.IsNullOrWhiteSpace(node.m_NodeId) ||
                    node.m_Payload == null || node.m_DynamicPorts == null)
                {
                    throw new InvalidOperationException(
                        $"Legacy Pose Graph '{graph.GraphId}' contains an incomplete node at index {i}.");
                }
            }
            for (int i = 0; i < graph.m_Edges.Length; i++)
            {
                LegacyPoseEdge edge = graph.m_Edges[i];
                if (edge == null || string.IsNullOrWhiteSpace(edge.m_EdgeId) ||
                    string.IsNullOrWhiteSpace(edge.m_SourceNodeId) ||
                    string.IsNullOrWhiteSpace(edge.m_SourcePortId) ||
                    string.IsNullOrWhiteSpace(edge.m_TargetNodeId) ||
                    string.IsNullOrWhiteSpace(edge.m_TargetPortId))
                {
                    throw new InvalidOperationException(
                        $"Legacy Pose Graph '{graph.GraphId}' contains an incomplete edge at index {i}.");
                }
            }
            if (graph.m_Layout.Any(value => value == null))
                throw new InvalidOperationException(
                    $"Legacy Pose Graph '{graph.GraphId}' contains an incomplete layout.");
        }

        internal CharacterPoseCanvasGraph[] CreateLegacyCanvasGraphs()
        {
            RequireLegacyCanvasMigrationInput();
            var legacyGraphs = new List<LegacyPoseGraph> { m_TypedGraph };
            legacyGraphs.AddRange(m_TypedGraphCatalog ?? Array.Empty<LegacyPoseGraph>());
            var graphs = new CharacterPoseCanvasGraph[legacyGraphs.Count];
            try
            {
                for (int graphIndex = 0; graphIndex < legacyGraphs.Count; graphIndex++)
                {
                    LegacyPoseGraph legacy = legacyGraphs[graphIndex] ??
                        throw new InvalidOperationException(
                            $"Pose asset '{name}' contains a missing legacy graph at index {graphIndex}.");
                    CharacterPoseCanvasNode[] nodes = legacy.Nodes
                        .Select(node => node == null
                            ? throw new InvalidOperationException(
                                $"Pose asset '{name}' contains a missing legacy node.")
                            : new CharacterPoseCanvasNode(
                                node.NodeId,
                                node.DisplayName,
                                node.Payload,
                                node.DynamicPorts.ToArray()))
                        .ToArray();
                    CharacterPoseCanvasConnection[] edges = legacy.Edges
                        .Select(edge => edge == null
                            ? throw new InvalidOperationException(
                                $"Pose asset '{name}' contains a missing legacy edge.")
                            : new CharacterPoseCanvasConnection(
                                edge.EdgeId,
                                edge.SourceNodeId,
                                edge.SourcePortId,
                                edge.TargetNodeId,
                                edge.TargetPortId))
                        .ToArray();
                    graphs[graphIndex] = CharacterPoseCanvasGraph.CreateAuthoring(
                        legacy.GraphId,
                        legacy.ContentRevision,
                        legacy.Parameters.ToArray(),
                        nodes,
                        edges,
                        legacy.Layout);
                }
            }
            catch
            {
                foreach (CharacterPoseCanvasGraph graph in graphs)
                    if (graph)
                        UnityEngine.Object.DestroyImmediate(graph);
                throw;
            }
            return graphs;
        }

        internal void RestoreLegacyCanvasMigrationState(
            LegacyCanvasMigrationState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            state.RestoreTo(this);
        }

        internal void SetMigratedCanvasGraphs(
            CharacterPoseCanvasGraph root,
            CharacterPoseCanvasGraph[] catalog)
        {
            if (!root)
                throw new ArgumentNullException(nameof(root));
            CharacterPoseCanvasGraph[] values = catalog ??
                Array.Empty<CharacterPoseCanvasGraph>();
            AttachGraph(root);
            for (int i = 0; i < values.Length; i++)
                AttachGraph(values[i] ?? throw new ArgumentException(
                    $"Migrated Pose Graph catalog entry #{i} is missing.",
                    nameof(catalog)));
            m_Graph = root;
            m_GraphCatalog = values;
            m_TypedGraph = null;
            m_TypedGraphCatalog = Array.Empty<LegacyPoseGraph>();
        }
#endif

        public IEnumerable<CharacterPoseStateMachineDefinition>
            EnumerateStateMachines() =>
            EnumerateGraphs()
                .Where(value => value != null)
                .SelectMany(value => value.Nodes)
                .Select(value => value?.Payload)
                .OfType<CharacterPoseStateMachineNodePayload>()
                .Select(value => value.StateMachine)
                .Where(value => value != null);

        public IReadOnlyList<CharacterPoseStateMachineLayoutElement>
            GetExplicitStateMachineLayout(PoseStateMachineId stateMachineId)
        {
            CharacterPoseStateMachineLayout layout =
                FindExplicitStateMachineLayout(stateMachineId);
            return layout?.Elements ??
                   Array.Empty<CharacterPoseStateMachineLayoutElement>();
        }

        public Vector2 ResolveStateMachineElementPosition(
            CharacterPoseStateMachineDefinition stateMachine,
            string elementId)
        {
            if (stateMachine == null ||
                !stateMachine.StateMachineId.IsValid ||
                string.IsNullOrWhiteSpace(elementId))
                throw new ArgumentException(
                    "Pose StateMachine layout lookup is invalid.");
            CharacterPoseStateMachineDefinition owned =
                RequireStateMachine(stateMachine.StateMachineId);
            if (!ReferenceEquals(owned, stateMachine))
                throw new InvalidOperationException(
                    $"Pose StateMachine '{stateMachine.StateMachineId}' is not owned by '{name}'.");
            CharacterPoseStateMachineLayoutElement explicitElement =
                GetExplicitStateMachineLayout(stateMachine.StateMachineId)
                    .SingleOrDefault(value => string.Equals(
                        value.ElementId,
                        elementId,
                        StringComparison.Ordinal));
            if (explicitElement != null)
                return explicitElement.Position;
            if (string.Equals(
                    stateMachine.Entry.EntryId.Value,
                    elementId,
                    StringComparison.Ordinal))
                return new Vector2(-360f, 0f);
            string[] states = stateMachine.States
                .Where(value => value != null)
                .Select(value => value.StateId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            int stateIndex = Array.IndexOf(states, elementId);
            if (stateIndex >= 0)
                return new Vector2(0f, stateIndex * 160f);
            string[] aliases = stateMachine.Aliases
                .Where(value => value != null)
                .Select(value => value.AliasId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            int aliasIndex = Array.IndexOf(aliases, elementId);
            if (aliasIndex >= 0)
                return new Vector2(-180f, (aliasIndex + 1) * 160f);
            throw new InvalidOperationException(
                $"Pose StateMachine '{stateMachine.StateMachineId}' has no layout element '{elementId}'.");
        }

        internal void SetStateMachineLayoutElement(
            PoseStateMachineId stateMachineId,
            string elementId,
            Vector2 position)
        {
            CharacterPoseStateMachineDefinition machine =
                RequireStateMachine(stateMachineId);
            RequireKnownLayoutElement(machine, elementId);
            var elements = GetExplicitStateMachineLayout(stateMachineId)
                .Where(value => !string.Equals(
                    value.ElementId,
                    elementId,
                    StringComparison.Ordinal))
                .Concat(new[]
                {
                    new CharacterPoseStateMachineLayoutElement(
                        elementId,
                        position)
                })
                .ToArray();
            SetStateMachineLayout(stateMachineId, elements);
        }

        internal void RemoveStateMachineLayoutElement(
            PoseStateMachineId stateMachineId,
            string elementId)
        {
            RequireStateMachine(stateMachineId);
            CharacterPoseStateMachineLayout current =
                FindExplicitStateMachineLayout(stateMachineId);
            if (current == null)
                return;
            CharacterPoseStateMachineLayoutElement[] elements = current.Elements
                .Where(value => !string.Equals(
                    value.ElementId,
                    elementId,
                    StringComparison.Ordinal))
                .ToArray();
            if (elements.Length == current.Elements.Count)
                return;
            SetStateMachineLayout(stateMachineId, elements);
        }

        internal void SetStateMachineLayout(
            PoseStateMachineId stateMachineId,
            CharacterPoseStateMachineLayoutElement[] elements)
        {
            CharacterPoseStateMachineDefinition machine =
                RequireStateMachine(stateMachineId);
            CharacterPoseStateMachineLayoutElement[] values = elements ??
                Array.Empty<CharacterPoseStateMachineLayoutElement>();
            CharacterPoseStateMachineLayout.RequireValidElements(values);
            foreach (CharacterPoseStateMachineLayoutElement element in values)
                RequireKnownLayoutElement(machine, element.ElementId);
            var layouts = (m_StateMachineLayouts ??
                           Array.Empty<CharacterPoseStateMachineLayout>())
                .Where(value => value != null &&
                                !value.StateMachineId.Equals(stateMachineId))
                .ToList();
            if (values.Length > 0)
                layouts.Add(new CharacterPoseStateMachineLayout(
                    stateMachineId,
                    values));
            m_StateMachineLayouts = layouts
                .OrderBy(value => value.StateMachineId)
                .ToArray();
        }

        CharacterPoseStateMachineLayout FindExplicitStateMachineLayout(
            PoseStateMachineId stateMachineId)
        {
            if (!stateMachineId.IsValid)
                throw new ArgumentException(
                    "Pose StateMachine layout owner identity is invalid.",
                    nameof(stateMachineId));
            CharacterPoseStateMachineLayout[] matches =
                (m_StateMachineLayouts ??
                 Array.Empty<CharacterPoseStateMachineLayout>())
                .Where(value => value != null &&
                                value.StateMachineId.Equals(stateMachineId))
                .ToArray();
            if (matches.Length > 1)
                throw new InvalidOperationException(
                    $"Pose StateMachine '{stateMachineId}' has duplicate layout owners.");
            if (matches.Length == 1)
                CharacterPoseStateMachineLayout.RequireValidElements(
                    matches[0].Elements);
            return matches.SingleOrDefault();
        }

        CharacterPoseStateMachineDefinition RequireStateMachine(
            PoseStateMachineId stateMachineId)
        {
            CharacterPoseStateMachineDefinition[] matches =
                EnumerateStateMachines()
                    .Where(value => value.StateMachineId.Equals(stateMachineId))
                    .ToArray();
            return matches.Length == 1
                ? matches[0]
                : throw new InvalidOperationException(
                    $"Pose StateMachine '{stateMachineId}' must have exactly one root-owned node.");
        }

        static void RequireKnownLayoutElement(
            CharacterPoseStateMachineDefinition machine,
            string elementId)
        {
            bool known = string.Equals(
                             machine.Entry.EntryId.Value,
                             elementId,
                             StringComparison.Ordinal) ||
                         machine.States.Any(value => value != null &&
                             string.Equals(
                                 value.StateId.Value,
                                 elementId,
                                 StringComparison.Ordinal)) ||
                         machine.Aliases.Any(value => value != null &&
                             string.Equals(
                                 value.AliasId.Value,
                                 elementId,
                                 StringComparison.Ordinal));
            if (!known)
                throw new InvalidOperationException(
                    $"Pose StateMachine '{machine.StateMachineId}' has no Entry, State or Alias '{elementId}'.");
        }
    }
}
