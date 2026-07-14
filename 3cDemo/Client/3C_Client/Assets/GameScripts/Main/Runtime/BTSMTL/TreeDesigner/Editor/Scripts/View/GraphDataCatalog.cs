using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TreeDesigner.Editor
{
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
