using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using BTSMTL;

namespace TreeDesigner.Editor
{
    public abstract class GraphAuthoringNodeViewBase :
        Node,
        IGraphAuthoringReadOnlyView
    {
        Capabilities m_AuthoringCapabilities;
        bool m_RuntimeReadOnly;
        Image m_AuthoringIcon;
        Label m_AuthoringStatus;

        protected GraphAuthoringNodeViewBase()
        {
        }

        protected GraphAuthoringNodeViewBase(string visualTreePath)
            : base(visualTreePath)
        {
        }

        protected bool RuntimeReadOnly => m_RuntimeReadOnly;

        protected void BindAuthoringPresentation(
            string elementId,
            string displayName,
            Vector2 position,
            Color? titleColor = null)
        {
            if (string.IsNullOrWhiteSpace(elementId))
            {
                throw new ArgumentException(
                    "Graph authoring element identity is missing.",
                    nameof(elementId));
            }
            viewDataKey = elementId;
            title = displayName ?? string.Empty;
            Label titleLabel = this.Q<Label>("title-label");
            if (titleLabel != null)
                titleLabel.text = displayName ?? string.Empty;
            style.left = position.x;
            style.top = position.y;
            if (titleColor.HasValue)
            {
                titleContainer.style.backgroundColor = titleColor.Value;
                VisualElement titleElement = this.Q("title");
                if (titleElement != null)
                    titleElement.style.backgroundColor = titleColor.Value;
            }
        }

        protected void BindAuthoringDescriptor(
            GraphAuthoringCapabilityDescriptor capability,
            string displayName,
            string status,
            bool applyDescriptorColor = true)
        {
            if (capability == null)
                throw new ArgumentNullException(nameof(capability));
            title = string.IsNullOrWhiteSpace(displayName)
                ? capability.DisplayName
                : displayName;
            Label titleLabel = this.Q<Label>("title-label");
            if (titleLabel != null)
                titleLabel.text = title;
            tooltip = capability.CapabilityId.Value;
            if (applyDescriptorColor)
            {
                titleContainer.style.backgroundColor =
                    capability.Color;
                VisualElement titleElement = this.Q("title");
                if (titleElement != null)
                    titleElement.style.backgroundColor = capability.Color;
            }

            m_AuthoringIcon?.RemoveFromHierarchy();
            m_AuthoringIcon = null;
            if (!string.IsNullOrWhiteSpace(capability.IconName))
            {
                GUIContent content =
                    EditorGUIUtility.IconContent(
                        capability.IconName);
                if (content?.image != null)
                {
                    m_AuthoringIcon = new Image
                    {
                        image = content.image,
                        tooltip = capability.DisplayName
                    };
                    m_AuthoringIcon.AddToClassList(
                        "graph-authoring-node-icon");
                    titleContainer.Insert(
                        0,
                        m_AuthoringIcon);
                }
            }

            m_AuthoringStatus?.RemoveFromHierarchy();
            m_AuthoringStatus = null;
            if (string.IsNullOrWhiteSpace(status))
                return;
            m_AuthoringStatus = new Label(status);
            m_AuthoringStatus.AddToClassList(
                "graph-authoring-node-status");
            extensionContainer.Add(m_AuthoringStatus);
        }

        public virtual void SetRuntimeReadOnly(bool readOnly)
        {
            if (m_RuntimeReadOnly == readOnly)
                return;
            m_RuntimeReadOnly = readOnly;
            if (readOnly)
            {
                m_AuthoringCapabilities = capabilities;
                capabilities &=
                    Capabilities.Selectable |
                    Capabilities.Ascendable;
            }
            else
            {
                capabilities = m_AuthoringCapabilities;
            }
            OnRuntimeReadOnlyChanged(readOnly);
        }

        protected virtual void OnRuntimeReadOnlyChanged(
            bool readOnly)
        {
        }
    }

