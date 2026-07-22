using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [Serializable]
    sealed class PoseGraphClipboardPayload
    {
        public CharacterPoseNodeDefinition[] nodes = Array.Empty<CharacterPoseNodeDefinition>();
        public CharacterPoseEdge[] edges = Array.Empty<CharacterPoseEdge>();
        public Vector2 center;
    }

    sealed class PoseGraphPosePortValue { }
    sealed class PoseGraphParameterPortValue { }

    sealed class PoseGraphNodeView : Node
    {
        readonly Dictionary<PosePortId, Port> m_Ports = new Dictionary<PosePortId, Port>();
        readonly Label m_Diagnostic = new Label();

        public PoseGraphNodeView(CharacterPoseNodeDefinition node, Action<CharacterPoseNodeDefinition> openSubgraph)
        {
            Node = node;
            title = string.IsNullOrWhiteSpace(node.DisplayName) ? node.Kind.ToString() : node.DisplayName;
            viewDataKey = node.NodeId.Value;
            SetPosition(new Rect(node.Position, new Vector2(220f, 120f)));
            for (int i = 0; i < node.Ports.Count; i++)
            {
                CharacterPosePortDefinition definition = node.Ports[i];
                Direction direction = definition.Direction == CharacterPosePortDirection.Input ? Direction.Input : Direction.Output;
                Port.Capacity capacity = direction == Direction.Input ? Port.Capacity.Single : Port.Capacity.Multi;
                Type valueType = definition.Kind == CharacterPosePortKind.Pose
                    ? typeof(PoseGraphPosePortValue)
                    : typeof(PoseGraphParameterPortValue);
                Port port = Port.Create<Edge>(Orientation.Horizontal, direction, capacity, valueType);
                port.portName = string.IsNullOrWhiteSpace(definition.Name) ? definition.PortId.Value : definition.Name;
                port.userData = definition;
                m_Ports.Add(definition.PortId, port);
                (direction == Direction.Input ? inputContainer : outputContainer).Add(port);
            }
            m_Diagnostic.style.whiteSpace = WhiteSpace.Normal;
            m_Diagnostic.style.color = new Color(1f, 0.55f, 0.35f);
            m_Diagnostic.style.display = DisplayStyle.None;
            extensionContainer.Add(m_Diagnostic);
            if (node.Kind == CharacterPoseNodeKind.PoseSubgraph)
            {
                RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button == 0 && evt.clickCount == 2)
                        openSubgraph(Node);
                });
            }
            RefreshExpandedState();
            RefreshPorts();
        }

        public CharacterPoseNodeDefinition Node { get; }

        public bool TryGetPort(PosePortId portId, out Port port) => m_Ports.TryGetValue(portId, out port);

        public void SetDiagnostics(IReadOnlyList<string> messages)
        {
            if (messages == null || messages.Count == 0)
            {
                m_Diagnostic.text = string.Empty;
                m_Diagnostic.style.display = DisplayStyle.None;
                return;
            }
            m_Diagnostic.text = string.Join("\n", messages);
            m_Diagnostic.style.display = DisplayStyle.Flex;
            expanded = true;
            RefreshExpandedState();
        }
    }

    sealed class PoseGraphView : GraphView, IGraphAuthoringDomainView
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;
        readonly Dictionary<PoseNodeId, PoseGraphNodeView> m_Nodes = new Dictionary<PoseNodeId, PoseGraphNodeView>();
        IGraphAuthoringDocument m_Document;
        IGraphAuthoringPortPolicy m_PortPolicy;
        IGraphAuthoringMutationAdapter m_Mutation;
        bool m_Rebuilding;

        public PoseGraphView(CharacterPresentationPoseGraphEditorWindow window)
        {
            m_Window = window;
            StyleSheet style = Resources.Load<StyleSheet>("StyleSheet/BaseTree");
            if (style)
                styleSheets.Add(style);
            Insert(0, new GridBackground());
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            graphViewChanged = ApplyChange;
            RegisterCallback<MouseMoveEvent>(evt =>
            {
                LocalMousePosition = contentViewContainer.WorldToLocal(evt.originalMousePosition);
            });
        }

        public Vector2 LocalMousePosition { get; private set; }

        public void BindAdapters(
            IGraphAuthoringDocument document,
            IGraphAuthoringPortPolicy portPolicy,
            IGraphAuthoringMutationAdapter mutation)
        {
            m_Document = document;
            m_PortPolicy = portPolicy;
            m_Mutation = mutation;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return m_PortPolicy == null
                ? new List<Port>()
                : ports.ToList().Where(port => m_PortPolicy.CanConnect(m_Document, startPort, port)).ToList();
        }

        public void Populate(CharacterPoseGraphData graph)
        {
            m_Rebuilding = true;
            DeleteElements(graphElements.ToList());
            m_Nodes.Clear();
            if (graph != null)
            {
                for (int i = 0; i < graph.Nodes.Count; i++)
                {
                    CharacterPoseNodeDefinition node = graph.Nodes[i];
                    if (node == null)
                        continue;
                    var view = new PoseGraphNodeView(node, m_Window.OpenSubgraph);
                    m_Nodes.Add(node.NodeId, view);
                    AddElement(view);
                }
                for (int i = 0; i < graph.Edges.Count; i++)
                {
                    CharacterPoseEdge edge = graph.Edges[i];
                    if (edge == null || !m_Nodes.TryGetValue(edge.SourceNodeId, out PoseGraphNodeView source) ||
                        !m_Nodes.TryGetValue(edge.TargetNodeId, out PoseGraphNodeView target) ||
                        !source.TryGetPort(edge.SourcePortId, out Port output) ||
                        !target.TryGetPort(edge.TargetPortId, out Port input))
                        continue;
                    Edge view = output.ConnectTo(input);
                    view.userData = edge;
                    AddElement(view);
                }
            }
            m_Rebuilding = false;
        }

        public void ApplyDiagnostics(CharacterPoseGraphValidationReport report, CharacterPoseGraphData graph)
        {
            foreach (KeyValuePair<PoseNodeId, PoseGraphNodeView> pair in m_Nodes)
            {
                IReadOnlyList<string> messages = report?.Issues
                    .Where(issue => string.Equals(issue.GraphId, graph?.GraphId, StringComparison.Ordinal) && issue.NodeId.Equals(pair.Key))
                    .Select(issue => issue.Message)
                    .ToArray() ?? Array.Empty<string>();
                pair.Value.SetDiagnostics(messages);
            }
        }

        GraphViewChange ApplyChange(GraphViewChange change)
        {
            if (m_Rebuilding || m_Window.CurrentGraph == null)
                return change;
            return m_Mutation == null
                ? change
                : m_Mutation.ApplyGraphViewChange(m_Document, change);
        }
    }

    sealed class PoseGraphDocumentAdapter : IGraphAuthoringDocument
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;

        public PoseGraphDocumentAdapter(CharacterPresentationPoseGraphEditorWindow window)
        {
            m_Window = window;
        }

        public string DomainId => "character-presentation-pose-graph";
        public string DocumentId => m_Window.CurrentGraph?.GraphId ?? string.Empty;
        public string DisplayName => m_Window.CurrentDisplayName;
        public string ContentRevision => m_Window.CurrentGraph?.ContentRevision ?? string.Empty;
        public UnityEngine.Object SerializedOwner => m_Window.CurrentOwner;
    }

    sealed class PoseGraphNodeCatalogAdapter : IGraphAuthoringNodeCatalog
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;

        public PoseGraphNodeCatalogAdapter(CharacterPresentationPoseGraphEditorWindow window)
        {
            m_Window = window;
        }

        public IReadOnlyList<GraphAuthoringNodeCatalogEntry> GetEntries(IGraphAuthoringDocument document)
        {
            var kinds = new List<CharacterPoseNodeKind>
            {
                CharacterPoseNodeKind.LayeredBoneBlend,
                CharacterPoseNodeKind.AdditivePose,
                CharacterPoseNodeKind.PoseCurveResolve,
                CharacterPoseNodeKind.PoseSubgraph
            };
            if (!m_Window.IsSubgraphDocument)
            {
                kinds.Insert(0, CharacterPoseNodeKind.PoseSlotInput);
                kinds.Add(CharacterPoseNodeKind.OutputPose);
            }
            return kinds.Select(kind => new GraphAuthoringNodeCatalogEntry("Pose/" + kind, kind.ToString())).ToArray();
        }
    }

    sealed class PoseGraphPortPolicyAdapter : IGraphAuthoringPortPolicy
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;

        public PoseGraphPortPolicyAdapter(CharacterPresentationPoseGraphEditorWindow window)
        {
            m_Window = window;
        }

        public bool CanConnect(IGraphAuthoringDocument document, Port startPort, Port endPort)
        {
            if (startPort == null || endPort == null || startPort.node == endPort.node || startPort.direction == endPort.direction)
                return false;
            CharacterPosePortDefinition start = startPort.userData as CharacterPosePortDefinition;
            CharacterPosePortDefinition end = endPort.userData as CharacterPosePortDefinition;
            if (start == null || end == null || start.Kind != end.Kind)
                return false;
            Port input = startPort.direction == Direction.Input ? startPort : endPort;
            if (input.connections.Any())
                return false;
            PoseGraphNodeView source = (startPort.direction == Direction.Output ? startPort.node : endPort.node) as PoseGraphNodeView;
            PoseGraphNodeView target = (startPort.direction == Direction.Input ? startPort.node : endPort.node) as PoseGraphNodeView;
            return source != null && target != null && !WouldCreateCycle(source.Node.NodeId, target.Node.NodeId);
        }

        bool WouldCreateCycle(PoseNodeId source, PoseNodeId target)
        {
            CharacterPoseGraphData graph = m_Window.CurrentGraph;
            if (graph == null)
                return true;
            var stack = new Stack<PoseNodeId>();
            var visited = new HashSet<PoseNodeId>();
            stack.Push(target);
            while (stack.Count > 0)
            {
                PoseNodeId current = stack.Pop();
                if (!visited.Add(current))
                    continue;
                if (current.Equals(source))
                    return true;
                for (int i = 0; i < graph.Edges.Count; i++)
                {
                    CharacterPoseEdge edge = graph.Edges[i];
                    if (edge != null && edge.SourceNodeId.Equals(current))
                        stack.Push(edge.TargetNodeId);
                }
            }
            return false;
        }
    }

    sealed class PoseGraphMutationAdapter : IGraphAuthoringMutationAdapter
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;

        public PoseGraphMutationAdapter(CharacterPresentationPoseGraphEditorWindow window)
        {
            m_Window = window;
        }

        public bool ReadOnly => false;

        public void CreateNode(IGraphAuthoringDocument document, string typeId, Vector2 graphPosition)
        {
            if (!Enum.TryParse(typeId, out CharacterPoseNodeKind kind) || !IsAllowed(kind))
                throw new InvalidOperationException($"Pose node type '{typeId}' is unknown.");
            CharacterPresentationPoseGraphAuthoringService.CreateNode(m_Window.CurrentOwner, m_Window.CurrentGraph, kind, graphPosition);
            m_Window.RefreshGraphView();
        }

        public GraphViewChange ApplyGraphViewChange(IGraphAuthoringDocument document, GraphViewChange change)
        {
            if (change.edgesToCreate != null)
            {
                for (int edgeIndex = 0; edgeIndex < change.edgesToCreate.Count; edgeIndex++)
                {
                    Edge edge = change.edgesToCreate[edgeIndex];
                    CharacterPoseNodeDefinition sourceNode = (edge.output.node as PoseGraphNodeView)?.Node;
                    CharacterPoseNodeDefinition targetNode = (edge.input.node as PoseGraphNodeView)?.Node;
                    CharacterPosePortDefinition sourcePort = edge.output.userData as CharacterPosePortDefinition;
                    CharacterPosePortDefinition targetPort = edge.input.userData as CharacterPosePortDefinition;
                    if (sourceNode == null || targetNode == null || sourcePort == null || targetPort == null)
                        continue;
                    edge.userData = CharacterPresentationPoseGraphAuthoringService.Connect(
                        m_Window.CurrentOwner,
                        m_Window.CurrentGraph,
                        sourceNode.NodeId,
                        sourcePort.PortId,
                        targetNode.NodeId,
                        targetPort.PortId);
                }
            }
            if (change.elementsToRemove != null)
            {
                PoseNodeId[] removedNodes = change.elementsToRemove.OfType<PoseGraphNodeView>()
                    .Select(view => view.Node.NodeId)
                    .ToArray();
                var nodeSet = new HashSet<PoseNodeId>(removedNodes);
                string[] removedEdges = change.elementsToRemove.OfType<Edge>()
                    .Select(edge => edge.userData as CharacterPoseEdge)
                    .Where(edge => edge != null && !nodeSet.Contains(edge.SourceNodeId) && !nodeSet.Contains(edge.TargetNodeId))
                    .Select(edge => edge.EdgeId)
                    .ToArray();
                CharacterPresentationPoseGraphAuthoringService.DeleteSelection(
                    m_Window.CurrentOwner,
                    m_Window.CurrentGraph,
                    removedNodes,
                    removedEdges);
            }
            if (change.movedElements != null)
            {
                Dictionary<PoseNodeId, Vector2> positions = change.movedElements
                    .OfType<PoseGraphNodeView>()
                    .ToDictionary(view => view.Node.NodeId, view => view.GetPosition().position);
                CharacterPresentationPoseGraphAuthoringService.MoveNodes(
                    m_Window.CurrentOwner,
                    m_Window.CurrentGraph,
                    positions);
            }
            m_Window.NotifyDocumentMutated();
            return change;
        }

        public string SerializeSelection(IGraphAuthoringDocument document, IEnumerable<GraphElement> elements)
        {
            PoseGraphNodeView[] views = elements?.OfType<PoseGraphNodeView>()
                .Where(view => view.Node.Kind != CharacterPoseNodeKind.GraphInput && view.Node.Kind != CharacterPoseNodeKind.GraphOutput)
                .ToArray() ?? Array.Empty<PoseGraphNodeView>();
            var ids = new HashSet<PoseNodeId>(views.Select(view => view.Node.NodeId));
            Vector2 center = views.Length == 0 ? Vector2.zero : views.Aggregate(Vector2.zero, (value, view) => value + view.Node.Position) / views.Length;
            return JsonUtility.ToJson(new PoseGraphClipboardPayload
            {
                nodes = views.Select(view => view.Node).ToArray(),
                edges = m_Window.CurrentGraph.Edges.Where(edge => edge != null && ids.Contains(edge.SourceNodeId) && ids.Contains(edge.TargetNodeId)).ToArray(),
                center = center
            });
        }

        public bool CanPaste(IGraphAuthoringDocument document, string payload)
        {
            try
            {
                PoseGraphClipboardPayload data = JsonUtility.FromJson<PoseGraphClipboardPayload>(payload);
                return data != null && data.nodes != null && data.nodes.Length > 0 &&
                       data.nodes.All(node => node != null && IsAllowed(node.Kind));
            }
            catch
            {
                return false;
            }
        }

        public void Paste(IGraphAuthoringDocument document, string operationName, string payload)
        {
            if (!CanPaste(document, payload))
                throw new InvalidOperationException("Pose Graph clipboard payload is not valid for the current document.");
            PoseGraphClipboardPayload data = JsonUtility.FromJson<PoseGraphClipboardPayload>(payload)
                ?? throw new InvalidOperationException("Pose Graph clipboard payload is invalid.");
            Vector2 offset = m_Window.GraphView.LocalMousePosition - data.center;
            CharacterPoseNodeDefinition[] nodes = CharacterPresentationPoseGraphAuthoringService.CloneNodesWithNewIdentities(
                data.nodes,
                offset,
                out Dictionary<PoseNodeId, PoseNodeId> nodeMap,
                out Dictionary<string, PosePortId> portMap);
            CharacterPoseEdge[] edges = CharacterPresentationPoseGraphAuthoringService.CloneInternalEdges(data.edges, nodeMap, portMap);
            CharacterPresentationPoseGraphAuthoringService.AppendClonedSelection(m_Window.CurrentOwner, m_Window.CurrentGraph, nodes, edges);
            m_Window.RefreshGraphView();
        }

        public void Reload(IGraphAuthoringDocument document)
        {
            m_Window.ReloadAfterUndo();
        }

        bool IsAllowed(CharacterPoseNodeKind kind)
        {
            if (m_Window.IsSubgraphDocument)
            {
                return kind == CharacterPoseNodeKind.LayeredBoneBlend ||
                       kind == CharacterPoseNodeKind.AdditivePose ||
                       kind == CharacterPoseNodeKind.PoseCurveResolve ||
                       kind == CharacterPoseNodeKind.PoseSubgraph;
            }
            return kind == CharacterPoseNodeKind.PoseSlotInput ||
                   kind == CharacterPoseNodeKind.LayeredBoneBlend ||
                   kind == CharacterPoseNodeKind.AdditivePose ||
                   kind == CharacterPoseNodeKind.PoseCurveResolve ||
                   kind == CharacterPoseNodeKind.PoseSubgraph ||
                   kind == CharacterPoseNodeKind.OutputPose;
        }

        public void RenameNode(CharacterPoseGraphData graph, CharacterPoseNodeDefinition node, string displayName)
        {
            CharacterPresentationPoseGraphAuthoringService.RenameNode(m_Window.CurrentOwner, graph, node.NodeId, displayName);
            m_Window.RefreshGraphView();
        }

        public void ConfigureNode(
            CharacterPoseGraphData graph,
            CharacterPoseNodeDefinition node,
            PoseSlotId slot,
            CharacterAnimationBoneMaskAsset mask,
            float weight,
            CharacterPoseParameterPolicy[] policies,
            string additiveReferencePoseId,
            AdditiveReferenceSpace additiveReferenceSpace,
            AdditiveScalePolicy additiveScalePolicy)
        {
            CharacterPresentationPoseGraphAuthoringService.ConfigureNode(
                m_Window.CurrentOwner,
                graph,
                node.NodeId,
                slot,
                mask,
                weight,
                policies,
                additiveReferencePoseId,
                additiveReferenceSpace,
                additiveScalePolicy);
            m_Window.RefreshGraphView();
        }

        public void CreateInline(CharacterPoseGraphData graph, CharacterPoseNodeDefinition node)
        {
            CharacterPresentationPoseGraphAuthoringService.CreateInline(m_Window.CurrentOwner, graph, node.NodeId);
            m_Window.RefreshGraphView();
        }

        public void ExtractShared(CharacterPoseGraphData graph, CharacterPoseNodeDefinition node, string assetPath)
        {
            CharacterPresentationPoseGraphAuthoringService.ExtractShared(m_Window.CurrentOwner, graph, node.NodeId, assetPath);
            m_Window.RefreshGraphView();
        }

        public void UseShared(CharacterPoseGraphData graph, CharacterPoseNodeDefinition node, CharacterPresentationPoseGraphAsset asset)
        {
            CharacterPresentationPoseGraphAuthoringService.UseShared(m_Window.CurrentOwner, graph, node.NodeId, asset);
            m_Window.RefreshGraphView();
        }

        public void ClearShared(CharacterPoseGraphData graph, CharacterPoseNodeDefinition node)
        {
            CharacterPresentationPoseGraphAuthoringService.ClearShared(m_Window.CurrentOwner, graph, node.NodeId);
            m_Window.RefreshGraphView();
        }
    }

    sealed class PoseGraphInspectorAdapter : IGraphAuthoringInspectorAdapter
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;
        readonly PoseGraphMutationAdapter m_Mutation;
        readonly ScrollView m_View = new ScrollView();
        CharacterPoseNodeDefinition m_SelectedNode;

        public PoseGraphInspectorAdapter(CharacterPresentationPoseGraphEditorWindow window, PoseGraphMutationAdapter mutation)
        {
            m_Window = window;
            m_Mutation = mutation;
        }

        public VisualElement View => m_View;

        public void Bind(IGraphAuthoringDocument document)
        {
            Draw(null);
        }

        public void Inspect(IReadOnlyList<ISelectable> selection)
        {
            CharacterPoseNodeDefinition[] nodes = selection?.OfType<PoseGraphNodeView>()
                .Select(view => view.Node)
                .Take(2)
                .ToArray() ?? Array.Empty<CharacterPoseNodeDefinition>();
            CharacterPoseNodeDefinition node = nodes.Length == 1 ? nodes[0] : null;
            if (node == m_SelectedNode)
                return;
            m_SelectedNode = node;
            Draw(node);
        }

        public void Clear()
        {
            m_SelectedNode = null;
            m_View.Clear();
        }

        void Draw(CharacterPoseNodeDefinition node)
        {
            m_View.Clear();
            CharacterPoseGraphData graph = m_Window.CurrentGraph;
            if (graph == null)
            {
                m_View.Add(new HelpBox("Pose Graph document is unavailable.", HelpBoxMessageType.Error));
                return;
            }
            m_View.Add(new Label(m_Window.CurrentDisplayName) { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            m_View.Add(new Label($"Graph {graph.GraphId}"));
            m_View.Add(new Label($"Revision {graph.ContentRevision}"));
            if (node == null)
            {
                m_View.Add(new HelpBox("Select one Pose node to edit its formal authoring fields.", HelpBoxMessageType.Info));
                return;
            }
            m_View.Add(new Label($"{node.Kind} / {node.NodeId}") { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            var displayName = new TextField("Display Name") { value = node.DisplayName, isDelayed = true };
            displayName.RegisterValueChangedCallback(evt =>
            {
                m_Mutation.RenameNode(graph, node, evt.newValue);
            });
            m_View.Add(displayName);

            if (node.Kind == CharacterPoseNodeKind.PoseSlotInput)
                DrawSlot(node, graph);
            if (node.Kind == CharacterPoseNodeKind.LayeredBoneBlend || node.Kind == CharacterPoseNodeKind.AdditivePose)
                DrawMask(node, graph);
            if (node.Kind == CharacterPoseNodeKind.AdditivePose)
                DrawAdditive(node, graph);
            if (node.Kind == CharacterPoseNodeKind.LayeredBoneBlend || node.Kind == CharacterPoseNodeKind.AdditivePose || node.Kind == CharacterPoseNodeKind.PoseCurveResolve)
                DrawPolicies(node, graph);
            if (node.Kind == CharacterPoseNodeKind.PoseSubgraph)
                DrawSubgraph(node, graph);
        }

        void DrawSlot(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph)
        {
            string[] slots = graph.PoseSlots.Select(slot => slot.PoseSlotId.Value).ToArray();
            if (slots.Length == 0)
            {
                m_View.Add(new HelpBox("PoseSlotInput requires a declared Pose Slot.", HelpBoxMessageType.Error));
                return;
            }
            int selected = Math.Max(0, Array.IndexOf(slots, node.PoseSlotId.Value));
            var field = new PopupField<string>("Pose Slot", slots.ToList(), selected);
            field.RegisterValueChangedCallback(evt => Configure(node, graph, new PoseSlotId(evt.newValue), node.BoneMask, node.Weight, node.ParameterPolicies.ToArray()));
            m_View.Add(field);
        }

        void DrawMask(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph)
        {
            var mask = new ObjectField("Bone Mask") { objectType = typeof(CharacterAnimationBoneMaskAsset), value = node.BoneMask };
            mask.RegisterValueChangedCallback(evt => Configure(node, graph, node.PoseSlotId, evt.newValue as CharacterAnimationBoneMaskAsset, node.Weight, node.ParameterPolicies.ToArray()));
            m_View.Add(mask);
            if (node.BoneMask)
            {
                m_View.Add(new Label($"Mask Rig {node.BoneMask.RigId} @ {node.BoneMask.RigRevision}"));
                CharacterAnimationRigDefinition rig = m_Window.RigDefinition;
                string status = rig && string.Equals(rig.RigId, node.BoneMask.RigId, StringComparison.Ordinal) &&
                                string.Equals(rig.Revision, node.BoneMask.RigRevision, StringComparison.Ordinal)
                    ? "Mask Rig matches editor context."
                    : "Mask Rig does not match the current Rig context.";
                m_View.Add(new HelpBox(status, status.StartsWith("Mask Rig matches", StringComparison.Ordinal) ? HelpBoxMessageType.Info : HelpBoxMessageType.Error));
            }
            var weight = new FloatField("Weight") { value = node.Weight, isDelayed = true };
            weight.RegisterValueChangedCallback(evt => Configure(
                node,
                graph,
                node.PoseSlotId,
                node.BoneMask,
                Mathf.Clamp01(evt.newValue),
                node.ParameterPolicies.ToArray()));
            m_View.Add(weight);
        }

        void DrawAdditive(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph)
        {
            m_View.Add(new Label($"Reference {node.AdditiveReferencePoseId}"));
            var space = new EnumField("Reference Space", node.AdditiveReferenceSpace);
            space.RegisterValueChangedCallback(evt => ConfigureAdditive(node, graph, (AdditiveReferenceSpace)evt.newValue, node.AdditiveScalePolicy));
            m_View.Add(space);
            var scale = new EnumField("Scale Policy", node.AdditiveScalePolicy);
            scale.RegisterValueChangedCallback(evt => ConfigureAdditive(node, graph, node.AdditiveReferenceSpace, (AdditiveScalePolicy)evt.newValue));
            m_View.Add(scale);
        }

        void DrawPolicies(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph)
        {
            var current = new Dictionary<PoseParameterId, PoseParameterResolvePolicy>();
            for (int policyIndex = 0; policyIndex < node.ParameterPolicies.Count; policyIndex++)
            {
                CharacterPoseParameterPolicy value = node.ParameterPolicies[policyIndex];
                if (value != null)
                    current.TryAdd(value.ParameterId, value.Policy);
            }
            for (int i = 0; i < graph.Parameters.Count; i++)
            {
                CharacterPoseParameterDeclaration parameter = graph.Parameters[i];
                if (!current.TryGetValue(parameter.ParameterId, out PoseParameterResolvePolicy policy))
                {
                    m_View.Add(new HelpBox($"Missing policy: {parameter.ParameterId}", HelpBoxMessageType.Error));
                    policy = PoseParameterResolvePolicy.Weighted;
                    current[parameter.ParameterId] = policy;
                }
                PoseParameterId parameterId = parameter.ParameterId;
                var field = new EnumField(parameterId.Value, policy);
                field.RegisterValueChangedCallback(evt =>
                {
                    current[parameterId] = (PoseParameterResolvePolicy)evt.newValue;
                    CharacterPoseParameterPolicy[] policies = graph.Parameters
                        .Select(declaration => new CharacterPoseParameterPolicy(declaration.ParameterId, current[declaration.ParameterId]))
                        .ToArray();
                    Configure(node, graph, node.PoseSlotId, node.BoneMask, node.Weight, policies);
                });
                m_View.Add(field);
            }
        }

        void DrawSubgraph(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph)
        {
            CharacterPoseSubgraphReference reference = node.Subgraph;
            string state = reference == null ? "Missing" : reference.HasInline ? "Inline" : reference.HasShared ? "Shared Asset" : "Missing";
            m_View.Add(new Label("Ownership: " + state));
            Button open = new Button(() => m_Window.OpenSubgraph(node)) { text = "Open" };
            open.SetEnabled(reference != null && reference.IsExclusive);
            m_View.Add(open);
            if (reference == null || !reference.IsExclusive)
            {
                m_View.Add(new Button(() =>
                {
                    m_Mutation.CreateInline(graph, node);
                }) { text = "Create Inline" });
            }
            if (reference != null && reference.HasInline && !reference.HasShared)
            {
                m_View.Add(new Button(() =>
                {
                    string path = EditorUtility.SaveFilePanelInProject("Extract Shared Pose Subgraph", "SharedPoseSubgraph", "asset", "Choose the formal shared Pose Graph asset path.");
                    if (string.IsNullOrEmpty(path))
                        return;
                    m_Mutation.ExtractShared(graph, node, path);
                }) { text = "Extract Shared" });
            }
            var shared = new ObjectField("Shared Asset")
            {
                objectType = typeof(CharacterPresentationPoseGraphAsset),
                value = reference != null && reference.HasShared ? reference.Shared : null
            };
            shared.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is CharacterPresentationPoseGraphAsset asset)
                    m_Mutation.UseShared(graph, node, asset);
                else if (reference != null && reference.HasShared)
                    m_Mutation.ClearShared(graph, node);
            });
            m_View.Add(shared);
        }

        void Configure(
            CharacterPoseNodeDefinition node,
            CharacterPoseGraphData graph,
            PoseSlotId slot,
            CharacterAnimationBoneMaskAsset mask,
            float weight,
            CharacterPoseParameterPolicy[] policies)
        {
            m_Mutation.ConfigureNode(
                graph,
                node,
                slot,
                mask,
                weight,
                policies,
                node.AdditiveReferencePoseId,
                node.AdditiveReferenceSpace,
                node.AdditiveScalePolicy);
        }

        void ConfigureAdditive(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph, AdditiveReferenceSpace space, AdditiveScalePolicy scale)
        {
            m_Mutation.ConfigureNode(
                graph,
                node,
                node.PoseSlotId,
                node.BoneMask,
                node.Weight,
                node.ParameterPolicies.ToArray(),
                AnimationAdditiveReferencePoseIds.RigReference,
                space,
                scale);
        }
    }

    sealed class PoseGraphDiagnosticsAdapter : IGraphAuthoringDiagnosticsAdapter
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;
        Label m_Status;

        public PoseGraphDiagnosticsAdapter(CharacterPresentationPoseGraphEditorWindow window)
        {
            m_Window = window;
        }

        public void Bind(IGraphAuthoringDocument document, GraphView graphView, VisualElement toolbar)
        {
            if (m_Status == null)
            {
                m_Status = new Label();
                m_Status.style.flexGrow = 1f;
                m_Status.style.unityTextAlign = TextAnchor.MiddleRight;
                toolbar.Add(m_Status);
            }
            Refresh();
        }

        public void Refresh()
        {
            CharacterPoseGraphData graph = m_Window.CurrentGraph;
            if (graph == null)
                return;
            CharacterAnimationRigDefinition rig = m_Window.RigDefinition;
            if (!rig)
            {
                m_Status.text = "Diagnostics Unavailable: open from a Presentation Profile with an explicit Rig";
                m_Window.GraphView.ApplyDiagnostics(null, graph);
                return;
            }
            if (m_Window.ValidationRoot == null || m_Window.ValidationRoot.Graph.Nodes.Any(node =>
                    node != null && (node.Kind == CharacterPoseNodeKind.GraphInput || node.Kind == CharacterPoseNodeKind.GraphOutput)))
            {
                m_Status.text = "Diagnostics Unavailable: shared subgraph requires a parent call-site context";
                m_Window.GraphView.ApplyDiagnostics(null, graph);
                return;
            }
            CharacterPoseGraphValidationReport report = CharacterPresentationPoseGraphValidator.Validate(m_Window.ValidationRoot, rig);
            string projection = m_Window.ProjectionRevision.Length > 0
                ? $"Projection {m_Window.ProjectionRevision}"
                : "Projection Unavailable";
            m_Status.text = report.IsValid
                ? $"Valid / {graph.ContentRevision} / {projection} / Live Snapshot Unavailable"
                : $"Invalid ({report.Issues.Count}) / {graph.ContentRevision} / {projection} / Live Snapshot Unavailable";
            m_Window.GraphView.ApplyDiagnostics(report, graph);
        }

        public void Clear()
        {
            if (m_Status != null)
                m_Status.RemoveFromHierarchy();
            m_Status = null;
        }
    }

    public sealed class CharacterPresentationPoseGraphEditorWindow : GraphAuthoringEditorShell
    {
        readonly struct Page
        {
            public Page(CharacterPresentationPoseGraphAsset owner, CharacterPoseGraphData graph, string displayName)
            {
                Owner = owner;
                Graph = graph;
                DisplayName = displayName;
            }

            public CharacterPresentationPoseGraphAsset Owner { get; }
            public CharacterPoseGraphData Graph { get; }
            public string DisplayName { get; }
        }

        [SerializeField] CharacterPresentationPoseGraphAsset m_Asset;
        [SerializeField] CharacterAnimationPresentationProfile m_Profile;
        [SerializeField] CharacterPresentationProjectionAsset m_Projection;
        readonly List<Page> m_Pages = new List<Page>();
        PoseGraphView m_GraphView;
        PoseGraphInspectorAdapter m_Inspector;
        PoseGraphMutationAdapter m_Mutation;

        internal PoseGraphView GraphView => m_GraphView;
        public CharacterPoseGraphData CurrentGraph => m_Pages.Count > 0 ? m_Pages[m_Pages.Count - 1].Graph : m_Asset?.Graph;
        public CharacterPresentationPoseGraphAsset CurrentOwner => m_Pages.Count > 0 ? m_Pages[m_Pages.Count - 1].Owner : m_Asset;
        public string CurrentDisplayName => m_Pages.Count > 0 ? m_Pages[m_Pages.Count - 1].DisplayName : m_Asset ? m_Asset.name : "Pose Graph";
        public CharacterPresentationPoseGraphAsset ValidationRoot => m_Asset;
        public CharacterAnimationRigDefinition RigDefinition => m_Profile ? m_Profile.RigDefinition : null;
        public string ProjectionRevision => m_Projection ? m_Projection.ProjectionRevision : string.Empty;
        public bool IsSubgraphDocument => CurrentGraph != null && CurrentGraph.Nodes.Any(node =>
            node != null && (node.Kind == CharacterPoseNodeKind.GraphInput || node.Kind == CharacterPoseNodeKind.GraphOutput));

        public static CharacterPresentationPoseGraphEditorWindow Open(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationPresentationProfile profile = null,
            CharacterPresentationProjectionAsset projection = null)
        {
            if (!asset)
                throw new ArgumentNullException(nameof(asset));
            CharacterPresentationPoseGraphEditorWindow window = GetWindow<CharacterPresentationPoseGraphEditorWindow>();
            window.titleContent = new GUIContent("Presentation Pose Graph");
            window.SetDocument(asset, profile, projection);
            window.Show();
            window.Focus();
            return window;
        }

        protected override GraphView CreateGraphAuthoringView()
        {
            m_GraphView = new PoseGraphView(this);
            return m_GraphView;
        }

        protected override VisualElement CreateGraphAuthoringInspectorView()
        {
            m_Mutation = new PoseGraphMutationAdapter(this);
            m_Inspector = new PoseGraphInspectorAdapter(this, m_Mutation);
            return m_Inspector.View;
        }

        protected override GraphAuthoringDomainAdapters CreateGraphAuthoringAdapters()
        {
            return new GraphAuthoringDomainAdapters(
                new PoseGraphDocumentAdapter(this),
                new PoseGraphNodeCatalogAdapter(this),
                new PoseGraphPortPolicyAdapter(this),
                m_Mutation,
                m_Inspector,
                new PoseGraphDiagnosticsAdapter(this));
        }

        protected override void OnGraphAuthoringShellCreated()
        {
            BindGraphAuthoringNavigation(PopPage);
            if (m_Asset)
                ResetPages();
            RefreshGraphView();
        }

        public void SetDocument(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationProjectionAsset projection)
        {
            m_Asset = asset;
            m_Profile = profile;
            m_Projection = projection;
            ResetPages();
            RefreshGraphView();
        }

        public void OpenSubgraph(CharacterPoseNodeDefinition node)
        {
            if (node == null || node.Kind != CharacterPoseNodeKind.PoseSubgraph || node.Subgraph == null || !node.Subgraph.IsExclusive)
                return;
            CharacterPresentationPoseGraphAsset owner = node.Subgraph.HasShared ? node.Subgraph.Shared : CurrentOwner;
            CharacterPoseGraphData graph = node.Subgraph.HasInline ? node.Subgraph.Inline : node.Subgraph.Shared.Graph;
            string displayName = string.IsNullOrWhiteSpace(node.DisplayName) ? node.NodeId.Value : node.DisplayName;
            m_Pages.Add(new Page(owner, graph, displayName));
            RefreshGraphView();
        }

        public void RefreshGraphView()
        {
            if (m_GraphView == null)
                return;
            m_GraphView.Populate(CurrentGraph);
            RefreshNavigation();
            RebindGraphAuthoringDocument();
        }

        public void NotifyDocumentMutated()
        {
            RebindGraphAuthoringDocument();
        }

        public void ReloadAfterUndo()
        {
            ResetPages();
            if (m_GraphView == null)
                return;
            m_GraphView.Populate(CurrentGraph);
            RefreshNavigation();
        }

        void ResetPages()
        {
            m_Pages.Clear();
            if (m_Asset && m_Asset.Graph != null)
                m_Pages.Add(new Page(m_Asset, m_Asset.Graph, m_Asset.name));
        }

        void PopPage()
        {
            if (m_Pages.Count <= 1)
                return;
            m_Pages.RemoveAt(m_Pages.Count - 1);
            RefreshGraphView();
        }

        void RefreshNavigation()
        {
            GraphAuthoringBreadcrumbEntry[] entries = m_Pages
                .Select(page => new GraphAuthoringBreadcrumbEntry(page.DisplayName, "Pose Graph"))
                .ToArray();
            RenderGraphAuthoringNavigation(entries, PopTo);
        }

        void PopTo(int index)
        {
            if (index < 0 || index >= m_Pages.Count)
                return;
            m_Pages.RemoveRange(index + 1, m_Pages.Count - index - 1);
            RefreshGraphView();
        }

        [UnityEditor.Callbacks.OnOpenAsset]
        static bool OnOpenAsset(int instanceId, int line)
        {
            CharacterPresentationPoseGraphAsset asset = EditorUtility.InstanceIDToObject(instanceId) as CharacterPresentationPoseGraphAsset;
            if (!asset)
                return false;
            Open(asset);
            return true;
        }
    }
}
