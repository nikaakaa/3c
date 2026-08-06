using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TreeDesigner.Editor
{
    public readonly struct GraphAuthoringNavigatorItem
    {
        public GraphAuthoringNavigatorItem(
            GraphAuthoringElementId itemId,
            string group,
            string displayName,
            string ownerId,
            string referenceId,
            GraphAuthoringCommandId openCommandId,
            string searchText = "")
        {
            ItemId = itemId.IsValid ? itemId : throw new ArgumentException("Navigator item identity is missing.", nameof(itemId));
            Group = group ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? throw new ArgumentException("Navigator item display name is missing.", nameof(displayName)) : displayName;
            OwnerId = ownerId ?? string.Empty;
            ReferenceId = referenceId ?? string.Empty;
            OpenCommandId = openCommandId;
            SearchText = searchText ?? string.Empty;
        }

        public GraphAuthoringElementId ItemId { get; }
        public string Group { get; }
        public string DisplayName { get; }
        public string OwnerId { get; }
        public string ReferenceId { get; }
        public GraphAuthoringCommandId OpenCommandId { get; }
        public string SearchText { get; }
    }

    public interface IGraphAuthoringNavigatorDataSource
    {
        IReadOnlyList<GraphAuthoringNavigatorItem> GetItems(IGraphAuthoringDocumentProjection document);
        void Open(IGraphAuthoringDocumentProjection document, GraphAuthoringNavigatorItem item);
    }

    public sealed class GraphAuthoringNavigatorPresenter :
        GraphAuthoringNavigatorHostView
    {
        readonly ToolbarSearchField m_Search;
        readonly VisualElement m_Items;
        IGraphAuthoringDocumentProjection m_Document;
        IGraphAuthoringNavigatorDataSource m_DataSource;

        public GraphAuthoringNavigatorPresenter() :
            base("BaseTreeNavigator")
        {
            AddToClassList("graph-authoring-navigator");
            m_Search = this.Q<ToolbarSearchField>(
                "graph-data-search") ??
                throw new InvalidOperationException(
                    "Graph authoring navigator search is missing.");
            m_Search.AddToClassList("graph-authoring-navigator-search");
            m_Search.RegisterValueChangedCallback(_ => Rebuild());
            m_Items = this.Q(
                "graph-data-catalog-container") ??
                throw new InvalidOperationException(
                    "Graph authoring navigator catalog is missing.");
            m_Items.AddToClassList("graph-authoring-navigator-items");
            this.Q("graph-data-filter-toolbar").style.display =
                DisplayStyle.None;
            this.Q("graph-data-blackboard-filter-panel").style.display =
                DisplayStyle.None;
            this.Q("graph-data-creation-bar").style.display =
                DisplayStyle.None;
            this.Q<Button>("graph-data-add-button").style.display =
                DisplayStyle.None;
        }

        public void Bind(IGraphAuthoringDocumentProjection document, IGraphAuthoringNavigatorDataSource dataSource)
        {
            m_Document = document ?? throw new ArgumentNullException(nameof(document));
            m_DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            Rebuild();
        }

        public void Refresh() => Rebuild();

        public bool FocusItem(
            string ownerId,
            string referenceId)
        {
            if (m_Document == null || m_DataSource == null)
                return false;
            GraphAuthoringNavigatorItem item =
                (m_DataSource.GetItems(m_Document) ??
                 Array.Empty<GraphAuthoringNavigatorItem>())
                .FirstOrDefault(value =>
                    string.Equals(
                        value.OwnerId,
                        ownerId,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        value.ReferenceId,
                        referenceId,
                        StringComparison.Ordinal));
            if (!item.ItemId.IsValid)
                return false;
            m_Search.value = item.DisplayName;
            Rebuild();
            return true;
        }

        public void Unbind()
        {
            m_Document = null;
            m_DataSource = null;
            m_Items.Clear();
        }

        void Rebuild()
        {
            m_Items.Clear();
            if (m_Document == null || m_DataSource == null)
                return;
            string search = m_Search.value?.Trim() ?? string.Empty;
            GraphAuthoringNavigatorItem[] items = (m_DataSource.GetItems(m_Document) ?? Array.Empty<GraphAuthoringNavigatorItem>())
                .Where(value => Matches(value, search))
                .OrderBy(value => value.Group, StringComparer.Ordinal)
                .ThenBy(value => value.DisplayName, StringComparer.Ordinal)
                .ToArray();
            for (int i = 0; i < items.Length; i++)
            {
                GraphAuthoringNavigatorItem item = items[i];
                AddItem(item);
            }
        }

        void AddItem(GraphAuthoringNavigatorItem item)
        {
            string[] segments = string.IsNullOrEmpty(item.Group)
                ? new[] { "Document" }
                : item.Group.Split('/');
            VisualElement parent = m_Items;
            string path = string.Empty;
            for (int i = 0; i < segments.Length; i++)
            {
                path = string.IsNullOrEmpty(path)
                    ? segments[i]
                    : path + "/" + segments[i];
                VisualElement group = parent.Q<VisualElement>(
                    $"navigator-group-{Sanitize(path)}");
                if (group == null)
                {
                    var foldout = new Foldout
                    {
                        text = segments[i],
                        value = true,
                        name = $"navigator-group-{Sanitize(path)}"
                    };
                    foldout.AddToClassList("graph-authoring-navigator-group");
                    parent.Add(foldout);
                    group = foldout;
                }
                parent = group;
            }
            var button = new Button(() => m_DataSource.Open(m_Document, item))
            {
                text = item.DisplayName,
                tooltip = BuildTooltip(item)
            };
            button.AddToClassList("graph-authoring-navigator-item");
            button.SetEnabled(item.OpenCommandId.IsValid);
            parent.Add(button);
        }

        static string Sanitize(string value)
        {
            var result = new System.Text.StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
                result.Append(char.IsLetterOrDigit(value[i]) ? value[i] : '-');
            return result.ToString();
        }

        static bool Matches(GraphAuthoringNavigatorItem item, string search)
        {
            if (string.IsNullOrEmpty(search))
                return true;
            return Contains(item.DisplayName, search) ||
                   Contains(item.Group, search) ||
                   Contains(item.OwnerId, search) ||
                   Contains(item.ReferenceId, search) ||
                   Contains(item.SearchText, search);
        }

        static bool Contains(string value, string search) =>
            !string.IsNullOrEmpty(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

        static string BuildTooltip(GraphAuthoringNavigatorItem item)
        {
            return string.IsNullOrWhiteSpace(item.Group)
                ? item.DisplayName
                : $"{item.Group} / {item.DisplayName}";
        }
    }
}