    public class BaseNodeView :
        GraphAuthoringNodeViewBase,
        IGroupable,
        IGraphAuthoringReadOnlyView
    {
        public const string DefaultVisualTreeGUID = "5eec7eeaaa8d8374181513f90c706047";
        public const string StyleSheetGUID = "f24502238ee8ac5478af96e8894528ee";
        static Regex s_ReplaceNodeIndexPropertyPath = new Regex(@"(^m_Nodes.Array.data\[)(\d+)(\])");

        protected BaseNode m_Node;
        public BaseNode Node => m_Node;
        public GraphAuthoringNodeProjection AuthoringProjection
        {
            get;
            private set;
        }
        public GraphAuthoringCapabilityDescriptor
            AuthoringCapability
        {
            get;
            private set;
        }

        protected BaseTreeWindow m_TreeWindow;
        public BaseTreeWindow TreeWindow => m_TreeWindow;
        protected VisualElement m_NodeBorder;
        protected VisualElement m_SelectionBorder;
        protected VisualElement m_Top;
        protected NodePortContainerView m_InputPortContainer;
        protected NodePortContainerView m_OutputPortContainer;
        protected NodePanelView m_NodePanel;
        protected NodeInputFieldContainerView m_NodeInputFieldContainer;
        public NodeInputFieldContainerView NodeInputFieldContainer => m_NodeInputFieldContainer;

        protected NodeGroupView m_NodeGroupView;
        public NodeGroupView NodeGroupView { get => m_NodeGroupView; set => m_NodeGroupView = value; }

        protected StackNodeView m_StackNodeView;
        public StackNodeView StackNodeView
        {
            get => m_StackNodeView;
            set
            {
                m_StackNodeView = value;

                RemoveFromClassList("stacked");
                if (m_StackNodeView != null)
                    AddToClassList("stacked");
            }
        }

        public Dictionary<string, BasePortView> InputPorts => m_InputPortContainer.PortViewMap;
        public Dictionary<string, BasePortView> OutputPorts => m_OutputPortContainer.PortViewMap;
        public Dictionary<string, PropertyPortView> InputPropertyPorts => m_InputPortContainer.PropertyPortViewMap;
        public Dictionary<string, PropertyPortView> OutputPropertyPorts => m_OutputPortContainer.PropertyPortViewMap;

        public BaseTreeView TreeView => m_TreeWindow.TreeView;

        public BaseNodeView(BaseNode node, BaseTreeWindow treeWindow) : this(node, treeWindow, AssetDatabase.GUIDToAssetPath(DefaultVisualTreeGUID))
        {
        }
        public BaseNodeView(BaseNode node, BaseTreeWindow treeWindow, string path) : base(path)
        {
            m_Node = node;
            m_TreeWindow = treeWindow;
            m_NodeBorder = this.Q("node-border");
            m_SelectionBorder = this.Q("node-selection-border");
            m_Top = this.Q("top");

            m_InputPortContainer = inputContainer as NodePortContainerView;
            m_InputPortContainer.Init(m_Node, this);

            m_OutputPortContainer = outputContainer as NodePortContainerView;
            m_OutputPortContainer.Init(m_Node, this);

            m_NodePanel = this.Q<NodePanelView>();
            m_NodePanel.Init(m_Node, this);

            m_NodeInputFieldContainer = this.Q<NodeInputFieldContainerView>();
            m_NodeInputFieldContainer.Init(m_Node, this);

            NodeColorAttribute nodeColor = m_Node.GetAttribute<NodeColorAttribute>();
            BindAuthoringPresentation(
                m_Node.GUID,
                NodeName(),
                m_Node.Position,
                nodeColor != null
                    ? nodeColor.Color / 255f
                    : (Color?)null);

            RefreshCapabilities();

            expanded = m_Node.Expanded;

            GeneratePorts();
            GeneratePropertyPorts();
            Refresh();
            SortPropertyPorts();
            RefreshNodeExpandedState();
            RefreshStateMachineGraphState();

            //inputContainer.RegisterCallback<MouseEnterEvent>((e) =>
            //{
            //    if(TreeView.Drag)
            //        Debug.Log($"Enter {title}");
            //});
            //inputContainer.RegisterCallback<MouseLeaveEvent>((e) =>
            //{
            //    if (TreeView.Drag)
            //        Debug.Log($"Leave {title}");
            //});

            this.Q("panel-button").AddManipulator(new Clickable(ToggleShowPanel));
            titleContainer.RegisterCallback<MouseDownEvent>(OnTitleMouseDown);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            m_Node.OnNodeChanged = () =>
            {
                RefreshPropertyPorts();
                Refresh();
                RefreshNodeExpandedState();
                SortPropertyPorts();
            };

            schedule.Execute(Update);
        }


        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (m_TreeWindow.IsLiveDebug)
            {
                AppendReferenceActions(evt.menu);
                return;
            }
            base.BuildContextualMenu(evt);
            if (evt.target is BaseNodeView bnv)
            {
                if (TreeView.selection.Contains(this))
                {
                    evt.menu.AppendAction("Select Node Script", (s) =>
                    {
                        SelectNodeScript();
                    });
                    evt.menu.AppendAction("Select NodeView Script", (s) =>
                    {
                        SelectNodeViewScript();
                    });
                    evt.menu.AppendAction("Open Node Script", (s) =>
                    {
                        OpenNodeScript();
                    });
                    evt.menu.AppendAction("Open NodeView Script", (s) =>
                    {
                        OpenNodeViewScript();
                    });
                    evt.menu.AppendSeparator();
                }

                List<BaseNodeView> canShowNodeViews = new List<BaseNodeView>();
                List<BaseNodeView> canHideNodeViews = new List<BaseNodeView>();
                foreach (var element in TreeView.selection)
                {
                    if (element is BaseNodeView nodeView)
                    {
                        if (nodeView.CanShowPanel())
                        {
                            if (nodeView.Node.ShowPanel)
                                canHideNodeViews.Add(nodeView);
                            else
                                canShowNodeViews.Add(nodeView);
                        }
                    }
                }
                evt.menu.AppendAction("Show Panel", delegate
                {
                    canShowNodeViews.ForEach(i => i.ToggleShowPanel());
                }, (DropdownMenuAction a) => canShowNodeViews.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendAction("Hide Panel", delegate
                {
                    canHideNodeViews.ForEach(i => i.ToggleShowPanel());
                }, (DropdownMenuAction a) => canHideNodeViews.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                AppendReferenceActions(evt.menu);
                evt.menu.AppendSeparator();
            }
        }

        public void BindSharedProjection(
            GraphAuthoringNodeProjection projection,
            GraphAuthoringCapabilityDescriptor capability)
        {
            if (projection == null ||
                projection.NodeId.Value != m_Node.GUID)
            {
                throw new InvalidOperationException(
                    $"BTSMTL Node '{m_Node.GUID}' received a mismatched authoring projection.");
            }
            if (capability == null ||
                !capability.CapabilityId.Equals(
                    projection.CapabilityId) ||
                capability.AuthoringType != m_Node.GetType())
            {
                throw new InvalidOperationException(
                    $"BTSMTL Node '{m_Node.GUID}' received a mismatched authoring capability.");
            }
            AuthoringProjection = projection;
            AuthoringCapability = capability;
            BindAuthoringDescriptor(
                capability,
                projection.DisplayName,
                projection.Status,
                m_Node.GetAttribute<NodeColorAttribute>() != null);
        }

        public void SetDisplayName(string displayName)
        {
            TreeView.SharedAuthoring.Mutation.Apply(
                TreeView.SharedAuthoring.Document,
                new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.SetDisplayName,
                    new GraphAuthoringElementId(
                        m_Node.GUID),
                    value: displayName ?? string.Empty));
            Refresh();
        }

        public void ExecuteAuthoringCommand(
            GraphAuthoringCommandId commandId,
            object value = null)
        {
            TreeView.SharedAuthoring.Mutation.Apply(
                TreeView.SharedAuthoring.Document,
                new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.ExecuteCommand,
                    new GraphAuthoringElementId(m_Node.GUID),
                    commandId: commandId,
                    value: value));
        }

