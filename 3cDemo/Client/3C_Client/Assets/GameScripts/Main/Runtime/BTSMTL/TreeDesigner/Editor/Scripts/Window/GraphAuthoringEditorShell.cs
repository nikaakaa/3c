using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace TreeDesigner.Editor
{
    public readonly struct GraphAuthoringNodeCatalogEntry
    {
        public GraphAuthoringNodeCatalogEntry(string path, string typeId)
        {
            Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Graph node path is missing.", nameof(path)) : path;
            TypeId = string.IsNullOrWhiteSpace(typeId) ? throw new ArgumentException("Graph node type identity is missing.", nameof(typeId)) : typeId;
        }

        public string Path { get; }
        public string TypeId { get; }
    }

    public readonly struct GraphAuthoringBreadcrumbEntry
    {
        public GraphAuthoringBreadcrumbEntry(string displayName, string tooltip = null)
        {
            DisplayName = displayName ?? string.Empty;
            Tooltip = tooltip ?? string.Empty;
        }

        public string DisplayName { get; }
        public string Tooltip { get; }
    }

    public interface IGraphAuthoringDocument
    {
        string DomainId { get; }
        string DocumentId { get; }
        string DisplayName { get; }
        string ContentRevision { get; }
        UnityEngine.Object SerializedOwner { get; }
    }

    public interface IGraphAuthoringNodeCatalog
    {
        IReadOnlyList<GraphAuthoringNodeCatalogEntry> GetEntries(IGraphAuthoringDocument document);
    }

    public interface IGraphAuthoringPortPolicy
    {
        bool CanConnect(IGraphAuthoringDocument document, Port startPort, Port endPort);
    }

    public interface IGraphAuthoringMutationAdapter
    {
        bool ReadOnly { get; }
        void CreateNode(IGraphAuthoringDocument document, string typeId, Vector2 graphPosition);
        GraphViewChange ApplyGraphViewChange(IGraphAuthoringDocument document, GraphViewChange change);
        string SerializeSelection(IGraphAuthoringDocument document, IEnumerable<GraphElement> elements);
        bool CanPaste(IGraphAuthoringDocument document, string payload);
        void Paste(IGraphAuthoringDocument document, string operationName, string payload);
        void Reload(IGraphAuthoringDocument document);
    }

    public interface IGraphAuthoringInspectorAdapter
    {
        VisualElement View { get; }
        void Bind(IGraphAuthoringDocument document);
        void Inspect(IReadOnlyList<ISelectable> selection);
        void Clear();
    }

    public interface IGraphAuthoringDiagnosticsAdapter
    {
        void Bind(IGraphAuthoringDocument document, GraphView graphView, VisualElement toolbar);
        void Refresh();
        void Clear();
    }

    public enum GraphAuthoringToolbarCommandKind : byte
    {
        Lightweight = 0,
        ExplicitOperation = 1
    }

    public readonly struct GraphAuthoringToolbarCommandDescriptor
    {
        public GraphAuthoringToolbarCommandDescriptor(
            string commandId,
            string label,
            GraphAuthoringToolbarCommandKind kind,
            Action execute)
        {
            CommandId = string.IsNullOrWhiteSpace(commandId) ? throw new ArgumentException("Toolbar command identity is missing.", nameof(commandId)) : commandId;
            Label = string.IsNullOrWhiteSpace(label) ? throw new ArgumentException("Toolbar command label is missing.", nameof(label)) : label;
            Kind = kind;
            Execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public string CommandId { get; }
        public string Label { get; }
        public GraphAuthoringToolbarCommandKind Kind { get; }
        public Action Execute { get; }
    }

    public sealed class GraphAuthoringWorkspaceDescriptor
    {
        public GraphAuthoringWorkspaceDescriptor(
            GraphAuthoringWorkspaceRegionDescriptor navigator,
            GraphAuthoringWorkspaceRegionDescriptor details,
            GraphAuthoringWorkspaceRegionDescriptor bottomDock,
            IReadOnlyList<GraphAuthoringToolbarCommandDescriptor> commands = null)
        {
            Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
            Details = details ?? throw new ArgumentNullException(nameof(details));
            BottomDock = bottomDock ?? throw new ArgumentNullException(nameof(bottomDock));
            Commands = commands ?? Array.Empty<GraphAuthoringToolbarCommandDescriptor>();
        }

        public GraphAuthoringWorkspaceRegionDescriptor Navigator { get; }
        public GraphAuthoringWorkspaceRegionDescriptor Details { get; }
        public GraphAuthoringWorkspaceRegionDescriptor BottomDock { get; }
        public IReadOnlyList<GraphAuthoringToolbarCommandDescriptor> Commands { get; }
    }

    public sealed class GraphAuthoringWorkspaceRegionDescriptor
    {
        public GraphAuthoringWorkspaceRegionDescriptor(
            string title,
            bool visible,
            float minimumDimension,
            float defaultDimension,
            bool defaultCollapsed = false)
        {
            if (minimumDimension <= 0f)
                throw new ArgumentOutOfRangeException(nameof(minimumDimension));
            if (defaultDimension < minimumDimension)
                throw new ArgumentOutOfRangeException(nameof(defaultDimension));
            Title = string.IsNullOrWhiteSpace(title) ? "Region" : title;
            Visible = visible;
            MinimumDimension = minimumDimension;
            DefaultDimension = defaultDimension;
            DefaultCollapsed = defaultCollapsed;
        }

        public string Title { get; }
        public bool Visible { get; }
        public float MinimumDimension { get; }
        public float DefaultDimension { get; }
        public bool DefaultCollapsed { get; }
    }

    [Serializable]
    public sealed class GraphAuthoringWorkspaceLayoutState
    {
        public bool initialized;
        public float navigatorWidth;
        public bool navigatorCollapsed;
        public float detailsWidth;
        public bool detailsCollapsed;
        public string detailsPageId;
        public float bottomDockHeight;
        public bool bottomDockCollapsed;
        public string bottomDockPageId;
    }

    public interface IGraphAuthoringWorkspaceRegionAdapter
    {
        VisualElement View { get; }
        void Bind(IGraphAuthoringDocument document);
        void Refresh();
        void Clear();
    }

    public interface IGraphAuthoringWorkspacePageAdapter
    {
        string ActivePageId { get; }
        void RestorePage(string pageId);
    }

    public sealed class GraphAuthoringDomainAdapters
    {
        public GraphAuthoringDomainAdapters(
            IGraphAuthoringDocument document,
            IGraphAuthoringNodeCatalog nodeCatalog,
            IGraphAuthoringPortPolicy portPolicy,
            IGraphAuthoringMutationAdapter mutation,
            IGraphAuthoringInspectorAdapter inspector,
            IGraphAuthoringDiagnosticsAdapter diagnostics,
            GraphAuthoringWorkspaceDescriptor workspace = null,
            IGraphAuthoringWorkspaceRegionAdapter navigator = null,
            IGraphAuthoringWorkspaceRegionAdapter bottomDock = null)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            NodeCatalog = nodeCatalog ?? throw new ArgumentNullException(nameof(nodeCatalog));
            PortPolicy = portPolicy ?? throw new ArgumentNullException(nameof(portPolicy));
            Mutation = mutation ?? throw new ArgumentNullException(nameof(mutation));
            Inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            Workspace = workspace ?? new GraphAuthoringWorkspaceDescriptor(
                new GraphAuthoringWorkspaceRegionDescriptor("Navigator", navigator != null, 220f, 240f),
                new GraphAuthoringWorkspaceRegionDescriptor("Details", true, 220f, 340f),
                new GraphAuthoringWorkspaceRegionDescriptor("Results", bottomDock != null, 120f, 220f));
            Navigator = navigator;
            BottomDock = bottomDock;
        }

        public IGraphAuthoringDocument Document { get; }
        public IGraphAuthoringNodeCatalog NodeCatalog { get; }
        public IGraphAuthoringPortPolicy PortPolicy { get; }
        public IGraphAuthoringMutationAdapter Mutation { get; }
        public IGraphAuthoringInspectorAdapter Inspector { get; }
        public IGraphAuthoringDiagnosticsAdapter Diagnostics { get; }
        public GraphAuthoringWorkspaceDescriptor Workspace { get; }
        public IGraphAuthoringWorkspaceRegionAdapter Navigator { get; }
        public IGraphAuthoringWorkspaceRegionAdapter BottomDock { get; }
    }

    public interface IGraphAuthoringDomainView
    {
        void BindAdapters(
            IGraphAuthoringDocument document,
            IGraphAuthoringPortPolicy portPolicy,
            IGraphAuthoringMutationAdapter mutation);
    }

    [Serializable]
    sealed class GraphAuthoringClipboardEnvelope
    {
        public string domainId;
        public string payload;
    }

    sealed class GraphAuthoringNodeSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        GraphAuthoringEditorShell m_Shell;
        Texture2D m_IndentationIcon;

        public void Initialize(GraphAuthoringEditorShell shell)
        {
            m_Shell = shell;
            m_IndentationIcon = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            m_IndentationIcon.SetPixel(0, 0, Color.clear);
            m_IndentationIcon.Apply();
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var result = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Nodes"))
            };
            IReadOnlyList<GraphAuthoringNodeCatalogEntry> entries = m_Shell.GetNodeCatalogEntries();
            var groups = new HashSet<string>(StringComparer.Ordinal);
            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                GraphAuthoringNodeCatalogEntry entry = entries[entryIndex];
                string[] parts = entry.Path.Split('/');
                string groupPath = string.Empty;
                for (int partIndex = 0; partIndex < parts.Length - 1; partIndex++)
                {
                    groupPath = string.IsNullOrEmpty(groupPath) ? parts[partIndex] : groupPath + "/" + parts[partIndex];
                    if (groups.Add(groupPath))
                        result.Add(new SearchTreeGroupEntry(new GUIContent(parts[partIndex]), partIndex + 1));
                }
                result.Add(new SearchTreeEntry(new GUIContent(parts[parts.Length - 1], m_IndentationIcon))
                {
                    level = parts.Length,
                    userData = entry
                });
            }
            return result;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (!(searchTreeEntry.userData is GraphAuthoringNodeCatalogEntry entry))
                return false;
            m_Shell.CreateNode(entry.TypeId, context.screenMousePosition);
            return true;
        }

        void OnDestroy()
        {
            if (m_IndentationIcon)
                DestroyImmediate(m_IndentationIcon);
        }
    }

    public abstract class GraphAuthoringEditorShell : EditorWindow
    {
        [SerializeField]
        GraphAuthoringWorkspaceLayoutState m_WorkspaceLayoutState = new GraphAuthoringWorkspaceLayoutState();

        protected VisualElement m_WorkspaceToolbar;
        protected VisualElement m_NavigatorHost;
        protected VisualElement m_GraphCanvasHost;
        protected VisualElement m_DetailsHost;
        protected VisualElement m_BottomDockHost;
        protected VisualElement m_NavigationToolbar;
        protected Label m_TreeTitle;

        GraphView m_GraphView;
        GraphAuthoringDomainAdapters m_Adapters;
        GraphAuthoringNodeSearchProvider m_NodeSearchProvider;
        IVisualElementScheduledItem m_SelectionWatcher;
        int[] m_LastSelection = Array.Empty<int>();
        Button m_NavigationBackButton;
        VisualElement m_BreadcrumbContainer;
        Action m_NavigateBack;
        TwoPaneSplitView m_NavigatorSplit;
        TwoPaneSplitView m_DetailsSplit;
        TwoPaneSplitView m_BottomDockSplit;
        VisualElement m_NavigatorRegion;
        VisualElement m_DetailsRegion;
        VisualElement m_BottomDockRegion;
        Button m_NavigatorToggle;
        Button m_DetailsToggle;
        Button m_BottomDockToggle;
        bool m_NarrowNavigatorCollapsed;
        bool m_NarrowDetailsCollapsed;
        bool m_NarrowBottomDockCollapsed;
        bool m_RestoringLayout;

        protected GraphView GraphAuthoringView => m_GraphView;
        protected GraphAuthoringDomainAdapters GraphAuthoringAdapters => m_Adapters;

        protected abstract GraphView CreateGraphAuthoringView();
        protected abstract VisualElement CreateGraphAuthoringInspectorView();
        protected abstract GraphAuthoringDomainAdapters CreateGraphAuthoringAdapters();

        public virtual void CreateGUI()
        {
            if (m_WorkspaceLayoutState == null)
                m_WorkspaceLayoutState = new GraphAuthoringWorkspaceLayoutState();
            rootVisualElement.Clear();
            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>("VisualTree/BaseTreeWindow");
            if (!visualTree)
                throw new InvalidOperationException("Graph Authoring Editor Shell visual tree is missing.");
            visualTree.CloneTree(rootVisualElement);

            m_WorkspaceToolbar = RequireHost("workspace-toolbar-content");
            m_NavigatorHost = RequireHost("workspace-navigator-content");
            m_GraphCanvasHost = RequireHost("workspace-graph-content");
            m_DetailsHost = RequireHost("workspace-details-content");
            m_BottomDockHost = RequireHost("workspace-bottom-content");
            m_NavigatorSplit = RequireHost("workspace-horizontal") as TwoPaneSplitView;
            m_BottomDockSplit = RequireHost("workspace-content-vertical") as TwoPaneSplitView;
            m_DetailsSplit = RequireHost("workspace-main-horizontal") as TwoPaneSplitView;
            m_NavigatorRegion = RequireHost("workspace-navigator");
            m_DetailsRegion = RequireHost("workspace-details");
            m_BottomDockRegion = RequireHost("workspace-bottom-dock");
            m_NavigationToolbar = rootVisualElement.Q("tree-navigation-toolbar");
            m_NavigationBackButton = rootVisualElement.Q<Button>("tree-navigation-back-button");
            m_BreadcrumbContainer = rootVisualElement.Q("tree-navigation-breadcrumb");
            m_NavigationBackButton.clicked += NavigateBack;
            m_GraphView = CreateGraphAuthoringView() ?? throw new InvalidOperationException("Graph Authoring domain did not create a GraphView.");
            m_GraphView.name = "tree-view";
            m_GraphCanvasHost.Add(m_GraphView);

            m_TreeTitle = new Label { name = "tree-title" };
            rootVisualElement.Add(m_TreeTitle);
            VisualElement inspector = CreateGraphAuthoringInspectorView() ?? throw new InvalidOperationException("Graph Authoring domain did not create an Inspector view.");
            inspector.name = "tree-inspector";

            m_Adapters = CreateGraphAuthoringAdapters();
            if (!ReferenceEquals(m_Adapters.Inspector.View, inspector))
                throw new InvalidOperationException("Graph Authoring Inspector adapter must own the Shell Inspector view.");
            ConfigureWorkspace(inspector);
            if (m_GraphView is IGraphAuthoringDomainView domainView)
                domainView.BindAdapters(m_Adapters.Document, m_Adapters.PortPolicy, m_Adapters.Mutation);
            BindSearch();
            BindClipboard();
            m_Adapters.Inspector.Bind(m_Adapters.Document);
            m_Adapters.Navigator?.Bind(m_Adapters.Document);
            m_Adapters.BottomDock?.Bind(m_Adapters.Document);
            m_Adapters.Diagnostics.Bind(m_Adapters.Document, m_GraphView, m_WorkspaceToolbar);
            RestorePageState();
            rootVisualElement.schedule.Execute(InitializeLayout);
            m_SelectionWatcher = m_GraphView.schedule.Execute(PublishSelection).Every(100);
            Undo.undoRedoPerformed += HandleUndoRedo;
            OnGraphAuthoringShellCreated();
        }

        protected virtual void OnGraphAuthoringShellCreated() { }

        protected virtual void OnDisable()
        {
            CaptureLayoutState();
            CapturePageState();
            Undo.undoRedoPerformed -= HandleUndoRedo;
            m_SelectionWatcher?.Pause();
            m_Adapters?.Diagnostics.Clear();
            m_Adapters?.BottomDock?.Clear();
            m_Adapters?.Navigator?.Clear();
            m_Adapters?.Inspector.Clear();
            if (m_NavigationBackButton != null)
                m_NavigationBackButton.clicked -= NavigateBack;
            m_NavigateBack = null;
            m_NavigationBackButton = null;
            m_BreadcrumbContainer = null;
            rootVisualElement.UnregisterCallback<GeometryChangedEvent>(HandleWorkspaceGeometryChanged);
            if (m_NodeSearchProvider)
                DestroyImmediate(m_NodeSearchProvider);
            m_NodeSearchProvider = null;
            m_Adapters = null;
            m_GraphView = null;
        }

        protected void RebindGraphAuthoringDocument()
        {
            if (m_Adapters == null)
                return;
            m_LastSelection = Array.Empty<int>();
            m_Adapters.Inspector.Bind(m_Adapters.Document);
            m_Adapters.Navigator?.Bind(m_Adapters.Document);
            m_Adapters.BottomDock?.Bind(m_Adapters.Document);
            m_Adapters.Diagnostics.Bind(m_Adapters.Document, m_GraphView, m_WorkspaceToolbar);
            PublishSelection();
            m_Adapters.Diagnostics.Refresh();
            m_Adapters.Navigator?.Refresh();
            m_Adapters.BottomDock?.Refresh();
            if (m_TreeTitle != null)
                m_TreeTitle.text = m_Adapters.Document.DisplayName;
        }

        protected virtual void OnGraphAuthoringUndoRedo() { }

        void ConfigureWorkspace(VisualElement inspector)
        {
            GraphAuthoringWorkspaceDescriptor descriptor = m_Adapters.Workspace;
            rootVisualElement.Q<Label>("workspace-navigator-title").text = descriptor.Navigator.Title;
            rootVisualElement.Q<Label>("workspace-details-title").text = descriptor.Details.Title;
            rootVisualElement.Q<Label>("workspace-bottom-title").text = descriptor.BottomDock.Title;
            m_NavigatorRegion.style.minWidth = descriptor.Navigator.MinimumDimension;
            m_DetailsRegion.style.minWidth = descriptor.Details.MinimumDimension;
            m_BottomDockRegion.style.minHeight = descriptor.BottomDock.MinimumDimension;
            m_DetailsHost.Add(inspector);
            MountRegion(m_NavigatorHost, m_Adapters.Navigator, "No navigator is available for this graph domain.");
            MountRegion(m_BottomDockHost, m_Adapters.BottomDock, "No bottom panel is available for this graph domain.");
            m_NavigatorToggle = CreateRegionToggle("Navigator", ToggleNavigator);
            m_DetailsToggle = CreateRegionToggle("Details", ToggleDetails);
            m_BottomDockToggle = CreateRegionToggle("Bottom", ToggleBottomDock);
            m_NavigatorToggle.SetEnabled(descriptor.Navigator.Visible);
            m_DetailsToggle.SetEnabled(descriptor.Details.Visible);
            m_BottomDockToggle.SetEnabled(descriptor.BottomDock.Visible);
            for (int i = 0; i < descriptor.Commands.Count; i++)
            {
                GraphAuthoringToolbarCommandDescriptor command = descriptor.Commands[i];
                var button = new Button(command.Execute)
                {
                    name = $"workspace-command-{command.CommandId}",
                    text = command.Label
                };
                button.EnableInClassList("workspace-explicit-operation", command.Kind == GraphAuthoringToolbarCommandKind.ExplicitOperation);
                m_WorkspaceToolbar.Add(button);
            }
        }

        Button CreateRegionToggle(string label, Action clicked)
        {
            var button = new Button(clicked) { text = label };
            button.AddToClassList("workspace-region-toggle");
            m_WorkspaceToolbar.Add(button);
            return button;
        }

        void InitializeLayout()
        {
            if (m_Adapters == null || m_NavigatorSplit == null || m_DetailsSplit == null || m_BottomDockSplit == null)
                return;
            GraphAuthoringWorkspaceDescriptor descriptor = m_Adapters.Workspace;
            if (!m_WorkspaceLayoutState.initialized)
            {
                m_WorkspaceLayoutState.initialized = true;
                m_WorkspaceLayoutState.navigatorWidth = descriptor.Navigator.DefaultDimension;
                m_WorkspaceLayoutState.navigatorCollapsed = descriptor.Navigator.DefaultCollapsed;
                m_WorkspaceLayoutState.detailsWidth = descriptor.Details.DefaultDimension;
                m_WorkspaceLayoutState.detailsCollapsed = descriptor.Details.DefaultCollapsed;
                m_WorkspaceLayoutState.bottomDockHeight = descriptor.BottomDock.DefaultDimension;
                m_WorkspaceLayoutState.bottomDockCollapsed = descriptor.BottomDock.DefaultCollapsed;
            }
            m_RestoringLayout = true;
            m_NavigatorSplit.fixedPaneInitialDimension = Math.Max(descriptor.Navigator.MinimumDimension, m_WorkspaceLayoutState.navigatorWidth);
            m_DetailsSplit.fixedPaneInitialDimension = Math.Max(descriptor.Details.MinimumDimension, m_WorkspaceLayoutState.detailsWidth);
            m_BottomDockSplit.fixedPaneInitialDimension = Math.Max(descriptor.BottomDock.MinimumDimension, m_WorkspaceLayoutState.bottomDockHeight);
            m_RestoringLayout = false;
            rootVisualElement.RegisterCallback<GeometryChangedEvent>(HandleWorkspaceGeometryChanged);
            ApplyNarrowLayout(rootVisualElement.resolvedStyle.width, rootVisualElement.resolvedStyle.height);
            ApplyCollapseState();
        }

        void HandleWorkspaceGeometryChanged(GeometryChangedEvent evt)
        {
            if (m_RestoringLayout || m_Adapters == null)
                return;
            CaptureDimensions();
            ApplyNarrowLayout(evt.newRect.width, evt.newRect.height);
            ApplyCollapseState();
        }

        void ApplyNarrowLayout(float width, float height)
        {
            m_NarrowBottomDockCollapsed = height > 0f && height < 520f;
            m_NarrowNavigatorCollapsed = width > 0f && width < 900f;
            m_NarrowDetailsCollapsed = width > 0f && width < 620f;
        }

        void ToggleNavigator()
        {
            m_WorkspaceLayoutState.navigatorCollapsed = !m_WorkspaceLayoutState.navigatorCollapsed;
            ApplyCollapseState();
        }

        void ToggleDetails()
        {
            m_WorkspaceLayoutState.detailsCollapsed = !m_WorkspaceLayoutState.detailsCollapsed;
            ApplyCollapseState();
        }

        void ToggleBottomDock()
        {
            m_WorkspaceLayoutState.bottomDockCollapsed = !m_WorkspaceLayoutState.bottomDockCollapsed;
            ApplyCollapseState();
        }

        void ApplyCollapseState()
        {
            if (m_Adapters == null)
                return;
            GraphAuthoringWorkspaceDescriptor descriptor = m_Adapters.Workspace;
            SetCollapsed(m_NavigatorSplit, 0, !descriptor.Navigator.Visible || m_WorkspaceLayoutState.navigatorCollapsed || m_NarrowNavigatorCollapsed);
            SetCollapsed(m_DetailsSplit, 1, !descriptor.Details.Visible || m_WorkspaceLayoutState.detailsCollapsed || m_NarrowDetailsCollapsed);
            SetCollapsed(m_BottomDockSplit, 1, !descriptor.BottomDock.Visible || m_WorkspaceLayoutState.bottomDockCollapsed || m_NarrowBottomDockCollapsed);
            UpdateRegionToggle(m_NavigatorToggle, m_NavigatorRegion.resolvedStyle.display != DisplayStyle.None && !m_WorkspaceLayoutState.navigatorCollapsed && !m_NarrowNavigatorCollapsed);
            UpdateRegionToggle(m_DetailsToggle, m_DetailsRegion.resolvedStyle.display != DisplayStyle.None && !m_WorkspaceLayoutState.detailsCollapsed && !m_NarrowDetailsCollapsed);
            UpdateRegionToggle(m_BottomDockToggle, m_BottomDockRegion.resolvedStyle.display != DisplayStyle.None && !m_WorkspaceLayoutState.bottomDockCollapsed && !m_NarrowBottomDockCollapsed);
        }

        static void SetCollapsed(TwoPaneSplitView split, int childIndex, bool collapsed)
        {
            if (split == null)
                return;
            if (collapsed)
                split.CollapseChild(childIndex);
            else
                split.UnCollapse();
        }

        static void UpdateRegionToggle(Button button, bool expanded)
        {
            if (button == null)
                return;
            button.EnableInClassList("workspace-region-toggle-expanded", expanded);
        }

        void CaptureDimensions()
        {
            if (m_NavigatorSplit == null || m_DetailsSplit == null || m_BottomDockSplit == null)
                return;
            if (!m_WorkspaceLayoutState.navigatorCollapsed && !m_NarrowNavigatorCollapsed)
                m_WorkspaceLayoutState.navigatorWidth = m_NavigatorSplit.fixedPane?.resolvedStyle.width ?? m_WorkspaceLayoutState.navigatorWidth;
            if (!m_WorkspaceLayoutState.detailsCollapsed && !m_NarrowDetailsCollapsed)
                m_WorkspaceLayoutState.detailsWidth = m_DetailsSplit.fixedPane?.resolvedStyle.width ?? m_WorkspaceLayoutState.detailsWidth;
            if (!m_WorkspaceLayoutState.bottomDockCollapsed && !m_NarrowBottomDockCollapsed)
                m_WorkspaceLayoutState.bottomDockHeight = m_BottomDockSplit.fixedPane?.resolvedStyle.height ?? m_WorkspaceLayoutState.bottomDockHeight;
        }

        void CaptureLayoutState()
        {
            if (m_WorkspaceLayoutState == null)
                m_WorkspaceLayoutState = new GraphAuthoringWorkspaceLayoutState();
            CaptureDimensions();
        }

        void CapturePageState()
        {
            if (m_Adapters?.Inspector is IGraphAuthoringWorkspacePageAdapter details)
                m_WorkspaceLayoutState.detailsPageId = details.ActivePageId;
            if (m_Adapters?.BottomDock is IGraphAuthoringWorkspacePageAdapter bottom)
                m_WorkspaceLayoutState.bottomDockPageId = bottom.ActivePageId;
        }

        void RestorePageState()
        {
            if (m_Adapters?.Inspector is IGraphAuthoringWorkspacePageAdapter details)
                details.RestorePage(m_WorkspaceLayoutState.detailsPageId);
            if (m_Adapters?.BottomDock is IGraphAuthoringWorkspacePageAdapter bottom)
                bottom.RestorePage(m_WorkspaceLayoutState.bottomDockPageId);
        }

        static void MountRegion(
            VisualElement host,
            IGraphAuthoringWorkspaceRegionAdapter adapter,
            string emptyMessage)
        {
            host.Clear();
            if (adapter?.View != null)
            {
                host.Add(adapter.View);
                return;
            }
            var label = new Label(emptyMessage);
            label.AddToClassList("workspace-empty-state");
            host.Add(label);
        }

        VisualElement RequireHost(string name) =>
            rootVisualElement.Q(name) ?? throw new InvalidOperationException($"Graph Authoring Workspace host '{name}' is missing.");

        protected void BindGraphAuthoringNavigation(Action navigateBack)
        {
            m_NavigateBack = navigateBack;
        }

        protected void RenderGraphAuthoringNavigation(
            IReadOnlyList<GraphAuthoringBreadcrumbEntry> entries,
            Action<int> navigateTo)
        {
            if (m_NavigationBackButton == null || m_BreadcrumbContainer == null)
                return;
            int count = entries?.Count ?? 0;
            m_NavigationBackButton.SetEnabled(count > 1);
            m_BreadcrumbContainer.Clear();
            for (int index = 0; index < count; index++)
            {
                if (index > 0)
                {
                    Label separator = new Label("/");
                    separator.AddToClassList("tree-navigation-separator");
                    m_BreadcrumbContainer.Add(separator);
                }
                GraphAuthoringBreadcrumbEntry entry = entries[index];
                if (index == count - 1)
                {
                    Label label = new Label(entry.DisplayName) { tooltip = entry.Tooltip };
                    label.AddToClassList("tree-navigation-current-segment");
                    m_BreadcrumbContainer.Add(label);
                }
                else
                {
                    int targetIndex = index;
                    Button button = new Button(() => navigateTo?.Invoke(targetIndex))
                    {
                        text = entry.DisplayName,
                        tooltip = entry.Tooltip
                    };
                    button.AddToClassList("tree-navigation-segment");
                    m_BreadcrumbContainer.Add(button);
                }
            }
        }

        internal IReadOnlyList<GraphAuthoringNodeCatalogEntry> GetNodeCatalogEntries()
        {
            return m_Adapters?.NodeCatalog.GetEntries(m_Adapters.Document) ?? Array.Empty<GraphAuthoringNodeCatalogEntry>();
        }

        internal void CreateNode(string typeId, Vector2 screenPosition)
        {
            if (m_Adapters == null || m_Adapters.Mutation.ReadOnly)
                return;
            Vector2 windowPosition = screenPosition - position.position;
            Vector2 graphPosition = m_GraphView.contentViewContainer.WorldToLocal(windowPosition);
            m_Adapters.Mutation.CreateNode(m_Adapters.Document, typeId, graphPosition);
            RebindGraphAuthoringDocument();
        }

        void BindSearch()
        {
            m_NodeSearchProvider = CreateInstance<GraphAuthoringNodeSearchProvider>();
            m_NodeSearchProvider.Initialize(this);
            m_GraphView.nodeCreationRequest = context =>
            {
                if (!m_Adapters.Mutation.ReadOnly)
                    SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), m_NodeSearchProvider);
            };
        }

        void BindClipboard()
        {
            m_GraphView.serializeGraphElements = elements =>
            {
                string payload = m_Adapters.Mutation.SerializeSelection(m_Adapters.Document, elements);
                return JsonUtility.ToJson(new GraphAuthoringClipboardEnvelope
                {
                    domainId = m_Adapters.Document.DomainId,
                    payload = payload
                });
            };
            m_GraphView.canPasteSerializedData = serialized =>
            {
                if (!TryReadEnvelope(serialized, out GraphAuthoringClipboardEnvelope envelope))
                    return false;
                return string.Equals(envelope.domainId, m_Adapters.Document.DomainId, StringComparison.Ordinal) &&
                       m_Adapters.Mutation.CanPaste(m_Adapters.Document, envelope.payload);
            };
            m_GraphView.unserializeAndPaste = (operationName, serialized) =>
            {
                if (!TryReadEnvelope(serialized, out GraphAuthoringClipboardEnvelope envelope) ||
                    !string.Equals(envelope.domainId, m_Adapters.Document.DomainId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Graph Authoring clipboard domain does not match the current document.");
                m_Adapters.Mutation.Paste(m_Adapters.Document, operationName, envelope.payload);
                RebindGraphAuthoringDocument();
            };
        }

        void PublishSelection()
        {
            if (m_GraphView == null || m_Adapters == null)
                return;
            IReadOnlyList<ISelectable> selection = m_GraphView.selection;
            int[] identity = selection.Select(value => value?.GetHashCode() ?? 0).ToArray();
            if (identity.SequenceEqual(m_LastSelection))
                return;
            m_LastSelection = identity;
            m_Adapters.Inspector.Inspect(selection);
        }

        void HandleUndoRedo()
        {
            if (m_Adapters == null || m_Adapters.Mutation.ReadOnly)
                return;
            m_Adapters.Mutation.Reload(m_Adapters.Document);
            OnGraphAuthoringUndoRedo();
            RebindGraphAuthoringDocument();
        }

        void NavigateBack()
        {
            m_NavigateBack?.Invoke();
        }

        static bool TryReadEnvelope(string serialized, out GraphAuthoringClipboardEnvelope envelope)
        {
            envelope = null;
            if (string.IsNullOrEmpty(serialized))
                return false;
            try
            {
                envelope = JsonUtility.FromJson<GraphAuthoringClipboardEnvelope>(serialized);
                return envelope != null && !string.IsNullOrEmpty(envelope.domainId) && envelope.payload != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
