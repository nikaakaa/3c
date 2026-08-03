using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace TreeDesigner.Editor
{
    interface IGraphAuthoringProjectedStateMachineMovable
    {
        GraphAuthoringElementId ElementId { get; }
    }

    public sealed class GraphAuthoringStateMachineBinding
    {
        public GraphAuthoringStateMachineBinding(
            IGraphAuthoringStateMachineProjection document,
            GraphAuthoringCapabilityCatalog capabilities,
            IGraphAuthoringDomainMutation mutation,
            IGraphAuthoringStateMachinePolicy policy)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
            Mutation = mutation ?? throw new ArgumentNullException(nameof(mutation));
            Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            if (document.SemanticKind != policy.SemanticKind)
                throw new InvalidOperationException($"StateMachine semantic '{document.SemanticKind}' cannot use policy '{policy.SemanticKind}'.");
            GraphAuthoringStateMachineProjectionValidator.RequireValid(document);
            policy.ValidateDocument(document);
        }

        public IGraphAuthoringStateMachineProjection Document { get; }
        public GraphAuthoringCapabilityCatalog Capabilities { get; }
        public IGraphAuthoringDomainMutation Mutation { get; }
        public IGraphAuthoringStateMachinePolicy Policy { get; }
    }

    sealed class GraphAuthoringProjectedStateMachinePortView :
        GraphAuthoringPortViewBase
    {
        public GraphAuthoringProjectedStateMachinePortView(
            GraphAuthoringElementId ownerId,
            Direction direction)
            : base(Orientation.Horizontal, direction, Capacity.Multi, typeof(GraphAuthoringStateMachineLink))
        {
            OwnerId = ownerId.IsValid ? ownerId : throw new ArgumentException("StateMachine port owner is missing.", nameof(ownerId));
            portName = string.Empty;
            InstallConnector<Edge>();
        }

        public GraphAuthoringElementId OwnerId { get; }
        sealed class GraphAuthoringStateMachineLink { }
    }

    sealed class GraphAuthoringProjectedStateMachineEntryView :
        GraphAuthoringNodeViewBase,
        IGraphAuthoringProjectedStateMachineMovable
    {
        public GraphAuthoringProjectedStateMachineEntryView(
            GraphAuthoringStateMachineEntryProjection projection)
        {
            Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            BindAuthoringPresentation(
                projection.ElementId.Value,
                "Entry",
                projection.Position);
            capabilities &= ~Capabilities.Deletable;
            Output = new GraphAuthoringProjectedStateMachinePortView(
                projection.ElementId,
                Direction.Output);
            outputContainer.Add(Output);
            AddToClassList("graph-authoring-state-machine-entry");
            RefreshPorts();
        }

        public GraphAuthoringStateMachineEntryProjection Projection { get; }
        public GraphAuthoringElementId ElementId => Projection.ElementId;
        public GraphAuthoringProjectedStateMachinePortView Output { get; }
    }

    sealed class GraphAuthoringProjectedStateMachineStateView :
        GraphAuthoringNodeViewBase,
        IGraphAuthoringProjectedStateMachineMovable
    {
        public GraphAuthoringProjectedStateMachineStateView(
            GraphAuthoringStateProjection projection,
            GraphAuthoringCapabilityDescriptor capability)
        {
            Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            Capability = capability ?? throw new ArgumentNullException(nameof(capability));
            BindAuthoringPresentation(
                projection.StateId.Value,
                string.IsNullOrWhiteSpace(projection.DisplayName)
                    ? capability.DisplayName
                    : projection.DisplayName,
                projection.Position,
                capability.Color);
            Input = new GraphAuthoringProjectedStateMachinePortView(
                projection.StateId,
                Direction.Input);
            Output = new GraphAuthoringProjectedStateMachinePortView(
                projection.StateId,
                Direction.Output);
            inputContainer.Add(Input);
            outputContainer.Add(Output);
            if (projection.ChildGraphId.IsValid)
            {
                var child = new Label("Open Graph");
                child.AddToClassList("graph-authoring-state-child-graph");
                extensionContainer.Add(child);
            }
            if (!string.IsNullOrWhiteSpace(projection.Status))
                extensionContainer.Add(new Label(projection.Status));
            AddToClassList("graph-authoring-state-machine-state");
            RefreshExpandedState();
            RefreshPorts();
        }

        public GraphAuthoringStateProjection Projection { get; }
        public GraphAuthoringElementId ElementId => Projection.StateId;
        public GraphAuthoringCapabilityDescriptor Capability { get; }
        public GraphAuthoringProjectedStateMachinePortView Input { get; }
        public GraphAuthoringProjectedStateMachinePortView Output { get; }
    }

    sealed class GraphAuthoringProjectedStateMachineAliasView :
        GraphAuthoringNodeViewBase,
        IGraphAuthoringProjectedStateMachineMovable
    {
        public GraphAuthoringProjectedStateMachineAliasView(
            GraphAuthoringStateAliasProjection projection)
        {
            Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            BindAuthoringPresentation(
                projection.AliasId.Value,
                string.IsNullOrWhiteSpace(projection.DisplayName)
                    ? "Alias"
                    : projection.DisplayName,
                projection.Position);
            Input = new GraphAuthoringProjectedStateMachinePortView(
                projection.AliasId,
                Direction.Input);
            Output = new GraphAuthoringProjectedStateMachinePortView(
                projection.AliasId,
                Direction.Output);
            inputContainer.Add(Input);
            outputContainer.Add(Output);
            AddToClassList("graph-authoring-state-machine-alias");
            RefreshPorts();
        }

        public GraphAuthoringStateAliasProjection Projection { get; }
        public GraphAuthoringElementId ElementId => Projection.AliasId;
        public GraphAuthoringProjectedStateMachinePortView Input { get; }
        public GraphAuthoringProjectedStateMachinePortView Output { get; }
    }

    sealed class GraphAuthoringProjectedStateMachineTransitionView :
        GraphAuthoringEdgeViewBase
    {
        public GraphAuthoringProjectedStateMachineTransitionView(
            GraphAuthoringTransitionProjection projection)
        {
            Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            BindAuthoringIdentity(projection.TransitionId.Value);
            tooltip = projection.DisplayName ?? string.Empty;
            AddToClassList("graph-authoring-state-machine-transition");
        }

        public GraphAuthoringTransitionProjection Projection { get; }
    }

    public partial class GraphAuthoringCanvasView
    {
        readonly Dictionary<
            GraphAuthoringElementId,
            GraphAuthoringProjectedStateMachinePortView>
            m_StateMachineInputs =
                new Dictionary<
                    GraphAuthoringElementId,
                    GraphAuthoringProjectedStateMachinePortView>();
        readonly Dictionary<
            GraphAuthoringElementId,
            GraphAuthoringProjectedStateMachinePortView>
            m_StateMachineOutputs =
                new Dictionary<
                    GraphAuthoringElementId,
                    GraphAuthoringProjectedStateMachinePortView>();
        GraphAuthoringStateMachineBinding m_StateMachineBinding;
        bool m_PopulatingStateMachine;

        public event Action<Vector2>
            StateMachineNodeCreationRequested;
        public GraphAuthoringStateMachineBinding StateMachineBinding =>
            m_StateMachineBinding;

        public void BindStateMachine(
            GraphAuthoringStateMachineBinding binding)
        {
            m_StateMachineBinding = binding ??
                throw new ArgumentNullException(nameof(binding));
            graphViewChanged = ApplyStateMachineChange;
            nodeCreationRequest = context =>
                StateMachineNodeCreationRequested?.Invoke(
                    context.screenMousePosition);
            PopulateStateMachine();
        }

        public void PopulateStateMachine()
        {
            if (m_StateMachineBinding == null)
                throw new InvalidOperationException("StateMachine surface is not bound.");
            m_PopulatingStateMachine = true;
            try
            {
                DeleteElements(graphElements.ToList());
                m_StateMachineInputs.Clear();
                m_StateMachineOutputs.Clear();
                AddEntry(m_StateMachineBinding.Document.Entry);
                foreach (GraphAuthoringStateProjection state in m_StateMachineBinding.Document.States ?? Array.Empty<GraphAuthoringStateProjection>())
                    AddState(state);
                foreach (GraphAuthoringStateAliasProjection alias in m_StateMachineBinding.Document.Aliases ?? Array.Empty<GraphAuthoringStateAliasProjection>())
                    AddAlias(alias);
                if (m_StateMachineBinding.Document.Entry.TargetStateId.IsValid)
                    AddLink(m_StateMachineBinding.Document.Entry.ElementId, m_StateMachineBinding.Document.Entry.TargetStateId, null);
                foreach (GraphAuthoringTransitionProjection transition in m_StateMachineBinding.Document.Transitions ?? Array.Empty<GraphAuthoringTransitionProjection>())
                    AddLink(transition.SourceStateId, transition.TargetStateId, transition);
            }
            finally
            {
                m_PopulatingStateMachine = false;
            }
            SetRuntimeReadOnly(
                m_StateMachineBinding.Mutation.ReadOnly);
        }

        List<Port> GetStateMachineCompatiblePorts(
            Port startPort)
        {
            if (m_StateMachineBinding == null ||
                m_StateMachineBinding.Mutation.ReadOnly ||
                !(startPort is
                    GraphAuthoringProjectedStateMachinePortView
                    source))
                return new List<Port>();
            return ports.OfType<
                    GraphAuthoringProjectedStateMachinePortView>()
                .Where(target => source.direction != target.direction && !source.OwnerId.Equals(target.OwnerId) && CanConnect(source, target))
                .Cast<Port>()
                .ToList();
        }

        public void OpenChildGraph(
            GraphAuthoringElementId stateId) =>
            m_StateMachineBinding.Policy.OpenStateChildGraph(
                m_StateMachineBinding.Document,
                stateId);

        public void OpenTransitionRule(
            GraphAuthoringElementId transitionId) =>
            m_StateMachineBinding.Policy.OpenTransitionRule(
                m_StateMachineBinding.Document,
                transitionId);

        IReadOnlyList<GraphAuthoringSelection>
            GetStateMachineStableSelection()
        {
            var result = new List<GraphAuthoringSelection>();
            for (int i = 0; i < selection.Count; i++)
            {
                if (selection[i] is
                    GraphAuthoringProjectedStateMachineStateView
                    state)
                    result.Add(new GraphAuthoringSelection(
                        GraphAuthoringSelectionKind.State,
                        state.Projection.StateId));
                else if (selection[i] is
                         GraphAuthoringProjectedStateMachineAliasView
                         alias)
                    result.Add(new GraphAuthoringSelection(
                        GraphAuthoringSelectionKind.State,
                        alias.Projection.AliasId));
                else if (selection[i] is
                         GraphAuthoringProjectedStateMachineTransitionView
                         transition)
                    result.Add(new GraphAuthoringSelection(
                        GraphAuthoringSelectionKind.Transition,
                        transition.Projection.TransitionId));
            }
            return result;
        }

        void FocusStateMachineElement(
            GraphAuthoringElementId elementId)
        {
            GraphElement element = graphElements
                .FirstOrDefault(value =>
                    string.Equals(
                        value.viewDataKey,
                        elementId.Value,
                        StringComparison.Ordinal));
            if (element == null)
                return;
            ClearSelection();
            AddToSelection(element);
            FrameSelection();
        }

        void AddEntry(GraphAuthoringStateMachineEntryProjection entry)
        {
            if (entry == null)
                throw new InvalidOperationException("StateMachine entry is missing.");
            var view =
                new GraphAuthoringProjectedStateMachineEntryView(
                    entry);
            if (!m_StateMachineBinding.Policy.PersistsLayout)
                view.capabilities &= ~Capabilities.Movable;
            AddOutput(entry.ElementId, view.Output);
            AddElement(view);
        }

        void AddState(GraphAuthoringStateProjection state)
        {
            GraphAuthoringCapabilityDescriptor capability =
                m_StateMachineBinding.Capabilities.Require(
                    state.CapabilityId,
                    m_StateMachineBinding.Document.DomainId,
                    m_StateMachineBinding.Document.DocumentRoleId);
            var view =
                new GraphAuthoringProjectedStateMachineStateView(
                    state,
                    capability);
            if (!m_StateMachineBinding.Policy.PersistsLayout)
                view.capabilities &= ~Capabilities.Movable;
            view.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0 || evt.clickCount != 2 ||
                    !state.ChildGraphId.IsValid)
                {
                    return;
                }
                OpenChildGraph(state.StateId);
                evt.StopPropagation();
            });
            AddInput(state.StateId, view.Input);
            AddOutput(state.StateId, view.Output);
            AddElement(view);
        }

        void AddAlias(GraphAuthoringStateAliasProjection alias)
        {
            var view =
                new GraphAuthoringProjectedStateMachineAliasView(
                    alias);
            if (!m_StateMachineBinding.Policy.PersistsLayout)
                view.capabilities &= ~Capabilities.Movable;
            AddInput(alias.AliasId, view.Input);
            AddOutput(alias.AliasId, view.Output);
            AddElement(view);
        }

        void AddInput(
            GraphAuthoringElementId id,
            GraphAuthoringProjectedStateMachinePortView port)
        {
            if (!m_StateMachineInputs.TryAdd(id, port))
                throw new InvalidOperationException($"StateMachine contains duplicate input owner '{id}'.");
        }

        void AddOutput(
            GraphAuthoringElementId id,
            GraphAuthoringProjectedStateMachinePortView port)
        {
            if (!m_StateMachineOutputs.TryAdd(id, port))
                throw new InvalidOperationException($"StateMachine contains duplicate output owner '{id}'.");
        }

        void AddLink(GraphAuthoringElementId sourceId, GraphAuthoringElementId targetId, GraphAuthoringTransitionProjection transition)
        {
            if (!m_StateMachineOutputs.TryGetValue(
                    sourceId,
                    out GraphAuthoringProjectedStateMachinePortView
                    output) ||
                !m_StateMachineInputs.TryGetValue(
                    targetId,
                    out GraphAuthoringProjectedStateMachinePortView
                    input))
                throw new InvalidOperationException($"StateMachine link '{sourceId}' → '{targetId}' has a missing endpoint.");
            Edge edge = transition == null
                ? new Edge()
                : new GraphAuthoringProjectedStateMachineTransitionView(
                    transition);
            if (edge is
                GraphAuthoringProjectedStateMachineTransitionView
                transitionView)
            {
                transitionView.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button != 0 || evt.clickCount != 2)
                        return;
                    OpenTransitionRule(
                        transitionView.Projection.TransitionId);
                    evt.StopPropagation();
                });
            }
            edge.output = output;
            edge.input = input;
            output.Connect(edge);
            input.Connect(edge);
            AddElement(edge);
        }

        bool CanConnect(
            GraphAuthoringProjectedStateMachinePortView first,
            GraphAuthoringProjectedStateMachinePortView second)
        {
            GraphAuthoringProjectedStateMachinePortView output =
                first.direction == Direction.Output ? first : second;
            GraphAuthoringProjectedStateMachinePortView input =
                first.direction == Direction.Input ? first : second;
            return m_StateMachineBinding.Policy.CanCreateTransition(
                m_StateMachineBinding.Document,
                output.OwnerId,
                input.OwnerId);
        }

        GraphViewChange ApplyStateMachineChange(
            GraphViewChange change)
        {
            if (m_PopulatingStateMachine ||
                m_StateMachineBinding == null)
                return change;
            if (m_StateMachineBinding.Mutation.ReadOnly)
            {
                change.edgesToCreate = null;
                change.elementsToRemove = new List<GraphElement>();
                change.movedElements = null;
                return change;
            }
            var requests = new List<GraphAuthoringMutationRequest>();
            IEnumerable<GraphElement> removed = change.elementsToRemove ?? Enumerable.Empty<GraphElement>();
            IEnumerable<GraphElement> moved = change.movedElements ?? Enumerable.Empty<GraphElement>();
            IEnumerable<Edge> created = change.edgesToCreate ?? Enumerable.Empty<Edge>();
            var acceptedEdges = new List<Edge>();
            foreach (
                GraphAuthoringProjectedStateMachineTransitionView
                transition in removed.OfType<
                    GraphAuthoringProjectedStateMachineTransitionView>())
                requests.Add(new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.DeleteTransition, transition.Projection.TransitionId));
            foreach (
                 GraphAuthoringProjectedStateMachineStateView state in
                removed.OfType<
                    GraphAuthoringProjectedStateMachineStateView>())
                requests.Add(new GraphAuthoringMutationRequest(GraphAuthoringMutationKind.DeleteState, state.Projection.StateId));
            foreach (
                GraphAuthoringProjectedStateMachineAliasView alias in
                removed.OfType<GraphAuthoringProjectedStateMachineAliasView>())
                requests.Add(new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.DeleteStateAlias,
                    alias.Projection.AliasId));
            if (m_StateMachineBinding.Policy.PersistsLayout)
            {
                foreach (GraphElement element in moved
                             .Where(value => value is IGraphAuthoringProjectedStateMachineMovable)
                             .GroupBy(value => ((IGraphAuthoringProjectedStateMachineMovable)value).ElementId)
                             .Select(value => value.Last()))
                {
                    var movable =
                        (IGraphAuthoringProjectedStateMachineMovable)element;
                    requests.Add(new GraphAuthoringMutationRequest(
                        GraphAuthoringMutationKind.MoveElement,
                        movable.ElementId,
                        position: element.GetPosition().position));
                }
            }
            foreach (Edge edge in created)
            {
                if (!(edge.output is
                        GraphAuthoringProjectedStateMachinePortView
                        output) ||
                    !(edge.input is
                        GraphAuthoringProjectedStateMachinePortView
                        input))
                    throw new InvalidOperationException("StateMachine transition endpoints are invalid.");
                object payload =
                    m_StateMachineBinding.Policy.CreateTransitionPayload(
                    m_StateMachineBinding.Document,
                    output.OwnerId,
                    input.OwnerId);
                bool entryLink = output.OwnerId.Equals(
                    m_StateMachineBinding.Document.Entry.ElementId);
                if (!entryLink && payload == null)
                    continue;
                acceptedEdges.Add(edge);
                requests.Add(new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.CreateTransition,
                    output.OwnerId,
                    secondaryTargetId: input.OwnerId,
                    value: payload));
            }
            if (requests.Count > 0)
            {
                m_StateMachineBinding.Mutation.Apply(
                    m_StateMachineBinding.Document,
                    requests);
                schedule.Execute(PopulateStateMachine);
            }
            if (change.edgesToCreate != null)
                change.edgesToCreate = acceptedEdges;
            return change;
        }
    }

    public sealed class GraphAuthoringPageStack
    {
        readonly List<GraphAuthoringPageProjection> m_Pages = new List<GraphAuthoringPageProjection>();

        public IReadOnlyList<GraphAuthoringPageProjection> Pages => m_Pages;
        public GraphAuthoringPageProjection Current => m_Pages.Count == 0 ? default : m_Pages[m_Pages.Count - 1];

        public void Reset(GraphAuthoringPageProjection root)
        {
            m_Pages.Clear();
            m_Pages.Add(root);
        }

        public void Push(GraphAuthoringPageProjection page)
        {
            int existing = m_Pages.FindIndex(value => value.PageId.Equals(page.PageId));
            if (existing >= 0)
                m_Pages.RemoveRange(existing, m_Pages.Count - existing);
            m_Pages.Add(page);
        }

        public bool Pop()
        {
            if (m_Pages.Count <= 1)
                return false;
            m_Pages.RemoveAt(m_Pages.Count - 1);
            return true;
        }

        public void NavigateTo(int index)
        {
            if (index < 0 || index >= m_Pages.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (index + 1 < m_Pages.Count)
                m_Pages.RemoveRange(index + 1, m_Pages.Count - index - 1);
        }
    }
}
