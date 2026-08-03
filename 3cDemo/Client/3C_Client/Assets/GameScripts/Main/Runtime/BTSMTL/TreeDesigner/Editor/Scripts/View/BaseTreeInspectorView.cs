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
    internal sealed class TreeSelectionInspectorController
    {
        readonly BaseTreeInspectorView m_View;
        readonly Dictionary<FieldInfo, object> m_ValueMap = new Dictionary<FieldInfo, object>();

        public TreeSelectionInspectorController(BaseTreeInspectorView view)
        {
            m_View = view;
        }

        public bool HasSelection { get; private set; }

        public void Clear()
        {
            HasSelection = false;
            m_View.SelectionInspectorContainer.Clear();
        }

        public void Populate(IEnumerable<GraphSelectable> selection)
        {
            List<GraphSelectable> selected = selection?.Where(i => i != null).ToList() ?? new List<GraphSelectable>();
            if (selected.Count == 0)
            {
                HasSelection = false;
                PopulateGraphAuthoringSettings();
                return;
            }

            HasSelection = true;
            m_View.SelectionInspectorContainer.Clear();

            if (selected.Count == 1 && selected[0] is BaseEdgeView edgeView)
            {
                if (edgeView.IsStateMachineTransitionEdge)
                {
                    PopulateTransitionEdge(edgeView);
                    m_View.ShowSelectionTabInternal();
                    return;
                }

                if (edgeView.IsBTConditionEdge)
                {
                    PopulateBTConditionEdge(edgeView);
                    m_View.ShowSelectionTabInternal();
                    return;
                }

                AddSelectionMessage("Edge");
                m_View.ShowSelectionTabInternal();
                return;
            }

            if (selected.Count == 1 && selected[0] is BaseNodeView nodeView)
            {
                PopulateNode(nodeView);
                m_View.ShowSelectionTabInternal();
                return;
            }

            AddSelectionMessage($"{selected.Count} Selected");
            m_View.ShowSelectionTabInternal();
        }
        public void PopulateGraphAuthoringSettings()
        {
            m_View.SelectionInspectorContainer.Clear();
            AddInspectorTitle("Graph Settings");

            VisualElement settingsContainer = new VisualElement();
            settingsContainer.AddToClassList("graph-settings-container");
            BaseTreeInspector.PopulateAuthoringProperties(m_View.Tree, settingsContainer, m_ValueMap);
            if (settingsContainer.childCount == 0)
            {
                AddSelectionMessage("No editable graph settings.");
                return;
            }

            m_View.SelectionInspectorContainer.Add(settingsContainer);
        }
        void PopulateTransitionEdge(BaseEdgeView edgeView)
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
            m_View.SelectionInspectorContainer.Add(priorityField);

            PopulateConditionRuleControls(edgeView, edge);
            AddInspectorRow("Status", string.IsNullOrEmpty(edgeView.EdgeSummary) ? "Unconditional" : edgeView.EdgeSummary);
        }

        void PopulateBTConditionEdge(BaseEdgeView edgeView)
        {
            BaseEdge edge = edgeView.Edge;
            AddInspectorTitle("BT Edge");
            AddInspectorRow("From", NodeLabel(edge.StartNode));
            AddInspectorRow("To", NodeLabel(edge.EndNode));

            EnumField abortPolicyField = new EnumField("Abort Policy", edge.AbortPolicy);
            abortPolicyField.RegisterValueChangedCallback(evt =>
            {
                edgeView.SetAbortPolicy((BTAbortPolicy)evt.newValue);
                Populate(new GraphSelectable[] { edgeView });
            });
            abortPolicyField.AddToClassList("selection-inspector-field");
            m_View.SelectionInspectorContainer.Add(abortPolicyField);

            PopulateConditionRuleControls(edgeView, edge);
            AddInspectorRow("Status", string.IsNullOrEmpty(edgeView.EdgeSummary) ? "Unconditional" : edgeView.EdgeSummary);
        }

        void PopulateConditionRuleControls(BaseEdgeView edgeView, BaseEdge edge)
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
                Populate(new GraphSelectable[] { edgeView });
            });
            ruleGraphField.AddToClassList("selection-inspector-field");
            m_View.SelectionInspectorContainer.Add(ruleGraphField);

            VisualElement buttonRow = new VisualElement();
            buttonRow.AddToClassList("selection-inspector-button-row");

            Button openButton = new Button(() => edgeView.OpenConditionRuleGraph()) { text = "Open Rule" };
            Button extractButton = new Button(() =>
            {
                edgeView.ExtractSharedConditionRuleGraph();
                Populate(new GraphSelectable[] { edgeView });
            }) { text = "Extract Shared" };
            Button useInlineButton = new Button(() =>
            {
                edgeView.UseInlineConditionRuleGraph();
                Populate(new GraphSelectable[] { edgeView });
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
            m_View.SelectionInspectorContainer.Add(buttonRow);
        }

        void PopulateNode(BaseNodeView nodeView)
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

            if (m_View.TryPopulateSharedNode(nodeView, node))
                return;

            AddNodeIdentity(nodeView, node);
            AuthoringPageOpenRegistry.PopulateInspector(m_View, nodeView, node);
        }
        public void RefreshNodeSelection(BaseNodeView nodeView)
        {
            Populate(new GraphSelectable[] { nodeView });
        }

        void PopulateStateMachineNode(BaseNodeView nodeView, StateMachineNode node)
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
                        Populate(new GraphSelectable[] { nodeView });
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
                    Populate(new GraphSelectable[] { nodeView });
                },
                () =>
                {
                    node.ApplyModify("Use Local State Machine Graph", () =>
                    {
                        module.SetInlineGraph(StateMachineNode.CreateDefaultGraph());
                    });
                    nodeView.Refresh();
                    Populate(new GraphSelectable[] { nodeView });
                });
        }

        void PopulateStateNode(BaseNodeView nodeView, StateNode node)
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
                        Populate(new GraphSelectable[] { nodeView });
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
                    Populate(new GraphSelectable[] { nodeView });
                },
                () =>
                {
                    node.ApplyModify("Use Local State Behavior Graph", () =>
                    {
                        module.SetInlineSubTree(StateNode.CreateDefaultStateBehaviorGraph());
                    });
                    nodeView.Refresh();
                    Populate(new GraphSelectable[] { nodeView });
                });
        }

        void AddNodeIdentity(BaseNodeView nodeView, BaseNode node)
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

                nodeView.SetDisplayName(value);
                titleLabel.text = NodeLabel(node);
            });
            displayNameField.AddToClassList("selection-inspector-field");
            m_View.SelectionInspectorContainer.Add(displayNameField);
            AddInspectorRow("Type", node.NodeTypeDisplayName);
        }

        void AddSharedGraphField(string label, BaseTreeAsset value, Func<BaseTreeAsset, bool> validate, Action<BaseTreeAsset> apply)
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
            m_View.SelectionInspectorContainer.Add(field);
        }

        void AddGraphReferenceButtons(BaseNodeView nodeView, BaseNode node, NodeGraphReference reference, BaseTree inlineGraph, Action extractShared, Action useLocalDefault)
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
            m_View.SelectionInspectorContainer.Add(buttonRow);
        }

        string GraphOwnershipLabel(BaseTree inlineGraph, BaseTreeAsset sharedAsset)
        {
            if (sharedAsset)
                return "Shared Asset";
            return inlineGraph ? "Inline" : "Missing";
        }

        bool CanCreateSharedGraphAsset(BaseNode node)
        {
            string ownerPath = AssetDatabase.GetAssetPath(node?.Owner?.SerializedOwner);
            return !string.IsNullOrEmpty(ownerPath);
        }

        BaseTreeAsset ExtractSharedGraphAsset(BaseNode node, BaseTree graph, string folderSuffix)
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

        string SanitizeFileName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "Graph";

            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value;
        }

        Label AddInspectorTitle(string text)
        {
            Label label = new Label(text);
            label.AddToClassList("selection-inspector-title");
            m_View.SelectionInspectorContainer.Add(label);
            return label;
        }

        void AddInspectorRow(string label, string value)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("selection-inspector-row");
            Label labelElement = new Label(label);
            labelElement.AddToClassList("selection-inspector-row-label");
            Label valueElement = new Label(value);
            valueElement.AddToClassList("selection-inspector-row-value");
            row.Add(labelElement);
            row.Add(valueElement);
            m_View.SelectionInspectorContainer.Add(row);
        }

        void AddSelectionMessage(string text)
        {
            Label label = new Label(text);
            label.AddToClassList("selection-inspector-message");
            m_View.SelectionInspectorContainer.Add(label);
        }

        static string NodeLabel(BaseNode node)
        {
            return node == null ? "None" : node.ResolvedDisplayName;
        }
    }

    public abstract class GraphAuthoringDetailsHostView : VisualElement
    {
        protected GraphAuthoringDetailsHostView(bool startsHidden)
        {
            VisualTreeAsset template = Resources.Load<VisualTreeAsset>(
                "VisualTree/BaseTreeInspectorInside");
            if (!template)
                throw new InvalidOperationException(
                    "Graph authoring details visual tree is missing.");
            template.CloneTree(this);
            AddToClassList("treeInspector");
            style.display = startsHidden
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            DetailsPage = this.Q("selection-inspector-page") ??
                throw new InvalidOperationException(
                    "Graph authoring details page is missing.");
            DetailsContent =
                this.Q("selection-inspector-container") ??
                throw new InvalidOperationException(
                    "Graph authoring details content is missing.");
            DetailsPage.style.display = DisplayStyle.Flex;
        }

        protected VisualElement DetailsPage { get; }
        protected VisualElement DetailsContent { get; }
    }

    public abstract class GraphAuthoringNavigatorHostView : VisualElement
    {
        protected GraphAuthoringNavigatorHostView(
            string visualTreeName)
        {
            VisualTreeAsset template =
                Resources.Load<VisualTreeAsset>(
                    $"VisualTree/{visualTreeName}");
            if (!template)
                throw new InvalidOperationException(
                    "Graph authoring navigator visual tree is missing.");
            template.CloneTree(this);
            style.flexGrow = 1f;
        }
    }

    public sealed class BaseTreeNavigatorView :
        GraphAuthoringNavigatorHostView
    {
        public BaseTreeNavigatorView() :
            base("BaseTreeNavigator")
        {
        }
    }

    public class BaseTreeInspectorView :
        GraphAuthoringDetailsHostView
    {
        public new class UxmlFactory : UxmlFactory<BaseTreeInspectorView, UxmlTraits> { }

        protected BaseTree m_Tree;
        public BaseTree Tree => m_Tree;
        readonly TreeSelectionInspectorController m_SelectionController;
        readonly GraphAuthoringDetailsPresenter m_SharedDetailsPresenter;
        GraphDataCatalogController m_DataCatalogController;

        protected VisualElement m_SelectionPage;
        protected VisualElement m_SelectionInspectorContainer;

        public BaseTreeInspectorView() :
            base(true)
        {
            m_SelectionController = new TreeSelectionInspectorController(this);
            m_SelectionPage = DetailsPage;
            m_SelectionInspectorContainer = DetailsContent;
            m_SharedDetailsPresenter = new GraphAuthoringDetailsPresenter(m_SelectionInspectorContainer);
        }

        public void BindSharedAuthoring(
            BtsmtlSharedAuthoringWorkspaceBinding binding,
            Action<GraphAuthoringDetailsCommandRequest> commandHandler)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
            m_SharedDetailsPresenter.Bind(
                new GraphAuthoringDetailsBinding(
                    binding.Document,
                    binding.Capabilities,
                    binding.Mutation,
                    binding.Details,
                    commandHandler,
                    true));
        }

        internal bool TryPopulateSharedNode(BaseNodeView nodeView, BaseNode node)
        {
            if (node == null)
                return false;
            m_SharedDetailsPresenter.Inspect(new GraphAuthoringSelection(GraphAuthoringSelectionKind.Node, new GraphAuthoringElementId(node.GUID)));
            AuthoringPageOpenRegistry.PopulateInspector(this, nodeView, node);
            return true;
        }

        internal void BindNavigator(BaseTreeNavigatorView navigator, GraphDataCatalogViewState viewState)
        {
            if (navigator == null)
                throw new ArgumentNullException(nameof(navigator));
            if (m_DataCatalogController != null)
                throw new InvalidOperationException("Base Tree Navigator is already bound.");
            m_DataCatalogController = new GraphDataCatalogController(navigator, viewState);
            navigator.Q<EnumField>("graph-data-scope-filter")?.RegisterValueChangedCallback(evt =>
                viewState.ScopeFilter = evt.newValue is PipelineBlackboardScopeFilter value
                    ? value
                    : PipelineBlackboardScopeFilter.All);
            navigator.Q<EnumField>("graph-data-context-filter")?.RegisterValueChangedCallback(evt =>
                viewState.ContextFilter = evt.newValue is PipelineBlackboardContextFilter value
                    ? value
                    : PipelineBlackboardContextFilter.AllVisible);
            navigator.Q<Button>("graph-data-source-all-button").clicked += () => viewState.SourceFilter = GraphDataCatalogSourceFilter.All;
            navigator.Q<Button>("graph-data-source-input-button").clicked += () =>
            {
                viewState.SourceFilter = GraphDataCatalogSourceFilter.Input;
                viewState.ScopeFilter = PipelineBlackboardScopeFilter.All;
                viewState.ContextFilter = PipelineBlackboardContextFilter.AllVisible;
                viewState.BlackboardFiltersExpanded = false;
            };
            navigator.Q<Button>("graph-data-source-blackboard-button").clicked += () =>
            {
                viewState.SourceFilter = GraphDataCatalogSourceFilter.Blackboard;
                viewState.BlackboardFiltersExpanded = true;
            };
            navigator.Q<Button>("graph-data-blackboard-filter-button").clicked += () => navigator.schedule.Execute(() =>
                viewState.BlackboardFiltersExpanded = navigator.Q("graph-data-blackboard-filter-panel").resolvedStyle.display == DisplayStyle.Flex);
        }

        public virtual void SetAuthoringContext(object authoringContext)
        {
            m_DataCatalogController?.SetAuthoringContext(authoringContext);
        }

        public virtual void SetVisibleBlackboardSources(IEnumerable<BaseTree> trees)
        {
            m_DataCatalogController?.SetVisibleBlackboardSources(trees);
        }

        public IEnumerable<BaseExposedProperty> VisibleBlackboardDeclarations =>
            m_DataCatalogController?.VisibleBlackboardDeclarations ?? Array.Empty<BaseExposedProperty>();

        public bool FocusBlackboardDeclaration(string graphAuthoringId, string declarationId)
        {
            return m_DataCatalogController != null &&
                   m_DataCatalogController.FocusBlackboardDeclaration(graphAuthoringId, declarationId);
        }

        public virtual void PopulateView(BaseTree tree)
        {
            ClearView();
            m_Tree = tree;
            m_SelectionController.PopulateGraphAuthoringSettings();
            m_DataCatalogController?.Bind(tree);

            style.display = DisplayStyle.Flex;
        }
        public virtual void ClearView()
        {
            m_DataCatalogController?.Clear();
            m_SelectionController.Clear();
            m_Tree = null;
        }
        public virtual void PopulateSelection(IEnumerable<GraphSelectable> selection)
        {
            m_SelectionController.Populate(selection);
        }

        public VisualElement SelectionInspectorContainer => m_SelectionInspectorContainer;

        public void RefreshNodeSelection(BaseNodeView nodeView)
        {
            m_SelectionController.RefreshNodeSelection(nodeView);
        }

        protected virtual void ShowSelectionTab()
        {
            if (!m_SelectionController.HasSelection)
                m_SelectionController.PopulateGraphAuthoringSettings();
            m_SelectionPage.style.display = DisplayStyle.Flex;
        }

        internal void ShowSelectionTabInternal()
        {
            ShowSelectionTab();
        }
   }
}
