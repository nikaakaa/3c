using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TreeDesigner.Editor
{
    public readonly struct GraphAuthoringReadOnlyDetail
    {
        public GraphAuthoringReadOnlyDetail(string label, string value, string tooltip = "")
        {
            Label = label ?? string.Empty;
            Value = value ?? string.Empty;
            Tooltip = tooltip ?? string.Empty;
        }

        public string Label { get; }
        public string Value { get; }
        public string Tooltip { get; }
    }

    public readonly struct GraphAuthoringFieldOption
    {
        public GraphAuthoringFieldOption(
            string value,
            string displayName)
        {
            Value = GraphAuthoringIdentity.Require(
                value,
                nameof(value));
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? Value
                : displayName.Trim();
        }

        public string Value { get; }
        public string DisplayName { get; }
    }

    public interface IGraphAuthoringFieldOptionSource
    {
        bool TryGetFieldOptions(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringElementId elementId,
            GraphAuthoringFieldDescriptor field,
            out IReadOnlyList<GraphAuthoringFieldOption> options);
    }

    public interface IGraphAuthoringDetailsDataSource
    {
        object ReadField(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringElementId elementId,
            GraphAuthoringFieldDescriptor field);

        IReadOnlyList<GraphAuthoringReadOnlyDetail> GetLive(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringSelection selection);

        IReadOnlyList<GraphAuthoringReadOnlyDetail> GetReferences(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringSelection selection);

        IReadOnlyList<GraphAuthoringReadOnlyDetail> GetDiagnostics(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringSelection selection);
    }

    public readonly struct GraphAuthoringDetailsCommandRequest
    {
        public GraphAuthoringDetailsCommandRequest(
            GraphAuthoringElementId elementId,
            GraphAuthoringMutationKind kind,
            GraphAuthoringCommandId commandId,
            object value = null)
        {
            ElementId = elementId.IsValid
                ? elementId
                : throw new ArgumentException(
                    "Details command element identity is missing.",
                    nameof(elementId));
            Kind = kind;
            CommandId = commandId.IsValid
                ? commandId
                : throw new ArgumentException(
                    "Details command identity is missing.",
                    nameof(commandId));
            Value = value;
        }

        public GraphAuthoringElementId ElementId { get; }
        public GraphAuthoringMutationKind Kind { get; }
        public GraphAuthoringCommandId CommandId { get; }
        public object Value { get; }
    }

    public sealed class GraphAuthoringDetailsBinding
    {
        public GraphAuthoringDetailsBinding(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringCapabilityCatalog capabilities,
            IGraphAuthoringDomainMutation mutation,
            IGraphAuthoringDetailsDataSource dataSource,
            Action<GraphAuthoringDetailsCommandRequest>
                commandHandler = null,
            bool allowDisplayNameMutation = false)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
            Mutation = mutation ?? throw new ArgumentNullException(nameof(mutation));
            DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            CommandHandler = commandHandler;
            AllowDisplayNameMutation = allowDisplayNameMutation;
        }

        public IGraphAuthoringDocumentProjection Document { get; }
        public GraphAuthoringCapabilityCatalog Capabilities { get; }
        public IGraphAuthoringDomainMutation Mutation { get; }
        public IGraphAuthoringDetailsDataSource DataSource { get; }
        public Action<GraphAuthoringDetailsCommandRequest>
            CommandHandler { get; }
        public bool AllowDisplayNameMutation { get; }
    }

    public sealed class GraphAuthoringDetailsRegion :
        GraphAuthoringDetailsHostView
    {
        readonly GraphAuthoringDetailsPresenter m_Presenter;
        readonly GraphAuthoringStateMachineDetailsPresenter
            m_StateMachinePresenter;

        public GraphAuthoringDetailsRegion() :
            base(false)
        {
            AddToClassList("graph-authoring-details");
            m_Presenter =
                new GraphAuthoringDetailsPresenter(DetailsContent);
            m_StateMachinePresenter =
                new GraphAuthoringStateMachineDetailsPresenter(
                    DetailsContent);
        }

        public void Bind(GraphAuthoringDetailsBinding binding) =>
            m_Presenter.Bind(binding);

        public void Inspect(GraphAuthoringSelection selection) =>
            m_Presenter.Inspect(selection);

        public void ClearSelection() =>
            m_Presenter.ClearSelection();

        public void BindStateMachine(
            GraphAuthoringStateMachineBinding binding,
            IGraphAuthoringStateMachineDetailsDataSource dataSource) =>
            m_StateMachinePresenter.Bind(binding, dataSource);

        public void InspectState(
            GraphAuthoringElementId stateId) =>
            m_StateMachinePresenter.InspectState(stateId);

        public void InspectTransition(
            GraphAuthoringElementId transitionId) =>
            m_StateMachinePresenter.InspectTransition(transitionId);

        public void ClearStateMachineSelection() =>
            m_StateMachinePresenter.ClearSelection();
    }

    public sealed class GraphAuthoringDetailsPresenter
    {
        readonly VisualElement m_Scroll;
        GraphAuthoringDetailsBinding m_Binding;
        GraphAuthoringSelection? m_Selection;

        public GraphAuthoringDetailsPresenter(
            VisualElement content)
        {
            m_Scroll = content ??
                throw new ArgumentNullException(nameof(content));
        }

        public void Bind(GraphAuthoringDetailsBinding binding)
        {
            m_Binding = binding ?? throw new ArgumentNullException(nameof(binding));
            m_Selection = null;
            Rebuild();
        }

        public void Inspect(GraphAuthoringSelection selection)
        {
            m_Selection = selection;
            Rebuild();
        }

        public void ClearSelection()
        {
            m_Selection = null;
            Rebuild();
        }

        void Rebuild()
        {
            m_Scroll.Clear();
            if (m_Binding == null)
            {
                AddEmpty("No graph document is bound.");
                return;
            }
            if (!m_Selection.HasValue)
            {
                AddEmpty("Select one graph element.");
                return;
            }
            GraphAuthoringSelection selection = m_Selection.Value;
            if (selection.Kind != GraphAuthoringSelectionKind.Node)
            {
                AddReadOnlySection("References", m_Binding.DataSource.GetReferences(m_Binding.Document, selection), true);
                AddReadOnlySection("Live", m_Binding.DataSource.GetLive(m_Binding.Document, selection), true);
                AddReadOnlySection("Diagnostics", m_Binding.DataSource.GetDiagnostics(m_Binding.Document, selection), false);
                return;
            }
            GraphAuthoringNodeProjection node = m_Binding.Document.Nodes.FirstOrDefault(value => value.NodeId.Equals(selection.ElementId));
            if (node == null)
                throw new InvalidOperationException($"Selected graph node '{selection.ElementId}' is not in the current document.");
            GraphAuthoringCapabilityDescriptor capability = m_Binding.Capabilities.Require(
                node.CapabilityId,
                m_Binding.Document.DomainId,
                m_Binding.Document.DocumentRoleId);
            AddHeader(node, capability);
            AddAuthoringSection(node, capability);
            AddCommandSection(node, capability);
            AddReadOnlySection("Live", m_Binding.DataSource.GetLive(m_Binding.Document, selection), true);
            AddReadOnlySection("References", m_Binding.DataSource.GetReferences(m_Binding.Document, selection), true);
            AddReadOnlySection("Diagnostics", m_Binding.DataSource.GetDiagnostics(m_Binding.Document, selection), false);
        }

        void AddHeader(GraphAuthoringNodeProjection node, GraphAuthoringCapabilityDescriptor capability)
        {
            var header = new VisualElement();
            header.AddToClassList("graph-authoring-details-header");
            header.Add(new Label(string.IsNullOrWhiteSpace(node.DisplayName) ? capability.DisplayName : node.DisplayName));
            header.Add(new Label(capability.Category));
            m_Scroll.Add(header);
            if (!m_Binding.AllowDisplayNameMutation)
                return;
            var displayName = new TextField("Display Name")
            {
                isDelayed = true,
                value = node.DisplayName
            };
            displayName.SetEnabled(!m_Binding.Mutation.ReadOnly);
            displayName.RegisterValueChangedCallback(evt =>
            {
                string value = evt.newValue ?? string.Empty;
                if (string.Equals(
                        value,
                        node.DisplayName,
                        StringComparison.Ordinal))
                    return;
                m_Binding.Mutation.Apply(
                    m_Binding.Document,
                    new GraphAuthoringMutationRequest(
                        GraphAuthoringMutationKind.SetDisplayName,
                        node.NodeId,
                        value: value));
                Rebuild();
            });
            m_Scroll.Add(displayName);
        }

        void AddAuthoringSection(GraphAuthoringNodeProjection node, GraphAuthoringCapabilityDescriptor capability)
        {
            var section = new Foldout { text = "Authoring", value = true };
            section.AddToClassList("graph-authoring-details-section");
            GraphAuthoringFieldDescriptor[] fields = capability.Fields
                .Where(value =>
                    value.AuthoringVisible &&
                    value.Section == GraphAuthoringDetailsSection.Authoring &&
                    value.IsVisible(controller =>
                        m_Binding.DataSource.ReadField(
                            m_Binding.Document,
                            node.NodeId,
                            capability.Fields.Single(candidate => candidate.FieldId.Equals(controller)))))
                .OrderBy(value => value.DisplayName, StringComparer.Ordinal)
                .ToArray();
            for (int i = 0; i < fields.Length; i++)
            {
                GraphAuthoringFieldDescriptor field = fields[i];
                object value = m_Binding.DataSource.ReadField(m_Binding.Document, node.NodeId, field);
                VisualElement control = CreateField(node.NodeId, field, value);
                control.SetEnabled(field.AuthoringWritable && !m_Binding.Mutation.ReadOnly);
                section.Add(control);
            }
            if (fields.Length == 0)
                section.Add(new Label("This node has no editable authoring fields."));
            m_Scroll.Add(section);
        }

        void AddCommandSection(GraphAuthoringNodeProjection node, GraphAuthoringCapabilityDescriptor capability)
        {
            if (capability.ChildSurfaces.Count == 0 && capability.Commands.Count == 0)
                return;
            var section = new Foldout { text = "Commands", value = true };
            section.AddToClassList("graph-authoring-details-section");
            foreach (GraphAuthoringChildSurfaceDescriptor child in capability.ChildSurfaces)
            {
                var button = new Button(() => Execute(
                    node.NodeId,
                    GraphAuthoringMutationKind.OpenChildSurface,
                    child.CommandId)) { text = child.DisplayName };
                button.SetEnabled(
                    m_Binding.CommandHandler != null ||
                    !m_Binding.Mutation.ReadOnly);
                section.Add(button);
            }
            foreach (GraphAuthoringCommandDescriptor command in capability.Commands)
            {
                if (command.PresentationKind !=
                    GraphAuthoringCommandPresentationKind.Button)
                    continue;
                var button = new Button(() => Execute(
                    node.NodeId,
                    GraphAuthoringMutationKind.ExecuteCommand,
                    command.CommandId)) { text = command.DisplayName };
                button.EnableInClassList("graph-authoring-destructive-command", command.Destructive);
                button.SetEnabled(!m_Binding.Mutation.ReadOnly);
                section.Add(button);
            }
            m_Scroll.Add(section);
        }

        void Execute(GraphAuthoringElementId elementId, GraphAuthoringMutationKind kind, GraphAuthoringCommandId commandId)
        {
            if (m_Binding.CommandHandler != null)
            {
                m_Binding.CommandHandler(
                    new GraphAuthoringDetailsCommandRequest(
                        elementId,
                        kind,
                        commandId));
                return;
            }
            m_Binding.Mutation.Apply(
                m_Binding.Document,
                new GraphAuthoringMutationRequest(kind, elementId, commandId: commandId));
        }

        VisualElement CreateField(GraphAuthoringElementId elementId, GraphAuthoringFieldDescriptor field, object value)
        {
            switch (field.ValueKind)
            {
                case GraphAuthoringFieldValueKind.Boolean:
                {
                    var control = new Toggle(field.DisplayName) { value = value is bool current && current };
                    control.RegisterValueChangedCallback(evt => SetField(elementId, field, evt.newValue));
                    return control;
                }
                case GraphAuthoringFieldValueKind.Integer:
                {
                    var control = new IntegerField(field.DisplayName) { value = value is int current ? current : 0 };
                    control.RegisterValueChangedCallback(evt => SetField(elementId, field, evt.newValue));
                    return control;
                }
                case GraphAuthoringFieldValueKind.Float:
                {
                    var control = new FloatField(field.DisplayName) { value = value is float current ? current : 0f };
                    control.RegisterValueChangedCallback(evt => SetField(elementId, field, evt.newValue));
                    return control;
                }
                case GraphAuthoringFieldValueKind.Vector2:
                {
                    var control = new Vector2Field(field.DisplayName) { value = value is Vector2 current ? current : Vector2.zero };
                    control.RegisterValueChangedCallback(evt => SetField(elementId, field, evt.newValue));
                    return control;
                }
                case GraphAuthoringFieldValueKind.Vector3:
                {
                    var control = new Vector3Field(field.DisplayName) { value = value is Vector3 current ? current : Vector3.zero };
                    control.RegisterValueChangedCallback(evt => SetField(elementId, field, evt.newValue));
                    return control;
                }
                case GraphAuthoringFieldValueKind.Quaternion:
                {
                    Quaternion quaternion = value is Quaternion current ? current : Quaternion.identity;
                    var control = new Vector3Field(field.DisplayName) { value = quaternion.eulerAngles };
                    control.RegisterValueChangedCallback(evt => SetField(elementId, field, Quaternion.Euler(evt.newValue)));
                    return control;
                }
                case GraphAuthoringFieldValueKind.Enum:
                {
                    List<string> choices = field.Constraint.AllowedValues.ToList();
                    string current = value?.ToString() ?? choices.FirstOrDefault() ?? string.Empty;
                    if (!choices.Contains(current))
                        choices.Add(current);
                    var control = new PopupField<string>(field.DisplayName, choices, current);
                    control.RegisterValueChangedCallback(evt => SetField(elementId, field, evt.newValue));
                    return control;
                }
                case GraphAuthoringFieldValueKind.IdentityReference:
                {
                    if (m_Binding.DataSource is
                            IGraphAuthoringFieldOptionSource optionSource &&
                        optionSource.TryGetFieldOptions(
                            m_Binding.Document,
                            elementId,
                            field,
                            out IReadOnlyList<
                                GraphAuthoringFieldOption> options))
                    {
                        return CreateOptionField(
                            elementId,
                            field,
                            value?.ToString() ?? string.Empty,
                            options);
                    }
                    var control = new TextField(field.DisplayName)
                    {
                        value = "Unavailable",
                        isReadOnly = true
                    };
                    return control;
                }
                case GraphAuthoringFieldValueKind.AssetReference:
                case GraphAuthoringFieldValueKind.Object:
                {
                    var control = new ObjectField(field.DisplayName)
                    {
                        objectType = field.ObjectType ?? typeof(UnityEngine.Object),
                        allowSceneObjects = false,
                        value = value as UnityEngine.Object
                    };
                    control.RegisterValueChangedCallback(evt => SetField(elementId, field, evt.newValue));
                    return control;
                }
                default:
                {
                    var control = new TextField(field.DisplayName) { value = value?.ToString() ?? string.Empty };
                    control.RegisterValueChangedCallback(evt => SetField(elementId, field, evt.newValue));
                    return control;
                }
            }
        }

        VisualElement CreateOptionField(
            GraphAuthoringElementId elementId,
            GraphAuthoringFieldDescriptor field,
            string currentValue,
            IReadOnlyList<GraphAuthoringFieldOption> source)
        {
            var values = new List<string>();
            var labels = new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<GraphAuthoringFieldOption> options =
                source ?? Array.Empty<GraphAuthoringFieldOption>();
            for (int i = 0; i < options.Count; i++)
            {
                GraphAuthoringFieldOption option = options[i];
                if (!unique.Add(option.Value))
                {
                    throw new InvalidOperationException(
                        $"Picker '{field.PickerKind}' contains duplicate identity '{option.Value}'.");
                }
                values.Add(option.Value);
                labels.Add(option.DisplayName);
            }
            int selected = values.FindIndex(value =>
                string.Equals(
                    value,
                    currentValue,
                    StringComparison.Ordinal));
            if (selected < 0 &&
                !string.IsNullOrWhiteSpace(currentValue))
            {
                selected = values.Count;
                values.Add(currentValue);
                labels.Add("Missing Reference");
            }
            if (values.Count == 0)
            {
                var unavailable = new TextField(field.DisplayName)
                {
                    value = "No available values",
                    isReadOnly = true
                };
                return unavailable;
            }
            if (selected < 0)
                selected = 0;
            var control = new PopupField<string>(
                field.DisplayName,
                labels,
                selected);
            control.RegisterValueChangedCallback(evt =>
            {
                int index = labels.IndexOf(evt.newValue);
                if (index >= 0)
                    SetField(elementId, field, values[index]);
            });
            return control;
        }

        void SetField(GraphAuthoringElementId elementId, GraphAuthoringFieldDescriptor field, object value)
        {
            m_Binding.Capabilities.RequireField(
                m_Binding.Document.Nodes.First(node => node.NodeId.Equals(elementId)).CapabilityId,
                field.FieldId,
                true);
            m_Binding.Mutation.Apply(
                m_Binding.Document,
                new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.SetField,
                    elementId,
                    fieldId: field.FieldId,
                    value: value));
        }

        void AddReadOnlySection(string title, IReadOnlyList<GraphAuthoringReadOnlyDetail> rows, bool expanded)
        {
            var section = new Foldout { text = title, value = expanded };
            section.AddToClassList("graph-authoring-details-section");
            IReadOnlyList<GraphAuthoringReadOnlyDetail> values = rows ?? Array.Empty<GraphAuthoringReadOnlyDetail>();
            for (int i = 0; i < values.Count; i++)
            {
                GraphAuthoringReadOnlyDetail row = values[i];
                var line = new VisualElement { tooltip = row.Tooltip };
                line.AddToClassList("graph-authoring-details-readonly-row");
                line.Add(new Label(row.Label));
                line.Add(new Label(row.Value));
                section.Add(line);
            }
            if (values.Count == 0)
                section.Add(new Label("None"));
            m_Scroll.Add(section);
        }

        void AddEmpty(string message)
        {
            var label = new Label(message);
            label.AddToClassList("graph-authoring-details-empty");
            m_Scroll.Add(label);
        }
    }
}
