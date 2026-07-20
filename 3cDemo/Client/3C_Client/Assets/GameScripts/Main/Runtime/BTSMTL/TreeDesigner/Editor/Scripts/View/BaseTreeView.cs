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
using GraphSelectable = UnityEditor.Experimental.GraphView.ISelectable;

namespace TreeDesigner.Editor
{
    internal readonly struct TreeSelectionIdentity : IEquatable<TreeSelectionIdentity>
    {
        public TreeSelectionIdentity(GraphSelectable selectable)
        {
            if (selectable is BaseEdgeView edgeView && edgeView.Edge != null)
            {
                Kind = 1;
                Identity = edgeView.Edge.GUID ?? string.Empty;
                RevisionA = edgeView.Edge.TransitionPriority;
                RevisionB = (int)edgeView.Edge.AbortPolicy;
                ReferenceId = edgeView.Edge.ConditionRuleGraph
                    ? edgeView.Edge.ConditionRuleGraph.GetHashCode()
                    : 0;
                return;
            }
            if (selectable is BaseNodeView nodeView && nodeView.Node != null)
            {
                Kind = 2;
                Identity = nodeView.Node.GUID ?? string.Empty;
                RevisionA = 0;
                RevisionB = 0;
                ReferenceId = 0;
                return;
            }
            if (selectable is StackNodeView stackNodeView && stackNodeView.StackNode != null)
            {
                Kind = 3;
                Identity = stackNodeView.StackNode.GUID ?? string.Empty;
                RevisionA = 0;
                RevisionB = 0;
                ReferenceId = 0;
                return;
            }
            if (selectable is NodeGroupView nodeGroupView && nodeGroupView.NodeGroup != null)
            {
                Kind = 4;
                Identity = string.Empty;
                RevisionA = 0;
                RevisionB = 0;
                ReferenceId = nodeGroupView.NodeGroup.GetHashCode();
                return;
            }
            Kind = 0;
            Identity = string.Empty;
            RevisionA = 0;
            RevisionB = 0;
            ReferenceId = selectable?.GetHashCode() ?? 0;
        }

        int Kind { get; }
        string Identity { get; }
        int RevisionA { get; }
        int RevisionB { get; }
        int ReferenceId { get; }

        public bool Equals(TreeSelectionIdentity other)
        {
            return Kind == other.Kind &&
                   Identity == other.Identity &&
                   RevisionA == other.RevisionA &&
                   RevisionB == other.RevisionB &&
                   ReferenceId == other.ReferenceId;
        }
    }

    internal sealed class TreeSelectionForwarder
    {
        readonly BaseTreeView m_View;
        TreeSelectionIdentity[] m_LastSelection = Array.Empty<TreeSelectionIdentity>();

        public TreeSelectionForwarder(BaseTreeView view)
        {
            m_View = view;
        }

        public void Invalidate()
        {
            m_LastSelection = Array.Empty<TreeSelectionIdentity>();
            Publish(true);
        }

        public void Tick()
        {
            Publish(false);
        }

