using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TreeDesigner.Editor
{
    public interface IGraphAuthoringStateMachineDetailsDataSource
    {
        object ReadStateField(
            IGraphAuthoringStateMachineProjection document,
            GraphAuthoringStateProjection state,
            GraphAuthoringFieldDescriptor field);

        object ReadTransitionField(
            IGraphAuthoringStateMachineProjection document,
            GraphAuthoringTransitionProjection transition,
            GraphAuthoringFieldDescriptor field);
    }

    public sealed class GraphAuthoringStateMachineDetailsPresenter
    {
        readonly VisualElement m_Content;
        GraphAuthoringStateMachineBinding m_Binding;
        IGraphAuthoringStateMachineDetailsDataSource m_DataSource;
        Func<GraphAuthoringSelection, IReadOnlyList<GraphAuthoringReadOnlyDetail>>
            m_AppliedValues;
        GraphAuthoringElementId m_DraftCustomTransitionId;

        public GraphAuthoringStateMachineDetailsPresenter(VisualElement content)
        {
            m_Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public void Bind(
            GraphAuthoringStateMachineBinding binding,
            IGraphAuthoringStateMachineDetailsDataSource dataSource,
            Func<GraphAuthoringSelection, IReadOnlyList<GraphAuthoringReadOnlyDetail>>
                appliedValues = null)
        {
            m_Binding = binding ?? throw new ArgumentNullException(nameof(binding));
            m_DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            m_AppliedValues = appliedValues;
            m_Content.Clear();
        }

        public void InspectState(GraphAuthoringElementId stateId)
        {
            RequireBinding();
            GraphAuthoringStateProjection state = m_Binding.Document.States
                .FirstOrDefault(value => value.StateId.Equals(stateId));
            if (state == null)
                throw new InvalidOperationException($"State '{stateId}' is not in the current StateMachine.");
            m_DraftCustomTransitionId = default;
            m_Content.Clear();
            m_Content.Add(new Label(string.IsNullOrWhiteSpace(state.DisplayName) ? "State" : state.DisplayName));
            IReadOnlyList<GraphAuthoringFieldDescriptor> fields =
                m_Binding.Policy.GetStateFields(state) ?? Array.Empty<GraphAuthoringFieldDescriptor>();
            AddStateAuthoringSection(state, fields);
            AddReadOnlySection(
                "Runtime Inputs",
                new[] { new GraphAuthoringReadOnlyDetail("Source", "Compiled Pose StateMachine runtime") },
                true);
            AddReadOnlySection(
                "Applied Values",
                GetAppliedValues(GraphAuthoringSelectionKind.State, state.StateId),
                true);
            AddReadOnlySection(
                "References",
                state.ChildGraphId.IsValid
                    ? new[] { new GraphAuthoringReadOnlyDetail("Child Graph", state.ChildGraphId.Value) }
                    : Array.Empty<GraphAuthoringReadOnlyDetail>(),
                true);
            AddReadOnlySection(
                "Diagnostics",
                new[] { new GraphAuthoringReadOnlyDetail("Status", string.IsNullOrWhiteSpace(state.Status) ? "No diagnostic." : state.Status) },
                false);
        }

        public void InspectTransition(GraphAuthoringElementId transitionId)
        {
            RequireBinding();
            GraphAuthoringTransitionProjection transition = m_Binding.Document.Transitions
                .FirstOrDefault(value => value.TransitionId.Equals(transitionId));
            if (transition == null)
                throw new InvalidOperationException($"Transition '{transitionId}' is not in the current StateMachine.");
            m_Content.Clear();
            m_Content.Add(new Label(string.IsNullOrWhiteSpace(transition.DisplayName) ? "Transition" : transition.DisplayName));
            IReadOnlyList<GraphAuthoringFieldDescriptor> fields =
                m_Binding.Policy.GetTransitionFields(transition) ?? Array.Empty<GraphAuthoringFieldDescriptor>();
            AddTransitionAuthoringSection(transition, fields);
            if (transition.RuleOwnerId.IsValid)
            {
                var openRule = new Button(() =>
                    m_Binding.Policy.OpenTransitionRule(m_Binding.Document, transition.TransitionId))
                {
                    text = "Open Rule"
                };
                var commands = new Foldout { text = "Commands", value = true };
                commands.AddToClassList("graph-authoring-details-section");
                commands.Add(openRule);
                m_Content.Add(commands);
            }
            AddReadOnlySection(
                "Runtime Inputs",
                new[] { new GraphAuthoringReadOnlyDetail("Source", "Compiled Pose StateMachine runtime") },
                true);
            AddReadOnlySection(
                "Applied Values",
                GetAppliedValues(GraphAuthoringSelectionKind.Transition, transition.TransitionId),
                true);
            AddReadOnlySection(
                "References",
                new[]
                {
                    new GraphAuthoringReadOnlyDetail("Source State", transition.SourceStateId.Value),
                    new GraphAuthoringReadOnlyDetail("Target State", transition.TargetStateId.Value),
                    new GraphAuthoringReadOnlyDetail("Rule", transition.RuleOwnerId.IsValid ? transition.RuleOwnerId.Value : "None")
                },
                true);
            AddReadOnlySection(
                "Diagnostics",
                new[] { new GraphAuthoringReadOnlyDetail("Status", "No diagnostic.") },
                false);
        }

        public void ClearSelection()
        {
            m_DraftCustomTransitionId = default;
            m_Content.Clear();
            m_Content.Add(new Label("Select one StateMachine State or Transition."));
        }

        VisualElement CreateField(
            GraphAuthoringFieldDescriptor field,
            object value,
            Action<object> setValue)
        {
            VisualElement control;
            switch (field.ValueKind)
            {
                case GraphAuthoringFieldValueKind.Boolean:
                {
                    var toggle = new Toggle(field.DisplayName) { value = value is bool current && current };
                    toggle.RegisterValueChangedCallback(evt => setValue(evt.newValue));
                    control = toggle;
                    break;
                }
                case GraphAuthoringFieldValueKind.Integer:
                {
                    var integer = new IntegerField(field.DisplayName) { value = value is int current ? current : 0 };
                    integer.RegisterValueChangedCallback(evt => setValue(evt.newValue));
                    control = integer;
                    break;
                }
                case GraphAuthoringFieldValueKind.Float:
                {
                    var number = new FloatField(field.DisplayName) { value = value is float current ? current : 0f };
                    number.RegisterValueChangedCallback(evt => setValue(evt.newValue));
                    control = number;
                    break;
                }
                case GraphAuthoringFieldValueKind.Enum:
                {
                    List<string> values = field.Constraint.AllowedValues.ToList();
                    string current = value?.ToString() ?? values.FirstOrDefault() ?? string.Empty;
                    if (!values.Contains(current))
                        values.Add(current);
                    var choice = new PopupField<string>(field.DisplayName, values, current);
                    choice.RegisterValueChangedCallback(evt => setValue(evt.newValue));
                    control = choice;
                    break;
                }
                case GraphAuthoringFieldValueKind.AssetReference:
                {
                    var asset = new ObjectField(field.DisplayName)
                    {
                        objectType = field.ObjectType ?? typeof(UnityEngine.Object),
                        allowSceneObjects = false,
                        value = value as UnityEngine.Object
                    };
                    asset.RegisterValueChangedCallback(evt => setValue(evt.newValue));
                    control = asset;
                    break;
                }
                default:
                {
                    var text = new TextField(field.DisplayName) { value = value?.ToString() ?? string.Empty };
                    text.RegisterValueChangedCallback(evt => setValue(evt.newValue));
                    control = text;
                    break;
                }
            }
            control.SetEnabled(field.AuthoringWritable && !m_Binding.Mutation.ReadOnly);
            return control;
        }

        void AddStateAuthoringSection(
            GraphAuthoringStateProjection state,
            IReadOnlyList<GraphAuthoringFieldDescriptor> fields)
        {
            var section = new Foldout { text = "Authoring Defaults", value = true };
            section.AddToClassList("graph-authoring-details-section");
            foreach (GraphAuthoringFieldDescriptor field in VisibleFields(fields, controller =>
                         m_DataSource.ReadStateField(
                             m_Binding.Document,
                             state,
                             fields.Single(candidate => candidate.FieldId.Equals(controller)))))
            {
                object current = m_DataSource.ReadStateField(m_Binding.Document, state, field);
                section.Add(CreateFieldRow(field, current, value => SetStateField(state, field, value)));
            }
            if (section.childCount == 0)
                section.Add(new Label("This State has no editable authoring fields."));
            m_Content.Add(section);
        }

        void AddTransitionAuthoringSection(
            GraphAuthoringTransitionProjection transition,
            IReadOnlyList<GraphAuthoringFieldDescriptor> fields)
        {
            var section = new Foldout { text = "Authoring Defaults", value = true };
            section.AddToClassList("graph-authoring-details-section");
            foreach (GraphAuthoringFieldDescriptor field in VisibleFields(fields, controller =>
                         ReadTransitionField(
                             transition,
                             fields.Single(candidate => candidate.FieldId.Equals(controller)))))
            {
                object current = ReadTransitionField(transition, field);
                section.Add(CreateFieldRow(field, current, value => SetTransitionField(transition, field, value)));
            }
            if (section.childCount == 0)
                section.Add(new Label("This Transition has no editable authoring fields."));
            m_Content.Add(section);
        }

        static IEnumerable<GraphAuthoringFieldDescriptor> VisibleFields(
            IReadOnlyList<GraphAuthoringFieldDescriptor> fields,
            Func<GraphAuthoringFieldId, object> readField) =>
            fields
                .Where(value => value.AuthoringVisible && value.IsVisible(readField))
                .OrderBy(value => value.DisplayName, StringComparer.Ordinal);

        VisualElement CreateFieldRow(
            GraphAuthoringFieldDescriptor field,
            object value,
            Action<object> setValue)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.AddToClassList("graph-authoring-details-field-row");
            row.Add(CreateField(field, value, setValue));
            row.Add(new Label(TuningLabel(field)));
            return row;
        }

        void AddReadOnlySection(
            string title,
            IReadOnlyList<GraphAuthoringReadOnlyDetail> details,
            bool expanded)
        {
            var section = new Foldout { text = title, value = expanded };
            section.AddToClassList("graph-authoring-details-section");
            if (details == null || details.Count == 0)
            {
                section.Add(new Label("No declared values."));
            }
            else
            {
                for (int i = 0; i < details.Count; i++)
                {
                    GraphAuthoringReadOnlyDetail detail = details[i];
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.Add(new Label(detail.Label));
                    row.Add(new Label(detail.Value));
                    section.Add(row);
                }
            }
            m_Content.Add(section);
        }

        static string TuningLabel(GraphAuthoringFieldDescriptor field)
        {
            switch (field.Interaction)
            {
                case GraphAuthoringFieldInteractionPolicy.Structural:
                    return "Build Required";
                case GraphAuthoringFieldInteractionPolicy.TunableDefault:
                    return field.Tuning != null &&
                           field.Tuning.ApplyTiming == GraphAuthoringFieldApplyTiming.NextActivation
                        ? "Next Activation"
                        : "Live Now";
                case GraphAuthoringFieldInteractionPolicy.RuntimeInput:
                    return "Runtime Input";
                case GraphAuthoringFieldInteractionPolicy.DerivedReadOnly:
                    return "Read Only";
                default:
                    return "Unclassified";
            }
        }

        IReadOnlyList<GraphAuthoringReadOnlyDetail> GetAppliedValues(
            GraphAuthoringSelectionKind kind,
            GraphAuthoringElementId elementId)
        {
            if (m_AppliedValues == null)
            {
                return new[]
                {
                    new GraphAuthoringReadOnlyDetail(
                        "Target",
                        "Select an exact Preview or Live target to inspect applied values.")
                };
            }
            return m_AppliedValues(new GraphAuthoringSelection(kind, elementId));
        }

        void SetStateField(
            GraphAuthoringStateProjection state,
            GraphAuthoringFieldDescriptor field,
            object value)
        {
            if (!field.AuthoringWritable)
                throw new InvalidOperationException($"State field '{field.FieldId}' is read-only.");
            m_Binding.Mutation.Apply(
                m_Binding.Document,
                new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.SetStateField,
                    state.StateId,
                    fieldId: field.FieldId,
                    value: value));
            InspectState(state.StateId);
        }

        void SetTransitionField(
            GraphAuthoringTransitionProjection transition,
            GraphAuthoringFieldDescriptor field,
            object value)
        {
            if (!field.AuthoringWritable)
                throw new InvalidOperationException($"Transition field '{field.FieldId}' is read-only.");
            if (string.Equals(field.FieldId.Value, "blend-mode", StringComparison.Ordinal) &&
                string.Equals(value?.ToString(), "Custom", StringComparison.Ordinal))
            {
                GraphAuthoringFieldDescriptor customCurve = m_Binding.Policy
                    .GetTransitionFields(transition)
                    .Single(candidate => string.Equals(
                        candidate.FieldId.Value,
                        "custom-blend-curve",
                        StringComparison.Ordinal));
                if (m_DataSource.ReadTransitionField(m_Binding.Document, transition, customCurve) == null)
                {
                    m_DraftCustomTransitionId = transition.TransitionId;
                    InspectTransition(transition.TransitionId);
                    return;
                }
            }
            m_Binding.Mutation.Apply(
                m_Binding.Document,
                new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.SetTransitionField,
                    transition.TransitionId,
                    fieldId: field.FieldId,
                    value: value));
            m_DraftCustomTransitionId = default;
            InspectTransition(transition.TransitionId);
        }

        object ReadTransitionField(
            GraphAuthoringTransitionProjection transition,
            GraphAuthoringFieldDescriptor field)
        {
            if (m_DraftCustomTransitionId.Equals(transition.TransitionId) &&
                string.Equals(field.FieldId.Value, "blend-mode", StringComparison.Ordinal))
            {
                return "Custom";
            }
            return m_DataSource.ReadTransitionField(m_Binding.Document, transition, field);
        }

        void RequireBinding()
        {
            if (m_Binding == null)
                throw new InvalidOperationException("StateMachine details is not bound.");
        }
    }
}
