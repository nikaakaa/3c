using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using Taco;
using Taco.Editor;

namespace TreeDesigner.Editor
{
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
                    if (!m_Tree || targetType == null)
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
            this.AddManipulator(new UnityEditor.Experimental.GraphView.RectangleSelector());

            RegisterCallback<KeyDownEvent>(KeyDownCallback);
            RegisterCallback<MouseMoveEvent>(MouseMoveCallback);

            m_DropArea = new DropArea();
            m_DropArea.Init(this);
            m_DropArea.DragValid = () => InputActionNodeDragFactory.CanCreateFromDrag(this);
            m_DropArea.onDragPerformEvent += e => InputActionNodeDragFactory.CreateFromDrag(this, e);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (!m_Tree) return;
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
            m_NodeSearchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
            m_NodeSearchWindow.Init(treeWindow, this);

            nodeCreationRequest = context =>
            {
                if (m_Tree != null)
                    SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), m_NodeSearchWindow);
            };

            serializeGraphElements = SerializeGraphElementsCallback;
            canPasteSerializedData = CanPasteSerializedDataCallback;
            unserializeAndPaste = UnserializeAndPasteCallback;
        }
        public virtual void PopulateView(BaseTree tree)
        {
            ClearView();
            Empty = false;
            m_Tree = tree;
            m_Tree.Nodes.ForEach(i => CreateNodeView(i));
            m_Tree.Edges.ForEach(i => CreateEdgeView(i));
            m_Tree.PropertyEdges.ForEach(i => CreatePropertyEdgeView(i));
            m_Tree.StackNodes.ForEach(i => CreateStackNodeView(i));
            m_Tree.NodeGroups.ForEach(i => CreateNodeGroupView(i));
            m_NodeViews.ForEach(i => i.RefreshNodeExpandedState());

            foreach (var propertyField in this.Query<PropertyField>().ToList())
            {
                propertyField.Unbind();
                propertyField.Bind(m_Tree.GetSerializedTree());
            }

            graphViewChanged += OnGraphViewChanged;
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

        public virtual BaseNode CreateNode(Type type, Vector2 position)
        {
            if (!m_Tree.CanCreateNodeType(type))
                return null;

            BaseNode node = null;
            m_Tree.ApplyModify("Create Node", () =>
            {
                Vector2 localPosition = (position - new Vector2(viewTransform.position.x, viewTransform.position.y)) / scale;
                node = m_Tree.CreateNode(type);
                node.Position = localPosition;
                CreateNodeView(node);
            });
            return node;
        }
        public virtual void DeleteNode(BaseNode node)
        {
            m_Tree.ApplyModify("Delete Node", () =>
            {
                m_Tree.DeleteNode(node);
            });
        }
        public virtual NodeGroup CreateNodeGroup(Vector2 position)
        {
            NodeGroup nodeGroup = null;
            m_Tree.ApplyModify("Create NodeGroup", () =>
            {
                Vector2 localPosition = (position - new Vector2(viewTransform.position.x, viewTransform.position.y)) / scale;
                nodeGroup = m_Tree.CreateNodeGroup();
                nodeGroup.Position = localPosition;
                CreateNodeGroupView(nodeGroup);
            });
            return nodeGroup;
        }
        public virtual void DeleteNodeGroup(NodeGroup nodeGroup)
        {
            m_Tree.ApplyModify("Delete NodeGroup", () =>
            {
                m_Tree.DeleteNodeGroup(nodeGroup);
            });
        }
        public virtual StackNode CreateStackNode(Vector2 position)
        {
            StackNode stackNode = null;
            m_Tree.ApplyModify("Create StackNode", () =>
            {
                Vector2 localPosition = (position - new Vector2(viewTransform.position.x, viewTransform.position.y)) / scale;
                stackNode = m_Tree.CreateStackNode();
                stackNode.Position = localPosition;
                CreateStackNodeView(stackNode);
            });
            return stackNode;
        }
        public virtual void DeleteStackNode(StackNode stackNode)
        {
            m_Tree.ApplyModify("Delete StackNode", () =>
            {
                m_Tree.DeleteStackNode(stackNode);
            });
        }
        public virtual BaseEdge Link(BaseNode startNode, BaseNode endNode, string outputName, string inputName)
        {
            BaseEdge edge = null;
            m_Tree.ApplyModify("Link Nodes", () =>
            {
                edge = m_Tree.Link(startNode, endNode, outputName, inputName);
            });
            return edge;
        }
        public virtual void UnLink(BaseEdge edge)
        {
            m_Tree.ApplyModify("UnLink Nodes", () =>
            {
                m_Tree.UnLink(edge);
            });
        }
        public virtual PropertyEdge LinkProperty(BaseNode startNode, BaseNode endNode, PropertyPort startPropertyPort, PropertyPort endPropertyPort)
        {
            PropertyEdge propertyEdge = null;
            m_Tree.ApplyModify("Link PropertyPorts", () =>
            {
                propertyEdge = m_Tree.LinkProperty(startNode, endNode, startPropertyPort, endPropertyPort);
            });
            return propertyEdge;
        }
        public virtual void UnLinkProperty(PropertyEdge propertyEdge)
        {
            m_Tree.ApplyModify("UnLink PropertyPorts", () =>
            {
                m_Tree.UnLinkProperty(propertyEdge);
            });
        }
        public virtual PropertyEdge LinkVariableProperty(BaseNode startNode, BaseNode endNode, PropertyPort startPropertyPort, PropertyPort endPropertyPort)
        {
            PropertyEdge propertyEdge = null;
            m_Tree.ApplyModify("Link VariablePropertyPorts", (Action)(() =>
            {
                if (startPropertyPort.ValueType == null)
                    startPropertyPort = startNode.SetPropertyPort(startPropertyPort.FieldKey, endPropertyPort.GetType(), startPropertyPort.Direction);
                if (endPropertyPort.ValueType == null)
                    endPropertyPort = endNode.SetPropertyPort(endPropertyPort.FieldKey, startPropertyPort.GetType(), endPropertyPort.Direction);

                propertyEdge = m_Tree.LinkProperty(startNode, endNode, startPropertyPort, endPropertyPort);
            }));
            return propertyEdge;
        }
        public virtual void UnLinkVariableProperty(PropertyEdge propertyEdge)
        {
            m_Tree.ApplyModify("UnLink VariablePropertyPorts", () =>
            {
                m_Tree.UnLinkProperty(propertyEdge);
            });
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
            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    if (edge is PropertyEdgeView propertyEdgeView)
                    {
                        if (propertyEdgeView.StartPropertyPortView is VariablePropertyPortView || propertyEdgeView.EndPropertyPortView is VariablePropertyPortView)
                            propertyEdgeView.Edge = LinkVariableProperty(propertyEdgeView.StartNodeView.Node, propertyEdgeView.EndNodeView.Node, propertyEdgeView.StartPropertyPortView.PropertyPort, propertyEdgeView.EndPropertyPortView.PropertyPort);
                        else
                            propertyEdgeView.Edge = LinkProperty(propertyEdgeView.StartNodeView.Node, propertyEdgeView.EndNodeView.Node, propertyEdgeView.StartPropertyPortView.PropertyPort, propertyEdgeView.EndPropertyPortView.PropertyPort);

                        propertyEdgeView.StartNodeView.OnOutputPropertyPortConnected(propertyEdgeView.StartPropertyPortView);
                        propertyEdgeView.EndNodeView.OnInputPropertyPortConnected(propertyEdgeView.EndPropertyPortView);
                    }
                    else if (edge is BaseEdgeView edgeView)
                    {
                        edgeView.Edge = Link(edgeView.StartNodeView.Node, edgeView.EndNodeView.Node, edgeView.StartPortView.Name, edgeView.EndPortView.Name);

                        edgeView.StartNodeView.OnOutputPortConnected(edgeView.StartPortView);
                        edgeView.EndNodeView.OnInputPortConnected(edgeView.EndPortView);
                    }
                }
            }
            if (graphViewChange.elementsToRemove != null)
            {
                bool nodeChanged = false;
                foreach (var element in graphViewChange.elementsToRemove)
                {
                    if (element is BaseNodeView nodeView)
                    {
                        nodeChanged = true;
                        DeleteNode(nodeView.Node);
                        m_NodeViews.Remove(nodeView);
                    }
                    if (element is NodeGroupView nodeGroupView)
                    {
                        DeleteNodeGroup(nodeGroupView.NodeGroup);
                        m_NodeGroupViews.Remove(nodeGroupView);
                    }
                    if (element is StackNodeView stackNodeView)
                    {
                        DeleteStackNode(stackNodeView.StackNode);
                        m_StackNodeViews.Remove(stackNodeView);
                    }
                    if (element is PropertyEdgeView propertyEdgeView)
                    {
                        if (propertyEdgeView.StartPropertyPortView is VariablePropertyPortView || propertyEdgeView.EndPropertyPortView is VariablePropertyPortView)
                            UnLinkVariableProperty(propertyEdgeView.PropertyEdge);
                        else
                            UnLinkProperty(propertyEdgeView.PropertyEdge);

                        propertyEdgeView.StartNodeView.OnOutputPropertyPortDisconnected(propertyEdgeView.StartPropertyPortView);
                        propertyEdgeView.EndNodeView.OnInputPropertyPortDisconnected(propertyEdgeView.EndPropertyPortView);
                    }
                    else if (element is BaseEdgeView edgeView)
                    {
                        UnLink(edgeView.Edge);
                        edgeView.StartNodeView.OnOutputPortDisconnected(edgeView.StartPortView);
                        edgeView.EndNodeView.OnInputPortDisconnected(edgeView.EndPortView);
                    }
                }

                m_Tree.GetNewSerializedTree();
                if (nodeChanged)
                    m_NodeViews.ForEach(i => i.SyncSerializedPropertyPathes());
            }
            if (graphViewChange.movedElements != null)
            {
                foreach (var element in graphViewChange.movedElements)
                {
                    if (element is BaseNodeView nodeView)
                    {
                        nodeView.OnMoved(nodeView.GetPosition().position);
                    }
                    else if (element is NodeGroupView nodeGroupView)
                    {
                        nodeGroupView.OnMoved(nodeGroupView.GetPosition().position);
                    }
                    else if (element is StackNodeView stackNodeView)
                    {
                        stackNodeView.OnMoved(stackNodeView.GetPosition().position);
                    }
                }
            }
            return graphViewChange;
        }
        protected virtual string SerializeGraphElementsCallback(IEnumerable<GraphElement> elements)
        {
            var data = new CopyPasteHelper();
            data.centerPosition = Vector2.zero;

            foreach (var element in elements)
            {
                if (element is BaseNodeView nodeView)
                {
                    data.copiedNodes.Add(JsonSerializer.SerializeNode(nodeView.Node));
                    data.centerPosition += nodeView.Node.Position + nodeView.GetPosition().size / 2;
                }
                if (element is StackNodeView stackNodeView)
                {
                    data.copiedStacks.Add(JsonSerializer.Serialize(stackNodeView.StackNode));
                    data.centerPosition += stackNodeView.GetPosition().center;
                }
                if (element is NodeGroupView nodeGroupView)
                {
                    data.copiedGroups.Add(JsonSerializer.Serialize(nodeGroupView.NodeGroup));
                    data.centerPosition += nodeGroupView.GetPosition().center;
                }
                if (element is BaseEdgeView edgeView && elements.Contains(edgeView.StartNodeView) && elements.Contains(edgeView.EndNodeView))
                {
                    if (edgeView.Edge is PropertyEdge propertyEdge)
                        data.copiedPropertyEdges.Add(JsonSerializer.Serialize(propertyEdge));
                    else if (edgeView.Edge is BaseEdge edge)
                        data.copiedEdges.Add(JsonSerializer.Serialize(edge));
                }
            }
            data.centerPosition /= (data.copiedNodes.Count + data.copiedGroups.Count + data.copiedStacks.Count);
            return JsonUtility.ToJson(data, true);
        }
        protected virtual bool CanPasteSerializedDataCallback(string serializedData)
        {
            try
            {
                return JsonUtility.FromJson(serializedData, typeof(CopyPasteHelper)) != null;
            }
            catch
            {
                return false;
            }
        }
        protected virtual void UnserializeAndPasteCallback(string operationName, string serializedData)
        {
            if (!m_Tree) return;

            m_Tree.ApplyModify(operationName, () =>
            {
                var data = JsonUtility.FromJson<CopyPasteHelper>(serializedData);
                Dictionary<string, BaseNode> sourceGUIDNewNodeMap = new Dictionary<string, BaseNode>();
                Dictionary<string, StackNode> sourceGUIDNewStackMap = new Dictionary<string, StackNode>();
                ClearSelection();

                Vector2 distance = m_LocalMousePosition - data.centerPosition;
                List<string> acceptableNodePaths = new List<string>();
                var acceptableNodePathsAttributes = m_TreeWindow.Tree.GetAttributes<AcceptableNodePathsAttribute>();
                foreach (var acceptableNodePathsAttribute in acceptableNodePathsAttributes)
                {
                    foreach (var acceptableNodePath in acceptableNodePathsAttribute.AcceptableNodePaths)
                    {
                        if (!acceptableNodePaths.Contains(acceptableNodePath))
                            acceptableNodePaths.Add(acceptableNodePath);
                    }
                }

                foreach (var copiedNode in data.copiedNodes)
                {
                    var newNode = JsonSerializer.DeserializeNode(copiedNode);
                    if (newNode == null)
                        continue;
                    if (newNode.Single && m_Tree.Nodes.Find(i => i.GetType() == newNode.GetType()))
                        continue;

                    string nodePath = TreeDesignerUtility.GetNodePath(newNode.GetType());
                    string startPath = nodePath.Split('/')[0];

                    if (acceptableNodePaths.Contains(startPath) && m_Tree.CanCreateNodeType(newNode.GetType()))
                    {
                        sourceGUIDNewNodeMap[newNode.GUID] = newNode;
                        newNode.GUID = Guid.NewGuid().ToString();
                        newNode.Position += distance;
                        newNode.Refresh();
                        m_Tree.AddNode(newNode);

                        BaseNodeView newNodeView = CreateNodeView(newNode);
                        AddToSelection(newNodeView);
                    }
                }
                foreach (var copiedStack in data.copiedStacks)
                {
                    var newStackNode = JsonSerializer.Deserialize<StackNode>(copiedStack);
                    if (newStackNode == null)
                        continue;

                    sourceGUIDNewStackMap[newStackNode.GUID] = newStackNode;
                    newStackNode.GUID = Guid.NewGuid().ToString();
                    newStackNode.Position += distance;
                    m_Tree.StackNodes.Add(newStackNode);

                    List<string> sourceNodeGUIDs = newStackNode.NodeGUIDs.ToList();
                    newStackNode.NodeGUIDs.Clear();
                    sourceNodeGUIDs.ForEach(i =>
                    {
                        if (sourceGUIDNewNodeMap.TryGetValue(i, out BaseNode newNode))
                            newStackNode.NodeGUIDs.Add(newNode.GUID);
                    });

                    StackNodeView stackNodeView = CreateStackNodeView(newStackNode);
                    AddToSelection(stackNodeView);
                }
                foreach (var copiedGroup in data.copiedGroups)
                {
                    var newNodeGroup = JsonSerializer.Deserialize<NodeGroup>(copiedGroup);
                    if (newNodeGroup == null)
                        continue;

                    newNodeGroup.Position += distance;
                    m_Tree.NodeGroups.Add(newNodeGroup);

                    List<string> sourceNodeGUIDs = newNodeGroup.NodeGUIDs.ToList();
                    newNodeGroup.NodeGUIDs.Clear();
                    sourceNodeGUIDs.ForEach(i =>
                    {
                        if (sourceGUIDNewNodeMap.TryGetValue(i, out BaseNode newNode))
                            newNodeGroup.NodeGUIDs.Add(newNode.GUID);
                    });

                    List<string> sourceStackGUIDs = newNodeGroup.StackGUIDs.ToList();
                    newNodeGroup.StackGUIDs.Clear();
                    sourceStackGUIDs.ForEach(i =>
                    {
                        if (sourceGUIDNewStackMap.TryGetValue(i, out StackNode newStackNode))
                            newNodeGroup.StackGUIDs.Add(newStackNode.GUID);
                    });

                    NodeGroupView nodeGroupView = CreateNodeGroupView(newNodeGroup);
                    AddToSelection(nodeGroupView);
                }
                foreach (var copiedEdge in data.copiedEdges)
                {
                    var edge = JsonSerializer.Deserialize<BaseEdge>(copiedEdge);
                    sourceGUIDNewNodeMap.TryGetValue(edge.EndNodeGUID, out var newEndNode);
                    sourceGUIDNewNodeMap.TryGetValue(edge.StartNodeGUID, out var newStartNode);

                    if (newStartNode && newEndNode)
                        CreateEdgeView(Link(newStartNode, newEndNode, edge.StartPortName, edge.EndPortName));
                }
                foreach (var copiedPropertyEdge in data.copiedPropertyEdges)
                {
                    var propertyEdge = JsonSerializer.Deserialize<PropertyEdge>(copiedPropertyEdge);
                    sourceGUIDNewNodeMap.TryGetValue(propertyEdge.EndNodeGUID, out var newEndNode);
                    sourceGUIDNewNodeMap.TryGetValue(propertyEdge.StartNodeGUID, out var newStartNode);

                    if (newStartNode && newEndNode)
                    {
                        newStartNode.PropertyPortMap.TryGetValue(propertyEdge.StartPortName, out var newStartPropertyPort);
                        newEndNode.PropertyPortMap.TryGetValue(propertyEdge.EndPortName, out var newEndPropertyPort);

                        if (newStartPropertyPort != null && newEndPropertyPort != null)
                            CreatePropertyEdgeView(LinkProperty(newStartNode, newEndNode, newStartPropertyPort, newEndPropertyPort));
                    }
                }
                m_NodeViews.ForEach(i => i.RefreshNodeExpandedState());
            });
        }

        void KeyDownCallback(KeyDownEvent e)
        {
            if (!m_Tree)
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