        void Publish(bool force)
        {
            if (m_View.TreeWindow == null)
                return;
            TreeSelectionIdentity[] current = m_View.selection.Select(item => new TreeSelectionIdentity(item)).ToArray();
            if (!force && current.SequenceEqual(m_LastSelection))
                return;
            m_LastSelection = current;
            m_View.TreeWindow.PopulateSelectionInspector(m_View.selection);
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

        public BaseNode CreateNode(Type type, Vector2 position)
        {
            if (!Tree.CanCreateNodeType(type))
                return null;
            BaseNode node = null;
            Tree.ApplyModify("Create Node", () =>
            {
                Vector2 localPosition = (position - new Vector2(m_View.viewTransform.position.x, m_View.viewTransform.position.y)) / m_View.scale;
                node = Tree.CreateNode(type);
                node.Position = localPosition;
                m_View.CreateNodeView(node);
            });
            return node;
        }

        public bool DeleteNode(BaseNode node, bool confirmed)
        {
            if (!confirmed && !ConfirmConditionEdges(CollectConditionEdgesForNode(node)))
                return false;
            Tree.ApplyModify("Delete Node", () =>
            {
                DeleteOwnedConditionRuleGraphs(CollectConditionEdgesForNode(node));
                Tree.DeleteNode(node);
            });
            return true;
        }

        public NodeGroup CreateNodeGroup(Vector2 position)
        {
            NodeGroup group = null;
            Tree.ApplyModify("Create NodeGroup", () =>
            {
                Vector2 localPosition = (position - new Vector2(m_View.viewTransform.position.x, m_View.viewTransform.position.y)) / m_View.scale;
                group = Tree.CreateNodeGroup();
                group.Position = localPosition;
                m_View.CreateNodeGroupView(group);
            });
            return group;
        }

        public void DeleteNodeGroup(NodeGroup group)
        {
            Tree.ApplyModify("Delete NodeGroup", () => Tree.DeleteNodeGroup(group));
        }

        public StackNode CreateStackNode(Vector2 position)
        {
            StackNode stack = null;
            Tree.ApplyModify("Create StackNode", () =>
            {
                Vector2 localPosition = (position - new Vector2(m_View.viewTransform.position.x, m_View.viewTransform.position.y)) / m_View.scale;
                stack = Tree.CreateStackNode();
                stack.Position = localPosition;
                m_View.CreateStackNodeView(stack);
            });
            return stack;
        }

        public void DeleteStackNode(StackNode stack)
        {
            Tree.ApplyModify("Delete StackNode", () => Tree.DeleteStackNode(stack));
        }

        public BaseEdge Link(BaseNode startNode, BaseNode endNode, string outputName, string inputName)
        {
            BaseEdge edge = null;
            Tree.ApplyModify("Link Nodes", () => edge = Tree.Link(startNode, endNode, outputName, inputName));
            return edge;
        }

        public bool Unlink(BaseEdge edge, bool confirmed)
        {
            if (!confirmed && !ConfirmConditionEdges(new[] { edge }))
                return false;
            Tree.ApplyModify("UnLink Nodes", () =>
            {
                DeleteOwnedConditionRuleGraph(edge);
                Tree.UnLink(edge);
            });
            return true;
        }

        public PropertyEdge LinkProperty(
            BaseNode startNode,
            BaseNode endNode,
            PropertyPort startPort,
            PropertyPort endPort,
            bool resolveVariableTypes)
        {
            PropertyEdge edge = null;
            Tree.ApplyModify(resolveVariableTypes ? "Link VariablePropertyPorts" : "Link PropertyPorts", () =>
            {
                if (resolveVariableTypes)
                {
                    if (startPort.ValueType == null)
                        startPort = startNode.SetPropertyPort(startPort.FieldKey, endPort.GetType(), startPort.Direction);
                    if (endPort.ValueType == null)
                        endPort = endNode.SetPropertyPort(endPort.FieldKey, startPort.GetType(), endPort.Direction);
                }
                edge = Tree.LinkProperty(startNode, endNode, startPort, endPort);
            });
            return edge;
        }

        public void UnlinkProperty(PropertyEdge edge, bool variable)
        {
            Tree.ApplyModify(variable ? "UnLink VariablePropertyPorts" : "UnLink PropertyPorts", () => Tree.UnLinkProperty(edge));
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
            GraphElement[] copiedElements = elements?.ToArray() ?? Array.Empty<GraphElement>();
            CopyPasteHelper data = new CopyPasteHelper { centerPosition = Vector2.zero };
            foreach (GraphElement element in copiedElements)
            {
                if (element is BaseNodeView nodeView)
                {
                    data.copiedNodes.Add(JsonSerializer.SerializeNode(nodeView.Node));
                    data.centerPosition += nodeView.Node.Position + nodeView.GetPosition().size / 2f;
                }
                else if (element is StackNodeView stackView)
                {
                    data.copiedStacks.Add(JsonSerializer.Serialize(stackView.StackNode));
                    data.centerPosition += stackView.GetPosition().center;
                }
                else if (element is NodeGroupView groupView)
                {
                    data.copiedGroups.Add(JsonSerializer.Serialize(groupView.NodeGroup));
                    data.centerPosition += groupView.GetPosition().center;
                }
                else if (element is BaseEdgeView edgeView &&
                         copiedElements.Contains(edgeView.StartNodeView) &&
                         copiedElements.Contains(edgeView.EndNodeView))
                {
                    if (edgeView.Edge is PropertyEdge propertyEdge)
                        data.copiedPropertyEdges.Add(JsonSerializer.Serialize(propertyEdge));
                    else if (edgeView.Edge != null)
                        data.copiedEdges.Add(JsonSerializer.Serialize(edgeView.Edge));
                }
            }
            int centerCount = data.copiedNodes.Count + data.copiedGroups.Count + data.copiedStacks.Count;
            if (centerCount > 0)
                data.centerPosition /= centerCount;
            return JsonUtility.ToJson(data, true);
        }

        public bool CanPaste(string serializedData)
        {
            if (m_View.RuntimeReadOnly)
                return false;
            try
            {
                return JsonUtility.FromJson(serializedData, typeof(CopyPasteHelper)) != null;
            }
            catch
            {
                return false;
            }
        }

        public void Paste(string operationName, string serializedData)
        {
            if (Tree == null || m_View.RuntimeReadOnly)
                return;
            CopyPasteHelper data = JsonUtility.FromJson<CopyPasteHelper>(serializedData)
                ?? throw new InvalidOperationException("Tree paste payload is invalid.");
            Tree.ApplyModify(operationName, () => PasteInsideTransaction(data));
        }

        void PasteInsideTransaction(CopyPasteHelper data)
        {
            Dictionary<string, BaseNode> nodeMap = new Dictionary<string, BaseNode>();
            Dictionary<string, StackNode> stackMap = new Dictionary<string, StackNode>();
            m_View.ClearSelection();
            Vector2 distance = m_View.LocalMousePosition - data.centerPosition;
            HashSet<string> acceptablePaths = new HashSet<string>(
                Tree.GetAttributes<AcceptableNodePathsAttribute>()
                    .SelectMany(attribute => attribute.AcceptableNodePaths));
            foreach (var copiedNode in data.copiedNodes)
            {
                BaseNode node = JsonSerializer.DeserializeNode(copiedNode);
                if (node == null || node.Single && Tree.Nodes.Any(item => item.GetType() == node.GetType()))
                    continue;
                string rootPath = TreeDesignerUtility.GetNodePath(node.GetType()).Split('/')[0];
                if (!acceptablePaths.Contains(rootPath) || !Tree.CanCreateNodeType(node.GetType()))
                    continue;
                string sourceGuid = node.GUID;
                node.GUID = Guid.NewGuid().ToString();
                node.RegenerateOwnedAuthoringIdentities();
                node.Position += distance;
                node.Refresh();
                Tree.AddNode(node);
                nodeMap[sourceGuid] = node;
                m_View.AddToSelection(m_View.CreateNodeView(node));
            }
            foreach (var copiedStack in data.copiedStacks)
            {
                StackNode stack = JsonSerializer.Deserialize<StackNode>(copiedStack);
                if (stack == null)
                    continue;
                string sourceGuid = stack.GUID;
                stack.GUID = Guid.NewGuid().ToString();
                stack.Position += distance;
                List<string> sourceNodes = stack.NodeGUIDs.ToList();
                stack.NodeGUIDs.Clear();
                for (int i = 0; i < sourceNodes.Count; i++)
                {
                    if (nodeMap.TryGetValue(sourceNodes[i], out BaseNode node))
                        stack.NodeGUIDs.Add(node.GUID);
                }
                Tree.StackNodes.Add(stack);
                stackMap[sourceGuid] = stack;
                m_View.AddToSelection(m_View.CreateStackNodeView(stack));
            }
            foreach (var copiedGroup in data.copiedGroups)
            {
                NodeGroup group = JsonSerializer.Deserialize<NodeGroup>(copiedGroup);
                if (group == null)
                    continue;
                group.Position += distance;
                RemapGuids(group.NodeGUIDs, nodeMap.ToDictionary(pair => pair.Key, pair => pair.Value.GUID));
                RemapGuids(group.StackGUIDs, stackMap.ToDictionary(pair => pair.Key, pair => pair.Value.GUID));
                Tree.NodeGroups.Add(group);
                m_View.AddToSelection(m_View.CreateNodeGroupView(group));
            }
            foreach (var copiedEdge in data.copiedEdges)
            {
                BaseEdge source = JsonSerializer.Deserialize<BaseEdge>(copiedEdge);
                if (source != null &&
                    nodeMap.TryGetValue(source.StartNodeGUID, out BaseNode start) &&
                    nodeMap.TryGetValue(source.EndNodeGUID, out BaseNode end))
                {
                    m_View.CreateEdgeView(Tree.Link(start, end, source.StartPortName, source.EndPortName));
                }
            }
            foreach (var copiedPropertyEdge in data.copiedPropertyEdges)
            {
                PropertyEdge source = JsonSerializer.Deserialize<PropertyEdge>(copiedPropertyEdge);
                if (source == null ||
                    !nodeMap.TryGetValue(source.StartNodeGUID, out BaseNode start) ||
                    !nodeMap.TryGetValue(source.EndNodeGUID, out BaseNode end) ||
                    !start.PropertyPortMap.TryGetValue(source.StartPortName, out PropertyPort startPort) ||
                    !end.PropertyPortMap.TryGetValue(source.EndPortName, out PropertyPort endPort))
                    continue;
                m_View.CreatePropertyEdgeView(Tree.LinkProperty(start, end, startPort, endPort));
            }
            m_View.NodeViews.ForEach(view => view.RefreshNodeExpandedState());
        }

        static void RemapGuids(List<string> guids, IReadOnlyDictionary<string, string> map)
        {
            string[] source = guids.ToArray();
            guids.Clear();
            for (int i = 0; i < source.Length; i++)
            {
                if (map.TryGetValue(source[i], out string guid))
                    guids.Add(guid);
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
            Tree.ApplyModify("Delete Graph Elements", () =>
            {
                DeleteOwnedConditionRuleGraphs(CollectConditionEdgesToRemove(elements));
                foreach (BaseEdgeView edgeView in edgeViews)
                {
                    if (edgeView.Edge == null || deletedNodes.Contains(edgeView.Edge.StartNode) || deletedNodes.Contains(edgeView.Edge.EndNode))
                        continue;
                    if (edgeView.Edge is PropertyEdge propertyEdge)
                        Tree.UnLinkProperty(propertyEdge);
                    else if (Tree.Edges.Contains(edgeView.Edge))
                        Tree.UnLink(edgeView.Edge);
                }
                foreach (BaseNodeView nodeView in nodeViews)
                    Tree.DeleteNode(nodeView.Node);
                foreach (NodeGroupView groupView in groupViews)
                    Tree.DeleteNodeGroup(groupView.NodeGroup);
                foreach (StackNodeView stackView in stackViews)
                    Tree.DeleteStackNode(stackView.StackNode);
            });
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

        void ForwardMovedElements(IEnumerable<GraphElement> elements)
        {
            if (elements == null)
                return;
            foreach (GraphElement element in elements)
            {
                if (element is BaseNodeView nodeView)
                    nodeView.OnMoved(nodeView.GetPosition().position);
                else if (element is NodeGroupView groupView)
                    groupView.OnMoved(groupView.GetPosition().position);
                else if (element is StackNodeView stackView)
                    stackView.OnMoved(stackView.GetPosition().position);
            }
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

    public class BaseTreeView : GraphView
    {
        public new class UxmlFactory : UxmlFactory<BaseTreeView, UxmlTraits> { }

        protected BaseTree m_Tree;
        public BaseTree Tree => m_Tree;

        protected BaseTreeWindow m_TreeWindow;
        public BaseTreeWindow TreeWindow => m_TreeWindow;

        protected NodeSearchWindow m_NodeSearchWindow;
        public NodeSearchWindow NodeSearchWindow => m_NodeSearchWindow;


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
        protected IVisualElementScheduledItem m_SelectionWatcher;
        TreeSelectionForwarder m_SelectionForwarder;
        TreeGraphMutationService m_MutationService;
        readonly Dictionary<GraphElement, Capabilities> m_ReadOnlyCapabilities = new Dictionary<GraphElement, Capabilities>();
        bool m_RuntimeReadOnly;
        public bool RuntimeReadOnly => m_RuntimeReadOnly;

        public string TargetTypeStr;

        public BaseTreeView()
        {
            StyleSheet styleSheet = Resources.Load<StyleSheet>("StyleSheet/BaseTree");
            styleSheets.Add(styleSheet);
            Insert(0, new GridBackground());

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

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new TreeRectangleSelector());

            RegisterCallback<KeyDownEvent>(KeyDownCallback);
            RegisterCallback<MouseMoveEvent>(MouseMoveCallback);

            m_DropArea = new DropArea();
            m_DropArea.Init(this);
            m_DropArea.DragValid = () => !m_RuntimeReadOnly && InputActionNodeDragFactory.CanCreateFromDrag(this);
            m_DropArea.onDragPerformEvent += e =>
            {
                if (!m_RuntimeReadOnly)
                    InputActionNodeDragFactory.CreateFromDrag(this, e);
            };
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (m_Tree == null) return;
            if (m_RuntimeReadOnly)
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
                            selectedNodeView.OnMoved(targetPosition);
                        }
                        else if (selectedElement is StackNodeView selectedStackNodeView)
                        {
                            Vector2 originalPosition = selectedStackNodeView.StackNode.Position;
                            Vector2 targetPosition = new Vector2(selectedStackNodeView.StackNode.Position.x, targetNodeView.Node.Position.y + (targetNodeView.StackNodeView != null ? 4 : 0));
                            offset = targetPosition - originalPosition;
                            selectedStackNodeView.SetPosition(new Rect(targetPosition, Vector2.zero));
                            selectedStackNodeView.OnMoved(targetPosition);
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
                            selectedNodeView.OnMoved(targetPosition);
                        }
                        else if (selectedElement is StackNodeView selectedStackNodeView)
                        {
                            Vector2 originalPosition = selectedStackNodeView.StackNode.Position;
                            Vector2 targetPosition = new Vector2(selectedStackNodeView.StackNode.Position.x, targetNodeView.Node.Position.y + targetNodeView.layout.height - selectedElement.layout.height + (targetNodeView.StackNodeView != null ? 4 : 0));
                            offset = targetPosition - originalPosition;
                            selectedStackNodeView.SetPosition(new Rect(targetPosition, Vector2.zero));
                            selectedStackNodeView.OnMoved(targetPosition);
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
                            selectedNodeView.OnMoved(targetPosition);
                        }
                        else if (selectedElement is StackNodeView selectedStackNodeView)
                        {
                            Vector2 originalPosition = selectedStackNodeView.StackNode.Position;
                            Vector2 targetPosition = new Vector2(targetNodeView.Node.Position.x + targetNodeView.layout.width + (targetNodeView.StackNodeView != null ? 14 : 12), selectedStackNodeView.StackNode.Position.y);
                            offset = targetPosition - originalPosition;
                            selectedStackNodeView.SetPosition(new Rect(targetPosition, Vector2.zero));
                            selectedStackNodeView.OnMoved(targetPosition);
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
                            nodeView.OnMoved(targetPosition);
                        }
                        else if (selectable is StackNodeView stackNodeView)
                        {
                            Vector2 targetPosition = stackNodeView.StackNode.Position + offset;
                            stackNodeView.SetPosition(new Rect(targetPosition, Vector2.zero));
                            stackNodeView.OnMoved(targetPosition);
                        }
                    }
                }
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            if (m_RuntimeReadOnly)
                return new List<Port>();
            List<Port> compatiblePorts = ports.ToList().Where(endPort =>
            {
                BasePortView startPortView = startPort as BasePortView;
                BasePortView endPortView = endPort as BasePortView;

                if (startPortView.NodeView == endPortView.NodeView)
                    return false;

                if (endPortView.direction == startPortView.direction)
                    return false;

                if (startPortView.portType == null || endPortView.portType == null)
                    return false;

                if (startPortView.portType == typeof(object))
                    return false;

                if (!IsCompatibleStateMachineFlowPort(startPortView, endPortView))
                    return false;

                if (endPortView is VariablePropertyPortView endVariablePropertyPortView && endVariablePropertyPortView.PropertyPort.ValueType == null)
                {
                    bool compatible = false;
                    foreach (var type in endVariablePropertyPortView.AcceptableTypes)
                    {
                        if (startPortView.portType.IsSubClassOfRawGeneric(type))
                        {
                            compatible = true;
                            break;
                        }
                    }
                    return compatible;
                }

                if (startPortView.portType == endPortView.portType)
                    return true;

                if (startPortView.portType.IsSubclassOf(endPortView.portType))
                    return true;

                if (endPortView is PropertyPortView propertyPortView
                   && propertyPortView.PropertyPort.GetAttribute<CompatiblePortsAttribute>() is CompatiblePortsAttribute compatiblePortsAttribute
                   && compatiblePortsAttribute.CompatibleTypes.Contains(startPortView.portType))
                    return true;

                return false;

            }).ToList();

            return compatiblePorts;
        }

        bool IsCompatibleStateMachineFlowPort(BasePortView startPortView, BasePortView endPortView)
        {
            if (!(m_Tree is StateMachineGraph))
                return true;

            if (IsPropertyPort(startPortView) || IsPropertyPort(endPortView))
                return true;

            BasePortView outputPortView = startPortView.direction == Direction.Output ? startPortView : endPortView;
            BasePortView inputPortView = startPortView.direction == Direction.Input ? startPortView : endPortView;
            BaseNode startNode = outputPortView.NodeView?.Node;
            BaseNode endNode = inputPortView.NodeView?.Node;

            return IsCompatibleStateTransition(outputPortView, inputPortView, startNode, endNode);
        }

        static bool IsCompatibleStateTransition(BasePortView outputPortView, BasePortView inputPortView, BaseNode startNode, BaseNode endNode)
        {
            if (outputPortView.Name != StateMachinePorts.StateOut || inputPortView.Name != StateMachinePorts.StateIn)
                return false;

            return startNode is StateMachineEnterNode && endNode is StateNode ||
                   startNode is StateMachineAnyStateNode && (endNode is StateNode || endNode is StateMachineExitNode) ||
                   startNode is StateNode && (endNode is StateNode || endNode is StateMachineExitNode);
        }

        static bool IsPropertyPort(BasePortView portView)
        {
            return portView is PropertyPortView || portView is VariablePropertyPortView;
        }


        public bool Empty { get; private set; } = true;
        public virtual void Init(BaseTreeWindow treeWindow)
        {
            m_TreeWindow = treeWindow;
            m_MutationService = new TreeGraphMutationService(this);
            m_SelectionForwarder = new TreeSelectionForwarder(this);
            m_NodeSearchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
            m_NodeSearchWindow.Init(treeWindow, this);

            nodeCreationRequest = context =>
            {
                if (m_Tree != null && !m_RuntimeReadOnly)
                    SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), m_NodeSearchWindow);
            };

