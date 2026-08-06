using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TreeDesigner.Editor
{
    public interface IGraphAuthoringReadOnlyPanel
    {
        VisualElement View { get; }
        void Bind(IGraphAuthoringDocumentProjection document);
        void Refresh();
        void Unbind();
    }

    public sealed class GraphAuthoringBottomDockTabDescriptor
    {
        readonly HashSet<GraphAuthoringDocumentRoleId> m_Roles;

        public GraphAuthoringBottomDockTabDescriptor(
            string tabId,
            GraphAuthoringDomainId domainId,
            IReadOnlyList<GraphAuthoringDocumentRoleId> roles,
            string displayName,
            Func<IGraphAuthoringReadOnlyPanel> createPanel,
            bool defaultVisible = false)
        {
            TabId = GraphAuthoringIdentity.Require(tabId, nameof(tabId));
            DomainId = domainId.IsValid ? domainId : throw new ArgumentException("Bottom Dock domain identity is missing.", nameof(domainId));
            if (roles == null || roles.Count == 0)
                throw new ArgumentException("Bottom Dock tab requires at least one document role.", nameof(roles));
            m_Roles = new HashSet<GraphAuthoringDocumentRoleId>(roles);
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? throw new ArgumentException("Bottom Dock tab name is missing.", nameof(displayName)) : displayName;
            CreatePanel = createPanel ?? throw new ArgumentNullException(nameof(createPanel));
            DefaultVisible = defaultVisible;
        }

        public string TabId { get; }
        public GraphAuthoringDomainId DomainId { get; }
        public string DisplayName { get; }
        public Func<IGraphAuthoringReadOnlyPanel> CreatePanel { get; }
        public bool DefaultVisible { get; }
        public bool Allows(GraphAuthoringDocumentRoleId role) => m_Roles.Contains(role);
    }

    public sealed class GraphAuthoringBottomDockCatalog
    {
        readonly Dictionary<string, GraphAuthoringBottomDockTabDescriptor> m_Tabs = new Dictionary<string, GraphAuthoringBottomDockTabDescriptor>(StringComparer.Ordinal);

        public void Register(GraphAuthoringBottomDockTabDescriptor descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            if (!m_Tabs.TryAdd(descriptor.TabId, descriptor))
                throw new InvalidOperationException($"Bottom Dock tab '{descriptor.TabId}' is already registered.");
        }

        public IReadOnlyList<GraphAuthoringBottomDockTabDescriptor> Get(GraphAuthoringDomainId domain, GraphAuthoringDocumentRoleId role) => m_Tabs.Values
            .Where(value => value.DomainId.Equals(domain) && value.Allows(role))
            .OrderByDescending(value => value.DefaultVisible)
            .ThenBy(value => value.DisplayName, StringComparer.Ordinal)
            .ToArray();
    }

    public sealed class GraphAuthoringBottomDockPresenter :
        VisualElement
    {
        readonly Toolbar m_Toolbar;
        readonly VisualElement m_Content;
        readonly List<IGraphAuthoringReadOnlyPanel> m_Panels = new List<IGraphAuthoringReadOnlyPanel>();
        readonly List<string> m_TabIds = new List<string>();
        IGraphAuthoringDocumentProjection m_Document;
        string m_ActivePageId = string.Empty;

        public GraphAuthoringBottomDockPresenter()
        {
            AddToClassList("graph-authoring-bottom-dock");
            m_Toolbar = new Toolbar();
            m_Content = new VisualElement();
            m_Content.AddToClassList("graph-authoring-bottom-dock-content");
            Add(m_Toolbar);
            Add(m_Content);
        }

        public string ActivePageId => m_ActivePageId;

        public void Bind(IGraphAuthoringDocumentProjection document, GraphAuthoringBottomDockCatalog catalog)
        {
            Unbind();
            m_Document = document ?? throw new ArgumentNullException(nameof(document));
            IReadOnlyList<GraphAuthoringBottomDockTabDescriptor> tabs = (catalog ?? throw new ArgumentNullException(nameof(catalog))).Get(document.DomainId, document.DocumentRoleId);
            for (int i = 0; i < tabs.Count; i++)
            {
                GraphAuthoringBottomDockTabDescriptor descriptor = tabs[i];
                IGraphAuthoringReadOnlyPanel panel = descriptor.CreatePanel() ?? throw new InvalidOperationException($"Bottom Dock tab '{descriptor.TabId}' did not create a panel.");
                panel.Bind(document);
                panel.View.style.display = DisplayStyle.None;
                m_Panels.Add(panel);
                m_TabIds.Add(descriptor.TabId);
                m_Content.Add(panel.View);
                int index = i;
                m_Toolbar.Add(new Button(() => Select(index)) { text = descriptor.DisplayName });
            }
            if (tabs.Count > 0)
            {
                int initial = tabs.ToList().FindIndex(value => value.DefaultVisible);
                Select(initial < 0 ? 0 : initial);
            }
        }

        public void Refresh()
        {
            foreach (IGraphAuthoringReadOnlyPanel panel in m_Panels)
                panel.Refresh();
        }

        public void Unbind()
        {
            foreach (IGraphAuthoringReadOnlyPanel panel in m_Panels)
                panel.Unbind();
            m_Panels.Clear();
            m_TabIds.Clear();
            m_Toolbar.Clear();
            m_Content.Clear();
            m_Document = null;
            m_ActivePageId = string.Empty;
        }

        public void RestorePage(string pageId)
        {
            if (string.IsNullOrEmpty(pageId))
                return;
            int index = m_TabIds.IndexOf(pageId);
            if (index >= 0)
                Select(index);
        }

        void Select(int index)
        {
            if ((uint)index >= (uint)m_Panels.Count)
                return;
            for (int i = 0; i < m_Panels.Count; i++)
                m_Panels[i].View.style.display = i == index ? DisplayStyle.Flex : DisplayStyle.None;
            m_ActivePageId = m_TabIds[index];
            m_Panels[index].Refresh();
        }
    }
}
