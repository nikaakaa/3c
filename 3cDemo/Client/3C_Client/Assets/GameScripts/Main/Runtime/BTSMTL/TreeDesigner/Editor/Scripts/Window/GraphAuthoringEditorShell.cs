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

    public sealed class GraphAuthoringDomainAdapters
    {
        public GraphAuthoringDomainAdapters(
            IGraphAuthoringDocument document,
            IGraphAuthoringNodeCatalog nodeCatalog,
            IGraphAuthoringPortPolicy portPolicy,
            IGraphAuthoringMutationAdapter mutation,
            IGraphAuthoringInspectorAdapter inspector,
            IGraphAuthoringDiagnosticsAdapter diagnostics)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            NodeCatalog = nodeCatalog ?? throw new ArgumentNullException(nameof(nodeCatalog));
            PortPolicy = portPolicy ?? throw new ArgumentNullException(nameof(portPolicy));
            Mutation = mutation ?? throw new ArgumentNullException(nameof(mutation));
            Inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public IGraphAuthoringDocument Document { get; }
        public IGraphAuthoringNodeCatalog NodeCatalog { get; }
        public IGraphAuthoringPortPolicy PortPolicy { get; }
        public IGraphAuthoringMutationAdapter Mutation { get; }
        public IGraphAuthoringInspectorAdapter Inspector { get; }
        public IGraphAuthoringDiagnosticsAdapter Diagnostics { get; }
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
        protected VisualElement m_LeftPanel;
        protected VisualElement m_RightPanel;
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

        protected GraphView GraphAuthoringView => m_GraphView;
        protected GraphAuthoringDomainAdapters GraphAuthoringAdapters => m_Adapters;

        protected abstract GraphView CreateGraphAuthoringView();
        protected abstract VisualElement CreateGraphAuthoringInspectorView();
        protected abstract GraphAuthoringDomainAdapters CreateGraphAuthoringAdapters();

        public virtual void CreateGUI()
        {
            rootVisualElement.Clear();
            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>("VisualTree/BaseTreeWindow");
            if (!visualTree)
                throw new InvalidOperationException("Graph Authoring Editor Shell visual tree is missing.");
            visualTree.CloneTree(rootVisualElement);

            m_LeftPanel = rootVisualElement.Q("left-panel");
            m_RightPanel = rootVisualElement.Q("right-panel");
            m_NavigationToolbar = rootVisualElement.Q("tree-navigation-toolbar");
            m_NavigationBackButton = rootVisualElement.Q<Button>("tree-navigation-back-button");
            m_BreadcrumbContainer = rootVisualElement.Q("tree-navigation-breadcrumb");
            m_NavigationBackButton.clicked += NavigateBack;
            m_GraphView = CreateGraphAuthoringView() ?? throw new InvalidOperationException("Graph Authoring domain did not create a GraphView.");
            m_GraphView.name = "tree-view";
            m_RightPanel.Add(m_GraphView);

            m_TreeTitle = new Label { name = "tree-title" };
            m_RightPanel.Add(m_TreeTitle);
            VisualElement inspector = CreateGraphAuthoringInspectorView() ?? throw new InvalidOperationException("Graph Authoring domain did not create an Inspector view.");
            inspector.name = "tree-inspector";
            m_LeftPanel.Add(inspector);

            m_Adapters = CreateGraphAuthoringAdapters();
            if (!ReferenceEquals(m_Adapters.Inspector.View, inspector))
                throw new InvalidOperationException("Graph Authoring Inspector adapter must own the Shell Inspector view.");
            if (m_GraphView is IGraphAuthoringDomainView domainView)
                domainView.BindAdapters(m_Adapters.Document, m_Adapters.PortPolicy, m_Adapters.Mutation);
            BindSearch();
            BindClipboard();
            m_Adapters.Inspector.Bind(m_Adapters.Document);
            m_Adapters.Diagnostics.Bind(m_Adapters.Document, m_GraphView, m_NavigationToolbar);
            m_SelectionWatcher = m_GraphView.schedule.Execute(PublishSelection).Every(100);
            Undo.undoRedoPerformed += HandleUndoRedo;
            OnGraphAuthoringShellCreated();
        }

        protected virtual void OnGraphAuthoringShellCreated() { }

        protected virtual void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedo;
            m_SelectionWatcher?.Pause();
            m_Adapters?.Diagnostics.Clear();
            m_Adapters?.Inspector.Clear();
            if (m_NavigationBackButton != null)
                m_NavigationBackButton.clicked -= NavigateBack;
            m_NavigateBack = null;
            m_NavigationBackButton = null;
            m_BreadcrumbContainer = null;
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
            m_Adapters.Diagnostics.Bind(m_Adapters.Document, m_GraphView, m_NavigationToolbar);
            PublishSelection();
            m_Adapters.Diagnostics.Refresh();
            if (m_TreeTitle != null)
                m_TreeTitle.text = m_Adapters.Document.DisplayName;
        }

        protected virtual void OnGraphAuthoringUndoRedo() { }

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
