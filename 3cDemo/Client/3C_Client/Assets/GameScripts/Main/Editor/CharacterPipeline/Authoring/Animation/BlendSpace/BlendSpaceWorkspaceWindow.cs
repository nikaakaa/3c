using BTSMTL.Authoring.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public readonly struct BlendSpaceNodeCatalogEntry
    {
        public BlendSpaceNodeCatalogEntry(string path, string typeId)
        {
            Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Graph node path is missing.", nameof(path)) : path;
            TypeId = string.IsNullOrWhiteSpace(typeId) ? throw new ArgumentException("Graph node type identity is missing.", nameof(typeId)) : typeId;
        }

        public string Path { get; }
        public string TypeId { get; }
    }

    public readonly struct BlendSpaceBreadcrumbEntry
    {
        public BlendSpaceBreadcrumbEntry(string displayName, string tooltip = null)
        {
            DisplayName = displayName ?? string.Empty;
            Tooltip = tooltip ?? string.Empty;
        }

        public string DisplayName { get; }
        public string Tooltip { get; }
    }

    public interface IBlendSpaceDocument
    {
        string DomainId { get; }
        string DocumentId { get; }
        string DisplayName { get; }
        string ContentRevision { get; }
        UnityEngine.Object SerializedOwner { get; }
    }

    public interface IBlendSpaceNodeCatalog
    {
        IReadOnlyList<BlendSpaceNodeCatalogEntry> GetEntries(IBlendSpaceDocument document);
    }

    public interface IBlendSpacePortPolicy
    {
        bool CanConnect(IBlendSpaceDocument document, Port startPort, Port endPort);
    }

    public interface IBlendSpaceMutationAdapter
    {
        bool ReadOnly { get; }
        void CreateNode(IBlendSpaceDocument document, string typeId, Vector2 graphPosition);
        GraphViewChange ApplyGraphViewChange(IBlendSpaceDocument document, GraphViewChange change);
        string SerializeSelection(IBlendSpaceDocument document, IEnumerable<GraphElement> elements);
        bool CanPaste(IBlendSpaceDocument document, string payload);
        void Paste(IBlendSpaceDocument document, string operationName, string payload);
        void Reload(IBlendSpaceDocument document);
    }

    public interface IBlendSpaceInspectorAdapter
    {
        VisualElement View { get; }
        void Bind(IBlendSpaceDocument document);
        void Inspect(IReadOnlyList<ISelectable> selection);
        void Clear();
    }

    public interface IBlendSpaceDiagnosticsAdapter
    {
        void Bind(IBlendSpaceDocument document, GraphView graphView, VisualElement toolbar);
        void Refresh();
        void Clear();
    }

    public enum BlendSpaceToolbarCommandKind : byte
    {
        Lightweight = 0,
        ExplicitOperation = 1
    }

    public readonly struct BlendSpaceToolbarCommandDescriptor
    {
        public BlendSpaceToolbarCommandDescriptor(
            string commandId,
            string label,
            BlendSpaceToolbarCommandKind kind,
            Action execute)
        {
            CommandId = string.IsNullOrWhiteSpace(commandId) ? throw new ArgumentException("Toolbar command identity is missing.", nameof(commandId)) : commandId;
            Label = string.IsNullOrWhiteSpace(label) ? throw new ArgumentException("Toolbar command label is missing.", nameof(label)) : label;
            Kind = kind;
            Execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public string CommandId { get; }
        public string Label { get; }
        public BlendSpaceToolbarCommandKind Kind { get; }
        public Action Execute { get; }
    }

    public sealed class BlendSpaceWorkspaceDescriptor
    {
        public BlendSpaceWorkspaceDescriptor(
            BlendSpaceWorkspaceRegionDescriptor navigator,
            BlendSpaceWorkspaceRegionDescriptor details,
            BlendSpaceWorkspaceRegionDescriptor bottomDock,
            IReadOnlyList<BlendSpaceToolbarCommandDescriptor> commands = null)
        {
            Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
            Details = details ?? throw new ArgumentNullException(nameof(details));
            BottomDock = bottomDock ?? throw new ArgumentNullException(nameof(bottomDock));
            Commands = commands ?? Array.Empty<BlendSpaceToolbarCommandDescriptor>();
        }

        public BlendSpaceWorkspaceRegionDescriptor Navigator { get; }
        public BlendSpaceWorkspaceRegionDescriptor Details { get; }
        public BlendSpaceWorkspaceRegionDescriptor BottomDock { get; }
        public IReadOnlyList<BlendSpaceToolbarCommandDescriptor> Commands { get; }
    }

    public sealed class BlendSpaceWorkspaceRegionDescriptor
    {
        public BlendSpaceWorkspaceRegionDescriptor(
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
    public sealed class BlendSpaceWorkspaceLayoutState
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

    public interface IBlendSpaceWorkspaceRegionAdapter
    {
        VisualElement View { get; }
        void Bind(IBlendSpaceDocument document);
        void Refresh();
        void Clear();
    }

    public interface IBlendSpaceWorkspacePageAdapter
    {
        string ActivePageId { get; }
        void RestorePage(string pageId);
    }

    public sealed class BlendSpaceDomainAdapters
    {
        public BlendSpaceDomainAdapters(
            IBlendSpaceDocument document,
            IBlendSpaceNodeCatalog nodeCatalog,
            IBlendSpacePortPolicy portPolicy,
            IBlendSpaceMutationAdapter mutation,
            IBlendSpaceInspectorAdapter inspector,
            IBlendSpaceDiagnosticsAdapter diagnostics,
            BlendSpaceWorkspaceDescriptor workspace = null,
            IBlendSpaceWorkspaceRegionAdapter navigator = null,
            IBlendSpaceWorkspaceRegionAdapter bottomDock = null)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            NodeCatalog = nodeCatalog ?? throw new ArgumentNullException(nameof(nodeCatalog));
            PortPolicy = portPolicy ?? throw new ArgumentNullException(nameof(portPolicy));
            Mutation = mutation ?? throw new ArgumentNullException(nameof(mutation));
            Inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            Workspace = workspace ?? new BlendSpaceWorkspaceDescriptor(
                new BlendSpaceWorkspaceRegionDescriptor("Navigator", navigator != null, 220f, 240f),
                new BlendSpaceWorkspaceRegionDescriptor("Details", true, 220f, 340f),
                new BlendSpaceWorkspaceRegionDescriptor("Results", bottomDock != null, 120f, 220f));
            Navigator = navigator;
            BottomDock = bottomDock;
        }

        public IBlendSpaceDocument Document { get; }
        public IBlendSpaceNodeCatalog NodeCatalog { get; }
        public IBlendSpacePortPolicy PortPolicy { get; }
        public IBlendSpaceMutationAdapter Mutation { get; }
        public IBlendSpaceInspectorAdapter Inspector { get; }
        public IBlendSpaceDiagnosticsAdapter Diagnostics { get; }
        public BlendSpaceWorkspaceDescriptor Workspace { get; }
        public IBlendSpaceWorkspaceRegionAdapter Navigator { get; }
        public IBlendSpaceWorkspaceRegionAdapter BottomDock { get; }
    }

    public interface IBlendSpaceDomainView
    {
        void BindAdapters(
            IBlendSpaceDocument document,
            IBlendSpacePortPolicy portPolicy,
            IBlendSpaceMutationAdapter mutation);
    }

    public sealed class BlendSpaceBreadcrumbHost :
        IDisposable
    {
        readonly Button m_BackButton;
        readonly VisualElement m_Breadcrumb;
        Action m_NavigateBack;

        public BlendSpaceBreadcrumbHost(
            Button backButton,
            VisualElement breadcrumb)
        {
            m_BackButton = backButton ??
                throw new ArgumentNullException(nameof(backButton));
            m_Breadcrumb = breadcrumb ??
                throw new ArgumentNullException(nameof(breadcrumb));
            m_BackButton.clicked += NavigateBack;
        }

        public void BindBack(Action navigateBack)
        {
            m_NavigateBack = navigateBack;
        }

        public void Render(
            IReadOnlyList<BlendSpaceBreadcrumbEntry> entries,
            Action<int> navigateTo)
        {
            int count = entries?.Count ?? 0;
            m_BackButton.SetEnabled(count > 1);
            m_Breadcrumb.Clear();
            for (int index = 0; index < count; index++)
            {
                if (index > 0)
                {
                    var separator = new Label("/");
                    separator.AddToClassList(
                        "tree-navigation-separator");
                    m_Breadcrumb.Add(separator);
                }
                BlendSpaceBreadcrumbEntry entry =
                    entries[index];
                if (index == count - 1)
                {
                    var label = new Label(entry.DisplayName)
                    {
                        tooltip = entry.Tooltip
                    };
                    label.AddToClassList(
                        "tree-navigation-current-segment");
                    m_Breadcrumb.Add(label);
                    continue;
                }
                int targetIndex = index;
                var button = new Button(
                    () => navigateTo?.Invoke(targetIndex))
                {
                    text = entry.DisplayName,
                    tooltip = entry.Tooltip
                };
                button.AddToClassList(
                    "tree-navigation-segment");
                m_Breadcrumb.Add(button);
            }
        }

        public void Dispose()
        {
            m_BackButton.clicked -= NavigateBack;
            m_NavigateBack = null;
            m_Breadcrumb.Clear();
        }

        void NavigateBack()
        {
            m_NavigateBack?.Invoke();
        }
    }

    sealed class BlendSpaceNodeSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        BlendSpaceWorkspaceWindow m_Shell;
        Texture2D m_IndentationIcon;

        public void Initialize(BlendSpaceWorkspaceWindow shell)
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
            IReadOnlyList<BlendSpaceNodeCatalogEntry> entries = m_Shell.GetNodeCatalogEntries();
            var groups = new HashSet<string>(StringComparer.Ordinal);
            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                BlendSpaceNodeCatalogEntry entry = entries[entryIndex];
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
            if (!(searchTreeEntry.userData is BlendSpaceNodeCatalogEntry entry))
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

    public abstract class BlendSpaceWorkspaceWindow : EditorWindow
    {
        [SerializeField]
        BlendSpaceWorkspaceLayoutState m_WorkspaceLayoutState = new BlendSpaceWorkspaceLayoutState();

        protected VisualElement m_WorkspaceToolbar;
        protected VisualElement m_NavigatorHost;
        protected VisualElement m_GraphCanvasHost;
        protected VisualElement m_PreviewHost;
        protected VisualElement m_DetailsHost;
        protected VisualElement m_BottomDockHost;
        protected VisualElement m_NavigationToolbar;
        protected Label m_TreeTitle;

        GraphView m_GraphView;
        BlendSpaceDomainAdapters m_Adapters;
        BlendSpaceNodeSearchProvider m_NodeSearchProvider;
        GraphAuthoringSelectionBinding m_SelectionBinding;
        int[] m_LastSelection = Array.Empty<int>();
        BlendSpaceBreadcrumbHost m_BreadcrumbHost;
        GraphAuthoringUndoBinding m_UndoBinding;
        TwoPaneSplitView m_NavigatorSplit;
        TwoPaneSplitView m_DetailsSplit;
        TwoPaneSplitView m_BottomDockSplit;
        TwoPaneSplitView m_PreviewSplit;
        VisualElement m_NavigatorRegion;
        VisualElement m_PreviewRegion;
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
        protected BlendSpaceDomainAdapters GraphAuthoringAdapters => m_Adapters;

        protected abstract GraphView CreateGraphAuthoringView();
        protected virtual VisualElement CreateGraphAuthoringPreviewView() => null;
        protected abstract VisualElement CreateGraphAuthoringInspectorView();
        protected abstract BlendSpaceDomainAdapters CreateGraphAuthoringAdapters();
        protected virtual bool UseGraphAuthoringShellInteractions => true;

        public virtual void CreateGUI()
        {
            if (m_WorkspaceLayoutState == null)
                m_WorkspaceLayoutState = new BlendSpaceWorkspaceLayoutState();
            rootVisualElement.Clear();
            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>("BlendSpace/Workspace");
            if (!visualTree)
                throw new InvalidOperationException("Graph Authoring Editor Shell visual tree is missing.");
            visualTree.CloneTree(rootVisualElement);

            m_WorkspaceToolbar = RequireHost("workspace-toolbar-content");
            m_NavigatorHost = RequireHost("workspace-navigator-content");
            m_GraphCanvasHost = RequireHost("workspace-graph-content");
            m_PreviewHost = RequireHost("workspace-preview-content");
            m_DetailsHost = RequireHost("workspace-details-content");
            m_BottomDockHost = RequireHost("workspace-bottom-content");
            m_NavigatorSplit = RequireHost("workspace-horizontal") as TwoPaneSplitView;
            m_DetailsSplit = RequireHost("workspace-content-horizontal") as TwoPaneSplitView;
            m_PreviewSplit = RequireHost("workspace-center-vertical") as TwoPaneSplitView;
            m_BottomDockSplit = RequireHost("workspace-side-vertical") as TwoPaneSplitView;
            m_NavigatorRegion = RequireHost("workspace-navigator");
            m_PreviewRegion = RequireHost("workspace-preview");
            m_DetailsRegion = RequireHost("workspace-details");
            m_BottomDockRegion = RequireHost("workspace-bottom-dock");
            m_NavigationToolbar = rootVisualElement.Q("tree-navigation-toolbar");
            m_BreadcrumbHost = new BlendSpaceBreadcrumbHost(
                rootVisualElement.Q<Button>(
                    "tree-navigation-back-button"),
                rootVisualElement.Q(
                    "tree-navigation-breadcrumb"));
            m_GraphView = CreateGraphAuthoringView() ?? throw new InvalidOperationException("Graph Authoring domain did not create a GraphView.");
            m_GraphView.name = "tree-view";
            m_GraphCanvasHost.Add(m_GraphView);
            VisualElement preview = CreateGraphAuthoringPreviewView();
            m_PreviewHost.Clear();
            if (preview != null)
                m_PreviewHost.Add(preview);
            m_PreviewRegion.style.display = preview != null
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            m_TreeTitle = new Label { name = "tree-title" };
            rootVisualElement.Add(m_TreeTitle);
            VisualElement inspector = CreateGraphAuthoringInspectorView() ?? throw new InvalidOperationException("Graph Authoring domain did not create an Inspector view.");
            inspector.name = "tree-inspector";

            m_Adapters = CreateGraphAuthoringAdapters();
            if (!ReferenceEquals(m_Adapters.Inspector.View, inspector))
                throw new InvalidOperationException("Graph Authoring Inspector adapter must own the Shell Inspector view.");
            ConfigureWorkspace(inspector);
            if (m_GraphView is IBlendSpaceDomainView domainView)
                domainView.BindAdapters(m_Adapters.Document, m_Adapters.PortPolicy, m_Adapters.Mutation);
            if (UseGraphAuthoringShellInteractions)
            {
                BindSearch();
                BindClipboard();
            }
            m_Adapters.Inspector.Bind(m_Adapters.Document);
            m_Adapters.Navigator?.Bind(m_Adapters.Document);
            m_Adapters.BottomDock?.Bind(m_Adapters.Document);
            m_Adapters.Diagnostics.Bind(m_Adapters.Document, m_GraphView, m_WorkspaceToolbar);
            RestorePageState();
            rootVisualElement.schedule.Execute(InitializeLayout);
            m_SelectionBinding =
                new GraphAuthoringSelectionBinding(
                    m_GraphView,
                    PublishSelection);
            m_UndoBinding =
                new GraphAuthoringUndoBinding(HandleUndoRedo);
            OnGraphAuthoringShellCreated();
        }

        protected virtual void OnGraphAuthoringShellCreated() { }

        protected virtual void OnDisable()
        {
            CaptureLayoutState();
            CapturePageState();
            m_UndoBinding?.Dispose();
            m_UndoBinding = null;
            m_SelectionBinding?.Dispose();
            m_SelectionBinding = null;
            m_Adapters?.Diagnostics.Clear();
            m_Adapters?.BottomDock?.Clear();
            m_Adapters?.Navigator?.Clear();
            m_Adapters?.Inspector.Clear();
            m_BreadcrumbHost?.Dispose();
            m_BreadcrumbHost = null;
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
            BlendSpaceWorkspaceDescriptor descriptor = m_Adapters.Workspace;
            rootVisualElement.Q<Label>("workspace-navigator-title").text = descriptor.Navigator.Title;
            rootVisualElement.Q<Label>("workspace-details-title").text = descriptor.Details.Title;
            rootVisualElement.Q<Label>("workspace-bottom-title").text = descriptor.BottomDock.Title;
            m_NavigatorRegion.style.minWidth = descriptor.Navigator.MinimumDimension;
            m_DetailsRegion.style.minWidth = descriptor.Details.MinimumDimension;
            m_BottomDockRegion.style.minHeight = descriptor.BottomDock.MinimumDimension;
            if (m_PreviewRegion.style.display == DisplayStyle.None)
                SetCollapsed(m_PreviewSplit, 0, true);
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
                BlendSpaceToolbarCommandDescriptor command = descriptor.Commands[i];
                var button = new Button(command.Execute)
                {
                    name = $"workspace-command-{command.CommandId}",
                    text = command.Label
                };
                button.EnableInClassList("workspace-explicit-operation", command.Kind == BlendSpaceToolbarCommandKind.ExplicitOperation);
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
            if (m_Adapters == null || m_NavigatorSplit == null || m_DetailsSplit == null || m_BottomDockSplit == null || m_PreviewSplit == null)
                return;
            BlendSpaceWorkspaceDescriptor descriptor = m_Adapters.Workspace;
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
            BlendSpaceWorkspaceDescriptor descriptor = m_Adapters.Workspace;
            SetCollapsed(m_NavigatorSplit, 0, !descriptor.Navigator.Visible || m_WorkspaceLayoutState.navigatorCollapsed || m_NarrowNavigatorCollapsed);
            SetCollapsed(m_DetailsSplit, 1, !descriptor.Details.Visible || m_WorkspaceLayoutState.detailsCollapsed || m_NarrowDetailsCollapsed);
            SetCollapsed(m_BottomDockSplit, 1, !descriptor.BottomDock.Visible || m_WorkspaceLayoutState.bottomDockCollapsed || m_NarrowBottomDockCollapsed);
            SetCollapsed(m_PreviewSplit, 0, m_PreviewRegion == null || m_PreviewRegion.style.display == DisplayStyle.None);
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
                m_WorkspaceLayoutState = new BlendSpaceWorkspaceLayoutState();
            CaptureDimensions();
        }

        void CapturePageState()
        {
            if (m_Adapters?.Inspector is IBlendSpaceWorkspacePageAdapter details)
                m_WorkspaceLayoutState.detailsPageId = details.ActivePageId;
            if (m_Adapters?.BottomDock is IBlendSpaceWorkspacePageAdapter bottom)
                m_WorkspaceLayoutState.bottomDockPageId = bottom.ActivePageId;
        }

        void RestorePageState()
        {
            if (m_Adapters?.Inspector is IBlendSpaceWorkspacePageAdapter details)
                details.RestorePage(m_WorkspaceLayoutState.detailsPageId);
            if (m_Adapters?.BottomDock is IBlendSpaceWorkspacePageAdapter bottom)
                bottom.RestorePage(m_WorkspaceLayoutState.bottomDockPageId);
        }

        static void MountRegion(
            VisualElement host,
            IBlendSpaceWorkspaceRegionAdapter adapter,
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
            m_BreadcrumbHost?.BindBack(navigateBack);
        }

        protected void RenderGraphAuthoringNavigation(
            IReadOnlyList<BlendSpaceBreadcrumbEntry> entries,
            Action<int> navigateTo)
        {
            m_BreadcrumbHost?.Render(entries, navigateTo);
        }

        internal IReadOnlyList<BlendSpaceNodeCatalogEntry> GetNodeCatalogEntries()
        {
            return m_Adapters?.NodeCatalog.GetEntries(m_Adapters.Document) ?? Array.Empty<BlendSpaceNodeCatalogEntry>();
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
            m_NodeSearchProvider = CreateInstance<BlendSpaceNodeSearchProvider>();
            m_NodeSearchProvider.Initialize(this);
            m_GraphView.nodeCreationRequest = context =>
            {
                if (!m_Adapters.Mutation.ReadOnly)
                    SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), m_NodeSearchProvider);
            };
        }

        void BindClipboard()
        {
            BlendSpaceClipboardBinding.Bind(
                m_GraphView,
                () => m_Adapters.Document.DomainId,
                elements =>
                    m_Adapters.Mutation.SerializeSelection(
                        m_Adapters.Document,
                        elements),
                payload =>
                    m_Adapters.Mutation.CanPaste(
                        m_Adapters.Document,
                        payload),
                (operationName, payload) =>
                {
                    m_Adapters.Mutation.Paste(
                        m_Adapters.Document,
                        operationName,
                        payload);
                    RebindGraphAuthoringDocument();
                });
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

    }
}
