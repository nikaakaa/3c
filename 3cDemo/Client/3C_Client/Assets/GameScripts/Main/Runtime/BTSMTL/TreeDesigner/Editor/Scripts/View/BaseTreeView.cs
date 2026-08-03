using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using BTSMTL;
using BTSMTL.Editor;

namespace TreeDesigner.Editor
{
    public interface IGraphAuthoringReadOnlyView
    {
        void SetRuntimeReadOnly(bool readOnly);
    }

    public partial class GraphAuthoringCanvasView :
        GraphView,
        IGraphAuthoringDomainView
    {
        readonly Dictionary<GraphElement, Capabilities>
            m_ReadOnlyCapabilities =
                new Dictionary<GraphElement, Capabilities>();
        IGraphAuthoringDocument m_AuthoringDocument;
        IGraphAuthoringPortPolicy m_PortPolicy;
        IGraphAuthoringMutationAdapter m_AuthoringMutation;
        bool m_RuntimeReadOnly;

        public GraphAuthoringCanvasView()
        {
            StyleSheet styleSheet =
                Resources.Load<StyleSheet>("StyleSheet/BaseTree");
            styleSheets.Add(styleSheet);
            Insert(0, new GridBackground());
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new TreeRectangleSelector());
        }

        public bool RuntimeReadOnly => m_RuntimeReadOnly;
        protected IGraphAuthoringDocument AuthoringDocument =>
            m_AuthoringDocument;

        public void BindAdapters(
            IGraphAuthoringDocument document,
            IGraphAuthoringPortPolicy portPolicy,
            IGraphAuthoringMutationAdapter mutation)
        {
            m_AuthoringDocument = document ??
                throw new ArgumentNullException(nameof(document));
            m_PortPolicy = portPolicy ??
                throw new ArgumentNullException(nameof(portPolicy));
            m_AuthoringMutation = mutation ??
                throw new ArgumentNullException(nameof(mutation));
        }

        public override List<Port> GetCompatiblePorts(
            Port startPort,
            NodeAdapter nodeAdapter)
        {
            if (m_ProjectionBinding != null)
                return GetProjectionCompatiblePorts(startPort);
            if (m_StateMachineBinding != null)
                return GetStateMachineCompatiblePorts(startPort);
            if (m_RuntimeReadOnly)
                return new List<Port>();
            return m_PortPolicy == null
                ? new List<Port>()
                : ports.ToList()
                    .Where(endPort =>
                        m_PortPolicy.CanConnect(
                            m_AuthoringDocument,
                            startPort,
                            endPort))
                    .ToList();
        }

        protected GraphViewChange ApplyBoundGraphViewChange(
            GraphViewChange change)
        {
            return m_AuthoringMutation == null
                ? change
                : m_AuthoringMutation.ApplyGraphViewChange(
                    m_AuthoringDocument,
                    change);
        }

        public IReadOnlyList<GraphAuthoringSelection>
            GetStableSelection()
        {
            if (m_StateMachineBinding != null)
                return GetStateMachineStableSelection();
            if (m_ProjectionBinding != null)
                return GetProjectionStableSelection();
            return Array.Empty<GraphAuthoringSelection>();
        }

        public void FocusElement(
            GraphAuthoringElementId elementId)
        {
            if (m_StateMachineBinding != null)
            {
                FocusStateMachineElement(elementId);
                return;
            }
            if (m_ProjectionBinding != null)
                FocusProjectionElement(elementId);
        }

