using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace TreeDesigner.Editor
{
    public enum GraphDataCatalogSourceFilter { All, Input, Blackboard }
    public enum PipelineBlackboardScopeFilter { All, Character, Graph, State, ActionInstance, Frame }
    public enum PipelineBlackboardContextFilter { AllVisible, CurrentContext, Local, Inherited }

    public enum GraphDataCatalogSourceKind
    {
        Input,
        Blackboard
    }

    public enum GraphDataCatalogEntryKind
    {
        InputValue,
        ActionRequest,
        BlackboardDeclaration,
        Status
    }

    public enum GraphDataCatalogOwnership
    {
        External,
        Local,
        Inherited
    }

    [Flags]
    public enum GraphDataCatalogCapability
    {
        None = 0,
        DragCreateNode = 1 << 0,
        ExpandDetails = 1 << 1,
        Edit = 1 << 2,
        Delete = 1 << 3,
        LocateSource = 1 << 4
    }

    public sealed class GraphDataCatalogContext
    {
        public GraphDataCatalogContext(
            BaseTree tree,
            object authoringContext,
            IEnumerable<BaseTree> visibleBlackboardSources,
            int generation)
        {
            Tree = tree;
            AuthoringContext = authoringContext;
            VisibleBlackboardSources = visibleBlackboardSources?
                .Where(i => i != null)
                .Distinct()
                .ToArray() ?? Array.Empty<BaseTree>();
            Generation = generation;
        }

        public BaseTree Tree { get; }
        public Type GraphType => Tree?.GetType();
        public object AuthoringContext { get; }
        public IReadOnlyList<BaseTree> VisibleBlackboardSources { get; }
        public int Generation { get; }
    }

    public sealed class GraphDataCatalogEntry
    {
        public GraphDataCatalogEntry(
            IGraphDataCatalogSource source,
            string stableId,
            GraphDataCatalogEntryKind kind,
            string displayName,
            string displayType,
            string groupPath,
            GraphDataCatalogOwnership ownership,
            string sourceLabel,
            string ownerLabel,
            Color typeColor,
            GraphDataCatalogCapability capabilities,
            object payload,
            int contextGeneration,
            string unavailableReason = "",
            string searchKeywords = "")
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            StableId = stableId ?? string.Empty;
            Kind = kind;
            DisplayName = displayName ?? string.Empty;
            DisplayType = displayType ?? string.Empty;
            GroupPath = groupPath ?? string.Empty;
            Ownership = ownership;
            SourceLabel = sourceLabel ?? string.Empty;
            OwnerLabel = ownerLabel ?? string.Empty;
            TypeColor = typeColor;
            Capabilities = capabilities;
            Payload = payload;
            ContextGeneration = contextGeneration;
            UnavailableReason = unavailableReason ?? string.Empty;
            SearchKeywords = searchKeywords ?? string.Empty;
        }

        public IGraphDataCatalogSource Source { get; }
        public string StableId { get; }
        public GraphDataCatalogEntryKind Kind { get; }
        public string DisplayName { get; }
        public string DisplayType { get; }
        public string GroupPath { get; }
        public GraphDataCatalogOwnership Ownership { get; }
        public string SourceLabel { get; }
        public string OwnerLabel { get; }
        public Color TypeColor { get; }
        public GraphDataCatalogCapability Capabilities { get; }
        public object Payload { get; }
        public int ContextGeneration { get; }
        public string UnavailableReason { get; }
        public string SearchKeywords { get; }
        public bool IsStatus => Kind == GraphDataCatalogEntryKind.Status;

        public bool HasCapability(GraphDataCatalogCapability capability)
        {
            return (Capabilities & capability) == capability;
        }

        public bool Matches(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return true;

            string text = $"{DisplayName} {DisplayType} {GroupPath} {SourceLabel} {OwnerLabel} {Ownership} {UnavailableReason} {SearchKeywords}";
            return text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    public interface IGraphDataCatalogSource : IDisposable
    {
        event Action Changed;
        int Order { get; }
        GraphDataCatalogSourceKind Kind { get; }
        string DisplayName { get; }
        IEnumerable<GraphDataCatalogEntry> GetEntries(GraphDataCatalogContext context);
        VisualElement CreateDetails(GraphDataCatalogEntry entry, GraphDataCatalogContext context, Action requestRefresh);
        bool CanCreateNode(GraphDataCatalogEntry entry, GraphDataCatalogContext context, BaseTreeView treeView, out string reason);
        bool TryCreateNode(GraphDataCatalogEntry entry, GraphDataCatalogContext context, BaseTreeView treeView, Vector2 position, out string error);
        bool TryDelete(GraphDataCatalogEntry entry, GraphDataCatalogContext context, out string error);
        void Locate(GraphDataCatalogEntry entry, GraphDataCatalogContext context);
    }

    public interface IGraphDataCatalogSourceProvider
    {
        int Order { get; }
        IGraphDataCatalogSource CreateSource();
    }

    public sealed class GraphDataCatalogCreationOption
    {
        public GraphDataCatalogCreationOption(string id, string displayName)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
        }

        public string Id { get; }
        public string DisplayName { get; }
    }

    public sealed class GraphDataCatalogCreateRequest
    {
        public GraphDataCatalogCreateRequest(string name, string scopeId, string typeId)
        {
            Name = name ?? string.Empty;
            ScopeId = scopeId ?? string.Empty;
            TypeId = typeId ?? string.Empty;
        }

        public string Name { get; }
        public string ScopeId { get; }
        public string TypeId { get; }
    }

    public interface IGraphDataCatalogCreationSource
    {
        IReadOnlyList<GraphDataCatalogCreationOption> GetScopeOptions(GraphDataCatalogContext context);
        IReadOnlyList<GraphDataCatalogCreationOption> GetTypeOptions(GraphDataCatalogContext context);
        bool TryCreate(GraphDataCatalogCreateRequest request, GraphDataCatalogContext context, out string error);
    }

    public static class GraphDataCatalogSourceRegistry
    {
        static readonly List<IGraphDataCatalogSourceProvider> s_Providers = new List<IGraphDataCatalogSourceProvider>();

        public static event Action Changed;

        public static void Register(IGraphDataCatalogSourceProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            s_Providers.RemoveAll(i => i == null || i.GetType() == provider.GetType());
            s_Providers.Add(provider);
            s_Providers.Sort((left, right) => left.Order.CompareTo(right.Order));
            Changed?.Invoke();
        }

        public static IReadOnlyList<IGraphDataCatalogSource> CreateSources()
        {
            return s_Providers
                .Where(i => i != null)
                .Select(i => i.CreateSource())
                .Where(i => i != null)
                .OrderBy(i => i.Order)
                .ToArray();
        }
    }

    internal sealed class GraphDataCatalogController
    {
        readonly BaseTreeInspectorView m_Root;
        readonly Action m_ShowDataTab;
        readonly List<BaseTree> m_VisibleBlackboardTrees = new List<BaseTree>();
        readonly Dictionary<string, bool> m_FoldoutStates = new Dictionary<string, bool>(StringComparer.Ordinal);
        readonly HashSet<string> m_ExpandedEntries = new HashSet<string>(StringComparer.Ordinal);
        readonly List<IGraphDataCatalogSource> m_Sources = new List<IGraphDataCatalogSource>();
        readonly VisualElement m_CatalogContainer;
        readonly VisualElement m_CreationBar;
        readonly VisualElement m_BlackboardFilterPanel;
        readonly Button m_AddButton;
        readonly Button m_CreateButton;
        readonly Button m_CancelButton;
        readonly Button m_AllSourceButton;
        readonly Button m_InputSourceButton;
        readonly Button m_BlackboardSourceButton;
        readonly Button m_BlackboardFilterButton;
        readonly TextField m_NameField;
        readonly DropdownField m_ScopeField;
        readonly DropdownField m_TypeField;
        readonly EnumField m_ScopeFilterField;
        readonly EnumField m_ContextFilterField;
        readonly ToolbarSearchField m_SearchField;

        IReadOnlyList<GraphDataCatalogCreationOption> m_ScopeOptions = Array.Empty<GraphDataCatalogCreationOption>();
        IReadOnlyList<GraphDataCatalogCreationOption> m_TypeOptions = Array.Empty<GraphDataCatalogCreationOption>();
        GraphDataCatalogContext m_Context;
        GraphDataCatalogSourceFilter m_SourceFilter = GraphDataCatalogSourceFilter.All;
        object m_AuthoringContext;
        int m_Generation;
        bool m_RefreshScheduled;
        bool m_BlackboardFiltersExpanded;

        public GraphDataCatalogController(BaseTreeInspectorView root, Action showDataTab)
        {
            m_Root = root;
            m_ShowDataTab = showDataTab;
            m_CatalogContainer = root.Q("graph-data-catalog-container");
            m_CreationBar = root.Q("graph-data-creation-bar");
            m_BlackboardFilterPanel = root.Q("graph-data-blackboard-filter-panel");
            m_NameField = root.Q<TextField>("graph-data-create-name");
            m_ScopeField = root.Q<DropdownField>("graph-data-create-scope");
            m_TypeField = root.Q<DropdownField>("graph-data-create-type");
            m_ScopeFilterField = root.Q<EnumField>("graph-data-scope-filter");
            m_ContextFilterField = root.Q<EnumField>("graph-data-context-filter");
            m_SearchField = root.Q<ToolbarSearchField>("graph-data-search");
            m_AllSourceButton = root.Q<Button>("graph-data-source-all-button");
            m_InputSourceButton = root.Q<Button>("graph-data-source-input-button");
            m_BlackboardSourceButton = root.Q<Button>("graph-data-source-blackboard-button");
            m_BlackboardFilterButton = root.Q<Button>("graph-data-blackboard-filter-button");
            m_AddButton = root.Q<Button>("graph-data-add-button");
            m_CreateButton = root.Q<Button>("graph-data-create-button");
            m_CancelButton = root.Q<Button>("graph-data-cancel-button");

            m_ScopeFilterField?.Init(PipelineBlackboardScopeFilter.All);
            m_ContextFilterField?.Init(PipelineBlackboardContextFilter.AllVisible);
            m_ScopeFilterField?.RegisterValueChangedCallback(_ => OnBlackboardFiltersChanged());
            m_ContextFilterField?.RegisterValueChangedCallback(_ => OnBlackboardFiltersChanged());
            m_SearchField?.RegisterValueChangedCallback(_ => RequestRefresh());
            m_AllSourceButton.clicked += () => SetSourceFilter(GraphDataCatalogSourceFilter.All);
            m_InputSourceButton.clicked += () => SetSourceFilter(GraphDataCatalogSourceFilter.Input);
            m_BlackboardSourceButton.clicked += () => SetSourceFilter(GraphDataCatalogSourceFilter.Blackboard);
            m_BlackboardFilterButton.clicked += ToggleBlackboardFilters;
            m_AddButton.clicked += ToggleCreation;
            m_CreateButton.clicked += CreateDeclaration;
            m_CancelButton.clicked += HideCreation;

            AddButtonIcon(m_BlackboardFilterButton, "d_FilterByType");
            AddButtonIcon(m_CreateButton, "TestPassed");
            AddButtonIcon(m_CancelButton, "d_winbtn_win_close");
            m_CreationBar.style.display = DisplayStyle.None;
            RefreshFilterPresentation();

            root.RegisterCallback<AttachToPanelEvent>(_ => Attach());
            root.RegisterCallback<DetachFromPanelEvent>(_ => Detach());
        }

        public IEnumerable<BaseExposedProperty> VisibleBlackboardDeclarations =>
            m_VisibleBlackboardTrees.SelectMany(i => i.ExposedProperties);

        public void SetAuthoringContext(object authoringContext)
        {
            m_AuthoringContext = authoringContext;
        }

        public void SetVisibleBlackboardSources(IEnumerable<BaseTree> trees)
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

        public void Bind(BaseTree tree)
        {
            Clear();
            if (tree != null && !m_VisibleBlackboardTrees.Contains(tree))
                m_VisibleBlackboardTrees.Add(tree);

            m_Context = new GraphDataCatalogContext(
                tree,
                m_AuthoringContext,
                m_VisibleBlackboardTrees,
                ++m_Generation);
            RefreshCreationOptions();
            Rebuild();
        }

        public void Clear()
        {
            m_CatalogContainer.Clear();
            m_Context = null;
        }

        public bool FocusBlackboardDeclaration(string graphAuthoringId, string declarationId)
        {
            BaseExposedProperty declaration = VisibleBlackboardDeclarations.FirstOrDefault(i =>
                i != null &&
                string.Equals(i.Owner?.GraphAuthoringId, graphAuthoringId, StringComparison.Ordinal) &&
                string.Equals(i.DeclarationId, declarationId, StringComparison.Ordinal));
            if (declaration == null)
                return false;

            string stableId = $"blackboard:{graphAuthoringId}:{declarationId}";
            m_ShowDataTab();
            m_SearchField?.SetValueWithoutNotify(string.Empty);
            m_ScopeFilterField?.SetValueWithoutNotify(PipelineBlackboardScopeFilter.All);
            m_ContextFilterField?.SetValueWithoutNotify(PipelineBlackboardContextFilter.AllVisible);
            m_ExpandedEntries.Add(stableId);
            SetSourceFilter(GraphDataCatalogSourceFilter.Blackboard);
            Rebuild();
            m_Root.schedule.Execute(() =>
            {
                GraphDataCatalogEntryView target = m_CatalogContainer
                    .Query<GraphDataCatalogEntryView>()
                    .ToList()
                    .FirstOrDefault(i => string.Equals(i.StableId, stableId, StringComparison.Ordinal));
                if (target != null)
                    m_Root.Q<ScrollView>("graph-data-scroll")?.ScrollTo(target);
            });
            return true;
        }

        void Attach()
        {
            GraphDataCatalogSourceRegistry.Changed -= RebuildSources;
            GraphDataCatalogSourceRegistry.Changed += RebuildSources;
            RebuildSources();
        }

        void Detach()
        {
            GraphDataCatalogSourceRegistry.Changed -= RebuildSources;
            DisposeSources();
        }

        IReadOnlyList<IGraphDataCatalogSource> GetSources()
        {
            if (m_Sources.Count == 0)
                RebuildSources();
            return m_Sources;
        }

        void RebuildSources()
        {
            DisposeSources();
            m_Sources.Add(new BlackboardGraphDataCatalogSource());
            m_Sources.AddRange(GraphDataCatalogSourceRegistry.CreateSources());
            m_Sources.Sort((left, right) => left.Order.CompareTo(right.Order));
            foreach (IGraphDataCatalogSource source in m_Sources)
                source.Changed += RequestRefresh;

            if (m_Context != null)
            {
                RefreshCreationOptions();
                RequestRefresh();
            }
        }

        void DisposeSources()
        {
            foreach (IGraphDataCatalogSource source in m_Sources)
            {
                source.Changed -= RequestRefresh;
                source.Dispose();
            }
            m_Sources.Clear();
        }

        void RequestRefresh()
        {
            if (m_Context == null || m_RefreshScheduled)
                return;

            m_RefreshScheduled = true;
            m_Root.schedule.Execute(() =>
            {
                m_RefreshScheduled = false;
                Rebuild();
            });
        }

        void Rebuild()
        {
            if (m_CatalogContainer == null || m_Context == null)
                return;

            CaptureEntryStates();
            m_CatalogContainer.Clear();
            Dictionary<string, Foldout> groups = new Dictionary<string, Foldout>(StringComparer.Ordinal);
            List<GraphDataCatalogEntry> entries = new List<GraphDataCatalogEntry>();
            foreach (IGraphDataCatalogSource source in GetSources())
            {
                List<GraphDataCatalogEntry> sourceEntries = source.GetEntries(m_Context)?.Where(i => i != null).ToList()
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
                        m_Context.Generation));
                }
                entries.AddRange(sourceEntries);
            }

            List<GraphDataCatalogEntry> visible = entries
                .Where(IsEntryVisible)
                .OrderBy(i => i.Source.Order)
                .ThenBy(i => i.GroupPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (visible.Count == 0)
            {
                AddMessage("No matching graph data.");
                return;
            }

            foreach (GraphDataCatalogEntry entry in visible)
            {
                VisualElement parent = ResolveGroup(entry.GroupPath, groups);
                GraphDataCatalogEntryView view = new GraphDataCatalogEntryView(
                    entry,
                    m_Context,
                    m_ExpandedEntries.Contains(entry.StableId),
                    SetEntryExpanded,
                    RequestRefresh,
                    ReportError);
                parent.Add(view);
            }
        }

        void CaptureEntryStates()
        {
            List<GraphDataCatalogEntryView> views = m_CatalogContainer
                .Query<GraphDataCatalogEntryView>()
                .ToList();
            foreach (GraphDataCatalogEntryView view in views)
                SetEntryExpanded(view.StableId, view.Expanded);
        }

        VisualElement ResolveGroup(string groupPath, Dictionary<string, Foldout> groups)
        {
            VisualElement parent = m_CatalogContainer;
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
                    bool expanded = !m_FoldoutStates.TryGetValue(stateKey, out bool saved) || saved;
                    foldout = new Foldout { text = segment, value = expanded };
                    foldout.AddToClassList(currentPath.Contains("/")
                        ? "graph-data-category-foldout"
                        : "graph-data-source-foldout");
                    foldout.RegisterValueChangedCallback(evt => m_FoldoutStates[stateKey] = evt.newValue);
                    groups.Add(currentPath, foldout);
                    parent.Add(foldout);
                }
                parent = foldout.contentContainer;
            }
            return parent;
        }

        bool IsEntryVisible(GraphDataCatalogEntry entry)
        {
            if (m_SourceFilter != GraphDataCatalogSourceFilter.All &&
                !string.Equals(m_SourceFilter.ToString(), entry.Source.Kind.ToString(), StringComparison.Ordinal))
                return false;

            PipelineBlackboardScopeFilter scopeFilter = m_ScopeFilterField?.value is PipelineBlackboardScopeFilter scope
                ? scope
                : PipelineBlackboardScopeFilter.All;
            PipelineBlackboardContextFilter contextFilter = m_ContextFilterField?.value is PipelineBlackboardContextFilter context
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

            return entry.Matches(m_SearchField?.value);
        }

        void SetEntryExpanded(string stableId, bool expanded)
        {
            if (expanded)
                m_ExpandedEntries.Add(stableId);
            else
                m_ExpandedEntries.Remove(stableId);
        }

        void SetSourceFilter(GraphDataCatalogSourceFilter sourceFilter)
        {
            m_SourceFilter = sourceFilter;
            if (sourceFilter == GraphDataCatalogSourceFilter.Input)
            {
                m_ScopeFilterField?.SetValueWithoutNotify(PipelineBlackboardScopeFilter.All);
                m_ContextFilterField?.SetValueWithoutNotify(PipelineBlackboardContextFilter.AllVisible);
                m_BlackboardFiltersExpanded = false;
            }

            if (sourceFilter == GraphDataCatalogSourceFilter.Blackboard)
                m_BlackboardFiltersExpanded = true;

            RefreshFilterPresentation();
            RequestRefresh();
        }

        void ToggleBlackboardFilters()
        {
            if (m_SourceFilter == GraphDataCatalogSourceFilter.Input)
                return;

            m_BlackboardFiltersExpanded = !m_BlackboardFiltersExpanded;
            RefreshFilterPresentation();
        }

        void OnBlackboardFiltersChanged()
        {
            RefreshFilterPresentation();
            RequestRefresh();
        }

        void RefreshFilterPresentation()
        {
            bool blackboardFilterAvailable = m_SourceFilter != GraphDataCatalogSourceFilter.Input;
            bool hasBlackboardFilters =
                (m_ScopeFilterField?.value is PipelineBlackboardScopeFilter scope && scope != PipelineBlackboardScopeFilter.All) ||
                (m_ContextFilterField?.value is PipelineBlackboardContextFilter context && context != PipelineBlackboardContextFilter.AllVisible);

            m_AllSourceButton.EnableInClassList("selected", m_SourceFilter == GraphDataCatalogSourceFilter.All);
            m_InputSourceButton.EnableInClassList("selected", m_SourceFilter == GraphDataCatalogSourceFilter.Input);
            m_BlackboardSourceButton.EnableInClassList("selected", m_SourceFilter == GraphDataCatalogSourceFilter.Blackboard);
            m_BlackboardFilterButton.style.display = blackboardFilterAvailable ? DisplayStyle.Flex : DisplayStyle.None;
            m_BlackboardFilterButton.EnableInClassList("selected", hasBlackboardFilters);
            m_BlackboardFilterButton.tooltip = m_BlackboardFiltersExpanded
                ? "Hide blackboard filters"
                : "Show blackboard filters";
            m_BlackboardFilterPanel.style.display = blackboardFilterAvailable && m_BlackboardFiltersExpanded
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        void ToggleCreation()
        {
            bool show = m_CreationBar.resolvedStyle.display == DisplayStyle.None;
            m_CreationBar.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (show)
                m_NameField.Focus();
        }

        void HideCreation()
        {
            m_CreationBar.style.display = DisplayStyle.None;
            m_NameField.SetValueWithoutNotify(string.Empty);
        }

        void RefreshCreationOptions()
        {
            IGraphDataCatalogCreationSource source = GetSources().OfType<IGraphDataCatalogCreationSource>().FirstOrDefault();
            m_ScopeOptions = source?.GetScopeOptions(m_Context) ?? Array.Empty<GraphDataCatalogCreationOption>();
            m_TypeOptions = source?.GetTypeOptions(m_Context) ?? Array.Empty<GraphDataCatalogCreationOption>();

            m_ScopeField.choices = m_ScopeOptions.Select(i => i.DisplayName).ToList();
            m_TypeField.choices = m_TypeOptions.Select(i => i.DisplayName).ToList();
            if (m_ScopeField.choices.Count > 0 && !m_ScopeField.choices.Contains(m_ScopeField.value))
                m_ScopeField.SetValueWithoutNotify(m_ScopeField.choices[0]);
            if (m_TypeField.choices.Count > 0 && !m_TypeField.choices.Contains(m_TypeField.value))
                m_TypeField.SetValueWithoutNotify(m_TypeField.choices[0]);

            bool canCreate = source != null && m_ScopeOptions.Count > 0 && m_TypeOptions.Count > 0;
            m_AddButton.SetEnabled(canCreate);
            m_CreateButton.SetEnabled(canCreate);
            if (!canCreate)
                HideCreation();
        }

        void CreateDeclaration()
        {
            IGraphDataCatalogCreationSource source = GetSources().OfType<IGraphDataCatalogCreationSource>().FirstOrDefault();
            GraphDataCatalogCreationOption scope = m_ScopeOptions.FirstOrDefault(i => i.DisplayName == m_ScopeField.value);
            GraphDataCatalogCreationOption type = m_TypeOptions.FirstOrDefault(i => i.DisplayName == m_TypeField.value);
            if (source == null || scope == null || type == null)
            {
                ReportError("Blackboard creation options are unavailable for the current graph.");
                return;
            }

            GraphDataCatalogCreateRequest request = new GraphDataCatalogCreateRequest(
                m_NameField.value,
                scope.Id,
                type.Id);
            if (!source.TryCreate(request, m_Context, out string error))
            {
                ReportError(error);
                return;
            }

            HideCreation();
            Rebuild();
        }

        void AddMessage(string text, bool error = false)
        {
            Label label = new Label(text);
            label.AddToClassList("graph-data-message");
            if (error)
                label.AddToClassList("graph-data-error");
            m_CatalogContainer.Add(label);
        }

        void ReportError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return;

            Debug.LogError($"Graph Data Catalog: {error}");
            Label label = new Label(error);
            label.AddToClassList("graph-data-message");
            label.AddToClassList("graph-data-error");
            m_CatalogContainer.Insert(0, label);
        }

        static void AddButtonIcon(Button button, string iconName)
        {
            Image image = new Image
            {
                image = EditorGUIUtility.IconContent(iconName).image,
                scaleMode = ScaleMode.ScaleToFit
            };
            image.AddToClassList("graph-data-button-icon");
            button.Add(image);
        }
    }

    public interface IBlackboardGraphDataNodeFactory
    {
        int Order { get; }
        bool CanCreate(GraphDataCatalogContext context, BaseExposedProperty declaration, BaseTree tree);
        bool TryCreate(BaseTreeView treeView, BaseExposedProperty declaration, Vector2 position, out string error);
    }

    public static class BlackboardGraphDataNodeFactoryRegistry
    {
        static readonly List<IBlackboardGraphDataNodeFactory> s_Factories = new List<IBlackboardGraphDataNodeFactory>();

        public static void Register(IBlackboardGraphDataNodeFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            s_Factories.RemoveAll(i => i == null || i.GetType() == factory.GetType());
            s_Factories.Add(factory);
            s_Factories.Sort((left, right) => left.Order.CompareTo(right.Order));
        }

        public static bool CanCreate(GraphDataCatalogContext context, BaseExposedProperty declaration, out string reason)
        {
            if (TryResolveFactory(context, declaration, out _))
            {
                reason = string.Empty;
                return true;
            }

            reason = context?.Tree == null
                ? "Graph context is missing."
                : $"{context.Tree.GetType().Name} does not accept a blackboard value node for {declaration?.ValueType?.Name ?? "Unknown"}.";
            return false;
        }

        public static bool TryCreate(
            GraphDataCatalogContext context,
            BaseTreeView treeView,
            BaseExposedProperty declaration,
            Vector2 position,
            out string error)
        {
            if (!TryResolveFactory(context, declaration, out IBlackboardGraphDataNodeFactory factory))
            {
                CanCreate(context, declaration, out error);
                return false;
            }

            return factory.TryCreate(treeView, declaration, position, out error);
        }

        static bool TryResolveFactory(
            GraphDataCatalogContext context,
            BaseExposedProperty declaration,
            out IBlackboardGraphDataNodeFactory factory)
        {
            factory = s_Factories.FirstOrDefault(i => i.CanCreate(context, declaration, context?.Tree));
            if (factory != null)
                return true;

            if (context?.Tree != null && context.Tree.CanCreateNodeType(typeof(ExposedPropertyNode)))
            {
                factory = GenericExposedPropertyNodeFactory.Instance;
                return true;
            }

            return false;
        }

        sealed class GenericExposedPropertyNodeFactory : IBlackboardGraphDataNodeFactory
        {
            public static readonly GenericExposedPropertyNodeFactory Instance = new GenericExposedPropertyNodeFactory();

            public int Order => int.MaxValue;

            public bool CanCreate(GraphDataCatalogContext context, BaseExposedProperty declaration, BaseTree tree)
            {
                return declaration != null && tree != null && tree.CanCreateNodeType(typeof(ExposedPropertyNode));
            }

            public bool TryCreate(BaseTreeView treeView, BaseExposedProperty declaration, Vector2 position, out string error)
            {
                error = string.Empty;
                if (treeView?.Tree == null || declaration == null || !treeView.Tree.CanCreateNodeType(typeof(ExposedPropertyNode)))
                {
                    error = "The current graph does not accept ExposedPropertyNode.";
                    return false;
                }

                ExposedPropertyNode node = treeView.CreateNode(typeof(ExposedPropertyNode), position) as ExposedPropertyNode;
                if (node == null)
                {
                    error = "Could not create ExposedPropertyNode.";
                    return false;
                }

                node.ApplyModify("Bind Blackboard Declaration", () => node.SetExposedProperty(declaration));
                node.Refresh();
                return true;
            }
        }
    }

    public static class GraphDataCatalogDetails
    {
        public static VisualElement CreateContainer()
        {
            VisualElement container = new VisualElement();
            container.AddToClassList("graph-data-entry-details");
            return container;
        }

        public static void AddRow(VisualElement container, string label, string value, string tooltip = "")
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("graph-data-detail-row");

            Label labelElement = new Label(label ?? string.Empty);
            labelElement.AddToClassList("graph-data-detail-label");
            row.Add(labelElement);

            Label valueElement = new Label(value ?? string.Empty);
            valueElement.AddToClassList("graph-data-detail-value");
            valueElement.tooltip = string.IsNullOrEmpty(tooltip) ? value : tooltip;
            row.Add(valueElement);

            container.Add(row);
        }
    }
}
