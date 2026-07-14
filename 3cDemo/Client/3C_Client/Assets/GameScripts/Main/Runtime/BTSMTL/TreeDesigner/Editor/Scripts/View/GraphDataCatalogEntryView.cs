using System;
using BTSMTL.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TreeDesigner.Editor
{
    public sealed class GraphDataCatalogEntryView : VisualElement, IDragableVisualElement
    {
        readonly GraphDataCatalogEntry m_Entry;
        readonly GraphDataCatalogContext m_Context;
        readonly Action<string, bool> m_SetExpanded;
        readonly Action m_RequestRefresh;
        readonly Action<string> m_ReportError;
        readonly VisualElement m_Details;
        readonly Label m_ExpandIndicator;
        readonly DragHandle m_DragHandle;
        bool m_Expanded;
        bool m_DetailsDirty;

        public string StableId => m_Entry.StableId;
        public bool Expanded => m_Expanded;

        public GraphDataCatalogEntryView(
            GraphDataCatalogEntry entry,
            GraphDataCatalogContext context,
            bool expanded,
            Action<string, bool> setExpanded,
            Action requestRefresh,
            Action<string> reportError)
        {
            m_Entry = entry ?? throw new ArgumentNullException(nameof(entry));
            m_Context = context ?? throw new ArgumentNullException(nameof(context));
            m_Expanded = expanded;
            m_SetExpanded = setExpanded;
            m_RequestRefresh = requestRefresh;
            m_ReportError = reportError;

            AddToClassList("graph-data-entry");
            if (entry.IsStatus)
                AddToClassList("graph-data-status-entry");

            VisualElement header = new VisualElement();
            header.AddToClassList("graph-data-entry-header");
            Add(header);

            VisualElement typeHandle = new VisualElement();
            typeHandle.AddToClassList("graph-data-type-handle");
            typeHandle.style.backgroundColor = entry.TypeColor;
            typeHandle.tooltip = entry.HasCapability(GraphDataCatalogCapability.DragCreateNode)
                ? $"Drag {entry.DisplayName} into the current graph"
                : entry.UnavailableReason;
            header.Add(typeHandle);

            VisualElement info = new VisualElement();
            info.AddToClassList("graph-data-entry-info");
            header.Add(info);

            VisualElement primary = new VisualElement();
            primary.AddToClassList("graph-data-entry-primary");
            info.Add(primary);

            Label nameLabel = new Label(string.IsNullOrEmpty(entry.DisplayName) ? "<missing>" : entry.DisplayName);
            nameLabel.AddToClassList("graph-data-entry-name");
            nameLabel.tooltip = entry.DisplayName;
            primary.Add(nameLabel);

            Label typeLabel = new Label(entry.DisplayType);
            typeLabel.AddToClassList("graph-data-entry-type");
            typeLabel.tooltip = entry.DisplayType;
            primary.Add(typeLabel);

            Label metadata = new Label(BuildMetadata(entry));
            metadata.AddToClassList("graph-data-entry-metadata");
            metadata.tooltip = BuildTooltip(entry);
            info.Add(metadata);

            VisualElement commands = new VisualElement();
            commands.AddToClassList("graph-data-entry-commands");
            header.Add(commands);

            if (entry.Ownership != GraphDataCatalogOwnership.Local)
            {
                VisualElement readOnly = CreateIcon("LockIcon-On", "Read only");
                readOnly.AddToClassList("graph-data-readonly-icon");
                commands.Add(readOnly);
            }

            if (entry.HasCapability(GraphDataCatalogCapability.LocateSource))
            {
                Button locateButton = CreateIconButton("d_Project", "Locate source", () => entry.Source.Locate(entry, context));
                commands.Add(locateButton);
            }

            if (entry.HasCapability(GraphDataCatalogCapability.Delete))
            {
                Button deleteButton = CreateIconButton("d_Toolbar Minus", "Delete declaration", DeleteEntry);
                commands.Add(deleteButton);
            }

            if (entry.HasCapability(GraphDataCatalogCapability.ExpandDetails))
            {
                Button expandButton = new Button(ToggleDetails);
                expandButton.AddToClassList("graph-data-icon-button");
                expandButton.tooltip = "Show details";
                m_ExpandIndicator = new Label();
                m_ExpandIndicator.AddToClassList("graph-data-expand-indicator");
                expandButton.Add(m_ExpandIndicator);
                commands.Add(expandButton);
                info.AddManipulator(new Clickable(ToggleDetails));
            }

            m_Details = new VisualElement();
            m_Details.AddToClassList("graph-data-entry-details-host");
            Add(m_Details);
            RefreshDetails();

            if (entry.HasCapability(GraphDataCatalogCapability.DragCreateNode))
            {
                m_DragHandle = new DragHandle();
                m_DragHandle.Init(typeHandle, this);
            }
        }

        static string BuildMetadata(GraphDataCatalogEntry entry)
        {
            string ownership = entry.Ownership.ToString();
            return string.IsNullOrEmpty(entry.OwnerLabel)
                ? $"{entry.SourceLabel} | {ownership}"
                : $"{entry.SourceLabel} | {ownership} | {entry.OwnerLabel}";
        }

        static string BuildTooltip(GraphDataCatalogEntry entry)
        {
            string tooltip = $"{entry.DisplayName} | {entry.DisplayType} | {BuildMetadata(entry)}";
            if (!string.IsNullOrEmpty(entry.UnavailableReason))
                tooltip += $"\n{entry.UnavailableReason}";
            return tooltip;
        }

        static VisualElement CreateIcon(string iconName, string tooltip)
        {
            Image image = new Image
            {
                image = EditorGUIUtility.IconContent(iconName).image,
                scaleMode = ScaleMode.ScaleToFit,
                tooltip = tooltip
            };
            image.AddToClassList("graph-data-command-icon");
            return image;
        }

        static Button CreateIconButton(string iconName, string tooltip, Action action)
        {
            Button button = new Button(action);
            button.AddToClassList("graph-data-icon-button");
            button.tooltip = tooltip;
            button.Add(CreateIcon(iconName, tooltip));
            return button;
        }

        void ToggleDetails()
        {
            bool closing = m_Expanded;
            m_Expanded = !m_Expanded;
            m_SetExpanded?.Invoke(m_Entry.StableId, m_Expanded);
            RefreshDetails();
            if (closing && m_DetailsDirty)
            {
                m_DetailsDirty = false;
                m_RequestRefresh?.Invoke();
            }
        }

        void RefreshDetails()
        {
            m_Details.Clear();
            if (m_ExpandIndicator != null)
                m_ExpandIndicator.text = m_Expanded ? "v" : ">";
            m_Details.style.display = m_Expanded ? DisplayStyle.Flex : DisplayStyle.None;
            if (!m_Expanded)
                return;

            m_DetailsDirty = false;
            VisualElement details = m_Entry.Source.CreateDetails(m_Entry, m_Context, () => m_DetailsDirty = true);
            if (details != null)
                m_Details.Add(details);
        }

        void DeleteEntry()
        {
            if (!m_Entry.Source.TryDelete(m_Entry, m_Context, out string error))
            {
                m_ReportError?.Invoke(error);
                return;
            }

            m_RequestRefresh?.Invoke();
        }

        public void StartDrag()
        {
            AddToClassList("dragged");
        }

        public void StopDrag()
        {
            RemoveFromClassList("dragged");
        }

        public void UpdateDrag(DragUpdatedEvent e, VisualElement dragArea)
        {
            if (dragArea is BaseTreeView treeView &&
                m_Entry.Source.CanCreateNode(m_Entry, m_Context, treeView, out _))
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
            else
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
        }

        public void PerformDrag(DragPerformEvent e, VisualElement dragArea)
        {
            if (!(dragArea is BaseTreeView treeView))
                return;

            if (!m_Entry.Source.TryCreateNode(
                    m_Entry,
                    m_Context,
                    treeView,
                    e.localMousePosition,
                    out string error))
            {
                m_ReportError?.Invoke(error);
                return;
            }

            e.StopPropagation();
        }
    }
}