            serializeGraphElements = m_MutationService.Serialize;
            canPasteSerializedData = m_MutationService.CanPaste;
            unserializeAndPaste = m_MutationService.Paste;
            m_SelectionWatcher = schedule.Execute(m_SelectionForwarder.Tick).Every(100);
        }
        public virtual void PopulateView(BaseTree tree)
        {
            bool restoreReadOnly = m_RuntimeReadOnly;
            if (restoreReadOnly)
            {
                m_RuntimeReadOnly = false;
                m_ReadOnlyCapabilities.Clear();
            }
            ClearView();
            Empty = false;
            m_Tree = tree;
            m_Tree.Nodes.ForEach(i => CreateNodeView(i));
            m_Tree.Edges.ForEach(i => CreateEdgeView(i));
            m_Tree.PropertyEdges.ForEach(i => CreatePropertyEdgeView(i));
            m_Tree.StackNodes.ForEach(i => CreateStackNodeView(i));
            m_Tree.NodeGroups.ForEach(i => CreateNodeGroupView(i));
            m_NodeViews.ForEach(i => i.RefreshNodeExpandedState());
            m_SelectionForwarder.Invalidate();

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
            m_SelectionForwarder?.Invalidate();
        }

        public virtual BaseNode CreateNode(Type type, Vector2 position)
        {
            return m_MutationService.CreateNode(type, position);
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

            AddElement(nodeView);
            m_NodeViews.Add(nodeView);
            return nodeView;
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
                startNodeView.OnOutputPropertyPortConnected(startPropertyPortView);
                endNodeView.OnInputPropertyPortConnected(endPropertyPortView);
                AddElement(propertyEdgeView);
                return propertyEdgeView;
            }
            else
                return null;

        }

        protected virtual GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            return m_MutationService.ApplyGraphViewChange(graphViewChange);
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

        public void SetRuntimeReadOnly(bool readOnly)
        {
            if (m_RuntimeReadOnly == readOnly)
                return;
            m_RuntimeReadOnly = readOnly;
            if (readOnly)
                m_ReadOnlyCapabilities.Clear();
            foreach (GraphElement element in graphElements.ToList())
            {
                if (element is BaseNodeView nodeView)
                {
                    nodeView.SetRuntimeReadOnly(readOnly);
                    continue;
                }
                if (element is BaseEdgeView edgeView)
                {
                    edgeView.SetRuntimeReadOnly(readOnly);
                    continue;
                }
                if (readOnly)
                {
                    m_ReadOnlyCapabilities[element] = element.capabilities;
                    element.capabilities &= Capabilities.Selectable | Capabilities.Ascendable;
                }
            }
            if (!readOnly)
            {
                foreach (KeyValuePair<GraphElement, Capabilities> pair in m_ReadOnlyCapabilities)
                    pair.Key.capabilities = pair.Value;
                m_ReadOnlyCapabilities.Clear();
            }
        }
        void MouseMoveCallback(MouseMoveEvent e)
        {
            var windowMousePosition = m_TreeWindow.rootVisualElement.ChangeCoordinatesTo(m_TreeWindow.rootVisualElement, e.originalMousePosition);
            m_LocalMousePosition = contentViewContainer.WorldToLocal(windowMousePosition);
        }
    }
}