        protected virtual void AppendReferenceActions(DropdownMenu menu)
        {
            if (AuthoringPageOpenRegistry.CanOpen(m_Node))
            {
                menu.AppendAction("Open", _ => AuthoringPageOpenRegistry.TryOpen(m_TreeWindow, m_Node));
                menu.AppendSeparator();
            }
            foreach (var graphReference in m_Node.GetGraphReferences())
            {
                NodeGraphReference reference = graphReference;
                string label = string.IsNullOrEmpty(reference.Label) ? "Graph" : reference.Label;
                menu.AppendAction($"Open Reference/{label}", (s) =>
                {
                    m_TreeWindow.PushReferencedTree(m_Node, reference);
                }, (DropdownMenuAction a) => reference.Tree ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            foreach (var assetReference in m_Node.GetAssetReferences())
            {
                NodeAssetReference reference = assetReference;
                string label = string.IsNullOrEmpty(reference.Label) ? "Asset" : reference.Label;
                menu.AppendAction($"Open Reference/{label}", (s) =>
                {
                    Selection.activeObject = reference.Asset;
                    AssetDatabase.OpenAsset(reference.Asset);
                }, (DropdownMenuAction a) => reference.Asset ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }
        }

        public virtual void Update()
        {
            m_InputPortContainer.Update();
            m_OutputPortContainer.Update();
        }

        public void SetRuntimeDebugState(string status, string detail)
        {
            string value = status ?? string.Empty;
            Color color = value.IndexOf("Failure", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          value.IndexOf("Force", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          value.IndexOf("Cancel", StringComparison.OrdinalIgnoreCase) >= 0
                ? m_FailureColor
                : value.IndexOf("Success", StringComparison.OrdinalIgnoreCase) >= 0 ||
                  value.IndexOf("Completed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                  value.IndexOf("Exited", StringComparison.OrdinalIgnoreCase) >= 0 ||
                  value.IndexOf("Stopped", StringComparison.OrdinalIgnoreCase) >= 0
                    ? m_SuccessColor
                    : m_RunningColor;
            m_Top.style.backgroundColor = m_LastColor = color;
            m_AnimationFrame = 0;
            tooltip = string.IsNullOrEmpty(detail) ? value : $"{value}\n{detail}";
            SetStateClass(color == m_FailureColor ? "nodeState-Failure" : color == m_SuccessColor ? "nodeState-Success" : "nodeState-Running");
        }

        public void ClearRuntimeDebugState()
        {
            m_Top.style.backgroundColor = m_LastColor = new Color(0, 0, 0, 0);
            tooltip = string.Empty;
            SetStateClass("nodeState-None");
        }

        protected override void OnRuntimeReadOnlyChanged(
            bool readOnly)
        {
            m_NodePanel?.SetEnabled(!readOnly);
            m_NodeInputFieldContainer?.SetEnabled(!readOnly);
        }

        void SetStateClass(string active)
        {
            string[] classes = { "nodeState-None", "nodeState-Running", "nodeState-Success", "nodeState-Failure" };
            for (int i = 0; i < classes.Length; i++)
            {
                if (classes[i] == active)
                    AddToClassList(classes[i]);
                else
                    RemoveFromClassList(classes[i]);
            }
        }

        int m_AnimationDuration = 60;
        Color m_RunningColor = new Color(242, 210, 63, 255) / 255;
        Color m_SuccessColor = new Color(65, 172, 66, 255) / 255;
        Color m_FailureColor = new Color(234, 65, 76, 255) / 255;

        int m_AnimationFrame;
        Color m_LastColor;
        public virtual void Animation()
        {
            if (m_AnimationFrame > 0)
            {
                m_Top.style.backgroundColor = Color.Lerp(m_LastColor, new Color(0, 0, 0, 0), (float)(m_AnimationDuration - m_AnimationFrame) / m_AnimationDuration);
                m_AnimationFrame--;
            }
        }

        public virtual void Refresh()
        {
            title = NodeName();
            m_NodePanel.Refresh();
            m_NodeInputFieldContainer.Refresh();
            RefreshShowPanelState();
            RefreshStateMachineGraphState();
            schedule.Execute(RefreshCollapseButton);
        }

        public virtual void RefreshStateMachineGraphState()
        {
            if (TreeView?.Tree is StateMachineGraph)
            {
                Color markerColor;
                if (m_Node is StateMachineEnterNode)
                    markerColor = new Color(0.25f, 0.9f, 0.55f, 1f);
                else if (m_Node is StateMachineAnyStateNode)
                    markerColor = new Color(0.25f, 0.85f, 0.95f, 1f);
                else if (m_Node is StateMachineExitNode)
                    markerColor = new Color(1f, 0.45f, 0.45f, 1f);
                else
                    markerColor = new Color(0.25f, 0.25f, 0.25f, 1f);

                if (m_Node is StateMachineControlNode)
                {
                    titleContainer.style.borderBottomWidth = 2;
                    titleContainer.style.borderBottomColor = markerColor;
                    return;
                }

                if (m_Node is StateNode)
                {
                    titleContainer.style.borderBottomWidth = 1;
                    titleContainer.style.borderBottomColor = markerColor;
                    return;
                }
            }

            titleContainer.style.borderBottomWidth = 0;
        }
        public virtual void SyncSerializedPropertyPathes()
        {
            //if (nodeIndex == -1)
            //    return;

            //var nodeIndexString = nodeIndex.ToString();
            //foreach (var propertyField in this.Query<PropertyField>().ToList())
            //{
            //    propertyField.Unbind();
            //    propertyField.bindingPath = s_ReplaceNodeIndexPropertyPath.Replace(propertyField.bindingPath, m => m.Groups[1].Value + nodeIndexString + m.Groups[3].Value);
            //    propertyField.BindProperty(m_Node.GetSerializedTree());
            //}

            m_NodePanel.Refresh();
            m_NodeInputFieldContainer.Refresh();
        }

        public virtual void OnMoved(Vector2 position)
        {
            SetPosition(new Rect(position, GetPosition().size));
            m_TreeWindow.TreeView.CommitMovedElements(
                new GraphElement[] { this });
        }
        public virtual void OnInputPortConnected(BasePortView portView)
        {
            schedule.Execute(RefreshCollapseButton);
        }
        public virtual void OnInputPortDisconnected(BasePortView portView)
        {
            schedule.Execute(RefreshCollapseButton);
        }
        public virtual void OnOutputPortConnected(BasePortView portView)
        {
            schedule.Execute(RefreshCollapseButton);
        }
        public virtual void OnOutputPortDisconnected(BasePortView portView)
        {
            schedule.Execute(RefreshCollapseButton);
        }
        public virtual void OnInputPropertyPortConnected(PropertyPortView inputPropertyPortView)
        {
            m_NodePanel.SetPropertyPortFieldEnable(inputPropertyPortView.PropertyPort.PortId, false);
            m_NodeInputFieldContainer.SetPropertyPortFieldEnable(inputPropertyPortView.PropertyPort.PortId, false);
            schedule.Execute(RefreshCollapseButton);

            PropertyPortOnLinkedAttribute propertyPortOnLinkedAttribute = m_Node.FindFieldAccessor(inputPropertyPortView.PropertyPort.FieldKey)?.GetAttribute<PropertyPortOnLinkedAttribute>();
            if (propertyPortOnLinkedAttribute != null)
            {
                MethodInfo methodInfo = m_Node.GetMethod(propertyPortOnLinkedAttribute.CallbackName);
                if (methodInfo != null)
                    methodInfo.Invoke(m_Node, null);
            }
        }
        public virtual void OnInputPropertyPortDisconnected(PropertyPortView inputPropertyPortView)
        {
            m_NodePanel.SetPropertyPortFieldEnable(inputPropertyPortView.PropertyPort.PortId, true);
            m_NodeInputFieldContainer.SetPropertyPortFieldEnable(inputPropertyPortView.PropertyPort.PortId, true);
            schedule.Execute(RefreshCollapseButton);

            PropertyPortOnUnlinkedAttribute propertyPortOnLinkedAttribute = m_Node.FindFieldAccessor(inputPropertyPortView.PropertyPort.FieldKey)?.GetAttribute<PropertyPortOnUnlinkedAttribute>();
            if (propertyPortOnLinkedAttribute != null)
            {
                MethodInfo methodInfo = m_Node.GetMethod(propertyPortOnLinkedAttribute.CallbackName);
                if (methodInfo != null)
                    methodInfo.Invoke(m_Node, null);
            }
        }
        public virtual void OnOutputPropertyPortConnected(PropertyPortView outputPropertyPortView)
        {
            schedule.Execute(RefreshCollapseButton);
        }
        public virtual void OnOutputPropertyPortDisconnected(PropertyPortView outputPropertyPortView)
        {
            schedule.Execute(RefreshCollapseButton);
        }

        public override bool expanded
        {
            get => base.expanded;
            set
            {
                base.expanded = value;
                RefreshNodeExpandedState();
            }
        }
        protected override void ToggleCollapse()
        {
            if (RuntimeReadOnly)
                return;
            if (CanCollapsed())
            {
                base.ToggleCollapse();
                m_Node.ApplyModify("SetExpandedState Node", () =>
                {
                    m_Node.Expanded = expanded;
                });

                if (m_StackNodeView == null)
                    BringToFront();
            }
        }
        protected virtual bool CanCollapsed()
        {
            //List<Port> inputPorts = inputContainer.Query<Port>().ToList();
            //List<Port> outputPorts = outputContainer.Query<Port>().ToList();
            //foreach (var item in inputPorts)
            //{
            //    if (!item.connected)
            //        return true;
            //}
            //foreach (var item in outputPorts)
            //{
            //    if (!item.connected)
            //        return true;
            //}
            //return false;
            return true;
        }
        protected virtual void RefreshCollapseButton()
        {
            bool flag = false;
            List<Port> list = inputContainer.Query<Port>().ToList();
            List<Port> list2 = outputContainer.Query<Port>().ToList();
            foreach (Port item in list)
            {
                if (!item.connected)
                {
                    flag = true;
                    break;
                }
            }

            if (!flag)
            {
                foreach (Port item2 in list2)
                {
                    if (!item2.connected)
                    {
                        flag = true;
                        break;
                    }
                }
            }

            if (m_CollapseButton != null)
            {
                if (flag)
                {
                    m_CollapseButton.SetEnabled(!m_CollapseButton.enabledSelf);
                    m_CollapseButton.SetEnabled(true);
                }
                else
                {
                    m_CollapseButton.SetEnabled(!m_CollapseButton.enabledSelf);
                    m_CollapseButton.SetEnabled(false);
                }
            }
        }
        public virtual void RefreshNodeExpandedState()
        {
            RefreshExpandedState();
            m_NodeInputFieldContainer.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            m_NodePanel.style.top = this.Query<BasePortView>().ToList().Count > 0 ? 26 : 0;
        }
        public virtual void RefreshCapabilities()
        {
            capabilities = (Capabilities)m_Node.Capabilities;
        }

        protected virtual void ToggleShowPanel()
        {
            m_Node.ApplyModify("Switch Node Panel", () =>
            {
                m_Node.ShowPanel = !m_Node.ShowPanel;
                RefreshShowPanelState();
            });

            if (m_StackNodeView == null)
                BringToFront();
        }

        protected virtual void OnTitleMouseDown(MouseDownEvent evt)
        {
            if (evt.clickCount != 2)
                return;

            if (AuthoringPageOpenRegistry.TryOpen(m_TreeWindow, m_Node))
            {
                evt.StopPropagation();
                return;
            }

            foreach (var graphReference in m_Node.GetGraphReferences())
            {
                if (graphReference.Tree == null)
                    continue;

                m_TreeWindow.PushReferencedTree(m_Node, graphReference);
                evt.StopPropagation();
                return;
            }
        }
        protected virtual bool CanShowPanel()
        {
            return m_NodePanel.PropertyCount > 0;
        }
        public virtual void RefreshShowPanelState()
        {
            if (CanShowPanel())
            {
                this.Q("title-label").style.borderRightWidth = 1;
                this.Q("panel-button-container").style.visibility = Visibility.Visible;
            }
            else
            {
                this.Q("title-label").style.borderRightWidth = 0;
                this.Q("panel-button-container").style.visibility = Visibility.Hidden;
            }

            RemoveFromClassList("showPanel");
            RemoveFromClassList("hidePanel");
            if (m_Node.ShowPanel && CanShowPanel())
            {
                m_NodePanel.style.visibility = Visibility.Visible;
                AddToClassList("showPanel");
            }
            else
            {
                m_NodePanel.style.visibility = Visibility.Hidden;
                AddToClassList("hidePanel");
            }
        }

        protected virtual void GeneratePorts()
        {
            foreach (var declaration in m_Node.GetFlowPortDeclarations(TreeView?.Tree))
            {
                switch (declaration.Direction)
                {
                    case PortDirection.Input:
                        m_InputPortContainer.AddPort(declaration.Name, Direction.Input, (Port.Capacity)declaration.Capacity);
                        break;
                    case PortDirection.Output:
                        m_OutputPortContainer.AddPort(declaration.Name, Direction.Output, (Port.Capacity)declaration.Capacity);
                        break;
                }
            }
        }
        protected virtual void GeneratePropertyPorts()
        {
            foreach (var accessor in m_Node.GetFieldAccessors())
            {
                if (!accessor.IsShow())
                    continue;

                if (accessor.TryGetPropertyPortList(out List<PropertyPort> propertyPorts))
                {
                    foreach (var propertyPort in propertyPorts)
                    {
                        if (propertyPort != null)
                            AddPropertyPortView(propertyPort, propertyPort.DisplayName);
                    }
                    continue;
                }

                var propertyPortAttributes = accessor.GetAttributes<PropertyPortAttribute>();
                if (propertyPortAttributes.Count() > 0)
                {
                    PropertyPortAttribute propertyPortAttribute = propertyPortAttributes.ElementAt(0);
                    PropertyPort propertyPort = accessor.GetValue() as PropertyPort;
                    if (propertyPort == null)
                        continue;
                    AddPropertyPortView(propertyPort, propertyPortAttribute.Name);
                }
                else
                {
                    var variablePropertyPortAttributes = accessor.GetAttributes<VariablePropertyPortAttribute>();
                    if (variablePropertyPortAttributes.Count() > 0)
                    {
                        VariablePropertyPortAttribute variablePropertyPortAttribute = variablePropertyPortAttributes.ElementAt(0);
                        PropertyPort propertyPort = accessor.GetValue() as PropertyPort;
                        if (propertyPort == null)
                            continue;
                        switch (variablePropertyPortAttribute.Direction)
                        {
                            case PortDirection.Input:
                                m_InputPortContainer.AddVariablePropertyPort(propertyPort, variablePropertyPortAttribute.Name, variablePropertyPortAttribute.AcceptableTypesMethodName, variablePropertyPortAttribute.AcceptableTypes, Port.Capacity.Single);
                                break;
                            case PortDirection.Output:
                                m_OutputPortContainer.AddVariablePropertyPort(propertyPort, variablePropertyPortAttribute.Name, variablePropertyPortAttribute.AcceptableTypesMethodName, variablePropertyPortAttribute.AcceptableTypes, Port.Capacity.Multi);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }
        protected virtual void RefreshPropertyPorts()
        {
            foreach (var accessor in m_Node.GetFieldAccessors())
            {
                if (accessor.TryGetPropertyPortList(out List<PropertyPort> propertyPorts))
                {
                    HashSet<string> visiblePortIds = new HashSet<string>();
                    if (accessor.IsShow())
                    {
                        foreach (var propertyPort in propertyPorts)
                        {
                            if (propertyPort == null)
                                continue;

                            visiblePortIds.Add(propertyPort.PortId);
                            SyncPropertyPortView(propertyPort, propertyPort.DisplayName);
                        }
                    }
                    RemoveStaleListPropertyPortViews(accessor, visiblePortIds);
                    continue;
                }

                var propertyPortAttributes = accessor.GetAttributes<PropertyPortAttribute>();
                if (propertyPortAttributes.Count() > 0)
                {
                    PropertyPortAttribute propertyPortAttribute = propertyPortAttributes.ElementAt(0);
                    PropertyPort propertyPort = accessor.GetValue() as PropertyPort;
                    if (propertyPort == null)
                        continue;
                    switch (propertyPortAttribute.Direction)
                    {
                        case PortDirection.Input:
                            if (accessor.IsShow() && !InputPropertyPorts.ContainsKey(propertyPort.PortId))
                                m_InputPortContainer.AddPropertyPort(propertyPort, propertyPortAttribute.Name, Port.Capacity.Single);
                            else if (!accessor.IsShow() && InputPropertyPorts.ContainsKey(propertyPort.PortId))
                                m_InputPortContainer.RemovePropertyPort(propertyPort);
                            break;
                        case PortDirection.Output:
                            if (accessor.IsShow() && !OutputPropertyPorts.ContainsKey(propertyPort.PortId))
                                m_OutputPortContainer.AddPropertyPort(propertyPort, propertyPortAttribute.Name, Port.Capacity.Multi);
                            else if (!accessor.IsShow() && OutputPropertyPorts.ContainsKey(propertyPort.PortId))
                                m_OutputPortContainer.RemovePropertyPort(propertyPort);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    var variablePropertyPortAttributes = accessor.GetAttributes<VariablePropertyPortAttribute>();
                    if (variablePropertyPortAttributes.Count() > 0)
                    {
                        VariablePropertyPortAttribute variablePropertyPortAttribute = variablePropertyPortAttributes.ElementAt(0);
                        PropertyPort propertyPort = accessor.GetValue() as PropertyPort;
                        if (propertyPort == null)
                            continue;
                        switch (variablePropertyPortAttribute.Direction)
                        {
                            case PortDirection.Input:
                                if (accessor.IsShow() && !InputPropertyPorts.ContainsKey(propertyPort.PortId))
                                    m_InputPortContainer.AddVariablePropertyPort(propertyPort, variablePropertyPortAttribute.Name, variablePropertyPortAttribute.AcceptableTypesMethodName, variablePropertyPortAttribute.AcceptableTypes, Port.Capacity.Single);
                                else if (!accessor.IsShow() && InputPropertyPorts.ContainsKey(propertyPort.PortId))
                                    m_InputPortContainer.RemovePropertyPort(propertyPort);
                                break;
                            case PortDirection.Output:
                                if (accessor.IsShow() && !OutputPropertyPorts.ContainsKey(propertyPort.PortId))
                                    m_OutputPortContainer.AddVariablePropertyPort(propertyPort, variablePropertyPortAttribute.Name, variablePropertyPortAttribute.AcceptableTypesMethodName, variablePropertyPortAttribute.AcceptableTypes, Port.Capacity.Multi);
                                else if (!accessor.IsShow() && OutputPropertyPorts.ContainsKey(propertyPort.PortId))
                                    m_OutputPortContainer.RemovePropertyPort(propertyPort);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }
        protected virtual void AddPropertyPortView(PropertyPort propertyPort, string portName)
        {
            switch (propertyPort.Direction)
            {
                case PortDirection.Input:
                    if (!InputPropertyPorts.ContainsKey(propertyPort.PortId))
                        m_InputPortContainer.AddPropertyPort(propertyPort, portName, Port.Capacity.Single);
                    break;
                case PortDirection.Output:
                    if (!OutputPropertyPorts.ContainsKey(propertyPort.PortId))
                        m_OutputPortContainer.AddPropertyPort(propertyPort, portName, Port.Capacity.Multi);
                    break;
            }
        }

        protected virtual bool ContainsPropertyPortView(PropertyPort propertyPort)
        {
            return InputPropertyPorts.ContainsKey(propertyPort.PortId) || OutputPropertyPorts.ContainsKey(propertyPort.PortId);
        }

        protected virtual void SyncPropertyPortView(PropertyPort propertyPort, string portName)
        {
            if (propertyPort.Direction == PortDirection.Input)
            {
                if (OutputPropertyPorts.ContainsKey(propertyPort.PortId))
                    m_OutputPortContainer.RemovePropertyPort(propertyPort);
                m_InputPortContainer.AddPropertyPort(propertyPort, portName, Port.Capacity.Single);
                return;
            }

            if (propertyPort.Direction == PortDirection.Output)
            {
                if (InputPropertyPorts.ContainsKey(propertyPort.PortId))
                    m_InputPortContainer.RemovePropertyPort(propertyPort);
                m_OutputPortContainer.AddPropertyPort(propertyPort, portName, Port.Capacity.Multi);
            }
        }

        protected virtual void RemoveStaleListPropertyPortViews(NodeFieldAccessor accessor, HashSet<string> visiblePortIds)
        {
            string fieldPrefix = accessor.FieldKey + ".";
            foreach (var propertyPort in InputPropertyPorts.Values
                         .Select(i => i.PropertyPort)
                         .Where(i => i != null && !string.IsNullOrEmpty(i.FieldKey) && i.FieldKey.StartsWith(fieldPrefix) && !visiblePortIds.Contains(i.PortId))
                         .ToList())
                m_InputPortContainer.RemovePropertyPort(propertyPort);

            foreach (var propertyPort in OutputPropertyPorts.Values
                         .Select(i => i.PropertyPort)
                         .Where(i => i != null && !string.IsNullOrEmpty(i.FieldKey) && i.FieldKey.StartsWith(fieldPrefix) && !visiblePortIds.Contains(i.PortId))
                         .ToList())
                m_OutputPortContainer.RemovePropertyPort(propertyPort);
        }
        protected virtual void SortPropertyPorts()
        {
            m_InputPortContainer.Sort();
            m_OutputPortContainer.Sort();
        }
        protected virtual void OnGeometryChanged(GeometryChangedEvent geometryChangedEvent)
        {

        }

        string NodeName()
        {
            return m_Node.ResolvedDisplayName;
        }

        void SelectNodeScript()
        {
            var scriptInfo = TreeDesignerUtility.GetNodeScript(m_Node.GetType());
            if (scriptInfo != null)
                Selection.activeObject = scriptInfo.Mono;
        }
        void OpenNodeScript()
        {
            var scriptInfo = TreeDesignerUtility.GetNodeScript(m_Node.GetType());
            if (scriptInfo != null)
                AssetDatabase.OpenAsset(scriptInfo.Mono.GetInstanceID(), scriptInfo.LineNumber, scriptInfo.ColumnNumber);
        }
        void SelectNodeViewScript()
        {
            NodeViewAttribute nodeViewAttribute = m_Node.GetAttribute<NodeViewAttribute>();
            if (nodeViewAttribute != null)
            {
                var script = TreeDesignerUtility.GetNodeViewScript(TreeDesignerUtility.GetNodeViewType(nodeViewAttribute.NodeViewTypeName));
                if (script != null)
                    Selection.activeObject = script;
            }
            else
            {
                var script = TreeDesignerUtility.GetNodeViewScript(typeof(BaseNodeView));
                if (script != null)
                    Selection.activeObject = script;
            }
        }
        void OpenNodeViewScript()
        {
            NodeViewAttribute nodeViewAttribute = m_Node.GetAttribute<NodeViewAttribute>();
            if (nodeViewAttribute != null)
            {
                var script = TreeDesignerUtility.GetNodeViewScript(TreeDesignerUtility.GetNodeViewType(nodeViewAttribute.NodeViewTypeName));
                if (script != null)
                    AssetDatabase.OpenAsset(script.GetInstanceID(), 0, 0);
            }
            else
            {
                var script = TreeDesignerUtility.GetNodeViewScript(typeof(BaseNodeView));
                if (script != null)
                    AssetDatabase.OpenAsset(script.GetInstanceID(), 0, 0);
            }
        }

    }
}