        public virtual void SetRuntimeReadOnly(bool readOnly)
        {
            bool changed = m_RuntimeReadOnly != readOnly;
            if (!changed && !readOnly)
                return;
            m_RuntimeReadOnly = readOnly;
            if (readOnly && changed)
                m_ReadOnlyCapabilities.Clear();
            foreach (GraphElement element in graphElements.ToList())
            {
                if (element is IGraphAuthoringReadOnlyView readOnlyView)
                {
                    readOnlyView.SetRuntimeReadOnly(readOnly);
                    continue;
                }
                if (readOnly &&
                    !m_ReadOnlyCapabilities.ContainsKey(element))
                {
                    m_ReadOnlyCapabilities[element] =
                        element.capabilities;
                    element.capabilities &=
                        Capabilities.Selectable |
                        Capabilities.Ascendable;
                }
            }
            if (!readOnly)
            {
                foreach (
                    KeyValuePair<GraphElement, Capabilities> pair
                    in m_ReadOnlyCapabilities)
                {
                    pair.Key.capabilities = pair.Value;
                }
                m_ReadOnlyCapabilities.Clear();
            }
        }
    }

    internal sealed class TreeGraphMutationService
    {
        readonly BaseTreeView m_View;

        public TreeGraphMutationService(BaseTreeView view)
        {
            m_View = view;
        }

        BaseTree Tree => m_View.Tree;
        bool UsesStateMachine =>
            m_View.SharedAuthoring.UsesStateMachineSurface;

        IGraphAuthoringStateMachineProjection StateMachineDocument =>
            m_View.SharedAuthoring.StateMachineDocument;

        IGraphAuthoringDomainMutation StateMachineMutation =>
            m_View.SharedAuthoring.StateMachineMutation;

        public BaseNode CreateNode(
            Type type,
            Vector2 position,
            IBtsmtlNodeCreationPayload payload = null)
        {
            if (!Tree.CanCreateNodeType(type))
                return null;
            BtsmtlSharedAuthoringWorkspaceBinding binding =
                m_View.SharedAuthoring;
            if (!binding.Capabilities.TryGetByAuthoringType(
                    binding.Document.DomainId,
                    type,
                    out GraphAuthoringCapabilityDescriptor capability))
            {
                throw new InvalidOperationException(
                    $"BTSMTL Node type '{type.FullName}' has no formal capability.");
            }
            binding.Capabilities.Require(
                capability.CapabilityId,
                binding.Document.DomainId,
                binding.Document.DocumentRoleId);
            var existing = new HashSet<string>(
                Tree.Nodes
                    .Where(value => value != null)
                    .Select(value => value.GUID),
                StringComparer.Ordinal);
            Vector2 localPosition =
                (position -
                 new Vector2(
                     m_View.viewTransform.position.x,
                     m_View.viewTransform.position.y)) /
                m_View.scale;
            if (UsesStateMachine && type == typeof(StateNode) && payload == null)
                StateMachineMutation.Apply(StateMachineDocument, new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.CreateState, value: capability.DisplayName, position: localPosition));
            else
                binding.Mutation.Apply(binding.Document, new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.CreateNode, capabilityId: capability.CapabilityId, value: payload, position: localPosition));
            BaseNode node = Tree.Nodes.Single(value =>
                value != null &&
                !existing.Contains(value.GUID));
            m_View.CreateNodeView(node);
            return node;
        }

        public bool DeleteNode(BaseNode node, bool confirmed)
        {
            if (!confirmed && !ConfirmConditionEdges(CollectConditionEdgesForNode(node)))
                return false;
            if (UsesStateMachine && node is StateNode)
                StateMachineMutation.Apply(
                    StateMachineDocument,
                    new GraphAuthoringMutationRequest(
                        GraphAuthoringMutationKind.DeleteState,
                        new GraphAuthoringElementId(node.GUID)));
            else
                m_View.SharedAuthoring.Mutation.Apply(
                    m_View.SharedAuthoring.Document,
                    new GraphAuthoringMutationRequest(
                        GraphAuthoringMutationKind.DeleteElement,
                        new GraphAuthoringElementId(node.GUID)));
            return true;
        }

        public NodeGroup CreateNodeGroup(Vector2 position)
        {
            var existing = new HashSet<NodeGroup>(
                Tree.NodeGroups);
            Vector2 localPosition =
                (position -
                 new Vector2(
                     m_View.viewTransform.position.x,
                     m_View.viewTransform.position.y)) /
                m_View.scale;
            m_View.SharedAuthoring.Mutation.Apply(
                m_View.SharedAuthoring.Document,
                new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.CreateGroup,
                    position: localPosition));
            NodeGroup group = Tree.NodeGroups.Single(value =>
                !existing.Contains(value));
            m_View.CreateNodeGroupView(group);
            return group;
        }

        public void DeleteNodeGroup(NodeGroup group)
        {
            m_View.SharedAuthoring.Mutation.Apply(
                m_View.SharedAuthoring.Document,
                new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.DeleteGroup,
                    value:
                    new BtsmtlNodeGroupMutationPayload(group)));
        }

        public StackNode CreateStackNode(Vector2 position)
        {
            var existing = new HashSet<string>(
                Tree.StackNodes
                    .Where(value => value != null)
                    .Select(value => value.GUID),
                StringComparer.Ordinal);
            Vector2 localPosition =
                (position -
                 new Vector2(
                     m_View.viewTransform.position.x,
                     m_View.viewTransform.position.y)) /
                m_View.scale;
            m_View.SharedAuthoring.Mutation.Apply(
                m_View.SharedAuthoring.Document,
                new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.CreateStack,
                    position: localPosition));
            StackNode stack = Tree.StackNodes.Single(value =>
                value != null &&
                !existing.Contains(value.GUID));
            m_View.CreateStackNodeView(stack);
            return stack;
        }

        public void DeleteStackNode(StackNode stack)
        {
            m_View.SharedAuthoring.Mutation.Apply(
                m_View.SharedAuthoring.Document,
                new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.DeleteStack,
                    new GraphAuthoringElementId(stack.GUID),
                    value:
                    new BtsmtlStackMutationPayload(stack)));
        }

        public BaseEdge Link(BaseNode startNode, BaseNode endNode, string outputName, string inputName)
        {
            var existing = new HashSet<string>(
                Tree.Edges
                    .Where(value => value != null)
                    .Select(value => value.GUID),
                StringComparer.Ordinal);
            if (UsesStateMachine)
            {
                GraphAuthoringElementId source =
                    new GraphAuthoringElementId(startNode.GUID);
                GraphAuthoringElementId target =
                    new GraphAuthoringElementId(endNode.GUID);
                IGraphAuthoringStateMachinePolicy policy =
                    m_View.SharedAuthoring.StateMachinePolicy;
                if (!policy.CanCreateTransition(
                        StateMachineDocument,
                        source,
                        target))
                    throw new InvalidOperationException(
                        $"BTSMTL StateMachine rejects transition '{startNode.ResolvedDisplayName}' → '{endNode.ResolvedDisplayName}'.");
                StateMachineMutation.Apply(
                    StateMachineDocument,
                    new GraphAuthoringMutationRequest(
                        GraphAuthoringMutationKind.CreateTransition,
                        source,
                        secondaryTargetId: target,
                        value: policy.CreateTransitionPayload(
                            StateMachineDocument,
                            source,
                            target)));
                if (startNode is StateMachineEnterNode)
                {
                    BaseEdge entry = Tree.Edges.Single(value =>
                        value != null &&
                        value.StartNodeGUID == startNode.GUID);
                    m_View.schedule.Execute(() =>
                        m_View.PopulateView(Tree));
                    return entry;
                }
                return Tree.Edges.Single(value =>
                    value != null &&
                    !existing.Contains(value.GUID));
            }
            m_View.SharedAuthoring.Mutation.Apply(
                m_View.SharedAuthoring.Document,
                new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.ConnectPorts,
                    sourceNodeId:
                    new GraphAuthoringElementId(
                        startNode.GUID),
                    sourcePortId:
                    BtsmtlSharedGraphPort.Flow(outputName),
                    targetNodeId:
                    new GraphAuthoringElementId(
                        endNode.GUID),
                    targetPortId:
                    BtsmtlSharedGraphPort.Flow(inputName)));
            return Tree.Edges.Single(value =>
                value != null &&
                !existing.Contains(value.GUID));
        }

        public bool Unlink(BaseEdge edge, bool confirmed)
        {
            if (!confirmed && !ConfirmConditionEdges(new[] { edge }))
                return false;
            if (UsesStateMachine && BaseEdgeView.IsStateMachineTransition(edge))
                StateMachineMutation.Apply(StateMachineDocument, new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.DeleteTransition, new GraphAuthoringElementId(edge.GUID)));
            else
                m_View.SharedAuthoring.Mutation.Apply(m_View.SharedAuthoring.Document, new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.DisconnectEdge, new GraphAuthoringElementId(edge.GUID)));
            return true;
        }

        public PropertyEdge LinkProperty(
            BaseNode startNode,
            BaseNode endNode,
            PropertyPort startPort,
            PropertyPort endPort,
            bool resolveVariableTypes)
        {
            var existing = new HashSet<string>(
                Tree.PropertyEdges
                    .Where(value => value != null)
                    .Select(value => value.GUID),
                StringComparer.Ordinal);
            m_View.SharedAuthoring.Mutation.Apply(
                m_View.SharedAuthoring.Document,
                new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.ConnectPorts,
                    sourceNodeId:
                    new GraphAuthoringElementId(
                        startNode.GUID),
                    sourcePortId:
                    BtsmtlSharedGraphPort.Property(
                        startPort.PortId),
                    targetNodeId:
                    new GraphAuthoringElementId(
                        endNode.GUID),
                    targetPortId:
                    BtsmtlSharedGraphPort.Property(
                        endPort.PortId)));
            return Tree.PropertyEdges.Single(value =>
                value != null &&
                !existing.Contains(value.GUID));
        }

        public void UnlinkProperty(PropertyEdge edge, bool variable)
        {
            m_View.SharedAuthoring.Mutation.Apply(
                m_View.SharedAuthoring.Document,
                new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.DisconnectEdge,
                    new GraphAuthoringElementId(edge.GUID)));
        }

        public GraphViewChange ApplyGraphViewChange(GraphViewChange change)
        {
            if (m_View.RuntimeReadOnly)
            {
                change.edgesToCreate = null;
                change.elementsToRemove = new List<GraphElement>();
                change.movedElements = null;
                return change;
            }
            CreateEdges(change.edgesToCreate);
            if (change.elementsToRemove != null && !DeleteGraphElements(change.elementsToRemove))
            {
                change.elementsToRemove = new List<GraphElement>();
                return change;
            }
            ForwardMovedElements(change.movedElements);
            return change;
        }

        public string Serialize(IEnumerable<GraphElement> elements)
        {
            return m_View.SharedAuthoring.Clipboard
                .SerializeElements(
                    m_View.SharedAuthoring.Document,
                    elements);
        }

        public bool CanPaste(string serializedData)
        {
            return !m_View.RuntimeReadOnly &&
                   m_View.SharedAuthoring.Clipboard
                       .CanPasteElements(
                           m_View.SharedAuthoring.Document,
                           serializedData);
        }

        public void Paste(string operationName, string serializedData)
        {
            if (Tree == null || m_View.RuntimeReadOnly)
                return;
            var nodeIds = new HashSet<string>(
                Tree.Nodes
                    .Where(value => value != null)
                    .Select(value => value.GUID),
                StringComparer.Ordinal);
            var stackIds = new HashSet<string>(
                Tree.StackNodes
                    .Where(value => value != null)
                    .Select(value => value.GUID),
                StringComparer.Ordinal);
            var groups = new HashSet<NodeGroup>(
                Tree.NodeGroups);
            m_View.SharedAuthoring.Clipboard.PasteElements(
                m_View.SharedAuthoring.Document,
                operationName,
                serializedData,
                m_View.LocalMousePosition);
            m_View.PopulateView(Tree);
            m_View.ClearSelection();
            foreach (BaseNodeView view in m_View.NodeViews)
            {
                if (!nodeIds.Contains(view.Node.GUID))
                    m_View.AddToSelection(view);
            }
            foreach (StackNodeView view in
                     m_View.StackNodeViews)
            {
                if (!stackIds.Contains(view.StackNode.GUID))
                    m_View.AddToSelection(view);
            }
            foreach (NodeGroupView view in
                     m_View.NodeGroupViews)
            {
                if (!groups.Contains(view.NodeGroup))
                    m_View.AddToSelection(view);
            }
        }

        void CreateEdges(IEnumerable<Edge> edges)
        {
            if (edges == null)
                return;
            foreach (Edge edge in edges)
            {
                if (edge is PropertyEdgeView propertyView)
                {
                    bool variable = propertyView.StartPropertyPortView is VariablePropertyPortView ||
                                    propertyView.EndPropertyPortView is VariablePropertyPortView;
                    propertyView.Edge = LinkProperty(
                        propertyView.StartNodeView.Node,
                        propertyView.EndNodeView.Node,
                        propertyView.StartPropertyPortView.PropertyPort,
                        propertyView.EndPropertyPortView.PropertyPort,
                        variable);
                    propertyView.StartNodeView.OnOutputPropertyPortConnected(propertyView.StartPropertyPortView);
                    propertyView.EndNodeView.OnInputPropertyPortConnected(propertyView.EndPropertyPortView);
                }
                else if (edge is BaseEdgeView edgeView)
                {
                    edgeView.Edge = Link(
                        edgeView.StartNodeView.Node,
                        edgeView.EndNodeView.Node,
                        edgeView.StartPortView.Name,
                        edgeView.EndPortView.Name);
                    edgeView.StartNodeView.OnOutputPortConnected(edgeView.StartPortView);
                    edgeView.EndNodeView.OnInputPortConnected(edgeView.EndPortView);
                }
            }
        }

        bool DeleteGraphElements(IReadOnlyCollection<GraphElement> elements)
        {
            if (!ConfirmConditionEdges(CollectConditionEdgesToRemove(elements)))
                return false;
            BaseNodeView[] nodeViews = elements.OfType<BaseNodeView>().ToArray();
            HashSet<BaseNode> deletedNodes = new HashSet<BaseNode>(nodeViews.Select(view => view.Node));
            BaseEdgeView[] edgeViews = elements.OfType<BaseEdgeView>().ToArray();
            NodeGroupView[] groupViews = elements.OfType<NodeGroupView>().ToArray();
            StackNodeView[] stackViews = elements.OfType<StackNodeView>().ToArray();
            var requests =
                new List<GraphAuthoringMutationRequest>();
            var stateMachineRequests =
                new List<GraphAuthoringMutationRequest>();
            foreach (BaseEdgeView edgeView in edgeViews)
            {
                if (edgeView.Edge == null ||
                    deletedNodes.Contains(
                        edgeView.Edge.StartNode) ||
                    deletedNodes.Contains(
                        edgeView.Edge.EndNode))
                    continue;
                bool stateTransition = UsesStateMachine && BaseEdgeView.IsStateMachineTransition(edgeView.Edge);
                (stateTransition ? stateMachineRequests : requests).Add(
                    new GraphAuthoringMutationRequest(
                        stateTransition ? GraphAuthoringMutationKind.DeleteTransition : GraphAuthoringMutationKind.DisconnectEdge,
                        new GraphAuthoringElementId(edgeView.Edge.GUID)));
            }
            foreach (BaseNodeView nodeView in nodeViews)
            {
                bool state =
                    UsesStateMachine &&
                    nodeView.Node is StateNode;
                (state ? stateMachineRequests : requests).Add(
                    new GraphAuthoringMutationRequest(
                        state
                            ? GraphAuthoringMutationKind.DeleteState
                            : GraphAuthoringMutationKind.DeleteElement,
                        new GraphAuthoringElementId(
                            nodeView.Node.GUID)));
            }
            foreach (NodeGroupView groupView in groupViews)
            {
                requests.Add(
                    new GraphAuthoringMutationRequest(
                        GraphAuthoringMutationKind.DeleteGroup,
                        value:
                        new BtsmtlNodeGroupMutationPayload(
                            groupView.NodeGroup)));
            }
            foreach (StackNodeView stackView in stackViews)
            {
                requests.Add(
                    new GraphAuthoringMutationRequest(
                        GraphAuthoringMutationKind.DeleteStack,
                        new GraphAuthoringElementId(
                            stackView.StackNode.GUID),
                        value:
                        new BtsmtlStackMutationPayload(
                            stackView.StackNode)));
            }
            if (requests.Count > 0)
            {
                m_View.SharedAuthoring.Mutation.Apply(
                    m_View.SharedAuthoring.Document,
                    requests);
            }
            if (stateMachineRequests.Count > 0)
                StateMachineMutation.Apply(
                    StateMachineDocument,
                    stateMachineRequests);
            foreach (BaseEdgeView edgeView in edgeViews)
            {
                if (edgeView is PropertyEdgeView propertyView)
                {
                    propertyView.StartNodeView.OnOutputPropertyPortDisconnected(propertyView.StartPropertyPortView);
                    propertyView.EndNodeView.OnInputPropertyPortDisconnected(propertyView.EndPropertyPortView);
                }
                else
                {
                    edgeView.StartNodeView.OnOutputPortDisconnected(edgeView.StartPortView);
                    edgeView.EndNodeView.OnInputPortDisconnected(edgeView.EndPortView);
                }
            }
            foreach (BaseNodeView view in nodeViews)
                m_View.NodeViews.Remove(view);
            foreach (NodeGroupView view in groupViews)
                m_View.NodeGroupViews.Remove(view);
            foreach (StackNodeView view in stackViews)
                m_View.StackNodeViews.Remove(view);
            Tree.GetNewSerializedTree();
            if (nodeViews.Length > 0)
                m_View.NodeViews.ForEach(view => view.SyncSerializedPropertyPathes());
            return true;
        }

        public void ForwardMovedElements(IEnumerable<GraphElement> elements)
        {
            if (elements == null)
                return;
            var requests =
                new Dictionary<string, GraphAuthoringMutationRequest>(
                    StringComparer.Ordinal);
            var groups =
                new Dictionary<NodeGroup, GraphAuthoringMutationRequest>();
            foreach (GraphElement element in elements.ToArray())
            {
                if (element is BaseNodeView nodeView)
                {
                    AddNodeMove(
                        requests,
                        nodeView,
                        nodeView.GetPosition().position);
                }
                else if (element is NodeGroupView groupView)
                {
                    groups[groupView.NodeGroup] =
                        new GraphAuthoringMutationRequest(
                            GraphAuthoringMutationKind.MoveElement,
                            value:
                            new BtsmtlNodeGroupMutationPayload(
                                groupView.NodeGroup),
                            position:
                            groupView.GetPosition().position);
                    foreach (BaseNodeView child in
                             groupView.NodeViews)
                    {
                        AddNodeMove(
                            requests,
                            child,
                            child.GetPosition().position);
                    }
                    foreach (StackNodeView child in
                             groupView.StackNodeViews)
                    {
                        AddStackMove(
                            requests,
                            child,
                            child.GetPosition().position);
                    }
                }
                else if (element is StackNodeView stackView)
                {
                    AddStackMove(
                        requests,
                        stackView,
                        stackView.GetPosition().position);
                    foreach (BaseNodeView child in
                             stackView.NodeViews)
                    {
                        AddNodeMove(
                            requests,
                            child,
                            child.GetPosition().position);
                    }
                }
            }
            var batch =
                new List<GraphAuthoringMutationRequest>(
                    requests.Values);
            batch.AddRange(groups.Values);
            var stateMachineBatch =
                new List<GraphAuthoringMutationRequest>();
            if (UsesStateMachine)
            {
                HashSet<string> nodeIds = Tree.Nodes
                    .Where(value => value != null)
                    .Select(value => value.GUID)
                    .ToHashSet(StringComparer.Ordinal);
                stateMachineBatch.AddRange(batch.Where(value =>
                    value.TargetId.IsValid &&
                    nodeIds.Contains(value.TargetId.Value)));
                batch.RemoveAll(stateMachineBatch.Contains);
            }
            if (batch.Count > 0)
            {
                m_View.SharedAuthoring.Mutation.Apply(
                    m_View.SharedAuthoring.Document,
                    batch);
            }
            if (stateMachineBatch.Count > 0)
                StateMachineMutation.Apply(
                    StateMachineDocument,
                    stateMachineBatch);
        }

        static void AddNodeMove(
            IDictionary<string, GraphAuthoringMutationRequest>
                requests,
            BaseNodeView view,
            Vector2 position)
        {
            string id = view.Node.GUID;
            requests[id] =
                new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.MoveElement,
                    new GraphAuthoringElementId(id),
                    position: position);
        }

        static void AddStackMove(
            IDictionary<string, GraphAuthoringMutationRequest>
                requests,
            StackNodeView view,
            Vector2 position)
        {
            string id = view.StackNode.GUID;
            requests[id] =
                new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.MoveElement,
                    new GraphAuthoringElementId(id),
                    value:
                    new BtsmtlStackMutationPayload(
                        view.StackNode),
                    position: position);
        }

        bool ConfirmConditionEdges(IEnumerable<BaseEdge> edges)
        {
            foreach (BaseEdge edge in edges)
            {
                if (!BaseEdgeView.ConfirmConditionEdgeDeletion(edge))
                    return false;
            }
            return true;
        }

        IEnumerable<BaseEdge> CollectConditionEdgesToRemove(IEnumerable<GraphElement> elements)
        {
            HashSet<string> edgeGuids = new HashSet<string>();
            foreach (GraphElement element in elements)
            {
                if (element is BaseEdgeView edgeView && edgeView.Edge != null && edgeGuids.Add(edgeView.Edge.GUID))
                    yield return edgeView.Edge;
                if (!(element is BaseNodeView nodeView))
                    continue;
                foreach (BaseEdge edge in CollectConditionEdgesForNode(nodeView.Node))
                {
                    if (edge != null && edgeGuids.Add(edge.GUID))
                        yield return edge;
                }
            }
        }

        IEnumerable<BaseEdge> CollectConditionEdgesForNode(BaseNode node)
        {
            if (node == null || Tree == null)
                yield break;
            foreach (BaseEdge edge in Tree.Edges)
            {
                if (edge == null ||
                    !(BaseEdgeView.IsStateMachineTransition(edge) || BaseEdgeView.IsBTConditionFlowEdge(edge)))
                    continue;
                if (edge.StartNode == node || edge.EndNode == node)
                    yield return edge;
            }
        }

        static void DeleteOwnedConditionRuleGraphs(IEnumerable<BaseEdge> edges)
        {
            foreach (BaseEdge edge in edges)
                DeleteOwnedConditionRuleGraph(edge);
        }

        static void DeleteOwnedConditionRuleGraph(BaseEdge edge)
        {
            BaseEdgeView.DeleteOwnedConditionRuleGraphForEdgeDelete(edge);
        }
    }

    public class BaseTreeView : GraphAuthoringCanvasView
    {
        public new class UxmlFactory : UxmlFactory<BaseTreeView, UxmlTraits> { }

        protected BaseTree m_Tree;
        public BaseTree Tree => m_Tree;
        BtsmtlSharedAuthoringWorkspaceBinding
            m_SharedAuthoring;
        internal BtsmtlSharedAuthoringWorkspaceBinding
            SharedAuthoring =>
            m_SharedAuthoring ??
            throw new InvalidOperationException(
                "BTSMTL shared authoring binding is missing.");

        protected BaseTreeWindow m_TreeWindow;
        public BaseTreeWindow TreeWindow => m_TreeWindow;

        protected List<BaseNodeView> m_NodeViews = new List<BaseNodeView>();
        public List<BaseNodeView> NodeViews => m_NodeViews;

        protected List<StackNodeView> m_StackNodeViews = new List<StackNodeView>();
        public List<StackNodeView> StackNodeViews => m_StackNodeViews;

        protected List<NodeGroupView> m_NodeGroupViews = new List<NodeGroupView>();
        public List<NodeGroupView> NodeGroupViews => m_NodeGroupViews;

        protected Vector2 m_LocalMousePosition;
        public Vector2 LocalMousePosition => m_LocalMousePosition;

        protected Label m_NodeDescription;
        protected DropArea m_DropArea;
        TreeGraphMutationService m_MutationService;
        public string TargetTypeStr;

        public BaseTreeView()
        {
            m_NodeDescription = new Label();
            m_NodeDescription.name = "node-description";
            Add(m_NodeDescription);

            IMGUIContainer nodeSearchContainer = new IMGUIContainer(() =>
            {
                GUILayout.BeginHorizontal();
                TargetTypeStr = GUILayout.TextField(TargetTypeStr, GUILayout.MinWidth(200));

                if (GUILayout.Button("Find"))
                {
                    var targetType = TreeDesignerUtility.GetNodeType(TargetTypeStr);
                    if (m_Tree == null || targetType == null)
                        return;

                    foreach (var nodeView in m_NodeViews)
                    {
                        if (nodeView.Node.GetType() == targetType)
                            AddToSelection(nodeView);
                    }
                }
                GUILayout.EndHorizontal();
            });
            Add(nodeSearchContainer);
            nodeSearchContainer.name = "nodeSearchContainer";

            RegisterCallback<KeyDownEvent>(KeyDownCallback);
            RegisterCallback<MouseMoveEvent>(MouseMoveCallback);

            m_DropArea = new DropArea();
            m_DropArea.Init(this);
            m_DropArea.DragValid = () =>
                !RuntimeReadOnly &&
                InputActionNodeDragFactory.CanCreateFromDrag(this);
            m_DropArea.onDragPerformEvent += e =>
            {
                if (!RuntimeReadOnly)
                    InputActionNodeDragFactory.CreateFromDrag(this, e);
            };
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (m_Tree == null) return;
            if (RuntimeReadOnly)
                return;
            base.BuildContextualMenu(evt);
            if (evt.target is GraphView)
            {
                Vector2 position = evt.localMousePosition;
                evt.menu.InsertAction(1, "Create Group", (s) =>
                {
                    CreateNodeGroup(position);
                });
                evt.menu.InsertAction(2, "Create Stack", (s) =>
                {
                    CreateStackNode(position);
                });
            }
            if (evt.target is IGroupable)
            {
                List<IGroupable> groupableElements = new List<IGroupable>();
                selection.ForEach(i =>
                {
                    if (i is IGroupable groupable && groupable.NodeGroupView != null)
                        groupableElements.Add(groupable);
                });
                if (groupableElements.Count > 0)
                {
                    evt.menu.AppendAction("RemoveTagWithChildren From Group", (s) =>
                    {
                        groupableElements.ForEach(i => i.NodeGroupView.RemoveFromGroup(i));
                    });
                }
            }

            if ((evt.target is BaseNodeView nodeView && nodeView.StackNodeView == null) || evt.target is StackNodeView)
            {
                GraphElement selectedElement = evt.target as GraphElement;
                Vector2 offset = Vector2.zero;

                evt.menu.AppendAction("Align To Top", (s) =>
                {
                    List<BaseNodeView> targetNodeViews = m_NodeViews.ToList().Where(i => !selection.Contains(i)).
                                                                              Where(i => contentContainer.worldBound.Contains(i.worldBound.center)).
                                                                              OrderBy(i => Vector2.Distance(i.Node.Position, selectedElement.GetPosition().position) + 3 * Mathf.Abs(i.Node.Position.y - selectedElement.GetPosition().yMin)).ToList();
                    if (targetNodeViews.Count > 0)
                    {
                        BaseNodeView targetNodeView = targetNodeViews[0];
                        if (selectedElement is BaseNodeView selectedNodeView)
                        {
                            Vector2 originalPosition = selectedNodeView.Node.Position;
                            Vector2 targetPosition = new Vector2(selectedNodeView.Node.Position.x, targetNodeView.Node.Position.y + (targetNodeView.StackNodeView != null ? 4 : 0));
                            offset = targetPosition - originalPosition;
                            selectedNodeView.SetPosition(new Rect(targetPosition, Vector2.zero));
                        }
                        else if (selectedElement is StackNodeView selectedStackNodeView)
                        {
                            Vector2 originalPosition = selectedStackNodeView.StackNode.Position;
                            Vector2 targetPosition = new Vector2(selectedStackNodeView.StackNode.Position.x, targetNodeView.Node.Position.y + (targetNodeView.StackNodeView != null ? 4 : 0));
                            offset = targetPosition - originalPosition;
                            selectedStackNodeView.SetPosition(new Rect(targetPosition, Vector2.zero));
                        }

                        SetElements();
                    }
                });
                evt.menu.AppendAction("Align To Bottom", (s) =>
                {
                    List<BaseNodeView> targetNodeViews = m_NodeViews.ToList().Where(i => !selection.Contains(i)).
                                                                              Where(i => contentContainer.worldBound.Contains(i.worldBound.center)).
                                                                              OrderBy(i => Vector2.Distance(i.Node.Position, selectedElement.GetPosition().position) + 3 * Mathf.Abs((i.Node.Position.y + i.layout.height) - selectedElement.GetPosition().yMax)).ToList();
                    if (targetNodeViews.Count > 0)
                    {
                        BaseNodeView targetNodeView = targetNodeViews[0];
                        if (selectedElement is BaseNodeView selectedNodeView)
                        {
                            Vector2 originalPosition = selectedNodeView.Node.Position;
                            Vector2 targetPosition = new Vector2(selectedNodeView.Node.Position.x, targetNodeView.Node.Position.y + targetNodeView.layout.height - selectedElement.layout.height + (targetNodeView.StackNodeView != null ? 4 : 0));
                            offset = targetPosition - originalPosition;
                            selectedNodeView.SetPosition(new Rect(targetPosition, Vector2.zero));
                        }
                        else if (selectedElement is StackNodeView selectedStackNodeView)
                        {
                            Vector2 originalPosition = selectedStackNodeView.StackNode.Position;
                            Vector2 targetPosition = new Vector2(selectedStackNodeView.StackNode.Position.x, targetNodeView.Node.Position.y + targetNodeView.layout.height - selectedElement.layout.height + (targetNodeView.StackNodeView != null ? 4 : 0));
                            offset = targetPosition - originalPosition;
                            selectedStackNodeView.SetPosition(new Rect(targetPosition, Vector2.zero));
                        }

                        SetElements();
                    }
                });
                evt.menu.AppendAction("Align To Space Right", (s) =>
                {
                    List<BaseNodeView> targetNodeViews = m_NodeViews.ToList().Where(i => !selection.Contains(i)).
                                                                              Where(i => contentContainer.worldBound.Contains(i.worldBound.center)).
                                                                              Where(i => i.Node.Position.x < selectedElement.GetPosition().position.x).
                                                                              OrderBy(i => Vector2.Distance(i.Node.Position, selectedElement.GetPosition().position) + Mathf.Abs(i.Node.Position.x + i.layout.width - selectedElement.GetPosition().xMin)).ToList();
                    if (targetNodeViews.Count > 0)
                    {
                        BaseNodeView targetNodeView = targetNodeViews[0];
                        if (selectedElement is BaseNodeView selectedNodeView)
                        {
                            Vector2 originalPosition = selectedNodeView.Node.Position;
                            Vector2 targetPosition = new Vector2(targetNodeView.Node.Position.x + targetNodeView.layout.width + (targetNodeView.StackNodeView != null ? 28 : 26), selectedNodeView.Node.Position.y);
                            offset = targetPosition - originalPosition;
                            selectedNodeView.SetPosition(new Rect(targetPosition, Vector2.zero));
                        }
                        else if (selectedElement is StackNodeView selectedStackNodeView)
                        {
                            Vector2 originalPosition = selectedStackNodeView.StackNode.Position;
                            Vector2 targetPosition = new Vector2(targetNodeView.Node.Position.x + targetNodeView.layout.width + (targetNodeView.StackNodeView != null ? 14 : 12), selectedStackNodeView.StackNode.Position.y);
                            offset = targetPosition - originalPosition;
                            selectedStackNodeView.SetPosition(new Rect(targetPosition, Vector2.zero));
                        }

                        SetElements();
                    }
                });

                void SetElements()
                {
                    foreach (var selectable in selection)
                    {
                        if (selectable == selectedElement) continue;

                        if (selectable is BaseNodeView nodeView)
                        {
                            Vector2 targetPosition = nodeView.Node.Position + offset;
                            nodeView.SetPosition(new Rect(targetPosition, Vector2.zero));
                        }
                        else if (selectable is StackNodeView stackNodeView)
                        {
                            Vector2 targetPosition = stackNodeView.StackNode.Position + offset;
                            stackNodeView.SetPosition(new Rect(targetPosition, Vector2.zero));
                        }
                    }
                    var movedElements = selection
                        .OfType<GraphElement>()
                        .ToList();
                    if (!movedElements.Contains(selectedElement))
                        movedElements.Add(selectedElement);
                    CommitMovedElements(movedElements);
                }
            }
        }

        public bool Empty { get; private set; } = true;
        public virtual void Init(BaseTreeWindow treeWindow)
        {
            m_TreeWindow = treeWindow;
            m_MutationService = new TreeGraphMutationService(this);
        }

        internal void CommitMovedElements(
            IEnumerable<GraphElement> elements)
        {
            if (m_MutationService == null)
                throw new InvalidOperationException(
                    "BTSMTL mutation service is not initialized.");
            m_MutationService.ForwardMovedElements(elements);
        }

        public void BindSharedAuthoring(
            BtsmtlSharedAuthoringWorkspaceBinding binding)
        {
            m_SharedAuthoring = binding ??
                throw new ArgumentNullException(nameof(binding));
            m_SharedAuthoring.SetReadOnly(RuntimeReadOnly);
        }

        public override List<Port> GetCompatiblePorts(
            Port startPort,
            NodeAdapter nodeAdapter)
        {
            if (RuntimeReadOnly ||
                m_SharedAuthoring == null ||
                !(startPort is BasePortView source))
            {
                return new List<Port>();
            }
            return ports
                .OfType<BasePortView>()
                .Where(target =>
                    !ReferenceEquals(
                        source.NodeView,
                        target.NodeView) &&
                    source.direction != target.direction &&
                    CanConnectShared(source, target))
                .Cast<Port>()
                .ToList();
        }

        public override void SetRuntimeReadOnly(bool readOnly)
        {
            m_SharedAuthoring?.SetReadOnly(readOnly);
            base.SetRuntimeReadOnly(readOnly);
        }

        bool CanConnectShared(
            BasePortView first,
            BasePortView second)
        {
            BasePortView output =
                first.direction == Direction.Output
                    ? first
                    : second;
            BasePortView input =
                first.direction == Direction.Input
                    ? first
                    : second;
            IGraphAuthoringDocumentProjection document =
                m_SharedAuthoring.Document;
            GraphAuthoringNodeProjection source =
                document.Nodes.Single(value =>
                    value.NodeId.Value ==
                    output.NodeView.Node.GUID);
            GraphAuthoringNodeProjection target =
                document.Nodes.Single(value =>
                    value.NodeId.Value ==
                    input.NodeView.Node.GUID);
            return m_SharedAuthoring.ConnectionPolicy.CanConnect(
                document,
                source,
                SharedPortId(output),
                target,
                SharedPortId(input));
        }

        static GraphAuthoringPortId SharedPortId(
            BasePortView port)
        {
            if (!port.AuthoringPortId.IsValid)
            {
                throw new InvalidOperationException(
                    $"BTSMTL Port '{port.Name}' has no formal authoring identity.");
            }
            return port.AuthoringPortId;
        }

        public virtual void PopulateView(BaseTree tree)
        {
            bool restoreReadOnly = RuntimeReadOnly;
            if (restoreReadOnly)
                SetRuntimeReadOnly(false);
            ClearView();
            Empty = false;
            m_Tree = tree;
            if (!ReferenceEquals(SharedAuthoring.Graph, tree))
            {
                throw new InvalidOperationException(
                    "BTSMTL Canvas received a document binding for a different Graph.");
            }
            foreach (GraphAuthoringNodeProjection projection in
                     SharedAuthoring.Document.Nodes)
            {
                BaseNode node = m_Tree.Nodes.Single(value =>
                    value != null &&
                    value.GUID == projection.NodeId.Value);
                CreateNodeView(node);
            }
            foreach (GraphAuthoringEdgeProjection projection in
                     SharedAuthoring.Document.Edges)
            {
                BaseEdge flow = m_Tree.Edges.SingleOrDefault(
                    value =>
                        value != null &&
                        value.GUID == projection.EdgeId.Value);
                if (flow != null)
                {
                    CreateEdgeView(flow);
                    continue;
                }
                PropertyEdge property =
                    m_Tree.PropertyEdges.SingleOrDefault(value =>
                        value != null &&
                        value.GUID == projection.EdgeId.Value);
                if (property == null)
                {
                    throw new InvalidOperationException(
                        $"BTSMTL document Edge '{projection.EdgeId}' has no authoring object.");
                }
                CreatePropertyEdgeView(property);
            }
            m_Tree.StackNodes.ForEach(i => CreateStackNodeView(i));
            m_Tree.NodeGroups.ForEach(i => CreateNodeGroupView(i));
            m_NodeViews.ForEach(i => i.RefreshNodeExpandedState());
            graphViewChanged += OnGraphViewChanged;
            if (restoreReadOnly)
                SetRuntimeReadOnly(true);
        }
        public virtual void ClearView()
        {
            Empty = true;
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(m_NodeGroupViews);
            DeleteElements(m_StackNodeViews);
            DeleteElements(m_NodeViews);
            DeleteElements(graphElements.ToList());
            m_NodeViews.Clear();
            m_StackNodeViews.Clear();
            m_NodeGroupViews.Clear();
        }

        internal string SerializeGraphElements(IEnumerable<GraphElement> elements) => m_MutationService.Serialize(elements);
        internal bool CanPasteGraphElements(string payload) => m_MutationService.CanPaste(payload);
        internal void PasteGraphElements(string operationName, string payload) => m_MutationService.Paste(operationName, payload);
        internal GraphViewChange ApplyDomainGraphViewChange(GraphViewChange change) => m_MutationService.ApplyGraphViewChange(change);

        public virtual BaseNode CreateNode(
            Type type,
            Vector2 position,
            IBtsmtlNodeCreationPayload payload = null)
        {
            return m_MutationService.CreateNode(
                type,
                position,
                payload);
        }

        public virtual bool DeleteNode(BaseNode node, bool confirmed = false)
        {
            return m_MutationService.DeleteNode(node, confirmed);
        }

        public virtual NodeGroup CreateNodeGroup(Vector2 position)
        {
            return m_MutationService.CreateNodeGroup(position);
        }

        public virtual void DeleteNodeGroup(NodeGroup nodeGroup)
        {
            m_MutationService.DeleteNodeGroup(nodeGroup);
        }

        public virtual StackNode CreateStackNode(Vector2 position)
        {
            return m_MutationService.CreateStackNode(position);
        }

        public virtual void DeleteStackNode(StackNode stackNode)
        {
            m_MutationService.DeleteStackNode(stackNode);
        }

        public virtual BaseEdge Link(BaseNode startNode, BaseNode endNode, string outputName, string inputName)
        {
            return m_MutationService.Link(startNode, endNode, outputName, inputName);
        }

        public virtual bool UnLink(BaseEdge edge, bool confirmed = false)
        {
            return m_MutationService.Unlink(edge, confirmed);
        }

        public virtual PropertyEdge LinkProperty(
            BaseNode startNode,
            BaseNode endNode,
            PropertyPort startPropertyPort,
            PropertyPort endPropertyPort)
        {
            return m_MutationService.LinkProperty(startNode, endNode, startPropertyPort, endPropertyPort, false);
        }

        public virtual void UnLinkProperty(PropertyEdge propertyEdge)
        {
            m_MutationService.UnlinkProperty(propertyEdge, false);
        }

        public virtual PropertyEdge LinkVariableProperty(
            BaseNode startNode,
            BaseNode endNode,
            PropertyPort startPropertyPort,
            PropertyPort endPropertyPort)
        {
            return m_MutationService.LinkProperty(startNode, endNode, startPropertyPort, endPropertyPort, true);
        }

        public virtual void UnLinkVariableProperty(PropertyEdge propertyEdge)
        {
            m_MutationService.UnlinkProperty(propertyEdge, true);
        }

        public virtual BaseNodeView FindNodeView(BaseNode node)
        {
            return GetNodeByGuid(node.GUID) as BaseNodeView;
        }
        public virtual BaseNodeView FindNodeView(string guid)
        {
            return GetNodeByGuid(guid) as BaseNodeView;
        }

        public virtual BaseNodeView CreateNodeView(BaseNode node)
        {
            NodeViewAttribute nodeViewAttribute = node.GetAttribute<NodeViewAttribute>();
            BaseNodeView nodeView;

            m_Tree.GetNewSerializedTree();

            if (nodeViewAttribute == null)
                nodeView = new BaseNodeView(node, m_TreeWindow);
            else
                nodeView = Activator.CreateInstance(TreeDesignerUtility.GetNodeViewType(nodeViewAttribute.NodeViewTypeName), node, m_TreeWindow) as BaseNodeView;

            BindSharedProjection(nodeView);
            AddElement(nodeView);
            m_NodeViews.Add(nodeView);
            return nodeView;
        }

        void BindSharedProjection(BaseNodeView nodeView)
        {
            GraphAuthoringNodeProjection projection =
                SharedAuthoring.Document.Nodes.Single(value =>
                    value.NodeId.Value ==
                    nodeView.Node.GUID);
            GraphAuthoringCapabilityDescriptor capability =
                SharedAuthoring.Capabilities.Require(
                    projection.CapabilityId,
                    SharedAuthoring.Document.DomainId,
                    SharedAuthoring.Document.DocumentRoleId);
            nodeView.BindSharedProjection(
                projection,
                capability);
            Dictionary<
                GraphAuthoringPortId,
                GraphAuthoringPortDescriptor> fixedPorts =
                capability.FixedPorts.ToDictionary(
                    value => value.PortId);
            Dictionary<
                GraphAuthoringPortId,
                GraphAuthoringDynamicPortProjection>
                dynamicPorts =
                    projection.DynamicPorts.ToDictionary(
                        value => value.PortId);
            var materialized =
                new HashSet<GraphAuthoringPortId>();
            foreach (BasePortView port in
                     nodeView.InputPorts.Values.Concat(
                         nodeView.OutputPorts.Values))
            {
                BindSharedPort(
                    port,
                    fixedPorts,
                    dynamicPorts,
                    materialized);
            }
            foreach (PropertyPortView port in
                     nodeView.InputPropertyPorts.Values.Concat(
                         nodeView.OutputPropertyPorts.Values))
            {
                BindSharedPort(
                    port,
                    fixedPorts,
                    dynamicPorts,
                    materialized);
            }
            foreach (GraphAuthoringPortId expected in
                     fixedPorts.Keys.Concat(dynamicPorts.Keys))
            {
                if (!materialized.Contains(expected))
                {
                    throw new InvalidOperationException(
                        $"BTSMTL Node '{nodeView.Node.GUID}' did not materialize projected Port '{expected}'.");
                }
            }
        }

        static void BindSharedPort(
            BasePortView port,
            IReadOnlyDictionary<
                GraphAuthoringPortId,
                GraphAuthoringPortDescriptor> fixedPorts,
            IReadOnlyDictionary<
                GraphAuthoringPortId,
                GraphAuthoringDynamicPortProjection>
                dynamicPorts,
            ISet<GraphAuthoringPortId> materialized)
        {
            GraphAuthoringPortId id =
                port is PropertyPortView
                    ? BtsmtlSharedGraphPort.Property(port.Name)
                    : BtsmtlSharedGraphPort.Flow(port.Name);
            if (!materialized.Add(id))
            {
                throw new InvalidOperationException(
                    $"BTSMTL Port '{id}' was materialized more than once.");
            }
            if (fixedPorts.TryGetValue(
                    id,
                    out GraphAuthoringPortDescriptor descriptor))
            {
                port.BindFixedAuthoringPort(descriptor);
                return;
            }
            if (dynamicPorts.TryGetValue(
                    id,
                    out GraphAuthoringDynamicPortProjection
                        projection))
            {
                port.BindDynamicAuthoringPort(projection);
                return;
            }
            throw new InvalidOperationException(
                $"BTSMTL Port '{id}' has no formal capability projection.");
        }
        public virtual NodeGroupView CreateNodeGroupView(NodeGroup nodeGroup)
        {
            NodeGroupView nodeGroupView = new NodeGroupView(nodeGroup, this);
            AddElement(nodeGroupView);
            m_NodeGroupViews.Add(nodeGroupView);
            return nodeGroupView;
        }
        public virtual StackNodeView CreateStackNodeView(StackNode stackNode)
        {
            StackNodeView stackNodeView = new StackNodeView(stackNode, this);
            AddElement(stackNodeView);
            m_StackNodeViews.Add(stackNodeView);
            return stackNodeView;
        }
        public virtual BaseEdgeView CreateEdgeView(BaseEdge edge)
        {
            BaseNodeView startNodeView = FindNodeView(edge.StartNode);
            BaseNodeView endNodeView = FindNodeView(edge.EndNode);

            if (startNodeView != null && startNodeView.OutputPorts.TryGetValue(edge.StartPortName, out BasePortView startPortView) &&
                endNodeView != null && endNodeView.InputPorts.TryGetValue(edge.EndPortName, out BasePortView endPortView))
            {
                BaseEdgeView edgeView = startPortView.ConnectTo<BaseEdgeView>(endPortView);
                edgeView.Edge = edge;
                BindSharedEdge(
                    edgeView,
                    edge.GUID,
                    startNodeView,
                    startPortView,
                    endNodeView,
                    endPortView);
                endNodeView.OnInputPortConnected(endPortView);
                AddElement(edgeView);
                return edgeView;
            }
            else
                return null;
        }
        public virtual PropertyEdgeView CreatePropertyEdgeView(PropertyEdge propertyEdge)
        {
            BaseNodeView startNodeView = FindNodeView(propertyEdge.StartNode);
            BaseNodeView endNodeView = FindNodeView(propertyEdge.EndNode);

            if (startNodeView != null && startNodeView.OutputPropertyPorts.TryGetValue(propertyEdge.StartPortName, out PropertyPortView startPropertyPortView)/*FindPortView(startNodeView.Node,propertyEdge.StartPortName) is PropertyPortView startPropertyPortView*/ &&
                endNodeView != null && endNodeView.InputPropertyPorts.TryGetValue(propertyEdge.EndPortName, out PropertyPortView endPropertyPortView)/*FindPortView(endNodeView.Node, propertyEdge.EndPortName) is PropertyPortView endPropertyPortView*/)
            {
                PropertyEdgeView propertyEdgeView = startPropertyPortView.ConnectTo<PropertyEdgeView>(endPropertyPortView);
                propertyEdgeView.Edge = propertyEdge;
                BindSharedEdge(
                    propertyEdgeView,
                    propertyEdge.GUID,
                    startNodeView,
                    startPropertyPortView,
                    endNodeView,
                    endPropertyPortView);
                startNodeView.OnOutputPropertyPortConnected(startPropertyPortView);
                endNodeView.OnInputPropertyPortConnected(endPropertyPortView);
                AddElement(propertyEdgeView);
                return propertyEdgeView;
            }
            else
                return null;

        }

        void BindSharedEdge(
            GraphAuthoringEdgeViewBase edgeView,
            string edgeGuid,
            BaseNodeView sourceNode,
            BasePortView sourcePort,
            BaseNodeView targetNode,
            BasePortView targetPort)
        {
            GraphAuthoringEdgeProjection projection =
                SharedAuthoring.Document.Edges.Single(value =>
                    value.EdgeId.Value == edgeGuid);
            if (projection.SourceNodeId.Value !=
                    sourceNode.Node.GUID ||
                !projection.SourcePortId.Equals(
                    SharedPortId(sourcePort)) ||
                projection.TargetNodeId.Value !=
                    targetNode.Node.GUID ||
                !projection.TargetPortId.Equals(
                    SharedPortId(targetPort)))
            {
                throw new InvalidOperationException(
                    $"BTSMTL Edge '{edgeGuid}' does not match its formal document projection.");
            }
            edgeView.BindAuthoringProjection(projection);
        }

        protected virtual GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            return ApplyBoundGraphViewChange(graphViewChange);
        }

        void KeyDownCallback(KeyDownEvent e)
        {
            if (m_Tree == null)
                return;

            if (e.ctrlKey)
            {
                if (e.keyCode == KeyCode.S)
                {
                }
            }
        }

        void MouseMoveCallback(MouseMoveEvent e)
        {
            var windowMousePosition = m_TreeWindow.rootVisualElement.ChangeCoordinatesTo(m_TreeWindow.rootVisualElement, e.originalMousePosition);
            m_LocalMousePosition = contentViewContainer.WorldToLocal(windowMousePosition);
        }
    }
}
