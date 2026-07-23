using System;
using System.Collections.Generic;

namespace TreeDesigner.Editor
{
    internal sealed class GraphDataCatalogExpandedEntrySet
    {
        readonly HashSet<string> m_Values = new HashSet<string>(StringComparer.Ordinal);
        GraphDataCatalogViewState m_State;

        public void Bind(GraphDataCatalogViewState state)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_State.ExpandedEntries ??= new List<string>();
            m_Values.Clear();
            m_Values.UnionWith(m_State.ExpandedEntries);
        }

        public bool Contains(string value) => m_Values.Contains(value);

        public bool Add(string value)
        {
            if (!m_Values.Add(value))
                return false;
            if (m_State != null && !m_State.ExpandedEntries.Contains(value))
                m_State.ExpandedEntries.Add(value);
            return true;
        }

        public bool Remove(string value)
        {
            if (!m_Values.Remove(value))
                return false;
            m_State?.ExpandedEntries.Remove(value);
            return true;
        }
    }

    internal sealed partial class GraphDataCatalogController
    {
        void SetGroupExpanded(string groupPath, bool expanded)
        {
            m_ViewState.CollapsedGroups ??= new List<string>();
            if (expanded)
                m_ViewState.CollapsedGroups.Remove(groupPath);
            else if (!m_ViewState.CollapsedGroups.Contains(groupPath))
                m_ViewState.CollapsedGroups.Add(groupPath);
        }

        void CaptureFilterViewState()
        {
            m_ViewState.SourceFilter = m_SourceFilter;
            m_ViewState.ScopeFilter = m_ScopeFilterField?.value is PipelineBlackboardScopeFilter scope
                ? scope
                : PipelineBlackboardScopeFilter.All;
            m_ViewState.ContextFilter = m_ContextFilterField?.value is PipelineBlackboardContextFilter context
                ? context
                : PipelineBlackboardContextFilter.AllVisible;
            m_ViewState.BlackboardFiltersExpanded = m_BlackboardFiltersExpanded;
        }

        void OnBlackboardFiltersChangedAndCapture()
        {
            OnBlackboardFiltersChanged();
            CaptureFilterViewState();
        }

        void SetSourceFilterAndCapture(GraphDataCatalogSourceFilter sourceFilter)
        {
            SetSourceFilter(sourceFilter);
            CaptureFilterViewState();
        }

        void ToggleBlackboardFiltersAndCapture()
        {
            ToggleBlackboardFilters();
            CaptureFilterViewState();
        }
    }
}
