using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
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
    public enum GraphDataCatalogSourceFilter { All, Input, Blackboard }
    public enum PipelineBlackboardScopeFilter { All, Character, Graph, State, ActionInstance, Frame }
    public enum PipelineBlackboardContextFilter { AllVisible, CurrentContext, Local, Inherited }

    public class BaseTreeInspectorView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<BaseTreeInspectorView, UxmlTraits> { }
        protected virtual string m_VisualTreeName => "BaseTreeInspectorInside";

        protected BaseTree m_Tree;
        public BaseTree Tree => m_Tree;
        protected readonly List<BaseTree> m_VisibleBlackboardTrees = new List<BaseTree>();

        protected VisualElement m_DataPage;
        protected VisualElement m_SelectionPage;
        protected VisualElement m_SelectionInspectorContainer;
        protected VisualElement m_GraphDataCatalogContainer;
        protected VisualElement m_GraphDataCreationBar;
        protected VisualElement m_GraphDataBlackboardFilterPanel;
        protected object m_AuthoringContext;

        protected Button m_DataTabButton;
        protected Button m_SelectionTabButton;
        protected Button m_GraphDataAddButton;
        protected Button m_GraphDataCreateButton;
        protected Button m_GraphDataCancelButton;
        protected Button m_GraphDataAllSourceButton;
        protected Button m_GraphDataInputSourceButton;
        protected Button m_GraphDataBlackboardSourceButton;
        protected Button m_GraphDataBlackboardFilterButton;
        protected TextField m_GraphDataNameField;
        protected DropdownField m_GraphDataScopeField;
        protected DropdownField m_GraphDataTypeField;
        protected EnumField m_GraphDataScopeFilterField;
        protected EnumField m_GraphDataContextFilterField;
        protected ToolbarSearchField m_GraphDataSearchField;

        readonly Dictionary<string, bool> m_GraphDataFoldoutStates = new Dictionary<string, bool>(StringComparer.Ordinal);
        readonly HashSet<string> m_ExpandedGraphDataEntries = new HashSet<string>(StringComparer.Ordinal);
        readonly List<IGraphDataCatalogSource> m_GraphDataSources = new List<IGraphDataCatalogSource>();
        IReadOnlyList<GraphDataCatalogCreationOption> m_GraphDataScopeOptions = Array.Empty<GraphDataCatalogCreationOption>();
        IReadOnlyList<GraphDataCatalogCreationOption> m_GraphDataTypeOptions = Array.Empty<GraphDataCatalogCreationOption>();
        GraphDataCatalogContext m_GraphDataContext;
        GraphDataCatalogSourceFilter m_GraphDataSourceFilter = GraphDataCatalogSourceFilter.All;
        int m_GraphDataGeneration;
        bool m_GraphDataRefreshScheduled;
        bool m_GraphDataBlackboardFiltersExpanded;
        bool m_HasSelection;

        Dictionary<FieldInfo, object> m_ValueMap = new Dictionary<FieldInfo, object>();
        
        public BaseTreeInspectorView()
        {
            VisualTreeAsset template = Resources.Load<VisualTreeAsset>($"VisualTree/{m_VisualTreeName}");
            template.CloneTree(this);
            AddToClassList("treeInspector");
            style.display = DisplayStyle.None;

            m_DataPage = this.Q("data-page");
            m_SelectionPage = this.Q("selection-inspector-page");
            m_SelectionInspectorContainer = this.Q("selection-inspector-container");
            m_DataTabButton = this.Q<Button>("data-tab-button");
            m_SelectionTabButton = this.Q<Button>("selection-tab-button");
            m_GraphDataCatalogContainer = this.Q("graph-data-catalog-container");
            m_GraphDataCreationBar = this.Q("graph-data-creation-bar");
            m_GraphDataBlackboardFilterPanel = this.Q("graph-data-blackboard-filter-panel");
            m_GraphDataNameField = this.Q<TextField>("graph-data-create-name");
            m_GraphDataScopeField = this.Q<DropdownField>("graph-data-create-scope");
            m_GraphDataTypeField = this.Q<DropdownField>("graph-data-create-type");
            m_GraphDataScopeFilterField = this.Q<EnumField>("graph-data-scope-filter");
            m_GraphDataContextFilterField = this.Q<EnumField>("graph-data-context-filter");
            m_GraphDataSearchField = this.Q<ToolbarSearchField>("graph-data-search");
            m_GraphDataAllSourceButton = this.Q<Button>("graph-data-source-all-button");
            m_GraphDataInputSourceButton = this.Q<Button>("graph-data-source-input-button");
            m_GraphDataBlackboardSourceButton = this.Q<Button>("graph-data-source-blackboard-button");
            m_GraphDataBlackboardFilterButton = this.Q<Button>("graph-data-blackboard-filter-button");

            m_GraphDataScopeFilterField?.Init(PipelineBlackboardScopeFilter.All);
            m_GraphDataContextFilterField?.Init(PipelineBlackboardContextFilter.AllVisible);
            m_GraphDataScopeFilterField?.RegisterValueChangedCallback(_ => OnGraphDataBlackboardFiltersChanged());
            m_GraphDataContextFilterField?.RegisterValueChangedCallback(_ => OnGraphDataBlackboardFiltersChanged());
            m_GraphDataSearchField?.RegisterValueChangedCallback(_ => RequestGraphDataRefresh());
            m_GraphDataAllSourceButton.clicked += () => SetGraphDataSourceFilter(GraphDataCatalogSourceFilter.All);
            m_GraphDataInputSourceButton.clicked += () => SetGraphDataSourceFilter(GraphDataCatalogSourceFilter.Input);
            m_GraphDataBlackboardSourceButton.clicked += () => SetGraphDataSourceFilter(GraphDataCatalogSourceFilter.Blackboard);
            m_GraphDataBlackboardFilterButton.clicked += ToggleGraphDataBlackboardFilters;

            m_DataTabButton.clicked += ShowDataTab;
            m_SelectionTabButton.clicked += ShowSelectionTab;

            m_GraphDataAddButton = this.Q<Button>("graph-data-add-button");
            m_GraphDataCreateButton = this.Q<Button>("graph-data-create-button");
            m_GraphDataCancelButton = this.Q<Button>("graph-data-cancel-button");
            AddGraphDataButtonIcon(m_GraphDataBlackboardFilterButton, "d_FilterByType");
            AddGraphDataButtonIcon(m_GraphDataCreateButton, "TestPassed");
            AddGraphDataButtonIcon(m_GraphDataCancelButton, "d_winbtn_win_close");
            m_GraphDataAddButton.clicked += ToggleGraphDataCreation;
            m_GraphDataCreateButton.clicked += CreateGraphDataDeclaration;
            m_GraphDataCancelButton.clicked += HideGraphDataCreation;
            m_GraphDataCreationBar.style.display = DisplayStyle.None;
            RefreshGraphDataFilterPresentation();

            RegisterCallback<AttachToPanelEvent>(_ => AttachGraphDataCatalog());
            RegisterCallback<DetachFromPanelEvent>(_ => DetachGraphDataCatalog());

            ShowDataTab();
        }

        protected virtual void AddGraphDataButtonIcon(Button button, string iconName)
        {
            Image image = new Image
            {
                image = EditorGUIUtility.IconContent(iconName).image,
                scaleMode = ScaleMode.ScaleToFit
            };
            image.AddToClassList("graph-data-button-icon");
            button.Add(image);
        }

        public virtual void SetAuthoringContext(object authoringContext)
        {
            m_AuthoringContext = authoringContext;
        }

        public virtual void SetVisibleBlackboardSources(IEnumerable<BaseTree> trees)
        {
            m_VisibleBlackboardTrees.Clear();
            if (trees == null)
                return;

            foreach (BaseTree tree in trees)
            {
                if (tree != null && !m_VisibleBlackboardTrees.Contains(tree))
                    m_VisibleBlackboardTrees.Add(tree);
            }
        }

        public IEnumerable<BaseExposedProperty> VisibleBlackboardDeclarations =>
            m_VisibleBlackboardTrees.SelectMany(i => i.ExposedProperties);

        public virtual void PopulateView(BaseTree tree)
        {
            ClearView();
            m_Tree = tree;
            if (!m_VisibleBlackboardTrees.Contains(tree))
                m_VisibleBlackboardTrees.Add(tree);

            PopulateGraphAuthoringSettings();
            m_GraphDataContext = new GraphDataCatalogContext(
                m_Tree,
                m_AuthoringContext,
                m_VisibleBlackboardTrees,
                ++m_GraphDataGeneration);
            RefreshGraphDataCreationOptions();
            RebuildGraphDataCatalog();

            ShowDataTab();
            style.display = DisplayStyle.Flex;
        }
        public virtual void ClearView()
        {
            m_GraphDataCatalogContainer.Clear();
            m_SelectionInspectorContainer.Clear();
            m_GraphDataContext = null;
            m_HasSelection = false;
            m_Tree = null;
        }
        public virtual void PopulateSelection(IEnumerable<GraphSelectable> selection)
        {
            List<GraphSelectable> selected = selection?.Where(i => i != null).ToList() ?? new List<GraphSelectable>();
            if (selected.Count == 0)
            {
                m_HasSelection = false;
                PopulateGraphAuthoringSettings();
                return;
            }

            m_HasSelection = true;
            m_SelectionInspectorContainer.Clear();

            if (selected.Count == 1 && selected[0] is BaseEdgeView edgeView)
            {
                if (edgeView.IsStateMachineTransitionEdge)
                {
                    PopulateTransitionEdge(edgeView);
                    ShowSelectionTab();
                    return;
                }

                if (edgeView.IsBTConditionEdge)
                {
                    PopulateBTConditionEdge(edgeView);
                    ShowSelectionTab();
                    return;
                }

                AddSelectionMessage("Edge");
                ShowSelectionTab();
                return;
            }

            if (selected.Count == 1 && selected[0] is BaseNodeView nodeView)
            {
                PopulateNode(nodeView);
                ShowSelectionTab();
                return;
            }

            AddSelectionMessage($"{selected.Count} Selected");
            ShowSelectionTab();
        }
        protected virtual void PopulateGraphAuthoringSettings()
        {
            m_SelectionInspectorContainer.Clear();
            AddInspectorTitle("Graph Settings");

            VisualElement settingsContainer = new VisualElement();
            settingsContainer.AddToClassList("graph-settings-container");
            BaseTreeInspector.PopulateAuthoringProperties(m_Tree, settingsContainer, m_ValueMap);
            if (settingsContainer.childCount == 0)
            {
                AddSelectionMessage("No editable graph settings.");
                return;
            }

            m_SelectionInspectorContainer.Add(settingsContainer);
        }
        protected virtual void PopulateTransitionEdge(BaseEdgeView edgeView)
        {
            BaseEdge edge = edgeView.Edge;
            AddInspectorTitle("Transition");
            AddInspectorRow("From", NodeLabel(edge.StartNode));
            AddInspectorRow("To", NodeLabel(edge.EndNode));

            IntegerField priorityField = new IntegerField("Priority");
            priorityField.value = edge.TransitionPriority;
            priorityField.RegisterValueChangedCallback(evt =>
            {
                int value = Mathf.Max(0, evt.newValue);
                if (value != evt.newValue)
                    priorityField.SetValueWithoutNotify(value);
                edgeView.SetTransitionPriority(value);
            });
            priorityField.AddToClassList("selection-inspector-field");
            m_SelectionInspectorContainer.Add(priorityField);

            PopulateConditionRuleControls(edgeView, edge);
            AddInspectorRow("Status", string.IsNullOrEmpty(edgeView.EdgeSummary) ? "Unconditional" : edgeView.EdgeSummary);
        }

        protected virtual void PopulateBTConditionEdge(BaseEdgeView edgeView)
        {
            BaseEdge edge = edgeView.Edge;
            AddInspectorTitle("BT Edge");
            AddInspectorRow("From", NodeLabel(edge.StartNode));
            AddInspectorRow("To", NodeLabel(edge.EndNode));

            EnumField abortPolicyField = new EnumField("Abort Policy", edge.AbortPolicy);
            abortPolicyField.RegisterValueChangedCallback(evt =>
            {
                edgeView.SetAbortPolicy((BTAbortPolicy)evt.newValue);
                PopulateSelection(new GraphSelectable[] { edgeView });
            });
            abortPolicyField.AddToClassList("selection-inspector-field");
            m_SelectionInspectorContainer.Add(abortPolicyField);

            PopulateConditionRuleControls(edgeView, edge);
            AddInspectorRow("Status", string.IsNullOrEmpty(edgeView.EdgeSummary) ? "Unconditional" : edgeView.EdgeSummary);
        }

        protected virtual void PopulateConditionRuleControls(BaseEdgeView edgeView, BaseEdge edge)
        {
            AddInspectorRow("Ownership", edgeView.RuleGraphOwnershipLabel);
            if (!string.IsNullOrEmpty(edge.ConditionRuleGraphReferenceError) &&
                !(edgeView.IsBTConditionEdge && edge.ConditionRuleGraphReferenceStatus == ConditionRuleGraphReferenceStatus.Unspecified))
            {
                AddInspectorRow("Condition Error", edge.ConditionRuleGraphReferenceError);
            }

            ObjectField ruleGraphField = new ObjectField("Shared Rule Asset");
            ruleGraphField.objectType = typeof(BaseTreeAsset);
            ruleGraphField.allowSceneObjects = false;
            ruleGraphField.value = edge.SharedConditionRuleGraphAsset;
            ruleGraphField.RegisterValueChangedCallback(evt =>
            {
                if (!edgeView.ReplaceConditionRuleGraphAsset(evt.newValue as BaseTreeAsset))
                    ruleGraphField.SetValueWithoutNotify(edge.SharedConditionRuleGraphAsset);
                PopulateSelection(new GraphSelectable[] { edgeView });
            });
            ruleGraphField.AddToClassList("selection-inspector-field");
            m_SelectionInspectorContainer.Add(ruleGraphField);

            VisualElement buttonRow = new VisualElement();
            buttonRow.AddToClassList("selection-inspector-button-row");

            Button openButton = new Button(() => edgeView.OpenConditionRuleGraph()) { text = "Open Rule" };
            Button extractButton = new Button(() =>
            {
                edgeView.ExtractSharedConditionRuleGraph();
                PopulateSelection(new GraphSelectable[] { edgeView });
            }) { text = "Extract Shared" };
            Button useInlineButton = new Button(() =>
            {
                edgeView.UseInlineConditionRuleGraph();
                PopulateSelection(new GraphSelectable[] { edgeView });
            }) { text = "Use Inline Rule" };

            openButton.SetEnabled(edge.ConditionRuleGraph ||
                                  edgeView.IsBTConditionEdge && edge.ConditionRuleGraphReferenceStatus == ConditionRuleGraphReferenceStatus.Unspecified);
            bool resolvedInline = edge.ConditionRuleGraphReferenceStatus == ConditionRuleGraphReferenceStatus.ResolvedInline;
            extractButton.SetEnabled(resolvedInline);
            useInlineButton.SetEnabled(!resolvedInline);
            buttonRow.Add(openButton);
            if (resolvedInline)
                buttonRow.Add(extractButton);
            else
                buttonRow.Add(useInlineButton);
            m_SelectionInspectorContainer.Add(buttonRow);
        }

        protected virtual void PopulateNode(BaseNodeView nodeView)
        {
            BaseNode node = nodeView.Node;
            if (node == null)
            {
                AddSelectionMessage("Node");
                return;
            }

            if (node is StateMachineNode stateMachineNode)
            {
                PopulateStateMachineNode(nodeView, stateMachineNode);
                return;
            }

            if (node is StateNode stateNode)
            {
                PopulateStateNode(nodeView, stateNode);
                return;
            }

            AddNodeIdentity(nodeView, node);
            AuthoringPageOpenRegistry.PopulateInspector(this, nodeView, node);
        }

        public VisualElement SelectionInspectorContainer => m_SelectionInspectorContainer;

        public void RefreshNodeSelection(BaseNodeView nodeView)
        {
            PopulateSelection(new GraphSelectable[] { nodeView });
        }

        protected virtual void PopulateStateMachineNode(BaseNodeView nodeView, StateMachineNode node)
        {
            ScopedGraphReferenceModule module = node.GetModule<ScopedGraphReferenceModule>();
            BaseTree graph = module?.Graph;
            AddNodeIdentity(nodeView, node);
            AddInspectorRow("Ownership", GraphOwnershipLabel(module?.InlineGraph, module?.SharedGraphAsset));
            AddInspectorRow("Graph", graph ? graph.name : "Missing");
            if (module?.SharedGraphAsset)
            {
                AddSharedGraphField(
                    "Shared Graph",
                    module.SharedGraphAsset,
                    asset => asset == null || asset.Tree is StateMachineGraph,
                    asset =>
                    {
                        node.ApplyModify("Set State Machine Graph Reference", () =>
                        {
                            if (asset)
                                module.SetSharedGraphAsset(asset);
                            else
                                module.SetInlineGraph(StateMachineNode.CreateDefaultGraph());
                        });
                        nodeView.Refresh();
                        PopulateSelection(new GraphSelectable[] { nodeView });
                    });
            }
            AddGraphReferenceButtons(
                nodeView,
                node,
                new NodeGraphReference(node, "scopedGraph.m_InlineGraph", "State Machine", graph, module?.SharedGraphAsset, module != null && module.SharedGraphAsset == null, module?.ScopeId ?? string.Empty, true),
                module?.InlineGraph,
                () =>
                {
                    BaseTreeAsset sharedAsset = ExtractSharedGraphAsset(node, module.InlineGraph, "SharedGraphs");
                    if (!sharedAsset)
                        return;

                    node.ApplyModify("Extract Shared State Machine Graph", () =>
                    {
                        module.SetSharedGraphAsset(sharedAsset);
                    });
                    nodeView.Refresh();
                    PopulateSelection(new GraphSelectable[] { nodeView });
                },
                () =>
                {
                    node.ApplyModify("Use Local State Machine Graph", () =>
                    {
                        module.SetInlineGraph(StateMachineNode.CreateDefaultGraph());
                    });
                    nodeView.Refresh();
                    PopulateSelection(new GraphSelectable[] { nodeView });
                });
        }

        protected virtual void PopulateStateNode(BaseNodeView nodeView, StateNode node)
        {
            StateBehaviorGraphReferenceModule module = node.GetModule<StateBehaviorGraphReferenceModule>();
            BaseTree graph = module?.SubTree;
            AddNodeIdentity(nodeView, node);
            AddInspectorRow("Ownership", GraphOwnershipLabel(module?.InlineSubTree, module?.SharedSubTreeAsset));
            AddInspectorRow("Behavior", graph ? graph.name : "Missing");
            if (module?.SharedSubTreeAsset)
            {
                AddSharedGraphField(
                    "Shared Behavior",
                    module.SharedSubTreeAsset,
                    asset => asset == null || StateBehaviorGraphReferenceModule.CanReferenceTree(asset.Tree),
                    asset =>
                    {
                        node.ApplyModify("Set State Behavior Graph Reference", () =>
                        {
                            if (asset)
                                module.SetSharedSubTreeAsset(asset);
                            else
                                module.SetInlineSubTree(StateNode.CreateDefaultStateBehaviorGraph());
                        });
                        nodeView.Refresh();
                        PopulateSelection(new GraphSelectable[] { nodeView });
                    });
            }
            AddGraphReferenceButtons(
                nodeView,
                node,
                new NodeGraphReference(node, "stateBehaviorGraph.m_InlineSubTree", "State Behavior", graph, module?.SharedSubTreeAsset, module != null && module.SharedSubTreeAsset == null, module?.ScopeId ?? string.Empty, false),
                module?.InlineSubTree,
                () =>
                {
                    BaseTreeAsset sharedAsset = ExtractSharedGraphAsset(node, module.InlineSubTree, "SharedStateBehaviors");
                    if (!sharedAsset)
                        return;

                    node.ApplyModify("Extract Shared State Behavior Graph", () =>
                    {
                        module.SetSharedSubTreeAsset(sharedAsset);
                    });
                    nodeView.Refresh();
                    PopulateSelection(new GraphSelectable[] { nodeView });
                },
                () =>
                {
                    node.ApplyModify("Use Local State Behavior Graph", () =>
                    {
                        module.SetInlineSubTree(StateNode.CreateDefaultStateBehaviorGraph());
                    });
                    nodeView.Refresh();
                    PopulateSelection(new GraphSelectable[] { nodeView });
                });
        }

        protected virtual void AddNodeIdentity(BaseNodeView nodeView, BaseNode node)
        {
            Label titleLabel = AddInspectorTitle(NodeLabel(node));
            TextField displayNameField = new TextField("Display Name");
            displayNameField.isDelayed = true;
            displayNameField.value = node.DisplayName;
            displayNameField.RegisterValueChangedCallback(evt =>
            {
                string value = evt.newValue ?? string.Empty;
                if (value == node.DisplayName)
                    return;

                node.ApplyModify("Set Node Display Name", () =>
                {
                    node.DisplayName = value;
                });
                nodeView.Refresh();
                titleLabel.text = NodeLabel(node);
            });
            displayNameField.AddToClassList("selection-inspector-field");
            m_SelectionInspectorContainer.Add(displayNameField);
            AddInspectorRow("Type", node.NodeTypeDisplayName);
        }

        protected virtual void AddSharedGraphField(string label, BaseTreeAsset value, Func<BaseTreeAsset, bool> validate, Action<BaseTreeAsset> apply)
        {
            ObjectField field = new ObjectField(label);
            field.objectType = typeof(BaseTreeAsset);
            field.allowSceneObjects = false;
            field.value = value;
            field.RegisterValueChangedCallback(evt =>
            {
                BaseTreeAsset asset = evt.newValue as BaseTreeAsset;
                if (!validate(asset))
                {
                    field.SetValueWithoutNotify(value);
                    return;
                }

                apply(asset);
            });
            field.AddToClassList("selection-inspector-field");
            m_SelectionInspectorContainer.Add(field);
        }

        protected virtual void AddGraphReferenceButtons(BaseNodeView nodeView, BaseNode node, NodeGraphReference reference, BaseTree inlineGraph, Action extractShared, Action useLocalDefault)
        {
            VisualElement buttonRow = new VisualElement();
            buttonRow.AddToClassList("selection-inspector-button-row");

            Button openButton = new Button(() => nodeView.TreeView.TreeWindow.PushReferencedTree(node, reference)) { text = "Open" };
            Button extractButton = new Button(extractShared) { text = "Extract Shared" };
            Button localButton = new Button(useLocalDefault) { text = "Use Local Default" };

            openButton.SetEnabled(reference.Tree != null);
            extractButton.SetEnabled(inlineGraph != null && CanCreateSharedGraphAsset(node));
            localButton.SetEnabled(reference.SharedAsset);

            buttonRow.Add(openButton);
            buttonRow.Add(extractButton);
            buttonRow.Add(localButton);
            m_SelectionInspectorContainer.Add(buttonRow);
        }

        protected virtual string GraphOwnershipLabel(BaseTree inlineGraph, BaseTreeAsset sharedAsset)
        {
            if (sharedAsset)
                return "Shared Asset";
            return inlineGraph ? "Inline" : "Missing";
        }

        protected virtual bool CanCreateSharedGraphAsset(BaseNode node)
        {
            string ownerPath = AssetDatabase.GetAssetPath(node?.Owner?.SerializedOwner);
            return !string.IsNullOrEmpty(ownerPath);
        }

        protected virtual BaseTreeAsset ExtractSharedGraphAsset(BaseNode node, BaseTree graph, string folderSuffix)
        {
            if (node?.Owner == null || graph == null)
                return null;

            string ownerPath = AssetDatabase.GetAssetPath(node.Owner.SerializedOwner);
            if (string.IsNullOrEmpty(ownerPath))
                return null;

            string directory = Path.GetDirectoryName(ownerPath)?.Replace("\\", "/");
            if (string.IsNullOrEmpty(directory))
                return null;

            string folderName = $"{SanitizeFileName(node.Owner.name)}.{folderSuffix}";
            string sharedFolder = $"{directory}/{folderName}";
            if (!AssetDatabase.IsValidFolder(sharedFolder))
                AssetDatabase.CreateFolder(directory, folderName);

            BaseTree sharedGraph = graph.CloneForAuthoring();
            sharedGraph.name = graph.name;
            BaseTreeAsset sharedAsset = ScriptableObject.CreateInstance<BaseTreeAsset>();
            sharedAsset.SetTree(sharedGraph);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{sharedFolder}/{SanitizeFileName(sharedGraph.name)}.asset");
            AssetDatabase.CreateAsset(sharedAsset, assetPath);
            EditorUtility.SetDirty(sharedAsset);
            AssetDatabase.SaveAssets();
            return sharedAsset;
        }

        protected virtual string SanitizeFileName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "Graph";

            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value;
        }
        protected virtual void ShowDataTab()
        {
            SetTab(false);
        }
        protected virtual void ShowSelectionTab()
        {
            if (!m_HasSelection)
                PopulateGraphAuthoringSettings();
            SetTab(true);
        }
        protected virtual void SetTab(bool selection)
        {
            m_DataPage.style.display = selection ? DisplayStyle.None : DisplayStyle.Flex;
            m_SelectionPage.style.display = selection ? DisplayStyle.Flex : DisplayStyle.None;
            m_DataTabButton.EnableInClassList("selected", !selection);
            m_SelectionTabButton.EnableInClassList("selected", selection);
        }
        protected virtual Label AddInspectorTitle(string text)
        {
            Label label = new Label(text);
            label.AddToClassList("selection-inspector-title");
            m_SelectionInspectorContainer.Add(label);
            return label;
        }
        protected virtual void AddInspectorRow(string label, string value)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("selection-inspector-row");
            Label labelElement = new Label(label);
            labelElement.AddToClassList("selection-inspector-row-label");
            Label valueElement = new Label(value);
            valueElement.AddToClassList("selection-inspector-row-value");
            row.Add(labelElement);
            row.Add(valueElement);
            m_SelectionInspectorContainer.Add(row);
        }
        protected virtual void AddSelectionMessage(string text)
        {
            Label label = new Label(text);
            label.AddToClassList("selection-inspector-message");
            m_SelectionInspectorContainer.Add(label);
        }
        protected virtual string NodeLabel(BaseNode node)
        {
            if (node == null)
                return "None";

            return node.ResolvedDisplayName;
        }
        protected virtual void AttachGraphDataCatalog()
        {
            GraphDataCatalogSourceRegistry.Changed -= RebuildGraphDataSources;
            GraphDataCatalogSourceRegistry.Changed += RebuildGraphDataSources;
            RebuildGraphDataSources();
        }

        protected virtual void DetachGraphDataCatalog()
        {
            GraphDataCatalogSourceRegistry.Changed -= RebuildGraphDataSources;
            DisposeGraphDataSources();
        }

        protected virtual IReadOnlyList<IGraphDataCatalogSource> CreateGraphDataSources()
        {
            if (m_GraphDataSources.Count == 0)
                RebuildGraphDataSources();
            return m_GraphDataSources;
        }

        protected virtual void RebuildGraphDataSources()
        {
            DisposeGraphDataSources();
            m_GraphDataSources.Add(new BlackboardGraphDataCatalogSource());
            m_GraphDataSources.AddRange(GraphDataCatalogSourceRegistry.CreateSources());
            m_GraphDataSources.Sort((left, right) => left.Order.CompareTo(right.Order));
            foreach (IGraphDataCatalogSource source in m_GraphDataSources)
                source.Changed += RequestGraphDataRefresh;

            if (m_GraphDataContext != null)
            {
                RefreshGraphDataCreationOptions();
                RequestGraphDataRefresh();
            }
        }

        protected virtual void DisposeGraphDataSources()
        {
            foreach (IGraphDataCatalogSource source in m_GraphDataSources)
            {
                source.Changed -= RequestGraphDataRefresh;
                source.Dispose();
            }
            m_GraphDataSources.Clear();
        }

        protected virtual void RequestGraphDataRefresh()
        {
            if (m_GraphDataContext == null || m_GraphDataRefreshScheduled)
                return;

            m_GraphDataRefreshScheduled = true;
            schedule.Execute(() =>
            {
                m_GraphDataRefreshScheduled = false;
                RebuildGraphDataCatalog();
            });
        }

        protected virtual void RebuildGraphDataCatalog()
        {
            if (m_GraphDataCatalogContainer == null || m_GraphDataContext == null)
                return;

            CaptureGraphDataEntryStates();
            m_GraphDataCatalogContainer.Clear();
            Dictionary<string, Foldout> groups = new Dictionary<string, Foldout>(StringComparer.Ordinal);
            List<GraphDataCatalogEntry> entries = new List<GraphDataCatalogEntry>();
            foreach (IGraphDataCatalogSource source in CreateGraphDataSources())
            {
                List<GraphDataCatalogEntry> sourceEntries = source.GetEntries(m_GraphDataContext)?.Where(i => i != null).ToList()
                    ?? new List<GraphDataCatalogEntry>();
                if (sourceEntries.Count == 0)
                {
                    sourceEntries.Add(new GraphDataCatalogEntry(
                        source,
                        $"{source.Kind}:empty",
                        GraphDataCatalogEntryKind.Status,
                        source.Kind == GraphDataCatalogSourceKind.Blackboard ? "No declarations." : "No entries.",
                        string.Empty,
                        source.DisplayName,
                        source.Kind == GraphDataCatalogSourceKind.Input
                            ? GraphDataCatalogOwnership.External
                            : GraphDataCatalogOwnership.Local,
                        source.DisplayName,
                        string.Empty,
                        new Color(0.35f, 0.35f, 0.35f),
                        GraphDataCatalogCapability.None,
                        null,
                        m_GraphDataContext.Generation));
                }
                entries.AddRange(sourceEntries);
            }

            List<GraphDataCatalogEntry> visible = entries
                .Where(IsGraphDataEntryVisible)
                .OrderBy(i => i.Source.Order)
                .ThenBy(i => i.GroupPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (visible.Count == 0)
            {
                AddGraphDataMessage("No matching graph data.");
                return;
            }

            foreach (GraphDataCatalogEntry entry in visible)
            {
                VisualElement parent = ResolveGraphDataGroup(entry.GroupPath, groups);
                GraphDataCatalogEntryView view = new GraphDataCatalogEntryView(
                    entry,
                    m_GraphDataContext,
                    m_ExpandedGraphDataEntries.Contains(entry.StableId),
                    SetGraphDataEntryExpanded,
                    RequestGraphDataRefresh,
                    ReportGraphDataError);
                parent.Add(view);
            }
        }

        protected virtual void CaptureGraphDataEntryStates()
        {
            List<GraphDataCatalogEntryView> views = m_GraphDataCatalogContainer
                .Query<GraphDataCatalogEntryView>()
                .ToList();
            foreach (GraphDataCatalogEntryView view in views)
                SetGraphDataEntryExpanded(view.StableId, view.Expanded);
        }

        protected virtual VisualElement ResolveGraphDataGroup(string groupPath, Dictionary<string, Foldout> groups)
        {
            VisualElement parent = m_GraphDataCatalogContainer;
            string currentPath = string.Empty;
            foreach (string rawSegment in (groupPath ?? string.Empty).Split('/'))
            {
                string segment = rawSegment.Trim();
                if (segment.Length == 0)
                    continue;

                currentPath = currentPath.Length == 0 ? segment : $"{currentPath}/{segment}";
                if (!groups.TryGetValue(currentPath, out Foldout foldout))
                {
                    string stateKey = currentPath;
                    bool expanded = !m_GraphDataFoldoutStates.TryGetValue(stateKey, out bool saved) || saved;
                    foldout = new Foldout { text = segment, value = expanded };
                    foldout.AddToClassList(currentPath.Contains("/")
                        ? "graph-data-category-foldout"
                        : "graph-data-source-foldout");
                    foldout.RegisterValueChangedCallback(evt => m_GraphDataFoldoutStates[stateKey] = evt.newValue);
                    groups.Add(currentPath, foldout);
                    parent.Add(foldout);
                }
                parent = foldout.contentContainer;
            }
            return parent;
        }

        protected virtual bool IsGraphDataEntryVisible(GraphDataCatalogEntry entry)
        {
            GraphDataCatalogSourceFilter sourceFilter = m_GraphDataSourceFilter;
            if (sourceFilter != GraphDataCatalogSourceFilter.All &&
                !string.Equals(sourceFilter.ToString(), entry.Source.Kind.ToString(), StringComparison.Ordinal))
                return false;

            PipelineBlackboardScopeFilter scopeFilter = m_GraphDataScopeFilterField?.value is PipelineBlackboardScopeFilter scope
                ? scope
                : PipelineBlackboardScopeFilter.All;
            PipelineBlackboardContextFilter contextFilter = m_GraphDataContextFilterField?.value is PipelineBlackboardContextFilter context
                ? context
                : PipelineBlackboardContextFilter.AllVisible;
            bool blackboardSpecificFilter = scopeFilter != PipelineBlackboardScopeFilter.All ||
                                            contextFilter != PipelineBlackboardContextFilter.AllVisible;
            if (entry.Source.Kind == GraphDataCatalogSourceKind.Input && blackboardSpecificFilter)
                return false;

            if (entry.Source.Kind == GraphDataCatalogSourceKind.Blackboard)
            {
                if (entry.IsStatus && blackboardSpecificFilter)
                    return false;
                if (entry.Payload is BaseExposedProperty declaration &&
                    scopeFilter != PipelineBlackboardScopeFilter.All &&
                    !string.Equals(scopeFilter.ToString(), declaration.BlackboardScope.ToString(), StringComparison.Ordinal))
                    return false;
                if (contextFilter == PipelineBlackboardContextFilter.CurrentContext ||
                    contextFilter == PipelineBlackboardContextFilter.Local)
                {
                    if (entry.Ownership != GraphDataCatalogOwnership.Local)
                        return false;
                }
                else if (contextFilter == PipelineBlackboardContextFilter.Inherited &&
                         entry.Ownership != GraphDataCatalogOwnership.Inherited)
                {
                    return false;
                }
            }

            return entry.Matches(m_GraphDataSearchField?.value);
        }

        protected virtual void SetGraphDataEntryExpanded(string stableId, bool expanded)
        {
            if (expanded)
                m_ExpandedGraphDataEntries.Add(stableId);
            else
                m_ExpandedGraphDataEntries.Remove(stableId);
        }

        protected virtual void SetGraphDataSourceFilter(GraphDataCatalogSourceFilter sourceFilter)
        {
            m_GraphDataSourceFilter = sourceFilter;
            if (sourceFilter == GraphDataCatalogSourceFilter.Input)
            {
                m_GraphDataScopeFilterField?.SetValueWithoutNotify(PipelineBlackboardScopeFilter.All);
                m_GraphDataContextFilterField?.SetValueWithoutNotify(PipelineBlackboardContextFilter.AllVisible);
                m_GraphDataBlackboardFiltersExpanded = false;
            }

            if (sourceFilter == GraphDataCatalogSourceFilter.Blackboard)
                m_GraphDataBlackboardFiltersExpanded = true;

            RefreshGraphDataFilterPresentation();
            RequestGraphDataRefresh();
        }
        protected virtual void ToggleGraphDataBlackboardFilters()
        {
            if (m_GraphDataSourceFilter == GraphDataCatalogSourceFilter.Input)
                return;

            m_GraphDataBlackboardFiltersExpanded = !m_GraphDataBlackboardFiltersExpanded;
            RefreshGraphDataFilterPresentation();
        }
        protected virtual void OnGraphDataBlackboardFiltersChanged()
        {
            RefreshGraphDataFilterPresentation();
            RequestGraphDataRefresh();
        }
        protected virtual void RefreshGraphDataFilterPresentation()
        {
            bool blackboardFilterAvailable = m_GraphDataSourceFilter != GraphDataCatalogSourceFilter.Input;
            bool hasBlackboardFilters =
                (m_GraphDataScopeFilterField?.value is PipelineBlackboardScopeFilter scope && scope != PipelineBlackboardScopeFilter.All) ||
                (m_GraphDataContextFilterField?.value is PipelineBlackboardContextFilter context && context != PipelineBlackboardContextFilter.AllVisible);

            m_GraphDataAllSourceButton.EnableInClassList("selected", m_GraphDataSourceFilter == GraphDataCatalogSourceFilter.All);
            m_GraphDataInputSourceButton.EnableInClassList("selected", m_GraphDataSourceFilter == GraphDataCatalogSourceFilter.Input);
            m_GraphDataBlackboardSourceButton.EnableInClassList("selected", m_GraphDataSourceFilter == GraphDataCatalogSourceFilter.Blackboard);
            m_GraphDataBlackboardFilterButton.style.display = blackboardFilterAvailable ? DisplayStyle.Flex : DisplayStyle.None;
            m_GraphDataBlackboardFilterButton.EnableInClassList("selected", hasBlackboardFilters);
            m_GraphDataBlackboardFilterButton.tooltip = m_GraphDataBlackboardFiltersExpanded
                ? "Hide blackboard filters"
                : "Show blackboard filters";
            m_GraphDataBlackboardFilterPanel.style.display = blackboardFilterAvailable && m_GraphDataBlackboardFiltersExpanded
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        protected virtual void ToggleGraphDataCreation()
        {
            bool show = m_GraphDataCreationBar.resolvedStyle.display == DisplayStyle.None;
            m_GraphDataCreationBar.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (show)
                m_GraphDataNameField.Focus();
        }

        protected virtual void HideGraphDataCreation()
        {
            m_GraphDataCreationBar.style.display = DisplayStyle.None;
            m_GraphDataNameField.SetValueWithoutNotify(string.Empty);
        }

        protected virtual void RefreshGraphDataCreationOptions()
        {
            IGraphDataCatalogCreationSource source = CreateGraphDataSources().OfType<IGraphDataCatalogCreationSource>().FirstOrDefault();
            m_GraphDataScopeOptions = source?.GetScopeOptions(m_GraphDataContext) ?? Array.Empty<GraphDataCatalogCreationOption>();
            m_GraphDataTypeOptions = source?.GetTypeOptions(m_GraphDataContext) ?? Array.Empty<GraphDataCatalogCreationOption>();

            m_GraphDataScopeField.choices = m_GraphDataScopeOptions.Select(i => i.DisplayName).ToList();
            m_GraphDataTypeField.choices = m_GraphDataTypeOptions.Select(i => i.DisplayName).ToList();
            if (m_GraphDataScopeField.choices.Count > 0 && !m_GraphDataScopeField.choices.Contains(m_GraphDataScopeField.value))
                m_GraphDataScopeField.SetValueWithoutNotify(m_GraphDataScopeField.choices[0]);
            if (m_GraphDataTypeField.choices.Count > 0 && !m_GraphDataTypeField.choices.Contains(m_GraphDataTypeField.value))
                m_GraphDataTypeField.SetValueWithoutNotify(m_GraphDataTypeField.choices[0]);

            bool canCreate = source != null && m_GraphDataScopeOptions.Count > 0 && m_GraphDataTypeOptions.Count > 0;
            m_GraphDataAddButton.SetEnabled(canCreate);
            m_GraphDataCreateButton.SetEnabled(canCreate);
            if (!canCreate)
                HideGraphDataCreation();
        }

        protected virtual void CreateGraphDataDeclaration()
        {
            IGraphDataCatalogCreationSource source = CreateGraphDataSources().OfType<IGraphDataCatalogCreationSource>().FirstOrDefault();
            GraphDataCatalogCreationOption scope = m_GraphDataScopeOptions.FirstOrDefault(i => i.DisplayName == m_GraphDataScopeField.value);
            GraphDataCatalogCreationOption type = m_GraphDataTypeOptions.FirstOrDefault(i => i.DisplayName == m_GraphDataTypeField.value);
            if (source == null || scope == null || type == null)
            {
                ReportGraphDataError("Blackboard creation options are unavailable for the current graph.");
                return;
            }

            GraphDataCatalogCreateRequest request = new GraphDataCatalogCreateRequest(
                m_GraphDataNameField.value,
                scope.Id,
                type.Id);
            if (!source.TryCreate(request, m_GraphDataContext, out string error))
            {
                ReportGraphDataError(error);
                return;
            }

            HideGraphDataCreation();
            RebuildGraphDataCatalog();
        }

        protected virtual void AddGraphDataMessage(string text, bool error = false)
        {
            Label label = new Label(text);
            label.AddToClassList("graph-data-message");
            if (error)
                label.AddToClassList("graph-data-error");
            m_GraphDataCatalogContainer.Add(label);
        }

        protected virtual void ReportGraphDataError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return;

            Debug.LogError($"Graph Data Catalog: {error}");
            Label label = new Label(error);
            label.AddToClassList("graph-data-message");
            label.AddToClassList("graph-data-error");
            m_GraphDataCatalogContainer.Insert(0, label);
        }
    }
}
